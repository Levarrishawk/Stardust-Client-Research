using System.Numerics;

namespace Stardust.Core.Assets;

public sealed record FloorTriangleBoxWalkResult(FloorBoxWalkStatus Status,FloorWalkResult Floor,
    FloorPathTriangleContact? Contact,string? Diagnostic=null);

/// <summary>Same-space supported floor contact adoption against owned triangles; no steps, portals or sliding.</summary>
public sealed class SingleCellFloorTriangleBoxWalker
{
    private readonly SingleCellFloorWalker floor;
    private readonly StaticTriangleBoxCollider collider;
    public SingleCellFloorTriangleBoxWalker(FloorMeshDescription description,IReadOnlyList<CollisionTriangle> triangles)
    {
        floor=new(description);collider=new(triangles);
    }
    public FloorTriangleBoxWalkResult Walk(int triangle,Vector3 start,Vector3 desired,Vector3 half,
        bool allowSeparatingContact=false,int maximumCrossings=4096)
    {
        var path=floor.Trace(triangle,start,desired,maximumCrossings);
        var contact=FloorPathTriangleBoxSweep.Sweep(path,collider,half,allowSeparatingContact);
        if(contact.Status==TriangleBoxContactStatus.Clear)
            return new(path.Result.Status==FloorWalkStatus.Accepted?FloorBoxWalkStatus.Accepted:FloorBoxWalkStatus.FloorStopped,path.Result,null);
        var feet=contact.Center with{Y=contact.Center.Y-half.Y};
        bool Validate(Vector3 position)
        {
            var candidate=floor.Trace(triangle,start,position,maximumCrossings);
            var stationary=floor.Trace(candidate.Result.TriangleIndex,position,position);
            if(FloorPathTriangleBoxSweep.Sweep(stationary,collider,half,true).Status!=TriangleBoxContactStatus.Clear)return false;
            var swept=FloorPathTriangleBoxSweep.Sweep(candidate,collider,half,true);
            return swept.Status==TriangleBoxContactStatus.Clear||
                swept.Status==TriangleBoxContactStatus.Blocked&&swept.Parameter==candidate.Result.Parameter;
        }
        var landing=FloorBoxContactLanding.Resolve(floor,path,triangle,start,feet,contact.Normal,
            contact.SegmentIndex,contact.Parameter,maximumCrossings,Validate);
        return new(landing.Accepted?FloorBoxWalkStatus.BoxStopped:FloorBoxWalkStatus.ContactLandingUnsupported,
            landing.Floor,contact,landing.Accepted?null:"UnrepresentableTriangleContactLanding");
    }
}
