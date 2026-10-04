using Stardust.Core.Assets;

namespace Stardust.Core.Geometry;

public sealed record TerrainSuppliedLayerHeightLimits(int MaximumSamples=256,int MaximumQueriesPerSample=256,
    int MaximumTotalQueries=65536,int MaximumRulesPerSample=256,int MaximumTotalRules=65536,long MaximumOperations=327680);

/// <summary>Owned ordered AHCN references and one immutable supplied layer sample.</summary>
public sealed class TerrainSuppliedLayerHeightSample
{
    private TerrainSuppliedLayerHeightSample(TerrainLayerInfluenceSample layer,uint initial,TerrainHeightConstantInput[] rules,
        TerrainHeightArithmeticPrecision precision,long charge)
    { Layer=layer;InitialHeightBits=initial;Rules=Array.AsReadOnly(rules);AffectorPrecision=precision;OperationCharge=charge; }
    public TerrainLayerInfluenceSample Layer { get; }
    public uint InitialHeightBits { get; }
    public IReadOnlyList<TerrainHeightConstantInput> Rules { get; }
    public TerrainHeightArithmeticPrecision AffectorPrecision { get; }
    /// <summary>Complete rule graph arithmetic plus final store reservation, regardless of skips.</summary>
    public long OperationCharge { get; }
    public static TerrainSuppliedLayerHeightSample Create(TerrainLayerInfluenceSample layer,uint initialHeightBits,
        ReadOnlySpan<TerrainHeightConstantInput> rules,TerrainHeightArithmeticPrecision affectorPrecision,int maximumRules=256)
    {
        ArgumentNullException.ThrowIfNull(layer);TerrainLayerInfluence.ValidatePrecision(affectorPrecision);
        if(maximumRules is <0 or >4096)throw new ArgumentOutOfRangeException(nameof(maximumRules));
        TerrainLayerInfluence.Need(rules.Length<=maximumRules,"Complete supplied rule budget.");long charge=0;
        foreach(var rule in rules)
        {
            ArgumentNullException.ThrowIfNull(rule);
            charge+=rule.OperationBits switch {1 or 2=>3,4=>1,_=>5};
        }
        return new(layer,initialHeightBits,rules.ToArray(),affectorPrecision,charge);
    }
}

public readonly record struct TerrainSuppliedLayerHeightVisit(int Index,TerrainHeightConstantResult Result);

public sealed class TerrainSuppliedLayerHeightReceipt
{
    internal TerrainSuppliedLayerHeightReceipt(TerrainSuppliedLayerHeightSample sample,TerrainLayerInfluenceReceipt influence,
        TerrainSuppliedLayerHeightVisit[] visits,uint final)
    { Sample=sample;Influence=influence;Visits=Array.AsReadOnly(visits);FinalHeightBits=final; }
    public TerrainSuppliedLayerHeightSample Sample { get; }
    public TerrainLayerInfluenceReceipt Influence { get; }
    public IReadOnlyList<TerrainSuppliedLayerHeightVisit> Visits { get; }
    public uint FinalHeightBits { get; }
    public bool FinalHeightIsFinite=>TerrainLayerInfluence.Finite(FinalHeightBits);
}

public sealed class TerrainSuppliedLayerHeightBatch
{
    internal TerrainSuppliedLayerHeightBatch(TerrainSuppliedLayerHeightReceipt[] results)=>Results=Array.AsReadOnly(results);
    public IReadOnlyList<TerrainSuppliedLayerHeightReceipt> Results { get; }
    public uint[] CopyHeightBits()=>Results.Select(r=>r.FinalHeightBits).ToArray();
}

/// <summary>Supplied local layer graph followed by ordered AHCN rules; no real filters or recursive generation.</summary>
public static class TerrainSuppliedLayerHeight
{
    public static TerrainSuppliedLayerHeightReceipt Evaluate(TerrainSuppliedLayerHeightSample sample,TerrainSuppliedLayerHeightLimits? limits=null)
    {
        ArgumentNullException.ThrowIfNull(sample);limits=Validate(limits);
        TerrainLayerInfluence.Need(limits.MaximumSamples>=1,"Supplied height sample budget.");
        var charge=Charge(sample,limits);Totals(charge.Queries,charge.Rules,charge.Operations,limits);
        return Execute(sample,limits);
    }
    public static TerrainSuppliedLayerHeightBatch EvaluateBatch(ReadOnlySpan<TerrainSuppliedLayerHeightSample> samples,
        TerrainSuppliedLayerHeightLimits? limits=null)
    {
        limits=Validate(limits);Preflight(samples,limits);
        var owned=samples.ToArray();var results=new TerrainSuppliedLayerHeightReceipt[owned.Length];
        for(int i=0;i<results.Length;i++)results[i]=Execute(owned[i],limits);
        return new(results);
    }
    private static (int Queries,int Rules,long Operations) Charge(TerrainSuppliedLayerHeightSample sample,TerrainSuppliedLayerHeightLimits limits)
    {
        ArgumentNullException.ThrowIfNull(sample);int count=sample.Layer.Boundaries.Count+sample.Layer.Filters.Count;
        TerrainLayerInfluence.Need(count<=limits.MaximumQueriesPerSample,"Supplied height per-sample query budget.");
        TerrainLayerInfluence.Need(sample.Rules.Count<=limits.MaximumRulesPerSample,"Supplied height per-sample rule budget.");
        return (count,sample.Rules.Count,sample.OperationCharge);
    }
    private static void Totals(long queries,long rules,long operations,TerrainSuppliedLayerHeightLimits limits)
    {
        TerrainLayerInfluence.Need(queries<=limits.MaximumTotalQueries,"Supplied height aggregate query budget.");
        TerrainLayerInfluence.Need(rules<=limits.MaximumTotalRules,"Supplied height aggregate rule budget.");
        TerrainLayerInfluence.Need(operations<=limits.MaximumOperations,"Supplied height operation budget.");
    }
    private static void Preflight(ReadOnlySpan<TerrainSuppliedLayerHeightSample> samples,TerrainSuppliedLayerHeightLimits limits)
    {
        TerrainLayerInfluence.Need(samples.Length<=limits.MaximumSamples,"Supplied height sample budget.");
        long queries=0,rules=0,operations=0;
        foreach(var sample in samples)
        {
            var charge=Charge(sample,limits);queries+=charge.Queries;rules+=charge.Rules;operations+=charge.Operations;
        }
        Totals(queries,rules,operations,limits);
    }
    private static TerrainSuppliedLayerHeightReceipt Execute(TerrainSuppliedLayerHeightSample sample,TerrainSuppliedLayerHeightLimits limits)
    {
        var influence=TerrainLayerInfluence.Evaluate(sample.Layer,new(1,limits.MaximumQueriesPerSample,limits.MaximumTotalQueries));
        uint height=sample.InitialHeightBits;
        if(!influence.AffectorEligible)return new(sample,influence,Array.Empty<TerrainSuppliedLayerHeightVisit>(),height);
        var visits=new TerrainSuppliedLayerHeightVisit[sample.Rules.Count];
        for(int i=0;i<visits.Length;i++)
        {
            var result=TerrainHeightConstantOperation.Evaluate(sample.Rules[i],height,influence.ProductBits,sample.AffectorPrecision);
            visits[i]=new(i,result);height=result.ResultHeightBits;
        }
        return new(sample,influence,visits,height);
    }
    private static TerrainSuppliedLayerHeightLimits Validate(TerrainSuppliedLayerHeightLimits? limits)
    {
        limits??=new();
        if(limits.MaximumSamples is <0 or >65536||limits.MaximumQueriesPerSample is <0 or >4096||limits.MaximumTotalQueries is <0 or >1048576
            ||limits.MaximumRulesPerSample is <0 or >4096||limits.MaximumTotalRules is <0 or >1048576||limits.MaximumOperations is <0 or >5242880)
            throw new ArgumentOutOfRangeException(nameof(limits));
        return limits;
    }
}

