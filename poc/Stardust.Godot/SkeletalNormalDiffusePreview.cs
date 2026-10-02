using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Stardust.Core.Assets;

internal static class SkeletalNormalDiffusePreview
{
    internal static string Code(DdsColorSpace space,bool normalMap)=>SkeletalAlbedoPreview.Header+
        "uniform vec3 light_direction_world=vec3(0.0,0.0,1.0); uniform vec3 ambient_color=vec3(0.35); uniform vec3 directional_color=vec3(0.75); "+
        (normalMap?"uniform sampler2D normal_texture : filter_linear_mipmap, repeat_enable; ":"")+
        "void fragment() { vec4 sample_value=texture(albedo_texture,UV); vec3 hue=alpha_hue_mask ? mix(first_factor,second_factor,sample_value.a) : first_factor; "+
        (normalMap?"vec4 ns=texture(normal_texture,UV); vec2 xy=(ns.ag-vec2(0.5))*2.0; vec3 nt=normalize(vec3(xy,sqrt(abs(1.0-dot(xy,xy))))); vec3 view_normal=normalize(TANGENT*nt.x+BINORMAL*nt.y+NORMAL*nt.z); ":"vec3 view_normal=normalize(NORMAL); ")+
        "vec3 world_normal=normalize(mat3(INV_VIEW_MATRIX)*view_normal); float intensity=clamp(dot(world_normal,light_direction_world),0.0,1.0); "+
        "vec3 rgb=sample_value.rgb*hue*clamp(ambient_color+directional_color*intensity,vec3(0.0),vec3(1.0)); ALBEDO="+SkeletalAlbedoPreview.DisplayExpression(space)+"; }";
    internal static void ApplyLighting(ShaderMaterial material)
    {
        material.SetShaderParameter("light_direction_world",new Vector3(.4f,.6f,-.7f).Normalized());material.SetShaderParameter("ambient_color",new Vector3(.35f,.35f,.35f));material.SetShaderParameter("directional_color",new Vector3(.75f,.75f,.75f));
    }
    internal static async Task VerifyGpuBoundary(Node owner)
    {
        using var shader=new Shader{Code=Code(DdsColorSpace.Unspecified,true)};
        using var image=Image.CreateFromData(1,1,false,Image.Format.Rgba8,new byte[]{255,255,255,255});using var texture=ImageTexture.CreateFromImage(image);
        using var normalImage=Image.CreateFromData(1,1,false,Image.Format.Rgba8,new byte[]{0,192,0,128});using var normalTexture=ImageTexture.CreateFromImage(normalImage);
        using var material=new ShaderMaterial{Shader=shader};material.SetShaderParameter("albedo_texture",texture);material.SetShaderParameter("normal_texture",normalTexture);
        SkeletalAlbedoPreview.Apply(material,new("synthetic",false,0x4D41494E,0,0xFFFFFFFF,0xFFFFFFFF));
        material.SetShaderParameter("light_direction_world",Vector3.Up);material.SetShaderParameter("ambient_color",new Vector3(.2f,.2f,.2f));material.SetShaderParameter("directional_color",new Vector3(.6f,.6f,.6f));
        using var mesh=new ArrayMesh();System.Numerics.Vector3 normal=new(0,0,1);
        MeshVertex[] vertices={new(new(-1,-1,0),normal,new(.5f,.5f)),new(new(-1,1,0),normal,new(.5f,.5f)),new(new(1,1,0),normal,new(.5f,.5f)),new(new(1,-1,0),normal,new(.5f,.5f))};
        var viewport=new SubViewport{Size=new(64,64),OwnWorld3D=true,RenderTargetUpdateMode=SubViewport.UpdateMode.Always};owner.AddChild(viewport);float maximum=0;
        try
        {
            var instance=new MeshInstance3D{Mesh=mesh};viewport.AddChild(instance);var camera=new Camera3D{Projection=Camera3D.ProjectionType.Orthogonal,Size=2,Position=new(0,0,-3),Current=true};viewport.AddChild(camera);camera.LookAt(Vector3.Zero);
            foreach(var test in new[]{(-1f,0f),(1f,0f),(-1f,MathF.PI/2)})
            {
                mesh.ClearSurfaces();using var arrays=StaticMeshPreview.CreateArrays(new("synthetic normal diffuse","0001",vertices,new ushort[]{0,2,1,0,3,2}));arrays[(int)Mesh.ArrayType.Tangent]=Enumerable.Range(0,4).SelectMany(_=>new[]{1f,0,0,test.Item1}).ToArray();mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles,arrays);mesh.SurfaceSetMaterial(0,material);instance.Rotation=new(0,0,test.Item2);
                await owner.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);await owner.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                using var result=viewport.GetTexture().GetImage();var actual=result.GetPixel(32,32);
                var sampled=SkeletalCompressedNormalEvaluator.Decode(new(0,192/255f,0,128/255f));var objectNormal=SkeletalCompressedNormalEvaluator.ToObject(sampled,new(0,0,-1),new(1,0,0,test.Item1));
                var world=System.Numerics.Vector3.Normalize(System.Numerics.Vector3.TransformNormal(objectNormal,System.Numerics.Matrix4x4.CreateRotationZ(test.Item2)));
                var expected=SkeletalDiffuseEvaluator.Evaluate(System.Numerics.Vector3.One,world,System.Numerics.Vector3.UnitY,new(.2f),new(.6f));
                if(ProjectSettings.GetSetting("rendering/renderer/rendering_method").AsString()=="gl_compatibility")
                {float Display(float value){float linear=value*(value*(value*.305306011f+.682171111f)+.012522878f);return Math.Max(0,1.055f*MathF.Pow(linear,.416666667f)-.055f);}expected=new(Display(expected.X),Display(expected.Y),Display(expected.Z));}
                float error=Math.Max(Math.Abs(actual.R-expected.X),Math.Max(Math.Abs(actual.G-expected.Y),Math.Abs(actual.B-expected.Z)));maximum=Math.Max(maximum,error);
                if(error>2/255f)throw new AssetPreviewException($"GPU compressed-normal/tangent/diffuse boundary differs: error={error} actual={actual} expected={expected}.");
            }
        }
        finally{viewport.Free();}
        GD.Print($"PASS: compressed-normal GPU reflected handedness/rotated basis/diffuse/display fixtures=3 maximum_error={maximum:0.######}");
    }
}
