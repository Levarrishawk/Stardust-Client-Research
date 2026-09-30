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
        AppearanceExtent extent = ReadLeafExtent(graph.Root);
        if (extent.Kind != "EXBX") throw new InvalidDataException("Door leaf requires authored box bounds.");
        using SingleLeafDoorRuntime validation = new(style, extent, AppearanceTransform.Identity, 0);
        return new(selected, graph, extent, cache.Sources);
    }
    public static AppearanceExtent ReadLeafExtent(AppearanceGeometryTemplate root)
    {
        ArgumentNullException.ThrowIfNull(root);
        if(root.Mesh is not null && root.Detail is not null || root.Component is not null)throw new InvalidDataException("Door leaf geometry family is ambiguous/unsupported.");
        IffNode? metadata = root.Mesh?.AppearanceMetadata;
        if(metadata is null && root.Detail is not null)
        {
            var detailRoot=root.Detail.Metadata;
            if(!detailRoot.IsForm("DTLA") || detailRoot.Children.Count!=1 || detailRoot.Children[0].Children.Count==0)
                throw new InvalidDataException("Invalid door detail metadata.");
            metadata=detailRoot.Children[0].Children[0];
        }
        if(metadata is null)throw new InvalidDataException("Door leaf requires a static mesh or detail appearance.");
        AppearanceExtent extent = AppearanceExtentDecoder.DecodeBase(metadata) ?? throw new InvalidDataException("Door leaf lacks an authored extent.");
        if(extent.Kind != "EXBX")throw new InvalidDataException("Door leaf requires authored box bounds.");
        if(root.Detail is not null)
        {
            // One rigid physical envelope is independent of visual LOD. Verify all supported bands fit it.
            if(root.Diagnostics.Count!=0 || root.Detail.Diagnostics.Count!=0 || root.Children.Count!=root.Detail.Levels.Count || root.Children.Count==0)
                throw new InvalidDataException("Door detail graph is incomplete.");
            long vertices=0;
            foreach(var child in root.Children)
            {
                StaticMeshDescription mesh=child.Template.Mesh??throw new InvalidDataException("Door detail requires direct static mesh bands.");
                if(child.Template.Diagnostics.Count!=0 || child.Template.Children.Count!=0 || child.LocalTransform!=AppearanceTransform.Identity)
                    throw new InvalidDataException("Door detail band transform/graph unsupported.");
                vertices+=mesh.VertexCount;if(vertices>1_000_000)throw new InvalidDataException("Door detail vertex budget.");
                foreach(var vertex in mesh.Primitives.SelectMany(p=>p.Vertices))
                {
                    var p=vertex.Position;var lo=extent.Minimum!.Value;var hi=extent.Maximum!.Value;
                    if(!float.IsFinite(p.X) || !float.IsFinite(p.Y) || !float.IsFinite(p.Z) || p.X<lo.X-1e-4f || p.Y<lo.Y-1e-4f || p.Z<lo.Z-1e-4f || p.X>hi.X+1e-4f || p.Y>hi.Y+1e-4f || p.Z>hi.Z+1e-4f)
                        throw new InvalidDataException("Door detail geometry exceeds authored root bounds.");
                }
            }
        }
        return extent;
    }
}
