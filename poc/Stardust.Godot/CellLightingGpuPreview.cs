using System;
using System.Threading.Tasks;
using System.Linq;
using Godot;
using Stardust.Core.Assets;

internal static class CellLightingGpuPreview
{
    internal static async Task VerifyGpuBoundary(Node owner,bool legacy=false)
    {
        var viewport=new SubViewport{Size=new(64,64),OwnWorld3D=true,RenderTargetUpdateMode=SubViewport.UpdateMode.Always};owner.AddChild(viewport);
        using var shader=new Shader{Code=SkeletalAlbedoPreview.Header+"uniform vec3 ambient_color;uniform vec3 material_specular;uniform float material_power;uniform vec3 position_input;uniform vec3 normal_input;uniform vec3 viewer_input;uniform bool show_specular;"+Uniforms+Functions+"void fragment(){vec3 d;vec3 s;cell_lighting(position_input,normal_input,viewer_input,d,s);vec3 rgb=show_specular?s:d;ALBEDO="+SkeletalAlbedoPreview.DisplayExpression(DdsColorSpace.Unspecified)+";}"};
        using var material=new ShaderMaterial{Shader=shader};using var mesh=new QuadMesh{Size=new(2,2),Material=material};
        float maximum=0,modernDifference=0;int cases=0;
        try
        {
            viewport.AddChild(new MeshInstance3D{Mesh=mesh});var camera=new Camera3D{Projection=Camera3D.ProjectionType.Orthogonal,Size=2,Position=new(0,0,3),Current=true};viewport.AddChild(camera);camera.LookAt(Vector3.Zero);
            foreach(bool rotated in new[]{false,true})foreach(int distanceCase in legacy?new[]{0,1,2,3}:new[]{0,1})foreach(bool back in new[]{false,true})foreach(bool showSpecular in new[]{false,true})foreach(bool fullBudget in new[]{false,true})
            {
                var pose=rotated?new AppearanceTransform(new(0,0,1),System.Numerics.Vector3.UnitY,new(-1,0,0),new(20,4,-9)):AppearanceTransform.Identity;
                var point=new PortalCellLight(PortalCellLightKind.Point,new((legacy?.000004f:.0004f),(legacy?.000008f:.0008f),(legacy?.000012f:.0012f),1),new((legacy?.000008f:.0008f),(legacy?.000012f:.0012f),(legacy?.000016f:.0016f),1),AppearanceTransform.Identity,AppearanceTransform.Identity with{Translation=new(0,0,distanceCase==1?.01f:distanceCase==2?0:2)},distanceCase==3?System.Numerics.Vector3.Zero:new(0,0,1));
                var directional=new PortalCellLight(PortalCellLightKind.Directional,new(.01f,.02f,.03f,1),new(.02f,.03f,.04f,1),AppearanceTransform.Identity,AppearanceTransform.Identity with{RowZ=-System.Numerics.Vector3.UnitZ},new(1,0,0));
                var records=fullBudget?(legacy?Enumerable.Repeat(point,5).Concat(Enumerable.Repeat(directional,3)).ToArray():new[]{point}.Concat(Enumerable.Repeat(directional,7)).ToArray()):new[]{point};
                var rig=legacy?CellLightingPreview.FromLegacyCell(records,pose,new(.05f)):new CellLightingPreview(records,pose,new(.05f));Apply(material,rig,true);
                var p=pose.TransformPoint(System.Numerics.Vector3.Zero);var n=pose.TransformDirection(back?-System.Numerics.Vector3.UnitZ:System.Numerics.Vector3.UnitZ);var viewer=pose.TransformPoint(new(0,0,5));
                Vector3 Reflect(System.Numerics.Vector3 v)=>new(v.X,v.Y,-v.Z);
                material.SetShaderParameter("position_input",Reflect(p));material.SetShaderParameter("normal_input",Reflect(n));material.SetShaderParameter("viewer_input",Reflect(viewer));material.SetShaderParameter("material_specular",new Vector3(.2f,.3f,.4f));material.SetShaderParameter("material_power",20f);material.SetShaderParameter("show_specular",showSpecular);
                var expected=rig.EvaluateVertex(p,n,viewer,new(.2f,.3f,.4f),20,true);var rgb=showSpecular?expected.Specular:expected.Diffuse;
                if(legacy&&!fullBudget){var modern=new CellLightingPreview(records,pose,new(.05f)).EvaluateVertex(p,n,viewer,new(.2f,.3f,.4f),20,true);modernDifference=Math.Max(modernDifference,System.Numerics.Vector3.Distance(rgb,showSpecular?modern.Specular:modern.Diffuse));}
                if(ProjectSettings.GetSetting("rendering/renderer/rendering_method").AsString()=="gl_compatibility")
                {float Display(float v){float linear=v*(v*(v*.305306011f+.682171111f)+.012522878f);return Math.Max(0,1.055f*MathF.Pow(linear,.416666667f)-.055f);}rgb=new(Display(rgb.X),Display(rgb.Y),Display(rgb.Z));}
                await owner.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);await owner.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                using var image=viewport.GetTexture().GetImage();var actual=image.GetPixel(32,32);float error=Math.Max(Math.Abs(actual.R-rgb.X),Math.Max(Math.Abs(actual.G-rgb.Y),Math.Abs(actual.B-rgb.Z)));maximum=Math.Max(maximum,error);cases++;
                if(error>2/255f)throw new AssetPreviewException($"Cell lighting GPU arithmetic differs case={cases} error={error}.");
            }
        }
        finally{viewport.Free();}
        if(legacy&&modernDifference<.05f)throw new AssetPreviewException("Recovered attenuation GPU fixtures did not distinguish the old cap.");
        GD.Print($"PASS: cell lighting GPU point/attenuation-cap/specular/back-facing/reflected-owner legacy={legacy} fixtures={cases} maximum_error={maximum:0.######} source_attenuation={legacy} cap_policy_difference={modernDifference:0.######}");
    }
    internal const string Uniforms="uniform bool source_point_attenuation=false; uniform bool cell_lighting_enabled=false; uniform int cell_light_count=0; uniform vec4 cell_light_inputs[8]; uniform vec3 cell_light_diffuse[8]; uniform vec3 cell_light_specular[8]; uniform vec3 cell_light_attenuation[8]; varying vec3 cell_vertex_diffuse; varying vec3 cell_vertex_specular; ";
    internal const string Functions="void cell_lighting(vec3 p,vec3 n,vec3 viewer,out vec3 diffuse,out vec3 specular) { diffuse=ambient_color; specular=vec3(0.0); vec3 view_delta=viewer-p; vec3 v=view_delta/max(length(view_delta),0.000001); for(int i=0;i<8;i++){if(i>=cell_light_count)break; bool point=cell_light_inputs[i].w>0.5; vec3 delta=point?cell_light_inputs[i].xyz-p:cell_light_inputs[i].xyz; float distance=point?length(delta):0.0; vec3 l=delta/max(length(delta),0.000001); float denominator=dot(cell_light_attenuation[i],vec3(1.0,distance,distance*distance));if(point&&source_point_attenuation&&(distance<=0.000001||denominator<=0.0))continue;float attenuation=point?1.0/(source_point_attenuation?denominator:max(0.01,denominator)):1.0; float nl=dot(n,l); diffuse+=cell_light_diffuse[i]*max(nl,0.0)*attenuation; vec3 hs=l+v; vec3 h=hs/max(length(hs),0.000001); if(nl>0.0)specular+=cell_light_specular[i]*material_specular*(material_power==0.0?1.0:pow(clamp(dot(n,h),0.0,1.0),material_power))*attenuation; } } ";
    internal const string Vertex="void vertex(){cell_vertex_diffuse=ambient_color;cell_vertex_specular=vertex_specular;if(cell_lighting_enabled){cell_lighting((MODEL_MATRIX*vec4(VERTEX,1.0)).xyz,normalize(MODEL_NORMAL_MATRIX*NORMAL),INV_VIEW_MATRIX[3].xyz,cell_vertex_diffuse,cell_vertex_specular);}} ";
    internal static void Apply(ShaderMaterial target,CellLightingPreview rig,bool includePrimary)
    {
        Vector3 Convert(System.Numerics.Vector3 v)=>new(v.X,v.Y,-v.Z);
        Vector3 Color(System.Numerics.Vector3 v)=>new(v.X,v.Y,v.Z);
        var inputs=new Vector4[8];var diffuse=new Vector3[8];var specular=new Vector3[8];var attenuation=new Vector3[8];int count=0;
        void Add(PreviewLight light){var v=Convert(light.DirectionOrPosition);inputs[count]=new(v.X,v.Y,v.Z,light.Point?1:0);diffuse[count]=Color(light.Diffuse);specular[count]=Color(light.Specular);attenuation[count]=Color(light.Attenuation);count++;}
        if(includePrimary&&rig.Primary is not null&&rig.LegacySelection is not null)Add(rig.Primary);foreach(var light in rig.Additional)Add(light);if(includePrimary&&rig.Primary is not null&&rig.LegacySelection is null)Add(rig.Primary);
        target.SetShaderParameter("source_point_attenuation",rig.UsesSourcePointAttenuation);
        target.SetShaderParameter("cell_light_inputs",inputs);target.SetShaderParameter("cell_light_diffuse",diffuse);target.SetShaderParameter("cell_light_specular",specular);target.SetShaderParameter("cell_light_attenuation",attenuation);target.SetShaderParameter("cell_light_count",count);
        target.SetShaderParameter("ambient_color",Color(rig.Ambient));target.SetShaderParameter("directional_color",rig.Primary is null?Vector3.Zero:Color(rig.Primary.Diffuse));target.SetShaderParameter("directional_specular",rig.Primary is null?Vector3.Zero:Color(rig.Primary.Specular));target.SetShaderParameter("light_direction_world",rig.Primary is null?Vector3.Forward:Convert(rig.Primary.DirectionOrPosition));target.SetShaderParameter("cell_lighting_enabled",true);
        if(!target.GetShaderParameter("cell_light_inputs").AsVector4Array().SequenceEqual(inputs)||!target.GetShaderParameter("cell_light_diffuse").AsVector3Array().SequenceEqual(diffuse)||!target.GetShaderParameter("cell_light_specular").AsVector3Array().SequenceEqual(specular)||!target.GetShaderParameter("cell_light_attenuation").AsVector3Array().SequenceEqual(attenuation))throw new AssetPreviewException("Cell lighting GPU array identity differs.");
        if(target.GetShaderParameter("source_point_attenuation").AsBool()!=rig.UsesSourcePointAttenuation||target.GetShaderParameter("cell_light_count").AsInt32()!=count||!target.GetShaderParameter("cell_lighting_enabled").AsBool())throw new AssetPreviewException("Cell lighting GPU publication failed.");
    }
}
