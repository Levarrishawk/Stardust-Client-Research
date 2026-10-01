using System.Numerics;
using Stardust.Core.Assets;

internal static class PortalLandingPrecisionScenarios
{
    internal static void Run()
    {
        var layout=PortalLayoutDecoder.Decode(PortalLayoutScenarios.Fixture());
        var basis=FloorMeshDecoder.Decode(FloorMeshScenarios.Fixture());
        var random=new Random(2026093023);int transfers=0;
        for(int i=0;i<1000;i++)
        {
            double angle=random.NextDouble()*Math.PI*2;Vector3 axis=new((float)Math.Cos(angle),0,(float)Math.Sin(angle));
            Vector3 normal=new(-axis.Z,0,axis.X),center=new(-30+(float)random.NextDouble()*3,0,17+(float)random.NextDouble()*3);
            Vector3 a=center-axis,b=center+axis,source=center+normal*.6f-axis*.6f,target=center-normal*.25f-axis*.9f;
            FloorTriangle row=new(new[]{0,1,2},0,new[]{-1,-1,-1},Vector3.UnitY,new byte[3],0,0,new[]{0,-1,-1});
            var floors=new Dictionary<int,FloorMeshDescription>{[0]=basis with{Vertices=new[]{a,b,source},Triangles=new[]{row}},[1]=basis with{Vertices=new[]{b,a,target},Triangles=new[]{row}}};
            var query=new PortalFloorTransferQuery(layout,floors);Vector3 start=center+normal*.05f,end=center-normal*.05f;
            var result=query.Walk(new(0,0,start),end,_=>new(true,true));
            if(result.Status!=CellFloorWalkStatus.Accepted || result.Transfers!=1 || result.Locator.CellIndex!=1 || Vector3.Distance(result.WorldPosition,end)>1e-5f)
                throw new Exception($"Slanted landing precision {i}: {result.Status}");
            if(query.Walk(result.Locator,result.WorldPosition,_=>throw new Exception("No-op requested permission")).Status!=CellFloorWalkStatus.Accepted)
                throw new Exception("Slanted landing not reusable");
            transfers++;
        }
        Console.WriteLine($"PASS: synthetic slanted thin target landings; reusable_transfers={transfers}");
    }
}
