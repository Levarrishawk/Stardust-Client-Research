using System.Numerics;
using Stardust.Core.Assets;

internal static class TriangleBoundsScenarios
{
    internal static void Run()
    {
        var random=new Random(20261001);
        float Next(float radius)=>(float)((random.NextDouble()*2-1)*radius);
        Vector3 V(float radius)=>new(Next(radius),Next(radius),Next(radius));
        var source=Enumerable.Range(0,128).Select(i=>{var center=V(20);return new CollisionTriangle(127-i,center+V(3),center+V(3),center+V(3));}).ToArray();
        var indexed=new StaticTriangleBoxCollider(source);var exhaustive=new StaticTriangleBoxCollider(source,useBoundsIndex:false);
        long candidates=0;int contacts=0;
        for(int i=0;i<512;i++)
        {
            var start=V(25);var end=V(25);var half=i%7==0?Vector3.Zero:new Vector3(Math.Abs(Next(2)),Math.Abs(Next(2)),Math.Abs(Next(2)));
            bool separating=i%2==0;var actual=indexed.SweepWithStatistics(start,end,half,separating);var expected=exhaustive.Sweep(start,end,half,separating);
            if(actual.Contact!=expected||actual.Statistics.CandidateTriangles>source.Length)throw new Exception("Triangle bounds index changed exhaustive contact.");
            candidates+=actual.Statistics.CandidateTriangles;if(actual.Contact.Status!=TriangleBoxContactStatus.Clear)contacts++;
        }
        var grid=Enumerable.Range(0,3072).Select(i=>new CollisionTriangle(i,new(i%64*10,-2,i/64*10-1),new(i%64*10,2,i/64*10-1),new(i%64*10,0,i/64*10+1))).ToArray();
        HostAccountingScenarios.Reject<InvalidDataException>(()=>new StaticTriangleBoxCollider(grid));
        var large=new StaticTriangleBoxCollider(grid,65536);var largeExhaustive=new StaticTriangleBoxCollider(grid,65536,false);
        var query=large.SweepWithStatistics(new(-3,0,0),new(3,0,0),new(.25f,.5f,.25f));
        if(query.Contact!=largeExhaustive.Sweep(new(-3,0,0),new(3,0,0),new(.25f,.5f,.25f))||query.Contact.TriangleId!=0||query.Statistics.CandidateTriangles!=1||query.Statistics.NodeVisits>=grid.Length)
            throw new Exception("Large triangle hierarchy failed conservative pruning.");
        Array.Clear(grid);if(large.Sweep(new(-3,0,0),new(3,0,0),new(.25f,.5f,.25f))!=query.Contact)throw new Exception("Bounds hierarchy borrowed input.");
        var tied=source.Select(t=>t with{A=t.A+new Vector3(0,0,1000),B=t.B+new Vector3(0,0,1000),C=t.C+new Vector3(0,0,1000)}).Concat(new[]{new CollisionTriangle(500,new(0,-10,-10),new(0,10,-10),new(0,0,10)),new CollisionTriangle(499,new(0,-10,-10),new(0,10,-10),new(0,0,10))}).ToArray();
        var tieIndex=new StaticTriangleBoxCollider(tied);var tieReference=new StaticTriangleBoxCollider(tied,useBoundsIndex:false);
        if(tieIndex.Sweep(new(-3,0,0),new(3,0,0),new(.5f,.5f,.5f))!=tieReference.Sweep(new(-3,0,0),new(3,0,0),new(.5f,.5f,.5f)))throw new Exception("Spatial reorder changed equal-time source ID policy.");
        if(tieIndex.Sweep(new(-3,0,0),new(3,0,0),new(.5f,.5f,.5f)).TriangleId!=499)throw new Exception("Bounds tie selected the wrong source ID.");
        var distant=new CollisionTriangle(0,new(999999,-1,-1),new(999999,1,-1),new(999999,0,1));
        var distantIndex=new StaticTriangleBoxCollider(new[]{distant});var distantReference=new StaticTriangleBoxCollider(new[]{distant},useBoundsIndex:false);
        if(distantIndex.Sweep(new(999998.5f,0,0),new(999999.5f,0,0),new(.5f,.5f,.5f),true)!=distantReference.Sweep(new(999998.5f,0,0),new(999999.5f,0,0),new(.5f,.5f,.5f),true))throw new Exception("Distant boundary broad phase lost contact.");
        HostAccountingScenarios.Reject<ArgumentOutOfRangeException>(()=>new StaticTriangleBoxCollider(Array.Empty<CollisionTriangle>(),65537));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>new StaticTriangleBoxCollider(Enumerable.Range(0,65537).Select(i=>distant with{Id=i}).ToArray(),65536));
        Console.WriteLine($"PASS: conservative triangle BVH/exhaustive512 queries contacts={contacts} candidates={candidates}/65536; large source=3072 candidates={query.Statistics.CandidateTriangles} nodes={query.Statistics.NodeVisits}, ownership/tie/distant/budget gates");
    }
}
