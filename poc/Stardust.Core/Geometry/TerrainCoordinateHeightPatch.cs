using System.Numerics;
using Number=Stardust.Core.Geometry.TerrainCircleArithmetic.Number;

namespace Stardust.Core.Geometry;

public sealed record TerrainCoordinateHeightPatchLimits(int MaximumGridSamples=65536,int MaximumPatches=64,int MaximumSelectedSamples=576);

/// <summary>Complete padded coordinate grid plus supplied channels; no procedural height/material/normal evaluation.</summary>
public sealed class TerrainCoordinateHeightPatch
{
    private readonly uint[] heights,positionHeights,colors;
    private readonly Vector3[] normals;
    private TerrainCoordinateHeightPatch(TerrainSampleCoordinateGrid coordinates,uint[] heights,uint[] positionHeights,
        Vector3[] normals,uint[] colors,TerrainPatchAssembly[] patches,int seamMask,float uvScale)
    {
        Coordinates=coordinates;this.heights=heights;this.positionHeights=positionHeights;this.normals=normals;this.colors=colors;
        Patches=Array.AsReadOnly(patches);SeamMask=seamMask;UvScale=uvScale;
    }
    public TerrainSampleCoordinateGrid Coordinates { get; }
    public int GridWidth=>Coordinates.Parameters.Count;
    public int CellsPerSide=>Coordinates.Request.Tiles;
    public int SampleBorder=>Coordinates.Request.Margin;
    public int LeadingPadding=>SampleBorder;
    public int TrailingPadding=>GridWidth-(SampleBorder+2*CellsPerSide+1);
    public int SeamMask { get; }
    public float UvScale { get; }
    public IReadOnlyList<TerrainPatchAssembly> Patches { get; }
    public uint[] CopyInputHeightBits()=>(uint[])heights.Clone();
    public uint[] CopyPositionHeightBits()=>(uint[])positionHeights.Clone();
    public Vector3[] CopyNormals()=>(Vector3[])normals.Clone();
    public uint[] CopyArgbColors()=>(uint[])colors.Clone();

    public static TerrainCoordinateHeightPatch Build(TerrainSampleCoordinateGrid coordinates,ReadOnlySpan<uint> heightBits,
        ReadOnlySpan<Vector3> normals,ReadOnlySpan<uint> argbColors,int seamMask=0,float uvScale=1,
        TerrainCoordinateHeightPatchLimits? limits=null)
    {
        ArgumentNullException.ThrowIfNull(coordinates);limits??=new();
        if(limits.MaximumGridSamples is <1 or >1048576 || limits.MaximumPatches is <1 or >1024 || limits.MaximumSelectedSamples is <1 or >9216)
            throw new ArgumentOutOfRangeException(nameof(limits));
        if((uint)seamMask>15 || !float.IsFinite(uvScale) || uvScale<=0 || uvScale>16)throw new ArgumentOutOfRangeException(nameof(seamMask));
        int width=coordinates.Parameters.Count,tiles=coordinates.Request.Tiles,border=coordinates.Request.Margin;
        Need(tiles is >0 and <=32 && border>=0 && (long)border+2L*tiles+1<=width,"Complete active padded extent.");
        long total=(long)width*width,patches=(long)tiles*tiles,selected=9*patches;
        Need(total<=limits.MaximumGridSamples && patches<=limits.MaximumPatches && selected<=limits.MaximumSelectedSamples,"Aggregate grid/patch/selected sample budget.");
        Need(heightBits.Length==total && normals.Length==total && argbColors.Length==total,"Exact complete row-major channel shapes.");
        Need(coordinates.Position.HasStrictlyIncreasingDisplayAxes,"Strict stored position axes required for display.");
        foreach(uint bits in coordinates.Position.FirstAxisBits)Axis(bits);
        foreach(uint bits in coordinates.Position.SecondAxisBits)Axis(bits);
        // Every complete channel is owned, including unused padding; numeric eligibility is modern policy.
        uint[] height=heightBits.ToArray(),color=argbColors.ToArray();Vector3[] normal=normals.ToArray();
        uint[] stored=new uint[(int)total];var positions=new Vector3[(int)total];
        var zero=Number.FromBits(coordinates.Parameters.VerticalOriginBits);int precision=(int)coordinates.Policies.PositionCoordinates;
        for(int i=0;i<positions.Length;i++)
        {
            float y=BitConverter.UInt32BitsToSingle(height[i]);Need(float.IsFinite(y)&&Math.Abs(y)<=1_000_000,"Finite display height cap.");
            var n=normal[i];Need(float.IsFinite(n.X)&&float.IsFinite(n.Y)&&float.IsFinite(n.Z)&&Math.Abs(n.X)<=4096&&Math.Abs(n.Y)<=4096&&Math.Abs(n.Z)<=4096,"Finite supplied normal cap.");
            Need((double)n.X*n.X+(double)n.Y*n.Y+(double)n.Z*n.Z>=1e-12,"Nonzero supplied normal.");
            stored[i]=Number.Add(Number.FromBits(height[i]),zero,precision).ToFloatBits();
            positions[i]=new(BitConverter.UInt32BitsToSingle(coordinates.Position.FirstAxisBits[i%width]),BitConverter.UInt32BitsToSingle(stored[i]),
                BitConverter.UInt32BitsToSingle(coordinates.Position.SecondAxisBits[i/width]));
        }
        var assembled=new TerrainPatchAssembly[(int)patches];
        for(int row=0;row<tiles;row++)for(int column=0;column<tiles;column++)
            assembled[row*tiles+column]=TerrainPatchAssembly.Assemble(width,width,positions,normal,color,
                new(tiles,column,row,border,seamMask),uvScale,limits:new(MaximumGridSamples:limits.MaximumGridSamples));
        // Frozen surface-query eligibility is a separate modern projected-area gate.
        foreach(var patch in assembled)_=new TerrainPatchSurfaceQuery(patch.Geometry);
        return new(coordinates,height,stored,normal,color,assembled,seamMask,uvScale);
    }
    private static void Axis(uint bits)
    { float f=BitConverter.UInt32BitsToSingle(bits);Need(float.IsFinite(f)&&Math.Abs(f)<=1_000_000,"Finite display coordinate cap."); }
    private static void Need(bool condition,string reason){if(!condition)throw new InvalidDataException(reason);}
}
