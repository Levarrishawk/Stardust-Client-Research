using System.Numerics;

namespace Stardust.Core.Assets;

public sealed record FloorTriangleSphereWalkResult(FloorBoxWalkStatus Status,FloorWalkResult Floor,
    FloorPathTriangleSphereContact? Contact,string? Diagnostic=null);

/// <summary>Same-space supported floor contact adoption against owned triangles; no steps, portals or sliding.</summary>
public sealed class SingleCellFloorTriangleSphereWalker
{
    private readonly SingleCellFloorWalker floor;
    private readonly StaticTriangleSphereCollider collider;
    public SingleCellFloorTriangleSphereWalker(FloorMeshDescription description,IReadOnlyList<CollisionTriangle> triangles,int maximumTriangles=2048)
        :this(new SingleCellFloorWalker(description),new StaticTriangleSphereCollider(triangles,maximumTriangles)){}
    internal SingleCellFloorTriangleSphereWalker(SingleCellFloorWalker floor,StaticTriangleSphereCollider collider)
    {
        this.floor=floor;this.collider=collider;
    }
    public FloorTriangleSphereWalkResult Walk(int triangle,Vector3 start,Vector3 desired,Vector3 centerOffset,float radius,
        bool allowSeparatingContact=false,int maximumCrossings=4096)
    {
        var path=floor.Trace(triangle,start,desired,maximumCrossings);
        var contact=FloorPathTriangleSphereSweep.Sweep(path,collider,centerOffset,radius,allowSeparatingContact);
        if(contact.Status==TriangleSphereContactStatus.Clear)
            return new(path.Result.Status==FloorWalkStatus.Accepted?FloorBoxWalkStatus.Accepted:FloorBoxWalkStatus.FloorStopped,path.Result,null);
        var feet=new FloorPathPoint(contact.Center.X-centerOffset.X,contact.Center.Y-centerOffset.Y,contact.Center.Z-centerOffset.Z);
        bool Validate(Vector3 position)
        {
            var candidate=floor.Trace(triangle,start,position,maximumCrossings);
            var stationary=floor.Trace(candidate.Result.TriangleIndex,position,position);
            if(FloorPathTriangleSphereSweep.Sweep(stationary,collider,centerOffset,radius,true).Status!=TriangleSphereContactStatus.Clear)return false;
            var swept=FloorPathTriangleSphereSweep.Sweep(candidate,collider,centerOffset,radius,true);
            return swept.Status==TriangleSphereContactStatus.Clear||
                swept.Status==TriangleSphereContactStatus.Blocked&&swept.Parameter==candidate.Result.Parameter;
        }
        var landing=FloorBoxContactLanding.Resolve(floor,path,triangle,start,feet,contact.Normal,
            contact.SegmentIndex,contact.Parameter,maximumCrossings,Validate);
        return new(landing.Accepted?FloorBoxWalkStatus.BoxStopped:FloorBoxWalkStatus.ContactLandingUnsupported,
            landing.Floor,contact,landing.Accepted?null:"UnrepresentableTriangleContactLanding");
    }
}
