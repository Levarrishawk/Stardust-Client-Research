using System.Numerics;
using Stardust.Core.Assets;

internal static class PreciseDoorActorScenarios
{
    internal static PortalSingleLeafDoorBundle Fixture()
    {
        var bundle=InteriorDoorScenarios.Fixture();
        var floors=bundle.Floors.ToDictionary(p=>p.Key,p=>p.Value with{Vertices=p.Value.Vertices.Select(v=>v with{Y=v.X*.2f}).ToArray()});
        return bundle with{Floors=floors};
    }
    internal static void Run()
    {
        var bundle=Fixture();Vector3 start=new(-.8f,-.16f,0),end=new(.8f,-.16f,0),half=new(.25f,.9f,.25f);var locator=new CellFloorLocator(0,0,start);
        using var leaf=new SingleLeafDoorRuntime(bundle.Leaf.Selection.Style,bundle.Leaf.Extent,bundle.DoorToCell,0,1);
        var bindings=new Dictionary<(int,int),SingleLeafDoorRuntime>{{(0,0),leaf},{(1,0),leaf}};
        var walker=new PortalDoorBoxFloorWalker(bundle.Layout,bundle.Floors,doorLeaves:bindings,allowSeparatingContacts:true);
        var result=walker.Walk(locator,end,half,_=>new(true,true));
        Require(result.ContactKind==PortalActorContactKind.DoorLeaf && result.Movement.Status==CellFloorWalkStatus.PortalBlocked && result.Movement.Locator.CellIndex==0 && Math.Abs(result.Movement.WorldPosition.X+.35)<1e-5 && Math.Abs(result.Movement.WorldPosition.Y+.07)<1e-5,"Closed leaf contact follows source slope");
        leaf.NotifyContact(1,0,0,start+new Vector3(0,.9f,0),true,0);leaf.Advance(0,1,_=>start,true);
        int calls=0;result=walker.Walk(locator,end,half,l=>{calls++;return new(l.SourceCell==0,true);});
        Require(result.Movement.Status==CellFloorWalkStatus.Accepted && result.Movement.Transfers==1 && result.Movement.Locator.CellIndex==1 && Math.Abs(result.Movement.WorldPosition.Y-.16)<1e-5 && calls==1,"Forward target entry does not use reverse denial");
        var reverse=walker.Walk(result.Movement.Locator,start,half,l=>new(l.SourceCell==0,true));
        Require(reverse.Movement.Status==CellFloorWalkStatus.EntryDenied && reverse.Movement.Transfers==0 && reverse.Movement.Locator.CellIndex==1 && reverse.ContactKind==PortalActorContactKind.PermissionBarrier,"Reverse denial stops actor before source-entry seam");
        Require(walker.Walk(locator,end,half,_=>new(null,true)).Movement.Status==CellFloorWalkStatus.PermissionUnknown,"Unknown permission barrier on slope");
        Require(walker.Walk(locator,end,half,_=>new(true,false)).Movement.Status==CellFloorWalkStatus.PortalBlocked,"Current closed permission barrier independent of leaf");
        var captured=walker.Walk(locator,end,half,_=>{leaf.Reset();return new(true,true);});
        Require(captured.Movement.Status==CellFloorWalkStatus.Accepted && walker.Walk(locator,end,half,_=>new(true,true)).Movement.Status==CellFloorWalkStatus.PortalBlocked,"Shared source/target physical snapshot predates callback reset");
        int invalidCalls=0;Require(walker.Walk(locator with{TriangleIndex=-1},end,half,_=>{invalidCalls++;return new(true,true);}).Movement.Status==CellFloorWalkStatus.InvalidStart && invalidCalls==0,"Invalid source never calls external policy");
        HostAccountingScenarios.Reject<InvalidDataException>(()=>walker.Walk(locator,end,new(float.NaN,0,0),_=>throw new Exception("invalid callback")));
        HostAccountingScenarios.Reject<InvalidOperationException>(()=>walker.Walk(locator,end,half,_=>throw new InvalidOperationException("policy")));
        Require(leaf.State.Controller.Fraction==0,"Query does not advance controller after callback failure");
        foreach(var pose in new[]{AppearanceTransform.Identity,new AppearanceTransform(new(0,0,1),Vector3.UnitY,new(-1,0,0),new(30,4,-11))})
        {
            using var actor=new PortalDoorActorSimulation(bundle,locator,pose,usePreciseMovement:true);
            var canonicalInitial=actor.State.Locator;
            for(int i=0;i<30;i++)actor.Tick(.05f,Vector3.Zero,true);
            for(int trip=0;trip<10;trip++)
            {
                for(int i=0;i<20;i++)actor.Tick(.05f,pose.TransformDirection(new(.08f,0,0)),true);
                Require(actor.State.Locator.CellIndex==1 && actor.State.TotalTransfers==trip*2+1 && actor.State.Movement!.Diagnostic is null,"Precise actor forward slope ownership");
                for(int i=0;i<20;i++)actor.Tick(.05f,pose.TransformDirection(new(-.08f,0,0)),true);
                Require(actor.State.Locator.CellIndex==0 && actor.State.TotalTransfers==trip*2+2 && Vector3.Distance(actor.State.Locator.LocalPosition,start)<1e-4,"Precise actor reverse slope round trip");
            }
            actor.Reset();Require(actor.State.Locator==canonicalInitial && actor.State.TotalTransfers==0 && actor.State.Door.RetainedActors==0,"Precise actor reset lifecycle");
            for(int i=0;i<20;i++)actor.Tick(.05f,pose.TransformDirection(new(.08f,0,0)),false);
            var blocked=actor.State.WorldPosition;actor.Tick(.05f,pose.TransformDirection(new(-.08f,0,0)),false);
            Require(actor.State.Locator.CellIndex==0 && Vector3.Distance(actor.State.WorldPosition,blocked)>.07,"Denied slope actor retreats from contact");
        }
        Console.WriteLine("PASS: precise door source/target slope clearance, directional/unknown/barrier policy, shared snapshot/errors and 40 persistent slope transfers over two owner poses");
    }
    private static void Require(bool value,string label){if(!value)throw new Exception("Precise door actor: "+label);}
}
