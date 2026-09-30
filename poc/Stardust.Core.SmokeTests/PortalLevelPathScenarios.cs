using System.Numerics;
using Stardust.Core.Assets;

internal static class PortalLevelPathScenarios
{
    internal static void Run()
    {
        var layout=PortalLayoutDecoder.Decode(PortalLayoutScenarios.Fixture());var basis=FloorPathScenarios.Fixture();
        Vector3[] vertices={new(0,0,0),new(1,1,0),new(2,0,0),new(0,0,1),new(1,1,1),new(2,0,1)};
        FloorTriangle T(int[] v,int[] n,int[]? p=null)=>new(v,0,n,Vector3.UnitY,new byte[]{1,1,1},0,0,p??new[]{-1,-1,-1});
        var ridge=basis with{Vertices=vertices,Triangles=new[]{T(new[]{0,1,3},new[]{-1,1,-1}),T(new[]{1,4,3},new[]{2,-1,0}),T(new[]{1,2,4},new[]{-1,3,1}),T(new[]{2,5,4},new[]{-1,-1,2})}};
        var floors=new Dictionary<int,FloorMeshDescription>{{0,ridge}};CellFloorLocator start=new(0,0,new(.25f,.25f,.25f));Vector3 end=new(1.75f,.25f,.25f);
        var query=new PortalFloorTransferQuery(layout,floors);var trace=query.Trace(start,end,_=>new(true,true));
        Require(trace.Result==query.Walk(start,end,_=>new(true,true)) && trace.Result.Status==CellFloorWalkStatus.Accepted && trace.Result.WorldPosition.Y==start.LocalPosition.Y && trace.Floors[0].Path.Segments.Any(s=>s.End.Y==1),"Same-height endpoints hide source ridge");
        var result=new PortalBarrierFloorWalker(layout,floors).Walk(start,end,new(.05f,.2f,.05f),_=>new(true,true));
        Require(result.Diagnostic=="UnsupportedNonlevelBarrierSweep" && result.Movement.Locator==start && result.Movement.Parameter==0,"Source ridge guard holds source");
        var source=basis with{Vertices=new Vector3[]{new(-1,0,0),new(0,0,0),new(0,0,1)},Triangles=new[]{T(new[]{0,1,2},new[]{-1,-1,-1},new[]{-1,0,-1})}};
        var first=ridge.Triangles[0] with{PortalIds=new[]{-1,-1,0}};ridge=ridge with{Triangles=new[]{first,ridge.Triangles[1],ridge.Triangles[2],ridge.Triangles[3]}};
        floors=new(){{0,source},{1,ridge}};start=new(0,0,new(-.5f,0,.25f));end=new(2,0,.25f);query=new(layout,floors);trace=query.Trace(start,end,_=>new(true,true));
        Require(trace.Result==query.Walk(start,end,_=>new(true,true)) && trace.Result.Status==CellFloorWalkStatus.Accepted && trace.Result.Transfers==1 && trace.Floors.Count==2 && trace.Result.WorldPosition.Y==0 && trace.Floors[1].Path.Segments.Any(s=>s.End.Y==1),"Same-height endpoints hide target ridge");
        result=new PortalBarrierFloorWalker(layout,floors).Walk(start,end,new(.05f,.2f,.05f),_=>new(true,true));
        Require(result.Diagnostic=="UnsupportedNonlevelBarrierSweep" && result.Movement.Transfers==0 && result.Movement.Locator==start,"Target ridge guard rejects before adoption");
        var flat=ridge with{Vertices=ridge.Vertices.Select(v=>v with{Y=0}).ToArray()};floors[1]=flat;
        Require(new PortalBarrierFloorWalker(layout,floors).Walk(start,end,new(.05f,.2f,.05f),_=>new(true,true)).Movement.Transfers==1,"Flat target regression remains accepted");
        Console.WriteLine("PASS: paired floor trace/result equivalence and source/target equal-endpoint hidden-ridge rejection with flat-transfer regression");
    }
    private static void Require(bool value,string label){if(!value)throw new Exception("Portal level path: "+label);}
}
