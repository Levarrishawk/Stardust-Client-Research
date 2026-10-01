using System.Numerics;

namespace Stardust.Core.Assets;

public enum PortalActorContactKind { PermissionBarrier, DoorLeaf, StaticTriangle }
public sealed record PortalBarrierFloorResult(CellFloorWalkResult Movement, PortalBarrierContact? Contact, PortalFloorLink? Barrier, string? Diagnostic = null, PortalActorContactKind? ContactKind = null,int Steps=0,
    int? ObstacleCell=null,int? ObstacleTriangle=null);

/// <summary>Offline source-cell actor-box barrier and optional leaf inspection before one transfer. Rigid yaw owners and level motion only.</summary>
public sealed class PortalBarrierFloorWalker
{
    private readonly PortalFloorTransferQuery transfers;
    private readonly Dictionary<int, SingleCellFloorWalker> floors = new();
    private readonly Dictionary<PortalFloorLink, PortalBarrierCollider> barriers = new();
    private readonly Dictionary<PortalFloorLink, SingleLeafDoorRuntime> leaves = new();
    private readonly Dictionary<int, AppearanceTransform> poses = new();
    private readonly bool allowSeparatingContacts;
    public int BarrierCount => barriers.Count;
    public PortalBarrierFloorWalker(PortalLayoutDescription layout, IReadOnlyDictionary<int, FloorMeshDescription> cellFloors,
        IReadOnlyDictionary<int, AppearanceTransform>? ownerPoses = null,
        IReadOnlyDictionary<(int Cell, int Portal), SingleLeafDoorRuntime>? doorLeaves = null, bool allowSeparatingContacts = false)
    {
        this.allowSeparatingContacts = allowSeparatingContacts;
        transfers = new(layout, cellFloors, ownerPoses);
        foreach (var pair in cellFloors)
        {
            floors.Add(pair.Key, new(pair.Value));
            poses.Add(pair.Key, ownerPoses is not null && ownerPoses.TryGetValue(pair.Key, out var supplied) ? supplied : AppearanceTransform.Identity);
        }
        foreach (PortalFloorLink link in transfers.Links)
        {
            PortalReference reference = layout.Cells[link.SourceCell].Portals[link.SourcePortal];
            if (PortalDoorFlags.FromReference(layout.Version, reference).CanCreateDoor)
            {
                PortalBarrierCollider local = PortalBarrierCollider.FromReference(layout, reference);
                barriers.Add(link, new(local.Minimum, local.Maximum, poses[link.SourceCell].Compose(local.Pose)));
            }
            if (doorLeaves is not null && doorLeaves.TryGetValue((link.SourceCell, link.SourcePortal), out var leaf))
            {
                if (!leaf.AcceptsCell(link.SourceCell) || !barriers.ContainsKey(link)) throw new InvalidDataException("Door leaf owner/eligibility mismatch.");
                leaves.Add(link, leaf);
            }
        }
        if (doorLeaves is not null && doorLeaves.Keys.Any(key => !leaves.Keys.Any(l => l.SourceCell == key.Cell && l.SourcePortal == key.Portal)))
            throw new InvalidDataException("Door leaf lacks a source floor link.");
    }
    public PortalBarrierFloorResult Walk(CellFloorLocator start, Vector3 desired, Vector3 actorHalfExtents, Func<PortalFloorLink, PortalTransferPermission> permission, int maximumCrossings = 4096)
    {
        ArgumentNullException.ThrowIfNull(permission);
        if (!float.IsFinite(actorHalfExtents.X) || !float.IsFinite(actorHalfExtents.Y) || !float.IsFinite(actorHalfExtents.Z) || actorHalfExtents.X < 0 || actorHalfExtents.Y < 0 || actorHalfExtents.Z < 0)
            throw new InvalidDataException("Invalid actor half extents.");
        Dictionary<PortalFloorLink, PortalTransferPermission> decisions = new();
        // Capture physical poses before invoking external policy callbacks. The query never advances or owns a door.
        var leafSnapshots = leaves.Where(p => p.Key.SourceCell == start.CellIndex).Select(p => (p.Key, Collider: p.Value.SnapshotLeafCollider())).ToArray();
        PortalTransferPermission Current(PortalFloorLink link)
        {
            if (!decisions.TryGetValue(link, out var value)) decisions.Add(link, value = permission(link));
            return value;
        }
        CellFloorWalkPath path = transfers.Trace(start, desired, Current, maximumCrossings);
        CellFloorWalkResult movement = path.Result;
        if (movement.Status == CellFloorWalkStatus.InvalidStart || !floors.ContainsKey(start.CellIndex)) return new(movement, null, null);
        AppearanceTransform owner = poses[start.CellIndex]; Vector3 worldStart = owner.TransformPoint(start.LocalPosition);
        PortalBarrierFloorResult Unsupported() => new(new(CellFloorWalkStatus.FloorStopped, start, worldStart, 0, 0, null, null), null, null, "UnsupportedNonlevelBarrierSweep");
        // This integration is deliberately level-only; sloped/vertical sweeps need the full floor contact path.
        if (Math.Abs(worldStart.Y - desired.Y) > 1e-4f || Math.Abs(movement.WorldPosition.Y - worldStart.Y) > 1e-4f) return Unsupported();
        if(path.Floors.Any(f=>f.Path.Segments.Any(s=>Math.Abs(s.Start.Y+poses[f.CellIndex].Translation.Y-worldStart.Y)>1e-4 || Math.Abs(s.End.Y+poses[f.CellIndex].Translation.Y-worldStart.Y)>1e-4)))return Unsupported();
        PortalBarrierContact? nearest = null; PortalFloorLink? chosen = null; CellFloorWalkResult? stopped = null;
        PortalActorContactKind? kind = null;
        Vector3 offset = new(0, actorHalfExtents.Y, 0);
        void Consider(PortalFloorLink link, PortalBarrierContact hit, CellFloorWalkStatus status, PortalActorContactKind contactKind)
        {
            if (hit.Status == PortalBarrierContactStatus.Clear || hit.Parameter > movement.Parameter || nearest is not null && hit.Parameter >= nearest.Parameter) return;
            Vector3 contactWorld = hit.Center - offset, delta = contactWorld - owner.Translation;
            Vector3 contactLocal = new(owner.RowX.X * delta.X + owner.RowY.X * delta.Y + owner.RowZ.X * delta.Z,
                owner.RowX.Y * delta.X + owner.RowY.Y * delta.Y + owner.RowZ.Y * delta.Z,
                owner.RowX.Z * delta.X + owner.RowY.Z * delta.Y + owner.RowZ.Z * delta.Z);
            FloorWalkResult landing = floors[start.CellIndex].Walk(start.TriangleIndex, start.LocalPosition, contactLocal, maximumCrossings);
            if (landing.Status != FloorWalkStatus.Accepted)
            {
                if (contactKind == PortalActorContactKind.DoorLeaf && landing.Status == FloorWalkStatus.PortalBoundary)
                {
                    stopped = new(CellFloorWalkStatus.PortalBlocked, start, worldStart, 0, 0, link, null);
                    nearest = hit; chosen = link; kind = contactKind;
                }
                return;
            }
            if (Math.Abs(landing.Position.Y - start.LocalPosition.Y) > 1e-4f)
            {
                stopped = new(CellFloorWalkStatus.FloorStopped, start, worldStart, 0, 0, link, null);
                nearest = hit; chosen = link; kind = contactKind; return;
            }
            stopped = new(status, new(start.CellIndex, landing.TriangleIndex, landing.Position), owner.TransformPoint(landing.Position), hit.Parameter, 0, link, landing);
            nearest = hit; chosen = link; kind = contactKind;
        }
        foreach (var pair in barriers.Where(p => p.Key.SourceCell == start.CellIndex))
        {
            PortalTransferPermission policy = Current(pair.Key);
            bool? allowed = !policy.AllowEntry.HasValue || !policy.BarrierOpen.HasValue ? null : policy.AllowEntry.Value && policy.BarrierOpen.Value;
            PortalBarrierContact hit = pair.Value.Sweep(worldStart + offset, desired + offset, actorHalfExtents, allowed, allowSeparatingContacts);
            CellFloorWalkStatus status = allowed is null ? CellFloorWalkStatus.PermissionUnknown : !policy.AllowEntry!.Value ? CellFloorWalkStatus.EntryDenied : CellFloorWalkStatus.PortalBlocked;
            Consider(pair.Key, hit, status, PortalActorContactKind.PermissionBarrier);
        }
        foreach (var snapshot in leafSnapshots)
            Consider(snapshot.Key, snapshot.Collider.Sweep(worldStart + offset, desired + offset, actorHalfExtents, false, allowSeparatingContacts), CellFloorWalkStatus.PortalBlocked, PortalActorContactKind.DoorLeaf);
        string? diagnostic = stopped?.FloorResult is null && nearest is not null ? "UnsupportedSourceContactLanding" : null;
        return new(stopped ?? movement, nearest, chosen, diagnostic, kind);
    }
}
