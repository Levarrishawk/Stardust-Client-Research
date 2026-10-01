using System.Numerics;

namespace Stardust.Core.Assets;

public sealed record FloorStepBoxWalkResult(FloorBoxWalkResult Movement,FloorStepTransition? Proposal,int Steps,string? Diagnostic=null);

/// <summary>One modern step per same-space request. Vertical collision holds a supported source floor; no partial airborne locator is adopted.</summary>
public sealed class SingleCellFloorStepBoxWalker
{
    private readonly SingleCellFloorWalker floor;
    private readonly SingleCellFloorBoxWalker boxed;
    private readonly FloorStepTransitionQuery steps;
    private readonly PortalBarrierCollider[] colliders;
    private readonly FloorStepPolicy policy;
    public SingleCellFloorStepBoxWalker(FloorMeshDescription description,IReadOnlyList<PortalBarrierCollider> boxes,FloorStepPolicy? stepPolicy=null)
    {
        ArgumentNullException.ThrowIfNull(boxes);policy=stepPolicy??new();policy.Validate();colliders=boxes.ToArray();boxed=new(description,colliders);floor=new(description);steps=new(description);
    }
    public FloorStepBoxWalkResult Walk(int triangle,Vector3 start,Vector3 desired,Vector3 half,Func<int,bool?> passageAllowed,
        bool allowSeparatingContact=false,int maximumCrossings=4096)
    {
        ArgumentNullException.ThrowIfNull(passageAllowed);
        if(!Finite(half)||half.X<0||half.Y<0||half.Z<0)throw new InvalidDataException("Invalid step actor bounds.");
        var path=floor.Trace(triangle,start,desired,maximumCrossings);
        if(path.Result.Status==FloorWalkStatus.InvalidStart || path.Segments.Count==0)
            return new(new(FloorBoxWalkStatus.FloorStopped,path.Result,null,null),null,0);
        bool?[] decisions=Enumerable.Range(0,colliders.Length).Select(passageAllowed).ToArray();
        var prefix=boxed.Walk(triangle,start,desired,half,i=>decisions[i],allowSeparatingContact,maximumCrossings);
        if(prefix.Status!=FloorBoxWalkStatus.FloorStopped || prefix.Floor.Status!=FloorWalkStatus.UnsupportedSurface)return new(prefix,null,0);
        var proposal=steps.Probe(path,desired,policy);
        if(proposal.Status!=FloorStepStatus.Accepted)return new(prefix,proposal,0,proposal.Status.ToString());
        int cost=proposal.StripTriangles.Count+1;
        if(prefix.Floor.Crossings+cost>maximumCrossings)return new(prefix,proposal,0,"StepTraversalLimit");
        var lifted=new FloorWalkPath(new(FloorWalkStatus.Accepted,proposal.TargetTriangle,proposal.Landing,1,-1,-1,0),
            new[]{new FloorWalkSegment(proposal.TargetTriangle,0,1,proposal.SourceContact,proposal.TargetContact),
                new FloorWalkSegment(proposal.TargetTriangle,1,1,proposal.TargetContact,proposal.Landing)});
        FloorPathBoxContact? blocked=null;int? collider=null;
        for(int i=0;i<colliders.Length;i++)
        {
            var hit=FloorPathBoxSweep.Sweep(lifted,colliders[i],half,decisions[i],allowSeparatingContact);
            if(hit.Status==PortalBarrierContactStatus.Clear || blocked is not null && hit.Parameter>=blocked.Parameter)continue;
            blocked=hit;collider=i;
        }
        if(blocked is not null)
        {
            var held=prefix.Floor;
            if(allowSeparatingContact&&held.Crossings==0&&Vector3.Distance(start,held.Position)<=1e-4f)
            {
                var initial=floor.Walk(triangle,start,start);
                held=held with{TriangleIndex=initial.TriangleIndex,Position=initial.Position,Parameter=0,Crossings=0};
            }
            // The contact parameter is horizontal request progress; the actor never adopts a floating Y.
            var movement=new FloorBoxWalkResult(FloorBoxWalkStatus.BoxStopped,held,blocked with{Parameter=held.Parameter},collider,"VerticalStepBlocked");
            return new(movement,proposal,0,"VerticalStepBlocked");
        }
        var continuation=boxed.Walk(proposal.TargetTriangle,proposal.Landing,desired,half,i=>decisions[i],allowSeparatingContact,maximumCrossings-prefix.Floor.Crossings-cost);
        if(continuation.Floor.Parameter==0 && continuation.Contact is not null)
            return new(new(FloorBoxWalkStatus.BoxStopped,prefix.Floor,continuation.Contact with{Parameter=prefix.Floor.Parameter},continuation.ColliderIndex,"StepLandingOverlap"),proposal,0,"StepLandingOverlap");
        double parameter=prefix.Floor.Parameter+(1-prefix.Floor.Parameter)*continuation.Floor.Parameter;
        var result=continuation with{Floor=continuation.Floor with{Parameter=parameter,Crossings=prefix.Floor.Crossings+cost+continuation.Floor.Crossings},
            Contact=continuation.Contact is null?null:continuation.Contact with{Parameter=parameter}};
        return new(result,proposal,1,result.Diagnostic);
    }
    private static bool Finite(Vector3 p)=>float.IsFinite(p.X)&&float.IsFinite(p.Y)&&float.IsFinite(p.Z);
}
