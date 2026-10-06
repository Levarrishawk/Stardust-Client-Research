using Stardust.Core.Geometry;

namespace Stardust.Core.Session;

/// <summary>Owns the configured actual point driver and its borrowed-driver controller.</summary>
public sealed class TerrainConfiguredOfflineSession : IDisposable
{
    private readonly int ownerThread = Environment.CurrentManagedThreadId;
    private readonly SceneWorldTerrainPointTargetDriver pointDriver;
    private readonly TerrainOfflineSessionController controller;
    private readonly TerrainOfflinePlaygroundSettings settings;
    private readonly TerrainOfflineSessionBudget budget;
    private bool disposed, busy;

    public TerrainConfiguredOfflineSession(SceneWorldCoordinator world, SceneWorldObjectMotionStore motions,
        ulong objectId, TerrainModernPatchSetRayQuery rayQuery, TerrainOfflinePlaygroundSettings settings,
        long initialTimestampMicroseconds, float footOffset = 0, SceneWorldTerrainMotionLimits? terrainLimits = null,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(world); ArgumentNullException.ThrowIfNull(motions);
        ArgumentNullException.ThrowIfNull(rayQuery); ArgumentNullException.ThrowIfNull(settings);
        token.ThrowIfCancellationRequested();
        this.settings = settings; budget = settings.CreateBudget();
        SceneWorldTerrainPointTargetDriver? ownedPoint = null;
        TerrainOfflineSessionController? ownedController = null;
        try
        {
            ownedPoint = new(world, motions, objectId, rayQuery, settings.WalkSpeed, settings.ArrivalRadius,
                initialTimestampMicroseconds, settings.StepMicroseconds, footOffset, settings.MaximumUpdates,
                settings.MaximumClickAttempts, terrainLimits, token, budget, settings.HoldMissingDestination);
            token.ThrowIfCancellationRequested();
            ownedController = new(ownedPoint, settings.MaximumUpdates, settings.StepMicroseconds);
            token.ThrowIfCancellationRequested();
            pointDriver = ownedPoint; controller = ownedController;
        }
        catch
        {
            try { ownedController?.Dispose(); } finally { ownedPoint?.Dispose(); }
            // Cleanup never rolls back an initial motion publication or another borrowed row.
            throw;
        }
    }
    private void CheckOwner()
    {
        if (Environment.CurrentManagedThreadId != ownerThread) throw new InvalidOperationException("Configured session owner thread required.");
        if (busy) throw new InvalidOperationException("Configured session reentry.");
    }
    public SceneWorldTerrainPointTargetDriver PointDriver { get { CheckOwner(); return pointDriver; } }
    public TerrainOfflineSessionController Controller { get { CheckOwner(); return controller; } }
    public TerrainOfflinePlaygroundSettings Settings { get { CheckOwner(); return settings; } }
    public TerrainOfflineSessionBudget Budget { get { CheckOwner(); return budget; } }
    public bool IsDisposed { get { CheckOwner(); return disposed; } }

    public void Dispose()
    {
        CheckOwner(); if (disposed) return;
        busy = true; disposed = true;
        try { try { controller.Dispose(); } finally { pointDriver.Dispose(); } }
        finally { busy = false; }
    }
}
