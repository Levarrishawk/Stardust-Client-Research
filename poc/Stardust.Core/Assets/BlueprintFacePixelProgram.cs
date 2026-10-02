using System.Buffers.Binary;

namespace Stardust.Core.Assets;

public sealed record BlueprintFacePixelProgram(byte TextureStage,byte FactorRegister,IffNode Metadata);

/// <summary>Recognizes the bounded PSHP0000 copy-alpha face program; never executes original code.</summary>
public static class BlueprintFacePixelProgramDecoder
{
    public static BlueprintFacePixelProgram Decode(ReadOnlySpan<byte> source)
    {
        var root=IffDocument.Parse(source,new(MaximumBytes:65536,MaximumNodes:16,MaximumDepth:4)).Root;
        Need(root.IsForm("PSHP") && root.Children.Count==1 && root.Children[0].IsForm("0000"));
        var parts=root.Children[0].Children;
        Need(parts.Count==2 && parts[0].Tag=="PSRC" && parts[1].Tag=="PEXE");
        var text=parts[0].Payload.Span;
        Need(text.Length>0 && text[^1]==0 && text[..^1].IndexOf((byte)0)<0);
        var code=parts[1].Payload.Span;
        // Shader model 1.1: TEX t0; MUL r0.rgb,t0,c3; coissued MOV r0.a,t0.a; END.
        uint[] tokens={0xFFFF0101,0x00000042,0xB00F0000,0x00000005,0x80070000,
            0xB0E40000,0xA0E40003,0x40000001,0x80080000,0xB0FF0000,0x0000FFFF};
        Need(code.Length==tokens.Length*4);
        for(int i=0;i<tokens.Length;i++)Need(BinaryPrimitives.ReadUInt32LittleEndian(code[(i*4)..])==tokens[i]);
        return new(0,3,root);
    }
    private static void Need(bool value){if(!value)throw new InvalidDataException("Unsupported face pixel-program envelope or instruction semantics.");}
}
