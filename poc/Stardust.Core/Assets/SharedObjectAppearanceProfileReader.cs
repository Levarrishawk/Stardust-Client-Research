using System.Collections.ObjectModel;

namespace Stardust.Core.Assets;

public sealed record SharedObjectAppearanceEvidence(string SourcePath, IReadOnlyList<SharedTemplateLayer> Layers);
public sealed record SharedObjectAppearanceProfile(string RootPath, string AppearancePath, ResolvedTemplateField AppearanceField,
    IReadOnlyList<SharedObjectAppearanceEvidence> Inheritance, IReadOnlyDictionary<string, AssetSourceIdentity> Sources);

/// <summary>Deterministic nearest SHOT appearance string only; no random/modifier evaluation or family factory inference.</summary>
public static class SharedObjectAppearanceProfileReader
{
    public static SharedObjectAppearanceProfile Load(Func<string, TreLookupResult> resolve, string objectPath, string objectSha256,
        int maximumDepth = 16, long maximumSourceBytes = 1048576)
    {
        ArgumentNullException.ThrowIfNull(resolve); ObjectTemplateCatalog.ValidatePin(objectSha256);
        if (maximumDepth is < 1 or > 16 || maximumSourceBytes is < 1 or > 16777216) throw new ArgumentOutOfRangeException(nameof(maximumDepth));
        objectPath = ObjectPath(objectPath);
        var cache = new OwnedAssetSourceCache(resolve, new(MaximumResources: maximumDepth, MaximumSourceBytes: maximumSourceBytes));
        var chain = new List<SharedObjectAppearanceEvidence>(); var seen = new HashSet<string>(StringComparer.Ordinal);
        string? current = objectPath;
        while (current is not null)
        {
            current = ObjectPath(current);
            if (!seen.Add(current)) throw new InvalidDataException("Shared object inheritance cycle.");
            if (chain.Count >= maximumDepth) throw new AppearanceGraphBudgetException("Shared object inheritance depth budget.");
            byte[] bytes;
            if (chain.Count == 0) bytes = cache.ReadPinned(current, objectSha256);
            else
            {
                var source = cache.Read(current);
                if (source.Kind != TreLookupKind.Present || source.Payload is null) throw new InvalidDataException("Inherited object missing/masked.");
                bytes = source.Payload;
            }
            // Inspect raw derivation strings before the shared decoder's legacy normalization removes leading traversal.
            void Derivations(IffNode family)
            {
                foreach (var child in family.Children)
                {
                    if (child.IsForm("DERV"))
                    {
                        if (child.Children.Count != 1 || child.Children[0].Tag != "XXXX") throw new InvalidDataException("Invalid object derivation.");
                        string raw = SharedObjectTemplateDecoder.Text(child.Children[0].Payload.Span, 1024);
                        if (raw.Length != 0) _ = ObjectPath(raw);
                    }
                    else if (child.FormType is "SCOT" or "STOT" or "SHOT") Derivations(child);
                }
            }
            Derivations(IffDocument.Parse(bytes, new(MaximumBytes: 1048576, MaximumNodes: 4096, MaximumDepth: 16)).Root);
            var description = SharedObjectTemplateDecoder.Decode(bytes);
            chain.Add(new(current, description.Layers)); current = description.ParentPath;
        }
        string? selected = null; ResolvedTemplateField? field = null;
        foreach (var item in chain)
        {
            var layer = item.Layers.Single(l => l.Family == "SHOT");
            if (!layer.Parameters.TryGetValue("appearanceFilename", out var encoded)) continue;
            var bytes = encoded.Span;
            if (bytes.Length == 0) throw new InvalidDataException("Missing appearance string mode.");
            if (bytes[0] == 0)
            { if (bytes.Length != 1) throw new InvalidDataException("Invalid inherited appearance string."); continue; }
            if (bytes[0] != 1) throw new InvalidDataException("Unsupported/random appearance string mode.");
            string value = SharedObjectTemplateDecoder.Text(bytes[1..], 1024);
            string normalized = value.Length == 0 ? string.Empty : AppearancePath(value);
            if (field is null) { selected = normalized; field = new(item.SourcePath, layer.Family, layer.Version); }
        }
        if (string.IsNullOrEmpty(selected) || field is null) throw new InvalidDataException("No nonempty deterministic nearest appearance.");
        return new(objectPath, selected, field, chain.AsReadOnly(), cache.Sources);
    }

    private static string CheckedPath(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (path.Replace('\\', '/').Split('/').Any(p => p is "." or "..")) throw new InvalidDataException("Traversal in object/appearance reference.");
        return LegacyAssetPath.Normalize(path);
    }
    private static string ObjectPath(string path)
    {
        path = CheckedPath(path);
        if (!path.StartsWith("object/", StringComparison.Ordinal) || !path.EndsWith(".iff", StringComparison.Ordinal) || path[(path.LastIndexOf('/') + 1)..].Length <= 4)
            throw new InvalidDataException("Object template path required.");
        return path;
    }
    private static string AppearancePath(string path)
    {
        path = CheckedPath(path);
        if (!path.StartsWith("appearance/", StringComparison.Ordinal) || path.Length <= 11 || path.EndsWith('/')) throw new InvalidDataException("Appearance namespace required.");
        return path;
    }
}
