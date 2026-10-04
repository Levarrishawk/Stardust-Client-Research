using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;

namespace Stardust.Core.SmokeTests;
public sealed record TerrainRetainedFractalRun(List<object> Cases,int OwnershipChecks,int BudgetChecks,int FactoryNegatives);
public static class TerrainRetainedFractalSampleScenarios
{
    private static TerrainSuppliedFractalSample? Sample(JsonElement value)
    {
        string kind=value.GetProperty("Kind").GetString()!;
        return kind switch
        {
            "null"=>null,
            "wide"=>TerrainSuppliedFractalSample.FromWide(value.GetProperty("Significand").GetInt64(),value.GetProperty("Exponent").GetInt32(),value.GetProperty("NegativeZero").GetBoolean()),
            "f32"=>TerrainSuppliedFractalSample.FromF32(value.GetProperty("Bits").GetUInt32()),
            "cache"=>TerrainSuppliedFractalSample.FromCacheF32(value.GetProperty("Bits").GetUInt32()),
            _=>throw new InvalidDataException("Unknown fixture sample kind.")
        };
    }
    public static TerrainRetainedFractalRun Run(string path)
    {
        using var doc=JsonDocument.Parse(File.ReadAllBytes(path));var rows=new List<object>();
        TerrainHeightFractalInput? height=null;TerrainFilterFractalInput? filter=null;
        foreach(var c in doc.RootElement.GetProperty("Cases").EnumerateArray())
        {
            string name=c.GetProperty("Name").GetString()!;
            try
            {
                var sample=Sample(c.GetProperty("Sample"));var p=(TerrainHeightArithmeticPrecision)c.GetProperty("Precision").GetInt32();
                byte[] encoded=Convert.FromHexString(c.GetProperty("Hex").GetString()!);
                if(c.GetProperty("Consumer").GetString()=="ahfr")
                {
                    var input=TerrainHeightFractalInput.Decode(encoded);if(input.Header?.IsEnabled==true)height??=input;
                    var r=TerrainSuppliedHeightFractal.Evaluate(input,c.GetProperty("Height").GetUInt32(),c.GetProperty("Influence").GetUInt32(),sample,p);
                    rows.Add(new{Name=name,Reject="None",Output=r.Consumer.ResultHeightBits,Outcome=(int)r.Consumer.Outcome,Projection=r.Sample?.ProjectionBits,Provenance=r.Sample is null?(int?)null:(int)r.Sample.Provenance});
                }
                else
                {
                    var input=TerrainFilterFractalInput.Decode(encoded);if(input.Header?.IsEnabled==true)filter??=input;
                    var r=TerrainFilterFractalInfluence.Evaluate(input,sample,p);var q=r.Consumer.Query;
                    var carrier=q is null?null:TerrainLayerInfluenceQuery.FromRetainedFractalFilter(q);
                    if(q is not null&&(!ReferenceEquals(q.RetainedSample,sample)||!ReferenceEquals(carrier!.RetainedFractalSample,sample)))throw new InvalidOperationException("Lost owned sample.");
                    rows.Add(new{Name=name,Reject="None",Scaled=q?.ScaledSampleBits,Query=q?.QueryProjectionBits,Influence=r.Consumer.InfluenceBits,Carrier=carrier?.ShapeProjectionBits(input.Fields!.FeatheringTypeBits,p),Branch=q is null?(int?)null:(int)q.Branch,Projection=r.Sample?.ProjectionBits,Provenance=r.Sample is null?(int?)null:(int)r.Sample.Provenance,Work=r.Consumer.QueryWork,OwnedBytes=carrier?.RetainedFractalSampleBytes});
                }
            }
            catch(ArgumentOutOfRangeException) { rows.Add(new{Name=name,Reject="Options"}); }
            catch(InvalidDataException) { rows.Add(new{Name=name,Reject="Input"}); }
        }
        int ownership=0,budget=0,negatives=0;
        void Check(bool okay) { if(!okay)throw new InvalidOperationException("Retained ownership invariant.");ownership++; }
        void Bad(Action action) { try { action();throw new InvalidOperationException("Expected retained rejection."); }catch(InvalidDataException) { budget++; } }
        var sample0=TerrainSuppliedFractalSample.FromWide(16777217,-24);var cache=sample0.ToCacheReload();
        Check(sample0.ProjectionBits==cache.ProjectionBits&&sample0.Provenance!=cache.Provenance);
        Check(typeof(TerrainSuppliedFractalSample).GetProperties().All(p=>p.SetMethod is null));
        Check(TerrainSuppliedFractalSample.FromWide(0,-512,true).ProjectionBits==0x80000000);
        Check(TerrainSuppliedFractalSample.FromCacheF32(0x80000000).NegativeZero);
        Check(TerrainSuppliedFractalSample.FromWide(long.MinValue,-63).ProjectionBits==0xbf800000);
        Check(TerrainSuppliedFractalSample.FromWide(33554430,103).ProjectionBits==0x7f7fffff);
        Check(TerrainSuppliedFractalSample.FromWide(-33554430,103).ProjectionBits==0xff7fffff);
        uint[] h={0xbf800000,0x3f800000},w={0x3f800000,0x3f800000};TerrainSuppliedFractalSample?[] samples={sample0,cache};
        var hb=TerrainSuppliedHeightFractal.EvaluateBatch(height!,h,w,samples,TerrainHeightArithmeticPrecision.Binary64,new(2,12,96));
        uint before=hb.Results[0].Consumer.ResultHeightBits;h[0]=0;w[0]=0;samples[0]=null;
        Check(hb.Results[0].Consumer.ResultHeightBits==before&&ReferenceEquals(hb.Results[0].Sample,sample0));
        Check(hb.Work==12&&hb.SnapshotBytes==96);
        var fb=TerrainFilterFractalInfluence.EvaluateBatch(filter!,new TerrainSuppliedFractalSample?[]{sample0,cache},TerrainHeightArithmeticPrecision.Binary64,new(2,6,80));
        Check(fb.Work==6&&fb.SnapshotBytes==80&&ReferenceEquals(fb.Results[0].Sample,sample0));
        var ownedQuery=TerrainFilterFractalInfluence.Query(filter!,sample0,TerrainHeightArithmeticPrecision.Binary64);
        var ownedCarrier=TerrainLayerInfluenceQuery.FromRetainedFractalFilter(ownedQuery);
        Check(ReferenceEquals(ownedCarrier.RetainedFractalSample,sample0)&&ownedCarrier.RetainedFractalSampleBytes==32);
        TerrainFractalFilterQuery?[] queryInputs={ownedQuery,ownedQuery};
        var carrierBatch=TerrainLayerInfluenceQuery.FromRetainedFractalFilters(queryInputs,new(2,4,80));
        queryInputs[0]=null;
        Check(carrierBatch.Work==4&&carrierBatch.SnapshotBytes==80&&ReferenceEquals(carrierBatch.Results[0].RetainedFractalSample,sample0));
        var validQueries=new TerrainFractalFilterQuery?[]{ownedQuery,ownedQuery};
        foreach(var cap in new[]{new TerrainRetainedFractalLimits(1,4,80),new(2,3,80),new(2,4,79)})
            Bad(()=>TerrainLayerInfluenceQuery.FromRetainedFractalFilters(validQueries,cap));
        Bad(()=>TerrainLayerInfluenceQuery.FromRetainedFractalFilters(new TerrainFractalFilterQuery?[]{ownedQuery,TerrainFilterFractalInfluence.Query(filter!,sample0.ProjectionBits,TerrainHeightArithmeticPrecision.Binary64)}));
        Bad(()=>TerrainLayerInfluenceQuery.FromRetainedFractalFilter(TerrainFilterFractalInfluence.Query(filter!,sample0.ProjectionBits,TerrainHeightArithmeticPrecision.Binary64)));
        var good=new TerrainSuppliedFractalSample?[]{sample0,cache};uint[] hs={0,0},ws={0x3f800000,0x3f800000};
        foreach(var cap in new[]{new TerrainRetainedFractalLimits(1,12,96),new(2,11,96),new(2,12,95)})
            Bad(()=>TerrainSuppliedHeightFractal.EvaluateBatch(height!,hs,ws,good,TerrainHeightArithmeticPrecision.Binary64,cap));
        foreach(var cap in new[]{new TerrainRetainedFractalLimits(1,6,80),new(2,5,80),new(2,6,79)})
            Bad(()=>TerrainFilterFractalInfluence.EvaluateBatch(filter!,good,TerrainHeightArithmeticPrecision.Binary64,cap));
        Bad(()=>TerrainSuppliedHeightFractal.EvaluateBatch(height!,hs,ws,new TerrainSuppliedFractalSample?[]{sample0,null},TerrainHeightArithmeticPrecision.Binary64));
        Bad(()=>TerrainFilterFractalInfluence.EvaluateBatch(filter!,new TerrainSuppliedFractalSample?[]{sample0,null},TerrainHeightArithmeticPrecision.Binary64));
        byte[] overflowHeightBytes=height!.CopyEncodedBytes();
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(overflowHeightBytes.AsSpan(overflowHeightBytes.Length-4),0x40000000);
        var overflowHeight=TerrainHeightFractalInput.Decode(overflowHeightBytes);
        byte[] overflowFilterBytes=filter!.CopyEncodedBytes();
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(overflowFilterBytes.AsSpan(overflowFilterBytes.Length-4),0x40000000);
        var overflowFilter=TerrainFilterFractalInput.Decode(overflowFilterBytes);
        var lateOverflow=new TerrainSuppliedFractalSample?[]{sample0,TerrainSuppliedFractalSample.FromF32(0x7f7fffff)};
        Bad(()=>TerrainSuppliedHeightFractal.EvaluateBatch(overflowHeight,hs,ws,lateOverflow,TerrainHeightArithmeticPrecision.Binary64));
        Bad(()=>TerrainFilterFractalInfluence.EvaluateBatch(overflowFilter,lateOverflow,TerrainHeightArithmeticPrecision.Binary64));
        Check(hb.Results[0].Consumer.ResultHeightBits==before&&ReferenceEquals(good[0],sample0));
        foreach(var cap in new[]{new TerrainRetainedFractalLimits(-1),new(MaximumWork:-1),new(MaximumSnapshotBytes:-1),new(MaximumSamples:1048577),new(MaximumWork:6291457),new(MaximumSnapshotBytes:50331649)})
        { try { TerrainSuppliedHeightFractal.EvaluateBatch(height!,hs,ws,good,TerrainHeightArithmeticPrecision.Binary64,cap);throw new InvalidOperationException("Expected policy rejection."); }catch(ArgumentOutOfRangeException) { budget++; } }
        foreach(var f in doc.RootElement.GetProperty("FactoryNegatives").EnumerateArray())
        { try { Sample(f);throw new InvalidOperationException("Expected factory rejection."); }catch(InvalidDataException) { negatives++; } }
        return new(rows,ownership,budget,negatives);
    }
}
