using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;

namespace Stardust.Core.SmokeTests;
public sealed record TerrainFractalFilterRun(List<object> RawCases,List<object> QueryCases,int OwnershipChecks,int ArgumentNegatives,object Original);
public static class TerrainFilterFractalScenarios
{
    private static string Hash(byte[] bytes)=>Convert.ToHexString(SHA256.HashData(bytes));
    private static object Snapshot(TerrainFilterFractalInput input)=>new
    {
        input.Version,input.ClientVersionRecognized,input.HasDecodedFields,input.DecodedCopyBytes,
        Words=input.CopyDataWordBits(),Lower=input.Fields?.LowerSampleBits,Upper=input.Fields?.UpperSampleBits,
        Family=input.Fields?.FamilyIdBits,Scale=input.Fields?.SampleScaleBits,Selector=input.Fields?.FeatheringTypeBits,Amount=input.Fields?.FeatheringAmountBits,
        HeaderVersion=input.Header?.Version,Enabled=input.Header?.EnabledBits,
        NameSha=input.Header is null?null:Hash(input.Header.CopyNameBytes()),LegacySha=input.Header is null?null:Hash(input.Header.CopyLegacyBytes()),
        EncodedBytes=input.CopyEncodedBytes().Length,Sha256=Hash(input.CopyEncodedBytes())
    };
    public static TerrainFractalFilterRun Run(string fixtures)
    {
        using var document=JsonDocument.Parse(File.ReadAllText(fixtures));var rawCases=new List<object>();var queryCases=new List<object>();
        TerrainFilterFractalInput? valid=null,disabled=null;
        foreach(var f in document.RootElement.GetProperty("RawCases").EnumerateArray())
        {
            string name=f.GetProperty("Name").GetString()!;
            try
            { var input=TerrainFilterFractalInput.Decode(Convert.FromHexString(f.GetProperty("Hex").GetString()!),JsonSerializer.Deserialize<TerrainFilterFractalInputLimits>(f.GetProperty("Limits")));rawCases.Add(new{Name=name,Reject="None",Snapshot=Snapshot(input)}); }
            catch(ArgumentOutOfRangeException) { rawCases.Add(new{Name=name,Reject="Options"}); }
            catch(InvalidDataException) { rawCases.Add(new{Name=name,Reject="Input"}); }
        }
        foreach(var f in document.RootElement.GetProperty("QueryCases").EnumerateArray())
        {
            string name=f.GetProperty("Name").GetString()!;var input=TerrainFilterFractalInput.Decode(Convert.FromHexString(f.GetProperty("Hex").GetString()!));
            if(input.Version=="0005"&&input.Header?.IsEnabled==true&&input.Fields?.FeatheringAmountBits==0x3f000000&&input.Fields.HasFiniteRange&&input.Fields.SampleScaleBits==0x3f800000)valid??=input;
            if(input.Header?.IsEnabled==false)disabled??=input;
            uint height=f.GetProperty("Sample").GetUInt32(),selector=f.GetProperty("Selector").GetUInt32();
            var precision=(TerrainHeightArithmeticPrecision)f.GetProperty("Precision").GetInt32();var limits=JsonSerializer.Deserialize<TerrainFractalFilterLimits>(f.GetProperty("Limits"));
            try
            {
                TerrainFractalFilterQuery? q;uint? shaped;long work;
                if(f.GetProperty("Action").GetString()=="query")
                { q=TerrainFilterFractalInfluence.Query(input,height,precision,limits);shaped=TerrainFilterFractalInfluence.Shape(q,selector);work=2; }
                else { var result=TerrainFilterFractalInfluence.Evaluate(input,height,precision,limits);q=result.Query;shaped=result.InfluenceBits;work=result.QueryWork; }
                uint? carrierBits=null,layerBits=null;int? sourceType=null;
                if(q is not null){var cp=(TerrainHeightArithmeticPrecision)f.GetProperty("Combine").GetInt32();var carrier=TerrainLayerInfluenceQuery.FromFractalFilter(q);carrierBits=carrier.ShapeProjectionBits(selector,cp);sourceType=(int)carrier.Source;var sample=TerrainLayerInfluenceSample.Create(0x3f800000,Array.Empty<TerrainLayerInfluenceEntry>(),new[]{new TerrainLayerInfluenceEntry(true,selector,carrier)},1,1,false,false,cp,cp);layerBits=TerrainLayerInfluence.Evaluate(sample).LocalInfluenceBits;}
                queryCases.Add(new{Name=name,Reject="None",CarrierBits=carrierBits,LayerBits=layerBits,CarrierSource=sourceType,Skipped=q is null,Influence=shaped,Query=q?.QueryProjectionBits,Branch=q is null?(int?)null:(int)q.Branch,Scaled=q?.ScaledSampleBits,Amount=q?.RuntimeAmountBits,Span=q?.FeatherSpanProjectionBits,Work=work});
            }
            catch(ArgumentOutOfRangeException) { queryCases.Add(new{Name=name,Reject="Options"}); }
            catch(InvalidDataException) { queryCases.Add(new{Name=name,Reject="Input"}); }
        }
        int checks=0,nulls=0;void Check(bool value) { checks++;if(!value)throw new InvalidOperationException("FFRA ownership/budget check failed."); }
        void Bad<T>(Action action) where T:Exception { try { action();throw new InvalidOperationException("Expected FFRA rejection."); }catch(T) { checks++; } }
        var p=TerrainHeightArithmeticPrecision.Binary64;
        var prior=TerrainFilterFractalInfluence.Query(valid!,0x3f800000,p);var before=Snapshot(valid!);
        byte[] source=valid!.CopyEncodedBytes();var independent=TerrainFilterFractalInput.Decode(source);var independentBefore=JsonSerializer.Serialize(Snapshot(independent));Array.Clear(source);
        Check(JsonSerializer.Serialize(Snapshot(independent))==independentBefore);
        Bad<InvalidDataException>(()=>TerrainFilterFractalInput.Decode(valid.CopyEncodedBytes().AsSpan(0,valid.CopyEncodedBytes().Length-1)));Array.Clear(valid.CopyDataWordBits());Array.Clear(valid.Header!.CopyNameBytes());
        Check(JsonSerializer.Serialize(Snapshot(valid))==JsonSerializer.Serialize(before));
        var changed=valid.Fields! with {LowerSampleBits=0x7fc12345};Check(changed.LowerSampleBits!=valid.Fields.LowerSampleBits);
        Check(typeof(TerrainFractalFilterQuery).GetProperties().All(x=>x.SetMethod is null));
        uint[] heights=[0x3f000000,0x3f800000];var batch=TerrainFilterFractalInfluence.EvaluateBatch(valid,heights,p,new(2,6));var values=batch.CopyInfluenceBits();
        Array.Clear(heights);var copy=batch.CopyInfluenceBits();Array.Clear(copy);Check(batch.CopyInfluenceBits().SequenceEqual(values));
        Check(batch.Results.Count==2&&batch.QueryWork==6&&batch.Results.All(r=>r.Query!.Input==valid));
        Bad<NotSupportedException>(()=>((IList<TerrainFractalFilterResult>)batch.Results)[0]=default);
        Bad<InvalidDataException>(()=>TerrainFilterFractalInfluence.EvaluateBatch(valid,new uint[]{0,0},p,new(1,6)));
        Bad<InvalidDataException>(()=>TerrainFilterFractalInfluence.EvaluateBatch(valid,new uint[]{0,0},p,new(2,5)));
        Bad<InvalidDataException>(()=>TerrainFilterFractalInfluence.Query(valid,0,p,new(1,0)));
        Check(TerrainFilterFractalInfluence.Query(valid,0,p,new(1,2)).QueryProjectionBits==0);
        Bad<InvalidDataException>(()=>TerrainFilterFractalInfluence.Query(valid,0,p,new(1,1)));
        Bad<InvalidDataException>(()=>TerrainFilterFractalInfluence.Evaluate(valid,0,p,new(1,1)));
        Bad<InvalidDataException>(()=>TerrainFilterFractalInfluence.EvaluateBatch(valid,new uint[]{0x3f800000,0x7fc12345},p,new(2,6)));
        Check(batch.CopyInfluenceBits().SequenceEqual(values)&&TerrainFilterFractalInfluence.Shape(prior,0)==prior.QueryProjectionBits);
        var empty=TerrainFilterFractalInfluence.EvaluateBatch(valid,Array.Empty<uint>(),p,new(0,0));Check(empty.Results.Count==0&&empty.QueryWork==0);
        var skip=TerrainFilterFractalInfluence.EvaluateBatch(disabled!,new uint[]{0x7fc12345},p,new(1,0));Check(skip.Results[0].SkippedDisabled&&skip.QueryWork==0);
        Bad<ArgumentOutOfRangeException>(()=>TerrainFilterFractalInfluence.Query(valid,0,(TerrainHeightArithmeticPrecision)23));
        foreach(var l in new[]{new TerrainFractalFilterLimits(-1),new(1048577),new(MaximumQueryWork:-1),new(MaximumQueryWork:3145729)})
            Bad<ArgumentOutOfRangeException>(()=>TerrainFilterFractalInfluence.EvaluateBatch(valid,Array.Empty<uint>(),p,l));
        var exact=new TerrainFilterFractalInputLimits(MaximumInputBytes:valid.CopyEncodedBytes().Length,MaximumStringBytes:valid.Header.CopyNameBytes().Length,MaximumDecodedCopyBytes:valid.DecodedCopyBytes);
        Check(TerrainFilterFractalInput.Decode(valid.CopyEncodedBytes(),exact).DecodedCopyBytes==valid.DecodedCopyBytes);
        Bad<InvalidDataException>(()=>TerrainFilterFractalInput.Decode(valid.CopyEncodedBytes(),exact with {MaximumInputBytes=exact.MaximumInputBytes-1}));
        Bad<InvalidDataException>(()=>TerrainFilterFractalInput.Decode(valid.CopyEncodedBytes(),exact with {MaximumStringBytes=exact.MaximumStringBytes-1}));
        Bad<InvalidDataException>(()=>TerrainFilterFractalInput.Decode(valid.CopyEncodedBytes(),exact with {MaximumDecodedCopyBytes=exact.MaximumDecodedCopyBytes-1}));
        Check(TerrainFilterFractalInfluence.Shape(prior,0xffffffff)==0);
        void Null(Action action) { try { action();throw new InvalidOperationException("Expected null rejection."); }catch(ArgumentNullException) { nulls++; } }
        Null(()=>TerrainFilterFractalInput.Decode((TerrainGeneratorSection)null!));Null(()=>TerrainFilterFractalInfluence.Query(null!,0,p));
        Null(()=>TerrainFilterFractalInfluence.Shape(null!,0));Null(()=>TerrainFilterFractalInfluence.Evaluate(null!,0,p));Null(()=>TerrainFilterFractalInfluence.EvaluateBatch(null!,Array.Empty<uint>(),p));
        var retainedCarrier=TerrainLayerInfluenceQuery.FromFractalFilter(prior);
        Check(ReferenceEquals(retainedCarrier.FractalFilter,prior)&&ReferenceEquals(prior.RuntimeCache.Input,valid)&&(int)retainedCarrier.Source==5&&retainedCarrier.ProjectionBits==prior.QueryProjectionBits);
        Check((int)TerrainLayerInfluenceQuerySource.HeightFilter==4);
        Null(()=>TerrainLayerInfluenceQuery.FromFractalFilter(null!));
        var original=Snapshot(TerrainFilterFractalInput.Decode(Convert.FromHexString(document.RootElement.GetProperty("OriginalHex").GetString()!)));
        return new(rawCases,queryCases,checks,nulls,original);
    }
}
