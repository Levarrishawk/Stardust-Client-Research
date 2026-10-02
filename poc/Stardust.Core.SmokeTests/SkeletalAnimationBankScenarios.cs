using System.Text;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class SkeletalAnimationBankScenarios
{
    internal static IReadOnlyDictionary<string,LogicalAnimationClipSelection> Profile()
    {using var stream=File.OpenRead("poc/preview-profiles/human-locomotion.json");return SkeletalAnimationBankProfile.Load(stream);}
    internal static void Run()
    {
        string hash=new string('A',64);string valid="{\"version\":1,\"animations\":[{\"key\":\"walk\",\"table\":\"appearance/lat/test.lat\",\"name\":\"loop\",\"sha256\":\""+hash+"\",\"route\":[0,1]}]}";
        IReadOnlyDictionary<string,LogicalAnimationClipSelection> Read(string text){using var stream=new MemoryStream(Encoding.UTF8.GetBytes(text));return SkeletalAnimationBankProfile.Load(stream);}
        var profile=Read(valid);if(profile["walk"].ExplicitChildRoute is not {Count:2})throw new Exception("Owned animation-bank profile mismatch.");
        void Reject(string text)=>HostAccountingScenarios.Reject<InvalidDataException>(()=>Read(text));
        Reject(valid.Replace("\"version\":1","\"version\":2"));Reject(valid.Replace("\"route\":[0,1]","\"route\":[-1]"));Reject(valid.Replace("appearance/lat/test.lat","object/test.iff"));Reject(valid.Replace(hash,"bad"));Reject(valid.Replace("\"route\":[0,1]","\"extra\":[],\"route\":[0,1]"));
        var entry=valid.Substring(valid.IndexOf("{\"key\""),valid.LastIndexOf('}')-valid.IndexOf("{\"key\"")-1);Reject("{\"version\":1,\"animations\":["+entry+","+entry+"]}");
        int lookups=0;HostAccountingScenarios.Reject<ArgumentException>(()=>CreatureAppearanceDescriptorBundleReader.Load(_=>{lookups++;return new(TreLookupKind.Missing);},"object/test.iff",hash,hash,additionalAnimationSelections:profile));
        if(lookups!=0)throw new Exception("Invalid animation-bank option reached lookup.");
        Console.WriteLine("PASS: explicit owned animation-bank profile and version/path/pin/key/route/extra-field/option gates");
    }
    internal static void Probe(CreatureAppearanceDescriptorBundle bundle,Func<string,TreLookupResult> read,string objectPath,string objectPin,string appearancePin)
    {
        var bank=bundle.AdditionalAnimations??throw new Exception("Original movement bank absent.");var binding=SkeletalJointBindingBuilder.Create(bundle.Appearance,bundle.Skeletons!,bundle.Appearance.Skeletons.ToDictionary(s=>s.Path,_=>0));
        if(bank.Count!=2||bundle.PreparedAnimation is null)throw new Exception("Original movement bank/idle mismatch.");
        foreach(var pair in bank)
        {
            var prepared=pair.Value;var motion=prepared.Locomotion!;var clip=CompressedKeyframeDecoder.Decode(read(prepared.ClipPath).Payload!);var direct=new CompressedKeyframeSampler(clip);var skeleton=bundle.Skeletons![prepared.SkeletonPath].ExposedLevels()[0];
            if(!motion.HasTranslation||prepared.MissingSkeletonTransforms.Count!=1||prepared.MissingSkeletonTransforms[0]!="hold_l"||!prepared.UnusedAnimationTransforms.SequenceEqual(new[]{"polearm1_polearm","l0"}))throw new Exception("Original movement binding diagnostic changed: missing="+string.Join(",",prepared.MissingSkeletonTransforms)+" unused="+string.Join(",",prepared.UnusedAnimationTransforms));
            for(int i=0;i<=prepared.EndFrame*4;i++)
            {
                double frame=i*.25;var pose=prepared.CreateLocalPose(binding,frame);var independent=SkeletonAnimationPoseBuilder.Apply(binding,prepared.SkeletonPath,skeleton,SkeletonAnimationPoseBuilder.Create(skeleton,direct.SampleFrame(frame),MissingAnimationTransformPolicy.BindPose));
                if(!pose.SequenceEqual(independent)||pose.Count!=binding.Joints.Count)throw new Exception("Prepared movement bank pose mismatch.");
            }
            Console.WriteLine("skeletal_locomotion_bank="+JsonSerializer.Serialize(new{key=pair.Key,table=Profile()[pair.Key].TablePath,name=Profile()[pair.Key].LogicalName,route=Profile()[pair.Key].ExplicitChildRoute,path=prepared.ClipPath,sha256=prepared.ClipSha256,prepared.FrameRate,prepared.EndFrame,motion.AuthoredSpeed,keys=motion.Positions.Count,messages=motion.Messages,prepared.UnusedAnimationTransforms,prepared.MissingSkeletonTransforms,poses=prepared.EndFrame*4+1,joints=binding.Joints.Count,root_motion_applied=false,event_dispatch=false}));
            foreach(var kind in new[]{TreLookupKind.Missing,TreLookupKind.Masked})HostAccountingScenarios.Reject<InvalidDataException>(()=>CreatureAppearanceDescriptorBundleReader.Load(path=>path==prepared.ClipPath?new(kind):read(path),objectPath,objectPin,appearancePin,loadSkeletons:true,loadLogicalTables:true,animationSelection:LogicalAnimationProxyScenarios.IdleSelection,prepareSelectedAnimation:true,additionalAnimationSelections:Profile()));
        }
        var wrong=Profile().ToDictionary(p=>p.Key,p=>p.Value with{ClipSha256=new string('0',64)});HostAccountingScenarios.Reject<InvalidDataException>(()=>CreatureAppearanceDescriptorBundleReader.Load(read,objectPath,objectPin,appearancePin,loadSkeletons:true,loadLogicalTables:true,animationSelection:LogicalAnimationProxyScenarios.IdleSelection,prepareSelectedAnimation:true,additionalAnimationSelections:wrong));
        Console.WriteLine("PASS: original explicit idle/walk/run bank,222 retained prepared poses, missing/masked/pin gates; root translation and callbacks remain separate");
    }
}
