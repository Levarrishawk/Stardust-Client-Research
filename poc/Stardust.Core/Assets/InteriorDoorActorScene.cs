using System.Collections.ObjectModel;
using System.Numerics;

namespace Stardust.Core.Assets;

public sealed record InteriorDoorBinding(int Cell, int Portal, SingleLeafDoorAsset Leaf);
public sealed record ActorSphereVolume(Vector3 WorldCenterOffset, float Radius);
public sealed record InteriorDoorActorSceneState(CellFloorLocator Locator, Vector3 WorldPosition,
    IReadOnlyDictionary<int, DoorLeafState> Doors, long Frame, long TotalTransfers, PortalBarrierFloorResult? Movement);

/// <summary>Owned bounded modern actor scene. One controller per explicit physical doorway, one transfer/step per tick.</summary>
public sealed class InteriorDoorActorScene : IDisposable
{
    private readonly AppearanceTransform owner;
    private readonly Vector3 half;
    private readonly ActorSphereVolume? sphere;
    private readonly CellFloorLocator initial;
    private readonly ulong actor;
    private readonly Dictionary<int, SingleLeafDoorRuntime> doors = new();
    private readonly Dictionary<int, PortalFloorLink[]> incident = new();
    private readonly PortalDoorBoxFloorWalker? walker;
    private readonly PortalDoorSphereFloorWalker? sphereWalker;
    private readonly PortalFloorTriangleSphereWalker? triangleGuard;
    private bool disposed, ticking;
    public IReadOnlyList<PortalFloorLink> Links { get; }
    public bool HoldClosingLeaves { get; }
    public ActorSphereVolume? SphereVolume => sphere;
    public bool HasTriangleGuard => triangleGuard is not null;
    public InteriorDoorActorSceneState State { get; private set; }

    public InteriorDoorActorScene(PortalLayoutDescription layout, IReadOnlyDictionary<int, FloorMeshDescription> floors,
        IReadOnlyList<InteriorDoorBinding> bindings, CellFloorLocator initial, AppearanceTransform? ownerPose = null,
        Vector3? actorHalfExtents = null, ulong actorHandle = 1, FloorStepPolicy? stepPolicy = null, bool holdClosingLeaves = false,
        ActorSphereVolume? actorSphere = null,
        IReadOnlyDictionary<int,IReadOnlyList<CollisionTriangle>>? staticTriangles = null,int maximumTriangles=2048)
    {
        ArgumentNullException.ThrowIfNull(layout); ArgumentNullException.ThrowIfNull(floors); ArgumentNullException.ThrowIfNull(bindings);
        if (floors.Count is < 1 or > 64 || bindings.Count > 16) throw new InvalidDataException("Interior scene resource budget.");
        owner = ownerPose ?? AppearanceTransform.Identity; half = actorHalfExtents ?? new(.25f, .9f, .25f); actor = actorHandle;
        if (actorSphere is not null)
        {
            if (actorHalfExtents.HasValue || stepPolicy is not null || !Bounded(actorSphere.WorldCenterOffset) ||
                !float.IsFinite(actorSphere.Radius) || actorSphere.Radius <= 0 || actorSphere.Radius > 100)
                throw new InvalidDataException("Explicit sphere volume requires bounded offset/radius and no box or step policy.");
            sphere = new(actorSphere.WorldCenterOffset, actorSphere.Radius);
        }
        HoldClosingLeaves = holdClosingLeaves;
        if (actor == 0 || !Finite(half) || half.X <= 0 || half.Y <= 0 || half.Z <= 0 || half.X > 100 || half.Y > 100 || half.Z > 100 || !Finite(initial.LocalPosition))
            throw new InvalidDataException("Invalid interior actor inputs.");
        stepPolicy?.Validate();
        var poses = floors.Keys.ToDictionary(c => c, _ => owner);
        var pairing = new PortalFloorTransferQuery(layout, floors, poses); Links = pairing.Links;
        if (Links.Count > 128) throw new InvalidDataException("Interior scene link budget.");
        var eligible = Links.Where(l => PortalDoorFlags.FromReference(layout.Version, layout.Cells[l.SourceCell].Portals[l.SourcePortal]).CanCreateDoor).ToArray();
        // At most 32 directional barriers plus 32 leaf incidences in every captured query.
        if (eligible.Length > 32) throw new InvalidDataException("Interior scene collider budget.");
        var valid = pairing.Walk(initial, owner.TransformPoint(initial.LocalPosition), _ => new(true, true));
        if (valid.Status != CellFloorWalkStatus.Accepted || valid.Locator.CellIndex != initial.CellIndex)
            throw new InvalidDataException("Invalid interior scene initial locator.");
        this.initial = valid.Locator;
        if (sphere is not null && !Bounded(valid.WorldPosition + sphere.WorldCenterOffset))
            throw new InvalidDataException("Initial sphere center bounds.");
        if (staticTriangles is not null)
        {
            if (sphere is null) throw new InvalidDataException("Static triangle guard requires an explicit sphere volume.");
            triangleGuard = new(layout,floors,staticTriangles,poses,maximumTriangles);
        }
        var leafBindings = new Dictionary<(int, int), SingleLeafDoorRuntime>();
        try
        {
            foreach (var binding in bindings)
            {
                ArgumentNullException.ThrowIfNull(binding); ArgumentNullException.ThrowIfNull(binding.Leaf);
                var links = eligible.Where(l => l.SourceCell == binding.Cell && l.SourcePortal == binding.Portal).ToArray();
                if (links.Length == 0 || links.Any(l => l.GeometryIndex != links[0].GeometryIndex || l.TargetCell != links[0].TargetCell || l.TargetPortal != links[0].TargetPortal))
                    throw new InvalidDataException("Interior leaf requires an unambiguous paired reference.");
                var link = links[0]; var reference = layout.Cells[link.SourceCell].Portals[link.SourcePortal];
                var reverse = layout.Cells[link.TargetCell].Portals[link.TargetPortal];
                if (binding.Leaf.Selection.UsedFallback || reference.DoorName != binding.Leaf.Selection.Style.Name ||
                    !PortalDoorFlags.FromReference(layout.Version, reverse).CanCreateDoor || reverse.DoorName != reference.DoorName ||
                    reference.TransformFlag != 1 || reverse.TransformFlag != 1 || !reference.DoorTransform.HasValue || reverse.DoorTransform != reference.DoorTransform)
                    throw new InvalidDataException("Interior physical leaf requires matching paired style and pose.");
                if (doors.ContainsKey(link.GeometryIndex)) throw new InvalidDataException("Duplicate physical interior doorway.");
                var leaf = new SingleLeafDoorRuntime(binding.Leaf.Selection.Style, binding.Leaf.Extent,
                    owner.Compose(reference.DoorTransform.Value), link.SourceCell, link.TargetCell);
                doors.Add(link.GeometryIndex, leaf);
                incident.Add(link.GeometryIndex, eligible.Where(l => l.GeometryIndex == link.GeometryIndex).ToArray());
                leafBindings.Add((link.SourceCell, link.SourcePortal), leaf); leafBindings.Add((link.TargetCell, link.TargetPortal), leaf);
            }
            if (eligible.Any(l => !leafBindings.ContainsKey((l.SourceCell, l.SourcePortal))))
                throw new InvalidDataException("Every loaded paired eligible door requires an explicit leaf.");
            if (sphere is null) walker = new(layout, floors, poses, leafBindings, true, stepPolicy);
            else sphereWalker = new(layout, floors, poses, leafBindings, true);
            State = Snapshot(this.initial, owner.TransformPoint(this.initial.LocalPosition), 0, 0, null);
        }
        catch { foreach (var leaf in doors.Values) leaf.Dispose(); throw; }
    }

    public InteriorDoorActorSceneState Tick(float seconds, Vector3 worldDelta, Func<PortalFloorLink, PortalTransferPermission> permission)
    {
        ObjectDisposedException.ThrowIf(disposed, this); ArgumentNullException.ThrowIfNull(permission);
        if (ticking) throw new InvalidOperationException("Interior actor ticks cannot reenter.");
        var desired = State.WorldPosition + worldDelta;
        var center = State.WorldPosition + (sphere?.WorldCenterOffset ?? new Vector3(0, half.Y, 0));
        if (!float.IsFinite(seconds) || seconds < 0 || seconds > 1 || !Finite(worldDelta) || Math.Abs(worldDelta.X) > 100 || Math.Abs(worldDelta.Y) > 100 || Math.Abs(worldDelta.Z) > 100 || !Finite(desired) || !Finite(center) ||
            State.Frame == long.MaxValue || State.TotalTransfers == long.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(seconds), "Finite bounded interior tick required.");
        if (sphere is not null && (!Bounded(center) || !Bounded(desired + sphere.WorldCenterOffset)))
            throw new ArgumentOutOfRangeException(nameof(worldDelta), "Bounded sphere tick center required.");
        ticking = true;
        try
        {
            // All external callbacks finish before any controller or actor mutation.
            var permissions = Links.ToDictionary(l => l, permission);
            foreach (var group in incident.Values.SelectMany(l => l).GroupBy(l => (l.SourceCell, l.SourcePortal)))
                if (group.Select(l => permissions[l]).Distinct().Count() != 1)
                    throw new InvalidDataException("A physical doorway incidence requires one consistent permission.");
            // Static path work/geometry is checked before controller mutation. Permission lookups now use owned decisions only.
            var triangles = triangleGuard?.Walk(State.Locator,desired,sphere!.WorldCenterOffset,sphere.Radius,l=>permissions[l],true);
            // Closing safety retains the established conservative box policy. For a sphere this cube encloses its full volume.
            var closingFeet = sphere is null ? State.WorldPosition : center - new Vector3(0, sphere.Radius, 0);
            var closingHalf = sphere is null ? half : new Vector3(sphere.Radius);
            DoorActorBody[]? closingBodies = HoldClosingLeaves ? new[] { new DoorActorBody(actor, State.Locator.CellIndex, closingFeet, closingHalf) } : null;
            foreach (var pair in doors.OrderBy(p => p.Key))
            {
                var leaf = pair.Value; bool present = leaf.AcceptsCell(State.Locator.CellIndex);
                var outgoing = incident[pair.Key].FirstOrDefault(l => l.SourceCell == State.Locator.CellIndex);
                bool? allowed = outgoing is null ? null : Combine(permissions[outgoing]);
                if (present) leaf.NotifyContact(actor, State.Locator.CellIndex, State.Locator.CellIndex, center, allowed, State.Frame);
                leaf.Advance(State.Frame, seconds, id => present && id == actor ? State.WorldPosition : null, allowed, closingBodies);
            }
            var movement = sphere is null ? walker!.Walk(State.Locator, desired, half, l => permissions[l]) :
                sphereWalker!.Walk(State.Locator, desired, sphere.WorldCenterOffset, sphere.Radius, l => permissions[l]);
            if (triangles?.Contact is not null)
            {
                var hit=triangles.Contact.World;
                double doorTime=movement.Contact?.Parameter??movement.Movement.Parameter;
                if(hit.Parameter<doorTime||hit.Parameter==doorTime&&movement.Contact is null)
                {
                    var status=hit.Status==TriangleSphereContactStatus.InitialOverlap?PortalBarrierContactStatus.InitialOverlap:PortalBarrierContactStatus.Blocked;
                    movement=new(triangles.Movement,new(status,hit.Parameter,hit.Center.ToVector3(),hit.Normal),null,
                        triangles.Diagnostic,PortalActorContactKind.StaticTriangle,ObstacleCell:triangles.Contact.ColliderCell,ObstacleTriangle:hit.TriangleId);
                }
            }
            if (triangleGuard is not null && (movement.Movement.WorldPosition!=State.WorldPosition||movement.Movement.Locator!=State.Locator))
            {
                // A float landing chosen by either query must remain clear of both obstacle families.
                var destination=movement.Movement;
                var triangleClear=triangleGuard.Walk(destination.Locator,destination.WorldPosition,sphere!.WorldCenterOffset,sphere.Radius,l=>permissions[l],true);
                var doorClear=sphereWalker!.Walk(destination.Locator,destination.WorldPosition,sphere.WorldCenterOffset,sphere.Radius,l=>permissions[l]);
                if(triangleClear.Status!=FloorBoxWalkStatus.Accepted||doorClear.Movement.Status!=CellFloorWalkStatus.Accepted||
                    triangleClear.Contact is not null||doorClear.Contact is not null)
                {
                    var held=movement.Movement with{Status=CellFloorWalkStatus.FloorStopped,Locator=State.Locator,
                        WorldPosition=State.WorldPosition,Parameter=0,Transfers=0,Link=null,FloorResult=null};
                    movement=movement with{Movement=held,Diagnostic="UnsupportedCombinedSphereLanding"};
                }
            }
            State = Snapshot(movement.Movement.Locator, movement.Movement.WorldPosition, State.Frame + 1,
                State.TotalTransfers + movement.Movement.Transfers, movement);
            return State;
        }
        finally { ticking = false; }
    }

    public void Reset()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (ticking) throw new InvalidOperationException("Cannot reset during an interior tick.");
        foreach (var leaf in doors.Values) leaf.Reset();
        State = Snapshot(initial, owner.TransformPoint(initial.LocalPosition), 0, 0, null);
    }
    public void Dispose()
    {
        if (disposed) return;
        if (ticking) throw new InvalidOperationException("Cannot dispose during an interior tick.");
        foreach (var leaf in doors.Values) leaf.Dispose();
        State = Snapshot(State.Locator, State.WorldPosition, State.Frame, State.TotalTransfers, null); disposed = true;
    }
    private InteriorDoorActorSceneState Snapshot(CellFloorLocator locator, Vector3 world, long frame, long total, PortalBarrierFloorResult? movement) =>
        new(locator, world, new ReadOnlyDictionary<int, DoorLeafState>(doors.ToDictionary(p => p.Key, p => p.Value.State)), frame, total, movement);
    private static bool? Combine(PortalTransferPermission p) => p.AllowEntry.HasValue && p.BarrierOpen.HasValue ? p.AllowEntry.Value && p.BarrierOpen.Value : null;
    private static bool Finite(Vector3 p) => float.IsFinite(p.X) && float.IsFinite(p.Y) && float.IsFinite(p.Z);
    private static bool Bounded(Vector3 p) => Finite(p) && Math.Abs(p.X) <= 1e6f && Math.Abs(p.Y) <= 1e6f && Math.Abs(p.Z) <= 1e6f;
}
