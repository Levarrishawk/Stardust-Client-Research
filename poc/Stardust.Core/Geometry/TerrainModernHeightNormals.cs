using System.Numerics;

namespace Stardust.Core.Geometry;

public enum TerrainModernNormalPolicy { ModernBinary64CentralDifference }
public sealed record TerrainModernHeightNormalLimits(int MaximumAxisSamples=32,int MaximumCells=1024,
    long MaximumCopyBytes=65536,long MaximumArrayBytes=131072,long MaximumWork=262144);

/// <summary>Owned modern normals from actual stored position axes/heights; no original-normal parity.</summary>
public sealed class TerrainModernHeightNormals
{
    private readonly uint[] x,z,heights;
    private readonly Vector3[] normals;
    private TerrainModernHeightNormals(TerrainCoordinateHeightPatch source,uint[] x,uint[] z,uint[] heights,Vector3[] normals,
        long copies,long arrays,long work)
    {
        Source=source;this.x=x;this.z=z;this.heights=heights;this.normals=normals;Normals=Array.AsReadOnly(normals);
        LogicalCopyBytes=copies;LogicalArrayBytes=arrays;LogicalWork=work;
    }
    public TerrainCoordinateHeightPatch Source { get; }
    public TerrainModernNormalPolicy Policy=>TerrainModernNormalPolicy.ModernBinary64CentralDifference;
    public bool OriginalNormalParity=>false;
    public int Width=>x.Length;
    public int Count=>normals.Length;
    public IReadOnlyList<Vector3> Normals { get; }
    public uint[] CopyPositionXBits()=>(uint[])x.Clone();
    public uint[] CopyPositionZBits()=>(uint[])z.Clone();
    public uint[] CopyPositionHeightBits()=>(uint[])heights.Clone();
    public Vector3[] CopyNormals()=>(Vector3[])normals.Clone();
    public long LogicalCopyBytes { get; }
    public long LogicalArrayBytes { get; }
    public long LogicalWork { get; }
    private static void Need(bool okay,string why){if(!okay)throw new InvalidDataException(why);}
    private static double F(uint bits)=>BitConverter.UInt32BitsToSingle(bits);
    private static void Axes(IReadOnlyList<uint> bits,int width,CancellationToken cancellation)
    {
        Need(bits.Count==width,"Complete position axis required.");
        for(int i=0;i<width;i++)
        {
            cancellation.ThrowIfCancellationRequested();double value=F(bits[i]);
            Need(double.IsFinite(value),"Finite stored position axis required.");
            if(i>0)Need(double.IsFinite(value-F(bits[i-1]))&&value>F(bits[i-1]),"Strictly positive stored coordinate spans required.");
        }
    }
    public static TerrainModernHeightNormals Build(TerrainCoordinateHeightPatch source,TerrainModernNormalPolicy policy,
        TerrainModernHeightNormalLimits? limits=null,CancellationToken cancellation=default)
    {
        ArgumentNullException.ThrowIfNull(source);limits??=new();
        if(policy!=TerrainModernNormalPolicy.ModernBinary64CentralDifference)throw new ArgumentOutOfRangeException(nameof(policy));
        if(limits.MaximumAxisSamples is <0 or >32||limits.MaximumCells is <0 or >1024
            ||limits.MaximumCopyBytes is <0 or >1048576||limits.MaximumArrayBytes is <0 or >2097152
            ||limits.MaximumWork is <0 or >4194304)throw new ArgumentOutOfRangeException(nameof(limits));
        int width=source.GridWidth;
        Need(width is >=2 and <=32&&width<=limits.MaximumAxisSamples,"Bounded position axes required.");
        int count=checked(width*width);Need(count<=limits.MaximumCells,"Normal cell cap.");
        long copies=8L*width+4L*count,arrays=256L+8L*width+16L*count,work=128L+4L*width+80L*count;
        Need(copies<=limits.MaximumCopyBytes&&arrays<=limits.MaximumArrayBytes&&work<=limits.MaximumWork,"Copy/array/work preflight before source exports or arrays.");
        cancellation.ThrowIfCancellationRequested();
        var position=source.Coordinates.Position;Axes(position.FirstAxisBits,width,cancellation);Axes(position.SecondAxisBits,width,cancellation);
        uint[] x=position.CopyFirstAxisBits(),z=position.CopySecondAxisBits(),heights=source.CopyPositionHeightBits();
        Axes(x,width,cancellation);Axes(z,width,cancellation);Need(heights.Length==count,"Complete owned stored-height shape required.");
        foreach(uint bits in heights){cancellation.ThrowIfCancellationRequested();Need(double.IsFinite(F(bits)),"Finite copied stored position heights required.");}
        var normals=new Vector3[count];
        for(int row=0;row<width;row++)for(int column=0;column<width;column++)
        {
            cancellation.ThrowIfCancellationRequested();
            int left=column==0?0:column-1,right=column==width-1?width-1:column+1;
            int previous=row==0?0:row-1,next=row==width-1?width-1:row+1;
            double dx=F(x[right])-F(x[left]),dz=F(z[next])-F(z[previous]);
            Need(double.IsFinite(dx)&&double.IsFinite(dz)&&dx>0&&dz>0,"Finite positive stencil spans required.");
            double gx=(F(heights[row*width+right])-F(heights[row*width+left]))/dx;
            double gz=(F(heights[next*width+column])-F(heights[previous*width+column]))/dz;
            Need(double.IsFinite(gx)&&double.IsFinite(gz),"Finite modern derivatives required.");
            double length=Math.Sqrt(gx*gx+1+gz*gz);
            Need(double.IsFinite(length)&&length>0,"Finite modern normalization required.");
            double nx=-gx/length,ny=1/length,nz=-gz/length;
            Need(double.IsFinite(nx)&&double.IsFinite(ny)&&double.IsFinite(nz),"Finite modern normalized components required.");
            var value=new Vector3((float)nx,(float)ny,(float)nz);
            Need(float.IsFinite(value.X)&&float.IsFinite(value.Y)&&float.IsFinite(value.Z),"Finite F32 normal export required.");
            normals[row*width+column]=value;
        }
        cancellation.ThrowIfCancellationRequested();return new(source,x,z,heights,normals,copies,arrays,work);
    }
}
