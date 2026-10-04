using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;

namespace Stardust.Core.SmokeTests;
public sealed record TerrainHeightFilterRun(List<object> RawCases,List<object> QueryCases,int OwnershipChecks,int ArgumentNegatives,object Original);
public static class TerrainFilterHeightScenarios
{
    private static string Hash(byte[] bytes)=>Convert.ToHexString(SHA256.HashData(bytes));
    private static object Snapshot(TerrainFilterHeightInput input)=>new
    {
        input.Version,input.ClientVersionRecognized,input.HasDecodedFields,input.DecodedCopyBytes,
        Words=input.CopyDataWordBits(),Lower=input.Fields?.LowerHeightBits,Upper=input.Fields?.UpperHeightBits,
        Prefix=input.Fields?.LegacyPrefixBits,Selector=input.Fields?.FeatheringTypeBits,Amount=input.Fields?.FeatheringAmountBits,
        HeaderVersion=input.Header?.Version,Enabled=input.Header?.EnabledBits,
        NameSha=input.Header is null?null:Hash(input.Header.CopyNameBytes()),LegacySha=input.Header is null?null:Hash(input.Header.CopyLegacyBytes()),
        EncodedBytes=input.CopyEncodedBytes().Length,Sha256=Hash(input.CopyEncodedBytes())
    };
    public static TerrainHeightFilterRun Run(string fixtures,string root,string config)
    {
        using var document=JsonDocument.Parse(File.ReadAllText(fixtures));var rawCases=new List<object>();var queryCases=new List<object>();
        TerrainFilterHeightInput? valid=null,disabled=null;
        foreach(var f in document.RootElement.GetProperty("RawCases").EnumerateArray())
        {
            string name=f.GetProperty("Name").GetString()!;
            try
            { var input=TerrainFilterHeightInput.Decode(Convert.FromHexString(f.GetProperty("Hex").GetString()!),JsonSerializer.Deserialize<TerrainFilterHeightInputLimits>(f.GetProperty("Limits")));rawCases.Add(new{Name=name,Reject="None",Snapshot=Snapshot(input)}); }
            catch(ArgumentOutOfRangeException) { rawCases.Add(new{Name=name,Reject="Options"}); }
            catch(InvalidDataException) { rawCases.Add(new{Name=name,Reject="Input"}); }
        }
        foreach(var f in document.RootElement.GetProperty("QueryCases").EnumerateArray())
        {
            string name=f.GetProperty("Name").GetString()!;var input=TerrainFilterHeightInput.Decode(Convert.FromHexString(f.GetProperty("Hex").GetString()!));
            if(input.Version=="0002"&&input.Header?.IsEnabled==true&&input.Fields?.FeatheringAmountBits==0x3f000000)valid??=input;
            if(input.Header?.IsEnabled==false)disabled??=input;
            uint height=f.GetProperty("Height").GetUInt32(),selector=f.GetProperty("Selector").GetUInt32();
            var precision=(TerrainHeightArithmeticPrecision)f.GetProperty("Precision").GetInt32();var limits=JsonSerializer.Deserialize<TerrainHeightFilterLimits>(f.GetProperty("Limits"));
            try
            {
                TerrainHeightFilterQuery? q;uint? shaped;long work;
                if(f.GetProperty("Action").GetString()=="query")
                { q=TerrainFilterHeightInfluence.Query(input,height,precision,limits);shaped=TerrainFilterHeightInfluence.Shape(q,selector);work=1; }
                else { var result=TerrainFilterHeightInfluence.Evaluate(input,height,precision,limits);q=result.Query;shaped=result.InfluenceBits;work=result.QueryWork; }
                queryCases.Add(new{Name=name,Reject="None",Skipped=q is null,Influence=shaped,Query=q?.QueryProjectionBits,Branch=q is null?(int?)null:(int)q.Branch,Amount=q?.RuntimeAmountBits,Span=q?.FeatherSpanProjectionBits,Work=work});
            }
            catch(ArgumentOutOfRangeException) { queryCases.Add(new{Name=name,Reject="Options"}); }
            catch(InvalidDataException) { queryCases.Add(new{Name=name,Reject="Input"}); }
        }
        int checks=0,nulls=0;void Check(bool value) { checks++;if(!value)throw new InvalidOperationException("FHGT ownership/budget check failed."); }
        void Bad<T>(Action action) where T:Exception { try { action();throw new InvalidOperationException("Expected FHGT rejection."); }catch(T) { checks++; } }
        var p=TerrainHeightArithmeticPrecision.Binary64;
        var prior=TerrainFilterHeightInfluence.Query(valid!,0x3f800000,p);var before=Snapshot(valid!);
        byte[] source=valid!.CopyEncodedBytes();var independent=TerrainFilterHeightInput.Decode(source);var independentBefore=JsonSerializer.Serialize(Snapshot(independent));Array.Clear(source);
        Check(JsonSerializer.Serialize(Snapshot(independent))==independentBefore);
        Bad<InvalidDataException>(()=>TerrainFilterHeightInput.Decode(valid.CopyEncodedBytes().AsSpan(0,valid.CopyEncodedBytes().Length-1)));Array.Clear(valid.CopyDataWordBits());Array.Clear(valid.Header!.CopyNameBytes());
        Check(JsonSerializer.Serialize(Snapshot(valid))==JsonSerializer.Serialize(before));
        var changed=valid.Fields! with {LowerHeightBits=0x7fc12345};Check(changed.LowerHeightBits!=valid.Fields.LowerHeightBits);
        Check(typeof(TerrainHeightFilterQuery).GetProperties().All(x=>x.SetMethod is null));
        uint[] heights=[0x3f000000,0x3f800000];var batch=TerrainFilterHeightInfluence.EvaluateBatch(valid,heights,p,new(2,4));var values=batch.CopyInfluenceBits();
        Array.Clear(heights);var copy=batch.CopyInfluenceBits();Array.Clear(copy);Check(batch.CopyInfluenceBits().SequenceEqual(values));
        Check(batch.Results.Count==2&&batch.QueryWork==4&&batch.Results.All(r=>r.Query!.Input==valid));
        Bad<NotSupportedException>(()=>((IList<TerrainHeightFilterResult>)batch.Results)[0]=default);
        Bad<InvalidDataException>(()=>TerrainFilterHeightInfluence.EvaluateBatch(valid,new uint[]{0,0},p,new(1,4)));
        Bad<InvalidDataException>(()=>TerrainFilterHeightInfluence.EvaluateBatch(valid,new uint[]{0,0},p,new(2,3)));
        Bad<InvalidDataException>(()=>TerrainFilterHeightInfluence.Query(valid,0,p,new(1,0)));
        Bad<InvalidDataException>(()=>TerrainFilterHeightInfluence.Evaluate(valid,0,p,new(1,1)));
        Bad<InvalidDataException>(()=>TerrainFilterHeightInfluence.EvaluateBatch(valid,new uint[]{0x3f800000,0x7fc12345},p,new(2,4)));
        Check(batch.CopyInfluenceBits().SequenceEqual(values)&&TerrainFilterHeightInfluence.Shape(prior,0)==prior.QueryProjectionBits);
        var empty=TerrainFilterHeightInfluence.EvaluateBatch(valid,Array.Empty<uint>(),p,new(0,0));Check(empty.Results.Count==0&&empty.QueryWork==0);
        var skip=TerrainFilterHeightInfluence.EvaluateBatch(disabled!,new uint[]{0x7fc12345},p,new(1,0));Check(skip.Results[0].SkippedDisabled&&skip.QueryWork==0);
        Bad<ArgumentOutOfRangeException>(()=>TerrainFilterHeightInfluence.Query(valid,0,(TerrainHeightArithmeticPrecision)23));
        foreach(var l in new[]{new TerrainHeightFilterLimits(-1),new(1048577),new(MaximumQueryWork:-1),new(MaximumQueryWork:2097153)})
            Bad<ArgumentOutOfRangeException>(()=>TerrainFilterHeightInfluence.EvaluateBatch(valid,Array.Empty<uint>(),p,l));
        var exact=new TerrainFilterHeightInputLimits(MaximumInputBytes:valid.CopyEncodedBytes().Length,MaximumStringBytes:valid.Header.CopyNameBytes().Length,MaximumDecodedCopyBytes:valid.DecodedCopyBytes);
        Check(TerrainFilterHeightInput.Decode(valid.CopyEncodedBytes(),exact).DecodedCopyBytes==valid.DecodedCopyBytes);
        Bad<InvalidDataException>(()=>TerrainFilterHeightInput.Decode(valid.CopyEncodedBytes(),exact with {MaximumInputBytes=exact.MaximumInputBytes-1}));
        Bad<InvalidDataException>(()=>TerrainFilterHeightInput.Decode(valid.CopyEncodedBytes(),exact with {MaximumStringBytes=exact.MaximumStringBytes-1}));
        Bad<InvalidDataException>(()=>TerrainFilterHeightInput.Decode(valid.CopyEncodedBytes(),exact with {MaximumDecodedCopyBytes=exact.MaximumDecodedCopyBytes-1}));
        Check(TerrainFilterHeightInfluence.Shape(prior,0xffffffff)==0);
        void Null(Action action) { try { action();throw new InvalidOperationException("Expected null rejection."); }catch(ArgumentNullException) { nulls++; } }
        Null(()=>TerrainFilterHeightInput.Decode((TerrainGeneratorSection)null!));Null(()=>TerrainFilterHeightInfluence.Query(null!,0,p));
        Null(()=>TerrainFilterHeightInfluence.Shape(null!,0));Null(()=>TerrainFilterHeightInfluence.Evaluate(null!,0,p));Null(()=>TerrainFilterHeightInfluence.EvaluateBatch(null!,Array.Empty<uint>(),p));
        string configHash=Hash(File.ReadAllBytes(config));Check(configHash=="22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524");
        byte[] terrain;int? priority;using(var cfg=File.OpenText(config))using(var overlay=ConfiguredTreOverlay.Open(root,cfg))
        { var found=overlay.Lookup("terrain/tatooine.trn");Check(found.Kind==TreLookupKind.Present&&found.Payload is not null);terrain=found.Payload!;priority=found.Priority; }
        string sourceHash=Hash(terrain);Check(priority==42&&terrain.Length==1188446&&sourceHash=="6039D0C15F9CCB518D72AE01A1ABDF41FCFF2153B5CBF7EB3BA6E6C634A3A427");
        var ptat=ProceduralTerrainInput.Decode(terrain,new(MaximumInputBytes:2097152,MaximumNodes:20000));byte[] generator=ptat.CopyGeneratorForm();string generatorHash=Hash(generator);
        Check(generator.Length==139608&&generatorHash=="B623B789E5F8486414C1D68CD341005E7E25FD97AF30567E64E7AC848DEB6C32");
        var rules=TerrainGeneratorInput.Decode(generator).Rules.Where(r=>r.Section.FormType=="FHGT").ToArray();object? first=null;
        if(rules.Length>0) { var rule=rules[0];first=new {rule.LayerIndex,rule.ChildOrdinal,rule.Section.Offset,Snapshot=Snapshot(TerrainFilterHeightInput.Decode(rule.Section))}; }
        object original=new {ConfigSha256=configHash,SourceSha256=sourceHash,GeneratorSha256=generatorHash,Priority=priority,Count=rules.Length,First=first};
        return new(rawCases,queryCases,checks,nulls,original);
    }
}
