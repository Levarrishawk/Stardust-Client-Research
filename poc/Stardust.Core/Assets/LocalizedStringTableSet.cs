using System.Collections.ObjectModel;

namespace Stardust.Core.Assets;

public sealed record LocalizedStringTableSetLimits(int MaximumPaths = 64, long MaximumSourceBytes = 67108864,
    long MaximumRecords = 262144, long MaximumValueUnits = 8388608, long MaximumKeyBytes = 8388608,
    LocalizedStringTableLimits? TableLimits = null);

/// <summary>Explicit finite atomic preload of owned variant1 tables; no lazy I/O or locale/cache policy.</summary>
public sealed class LocalizedStringTableSet
{
    private sealed class Frame { public bool Violated; }
    [ThreadStatic] private static Frame? active;
    private LocalizedStringTableSet(List<string> requested, Dictionary<string, LocalizedStringTable> tables,
        List<string> missing, long sourceBytes, long records, long units, long keys)
    {
        RequestedPaths = requested.AsReadOnly(); Tables = new ReadOnlyDictionary<string, LocalizedStringTable>(tables);
        MissingPaths = missing.AsReadOnly(); SourceBytes = sourceBytes; TotalRecords = records; TotalValueUnits = units; TotalKeyBytes = keys;
    }
    public IReadOnlyList<string> RequestedPaths { get; }
    public IReadOnlyDictionary<string, LocalizedStringTable> Tables { get; }
    public IReadOnlyList<string> MissingPaths { get; }
    public long SourceBytes { get; }
    public long TotalRecords { get; }
    public long TotalValueUnits { get; }
    public long TotalKeyBytes { get; }
    public LocalizedStringTable? Find(string path)
    {
        if (!PathValid(path)) throw new ArgumentException("Safe normalized STF VFS path required.", nameof(path));
        return Tables.TryGetValue(path, out var table) ? table : null;
    }
    public static LocalizedStringTableSet Build(IEnumerable<string> paths,
        Func<string, ReadOnlyMemory<byte>?> resourceResolver, LocalizedStringTableSetLimits? limits = null,
        CancellationToken cancellationToken = default)
    {
        if (active is not null) { active.Violated = true; throw new InvalidOperationException("Recursive localized-table preload."); }
        ArgumentNullException.ThrowIfNull(paths); ArgumentNullException.ThrowIfNull(resourceResolver);
        limits ??= new(); var parser = limits.TableLimits ?? new(); Validate(limits, parser);
        var frame = new Frame(); active = frame;
        try
        {
            Check(frame, cancellationToken);
            var requested = new List<string>(); var unique = new HashSet<string>(StringComparer.Ordinal);
            // Bound traversal directly; do not trust collection Count or enumerate again after callbacks.
            foreach (string path in paths)
            {
                Check(frame, cancellationToken);
                Need(requested.Count < limits.MaximumPaths && PathValid(path), "STF request count/path rejected.");
                Need(unique.Add(path), "Duplicate exact STF request path."); requested.Add(path);
            }
            Check(frame, cancellationToken);
            var tables = new Dictionary<string, LocalizedStringTable>(StringComparer.Ordinal); var missing = new List<string>();
            long bytes = 0, records = 0, units = 0, keys = 0;
            foreach (string path in requested)
            {
                Check(frame, cancellationToken); var memory = resourceResolver(path); Check(frame, cancellationToken);
                if (memory is null) { missing.Add(path); continue; }
                int length = memory.Value.Length;
                Need(length >= 13 && length <= parser.MaximumBytes && length <= limits.MaximumSourceBytes - bytes,
                    "STF per-table/aggregate source budget rejected.");
                var effective = parser with {
                    MaximumRecords = (int)Math.Min(parser.MaximumRecords, limits.MaximumRecords - records),
                    MaximumTotalValueUnits = (int)Math.Min(parser.MaximumTotalValueUnits, limits.MaximumValueUnits - units),
                    MaximumTotalKeyBytes = (int)Math.Min(parser.MaximumTotalKeyBytes, limits.MaximumKeyBytes - keys) };
                // Only this private snapshot is cleared. Caller memory may be reused at the next callback.
                byte[] copy = memory.Value.ToArray(); LocalizedStringTable table;
                try { Check(frame, cancellationToken); table = LocalizedStringTable.Load(copy, effective); }
                finally { Array.Clear(copy); }
                Check(frame, cancellationToken);
                bytes += length; records += table.Header.RecordCount; units += table.TotalValueUnits; keys += table.TotalKeyBytes;
                tables.Add(path, table);
            }
            Check(frame, cancellationToken);
            var result = new LocalizedStringTableSet(requested, tables, missing, bytes, records, units, keys);
            Check(frame, cancellationToken); return result;
        }
        finally { active = null; }
    }
    private static void Validate(LocalizedStringTableSetLimits limits, LocalizedStringTableLimits parser)
    {
        if (limits.MaximumPaths is < 0 or > 1024 || limits.MaximumSourceBytes is < 0 or > 268435456 ||
            limits.MaximumRecords is < 0 or > 1000000 || limits.MaximumValueUnits is < 0 or > 33554432 ||
            limits.MaximumKeyBytes is < 0 or > 33554432 || parser.MaximumBytes is < 13 or > 67108864 ||
            parser.MaximumRecords is < 0 or > 1000000 || parser.MaximumValueUnits is < 0 or > 1000000 ||
            parser.MaximumKeyBytes is < 0 or > 65536 || parser.MaximumTotalValueUnits is < 0 or > 16777216 ||
            parser.MaximumTotalKeyBytes is < 0 or > 16777216)
            throw new ArgumentOutOfRangeException(nameof(limits));
    }
    private static void Check(Frame frame, CancellationToken token)
    { if (frame.Violated) throw new InvalidOperationException("Recursive localized-table callback rejected."); token.ThrowIfCancellationRequested(); }
    private static bool Component(char c) => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '_' or '-';
    private static bool PathValid(string? path)
    {
        if (path is null || path.Length > 389 || !path.StartsWith("string/", StringComparison.Ordinal) || !path.EndsWith(".stf", StringComparison.Ordinal)) return false;
        int slash = path.LastIndexOf('/'), identifierLength = path.Length - slash - 5;
        if (slash < 8 || slash > 256 || identifierLength is < 1 or > 128) return false;
        bool component = false;
        for (int i = 0; i < slash; i++)
            if (path[i] == '/') { if (!component) return false; component = false; }
            else { if (!Component(path[i])) return false; component = true; }
        if (!component) return false;
        for (int i = slash + 1; i < path.Length - 4; i++) if (!Component(path[i])) return false;
        return true;
    }
    private static void Need(bool condition, string message) { if (!condition) throw new InvalidDataException(message); }
}
