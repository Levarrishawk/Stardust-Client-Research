using System.Buffers.Binary;
using System.Text;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;
namespace Stardust.Core.SmokeTests;
public sealed record TerrainSuppliedHeightFilteredSequenceScenarioResult(IReadOnlyList<object> Cases,int OwnershipChecks);
public static class TerrainSuppliedHeightFilteredSequenceScenarios
{
 public static TerrainSuppliedHeightFilteredSequenceScenarioResult Run(string path)
 {
  using var doc=JsonDocument.Parse(File.ReadAllText(path));var cases=new List<object>();
  foreach(var f in doc.RootElement.EnumerateArray())
  {
   string name=f.GetProperty("Name").GetString()!;
   try
   {
    var source=f.GetProperty("Layers");var layers=source.EnumerateArray().Select(v=>v.ValueKind==JsonValueKind.Null?null!:Descriptor(v)).ToArray();
    var plan=TerrainHeightFilteredPlan.Create(f.GetProperty("Initial").GetUInt32(),layers,f.GetProperty("CreateMaximumLayers").GetInt32());
    TerrainHeightFilteredLimits? limits=null;
    if(f.TryGetProperty("Limits",out var l)){var t=l.GetProperty("Sequence");limits=new(new(t[0].GetInt32(),t[1].GetInt32(),t[2].GetInt32(),t[3].GetInt32(),t[4].GetInt32(),t[5].GetInt32(),t[6].GetInt32(),t[7].GetInt64(),t[8].GetInt64()),l.GetProperty("Work").GetInt64(),l.GetProperty("Slots").GetInt64());}
    var result=TerrainSuppliedHeightFilteredSequence.Evaluate(plan,limits);
    cases.Add(new {Name=name,Reject="None",result.FinalHeightBits,result.FinalHeightIsFinite,Stages=result.Stages.Select(st=>new {st.Index,Receipt=Child(st.Receipt,source[st.Index]),Bindings=st.Bindings.Select(b=>new {b.Index,State=(int)b.State,Height=b.Query?.CurrentHeightBits,Projection=b.Query?.QueryProjectionBits,b.CandidateBits}).ToArray()}).ToArray()});
   }
   catch(ArgumentOutOfRangeException){cases.Add(new {Name=name,Reject="Options"});}
   catch(InvalidDataException){cases.Add(new {Name=name,Reject="Input"});}
   catch(ArgumentNullException){cases.Add(new {Name=name,Reject="Input"});}
  }
  int checks=0;void Check(bool b){if(!b)throw new InvalidOperationException("Filtered check "+checks);checks++;}
  void Bad<T>(Action a) where T:Exception{try{a();}catch(T){checks++;return;}throw new InvalidOperationException("Expected "+typeof(T).Name+" at "+checks);}
  var p=TerrainHeightArithmeticPrecision.Binary53;
  var raw=TerrainLayerInfluenceQuery.FromRawBits(0x3f800000,p);
  var fh=Filter(1,0,0x40800000,1,0x3f800000);var rule=Rule(1,1,0x3f800000);
  TerrainLayerInfluenceEntry[] bs=[new(true,0,raw)];TerrainFilterHeightInput[] fs=[fh];TerrainHeightConstantInput[] rs=[rule];
  var layer=TerrainHeightFilteredLayer.Create(bs,fs,rs,0x3f800000,1,1,false,false,p,p,p,p);
  bs[0]=new(false,0,null);fs[0]=Filter(0,0,0,0,0);rs[0]=Rule(1,4,0);
  TerrainHeightFilteredLayer[] ls=[layer,layer];var planOwned=TerrainHeightFilteredPlan.Create(0x3f800000,ls);ls[0]=null!;
  var resultOwned=TerrainSuppliedHeightFilteredSequence.Evaluate(planOwned);
  Check(ReferenceEquals(layer.BoundaryEntries[0].Query,raw)&&ReferenceEquals(layer.Filters[0],fh)&&ReferenceEquals(layer.Rules[0],rule));
  Check(ReferenceEquals(planOwned.Layers[0],layer)&&ReferenceEquals(resultOwned.Plan,planOwned));
  Check(resultOwned.Stages[1].Bindings[0].Query!.CurrentHeightBits==resultOwned.Stages[0].Receipt.FinalHeightBits);
  var q=resultOwned.Stages[0].Bindings[0].Query!;var carrier=TerrainLayerInfluenceQuery.FromHeightFilter(q);
  Check((int)carrier.Source==4&&ReferenceEquals(carrier.HeightFilter,q)&&carrier.Precision==p&&carrier.ProjectionBits==q.QueryProjectionBits&&carrier.RawBits is null);
  Check((int)TerrainLayerInfluenceQuerySource.SuppliedRawBits==0&&(int)TerrainLayerInfluenceQuerySource.Circle==1&&(int)TerrainLayerInfluenceQuerySource.Rectangle==2&&(int)TerrainLayerInfluenceQuerySource.Polygon==3);
  Bad<ArgumentNullException>(()=>TerrainLayerInfluenceQuery.FromHeightFilter(null!));
  Check(((IList<TerrainHeightFilteredLayer>)planOwned.Layers).IsReadOnly&&((IList<TerrainFilterHeightInput>)layer.Filters).IsReadOnly);
  Bad<NotSupportedException>(()=>((IList<TerrainHeightFilteredStage>)resultOwned.Stages)[0]=default);
  Bad<NotSupportedException>(()=>((IList<TerrainHeightFilterBinding>)resultOwned.Stages[0].Bindings)[0]=default);
  var batch=TerrainSuppliedHeightFilteredSequence.EvaluateBatch(new[]{planOwned,planOwned});var copy=batch.CopyHeightBits();copy[0]=0;
  Check(batch.Results[0].FinalHeightBits==resultOwned.FinalHeightBits&&((IList<TerrainHeightFilteredReceipt>)batch.Results).IsReadOnly);
  var empty=TerrainHeightFilteredPlan.Create(0x7fc12345,Array.Empty<TerrainHeightFilteredLayer>(),0);
  Check(TerrainSuppliedHeightFilteredSequence.Evaluate(empty).FinalHeightBits==0x7fc12345);
  Check(TerrainSuppliedHeightFilteredSequence.EvaluateBatch(Array.Empty<TerrainHeightFilteredPlan>(),new(new(0,0,0,0,0,0,0,0,0),0,0)).Results.Count==0);
  foreach(var lim in new[]{new TerrainHeightFilteredLimits(new(MaximumSamples:0)),new(new(MaximumLayersPerSample:1)),new(new(MaximumTotalLayers:1)),new(new(MaximumQueriesPerLayer:1)),new(new(MaximumTotalQueries:3)),new(new(MaximumRulesPerLayer:0)),new(new(MaximumTotalRules:1)),new(new(MaximumOperations:5)),new(new(MaximumRuleReferenceSlots:4)),new(MaximumQueryWork:9),new(MaximumCandidateSlots:35)})Bad<InvalidDataException>(()=>TerrainSuppliedHeightFilteredSequence.Evaluate(planOwned,lim));
  foreach(var lim in new[]{new TerrainHeightFilteredLimits(MaximumQueryWork:-1),new(MaximumQueryWork:3145729),new(MaximumCandidateSlots:-1),new(MaximumCandidateSlots:9437185),new(new(MaximumSamples:-1)),new(new(MaximumOperations:5242881))})Bad<ArgumentOutOfRangeException>(()=>TerrainSuppliedHeightFilteredSequence.Evaluate(planOwned,lim));
  Bad<ArgumentNullException>(()=>TerrainSuppliedHeightFilteredSequence.Evaluate(null!));
  Bad<ArgumentNullException>(()=>TerrainSuppliedHeightFilteredSequence.EvaluateBatch(new TerrainHeightFilteredPlan[]{planOwned,null!}));
  Bad<ArgumentNullException>(()=>TerrainHeightFilteredPlan.Create(0,new TerrainHeightFilteredLayer[]{layer,null!}));
  Bad<InvalidDataException>(()=>TerrainHeightFilteredPlan.Create(0,new[]{layer},0));
  Bad<ArgumentOutOfRangeException>(()=>TerrainHeightFilteredPlan.Create(0,new[]{layer},4097));
  Bad<ArgumentNullException>(()=>TerrainHeightFilteredLayer.Create(bs,new TerrainFilterHeightInput[]{null!},rs,0,0,0,false,false,p,p,p,p));
  Bad<ArgumentOutOfRangeException>(()=>TerrainHeightFilteredLayer.Create(bs,new[]{fh},rs,0,0,0,false,false,(TerrainHeightArithmeticPrecision)0,p,p,p));
  Bad<InvalidDataException>(()=>TerrainHeightFilteredLayer.Create(bs,new[]{fh},rs,0,0,0,false,false,p,p,p,p,1));
  var late=TerrainHeightFilteredPlan.Create(0x7fc12345,new[]{layer});
  Bad<InvalidDataException>(()=>TerrainSuppliedHeightFilteredSequence.EvaluateBatch(new[]{planOwned,late}));
  var exact=new TerrainHeightFilteredLimits(new(1,2,2,2,4,1,2,6,5),10,36);
  Check(TerrainSuppliedHeightFilteredSequence.Evaluate(planOwned,exact).FinalHeightBits==resultOwned.FinalHeightBits);
  var batchExact=new TerrainHeightFilteredLimits(new(2,2,4,2,8,1,4,12,9),20,72);
  Check(TerrainSuppliedHeightFilteredSequence.EvaluateBatch(new[]{planOwned,planOwned},batchExact).Results.Count==2);
  foreach(var lim in new[]{batchExact with {Sequence=batchExact.Sequence! with {MaximumSamples=1}},batchExact with {Sequence=batchExact.Sequence! with {MaximumTotalLayers=3}},batchExact with {Sequence=batchExact.Sequence! with {MaximumTotalQueries=7}},batchExact with {Sequence=batchExact.Sequence! with {MaximumTotalRules=3}},batchExact with {Sequence=batchExact.Sequence! with {MaximumOperations=11}},batchExact with {Sequence=batchExact.Sequence! with {MaximumRuleReferenceSlots=8}},batchExact with {MaximumQueryWork=19},batchExact with {MaximumCandidateSlots=71}})
   Bad<InvalidDataException>(()=>TerrainSuppliedHeightFilteredSequence.EvaluateBatch(new[]{planOwned,planOwned},lim));
  try{TerrainSuppliedHeightFilteredSequence.EvaluateBatch(new[]{late,planOwned},batchExact with {MaximumQueryWork=0});throw new InvalidOperationException("Missing preflight.");}
  catch(InvalidDataException e){Check(e.Message=="Filtered aggregate reservation.");}
  Check(resultOwned.Stages.Count==2&&batch.Results.Count==2);
  return new(cases.AsReadOnly(),checks);
 }
 private static TerrainHeightFilteredLayer Descriptor(JsonElement f)
 {
  var l=f.GetProperty("Layer");var filters=f.GetProperty("Filters").EnumerateArray().Select(x=>TerrainFilterHeightInput.Decode(Convert.FromHexString(x.GetProperty("Hex").GetString()!))).ToArray();
  var rules=f.GetProperty("Rules").EnumerateArray().Select(x=>x.ValueKind==JsonValueKind.Null?null!:TerrainGeneratorInput.Decode(Convert.FromHexString(x.GetProperty("GeneratorHex").GetString()!)).Rules.Single().HeightConstant!).ToArray();
  return TerrainHeightFilteredLayer.Create(Entries(l.GetProperty("Boundaries")),filters,rules,l.GetProperty("Incoming").GetUInt32(),l.GetProperty("BoundaryGate").GetInt32(),l.GetProperty("FilterGate").GetInt32(),l.GetProperty("InvertBoundary").GetBoolean(),l.GetProperty("InvertFilter").GetBoolean(),(TerrainHeightArithmeticPrecision)f.GetProperty("QueryPrecision").GetInt32(),(TerrainHeightArithmeticPrecision)l.GetProperty("Combine").GetInt32(),(TerrainHeightArithmeticPrecision)l.GetProperty("Product").GetInt32(),(TerrainHeightArithmeticPrecision)f.GetProperty("AffectorPrecision").GetInt32(),l.GetProperty("CreateMaximumQueries").GetInt32(),f.GetProperty("CreateMaximumRules").GetInt32());
 }
 private static TerrainFilterHeightInput Filter(uint enabled,uint lo,uint hi,uint selector,uint amount)
 {
  byte[] header=new byte[5],data=new byte[16];BinaryPrimitives.WriteUInt32LittleEndian(header,enabled);uint[] words=[lo,hi,selector,amount];for(int i=0;i<4;i++)BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(i*4),words[i]);
  return TerrainFilterHeightInput.Decode(Form("FHGT",Form("0002",Form("IHDR",Form("0001",Chunk("DATA",header))),Chunk("DATA",data))));
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



