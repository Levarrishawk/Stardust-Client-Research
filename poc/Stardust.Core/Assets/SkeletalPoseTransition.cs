using System.Numerics;

namespace Stardust.Core.Assets;

public static class SkeletalPoseBlender
{
    public static IReadOnlyList<Matrix4x4> Blend(IReadOnlyList<Matrix4x4> first,IReadOnlyList<Matrix4x4> second,float weight)
    {
        ArgumentNullException.ThrowIfNull(first);ArgumentNullException.ThrowIfNull(second);
        if(!float.IsFinite(weight)||weight<0||weight>1||first.Count!=second.Count||first.Count is <1 or >8192)throw new InvalidDataException("Invalid rigid pose blend count/weight.");
        foreach(var matrix in first)SkeletonBindPoseBuilder.RigidInverse(matrix);
        foreach(var matrix in second)SkeletonBindPoseBuilder.RigidInverse(matrix);
        if(weight==0)return Array.AsReadOnly(first.ToArray());if(weight==1)return Array.AsReadOnly(second.ToArray());
        var result=new Matrix4x4[first.Count];
        for(int i=0;i<result.Length;i++)
        {
            var a=Quaternion.Normalize(Quaternion.CreateFromRotationMatrix(first[i]));var b=Quaternion.Normalize(Quaternion.CreateFromRotationMatrix(second[i]));
            result[i]=Matrix4x4.CreateFromQuaternion(Quaternion.Normalize(Quaternion.Slerp(a,b,weight)));
            result[i].Translation=Vector3.Lerp(first[i].Translation,second[i].Translation,weight);SkeletonBindPoseBuilder.RigidInverse(result[i]);
        }
        return Array.AsReadOnly(result);
    }
}

public sealed class PreparedSkeletalPoseTransition
{
    public IReadOnlyList<Matrix4x4> LocalPose{get;}
    public string TargetKey{get;}
    public float Weight{get;}
    public bool Transitioning=>Weight<1;
    internal SkeletalPoseTransition Owner{get;}
    internal long Version{get;}
    internal IReadOnlyList<Matrix4x4> From{get;}
    internal double Age{get;}
    internal PreparedSkeletalPoseTransition(SkeletalPoseTransition owner,long version,string key,IReadOnlyList<Matrix4x4> from,IReadOnlyList<Matrix4x4> pose,double age,float weight)
    {Owner=owner;Version=version;TargetKey=key;From=from;LocalPose=pose;Age=age;Weight=weight;}
}

/// <summary>Explicit snapshot-to-moving-target crossfade; prepare before GPU publication, commit after success.</summary>
public sealed class SkeletalPoseTransition
{
    private IReadOnlyList<Matrix4x4> from;
    private double age;
    private long version;
    private readonly double duration;
    public IReadOnlyList<Matrix4x4> CurrentPose{get;private set;}
    public string TargetKey{get;private set;}
    public bool Transitioning=>age<duration;
    public SkeletalPoseTransition(IReadOnlyList<Matrix4x4> initial,string key,double durationSeconds)
    {
        CheckKey(key);if(!double.IsFinite(durationSeconds)||durationSeconds<=0||durationSeconds>1)throw new InvalidDataException("Bounded explicit transition duration required.");
        CurrentPose=SkeletalPoseBlender.Blend(initial,initial,0);from=CurrentPose;TargetKey=key;duration=durationSeconds;age=duration;
    }
    public PreparedSkeletalPoseTransition Prepare(string key,IReadOnlyList<Matrix4x4> target,double seconds)
    {
        CheckKey(key);if(!double.IsFinite(seconds)||seconds<0||seconds>.25)throw new InvalidDataException("Bounded pose-transition tick required.");
        bool changed=key!=TargetKey;var nextFrom=changed?CurrentPose:from;double nextAge=Math.Min(duration,(changed?0:age)+seconds);float weight=(float)(nextAge/duration);
        var pose=SkeletalPoseBlender.Blend(nextFrom,target,weight);
        return new(this,version,key,nextFrom,pose,nextAge,weight);
    }
    public void Commit(PreparedSkeletalPoseTransition candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);if(!ReferenceEquals(candidate.Owner,this)||candidate.Version!=version)throw new InvalidOperationException("Foreign/stale pose transition publication.");
        long nextVersion=checked(version+1);CurrentPose=candidate.LocalPose;from=candidate.From;TargetKey=candidate.TargetKey;age=candidate.Age;version=nextVersion;
    }
    private static void CheckKey(string key)
    {if(string.IsNullOrEmpty(key)||key.Length>64||key.Any(c=>c<32||c>126))throw new InvalidDataException("Bounded transition key required.");}
}
