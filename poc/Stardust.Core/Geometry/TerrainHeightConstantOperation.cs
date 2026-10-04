using System.Numerics;
using Stardust.Core.Assets;

namespace Stardust.Core.Geometry;

/// <summary>Explicit nearest-even software significand precision; original thread selection is unproven.</summary>
public enum TerrainHeightArithmeticPrecision { Binary24=24,Binary53=53,Binary64=64 }
public enum TerrainHeightConstantOutcome { Applied,SkippedDisabled,SkippedNonPositiveInfluence,InvalidOperationZero }
public readonly record struct TerrainHeightConstantResult(uint InputHeightBits,uint InfluenceBits,
    uint ConstantHeightBits,uint OperationBits,uint EnabledBits,uint ResultHeightBits,
    TerrainHeightConstantOutcome Outcome,TerrainHeightArithmeticPrecision Precision);

public sealed class TerrainHeightConstantBatch
{
    private readonly TerrainHeightConstantResult[] results;
    internal TerrainHeightConstantBatch(TerrainHeightConstantResult[] results)
    { this.results=results;Results=Array.AsReadOnly(results); }
    public IReadOnlyList<TerrainHeightConstantResult> Results { get; }
    public uint[] CopyHeightBits()=>results.Select(r=>r.ResultHeightBits).ToArray();
}

/// <summary>AHCN supplied-influence kernel. No ancestor, filter, boundary or grid generation policy.</summary>
public static class TerrainHeightConstantOperation
{
    public static TerrainHeightConstantResult Evaluate(TerrainHeightConstantInput rule,uint inputHeightBits,
        uint influenceBits,TerrainHeightArithmeticPrecision precision)
    {
        ArgumentNullException.ThrowIfNull(rule);ValidatePrecision(precision);
        TerrainHeightConstantResult Result(uint bits,TerrainHeightConstantOutcome outcome)=>new(inputHeightBits,influenceBits,
            rule.HeightBits,rule.OperationBits,rule.Header.EnabledBits,bits,outcome,precision);
        if(!rule.Header.IsEnabled)return Result(inputHeightBits,TerrainHeightConstantOutcome.SkippedDisabled);
        // Modern explicit policy: nonfinite influence is unsupported even where legacy masked NaN comparison skips.
        Need(Finite(influenceBits),"Nonfinite supplied influence is unsupported.");
        if((influenceBits&0x80000000)!=0||(influenceBits&0x7fffffff)==0)
            return Result(inputHeightBits,TerrainHeightConstantOutcome.SkippedNonPositiveInfluence);
        if(rule.OperationBits==4)return Result(0,TerrainHeightConstantOutcome.InvalidOperationZero);
        Need(Finite(inputHeightBits)&&Finite(rule.HeightBits),"Active arithmetic requires finite height operands.");
        int p=(int)precision;var b=Binary.FromBits(inputHeightBits);var w=Binary.FromBits(influenceBits);var c=Binary.FromBits(rule.HeightBits);
        Binary Mul(Binary a,Binary d)=>Binary.Multiply(a,d).Round(p);
        Binary Add(Binary a,Binary d)=>Binary.Add(a,d).Round(p);
        Binary result;
        switch(rule.OperationBits)
        {
            case 1:result=Add(Mul(w,c),b);break;
            case 2:result=Add(b,Mul(w,c).Negate());break;
            case 3:result=Add(b,Mul(Add(Mul(b,c),b.Negate()),w));break;
            default:result=Add(Mul(Add(Binary.FromBits(0x3f800000),w.Negate()),b),Mul(w,c));break;
        }
        uint output=result.ToFloatBits();Need(Finite(output),"Modern finite output eligibility; no result published on overflow.");
        return Result(output,TerrainHeightConstantOutcome.Applied);
    }
    public static TerrainHeightConstantBatch EvaluateBatch(TerrainHeightConstantInput rule,
        ReadOnlySpan<uint> inputHeightBits,ReadOnlySpan<uint> influenceBits,TerrainHeightArithmeticPrecision precision,int maximumSamples=65536)
    {
        ArgumentNullException.ThrowIfNull(rule);ValidatePrecision(precision);
        if(maximumSamples is <0 or >1048576)throw new ArgumentOutOfRangeException(nameof(maximumSamples));
        Need(inputHeightBits.Length==influenceBits.Length&&inputHeightBits.Length<=maximumSamples,"Equal-length bounded sample inputs.");
        uint[] input=inputHeightBits.ToArray(),influence=influenceBits.ToArray();var output=new TerrainHeightConstantResult[input.Length];
        for(int i=0;i<output.Length;i++)output[i]=Evaluate(rule,input[i],influence[i],precision);
        return new(output);
    }
    private static void ValidatePrecision(TerrainHeightArithmeticPrecision precision)
    { if(precision is not (TerrainHeightArithmeticPrecision.Binary24 or TerrainHeightArithmeticPrecision.Binary53 or TerrainHeightArithmeticPrecision.Binary64))throw new ArgumentOutOfRangeException(nameof(precision)); }
    private static bool Finite(uint bits)=>(bits&0x7f800000)!=0x7f800000;
    private static void Need(bool value,string reason) { if(!value)throw new InvalidDataException(reason); }

    // Finite dyadic arithmetic. Float inputs and this fixed instruction graph need fewer than1024 significant bits.
    // Wide x87 exponent behavior is sufficient here: no finite intermediate can approach its exponent boundaries.
    private readonly record struct Binary(BigInteger Mantissa,int Exponent,bool NegativeZero)
    {
        private bool Negative=>Mantissa.Sign<0||(Mantissa.IsZero&&NegativeZero);
        public static Binary FromBits(uint bits)
        {
            int encoded=(int)((bits>>23)&255);uint fraction=bits&0x7fffff;bool negative=(bits&0x80000000)!=0;
            BigInteger m=encoded==0?fraction:fraction|0x800000;int e=encoded==0?-149:encoded-150;
            return new(negative?-m:m,e,m.IsZero&&negative);
        }
        public Binary Negate()=>new(-Mantissa,Exponent,Mantissa.IsZero?!NegativeZero:false);
        public static Binary Multiply(Binary a,Binary b)=>new(a.Mantissa*b.Mantissa,a.Exponent+b.Exponent,
            (a.Mantissa.IsZero||b.Mantissa.IsZero)&&(a.Negative^b.Negative));
        public static Binary Add(Binary a,Binary b)
        {
            int e=Math.Min(a.Exponent,b.Exponent);BigInteger m=(a.Mantissa<<(a.Exponent-e))+(b.Mantissa<<(b.Exponent-e));
            return new(m,e,m.IsZero&&a.Mantissa.IsZero&&b.Mantissa.IsZero&&a.Negative&&b.Negative);
        }
        private static BigInteger ShiftNearestEven(BigInteger magnitude,int shift)
        {
            if(shift<=0)return magnitude<<(-shift);
            BigInteger q=magnitude>>shift,remainder=magnitude-(q<<shift),half=BigInteger.One<<(shift-1);
            if(remainder>half||(remainder==half&&!q.IsEven))q++;
            return q;
        }
        public Binary Round(int precision)
        {
            if(Mantissa.IsZero)return this;
            BigInteger magnitude=BigInteger.Abs(Mantissa);int shift=checked((int)magnitude.GetBitLength())-precision;
            if(shift<=0)return this;
            BigInteger rounded=ShiftNearestEven(magnitude,shift);return new(Mantissa.Sign<0?-rounded:rounded,Exponent+shift,false);
        }
        public uint ToFloatBits()
        {
            uint sign=Negative?0x80000000u:0;if(Mantissa.IsZero)return sign;
            BigInteger magnitude=BigInteger.Abs(Mantissa);int length=checked((int)magnitude.GetBitLength());int floor=Exponent+length-1;
            int quantum=Math.Max(-149,floor-23);BigInteger rounded=ShiftNearestEven(magnitude,quantum-Exponent);
            if(rounded.IsZero)return sign;
            int roundedLength=checked((int)rounded.GetBitLength()),roundedFloor=quantum+roundedLength-1;
            if(roundedFloor>127)return sign|0x7f800000;
            if(roundedFloor< -126)return sign|(uint)rounded;
            BigInteger normalized=roundedLength>24?rounded>>(roundedLength-24):rounded<<(24-roundedLength);
            return sign|((uint)(roundedFloor+127)<<23)|((uint)normalized&0x7fffff);
        }
    }
}
