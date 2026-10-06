using System.Text;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;
if(args.Length!=2||File.Exists(args[1])||new FileInfo(args[0]).Length is <1 or >4194304)throw new ArgumentException("Bounded fresh layer fixture/output");
using var document=JsonDocument.Parse(File.ReadAllBytes(args[0]));var root=document.RootElement;
const string policy="owned_layer_sparse_first_curve_modern_approximation";
if(root.GetProperty("Version").GetInt32()!=9||root.GetProperty("HeightPolicy").GetString()!=policy||root.GetProperty("OriginalNumericParity").GetBoolean())throw new InvalidDataException("Explicit approximate layer policy");
var generator=TerrainGeneratorInput.Decode(Convert.FromHexString(root.GetProperty("GeneratorHex").GetString()!));
var shaderFamilies=TerrainGeneratorShaderFamilies.Bind(generator);
var binding=TerrainGeneratorFractalFamilies.Bind(generator);
var slice=TerrainGeneratorHeightLayerSlice.Select(generator,binding,3,TerrainGeneratorHeightLayerSlicePolicy.TypedEvidence);
var chunks=new List<object>();var assemblies=new List<TerrainModernHeightPatch>();int index=0;
foreach(var chunk in root.GetProperty("Chunks").EnumerateArray()){
 var r=chunk.GetProperty("Request");
 var grid=TerrainSampleCoordinateGrid.Build(new(r.GetProperty("ChunkWidthBits").GetUInt32(),r.GetProperty("Tiles").GetInt32(),r.GetProperty("Margin").GetInt32(),r.GetProperty("First").GetInt32(),r.GetProperty("Second").GetInt32(),r.GetProperty("Scale").GetInt32()),
  TerrainHeightArithmeticPrecision.Binary24,TerrainHeightArithmeticPrecision.Binary24,TerrainHeightArithmeticPrecision.Binary24,TerrainHeightArithmeticPrecision.Binary24);
 var boundary=TerrainHeightLayerBoundaryAdmission.Evaluate(slice,grid,new(TerrainHeightArithmeticPrecision.Binary64,TerrainHeightArithmeticPrecision.Binary64,TerrainHeightArithmeticPrecision.Binary64));
 uint[] incoming=chunk.GetProperty("IncomingWeightBits").EnumerateArray().Select(v=>v.GetUInt32()).ToArray();
 uint[] prior=chunk.GetProperty("PriorHeightBits").EnumerateArray().Select(v=>v.GetUInt32()).ToArray();
 var filtered=TerrainHeightLayerFilteredInfluence.Evaluate(boundary,slice,grid,incoming,new(53,64,64,TerrainHeightArithmeticPrecision.Binary64,TerrainHeightArithmeticPrecision.Binary64,TerrainHeightArithmeticPrecision.Binary64));
 int n=grid.Layer.Count;
 var evaluation=TerrainHeightLayerSparseFirstCurveEvaluation.Evaluate(filtered,slice,grid,prior,new(53,64,64),TerrainHeightArithmeticPrecision.Binary64,TerrainFirstCurveNumericPolicy.ModernBinary64LogPowApproximation);
 var batch=evaluation.Batch;var field=evaluation.Consumer;
 var initial=TerrainCoordinateHeightPatch.Build(grid,field.CopyHeightBits(),Enumerable.Repeat(System.Numerics.Vector3.UnitY,n*n).ToArray(),Enumerable.Repeat(0xFF20A060u,n*n).ToArray(),0,1);
 var assembly=TerrainModernHeightPatch.Build(initial,TerrainModernNormalPolicy.ModernBinary64CentralDifference);
 assemblies.Add(assembly);var normals=assembly.NormalField;var patch=assembly.Patch;
 chunks.Add(new{Chunk=index++,PriorHeightBits=field.PriorHeightBits,InfluenceBits=filtered.CopyProductBits(),HeightBits=field.CopyHeightBits(),FilterCounts=filtered.Counts,HeightCounts=field.Counts,
  NormalBits=normals.Normals.Select(v=>new[]{BitConverter.SingleToUInt32Bits(v.X),BitConverter.SingleToUInt32Bits(v.Y),BitConverter.SingleToUInt32Bits(v.Z)}).ToArray(),PositionHeightBits=normals.CopyPositionHeightBits(),NormalCount=normals.Count,NormalSourceActual=ReferenceEquals(normals.Source,initial),
  SparseReservation=evaluation.Reservation,OwnedSamplerFactories=evaluation.OwnedSamplerFactories,OwnedSamplerQueryBatches=evaluation.OwnedSamplerQueryBatches,OwnedSamplerDisposals=evaluation.OwnedSamplerDisposals,
  SparseReferences=ReferenceEquals(evaluation.Filtered,filtered)&&ReferenceEquals(evaluation.Batch,field.Batch)&&ReferenceEquals(evaluation.Consumer,field),
  ModernAssembly=new{assembly.NormalBuilds,assembly.FinalPatchBuilds,assembly.OwnCopyBytes,assembly.NormalCopyBytes,assembly.FinalReservedCopyBytes,assembly.AggregateCopyBytes,assembly.AggregateArrayBytes,assembly.AggregateWork,assembly.PatchCount,assembly.SelectedSamples,assembly.FinalBuilderSupportsCancellation},
  AssemblyReferences=ReferenceEquals(assembly.Source,initial)&&ReferenceEquals(normals.Source,initial)&&ReferenceEquals(patch.Coordinates,grid),
  CurveCells=batch.Cells.Select(c=>new{c.Index,c.RequestIndex,c.Approximation.BaseF32Bits,c.Approximation.OutputBinary64Bits,KernelSample=c.KernelResult.Sample}).ToArray()});
}
var patchSet=TerrainModernHeightPatchSet.Build(assemblies.ToArray(),TerrainModernPatchEdgePolicy.RequireBitIdenticalActiveSharedEdges,TerrainModernPatchNormalPolicy.RequireBitIdenticalSharedNormals);
var querySet=TerrainModernPatchSetSurfaceQuery.Build(patchSet);
var locations=new List<TerrainPatchSurfaceLocation>();for(int z=-8;z<=8;z+=2)for(int x=-8;x<=8;x+=2)locations.Add(new(x,z));
locations.AddRange(new TerrainPatchSurfaceLocation[]{new(-10,0),new(10,0),new(0,-10),new(0,10)});
var hits=querySet.QueryBatch(locations.ToArray());var motionChecks=RetainedSetMotionChecks.Run(querySet);
File.WriteAllText(args[1],JsonSerializer.Serialize(new{Policy=policy,LayerIndex=slice.LayerIndex,binding.GeneratorSha256,slice.LayerSha256,HeightRuleSha256=slice.Height.RuleSha256,OriginalNumericParity=false,OriginalTerrainParity=false,
 MotionChecks=motionChecks,
 QueryGeometry=querySet.Children.Select(child=>new{child.ChunkIndex,child.PatchIndex,child.Geometry.PatternIndex,
  Positions=child.Geometry.CopyPositions().Select(v=>new[]{BitConverter.SingleToUInt32Bits(v.X),BitConverter.SingleToUInt32Bits(v.Y),BitConverter.SingleToUInt32Bits(v.Z)}),
  Normals=child.Geometry.CopyNormals().Select(v=>new[]{BitConverter.SingleToUInt32Bits(v.X),BitConverter.SingleToUInt32Bits(v.Y),BitConverter.SingleToUInt32Bits(v.Z)}),Indices=child.Geometry.CopyIndices()}),
 SurfaceQueries=locations.Select((point,i)=>new{point.X,point.Z,Hit=hits[i] is {} hit?new{hit.ChunkIndex,hit.PatchIndex,Surface=hit.Hit}:null}),
 QuerySet=new{querySet.ChunkCount,querySet.PatchCount,querySet.TotalTriangles,querySet.ChildBuilds,querySet.NormalBuilds,querySet.GeometryBuilds,querySet.SetAdmissions,querySet.AggregateCopyBytes,querySet.AggregateArrayBytes,querySet.AggregateWork,querySet.ChildLogicalOwnedBytes,ActualSource=ReferenceEquals(querySet.Source,patchSet),OriginalCollisionParity=false},
 Shader=new{shaderFamilies.EncodedBytes,shaderFamilies.SourceSha256,Families=shaderFamilies.Families.Count,shaderFamilies.TotalEntries,shaderFamilies.TotalStringBytes,shaderFamilies.HasShaderSemantics,ActualSource=ReferenceEquals(shaderFamilies.Generator,generator)&&ReferenceEquals(shaderFamilies.Group,generator.Groups[0])},
 PatchSet=new{patchSet.Count,patchSet.TotalCells,patchSet.TotalPatches,patchSet.SelectedSamples,patchSet.OwnCopyBytes,patchSet.OwnArrayBytes,patchSet.OwnWork,patchSet.BorrowedCopyReservation,patchSet.BorrowedArrayReservation,patchSet.BorrowedWorkReservation,patchSet.SharedEdgePairs,patchSet.SharedCornerPairs,patchSet.SharedSampleComparisons,patchSet.NormalBuilds,patchSet.GeometryBuilds,
 ActualReferences=patchSet.Patches.Select((p,i)=>ReferenceEquals(p,assemblies[i])).All(v=>v),EdgePolicy=patchSet.EdgePolicy.ToString(),NormalPolicy=patchSet.NormalPolicy.ToString()},Chunks=chunks})+"\n",new UTF8Encoding(false));
Console.WriteLine("PASS: original-layer modern first-curve consumer");

