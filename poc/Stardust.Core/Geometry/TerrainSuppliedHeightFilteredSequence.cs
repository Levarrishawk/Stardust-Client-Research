using Stardust.Core.Assets;
using Number=Stardust.Core.Geometry.TerrainCircleArithmetic.Number;
namespace Stardust.Core.Geometry;

public sealed class TerrainHeightFilteredLayer
{
    internal readonly TerrainLayerInfluenceEntry[] Boundaries;
    private readonly TerrainSuppliedLayerHeightSample template;
    private TerrainHeightFilteredLayer(TerrainLayerInfluenceEntry[] boundaries,TerrainFilterHeightInput[] filters,
        TerrainSuppliedLayerHeightSample template,int filterGate,bool invertFilter,TerrainHeightArithmeticPrecision query)
    { Boundaries=boundaries;Filters=Array.AsReadOnly(filters);this.template=template;FilterGate=filterGate;InvertFilter=invertFilter;QueryPrecision=query; }
    public IReadOnlyList<TerrainLayerInfluenceEntry> BoundaryEntries=>Array.AsReadOnly(Boundaries);
    public IReadOnlyList<TerrainFilterHeightInput> Filters { get; }
    public IReadOnlyList<TerrainHeightConstantInput> Rules=>template.Rules;
    public uint IncomingWeightBits=>template.Layer.IncomingWeightBits;
    public int BoundaryGate=>template.Layer.BoundaryGate;
    public int FilterGate { get; }
    public bool InvertBoundary=>template.Layer.InvertBoundary;
    public bool InvertFilter { get; }
    public TerrainHeightArithmeticPrecision QueryPrecision { get; }
    public TerrainHeightArithmeticPrecision CombinePrecision=>template.Layer.CombinePrecision;
    public TerrainHeightArithmeticPrecision ProductPrecision=>template.Layer.ProductPrecision;
    public TerrainHeightArithmeticPrecision AffectorPrecision=>template.AffectorPrecision;
    public long OperationCharge=>template.OperationCharge;
    public static TerrainHeightFilteredLayer Create(ReadOnlySpan<TerrainLayerInfluenceEntry> boundaries,
        ReadOnlySpan<TerrainFilterHeightInput> filters,ReadOnlySpan<TerrainHeightConstantInput> rules,uint incomingWeightBits,
        int boundaryGate,int filterGate,bool invertBoundary,bool invertFilter,TerrainHeightArithmeticPrecision queryPrecision,
        TerrainHeightArithmeticPrecision combinePrecision,TerrainHeightArithmeticPrecision productPrecision,
        TerrainHeightArithmeticPrecision affectorPrecision,int maximumQueries=256,int maximumRules=256)
    {
        TerrainLayerInfluence.ValidatePrecision(queryPrecision);
        if(maximumQueries is <0 or >4096)throw new ArgumentOutOfRangeException(nameof(maximumQueries));
        TerrainLayerInfluence.Need((long)boundaries.Length+filters.Length<=maximumQueries,"Complete filtered query budget.");
        foreach(var input in filters){ArgumentNullException.ThrowIfNull(input);TerrainLayerInfluence.Need(input.Header is not null&&input.Fields is not null,"Decoded FHGT header/fields required.");}
        var layer=TerrainLayerInfluenceSample.Create(incomingWeightBits,boundaries,Array.Empty<TerrainLayerInfluenceEntry>(),boundaryGate,0,invertBoundary,false,combinePrecision,productPrecision,maximumQueries);
        var template=TerrainSuppliedLayerHeightSample.Create(layer,0,rules,affectorPrecision,maximumRules);
        return new(boundaries.ToArray(),filters.ToArray(),template,filterGate,invertFilter,queryPrecision);
    }
}
public sealed class TerrainHeightFilteredPlan
{
    private TerrainHeightFilteredPlan(uint initial,TerrainHeightFilteredLayer[] layers){InitialHeightBits=initial;Layers=Array.AsReadOnly(layers);}
    public uint InitialHeightBits { get; }
    public IReadOnlyList<TerrainHeightFilteredLayer> Layers { get; }
    public static TerrainHeightFilteredPlan Create(uint initialHeightBits,ReadOnlySpan<TerrainHeightFilteredLayer> layers,int maximumLayers=64)
    { if(maximumLayers is <0 or >4096)throw new ArgumentOutOfRangeException(nameof(maximumLayers));TerrainLayerInfluence.Need(layers.Length<=maximumLayers,"Filtered plan cap.");foreach(var layer in layers)ArgumentNullException.ThrowIfNull(layer);return new(initialHeightBits,layers.ToArray()); }
}
public sealed record TerrainHeightFilteredLimits(TerrainSuppliedLayerSequenceLimits? Sequence=null,long MaximumQueryWork=196608,long MaximumCandidateSlots=589824);
public enum TerrainHeightFilterBindingState { Queried, Disabled, BoundaryGateFailed, FilterGateDisabled, EarlierZeroExit }
public readonly record struct TerrainHeightFilterBinding(int Index,TerrainHeightFilterBindingState State,TerrainHeightFilterQuery? Query,uint? CandidateBits);
public readonly record struct TerrainHeightFilteredStage(int Index,TerrainSuppliedLayerHeightReceipt Receipt,IReadOnlyList<TerrainHeightFilterBinding> Bindings);
public sealed class TerrainHeightFilteredReceipt
{
    internal TerrainHeightFilteredReceipt(TerrainHeightFilteredPlan plan,TerrainHeightFilteredStage[] stages,uint final){Plan=plan;Stages=Array.AsReadOnly(stages);FinalHeightBits=final;}
    public TerrainHeightFilteredPlan Plan { get; }
    public IReadOnlyList<TerrainHeightFilteredStage> Stages { get; }
    public uint FinalHeightBits { get; }
    public bool FinalHeightIsFinite=>TerrainLayerInfluence.Finite(FinalHeightBits);
}
public sealed class TerrainHeightFilteredBatch
{
    internal TerrainHeightFilteredBatch(TerrainHeightFilteredReceipt[] results)=>Results=Array.AsReadOnly(results);
    public IReadOnlyList<TerrainHeightFilteredReceipt> Results { get; }
    public uint[] CopyHeightBits()=>Results.Select(r=>r.FinalHeightBits).ToArray();
}
public static class TerrainSuppliedHeightFilteredSequence
{
    public static TerrainHeightFilteredReceipt Evaluate(TerrainHeightFilteredPlan plan,TerrainHeightFilteredLimits? limits=null)
    { ArgumentNullException.ThrowIfNull(plan);var l=Validate(limits);Preflight(new[]{plan},l);return Execute(plan,l); }
    public static TerrainHeightFilteredBatch EvaluateBatch(ReadOnlySpan<TerrainHeightFilteredPlan> plans,TerrainHeightFilteredLimits? limits=null)
    { var l=Validate(limits);Preflight(plans,l);var owned=plans.ToArray();var results=new TerrainHeightFilteredReceipt[owned.Length];for(int i=0;i<results.Length;i++)results[i]=Execute(owned[i],l);return new(results); }
    private static TerrainHeightFilteredLimits Validate(TerrainHeightFilteredLimits? limits)
    {
        limits??=new();var s=limits.Sequence??new();
        // Reuse the frozen public configuration validator before any child work.
        TerrainSuppliedLayerSequence.EvaluateBatch(Array.Empty<TerrainSuppliedLayerSequencePlan>(),s);
        if(limits.MaximumQueryWork is <0 or >3145728||limits.MaximumCandidateSlots is <0 or >9437184)throw new ArgumentOutOfRangeException(nameof(limits));
        return limits with {Sequence=s};
    }
    private static void Preflight(ReadOnlySpan<TerrainHeightFilteredPlan> plans,TerrainHeightFilteredLimits limits)
    {
        var s=limits.Sequence!;TerrainLayerInfluence.Need(plans.Length<=s.MaximumSamples,"Filtered sample cap.");
        long layers=0,queries=0,rules=0,ops=0,work=0;int largest=0;
        foreach(var plan in plans)
        {
            ArgumentNullException.ThrowIfNull(plan);TerrainLayerInfluence.Need(plan.Layers.Count<=s.MaximumLayersPerSample,"Filtered per-plan layer cap.");
            foreach(var layer in plan.Layers)
            {
                int b=layer.Boundaries.Length,f=layer.Filters.Count,q=b+f,r=layer.Rules.Count;
                TerrainLayerInfluence.Need(q<=s.MaximumQueriesPerLayer&&r<=s.MaximumRulesPerLayer,"Filtered per-layer caps.");
                layers++;queries+=q;rules+=r;ops+=layer.OperationCharge;work+=2L*b+3L*f;largest=Math.Max(largest,r);
                TerrainLayerInfluence.Need(layers<=s.MaximumTotalLayers&&queries<=s.MaximumTotalQueries&&rules<=s.MaximumTotalRules&&ops<=s.MaximumOperations&&2*rules+largest<=s.MaximumRuleReferenceSlots&&work<=limits.MaximumQueryWork&&9*queries<=limits.MaximumCandidateSlots,"Filtered aggregate reservation.");
            }
        }
    }
    private static TerrainHeightFilteredReceipt Execute(TerrainHeightFilteredPlan plan,TerrainHeightFilteredLimits limits)
    {
        var s=limits.Sequence!;var stages=new TerrainHeightFilteredStage[plan.Layers.Count];uint height=plan.InitialHeightBits;
        for(int index=0;index<stages.Length;index++)
        {
            var layer=plan.Layers[index];
            var probeSample=TerrainLayerInfluenceSample.Create(0,layer.Boundaries,Array.Empty<TerrainLayerInfluenceEntry>(),layer.BoundaryGate,0,layer.InvertBoundary,false,layer.CombinePrecision,layer.ProductPrecision,s.MaximumQueriesPerLayer);
            var probe=TerrainLayerInfluence.Evaluate(probeSample,new(1,s.MaximumQueriesPerLayer,s.MaximumTotalQueries));
            uint accumulator=probe.LocalInfluenceBits;bool stopped=false;int prefix=0;
            var entries=new TerrainLayerInfluenceEntry[layer.Filters.Count];var bindings=new TerrainHeightFilterBinding[layer.Filters.Count];
            for(int i=0;i<entries.Length;i++)
            {
                var input=layer.Filters[i];bool enabled=input.Header!.IsEnabled;uint selector=input.Fields!.FeatheringTypeBits??0;
                var state=!enabled?TerrainHeightFilterBindingState.Disabled:!probe.BoundaryPositive?TerrainHeightFilterBindingState.BoundaryGateFailed:layer.FilterGate<=0?TerrainHeightFilterBindingState.FilterGateDisabled:stopped?TerrainHeightFilterBindingState.EarlierZeroExit:TerrainHeightFilterBindingState.Queried;
                TerrainHeightFilterQuery? query=null;uint? candidate=null;
                if(state==TerrainHeightFilterBindingState.Queried)
                {
                    query=TerrainFilterHeightInfluence.Query(input,height,layer.QueryPrecision,new(1,1));var carrier=TerrainLayerInfluenceQuery.FromHeightFilter(query);
                    Number shaped=carrier.Shape(selector,layer.CombinePrecision);candidate=TerrainLayerInfluence.Store(shaped);
                    if(Number.Compare(shaped,Number.FromBits(accumulator))<0)accumulator=candidate.Value;
                    entries[prefix++]=new(true,selector,carrier);stopped=Number.Compare(Number.FromBits(accumulator),Number.FromBits(0))==0;
                }
                else if(!stopped&&probe.BoundaryPositive&&layer.FilterGate>0)entries[prefix++]=new(false,selector,null);
                bindings[i]=new(i,state,query,candidate);
            }
            var bound=TerrainLayerInfluenceSample.Create(layer.IncomingWeightBits,layer.Boundaries,entries.AsSpan(0,prefix),layer.BoundaryGate,layer.FilterGate,layer.InvertBoundary,layer.InvertFilter,layer.CombinePrecision,layer.ProductPrecision,s.MaximumQueriesPerLayer);
            var transfer=layer.Rules.ToArray();var sample=TerrainSuppliedLayerHeightSample.Create(bound,height,transfer,layer.AffectorPrecision,s.MaximumRulesPerLayer);
            var result=TerrainSuppliedLayerHeight.Evaluate(sample,new(1,s.MaximumQueriesPerLayer,s.MaximumTotalQueries,s.MaximumRulesPerLayer,s.MaximumTotalRules,s.MaximumOperations));
            stages[index]=new(index,result,Array.AsReadOnly(bindings));height=result.FinalHeightBits;
        }
        return new(plan,stages,height);
    }
}
