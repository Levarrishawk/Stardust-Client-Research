using System.Numerics;

namespace Stardust.Core.Assets;

public enum MissingAnimationTransformPolicy{Reject,BindPose}
public sealed record SkeletonAnimationPose(IReadOnlyList<Matrix4x4> LocalPose,
    IReadOnlyList<string> UnusedAnimationTransforms,IReadOnlyList<string> MissingSkeletonTransforms);

public static class SkeletonAnimationPoseBuilder
{
    /// <summary>Sample rotation sits between RPST and BPRO; sampled translation adds to BPTR.</summary>
    public static SkeletonAnimationPose Create(BasicSkeletonDescription skeleton,IReadOnlyList<SkeletalTransformSample> samples,
        MissingAnimationTransformPolicy missingPolicy=MissingAnimationTransformPolicy.Reject)
    {
        ArgumentNullException.ThrowIfNull(skeleton);ArgumentNullException.ThrowIfNull(samples);
        if(samples.Count>2048||!Enum.IsDefined(missingPolicy))throw new InvalidDataException("Animation pose count/policy mismatch.");
        var local=SkeletonBindPoseBuilder.Create(skeleton).LocalBind.ToArray();
        var lookup=new Dictionary<string,SkeletalTransformSample>(StringComparer.OrdinalIgnoreCase);
        foreach(var sample in samples)
        {
            CheckName(sample.Name);if(!lookup.TryAdd(sample.Name,sample))throw new InvalidDataException("Ambiguous animation sample name.");
            CheckQuaternion(sample.Rotation);var p=sample.Translation;
            if(!float.IsFinite(p.X)||!float.IsFinite(p.Y)||!float.IsFinite(p.Z)||Math.Abs(p.X)>1e6||Math.Abs(p.Y)>1e6||Math.Abs(p.Z)>1e6)
                throw new InvalidDataException("Invalid animation sample translation.");
        }
        var used=new HashSet<string>(StringComparer.OrdinalIgnoreCase);var missing=new List<string>();
        for(int i=0;i<skeleton.Joints.Count;i++)
        {
            var joint=skeleton.Joints[i];CheckName(joint.Name);if(!used.Add(joint.Name))throw new InvalidDataException("Ambiguous skeleton pose name.");
            if(!lookup.TryGetValue(joint.Name,out var sample)){missing.Add(joint.Name);continue;}
            Quaternion Convert(ScalarFirstQuaternion q)=>new(q.X,q.Y,q.Z,q.W);
            Quaternion rotation=Convert(joint.Rpst)*(sample.Rotation*Convert(joint.Bpro)*Convert(joint.Rpre));
            CheckQuaternion(rotation);
            var matrix=Matrix4x4.CreateFromQuaternion(Quaternion.Normalize(rotation));
            matrix.Translation=joint.Bptr+sample.Translation;SkeletonBindPoseBuilder.RigidInverse(matrix);local[i]=matrix;
        }
        if(missing.Count>0&&missingPolicy==MissingAnimationTransformPolicy.Reject)throw new InvalidDataException("Animation lacks required skeleton transforms.");
        return new(Array.AsReadOnly(local),Array.AsReadOnly(samples.Where(s=>!used.Contains(s.Name)).Select(s=>s.Name).ToArray()),Array.AsReadOnly(missing.ToArray()));
    }

    /// <summary>Apply one selected skeleton's local pose; other attached skeletons retain their explicit binds.</summary>
    public static IReadOnlyList<Matrix4x4> Apply(SkeletalJointBinding binding,string skeletonPath,BasicSkeletonDescription skeleton,SkeletonAnimationPose pose)
    {
        ArgumentNullException.ThrowIfNull(binding);ArgumentNullException.ThrowIfNull(skeleton);ArgumentNullException.ThrowIfNull(pose);
        if(pose.LocalPose.Count!=skeleton.Joints.Count||binding.Joints.Count>8192)throw new InvalidDataException("Applied animation pose count mismatch.");
        var local=binding.Joints.Select(j=>j.LocalBind).ToArray();var covered=new HashSet<int>();
        for(int i=0;i<binding.Joints.Count;i++)
        {
            var joint=binding.Joints[i];if(joint.SkeletonPath!=skeletonPath)continue;
            int source=joint.SourceJoint;
            if(source<0||source>=skeleton.Joints.Count||!covered.Add(source)||joint.Name!=skeleton.Joints[source].Name)
                throw new InvalidDataException("Applied animation source binding mismatch.");
            SkeletonBindPoseBuilder.RigidInverse(pose.LocalPose[source]);local[i]=pose.LocalPose[source];
        }
        if(covered.Count!=skeleton.Joints.Count||covered.Count==0)throw new InvalidDataException("Selected animation skeleton absent/incomplete.");
        return Array.AsReadOnly(local);
    }
    private static void CheckName(string name)
    {if(string.IsNullOrEmpty(name)||name.Length>1024||name.Any(c=>c<32||c>126))throw new InvalidDataException("Invalid animation pose name.");}
    private static void CheckQuaternion(Quaternion q)
    {double norm=(double)q.X*q.X+(double)q.Y*q.Y+(double)q.Z*q.Z+(double)q.W*q.W;if(!double.IsFinite(norm)||Math.Abs(norm-1)>.001)throw new InvalidDataException("Invalid animation pose rotation.");}
}
