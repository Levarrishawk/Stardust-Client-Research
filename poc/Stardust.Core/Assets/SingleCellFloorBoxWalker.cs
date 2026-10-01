using System.Numerics;

namespace Stardust.Core.Assets;

public enum FloorBoxWalkStatus { Accepted,FloorStopped,BoxStopped,ContactLandingUnsupported }
public sealed record FloorBoxWalkResult(FloorBoxWalkStatus Status,FloorWalkResult Floor,
    FloorPathBoxContact? Contact,int? ColliderIndex,string? Diagnostic=null);

/// <summary>Owned same-space single-cell floor traversal with immutable box snapshots and validated contact landing. No cell transfer or sliding.</summary>
public sealed class SingleCellFloorBoxWalker
{
    private readonly SingleCellFloorWalker floor;
    private readonly PortalBarrierCollider[] boxes;
    public SingleCellFloorBoxWalker(FloorMeshDescription description,IReadOnlyList<PortalBarrierCollider> colliders)
    {
        ArgumentNullException.ThrowIfNull(colliders);
        if(colliders.Count>64 || colliders.Any(c=>c is null))throw new InvalidDataException("Single-cell collider budget.");
        floor=new(description);boxes=colliders.ToArray();
    }
    public FloorBoxWalkResult Walk(int triangle,Vector3 start,Vector3 desired,Vector3 actorHalf,
        Func<int,bool?> passageAllowed,bool allowSeparatingContact=false,int maximumCrossings=4096)
    {
        ArgumentNullException.ThrowIfNull(passageAllowed);
        if(!Finite(actorHalf) || actorHalf.X<0 || actorHalf.Y<0 || actorHalf.Z<0)throw new InvalidDataException("Invalid single-cell actor bounds.");
        FloorWalkPath path=floor.Trace(triangle,start,desired,maximumCrossings);
        if(path.Segments.Count==0)return new(FloorBoxWalkStatus.FloorStopped,path.Result,null,null);
        // Capture each decision once; this stateless query never advances a controller.
        bool?[] decisions=Enumerable.Range(0,boxes.Length).Select(passageAllowed).ToArray();
        FloorPathBoxContact? chosen=null;int? boxIndex=null;
        for(int i=0;i<boxes.Length;i++)
        {
            var contact=FloorPathBoxSweep.Sweep(path,boxes[i],actorHalf,decisions[i],allowSeparatingContact);
            if(contact.Status==PortalBarrierContactStatus.Clear || chosen is not null && contact.Parameter>=chosen.Parameter)continue;
            chosen=contact;boxIndex=i;
        }
        if(chosen is null)return new(path.Result.Status==FloorWalkStatus.Accepted?FloorBoxWalkStatus.Accepted:FloorBoxWalkStatus.FloorStopped,path.Result,null,null);
        FloorPathPoint feet=chosen.Center with{Y=chosen.Center.Y-actorHalf.Y};
        var landing=FloorBoxContactLanding.Resolve(floor,path,triangle,start,feet,chosen.Normal,chosen.SegmentIndex,chosen.Parameter,maximumCrossings);
        return new(landing.Accepted?FloorBoxWalkStatus.BoxStopped:FloorBoxWalkStatus.ContactLandingUnsupported,
            landing.Floor,chosen,boxIndex,landing.Accepted?null:"UnrepresentableBoxContactLanding");
    }
    private static bool Finite(Vector3 p)=>float.IsFinite(p.X)&&float.IsFinite(p.Y)&&float.IsFinite(p.Z);
}
