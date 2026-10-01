using System.Numerics;

namespace Stardust.Core.Assets;

/// <summary>One bounded modern step and at most one portal transfer per world-space request. Upright rigid owners; supported-floor rollback on vertical contact.</summary>
public sealed class PortalFloorStepBoxWalker
{
    private readonly PortalFloorBoxWalker boxed;
    private readonly PortalFloorTransferQuery transfers;
    private readonly Dictionary<int,FloorStepTransitionQuery> steps=new();
    private readonly Dictionary<int,AppearanceTransform> poses=new();
    private readonly FloorStepPolicy policy;
    public PortalFloorStepBoxWalker(PortalLayoutDescription layout,IReadOnlyDictionary<int,FloorMeshDescription> floors,
        IReadOnlyDictionary<int,AppearanceTransform>? ownerPoses=null,FloorStepPolicy? stepPolicy=null)
        :this(new(layout,floors,ownerPoses),layout,floors,ownerPoses,stepPolicy??new()){}
    internal PortalFloorStepBoxWalker(PortalFloorBoxWalker query,PortalLayoutDescription layout,IReadOnlyDictionary<int,FloorMeshDescription> floors,
        IReadOnlyDictionary<int,AppearanceTransform>? ownerPoses,FloorStepPolicy stepPolicy)
    {
        boxed=query;policy=stepPolicy;policy.Validate();transfers=new(layout,floors,ownerPoses);
        foreach(var pair in floors)
        {
            var pose=ownerPoses is not null&&ownerPoses.TryGetValue(pair.Key,out var supplied)?supplied:AppearanceTransform.Identity;
            if(Vector3.Distance(pose.RowY,Vector3.UnitY)>1e-5f||Math.Abs(pose.RowX.Y)>1e-5f||Math.Abs(pose.RowZ.Y)>1e-5f)
                throw new InvalidDataException("Modern steps require upright cell owners.");
            poses.Add(pair.Key,pose);steps.Add(pair.Key,new(pair.Value));
        }
    }
    public PortalFloorBoxResult Walk(CellFloorLocator start,Vector3 desired,Vector3 half,IReadOnlyList<PortalFloorBox> snapshots,
        Func<PortalFloorLink,PortalTransferPermission> permission,bool allowSeparatingContact=false,int maximumCrossings=4096)
    {
        ArgumentNullException.ThrowIfNull(snapshots);ArgumentNullException.ThrowIfNull(permission);
        if(snapshots.Count>64)throw new InvalidDataException("Portal step collider budget.");
        var captured=snapshots.ToArray();var decisions=new Dictionary<PortalFloorLink,PortalTransferPermission>();
        PortalTransferPermission Current(PortalFloorLink link)
        {
            if(!decisions.TryGetValue(link,out var value))decisions.Add(link,value=permission(link));return value;
        }
        var prefix=boxed.WalkAllSnapshots(start,desired,half,captured,Current,allowSeparatingContact,maximumCrossings,1);
        if(prefix.Status!=FloorBoxWalkStatus.FloorStopped||prefix.Movement.FloorResult?.Status!=FloorWalkStatus.UnsupportedSurface)return prefix;
        var trace=transfers.Trace(start,desired,Current,maximumCrossings);var part=trace.Floors[^1];var pose=poses[part.CellIndex];
        Vector3 offset=desired-pose.Translation;Vector3 localDesired=new(pose.RowX.X*offset.X+pose.RowY.X*offset.Y+pose.RowZ.X*offset.Z,
            pose.RowX.Y*offset.X+pose.RowY.Y*offset.Y+pose.RowZ.Y*offset.Z,pose.RowX.Z*offset.X+pose.RowY.Z*offset.Y+pose.RowZ.Z*offset.Z);
        var proposal=steps[part.CellIndex].Probe(part.Path,localDesired,policy);
        if(proposal.Status!=FloorStepStatus.Accepted)return prefix with{Diagnostic="Step"+proposal.Status};
        int used=trace.Floors.Sum(f=>f.Path.Result.Crossings)+proposal.StripTriangles.Count+1;
        if(used>maximumCrossings)return prefix with{Diagnostic="StepTraversalLimit"};
        var source=World(proposal.SourceContact,pose);var target=World(proposal.TargetContact,pose);var landing=pose.TransformPoint(proposal.Landing);
        var lifted=new FloorWalkPath(new(FloorWalkStatus.Accepted,proposal.TargetTriangle,landing,1,-1,-1,0),
            new[]{new FloorWalkSegment(proposal.TargetTriangle,0,1,source,target),new FloorWalkSegment(proposal.TargetTriangle,1,1,target,landing)});
        PortalFloorBoxContact? chosen=null;
        foreach(var box in captured)
        {
            var hit=FloorPathBoxSweep.Sweep(lifted,box.Collider,half,box.PassageAllowed,allowSeparatingContact);
            if(hit.Status==PortalBarrierContactStatus.Clear||chosen is not null&&hit.Parameter>=chosen.World.Parameter)continue;
            chosen=new(box.CellIndex,box.Id,part.CellIndex,hit);
        }
        if(chosen is not null)
        {
            var held=prefix.Movement;
            if(allowSeparatingContact&&held.Transfers==0&&held.FloorResult?.Crossings==0&&Vector3.Distance(start.LocalPosition,held.Locator.LocalPosition)<=1e-4f)
            {
                var initial=boxed.ValidateStart(start);
                held=held with{Locator=initial.Locator,WorldPosition=initial.WorldPosition,Parameter=0,
                    FloorResult=held.FloorResult with{TriangleIndex=initial.Locator.TriangleIndex,Position=initial.Locator.LocalPosition,Parameter=0,Crossings=0}};
            }
            return new(FloorBoxWalkStatus.BoxStopped,held,chosen with{World=chosen.World with{Parameter=held.Parameter}},"VerticalStepBlocked");
        }
        var continuation=boxed.WalkAllSnapshots(new(part.CellIndex,proposal.TargetTriangle,proposal.Landing),desired,half,captured,Current,
            allowSeparatingContact,maximumCrossings-used,1-prefix.Movement.Transfers);
        if(continuation.Contact is not null&&continuation.Movement.Parameter==0)
            return new(FloorBoxWalkStatus.BoxStopped,prefix.Movement,continuation.Contact with{World=continuation.Contact.World with{Parameter=prefix.Movement.Parameter}},"StepLandingOverlap");
        double parameter=prefix.Movement.Parameter+(1-prefix.Movement.Parameter)*continuation.Movement.Parameter;
        var movement=continuation.Movement with{Parameter=parameter,Transfers=prefix.Movement.Transfers+continuation.Movement.Transfers,
            Link=continuation.Movement.Link??prefix.Movement.Link};
        return continuation with{Movement=movement,Contact=continuation.Contact is null?null:continuation.Contact with{World=continuation.Contact.World with{Parameter=parameter}},Steps=1};
    }
    private static FloorPathPoint World(FloorPathPoint p,AppearanceTransform t)=>new(
        t.RowX.X*p.X+t.RowX.Y*p.Y+t.RowX.Z*p.Z+t.Translation.X,t.RowY.X*p.X+t.RowY.Y*p.Y+t.RowY.Z*p.Z+t.Translation.Y,
        t.RowZ.X*p.X+t.RowZ.Y*p.Y+t.RowZ.Z*p.Z+t.Translation.Z);
}
