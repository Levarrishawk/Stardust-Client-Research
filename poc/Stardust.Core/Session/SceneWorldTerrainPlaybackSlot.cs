using Stardust.Core.Geometry;

namespace Stardust.Core.Session;

/// <summary>Owner-thread selection slot; candidate construction can publish borrowed motion before replacement.</summary>
public sealed class SceneWorldTerrainPlaybackSlot : IDisposable
{
    private readonly int ownerThread = Environment.CurrentManagedThreadId;
    private SceneWorldCoordinator? world;
    private SceneWorldObjectMotionStore? motions;
    private SceneWorldCurrentSpeedTerrainPlayback? current;
    private ulong replacementRevision;
    private bool busy, disposed;
    public SceneWorldTerrainPlaybackSlot(SceneWorldCoordinator world, SceneWorldObjectMotionStore motions)
    {
        ArgumentNullException.ThrowIfNull(world); ArgumentNullException.ThrowIfNull(motions);
        if (!motions.IsBoundTo(world)) throw new ArgumentException("Open motion store bound to the exact supplied world required.", nameof(motions));
        this.world = world; this.motions = motions;
    }
    private void Enter()
    {
        if (Environment.CurrentManagedThreadId != ownerThread) throw new InvalidOperationException("Terrain slot owner thread required.");
        if (busy) throw new InvalidOperationException("Terrain slot reentry."); busy = true;
    }
    private T Read<T>(Func<T> read) { Enter(); try { return read(); } finally { busy = false; } }
    public SceneWorldCurrentSpeedTerrainPlayback? Current => Read(() => current);
    public ulong ReplacementRevision => Read(() => replacementRevision);
    public bool IsDisposed => Read(() => disposed);
    public bool TryReplace(SceneWorldSuppliedCurrentSpeedPlan composition, SceneWorldSuppliedCurrentSpeedPlanReceipt source,
        TerrainModernPatchSetSurfaceQuery surface, SceneWorldCurrentSpeedPlaybackPolicy policy,
        long initialTimestampMicroseconds, long stepMicroseconds, float footOffset = 0,
        SceneWorldTerrainMotionLimits? terrainLimits = null, SceneWorldCurrentSpeedPlaybackLimits? playbackLimits = null,
        CancellationToken cancellationToken = default)
    {
        Enter(); try
        {
            ObjectDisposedException.ThrowIf(disposed, this); cancellationToken.ThrowIfCancellationRequested();
            ulong nextRevision = checked(replacementRevision + 1);
            SceneWorldCurrentSpeedTerrainPlayback? candidate = null;
            try
            {
                candidate = SceneWorldCurrentSpeedTerrainPlayback.Bind(world!, motions!, composition, source, surface,
                    policy, initialTimestampMicroseconds, stepMicroseconds, footOffset, terrainLimits, playbackLimits, cancellationToken);
                if (candidate is null) return false;
                cancellationToken.ThrowIfCancellationRequested();
                if (!composition.IsCurrent(source)) return false;
                // No asynchronous advance/callback occurs in the synchronous temporary-two-owner window.
                current?.Retire();
                current = candidate; candidate = null; replacementRevision = nextRevision;
                return true;
            }
            finally { candidate?.Dispose(); }
        } finally { busy = false; }
    }
    public SceneWorldCurrentSpeedPlaybackReceipt? AdvanceNext(long submitTimestampMicroseconds,
        long advanceTimestampMicroseconds, CancellationToken cancellationToken = default)
    {
        Enter(); try
        {
            ObjectDisposedException.ThrowIf(disposed, this); cancellationToken.ThrowIfCancellationRequested();
            return current?.AdvanceNext(submitTimestampMicroseconds, advanceTimestampMicroseconds, cancellationToken);
        } finally { busy = false; }
    }
    public bool IsCurrent(SceneWorldCurrentSpeedPlaybackReceipt receipt)
    {
        Enter(); try { ArgumentNullException.ThrowIfNull(receipt); return !disposed && current is not null && current.IsCurrent(receipt); }
        finally { busy = false; }
    }
    public void RetireSelection()
    {
        Enter(); try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            try { current?.Retire(); } finally { current = null; }
        } finally { busy = false; }
    }
    public void Dispose()
    {
        Enter(); try
        {
            if (disposed) return; disposed = true;
            try { current?.Dispose(); } finally { current = null; world = null; motions = null; }
        } finally { busy = false; }
    }
}
