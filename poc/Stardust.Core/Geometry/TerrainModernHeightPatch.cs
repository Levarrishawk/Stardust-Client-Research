using System.Numerics;

namespace Stardust.Core.Geometry;

public sealed record TerrainModernHeightPatchLimits(int MaximumAxisSamples=32,int MaximumCells=1024,int MaximumPatches=64,
    int MaximumSelectedSamples=576,long MaximumCopyBytes=1048576,long MaximumArrayBytes=2097152,long MaximumWork=4194304,
    TerrainModernHeightNormalLimits? NormalLimits=null,TerrainCoordinateHeightPatchLimits? PatchLimits=null);

/// <summary>Bounded engine-neutral modern normal composition from an actual immutable source patch.</summary>
public sealed class TerrainModernHeightPatch
{
    private TerrainModernHeightPatch(TerrainCoordinateHeightPatch source,TerrainModernHeightNormals normals,TerrainCoordinateHeightPatch patch)
    {Source=source;NormalField=normals;Patch=patch;}
    public TerrainCoordinateHeightPatch Source { get; }
    public TerrainModernHeightNormals NormalField { get; }
    public TerrainCoordinateHeightPatch Patch { get; }
    public TerrainModernNormalPolicy Policy=>TerrainModernNormalPolicy.ModernBinary64CentralDifference;
    public bool OriginalNormalParity=>false;
    public bool OriginalMaterialParity=>false;
    public bool FinalBuilderSupportsCancellation=>false;
    public int NormalBuilds=>1;
    public int FinalPatchBuilds=>1;
    public int Width=>Source.GridWidth;
    public int Count=>Width*Width;
    public int PatchCount=>Patch.Patches.Count;
    public int SelectedSamples=>9*PatchCount;
    public long OwnCopyBytes=>36L*Count;
    public long OwnArrayBytes=>256L+36L*Count;
    public long OwnWork=>128L+4L*Width+16L*Count+4L*PatchCount;
    public long NormalCopyBytes=>NormalField.LogicalCopyBytes;
    public long NormalArrayBytes=>NormalField.LogicalArrayBytes;
    public long NormalWork=>NormalField.LogicalWork;
    public long FinalReservedCopyBytes=>20L*Count+1024L*PatchCount;
    public long FinalReservedArrayBytes=>256L+36L*Count+2048L*PatchCount;
    public long FinalReservedWork=>256L+96L*Count+4096L*PatchCount;
    public long AggregateCopyBytes=>OwnCopyBytes+NormalCopyBytes+FinalReservedCopyBytes;
    public long AggregateArrayBytes=>OwnArrayBytes+NormalArrayBytes+FinalReservedArrayBytes;
    public long AggregateWork=>OwnWork+NormalWork+FinalReservedWork;
    public uint[] CopyInputHeightBits()=>Patch.CopyInputHeightBits();
    public uint[] CopyPositionHeightBits()=>Patch.CopyPositionHeightBits();
    public uint[] CopyArgbColors()=>Patch.CopyArgbColors();
    public Vector3[] CopyNormals()=>Patch.CopyNormals();
    private static void Need(bool okay,string why){if(!okay)throw new InvalidDataException(why);}
    public static TerrainModernHeightPatch Build(TerrainCoordinateHeightPatch source,TerrainModernNormalPolicy policy,
        TerrainModernHeightPatchLimits? limits=null,CancellationToken cancellation=default)
    {
        ArgumentNullException.ThrowIfNull(source);limits??=new();var normal=limits.NormalLimits??new();var final=limits.PatchLimits??new();
        if(policy!=TerrainModernNormalPolicy.ModernBinary64CentralDifference)throw new ArgumentOutOfRangeException(nameof(policy));
        if(limits.MaximumAxisSamples is <0 or >32||limits.MaximumCells is <0 or >1024||limits.MaximumPatches is <0 or >256
            ||limits.MaximumSelectedSamples is <0 or >2304||limits.MaximumCopyBytes is <0 or >4194304
            ||limits.MaximumArrayBytes is <0 or >16777216||limits.MaximumWork is <0 or >33554432)throw new ArgumentOutOfRangeException(nameof(limits));
        if(normal.MaximumAxisSamples is <0 or >32||normal.MaximumCells is <0 or >1024||normal.MaximumCopyBytes is <0 or >1048576
            ||normal.MaximumArrayBytes is <0 or >2097152||normal.MaximumWork is <0 or >4194304)throw new ArgumentOutOfRangeException(nameof(limits));
        if(final.MaximumGridSamples is <1 or >1048576||final.MaximumPatches is <1 or >1024||final.MaximumSelectedSamples is <1 or >9216)
            throw new ArgumentOutOfRangeException(nameof(limits));
        int w=source.GridWidth,t=source.CellsPerSide,b=source.SampleBorder;
        Need(w is >=2 and <=32&&w<=limits.MaximumAxisSamples&&t is >=1 and <=32&&b>=0&&(long)b+2L*t+1<=w,"Bounded complete padded source required.");
        int n=checked(w*w),p=checked(t*t),selected=checked(9*p);
        Need(n<=limits.MaximumCells&&p<=limits.MaximumPatches&&selected<=limits.MaximumSelectedSamples&&source.Patches.Count==p,"Aggregate own counts.");
        Need((uint)source.SeamMask<=15&&float.IsFinite(source.UvScale)&&source.UvScale>0&&source.UvScale<=16,"Source seam/UV options.");
        long nc=8L*w+4L*n,na=256L+8L*w+16L*n,nw=128L+4L*w+80L*n;
        Need(w<=normal.MaximumAxisSamples&&n<=normal.MaximumCells&&nc<=normal.MaximumCopyBytes&&na<=normal.MaximumArrayBytes&&nw<=normal.MaximumWork,"Normal child preflight before exports.");
        Need(n<=final.MaximumGridSamples&&p<=final.MaximumPatches&&selected<=final.MaximumSelectedSamples,"Final child count preflight before exports.");
        long copies=60L*n+8L*w+1024L*p,arrays=768L+8L*w+88L*n+2048L*p,work=512L+8L*w+192L*n+4100L*p;
        Need(copies<=limits.MaximumCopyBytes&&arrays<=limits.MaximumArrayBytes&&work<=limits.MaximumWork,"Aggregate own plus child copy/array/conservative-work preflight.");
        cancellation.ThrowIfCancellationRequested();
        var position=source.Coordinates.Position;
        Need(position.FirstAxisBits.Count==w&&position.SecondAxisBits.Count==w,"Complete source position axes.");
        for(int i=0;i<w;i++)
        {
            cancellation.ThrowIfCancellationRequested();float x=BitConverter.UInt32BitsToSingle(position.FirstAxisBits[i]),z=BitConverter.UInt32BitsToSingle(position.SecondAxisBits[i]);
            Need(float.IsFinite(x)&&float.IsFinite(z)&&Math.Abs(x)<=1000000&&Math.Abs(z)<=1000000,"Finite eligible source position axes.");
            if(i>0)Need(x>BitConverter.UInt32BitsToSingle(position.FirstAxisBits[i-1])&&z>BitConverter.UInt32BitsToSingle(position.SecondAxisBits[i-1]),"Strict source position spans.");
        }
        uint[] raw=source.CopyInputHeightBits(),stored=source.CopyPositionHeightBits(),colors=source.CopyArgbColors();
        Need(raw.Length==n&&stored.Length==n&&colors.Length==n,"Complete owned source snapshots.");
        for(int i=0;i<n;i++)
        {
            cancellation.ThrowIfCancellationRequested();float h=BitConverter.UInt32BitsToSingle(raw[i]),y=BitConverter.UInt32BitsToSingle(stored[i]);
            Need(float.IsFinite(h)&&Math.Abs(h)<=1000000&&float.IsFinite(y),"Finite owned source channels.");
        }
        var normals=TerrainModernHeightNormals.Build(source,policy,normal,cancellation);
        var values=normals.CopyNormals();Need(values.Length==n,"Complete actual derived normal export.");
        cancellation.ThrowIfCancellationRequested();
        // Frozen final builder has count caps but no byte/work policy or cancellation hook.
        // Pass raw heights: passing stored heights would apply VerticalOriginBits twice.
        var patch=TerrainCoordinateHeightPatch.Build(source.Coordinates,raw,values,colors,source.SeamMask,source.UvScale,final);
        cancellation.ThrowIfCancellationRequested();
        Need(patch.CopyInputHeightBits().AsSpan().SequenceEqual(raw)&&patch.CopyPositionHeightBits().AsSpan().SequenceEqual(stored)
            &&patch.CopyArgbColors().AsSpan().SequenceEqual(colors),"Final raw/stored height and full ARGB preservation.");
        cancellation.ThrowIfCancellationRequested();return new(source,normals,patch);
    }
}
