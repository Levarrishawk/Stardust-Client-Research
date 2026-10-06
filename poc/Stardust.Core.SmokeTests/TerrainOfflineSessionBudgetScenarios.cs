using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Geometry;
using Stardust.Core.Session;
namespace Stardust.Core.SmokeTests;
public static class TerrainOfflineSessionBudgetScenarios
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
        var chunks=fixture.GetProperty("Terrain").EnumerateArray().Select(Make).ToArray();var surface=TerrainModernPatchSetSurfaceQuery.Build(TerrainModernHeightPatchSet.Build(chunks,TerrainModernPatchEdgePolicy.RequireBitIdenticalActiveSharedEdges,TerrainModernPatchNormalPolicy.RequireBitIdenticalSharedNormals));var query=TerrainModernPatchSetRayQuery.Build(surface);var clips=fixture.GetProperty("Clips");
        PreviewLocomotionClip Describe(int i){var e=clips[i];return new(e.GetProperty("FrameRate").GetSingle(),e.GetProperty("EndFrame").GetUInt16(),e.GetProperty("IdleTimeScale").GetDouble(),e.GetProperty("AuthoredSpeed").GetSingle());}
        CompressedLocomotion Messages(int i)=>CompressedLocomotion.Create(CompressedKeyframeDecoder.Decode(Convert.FromHexString(clips[i].GetProperty("Hex").GetString()!)));
        foreach(var bad in new[]{0,6001}){Reject(()=>new TerrainOfflineSessionBudget(bad,16));negatives++;}foreach(var bad in new[]{0,129}){Reject(()=>new TerrainOfflineSessionBudget(128,bad));negatives++;}
        var budget=new TerrainOfflineSessionBudget(1200,32);Check(budget.MaximumUpdates==1200&&budget.MaximumTargets==32,"immutable explicit budget counts");
        using(var w=World(pin))
        {
            Stage(w,baseline,true,true);using var m=new SceneWorldObjectMotionStore(w);
            Reject(()=>new SceneWorldTerrainPointTargetDriver(w,m,42,query,3,.01f,0,10000,maximumUpdates:129));negatives++;
            Reject(()=>new SceneWorldTerrainPointTargetDriver(w,m,42,query,3,.01f,0,10000,maximumTargets:17));negatives++;
            Reject(()=>new SceneWorldTerrainPointTargetDriver(w,m,42,query,3,.01f,0,10000,maximumUpdates:1201,budget:budget));negatives++;
            Reject(()=>new SceneWorldTerrainPointTargetDriver(w,m,42,query,3,.01f,0,10000,maximumTargets:33,budget:budget));negatives++;
            Reject(()=>new SceneWorldTerrainPointTargetDriver(w,m,42,query,3,.01f,0,10000,budget:new(1,1)));negatives++;
            Reject(()=>new TerrainCameraPivotFollowClock(Vector3.Zero,Vector3.Zero,maximumUpdates:257));negatives++;
            Reject(()=>new TerrainCameraPivotFollowClock(Vector3.Zero,Vector3.Zero,maximumUpdates:1201,budget:budget));negatives++;
            Reject(()=>new TerrainCameraPivotFollowClock(Vector3.Zero,Vector3.Zero,maximumUpdates:1200,maximumRebases:257,budget:budget));negatives++;
            using var p=new SceneWorldTerrainPointTargetDriver(w,m,42,query,3,.01f,0,10000,budget:new(6000,128));
            Reject(()=>new SceneWorldTerrainLocomotionObserver(w,m,p.Driver!,10000,Describe(0),Describe(1),Describe(2),3.5f,Messages(0),Messages(1),Messages(2),maximumObservations:257));negatives++;
            Reject(()=>new SceneWorldTerrainLocomotionObserver(w,m,p.Driver!,10000,Describe(0),Describe(1),Describe(2),3.5f,Messages(0),Messages(1),Messages(2),maximumObservations:1201,budget:budget));negatives++;
            using var o=new SceneWorldTerrainLocomotionObserver(w,m,p.Driver!,10000,Describe(0),Describe(1),Describe(2),3.5f,Messages(0),Messages(1),Messages(2),budget:new(6000,128));
            using var f=new TerrainCameraPivotFollowClock(p.Driver!.State.Transform.Translation,Vector3.Zero,budget:new(6000,128));
            for(int i=0;i<128;i++){p.AdvanceNext(i*10000L,(i+1)*10000L);o.Advance((i+1)*10000L);f.Advance(p.Driver!.State.Transform.Translation,(i+1)*10000L);}
            var ps=p.State;var os=o.State;var fs=f.State;Reject(()=>p.AdvanceNext(1280000,1290000));Reject(()=>o.Advance(1290000));Reject(()=>f.Advance(Vector3.Zero,1290000));negatives+=3;Check(ReferenceEquals(ps,p.State)&&ReferenceEquals(os,o.State)&&ReferenceEquals(fs,f.State),"passing budget never changes default requested128 caps");
        }
        using(var w=World(pin))
        {
            Stage(w,baseline,true,true);using var m=new SceneWorldObjectMotionStore(w);long near=long.MaxValue-10000;
            Reject(()=>new SceneWorldTerrainPointTargetDriver(w,m,42,query,3,.01f,near,10000,maximumUpdates:1200,budget:budget));negatives++;
            Reject(()=>new TerrainCameraPivotFollowClock(Vector3.Zero,Vector3.Zero,near,maximumUpdates:1200,budget:budget));negatives++;
            using var p=new SceneWorldTerrainPointTargetDriver(w,m,42,query,3,.01f,near,10000,maximumUpdates:1);
            Reject(()=>new SceneWorldTerrainLocomotionObserver(w,m,p.Driver!,10000,Describe(0),Describe(1),Describe(2),3.5f,Messages(0),Messages(1),Messages(2),maximumObservations:1200,budget:budget));negatives++;
        }
        using var world=World(pin);Stage(world,baseline,true,true);using var motions=new SceneWorldObjectMotionStore(world);var snapshot=world.Snapshots().Single();var c=fixture.GetProperty("Cases")[0];var start=c.GetProperty("Start");motions.PublishLocal(snapshot,AppearanceTransform.Identity with {Translation=new(start[0].GetSingle(),0,start[1].GetSingle())},0);
        using var controller=new SceneWorldTerrainPointTargetDriver(world,motions,42,query,3,.01f,0,10000,.25f,maximumUpdates:1200,maximumTargets:20,budget:budget);
        using var observer=new SceneWorldTerrainLocomotionObserver(world,motions,controller.Driver!,10000,Describe(0),Describe(1),Describe(2),3.5f,Messages(0),Messages(1),Messages(2),maximumObservations:1200,budget:budget);
        using var follow=new TerrainCameraPivotFollowClock(controller.Driver!.State.Transform.Translation,new(0,2,0),maximumUpdates:1200,budget:budget);
        var rows=new List<object>();long clock=0;int eventCount=0;SceneWorldTerrainLocomotionReceipt? historic=null;
        for(int i=0;i<1200;i++)
        {
            foreach(var s in c.GetProperty("Speeds").EnumerateArray().Where(e=>e.GetProperty("At").GetInt32()==i))controller.SetMovementSpeed(s.GetProperty("Value").GetSingle(),clock);
            foreach(var t in c.GetProperty("Targets").EnumerateArray().Where(e=>e.GetProperty("At").GetInt32()==i))controller.SubmitTarget(Pick(query,t.GetProperty("X").GetDouble(),t.GetProperty("Z").GetDouble()),clock);
            foreach(var m in c.GetProperty("Manual").EnumerateArray().Where(e=>e.GetProperty("At").GetInt32()==i))controller.SetManualInput(new(m.GetProperty("X").GetSingle(),m.GetProperty("Z").GetSingle()),clock);
            foreach(var p in c.GetProperty("Pauses").EnumerateArray().Where(e=>e.GetProperty("At").GetInt32()==i))controller.SetPaused(p.GetProperty("Value").GetBoolean(),clock);
            if(c.TryGetProperty("Correction",out var correction)&&correction.GetProperty("At").GetInt32()==i)
            {var p=correction.GetProperty("Position");motions.PublishAuthoritative(snapshot,AppearanceTransform.Identity with {Translation=new(p[0].GetSingle(),99,p[1].GetSingle())},0,1);var auth=motions.Find(snapshot)!;var before=observer.State;observer.Rebase(auth,clock);follow.Rebase(auth.Transform.Translation,clock);Check(observer.State.Observations==before.Observations&&ReferenceEquals(before.Locomotion,observer.State.Locomotion),"long correction no phase or observation refund");}
            if(i==512){var ps=controller.State;var os=observer.State;var fs=follow.State;Reject(()=>controller.AdvanceNext(clock,clock+10000,new CancellationToken(true)));Reject(()=>observer.Advance(clock+10000,new CancellationToken(true)));Reject(()=>follow.Advance(Vector3.Zero,clock+10000,new CancellationToken(true)));negatives+=3;Check(ReferenceEquals(ps,controller.State)&&ReferenceEquals(os,observer.State)&&ReferenceEquals(fs,follow.State),"mid-session cancellation no hidden reset");}
            var result=controller.AdvanceNext(clock,clock+10000);var actual=result.DriverResult;var receipt=observer.Advance(clock+10000);var followed=follow.Advance(actual.Transform.Translation,clock+10000);eventCount+=receipt.Events.Count;if(i==127)historic=receipt;
            Check(result.CompletedUpdates==i+1&&receipt.State.Observations==i+1&&followed.Updates==i+1&&ReferenceEquals(actual,controller.Driver.State)&&ReferenceEquals(receipt.Motion,motions.Find(snapshot)),"long actual driver/observer/follow persistence");
            if(i==255)Check(receipt.State.Locomotion.AcceptedDistance>historic!.State.Locomotion.AcceptedDistance&&historic.Events.State==historic.State.Locomotion,"no128-reset and historical receipt retained");
            rows.Add(new {Index=i,Mode=result.Mode.ToString(),result.AttemptedUpdates,result.CompletedUpdates,result.SuccessfulTargets,Time=actual.TimestampMicroseconds,Position=V(actual.Transform.Translation),Speed=BitConverter.SingleToUInt32Bits(actual.Speed),X=V(actual.Transform.RowX),Y=V(actual.Transform.RowY),Z=V(actual.Transform.RowZ),Manual=new[]{BitConverter.SingleToUInt32Bits(controller.ManualInput.X),BitConverter.SingleToUInt32Bits(controller.ManualInput.Y)},MovementSpeed=BitConverter.SingleToUInt32Bits(controller.MovementSpeed),Observer=new {receipt.State.Observations,receipt.State.TimestampMicroseconds,receipt.State.MotionRevision,receipt.State.Generation,Mode=receipt.State.Locomotion.Mode.ToString(),receipt.State.Locomotion.Frame,receipt.State.Locomotion.NormalizedPhase,receipt.State.Locomotion.AcceptedDistance,receipt.State.Locomotion.Switches,Heading=V(receipt.State.Locomotion.Heading),receipt.State.UnwrappedPhase,receipt.State.GenerationSeconds,receipt.State.CompletedCycles,Events=receipt.Events.ToArray()},Follow=new {Pivot=V(followed.Pivot),Target=V(followed.Target),Time=followed.TimestampMicroseconds,followed.Updates,followed.Rebases}});clock+=10000;
        }
        var finalPoint=controller.State;var finalObserver=observer.State;var finalFollow=follow.State;var finalRow=motions.Find(snapshot);Reject(()=>controller.AdvanceNext(clock,clock+10000));Reject(()=>controller.SubmitTarget(Pick(query,8,8),clock));Reject(()=>observer.Advance(clock+10000));Reject(()=>follow.Advance(Vector3.Zero,clock+10000));Reject(()=>follow.Rebase(Vector3.Zero,clock));negatives+=5;Check(ReferenceEquals(finalPoint,controller.State)&&ReferenceEquals(finalObserver,observer.State)&&ReferenceEquals(finalFollow,follow.State)&&ReferenceEquals(finalRow,motions.Find(snapshot)),"last-stage caps preserve all history and borrowed row");
        var actualBorrowed=controller.Driver!.State;observer.Dispose();follow.Dispose();Check(ReferenceEquals(actualBorrowed,controller.Driver.State)&&ReferenceEquals(finalRow,motions.Find(snapshot)),"observer/follow disposal preserves borrowed state");controller.Dispose();Check(ReferenceEquals(finalRow,motions.Find(snapshot)),"long controller disposal preserves world/store");
        return new {Cases=new[]{new {Name=c.GetProperty("Name").GetString(),Rows=rows}},NativeChecks=checks,NegativeChecks=negatives,EventCount=eventCount,DefaultProbeUpdates=128};
    }
}
