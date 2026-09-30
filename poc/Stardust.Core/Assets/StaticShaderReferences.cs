using System.Buffers.Binary;
using System.Text;

namespace Stardust.Core.Assets;

public sealed record ShaderTextureReference(uint Tag, byte SourceFlag, string Version,
    string? AssetPath, ReadOnlyMemory<byte> BindingMetadata);
public sealed record StaticShaderReferenceDescription(string Version, string EffectPath,
    IReadOnlyList<ShaderTextureReference> Textures, IffNode Metadata);

/// <summary>External SSHT effect/texture references only. Material, coordinate and sampler bytes remain owned metadata.</summary>
public static class StaticShaderReferences
{
    public static StaticShaderReferenceDescription Decode(ReadOnlySpan<byte> source)
    {
        IffNode root = IffDocument.Parse(source).Root;
        return DecodeOwned(root);
    }

    internal static StaticShaderReferenceDescription DecodeOwned(IffNode root)
    {
        Require(root.IsForm("SSHT") && root.Children.Count == 1, "Unsupported static shader envelope.");
        IffNode version = root.Children[0];
        Require(version.Tag == "FORM" && version.FormType is "0000" or "0001", "Unsupported static shader version.");
        IffNode[] effects = version.Children.Where(node => node.Tag == "NAME").ToArray();
        Require(effects.Length == 1, "Only one external effect reference is supported.");
        string effect = ReadName(effects[0].Payload.Span);
        if (!effect.Contains('/')) effect = "effect/" + effect;
        IffNode[] maps = version.Children.Where(node => node.IsForm("TXMS")).ToArray();
        Require(maps.Length <= 1, "Duplicate texture map form.");
        List<ShaderTextureReference> textures = new();
        HashSet<uint> tags = new();
        if (maps.Length == 1)
        {
            Require(maps[0].Children.Count <= 1024, "Too many texture bindings.");
            foreach (IffNode binding in maps[0].Children)
            {
                Require(binding.IsForm("TXM ") && binding.Children.Count == 1, "Invalid texture binding envelope.");
                IffNode bindingVersion = binding.Children[0];
                Require(bindingVersion.Tag == "FORM" && bindingVersion.FormType is "0000" or "0001" or "0002", "Unsupported texture binding version.");
                IReadOnlyList<IffNode> children = bindingVersion.Children;
                Require(children.Count is 1 or 2 && children[0].Tag == "DATA" &&
                    (children.Count == 1 || children[1].Tag == "NAME"), "Invalid texture binding child layout.");
                ReadOnlySpan<byte> data = children[0].Payload.Span;
                bool valid = bindingVersion.FormType switch
                {
                    "0000" => data.Length == 5,
                    "0001" => data.Length == 11 || (data.Length == 12 && data[^1] == 0),
                    "0002" => data.Length == 12,
                    _ => false
                };
                Require(valid, "Invalid texture binding metadata length.");
                uint tag = BinaryPrimitives.ReadUInt32LittleEndian(bindingVersion.FormType == "0000" ? data[1..] : data);
                byte flag = bindingVersion.FormType == "0000" ? data[0] : data[4];
                Require(tags.Add(tag), "Duplicate texture binding tag.");
                textures.Add(new(tag, flag, bindingVersion.FormType!, children.Count == 2 ? ReadName(children[1].Payload.Span) : null,
                    children[0].Payload));
            }
        }
        return new(version.FormType!, effect, textures.AsReadOnly(), root);
    }

    private static string ReadName(ReadOnlySpan<byte> bytes)
    {
        Require(bytes.Length is >= 2 and <= 1025 && bytes[^1] == 0, "Invalid shader resource name.");
        foreach (byte value in bytes[..^1]) Require(value is >= 0x20 and <= 0x7E, "Invalid shader resource name encoding.");
        try { return LegacyAssetPath.Normalize(Encoding.ASCII.GetString(bytes[..^1])); }
        catch (ArgumentException) { throw new InvalidDataException("Invalid shader logical resource name."); }
    }
    private static void Require(bool valid, string message)
    {
        if (!valid) throw new InvalidDataException(message);
    }
}
