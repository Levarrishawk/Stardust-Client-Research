using System.Numerics;
using Stardust.Core.Assets;

internal static class FloorContactScenarios
{
    internal static void Run()
    {
        var basis=FloorMeshDecoder.Decode(FloorMeshScenarios.Fixture());Random random=new(73121);int corrected=0,held=0;const int count=5000;
        for(int i=0;i<count;i++)
        {
            float origin=i<4000?0:10_000;
            Vector3[] vertices={new(origin+(float)random.NextDouble()*10,1.330452f,origin+(float)random.NextDouble()*10),new(origin+15+(float)random.NextDouble()*10,1.330452f,origin+(float)random.NextDouble()*10),new(origin+(float)random.NextDouble()*10,1.330452f,origin+15+(float)random.NextDouble()*10)};
            var triangle=new FloorTriangle(new[]{0,1,2},0,new[]{-1,-1,-1},Vector3.UnitY,new byte[]{1,1,1},0,0,new[]{-1,-1,-1});var walker=new SingleCellFloorWalker(basis with{Vertices=vertices,Triangles=new[]{triangle}});
            Vector3 start=(vertices[0]+vertices[1]+vertices[2])/3;float angle=(float)random.NextDouble()*MathF.Tau;Vector3 desired=start+new Vector3(MathF.Cos(angle)*50,0,MathF.Sin(angle)*50);
            var result=walker.Walk(0,start,desired);Require(result.Status is FloorWalkStatus.OpenBoundary or FloorWalkStatus.UnrepresentableContact,"Explicit boundary/canonicalization result");
            Require(walker.Walk(result.TriangleIndex,result.Position,result.Position).Status==FloorWalkStatus.Accepted,"Every returned contact remains a valid start");
            if(result.Status==FloorWalkStatus.UnrepresentableContact){Require(result.Parameter==0 && result.Position==start && result.EdgeIndex==-1,"Precision failure holds canonical initial locator");held++;continue;}
            Vector3 exact=new((float)(start.X+((double)desired.X-start.X)*result.Parameter),start.Y,(float)(start.Z+((double)desired.Z-start.Z)*result.Parameter));
            double dx=(double)result.Position.X-exact.X,dy=(double)result.Position.Y-exact.Y,dz=(double)result.Position.Z-exact.Z;
            Require(dx*dx+dy*dy+dz*dz<=1e-8,"Contact correction bounded to 1e-4 metres");
            if(result.Position!=exact)corrected++;
            var retreat=walker.Walk(result.TriangleIndex,result.Position,start);Require(retreat.Status==FloorWalkStatus.Accepted && retreat.Position==start,"Returned boundary permits inward retreat");
            // External invalid starts remain strict even after the output contact fix.
            Require(walker.Walk(0,desired,start).Status==FloorWalkStatus.InvalidStart,"Outside start remains rejected");
        }
        Require(corrected>500 && held>100,"Rounded and precision-budget cases exercised");
        Console.WriteLine($"PASS: {count} seeded returned floor contacts remain reusable; bounded corrections={corrected} precision holds={held} retreat and invalid-input gates");
    }
    private static void Require(bool value,string label){if(!value)throw new Exception("Floor contact: "+label);}
}
