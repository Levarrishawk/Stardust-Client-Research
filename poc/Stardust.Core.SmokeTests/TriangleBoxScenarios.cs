using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class TriangleBoxScenarios
{
    internal static void Run()
    {
        var wall=new CollisionTriangle(7,new(0,-10,-10),new(0,10,-10),new(0,0,10));var input=new[]{wall};var collider=new StaticTriangleBoxCollider(input);input[0]=wall with{A=new(float.NaN,0,0)};
        var hit=collider.Sweep(new(-3,0,0),new(3,0,0),new(.5f,.5f,.5f));
        if(hit.Status!=TriangleBoxContactStatus.Blocked||Math.Abs(hit.Parameter-5.0/12)>1e-12||hit.Normal!=new Vector3(-1,0,0))throw new Exception("Triangle wall crossing/tunneling/owned snapshot failed.");
        hit=collider.Sweep(new(-.5f,0,0),new(-1,0,0),new(.5f,.5f,.5f),true);if(hit.Status!=TriangleBoxContactStatus.Clear)throw new Exception("Triangle outward touch failed.");
        hit=collider.Sweep(new(-.5f,0,0),new(.5f,0,0),new(.5f,.5f,.5f),true);if(hit.Status!=TriangleBoxContactStatus.Blocked||hit.Parameter!=0)throw new Exception("Triangle inward touch escaped.");
        if(collider.Sweep(Vector3.Zero,new(-1,0,0),new(.5f,.5f,.5f),true).Status!=TriangleBoxContactStatus.InitialOverlap)throw new Exception("Triangle genuine overlap incorrectly separated.");
        var small=new StaticTriangleBoxCollider(new[]{new CollisionTriangle(0,Vector3.Zero,Vector3.UnitX,Vector3.UnitY)});
        if(small.Sweep(new(.9f,.9f,-1),new(.9f,.9f,1),new(.05f,.05f,.05f)).Status!=TriangleBoxContactStatus.Clear)throw new Exception("Triangle edge separation treated bounding box as collision.");
        hit=small.Sweep(new(.25f,.25f,-1),new(.25f,.25f,1),Vector3.Zero);if(hit.Status!=TriangleBoxContactStatus.Blocked||hit.Parameter!=.5)throw new Exception("Triangle zero-box ray failed.");
        var floor=new StaticTriangleBoxCollider(new[]{new CollisionTriangle(1,new(-10,0,-10),new(10,0,-10),new(0,0,10))});
        if(floor.Sweep(new(0,.5f,0),new(1,.5f,0),new(.5f,.5f,.5f),true).Status!=TriangleBoxContactStatus.Clear)throw new Exception("Triangle tangent support failed.");
        var tied=new StaticTriangleBoxCollider(new[]{wall with{Id=9},wall with{Id=3}});if(tied.Sweep(new(-3,0,0),new(3,0,0),new(.5f,.5f,.5f)).TriangleId!=3)throw new Exception("Triangle contact tie unstable.");
        HostAccountingScenarios.Reject<ArgumentException>(()=>collider.Sweep(Vector3.Zero,Vector3.One,new(-1,1,1)));
        HostAccountingScenarios.Reject<ArgumentException>(()=>collider.Sweep(Vector3.Zero,new(101,0,0),Vector3.One));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>new StaticTriangleBoxCollider(new[]{wall,wall}));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>new StaticTriangleBoxCollider(new[]{wall,wall with{Id=8,A=new(float.NaN,0,0)}}));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>new StaticTriangleBoxCollider(new[]{new CollisionTriangle(0,Vector3.Zero,Vector3.Zero,Vector3.Zero)}));
        Console.WriteLine("PASS: continuous triangle-box wall/edge/ray/support contact, separating versus overlap, stable ties, owned geometry and bounds negatives");
        RunPaths();
        RunWalks();
        RunSteps();
    }
    private static void RunSteps()
    {
        var basis=FloorMeshDecoder.Decode(FloorMeshScenarios.Fixture());
        FloorMeshDescription Fixture(float height=.1f,bool down=false)
        {
            Vector3[] vertices={new(0,0,.5f),new(1,0,0),new(1,0,1),new(1,height,0),new(1,height,1),new(2,height,.5f)};
            if(down)vertices=vertices.Select(v=>v with{Y=height-v.Y}).ToArray();
            FloorTriangle Row(int[] indices,int[] neighbors)=>new(indices,0,neighbors,Vector3.UnitY,new byte[]{1,1,1},0,0,new[]{-1,-1,-1});
            return basis with{Vertices=vertices,Triangles=new[]{Row(new[]{0,1,2},new[]{-1,1,-1}),Row(new[]{2,1,3},new[]{0,-1,2}),Row(new[]{2,3,4},new[]{1,3,-1}),Row(new[]{4,3,5},new[]{2,-1,-1})}};
        }
        Vector3 start=new(.5f,0,.5f),end=new(1.5f,0,.5f),half=new(.25f,.9f,.25f);
        foreach(bool down in new[]{false,true})
        {
            var clear=new SingleCellFloorStepTriangleBoxWalker(Fixture(down:down),Array.Empty<CollisionTriangle>());
            var result=clear.Walk(0,start with{Y=down?.1f:0},end,half,true);
            if(result.Steps!=1||result.Movement.Status!=FloorBoxWalkStatus.Accepted||result.Movement.Floor.Position!=new Vector3(1.5f,down?0:.1f,.5f))throw new Exception("Triangle step clear rise/drop failed.");
        }
        var ceiling=new CollisionTriangle(1,new(-10,1.85f,-10),new(10,1.85f,-10),new(0,1.85f,10));
        var blocked=new SingleCellFloorStepTriangleBoxWalker(Fixture(),new[]{ceiling});
        var hit=blocked.Walk(0,start,end,half,true);
        if(hit.Steps!=0||hit.Diagnostic!="VerticalStepBlocked"||hit.Movement.Floor.Position!=new Vector3(1,0,.5f))throw new Exception("Triangle step ceiling did not hold source.");
        var held=hit.Movement.Floor;
        for(int i=0;i<100;i++)
        {
            hit=blocked.Walk(held.TriangleIndex,held.Position,end,half,true);
            if(hit.Steps!=0||hit.Movement.Floor.Position!=held.Position)throw new Exception("Triangle step ceiling repeat drifted.");
        }
        if(blocked.Walk(held.TriangleIndex,held.Position,start,half,true).Movement.Floor.Position!=start)throw new Exception("Triangle step ceiling retreat failed.");
        var wall=new CollisionTriangle(2,new(1.4f,-10,-10),new(1.4f,10,-10),new(1.4f,0,10));
        var target=new SingleCellFloorStepTriangleBoxWalker(Fixture(),new[]{wall});
        hit=target.Walk(0,start,end,half,true);
        if(hit.Steps!=1||hit.Movement.Status!=FloorBoxWalkStatus.BoxStopped||hit.Movement.Floor.TriangleIndex!=3||Math.Abs(hit.Movement.Floor.Position.X-1.15f)>1e-6||hit.Movement.Floor.Position.Y!=.1f)throw new Exception("Triangle step target continuation contact failed.");
        var source=new SingleCellFloorStepTriangleBoxWalker(Fixture(),new[]{wall with{A=new(.9f,-10,-10),B=new(.9f,10,-10),C=new(.9f,0,10)}});
        hit=source.Walk(0,start,end,half,true);
        if(hit.Steps!=0||hit.Movement.Status!=FloorBoxWalkStatus.BoxStopped||hit.Proposal is not null)throw new Exception("Triangle step approach wall bypassed.");
        hit=new SingleCellFloorStepTriangleBoxWalker(Fixture(.21f),Array.Empty<CollisionTriangle>()).Walk(0,start,end,half,true);
        if(hit.Steps!=0||hit.Diagnostic!="HeightLimit")throw new Exception("Triangle step height policy bypassed.");
        Console.WriteLine("PASS: triangle step clear rise/drop, ceiling source rollback, 100 repeat holds/retreat, source/target wall contact and height-policy gates");
    }
    private static void RunWalks()
    {
        var description=FloorPathScenarios.Fixture();Vector3 start=new(.25f,0,.25f),end=new(1.75f,0,1.75f),half=new(.05f,.1f,.05f);
        var wall=new CollisionTriangle(0,new(.95f,-10,-10),new(.95f,10,-10),new(.95f,0,10));
        var walker=new SingleCellFloorTriangleBoxWalker(description,new[]{wall});
        var hit=walker.Walk(0,start,end,half,true);
        if(hit.Status!=FloorBoxWalkStatus.BoxStopped||hit.Floor.TriangleIndex!=0||Math.Abs(hit.Floor.Position.X-.9f)>1e-6||hit.Floor.Position.Y!=0)throw new Exception("Triangle floor supported contact adoption failed.");
        var held=hit.Floor;
        for(int i=0;i<100;i++)
        {
            var repeat=walker.Walk(held.TriangleIndex,held.Position,end,half,true);
            if(repeat.Status!=FloorBoxWalkStatus.BoxStopped||repeat.Floor.Position!=held.Position||repeat.Floor.TriangleIndex!=held.TriangleIndex)throw new Exception("Triangle floor repeat crept.");
        }
        var retreat=walker.Walk(held.TriangleIndex,held.Position,start,half,true);
        if(retreat.Status!=FloorBoxWalkStatus.Accepted||retreat.Floor.Position!=start)throw new Exception("Triangle floor retreat failed.");
        var ramp=new SingleCellFloorTriangleBoxWalker(description,new[]{wall with{A=new(1.5f,-10,-10),B=new(1.5f,10,-10),C=new(1.5f,0,10)}});
        hit=ramp.Walk(0,start,end,half,true);
        if(hit.Status!=FloorBoxWalkStatus.BoxStopped||hit.Floor.TriangleIndex!=1||Math.Abs(hit.Floor.Position.Y-(hit.Floor.Position.X+hit.Floor.Position.Z-2))>1e-6)throw new Exception("Triangle floor ramp locator failed.");
        if(ramp.Walk(hit.Floor.TriangleIndex,hit.Floor.Position,start,half,true).Status!=FloorBoxWalkStatus.Accepted)throw new Exception("Triangle floor ramp retreat failed.");
        var ceiling=new SingleCellFloorTriangleBoxWalker(description,new[]{new CollisionTriangle(8,new(-10,.75f,-10),new(10,.75f,-10),new(0,.75f,10))});
        hit=ceiling.Walk(0,start,end,half,true);
        if(hit.Status!=FloorBoxWalkStatus.BoxStopped||hit.Floor.TriangleIndex!=1||Math.Abs(hit.Floor.Position.Y-.55f)>1e-6)throw new Exception("Triangle floor rising-path ceiling stop failed.");
        Vector3 offset=new(999998,0,999998);
        var distantFloor=description with{Vertices=description.Vertices.Select(v=>v+offset).ToArray()};
        var distantWall=new CollisionTriangle(1,new(999998.95f,-10,999990),new(999998.95f,10,999990),new(999998.95f,0,1000000));
        var distant=new SingleCellFloorTriangleBoxWalker(distantFloor,new[]{distantWall});
        hit=distant.Walk(0,start+offset,end+offset,half,true);
        if(hit.Status!=FloorBoxWalkStatus.ContactLandingUnsupported||hit.Floor.Position!=start+offset||hit.Floor.Parameter!=0)throw new Exception("Triangle floor imprecise landing did not hold supported source.");
        var clear=new SingleCellFloorTriangleBoxWalker(description,Array.Empty<CollisionTriangle>());
        if(clear.Walk(0,start,end,half).Floor.Position!=new Vector3(1.75f,1.5f,1.75f))throw new Exception("Triangle floor clear slope failed.");
        if(clear.Walk(0,start,end,half,false,0).Status!=FloorBoxWalkStatus.FloorStopped)throw new Exception("Triangle floor prefix limit failed.");
        if(walker.Walk(0,new(-1,0,-1),end,half).Status!=FloorBoxWalkStatus.FloorStopped)throw new Exception("Triangle floor invalid start adopted.");
        HostAccountingScenarios.Reject<InvalidDataException>(()=>walker.Walk(0,start,end,new(-1,1,1)));
        if(walker.Walk(0,start,start,half,true).Status!=FloorBoxWalkStatus.Accepted)throw new Exception("Triangle floor failure changed query state.");
        Console.WriteLine("PASS: triangle floor supported flat/ramp/ceiling adoption, 100 repeat holds, retreat, precision rollback, clear slope, invalid/prefix and stateless failure gates");
    }
    private static void RunPaths()
    {
        FloorWalkPath Path(params FloorWalkSegment[] segments)
        {
            var last=segments[^1];return new(new(FloorWalkStatus.Accepted,last.TriangleIndex,
                new((float)last.End.X,(float)last.End.Y,(float)last.End.Z),last.EndParameter,-1,-1,segments.Length-1),segments);
        }
        var wall=new StaticTriangleBoxCollider(new[]{new CollisionTriangle(2,new(0,-10,-10),new(0,10,-10),new(0,0,10))});
        var path=Path(new(3,0,.25,new(-3,0,0),new(-1,0,0)),new(4,.25,1,new(-1,0,0),new(3,0,0)));
        var hit=FloorPathTriangleBoxSweep.Sweep(path,wall,new(.5f,.5f,.5f));
        if(hit.Status!=TriangleBoxContactStatus.Blocked||hit.SegmentIndex!=1||hit.TriangleId!=2||hit.Parameter!=.34375||hit.Center!=new FloorPathPoint(-.5,.5,0))throw new Exception("Triangle feet-path mapped progress/center failed.");
        path=Path(new FloorWalkSegment(3,0,1,new(-3,0,0),new(3,3,0)));
        hit=FloorPathTriangleBoxSweep.Sweep(path,wall,new(.5f,.5f,.5f));
        if(Math.Abs(hit.Parameter-5.0/12)>1e-12||Math.Abs(hit.Center.Y-1.75)>1e-12)throw new Exception("Triangle feet-path slope was flattened.");
        var ceiling=new StaticTriangleBoxCollider(new[]{new CollisionTriangle(9,new(-10,1.9f,-10),new(10,1.9f,-10),new(0,1.9f,10))});
        path=Path(new(3,0,0,new(0,0,0),new(0,.2,0)),new(4,0,1,new(0,.2,0),new(1,.2,0)));
        hit=FloorPathTriangleBoxSweep.Sweep(path,ceiling,new(.25f,.9f,.25f),true);
        if(hit.Status!=TriangleBoxContactStatus.Blocked||hit.SegmentIndex!=0||hit.Parameter!=0||Math.Abs(hit.Center.Y-(1.9f-.9f))>1e-12)throw new Exception("Triangle feet-path vertical step clearance failed.");
        var distant=new StaticTriangleBoxCollider(new[]{new CollisionTriangle(1,new(1e6f,-10,-10),new(1e6f,10,-10),new(1e6f,0,10))});
        path=Path(new FloorWalkSegment(1,0,1,new(999999.99,0,0),new(1000000,0,0)));
        hit=FloorPathTriangleBoxSweep.Sweep(path,distant,new(.001f,.5f,.5f));
        double expected=(1000000-(double).001f-999999.99)/(1000000-999999.99);
        if(hit.Status!=TriangleBoxContactStatus.Blocked||Math.Abs(hit.Parameter-expected)>1e-12||hit.Parameter<.8)throw new Exception("Triangle feet-path double precision lost.");
        var bad=Path(new(3,0,.25,new(-3,0,0),new(-1,0,0)),new(4,.25,1,new(-1,0,0),new(double.NaN,0,0)));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>FloorPathTriangleBoxSweep.Sweep(bad,wall,Vector3.One));
        bad=Path(new(3,0,.25,new(-3,0,0),new(-1,0,0)),new(4,.25,1,new(-.9,0,0),new(3,0,0)));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>FloorPathTriangleBoxSweep.Sweep(bad,wall,Vector3.One));
        var floor=new StaticTriangleBoxCollider(new[]{new CollisionTriangle(1,new(-10,0,-10),new(10,0,-10),new(0,0,10))});
        path=Path(new FloorWalkSegment(3,0,1,new(0,0,0),new(1,0,0)));
        if(FloorPathTriangleBoxSweep.Sweep(path,floor,new(.5f,.5f,.5f),true).Status!=TriangleBoxContactStatus.Clear)throw new Exception("Triangle feet-path support prevented walking.");
        var many=new StaticTriangleBoxCollider(Enumerable.Range(0,2048).Select(i=>new CollisionTriangle(i,new(0,-10,-10),new(0,10,-10),new(0,0,10))).ToArray());
        path=Path(Enumerable.Range(0,129).Select(i=>new FloorWalkSegment(0,i/129.0,(i+1)/129.0,new(0,0,0),new(0,0,0))).ToArray());
        HostAccountingScenarios.Reject<InvalidDataException>(()=>FloorPathTriangleBoxSweep.Sweep(path,many,Vector3.One));
        Console.WriteLine("PASS: precise triangle feet-path mapped progress, slopes, vertical ceiling clearance, distant precision, support and whole-path/work-budget negatives");
    }
    internal static void ProbeOptional(string[] args,bool paths=false)
    {
        if(args.Length!=4||args[2]!="--expected-sha256"||new FileInfo(args[1]).Length>1_048_576)throw new ArgumentException("Pinned bounded synthetic sweep fixture required.");
        byte[] bytes=File.ReadAllBytes(args[1]);if(Convert.ToHexString(SHA256.HashData(bytes))!=args[3].ToUpperInvariant())throw new InvalidDataException("Synthetic sweep fixture pin mismatch.");
        using var doc=JsonDocument.Parse(bytes);var cases=doc.RootElement.GetProperty("cases");if(cases.GetArrayLength()>2048)throw new InvalidDataException("Sweep case budget.");
        Vector3 V(JsonElement value){if(value.GetArrayLength()!=3)throw new InvalidDataException("Sweep vector extent.");return new(value[0].GetSingle(),value[1].GetSingle(),value[2].GetSingle());}
        int index=0;foreach(var item in cases.EnumerateArray())
        {
            var v=item.GetProperty("triangle");if(v.GetArrayLength()!=3)throw new InvalidDataException("Synthetic triangle extent.");
            var collider=new StaticTriangleBoxCollider(new[]{new CollisionTriangle(0,V(v[0]),V(v[1]),V(v[2]))});
            if(paths)
            {
                var points=item.GetProperty("points");var parameters=item.GetProperty("parameters");
                if(points.GetArrayLength()!=3||parameters.GetArrayLength()!=3)throw new InvalidDataException("Synthetic path extent.");
                FloorPathPoint P(JsonElement p){if(p.GetArrayLength()!=3)throw new InvalidDataException("Synthetic precise vector extent.");return new(p[0].GetDouble(),p[1].GetDouble(),p[2].GetDouble());}
                var last=P(points[2]);var path=new FloorWalkPath(new(FloorWalkStatus.Accepted,0,new((float)last.X,(float)last.Y,(float)last.Z),parameters[2].GetDouble(),-1,-1,1),
                    new[]{new FloorWalkSegment(0,parameters[0].GetDouble(),parameters[1].GetDouble(),P(points[0]),P(points[1])),new FloorWalkSegment(1,parameters[1].GetDouble(),parameters[2].GetDouble(),P(points[1]),last)});
                var hit=FloorPathTriangleBoxSweep.Sweep(path,collider,V(item.GetProperty("half")));
                Console.WriteLine("triangle_box="+JsonSerializer.Serialize(new{index=index++,status=hit.Status.ToString(),parameter=hit.Parameter,segment=hit.SegmentIndex,normal=new[]{hit.Normal.X,hit.Normal.Y,hit.Normal.Z}}));
            }
            else
            {
                var hit=collider.Sweep(V(item.GetProperty("start")),V(item.GetProperty("end")),V(item.GetProperty("half")));
                Console.WriteLine("triangle_box="+JsonSerializer.Serialize(new{index=index++,status=hit.Status.ToString(),parameter=hit.Parameter,normal=new[]{hit.Normal.X,hit.Normal.Y,hit.Normal.Z}}));
            }
        }
    }
}
