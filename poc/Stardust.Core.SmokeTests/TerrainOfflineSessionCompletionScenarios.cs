using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Geometry;
using Stardust.Core.Session;
namespace Stardust.Core.SmokeTests;
public static class TerrainOfflineSessionCompletionScenarios
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
        checks=0;int negatives=0;using var doc=JsonDocument.Parse(File.ReadAllText(input));var fixture=doc.RootElement;var baseline=Convert.FromHexString(fixture.GetProperty("Baseline").GetString()!);var authored=Profile(fixture.GetProperty("Profiles")[0]);var chunks=fixture.GetProperty("Terrain").EnumerateArray().Select(Make).ToArray();var surface=TerrainModernPatchSetSurfaceQuery.Build(TerrainModernHeightPatchSet.Build(chunks,TerrainModernPatchEdgePolicy.RequireBitIdenticalActiveSharedEdges,TerrainModernPatchNormalPolicy.RequireBitIdenticalSharedNormals));var query=TerrainModernPatchSetRayQuery.Build(surface);var output=new List<object>();
        foreach(var c in fixture.GetProperty("Cases").EnumerateArray())
        {
            using var world=World(authored.Sources[path].Sha256);Stage(world,baseline,true,true);using var motions=new SceneWorldObjectMotionStore(world);var snapshot=world.Snapshots().Single();var start=c.GetProperty("Start");motions.PublishLocal(snapshot,AppearanceTransform.Identity with {Translation=new(start[0].GetSingle(),0,start[1].GetSingle())},0);
            int maximum=c.GetProperty("Maximum").GetInt32();var settings=TerrainOfflinePlaygroundSettings.Create(maximumUpdates:maximum,maximumClickAttempts:1,holdMissingDestination:c.GetProperty("Hold").GetBoolean());using var owner=new TerrainConfiguredOfflineSession(world,motions,42,query,settings,0,.25f);var controller=owner.Controller;var point=owner.PointDriver;var records=new List<object>();long clock=0;
            TerrainOfflineSessionCompletion Capture(bool finish=false)
            {var state=controller.State;var actual=point.State;var row=motions.Find(snapshot);var report=TerrainOfflineSessionCompletion.Capture(controller,maximum,finish);Check(ReferenceEquals(state,controller.State)&&ReferenceEquals(actual,point.State)&&ReferenceEquals(row,motions.Find(snapshot)),"capture pure actual snapshot no row/state mutation");string json=JsonSerializer.Serialize(report);using var d=JsonDocument.Parse(json);Check(d.RootElement.EnumerateObject().Count()==10&&d.RootElement.EnumerateObject().All(p=>p.Value.ValueKind is JsonValueKind.Number or JsonValueKind.String or JsonValueKind.True or JsonValueKind.False),"serialized primitives only, no borrowed spans/geometry/world");return report;}
            if(c.GetProperty("Name").GetString()=="initial")records.Add(new {Name="initial",Completion=Capture()});
            else
            {
                if(c.GetProperty("Point").GetBoolean())point.SubmitTarget(Pick(query,8.5,7.8),0);else point.SetManualInput(Vector2.UnitX,0);
                for(int i=0;i<c.GetProperty("Updates").GetInt32();i++)
                {
                    if(c.GetProperty("StopAt").GetInt32()==i){controller.RequestStop();records.Add(new {Name="pending-stop",Completion=Capture(true)});}
                    if(c.GetProperty("Fault").GetBoolean()&&i==1)
                    {Reject(()=>controller.AdvanceNext(clock,clock+10000));negatives++;Check(controller.State.IsFaulted&&point.State.CompletedUpdates==1&&point.State.AttemptedUpdates==2,"actual missing destination fault retained prefix");break;}
                    controller.AdvanceNext(clock,clock+10000);clock+=10000;
                }
                records.Add(new {Name="actual",Completion=Capture()});
                if(c.GetProperty("Override").GetBoolean())records.Add(new {Name="explicit-finish",Completion=Capture(true)});
            }
            foreach(int invalid in new[]{0,6001}){Reject(()=>TerrainOfflineSessionCompletion.Capture(controller,invalid));negatives++;}
            Reject(()=>TerrainOfflineSessionCompletion.Capture(null!,maximum));negatives++;
            Check(Task.Run(()=>{try{TerrainOfflineSessionCompletion.Capture(controller,maximum);return false;}catch(InvalidOperationException){return true;}}).GetAwaiter().GetResult(),"capture inherits controller owner guard");negatives++;
            if(controller.State.CompletedUpdates>1){var state=controller.State;Reject(()=>TerrainOfflineSessionCompletion.Capture(controller,1));negatives++;Check(ReferenceEquals(state,controller.State),"caller maximum inconsistent with actual counts rejects unchanged");}
            var retained=Capture();string serialized=JsonSerializer.Serialize(retained);var rowBefore=motions.Find(snapshot);owner.Dispose();Check(serialized==JsonSerializer.Serialize(retained)&&ReferenceEquals(rowBefore,motions.Find(snapshot))&&query.Source.Query(start[0].GetDouble(),start[1].GetDouble()) is not null,"immutable primitive history survives owner disposal and borrowers intact");Check(serialized==JsonSerializer.Serialize(TerrainOfflineSessionCompletion.Capture(controller,maximum)),"disposed controller historical snapshot remains identical");
            output.Add(new {Name=c.GetProperty("Name").GetString(),Records=records});
        }
        Check(typeof(TerrainOfflineSessionCompletion).GetConstructors().Length==0&&typeof(TerrainOfflineSessionCompletion).GetProperties().All(p=>p.SetMethod is null&&p.PropertyType is { } t&&(t.IsPrimitive||t.IsEnum||t==typeof(string))),"private immutable primitive-only report shape");
        return new {Cases=output,NativeChecks=checks,NegativeChecks=negatives};
    }
}
