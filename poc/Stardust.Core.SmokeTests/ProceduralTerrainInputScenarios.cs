using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;

internal static class ProceduralTerrainInputScenarios
{
    private const string Config="22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524";
    private const string Source="6039D0C15F9CCB518D72AE01A1ABDF41FCFF2153B5CBF7EB3BA6E6C634A3A427";
    private static string Hash(ReadOnlySpan<byte> b)=>Convert.ToHexString(SHA256.HashData(b));
    private static void Need(bool value,string reason) { if(!value)throw new Exception(reason); }
    private static object Metadata(ProceduralTerrainInput p)=>new {
        p.Version,Prefix=new[]{p.WorldSizeBits,p.ChunkSizeBits,p.TilesPerChunk,p.GlobalWaterFlagBits,p.GlobalWaterHeightBits,p.GlobalWaterShaderSizeBits,p.EnvironmentCycleDurationBits},
        MetadataWords=Enumerable.Range(0,20).Select(p.GetMetadataWordBits).ToArray(),TerrainNameHex=Convert.ToHexString(p.CopyTerrainNameBytes()),WaterShaderHex=Convert.ToHexString(p.CopyWaterShaderBytes()),
        p.DataBytes,DataSha256=Hash(p.CopyDataRecord()),p.GeneratorBytes,GeneratorSha256=Hash(p.CopyGeneratorForm()),Version13BranchSha256=Hash(p.CopyVersion13Branch()),p.UnknownTailBytes,UnknownTailSha256=Hash(p.CopyUnknownDataTail()),
        MapVersion=p.Maps.Version,MapData=p.Maps.IsEmpty?Array.Empty<uint>():new[]{p.Maps.WorldSizeBits,p.Maps.MetersPerBitBits,p.Maps.BytesPerRow,p.Maps.BitDimension},WmapSha256=Hash(p.Maps.CopyMask(TerrainMapMaskKind.Wmap)),SmapSha256=Hash(p.Maps.CopyMask(TerrainMapMaskKind.Smap))};
    internal static void Run(string[] args)
    {
        Need(args.Length==3,"Explicit asset root/config/synthetic fixtures required.");
        Need(Hash(File.ReadAllBytes(args[1]))==Config,"Configuration pin.");
        using var fixtures=JsonDocument.Parse(File.ReadAllBytes(args[2]));var cases=new List<object>();
        foreach(var f in fixtures.RootElement.EnumerateArray())
        {
            string name=f.GetProperty("Name").GetString()!,expected=f.GetProperty("Reject").GetString()!;
            byte[] bytes=Convert.FromHexString(f.GetProperty("Hex").GetString()!);var limits=JsonSerializer.Deserialize<ProceduralTerrainInputLimits>(f.GetProperty("Limits"))!;
            string actual="None";object? metadata=null;
            try { metadata=Metadata(ProceduralTerrainInput.Decode(bytes,limits)); }
            catch(InvalidDataException) { actual="Input"; }
            catch(ArgumentOutOfRangeException) { actual="Options"; }
            Need(actual==expected,"Fixture mismatch: "+name+" expected "+expected+" got "+actual);cases.Add(new {Name=name,Reject=actual,Metadata=metadata});
        }
        var raw=Convert.FromHexString(fixtures.RootElement[1].GetProperty("Hex").GetString()!);var input=ProceduralTerrainInput.Decode(raw);
        Need(input.UsesGlobalWater&&input.GlobalWaterFlagBits==7&&input.WorldSize==16384&&input.ChunkSize==32&&input.EnvironmentCycleDuration==11280,"Raw flag normalization and typed prefix accessors.");
        string before=JsonSerializer.Serialize(Metadata(input));int ownership=0;
        Array.Clear(raw);Need(before==JsonSerializer.Serialize(Metadata(input)),"Source snapshot ownership.");ownership++;
        foreach(var copy in new[]{input.CopyDataRecord(),input.CopyGeneratorForm(),input.CopyTerrainNameBytes(),input.CopyWaterShaderBytes(),input.CopyVersion13Branch(),input.Maps.CopyMask(TerrainMapMaskKind.Wmap)})
        { Array.Clear(copy);Need(before==JsonSerializer.Serialize(Metadata(input)),"Inspection clone ownership.");ownership++; }
        var tailInput=ProceduralTerrainInput.Decode(Convert.FromHexString(fixtures.RootElement[2].GetProperty("Hex").GetString()!));var tail=tailInput.CopyUnknownDataTail();Array.Clear(tail);Need(Convert.ToHexString(tailInput.CopyUnknownDataTail())=="00FF7F","Unknown tail owned.");ownership++;
        Need(!ReferenceEquals(input.CopyGeneratorForm(),input.CopyGeneratorForm())&&typeof(ProceduralTerrainInput).GetProperties().All(p=>p.SetMethod is null),"Separate clones/get-only properties.");ownership++;
        int accessorRejections=0;
        foreach(int index in new[]{-1,20}) { try { input.GetMetadataWordBits(index);throw new Exception("Unbounded word access."); }catch(ArgumentOutOfRangeException) { accessorRejections++; } }
        byte[] terrain;int? priority;
        using(var cfg=File.OpenText(args[1]))using(var overlay=ConfiguredTreOverlay.Open(args[0],cfg))
        { var lookup=overlay.Lookup("terrain/tatooine.trn");Need(lookup.Kind==TreLookupKind.Present&&lookup.Payload is not null,"Original sample available.");terrain=lookup.Payload!;priority=lookup.Priority; }
        Need(priority==42&&terrain.Length==1188446&&Hash(terrain)==Source,"Original full identity pin.");
        var tree=IffDocument.Parse(terrain,new(2097152,20000,32)).Root;var v=tree.Children.Single();var d=v.Children[0];var g=v.Children[1];var m=v.Children[2];
        var originalInput=ProceduralTerrainInput.Decode(terrain,new(MaximumInputBytes:2097152,MaximumNodes:20000));
        Need(originalInput.WorldSize==16384&&originalInput.ChunkSize==8&&originalInput.TilesPerChunk==2&&!originalInput.UsesGlobalWater&&originalInput.EnvironmentCycleDuration==11280,"Original typed prefix metadata.");
        int dataOffset=d.Offset,generatorOffset=g.Offset,mapOffset=m.Offset,mapBytes=m.Payload.Length+12;string mapHash=Hash(terrain.AsSpan(mapOffset,mapBytes));Array.Clear(terrain);
        var original=new {Root=Path.GetFullPath(args[0]).Replace('\\','/'),Configuration=Path.GetFullPath(args[1]).Replace('\\','/'),ConfigSha256=Config,Path="terrain/tatooine.trn",Priority=priority,SourceBytes=1188446,SourceSha256=Source,Metadata=Metadata(originalInput),DataOffset=dataOffset,GeneratorOffset=generatorOffset,MapOffset=mapOffset,MapBytes=mapBytes,MapSha256=mapHash};
        Console.WriteLine("ptat_input="+JsonSerializer.Serialize(new {Cases=cases,OwnershipChecks=ownership,AccessorRejections=accessorRejections,Original=original}));
        Console.WriteLine("PASS: owned bounded PTAT envelope and DATA, opaque TGEN, composed masks, original identity and synthetic negatives.");
    }
}
