using Stardust.Core.Assets;
using Number = Stardust.Core.Geometry.TerrainCircleArithmetic.Number;

namespace Stardust.Core.Geometry;

public sealed record TerrainHeightFilterLimits(int MaximumSamples=65536,long MaximumQueryWork=131072);
public enum TerrainHeightFilterBranch { Outside, LowerFeather, UpperFeather, Interior }

/// <summary>Owned input plus a supplied current height; inspection projection is not the Shape input.</summary>
public sealed class TerrainHeightFilterQuery
{
    internal TerrainHeightFilterQuery(TerrainFilterHeightInput input,uint height,uint amount,
        TerrainHeightArithmeticPrecision precision,TerrainHeightFilterBranch branch,Number value,uint? span)
    { Input=input;CurrentHeightBits=height;RuntimeAmountBits=amount;Precision=precision;Branch=branch;Value=value;QueryProjectionBits=value.ToFloatBits();FeatherSpanProjectionBits=span; }
    public TerrainFilterHeightInput Input { get; }
    public uint CurrentHeightBits { get; }
    public uint RuntimeAmountBits { get; }
    public TerrainHeightArithmeticPrecision Precision { get; }
    public TerrainHeightFilterBranch Branch { get; }
    public uint QueryProjectionBits { get; }
    public uint? FeatherSpanProjectionBits { get; }
    internal Number Value { get; }
}
public readonly record struct TerrainHeightFilterResult(uint CurrentHeightBits,uint EnabledBits,uint SelectorBits,
    TerrainHeightArithmeticPrecision Precision,TerrainHeightFilterQuery? Query,uint? InfluenceBits,long QueryWork)
{ public bool SkippedDisabled=>Query is null; }
public sealed class TerrainHeightFilterBatch
{
    internal TerrainHeightFilterBatch(TerrainHeightFilterResult[] results,long work)
    { Results=Array.AsReadOnly(results);QueryWork=work; }
    public IReadOnlyList<TerrainHeightFilterResult> Results { get; }
    public long QueryWork { get; }
    public uint?[] CopyInfluenceBits()=>Results.Select(r=>r.InfluenceBits).ToArray();
}

/// <summary>Fresh FHGT finite query, with explicit nearest-even precision; no layer adapter or height producer.</summary>
public static class TerrainFilterHeightInfluence
{
    private static readonly Number Zero=Number.FromBits(0),One=Number.FromBits(0x3f800000),
        Half=Number.FromBits(0x3f000000),Three=Number.FromBits(0x40400000);
    public static TerrainHeightFilterQuery Query(TerrainFilterHeightInput input,uint currentHeightBits,
        TerrainHeightArithmeticPrecision precision,TerrainHeightFilterLimits? limits=null)
    { ArgumentNullException.ThrowIfNull(input);Policy(precision);limits=Limits(limits);Fields(input);Cap(1,1,limits);return QueryCore(input,currentHeightBits,precision); }
    public static uint Shape(TerrainHeightFilterQuery query,uint selectorBits)
    {
        ArgumentNullException.ThrowIfNull(query);int p=(int)query.Precision;Number u=query.Value;
        Number result=selectorBits switch
        {
            0=>u,1=>Number.Multiply(u,u,p),2=>Number.SquareRoot(u,p),
            3=>Number.Multiply(Number.Multiply(Number.Subtract(Three,Number.Add(u,u,p),p),u,p),u,p),_=>Zero
        };
        return Store(result);
    }
    public static TerrainHeightFilterResult Evaluate(TerrainFilterHeightInput input,uint currentHeightBits,
        TerrainHeightArithmeticPrecision precision,TerrainHeightFilterLimits? limits=null)
    {
        ArgumentNullException.ThrowIfNull(input);Policy(precision);limits=Limits(limits);var fields=Fields(input);
        Need(input.Header is not null,"Decoded header required.");Cap(1,input.Header!.IsEnabled?2:0,limits);
        uint enabled=input.Header.EnabledBits,selector=fields.FeatheringTypeBits??0;
        if(!input.Header.IsEnabled)return new(currentHeightBits,enabled,selector,precision,null,null,0);
        var query=QueryCore(input,currentHeightBits,precision);
        return new(currentHeightBits,enabled,selector,precision,query,Shape(query,selector),2);
    }
    public static TerrainHeightFilterBatch EvaluateBatch(TerrainFilterHeightInput input,ReadOnlySpan<uint> currentHeightBits,
        TerrainHeightArithmeticPrecision precision,TerrainHeightFilterLimits? limits=null)
    {
        ArgumentNullException.ThrowIfNull(input);Policy(precision);limits=Limits(limits);var fields=Fields(input);
        Need(input.Header is not null,"Decoded header required.");bool active=input.Header!.IsEnabled;
        long work=active?2L*currentHeightBits.Length:0;Cap(currentHeightBits.Length,work,limits);
        uint[] heights=currentHeightBits.ToArray();var results=new TerrainHeightFilterResult[heights.Length];
        uint selector=fields.FeatheringTypeBits??0,enabled=input.Header.EnabledBits;
        for(int i=0;i<results.Length;i++)
        { var query=active?QueryCore(input,heights[i],precision):null;results[i]=new(heights[i],enabled,selector,precision,query,query is null?null:Shape(query,selector),active?2:0); }
        return new(results,work);
    }
    private static TerrainHeightFilterQuery QueryCore(TerrainFilterHeightInput input,uint heightBits,TerrainHeightArithmeticPrecision precision)
    {
        var fields=Fields(input);Need(fields.HasFiniteRange&&Finite(heightBits)&&Finite(fields.FeatheringAmountBits??0),"Finite active range, height and amount required.");
        int p=(int)precision;Number lo=Number.FromBits(fields.LowerHeightBits),hi=Number.FromBits(fields.UpperHeightBits),h=Number.FromBits(heightBits);
        uint amountBits=fields.FeatheringAmountBits??0;Number amount=Number.FromBits(amountBits);
        if(Number.Compare(amount,Zero)<0)amountBits=0;else if(Number.Compare(amount,One)>0)amountBits=0x3f800000;
        amount=Number.FromBits(amountBits);
        if(Number.Compare(h,lo)<=0||Number.Compare(h,hi)>=0)
            return new(input,heightBits,amountBits,precision,TerrainHeightFilterBranch.Outside,Zero,null);
        Number span=Number.Multiply(Number.Multiply(Number.Subtract(hi,lo,p),amount,p),Half,p);
        Number value;TerrainHeightFilterBranch branch;
        if(Number.Compare(Number.Add(lo,span,p),h)>0)
        { value=Number.Divide(Number.Subtract(h,lo,p),span,p);branch=TerrainHeightFilterBranch.LowerFeather; }
        else if(Number.Compare(Number.Subtract(hi,span,p),h)<0)
        { value=Number.Divide(Number.Subtract(hi,h,p),span,p);branch=TerrainHeightFilterBranch.UpperFeather; }
        else { value=One;branch=TerrainHeightFilterBranch.Interior; }
        Store(value);return new(input,heightBits,amountBits,precision,branch,value,Store(span));
    }
    private static TerrainFilterHeightFields Fields(TerrainFilterHeightInput input)
    { Need(input.HasDecodedFields&&input.Fields is not null,"Opaque height filter cannot be queried.");return input.Fields!; }
    private static TerrainHeightFilterLimits Limits(TerrainHeightFilterLimits? limits)
    { limits??=new();if(limits.MaximumSamples is <0 or >1048576||limits.MaximumQueryWork is <0 or >2097152)throw new ArgumentOutOfRangeException(nameof(limits));return limits; }
    private static void Cap(int samples,long work,TerrainHeightFilterLimits limits)=>Need(samples<=limits.MaximumSamples&&work<=limits.MaximumQueryWork,"Sample/work caps before snapshots and publication.");
    private static void Policy(TerrainHeightArithmeticPrecision precision)
    { if(precision is not (TerrainHeightArithmeticPrecision.Binary24 or TerrainHeightArithmeticPrecision.Binary53 or TerrainHeightArithmeticPrecision.Binary64))throw new ArgumentOutOfRangeException(nameof(precision)); }
    private static bool Finite(uint bits)=>(bits&0x7f800000)!=0x7f800000;
    private static uint Store(Number value)
    { uint bits=value.ToFloatBits();Need(Finite(bits),"Modern finite inspection/store policy.");return bits; }
    private static void Need(bool value,string reason) { if(!value)throw new InvalidDataException(reason); }
}
