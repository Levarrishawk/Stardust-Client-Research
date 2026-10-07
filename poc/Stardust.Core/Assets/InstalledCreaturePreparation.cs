using System.Collections.ObjectModel;
using Stardust.Core.Configuration;

namespace Stardust.Core.Assets;

public sealed record InstalledAnimationChoice(string TablePath, string LogicalName,
    IReadOnlyList<int>? ExplicitChildRoute = null);

/// <summary>Discovers pins from borrowed installation bytes; does not retain payloads or own the installation.</summary>
public sealed class InstalledCreaturePreparation
{
    public ObjectTemplateCatalog Catalog { get; }
    public SceneCreatureResourceOptions Options { get; }
    public IReadOnlyDictionary<string, AssetSourceIdentity> Sources { get; }

    private InstalledCreaturePreparation(ObjectTemplateCatalog catalog, SceneCreatureResourceOptions options,
        IReadOnlyDictionary<string, AssetSourceIdentity> sources)
    {
        Catalog = catalog;
        Options = options;
        Sources = new ReadOnlyDictionary<string, AssetSourceIdentity>(
            sources.ToDictionary(pair => pair.Key, pair => pair.Value with { }, StringComparer.Ordinal));
    }

    public static InstalledCreaturePreparation Create(OriginalInstallationInputs inputs, string objectPath,
        InstalledAnimationChoice idle, InstalledAnimationChoice walk, InstalledAnimationChoice run)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        string root = Path(objectPath, "object/", ".iff");
        InstalledAnimationChoice[] choices = [Copy(idle), Copy(walk), Copy(run)];
        ConfiguredTreOverlay overlay = inputs.BorrowedOverlay;
        OwnedAssetSourceCache cache = new(overlay.Lookup, new(MaximumResources: 256, MaximumSourceBytes: 64 * 1024 * 1024));
        byte[] Read(string path)
        {
            TreLookupResult found = cache.Read(path);
            if (found.Kind != TreLookupKind.Present || found.Payload is null)
                throw new InvalidDataException("Required creature preparation source is missing or masked.");
            return found.Payload;
        }
        Read(root);
        string rootHash = cache.Sources[root].Sha256;
        SharedCreatureTemplateProfile profile = SharedCreatureTemplateProfileReader.Load(cache.Read, root, rootHash);
        string appearancePath = Path(profile.AppearancePath, "appearance/", ".sat");
        SkeletalAppearanceDescription appearance = SkeletalAppearanceDecoder.Decode(Read(appearancePath));
        Dictionary<string, LogicalAnimationTableDescription> tables = new(StringComparer.Ordinal);
        foreach (SkeletalAnimationReference reference in appearance.AnimationTables)
        {
            string table = Path(reference.AnimationTablePath, "appearance/lat/", ".lat");
            if (!tables.ContainsKey(table)) tables.Add(table, LogicalAnimationTableDecoder.Decode(Read(table)));
        }
        LogicalAnimationClipSelection Select(InstalledAnimationChoice choice)
        {
            if (!tables.TryGetValue(choice.TablePath, out LogicalAnimationTableDescription? table))
                throw new InvalidDataException("Selected animation table is not declared by the skeletal appearance.");
            // Do not use Find: its absent-name policy intentionally returns a default entry.
            LogicalAnimationEntry[] exact = table.SerializedEntries.Where(entry =>
                entry.Name.Equals(choice.LogicalName, StringComparison.OrdinalIgnoreCase)).Take(2).ToArray();
            if (exact.Length != 1 || exact[0].Synthetic || exact[0].Template is null)
                throw new InvalidDataException("Explicit unique serialized logical animation required; no default fallback.");
            LogicalAnimationProxyDescription proxy = LogicalAnimationProxyDecoder.Decode(exact[0].Template!, choice.ExplicitChildRoute);
            Read(proxy.ClipPath);
            return new(choice.TablePath, choice.LogicalName, cache.Sources[proxy.ClipPath].Sha256, choice.ExplicitChildRoute);
        }
        LogicalAnimationClipSelection selectedIdle = Select(choices[0]);
        LogicalAnimationClipSelection selectedWalk = Select(choices[1]);
        LogicalAnimationClipSelection selectedRun = Select(choices[2]);
        ObjectTemplateCatalog catalog = new([new(LegacyAssetPath.Checksum(root), root, rootHash,
            cache.Sources[appearancePath].Sha256)]);
        SceneCreatureResourceOptions options = new(selectedIdle,
            new Dictionary<string, LogicalAnimationClipSelection>(StringComparer.Ordinal)
                { ["walk"] = selectedWalk, ["run"] = selectedRun }, loadAlbedoFactors: true,
            maximumResources: 256, maximumSourceBytes: 64 * 1024 * 1024);
        return new(catalog, options, cache.Sources);
    }

    private static InstalledAnimationChoice Copy(InstalledAnimationChoice choice)
    {
        ArgumentNullException.ThrowIfNull(choice);
        string table = Path(choice.TablePath, "appearance/lat/", ".lat");
        ArgumentNullException.ThrowIfNull(choice.LogicalName);
        string name = LogicalAnimationTableDecoder.ValidateName(choice.LogicalName).TrimEnd(' ');
        if (name.Length == 0 || name.Equals("default", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("An explicit nondefault logical animation is required.");
        int[]? route = null;
        if (choice.ExplicitChildRoute is { } supplied)
        {
            int count = supplied.Count;
            if (count is < 0 or > 8) throw new InvalidDataException("Animation route depth budget.");
            route = new int[count];
            for (int index = 0; index < count; index++)
            {
                int child = supplied[index];
                if (child is < 0 or > 4095) throw new InvalidDataException("Animation child route index budget.");
                route[index] = child;
            }
        }
        return new(table, name, route is null ? null : Array.AsReadOnly(route));
    }

    private static string Path(string value, string prefix, string suffix)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 1024 || value != value.Trim() ||
            value.Any(c => c < 32 || c > 126) || value.Contains(':') || value.StartsWith('/') || value.StartsWith('\\') ||
            value.Replace('\\', '/').Split('/').Any(part => part.Length == 0 || part is "." or ".."))
            throw new InvalidDataException("Explicit relative legacy resource path required.");
        string path = LegacyAssetPath.Normalize(value);
        if (!path.StartsWith(prefix, StringComparison.Ordinal) || !path.EndsWith(suffix, StringComparison.Ordinal))
            throw new InvalidDataException("Unsupported preparation resource path family.");
        return path;
    }
}
