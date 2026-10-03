using System.Numerics;
using Stardust.Core.Assets;

namespace Stardust.Core.Session;

/// <summary>Modern planar free movement over the shared integer scheduler and input timeline.</summary>
public sealed class SceneWorldLocalMotionDriver : ISceneWorldLocalMotionDriver
{
    private sealed record Pose(AppearanceTransform Transform, float Speed);
    private SceneWorldCoordinator? world;
    private SceneWorldObjectMotionStore? motions;
    private FixedStepActorHost<Pose>? host;
    private PortalActorInputTimeline<Pose>? timeline;
    private readonly SceneObjectIncarnation incarnation;
    private readonly AppearanceTransform initialTransform;
    private readonly int ownerThread = Environment.CurrentManagedThreadId;
    private Pose pose;
    private SceneWorldObjectMotionSnapshot? lastPublication;
    private ulong observedAuthoritativeRevision;
    private string? fault;
    private bool busy, disposed;
    private SceneWorldLocalMotionDriverState state;
    public SceneWorldLocalMotionDriver(SceneWorldCoordinator world, SceneWorldObjectMotionStore motions, ulong objectId,
        long initialTimestampMicroseconds = 0, long stepMicroseconds = 16667, int maximumStepsPerUpdate = 8)
    {
        ArgumentNullException.ThrowIfNull(world); ArgumentNullException.ThrowIfNull(motions);
        if (initialTimestampMicroseconds < 0) throw new ArgumentOutOfRangeException(nameof(initialTimestampMicroseconds));
        if (stepMicroseconds is < 1000 or > 100000) throw new ArgumentOutOfRangeException(nameof(stepMicroseconds));
        if (maximumStepsPerUpdate is < 1 or > 32) throw new ArgumentOutOfRangeException(nameof(maximumStepsPerUpdate));
        var snapshot = world.Snapshots().SingleOrDefault(s => s.Object.Handle.ObjectId == objectId);
        if (world.State is not (ZoneSceneTransitionState.AwaitingServerReady or ZoneSceneTransitionState.Ready) ||
            snapshot is not { Object.Phase: SceneObjectPhase.Finalized, Replication: not null } || !world.IsCurrent(snapshot.Replication))
            throw new InvalidDataException("Local motion requires a current finalized typed CREO incarnation.");
        var existing = motions.Find(snapshot); // Checks store owner affinity before retaining borrowed dependencies.
        incarnation = snapshot.Object.Incarnation; initialTransform = snapshot.Placement.Transform;
        pose = new(existing?.Transform ?? initialTransform, existing?.Speed ?? 0);
        this.world = world; this.motions = motions;
        host = new(() => pose, Tick, () => pose = new(initialTransform, 0), (int)stepMicroseconds, maximumStepsPerUpdate);
        timeline = new(host, initialTimestampMicroseconds, new(Vector3.Zero, null));
        if (!motions.PublishLocal(snapshot, pose.Transform, pose.Speed)) throw new ArgumentException("Motion store must belong to this world.", nameof(motions));
        lastPublication = motions.Find(snapshot) ?? throw new InvalidOperationException("Initial local publication missing.");
        observedAuthoritativeRevision = existing is { Authoritative: true } ? existing.Revision : 0;
        state = Map(timeline.State);
    }
    private void Enter()
    {
        if (Environment.CurrentManagedThreadId != ownerThread) throw new InvalidOperationException("Local motion owner thread required.");
        if (busy) throw new InvalidOperationException("Local motion driver reentrancy.");
        busy = true;
    }
    public SceneWorldLocalMotionDriverState State { get { Enter(); try { return state; } finally { busy = false; } } }
    private SceneWorldObjectSnapshot? Current()
    {
        if (world!.State is not (ZoneSceneTransitionState.AwaitingServerReady or ZoneSceneTransitionState.Ready)) return null;
        var snapshot = world.Snapshots().SingleOrDefault(s => s.Object.Incarnation == incarnation);
        return snapshot is { Object.Phase: SceneObjectPhase.Finalized, Replication: not null } && world.IsCurrent(snapshot.Replication) ? snapshot : null;
    }
    private SceneWorldObjectSnapshot RequireCurrent()
    {
        if (Current() is { } snapshot) return snapshot;
        throw new InvalidOperationException("Selected local motion incarnation is no longer current.");
    }
    private void Latch(Exception error) { fault ??= error.GetType().Name; state = state with { Active = false, FaultType = fault }; }
    private SceneWorldLocalMotionDriverState Map(FixedStepActorState<Pose> clock) => new(incarnation.ObjectId,
        pose.Transform, pose.Speed, clock.TimestampMicroseconds!.Value, clock.StepsThisUpdate,
        clock.DroppedMicroseconds, clock.SuspendedMicroseconds, !disposed && fault is null && clock.Active, fault ?? clock.FaultType);
    private static void Validate(PortalActorInput input)
    {
        FixedStepActorHost<Pose>.ValidateInput(input);
        if (input.WorldVelocity.Y != 0) throw new ArgumentOutOfRangeException(nameof(input), "Planar input requires velocity Y zero.");
    }
    public void Submit(long timestampMicroseconds, PortalActorInput input)
    {
        Enter(); try
        {
            ObjectDisposedException.ThrowIf(disposed, this); Validate(input);
            if (timestampMicroseconds < state.TimestampMicroseconds ||
                timestampMicroseconds - state.TimestampMicroseconds > PortalActorInputTimeline<Pose>.MaximumFutureMicroseconds)
                throw new ArgumentOutOfRangeException(nameof(timestampMicroseconds), "Nonlate bounded input time required.");
            if (fault is not null) throw new InvalidOperationException("Local motion is faulted; explicit reset required.");
            try { _ = RequireCurrent(); } catch (Exception error) { Latch(error); throw; }
            timeline!.Submit(new(timestampMicroseconds, input)); // Admission rejects before modifying timeline history.
        } finally { busy = false; }
    }
    private void Tick(float seconds, Vector3 displacement, bool? entryAllowed)
    {
        var snapshot = RequireCurrent();
        var velocity = timeline!.CurrentInput.WorldVelocity;
        float speed = velocity.Length(); var transform = pose.Transform with { Translation = pose.Transform.Translation + displacement };
        if (speed > 0)
        {
            float sine = velocity.X / speed, cosine = velocity.Z / speed;
            transform = transform with { RowX = new(cosine, 0, sine), RowY = Vector3.UnitY, RowZ = new(-sine, 0, cosine) };
        }
        if (!motions!.PublishLocal(snapshot, transform, speed)) throw new InvalidOperationException("Local motion publication became stale.");
        var publication = motions.Find(snapshot) ?? throw new InvalidOperationException("Local motion publication missing.");
        pose = new(transform, speed); lastPublication = publication;
    }
    public SceneWorldLocalMotionDriverState Advance(long timestampMicroseconds, bool active = true)
    {
        Enter(); try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (timestampMicroseconds < state.TimestampMicroseconds) throw new ArgumentOutOfRangeException(nameof(timestampMicroseconds), "Monotonic time required.");
            try
            {
                if (fault is null)
                {
                    var snapshot = RequireCurrent(); var motion = motions!.Find(snapshot);
                    if (motion is { Authoritative: true } && !ReferenceEquals(motion, lastPublication) && motion.Revision > observedAuthoritativeRevision)
                    { pose = new(motion.Transform, motion.Speed); observedAuthoritativeRevision = motion.Revision; }
                }
                var clock = timeline!.AdvanceTo(timestampMicroseconds, active && fault is null);
                if (clock.FaultType is not null) fault ??= clock.FaultType;
                state = Map(clock); return state;
            }
            catch (Exception error)
            {
                Latch(error);
                // No implicit tick retry. Preserve any completed publications and last observed clock state.
                state = Map(timeline!.State); return state;
            }
        } finally { busy = false; }
    }
    public void Reset(long timestampMicroseconds)
    {
        Enter(); try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (timestampMicroseconds < 0) throw new ArgumentOutOfRangeException(nameof(timestampMicroseconds));
            try
            {
                var snapshot = RequireCurrent();
                if (!motions!.PublishLocal(snapshot, initialTransform, 0)) throw new InvalidOperationException("Reset motion publication became stale.");
                var publication = motions.Find(snapshot) ?? throw new InvalidOperationException("Reset publication missing.");
                timeline!.Reset(timestampMicroseconds, new(Vector3.Zero, null));
                lastPublication = publication; observedAuthoritativeRevision = 0; fault = null; state = Map(timeline.State);
            }
            catch (Exception error) { Latch(error); throw; }
        } finally { busy = false; }
    }
    public void Dispose()
    {
        Enter(); try
        {
            if (disposed) return; disposed = true;
            state = state with { Active = false }; timeline = null; host = null; world = null; motions = null; lastPublication = null;
        } finally { busy = false; }
    }
}
