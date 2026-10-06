using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Geometry;
using Stardust.Core.Session;
namespace Stardust.Core.SmokeTests;
public static class TerrainOfflineDestinationGuardScenarios
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
        checks=0; int negatives=0; using var doc=JsonDocument.Parse(File.ReadAllText(input)); var fixture=doc.RootElement;
        var baseline=Convert.FromHexString(fixture.GetProperty("Baseline").GetString()!);var authored=Profile(fixture.GetProperty("Profiles")[0]);string pin=authored.Sources[path].Sha256;
        var chunks=fixture.GetProperty("Terrain").EnumerateArray().Select(Make).ToArray();
        TerrainModernPatchSetSurfaceQuery Surface(bool hole=false)=>TerrainModernPatchSetSurfaceQuery.Build(TerrainModernHeightPatchSet.Build(hole?[chunks[0],chunks[2],chunks[3]]:chunks,TerrainModernPatchEdgePolicy.RequireBitIdenticalActiveSharedEdges,TerrainModernPatchNormalPolicy.RequireBitIdenticalSharedNormals));
        var full=Surface();var sparse=Surface(true);var query=TerrainModernPatchSetRayQuery.Build(full);var holeQuery=TerrainModernPatchSetRayQuery.Build(sparse);var output=new List<object>();
        foreach(var c in fixture.GetProperty("Cases").EnumerateArray())
        {
            using var world=World(pin);Stage(world,baseline,true,true);using var motions=new SceneWorldObjectMotionStore(world);var snapshot=world.Snapshots().Single();var start=c.GetProperty("Start");motions.PublishLocal(snapshot,AppearanceTransform.Identity with {Translation=new(start[0].GetSingle(),0,start[1].GetSingle())},0);
            var ray=c.GetProperty("Sparse").GetBoolean()?holeQuery:query;bool hold=c.GetProperty("Hold").GetBoolean();int updates=c.GetProperty("Updates").GetInt32();long step=c.GetProperty("Step").GetInt64();
            using var driver=new SceneWorldTerrainPointTargetDriver(world,motions,42,ray,c.GetProperty("Speed").GetSingle(),.01f,0,step,.25f,maximumUpdates:updates,holdMissingTerrainDestination:hold);var rows=new List<object>();long clock=0;
            Check(driver.LastDestinationReceipt is null,"no initial receipt");
            void Unchanged(Action action){var s=driver.State;var receipt=driver.LastDestinationReceipt;var row=motions.Find(snapshot);Reject(action);negatives++;Check(ReferenceEquals(s,driver.State)&&ReferenceEquals(receipt,driver.LastDestinationReceipt)&&ReferenceEquals(row,motions.Find(snapshot)),"rejected preflight preserves row/state/receipt");}
            for(int i=0;i<updates;i++)
            {
                foreach(var t in c.GetProperty("Targets").EnumerateArray().Where(e=>e.GetProperty("At").GetInt32()==i))driver.SubmitTarget(Pick(ray,t.GetProperty("X").GetDouble(),t.GetProperty("Z").GetDouble()),clock);
                foreach(var m in c.GetProperty("Manual").EnumerateArray().Where(e=>e.GetProperty("At").GetInt32()==i))driver.SetManualInput(new(m.GetProperty("X").GetSingle(),m.GetProperty("Z").GetSingle()),clock);
                foreach(var p in c.GetProperty("Pauses").EnumerateArray().Where(e=>e.GetProperty("At").GetInt32()==i))driver.SetPaused(p.GetProperty("Value").GetBoolean(),clock);
                if(c.GetProperty("Clears").EnumerateArray().Any(e=>e.GetInt32()==i))driver.ClearTarget(clock);
                if(c.TryGetProperty("Correction",out var correction)&&correction.GetProperty("At").GetInt32()==i){var p=correction.GetProperty("Position");motions.PublishAuthoritative(snapshot,AppearanceTransform.Identity with {Translation=new(p[0].GetSingle(),99,p[1].GetSingle())},0,1);}
                Unchanged(()=>driver.AdvanceNext(clock+1,clock+step));Unchanged(()=>driver.AdvanceNext(clock,clock+step-1));Unchanged(()=>driver.AdvanceNext(clock,clock+step,new CancellationToken(true)));
                var before=motions.Find(snapshot);var result=driver.AdvanceNext(clock,clock+step);var actual=result.DriverResult;var receipt=driver.LastDestinationReceipt;
                Check(hold==(receipt is not null),"opt-in only destination receipt");
                if(receipt is {HeldAtMissingDestination:true})Check(receipt.AcceptedVelocity==Vector3.Zero&&actual.Speed==0,"held actual zero velocity/speed");
                if(result.Mode!=SceneWorldTerrainPointTargetMode.Faulted)Check(result.CompletedUpdates==i+1&&actual.TimestampMicroseconds==clock+step&&actual.Transform==motions.Find(snapshot)!.Transform,"ordinary actual grounded step");
                else Check(result.CompletedUpdates==i&&ReferenceEquals(before,motions.Find(snapshot)),"default missing edge preserves prefix");
                rows.Add(new {Index=i,Mode=result.Mode.ToString(),result.AttemptedUpdates,result.CompletedUpdates,result.SuccessfulTargets,Time=actual.TimestampMicroseconds,Position=V(actual.Transform.Translation),Speed=BitConverter.SingleToUInt32Bits(actual.Speed),X=V(actual.Transform.RowX),Y=V(actual.Transform.RowY),Z=V(actual.Transform.RowZ),Manual=new[]{BitConverter.SingleToUInt32Bits(driver.ManualInput.X),BitConverter.SingleToUInt32Bits(driver.ManualInput.Y)},MovementSpeed=BitConverter.SingleToUInt32Bits(driver.MovementSpeed),Destination=receipt is null?null:new {Proposed=V(receipt.ProposedPosition),Velocity=V(receipt.AcceptedVelocity),Held=receipt.HeldAtMissingDestination}});
                clock+=step;if(result.Mode==SceneWorldTerrainPointTargetMode.Faulted)break;
            }
            if(driver.State.Mode!=SceneWorldTerrainPointTargetMode.Faulted)Unchanged(()=>driver.AdvanceNext(clock,clock+step));
            Check(Task.Run(()=>{try{_=driver.LastDestinationReceipt;return false;}catch(InvalidOperationException){return true;}}).GetAwaiter().GetResult(),"receipt owner affinity");negatives++;
            var history=driver.LastDestinationReceipt;var retainedRow=motions.Find(snapshot);driver.Dispose();driver.Dispose();Check(ReferenceEquals(history,driver.LastDestinationReceipt)&&ReferenceEquals(retainedRow,motions.Find(snapshot))&&ray.Source.Query(start[0].GetDouble(),start[1].GetDouble()) is not null,"history value receipt and borrowers survive dispose");Unchanged(()=>driver.AdvanceNext(clock,clock+step));
            output.Add(new {Name=c.GetProperty("Name").GetString(),Rows=rows});
        }
        // Opt-in admission caps fail before the initial owned driver can publish.
        foreach(var limits in new[]{new SceneWorldTerrainMotionLimits(MaximumPatches:1),new SceneWorldTerrainMotionLimits(MaximumTriangleTestsPerCandidate:1)})
        {using var w=World(pin);Stage(w,baseline,true,true);using var m=new SceneWorldObjectMotionStore(w);var snap=w.Snapshots().Single();var before=m.Find(snap);Reject(()=>new SceneWorldTerrainPointTargetDriver(w,m,42,query,3,.01f,0,10000,terrainLimits:limits,holdMissingTerrainDestination:true));negatives++;Check(ReferenceEquals(before,m.Find(snap)),"caps before publication");}
        // A ray query whose surface cannot reserve one query must reject before publication.
        var zeroQueries=TerrainModernPatchSetSurfaceQuery.Build(full.Source,new(MaximumQueries:0));var zeroRay=TerrainModernPatchSetRayQuery.Build(zeroQueries);
        {using var w=World(pin);Stage(w,baseline,true,true);using var m=new SceneWorldObjectMotionStore(w);var snap=w.Snapshots().Single();Reject(()=>new SceneWorldTerrainPointTargetDriver(w,m,42,zeroRay,3,.01f,0,10000,holdMissingTerrainDestination:true));negatives++;Check(m.Find(snap) is null,"reservation before publication");}
        // Missing current authoritative ground cannot be hidden by a next destination inside coverage.
        {using var w=World(pin);Stage(w,baseline,true,true);using var m=new SceneWorldObjectMotionStore(w);using var d=new SceneWorldTerrainPointTargetDriver(w,m,42,query,3,.01f,0,10000,holdMissingTerrainDestination:true);var snap=w.Snapshots().Single();d.SetManualInput(Vector2.UnitX,0);m.PublishAuthoritative(snap,AppearanceTransform.Identity with {Translation=new(-.01f,99,4)},0,1);var row=m.Find(snap);var state=d.State;Reject(()=>d.AdvanceNext(0,10000));negatives++;Check(ReferenceEquals(row,m.Find(snap))&&ReferenceEquals(state,d.State)&&d.LastDestinationReceipt is null,"missing current authoritative ground explicit pre-effect failure, no undo");}
        // Actual source identity is enforced by the existing target admission, with guard enabled.
        {using var w=World(pin);Stage(w,baseline,true,true);using var m=new SceneWorldObjectMotionStore(w);using var d=new SceneWorldTerrainPointTargetDriver(w,m,42,query,3,.01f,0,10000,holdMissingTerrainDestination:true);var foreign=TerrainModernPatchSetRayQuery.Build(Surface());var before=d.State;Reject(()=>d.SubmitTarget(Pick(foreign,1,1),0));negatives++;Check(ReferenceEquals(before,d.State)&&d.LastDestinationReceipt is null,"foreign target source unchanged");}
        {using var w=World(pin);Stage(w,baseline,true,true);using var m=new SceneWorldObjectMotionStore(w);var snap=w.Snapshots().Single();m.PublishLocal(snap,AppearanceTransform.Identity with {Translation=new(15.98f,0,4)},0);using var d=new SceneWorldTerrainPointTargetDriver(w,m,42,query,3,.01f,0,10000,terrainLimits:new(MaximumPositionComponent:16),holdMissingTerrainDestination:true);d.SetManualInput(Vector2.UnitX,0);var before=d.State;var row=m.Find(snap);Reject(()=>d.AdvanceNext(0,10000));negatives++;Check(ReferenceEquals(before,d.State)&&ReferenceEquals(row,m.Find(snap))&&d.LastDestinationReceipt is null,"missing destination cannot mask configured position cap");}
        return new {Cases=output,NativeChecks=checks,NegativeChecks=negatives};
    }
}
