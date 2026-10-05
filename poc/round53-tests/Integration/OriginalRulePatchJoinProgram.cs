using System.Text;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;
if(args.Length!=2)throw new ArgumentException("Private fixture and output required");
if(new FileInfo(args[0]).Length is <1 or >524288||File.Exists(args[1]))throw new InvalidDataException("Bounded fresh fixture/output");
using var document=JsonDocument.Parse(File.ReadAllBytes(args[0]));var root=document.RootElement;
if(root.GetProperty("Version").GetInt32()!=5||root.GetProperty("OriginalTerrainParity").GetBoolean())throw new InvalidDataException("Explicit original-rule preview policy");
var generator=TerrainGeneratorInput.Decode(Convert.FromHexString(root.GetProperty("GeneratorHex").GetString()!));
var binding=TerrainGeneratorFractalFamilies.Bind(generator);
var selected=TerrainGeneratorHeightFractalRule.Select(generator,binding,root.GetProperty("SelectedRule").GetProperty("RuleIndex").GetInt32(),TerrainGeneratorHeightFractalRulePolicy.EnabledOperationalSubset);
byte[] rule=selected.CopyEncodedBytes();
if(!rule.AsSpan().SequenceEqual(Convert.FromHexString(root.GetProperty("RuleHex").GetString()!)))throw new InvalidDataException("Selected rule bytes");
var controls=new TerrainFamilyHeightFractalControls(53,64,64,TerrainHeightArithmeticPrecision.Binary64);
var chunks=new List<object>();int index=0;
foreach(var chunk in root.GetProperty("Chunks").EnumerateArray())
{
    var q=chunk.GetProperty("Request");
    var coordinates=TerrainSampleCoordinateGrid.Build(new(q.GetProperty("ChunkWidthBits").GetUInt32(),q.GetProperty("Tiles").GetInt32(),q.GetProperty("Margin").GetInt32(),q.GetProperty("First").GetInt32(),q.GetProperty("Second").GetInt32(),q.GetProperty("Scale").GetInt32()),TerrainHeightArithmeticPrecision.Binary24,TerrainHeightArithmeticPrecision.Binary24,TerrainHeightArithmeticPrecision.Binary24,TerrainHeightArithmeticPrecision.Binary24);
    var request=new TerrainFamilyHeightPatchRequest(10,10,coordinates.Parameters.FirstOriginBits,coordinates.Parameters.SecondOriginBits,coordinates.Parameters.StepBits,coordinates.Parameters.StepBits,chunk.GetProperty("InitialHeightBits").GetUInt32(),chunk.GetProperty("InfluenceBits").GetUInt32());
    var reference=TerrainFamilyHeightPatch.Build(binding.Families,rule,request,controls);
    var batch=TerrainFamilyHeightPatch.BuildSingleSampler(binding.Families,rule,request,controls);
    if(!reference.HeightBits.SequenceEqual(batch.HeightBits)||!reference.XAxisBits.SequenceEqual(batch.XAxisBits)||!reference.ZAxisBits.SequenceEqual(batch.ZAxisBits))throw new InvalidDataException("Stored result divergence");
    if(batch.AccountedWork>=reference.AccountedWork||batch.AggregateRouteBytes>=reference.AggregateRouteBytes)throw new InvalidDataException("No logical accounting benefit");
    for(int i=0;i<100;i++)
    {
        var a=reference.Samples[i];var b=batch.Samples[i];
        if(a.Sample.Magnitude!=b.Sample.Magnitude||a.Sample.Exponent!=b.Sample.Exponent||a.Sample.IsNegative!=b.Sample.IsNegative||a.Sample.ProjectionBits!=b.Sample.ProjectionBits||a.Origin!=b.Origin||!ReferenceEquals(b.Sample,b.Affector.Sample)||!ReferenceEquals(b.Family,selected.Family))throw new InvalidDataException("Retained sample divergence");
        if(b.Request.Column!=i%10||b.Request.Row!=i/10||b.Request.Width!=10||b.Request.Rows!=10||b.AccountingPolicy!=TerrainFamilyHeightFractalAccountingPolicy.SharedPreparationCell)throw new InvalidDataException("Truthful shared cell provenance");
    }
    chunks.Add(new{Chunk=index++,HeightBits=batch.CopyHeightBits(),Samples=batch.Samples.Select(s=>new{Magnitude=s.Sample.Magnitude,Exponent=s.Sample.Exponent,Negative=s.Sample.IsNegative}),BatchWork=batch.AccountedWork,ReferenceWork=reference.AccountedWork,BatchBytes=batch.AggregateRouteBytes,ReferenceBytes=reference.AggregateRouteBytes,BatchCounts=batch.SamplingCounts,ReferenceCounts=reference.SamplingCounts});
}
if(index!=4)throw new InvalidDataException("Four chunks required");
File.WriteAllText(args[1],JsonSerializer.Serialize(new{selected.RuleIndex,selected.RuleOffset,selected.RuleSha256,selected.FamilyId,Chunks=chunks,OriginalRuleUsed=true,OriginalTerrainParity=false})+"\n",new UTF8Encoding(false));
Console.WriteLine("PASS:400 original-rule retained samples and heights;4 batch samplers versus400 reference samplers");
