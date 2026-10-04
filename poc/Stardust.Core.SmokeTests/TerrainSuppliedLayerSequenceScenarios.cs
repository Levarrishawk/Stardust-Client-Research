using System.Buffers.Binary;
using System.Text;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;

namespace Stardust.Core.SmokeTests;
public sealed record TerrainSuppliedLayerSequenceScenarioResult(IReadOnlyList<object> Cases,int OwnershipChecks);
public static class TerrainSuppliedLayerSequenceScenarios
{
    public static TerrainSuppliedLayerSequenceScenarioResult Run(string path)
    {
        using var document=JsonDocument.Parse(File.ReadAllText(path));var cases=new List<object>();
        foreach(var f in document.RootElement.EnumerateArray())
        {
            string name=f.GetProperty("Name").GetString()!;
            try
            {
                var source=f.GetProperty("Layers");var layers=source.EnumerateArray().Select(v=>v.ValueKind==JsonValueKind.Null?null!:Descriptor(v)).ToArray();
                var plan=TerrainSuppliedLayerSequencePlan.Create(f.GetProperty("Initial").GetUInt32(),layers,f.GetProperty("CreateMaximumLayers").GetInt32());
                TerrainSuppliedLayerSequenceLimits? fixtureLimits=null;
                if(f.TryGetProperty("Limits",out var l))fixtureLimits=new(l[0].GetInt32(),l[1].GetInt32(),l[2].GetInt32(),l[3].GetInt32(),l[4].GetInt32(),l[5].GetInt32(),l[6].GetInt32(),l[7].GetInt64(),l[8].GetInt64());
                var result=TerrainSuppliedLayerSequence.Evaluate(plan,fixtureLimits);
                cases.Add(new {Name=name,Reject="None",plan.Charge,result.FinalHeightBits,result.FinalHeightIsFinite,Stages=result.Stages.Select(s=>new {s.Index,Receipt=Child(s.Receipt,source[s.Index])}).ToArray()});
            }
            catch(ArgumentOutOfRangeException){cases.Add(new {Name=name,Reject="Options"});}
            catch(InvalidDataException){cases.Add(new {Name=name,Reject="Input"});}
            catch(ArgumentNullException){cases.Add(new {Name=name,Reject="Input"});}
        }
        int checks=0;
        void Check(bool x){if(!x)throw new InvalidOperationException("Sequence check "+checks);checks++;}
        void Bad<T>(Action action) where T:Exception{try{action();}catch(T){checks++;return;}throw new InvalidOperationException("Expected "+typeof(T).Name+" at "+checks);}
        var p=TerrainHeightArithmeticPrecision.Binary53;var q=TerrainLayerInfluenceQuery.FromRawBits(0x3f000000,p);
        var layer=TerrainLayerInfluenceSample.Create(0x3f800000,new TerrainLayerInfluenceEntry[]{new(true,0,q)},new TerrainLayerInfluenceEntry[]{new(true,0,q)},1,1,false,false,p,p);
        var add=Rule(1,1,0x3f800000);var multiply=Rule(1,3,0x40000000);TerrainHeightConstantInput[] rules=[add,multiply];
        var descriptor=TerrainSuppliedLayerSequenceLayer.Create(layer,rules,p,2);rules[0]=Rule(1,4,0);
        TerrainSuppliedLayerSequenceLayer[] inputs=[descriptor,descriptor];var planOwned=TerrainSuppliedLayerSequencePlan.Create(0x3f800000,inputs,2);inputs[0]=TerrainSuppliedLayerSequenceLayer.Create(layer,Array.Empty<TerrainHeightConstantInput>(),p);
        var limits=new TerrainSuppliedLayerSequenceLimits(1,2,2,2,4,2,4,16,10);var retained=TerrainSuppliedLayerSequence.Evaluate(planOwned,limits);
        Check(retained.FinalHeightBits==0x40840000&&retained.Stages[0].Receipt.FinalHeightBits==0x40100000);
        Check(retained.Stages[1].Receipt.Sample.InitialHeightBits==retained.Stages[0].Receipt.FinalHeightBits&&retained.Stages[0].Receipt.Sample.InitialHeightBits==0x3f800000);
        Check(ReferenceEquals(descriptor.Rules[0],add)&&ReferenceEquals(planOwned.Layers[0],descriptor)&&ReferenceEquals(retained.Plan,planOwned));
        Check(planOwned.Charge==new TerrainSuppliedLayerSequenceCharge(2,4,4,16,10));
        Check(retained.Stages.All(s=>ReferenceEquals(s.Receipt.Sample.Layer,layer)&&s.Receipt.Sample.AffectorPrecision==p));
        Check(((IList<TerrainSuppliedLayerSequenceLayer>)planOwned.Layers).IsReadOnly&&((IList<TerrainSuppliedLayerSequenceStage>)retained.Stages).IsReadOnly);
        Bad<NotSupportedException>(()=>((IList<TerrainSuppliedLayerSequenceLayer>)planOwned.Layers)[0]=descriptor);
        Bad<NotSupportedException>(()=>((IList<TerrainSuppliedLayerSequenceStage>)retained.Stages)[0]=default);
        foreach(var l in new[]{limits with {MaximumSamples=0},limits with {MaximumLayersPerSample=1},limits with {MaximumTotalLayers=1},limits with {MaximumQueriesPerLayer=1},limits with {MaximumTotalQueries=3},limits with {MaximumRulesPerLayer=1},limits with {MaximumTotalRules=3},limits with {MaximumOperations=15},limits with {MaximumRuleReferenceSlots=9}})
            Bad<InvalidDataException>(()=>TerrainSuppliedLayerSequence.Evaluate(planOwned,l));
        foreach(var l in new[]{limits with {MaximumSamples=-1},limits with {MaximumSamples=65537},limits with {MaximumLayersPerSample=-1},limits with {MaximumLayersPerSample=4097},limits with {MaximumTotalLayers=-1},limits with {MaximumTotalLayers=65537},limits with {MaximumQueriesPerLayer=-1},limits with {MaximumQueriesPerLayer=4097},limits with {MaximumTotalQueries=-1},limits with {MaximumTotalQueries=1048577},limits with {MaximumRulesPerLayer=-1},limits with {MaximumRulesPerLayer=4097},limits with {MaximumTotalRules=-1},limits with {MaximumTotalRules=1048577},limits with {MaximumOperations=-1},limits with {MaximumOperations=5242881},limits with {MaximumRuleReferenceSlots=-1},limits with {MaximumRuleReferenceSlots=2101249}})
            Bad<ArgumentOutOfRangeException>(()=>TerrainSuppliedLayerSequence.Evaluate(planOwned,l));
        var empty=TerrainSuppliedLayerSequencePlan.Create(0x7fc12345,Array.Empty<TerrainSuppliedLayerSequenceLayer>(),0);
        var emptyResult=TerrainSuppliedLayerSequence.Evaluate(empty,new(1,0,0,0,0,0,0,0,0));
        Check(emptyResult.FinalHeightBits==0x7fc12345&&!emptyResult.FinalHeightIsFinite&&emptyResult.Stages.Count==0);
        Check(TerrainSuppliedLayerSequence.EvaluateBatch(Array.Empty<TerrainSuppliedLayerSequencePlan>(),new(0,0,0,0,0,0,0,0,0)).Charge==default);
        Check(TerrainSuppliedLayerSequence.EvaluateBatch(Array.Empty<TerrainSuppliedLayerSequencePlan>(),new(65536,4096,65536,4096,1048576,4096,1048576,5242880,2101248)).Results.Count==0);
        TerrainSuppliedLayerSequencePlan[] plans=[planOwned,planOwned];var batch=TerrainSuppliedLayerSequence.EvaluateBatch(plans,new(2,2,4,2,8,2,8,32,18));plans[0]=empty;
        Check(batch.Results.Count==2&&batch.Charge==new TerrainSuppliedLayerSequenceCharge(4,8,8,32,18));
        Check(ReferenceEquals(batch.Results[0].Plan,planOwned));var heights=batch.CopyHeightBits();heights[0]=0;Check(batch.Results[0].FinalHeightBits==0x40840000);
        Check(((IList<TerrainSuppliedLayerSequenceReceipt>)batch.Results).IsReadOnly);
        foreach(var l in new[]{new TerrainSuppliedLayerSequenceLimits(1,2,4,2,8,2,8,32,18),new(2,2,3,2,8,2,8,32,18),new(2,2,4,2,7,2,8,32,18),new(2,2,4,2,8,2,7,32,18),new(2,2,4,2,8,2,8,31,18),new(2,2,4,2,8,2,8,32,17)})
            Bad<InvalidDataException>(()=>TerrainSuppliedLayerSequence.EvaluateBatch(new[]{planOwned,planOwned},l));
        Bad<ArgumentNullException>(()=>TerrainSuppliedLayerSequence.Evaluate(null!));
        Bad<ArgumentNullException>(()=>TerrainSuppliedLayerSequence.EvaluateBatch(new TerrainSuppliedLayerSequencePlan[]{empty,null!}));
        Bad<ArgumentNullException>(()=>TerrainSuppliedLayerSequencePlan.Create(0,new TerrainSuppliedLayerSequenceLayer[]{descriptor,null!}));
        Bad<ArgumentOutOfRangeException>(()=>TerrainSuppliedLayerSequencePlan.Create(0,inputs,-1));
        Bad<ArgumentOutOfRangeException>(()=>TerrainSuppliedLayerSequencePlan.Create(0,inputs,4097));
        Bad<InvalidDataException>(()=>TerrainSuppliedLayerSequencePlan.Create(0,inputs,1));
        Bad<ArgumentNullException>(()=>TerrainSuppliedLayerSequenceLayer.Create(null!,rules,p));
        Bad<ArgumentNullException>(()=>TerrainSuppliedLayerSequenceLayer.Create(layer,new TerrainHeightConstantInput[]{add,null!},p));
        Bad<ArgumentOutOfRangeException>(()=>TerrainSuppliedLayerSequenceLayer.Create(layer,rules,(TerrainHeightArithmeticPrecision)0));
        var late=TerrainSuppliedLayerSequencePlan.Create(0x7f7fffff,new[]{descriptor,descriptor});
        Bad<InvalidDataException>(()=>TerrainSuppliedLayerSequence.EvaluateBatch(new[]{planOwned,late},new(2,2,4,2,8,2,8,32,18)));
        try{TerrainSuppliedLayerSequence.EvaluateBatch(new[]{late,planOwned},new(2,2,4,2,8,2,8,32,17));throw new InvalidOperationException("Missing complete reservation.");}
        catch(InvalidDataException e){Check(e.Message=="Flat sequence rule reference budget.");}
        Check(retained.FinalHeightBits==0x40840000&&batch.Results.Count==2&&planOwned.Charge.RuleReferenceSlots==10);
        var many=TerrainSuppliedLayerSequenceLayer.Create(layer,Enumerable.Repeat(Rule(0,0,0x7fc12345),4096).ToArray(),p,4096);
        var capped=TerrainSuppliedLayerSequencePlan.Create(0x7fc12345,new[]{many});
        Check(TerrainSuppliedLayerSequence.Evaluate(capped,new(1,1,1,2,2,4096,4096,20480,12288)).Stages[0].Receipt.Visits.Count==4096);
        Bad<InvalidDataException>(()=>TerrainSuppliedLayerSequence.Evaluate(capped,new(1,1,1,2,2,4096,4096,20480,12287)));
        var zeroLayer=TerrainLayerInfluenceSample.Create(0,Array.Empty<TerrainLayerInfluenceEntry>(),Array.Empty<TerrainLayerInfluenceEntry>(),0,0,false,false,p,p,0);
        var noOp=TerrainSuppliedLayerSequenceLayer.Create(zeroLayer,Array.Empty<TerrainHeightConstantInput>(),p,0);
        var fullPlan=TerrainSuppliedLayerSequencePlan.Create(0x80000000,Enumerable.Repeat(noOp,4096).ToArray(),4096);
        Check(TerrainSuppliedLayerSequence.Evaluate(fullPlan,new(1,4096,4096,0,0,0,0,0,0)).Stages.Count==4096);
        Bad<InvalidDataException>(()=>TerrainSuppliedLayerSequence.Evaluate(fullPlan,new(1,4096,4095,0,0,0,0,0,0)));
        return new(cases.AsReadOnly(),checks);
    }
    private static TerrainSuppliedLayerSequenceLayer Descriptor(JsonElement f)
    {
        var layer=Layer(f.GetProperty("Layer"));var rules=f.GetProperty("Rules").EnumerateArray().Select(r=>r.ValueKind==JsonValueKind.Null?null!:TerrainGeneratorInput.Decode(Convert.FromHexString(r.GetProperty("GeneratorHex").GetString()!)).Rules.Single().HeightConstant!).ToArray();
        return TerrainSuppliedLayerSequenceLayer.Create(layer,rules,(TerrainHeightArithmeticPrecision)f.GetProperty("AffectorPrecision").GetInt32(),f.GetProperty("CreateMaximumRules").GetInt32());
    }
    private static object Child(TerrainSuppliedLayerHeightReceipt r,JsonElement f)=>new {Name=f.GetProperty("Name").GetString()!,Reject="None",Influence=Influence(r.Influence,f.GetProperty("Layer").GetProperty("Name").GetString()!),r.Sample.OperationCharge,r.FinalHeightBits,r.FinalHeightIsFinite,r.Visits};
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


