using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Geometry;
using Stardust.Core.Session;
namespace Stardust.Core.SmokeTests;
public static class TerrainOfflineSessionRestartOwnerScenarios
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
        checks=0;int negatives=0;using var doc=JsonDocument.Parse(File.ReadAllText(input));var fixture=doc.RootElement;var baseline=Convert.FromHexString(fixture.GetProperty("Baseline").GetString()!);var authored=Profile(fixture.GetProperty("Profiles")[0]);string pin=authored.Sources[path].Sha256;var chunks=fixture.GetProperty("Terrain").EnumerateArray().Select(Make).ToArray();var surface=TerrainModernPatchSetSurfaceQuery.Build(TerrainModernHeightPatchSet.Build(chunks,TerrainModernPatchEdgePolicy.RequireBitIdenticalActiveSharedEdges,TerrainModernPatchNormalPolicy.RequireBitIdenticalSharedNormals));var query=TerrainModernPatchSetRayQuery.Build(surface);
        using var world=World(pin);using var motions=new SceneWorldObjectMotionStore(world);var rows=new List<object>();var incarnations=new HashSet<SceneObjectIncarnation>();
        TerrainConfiguredOfflineSession MakeOwner(bool fault=false,long clock=0)
        {Stage(world,baseline,true,true);var s=world.Snapshots().Single();motions.PublishLocal(s,AppearanceTransform.Identity with {Translation=fault?new(15.95f,0,4):new(7.8f,0,7.8f)},0);return new(world,motions,42,query,TerrainOfflinePlaygroundSettings.Create(maximumUpdates:3,maximumClickAttempts:1,holdMissingDestination:!fault),clock,.25f);}
        using(var restart=new TerrainOfflineSessionRestartOwner(3))
        {
            TerrainConfiguredOfflineSession? previous=null;TerrainOfflineSessionCompletion? previousReport=null;
            for(int run=0;run<3;run++)
            {
                bool fault=run==1;long clock=run*100000;int calls=0;
                TerrainConfiguredOfflineSession Factory(){calls++;if(previous is not null)Check(previous.IsDisposed&&previous.PointDriver.State.Mode==SceneWorldTerrainPointTargetMode.Disposed,"old children retired BEFORE next factory");return MakeOwner(fault,clock);}
                var active=run==0?restart.Start(Factory):restart.Restart(Factory);Check(calls==1&&ReferenceEquals(active,restart.Active)&&restart.Generation==run+1&&ReferenceEquals(previousReport,restart.LastCompletion),"one factory successful generation/history prefix");var snapshot=world.Snapshots().Single();Check(incarnations.Add(snapshot.Object.Incarnation),"fresh actual incarnation each run");var point=active.PointDriver;var controller=active.Controller;point.SetManualInput(Vector2.UnitX,clock);var before=controller.State;var row=motions.Find(snapshot);Reject(()=>restart.Complete());negatives++;Check(ReferenceEquals(active,restart.Active)&&ReferenceEquals(before,controller.State)&&ReferenceEquals(row,motions.Find(snapshot))&&ReferenceEquals(previousReport,restart.LastCompletion),"incomplete complete preserves active/history/row");
                int forbiddenCalls=0;Reject(()=>restart.Restart(()=>{forbiddenCalls++;return active;}));negatives++;Reject(()=>restart.Start(()=>{forbiddenCalls++;return active;}));negatives++;Check(forbiddenCalls==0,"active phase rejects BEFORE factory");
                var updates=new List<object>();for(int i=0;i<3;i++)
                {
                    if(fault&&i==1){Reject(()=>controller.AdvanceNext(clock,clock+10000));negatives++;break;}
                    var state=controller.AdvanceNext(clock,clock+10000);var actual=state.DriverResult.DriverResult;updates.Add(new {Index=i,Time=actual.TimestampMicroseconds,Position=V(actual.Transform.Translation),Speed=BitConverter.SingleToUInt32Bits(actual.Speed),Mode=state.DriverResult.Mode.ToString(),Completed=state.CompletedUpdates,Attempted=state.DriverResult.AttemptedUpdates,Stopped=state.Stopped});clock+=10000;
                }
                var actualRow=motions.Find(snapshot);var history=controller.State;var result=restart.Complete();Check(ReferenceEquals(result,restart.LastCompletion)&&restart.Active is null&&restart.Generation==run+1&&active.IsDisposed&&point.State.Mode==SceneWorldTerrainPointTargetMode.Disposed&&ReferenceEquals(history,controller.State)&&ReferenceEquals(actualRow,motions.Find(snapshot)),"complete captures then retires actual owners, primitive history/borrowed row survives");
                rows.Add(new {Generation=restart.Generation,SceneGeneration=world.Generation,Updates=updates,Completion=result});previous=active;previousReport=result;
                if(run==0){int n=0;Reject(()=>restart.Restart(()=>{n++;throw new InvalidDataException("fixture factory failure");}));negatives++;Check(n==1&&restart.Active is null&&restart.Generation==1&&ReferenceEquals(result,restart.LastCompletion),"failed restart keeps generation/completion no active");Reject(()=>restart.Restart(()=>null!));negatives++;Reject(()=>restart.Restart(()=>previous));negatives++;Check(restart.Generation==1&&ReferenceEquals(result,restart.LastCompletion)&&restart.CleanupFaultType is null,"null/stale disposed factory history preserved");}
            }
            int capped=0;Reject(()=>restart.Restart(()=>{capped++;return MakeOwner();}));negatives++;Check(capped==0&&restart.Generation==3,"successful run cap no factory/retry");
            foreach(var action in new Action[]{()=>{_=restart.Active;},()=>{_=restart.Generation;},()=>{_=restart.LastCompletion;},()=>{_=restart.IsDisposed;},()=>{_=restart.CleanupFaultType;},()=>restart.Dispose()}){Check(Task.Run(()=>{try{action();return false;}catch(InvalidOperationException){return true;}}).GetAwaiter().GetResult(),"restart owner/thread reads/Dispose");negatives++;}
            var last=restart.LastCompletion;restart.Dispose();restart.Dispose();Check(restart.IsDisposed&&restart.Active is null&&restart.Generation==3&&ReferenceEquals(last,restart.LastCompletion)&&query.Source.Query(1,1) is not null,"restart Dispose retains history/borrowed query");Reject(()=>restart.Complete());negatives++;Reject(()=>restart.Start(()=>MakeOwner()));negatives++;Reject(()=>restart.Restart(()=>MakeOwner()));negatives++;
        }
        foreach(int cap in new[]{0,33}){Reject(()=>new TerrainOfflineSessionRestartOwner(cap));negatives++;}
        using(var maximum=new TerrainOfflineSessionRestartOwner(32)){Check(maximum.Generation==0&&maximum.Active is null,"maximum32 run budget admitted without starting work");}
        using(var restart=new TerrainOfflineSessionRestartOwner())
        {
            int calls=0;Reject(()=>restart.Restart(()=>{calls++;return MakeOwner();}));negatives++;Reject(()=>restart.Complete());negatives++;Reject(()=>restart.Start(null!));negatives++;Check(calls==0&&restart.Generation==0,"initial invalid phase no factory");
            Reject(()=>restart.Start(()=>throw new OperationCanceledException()));negatives++;Reject(()=>restart.Start(()=>null!));negatives++;Check(restart.Active is null&&restart.Generation==0,"failed initial factory permits explicit retry");
            TerrainConfiguredOfflineSession? rejected=null;Reject(()=>restart.Start(()=>{rejected=MakeOwner();rejected.Controller.AdvanceNext(0,10000);return rejected;}));negatives++;Check(rejected!.IsDisposed&&rejected.PointDriver.State.Mode==SceneWorldTerrainPointTargetMode.Disposed&&restart.Generation==0&&restart.Active is null,"advanced returned prefix retired no successful generation");
            TerrainConfiguredOfflineSession? deadController=null;Reject(()=>restart.Start(()=>{deadController=MakeOwner();deadController.Controller.Dispose();return deadController;}));negatives++;Check(deadController!.IsDisposed&&restart.Generation==0&&restart.Active is null,"externally disposed controller admission fails and returned point prefix retires");
            TerrainConfiguredOfflineSession? stale=null;Reject(()=>restart.Start(()=>{stale=MakeOwner();world.BeginSelection(42);return stale;}));negatives++;Check(stale!.IsDisposed&&restart.Generation==0&&restart.Active is null,"stale actual world/incarnation factory admission fails and prefix retires");
            TerrainConfiguredOfflineSession? poisoned=null;Reject(()=>restart.Start(()=>{try{_=restart.Active;}catch(InvalidOperationException){}poisoned=MakeOwner();return poisoned;}));negatives++;Check(poisoned!.IsDisposed&&restart.Active is null&&restart.Generation==0&&restart.CleanupFaultType is null,"caught getter reentry poisons factory admission, clean retirement remains retryable");
            Reject(()=>restart.Start(()=>{try{restart.Dispose();}catch(InvalidOperationException){}return MakeOwner();}));negatives++;Check(!restart.IsDisposed&&restart.Generation==0,"caught Dispose reentry does not publish or dispose parent");
            var active=restart.Start(()=>MakeOwner());active.PointDriver.SetManualInput(Vector2.UnitX,0);active.Controller.AdvanceNext(0,10000);var current=motions.Find(world.Snapshots().Single());restart.Dispose();Check(active.IsDisposed&&ReferenceEquals(current,motions.Find(world.Snapshots().Single()))&&restart.LastCompletion is null&&restart.Generation==1,"Dispose incomplete owner retires without inventing completion or final tick");
        }
        TerrainOfflineSessionCompletion requested;
        using(var restart=new TerrainOfflineSessionRestartOwner(1))
        {var active=restart.Start(()=>MakeOwner());active.PointDriver.SetManualInput(Vector2.UnitX,0);active.Controller.AdvanceNext(0,10000);active.Controller.RequestStop();active.Controller.AdvanceNext(10000,20000);requested=restart.Complete(true);Check(requested.Reason==TerrainOfflineCompletionReason.FinishRequested&&requested.FinishRequested&&requested.CompletedUpdates==2&&active.IsDisposed,"Complete forwards explicit finish flag and retires actual early stop");}
        using(var ready=new ManualResetEventSlim())using(var release=new ManualResetEventSlim())
        {
            TerrainConfiguredOfflineSession? foreign=null;Exception? threadError=null;bool foreignRetired=false;
            var thread=new Thread(()=>{try{using var w=World(pin);Stage(w,baseline,true,true);using var m=new SceneWorldObjectMotionStore(w);using var owner=new TerrainConfiguredOfflineSession(w,m,42,query,TerrainOfflinePlaygroundSettings.Create(maximumUpdates:3),0);foreign=owner;ready.Set();if(!release.Wait(TimeSpan.FromSeconds(30)))throw new TimeoutException("fixture cleanup release");owner.Dispose();foreignRetired=owner.IsDisposed;}catch(Exception e){threadError=e;ready.Set();}});
            thread.Start();try
            {Check(ready.Wait(TimeSpan.FromSeconds(30))&&threadError is null&&foreign is not null,"foreign owner fixture ready");using var restart=new TerrainOfflineSessionRestartOwner();Reject(()=>restart.Start(()=>foreign!));negatives++;Check(restart.Active is null&&restart.Generation==0&&restart.LastCompletion is null&&restart.CleanupFaultType==nameof(InvalidOperationException),"actual wrong-owner cleanup failure retains terminal honest prefix");int retry=0;Reject(()=>restart.Start(()=>{retry++;return MakeOwner();}));negatives++;Check(retry==0,"cleanup failure blocks every later factory");restart.Dispose();Check(restart.IsDisposed&&restart.CleanupFaultType==nameof(InvalidOperationException),"failed retirement not silently repaired on Dispose");}
            finally{release.Set();Check(thread.Join(TimeSpan.FromSeconds(30)),"foreign fixture joined");}
            Check(threadError is null&&foreignRetired,"foreign fixture explicitly retired on its original owner thread");
        }
        return new {Runs=rows,Requested=requested,NativeChecks=checks,NegativeChecks=negatives};
    }
}
