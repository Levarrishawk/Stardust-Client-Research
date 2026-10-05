using Stardust.Core.Assets;
namespace Stardust.Core.Geometry;
public readonly record struct TerrainHeightLayerFilteredInfluenceControls(int TablePrecision,int ConstructionPrecision,int SamplerQueryPrecision,
    TerrainHeightArithmeticPrecision FilterQueryPrecision,TerrainHeightArithmeticPrecision CombinePrecision,TerrainHeightArithmeticPrecision ProductPrecision);
public sealed record TerrainHeightLayerFilteredInfluenceLimits(int MaximumAxisCount=32,int MaximumCells=1024,int MaximumFilterQueries=1024,
    long MaximumCopyBytes=131072,long MaximumArrayBytes=1048576,long MaximumRouteBytes=8388608,long MaximumWork=16777216,
    int MaximumSamplerBytes=1048576,int MaximumSamplerWork=1048576,int MaximumQueryBytes=2097152,int MaximumQueryWork=16777216);
public sealed record TerrainHeightLayerFilteredInfluenceCell(int Index,TerrainHeightLayerBoundaryAdmissionCell Boundary,uint IncomingWeightBits,
    TerrainConditionalFractalSampler.Result? Computed,TerrainFractalFilterQuery? FilterQuery,TerrainLayerInfluenceQuery? FilterCarrier,TerrainLayerInfluenceReceipt Influence)
{
    public uint LocalInfluenceBits=>Influence.LocalInfluenceBits;
    public uint ProductBits=>Influence.ProductBits;
}
public sealed record TerrainHeightLayerFilteredInfluenceCounts(int BoundaryProbesRetained,int Recombinations,int SamplerConstructions,
    int QueryBatches,int ComputedCells,int FilterQueries,int FilterCarriers,int SamplerDisposals,int KernelOctaveQueries);
/// <summary>One complete supplied grid's BCIR/FFRA influence. No AHFR, ancestors or terrain traversal.</summary>
public sealed class TerrainHeightLayerFilteredInfluence
{
    private readonly uint[] incoming;
    private TerrainHeightLayerFilteredInfluence(TerrainHeightLayerBoundaryAdmission boundary,TerrainHeightLayerFilteredInfluenceControls controls,
        uint[] incoming,TerrainHeightLayerFilteredInfluenceCell[] cells,TerrainHeightLayerFilteredInfluenceCounts counts,long arrays,long copies,
        long reservedCopies,long reservedBytes,long bytes,long reservedWork,long work,int samplerBytes,int queryBytes,int queryWork)
    {
        Boundary=boundary;Controls=controls;this.incoming=incoming;IncomingWeightBits=Array.AsReadOnly(incoming);Cells=Array.AsReadOnly(cells);Counts=counts;
        ArrayBytes=arrays;CopiedArrayBytes=copies;ReservedCopyBytes=reservedCopies;ReservedRouteBytes=reservedBytes;AccountedRouteBytes=bytes;
        ReservedWork=reservedWork;AccountedWork=work;ReservedSamplerBytes=samplerBytes;ReservedSamplerWork=20256;
        ReservedQueryBytes=queryBytes;ReservedQueryWork=queryWork;
    }
    public TerrainHeightLayerBoundaryAdmission Boundary {get;}
    public TerrainGeneratorHeightLayerSlice Slice=>Boundary.Slice;
    public TerrainSampleCoordinateGrid Grid=>Boundary.Grid;
    public TerrainGeneratorFractalFilterRule Filter=>Slice.Filter;
    public TerrainGeneratorHeightLayerChild FilterSource=>Slice.Children[1];
    public TerrainHeightLayerFilteredInfluenceControls Controls {get;}
    public IReadOnlyList<uint> IncomingWeightBits {get;}
    public IReadOnlyList<TerrainHeightLayerFilteredInfluenceCell> Cells {get;}
    public TerrainHeightLayerFilteredInfluenceCounts Counts {get;}
    public long ArrayBytes {get;}
    public long CopiedArrayBytes {get;}
    public long CopiedInputBytes=>4L*incoming.Length;
    public long CopiedEncodedInputBytes=>0;
    public long ReservedCopyBytes {get;}
    public long ReservedRouteBytes {get;}
    public long AccountedRouteBytes {get;}
    public long ReservedWork {get;}
    public long AccountedWork {get;}
    public int ReservedSamplerBytes {get;}
    public int ReservedSamplerWork {get;}
    public int ReservedQueryBytes {get;}
    public int ReservedQueryWork {get;}
    public uint[] CopyIncomingWeightBits()=>(uint[])incoming.Clone();
    public uint[] CopyLocalInfluenceBits()=>Cells.Select(c=>c.LocalInfluenceBits).ToArray();
    public uint[] CopyProductBits()=>Cells.Select(c=>c.ProductBits).ToArray();
    private static bool Finite(uint b)=>(b&0x7f800000)!=0x7f800000;
    private static void Need(bool v,string reason){if(!v)throw new InvalidDataException(reason);}
    public static TerrainHeightLayerFilteredInfluence Evaluate(TerrainHeightLayerBoundaryAdmission boundary,TerrainGeneratorHeightLayerSlice slice,
        TerrainSampleCoordinateGrid grid,ReadOnlySpan<uint> incomingWeightBits,TerrainHeightLayerFilteredInfluenceControls controls,
        TerrainHeightLayerFilteredInfluenceLimits? limits=null,CancellationToken token=default)
    {
        ArgumentNullException.ThrowIfNull(boundary);ArgumentNullException.ThrowIfNull(slice);ArgumentNullException.ThrowIfNull(grid);limits??=new();
        if(limits.MaximumAxisCount is <0 or >32||limits.MaximumCells is <0 or >1024||limits.MaximumFilterQueries is <0 or >1024
            ||limits.MaximumCopyBytes is <0 or >1048576||limits.MaximumArrayBytes is <0 or >8388608||limits.MaximumRouteBytes is <0 or >33554432
            ||limits.MaximumWork is <0 or >33554432||limits.MaximumSamplerBytes is <0 or >1048576||limits.MaximumSamplerWork is <0 or >1048576
            ||limits.MaximumQueryBytes is <0 or >2097152||limits.MaximumQueryWork is <0 or >16777216)throw new ArgumentOutOfRangeException(nameof(limits));
        foreach(int p in new[]{controls.TablePrecision,controls.ConstructionPrecision,controls.SamplerQueryPrecision,(int)controls.FilterQueryPrecision,
            (int)controls.CombinePrecision,(int)controls.ProductPrecision})if(p is not (24 or 53 or 64))throw new ArgumentOutOfRangeException(nameof(controls));
        token.ThrowIfCancellationRequested();Need(ReferenceEquals(boundary.Slice,slice)&&ReferenceEquals(boundary.Grid,grid),"Same actual slice and grid required.");
        Need(slice.Policy==TerrainGeneratorHeightLayerSlicePolicy.TypedEvidence&&slice.IsLocallyEnabled&&controls.CombinePrecision==boundary.Controls.CombinePrecision,
            "Locally enabled evidence slice and unchanged first-gate combine precision required.");
        var filter=slice.Filter;var source=slice.Children[1];var raw=filter.Family.Raw.Fields;
        Need(filter.Policy==TerrainGeneratorFractalFilterRulePolicy.TypedEvidence&&filter.IsEnabled&&filter.HasOperationalInputs&&filter.Input.Version=="0005"
            &&filter.Fields.HasFiniteRange&&filter.Fields.HasFiniteSerializedFeatheringAmount&&Finite(filter.Fields.SampleScaleBits),"Enabled finite operational FFRA inputs required.");
        Need(source.FormType=="FFRA"&&source.Version=="0005"&&source.ChildOrdinal==3&&source.GlobalRuleIndex==filter.RuleIndex
            &&source.Offset==filter.RuleOffset&&source.EncodedBytes==filter.RuleBytes&&source.Sha256==filter.RuleSha256
            &&filter.LayerIndex==slice.LayerIndex&&filter.ChildOrdinal==3&&filter.GeneratorSha256==slice.GeneratorSha256&&filter.GeneratorBytes==slice.GeneratorBytes
            &&filter.GroupSha256==slice.GroupSha256&&filter.GroupOffset==slice.GroupOffset&&filter.GroupBytes==slice.GroupBytes
            &&filter.Fields.FamilyIdBits==unchecked((uint)filter.FamilyId)&&filter.FamilyGeneratorOffset==slice.GroupOffset+filter.Family.Offset
            &&filter.Family.Offset>=24&&(long)filter.Family.Offset+filter.Family.EncodedBytes<=slice.GroupBytes,"Exact owned FFRA rule/family/group/slice provenance required.");
        Need(raw.ModeBits<=5&&!raw.FirstCurveEnabled&&!raw.SecondCurveEnabled&&raw.SignedOctaveCount is >=1 and <=16&&raw.SignedSeed!=int.MaxValue
            &&Finite(raw.FrequencyMultiplierBits)&&Finite(raw.AmplitudeMultiplierBits)&&Finite(raw.FirstCoordinateScaleBits)&&Finite(raw.SecondCoordinateScaleBits)
            &&Finite(raw.FirstOffsetBits)&&Finite(raw.SecondOffsetBits),"Frozen no-curve supported family required.");
        int axis=grid.Layer.Count;long nlong=(long)axis*axis;
        Need(axis is >=2 and <=32&&axis<=limits.MaximumAxisCount&&nlong<=limits.MaximumCells&&nlong<=limits.MaximumFilterQueries
            &&boundary.Cells.Count==nlong&&incomingWeightBits.Length==nlong,"Complete full shape and incoming weights required.");int n=(int)nlong;
        long arrays=512L*n,reservedCopies=68L*n;int sb=9648+16*n,qb=1456*n,qw=8257*n;
        long reservedBytes=arrays+sb+qb,reservedWork=20256L+qw+96L*n;
        Need(arrays<=limits.MaximumArrayBytes&&reservedCopies<=limits.MaximumCopyBytes&&reservedBytes<=limits.MaximumRouteBytes&&reservedWork<=limits.MaximumWork
            &&sb<=limits.MaximumSamplerBytes&&20256<=limits.MaximumSamplerWork&&qb<=limits.MaximumQueryBytes&&qw<=limits.MaximumQueryWork,
            "Worst-case full-grid copy/array/sampler/query/work preflight required.");
        int admitted=0;
        for(int i=0;i<n;i++){
            token.ThrowIfCancellationRequested();Need(Finite(incomingWeightBits[i]),"Complete finite incoming weights required.");var b=boundary.Cells[i];
            Need(b.Coordinates==grid.Layer.GetPair(i%axis,i/axis)&&ReferenceEquals(b.Carrier.Circle,b.CircleQuery)
                &&ReferenceEquals(b.CircleQuery.Fields,slice.Circle.Fields)&&ReferenceEquals(b.BoundaryProbe.Sample.Boundaries[0].Query,b.Carrier),"Actual row-major boundary identities required.");
            if(b.Admitted)admitted++;
        }
        token.ThrowIfCancellationRequested();var incoming=incomingWeightBits.ToArray();var cells=new TerrainHeightLayerFilteredInfluenceCell[n];
        var computed=new TerrainConditionalFractalSampler.Result?[n];var queries=new TerrainFractalFilterQuery?[n];var carriers=new TerrainLayerInfluenceQuery?[n];
        int constructions=0,batches=0,disposals=0;
        if(admitted>0){
            var requests=new TerrainConditionalFractalSampler.Request[admitted];int j=0;
            for(int i=0;i<n;i++)if(boundary.Cells[i].Admitted){token.ThrowIfCancellationRequested();var c=boundary.Cells[i].Coordinates;requests[j++]=new(c.Column,c.Row,c.FirstBits,c.SecondBits);}
            using(var sampler=TerrainConditionalFractalSampler.CreateForSupportedModes(filter.Family.Raw,axis,axis,controls.TablePrecision,controls.ConstructionPrecision,sb,20256,false,token)){
                constructions++;var values=sampler.Query(requests,controls.SamplerQueryPrecision,qb,qw,token);batches++;j=0;
                for(int i=0;i<n;i++)if(boundary.Cells[i].Admitted){
                    token.ThrowIfCancellationRequested();var value=values[j++];Need(value.Origin==TerrainConditionalFractalOrigin.ConditionalComputedMiss,"Fresh unique admitted cell miss required.");
                    var query=TerrainFilterFractalInfluence.Query(filter.Input,value.Sample,controls.FilterQueryPrecision);
                    var carrier=TerrainLayerInfluenceQuery.FromRetainedFractalFilter(query);
                    Need(ReferenceEquals(query.Input,filter.Input)&&ReferenceEquals(query.RetainedSample,value.Sample)&&ReferenceEquals(carrier.FractalFilter,query)
                        &&ReferenceEquals(carrier.RetainedFractalSample,value.Sample),"Actual retained FFRA result/query/carrier identity required.");
                    computed[i]=value;queries[i]=query;carriers[i]=carrier;
                }
            }
            disposals++;
        }
        for(int i=0;i<n;i++){
            token.ThrowIfCancellationRequested();var b=boundary.Cells[i];TerrainLayerInfluenceEntry[] filters=b.Admitted?[new(true,filter.Fields.FeatheringTypeBits,carriers[i])]:[];
            var sample=TerrainLayerInfluenceSample.Create(incoming[i],[new(true,slice.Circle.Fields!.FeatheringTypeBits!.Value,b.Carrier)],filters,
                1,1,slice.BoundaryInverted,slice.FilterInverted,controls.CombinePrecision,controls.ProductPrecision,2);
            var receipt=TerrainLayerInfluence.Evaluate(sample,new(1,2,2));Need(receipt.BoundaryPositive==b.Admitted,"Recombined first gate must equal actual prior admission.");
            cells[i]=new(i,b,incoming[i],computed[i],queries[i],carriers[i],receipt);
        }
        int o=raw.SignedOctaveCount;long copies=28L*n+40L*admitted;
        long bytes=arrays+(admitted>0?sb+16L*n+admitted*(160L+80L*o):0);
        long work=96L*n+(admitted>0?20000L+16L*o+n+admitted*(64L+512L*o):0);
        token.ThrowIfCancellationRequested();return new(boundary,controls,incoming,cells,new(n,n,constructions,batches,admitted,admitted,admitted,disposals,admitted*o),
            arrays,copies,reservedCopies,reservedBytes,bytes,reservedWork,work,sb,qb,qw);
    }
}
