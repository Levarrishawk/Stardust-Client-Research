using System.Numerics;
using Stardust.Core.Assets;
using System.Text.Json;

internal static class CellLightingPreviewScenarios
{
    internal static void Probe(InteriorSceneContentBundle content)
    {
        int cases=0;
        float[] V(Vector3 v)=>new[]{v.X,v.Y,v.Z};
        foreach(int cell in content.Rooms!.CellAppearances.Keys.Order())
        {
            var lights=PortalCellLightsDecoder.Decode(content.Scene.Layout.Cells[cell]);
            var anchor=lights.First(l=>l.Kind==PortalCellLightKind.Point).RuntimeTransform.Translation;
            foreach(bool rotated in new[]{false,true})
            {
                var owner=rotated?new AppearanceTransform(new(0,0,1),Vector3.UnitY,new(-1,0,0),new(20,4,-9)):AppearanceTransform.Identity;
                var rig=new CellLightingPreview(lights,owner,new(.1f));
                foreach(var offset in new[]{new Vector3(0,0,-2),new Vector3(1,-1,3),new Vector3(-3,2,1),Vector3.Zero})
                foreach(var localNormal in new[]{Vector3.UnitZ,-Vector3.UnitZ,Vector3.Normalize(new Vector3(1,2,3))})
                foreach(float power in new[]{20f,300f})foreach(bool includePrimary in new[]{false,true})
                {
                    var position=owner.TransformPoint(anchor+offset);var normal=owner.TransformDirection(localNormal);var viewer=owner.TransformPoint(anchor+new Vector3(0,0,-5));var specular=new Vector3(.2f,.3f,.4f);
                    var value=rig.EvaluateVertex(position,normal,viewer,specular,power,includePrimary);
                    Console.WriteLine("original_cell_lighting="+JsonSerializer.Serialize(new{cell,rotated,offset=V(offset),localNormal=V(localNormal),power,includePrimary,diffuse=V(value.Diffuse),specular=V(value.Specular),primary=rig.Primary!.SourceIndex,additional=rig.Additional.Count}));cases++;
                }
            }
        }
        Console.WriteLine($"PASS: original cell lighting math fixtures={cases} source_order_policy=true rigid_owner_covariance=true");
    }
    internal static void Run()
    {
        if(LegacyPixelColorInput.Saturate(new(-.2f,.5f,1.5f))!=new Vector3(0,.5f,1))throw new Exception("Legacy pixel COLOR saturation failed.");
        HostAccountingScenarios.Reject<InvalidDataException>(()=>LegacyPixelColorInput.Saturate(new(float.NaN)));
        PortalCellLight Light(PortalCellLightKind kind,Vector3 position,Vector3 color,Vector3 attenuation)=>new(kind,new(color,1),new(Vector3.One,1),AppearanceTransform.Identity,AppearanceTransform.Identity with{Translation=position,RowZ=-Vector3.UnitZ},attenuation);
        var directional=Light(PortalCellLightKind.Directional,Vector3.Zero,new(2,3,4),Vector3.Zero);
        var point=Light(PortalCellLightKind.Point,new(0,0,2),new(4,8,12),new(0,0,1));
        var inputs=new[]{directional,point};var rig=new CellLightingPreview(inputs,AppearanceTransform.Identity,new(.1f));inputs[0]=point;
        if(rig.Primary?.SourceIndex!=0||rig.Primary.DirectionOrPosition!=Vector3.UnitZ||rig.Additional.Count!=1)throw new Exception("Owned source-order light selection failed.");
        var evaluated=rig.EvaluateVertex(Vector3.Zero,Vector3.UnitZ,new(0,0,5),new(.2f),20);
        if(Vector3.Distance(evaluated.Diffuse,new(1.1f,2.1f,3.1f))>1e-6||Vector3.Distance(evaluated.Specular,new(.05f))>1e-6)throw new Exception("Hand-computed inverse-square point fixture failed.");
        var all=rig.EvaluateVertex(Vector3.Zero,Vector3.UnitZ,new(0,0,5),new(.2f),20,true);
        if(Vector3.Distance(all.Diffuse,new(3.1f,5.1f,7.1f))>1e-6||Vector3.Distance(all.Specular,new(.25f))>1e-6)throw new Exception("Primary inclusion failed.");
        var back=rig.EvaluateVertex(Vector3.Zero,-Vector3.UnitZ,new(0,0,5),Vector3.One,20,true);
        if(back.Diffuse!=new Vector3(.1f)||back.Specular!=Vector3.Zero)throw new Exception("Vertex back-face light gate failed.");
        var coincident=rig.EvaluateVertex(new(0,0,2),Vector3.UnitZ,new(0,0,2),Vector3.One,20);
        if(coincident.Diffuse!=new Vector3(.1f)||coincident.Specular!=Vector3.Zero)throw new Exception("Coincident point/view policy failed.");
        HostAccountingScenarios.Reject<InvalidDataException>(()=>new CellLightingPreview(Enumerable.Repeat(point,9).ToArray(),AppearanceTransform.Identity,Vector3.Zero));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>new CellLightingPreview(new[]{point with{Attenuation=new(-1,0,1)}},AppearanceTransform.Identity,Vector3.Zero));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>new CellLightingPreview(new[]{directional with{RuntimeTransform=default}},AppearanceTransform.Identity,Vector3.Zero));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>rig.EvaluateVertex(Vector3.Zero,Vector3.Zero,Vector3.Zero,Vector3.One,20));
        HostAccountingScenarios.Reject<ArgumentOutOfRangeException>(()=>rig.EvaluateVertex(Vector3.Zero,Vector3.UnitZ,Vector3.Zero,Vector3.One,float.NaN));
        HostAccountingScenarios.Reject<ArgumentException>(()=>InteriorSceneContentBundleReader.Load(_=>throw new Exception("Invalid cell-light request reached asset lookup."),Array.Empty<byte>(),"","","","",loadCellLights:true));
        Console.WriteLine("PASS: explicit owned cell-light selection, hand-computed point attenuation/specular, back-facing/coincident and budget/direction/negative gates");
    }
}
