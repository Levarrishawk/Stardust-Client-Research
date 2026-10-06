using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Geometry;
using Stardust.Core.Session;
namespace Stardust.Core.SmokeTests;
public static class SceneWorldTerrainPointTargetDriverScenarios
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
        checks=0;using var doc=JsonDocument.Parse(File.ReadAllText(input));var fixture=doc.RootElement;
        var baseline=Convert.FromHexString(fixture.GetProperty("Baseline").GetString()!);var authored=Profile(fixture.GetProperty("Profiles")[0]);string pin=authored.Sources[path].Sha256;
        var chunks=fixture.GetProperty("Terrain").EnumerateArray().Select(Make).ToArray();
        TerrainModernPatchSetSurfaceQuery Surface(int[] ids)=>TerrainModernPatchSetSurfaceQuery.Build(TerrainModernHeightPatchSet.Build(ids.Select(i=>chunks[i]).ToArray(),TerrainModernPatchEdgePolicy.RequireBitIdenticalActiveSharedEdges,TerrainModernPatchNormalPolicy.RequireBitIdenticalSharedNormals));
        var full=Surface([0,1,2,3]);var sparse=Surface([0,3]);var fullRay=TerrainModernPatchSetRayQuery.Build(full);var sparseRay=TerrainModernPatchSetRayQuery.Build(sparse);var foreignRay=TerrainModernPatchSetRayQuery.Build(Surface([0,1,2,3]));
        var results=new List<object>();int negatives=0;
        foreach(var c in fixture.GetProperty("Cases").EnumerateArray())
        {
            using var world=World(pin);long generation=Stage(world,baseline,true,true);using var motions=new SceneWorldObjectMotionStore(world);var snapshot=world.Snapshots().Single();
            var start=c.GetProperty("Start");Check(motions.PublishLocal(snapshot,AppearanceTransform.Identity with {Translation=new(start[0].GetSingle(),0,start[1].GetSingle())},0),"initial supplied pose");
            var ray=c.GetProperty("Sparse").GetBoolean()?sparseRay:fullRay;float speed=c.GetProperty("Speed").GetSingle(),radius=c.GetProperty("Radius").GetSingle();long step=c.GetProperty("Step").GetInt64();
            var prior=motions.Find(snapshot)!;
            using(var foreignWorld=World(pin))using(var foreignStore=new SceneWorldObjectMotionStore(foreignWorld))
            {Stage(foreignWorld,baseline,true,true);Reject(()=>new SceneWorldTerrainPointTargetDriver(world,foreignStore,42,ray,speed,radius,0,step));negatives++;}
            Reject(()=>new SceneWorldTerrainPointTargetDriver(world,motions,42,ray,0,radius,0,step));Reject(()=>new SceneWorldTerrainPointTargetDriver(world,motions,42,ray,11,radius,0,step));
            Reject(()=>new SceneWorldTerrainPointTargetDriver(world,motions,42,ray,speed,0,0,step));Reject(()=>new SceneWorldTerrainPointTargetDriver(world,motions,42,ray,speed,radius,long.MaxValue,step));
            Reject(()=>new SceneWorldTerrainPointTargetDriver(world,motions,42,ray,speed,radius,0,step,maximumUpdates:129));Reject(()=>new SceneWorldTerrainPointTargetDriver(world,motions,42,ray,speed,radius,0,step,maximumTargets:17));
            Reject(()=>new SceneWorldTerrainPointTargetDriver(world,motions,42,ray,speed,radius,0,step,cancellationToken:new CancellationToken(true)));negatives+=7;
            Check(ReferenceEquals(prior,motions.Find(snapshot)),"constructor preflight no initial publication");
            using var controller=new SceneWorldTerrainPointTargetDriver(world,motions,42,ray,speed,radius,0,step,.25f);var driver=controller.Driver!;
            Check(ReferenceEquals(controller.Query,ray),"actual borrowed ray query");long clock=0;var rows=new List<object>();SceneWorldTerrainPointTargetReceipt? previousTarget=null;
            var firstEvent=c.GetProperty("Targets")[0];var firstHit=Pick(ray,firstEvent.GetProperty("X").GetDouble(),firstEvent.GetProperty("Z").GetDouble());
            var initialMotion=motions.Find(snapshot)!;
            Reject(()=>controller.SubmitTarget(firstHit with {TriangleIndex=-1},0));
            Reject(()=>controller.SubmitTarget(firstHit with {Position=firstHit.Position with {X=firstHit.Position.X+.25}},0));negatives+=2;
            Reject(()=>controller.SubmitTarget(Pick(foreignRay,firstHit.Position.X,firstHit.Position.Z),0));Reject(()=>controller.SubmitTarget(firstHit,1));Reject(()=>controller.SubmitTarget(firstHit,0,new CancellationToken(true)));
            Reject(()=>controller.AdvanceNext(0,step+1));Reject(()=>controller.AdvanceNext(0,step,new CancellationToken(true)));negatives+=5;
            Check(controller.State.AttemptedUpdates==0&&controller.Target==null&&ReferenceEquals(initialMotion,motions.Find(snapshot)),"target/update admission before effects");
            int updates=c.GetProperty("Updates").GetInt32();
            for(int index=0;index<updates;index++)
            {
                foreach(var e in c.GetProperty("Targets").EnumerateArray().Where(t=>t.GetProperty("At").GetInt32()==index))
                {
                    var hit=Pick(ray,e.GetProperty("X").GetDouble(),e.GetProperty("Z").GetDouble());var receipt=controller.SubmitTarget(hit,clock);
                    Check(ReferenceEquals(receipt.Hit,hit)&&ReferenceEquals(receipt.Query,ray)&&ReferenceEquals(receipt,controller.Target)&&controller.IsCurrent(receipt),"actual hit/query/receipt identities");
                    if(previousTarget!=null)Check(!controller.IsCurrent(previousTarget),"retarget retires previous receipt currency");previousTarget=receipt;
                }
                if(c.TryGetProperty("Correction",out var correction)&&correction.GetProperty("At").GetInt32()==index)
                {var position=correction.GetProperty("Position");Check(motions.PublishAuthoritative(snapshot,AppearanceTransform.Identity with {Translation=new(position[0].GetSingle(),99,position[1].GetSingle())},0,1),"non-collinear actual authoritative correction");}
                bool wasArrived=controller.State.Mode==SceneWorldTerrainPointTargetMode.Arrived;
                var before=motions.Find(snapshot)!;var state=controller.AdvanceNext(clock,clock+step);var actual=state.DriverResult;
                Check(ReferenceEquals(actual,driver.State),"actual one-step result identity");
                if(wasArrived)Check(actual.Speed==0,"one further arrival update publishes zero stored speed");
                rows.Add(new {Index=index,Mode=state.Mode.ToString(),state.AttemptedUpdates,state.CompletedUpdates,state.SuccessfulTargets,Time=actual.TimestampMicroseconds,Position=V(actual.Transform.Translation),Speed=BitConverter.SingleToUInt32Bits(actual.Speed),X=V(actual.Transform.RowX),Y=V(actual.Transform.RowY),Z=V(actual.Transform.RowZ)});
                if(state.Mode==SceneWorldTerrainPointTargetMode.Faulted)
                {Check(state.CompletedUpdates==index&&ReferenceEquals(before,motions.Find(snapshot)),"late outside failure preserves exact successful publication prefix");Check(ReferenceEquals(state,controller.AdvanceNext(0,step)),"fault terminal no retry");negatives++;break;}
                Check(actual.StepsThisUpdate==1&&actual.DroppedMicroseconds==0&&actual.SuspendedMicroseconds==0&&actual.Transform==motions.Find(snapshot)!.Transform,"one actual grounded step/publication");clock+=step;
            }
            var owner=Task.Run(()=>{try{_=controller.State;return false;}catch(InvalidOperationException){return true;}}).GetAwaiter().GetResult();Check(owner,"owner guard");negatives++;
            var last=motions.Find(snapshot)!;controller.Retire();controller.Dispose();Check(controller.Query==null&&controller.Target==null&&controller.Driver==null&&!driver.State.Active&&!controller.IsCurrent(previousTarget!),"owned cleanup and prior target retirement");
            Check(ReferenceEquals(last,motions.Find(snapshot))&&ray.Query(new(new(1.25,100,1.25),new(0,-1,0),0,200))!=null,"borrowed store/query survive");
            results.Add(new {Name=c.GetProperty("Name").GetString(),Rows=rows});
        }
        // Separate real limits/lifecycle probes, without repeating the frozen numerical suite.
        using(var world=World(pin))
        {long generation=Stage(world,baseline,true,true);using var motions=new SceneWorldObjectMotionStore(world);var snapshot=world.Snapshots().Single();
         using var bounded=new SceneWorldTerrainPointTargetDriver(world,motions,42,fullRay,2,.01f,0,10000,maximumUpdates:2,maximumTargets:16);
         var hit=Pick(fullRay,1,1);for(int i=0;i<16;i++)bounded.SubmitTarget(hit,0);Reject(()=>bounded.SubmitTarget(hit,0));negatives++;
         bounded.AdvanceNext(0,10000);bounded.AdvanceNext(10000,20000);var row=motions.Find(snapshot);Reject(()=>bounded.AdvanceNext(20000,30000));Check(ReferenceEquals(row,motions.Find(snapshot))&&bounded.State.AttemptedUpdates==2,"update cap before any calls");negatives++;
         using(var maximumStore=new SceneWorldObjectMotionStore(world))
         {
             Check(maximumStore.PublishLocal(snapshot,AppearanceTransform.Identity with {Translation=new(1.25f,0,1.25f)},0),"maximum zero-distance seed");
             using var maximum=new SceneWorldTerrainPointTargetDriver(world,maximumStore,42,fullRay,2,.01f,0,1000);
             maximum.SubmitTarget(Pick(fullRay,1.25,1.25),0);
             for(int i=0;i<128;i++){var state=maximum.AdvanceNext(i*1000,(i+1)*1000);Check(state.CompletedUpdates==i+1&&state.DriverResult.Speed==0&&state.Mode==SceneWorldTerrainPointTargetMode.Arrived,"hard128 exact update witness");}
             var retained=maximumStore.Find(snapshot);Reject(()=>maximum.AdvanceNext(128000,129000));Check(ReferenceEquals(retained,maximumStore.Find(snapshot)),"hard128 one-less remaining cap before publication");negatives++;
         }
         // Invalid correction is admitted by the borrowed store but rejected before target-driver Submit.
         using var corrected=new SceneWorldTerrainPointTargetDriver(world,motions,42,fullRay,2,.01f,20000,10000);
         corrected.SubmitTarget(hit,20000);Check(motions.PublishAuthoritative(snapshot,AppearanceTransform.Identity with {Translation=new(1_000_001,0,0)},0,1),"outside correction store accepts");var correctionRow=motions.Find(snapshot);
         Reject(()=>corrected.AdvanceNext(20000,30000));Check(ReferenceEquals(correctionRow,motions.Find(snapshot))&&corrected.State.AttemptedUpdates==0,"invalid correction admission before Submit");negatives++;
         Check(world.Accept(generation,SceneObjectLifecycleCodec.EncodeDestroy(new(42,0)))&&world.Accept(generation,Create())&&world.Accept(generation,baseline)&&world.Accept(generation,SceneObjectLifecycleCodec.EncodeEndBaselines(new(42))),"recreated numeric id");
         Check(corrected.AdvanceNext(20000,30000).Mode==SceneWorldTerrainPointTargetMode.Unavailable,"incarnation unavailable before effects");Reject(()=>corrected.SubmitTarget(hit,20000));negatives++;
        }
        using(var world=World(pin)){Stage(world,baseline,true,true);using var motions=new SceneWorldObjectMotionStore(world);using var controller=new SceneWorldTerrainPointTargetDriver(world,motions,42,fullRay,2,.01f,0,10000);motions.Dispose();Check(controller.AdvanceNext(0,10000).Mode==SceneWorldTerrainPointTargetMode.Unavailable,"closed store unavailable");negatives++;}
        using(var world=World(pin))
        {Stage(world,baseline,true,true);using var motions=new SceneWorldObjectMotionStore(world);using var controller=new SceneWorldTerrainPointTargetDriver(world,motions,42,fullRay,2,.01f,0,10000);
         var receipt=controller.SubmitTarget(Pick(fullRay,1,1),0);world.BeginSelection(42);
         Check(controller.AdvanceNext(0,10000).Mode==SceneWorldTerrainPointTargetMode.Unavailable&&controller.State.AttemptedUpdates==0&&!controller.IsCurrent(receipt),"generation retirement before new effects");negatives++;}
        return new {Cases=results,NativeChecks=checks,NegativeChecks=negatives};
    }
}
