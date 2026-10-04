using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

// A synthetic literal server-shaped observer update. This is a counterexample
// for the accepted R43 opt-in full-state policy, not a recovered packet capture.
static class ObserverBeforeEndProbe
{
    const ulong Id=0xDABC001122334455;
    static int checks;
    static void Check(bool value,string why){checks++;if(!value)throw new Exception(why);}
    static string Reject(Action action){try{action();}catch(InvalidDataException e){checks++;return e.Message;}throw new Exception("Expected compatibility gap rejection");}
    static readonly string path="object/synthetic_round44.iff",pin=new('0',64);
    static readonly uint crc=LegacyAssetPath.Checksum(path);
    static SceneWorldCoordinator World(bool typed)
    {
        var catalog=new ObjectTemplateCatalog(new[]{new ObjectTemplateCatalogEntry(crc,path,pin,pin,SceneObjectResourceRecipe.CreatureDetail0)});
        var idle=new LogicalAnimationClipSelection("appearance/lat/synthetic.lat","synthetic_idle",pin);
        var movement=new Dictionary<string,LogicalAnimationClipSelection>{{"walk",idle},{"run",idle}};
        return new(catalog,new Dictionary<SceneObjectResourceRecipe,SceneObjectResourceOptions>{{SceneObjectResourceRecipe.CreatureDetail0,new SceneCreatureResourceOptions(idle,movement,false)}},(_,_)=>throw new Exception("Probe must never load assets"),SceneCreateZeroQuaternionPolicy.Reject,8,2,16,4096,16,4096,1024,creatureView4FullPolicy:typed?new(new(MaximumPayloadBytes:1024),new(MaximumPayloadBytes:1024),new(),new()):null);
    }
    static byte[] Start()
    {
        using var stream=new MemoryStream();using var writer=new BinaryWriter(stream);
        writer.Write((ushort)9);writer.Write(ZoneSceneProtocolCodec.CmdStartSceneOpcode);writer.Write((byte)0);writer.Write(Id);writer.Write((ushort)0);writer.Write(0f);writer.Write(0f);writer.Write(0f);writer.Write((ushort)0);writer.Write(0UL);return stream.ToArray();
    }
    static long Begin(SceneWorldCoordinator world)
    {
        long generation=world.BeginSelection(Id).Generation;
        Check(world.Accept(generation,Start()),"start");
        Check(world.Accept(generation,SceneObjectLifecycleCodec.EncodeCreate(new(Id,0,0,0,1,0,0,0,crc,0))),"create staged");
        return generation;
    }
    public static void Main(string[] args)
    {
        byte[] wire=Convert.FromHexString("050053218612554433221100BCDA4F4552430414000000030001000000803F0500000000400A0000004040");
        var decoded=CreatureView4Codec.DecodeDelta(wire,new());
        Check(decoded.ObjectId==Id&&decoded.DeclaredCount==3&&decoded.Updates.Count==3,"literal envelope");
        Check(decoded.Updates.Select(x=>x.Index).SequenceEqual(new ushort[]{1,5,10}),"observer field order");
        Check(decoded.Updates.Select(x=>x.Bits).SequenceEqual(new ulong[]{0x3F800000,0x40000000,0x40400000}),"literal raw values");
        string activeError,stagedError;
        using(var world=World(true))
        {
            long generation=Begin(world);Check(world.CompleteMinimumSceneBarrier(generation,true) is not null,"publish created-only scene");
            var before=world.Snapshots().Single();Check(before.Object.Phase==SceneObjectPhase.PendingBaselines,"before end");
            activeError=Reject(()=>world.Accept(generation,wire));
            Check(world.State==ZoneSceneTransitionState.AwaitingServerReady&&world.Snapshots().Single().Object.Incarnation==before.Object.Incarnation,"active typed rejection retains incarnation");
            Check(world.FindCreatureView4Full(generation,Id) is null&&world.RetainedCreatureView4FullBytes==0,"no fabricated private baseline");
            Check(world.Accept(generation,SceneObjectLifecycleCodec.EncodeEndBaselines(new(Id))),"ordinary end remains possible");
            Check(world.Snapshots().Single().Object.Phase==SceneObjectPhase.Finalized,"unseeded finalization remains optional");
        }
        using(var world=World(true))
        {
            long generation=Begin(world);Check(world.Accept(generation,wire),"observer staged");
            stagedError=Reject(()=>world.CompleteMinimumSceneBarrier(generation,true));
            Check(world.State==ZoneSceneTransitionState.Failed&&world.Snapshots().Count==0&&world.RetainedCreatureView4FullBytes==0,"failed staged replay clears all state");
        }
        using(var world=World(false))
        {
            long generation=Begin(world);Check(world.Accept(generation,wire),"opaque observer staged");
            Check(world.Accept(generation,SceneObjectLifecycleCodec.EncodeEndBaselines(new(Id))),"opaque end staged");
            Check(world.CompleteMinimumSceneBarrier(generation,true) is not null,"opaque sequence publishes");
            Check(world.Snapshots().Single().Object.Phase==SceneObjectPhase.Finalized&&world.Snapshots().Single().Envelopes.Count==1,"opaque retains wire without scalar semantics");
        }
        int counterexampleChecks=checks;
        object Row(CreatureView4PartialState state)=>new{state.ObjectId,state.Revision,state.KnownMask,state.KnownCount,state.DecodedBytes,Fields=state.KnownFields.Select(f=>new{f.Index,f.Bits,f.StateRevision,f.Provenance}).ToArray()};
        var empty=CreatureView4PartialState.Empty(Id);
        Check(empty.KnownCount==0&&!empty.TryGetField(0,out var absent)&&absent is null,"unknown is not zero");
        var first=empty.Apply(decoded);
        Check(first.Current.KnownMask==1058&&first.Current.KnownCount==3&&empty.KnownCount==0,"observer known-mask and immutable empty");
        Check(first.Events.Count==3&&first.Events.All(e=>!e.OldWasKnown&&e.OldBits is null&&e.OldProvenance is null),"first assignment has no fabricated old data");
        foreach(ushort field in new ushort[]{0,1,4,5,6,7,8,9,10,11,12})
            Check(first.Current.IsKnown(field)==(field is 1 or 5 or 10),"only delivered scalars are known");
        Check(first.Current.TryGetField(1,out var originalReceipt)&&first.Current.IsCurrent(originalReceipt!),"first receipt identity");
        var repeat=first.Current.Apply(decoded);
        Check(repeat.Events.All(e=>e.OldWasKnown&&e.OldBits==e.NewBits&&e.Provenance.Revision==2),"equal assignments retain ordered provenance");
        Check(!repeat.Current.IsCurrent(originalReceipt!)&&first.Current.IsCurrent(originalReceipt!),"receipt belongs to exact immutable branch");
        byte[] signedZero=Convert.FromHexString("050053218612554433221100BCDA4F45524304080000000100050000000080");
        var third=repeat.Current.Apply(CreatureView4Codec.DecodeDelta(signedZero,new()));
        Check(third.Current.TryGetField(5,out var zero)&&zero!.Bits==0x80000000&&zero.Provenance.Revision==3,"received signed zero is known");
        Check(third.Current.TryGetField(1,out var unchanged)&&unchanged!.Provenance.Revision==2&&unchanged.StateRevision==3,"source provenance differs from containing state revision");
        Check(!third.Current.TryGetField(6,out var listen)&&listen is null,"unreceived full-width ID remains unknown");
        File.WriteAllText(args[0],JsonSerializer.Serialize(new{Checks=checks,CounterexampleChecks=counterexampleChecks,Literal=Convert.ToHexString(wire),SignedZeroLiteral=Convert.ToHexString(signedZero),ActiveError=activeError,StagedError=stagedError,PartialStateRequired=true,Partial=new{Empty=Row(empty),First=Row(first.Current),Repeat=Row(repeat.Current),Third=Row(third.Current),FirstEvents=first.Events,RepeatEvents=repeat.Events,ThirdEvents=third.Events}},new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine($"R44_OBSERVER_PRE_END_COUNTEREXAMPLE_PASS checks={checks}");
    }
}
