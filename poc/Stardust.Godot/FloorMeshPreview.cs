using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stardust.Core.Assets;

/// <summary>Offline floor, transfer and bounded permission-barrier inspection.</summary>
public sealed class FloorMeshPreview : IDisposable
{
    private readonly List<Resource> resources=new();
    private readonly Dictionary<int,Node3D> cells=new();
    private readonly Dictionary<int,FloorSurfaceQuery> queries=new();
    private readonly Dictionary<int,SingleCellFloorWalker> walkers=new();
    private readonly Dictionary<int,FloorMeshDescription> source=new();
    private readonly MeshInstance3D marker;
    private readonly ImmediateMesh walkLine;
    private readonly StandardMaterial3D walkMaterial;
    public FloorWalkResult? LastWalk{get;private set;}
    private readonly PortalFloorTransferQuery? transfers;
    private readonly PortalBarrierFloorWalker? barrierWalk;
    public PortalBarrierFloorResult? LastBarrier{get;private set;}
    public CellFloorWalkResult? LastTransfer{get;private set;}
    private readonly List<(FloorPathHit From,FloorPathHit To)> sampleTransfers=new();
    public IReadOnlyList<(FloorPathHit From,FloorPathHit To)> SampleTransfers=>sampleTransfers.AsReadOnly();
    private readonly List<(FloorPathHit From,FloorPathHit To)> sampleBarrierTransfers=new();
    public IReadOnlyList<(FloorPathHit From,FloorPathHit To)> SampleBarrierTransfers=>sampleBarrierTransfers.AsReadOnly();
    private int selected=-1;
    private readonly PortalSurfacePathQuery surfacePaths;
    public FloorPathHit? LastSelection{get;private set;}
    private readonly List<FloorPathHit> sampleSelections=new();
    public IReadOnlyList<FloorPathHit> SampleSelections=>sampleSelections.AsReadOnly();
    public Node3D Root{get;}=new();
    public int FloorCells=>cells.Count;
    public int Triangles{get;private set;}
    public FloorMeshPreview(IReadOnlyDictionary<int,FloorMeshDescription> floors,IReadOnlyDictionary<int,PathGraphDescription>? pathGraphs=null,PortalLayoutDescription? layout=null)
    {
        Dictionary<FloorMeshDescription,ArrayMesh> shared=new(System.Collections.Generic.ReferenceEqualityComparer.Instance);
        Dictionary<FloorMeshDescription,FloorSurfaceQuery> sharedQueries=new(System.Collections.Generic.ReferenceEqualityComparer.Instance);
        try
        {
            transfers=layout is null?null:new(layout,floors);
            barrierWalk=layout is null?null:new(layout,floors);
            foreach(var pair in floors)
            {
                FloorMeshDescription floor=pair.Value;
                if(!shared.TryGetValue(floor,out ArrayMesh? mesh))
                {
                    mesh=new();resources.Add(mesh);
                    foreach(bool nonSolid in new[]{false,true})
                    {
                        int[] indices=floor.Triangles.Where(t=>(t.NonSolidByte!=0)==nonSolid).SelectMany(t=>new[]{t.VertexIndices[0],t.VertexIndices[2],t.VertexIndices[1]}).ToArray();
                        if(indices.Length==0)continue;
                        Vector3[] vertices=floor.Vertices.Select(v=>new Vector3(v.X,v.Y,-v.Z)).ToArray();
                        using Godot.Collections.Array arrays=new();arrays.Resize((int)Godot.Mesh.ArrayType.Max);arrays[(int)Godot.Mesh.ArrayType.Vertex]=vertices;arrays[(int)Godot.Mesh.ArrayType.Index]=indices;
                        mesh.AddSurfaceFromArrays(Godot.Mesh.PrimitiveType.Triangles,arrays);
                        StandardMaterial3D material=new(){ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,CullMode=BaseMaterial3D.CullModeEnum.Disabled,AlbedoColor=nonSolid?new Color("#df8544"):new Color("#51c8a4")};resources.Add(material);mesh.SurfaceSetMaterial(mesh.GetSurfaceCount()-1,material);
                        using Godot.Collections.Array uploaded=mesh.SurfaceGetArrays(mesh.GetSurfaceCount()-1);
                        if(!uploaded[(int)Godot.Mesh.ArrayType.Vertex].AsVector3Array().SequenceEqual(vertices) || !uploaded[(int)Godot.Mesh.ArrayType.Index].AsInt32Array().SequenceEqual(indices))throw new AssetPreviewException("Floor geometry upload/reflection mismatch.");
                    }
                    shared.Add(floor,mesh);sharedQueries.Add(floor,new(floor));
                }
                Node3D node=new();node.AddChild(new MeshInstance3D{Mesh=mesh});Root.AddChild(node);cells.Add(pair.Key,node);source.Add(pair.Key,floor);queries.Add(pair.Key,sharedQueries[floor]);walkers.Add(pair.Key,new(floor));Triangles+=floor.Triangles.Count;
            }
            SphereMesh sphere=new(){Radius=0.1f,Height=0.2f};StandardMaterial3D markerMaterial=new(){ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,AlbedoColor=new Color("#ffd86b")};resources.Add(sphere);resources.Add(markerMaterial);sphere.Material=markerMaterial;
            surfacePaths=new(queries,(pathGraphs??new Dictionary<int,PathGraphDescription>()).ToDictionary(pair=>pair.Key,pair=>new DirectedPathGraphQuery(pair.Value)));
            walkLine=new();walkMaterial=new(){ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,AlbedoColor=new Color("#ffd86b")};resources.Add(walkLine);resources.Add(walkMaterial);Root.AddChild(new MeshInstance3D{Mesh=walkLine});
            marker=new(){Mesh=sphere,Visible=false};Root.AddChild(marker);SelectCell(-1);
        }
        catch{Root.Free();Dispose();throw;}
    }
    public void SelectCell(int cell){selected=cell;foreach(var pair in cells)pair.Value.Visible=cell<0 || pair.Key==cell;marker.Visible=false;LastSelection=null;LastWalk=null;LastTransfer=null;LastBarrier=null;walkLine.ClearSurfaces();}
    public string WalkTransfer(FloorPathHit from,FloorPathHit to,PortalTransferPermission policy,bool actorBounds=false)
    {
        LastWalk=null;LastTransfer=null;LastBarrier=null;walkLine.ClearSurfaces();
        if(transfers is null)return "Portal movement: layout unavailable";
        LastTransfer=transfers.Walk(new(from.CellIndex,from.Surface.TriangleIndex,from.Surface.Position),to.Surface.Position,_=>policy);
        if(actorBounds && barrierWalk is not null)
        {
            LastBarrier=barrierWalk.Walk(new(from.CellIndex,from.Surface.TriangleIndex,from.Surface.Position),to.Surface.Position,new(.25f,.9f,.25f),_=>policy);
            LastTransfer=LastBarrier.Movement;
        }
        DrawMovement(from.Surface.Position,LastTransfer.WorldPosition);
        string body=actorBounds?$" • actor box .5×1.8×.5 • {LastBarrier?.Diagnostic??LastBarrier?.Contact?.Status.ToString()??"clear"}":"";
        return $"Portal movement: {LastTransfer.Status} • cell {from.CellIndex} → {LastTransfer.Locator.CellIndex} • transfers {LastTransfer.Transfers} • offline policy{body}";
    }
    public string Walk(FloorPathHit from,FloorPathHit to)
    {
        walkLine.ClearSurfaces();LastWalk=null;
        if(from.CellIndex!=to.CellIndex)return "Movement: choose two surfaces in the same cell";
        LastWalk=walkers[from.CellIndex].Walk(from.Surface.TriangleIndex,from.Surface.Position,to.Surface.Position);
        var result=LastWalk;DrawMovement(from.Surface.Position,result.Position);
        return $"Movement: {result.Status} • cell {from.CellIndex} • triangle {result.TriangleIndex} • crossings {result.Crossings}"+(result.EdgeIndex>=0?$" • edge {result.EdgeIndex}":"")+(result.PortalId!=-1?$" • portal {result.PortalId}":"");
    }
    private void DrawMovement(System.Numerics.Vector3 from,System.Numerics.Vector3 to)
    {
        Transform3D inverse=Root.GlobalTransform.AffineInverse();
        Vector3 localStart=inverse*new Vector3(from.X,from.Y,-from.Z), localEnd=inverse*new Vector3(to.X,to.Y,-to.Z);
        marker.Visible=true;marker.Position=localEnd;
        walkLine.SurfaceBegin(Godot.Mesh.PrimitiveType.Lines,walkMaterial);
        walkLine.SurfaceAddVertex(localStart+new Vector3(0,.03f,0));
        walkLine.SurfaceAddVertex(marker.Position+new Vector3(0,.03f,0));walkLine.SurfaceEnd();
    }
    public void ShowActorWalk(System.Numerics.Vector3 worldStart, PortalBarrierFloorResult result)
    {
        LastBarrier=result;LastTransfer=result.Movement;walkLine.ClearSurfaces();DrawMovement(worldStart,result.Movement.WorldPosition);
    }
    public string Pick(Vector3 start,Vector3 end)
    {
        System.Numerics.Vector3 a=new(start.X,start.Y,-start.Z),b=new(end.X,end.Y,-end.Z);LastSelection=surfacePaths.Pick(a,b,selected);FloorSegmentHit? nearest=LastSelection?.Surface;int hitCell=LastSelection?.CellIndex??-1;
        marker.Visible=nearest is not null;
        if(nearest is null)return "Surface query: no solid hit";
        marker.Position=new(nearest.Position.X,nearest.Position.Y,-nearest.Position.Z);
        return $"Surface query: cell {hitCell}, triangle {nearest.TriangleIndex}, authored ID {nearest.TriangleId}"+(LastSelection!.NearestNode.HasValue?$" • nearest graph node {LastSelection.NearestNode}":$" • {LastSelection.Status}");
    }
    public void VerifyQueries()
    {
        int verified=0;
        foreach(var pair in source)
        {
            FloorMeshDescription floor=pair.Value;
            foreach(FloorTriangle t in floor.Triangles)
            {
                if(t.NonSolidByte!=0)continue;
                var a=floor.Vertices[t.VertexIndices[0]];var b=floor.Vertices[t.VertexIndices[1]];var c=floor.Vertices[t.VertexIndices[2]];
                if(System.Numerics.Vector3.Cross(b-a,c-a).Y==0)continue;
                float x=(float)(((double)a.X+b.X+c.X)/3),z=(float)(((double)a.Z+b.Z+c.Z)/3);
                float lo=floor.Vertices.Min(v=>v.Y)-1,hi=floor.Vertices.Max(v=>v.Y)+1;
                if(queries[pair.Key].IntersectSegment(new(x,hi,z),new(x,lo,z)) is null)throw new AssetPreviewException("Floor host surface query missed solid triangle center.");SelectCell(pair.Key);string result=Pick(new(x,hi,-z),new(x,lo,-z));
                if(!marker.Visible || !result.StartsWith($"Surface query: cell {pair.Key},",StringComparison.Ordinal))throw new AssetPreviewException("Floor reflected picking/cell filtering mismatch.");sampleSelections.Add(LastSelection!);verified++;break;
            }
        }
        int movement=0;
        foreach(FloorPathHit hit in sampleSelections)
        {
            var vertex=source[hit.CellIndex].Vertices[source[hit.CellIndex].Triangles[hit.Surface.TriangleIndex].VertexIndices[0]];
            var destination=(hit.Surface.Position+vertex)*.5f;
            string message=Walk(hit,hit with{Surface=hit.Surface with{Position=destination}});
            if(LastWalk?.Status!=FloorWalkStatus.Accepted || marker.Position.X!=destination.X || marker.Position.Z!=-destination.Z || !message.StartsWith("Movement: Accepted",StringComparison.Ordinal))throw new AssetPreviewException("Floor movement adapter/reflection mismatch.");movement++;
        }
        SelectCell(-1);marker.Visible=false;
        GD.Print($"PASS: floor geometry/upload/query inspection cells={FloorCells} triangles={Triangles} center_probes={verified}");
        GD.Print($"PASS: single-cell movement adapter/reflection probes={movement}");
        if(transfers is not null)
        {
            FloorPathHit Hit(int cell,int triangle)
            {
                FloorMeshDescription f=source[cell];FloorTriangle t=f.Triangles[triangle];var p=(f.Vertices[t.VertexIndices[0]]+f.Vertices[t.VertexIndices[1]]+f.Vertices[t.VertexIndices[2]])/3;
                return new(cell,new(triangle,t.Id,0,p,System.Numerics.Vector3.One/3,System.Numerics.Vector3.UnitY),null,FloorPathSelectionStatus.MissingCellGraph);
            }
            int checkedTransfers=0,checkedBarriers=0,unsupportedBarriers=0;
            foreach(PortalFloorLink link in transfers.Links)
            {
                var from=Hit(link.SourceCell,link.SourceTriangle);var to=Hit(link.TargetCell,link.TargetTriangle);sampleTransfers.Add((from,to));
                WalkTransfer(from,to,new(true,true));
                if(LastTransfer?.Status!=CellFloorWalkStatus.Accepted || LastTransfer.Locator.CellIndex!=link.TargetCell || marker.Position.X!=to.Surface.Position.X || marker.Position.Z!=-to.Surface.Position.Z)throw new AssetPreviewException("Reflected portal transfer owner/position mismatch.");
                WalkTransfer(from,to,new(false,true));
                if(LastTransfer?.Status!=CellFloorWalkStatus.EntryDenied || LastTransfer.Locator.CellIndex!=link.SourceCell || LastTransfer.Transfers!=0)throw new AssetPreviewException("Denied portal transfer changed owner.");checkedTransfers++;
                WalkTransfer(from,to,new(false,true),true);
                if(LastBarrier?.Diagnostic is not null)
                {
                    if(LastTransfer?.Status!=CellFloorWalkStatus.FloorStopped || LastTransfer.Locator.CellIndex!=link.SourceCell || LastTransfer.WorldPosition!=from.Surface.Position)throw new AssetPreviewException("Unsupported barrier sweep failed to retain source locator.");unsupportedBarriers++;
                }
                if(LastBarrier?.Contact is not null)
                {
                    sampleBarrierTransfers.Add((from,to));
                    if(LastTransfer?.Locator.CellIndex!=link.SourceCell || LastTransfer.Transfers!=0 || marker.Position.X!=LastTransfer.WorldPosition.X || marker.Position.Z!=-LastTransfer.WorldPosition.Z)throw new AssetPreviewException("Barrier actor contact owner/reflection mismatch.");
                    WalkTransfer(from,to,new(true,true),true);
                    if(LastTransfer?.Status!=CellFloorWalkStatus.Accepted || LastBarrier?.Contact is not null)throw new AssetPreviewException("Barrier current permission failed to recover.");checkedBarriers++;
                }
            }
            SelectCell(-1);GD.Print($"PASS: portal transfer adapter/reflection directed_links={checkedTransfers} allowed_denied_probes={checkedTransfers*2}");
            GD.Print($"PASS: portal barrier actor-box adapter/reflection checked_links={checkedBarriers} current_policy_probes={checkedBarriers*2} explicit_nonlevel_stops={unsupportedBarriers}");
        }
    }
    public void Dispose(){foreach(Resource resource in resources.AsEnumerable().Reverse())resource.Dispose();resources.Clear();cells.Clear();queries.Clear();walkers.Clear();source.Clear();sampleTransfers.Clear();sampleBarrierTransfers.Clear();sampleSelections.Clear();}
}
