using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Stardust.Core.Assets;
using NVector=System.Numerics.Vector3;
using NVector4=System.Numerics.Vector4;

/// <summary>Bounded replacement GPU proof of source-shaped mouth COLOR channels; literal power is modern.</summary>
internal static class SkeletalMouthLightingGpuVerification
{
    internal static async Task VerifyAsync(Node owner)
    {
        ArgumentNullException.ThrowIfNull(owner);VerifyReferenceInputs();
        var viewport=new SubViewport{Size=new(64,64),OwnWorld3D=true,RenderTargetUpdateMode=SubViewport.UpdateMode.Always};owner.AddChild(viewport);
        using var mainImage=Image.CreateFromData(1,1,false,Image.Format.Rgba8,new byte[]{160,112,64,192});using var main=ImageTexture.CreateFromImage(mainImage);
        using var maskImage=Image.CreateFromData(1,1,false,Image.Format.Rgba8,new byte[]{0,0,0,96});using var mask=ImageTexture.CreateFromImage(maskImage);
        // Instrument only the observed varying at the existing fragment/display boundary; the vertex code is unchanged.
        string code=SkeletalVertexLitSpecularPreview.Code(DdsColorSpace.Unspecified,false)
            .Replace("void fragment() {","uniform int mouth_debug=0; void fragment() {")
            .Replace("if(visualize_alpha)rgb=vec3(result_alpha);","if(visualize_alpha)rgb=vec3(result_alpha); if(mouth_debug==1)rgb=vertex_diffuse_preview; if(mouth_debug==2)rgb=vertex_specular_preview;");
        using var shader=new Shader{Code=code};using var material=new ShaderMaterial{Shader=shader};
        material.SetShaderParameter("albedo_texture",main);material.SetShaderParameter("specular_texture",mask);
        var factors=new SkeletalAlbedoFactors("synthetic mouth multi-light",true,0,0,0xFF4080E0,0xFFE06020);SkeletalAlbedoPreview.Apply(material,factors);SkeletalAlbedoPreview.Verify(material,factors);
        var fields=new StaticShaderMaterial(new(3,4,5,1),new(6,7,8,1),new(.025f,.035f,.02f,1),new(.45f,.25f,.1f,1),2);
        var metadata=IffDocument.Parse(new byte[]{70,79,82,77,0,0,0,4,84,69,83,84}).Root;var program=new SkeletalVertexLitSpecularProgram(true,false,"synthetic",metadata);
        var instance=new MeshInstance3D();viewport.AddChild(instance);var camera=new Camera3D{Size=2,Fov=60,Current=true};viewport.AddChild(camera);
        int cases=0;float maximum=0,primaryDifference=0,channelDifference=0,emissiveDifference=0,fragmentDifference=0,specularLeakDifference=0;
        try
        {
            foreach(bool back in new[]{false,true})
            {
                NVector[] positions={new(-1,-1,0),new(1,-1,.5f),new(0,1,-.5f)};
                NVector[] normals={NVector.UnitZ,NVector.Normalize(new(.12f,.03f,1)),NVector.Normalize(new(-.05f,-.08f,1))};if(back)normals=normals.Select(n=>-n).ToArray();
                var vertices=positions.Select((p,i)=>new MeshVertex(p,normals[i],new(.5f))).ToArray();using var mesh=new ArrayMesh();
                using(var arrays=StaticMeshPreview.CreateArrays(new("synthetic mouth channel triangle","0001",vertices,new ushort[]{0,1,2})))mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles,arrays);
                mesh.SurfaceSetMaterial(0,material);instance.Mesh=mesh;
                try
                {
                    foreach(bool rotated in new[]{false,true})foreach(bool perspective in new[]{false,true})foreach(float power in new[]{2f,20f,300f})
                    {
                        var pose=rotated?new AppearanceTransform(new(0,0,1),NVector.UnitY,new(-1,0,0),new(20,4,-9)):AppearanceTransform.Identity;
                        instance.Transform=GodotAppearanceTransform.ToGodot(pose);var viewerLocal=new NVector(0,0,3.5f);var viewer=pose.TransformPoint(viewerLocal);
                        camera.Position=Reflect(viewer);camera.LookAt(Reflect(pose.TransformPoint(NVector.Zero)));camera.Projection=perspective?Camera3D.ProjectionType.Perspective:Camera3D.ProjectionType.Orthogonal;
                        var records=Lights();var ambient=new NVector(.045f,.065f,.055f);var rig=CellLightingPreview.FromLegacyCell(records,pose,ambient,scaleLightColors:false);
                        if(rig.Additional.Count!=7||rig.Primary is null||NVector.Distance(rig.Primary.DirectionOrPosition,pose.TransformDirection(NVector.Normalize(new(.15f,.1f,1))))>1e-6f)throw new AssetPreviewException("Mouth fixture primary axis/slot selection differs.");
                        fields=fields with{SpecularPower=power};SkeletalSpecularPreview.Apply(material,new("synthetic",fields,0,false));SkeletalSpecularPreview.Verify(material,new("synthetic",fields,0,false));
                        material.SetShaderParameter("emissive_color",new Vector3(fields.EmissiveRgba.X,fields.EmissiveRgba.Y,fields.EmissiveRgba.Z));CellLightingGpuPreview.Apply(material,rig,true);
                        var colors=positions.Select((p,i)=>SkeletalVertexLitLightingEvaluator.EvaluateModernLiteralPower(rig,pose.TransformPoint(p),pose.TransformDirection(normals[i]),viewer,fields)).ToArray();
                        var noPrimary=positions.Select((p,i)=>rig.EvaluateVertex(pose.TransformPoint(p),pose.TransformDirection(normals[i]),viewer,new(.45f,.25f,.1f),power)).ToArray();
                        var incorrectChannels=CellLightingPreview.FromLegacyCell(new[]{records[0],records[3]},pose,ambient,scaleLightColors:false);
                        var fewer=positions.Select((p,i)=>SkeletalVertexLitLightingEvaluator.EvaluateModernLiteralPower(incorrectChannels,pose.TransformPoint(p),pose.TransformDirection(normals[i]),viewer,fields)).ToArray();
                        var wrongChannels=new CellLightingPreview(records,pose,ambient);
                        var leaked=positions.Select((p,i)=>wrongChannels.EvaluateVertex(pose.TransformPoint(p),pose.TransformDirection(normals[i]),viewer,new(.45f,.25f,.1f),power,includePrimary:true)).ToArray();
                        var projected=positions.Select(p=>camera.UnprojectPosition(instance.ToGlobal(Reflect(p)))).ToArray();var clipW=perspective?new NVector(3.5f-positions[0].Z,3.5f-positions[1].Z,3.5f-positions[2].Z):NVector.One;
                        foreach(int mode in new[]{1,2,0})
                        {
                            material.SetShaderParameter("mouth_debug",mode);material.SetShaderParameter("bloom_enabled",power==300);material.SetShaderParameter("fixed_vertex_lighting",false);
                            await owner.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);await owner.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                            using var image=viewport.GetTexture().GetImage();
                            foreach(var pixel in new[]{new Vector2I(32,32),new Vector2I(28,38),new Vector2I(36,38)})
                            {
                                var weights=Weights(projected,pixel,clipW);var vd=Blend(colors.Select(v=>v.Diffuse).ToArray(),weights);var vs=Blend(colors.Select(v=>v.Specular).ToArray(),weights);
                                var expectedPixel=SkeletalVertexLitSpecularEvaluator.Evaluate(new(160/255f,112/255f,64/255f,192/255f),96/255f,factors,program,LegacyPixelColorInput.Saturate(vd),LegacyPixelColorInput.Saturate(vs),power==300);
                                var rgb=mode==1?vd:mode==2?vs:new NVector(expectedPixel.X,expectedPixel.Y,expectedPixel.Z);rgb=Display(rgb);
                                var actual=image.GetPixel(pixel.X,pixel.Y);float error=Math.Max(Math.Abs(actual.R-rgb.X),Math.Max(Math.Abs(actual.G-rgb.Y),Math.Abs(actual.B-rgb.Z)));maximum=Math.Max(maximum,error);cases++;
                                if(error>2/255f)throw new AssetPreviewException($"Mouth multi-light COLOR GPU differs case={cases} mode={mode} power={power} back={back} owner={rotated} perspective={perspective} error={error} actual={actual} expected={rgb}.");
                                var omitted=Blend(noPrimary.Select(v=>v.Diffuse).ToArray(),weights)+new NVector(.025f,.035f,.02f);primaryDifference=Math.Max(primaryDifference,MaxDifference(Display(vd),Display(omitted)));
                                channelDifference=Math.Max(channelDifference,MaxDifference(Display(vd),Display(Blend(fewer.Select(v=>v.Diffuse).ToArray(),weights))));
                                specularLeakDifference=Math.Max(specularLeakDifference,MaxDifference(Display(vs),Display(Blend(leaked.Select(v=>v.Specular).ToArray(),weights))));
                                emissiveDifference=Math.Max(emissiveDifference,MaxDifference(Display(vd),Display(vd-new NVector(.025f,.035f,.02f))));
                                var p=Blend(positions,weights);var n=NVector.Normalize(Blend(normals,weights));var late=SkeletalVertexLitLightingEvaluator.EvaluateModernLiteralPower(rig,pose.TransformPoint(p),pose.TransformDirection(n),viewer,fields);
                                fragmentDifference=Math.Max(fragmentDifference,Math.Max(MaxDifference(Display(vd),Display(late.Diffuse)),MaxDifference(Display(vs),Display(late.Specular))));
                            }
                        }
                    }
                }
                finally{instance.Mesh=null!;}
            }
            if(primaryDifference<=4/255f||channelDifference<=4/255f||emissiveDifference<=4/255f||fragmentDifference<=4/255f||specularLeakDifference<=4/255f)throw new AssetPreviewException("Mouth diagnostic failed to distinguish primary/channel/emissive/vertex-stage errors.");
        }
        finally{viewport.Free();}
        if(GodotObject.IsInstanceValid(viewport))throw new AssetPreviewException("Mouth verification viewport survived cleanup.");
        GD.Print($"PASS: mouth source-shaped multi-light COLOR0/COLOR1/pixel GPU fixtures={cases} maximum_error={maximum:0.######} primary_discriminator={primaryDifference:0.######} diffuse_slot_discriminator={channelDifference:0.######} wrong_specular_channel_discriminator={specularLeakDifference:0.######} emissive_discriminator={emissiveDifference:0.######} fragment_discriminator={fragmentDifference:0.######} power_policy=modern_literal_2_20_300 original_compiled_LIT=UNKNOWN cleanup=deterministic");
    }
    private static PortalCellLight[] Lights()
    {
        PortalCellLight Direction(NVector toward,NVector diffuse,NVector specular)
        {
            toward=NVector.Normalize(toward);
            // TransformDirection(UnitZ) reads the third COLUMN, not RowZ.
            var pose=new AppearanceTransform(new(1,0,-toward.X),new(0,1,-toward.Y),new(0,0,-toward.Z),NVector.Zero);
            return new(PortalCellLightKind.Directional,new(diffuse,1),new(specular,1),AppearanceTransform.Identity,pose,new(1,0,0));
        }
        var result=new PortalCellLight[8];result[0]=Direction(new(.15f,.1f,1),new(.3f,.2f,.12f),new(.8f,.65f,.5f));
        result[1]=Direction(new(.2f,-.3f,1),new(.08f,.05f,.04f),new(.03f));result[2]=Direction(new(-.3f,.2f,1),new(.04f,.07f,.02f),new(.02f));
        for(int i=0;i<5;i++)result[i+3]=new(PortalCellLightKind.Point,new(i==0?new NVector(.2f,.1f,.05f):new NVector(.015f+.005f*i,.025f,.02f),1),new(i==0?new NVector(.35f,.25f,.2f):new NVector(.015f),1),AppearanceTransform.Identity,AppearanceTransform.Identity with{Translation=i==0?new(-.3f,-.4f,1.5f):new(.3f*i,-.2f*i,1.8f+.2f*i)},new(.6f,.2f,.05f));
        return result;
    }
    private static Vector3 Reflect(NVector value)=>new(value.X,value.Y,-value.Z);
    private static NVector Blend(NVector[] values,NVector weights)=>values[0]*weights.X+values[1]*weights.Y+values[2]*weights.Z;
    private static float MaxDifference(NVector a,NVector b)=>Math.Max(Math.Abs(a.X-b.X),Math.Max(Math.Abs(a.Y-b.Y),Math.Abs(a.Z-b.Z)));
    private static NVector Weights(Vector2[] projected,Vector2I pixel,NVector clipW)
    {
        if(projected.Length!=3||!float.IsFinite(clipW.X)||!float.IsFinite(clipW.Y)||!float.IsFinite(clipW.Z)||clipW.X<=0||clipW.Y<=0||clipW.Z<=0)throw new AssetPreviewException("Positive finite mouth clip-W required.");
        var p=new Vector2(pixel.X+.5f,pixel.Y+.5f);float Cross(Vector2 a,Vector2 b)=>a.X*b.Y-a.Y*b.X;float area=Cross(projected[1]-projected[0],projected[2]-projected[0]);
        float b=Cross(p-projected[0],projected[2]-projected[0])/area,c=Cross(projected[1]-projected[0],p-projected[0])/area;var weights=new NVector(1-b-c,b,c);
        if(NVector.Min(weights,NVector.Zero)!=NVector.Zero||!float.IsFinite(area)||Math.Abs(area)<1e-6f)throw new AssetPreviewException("Invalid mouth diagnostic screen triangle/sample.");
        weights/=clipW;return weights/(weights.X+weights.Y+weights.Z);
    }
    private static void VerifyReferenceInputs()
    {
        Vector2[] screen={new(0,0),new(2,0),new(0,2)};
        var weights=Weights(screen,new(0,0),new(1,2,4));
        if(NVector.Distance(weights,new(8/11f,2/11f,1/11f))>1e-6f)throw new AssetPreviewException("Mouth hand perspective-weight fixture differs.");
        void Reject(Action action){try{action();}catch(AssetPreviewException){return;}throw new AssetPreviewException("Invalid mouth reference accepted.");}
        Reject(()=>Weights(screen,new(4,4),NVector.One));Reject(()=>Weights(screen,new(0,0),new(0,1,1)));
        Reject(()=>Weights(screen,new(0,0),new(float.NaN,1,1)));Reject(()=>Weights(new[]{Vector2.Zero,Vector2.Zero,Vector2.Zero},new(0,0),NVector.One));
        GD.Print("PASS: mouth hand perspective weights and outside/zero-W/nonfinite/degenerate reference gates");
    }
    private static NVector Display(NVector rgb)
    {
        if(ProjectSettings.GetSetting("rendering/renderer/rendering_method").AsString()!="gl_compatibility")return rgb;
        float Transfer(float value){float linear=value*(value*(value*.305306011f+.682171111f)+.012522878f);return Math.Max(0,1.055f*MathF.Pow(linear,.416666667f)-.055f);}
        return new(Transfer(rgb.X),Transfer(rgb.Y),Transfer(rgb.Z));
    }
}
