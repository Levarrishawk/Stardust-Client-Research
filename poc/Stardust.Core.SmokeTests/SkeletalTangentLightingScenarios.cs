using System.Text.Json;
using Stardust.Core.Assets;

internal static class SkeletalTangentLightingScenarios
{
    internal static void Probe(SkeletalMaterialBundle source,Func<string,TreLookupResult> resolve)
    {
        if(source.TangentLighting is not { } bundle)return;
        Console.WriteLine("skeletal_tangent_vertex="+JsonSerializer.Serialize(new{path=SkeletalTangentLightingReader.VertexPath,sha256=bundle.VertexProgramSha256,functionsPath=SkeletalTangentLightingReader.FunctionsPath,functionsSha256=bundle.FunctionsSha256,vertexSources=bundle.VertexSources.Sha256}));
        foreach(var pair in bundle.Materials)Console.WriteLine("skeletal_tangent_material="+JsonSerializer.Serialize(new{path=pair.Key,pair.Value.PixelProgram,pair.Value.VertexProgram,pair.Value.TangentCoordinateSet}));
        foreach(string path in bundle.VertexSources.Sha256.Keys)
        {
            foreach(var kind in new[]{TreLookupKind.Missing,TreLookupKind.Masked})HostAccountingScenarios.Reject<InvalidDataException>(()=>SkeletalTangentLightingReader.Load(source,p=>p==path?new(kind):resolve(p)));
            var bytes=resolve(path).Payload!;
            foreach(int size in new[]{0,bytes.Length-1})HostAccountingScenarios.Reject<InvalidDataException>(()=>SkeletalTangentLightingReader.Load(source,p=>p==path?new(TreLookupKind.Present,bytes[..size]):resolve(p)));
            var changed=bytes.ToArray();changed[^1]^=1;HostAccountingScenarios.Reject<InvalidDataException>(()=>SkeletalTangentLightingReader.Load(source,p=>p==path?new(TreLookupKind.Present,changed):resolve(p)));
            HostAccountingScenarios.Reject<InvalidDataException>(()=>SkeletalTangentLightingReader.Load(source,p=>p==path?new(TreLookupKind.Present,new byte[65537]):resolve(p)));
        }
        var first=bundle.Materials.Keys.First();var shaders=source.Materials.ToDictionary();var shader=shaders[first];
        foreach(byte set in new byte[]{0,2})
        {
            var coordinates=shader.Parameters.CoordinateSets.ToDictionary();coordinates[0x444F5433]=set;
            shaders[first]=shader with{Parameters=shader.Parameters with{CoordinateSets=coordinates}};
            HostAccountingScenarios.Reject<InvalidDataException>(()=>SkeletalTangentLightingReader.Load(source with{Materials=shaders},resolve));
        }
        Console.WriteLine("PASS: original skeletal tangent vertex closure, shared source identities, missing/masked/truncated/changed/oversized source and coordinate-set gates");
    }
}
