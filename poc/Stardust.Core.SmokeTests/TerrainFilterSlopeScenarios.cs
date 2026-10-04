using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;

namespace Stardust.Core.SmokeTests;
public sealed record TerrainSlopeFilterRun(List<object> RawCases,int OwnershipChecks,int ArgumentNegatives,object Original);
public static class TerrainFilterSlopeScenarios
{
    private static string Hash(byte[] bytes)=>Convert.ToHexString(SHA256.HashData(bytes));
    private static object Snapshot(TerrainFilterSlopeInput input)=>new
    {
        input.Version,input.ClientVersionRecognized,input.HasDecodedFields,input.DecodedCopyBytes,
        Words=input.CopyDataWordBits(),First=input.Fields?.FirstThresholdBits,Second=input.Fields?.SecondThresholdBits,
        Prefix=input.Fields?.LegacyPrefixBits,Selector=input.Fields?.FeatheringTypeBits,Amount=input.Fields?.FeatheringAmountBits,
        HeaderVersion=input.Header?.Version,Enabled=input.Header?.EnabledBits,
        NameSha=input.Header is null?null:Hash(input.Header.CopyNameBytes()),LegacySha=input.Header is null?null:Hash(input.Header.CopyLegacyBytes()),
        EncodedBytes=input.CopyEncodedBytes().Length,Sha256=Hash(input.CopyEncodedBytes())
    };
    public static TerrainSlopeFilterRun Run(string fixtures,string root,string config)
    {
        using var document=JsonDocument.Parse(File.ReadAllText(fixtures));var cases=new List<object>();TerrainFilterSlopeInput? valid=null;
        foreach(var f in document.RootElement.GetProperty("RawCases").EnumerateArray())
        {
            string name=f.GetProperty("Name").GetString()!;
            try
            {
                var input=TerrainFilterSlopeInput.Decode(Convert.FromHexString(f.GetProperty("Hex").GetString()!),JsonSerializer.Deserialize<TerrainFilterSlopeInputLimits>(f.GetProperty("Limits")));
                if(input.HasDecodedFields&&input.Header is not null)valid??=input;
                cases.Add(new{Name=name,Reject="None",Snapshot=Snapshot(input)});
            }
            catch(ArgumentOutOfRangeException) { cases.Add(new{Name=name,Reject="Options"}); }
            catch(InvalidDataException) { cases.Add(new{Name=name,Reject="Input"}); }
        }
        int checks=0,nulls=0;void Check(bool condition) { checks++;if(!condition)throw new InvalidOperationException("FSLP ownership/budget check failed."); }
        void Bad<T>(Action action) where T:Exception { try { action();throw new InvalidOperationException("Expected FSLP rejection."); }catch(T) { checks++; } }
        var prior=JsonSerializer.Serialize(Snapshot(valid!));byte[] bytes=valid!.CopyEncodedBytes();var separate=TerrainFilterSlopeInput.Decode(bytes);
        Array.Clear(bytes);Check(JsonSerializer.Serialize(Snapshot(separate))==prior);
        Array.Clear(valid.CopyDataWordBits());Array.Clear(valid.Header!.CopyNameBytes());Array.Clear(valid.Header.CopyLegacyBytes());
        Check(JsonSerializer.Serialize(Snapshot(valid))==prior);
        var fields=valid.Fields! with {FirstThresholdBits=0x7fc12345};Check(fields.FirstThresholdBits!=valid.Fields!.FirstThresholdBits);
        Check(typeof(TerrainFilterSlopeInput).GetProperties().All(p=>p.SetMethod is null));
        Bad<InvalidDataException>(()=>TerrainFilterSlopeInput.Decode(valid.CopyEncodedBytes().AsSpan(0,valid.CopyEncodedBytes().Length-1)));
        Check(JsonSerializer.Serialize(Snapshot(valid))==prior);
        var exact=new TerrainFilterSlopeInputLimits(MaximumInputBytes:valid.CopyEncodedBytes().Length,MaximumStringBytes:valid.Header.CopyNameBytes().Length,MaximumDecodedCopyBytes:valid.DecodedCopyBytes);
        Check(TerrainFilterSlopeInput.Decode(valid.CopyEncodedBytes(),exact).DecodedCopyBytes==valid.DecodedCopyBytes);
        Bad<InvalidDataException>(()=>TerrainFilterSlopeInput.Decode(valid.CopyEncodedBytes(),exact with {MaximumInputBytes=exact.MaximumInputBytes-1}));
        Bad<InvalidDataException>(()=>TerrainFilterSlopeInput.Decode(valid.CopyEncodedBytes(),exact with {MaximumStringBytes=exact.MaximumStringBytes-1}));
        Bad<InvalidDataException>(()=>TerrainFilterSlopeInput.Decode(valid.CopyEncodedBytes(),exact with {MaximumDecodedCopyBytes=exact.MaximumDecodedCopyBytes-1}));
        foreach(var l in new[]{new TerrainFilterSlopeInputLimits(MaximumInputBytes:23),new(MaximumNodes:0),new(MaximumDepth:0),new(MaximumStringBytes:-1),new(MaximumDecodedCopyBytes:-1)})
            Bad<ArgumentOutOfRangeException>(()=>TerrainFilterSlopeInput.Decode(valid.CopyEncodedBytes(),l));
        try { TerrainFilterSlopeInput.Decode((TerrainGeneratorSection)null!);throw new InvalidOperationException("Expected null section rejection."); }catch(ArgumentNullException) { nulls++; }
        string configHash=Hash(File.ReadAllBytes(config));Check(configHash=="22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524");
        byte[] terrain;int? priority;using(var cfg=File.OpenText(config))using(var overlay=ConfiguredTreOverlay.Open(root,cfg))
        { var found=overlay.Lookup("terrain/tatooine.trn");Check(found.Kind==TreLookupKind.Present&&found.Payload is not null);terrain=found.Payload!;priority=found.Priority; }
        string sourceHash=Hash(terrain);Check(priority==42&&terrain.Length==1188446&&sourceHash=="6039D0C15F9CCB518D72AE01A1ABDF41FCFF2153B5CBF7EB3BA6E6C634A3A427");
        var ptat=ProceduralTerrainInput.Decode(terrain,new(MaximumInputBytes:2097152,MaximumNodes:20000));byte[] generator=ptat.CopyGeneratorForm();string generatorHash=Hash(generator);
        Check(generator.Length==139608&&generatorHash=="B623B789E5F8486414C1D68CD341005E7E25FD97AF30567E64E7AC848DEB6C32");
        var rules=TerrainGeneratorInput.Decode(generator).Rules.Where(r=>r.Section.FormType=="FSLP").ToArray();object? first=null;
        if(rules.Length>0) { var rule=rules[0];first=new {rule.LayerIndex,rule.ChildOrdinal,rule.Section.Offset,Snapshot=Snapshot(TerrainFilterSlopeInput.Decode(rule.Section))}; }
        object original=new {ConfigSha256=configHash,SourceSha256=sourceHash,GeneratorSha256=generatorHash,Priority=priority,Count=rules.Length,First=first};
        return new(cases,checks,nulls,original);
    }
}
