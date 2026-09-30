using System.Numerics;
namespace Stardust.Core.Assets;

public readonly record struct CellFloorLocator(int CellIndex,int TriangleIndex,Vector3 LocalPosition);
public readonly record struct PortalTransferPermission(bool? AllowEntry,bool? BarrierOpen);
public sealed record PortalFloorLink(int SourceCell,int SourceTriangle,int SourceEdge,int SourcePortal,int GeometryIndex,int TargetCell,int TargetTriangle,int TargetEdge,int TargetPortal);
public enum CellFloorWalkStatus { Accepted, FloorStopped, UnpairedPortal, MissingTargetFloor, GeometryMismatch, EntryDenied, PortalBlocked, PermissionUnknown, LandingFailed, TransferLimit, InvalidStart }
public sealed record CellFloorWalkResult(CellFloorWalkStatus Status,CellFloorLocator Locator,Vector3 WorldPosition,double Parameter,int Transfers,PortalFloorLink? Link,FloorWalkResult? FloorResult);
public sealed record CellFloorTrace(int CellIndex,FloorWalkPath Path);
public sealed record CellFloorWalkPath(CellFloorWalkResult Result,IReadOnlyList<CellFloorTrace> Floors);

/// <summary>One paired-portal transfer with explicit owner poses and caller-supplied current permission/barrier state. Offline compatibility slice, not full actor collision.</summary>
public sealed class PortalFloorTransferQuery
{
    private const float EdgeTolerance=1e-4f;
    private readonly Dictionary<int,FloorMeshDescription> floors=new();
    private readonly Dictionary<int,SingleCellFloorWalker> walkers=new();
    private readonly Dictionary<int,AppearanceTransform> poses=new();
    private readonly Dictionary<(int,int,int),PortalFloorLink> links=new();
    private readonly Dictionary<(int,int,int),CellFloorWalkStatus> failures=new();
    public IReadOnlyList<PortalFloorLink> Links{get;}
    public PortalFloorTransferQuery(PortalLayoutDescription layout,IReadOnlyDictionary<int,FloorMeshDescription> cellFloors,IReadOnlyDictionary<int,AppearanceTransform>? ownerPoses=null,int maximumPairTests=1_000_000)
    {
        ArgumentNullException.ThrowIfNull(layout);ArgumentNullException.ThrowIfNull(cellFloors);
        if(maximumPairTests is <0 or >1_000_000)throw new ArgumentOutOfRangeException(nameof(maximumPairTests));
        if(layout.Cells.Count==0 || layout.Cells.SelectMany(c=>c.Portals).Any(r=>r.GeometryIndex<0 || r.GeometryIndex>=layout.Geometry.Count || r.TargetCell<0 || r.TargetCell>=layout.Cells.Count))throw new InvalidDataException("Invalid portal movement reference.");
        if(layout.Cells.Count>4096 || layout.Geometry.Count>4096 || layout.Cells.Sum(c=>(long)c.Portals.Count)>32768 || cellFloors.Count>4096 || cellFloors.Values.Sum(f=>(long)f.Vertices.Count)>1_000_000 || cellFloors.Values.Sum(f=>(long)f.Triangles.Count)>1_000_000)throw new InvalidDataException("Portal movement resource budget.");
        if(ownerPoses is not null && ownerPoses.Keys.Any(key=>!cellFloors.ContainsKey(key)))throw new InvalidDataException("Unknown portal movement pose key.");
        foreach(var pair in cellFloors)
        {
            if(pair.Key<0 || pair.Key>=layout.Cells.Count)throw new InvalidDataException("Portal floor cell index.");
            AppearanceTransform pose=ownerPoses is not null && ownerPoses.TryGetValue(pair.Key,out var supplied)?supplied:AppearanceTransform.Identity;
            ValidatePose(pose);poses.Add(pair.Key,pose);
            FloorMeshDescription f=pair.Value with{Vertices=pair.Value.Vertices.ToArray(),Triangles=pair.Value.Triangles.Select(t=>t with{VertexIndices=t.VertexIndices.ToArray(),Neighbors=t.Neighbors.ToArray(),PortalIds=t.PortalIds.ToArray(),EdgeFlags=t.EdgeFlags.ToArray()}).ToArray()};
            if(f.Vertices.Any(v=>!Finite(pose.TransformPoint(v))))throw new InvalidDataException("Nonfinite portal movement world vertex.");
            walkers.Add(pair.Key,new(f));floors.Add(pair.Key,f);
        }
        var sideMap=layout.Cells.SelectMany((cell,ci)=>cell.Portals.Select((r,ri)=>(Cell:ci,Reference:ri,Geometry:r.GeometryIndex))).GroupBy(s=>s.Geometry).ToDictionary(g=>g.Key,g=>g.ToArray());
        var boundaryMap=floors.SelectMany(pair=>pair.Value.Triangles.SelectMany((t,ti)=>Enumerable.Range(0,3).Where(e=>t.PortalIds[e]!=-1 && t.Neighbors[e]==-1).Select(e=>(Cell:pair.Key,Triangle:ti,Edge:e,Portal:t.PortalIds[e])))).GroupBy(e=>(e.Cell,e.Portal)).ToDictionary(g=>g.Key,g=>g.ToArray());
        int pairTests=0;
        foreach(var pair in floors.OrderBy(p=>p.Key))for(int ti=0;ti<pair.Value.Triangles.Count;ti++)for(int edge=0;edge<3;edge++)
        {
            FloorTriangle triangle=pair.Value.Triangles[ti];int portal=triangle.PortalIds[edge];if(portal==-1 || triangle.Neighbors[edge]!=-1)continue;
            var key=(pair.Key,ti,edge);var references=layout.Cells[pair.Key].Portals;
            if(portal<0 || portal>=references.Count){failures[key]=CellFloorWalkStatus.UnpairedPortal;continue;}
            int geometry=references[portal].GeometryIndex;
            // Runtime floor portal ID indexes the owning cell's reference vector, not the geometry pool.
            var sides=sideMap[geometry];
            if(sides.Length!=2 || sides[0].Cell==sides[1].Cell || !sides.Any(s=>s.Cell==pair.Key && s.Reference==portal)){failures[key]=CellFloorWalkStatus.UnpairedPortal;continue;}
            var target=sides.Single(s=>s.Cell!=pair.Key);
            if(references[portal].TargetCell!=target.Cell || layout.Cells[target.Cell].Portals[target.Reference].TargetCell!=pair.Key){failures[key]=CellFloorWalkStatus.UnpairedPortal;continue;}
            if(!floors.TryGetValue(target.Cell,out var targetFloor)){failures[key]=CellFloorWalkStatus.MissingTargetFloor;continue;}
            List<(int Triangle,int Edge)> candidates=new();
            var (a,b)=Edge(pair.Key,ti,edge);
            foreach(var targetEdge in boundaryMap.GetValueOrDefault((target.Cell,target.Reference),Array.Empty<(int Cell,int Triangle,int Edge,int Portal)>()))
            {
                if(++pairTests>maximumPairTests)throw new InvalidDataException("Portal movement pair-comparison budget.");
                int nt=targetEdge.Triangle,ne=targetEdge.Edge;
                var (c,d)=Edge(target.Cell,nt,ne);
                if(Vector3.Distance(a,d)<=EdgeTolerance && Vector3.Distance(b,c)<=EdgeTolerance)candidates.Add((nt,ne));
            }
            if(candidates.Count!=1){failures[key]=CellFloorWalkStatus.GeometryMismatch;continue;}
            var candidate=candidates[0];links.Add(key,new(pair.Key,ti,edge,portal,geometry,target.Cell,candidate.Triangle,candidate.Edge,target.Reference));
        }
        Links=Array.AsReadOnly(links.Values.OrderBy(l=>l.SourceCell).ThenBy(l=>l.SourceTriangle).ThenBy(l=>l.SourceEdge).ToArray());
    }
    public CellFloorWalkResult Walk(CellFloorLocator start,Vector3 worldDesired,Func<PortalFloorLink,PortalTransferPermission> permission,int maximumCrossings=4096)
        =>WalkCore(start,worldDesired,permission,maximumCrossings,null);
    public CellFloorWalkPath Trace(CellFloorLocator start,Vector3 worldDesired,Func<PortalFloorLink,PortalTransferPermission> permission,int maximumCrossings=4096)
    {
        List<CellFloorTrace> paths=new();var result=WalkCore(start,worldDesired,permission,maximumCrossings,paths.Add);
        return new(result,paths.AsReadOnly());
    }
    private CellFloorWalkResult WalkCore(CellFloorLocator start,Vector3 worldDesired,Func<PortalFloorLink,PortalTransferPermission> permission,int maximumCrossings,Action<CellFloorTrace>? observe)
    {
        ArgumentNullException.ThrowIfNull(permission);if(!Finite(worldDesired) || !Finite(start.LocalPosition))throw new InvalidDataException("Nonfinite portal movement.");
        if(maximumCrossings is <0 or >4096)throw new ArgumentOutOfRangeException(nameof(maximumCrossings));
        if(!walkers.TryGetValue(start.CellIndex,out var walker))return new(CellFloorWalkStatus.InvalidStart,start,start.LocalPosition,0,0,null,null);
        FloorWalkPath? sourcePath=observe is null?null:walker.Trace(start.TriangleIndex,start.LocalPosition,Local(start.CellIndex,worldDesired),maximumCrossings);
        FloorWalkResult source=sourcePath?.Result??walker.Walk(start.TriangleIndex,start.LocalPosition,Local(start.CellIndex,worldDesired),maximumCrossings);
        if(sourcePath is not null)observe!(new(start.CellIndex,sourcePath));
        CellFloorWalkResult Stop(CellFloorWalkStatus status)=>Result(status,start.CellIndex,source,source.Parameter,0,null);
        if(source.Status==FloorWalkStatus.Accepted)return Stop(CellFloorWalkStatus.Accepted);
        if(source.Status!=FloorWalkStatus.PortalBoundary)return Stop(source.Status==FloorWalkStatus.InvalidStart?CellFloorWalkStatus.InvalidStart:CellFloorWalkStatus.FloorStopped);
        var key=(start.CellIndex,source.TriangleIndex,source.EdgeIndex);
        if(!links.TryGetValue(key,out var link))return Stop(failures.GetValueOrDefault(key,CellFloorWalkStatus.UnpairedPortal));
        // Reconcile independently authored paired edges within the declared geometric tolerance.
        Vector3 worldContact=poses[start.CellIndex].TransformPoint(source.Position);var (a,b)=Edge(link.TargetCell,link.TargetTriangle,link.TargetEdge);
        Vector3 span=b-a;double length=Vector3.Dot(span,span);if(length==0)return Stop(CellFloorWalkStatus.LandingFailed);
        double fraction=Math.Clamp(Vector3.Dot(worldContact-a,span)/length,0,1);
        Vector3 landingWorld=a+span*(float)fraction;
        if(Vector3.Distance(worldContact,landingWorld)>EdgeTolerance)return Stop(CellFloorWalkStatus.GeometryMismatch);
        Vector3 landing=Local(link.TargetCell,landingWorld),desired=Local(link.TargetCell,worldDesired);
        FloorWalkResult landingCheck=walkers[link.TargetCell].Walk(link.TargetTriangle,landing,landing,0);
        if(landingCheck.Status==FloorWalkStatus.InvalidStart)
        {
            // A float-rounded slanted edge can lie just outside the strict triangle.
            // Bridge at most 1e-5 units toward its interior; never relax the core walker globally.
            FloorMeshDescription f=floors[link.TargetCell];FloorTriangle t=f.Triangles[link.TargetTriangle];
            Vector3 center=(f.Vertices[t.VertexIndices[0]]+f.Vertices[t.VertexIndices[1]]+f.Vertices[t.VertexIndices[2]])/3;
            Vector3 inward=center-landing;float distance=inward.Length();
            if(distance>0)landing+=inward*Math.Min(1,1e-5f/distance);
            if(Vector3.Distance(worldContact,poses[link.TargetCell].TransformPoint(landing))>EdgeTolerance)return Stop(CellFloorWalkStatus.GeometryMismatch);
            landingCheck=walkers[link.TargetCell].Walk(link.TargetTriangle,landing,landing,0);
        }
        if(landingCheck.Status!=FloorWalkStatus.Accepted)return Stop(CellFloorWalkStatus.LandingFailed);
        PortalTransferPermission decision=permission(link);
        if(!decision.AllowEntry.HasValue || !decision.BarrierOpen.HasValue)return Stop(CellFloorWalkStatus.PermissionUnknown);
        if(!decision.AllowEntry.Value)return Stop(CellFloorWalkStatus.EntryDenied);
        if(!decision.BarrierOpen.Value)return Stop(CellFloorWalkStatus.PortalBlocked);
        FloorWalkPath? targetPath=observe is null?null:walkers[link.TargetCell].Trace(link.TargetTriangle,landing,desired,maximumCrossings-source.Crossings);
        FloorWalkResult next=targetPath?.Result??walkers[link.TargetCell].Walk(link.TargetTriangle,landing,desired,maximumCrossings-source.Crossings);
        if(targetPath is not null)observe!(new(link.TargetCell,targetPath));
        double progress=source.Parameter+(1-source.Parameter)*next.Parameter;
        return Result(next.Status==FloorWalkStatus.Accepted?CellFloorWalkStatus.Accepted:next.Status==FloorWalkStatus.PortalBoundary?CellFloorWalkStatus.TransferLimit:CellFloorWalkStatus.FloorStopped,link.TargetCell,next,progress,1,link);
    }
    private CellFloorWalkResult Result(CellFloorWalkStatus status,int cell,FloorWalkResult walk,double parameter,int transfers,PortalFloorLink? link)
        =>new(status,new(cell,walk.TriangleIndex,walk.Position),poses[cell].TransformPoint(walk.Position),parameter,transfers,link,walk);
    private (Vector3,Vector3) Edge(int cell,int triangle,int edge)
    {
        FloorMeshDescription f=floors[cell];FloorTriangle t=f.Triangles[triangle];AppearanceTransform p=poses[cell];
        return (p.TransformPoint(f.Vertices[t.VertexIndices[edge]]),p.TransformPoint(f.Vertices[t.VertexIndices[(edge+1)%3]]));
    }
    private Vector3 Local(int cell,Vector3 world)
    {
        AppearanceTransform p=poses[cell];Vector3 v=world-p.Translation;
        return new(p.RowX.X*v.X+p.RowY.X*v.Y+p.RowZ.X*v.Z,p.RowX.Y*v.X+p.RowY.Y*v.Y+p.RowZ.Y*v.Z,p.RowX.Z*v.X+p.RowY.Z*v.Y+p.RowZ.Z*v.Z);
    }
    private static bool Finite(Vector3 v)=>float.IsFinite(v.X)&&float.IsFinite(v.Y)&&float.IsFinite(v.Z);
    private static void ValidatePose(AppearanceTransform p)
    {
        if(!Finite(p.RowX)||!Finite(p.RowY)||!Finite(p.RowZ)||!Finite(p.Translation) || p.RowY!=Vector3.UnitY || p.RowX.Y!=0 || p.RowZ.Y!=0 || Math.Abs(p.RowX.LengthSquared()-1)>1e-5 || Math.Abs(p.RowZ.LengthSquared()-1)>1e-5 || Math.Abs(Vector3.Dot(p.RowX,p.RowZ))>1e-5 || Vector3.Dot(Vector3.Cross(p.RowX,p.RowY),p.RowZ)<.99999f)throw new InvalidDataException("Portal movement supports finite rigid yaw owner poses only.");
    }
}
