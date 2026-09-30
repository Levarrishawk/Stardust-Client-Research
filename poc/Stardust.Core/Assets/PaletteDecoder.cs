using System.Buffers.Binary;

namespace Stardust.Core.Assets;

public readonly record struct PaletteEntry(byte Red,byte Green,byte Blue,byte RawFlags)
{
    public uint OpaqueArgb=>0xFF000000u|((uint)Red<<16)|((uint)Green<<8)|Blue;
}
public sealed record PaletteDescription(ushort Version,IReadOnlyList<PaletteEntry> Entries,
    ReadOnlyMemory<byte> IgnoredTrailingBytes,IReadOnlyList<string> Diagnostics);
public sealed record PaletteColorSelection(int RequestedIndex,int SelectedIndex,uint OpaqueArgb,byte RawFlags);

/// <summary>Owned bounded legacy RIFF PAL data, flags preserved but alpha forced opaque; declared trailing bytes tolerated.</summary>
public static class PaletteDecoder
{
    public static PaletteDescription Decode(ReadOnlySpan<byte> source)
    {
        Require(source.Length is >=24 and <=4120,"Palette physical size exceeded.");
        Require(source[..4].SequenceEqual("RIFF"u8) && source.Slice(8,4).SequenceEqual("PAL "u8) && source.Slice(12,4).SequenceEqual("data"u8),"Unsupported palette envelope.");
        uint riff=BinaryPrimitives.ReadUInt32LittleEndian(source[4..]),data=BinaryPrimitives.ReadUInt32LittleEndian(source[16..]);
        ushort version=BinaryPrimitives.ReadUInt16LittleEndian(source[20..]),count=BinaryPrimitives.ReadUInt16LittleEndian(source[22..]);
        Require((version>>8)==3 && count<=1024,"Unsupported palette version/count.");
        Require((long)data==4L+count*4L && (long)riff==data+12L && data+20L<=source.Length,"Invalid palette declared size/truncation.");
        PaletteEntry[] entries=new PaletteEntry[count];
        for(int i=0;i<count;i++){int at=24+i*4;entries[i]=new(source[at],source[at+1],source[at+2],source[at+3]);}
        string[] diagnostics=(version&255)==0?Array.Empty<string>():new[]{$"NonzeroPaletteVersionLowByte:{version&255}"};
        return new(version,Array.AsReadOnly(entries),source[(int)(data+20)..].ToArray(),Array.AsReadOnly(diagnostics));
    }
    public static PaletteColorSelection? Select(PaletteDescription palette,int index)
    {
        ArgumentNullException.ThrowIfNull(palette);
        if(palette.Entries.Count==0)return null;
        int selected=index<0 || index>=palette.Entries.Count?0:index;
        PaletteEntry entry=palette.Entries[selected];return new(index,selected,entry.OpaqueArgb,entry.RawFlags);
    }
    public static PaletteDescription CreateMissingFallback()=>new(0x0300,
        Array.AsReadOnly(new[]{new PaletteEntry(0,255,255,0)}),ReadOnlyMemory<byte>.Empty,
        Array.AsReadOnly(new[]{"MissingPaletteFallback:OpaqueCyan"}));
    private static void Require(bool condition,string message){if(!condition)throw new InvalidDataException(message);}
}
