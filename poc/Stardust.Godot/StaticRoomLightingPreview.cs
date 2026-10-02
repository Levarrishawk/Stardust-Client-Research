using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Stardust.Core.Assets;

internal sealed class StaticRoomLightingPreview : IDisposable
{
    private readonly List<Resource> resources=new();
    private readonly List<(int Cell,ShaderMaterial Material)> materials=new();
    private readonly List<(MeshInstance3D Node,int Surface,Material Original,ShaderMaterial Override)> surfaces=new();
    private readonly IReadOnlyDictionary<int,IReadOnlyList<PortalCellLight>> lights;
    private AppearanceTransform? ownerPose;
    internal int SurfaceCount=>surfaces.Count;
    internal int MaterialCount=>materials.Count;
    internal int OwnerPublications{get;private set;}
    internal static async Task VerifyGpuBoundary(Node owner)
    {
        var viewport=new SubViewport{Size=new(64,64),OwnWorld3D=true,RenderTargetUpdateMode=SubViewport.UpdateMode.Always};owner.AddChild(viewport);
        using var mesh=new ArrayMesh();var n=System.Numerics.Vector3.UnitZ;MeshVertex[] vertices={new(new(-1,-1,0),n,new(.5f)),new(new(-1,1,0),n,new(.5f)),new(new(1,1,0),n,new(.5f)),new(new(1,-1,0),n,new(.5f))};
        using(var arrays=StaticMeshPreview.CreateArrays(new("synthetic room pixel","0001",vertices,new ushort[]{0,2,1,0,3,2})))mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles,arrays);
        int cases=0;float maximum=0;
        try
        {
            viewport.AddChild(new MeshInstance3D{Mesh=mesh});var camera=new Camera3D{Projection=Camera3D.ProjectionType.Orthogonal,Size=2,Position=new(0,0,-3),Current=true};viewport.AddChild(camera);camera.LookAt(Vector3.Zero);
            foreach(var kind in Enum.GetValues<StaticRoomPixelKind>())
            {
                using var shader=new Shader{Code=Code(DdsColorSpace.Unspecified,kind)};using var material=new ShaderMaterial{Shader=shader};mesh.SurfaceSetMaterial(0,material);
                material.SetShaderParameter("fixed_room_lighting",true);material.SetShaderParameter("fixed_diffuse",new Vector3(.25f,.5f,1.5f));material.SetShaderParameter("fixed_specular",new Vector3(1.5f,-.2f,.5f));material.SetShaderParameter("emissive_strength",.7f);material.SetShaderParameter("fade_opacity",.6f);
                foreach(byte mask in new byte[]{64,192})foreach(byte secondMask in new byte[]{32,192})
                {
                    using var mainImage=Image.CreateFromData(1,1,false,Image.Format.Rgba8,new byte[]{32,64,96,mask});using var mainTexture=ImageTexture.CreateFromImage(mainImage);
                    using var secondImage=Image.CreateFromData(1,1,false,Image.Format.Rgba8,new byte[]{0,0,0,secondMask});using var secondTexture=ImageTexture.CreateFromImage(secondImage);material.SetShaderParameter("albedo_texture",mainTexture);material.SetShaderParameter("secondary_texture",secondTexture);
                    foreach(var test in new[]{(false,false,false),(true,false,false),(true,false,true),(true,true,true)})
                    {
                        material.SetShaderParameter("bloom_enabled",test.Item1);material.SetShaderParameter("fade_enabled",test.Item2);material.SetShaderParameter("visualize_alpha",test.Item3);
                        var expected=StaticRoomPixelEvaluator.Evaluate(kind,new(32/255f,64/255f,96/255f,mask/255f),secondMask/255f,LegacyPixelColorInput.Saturate(new(.25f,.5f,1.5f)),LegacyPixelColorInput.Saturate(new(1.5f,-.2f,.5f)),.7f,test.Item1,test.Item2,.6f);
                        var rgb=test.Item3?new System.Numerics.Vector3(expected.W):new(expected.X,expected.Y,expected.Z);
                        if(ProjectSettings.GetSetting("rendering/renderer/rendering_method").AsString()=="gl_compatibility")
                        {float Display(float v){float linear=v*(v*(v*.305306011f+.682171111f)+.012522878f);return Math.Max(0,1.055f*MathF.Pow(linear,.416666667f)-.055f);}rgb=new(Display(rgb.X),Display(rgb.Y),Display(rgb.Z));}
                        await owner.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);await owner.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                        using var image=viewport.GetTexture().GetImage();var actual=image.GetPixel(32,32);float error=Math.Max(Math.Abs(actual.R-rgb.X),Math.Max(Math.Abs(actual.G-rgb.Y),Math.Abs(actual.B-rgb.Z)));maximum=Math.Max(maximum,error);cases++;
                        if(error>2/255f)throw new AssetPreviewException($"Original room GPU pixel boundary differs kind={kind} case={cases} error={error}.");
                    }
                }
            }
        }
        finally{viewport.Free();}
        GD.Print($"PASS: original room GPU pixel/mask/emissive/bloom/fade/legacy-COLOR fixtures={cases} maximum_error={maximum:0.######}");
    }
    internal static string Code(DdsColorSpace space,StaticRoomPixelKind kind)=>SkeletalAlbedoPreview.Header+
        "uniform sampler2D secondary_texture : filter_linear_mipmap, repeat_enable; uniform vec3 ambient_color=vec3(0.0);uniform vec3 material_specular=vec3(0.5);uniform float material_power=20.0;uniform vec3 material_emissive=vec3(0.0);"+
        "uniform vec3 light_direction_world;uniform vec3 directional_color;uniform vec3 directional_specular;uniform float emissive_strength=0.0;uniform bool bloom_enabled=false;uniform bool fade_enabled=false;uniform float fade_opacity=1.0;uniform bool visualize_alpha=false;uniform bool fixed_room_lighting=false;uniform vec3 fixed_diffuse;uniform vec3 fixed_specular;varying vec3 room_diffuse;varying vec3 room_specular;"+
        CellLightingGpuPreview.Uniforms+CellLightingGpuPreview.Functions+
        "void vertex(){room_diffuse=COLOR.rgb;room_specular=vec3(0.0);"+(kind==StaticRoomPixelKind.EmissivePs20?"":"vec3 d;vec3 s;cell_lighting((MODEL_MATRIX*vec4(VERTEX,1.0)).xyz,normalize(MODEL_NORMAL_MATRIX*NORMAL),INV_VIEW_MATRIX[3].xyz,d,s);room_diffuse+=d+material_emissive;room_specular=s;")+"if(fixed_room_lighting){room_diffuse=fixed_diffuse;room_specular=fixed_specular;}}"+
        "void fragment(){vec4 main_sample=texture(albedo_texture,UV);vec3 d=clamp(room_diffuse,vec3(0.0),vec3(1.0));vec3 s=clamp(room_specular,vec3(0.0),vec3(1.0));vec3 rgb;float alpha;"+
        (kind switch{
            StaticRoomPixelKind.SpecularPs11=>"rgb=main_sample.rgb*d+s*texture(secondary_texture,UV).a;alpha=fade_opacity;",
            StaticRoomPixelKind.EmissivePs20=>"rgb=main_sample.rgb*(d*(1.0-main_sample.a)+vec3(main_sample.a));alpha=fade_enabled?fade_opacity:emissive_strength*main_sample.a;",
            StaticRoomPixelKind.SpecularEmissivePs20=>"float e=texture(secondary_texture,UV).a;vec3 raw=s*main_sample.a;rgb=main_sample.rgb*clamp(d+vec3(e),vec3(0.0),vec3(1.0))+raw*(bloom_enabled?0.5:1.0);alpha=fade_enabled?fade_opacity:dot(raw,vec3(0.3,0.59,0.11))*(bloom_enabled?0.65:1.0)+emissive_strength*e;",
            _=>throw new AssetPreviewException("Unsupported room GPU family.")})+
        "if(visualize_alpha)rgb=vec3(alpha);ALBEDO="+SkeletalAlbedoPreview.DisplayExpression(space)+";}";
    internal StaticRoomLightingPreview(InteriorSceneContentBundle content,IReadOnlyDictionary<int,Node3D> roots,GeometryAppearancePreview geometry)
    {
        lights=content.CellLights!;var bundle=content.RoomLighting!;var shaders=new Dictionary<(DdsColorSpace,StaticRoomPixelKind),Shader>();var retained=new Dictionary<(int,string),ShaderMaterial>();
        IEnumerable<MeshInstance3D> Meshes(Node node){foreach(Node child in node.GetChildren()){if(child is MeshInstance3D mesh)yield return mesh;foreach(var nested in Meshes(child))yield return nested;}}
        try
        {
            foreach(var room in roots.OrderBy(p=>p.Key))foreach(var node in Meshes(room.Value))
            {
                if(!node.HasMeta("original_mesh_path"))continue;string path=node.GetMeta("original_mesh_path").AsString();var mesh=content.Materials!.Meshes[path];
                var visible=mesh.Mesh.Primitives.Where(p=>mesh.Effects[mesh.Materials[p.ShaderPath].References.EffectPath].HasRenderPasses).ToArray();
                if(visible.Length!=node.Mesh.GetSurfaceCount())throw new AssetPreviewException("Room surface/source mapping differs.");
                for(int surface=0;surface<visible.Length;surface++)
                {
                    string materialPath=visible[surface].ShaderPath;if(!bundle.Materials.TryGetValue(materialPath,out var description))continue;
                    if(visible[surface].Vertices.Any(v=>!v.PackedColor0.HasValue))throw new AssetPreviewException("Selected baked room material lacks COLOR0.");
                    if(!retained.TryGetValue((room.Key,materialPath),out var material))
                    {
                        var main=mesh.Textures[description.MainTexture];var key=(main.ColorSpace,description.Kind);
                        if(!shaders.TryGetValue(key,out var shader)){shader=new(){Code=Code(key.ColorSpace,key.Kind)};resources.Add(shader);shaders.Add(key,shader);}
                        material=new(){Shader=shader};resources.Add(material);material.SetShaderParameter("albedo_texture",geometry.GetOwnedTexture(main));
                        if(description.SecondaryTexture is not null)material.SetShaderParameter("secondary_texture",geometry.GetOwnedTexture(mesh.Textures[description.SecondaryTexture]));
                        var spec=description.Material.SpecularRgba;var emissive=description.Material.EmissiveRgba;
                        material.SetShaderParameter("material_specular",new Vector3(spec.X,spec.Y,spec.Z));material.SetShaderParameter("material_power",description.Material.SpecularPower);material.SetShaderParameter("material_emissive",new Vector3(emissive.X,emissive.Y,emissive.Z));
                        // Opaque preview: an unselected original factor gets an explicit alpha-only zero policy.
                        material.SetShaderParameter("emissive_strength",description.TextureFactor?.X??0);retained.Add((room.Key,materialPath),material);materials.Add((room.Key,material));
                    }
                    surfaces.Add((node,surface,node.Mesh.SurfaceGetMaterial(surface),material));node.SetSurfaceOverrideMaterial(surface,material);
                }
            }
            if(surfaces.Count==0)throw new AssetPreviewException("No supported room-lighting surfaces.");Verify();
        }
        catch{Dispose();throw;}
    }
    internal void SetOwner(AppearanceTransform owner)
    {
        if(ownerPose==owner)return;
        var rigs=lights.ToDictionary(p=>p.Key,p=>CellLightingPreview.FromLegacyCell(p.Value,owner,System.Numerics.Vector3.Zero,true));
        string before=Fingerprint();foreach(var entry in materials)CellLightingGpuPreview.Apply(entry.Material,rigs[entry.Cell],true);Verify();
        if(before!=Fingerprint())throw new AssetPreviewException("Room owner lighting replaced retained resources.");ownerPose=owner;OwnerPublications++;
    }
    internal string Fingerprint()=>string.Join(";",surfaces.Select(s=>$"{s.Node.GetInstanceId()}:{s.Node.Mesh.GetInstanceId()}:{s.Surface}:{s.Original.GetInstanceId()}:{s.Override.GetInstanceId()}"));
    internal void Verify(){foreach(var s in surfaces)if(s.Node.Mesh.SurfaceGetMaterial(s.Surface)!=s.Original||s.Node.GetSurfaceOverrideMaterial(s.Surface)!=s.Override)throw new AssetPreviewException("Room instance override changed shared template material.");}
    public void Dispose(){foreach(var s in surfaces)if(GodotObject.IsInstanceValid(s.Node))s.Node.SetSurfaceOverrideMaterial(s.Surface,null!);surfaces.Clear();materials.Clear();foreach(var r in resources.AsEnumerable().Reverse())r.Dispose();resources.Clear();}
}
