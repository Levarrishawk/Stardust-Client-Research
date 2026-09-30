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
        if(chosen.Parameter==0)return new(FloorBoxWalkStatus.BoxStopped,floor.Walk(triangle,start,start) with{Parameter=0,Crossings=0},chosen,boxIndex);
        FloorPathPoint feet=chosen.Center with{Y=chosen.Center.Y-actorHalf.Y};
        FloorWalkResult landing=floor.Walk(triangle,start,feet.ToVector3(),maximumCrossings);
        bool Valid(FloorWalkResult candidate)
        {
            double dx=candidate.Position.X-feet.X,dy=candidate.Position.Y-feet.Y,dz=candidate.Position.Z-feet.Z;
            return candidate.Status==FloorWalkStatus.Accepted && dx*dx+dy*dy+dz*dz<=1e-8 &&
                dx*chosen.Normal.X+dy*chosen.Normal.Y+dz*chosen.Normal.Z>=-1e-6 &&
                floor.Walk(candidate.TriangleIndex,candidate.Position,candidate.Position).Status==FloorWalkStatus.Accepted;
        }
        // Preserve strict input checks while seeking a representable contact on the approach side.
        if(!Valid(landing))
        {
            var segment=path.Segments[chosen.SegmentIndex];
            (double X,double Z)[] directions={(chosen.Normal.X,chosen.Normal.Z),(segment.Start.X-segment.End.X,segment.Start.Z-segment.End.Z)};
            bool found=false;
            foreach(var direction in directions)
            {
                double length=Math.Sqrt(direction.X*direction.X+direction.Z*direction.Z);if(length==0)continue;
                foreach(double distance in new[]{1e-6,2e-6,5e-6,1e-5,2e-5,5e-5,1e-4})
                {
                    FloorPathPoint biased=feet with{X=feet.X+direction.X/length*distance,Z=feet.Z+direction.Z/length*distance};
                    var candidate=floor.Walk(triangle,start,biased.ToVector3(),maximumCrossings);
                    if(!Valid(candidate))continue;landing=candidate;found=true;break;
                }
                if(found)break;
            }
        }
        if(!Valid(landing))
        {
            var held=floor.Walk(triangle,start,start) with{Parameter=0,Crossings=0};
            return new(FloorBoxWalkStatus.ContactLandingUnsupported,held,chosen,boxIndex,"UnrepresentableBoxContactLanding");
        }
        return new(FloorBoxWalkStatus.BoxStopped,landing with{Parameter=chosen.Parameter},chosen,boxIndex);
    }
    private static bool Finite(Vector3 p)=>float.IsFinite(p.X)&&float.IsFinite(p.Y)&&float.IsFinite(p.Z);
}
