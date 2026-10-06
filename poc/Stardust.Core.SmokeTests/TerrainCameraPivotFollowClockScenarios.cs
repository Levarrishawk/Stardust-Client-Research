using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Geometry;
using Stardust.Core.Session;
namespace Stardust.Core.SmokeTests;
public static class TerrainCameraPivotFollowClockScenarios
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
        checks=0;using var doc=JsonDocument.Parse(File.ReadAllText(input));var fixture=doc.RootElement;int negatives=0;var pure=new List<object>();
        static Vector3 Read(JsonElement a)=>new(a[0].GetSingle(),a[1].GetSingle(),a[2].GetSingle());
        static object Row(TerrainCameraPivotFollowState s)=>new {Pivot=V(s.Pivot),Target=V(s.Target),Time=s.TimestampMicroseconds,s.Updates,s.Rebases};
        foreach(var c in fixture.GetProperty("Pure").EnumerateArray())
        {
            var offset=Read(c.GetProperty("Offset"));using var clock=new TerrainCameraPivotFollowClock(Read(c.GetProperty("Initial")),offset,c.GetProperty("InitialClock").GetInt64(),c.GetProperty("Step").GetInt64(),c.GetProperty("Response").GetDouble(),maximumUpdates:c.GetProperty("Updates").GetInt32());var rows=new List<object>();
            void Unchanged(Action action){var before=clock.State;Reject(action);negatives++;Check(ReferenceEquals(before,clock.State),"follow preflight preserves exact immutable state");}
            foreach(var e in c.GetProperty("Events").EnumerateArray())
            {
                long time=e.GetProperty("Time").GetInt64();var position=Read(e.GetProperty("Position"));bool rebase=e.GetProperty("Kind").GetString()=="Rebase";
                Unchanged(()=>clock.Advance(position,clock.State.TimestampMicroseconds));Unchanged(()=>clock.Advance(position,clock.State.TimestampMicroseconds-1));Unchanged(()=>clock.Rebase(position,clock.State.TimestampMicroseconds,new CancellationToken(true)));Unchanged(()=>clock.Advance(position,time,new CancellationToken(true)));Unchanged(()=>clock.Rebase(position,clock.State.TimestampMicroseconds+1));
                foreach(var bad in new[]{new Vector3(float.NaN,0,0),new Vector3(0,float.PositiveInfinity,0),new Vector3(0,0,1000001)}){Unchanged(()=>clock.Advance(bad,time));Unchanged(()=>clock.Rebase(bad,clock.State.TimestampMicroseconds));}
                var previous=clock.State;var result=rebase?clock.Rebase(position,time):clock.Advance(position,time);
                Check(ReferenceEquals(result,clock.State)&&result.Updates==previous.Updates+(rebase?0:1)&&result.Rebases==previous.Rebases+(rebase?1:0),"follow owned counters");if(rebase)Check(result.Pivot==result.Target&&result.TimestampMicroseconds==previous.TimestampMicroseconds,"snap without elapsed/budget refund");
                rows.Add(Row(result));
            }
            Unchanged(()=>clock.Advance(Vector3.Zero,clock.State.TimestampMicroseconds+c.GetProperty("Step").GetInt64()));Unchanged(()=>clock.Rebase(Vector3.Zero,clock.State.TimestampMicroseconds));
            var owner=Task.Run(()=>{try{_ = clock.State;return false;}catch(InvalidOperationException){return true;}}).GetAwaiter().GetResult();Check(owner,"follow getter owner");negatives++;
            owner=Task.Run(()=>{try{clock.Advance(Vector3.Zero,0);return false;}catch(InvalidOperationException){return true;}}).GetAwaiter().GetResult();Check(owner,"follow mutation owner");negatives++;
            var retained=clock.State;clock.Dispose();clock.Dispose();Check(ReferenceEquals(retained,clock.State),"disposed state remains historical");Unchanged(()=>clock.Advance(Vector3.Zero,0));Unchanged(()=>clock.Rebase(Vector3.Zero,0));
            pure.Add(new {Name=c.GetProperty("Name").GetString(),Rows=rows});
        }
        foreach(var step in new long[]{0,999,100001}){Reject(()=>new TerrainCameraPivotFollowClock(Vector3.Zero,Vector3.Zero,stepMicroseconds:step));negatives++;}
        foreach(var response in new[]{double.NaN,double.PositiveInfinity,0,0.0000009,10.001}){Reject(()=>new TerrainCameraPivotFollowClock(Vector3.Zero,Vector3.Zero,responseSeconds:response));negatives++;}
        foreach(var cap in new[]{0,257}){Reject(()=>new TerrainCameraPivotFollowClock(Vector3.Zero,Vector3.Zero,maximumUpdates:cap));Reject(()=>new TerrainCameraPivotFollowClock(Vector3.Zero,Vector3.Zero,maximumRebases:cap));negatives+=2;}
        Reject(()=>new TerrainCameraPivotFollowClock(Vector3.Zero,Vector3.Zero,-1));Reject(()=>new TerrainCameraPivotFollowClock(Vector3.Zero,Vector3.Zero,long.MaxValue));Reject(()=>new TerrainCameraPivotFollowClock(Vector3.Zero,new(float.NaN,0,0)));Reject(()=>new TerrainCameraPivotFollowClock(new(1000000,0,0),new(.0625f,0,0)));Reject(()=>new TerrainCameraPivotFollowClock(Vector3.Zero,Vector3.Zero,token:new CancellationToken(true)));negatives+=5;
        using(var clock=new TerrainCameraPivotFollowClock(Vector3.Zero,Vector3.Zero,long.MaxValue-1000,1000,maximumUpdates:1,maximumRebases:1))
        {clock.Rebase(Vector3.One,long.MaxValue-1000);var before=clock.State;Reject(()=>clock.Rebase(Vector3.Zero,long.MaxValue-1000));negatives++;Check(ReferenceEquals(before,clock.State),"rebase cap no refund");clock.Advance(Vector3.One,long.MaxValue);before=clock.State;Reject(()=>clock.Advance(Vector3.Zero,long.MinValue));negatives++;Check(ReferenceEquals(before,clock.State),"complete clock range no wrapping");}
        using(var clock=new TerrainCameraPivotFollowClock(Vector3.Zero,new(.0625f,0,0)))
        {var before=clock.State;Reject(()=>clock.Advance(new(1000000,0,0),10000));Reject(()=>clock.Rebase(new(1000000,0,0),0));negatives+=2;Check(ReferenceEquals(before,clock.State),"unrounded target sum bound before float publication");}
        var baseline=Convert.FromHexString(fixture.GetProperty("Baseline").GetString()!);var authored=Profile(fixture.GetProperty("Profiles")[0]);string pin=authored.Sources[path].Sha256;
        var chunks=fixture.GetProperty("Terrain").EnumerateArray().Select(Make).ToArray();var surface=TerrainModernPatchSetSurfaceQuery.Build(TerrainModernHeightPatchSet.Build(chunks,TerrainModernPatchEdgePolicy.RequireBitIdenticalActiveSharedEdges,TerrainModernPatchNormalPolicy.RequireBitIdenticalSharedNormals));var query=TerrainModernPatchSetRayQuery.Build(surface);var cdriver=fixture.GetProperty("Driver");
        using var world=World(pin);Stage(world,baseline,true,true);using var motions=new SceneWorldObjectMotionStore(world);var snapshot=world.Snapshots().Single();var start=cdriver.GetProperty("Start");motions.PublishLocal(snapshot,AppearanceTransform.Identity with {Translation=new(start[0].GetSingle(),0,start[1].GetSingle())},0);
        using var driver=new SceneWorldTerrainPointTargetDriver(world,motions,42,query,cdriver.GetProperty("Speed").GetSingle(),cdriver.GetProperty("Radius").GetSingle(),0,cdriver.GetProperty("Step").GetInt64(),.25f);
        using var follow=new TerrainCameraPivotFollowClock(driver.Driver!.State.Transform.Translation,Read(fixture.GetProperty("DriverOffset")),maximumUpdates:cdriver.GetProperty("Updates").GetInt32());var driverRows=new List<object>();long timeClock=0,driverStep=cdriver.GetProperty("Step").GetInt64();
        for(int i=0;i<cdriver.GetProperty("Updates").GetInt32();i++)
        {
            foreach(var s in cdriver.GetProperty("Speeds").EnumerateArray().Where(e=>e.GetProperty("At").GetInt32()==i))driver.SetMovementSpeed(s.GetProperty("Value").GetSingle(),timeClock);
            foreach(var t in cdriver.GetProperty("Targets").EnumerateArray().Where(e=>e.GetProperty("At").GetInt32()==i))driver.SubmitTarget(Pick(query,t.GetProperty("X").GetDouble(),t.GetProperty("Z").GetDouble()),timeClock);
            foreach(var m in cdriver.GetProperty("Manual").EnumerateArray().Where(e=>e.GetProperty("At").GetInt32()==i))driver.SetManualInput(new(m.GetProperty("X").GetSingle(),m.GetProperty("Z").GetSingle()),timeClock);
            foreach(var p in cdriver.GetProperty("Pauses").EnumerateArray().Where(e=>e.GetProperty("At").GetInt32()==i))driver.SetPaused(p.GetProperty("Value").GetBoolean(),timeClock);
            if(cdriver.TryGetProperty("Correction",out var correction)&&correction.GetProperty("At").GetInt32()==i)
            {var p=correction.GetProperty("Position");motions.PublishAuthoritative(snapshot,AppearanceTransform.Identity with {Translation=new(p[0].GetSingle(),99,p[1].GetSingle())},0,1);var rebased=follow.Rebase(motions.Find(snapshot)!.Transform.Translation,timeClock);driverRows.Add(new {Kind="Rebase",Index=i,Follow=Row(rebased),Driver=(object?)null});}
            var result=driver.AdvanceNext(timeClock,timeClock+driverStep);var actual=result.DriverResult;var borrowed=motions.Find(snapshot);var prior=follow.State;Reject(()=>follow.Advance(actual.Transform.Translation,timeClock+driverStep,new CancellationToken(true)));negatives++;Check(ReferenceEquals(prior,follow.State),"follow cancellation no accepted mutation");
            var followed=follow.Advance(actual.Transform.Translation,timeClock+driverStep);Check(ReferenceEquals(actual,driver.Driver.State)&&ReferenceEquals(borrowed,motions.Find(snapshot)),"follow has no borrowed driver/publication effects");
            driverRows.Add(new {Kind="Advance",Index=i,Follow=Row(followed),Driver=(object?)new {Position=V(actual.Transform.Translation),Speed=BitConverter.SingleToUInt32Bits(actual.Speed),Time=actual.TimestampMicroseconds,Mode=result.Mode.ToString()}});timeClock+=driverStep;
        }
        var survived=driver.Driver.State;var rowSurvived=motions.Find(snapshot);follow.Dispose();Check(ReferenceEquals(survived,driver.Driver.State)&&ReferenceEquals(rowSurvived,motions.Find(snapshot)),"follow owns no borrowed resources");
        return new {Pure=pure,Driver=driverRows,NativeChecks=checks,NegativeChecks=negatives};
    }
}
