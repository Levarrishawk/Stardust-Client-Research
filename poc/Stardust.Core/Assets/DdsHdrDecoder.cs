using System.Buffers.Binary;
using BCnEncoder.Decoder;
using BCnEncoder.Shared;
using CommunityToolkit.HighPerformance;

namespace Stardust.Core.Assets;

public enum DdsHdrFormat { Bc6Unsigned, Bc6Signed }
/// <summary>Owned little-endian IEEE binary16 RGB, top-to-bottom rows; no alpha or tone mapping.</summary>
public sealed record DdsHdrMip(int Width,int Height,ReadOnlyMemory<byte> RgbHalf);
public sealed record DdsHdrImage(DdsHdrFormat Format,IReadOnlyList<DdsHdrMip> Mips,DdsAlphaMode AlphaMode,uint DxgiFormat)
{
    public int Width=>Mips[0].Width;
    public int Height=>Mips[0].Height;
}

public static class DdsHdrDecoder
{
    public static DdsHdrImage Decode(ReadOnlySpan<byte> source,DdsReadLimits? limits=null)
    {
        limits??=new();
        if(limits.MaximumDimension is <1 or >32768 || limits.MaximumInputBytes<148 || limits.MaximumDecodedBytes<6)
            throw new ArgumentOutOfRangeException(nameof(limits));
        Require(source.Length>=148 && source.Length<=limits.MaximumInputBytes && source[..4].SequenceEqual("DDS "u8),"Invalid HDR DDS size/magic.");
        Require(U32(source,4)==124 && U32(source,76)==32,"Invalid HDR DDS header sizes.");
        uint flags=U32(source,8),height=U32(source,12),width=U32(source,16),alpha=U32(source,144),dxgi=U32(source,128);
        Require((flags&0x1007)==0x1007 && (U32(source,108)&0x1000)!=0,"Missing HDR DDS texture/header fields.");
        Require(width>0 && height>0 && width<=limits.MaximumDimension && height<=limits.MaximumDimension,"HDR DDS dimension limit.");
        Require(U32(source,24)<=1 && U32(source,112)==0 && U32(source,132)==3 && U32(source,136)==0 && U32(source,140)==1,
            "Only one 2D HDR DDS surface is supported.");
        Require((U32(source,80)&4)!=0 && (U32(source,80)&~5u)==0 && U32(source,84)==0x30315844,"HDR DDS requires DX10 FourCC.");
        Require(dxgi is 95 or 96 && alpha<=4,"HDR DDS requires typed BC6H and valid alpha metadata.");
        uint count=Math.Max(1u,U32(source,28));int maximum=1;
        for(uint dimension=Math.Max(width,height);dimension>1;dimension>>=1)maximum++;
        Require(count<=maximum,"HDR DDS mip count exceeds dimensions.");
        List<(int W,int H,int Offset,int Length)> ranges=new();int cursor=148;long decoded=0;
        for(int level=0;level<count;level++)
        {
            int w=(int)Math.Max(1u,width>>level),h=(int)Math.Max(1u,height>>level);
            long length=(long)((w+3)/4)*((h+3)/4)*16;decoded+=(long)w*h*6;
            Require(decoded<=limits.MaximumDecodedBytes && length<=source.Length-cursor,"HDR DDS truncation/decode budget.");
            ranges.Add((w,h,cursor,(int)length));cursor+=(int)length;
        }
        Require(cursor==source.Length,"HDR DDS trailing bytes.");
        BcDecoder decoder=new();ColorRgbFloat[,] pixels=new ColorRgbFloat[4,4];Span2D<ColorRgbFloat> blockPixels=new(pixels);
        List<DdsHdrMip> mips=new();
        foreach(var range in ranges)
        {
            byte[] output=new byte[range.W*range.H*6];int offset=range.Offset;
            for(int by=0;by<(range.H+3)/4;by++)for(int bx=0;bx<(range.W+3)/4;bx++)
            {
                ReadOnlySpan<byte> block=source.Slice(offset,16);offset+=16;
                int mode=block[0]&31;
                if(mode is 19 or 23 or 27 or 31)Array.Clear(pixels);
                else decoder.DecodeBlockHdr(block,dxgi==95?CompressionFormat.Bc6U:CompressionFormat.Bc6S,blockPixels);
                for(int y=0;y<4 && by*4+y<range.H;y++)for(int x=0;x<4 && bx*4+x<range.W;x++)
                {
                    ColorRgbFloat pixel=pixels[y,x];int destination=((by*4+y)*range.W+bx*4+x)*6;
                    Write(output.AsSpan(destination),pixel.r);Write(output.AsSpan(destination+2),pixel.g);Write(output.AsSpan(destination+4),pixel.b);
                }
            }
            mips.Add(new(range.W,range.H,output));
        }
        return new(dxgi==95?DdsHdrFormat.Bc6Unsigned:DdsHdrFormat.Bc6Signed,mips.AsReadOnly(),(DdsAlphaMode)alpha,dxgi);
    }
    private static void Write(Span<byte> output,float value)=>BinaryPrimitives.WriteUInt16LittleEndian(output,BitConverter.HalfToUInt16Bits((System.Half)value));
    private static uint U32(ReadOnlySpan<byte> data,int offset)=>BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
    private static void Require(bool condition,string message){if(!condition)throw new InvalidDataException(message);}
}
