using System.Numerics;
using Stardust.Core.Assets;

internal static class InteriorSceneSchedulingScenarios
{
    private static readonly (long Time,float Speed,bool? Allowed)[] Changes =
    { (35_000,1.6f,true),(4_055_000,-1.6f,true),(8_105_000,0,null) };
    private static InteriorDoorActorScene Create()
    { var f=InteriorDoorSceneScenarios.Fixture();return new(f.Layout,f.Floors,f.Bindings,new(0,0,new(-.8f,0,0))); }
    private static FixedStepActorHost<InteriorDoorActorSceneState> Host(InteriorDoorActorScene scene) =>
        new(()=>scene.State,(seconds,delta,entry)=>scene.Tick(seconds,delta,_=>new(entry,true)),scene.Reset);
    private static bool Same(InteriorDoorActorSceneState a,InteriorDoorActorSceneState b) =>
        a.Locator==b.Locator&&a.WorldPosition==b.WorldPosition&&a.Frame==b.Frame&&a.TotalTransfers==b.TotalTransfers&&a.Movement==b.Movement&&
        a.Doors.OrderBy(p=>p.Key).SequenceEqual(b.Doors.OrderBy(p=>p.Key));
    internal static void Run()
    {
        InteriorDoorActorSceneState? expected=null;
        foreach(int[] partitions in new[]{new[]{10_000},new[]{50_000},new[]{7_000,11_000,23_000,41_000}})
        {
            using var scene=Create();var timeline=new PortalActorInputTimeline<InteriorDoorActorSceneState>(Host(scene),0,new(Vector3.Zero,true));
            foreach(var change in Changes)timeline.Submit(new(change.Time,new(new(change.Speed,0,0),change.Allowed)));
            long now=0;int render=0;
            while(now<8_400_000)timeline.AdvanceTo(now=Math.Min(8_400_000,now+partitions[render++%partitions.Length]));
            Require(scene.State.Frame==420&&timeline.State.DroppedMicroseconds==0&&timeline.PendingTransitions==0,"Typed scene tick/queue accounting");
            expected??=scene.State;Require(Same(scene.State,expected),"All controllers and ownership invariant across render partitions");
        }
        using var direct=Create();
        for(long time=0;time<8_400_000;time+=20_000)
        {
            var change=Changes.LastOrDefault(c=>c.Time<=time);direct.Tick(.02f,new(change.Speed*.02f,0,0),_=>new(change.Time==0?true:change.Allowed,true));
        }
        Require(Same(direct.State,expected!),"Typed timeline matches independently indexed direct scene ticks");
        using var stalled=Create();var clock=Host(stalled);var moving=new PortalActorInput(new(1.6f,0,0),true);
        clock.AdvanceTo(0,moving);clock.AdvanceTo(2_000_007,moving);
        Require(clock.State.StepsThisUpdate==8&&stalled.State.Frame==8&&clock.State.PendingMicroseconds==7&&clock.State.DroppedMicroseconds==1_840_000,"Scene stall bounded without replay");
        clock.Reset(0);clock.AdvanceTo(10_000,moving);clock.AdvanceTo(20_000,moving,false);clock.AdvanceTo(1_000_000,moving,false);clock.AdvanceTo(1_020_000,moving);
        Require(stalled.State.Frame==0&&clock.State.SuspendedMicroseconds==1_020_000&&clock.State.PendingMicroseconds==0,"Scene pause/resume discards suspended history");
        using var failing=Create();int calls=0;
        var fault=new FixedStepActorHost<InteriorDoorActorSceneState>(()=>failing.State,(s,d,a)=>failing.Tick(s,d,_=>
            {if(++calls==9)throw new InvalidOperationException("Injected scene callback failure");return new(a,true);}),failing.Reset);
        fault.AdvanceTo(0,moving);fault.AdvanceTo(100_000,moving);
        Require(fault.State.FaultType==nameof(InvalidOperationException)&&fault.State.StepsThisUpdate==2&&failing.State.Frame==2&&fault.State.DroppedMicroseconds==60_000,"Partial completed ticks retained; failed tick latched without mutation");
        int prior=calls;fault.AdvanceTo(200_000,moving);Require(calls==prior&&failing.State.Frame==2,"Scene fault never retries implicitly");
        fault.Reset(0);Require(fault.State.FaultType is null&&failing.State.Frame==0,"Explicit typed reset starts fresh epoch");
        failing.Dispose();fault.AdvanceTo(20_000,moving);Require(fault.State.FaultType==nameof(ObjectDisposedException),"Disposed borrowed scene faults explicitly");
        using var reentrant=Create();FixedStepActorHost<InteriorDoorActorSceneState>? recursive=null;
        recursive=new(()=>reentrant.State,(s,d,a)=>reentrant.Tick(s,d,_=>{recursive!.Reset(0);return new(a,true);}),reentrant.Reset);
        recursive.AdvanceTo(0,moving);recursive.AdvanceTo(20_000,moving);
        Require(recursive.State.FaultType==nameof(InvalidOperationException)&&reentrant.State.Frame==0,"Scheduler reentrant reset fails before actor mutation");
        Console.WriteLine("PASS: shared typed scene scheduler/timeline, three render partitions and direct 420-tick oracle, stall/pause accounting, partial callback fault latch/reset/dispose/reentrancy");
    }
    private static void Require(bool value,string text){if(!value)throw new Exception("Scene schedule: "+text);}
}
