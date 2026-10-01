using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class PortalTriangleScenarios
{
    internal static void Run()
    {
        var (layout,floors)=PortalFloorBoxScenarios.Fixture();
        var start=new CellFloorLocator(0,0,new(-1,-.5f,.25f));Vector3 end=new(1,0,.25f),half=new(.25f,.5f,.25f);
        PortalTransferPermission Allowed(PortalFloorLink _)=>new(true,true);
        CollisionTriangle[] Wall(float x)=>new[]{new CollisionTriangle(3,new(x,-2,-1),new(x,3,-1),new(x,-2,2)),new CollisionTriangle(7,new(x,3,-1),new(x,3,2),new(x,-2,2))};
        PortalFloorTriangleBoxWalker Make(float x)=>new(layout,floors,new Dictionary<int,IReadOnlyList<CollisionTriangle>>{{1,Wall(x)}});
        var empty=new PortalFloorTriangleBoxWalker(layout,floors,new Dictionary<int,IReadOnlyList<CollisionTriangle>>());
        var clear=empty.Walk(start,end,half,Allowed);
        Require(clear.Status==FloorBoxWalkStatus.Accepted&&clear.Movement.Transfers==1&&clear.Movement.Locator.CellIndex==1&&clear.Movement.WorldPosition==new Vector3(1,.5f,.25f),"clear slope owner transfer");
        var sourceWall=Make(.2f);var r=sourceWall.Walk(start,end,half,Allowed,true);
        Require(r.Status==FloorBoxWalkStatus.BoxStopped&&r.Movement.Transfers==0&&r.Movement.Locator.CellIndex==0&&r.Contact!.ColliderCell==1&&r.Contact.PathCell==0&&Math.Abs(r.Movement.WorldPosition.X+.05)<1e-5,"target triangle stops body before feet seam");
        Require(sourceWall.Walk(r.Movement.Locator,start.LocalPosition,half,Allowed,true).Status==FloorBoxWalkStatus.Accepted,"source contact retreat");
        var targetWall=Make(.8f);r=targetWall.Walk(start,end,half,Allowed,true);
        Require(r.Status==FloorBoxWalkStatus.BoxStopped&&r.Movement.Transfers==1&&r.Movement.Locator.CellIndex==1&&r.Contact!.PathCell==1&&Math.Abs(r.Movement.Parameter-.775)<1e-6&&Math.Abs(r.Movement.WorldPosition.Y-.275)<1e-6,"target contact global progress and supported height");
        var held=r.Movement.Locator;
        var clearance=targetWall.Walk(start,end,half,Allowed,true,feetClearance:.0001f);
        Require(clearance.Status==FloorBoxWalkStatus.BoxStopped&&Math.Abs(clearance.Movement.WorldPosition.Y-.275)<1e-6&&Math.Abs(clearance.Contact!.World.Center.Y-(.775+.0001))<1e-6,"explicit clearance preserves target floor height");
        for(int i=0;i<100;i++){r=targetWall.Walk(r.Movement.Locator,end,half,Allowed,true);Require(r.Movement.Locator==held,"repeat target contact stability");}
        r=targetWall.Walk(held,start.LocalPosition,half,Allowed,true);
        Require(r.Status==FloorBoxWalkStatus.Accepted&&r.Movement.Locator.CellIndex==0&&r.Movement.Transfers==1,"reverse contact retreat and owner transfer");
        var denied=empty.Walk(start,end,half,_=>new(false,true));Require(denied.Movement.Status==CellFloorWalkStatus.EntryDenied&&denied.Movement.Transfers==0,"permission preserves source");
        AppearanceTransform pose=new(new(0,0,1),Vector3.UnitY,new(-1,0,0),new(30,4,-11));
        var rotated=new PortalFloorTriangleBoxWalker(layout,floors,new Dictionary<int,IReadOnlyList<CollisionTriangle>>{{1,Wall(.8f)}},new Dictionary<int,AppearanceTransform>{{0,pose},{1,pose}});
        r=rotated.Walk(start,pose.TransformPoint(end),half,Allowed,true);
        Require(r.Status==FloorBoxWalkStatus.BoxStopped&&r.Movement.Locator.CellIndex==1&&Vector3.Distance(r.Movement.WorldPosition,pose.TransformPoint(new(.55f,.275f,.25f)))<1e-4,"rotated translated triangle owner");
        AppearanceTransform targetPose=new(new(0,0,1),Vector3.UnitY,new(-1,0,0),new(20,7,30));
        Vector3 Inverse(Vector3 p)=>new(30-p.Z,p.Y-7,p.X-20);
        var asymmetricFloors=new Dictionary<int,FloorMeshDescription>{{0,floors[0]},{1,floors[1] with{Vertices=floors[1].Vertices.Select(Inverse).ToArray()}}};
        var asymmetricWall=Wall(.8f).Select(t=>t with{A=Inverse(t.A),B=Inverse(t.B),C=Inverse(t.C)}).ToArray();
        var asymmetric=new PortalFloorTriangleBoxWalker(layout,asymmetricFloors,new Dictionary<int,IReadOnlyList<CollisionTriangle>>{{1,asymmetricWall}},new Dictionary<int,AppearanceTransform>{{1,targetPose}});
        r=asymmetric.Walk(start,end,half,Allowed,true);
        Require(r.Status==FloorBoxWalkStatus.BoxStopped&&r.Movement.Locator.CellIndex==1&&Math.Abs(r.Movement.WorldPosition.X-.55)<1e-4&&Vector3.Distance(r.Movement.Locator.LocalPosition,Inverse(r.Movement.WorldPosition))<1e-4,"asymmetric target floor and triangle transforms");
        Require(asymmetric.Walk(r.Movement.Locator,start.LocalPosition,half,Allowed,true).Movement.Locator.CellIndex==0,"asymmetric reverse transition");
        var supplied=Wall(.8f);var owned=new PortalFloorTriangleBoxWalker(layout,floors,new Dictionary<int,IReadOnlyList<CollisionTriangle>>{{1,supplied}});Array.Clear(supplied);
        Require(owned.Walk(start,end,half,Allowed,true).Status==FloorBoxWalkStatus.BoxStopped,"owned triangles survive caller mutation");
        HostAccountingScenarios.Reject<InvalidDataException>(()=>new PortalFloorTriangleBoxWalker(layout,floors,new Dictionary<int,IReadOnlyList<CollisionTriangle>>{{9,Wall(.8f)}}));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>new PortalFloorTriangleBoxWalker(layout,floors,new Dictionary<int,IReadOnlyList<CollisionTriangle>>{{1,Enumerable.Range(0,2049).Select(i=>Wall(.8f)[0] with{Id=i}).ToArray()}}));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>targetWall.Walk(start,end,new(float.NaN,1,1),Allowed));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>empty.Walk(start,end,half,Allowed,feetClearance:.1f));
        HostAccountingScenarios.Reject<InvalidOperationException>(()=>targetWall.Walk(start,end,half,_=>throw new InvalidOperationException("policy")));
        Require(targetWall.Walk(start,end,half,Allowed,true).Status==FloorBoxWalkStatus.BoxStopped,"callback failure leaves query reusable");
        Console.WriteLine("PASS: portal triangle source approach, target slope support/global progress,100 holds/retreat, transformed owners, owned inputs and negative gates");
    }
    private static void Require(bool value,string message){if(!value)throw new Exception("Portal triangles: "+message);}
    internal static void ProbeOptional(string[] args)
    {
        if(args.Length!=4||args[2]!="--expected-sha256")throw new ArgumentException("Pinned portal triangle fixture required.");
        byte[] bytes=File.ReadAllBytes(args[1]);if(bytes.Length>2_000_000||Convert.ToHexString(SHA256.HashData(bytes))!=args[3])throw new InvalidDataException("Portal triangle fixture identity/budget.");
        using var doc=JsonDocument.Parse(bytes);if(doc.RootElement.GetArrayLength()>1024)throw new InvalidDataException("Portal triangle fixture count.");
        Vector3 V(JsonElement p)=>new(p[0].GetSingle(),p[1].GetSingle(),p[2].GetSingle());
        int index=0;
        foreach(var row in doc.RootElement.EnumerateArray())
        {
            var (layout,floors)=PortalFloorBoxScenarios.Fixture();var p=row.GetProperty("owner");AppearanceTransform pose=new(V(p[0]),V(p[1]),V(p[2]),V(p[3]));
            var triangles=row.GetProperty("triangles").EnumerateArray().Select((t,i)=>new CollisionTriangle(i,V(t[0]),V(t[1]),V(t[2]))).ToArray();
            var walker=new PortalFloorTriangleBoxWalker(layout,floors,new Dictionary<int,IReadOnlyList<CollisionTriangle>>{{row.GetProperty("collider_cell").GetInt32(),triangles}},new Dictionary<int,AppearanceTransform>{{0,pose},{1,pose}});
            var start=new CellFloorLocator(row.GetProperty("cell").GetInt32(),row.GetProperty("triangle").GetInt32(),V(row.GetProperty("start")));
            bool permit=row.GetProperty("permit").GetBoolean();var result=walker.Walk(start,V(row.GetProperty("end")),V(row.GetProperty("half")),_=>new(permit,true),true);
            var r=result.Movement;var repeat=walker.Walk(r.Locator,r.WorldPosition,V(row.GetProperty("half")),_=>new(true,true),true);
            Console.WriteLine("portal_triangle="+JsonSerializer.Serialize(new{index=index++,status=result.Status.ToString(),movement=r.Status.ToString(),cell=r.Locator.CellIndex,triangle=r.Locator.TriangleIndex,local=InteriorDoorScenarios.V(r.Locator.LocalPosition),world=InteriorDoorScenarios.V(r.WorldPosition),parameter=r.Parameter,transfers=r.Transfers,diagnostic=result.Diagnostic,
                reusable=repeat.Movement.Locator.CellIndex==r.Locator.CellIndex,
                contact=result.Contact is null?null:new{cell=result.Contact.ColliderCell,path_cell=result.Contact.PathCell,status=result.Contact.World.Status.ToString(),parameter=result.Contact.World.Parameter,center=new[]{result.Contact.World.Center.X,result.Contact.World.Center.Y,result.Contact.World.Center.Z},id=result.Contact.World.TriangleId}}));
        }
    }
}
