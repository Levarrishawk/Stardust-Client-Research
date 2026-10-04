using Stardust.Core.Assets;

namespace Stardust.Core.Geometry;

/// <summary>Owned immutable descriptor; its frozen template's initial zero is never used by traversal.</summary>
public sealed class TerrainSuppliedLayerSequenceLayer
{
    private readonly TerrainSuppliedLayerHeightSample template;
    private TerrainSuppliedLayerSequenceLayer(TerrainSuppliedLayerHeightSample template)=>this.template=template;
    public TerrainLayerInfluenceSample Layer=>template.Layer;
    public IReadOnlyList<TerrainHeightConstantInput> Rules=>template.Rules;
    public TerrainHeightArithmeticPrecision AffectorPrecision=>template.AffectorPrecision;
    public long OperationCharge=>template.OperationCharge;
    public static TerrainSuppliedLayerSequenceLayer Create(TerrainLayerInfluenceSample layer,
        ReadOnlySpan<TerrainHeightConstantInput> rules,TerrainHeightArithmeticPrecision affectorPrecision,int maximumRules=256)
        =>new(TerrainSuppliedLayerHeightSample.Create(layer,0,rules,affectorPrecision,maximumRules));
    internal TerrainSuppliedLayerHeightSample Child(uint initial,int maximumRules)
    {
        // Frozen public boundary requires a span: one transfer array and one owned child snapshot.
        var transfer=template.Rules.ToArray();
        return TerrainSuppliedLayerHeightSample.Create(template.Layer,initial,transfer,template.AffectorPrecision,maximumRules);
    }
}

public readonly record struct TerrainSuppliedLayerSequenceCharge(long Layers,long Queries,long Rules,long Operations,long RuleReferenceSlots);

/// <summary>Modern supplied flat plan. Incoming weights and fixed queries are retained per descriptor.</summary>
public sealed class TerrainSuppliedLayerSequencePlan
{
    private TerrainSuppliedLayerSequencePlan(uint initial,TerrainSuppliedLayerSequenceLayer[] layers,
        long queries,long rules,long operations,int maxQueries,int maxRules)
    {
        InitialHeightBits=initial;Layers=Array.AsReadOnly(layers);MaximumQueriesInLayer=maxQueries;MaximumRulesInLayer=maxRules;
        Charge=new(layers.Length,queries,rules,operations,2*rules+maxRules);
    }
    public uint InitialHeightBits { get; }
    public IReadOnlyList<TerrainSuppliedLayerSequenceLayer> Layers { get; }
    public TerrainSuppliedLayerSequenceCharge Charge { get; }
    internal int MaximumQueriesInLayer { get; }
    internal int MaximumRulesInLayer { get; }
    public static TerrainSuppliedLayerSequencePlan Create(uint initialHeightBits,
        ReadOnlySpan<TerrainSuppliedLayerSequenceLayer> layers,int maximumLayers=64)
    {
        if(maximumLayers is <0 or >4096)throw new ArgumentOutOfRangeException(nameof(maximumLayers));
        TerrainLayerInfluence.Need(layers.Length<=maximumLayers,"Complete flat layer plan budget.");
        long queries=0,rules=0,operations=0;int maxQueries=0,maxRules=0;
        foreach(var layer in layers)
        {
            ArgumentNullException.ThrowIfNull(layer);int count=layer.Layer.Boundaries.Count+layer.Layer.Filters.Count;
            queries+=count;rules+=layer.Rules.Count;operations+=layer.OperationCharge;
            maxQueries=Math.Max(maxQueries,count);maxRules=Math.Max(maxRules,layer.Rules.Count);
        }
        return new(initialHeightBits,layers.ToArray(),queries,rules,operations,maxQueries,maxRules);
    }
}

public sealed record TerrainSuppliedLayerSequenceLimits(int MaximumSamples=256,int MaximumLayersPerSample=64,
    int MaximumTotalLayers=16384,int MaximumQueriesPerLayer=256,int MaximumTotalQueries=65536,
    int MaximumRulesPerLayer=256,int MaximumTotalRules=65536,long MaximumOperations=327680,long MaximumRuleReferenceSlots=131328);

public readonly record struct TerrainSuppliedLayerSequenceStage(int Index,TerrainSuppliedLayerHeightReceipt Receipt);
public sealed class TerrainSuppliedLayerSequenceReceipt
{
    internal TerrainSuppliedLayerSequenceReceipt(TerrainSuppliedLayerSequencePlan plan,TerrainSuppliedLayerSequenceStage[] stages,uint final)
    { Plan=plan;Stages=Array.AsReadOnly(stages);FinalHeightBits=final; }
    public TerrainSuppliedLayerSequencePlan Plan { get; }
    public IReadOnlyList<TerrainSuppliedLayerSequenceStage> Stages { get; }
    public uint FinalHeightBits { get; }
    public bool FinalHeightIsFinite=>TerrainLayerInfluence.Finite(FinalHeightBits);
}
public sealed class TerrainSuppliedLayerSequenceBatch
{
    internal TerrainSuppliedLayerSequenceBatch(TerrainSuppliedLayerSequenceReceipt[] results,TerrainSuppliedLayerSequenceCharge charge)
    { Results=Array.AsReadOnly(results);Charge=charge; }
    public IReadOnlyList<TerrainSuppliedLayerSequenceReceipt> Results { get; }
    public TerrainSuppliedLayerSequenceCharge Charge { get; }
    public uint[] CopyHeightBits()=>Results.Select(r=>r.FinalHeightBits).ToArray();
}

/// <summary>Source-order height threading across supplied flat layers; no recursive/filter/cache semantics.</summary>
public static class TerrainSuppliedLayerSequence
{
    public static TerrainSuppliedLayerSequenceReceipt Evaluate(TerrainSuppliedLayerSequencePlan plan,TerrainSuppliedLayerSequenceLimits? limits=null)
    {
        ArgumentNullException.ThrowIfNull(plan);limits=Validate(limits);
        TerrainLayerInfluence.Need(limits.MaximumSamples>=1,"Flat sequence sample budget.");
        CheckPlan(plan,limits);CheckTotals(plan.Charge,limits);return Execute(plan,limits);
    }
    public static TerrainSuppliedLayerSequenceBatch EvaluateBatch(ReadOnlySpan<TerrainSuppliedLayerSequencePlan> plans,
        TerrainSuppliedLayerSequenceLimits? limits=null)
    {
        limits=Validate(limits);TerrainLayerInfluence.Need(plans.Length<=limits.MaximumSamples,"Flat sequence sample budget.");
        long layers=0,queries=0,rules=0,operations=0;int largest=0;
        foreach(var plan in plans)
        {
            ArgumentNullException.ThrowIfNull(plan);CheckPlan(plan,limits);
            layers+=plan.Charge.Layers;queries+=plan.Charge.Queries;rules+=plan.Charge.Rules;operations+=plan.Charge.Operations;
            largest=Math.Max(largest,plan.MaximumRulesInLayer);
            CheckTotals(new(layers,queries,rules,operations,2*rules+largest),limits);
        }
        var charge=new TerrainSuppliedLayerSequenceCharge(layers,queries,rules,operations,2*rules+largest);
        var owned=plans.ToArray();var results=new TerrainSuppliedLayerSequenceReceipt[owned.Length];
        for(int i=0;i<results.Length;i++)results[i]=Execute(owned[i],limits);
        return new(results,charge);
    }
    private static void CheckPlan(TerrainSuppliedLayerSequencePlan plan,TerrainSuppliedLayerSequenceLimits limits)
    {
        TerrainLayerInfluence.Need(plan.Layers.Count<=limits.MaximumLayersPerSample,"Flat sequence per-sample layer budget.");
        TerrainLayerInfluence.Need(plan.MaximumQueriesInLayer<=limits.MaximumQueriesPerLayer,"Flat sequence per-layer query budget.");
        TerrainLayerInfluence.Need(plan.MaximumRulesInLayer<=limits.MaximumRulesPerLayer,"Flat sequence per-layer rule budget.");
    }
    private static void CheckTotals(TerrainSuppliedLayerSequenceCharge charge,TerrainSuppliedLayerSequenceLimits limits)
    {
        TerrainLayerInfluence.Need(charge.Layers<=limits.MaximumTotalLayers,"Flat sequence total layer budget.");
        TerrainLayerInfluence.Need(charge.Queries<=limits.MaximumTotalQueries,"Flat sequence total query budget.");
        TerrainLayerInfluence.Need(charge.Rules<=limits.MaximumTotalRules,"Flat sequence total rule budget.");
        TerrainLayerInfluence.Need(charge.Operations<=limits.MaximumOperations,"Flat sequence operation budget.");
        TerrainLayerInfluence.Need(charge.RuleReferenceSlots<=limits.MaximumRuleReferenceSlots,"Flat sequence rule reference budget.");
    }
    private static TerrainSuppliedLayerSequenceReceipt Execute(TerrainSuppliedLayerSequencePlan plan,TerrainSuppliedLayerSequenceLimits limits)
    {
        var childLimits=new TerrainSuppliedLayerHeightLimits(1,limits.MaximumQueriesPerLayer,limits.MaximumTotalQueries,
            limits.MaximumRulesPerLayer,limits.MaximumTotalRules,limits.MaximumOperations);
        var stages=new TerrainSuppliedLayerSequenceStage[plan.Layers.Count];uint height=plan.InitialHeightBits;
        for(int i=0;i<stages.Length;i++)
        {
            var receipt=TerrainSuppliedLayerHeight.Evaluate(plan.Layers[i].Child(height,limits.MaximumRulesPerLayer),childLimits);
            stages[i]=new(i,receipt);height=receipt.FinalHeightBits;
        }
        return new(plan,stages,height);
    }
    private static TerrainSuppliedLayerSequenceLimits Validate(TerrainSuppliedLayerSequenceLimits? limits)
    {
        limits??=new();
        if(limits.MaximumSamples is <0 or >65536||limits.MaximumLayersPerSample is <0 or >4096||limits.MaximumTotalLayers is <0 or >65536
            ||limits.MaximumQueriesPerLayer is <0 or >4096||limits.MaximumTotalQueries is <0 or >1048576||limits.MaximumRulesPerLayer is <0 or >4096
            ||limits.MaximumTotalRules is <0 or >1048576||limits.MaximumOperations is <0 or >5242880||limits.MaximumRuleReferenceSlots is <0 or >2101248)
            throw new ArgumentOutOfRangeException(nameof(limits));
        return limits;
    }
}
