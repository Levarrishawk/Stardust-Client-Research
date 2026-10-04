using Number=Stardust.Core.Geometry.TerrainCircleArithmetic.Number;

namespace Stardust.Core.Geometry;

public enum TerrainLayerInfluenceQuerySource { SuppliedRawBits, Circle, Rectangle, Polygon, HeightFilter, FractalFilter }

/// <summary>Owned immutable query value; inspection is F32, caller shaping uses retained arithmetic value.</summary>
public sealed class TerrainLayerInfluenceQuery
{
    private TerrainLayerInfluenceQuery(Number value,TerrainHeightArithmeticPrecision precision,
        TerrainLayerInfluenceQuerySource source,uint? rawBits,TerrainCircleQuery? circle,TerrainRectangleQuery? rectangle,TerrainPolygonQuery? polygon=null,TerrainHeightFilterQuery? heightFilter=null,TerrainFractalFilterQuery? fractalFilter=null)
    {
        TerrainLayerInfluence.ValidatePrecision(precision);uint projection=value.ToFloatBits();
        TerrainLayerInfluence.Need(TerrainLayerInfluence.Finite(projection),"Finite query inspection required.");
        Value=value;Precision=precision;Source=source;RawBits=rawBits;ProjectionBits=projection;Circle=circle;Rectangle=rectangle;Polygon=polygon;HeightFilter=heightFilter;FractalFilter=fractalFilter;
    }
    internal Number Value { get; }
    public TerrainHeightArithmeticPrecision Precision { get; }
    public TerrainLayerInfluenceQuerySource Source { get; }
    public uint? RawBits { get; }
    public uint ProjectionBits { get; }
    public TerrainCircleQuery? Circle { get; }
    public TerrainRectangleQuery? Rectangle { get; }
    public TerrainPolygonQuery? Polygon { get; }
    public TerrainHeightFilterQuery? HeightFilter { get; }
    public TerrainFractalFilterQuery? FractalFilter { get; }
    public TerrainSuppliedFractalSample? RetainedFractalSample=>FractalFilter?.RetainedSample;
    public int RetainedFractalSampleBytes=>RetainedFractalSample is null?0:TerrainSuppliedFractalSample.AccountedBytes;
    public static TerrainLayerInfluenceQuery FromRawBits(uint bits,TerrainHeightArithmeticPrecision precision)
        =>new(Number.FromBits(bits),precision,TerrainLayerInfluenceQuerySource.SuppliedRawBits,bits,null,null);
    public static TerrainLayerInfluenceQuery FromCircle(TerrainCircleQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        return new(query.Value,query.Precision,TerrainLayerInfluenceQuerySource.Circle,null,query,null);
    }
    public static TerrainLayerInfluenceQuery FromRectangle(TerrainRectangleQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        return new(query.Value,query.Precision,TerrainLayerInfluenceQuerySource.Rectangle,null,null,query);
    }
    public static TerrainLayerInfluenceQuery FromPolygon(TerrainPolygonQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        return new(query.Value,query.Precision,TerrainLayerInfluenceQuerySource.Polygon,null,null,null,query);
    }
    public static TerrainLayerInfluenceQuery FromHeightFilter(TerrainHeightFilterQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        return new(query.Value,query.Precision,TerrainLayerInfluenceQuerySource.HeightFilter,null,null,null,null,query);
    }
    public static TerrainLayerInfluenceQuery FromFractalFilter(TerrainFractalFilterQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        return new(query.Value,query.Precision,TerrainLayerInfluenceQuerySource.FractalFilter,null,null,null,null,null,query);
    }
    public static TerrainLayerInfluenceQuery FromRetainedFractalFilter(TerrainFractalFilterQuery query,TerrainRetainedFractalLimits? limits=null)
    {
        TerrainSuppliedFractalSample.Cap(1,2,40,limits);
        ArgumentNullException.ThrowIfNull(query);
        TerrainLayerInfluence.Need(query.RetainedSample is not null,"An explicitly owned retained sample is required.");
        return FromFractalFilter(query);
    }
    public static TerrainRetainedFractalBatch<TerrainLayerInfluenceQuery> FromRetainedFractalFilters(
        ReadOnlySpan<TerrainFractalFilterQuery?> queries,TerrainRetainedFractalLimits? limits=null)
    {
        long work=2L*queries.Length,bytes=40L*queries.Length;
        TerrainSuppliedFractalSample.Cap(queries.Length,work,bytes,limits);
        var owned=queries.ToArray();var carriers=new TerrainLayerInfluenceQuery[owned.Length];
        for(int i=0;i<carriers.Length;i++)
        {
            TerrainLayerInfluence.Need(owned[i] is not null,"Owned retained query required.");
            carriers[i]=FromRetainedFractalFilter(owned[i]!,limits);
        }
        return new(carriers,work,bytes);
    }
    /// <summary>Inspection projection only. Combine compares the unspilled shaped Number.</summary>
    public uint ShapeProjectionBits(uint selectorBits,TerrainHeightArithmeticPrecision combinePrecision)
        =>TerrainLayerInfluence.Store(Shape(selectorBits,combinePrecision));
    internal Number Shape(uint selectorBits,TerrainHeightArithmeticPrecision combinePrecision)
    {
        TerrainLayerInfluence.ValidatePrecision(combinePrecision);int p=(int)combinePrecision;
        Number u=Value;
        return selectorBits switch
        {
            0=>u,
            1=>Number.Multiply(u,u,p),
            2=>Number.SquareRoot(u,p),
            3=>Number.Multiply(Number.Multiply(Number.Subtract(Number.FromBits(0x40400000),Number.Add(u,u,p),p),u,p),u,p),
            _=>Number.FromBits(0)
        };
    }
}


