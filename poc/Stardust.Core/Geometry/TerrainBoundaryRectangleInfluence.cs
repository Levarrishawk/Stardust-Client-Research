using Stardust.Core.Assets;
using Number = Stardust.Core.Geometry.TerrainCircleArithmetic.Number;

namespace Stardust.Core.Geometry;

public enum TerrainRectangleQueryBranch { Outside, ZeroFeather, Inner, Feather }

public readonly record struct TerrainRectangleBounds(uint FirstABits, uint SecondABits, uint FirstBBits, uint SecondBBits);
public readonly record struct TerrainRectangleEdgeDistances(uint FirstABits, uint FirstBBits, uint SecondABits, uint SecondBBits);

/// <summary>Fresh decoded input only: constructor-default transform is disabled.</summary>
public sealed class TerrainRectangleCache
{
    internal TerrainRectangleCache(TerrainBoundaryRectangleFields fields, uint amount,
        TerrainRectangleBounds outer, TerrainRectangleBounds inner, uint firstExtent, uint secondExtent,
        uint minimumExtent, TerrainHeightArithmeticPrecision precision)
    {
        Fields=fields; RuntimeFeatherAmountBits=amount; Outer=outer; Inner=inner;
        FirstExtentBits=firstExtent; SecondExtentBits=secondExtent; MinimumExtentBits=minimumExtent; Precision=precision;
    }
    public TerrainBoundaryRectangleFields Fields { get; }
    public uint RuntimeFeatherAmountBits { get; }
    public TerrainRectangleBounds Outer { get; }
    public TerrainRectangleBounds Inner { get; }
    public uint FirstExtentBits { get; }
    public uint SecondExtentBits { get; }
    public uint MinimumExtentBits { get; }
    public TerrainHeightArithmeticPrecision Precision { get; }
}

/// <summary>Projection is inspection only; shaping retains the unspilled value.</summary>
public sealed class TerrainRectangleQuery
{
    internal TerrainRectangleQuery(TerrainRectangleCache cache, uint first, uint second,
        TerrainRectangleQueryBranch branch, Number value, TerrainHeightArithmeticPrecision precision,
        TerrainRectangleEdgeDistances? edges=null, uint? firstExtent=null, uint? secondExtent=null, uint? minimumExtent=null)
    {
        Cache=cache; FirstCoordinateBits=first; SecondCoordinateBits=second; Branch=branch;
        Value=value; QueryProjectionBits=value.ToFloatBits(); Precision=precision;
        EdgeDistances=edges; FirstExtentBits=firstExtent; SecondExtentBits=secondExtent; MinimumExtentBits=minimumExtent;
    }
    public TerrainRectangleCache Cache { get; }
    public uint FirstCoordinateBits { get; }
    public uint SecondCoordinateBits { get; }
    public TerrainRectangleQueryBranch Branch { get; }
    public uint QueryProjectionBits { get; }
    public TerrainHeightArithmeticPrecision Precision { get; }
    public TerrainRectangleEdgeDistances? EdgeDistances { get; }
    public uint? FirstExtentBits { get; }
    public uint? SecondExtentBits { get; }
    public uint? MinimumExtentBits { get; }
    internal Number Value { get; }
}

public readonly record struct TerrainRectangleInfluenceResult(uint FirstCoordinateBits, uint SecondCoordinateBits,
    uint EnabledBits, uint SelectorBits, TerrainHeightArithmeticPrecision CachePrecision,
    TerrainHeightArithmeticPrecision Precision, TerrainRectangleQuery? Query, uint? InfluenceBits)
{
    public bool SkippedDisabled=>Query is null;
}

public sealed class TerrainRectangleInfluenceBatch
{
    internal TerrainRectangleInfluenceBatch(TerrainRectangleInfluenceResult[] results)=>Results=Array.AsReadOnly(results);
    public IReadOnlyList<TerrainRectangleInfluenceResult> Results { get; }
    public uint?[] CopyInfluenceBits()=>Results.Select(r=>r.InfluenceBits).ToArray();
}

/// <summary>Conditional nearest-even 24/53/64 evaluation; actual original phase policies remain unknown.</summary>
public static class TerrainBoundaryRectangleInfluence
{
    private static readonly Number Zero=Number.FromBits(0), One=Number.FromBits(0x3f800000),
        Half=Number.FromBits(0x3f000000), Three=Number.FromBits(0x40400000);

    public static TerrainRectangleCache Cache(TerrainBoundaryRectangleInput input, TerrainHeightArithmeticPrecision precision)
    {
        ArgumentNullException.ThrowIfNull(input); ValidatePrecision(precision);
        var f=Fields(input); Need(f.HasFiniteGeometry,"Finite fresh rectangle geometry required.");
        uint amount=f.FeatheringAmountBits??0; Need(Finite(amount),"Nonfinite serialized amount unsupported.");
        if(Number.Compare(Number.FromBits(amount),Zero)<0)amount=0;
        else if(Number.Compare(Number.FromBits(amount),One)>0)amount=0x3f800000;
        uint a=f.CornerFirstABits,b=f.CornerSecondABits,c=f.CornerFirstBBits,d=f.CornerSecondBBits;
        if(Number.Compare(Number.FromBits(a),Number.FromBits(c))>0)(a,c)=(c,a);
        if(Number.Compare(Number.FromBits(b),Number.FromBits(d))>0)(b,d)=(d,b);
        int p=(int)precision;
        var extents=Extents(a,b,c,d,p);
        Number inset=Number.Multiply(Number.Multiply(Number.FromBits(amount),Number.FromBits(extents.Minimum),p),Half,p);
        uint ia=Store(Number.Add(inset,Number.FromBits(a),p)),ib=Store(Number.Add(inset,Number.FromBits(b),p)),
            ic=Store(Number.Subtract(Number.FromBits(c),inset,p)),id=Store(Number.Subtract(Number.FromBits(d),inset,p));
        return new(f,amount,new(a,b,c,d),new(ia,ib,ic,id),extents.First,extents.Second,extents.Minimum,precision);
    }

    public static TerrainRectangleQuery Query(TerrainBoundaryRectangleInput input,uint firstCoordinateBits,
        uint secondCoordinateBits,TerrainHeightArithmeticPrecision cachePrecision,TerrainHeightArithmeticPrecision precision)
    {
        ArgumentNullException.ThrowIfNull(input); ValidatePrecision(cachePrecision); ValidatePrecision(precision);
        return Query(Cache(input,cachePrecision),firstCoordinateBits,secondCoordinateBits,precision);
    }

    public static TerrainRectangleQuery Query(TerrainRectangleCache cache,uint firstCoordinateBits,
        uint secondCoordinateBits,TerrainHeightArithmeticPrecision precision)
    {
        ArgumentNullException.ThrowIfNull(cache); ValidatePrecision(precision);
        Need(Finite(firstCoordinateBits)&&Finite(secondCoordinateBits),"Finite active query coordinates required.");
        var x=Number.FromBits(firstCoordinateBits);var y=Number.FromBits(secondCoordinateBits);int p=(int)precision;
        if(!Contains(cache.Outer,x,y))return new(cache,firstCoordinateBits,secondCoordinateBits,TerrainRectangleQueryBranch.Outside,Zero,precision);
        if(Number.Compare(Number.FromBits(cache.RuntimeFeatherAmountBits),Zero)==0)
            return new(cache,firstCoordinateBits,secondCoordinateBits,TerrainRectangleQueryBranch.ZeroFeather,One,precision);
        if(Contains(cache.Inner,x,y))return new(cache,firstCoordinateBits,secondCoordinateBits,TerrainRectangleQueryBranch.Inner,One,precision);
        var o=cache.Outer;
        uint da=Store(Number.Subtract(x,Number.FromBits(o.FirstABits),p)),dc=Store(Number.Subtract(Number.FromBits(o.FirstBBits),x,p)),
            db=Store(Number.Subtract(y,Number.FromBits(o.SecondABits),p)),dd=Store(Number.Subtract(Number.FromBits(o.SecondBBits),y,p));
        var extents=Extents(o.FirstABits,o.SecondABits,o.FirstBBits,o.SecondBBits,p);
        Number inset=Number.Multiply(Number.Multiply(Number.FromBits(cache.RuntimeFeatherAmountBits),Number.FromBits(extents.Minimum),p),Half,p);
        Number edge=inset;
        foreach(uint bits in new[]{da,dc,db,dd})if(Number.Compare(Number.FromBits(bits),edge)<0)edge=Number.FromBits(bits);
        Number value=Number.Divide(edge,inset,p); Need(Finite(value.ToFloatBits()),"Finite query projection policy.");
        return new(cache,firstCoordinateBits,secondCoordinateBits,TerrainRectangleQueryBranch.Feather,value,precision,
            new(da,dc,db,dd),extents.First,extents.Second,extents.Minimum);
    }

    public static uint Shape(TerrainRectangleQuery query,uint selectorBits)
    {
        ArgumentNullException.ThrowIfNull(query);Number u=query.Value,result;int p=(int)query.Precision;
        switch(selectorBits)
        {
            case 0:result=u;break;
            case 1:result=Number.Multiply(u,u,p);break;
            case 2:result=Number.SquareRoot(u,p);break;
            case 3:result=Number.Multiply(Number.Multiply(Number.Subtract(Three,Number.Add(u,u,p),p),u,p),u,p);break;
            default:result=Zero;break;
        }
        return Store(result);
    }

    public static TerrainRectangleInfluenceResult Evaluate(TerrainBoundaryRectangleInput input,uint firstCoordinateBits,
        uint secondCoordinateBits,TerrainHeightArithmeticPrecision cachePrecision,TerrainHeightArithmeticPrecision precision)
    {
        ArgumentNullException.ThrowIfNull(input);ValidatePrecision(cachePrecision);ValidatePrecision(precision);
        var f=Fields(input);Need(input.Header is not null,"Decoded local header required.");
        uint enabled=input.Header!.EnabledBits,selector=f.FeatheringTypeBits??0;
        if(!input.Header.IsEnabled)return new(firstCoordinateBits,secondCoordinateBits,enabled,selector,cachePrecision,precision,null,null);
        var query=Query(input,firstCoordinateBits,secondCoordinateBits,cachePrecision,precision);
        return new(firstCoordinateBits,secondCoordinateBits,enabled,selector,cachePrecision,precision,query,Shape(query,selector));
    }

    public static TerrainRectangleInfluenceBatch EvaluateBatch(TerrainBoundaryRectangleInput input,
        ReadOnlySpan<uint> firstCoordinateBits,ReadOnlySpan<uint> secondCoordinateBits,
        TerrainHeightArithmeticPrecision cachePrecision,TerrainHeightArithmeticPrecision precision,int maximumSamples=65536)
    {
        ArgumentNullException.ThrowIfNull(input);ValidatePrecision(cachePrecision);ValidatePrecision(precision);
        if(maximumSamples is <0 or >1048576)throw new ArgumentOutOfRangeException(nameof(maximumSamples));
        Need(firstCoordinateBits.Length==secondCoordinateBits.Length&&firstCoordinateBits.Length<=maximumSamples,"Equal-length bounded coordinates.");
        Fields(input);Need(input.Header is not null,"Decoded local header required.");
        uint[] first=firstCoordinateBits.ToArray(),second=secondCoordinateBits.ToArray();
        var results=new TerrainRectangleInfluenceResult[first.Length];
        for(int i=0;i<results.Length;i++)results[i]=Evaluate(input,first[i],second[i],cachePrecision,precision);
        return new(results);
    }

    private static (uint First,uint Second,uint Minimum) Extents(uint a,uint b,uint c,uint d,int p)
    {
        uint first=Store(Number.Subtract(Number.FromBits(c),Number.FromBits(a),p));
        Number unspilledSecond=Number.Subtract(Number.FromBits(d),Number.FromBits(b),p);
        uint second=Store(unspilledSecond);
        uint minimum=Number.Compare(unspilledSecond,Number.FromBits(first))<0?second:first;
        return (first,second,minimum);
    }
    private static bool Contains(TerrainRectangleBounds bounds,Number x,Number y)
    {
        static bool Between(Number v,uint ab,uint bb)
        {
            Number a=Number.FromBits(ab),b=Number.FromBits(bb);
            if(Number.Compare(a,b)>0)(a,b)=(b,a);
            return Number.Compare(v,a)>=0&&Number.Compare(v,b)<=0;
        }
        return Between(x,bounds.FirstABits,bounds.FirstBBits)&&Between(y,bounds.SecondABits,bounds.SecondBBits);
    }
    private static TerrainBoundaryRectangleFields Fields(TerrainBoundaryRectangleInput input)
    { Need(input.HasDecodedFields&&input.Fields is not null,"Opaque rectangle input cannot be evaluated.");return input.Fields!; }
    private static uint Store(Number value)
    { uint bits=value.ToFloatBits();Need(Finite(bits),"Modern finite F32 spill policy.");return bits; }
    private static bool Finite(uint bits)=>(bits&0x7f800000)!=0x7f800000;
    private static void ValidatePrecision(TerrainHeightArithmeticPrecision precision)
    { if(precision is not (TerrainHeightArithmeticPrecision.Binary24 or TerrainHeightArithmeticPrecision.Binary53 or TerrainHeightArithmeticPrecision.Binary64))throw new ArgumentOutOfRangeException(nameof(precision)); }
    private static void Need(bool value,string reason) { if(!value)throw new InvalidDataException(reason); }
}
