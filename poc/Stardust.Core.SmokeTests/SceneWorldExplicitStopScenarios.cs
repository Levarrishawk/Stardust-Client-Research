using System.Numerics;
using System.Reflection;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

namespace Stardust.Core.SmokeTests;
public static class SceneWorldExplicitStopScenarios
{
    private static int checks;
    private static void Check(bool value,string why) { checks++; if(!value) throw new InvalidDataException(why); }
    private static void Reject(Action action)
    { bool failed=false;try{action();}catch(Exception e)when(e is InvalidDataException or InvalidOperationException or ArgumentException or ObjectDisposedException){failed=true;}Check(failed,"expected rejection"); }
    public const string RootPath="object/root.iff";
    private static readonly string pin=new('0',64);
    private static readonly uint crc=LegacyAssetPath.Checksum(RootPath);
    private static readonly LogicalAnimationClipSelection idle=new("appearance/lat/synthetic.lat","synthetic_idle",pin);
    private static readonly IReadOnlyDictionary<string,LogicalAnimationClipSelection> movement=new Dictionary<string,LogicalAnimationClipSelection>{{"walk",idle},{"run",idle}};
    public static SharedCreatureAuthoredMovementInputs Profile(JsonElement p)
    {
        byte[] raw=Convert.FromHexString(p.GetProperty("Hex").GetString()!);string sha=p.GetProperty("RootSha256").GetString()!;
        return SharedCreatureAuthoredMovementInputs.FromProfile(SharedCreatureMovementInputProfile.Load(q=>q==RootPath?new(TreLookupKind.Present,raw,77):new(TreLookupKind.Missing),RootPath,sha));
    }
    public static SceneWorldCoordinator World(SharedCreatureAuthoredMovementInputs p)=>new(
        new ObjectTemplateCatalog(new[]{new ObjectTemplateCatalogEntry(crc,RootPath,p.Sources[RootPath].Sha256,pin,SceneObjectResourceRecipe.CreatureDetail0)}),
        new Dictionary<SceneObjectResourceRecipe,SceneObjectResourceOptions>{{SceneObjectResourceRecipe.CreatureDetail0,new SceneCreatureResourceOptions(idle,movement,false)}},
        (_,_)=>throw new InvalidDataException("No synthetic resource loads"),SceneCreateZeroQuaternionPolicy.Reject,4,2,32,4194304,32,4194304,1048576,
        replicationPolicy:new(SceneWorldReplicationMode.CreatureView3MixedComponents,new(1024,64,64,16,512),16,1024,new(1024,32,24,64,64,16),CreatureWoundsApplicationPolicy.LegacyBounded,new(1024,32,64,64,16),new(16,16)),
        creatureView4PartialPolicy:new(new(MessageLimits:new(MaximumPayloadBytes:1048576)),new()));
    private static byte[] Start(){using var s=new MemoryStream();using var w=new BinaryWriter(s);w.Write((ushort)9);w.Write(ZoneSceneProtocolCodec.CmdStartSceneOpcode);w.Write((byte)0);w.Write(42UL);w.Write((ushort)0);w.Write(0f);w.Write(0f);w.Write(0f);w.Write((ushort)0);w.Write(0UL);return s.ToArray();}
    public static byte[] Create()=>SceneObjectLifecycleCodec.EncodeCreate(new(42,0,0,0,1,0,0,0,crc,0));
    public static long Stage(SceneWorldCoordinator w,byte[] baseline)
    {long g=w.BeginSelection(42).Generation;Check(w.Accept(g,Start()),"start");Check(w.Accept(g,Create()),"create");Check(w.Accept(g,baseline),"typed3");Check(w.Accept(g,SceneObjectLifecycleCodec.EncodeEndBaselines(new(42))),"end");Check(w.CompleteMinimumSceneBarrier(g,true)is not null,"barrier");return g;}
    public static SceneWorldObjectSnapshot Target(SceneWorldCoordinator w)=>w.Snapshots().Single(s=>s.Object.Handle.ObjectId==42);
    public static void Received(SceneWorldCoordinator w,long generation,uint? field7,uint? field11)
    {var fields=new List<CreatureView4Update>();if(field7 is uint a)fields.Add(new(7,a));if(field11 is uint b)fields.Add(new(11,b));if(fields.Count==0)fields.Add(new(1,0));Check(w.ApplyCreatureView4PartialDelta(generation,w.FindCreatureView4Partial(generation,42),new(42,0,fields.ToArray())),"received");}
    public static SceneWorldExplicitMovementAdmission Admission(SceneWorldCoordinator w,SceneWorldObjectMotionStore motions,SharedCreatureAuthoredMovementInputs p,SceneWorldMovementSpeedSource source,SceneWorldReceivedCreatureMovementInputs reader)=>new(w,motions,42,source,p,p.Sources[RootPath].Sha256,reader,0,10000);
    public static void Run(string fixture,string output)
    {
        using var json=JsonDocument.Parse(File.ReadAllText(fixture));var p=Profile(json.RootElement.GetProperty("Profile"));var missing=Profile(json.RootElement.GetProperty("Missing"));byte[] baseline=Convert.FromHexString(json.RootElement.GetProperty("Baseline").GetString()!);var rows=new List<object>();
        foreach(var c in json.RootElement.GetProperty("Cases").EnumerateArray())
        {
            using var w=World(p);long g=Stage(w,baseline);using var motions=new SceneWorldObjectMotionStore(w);using var reader=new SceneWorldReceivedCreatureMovementInputs(w);Received(w,g,BitConverter.SingleToUInt32Bits(3),BitConverter.SingleToUInt32Bits(8));
            var source=(SceneWorldMovementSpeedSource)c.GetProperty("Source").GetInt32();using var a=Admission(w,motions,p,source,reader);var d=c.GetProperty("Direction").EnumerateArray().Select(x=>x.GetSingle()).ToArray();
            Check(a.SpeedSource==source,"fixed readable source");Check(a.Submit(0,new(d[0],d[1]),(int)source>=3?reader.Read(g,42):null)is not null,"selected movement");long stop=c.GetProperty("Stop").GetInt64();Check(a.SubmitStop(stop),"explicit stop admitted");int partition=c.GetProperty("Partition").GetInt32();for(long t=partition;t<50000;t+=partition)a.Advance(t);var state=a.Advance(50000);Check(state.Active&&state.FaultType is null,"actual stop scheduler ticks");
            rows.Add(new{Source=(int)a.SpeedSource,StopAccepted=true,Position=new[]{state.Transform.Translation.X,state.Transform.Translation.Y,state.Transform.Translation.Z},state.Speed});
        }
        Negatives(p,missing,baseline);File.WriteAllText(output,JsonSerializer.Serialize(new{Cases=rows,Checks=checks}));Console.WriteLine($"R49_STOP_PASS cases={rows.Count} checks={checks}");
    }
    private static void Negatives(SharedCreatureAuthoredMovementInputs p,SharedCreatureAuthoredMovementInputs missing,byte[] baseline)
    {
        using var w=World(p);long g=Stage(w,baseline);using var motions=new SceneWorldObjectMotionStore(w);using var reader=new SceneWorldReceivedCreatureMovementInputs(w);Received(w,g,BitConverter.SingleToUInt32Bits(3),BitConverter.SingleToUInt32Bits(8));
        using var a=Admission(w,motions,p,SceneWorldMovementSpeedSource.ReceivedField7,reader);var source=reader.Read(g,42)!;var captured=a.Submit(0,Vector2.UnitX,source)!;var state=a.Advance(10000);reader.Dispose();Check(a.SubmitStop(10000),"stop does not sample disposed reader");var stopped=a.Advance(20000);Check(stopped.Transform==state.Transform&&stopped.Speed==0&&a.SpeedSource==SceneWorldMovementSpeedSource.ReceivedField7,"exact zero velocity no source switch");Check(!a.IsSourceCurrent(captured),"disposed sampled source remains historical");
        a.Reset(0);var before=a.State;var pub=motions.Find(Target(w));Reject(()=>a.SubmitStop(-1));Reject(()=>a.SubmitStop(60000001));Check(a.State==before&&ReferenceEquals(pub,motions.Find(Target(w))),"stop time failures no mutation");Check(a.SubmitStop(60000000),"exact horizon stop accepted");Reject(()=>a.SubmitStop(59999999));a.Reset(0);
        for(int i=0;i<1024;i++)Check(a.SubmitStop(0),"stop consumes existing pending capacity");Reject(()=>a.SubmitStop(0));Check(a.State.TimestampMicroseconds==0&&a.State.FaultType is null,"capacity rejection atomic/nonsticky");a.Advance(10000);Check(a.SubmitStop(10000),"stop pending capacity freed");Reject(()=>a.SubmitStop(9999));
        Task.Run(()=>Reject(()=>a.SubmitStop(10000))).GetAwaiter().GetResult();var busy=typeof(SceneWorldExplicitMovementAdmission).GetField("busy",BindingFlags.Instance|BindingFlags.NonPublic)!;busy.SetValue(a,true);Reject(()=>a.SubmitStop(10000));busy.SetValue(a,false);
        a.Dispose();a.Dispose();Check(a.SpeedSource==SceneWorldMovementSpeedSource.ReceivedField7,"fixed source remains readable after dispose");Reject(()=>a.SubmitStop(10000));
        foreach(var change in new[]{"recreate","generation","close"})using(var world=World(p))
        {
            long gen=Stage(world,baseline);using var store=new SceneWorldObjectMotionStore(world);using var r=new SceneWorldReceivedCreatureMovementInputs(world);using var old=Admission(world,store,p,SceneWorldMovementSpeedSource.AuthoredSpeed0,r);var saved=old.State;
            if(change=="recreate"){world.Accept(gen,SceneObjectLifecycleCodec.EncodeDestroy(new(42,0)));world.Accept(gen,Create());world.Accept(gen,baseline);world.Accept(gen,SceneObjectLifecycleCodec.EncodeEndBaselines(new(42)));}else if(change=="generation")Stage(world,baseline);else world.Close();Check(!old.SubmitStop(0)&&old.State==saved,"stale stop no driver mutation "+change);
        }
        using(var world=World(missing)){Stage(world,baseline);using var store=new SceneWorldObjectMotionStore(world);using var r=new SceneWorldReceivedCreatureMovementInputs(world);using var m=Admission(world,store,missing,SceneWorldMovementSpeedSource.AuthoredSpeed0,r);Check(m.Submit(0,Vector2.UnitX)is null&&m.SubmitStop(0)&&m.Advance(10000).Speed==0,"explicit stop with absent authored speed; no seed/fallback");}
    }
}
