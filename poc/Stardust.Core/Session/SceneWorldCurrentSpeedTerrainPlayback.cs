using Stardust.Core.Geometry;

namespace Stardust.Core.Session;

/// <summary>Owns modern grounded playback; borrows the exact world, store, source and retained surface.</summary>
public sealed class SceneWorldCurrentSpeedTerrainPlayback : IDisposable
{
    private readonly int ownerThread = Environment.CurrentManagedThreadId;
    private bool busy, closed;
    private SceneWorldCoordinator? world;
    private SceneWorldObjectMotionStore? motions;
    private SceneWorldSuppliedCurrentSpeedPlan? composition;
    private SceneWorldSuppliedCurrentSpeedPlanReceipt? source;
    private TerrainModernPatchSetSurfaceQuery? surface;
    private SceneWorldTerrainMotionDriver? driver;
    private SceneWorldCurrentSpeedPlanPlayback? playback;
    private SceneWorldCurrentSpeedTerrainPlayback() { }

    public static SceneWorldCurrentSpeedTerrainPlayback? Bind(SceneWorldCoordinator world,
        SceneWorldObjectMotionStore motions, SceneWorldSuppliedCurrentSpeedPlan composition,
        SceneWorldSuppliedCurrentSpeedPlanReceipt source, TerrainModernPatchSetSurfaceQuery surface,
        SceneWorldCurrentSpeedPlaybackPolicy policy, long initialTimestampMicroseconds, long stepMicroseconds,
        float footOffset = 0, SceneWorldTerrainMotionLimits? terrainLimits = null,
        SceneWorldCurrentSpeedPlaybackLimits? playbackLimits = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(world); ArgumentNullException.ThrowIfNull(motions);
        ArgumentNullException.ThrowIfNull(composition); ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(surface); cancellationToken.ThrowIfCancellationRequested();
        if (policy != SceneWorldCurrentSpeedPlaybackPolicy.SuppliedLocalOnlyStoredSpeedOnWorldZ ||
            source.Policy != CreatureSuppliedCurrentSpeedPolicy.SuppliedLocalOnly)
            throw new ArgumentOutOfRangeException(nameof(policy));
        if (initialTimestampMicroseconds < 0 || stepMicroseconds is < 1000 or > 100000 ||
            initialTimestampMicroseconds > long.MaxValue - checked((long)source.Plan.StepCount * stepMicroseconds))
            throw new ArgumentOutOfRangeException(nameof(initialTimestampMicroseconds));
        var p = playbackLimits ?? new(); var t = terrainLimits ?? new();
        if (p.MaximumSteps is < 0 or > 128 || p.MaximumDriverOperations is < 0 or > 384 ||
            p.MaximumOutputBytes is < 0 or > 65536) throw new ArgumentOutOfRangeException(nameof(playbackLimits));
        if (source.Plan.StepCount > p.MaximumSteps || source.Plan.StepCount * 3 > p.MaximumDriverOperations ||
            p.MaximumOutputBytes < SceneWorldCurrentSpeedPlanPlayback.OutputBytes)
            throw new InvalidDataException("Complete playback reservation before initial publication.");
        if (t.MaximumPatches is < 1 or > 256 || t.MaximumTriangleTestsPerCandidate is < 1 or > 2048 ||
            !float.IsFinite(t.MaximumPositionComponent) || t.MaximumPositionComponent <= 0 || t.MaximumPositionComponent > 1e9f ||
            !float.IsFinite(t.MaximumFootOffset) || t.MaximumFootOffset < 0 || t.MaximumFootOffset > 1000f ||
            !float.IsFinite(footOffset) || Math.Abs(footOffset) > t.MaximumFootOffset)
            throw new ArgumentOutOfRangeException(nameof(terrainLimits));
        if (surface.PatchCount < 1 || surface.PatchCount > t.MaximumPatches || surface.TotalTriangles > t.MaximumTriangleTestsPerCandidate)
            throw new InvalidDataException("Complete terrain reservation before initial publication.");
        _ = surface.GetQueryReservation(1);
        foreach (var step in source.Plan.Steps)
        {
            cancellationToken.ThrowIfCancellationRequested();
            float speed = BitConverter.Int32BitsToSingle(unchecked((int)step.Transition.StoredBits));
            if (!float.IsFinite(speed) || Math.Abs(speed) > 10)
                throw new InvalidDataException("Complete stored-speed eligibility before initial publication.");
        }
        if (!motions.IsBoundTo(world) || !composition.IsCurrent(source)) return null;
        var target = source.Pair.View.Target;
        var selected = world.Snapshots().SingleOrDefault(s => s.Object.Handle.ObjectId == source.Incarnation.ObjectId);
        if (world.Generation != source.Generation || world.State is not (ZoneSceneTransitionState.AwaitingServerReady or ZoneSceneTransitionState.Ready) ||
            selected is not { Object.Phase: SceneObjectPhase.Finalized, Replication: not null } ||
            !ReferenceEquals(selected.Object, target.Object) || !ReferenceEquals(selected.Placement, target.Placement) ||
            selected.Object.Incarnation != source.Incarnation || target.Replication is null || !world.IsCurrent(target.Replication)) return null;
        var existing = motions.Find(selected);
        if (existing is not null && (!float.IsFinite(existing.Speed) || existing.Speed < 0 || existing.Speed > 10))
            throw new InvalidDataException("Existing selected motion speed exceeds playback state eligibility before publication.");
        cancellationToken.ThrowIfCancellationRequested();
        var result = new SceneWorldCurrentSpeedTerrainPlayback
        { world = world, motions = motions, composition = composition, source = source, surface = surface };
        try
        {
            result.driver = new(world, motions, source.Incarnation.ObjectId, surface, footOffset,
                initialTimestampMicroseconds, stepMicroseconds, 1, t);
            result.playback = new(composition, source, result.driver, policy, initialTimestampMicroseconds, stepMicroseconds, p);
            cancellationToken.ThrowIfCancellationRequested();
            if (!composition.IsCurrent(source)) { result.Dispose(); return null; }
            return result;
        }
        catch { result.Dispose(); throw; }
    }
    private void Enter()
    {
        if (Environment.CurrentManagedThreadId != ownerThread) throw new InvalidOperationException("Terrain playback owner thread required.");
        if (busy) throw new InvalidOperationException("Terrain playback reentry."); busy = true;
    }
    private T Read<T>(Func<T> get) { Enter(); try { return get(); } finally { busy = false; } }
    public SceneWorldCoordinator? World => Read(() => world);
    public SceneWorldObjectMotionStore? Motions => Read(() => motions);
    public SceneWorldSuppliedCurrentSpeedPlan? Composition => Read(() => composition);
    public SceneWorldSuppliedCurrentSpeedPlanReceipt? Source => Read(() => source);
    public TerrainModernPatchSetSurfaceQuery? Surface => Read(() => surface);
    public SceneWorldTerrainMotionDriver? Driver => Read(() => driver);
    public SceneWorldCurrentSpeedPlanPlayback? Playback => Read(() => playback);
    public SceneWorldCurrentSpeedPlaybackState State => Read(() => closed ? SceneWorldCurrentSpeedPlaybackState.Disposed : playback!.State);
    public SceneWorldCurrentSpeedPlaybackReceipt? AdvanceNext(long submitTimestampMicroseconds,
        long advanceTimestampMicroseconds, CancellationToken cancellationToken = default)
    {
        Enter(); try
        {
            ObjectDisposedException.ThrowIf(closed, this);
            return playback!.AdvanceNext(submitTimestampMicroseconds, advanceTimestampMicroseconds, cancellationToken);
        } finally { busy = false; }
    }
    public bool IsCurrent(SceneWorldCurrentSpeedPlaybackReceipt receipt)
    { Enter(); try { ArgumentNullException.ThrowIfNull(receipt); return !closed && playback!.IsCurrent(receipt); } finally { busy = false; } }
    public void Retire() => Dispose();
    public void Dispose()
    {
        Enter(); try
        {
            if (closed) return; closed = true;
            try { playback?.Dispose(); } finally
            {
                try { driver?.Dispose(); } finally
                { playback = null; driver = null; world = null; motions = null; composition = null; source = null; surface = null; }
            }
        } finally { busy = false; }
    }
}
