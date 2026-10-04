using Stardust.Core.Assets;
using Number = Stardust.Core.Geometry.TerrainCircleArithmetic.Number;

namespace Stardust.Core.Geometry;

public enum TerrainCircleQueryBranch { Outside, Inner, Feather }

/// <summary>Immutable query retaining the unspilled arithmetic value for caller shaping.</summary>
public sealed class TerrainCircleQuery
{
    internal TerrainCircleQuery(uint first, uint second, TerrainBoundaryCircleFields fields,
        uint amount, uint radiusSquared, uint distanceSquared, Number value,
        TerrainCircleQueryBranch branch, TerrainHeightArithmeticPrecision cachePrecision, TerrainHeightArithmeticPrecision precision)
    {
        FirstCoordinateBits=first; SecondCoordinateBits=second; Fields=fields;
        RuntimeFeatherAmountBits=amount; RadiusSquaredBits=radiusSquared; DistanceSquaredBits=distanceSquared;
        Value=value; QueryProjectionBits=value.ToFloatBits(); Branch=branch; CachePrecision=cachePrecision; Precision=precision;
    }
    public uint FirstCoordinateBits { get; }
    public uint SecondCoordinateBits { get; }
    public TerrainBoundaryCircleFields Fields { get; }
    public uint RuntimeFeatherAmountBits { get; }
    public uint RadiusSquaredBits { get; }
    public uint DistanceSquaredBits { get; }
    /// <summary>F32 inspection projection only; Shape uses the retained unspilled value.</summary>
    public uint QueryProjectionBits { get; }
    public TerrainCircleQueryBranch Branch { get; }
    public TerrainHeightArithmeticPrecision CachePrecision { get; }
    public TerrainHeightArithmeticPrecision Precision { get; }
    internal Number Value { get; }
}

public readonly record struct TerrainCircleInfluenceResult(uint FirstCoordinateBits, uint SecondCoordinateBits,
    uint EnabledBits, uint SelectorBits, TerrainHeightArithmeticPrecision CachePrecision, TerrainHeightArithmeticPrecision Precision,
    TerrainCircleQuery? Query, uint? InfluenceBits)
{
    public bool SkippedDisabled => Query is null;
}

public sealed class TerrainCircleInfluenceBatch
{
    internal TerrainCircleInfluenceBatch(TerrainCircleInfluenceResult[] results) => Results=Array.AsReadOnly(results);
    public IReadOnlyList<TerrainCircleInfluenceResult> Results { get; }
    public uint?[] CopyInfluenceBits() => Results.Select(r=>r.InfluenceBits).ToArray();
}

/// <summary>One supplied-coordinate BCIR query and separate layer selector shaping; no layer composition.</summary>
public static class TerrainBoundaryCircleInfluence
{
    private static readonly Number Zero=Number.FromBits(0), One=Number.FromBits(0x3f800000), Three=Number.FromBits(0x40400000);
    public static TerrainCircleQuery Query(TerrainBoundaryCircleInput input, uint firstCoordinateBits,
        uint secondCoordinateBits, TerrainHeightArithmeticPrecision cachePrecision, TerrainHeightArithmeticPrecision precision)
    {
        ArgumentNullException.ThrowIfNull(input); ValidatePrecision(cachePrecision); ValidatePrecision(precision);
        TerrainBoundaryCircleFields f=Fields(input);
        Need(f.HasFiniteGeometry && Finite(firstCoordinateBits) && Finite(secondCoordinateBits), "Active query requires finite geometry and coordinates.");
        uint amount=f.FeatheringAmountBits ?? 0;
        Need(Finite(amount), "Nonfinite serialized feather amount is unsupported.");
        Number feather=Number.FromBits(amount);
        if (Number.Compare(feather, Zero)<0) { amount=0; feather=Zero; }
        else if (Number.Compare(feather, One)>0) { amount=0x3f800000; feather=One; }
        int p=(int)precision;
        Number radius=Number.FromBits(f.RadiusBits);
        uint radiusBits=Number.Multiply(radius, radius, (int)cachePrecision).ToFloatBits();
        Need(Finite(radiusBits), "Modern finite radius-square cache policy.");
        Number dx=Number.Subtract(Number.FromBits(f.CenterFirstBits), Number.FromBits(firstCoordinateBits), p);
        Number dy=Number.Subtract(Number.FromBits(f.CenterSecondBits), Number.FromBits(secondCoordinateBits), p);
        uint distanceBits=Number.Add(Number.Multiply(dy,dy,p), Number.Multiply(dx,dx,p), p).ToFloatBits();
        Need(Finite(distanceBits), "Modern finite distance-square spill policy.");
        Number distance=Number.FromBits(distanceBits), cache=Number.FromBits(radiusBits), value;
        TerrainCircleQueryBranch branch;
        if (Number.Compare(distance,cache)>0) { value=Zero; branch=TerrainCircleQueryBranch.Outside; }
        else
        {
            Number inner=Number.Multiply(Number.Subtract(One,feather,p),radius,p);
            Number innerSquared=Number.Multiply(inner,inner,p);
            if (Number.Compare(distance,innerSquared)<=0) { value=One; branch=TerrainCircleQueryBranch.Inner; }
            else
            {
                Number numerator=Number.Subtract(distance,innerSquared,p);
                Number denominator=Number.Subtract(cache,innerSquared,p);
                value=Number.Subtract(One,Number.Divide(numerator,denominator,p),p);
                branch=TerrainCircleQueryBranch.Feather;
            }
        }
        return new(firstCoordinateBits,secondCoordinateBits,f,amount,radiusBits,distanceBits,value,branch,cachePrecision,precision);
    }
    public static uint Shape(TerrainCircleQuery query, uint selectorBits)
    {
        ArgumentNullException.ThrowIfNull(query);
        Number u=query.Value, result; int p=(int)query.Precision;
        switch (selectorBits)
        {
            case 0: result=u; break;
            case 1: result=Number.Multiply(u,u,p); break;
            case 2: result=Number.SquareRoot(u,p); break;
            case 3:
                Number twice=Number.Add(u,u,p);
                Number difference=Number.Subtract(Three,twice,p);
                result=Number.Multiply(Number.Multiply(difference,u,p),u,p); break;
            default: result=Zero; break;
        }
        uint bits=result.ToFloatBits(); Need(Finite(bits), "Modern finite shaped output policy."); return bits;
    }
    public static TerrainCircleInfluenceResult Evaluate(TerrainBoundaryCircleInput input, uint firstCoordinateBits,
        uint secondCoordinateBits, TerrainHeightArithmeticPrecision cachePrecision, TerrainHeightArithmeticPrecision precision)
    {
        ArgumentNullException.ThrowIfNull(input); ValidatePrecision(cachePrecision); ValidatePrecision(precision);
        TerrainBoundaryCircleFields f=Fields(input);
        Need(input.Header is not null, "Decoded local header required.");
        uint selector=f.FeatheringTypeBits ?? 0, enabled=input.Header!.EnabledBits;
        if (!input.Header.IsEnabled) return new(firstCoordinateBits,secondCoordinateBits,enabled,selector,cachePrecision,precision,null,null);
        var query=Query(input,firstCoordinateBits,secondCoordinateBits,cachePrecision,precision);
        return new(firstCoordinateBits,secondCoordinateBits,enabled,selector,cachePrecision,precision,query,Shape(query,selector));
    }
    public static TerrainCircleInfluenceBatch EvaluateBatch(TerrainBoundaryCircleInput input,
        ReadOnlySpan<uint> firstCoordinateBits, ReadOnlySpan<uint> secondCoordinateBits,
        TerrainHeightArithmeticPrecision cachePrecision, TerrainHeightArithmeticPrecision precision, int maximumSamples=65536)
    {
        ArgumentNullException.ThrowIfNull(input); ValidatePrecision(cachePrecision); ValidatePrecision(precision);
        if (maximumSamples is <0 or >1048576) throw new ArgumentOutOfRangeException(nameof(maximumSamples));
        Need(firstCoordinateBits.Length==secondCoordinateBits.Length && firstCoordinateBits.Length<=maximumSamples, "Equal-length bounded coordinate inputs.");
        Fields(input); Need(input.Header is not null, "Decoded local header required.");
        uint[] first=firstCoordinateBits.ToArray(), second=secondCoordinateBits.ToArray();
        var results=new TerrainCircleInfluenceResult[first.Length];
        for (int i=0;i<results.Length;i++) results[i]=Evaluate(input,first[i],second[i],cachePrecision,precision);
        return new(results);
    }
    private static TerrainBoundaryCircleFields Fields(TerrainBoundaryCircleInput input)
    {
        Need(input.HasDecodedFields && input.Fields is not null, "Opaque circle inputs cannot be evaluated.");
        return input.Fields!;
    }
    private static void ValidatePrecision(TerrainHeightArithmeticPrecision precision)
    {
        if (precision is not (TerrainHeightArithmeticPrecision.Binary24 or TerrainHeightArithmeticPrecision.Binary53 or TerrainHeightArithmeticPrecision.Binary64))
            throw new ArgumentOutOfRangeException(nameof(precision));
    }
    private static bool Finite(uint bits) => (bits & 0x7f800000)!=0x7f800000;
    private static void Need(bool value, string reason) { if (!value) throw new InvalidDataException(reason); }
}
