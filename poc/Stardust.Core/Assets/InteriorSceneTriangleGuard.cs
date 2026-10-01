using System.Collections.ObjectModel;

namespace Stardust.Core.Assets;

public sealed record InteriorSceneTriangleGuard(uint Selector,IReadOnlyDictionary<int,StaticMeshCollisionGeometry> Geometry,
    int SourceTriangles,int EligibleTriangles,int DegenerateTriangles,int Triangles);

/// <summary>Explicit modern guard for selected direct static-mesh cells. It does not infer original actor participation.</summary>
public static class InteriorSceneTriangleGuardReader
{
    public static InteriorSceneTriangleGuard Create(InteriorSceneContentBundle content,uint selector,int maximumTriangles=2048)
    {
        ArgumentNullException.ThrowIfNull(content);
        if(selector>1||maximumTriangles is <0 or >65536)throw new ArgumentOutOfRangeException(nameof(selector));
        if(content.Rooms is null||content.Materials is null||content.Rooms.CellAppearances.Count>64)
            throw new InvalidDataException("Triangle guard requires selected room/material content.");
        var geometry=new Dictionary<int,StaticMeshCollisionGeometry>();int source=0,eligible=0,degenerate=0,total=0;
        foreach(var pair in content.Rooms.CellAppearances.OrderBy(p=>p.Key))
        {
            var template=pair.Value;string path=template.MeshSourcePath??template.Path;
            if(!content.Scene.Floors.ContainsKey(pair.Key)||template.Mesh is null||template.Children.Count!=0||
                !content.Materials.Meshes.TryGetValue(path,out var material))
                throw new InvalidDataException("Triangle guard supports explicitly loaded direct static-mesh cells only.");
            if(material.Mesh.Primitives.Sum(p=>(long)p.Indices.Count/3)>65536-source)
                throw new InvalidDataException("Scene guard source triangle budget.");
            var flags=material.Materials.ToDictionary(p=>p.Key,p=>MeshPrimitiveCollisionPolicy.FromInvariantCandidates(material.Effects[p.Value.References.EffectPath]));
            var selected=StaticMeshCollisionGeometryReader.Create(material.Mesh,flags,selector,maximumTriangles-total);
            source=checked(source+selected.SourceTriangleCount);eligible=checked(eligible+selected.EligibleTriangleCount);
            degenerate=checked(degenerate+selected.DegenerateTriangleCount);total=checked(total+selected.Triangles.Count);
            geometry.Add(pair.Key,selected);
        }
        return new(selector,new ReadOnlyDictionary<int,StaticMeshCollisionGeometry>(geometry),source,eligible,degenerate,total);
    }
}
