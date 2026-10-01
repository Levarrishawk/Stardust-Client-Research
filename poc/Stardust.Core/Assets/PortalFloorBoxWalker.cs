using System.Numerics;

namespace Stardust.Core.Assets;

public sealed record PortalFloorBox(int CellIndex,int Id,PortalBarrierCollider Collider,bool? PassageAllowed);
public sealed record PortalFloorBoxContact(int ColliderCell,int ColliderId,int PathCell,FloorPathBoxContact World);
public sealed record PortalFloorBoxResult(FloorBoxWalkStatus Status,CellFloorWalkResult Movement,
    PortalFloorBoxContact? Contact,string? Diagnostic=null,int Steps=0);

/// <summary>Stateless one-transfer floor/box query. Precise world paths, fixed world-axis actor bounds, immutable per-query box snapshots.</summary>
public sealed class PortalFloorBoxWalker
{
    private readonly PortalFloorTransferQuery transfers;
    private readonly Dictionary<int,SingleCellFloorWalker> floors=new();
    private readonly Dictionary<int,AppearanceTransform> poses=new();
    public IReadOnlyList<PortalFloorLink> Links=>transfers.Links;
    internal CellFloorWalkResult ValidateStart(CellFloorLocator start)
    {
        if(!Finite(start.LocalPosition))throw new InvalidDataException("Nonfinite portal floor start.");
        if(!floors.TryGetValue(start.CellIndex,out var floor))return new(CellFloorWalkStatus.InvalidStart,start,start.LocalPosition,0,0,null,null);
        var result=floor.Walk(start.TriangleIndex,start.LocalPosition,start.LocalPosition);
        return new(result.Status==FloorWalkStatus.Accepted?CellFloorWalkStatus.Accepted:result.Status==FloorWalkStatus.InvalidStart?CellFloorWalkStatus.InvalidStart:CellFloorWalkStatus.FloorStopped,
            new(start.CellIndex,result.TriangleIndex,result.Position),poses[start.CellIndex].TransformPoint(result.Position),0,0,null,result);
    }
    public PortalFloorBoxWalker(PortalLayoutDescription layout,IReadOnlyDictionary<int,FloorMeshDescription> cellFloors,
        IReadOnlyDictionary<int,AppearanceTransform>? ownerPoses=null)
    {
        transfers=new(layout,cellFloors,ownerPoses);
        foreach(var pair in cellFloors)
        {
            floors.Add(pair.Key,new(pair.Value));
            poses.Add(pair.Key,ownerPoses is not null && ownerPoses.TryGetValue(pair.Key,out var p)?p:AppearanceTransform.Identity);
        }
    }
    public PortalFloorBoxResult Walk(CellFloorLocator start,Vector3 desired,Vector3 actorHalf,
        IReadOnlyList<PortalFloorBox> colliderSnapshots,Func<PortalFloorLink,PortalTransferPermission> permission,
        bool allowSeparatingContact=false,int maximumCrossings=4096,int maximumTransfers=1)
        =>WalkCore(start,desired,actorHalf,colliderSnapshots,permission,allowSeparatingContact,maximumCrossings,maximumTransfers,false);
    internal PortalFloorBoxResult WalkAllSnapshots(CellFloorLocator start,Vector3 desired,Vector3 actorHalf,
        IReadOnlyList<PortalFloorBox> colliderSnapshots,Func<PortalFloorLink,PortalTransferPermission> permission,
        bool allowSeparatingContact,int maximumCrossings,int maximumTransfers)
        =>WalkCore(start,desired,actorHalf,colliderSnapshots,permission,allowSeparatingContact,maximumCrossings,maximumTransfers,true);
    private PortalFloorBoxResult WalkCore(CellFloorLocator start,Vector3 desired,Vector3 actorHalf,
        IReadOnlyList<PortalFloorBox> colliderSnapshots,Func<PortalFloorLink,PortalTransferPermission> permission,
        bool allowSeparatingContact,int maximumCrossings,int maximumTransfers,bool includeAllSnapshots)
    {
        ArgumentNullException.ThrowIfNull(colliderSnapshots);ArgumentNullException.ThrowIfNull(permission);
        if(!Finite(actorHalf) || actorHalf.X<0 || actorHalf.Y<0 || actorHalf.Z<0 || colliderSnapshots.Count>64)
            throw new InvalidDataException("Invalid portal actor-box bounds/budget.");
        PortalFloorBox[] boxes=colliderSnapshots.ToArray();HashSet<(int,int)> keys=new();
        foreach(var box in boxes)
            if(box is null || box.Collider is null || box.Id<0 || !floors.ContainsKey(box.CellIndex) || !keys.Add((box.CellIndex,box.Id)))
                throw new InvalidDataException("Invalid or duplicate portal collider snapshot.");
        CellFloorWalkPath trace=transfers.Trace(start,desired,permission,maximumCrossings,maximumTransfers);
        if(trace.Floors.Count==0 || trace.Floors[0].Path.Segments.Count==0)
            return new(FloorBoxWalkStatus.FloorStopped,trace.Result,null);
        var participating=trace.Floors.Select(f=>f.CellIndex).ToHashSet();
        // Target bounds can stop the actor before its feet leave the source floor.
        if(!includeAllSnapshots)boxes=boxes.Where(b=>participating.Contains(b.CellIndex)).ToArray();
        PortalFloorBoxContact? chosen=null;FloorPathBoxContact? localHit=null;int chosenLeg=-1;
        double sourceParameter=trace.Floors[0].Path.Result.Parameter;
        for(int leg=0;leg<trace.Floors.Count;leg++)
        {
            var part=trace.Floors[leg];FloorWalkPath world=WorldPath(part.Path,poses[part.CellIndex]);
            foreach(var box in boxes)
            {
                var hit=FloorPathBoxSweep.Sweep(world,box.Collider,actorHalf,box.PassageAllowed,allowSeparatingContact);
                if(hit.Status==PortalBarrierContactStatus.Clear)continue;
                double parameter=leg==0?hit.Parameter:sourceParameter+(1-sourceParameter)*hit.Parameter;
                if(chosen is not null && parameter>=chosen.World.Parameter)continue;
                chosen=new(box.CellIndex,box.Id,part.CellIndex,hit with{Parameter=parameter});localHit=hit;chosenLeg=leg;
            }
        }
        if(chosen is null)return new(trace.Result.Status==CellFloorWalkStatus.Accepted?FloorBoxWalkStatus.Accepted:FloorBoxWalkStatus.FloorStopped,trace.Result,null);
        var selected=trace.Floors[chosenLeg];var pose=poses[selected.CellIndex];
        Vector3 initial=selected.Path.Segments[0].Start.ToVector3();int triangle=selected.Path.Segments[0].TriangleIndex;
        FloorPathPoint worldFeet=localHit!.Center with{Y=localHit.Center.Y-actorHalf.Y};
        var feet=Local(worldFeet,pose);Vector3 normal=LocalDirection(localHit.Normal,pose);
        int remaining=maximumCrossings-(chosenLeg==0?0:trace.Floors[0].Path.Result.Crossings);
        bool ValidWorld(Vector3 p)
        {
            Vector3 world=pose.TransformPoint(p);double dx=world.X-worldFeet.X,dy=world.Y-worldFeet.Y,dz=world.Z-worldFeet.Z;
            return Finite(world) && dx*dx+dy*dy+dz*dz<=1e-8 &&
                dx*localHit.Normal.X+dy*localHit.Normal.Y+dz*localHit.Normal.Z>=-1e-6;
        }
        var landing=FloorBoxContactLanding.Resolve(floors[selected.CellIndex],selected.Path,triangle,initial,feet,normal,
            localHit.SegmentIndex,localHit.Parameter,remaining,ValidWorld);
        if(!landing.Accepted)
        {
            var held=floors[start.CellIndex].Walk(start.TriangleIndex,start.LocalPosition,start.LocalPosition);
            var movement=new CellFloorWalkResult(CellFloorWalkStatus.FloorStopped,new(start.CellIndex,held.TriangleIndex,held.Position),poses[start.CellIndex].TransformPoint(held.Position),0,0,null,held);
            return new(FloorBoxWalkStatus.ContactLandingUnsupported,movement,chosen,"UnrepresentablePortalBoxContactLanding");
        }
        // A target-only initial overlap is not clearance to adopt ownership across the seam.
        if(chosenLeg==1 && localHit.Parameter==0)
        {
            var source=trace.Floors[0].Path.Result;
            return new(FloorBoxWalkStatus.BoxStopped,new(CellFloorWalkStatus.FloorStopped,
                new(start.CellIndex,source.TriangleIndex,source.Position),poses[start.CellIndex].TransformPoint(source.Position),sourceParameter,0,null,source),chosen,"TargetSeamOverlap");
        }
        int cell=selected.CellIndex;
        var result=new CellFloorWalkResult(CellFloorWalkStatus.FloorStopped,new(cell,landing.Floor.TriangleIndex,landing.Floor.Position),
            pose.TransformPoint(landing.Floor.Position),chosen.World.Parameter,chosenLeg==0?0:1,chosenLeg==0?null:trace.Result.Link,landing.Floor);
        return new(FloorBoxWalkStatus.BoxStopped,result,chosen);
    }
    private static FloorWalkPath WorldPath(FloorWalkPath path,AppearanceTransform pose)
        =>new(path.Result with{Position=pose.TransformPoint(path.Result.Position)},Array.AsReadOnly(path.Segments.Select(s=>s with{Start=World(s.Start,pose),End=World(s.End,pose)}).ToArray()));
    private static FloorPathPoint World(FloorPathPoint p,AppearanceTransform t)=>new(
        t.RowX.X*p.X+t.RowX.Y*p.Y+t.RowX.Z*p.Z+t.Translation.X,
        t.RowY.X*p.X+t.RowY.Y*p.Y+t.RowY.Z*p.Z+t.Translation.Y,
        t.RowZ.X*p.X+t.RowZ.Y*p.Y+t.RowZ.Z*p.Z+t.Translation.Z);
    private static FloorPathPoint Local(FloorPathPoint p,AppearanceTransform t)
    {
        double x=p.X-t.Translation.X,y=p.Y-t.Translation.Y,z=p.Z-t.Translation.Z;
        return new(t.RowX.X*x+t.RowY.X*y+t.RowZ.X*z,t.RowX.Y*x+t.RowY.Y*y+t.RowZ.Y*z,t.RowX.Z*x+t.RowY.Z*y+t.RowZ.Z*z);
    }
    private static Vector3 LocalDirection(Vector3 p,AppearanceTransform t)=>new(
        t.RowX.X*p.X+t.RowY.X*p.Y+t.RowZ.X*p.Z,t.RowX.Y*p.X+t.RowY.Y*p.Y+t.RowZ.Y*p.Z,t.RowX.Z*p.X+t.RowY.Z*p.Y+t.RowZ.Z*p.Z);
    private static bool Finite(Vector3 p)=>float.IsFinite(p.X)&&float.IsFinite(p.Y)&&float.IsFinite(p.Z);
}
