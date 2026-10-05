using System.Numerics;
using Number = Stardust.Core.Geometry.TerrainCircleArithmetic.Number;

namespace Stardust.Core.Session;

public enum CreatureNominalSpeedPrecision { NearestEven24=24, NearestEven53=53, NearestEven64=64 }
public enum CreatureNominalSpeedBranch { Equal, Rise, Fall }
public enum CreatureNominalRateProvenance { NotUsed, SuppliedNominal }
/// <summary>Owned synthetic nominal rate: signed Magnitude * 2^Exponent, not a physical getter.</summary>
public readonly record struct CreatureNominalRate(ulong Magnitude, int Exponent, bool IsNegative=false);
public readonly record struct CreatureNominalRetainedValue(ulong Magnitude, int Exponent, bool IsNegative)
{
    public bool NegativeZero => Magnitude==0 && IsNegative;
}
/// <summary>Logical bit-work reservation; not CLR allocation bytes or original x87 limits.</summary>
public sealed record CreatureNominalSpeedLimits(int MaximumMagnitudeBits=64, int MaximumAbsoluteExponent=512, int MaximumWork=8192);
public sealed class CreatureNominalSpeedResult
{
    internal CreatureNominalSpeedResult(uint stored,uint candidate,CreatureNominalRetainedValue retained,
        CreatureNominalRetainedValue? step,CreatureNominalSpeedBranch branch,bool clamped,
        CreatureNominalSpeedPrecision precision,int work,CreatureNominalRate? rate)
    { StoredBits=stored;CandidateBits=candidate;RetainedCandidate=retained;RetainedStep=step;Branch=branch;
      Clamped=clamped;Precision=precision;ReservedWork=work;SuppliedRate=rate; }
    public uint StoredBits { get; }
    public uint CandidateBits { get; }
    public CreatureNominalRetainedValue RetainedCandidate { get; }
    public CreatureNominalRetainedValue? RetainedStep { get; }
    public CreatureNominalSpeedBranch Branch { get; }
    public bool Clamped { get; }
    public CreatureNominalSpeedPrecision Precision { get; }
    public int ReservedWork { get; }
    public CreatureNominalRate? SuppliedRate { get; }
    public int RatesUsed => SuppliedRate.HasValue?1:0;
    public CreatureNominalRateProvenance RateProvenance => RatesUsed==0?CreatureNominalRateProvenance.NotUsed:CreatureNominalRateProvenance.SuppliedNominal;
}

/// <summary>Pure bounded nominal speed step; no vector, root motion, getter, or later zeroing.</summary>
public static class CreatureNominalSpeedTransition
{
    public const int HardMaximumMagnitudeBits=64;
    public const int HardMaximumAbsoluteExponent=512;
    public const int HardMaximumWork=8192;
    private static bool Finite(uint bits)=>(bits&0x7f800000)!=0x7f800000;
    private static void Need(bool value,string message) { if(!value)throw new InvalidDataException(message); }
    private static CreatureNominalRetainedValue Decode(uint bits)
    {
        int e=(int)((bits>>23)&255);ulong m=e==0?bits&0x7fffff:(bits&0x7fffff)|0x800000;
        return new(m,e==0?-149:e-150,(bits&0x80000000)!=0);
    }
    private static int Width(ulong m)=>m==0?0:BitOperations.Log2(m)+1;
    private static int CompareFinite(uint a,uint b)
    {
        uint am=a&0x7fffffff,bm=b&0x7fffffff;
        if(am==0&&bm==0)return 0;
        bool an=(a&0x80000000)!=0,bn=(b&0x80000000)!=0;
        if(an!=bn)return an?-1:1;
        int c=am.CompareTo(bm);return an?-c:c;
    }
    private static (CreatureNominalSpeedBranch Branch,int Work) Plan(uint current,uint target,uint seconds,
        CreatureNominalSpeedPrecision precision,CreatureNominalRate? rate,CreatureNominalSpeedLimits? limits)
    {
        limits??=new();
        if(precision is not (CreatureNominalSpeedPrecision.NearestEven24 or CreatureNominalSpeedPrecision.NearestEven53 or CreatureNominalSpeedPrecision.NearestEven64))
            throw new ArgumentOutOfRangeException(nameof(precision));
        if(limits.MaximumMagnitudeBits is <0 or >64||limits.MaximumAbsoluteExponent is <0 or >512||limits.MaximumWork is <0 or >8192)
            throw new ArgumentOutOfRangeException(nameof(limits));
        Need(Finite(current)&&Finite(target)&&Finite(seconds),"Finite binary32 current/target/seconds required before arithmetic.");
        if(rate.HasValue)
        {
            var r=rate.Value;
            Need(r.Exponent>=-limits.MaximumAbsoluteExponent&&r.Exponent<=limits.MaximumAbsoluteExponent,"Supplied rate exponent exceeds bounded profile.");
            Need(Width(r.Magnitude)<=limits.MaximumMagnitudeBits,"Supplied rate magnitude exceeds bounded profile.");
        }
        int order=CompareFinite(target,current);
        if(order==0)return (CreatureNominalSpeedBranch.Equal,0);
        Need(rate.HasValue,"Ordered transition requires one supplied nominal rate.");
        var c=Decode(current);var t=Decode(target);var s=Decode(seconds);var rr=rate!.Value;
        int productExponent=rr.Exponent+s.Exponent;
        int minimum=Math.Min(Math.Min(c.Exponent,t.Exponent),productExponent);
        int top=Math.Max(Math.Max(c.Exponent+Width(c.Magnitude),t.Exponent+Width(t.Magnitude)),productExponent+Width(rr.Magnitude)+Width(s.Magnitude)+2);
        // Conservative reservation for alignment, product/add/carry, comparison and narrowing.
        int work=checked(64+8*(top-minimum+2));
        Need(work<=limits.MaximumWork,"Nominal transition work reservation exceeds profile before BigInteger arithmetic.");
        return (order>0?CreatureNominalSpeedBranch.Rise:CreatureNominalSpeedBranch.Fall,work);
    }
    public static int GetRequiredWork(uint currentBits,uint targetBits,uint secondsBits,CreatureNominalSpeedPrecision precision,
        CreatureNominalRate? suppliedRate=null,CreatureNominalSpeedLimits? limits=null) =>
        Plan(currentBits,targetBits,secondsBits,precision,suppliedRate,limits).Work;
    private static CreatureNominalRetainedValue Export(Number value)
    {
        BigInteger magnitude=BigInteger.Abs(value.Mantissa);int exponent=value.Exponent;
        // A rounding carry may be 2^64; reduce exact trailing powers of two before U64 export.
        while(magnitude.GetBitLength()>64&&magnitude.IsEven){magnitude>>=1;exponent++;}
        Need(magnitude.GetBitLength()<=64,"Retained precision export invariant.");
        return new((ulong)magnitude,exponent,value.Negative);
    }
    public static CreatureNominalSpeedResult Evaluate(uint currentBits,uint targetBits,uint secondsBits,
        CreatureNominalSpeedPrecision precision,CreatureNominalRate? suppliedRate=null,
        CreatureNominalSpeedLimits? limits=null,CancellationToken cancellationToken=default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var plan=Plan(currentBits,targetBits,secondsBits,precision,suppliedRate,limits);
        cancellationToken.ThrowIfCancellationRequested();
        if(plan.Branch==CreatureNominalSpeedBranch.Equal)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return new(currentBits,currentBits,Decode(currentBits),null,plan.Branch,false,precision,0,null);
        }
        var rate=suppliedRate!.Value;BigInteger m=new(rate.Magnitude);
        var nominal=new Number(rate.IsNegative?-m:m,rate.Exponent,rate.Magnitude==0&&rate.IsNegative);
        if(plan.Branch==CreatureNominalSpeedBranch.Fall)nominal=nominal.Negate();
        var step=Number.Multiply(nominal,Number.FromBits(secondsBits),(int)precision);
        cancellationToken.ThrowIfCancellationRequested();
        var retained=Number.Add(step,Number.FromBits(currentBits),(int)precision);
        uint candidate=retained.ToFloatBits();
        Need(Finite(candidate),"Finite candidate binary32 store required; overflow is rejected before publication, even if a target clamp would follow.");
        int comparison=Number.Compare(retained,Number.FromBits(targetBits));
        bool clamp=plan.Branch==CreatureNominalSpeedBranch.Rise?comparison>0:comparison<0;
        var result=new CreatureNominalSpeedResult(clamp?targetBits:candidate,candidate,Export(retained),Export(step),plan.Branch,clamp,precision,plan.Work,rate);
        cancellationToken.ThrowIfCancellationRequested();
        return result;
    }
}
