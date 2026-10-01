using System.Numerics;
using Stardust.Core.Assets;

internal static class FloorContactHeightScenarios
{
    internal static void Run()
    {
        var basis=FloorPathScenarios.Fixture();Random random=new(6030926);int held=0,contacts=0;
        for(int i=0;i<1000;i++)
        {
            float z=(float)(10+random.NextDouble()*90),width=(float)(.005+random.NextDouble()*.01);
            Vector3[] vertices={new(0,0,z),new(width,1,z),new(0,2,z+width)};
            var triangle=new FloorTriangle(new[]{0,1,2},0,new[]{-1,-1,-1},Vector3.UnitY,new byte[]{1,1,1},0,0,new[]{-1,-1,-1});
            var walker=new SingleCellFloorWalker(basis with{Vertices=vertices,Triangles=new[]{triangle}});
            Vector3 start=new(width/3,0,(float)(((double)vertices[0].Z+vertices[1].Z+vertices[2].Z)/3));
            // Independent authored plane equation on the float XZ coordinates supplied to the API.
            start.Y=(float)((double)start.X/width+2*((double)start.Z-z)/((double)vertices[2].Z-z));
            start=walker.Walk(0,start,start).Position;
            Vector3 desired=start+new Vector3((float)(random.NextDouble()-.5)*.1f,0,(float)(random.NextDouble()-.5)*.1f);
            var result=walker.Walk(0,start,desired);
            Require(result.Status is FloorWalkStatus.Accepted or FloorWalkStatus.OpenBoundary or FloorWalkStatus.UnrepresentableContact,"Supported steep boundary outcome");
            Require(walker.Walk(result.TriangleIndex,result.Position,result.Position).Status==FloorWalkStatus.Accepted,"Rounded steep contact height reusable");
            double y=(double)result.Position.X/width+2*((double)result.Position.Z-z)/((double)vertices[2].Z-z);
            Require(Math.Abs(result.Position.Y-(float)y)<1e-6,"Stored locator agrees with independent rounded-coordinate plane");
            if(result.Status==FloorWalkStatus.UnrepresentableContact){Require(result.Parameter==0 && result.Position==start,"Height precision hold retains source");held++;}else contacts++;
        }
        Require(held>100 && contacts>50,"Steep height correction and budget holds exercised");
        Console.WriteLine($"PASS: 1000 independent steep-plane contact heights/reuse; bounded contacts={contacts} precision holds={held}");
    }
    private static void Require(bool value,string label){if(!value)throw new Exception("Floor contact height: "+label);}
}
