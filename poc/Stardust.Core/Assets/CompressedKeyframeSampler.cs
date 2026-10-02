using System.Numerics;

namespace Stardust.Core.Assets;

public sealed record SkeletalTransformSample(string Name,Quaternion Rotation,Vector3 Translation,bool RotationRepaired);

/// <summary>Owned finite preview sampler. Frame time is explicit; no event, root-motion or state-machine execution.</summary>
public sealed class CompressedKeyframeSampler
{
    private sealed record RotationCurve(ushort[] Frames,Quaternion[] Values,bool[] Repairs);
    private sealed record TranslationCurve(ushort[] Frames,float[] Values);
    private readonly CompressedAnimationTransform[] transforms;
    private readonly RotationCurve[] rotations;
    private readonly DecodedPackedQuaternion[] staticRotations;
    private readonly TranslationCurve[] translations;
    private readonly float[] staticTranslations;
    public float FrameRate{get;}
    public ushort EndFrame{get;}
    public double DurationSeconds=>EndFrame/(double)FrameRate;
    public int RepairedPackedValues{get;}
    public bool HasUninterpretedSuffix{get;}

    public CompressedKeyframeSampler(CompressedKeyframeDescription clip)
    {
        ArgumentNullException.ThrowIfNull(clip);
        if(!float.IsFinite(clip.FrameRate)||clip.FrameRate<=0||clip.FrameRate>1000||clip.EndFrame==0||clip.Transforms.Count>2048||
           clip.AnimatedRotations.Count>4096||clip.StaticRotations.Count>4096||clip.AnimatedTranslations.Count>4096||clip.StaticTranslations.Count>4096)
            throw new InvalidDataException("Invalid sampling descriptor budget/time base.");
        FrameRate=clip.FrameRate;EndFrame=clip.EndFrame;HasUninterpretedSuffix=clip.UninterpretedSuffix.Count!=0;
        transforms=clip.Transforms.ToArray();int totalKeys=0;
        rotations=clip.AnimatedRotations.Select(channel=>
        {
            var keys=channel.Keys.ToArray();CheckFrames(keys.Select(k=>k.Frame).ToArray());
            var decoded=keys.Select(k=>PackedQuaternionDecoder.Decode(new(channel.XSelector,channel.YSelector,channel.ZSelector,k.Value))).ToArray();
            return new RotationCurve(keys.Select(k=>k.Frame).ToArray(),decoded.Select(q=>q.Publication).ToArray(),decoded.Select(q=>q.Repaired).ToArray());
        }).ToArray();
        staticRotations=clip.StaticRotations.Select(PackedQuaternionDecoder.Decode).ToArray();
        translations=clip.AnimatedTranslations.Select(channel=>
        {
            var keys=channel.Keys.ToArray();var frames=keys.Select(k=>k.Frame).ToArray();CheckFrames(frames);
            var values=keys.Select(k=>k.Value).ToArray();foreach(float value in values)CheckValue(value);
            return new TranslationCurve(frames,values);
        }).ToArray();
        staticTranslations=clip.StaticTranslations.ToArray();foreach(float value in staticTranslations)CheckValue(value);
        var names=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach(var t in transforms)
        {
            if(string.IsNullOrEmpty(t.Name)||t.Name.Length>1024||t.Name.Any(c=>c<32||c>126)||!names.Add(t.Name)||
               (t.TranslationMask&~56)!=0||t.RotationIndex>=(t.AnimatedRotation?rotations.Length:staticRotations.Length))
                throw new InvalidDataException("Invalid sampling transform name/flags/rotation index.");
            ushort[] indices={t.TranslationX,t.TranslationY,t.TranslationZ};
            for(int axis=0;axis<3;axis++)if(indices[axis]>=((t.TranslationMask&(8<<axis))!=0?translations.Length:staticTranslations.Length))
                throw new InvalidDataException("Sampling translation index out of range.");
        }
        RepairedPackedValues=rotations.Sum(c=>c.Repairs.Count(r=>r))+staticRotations.Count(q=>q.Repaired);
        void CheckFrames(ushort[] frames)
        {
            if(frames.Length<2||frames.Length>65535||(totalKeys+=frames.Length)>262144)throw new InvalidDataException("Sampling key budget/count.");
            for(int i=0;i<frames.Length;i++)if(frames[i]>EndFrame||(i>0&&frames[i]<=frames[i-1]))throw new InvalidDataException("Sampling key times must increase within clip.");
        }
    }

    public IReadOnlyList<SkeletalTransformSample> SampleSeconds(double seconds)
    {
        if(!double.IsFinite(seconds)||seconds<0||seconds>DurationSeconds)throw new InvalidDataException("Sample seconds outside clip.");
        return SampleFrame(Math.Min(EndFrame,seconds*FrameRate));
    }

    public IReadOnlyList<SkeletalTransformSample> SampleFrame(double frame)
    {
        if(!double.IsFinite(frame)||frame<0||frame>EndFrame)throw new InvalidDataException("Sample frame outside clip.");
        var result=new SkeletalTransformSample[transforms.Length];
        for(int i=0;i<result.Length;i++)
        {
            var t=transforms[i];Quaternion rotation;bool repaired;
            if(t.AnimatedRotation)
            {
                var curve=rotations[t.RotationIndex];var interval=Interval(curve.Frames,frame);
                rotation=interval.Low==interval.High?curve.Values[interval.Low]:Quaternion.Normalize(Quaternion.Slerp(curve.Values[interval.Low],curve.Values[interval.High],interval.Fraction));
                repaired=curve.Repairs[interval.Low]||curve.Repairs[interval.High];
            }
            else{var q=staticRotations[t.RotationIndex];rotation=q.Publication;repaired=q.Repaired;}
            float Axis(ushort index,int mask)
            {
                if((t.TranslationMask&mask)==0)return staticTranslations[index];
                var curve=translations[index];var interval=Interval(curve.Frames,frame);
                return interval.Low==interval.High?curve.Values[interval.Low]:(float)((1d-interval.Fraction)*curve.Values[interval.Low]+interval.Fraction*(double)curve.Values[interval.High]);
            }
            result[i]=new(t.Name,rotation,new(Axis(t.TranslationX,8),Axis(t.TranslationY,16),Axis(t.TranslationZ,32)),repaired);
        }
        return Array.AsReadOnly(result);
    }

    private static (int Low,int High,float Fraction) Interval(ushort[] frames,double frame)
    {
        if(frame<=frames[0])return(0,0,0);
        int last=frames.Length-1;if(frame>=frames[last])return(last,last,0);
        int low=0,high=last;
        while(high-low>1){int middle=(low+high)/2;if(frames[middle]<=frame)low=middle;else high=middle;}
        if(frame==frames[low])return(low,low,0);
        return(low,high,(float)((frame-frames[low])/(frames[high]-frames[low])));
    }
    private static void CheckValue(float value)
    {if(!float.IsFinite(value)||Math.Abs(value)>1e6)throw new InvalidDataException("Invalid sampling translation.");}
}
