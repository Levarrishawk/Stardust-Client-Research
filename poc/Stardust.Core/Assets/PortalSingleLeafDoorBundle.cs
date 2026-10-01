using System.Collections.ObjectModel;

namespace Stardust.Core.Assets;

public sealed record PortalSingleLeafDoorBundle(PortalLayoutDescription Layout, int Cell, int Portal, int TargetCell,
    SingleLeafDoorAsset Leaf, AppearanceTransform DoorToCell, IReadOnlyDictionary<int, FloorMeshDescription> Floors,
    IReadOnlyDictionary<string, AssetSourceIdentity> Sources)
{
    public SingleLeafDoorRuntime CreateRuntime(AppearanceTransform? cellToWorld = null) =>
        new(Leaf.Selection.Style, Leaf.Extent, (cellToWorld ?? AppearanceTransform.Identity).Compose(DoorToCell), Cell);

    // Optional modern fixture construction: walk the selected connected sheet from its portal edge.
    public (CellFloorLocator Start, System.Numerics.Vector3 End) ConnectedApproach(float distance = .8f, FloorStepPolicy? stepPolicy = null)
    {
        if (!float.IsFinite(distance) || distance <= 0 || distance > 100) throw new ArgumentOutOfRangeException(nameof(distance));
        var policy = stepPolicy ?? new(); policy.Validate();
        var link = new PortalFloorTransferQuery(Layout, Floors).Links.Where(l => l.SourceCell == Cell && l.SourcePortal == Portal)
            .OrderBy(l =>
            {
                var f = Floors[Cell]; var t = f.Triangles[l.SourceTriangle];
                var midpoint = (f.Vertices[t.VertexIndices[l.SourceEdge]] + f.Vertices[t.VertexIndices[(l.SourceEdge + 1) % 3]]) * .5f;
                return System.Numerics.Vector3.DistanceSquared(midpoint, DoorToCell.Translation);
            }).ThenBy(l => l.SourceTriangle).ThenBy(l => l.SourceEdge).First();
        var source = ConnectedEndpoint(Floors[Cell], link.SourceTriangle, link.SourceEdge, distance, policy);
        var target = ConnectedEndpoint(Floors[TargetCell], link.TargetTriangle, link.TargetEdge, distance, policy);
        return (new(Cell, source.TriangleIndex, source.Position), target.Position);
    }

    internal static FloorWalkResult ConnectedEndpoint(FloorMeshDescription floor, int triangle, int edge, float distance, FloorStepPolicy policy)
    {
        var t = floor.Triangles[triangle];
        var a = floor.Vertices[t.VertexIndices[edge]]; var b = floor.Vertices[t.VertexIndices[(edge + 1) % 3]];
        var midpoint = (a + b) * .5f;
        var center = (floor.Vertices[t.VertexIndices[0]] + floor.Vertices[t.VertexIndices[1]] + floor.Vertices[t.VertexIndices[2]]) / 3;
        var inward = new System.Numerics.Vector3(-(b.Z - a.Z), 0, b.X - a.X);
        if (inward.LengthSquared() <= 1e-12f) throw new InvalidDataException("Portal edge lacks a horizontal approach normal.");
        inward = System.Numerics.Vector3.Normalize(inward);
        if (System.Numerics.Vector3.Dot(inward, center - midpoint) < 0) inward = -inward;
        var walker = new SingleCellFloorWalker(floor);
        if (!walker.TryCanonicalContact(triangle, midpoint, out var start)) throw new InvalidDataException("Portal midpoint is not a reusable supported contact.");
        var desired = midpoint + inward * distance;
        var query = new SingleCellFloorStepBoxWalker(floor, Array.Empty<PortalBarrierCollider>(), policy);
        for (int count = 0; count < 8; count++)
        {
            var result = query.Walk(triangle, start, desired, System.Numerics.Vector3.Zero, _ => true);
            if (result.Movement.Floor.Status == FloorWalkStatus.Accepted) return result.Movement.Floor;
            if (result.Steps == 0 || System.Numerics.Vector3.DistanceSquared(start, result.Movement.Floor.Position) <= 1e-12f)
                throw new InvalidDataException("Selected connected doorway approach is obstructed: " + result.Movement.Floor.Status);
            triangle = result.Movement.Floor.TriangleIndex; start = result.Movement.Floor.Position;
        }
        throw new InvalidDataException("Selected connected doorway approach exceeds eight step requests.");
    }

    // Modern deterministic inspection points; this is not an original navigation rule.
    public (CellFloorLocator Start, System.Numerics.Vector3 End) InspectionApproach(float distance = .8f)
    {
        if (!float.IsFinite(distance) || distance <= 0 || distance > 100) throw new ArgumentOutOfRangeException(nameof(distance));
        var link = new PortalFloorTransferQuery(Layout, Floors).Links.First(l => l.SourceCell == Cell && l.SourcePortal == Portal);
        FloorMeshDescription f = Floors[Cell]; FloorTriangle t = f.Triangles[link.SourceTriangle];
        var center = (f.Vertices[t.VertexIndices[0]] + f.Vertices[t.VertexIndices[1]] + f.Vertices[t.VertexIndices[2]]) / 3;
        var normal = DoorToCell.TransformDirection(System.Numerics.Vector3.UnitZ);
        float side = Math.Sign(System.Numerics.Vector3.Dot(center - DoorToCell.Translation, normal));
        if (side == 0) throw new InvalidDataException("Ambiguous door approach side.");
        var a = DoorToCell.TransformPoint(new(0, 0, side * distance)); var b = DoorToCell.TransformPoint(new(0, 0, -side * distance));
        var y = System.Numerics.Vector3.UnitY * 5;
        FloorSegmentHit start = new FloorSurfaceQuery(f).IntersectSegment(a + y, a - y) ?? throw new InvalidDataException("Door approach lacks source floor.");
        FloorSegmentHit end = new FloorSurfaceQuery(Floors[TargetCell]).IntersectSegment(b + y, b - y) ?? throw new InvalidDataException("Door approach lacks target floor.");
        return (new(Cell, start.TriangleIndex, start.Position), end.Position);
    }
}

/// <summary>One explicit named original portal reference, primary leaf and both paired floors. No inferred transform or style fallback.</summary>
public static class PortalSingleLeafDoorBundleReader
{
    public static PortalSingleLeafDoorBundle Load(Func<string, TreLookupResult> resolve, string layoutPath, string layoutSha256,
        int cell, int portal, string tableSha256, string leafSha256)
    {
        OwnedAssetSourceCache cache = new(resolve, new());
        PortalLayoutDescription layout = PortalLayoutDecoder.Decode(cache.ReadPinned(layoutPath, layoutSha256));
        if (cell < 0 || cell >= layout.Cells.Count || portal < 0 || portal >= layout.Cells[cell].Portals.Count)
            throw new InvalidDataException("Invalid door instance reference.");
        PortalReference reference = layout.Cells[cell].Portals[portal];
        if (!PortalDoorFlags.FromReference(layout.Version, reference).CanCreateDoor || reference.DoorName.Length == 0 ||
            reference.TransformFlag != 1 || !reference.DoorTransform.HasValue || reference.TargetCell == cell)
            throw new InvalidDataException("Door instance requires an eligible named reference with an explicit transform.");
        SingleLeafDoorAsset leaf = SingleLeafDoorAssetReader.Load(cache.Read, reference.DoorName, tableSha256, leafSha256);
        if (leaf.Selection.UsedFallback) throw new InvalidDataException("Door instance style fallback is outside this slice.");
        Dictionary<int, FloorMeshDescription> floors = new();
        foreach (int owner in new[] { cell, reference.TargetCell })
        {
            string path = layout.Cells[owner].FloorPath ?? throw new InvalidDataException("Door owner lacks a floor.");
            TreLookupResult source = cache.Read(path);
            if (source.Payload is null || source.Kind != TreLookupKind.Present) throw new InvalidDataException("Door owner floor missing/masked.");
            floors.Add(owner, FloorMeshDecoder.Decode(source.Payload));
        }
        PortalFloorTransferQuery transfer = new(layout, floors);
        if (!transfer.Links.Any(l => l.SourceCell == cell && l.SourcePortal == portal))
            throw new InvalidDataException("Door reference lacks a uniquely paired floor edge.");
        PortalSingleLeafDoorBundle result = new(layout, cell, portal, reference.TargetCell, leaf, reference.DoorTransform.Value,
            new ReadOnlyDictionary<int, FloorMeshDescription>(floors), cache.Sources);
        using SingleLeafDoorRuntime validation = result.CreateRuntime();
        return result;
    }
}
