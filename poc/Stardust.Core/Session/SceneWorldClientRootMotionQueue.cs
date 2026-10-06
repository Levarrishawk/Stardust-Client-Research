using Stardust.Core.Protocol;

namespace Stardust.Core.Session;

/// <summary>Bounded offline FIFO. Owns one builder and retained arrays; borrows world/store.</summary>
public sealed class SceneWorldClientRootMotionQueue : IDisposable
{
    private sealed record Row(long Generation, SceneObjectIncarnation Incarnation, byte[] Bytes);
    private readonly Queue<Row> rows = new();
    private SceneWorldCoordinator? world;
    private SceneWorldObjectMotionStore? motions;
    private SceneWorldClientRootMotionBuilder? builder;
    private readonly int maximumPackets, maximumBytes, ownerThread = Environment.CurrentManagedThreadId;
    private int queuedBytes;
    private bool busy, disposed;
    public SceneWorldClientRootMotionQueue(SceneWorldCoordinator world, SceneWorldObjectMotionStore motions,
        SceneWorldClientRootMotionPolicy policy, int maxPackets = 8, int maxBytes = 496)
    {
        ArgumentNullException.ThrowIfNull(world); ArgumentNullException.ThrowIfNull(motions); ArgumentNullException.ThrowIfNull(policy);
        if (maxPackets is < 1 or > 4096) throw new ArgumentOutOfRangeException(nameof(maxPackets));
        if (maxBytes is < ObjectControllerTransformCodec.RootLength or > 253952) throw new ArgumentOutOfRangeException(nameof(maxBytes));
        builder = new(world, motions, policy);
        this.world = world; this.motions = motions; maximumPackets = maxPackets; maximumBytes = maxBytes;
    }
    private void Enter()
    {
        if (Environment.CurrentManagedThreadId != ownerThread) throw new InvalidOperationException("Outbound queue owner thread required.");
        if (busy) throw new InvalidOperationException("Outbound queue reentrancy.");
        busy = true;
    }
    private bool Active => world!.State is ZoneSceneTransitionState.AwaitingServerReady or ZoneSceneTransitionState.Ready;
    public int Count { get { Enter(); try { return rows.Count; } finally { busy = false; } } }
    public int QueuedBytes { get { Enter(); try { return queuedBytes; } finally { busy = false; } } }
    public void Enqueue(SceneWorldObjectSnapshot snapshot, SceneWorldObjectMotionSnapshot motion,
        uint flags, uint movementCounter, uint timestamp, CancellationToken cancellationToken = default)
    {
        Enter(); try
        {
            ObjectDisposedException.ThrowIf(disposed, this); cancellationToken.ThrowIfCancellationRequested();
            if (!Active || !motions!.IsBoundTo(world!)) throw new InvalidDataException("Active world and bound open store required.");
            if (rows.Count >= maximumPackets || ObjectControllerTransformCodec.RootLength > maximumBytes - queuedBytes)
                throw new InvalidDataException("Outbound queue capacity.");
            // No stale pruning or builder allocation occurs before capacity preflight.
            var packet = builder!.Build(snapshot, motion, flags, movementCounter, timestamp, cancellationToken);
            rows.Enqueue(new(world!.Generation, packet.SourceMotion.Incarnation, packet.Application));
            queuedBytes += packet.Application.Length;
        } finally { busy = false; }
    }
    /// <summary>Transfers one historical same-incarnation array. Discards stale FIFO prefixes with clearing.</summary>
    public bool TryTake(long currentGeneration, out byte[]? application, CancellationToken cancellationToken = default)
    {
        application = null; Enter(); try
        {
            ObjectDisposedException.ThrowIf(disposed, this); cancellationToken.ThrowIfCancellationRequested();
            if (currentGeneration != world!.Generation || !Active || !motions!.IsBoundTo(world)) return false;
            var live = world.Snapshots().Where(s => s.Object.Phase == SceneObjectPhase.Finalized).Select(s => s.Object.Incarnation).ToHashSet();
            int remaining = rows.Count; // Fixed bounded prefix scan, no callbacks or new enqueues.
            while (remaining-- > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var row = rows.Peek();
                bool current = row.Generation == currentGeneration && live.Contains(row.Incarnation);
                rows.Dequeue(); queuedBytes -= row.Bytes.Length;
                if (current) { application = row.Bytes; return true; }
                Array.Clear(row.Bytes);
            }
            return false;
        } finally { busy = false; }
    }
    public void Dispose()
    {
        Enter(); try
        {
            if (disposed) return;
            disposed = true;
            foreach (var row in rows) Array.Clear(row.Bytes);
            rows.Clear(); queuedBytes = 0;
            builder!.Dispose(); builder = null; world = null; motions = null;
        } finally { busy = false; }
    }
}
