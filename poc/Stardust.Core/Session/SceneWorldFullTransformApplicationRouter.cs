using Stardust.Core.Protocol;

namespace Stardust.Core.Session;

public enum SceneWorldFullTransformApplicationStatus
{
    Applied, WrongDirection, StaleGeneration, InactiveWorld, UnsupportedParent,
    MissingObject, Unfinalized, Contained, ClosedStore, StaleSnapshot, CounterRejected
}

/// <summary>Actual issued motion identity and raw frame values; no invented protocol interpretation.</summary>
public sealed record SceneWorldFullTransformApplicationResult(
    SceneWorldFullTransformApplicationStatus Status, ObjectControllerTransformMessage Raw,
    SceneWorldObjectSnapshot? Snapshot, SceneCreatePlacement? DerivedPlacement,
    SceneWorldObjectMotionSnapshot? PriorMotion, SceneWorldObjectMotionSnapshot? Motion)
{
    public bool Applied => Status == SceneWorldFullTransformApplicationStatus.Applied;
}

/// <summary>One immediate modern root correction, borrowing an exact world/store on their owner thread.</summary>
public sealed class SceneWorldFullTransformApplicationRouter : IDisposable
{
    private SceneWorldCoordinator? world;
    private SceneWorldObjectMotionStore? motions;
    private readonly SceneCreateZeroQuaternionPolicy zeroPolicy;
    private readonly int ownerThread = Environment.CurrentManagedThreadId;
    private bool busy, disposed;
    public SceneWorldFullTransformApplicationRouter(SceneWorldCoordinator world,
        SceneWorldObjectMotionStore motions, SceneCreateZeroQuaternionPolicy zeroPolicy)
    {
        ArgumentNullException.ThrowIfNull(world); ArgumentNullException.ThrowIfNull(motions);
        if (zeroPolicy is not (SceneCreateZeroQuaternionPolicy.Reject or SceneCreateZeroQuaternionPolicy.Identity))
            throw new ArgumentOutOfRangeException(nameof(zeroPolicy));
        _ = world.Snapshots();
        if (!motions.IsBoundTo(world)) throw new ArgumentException("Motion store must be open and bound to this world.", nameof(motions));
        this.world = world; this.motions = motions; this.zeroPolicy = zeroPolicy;
    }
    private void Enter()
    {
        if (Environment.CurrentManagedThreadId != ownerThread) throw new InvalidOperationException("Full transform owner thread required.");
        if (busy) throw new InvalidOperationException("Full transform reentrancy.");
        busy = true;
    }
    public SceneWorldFullTransformApplicationResult Accept(long generation, ReadOnlySpan<byte> application,
        ObjectControllerTransformDirection direction, CancellationToken cancellationToken = default)
    {
        Enter(); try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            cancellationToken.ThrowIfCancellationRequested();
            var raw = ObjectControllerTransformCodec.Decode(application, direction);
            SceneWorldFullTransformApplicationResult Decline(SceneWorldFullTransformApplicationStatus status,
                SceneWorldObjectSnapshot? snapshot = null, SceneCreatePlacement? placement = null,
                SceneWorldObjectMotionSnapshot? prior = null) => new(status, raw, snapshot, placement, prior, prior);
            if (direction != ObjectControllerTransformDirection.ServerToClient) return Decline(SceneWorldFullTransformApplicationStatus.WrongDirection);
            if (generation != world!.Generation) return Decline(SceneWorldFullTransformApplicationStatus.StaleGeneration);
            if (world.State is not (ZoneSceneTransitionState.AwaitingServerReady or ZoneSceneTransitionState.Ready)) return Decline(SceneWorldFullTransformApplicationStatus.InactiveWorld);
            if (raw.Kind != ObjectControllerTransformKind.Root) return Decline(SceneWorldFullTransformApplicationStatus.UnsupportedParent);
            if (!motions!.IsBoundTo(world)) return Decline(SceneWorldFullTransformApplicationStatus.ClosedStore);
            var snapshot = world.Snapshots().SingleOrDefault(s => s.Object.Handle.ObjectId == raw.ObjectId);
            if (snapshot is null) return Decline(SceneWorldFullTransformApplicationStatus.MissingObject);
            if (snapshot.Object.Phase != SceneObjectPhase.Finalized) return Decline(SceneWorldFullTransformApplicationStatus.Unfinalized, snapshot);
            if (snapshot.Object.ContainerId != 0) return Decline(SceneWorldFullTransformApplicationStatus.Contained, snapshot);
            if (raw.Speed < 0) throw new InvalidDataException("Full transform application requires nonnegative speed.");
            var batch = world.DrainChanges(generation)!;
            var state = world.State;
            var prior = motions.Find(snapshot);
            var create = snapshot.Object.Create with { QuaternionX = raw.QuaternionX, QuaternionY = raw.QuaternionY,
                QuaternionZ = raw.QuaternionZ, QuaternionW = raw.QuaternionW,
                PositionX = raw.PositionX, PositionY = raw.PositionY, PositionZ = raw.PositionZ };
            var placement = SceneCreatePlacementBuilder.Create(create, zeroPolicy);
            cancellationToken.ThrowIfCancellationRequested();
            if (generation != world.Generation || world.State != state || !world.IsCurrent(batch)
                || !ReferenceEquals(prior, motions.Find(snapshot)))
                return Decline(SceneWorldFullTransformApplicationStatus.StaleSnapshot, snapshot, placement, prior);
            if (!motions.PublishAuthoritative(snapshot, placement.Transform, raw.Speed, raw.MovementCounter))
                return Decline(SceneWorldFullTransformApplicationStatus.CounterRejected, snapshot, placement, prior);
            var motion = motions.Find(snapshot) ?? throw new InvalidOperationException("Published motion missing.");
            return new(SceneWorldFullTransformApplicationStatus.Applied, raw, snapshot, placement, prior, motion);
        } finally { busy = false; }
    }
    public void Dispose()
    {
        Enter(); try { if (disposed) return; disposed = true; world = null; motions = null; }
        finally { busy = false; }
    }
}
