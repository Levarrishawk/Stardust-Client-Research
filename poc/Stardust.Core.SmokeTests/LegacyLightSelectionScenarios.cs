using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class LegacyLightSelectionScenarios
{
    internal static void Run()
    {
        var rows=Enumerable.Range(0,8).Select(i=>new LegacyLightInput(i,3,new(i/10f),new((7-i)/10f))).ToArray();var slots=LegacyLightSelector.Select(rows,Vector3.Zero);
        if(slots.SpecularPoint?.Key!=0||!slots.DiffusePoint.Select(r=>r.Key).SequenceEqual(new[]{7,6,5,4}))throw new Exception("Independent strongest-specular then diffuse partition failed.");
        var ties=new[]{new LegacyLightInput(9,1,new(1),new(1)),new LegacyLightInput(8,1,new(1),new(1)),new LegacyLightInput(7,1,new(1),new(1)),new LegacyLightInput(6,1,new(1),new(1))};slots=LegacyLightSelector.Select(ties,new(.1f));
        if(slots.SpecularDirectional?.Key!=9||!slots.DiffuseDirectional.Select(r=>r.Key).SequenceEqual(new[]{8,7}))throw new Exception("Strict stable tie/input-order behavior failed.");
        slots=LegacyLightSelector.Select(new[]{new LegacyLightInput(0,1,new(1),new(1)),new LegacyLightInput(1,1,new(1),new(.5f)),new LegacyLightInput(2,1,new(1),new(2))},Vector3.Zero);if(slots.SpecularDirectional?.Key!=2||!slots.DiffuseDirectional.Select(r=>r.Key).SequenceEqual(new[]{1,0}))throw new Exception("Displacement-arrival diffuse tie order failed.");
        var scaled=new[]{new LegacyLightInput(0,1,new(1),new(1),1,.1f),new LegacyLightInput(1,1,new(.2f),new(.5f),10,4)};if(LegacyLightSelector.Select(scaled,Vector3.Zero).SpecularDirectional?.Key!=0||LegacyLightSelector.Select(scaled,Vector3.Zero,true).SpecularDirectional?.Key!=1)throw new Exception("Scaled score selection failed.");
        slots=LegacyLightSelector.Select(new[]{new LegacyLightInput(0,0,new(.2f,.3f,.4f),new(1)),new LegacyLightInput(1,2,new(1),new(1)),new LegacyLightInput(2,4,new(1),new(1))},new(.1f));if(Vector3.Distance(slots.Ambient,new(.3f,.4f,.5f))>1e-6||slots.SpecularDirectional is not null||slots.SpecularPoint is not null)throw new Exception("Ambient/type-ignore boundary failed.");
        if(LegacyLightSelector.Score(new(-1,0,0))!=.3f)throw new Exception("Absolute signed-RGB ranking failed.");
        HostAccountingScenarios.Reject<InvalidDataException>(()=>LegacyLightSelector.Select(rows.Concat(rows).ToArray(),Vector3.Zero));HostAccountingScenarios.Reject<InvalidDataException>(()=>LegacyLightSelector.Select(Enumerable.Range(0,33).Select(i=>rows[0] with{Key=i}).ToArray(),Vector3.Zero));HostAccountingScenarios.Reject<InvalidDataException>(()=>LegacyLightSelector.Select(new[]{rows[0] with{Specular=new(float.NaN)}},Vector3.Zero));
        for(int scenario=0;scenario<64;scenario++)
        {
            var generated=Enumerable.Range(0,24).Select(i=>new LegacyLightInput(i,i<9?1:i<20?3:i<22?0:i==22?2:4,new(((i*7+scenario)%5-2)/4f,((i+scenario)%3)/4f,((i*3+scenario)%4)/4f),new(((i+scenario)%4)/4f,((i*5+scenario)%3)/4f,((i+scenario)%5)/4f),i%3==0?.5f:2,i%4==0?.25f:1.5f)).ToArray();
            if(scenario%2==1)Array.Reverse(generated);bool useScaled=(scenario&2)!=0;var result=LegacyLightSelector.Select(generated,new(.1f,.2f,.3f),useScaled);float[] V(Vector3 v)=>new[]{v.X,v.Y,v.Z};
            Console.WriteLine("synthetic_legacy_light_slots="+JsonSerializer.Serialize(new{scenario,scaled=useScaled,inputs=generated.Select(r=>new{r.Key,r.RuntimeType,diffuse=V(r.Diffuse),specular=V(r.Specular),r.DiffuseScale,r.SpecularScale}),ambient=V(result.Ambient),specularDirectional=result.SpecularDirectional?.Key,diffuseDirectional=result.DiffuseDirectional.Select(r=>r.Key),specularPoint=result.SpecularPoint?.Key,diffusePoint=result.DiffusePoint.Select(r=>r.Key)}));
        }
        Console.WriteLine("PASS: bounded legacy one-specular/two-directional/four-point partition, stable ties/scaled/ambient/type-ignore/absolute-score and malformed gates");
    }
    internal static void Probe(InteriorSceneContentBundle content)
    {
        foreach(var cell in content.CellLights!.OrderBy(p=>p.Key))foreach(bool baked in new[]{false,true})foreach(bool scaled in new[]{false,true})
        {
            var inputs=LegacyLightSelector.CellInputs(cell.Value,baked);var slots=LegacyLightSelector.Select(inputs,Vector3.Zero,scaled);
            float[] V(Vector3 v)=>new[]{v.X,v.Y,v.Z};
            Console.WriteLine("original_legacy_light_slots="+JsonSerializer.Serialize(new{cell=cell.Key,baked,scaled,inputs=inputs.Select(r=>new{r.Key,r.RuntimeType,diffuse=V(r.Diffuse),specular=V(r.Specular),diffuseScore=LegacyLightSelector.Score(r.Diffuse),specularScore=LegacyLightSelector.Score(r.Specular)}),ambient=V(slots.Ambient),specularDirectional=slots.SpecularDirectional?.Key,diffuseDirectional=slots.DiffuseDirectional.Select(r=>r.Key),specularPoint=slots.SpecularPoint?.Key,diffusePoint=slots.DiffusePoint.Select(r=>r.Key)}));
        }
        Console.WriteLine("PASS: original authored-order cell runtime-mask fixture slot cases=12 original_live_visibility_order_unknown=true");
    }
}
