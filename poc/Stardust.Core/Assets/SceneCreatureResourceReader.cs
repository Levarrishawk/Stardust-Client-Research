using System.Collections.ObjectModel;
using Stardust.Core.Protocol;

namespace Stardust.Core.Assets;

/// <summary>Owned descriptors and prepared clips; create transform scalars remain in wire order and coordinates.</summary>
public sealed record SceneCreatureResources(SceneCreateObjectByCrcMessage Create, ObjectTemplateCatalogEntry Recipe,
    CreatureAppearanceDescriptorBundle Creature, int ExternalReads, long SourceBytes);

public static class SceneCreatureResourceReader
{
    public static SceneCreatureResources Load(SceneCreateObjectByCrcMessage create, ObjectTemplateCatalog catalog,
        LogicalAnimationClipSelection idle, IReadOnlyDictionary<string, LogicalAnimationClipSelection> movementSelections,
        Func<string, TreLookupResult> resolve, CancellationToken token = default,
        int maximumResources = 64, long maximumSourceBytes = 16 * 1024 * 1024, bool loadAlbedoFactors = false)
    {
        ArgumentNullException.ThrowIfNull(create); ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(resolve); ArgumentNullException.ThrowIfNull(movementSelections);
        if (maximumResources is < 1 or > 4096 || maximumSourceBytes is < 1 or > 134217728)
            throw new ArgumentOutOfRangeException(nameof(maximumResources));
        // Validate scalars without normalizing, remapping or repairing quaternion/position values.
        if (new[] { create.QuaternionX, create.QuaternionY, create.QuaternionZ, create.QuaternionW,
                    create.PositionX, create.PositionZ, create.PositionY }.Any(f => !float.IsFinite(f)))
            throw new InvalidDataException("Finite create transform required.");
        var recipe = catalog.Require(create.TemplateCrc);
        if (recipe.Recipe != SceneObjectResourceRecipe.CreatureDetail0) throw new InvalidDataException("Creature resource recipe required.");
        var ownedIdle = CopySelection(idle);
        if (movementSelections.Count != 2 || !movementSelections.ContainsKey("walk") || !movementSelections.ContainsKey("run"))
            throw new ArgumentException("Explicit walk and run selections required.", nameof(movementSelections));
        var bank = new ReadOnlyDictionary<string, LogicalAnimationClipSelection>(new Dictionary<string, LogicalAnimationClipSelection>(StringComparer.Ordinal)
        { ["walk"] = CopySelection(movementSelections["walk"]), ["run"] = CopySelection(movementSelections["run"]) });
        token.ThrowIfCancellationRequested();
        int reads = 0;
        TreLookupResult Read(string path)
        {
            token.ThrowIfCancellationRequested();
            var source = resolve(path); reads++;
            token.ThrowIfCancellationRequested();
            return source; // Unchanged descriptor reader snapshots payloads through its owned cache.
        }
        var creature = CreatureAppearanceDescriptorBundleReader.Load(Read, recipe.ObjectPath, recipe.ObjectSha256, recipe.AppearanceSha256,
            limits: new(MaximumResources: maximumResources, MaximumSourceBytes: maximumSourceBytes),
            loadSkeletons: true, loadSkinSources: true, meshDetail: 0, loadGeometry: true,
            loadLogicalTables: true, animationSelection: ownedIdle, prepareSelectedAnimation: true,
            loadMaterialDefaults: true, loadPaletteDefaults: true, loadAlbedoFactors: loadAlbedoFactors, additionalAnimationSelections: bank);
        token.ThrowIfCancellationRequested();
        long decodedBytes = creature.MaterialBundle!.Textures.Values.Sum(t => t.Mips.Sum(m => (long)m.Rgba.Length));
        if (decodedBytes > 32 * 1024 * 1024) throw new AppearanceGraphBudgetException("Scene creature decoded image budget.");
        return new(create with { }, recipe, creature, reads, creature.Sources.Values.Sum(s => (long)s.Bytes));
    }

    private static LogicalAnimationClipSelection CopySelection(LogicalAnimationClipSelection selection)
    {
        ArgumentNullException.ThrowIfNull(selection);
        ObjectTemplateCatalog.ValidatePin(selection.ClipSha256);
        string table = LegacyAssetPath.Normalize(selection.TablePath);
        if (!table.StartsWith("appearance/lat/", StringComparison.Ordinal) || !table.EndsWith(".lat", StringComparison.Ordinal) ||
            table.Split('/').Any(s => s is "." or "..")) throw new ArgumentException("Logical table path required.");
        LogicalAnimationTableDecoder.ValidateName(selection.LogicalName);
        if (selection.ExplicitChildRoute is { Count: > 8 }) throw new ArgumentException("Explicit route budget.");
        int[]? route = selection.ExplicitChildRoute?.ToArray();
        if (route is { Length: > 8 } || route is not null && route.Any(i => i is < 0 or > 4095))
            throw new ArgumentException("Explicit route budget.");
        return selection with { TablePath = table, ClipSha256 = selection.ClipSha256.ToUpperInvariant(),
            ExplicitChildRoute = route is null ? null : Array.AsReadOnly(route) };
    }
}
