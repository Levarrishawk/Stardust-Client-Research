using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stardust.Core.Assets;

internal sealed class StaticDoorLightingPreview : IDisposable
{
    private readonly List<Resource> resources=new();
    private readonly List<(int Cell,ShaderMaterial Material)> materials=new();
    private readonly List<(MeshInstance3D Node,int Surface,Material Original,ShaderMaterial Override)> surfaces=new();
    private readonly IReadOnlyDictionary<int,IReadOnlyList<PortalCellLight>> lights;
    private AppearanceTransform? ownerPose;
    private readonly bool legacySelection;
    internal int SurfaceCount=>surfaces.Count;
    internal int MaterialCount=>materials.Count;
    internal int FallbackVertices{get;private set;}
    internal int OwnerPublications{get;private set;}
    internal static async System.Threading.Tasks.Task VerifyGpuBoundary(Node owner)
    {
        await SkeletalSpecularPreview.VerifyGpuBoundary(owner,false,false,false,true);
        await SkeletalSpecularPreview.VerifyGpuBoundary(owner,false,true,false,true);
        await StaticDoorVaryingGpuScenarios.Verify(owner);
        GD.Print("PASS: static door no-tint MAIN-alpha compressed-normal/specular GPU fixtures=12 active_cell_inputs=true");
    }
    internal static string Code(DdsColorSpace space)=>TangentSpecularPreview.Code(space,true,false);
    internal StaticDoorLightingPreview(InteriorSceneContentBundle content,IReadOnlyList<Node3D> roots,GeometryAppearancePreview geometry,bool legacySelection=false)
    {
        this.legacySelection=legacySelection;lights=content.CellLights!;var bundle=content.StaticNormals!;var shaders=new Dictionary<DdsColorSpace,Shader>();var retained=new Dictionary<(int,string),ShaderMaterial>();var verifiedMeshes=new HashSet<ulong>();
        IEnumerable<MeshInstance3D> Meshes(Node node){foreach(Node child in node.GetChildren()){if(child is MeshInstance3D mesh)yield return mesh;foreach(var nested in Meshes(child))yield return nested;}}
        try
        {
            for(int door=0;door<roots.Count;door++)foreach(var node in Meshes(roots[door]))
            {
                if(!node.HasMeta("original_mesh_path"))continue;string path=node.GetMeta("original_mesh_path").AsString();var mesh=content.Materials!.Meshes[path];
                var visible=mesh.Mesh.Primitives.Where(p=>mesh.Effects[mesh.Materials[p.ShaderPath].References.EffectPath].HasRenderPasses).ToArray();
                if(visible.Length!=node.Mesh.GetSurfaceCount())throw new AssetPreviewException("Door surface/source mapping differs.");
                for(int surface=0;surface<visible.Length;surface++)
                {
                    string materialPath=visible[surface].ShaderPath;if(!bundle.Materials.TryGetValue(materialPath,out var description))continue;
                    if(verifiedMeshes.Add(node.Mesh.GetInstanceId()))
                    {
                        using var arrays=node.Mesh.SurfaceGetArrays(surface);var tangents=arrays[(int)Mesh.ArrayType.Tangent].AsFloat32Array();var normals=arrays[(int)Mesh.ArrayType.Normal].AsVector3Array();
                        if(tangents.Length!=visible[surface].Vertices.Count*4)throw new AssetPreviewException("Door tangent upload cardinality differs.");
                        for(int i=0;i<visible[surface].Vertices.Count;i++)
                        {
                            var expected=StaticNormalMaterialReader.PreviewTangent(visible[surface].Vertices[i],description.TangentCoordinateSet,out bool fallback);if(fallback)FallbackVertices++;
                            if(Math.Abs(tangents[i*4]-expected.X)>.003f||Math.Abs(tangents[i*4+1]-expected.Y)>.003f||Math.Abs(tangents[i*4+2]+expected.Z)>.003f||tangents[i*4+3]!=-expected.W)throw new AssetPreviewException("Door reflected/projected tangent readback differs.");
                            if(Math.Abs(normals[i].LengthSquared()-1)>.003f)throw new AssetPreviewException("Door unit-normal upload differs.");
                        }
                    }
                    if(!retained.TryGetValue((door,materialPath),out var material))
                    {
                        var main=mesh.Textures[description.MainTexture];if(!shaders.TryGetValue(main.ColorSpace,out var shader)){shader=new(){Code=Code(main.ColorSpace)};resources.Add(shader);shaders.Add(main.ColorSpace,shader);}
                        material=new(){Shader=shader};resources.Add(material);material.SetShaderParameter("albedo_texture",geometry.GetOwnedTexture(main));material.SetShaderParameter("normal_texture",geometry.GetOwnedTexture(mesh.Textures[description.NormalTexture]));
                        SkeletalAlbedoPreview.Apply(material,new(description.PixelProgram,false,0,0,uint.MaxValue,uint.MaxValue));SkeletalSpecularPreview.Apply(material,new(description.PixelProgram,description.Material,0x4D41494E,true));
                        var emissive=description.Material.EmissiveRgba;material.SetShaderParameter("material_emissive",new Vector3(emissive.X,emissive.Y,emissive.Z));
                        retained.Add((door,materialPath),material);materials.Add((content.Scene.Doorways[door].Cell,material));
                    }
                    surfaces.Add((node,surface,node.Mesh.SurfaceGetMaterial(surface),material));node.SetSurfaceOverrideMaterial(surface,material);
                }
            }
            if(surfaces.Count==0)throw new AssetPreviewException("No supported door normal surfaces.");Verify();
        }
        catch{Dispose();throw;}
    }
    internal void SetOwner(AppearanceTransform owner)
    {
        if(ownerPose==owner)return;var rigs=lights.ToDictionary(p=>p.Key,p=>legacySelection?CellLightingPreview.FromLegacyCell(p.Value,owner,new System.Numerics.Vector3(.1f)):new CellLightingPreview(p.Value,owner,new System.Numerics.Vector3(.1f)));
        string before=Fingerprint();foreach(var entry in materials)CellLightingGpuPreview.Apply(entry.Material,rigs[entry.Cell],false);Verify();
        if(before!=Fingerprint())throw new AssetPreviewException("Door lighting replaced retained resources.");ownerPose=owner;OwnerPublications++;
    }
    internal string Fingerprint()=>string.Join(";",surfaces.Select(s=>$"{s.Node.GetInstanceId()}:{s.Node.Mesh.GetInstanceId()}:{s.Surface}:{s.Original.GetInstanceId()}:{s.Override.GetInstanceId()}"));
    internal void Verify(){foreach(var s in surfaces)if(s.Node.Mesh.SurfaceGetMaterial(s.Surface)!=s.Original||s.Node.GetSurfaceOverrideMaterial(s.Surface)!=s.Override)throw new AssetPreviewException("Door override changed shared template material.");}
    public void Dispose(){foreach(var s in surfaces)if(GodotObject.IsInstanceValid(s.Node))s.Node.SetSurfaceOverrideMaterial(s.Surface,null!);surfaces.Clear();materials.Clear();foreach(var r in resources.AsEnumerable().Reverse())r.Dispose();resources.Clear();}
}
