using System.Collections.ObjectModel;

namespace Stardust.Core.Assets;

public sealed record AssetCustomizationContextLimits(int MaximumSelectedKeys = 4096, int MaximumDescriptors = 4096,
    int MaximumPalettePaths = 256, int MaximumPaletteEntries = 1024, int MaximumTotalPaletteEntries = 262144);
public enum AssetCustomizationContextBindingKind { Ranged, Palette }
public sealed record AssetCustomizationContextBinding(ShaderCustomizationKey Key, ushort Id, string FullName,
    AssetCustomizationContextBindingKind Kind, int MinimumInclusive, int MaximumExclusive, int AuthoredDefault, string? PalettePath);
public sealed class AssetCustomizationContext
{
    internal AssetCustomizationContext(CustomizationVariableRangeContext ranges, Dictionary<ShaderCustomizationKey, int> defaults,
        List<AssetCustomizationContextBinding> bindings)
    { RangeContext = ranges; AuthoredDefaults = new ReadOnlyDictionary<ShaderCustomizationKey, int>(defaults); Bindings = bindings.AsReadOnly(); }
    public CustomizationVariableRangeContext RangeContext { get; }
    public IReadOnlyDictionary<ShaderCustomizationKey, int> AuthoredDefaults { get; }
    public IReadOnlyList<AssetCustomizationContextBinding> Bindings { get; }
}

/// <summary>Selected owned ACST/CIDM binding. Authored asset defaults are not remote player state.</summary>
public static class AssetCustomizationContextBuilder
{
    [ThreadStatic] private static bool building;
    [ThreadStatic] private static bool reentryAttempted;
    public static AssetCustomizationContext Build(AssetCustomizationDirectoryQuery directoryQuery, CustomizationIdDictionary dictionary,
        IReadOnlyCollection<ShaderCustomizationKey> selectedKeys, Func<string, PaletteDescription?> ownedPaletteResolver,
        CancellationToken token = default, AssetCustomizationContextLimits? limits = null)
    {
        if (building) { reentryAttempted = true; throw new InvalidOperationException("Customization context callback reentry."); }
        building = true; reentryAttempted = false;
        try
        {
            var result = BuildCore(directoryQuery, dictionary, selectedKeys, ownedPaletteResolver, token, limits ?? new());
            token.ThrowIfCancellationRequested();
            if (reentryAttempted) throw new InvalidOperationException("Customization callback attempted nested build.");
            return result;
        }
        finally { building = false; reentryAttempted = false; }
    }
    private static AssetCustomizationContext BuildCore(AssetCustomizationDirectoryQuery query, CustomizationIdDictionary dictionary,
        IReadOnlyCollection<ShaderCustomizationKey> selected, Func<string, PaletteDescription?> resolver,
        CancellationToken token, AssetCustomizationContextLimits limits)
    {
        ArgumentNullException.ThrowIfNull(query); ArgumentNullException.ThrowIfNull(dictionary);
        ArgumentNullException.ThrowIfNull(selected); ArgumentNullException.ThrowIfNull(resolver);
        if (limits.MaximumSelectedKeys is < 1 or > 4096 || limits.MaximumDescriptors is < 1 or > 4096 ||
            limits.MaximumPalettePaths is < 1 or > 4096 || limits.MaximumPaletteEntries is < 1 or > 1024 ||
            limits.MaximumTotalPaletteEntries is < 1 or > 4194304) throw new ArgumentOutOfRangeException(nameof(limits));
        token.ThrowIfCancellationRequested();
        Need(query.Found && query.RootKey is > 0 && query.Variables is not null && query.Variables.Count <= limits.MaximumDescriptors,
            "A bounded known ACST query is required.");
        Need(selected.Count is > 0 && selected.Count <= limits.MaximumSelectedKeys, "Selected customization key budget.");
        var keys = new List<ShaderCustomizationKey>(); var unique = new HashSet<ShaderCustomizationKey>();
        foreach (var key in selected)
        {
            token.ThrowIfCancellationRequested(); CustomizationVariableRangeContext.ValidateKey(key);
            Need(keys.Count < limits.MaximumSelectedKeys && unique.Add(key), "Duplicate/excessive selected customization key."); keys.Add(key);
        }
        Need(keys.Count == selected.Count, "Selected key collection changed during snapshot.");
        var descriptors = new List<AssetCustomizationVariable>(); var byName = new Dictionary<string, AssetCustomizationVariable>(StringComparer.Ordinal);
        foreach (var description in query.Variables!)
        {
            token.ThrowIfCancellationRequested(); Need(descriptors.Count < limits.MaximumDescriptors && description is not null, "Descriptor budget/null rejected.");
            Need(description!.FullName is { Length: > 0 and <= 4096 } && description.FullName.All(c => c is >= (char)32 and <= (char)126), "Invalid ACST full name.");
            Need(description is AssetPaletteCustomizationVariable or AssetRangedCustomizationVariable, "Unsupported customization descriptor type.");
            if (byName.TryGetValue(description.FullName, out var prior)) Need(prior == description, "Conflicting duplicate customization descriptor.");
            else byName.Add(description.FullName, description);
            descriptors.Add(description);
        }
        Need(descriptors.Count == query.Variables!.Count, "Descriptor collection changed during snapshot.");
        void InputsUnchanged()
        {
            Need(selected.Count == keys.Count && selected.SequenceEqual(keys) && query.Variables.Count == descriptors.Count && query.Variables.SequenceEqual(descriptors), "Customization callback mutated query/selection inputs.");
            if (reentryAttempted) throw new InvalidOperationException("Customization callback attempted nested build.");
        }
        // Resolve exact selected descriptor/ID bindings before calling external code.
        var chosen = new List<(ShaderCustomizationKey Key, ushort Id, AssetCustomizationVariable Description)>();
        foreach (var key in keys)
        {
            string full = (key.Private ? "/private/" : "/shared_owner/") + key.VariableName;
            Need(byName.TryGetValue(full, out var description), "Selected customization description absent/namespace unsupported.");
            var projected = CustomizationIdDictionary.ProjectName(description!.FullName);
            Need(projected.Key is { } p && p.VariableName == key.VariableName && p.FactoryFlag == key.Private &&
                dictionary.ByName.TryGetValue(full, out _), "Selected customization CIDM/namespace mapping absent.");
            chosen.Add((key, dictionary.ByName[full].Id, description));
        }
        var palettes = new Dictionary<string, BorrowedPaletteSnapshot>(StringComparer.Ordinal); int totalEntries = 0;
        void PalettesUnchanged() { foreach (var palette in palettes.Values) palette.RequireUnchanged(); }
        var ranges = new Dictionary<ShaderCustomizationKey, CustomizationVariableRange>(); var defaults = new Dictionary<ShaderCustomizationKey, int>();
        var bindings = new List<AssetCustomizationContextBinding>();
        foreach (var item in chosen)
        {
            token.ThrowIfCancellationRequested(); InputsUnchanged(); PalettesUnchanged();
            int minimum, maximum; string? path = null; AssetCustomizationContextBindingKind kind;
            if (item.Description is AssetRangedCustomizationVariable ranged)
            { minimum = ranged.MinimumInclusive; maximum = ranged.MaximumExclusive; kind = AssetCustomizationContextBindingKind.Ranged; }
            else
            {
                var description = (AssetPaletteCustomizationVariable)item.Description; path = description.PalettePath;
                Need(path is { Length: > 0 and <= 1024 } && path.All(c => c is >= (char)32 and <= (char)126), "Invalid exact palette path.");
                if (!palettes.TryGetValue(path, out var palette))
                {
                    Need(palettes.Count < limits.MaximumPalettePaths, "Selected palette path budget.");
                    token.ThrowIfCancellationRequested(); var provided = resolver(path);
                    token.ThrowIfCancellationRequested(); InputsUnchanged(); PalettesUnchanged();
                    Need(provided is not null, "Selected palette missing.");
                    palette = new(provided!, limits.MaximumPaletteEntries);
                    totalEntries = checked(totalEntries + palette.Count);
                    Need(totalEntries <= limits.MaximumTotalPaletteEntries, "Selected palette aggregate entry budget.");
                    palettes.Add(path, palette);
                }
                minimum = 0; maximum = palette.Count; kind = AssetCustomizationContextBindingKind.Palette;
            }
            int authored = item.Description.AuthoredDefault;
            Need(minimum < maximum && authored >= minimum && authored < maximum, "Authored default cannot seed selected variable range.");
            ranges.Add(item.Key, new(minimum, maximum)); defaults.Add(item.Key, authored);
            bindings.Add(new(item.Key, item.Id, item.Description.FullName, kind, minimum, maximum, authored, path));
        }
        token.ThrowIfCancellationRequested(); InputsUnchanged(); PalettesUnchanged();
        return new(new(ranges), defaults, bindings);
    }
    private sealed class BorrowedPaletteSnapshot
    {
        private readonly PaletteDescription source; private readonly PaletteEntry[] entries;
        private readonly string[] diagnostics; private readonly byte[] trailing; private readonly ushort version;
        public BorrowedPaletteSnapshot(PaletteDescription source, int maximumEntries)
        {
            Need(source.Entries is not null && source.Diagnostics is not null && source.Entries.Count is > 0 && source.Entries.Count <= maximumEntries &&
                (source.Version >> 8) == 3 && source.Diagnostics.Count <= 32 && source.IgnoredTrailingBytes.Length <= 4096, "Selected palette shape/count/version rejected.");
            this.source = source; entries = CopyBounded(source.Entries!, maximumEntries); diagnostics = CopyBounded(source.Diagnostics!, 32); trailing = source.IgnoredTrailingBytes.ToArray(); version = source.Version;
            Need(diagnostics.All(d => d is { Length: <= 256 } && !d.StartsWith("MissingPaletteFallback", StringComparison.Ordinal)), "Missing palette fallback/diagnostic rejected.");
            Need(entries.Length > 0, "Empty selected palette."); RequireUnchanged();
        }
        private static T[] CopyBounded<T>(IEnumerable<T> source, int maximum)
        {
            var result = new List<T>();
            foreach (var value in source) { Need(result.Count < maximum, "Borrowed palette collection budget."); result.Add(value); }
            return result.ToArray();
        }
        public int Count => entries.Length;
        public void RequireUnchanged()
        {
            Need(source.Version == version && source.Entries.Count == entries.Length && source.Diagnostics.Count == diagnostics.Length && source.Entries.SequenceEqual(entries) && source.Diagnostics.SequenceEqual(diagnostics) && source.IgnoredTrailingBytes.Span.SequenceEqual(trailing), "Customization callback mutated borrowed palette.");
        }
    }
    private static void Need(bool condition, string message) { if (!condition) throw new InvalidDataException(message); }
}
