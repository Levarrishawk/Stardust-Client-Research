using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text.Json;

namespace Stardust.Core.Assets;

public sealed record InteriorDoorSceneBundle(PortalLayoutDescription Layout, IReadOnlyDictionary<int, FloorMeshDescription> Floors,
    IReadOnlyList<PortalSingleLeafDoorBundle> Doorways, IReadOnlyList<InteriorDoorBinding> Bindings,
    IReadOnlyDictionary<string, AssetSourceIdentity> Sources, string SpecificationSha256, string LayoutPath)
{
    public InteriorDoorActorScene CreateActor(CellFloorLocator initial, AppearanceTransform? owner = null, FloorStepPolicy? stepPolicy = null, bool holdClosingLeaves = false,
        ActorSphereVolume? actorSphere = null,IReadOnlyDictionary<int,IReadOnlyList<CollisionTriangle>>? staticTriangles=null,int maximumTriangles=2048) =>
        new(Layout, Floors, Bindings, initial, owner, stepPolicy: stepPolicy, holdClosingLeaves: holdClosingLeaves,actorSphere:actorSphere,
            staticTriangles:staticTriangles,maximumTriangles:maximumTriangles);
}

/// <summary>Explicit bounded scene selection. No asset search, style fallback or inferred doorway selection.</summary>
public static class InteriorDoorSceneBundleReader
{
    public static InteriorDoorSceneBundle Load(Func<string, TreLookupResult> resolve, ReadOnlySpan<byte> specification,
        string expectedSpecificationSha256, string tableSha256)
    {
        ArgumentNullException.ThrowIfNull(resolve);
        if (specification.Length is < 1 or > 16384) throw new InvalidDataException("Interior scene specification byte budget.");
        byte[] bytes = specification.ToArray(); string digest = Convert.ToHexString(SHA256.HashData(bytes));
        if (!string.Equals(digest, expectedSpecificationSha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Interior scene specification identity mismatch.");
        try
        {
            using var document = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 8 });
            var root = document.RootElement;
            string member = root.GetProperty("member").GetString() ?? throw new InvalidDataException("Missing scene member.");
            string layoutSha = root.GetProperty("layout_sha256").GetString() ?? throw new InvalidDataException("Missing scene layout pin.");
            var definitions = root.GetProperty("doors");
            if (definitions.ValueKind != JsonValueKind.Array || definitions.GetArrayLength() is < 1 or > 16)
                throw new InvalidDataException("Interior scene requires one to sixteen explicit door bindings.");
            var doors = new List<PortalSingleLeafDoorBundle>(); var geometries = new HashSet<int>();
            var floors = new Dictionary<int, FloorMeshDescription>(); var sources = new Dictionary<string, AssetSourceIdentity>(StringComparer.Ordinal);
            var cache = new OwnedAssetSourceCache(resolve, new());
            foreach (var definition in definitions.EnumerateArray())
            {
                var door = PortalSingleLeafDoorBundleReader.Load(cache.Read, member, layoutSha, definition.GetProperty("cell").GetInt32(),
                    definition.GetProperty("portal").GetInt32(), tableSha256,
                    definition.GetProperty("leaf_sha256").GetString() ?? throw new InvalidDataException("Missing scene leaf pin."));
                int geometry = door.Layout.Cells[door.Cell].Portals[door.Portal].GeometryIndex;
                if (!geometries.Add(geometry)) throw new InvalidDataException("Duplicate physical scene doorway selection.");
                foreach (var identity in door.Sources.Values)
                {
                    if (sources.TryGetValue(identity.Path, out var prior) && prior != identity)
                        throw new InvalidDataException("Scene asset identity changed during bundle loading.");
                    sources[identity.Path] = identity;
                }
                foreach (var pair in door.Floors) floors.TryAdd(pair.Key, pair.Value);
                doors.Add(door);
            }
            return new(doors[0].Layout, new ReadOnlyDictionary<int, FloorMeshDescription>(floors), Array.AsReadOnly(doors.ToArray()),
                Array.AsReadOnly(doors.Select(d => new InteriorDoorBinding(d.Cell, d.Portal, d.Leaf)).ToArray()),
                new ReadOnlyDictionary<string, AssetSourceIdentity>(sources), digest, LegacyAssetPath.Normalize(member));
        }
        catch (Exception error) when (error is JsonException or KeyNotFoundException or FormatException or InvalidOperationException or OverflowException)
        { throw new InvalidDataException("Malformed interior scene specification.", error); }
    }
}
