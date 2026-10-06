using System.Numerics;
namespace Stardust.Core.Geometry;

public readonly record struct TerrainModernRayVector(double X,double Y,double Z);
public readonly record struct TerrainModernPatchSetRay(TerrainModernRayVector Origin,TerrainModernRayVector Direction,double MinimumT,double MaximumT);
public sealed record TerrainModernPatchSetRayQueryLimits(int MaximumChunks=16,int MaximumPatches=256,int MaximumTriangles=2048,int MaximumQueries=4096,
    long MaximumBuildCopyBytes=1048576,long MaximumBuildArrayBytes=2097152,long MaximumBuildWork=1048576,
    long MaximumQueryCopyBytes=1048576,long MaximumQueryArrayBytes=2097152,long MaximumQueryWork=2147483648L,long MaximumTriangleTests=8388608,
    double MaximumComponent=1000000,double MaximumParameter=1000000000,double MinimumInterpolatedNormalLength=1e-6);
public readonly record struct TerrainModernPatchSetRayReservation(int Queries,long CopyBytes,long ArrayBytes,long Work,long TriangleTestBound);
public sealed record TerrainModernPatchSetRayHit(TerrainModernPatchSetSurfaceQuery Source,TerrainModernPatchSetSurfaceChild Membership,
    int PatternIndex,int TriangleIndex,ushort VertexA,ushort VertexB,ushort VertexC,double T,TerrainModernRayVector Position,
    double WeightA,double WeightB,double WeightC,TerrainModernRayVector Normal)
{
    public int ChunkIndex=>Membership.ChunkIndex;
    public int PatchIndex=>Membership.PatchIndex;
    public TerrainModernHeightPatch Chunk=>Membership.Chunk;
    public TerrainPatchAssembly Patch=>Membership.Patch;
    public TerrainPatchGeometry Geometry=>Membership.Geometry;
    public TerrainPatchSurfaceQuery Child=>Membership.Query;
}

/// <summary>Bounded modern double-sided native-space rays; direction is never normalized.</summary>
public sealed class TerrainModernPatchSetRayQuery
{
    private sealed record Snapshot(TerrainModernPatchSetSurfaceChild Membership,Vector3[] Positions,Vector3[] Normals,ushort[] Indices);
    private readonly Snapshot[] snapshots;
    private readonly TerrainModernPatchSetRayQueryLimits limits;
    private TerrainModernPatchSetRayQuery(TerrainModernPatchSetSurfaceQuery source,Snapshot[] snapshots,TerrainModernPatchSetRayQueryLimits limits)
    {Source=source;this.snapshots=snapshots;this.limits=limits;}
    public TerrainModernPatchSetSurfaceQuery Source {get;}
    public IReadOnlyList<TerrainModernPatchSetSurfaceChild> Children=>Source.Children;
    public int ChunkCount=>Source.ChunkCount;
    public int PatchCount=>Source.PatchCount;
    public int TotalTriangles=>Source.TotalTriangles;
    public int NormalBuilds=>0;
    public int GeometryBuilds=>0;
    public int SurfaceBuilds=>0;
    public int SetAdmissions=>0;
    public bool OriginalCollisionParity=>false;
    public long OwnCopyBytes=>216L*PatchCount+6L*TotalTriangles;
    public long OwnArrayBytes=>512L+256L*PatchCount+OwnCopyBytes;
    public long OwnWork=>128L+64L*ChunkCount+128L*PatchCount+128L*TotalTriangles;
    public long BorrowedSurfaceArrayReservation=>Source.AggregateArrayBytes+Source.BorrowedSetArrayReservation;
    public Vector3[] CopyPositions(int childIndex)=>(Vector3[])snapshots[childIndex].Positions.Clone();
    public Vector3[] CopyNormals(int childIndex)=>(Vector3[])snapshots[childIndex].Normals.Clone();
    public ushort[] CopyIndices(int childIndex)=>(ushort[])snapshots[childIndex].Indices.Clone();
    private static void Need(bool value,string reason){if(!value)throw new InvalidDataException(reason);}
    private static void Options(TerrainModernPatchSetRayQueryLimits l)
    {
        if(l.MaximumChunks is <0 or >16||l.MaximumPatches is <0 or >256||l.MaximumTriangles is <0 or >2048||l.MaximumQueries is <0 or >4096
            ||l.MaximumBuildCopyBytes is <0 or >4194304||l.MaximumBuildArrayBytes is <0 or >8388608||l.MaximumBuildWork is <0 or >16777216
            ||l.MaximumQueryCopyBytes is <0 or >4194304||l.MaximumQueryArrayBytes is <0 or >8388608||l.MaximumQueryWork is <0 or >4294967296L
            ||l.MaximumTriangleTests is <0 or >8388608||!double.IsFinite(l.MaximumComponent)||l.MaximumComponent<=0||l.MaximumComponent>1e9
            ||!double.IsFinite(l.MaximumParameter)||l.MaximumParameter<=0||l.MaximumParameter>1e12
            ||!double.IsFinite(l.MinimumInterpolatedNormalLength)||l.MinimumInterpolatedNormalLength<=0||l.MinimumInterpolatedNormalLength>1e9)
            throw new ArgumentOutOfRangeException(nameof(l));
    }
    private static bool Bounded(TerrainModernRayVector v,double cap)=>double.IsFinite(v.X)&&double.IsFinite(v.Y)&&double.IsFinite(v.Z)&&Math.Abs(v.X)<=cap&&Math.Abs(v.Y)<=cap&&Math.Abs(v.Z)<=cap;
    private static TerrainModernRayVector V(Vector3 v)=>new(v.X,v.Y,v.Z);
    private static TerrainModernRayVector Sub(TerrainModernRayVector a,TerrainModernRayVector b)=>new(a.X-b.X,a.Y-b.Y,a.Z-b.Z);
    private static TerrainModernRayVector Cross(TerrainModernRayVector a,TerrainModernRayVector b)=>new(a.Y*b.Z-a.Z*b.Y,a.Z*b.X-a.X*b.Z,a.X*b.Y-a.Y*b.X);
    private static double Dot(TerrainModernRayVector a,TerrainModernRayVector b)=>a.X*b.X+a.Y*b.Y+a.Z*b.Z;
    public static TerrainModernPatchSetRayQuery Build(TerrainModernPatchSetSurfaceQuery source,TerrainModernPatchSetRayQueryLimits? limits=null,CancellationToken cancellation=default)
    {
        ArgumentNullException.ThrowIfNull(source);var l=limits??new();Options(l);cancellation.ThrowIfCancellationRequested();
        Need(source.ChunkCount<=l.MaximumChunks&&source.PatchCount<=l.MaximumPatches&&source.TotalTriangles<=l.MaximumTriangles,"Retained count caps before copies.");
        int triangles=0,previousChunk=-1,previousPatch=-1;
        foreach(var child in source.Children)
        {
            cancellation.ThrowIfCancellationRequested();
            Need(child.ChunkIndex>=0&&child.ChunkIndex<source.ChunkCount&&ReferenceEquals(child.Chunk,source.Source.Patches[child.ChunkIndex])
                &&child.PatchIndex>=0&&child.PatchIndex<child.Chunk.Patch.Patches.Count&&ReferenceEquals(child.Patch,child.Chunk.Patch.Patches[child.PatchIndex])
                &&ReferenceEquals(child.Geometry,child.Patch.Geometry)&&child.Query.TriangleCount==child.Geometry.TriangleCount,"Actual retained identities.");
            Need(child.ChunkIndex>previousChunk||(child.ChunkIndex==previousChunk&&child.PatchIndex>previousPatch),"Retained lexicographic order.");
            previousChunk=child.ChunkIndex;previousPatch=child.PatchIndex;
            Need(Bounded(V(child.Geometry.Minimum),l.MaximumComponent)&&Bounded(V(child.Geometry.Maximum),l.MaximumComponent),"Geometry bounds before copies.");
            triangles=checked(triangles+child.Geometry.TriangleCount);
        }
        Need(triangles==source.TotalTriangles&&source.Children.Count==source.PatchCount,"Retained triangle count.");
        long copies=checked(216L*source.PatchCount+6L*triangles),arrays=checked(512L+256L*source.PatchCount+copies),work=checked(128L+64L*source.ChunkCount+128L*source.PatchCount+128L*triangles);
        Need(copies<=l.MaximumBuildCopyBytes&&arrays<=l.MaximumBuildArrayBytes&&work<=l.MaximumBuildWork,"Complete construction preflight before arrays.");
        cancellation.ThrowIfCancellationRequested();var owned=new Snapshot[source.PatchCount];int index=0;
        foreach(var child in source.Children)
        {
            cancellation.ThrowIfCancellationRequested();owned[index++]=new(child,child.Geometry.CopyPositions(),child.Geometry.CopyNormals(),child.Geometry.CopyIndices());
        }
        cancellation.ThrowIfCancellationRequested();return new(source,owned,l);
    }
    public TerrainModernPatchSetRayReservation GetQueryReservation(int queries)
    {
        if(queries<0||queries>limits.MaximumQueries)throw new ArgumentOutOfRangeException(nameof(queries));
        long tests=checked((long)queries*TotalTriangles),copy=checked(64L*queries),arrays=checked(256L+256L*queries),work=checked(128L+64L*queries+(long)queries*(16L*PatchCount+128L*TotalTriangles));
        Need(tests<=limits.MaximumTriangleTests&&copy<=limits.MaximumQueryCopyBytes&&arrays<=limits.MaximumQueryArrayBytes&&work<=limits.MaximumQueryWork,"Query preflight before arrays.");
        return new(queries,copy,arrays,work,tests);
    }
    private void Validate(TerrainModernPatchSetRay ray)
    {
        Need(Bounded(ray.Origin,limits.MaximumComponent)&&Bounded(ray.Direction,limits.MaximumComponent)
            &&(ray.Direction.X!=0||ray.Direction.Y!=0||ray.Direction.Z!=0),"Finite bounded nonzero direction and origin.");
        Need(double.IsFinite(ray.MinimumT)&&double.IsFinite(ray.MaximumT)&&ray.MinimumT>=0&&ray.MinimumT<=ray.MaximumT&&ray.MaximumT<=limits.MaximumParameter,"Finite ordered bounded interval.");
    }
    public TerrainModernPatchSetRayHit? Query(TerrainModernPatchSetRay ray,CancellationToken cancellation=default)
    {cancellation.ThrowIfCancellationRequested();_=GetQueryReservation(1);Validate(ray);var hit=Find(ray,cancellation);cancellation.ThrowIfCancellationRequested();return hit;}
    public IReadOnlyList<TerrainModernPatchSetRayHit?> QueryBatch(ReadOnlySpan<TerrainModernPatchSetRay> rays,CancellationToken cancellation=default)
    {
        cancellation.ThrowIfCancellationRequested();_=GetQueryReservation(rays.Length);
        foreach(var ray in rays){cancellation.ThrowIfCancellationRequested();Validate(ray);}
        var owned=rays.ToArray();foreach(var ray in owned){cancellation.ThrowIfCancellationRequested();Validate(ray);}
        var result=new TerrainModernPatchSetRayHit?[owned.Length];
        for(int i=0;i<owned.Length;i++)result[i]=Find(owned[i],cancellation);
        cancellation.ThrowIfCancellationRequested();return Array.AsReadOnly(result);
    }
    private TerrainModernPatchSetRayHit? Find(TerrainModernPatchSetRay ray,CancellationToken cancellation)
    {
        Snapshot? chosen=null;int chosenTriangle=-1;ushort ia=0,ib=0,ic=0;double best=double.PositiveInfinity,wa=0,wb=0,wc=0;TerrainModernRayVector point=default;
        foreach(var s in snapshots)
        {
            for(int ti=0;ti<s.Indices.Length/3;ti++)
            {
                cancellation.ThrowIfCancellationRequested();ushort aIndex=s.Indices[3*ti],bIndex=s.Indices[3*ti+1],cIndex=s.Indices[3*ti+2];
                var a=V(s.Positions[aIndex]);var b=V(s.Positions[bIndex]);var c=V(s.Positions[cIndex]);var e=Sub(b,a);var f=Sub(c,a);var n=Cross(e,f);double denominator=Dot(n,ray.Direction);
                if(denominator==0)continue; // Parallel, coplanar, degenerate, or rounded-underflow denominator: modern miss.
                double t=Dot(n,Sub(a,ray.Origin))/denominator;
                if(!double.IsFinite(t)||t<ray.MinimumT||t>ray.MaximumT||t>=best)continue;
                var p=new TerrainModernRayVector(ray.Origin.X+t*ray.Direction.X,ray.Origin.Y+t*ray.Direction.Y,ray.Origin.Z+t*ray.Direction.Z);
                // Project onto the largest face-normal component; no epsilon or winding cull.
                int drop=Math.Abs(n.X)>=Math.Abs(n.Y)&&Math.Abs(n.X)>=Math.Abs(n.Z)?0:Math.Abs(n.Y)>=Math.Abs(n.Z)?1:2;
                static (double U,double V) Project(TerrainModernRayVector v,int axis)=>axis==0?(v.Y,v.Z):axis==1?(v.X,v.Z):(v.X,v.Y);
                var ep=Project(e,drop);var fp=Project(f,drop);var pp=Project(Sub(p,a),drop);double area=ep.U*fp.V-ep.V*fp.U;
                if(area==0)continue;double v=(pp.U*fp.V-pp.V*fp.U)/area,w=(ep.U*pp.V-ep.V*pp.U)/area,u=1-v-w;
                Need(double.IsFinite(u)&&double.IsFinite(v)&&double.IsFinite(w),"Late barycentric arithmetic.");
                if(u<0||v<0||w<0||u>1||v>1||w>1)continue;
                Need(Bounded(p,limits.MaximumComponent),"Late intersection position bounds.");
                chosen=s;chosenTriangle=ti;ia=aIndex;ib=bIndex;ic=cIndex;best=t;wa=u;wb=v;wc=w;point=p;
            }
        }
        cancellation.ThrowIfCancellationRequested();if(chosen is null)return null;
        var na=V(chosen.Normals[ia]);var nb=V(chosen.Normals[ib]);var nc=V(chosen.Normals[ic]);var normal=new TerrainModernRayVector(wa*na.X+wb*nb.X+wc*nc.X,wa*na.Y+wb*nb.Y+wc*nc.Y,wa*na.Z+wb*nb.Z+wc*nc.Z);
        double length=Math.Sqrt(Dot(normal,normal));Need(double.IsFinite(length)&&length>limits.MinimumInterpolatedNormalLength,"Late selected supplied-normal failure; no fallback.");
        var normalized=new TerrainModernRayVector(normal.X/length,normal.Y/length,normal.Z/length);
        cancellation.ThrowIfCancellationRequested();return new(Source,chosen.Membership,chosen.Membership.Geometry.PatternIndex,chosenTriangle,ia,ib,ic,best,point,wa,wb,wc,normalized);
    }
}
