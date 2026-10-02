using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class PortalCellLightScenarios
{
    private static byte[] Fixture()
    {
        using var stream=new MemoryStream();using var writer=new BinaryWriter(stream);writer.Write(3);
        for(byte type=0;type<3;type++)
        {
            writer.Write(type);foreach(float value in new[]{.5f,2f,.3f,.4f,1f,.1f,.2f,.3f,1f,0f,0f,4f,0f,1f,0f,5f,0f,0f,1f,6f,1f,.25f,.5f})writer.Write(value);
        }
        return stream.ToArray();
    }
    private static PortalCell Cell(byte[] payload)=>PortalLayoutDecoder.Decode(PortalLayoutScenarios.Fixture()).Cells[0] with{Version="0005",Lights=new[]{IffDocument.Parse(MeshScenarios.Form("TEST",MeshScenarios.Chunk("LGHT",payload))).Root.Children[0]}};
    internal static void Run()
    {
        var raw=Fixture();var cell=Cell(raw);var lights=PortalCellLightsDecoder.Decode(cell);Array.Clear(raw);
        if(lights.Count!=3||lights[2].RuntimeLightType!=3||lights[1].DiffuseRgba!=new Vector4(2,.3f,.4f,.5f)||lights[1].SpecularRgba!=new Vector4(.1f,.2f,.3f,1)||lights[1].AuthoredTransform!=AppearanceTransform.Identity with{Translation=new(4,5,6)}||Vector3.Distance(lights[1].TowardLight,Vector3.UnitZ)>1e-6||lights[1].RuntimeTransform.Translation!=new Vector3(4,5,6)||lights[2].Attenuation!=new Vector3(1,.25f,.5f))throw new Exception("Cell-light owned fields/type/ARGB/yaw mapping mismatch.");
        void Reject(byte[] bytes)=>HostAccountingScenarios.Reject<InvalidDataException>(()=>PortalCellLightsDecoder.Decode(Cell(bytes)));
        var complete=Fixture();for(int length=0;length<complete.Length;length++)Reject(complete[..length]);Reject(complete.Concat(new byte[]{0}).ToArray());var invalid=(byte[])complete.Clone();invalid[4]=255;Reject(invalid);invalid=(byte[])complete.Clone();BitConverter.GetBytes(float.NaN).CopyTo(invalid,5);Reject(invalid);invalid=(byte[])complete.Clone();BitConverter.GetBytes(-1).CopyTo(invalid,0);Reject(invalid);
        HostAccountingScenarios.Reject<InvalidDataException>(()=>PortalCellLightsDecoder.Decode(Cell(complete),2));
        if(PortalCellLightsDecoder.Decode(cell with{Lights=Array.Empty<IffNode>()}).Count!=0)throw new Exception("Absent cell lights should remain absent.");
        Console.WriteLine("PASS: owned CELL LGHT count/type/HDR ARGB/transform/ordered attenuation, original pi yaw, absence and truncation/type/float/width/budget negative gates");
    }
    internal static void Probe(InteriorSceneContentBundle content)
    {
        CellLightingPreviewScenarios.Probe(content);
        StaticVertexColorScenarios.Probe(content);
        StaticRoomLightingScenarios.Probe(content);
        StaticNormalScenarios.Probe(content);
        LegacyLightSelectionScenarios.Probe(content);
        LegacyCellLightingScenarios.Probe(content);
        LegacyPointAttenuationScenarios.Probe(content);
        int total=0,specularDuplicates=0;
        foreach(int cell in content.Rooms!.CellAppearances.Keys.Order())
        {
            var lights=PortalCellLightsDecoder.Decode(content.Scene.Layout.Cells[cell]);
            if(content.CellLights is null||!content.CellLights[cell].SequenceEqual(lights))throw new Exception("Owned worker cell-light publication differs from original records.");total+=lights.Count;specularDuplicates+=lights.Count(l=>l.HasSpecularRgb);
            for(int index=0;index<lights.Count;index++)
            {
                var light=lights[index];Console.WriteLine("original_cell_light="+JsonSerializer.Serialize(new{cell,index,kind=(int)light.Kind,light.RuntimeLightType,diffuse=V(light.DiffuseRgba),specular=V(light.SpecularRgba),authored=T(light.AuthoredTransform),runtime=T(light.RuntimeTransform),toward=new[]{light.TowardLight.X,light.TowardLight.Y,light.TowardLight.Z},attenuation=new[]{light.Attenuation.X,light.Attenuation.Y,light.Attenuation.Z},light.HasSpecularRgb}));
            }
        }
        if(total!=19||specularDuplicates!=5)throw new Exception("Original selected cell-light fixture distribution changed.");
        Console.WriteLine($"PASS: original selected cell lights={total} directional=9 point=10 nonzero_specular_records={specularDuplicates} loader_records_owned=true");
    }
    private static float[] V(Vector4 v)=>new[]{v.X,v.Y,v.Z,v.W};
    private static float[] T(AppearanceTransform t)=>new[]{t.RowX.X,t.RowX.Y,t.RowX.Z,t.Translation.X,t.RowY.X,t.RowY.Y,t.RowY.Z,t.Translation.Y,t.RowZ.X,t.RowZ.Y,t.RowZ.Z,t.Translation.Z};
}
