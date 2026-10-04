using System.Numerics;
using System.Reflection;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

namespace Stardust.Core.SmokeTests;
public static class SceneWorldExplicitMovementAdmissionScenarios
{
    private static int checks;
    private static void Check(bool value,string why) { checks++; if(!value) throw new InvalidDataException(why); }
    private static void Reject(Action action)
    { bool failed=false;try{action();}catch(Exception e)when(e is InvalidDataException or InvalidOperationException or ArgumentException or ObjectDisposedException){failed=true;}Check(failed,"expected rejection"); }
    private static readonly string path="object/root.iff",pin=new('0',64);
    private static readonly uint crc=LegacyAssetPath.Checksum(path);
    private static readonly LogicalAnimationClipSelection idle=new("appearance/lat/synthetic.lat","synthetic_idle",pin);
    private static readonly IReadOnlyDictionary<string,LogicalAnimationClipSelection> movement=new Dictionary<string,LogicalAnimationClipSelection>{{"walk",idle},{"run",idle}};
    private static SceneWorldCoordinator World(string rootPin)=>new(
        new ObjectTemplateCatalog(new[]{new ObjectTemplateCatalogEntry(crc,path,rootPin,pin,SceneObjectResourceRecipe.CreatureDetail0)}),
        new Dictionary<SceneObjectResourceRecipe,SceneObjectResourceOptions>{{SceneObjectResourceRecipe.CreatureDetail0,new SceneCreatureResourceOptions(idle,movement,false)}},
        (_,_)=>throw new InvalidDataException("No synthetic resource loads"),SceneCreateZeroQuaternionPolicy.Reject,4,2,32,4194304,32,4194304,1048576,
        replicationPolicy:new(SceneWorldReplicationMode.CreatureView3MixedComponents,new(1024,64,64,16,512),16,1024,new(1024,32,24,64,64,16),CreatureWoundsApplicationPolicy.LegacyBounded,new(1024,32,64,64,16),new(16,16)),
        creatureView4PartialPolicy:new(new(MessageLimits:new(MaximumPayloadBytes:1048576)),new()));
    private static byte[] Start(){using var s=new MemoryStream();using var w=new BinaryWriter(s);w.Write((ushort)9);w.Write(ZoneSceneProtocolCodec.CmdStartSceneOpcode);w.Write((byte)0);w.Write(42UL);w.Write((ushort)0);w.Write(0f);w.Write(0f);w.Write(0f);w.Write((ushort)0);w.Write(0UL);return s.ToArray();}
    private static byte[] Create()=>SceneObjectLifecycleCodec.EncodeCreate(new(42,0,0,0,1,0,0,0,crc,0));
    private static long Stage(SceneWorldCoordinator w,byte[] baseline,bool finalized=true)
    {long g=w.BeginSelection(42).Generation;Check(w.Accept(g,Start()),"start");Check(w.Accept(g,Create()),"create");Check(w.Accept(g,baseline),"typed3");if(finalized)Check(w.Accept(g,SceneObjectLifecycleCodec.EncodeEndBaselines(new(42))),"end");Check(w.CompleteMinimumSceneBarrier(g,true)is not null,"barrier");return g;}
    private static SceneWorldObjectSnapshot Target(SceneWorldCoordinator w)=>w.Snapshots().Single(s=>s.Object.Handle.ObjectId==42);
    private static SharedCreatureAuthoredMovementInputs Profile(JsonElement p)
    {
        byte[] raw=Convert.FromHexString(p.GetProperty("Hex").GetString()!);string sha=p.GetProperty("RootSha256").GetString()!;
        var profile=SharedCreatureMovementInputProfile.Load(q=>q==path?new(TreLookupKind.Present,raw,77):new(TreLookupKind.Missing),path,sha);
        return SharedCreatureAuthoredMovementInputs.FromProfile(profile);
    }
    public static void Run(string fixture,string output)
    {
        using var json=JsonDocument.Parse(File.ReadAllText(fixture));byte[] baseline=Convert.FromHexString(json.RootElement.GetProperty("Baseline").GetString()!);
        var profiles=json.RootElement.GetProperty("Profiles").EnumerateArray().Select(Profile).ToArray();var rows=new List<object>();
        foreach(var c in json.RootElement.GetProperty("Cases").EnumerateArray())
        {
            var p=profiles[c.GetProperty("Profile").GetInt32()];string sha=p.Sources[path].Sha256;
            using var world=World(sha);long g=Stage(world,baseline);using var motions=new SceneWorldObjectMotionStore(world);
            using var reader=new SceneWorldReceivedCreatureMovementInputs(world);Check(world.Accept(g,Convert.FromHexString(c.GetProperty("Wire").GetString()!)),"received wire");
            var received=reader.Read(g,42)!;var source=(SceneWorldMovementSpeedSource)c.GetProperty("Source").GetInt32();
            using var adapter=new SceneWorldExplicitMovementAdmission(world,motions,42,source,p,sha,reader,0,10000);
            var before=adapter.State;var publication=motions.Find(Target(world));var d=c.GetProperty("Direction").EnumerateArray().Select(x=>x.GetSingle()).ToArray();
            var r=adapter.Submit(0,new(d[0],d[1]),(int)source>=3?received:null);
            Check((r is not null)==c.GetProperty("ExpectedEligible").GetBoolean(),"independent eligibility "+c.GetRawText()+" got="+(r is not null));
            if(r is null)Check(adapter.State==before&&ReferenceEquals(publication,motions.Find(Target(world))),"ineligible admission no driver/publication mutation");
            else
            {
                Check(r.RawSpeedBits==c.GetProperty("Bits").GetUInt32()&&r.Incarnation==Target(world).Object.Incarnation&&r.RootSha256==sha,"captured identity/bits");
                Check(adapter.IsSourceCurrent(r),"exact source current");Check((r.AuthoredSelection is not null)==((int)source<=2),"explicit source provenance branch");
            }
            var state=adapter.Advance(40000);Check(state.Active&&state.FaultType is null&&state.StepsThisUpdate==4,"real driver four native ticks");
            rows.Add(new{Eligible=r is not null,RawBits=r?.RawSpeedBits,Velocity=r is null?null:new[]{r.WorldVelocity.X,r.WorldVelocity.Y,r.WorldVelocity.Z},Position=new[]{state.Transform.Translation.X,state.Transform.Translation.Y,state.Transform.Translation.Z},SourceCurrent=r is not null&&adapter.IsSourceCurrent(r)});
        }
        Negatives(profiles[0],baseline,Convert.FromHexString(json.RootElement.GetProperty("Profiles")[0].GetProperty("Hex").GetString()!));File.WriteAllText(output,JsonSerializer.Serialize(new{Cases=rows,Checks=checks}));Console.WriteLine($"R48_EXPLICIT_MOVEMENT_PASS cases={rows.Count} checks={checks}");
    }
    private static void Negatives(SharedCreatureAuthoredMovementInputs p,byte[] baseline,byte[] rootBytes)
    {
        string sha=p.Sources[path].Sha256;
        using var w=World(sha);long g=Stage(w,baseline);using var motions=new SceneWorldObjectMotionStore(w);using var reader=new SceneWorldReceivedCreatureMovementInputs(w);
        Check(w.ApplyCreatureView4PartialDelta(g,null,new(42,0,[new(7,BitConverter.SingleToUInt32Bits(3)),new(11,BitConverter.SingleToUInt32Bits(8))])),"initial source");
        var received=reader.Read(g,42)!;
        using var adapter=new SceneWorldExplicitMovementAdmission(w,motions,42,SceneWorldMovementSpeedSource.ReceivedField7,p,sha,reader,0,10000);
        var r=adapter.Submit(20000,Vector2.UnitX,received)!;Check(r.Speed==3&&r.ReceivedSource==received,"selected7 only captured");
        Check(w.ApplyCreatureView4PartialDelta(g,received.Source,new(42,0,[new(7,BitConverter.SingleToUInt32Bits(9))])),"later source");
        using(var eleven=new SceneWorldExplicitMovementAdmission(w,motions,42,SceneWorldMovementSpeedSource.ReceivedField11,p,sha,reader,0,10000))Check(eleven.Submit(0,Vector2.UnitX,reader.Read(g,42))!.Speed==8,"selected11 distinct from field7");
        var before=adapter.State;var pub=motions.Find(Target(w));Check(!adapter.IsSourceCurrent(r)&&adapter.Submit(20000,Vector2.UnitX,received)is null,"stale source rejects");
        Check(adapter.State==before&&ReferenceEquals(pub,motions.Find(Target(w))),"stale receipt no driver fault/history/publication mutation");
        Check(Math.Abs(adapter.Advance(30000).Transform.Translation.X-.03f)<1e-7,"future queued speed captured despite later source9");
        adapter.Reset(0);Check(adapter.Advance(30000).Transform.Translation==Vector3.Zero,"reset clears queued captured input");
        var fresh=reader.Read(g,42)!;adapter.Reset(0);var first=adapter.Submit(0,Vector2.UnitY,fresh)!;Check(first.Speed==9,"fresh source new admission");
        foreach(var d in new[]{Vector2.Zero,new Vector2(1,1),new Vector2(float.NaN,0),new Vector2(float.PositiveInfinity,0)})Check(adapter.Submit(0,d,fresh)is null,"direction rejects before history");
        before=adapter.State;pub=motions.Find(Target(w));Reject(()=>adapter.Submit(-1,Vector2.UnitX,fresh));Reject(()=>adapter.Submit(60000001,Vector2.UnitX,fresh));
        adapter.Submit(20000,Vector2.UnitX,fresh);Reject(()=>adapter.Submit(19999,Vector2.UnitX,fresh));
        Check(adapter.State==before&&ReferenceEquals(pub,motions.Find(Target(w))),"time/order rejection no driver/publication mutation");
        adapter.Reset(0);for(int i=0;i<1024;i++)Check(adapter.Submit(0,Vector2.UnitX,fresh)is not null,"bounded pending admission");
        Reject(()=>adapter.Submit(0,Vector2.UnitY,fresh));Check(Math.Abs(adapter.Advance(10000).Transform.Translation.X-.09f)<1e-7,"capacity rejection retains admitted input");
        var lateState=adapter.State;Reject(()=>adapter.Submit(9999,Vector2.UnitX,fresh));Check(adapter.State==lateState,"late admission preserves clock/publication");
        adapter.Reset(0);Check(adapter.Submit(0,Vector2.UnitY,fresh)is not null,"capacity frees on reset");
        Task.Run(()=>Reject(()=>adapter.Submit(0,Vector2.UnitX,fresh))).GetAwaiter().GetResult();Task.Run(()=>Reject(()=>adapter.Dispose())).GetAwaiter().GetResult();
        var busy=typeof(SceneWorldExplicitMovementAdmission).GetField("busy",BindingFlags.Instance|BindingFlags.NonPublic)!;busy.SetValue(adapter,true);Reject(()=>adapter.Submit(0,Vector2.UnitX,fresh));busy.SetValue(adapter,false);
        Check(adapter.IsSourceCurrent(first),"owner/reentry rejection preserves current source");
        using(var other=World(sha))
        {
            long og=Stage(other,baseline);using var or=new SceneWorldReceivedCreatureMovementInputs(other);other.ApplyCreatureView4PartialDelta(og,null,new(42,0,[new(7,0)]));
            Check(adapter.Submit(0,Vector2.UnitX,or.Read(og,42))is null,"foreign world/reader source");
        }
        using(var another=new SceneWorldReceivedCreatureMovementInputs(w))Check(adapter.Submit(0,Vector2.UnitX,another.Read(g,42))is null,"foreign reader same world");
        Check(adapter.Submit(0,Vector2.UnitX)is null,"no received fallback to authored");
        var current=reader.Read(g,42)!;w.Accept(g,SceneObjectLifecycleCodec.EncodeContainment(new(42,0,1)));before=adapter.State;
        Check(adapter.Submit(0,Vector2.UnitX,current)is null&&adapter.State==before,"registry context changed no admission mutation");
        var retained=r.WorldVelocity;reader.Dispose();before=adapter.State;Check(adapter.Submit(0,Vector2.UnitX,fresh)is null&&adapter.State==before,"closed borrowed reader rejects before driver mutation");adapter.Dispose();adapter.Dispose();Check(!adapter.IsSourceCurrent(r)&&r.WorldVelocity==retained&&w.State!=ZoneSceneTransitionState.Closed,"historical receipt and borrowed world survive dispose");Reject(()=>adapter.Submit(0,Vector2.UnitX));
        using(var typed=World(sha))
        {
            long tg=Stage(typed,baseline);using var store=new SceneWorldObjectMotionStore(typed);using var tr=new SceneWorldReceivedCreatureMovementInputs(typed);
            typed.ApplyCreatureView4PartialDelta(tg,null,new(42,0,[new(7,BitConverter.SingleToUInt32Bits(3))]));var partial=tr.Read(tg,42)!;
            using var a=new SceneWorldExplicitMovementAdmission(typed,store,42,SceneWorldMovementSpeedSource.ReceivedField7,p,sha,tr,0,10000);
            var captured=a.Submit(20000,Vector2.UnitX,partial)!;Check(typed.Accept(tg,baseline),"replacement typed3 publication");
            Check(!a.IsSourceCurrent(captured)&&tr.IsCurrent(partial),"typed3 currency separate from current partial receipt");
            Check(Math.Abs(a.Advance(30000).Transform.Translation.X-.03f)<1e-7,"typed3 change does not rewrite captured queue");
            Check(a.Submit(30000,Vector2.UnitY,partial)is not null,"current partial can capture fresh typed3 context explicitly");
        }
        foreach(string change in new[]{"recreate","generation","close"})
        using(var world=World(sha))
        {
            long gen=Stage(world,baseline);using var store=new SceneWorldObjectMotionStore(world);using var a=new SceneWorldExplicitMovementAdmission(world,store,42,SceneWorldMovementSpeedSource.AuthoredSpeed0,p,sha,initialTimestampMicroseconds:0,stepMicroseconds:10000);
            var receipt=a.Submit(0,Vector2.UnitX)!;var saved=a.State;
            if(change=="recreate"){world.Accept(gen,SceneObjectLifecycleCodec.EncodeDestroy(new(42,0)));world.Accept(gen,Create());world.Accept(gen,baseline);world.Accept(gen,SceneObjectLifecycleCodec.EncodeEndBaselines(new(42)));}
            else if(change=="generation")Stage(world,baseline);else world.Close();
            Check(!a.IsSourceCurrent(receipt)&&a.Submit(0,Vector2.UnitX)is null&&a.State==saved,"old incarnation rejected without driver mutation "+change);
        }
        using(var world=World(sha))
        {
            Stage(world,baseline);using var store=new SceneWorldObjectMotionStore(world);
            foreach(var source in new[]{SceneWorldMovementSpeedSource.Invalid,(SceneWorldMovementSpeedSource)99})Reject(()=>new SceneWorldExplicitMovementAdmission(world,store,42,source,p,sha));
            Reject(()=>new SceneWorldExplicitMovementAdmission(world,store,42,SceneWorldMovementSpeedSource.AuthoredSpeed0,p,pin));
            Reject(()=>new SceneWorldExplicitMovementAdmission(world,store,42,SceneWorldMovementSpeedSource.AuthoredSpeed0,p,"CRC"));
            Reject(()=>new SceneWorldExplicitMovementAdmission(world,store,42,SceneWorldMovementSpeedSource.ReceivedField7,p,sha));
            var otherPath="object/other.iff";var otherRoot=SharedCreatureAuthoredMovementInputs.FromProfile(SharedCreatureMovementInputProfile.Load(_=>new(TreLookupKind.Present,rootBytes,77),otherPath,sha));
            Reject(()=>new SceneWorldExplicitMovementAdmission(world,store,42,SceneWorldMovementSpeedSource.AuthoredSpeed0,otherRoot,sha));
            using(var foreign=World(sha)){Stage(foreign,baseline);using var foreignStore=new SceneWorldObjectMotionStore(foreign);Reject(()=>new SceneWorldExplicitMovementAdmission(world,foreignStore,42,SceneWorldMovementSpeedSource.AuthoredSpeed0,p,sha));Check(foreignStore.Find(Target(foreign))is null,"foreign motion store no publication");}
            Check(store.Find(Target(world))is null,"constructor failures before initial publication");
            using var a=new SceneWorldExplicitMovementAdmission(world,store,42,SceneWorldMovementSpeedSource.AuthoredSpeed1,p,sha,stepMicroseconds:10000);
            Reject(()=>a.Submit(0,Vector2.UnitX,received));Check(a.Submit(0,Vector2.UnitX)!.Speed==6,"authored branch never uses received");
        }
        using(var pending=World(sha)){Stage(pending,baseline,false);using var store=new SceneWorldObjectMotionStore(pending);Reject(()=>new SceneWorldExplicitMovementAdmission(pending,store,42,SceneWorldMovementSpeedSource.AuthoredSpeed0,p,sha));Check(store.Find(Target(pending))is null,"pending finalized requirement");}
    }
}
