using System.Collections.ObjectModel;
using Stardust.Core.Protocol;

namespace Stardust.Core.Assets;

/// <summary>Explicit modern load policy. Only the two sealed recipe-specific options are constructible.</summary>
public abstract class SceneObjectResourceOptions
{
    private protected SceneObjectResourceOptions() { }
    public abstract SceneObjectResourceRecipe Recipe { get; }
}

public sealed class SceneCreatureResourceOptions : SceneObjectResourceOptions
{
    public override SceneObjectResourceRecipe Recipe => SceneObjectResourceRecipe.CreatureDetail0;
    public LogicalAnimationClipSelection Idle { get; }
    public IReadOnlyDictionary<string, LogicalAnimationClipSelection> Movement { get; }
    public bool LoadAlbedoFactors { get; }
    public SceneCreatureFaceInspectionPolicy? FaceInspection { get; }
    public int MaximumResources { get; }
    public long MaximumSourceBytes { get; }
    public SceneCreatureResourceOptions(LogicalAnimationClipSelection idle,
        IReadOnlyDictionary<string, LogicalAnimationClipSelection> movement, bool loadAlbedoFactors,
        int maximumResources = 64, long maximumSourceBytes = 16777216, SceneCreatureFaceInspectionPolicy? faceInspection = null)
    {
        ArgumentNullException.ThrowIfNull(movement);
        if (maximumResources is < 1 or > 4096 || maximumSourceBytes is < 1 or > 134217728)
            throw new ArgumentOutOfRangeException(nameof(maximumResources));
        if (movement.Count != 2 || !movement.ContainsKey("walk") || !movement.ContainsKey("run"))
            throw new ArgumentException("Explicit walk and run required.", nameof(movement));
        Idle = Copy(idle);
        Movement = new ReadOnlyDictionary<string, LogicalAnimationClipSelection>(new Dictionary<string, LogicalAnimationClipSelection>(StringComparer.Ordinal)
            { ["walk"] = Copy(movement["walk"]), ["run"] = Copy(movement["run"]) });
        if(faceInspection is not null && !loadAlbedoFactors)throw new ArgumentException("Face inspection requires the complete albedo route.");
        FaceInspection = faceInspection; LoadAlbedoFactors = loadAlbedoFactors; MaximumResources = maximumResources; MaximumSourceBytes = maximumSourceBytes;
    }
    private static LogicalAnimationClipSelection Copy(LogicalAnimationClipSelection selection)
    {
        ArgumentNullException.ThrowIfNull(selection); ObjectTemplateCatalog.ValidatePin(selection.ClipSha256);
        string path = LegacyAssetPath.Normalize(selection.TablePath);
        if (!path.StartsWith("appearance/lat/", StringComparison.Ordinal) || !path.EndsWith(".lat", StringComparison.Ordinal) || path.Split('/').Any(p => p is "." or ".."))
            throw new ArgumentException("Logical table path required.");
        LogicalAnimationTableDecoder.ValidateName(selection.LogicalName);
        if (selection.ExplicitChildRoute is { Count: > 8 }) throw new ArgumentException("Explicit route budget.");
        int[]? route = selection.ExplicitChildRoute?.ToArray();
        if (route is { Length: > 8 } || route is not null && route.Any(i => i is < 0 or > 4095)) throw new ArgumentException("Explicit route budget.");
        return selection with { TablePath = path, ClipSha256 = selection.ClipSha256.ToUpperInvariant(), ExplicitChildRoute = route is null ? null : Array.AsReadOnly(route) };
    }
}

public sealed class SceneStaticResourceOptions : SceneObjectResourceOptions
{
    public override SceneObjectResourceRecipe Recipe => SceneObjectResourceRecipe.StaticAppearance;
    public SceneStaticMaterialPolicy MaterialPolicy { get; }
    public SwitchTextureFrameSelection? SwitchTextureFrame { get; }
    public AppearanceGraphLimits Limits { get; }
    public long MaximumDecodedBytes { get; }
    public SceneStaticSwitchFramePolicy? SwitchFrames { get; }
    public SceneStaticResourceOptions(SceneStaticMaterialPolicy materialPolicy, SwitchTextureFrameSelection? switchTextureFrame,
        AppearanceGraphLimits? limits = null, long maximumDecodedBytes = 134217728, SceneStaticSwitchFramePolicy? switchFrames = null)
    {
        if (!Enum.IsDefined(materialPolicy)) throw new ArgumentOutOfRangeException(nameof(materialPolicy));
        switchTextureFrame?.Validate(); switchFrames?.Validate(materialPolicy,switchTextureFrame); limits ??= new(MaximumResources: 64, MaximumSourceBytes: 16777216);
        if (limits.MaximumResources is < 1 or > 4096 || limits.MaximumSourceBytes is < 1 or > 134217728 ||
            limits.MaximumDepth is < 1 or > 32 || limits.MaximumEdges is < 1 or > 4096 || maximumDecodedBytes is < 4 or > 134217728)
            throw new ArgumentOutOfRangeException(nameof(limits));
        MaterialPolicy = materialPolicy; SwitchTextureFrame = switchTextureFrame is null ? null : switchTextureFrame with { };
        Limits = limits with { }; MaximumDecodedBytes = maximumDecodedBytes; SwitchFrames=switchFrames is null?null:switchFrames with {Limits=switchFrames.Limits is null?null:switchFrames.Limits with {DdsLimits=switchFrames.Limits.DdsLimits is null?null:switchFrames.Limits.DdsLimits with {}}};
    }
}

/// <summary>One owned typed payload; no resolver, options, cancellation token or publication callback is retained.</summary>
public sealed class SceneObjectResources
{
    public SceneObjectResourceRecipe Kind => Recipe.Recipe;
    public SceneCreateObjectByCrcMessage Create { get; }
    public ObjectTemplateCatalogEntry Recipe { get; }
    public IReadOnlyDictionary<string, AssetSourceIdentity> Sources { get; }
    public int ExternalReads { get; }
    public long SourceBytes { get; }
    public SceneCreatureResources? Creature { get; }
    public SceneStaticResources? Static { get; }
    internal SceneObjectResources(SceneCreatureResources payload)
    {
        Creature = payload; Create = payload.Create; Recipe = payload.Recipe;
        Sources = payload.Creature.Sources; ExternalReads = payload.ExternalReads; SourceBytes = payload.SourceBytes;
    }
    internal SceneObjectResources(SceneStaticResources payload)
    {
        Static = payload; Create = payload.Create; Recipe = payload.Recipe;
        Sources = payload.Sources; ExternalReads = payload.ExternalReads; SourceBytes = payload.SourceBytes;
    }
}

public static class SceneObjectResourceRouter
{
    /// <summary>Synchronous and engine-neutral; callers choose the worker thread and own resolver lifetime.</summary>
    public static SceneObjectResources Load(SceneCreateObjectByCrcMessage create, ObjectTemplateCatalog catalog,
        SceneObjectResourceOptions options, Func<string, TreLookupResult> resolve, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(create); ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(options); ArgumentNullException.ThrowIfNull(resolve); token.ThrowIfCancellationRequested();
        var recipe = catalog.Require(create.TemplateCrc);
        if (recipe.Recipe != options.Recipe) throw new InvalidDataException("Resource options do not match the catalog recipe.");
        SceneObjectResources result = options switch
        {
            SceneCreatureResourceOptions c => new(SceneCreatureResourceReader.Load(create, catalog, c.Idle, c.Movement, resolve, token,
                c.MaximumResources, c.MaximumSourceBytes, c.LoadAlbedoFactors, c.FaceInspection)),
            SceneStaticResourceOptions s => new(SceneStaticResourceReader.Load(create, catalog, resolve, s.MaterialPolicy, token,
                s.Limits, s.MaximumDecodedBytes, s.SwitchTextureFrame, s.SwitchFrames)),
            _ => throw new InvalidDataException("Unsupported resource options.")
        };
        token.ThrowIfCancellationRequested(); return result;
    }
}
