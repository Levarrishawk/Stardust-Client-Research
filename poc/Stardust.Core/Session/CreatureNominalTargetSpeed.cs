using System.Numerics;
using Number = Stardust.Core.Geometry.TerrainCircleArithmetic.Number;
namespace Stardust.Core.Session;

/// <summary>Supplied direction words in recovered load order: -8, -C, -4.</summary>
public readonly record struct CreatureNominalTargetDirection(uint FirstBits,uint SecondBits,uint ThirdBits);
public readonly record struct CreatureNominalTargetFactor(ulong Magnitude,int Exponent,bool IsNegative=false);
public enum CreatureNominalTargetSelection { Run, Walk }
public enum CreatureNominalTargetProvenance { SuppliedFiniteInputs }
public sealed record CreatureNominalTargetLimits(int MaximumMagnitudeBits=64,int MaximumAbsoluteExponent=512,int MaximumWork=65536);
public sealed class CreatureNominalTargetSpeedResult
{
    internal CreatureNominalTargetSpeedResult(CreatureNominalTargetSelection selection,uint selected,byte flag,
        CreatureNominalTargetDirection direction,CreatureNominalTargetFactor factor,CreatureNominalSpeedPrecision precision,
        CreatureNominalRetainedValue sum,CreatureNominalRetainedValue magnitude,CreatureNominalRetainedValue intermediate,
        uint intermediateBits,CreatureNominalRetainedValue target,uint targetBits,int work)
    { Selection=selection;SelectedSpeedBits=selected;RunFlag=flag;Direction=direction;Factor=factor;Precision=precision;
      RetainedSquaredMagnitude=sum;RetainedMagnitude=magnitude;RetainedIntermediate=intermediate;
      IntermediateBits=intermediateBits;RetainedTarget=target;TargetBits=targetBits;ReservedWork=work; }
    public CreatureNominalTargetSelection Selection { get; }
    public uint SelectedSpeedBits { get; }
    public byte RunFlag { get; }
    public CreatureNominalTargetDirection Direction { get; }
    public CreatureNominalTargetFactor Factor { get; }
    public CreatureNominalSpeedPrecision Precision { get; }
    public CreatureNominalRetainedValue RetainedSquaredMagnitude { get; }
    public CreatureNominalRetainedValue RetainedMagnitude { get; }
    public CreatureNominalRetainedValue RetainedIntermediate { get; }
    public uint IntermediateBits { get; }
    public CreatureNominalRetainedValue RetainedTarget { get; }
    public uint TargetBits { get; }
    public int ReservedWork { get; }
    public int SuppliedGetterWords => 2;
    public int DirectionWords => 3;
    public int SelectedSpeedUses => 1;
    public int FactorValuesUsed => 1;
    public int ArithmeticOperations => 8;
    public int F32Stores => 2;
    public CreatureNominalTargetProvenance Provenance => CreatureNominalTargetProvenance.SuppliedFiniteInputs;
}

/// <summary>Pure supplied target construction; no original getter, gameplay flag or engine state.</summary>
public static class CreatureNominalTargetSpeed
{
    public const int HardMaximumMagnitudeBits=64;
    public const int HardMaximumAbsoluteExponent=512;
    public const int HardMaximumWork=65536;
    private static bool Finite(uint bits)=>(bits&0x7f800000)!=0x7f800000;
    private static void Need(bool value,string message) { if(!value)throw new InvalidDataException(message); }
    private static (int Width,int Exponent) Inspect(uint bits)
    {
        int e=(int)((bits>>23)&255);uint m=e==0?bits&0x7fffff:(bits&0x7fffff)|0x800000;
        return (m==0?0:BitOperations.Log2(m)+1,e==0?-149:e-150);
    }
    private static (CreatureNominalTargetSelection Selection,uint Selected,int Work) Plan(uint run,uint walk,byte flag,
        CreatureNominalTargetDirection direction,CreatureNominalTargetFactor factor,CreatureNominalSpeedPrecision precision,CreatureNominalTargetLimits? limits)
    {
        limits??=new();
        if(precision is not (CreatureNominalSpeedPrecision.NearestEven24 or CreatureNominalSpeedPrecision.NearestEven53 or CreatureNominalSpeedPrecision.NearestEven64))throw new ArgumentOutOfRangeException(nameof(precision));
        if(limits.MaximumMagnitudeBits is <0 or >64||limits.MaximumAbsoluteExponent is <0 or >512||limits.MaximumWork is <0 or >65536)throw new ArgumentOutOfRangeException(nameof(limits));
        Need(Finite(run)&&Finite(walk)&&Finite(direction.FirstBits)&&Finite(direction.SecondBits)&&Finite(direction.ThirdBits),"Finite supplied getter/direction F32 words required before arithmetic.");
        Need(factor.Exponent>=-limits.MaximumAbsoluteExponent&&factor.Exponent<=limits.MaximumAbsoluteExponent,"Supplied factor exponent exceeds profile.");
        int factorWidth=factor.Magnitude==0?0:BitOperations.Log2(factor.Magnitude)+1;
        Need(factorWidth<=limits.MaximumMagnitudeBits,"Supplied factor magnitude exceeds profile.");
        bool useRun=flag!=0&&(run&0x80000000)==0&&(run&0x7fffffff)!=0;
        uint selected=useRun?run:walk;
        var a=Inspect(direction.FirstBits);var b=Inspect(direction.SecondBits);var c=Inspect(direction.ThirdBits);var s=Inspect(selected);
        int low=Math.Min(2*a.Exponent,Math.Min(2*b.Exponent,2*c.Exponent));
        int high=Math.Max(2*a.Exponent+2*a.Width,Math.Max(2*b.Exponent+2*b.Width,2*c.Exponent+2*c.Width))+2;
        // Conservative logical bit-work covers align/carry, bounded sqrt, products and projections.
        int work=checked(1024+16*(high-low+Math.Abs(s.Exponent)+s.Width+2*Math.Abs(factor.Exponent)+factorWidth+(int)precision+128));
        Need(work<=limits.MaximumWork,"Target construction reservation exceeds profile before BigInteger work.");
        return (useRun?CreatureNominalTargetSelection.Run:CreatureNominalTargetSelection.Walk,selected,work);
    }
    public static int GetRequiredWork(uint runBits,uint walkBits,byte runFlag,CreatureNominalTargetDirection direction,
        CreatureNominalTargetFactor factor,CreatureNominalSpeedPrecision precision,CreatureNominalTargetLimits? limits=null) =>
        Plan(runBits,walkBits,runFlag,direction,factor,precision,limits).Work;
    private static CreatureNominalRetainedValue Export(Number value)
    {
        BigInteger m=BigInteger.Abs(value.Mantissa);int e=value.Exponent;
        while(m.GetBitLength()>64&&m.IsEven){m>>=1;e++;}
        Need(m.GetBitLength()<=64,"Target retained export invariant.");
        return new((ulong)m,e,value.Negative);
    }
    public static CreatureNominalTargetSpeedResult Evaluate(uint runBits,uint walkBits,byte runFlag,
        CreatureNominalTargetDirection direction,CreatureNominalTargetFactor factor,CreatureNominalSpeedPrecision precision,
        CreatureNominalTargetLimits? limits=null,CancellationToken cancellationToken=default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var plan=Plan(runBits,walkBits,runFlag,direction,factor,precision,limits);
        cancellationToken.ThrowIfCancellationRequested();
        int p=(int)precision;
        var first=Number.FromBits(direction.FirstBits);var second=Number.FromBits(direction.SecondBits);var third=Number.FromBits(direction.ThirdBits);
        var aa=Number.Multiply(first,first,p);var bb=Number.Multiply(second,second,p);
        var pair=Number.Add(aa,bb,p);var cc=Number.Multiply(third,third,p);
        var squared=Number.Add(pair,cc,p);
        cancellationToken.ThrowIfCancellationRequested();
        var magnitude=Number.SquareRoot(squared,p);
        var intermediate=Number.Multiply(magnitude,Number.FromBits(plan.Selected),p);
        uint snapshot=intermediate.ToFloatBits();
        Need(Finite(snapshot),"Finite +154 candidate projection required before publication.");
        cancellationToken.ThrowIfCancellationRequested();
        BigInteger fm=new(factor.Magnitude);var supplied=new Number(factor.IsNegative?-fm:fm,factor.Exponent,factor.Magnitude==0&&factor.IsNegative);
        // The snapshot is intentionally never reloaded for the factor operation.
        var target=Number.Multiply(intermediate,supplied,p);uint targetBits=target.ToFloatBits();
        Need(Finite(targetBits),"Finite nominal target projection required before publication.");
        var result=new CreatureNominalTargetSpeedResult(plan.Selection,plan.Selected,runFlag,direction,factor,precision,
            Export(squared),Export(magnitude),Export(intermediate),snapshot,Export(target),targetBits,plan.Work);
        cancellationToken.ThrowIfCancellationRequested();
        return result;
    }
}
