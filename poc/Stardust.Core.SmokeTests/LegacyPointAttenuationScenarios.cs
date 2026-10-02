using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class LegacyPointAttenuationScenarios
{
    internal static void Run()
    {
        void Near(float a,float b){if(Math.Abs(a-b)>Math.Max(1e-6f,Math.Abs(b)*2e-7f))throw new Exception("Source attenuation hand equation differs.");}
        Near(LegacyPointAttenuation.Evaluate(new(1,0,0),4),1);Near(LegacyPointAttenuation.Evaluate(new(0,.25f,0),4),2);Near(LegacyPointAttenuation.Evaluate(new(0,0,1),.0001f),10000);
        Near(LegacyPointAttenuation.Evaluate(new(.25f,.5f,.75f),4),1/4.25f);
        foreach(float distance in new[]{0f,-1,float.NaN,float.PositiveInfinity})HostAccountingScenarios.Reject<InvalidDataException>(()=>LegacyPointAttenuation.Evaluate(Vector3.One,distance));
        foreach(var coefficients in new[]{Vector3.Zero,new Vector3(-1,0,0),new Vector3(float.NaN),new Vector3(float.MaxValue)})HostAccountingScenarios.Reject<InvalidDataException>(()=>LegacyPointAttenuation.Evaluate(coefficients,4));
        Console.WriteLine("PASS: source positive point attenuation hand/uncapped coefficient order and singular/nonfinite gates");
    }
    internal static void Probe(InteriorSceneContentBundle content)
    {
        int original=0,synthetic=0;float maximumDifference=0;
        foreach(var cell in content.CellLights!)for(int index=0;index<cell.Value.Count;index++)
        {
            var light=cell.Value[index];if(light.Kind!=PortalCellLightKind.Point)continue;
            foreach(float distance in new[]{.01f,.1f,1f,5f,20f,100f})
            {
                float distanceSquared=distance*distance,value=LegacyPointAttenuation.Evaluate(light.Attenuation,distanceSquared);
                Console.WriteLine("original_legacy_point_attenuation="+JsonSerializer.Serialize(new{cell=cell.Key,index,distanceSquared,value}));original++;
            }
        }
        foreach(var coefficients in new[]{new Vector3(1,0,0),new(0,.25f,0),new(0,0,1),new(.25f,.5f,.75f),new(.001f,0,0),new(0,1,2)})foreach(float distance in new[]{.01f,.05f,.1f,.5f,1f,2f,5f,10f})
        {
            float distanceSquared=distance*distance,value=LegacyPointAttenuation.Evaluate(coefficients,distanceSquared),capped=1/Math.Max(.01f,coefficients.X+coefficients.Y*MathF.Sqrt(distanceSquared)+coefficients.Z*distanceSquared);
            maximumDifference=Math.Max(maximumDifference,Math.Abs(value-capped));
            Console.WriteLine("synthetic_legacy_point_attenuation="+JsonSerializer.Serialize(new{coefficients=new[]{coefficients.X,coefficients.Y,coefficients.Z},distanceSquared,value}));synthetic++;
        }
        if(original!=60||synthetic!=48||maximumDifference<9000)throw new Exception("Source point attenuation fixture coverage changed.");
        Console.WriteLine($"PASS: source point attenuation original_coefficients={original} synthetic={synthetic} modern_cap_difference={maximumDifference}");
    }
}
