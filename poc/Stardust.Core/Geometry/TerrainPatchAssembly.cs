using System.Numerics;

namespace Stardust.Core.Geometry;

public readonly record struct TerrainPatchCell(int CellsPerSide, int Column, int Row, int SampleBorder, int SeamMask);
public sealed record TerrainPatchAssemblyLimits(int MaximumGridSamples = 1_048_576, bool RequireCoherentWinding = true);

/// <summary>Exact sample selection and seam topology over supplied resolved common-stride grids.</summary>
public sealed class TerrainPatchAssembly
{
    private static readonly (int Column,int Row)[] Offsets = [(1,1),(0,1),(0,2),(1,2),(2,2),(2,1),(2,0),(1,0),(0,0)];
    private readonly int[] sampleIndices;
    public TerrainPatchGeometry Geometry { get; }
    public TerrainPatchCell Cell { get; }
    public int GridWidth { get; }
    public int GridHeight { get; }
    private TerrainPatchAssembly(TerrainPatchGeometry geometry, TerrainPatchCell cell, int width, int height, int[] indices)
    { Geometry=geometry;Cell=cell;GridWidth=width;GridHeight=height;sampleIndices=indices; }
    public int[] CopySourceSampleIndices() => (int[])sampleIndices.Clone();

    public static int SelectPattern(int cellsPerSide, int column, int row, int seamMask)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(cellsPerSide,1);
        if ((uint)column >= cellsPerSide) throw new ArgumentOutOfRangeException(nameof(column));
        if ((uint)row >= cellsPerSide) throw new ArgumentOutOfRangeException(nameof(row));
        if ((uint)seamMask > 15) throw new ArgumentOutOfRangeException(nameof(seamMask));
        int last=cellsPerSide-1;
        // Branch order matters when minimum and maximum boundaries coincide.
        if (row==0 && (seamMask&4)!=0)
        {
            if(column==0 && (seamMask&2)!=0)return 4;
            if(column==last && (seamMask&8)!=0)return 6;
            return 5;
        }
        if(column==0 && (seamMask&2)!=0)return row==last && (seamMask&1)!=0 ? 2 : 3;
        if(row==last && (seamMask&1)!=0)return column==last && (seamMask&8)!=0 ? 8 : 1;
        if(column==last && (seamMask&8)!=0)return 7;
        return 0;
    }

    public static TerrainPatchAssembly Assemble(int gridWidth, int gridHeight,
        ReadOnlySpan<Vector3> positions, ReadOnlySpan<Vector3> normals, ReadOnlySpan<uint> argbColors,
        TerrainPatchCell cell, float uvScale, ReadOnlySpan<int> auxiliarySelectors = default,
        TerrainPatchGeometryLimits? geometryLimits = null, TerrainPatchAssemblyLimits? limits = null)
    {
        limits ??= new();
        ArgumentOutOfRangeException.ThrowIfLessThan(limits.MaximumGridSamples,1);
        int pattern=SelectPattern(cell.CellsPerSide,cell.Column,cell.Row,cell.SeamMask);
        ArgumentOutOfRangeException.ThrowIfNegative(cell.SampleBorder);
        ArgumentOutOfRangeException.ThrowIfLessThan(gridWidth,1);
        ArgumentOutOfRangeException.ThrowIfLessThan(gridHeight,1);
        int total=checked(gridWidth*gridHeight);
        int required=checked(cell.SampleBorder+checked(2*cell.CellsPerSide)+1);
        if(gridWidth<required || gridHeight<required)throw new ArgumentException("Complete active cell grid must fit the supplied dimensions.");
        if(total>limits.MaximumGridSamples)throw new ArgumentOutOfRangeException(nameof(limits),"Grid sample cap.");
        if(positions.Length!=total || normals.Length!=total || argbColors.Length!=total)
            throw new ArgumentException("All supplied common-stride grids must have exactly width*height samples.");
        int originColumn=checked(cell.SampleBorder+checked(2*cell.Column));
        int originRow=checked(cell.SampleBorder+checked(2*cell.Row));
        var p=new Vector3[9];var n=new Vector3[9];var colors=new uint[9];var indices=new int[9];
        for(int i=0;i<9;i++)
        {
            int index=checked(checked((originRow+Offsets[i].Row)*gridWidth)+originColumn+Offsets[i].Column);
            indices[i]=index;p[i]=positions[index];n[i]=normals[index];colors[i]=argbColors[index];
        }
        var geometry=new TerrainPatchGeometry(p,n,colors,uvScale,pattern,auxiliarySelectors,geometryLimits);
        if(limits.RequireCoherentWinding)ValidateWinding(geometry);
        return new(geometry,cell,gridWidth,gridHeight,indices);
    }

    private static void ValidateWinding(TerrainPatchGeometry geometry)
    {
        var p=geometry.CopyPositions();var n=geometry.CopyNormals();var indices=geometry.CopyIndices();
        for(int i=0;i<indices.Length;i+=3)
        {
            var a=p[indices[i]];var b=p[indices[i+1]];var c=p[indices[i+2]];
            double bx=(double)b.X-a.X,by=(double)b.Y-a.Y,bz=(double)b.Z-a.Z;
            double cx=(double)c.X-a.X,cy=(double)c.Y-a.Y,cz=(double)c.Z-a.Z;
            double x=by*cz-bz*cy,y=bz*cx-bx*cz,z=bx*cy-by*cx;
            var normal=n[indices[i]]+n[indices[i+1]]+n[indices[i+2]];
            if(x*x+y*y+z*z<1e-20 || x*normal.X+y*normal.Y+z*normal.Z<=0)
                throw new ArgumentException("Modern assembly winding requires nondegenerate CCW triangles coherent with normals.");
        }
    }
}
