using System.Text;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;
if(args.Length!=2||File.Exists(args[1])||new FileInfo(args[0]).Length is <1 or >4194304)throw new ArgumentException("Bounded fresh layer fixture/output");
using var document=JsonDocument.Parse(File.ReadAllBytes(args[0]));var root=document.RootElement;
const string policy="owned_layer_first_curve_modern_approximation";
if(root.GetProperty("Version").GetInt32()!=8||root.GetProperty("HeightPolicy").GetString()!=policy||root.GetProperty("OriginalNumericParity").GetBoolean())throw new InvalidDataException("Explicit approximate layer policy");
var generator=TerrainGeneratorInput.Decode(Convert.FromHexString(root.GetProperty("GeneratorHex").GetString()!));
var binding=TerrainGeneratorFractalFamilies.Bind(generator);
var slice=TerrainGeneratorHeightLayerSlice.Select(generator,binding,3,TerrainGeneratorHeightLayerSlicePolicy.TypedEvidence);
var chunks=new List<object>();int index=0;
foreach(var chunk in root.GetProperty("Chunks").EnumerateArray()){
 var r=chunk.GetProperty("Request");
 var grid=TerrainSampleCoordinateGrid.Build(new(r.GetProperty("ChunkWidthBits").GetUInt32(),r.GetProperty("Tiles").GetInt32(),r.GetProperty("Margin").GetInt32(),r.GetProperty("First").GetInt32(),r.GetProperty("Second").GetInt32(),r.GetProperty("Scale").GetInt32()),
  TerrainHeightArithmeticPrecision.Binary24,TerrainHeightArithmeticPrecision.Binary24,TerrainHeightArithmeticPrecision.Binary24,TerrainHeightArithmeticPrecision.Binary24);
 var boundary=TerrainHeightLayerBoundaryAdmission.Evaluate(slice,grid,new(TerrainHeightArithmeticPrecision.Binary64,TerrainHeightArithmeticPrecision.Binary64,TerrainHeightArithmeticPrecision.Binary64));
 uint[] incoming=chunk.GetProperty("IncomingWeightBits").EnumerateArray().Select(v=>v.GetUInt32()).ToArray();
 uint[] prior=chunk.GetProperty("PriorHeightBits").EnumerateArray().Select(v=>v.GetUInt32()).ToArray();
 var filtered=TerrainHeightLayerFilteredInfluence.Evaluate(boundary,slice,grid,incoming,new(53,64,64,TerrainHeightArithmeticPrecision.Binary64,TerrainHeightArithmeticPrecision.Binary64,TerrainHeightArithmeticPrecision.Binary64));
 int n=grid.Layer.Count;
 var requests=Enumerable.Range(0,n*n).Select(i=>{var pair=grid.Layer.GetPair(i%n,i/n);return new TerrainConditionalFractalSampler.Request(pair.Column,pair.Row,pair.FirstBits,pair.SecondBits);}).ToArray();
 var batch=TerrainFirstCurveFractalBatch.Build(slice.Height.Family.Raw.CopyEncodedBytes(),n,n,requests,new(53,64,64),TerrainFirstCurveNumericPolicy.ModernBinary64LogPowApproximation);
 var field=TerrainHeightLayerFirstCurveConsumer.Evaluate(filtered,slice,grid,batch,prior,TerrainHeightArithmeticPrecision.Binary64,TerrainFirstCurveNumericPolicy.ModernBinary64LogPowApproximation);
 chunks.Add(new{Chunk=index++,PriorHeightBits=field.PriorHeightBits,InfluenceBits=filtered.CopyProductBits(),HeightBits=field.CopyHeightBits(),FilterCounts=filtered.Counts,HeightCounts=field.Counts,
  CurveCells=batch.Cells.Select(c=>new{c.Approximation.BaseF32Bits,c.Approximation.OutputBinary64Bits,KernelSample=c.KernelResult.Sample}).ToArray()});
}
File.WriteAllText(args[1],JsonSerializer.Serialize(new{Policy=policy,LayerIndex=slice.LayerIndex,binding.GeneratorSha256,slice.LayerSha256,HeightRuleSha256=slice.Height.RuleSha256,OriginalNumericParity=false,OriginalTerrainParity=false,Chunks=chunks})+"\n",new UTF8Encoding(false));
Console.WriteLine("PASS: original-layer modern first-curve consumer");

