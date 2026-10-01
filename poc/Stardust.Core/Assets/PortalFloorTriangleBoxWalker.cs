using System.Numerics;

namespace Stardust.Core.Assets;

public sealed record PortalFloorTriangleContact(int ColliderCell,int PathCell,FloorPathTriangleContact World);
public sealed record PortalFloorTriangleResult(FloorBoxWalkStatus Status,CellFloorWalkResult Movement,
    PortalFloorTriangleContact? Contact,string? Diagnostic=null);

/// <summary>Owned static triangles in cell-local space, queried as world-axis body bounds over one floor transfer.</summary>
public sealed class PortalFloorTriangleBoxWalker
{
    private readonly PortalFloorTransferQuery transfers;
    private readonly Dictionary<int,SingleCellFloorWalker> floors=new();
    private readonly Dictionary<int,AppearanceTransform> poses=new();
    private readonly SortedDictionary<int,StaticTriangleBoxCollider> colliders=new();
    private readonly int triangleCount;
    public PortalFloorTriangleBoxWalker(PortalLayoutDescription layout,IReadOnlyDictionary<int,FloorMeshDescription> cellFloors,
        IReadOnlyDictionary<int,IReadOnlyList<CollisionTriangle>> cellTriangles,IReadOnlyDictionary<int,AppearanceTransform>? ownerPoses=null,int maximumTriangles=2048)
    {
        ArgumentNullException.ThrowIfNull(cellTriangles);
        if(maximumTriangles is <0 or >StaticTriangleBoxCollider.MaximumSupportedTriangles)throw new ArgumentOutOfRangeException(nameof(maximumTriangles));
        transfers=new(layout,cellFloors,ownerPoses);
        foreach(var pair in cellFloors)
        {
            floors.Add(pair.Key,new(pair.Value));
            poses.Add(pair.Key,ownerPoses is not null&&ownerPoses.TryGetValue(pair.Key,out var p)?p:AppearanceTransform.Identity);
        }
        if(cellTriangles.Count>64||cellTriangles.Any(pair=>!floors.ContainsKey(pair.Key)||pair.Value is null))
            throw new InvalidDataException("Invalid triangle owner set.");
        long total=cellTriangles.Sum(pair=>(long)pair.Value.Count);
        if(total>maximumTriangles)throw new InvalidDataException("Portal triangle resource budget.");
        triangleCount=(int)total;
        foreach(var pair in cellTriangles)
        {
            var pose=poses[pair.Key];
            colliders.Add(pair.Key,new(pair.Value.Select(t=>t with{A=pose.TransformPoint(t.A),B=pose.TransformPoint(t.B),C=pose.TransformPoint(t.C)}).ToArray(),maximumTriangles));
        }
    }
    public PortalFloorTriangleResult Walk(CellFloorLocator start,Vector3 desired,Vector3 half,
        Func<PortalFloorLink,PortalTransferPermission> permission,bool allowSeparatingContact=false,
        int maximumCrossings=4096,int maximumTransfers=1,float feetClearance=0)
    {
        ArgumentNullException.ThrowIfNull(permission);
        if(!Finite(half)||half.X<0||half.Y<0||half.Z<0||half.X>100||half.Y>100||half.Z>100||!float.IsFinite(feetClearance)||feetClearance<0||feetClearance>.05f)
            throw new InvalidDataException("Invalid portal triangle body bounds.");
        var trace=transfers.Trace(start,desired,permission,maximumCrossings,maximumTransfers);
        if(trace.Floors.Count==0||trace.Floors[0].Path.Segments.Count==0)
            return new(FloorBoxWalkStatus.FloorStopped,trace.Result,null);
        if((long)trace.Floors.Sum(p=>p.Path.Segments.Count)*triangleCount>262144)
            throw new InvalidDataException("Portal triangle path work budget.");
        var worlds=trace.Floors.Select(p=>WorldPath(p.Path,poses[p.CellIndex])).ToArray();
        // Empty obstacle sets still validate every transformed path and body center.
        var validationCollider=new StaticTriangleBoxCollider(Array.Empty<CollisionTriangle>());
        foreach(var world in worlds)FloorPathTriangleBoxSweep.Sweep(world,validationCollider,half,allowSeparatingContact,feetClearance);
        PortalFloorTriangleContact? chosen=null;FloorPathTriangleContact? localHit=null;int chosenLeg=-1;
        double sourceParameter=trace.Floors[0].Path.Result.Parameter;
        for(int leg=0;leg<worlds.Length;leg++)
        foreach(var collider in colliders)
        {
            var hit=FloorPathTriangleBoxSweep.Sweep(worlds[leg],collider.Value,half,allowSeparatingContact,feetClearance);
            if(hit.Status==TriangleBoxContactStatus.Clear)continue;
            double parameter=leg==0?hit.Parameter:sourceParameter+(1-sourceParameter)*hit.Parameter;
            if(chosen is not null&&parameter>=chosen.World.Parameter)continue;
            chosen=new(collider.Key,trace.Floors[leg].CellIndex,hit with{Parameter=parameter});localHit=hit;chosenLeg=leg;
        }
        if(chosen is null)return new(trace.Result.Status==CellFloorWalkStatus.Accepted?FloorBoxWalkStatus.Accepted:FloorBoxWalkStatus.FloorStopped,trace.Result,null);
        var selected=trace.Floors[chosenLeg];var pose=poses[selected.CellIndex];
        Vector3 initial=selected.Path.Segments[0].Start.ToVector3();int triangle=selected.Path.Segments[0].TriangleIndex;
        var worldFeet=localHit!.Center with{Y=localHit.Center.Y-half.Y-feetClearance};
        var feet=Local(worldFeet,pose);var normal=LocalDirection(localHit.Normal,pose);
        int remaining=maximumCrossings-(chosenLeg==0?0:trace.Floors[0].Path.Result.Crossings);
        bool Validate(Vector3 position)
        {
            var world=World(new(position.X,position.Y,position.Z),pose);
            double dx=world.X-worldFeet.X,dy=world.Y-worldFeet.Y,dz=world.Z-worldFeet.Z;
            if(dx*dx+dy*dy+dz*dz>1e-8||dx*localHit.Normal.X+dy*localHit.Normal.Y+dz*localHit.Normal.Z< -1e-6)return false;
            var candidate=floors[selected.CellIndex].Trace(triangle,initial,position,remaining);
            var stationary=floors[selected.CellIndex].Trace(candidate.Result.TriangleIndex,position,position);
            foreach(var collider in colliders.Values)
            {
                if(FloorPathTriangleBoxSweep.Sweep(WorldPath(stationary,pose),collider,half,true,feetClearance).Status!=TriangleBoxContactStatus.Clear)return false;
                var swept=FloorPathTriangleBoxSweep.Sweep(WorldPath(candidate,pose),collider,half,true,feetClearance);
                if(swept.Status!=TriangleBoxContactStatus.Clear&&!(swept.Status==TriangleBoxContactStatus.Blocked&&swept.Parameter==candidate.Result.Parameter))return false;
            }
            return true;
        }
        var landing=FloorBoxContactLanding.Resolve(floors[selected.CellIndex],selected.Path,triangle,initial,feet,normal,
            localHit.SegmentIndex,localHit.Parameter,remaining,Validate);
        if(!landing.Accepted)
        {
            var held=floors[start.CellIndex].Walk(start.TriangleIndex,start.LocalPosition,start.LocalPosition);
            var movement=new CellFloorWalkResult(CellFloorWalkStatus.FloorStopped,new(start.CellIndex,held.TriangleIndex,held.Position),poses[start.CellIndex].TransformPoint(held.Position),0,0,null,held);
            return new(FloorBoxWalkStatus.ContactLandingUnsupported,movement,chosen,"UnrepresentablePortalTriangleContactLanding");
        }
        if(chosenLeg==1&&localHit.Parameter==0)
        {
            var source=trace.Floors[0].Path.Result;
            return new(FloorBoxWalkStatus.BoxStopped,new(CellFloorWalkStatus.FloorStopped,new(start.CellIndex,source.TriangleIndex,source.Position),
                poses[start.CellIndex].TransformPoint(source.Position),sourceParameter,0,null,source),chosen,"TargetSeamOverlap");
        }
        var result=new CellFloorWalkResult(CellFloorWalkStatus.FloorStopped,new(selected.CellIndex,landing.Floor.TriangleIndex,landing.Floor.Position),
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
    private static bool Finite(Vector3 v)=>float.IsFinite(v.X)&&float.IsFinite(v.Y)&&float.IsFinite(v.Z);
}
