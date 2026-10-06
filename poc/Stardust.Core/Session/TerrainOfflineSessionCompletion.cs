namespace Stardust.Core.Session;

public enum TerrainOfflineCompletionReason { BudgetCompleted, FinishRequested, Faulted, Incomplete }

/// <summary>Immutable primitive-only historical diagnostics from one actual controller snapshot.</summary>
public sealed class TerrainOfflineSessionCompletion
{
    private TerrainOfflineSessionCompletion(TerrainOfflineCompletionReason reason, int maximumUpdates,
        int completedUpdates, int attemptedUpdates, bool stopped, bool faulted, string mode, float speed,
        bool targetActive, bool finishRequested)
    {
        Reason = reason; MaximumUpdates = maximumUpdates; CompletedUpdates = completedUpdates;
        AttemptedUpdates = attemptedUpdates; Stopped = stopped; Faulted = faulted; Mode = mode;
        Speed = speed; TargetActive = targetActive; FinishRequested = finishRequested;
    }
    public TerrainOfflineCompletionReason Reason { get; }
    public int MaximumUpdates { get; }
    public int CompletedUpdates { get; }
    public int AttemptedUpdates { get; }
    public bool Stopped { get; }
    public bool Faulted { get; }
    public string Mode { get; }
    public float Speed { get; }
    public bool TargetActive { get; }
    public bool FinishRequested { get; }

    public static TerrainOfflineSessionCompletion Capture(TerrainOfflineSessionController controller,
        int maximumUpdates, bool finishRequested = false)
    {
        ArgumentNullException.ThrowIfNull(controller);
        if (maximumUpdates is < 1 or > 6000) throw new ArgumentOutOfRangeException(nameof(maximumUpdates));
        var state = controller.State; // Inherits actual controller owner/reentry guard; one historical snapshot.
        var point = state.DriverResult;
        if (point is null || point.DriverResult is null || state.CompletedUpdates != point.CompletedUpdates ||
            point.CompletedUpdates < 0 || point.CompletedUpdates > maximumUpdates ||
            point.AttemptedUpdates < point.CompletedUpdates || point.AttemptedUpdates > maximumUpdates ||
            point.AttemptedUpdates - point.CompletedUpdates > 1 || !Enum.IsDefined(point.Mode))
            throw new InvalidDataException("Inconsistent offline completion counts or mode.");
        float speed = point.DriverResult.Speed;
        if (!float.IsFinite(speed) || speed < 0 || speed > 10)
            throw new InvalidDataException("Invalid offline completion speed.");
        bool targetActive = point.Target is not null;
        bool faulted = state.IsFaulted || state.FaultType is not null || point.FaultType is not null ||
            point.DriverResult.FaultType is not null || !point.DriverResult.Active ||
            point.Mode is SceneWorldTerrainPointTargetMode.Faulted or SceneWorldTerrainPointTargetMode.Unavailable or SceneWorldTerrainPointTargetMode.Disposed;
        if (!faulted && point.AttemptedUpdates != point.CompletedUpdates)
            throw new InvalidDataException("Healthy offline completion counts must match.");
        if (state.Stopped && (point.CompletedUpdates == 0 || point.Mode != SceneWorldTerrainPointTargetMode.Idle ||
            speed != 0 || targetActive || point.AttemptedUpdates != point.CompletedUpdates))
            throw new InvalidDataException("Stopped completion requires an actual successful zero-speed idle tick without target.");
        var reason = faulted ? TerrainOfflineCompletionReason.Faulted : !state.Stopped ? TerrainOfflineCompletionReason.Incomplete :
            finishRequested || point.CompletedUpdates < maximumUpdates ? TerrainOfflineCompletionReason.FinishRequested : TerrainOfflineCompletionReason.BudgetCompleted;
        return new(reason, maximumUpdates, point.CompletedUpdates, point.AttemptedUpdates, state.Stopped,
            faulted, point.Mode.ToString(), speed, targetActive, finishRequested);
    }
}
