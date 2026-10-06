using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Geometry;
using Stardust.Core.Session;
namespace Stardust.Core.SmokeTests;
public static class TerrainOfflinePlaygroundSettingsScenarios
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
    private static object Describe(TerrainOfflinePlaygroundSettings s)
    {var budget=s.CreateBudget();var other=s.CreateBudget();Check(!ReferenceEquals(budget,other)&&budget.MaximumUpdates==s.MaximumUpdates&&budget.MaximumTargets==s.MaximumClickAttempts,"fresh exact actual budget");return new {s.Version,s.MaximumUpdates,s.MaximumClickAttempts,s.StepMicroseconds,Walk=BitConverter.SingleToUInt32Bits(s.WalkSpeed),Run=BitConverter.SingleToUInt32Bits(s.RunSpeed),Radius=BitConverter.SingleToUInt32Bits(s.ArrivalRadius),s.HoldMissingDestination,BudgetUpdates=budget.MaximumUpdates,BudgetTargets=budget.MaximumTargets};}
    public static object Run(string input)
    {
        checks=0;int negatives=0;using var doc=JsonDocument.Parse(File.ReadAllText(input));var fixture=doc.RootElement;var output=new List<object>();
        foreach(var c in fixture.GetProperty("Cases").EnumerateArray())
        {
            TerrainOfflinePlaygroundSettings? value=null;string reject="None";
            try{value=TerrainOfflinePlaygroundSettings.Parse(Convert.FromHexString(c.GetProperty("Hex").GetString()!));}
            catch(InvalidDataException){reject="Input";negatives++;}
            output.Add(new {Name=c.GetProperty("Name").GetString(),Reject=reject,Settings=value is null?null:Describe(value)});
        }
        var defaults=Describe(TerrainOfflinePlaygroundSettings.Create());
        Check(typeof(TerrainOfflinePlaygroundSettings).GetConstructors().Length==0&&typeof(TerrainOfflinePlaygroundSettings).GetProperties().All(p=>p.SetMethod is null),"private ctor/get-only properties cannot init bypass validation");
        var valid=TerrainOfflinePlaygroundSettings.Create();Check(Task.Run(()=>valid.CreateBudget().MaximumUpdates==6000&&valid.WalkSpeed==3).GetAwaiter().GetResult(),"immutable settings usable across threads");
        foreach(var action in new Action[]{
            ()=>TerrainOfflinePlaygroundSettings.Create(version:0),()=>TerrainOfflinePlaygroundSettings.Create(version:2),
            ()=>TerrainOfflinePlaygroundSettings.Create(maximumUpdates:0),()=>TerrainOfflinePlaygroundSettings.Create(maximumUpdates:6001),
            ()=>TerrainOfflinePlaygroundSettings.Create(maximumClickAttempts:0),()=>TerrainOfflinePlaygroundSettings.Create(maximumClickAttempts:129),
            ()=>TerrainOfflinePlaygroundSettings.Create(stepMicroseconds:9999),()=>TerrainOfflinePlaygroundSettings.Create(stepMicroseconds:long.MaxValue),
            ()=>TerrainOfflinePlaygroundSettings.Create(walkSpeed:0),()=>TerrainOfflinePlaygroundSettings.Create(walkSpeed:float.NaN),
            ()=>TerrainOfflinePlaygroundSettings.Create(walkSpeed:float.PositiveInfinity),()=>TerrainOfflinePlaygroundSettings.Create(walkSpeed:11),
            ()=>TerrainOfflinePlaygroundSettings.Create(runSpeed:2),()=>TerrainOfflinePlaygroundSettings.Create(runSpeed:float.NaN),
            ()=>TerrainOfflinePlaygroundSettings.Create(runSpeed:float.NegativeInfinity),()=>TerrainOfflinePlaygroundSettings.Create(runSpeed:11),
            ()=>TerrainOfflinePlaygroundSettings.Create(arrivalRadius:0),()=>TerrainOfflinePlaygroundSettings.Create(arrivalRadius:float.NaN),
            ()=>TerrainOfflinePlaygroundSettings.Create(arrivalRadius:float.PositiveInfinity),()=>TerrainOfflinePlaygroundSettings.Create(arrivalRadius:1.01f)})
        {Reject(action);negatives++;}
        var baseline=Convert.FromHexString(fixture.GetProperty("Baseline").GetString()!);var authored=Profile(fixture.GetProperty("Profiles")[0]);var chunks=fixture.GetProperty("Terrain").EnumerateArray().Select(Make).ToArray();
        var surface=TerrainModernPatchSetSurfaceQuery.Build(TerrainModernHeightPatchSet.Build(chunks,TerrainModernPatchEdgePolicy.RequireBitIdenticalActiveSharedEdges,TerrainModernPatchNormalPolicy.RequireBitIdenticalSharedNormals));var query=TerrainModernPatchSetRayQuery.Build(surface);
        var settings=TerrainOfflinePlaygroundSettings.Parse(Convert.FromHexString(fixture.GetProperty("JoinSettingsHex").GetString()!));
        using var world=World(authored.Sources[path].Sha256);Stage(world,baseline,true,true);using var motions=new SceneWorldObjectMotionStore(world);var snapshot=world.Snapshots().Single();motions.PublishLocal(snapshot,AppearanceTransform.Identity with {Translation=new(7.8f,0,7.8f)},0);
        var before=motions.Find(snapshot);var budget=settings.CreateBudget();Check(ReferenceEquals(before,motions.Find(snapshot)),"settings/budget creation no world mutation");
        using var driver=new SceneWorldTerrainPointTargetDriver(world,motions,42,query,settings.WalkSpeed,settings.ArrivalRadius,0,settings.StepMicroseconds,.25f,settings.MaximumUpdates,settings.MaximumClickAttempts,budget:budget,holdMissingTerrainDestination:settings.HoldMissingDestination);
        using var controller=new TerrainOfflineSessionController(driver,settings.MaximumUpdates,settings.StepMicroseconds);driver.SetManualInput(Vector2.UnitX,0);var rows=new List<object>();long clock=0;
        for(int i=0;i<settings.MaximumUpdates;i++)
        {var result=controller.AdvanceNext(clock,clock+settings.StepMicroseconds);var actual=result.DriverResult.DriverResult;Check(result.CompletedUpdates==i+1&&actual.Transform==motions.Find(snapshot)!.Transform,"actual parsed bounded tick");rows.Add(new {Index=i,Mode=result.DriverResult.Mode.ToString(),Completed=result.CompletedUpdates,Time=actual.TimestampMicroseconds,Position=V(actual.Transform.Translation),Speed=BitConverter.SingleToUInt32Bits(actual.Speed),Stopped=result.Stopped,Held=driver.LastDestinationReceipt!.HeldAtMissingDestination});clock+=settings.StepMicroseconds;}
        Check(controller.State.Stopped&&controller.State.CompletedUpdates==3&&driver.State.DriverResult.Speed==0&&driver.Target is null&&driver.ManualInput==Vector2.Zero,"max3 actual final zero tick");
        var history=controller.State;var row=motions.Find(snapshot);Reject(()=>controller.AdvanceNext(clock,clock+settings.StepMicroseconds));negatives++;Check(ReferenceEquals(history,controller.State)&&ReferenceEquals(row,motions.Find(snapshot)),"no extra terminal tick");controller.Dispose();Check(ReferenceEquals(history,controller.State)&&ReferenceEquals(row,motions.Find(snapshot))&&driver.State.Mode==SceneWorldTerrainPointTargetMode.Idle,"borrowed driver/history retained");
        return new {Cases=output,Defaults=defaults,Join=rows,NativeChecks=checks,NegativeChecks=negatives};
    }
}
