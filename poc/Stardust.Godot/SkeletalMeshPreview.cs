using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stardust.Core.Assets;

/// <summary>Neutral original geometry with explicit diagnostic skin poses; no animation player or generated face texture.</summary>
internal sealed class SkeletalMeshPreview : IDisposable
{
    internal Node3D Root{get;}=new(){Name="OriginalBindGeometry"};
    internal Aabb Bounds{get;private set;}
    internal int Surfaces{get;private set;}
    internal int Vertices{get;private set;}
    internal int Triangles{get;private set;}
    internal float MaximumDisplacement{get;private set;}
    private readonly List<Resource> resources=new();
    private sealed record PreviewMesh(ArrayMesh Mesh,SkeletalMeshGeometryDescription Source,List<StandardMaterial3D> Materials)
    {internal IReadOnlyList<SkeletalDeformedShader>? Deformed{get;set;}}
    private readonly List<PreviewMesh> meshes=new();
    private readonly SkeletalJointBinding binding;
    internal SkeletalMeshPreview(CreatureAppearanceDescriptorBundle bundle,bool unshaded=false)
    {
        if(bundle.Geometries is null||bundle.Geometries.Count==0)throw new AssetPreviewException("Owned skeletal geometry required.");
        binding=SkeletalJointBindingBuilder.Create(bundle.Appearance,bundle.Skeletons??throw new AssetPreviewException("Owned skeletons required."),bundle.Appearance.Skeletons.ToDictionary(r=>r.Path,_=>0));
        bool bounded=false;int slot=0;
        try
        {
            foreach(string generator in bundle.Appearance.MeshGenerators)
            {
                string path=bundle.LodMeshes[generator].ExposedPaths()[0];var source=bundle.Geometries[path];var mesh=Own(new ArrayMesh());var materials=new List<StandardMaterial3D>();
                foreach(var shader in source.Shaders)
                {
                    using var arrays=Arrays(source,shader);mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles,arrays);
                    var material=Own(new StandardMaterial3D{AlbedoColor=slot++%2==0?new Color("#83aac4"):new Color("#60798b"),Roughness=.9f,CullMode=BaseMaterial3D.CullModeEnum.Back,
                        ShadingMode=unshaded?BaseMaterial3D.ShadingModeEnum.Unshaded:BaseMaterial3D.ShadingModeEnum.PerPixel});
                    materials.Add(material);
                    mesh.SurfaceSetMaterial(mesh.GetSurfaceCount()-1,material);Surfaces++;Vertices+=shader.PositionIndices.Count;Triangles+=shader.Triangles.Count;
                    foreach(uint index in shader.PositionIndices)
                    {
                        var p=source.Skin.Positions[(int)index];Vector3 point=new(p.X,p.Y,-p.Z);
                        if(!bounded){Bounds=new(point,Vector3.Zero);bounded=true;}else Bounds=Bounds.Expand(point);
                    }
                }
                Root.AddChild(new MeshInstance3D{Mesh=mesh,Name=path.Contains("head")?"HeadBindGeometry":"BodyBindGeometry"});meshes.Add(new(mesh,source,materials));
            }
            if(!bounded||Surfaces==0||Triangles==0)throw new AssetPreviewException("Empty skeletal preview geometry.");
        }
        catch{Dispose();throw;}
    }
    private static Godot.Collections.Array Arrays(SkeletalMeshGeometryDescription source,SkeletalShaderGeometry shader,SkeletalDeformedShader? deformed=null)
    {
        if(shader.NormalIndices is null||shader.Coordinates.Count==0||shader.Coordinates[0].Dimension!=2)throw new AssetPreviewException("Neutral skeletal inspection requires original normals and a 2D UV set.");
        Vector3[] vertices=(deformed?.Positions??shader.PositionIndices.Select(i=>source.Skin.Positions[(int)i]).ToArray()).Select(p=>new Vector3(p.X,p.Y,-p.Z)).ToArray();
        Vector3[] normals=(deformed?.Normals??shader.NormalIndices.Select(i=>source.Skin.Normals[(int)i]).ToArray()).Select(p=>new Vector3(p.X,p.Y,-p.Z)).ToArray();
        // Godot's packed normal representation stores unit directions; the core retains raw weighted sums.
        if(deformed is not null)for(int i=0;i<normals.Length;i++){if(normals[i].LengthSquared()<1e-12f)throw new AssetPreviewException("Zero diagnostic normal cannot be packed.");normals[i]=normals[i].Normalized();}
        var raw=shader.Coordinates[0].Values;Vector2[] uv=Enumerable.Range(0,vertices.Length).Select(i=>new Vector2(raw[i*2],raw[i*2+1])).ToArray();
        int[] indices=shader.Triangles.SelectMany(t=>new[]{(int)t.A,(int)t.C,(int)t.B}).ToArray();
        Godot.Collections.Array arrays=new();arrays.Resize((int)Mesh.ArrayType.Max);arrays[(int)Mesh.ArrayType.Vertex]=vertices;arrays[(int)Mesh.ArrayType.Normal]=normals;arrays[(int)Mesh.ArrayType.TexUV]=uv;arrays[(int)Mesh.ArrayType.Index]=indices;return arrays;
    }
    internal void VerifyBoundary(bool uploaded)
    {
        foreach(var entry in meshes)
        {
            if(entry.Mesh.GetSurfaceCount()!=entry.Source.Shaders.Count)throw new AssetPreviewException("Skeletal surface count mismatch.");
            for(int surface=0;surface<entry.Source.Shaders.Count;surface++)
            {
                using var expected=Arrays(entry.Source,entry.Source.Shaders[surface],entry.Deformed?[surface]);
                if(!uploaded)continue;using var actual=entry.Mesh.SurfaceGetArrays(surface);
                bool Near3(Vector3[] a,Vector3[] b,float tolerance)=>a.Length==b.Length&&a.Zip(b).All(p=>p.First.DistanceTo(p.Second)<=tolerance);
                bool Near2(Vector2[] a,Vector2[] b,float tolerance)=>a.Length==b.Length&&a.Zip(b).All(p=>p.First.DistanceTo(p.Second)<=tolerance);
                if(!Near3(actual[(int)Mesh.ArrayType.Vertex].AsVector3Array(),expected[(int)Mesh.ArrayType.Vertex].AsVector3Array(),1e-5f)||
                    !Near3(actual[(int)Mesh.ArrayType.Normal].AsVector3Array(),expected[(int)Mesh.ArrayType.Normal].AsVector3Array(),.002f)||
                    !Near2(actual[(int)Mesh.ArrayType.TexUV].AsVector2Array(),expected[(int)Mesh.ArrayType.TexUV].AsVector2Array(),.002f)||
                    !actual[(int)Mesh.ArrayType.Index].AsInt32Array().SequenceEqual(expected[(int)Mesh.ArrayType.Index].AsInt32Array()))throw new AssetPreviewException("Skeletal upload readback mismatch.");
            }
        }
        VerifyAsymmetricMapping();
    }
    internal void SetDiagnosticPose(bool enabled)
    {
        var local=binding.Joints.Select(j=>j.LocalBind).ToArray();
        if(enabled)
        {
            int[] forearms=binding.Joints.Select((j,i)=>(j,i)).Where(p=>p.j.Name.Equals("lForeArm",StringComparison.OrdinalIgnoreCase)).Select(p=>p.i).ToArray();
            if(forearms.Length!=1)throw new AssetPreviewException("Selected diagnostic requires one left forearm joint.");
            local[forearms[0]]=System.Numerics.Matrix4x4.CreateRotationZ(-.8f)*local[forearms[0]];
        }
        var world=binding.CreateWorldPose(local);
        var pending=meshes.Select(entry=>
        {
            var palette=binding.CreatePalette(world,binding.Resolve(entry.Source.Skin));
            return entry.Source.Shaders.Select((_,i)=>SkeletalShaderDeformation.Create(entry.Source,i,palette)).ToArray();
        }).ToArray();
        // Validate every CPU array before altering any published surface.
        for(int i=0;i<meshes.Count;i++)for(int surface=0;surface<pending[i].Length;surface++)using(Arrays(meshes[i].Source,meshes[i].Source.Shaders[surface],pending[i][surface])){}
        bool bounded=false;Aabb bounds=default;float displacement=0;
        for(int i=0;i<meshes.Count;i++)
        {
            var entry=meshes[i];entry.Mesh.ClearSurfaces();entry.Deformed=pending[i];
            for(int surface=0;surface<entry.Source.Shaders.Count;surface++)
            {
                using var arrays=Arrays(entry.Source,entry.Source.Shaders[surface],pending[i][surface]);entry.Mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles,arrays);entry.Mesh.SurfaceSetMaterial(surface,entry.Materials[surface]);
                for(int vertex=0;vertex<pending[i][surface].Positions.Count;vertex++)
                {
                    var p=pending[i][surface].Positions[vertex];var authored=entry.Source.Skin.Positions[(int)entry.Source.Shaders[surface].PositionIndices[vertex]];
                    displacement=Math.Max(displacement,System.Numerics.Vector3.Distance(p,authored));
                    Vector3 point=new(p.X,p.Y,-p.Z);if(!bounded){bounds=new(point,Vector3.Zero);bounded=true;}else bounds=bounds.Expand(point);
                }
            }
        }
        Bounds=bounds;MaximumDisplacement=displacement;
    }
    private static void VerifyAsymmetricMapping()
    {
        var skin=new SkeletalMeshSkinSourceDescription(Array.Empty<int>(),Array.Empty<short>(),Array.Empty<string>(),Array.Empty<string>(),
            Array.AsReadOnly(new[]{new System.Numerics.Vector3(1,2,3),new System.Numerics.Vector3(4,2,5),new System.Numerics.Vector3(1,6,7)}),
            Array.Empty<SkeletalWeightRange>(),Array.Empty<SkeletalTransformWeight>(),Array.AsReadOnly(new[]{new System.Numerics.Vector3(1,0,0),new System.Numerics.Vector3(0,1,0),new System.Numerics.Vector3(0,0,1)}),Array.Empty<IffNode>());
        var triangles=Array.AsReadOnly(new[]{new SkeletalMeshTriangle(0,null,0,1,2)});
        var shader=new SkeletalShaderGeometry("synthetic",Array.AsReadOnly(new uint[]{2,0,1}),Array.AsReadOnly(new uint[]{1,2,0}),null,null,
            Array.AsReadOnly(new[]{new SkeletalTextureCoordinates(2,Array.AsReadOnly(new float[]{.1f,.2f,.3f,.4f,.5f,.6f}))}),triangles,triangles);
        var geometry=new SkeletalMeshGeometryDescription(skin,Array.Empty<System.Numerics.Vector4>(),Array.AsReadOnly(new[]{shader}),Array.Empty<SkeletalTextureRendererReference>(),1,0,1);
        using var arrays=Arrays(geometry,shader);
        if(!arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array().SequenceEqual(new[]{new Vector3(1,6,-7),new Vector3(1,2,-3),new Vector3(4,2,-5)})||
            !arrays[(int)Mesh.ArrayType.Normal].AsVector3Array().SequenceEqual(new[]{new Vector3(0,1,0),new Vector3(0,0,-1),new Vector3(1,0,0)})||
            arrays[(int)Mesh.ArrayType.TexUV].AsVector2Array()[2]!=new Vector2(.5f,.6f)||!arrays[(int)Mesh.ArrayType.Index].AsInt32Array().SequenceEqual(new[]{0,2,1}))throw new AssetPreviewException("Asymmetric skeletal position/normal map and winding fixture failed.");
    }
    private T Own<T>(T resource)where T:Resource{resources.Add(resource);return resource;}
    public void Dispose(){for(int i=resources.Count-1;i>=0;i--)resources[i].Dispose();resources.Clear();meshes.Clear();}
}
