using System.Buffers.Binary;
using System.Text.Json;
using Stardust.Core.Protocol;
namespace Stardust.Core.SmokeTests;
public static class CreatureView3CompositeDeltaScenarios
{
    public const ulong Id=0xF122334455667788;private const uint C=0xF1234567;
    public static CreatureView3CompositeDeltaLimits Limits=>new(4096,32,24,64,64,16,64,64,16);
    private static byte[] Golden(CreatureWoundsApplicationPolicy policy)
    {
        var assign=CreatureView3WoundsDeltaScenarios.Field(policy==CreatureWoundsApplicationPolicy.LegacyBounded?5U:2U,CreatureView3WoundsDeltaScenarios.Command(3,0,-1,0,7));
        if(policy==CreatureWoundsApplicationPolicy.LegacyBounded)BinaryPrimitives.WriteUInt32LittleEndian(assign.AsSpan(2),4);
        byte[] fields=[..CreatureView3MixedDeltaScenarios.Scalar(11,255),..CreatureView3ComponentsDeltaScenarios.Field(C+2,CreatureView3ComponentsDeltaScenarios.Command(1,17),CreatureView3ComponentsDeltaScenarios.Command(0,-1)),
            ..CreatureView3WoundsDeltaScenarios.Field(1,CreatureView3WoundsDeltaScenarios.Command(2,0,int.MinValue)),..CreatureView3MixedDeltaScenarios.Scalar(14,0,0,0,128),
            ..CreatureView3ComponentsDeltaScenarios.Field(C+9,CreatureView3ComponentsDeltaScenarios.Command(1,int.MaxValue)),..assign,..CreatureView3MixedDeltaScenarios.Scalar(16,135,150,165,180,195,210,225,240),
            ..CreatureView3MixedDeltaScenarios.Scalar(11,9),..CreatureView3ComponentsDeltaScenarios.Field(C+10,CreatureView3ComponentsDeltaScenarios.Command(2)),
            ..CreatureView3WoundsDeltaScenarios.Field(policy==CreatureWoundsApplicationPolicy.LegacyBounded?6U:3U,CreatureView3WoundsDeltaScenarios.Command(0,1)),
            ..CreatureView3ComponentsDeltaScenarios.Field(C+12,CreatureView3ComponentsDeltaScenarios.Command(1,int.MinValue),CreatureView3ComponentsDeltaScenarios.Command(1,int.MaxValue)),
            ..CreatureView3MixedDeltaScenarios.Scalar(12,254),..CreatureView3MixedDeltaScenarios.Scalar(13,8,7,6,5,4,3,2,254),..CreatureView3MixedDeltaScenarios.Scalar(15,68,51,34,241)];
        return CreatureView3ComponentsDeltaScenarios.Message(fields);
    }
    private static void Check(bool ok,string name){if(!ok)throw new Exception(name);}
    private static void Reject(Action a){try{a();}catch(InvalidDataException){return;}throw new Exception("Expected composite rejection.");}
    private static object Export(CreatureView3CompositeState s)=>new{s.ObjectId,s.Scalars.Posture,s.Scalars.Rank,s.Scalars.LinkId,HeightBits=(uint)BitConverter.SingleToInt32Bits(s.Scalars.Height),s.Scalars.Shock,s.Scalars.States,WoundsCounter=s.Wounds.Counter,Wounds=s.Wounds.Values,ComponentsCounter=s.Components.Counter,Components=s.Components.Keys,RawCounter=s.RawComponents.Counter,RawComponents=s.RawComponents.RawKeys};
    public static void Run(string peerPath,string? exportDirectory=null)
    {
        var peer=JsonSerializer.Deserialize<Peer>(File.ReadAllText(peerPath))!;byte[] bytes=Convert.FromHexString(peer.BaselineHex);
        var baseline=CreatureView3BaselineCodec.Decode(bytes,new(bytes.Length-23,3,3,3,119));
        var seed=CreatureView3CompositeState.FromBaseline(baseline,3,16,16);Array.Clear(bytes);
        Check(seed.RawComponents.RawCount==3 && seed.RawComponents.RawKeys.SequenceEqual(new[]{-1,int.MinValue,-1}) && seed.Components.Keys.SequenceEqual(new[]{int.MinValue,-1}),"Raw/normalized seed separation.");
        foreach(var policy in new[]{CreatureWoundsApplicationPolicy.StrictInterval,CreatureWoundsApplicationPolicy.LegacyBounded})
        {
            byte[] wire=Golden(policy);var plan=CreatureView3CompositeDeltaCodec.Decode(wire,Limits,policy);var final=plan.Apply(seed);
            Check(plan.Updates.Select(x=>x.Index).SequenceEqual(new ushort[]{11,5,17,14,5,17,16,11,5,17,5,12,13,15}),"Interleaved order.");
            Check(final.Scalars.Posture==9 && BitConverter.SingleToInt32Bits(final.Scalars.Height)==int.MinValue && final.Components.Counter==C+12 && final.Components.Keys.SequenceEqual(new[]{int.MinValue,int.MaxValue}) && final.Wounds.Counter==(policy==CreatureWoundsApplicationPolicy.LegacyBounded?6U:3U) && final.Wounds.Values.SequenceEqual(new[]{-1,7}),"Composite kernel result.");
            Check(ReferenceEquals(final.RawComponents,seed.RawComponents),"Raw baseline retained independently.");
            Check(CreatureView3CompositeDeltaCodec.Encode(plan,Limits).SequenceEqual(wire),"Exact composite encode.");
            foreach(var cap in new[]{Limits with{MaximumPayloadBytes=wire.Length-24},Limits with{MaximumFields=13},Limits with{MaximumScalarUpdates=6},Limits with{MaximumWoundsCommands=policy==CreatureWoundsApplicationPolicy.LegacyBounded?5:2},Limits with{MaximumWoundsValues=3},Limits with{MaximumComponentsCommands=5},Limits with{MaximumComponentsKeys=4}})
            {Reject(()=>CreatureView3CompositeDeltaCodec.Decode(wire,cap,policy));Reject(()=>CreatureView3CompositeDeltaCodec.Encode(plan,cap));}
            Reject(()=>CreatureView3CompositeDeltaCodec.Decode(wire,Limits with{MaximumWoundsElements=2},policy).Apply(seed));
            Reject(()=>CreatureView3CompositeDeltaCodec.Decode(wire,Limits with{MaximumComponentsElements=2},policy).Apply(seed));
            var exact=Limits with{MaximumPayloadBytes=wire.Length-23,MaximumFields=14,MaximumScalarUpdates=7,MaximumWoundsCommands=policy==CreatureWoundsApplicationPolicy.LegacyBounded?6:3,MaximumWoundsValues=4,MaximumComponentsCommands=6,MaximumComponentsKeys=5,MaximumWoundsElements=3,MaximumComponentsElements=3};
            Check(CreatureView3CompositeDeltaCodec.Decode(wire,exact,policy).Apply(seed).Components.Keys.Count==2,"Exact independent budgets.");
            for(int n=0;n<wire.Length;n++)Reject(()=>CreatureView3CompositeDeltaCodec.Decode(wire[..n],Limits,policy));
            byte[] borrowed=(byte[])wire.Clone();var owned=CreatureView3CompositeDeltaCodec.Decode(borrowed,Limits,policy);Array.Clear(borrowed);Check(CreatureView3CompositeDeltaCodec.Encode(owned,Limits).SequenceEqual(wire),"Owned composite plan.");
            try{((IList<CreatureView3CompositeUpdate>)plan.Updates).Clear();throw new Exception("Mutable updates.");}catch(NotSupportedException){}
            try{((IList<CreatureComponentsCommand>)plan.Updates[1].Components!.Commands)[0]=new(CreatureComponentsCommandKind.Clear,0);throw new Exception("Mutable components.");}catch(NotSupportedException){}
        }
        foreach(ushort index in new ushort[]{0,1,2,3,4,6,7,8,9,10,18,65535})Reject(()=>CreatureView3CompositeDeltaCodec.Decode(CreatureView3ComponentsDeltaScenarios.Message(CreatureView3MixedDeltaScenarios.Scalar(index)),Limits));
        foreach(int offset in new[]{0,2,14,18,19}){byte[] bad=Golden(CreatureWoundsApplicationPolicy.StrictInterval);bad[offset]^=1;Reject(()=>CreatureView3CompositeDeltaCodec.Decode(bad,Limits));}
        Reject(()=>CreatureView3CompositeState.FromBaseline(baseline,2,16,16));Reject(()=>CreatureView3CompositeState.FromBaseline(baseline,3,1,16));Reject(()=>CreatureView3CompositeState.FromBaseline(baseline,3,16,2));
        var objectMismatch=new CreatureView3CompositeState(0,seed.Scalars,new(0,0,Array.Empty<int>(),16),new(0,0,Array.Empty<int>(),16),CreatureView3ComponentsDeltaCodec.DecodeSnapshot(0,new byte[8],0));
        Reject(()=>CreatureView3CompositeDeltaCodec.Decode(Golden(CreatureWoundsApplicationPolicy.StrictInterval),Limits).Apply(objectMismatch));
        var empty=CreatureView3CompositeDeltaCodec.Decode(CreatureView3ComponentsDeltaScenarios.Message(Array.Empty<byte>(),65535),Limits with{MaximumFields=0,MaximumScalarUpdates=0,MaximumWoundsCommands=0,MaximumWoundsValues=0,MaximumComponentsCommands=0,MaximumComponentsKeys=0});Check(empty.Apply(seed).RawComponents.RawCount==3,"Empty composite plan.");
        if(exportDirectory is not null){Directory.CreateDirectory(exportDirectory);File.WriteAllText(Path.Combine(exportDirectory,"seed.json"),JsonSerializer.Serialize(Export(seed)));}
        List<object> results=new();
        foreach(var spec in peer.Cases)
        {
            var limits=Limits with{MaximumComponentsElements=spec.ComponentsElements??16,MaximumComponentsKeys=spec.ComponentsKeys??64,MaximumWoundsValues=spec.WoundsValues??64};
            var initial=new CreatureView3CompositeState(seed.ObjectId,seed.Scalars,new(seed.ObjectId,spec.WoundsCounter??0,seed.Wounds.Values.ToArray(),16),new(seed.ObjectId,spec.ComponentsCounter??C,seed.Components.Keys.ToArray(),16),seed.RawComponents);
            bool decoded=false;
            try
            {
                byte[] wire=Convert.FromHexString(spec.WireHex);var plan=CreatureView3CompositeDeltaCodec.Decode(wire,limits,(CreatureWoundsApplicationPolicy)spec.Policy);decoded=true;
                if(spec.Name is "strict" or "legacy")Check(wire.SequenceEqual(Golden((CreatureWoundsApplicationPolicy)spec.Policy)),"Independent/native hand-pack agreement.");
                if(exportDirectory is not null)File.WriteAllBytes(Path.Combine(exportDirectory,spec.Name+".bin"),CreatureView3CompositeDeltaCodec.Encode(plan,limits));
                var final=plan.Apply(initial);results.Add(new{spec.Name,Decoded=true,Accepted=true,State=Export(final)});
            }
            catch(InvalidDataException){results.Add(new{spec.Name,Decoded=decoded,Accepted=false,State=Export(initial)});}
            Check(initial.Components.Counter==(spec.ComponentsCounter??C) && initial.Wounds.Counter==(spec.WoundsCounter??0) && initial.RawComponents.RawKeys.SequenceEqual(new[]{-1,int.MinValue,-1}) && initial.Components.Keys.SequenceEqual(new[]{int.MinValue,-1}) && initial.Wounds.Values.SequenceEqual(new[]{-2,int.MaxValue,-2}),"Atomic initial state.");
        }
        if(exportDirectory is not null)File.WriteAllText(Path.Combine(exportDirectory,"results.json"),JsonSerializer.Serialize(results));
        Console.WriteLine("Composite ordering, distinct kernels, all budgets, ownership and late failures passed.");
    }
    public sealed record Peer(string BaselineHex,Case[] Cases);
    public sealed record Case(string Name,string WireHex,int Policy,int? ComponentsElements=null,int? ComponentsKeys=null,int? WoundsValues=null,uint? ComponentsCounter=null,uint? WoundsCounter=null);
}
