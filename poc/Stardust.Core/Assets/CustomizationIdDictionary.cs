using System.Buffers.Binary;
using System.Collections.ObjectModel;
using System.Text;

namespace Stardust.Core.Assets;

public sealed record CustomizationIdDictionaryLimits(int MaximumBytes = 65536,
    int MaximumRecords = 127, int MaximumNameBytes = 1024);
public sealed record CustomizationIdEntry(ushort Id, string FullName);
public enum CustomizationNameProjectionKind { Projected, UnsupportedNamespace }
public sealed record CustomizationNameProjection(CustomizationNameProjectionKind Kind,
    string FullName, BlueprintCustomizationKey? Key);

/// <summary>Owned CIDM/0001 dictionary only; IDs are neither palette indices nor packed value flags.</summary>
public sealed class CustomizationIdDictionary
{
    private CustomizationIdDictionary(List<CustomizationIdEntry> entries)
    {
        Entries = entries.AsReadOnly();
        ById = new ReadOnlyDictionary<ushort, CustomizationIdEntry>(entries.ToDictionary(e => e.Id));
        ByName = new ReadOnlyDictionary<string, CustomizationIdEntry>(entries.ToDictionary(e => e.FullName, StringComparer.Ordinal));
    }
    public IReadOnlyList<CustomizationIdEntry> Entries { get; }
    public IReadOnlyDictionary<ushort, CustomizationIdEntry> ById { get; }
    public IReadOnlyDictionary<string, CustomizationIdEntry> ByName { get; }

    public static CustomizationIdDictionary Load(ReadOnlySpan<byte> source,
        CustomizationIdDictionaryLimits? limits = null, CancellationToken token = default)
    {
        limits ??= new();
        if (limits.MaximumBytes is < 32 or > 1048576 || limits.MaximumRecords is < 1 or > 127 ||
            limits.MaximumNameBytes is < 1 or > 4096) throw new ArgumentOutOfRangeException(nameof(limits));
        token.ThrowIfCancellationRequested();
        var root = IffDocument.Parse(source, new(limits.MaximumBytes, 3, 2)).Root;
        Need(root.IsForm("CIDM") && root.Children.Count == 1 && root.Children[0].IsForm("0001"), "Unsupported CIDM envelope/version.");
        var version = root.Children[0];
        Need(version.Children.Count == 1 && version.Children[0].Tag == "DATA", "CIDM requires one DATA chunk.");
        var data = version.Children[0].Payload.Span;
        var entries = new List<CustomizationIdEntry>();
        var names = new HashSet<string>(StringComparer.Ordinal);
        int cursor = 0;
        while (cursor < data.Length)
        {
            token.ThrowIfCancellationRequested();
            Need(entries.Count < limits.MaximumRecords && data.Length - cursor >= 2, "CIDM record limit or truncated ID.");
            ushort id = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(cursor, 2)); cursor += 2;
            Need(id == entries.Count + 1 && id < 128, "CIDM IDs must be consecutive from one and below 128.");
            int start = cursor;
            while (cursor < data.Length && data[cursor] != 0)
            {
                Need(cursor - start < limits.MaximumNameBytes && data[cursor] is >= 32 and <= 126, "CIDM name byte/length rejected.");
                cursor++;
            }
            Need(cursor < data.Length && cursor > start, "CIDM name must be nonempty and NUL terminated.");
            string name = Encoding.ASCII.GetString(data.Slice(start, cursor - start)); cursor++;
            Need(names.Add(name), "Duplicate CIDM name.");
            entries.Add(new(id, name));
        }
        Need(entries.Count > 0, "Empty CIDM dictionary.");
        token.ThrowIfCancellationRequested();
        return new(entries);
    }

    /// <summary>Exact ordinal prefix projection; unsupported full names remain available verbatim.</summary>
    public static CustomizationNameProjection ProjectName(string fullName)
    {
        ArgumentNullException.ThrowIfNull(fullName);
        foreach (var domain in new[] { (Prefix: "/private/", Flag: true), (Prefix: "/shared_owner/", Flag: false) })
            if (fullName.StartsWith(domain.Prefix, StringComparison.Ordinal) && fullName.Length > domain.Prefix.Length)
                return new(CustomizationNameProjectionKind.Projected, fullName, new(fullName[domain.Prefix.Length..], domain.Flag));
        return new(CustomizationNameProjectionKind.UnsupportedNamespace, fullName, null);
    }
    private static void Need(bool condition, string message)
    { if (!condition) throw new InvalidDataException(message); }
}
