using Stardust.Core.Assets;
using Number = Stardust.Core.Geometry.TerrainCircleArithmetic.Number;

namespace Stardust.Core.Geometry;

public enum TerrainHeightFractalOutcome { Applied,SkippedDisabled,SkippedNonPositiveInfluence,InvalidOperationPreserved }
public sealed record TerrainHeightFractalLimits(int MaximumSamples=65536,long MaximumWork=393216,int MaximumSnapshotBytes=786432);
public readonly record struct TerrainHeightFractalResult(uint InputHeightBits,uint InfluenceBits,uint SuppliedFractalBits,
    uint FamilyKeyBits,uint ScaleBits,uint OperationBits,uint ResultHeightBits,TerrainHeightFractalOutcome Outcome,
    TerrainHeightArithmeticPrecision Precision);
public readonly record struct TerrainRetainedHeightFractalResult(TerrainHeightFractalResult Consumer,TerrainSuppliedFractalSample? Sample);
public sealed class TerrainHeightFractalBatch
{
    private readonly TerrainHeightFractalResult[] results;
    internal TerrainHeightFractalBatch(TerrainHeightFractalResult[] results,long work)
    { this.results=results;Results=Array.AsReadOnly(results);Work=work; }
    public IReadOnlyList<TerrainHeightFractalResult> Results { get; }
    public long Work { get; }
    public uint[] CopyHeightBits()=>results.Select(r=>r.ResultHeightBits).ToArray();
}

/// <summary>AHFR supplied F32 sampler-result consumer, narrower than original retained sampler return.
/// No family lookup, caching, noise, grid/driver mutation or original active-control selection.</summary>
public static class TerrainSuppliedHeightFractal
{
    public static TerrainHeightFractalResult Evaluate(TerrainHeightFractalInput input,uint heightBits,uint influenceBits,
        uint suppliedFractalBits,TerrainHeightArithmeticPrecision precision)
    {
        ArgumentNullException.ThrowIfNull(input);ValidatePrecision(precision);
        Need(input.Fields is not null&&input.Header is not null,"Typed AHFR/0003 required.");
        var fields=input.Fields!;
        TerrainHeightFractalResult Result(uint bits,TerrainHeightFractalOutcome outcome)=>new(heightBits,influenceBits,
            suppliedFractalBits,fields.FamilyKeyBits,fields.ScaleBits,fields.OperationBits,bits,outcome,precision);
        if(!input.Header!.IsEnabled)return Result(heightBits,TerrainHeightFractalOutcome.SkippedDisabled);
        Need(Finite(influenceBits),"Nonfinite influence unsupported.");
        if((influenceBits&0x80000000)!=0||(influenceBits&0x7fffffff)==0)
            return Result(heightBits,TerrainHeightFractalOutcome.SkippedNonPositiveInfluence);
        Need(Finite(heightBits)&&Finite(suppliedFractalBits)&&Finite(fields.ScaleBits),"Active consumer requires finite operands.");
        int p=(int)precision;
        var h=Number.FromBits(heightBits);var w=Number.FromBits(influenceBits);
        // The sampler result and scaled target remain retained: no intervening F32 projection.
        var t=Number.Multiply(Number.FromBits(suppliedFractalBits),Number.FromBits(fields.ScaleBits),p);
        if(fields.OperationBits==4)return Result(heightBits,TerrainHeightFractalOutcome.InvalidOperationPreserved);
        var value=fields.OperationBits switch
        {
            1=>Number.Add(h,Number.Multiply(t,w,p),p),
            2=>Number.Subtract(h,Number.Multiply(t,w,p),p),
            3=>Number.Add(h,Number.Multiply(Number.Subtract(Number.Multiply(t,h,p),h,p),w,p),p),
            _=>Number.Add(h,Number.Multiply(Number.Subtract(t,h,p),w,p),p)
        };
        uint output=value.ToFloatBits();Need(Finite(output),"Finite final F32 store required; no overflow publication.");
        return Result(output,TerrainHeightFractalOutcome.Applied);
    }
    public static TerrainHeightFractalBatch EvaluateBatch(TerrainHeightFractalInput input,ReadOnlySpan<uint> heights,
        ReadOnlySpan<uint> influences,ReadOnlySpan<uint> suppliedFractals,TerrainHeightArithmeticPrecision precision,
        TerrainHeightFractalLimits? limits=null)
    {
        ArgumentNullException.ThrowIfNull(input);ValidatePrecision(precision);limits??=new();Validate(limits);
        Need(input.Fields is not null&&input.Header is not null,"Typed AHFR/0003 required.");
        // All aggregate policies precede snapshots and candidate/result arrays.
        Need(heights.Length==influences.Length&&heights.Length==suppliedFractals.Length,"Equal sample lengths required.");
        long work=6L*heights.Length,bytes=12L*heights.Length;
        Need(heights.Length<=limits.MaximumSamples&&work<=limits.MaximumWork&&bytes<=limits.MaximumSnapshotBytes,"Aggregate sample/work/snapshot budget.");
        uint[] h=heights.ToArray(),w=influences.ToArray(),f=suppliedFractals.ToArray();
        var results=new TerrainHeightFractalResult[h.Length];
        for(int i=0;i<results.Length;i++)results[i]=Evaluate(input,h[i],w[i],f[i],precision);
        return new(results,work);
    }
    public static TerrainRetainedHeightFractalResult Evaluate(TerrainHeightFractalInput input,uint heightBits,uint influenceBits,
        TerrainSuppliedFractalSample? sample,TerrainHeightArithmeticPrecision precision)
    {
        ArgumentNullException.ThrowIfNull(input);ValidatePrecision(precision);
        Need(input.Fields is not null&&input.Header is not null,"Typed AHFR/0003 required.");
        var fields=input.Fields!;
        // Gate before inspecting sample, including its projection/provenance.
        TerrainRetainedHeightFractalResult Skipped(TerrainHeightFractalOutcome outcome)=>new(
            new(heightBits,influenceBits,0,fields.FamilyKeyBits,fields.ScaleBits,fields.OperationBits,heightBits,outcome,precision),null);
        if(!input.Header!.IsEnabled)return Skipped(TerrainHeightFractalOutcome.SkippedDisabled);
        Need(Finite(influenceBits),"Nonfinite influence unsupported.");
        if((influenceBits&0x80000000)!=0||(influenceBits&0x7fffffff)==0)return Skipped(TerrainHeightFractalOutcome.SkippedNonPositiveInfluence);
        Need(Finite(heightBits)&&Finite(fields.ScaleBits)&&sample is not null,"Active consumer requires finite operands and owned sample.");
        int p=(int)precision;var h=Number.FromBits(heightBits);var w=Number.FromBits(influenceBits);
        var t=Number.Multiply(sample!.Value,Number.FromBits(fields.ScaleBits),p);
        TerrainRetainedHeightFractalResult Result(uint bits,TerrainHeightFractalOutcome outcome)=>new(
            new(heightBits,influenceBits,sample.ProjectionBits,fields.FamilyKeyBits,fields.ScaleBits,fields.OperationBits,bits,outcome,precision),sample);
        if(fields.OperationBits==4)return Result(heightBits,TerrainHeightFractalOutcome.InvalidOperationPreserved);
        var value=fields.OperationBits switch
        {
            1=>Number.Add(h,Number.Multiply(t,w,p),p),
            2=>Number.Subtract(h,Number.Multiply(t,w,p),p),
            3=>Number.Add(h,Number.Multiply(Number.Subtract(Number.Multiply(t,h,p),h,p),w,p),p),
            _=>Number.Add(h,Number.Multiply(Number.Subtract(t,h,p),w,p),p)
        };
        uint output=value.ToFloatBits();Need(Finite(output),"Finite final F32 store required; no overflow publication.");
        return Result(output,TerrainHeightFractalOutcome.Applied);
    }
    public static TerrainRetainedFractalBatch<TerrainRetainedHeightFractalResult> EvaluateBatch(TerrainHeightFractalInput input,
        ReadOnlySpan<uint> heights,ReadOnlySpan<uint> influences,ReadOnlySpan<TerrainSuppliedFractalSample?> samples,
        TerrainHeightArithmeticPrecision precision,TerrainRetainedFractalLimits? limits=null)
    {
        ArgumentNullException.ThrowIfNull(input);ValidatePrecision(precision);
        Need(input.Fields is not null&&input.Header is not null,"Typed AHFR/0003 required.");
        Need(heights.Length==influences.Length&&heights.Length==samples.Length,"Equal sample lengths required.");
        long work=6L*heights.Length,bytes=48L*heights.Length;
        TerrainSuppliedFractalSample.Cap(heights.Length,work,bytes,limits);
        var h=heights.ToArray();var w=influences.ToArray();var owned=samples.ToArray();
        var results=new TerrainRetainedHeightFractalResult[h.Length];
        for(int i=0;i<results.Length;i++)results[i]=Evaluate(input,h[i],w[i],owned[i],precision);
        return new(results,work,bytes);
    }
    private static void ValidatePrecision(TerrainHeightArithmeticPrecision precision)
    { if(precision is not (TerrainHeightArithmeticPrecision.Binary24 or TerrainHeightArithmeticPrecision.Binary53 or TerrainHeightArithmeticPrecision.Binary64))throw new ArgumentOutOfRangeException(nameof(precision)); }
    private static void Validate(TerrainHeightFractalLimits limits)
    { if(limits.MaximumSamples is <0 or >1048576||limits.MaximumWork is <0 or >6291456||limits.MaximumSnapshotBytes is <0 or >12582912)throw new ArgumentOutOfRangeException(nameof(limits)); }
    private static bool Finite(uint bits)=>(bits&0x7f800000)!=0x7f800000;
    private static void Need(bool condition,string reason) { if(!condition)throw new InvalidDataException(reason); }
}
