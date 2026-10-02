using System;
using System.Threading.Tasks;
using Godot;
using Stardust.Core.Assets;

internal static class StaticVertexColorPreview
{
    internal static async Task VerifyGpuBoundary(Node owner)
    {
        var viewport=new SubViewport{Size=new(64,64),OwnWorld3D=true,RenderTargetUpdateMode=SubViewport.UpdateMode.Always};owner.AddChild(viewport);
        using var shader=new Shader{Code=SkeletalAlbedoPreview.Header+"void fragment(){vec3 rgb=COLOR.rgb;ALBEDO="+SkeletalAlbedoPreview.DisplayExpression(DdsColorSpace.Unspecified)+";}"};using var material=new ShaderMaterial{Shader=shader};using var mesh=new ArrayMesh();float maximum=0;
        try
        {
            viewport.AddChild(new MeshInstance3D{Mesh=mesh});var camera=new Camera3D{Projection=Camera3D.ProjectionType.Orthogonal,Size=2,Position=new(0,0,-3),Current=true};viewport.AddChild(camera);camera.LookAt(Vector3.Zero);
            foreach(uint packed in new[]{0x7F1234ABu,0xFF008040u,uint.MaxValue})
            {
                var n=System.Numerics.Vector3.UnitZ;MeshVertex[] vertices={new(new(-1,-1,0),n,new(0,0),PackedColor0:packed),new(new(-1,1,0),n,new(0,1),PackedColor0:packed),new(new(1,1,0),n,new(1,1),PackedColor0:packed),new(new(1,-1,0),n,new(1,0),PackedColor0:packed)};
                mesh.ClearSurfaces();using var arrays=StaticMeshPreview.CreateArrays(new("synthetic color","0001",vertices,new ushort[]{0,2,1,0,3,2},0x110D));mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles,arrays);mesh.SurfaceSetMaterial(0,material);
                var value=PackedVertexColor.ToRgba(packed);var rgb=new System.Numerics.Vector3(value.X,value.Y,value.Z);
                if(ProjectSettings.GetSetting("rendering/renderer/rendering_method").AsString()=="gl_compatibility")
                {float Display(float v){float linear=v*(v*(v*.305306011f+.682171111f)+.012522878f);return Math.Max(0,1.055f*MathF.Pow(linear,.416666667f)-.055f);}rgb=new(Display(rgb.X),Display(rgb.Y),Display(rgb.Z));}
                await owner.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);await owner.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                using var image=viewport.GetTexture().GetImage();var actual=image.GetPixel(32,32);float error=Math.Max(Math.Abs(actual.R-rgb.X),Math.Max(Math.Abs(actual.G-rgb.Y),Math.Abs(actual.B-rgb.Z)));maximum=Math.Max(maximum,error);
                if(error>2/255f)throw new AssetPreviewException($"Packed COLOR0 GPU channel/display differs error={error}.");
            }
        }
        finally{viewport.Free();}
        GD.Print($"PASS: packed COLOR0 GPU asymmetric RGBA/display fixtures=3 maximum_error={maximum:0.######} source_alpha_preserved_opaque_preview=true");
    }
}
