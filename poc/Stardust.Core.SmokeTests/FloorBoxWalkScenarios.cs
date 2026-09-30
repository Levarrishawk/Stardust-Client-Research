using System.Numerics;
using Stardust.Core.Assets;

internal static class FloorBoxWalkScenarios
{
    internal static void Run()
    {
        var description=FloorPathScenarios.Fixture();Vector3 start=new(.25f,0,.25f),end=new(1.75f,0,1.75f),half=new(.05f,.1f,.05f);
        var near=new PortalBarrierCollider(new(.95f,0,.95f),new(1.05f,.3f,1.05f),AppearanceTransform.Identity);
        var far=new PortalBarrierCollider(new(1.4f,.7f,1.4f),new(1.5f,1,1.5f),AppearanceTransform.Identity);
        PortalBarrierCollider[] supplied={far,near};var walker=new SingleCellFloorBoxWalker(description,supplied);supplied[1]=far;
        int callbacks=0;var hit=walker.Walk(0,start,end,half,_=>{callbacks++;return false;});
        Require(hit.Status==FloorBoxWalkStatus.BoxStopped && hit.ColliderIndex==1 && callbacks==2 && hit.Floor.TriangleIndex==0,"Earliest contact and owned list/once policy");
        var repeat=walker.Walk(hit.Floor.TriangleIndex,hit.Floor.Position,end,half,_=>false,true);
        Require(repeat.Status==FloorBoxWalkStatus.BoxStopped && repeat.Floor.Position==hit.Floor.Position,"Stable repeated stopped locator");
        var retreat=walker.Walk(hit.Floor.TriangleIndex,hit.Floor.Position,start,half,_=>false,true);
        Require(retreat.Status==FloorBoxWalkStatus.Accepted && retreat.Floor.Position==start,"Stopped actor may retreat");
        hit=walker.Walk(0,start,end,half,_=>true);Require(hit.Status==FloorBoxWalkStatus.Accepted && hit.Floor.Position==new Vector3(1.75f,1.5f,1.75f),"Allowed slope traversal");
        var ramp=new SingleCellFloorBoxWalker(description,new[]{far});hit=ramp.Walk(0,start,end,half,_=>false);
        Require(hit.Status==FloorBoxWalkStatus.BoxStopped && hit.Floor.TriangleIndex==1 && Math.Abs(hit.Floor.Position.Y-(hit.Floor.Position.X+hit.Floor.Position.Z-2))<1e-6,"Ramp landing owns projected reusable locator");
        Require(ramp.Walk(hit.Floor.TriangleIndex,hit.Floor.Position,start,half,_=>false,true).Status==FloorBoxWalkStatus.Accepted,"Ramp reverse retreat");
        Require(ramp.Walk(0,start,end,half,_=>false,false,0).Status==FloorBoxWalkStatus.FloorStopped,"Floor prefix limits collision traversal");
        Require(ramp.Walk(0,new(-1,0,-1),end,half,_=>throw new Exception("invalid start callback")).Status==FloorBoxWalkStatus.FloorStopped,"Invalid start never invokes policy");
        var tied=new SingleCellFloorBoxWalker(description,new[]{near,near});Require(tied.Walk(0,start,end,half,_=>false).ColliderIndex==0,"Equal contacts keep input order");
        Vector3 offset=new(1_000_000,0,1_000_000);var large=description with{Vertices=description.Vertices.Select(v=>v+offset).ToArray()};
        var largeBox=new PortalBarrierCollider(near.Minimum+offset,near.Maximum+offset,AppearanceTransform.Identity);
        var held=new SingleCellFloorBoxWalker(large,new[]{largeBox}).Walk(0,start+offset,end+offset,half,_=>false);
        Require(held.Status==FloorBoxWalkStatus.ContactLandingUnsupported && held.Floor.Position==start+offset && held.Floor.Parameter==0,"Large-coordinate imprecise contact holds validated source");
        HostAccountingScenarios.Reject<InvalidDataException>(()=>new SingleCellFloorBoxWalker(description,Enumerable.Repeat(near,65).ToArray()));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>walker.Walk(0,start,end,new(-1,0,0),_=>true));
        HostAccountingScenarios.Reject<InvalidOperationException>(()=>walker.Walk(0,start,end,half,_=>throw new InvalidOperationException("policy")));
        Require(walker.Walk(0,start,end,half,_=>true).Status==FloorBoxWalkStatus.Accepted,"Policy failure has no retained mutation");
        Console.WriteLine("PASS: same-cell floor-box earliest/tied/owned policy, ramp locator/repeat/retreat, prefix/invalid, precision hold and stateless failure gates");
    }
    private static void Require(bool value,string label){if(!value)throw new Exception("Floor box walk: "+label);}
}
