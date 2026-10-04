using System.Buffers.Binary;
using System.Text;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;

namespace Stardust.Core.SmokeTests;

public sealed record TerrainSuppliedLayerHeightScenarioResult(IReadOnlyList<object> Cases,int OwnershipChecks);
public static class TerrainSuppliedLayerHeightScenarios
{
    public static TerrainSuppliedLayerHeightScenarioResult Run(string path)
    {
        using var document=JsonDocument.Parse(File.ReadAllText(path));var cases=new List<object>();TerrainLayerInfluenceQuery? polygon=null;
        foreach(var f in document.RootElement.EnumerateArray())
        {
            string name=f.GetProperty("Name").GetString()!;
            try
            {
                var layer=Layer(f.GetProperty("Layer"));
                foreach(var e in layer.Boundaries.Concat(layer.Filters))if(e.Query?.Polygon is not null)polygon=e.Query;
                var rules=f.GetProperty("Rules").EnumerateArray().Select(r=>r.ValueKind==JsonValueKind.Null?null!:TerrainGeneratorInput.Decode(Convert.FromHexString(r.GetProperty("GeneratorHex").GetString()!)).Rules.Single().HeightConstant!).ToArray();
                var sample=TerrainSuppliedLayerHeightSample.Create(layer,f.GetProperty("Initial").GetUInt32(),rules,
                    (TerrainHeightArithmeticPrecision)f.GetProperty("AffectorPrecision").GetInt32(),f.GetProperty("CreateMaximumRules").GetInt32());
                TerrainSuppliedLayerHeightLimits? limits=null;
                if(f.TryGetProperty("Limits",out var l))limits=new(l[0].GetInt32(),l[1].GetInt32(),l[2].GetInt32(),l[3].GetInt32(),l[4].GetInt32(),l[5].GetInt64());
                var receipt=TerrainSuppliedLayerHeight.Evaluate(sample,limits);
                cases.Add(new {Name=name,Reject="None",Influence=Influence(receipt.Influence,f.GetProperty("Layer").GetProperty("Name").GetString()!),sample.OperationCharge,
                    receipt.FinalHeightBits,receipt.FinalHeightIsFinite,receipt.Visits});
            }
            catch(ArgumentOutOfRangeException){cases.Add(new {Name=name,Reject="Options"});}
            catch(InvalidDataException){cases.Add(new {Name=name,Reject="Input"});}
            catch(ArgumentNullException){cases.Add(new {Name=name,Reject="Input"});}
        }
        int checks=0;
        void Check(bool x){if(!x)throw new InvalidOperationException("Supplied height check "+checks);checks++;}
        void Bad<T>(Action action) where T:Exception{try{action();}catch(T){checks++;return;}throw new InvalidOperationException("Expected "+typeof(T).Name+" at "+checks);}
        var p=TerrainHeightArithmeticPrecision.Binary53;
        var q=TerrainLayerInfluenceQuery.FromRawBits(0x3f000000,p);
        var layerOwned=TerrainLayerInfluenceSample.Create(0x3f800000,new TerrainLayerInfluenceEntry[]{new(true,0,q)},new TerrainLayerInfluenceEntry[]{new(true,0,q)},1,1,false,false,p,p);
        var add=Rule(1,1,0x3f800000);var multiply=Rule(1,3,0x40000000);
        TerrainHeightConstantInput[] source=[add,multiply];
        var sampleOwned=TerrainSuppliedLayerHeightSample.Create(layerOwned,0x3f800000,source,p,2);source[0]=Rule(1,4,0);
        var exact=new TerrainSuppliedLayerHeightLimits(1,2,2,2,2,8);var retained=TerrainSuppliedLayerHeight.Evaluate(sampleOwned,exact);
        Check(retained.FinalHeightBits==0x40100000&&sampleOwned.OperationCharge==8&&ReferenceEquals(sampleOwned.Rules[0],add));
        Check(ReferenceEquals(retained.Influence.Sample,layerOwned)&&ReferenceEquals(retained.Sample,sampleOwned));
        Check(((IList<TerrainHeightConstantInput>)sampleOwned.Rules).IsReadOnly&&((IList<TerrainSuppliedLayerHeightVisit>)retained.Visits).IsReadOnly);
        Bad<NotSupportedException>(()=>((IList<TerrainHeightConstantInput>)sampleOwned.Rules)[0]=multiply);
        Bad<NotSupportedException>(()=>((IList<TerrainSuppliedLayerHeightVisit>)retained.Visits)[0]=default);
        Check(polygon is not null&&polygon.Source==TerrainLayerInfluenceQuerySource.Polygon&&polygon.RawBits is null&&polygon.Circle is null&&polygon.Rectangle is null);
        Check(ReferenceEquals(polygon!.Polygon,TerrainLayerInfluenceQuery.FromPolygon(polygon.Polygon!).Polygon));
        Check(polygon.Precision==polygon.Polygon!.Precision&&polygon.ProjectionBits==polygon.Polygon.QueryProjectionBits);
        Check(polygon.ShapeProjectionBits(3,polygon.Precision)==TerrainBoundaryPolygonInfluence.Shape(polygon.Polygon,3));
        Bad<ArgumentNullException>(()=>TerrainLayerInfluenceQuery.FromPolygon(null!));
        Check((int)TerrainLayerInfluenceQuerySource.SuppliedRawBits==0&&(int)TerrainLayerInfluenceQuerySource.Circle==1&&(int)TerrainLayerInfluenceQuerySource.Rectangle==2);
        foreach(var limits in new[]{exact with {MaximumSamples=0},exact with {MaximumQueriesPerSample=1},exact with {MaximumTotalQueries=1},exact with {MaximumRulesPerSample=1},exact with {MaximumTotalRules=1},exact with {MaximumOperations=7}})
            Bad<InvalidDataException>(()=>TerrainSuppliedLayerHeight.Evaluate(sampleOwned,limits));
        foreach(var limits in new[]{exact with {MaximumSamples=-1},exact with {MaximumSamples=65537},exact with {MaximumQueriesPerSample=-1},exact with {MaximumQueriesPerSample=4097},exact with {MaximumTotalQueries=-1},exact with {MaximumTotalQueries=1048577},exact with {MaximumRulesPerSample=-1},exact with {MaximumRulesPerSample=4097},exact with {MaximumTotalRules=-1},exact with {MaximumTotalRules=1048577},exact with {MaximumOperations=-1},exact with {MaximumOperations=5242881}})
            Bad<ArgumentOutOfRangeException>(()=>TerrainSuppliedLayerHeight.Evaluate(sampleOwned,limits));
        var empty=TerrainSuppliedLayerHeightSample.Create(layerOwned,0x7fc12345,Array.Empty<TerrainHeightConstantInput>(),p,0);
        Check(!TerrainSuppliedLayerHeight.Evaluate(empty).FinalHeightIsFinite);
        Check(TerrainSuppliedLayerHeight.EvaluateBatch(Array.Empty<TerrainSuppliedLayerHeightSample>(),new(0,0,0,0,0,0)).Results.Count==0);
        Check(TerrainSuppliedLayerHeight.EvaluateBatch(Array.Empty<TerrainSuppliedLayerHeightSample>(),new(65536,4096,1048576,4096,1048576,5242880)).Results.Count==0);
        TerrainSuppliedLayerHeightSample[] array=[sampleOwned,sampleOwned];var batch=TerrainSuppliedLayerHeight.EvaluateBatch(array,new(2,2,4,2,4,16));array[0]=empty;
        Check(batch.Results.Count==2&&batch.Results[0].FinalHeightBits==retained.FinalHeightBits&&ReferenceEquals(batch.Results[0].Sample,sampleOwned));
        var copied=batch.CopyHeightBits();copied[0]=0;Check(batch.Results[0].FinalHeightBits==0x40100000);
        Check(((IList<TerrainSuppliedLayerHeightReceipt>)batch.Results).IsReadOnly);
        foreach(var limits in new[]{new TerrainSuppliedLayerHeightLimits(1,2,4,2,4,16),new(2,2,3,2,4,16),new(2,2,4,2,3,16),new(2,2,4,2,4,15)})
            Bad<InvalidDataException>(()=>TerrainSuppliedLayerHeight.EvaluateBatch(new[]{sampleOwned,sampleOwned},limits));
        Bad<ArgumentNullException>(()=>TerrainSuppliedLayerHeight.Evaluate(null!));
        Bad<ArgumentNullException>(()=>TerrainSuppliedLayerHeight.EvaluateBatch(new TerrainSuppliedLayerHeightSample[]{sampleOwned,null!}));
        Bad<ArgumentNullException>(()=>TerrainSuppliedLayerHeightSample.Create(null!,0,source,p));
        Bad<ArgumentNullException>(()=>TerrainSuppliedLayerHeightSample.Create(layerOwned,0,new TerrainHeightConstantInput[]{add,null!},p));
        Bad<ArgumentOutOfRangeException>(()=>TerrainSuppliedLayerHeightSample.Create(layerOwned,0,source,(TerrainHeightArithmeticPrecision)0));
        Bad<ArgumentOutOfRangeException>(()=>TerrainSuppliedLayerHeightSample.Create(layerOwned,0,source,p,-1));
        Bad<ArgumentOutOfRangeException>(()=>TerrainSuppliedLayerHeightSample.Create(layerOwned,0,source,p,4097));
        Bad<InvalidDataException>(()=>TerrainSuppliedLayerHeightSample.Create(layerOwned,0,source,p,1));
        var late=TerrainSuppliedLayerHeightSample.Create(layerOwned,0x7f7fffff,new[]{add,multiply},p);
        Bad<InvalidDataException>(()=>TerrainSuppliedLayerHeight.EvaluateBatch(new[]{sampleOwned,late},new(2,2,4,2,4,16)));
        try{TerrainSuppliedLayerHeight.EvaluateBatch(new[]{late,sampleOwned},new(2,2,4,2,4,15));throw new InvalidOperationException("Missing reservation failure.");}
        catch(InvalidDataException error){Check(error.Message=="Supplied height operation budget.");}
        Check(retained.FinalHeightBits==0x40100000&&batch.Results.Count==2&&sampleOwned.Rules.Count==2);
        TerrainHeightConstantInput[] disabled=Enumerable.Repeat(Rule(0,0,0x7fc12345),4096).ToArray();
        var capped=TerrainSuppliedLayerHeightSample.Create(layerOwned,0x7fc12345,disabled,p,4096);
        Check(TerrainSuppliedLayerHeight.Evaluate(capped,new(1,2,2,4096,4096,20480)).Visits.Count==4096);
        Bad<InvalidDataException>(()=>TerrainSuppliedLayerHeight.Evaluate(capped,new(1,2,2,4096,4096,20479)));
        return new(cases.AsReadOnly(),checks);
    }
    private static object Influence(TerrainLayerInfluenceReceipt r,string name)=>new {Name=name,Reject="None",r.LocalInfluenceBits,r.ProductBits,r.BoundaryFallback,r.BoundaryEarlyExit,r.FilterEarlyExit,r.BoundaryPositive,r.LocalPositive,r.AffectorEligible,r.ProductPositive,r.BoundaryVisits,r.FilterVisits,BoundaryInputs=r.Sample.Boundaries.Select(e=>Meta(e.Query)).ToArray(),FilterInputs=r.Sample.Filters.Select(e=>Meta(e.Query)).ToArray()};
    private static object? Meta(TerrainLayerInfluenceQuery? q)=>q is null?null:new {Source=(int)q.Source,Precision=(int)q.Precision,q.RawBits,q.ProjectionBits};
    private static TerrainLayerInfluenceSample Layer(JsonElement f)=>TerrainLayerInfluenceSample.Create(f.GetProperty("Incoming").GetUInt32(),Entries(f.GetProperty("Boundaries")),Entries(f.GetProperty("Filters")),f.GetProperty("BoundaryGate").GetInt32(),f.GetProperty("FilterGate").GetInt32(),f.GetProperty("InvertBoundary").GetBoolean(),f.GetProperty("InvertFilter").GetBoolean(),(TerrainHeightArithmeticPrecision)f.GetProperty("Combine").GetInt32(),(TerrainHeightArithmeticPrecision)f.GetProperty("Product").GetInt32(),f.GetProperty("CreateMaximumQueries").GetInt32());
    private static TerrainLayerInfluenceEntry[] Entries(JsonElement f)=>f.EnumerateArray().Select(e=>new TerrainLayerInfluenceEntry(e.GetProperty("Enabled").GetBoolean(),e.GetProperty("Selector").GetUInt32(),Query(e.GetProperty("Query")))).ToArray();
    private static TerrainLayerInfluenceQuery? Query(JsonElement q)
    {
        if(q.ValueKind==JsonValueKind.Null)return null;var p=(TerrainHeightArithmeticPrecision)q.GetProperty("Precision").GetInt32();string type=q.GetProperty("Type").GetString()!;
        if(type=="Raw")return TerrainLayerInfluenceQuery.FromRawBits(q.GetProperty("Bits").GetUInt32(),p);
        var cp=(TerrainHeightArithmeticPrecision)q.GetProperty("CachePrecision").GetInt32();var xy=q.GetProperty("Coordinates");byte[] bytes=Convert.FromHexString(q.GetProperty("RuleHex").GetString()!);
        return type switch
        {
            "Circle"=>TerrainLayerInfluenceQuery.FromCircle(TerrainBoundaryCircleInfluence.Query(TerrainBoundaryCircleInput.Decode(bytes),xy[0].GetUInt32(),xy[1].GetUInt32(),cp,p)),
            "Rectangle"=>TerrainLayerInfluenceQuery.FromRectangle(TerrainBoundaryRectangleInfluence.Query(TerrainBoundaryRectangleInput.Decode(bytes),xy[0].GetUInt32(),xy[1].GetUInt32(),cp,p)),
            _=>TerrainLayerInfluenceQuery.FromPolygon(TerrainBoundaryPolygonInfluence.Query(TerrainBoundaryPolygonInput.Decode(bytes),xy[0].GetUInt32(),xy[1].GetUInt32(),cp,p))
        };
    }
    private static byte[] Chunk(string tag,byte[] bytes){byte[] result=new byte[bytes.Length+8];Encoding.ASCII.GetBytes(tag).CopyTo(result,0);BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(4),(uint)bytes.Length);bytes.CopyTo(result,8);return result;}
    private static byte[] Form(string tag,params byte[][] children)=>Chunk("FORM",Encoding.ASCII.GetBytes(tag).Concat(children.SelectMany(b=>b)).ToArray());
    private static TerrainHeightConstantInput Rule(uint enabled,uint operation,uint constant)
    {
        byte[] header=new byte[5],data=new byte[8];BinaryPrimitives.WriteUInt32LittleEndian(header,enabled);BinaryPrimitives.WriteUInt32LittleEndian(data,operation);BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(4),constant);
        var h=Form("IHDR",Form("0001",Chunk("DATA",header)));var rule=Form("AHCN",Form("0000",h,Chunk("DATA",data)));
        var layer=Form("LAYR",Form("0001",h,Chunk("ADTA",new byte[8]),rule));string[] tags=["SGRP","FGRP","RGRP","EGRP","MGRP"],versions=["0006","0008","0003","0002","0000"];
        return TerrainGeneratorInput.Decode(Form("TGEN",Form("0000",tags.Select((t,i)=>Form(t,Form(versions[i]))).Append(Form("LYRS",layer)).ToArray()))).Rules.Single().HeightConstant!;
    }
}
