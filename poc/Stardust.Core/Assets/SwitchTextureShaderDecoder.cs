using System.Buffers.Binary;
using System.Text;

namespace Stardust.Core.Assets;

/// <summary>Caller-selected inspection frame, pinned to the original SWTS source. No runtime switcher.</summary>
public sealed record SwitchTextureFrameSelection(string ShaderPath, string ShaderSha256, int FrameIndex)
{
    public void Validate()
    {
        if (ShaderPath is null || ShaderPath.Replace('\\', '/').Split('/').Any(s => s is "." or "..") || LegacyAssetPath.Normalize(ShaderPath) != ShaderPath ||
            !ShaderPath.StartsWith("shader/", StringComparison.Ordinal) || !ShaderPath.EndsWith(".sht", StringComparison.Ordinal))
            throw new ArgumentException("Canonical shader logical path required.");
        if (ShaderSha256 is null || ShaderSha256.Length != 64 || !ShaderSha256.All(Uri.IsHexDigit))
            throw new ArgumentException("SWTS SHA-256 required.");
        if (FrameIndex is < 0 or >= 256) throw new ArgumentOutOfRangeException(nameof(FrameIndex));
    }
}

public sealed record SwitchTextureFrame(uint TextureTag, string TexturePath);
public sealed record SwitchTextureShaderDescription(string BaseShaderPath, IReadOnlyList<SwitchTextureFrame> Frames,
    uint RawDtstWord0, uint RawDtstWord1, IffNode Metadata);
public sealed record SwitchTextureMaterialInspection(SwitchTextureShaderDescription Description,
    SwitchTextureFrameSelection Selection, AssetSourceIdentity BaseShaderSource);

/// <summary>Observed SWTS/0000 NAME, DTST/0000 eight-byte record, ordered TEXT references. DTST semantics remain unknown.</summary>
public static class SwitchTextureShaderDecoder
{
    public static SwitchTextureShaderDescription Decode(ReadOnlySpan<byte> source)
    {
        IffNode root = IffDocument.Parse(source, new(MaximumBytes: 1048576, MaximumNodes: 1024, MaximumDepth: 8)).Root;
        Require(root.IsForm("SWTS") && root.Children.Count == 1, "Unsupported switch texture envelope.");
        IffNode version = root.Children[0];
        Require(version.IsForm("0000"), "Unsupported switch texture version.");
        var children = version.Children;
        Require(children.Count is >= 3 and <= 258 && children[0].Tag == "NAME" && children[1].IsForm("DTST"), "Invalid switch texture child layout.");
        string basePath = Name(children[0].Payload.Span, "shader/", ".sht");
        IffNode dtst = children[1];
        Require(dtst.Children.Count == 1 && dtst.Children[0].Tag == "0000" && dtst.Children[0].Payload.Length == 8, "Unsupported switch texture DTST record.");
        ReadOnlySpan<byte> raw = dtst.Children[0].Payload.Span;
        var frames = new List<SwitchTextureFrame>();
        foreach (IffNode node in children.Skip(2))
        {
            Require(node.Tag == "TEXT" && node.Payload.Length >= 6, "Invalid switch texture frame record.");
            frames.Add(new(BinaryPrimitives.ReadUInt32LittleEndian(node.Payload.Span), Name(node.Payload.Span[4..], "texture/", ".dds")));
        }
        return new(basePath, frames.AsReadOnly(), BinaryPrimitives.ReadUInt32LittleEndian(raw),
            BinaryPrimitives.ReadUInt32LittleEndian(raw[4..]), root);
    }

    public static StaticShaderReferenceDescription Select(SwitchTextureShaderDescription description,
        StaticShaderReferenceDescription baseShader, int frameIndex)
    {
        ArgumentNullException.ThrowIfNull(description); ArgumentNullException.ThrowIfNull(baseShader);
        Require(frameIndex >= 0 && frameIndex < description.Frames.Count, "Switch texture frame index outside authored references.");
        SwitchTextureFrame frame = description.Frames[frameIndex];
        Require(baseShader.Textures.Count(t => t.Tag == frame.TextureTag) == 1, "Switch texture tag absent or ambiguous in base shader.");
        Require(baseShader.Textures.Single(t => t.Tag == frame.TextureTag) is { SourceFlag: 0, AssetPath: not null }, "Switch texture requires a named static base binding.");
        return baseShader with { Textures = Array.AsReadOnly(baseShader.Textures.Select(t => t.Tag == frame.TextureTag
            ? t with { AssetPath = frame.TexturePath } : t).ToArray()) };
    }

    private static string Name(ReadOnlySpan<byte> bytes, string prefix, string suffix)
    {
        Require(bytes.Length is >= 2 and <= 1025 && bytes[^1] == 0, "Invalid switch texture resource name.");
        foreach (byte b in bytes[..^1]) Require(b is >= 0x20 and <= 0x7E, "Invalid switch texture name encoding.");
        string path;
        string literal = Encoding.ASCII.GetString(bytes[..^1]);
        Require(!literal.Replace('\\', '/').Split('/').Any(s => s is "." or ".."), "Switch texture dot path segments are unsupported.");
        try { path = LegacyAssetPath.Normalize(literal); }
        catch (ArgumentException) { throw new InvalidDataException("Invalid switch texture logical path."); }
        Require(path.StartsWith(prefix, StringComparison.Ordinal) && path.EndsWith(suffix, StringComparison.Ordinal), "Unsupported switch texture resource kind.");
        return path;
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidDataException(message); }
}
