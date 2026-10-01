using System.Numerics;

namespace Stardust.Core.Assets;

public enum DoorContactResult { Retained, Duplicate, WrongCell, OutsideTrigger, EntryDenied, PermissionUnknown, Capacity }
public sealed record DoorLeafState(DoorControllerState Controller, float DisplacementFraction, AppearanceTransform LeafPose, string Effect, int RetainedActors, bool ClosingHeld = false);
public sealed record DoorActorBody(ulong Handle, int Cell, Vector3 Feet, Vector3 HalfExtents);

/// <summary>Offline single normal leaf with explicit stable actor handles, authored-box clearance and host-provided positions/permission.</summary>
public sealed class SingleLeafDoorRuntime : IDisposable
{
    private readonly DoorStyle style;
    private readonly AppearanceTransform doorPose;
    private readonly Vector3 minimum, maximum;
    private readonly int cell;
    private readonly int? pairedCell;
    private readonly HashSet<ulong> actors = new();
    private readonly PortalDoorController controller;
    private bool disposed;
    private bool closingHeld;
    public DoorLeafState State { get; private set; }
    public int OwningCell => cell;
    public bool AcceptsCell(int value) => value == cell || pairedCell.HasValue && value == pairedCell.Value;
    public PortalBarrierCollider SnapshotLeafCollider()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return new(minimum, maximum, State.LeafPose);
    }
    public SingleLeafDoorRuntime(DoorStyle style, AppearanceExtent leafExtent, AppearanceTransform doorPose, int cell, int? pairedCell = null)
    {
        ArgumentNullException.ThrowIfNull(style); ArgumentNullException.ThrowIfNull(leafExtent);
        if (pairedCell.HasValue && (pairedCell.Value < 0 || pairedCell.Value == cell)) throw new InvalidDataException("Invalid paired contact cell.");
        if (style.IsForceField || style.SecondaryAppearance.Length != 0 || style.PrimaryAppearance.Length == 0 || style.FlipSecondaryRaw != 0)
            throw new InvalidDataException("Only one normal unflipped authored leaf is supported.");
        if (cell < 0 || leafExtent.Kind != "EXBX" || !leafExtent.Minimum.HasValue || !leafExtent.Maximum.HasValue || !Finite(style.Movement) ||
            !float.IsFinite(style.Springiness) || style.Springiness <= 0 || !float.IsFinite(style.Smoothness) || style.Smoothness is < 0 or > 1 || !float.IsFinite(style.TriggerRadius) || style.TriggerRadius <= 0)
            throw new InvalidDataException("Invalid single-leaf door inputs.");
        minimum = leafExtent.Minimum.Value; maximum = leafExtent.Maximum.Value;
        _ = new PortalBarrierCollider(minimum, maximum, doorPose); // Validate the rigid authored frame once.
        AppearanceTransform openPose = doorPose.Compose(AppearanceTransform.Identity with { Translation = style.Movement });
        _ = new PortalBarrierCollider(minimum, maximum, openPose); // Reject overflow before any contact changes state.
        this.style = style; this.doorPose = doorPose; this.cell = cell; this.pairedCell = pairedCell; controller = new(style.OpenSeconds, style.CloseSeconds);
        State = BuildState();
    }
    public DoorContactResult NotifyContact(ulong actor, int startCell, int endCell, Vector3 actorPosition, bool? allowEntry, long frame)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (actor == 0 || frame < 0 || !Finite(actorPosition)) throw new ArgumentOutOfRangeException(nameof(actor));
        if (!AcceptsCell(startCell) && !AcceptsCell(endCell)) return DoorContactResult.WrongCell;
        if (allowEntry is null) return DoorContactResult.PermissionUnknown;
        if (!allowEntry.Value) return DoorContactResult.EntryDenied;
        Vector3 center = doorPose.TransformPoint(new(0, 1, 0));
        if (DistanceSquared(actorPosition, center) > (double)style.TriggerRadius * style.TriggerRadius) return DoorContactResult.OutsideTrigger;
        if (!actors.Contains(actor) && actors.Count == 25) return DoorContactResult.Capacity;
        // Request before mutating membership so a rejected frame cannot partially add an actor.
        controller.RequestOpen(frame); bool added = actors.Add(actor); State = BuildState();
        return added ? DoorContactResult.Retained : DoorContactResult.Duplicate;
    }
    public DoorLeafState Advance(long frame, float seconds, Func<ulong, Vector3?> locateActor, bool? allowEntry, IReadOnlyList<DoorActorBody>? closingActors = null)
    {
        ObjectDisposedException.ThrowIf(disposed, this); ArgumentNullException.ThrowIfNull(locateActor);
        var candidate = controller.PreviewAdvance(frame, seconds);
        DoorActorBody[] bodies = closingActors?.ToArray() ?? Array.Empty<DoorActorBody>();
        if (bodies.Length > 32 || bodies.Any(b => b is null || b.Handle == 0 || b.Cell < 0 || !Finite(b.Feet) || !Finite(b.HalfExtents) ||
            b.HalfExtents.X <= 0 || b.HalfExtents.Y <= 0 || b.HalfExtents.Z <= 0 || b.HalfExtents.X > 100 || b.HalfExtents.Y > 100 || b.HalfExtents.Z > 100) ||
            bodies.Select(b => b.Handle).Distinct().Count() != bodies.Length)
            throw new InvalidDataException("Invalid closing actor snapshot/budget.");
        // Resolve external callbacks before changing state; failure leaves this step uncommitted.
        List<ulong> remove = new(); Vector3 origin = doorPose.Translation; double radius = 1.5 * style.TriggerRadius;
        foreach (ulong actor in actors)
        {
            Vector3? position = locateActor(actor);
            if (position.HasValue && !Finite(position.Value)) throw new InvalidDataException("Nonfinite retained actor position.");
            if (!position.HasValue || DistanceSquared(position.Value, origin) >= radius * radius) remove.Add(actor);
        }
        bool obstructed = false;
        if (candidate.Fraction < controller.State.Fraction && bodies.Length != 0)
        {
            var proposed = BuildState(candidate);var current = SnapshotLeafCollider();var displacement = proposed.LeafPose.Translation - State.LeafPose.Translation;
            if (!Finite(displacement)) throw new InvalidDataException("Closing leaf displacement overflow.");
            foreach (var body in bodies.Where(b => AcceptsCell(b.Cell)))
            {
                var center = body.Feet + new Vector3(0, body.HalfExtents.Y, 0);
                if (!Finite(center) || !Finite(center - displacement)) throw new InvalidDataException("Closing actor relative sweep overflow.");
                // Constant orientation and monotonic authored easing: relative actor motion covers the complete leaf translation.
                if (current.Sweep(center, center - displacement, body.HalfExtents, false, true).Status != PortalBarrierContactStatus.Clear)
                    obstructed = true;
            }
        }
        controller.Advance(frame, seconds, obstructed);closingHeld = obstructed;
        foreach (ulong actor in remove) actors.Remove(actor);
        if (actors.Count > 0 && allowEntry == true) controller.RequestOpen(frame);
        State = BuildState(); return State;
    }
    public PortalBarrierContact SweepLeaf(Vector3 startCenter, Vector3 endCenter, Vector3 actorHalfExtents)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        // Uses the authored appearance box as a conservative physical leaf envelope, without barrier padding.
        return SnapshotLeafCollider().Sweep(startCenter, endCenter, actorHalfExtents, false);
    }
    private DoorLeafState BuildState(DoorControllerState? proposed = null)
    {
        var state = proposed ?? controller.State;
        double powered = Math.Pow(state.Fraction, 1.0 / style.Springiness);
        float factor = (float)(powered * (1 - style.Smoothness) + (1 - Math.Cos((double)MathF.PI * powered)) * .5 * style.Smoothness);
        AppearanceTransform leaf = doorPose.Compose(AppearanceTransform.Identity with { Translation = style.Movement * factor });
        return new(state, factor, leaf, style.Effect(state.Transition), actors.Count, closingHeld);
    }
    public void Reset() { ObjectDisposedException.ThrowIf(disposed, this); actors.Clear(); controller.Reset(); closingHeld = false; State = BuildState(); }
    public void Dispose() { if (disposed) return; actors.Clear(); controller.Reset(); closingHeld = false; State = BuildState(); disposed = true; }
    private static bool Finite(Vector3 v) => float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z);
    private static double DistanceSquared(Vector3 a, Vector3 b) { double x = (double)a.X - b.X, y = (double)a.Y - b.Y, z = (double)a.Z - b.Z; return x * x + y * y + z * z; }
}
