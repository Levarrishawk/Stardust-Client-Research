using System.Buffers.Binary;

namespace Stardust.Core.Assets;

public enum TerrainMapMaskKind { Wmap, Smap }
public sealed record TerrainMapMaskLimits(int MaximumInputBytes=16777216, int MaximumBitDimension=8192,
    int MaximumBytesPerRow=1048576, int MaximumMaskBytes=8388608);

/// <summary>Immutable map-object version form and raw grid inputs; no world-coordinate conversion or terrain generation.</summary>
public sealed class TerrainMapMasks
{
    private readonly byte[] wmap, smap;
    private TerrainMapMasks(string version,uint world,uint meters,uint row,uint dimension,byte[] water,byte[] seed)
    { Version=version;WorldSizeBits=world;MetersPerBitBits=meters;BytesPerRow=row;BitDimension=dimension;wmap=water;smap=seed; }
    public string Version { get; }
    public bool IsEmpty=>Version=="0000";
    public uint WorldSizeBits { get; }
    public uint MetersPerBitBits { get; }
    public float WorldSize=>BitConverter.UInt32BitsToSingle(WorldSizeBits);
    public float MetersPerBit=>BitConverter.UInt32BitsToSingle(MetersPerBitBits);
    public uint BytesPerRow { get; }
    public uint BitDimension { get; }
    public int BytesPerMask=>wmap.Length;
    public byte[] CopyMask(TerrainMapMaskKind kind)=>(byte[])Mask(kind).Clone();
    public byte[] CopyDataRecord()
    {
        if(IsEmpty)return Array.Empty<byte>();
        var result=new byte[16];BinaryPrimitives.WriteUInt32LittleEndian(result,WorldSizeBits);BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(4),MetersPerBitBits);
        BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(8),BytesPerRow);BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(12),BitDimension);return result;
    }
    public static TerrainMapMasks Decode(ReadOnlySpan<byte> versionForm,TerrainMapMaskLimits? limits=null)
    {
        limits??=new();
        if(limits.MaximumInputBytes is <12 or >67108864 || limits.MaximumBitDimension is <1 or >32768 ||
            limits.MaximumBytesPerRow is <1 or >1048576 || limits.MaximumMaskBytes is <0 or >33554432)throw new ArgumentOutOfRangeException(nameof(limits));
        var root=IffDocument.Parse(versionForm,new(limits.MaximumInputBytes,8,3)).Root;
        if(root.IsForm("0000")) { Need(root.Children.Count==0,"Empty map version has no records.");return new("0000",0,0,0,0,Array.Empty<byte>(),Array.Empty<byte>()); }
        Need(root.IsForm("0001") && root.Children.Count==3,"Map version requires DATA/WMAP/SMAP.");
        var data=root.Children[0];var water=root.Children[1];var seed=root.Children[2];
        Need(data.Tag=="DATA" && data.FormType is null && data.Payload.Length==16 && water.Tag=="WMAP" && water.FormType is null && seed.Tag=="SMAP" && seed.FormType is null,"Exact map record order/tags/metadata extent required.");
        var raw=data.Payload.Span;uint world=U32(raw),meters=U32(raw[4..]),row=U32(raw[8..]),dimension=U32(raw[12..]);
        float worldValue=BitConverter.UInt32BitsToSingle(world),metersValue=BitConverter.UInt32BitsToSingle(meters);
        Need(float.IsFinite(worldValue)&&worldValue>0 && float.IsFinite(metersValue)&&metersValue>0,"Modern finite positive map metadata required.");
        Need(row>0 && row<=limits.MaximumBytesPerRow && dimension>0 && dimension<=limits.MaximumBitDimension,"Modern map row/dimension budget.");
        Need((long)row*8>=dimension,"Map row cannot address grid width.");
        long length=checked((long)row*dimension);Need(length<=limits.MaximumMaskBytes && length<=int.MaxValue,"Map multiplication/byte budget.");
        Need(water.Payload.Length==length && seed.Payload.Length==length,"Exact mask chunk lengths required.");
        return new("0001",world,meters,row,dimension,water.Payload.ToArray(),seed.Payload.ToArray());
    }
    /// <summary>Raw unsigned grid axes represented by signed caller integers; outside the grid returns false.</summary>
    public bool GetBit(TerrainMapMaskKind kind,int x,int y)
    {
        var mask=Mask(kind);if(x<0||y<0||x>=BitDimension||y>=BitDimension)return false;
        return (mask[checked((int)((long)y*BytesPerRow+(x>>3)))]&(1<<(x&7)))!=0;
    }
    /// <summary>Normalize inclusive integer corners, clip to the grid, and preflight the entire clipped area before scanning.</summary>
    public bool AnyBit(TerrainMapMaskKind kind,int x0,int y0,int x1,int y1,int maximumCells=65536)
    {
        _=Mask(kind);if(maximumCells is <1 or >1048576)throw new ArgumentOutOfRangeException(nameof(maximumCells));
        if(IsEmpty)return false;
        int last=checked((int)BitDimension-1),left=Math.Max(0,Math.Min(x0,x1)),right=Math.Min(last,Math.Max(x0,x1)),top=Math.Max(0,Math.Min(y0,y1)),bottom=Math.Min(last,Math.Max(y0,y1));
        if(left>right||top>bottom)return false;
        long cells=checked((long)(right-left+1)*(bottom-top+1));Need(cells<=maximumCells,"Clipped rectangle query work budget.");
        for(int y=top;y<=bottom;y++)for(int x=left;x<=right;x++)if(GetBit(kind,x,y))return true;
        return false;
    }
    private byte[] Mask(TerrainMapMaskKind kind)=>kind switch {TerrainMapMaskKind.Wmap=>wmap,TerrainMapMaskKind.Smap=>smap,_=>throw new ArgumentOutOfRangeException(nameof(kind))};
    private static uint U32(ReadOnlySpan<byte> bytes)=>BinaryPrimitives.ReadUInt32LittleEndian(bytes);
    private static void Need(bool value,string why) { if(!value)throw new InvalidDataException(why); }
}
