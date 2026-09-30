using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stardust.Core.Assets;

/// <summary>Offline eager LOD inspection. A caller supplies selection distance; pivot-origin fidelity is not implied.</summary>
public sealed class DetailAppearancePreview : IDisposable
{
    private readonly Dictionary<string,StaticMeshPreview> children=new(StringComparer.Ordinal);
    private readonly TexturedDetailAppearance source;
    private readonly DetailAppearanceSelector selector;
    private int selected=-1;
    private readonly AppearanceExtent? extent;
    public DetailAppearancePreview(TexturedDetailAppearance source,bool allowMainTextureOnly=false)
    {
        this.source=source;selector=new(source.Description);
        extent=AppearanceExtentDecoder.DecodeBase(source.Description.Metadata.Children[0].Children[0]);
        try
        {
            foreach((string path,TexturedStaticMesh child) in source.Children)
            {
                StaticMeshPreview adapter=new(child,allowMainTextureOnly);children.Add(path,adapter);adapter.VerifyUploadedAsset(child);
            }
        }
        catch{Dispose();throw;}
    }
    public ArrayMesh Select(float distance)
    {
        selected=selector.Select(distance,selected,selector.Levels.Select(level=>level.ChildPath is not null && children.ContainsKey(level.ChildPath)).ToArray());
        if(selected<0) throw new InvalidOperationException("No loaded detail child.");
        return children[selector.Levels[selected].ChildPath!].Mesh;
    }
    public string Describe()
    {
        DetailAppearanceLevel level=selector.Levels[selected];TexturedStaticMesh child=source.Children[level.ChildPath!];
        return $"Detail {level.Id}: {child.Mesh.VertexCount} vertices • {child.Mesh.TriangleCount} triangles • {child.Materials.Count} batches";
    }
    public float CameraDistance(Vector3 cameraWorld,Transform3D world)=>GodotAppearanceTransform.Distance(source.Description.Pivot,extent,world,cameraWorld);
    public ArrayMesh SelectFromCamera(Vector3 cameraWorld,Transform3D world)=>Select(CameraDistance(cameraWorld,world));
    public void VerifyCameraSwitches(Transform3D world)
    {
        System.Numerics.Vector3 center=source.Description.Pivot==1?System.Numerics.Vector3.Zero:extent?.Center??System.Numerics.Vector3.Zero;
        Vector3 origin=world*new Vector3(center.X,center.Y,-center.Z);
        foreach(DetailAppearanceLevel level in selector.Levels)
        {
            float distance=level.LowerDistance+(level.UpperDistance-level.LowerDistance)*0.5f;
            ArrayMesh mesh=SelectFromCamera(origin+new Vector3(distance,0,0),world);
            if(!ReferenceEquals(mesh,children[level.ChildPath!].Mesh))throw new InvalidOperationException("Standalone camera/PIVT detail switch mismatch.");
        }
        Select(0);
    }
    public Aabb Bounds => children.Values.Select(child=>child.Mesh.GetAabb()).Aggregate((first,next)=>first.Merge(next));
    public void VerifySwitches()
    {
        foreach(DetailAppearanceLevel level in selector.Levels)
        {
            float distance=level.LowerDistance+(level.UpperDistance-level.LowerDistance)*0.5f;
            ArrayMesh mesh=Select(distance);
            if(!ReferenceEquals(mesh,children[level.ChildPath!].Mesh)) throw new InvalidOperationException("Authored detail switch selected another child.");
        }
        Select(0);
    }
    public void Dispose(){foreach(StaticMeshPreview child in children.Values)child.Dispose();children.Clear();}
}
