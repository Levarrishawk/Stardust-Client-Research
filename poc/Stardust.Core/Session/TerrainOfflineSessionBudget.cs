namespace Stardust.Core.Session;

/// <summary>Explicit immutable offline opt-in; never changes requested counts or per-step work limits.</summary>
public sealed class TerrainOfflineSessionBudget
{
    public TerrainOfflineSessionBudget(int maximumUpdates, int maximumTargets)
    {
        if (maximumUpdates is < 1 or > 6000) throw new ArgumentOutOfRangeException(nameof(maximumUpdates));
        if (maximumTargets is < 1 or > 128) throw new ArgumentOutOfRangeException(nameof(maximumTargets));
        MaximumUpdates = maximumUpdates; MaximumTargets = maximumTargets;
    }
    public int MaximumUpdates { get; }
    public int MaximumTargets { get; }
}
