using System.Buffers.Binary;

namespace Stardust.Core.Assets;

public sealed record SkeletalAlbedoProgram(bool UsesAlphaHueMask,IffNode Metadata);

/// <summary>Recognizes two deployed PS1.1 programs' albedo factors while retaining their original lighting program as data.</summary>
public static class SkeletalAlbedoProgramDecoder
{
    private static readonly uint[] SingleFactorInstructions={0x42,0xB00F0000,0x42,0xB00F0001,
        0x05,0x80070000,0xB0E40000,0x90E40000,0x05,0x80070001,0xB0FF0001,0x90E40001,
        0x04,0x80070000,0x80E40000,0xA0E40003,0x80E40001,0x40000001,0x80080000,0xA0FF0007,0xFFFF};
    private static readonly uint[] AlphaMaskInstructions={0x42,0xB00F0000,0x42,0xB00F0001,
        0x05,0x80070000,0xB0E40000,0x90E40000,0x12,0x80070001,0xB0FF0000,0xA0E40004,0xA0E40003,
        0x05,0xB0070000,0xB0FF0001,0x90E40001,0x04,0x80070000,0x80E40000,0x80E40001,0xB0E40000,
        0x40000001,0x80080000,0xA0FF0007,0xFFFF};

    public static SkeletalAlbedoProgram Decode(ReadOnlySpan<byte> source)
    {
        var root=IffDocument.Parse(source,new(MaximumBytes:65536,MaximumNodes:16,MaximumDepth:4)).Root;
        Need(root.IsForm("PSHP")&&root.Children.Count==1&&root.Children[0].IsForm("0000"));var parts=root.Children[0].Children;
        Need(parts.Count==2&&parts[0].Tag=="PSRC"&&parts[1].Tag=="PEXE");var text=parts[0].Payload.Span;
        Need(text.Length>0&&text[^1]==0&&text[..^1].IndexOf((byte)0)<0);
        var code=parts[1].Payload.Span;Need(code.Length>=8&&code.Length%4==0&&Word(code,0)==0xFFFF0101);int at=1,count=code.Length/4;
        // Only leading bounded COMMENT blocks (such as CTAB) are ignored, never instruction words.
        while(at<count&&(Word(code,at)&65535)==65534)
        {uint comment=Word(code,at);Need((comment&0x80000000)==0);int length=(int)((comment>>16)&32767);Need(length<=count-at-1);at+=length+1;}
        var words=new uint[count-at];for(int i=0;i<words.Length;i++)words[i]=Word(code,at+i);
        bool mask=words.AsSpan().SequenceEqual(AlphaMaskInstructions);
        Need(mask||words.AsSpan().SequenceEqual(SingleFactorInstructions));return new(mask,root);
    }
    private static uint Word(ReadOnlySpan<byte> bytes,int index)=>BinaryPrimitives.ReadUInt32LittleEndian(bytes[(index*4)..]);
    private static void Need(bool value){if(!value)throw new InvalidDataException("Unsupported skeletal albedo program semantics.");}
}
