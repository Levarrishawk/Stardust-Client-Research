using System.Numerics;

namespace Stardust.Core.Geometry;

public enum TerrainModernPatchEdgePolicy { IndependentSuppliedPatches,RequireBitIdenticalActiveSharedEdges }
public enum TerrainModernPatchNormalPolicy { IgnoreSharedNormals,RequireBitIdenticalSharedNormals }
public sealed record TerrainModernHeightPatchSetLimits(int MaximumChunks=16,int MaximumAxisSamples=32,int MaximumTotalCells=4096,
    int MaximumTotalPatches=256,int MaximumSelectedSamples=2304,long MaximumCopyBytes=262144,long MaximumArrayBytes=1048576,
    long MaximumWork=2097152,long MaximumBorrowedCopyReservation=4194304,long MaximumBorrowedArrayReservation=8388608,
    long MaximumBorrowedWorkReservation=16777216);

/// <summary>Owned list admission of actual immutable receipts; no normal or geometry rebuild.</summary>
public sealed class TerrainModernHeightPatchSet
{
    private readonly TerrainModernHeightPatch[] patches;
    private TerrainModernHeightPatchSet(TerrainModernHeightPatch[] patches,TerrainModernPatchEdgePolicy edge,TerrainModernPatchNormalPolicy normal,
        int cells,int totalPatches,int selected,long copies,long arrays,long work,long borrowedCopies,long borrowedArrays,long borrowedWork,
        int edges,int corners,int comparisons)
    {
        this.patches=patches;Patches=Array.AsReadOnly(patches);EdgePolicy=edge;NormalPolicy=normal;TotalCells=cells;TotalPatches=totalPatches;
        SelectedSamples=selected;OwnCopyBytes=copies;OwnArrayBytes=arrays;OwnWork=work;BorrowedCopyReservation=borrowedCopies;
        BorrowedArrayReservation=borrowedArrays;BorrowedWorkReservation=borrowedWork;SharedEdgePairs=edges;SharedCornerPairs=corners;SharedSampleComparisons=comparisons;
    }
    public IReadOnlyList<TerrainModernHeightPatch> Patches { get; }
    public TerrainModernHeightPatch[] CopyPatches()=>(TerrainModernHeightPatch[])patches.Clone();
    public TerrainModernPatchEdgePolicy EdgePolicy { get; }
    public TerrainModernPatchNormalPolicy NormalPolicy { get; }
    public int Count=>patches.Length;
    public int TotalCells { get; }
    public int TotalPatches { get; }
    public int SelectedSamples { get; }
    public long OwnCopyBytes { get; }
    public long OwnArrayBytes { get; }
    public long OwnWork { get; }
    public long BorrowedCopyReservation { get; }
    public long BorrowedArrayReservation { get; }
    public long BorrowedWorkReservation { get; }
    public int SharedEdgePairs { get; }
    public int SharedCornerPairs { get; }
    public int SharedSampleComparisons { get; }
    public int NormalBuilds=>0;
    public int GeometryBuilds=>0;
    public bool OriginalNormalParity=>false;
    public bool OriginalTerrainParity=>false;
    public bool SignedZeroHeightWordsAreDistinct=>true;
    private static void Need(bool okay,string why){if(!okay)throw new InvalidDataException(why);}
    private static float F(uint bits)=>BitConverter.UInt32BitsToSingle(bits);
    private static uint B(float value)=>BitConverter.SingleToUInt32Bits(value);
    private readonly record struct Region(TerrainModernHeightPatch Value,int Begin,int End)
    {
        public TerrainSampleCoordinateAxes Axes=>Value.Source.Coordinates.Position;
        public float X0=>F(Axes.FirstAxisBits[Begin]);public float X1=>F(Axes.FirstAxisBits[End]);
        public float Z0=>F(Axes.SecondAxisBits[Begin]);public float Z1=>F(Axes.SecondAxisBits[End]);
    }
    private static Region R(TerrainModernHeightPatch p)=>new(p,p.Source.SampleBorder,p.Source.SampleBorder+2*p.Source.CellsPerSide);
    private readonly record struct AdmissionPlan(int Cells,int Total,int Selected,long Copies,long Arrays,long Work,long BorrowedCopies,long BorrowedArrays,long BorrowedWork);
    private static AdmissionPlan Preflight(ReadOnlySpan<TerrainModernHeightPatch> supplied,TerrainModernPatchEdgePolicy edgePolicy,
        TerrainModernHeightPatchSetLimits limits,CancellationToken cancellation)
    {
        Need(supplied.Length<=limits.MaximumChunks,"Chunk cap.");cancellation.ThrowIfCancellationRequested();
        int cells=0,total=0,selected=0,sumWidth=0,maxWidth=0;long bc=0,ba=0,bw=0;
        foreach(var value in supplied)
        {
            cancellation.ThrowIfCancellationRequested();ArgumentNullException.ThrowIfNull(value);
            var source=value.Source;int w=source.GridWidth,b=source.SampleBorder,t=source.CellsPerSide;
            Need(w is >=2 and <=32&&w<=limits.MaximumAxisSamples&&b>=0&&t>0&&(long)b+2L*t+1<=w,"Complete active source bounds.");
            Need(ReferenceEquals(value.NormalField.Source,source)&&ReferenceEquals(value.Patch.Coordinates,source.Coordinates)
                &&value.NormalField.Count==w*w&&value.Patch.Patches.Count==t*t&&value.Policy==TerrainModernNormalPolicy.ModernBinary64CentralDifference,"Actual source/normal/final identity.");
            Need(source.Coordinates.Position.FirstAxisBits.Count==w&&source.Coordinates.Position.SecondAxisBits.Count==w,"Complete stored axes.");
            Need((uint)source.SeamMask<=15&&float.IsFinite(source.UvScale)&&source.UvScale>0&&source.UvScale<=16
                &&value.Patch.SeamMask==source.SeamMask&&B(value.Patch.UvScale)==B(source.UvScale),"Actual seam/UV options.");
            var axes=source.Coordinates.Position;
            for(int i=0;i<w;i++)
            {
                float x=F(axes.FirstAxisBits[i]),z=F(axes.SecondAxisBits[i]);Need(float.IsFinite(x)&&float.IsFinite(z)&&Math.Abs(x)<=1000000&&Math.Abs(z)<=1000000,"Eligible actual stored axes.");
                if(i>0)Need(x>F(axes.FirstAxisBits[i-1])&&z>F(axes.SecondAxisBits[i-1]),"Strict actual axes.");
            }
            cells=checked(cells+w*w);total=checked(total+t*t);selected=checked(selected+9*t*t);sumWidth+=w;maxWidth=Math.Max(maxWidth,w);
            bc=checked(bc+value.AggregateCopyBytes);ba=checked(ba+value.AggregateArrayBytes);bw=checked(bw+value.AggregateWork);
        }
        Need(cells<=limits.MaximumTotalCells&&total<=limits.MaximumTotalPatches&&selected<=limits.MaximumSelectedSamples,"Total count caps.");
        int k=supplied.Length,pairs=k*(k-1)/2;bool strict=edgePolicy==TerrainModernPatchEdgePolicy.RequireBitIdenticalActiveSharedEdges;
        long channels=strict?4L*cells:0,copies=8L*k+channels,arrays=256L+128L*k+channels,work=128L+128L*k+16L*sumWidth+channels+(long)pairs*(256L+64L*maxWidth);
        Need(copies<=limits.MaximumCopyBytes&&arrays<=limits.MaximumArrayBytes&&work<=limits.MaximumWork,"Owned list/snapshot/pair/edge preflight.");
        Need(bc<=limits.MaximumBorrowedCopyReservation&&ba<=limits.MaximumBorrowedArrayReservation&&bw<=limits.MaximumBorrowedWorkReservation,"Borrowed historical child reservation caps.");
        // Pairwise identity/interior preflight uses borrowed immutable metadata, before arrays/exports.
        for(int i=0;i<k;i++)for(int j=i+1;j<k;j++)
        {
            cancellation.ThrowIfCancellationRequested();var left=supplied[i];var right=supplied[j];ArgumentNullException.ThrowIfNull(left);ArgumentNullException.ThrowIfNull(right);
            Need(left.Source.Coordinates.Request!=right.Source.Coordinates.Request,"Unique complete chunk-request key.");
            var a=R(left);var b=R(right);Need(!(Math.Max(a.X0,b.X0)<Math.Min(a.X1,b.X1)&&Math.Max(a.Z0,b.Z0)<Math.Min(a.Z1,b.Z1)),"Overlapping active interiors.");
        }
        return new(cells,total,selected,copies,arrays,work,bc,ba,bw);
    }
    public static TerrainModernHeightPatchSet Build(ReadOnlySpan<TerrainModernHeightPatch> supplied,TerrainModernPatchEdgePolicy edgePolicy,
        TerrainModernPatchNormalPolicy normalPolicy,TerrainModernHeightPatchSetLimits? limits=null,CancellationToken cancellation=default)
    {
        limits??=new();
        if((uint)edgePolicy>1||(uint)normalPolicy>1||edgePolicy==TerrainModernPatchEdgePolicy.IndependentSuppliedPatches&&normalPolicy!=TerrainModernPatchNormalPolicy.IgnoreSharedNormals)
            throw new ArgumentOutOfRangeException(nameof(edgePolicy));
        if(limits.MaximumChunks is <0 or >16||limits.MaximumAxisSamples is <0 or >32||limits.MaximumTotalCells is <0 or >4096
            ||limits.MaximumTotalPatches is <0 or >256||limits.MaximumSelectedSamples is <0 or >2304||limits.MaximumCopyBytes is <0 or >1048576
            ||limits.MaximumArrayBytes is <0 or >4194304||limits.MaximumWork is <0 or >16777216||limits.MaximumBorrowedCopyReservation is <0 or >16777216
            ||limits.MaximumBorrowedArrayReservation is <0 or >33554432||limits.MaximumBorrowedWorkReservation is <0 or >67108864)
            throw new ArgumentOutOfRangeException(nameof(limits));
        var initial=Preflight(supplied,edgePolicy,limits,cancellation);
        cancellation.ThrowIfCancellationRequested();var owned=supplied.ToArray();
        // Capture is not atomic against arbitrary caller mutation. Revalidate the actual captured list
        // before any height exports; a changed reservation rejects the candidate.
        var captured=Preflight(owned,edgePolicy,limits,cancellation);Need(captured==initial,"Captured candidate reservation changed.");
        int k=owned.Length,cells=captured.Cells,total=captured.Total,selected=captured.Selected;
        long copies=captured.Copies,arrays=captured.Arrays,work=captured.Work,bc=captured.BorrowedCopies,ba=captured.BorrowedArrays,bw=captured.BorrowedWork;
        bool strict=edgePolicy==TerrainModernPatchEdgePolicy.RequireBitIdenticalActiveSharedEdges;var heights=new uint[k][];
        if(strict)for(int i=0;i<k;i++){cancellation.ThrowIfCancellationRequested();heights[i]=owned[i].Patch.CopyPositionHeightBits();Need(heights[i].Length==owned[i].Count,"Complete actual stored heights.");}
        int edges=0,corners=0,comparisons=0;
        for(int i=0;i<k;i++)for(int j=i+1;j<k;j++)
        {
            cancellation.ThrowIfCancellationRequested();var a=R(owned[i]);var b=R(owned[j]);
            float xlo=Math.Max(a.X0,b.X0),xhi=Math.Min(a.X1,b.X1),zlo=Math.Max(a.Z0,b.Z0),zhi=Math.Min(a.Z1,b.Z1);
            bool xtouch=a.X1==b.X0||b.X1==a.X0,ztouch=a.Z1==b.Z0||b.Z1==a.Z0;
            if(xtouch&&zlo<zhi){edges++;if(strict)Edge(a,b,i,j,true,zlo,zhi);}
            else if(ztouch&&xlo<xhi){edges++;if(strict)Edge(a,b,i,j,false,xlo,xhi);}
            else if(xtouch&&ztouch){corners++;if(strict)Point(a,b,i,j,a.X1==b.X0?a.End:a.Begin,a.Z1==b.Z0?a.End:a.Begin,a.X1==b.X0?b.Begin:b.End,a.Z1==b.Z0?b.Begin:b.End);}
        }
        cancellation.ThrowIfCancellationRequested();return new(owned,edgePolicy,normalPolicy,cells,total,selected,copies,arrays,work,bc,ba,bw,edges,corners,comparisons);
        void Point(Region a,Region b,int ai,int bi,int ac,int ar,int bcol,int br)
        {
            cancellation.ThrowIfCancellationRequested();Need(a.Axes.FirstAxisBits[ac]==b.Axes.FirstAxisBits[bcol]&&a.Axes.SecondAxisBits[ar]==b.Axes.SecondAxisBits[br],"Bit-identical shared position words.");
            int ia=ar*a.Value.Width+ac,ib=br*b.Value.Width+bcol;Need(heights[ai][ia]==heights[bi][ib],"Bit-identical stored shared height words, signed zero distinct.");
            if(normalPolicy==TerrainModernPatchNormalPolicy.RequireBitIdenticalSharedNormals)
            {
                Vector3 n=a.Value.NormalField.Normals[ia],m=b.Value.NormalField.Normals[ib];Need(B(n.X)==B(m.X)&&B(n.Y)==B(m.Y)&&B(n.Z)==B(m.Z),"Bit-identical actual shared normal words.");
            }
            comparisons++;
        }
        void Edge(Region a,Region b,int ai,int bi,bool vertical,float lo,float hi)
        {
            var ax=vertical?a.Axes.SecondAxisBits:a.Axes.FirstAxisBits;var bx=vertical?b.Axes.SecondAxisBits:b.Axes.FirstAxisBits;
            int u=a.Begin,v=b.Begin;while(u<=a.End&&F(ax[u])<lo)u++;while(v<=b.End&&F(bx[v])<lo)v++;
            Need(u<=a.End&&v<=b.End&&F(ax[u])==lo&&F(bx[v])==lo,"Shared overlap endpoint samples required.");float last=lo;
            while(u<=a.End&&v<=b.End&&F(ax[u])<=hi&&F(bx[v])<=hi)
            {
                cancellation.ThrowIfCancellationRequested();Need(ax[u]==bx[v],"Compatible shared active sample spacing.");last=F(ax[u]);
                if(vertical)Point(a,b,ai,bi,a.X1==b.X0?a.End:a.Begin,u,a.X1==b.X0?b.Begin:b.End,v);
                else Point(a,b,ai,bi,u,a.Z1==b.Z0?a.End:a.Begin,v,a.Z1==b.Z0?b.Begin:b.End);
                u++;v++;
            }
            Need(last==hi&&(u>a.End||F(ax[u])>hi)&&(v>b.End||F(bx[v])>hi),"Complete shared overlap sample agreement.");
        }
    }
}
