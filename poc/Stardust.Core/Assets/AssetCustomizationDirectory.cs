using System.Buffers.Binary;
using System.Collections.ObjectModel;
using System.Text;

namespace Stardust.Core.Assets;

public sealed record AssetCustomizationDirectoryLimits(int MaximumBytes = 2097152, int MaximumTableRecords = 131072,
    int MaximumNameBytes = 1024, int MaximumDepth = 128, int MaximumValidationVisits = 1000000,
    int MaximumTraversalVisits = 65536, int MaximumExpandedValues = 4096);
public abstract record AssetCustomizationVariable(string FullName, int AuthoredDefault);
public sealed record AssetPaletteCustomizationVariable(string FullName, string PalettePath, int AuthoredDefault)
    : AssetCustomizationVariable(FullName, AuthoredDefault);
public sealed record AssetRangedCustomizationVariable(string FullName, int MinimumInclusive, int MaximumExclusive, int AuthoredDefault)
    : AssetCustomizationVariable(FullName, AuthoredDefault);
public sealed record AssetCustomizationCrcInversion(int RowIndex, uint PreviousCrc, uint CurrentCrc);
public sealed record AssetCustomizationDirectoryMetadata(IReadOnlyDictionary<string, int> TableCounts, int NameStrings,
    int DuplicateNameStrings, int KnownKeys, IReadOnlyList<AssetCustomizationCrcInversion> CrcOrderingInversions);
public sealed record AssetCustomizationDirectoryQuery(uint FilenameCrc, bool Found, ushort? RootKey,
    IReadOnlyList<AssetCustomizationVariable> Variables);

/// <summary>Owned bounded ACST/0000 tables and order-independent lookup; no palette resolution or live mounting.</summary>
public sealed class AssetCustomizationDirectory
{
    private readonly Dictionary<ushort, ushort[]> direct, links;
    private readonly Dictionary<uint, ushort> roots;
    private readonly AssetCustomizationVariable[] templates;
    private readonly AssetCustomizationDirectoryLimits limits;
    private AssetCustomizationDirectory(Dictionary<ushort, ushort[]> direct, Dictionary<ushort, ushort[]> links,
        Dictionary<uint, ushort> roots, AssetCustomizationVariable[] templates, AssetCustomizationDirectoryLimits limits,
        AssetCustomizationDirectoryMetadata metadata)
    { this.direct = direct; this.links = links; this.roots = roots; this.templates = templates; this.limits = limits; Metadata = metadata; }
    public AssetCustomizationDirectoryMetadata Metadata { get; }

    public static AssetCustomizationDirectory Load(ReadOnlySpan<byte> source, AssetCustomizationDirectoryLimits? limits = null)
    {
        limits ??= new();
        if (limits.MaximumBytes is < 32 or > 16777216 || limits.MaximumTableRecords is < 1 or > 1000000 ||
            limits.MaximumNameBytes is < 1 or > 4096 || limits.MaximumDepth is < 1 or > 256 ||
            limits.MaximumValidationVisits is < 1 or > 4000000 || limits.MaximumTraversalVisits is < 1 or > 1000000 ||
            limits.MaximumExpandedValues is < 1 or > 4096) throw new ArgumentOutOfRangeException(nameof(limits));
        var root = IffDocument.Parse(source, new(limits.MaximumBytes, 14, 2)).Root;
        Need(root.IsForm("ACST") && root.Children.Count == 1 && root.Children[0].IsForm("0000"), "Unsupported ACST envelope/version.");
        string[] tags = { "NAME", "PNOF", "VNOF", "DEFV", "IRNG", "RTYP", "UCMP", "ULST", "UIDX", "LLST", "LIDX", "CIDX" };
        int[] widths = { 1, 2, 2, 4, 8, 2, 3, 2, 5, 2, 5, 6 };
        var children = root.Children[0].Children;
        Need(children.Count == tags.Length, "ACST requires all twelve chunks.");
        var table = new Dictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal);
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < tags.Length; i++)
        {
            Need(children[i].Tag == tags[i], "ACST chunk order/tag mismatch.");
            var bytes = children[i].Payload;
            Need(bytes.Length % widths[i] == 0 && (i == 0 || bytes.Length / widths[i] <= limits.MaximumTableRecords), "ACST width/record budget.");
            table.Add(tags[i], bytes); counts.Add(tags[i], bytes.Length / widths[i]);
        }
        var names = new Dictionary<ushort, string>(); var nameSet = new HashSet<string>(StringComparer.Ordinal);
        var pool = table["NAME"].Span; int cursor = 0, duplicateNames = 0;
        while (cursor < pool.Length)
        {
            int start = cursor; Need(start <= ushort.MaxValue && names.Count < limits.MaximumTableRecords, "ACST NAME offset/string budget.");
            while (cursor < pool.Length && pool[cursor] != 0)
            { Need(cursor - start < limits.MaximumNameBytes && pool[cursor] is >= 32 and <= 126, "ACST NAME byte/length rejected."); cursor++; }
            Need(cursor < pool.Length && cursor > start, "ACST NAME must be nonempty and terminated.");
            string name = Encoding.ASCII.GetString(pool.Slice(start, cursor - start)); cursor++;
            names.Add((ushort)start, name); if (!nameSet.Add(name)) duplicateNames++;
        }
        ushort[] U16(string tag)
        {
            var bytes = table[tag].Span; var values = new ushort[bytes.Length / 2];
            for (int i = 0; i < values.Length; i++) values[i] = BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(i * 2, 2));
            return values;
        }
        string[] ResolveNames(string tag) => U16(tag).Select(offset => names.TryGetValue(offset, out var value) ? value : throw new InvalidDataException("ACST NAME offset is not a string boundary.")).ToArray();
        var paletteNames = ResolveNames("PNOF"); var variableNames = ResolveNames("VNOF");
        var defaults = new int[counts["DEFV"]];
        for (int i = 0; i < defaults.Length; i++) defaults[i] = BinaryPrimitives.ReadInt32LittleEndian(table["DEFV"].Span.Slice(i * 4, 4));
        var ranges = new (int Min, int Max)[counts["IRNG"]];
        for (int i = 0; i < ranges.Length; i++)
        {
            var bytes = table["IRNG"].Span.Slice(i * 8, 8);
            ranges[i] = (BinaryPrimitives.ReadInt32LittleEndian(bytes), BinaryPrimitives.ReadInt32LittleEndian(bytes[4..]));
            Need(ranges[i].Min < ranges[i].Max, "ACST invalid exclusive integer range.");
        }
        var types = U16("RTYP");
        foreach (ushort type in types) Need((type & 32767) >= 1 && (type & 32767) <= ((type & 32768) != 0 ? paletteNames.Length : ranges.Length), "ACST RTYP target rejected.");
        var templates = new AssetCustomizationVariable[counts["UCMP"]];
        for (int i = 0; i < templates.Length; i++)
        {
            var bytes = table["UCMP"].Span.Slice(i * 3, 3); int vn = bytes[0], rt = bytes[1], dv = bytes[2];
            Need(vn >= 1 && vn <= variableNames.Length && rt >= 1 && rt <= types.Length && dv >= 1 && dv <= defaults.Length, "ACST UCMP index rejected.");
            ushort type = types[rt - 1]; int target = (type & 32767) - 1;
            templates[i] = (type & 32768) != 0 ? new AssetPaletteCustomizationVariable(variableNames[vn - 1], paletteNames[target], defaults[dv - 1]) :
                new AssetRangedCustomizationVariable(variableNames[vn - 1], ranges[target].Min, ranges[target].Max, defaults[dv - 1]);
        }
        var values = U16("ULST"); var inherited = U16("LLST");
        foreach (ushort value in values) Need(value >= 1 && value <= templates.Length, "ACST ULST index rejected.");
        Dictionary<ushort, ushort[]> Index(string tag, ushort[] list)
        {
            var result = new Dictionary<ushort, ushort[]>();
            for (int i = 0; i < counts[tag]; i++)
            {
                var bytes = table[tag].Span.Slice(i * 5, 5); ushort key = BinaryPrimitives.ReadUInt16LittleEndian(bytes), start = BinaryPrimitives.ReadUInt16LittleEndian(bytes[2..]); int count = bytes[4];
                Need(key != 0 && start <= list.Length && count <= list.Length - start && !result.ContainsKey(key), "ACST index key/list slice/duplicate rejected.");
                result.Add(key, list.AsSpan(start, count).ToArray());
            }
            return result;
        }
        var direct = Index("UIDX", values); var links = Index("LIDX", inherited); var known = direct.Keys.Concat(links.Keys).ToHashSet();
        foreach (ushort key in inherited) Need(key != 0 && known.Contains(key), "ACST LLST key rejected.");
        var roots = new Dictionary<uint, ushort>(); var inversions = new List<AssetCustomizationCrcInversion>(); uint previous = 0;
        for (int i = 0; i < counts["CIDX"]; i++)
        {
            var bytes = table["CIDX"].Span.Slice(i * 6, 6); uint crc = BinaryPrimitives.ReadUInt32LittleEndian(bytes); ushort key = BinaryPrimitives.ReadUInt16LittleEndian(bytes[4..]);
            Need(known.Contains(key) && roots.TryAdd(crc, key), "ACST CIDX root/duplicate CRC rejected.");
            if (i > 0 && crc < previous) inversions.Add(new(i, previous, crc)); previous = crc;
        }
        // Validate every key, including unreachable keys, before exposing any directory.
        var state = new Dictionary<ushort, byte>(); var heights = new Dictionary<ushort, int>(); int visits = 0;
        int Validate(ushort key, int depth)
        {
            Need(++visits <= limits.MaximumValidationVisits && depth <= limits.MaximumDepth, "ACST validation traversal/depth budget.");
            if (state.TryGetValue(key, out byte status)) { Need(status == 2, "ACST inheritance cycle."); return heights[key]; }
            state[key] = 1; int height = 1;
            if (links.TryGetValue(key, out var children)) foreach (ushort child in children) height = Math.Max(height, 1 + Validate(child, depth + 1));
            Need(height <= limits.MaximumDepth, "ACST graph height budget."); state[key] = 2; heights[key] = height; return height;
        }
        foreach (ushort key in known) Validate(key, 1);
        var metadata = new AssetCustomizationDirectoryMetadata(new ReadOnlyDictionary<string, int>(counts), names.Count, duplicateNames, known.Count, inversions.AsReadOnly());
        return new(direct, links, roots, templates, limits, metadata);
    }

    public AssetCustomizationDirectoryQuery Query(uint filenameCrc, bool suppressSharedOwner = false, string? normalizedAssetPath = null)
    {
        if (normalizedAssetPath is not null && (LegacyAssetPath.Normalize(normalizedAssetPath) != normalizedAssetPath || LegacyAssetPath.Checksum(normalizedAssetPath) != filenameCrc))
            throw new ArgumentException("Normalized asset path must match the explicit CRC.", nameof(normalizedAssetPath));
        if (!roots.TryGetValue(filenameCrc, out ushort root)) return new(filenameCrc, false, null, Array.Empty<AssetCustomizationVariable>());
        var output = new List<AssetCustomizationVariable>(); var names = new HashSet<string>(StringComparer.Ordinal); var visited = new HashSet<ushort>(); int visits = 0;
        void Expand(ushort key, int depth)
        {
            Need(++visits <= limits.MaximumTraversalVisits && depth <= limits.MaximumDepth, "ACST query traversal/depth budget.");
            if (!visited.Add(key)) return;
            if (direct.TryGetValue(key, out var values)) foreach (ushort index in values)
            {
                var value = templates[index - 1]; if (!names.Add(value.FullName)) continue;
                if (suppressSharedOwner && value.FullName.Contains("/shared_owner/", StringComparison.Ordinal)) continue;
                Need(output.Count < limits.MaximumExpandedValues, "ACST expanded value budget."); output.Add(value);
            }
            if (links.TryGetValue(key, out var children)) foreach (ushort child in children) Expand(child, depth + 1);
        }
        Expand(root, 1); return new(filenameCrc, true, root, output.AsReadOnly());
    }
    private static void Need(bool condition, string message) { if (!condition) throw new InvalidDataException(message); }
}
