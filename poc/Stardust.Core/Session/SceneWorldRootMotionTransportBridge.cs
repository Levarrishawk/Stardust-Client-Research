using Stardust.Core.Protocol;

namespace Stardust.Core.Session;

/// <summary>Offline channel-zero admission only. Borrows queue/transport, owns at most one transferred root array.</summary>
public sealed class SceneWorldRootMotionTransportBridge : IDisposable
{
    private SceneWorldClientRootMotionQueue? queue;
    private SoeClientTransport? transport;
    private byte[]? pending;
    private ulong acceptedCount;
    private readonly int ownerThread = Environment.CurrentManagedThreadId;
    private bool busy, disposed, faulted;
    public SceneWorldRootMotionTransportBridge(SceneWorldClientRootMotionQueue queue, SoeClientTransport transport)
    {
        ArgumentNullException.ThrowIfNull(queue); ArgumentNullException.ThrowIfNull(transport);
        _ = queue.Count; // Prove borrowed queue owner affinity; transport itself has no affinity API.
        this.queue = queue; this.transport = transport;
    }
    private void Enter()
    {
        if (Environment.CurrentManagedThreadId != ownerThread) throw new InvalidOperationException("Root transport bridge owner thread required.");
        if (busy) throw new InvalidOperationException("Root transport bridge reentrancy.");
        busy = true;
    }
    public int OwnedPendingBytes { get { Enter(); try { return pending?.Length ?? 0; } finally { busy = false; } } }
    public ulong AcceptedCount { get { Enter(); try { return acceptedCount; } finally { busy = false; } } }
    public bool IsFaulted { get { Enter(); try { return faulted; } finally { busy = false; } } }
    public bool TryQueueNext(long currentGeneration, CancellationToken cancellationToken = default)
    {
        Enter(); try
        {
            ObjectDisposedException.ThrowIf(disposed, this); cancellationToken.ThrowIfCancellationRequested();
            if (faulted) throw new InvalidOperationException("Root transport bridge is terminally faulted.");
            if (transport!.State != SoeClientTransportState.Established || transport.PendingMessageCount != 0) return false;
            ulong next = checked(acceptedCount + 1); // Preflight before ownership transfer.
            if (!queue!.TryTake(currentGeneration, out var frame, cancellationToken)) return false;
            pending = frame ?? throw new InvalidOperationException("Queue reported transfer without an array.");
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                transport.QueueReliableMessage(pending, 0); // Existing API copies to its own retained bytes.
            }
            catch { faulted = true; throw; } // Honest transferred prefix, no retry/requeue or borrower rollback.
            Array.Clear(pending); pending = null; acceptedCount = next;
            return true;
        } finally { busy = false; }
    }
    public void Dispose()
    {
        Enter(); try
        {
            if (disposed) return;
            disposed = true;
            if (pending is not null) Array.Clear(pending);
            pending = null; queue = null; transport = null;
        } finally { busy = false; }
    }
}
