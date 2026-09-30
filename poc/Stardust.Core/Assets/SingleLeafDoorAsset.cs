namespace Stardust.Core.Assets;

public sealed record SingleLeafDoorAsset(DoorStyleSelection Selection, AppearanceGeometryGraph Geometry, AppearanceExtent Extent, IReadOnlyDictionary<string, AssetSourceIdentity> Sources);
public static class SingleLeafDoorAssetReader
{
    public const string TablePath = "datatables/appearance/door_style.iff";
    public static SingleLeafDoorAsset Load(Func<string, TreLookupResult> resolve, string styleName, string expectedTableSha256, string expectedLeafSha256)
    {
        ArgumentNullException.ThrowIfNull(resolve);
        OwnedAssetSourceCache cache = new(resolve, new());
        byte[] source = cache.ReadPinned(TablePath, expectedTableSha256);
        DoorStyleSelection selected = DoorStyleTableDecoder.Decode(source).Resolve(styleName);
        DoorStyle style = selected.Style;
        if (style.IsForceField || style.SecondaryAppearance.Length != 0 || style.PrimaryAppearance.Length == 0 || style.FlipSecondaryRaw != 0) throw new InvalidDataException("Door asset requires one normal unflipped leaf.");
        AppearanceGeometryGraph graph = AppearanceGeometryGraphReader.Load(cache.Read, style.PrimaryAppearance, expectedLeafSha256);
        StaticMeshDescription mesh = graph.Root.Mesh ?? throw new InvalidDataException("Door leaf asset slice requires a direct static mesh.");
        AppearanceExtent extent = AppearanceExtentDecoder.DecodeBase(mesh.AppearanceMetadata) ?? throw new InvalidDataException("Door leaf lacks an authored extent.");
        if (extent.Kind != "EXBX") throw new InvalidDataException("Door leaf requires authored box bounds.");
        using SingleLeafDoorRuntime validation = new(style, extent, AppearanceTransform.Identity, 0);
        return new(selected, graph, extent, cache.Sources);
    }
}
