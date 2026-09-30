using System.Buffers.Binary;
using System.Collections.ObjectModel;
using System.Text;

namespace Stardust.Core.Assets;

public sealed record PixelTextureBinding(byte Stage, uint TextureTag);
public sealed record FixedTextureStage(uint TextureTag, uint CoordinateTag, ReadOnlyMemory<byte> Metadata);
public sealed record StaticEffectPass(byte FixedStageCount, IReadOnlyDictionary<int, uint> Fields,
    string? VertexProgram, string? PixelProgram, IReadOnlyList<PixelTextureBinding> PixelTextures,
    ReadOnlyMemory<byte> FixedVertexLighting, IReadOnlyList<FixedTextureStage> FixedStages, IffNode Metadata, string Version = "0009");
public sealed record StaticEffectImplementation(IReadOnlyList<uint> Capabilities, IReadOnlyList<uint> Options,
    uint PhaseTag, byte? Flag5, byte? Flag6, IReadOnlyList<StaticEffectPass> Passes, string Version = "0005");
public sealed record StaticEffectDescription(bool ContainsPrecalculatedVertexLighting,
    IReadOnlyList<StaticEffectImplementation> Implementations, IffNode Metadata);

/// <summary>Bounded EFCT 0001 / inline IMPL 0004-0005 / PASS 0008-0009 graph. Preserves raw enums; does not choose capabilities.</summary>
public static class StaticEffectDecoder
{
    public static StaticEffectDescription Decode(ReadOnlySpan<byte> source)
    {
        IffNode root = IffDocument.Parse(source).Root;
        Require(root.IsForm("EFCT") && root.Children.Count == 1 && root.Children[0].IsForm("0001"), "Unsupported effect envelope/version.");
        IReadOnlyList<IffNode> children = root.Children[0].Children;
        Require(children.Count >= 1 && children[0].Tag == "DATA" && children[0].Payload.Length == 2, "Invalid effect DATA.");
        ReadOnlySpan<byte> data = children[0].Payload.Span;
        Require(data[0] <= 32 && data[0] == children.Count - 1 && data[1] <= 1, "Effect implementation count/flag mismatch.");
        List<StaticEffectImplementation> implementations = new();
        foreach (IffNode impl in children.Skip(1))
        {
            Require(impl.IsForm("IMPL") && impl.Children.Count == 1 && (impl.Children[0].IsForm("0004") || impl.Children[0].IsForm("0005")), "Unsupported implementation version or external reference.");
            string implementationVersion = impl.Children[0].FormType!;
            bool hasFlags = implementationVersion == "0005";
            IReadOnlyList<IffNode> parts = impl.Children[0].Children;
            int cursor = 0;
            Require(parts.Count >= 2 && parts[cursor].Tag == "SCAP", "Missing implementation capabilities.");
            IReadOnlyList<uint> caps = ReadWords(parts[cursor++].Payload.Span);
            IReadOnlyList<uint> options = Array.Empty<uint>();
            if (cursor < parts.Count && parts[cursor].Tag == "OPTN") options = ReadWords(parts[cursor++].Payload.Span);
            Require(cursor < parts.Count && parts[cursor].Tag == "DATA" && parts[cursor].Payload.Length == (hasFlags ? 7 : 5), "Invalid implementation DATA.");
            ReadOnlySpan<byte> info = parts[cursor++].Payload.Span;
            Require(info[0] <= 32 && info[0] == parts.Count - cursor && (!hasFlags || (info[5] <= 1 && info[6] <= 1)), "Invalid implementation pass count/flags.");
            List<StaticEffectPass> passes = new();
            foreach (IffNode pass in parts.Skip(cursor)) passes.Add(ReadPass(pass));
            implementations.Add(new(caps, options, U32(info[1..]), hasFlags ? info[5] : null, hasFlags ? info[6] : null, passes.AsReadOnly(), implementationVersion));
        }
        return new(data[1] != 0, implementations.AsReadOnly(), root);
    }

    private static StaticEffectPass ReadPass(IffNode pass)
    {
        Require(pass.IsForm("PASS") && pass.Children.Count == 1 && (pass.Children[0].IsForm("0008") || pass.Children[0].IsForm("0009")), "Unsupported pass version.");
        string version = pass.Children[0].FormType!;
        IReadOnlyList<IffNode> children = pass.Children[0].Children;
        Require(children.Count >= 2 && children[0].Tag == "DATA" && children[0].Payload.Length == (version == "0009" ? 56 : 51), "Invalid pass DATA.");
        ReadOnlySpan<byte> data = children[0].Payload.Span;
        Dictionary<int, uint> fields = new(); int cursor = 1;
        foreach ((int offset, int width) in new (int, int)[] {
            (0x10,1),(0x14,1),(0x18,1),(0x19,1),(0x1A,1),(0x1C,1),(0x20,1),(0x24,1),(0x28,1),(0x2C,1),(0x30,1),
            (0x34,4),(0x38,1),(0x3C,1),(0x40,4),(0x44,4),(0x48,4),(0x4C,1),(0x4D,1),(0x50,4),
            (0x54,1),(0x58,1),(0x5C,1),(0x60,1),(0x64,1),(0x68,1),(0x6C,1),(0x70,1),(0x74,4),(0x78,4),(0x7C,4) })
        {
            // 0008 omits offset 4D and the four fields 64/68/6C/70; absence stays explicit.
            if (version == "0008" && offset is 0x4D or 0x64 or 0x68 or 0x6C or 0x70) continue;
            fields.Add(offset, width == 1 ? data[cursor] : U32(data[cursor..])); cursor += width;
        }
        Require(cursor == data.Length, "Pass schema extent mismatch.");
        IffNode vertex = children[1]; string? vertexName = null, pixelName = null;
        ReadOnlyMemory<byte> fixedVertex = default;
        if (vertex.IsForm("PVSH"))
        {
            Require(vertex.Children.Count == 1 && vertex.Children[0].Tag == "0000", "Unsupported vertex-program envelope.");
            vertexName = ReadNamePrefix(vertex.Children[0].Payload.Span, allowTail: true);
        }
        else
        {
            Require(vertex.IsForm("PFFP") && vertex.Children.Count == 1 && vertex.Children[0].Tag == "0001" &&
                vertex.Children[0].Payload.Length == 7, "Unsupported fixed vertex-lighting envelope.");
            fixedVertex = vertex.Children[0].Payload;
        }
        List<PixelTextureBinding> bindings = new(); List<FixedTextureStage> stages = new();
        if (children.Count >= 3 && children[2].IsForm("PPSH"))
        {
            Require(children.Count == 3 && data[0] == 0, "Invalid programmable pass child count.");
            IffNode pixel = children[2];
            Require(pixel.Children.Count == 1 && pixel.Children[0].IsForm("0001"), "Unsupported pixel-program version.");
            IReadOnlyList<IffNode> pixelParts = pixel.Children[0].Children;
            Require(pixelParts.Count >= 1 && pixelParts[0].Tag == "DATA" && pixelParts[0].Payload.Length >= 3, "Invalid pixel-program children.");
            byte count = pixelParts[0].Payload.Span[0];
            pixelName = ReadNamePrefix(pixelParts[0].Payload.Span[1..], allowTail: false);
            Require(count <= 8 && pixelParts.Count == count + 1, "Pixel texture-map count mismatch.");
            HashSet<byte> slots = new();
            foreach(IffNode textures in pixelParts.Skip(1))
            {
                Require(textures.IsForm("PTXM") && textures.Children.Count == 1 && textures.Children[0].Tag == "0002" &&
                    textures.Children[0].Payload.Length == 5, "Unsupported pixel texture-map version/extent.");
                ReadOnlySpan<byte> map = textures.Children[0].Payload.Span;
                Require(map[0] < 8 && slots.Add(map[0]), "Invalid or duplicate pixel texture stage.");
                bindings.Add(new(map[0], U32(map[1..])));
            }
        }
        else
        {
            Require(data[0] <= 8 && data[0] == children.Count - 2, "Fixed stage count mismatch.");
            foreach (IffNode stage in children.Skip(2))
            {
                Require(stage.IsForm("STAG") && stage.Children.Count == 1 && stage.Children[0].Tag == "0000" && stage.Children[0].Payload.Length == 33, "Unsupported fixed texture stage.");
                ReadOnlyMemory<byte> bytes = stage.Children[0].Payload;
                stages.Add(new(U32(bytes.Span[18..]), U32(bytes.Span[22..]), bytes));
            }
        }
        return new(data[0], new ReadOnlyDictionary<int,uint>(fields), vertexName, pixelName, bindings.AsReadOnly(), fixedVertex, stages.AsReadOnly(), pass, version);
    }

    internal static string ReadNamePrefix(ReadOnlySpan<byte> bytes, bool allowTail)
    {
        int end = bytes.IndexOf((byte)0);
        Require(end is > 0 and <= 1024 && (allowTail || end == bytes.Length - 1), "Invalid effect program name.");
        foreach (byte value in bytes[..end]) Require(value is >= 0x20 and <= 0x7E, "Invalid effect program name encoding.");
        try { return LegacyAssetPath.Normalize(Encoding.ASCII.GetString(bytes[..end])); }
        catch (ArgumentException) { throw new InvalidDataException("Invalid effect logical name."); }
    }
    private static IReadOnlyList<uint> ReadWords(ReadOnlySpan<byte> bytes)
    {
        Require(bytes.Length % 4 == 0 && bytes.Length <= 128, "Invalid capability/option words.");
        uint[] words = new uint[bytes.Length / 4];
        for (int i = 0; i < words.Length; i++) words[i] = U32(bytes[(i * 4)..]);
        return Array.AsReadOnly(words);
    }
    private static uint U32(ReadOnlySpan<byte> bytes) => BinaryPrimitives.ReadUInt32LittleEndian(bytes);
    internal static void Require(bool valid, string message) { if (!valid) throw new InvalidDataException(message); }
}
