using System;
using System.Threading.Tasks;
using Godot;
using Stardust.Core.Assets;

internal static class SkeletalSpecularPreview
{
    internal static string Code(DdsColorSpace space,bool mainMask,bool vertexVaryings=false)=>vertexVaryings?TangentSpecularPreview.Code(space,mainMask,true):SkeletalAlbedoPreview.Header+
        "uniform sampler2D normal_texture : filter_linear_mipmap, repeat_enable; "+
        (mainMask?"":"uniform sampler2D specular_texture : filter_linear_mipmap, repeat_enable; ")+
        "uniform vec3 light_direction_world=vec3(0.0,0.0,1.0); uniform vec3 ambient_color=vec3(0.35); uniform vec3 directional_color=vec3(0.75); "+
        "uniform vec3 directional_specular=vec3(0.75); uniform vec3 vertex_specular=vec3(0.0); uniform vec3 material_specular=vec3(0.2); uniform float material_power=20.0; "+
        "uniform bool bloom_enabled=false; uniform bool fade_enabled=false; uniform float fade_opacity=1.0; uniform bool visualize_alpha=false; "+
        "uniform bool fixed_half_angle=false; uniform vec3 half_angle_world=vec3(0.0,0.0,1.0); "+
        CellLightingGpuPreview.Uniforms+CellLightingGpuPreview.Functions+CellLightingGpuPreview.Vertex+
        "void fragment() { vec4 sample_value=texture(albedo_texture,UV); vec3 hue=alpha_hue_mask ? mix(first_factor,second_factor,sample_value.a) : first_factor; "+
        "vec4 ns=texture(normal_texture,UV); vec2 xy=(ns.ag-vec2(0.5))*2.0; vec3 nt=normalize(vec3(xy,sqrt(abs(1.0-dot(xy,xy))))); "+
        "vec3 view_normal=normalize(TANGENT*nt.x+BINORMAL*nt.y+NORMAL*nt.z); vec3 world_normal=normalize(mat3(INV_VIEW_MATRIX)*view_normal); "+
        "vec3 position_world=(INV_VIEW_MATRIX*vec4(VERTEX,1.0)).xyz; vec3 half_sum=fixed_half_angle ? half_angle_world : light_direction_world+normalize(INV_VIEW_MATRIX[3].xyz-position_world); vec3 half_direction=half_sum/max(length(half_sum),0.000001); "+
        "float d=clamp(dot(world_normal,light_direction_world),0.0,1.0); vec3 diffuse_light=clamp(clamp(cell_lighting_enabled?cell_vertex_diffuse:ambient_color,vec3(0.0),vec3(1.0))+directional_color*d,vec3(0.0),vec3(1.0)); "+
        "float s=pow(clamp(dot(world_normal,half_direction),0.0,1.0),material_power); float mask="+(mainMask?"sample_value.a":"texture(specular_texture,UV).a")+"; "+
        "vec3 specular_raw=(s*directional_specular*material_specular+clamp(cell_lighting_enabled?cell_vertex_specular:vertex_specular,vec3(0.0),vec3(1.0)))*mask; vec3 rgb=sample_value.rgb*hue*diffuse_light+specular_raw*(bloom_enabled?0.5:1.0); "+
        "float result_alpha=fade_enabled?fade_opacity:dot(specular_raw,vec3(0.3,0.59,0.11))*(bloom_enabled?0.65:1.0); if(visualize_alpha)rgb=vec3(result_alpha); ALBEDO="+SkeletalAlbedoPreview.DisplayExpression(space)+"; }";
    internal static void Apply(ShaderMaterial target,SkeletalSpecularMaterial description)
    {
        var color=description.Material.SpecularRgba;
        target.SetShaderParameter("material_specular",new Vector3(color.X,color.Y,color.Z));target.SetShaderParameter("material_power",description.Material.SpecularPower);
    }
    internal static void Verify(ShaderMaterial target,SkeletalSpecularMaterial description)
    {
        var color=description.Material.SpecularRgba;
        if(target.GetShaderParameter("material_specular").AsVector3()!=new Vector3(color.X,color.Y,color.Z)||target.GetShaderParameter("material_power").AsSingle()!=description.Material.SpecularPower)
            throw new AssetPreviewException("Specular GPU material constants differ.");
    }
    internal static async Task VerifyGpuBoundary(Node owner,bool vertexLit=false,bool cellLighting=false,bool legacyHdr=false,bool staticDoor=false,bool vertexVaryings=false)
    {
        byte[] diffuseBytes={32,64,128,0,224,128,64,255,64,224,32,64,128,32,224,192};byte[] maskBytes={0,0,0,0,0,0,0,32,0,0,0,64,0,0,0,96};
        using var diffuseImage=Image.CreateFromData(2,2,false,Image.Format.Rgba8,diffuseBytes);using var diffuse=ImageTexture.CreateFromImage(diffuseImage);
        using var normalImage=Image.CreateFromData(1,1,false,Image.Format.Rgba8,new byte[]{0,192,0,128});using var normal=ImageTexture.CreateFromImage(normalImage);
        using var maskImage=Image.CreateFromData(2,2,false,Image.Format.Rgba8,maskBytes);using var mask=ImageTexture.CreateFromImage(maskImage);
        using var mesh=new ArrayMesh();var n=System.Numerics.Vector3.UnitZ;
        MeshVertex[] vertices={new(new(-1,-1,0),n,new(.5f,.5f)),new(new(-1,1,0),n,new(.5f,.5f)),new(new(1,1,0),n,new(.5f,.5f)),new(new(1,-1,0),n,new(.5f,.5f))};
        using(var arrays=StaticMeshPreview.CreateArrays(new("synthetic specular","0001",vertices,new ushort[]{0,2,1,0,3,2})))
        {arrays[(int)Mesh.ArrayType.Tangent]=new float[]{1,0,0,-1,1,0,0,-1,1,0,0,-1,1,0,0,-1};mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles,arrays);}
        var viewport=new SubViewport{Size=new(64,64),OwnWorld3D=true,RenderTargetUpdateMode=SubViewport.UpdateMode.Always};owner.AddChild(viewport);float maximum=0;int cases=0;
        try
        {
            var instance=new MeshInstance3D{Mesh=mesh};viewport.AddChild(instance);var camera=new Camera3D{Projection=Camera3D.ProjectionType.Orthogonal,Size=2,Position=new(0,0,-3),Current=true};viewport.AddChild(camera);camera.LookAt(Vector3.Zero);
            foreach(bool mainMask in staticDoor?new[]{true}:vertexLit?new[]{false}:new[]{false,true})
            {
                using var shader=new Shader{Code=staticDoor?StaticDoorLightingPreview.Code(DdsColorSpace.Unspecified):vertexLit?SkeletalVertexLitSpecularPreview.Code(DdsColorSpace.Unspecified,false):Code(DdsColorSpace.Unspecified,mainMask,vertexVaryings)};using var material=new ShaderMaterial{Shader=shader};mesh.SurfaceSetMaterial(0,material);
                material.SetShaderParameter("albedo_texture",diffuse);material.SetShaderParameter("normal_texture",normal);if(!mainMask)material.SetShaderParameter("specular_texture",mask);
                var factors=new SkeletalAlbedoFactors("synthetic",!mainMask,0,0,staticDoor?uint.MaxValue:0xFF4080E0,staticDoor?uint.MaxValue:0xFFE06020);SkeletalAlbedoPreview.Apply(material,factors);
                var fields=new StaticShaderMaterial(System.Numerics.Vector4.One,System.Numerics.Vector4.One,System.Numerics.Vector4.Zero,new(.2f,.3f,.4f,1),20);
                Apply(material,new("synthetic",fields,0,mainMask));Verify(material,new("synthetic",fields,0,mainMask));
                material.SetShaderParameter("fixed_half_angle",true);material.SetShaderParameter("half_angle_world",new Vector3(0,.2f,-1));material.SetShaderParameter("light_direction_world",Vector3.Up);
                material.SetShaderParameter("ambient_color",new Vector3(.1f,.2f,.05f));material.SetShaderParameter("directional_color",new Vector3(.4f,.3f,.2f));material.SetShaderParameter("directional_specular",new Vector3(.6f,.4f,.2f));material.SetShaderParameter("vertex_specular",new Vector3(.02f,.03f,.01f));
                if(cellLighting)
                {
                    var first=new PortalCellLight(PortalCellLightKind.Directional,new(.4f,.3f,.2f,1),new(.6f,.4f,.2f,1),AppearanceTransform.Identity,AppearanceTransform.Identity with{RowX=System.Numerics.Vector3.UnitX,RowY=-System.Numerics.Vector3.UnitZ,RowZ=-System.Numerics.Vector3.UnitY},new(1,0,0));
                    var second=first with{DiffuseRgba=new(.05f,.04f,.03f,1),SpecularRgba=System.Numerics.Vector4.Zero,RuntimeTransform=AppearanceTransform.Identity with{RowZ=-System.Numerics.Vector3.UnitZ}};
                    CellLightingGpuPreview.Apply(material,new CellLightingPreview(new[]{first,second},AppearanceTransform.Identity,new(.1f,.2f,.05f)),false);
                    material.SetShaderParameter("fixed_half_angle",true);material.SetShaderParameter("half_angle_world",new Vector3(0,.2f,-1));
                }
                if(legacyHdr){material.SetShaderParameter("ambient_color",new Vector3(-.2f,.5f,1.5f));material.SetShaderParameter("vertex_specular",new Vector3(1.5f,-.2f,.5f));}
                var sample=new System.Numerics.Vector4(112/255f,112/255f,112/255f,127.75f/255);var normalSample=new System.Numerics.Vector4(0,192/255f,0,128/255f);
                foreach(var test in legacyHdr?
                    new[]{(false,false,false,false,false),(true,false,false,false,false),(true,false,true,false,false),(true,true,true,false,false)}:vertexLit?
                    new[]{(false,false,false,false,false),(true,false,false,false,false),(false,false,true,false,false),(true,false,true,false,false),(true,true,true,false,false),(false,false,false,true,false),(false,false,false,false,true),(false,false,false,true,true)}:
                    new[]{(false,false,false,false,false),(true,false,false,false,false),(false,false,true,false,false),(true,false,true,false,false),(true,true,true,false,false),(false,false,false,true,false)})
                {
                    material.SetShaderParameter("light_direction_world",test.Item5?(test.Item4?Vector3.Back:Vector3.Forward):(test.Item4?-Vector3.Up:Vector3.Up));
                    if(vertexLit)material.SetShaderParameter("fixed_vertex_lighting",!test.Item5);
                    material.SetShaderParameter("bloom_enabled",test.Item1);material.SetShaderParameter("fade_enabled",test.Item2);material.SetShaderParameter("fade_opacity",.6f);material.SetShaderParameter("visualize_alpha",test.Item3);
                    await owner.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);await owner.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                    var lighting=new SkeletalPixelLighting(test.Item4?-System.Numerics.Vector3.UnitY:System.Numerics.Vector3.UnitY,new(0,.2f,1),new(.4f,.3f,.2f),new(.6f,.4f,.2f),new(.1f,.2f,.05f),new(.02f,.03f,.01f),test.Item1,test.Item2,.6f);
                    if(legacyHdr)lighting=lighting with{VertexDiffuse=LegacyPixelColorInput.Saturate(new(-.2f,.5f,1.5f)),VertexSpecular=LegacyPixelColorInput.Saturate(new(1.5f,-.2f,.5f))};
                    if(cellLighting)lighting=lighting with{VertexDiffuse=new(.15f,.24f,.08f),VertexSpecular=System.Numerics.Vector3.Zero};
                    var expected=SkeletalSpecularEvaluator.Evaluate(sample,normalSample,mainMask?sample.W:48f/255,factors,fields,lighting);if(vertexLit)
                    {
                        var vd=new System.Numerics.Vector3(.1f,.2f,.05f);var vs=new System.Numerics.Vector3(.02f,.03f,.01f);
                        if(legacyHdr){vd=LegacyPixelColorInput.Saturate(new(-.2f,.5f,1.5f));vs=LegacyPixelColorInput.Saturate(new(1.5f,-.2f,.5f));}
                        if(test.Item5){if(!test.Item4){vd+=new System.Numerics.Vector3(.4f,.3f,.2f);vs=new System.Numerics.Vector3(.6f,.4f,.2f)*new System.Numerics.Vector3(.2f,.3f,.4f)*MathF.Pow(1/MathF.Sqrt(1.04f),20);}else vs=System.Numerics.Vector3.Zero;}
                        var metadata=IffDocument.Parse(new byte[]{70,79,82,77,0,0,0,4,84,69,83,84}).Root;
                        expected=SkeletalVertexLitSpecularEvaluator.Evaluate(sample,48f/255,factors,new(true,false,"synthetic",metadata),vd,vs,test.Item1,test.Item2,.6f);
                    }
                    var rgb=test.Item3?new System.Numerics.Vector3(expected.W):new(expected.X,expected.Y,expected.Z);
                    if(ProjectSettings.GetSetting("rendering/renderer/rendering_method").AsString()=="gl_compatibility")
                    {float Display(float value){float linear=value*(value*(value*.305306011f+.682171111f)+.012522878f);return Math.Max(0,1.055f*MathF.Pow(linear,.416666667f)-.055f);}rgb=new(Display(rgb.X),Display(rgb.Y),Display(rgb.Z));}
                    using var image=viewport.GetTexture().GetImage();var actual=image.GetPixel(32,32);float error=Math.Max(Math.Abs(actual.R-rgb.X),Math.Max(Math.Abs(actual.G-rgb.Y),Math.Abs(actual.B-rgb.Z)));maximum=Math.Max(maximum,error);cases++;
                    if(error>2/255f)throw new AssetPreviewException($"GPU specular boundary differs case={cases} error={error} actual={actual} expected={rgb}.");
                }
            }
        }
        finally{viewport.Free();}
        if(vertexLit&&!cellLighting&&!legacyHdr&&!staticDoor)await SkeletalMouthLightingGpuVerification.VerifyAsync(owner);
        GD.Print($"PASS: sampled {(vertexLit?"vertex-lit":"normal-mapped")} specular GPU mask/hue/bloom/fade/alpha-display cell_lighting={cellLighting} legacy_hdr={legacyHdr} fixtures={cases} maximum_error={maximum:0.######}");
    }
}
