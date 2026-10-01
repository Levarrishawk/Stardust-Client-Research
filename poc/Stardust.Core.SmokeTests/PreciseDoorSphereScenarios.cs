using System.Numerics;
using Stardust.Core.Assets;

internal static class PreciseDoorSphereScenarios
{
    internal static void Run()
    {
        var bundle=PreciseDoorActorScenarios.Fixture();Vector3 start=new(-.8f,-.16f,0),end=new(.8f,-.16f,0),centerOffset=new(0,1,0);float radius=.5f;var locator=new CellFloorLocator(0,0,start);
        using var leaf=new SingleLeafDoorRuntime(bundle.Leaf.Selection.Style,bundle.Leaf.Extent,bundle.DoorToCell,0,1);
        var bindings=new Dictionary<(int,int),SingleLeafDoorRuntime>{{(0,0),leaf},{(1,0),leaf}};
        var walker=new PortalDoorSphereFloorWalker(bundle.Layout,bundle.Floors,doorLeaves:bindings,allowSeparatingContacts:true);
        var result=walker.Walk(locator,end,centerOffset,radius,_=>new(true,true));
        Require(result.ContactKind==PortalActorContactKind.DoorLeaf && result.Movement.Status==CellFloorWalkStatus.PortalBlocked && result.Movement.Locator.CellIndex==0 && Math.Abs(result.Movement.WorldPosition.X+.6)<1e-5 && Math.Abs(result.Movement.WorldPosition.Y+.12)<1e-5,"Closed leaf contact follows source slope");
        leaf.NotifyContact(1,0,0,start+centerOffset,true,0);leaf.Advance(0,1,_=>start,true);
        int calls=0;result=walker.Walk(locator,end,centerOffset,radius,l=>{calls++;return new(l.SourceCell==0,true);});
        Require(result.Movement.Status==CellFloorWalkStatus.Accepted && result.Movement.Transfers==1 && result.Movement.Locator.CellIndex==1 && Math.Abs(result.Movement.WorldPosition.Y-.16)<1e-5 && calls==1,"Forward target entry does not use reverse denial");
        var reverse=walker.Walk(result.Movement.Locator,start,centerOffset,radius,l=>new(l.SourceCell==0,true));
        Require(reverse.Movement.Status==CellFloorWalkStatus.EntryDenied && reverse.Movement.Transfers==0 && reverse.Movement.Locator.CellIndex==1 && reverse.ContactKind==PortalActorContactKind.PermissionBarrier,"Reverse denial stops actor before source-entry seam");
        Require(walker.Walk(locator,end,centerOffset,radius,_=>new(null,true)).Movement.Status==CellFloorWalkStatus.PermissionUnknown,"Unknown permission barrier on slope");
        Require(walker.Walk(locator,end,centerOffset,radius,_=>new(true,false)).Movement.Status==CellFloorWalkStatus.PortalBlocked,"Current closed permission barrier independent of leaf");
        var captured=walker.Walk(locator,end,centerOffset,radius,_=>{leaf.Reset();return new(true,true);});
        Require(captured.Movement.Status==CellFloorWalkStatus.Accepted && walker.Walk(locator,end,centerOffset,radius,_=>new(true,true)).Movement.Status==CellFloorWalkStatus.PortalBlocked,"Shared source/target physical snapshot predates callback reset");
        int invalidCalls=0;Require(walker.Walk(locator with{TriangleIndex=-1},end,centerOffset,radius,_=>{invalidCalls++;return new(true,true);}).Movement.Status==CellFloorWalkStatus.InvalidStart && invalidCalls==0,"Invalid source never calls external policy");
        HostAccountingScenarios.Reject<InvalidDataException>(()=>walker.Walk(locator,end,new(float.NaN,0,0),radius,_=>throw new Exception("invalid callback")));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>walker.Walk(locator,end,new(1000001,1,0),radius,_=>throw new Exception("invalid offset callback")));
        HostAccountingScenarios.Reject<InvalidOperationException>(()=>walker.Walk(locator,end,centerOffset,radius,_=>throw new InvalidOperationException("policy")));
        Require(leaf.State.Controller.Fraction==0,"Query does not advance controller after callback failure");
        Console.WriteLine("PASS: sphere door captured leaf slope contact, directional/unknown/barrier policy, shared snapshots, invalid start and callback failure gates");
    }
    private static void Require(bool value,string label){if(!value)throw new Exception("Precise door actor: "+label);}
}
