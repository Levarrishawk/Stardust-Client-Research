using System.Numerics;
using Stardust.Core.Assets;

internal static class FloorPathScenarios
{
    internal static FloorMeshDescription Fixture()
    {
        var basis=FloorMeshDecoder.Decode(FloorMeshScenarios.Fixture());
        FloorTriangle T(int[] v,int[] n)=>new(v,0,n,Vector3.UnitY,new byte[]{1,1,1},0,0,new[]{-1,-1,-1});
        return basis with{Vertices=new Vector3[]{new(0,0,0),new(2,0,0),new(0,0,2),new(2,2,2)},Triangles=new[]{T(new[]{0,1,2},new[]{-1,1,-1}),T(new[]{1,3,2},new[]{-1,-1,0})}};
    }
    internal static void Run()
    {
        var floor=Fixture();var walker=new SingleCellFloorWalker(floor);Vector3 start=new(.25f,0,.25f),end=new(1.75f,0,1.75f);
        var path=walker.Trace(0,start,end);
        Require(path.Result==walker.Walk(0,start,end) && path.Segments.Count==2,"Trace preserves walk result and triangle count");
        var a=path.Segments[0];var b=path.Segments[1];
        Require(a.TriangleIndex==0 && a.StartParameter==0 && a.EndParameter==.5 && a.Start==start && a.End==new Vector3(1,0,1),"Flat approach reaches crease at actual floor height");
        Require(b.TriangleIndex==1 && b.StartParameter==.5 && b.EndParameter==1 && b.Start==a.End && b.End==new Vector3(1.75f,1.5f,1.75f),"Ramp segment retains authored plane instead of endpoint chord");
        Require(Vector3.Lerp(start,b.End.ToVector3(),.5f).Y==.75f && a.End.Y==0,"Endpoint chord differs from piecewise floor path");
        var reverse=walker.Trace(1,path.Result.Position,start);Require(reverse.Segments.Count==2 && reverse.Segments[0].TriangleIndex==1 && reverse.Segments[1].TriangleIndex==0 && reverse.Result.Position==start,"Reverse crease path");
        var limited=walker.Trace(0,start,end,0);Require(limited.Result.Status==FloorWalkStatus.TraversalLimit && limited.Segments.Count==1 && limited.Segments[0].End==limited.Result.Position,"Traversal limit returns accepted prefix only");
        var invalid=walker.Trace(0,new(-1,0,-1),end);Require(invalid.Result.Status==FloorWalkStatus.InvalidStart && invalid.Segments.Count==0,"Invalid start has no manufactured path");
        var boundary=walker.Trace(0,start,new(-1,0,.3f));Require(boundary.Result.Status==FloorWalkStatus.OpenBoundary && Vector3.Distance(boundary.Segments[^1].End.ToVector3(),boundary.Result.Position)<1e-4,"Boundary precise path stays near canonical returned contact");
        var zero=walker.Trace(0,start,start);Require(zero.Segments.Count==1 && zero.Segments[0].Start==zero.Segments[0].End && zero.Result.Position==start,"No-op has one stationary segment");
        Random random=new(74412);int traces=0;
        for(int i=0;i<1000;i++)
        {
            float x=.1f+(float)random.NextDouble()*.7f,z=.1f+(float)random.NextDouble()*.7f,ex=1.2f+(float)random.NextDouble()*.6f,ez=1.2f+(float)random.NextDouble()*.6f;
            var p=walker.Trace(0,new(x,0,z),new(ex,0,ez));Require(p.Result==walker.Walk(0,new(x,0,z),new(ex,0,ez)) && p.Segments.Count==2,"Seeded trace/result equivalence");
            Require(p.Segments[0].EndParameter==p.Segments[1].StartParameter && Vector3.Distance(p.Segments[0].End.ToVector3(),p.Segments[1].Start.ToVector3())<1e-5,"Ordered continuous trace prefix");
            Require(p.Segments[0].Start.Y==0 && Math.Abs(p.Segments[1].End.Y-(ex+ez-2))<1e-6,"Independent two-plane height");traces++;
        }
        Console.WriteLine($"PASS: piecewise floor crease/reverse/boundary/invalid/no-op/traversal prefixes and {traces} seeded trace/result/height gates");
    }
    private static void Require(bool value,string label){if(!value)throw new Exception("Floor path: "+label);}
}
