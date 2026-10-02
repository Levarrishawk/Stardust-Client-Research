using System;
using System.Threading.Tasks;
using Godot;
using Stardust.Core.Assets;

/// <summary>Unlit sampled albedo factors, with explicit display conversion after encoded RGB/mask interpolation.</summary>
internal static class SkeletalAlbedoPreview
{
    internal const string Header=
        "shader_type spatial; render_mode unshaded, cull_back; "+
        "uniform sampler2D albedo_texture : filter_linear_mipmap, repeat_enable; "+
        "uniform vec3 first_factor=vec3(1.0); uniform vec3 second_factor=vec3(1.0); uniform bool alpha_hue_mask=false; "+
        "vec3 display_linear(vec3 value) { return mix(value/12.92, pow((value+0.055)/1.055,vec3(2.4)),step(vec3(0.04045),value)); } "+
        "vec3 display_srgb(vec3 value) { return mix(value*12.92, 1.055*pow(value,vec3(1.0/2.4))-0.055,step(vec3(0.0031308),value)); } ";
    internal static string DisplayExpression(DdsColorSpace colorSpace)=>colorSpace==DdsColorSpace.Linear?"(OUTPUT_IS_SRGB ? display_srgb(rgb) : rgb)":"(OUTPUT_IS_SRGB ? rgb : display_linear(rgb))";
    internal static string Code(DdsColorSpace colorSpace)=>Header+
        "void fragment() { vec4 sample_value=texture(albedo_texture,UV); "+
        "vec3 hue=alpha_hue_mask ? mix(first_factor,second_factor,sample_value.a) : first_factor; vec3 rgb=sample_value.rgb*hue; "+
        "ALBEDO="+DisplayExpression(colorSpace)+"; }";
    private static Vector3 Color(uint argb)=>new(((argb>>16)&255)/255f,((argb>>8)&255)/255f,(argb&255)/255f);
    internal static void Apply(ShaderMaterial material,SkeletalAlbedoFactors factors)
    {
        material.SetShaderParameter("first_factor",Color(factors.FirstArgb));material.SetShaderParameter("second_factor",Color(factors.SecondArgb));
        material.SetShaderParameter("alpha_hue_mask",factors.UsesAlphaHueMask);
    }
    internal static void Verify(ShaderMaterial material,SkeletalAlbedoFactors factors)
    {
        if(material.GetShaderParameter("first_factor").AsVector3()!=Color(factors.FirstArgb)||material.GetShaderParameter("second_factor").AsVector3()!=Color(factors.SecondArgb)||material.GetShaderParameter("alpha_hue_mask").AsBool()!=factors.UsesAlphaHueMask)throw new AssetPreviewException("Skeletal albedo factor GPU parameters differ.");
    }
    internal static async Task VerifyGpuBoundary(Node owner)
    {
        using var shader=new Shader{Code=Code(DdsColorSpace.Unspecified)};
        using var image=Image.CreateFromData(2,2,false,Image.Format.Rgba8,new byte[]{32,64,128,0,224,128,64,255,64,224,32,64,128,32,224,192});
        using var texture=ImageTexture.CreateFromImage(image);using var material=new ShaderMaterial{Shader=shader};material.SetShaderParameter("albedo_texture",texture);
        using var mesh=new ArrayMesh();System.Numerics.Vector3 normal=new(0,0,1);
        MeshVertex[] vertices={new(new(-1,-1,0),normal,new(.5f,.5f)),new(new(-1,1,0),normal,new(.5f,.5f)),new(new(1,1,0),normal,new(.5f,.5f)),new(new(1,-1,0),normal,new(.5f,.5f))};
        using var arrays=StaticMeshPreview.CreateArrays(new("synthetic albedo probe","0001",vertices,new ushort[]{0,2,1,0,3,2}));mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles,arrays);mesh.SurfaceSetMaterial(0,material);
        var viewport=new SubViewport{Size=new(64,64),OwnWorld3D=true,RenderTargetUpdateMode=SubViewport.UpdateMode.Always};owner.AddChild(viewport);float maximum=0;
        try
        {
            viewport.AddChild(new MeshInstance3D{Mesh=mesh});var camera=new Camera3D{Projection=Camera3D.ProjectionType.Orthogonal,Size=2,Position=new(0,0,-3),Current=true};viewport.AddChild(camera);camera.LookAt(Vector3.Zero);
            foreach(var factors in new[]{new SkeletalAlbedoFactors("synthetic",true,0x4D41494E,0x48554542,0x008020FF,0xFF40FF00),new("synthetic",false,0x4D41494E,0,0x008020FF,0xFF40FF00),new("synthetic",true,0x4D41494E,0x48554542,0xFFFFFFFF,0xFFFFFFFF)})
            {
                Apply(material,factors);Verify(material,factors);
                await owner.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);await owner.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                using var result=viewport.GetTexture().GetImage();Color actual=result.GetPixel(32,32);
                var expected=SkeletalAlbedoEvaluator.Evaluate(new(112/255f,112/255f,112/255f,127.75f/255),factors);
                if(ProjectSettings.GetSetting("rendering/renderer/rendering_method").AsString()=="gl_compatibility")
                {
                    // Compatibility's renderer applies a documented approximate transfer pair after ALBEDO.
                    // Reference that adapter boundary separately from the engine-neutral hue evaluation.
                    float Display(float value)
                    {
                        float linear=value*(value*(value*.305306011f+.682171111f)+.012522878f);
                        return Math.Max(0,1.055f*MathF.Pow(linear,.416666667f)-.055f);
                    }
                    expected=new(Display(expected.X),Display(expected.Y),Display(expected.Z));
                }
                float error=Math.Max(Math.Abs(actual.R-expected.X),Math.Max(Math.Abs(actual.G-expected.Y),Math.Abs(actual.B-expected.Z)));maximum=Math.Max(maximum,error);
                if(error>2/255f)throw new AssetPreviewException($"GPU sampled albedo factor/mask/display boundary differs: error={error} actual={actual} expected={expected}.");
            }
        }
        finally{viewport.Free();}
        GD.Print($"PASS: sampled albedo GPU factor/mask/linear-filter/display fixtures=3 maximum_error={maximum:0.######}");
    }
}
