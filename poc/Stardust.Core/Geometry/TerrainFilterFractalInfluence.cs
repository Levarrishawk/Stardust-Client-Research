using Stardust.Core.Assets;
using Number = Stardust.Core.Geometry.TerrainCircleArithmetic.Number;

namespace Stardust.Core.Geometry;

public sealed record TerrainFractalFilterLimits(int MaximumSamples=65536,long MaximumQueryWork=196608);
public sealed record TerrainFractalFilterCache(TerrainFilterFractalInput Input,uint FamilyIdBits,uint SampleScaleBits,uint LowerSampleBits,uint UpperSampleBits,uint RuntimeAmountBits);
public enum TerrainFractalFilterBranch { Outside, LowerFeather, UpperFeather, Interior }

/// <summary>Owned input plus a supplied F32 sampler return; inspection projection is not the Shape input.</summary>
public sealed class TerrainFractalFilterQuery
{
    internal TerrainFractalFilterQuery(TerrainFilterFractalInput input,uint sample,uint scaled,uint amount,
        TerrainHeightArithmeticPrecision precision,TerrainFractalFilterBranch branch,Number value,uint? span,TerrainSuppliedFractalSample? retainedSample=null)
    { RetainedSample=retainedSample;Input=input;SuppliedSampleBits=sample;ScaledSampleBits=scaled;RuntimeAmountBits=amount;RuntimeCache=new(input,input.Fields!.FamilyIdBits,input.Fields.SampleScaleBits,input.Fields.LowerSampleBits,input.Fields.UpperSampleBits,amount);Precision=precision;Branch=branch;Value=value;QueryProjectionBits=value.ToFloatBits();FeatherSpanProjectionBits=span; }
    public TerrainSuppliedFractalSample? RetainedSample { get; }
    public TerrainFilterFractalInput Input { get; }
    public uint SuppliedSampleBits { get; }
    public uint ScaledSampleBits { get; }
    public TerrainFractalFilterCache RuntimeCache { get; }
    public uint RuntimeAmountBits { get; }
    public TerrainHeightArithmeticPrecision Precision { get; }
    public TerrainFractalFilterBranch Branch { get; }
    public uint QueryProjectionBits { get; }
    public uint? FeatherSpanProjectionBits { get; }
    internal Number Value { get; }
}
public readonly record struct TerrainFractalFilterResult(uint SuppliedSampleBits,uint EnabledBits,uint SelectorBits,
    TerrainHeightArithmeticPrecision Precision,TerrainFractalFilterQuery? Query,uint? InfluenceBits,long QueryWork)
{ public bool SkippedDisabled=>Query is null; }
public readonly record struct TerrainRetainedFractalFilterResult(TerrainFractalFilterResult Consumer,TerrainSuppliedFractalSample? Sample);
public sealed class TerrainFractalFilterBatch
{
    internal TerrainFractalFilterBatch(TerrainFractalFilterResult[] results,long work)
    { Results=Array.AsReadOnly(results);QueryWork=work; }
    public IReadOnlyList<TerrainFractalFilterResult> Results { get; }
    public long QueryWork { get; }
    public uint?[] CopyInfluenceBits()=>Results.Select(r=>r.InfluenceBits).ToArray();
}

/// <summary>Conditional FFRA consumer of a supplied F32 sampler return; exact scale store and retained query.</summary>
public static class TerrainFilterFractalInfluence
{
    private static readonly Number Zero=Number.FromBits(0),One=Number.FromBits(0x3f800000),
        Half=Number.FromBits(0x3f000000),Three=Number.FromBits(0x40400000);
    public static TerrainFractalFilterQuery Query(TerrainFilterFractalInput input,uint suppliedSampleBits,
        TerrainHeightArithmeticPrecision precision,TerrainFractalFilterLimits? limits=null)
    { ArgumentNullException.ThrowIfNull(input);Policy(precision);limits=Limits(limits);Fields(input);Cap(1,2,limits);return QueryCore(input,suppliedSampleBits,precision); }
    public static uint Shape(TerrainFractalFilterQuery query,uint selectorBits)
    {
        ArgumentNullException.ThrowIfNull(query);int p=(int)query.Precision;Number u=query.Value;
        Number result=selectorBits switch
        {
            0=>u,1=>Number.Multiply(u,u,p),2=>Number.SquareRoot(u,p),
            3=>Number.Multiply(Number.Multiply(Number.Subtract(Three,Number.Add(u,u,p),p),u,p),u,p),_=>Zero
        };
        return Store(result);
    }
    public static TerrainFractalFilterResult Evaluate(TerrainFilterFractalInput input,uint suppliedSampleBits,
        TerrainHeightArithmeticPrecision precision,TerrainFractalFilterLimits? limits=null)
    {
        ArgumentNullException.ThrowIfNull(input);Policy(precision);limits=Limits(limits);var fields=Fields(input);
        Need(input.Header is not null,"Decoded header required.");Cap(1,input.Header!.IsEnabled?3:0,limits);
        uint enabled=input.Header.EnabledBits,selector=fields.FeatheringTypeBits;
        if(!input.Header.IsEnabled)return new(suppliedSampleBits,enabled,selector,precision,null,null,0);
        var query=QueryCore(input,suppliedSampleBits,precision);
        return new(suppliedSampleBits,enabled,selector,precision,query,Shape(query,selector),3);
    }
    public static TerrainFractalFilterBatch EvaluateBatch(TerrainFilterFractalInput input,ReadOnlySpan<uint> suppliedSampleBits,
        TerrainHeightArithmeticPrecision precision,TerrainFractalFilterLimits? limits=null)
    {
        ArgumentNullException.ThrowIfNull(input);Policy(precision);limits=Limits(limits);var fields=Fields(input);
        Need(input.Header is not null,"Decoded header required.");bool active=input.Header!.IsEnabled;
        long work=active?3L*suppliedSampleBits.Length:0;Cap(suppliedSampleBits.Length,work,limits);
        uint[] heights=suppliedSampleBits.ToArray();var results=new TerrainFractalFilterResult[heights.Length];
        uint selector=fields.FeatheringTypeBits,enabled=input.Header.EnabledBits;
        for(int i=0;i<results.Length;i++)
        { var query=active?QueryCore(input,heights[i],precision):null;results[i]=new(heights[i],enabled,selector,precision,query,query is null?null:Shape(query,selector),active?3:0); }
        return new(results,work);
    }
    public static TerrainFractalFilterQuery Query(TerrainFilterFractalInput input,TerrainSuppliedFractalSample sample,
        TerrainHeightArithmeticPrecision precision,TerrainRetainedFractalLimits? limits=null)
    {
        ArgumentNullException.ThrowIfNull(input);Policy(precision);Fields(input);
        TerrainSuppliedFractalSample.Cap(1,2,40,limits);return QueryRetained(input,sample,precision);
    }
    public static TerrainRetainedFractalFilterResult Evaluate(TerrainFilterFractalInput input,TerrainSuppliedFractalSample? sample,
        TerrainHeightArithmeticPrecision precision,TerrainRetainedFractalLimits? limits=null)
    {
        ArgumentNullException.ThrowIfNull(input);Policy(precision);var fields=Fields(input);
        Need(input.Header is not null,"Decoded header required.");bool active=input.Header!.IsEnabled;
        TerrainSuppliedFractalSample.Cap(1,active?3:0,40,limits);
        if(!active)return new(new(0,input.Header.EnabledBits,fields.FeatheringTypeBits,precision,null,null,0),null);
        var query=QueryRetained(input,sample,precision);
        return new(new(sample!.ProjectionBits,input.Header.EnabledBits,fields.FeatheringTypeBits,precision,query,Shape(query,fields.FeatheringTypeBits),3),sample);
    }
    public static TerrainRetainedFractalBatch<TerrainRetainedFractalFilterResult> EvaluateBatch(TerrainFilterFractalInput input,
        ReadOnlySpan<TerrainSuppliedFractalSample?> samples,TerrainHeightArithmeticPrecision precision,TerrainRetainedFractalLimits? limits=null)
    {
        ArgumentNullException.ThrowIfNull(input);Policy(precision);Fields(input);Need(input.Header is not null,"Decoded header required.");
        long work=input.Header!.IsEnabled?3L*samples.Length:0,bytes=40L*samples.Length;
        TerrainSuppliedFractalSample.Cap(samples.Length,work,bytes,limits);
        var owned=samples.ToArray();var results=new TerrainRetainedFractalFilterResult[owned.Length];
        for(int i=0;i<results.Length;i++)results[i]=Evaluate(input,owned[i],precision,limits);
        return new(results,work,bytes);
    }
    private static TerrainFractalFilterQuery QueryRetained(TerrainFilterFractalInput input,TerrainSuppliedFractalSample? sample,TerrainHeightArithmeticPrecision precision)
    {
        var fields=Fields(input);Need(fields.HasFiniteRange&&sample is not null&&Finite(fields.SampleScaleBits)&&Finite(fields.FeatheringAmountBits),"Finite active range, owned sample, scale and amount required.");
        uint scaled=Store(Number.Multiply(sample!.Value,Number.FromBits(fields.SampleScaleBits),(int)precision));
        return QueryScaled(input,sample.ProjectionBits,scaled,precision,sample);
    }
    private static TerrainFractalFilterQuery QueryCore(TerrainFilterFractalInput input,uint sampleBits,TerrainHeightArithmeticPrecision precision)
    {
        var fields=Fields(input);Need(fields.HasFiniteRange&&Finite(sampleBits)&&Finite(fields.SampleScaleBits)&&Finite(fields.FeatheringAmountBits),"Finite active range, sample, scale and amount required.");
        int p=(int)precision;uint scaled=Store(Number.Multiply(Number.FromBits(sampleBits),Number.FromBits(fields.SampleScaleBits),p));return QueryScaled(input,sampleBits,scaled,precision,null);
    }
    private static TerrainFractalFilterQuery QueryScaled(TerrainFilterFractalInput input,uint sampleBits,uint scaled,
        TerrainHeightArithmeticPrecision precision,TerrainSuppliedFractalSample? sample)
    {
        var fields=Fields(input);int p=(int)precision;Number lo=Number.FromBits(fields.LowerSampleBits),hi=Number.FromBits(fields.UpperSampleBits),h=Number.FromBits(scaled);
        uint amountBits=fields.FeatheringAmountBits;Number amount=Number.FromBits(amountBits);
        if(Number.Compare(amount,Zero)<0)amountBits=0;else if(Number.Compare(amount,One)>0)amountBits=0x3f800000;
        amount=Number.FromBits(amountBits);
        if(Number.Compare(h,lo)<=0||Number.Compare(h,hi)>=0)
            return new(input,sampleBits,scaled,amountBits,precision,TerrainFractalFilterBranch.Outside,Zero,null,sample);
        Number span=Number.Multiply(Number.Multiply(Number.Subtract(hi,lo,p),amount,p),Half,p);
        Number value;TerrainFractalFilterBranch branch;
        if(Number.Compare(Number.Add(lo,span,p),h)>0)
        { value=Number.Divide(Number.Subtract(h,lo,p),span,p);branch=TerrainFractalFilterBranch.LowerFeather; }
        else if(Number.Compare(Number.Subtract(hi,span,p),h)<0)
        { value=Number.Divide(Number.Subtract(hi,h,p),span,p);branch=TerrainFractalFilterBranch.UpperFeather; }
        else { value=One;branch=TerrainFractalFilterBranch.Interior; }
        Store(value);return new(input,sampleBits,scaled,amountBits,precision,branch,value,Store(span),sample);
    }
    private static TerrainFilterFractalFields Fields(TerrainFilterFractalInput input)
    { Need(input.HasDecodedFields&&input.Fields is not null,"Opaque fractal filter cannot be queried.");return input.Fields!; }
    private static TerrainFractalFilterLimits Limits(TerrainFractalFilterLimits? limits)
    { limits??=new();if(limits.MaximumSamples is <0 or >1048576||limits.MaximumQueryWork is <0 or >3145728)throw new ArgumentOutOfRangeException(nameof(limits));return limits; }
    private static void Cap(int samples,long work,TerrainFractalFilterLimits limits)=>Need(samples<=limits.MaximumSamples&&work<=limits.MaximumQueryWork,"Sample/work caps before snapshots and publication.");
    private static void Policy(TerrainHeightArithmeticPrecision precision)
    { if(precision is not (TerrainHeightArithmeticPrecision.Binary24 or TerrainHeightArithmeticPrecision.Binary53 or TerrainHeightArithmeticPrecision.Binary64))throw new ArgumentOutOfRangeException(nameof(precision)); }
    private static bool Finite(uint bits)=>(bits&0x7f800000)!=0x7f800000;
    private static uint Store(Number value)
    { uint bits=value.ToFloatBits();Need(Finite(bits),"Modern finite inspection/store policy.");return bits; }
    private static void Need(bool value,string reason) { if(!value)throw new InvalidDataException(reason); }
}
