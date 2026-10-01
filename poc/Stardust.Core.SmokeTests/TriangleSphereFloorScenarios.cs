using System.Numerics;
using Stardust.Core.Assets;

internal static class TriangleSphereFloorScenarios
{
    internal static void Run(){RunWalks();RunPaths();}
    private static void RunWalks()
    {
        var description=FloorPathScenarios.Fixture();Vector3 start=new(.25f,0,.25f),end=new(1.75f,0,1.75f),centerOffset=new(0,.1f,0);float radius=.05f;
        var wall=new CollisionTriangle(0,new(.95f,-10,-10),new(.95f,10,-10),new(.95f,0,10));
        var walker=new SingleCellFloorTriangleSphereWalker(description,new[]{wall});
        var hit=walker.Walk(0,start,end,centerOffset,radius,true);
        if(hit.Status!=FloorBoxWalkStatus.BoxStopped||hit.Floor.TriangleIndex!=0||Math.Abs(hit.Floor.Position.X-.9f)>1e-6||hit.Floor.Position.Y!=0)throw new Exception("Sphere floor supported contact adoption failed.");
        var held=hit.Floor;
        for(int i=0;i<100;i++)
        {
            var repeat=walker.Walk(held.TriangleIndex,held.Position,end,centerOffset,radius,true);
            if(repeat.Status!=FloorBoxWalkStatus.BoxStopped||repeat.Floor.Position!=held.Position||repeat.Floor.TriangleIndex!=held.TriangleIndex)throw new Exception("Sphere floor repeat crept.");
        }
        var retreat=walker.Walk(held.TriangleIndex,held.Position,start,centerOffset,radius,true);
        if(retreat.Status!=FloorBoxWalkStatus.Accepted||retreat.Floor.Position!=start)throw new Exception("Sphere floor retreat failed.");
        var ramp=new SingleCellFloorTriangleSphereWalker(description,new[]{wall with{A=new(1.5f,-10,-10),B=new(1.5f,10,-10),C=new(1.5f,0,10)}});
        hit=ramp.Walk(0,start,end,centerOffset,radius,true);
        if(hit.Status!=FloorBoxWalkStatus.BoxStopped||hit.Floor.TriangleIndex!=1||Math.Abs(hit.Floor.Position.Y-(hit.Floor.Position.X+hit.Floor.Position.Z-2))>1e-6)throw new Exception("Sphere floor ramp locator failed.");
        if(ramp.Walk(hit.Floor.TriangleIndex,hit.Floor.Position,start,centerOffset,radius,true).Status!=FloorBoxWalkStatus.Accepted)throw new Exception("Sphere floor ramp retreat failed.");
        var ceiling=new SingleCellFloorTriangleSphereWalker(description,new[]{new CollisionTriangle(8,new(-10,.75f,-10),new(10,.75f,-10),new(0,.75f,10))});
        hit=ceiling.Walk(0,start,end,centerOffset,radius,true);
        if(hit.Status!=FloorBoxWalkStatus.BoxStopped||hit.Floor.TriangleIndex!=1||Math.Abs(hit.Floor.Position.Y-.6f)>1e-6)throw new Exception("Sphere floor rising-path ceiling stop failed.");
        Vector3 offset=new(999998,0,999998);
        var distantFloor=description with{Vertices=description.Vertices.Select(v=>v+offset).ToArray()};
        var distantWall=new CollisionTriangle(1,new(999998.95f,-10,999990),new(999998.95f,10,999990),new(999998.95f,0,1000000));
        var distant=new SingleCellFloorTriangleSphereWalker(distantFloor,new[]{distantWall});
        hit=distant.Walk(0,start+offset,end+offset,centerOffset,radius,true);
        if(hit.Status!=FloorBoxWalkStatus.ContactLandingUnsupported||hit.Floor.Position!=start+offset||hit.Floor.Parameter!=0)throw new Exception("Sphere floor imprecise landing did not hold supported source.");
        var clear=new SingleCellFloorTriangleSphereWalker(description,Array.Empty<CollisionTriangle>());
        if(clear.Walk(0,start,end,centerOffset,radius).Floor.Position!=new Vector3(1.75f,1.5f,1.75f))throw new Exception("Sphere floor clear slope failed.");
        if(clear.Walk(0,start,end,centerOffset,radius,false,0).Status!=FloorBoxWalkStatus.FloorStopped)throw new Exception("Sphere floor prefix limit failed.");
        if(walker.Walk(0,new(-1,0,-1),end,centerOffset,radius).Status!=FloorBoxWalkStatus.FloorStopped)throw new Exception("Sphere floor invalid start adopted.");
        HostAccountingScenarios.Reject<InvalidDataException>(()=>walker.Walk(0,start,end,centerOffset,0));
        if(walker.Walk(0,start,start,centerOffset,radius,true).Status!=FloorBoxWalkStatus.Accepted)throw new Exception("Sphere floor failure changed query state.");
        Console.WriteLine("PASS: sphere floor supported flat/ramp/ceiling adoption, 100 repeat holds, retreat, precision rollback, clear slope, invalid/prefix and stateless failure gates");
    }
    private static void RunPaths()
    {
        FloorWalkPath Path(params FloorWalkSegment[] s)=>new(new(FloorWalkStatus.Accepted,s[^1].TriangleIndex,s[^1].End.ToVector3(),s[^1].EndParameter,-1,-1,s.Length-1),s);
        var wall=new StaticTriangleSphereCollider(new[]{new CollisionTriangle(2,new(0,-10,-10),new(0,10,-10),new(0,0,10))});
        var path=Path(new(3,0,.25,new(-3,0,0),new(-1,0,0)),new(4,.25,1,new(-1,0,0),new(3,0,0)));
        var hit=FloorPathTriangleSphereSweep.Sweep(path,wall,new(0,1,0),.5f);
        if(hit.Status!=TriangleSphereContactStatus.Blocked||hit.SegmentIndex!=1||hit.Parameter!=.34375||hit.Center!=new FloorPathPoint(-.5,1,0))throw new Exception("Sphere floor global progress/offset.");
        hit=FloorPathTriangleSphereSweep.Sweep(path,wall,new(.25f,1,0),.5f);
        if(hit.Center.X!=-.5||hit.Parameter!=.296875)throw new Exception("Sphere horizontal center offset ignored.");
        var bad=Path(new(3,0,.25,new(-3,0,0),new(-1,0,0)),new(4,.25,1,new(-1,0,0),new(double.NaN,0,0)));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>FloorPathTriangleSphereSweep.Sweep(bad,wall,Vector3.UnitY,.5f));
        bad=Path(new(3,0,.25,new(-3,0,0),new(-1,0,0)),new(4,.25,1,new(-.9,0,0),new(3,0,0)));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>FloorPathTriangleSphereSweep.Sweep(bad,wall,Vector3.UnitY,.5f));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>FloorPathTriangleSphereSweep.Sweep(path,wall,Vector3.UnitY,0));
        var many=new StaticTriangleSphereCollider(Enumerable.Range(0,2048).Select(i=>new CollisionTriangle(i,new(0,-10,-10),new(0,10,-10),new(0,0,10))).ToArray());
        path=Path(Enumerable.Range(0,129).Select(i=>new FloorWalkSegment(0,i/129.0,(i+1)/129.0,new(0,0,0),new(0,0,0))).ToArray());
        HostAccountingScenarios.Reject<InvalidDataException>(()=>FloorPathTriangleSphereSweep.Sweep(path,many,Vector3.UnitY,.5f));
        Console.WriteLine("PASS: sphere floor precise progress, full center offsets, whole-path malformed and work-budget gates");
    }
}
