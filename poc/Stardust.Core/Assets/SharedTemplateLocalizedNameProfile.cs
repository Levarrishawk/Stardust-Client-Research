using System.Collections.ObjectModel;

namespace Stardust.Core.Assets;

public sealed record SharedTemplateLocalizedNameLimits(int MaximumDepth = 16, int MaximumTemplateBytes = 1048576,
    long MaximumSourceBytes = 1048576, int MaximumStringBytes = 1024);
public enum SharedTemplateLocalizedNameKind { Absent, Inherited, Explicit }
public enum SharedTemplateLocalizedNameStop { Resolved, ParentTraversalDisabled, EndOfChain }
public sealed record SharedTemplateLocalizedNameVisit(string SourcePath, string Version, string? ParentPath,
    SharedTemplateLocalizedNameKind ObjectNameKind, SharedTemplateLocalizedNameKind DetailedDescriptionKind);
public sealed class SharedTemplateLocalizedNamePair
{
    internal SharedTemplateLocalizedNamePair(LocalizedStringKey table, LocalizedStringKey key, ResolvedTemplateField field)
    { Table = table; Key = key; Field = field; }
    public LocalizedStringKey Table { get; }
    public LocalizedStringKey Key { get; }
    public ResolvedTemplateField Field { get; }
    public bool IsEmptyPair => Table.Length == 0 && Key.Length == 0;
}

/// <summary>Bounded modern nearest-explicit SHOT name/description profile, preserving raw StringId bytes.</summary>
public sealed class SharedTemplateLocalizedNameProfile
{
    private sealed class Frame { public bool Violated; }
    [ThreadStatic] private static Frame? active;
    private SharedTemplateLocalizedNameProfile(string root, SharedTemplateLocalizedNamePair? name,
        SharedTemplateLocalizedNamePair? description, List<SharedTemplateLocalizedNameVisit> visits,
        IReadOnlyDictionary<string, AssetSourceIdentity> sources, SharedTemplateLocalizedNameStop stop)
    {
        RootPath = root; ObjectName = name; DetailedDescription = description; Visits = visits.AsReadOnly();
        Sources = new ReadOnlyDictionary<string, AssetSourceIdentity>(sources.ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal));
        SourceBytes = Sources.Values.Sum(s => (long)s.Bytes); Stop = stop;
    }
    public string RootPath { get; }
    public SharedTemplateLocalizedNamePair? ObjectName { get; }
    public SharedTemplateLocalizedNamePair? DetailedDescription { get; }
    public bool Complete => ObjectName is not null && DetailedDescription is not null;
    public IReadOnlyList<SharedTemplateLocalizedNameVisit> Visits { get; }
    public IReadOnlyDictionary<string, AssetSourceIdentity> Sources { get; }
    public long SourceBytes { get; }
    public SharedTemplateLocalizedNameStop Stop { get; }
    public static SharedTemplateLocalizedNameProfile Load(Func<string, TreLookupResult> resolve, string rootPath,
        string rootSha256, SharedTemplateLocalizedNameLimits? limits = null, bool followParents = true,
        CancellationToken cancellationToken = default)
    {
        if (active is not null) { active.Violated = true; throw new InvalidOperationException("Recursive shared-template name profile."); }
        ArgumentNullException.ThrowIfNull(resolve); ObjectTemplateCatalog.ValidatePin(rootSha256);
        limits ??= new();
        if (limits.MaximumDepth is < 1 or > 16 || limits.MaximumTemplateBytes is < 1 or > 1048576 ||
            limits.MaximumSourceBytes is < 1 or > 16777216 || limits.MaximumStringBytes is < 0 or > 65536)
            throw new ArgumentOutOfRangeException(nameof(limits));
        rootPath = ObjectPath(rootPath);
        var frame = new Frame(); active = frame;
        try
        {
            Check(frame, cancellationToken);
            var cache = new OwnedAssetSourceCache(path => {
                Check(frame, cancellationToken); var source = resolve(path); Check(frame, cancellationToken);
                Need(source is not null, "Null template resource response.");
                if (source!.Kind == TreLookupKind.Present && source.Payload is not null)
                    Need(source.Payload.Length <= limits.MaximumTemplateBytes, "Template per-source byte budget.");
                return source;
            }, new(MaximumResources: limits.MaximumDepth, MaximumSourceBytes: limits.MaximumSourceBytes));
            var seen = new HashSet<string>(StringComparer.Ordinal); var visits = new List<SharedTemplateLocalizedNameVisit>();
            SharedTemplateLocalizedNamePair? name = null, description = null; string? current = rootPath;
            var stop = SharedTemplateLocalizedNameStop.EndOfChain;
            while (current is not null)
            {
                Check(frame, cancellationToken); current = ObjectPath(current);
                Need(seen.Add(current), "Localized-name template inheritance cycle.");
                Need(visits.Count < limits.MaximumDepth, "Localized-name inheritance depth budget.");
                byte[] bytes;
                if (visits.Count == 0) bytes = cache.ReadPinned(current, rootSha256);
                else { var source = cache.Read(current); Need(source.Kind == TreLookupKind.Present && source.Payload is not null, "Inherited template missing/masked."); bytes = source.Payload!; }
                Check(frame, cancellationToken);
                // Preflight raw DERV references before the accepted decoder normalizes legacy paths.
                void Derivations(IffNode family)
                {
                    foreach (var child in family.Children)
                        if (child.IsForm("DERV"))
                        {
                            Need(child.Children.Count == 1 && child.Children[0].Tag == "XXXX", "Invalid shared-template derivation.");
                            string raw = SharedObjectTemplateDecoder.Text(child.Children[0].Payload.Span, 1024);
                            if (raw.Length != 0) _ = ObjectPath(raw);
                        }
                        else if (child.FormType is "SCOT" or "STOT" or "SHOT") Derivations(child);
                }
                Derivations(IffDocument.Parse(bytes, new(limits.MaximumTemplateBytes, 4096, 16)).Root);
                var decoded = SharedObjectTemplateDecoder.Decode(bytes); var shot = decoded.Layers.Single(l => l.Family == "SHOT");
                var n = Parameter(shot, "objectName", current, limits.MaximumStringBytes);
                var d = Parameter(shot, "detailedDescription", current, limits.MaximumStringBytes);
                name ??= n.Pair; description ??= d.Pair;
                visits.Add(new(current, shot.Version, decoded.ParentPath, n.Kind, d.Kind));
                Check(frame, cancellationToken);
                if (name is not null && description is not null) { stop = SharedTemplateLocalizedNameStop.Resolved; break; }
                if (!followParents && decoded.ParentPath is not null) { stop = SharedTemplateLocalizedNameStop.ParentTraversalDisabled; break; }
                current = decoded.ParentPath;
            }
            var result = new SharedTemplateLocalizedNameProfile(rootPath, name, description, visits, cache.Sources, stop);
            Check(frame, cancellationToken); return result;
        }
        finally { active = null; }
    }
    private static (SharedTemplateLocalizedNameKind Kind, SharedTemplateLocalizedNamePair? Pair) Parameter(
        SharedTemplateLayer layer, string parameter, string sourcePath, int maximum)
    {
        if (!layer.Parameters.TryGetValue(parameter, out var payload)) return (SharedTemplateLocalizedNameKind.Absent, null);
        var raw = payload.Span; Need(raw.Length != 0, "Missing localized StringId outer mode.");
        if (raw[0] == 0) { Need(raw.Length == 1, "Trailing inherited StringId bytes."); return (SharedTemplateLocalizedNameKind.Inherited, null); }
        Need(raw[0] == 1, "Unsupported localized StringId outer mode."); int cursor = 1;
        var table = Narrow(raw, ref cursor, maximum); var key = Narrow(raw, ref cursor, maximum);
        Need(cursor == raw.Length, "Trailing explicit StringId bytes.");
        return (SharedTemplateLocalizedNameKind.Explicit, new(table, key, new(sourcePath, "SHOT", layer.Version)));
    }
    private static LocalizedStringKey Narrow(ReadOnlySpan<byte> raw, ref int cursor, int maximum)
    {
        Need(cursor < raw.Length && raw[cursor++] == 1, "Unsupported/missing nested StringId string mode.");
        var remaining = raw[cursor..]; int end = remaining[..Math.Min(remaining.Length, maximum + 1)].IndexOf((byte)0);
        Need(end >= 0 && end <= maximum, "Unterminated/over-budget StringId narrow string.");
        var result = new LocalizedStringKey(remaining[..end]); cursor += end + 1; return result;
    }
    private static string ObjectPath(string path)
    {
        Need(path is not null && path.Length <= 1024 && path.StartsWith("object/", StringComparison.Ordinal) && path.EndsWith(".iff", StringComparison.Ordinal), "Safe normalized object template path required.");
        string body = path![..^4]; bool component = false;
        foreach (char c in body)
            if (c == '/') { Need(component, "Empty template path component."); component = false; }
            else { Need(c is >= 'a' and <= 'z' or >= '0' and <= '9' or '_' or '-', "Unsafe/non-normalized template path."); component = true; }
        Need(component, "Empty template identifier."); return path;
    }
    private static void Check(Frame frame, CancellationToken token)
    { if (frame.Violated) throw new InvalidOperationException("Recursive template callback rejected."); token.ThrowIfCancellationRequested(); }
    private static void Need(bool condition, string message) { if (!condition) throw new InvalidDataException(message); }
}
