using System.Numerics;

namespace Stardust.Core.Assets;
public enum FloorPathSelectionStatus { Selected,MissingCellGraph,EmptyCellGraph }
public sealed record FloorPathHit(int CellIndex,FloorSegmentHit Surface,int? NearestNode,FloorPathSelectionStatus Status);

/// <summary>Owned cell maps retaining immutable queries. Spatial inspection only; not a navigation or cross-cell policy.</summary>
public sealed class PortalSurfacePathQuery
{
    private readonly KeyValuePair<int,FloorSurfaceQuery>[] floors;
    private readonly Dictionary<int,DirectedPathGraphQuery> graphs;
    public PortalSurfacePathQuery(IReadOnlyDictionary<int,FloorSurfaceQuery> floors,IReadOnlyDictionary<int,DirectedPathGraphQuery> graphs)
    {
        ArgumentNullException.ThrowIfNull(floors);ArgumentNullException.ThrowIfNull(graphs);
        if(floors.Count>4096 || graphs.Count>4096)throw new InvalidDataException("Surface/path cell-map budget exceeded.");
        if(floors.Keys.Any(key=>key<0) || graphs.Keys.Any(key=>key<0))throw new ArgumentException("Surface/path maps require zero-based cell keys; layout graphs are separate.");
        this.floors=floors.OrderBy(pair=>pair.Key).ToArray();this.graphs=graphs.ToDictionary(pair=>pair.Key,pair=>pair.Value);
        if(this.floors.Any(pair=>pair.Value is null) || this.graphs.Values.Any(value=>value is null))throw new ArgumentException("Surface/path queries must be present.");
    }
    public FloorPathHit? Pick(Vector3 start,Vector3 end,int cellIndex=-1)
    {
        if(cellIndex<-1)throw new ArgumentOutOfRangeException(nameof(cellIndex));
        foreach(var point in new[]{start,end})if(!float.IsFinite(point.X)||!float.IsFinite(point.Y)||!float.IsFinite(point.Z))throw new InvalidDataException("Surface/path segment is nonfinite.");
        FloorSegmentHit? nearest=null;int selected=-1;
        foreach(var pair in floors)
        {
            if(cellIndex>=0 && cellIndex!=pair.Key)continue;
            var hit=pair.Value.IntersectSegment(start,end);
            if(hit is not null && (nearest is null || hit.Parameter<nearest.Parameter)){nearest=hit;selected=pair.Key;}
        }
        if(nearest is null)return null;
        if(!graphs.TryGetValue(selected,out var graph))return new(selected,nearest,null,FloorPathSelectionStatus.MissingCellGraph);
        int? node=graph.FindNearestNode(nearest.Position);
        return new(selected,nearest,node,node.HasValue?FloorPathSelectionStatus.Selected:FloorPathSelectionStatus.EmptyCellGraph);
    }
}
