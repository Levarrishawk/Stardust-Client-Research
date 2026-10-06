using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Geometry;
using Stardust.Core.Session;
namespace Stardust.Core.SmokeTests;
public static class SceneWorldTerrainPointTargetPauseScenarios
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
        var surface=TerrainModernPatchSetSurfaceQuery.Build(TerrainModernHeightPatchSet.Build(chunks,TerrainModernPatchEdgePolicy.RequireBitIdenticalActiveSharedEdges,TerrainModernPatchNormalPolicy.RequireBitIdenticalSharedNormals));var query=TerrainModernPatchSetRayQuery.Build(surface);
        var cases=new List<object>();int negatives=0;
        Check((int)SceneWorldTerrainPointTargetMode.Idle==0&&(int)SceneWorldTerrainPointTargetMode.Disposed==5&&(int)SceneWorldTerrainPointTargetMode.Paused==6,"appended enum without renumbering");
        foreach(var c in fixture.GetProperty("Cases").EnumerateArray())
        {
            using var world=World(pin);long generation=Stage(world,baseline,true,true);using var motions=new SceneWorldObjectMotionStore(world);var snapshot=world.Snapshots().Single();
            var start=c.GetProperty("Start");Check(motions.PublishLocal(snapshot,AppearanceTransform.Identity with {Translation=new(start[0].GetSingle(),0,start[1].GetSingle())},0),"supplied pose");
            using var controller=new SceneWorldTerrainPointTargetDriver(world,motions,42,query,c.GetProperty("Speed").GetSingle(),c.GetProperty("Radius").GetSingle(),0,c.GetProperty("Step").GetInt64(),.25f);
            var initial=motions.Find(snapshot)!;var initialDriver=controller.Driver!.State;
            controller.ValidateTargetAdmission(0);Check(controller.Target==null&&ReferenceEquals(initial,motions.Find(snapshot))&&ReferenceEquals(initialDriver,controller.Driver.State),"fresh no-target pre-pick admission read-only");
            Reject(()=>controller.ValidateTargetAdmission(1));Reject(()=>controller.ValidateTargetAdmission(0,new CancellationToken(true)));negatives+=2;
            Reject(()=>controller.SetPaused(true,1));Reject(()=>controller.SetPaused(true,0,new CancellationToken(true)));negatives+=2;
            Check(!controller.IsPaused&&ReferenceEquals(initial,motions.Find(snapshot))&&ReferenceEquals(initialDriver,controller.Driver.State),"setter negative zero driver/publication effects");
            var rows=new List<object>();long clock=0,step=c.GetProperty("Step").GetInt64();SceneWorldTerrainPointTargetReceipt? retained=null;
            for(int i=0;i<c.GetProperty("Updates").GetInt32();i++)
            {
                foreach(var t in c.GetProperty("Targets").EnumerateArray().Where(t=>t.GetProperty("At").GetInt32()==i))
                {var hit=Pick(query,t.GetProperty("X").GetDouble(),t.GetProperty("Z").GetDouble());retained=controller.SubmitTarget(hit,clock);Check(ReferenceEquals(retained.Hit,hit)&&ReferenceEquals(retained.Query,query),"actual target identities");if(controller.IsPaused)Check(controller.State.Mode==SceneWorldTerrainPointTargetMode.Paused,"retarget retains paused intent");}
                foreach(var p in c.GetProperty("Pauses").EnumerateArray().Where(p=>p.GetProperty("At").GetInt32()==i))
                {var before=motions.Find(snapshot)!;var driver=controller.Driver.State;var priorTarget=controller.Target;var priorUpdates=controller.State.AttemptedUpdates;
                 controller.SetPaused(p.GetProperty("Value").GetBoolean(),clock);
                 Check(ReferenceEquals(before,motions.Find(snapshot))&&ReferenceEquals(driver,controller.Driver.State)&&ReferenceEquals(priorTarget,controller.Target)&&priorUpdates==controller.State.AttemptedUpdates,"setter intent only no driver/store/target effects");}
                if(c.TryGetProperty("Correction",out var correction)&&correction.GetProperty("At").GetInt32()==i)
                {var position=correction.GetProperty("Position");Check(motions.PublishAuthoritative(snapshot,AppearanceTransform.Identity with {Translation=new(position[0].GetSingle(),99,position[1].GetSingle())},0,1),"actual paused authoritative correction");}
                var beforeStep=motions.Find(snapshot)!;var state=controller.AdvanceNext(clock,clock+step);var actual=state.DriverResult;
                Check(ReferenceEquals(actual,controller.Driver.State)&&state.CompletedUpdates==i+1&&actual.StepsThisUpdate==1&&actual.TimestampMicroseconds==clock+step,"actual consecutive active fixed tick");
                Check(actual.Transform==motions.Find(snapshot)!.Transform&&ReferenceEquals(retained,controller.Target),"actual grounded publication and retained target");
                if(controller.IsPaused)Check(state.Mode==SceneWorldTerrainPointTargetMode.Paused&&actual.Speed==0&&actual.Transform.Translation.X==beforeStep.Transform.Translation.X&&actual.Transform.Translation.Z==beforeStep.Transform.Translation.Z,"paused tick zero speed/no planar travel including correction");
                if(retained!=null)Check(controller.IsCurrent(retained),"target receipt current while paused/resumed");
                rows.Add(new {Index=i,Mode=state.Mode.ToString(),state.AttemptedUpdates,state.CompletedUpdates,state.SuccessfulTargets,Time=actual.TimestampMicroseconds,Position=V(actual.Transform.Translation),Speed=BitConverter.SingleToUInt32Bits(actual.Speed),X=V(actual.Transform.RowX),Y=V(actual.Transform.RowY),Z=V(actual.Transform.RowZ)});clock+=step;
            }
            var owner=Task.Run(()=>{try{controller.SetPaused(true,clock);return false;}catch(InvalidOperationException){return true;}}).GetAwaiter().GetResult();Check(owner,"setter owner thread");negatives++;
            var last=motions.Find(snapshot)!;var owned=controller.Driver!;controller.Dispose();controller.Dispose();Check(!controller.IsPaused&&controller.Target==null&&!owned.State.Active&&ReferenceEquals(last,motions.Find(snapshot)),"dispose clears paused intent, owns driver only");Reject(()=>controller.SetPaused(true,clock));negatives++;
            cases.Add(new {Name=c.GetProperty("Name").GetString(),Rows=rows});
        }
        using(var world=World(pin))
        {Stage(world,baseline,true,true);using var motions=new SceneWorldObjectMotionStore(world);var snapshot=world.Snapshots().Single();
         using var controller=new SceneWorldTerrainPointTargetDriver(world,motions,42,query,2,.01f,0,1000,maximumUpdates:2);controller.SetPaused(true,0);controller.AdvanceNext(0,1000);controller.AdvanceNext(1000,2000);var row=motions.Find(snapshot);var prior=controller.State;
         Reject(()=>controller.SetPaused(false,2000));Check(controller.IsPaused&&ReferenceEquals(prior,controller.State)&&ReferenceEquals(row,motions.Find(snapshot)),"setter cap no mutation");negatives++;
        }
        using(var world=World(pin))
        {Stage(world,baseline,true,true);using var motions=new SceneWorldObjectMotionStore(world);using var controller=new SceneWorldTerrainPointTargetDriver(world,motions,42,query,2,.01f,0,1000);controller.SetPaused(true,0);world.BeginSelection(42);var state=controller.State;
         Reject(()=>controller.SetPaused(false,0));Check(controller.IsPaused&&ReferenceEquals(state,controller.State),"stale setter no mutation");Check(controller.AdvanceNext(0,1000).Mode==SceneWorldTerrainPointTargetMode.Unavailable,"stale advance inherited unavailable");Reject(()=>controller.SetPaused(false,0));negatives+=2;
        }
        using(var world=World(pin))
        {Stage(world,baseline,true,true);using var motions=new SceneWorldObjectMotionStore(world);var snapshot=world.Snapshots().Single();using var controller=new SceneWorldTerrainPointTargetDriver(world,motions,42,query,2,.01f,0,1000);controller.SetPaused(true,0);
         Check(motions.PublishAuthoritative(snapshot,AppearanceTransform.Identity with {Translation=new(-1,99,-1)},0,1),"bounded outside correction");var row=motions.Find(snapshot);var state=controller.AdvanceNext(0,1000);
         Check(state.Mode==SceneWorldTerrainPointTargetMode.Faulted&&state.CompletedUpdates==0&&ReferenceEquals(row,motions.Find(snapshot)),"paused late ground failure retains external prefix");Reject(()=>controller.SetPaused(false,1000));Check(controller.IsPaused&&ReferenceEquals(state,controller.State),"faulted setter retains intent/state");negatives++;
        }
        using(var world=World(pin))
        {Stage(world,baseline,true,true);using var motions=new SceneWorldObjectMotionStore(world);using var controller=new SceneWorldTerrainPointTargetDriver(world,motions,42,query,2,.01f,0,1000);controller.SetPaused(true,0);var state=controller.State;var driver=controller.Driver!.State;motions.Dispose();
         Reject(()=>controller.SetPaused(false,0));Check(controller.IsPaused&&ReferenceEquals(state,controller.State)&&ReferenceEquals(driver,controller.Driver.State),"closed store setter no intent/driver mutation");negatives++;}
        using(var world=World(pin))
        {Stage(world,baseline,true,true);using var motions=new SceneWorldObjectMotionStore(world);var snapshot=world.Snapshots().Single();using var controller=new SceneWorldTerrainPointTargetDriver(world,motions,42,query,2,.01f,0,1000,maximumTargets:1);
         controller.ValidateTargetAdmission(0);controller.SubmitTarget(Pick(query,1,1),0);var row=motions.Find(snapshot);var state=controller.State;
         Reject(()=>controller.ValidateTargetAdmission(0));Check(ReferenceEquals(row,motions.Find(snapshot))&&ReferenceEquals(state,controller.State),"reduced target cap rejects pre-pick admission without effects");negatives++;
        }
        using(var world=World(pin))
        {long generation=Stage(world,baseline,true,true);using var motions=new SceneWorldObjectMotionStore(world);using var controller=new SceneWorldTerrainPointTargetDriver(world,motions,42,query,2,.01f,0,1000);
         controller.ValidateTargetAdmission(0);Check(world.Accept(generation,SceneObjectLifecycleCodec.EncodeDestroy(new(42,0))),"retire target incarnation");var state=controller.State;
         Reject(()=>controller.ValidateTargetAdmission(0));Check(controller.Target==null&&ReferenceEquals(state,controller.State),"retired incarnation admission no state mutation");negatives++;
        }
        return new {Cases=cases,NativeChecks=checks,NegativeChecks=negatives};
    }
}
