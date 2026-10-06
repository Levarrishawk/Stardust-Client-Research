using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Geometry;
using Stardust.Core.Session;
namespace Stardust.Core.SmokeTests;
public static class TerrainCameraRelativeMovementScenarios
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
        checks=0;using var doc=JsonDocument.Parse(File.ReadAllText(input));var fixture=doc.RootElement;var baseline=Convert.FromHexString(fixture.GetProperty("Baseline").GetString()!);var authored=Profile(fixture.GetProperty("Profiles")[0]);string pin=authored.Sources[path].Sha256;
        var chunks=fixture.GetProperty("Terrain").EnumerateArray().Select(Make).ToArray();var surface=TerrainModernPatchSetSurfaceQuery.Build(TerrainModernHeightPatchSet.Build(chunks,TerrainModernPatchEdgePolicy.RequireBitIdenticalActiveSharedEdges,TerrainModernPatchNormalPolicy.RequireBitIdenticalSharedNormals));var query=TerrainModernPatchSetRayQuery.Build(surface);var output=new List<object>();int negatives=0;
        static float F(JsonElement v)=>BitConverter.UInt32BitsToSingle(v.GetUInt32());
        static Vector2 Keys(JsonElement e)=>new(F(e.GetProperty("Keys")[0]),F(e.GetProperty("Keys")[1]));
        static Vector3 Forward(JsonElement e)=>new(F(e.GetProperty("Forward")[0]),F(e.GetProperty("Forward")[1]),F(e.GetProperty("Forward")[2]));
        var projections=new List<uint[]>();
        foreach(var mapping in fixture.GetProperty("Mappings").EnumerateArray())
        {
            var keys=Keys(mapping);var forward=Forward(mapping);var oldKeys=keys;var oldForward=forward;var projected=TerrainCameraRelativeMovement.Project(keys,forward);
            Check(keys==oldKeys&&forward==oldForward&&projected.Length()<=1&&(double)projected.X*projected.X+(double)projected.Y*projected.Y<=1,"pure source values and double/float disk bounds");
            Check(projected==TerrainCameraRelativeMovement.Project(keys,forward),"pure deterministic repeated mapping");
            Check(Task.Run(()=>TerrainCameraRelativeMovement.Project(keys,forward)).GetAwaiter().GetResult()==projected,"pure thread-independent projection");
            projections.Add([BitConverter.SingleToUInt32Bits(projected.X),BitConverter.SingleToUInt32Bits(projected.Y)]);
        }
        foreach(var bad in fixture.GetProperty("InvalidMappings").EnumerateArray()){Reject(()=>TerrainCameraRelativeMovement.Project(Keys(bad),Forward(bad)));negatives++;}
        Check((int)SceneWorldTerrainPointTargetMode.Paused==6&&(int)SceneWorldTerrainPointTargetMode.Manual==7,"append manual enum");
        foreach(var c in fixture.GetProperty("Cases").EnumerateArray())
        {
            using var world=World(pin);long generation=Stage(world,baseline,true,true);using var motions=new SceneWorldObjectMotionStore(world);var snapshot=world.Snapshots().Single();var start=c.GetProperty("Start");
            motions.PublishLocal(snapshot,AppearanceTransform.Identity with {Translation=new(start[0].GetSingle(),0,start[1].GetSingle())},0);
            using var controller=new SceneWorldTerrainPointTargetDriver(world,motions,42,query,c.GetProperty("Speed").GetSingle(),c.GetProperty("Radius").GetSingle(),0,c.GetProperty("Step").GetInt64(),.25f);long clock=0,step=c.GetProperty("Step").GetInt64();var rows=new List<object>();SceneWorldTerrainPointTargetReceipt? retained=null;
            var stateBefore=controller.State;var rowBefore=motions.Find(snapshot);var driverBefore=controller.Driver!.State;
            foreach(var invalid in new[]{0f,-1f,10.00001f,float.NaN,float.PositiveInfinity,float.NegativeInfinity}){Reject(()=>controller.SetMovementSpeed(invalid,0));negatives++;}
            Reject(()=>controller.SetMovementSpeed(3,1));Reject(()=>controller.SetMovementSpeed(3,0,new CancellationToken(true)));negatives+=2; Check(controller.MovementSpeed==c.GetProperty("Speed").GetSingle(),"initial speed unchanged after failed admission");
            Check(controller.ManualInput==Vector2.Zero&&ReferenceEquals(stateBefore,controller.State)&&ReferenceEquals(rowBefore,motions.Find(snapshot))&&ReferenceEquals(driverBefore,controller.Driver.State),"manual negative admission no intent/driver/store effects");
            for(int i=0;i<c.GetProperty("Updates").GetInt32();i++)
            {
                foreach(var change in c.GetProperty("Speeds").EnumerateArray().Where(e=>e.GetProperty("At").GetInt32()==i))
                {
                    var before=controller.State;var row=motions.Find(snapshot);var actualDriver=controller.Driver;var driverState=actualDriver!.State;var target=controller.Target;var direction=controller.ManualInput;bool paused=controller.IsPaused;var actualQuery=controller.Query;
                    float supplied=change.GetProperty("Value").GetSingle();controller.SetMovementSpeed(supplied,clock);controller.SetMovementSpeed(supplied,clock);
                    Check(controller.MovementSpeed==supplied&&ReferenceEquals(before,controller.State)&&ReferenceEquals(row,motions.Find(snapshot))&&ReferenceEquals(actualDriver,controller.Driver)&&ReferenceEquals(driverState,controller.Driver.State)&&ReferenceEquals(target,controller.Target)&&ReferenceEquals(actualQuery,controller.Query)&&controller.ManualInput==direction&&controller.IsPaused==paused,"speed setter identity and samevalue admission without effects");
                    if(before.Mode==SceneWorldTerrainPointTargetMode.Arrived)Check(controller.State.Mode==SceneWorldTerrainPointTargetMode.Arrived&&driverState.Speed>0&&ReferenceEquals(row,motions.Find(snapshot)),"arrived speed setter preserves final approach until ordinary zero tick");
                    Reject(()=>controller.SetMovementSpeed(supplied,clock+1));Reject(()=>controller.SetMovementSpeed(supplied,clock,new CancellationToken(true)));negatives+=2;
                    Check(controller.MovementSpeed==supplied&&ReferenceEquals(before,controller.State)&&ReferenceEquals(row,motions.Find(snapshot)),"samevalue still validates clock/token");
                }
                foreach(var t in c.GetProperty("Targets").EnumerateArray().Where(t=>t.GetProperty("At").GetInt32()==i))
                {retained=controller.SubmitTarget(Pick(query,t.GetProperty("X").GetDouble(),t.GetProperty("Z").GetDouble()),clock);Check(controller.ManualInput==Vector2.Zero&&ReferenceEquals(retained,controller.Target),"successful point replaces manual only after admission");}
                foreach(var p in c.GetProperty("Pauses").EnumerateArray().Where(p=>p.GetProperty("At").GetInt32()==i))controller.SetPaused(p.GetProperty("Value").GetBoolean(),clock);
                foreach(var m in c.GetProperty("Manual").EnumerateArray().Where(m=>m.GetProperty("At").GetInt32()==i))
                {
                    var row=motions.Find(snapshot);var driver=controller.Driver.State;var before=controller.State;var old=controller.Target;bool paused=controller.IsPaused;var direction=TerrainCameraRelativeMovement.Project(Keys(m),Forward(m));
                    Reject(()=>controller.SetManualInput(TerrainCameraRelativeMovement.Project(Vector2.One,Vector3.UnitY),clock));negatives++;
                    Check(ReferenceEquals(before,controller.State)&&ReferenceEquals(driver,controller.Driver.State)&&ReferenceEquals(row,motions.Find(snapshot))&&ReferenceEquals(old,controller.Target),"invalid projection before setter has zero driver/intent/publication effects");
                    controller.SetManualInput(direction,clock);
                    Check(controller.ManualInput==direction&&controller.Target==null&&controller.State.Target==null&&controller.IsPaused==paused&&controller.State.SuccessfulTargets==before.SuccessfulTargets&&controller.State.AttemptedUpdates==before.AttemptedUpdates&&ReferenceEquals(driver,controller.Driver.State)&&ReferenceEquals(driver,controller.State.DriverResult)&&ReferenceEquals(row,motions.Find(snapshot)),"manual setter only replaces intent retaining budgets/driver/motion");
                    if(old!=null)Check(!controller.IsCurrent(old),"point receipt stale after manual");retained=null;
                    var bad=Pick(query,1,1) with {TriangleIndex=-1};var active=controller.State;Reject(()=>controller.SubmitTarget(bad,clock));Check(controller.ManualInput==direction&&ReferenceEquals(active,controller.State),"failed point admission preserves manual");negatives++;
                }
                foreach(var e in c.GetProperty("Clears").EnumerateArray().Where(e=>e.GetInt32()==i))
                {var row=motions.Find(snapshot);var driver=controller.Driver.State;int count=controller.State.SuccessfulTargets;controller.ClearTarget(clock);retained=null;Check(controller.ManualInput==Vector2.Zero&&controller.Target==null&&controller.State.SuccessfulTargets==count&&ReferenceEquals(driver,controller.Driver.State)&&ReferenceEquals(row,motions.Find(snapshot)),"clear all movement intents without cap refund/driver effects");}
                if(c.TryGetProperty("Correction",out var correction)&&correction.GetProperty("At").GetInt32()==i)
                {var p=correction.GetProperty("Position");motions.PublishAuthoritative(snapshot,AppearanceTransform.Identity with {Translation=new(p[0].GetSingle(),99,p[1].GetSingle())},0,1);}
                var ownedDirection=controller.ManualInput;var result=controller.AdvanceNext(clock,clock+step);var actual=result.DriverResult;
                Check(ReferenceEquals(actual,controller.Driver.State)&&actual.StepsThisUpdate==1&&result.CompletedUpdates==i+1&&actual.Transform==motions.Find(snapshot)!.Transform&&actual.Speed<=controller.MovementSpeed,"actual one-step grounded result and strict float speed cap");
                Check(controller.ManualInput==ownedDirection,"manual retained through tick/correction/pause");if(controller.IsPaused)Check(actual.Speed==0&&result.Mode==SceneWorldTerrainPointTargetMode.Paused,"pause stops but retains manual");
                rows.Add(new {Index=i,Mode=result.Mode.ToString(),result.AttemptedUpdates,result.CompletedUpdates,result.SuccessfulTargets,Time=actual.TimestampMicroseconds,Position=V(actual.Transform.Translation),Speed=BitConverter.SingleToUInt32Bits(actual.Speed),X=V(actual.Transform.RowX),Y=V(actual.Transform.RowY),Z=V(actual.Transform.RowZ),Manual=new[]{BitConverter.SingleToUInt32Bits(ownedDirection.X),BitConverter.SingleToUInt32Bits(ownedDirection.Y)},MovementSpeed=BitConverter.SingleToUInt32Bits(controller.MovementSpeed)});clock+=step;
            }
            var owner=Task.Run(()=>{try{controller.SetMovementSpeed(3,clock);return false;}catch(InvalidOperationException){return true;}}).GetAwaiter().GetResult();Check(owner,"speed setter owner thread");negatives++;
            var getterOwner=Task.Run(()=>{try{_ = controller.MovementSpeed;return false;}catch(InvalidOperationException){return true;}}).GetAwaiter().GetResult();Check(getterOwner,"speed getter owner thread");negatives++;
            var last=motions.Find(snapshot);var owned=controller.Driver!;float retainedSpeed=controller.MovementSpeed;controller.Dispose();Check(controller.MovementSpeed==retainedSpeed&&controller.ManualInput==Vector2.Zero&&controller.Target==null&&!owned.State.Active&&ReferenceEquals(last,motions.Find(snapshot)),"dispose drops intents owneddriveronly");Reject(()=>controller.SetMovementSpeed(3,clock));negatives++;
            output.Add(new {Name=c.GetProperty("Name").GetString(),Rows=rows});
        }
        return new {Cases=output,Mappings=projections,NativeChecks=checks,NegativeChecks=negatives};
    }
}
