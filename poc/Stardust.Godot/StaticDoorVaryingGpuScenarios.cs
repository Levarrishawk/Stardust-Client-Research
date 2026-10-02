using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Stardust.Core.Assets;
using NVector=System.Numerics.Vector3;
using NVector4=System.Numerics.Vector4;

internal static class StaticDoorVaryingGpuScenarios
{
    internal static async Task Verify(Node owner,bool skeletal=false,bool pointLighting=false)
    {
        var viewport=new SubViewport{Size=new(64,64),OwnWorld3D=true,RenderTargetUpdateMode=SubViewport.UpdateMode.Always};owner.AddChild(viewport);
        using var mainImage=Image.CreateFromData(1,1,false,Image.Format.Rgba8,new byte[]{160,112,64,192});using var main=ImageTexture.CreateFromImage(mainImage);
        using var normalImage=Image.CreateFromData(1,1,false,Image.Format.Rgba8,new byte[]{0,192,0,192});using var normal=ImageTexture.CreateFromImage(normalImage);
        using var specularImage=Image.CreateFromData(1,1,false,Image.Format.Rgba8,new byte[]{0,0,0,64});using var specular=ImageTexture.CreateFromImage(specularImage);
        string code=skeletal?SkeletalSpecularPreview.Code(DdsColorSpace.Unspecified,false,true):StaticDoorLightingPreview.Code(DdsColorSpace.Unspecified);
        if(pointLighting)code=code.Replace("void fragment(){","uniform int color_debug=0;void fragment(){").Replace("if(visualize_alpha)rgb=vec3(alpha);","if(visualize_alpha)rgb=vec3(alpha);if(color_debug==1)rgb=clamp(cell_vertex_diffuse,vec3(0.0),vec3(1.0));if(color_debug==2)rgb=clamp(cell_vertex_specular,vec3(0.0),vec3(1.0));if(color_debug==3)rgb=diffuse;if(color_debug==4)rgb=raw;if(color_debug==5)rgb=main_sample.rgb*(alpha_hue_mask?mix(first_factor,second_factor,main_sample.a):first_factor);");
        using var shader=new Shader{Code=code};using var material=new ShaderMaterial{Shader=shader};
        material.SetShaderParameter("albedo_texture",main);material.SetShaderParameter("normal_texture",normal);material.SetShaderParameter("ambient_color",new Vector3(.1f,.2f,.05f));material.SetShaderParameter("directional_color",new Vector3(.4f,.3f,.2f));material.SetShaderParameter("directional_specular",new Vector3(.6f,.4f,.2f));material.SetShaderParameter("vertex_specular",new Vector3(.02f,.03f,.01f));material.SetShaderParameter("material_specular",new Vector3(.2f,.3f,.4f));
        var factors=new SkeletalAlbedoFactors("synthetic",skeletal,0,0,skeletal?0xFF4080E0:uint.MaxValue,skeletal?0xFFE06020:uint.MaxValue);SkeletalAlbedoPreview.Apply(material,factors);if(skeletal)material.SetShaderParameter("specular_texture",specular);
        var instance=new MeshInstance3D();viewport.AddChild(instance);var camera=new Camera3D{Size=2,Fov=60,Current=true};viewport.AddChild(camera);int cases=0;float maximum=0,difference=0,pointStageDifference=0;
        try
        {
            foreach(float sign in new[]{1f,-1f})
            {
                float[] angles={-1.2f,.15f,1.1f};NVector[] positions={new(-1,-1,0),new(1,-1,.5f),new(0,1,-.5f)};
                var vertices=positions.Select((p,i)=>new MeshVertex(p,NVector.UnitZ,new(.5f),new[]{new MeshTextureCoordinate(new(MathF.Cos(angles[i]),MathF.Sin(angles[i]),0,sign),4)})).ToArray();using var mesh=new ArrayMesh();
                using(var arrays=StaticMeshPreview.CreateArrays(new("synthetic varying triangle","0001",vertices,new ushort[]{0,1,2}),1))mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles,arrays);mesh.SurfaceSetMaterial(0,material);instance.Mesh=mesh;
                foreach(bool rotated in new[]{false,true})foreach(bool perspective in new[]{false,true})foreach(float power in new[]{2f,20f})
                {
                    var pose=rotated?new AppearanceTransform(new(0,0,1),NVector.UnitY,new(-1,0,0),new(20,4,-9)):AppearanceTransform.Identity;instance.Transform=GodotAppearanceTransform.ToGodot(pose);
                    Vector3 Reflect(NVector v)=>new(v.X,v.Y,-v.Z);var localCamera=new NVector(0,0,3.5f);camera.Position=Reflect(pose.TransformPoint(localCamera));camera.LookAt(Reflect(pose.TransformPoint(NVector.Zero)));camera.Projection=perspective?Camera3D.ProjectionType.Perspective:Camera3D.ProjectionType.Orthogonal;
                    var light=NVector.Normalize(new NVector(.2f,.4f,1));material.SetShaderParameter("light_direction_world",Reflect(pose.TransformDirection(light)));material.SetShaderParameter("material_power",power);
                    CellLightingPreview? rig=null;var emissive=pointLighting?new NVector(.01f,.02f,.015f):NVector.Zero;
                    material.SetShaderParameter("material_emissive",new Vector3(emissive.X,emissive.Y,emissive.Z));
                    if(pointLighting)
                    {
                        var primary=new PortalCellLight(PortalCellLightKind.Directional,new(.4f,.3f,.2f,1),new(.6f,.4f,.2f,1),AppearanceTransform.Identity,new AppearanceTransform(new(1,0,-light.X),new(0,1,-light.Y),new(0,0,-light.Z),NVector.Zero),new(1,0,0));
                        var point=new PortalCellLight(PortalCellLightKind.Point,new(.5f,.35f,.2f,1),new(.6f,.4f,.2f,1),AppearanceTransform.Identity,AppearanceTransform.Identity with{Translation=new(-.5f,-.5f,1.2f)},new(.2f,.1f,.02f));
                        var second=point with{DiffuseRgba=new(.05f,.08f,.06f,1),SpecularRgba=new(.04f,.05f,.06f,1),RuntimeTransform=AppearanceTransform.Identity with{Translation=new(1,.5f,2)}};
                        rig=CellLightingPreview.FromLegacyCell(new[]{primary,point,second},pose,new(.1f,.2f,.05f));CellLightingGpuPreview.Apply(material,rig,false);
                    }
                    var colors=positions.Select(p=>rig is null?new PreviewVertexLighting(new(.1f,.2f,.05f),new(.02f,.03f,.01f)):rig.EvaluateVertex(pose.TransformPoint(p),pose.TransformDirection(NVector.UnitZ),pose.TransformPoint(localCamera),new(.2f,.3f,.4f),power)).ToArray();
                    var varying=vertices.Select(v=>TangentLightingVertexEvaluator.Evaluate(v.Position,v.Normal,StaticNormalMaterialReader.PreviewTangent(v,1,out _),light,localCamera)).ToArray();
                    var projected=positions.Select(p=>camera.UnprojectPosition(instance.ToGlobal(Reflect(p)))).ToArray();var clipW=perspective?new NVector(3.5f-positions[0].Z,3.5f-positions[1].Z,3.5f-positions[2].Z):NVector.One;
                    await owner.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);await owner.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);using var image=viewport.GetTexture().GetImage();
                    foreach(var pixel in new[]{new Vector2I(32,32),new Vector2I(28,38),new Vector2I(36,38)})
                    {
                        var point=new Vector2(pixel.X+.5f,pixel.Y+.5f);float Cross(Vector2 a,Vector2 b)=>a.X*b.Y-a.Y*b.X;float area=Cross(projected[1]-projected[0],projected[2]-projected[0]);float b=Cross(point-projected[0],projected[2]-projected[0])/area,c=Cross(projected[1]-projected[0],point-projected[0])/area;var weights=new NVector(1-b-c,b,c);
                        var wp=weights/clipW;wp/=wp.X+wp.Y+wp.Z;var vd=colors[0].Diffuse*wp.X+colors[1].Diffuse*wp.Y+colors[2].Diffuse*wp.Z+emissive;var vs=colors[0].Specular*wp.X+colors[1].Specular*wp.Y+colors[2].Specular*wp.Z;
                        var interpolated=TangentLightingVertexEvaluator.Interpolate(varying,weights,clipW);var fields=new StaticShaderMaterial(NVector4.One,NVector4.One,NVector4.Zero,new(.2f,.3f,.4f,1),power);var descriptor=new StaticNormalMaterial("synthetic","synthetic","synthetic","synthetic",1,fields);var lighting=new SkeletalPixelLighting(interpolated.Light,interpolated.HalfAngle,new(.4f,.3f,.2f),new(.6f,.4f,.2f),LegacyPixelColorInput.Saturate(vd),LegacyPixelColorInput.Saturate(vs));var value=SkeletalSpecularEvaluator.Evaluate(new(160/255f,112/255f,64/255f,192/255f),new(0,192/255f,0,192/255f),skeletal?64/255f:192/255f,factors,fields,lighting);var rgb=new NVector(value.X,value.Y,value.Z);
                        var p=positions[0]*wp.X+positions[1]*wp.Y+positions[2]*wp.Z;var t=NVector.Zero;for(int i=0;i<3;i++){var v=StaticNormalMaterialReader.PreviewTangent(vertices[i],1,out _);t+=new NVector(v.X,v.Y,v.Z)*(i==0?wp.X:i==1?wp.Y:wp.Z);}var bitangent=NVector.Cross(NVector.UnitZ,t)*sign;var nt=SkeletalCompressedNormalEvaluator.Decode(new(0,192/255f,0,192/255f));var mapped=NVector.Normalize(t*nt.X+bitangent*nt.Y+NVector.UnitZ*nt.Z);var half=NVector.Normalize(light+NVector.Normalize(localCamera-p));var d=NVector.Clamp(lighting.VertexDiffuse+new NVector(.4f,.3f,.2f)*Math.Clamp(NVector.Dot(light,mapped),0,1),NVector.Zero,NVector.One);var s=(new NVector(.6f,.4f,.2f)*new NVector(.2f,.3f,.4f)*MathF.Pow(Math.Clamp(NVector.Dot(half,mapped),0,1),power)+lighting.VertexSpecular)*(skeletal?64/255f:192/255f);var old=SkeletalAlbedoEvaluator.Evaluate(new(160/255f,112/255f,64/255f,192/255f),factors)*d+s;difference=Math.Max(difference,NVector.Distance(old,rgb));
                        if(rig is not null)
                        {
                            var late=rig.EvaluateVertex(pose.TransformPoint(p),pose.TransformDirection(NVector.UnitZ),pose.TransformPoint(localCamera),new(.2f,.3f,.4f),power);
                            pointStageDifference=Math.Max(pointStageDifference,Math.Max(NVector.Distance(LegacyPixelColorInput.Saturate(late.Diffuse+emissive),lighting.VertexDiffuse),NVector.Distance(LegacyPixelColorInput.Saturate(late.Specular),lighting.VertexSpecular)));
                        }
                        if(ProjectSettings.GetSetting("rendering/renderer/rendering_method").AsString()=="gl_compatibility"){float Display(float v){float linear=v*(v*(v*.305306011f+.682171111f)+.012522878f);return Math.Max(0,1.055f*MathF.Pow(linear,.416666667f)-.055f);}rgb=new(Display(rgb.X),Display(rgb.Y),Display(rgb.Z));}
                        var actual=image.GetPixel(pixel.X,pixel.Y);float error=Math.Max(Math.Abs(actual.R-rgb.X),Math.Max(Math.Abs(actual.G-rgb.Y),Math.Abs(actual.B-rgb.Z)));maximum=Math.Max(maximum,error);cases++;if(error>2/255f&&pointLighting)
                        {
                            foreach(int debug in new[]{1,2,3,4,5}){material.SetShaderParameter("color_debug",debug);await owner.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);await owner.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);using var diagnostic=viewport.GetTexture().GetImage();GD.Print($"POINT DIAGNOSTIC mode={debug} actual={diagnostic.GetPixel(pixel.X,pixel.Y)} vd={vd} vs={vs} colors={string.Join(';',colors.Select(v=>$"{v.Diffuse}/{v.Specular}"))}");}material.SetShaderParameter("color_debug",0);
                        }
                        if(error>2/255f)throw new AssetPreviewException($"Tangent vertex varying GPU projection/interpolation differs case={cases} error={error} perspective={perspective} rotated={rotated} actual={actual} expected={rgb} vd={vd} vs={vs} weights={wp}.");
                    }
                }
                instance.Mesh=null!;
            }
            if(pointLighting&&pointStageDifference<.005f)throw new AssetPreviewException("Point vertex-color fixture failed to distinguish per-fragment light recomputation.");
            if(difference<.005f)throw new AssetPreviewException("Tangent varying fixture failed to distinguish fragment recomputation.");
        }
        finally{viewport.Free();}
        GD.Print($"PASS: {(skeletal?"skeletal":"door")} vertex tangent light/half-angle interpolation GPU fixtures={cases} handedness_owner_perspective=true maximum_error={maximum:0.######} fragment_policy_difference={difference:0.######} point_lighting={pointLighting} point_vertex_stage_difference={pointStageDifference:0.######}");
    }
}
