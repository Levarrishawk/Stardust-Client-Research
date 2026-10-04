using Stardust.Core.Assets;
using Number = Stardust.Core.Geometry.TerrainCircleArithmetic.Number;

namespace Stardust.Core.Geometry;

public sealed record TerrainPolygonInfluenceLimits(int MaximumPoints=8192,int MaximumSamples=65536,long MaximumPointVisits=1048576);
public readonly record struct TerrainPolygonBounds(uint FirstMinimumBits,uint SecondMinimumBits,uint FirstMaximumBits,uint SecondMaximumBits);
public enum TerrainPolygonQueryBranch { BoundsOutside, ParityOutside, ZeroFeather, Feather, FullBand }

/// <summary>Owned fresh ordered points and comparison-only cached bounds; no winding repair.</summary>
public sealed class TerrainPolygonCache
{
    private readonly TerrainBoundaryPolygonPoint[] points;
    internal TerrainPolygonCache(TerrainBoundaryPolygonPoint[] points,TerrainBoundaryPolygonMetadata metadata,
        TerrainPolygonBounds bounds,TerrainHeightArithmeticPrecision precision)
    { this.points=points;Points=Array.AsReadOnly(points);Metadata=metadata;Bounds=bounds;Precision=precision; }
    public IReadOnlyList<TerrainBoundaryPolygonPoint> Points { get; }
    public int PointCount=>points.Length;
    public TerrainBoundaryPolygonMetadata Metadata { get; }
    public uint RuntimeFeatherAmountBits=>Metadata.FeatheringAmountBits??0;
    public TerrainPolygonBounds Bounds { get; }
    public TerrainHeightArithmeticPrecision Precision { get; }
    public long PointVisitCharge=>PointCount;
    public TerrainBoundaryPolygonPoint[] CopyPoints()=>(TerrainBoundaryPolygonPoint[])points.Clone();
    internal ReadOnlySpan<TerrainBoundaryPolygonPoint> PointSpan=>points;
}

/// <summary>F32 projections inspect the retained arithmetic value; Shape does not reload them.</summary>
public sealed class TerrainPolygonQuery
{
    internal TerrainPolygonQuery(TerrainPolygonCache cache,uint first,uint second,TerrainHeightArithmeticPrecision precision,
        TerrainPolygonQueryBranch branch,Number value,int? crossings=null,uint? square=null,uint? minimum=null,int? projections=null)
    {
        Cache=cache;FirstCoordinateBits=first;SecondCoordinateBits=second;Precision=precision;Branch=branch;
        Value=value;QueryProjectionBits=value.ToFloatBits();Crossings=crossings;SquaredAmountBits=square;
        MinimumDistanceSquaredBits=minimum;SegmentProjectionsConsidered=projections;
    }
    public TerrainPolygonCache Cache { get; }
    public uint FirstCoordinateBits { get; }
    public uint SecondCoordinateBits { get; }
    public TerrainHeightArithmeticPrecision Precision { get; }
    public TerrainPolygonQueryBranch Branch { get; }
    public uint QueryProjectionBits { get; }
    public int? Crossings { get; }
    public uint? SquaredAmountBits { get; }
    public uint? MinimumDistanceSquaredBits { get; }
    public int? SegmentProjectionsConsidered { get; }
    public long PointVisitCharge=>3L*Cache.PointCount;
    internal Number Value { get; }
}

public readonly record struct TerrainPolygonInfluenceResult(uint FirstCoordinateBits,uint SecondCoordinateBits,
    uint EnabledBits,uint SelectorBits,TerrainHeightArithmeticPrecision CachePrecision,TerrainHeightArithmeticPrecision Precision,
    TerrainPolygonQuery? Query,uint? InfluenceBits,long PointVisitCharge)
{
    public bool SkippedDisabled=>Query is null;
}

public sealed class TerrainPolygonInfluenceBatch
{
    internal TerrainPolygonInfluenceBatch(TerrainPolygonCache? cache,TerrainPolygonInfluenceResult[] results,long charge)
    { Cache=cache;Results=Array.AsReadOnly(results);PointVisitCharge=charge; }
    public TerrainPolygonCache? Cache { get; }
    public IReadOnlyList<TerrainPolygonInfluenceResult> Results { get; }
    public long PointVisitCharge { get; }
    public uint?[] CopyInfluenceBits()=>Results.Select(r=>r.InfluenceBits).ToArray();
}

/// <summary>Conditional finite fresh BPOL graph, with explicit nearest-even policies and modern work caps.</summary>
public static class TerrainBoundaryPolygonInfluence
{
    private static readonly Number Zero=Number.FromBits(0),One=Number.FromBits(0x3f800000),
        Three=Number.FromBits(0x40400000),Tolerance=Number.FromBits(0x38d1b717);

    public static TerrainPolygonCache Cache(TerrainBoundaryPolygonInput input,TerrainHeightArithmeticPrecision precision,
        TerrainPolygonInfluenceLimits? limits=null)
    {
        ArgumentNullException.ThrowIfNull(input);ValidatePrecision(precision);limits=Limits(limits);
        Metadata(input);PointCap(input.Points.Count,limits);Charge(input.Points.Count,limits);
        return CacheCore(input,precision);
    }
    public static TerrainPolygonQuery Query(TerrainBoundaryPolygonInput input,uint firstCoordinateBits,uint secondCoordinateBits,
        TerrainHeightArithmeticPrecision cachePrecision,TerrainHeightArithmeticPrecision precision,TerrainPolygonInfluenceLimits? limits=null)
    {
        ArgumentNullException.ThrowIfNull(input);ValidatePrecision(cachePrecision);ValidatePrecision(precision);limits=Limits(limits);
        Metadata(input);PointCap(input.Points.Count,limits);SampleCap(1,limits);Charge(4L*input.Points.Count,limits);
        return QueryCore(CacheCore(input,cachePrecision),firstCoordinateBits,secondCoordinateBits,precision);
    }
    public static TerrainPolygonQuery Query(TerrainPolygonCache cache,uint firstCoordinateBits,uint secondCoordinateBits,
        TerrainHeightArithmeticPrecision precision,TerrainPolygonInfluenceLimits? limits=null)
    {
        ArgumentNullException.ThrowIfNull(cache);ValidatePrecision(precision);limits=Limits(limits);
        PointCap(cache.PointCount,limits);SampleCap(1,limits);Charge(3L*cache.PointCount,limits);
        return QueryCore(cache,firstCoordinateBits,secondCoordinateBits,precision);
    }
    public static uint Shape(TerrainPolygonQuery query,uint selectorBits)
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
    public static TerrainPolygonInfluenceResult Evaluate(TerrainBoundaryPolygonInput input,uint firstCoordinateBits,uint secondCoordinateBits,
        TerrainHeightArithmeticPrecision cachePrecision,TerrainHeightArithmeticPrecision precision,TerrainPolygonInfluenceLimits? limits=null)
    {
        ArgumentNullException.ThrowIfNull(input);ValidatePrecision(cachePrecision);ValidatePrecision(precision);limits=Limits(limits);
        var metadata=Metadata(input);PointCap(input.Points.Count,limits);SampleCap(1,limits);Need(input.Header is not null,"Decoded header required.");
        uint enabled=input.Header!.EnabledBits,selector=metadata.FeatheringTypeBits??0;
        if(!input.Header.IsEnabled)return new(firstCoordinateBits,secondCoordinateBits,enabled,selector,cachePrecision,precision,null,null,0);
        Charge(4L*input.Points.Count,limits);
        var query=QueryCore(CacheCore(input,cachePrecision),firstCoordinateBits,secondCoordinateBits,precision);
        return new(firstCoordinateBits,secondCoordinateBits,enabled,selector,cachePrecision,precision,query,Shape(query,selector),4L*input.Points.Count);
    }
    public static TerrainPolygonInfluenceBatch EvaluateBatch(TerrainBoundaryPolygonInput input,ReadOnlySpan<uint> firstCoordinateBits,
        ReadOnlySpan<uint> secondCoordinateBits,TerrainHeightArithmeticPrecision cachePrecision,TerrainHeightArithmeticPrecision precision,
        TerrainPolygonInfluenceLimits? limits=null)
    {
        ArgumentNullException.ThrowIfNull(input);ValidatePrecision(cachePrecision);ValidatePrecision(precision);limits=Limits(limits);
        var metadata=Metadata(input);PointCap(input.Points.Count,limits);SampleCap(firstCoordinateBits.Length,limits);
        Need(firstCoordinateBits.Length==secondCoordinateBits.Length,"Equal coordinate lengths required.");Need(input.Header is not null,"Decoded header required.");
        bool active=input.Header!.IsEnabled&&firstCoordinateBits.Length>0;
        long charge=active?input.Points.Count+3L*input.Points.Count*firstCoordinateBits.Length:0;Charge(charge,limits);
        uint[] first=firstCoordinateBits.ToArray(),second=secondCoordinateBits.ToArray();
        var cache=active?CacheCore(input,cachePrecision):null;
        var results=new TerrainPolygonInfluenceResult[first.Length];uint selector=metadata.FeatheringTypeBits??0,enabled=input.Header.EnabledBits;
        for(int i=0;i<results.Length;i++)
        {
            var query=cache is null?null:QueryCore(cache,first[i],second[i],precision);
            results[i]=new(first[i],second[i],enabled,selector,cachePrecision,precision,query,
                query is null?null:Shape(query,selector),query is null?0:3L*cache!.PointCount);
        }
        return new(cache,results,charge);
    }
    private static TerrainPolygonCache CacheCore(TerrainBoundaryPolygonInput input,TerrainHeightArithmeticPrecision precision)
    {
        var metadata=Metadata(input);Need(Finite(metadata.FeatheringAmountBits??0),"Finite raw amount required; no clamp.");
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
        return new(points,metadata,new(a,b,c,d),precision);
    }
    private static TerrainPolygonQuery QueryCore(TerrainPolygonCache cache,uint first,uint second,TerrainHeightArithmeticPrecision precision)
    {
        Need(Finite(first)&&Finite(second),"Finite active coordinates required.");int p=(int)precision;
        Number x=Number.FromBits(first),y=Number.FromBits(second);var points=cache.PointSpan;var bounds=cache.Bounds;
        static bool Between(Number value,uint ab,uint bb)
        { Number a=Number.FromBits(ab),b=Number.FromBits(bb);if(Number.Compare(a,b)>0)(a,b)=(b,a);return Number.Compare(value,a)>=0&&Number.Compare(value,b)<=0; }
        if(!Between(x,bounds.FirstMinimumBits,bounds.FirstMaximumBits)||!Between(y,bounds.SecondMinimumBits,bounds.SecondMaximumBits))
            return new(cache,first,second,precision,TerrainPolygonQueryBranch.BoundsOutside,Zero);
        int crossings=0;
        for(int i=0,j=points.Length-1;i<points.Length;j=i++)
        {
            Number cx=Number.FromBits(points[i].FirstBits),cy=Number.FromBits(points[i].SecondBits),
                px=Number.FromBits(points[j].FirstBits),py=Number.FromBits(points[j].SecondBits);
            bool crosses=(Number.Compare(cy,y)<=0&&Number.Compare(y,py)<0)||(Number.Compare(py,y)<=0&&Number.Compare(y,cy)<0);
            if(!crosses)continue;
            Number intersection=Number.Add(Number.Divide(Number.Multiply(Number.Subtract(y,cy,p),Number.Subtract(px,cx,p),p),Number.Subtract(py,cy,p),p),cx,p);
            if(Number.Compare(intersection,x)>0)crossings++;
        }
        if((crossings&1)==0)return new(cache,first,second,precision,TerrainPolygonQueryBranch.ParityOutside,Zero,crossings);
        Number amount=Number.FromBits(cache.RuntimeFeatherAmountBits);
        if(Number.Compare(amount,Zero)==0)return new(cache,first,second,precision,TerrainPolygonQueryBranch.ZeroFeather,One,crossings);
        Number minimum=Number.Multiply(amount,amount,p);uint square=Store(minimum);
        static Number Distance(Number dx,Number dy,int p)=>Number.Add(Number.Multiply(dy,dy,p),Number.Multiply(dx,dx,p),p);
        static Number Select(Number current,Number candidate)
        { uint bits=Store(candidate);return Number.Compare(candidate,current)<0?Number.FromBits(bits):current; }
        foreach(var point in points)
            minimum=Select(minimum,Distance(Number.Subtract(x,Number.FromBits(point.FirstBits),p),Number.Subtract(y,Number.FromBits(point.SecondBits),p),p));
        int projections=0;
        for(int i=0,j=points.Length-1;i<points.Length;j=i++)
        {
            Number ax=Number.FromBits(points[j].FirstBits),ay=Number.FromBits(points[j].SecondBits),
                bx=Number.FromBits(points[i].FirstBits),by=Number.FromBits(points[i].SecondBits);
            uint nx=Store(Number.Subtract(ax,bx,p)),ny=Store(Number.Subtract(ay,by,p)),
                dy=Store(Number.Subtract(by,ay,p)),dx=Store(Number.Subtract(bx,ax,p));
            Number dot=Number.Add(Number.Multiply(Number.Subtract(x,ax,p),Number.FromBits(dx),p),Number.Multiply(Number.Subtract(y,ay,p),Number.FromBits(dy),p),p);
            Number length=Number.Add(Number.Multiply(Number.FromBits(ny),Number.FromBits(ny),p),Number.Multiply(Number.FromBits(nx),Number.FromBits(nx),p),p);
            Number parameter=Number.Divide(dot,length,p);uint parameterBits=Store(parameter);
            if(Number.Compare(parameter,Zero)<0||Number.Compare(Number.FromBits(parameterBits),One)>0)continue;
            projections++;
            Number projectedX=Number.Add(Number.Multiply(Number.FromBits(dx),Number.FromBits(parameterBits),p),ax,p),
                projectedY=Number.Add(Number.Multiply(Number.FromBits(dy),Number.FromBits(parameterBits),p),ay,p);
            minimum=Select(minimum,Distance(Number.Subtract(x,projectedX,p),Number.Subtract(y,projectedY,p),p));
        }
        Number lower=Number.Subtract(Number.FromBits(square),Tolerance,p),upper=Number.Add(Number.FromBits(square),Tolerance,p);
        bool full=Number.Compare(minimum,lower)>=0&&Number.Compare(minimum,upper)<=0;
        Number value=full?One:Number.Divide(Number.SquareRoot(minimum,p),amount,p);Store(value);
        return new(cache,first,second,precision,full?TerrainPolygonQueryBranch.FullBand:TerrainPolygonQueryBranch.Feather,
            value,crossings,square,Store(minimum),projections);
    }
    private static TerrainBoundaryPolygonMetadata Metadata(TerrainBoundaryPolygonInput input)
    { Need(input.HasDecodedFields&&input.Metadata is not null,"Opaque polygon input cannot be evaluated.");return input.Metadata!; }
    private static TerrainPolygonInfluenceLimits Limits(TerrainPolygonInfluenceLimits? limits)
    {
        limits??=new();if(limits.MaximumPoints is <0 or >65536||limits.MaximumSamples is <0 or >1048576||limits.MaximumPointVisits is <0 or >67108864)
            throw new ArgumentOutOfRangeException(nameof(limits));return limits;
    }
    private static void PointCap(int count,TerrainPolygonInfluenceLimits limits)=>Need(count<=limits.MaximumPoints,"Point cap before owned cache copy.");
    private static void SampleCap(int count,TerrainPolygonInfluenceLimits limits)=>Need(count<=limits.MaximumSamples,"Sample cap before snapshots/results.");
    private static void Charge(long visits,TerrainPolygonInfluenceLimits limits)=>Need(visits<=limits.MaximumPointVisits,"Conservative point-visit budget before work/publication.");
    private static uint Store(Number value)
    { uint bits=value.ToFloatBits();Need(Finite(bits),"Modern finite F32 spill/projection policy.");return bits; }
    private static bool Finite(uint bits)=>(bits&0x7f800000)!=0x7f800000;
    private static void ValidatePrecision(TerrainHeightArithmeticPrecision precision)
    { if(precision is not (TerrainHeightArithmeticPrecision.Binary24 or TerrainHeightArithmeticPrecision.Binary53 or TerrainHeightArithmeticPrecision.Binary64))throw new ArgumentOutOfRangeException(nameof(precision)); }
    private static void Need(bool value,string reason) { if(!value)throw new InvalidDataException(reason); }
}
