using Stardust.Core.Assets;
namespace Stardust.Core.Geometry;
public sealed record TerrainHeightLayerSparseFirstCurveEvaluationLimits(int MaximumAxisCount=32,int MaximumCells=1024,int MaximumRequests=1024,
    long MaximumAggregateCopyBytes=4194304,long MaximumOwnedArrayBytes=1048576,long MaximumAggregateRouteBytes=16777216,
    long MaximumAggregateWork=67108864,uint MaximumAbsoluteHeightBits=0x49742400);
public sealed record TerrainHeightLayerSparseFirstCurveEvaluationReservation(long OwnCopyBytes,long OwnEncodedCopyBytes,long OwnRequestBytes,
    long OwnPriorBytes,long OwnArrayBytes,long OwnWork,long BatchCopyBytes,long BatchRouteBytes,long BatchWork,int BatchQueryBytes,int BatchQueryWork,
    long ConsumerCopyBytes,long ConsumerArrayBytes,long ConsumerMappingBytes,long ConsumerWork)
{
    public long AggregateCopyBytes=>OwnCopyBytes+BatchCopyBytes+ConsumerCopyBytes;
    // Whole child route conservatively bounds child arrays because the frozen API exposes no array-only charge.
    public long OwnedArrayBytes=>OwnArrayBytes+BatchRouteBytes+ConsumerArrayBytes;
    public long AggregateRouteBytes=>OwnArrayBytes+BatchRouteBytes+ConsumerArrayBytes+ConsumerMappingBytes;
    public long AggregateWork=>OwnWork+BatchWork+ConsumerWork;
}
/// <summary>One owned row-major sparse modern evaluation over actual filtered inputs. Numerical children remain frozen.</summary>
public sealed class TerrainHeightLayerSparseFirstCurveEvaluation
{
    private readonly uint[] priors;private readonly byte[] encoded;
    private TerrainHeightLayerSparseFirstCurveEvaluation(TerrainHeightLayerFilteredInfluence filtered,TerrainFirstCurveFractalSparseBatch batch,
        TerrainHeightLayerFirstCurveSparseConsumer consumer,TerrainConditionalFractalSampler.Request[] requests,uint[] priors,byte[] encoded,
        TerrainHeightLayerSparseFirstCurveEvaluationReservation reservation)
    {Filtered=filtered;Batch=batch;Consumer=consumer;Requests=Array.AsReadOnly(requests);this.priors=priors;this.encoded=encoded;PriorHeightBits=Array.AsReadOnly(priors);Reservation=reservation;}
    public TerrainHeightLayerFilteredInfluence Filtered {get;}
    public TerrainGeneratorHeightLayerSlice Slice=>Filtered.Slice;
    public TerrainSampleCoordinateGrid Grid=>Filtered.Grid;
    public TerrainFirstCurveFractalSparseBatch Batch {get;}
    public TerrainHeightLayerFirstCurveSparseConsumer Consumer {get;}
    public IReadOnlyList<TerrainConditionalFractalSampler.Request> Requests {get;}
    public IReadOnlyList<uint> PriorHeightBits {get;}
    public IReadOnlyList<TerrainHeightLayerFirstCurveSparseConsumerCell> Cells=>Consumer.Cells;
    public TerrainHeightLayerSparseFirstCurveEvaluationReservation Reservation {get;}
    public TerrainFirstCurveNumericPolicy Policy=>TerrainFirstCurveNumericPolicy.ModernBinary64LogPowApproximation;
    public bool OriginalNumericParity=>false;
    public long AccountedAggregateRouteBytes=>Reservation.OwnArrayBytes+Batch.AccountedRouteBytes+Consumer.ArrayBytes+Consumer.MappingBytes;
    public long AccountedAggregateWork=>Reservation.OwnWork+Batch.AccountedWork+Consumer.Work;
    public TerrainFirstCurveFractalControls CurveControls=>Batch.Controls;
    public TerrainHeightArithmeticPrecision HeightPrecision=>Consumer.Precision;
    public int OwnedSamplerFactories=>Batch.SamplerFactories;
    public int OwnedSamplerQueryBatches=>Batch.KernelBatches;
    public int OwnedSamplerDisposals=>Batch.SamplerDisposals;
    public TerrainHeightLayerFirstCurveSparseConsumerCounts ConsumerCounts=>Consumer.Counts;
    public TerrainHeightLayerFilteredInfluenceCounts BorrowedFilterCounts=>Filtered.Counts;
    public uint[] CopyPriorHeightBits()=>(uint[])priors.Clone();
    public uint[] CopyHeightBits()=>Consumer.CopyHeightBits();
    public byte[] CopyEncodedHeightFamilyBytes()=>(byte[])encoded.Clone();
    private static bool Finite(uint bits)=>(bits&0x7f800000)!=0x7f800000;
    private static bool Positive(uint bits)=>(bits&0x80000000)==0&&(bits&0x7fffffff)!=0;
    private static float Value(uint bits)=>BitConverter.Int32BitsToSingle(unchecked((int)bits));
    private static void Need(bool okay,string reason){if(!okay)throw new InvalidDataException(reason);}
    private static void Precision(int p){if(p is not (24 or 53 or 64))throw new ArgumentOutOfRangeException(nameof(p));}
    public static TerrainHeightLayerSparseFirstCurveEvaluation Evaluate(TerrainHeightLayerFilteredInfluence filtered,TerrainGeneratorHeightLayerSlice slice,
        TerrainSampleCoordinateGrid grid,ReadOnlySpan<uint> priorHeightBits,TerrainFirstCurveFractalControls curveControls,
        TerrainHeightArithmeticPrecision heightPrecision,TerrainFirstCurveNumericPolicy policy,
        TerrainHeightLayerSparseFirstCurveEvaluationLimits? limits=null,CancellationToken token=default)
    {
        ArgumentNullException.ThrowIfNull(filtered);ArgumentNullException.ThrowIfNull(slice);ArgumentNullException.ThrowIfNull(grid);ArgumentNullException.ThrowIfNull(curveControls);limits??=new();
        float maximum=Value(limits.MaximumAbsoluteHeightBits);
        if(limits.MaximumAxisCount is <0 or >32||limits.MaximumCells is <0 or >1024||limits.MaximumRequests is <0 or >1024
            ||limits.MaximumAggregateCopyBytes is <0 or >4194304||limits.MaximumOwnedArrayBytes is <0 or >8388608
            ||limits.MaximumAggregateRouteBytes is <0 or >16777216||limits.MaximumAggregateWork is <0 or >67108864
            ||!float.IsFinite(maximum)||maximum<0||maximum>1000000)throw new ArgumentOutOfRangeException(nameof(limits));
        if(policy!=TerrainFirstCurveNumericPolicy.ModernBinary64LogPowApproximation)throw new ArgumentOutOfRangeException(nameof(policy));
        Precision(curveControls.TablePrecision);Precision(curveControls.ConstructionPrecision);Precision(curveControls.QueryPrecision);
        TerrainLayerInfluence.ValidatePrecision(heightPrecision);token.ThrowIfCancellationRequested();
        Need(ReferenceEquals(filtered.Slice,slice)&&ReferenceEquals(filtered.Grid,grid)&&slice.Policy==TerrainGeneratorHeightLayerSlicePolicy.TypedEvidence&&slice.IsLocallyEnabled,
            "Same actual enabled evidence slice/grid/filter required.");
        int axis=grid.Layer.Count;long nlong=(long)axis*axis;
        Need(axis is >=2 and <=32&&axis<=limits.MaximumAxisCount&&nlong<=limits.MaximumCells&&filtered.Cells.Count==nlong&&priorHeightBits.Length==nlong,
            "Complete bounded filtered/grid/prior shape required.");int n=(int)nlong;
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
        Need(height.Family.Raw.Version=="0001"&&raw.ModeBits<=5&&raw.SignedOctaveCount is >=1 and <=16&&raw.SignedSeed!=int.MaxValue
            &&Finite(raw.FrequencyMultiplierBits)&&Finite(raw.AmplitudeMultiplierBits)&&Finite(raw.FirstCoordinateScaleBits)
            &&Finite(raw.SecondCoordinateScaleBits)&&Finite(raw.FirstOffsetBits)&&Finite(raw.SecondOffsetBits),"Known frozen sparse raw eligibility required before copies.");
        int m=0;
        for(int i=0;i<n;i++){
            token.ThrowIfCancellationRequested();var pair=grid.Layer.GetPair(i%axis,i/axis);var f=filtered.Cells[i];
            Need(f.Index==i&&f.Boundary.Coordinates==pair&&ReferenceEquals(f.Boundary,filtered.Boundary.Cells[i])
                &&f.Influence.ProductBits==f.ProductBits&&Finite(f.ProductBits)&&Finite(pair.FirstBits)&&Finite(pair.SecondBits),"Actual finite grid/product identity required.");
            Need(Finite(priorHeightBits[i])&&Math.Abs((double)Value(priorHeightBits[i]))<=maximum,"All finite bounded priors required before copies/sampler.");
            if(Positive(f.ProductBits))m++;
        }
        Need(m<=limits.MaximumRequests,"Positive request count cap required.");
        // Known primitive child impossibilities; normalization/query arithmetic otherwise stays inside the frozen child stage.
        if(m>0)Need(raw.SignedSeed!=0&&!(raw.AmplitudeMultiplierBits==0xbf800000&&(raw.SignedOctaveCount&1)==0),
            "Known zero seed or exactly cancelling even-octave normalization has no nonempty child.");
        long ownRequests=16L*m,ownPrior=4L*n,ownCopies=84+ownRequests+ownPrior,ownArrays=340+ownRequests+ownPrior,ownWork=84L+16L*n+16L*m;
        long batchCopies=696L+176L*m,batchRoute=m==0?440:10088L+32L*n+1600L*m,batchWork=m==0?168:20424L+n+8288L*m;
        int queryBytes=m==0?0:checked(16*n+1440*m),queryWork=m==0?0:checked(n+8256*m);
        long consumerCopies=4L*n,consumerArrays=256L+192L*n,consumerMap=4L*n,consumerWork=84L+32L*n+16L*m;
        Need(ownCopies+batchCopies+consumerCopies<=limits.MaximumAggregateCopyBytes&&ownArrays+batchRoute+consumerArrays<=limits.MaximumOwnedArrayBytes
            &&ownArrays+batchRoute+consumerArrays+consumerMap<=limits.MaximumAggregateRouteBytes&&ownWork+batchWork+consumerWork<=limits.MaximumAggregateWork,
            "Aggregate own/child reservation before requests/priors/raw exports and sampler.");
        Need(queryBytes<=2097152&&queryWork<=16777216&&batchCopies<=4194304&&batchRoute<=16777216&&batchWork<=67108864
            &&consumerCopies<=1048576&&consumerArrays<=8388608&&consumerMap<=4096&&consumerWork<=8388608,
            "Known exact child option viability required.");
        token.ThrowIfCancellationRequested();
        var priors=priorHeightBits.ToArray();
        for(int i=0;i<n;i++){token.ThrowIfCancellationRequested();Need(Finite(priors[i])&&Math.Abs((double)Value(priors[i]))<=maximum,"Owned prior snapshot must remain finite/bounded.");}
        var requests=new TerrainConditionalFractalSampler.Request[m];int j=0;
        for(int i=0;i<n;i++){
            token.ThrowIfCancellationRequested();if(!Positive(filtered.Cells[i].ProductBits))continue;var p=grid.Layer.GetPair(i%axis,i/axis);
            requests[j++]=new(p.Column,p.Row,p.FirstBits,p.SecondBits);
        }
        Need(j==m,"Immutable positive set count remained exact.");token.ThrowIfCancellationRequested();
        byte[] encoded=height.Family.Raw.CopyEncodedBytes();Need(encoded.Length==84,"Actual selected raw export extent.");
        var reservation=new TerrainHeightLayerSparseFirstCurveEvaluationReservation(ownCopies,84,ownRequests,ownPrior,ownArrays,ownWork,batchCopies,batchRoute,batchWork,queryBytes,queryWork,consumerCopies,consumerArrays,consumerMap,consumerWork);
        var batch=TerrainFirstCurveFractalSparseBatch.Build(encoded,axis,axis,requests,curveControls,policy,
            new(n,m,84,batchCopies,batchRoute,batchWork,queryBytes,queryWork),token);
        var consumer=TerrainHeightLayerFirstCurveSparseConsumer.Evaluate(filtered,slice,grid,batch,priors,heightPrecision,policy,
            new(axis,n,consumerCopies,consumerArrays,consumerWork,limits.MaximumAbsoluteHeightBits,consumerMap),token);
        Need(batch.LogicalCopyBytes==batchCopies&&batch.ReservedRouteBytes==batchRoute&&batch.ReservedWork==batchWork
            &&batch.ReservedQueryBytes==queryBytes&&batch.ReservedQueryWork==queryWork&&consumer.CopiedPriorBytes==consumerCopies
            &&consumer.CopiedEncodedInputBytes==0&&consumer.ArrayBytes==consumerArrays&&consumer.MappingBytes==consumerMap&&consumer.Work==consumerWork,"Actual child reservation identity.");
        Need(ReferenceEquals(consumer.Batch,batch)&&ReferenceEquals(consumer.Filtered,filtered)&&consumer.Counts.BorrowedCurveQueries==m&&consumer.Counts.ConsumerCalls==n,
            "Actual child reference/count identity.");
        token.ThrowIfCancellationRequested();return new(filtered,batch,consumer,requests,priors,encoded,reservation);
    }
}
