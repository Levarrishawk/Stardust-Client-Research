using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Geometry;
using Stardust.Core.Session;
namespace Stardust.Core.SmokeTests;
public static class TerrainConfiguredOfflineSessionScenarios
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
        var chunks=fixture.GetProperty("Terrain").EnumerateArray().Select(Make).ToArray();var surface=TerrainModernPatchSetSurfaceQuery.Build(TerrainModernHeightPatchSet.Build(chunks,TerrainModernPatchEdgePolicy.RequireBitIdenticalActiveSharedEdges,TerrainModernPatchNormalPolicy.RequireBitIdenticalSharedNormals));var query=TerrainModernPatchSetRayQuery.Build(surface);
        var settings=TerrainOfflinePlaygroundSettings.Parse(Convert.FromHexString(fixture.GetProperty("SettingsHex").GetString()!));var rows=new List<object>();object budgetValues;
        using(var world=World(pin))
        {
            Stage(world,baseline,true,true);using var motions=new SceneWorldObjectMotionStore(world);var snapshot=world.Snapshots().Single();motions.PublishLocal(snapshot,AppearanceTransform.Identity with {Translation=new(7.8f,0,7.8f)},0);
            using var owner=new TerrainConfiguredOfflineSession(world,motions,42,query,settings,0,.25f);var point=owner.PointDriver;var controller=owner.Controller;var budget=owner.Budget;
            Check(ReferenceEquals(settings,owner.Settings)&&!owner.IsDisposed&&budget.MaximumUpdates==3&&budget.MaximumTargets==32&&point.MovementSpeed==settings.WalkSpeed&&ReferenceEquals(point.Query,query),"exact settings/query/children/budget ownership");budgetValues=new {budget.MaximumUpdates,budget.MaximumTargets};
            var picked=Pick(query,7.9,7.8);for(int i=0;i<32;i++)point.SubmitTarget(picked,0);var fullTarget=point.State;Reject(()=>point.SubmitTarget(picked,0));negatives++;Check(ReferenceEquals(fullTarget,point.State)&&point.State.SuccessfulTargets==32,"configured actual target capacity32 then reject unchanged");point.SetManualInput(Vector2.UnitX,0);long clock=0;
            for(int i=0;i<3;i++)
            {
                var before=controller.State;var row=motions.Find(snapshot);Reject(()=>controller.AdvanceNext(clock,clock+10000,new CancellationToken(true)));negatives++;Check(ReferenceEquals(before,controller.State)&&ReferenceEquals(row,motions.Find(snapshot)),"precancel child no effects");
                var s=controller.AdvanceNext(clock,clock+settings.StepMicroseconds);var actual=s.DriverResult.DriverResult;Check(s.CompletedUpdates==i+1&&actual.Transform==motions.Find(snapshot)!.Transform,"owned actual tick publication");rows.Add(new {Index=i,Mode=s.DriverResult.Mode.ToString(),Completed=s.CompletedUpdates,Time=actual.TimestampMicroseconds,Position=V(actual.Transform.Translation),Speed=BitConverter.SingleToUInt32Bits(actual.Speed),Stopped=s.Stopped,Held=point.LastDestinationReceipt!.HeldAtMissingDestination,Targets=s.DriverResult.SuccessfulTargets});clock+=settings.StepMicroseconds;
            }
            var history=controller.State;var receipt=point.LastDestinationReceipt;var retained=motions.Find(snapshot);Check(history.Stopped&&point.State.DriverResult.Speed==0&&point.ManualInput==Vector2.Zero,"actual final zero stop");
            foreach(var getter in new Action[]{()=>{_=owner.PointDriver;},()=>{_=owner.Controller;},()=>{_=owner.Settings;},()=>{_=owner.Budget;},()=>{_=owner.IsDisposed;},()=>owner.Dispose()})
            {Check(Task.Run(()=>{try{getter();return false;}catch(InvalidOperationException){return true;}}).GetAwaiter().GetResult(),"owner getter/dispose affinity");negatives++;}
            Check(!owner.IsDisposed&&ReferenceEquals(history,controller.State),"foreign dispose did not mutate");owner.Dispose();owner.Dispose();
            Check(owner.IsDisposed&&ReferenceEquals(point,owner.PointDriver)&&ReferenceEquals(controller,owner.Controller)&&ReferenceEquals(settings,owner.Settings)&&ReferenceEquals(budget,owner.Budget)&&ReferenceEquals(history,controller.State)&&point.State.Mode==SceneWorldTerrainPointTargetMode.Disposed&&ReferenceEquals(receipt,point.LastDestinationReceipt),"idempotent parent disposes actual children and preserves histories/references");
            Check(ReferenceEquals(retained,motions.Find(snapshot))&&world.Snapshots().Single().Object.Incarnation==snapshot.Object.Incarnation&&query.Source.Query(7.8,7.8) is not null,"borrowed world/store/query remain alive row retained");Reject(()=>controller.AdvanceNext(clock,clock+10000));negatives++;Reject(()=>point.SetManualInput(Vector2.UnitX,clock));negatives++;
        }
        using(var world=World(pin))
        {
            Stage(world,baseline,true,true);using var motions=new SceneWorldObjectMotionStore(world);var snapshot=world.Snapshots().Single();
            void RejectedConstructor(Action action){var row=motions.Find(snapshot);Reject(action);negatives++;Check(ReferenceEquals(row,motions.Find(snapshot))&&query.Source.Query(1,1) is not null,"failed construction no new row/borrower disposal");}
            RejectedConstructor(()=>new TerrainConfiguredOfflineSession(null!,motions,42,query,settings,0));RejectedConstructor(()=>new TerrainConfiguredOfflineSession(world,null!,42,query,settings,0));RejectedConstructor(()=>new TerrainConfiguredOfflineSession(world,motions,42,null!,settings,0));RejectedConstructor(()=>new TerrainConfiguredOfflineSession(world,motions,42,query,null!,0));
            RejectedConstructor(()=>new TerrainConfiguredOfflineSession(world,motions,42,query,settings,0,token:new CancellationToken(true)));
            RejectedConstructor(()=>new TerrainConfiguredOfflineSession(world,motions,42,query,settings,-1));RejectedConstructor(()=>new TerrainConfiguredOfflineSession(world,motions,42,query,settings,long.MaxValue));RejectedConstructor(()=>new TerrainConfiguredOfflineSession(world,motions,43,query,settings,0));
            RejectedConstructor(()=>new TerrainConfiguredOfflineSession(world,motions,42,query,settings,0,float.NaN));RejectedConstructor(()=>new TerrainConfiguredOfflineSession(world,motions,42,query,settings,0,1001));RejectedConstructor(()=>new TerrainConfiguredOfflineSession(world,motions,42,query,settings,0,terrainLimits:new(MaximumTriangleTestsPerCandidate:1)));
            Check(Task.Run(()=>{try{new TerrainConfiguredOfflineSession(world,motions,42,query,settings,0);return false;}catch(InvalidOperationException){return true;}}).GetAwaiter().GetResult(),"wrong owner construction rejects before publication");negatives++;Check(motions.Find(snapshot) is null,"failed constructors leave no initial row");
            motions.PublishLocal(snapshot,AppearanceTransform.Identity with {Translation=new(20,0,4)},0);RejectedConstructor(()=>new TerrainConfiguredOfflineSession(world,motions,42,query,settings,0));Check(motions.Find(snapshot)!.Transform.Translation.X==20,"actual initial grounding failure retains existing row, no false rollback");
            world.BeginSelection(42);Reject(()=>new TerrainConfiguredOfflineSession(world,motions,42,query,settings,0));negatives++;
        }
        using(var world=World(pin))
        {Stage(world,baseline,true,true);using var motions=new SceneWorldObjectMotionStore(world);using var other=World(pin);Stage(other,baseline,true,true);using var foreign=new SceneWorldObjectMotionStore(other);Reject(()=>new TerrainConfiguredOfflineSession(world,foreign,42,query,settings,0));negatives++;Check(motions.Find(world.Snapshots().Single()) is null&&foreign.Find(other.Snapshots().Single()) is null,"foreign store admission no row effects");}
        using(var world=World(pin))
        {Stage(world,baseline,true,true);using var motions=new SceneWorldObjectMotionStore(world);using var owner=new TerrainConfiguredOfflineSession(world,motions,42,query,settings,0);var point=owner.PointDriver;var controller=owner.Controller;point.AdvanceNext(0,10000);var actual=point.State;Reject(()=>controller.AdvanceNext(0,10000));negatives++;var history=controller.State;Check(history.IsFaulted&&history.FaultType=="ExternalDriverProgress"&&history.CompletedUpdates==1&&ReferenceEquals(actual,point.State),"foreign advance exact fault prefix");var row=motions.Find(world.Snapshots().Single());owner.Dispose();Check(ReferenceEquals(history,controller.State)&&ReferenceEquals(row,motions.Find(world.Snapshots().Single()))&&point.State.Mode==SceneWorldTerrainPointTargetMode.Disposed,"fault cleanup preserves actual row/history");}
        foreach(bool pointFirst in new[]{false,true})
        {using var world=World(pin);Stage(world,baseline,true,true);using var motions=new SceneWorldObjectMotionStore(world);using var owner=new TerrainConfiguredOfflineSession(world,motions,42,query,settings,0);if(pointFirst)owner.PointDriver.Dispose();else owner.Controller.Dispose();owner.Dispose();Check(owner.IsDisposed&&owner.PointDriver.State.Mode==SceneWorldTerrainPointTargetMode.Disposed&&query.Source.Query(1,1) is not null,"external child dispose then parent idempotent borrower-safe cleanup");}
        return new {Budget=budgetValues,Join=rows,NativeChecks=checks,NegativeChecks=negatives};
    }
}
