using Stardust.Core.Assets;

namespace Stardust.Core.Geometry;

/// <summary>Bounded Tatooine base-height slice. Later geographic layers are deliberately not evaluated.</summary>
public sealed record OriginalTerrainRegion(TerrainGeneratorInput Generator, TerrainGeneratorHeightFractalRule Height,
    TerrainModernHeightPatchSet Patches, TerrainModernPatchSetSurfaceQuery Surface, string TerrainSha256)
{
    public const string TerrainPath = "terrain/tatooine.trn";
    public System.Numerics.Vector2 Start {get;init;}
    public IReadOnlyList<int> AppliedHeightLayers {get;init;}=Array.Empty<int>();
    public static OriginalTerrainRegion BuildMosTaike(ReadOnlySpan<byte> bytes,CancellationToken token=default)
    {
        var terrain=ProceduralTerrainInput.Decode(bytes);var generator=TerrainGeneratorInput.Decode(terrain.CopyGeneratorForm());
        if(generator.SourceSha256!="B623B789E5F8486414C1D68CD341005E7E25FD97AF30567E64E7AC848DEB6C32")throw new InvalidDataException("Unverified Mos Taike generator.");
        var binding=TerrainGeneratorFractalFamilies.Bind(generator);
        var height=TerrainGeneratorHeightFractalRule.Select(generator,binding,0,TerrainGeneratorHeightFractalRulePolicy.EnabledOperationalSubset);
        var evaluator=new OriginalTerrainHeightTraversal(generator);var chunks=new List<TerrainModernHeightPatch>();
        for(int z=57;z<61;z++)for(int x=93;x<97;x++)
        {
            token.ThrowIfCancellationRequested();var grid=TerrainSampleCoordinateGrid.Build(new(BitConverter.SingleToUInt32Bits(40),4,3,x,z,1),
                TerrainHeightArithmeticPrecision.Binary24,TerrainHeightArithmeticPrecision.Binary24,TerrainHeightArithmeticPrecision.Binary24,TerrainHeightArithmeticPrecision.Binary24);
            var heights=evaluator.Evaluate(grid,token);int n=grid.Layer.Count;
            var source=TerrainCoordinateHeightPatch.Build(grid,heights,Enumerable.Repeat(System.Numerics.Vector3.UnitY,n*n).ToArray(),Enumerable.Repeat(0xFFFFFFFFu,n*n).ToArray(),0,1);
            chunks.Add(TerrainModernHeightPatch.Build(source,TerrainModernNormalPolicy.ModernBinary64CentralDifference,cancellation:token));
        }
        var set=TerrainModernHeightPatchSet.Build(chunks.ToArray(),TerrainModernPatchEdgePolicy.RequireBitIdenticalActiveSharedEdges,TerrainModernPatchNormalPolicy.RequireBitIdenticalSharedNormals,cancellation:token);
        var surface=TerrainModernPatchSetSurfaceQuery.Build(set,cancellation:token);
        return new(generator,height,set,surface,Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)))
            {Start=new(3833,2357),AppliedHeightLayers=evaluator.AppliedHeightLayers.Order().ToArray()};
    }
    public static OriginalTerrainRegion Build(ReadOnlySpan<byte> bytes, CancellationToken token = default)
    {
        string sha = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes));
        var terrain = ProceduralTerrainInput.Decode(bytes);
        var generator = TerrainGeneratorInput.Decode(terrain.CopyGeneratorForm());
        if (generator.SourceSha256 != "B623B789E5F8486414C1D68CD341005E7E25FD97AF30567E64E7AC848DEB6C32")
            throw new InvalidDataException("Unsupported terrain generator identity; no synthetic fallback.");
        var binding = TerrainGeneratorFractalFamilies.Bind(generator);
        var height = TerrainGeneratorHeightFractalRule.Select(generator, binding, 0,
            TerrainGeneratorHeightFractalRulePolicy.EnabledOperationalSubset);
        var layer = generator.Layers[0];
        if (layer.ParentIndex is not null || layer.Header?.IsEnabled != true ||
            layer.Children.Any(c => c.FormType?.StartsWith('B') == true || c.FormType?.StartsWith('F') == true) ||
            height.LayerIndex != 0 || height.ChildOrdinal != 2)
            throw new InvalidDataException("Enabled unbounded root height layer required.");
        var detail=TerrainGeneratorHeightLayerSlice.Select(generator,binding,3,TerrainGeneratorHeightLayerSlicePolicy.TypedEvidence);
        if(detail.ParentIndex!=0)throw new InvalidDataException("Detail layer must inherit the admitted base layer.");
        var chunks = new List<TerrainModernHeightPatch>();
        for (int z = -2; z < 2; z++) for (int x = -2; x < 2; x++)
        {
            token.ThrowIfCancellationRequested();
            // 16 chunks, each64m and4 cells: 8m height spacing; margin supplies shared central differences.
            var grid = TerrainSampleCoordinateGrid.Build(new(BitConverter.SingleToUInt32Bits(64),4,3,x,z,1),
                TerrainHeightArithmeticPrecision.Binary24,TerrainHeightArithmeticPrecision.Binary24,
                TerrainHeightArithmeticPrecision.Binary24,TerrainHeightArithmeticPrecision.Binary24);
            int n = grid.Layer.Count;
            using var sampler = TerrainConditionalFractalSampler.CreateForSupportedModes(height.Family.Raw,n,n,53,64,1048576,1048576,cancellation:token);
            var requests = grid.Layer.CopyPairs().Select(p => new TerrainConditionalFractalSampler.Request(p.Column,p.Row,p.FirstBits,p.SecondBits)).ToArray();
            var samples = sampler.Query(requests,64,2097152,16777216,token);
            var heights = samples.Select(s => TerrainSuppliedHeightFractal.Evaluate(height.Input,0,0x3F800000,s.Sample,
                TerrainHeightArithmeticPrecision.Binary64).Consumer.ResultHeightBits).ToArray();
            // The existing verified circle/filter/first-curve slice consumes the actual base heights as its priors.
            var boundary=TerrainHeightLayerBoundaryAdmission.Evaluate(detail,grid,new(TerrainHeightArithmeticPrecision.Binary64,
                TerrainHeightArithmeticPrecision.Binary64,TerrainHeightArithmeticPrecision.Binary64),token:token);
            var filtered=TerrainHeightLayerFilteredInfluence.Evaluate(boundary,detail,grid,Enumerable.Repeat(0x3F800000u,n*n).ToArray(),
                new(53,64,64,TerrainHeightArithmeticPrecision.Binary64,TerrainHeightArithmeticPrecision.Binary64,TerrainHeightArithmeticPrecision.Binary64),token:token);
            heights=TerrainHeightLayerSparseFirstCurveEvaluation.Evaluate(filtered,detail,grid,heights,new(53,64,64),
                TerrainHeightArithmeticPrecision.Binary64,TerrainFirstCurveNumericPolicy.ModernBinary64LogPowApproximation,token:token).CopyHeightBits();
            var source = TerrainCoordinateHeightPatch.Build(grid,heights,Enumerable.Repeat(System.Numerics.Vector3.UnitY,n*n).ToArray(),
                Enumerable.Repeat(0xFFFFFFFFu,n*n).ToArray(),0,1);
            chunks.Add(TerrainModernHeightPatch.Build(source,TerrainModernNormalPolicy.ModernBinary64CentralDifference,cancellation:token));
        }
        var set = TerrainModernHeightPatchSet.Build(chunks.ToArray(),TerrainModernPatchEdgePolicy.RequireBitIdenticalActiveSharedEdges,
            TerrainModernPatchNormalPolicy.RequireBitIdenticalSharedNormals,cancellation:token);
        var query = TerrainModernPatchSetSurfaceQuery.Build(set,cancellation:token);
        return new(generator,height,set,query,sha);
    }
}
