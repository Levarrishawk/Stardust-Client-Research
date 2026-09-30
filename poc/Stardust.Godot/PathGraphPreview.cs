using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stardust.Core.Assets;
public sealed class PathGraphPreview : IDisposable
{
    private readonly List<Resource> resources=new();
    private readonly Dictionary<int,(Node3D Node,PathGraphDescription Graph,DirectedPathGraphQuery Query)> graphs=new();
    private readonly ArrayMesh routeMesh=new();
    private int selected=-1;
    public Node3D Root{get;}=new();
    public int CurrentNodes=>graphs.TryGetValue(selected,out var graph)?graph.Graph.Nodes.Count:0;
    private static Vector3 Reflect(System.Numerics.Vector3 p)=>new(p.X,p.Y,-p.Z);
    public PathGraphPreview(PortalSceneBundle bundle)
    {
        try
        {
            resources.Add(routeMesh);StandardMaterial3D lineMaterial=new(){ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,AlbedoColor=new Color("#8eacd9")};resources.Add(lineMaterial);
            StandardMaterial3D glyphMaterial=new(){ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,VertexColorUseAsAlbedo=true};resources.Add(glyphMaterial);
            SphereMesh sphere=new(){Radius=0.08f,Height=0.16f,Material=glyphMaterial};resources.Add(sphere);
            Dictionary<int,PathGraphDescription> requested=bundle.CellPathGraphs.ToDictionary(pair=>pair.Key,pair=>pair.Value);if(bundle.LayoutPathGraph is not null)requested.Add(-1,bundle.LayoutPathGraph);
            if(requested.Values.Sum(graph=>graph.Nodes.Count)>4096 || requested.Values.Sum(graph=>graph.Edges.Count)>32768)throw new AssetPreviewException("Path graph inspection geometry budget.");
            foreach(var pair in requested)
            {
                PathGraphDescription graph=pair.Value;Node3D node=new();Root.AddChild(node);DirectedPathGraphQuery query=new(graph);graphs.Add(pair.Key,(node,graph,query));
                ArrayMesh edges=new();resources.Add(edges);Vector3[] positions=graph.Edges.SelectMany(e=>new[]{Reflect(graph.Nodes[e.From].SerializedPosition),Reflect(graph.Nodes[e.To].SerializedPosition)}).ToArray();
                AddLines(edges,positions,lineMaterial);node.AddChild(new MeshInstance3D{Mesh=edges});
                MultiMesh glyphs=new(){TransformFormat=MultiMesh.TransformFormatEnum.Transform3D,UseColors=true,Mesh=sphere,InstanceCount=graph.Nodes.Count};resources.Add(glyphs);
                for(int i=0;i<graph.Nodes.Count;i++)
                {
                    Transform3D transform=new(Basis.Identity,Reflect(graph.Nodes[i].SerializedPosition));glyphs.SetInstanceTransform(i,transform);glyphs.SetInstanceColor(i,Color.FromHsv((graph.Nodes[i].RawType*0.137f)%1,0.4f,1));
                    if(DisplayServer.GetName()!="headless" && glyphs.GetInstanceTransform(i)!=transform)throw new AssetPreviewException("Path node transform upload mismatch.");
                }
                node.AddChild(new MultiMeshInstance3D{Multimesh=glyphs});
            }
            Root.AddChild(new MeshInstance3D{Mesh=routeMesh});SelectCell(-1);VerifyRoutes();
        }
        catch{Root.Free();Dispose();throw;}
    }
    private static void AddLines(ArrayMesh mesh,Vector3[] positions,Material material)
    {
        if(positions.Length==0)return;using Godot.Collections.Array arrays=new();arrays.Resize((int)Godot.Mesh.ArrayType.Max);arrays[(int)Godot.Mesh.ArrayType.Vertex]=positions;
        mesh.AddSurfaceFromArrays(Godot.Mesh.PrimitiveType.Lines,arrays);mesh.SurfaceSetMaterial(mesh.GetSurfaceCount()-1,material);
        using Godot.Collections.Array uploaded=mesh.SurfaceGetArrays(mesh.GetSurfaceCount()-1);if(!uploaded[(int)Godot.Mesh.ArrayType.Vertex].AsVector3Array().SequenceEqual(positions))throw new AssetPreviewException("Path edge/route upload mismatch.");
    }
    public void SelectCell(int cell){selected=cell;foreach(var pair in graphs)pair.Value.Node.Visible=pair.Key==cell;routeMesh.ClearSurfaces();}
    public string ShowRoute(int from,int to)
    {
        routeMesh.ClearSurfaces();if(!graphs.TryGetValue(selected,out var graph) || graph.Graph.Nodes.Count==0)return "Path graph: unavailable for this cell";
        if(from<0 || to<0 || from>=graph.Graph.Nodes.Count || to>=graph.Graph.Nodes.Count)return "Route endpoint outside this graph";
        DirectedPathRoute? route=graph.Query.FindRoute(from,to);if(route is null)return $"Directed route: {from} → {to} is unreachable";
        if(routeMaterial is null){routeMaterial=new(){ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,AlbedoColor=new Color("#ffe46e"),NoDepthTest=true};resources.Add(routeMaterial);}
        Vector3[] positions=route.EdgeIndices.SelectMany(index=>new[]{Reflect(graph.Graph.Nodes[graph.Graph.Edges[index].From].SerializedPosition),Reflect(graph.Graph.Nodes[graph.Graph.Edges[index].To].SerializedPosition)}).ToArray();AddLines(routeMesh,positions,routeMaterial);
        return $"Directed Euclidean route {from} → {to}: {route.EdgeIndices.Count} edges, {route.Distance:0.###} units";
    }
    private StandardMaterial3D? routeMaterial;
    private void VerifyRoutes()
    {
        int routes=0;
        foreach(var pair in graphs)
        {
            SelectCell(pair.Key);if(pair.Value.Graph.Nodes.Count==0)continue;
            int last=pair.Value.Graph.Nodes.Count-1;DirectedPathRoute? route=pair.Value.Query.FindRoute(0,last);string message=ShowRoute(0,last);
            if(route is not null){if(!message.StartsWith("Directed Euclidean route",StringComparison.Ordinal))throw new AssetPreviewException("Path host route mismatch.");routes++;}
        }
        SelectCell(-1);GD.Print($"PASS: path graph node/edge/route upload graphs={graphs.Count} reachable_sample_routes={routes} node_transform_readback={(DisplayServer.GetName()=="headless"?"unavailable_headless":"verified_gpu")}");
    }
    public void Dispose(){foreach(Resource resource in resources.AsEnumerable().Reverse())resource.Dispose();resources.Clear();graphs.Clear();routeMaterial=null;}
}
