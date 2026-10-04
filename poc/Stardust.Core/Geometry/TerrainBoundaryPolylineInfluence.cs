using Stardust.Core.Assets;
using Number = Stardust.Core.Geometry.TerrainCircleArithmetic.Number;

namespace Stardust.Core.Geometry;

public sealed record TerrainPolylineInfluenceLimits(int MaximumPoints=8192,int MaximumSamples=65536,long MaximumPointVisits=1048576);
public readonly record struct TerrainPolylineBounds(uint FirstMinimumBits,uint SecondMinimumBits,uint FirstMaximumBits,uint SecondMaximumBits);
public enum TerrainPolylineQueryBranch { BoundsOutside, WidthOutside, Inner, Feather }

/// <summary>Owned fresh ordered points and width-expanded cached bounds; open consecutive segments; no sorting or closure.</summary>
public sealed class TerrainPolylineCache
{
    private readonly TerrainBoundaryPolylinePoint[] points;
    internal TerrainPolylineCache(TerrainBoundaryPolylinePoint[] points,TerrainBoundaryPolylineMetadata metadata,
        TerrainPolylineBounds bounds,TerrainHeightArithmeticPrecision precision)
    { this.points=points;Points=Array.AsReadOnly(points);Metadata=metadata;Bounds=bounds;Precision=precision; }
    public IReadOnlyList<TerrainBoundaryPolylinePoint> Points { get; }
    public int PointCount=>points.Length;
    public TerrainBoundaryPolylineMetadata Metadata { get; }
    public uint RuntimeFeatherAmountBits=>Metadata.FeatheringAmountBits;
    public uint WidthBits=>Metadata.WidthBits;
    public TerrainPolylineBounds Bounds { get; }
    public TerrainHeightArithmeticPrecision Precision { get; }
    public long PointVisitCharge=>PointCount;
    public TerrainBoundaryPolylinePoint[] CopyPoints()=>(TerrainBoundaryPolylinePoint[])points.Clone();
    internal ReadOnlySpan<TerrainBoundaryPolylinePoint> PointSpan=>points;
}

/// <summary>F32 projections inspect the retained arithmetic value; Shape does not reload them.</summary>
public sealed class TerrainPolylineQuery
{
    internal TerrainPolylineQuery(TerrainPolylineCache cache,uint first,uint second,TerrainHeightArithmeticPrecision precision,
        TerrainPolylineQueryBranch branch,Number value,uint? square=null,uint? minimum=null,int? projections=null)
    {
        Cache=cache;FirstCoordinateBits=first;SecondCoordinateBits=second;Precision=precision;Branch=branch;
        Value=value;QueryProjectionBits=value.ToFloatBits();SquaredWidthBits=square;
        MinimumDistanceSquaredBits=minimum;SegmentProjectionsConsidered=projections;
    }
    public TerrainPolylineCache Cache { get; }
    public uint FirstCoordinateBits { get; }
    public uint SecondCoordinateBits { get; }
    public TerrainHeightArithmeticPrecision Precision { get; }
    public TerrainPolylineQueryBranch Branch { get; }
    public uint QueryProjectionBits { get; }
    public uint? SquaredWidthBits { get; }
    public uint? MinimumDistanceSquaredBits { get; }
    public int? SegmentProjectionsConsidered { get; }
    public long PointVisitCharge=>3L*Cache.PointCount;
    internal Number Value { get; }
}

public readonly record struct TerrainPolylineInfluenceResult(uint FirstCoordinateBits,uint SecondCoordinateBits,
    uint EnabledBits,uint SelectorBits,TerrainHeightArithmeticPrecision CachePrecision,TerrainHeightArithmeticPrecision Precision,
    TerrainPolylineQuery? Query,uint? InfluenceBits,long PointVisitCharge)
{
    public bool SkippedDisabled=>Query is null;
}

public sealed class TerrainPolylineInfluenceBatch
{
    internal TerrainPolylineInfluenceBatch(TerrainPolylineCache? cache,TerrainPolylineInfluenceResult[] results,long charge)
    { Cache=cache;Results=Array.AsReadOnly(results);PointVisitCharge=charge; }
    public TerrainPolylineCache? Cache { get; }
    public IReadOnlyList<TerrainPolylineInfluenceResult> Results { get; }
    public long PointVisitCharge { get; }
    public uint?[] CopyInfluenceBits()=>Results.Select(r=>r.InfluenceBits).ToArray();
}

/// <summary>Conditional finite fresh BPLN graph, with explicit nearest-even policies and modern work caps.</summary>
public static class TerrainBoundaryPolylineInfluence
{
    private static readonly Number Zero=Number.FromBits(0),One=Number.FromBits(0x3f800000),
        Three=Number.FromBits(0x40400000);

    public static TerrainPolylineCache Cache(TerrainBoundaryPolylineInput input,TerrainHeightArithmeticPrecision precision,
        TerrainPolylineInfluenceLimits? limits=null)
    {
        ArgumentNullException.ThrowIfNull(input);ValidatePrecision(precision);limits=Limits(limits);
        Metadata(input);PointCap(input.Points.Count,limits);Charge(input.Points.Count,limits);
        return CacheCore(input,precision);
    }
    public static TerrainPolylineQuery Query(TerrainBoundaryPolylineInput input,uint firstCoordinateBits,uint secondCoordinateBits,
        TerrainHeightArithmeticPrecision cachePrecision,TerrainHeightArithmeticPrecision precision,TerrainPolylineInfluenceLimits? limits=null)
    {
        ArgumentNullException.ThrowIfNull(input);ValidatePrecision(cachePrecision);ValidatePrecision(precision);limits=Limits(limits);
        Metadata(input);PointCap(input.Points.Count,limits);SampleCap(1,limits);Charge(4L*input.Points.Count,limits);
        return QueryCore(CacheCore(input,cachePrecision),firstCoordinateBits,secondCoordinateBits,precision);
    }
    public static TerrainPolylineQuery Query(TerrainPolylineCache cache,uint firstCoordinateBits,uint secondCoordinateBits,
        TerrainHeightArithmeticPrecision precision,TerrainPolylineInfluenceLimits? limits=null)
    {
        ArgumentNullException.ThrowIfNull(cache);ValidatePrecision(precision);limits=Limits(limits);
        PointCap(cache.PointCount,limits);SampleCap(1,limits);Charge(3L*cache.PointCount,limits);
        return QueryCore(cache,firstCoordinateBits,secondCoordinateBits,precision);
    }
    public static uint Shape(TerrainPolylineQuery query,uint selectorBits)
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
    public static TerrainPolylineInfluenceResult Evaluate(TerrainBoundaryPolylineInput input,uint firstCoordinateBits,uint secondCoordinateBits,
        TerrainHeightArithmeticPrecision cachePrecision,TerrainHeightArithmeticPrecision precision,TerrainPolylineInfluenceLimits? limits=null)
    {
        ArgumentNullException.ThrowIfNull(input);ValidatePrecision(cachePrecision);ValidatePrecision(precision);limits=Limits(limits);
        var metadata=Metadata(input);PointCap(input.Points.Count,limits);SampleCap(1,limits);Need(input.Header is not null,"Decoded header required.");
        uint enabled=input.Header!.EnabledBits,selector=metadata.FeatheringTypeBits;
        if(!input.Header.IsEnabled)return new(firstCoordinateBits,secondCoordinateBits,enabled,selector,cachePrecision,precision,null,null,0);
        Charge(4L*input.Points.Count,limits);
        var query=QueryCore(CacheCore(input,cachePrecision),firstCoordinateBits,secondCoordinateBits,precision);
        return new(firstCoordinateBits,secondCoordinateBits,enabled,selector,cachePrecision,precision,query,Shape(query,selector),4L*input.Points.Count);
    }
    public static TerrainPolylineInfluenceBatch EvaluateBatch(TerrainBoundaryPolylineInput input,ReadOnlySpan<uint> firstCoordinateBits,
        ReadOnlySpan<uint> secondCoordinateBits,TerrainHeightArithmeticPrecision cachePrecision,TerrainHeightArithmeticPrecision precision,
        TerrainPolylineInfluenceLimits? limits=null)
    {
        ArgumentNullException.ThrowIfNull(input);ValidatePrecision(cachePrecision);ValidatePrecision(precision);limits=Limits(limits);
        var metadata=Metadata(input);PointCap(input.Points.Count,limits);SampleCap(firstCoordinateBits.Length,limits);
        Need(firstCoordinateBits.Length==secondCoordinateBits.Length,"Equal coordinate lengths required.");Need(input.Header is not null,"Decoded header required.");
        bool active=input.Header!.IsEnabled&&firstCoordinateBits.Length>0;
        long charge=active?input.Points.Count+3L*input.Points.Count*firstCoordinateBits.Length:0;Charge(charge,limits);
        uint[] first=firstCoordinateBits.ToArray(),second=secondCoordinateBits.ToArray();
        var cache=active?CacheCore(input,cachePrecision):null;
        var results=new TerrainPolylineInfluenceResult[first.Length];uint selector=metadata.FeatheringTypeBits,enabled=input.Header.EnabledBits;
        for(int i=0;i<results.Length;i++)
        {
            var query=cache is null?null:QueryCore(cache,first[i],second[i],precision);
            results[i]=new(first[i],second[i],enabled,selector,cachePrecision,precision,query,
                query is null?null:Shape(query,selector),query is null?0:3L*cache!.PointCount);
        }
        return new(cache,results,charge);
    }
    private static TerrainPolylineCache CacheCore(TerrainBoundaryPolylineInput input,TerrainHeightArithmeticPrecision precision)
    {
        var metadata=Metadata(input);Need(Finite(metadata.FeatheringAmountBits)&&Finite(metadata.WidthBits),"Finite raw amount and width required; no clamp.");
        var points=input.CopyPoints();uint a=0x7f7fffff,b=a,c=0xff7fffff,d=c;
        foreach(var point in points)
        {
            Need(point.IsFinite,"Finite ordered points required.");
            Number x=Number.FromBits(point.FirstBits),y=Number.FromBits(point.SecondBits);
            if(Number.Compare(x,Number.FromBits(a))<0)a=point.FirstBits;
            if(Number.Compare(y,Number.FromBits(b))<0)b=point.SecondBits;
            if(Number.Compare(x,Number.FromBits(c))>0)c=point.FirstBits;
            if(Number.Compare(y,Number.FromBits(d))>0)d=point.SecondBits;
        }
        int p=(int)precision;Number width=Number.FromBits(metadata.WidthBits);
        a=Store(Number.Subtract(Number.FromBits(a),width,p));c=Store(Number.Add(Number.FromBits(c),width,p));
        b=Store(Number.Subtract(Number.FromBits(b),width,p));d=Store(Number.Add(Number.FromBits(d),width,p));
        return new(points,metadata,new(a,b,c,d),precision);
    }
    private static TerrainPolylineQuery QueryCore(TerrainPolylineCache cache,uint first,uint second,TerrainHeightArithmeticPrecision precision)
    {
        Need(Finite(first)&&Finite(second),"Finite active coordinates required.");int p=(int)precision;
        Number x=Number.FromBits(first),y=Number.FromBits(second);var points=cache.PointSpan;var bounds=cache.Bounds;
        static bool Between(Number value,uint ab,uint bb)
        { Number a=Number.FromBits(ab),b=Number.FromBits(bb);if(Number.Compare(a,b)>0)(a,b)=(b,a);return Number.Compare(value,a)>=0&&Number.Compare(value,b)<=0; }
        if(!Between(x,bounds.FirstMinimumBits,bounds.FirstMaximumBits)||!Between(y,bounds.SecondMinimumBits,bounds.SecondMaximumBits))
            return new(cache,first,second,precision,TerrainPolylineQueryBranch.BoundsOutside,Zero);
        Number width=Number.FromBits(cache.WidthBits),minimum=Number.Multiply(width,width,p);uint square=Store(minimum);
        static Number Distance(Number dx,Number dy,int p)=>Number.Add(Number.Multiply(dy,dy,p),Number.Multiply(dx,dx,p),p);
        static Number Select(Number current,Number candidate)
        { uint bits=Store(candidate);return Number.Compare(candidate,current)<0?Number.FromBits(bits):current; }
        foreach(var point in points)
            minimum=Select(minimum,Distance(Number.Subtract(x,Number.FromBits(point.FirstBits),p),Number.Subtract(y,Number.FromBits(point.SecondBits),p),p));
        int projections=0;
        for(int i=1;i<points.Length;i++)
        {
            Number ax=Number.FromBits(points[i-1].FirstBits),ay=Number.FromBits(points[i-1].SecondBits),
                bx=Number.FromBits(points[i].FirstBits),by=Number.FromBits(points[i].SecondBits);
            uint dxBits=Store(Number.Subtract(bx,ax,p));Number dx=Number.FromBits(dxBits),dyRetained=Number.Subtract(by,ay,p);
            uint dyBits=Store(dyRetained);Number dy=Number.FromBits(dyBits);
            Number dot=Number.Add(Number.Multiply(Number.Subtract(y,ay,p),dy,p),Number.Multiply(Number.Subtract(x,ax,p),dx,p),p);
            Number length=Number.Add(Number.Multiply(dyRetained,dyRetained,p),Number.Multiply(dx,dx,p),p);
            Number parameter=Number.FromBits(Store(Number.Divide(dot,length,p)));
            if(Number.Compare(parameter,Zero)<0||Number.Compare(parameter,One)>0)continue;
            projections++;
            Number projectedX=Number.Add(Number.Multiply(dx,parameter,p),ax,p),projectedY=Number.Add(Number.Multiply(dy,parameter,p),ay,p);
            minimum=Select(minimum,Distance(Number.Subtract(x,projectedX,p),Number.Subtract(y,projectedY,p),p));
        }
        if(Number.Compare(minimum,Number.FromBits(square))>=0)
            return new(cache,first,second,precision,TerrainPolylineQueryBranch.WidthOutside,Zero,square,Store(minimum),projections);
        Number amount=Number.FromBits(cache.RuntimeFeatherAmountBits);
        Number threshold=Number.Multiply(Number.Subtract(One,amount,p),width,p);uint thresholdBits=Store(threshold);
        Number thresholdSquare=Number.Multiply(threshold,Number.FromBits(thresholdBits),p);
        bool inner=Number.Compare(minimum,thresholdSquare)<0;
        Number value=inner?One:Number.Subtract(One,Number.Divide(
            Number.Subtract(Number.SquareRoot(minimum,p),Number.FromBits(thresholdBits),p),
            Number.Subtract(width,Number.FromBits(thresholdBits),p),p),p);
        Store(value);
        return new(cache,first,second,precision,inner?TerrainPolylineQueryBranch.Inner:TerrainPolylineQueryBranch.Feather,
            value,square,Store(minimum),projections);
    }
    private static TerrainBoundaryPolylineMetadata Metadata(TerrainBoundaryPolylineInput input)
    { Need(input.HasDecodedFields&&input.Metadata is not null,"Opaque polyline input cannot be evaluated.");return input.Metadata!; }
    private static TerrainPolylineInfluenceLimits Limits(TerrainPolylineInfluenceLimits? limits)
    {
        limits??=new();if(limits.MaximumPoints is <0 or >65536||limits.MaximumSamples is <0 or >1048576||limits.MaximumPointVisits is <0 or >67108864)
            throw new ArgumentOutOfRangeException(nameof(limits));return limits;
    }
    private static void PointCap(int count,TerrainPolylineInfluenceLimits limits)=>Need(count<=limits.MaximumPoints,"Point cap before owned cache copy.");
    private static void SampleCap(int count,TerrainPolylineInfluenceLimits limits)=>Need(count<=limits.MaximumSamples,"Sample cap before snapshots/results.");
    private static void Charge(long visits,TerrainPolylineInfluenceLimits limits)=>Need(visits<=limits.MaximumPointVisits,"Conservative point-visit budget before work/publication.");
    private static uint Store(Number value)
    { uint bits=value.ToFloatBits();Need(Finite(bits),"Modern finite F32 spill/projection policy.");return bits; }
    private static bool Finite(uint bits)=>(bits&0x7f800000)!=0x7f800000;
    private static void ValidatePrecision(TerrainHeightArithmeticPrecision precision)
    { if(precision is not (TerrainHeightArithmeticPrecision.Binary24 or TerrainHeightArithmeticPrecision.Binary53 or TerrainHeightArithmeticPrecision.Binary64))throw new ArgumentOutOfRangeException(nameof(precision)); }
    private static void Need(bool value,string reason) { if(!value)throw new InvalidDataException(reason); }
}
