using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

internal static class SceneWorldLocalMotionScenarios
{
    private static int checks;
    private static void Check(bool value, string why) { checks++; if (!value) throw new Exception(why); }
    private static void Reject<T>(Action action) where T:Exception
    { try { action(); } catch (T) { checks++; return; } throw new Exception("Expected "+typeof(T).Name); }
    internal static void Run(IReadOnlyDictionary<string,string> peer, string exportPath)
    {
        checks=0;
        var catalog=ObjectTemplateCatalogProfile.Decode(File.ReadAllBytes("poc/preview-profiles/scene-object-catalog.json"));
        using var bankFile=File.OpenRead("poc/preview-profiles/human-locomotion.json");var bank=SkeletalAnimationBankProfile.Load(bankFile);
        var idle=new LogicalAnimationClipSelection("appearance/lat/all_m.lat","emt_standing_idle_ambient_1_ag","798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B");
        var options=new Dictionary<SceneObjectResourceRecipe,SceneObjectResourceOptions>{{SceneObjectResourceRecipe.CreatureDetail0,new SceneCreatureResourceOptions(idle,bank,true)},{SceneObjectResourceRecipe.StaticAppearance,new SceneStaticResourceOptions(SceneStaticMaterialPolicy.RequireComplete,null)}};
        var policy=new SceneWorldReplicationPolicy(SceneWorldReplicationMode.CreatureView3MixedComponents,new(1024,64,64,16,512),16,1024,new(1024,32,24,64,64,16),CreatureWoundsApplicationPolicy.LegacyBounded,new(1024,32,64,64,16),new(16,16));
        int loads=0;
        SceneWorldCoordinator World()=>new(catalog,options,(q,t)=>{loads++;return Task.FromException<SceneObjectResources>(new Exception("No resource load expected"));},SceneCreateZeroQuaternionPolicy.Reject,4,2,32,8192,16,4096,1024,policy);
        byte[] Bytes(string name)=>Convert.FromHexString(peer[name]);ulong humanId=SceneObjectLifecycleCodec.DecodeCreate(Bytes("create")).ObjectId,staticId=SceneObjectLifecycleCodec.DecodeCreate(Bytes("staticCreate")).ObjectId;
        long Stage(SceneWorldCoordinator world){long g=world.BeginSelection(humanId).Generation;foreach(var name in new[]{"start","create","staticCreate","baseline","staticBaseline","end","staticEnd"})world.Accept(g,Bytes(name));world.CompleteMinimumSceneBarrier(g,true);return g;}
        SceneWorldObjectSnapshot Snapshot(SceneWorldCoordinator world,ulong id)=>world.Snapshots().Single(s=>s.Object.Handle.ObjectId==id);
        PortalActorInput Input(float x,float z,bool? entry=null)=>new(new(x,0,z),entry);
        var pose=AppearanceTransform.Identity with{Translation=new(10,20,30)};
        var transitions=new[]{new PortalActorInputTransition(0,Input(2,0)),new(15500,Input(0,4)),new(65001,Input(-3,0)),new(100003,Input(3,0)),new(100003,Input(3,4)),new(211111,Input(0,0)),new(271234,Input(0,-2)),new(350000,Input(0,0))};
        var finals=new List<SceneWorldLocalMotionDriverState>();
        foreach(int partition in new[]{1000,27000,31000})
        using(var world=World())
        {
            Stage(world);using var motions=new SceneWorldObjectMotionStore(world);var snapshot=Snapshot(world,humanId);byte[] raw=SceneObjectLifecycleCodec.EncodeCreate(snapshot.Object.Create);using var driver=new SceneWorldLocalMotionDriver(world,motions,humanId,0,10000,8);foreach(var transition in transitions)driver.Submit(transition.TimestampMicroseconds,transition.Input);
            for(long time=partition;time<400000;time+=partition)driver.Advance(time);var final=driver.Advance(400000);finals.Add(final);
            double x=snapshot.Placement.Transform.Translation.X,z=snapshot.Placement.Transform.Translation.Z;int index=0;Vector3 velocity=Vector3.Zero,lastFacing=Vector3.Zero;int ticks=0;
            for(long start=0;start<400000;start+=10000){while(index<transitions.Length&&transitions[index].TimestampMicroseconds<=start)velocity=transitions[index++].Input.WorldVelocity;x+=velocity.X*.01;z+=velocity.Z*.01;if(velocity!=Vector3.Zero)lastFacing=Vector3.Normalize(velocity);ticks++;}
            Check(Math.Abs(final.Transform.Translation.X-x)<.001&&Math.Abs(final.Transform.Translation.Z-z)<.001&&final.Transform.Translation.Y==snapshot.Placement.Transform.Translation.Y,"Independent tick-indexed planar integration "+partition);
            Check(final.TimestampMicroseconds==400000&&final.DroppedMicroseconds==0&&final.SuspendedMicroseconds==0&&final.Speed==0&&final.Active&&final.FaultType is null,"Bounded render partitions without dropped work");Check(final.Transform.TransformDirection(Vector3.UnitZ)==lastFacing,"Native yaw follows velocity and holds on zero");Check(motions.Find(Snapshot(world,humanId))!.Revision==(ulong)ticks+1,"One local publication per tick plus constructor");Check(raw.SequenceEqual(SceneObjectLifecycleCodec.EncodeCreate(Snapshot(world,humanId).Object.Create)),"Initial raw create immutable through movement");
        }
        Check(finals.All(s=>s.Transform==finals[0].Transform&&s.Speed==finals[0].Speed),"Render partitions bit-identical after same tick-start input history");
        using(var world=World())
        {
            long g=Stage(world);using var motions=new SceneWorldObjectMotionStore(world);using var driver=new SceneWorldLocalMotionDriver(world,motions,humanId,0,10000);driver.Submit(0,Input(1,0));var partial=driver.Advance(15000);var before=motions.Find(Snapshot(world,humanId))!;var corrected=pose with{Translation=new(100,9,200)};Check(motions.PublishAuthoritative(Snapshot(world,humanId),corrected,7,9),"Current authoritative correction accepted");var correction=motions.Find(Snapshot(world,humanId))!;var observed=driver.Advance(15000);Check(observed.Transform==corrected&&observed.Speed==7&&observed.StepsThisUpdate==0&&ReferenceEquals(correction,motions.Find(Snapshot(world,humanId))),"Immediate correction without a tick/local overwrite");var resumed=driver.Advance(20000);Check(Math.Abs(resumed.Transform.Translation.X-100.01f)<1e-5&&resumed.Transform.Translation.Y==9&&resumed.Transform.Translation.Z==200&&resumed.Speed==1,"Next local step advances from correction using retained fractional clock");Check(motions.Find(Snapshot(world,humanId))!.Revision>before.Revision&&!motions.Find(Snapshot(world,humanId))!.Authoritative,"Correction then exact local publication");
            world.Accept(g,Bytes("composite"));world.RestartResource(g,humanId);Check(driver.Advance(30000).Active&&driver.State.Transform.Translation.Y==9&&Snapshot(world,humanId).Replication!.Scalars.Height==1.25f,"Height/resource restart independent of native local pose");var initial=Snapshot(world,humanId).Placement.Transform;var revision=motions.Find(Snapshot(world,humanId))!.Revision;driver.Submit(35000,Input(0,5));driver.Reset(0);Check(driver.State.Transform==initial&&driver.State.Speed==0&&driver.State.TimestampMicroseconds==0&&motions.Find(Snapshot(world,humanId))!.Revision>revision,"Reset initial native pose, cleared clock/history and monotonic source revision");Check(driver.Advance(40000).Transform==initial&&driver.State.Speed==0,"Reset discards future queued input without replay");
        }
        using(var world=World())
        {
            Stage(world);using var motions=new SceneWorldObjectMotionStore(world);using var driver=new SceneWorldLocalMotionDriver(world,motions,humanId,0,10000,2);var initial=driver.State.Transform;driver.Submit(0,Input(1,0));driver.Submit(45000,Input(0,2));var stalled=driver.Advance(200007);Check(stalled.StepsThisUpdate==2&&stalled.DroppedMicroseconds==180000&&Math.Abs(stalled.Transform.Translation.X-initial.Translation.X-.02f)<1e-5,"Bounded stall runs two ticks and drops whole backlog");var next=driver.Advance(210000);Check(next.StepsThisUpdate==1&&Math.Abs(next.Transform.Translation.Z-initial.Translation.Z-.02f)<1e-5&&next.Speed==2,"Skipped input history retained as latest only, fractional carry preserved");
        }
        using(var world=World())
        {
            Stage(world);using var motions=new SceneWorldObjectMotionStore(world);using var driver=new SceneWorldLocalMotionDriver(world,motions,humanId,0,10000);driver.Submit(0,Input(2,0));driver.Submit(15500,Input(0,4));var first=driver.Advance(15000);driver.Advance(20000,false);driver.Submit(25000,Input(-3,0));var paused=driver.Advance(100000,false);var resumed=driver.Advance(110000);Check(first.Transform==paused.Transform&&paused.Transform==resumed.Transform&&paused.SuspendedMicroseconds==90000&&resumed.SuspendedMicroseconds==100000&&resumed.StepsThisUpdate==0,"Pause/resume discards pending and elapsed time");var live=driver.Advance(120000);Check(Math.Abs(live.Transform.Translation.X-first.Transform.Translation.X+.03f)<1e-5&&live.Speed==3&&live.Transform.TransformDirection(Vector3.UnitZ)==-Vector3.UnitX,"Latest suspended input adopts without replay");
        }
        using(var world=World())
        {
            Stage(world);using var motions=new SceneWorldObjectMotionStore(world);using var driver=new SceneWorldLocalMotionDriver(world,motions,humanId,0,10000);var before=driver.State;var publication=motions.Find(Snapshot(world,humanId))!;
            foreach(var input in new[]{new PortalActorInput(new(1,.1f,0),null),Input(11,0),Input(float.NaN,0),Input(0,float.PositiveInfinity)})Reject<ArgumentOutOfRangeException>(()=>driver.Submit(0,input));Reject<ArgumentOutOfRangeException>(()=>driver.Submit(-1,Input(1,0)));Reject<ArgumentOutOfRangeException>(()=>driver.Submit(60000001,Input(1,0)));Check(driver.State==before&&ReferenceEquals(publication,motions.Find(Snapshot(world,humanId))),"Invalid admission preserves pose/clock/publication");driver.Submit(15000,Input(1,0));Reject<ArgumentOutOfRangeException>(()=>driver.Submit(14000,Input(0,1)));driver.Advance(20000);var after=driver.State;Reject<ArgumentOutOfRangeException>(()=>driver.Submit(19999,Input(0,1)));Reject<ArgumentOutOfRangeException>(()=>driver.Advance(19999));Reject<ArgumentOutOfRangeException>(()=>driver.Reset(-1));Check(driver.State==after&&after.FaultType is null,"Invalid ordered/time/reset admission nonsticky and atomic");
            driver.Reset(0);for(int i=0;i<1024;i++)driver.Submit(0,Input(1,0));Reject<InvalidOperationException>(()=>driver.Submit(0,Input(0,1)));Check(driver.Advance(10000).Speed==1,"Input capacity rejects only new transition");driver.Submit(10000,Input(0,0));Check(driver.Advance(20000).Speed==0,"Capacity frees after tick-start consumption");
            Exception? foreign=null;var thread=new Thread(()=>{try{driver.Advance(30000);}catch(Exception e){foreign=e;}});thread.Start();thread.Join();Check(foreign is InvalidOperationException&&driver.State.TimestampMicroseconds==20000,"Owner thread rejection preserves driver clock");
        }
        using(var world=World())
        {
            Stage(world);using var motions=new SceneWorldObjectMotionStore(world,1);using var driver=new SceneWorldLocalMotionDriver(world,motions,humanId,0,10000);driver.Submit(0,Input(1,0));motions.Clear();motions.PublishLocal(Snapshot(world,staticId),pose,0);var failed=driver.Advance(10000);Check(failed.FaultType==nameof(InvalidDataException)&&!failed.Active&&failed.StepsThisUpdate==0,"Publication capacity creates sticky tick fault");Reject<InvalidOperationException>(()=>driver.Submit(10000,Input(0,0)));var stopped=driver.Advance(20000);Check(stopped.Transform==failed.Transform&&!stopped.Active&&stopped.StepsThisUpdate==0,"Faulted movement not implicitly replayed");Reject<InvalidDataException>(()=>driver.Reset(20000));motions.Clear();driver.Reset(30000);driver.Submit(30000,Input(1,0));Check(driver.Advance(40000).Active&&driver.State.FaultType is null,"Explicit reset recovers still-current incarnation after fault");
        }
        foreach(var change in new[]{"recreate","generation","failure","close"})
        using(var world=World())
        {
            long g=Stage(world);using var motions=new SceneWorldObjectMotionStore(world);using var driver=new SceneWorldLocalMotionDriver(world,motions,humanId,0,10000);driver.Submit(0,Input(1,0));var initial=driver.State;
            if(change=="recreate"){world.Accept(g,Bytes("destroy"));world.Accept(g,Bytes("create"));world.Accept(g,Bytes("baseline"));world.Accept(g,Bytes("end"));}else if(change=="generation")Stage(world);else if(change=="failure")Reject<InvalidDataException>(()=>world.Accept(g,new byte[]{1}));else world.Close();
            var stopped=driver.Advance(10000);Check(!stopped.Active&&stopped.FaultType is not null&&stopped.Transform==initial.Transform,"Old incarnation stops "+change);Reject<InvalidOperationException>(()=>driver.Reset(10000));if(change is "recreate" or "generation"){using var replacement=new SceneWorldLocalMotionDriver(world,motions,humanId,10000,10000);var latest=motions.Find(Snapshot(world,humanId))!;driver.Dispose();Check(motions.IsCurrent(latest)&&replacement.State.Active,"Old dispose does not clear replacement motion");}
        }
        using(var world=World())
        using(var foreign=World())
        {
            Stage(world);Stage(foreign);using var motions=new SceneWorldObjectMotionStore(world);using var other=new SceneWorldObjectMotionStore(foreign);Reject<ArgumentException>(()=>new SceneWorldLocalMotionDriver(world,other,humanId));Reject<InvalidDataException>(()=>new SceneWorldLocalMotionDriver(world,motions,staticId));Reject<InvalidDataException>(()=>new SceneWorldLocalMotionDriver(world,motions,123));foreach(long step in new[]{999L,100001L,long.MaxValue})Reject<ArgumentOutOfRangeException>(()=>new SceneWorldLocalMotionDriver(world,motions,humanId,0,step));Reject<ArgumentOutOfRangeException>(()=>new SceneWorldLocalMotionDriver(world,motions,humanId,-1));Reject<ArgumentOutOfRangeException>(()=>new SceneWorldLocalMotionDriver(world,motions,humanId,0,10000,0));Check(motions.Find(Snapshot(world,humanId)) is null,"Invalid construction does not publish into supplied motion store");using var driver=new SceneWorldLocalMotionDriver(world,motions,humanId);motions.PublishLocal(Snapshot(world,staticId),pose,1);var human=motions.Find(Snapshot(world,humanId))!;var terminal=motions.Find(Snapshot(world,staticId))!;driver.Dispose();driver.Dispose();Check(motions.IsCurrent(human)&&motions.IsCurrent(terminal)&&world.State!=ZoneSceneTransitionState.Closed&&!driver.State.Active,"Dispose releases own roots, never borrowed rows/world");Reject<ObjectDisposedException>(()=>driver.Submit(0,Input(0,0)));Reject<ObjectDisposedException>(()=>driver.Advance(0));
        }
        using(var world=World())
        {
            Stage(world);using var motions=new SceneWorldObjectMotionStore(world);Exception? error=null;var foreign=new Thread(()=>{try{using var driver=new SceneWorldLocalMotionDriver(world,motions,humanId);}catch(Exception e){error=e;}});foreign.Start();foreign.Join();Check(error is InvalidOperationException&&motions.Find(Snapshot(world,humanId)) is null,"Constructor world owner affinity before publication/retention");
        }
        var correctedFinals=new List<SceneWorldLocalMotionDriverState>();
        foreach(int partition in new[]{1000,27000,31000})
        using(var world=World())
        {
            Stage(world);using var motions=new SceneWorldObjectMotionStore(world);using var driver=new SceneWorldLocalMotionDriver(world,motions,humanId,0,10000);driver.Submit(0,Input(1,0));driver.Submit(45001,Input(0,2));for(long t=partition;t<40000;t+=partition)driver.Advance(t);driver.Advance(40000);motions.PublishAuthoritative(Snapshot(world,humanId),pose,7,5);var observed=driver.Advance(40000);Check(observed.Transform==pose&&observed.Speed==7&&observed.StepsThisUpdate==0,"Correction observed at same clock boundary "+partition);for(long t=40000+partition;t<100000;t+=partition)driver.Advance(t);var final=driver.Advance(100000);correctedFinals.Add(final);Check(Math.Abs(final.Transform.Translation.X-10.01f)<1e-5&&Math.Abs(final.Transform.Translation.Z-30.1f)<1e-5&&final.Speed==2&&final.DroppedMicroseconds==0,"Independent post-correction tick trajectory "+partition);
        }
        Check(correctedFinals.All(s=>s.Transform==correctedFinals[0].Transform&&s.Speed==2),"Correction at common observation boundary preserves render-partition determinism");
        using(var world=World())
        {
            Stage(world);using var motions=new SceneWorldObjectMotionStore(world);using var driver=new SceneWorldLocalMotionDriver(world,motions,humanId,0,10000);driver.Submit(0,Input(1,0));driver.Advance(15000);driver.Advance(20000,false);driver.Submit(50000,Input(0,2));motions.PublishAuthoritative(Snapshot(world,humanId),pose,8,8);var paused=driver.Advance(100000,false);Check(paused.Transform==pose&&paused.Speed==8&&paused.StepsThisUpdate==0,"Paused authoritative correction adopted without movement");Check(driver.Advance(110000).Transform==pose,"Resume boundary does not replay suspended time");Check(Math.Abs(driver.Advance(120000).Transform.Translation.Z-30.02f)<1e-5&&driver.State.Speed==2,"Latest suspended input moves from corrected pose");
        }
        using(var world=World())
        {
            Stage(world);using var motions=new SceneWorldObjectMotionStore(world);var snapshot=Snapshot(world,humanId);motions.PublishAuthoritative(snapshot,pose,7,5);using var driver=new SceneWorldLocalMotionDriver(world,motions,humanId,100000,10000);Check(driver.State.Transform==pose&&driver.State.Speed==7&&driver.State.TimestampMicroseconds==100000,"Existing authoritative pose adopted at construction");Check(!motions.PublishAuthoritative(snapshot,pose,7,5),"Constructor local publication retains source private authoritative freshness");driver.Advance(110000);Check(driver.State.Speed==0&&driver.State.Transform==pose,"Zero input holds corrected facing and position");driver.Reset(0);Check(driver.State.Transform==snapshot.Placement.Transform,"Explicit reset uses immutable create placement, not later correction");
        }
        using(var world=World())
        {
            long g=world.BeginSelection(humanId).Generation;world.Accept(g,Bytes("start"));world.Accept(g,Bytes("create"));world.Accept(g,Bytes("baseline"));using var motions=new SceneWorldObjectMotionStore(world);Reject<InvalidDataException>(()=>new SceneWorldLocalMotionDriver(world,motions,humanId));world.Accept(g,Bytes("end"));Reject<InvalidDataException>(()=>new SceneWorldLocalMotionDriver(world,motions,humanId));world.CompleteMinimumSceneBarrier(g,true);using var driver=new SceneWorldLocalMotionDriver(world,motions,humanId,0,10000);driver.Submit(0,Input(10,10,false));var moved=driver.Advance(10000);Check(moved.Active&&moved.Speed==MathF.Sqrt(200)&&motions.IsCurrent(motions.Find(Snapshot(world,humanId))!),"Finalized active gate and maximum diagonal proper yaw, no portal-permission semantics");
        }
        Check(loads==0,"Local driver fixtures perform no resource loads");File.WriteAllText(exportPath,JsonSerializer.Serialize(new{Checks=checks,RenderPartitions=3,ResourceLoads=loads,AssetReads=0,Result="PASS"},new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine("R18_LOCAL_MOTION_PASS checks="+checks);
    }
}
