using System.Text;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;
if(args.Length!=2||File.Exists(args[1])||new FileInfo(args[0]).Length is <1 or >524288)throw new ArgumentException("Bounded fresh fixture/output");
using var document=JsonDocument.Parse(File.ReadAllBytes(args[0]));var root=document.RootElement;
if(root.GetProperty("Version").GetInt32()!=6||root.GetProperty("OriginalTerrainParity").GetBoolean())throw new InvalidDataException("Frozen synthetic filter-only fixture");
var generator=TerrainGeneratorInput.Decode(Convert.FromHexString(root.GetProperty("GeneratorHex").GetString()!));var binding=TerrainGeneratorFractalFamilies.Bind(generator);
var selected=TerrainGeneratorHeightFractalRule.Select(generator,binding,root.GetProperty("SelectedRule").GetProperty("RuleIndex").GetInt32(),TerrainGeneratorHeightFractalRulePolicy.EnabledOperationalSubset);
var filter=TerrainGeneratorFractalFilterRule.Select(generator,binding,root.GetProperty("SelectedFilterRule").GetProperty("RuleIndex").GetInt32(),TerrainGeneratorFractalFilterRulePolicy.EnabledOperationalSubset);
var controls=new TerrainFamilyFilteredHeightFieldControls(new(53,64,64,TerrainHeightArithmeticPrecision.Binary64),TerrainHeightArithmeticPrecision.Binary64,TerrainHeightArithmeticPrecision.Binary64,TerrainHeightArithmeticPrecision.Binary64);
var chunks=new List<object>();int index=0;
foreach(var chunk in root.GetProperty("Chunks").EnumerateArray())
{
 var q=chunk.GetProperty("Request");var grid=TerrainSampleCoordinateGrid.Build(new(q.GetProperty("ChunkWidthBits").GetUInt32(),q.GetProperty("Tiles").GetInt32(),q.GetProperty("Margin").GetInt32(),q.GetProperty("First").GetInt32(),q.GetProperty("Second").GetInt32(),q.GetProperty("Scale").GetInt32()),TerrainHeightArithmeticPrecision.Binary24,TerrainHeightArithmeticPrecision.Binary24,TerrainHeightArithmeticPrecision.Binary24,TerrainHeightArithmeticPrecision.Binary24);
 uint[] prior=chunk.GetProperty("PriorHeightBits").EnumerateArray().Select(v=>v.GetUInt32()).ToArray();uint incoming=chunk.GetProperty("InfluenceBits").GetUInt32();
 var result=TerrainFamilyFilteredHeightField.Build(binding.Families,filter,selected.CopyEncodedBytes(),new(10,10,grid.Parameters.FirstOriginBits,grid.Parameters.SecondOriginBits,grid.Parameters.StepBits,grid.Parameters.StepBits),prior,Enumerable.Repeat(incoming,100).ToArray(),controls);
 for(int i=0;i<100;i++)if(!ReferenceEquals(result.Cells[i].Computed.Sample,result.Cells[i].Query.RetainedSample)||!ReferenceEquals(result.Cells[i].Computed.Sample,result.Cells[i].Carrier.RetainedFractalSample)||!ReferenceEquals(result.Cells[i].Height,result.HeightField.Cells[i]))throw new InvalidDataException("Actual retained receipt identity");
 if(result.SamplingCounts.FilterSamplerConstructions!=1||result.SamplingCounts.FilterSamplerDisposals!=1||result.SamplingCounts.FilterComputedCells!=100)throw new InvalidDataException("Truthful filter counts");
 chunks.Add(new{Chunk=index++,PriorHeightBits=result.CopyPriorHeightBits(),InfluenceBits=result.CopyProductBits(),HeightBits=result.HeightField.CopyHeightBits(),FilterSamples=result.Cells.Select(c=>new{Magnitude=c.Computed.Sample.Magnitude,Exponent=c.Computed.Sample.Exponent,Negative=c.Computed.Sample.IsNegative}),PositiveCells=result.HeightField.SamplingCounts.ComputedCells,SkippedCells=100-result.HeightField.SamplingCounts.ComputedCells,result.SamplingCounts});
}
File.WriteAllText(args[1],JsonSerializer.Serialize(new{FilterRuleIndex=filter.RuleIndex,FilterRuleSha256=filter.RuleSha256,Chunks=chunks,OriginalRuleUsed=true,OriginalTerrainParity=false})+"\n",new UTF8Encoding(false));
Console.WriteLine("PASS:400 owned pipeline cells with actual retained receipt identity");

