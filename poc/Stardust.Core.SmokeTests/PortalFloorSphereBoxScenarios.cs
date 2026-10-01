using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class PortalFloorSphereBoxScenarios
{
    internal static void Run()
    {
        var (layout,floors)=PortalFloorBoxScenarios.Fixture();var walker=new PortalFloorSphereBoxWalker(layout,floors);CellFloorLocator start=new(0,0,new(-1,-.5f,.25f));Vector3 end=new(1,0,.25f),centerOffset=new(0,.5f,0);float radius=.25f;
        PortalTransferPermission Allowed(PortalFloorLink _)=>new(true,true);
        PortalFloorBox Box(int cell,int id,float lo,float hi)=>new(cell,id,new(new(lo,-2,-1),new(hi,3,2),AppearanceTransform.Identity),false);
        var result=walker.Walk(start,end,centerOffset,radius,Array.Empty<PortalFloorBox>(),Allowed);
        Require(result.Status==FloorBoxWalkStatus.Accepted && result.Movement.Transfers==1 && result.Movement.Locator.CellIndex==1 && result.Movement.WorldPosition==new Vector3(1,.5f,.25f),"Unobstructed slope transfer");
        result=walker.Walk(start,end,centerOffset,radius,new[]{Box(1,7,.2f,.3f)},Allowed,true);
        Require(result.Status==FloorBoxWalkStatus.BoxStopped && result.Movement.Transfers==0 && result.Movement.Locator.CellIndex==0 && result.Contact!.ColliderCell==1 && result.Contact.PathCell==0 && Math.Abs(result.Movement.WorldPosition.X+.05)<1e-5,"Target box blocks actor before feet seam");
        var retreat=walker.Walk(result.Movement.Locator,start.LocalPosition,centerOffset,radius,new[]{Box(1,7,.2f,.3f)},Allowed,true);
        Require(retreat.Status==FloorBoxWalkStatus.Accepted && retreat.Movement.Locator.CellIndex==0 && retreat.Movement.WorldPosition==start.LocalPosition,"Source-side contact retreat");
        var targetBox=Box(1,9,.8f,.9f);result=walker.Walk(start,end,centerOffset,radius,new[]{targetBox},Allowed,true);
        Require(result.Status==FloorBoxWalkStatus.BoxStopped && result.Movement.Transfers==1 && result.Movement.Locator.CellIndex==1 && result.Contact!.PathCell==1 && Math.Abs(result.Movement.Parameter-.775)<1e-6 && Math.Abs(result.Movement.WorldPosition.Y-.275)<1e-6,"Target contact maps remaining parameter and height");
        var stopped=result.Movement.Locator;
        for(int i=0;i<100;i++){result=walker.Walk(result.Movement.Locator,end,centerOffset,radius,new[]{targetBox},Allowed,true);Require(result.Movement.Locator==stopped,"Persistent target contact no drift");}
        result=walker.Walk(stopped,start.LocalPosition,centerOffset,radius,new[]{targetBox},Allowed,true);
        Require(result.Status==FloorBoxWalkStatus.Accepted && result.Movement.Transfers==1 && result.Movement.Locator.CellIndex==0 && result.Movement.WorldPosition==start.LocalPosition,"Reverse slope owner transfer after retreat");
        var denied=walker.Walk(start,end,centerOffset,radius,Array.Empty<PortalFloorBox>(),_=>new(false,true));Require(denied.Movement.Status==CellFloorWalkStatus.EntryDenied && denied.Movement.Transfers==0,"Feet permission remains explicit");
        var unknown=walker.Walk(start,end,centerOffset,radius,new[]{targetBox with{PassageAllowed=null}},Allowed);Require(unknown.Contact!.World.Status==PortalBarrierContactStatus.PermissionUnknown && unknown.Movement.Locator.CellIndex==1,"Unknown physical box passability");
        var allowedBox=walker.Walk(start,end,centerOffset,radius,new[]{targetBox with{PassageAllowed=true}},Allowed);Require(allowedBox.Status==FloorBoxWalkStatus.Accepted,"Explicit allowed box skips");
        var tied=walker.Walk(start,end,centerOffset,radius,new[]{targetBox with{Id=11},targetBox with{Id=10}},Allowed);Require(tied.Contact!.ColliderId==11,"Caller order breaks equal box ties");
        PortalFloorBox[] snapshots={targetBox};result=walker.Walk(start,end,centerOffset,radius,snapshots,_=>{snapshots[0]=targetBox with{PassageAllowed=true};return new(true,true);});Require(result.Status==FloorBoxWalkStatus.BoxStopped,"Box snapshot precedes external permission callback");
        HostAccountingScenarios.Reject<InvalidDataException>(()=>walker.Walk(start,end,centerOffset,radius,new[]{targetBox,targetBox},Allowed));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>walker.Walk(start,end,centerOffset,radius,new[]{targetBox with{CellIndex=9}},Allowed));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>walker.Walk(start,end,centerOffset,radius,Enumerable.Repeat(targetBox,65).ToArray(),Allowed));
        HostAccountingScenarios.Reject<InvalidOperationException>(()=>walker.Walk(start,end,centerOffset,radius,Array.Empty<PortalFloorBox>(),_=>throw new InvalidOperationException("policy")));
        Require(walker.Walk(start,end,centerOffset,radius,Array.Empty<PortalFloorBox>(),Allowed).Status==FloorBoxWalkStatus.Accepted,"Callback failure leaves no adoption");
        AppearanceTransform pose=new(new(0,0,1),Vector3.UnitY,new(-1,0,0),new(30,4,-11));
        var transformed=new PortalFloorSphereBoxWalker(layout,floors,new Dictionary<int,AppearanceTransform>{{0,pose},{1,pose}});
        var transformedBox=targetBox with{Collider=new(targetBox.Collider.Minimum,targetBox.Collider.Maximum,pose)};
        result=transformed.Walk(start,pose.TransformPoint(end),centerOffset,radius,new[]{transformedBox},Allowed,true);
        Require(result.Status==FloorBoxWalkStatus.BoxStopped && result.Movement.Locator.CellIndex==1 && Math.Abs(result.Movement.Parameter-.775)<1e-6 && Vector3.Distance(result.Movement.WorldPosition,pose.TransformPoint(new(.55f,.275f,.25f)))<1e-4,"Transformed world actor/normal/contact");
        AppearanceTransform targetPose=new(new(0,0,1),Vector3.UnitY,new(-1,0,0),new(20,7,30));
        Vector3 Inverse(Vector3 p)=>new(30-p.Z,p.Y-7,p.X-20);
        var asymmetricFloors=new Dictionary<int,FloorMeshDescription>{{0,floors[0]},{1,floors[1] with{Vertices=floors[1].Vertices.Select(Inverse).ToArray()}}};
        var asymmetric=new PortalFloorSphereBoxWalker(layout,asymmetricFloors,new Dictionary<int,AppearanceTransform>{{1,targetPose}});
        result=asymmetric.Walk(start,end,centerOffset,radius,new[]{targetBox},Allowed,true);
        Require(result.Status==FloorBoxWalkStatus.BoxStopped && result.Movement.Locator.CellIndex==1 && Vector3.Distance(result.Movement.Locator.LocalPosition,Inverse(result.Movement.WorldPosition))<1e-4 && Math.Abs(result.Movement.WorldPosition.X-.55)<1e-4,"Asymmetric target-owner contact landing");
        result=asymmetric.Walk(result.Movement.Locator,start.LocalPosition,centerOffset,radius,new[]{targetBox},Allowed,true);
        Require(result.Status==FloorBoxWalkStatus.Accepted && result.Movement.Locator.CellIndex==0 && result.Movement.Transfers==1,"Asymmetric target-owner reverse transition");
        var largePose=AppearanceTransform.Identity with{Translation=new(999998,0,999998)};
        var largeWalker=new PortalFloorSphereBoxWalker(layout,floors,new Dictionary<int,AppearanceTransform>{{0,largePose},{1,largePose}});
        var largeBox=targetBox with{Collider=new(targetBox.Collider.Minimum,targetBox.Collider.Maximum,largePose)};
        result=largeWalker.Walk(start,largePose.TransformPoint(end),centerOffset,radius,new[]{largeBox},Allowed,true);
        Require(result.Status==FloorBoxWalkStatus.ContactLandingUnsupported && result.Movement.Locator.CellIndex==0 && result.Movement.Parameter==0 && result.Movement.Transfers==0,"Unrepresentable world transform rolls back entire query");
        Console.WriteLine("PASS: precise portal slope transfer, target-box source approach, target parameter/landing/repeat/reverse, current policy, ties/snapshots, malformed budget and transformed-owner gates");
    }
    private static void Require(bool value,string label){if(!value)throw new Exception("Portal floor box: "+label);}
    internal static void ProbeOptional(string[] args)
    {
        if(args.Length!=4 || args[2]!="--expected-sha256")throw new ArgumentException("Pinned portal box fixture required.");
        byte[] bytes=File.ReadAllBytes(args[1]);if(bytes.Length>8_000_000 || Convert.ToHexString(SHA256.HashData(bytes))!=args[3])throw new InvalidDataException("Portal box fixture identity/budget.");
        using JsonDocument doc=JsonDocument.Parse(bytes);if(doc.RootElement.GetArrayLength()>10000)throw new InvalidDataException("Portal box fixture count.");
        Vector3 V(JsonElement p)=>new(p[0].GetSingle(),p[1].GetSingle(),p[2].GetSingle());
        AppearanceTransform Pose(JsonElement p)=>new(V(p[0]),V(p[1]),V(p[2]),V(p[3]));
        bool? B(JsonElement p)=>p.ValueKind==JsonValueKind.Null?null:p.GetBoolean();
        int index=0;
        foreach(var row in doc.RootElement.EnumerateArray())
        {
            var (layout,floors)=PortalFloorBoxScenarios.Fixture();var pose=Pose(row.GetProperty("owner"));var walker=new PortalFloorSphereBoxWalker(layout,floors,new Dictionary<int,AppearanceTransform>{{0,pose},{1,pose}});
            var boxes=row.GetProperty("boxes").EnumerateArray().Select(b=>new PortalFloorBox(b.GetProperty("cell").GetInt32(),b.GetProperty("id").GetInt32(),
                new PortalBarrierCollider(V(b.GetProperty("minimum")),V(b.GetProperty("maximum")),Pose(b.GetProperty("pose"))),B(b.GetProperty("allowed")))).ToArray();
            var start=new CellFloorLocator(row.GetProperty("cell").GetInt32(),row.GetProperty("triangle").GetInt32(),V(row.GetProperty("start")));
            var permission=row.GetProperty("permission");var result=walker.Walk(start,V(row.GetProperty("end")),V(row.GetProperty("center_offset")),row.GetProperty("radius").GetSingle(),boxes,_=>new(B(permission[0]),B(permission[1])),true);
            var r=result.Movement;var repeat=walker.Walk(r.Locator,r.WorldPosition,V(row.GetProperty("center_offset")),row.GetProperty("radius").GetSingle(),Array.Empty<PortalFloorBox>(),_=>new(true,true));
            Console.WriteLine("portal_sphere_box="+JsonSerializer.Serialize(new{index=index++,status=result.Status.ToString(),movement=r.Status.ToString(),cell=r.Locator.CellIndex,triangle=r.Locator.TriangleIndex,local=InteriorDoorScenarios.V(r.Locator.LocalPosition),world=InteriorDoorScenarios.V(r.WorldPosition),parameter=r.Parameter,transfers=r.Transfers,diagnostic=result.Diagnostic,
                reusable=repeat.Movement.Status==CellFloorWalkStatus.Accepted && repeat.Movement.Locator.CellIndex==r.Locator.CellIndex,
                contact=result.Contact is null?null:new{cell=result.Contact.ColliderCell,id=result.Contact.ColliderId,path_cell=result.Contact.PathCell,status=result.Contact.World.Status.ToString(),parameter=result.Contact.World.Parameter,center=new[]{result.Contact.World.Center.X,result.Contact.World.Center.Y,result.Contact.World.Center.Z},normal=InteriorDoorScenarios.V(result.Contact.World.Normal)}}));
        }
    }
}
