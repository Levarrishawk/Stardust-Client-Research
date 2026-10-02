using Stardust.Core.Protocol;

namespace Stardust.Core.Assets;

public enum SceneStaticMaterialPolicy { RequireComplete, AllowNeutral }
public sealed record SceneStaticResources(SceneCreateObjectByCrcMessage Create, ObjectTemplateCatalogEntry Recipe,
    SharedObjectAppearanceProfile Profile, TexturedAppearanceGraph Appearance, IReadOnlyDictionary<string, AssetSourceIdentity> Sources,
    int ExternalReads, long SourceBytes, SceneStaticMaterialPolicy MaterialPolicy);

public static class SceneStaticResourceReader
{
    public static SceneStaticResources Load(SceneCreateObjectByCrcMessage create, ObjectTemplateCatalog catalog,
        Func<string, TreLookupResult> resolve, SceneStaticMaterialPolicy materialPolicy, CancellationToken token = default,
        AppearanceGraphLimits? limits = null, long maximumDecodedBytes = 134217728, SwitchTextureFrameSelection? switchTextureFrame = null)
    {
        ArgumentNullException.ThrowIfNull(create); ArgumentNullException.ThrowIfNull(catalog); ArgumentNullException.ThrowIfNull(resolve);
        switchTextureFrame?.Validate();
        if (!Enum.IsDefined(materialPolicy)) throw new ArgumentOutOfRangeException(nameof(materialPolicy));
        limits ??= new(MaximumResources: 64, MaximumSourceBytes: 16777216);
        if (limits.MaximumResources is < 1 or > 4096 || limits.MaximumSourceBytes is < 1 or > 134217728 ||
            limits.MaximumDepth is < 1 or > 32 || limits.MaximumEdges is < 1 or > 4096 || maximumDecodedBytes is < 4 or > 134217728)
            throw new ArgumentOutOfRangeException(nameof(limits));
        if (new[] { create.QuaternionX, create.QuaternionY, create.QuaternionZ, create.QuaternionW,
                    create.PositionX, create.PositionZ, create.PositionY }.Any(f => !float.IsFinite(f)))
            throw new InvalidDataException("Finite raw create required.");
        var recipe = catalog.Require(create.TemplateCrc);
        if (recipe.Recipe != SceneObjectResourceRecipe.StaticAppearance) throw new InvalidDataException("Static appearance recipe required.");
        token.ThrowIfCancellationRequested(); int reads = 0;
        var cache = new OwnedAssetSourceCache(path =>
        {
            token.ThrowIfCancellationRequested(); var result = resolve(path); reads++; token.ThrowIfCancellationRequested(); return result;
        }, limits);
        TreLookupResult Read(string path) { token.ThrowIfCancellationRequested(); return cache.Read(path); }
        var profile = SharedObjectAppearanceProfileReader.Load(Read, recipe.ObjectPath, recipe.ObjectSha256,
            Math.Min(16, limits.MaximumDepth), Math.Min(1048576, limits.MaximumSourceBytes));
        var appearance = TexturedAppearanceGraphReader.Load(Read, profile.AppearancePath, recipe.AppearanceSha256, limits, maximumDecodedBytes, switchTextureFrame: switchTextureFrame);
        token.ThrowIfCancellationRequested(); var visited = new HashSet<string>(StringComparer.Ordinal); int meshCount = 0;
        void Check(AppearanceGeometryTemplate template)
        {
            if (!visited.Add(template.Path)) return;
            if (template.Diagnostics.Count != 0) throw new InvalidDataException("Required static geometry part unavailable.");
            if (template.Mesh is not null) meshCount++;
            foreach (var child in template.Children) Check(child.Template);
        }
        Check(appearance.Geometry.Root);
        if (meshCount == 0) throw new InvalidDataException("Static recipe has no supported geometry.");
        if (materialPolicy == SceneStaticMaterialPolicy.RequireComplete && (appearance.Diagnostics.Count != 0 || appearance.Meshes.Count != meshCount))
            throw new InvalidDataException("Complete static materials required by caller policy.");
        token.ThrowIfCancellationRequested();
        return new(create with { }, recipe, profile, appearance, cache.Sources, reads, cache.Sources.Values.Sum(s => (long)s.Bytes), materialPolicy);
    }
}
