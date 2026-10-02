using System.Collections.ObjectModel;

namespace Stardust.Core.Assets;

public enum SceneObjectResourceRecipe { CreatureDetail0, StaticAppearance }

/// <summary>Caller-supplied modern configuration; not a recovered global template table.</summary>
public sealed record ObjectTemplateCatalogEntry(uint TemplateCrc, string ObjectPath, string ObjectSha256,
    string AppearanceSha256, SceneObjectResourceRecipe Recipe = SceneObjectResourceRecipe.CreatureDetail0);

public sealed class ObjectTemplateCatalog
{
    public IReadOnlyDictionary<uint, ObjectTemplateCatalogEntry> Entries { get; }

    public ObjectTemplateCatalog(IEnumerable<ObjectTemplateCatalogEntry> entries,
        int maximumEntries = 256, int maximumPathCharacters = 262144)
    {
        ArgumentNullException.ThrowIfNull(entries);
        if (maximumEntries is < 1 or > 4096 || maximumPathCharacters is < 1 or > 4194304)
            throw new ArgumentOutOfRangeException(nameof(maximumEntries));
        var snapshot = new Dictionary<uint, ObjectTemplateCatalogEntry>();
        var paths = new HashSet<string>(StringComparer.Ordinal);
        int characters = 0;
        foreach (var entry in entries)
        {
            ArgumentNullException.ThrowIfNull(entry);
            if (snapshot.Count >= maximumEntries) throw new ArgumentException("Catalog entry budget.", nameof(entries));
            string path = LegacyAssetPath.Normalize(entry.ObjectPath);
            if (!path.StartsWith("object/", StringComparison.Ordinal) || !path.EndsWith(".iff", StringComparison.Ordinal) ||
                path.Split('/').Any(s => s is "." or "..") || path[(path.LastIndexOf('/') + 1)..].Length <= 4)
                throw new ArgumentException("Normalized object/*.iff path required.", nameof(entries));
            characters = checked(characters + path.Length);
            if (characters > maximumPathCharacters) throw new ArgumentException("Catalog path budget.", nameof(entries));
            if (entry.TemplateCrc != LegacyAssetPath.Checksum(path))
                throw new ArgumentException("Template CRC must equal the normalized legacy path checksum.", nameof(entries));
            if (entry.Recipe is not (SceneObjectResourceRecipe.CreatureDetail0 or SceneObjectResourceRecipe.StaticAppearance))
                throw new ArgumentException("Unsupported resource recipe.", nameof(entries));
            ValidatePin(entry.ObjectSha256); ValidatePin(entry.AppearanceSha256);
            if (!paths.Add(path) || !snapshot.TryAdd(entry.TemplateCrc, entry with
                { ObjectPath = path, ObjectSha256 = entry.ObjectSha256.ToUpperInvariant(), AppearanceSha256 = entry.AppearanceSha256.ToUpperInvariant() }))
                throw new ArgumentException("Duplicate/conflicting path or duplicate/colliding CRC.", nameof(entries));
        }
        Entries = new ReadOnlyDictionary<uint, ObjectTemplateCatalogEntry>(snapshot);
    }

    public ObjectTemplateCatalogEntry Require(uint templateCrc)
        => Entries.TryGetValue(templateCrc, out var entry) ? entry : throw new InvalidDataException("Unknown object template CRC; no fallback recipe.");

    internal static void ValidatePin(string pin)
    {
        if (pin is null || pin.Length != 64 || !pin.All(Uri.IsHexDigit))
            throw new ArgumentException("Explicit SHA-256 pin required.", nameof(pin));
    }
}
