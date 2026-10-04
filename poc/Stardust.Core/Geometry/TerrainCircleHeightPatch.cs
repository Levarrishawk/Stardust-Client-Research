using System.Numerics;
using Stardust.Core.Assets;

namespace Stardust.Core.Geometry;

public sealed record TerrainCircleHeightPatchLimits(int MaximumPatches=64,int MaximumGridSamples=289);
public readonly record struct TerrainCircleHeightSample(uint FirstCoordinateBits,uint SecondCoordinateBits,
    uint InputHeightBits,TerrainCircleInfluenceResult Circle,TerrainHeightConstantResult? Height,uint ResultHeightBits);

/// <summary>One supplied circle and height affector on an explicit synthetic X/height/Z grid.</summary>
public sealed class TerrainCircleHeightPatch
{
    private readonly TerrainCircleHeightSample[] samples;
    private readonly TerrainPatchAssembly[] patches;
    public int CellsPerSide { get; }
    public int GridWidth { get; }
    public TerrainHeightArithmeticPrecision CachePrecision { get; }
    public TerrainHeightArithmeticPrecision QueryPrecision { get; }
    public TerrainHeightArithmeticPrecision HeightPrecision { get; }
    public IReadOnlyList<TerrainCircleHeightSample> Samples { get; }
    public IReadOnlyList<TerrainPatchAssembly> Patches { get; }
    private TerrainCircleHeightPatch(int cells,TerrainCircleHeightSample[] samples,TerrainPatchAssembly[] patches,
        TerrainHeightArithmeticPrecision cache,TerrainHeightArithmeticPrecision query,TerrainHeightArithmeticPrecision height)
    {
        CellsPerSide=cells;GridWidth=2*cells+1;this.samples=samples;this.patches=patches;
        Samples=Array.AsReadOnly(samples);Patches=Array.AsReadOnly(patches);
        CachePrecision=cache;QueryPrecision=query;HeightPrecision=height;
    }
    public uint[] CopyHeightBits()=>samples.Select(s=>s.ResultHeightBits).ToArray();

    public static TerrainCircleHeightPatch Build(int cellsPerSide,ReadOnlySpan<uint> firstAxisBits,
        ReadOnlySpan<uint> secondAxisBits,ReadOnlySpan<uint> inputHeightBits,TerrainBoundaryCircleInput circle,
        TerrainHeightConstantInput height,TerrainHeightArithmeticPrecision cachePrecision,
        TerrainHeightArithmeticPrecision queryPrecision,TerrainHeightArithmeticPrecision heightPrecision,
        int seamMask=0,float uvScale=1,TerrainCircleHeightPatchLimits? limits=null)
    {
        ArgumentNullException.ThrowIfNull(circle);ArgumentNullException.ThrowIfNull(height);
        Precision(cachePrecision);Precision(queryPrecision);Precision(heightPrecision);
        limits??=new();
        if(limits.MaximumPatches is <1 or >1024 || limits.MaximumGridSamples is <9 or >4225)
            throw new ArgumentOutOfRangeException(nameof(limits));
        if(cellsPerSide is <1 or >32 || (uint)seamMask>15 || !float.IsFinite(uvScale) || uvScale<=0 || uvScale>16)
            throw new ArgumentOutOfRangeException(nameof(cellsPerSide));
        int width=2*cellsPerSide+1,total=width*width,count=cellsPerSide*cellsPerSide;
        Need(count<=limits.MaximumPatches && total<=limits.MaximumGridSamples,"Patch/grid budget.");
        Need(firstAxisBits.Length==width && secondAxisBits.Length==width && inputHeightBits.Length==total,"Exact grid shape.");
        // Reject opaque input even if disabled; inspect no inactive geometry numerically.
        Need(circle.Header is not null && circle.Fields is not null,"Decoded circle required.");
        uint[] first=firstAxisBits.ToArray(),second=secondAxisBits.ToArray(),initial=inputHeightBits.ToArray();
        Axis(first);Axis(second);
        var result=new TerrainCircleHeightSample[total];var positions=new Vector3[total];
        var normals=new Vector3[total];var colors=new uint[total];
        for(int row=0;row<width;row++)for(int column=0;column<width;column++)
        {
            int i=row*width+column;
            var influence=TerrainBoundaryCircleInfluence.Evaluate(circle,first[column],second[row],cachePrecision,queryPrecision);
            TerrainHeightConstantResult? applied=influence.SkippedDisabled?null:
                TerrainHeightConstantOperation.Evaluate(height,initial[i],influence.InfluenceBits!.Value,heightPrecision);
            uint output=applied?.ResultHeightBits??initial[i];float y=BitConverter.UInt32BitsToSingle(output);
            Need(float.IsFinite(y)&&Math.Abs(y)<=1_000_000,"Finite display height cap.");
            result[i]=new(first[column],second[row],initial[i],influence,applied,output);
            positions[i]=new(BitConverter.UInt32BitsToSingle(first[column]),y,BitConverter.UInt32BitsToSingle(second[row]));
            // Modern unshaded inspection policy: up normals and a fixed opaque blue color.
            normals[i]=Vector3.UnitY;colors[i]=0xFF4080C0;
        }
        var assembled=new TerrainPatchAssembly[count];
        for(int row=0;row<cellsPerSide;row++)for(int column=0;column<cellsPerSide;column++)
            assembled[row*cellsPerSide+column]=TerrainPatchAssembly.Assemble(width,width,positions,normals,colors,
                new(cellsPerSide,column,row,0,seamMask),uvScale,limits:new(MaximumGridSamples:limits.MaximumGridSamples));
        return new(cellsPerSide,result,assembled,cachePrecision,queryPrecision,heightPrecision);
    }
    private static void Axis(uint[] bits)
    {
        float previous=float.NegativeInfinity;
        foreach(uint raw in bits)
        {
            float value=BitConverter.UInt32BitsToSingle(raw);
            Need(float.IsFinite(value)&&Math.Abs(value)<=1_000_000&&value>previous,"Finite strictly increasing coordinate axis.");
            previous=value;
        }
    }
    private static void Precision(TerrainHeightArithmeticPrecision p)
    { if(p is not (TerrainHeightArithmeticPrecision.Binary24 or TerrainHeightArithmeticPrecision.Binary53 or TerrainHeightArithmeticPrecision.Binary64))throw new ArgumentOutOfRangeException(nameof(p)); }
    private static void Need(bool condition,string reason){if(!condition)throw new InvalidDataException(reason);}
}
