namespace Stardust.Core.Assets;

/// <summary>One explicitly selected clip and skeleton for finite preview poses; no graph/event/root-motion execution.</summary>
public sealed class PreparedSkeletalAnimation
{
    private readonly CompressedKeyframeSampler sampler;
    private readonly BasicSkeletonDescription skeleton;
    private readonly MissingAnimationTransformPolicy missingPolicy;
    public string SkeletonPath{get;}
    public string ClipPath{get;}
    public string ClipSha256{get;}
    public float FrameRate=>sampler.FrameRate;
    public ushort EndFrame=>sampler.EndFrame;
    public double DurationSeconds=>sampler.DurationSeconds;
    public double ProxyTimeScale{get;}
    public CompressedLocomotion? Locomotion{get;}
    public int RepairedPackedValues=>sampler.RepairedPackedValues;
    public IReadOnlyList<string> UnusedAnimationTransforms{get;}
    public IReadOnlyList<string> MissingSkeletonTransforms{get;}

    private PreparedSkeletalAnimation(CreatureAppearanceDescriptorBundle bundle,MissingAnimationTransformPolicy policy,bool allowTranslationSuffix)
    {
        var selected=bundle.SelectedAnimation??throw new InvalidDataException("Explicit selected animation required.");
        ClipPath=selected.Proxy.ClipPath;ClipSha256=selected.Selection.ClipSha256;
        if(bundle.Skeletons is null)throw new InvalidDataException("Selected animation requires owned skeletons.");
        var references=bundle.Appearance.AnimationTables.Where(r=>r.AnimationTablePath==selected.Selection.TablePath).ToArray();
        if(references.Length!=1||!bundle.Skeletons.TryGetValue(references[0].SkeletonPath,out var source))
            throw new InvalidDataException("Selected animation table has ambiguous/missing skeleton mapping.");
        SkeletonPath=references[0].SkeletonPath;
        var levels=source.ExposedLevels();if(levels.Count==0)throw new InvalidDataException("Selected animation skeleton has no supported level.");
        var basic=levels[0];skeleton=new(Array.AsReadOnly(basic.Joints.ToArray()));missingPolicy=policy;
        var clip=CompressedKeyframeDecoder.Create(selected.Clip);sampler=new(clip);
        if(allowTranslationSuffix)Locomotion=CompressedLocomotion.Create(clip);
        else if(sampler.HasUninterpretedSuffix)throw new InvalidDataException("Preview animation event/root suffix execution is unsupported.");
        ProxyTimeScale=selected.Proxy.EffectiveTimeScale;
        if(!double.IsFinite(ProxyTimeScale)||ProxyTimeScale<=0||ProxyTimeScale>1e6)throw new InvalidDataException("Invalid prepared proxy scale.");
        var diagnostic=SkeletonAnimationPoseBuilder.Create(skeleton,sampler.SampleFrame(0),policy);
        UnusedAnimationTransforms=diagnostic.UnusedAnimationTransforms;MissingSkeletonTransforms=diagnostic.MissingSkeletonTransforms;
    }
    public static PreparedSkeletalAnimation Create(CreatureAppearanceDescriptorBundle bundle,
        MissingAnimationTransformPolicy policy=MissingAnimationTransformPolicy.Reject,bool allowTranslationSuffix=false)
    {ArgumentNullException.ThrowIfNull(bundle);return new(bundle,policy,allowTranslationSuffix);}

    public IReadOnlyList<System.Numerics.Matrix4x4> CreateLocalPose(SkeletalJointBinding binding,double frame)
    {
        var pose=SkeletonAnimationPoseBuilder.Create(skeleton,sampler.SampleFrame(frame),missingPolicy);
        return SkeletonAnimationPoseBuilder.Apply(binding,SkeletonPath,skeleton,pose);
    }
}
