using BCnEncoder.Decoder;
using BCnEncoder.Shared;
using CommunityToolkit.HighPerformance;

namespace Stardust.Core.Assets;

/// <summary>Pinned managed codec boundary: only validated raw 16-byte BC7 blocks enter the dependency.</summary>
internal static class Bc7Pixels
{
    internal static void Decode(ReadOnlySpan<byte> source,int width,int height,Span<byte> output)
    {
        BcDecoder decoder=new();
        ColorRgba32[,] pixels=new ColorRgba32[4,4];
        Span2D<ColorRgba32> blockPixels=new(pixels);
        int cursor=0;
        for(int by=0;by<(height+3)/4;by++)
            for(int bx=0;bx<(width+3)/4;bx++)
            {
                ReadOnlySpan<byte> block=source.Slice(cursor,16);cursor+=16;
                // The reserved no-mode prefix has defined zero RGBA output; do not ask a library to guess.
                if(block[0]==0) Array.Clear(pixels);
                else decoder.DecodeBlock(block,CompressionFormat.Bc7,blockPixels);
                for(int y=0;y<4 && by*4+y<height;y++)
                    for(int x=0;x<4 && bx*4+x<width;x++)
                    {
                        ColorRgba32 pixel=pixels[y,x];int offset=((by*4+y)*width+bx*4+x)*4;
                        output[offset]=pixel.r;output[offset+1]=pixel.g;output[offset+2]=pixel.b;output[offset+3]=pixel.a;
                    }
            }
    }
}
