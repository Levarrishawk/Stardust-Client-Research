using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Geometry;
using Stardust.Core.Session;
namespace Stardust.Core.SmokeTests;
public static class TerrainOfflineSessionControllerScenarios
{
    private static int checks;
    private static readonly string path = "object/root.iff", zero = new('0', 64);
    private static readonly uint crc = LegacyAssetPath.Checksum(path);
    private static void Check(bool value, string why) { checks++; if (!value) throw new InvalidDataException(why); }
    private static void Reject(Action action)
    { try { action(); } catch (Exception e) when (e is InvalidDataException or InvalidOperationException or ArgumentException or NotSupportedException or ObjectDisposedException or OperationCanceledException) { checks++; return; } throw new Exception("Expected rejection"); }
    private static SceneWorldCoordinator World(string pin, uint? rootCrc = null) => new(
        new ObjectTemplateCatalog([new(rootCrc ?? crc, rootCrc is null ? path : "object/wrong.iff", pin, zero, SceneObjectResourceRecipe.CreatureDetail0)]),
        new Dictionary<SceneObjectResourceRecipe, SceneObjectResourceOptions> { [SceneObjectResourceRecipe.CreatureDetail0] = new SceneCreatureResourceOptions(
            new("appearance/lat/synthetic.lat", "idle", zero), new Dictionary<string, LogicalAnimationClipSelection> {
                ["walk"] = new("appearance/lat/synthetic.lat", "idle", zero), ["run"] = new("appearance/lat/synthetic.lat", "idle", zero) }, false) },
        (_, _) => throw new InvalidDataException("No resource loads"), SceneCreateZeroQuaternionPolicy.Reject, 4, 2, 32, 4194304, 32, 4194304, 1048576,
        replicationPolicy: new(SceneWorldReplicationMode.CreatureView3MixedComponents, new(1024, 64, 64, 16, 512), 16, 1024,
            new(1024, 32, 24, 64, 64, 16), CreatureWoundsApplicationPolicy.LegacyBounded, new(1024, 32, 64, 64, 16), new(16, 16)),
        creatureView4PartialPolicy: new(new(MessageLimits: new(MaximumPayloadBytes: 1048576)), new()));
    private static byte[] Start()
    { using var s = new MemoryStream(); using var w = new BinaryWriter(s); w.Write((ushort)9); w.Write(ZoneSceneProtocolCodec.CmdStartSceneOpcode);
        w.Write((byte)0); w.Write(42UL); w.Write((ushort)0); w.Write(0f); w.Write(0f); w.Write(0f); w.Write((ushort)0); w.Write(0UL); return s.ToArray(); }
    private static byte[] Create(uint? rootCrc = null) => SceneObjectLifecycleCodec.EncodeCreate(new(42, 0, 0, 0, 1, 0, 0, 0, rootCrc ?? crc, 0));
    private static long Stage(SceneWorldCoordinator w, byte[] baseline, bool typed, bool final, uint? rootCrc = null)
    { long g = w.BeginSelection(42).Generation; Check(w.Accept(g, Start()), "start"); Check(w.Accept(g, Create(rootCrc)), "create");
        if (typed) Check(w.Accept(g, baseline), "typed3"); if (final) Check(w.Accept(g, SceneObjectLifecycleCodec.EncodeEndBaselines(new(42))), "end");
        Check(w.CompleteMinimumSceneBarrier(g, true) is not null, "barrier"); return g; }
    private static SharedCreatureAuthoredMovementInputs Profile(JsonElement p)
    { var resources = p.GetProperty("Resources").EnumerateArray().ToDictionary(r => r.GetProperty("Path").GetString()!, r => new TreLookupResult(TreLookupKind.Present, Convert.FromHexString(r.GetProperty("Hex").GetString()!), 77));
        return SharedCreatureAuthoredMovementInputs.FromProfile(SharedCreatureMovementInputProfile.Load(q => resources.GetValueOrDefault(q, new(TreLookupKind.Missing)), path, p.GetProperty("RootSha256").GetString()!)); }
    private static TerrainModernHeightPatch Make(JsonElement c)
    {
        var p=c.GetProperty("Profiles").EnumerateArray().Select(v=>(TerrainHeightArithmeticPrecision)v.GetInt32()).ToArray();
        var grid=TerrainSampleCoordinateGrid.Build(c.GetProperty("Request").Deserialize<TerrainSampleCoordinateRequest>(),p[0],p[1],p[2],p[3]);
        var heights=c.GetProperty("Heights").EnumerateArray().Select(v=>v.GetUInt32()).ToArray();
        var data=TerrainCoordinateHeightPatch.Build(grid,heights,Enumerable.Repeat(Vector3.UnitY,heights.Length).ToArray(),c.GetProperty("Colors").EnumerateArray().Select(v=>v.GetUInt32()).ToArray(),c.GetProperty("Mask").GetInt32(),BitConverter.UInt32BitsToSingle(c.GetProperty("ScaleBits").GetUInt32()));
        return TerrainModernHeightPatch.Build(data,TerrainModernNormalPolicy.ModernBinary64CentralDifference);
    }
    private static uint[] V(Vector3 v)=>[BitConverter.SingleToUInt32Bits(v.X),BitConverter.SingleToUInt32Bits(v.Y),BitConverter.SingleToUInt32Bits(v.Z)];
    private static TerrainModernPatchSetRayHit Pick(TerrainModernPatchSetRayQuery query,double x,double z)=>query.Query(new(new(x,100,z),new(0,-1,0),0,200))??throw new InvalidDataException("Synthetic target ray missed.");
    public static object Run(string input)
    {
        checks=0;int negatives=0;using var doc=JsonDocument.Parse(File.ReadAllText(input));var fixture=doc.RootElement;var baseline=Convert.FromHexString(fixture.GetProperty("Baseline").GetString()!);var authored=Profile(fixture.GetProperty("Profiles")[0]);string pin=authored.Sources[path].Sha256;
        var chunks=fixture.GetProperty("Terrain").EnumerateArray().Select(Make).ToArray();var surface=TerrainModernPatchSetSurfaceQuery.Build(TerrainModernHeightPatchSet.Build(chunks,TerrainModernPatchEdgePolicy.RequireBitIdenticalActiveSharedEdges,TerrainModernPatchNormalPolicy.RequireBitIdenticalSharedNormals));var query=TerrainModernPatchSetRayQuery.Build(surface);var output=new List<object>();
        foreach(var c in fixture.GetProperty("Cases").EnumerateArray())
        {
            using var world=World(pin);Stage(world,baseline,true,true);using var motions=new SceneWorldObjectMotionStore(world);var snapshot=world.Snapshots().Single();var start=c.GetProperty("Start");motions.PublishLocal(snapshot,AppearanceTransform.Identity with {Translation=new(start[0].GetSingle(),0,start[1].GetSingle())},0);
            using var driver=new SceneWorldTerrainPointTargetDriver(world,motions,42,query,3,.01f,0,10000,.25f,maximumUpdates:c.GetProperty("Maximum").GetInt32());using var session=new TerrainOfflineSessionController(driver,c.GetProperty("Maximum").GetInt32());var rows=new List<object>();long clock=0;
            void Unchanged(Action action){var own=session.State;var actual=driver.State;var row=motions.Find(snapshot);Reject(action);negatives++;Check(ReferenceEquals(own,session.State)&&ReferenceEquals(actual,driver.State)&&ReferenceEquals(row,motions.Find(snapshot)),"session rejected preflight no owner or borrowed effect");}
            for(int i=0;i<c.GetProperty("Updates").GetInt32();i++)
            {
                foreach(var t in c.GetProperty("Targets").EnumerateArray().Where(e=>e.GetProperty("At").GetInt32()==i))driver.SubmitTarget(Pick(query,t.GetProperty("X").GetDouble(),t.GetProperty("Z").GetDouble()),clock);
                foreach(var m in c.GetProperty("Manual").EnumerateArray().Where(e=>e.GetProperty("At").GetInt32()==i))driver.SetManualInput(new(m.GetProperty("X").GetSingle(),m.GetProperty("Z").GetSingle()),clock);
                foreach(var p in c.GetProperty("IntentPauses").EnumerateArray().Where(e=>e.GetProperty("At").GetInt32()==i))
                {var actual=driver.State;var row=motions.Find(snapshot);session.SetPaused(p.GetProperty("Value").GetBoolean());Check(ReferenceEquals(actual,driver.State)&&ReferenceEquals(row,motions.Find(snapshot)),"pause intent only");}
                if(c.GetProperty("StopAt").GetInt32()==i){var actual=driver.State;var row=motions.Find(snapshot);session.RequestStop();session.RequestStop();Check(ReferenceEquals(actual,driver.State)&&ReferenceEquals(row,motions.Find(snapshot)),"stop intent only/idempotent");}
                if(c.TryGetProperty("Correction",out var correction)&&correction.GetProperty("At").GetInt32()==i){var p=correction.GetProperty("Position");motions.PublishAuthoritative(snapshot,AppearanceTransform.Identity with {Translation=new(p[0].GetSingle(),99,p[1].GetSingle())},0,1);}
                Unchanged(()=>session.AdvanceNext(clock+1,clock+10000));Unchanged(()=>session.AdvanceNext(clock,clock+9000));Unchanged(()=>session.AdvanceNext(clock,clock+10000,new CancellationToken(true)));
                var state=session.AdvanceNext(clock,clock+10000);var result=state.DriverResult;var actualResult=result.DriverResult;Check(ReferenceEquals(result,driver.State)&&result.CompletedUpdates==i+1&&state.CompletedUpdates==i+1&&ReferenceEquals(actualResult,driver.Driver!.State)&&actualResult.Transform==motions.Find(snapshot)!.Transform,"actual session strict tick and borrowed publication");
                rows.Add(new {Index=i,Mode=result.Mode.ToString(),result.AttemptedUpdates,result.CompletedUpdates,result.SuccessfulTargets,Time=actualResult.TimestampMicroseconds,Position=V(actualResult.Transform.Translation),Speed=BitConverter.SingleToUInt32Bits(actualResult.Speed),X=V(actualResult.Transform.RowX),Y=V(actualResult.Transform.RowY),Z=V(actualResult.Transform.RowZ),Manual=new[]{BitConverter.SingleToUInt32Bits(driver.ManualInput.X),BitConverter.SingleToUInt32Bits(driver.ManualInput.Y)},MovementSpeed=BitConverter.SingleToUInt32Bits(driver.MovementSpeed),Session=new {state.CompletedUpdates,state.Paused,state.StopRequested,state.Stopped,state.IsFaulted,state.FaultType}});clock+=10000;
            }
            Check(session.State.Stopped&&session.State.StopRequested&&!session.State.Paused&&driver.State.Mode==SceneWorldTerrainPointTargetMode.Idle&&driver.Target==null&&driver.ManualInput==Vector2.Zero&&driver.State.DriverResult.Speed==0,"terminal only actual zero-speed clear/unpause tick");Unchanged(()=>session.AdvanceNext(clock,clock+10000));Unchanged(()=>session.SetPaused(false));Unchanged(()=>session.RequestStop());
            var owner=Task.Run(()=>{try{_ = session.State;return false;}catch(InvalidOperationException){return true;}}).GetAwaiter().GetResult();Check(owner,"session owner getter");owner=Task.Run(()=>{try{session.RequestStop();return false;}catch(InvalidOperationException){return true;}}).GetAwaiter().GetResult();Check(owner,"session owner mutation");
            var retained=session.State;var borrowed=driver.State;var borrowedRow=motions.Find(snapshot);session.Dispose();session.Dispose();Check(ReferenceEquals(retained,session.State)&&ReferenceEquals(borrowed,driver.State)&&ReferenceEquals(borrowedRow,motions.Find(snapshot)),"session dispose owns no borrowed resources/history");Unchanged(()=>session.AdvanceNext(clock,clock+10000));
            output.Add(new {Name=c.GetProperty("Name").GetString(),Rows=rows});
        }
        foreach(var cap in new[]{0,6001}){using var w=World(pin);Stage(w,baseline,true,true);using var m=new SceneWorldObjectMotionStore(w);using var d=new SceneWorldTerrainPointTargetDriver(w,m,42,query,3,.01f,0,10000);Reject(()=>new TerrainOfflineSessionController(d,cap));negatives++;}
        using(var w=World(pin))
        {Stage(w,baseline,true,true);using var m=new SceneWorldObjectMotionStore(w);using var d=new SceneWorldTerrainPointTargetDriver(w,m,42,query,3,.01f,0,10000);using var s=new TerrainOfflineSessionController(d,10);d.AdvanceNext(0,10000);var actual=d.State;Reject(()=>s.AdvanceNext(0,10000));negatives++;Check(s.State.IsFaulted&&s.State.FaultType=="ExternalDriverProgress"&&s.State.CompletedUpdates==1&&ReferenceEquals(actual,d.State),"foreign external progress terminal actual count no borrowed mutation");Reject(()=>new TerrainOfflineSessionController(d,10));negatives++;}
        using(var w=World(pin))
        {Stage(w,baseline,true,true);using var m=new SceneWorldObjectMotionStore(w);using var d=new SceneWorldTerrainPointTargetDriver(w,m,42,query,3,.01f,0,10000);using var s=new TerrainOfflineSessionController(d,10);w.BeginSelection(42);Reject(()=>s.AdvanceNext(0,10000));negatives++;Check(s.State.IsFaulted&&s.State.CompletedUpdates==0&&d.State.CompletedUpdates==0,"stale mutation-boundary failure terminal no fictitious step");}
        using(var w=World(pin))
        {Stage(w,baseline,true,true);using var m=new SceneWorldObjectMotionStore(w);using var d=new SceneWorldTerrainPointTargetDriver(w,m,42,query,3,.01f,0,10000);using var s=new TerrainOfflineSessionController(d,10);d.SetManualInput(-Vector2.UnitX,0);Reject(()=>s.AdvanceNext(0,10000));negatives++;Check(s.State.IsFaulted&&s.State.CompletedUpdates==0&&d.State.AttemptedUpdates==1&&d.State.Mode==SceneWorldTerrainPointTargetMode.Faulted,"actual driver edge failure terminal attempted prefix");}
        using(var w=World(pin))
        {Stage(w,baseline,true,true);using var m=new SceneWorldObjectMotionStore(w);using var d=new SceneWorldTerrainPointTargetDriver(w,m,42,query,3,.01f,0,10000,maximumUpdates:1);using var s=new TerrainOfflineSessionController(d,2);s.AdvanceNext(0,10000);Reject(()=>s.AdvanceNext(10000,20000));negatives++;Check(s.State.IsFaulted&&s.State.CompletedUpdates==1&&!s.State.Stopped,"hidden configured cap caller mismatch preserves actual prefix");}
        using(var w=World(pin))
        {Stage(w,baseline,true,true);using var m=new SceneWorldObjectMotionStore(w);using var d=new SceneWorldTerrainPointTargetDriver(w,m,42,query,3,.01f,0,10000);using var s=new TerrainOfflineSessionController(d,10);d.Dispose();var actual=d.State;Reject(()=>s.AdvanceNext(0,10000));negatives++;Check(s.State.IsFaulted&&s.State.FaultType=="ExternalDriverUnhealthy"&&ReferenceEquals(actual,d.State),"external disposed driver terminal healthy gate no borrowed mutation");}
        using(var w=World(pin))
        {Stage(w,baseline,true,true);using var m=new SceneWorldObjectMotionStore(w);using var d=new SceneWorldTerrainPointTargetDriver(w,m,42,query,3,.01f,long.MaxValue-10000,10000,maximumUpdates:1);using var s=new TerrainOfflineSessionController(d,1,100000);var own=s.State;var actual=d.State;Reject(()=>s.AdvanceNext(long.MaxValue-10000,long.MaxValue));negatives++;Check(ReferenceEquals(own,s.State)&&ReferenceEquals(actual,d.State),"exact step overflow before stop mutation");}
        return new {Cases=output,NativeChecks=checks,NegativeChecks=negatives};
    }
}
