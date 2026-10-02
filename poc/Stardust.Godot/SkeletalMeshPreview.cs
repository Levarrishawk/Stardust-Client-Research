using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stardust.Core.Assets;

/// <summary>Owned original geometry with diagnostic or selected sampled poses and optional authored MAIN textures.</summary>
internal sealed class SkeletalMeshPreview : IDisposable
{
    internal Node3D Root{get;}=new(){Name="OriginalBindGeometry"};
    internal Aabb Bounds{get;private set;}
    internal int Surfaces{get;private set;}
    internal int Vertices{get;private set;}
    internal int Triangles{get;private set;}
    internal float MaximumDisplacement{get;private set;}
    private readonly List<Resource> resources=new();
    private sealed record PreviewMesh(ArrayMesh Mesh,SkeletalMeshGeometryDescription Source,List<Material> Materials,IReadOnlyList<int> TransformJoints)
    {internal IReadOnlyList<SkeletalDeformedShader>? Deformed{get;set;}internal SkeletalMeshGeometryDescription SelectedSource{get;set;}=Source;internal string Path{get;init;}="";}
    private readonly List<PreviewMesh> meshes=new();
    private readonly SkeletalJointBinding binding;
    private readonly PreparedSkeletalAnimation? animation;
    private SkeletalPoseTransition? movementTransition;
    private string currentClipKey="idle";
    internal bool MovementTransitioning=>movementTransition?.Transitioning??false;
    private SkeletalMaterialPreview? materialPreview;
    private readonly CreatureAppearanceDescriptorBundle original;
    private readonly bool uploadTangents;
    private IReadOnlyList<System.Numerics.Matrix4x4>? currentLocalPose;
    internal int SelectedTextureValue(ShaderCustomizationKey key)=>materialPreview!.SelectedTextureValue(key);
    internal void ApplyCellLighting(CellLightingPreview rig)=>materialPreview!.ApplyCellLighting(rig);
    internal int TextureCount=>materialPreview?.TextureCount??0;
    internal long TextureBytes=>materialPreview?.TextureBytes??0;
    internal SkeletalMeshPreview(CreatureAppearanceDescriptorBundle bundle,bool unshaded=false,bool specularPreview=false,int meshDetail=0,bool vertexLitOnly=false)
    {
        original=bundle;
        uploadTangents=bundle.MaterialBundle?.CompressedNormals is not null;
        if(bundle.Geometries is null||bundle.Geometries.Count==0)throw new AssetPreviewException("Owned skeletal geometry required.");
        binding=SkeletalJointBindingBuilder.Create(bundle.Appearance,bundle.Skeletons??throw new AssetPreviewException("Owned skeletons required."),bundle.Appearance.Skeletons.ToDictionary(r=>r.Path,_=>0));
        animation=bundle.PreparedAnimation;
        var resolved=bundle.Geometries.ToDictionary(pair=>pair.Key,pair=>binding.Resolve(pair.Value.Skin),StringComparer.Ordinal);
        bool bounded=false;int slot=0;
        try
        {
            if(bundle.GeneratedFaces is not null&&bundle.MaterialBundle is null)throw new AssetPreviewException("Generated face display requires owned materials.");
            if(bundle.MaterialBundle is not null)materialPreview=new SkeletalMaterialPreview(bundle.MaterialBundle,bundle.GeneratedFaces,specularPreview,vertexLitOnly);
            foreach(string generator in bundle.Appearance.MeshGenerators)
            {
                string path=bundle.LodMeshes[generator].ExposedPaths()[meshDetail];var source=bundle.Geometries[path];var mesh=Own(new ArrayMesh());var materials=new List<Material>();
                for(int shaderIndex=0;shaderIndex<source.Shaders.Count;shaderIndex++)
                {
                    var shader=source.Shaders[shaderIndex];
                    if(uploadTangents&&bundle.MaterialBundle!.CompressedNormals!.Materials.ContainsKey(shader.ShaderPath)&&shader.TangentIndices is null)throw new AssetPreviewException("Normal-mapped shader requires owned tangent indices.");
                    using var arrays=Arrays(source,shader,uploadTangents:uploadTangents);mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles,arrays);
                    Material material=materialPreview?.Get(shader.ShaderPath,path,(uint)shaderIndex)??Own(new StandardMaterial3D{AlbedoColor=slot++%2==0?new Color("#83aac4"):new Color("#60798b"),Roughness=.9f,CullMode=BaseMaterial3D.CullModeEnum.Back,
                        ShadingMode=unshaded?BaseMaterial3D.ShadingModeEnum.Unshaded:BaseMaterial3D.ShadingModeEnum.PerPixel});
                    materials.Add(material);
                    mesh.SurfaceSetMaterial(mesh.GetSurfaceCount()-1,material);Surfaces++;Vertices+=shader.PositionIndices.Count;Triangles+=shader.Triangles.Count;
                    foreach(uint index in shader.PositionIndices)
                    {
                        var p=source.Skin.Positions[(int)index];Vector3 point=new(p.X,p.Y,-p.Z);
                        if(!bounded){Bounds=new(point,Vector3.Zero);bounded=true;}else Bounds=Bounds.Expand(point);
                    }
                }
                Root.AddChild(new MeshInstance3D{Mesh=mesh,Name=path.Contains("head")?"HeadBindGeometry":"BodyBindGeometry"});meshes.Add(new(mesh,source,materials,resolved[path]){Path=path});
            }
            if(!bounded||Surfaces==0||Triangles==0)throw new AssetPreviewException("Empty skeletal preview geometry.");
            currentLocalPose=binding.Joints.Select(j=>j.LocalBind).ToArray();
            if(bundle.ShapeSelection is not null)ApplyShapes(bundle);
        }
        catch{Dispose();throw;}
    }
    private static Godot.Collections.Array Arrays(SkeletalMeshGeometryDescription source,SkeletalShaderGeometry shader,SkeletalDeformedShader? deformed=null,bool uploadTangents=false)
    {
        if(shader.NormalIndices is null||shader.Coordinates.Count==0||shader.Coordinates[0].Dimension!=2)throw new AssetPreviewException("Neutral skeletal inspection requires original normals and a 2D UV set.");
        Vector3[] vertices=(deformed?.Positions??shader.PositionIndices.Select(i=>source.Skin.Positions[(int)i]).ToArray()).Select(p=>new Vector3(p.X,p.Y,-p.Z)).ToArray();
        Vector3[] normals=(deformed?.Normals??shader.NormalIndices.Select(i=>source.Skin.Normals[(int)i]).ToArray()).Select(p=>new Vector3(p.X,p.Y,-p.Z)).ToArray();
        // Godot's packed normal representation stores unit directions; the core retains raw weighted sums.
        if(deformed is not null)for(int i=0;i<normals.Length;i++){if(normals[i].LengthSquared()<1e-12f)throw new AssetPreviewException("Zero diagnostic normal cannot be packed.");normals[i]=normals[i].Normalized();}
        var raw=shader.Coordinates[0].Values;Vector2[] uv=Enumerable.Range(0,vertices.Length).Select(i=>new Vector2(raw[i*2],raw[i*2+1])).ToArray();
        int[] indices=shader.Triangles.SelectMany(t=>new[]{(int)t.A,(int)t.B,(int)t.C}).ToArray();
        float[]? tangents=null;
        if(uploadTangents&&shader.TangentIndices is not null)
        {
            var rawTangents=deformed?.Tangents??shader.TangentIndices.Select(i=>source.Tangents[(int)i]).ToArray();
            if(rawTangents.Count!=vertices.Length)throw new AssetPreviewException("Tangent upload count differs.");
            tangents=new float[vertices.Length*4];
            for(int i=0;i<vertices.Length;i++)
            {
                var rawTangent=rawTangents[i];Vector3 tangent=new(rawTangent.X,rawTangent.Y,-rawTangent.Z);
                if(normals[i].LengthSquared()<1e-12f||Math.Abs(rawTangent.W)!=1||!float.IsFinite(rawTangent.X)||!float.IsFinite(rawTangent.Y)||!float.IsFinite(rawTangent.Z))throw new AssetPreviewException("Invalid packed tangent basis.");
                normals[i]=normals[i].Normalized();tangent-=normals[i]*normals[i].Dot(tangent);
                if(tangent.LengthSquared()<1e-12f)throw new AssetPreviewException("Degenerate packed tangent basis.");tangent=tangent.Normalized();
                tangents[i*4]=tangent.X;tangents[i*4+1]=tangent.Y;tangents[i*4+2]=tangent.Z;tangents[i*4+3]=-rawTangent.W;
            }
        }
        Godot.Collections.Array arrays=new();arrays.Resize((int)Mesh.ArrayType.Max);arrays[(int)Mesh.ArrayType.Vertex]=vertices;arrays[(int)Mesh.ArrayType.Normal]=normals;arrays[(int)Mesh.ArrayType.TexUV]=uv;arrays[(int)Mesh.ArrayType.Index]=indices;
        if(tangents is not null)arrays[(int)Mesh.ArrayType.Tangent]=tangents;return arrays;
    }
    internal void VerifyBoundary(bool uploaded)
    {
        foreach(var entry in meshes)
        {
            if(entry.Mesh.GetSurfaceCount()!=entry.Source.Shaders.Count)throw new AssetPreviewException("Skeletal surface count mismatch.");
            for(int surface=0;surface<entry.Source.Shaders.Count;surface++)
            {
                using var expected=Arrays(entry.SelectedSource,entry.SelectedSource.Shaders[surface],entry.Deformed?[surface],uploadTangents);
                if(!uploaded)continue;using var actual=entry.Mesh.SurfaceGetArrays(surface);
                if(entry.Mesh.SurfaceGetMaterial(surface).GetInstanceId()!=entry.Materials[surface].GetInstanceId())throw new AssetPreviewException("Skeletal pose update replaced its retained material resource.");
                bool Near3(Vector3[] a,Vector3[] b,float tolerance)=>a.Length==b.Length&&a.Zip(b).All(p=>p.First.DistanceTo(p.Second)<=tolerance);
                bool Near2(Vector2[] a,Vector2[] b,float tolerance)=>a.Length==b.Length&&a.Zip(b).All(p=>p.First.DistanceTo(p.Second)<=tolerance);
                if(!Near3(actual[(int)Mesh.ArrayType.Vertex].AsVector3Array(),expected[(int)Mesh.ArrayType.Vertex].AsVector3Array(),1e-5f)||
                    !Near3(actual[(int)Mesh.ArrayType.Normal].AsVector3Array(),expected[(int)Mesh.ArrayType.Normal].AsVector3Array(),.002f)||
                    !Near2(actual[(int)Mesh.ArrayType.TexUV].AsVector2Array(),expected[(int)Mesh.ArrayType.TexUV].AsVector2Array(),.002f)||
                    !actual[(int)Mesh.ArrayType.Index].AsInt32Array().SequenceEqual(expected[(int)Mesh.ArrayType.Index].AsInt32Array()))throw new AssetPreviewException("Skeletal upload readback mismatch.");
                if(uploadTangents&&entry.Source.Shaders[surface].TangentIndices is not null)
                {
                    var a=actual[(int)Mesh.ArrayType.Tangent].AsFloat32Array();var b=expected[(int)Mesh.ArrayType.Tangent].AsFloat32Array();
                    if(a.Length!=b.Length||a.Zip(b).Where((p,i)=>i%4!=3).Any(p=>Math.Abs(p.First-p.Second)>.003f)||a.Where((v,i)=>i%4==3).Zip(b.Where((v,i)=>i%4==3)).Any(p=>p.First!=p.Second))throw new AssetPreviewException("Reflected tangent GPU readback differs.");
                }
            }
        }
        if(uploaded)materialPreview?.VerifyUploadedTextures();
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
        ApplyLocalPose(local);
    }
    internal void ApplyCustomization(CreatureAppearanceDescriptorBundle revision)
    {
        if(!ReferenceEquals(original.Geometries,revision.Geometries)||!ReferenceEquals(original.PreparedAnimation,revision.PreparedAnimation)||
            !ReferenceEquals(original.Skeletons,revision.Skeletons)||revision.MaterialBundle is null||revision.GeneratedFaces is null||materialPreview is null)
            throw new AssetPreviewException("Customization changed its retained skeletal/animation resources.");
        materialPreview.ApplyCustomization(revision.MaterialBundle,revision.GeneratedFaces);
    }
    internal void ApplyShapes(CreatureAppearanceDescriptorBundle revision)
    {
        if(!ReferenceEquals(original.Geometries,revision.Geometries)||!ReferenceEquals(original.Skeletons,revision.Skeletons)||
            !ReferenceEquals(original.PreparedAnimation,revision.PreparedAnimation)||!ReferenceEquals(original.BlendShapes,revision.BlendShapes)||revision.ShapeSelection is null)
            throw new AssetPreviewException("Shape revision changed retained source identities.");
        var selected=revision.ShapeSelection.Geometries;
        if(selected.Count!=original.Geometries!.Count)throw new AssetPreviewException("Shape mesh set differs.");
        foreach(var entry in meshes)
        {
            if(!selected.TryGetValue(entry.Path,out var shape)||!ReferenceEquals(shape.Shaders,entry.Source.Shaders)||
                !ReferenceEquals(shape.Skin.Weights,entry.Source.Skin.Weights)||!ReferenceEquals(shape.Skin.WeightRanges,entry.Source.Skin.WeightRanges)||
                !ReferenceEquals(shape.Skin.TransformNames,entry.Source.Skin.TransformNames)||shape.Skin.Positions.Count!=entry.Source.Skin.Positions.Count||
                shape.Skin.Normals.Count!=entry.Source.Skin.Normals.Count||shape.Tangents.Count!=entry.Source.Tangents.Count)
                throw new AssetPreviewException("Shape revision changed topology or skin maps.");
        }
        var previous=meshes.Select(e=>e.SelectedSource).ToArray();
        try{foreach(var entry in meshes)entry.SelectedSource=selected[entry.Path];ApplyLocalPose(currentLocalPose!);}
        catch{for(int i=0;i<meshes.Count;i++)meshes[i].SelectedSource=previous[i];ApplyLocalPose(currentLocalPose!);throw;}
    }
    internal void ApplyTextureChoices(CreatureAppearanceDescriptorBundle revision)
    {
        if(!ReferenceEquals(original.Geometries,revision.Geometries)||materialPreview is null||revision.MaterialBundle?.TextureSelection is null||
            !ReferenceEquals(original.MaterialBundle!.Materials,revision.MaterialBundle.Materials)||!ReferenceEquals(original.MaterialBundle.Textures,revision.MaterialBundle.Textures)||
            !ReferenceEquals(original.MaterialBundle.CompressedNormals,revision.MaterialBundle.CompressedNormals))throw new AssetPreviewException("Texture choice changed retained source identities.");
        materialPreview.ApplyTextureChoices(revision.MaterialBundle.TextureSelection);
    }
    internal string GeometryFingerprint(bool includePose=true)
    {
        using var hash=System.Security.Cryptography.IncrementalHash.CreateHash(System.Security.Cryptography.HashAlgorithmName.SHA256);
        void Add(float value)=>hash.AppendData(BitConverter.GetBytes(value));
        foreach(var entry in meshes)
        {
            hash.AppendData(BitConverter.GetBytes(entry.Mesh.GetInstanceId()));
            for(int surface=0;surface<entry.Mesh.GetSurfaceCount();surface++)
            {
                hash.AppendData(BitConverter.GetBytes(entry.Mesh.SurfaceGetMaterial(surface).GetInstanceId()));
                using var arrays=entry.Mesh.SurfaceGetArrays(surface);
                if(includePose)
                {
                    foreach(var value in arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array()){Add(value.X);Add(value.Y);Add(value.Z);}
                    foreach(var value in arrays[(int)Mesh.ArrayType.Normal].AsVector3Array()){Add(value.X);Add(value.Y);Add(value.Z);}
                    if(uploadTangents&&arrays[(int)Mesh.ArrayType.Tangent].VariantType!=Variant.Type.Nil)foreach(float value in arrays[(int)Mesh.ArrayType.Tangent].AsFloat32Array())Add(value);
                }
                foreach(var value in arrays[(int)Mesh.ArrayType.TexUV].AsVector2Array()){Add(value.X);Add(value.Y);}
                foreach(var value in arrays[(int)Mesh.ArrayType.Index].AsInt32Array())hash.AppendData(BitConverter.GetBytes(value));
            }
        }
        return Convert.ToHexString(hash.GetHashAndReset());
    }
    internal void SetOriginalFrame(double frame)
    {
        if(animation is null)throw new AssetPreviewException("Prepared original animation required.");
        ApplyLocalPose(animation.CreateLocalPose(binding,frame));currentClipKey="idle";movementTransition=null;
    }
    internal void SetClipFrame(string key,double frame)
    {
        if(key=="idle"){SetOriginalFrame(frame);return;}
        if(original.AdditionalAnimations is null||!original.AdditionalAnimations.TryGetValue(key,out var clip))throw new AssetPreviewException("Prepared movement clip required.");
        ApplyLocalPose(clip.CreateLocalPose(binding,frame));currentClipKey=key;movementTransition=null;
    }
    internal void SetMovementFrame(string key,double frame,double seconds)
    {
        PreparedSkeletalAnimation clip;
        if(key=="idle")clip=animation??throw new AssetPreviewException("Prepared original animation required.");
        else if(original.AdditionalAnimations is null||!original.AdditionalAnimations.TryGetValue(key,out clip!))throw new AssetPreviewException("Prepared movement clip required.");
        var target=clip.CreateLocalPose(binding,frame);
        var transition=movementTransition??new SkeletalPoseTransition(currentLocalPose!,currentClipKey,.15);
        var candidate=transition.Prepare(key,target,seconds);ApplyLocalPose(candidate.LocalPose);transition.Commit(candidate);
        movementTransition=transition;currentClipKey=key;
    }
    private void ApplyLocalPose(IReadOnlyList<System.Numerics.Matrix4x4> local)
    {
        var world=binding.CreateWorldPose(local);
        var pending=meshes.Select(entry=>
        {
            var palette=binding.CreatePalette(world,entry.TransformJoints);
            return SkeletalShaderDeformation.CreateAll(entry.SelectedSource,palette).ToArray();
        }).ToArray();
        // Validate every CPU array before altering any published surface.
        for(int i=0;i<meshes.Count;i++)for(int surface=0;surface<pending[i].Length;surface++)using(Arrays(meshes[i].SelectedSource,meshes[i].Source.Shaders[surface],pending[i][surface],uploadTangents)){}
        bool bounded=false;Aabb bounds=default;float displacement=0;
        for(int i=0;i<meshes.Count;i++)
        {
            var entry=meshes[i];entry.Mesh.ClearSurfaces();entry.Deformed=pending[i];
            for(int surface=0;surface<entry.Source.Shaders.Count;surface++)
            {
                using var arrays=Arrays(entry.SelectedSource,entry.Source.Shaders[surface],pending[i][surface],uploadTangents);entry.Mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles,arrays);entry.Mesh.SurfaceSetMaterial(surface,entry.Materials[surface]);
                for(int vertex=0;vertex<pending[i][surface].Positions.Count;vertex++)
                {
                    var p=pending[i][surface].Positions[vertex];var authored=entry.SelectedSource.Skin.Positions[(int)entry.Source.Shaders[surface].PositionIndices[vertex]];
                    displacement=Math.Max(displacement,System.Numerics.Vector3.Distance(p,authored));
                    Vector3 point=new(p.X,p.Y,-p.Z);if(!bounded){bounds=new(point,Vector3.Zero);bounded=true;}else bounds=bounds.Expand(point);
                }
            }
        }
        Bounds=bounds;MaximumDisplacement=displacement;
        currentLocalPose=local.ToArray();
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
            arrays[(int)Mesh.ArrayType.TexUV].AsVector2Array()[2]!=new Vector2(.5f,.6f)||!arrays[(int)Mesh.ArrayType.Index].AsInt32Array().SequenceEqual(new[]{0,1,2}))throw new AssetPreviewException("Asymmetric skeletal position/normal map and winding fixture failed.");
    }
    private T Own<T>(T resource)where T:Resource{resources.Add(resource);return resource;}
    public void Dispose(){for(int i=resources.Count-1;i>=0;i--)resources[i].Dispose();resources.Clear();meshes.Clear();materialPreview?.Dispose();materialPreview=null;}
}
