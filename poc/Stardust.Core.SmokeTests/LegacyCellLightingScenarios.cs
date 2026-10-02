using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class LegacyCellLightingScenarios
{
    internal static void Probe(InteriorSceneContentBundle content)
    {
        int cases=0;
        float[] V(Vector3 v)=>new[]{v.X,v.Y,v.Z};
        foreach(int cell in content.Rooms!.CellAppearances.Keys.Order())
        {
            var lights=PortalCellLightsDecoder.Decode(content.Scene.Layout.Cells[cell]);
            var anchor=lights.First(l=>l.Kind==PortalCellLightKind.Point).RuntimeTransform.Translation;
            foreach(bool baked in new[]{false,true})foreach(bool rotated in new[]{false,true})
            {
                var owner=rotated?new AppearanceTransform(new(0,0,1),Vector3.UnitY,new(-1,0,0),new(20,4,-9)):AppearanceTransform.Identity;
                var rig=CellLightingPreview.FromLegacyCell(lights,owner,baked?Vector3.Zero:new(.1f),baked);
                foreach(var offset in new[]{new Vector3(0,0,-2),new Vector3(1,-1,3),new Vector3(-3,2,1),Vector3.Zero})
                foreach(var localNormal in new[]{Vector3.UnitZ,-Vector3.UnitZ,Vector3.Normalize(new Vector3(1,2,3))})
                foreach(float power in new[]{20f,300f})foreach(bool includePrimary in new[]{false,true})
                {
                    var position=owner.TransformPoint(anchor+offset);var normal=owner.TransformDirection(localNormal);var viewer=owner.TransformPoint(anchor+new Vector3(0,0,-5));var specular=new Vector3(.2f,.3f,.4f);
                    var value=rig.EvaluateVertex(position,normal,viewer,specular,power,includePrimary);
                    Console.WriteLine("original_legacy_cell_lighting="+JsonSerializer.Serialize(new{cell,baked,rotated,offset=V(offset),localNormal=V(localNormal),power,includePrimary,scaledColors=rig.LegacySelection!.Scaled,sourcePointAttenuation=rig.UsesSourcePointAttenuation,diffuse=V(value.Diffuse),specular=V(value.Specular),primary=rig.Primary?.SourceIndex,additional=rig.Additional.Count}));cases++;
                }
            }
        }
        Console.WriteLine($"PASS: legacy selected cell lighting math fixtures={cases} recovered_mask_slot_policy=true rigid_owner_covariance=true");
    }
    internal static void Run()
    {
        var source=Enumerable.Range(0,4).Select(i=>new PortalCellLight(PortalCellLightKind.Directional,Vector4.One,new(new Vector3(i+1),1),AppearanceTransform.Identity,AppearanceTransform.Identity with{RowZ=-Vector3.UnitZ},new(1,0,0))).ToArray();
        var rig=CellLightingPreview.FromLegacyCell(source,AppearanceTransform.Identity,new(.1f));var value=rig.EvaluateVertex(Vector3.Zero,Vector3.UnitZ,new(0,0,5),new(.2f),0,true);
        var raw=CellLightingPreview.FromLegacyCell(source,AppearanceTransform.Identity,new(.1f),scaleLightColors:false);
        if(!rig.LegacySelection!.Scaled||raw.LegacySelection!.Scaled||raw.EvaluateVertex(Vector3.Zero,Vector3.UnitZ,new(0,0,5),new(.2f),0,true)!=value)throw new Exception("Recovered default/configurable light-scaling snapshot differs for unit CELL scales.");
        if(rig.Primary?.SourceIndex!=3||!rig.Additional.Select(l=>l.SourceIndex).SequenceEqual(new[]{0,1})||rig.Additional.Any(l=>l.Specular!=Vector3.Zero)||Vector3.Distance(value.Diffuse,new(3.1f))>1e-6||Vector3.Distance(value.Specular,new(.8f))>1e-6)throw new Exception("Recovered role-specific overflow/channel arithmetic failed.");
        var baked=CellLightingPreview.FromLegacyCell(source,AppearanceTransform.Identity,Vector3.Zero,true);value=baked.EvaluateVertex(Vector3.Zero,Vector3.UnitZ,new(0,0,5),new(.2f),0,true);
        if(value.Diffuse!=Vector3.Zero||Vector3.Distance(value.Specular,new(.8f))>1e-6)throw new Exception("Baked room included primary diffuse cell lights.");
        Array.Clear(source);if(rig.Primary?.Specular!=new Vector3(4)||rig.Additional.Count!=2)throw new Exception("Legacy rig borrowed mutable source array.");
        var point=new PortalCellLight(PortalCellLightKind.Point,new(.00001f,.00002f,.00003f,1),Vector4.Zero,AppearanceTransform.Identity,AppearanceTransform.Identity with{Translation=new(0,0,.01f)},new(0,0,1));
        var original=CellLightingPreview.FromLegacyCell(new[]{point},AppearanceTransform.Identity,Vector3.Zero);var capped=new CellLightingPreview(new[]{point},AppearanceTransform.Identity,Vector3.Zero);
        var direct=original.EvaluateVertex(Vector3.Zero,Vector3.UnitZ,new(0,0,5),Vector3.Zero,20);var modern=capped.EvaluateVertex(Vector3.Zero,Vector3.UnitZ,new(0,0,5),Vector3.Zero,20);
        if(!original.UsesSourcePointAttenuation||capped.UsesSourcePointAttenuation||Vector3.Distance(direct.Diffuse,new(.1f,.2f,.3f))>1e-6||Vector3.Distance(modern.Diffuse,new(.001f,.002f,.003f))>1e-6)throw new Exception("Recovered positive attenuation was capped.");
        if(original.EvaluateVertex(new(0,0,.01f),Vector3.UnitZ,new(0,0,5),Vector3.Zero,20).Diffuse!=Vector3.Zero)throw new Exception("Coincident replacement light policy is not finite.");
        var zeroDenominator=CellLightingPreview.FromLegacyCell(new[]{point with{Attenuation=Vector3.Zero}},AppearanceTransform.Identity,Vector3.Zero);
        if(zeroDenominator.EvaluateVertex(Vector3.Zero,Vector3.UnitZ,new(0,0,5),Vector3.Zero,20).Diffuse!=Vector3.Zero)throw new Exception("Zero-denominator replacement light policy is not finite.");
        Console.WriteLine("PASS: recovered light slot roles, discarded specular overflow, baked zero diffuse and owned source identity");
    }
}
