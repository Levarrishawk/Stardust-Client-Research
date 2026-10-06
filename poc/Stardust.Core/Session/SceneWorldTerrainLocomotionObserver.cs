using System.Numerics;
using Stardust.Core.Assets;

namespace Stardust.Core.Session;

public sealed record SceneWorldTerrainLocomotionState(int Observations, long TimestampMicroseconds,
    ulong MotionRevision, Vector3 Baseline, long Generation, LocomotionPreviewState Locomotion,
    double UnwrappedPhase, double GenerationSeconds, long CompletedCycles);
public sealed class SceneWorldTerrainLocomotionReceipt
{
    internal object Owner { get; }
    internal SceneWorldTerrainLocomotionReceipt(object owner, SceneWorldTerrainLocomotionState state,
        SceneWorldLocalMotionDriverState driver, SceneWorldObjectMotionSnapshot motion, LocomotionEventPreviewBatch events)
    { Owner = owner; State = state; Driver = driver; Motion = motion; Events = events; }
    public SceneWorldTerrainLocomotionState State { get; }
    public SceneWorldLocalMotionDriverState Driver { get; }
    public SceneWorldObjectMotionSnapshot Motion { get; }
    public LocomotionEventPreviewBatch Events { get; }
}

/// <summary>Accepted native planar distance observer; exact driver association is the caller's contract.</summary>
public sealed class SceneWorldTerrainLocomotionObserver : IDisposable
{
    private readonly object owner = new();
    private readonly int ownerThread = Environment.CurrentManagedThreadId, maximumObservations;
    private readonly long stepMicroseconds, generation;
    private readonly SceneObjectIncarnation incarnation;
    private SceneWorldCoordinator? world;
    private SceneWorldObjectMotionStore? motions;
    private SceneWorldTerrainMotionDriver? driver;
    private LocomotionEventPreviewClock? clock;
    private SceneWorldTerrainLocomotionState state;
    private bool disposed, busy, faulted;
    public SceneWorldTerrainLocomotionObserver(SceneWorldCoordinator world, SceneWorldObjectMotionStore motions,
        SceneWorldTerrainMotionDriver driver, long stepMicroseconds,
        PreviewLocomotionClip idle, PreviewLocomotionClip walk, PreviewLocomotionClip run, float runThreshold,
        CompressedLocomotion idleMessages, CompressedLocomotion walkMessages, CompressedLocomotion runMessages,
        int maximumObservations = 128, int maximumCyclesPerTick = 64, int maximumEventsPerTick = 4096,
        CancellationToken token = default, TerrainOfflineSessionBudget? budget = null)
    {
        ArgumentNullException.ThrowIfNull(world); ArgumentNullException.ThrowIfNull(motions); ArgumentNullException.ThrowIfNull(driver);
        token.ThrowIfCancellationRequested();
        if (stepMicroseconds is < 1000 or > 100000 || maximumObservations < 1 || maximumObservations > (budget?.MaximumUpdates ?? 256))
            throw new ArgumentOutOfRangeException(nameof(stepMicroseconds));
        this.world = world; this.motions = motions; this.driver = driver; this.stepMicroseconds = stepMicroseconds;
        this.maximumObservations = maximumObservations; generation = world.Generation;
        var actual = driver.State; incarnation = Current(actual.ObjectId).Object.Incarnation; Healthy(actual);
        if (budget is not null && actual.TimestampMicroseconds > long.MaxValue-checked((long)maximumObservations*stepMicroseconds))
            throw new ArgumentOutOfRangeException(nameof(maximumObservations));
        var motion = motions.Find(Current(actual.ObjectId)) ?? throw new InvalidDataException("Initial issued local motion required.");
        Local(motion, actual);
        var candidate = new LocomotionEventPreviewClock(idle, walk, run, runThreshold, idleMessages, walkMessages, runMessages,
            maximumCyclesPerTick, maximumEventsPerTick);
        token.ThrowIfCancellationRequested(); clock = candidate;
        state = new(0, actual.TimestampMicroseconds, motion.Revision, actual.Transform.Translation, clock.Generation,
            clock.State, clock.UnwrappedPhase, clock.GenerationSeconds, clock.CompletedCycles);
    }
    private void Enter()
    {
        if (Environment.CurrentManagedThreadId != ownerThread) throw new InvalidOperationException("Locomotion observer owner thread required.");
        if (busy) throw new InvalidOperationException("Locomotion observer reentry."); busy = true;
    }
    public SceneWorldTerrainLocomotionState State { get { Enter(); try { return state; } finally { busy = false; } } }
    private SceneWorldObjectSnapshot Current(ulong objectId)
    {
        if (!motions!.IsBoundTo(world!) || world!.Generation != generation ||
            world.State is not (ZoneSceneTransitionState.Ready or ZoneSceneTransitionState.AwaitingServerReady))
            throw new InvalidDataException("Current world/store required.");
        var snapshot = world.Snapshots().SingleOrDefault(s => s.Object.Handle.ObjectId == objectId);
        if (snapshot is not { Object.Phase: SceneObjectPhase.Finalized, Replication: not null } || snapshot.Object.ContainerId != 0 ||
            !world.IsCurrent(snapshot.Replication) || (incarnation != default && snapshot.Object.Incarnation != incarnation))
            throw new InvalidDataException("Current finalized typed root incarnation required.");
        return snapshot;
    }
    private static void Position(Vector3 p)
    {
        if (!float.IsFinite(p.X) || !float.IsFinite(p.Y) || !float.IsFinite(p.Z) || Math.Abs(p.X)>1e6 || Math.Abs(p.Y)>1e6 || Math.Abs(p.Z)>1e6)
            throw new InvalidDataException("Finite bounded observer position required.");
    }
    private static void Healthy(SceneWorldLocalMotionDriverState actual)
    {
        Position(actual.Transform.Translation);
        if (!actual.Active || actual.FaultType is not null || actual.TimestampMicroseconds < 0 || actual.DroppedMicroseconds != 0 ||
            actual.SuspendedMicroseconds != 0 || !float.IsFinite(actual.Speed) || actual.Speed < 0 || actual.Speed > 10)
            throw new InvalidDataException("Healthy bounded actual driver required.");
    }
    private void Local(SceneWorldObjectMotionSnapshot row, SceneWorldLocalMotionDriverState actual)
    {
        if (row.Authoritative || row.MovementCounter is not null || row.Incarnation != incarnation || !motions!.IsCurrent(row) ||
            row.Transform != actual.Transform || row.Speed != actual.Speed)
            throw new InvalidDataException("Exact current issued LOCAL row matching actual driver required.");
    }
    private void Admit(CancellationToken token)
    {
        ObjectDisposedException.ThrowIf(disposed, this); token.ThrowIfCancellationRequested();
        if (faulted) throw new InvalidOperationException("Observer post-commit fault.");
        if (state.Observations >= maximumObservations) throw new InvalidOperationException("Observation capacity.");
    }
    public void Rebase(SceneWorldObjectMotionSnapshot authoritative, long exactDriverClock, CancellationToken token = default)
    {
        Enter(); try
        {
            Admit(token); ArgumentNullException.ThrowIfNull(authoritative);
            var actual = driver!.State; Healthy(actual); var selected = Current(actual.ObjectId);
            if (actual.TimestampMicroseconds != state.TimestampMicroseconds || exactDriverClock != state.TimestampMicroseconds ||
                !authoritative.Authoritative || authoritative.MovementCounter is null || authoritative.Incarnation != incarnation ||
                authoritative.Revision <= state.MotionRevision || !ReferenceEquals(motions!.Find(selected), authoritative) || !motions.IsCurrent(authoritative))
                throw new InvalidDataException("Fresh actual authoritative rebase before driver step required.");
            Position(authoritative.Transform.Translation);
            if (!float.IsFinite(authoritative.Speed) || authoritative.Speed < 0 || authoritative.Speed > 10)
                throw new InvalidDataException("Authoritative observer speed bound.");
            var candidate = state with { Baseline = authoritative.Transform.Translation, MotionRevision = authoritative.Revision };
            token.ThrowIfCancellationRequested(); state = candidate;
        } finally { busy = false; }
    }
    public SceneWorldTerrainLocomotionReceipt Advance(long exactDriverClock, CancellationToken token = default)
    {
        Enter(); try
        {
            Admit(token); var actual = driver!.State; Healthy(actual); var selected = Current(actual.ObjectId);
            if (state.TimestampMicroseconds > long.MaxValue-stepMicroseconds || exactDriverClock != state.TimestampMicroseconds+stepMicroseconds ||
                actual.TimestampMicroseconds != exactDriverClock || actual.StepsThisUpdate != 1)
                throw new InvalidDataException("One exact consecutive configured driver step required.");
            var row = motions!.Find(selected) ?? throw new InvalidDataException("Current local row missing."); Local(row,actual);
            if (state.MotionRevision == ulong.MaxValue || row.Revision != state.MotionRevision+1)
                throw new InvalidDataException("Unseen publication; authoritative correction requires pre-step rebase.");
            var delta = actual.Transform.Translation-state.Baseline; delta.Y = 0; Position(delta);
            double seconds = (exactDriverClock-state.TimestampMicroseconds)/1_000_000.0;
            token.ThrowIfCancellationRequested();
            // Frozen Tick preflights atomically. No cancellation/foreign calls follow its commit.
            var events = clock!.Tick(seconds,delta);
            try
            {
                state = new(state.Observations+1,exactDriverClock,row.Revision,actual.Transform.Translation,clock.Generation,
                    clock.State,clock.UnwrappedPhase,clock.GenerationSeconds,clock.CompletedCycles);
                return new(owner,state,actual,row,events);
            }
            catch { faulted = true; throw; } // Clock prefix survives; never retry a committed event traversal.
        } finally { busy = false; }
    }
    public bool IsCurrent(SceneWorldTerrainLocomotionReceipt receipt)
    {
        Enter(); try
        {
            ArgumentNullException.ThrowIfNull(receipt);
            if (disposed || !ReferenceEquals(receipt.Owner,owner) || receipt.State.Generation != state.Generation) return false;
            _ = Current(incarnation.ObjectId); return true;
        } finally { busy = false; }
    }
    public void Dispose()
    { Enter(); try { if(disposed)return; disposed=true; world=null; motions=null; driver=null; clock=null; } finally { busy=false; } }
}
