using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Godot;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

public sealed record SceneStaticVisualLimits(int MaximumMeshes=64,int MaximumTextures=64,int MaximumSurfaces=256);

/// <summary>One exact finalized registry handle; owned Godot resources, borrowed immutable CPU closure.</summary>
public sealed class SceneStaticVisual : IDisposable
{
    private Func<bool>? currentLifetime;
    private SceneStaticResources? source;
    private GeometryAppearancePreview? geometry;
    private readonly List<Resource> owned=new();
    private readonly int ownerThread=System.Environment.CurrentManagedThreadId;
    private bool disposed;
    public Node3D Root{get;}
    public bool IsRetired=>disposed;
    public bool IsPublished{get;private set;}
    public int SelectedDetail{get;private set;}
    public int MeshInstances=>geometry?.MeshInstances??0;
    public int TextureCount=>geometry?.UniqueGpuTextures??0;
    public static int Allocations{get;private set;}
    public static int Disposals{get;private set;}
    public Aabb Bounds{get{OwnerThread();return disposed?default:GeometryAppearancePreview.BoundsFor(Root);}}
    public static bool Matches(SceneObjectRegistry registry,SceneObjectHandle handle,SceneStaticResources source)
    {
        ArgumentNullException.ThrowIfNull(registry);ArgumentNullException.ThrowIfNull(source);
        var current=registry.Find(handle.ObjectId);
        return registry.IsCurrent(handle) && current is {Phase:SceneObjectPhase.Finalized} && source.Recipe.Recipe==SceneObjectResourceRecipe.StaticAppearance && SameCreate(current.Create,source.Create);
    }
    private static bool SameCreate(SceneCreateObjectByCrcMessage a,SceneCreateObjectByCrcMessage b)=>SceneObjectLifecycleCodec.EncodeCreate(a).AsSpan().SequenceEqual(SceneObjectLifecycleCodec.EncodeCreate(b));
    public static void ValidateDescriptor(SceneStaticResources source,bool terminalFrame0Inspection,SceneStaticVisualLimits? limits=null)
    {
        ArgumentNullException.ThrowIfNull(source);limits??=new();
        if(limits.MaximumMeshes is <1 or >4096 || limits.MaximumTextures is <1 or >4096 || limits.MaximumSurfaces is <1 or >65536)throw new ArgumentOutOfRangeException(nameof(limits));
        var meshes=source.Appearance.Meshes;int surfaces=meshes.Values.Sum(m=>m.Mesh.Primitives.Count),textures=meshes.Values.SelectMany(m=>m.Textures.Keys).Distinct(StringComparer.Ordinal).Count();
        if(source.MaterialPolicy!=SceneStaticMaterialPolicy.RequireComplete || source.Recipe.Recipe!=SceneObjectResourceRecipe.StaticAppearance || source.Create.TemplateCrc!=source.Recipe.TemplateCrc || source.Appearance.Diagnostics.Count!=0 || meshes.Count==0)throw new InvalidDataException("Complete explicit static recipe/material closure required.");
        if(meshes.Count>limits.MaximumMeshes || surfaces>limits.MaximumSurfaces || textures>limits.MaximumTextures)throw new InvalidDataException("Static visual mesh/texture/surface capacity.");
        var seen=new HashSet<string>();int graphMeshes=0;
        void Visit(AppearanceGeometryTemplate node){if(!seen.Add(node.Path))return;if(node.Diagnostics.Count!=0)throw new InvalidDataException("Required geometry omitted.");if(node.Mesh is not null){graphMeshes++;if(!meshes.ContainsKey(node.Path))throw new InvalidDataException("Required material mesh absent.");}foreach(var c in node.Children)Visit(c.Template);}
        Visit(source.Appearance.Geometry.Root);if(graphMeshes!=meshes.Count)throw new InvalidDataException("Material/geometry closure differs.");
        if(terminalFrame0Inspection)
        {
            if(source.Create.TemplateCrc!=2907007746 || meshes.Count!=4 || source.Appearance.Geometry.Root.Detail?.Levels.Count!=4)throw new InvalidDataException("Pinned four-LOD terminal inspection required.");
            int screens=0;
            foreach(var mesh in meshes.Values)foreach(var primitive in mesh.Mesh.Primitives)
            {
                if(primitive.ShaderPath!=StaticSwitchTexturePreview.ShaderPath)continue;
                var material=mesh.Materials[primitive.ShaderPath];var switched=material.SwitchTexture;
                if(material.References.EffectPath!="effect/a_emis_full.eft" || switched is null || switched.Selection.ShaderPath!=StaticSwitchTexturePreview.ShaderPath || switched.Selection.ShaderSha256!=StaticSwitchTexturePreview.ShaderPin || switched.Selection.FrameIndex!=0)throw new InvalidDataException("Only exact pinned SWTS frame0 screen override permitted.");
                var main=material.References.Textures.Single(t=>t.Tag==0x4D41494E);
                if(main.SourceFlag!=0 || main.AssetPath!=switched.Description.Frames[0].TexturePath || material.Parameters.CoordinateSets[0x4D41494E]!=0)throw new InvalidDataException("Pinned MAIN/UV0 binding required.");screens++;
            }
            if(screens!=2)throw new InvalidDataException("Exactly two original screen surfaces required.");
        }
    }
    public static SceneStaticVisual? TryPublish(SceneObjectRegistry registry,SceneObjectHandle handle,SceneStaticResources source,Node parent,bool terminalFrame0Inspection=false,SceneStaticVisualLimits? limits=null,Action? beforePublication=null)
    {
        MainThread();if(!Matches(registry,handle,source))return null;
        var placement=SceneCreatePlacementBuilder.Create(source.Create,SceneCreateZeroQuaternionPolicy.Reject);
        return TryPublish(source,GodotAppearanceTransform.ToGodot(placement.Transform),parent,()=>Matches(registry,handle,source),terminalFrame0Inspection,limits,beforePublication);
    }
    /// <summary>Caller supplies accepted resources, derived placement and a borrowed lifetime predicate.</summary>
    public static SceneStaticVisual? TryPublish(SceneStaticResources source,Transform3D transform,Node parent,Func<bool> currentLifetime,bool terminalFrame0Inspection=false,SceneStaticVisualLimits? limits=null,Action? beforePublication=null)
    {
        MainThread();ArgumentNullException.ThrowIfNull(source);ArgumentNullException.ThrowIfNull(parent);ArgumentNullException.ThrowIfNull(currentLifetime);
        if(!currentLifetime())return null;
        if(!GodotObject.IsInstanceValid(parent) || !parent.IsInsideTree())throw new ArgumentException("Live parent required.");
        ValidateDescriptor(source,terminalFrame0Inspection,limits);SceneCreatureVisual.ValidateEngineTransform(transform);
        SceneStaticVisual? candidate=null;
        try
        {
            candidate=new(source,transform,currentLifetime,terminalFrame0Inspection);beforePublication?.Invoke();
            if(!currentLifetime()){candidate.Dispose();return null;}
            parent.AddChild(candidate.Root);
            if(!currentLifetime()){candidate.Dispose();return null;}
            candidate.Root.Visible=true;candidate.IsPublished=true;return candidate;
        }
        catch{candidate?.Dispose();throw;}
    }
    private SceneStaticVisual(SceneStaticResources source,Transform3D transform,Func<bool> currentLifetime,bool terminal)
    {
        this.currentLifetime=currentLifetime;this.source=source;
        Root=new(){Name="OwnedSceneStatic",Visible=false,Transform=transform};Allocations++;
        try
        {
            geometry=new(source.Appearance.Geometry,source.Appearance.Meshes,allowMainTextureOnly:terminal);Root.AddChild(geometry.Root);
            if(terminal)
            {
                var expected=source.Appearance.Meshes.Where(p=>p.Value.Materials.ContainsKey(StaticSwitchTexturePreview.ShaderPath)).Select(p=>p.Key+": "+StaticSwitchTexturePreview.ShaderPath+": Material is outside opaque MAIN inspection policy.").ToHashSet(StringComparer.Ordinal);
                if(geometry.NeutralSurfaces!=2 || geometry.MaterialDiagnostics.Count!=2 || !expected.SetEquals(geometry.MaterialDiagnostics) || geometry.UniqueGpuTextures!=7 || geometry.MeshInstances!=4)throw new InvalidDataException("Terminal outside exact accepted inspection boundary.");
                BindTerminalScreens();
            }
            else if(geometry.NeutralSurfaces!=0 || geometry.MaterialDiagnostics.Count!=0)throw new InvalidDataException("Strict opaque static presentation rejected unsupported materials.");
            if(geometry.OmittedSurfaces!=0)throw new InvalidDataException("Static authored surface omitted.");
            SelectDetailUnchecked(source.Appearance.Geometry.Root.Detail is null?0:source.Appearance.Geometry.Root.Detail.Levels.Count-1);
        }
        catch{Dispose();throw;}
    }
    private static IEnumerable<MeshInstance3D> Meshes(Node root)
    {foreach(Node child in root.GetChildren()){if(child is MeshInstance3D mesh)yield return mesh;foreach(var nested in Meshes(child))yield return nested;}}
    private void BindTerminalScreens()
    {
        StandardMaterial3D? inspection=null;int count=0;
        foreach(var node in Meshes(geometry!.Root))
        {
            var mesh=source!.Appearance.Meshes[node.GetMeta("original_mesh_path").AsString()];
            for(int i=0;i<mesh.Mesh.Primitives.Count;i++)if(mesh.Mesh.Primitives[i].ShaderPath==StaticSwitchTexturePreview.ShaderPath)
            {
                var main=mesh.Materials[StaticSwitchTexturePreview.ShaderPath].References.Textures.Single(t=>t.Tag==0x4D41494E);
                if(inspection is null){inspection=new(){ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,AlbedoTexture=geometry.GetOwnedTexture(mesh.Textures[main.AssetPath!])};owned.Add(inspection);}
                node.SetSurfaceOverrideMaterial(i,inspection);count++;
            }
        }
        if(count!=2)throw new InvalidDataException("Two pinned screens required.");Root.SetMeta("inspection_policy","Pinned terminal frame0; two SWTS surfaces use unshaded MAIN/UV0; original effect/timing unknown");
    }
    private static void MainThread(){if(OS.GetThreadCallerId()!=OS.GetMainThreadId())throw new InvalidOperationException("Static visual requires Godot main thread.");}
    private void OwnerThread(){if(System.Environment.CurrentManagedThreadId!=ownerThread)throw new InvalidOperationException("Serialized static visual owner thread required.");MainThread();}
    private bool Current(){OwnerThread();if(disposed)return false;if(currentLifetime!())return true;Dispose();return false;}
    public bool Tick()=>Current();
    public bool SetVisible(bool value){if(!Current())return false;Root.Visible=value;return true;}
    public bool ApplyPlacement(SceneCreateObjectByCrcMessage create)
    {if(!Current())return false;ArgumentNullException.ThrowIfNull(create);if(!SameCreate(source!.Create,create))return false;var transform=GodotAppearanceTransform.ToGodot(SceneCreatePlacementBuilder.Create(create,SceneCreateZeroQuaternionPolicy.Reject).Transform);SceneCreatureVisual.ValidateEngineTransform(transform);if(!Current())return false;Root.Transform=transform;return true;}
    public bool SetEngineTransform(Transform3D value)
    {if(!Current())return false;SceneCreatureVisual.ValidateEngineTransform(value);if(!Current())return false;Root.Transform=value;return true;}
    public bool SelectDetail(int ordinal)
    {if(!Current())return false;int count=source!.Appearance.Geometry.Root.Detail?.Levels.Count??1;if(ordinal<0 || ordinal>=count)throw new ArgumentOutOfRangeException(nameof(ordinal));SelectDetailUnchecked(ordinal);return true;}
    private void SelectDetailUnchecked(int ordinal)
    {var detail=source!.Appearance.Geometry.Root.Detail;if(detail is not null){var level=detail.Levels[ordinal];geometry!.Select(level.LowerDistance+(level.UpperDistance-level.LowerDistance)*.5f);}SelectedDetail=ordinal;}
    public string? Fingerprint(bool gpu=false)
    {
        if(!Current())return null;using var hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);hash.AppendData(BitConverter.GetBytes(Root.GetInstanceId()));
        foreach(var node in Meshes(geometry!.Root))
        {
            hash.AppendData(BitConverter.GetBytes(node.Mesh.GetInstanceId()));
            for(int surface=0;surface<node.Mesh.GetSurfaceCount();surface++)
            {
                var material=node.GetSurfaceOverrideMaterial(surface)??node.Mesh.SurfaceGetMaterial(surface);hash.AppendData(BitConverter.GetBytes(material.GetInstanceId()));
                ImageTexture texture;if(material is ShaderMaterial shader){hash.AppendData(BitConverter.GetBytes(shader.Shader.GetInstanceId()));texture=(ImageTexture)shader.GetShaderParameter("albedo_texture").AsGodotObject();}else texture=(ImageTexture)((StandardMaterial3D)material).AlbedoTexture;
                hash.AppendData(BitConverter.GetBytes(texture.GetInstanceId()));if(gpu){using var image=texture.GetImage();hash.AppendData(image.GetData());}
            }
        }
        return Convert.ToHexString(hash.GetHashAndReset());
    }
    public bool Verify(bool gpu)
    {
        if(!Current())return false;
        foreach(var node in Meshes(geometry!.Root))
        {
            var mesh=source!.Appearance.Meshes[node.GetMeta("original_mesh_path").AsString()];StaticMeshPreview.VerifyUploadedGeometry((ArrayMesh)node.Mesh,mesh.Mesh);
            if(node.Mesh.GetSurfaceCount()!=mesh.Mesh.Primitives.Count)throw new InvalidDataException("Static surface count changed.");
            for(int i=0;i<mesh.Mesh.Primitives.Count;i++)
            {
                var primitive=mesh.Mesh.Primitives[i];var main=mesh.Materials[primitive.ShaderPath].References.Textures.Single(t=>t.Tag==0x4D41494E);var original=mesh.Textures[main.AssetPath!];var material=node.GetSurfaceOverrideMaterial(i)??node.Mesh.SurfaceGetMaterial(i);
                ImageTexture texture=material is ShaderMaterial shader?(ImageTexture)shader.GetShaderParameter("albedo_texture").AsGodotObject():(ImageTexture)((StandardMaterial3D)material).AlbedoTexture;
                if(gpu){using var image=texture.GetImage();if(image.GetWidth()!=original.Width || image.GetHeight()!=original.Height || image.GetFormat()!=Image.Format.Rgba8 || image.GetMipmapCount()!=original.Mips.Count-1 || !image.GetData().AsSpan().SequenceEqual(original.Mips.SelectMany(m=>m.Rgba.ToArray()).ToArray()))throw new InvalidDataException("Original static GPU mip bytes changed.");}
            }
        }
        return true;
    }
    public void Dispose()
    {OwnerThread();if(disposed)return;disposed=true;IsPublished=false;geometry?.Dispose();geometry=null;foreach(var r in owned.AsEnumerable().Reverse())r.Dispose();owned.Clear();currentLifetime=null;source=null;ulong id=Root.GetInstanceId();Root.Free();Disposals++;if(GodotObject.InstanceFromId(id)is not null)throw new InvalidOperationException("Static root survived disposal.");}
}
