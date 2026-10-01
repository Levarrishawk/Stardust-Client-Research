using System.Numerics;

namespace Stardust.Core.Assets;

public sealed record PortalDoorActorState(CellFloorLocator Locator, Vector3 WorldPosition, DoorLeafState Door,
    long Frame, long TotalTransfers, PortalBarrierFloorResult? Movement, bool? EntryAllowed);

/// <summary>Owned offline actor scheduler with one shared physical leaf and two explicitly compatible incident references.</summary>
public sealed class PortalDoorActorSimulation : IDisposable
{
    private readonly AppearanceTransform owner;
    private readonly Vector3 half;
    private readonly CellFloorLocator initial;
    private readonly SingleLeafDoorRuntime leaf;
    private readonly PortalBarrierFloorWalker? walker;
    private readonly PortalDoorBoxFloorWalker? preciseWalker;
    private readonly ulong actor;
    private long frame, total;
    private bool disposed;
    public PortalDoorActorState State { get; private set; }
    public PortalDoorActorSimulation(PortalSingleLeafDoorBundle bundle, CellFloorLocator initial,
        AppearanceTransform? ownerPose = null, Vector3? actorHalfExtents = null, ulong actorHandle = 1,bool usePreciseMovement=false,FloorStepPolicy? stepPolicy=null)
    {
        ArgumentNullException.ThrowIfNull(bundle);
        if(stepPolicy is not null&&!usePreciseMovement)throw new ArgumentException("Step policy requires precise movement.");
        stepPolicy?.Validate();
        owner = ownerPose ?? AppearanceTransform.Identity; half = actorHalfExtents ?? new(.25f, .9f, .25f); actor = actorHandle;
        if (actor == 0 || !Finite(half) || half.X <= 0 || half.Y <= 0 || half.Z <= 0 || !bundle.Floors.ContainsKey(initial.CellIndex) || !Finite(initial.LocalPosition))
            throw new InvalidDataException("Invalid persistent actor inputs.");
        PortalFloorTransferQuery pairing = new(bundle.Layout, bundle.Floors);
        var links = pairing.Links.Where(l => l.SourceCell == bundle.Cell && l.SourcePortal == bundle.Portal).ToArray();
        if (links.Length == 0 || links.Any(l => l.TargetCell != bundle.TargetCell || l.TargetPortal != links[0].TargetPortal))
            throw new InvalidDataException("Ambiguous paired actor doorway.");
        PortalReference reverse = bundle.Layout.Cells[bundle.TargetCell].Portals[links[0].TargetPortal];
        if (!PortalDoorFlags.FromReference(bundle.Layout.Version, reverse).CanCreateDoor || reverse.DoorName != bundle.Leaf.Selection.Style.Name ||
            reverse.TargetCell != bundle.Cell || reverse.TransformFlag != 1 || reverse.DoorTransform != bundle.DoorToCell)
            throw new InvalidDataException("Shared physical leaf requires matching paired style and authored pose.");
        var poses = bundle.Floors.Keys.ToDictionary(c => c, _ => owner);
        // Validate owner and initial locator before allocating retained contact state.
        var initialWalk = new PortalFloorTransferQuery(bundle.Layout, bundle.Floors, poses).Walk(initial, owner.TransformPoint(initial.LocalPosition), _ => new(true, true));
        if (initialWalk.Status != CellFloorWalkStatus.Accepted || initialWalk.Locator.CellIndex != initial.CellIndex || Vector3.Distance(initialWalk.Locator.LocalPosition, initial.LocalPosition) > 1e-4f)
            throw new InvalidDataException("Persistent actor initial floor locator is invalid.");
        this.initial = initialWalk.Locator;
        leaf = new(bundle.Leaf.Selection.Style, bundle.Leaf.Extent, owner.Compose(bundle.DoorToCell), bundle.Cell, bundle.TargetCell);
        try
        {
            var bindings=new Dictionary<(int,int),SingleLeafDoorRuntime>{{(bundle.Cell,bundle.Portal),leaf},{(bundle.TargetCell,links[0].TargetPortal),leaf}};
            if(usePreciseMovement)preciseWalker=new(bundle.Layout,bundle.Floors,poses,bindings,true,stepPolicy);
            else walker=new(bundle.Layout,bundle.Floors,poses,bindings,true);
        }
        catch { leaf.Dispose(); throw; }
        State = new(this.initial, owner.TransformPoint(this.initial.LocalPosition), leaf.State, 0, 0, null, null);
    }
    public PortalDoorActorState Tick(float seconds, Vector3 worldDelta, bool? entryAllowed)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        Vector3 desired = State.WorldPosition + worldDelta;
        if (!float.IsFinite(seconds) || seconds < 0 || seconds > 1 || !Finite(worldDelta) || !Finite(desired) || frame == long.MaxValue || total == long.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(seconds), "Finite bounded tick and world motion required.");
        Vector3 actorCenter = State.WorldPosition + new Vector3(0, half.Y, 0);
        if (!Finite(actorCenter)) throw new InvalidDataException("Actor center overflow.");
        leaf.NotifyContact(actor, State.Locator.CellIndex, State.Locator.CellIndex, actorCenter, entryAllowed, frame);
        leaf.Advance(frame, seconds, id => id == actor ? State.WorldPosition : null, entryAllowed);
        PortalBarrierFloorResult movement = preciseWalker is not null?preciseWalker.Walk(State.Locator,desired,half,_=>new(entryAllowed,true)):
            walker!.Walk(State.Locator,desired,half,_=>new(entryAllowed,true));
        // One owned leaf is shared by both directions; the returned locator becomes the next tick's source.
        total += movement.Movement.Transfers; frame++;
        State = new(movement.Movement.Locator, movement.Movement.WorldPosition, leaf.State, frame, total, movement, entryAllowed);
        return State;
    }
    public void Reset()
    {
        ObjectDisposedException.ThrowIf(disposed, this);leaf.Reset();frame = total = 0;
        State = new(initial, owner.TransformPoint(initial.LocalPosition), leaf.State, 0, 0, null, null);
    }
    public void Dispose()
    {
        if (disposed) return;leaf.Dispose();State = State with { Door = leaf.State, Movement = null };disposed = true;
    }
    private static bool Finite(Vector3 v) => float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z);
}
