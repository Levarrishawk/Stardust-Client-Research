using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class CompressedSamplingScenarios
{
    internal static void Run()
    {
        var keys=new[]{new CompressedRotationKey(0,0),new CompressedRotationKey(10,511)};
        var floats=new[]{new AnimationFloatKey(0,0),new AnimationFloatKey(10,10)};
        var clip=new CompressedKeyframeDescription(30,10,new[]{new CompressedAnimationTransform("root",true,0,8,0,0,1)},
            new[]{new CompressedRotationChannel(254,254,254,keys)},Array.Empty<PackedAnimationQuaternion>(),
            new[]{new AnimationFloatChannel(floats)},new float[]{2,3},Array.Empty<IffNode>());
        var sampler=new CompressedKeyframeSampler(clip);Array.Clear(keys);Array.Clear(floats);
        foreach(double frame in new[]{0d,5,10,2,8,0})
        {
            var sample=sampler.SampleFrame(frame)[0];Vector3 vector=Vector3.Transform(Vector3.UnitX,sample.Rotation);
            var expected=new Vector3((float)Math.Cos(Math.PI*frame/10),(float)Math.Sin(Math.PI*frame/10),0);
            if(Vector3.Distance(vector,expected)>2e-6||Vector3.Distance(sample.Translation,new((float)frame,2,3))>1e-6)throw new Exception("Keyed endpoint/interior/reverse known-vector mismatch.");
        }
        if(Vector3.Distance(sampler.SampleSeconds(1d/6)[0].Translation,new(5,2,3))>1e-6)throw new Exception("Seconds-to-frame mismatch.");
        var sparse=clip with{AnimatedRotations=new[]{new CompressedRotationChannel(254,254,254,new[]{new CompressedRotationKey(2,0),new CompressedRotationKey(8,511)})},AnimatedTranslations=new[]{new AnimationFloatChannel(new[]{new AnimationFloatKey(2,2),new AnimationFloatKey(8,8)})}};
        var held=new CompressedKeyframeSampler(sparse);if(held.SampleFrame(0)[0].Translation.X!=2||held.SampleFrame(10)[0].Translation.X!=8)throw new Exception("Explicit endpoint hold mismatch.");
        void Reject(Action action)=>HostAccountingScenarios.Reject<InvalidDataException>(action);
        Reject(()=>sampler.SampleFrame(double.NaN));Reject(()=>sampler.SampleFrame(-1));Reject(()=>sampler.SampleFrame(11));Reject(()=>sampler.SampleSeconds(double.PositiveInfinity));
        Reject(()=>new CompressedKeyframeSampler(sparse with{Transforms=new[]{sparse.Transforms[0] with{RotationIndex=1}}}));
        Reject(()=>new CompressedKeyframeSampler(sparse with{AnimatedTranslations=new[]{new AnimationFloatChannel(new[]{new AnimationFloatKey(2,0),new AnimationFloatKey(2,1)})}}));
        Reject(()=>new CompressedKeyframeSampler(sparse with{StaticTranslations=new[]{float.NaN,3}}));
        Console.WriteLine("PASS: owned deterministic compressed sampling, known rotation/translation endpoints/interiors, reverse access, seconds conversion, endpoint holds and invalid descriptor/time gates");
    }
    internal static void Probe(CreatureAppearanceDescriptorBundle bundle)
    {
        var clip=CompressedKeyframeDecoder.Create(bundle.SelectedAnimation!.Clip);var sampler=new CompressedKeyframeSampler(clip);
        foreach(double frame in new[]{0d,.5,15.25,30,59.5,90,119.5,120,15.25,0})
        {
            var samples=sampler.SampleFrame(frame);
            for(int transform=0;transform<samples.Count;transform++)
            {
                var s=samples[transform];Console.WriteLine("compressed_sample="+JsonSerializer.Serialize(new{frame,transform,s.Name,rotation=new[]{s.Rotation.X,s.Rotation.Y,s.Rotation.Z,s.Rotation.W},translation=new[]{s.Translation.X,s.Translation.Y,s.Translation.Z},s.RotationRepaired}));
            }
        }
        Console.WriteLine("compressed_sampler="+JsonSerializer.Serialize(new{sampler.FrameRate,sampler.EndFrame,sampler.DurationSeconds,sampler.RepairedPackedValues,sampler.HasUninterpretedSuffix,playback=false}));
    }
}
