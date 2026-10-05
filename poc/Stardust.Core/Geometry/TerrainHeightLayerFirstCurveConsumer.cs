using System.Buffers.Binary;
using System.Security.Cryptography;
using Stardust.Core.Assets;
namespace Stardust.Core.Geometry;
public sealed record TerrainHeightLayerFirstCurveConsumerLimits(int MaximumAxisCount=32,int MaximumCells=1024,
    long MaximumCopyBytes=131072,long MaximumArrayBytes=1048576,long MaximumWork=1048576,uint MaximumAbsoluteHeightBits=0x49742400);
public sealed record TerrainHeightLayerFirstCurveConsumerCell(int Index,TerrainHeightLayerFilteredInfluenceCell Filtered,
    TerrainFirstCurveFractalBatch.Cell Curve,uint PriorHeightBits,TerrainSuppliedFractalSample? Supplied,TerrainRetainedHeightFractalResult Consumer)
{
    public uint HeightBits=>Consumer.Consumer.ResultHeightBits;
    public bool Applied=>Consumer.Consumer.Outcome==TerrainHeightFractalOutcome.Applied;
    public TerrainFirstCurveApproximationSample Approximation=>Curve.Approximation;
}
public sealed record TerrainHeightLayerFirstCurveConsumerCounts(int BorrowedCurveQueries,int ConsumerCalls,int DyadicConversions,
    int HeightApplications,int SkippedCells,int OwnedSamplerConstructions,int OwnedSamplerQueryBatches,int OwnedSamplerDisposals);
/// <summary>Consumes a supplied complete modern curve batch; no noise suppression, sampler, ancestors or original numerical parity.</summary>
public sealed class TerrainHeightLayerFirstCurveConsumer
{
    private readonly uint[] priors;
    private TerrainHeightLayerFirstCurveConsumer(TerrainHeightLayerFilteredInfluence filtered,TerrainFirstCurveFractalBatch batch,
        uint[] priors,TerrainHeightLayerFirstCurveConsumerCell[] cells,TerrainHeightArithmeticPrecision precision,string rawHash,
        long bytes,long work,uint bound,int applied)
    {
        Filtered=filtered;Batch=batch;this.priors=priors;PriorHeightBits=Array.AsReadOnly(priors);Cells=Array.AsReadOnly(cells);Precision=precision;
        FamilyRawSha256=rawHash;CopiedPriorBytes=4L*priors.Length;ArrayBytes=bytes;Work=work;MaximumAbsoluteHeightBits=bound;
        Counts=new(cells.Length,cells.Length,applied,applied,cells.Length-applied,0,0,0);
    }
    public TerrainHeightLayerFilteredInfluence Filtered {get;}
    public TerrainFirstCurveFractalBatch Batch {get;}
    public TerrainGeneratorHeightLayerSlice Slice=>Filtered.Slice;
    public TerrainSampleCoordinateGrid Grid=>Filtered.Grid;
    public TerrainGeneratorHeightFractalRule Height=>Slice.Height;
    public TerrainGeneratorHeightLayerChild HeightSource=>Slice.Children[2];
    public TerrainFirstCurveNumericPolicy Policy=>TerrainFirstCurveNumericPolicy.ModernBinary64LogPowApproximation;
    public bool OriginalNumericParity=>false;
    public TerrainHeightArithmeticPrecision Precision {get;}
    public string FamilyRawSha256 {get;}
    public IReadOnlyList<uint> PriorHeightBits {get;}
    public IReadOnlyList<TerrainHeightLayerFirstCurveConsumerCell> Cells {get;}
    public TerrainHeightLayerFirstCurveConsumerCounts Counts {get;}
    public long CopiedPriorBytes {get;}
    public long CopiedEncodedInputBytes=>0;
    public long ArrayBytes {get;}
    public long Work {get;}
    public uint MaximumAbsoluteHeightBits {get;}
    public uint[] CopyPriorHeightBits()=>(uint[])priors.Clone();
    public uint[] CopyHeightBits()=>Cells.Select(c=>c.HeightBits).ToArray();
    private static bool Finite(uint bits)=>(bits&0x7f800000)!=0x7f800000;
    private static bool Positive(uint bits)=>(bits&0x80000000)==0&&(bits&0x7fffffff)!=0;
    private static float Value(uint bits)=>BitConverter.Int32BitsToSingle(unchecked((int)bits));
    private static void Need(bool okay,string reason){if(!okay)throw new InvalidDataException(reason);}
    // Every accepted frozen raw input has this exact 84-byte envelope; all 13 words are represented by immutable Fields.
    // Stack reconstruction hashes the actual selected owner's complete bytes without an encoded-input export/copy.
    private static string RawHash(TerrainFractalRawFields f)
    {
        Span<byte> bytes=stackalloc byte[84];"FORM"u8.CopyTo(bytes);BinaryPrimitives.WriteUInt32BigEndian(bytes[4..],76);"MFRC"u8.CopyTo(bytes[8..]);
        "FORM"u8.CopyTo(bytes[12..]);BinaryPrimitives.WriteUInt32BigEndian(bytes[16..],64);"0001"u8.CopyTo(bytes[20..]);
        "DATA"u8.CopyTo(bytes[24..]);BinaryPrimitives.WriteUInt32BigEndian(bytes[28..],52);
        ReadOnlySpan<uint> words=[f.SeedBits,f.FirstCurveFlagBits,f.FirstCurveParameterBits,f.SecondCurveFlagBits,f.SecondCurveParameterBits,
            f.OctaveCountBits,f.FrequencyMultiplierBits,f.AmplitudeMultiplierBits,f.FirstCoordinateScaleBits,f.SecondCoordinateScaleBits,f.FirstOffsetBits,f.SecondOffsetBits,f.ModeBits];
        for(int i=0;i<13;i++)BinaryPrimitives.WriteUInt32LittleEndian(bytes[(32+4*i)..],words[i]);
        Span<byte> digest=stackalloc byte[32];SHA256.HashData(bytes,digest);return Convert.ToHexString(digest);
    }
    public static TerrainHeightLayerFirstCurveConsumer Evaluate(TerrainHeightLayerFilteredInfluence filtered,TerrainGeneratorHeightLayerSlice slice,
        TerrainSampleCoordinateGrid grid,TerrainFirstCurveFractalBatch batch,ReadOnlySpan<uint> priorHeightBits,TerrainHeightArithmeticPrecision precision,
        TerrainFirstCurveNumericPolicy policy,TerrainHeightLayerFirstCurveConsumerLimits? limits=null,CancellationToken token=default)
    {
        ArgumentNullException.ThrowIfNull(filtered);ArgumentNullException.ThrowIfNull(slice);ArgumentNullException.ThrowIfNull(grid);ArgumentNullException.ThrowIfNull(batch);limits??=new();
        float maximum=Value(limits.MaximumAbsoluteHeightBits);
        if(limits.MaximumAxisCount is <0 or >32||limits.MaximumCells is <0 or >1024||limits.MaximumCopyBytes is <0 or >1048576
            ||limits.MaximumArrayBytes is <0 or >8388608||limits.MaximumWork is <0 or >8388608||!float.IsFinite(maximum)||maximum<0||maximum>1000000)throw new ArgumentOutOfRangeException(nameof(limits));
        if(policy!=TerrainFirstCurveNumericPolicy.ModernBinary64LogPowApproximation)throw new ArgumentOutOfRangeException(nameof(policy));
        TerrainLayerInfluence.ValidatePrecision(precision);token.ThrowIfCancellationRequested();
        Need(ReferenceEquals(filtered.Slice,slice)&&ReferenceEquals(filtered.Grid,grid)&&slice.Policy==TerrainGeneratorHeightLayerSlicePolicy.TypedEvidence&&slice.IsLocallyEnabled,
            "Same actual enabled evidence slice/grid/filter receipt required.");
        int axis=grid.Layer.Count;long nlong=(long)axis*axis;
        Need(axis is >=2 and <=32&&axis<=limits.MaximumAxisCount&&nlong<=limits.MaximumCells&&filtered.Cells.Count==nlong&&priorHeightBits.Length==nlong
            &&batch.Width==axis&&batch.Rows==axis&&batch.Cells.Count==nlong,"Complete matching supplied curve/filter/grid/prior shape required.");int n=(int)nlong;
        long copies=4L*n,arrays=256L+192L*n,work=84L+32L*n;
        Need(copies<=limits.MaximumCopyBytes&&arrays<=limits.MaximumArrayBytes&&work<=limits.MaximumWork,"Full copy/array/work preflight before conversion or allocation.");
        var height=slice.Height;var source=slice.Children[2];var fields=height.Fields;var raw=height.Family.Raw.Fields;
        Need(height.Policy==TerrainGeneratorHeightFractalRulePolicy.TypedEvidence&&height.IsEnabled&&height.Input.Version=="0003"&&height.Input.HasDecodedFields
            &&fields.OperationBits<=3&&Finite(fields.ScaleBits),"Enabled typed AHFR/0003 op0..3 and finite scale required.");
        Need(source.FormType=="AHFR"&&source.Version=="0003"&&source.ChildOrdinal==4&&source.GlobalRuleIndex==height.RuleIndex
            &&source.Offset==height.RuleOffset&&source.EncodedBytes==height.RuleBytes&&source.Sha256==height.RuleSha256
            &&height.LayerIndex==slice.LayerIndex&&height.ChildOrdinal==4&&height.GeneratorSha256==slice.GeneratorSha256&&height.GeneratorBytes==slice.GeneratorBytes
            &&height.GroupSha256==slice.GroupSha256&&height.GroupOffset==slice.GroupOffset&&height.GroupBytes==slice.GroupBytes
            &&fields.FamilyKeyBits==unchecked((uint)height.FamilyId)&&height.FamilyGeneratorOffset==slice.GroupOffset+height.Family.Offset
            &&height.Family.Offset>=24&&(long)height.Family.Offset+height.Family.EncodedBytes<=slice.GroupBytes,"Exact selected AHFR/family/group/rule identity required.");
        Need(raw.FirstCurveEnabled&&!raw.SecondCurveEnabled&&Finite(raw.FirstCurveParameterBits)&&Value(raw.FirstCurveParameterBits)>0&&Value(raw.FirstCurveParameterBits)<1,
            "Selected first-on/second-off modern parameter domain required.");
        string rawHash=RawHash(raw);
        Need(batch.Policy==policy&&!batch.OriginalNumericParity&&batch.OriginalSha256==rawHash,"Actual curve batch must match selected exact MFRC bytes/SHA and modern provenance.");
        for(int i=0;i<n;i++){
            token.ThrowIfCancellationRequested();var pair=grid.Layer.GetPair(i%axis,i/axis);var f=filtered.Cells[i];var c=batch.Cells[i];
            Need(f.Index==i&&f.Boundary.Coordinates==pair&&ReferenceEquals(f.Boundary,filtered.Boundary.Cells[i])
                &&f.Influence.ProductBits==f.ProductBits&&Finite(f.ProductBits),"Actual row-major filtered product required.");
            Need(c.Index==i&&c.Request.Column==pair.Column&&c.Request.Row==pair.Row&&c.Request.XBits==pair.FirstBits&&c.Request.ZBits==pair.SecondBits
                &&c.KernelResult.Origin==TerrainConditionalFractalOrigin.ConditionalComputedMiss&&c.Approximation.BaseF32Bits==c.KernelResult.Sample.ProjectionBits
                &&c.Approximation.Policy==policy&&!c.Approximation.OriginalNumericParity,"Exact row-major curve request/result/approximation provenance required.");
            Need(Finite(priorHeightBits[i])&&Math.Abs((double)Value(priorHeightBits[i]))<=maximum,"Complete finite priors within modern absolute height limit required.");
        }
        token.ThrowIfCancellationRequested();var priors=priorHeightBits.ToArray();
        for(int i=0;i<n;i++){token.ThrowIfCancellationRequested();Need(Finite(priors[i])&&Math.Abs((double)Value(priors[i]))<=maximum,"Owned finite bounded prior snapshot required.");}
        var cells=new TerrainHeightLayerFirstCurveConsumerCell[n];int applied=0;
        for(int i=0;i<n;i++){
            token.ThrowIfCancellationRequested();var f=filtered.Cells[i];var c=batch.Cells[i];var supplied=Positive(f.ProductBits)?c.Approximation.ToSuppliedSample():null;
            var consumer=TerrainSuppliedHeightFractal.Evaluate(height.Input,priors[i],f.ProductBits,supplied,precision);
            Need(ReferenceEquals(consumer.Sample,supplied)&&Finite(consumer.Consumer.ResultHeightBits)
                &&Math.Abs((double)Value(consumer.Consumer.ResultHeightBits))<=maximum,"Owned height consumer identity and modern output height limit required.");
            if(supplied is null)Need(consumer.Consumer.Outcome==TerrainHeightFractalOutcome.SkippedNonPositiveInfluence&&consumer.Consumer.ResultHeightBits==priors[i],"Blocked consumer preserves exact prior including signed zero.");
            else{Need(consumer.Consumer.Outcome==TerrainHeightFractalOutcome.Applied,"Strict active op0..3 application required.");applied++;}
            cells[i]=new(i,f,c,priors[i],supplied,consumer);
        }
        token.ThrowIfCancellationRequested();return new(filtered,batch,priors,cells,precision,rawHash,arrays,work,limits.MaximumAbsoluteHeightBits,applied);
    }
}
