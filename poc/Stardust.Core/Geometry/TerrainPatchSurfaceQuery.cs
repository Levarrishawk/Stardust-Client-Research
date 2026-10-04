using System.Numerics;
namespace Stardust.Core.Geometry;

public sealed record TerrainPatchSurfaceQueryLimits(double MaximumCoordinateComponent=1_000_000,
    double MinimumProjectedDoubleArea=1e-12,double MinimumInterpolatedNormalLength=1e-6,int MaximumQueriesPerBatch=4096,
    bool RequireCoherentWinding=true);
public readonly record struct TerrainPatchSurfaceLocation(double X,double Z);
public readonly record struct TerrainPatchSurfaceVector(double X,double Y,double Z);
public readonly record struct TerrainPatchSurfaceHit(int PatternIndex,int TriangleIndex,ushort VertexA,ushort VertexB,ushort VertexC,
    double WeightA,double WeightB,double WeightC,double Height,TerrainPatchSurfaceVector Normal);

/// <summary>Immutable supplied native X/Z triangle-height query; modern binary64/closed-edge policy, no original collision claim.</summary>
public sealed class TerrainPatchSurfaceQuery
{
    private readonly Vector3[] positions,normals;
    private readonly ushort[] indices;
    private readonly double[] areas;
    private readonly TerrainPatchSurfaceQueryLimits limits;
    private readonly double minimumX,maximumX,minimumZ,maximumZ;
    public int PatternIndex{get;}
    public int TriangleCount=>areas.Length;
    public long LogicalOwnedBytes=>216L+2L*indices.Length+8L*areas.Length;
    public TerrainPatchSurfaceQuery(TerrainPatchGeometry geometry,TerrainPatchSurfaceQueryLimits? limits=null)
    {
        ArgumentNullException.ThrowIfNull(geometry);var l=limits??new();
        if(!double.IsFinite(l.MaximumCoordinateComponent)||l.MaximumCoordinateComponent<=0||l.MaximumCoordinateComponent>1e9||
           !double.IsFinite(l.MinimumProjectedDoubleArea)||l.MinimumProjectedDoubleArea<=0||l.MinimumProjectedDoubleArea>1e18||
           !double.IsFinite(l.MinimumInterpolatedNormalLength)||l.MinimumInterpolatedNormalLength<=0||l.MinimumInterpolatedNormalLength>1e9||
           l.MaximumQueriesPerBatch is <1 or >65536)throw new ArgumentOutOfRangeException(nameof(limits));
        this.limits=l with {};PatternIndex=geometry.PatternIndex;positions=geometry.CopyPositions();normals=geometry.CopyNormals();indices=geometry.CopyIndices();areas=new double[indices.Length/3];
        foreach(var v in positions)ValidatePosition(v,l.MaximumCoordinateComponent);
        minimumX=positions.Min(p=>(double)p.X);maximumX=positions.Max(p=>(double)p.X);minimumZ=positions.Min(p=>(double)p.Z);maximumZ=positions.Max(p=>(double)p.Z);
        for(int t=0;t<areas.Length;t++)
        {
            var a=positions[indices[3*t]];var b=positions[indices[3*t+1]];var c=positions[indices[3*t+2]];
            double bx=(double)b.X-a.X,bz=(double)b.Z-a.Z,cx=(double)c.X-a.X,cz=(double)c.Z-a.Z;
            double area=Cross(bx,bz,cx,cz);if(!double.IsFinite(area)||Math.Abs(area)<=l.MinimumProjectedDoubleArea)throw new ArgumentException("Degenerate/ineligible projected X/Z triangle.",nameof(geometry));areas[t]=area;
            if(l.RequireCoherentWinding)
            {
                double by=(double)b.Y-a.Y,cy=(double)c.Y-a.Y,x=by*cz-bz*cy,y=bz*cx-bx*cz,z=bx*cy-by*cx;
                var na=normals[indices[3*t]];var nb=normals[indices[3*t+1]];var nc=normals[indices[3*t+2]];
                double dot=x*((double)na.X+nb.X+nc.X)+y*((double)na.Y+nb.Y+nc.Y)+z*((double)na.Z+nb.Z+nc.Z);
                if(!double.IsFinite(dot)||dot<=0)throw new ArgumentException("Triangle winding disagrees with summed supplied normals.",nameof(geometry));
            }
        }
    }
    private static double Cross(double x,double z,double otherX,double otherZ)=>x*otherZ-z*otherX;
    private static void ValidatePosition(Vector3 p,double cap)
    {if(!float.IsFinite(p.X)||!float.IsFinite(p.Y)||!float.IsFinite(p.Z)||Math.Abs((double)p.X)>cap||Math.Abs((double)p.Y)>cap||Math.Abs((double)p.Z)>cap)throw new ArgumentException("Supplied position outside finite query cap.");}
    private void ValidateLocation(TerrainPatchSurfaceLocation p)
    {if(!double.IsFinite(p.X)||!double.IsFinite(p.Z)||Math.Abs(p.X)>limits.MaximumCoordinateComponent||Math.Abs(p.Z)>limits.MaximumCoordinateComponent)throw new ArgumentOutOfRangeException(nameof(p),"Finite bounded native X/Z location required.");}
    public TerrainPatchSurfaceHit? Query(double x,double z)
    {var p=new TerrainPatchSurfaceLocation(x,z);ValidateLocation(p);return QueryValidated(p);}
    public IReadOnlyList<TerrainPatchSurfaceHit?> QueryBatch(ReadOnlySpan<TerrainPatchSurfaceLocation> locations)
    {
        if(locations.Length>limits.MaximumQueriesPerBatch)throw new ArgumentOutOfRangeException(nameof(locations),"Surface query batch cap.");
        var owned=locations.ToArray();foreach(var p in owned)ValidateLocation(p);
        var results=new TerrainPatchSurfaceHit?[owned.Length];for(int i=0;i<results.Length;i++)results[i]=QueryValidated(owned[i]);return Array.AsReadOnly(results);
    }
    private TerrainPatchSurfaceHit? QueryValidated(TerrainPatchSurfaceLocation p)
    {
        if(p.X<minimumX||p.X>maximumX||p.Z<minimumZ||p.Z>maximumZ)return null;
        for(int t=0;t<areas.Length;t++)
        {
            ushort ia=indices[3*t],ib=indices[3*t+1],ic=indices[3*t+2];var a=positions[ia];var b=positions[ib];var c=positions[ic];double area=areas[t];
            double aw=Cross((double)b.X-p.X,(double)b.Z-p.Z,(double)c.X-p.X,(double)c.Z-p.Z);
            double bw=Cross((double)c.X-p.X,(double)c.Z-p.Z,(double)a.X-p.X,(double)a.Z-p.Z);
            double cw=Cross((double)a.X-p.X,(double)a.Z-p.Z,(double)b.X-p.X,(double)b.Z-p.Z);
            if(area>0?(aw<0||bw<0||cw<0):(aw>0||bw>0||cw>0))continue;
            double wa=aw/area,wb=bw/area,wc=cw/area;
            if(wa<0||wa>1||wb<0||wb>1||wc<0||wc>1)throw new InvalidDataException("Selected computed barycentric weights outside [0,1].");
            double height=(double)a.Y+wb*((double)b.Y-a.Y)+wc*((double)c.Y-a.Y);
            var na=normals[ia];var nb=normals[ib];var nc=normals[ic];
            double nx=(double)na.X+wb*((double)nb.X-na.X)+wc*((double)nc.X-na.X);
            double ny=(double)na.Y+wb*((double)nb.Y-na.Y)+wc*((double)nc.Y-na.Y);
            double nz=(double)na.Z+wb*((double)nb.Z-na.Z)+wc*((double)nc.Z-na.Z);
            double length=Math.Sqrt(nx*nx+ny*ny+nz*nz);
            if(!double.IsFinite(height)||Math.Abs(height)>limits.MaximumCoordinateComponent||!double.IsFinite(length)||length<=limits.MinimumInterpolatedNormalLength)throw new InvalidDataException("Selected surface result has ineligible height/interpolated normal.");
            var normal=new TerrainPatchSurfaceVector(nx/length,ny/length,nz/length);
            if(!double.IsFinite(wa)||!double.IsFinite(wb)||!double.IsFinite(wc)||!double.IsFinite(normal.X)||!double.IsFinite(normal.Y)||!double.IsFinite(normal.Z))throw new InvalidDataException("Nonfinite surface result.");
            return new(PatternIndex,t,ia,ib,ic,wa,wb,wc,height,normal);
        }
        return null;
    }
    public Vector3[] CopyPositions()=>(Vector3[])positions.Clone();
    public Vector3[] CopyNormals()=>(Vector3[])normals.Clone();
    public ushort[] CopyIndices()=>(ushort[])indices.Clone();
}
