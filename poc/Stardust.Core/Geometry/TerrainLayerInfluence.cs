using Number=Stardust.Core.Geometry.TerrainCircleArithmetic.Number;

namespace Stardust.Core.Geometry;

public readonly record struct TerrainLayerInfluenceEntry(bool Enabled,uint SelectorBits,TerrainLayerInfluenceQuery? Query);
public sealed record TerrainLayerInfluenceLimits(int MaximumSamples=256,int MaximumQueriesPerSample=256,int MaximumTotalQueries=65536);

/// <summary>One supplied sample, not a layer callback or height-mutating traversal.</summary>
public sealed class TerrainLayerInfluenceSample
{
    private TerrainLayerInfluenceSample(uint incoming,TerrainLayerInfluenceEntry[] boundaries,TerrainLayerInfluenceEntry[] filters,
        int boundaryGate,int filterGate,bool invertBoundary,bool invertFilter,
        TerrainHeightArithmeticPrecision combine,TerrainHeightArithmeticPrecision product)
    {
        IncomingWeightBits=incoming;Boundaries=Array.AsReadOnly(boundaries);Filters=Array.AsReadOnly(filters);
        BoundaryGate=boundaryGate;FilterGate=filterGate;InvertBoundary=invertBoundary;InvertFilter=invertFilter;
        CombinePrecision=combine;ProductPrecision=product;
    }
    public uint IncomingWeightBits { get; }
    public IReadOnlyList<TerrainLayerInfluenceEntry> Boundaries { get; }
    public IReadOnlyList<TerrainLayerInfluenceEntry> Filters { get; }
    public int BoundaryGate { get; }
    public int FilterGate { get; }
    public bool InvertBoundary { get; }
    public bool InvertFilter { get; }
    public TerrainHeightArithmeticPrecision CombinePrecision { get; }
    public TerrainHeightArithmeticPrecision ProductPrecision { get; }
    public static TerrainLayerInfluenceSample Create(uint incomingWeightBits,ReadOnlySpan<TerrainLayerInfluenceEntry> boundaries,
        ReadOnlySpan<TerrainLayerInfluenceEntry> filters,int boundaryGate,int filterGate,bool invertBoundary,bool invertFilter,
        TerrainHeightArithmeticPrecision combinePrecision,TerrainHeightArithmeticPrecision productPrecision,int maximumQueries=256)
    {
        TerrainLayerInfluence.ValidatePrecision(combinePrecision);TerrainLayerInfluence.ValidatePrecision(productPrecision);
        if(maximumQueries is <0 or >4096)throw new ArgumentOutOfRangeException(nameof(maximumQueries));
        TerrainLayerInfluence.Need((long)boundaries.Length+filters.Length<=maximumQueries,"Complete supplied query budget.");
        TerrainLayerInfluence.Need(TerrainLayerInfluence.Finite(incomingWeightBits),"Finite incoming weight required.");
        foreach(var entry in boundaries)TerrainLayerInfluence.Need(!entry.Enabled||entry.Query is not null,"Enabled boundary requires a supplied query.");
        foreach(var entry in filters)TerrainLayerInfluence.Need(!entry.Enabled||entry.Query is not null,"Enabled filter requires a supplied query.");
        return new(incomingWeightBits,boundaries.ToArray(),filters.ToArray(),boundaryGate,filterGate,invertBoundary,invertFilter,combinePrecision,productPrecision);
    }
}

public readonly record struct TerrainLayerInfluenceVisit(int Index,bool Enabled,uint SelectorBits,uint? QueryProjectionBits,
    uint? CandidateBits,uint OldAccumulatorBits,uint SelectedBits,bool Replaced,bool EarlyExit);

public sealed class TerrainLayerInfluenceReceipt
{
    internal TerrainLayerInfluenceReceipt(TerrainLayerInfluenceSample sample,TerrainLayerInfluenceVisit[] boundaries,
        TerrainLayerInfluenceVisit[] filters,bool fallback,bool boundaryEarly,bool filterEarly,bool firstGate,uint local,uint product)
    {
        Sample=sample;BoundaryVisits=Array.AsReadOnly(boundaries);FilterVisits=Array.AsReadOnly(filters);
        BoundaryFallback=fallback;BoundaryEarlyExit=boundaryEarly;FilterEarlyExit=filterEarly;BoundaryPositive=firstGate;
        LocalInfluenceBits=local;ProductBits=product;
        LocalPositive=firstGate&&TerrainLayerInfluence.Positive(local);ProductPositive=TerrainLayerInfluence.Positive(product);
    }
    public TerrainLayerInfluenceSample Sample { get; }
    public IReadOnlyList<TerrainLayerInfluenceVisit> BoundaryVisits { get; }
    public IReadOnlyList<TerrainLayerInfluenceVisit> FilterVisits { get; }
    public bool BoundaryFallback { get; }
    public bool BoundaryEarlyExit { get; }
    public bool FilterEarlyExit { get; }
    public bool BoundaryPositive { get; }
    public bool LocalPositive { get; }
    public bool AffectorEligible=>LocalPositive;
    public bool ProductPositive { get; }
    public uint LocalInfluenceBits { get; }
    public uint ProductBits { get; }
}

public sealed class TerrainLayerInfluenceBatch
{
    internal TerrainLayerInfluenceBatch(TerrainLayerInfluenceReceipt[] results)=>Results=Array.AsReadOnly(results);
    public IReadOnlyList<TerrainLayerInfluenceReceipt> Results { get; }
    public TerrainLayerInfluenceReceipt[] CopyResults()=>Results.ToArray();
}

/// <summary>Conditional finite general sample graph over ordered supplied queries. No original profile assumption.</summary>
public static class TerrainLayerInfluence
{
    private static readonly Number Zero=Number.FromBits(0),One=Number.FromBits(0x3f800000);
    public static TerrainLayerInfluenceReceipt Evaluate(TerrainLayerInfluenceSample sample,TerrainLayerInfluenceLimits? limits=null)
    {
        ArgumentNullException.ThrowIfNull(sample);limits??=new();ValidateLimits(limits);
        Need(limits.MaximumSamples>=1&&QueryCount(sample)<=limits.MaximumQueriesPerSample&&QueryCount(sample)<=limits.MaximumTotalQueries,"Single sample budgets.");
        return Combine(sample);
    }
    public static TerrainLayerInfluenceBatch EvaluateBatch(ReadOnlySpan<TerrainLayerInfluenceSample> samples,TerrainLayerInfluenceLimits? limits=null)
    {
        limits??=new();ValidateLimits(limits);Need(samples.Length<=limits.MaximumSamples,"Batch sample budget.");
        long total=0;
        foreach(var sample in samples)
        {
            ArgumentNullException.ThrowIfNull(sample);int count=QueryCount(sample);
            Need(count<=limits.MaximumQueriesPerSample,"Per-sample supplied query budget.");total+=count;
        }
        Need(total<=limits.MaximumTotalQueries,"Aggregate supplied query budget.");
        var owned=samples.ToArray();var results=new TerrainLayerInfluenceReceipt[owned.Length];
        for(int i=0;i<results.Length;i++)results[i]=Combine(owned[i]);
        return new(results);
    }
    private static TerrainLayerInfluenceReceipt Combine(TerrainLayerInfluenceSample sample)
    {
        uint accumulator=0;bool any=false,boundaryEarly=false,filterEarly=false;int p=(int)sample.CombinePrecision;
        var boundaries=new List<TerrainLayerInfluenceVisit>();var filters=new List<TerrainLayerInfluenceVisit>();
        if(sample.BoundaryGate>0)
        {
            for(int i=0;i<sample.Boundaries.Count;i++)
            {
                var entry=sample.Boundaries[i];uint old=accumulator;
                if(!entry.Enabled){boundaries.Add(new(i,false,entry.SelectorBits,entry.Query?.ProjectionBits,null,old,old,false,false));continue;}
                any=true;Number shaped=entry.Query!.Shape(entry.SelectorBits,sample.CombinePrecision);uint candidate=Store(shaped);
                bool replace=Number.Compare(Number.FromBits(old),shaped)<0;if(replace)accumulator=candidate;
                boundaryEarly=Number.Compare(Number.FromBits(accumulator),One)==0;
                boundaries.Add(new(i,true,entry.SelectorBits,entry.Query.ProjectionBits,candidate,old,accumulator,replace,boundaryEarly));
                if(boundaryEarly)break;
            }
        }
        bool fallback=!any;if(fallback)accumulator=0x3f800000;
        if(sample.InvertBoundary)accumulator=Store(Number.Subtract(One,Number.FromBits(accumulator),p));
        bool firstGate=Positive(accumulator);
        if(firstGate)
        {
            if(sample.FilterGate>0)
            {
                for(int i=0;i<sample.Filters.Count;i++)
                {
                    var entry=sample.Filters[i];uint old=accumulator;
                    if(!entry.Enabled){filters.Add(new(i,false,entry.SelectorBits,entry.Query?.ProjectionBits,null,old,old,false,false));continue;}
                    Number shaped=entry.Query!.Shape(entry.SelectorBits,sample.CombinePrecision);uint candidate=Store(shaped);
                    bool replace=Number.Compare(shaped,Number.FromBits(old))<0;if(replace)accumulator=candidate;
                    filterEarly=Number.Compare(Number.FromBits(accumulator),Zero)==0;
                    filters.Add(new(i,true,entry.SelectorBits,entry.Query.ProjectionBits,candidate,old,accumulator,replace,filterEarly));
                    if(filterEarly)break;
                }
            }
            if(sample.InvertFilter)accumulator=Store(Number.Subtract(One,Number.FromBits(accumulator),p));
        }
        uint product=Store(Number.Multiply(Number.FromBits(accumulator),Number.FromBits(sample.IncomingWeightBits),(int)sample.ProductPrecision));
        return new(sample,boundaries.ToArray(),filters.ToArray(),fallback,boundaryEarly,filterEarly,firstGate,accumulator,product);
    }
    private static int QueryCount(TerrainLayerInfluenceSample sample)=>sample.Boundaries.Count+sample.Filters.Count;
    private static void ValidateLimits(TerrainLayerInfluenceLimits limits)
    {
        if(limits.MaximumSamples is <0 or >65536||limits.MaximumQueriesPerSample is <0 or >4096||limits.MaximumTotalQueries is <0 or >1048576)
            throw new ArgumentOutOfRangeException(nameof(limits));
    }
    internal static uint Store(Number value){uint bits=value.ToFloatBits();Need(Finite(bits),"Finite F32 store required.");return bits;}
    internal static bool Finite(uint bits)=>(bits&0x7f800000)!=0x7f800000;
    internal static bool Positive(uint bits)=>Number.Compare(Number.FromBits(bits),Zero)>0;
    internal static void ValidatePrecision(TerrainHeightArithmeticPrecision precision)
    {
        if(precision is not (TerrainHeightArithmeticPrecision.Binary24 or TerrainHeightArithmeticPrecision.Binary53 or TerrainHeightArithmeticPrecision.Binary64))throw new ArgumentOutOfRangeException(nameof(precision));
    }
    internal static void Need(bool value,string reason){if(!value)throw new InvalidDataException(reason);}
}
