using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;

namespace Stardust.Core.SmokeTests;

public sealed record TerrainLayerInfluenceScenarioResult(IReadOnlyList<object> Cases,int OwnershipChecks);

public static class TerrainLayerInfluenceScenarios
{
    public static TerrainLayerInfluenceScenarioResult Run(string fixturePath)
    {
        using var doc=JsonDocument.Parse(File.ReadAllText(fixturePath));var cases=new List<object>();
        TerrainLayerInfluenceQuery? circleCarrier=null,rectangleCarrier=null;
        foreach(var f in doc.RootElement.EnumerateArray())
        {
            string name=f.GetProperty("Name").GetString()!;
            try
            {
                var boundaries=Entries(f.GetProperty("Boundaries"));var filters=Entries(f.GetProperty("Filters"));
                foreach(var entry in boundaries.Concat(filters))
                {
                    if(entry.Query?.Source==TerrainLayerInfluenceQuerySource.Circle)circleCarrier=entry.Query;
                    if(entry.Query?.Source==TerrainLayerInfluenceQuerySource.Rectangle)rectangleCarrier=entry.Query;
                }
                var sample=TerrainLayerInfluenceSample.Create(f.GetProperty("Incoming").GetUInt32(),boundaries,filters,
                    f.GetProperty("BoundaryGate").GetInt32(),f.GetProperty("FilterGate").GetInt32(),f.GetProperty("InvertBoundary").GetBoolean(),
                    f.GetProperty("InvertFilter").GetBoolean(),(TerrainHeightArithmeticPrecision)f.GetProperty("Combine").GetInt32(),
                    (TerrainHeightArithmeticPrecision)f.GetProperty("Product").GetInt32(),f.GetProperty("CreateMaximumQueries").GetInt32());
                TerrainLayerInfluenceLimits? limits=null;
                if(f.TryGetProperty("Limits",out var l))limits=new(l[0].GetInt32(),l[1].GetInt32(),l[2].GetInt32());
                var receipt=TerrainLayerInfluence.Evaluate(sample,limits);
                cases.Add(new {Name=name,Reject="None",receipt.LocalInfluenceBits,receipt.ProductBits,receipt.BoundaryFallback,
                    receipt.BoundaryEarlyExit,receipt.FilterEarlyExit,receipt.BoundaryPositive,receipt.LocalPositive,receipt.AffectorEligible,
                    receipt.ProductPositive,receipt.BoundaryVisits,receipt.FilterVisits,BoundaryInputs=sample.Boundaries.Select(e=>Metadata(e.Query)).ToArray(),
                    FilterInputs=sample.Filters.Select(e=>Metadata(e.Query)).ToArray()});
            }
            catch(ArgumentOutOfRangeException){cases.Add(new {Name=name,Reject="Options"});}
            catch(InvalidDataException){cases.Add(new {Name=name,Reject="Input"});}
        }
        int checks=0;
        void Check(bool condition){if(!condition)throw new InvalidOperationException("Layer influence ownership check "+checks);checks++;}
        void Bad<T>(Action action) where T:Exception
        {
            try{action();}catch(T){checks++;return;}throw new InvalidOperationException("Expected "+typeof(T).Name+" at "+checks);
        }
        var p=TerrainHeightArithmeticPrecision.Binary53;
        var q=TerrainLayerInfluenceQuery.FromRawBits(0x3f000000,p);
        Check(q.RawBits==0x3f000000&&q.ProjectionBits==0x3f000000&&q.Precision==p&&q.Source==TerrainLayerInfluenceQuerySource.SuppliedRawBits);
        Check(TerrainLayerInfluenceQuery.FromRawBits(0x80000000,p).ShapeProjectionBits(0,p)==0x80000000);
        Bad<InvalidDataException>(()=>TerrainLayerInfluenceQuery.FromRawBits(0x7fc12345,p));
        Bad<ArgumentOutOfRangeException>(()=>TerrainLayerInfluenceQuery.FromRawBits(0,(TerrainHeightArithmeticPrecision)25));
        Bad<ArgumentOutOfRangeException>(()=>q.ShapeProjectionBits(0,(TerrainHeightArithmeticPrecision)0));
        Bad<ArgumentNullException>(()=>TerrainLayerInfluenceQuery.FromCircle(null!));
        Bad<ArgumentNullException>(()=>TerrainLayerInfluenceQuery.FromRectangle(null!));
        Check(circleCarrier is not null&&ReferenceEquals(circleCarrier.Circle,TerrainLayerInfluenceQuery.FromCircle(circleCarrier.Circle!).Circle));
        Check(rectangleCarrier is not null&&ReferenceEquals(rectangleCarrier.Rectangle,TerrainLayerInfluenceQuery.FromRectangle(rectangleCarrier.Rectangle!).Rectangle));
        Check(circleCarrier!.ShapeProjectionBits(3,circleCarrier.Precision)==TerrainBoundaryCircleInfluence.Shape(circleCarrier.Circle!,3));
        Check(rectangleCarrier!.ShapeProjectionBits(3,rectangleCarrier.Precision)==TerrainBoundaryRectangleInfluence.Shape(rectangleCarrier.Rectangle!,3));
        TerrainLayerInfluenceEntry[] source=[new(true,0,q),new(false,2,null)];
        var sampleOwned=TerrainLayerInfluenceSample.Create(0xbf800000,source,source,1,1,false,false,p,p,4);
        source[0]=new(true,1,TerrainLayerInfluenceQuery.FromRawBits(0x7f7fffff,p));
        var retained=TerrainLayerInfluence.Evaluate(sampleOwned,new(1,4,4));
        Check(retained.LocalInfluenceBits==0x3f000000&&retained.ProductBits==0xbf000000&&retained.AffectorEligible&&!retained.ProductPositive);
        Check(ReferenceEquals(retained.Sample,sampleOwned)&&ReferenceEquals(sampleOwned.Boundaries[0].Query,q));
        Check(((IList<TerrainLayerInfluenceEntry>)sampleOwned.Boundaries).IsReadOnly);
        Check(((IList<TerrainLayerInfluenceVisit>)retained.BoundaryVisits).IsReadOnly);
        Bad<InvalidDataException>(()=>TerrainLayerInfluence.Evaluate(sampleOwned,new(0,4,4)));
        Bad<InvalidDataException>(()=>TerrainLayerInfluence.Evaluate(sampleOwned,new(1,3,4)));
        Bad<InvalidDataException>(()=>TerrainLayerInfluence.Evaluate(sampleOwned,new(1,4,3)));
        var empty=TerrainLayerInfluenceSample.Create(0,Array.Empty<TerrainLayerInfluenceEntry>(),Array.Empty<TerrainLayerInfluenceEntry>(),0,0,false,false,p,p,0);
        var emptyReceipt=TerrainLayerInfluence.Evaluate(empty,new(1,0,0));
        Check(emptyReceipt.BoundaryFallback&&emptyReceipt.LocalInfluenceBits==0x3f800000&&emptyReceipt.AffectorEligible&&!emptyReceipt.ProductPositive);
        Check(TerrainLayerInfluence.EvaluateBatch(Array.Empty<TerrainLayerInfluenceSample>(),new(0,0,0)).Results.Count==0);
        foreach(var limits in new[]{new TerrainLayerInfluenceLimits(-1,4,4),new(65537,4,4),new(1,-1,4),new(1,4097,4),new(1,4,-1),new(1,4,1048577)})
            Bad<ArgumentOutOfRangeException>(()=>TerrainLayerInfluence.Evaluate(sampleOwned,limits));
        Check(TerrainLayerInfluence.EvaluateBatch(Array.Empty<TerrainLayerInfluenceSample>(),new(65536,4096,1048576)).Results.Count==0);
        TerrainLayerInfluenceSample[] batchSource=[sampleOwned,sampleOwned];
        var batch=TerrainLayerInfluence.EvaluateBatch(batchSource,new(2,4,8));batchSource[0]=empty;
        Check(ReferenceEquals(batch.Results[0].Sample,sampleOwned)&&batch.Results[1].ProductBits==retained.ProductBits);
        var copy=batch.CopyResults();copy[0]=emptyReceipt;
        Check(ReferenceEquals(batch.Results[0].Sample,sampleOwned)&&((IList<TerrainLayerInfluenceReceipt>)batch.Results).IsReadOnly);
        Bad<InvalidDataException>(()=>TerrainLayerInfluence.EvaluateBatch(new[]{sampleOwned,sampleOwned},new(1,4,8)));
        Bad<InvalidDataException>(()=>TerrainLayerInfluence.EvaluateBatch(new[]{sampleOwned,sampleOwned},new(2,4,7)));
        Bad<ArgumentNullException>(()=>TerrainLayerInfluence.Evaluate(null!));
        Bad<ArgumentNullException>(()=>TerrainLayerInfluence.EvaluateBatch(new TerrainLayerInfluenceSample[]{empty,null!}));
        Bad<InvalidDataException>(()=>TerrainLayerInfluenceSample.Create(0x7f800000,source,source,0,0,false,false,p,p,4));
        Bad<InvalidDataException>(()=>TerrainLayerInfluenceSample.Create(0,new TerrainLayerInfluenceEntry[]{new(true,0,null)},Array.Empty<TerrainLayerInfluenceEntry>(),0,0,false,false,p,p));
        Bad<InvalidDataException>(()=>TerrainLayerInfluenceSample.Create(0,source,source,0,0,false,false,p,p,3));
        Bad<ArgumentOutOfRangeException>(()=>TerrainLayerInfluenceSample.Create(0,source,source,0,0,false,false,p,p,-1));
        var negative=TerrainLayerInfluenceQuery.FromRawBits(0xbf800000,p);
        var late=TerrainLayerInfluenceSample.Create(0x3f800000,new TerrainLayerInfluenceEntry[]{new(true,0,q)},new TerrainLayerInfluenceEntry[]{new(true,2,negative)},1,1,false,false,p,p);
        Bad<InvalidDataException>(()=>TerrainLayerInfluence.EvaluateBatch(new[]{sampleOwned,late},new(2,4,8)));
        var overflow=TerrainLayerInfluenceSample.Create(0x7f7fffff,new TerrainLayerInfluenceEntry[]{new(true,0,TerrainLayerInfluenceQuery.FromRawBits(0x7f7fffff,p))},Array.Empty<TerrainLayerInfluenceEntry>(),1,0,false,false,p,p);
        Bad<InvalidDataException>(()=>TerrainLayerInfluence.EvaluateBatch(new[]{sampleOwned,overflow},new(2,4,8)));
        Check(retained.LocalInfluenceBits==0x3f000000&&batch.Results.Count==2&&sampleOwned.Boundaries.Count==2);
        TerrainLayerInfluenceEntry[] disabled=new TerrainLayerInfluenceEntry[4096];
        var capped=TerrainLayerInfluenceSample.Create(0,disabled,Array.Empty<TerrainLayerInfluenceEntry>(),1,0,false,false,p,p,4096);
        Check(TerrainLayerInfluence.Evaluate(capped,new(1,4096,4096)).BoundaryVisits.Count==4096);
        Bad<InvalidDataException>(()=>TerrainLayerInfluence.Evaluate(capped,new(1,4096,4095)));
        Check(TerrainLayerInfluence.Evaluate(empty).Sample.CombinePrecision==p);
        Bad<NotSupportedException>(()=>((IList<TerrainLayerInfluenceEntry>)sampleOwned.Boundaries)[0]=default);
        Bad<NotSupportedException>(()=>((IList<TerrainLayerInfluenceVisit>)retained.BoundaryVisits)[0]=default);
        try{TerrainLayerInfluence.EvaluateBatch(new[]{late,sampleOwned},new(2,3,8));throw new InvalidOperationException("Missing preflight rejection.");}
        catch(InvalidDataException error){Check(error.Message=="Per-sample supplied query budget.");}
        try{TerrainLayerInfluence.EvaluateBatch(new[]{late,sampleOwned},new(2,4,5));throw new InvalidOperationException("Missing aggregate rejection.");}
        catch(InvalidDataException error){Check(error.Message=="Aggregate supplied query budget.");}
        Bad<ArgumentOutOfRangeException>(()=>TerrainLayerInfluenceSample.Create(0,source,source,0,0,false,false,p,p,4097));
        var defaultEntries=new TerrainLayerInfluenceEntry[256];
        var defaultSample=TerrainLayerInfluenceSample.Create(0,defaultEntries,Array.Empty<TerrainLayerInfluenceEntry>(),1,0,false,false,p,p);
        var defaultSamples=Enumerable.Repeat(defaultSample,256).ToArray();
        var defaultBatch=TerrainLayerInfluence.EvaluateBatch(defaultSamples);
        Check(defaultBatch.Results.Count==256&&defaultBatch.Results.All(r=>r.BoundaryVisits.Count==256));
        Bad<InvalidDataException>(()=>TerrainLayerInfluence.EvaluateBatch(Enumerable.Repeat(defaultSample,257).ToArray()));
        Bad<InvalidDataException>(()=>TerrainLayerInfluence.EvaluateBatch(defaultSamples,new(256,255,65536)));
        Bad<InvalidDataException>(()=>TerrainLayerInfluence.EvaluateBatch(defaultSamples,new(256,256,65535)));
        Check(defaultSample.Boundaries.Count==256&&defaultBatch.Results[255].LocalInfluenceBits==0x3f800000);
        return new(cases.AsReadOnly(),checks);
    }
    private static object? Metadata(TerrainLayerInfluenceQuery? q)=>q is null?null:new {Source=(int)q.Source,Precision=(int)q.Precision,q.RawBits,q.ProjectionBits};
    private static TerrainLayerInfluenceEntry[] Entries(JsonElement array)=>array.EnumerateArray().Select(e=>new TerrainLayerInfluenceEntry(e.GetProperty("Enabled").GetBoolean(),e.GetProperty("Selector").GetUInt32(),Query(e.GetProperty("Query")))).ToArray();
    private static TerrainLayerInfluenceQuery? Query(JsonElement q)
    {
        if(q.ValueKind==JsonValueKind.Null)return null;
        var p=(TerrainHeightArithmeticPrecision)q.GetProperty("Precision").GetInt32();
        if(q.GetProperty("Type").GetString()=="Raw")return TerrainLayerInfluenceQuery.FromRawBits(q.GetProperty("Bits").GetUInt32(),p);
        var cp=(TerrainHeightArithmeticPrecision)q.GetProperty("CachePrecision").GetInt32();var xy=q.GetProperty("Coordinates");
        byte[] bytes=Convert.FromHexString(q.GetProperty("RuleHex").GetString()!);
        return q.GetProperty("Type").GetString()=="Circle"
            ?TerrainLayerInfluenceQuery.FromCircle(TerrainBoundaryCircleInfluence.Query(TerrainBoundaryCircleInput.Decode(bytes),xy[0].GetUInt32(),xy[1].GetUInt32(),cp,p))
            :TerrainLayerInfluenceQuery.FromRectangle(TerrainBoundaryRectangleInfluence.Query(TerrainBoundaryRectangleInput.Decode(bytes),xy[0].GetUInt32(),xy[1].GetUInt32(),cp,p));
    }
}


