using System.Buffers.Binary;
using System.Collections.ObjectModel;
using System.Text;

namespace Stardust.Core.Assets;

public sealed record SwitchTextureFrameBundleLimits(int MaximumFrames = 256, int MaximumSources = 258,
    long MaximumSourceBytes = 134217728, long MaximumDecodedBytes = 134217728, int MaximumMips = 4096,
    int MaximumShaderBytes = 1048576, DdsReadLimits? DdsLimits = null);
public sealed class SwitchTextureFrameMip
{
    private readonly byte[] rgba;
    internal SwitchTextureFrameMip(DdsMip mip) { Width = mip.Width; Height = mip.Height; rgba = mip.Rgba.ToArray(); }
    public int Width { get; }
    public int Height { get; }
    public int DecodedBytes => rgba.Length;
    public byte[] CopyRgba() => (byte[])rgba.Clone();
}
public sealed class SwitchTextureFrameImage
{
    private readonly byte[] tail;
    internal SwitchTextureFrameImage(DdsImage image)
    {
        Format = image.Format; ColorSpace = image.ColorSpace; AlphaMode = image.AlphaMode; DxgiFormat = image.DxgiFormat;
        Mips = Array.AsReadOnly(image.Mips.Select(m => new SwitchTextureFrameMip(m)).ToArray()); tail = image.RetainedTerminalBlock.ToArray();
    }
    public DdsFormat Format { get; }
    public DdsColorSpace ColorSpace { get; }
    public DdsAlphaMode AlphaMode { get; }
    public uint? DxgiFormat { get; }
    public IReadOnlyList<SwitchTextureFrameMip> Mips { get; }
    public int RetainedTerminalBytes => tail.Length;
    public byte[] CopyRetainedTerminalBlock() => (byte[])tail.Clone();
    public DdsImage CopyImage() => new(Format, Array.AsReadOnly(Mips.Select(m => new DdsMip(m.Width, m.Height, m.CopyRgba())).ToArray()), ColorSpace, AlphaMode, DxgiFormat, CopyRetainedTerminalBlock());
}
public sealed record SwitchTextureFrameInput(int Index, uint TextureTag, string TexturePath, SwitchTextureFrameImage Image);

/// <summary>Owned immutable authored frame inputs only; no cursor, effects/default texture I/O or material/world closure.</summary>
public sealed class SwitchTextureFrameBundle
{
    private sealed class Frame { public bool Violated; }
    [ThreadStatic] private static Frame? active;
    private readonly byte[] shaderBytes, baseBytes;
    private SwitchTextureFrameBundle(string path, SwitchTextureShaderDescription shader, byte[] rawShader, byte[] rawBase,
        List<SwitchTextureFrameInput> frames, Dictionary<string, SwitchTextureFrameImage> images,
        IReadOnlyDictionary<string, AssetSourceIdentity> sources, long decoded, int mips)
    {
        ShaderPath = path; BaseShaderPath = shader.BaseShaderPath; RawDtstWord0 = shader.RawDtstWord0; RawDtstWord1 = shader.RawDtstWord1;
        shaderBytes = rawShader.ToArray(); baseBytes = rawBase.ToArray(); Frames = frames.AsReadOnly();
        Images = new ReadOnlyDictionary<string, SwitchTextureFrameImage>(images);
        Sources = new ReadOnlyDictionary<string, AssetSourceIdentity>(sources.ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal));
        SourceBytes = Sources.Values.Sum(s => (long)s.Bytes); DecodedBytes = decoded; MipCount = mips;
    }
    public string ShaderPath { get; }
    public string BaseShaderPath { get; }
    public uint RawDtstWord0 { get; }
    public uint RawDtstWord1 { get; }
    public int FrameCount => checked((int)RawDtstWord0);
    public float Interval => BitConverter.UInt32BitsToSingle(RawDtstWord1);
    public IReadOnlyList<SwitchTextureFrameInput> Frames { get; }
    public IReadOnlyDictionary<string, SwitchTextureFrameImage> Images { get; }
    public IReadOnlyDictionary<string, AssetSourceIdentity> Sources { get; }
    public long SourceBytes { get; }
    /// <summary>Unique decoded RGBA mip bytes plus retained terminal bytes; aliases count once.</summary>
    public long DecodedBytes { get; }
    public int MipCount { get; }
    public SwitchTextureShaderDescription CopySwitchDescription() => SwitchTextureShaderDecoder.Decode(shaderBytes);
    public StaticShaderReferenceDescription CopyBaseDescription() => StaticShaderReferences.Decode(baseBytes);
    public static SwitchTextureFrameBundle Load(Func<string, TreLookupResult> resolve, string shaderPath, string shaderSha256,
        SwitchTextureFrameBundleLimits? limits = null, CancellationToken cancellationToken = default)
    {
        if (active is not null) { active.Violated = true; throw new InvalidOperationException("Recursive switch-frame bundle."); }
        ArgumentNullException.ThrowIfNull(resolve); shaderPath = Path(shaderPath, "shader/", ".sht"); ObjectTemplateCatalog.ValidatePin(shaderSha256);
        limits ??= new(); var dds = limits.DdsLimits ?? new();
        if (limits.MaximumFrames is < 1 or > 256 || limits.MaximumSources is < 1 or > 258 ||
            limits.MaximumSourceBytes is < 1 or > 268435456 || limits.MaximumDecodedBytes is < 0 or > 536870912 ||
            limits.MaximumMips is < 0 or > 4096 || limits.MaximumShaderBytes is < 1 or > 1048576 ||
            dds.MaximumDimension is < 1 or > 32768 || dds.MaximumInputBytes is < 128 or > 67108864 ||
            dds.MaximumDecodedBytes is < 4 or > 268435456) throw new ArgumentOutOfRangeException(nameof(limits));
        var frame = new Frame(); active = frame;
        try
        {
            Check(frame, cancellationToken);
            var cache = new OwnedAssetSourceCache(p => {
                Check(frame, cancellationToken); var source = resolve(p); Check(frame, cancellationToken);
                Need(source is not null, "Null frame resource response.");
                if (source!.Kind == TreLookupKind.Present && source.Payload is not null)
                    Need(source.Payload.Length <= (p.StartsWith("shader/", StringComparison.Ordinal) ? limits.MaximumShaderBytes : dds.MaximumInputBytes), "Per-source switch-frame budget.");
                return source;
            }, new(MaximumResources: limits.MaximumSources, MaximumSourceBytes: limits.MaximumSourceBytes));
            byte[] rawShader = cache.ReadPinned(shaderPath, shaderSha256); Check(frame, cancellationToken);
            var shader = SwitchTextureShaderDecoder.Decode(rawShader);
            Need(shader.Frames.Count <= limits.MaximumFrames && shader.RawDtstWord0 is > 0 and <= int.MaxValue && shader.RawDtstWord0 == shader.Frames.Count,
                "Modern positive signed DTST count/reference correspondence required.");
            float interval = BitConverter.UInt32BitsToSingle(shader.RawDtstWord1);
            Need(float.IsFinite(interval) && interval > 0, "Modern finite positive DTST interval required.");
            var children = shader.Metadata.Children[0].Children;
            Need(Literal(children[0].Payload.Span, "shader/", ".sht") == shader.BaseShaderPath, "Noncanonical base shader reference.");
            var needed = new HashSet<string>(StringComparer.Ordinal) { shaderPath, shader.BaseShaderPath };
            for (int i = 0; i < shader.Frames.Count; i++)
            {
                Need(Literal(children[i + 2].Payload.Span[4..], "texture/", ".dds") == shader.Frames[i].TexturePath, "Noncanonical frame path.");
                needed.Add(shader.Frames[i].TexturePath);
            }
            Need(needed.Count <= limits.MaximumSources, "Whole frame-input source count budget.");
            var baseSource = cache.Read(shader.BaseShaderPath); Need(baseSource.Kind == TreLookupKind.Present && baseSource.Payload is not null, "Base shader missing/masked.");
            byte[] rawBase = baseSource.Payload!; var baseShader = StaticShaderReferences.Decode(rawBase);
            foreach (var map in baseShader.Metadata.Children[0].Children.Where(n => n.IsForm("TXMS")))
                foreach (var binding in map.Children)
                    foreach (var name in binding.Children[0].Children.Where(n => n.Tag == "NAME")) _ = Literal(name.Payload.Span, "texture/", ".dds");
            // Reuse the accepted tag/source-flag/named-binding validator for every authored variant.
            for (int i = 0; i < shader.Frames.Count; i++) _ = SwitchTextureShaderDecoder.Select(shader, baseShader, i);
            Check(frame, cancellationToken);
            var images = new Dictionary<string, SwitchTextureFrameImage>(StringComparer.Ordinal); var frames = new List<SwitchTextureFrameInput>();
            long decoded = 0; int mips = 0;
            for (int i = 0; i < shader.Frames.Count; i++)
            {
                Check(frame, cancellationToken); var authored = shader.Frames[i];
                if (!images.TryGetValue(authored.TexturePath, out var image))
                {
                    var source = cache.Read(authored.TexturePath); Need(source.Kind == TreLookupKind.Present && source.Payload is not null, "Frame DDS missing/masked.");
                    byte[] raw = source.Payload!; Need(raw.Length >= 128, "Truncated frame DDS header.");
                    uint count = Math.Max(1u, BinaryPrimitives.ReadUInt32LittleEndian(raw.AsSpan(28)));
                    Need(count <= limits.MaximumMips - mips, "Aggregate frame mip budget.");
                    long remaining = limits.MaximumDecodedBytes - decoded; Need(remaining >= 4, "Aggregate frame decode budget.");
                    var decodedImage = DdsDecoder.Decode(raw, dds with { MaximumDecodedBytes = (int)Math.Min(dds.MaximumDecodedBytes, remaining) });
                    long bytes = decodedImage.Mips.Sum(m => (long)m.Rgba.Length) + decodedImage.RetainedTerminalBlock.Length;
                    Need(bytes <= remaining, "Aggregate decoded/tail frame budget."); Check(frame, cancellationToken);
                    image = new(decodedImage); images.Add(authored.TexturePath, image); decoded += bytes; mips += image.Mips.Count;
                }
                frames.Add(new(i, authored.TextureTag, authored.TexturePath, image));
            }
            var result = new SwitchTextureFrameBundle(shaderPath, shader, rawShader, rawBase, frames, images, cache.Sources, decoded, mips);
            Check(frame, cancellationToken); return result;
        }
        finally { active = null; }
    }
    private static string Literal(ReadOnlySpan<byte> raw, string prefix, string suffix)
    {
        Need(raw.Length is >= 2 and <= 1025 && raw[^1] == 0 && raw[..^1].IndexOf((byte)0) < 0, "Invalid terminated frame-input path.");
        foreach (byte b in raw[..^1]) Need(b <= 127, "Non-ASCII frame-input path.");
        return Path(Encoding.ASCII.GetString(raw[..^1]), prefix, suffix);
    }
    private static string Path(string path, string prefix, string suffix)
    {
        Need(path is not null && path.Length <= 1024 && path.StartsWith(prefix, StringComparison.Ordinal) && path.EndsWith(suffix, StringComparison.Ordinal), "Safe canonical frame-input path required.");
        bool component = false;
        foreach (char c in path![..^suffix.Length])
            if (c == '/') { Need(component, "Empty frame-input path component."); component = false; }
            else { Need(c is >= 'a' and <= 'z' or >= '0' and <= '9' or '_' or '-', "Unsafe/noncanonical frame-input path."); component = true; }
        Need(component, "Empty frame-input identifier."); return path;
    }
    private static void Check(Frame frame, CancellationToken token)
    { if (frame.Violated) throw new InvalidOperationException("Recursive frame callback rejected."); token.ThrowIfCancellationRequested(); }
    private static void Need(bool condition, string message) { if (!condition) throw new InvalidDataException(message); }
}
