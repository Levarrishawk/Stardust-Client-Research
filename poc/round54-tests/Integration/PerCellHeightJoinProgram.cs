using System.Text;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;
if(args.Length!=2||File.Exists(args[1])||new FileInfo(args[0]).Length is <1 or >524288)throw new ArgumentException("Bounded fresh fixture/output required");
using var document=JsonDocument.Parse(File.ReadAllBytes(args[0]));var root=document.RootElement;
if(root.GetProperty("Version").GetInt32()!=6||root.GetProperty("OriginalTerrainParity").GetBoolean())throw new InvalidDataException("Explicit synthetic composition");
var generator=TerrainGeneratorInput.Decode(Convert.FromHexString(root.GetProperty("GeneratorHex").GetString()!));
var binding=TerrainGeneratorFractalFamilies.Bind(generator);
var heightRule=TerrainGeneratorHeightFractalRule.Select(generator,binding,root.GetProperty("SelectedRule").GetProperty("RuleIndex").GetInt32(),TerrainGeneratorHeightFractalRulePolicy.EnabledOperationalSubset);
var filterRule=TerrainGeneratorFractalFilterRule.Select(generator,binding,root.GetProperty("SelectedFilterRule").GetProperty("RuleIndex").GetInt32(),TerrainGeneratorFractalFilterRulePolicy.EnabledOperationalSubset);
byte[] rule=heightRule.CopyEncodedBytes();
if(!rule.AsSpan().SequenceEqual(Convert.FromHexString(root.GetProperty("RuleHex").GetString()!))||!filterRule.CopyEncodedBytes().AsSpan().SequenceEqual(Convert.FromHexString(root.GetProperty("FilterRuleHex").GetString()!)))throw new InvalidDataException("Actual selected bytes");
var controls=new TerrainFamilyHeightFractalControls(53,64,64,TerrainHeightArithmeticPrecision.Binary64);
var chunks=new List<object>();int index=0;
bool Positive(uint v)=>(v&0x80000000)==0&&(v&0x7fffffff)!=0;
foreach(var chunk in root.GetProperty("Chunks").EnumerateArray())
{
 var q=chunk.GetProperty("Request");
 var grid=TerrainSampleCoordinateGrid.Build(new(q.GetProperty("ChunkWidthBits").GetUInt32(),q.GetProperty("Tiles").GetInt32(),q.GetProperty("Margin").GetInt32(),q.GetProperty("First").GetInt32(),q.GetProperty("Second").GetInt32(),q.GetProperty("Scale").GetInt32()),TerrainHeightArithmeticPrecision.Binary24,TerrainHeightArithmeticPrecision.Binary24,TerrainHeightArithmeticPrecision.Binary24,TerrainHeightArithmeticPrecision.Binary24);
 var prior=TerrainFamilyHeightPatch.BuildSingleSampler(binding.Families,rule,new(10,10,grid.Parameters.FirstOriginBits,grid.Parameters.SecondOriginBits,grid.Parameters.StepBits,grid.Parameters.StepBits,chunk.GetProperty("InitialHeightBits").GetUInt32(),chunk.GetProperty("InfluenceBits").GetUInt32()),controls);
 var requests=Enumerable.Range(0,100).Select(i=>new TerrainConditionalFractalSampler.Request(i%10,i/10,prior.XAxisBits[i%10],prior.ZAxisBits[i/10])).ToArray();
 uint[] weights=new uint[100];object[] filterSamples=new object[100];
 using(var sampler=TerrainConditionalFractalSampler.CreateForSupportedModes(filterRule.Family.Raw,10,10,53,64,1048576,1048576))
 {
  var computed=sampler.Query(requests,64,2097152,16777216);
  for(int i=0;i<100;i++)
  {
   var sample=computed[i].Sample;
   var query=TerrainFilterFractalInfluence.Query(filterRule.Input,sample,TerrainHeightArithmeticPrecision.Binary64);
   var carrier=TerrainLayerInfluenceQuery.FromRetainedFractalFilter(query);
   var combined=TerrainLayerInfluence.Evaluate(TerrainLayerInfluenceSample.Create(chunk.GetProperty("InfluenceBits").GetUInt32(),[],[new(true,filterRule.Fields.FeatheringTypeBits,carrier)],1,1,false,false,TerrainHeightArithmeticPrecision.Binary64,TerrainHeightArithmeticPrecision.Binary64));
   weights[i]=combined.ProductBits;filterSamples[i]=new{Magnitude=sample.Magnitude,Exponent=sample.Exponent,Negative=sample.IsNegative};
  }
 }
 var request=new TerrainFamilyHeightFieldRequest(10,10,grid.Parameters.FirstOriginBits,grid.Parameters.SecondOriginBits,grid.Parameters.StepBits,grid.Parameters.StepBits);
 var field=TerrainFamilyHeightField.Build(binding.Families,rule,request,prior.CopyHeightBits(),weights,controls);
 uint[] reference=prior.CopyHeightBits();
 for(int i=0;i<100;i++)if(Positive(weights[i]))
 {
  var receipt=TerrainFamilyHeightFractal.Evaluate(binding.Families,rule,new(10,10,i%10,i/10,field.XAxisBits[i%10],field.ZAxisBits[i/10],reference[i],weights[i]),controls);
  reference[i]=receipt.Affector.Consumer.ResultHeightBits;
  var applied=field.Cells[i].Applied??throw new InvalidDataException("Missing applied receipt");
  if(applied.Sample.Magnitude!=receipt.Sample.Magnitude||applied.Sample.Exponent!=receipt.Sample.Exponent||applied.Sample.IsNegative!=receipt.Sample.IsNegative||!ReferenceEquals(applied.Sample,applied.Affector.Sample))throw new InvalidDataException("Retained height sample");
 }
 if(!reference.SequenceEqual(field.HeightBits))throw new InvalidDataException("Independent route divergence");
 uint[] mixed=(uint[])weights.Clone();for(int i=0;i<100;i++)if(i%3!=0)mixed[i]=i%3==1?0x80000000u:0xbf800000u;
 var mixedField=TerrainFamilyHeightField.Build(binding.Families,rule,request,prior.CopyHeightBits(),mixed,controls);
 var zeroField=TerrainFamilyHeightField.Build(binding.Families,rule,request,prior.CopyHeightBits(),new uint[100],controls);
 if(zeroField.SamplingCounts.SamplerConstructions!=0||zeroField.SamplingCounts.ComputedCells!=0||!zeroField.HeightBits.SequenceEqual(prior.HeightBits)||zeroField.Cells.Any(c=>c.Applied is not null))throw new InvalidDataException("Truthful all-zero field");
 for(int i=0;i<100;i++)if(!Positive(mixed[i])&&(mixedField.HeightBits[i]!=prior.HeightBits[i]||mixedField.Cells[i].Applied is not null))throw new InvalidDataException("Exact skipped state");
 chunks.Add(new{Chunk=index++,PriorHeightBits=prior.CopyHeightBits(),InfluenceBits=weights,HeightBits=field.CopyHeightBits(),FilterSamples=filterSamples,PositiveCells=field.SamplingCounts.ComputedCells,SkippedCells=100-field.SamplingCounts.ComputedCells,field.SamplingCounts,MixedHeightBits=mixedField.CopyHeightBits(),MixedInfluenceBits=mixed,MixedCounts=mixedField.SamplingCounts,ZeroCounts=zeroField.SamplingCounts});
}
File.WriteAllText(args[1],JsonSerializer.Serialize(new{FilterRuleIndex=filterRule.RuleIndex,FilterRuleSha256=filterRule.RuleSha256,Chunks=chunks,OriginalRuleUsed=true,OriginalTerrainParity=false})+"\n",new UTF8Encoding(false));
Console.WriteLine("PASS:400 selected-filter field cells; reference, mixed and zero-state checks");

