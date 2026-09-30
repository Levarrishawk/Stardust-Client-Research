using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;
internal static class PortalTransferScenarios
{
    internal static void Run()
    {
        PortalLayoutDescription layout=PortalLayoutDecoder.Decode(PortalLayoutScenarios.Fixture());
        FloorMeshDescription basis=FloorMeshDecoder.Decode(FloorMeshScenarios.Fixture());
        FloorTriangle left=new(new[]{0,1,2},1,new[]{-1,-1,-1},Vector3.UnitY,new byte[3],0,0,new[]{-1,0,-1});
        FloorTriangle right=left with{VertexIndices=new[]{0,1,2},PortalIds=new[]{-1,-1,0}};
        FloorMeshDescription a=basis with{Vertices=new Vector3[]{new(0,0,0),new(2,0,0),new(0,0,2)},Triangles=new[]{left}};
        FloorMeshDescription b=basis with{Vertices=new Vector3[]{new(2,0,0),new(2,0,2),new(0,0,2)},Triangles=new[]{right}};
        Dictionary<int,FloorMeshDescription> floors=new(){[0]=a,[1]=b};PortalFloorTransferQuery query=new(layout,floors);
        Require(query.Links.Count==2,"Directed paired edges");int calls=0;bool allow=true;
        Require(new PortalFloorTransferQuery(layout,floors,maximumPairTests:2).Links.Count==2,"Exact pairing budget");
        HostAccountingScenarios.Reject<InvalidDataException>(()=>new PortalFloorTransferQuery(layout,floors,maximumPairTests:1));
        PortalTransferPermission Permission(PortalFloorLink link){calls++;Require(link.SourceCell==0 && link.TargetCell==1,"Permission destination");return new(allow,true);}
        CellFloorLocator start=new(0,0,new(.25f,0,.25f));Vector3 destination=new(1.75f,0,1.75f);
        CellFloorWalkResult result=query.Walk(start,destination,Permission);
        Require(result.Status==CellFloorWalkStatus.Accepted && result.Transfers==1 && result.Locator.CellIndex==1 && result.WorldPosition==destination && calls==1,"Transfer/remaining motion");
        allow=false;result=query.Walk(start,destination,Permission);Require(result.Status==CellFloorWalkStatus.EntryDenied && result.Transfers==0 && result.Locator.CellIndex==0 && result.WorldPosition==new Vector3(1,0,1),"Current denial retains owner/contact");
        allow=true;Require(query.Walk(start,destination,Permission).Status==CellFloorWalkStatus.Accepted,"No cached permission");
        HostAccountingScenarios.Reject<InvalidOperationException>(()=>query.Walk(start,destination,_=>throw new InvalidOperationException("Synthetic policy failure")));
        Require(query.Walk(start,destination,Permission).Status==CellFloorWalkStatus.Accepted,"Policy failure leaves no retained transition");
        Require(query.Walk(start,destination,_=>new(true,false)).Status==CellFloorWalkStatus.PortalBlocked,"Barrier state");
        Require(query.Walk(start,destination,_=>new(null,true)).Status==CellFloorWalkStatus.PermissionUnknown,"Unknown entry");
        Require(query.Walk(start,destination,_=>new(true,null)).Status==CellFloorWalkStatus.PermissionUnknown,"Unknown barrier");
        PortalCell sourceCell=layout.Cells[0],targetCell=layout.Cells[1];
        Require(new PortalFloorTransferQuery(layout with{Cells=new[]{sourceCell with{Portals=new[]{sourceCell.Portals[0] with{SolidByte=1}}},targetCell}},floors).Links.Count==2,"Raw portal byte does not invent barrier state");
        Require(new PortalFloorTransferQuery(layout,new Dictionary<int,FloorMeshDescription>{{0,a with{Triangles=new[]{left with{PortalIds=new[]{-1,8,-1}}}}},{1,b}}).Walk(start,destination,Permission).Status==CellFloorWalkStatus.UnpairedPortal,"Local portal ID out of range");
        Require(new PortalFloorTransferQuery(layout,new Dictionary<int,FloorMeshDescription>{{0,a},{1,b with{Triangles=new[]{right,right}}}}).Walk(start,destination,Permission).Status==CellFloorWalkStatus.GeometryMismatch,"Ambiguous landing edges");
        Require(query.Walk(new(1,0,destination),start.LocalPosition,_=>new(true,true)).Locator.CellIndex==0,"Reverse transfer");
        Require(new PortalFloorTransferQuery(layout,new Dictionary<int,FloorMeshDescription>{{0,a}}).Walk(start,destination,Permission).Status==CellFloorWalkStatus.MissingTargetFloor,"Missing target floor");
        Require(new PortalFloorTransferQuery(layout,new Dictionary<int,FloorMeshDescription>{{0,a},{1,b with{Vertices=b.Vertices.Select(v=>v+Vector3.UnitX).ToArray()}}}).Walk(start,destination,Permission).Status==CellFloorWalkStatus.GeometryMismatch,"Displaced counterpart");
        Require(query.Walk(start,new(-1,0,.25f),Permission).Status==CellFloorWalkStatus.FloorStopped,"Same-cell blocked edge");
        Require(query.Walk(new(99,0,Vector3.Zero),destination,Permission).Status==CellFloorWalkStatus.InvalidStart,"Missing owner");
        AppearanceTransform pose=new(new(0,0,1),Vector3.UnitY,new(-1,0,0),new(20,7,30));
        // Different local vertex frames, same world edge and world movement.
        Vector3 Inverse(Vector3 world){Vector3 v=world-pose.Translation;return new(-v.Z,v.Y,v.X);}
        PortalFloorTransferQuery transformed=new(layout,new Dictionary<int,FloorMeshDescription>{{0,a},{1,b with{Vertices=b.Vertices.Select(v=>Inverse(v)).ToArray()}}},new Dictionary<int,AppearanceTransform>{{1,pose}});
        result=transformed.Walk(start,destination,_=>new(true,true));Require(result.Status==CellFloorWalkStatus.Accepted && result.WorldPosition==destination && result.Locator.LocalPosition==Inverse(destination),"Asymmetric owner transform/inverse");
        HostAccountingScenarios.Reject<InvalidDataException>(()=>new PortalFloorTransferQuery(layout,floors,new Dictionary<int,AppearanceTransform>{{1,pose with{RowX=pose.RowX*2}}}));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>query.Walk(start,new(float.NaN,0,0),Permission));
        Vector3[] mutable=b.Vertices.ToArray();query=new(layout,new Dictionary<int,FloorMeshDescription>{{0,a},{1,b with{Vertices=mutable}}});Array.Clear(mutable);
        Require(query.Walk(start,destination,_=>new(true,true)).Status==CellFloorWalkStatus.Accepted,"Owned floor instances");
        FloorMeshDescription Square(int cell)
        {
            FloorTriangle first=left with{VertexIndices=new[]{0,1,2},Neighbors=new[]{-1,1,-1},EdgeFlags=new byte[]{0,1,0},PortalIds=new[]{-1,-1,cell==0?-1:0}};
            FloorTriangle second=left with{VertexIndices=new[]{1,3,2},Neighbors=new[]{-1,-1,0},EdgeFlags=new byte[]{0,0,1},PortalIds=new[]{cell==2?-1:cell==0?0:1,-1,-1}};
            return basis with{Vertices=new Vector3[]{new(cell*2,0,0),new(cell*2+2,0,0),new(cell*2,0,2),new(cell*2+2,0,2)},Triangles=new[]{first,second}};
        }
        PortalLayoutDescription chain=layout with{Geometry=new[]{layout.Geometry[0],layout.Geometry[0]},Cells=new[]{sourceCell,targetCell with{Portals=new[]{targetCell.Portals[0],sourceCell.Portals[0] with{GeometryIndex=1,TargetCell=2}}},targetCell with{Portals=new[]{targetCell.Portals[0] with{GeometryIndex=1,TargetCell=1}}}}};
        PortalFloorTransferQuery bounded=new(chain,new Dictionary<int,FloorMeshDescription>{{0,Square(0)},{1,Square(1)},{2,Square(2)}});calls=0;
        result=bounded.Walk(new(0,0,new(.5f,0,.75f)),new(5.5f,0,.75f),Permission);
        Require(result.Status==CellFloorWalkStatus.TransferLimit && result.Transfers==1 && result.Locator.CellIndex==1 && result.WorldPosition==new Vector3(4,0,.75f) && calls==1,"Second portal stop/no extra permission call");
        Console.WriteLine("PASS: paired floor portal transfer, local reference IDs, remaining motion, owner transforms, current permission/barrier state, rejection ownership and snapshots");
    }
    private static void Require(bool value,string label){if(!value)throw new Exception("Portal transfer: "+label);}
    internal static void ProbeOptional(string[] args)
    {
        if(args.Length!=8 || args[2]!="--configuration" || args[4]!="--member" || args[6]!="--expected-sha256")throw new ArgumentException("Portal transfers need pinned layout/configuration.");
        using StreamReader config=File.OpenText(args[3]);using ConfiguredTreOverlay overlay=ConfiguredTreOverlay.Open(args[1],config);
        PortalSceneBundle scene=PortalSceneBundleReader.Load(overlay.Lookup,args[5],args[7],loadFloors:true);PortalFloorTransferQuery query=new(scene.Layout,scene.CellFloors);
        foreach(var source in scene.Sources)Console.WriteLine("transfer_source="+JsonSerializer.Serialize(source.Value));
        foreach(PortalFloorLink link in query.Links)
        {
            Vector3 Center(int cell,int triangle){FloorMeshDescription floor=scene.CellFloors[cell];var indices=floor.Triangles[triangle].VertexIndices;return (floor.Vertices[indices[0]]+floor.Vertices[indices[1]]+floor.Vertices[indices[2]])/3;}
            Vector3 start=Center(link.SourceCell,link.SourceTriangle),end=Center(link.TargetCell,link.TargetTriangle);
            foreach(string policy in new[]{"allow","deny","barrier","unknown"})
            {
                CellFloorWalkResult result=query.Walk(new(link.SourceCell,link.SourceTriangle,start),end,_=>policy switch{"allow"=>new(true,true),"deny"=>new(false,true),"barrier"=>new(true,false),_=>new(null,true)});
                Console.WriteLine("transfer="+JsonSerializer.Serialize(new{link,policy,status=result.Status.ToString(),cell=result.Locator.CellIndex,triangle=result.Locator.TriangleIndex,parameter=result.Parameter,transfers=result.Transfers,position=new[]{result.WorldPosition.X,result.WorldPosition.Y,result.WorldPosition.Z}}));
            }
        }
        Console.WriteLine($"portal_transfer_links={query.Links.Count}");
    }
}
