using System.Numerics;

namespace Stardust.Core.Assets;

public sealed record FloorStepTriangleBoxWalkResult(FloorTriangleBoxWalkResult Movement,FloorStepTransition? Proposal,
    int Steps,string? Diagnostic=null);

/// <summary>One bounded same-cell step with triangle lift clearance and supported rollback.</summary>
public sealed class SingleCellFloorStepTriangleBoxWalker
{
    private readonly SingleCellFloorWalker floor;
    private readonly SingleCellFloorTriangleBoxWalker boxed;
    private readonly FloorStepTransitionQuery steps;
    private readonly StaticTriangleBoxCollider collider;
    private readonly FloorStepPolicy policy;
    public SingleCellFloorStepTriangleBoxWalker(FloorMeshDescription description,IReadOnlyList<CollisionTriangle> triangles,
        FloorStepPolicy? stepPolicy=null)
    {
        ArgumentNullException.ThrowIfNull(triangles);
        if(triangles.Count>2048)throw new InvalidDataException("Triangle step collider budget.");
        policy=stepPolicy??new();policy.Validate();var captured=triangles.ToArray();
        floor=new(description);boxed=new(description,captured);steps=new(description);collider=new(captured);
    }
    public FloorStepTriangleBoxWalkResult Walk(int triangle,Vector3 start,Vector3 desired,Vector3 half,
        bool allowSeparatingContact=false,int maximumCrossings=4096)
    {
        var prefix=boxed.Walk(triangle,start,desired,half,allowSeparatingContact,maximumCrossings);
        if(prefix.Status!=FloorBoxWalkStatus.FloorStopped||prefix.Floor.Status!=FloorWalkStatus.UnsupportedSurface)
            return new(prefix,null,0);
        var proposal=steps.Probe(floor.Trace(triangle,start,desired,maximumCrossings),desired,policy);
        if(proposal.Status!=FloorStepStatus.Accepted)return new(prefix,proposal,0,proposal.Status.ToString());
        int cost=proposal.StripTriangles.Count+1;
        if(prefix.Floor.Crossings+cost>maximumCrossings)return new(prefix,proposal,0,"StepTraversalLimit");
        var lifted=new FloorWalkPath(new(FloorWalkStatus.Accepted,proposal.TargetTriangle,proposal.Landing,1,-1,-1,0),
            new[]{new FloorWalkSegment(proposal.TargetTriangle,0,1,proposal.SourceContact,proposal.TargetContact),
                new FloorWalkSegment(proposal.TargetTriangle,1,1,proposal.TargetContact,proposal.Landing)});
        var contact=FloorPathTriangleBoxSweep.Sweep(lifted,collider,half,allowSeparatingContact);
        if(contact.Status!=TriangleBoxContactStatus.Clear)
        {
            var blocked=new FloorTriangleBoxWalkResult(FloorBoxWalkStatus.BoxStopped,prefix.Floor,
                contact with{Parameter=prefix.Floor.Parameter},"VerticalStepBlocked");
            return new(blocked,proposal,0,"VerticalStepBlocked");
        }
        var continuation=boxed.Walk(proposal.TargetTriangle,proposal.Landing,desired,half,allowSeparatingContact,
            maximumCrossings-prefix.Floor.Crossings-cost);
        if(continuation.Contact is not null&&continuation.Floor.Parameter==0)
            return new(new(FloorBoxWalkStatus.BoxStopped,prefix.Floor,continuation.Contact with{Parameter=prefix.Floor.Parameter},
                "StepLandingOverlap"),proposal,0,"StepLandingOverlap");
        double parameter=prefix.Floor.Parameter+(1-prefix.Floor.Parameter)*continuation.Floor.Parameter;
        var result=continuation with{Floor=continuation.Floor with{Parameter=parameter,
            Crossings=prefix.Floor.Crossings+cost+continuation.Floor.Crossings},
            Contact=continuation.Contact is null?null:continuation.Contact with{Parameter=prefix.Floor.Parameter+(1-prefix.Floor.Parameter)*continuation.Contact.Parameter}};
        return new(result,proposal,1,result.Diagnostic);
    }
}
