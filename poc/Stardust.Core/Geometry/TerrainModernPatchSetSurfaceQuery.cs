namespace Stardust.Core.Geometry;

public sealed record TerrainModernPatchSetSurfaceQueryLimits(int MaximumChunks=16,int MaximumPatches=256,int MaximumTriangles=2048,int MaximumQueries=4096,
    long MaximumBuildCopyBytes=1048576,long MaximumBuildArrayBytes=2097152,long MaximumBuildWork=1048576,
    long MaximumQueryCopyBytes=1048576,long MaximumQueryArrayBytes=1048576,long MaximumQueryWork=1073741824,long MaximumTriangleTests=8388608,
    TerrainPatchSurfaceQueryLimits? ChildLimits=null);
public sealed record TerrainModernPatchSetSurfaceChild(int ChunkIndex,int PatchIndex,TerrainModernHeightPatch Chunk,TerrainPatchAssembly Patch,
    TerrainPatchGeometry Geometry,TerrainPatchSurfaceQuery Query);
public sealed record TerrainModernPatchSetSurfaceHit(int ChunkIndex,int PatchIndex,TerrainModernHeightPatch Chunk,TerrainPatchAssembly Patch,
    TerrainPatchGeometry Geometry,TerrainPatchSurfaceQuery Child,TerrainPatchSurfaceHit Hit);
public readonly record struct TerrainModernPatchSetSurfaceQueryReservation(int Queries,long CopyBytes,long ArrayBytes,long Work,long TriangleTestBound);

/// <summary>Modern ordered queries over an actual retained set; no geometry/normal/admission rebuild.</summary>
public sealed class TerrainModernPatchSetSurfaceQuery
{
    private readonly TerrainModernPatchSetSurfaceChild[] children;
    private readonly TerrainModernPatchSetSurfaceQueryLimits limits;
    private readonly TerrainPatchSurfaceQueryLimits childLimits;
    private TerrainModernPatchSetSurfaceQuery(TerrainModernHeightPatchSet source,TerrainModernPatchSetSurfaceChild[] children,
        TerrainModernPatchSetSurfaceQueryLimits limits,TerrainPatchSurfaceQueryLimits childLimits,int triangles,long childLogical)
    {Source=source;this.children=children;Children=Array.AsReadOnly(children);this.limits=limits;this.childLimits=childLimits;TotalTriangles=triangles;ChildLogicalOwnedBytes=childLogical;}
    public TerrainModernHeightPatchSet Source { get; }
    public IReadOnlyList<TerrainModernPatchSetSurfaceChild> Children { get; }
    public TerrainModernPatchSetSurfaceChild[] CopyChildren()=>(TerrainModernPatchSetSurfaceChild[])children.Clone();
    public int ChunkCount=>Source.Count;
    public int PatchCount=>children.Length;
    public int TotalTriangles { get; }
    public int ChildBuilds=>PatchCount;
    public int NormalBuilds=>0;
    public int GeometryBuilds=>0;
    public int SetAdmissions=>0;
    public bool OriginalCollisionParity=>false;
    public long OwnCopyBytes=>0;
    public long OwnArrayBytes=>512L+128L*PatchCount;
    public long OwnWork=>128L+64L*ChunkCount+64L*PatchCount;
    public long ChildPositionCopyBytes=>108L*PatchCount;
    public long ChildNormalCopyBytes=>108L*PatchCount;
    public long ChildIndexCopyBytes=>6L*TotalTriangles;
    public long ChildCopyBytes=>ChildPositionCopyBytes+ChildNormalCopyBytes+ChildIndexCopyBytes;
    public long ChildAreaArrayBytes=>8L*TotalTriangles;
    public long ChildArrayBytes=>256L*PatchCount+ChildCopyBytes+ChildAreaArrayBytes;
    public long ChildWork=>64L*PatchCount+128L*TotalTriangles;
    public long ChildLogicalOwnedBytes { get; }
    public long AggregateCopyBytes=>OwnCopyBytes+ChildCopyBytes;
    public long AggregateArrayBytes=>OwnArrayBytes+ChildArrayBytes;
    public long AggregateWork=>OwnWork+ChildWork;
    public long BorrowedSetArrayReservation=>Source.OwnArrayBytes+Source.BorrowedArrayReservation;
    private static void Need(bool okay,string why){if(!okay)throw new InvalidDataException(why);}
    private static void Options(TerrainModernPatchSetSurfaceQueryLimits l,TerrainPatchSurfaceQueryLimits c)
    {
        if(l.MaximumChunks is <0 or >16||l.MaximumPatches is <0 or >256||l.MaximumTriangles is <0 or >2048||l.MaximumQueries is <0 or >4096
            ||l.MaximumBuildCopyBytes is <0 or >4194304||l.MaximumBuildArrayBytes is <0 or >8388608||l.MaximumBuildWork is <0 or >16777216
            ||l.MaximumQueryCopyBytes is <0 or >4194304||l.MaximumQueryArrayBytes is <0 or >8388608||l.MaximumQueryWork is <0 or >2147483648L
            ||l.MaximumTriangleTests is <0 or >8388608)throw new ArgumentOutOfRangeException(nameof(l));
        if(!double.IsFinite(c.MaximumCoordinateComponent)||c.MaximumCoordinateComponent<=0||c.MaximumCoordinateComponent>1e9
            ||!double.IsFinite(c.MinimumProjectedDoubleArea)||c.MinimumProjectedDoubleArea<=0||c.MinimumProjectedDoubleArea>1e18
            ||!double.IsFinite(c.MinimumInterpolatedNormalLength)||c.MinimumInterpolatedNormalLength<=0||c.MinimumInterpolatedNormalLength>1e9
            ||c.MaximumQueriesPerBatch is <1 or >65536)throw new ArgumentOutOfRangeException(nameof(c));
    }
    private static void Bounds(System.Numerics.Vector3 v,double cap)
    {Need(float.IsFinite(v.X)&&float.IsFinite(v.Y)&&float.IsFinite(v.Z)&&Math.Abs((double)v.X)<=cap&&Math.Abs((double)v.Y)<=cap&&Math.Abs((double)v.Z)<=cap,"Child position bounds preflight.");}
    public static TerrainModernPatchSetSurfaceQuery Build(TerrainModernHeightPatchSet source,TerrainModernPatchSetSurfaceQueryLimits? limits=null,
        CancellationToken cancellation=default)
    {
        ArgumentNullException.ThrowIfNull(source);var l=limits??new();var c=l.ChildLimits??new();Options(l,c);cancellation.ThrowIfCancellationRequested();
        Need(source.Count<=l.MaximumChunks,"Chunk cap before child copies.");int patches=0,triangles=0;
        foreach(var chunk in source.Patches)
        {
            cancellation.ThrowIfCancellationRequested();Need(ReferenceEquals(chunk.NormalField.Source,chunk.Source)&&ReferenceEquals(chunk.Patch.Coordinates,chunk.Source.Coordinates),"Actual retained chunk identity.");
            foreach(var patch in chunk.Patch.Patches)
            {
                cancellation.ThrowIfCancellationRequested();var geometry=patch.Geometry;int t=geometry.TriangleCount;Need(t is >0 and <=8,"Frozen nine-vertex topology.");
                Bounds(geometry.Minimum,c.MaximumCoordinateComponent);Bounds(geometry.Maximum,c.MaximumCoordinateComponent);
                patches=checked(patches+1);triangles=checked(triangles+t);
            }
        }
        Need(patches==source.TotalPatches&&patches<=l.MaximumPatches&&triangles<=l.MaximumTriangles,"Patch/triangle count caps.");
        long copies=216L*patches+6L*triangles,arrays=512L+600L*patches+14L*triangles,work=128L+64L*source.Count+128L*patches+128L*triangles;
        Need(copies<=l.MaximumBuildCopyBytes&&arrays<=l.MaximumBuildArrayBytes&&work<=l.MaximumBuildWork,"Own plus actual child construction preflight.");
        cancellation.ThrowIfCancellationRequested();var owned=new TerrainModernPatchSetSurfaceChild[patches];int index=0,chunkIndex=0;long childLogical=0;
        foreach(var chunk in source.Patches)
        {
            int patchIndex=0;foreach(var patch in chunk.Patch.Patches)
            {
                cancellation.ThrowIfCancellationRequested();var child=new TerrainPatchSurfaceQuery(patch.Geometry,c);cancellation.ThrowIfCancellationRequested();
                Need(child.TriangleCount==patch.Geometry.TriangleCount,"Actual child triangle count.");childLogical=checked(childLogical+child.LogicalOwnedBytes);
                owned[index++]=new(chunkIndex,patchIndex++,chunk,patch,patch.Geometry,child);
            }
            chunkIndex++;
        }
        Need(index==patches&&childLogical==216L*patches+14L*triangles,"Independently verified child logical payload.");
        cancellation.ThrowIfCancellationRequested();return new(source,owned,l with {},c with {},triangles,childLogical);
    }
    public TerrainModernPatchSetSurfaceQueryReservation GetQueryReservation(int count)
    {
        if(count<0||count>limits.MaximumQueries)throw new ArgumentOutOfRangeException(nameof(count));
        long copy=16L*count,arrays=256L+160L*count,tests=(long)count*TotalTriangles,work=128L+16L*count+(long)count*(16L*PatchCount+64L*TotalTriangles);
        Need(copy<=limits.MaximumQueryCopyBytes&&arrays<=limits.MaximumQueryArrayBytes&&work<=limits.MaximumQueryWork&&tests<=limits.MaximumTriangleTests,"Query count/copy/array/triangle-work preflight.");
        return new(count,copy,arrays,work,tests);
    }
    private void Location(TerrainPatchSurfaceLocation p)
    {if(!double.IsFinite(p.X)||!double.IsFinite(p.Z)||Math.Abs(p.X)>childLimits.MaximumCoordinateComponent||Math.Abs(p.Z)>childLimits.MaximumCoordinateComponent)throw new ArgumentOutOfRangeException(nameof(p));}
    private TerrainModernPatchSetSurfaceHit? QueryValidated(TerrainPatchSurfaceLocation p,CancellationToken cancellation)
    {
        foreach(var child in children)
        {
            cancellation.ThrowIfCancellationRequested();var hit=child.Query.Query(p.X,p.Z);cancellation.ThrowIfCancellationRequested();
            if(hit.HasValue)return new(child.ChunkIndex,child.PatchIndex,child.Chunk,child.Patch,child.Geometry,child.Query,hit.Value);
        }
        cancellation.ThrowIfCancellationRequested();return null;
    }
    public TerrainModernPatchSetSurfaceHit? Query(double x,double z,CancellationToken cancellation=default)
    {
        _=GetQueryReservation(1);var point=new TerrainPatchSurfaceLocation(x,z);Location(point);cancellation.ThrowIfCancellationRequested();
        var result=QueryValidated(point,cancellation);cancellation.ThrowIfCancellationRequested();return result;
    }
    public IReadOnlyList<TerrainModernPatchSetSurfaceHit?> QueryBatch(ReadOnlySpan<TerrainPatchSurfaceLocation> locations,CancellationToken cancellation=default)
    {
        _=GetQueryReservation(locations.Length);cancellation.ThrowIfCancellationRequested();
        foreach(var point in locations){cancellation.ThrowIfCancellationRequested();Location(point);}
        var owned=locations.ToArray();foreach(var point in owned){cancellation.ThrowIfCancellationRequested();Location(point);}
        cancellation.ThrowIfCancellationRequested();var results=new TerrainModernPatchSetSurfaceHit?[owned.Length];
        for(int i=0;i<results.Length;i++){cancellation.ThrowIfCancellationRequested();results[i]=QueryValidated(owned[i],cancellation);}
        cancellation.ThrowIfCancellationRequested();return Array.AsReadOnly(results);
    }
}
