using System.Numerics;
using Number = Stardust.Core.Geometry.TerrainCircleArithmetic.Number;

namespace Stardust.Core.Geometry;

public enum TerrainSuppliedFractalProvenance { SuppliedF32, SuppliedWideMiss, CacheReloadF32 }
public sealed record TerrainRetainedFractalLimits(int MaximumSamples=65536,long MaximumWork=393216,long MaximumSnapshotBytes=3145728);
public sealed class TerrainRetainedFractalBatch<T>
{
    internal TerrainRetainedFractalBatch(T[] results,long work,long bytes)
    { Results=Array.AsReadOnly(results);Work=work;SnapshotBytes=bytes; }
    public IReadOnlyList<T> Results { get; }
    public long Work { get; }
    public long SnapshotBytes { get; }
}

/// <summary>Bounded modern caller-supplied binary value, not native x87 bytes or generated noise.</summary>
public sealed class TerrainSuppliedFractalSample
{
    public const int MaximumAbsoluteExponent=512;
    // Logical owned-value accounting, independent of CLR object overhead.
    public const int AccountedBytes=32;
    private TerrainSuppliedFractalSample(ulong magnitude,bool negative,int exponent,
        TerrainSuppliedFractalProvenance provenance,Number value)
    {
        uint bits=value.ToFloatBits();Need(Finite(bits),"Finite F32 cache projection required.");
        Magnitude=magnitude;IsNegative=negative;Exponent=exponent;Provenance=provenance;Value=value;ProjectionBits=bits;
    }
    public ulong Magnitude { get; }
    public bool IsNegative { get; }
    /// <summary>Exact signed inspection when representable; null for the full-U64 extension.</summary>
    public long? Significand=>IsNegative
        ? Magnitude<=0x8000000000000000UL?(Magnitude==0x8000000000000000UL?long.MinValue:-(long)Magnitude):null
        : Magnitude<=long.MaxValue?(long)Magnitude:null;
    public int Exponent { get; }
    public bool NegativeZero=>Magnitude==0&&IsNegative;
    public TerrainSuppliedFractalProvenance Provenance { get; }
    public uint ProjectionBits { get; }
    internal Number Value { get; }
    public static TerrainSuppliedFractalSample FromWide(long signedSignificand,int exponent,bool negativeZero=false)
    {
        // Primitive checks precede BigInteger construction, shifts and projection.
        Need(exponent>=-MaximumAbsoluteExponent&&exponent<=MaximumAbsoluteExponent,"Bounded supplied exponent required.");
        Need(signedSignificand==0||!negativeZero,"Negative-zero flag is valid only for zero significand.");
        ulong magnitude=signedSignificand<0?unchecked((ulong)(-(signedSignificand+1)))+1:(ulong)signedSignificand;
        return FromWideMagnitude(magnitude,signedSignificand<0||negativeZero,exponent);
    }
    public static TerrainSuppliedFractalSample FromWideMagnitude(ulong magnitude,bool negative,int exponent)
    {
        Need(exponent>=-MaximumAbsoluteExponent&&exponent<=MaximumAbsoluteExponent,"Bounded supplied exponent required.");
        // F32 nearest-even overflows at magnitude * 2^exponent >= (2^25-1) * 2^103.
        // Primitive comparison rejects that boundary without BigInteger work.
        int shift=103-exponent;bool overflow=false;
        if(magnitude!=0)
        {
            if(shift>=0&&shift<=39)overflow=magnitude>=(33554431UL<<shift);
            else if(shift<0)
            {
                int right=-shift;
                overflow=right>=25||magnitude>=((33554431UL+((1UL<<right)-1))>>right);
            }
        }
        Need(!overflow,"Finite F32 cache projection required before arithmetic.");
        BigInteger m=new(magnitude);
        return new(magnitude,negative,exponent,TerrainSuppliedFractalProvenance.SuppliedWideMiss,
            new Number(negative?-m:m,exponent,magnitude==0&&negative));
    }
    public static TerrainSuppliedFractalSample FromF32(uint bits)=>FromBits(bits,TerrainSuppliedFractalProvenance.SuppliedF32);
    public static TerrainSuppliedFractalSample FromCacheF32(uint bits)=>FromBits(bits,TerrainSuppliedFractalProvenance.CacheReloadF32);
    public TerrainSuppliedFractalSample ToCacheReload()=>FromCacheF32(ProjectionBits);
    private static TerrainSuppliedFractalSample FromBits(uint bits,TerrainSuppliedFractalProvenance provenance)
    {
        Need(Finite(bits),"Finite supplied F32 required before arithmetic.");
        int encoded=(int)((bits>>23)&255),exponent=encoded==0?-149:encoded-150;
        long m=encoded==0?bits&0x7fffff:(bits&0x7fffff)|0x800000;
        bool negative=(bits&0x80000000)!=0;
        return new((ulong)m,negative,exponent,provenance,Number.FromBits(bits));
    }
    internal static void Cap(int count,long work,long bytes,TerrainRetainedFractalLimits? limits)
    {
        limits??=new();
        if(limits.MaximumSamples is <0 or >1048576||limits.MaximumWork is <0 or >6291456||limits.MaximumSnapshotBytes is <0 or >50331648)
            throw new ArgumentOutOfRangeException(nameof(limits));
        Need(count<=limits.MaximumSamples&&work<=limits.MaximumWork&&bytes<=limits.MaximumSnapshotBytes,"Aggregate retained sample/work/snapshot budget before snapshots.");
    }
    private static bool Finite(uint bits)=>(bits&0x7f800000)!=0x7f800000;
    private static void Need(bool condition,string reason) { if(!condition)throw new InvalidDataException(reason); }
}
