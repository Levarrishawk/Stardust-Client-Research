using System.Numerics;
using Stardust.Core.Assets;

namespace Stardust.Core.Session;

/// <summary>Bounded modern native motion, independent of replication and resource attempts.</summary>
public sealed class SceneWorldObjectMotionStore : ISceneWorldObjectMotionSource, IDisposable
{
    private sealed record Row(SceneWorldObjectMotionSnapshot Motion, uint? LastAuthoritativeCounter);
    private SceneWorldCoordinator? world;
    private readonly Dictionary<ulong, Row> rows = new();
    private readonly int maximumObjects, ownerThread = Environment.CurrentManagedThreadId;
    private bool busy, disposed;
    private ulong revision;
    public SceneWorldObjectMotionStore(SceneWorldCoordinator world, int maximumObjects = 64)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (maximumObjects is < 1 or > 4096) throw new ArgumentOutOfRangeException(nameof(maximumObjects));
        _ = world.Snapshots(); // Prove world-owner affinity before retaining the borrowed world.
        this.world = world; this.maximumObjects = maximumObjects;
    }
    private void Enter()
    {
        if (Environment.CurrentManagedThreadId != ownerThread) throw new InvalidOperationException("Motion owner thread required.");
        if (busy) throw new InvalidOperationException("Motion store reentrancy.");
        busy = true;
    }
    private bool Active => !disposed && world!.State is ZoneSceneTransitionState.AwaitingServerReady or ZoneSceneTransitionState.Ready;
    private SceneWorldObjectSnapshot? Current(SceneWorldObjectSnapshot snapshot)
    {
        if (!Active || snapshot.Object is null || snapshot.Placement is null || snapshot.Envelopes is null) return null;
        var latest = world!.Snapshots().SingleOrDefault(s => s.Object.Handle == snapshot.Object.Handle);
        if (latest is not { Object.Phase: SceneObjectPhase.Finalized } ||
            !ReferenceEquals(latest.Object, snapshot.Object) || !ReferenceEquals(latest.Placement, snapshot.Placement) ||
            !snapshot.Envelopes.SequenceEqual(latest.Envelopes)) return null;
        return snapshot.Replication is null ? latest.Replication is null ? latest : null :
            world.IsCurrent(snapshot.Replication) && snapshot.Replication.Incarnation == latest.Object.Incarnation ? latest : null;
    }
    private HashSet<SceneObjectIncarnation> Live() => !Active ? new() : world!.Snapshots()
        .Where(s => s.Object.Phase == SceneObjectPhase.Finalized).Select(s => s.Object.Incarnation).ToHashSet();
    private void PruneCore()
    {
        if (disposed) return;
        var live = Live();
        foreach (var pair in rows.ToArray()) if (!live.Contains(pair.Value.Motion.Incarnation)) rows.Remove(pair.Key);
    }
    public bool PublishLocal(SceneWorldObjectSnapshot snapshot, AppearanceTransform transform, float speed)
    { Enter(); try { return Publish(snapshot, transform, speed, null); } finally { busy = false; } }
    public bool PublishAuthoritative(SceneWorldObjectSnapshot snapshot, AppearanceTransform transform, float speed, uint movementCounter)
    { Enter(); try { return Publish(snapshot, transform, speed, movementCounter); } finally { busy = false; } }
    private bool Publish(SceneWorldObjectSnapshot snapshot, AppearanceTransform transform, float speed, uint? counter)
    {
        ObjectDisposedException.ThrowIf(disposed, this); ArgumentNullException.ThrowIfNull(snapshot);
        Validate(transform, speed);
        if (Current(snapshot) is not { } current) return false;
        ulong id = current.Object.Handle.ObjectId;
        var previous = rows.GetValueOrDefault(id);
        if (previous?.Motion.Incarnation != current.Object.Incarnation) previous = null;
        if (counter is { } incoming && previous?.LastAuthoritativeCounter is { } last)
        {
            uint distance = unchecked(incoming - last);
            if (distance == 0 || distance >= 0x80000000U) return false;
        }
        ulong candidateRevision = checked(revision + 1);
        var candidate = new Row(new(current.Object.Incarnation, candidateRevision, transform, speed, counter, counter.HasValue),
            counter ?? previous?.LastAuthoritativeCounter);
        var live = Live();
        int retained = rows.Values.Count(row => live.Contains(row.Motion.Incarnation));
        if (previous is null && retained >= maximumObjects) throw new InvalidDataException("Motion object capacity.");
        // All validation, serial/revision checks and capacity preflight precede any row mutation.
        foreach (var pair in rows.ToArray()) if (!live.Contains(pair.Value.Motion.Incarnation)) rows.Remove(pair.Key);
        rows[id] = candidate; revision = candidateRevision; return true;
    }
    public SceneWorldObjectMotionSnapshot? Find(SceneWorldObjectSnapshot snapshot)
    {
        Enter(); try
        {
            ArgumentNullException.ThrowIfNull(snapshot); if (disposed) return null;
            PruneCore();
            return Current(snapshot) is { } current && rows.GetValueOrDefault(current.Object.Handle.ObjectId) is { } row &&
                row.Motion.Incarnation == current.Object.Incarnation ? row.Motion : null;
        } finally { busy = false; }
    }
    public bool IsCurrent(SceneWorldObjectMotionSnapshot motion)
    {
        Enter(); try
        {
            ArgumentNullException.ThrowIfNull(motion); if (disposed) return false;
            PruneCore();
            return rows.GetValueOrDefault(motion.Incarnation.ObjectId) is { } row && ReferenceEquals(row.Motion, motion);
        } finally { busy = false; }
    }
    private static void Validate(AppearanceTransform transform, float speed)
    {
        if (!float.IsFinite(speed) || speed < 0) throw new ArgumentOutOfRangeException(nameof(speed));
        static bool Finite(Vector3 v) => float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z);
        static double Dot(Vector3 a, Vector3 b) => (double)a.X*b.X + (double)a.Y*b.Y + (double)a.Z*b.Z;
        var x = transform.RowX; var y = transform.RowY; var z = transform.RowZ;
        const double tolerance = 1e-4;
        double determinant = (double)x.X*((double)y.Y*z.Z-(double)y.Z*z.Y) -
            (double)x.Y*((double)y.X*z.Z-(double)y.Z*z.X) + (double)x.Z*((double)y.X*z.Y-(double)y.Y*z.X);
        if (!Finite(x) || !Finite(y) || !Finite(z) || !Finite(transform.Translation) ||
            Math.Abs(Dot(x,x)-1) > tolerance || Math.Abs(Dot(y,y)-1) > tolerance || Math.Abs(Dot(z,z)-1) > tolerance ||
            Math.Abs(Dot(x,y)) > tolerance || Math.Abs(Dot(x,z)) > tolerance || Math.Abs(Dot(y,z)) > tolerance ||
            Math.Abs(determinant-1) > tolerance) throw new InvalidDataException("Motion transform must be finite, rigid and proper within 1e-4.");
    }
    public void Prune() { Enter(); try { PruneCore(); } finally { busy = false; } }
    public void Clear() { Enter(); try { rows.Clear(); } finally { busy = false; } }
    public void Dispose()
    {
        Enter(); try { if (disposed) return; disposed = true; rows.Clear(); world = null; } finally { busy = false; }
    }
}
