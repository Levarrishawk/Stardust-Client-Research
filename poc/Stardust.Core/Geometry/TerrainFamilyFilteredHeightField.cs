using System.Security.Cryptography;
using Stardust.Core.Assets;
namespace Stardust.Core.Geometry;

public readonly record struct TerrainFamilyFilteredHeightFieldControls(TerrainFamilyHeightFractalControls Sampling,
    TerrainHeightArithmeticPrecision FilterQueryPrecision,TerrainHeightArithmeticPrecision CombinePrecision,TerrainHeightArithmeticPrecision ProductPrecision);
public sealed record TerrainFamilyFilteredHeightFieldLimits(int MaximumSamples=256,int MaximumRuleBytes=65536,
    long MaximumCopyBytes=4194304,long MaximumRouteBytes=134217728,long MaximumWork=67108864,
    int MaximumFilterQueryBytes=2097152,int MaximumFilterQueryWork=16777216,TerrainFamilyHeightFieldLimits? HeightField=null);
public sealed record TerrainFamilyFilteredHeightFieldCell(int Index,int Column,int Row,uint IncomingWeightBits,
    TerrainConditionalFractalSampler.Result Computed,TerrainFractalFilterQuery Query,TerrainLayerInfluenceQuery Carrier,
    TerrainLayerInfluenceReceipt Influence,TerrainFamilyHeightFieldCell Height);
public sealed record TerrainFamilyFilteredHeightFieldCounts(int HeightPrevalidations,int FilterSamplerConstructions,
    int FilterQueryBatches,int FilterComputedCells,int FilterQueries,int Products,int FilterSamplerDisposals,
    int FilterKernelOctaveQueries,TerrainFamilyHeightPatchSamplingCounts Height)
{
    public int HeightPositiveCells=>Height.ComputedCells;
    public int HeightSkippedCells=>FilterComputedCells-Height.ComputedCells;
}

/// <summary>Explicit filter-only modern composition: no boundaries, fallback one, no inversion or traversal.</summary>
public sealed class TerrainFamilyFilteredHeightField
{
    private readonly uint[] prior,incoming,products;
    private readonly byte[] rule;
    private TerrainFamilyFilteredHeightField(TerrainGeneratorFractalFilterRule filter,TerrainFamilyFilteredHeightFieldControls controls,
        byte[] rule,uint[] prior,uint[] incoming,uint[] products,TerrainFamilyFilteredHeightFieldCell[] cells,TerrainFamilyHeightField height,
        long copies,long reservedBytes,long reservedWork,int queryBytes,int queryWork,long bytes,long work)
    {
        Filter=filter;Controls=controls;this.rule=rule;this.prior=prior;this.incoming=incoming;this.products=products;HeightField=height;
        PriorHeightBits=Array.AsReadOnly(prior);IncomingWeightBits=Array.AsReadOnly(incoming);ProductBits=Array.AsReadOnly(products);Cells=Array.AsReadOnly(cells);
        LogicalCopyBytes=copies;ReservedRouteBytes=reservedBytes;ReservedWork=reservedWork;ReservedFilterQueryBytes=queryBytes;ReservedFilterQueryWork=queryWork;
        AccountedRouteBytes=bytes;AccountedWork=work;SamplingCounts=new(1,1,1,cells.Length,cells.Length,cells.Length,1,
            cells.Length*filter.Family.Raw.Fields.SignedOctaveCount,height.SamplingCounts);
    }
    public TerrainGeneratorFractalFilterRule Filter{get;}
    public TerrainFamilyFilteredHeightFieldControls Controls{get;}
    public TerrainFamilyHeightField HeightField{get;}
    public IReadOnlyList<uint> PriorHeightBits{get;}
    public IReadOnlyList<uint> IncomingWeightBits{get;}
    public IReadOnlyList<uint> ProductBits{get;}
    public IReadOnlyList<TerrainFamilyFilteredHeightFieldCell> Cells{get;}
    public TerrainFamilyFilteredHeightFieldCounts SamplingCounts{get;}
    public long LogicalCopyBytes{get;}
    public long ReservedRouteBytes{get;}
    public long ReservedWork{get;}
    public int ReservedFilterQueryBytes{get;}
    public int ReservedFilterQueryWork{get;}
    public long AccountedRouteBytes{get;}
    public long AccountedWork{get;}
    public uint[] CopyPriorHeightBits()=>(uint[])prior.Clone();
    public uint[] CopyIncomingWeightBits()=>(uint[])incoming.Clone();
    public uint[] CopyProductBits()=>(uint[])products.Clone();
    public byte[] CopyEncodedHeightRule()=>(byte[])rule.Clone();
    private static void Need(bool value,string reason){if(!value)throw new InvalidDataException(reason);}
    private static bool Finite(uint v)=>(v&0x7f800000)!=0x7f800000;
    private static bool Positive(uint v)=>(v&0x80000000)==0&&(v&0x7fffffff)!=0;
    public static TerrainFamilyFilteredHeightField Build(TerrainFractalFamilySet families,TerrainGeneratorFractalFilterRule filter,
        ReadOnlySpan<byte> heightRule,TerrainFamilyHeightFieldRequest request,ReadOnlySpan<uint> priorHeightBits,
        ReadOnlySpan<uint> incomingWeightBits,TerrainFamilyFilteredHeightFieldControls controls,
        TerrainFamilyFilteredHeightFieldLimits? limits=null,CancellationToken cancellation=default)
    {
        ArgumentNullException.ThrowIfNull(families);ArgumentNullException.ThrowIfNull(filter);limits??=new();var h=limits.HeightField??new();
        if(limits.MaximumSamples is <0 or >1024||limits.MaximumRuleBytes is <24 or >65536||limits.MaximumCopyBytes is <0 or >8388608||
            limits.MaximumRouteBytes is <0 or >536870912||limits.MaximumWork is <0 or >134217728||limits.MaximumFilterQueryBytes is <0 or >2097152||
            limits.MaximumFilterQueryWork is <0 or >16777216)throw new ArgumentOutOfRangeException(nameof(limits));
        if(h.MaximumSamples is <0 or >1024||h.MaximumRuleBytes is <0 or >65536||h.MaximumCopyBytes is <0 or >4194304||
            h.MaximumRouteBytes is <0 or >268435456||h.MaximumWork is <0 or >67108864||h.MaximumQueryBytes is <0 or >2097152||
            h.MaximumQueryWork is <0 or >16777216)throw new ArgumentOutOfRangeException(nameof(limits));
        foreach(int p in new[]{controls.Sampling.TablePrecision,controls.Sampling.ConstructionPrecision,controls.Sampling.QueryPrecision,
            (int)controls.Sampling.ConsumerPrecision,(int)controls.FilterQueryPrecision,(int)controls.CombinePrecision,(int)controls.ProductPrecision})
            if(p is not (24 or 53 or 64))throw new ArgumentOutOfRangeException(nameof(controls));
        Need(request.Width is >=2 and <=128&&request.Rows is >=2 and <=128,"Bounded full rectangle required.");int n=request.Width*request.Rows;
        Need(n<=limits.MaximumSamples&&n<=h.MaximumSamples&&priorHeightBits.Length==n&&incomingWeightBits.Length==n,"Complete row-major operands required.");
        Need(Finite(request.OriginXBits)&&Finite(request.OriginZBits)&&Finite(request.SpacingXBits)&&Finite(request.SpacingZBits)&&Positive(request.SpacingXBits)&&Positive(request.SpacingZBits),"Finite origin and positive spacing required.");
        int l=heightRule.Length,f=filter.RuleBytes;Need(l>=24&&l<=limits.MaximumRuleBytes&&l<=h.MaximumRuleBytes&&f>=24&&f<=limits.MaximumRuleBytes,"Bounded complete rules required.");
        long hc=l+4L*(request.Width+request.Rows)+100L*n+256,hd=8L*l+65536;
        long hb=hd+9512+136+32L*n+1696L*n,hw=l+32L*(request.Width+request.Rows)+24L*n+l+20000+256+n+8304L*n;
        int qb=1456*n,qw=8257*n;
        long copies=hc+l+f+8L*n+4L*(request.Width+request.Rows)+512L*n+256;
        long shared=hd+9512+136+32L*n,sharedWork=l+20000+256+n;
        long reserved=hb+shared+1760L*n,reservedWork=hw+sharedWork+8272L*n+32L*(request.Width+request.Rows)+f;
        Need(hc<=h.MaximumCopyBytes&&hb<=h.MaximumRouteBytes&&hw<=h.MaximumWork&&qb<=h.MaximumQueryBytes&&qw<=h.MaximumQueryWork,
            "Complete downstream field reservation before copies.");
        Need(copies<=limits.MaximumCopyBytes&&reserved<=limits.MaximumRouteBytes&&reservedWork<=limits.MaximumWork&&qb<=limits.MaximumFilterQueryBytes&&qw<=limits.MaximumFilterQueryWork,
            "Complete all-positive aggregate/filter reservation before copies.");
        cancellation.ThrowIfCancellationRequested();var rule=heightRule.ToArray();var prior=priorHeightBits.ToArray();var incoming=incomingWeightBits.ToArray();
        for(int i=0;i<n;i++){cancellation.ThrowIfCancellationRequested();Need(Finite(prior[i])&&Finite(incoming[i]),"Finite owned operands required.");}
        Need(filter.Policy==TerrainGeneratorFractalFilterRulePolicy.EnabledOperationalSubset&&filter.IsEnabled&&filter.HasOperationalInputs,
            "Explicit accepted operational filter required.");
        Need(filter.GroupSha256==families.SourceSha256&&filter.GroupBytes==families.InputBytes&&ReferenceEquals(families.Find(filter.FamilyId),filter.Family),"Exact selected group/family identity required.");
        Need(filter.Input.Version=="0005"&&filter.Input.HasDecodedFields&&filter.Fields.FamilyIdBits==unchecked((uint)filter.Family.Id)&&
            filter.Fields.HasFiniteRange&&filter.Fields.HasFiniteSerializedFeatheringAmount&&Finite(filter.Fields.SampleScaleBits),"Strict finite FFRA eligibility required.");
        var raw=filter.Family.Raw.Fields;
        Need(raw.ModeBits<=5&&!raw.FirstCurveEnabled&&!raw.SecondCurveEnabled&&raw.SignedOctaveCount is >=1 and <=16&&raw.SignedSeed!=int.MaxValue&&
            Finite(raw.FrequencyMultiplierBits)&&Finite(raw.AmplitudeMultiplierBits)&&Finite(raw.FirstCoordinateScaleBits)&&Finite(raw.SecondCoordinateScaleBits)&&Finite(raw.FirstOffsetBits)&&Finite(raw.SecondOffsetBits),"Strict FFRA family eligibility required.");
        var filterBytes=filter.CopyEncodedBytes();Need(filterBytes.Length==f&&Convert.ToHexString(SHA256.HashData(filterBytes))==filter.RuleSha256,"Selected owned rule identity required.");
        _=TerrainFamilyHeightFractal.Prepare(families,rule,h.MaximumRuleBytes,cancellation); // Validate even zero incoming BEFORE any filter query.
        var x=TerrainFamilyHeightPatch.Axis(request.OriginXBits,request.SpacingXBits,request.Width,cancellation);
        var z=TerrainFamilyHeightPatch.Axis(request.OriginZBits,request.SpacingZBits,request.Rows,cancellation);
        var requests=new TerrainConditionalFractalSampler.Request[n];for(int i=0;i<n;i++)requests[i]=new(i%request.Width,i/request.Width,x[i%request.Width],z[i/request.Width]);
        var products=new uint[n];var queries=new TerrainFractalFilterQuery[n];var carriers=new TerrainLayerInfluenceQuery[n];var influences=new TerrainLayerInfluenceReceipt[n];
        IReadOnlyList<TerrainConditionalFractalSampler.Result> computed;
        using(var sampler=TerrainConditionalFractalSampler.CreateForSupportedModes(filter.Family.Raw,request.Width,request.Rows,
            controls.Sampling.TablePrecision,controls.Sampling.ConstructionPrecision,9512+16*n+136,20256,false,cancellation))
        {
            computed=sampler.Query(requests,controls.Sampling.QueryPrecision,qb,qw,cancellation);
            for(int i=0;i<n;i++)
            {
                cancellation.ThrowIfCancellationRequested();Need(computed[i].Origin==TerrainConditionalFractalOrigin.ConditionalComputedMiss,"Fresh unique full-field filter miss required.");
                queries[i]=TerrainFilterFractalInfluence.Query(filter.Input,computed[i].Sample,controls.FilterQueryPrecision);
                carriers[i]=TerrainLayerInfluenceQuery.FromRetainedFractalFilter(queries[i]);
                influences[i]=TerrainLayerInfluence.Evaluate(TerrainLayerInfluenceSample.Create(incoming[i],[],
                    [new(true,filter.Fields.FeatheringTypeBits,carriers[i])],1,1,false,false,controls.CombinePrecision,controls.ProductPrecision));
                products[i]=influences[i].ProductBits;
            }
        }
        cancellation.ThrowIfCancellationRequested();var height=TerrainFamilyHeightField.Build(families,rule,request,prior,products,controls.Sampling,h,cancellation);
        var cells=new TerrainFamilyFilteredHeightFieldCell[n];for(int i=0;i<n;i++)cells[i]=new(i,i%request.Width,i/request.Width,incoming[i],computed[i],queries[i],carriers[i],influences[i],height.Cells[i]);
        long bytes=height.AccountedRouteBytes+shared+n*(480L+80L*raw.SignedOctaveCount);
        long work=height.AccountedWork+sharedWork+n*(80L+512L*raw.SignedOctaveCount)+32L*(request.Width+request.Rows)+f;
        cancellation.ThrowIfCancellationRequested();return new(filter,controls,rule,prior,incoming,products,cells,height,copies,reserved,reservedWork,qb,qw,bytes,work);
    }
}
