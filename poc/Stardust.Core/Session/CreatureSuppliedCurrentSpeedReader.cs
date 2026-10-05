using System.Numerics;
using Number = Stardust.Core.Geometry.TerrainCircleArithmetic.Number;
namespace Stardust.Core.Session;

public enum CreatureSuppliedCurrentSpeedPolicy { SuppliedLocalOnly=1 }
public enum CreatureSuppliedCurrentSpeedProvenance { SuppliedLocalFiniteWords }
public sealed record CreatureSuppliedCurrentSpeedLimits(int MaximumCopyBytes=128,int MaximumWork=8192);
public sealed class CreatureSuppliedCurrentSpeedResult
{
    internal CreatureSuppliedCurrentSpeedResult(uint b,uint first,uint second,CreatureNominalSpeedPrecision p,
        CreatureNominalRetainedValue modifiers,CreatureNominalRetainedValue speed,uint bits,int work)
    { BaseBits=b;FirstModifierSnapshotBits=first;SecondModifierBits=second;Precision=p;
      RetainedModifierProduct=modifiers;RetainedSpeed=speed;ReturnBits=bits;ReservedWork=work; }
    public uint BaseBits { get; }
    public uint FirstModifierSnapshotBits { get; }
    public uint SecondModifierBits { get; }
    public CreatureNominalSpeedPrecision Precision { get; }
    public CreatureNominalRetainedValue RetainedModifierProduct { get; }
    public CreatureNominalRetainedValue RetainedSpeed { get; }
    public uint ReturnBits { get; }
    public int ReservedWork { get; }
    public int ReservedCopyBytes => CreatureSuppliedCurrentSpeedReader.AccountedCopyBytes;
    public int SuppliedWords => 3;
    public int Multiplications => 2;
    public int CallerF32Stores => 1;
    public CreatureSuppliedCurrentSpeedPolicy Policy => CreatureSuppliedCurrentSpeedPolicy.SuppliedLocalOnly;
    public CreatureSuppliedCurrentSpeedProvenance Provenance => CreatureSuppliedCurrentSpeedProvenance.SuppliedLocalFiniteWords;
}
/// <summary>One independent supplied local-current reader; no source selection or recursive query.</summary>
public static class CreatureSuppliedCurrentSpeedReader
{
    public const int AccountedCopyBytes=128;
    public const int HardMaximumCopyBytes=128;
    public const int HardMaximumWork=8192;
    private static void Need(bool v,string reason) { if(!v)throw new InvalidDataException(reason); }
    private static bool Finite(uint b)=>(b&0x7f800000)!=0x7f800000;
    private static (int Width,int Exponent) Inspect(uint b)
    {
        int e=(int)((b>>23)&255);uint m=(b&0x7fffff)|(e==0?0u:0x800000u);
        return (m==0?0:BitOperations.Log2(m)+1,e==0?-149:e-150);
    }
    public static (int CopyBytes,int Work) GetRequiredReservations(uint baseBits,uint firstModifierSnapshotBits,
        uint secondModifierBits,CreatureSuppliedCurrentSpeedPolicy policy,CreatureNominalSpeedPrecision precision,
        CreatureSuppliedCurrentSpeedLimits? limits=null,CancellationToken cancellationToken=default)
    {
        cancellationToken.ThrowIfCancellationRequested();limits??=new();
        if(policy!=CreatureSuppliedCurrentSpeedPolicy.SuppliedLocalOnly)throw new ArgumentOutOfRangeException(nameof(policy));
        if(precision is not (CreatureNominalSpeedPrecision.NearestEven24 or CreatureNominalSpeedPrecision.NearestEven53 or CreatureNominalSpeedPrecision.NearestEven64))throw new ArgumentOutOfRangeException(nameof(precision));
        if(limits.MaximumCopyBytes is <0 or >HardMaximumCopyBytes||limits.MaximumWork is <0 or >HardMaximumWork)throw new ArgumentOutOfRangeException(nameof(limits));
        Need(Finite(baseBits)&&Finite(firstModifierSnapshotBits)&&Finite(secondModifierBits),"All three supplied local words must be finite before arithmetic.");
        Need(AccountedCopyBytes<=limits.MaximumCopyBytes,"Reader value-copy reservation exceeds profile before arithmetic.");
        var a=Inspect(baseBits);var b=Inspect(firstModifierSnapshotBits);var c=Inspect(secondModifierBits);
        // Conservative logical bit-work covers two bounded products and the final projection.
        int work=checked(64+8*(a.Width+b.Width+c.Width+Math.Abs(a.Exponent)+Math.Abs(b.Exponent)+Math.Abs(c.Exponent)+(int)precision+8));
        Need(work<=limits.MaximumWork,"Reader work reservation exceeds profile before BigInteger arithmetic.");
        return (AccountedCopyBytes,work);
    }
    private static CreatureNominalRetainedValue Export(Number value)
    {
        BigInteger m=BigInteger.Abs(value.Mantissa);int e=value.Exponent;
        while(m.GetBitLength()>64&&m.IsEven){m>>=1;e++;}
        Need(m.GetBitLength()<=64,"Reader retained export invariant.");return new((ulong)m,e,value.Negative);
    }
    public static CreatureSuppliedCurrentSpeedResult Evaluate(uint baseBits,uint firstModifierSnapshotBits,
        uint secondModifierBits,CreatureSuppliedCurrentSpeedPolicy policy,CreatureNominalSpeedPrecision precision,
        CreatureSuppliedCurrentSpeedLimits? limits=null,CancellationToken cancellationToken=default)
    {
        var reservation=GetRequiredReservations(baseBits,firstModifierSnapshotBits,secondModifierBits,policy,precision,limits,cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();int p=(int)precision;
        var modifiers=Number.Multiply(Number.FromBits(secondModifierBits),Number.FromBits(firstModifierSnapshotBits),p);
        cancellationToken.ThrowIfCancellationRequested();
        // Retained first product is never projected/reloaded before the base multiply.
        var speed=Number.Multiply(modifiers,Number.FromBits(baseBits),p);uint bits=speed.ToFloatBits();
        Need(Finite(bits),"Finite caller return required before publication.");
        var result=new CreatureSuppliedCurrentSpeedResult(baseBits,firstModifierSnapshotBits,secondModifierBits,precision,Export(modifiers),Export(speed),bits,reservation.Work);
        cancellationToken.ThrowIfCancellationRequested();return result;
    }
}
