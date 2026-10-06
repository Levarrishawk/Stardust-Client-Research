namespace Stardust.Core.Session;

/// <summary>Bounded serialized factory ownership; never revives retired children or retries automatically.</summary>
public sealed class TerrainOfflineSessionRestartOwner : IDisposable
{
    private readonly int ownerThread = Environment.CurrentManagedThreadId, maximumRuns;
    private TerrainConfiguredOfflineSession? active, failedRetirement;
    private TerrainOfflineSessionCompletion? lastCompletion;
    private int generation;
    private bool busy, reentered, disposed;
    private string? cleanupFaultType;
    public TerrainOfflineSessionRestartOwner(int maximumRuns = 8)
    {
        if (maximumRuns is < 1 or > 32) throw new ArgumentOutOfRangeException(nameof(maximumRuns));
        this.maximumRuns = maximumRuns;
    }
    private void Enter()
    {
        if (Environment.CurrentManagedThreadId != ownerThread) throw new InvalidOperationException("Restart owner thread required.");
        if (busy) { reentered = true; throw new InvalidOperationException("Restart owner reentry."); }
        busy = true; reentered = false;
    }
    private void CheckReentry() { if (reentered) throw new InvalidOperationException("Restart operation poisoned by reentry."); }
    private T Read<T>(Func<T> read)
    { Enter(); try { var value = read(); CheckReentry(); return value; } finally { busy = false; } }
    public TerrainConfiguredOfflineSession? Active => Read(() => active);
    public int Generation => Read(() => generation);
    public TerrainOfflineSessionCompletion? LastCompletion => Read(() => lastCompletion);
    public bool IsDisposed => Read(() => disposed);
    public string? CleanupFaultType => Read(() => cleanupFaultType);
    private void Open()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (cleanupFaultType is not null) throw new InvalidOperationException("Failed retirement is terminal; no new factory permitted.");
    }
    private void Retire(TerrainConfiguredOfflineSession owner)
    {
        bool alreadyPoisoned = reentered;
        try { owner.Dispose(); if (!alreadyPoisoned) CheckReentry(); }
        catch (Exception error) { failedRetirement = owner; cleanupFaultType = error.GetType().Name; throw; }
    }
    private TerrainConfiguredOfflineSession Begin(Func<TerrainConfiguredOfflineSession> factory, bool restart)
    {
        Open(); ArgumentNullException.ThrowIfNull(factory);
        if (active is not null || generation >= maximumRuns ||
            (restart ? lastCompletion is null : generation != 0 || lastCompletion is not null))
            throw new InvalidOperationException("Start/restart phase or successful-run budget rejected before factory.");
        TerrainConfiguredOfflineSession? candidate = null;
        try
        {
            candidate = factory(); CheckReentry();
            if (candidate is null || candidate.IsDisposed) throw new InvalidDataException("Fresh non-disposed factory owner required.");
            var point = candidate.PointDriver.State; var controller = candidate.Controller.State;
            if (point.CompletedUpdates != 0 || point.AttemptedUpdates != 0 || controller.CompletedUpdates != 0 ||
                controller.Stopped || controller.StopRequested || controller.IsFaulted ||
                point.Mode is SceneWorldTerrainPointTargetMode.Faulted or SceneWorldTerrainPointTargetMode.Unavailable or SceneWorldTerrainPointTargetMode.Disposed ||
                !point.DriverResult.Active || point.DriverResult.FaultType is not null ||
                point.DriverResult.TimestampMicroseconds != controller.DriverResult.DriverResult.TimestampMicroseconds)
                throw new InvalidDataException("Factory owner must have fresh unadvanced healthy children.");
            candidate.PointDriver.ValidateTargetAdmission(point.DriverResult.TimestampMicroseconds);
            // Same-value controller intent is a no-effect admission probe, including its disposed guard.
            candidate.Controller.SetPaused(controller.Paused);
            CheckReentry(); active = candidate; generation = checked(generation + 1); return candidate;
        }
        catch { if (candidate is not null) Retire(candidate); throw; }
    }
    public TerrainConfiguredOfflineSession Start(Func<TerrainConfiguredOfflineSession> factory)
    { Enter(); try { return Begin(factory, false); } finally { busy = false; } }
    public TerrainConfiguredOfflineSession Restart(Func<TerrainConfiguredOfflineSession> factory)
    { Enter(); try { return Begin(factory, true); } finally { busy = false; } }
    public TerrainOfflineSessionCompletion Complete(bool finishRequested = false)
    {
        Enter(); try
        {
            Open(); var current = active ?? throw new InvalidOperationException("No active run.");
            var completion = TerrainOfflineSessionCompletion.Capture(current.Controller, current.Settings.MaximumUpdates, finishRequested);
            if (completion.Reason == TerrainOfflineCompletionReason.Incomplete)
                throw new InvalidOperationException("Incomplete run remains active.");
            CheckReentry(); lastCompletion = completion; active = null;
            Retire(current); return completion;
        } finally { busy = false; }
    }
    public void Dispose()
    {
        Enter(); try
        {
            if (disposed) return;
            disposed = true; var current = active; active = null;
            // A failed retirement is retained as an honest terminal prefix, not silently retried or declared repaired.
            if (current is not null) Retire(current);
            GC.KeepAlive(failedRetirement);
        } finally { busy = false; }
    }
}
