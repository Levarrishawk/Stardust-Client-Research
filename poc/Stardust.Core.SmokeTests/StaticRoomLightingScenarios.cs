using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class StaticRoomLightingScenarios
{
    internal static void Run()
    {
        var main=new Vector4(.2f,.4f,.8f,.5f);var diffuse=new Vector3(.25f,.5f,1.5f);var specular=new Vector3(.2f,.3f,.4f);
        var emissive=StaticRoomPixelEvaluator.Evaluate(StaticRoomPixelKind.EmissivePs20,main,.1f,diffuse,specular,.7f);
        if(Vector4.Distance(emissive,new(.125f,.3f,1,.35f))>1e-6)throw new Exception("Hand-computed baked/emissive blend failed.");
        var spec=StaticRoomPixelEvaluator.Evaluate(StaticRoomPixelKind.SpecularPs11,main,.25f,diffuse,specular,.7f,opacity:.6f);
        if(Vector4.Distance(spec,new(.1f,.275f,1.3f,.6f))>1e-6)throw new Exception("Hand-computed separate specular mask failed.");
        HostAccountingScenarios.Reject<InvalidDataException>(()=>StaticRoomPixelEvaluator.Evaluate((StaticRoomPixelKind)99,main,.1f,diffuse,specular,.7f));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>StaticRoomPixelEvaluator.Evaluate(StaticRoomPixelKind.EmissivePs20,main with{W=2},.1f,diffuse,specular,.7f));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>StaticRoomPixelEvaluator.Evaluate(StaticRoomPixelKind.EmissivePs20,main,.1f,new(float.NaN),specular,.7f));
        Console.WriteLine("PASS: static room separate-specular/emissive hand arithmetic, HDR preservation and kind/mask/finite negative gates");
    }
    internal static void Probe(InteriorSceneContentBundle content)
    {
        var bundle=content.RoomLighting!;int cases=0;
        foreach(var kind in new[]{TreLookupKind.Missing,TreLookupKind.Masked})HostAccountingScenarios.Reject<InvalidDataException>(()=>StaticRoomLightingReader.Load(_=>new(kind),content.Materials!.Meshes));
        HostAccountingScenarios.Reject<InvalidDataException>(()=>StaticRoomLightingReader.Load(_=>new(TreLookupKind.Present,new byte[65537]),content.Materials!.Meshes));
        float[] V(Vector4 v)=>new[]{v.X,v.Y,v.Z,v.W};float[] V3(Vector3 v)=>new[]{v.X,v.Y,v.Z};
        foreach(var pair in bundle.Materials.OrderBy(p=>p.Key,StringComparer.Ordinal))
        {
            var m=pair.Value;Console.WriteLine("original_room_material="+JsonSerializer.Serialize(new{path=pair.Key,m.PixelProgram,m.VertexProgram,kind=(int)m.Kind,m.MainTexture,m.SecondaryTexture,m.TextureFactorTag,factor=m.TextureFactor is Vector4 f?V(f):null,ambient=V(m.Material.AmbientRgba),diffuse=V(m.Material.DiffuseRgba),emissive=V(m.Material.EmissiveRgba),specular=V(m.Material.SpecularRgba),power=m.Material.SpecularPower}));
        }
        foreach(var pair in bundle.Programs.OrderBy(p=>p.Key,StringComparer.Ordinal))
        {
            var program=pair.Value;Console.WriteLine("original_room_pixel_program="+JsonSerializer.Serialize(new{path=pair.Key,kind=(int)program.Kind,program.InstructionsSha256}));
            var parts=program.Metadata.Children[0].Children;var fixture=MeshScenarios.Form("PSHP",MeshScenarios.Form("0000",MeshScenarios.Chunk("PSRC",System.Text.Encoding.ASCII.GetBytes("synthetic\0")),MeshScenarios.Chunk("PEXE",parts[1].Payload.ToArray())));
            for(int i=0;i<fixture.Length;i++)HostAccountingScenarios.Reject<InvalidDataException>(()=>StaticRoomPixelProgramDecoder.Decode(fixture.AsSpan(0,i)));
            var changed=parts[1].Payload.ToArray();changed[^5]^=1;var invalid=MeshScenarios.Form("PSHP",MeshScenarios.Form("0000",MeshScenarios.Chunk("PSRC",System.Text.Encoding.ASCII.GetBytes("synthetic\0")),MeshScenarios.Chunk("PEXE",changed)));
            HostAccountingScenarios.Reject<InvalidDataException>(()=>StaticRoomPixelProgramDecoder.Decode(invalid));
            foreach(var color in new[]{new Vector3(.2f,.4f,.8f),new Vector3(.7f,.1f,.3f)})foreach(float mask in new[]{0f,.25f,.75f,1f})foreach(float secondary in new[]{.1f,.8f})foreach(bool bloom in new[]{false,true})foreach(bool fade in new[]{false,true})
            {
                var main=new Vector4(color,mask);var diffuse=new Vector3(.25f,.5f,1.5f);var specular=new Vector3(.2f,.3f,.4f);var value=StaticRoomPixelEvaluator.Evaluate(program.Kind,main,secondary,diffuse,specular,.7f,bloom,fade,.6f);
                Console.WriteLine("original_room_pixel_case="+JsonSerializer.Serialize(new{path=pair.Key,kind=(int)program.Kind,main=V(main),secondary,diffuse=V3(diffuse),specular=V3(specular),strength=.7f,bloom,fade,opacity=.6f,result=V(value)}));cases++;
            }
        }
        if(bundle.Programs.Count!=3||cases!=192)throw new Exception("Original room pixel family fixture count changed.");
        Console.WriteLine($"PASS: original selected room materials={bundle.Materials.Count} pixel_programs={bundle.Programs.Count} compiled_pixel_cases={cases} truncation_instruction_identity_gates=true");
    }
}
