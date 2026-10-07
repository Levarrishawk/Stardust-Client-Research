using System.Numerics;

namespace Stardust.Core.Session;

/// <summary>Operator assertions for the bounded modern fresh-scene experiment, not inferred readiness.</summary>
public sealed record ReplacementControlledMovementScenario(ulong SelectedObjectId, bool FreshStampZero,
    bool MatchingValidatedPosition, bool MovementPrerequisitesValidated);
public enum ReplacementControlledMovementStatus { WaitingReady, WaitingTime, Backpressure, Queued, Completed, Stopped }

/// <summary>Three caller-paced modern horizontal roots over accepted local driver and selected run.</summary>
public sealed class ReplacementControlledMovement : IDisposable
{
    private readonly ReplacementClientRun run;
    private readonly ReplacementClientSession session;
    private readonly SceneWorldCoordinator world;
    private readonly SceneWorldObjectMotionStore motions;
    private readonly ulong selected;
    private readonly long generation;
    private readonly Vector3 direction;
    private readonly int owner = Environment.CurrentManagedThreadId;
    private SceneWorldLocalMotionDriver? driver;
    private SceneWorldObjectMotionSnapshot? publication;
    private SceneObjectIncarnation? incarnation;
    private long clock = -1;
    private long? accepted;
    private int queued;
    private string? stop;
    private bool busy, disposed;

    public ReplacementControlledMovement(ReplacementClientRun run, ReplacementClientSession session,
        SceneWorldCoordinator world, SceneWorldObjectMotionStore motions,
        ReplacementControlledMovementScenario scenario, Vector3 horizontalDirection)
    {
        ArgumentNullException.ThrowIfNull(run); ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(world); ArgumentNullException.ThrowIfNull(motions); ArgumentNullException.ThrowIfNull(scenario);
        if (scenario.SelectedObjectId == 0 || !scenario.FreshStampZero || !scenario.MatchingValidatedPosition || !scenario.MovementPrerequisitesValidated)
            throw new ArgumentException("Explicit verified fresh movement scenario required.", nameof(scenario));
        if (!float.IsFinite(horizontalDirection.X) || !float.IsFinite(horizontalDirection.Z) || horizontalDirection.Y != 0
            || Math.Abs(horizontalDirection.LengthSquared() - 1) > 1e-6)
            throw new ArgumentException("Finite horizontal unit direction required.", nameof(horizontalDirection));
        if (run.Generation <= 0 || run.Generation != world.Generation || run.SelectedObjectId != scenario.SelectedObjectId
            || session.SelectedObjectId != scenario.SelectedObjectId || !motions.IsBoundTo(world))
            throw new ArgumentException("Current selected owners required.");
        this.run=run;this.session=session;this.world=world;this.motions=motions;
        selected=scenario.SelectedObjectId;generation=run.Generation;direction=horizontalDirection;
    }
    public bool Armed { get { Owner();return driver is not null; } }
    public int RootsQueued { get { Owner();return queued; } }
    public string? StopReason { get { Owner();return stop; } }
    public long? LastAcceptedMilliseconds { get { Owner();return accepted; } }
    private void Owner() { if(Environment.CurrentManagedThreadId!=owner)throw new InvalidOperationException("Movement owner thread required."); }
    private void Stop(string reason) { stop??=reason;driver?.Dispose();driver=null; }
    public ReplacementControlledMovementStatus Tick(long nowMilliseconds, CancellationToken cancellationToken=default)
    {
        Owner();if(busy)throw new InvalidOperationException("Movement reentry.");
        if(nowMilliseconds<0||nowMilliseconds<clock)throw new ArgumentOutOfRangeException(nameof(nowMilliseconds));
        busy=true;try
        {
            clock=nowMilliseconds;
            if(disposed||stop is not null)return ReplacementControlledMovementStatus.Stopped;
            if(cancellationToken.IsCancellationRequested){Stop("Canceled");return ReplacementControlledMovementStatus.Stopped;}
            if(run.FailureType is not null||run.DisconnectRequested||session.Phase is ReplacementClientPhase.Closed or ReplacementClientPhase.Faulted
                ||world.State==ZoneSceneTransitionState.Closed){Stop("ClosingOrFaulted");return ReplacementControlledMovementStatus.Stopped;}
            if(run.Generation!=generation||world.Generation!=generation||run.SelectedObjectId!=selected||session.SelectedObjectId!=selected)
            {Stop("SelectionChanged");return ReplacementControlledMovementStatus.Stopped;}
            if(run.AppliedCorrections!=0||run.CounterRejectedCorrections!=0||run.LastInboundStatus is not null)
            {Stop("CorrectionRisk");return ReplacementControlledMovementStatus.Stopped;}
            if(queued==3)return ReplacementControlledMovementStatus.Completed;
            if(session.Phase!=ReplacementClientPhase.Selected||world.State!=ZoneSceneTransitionState.Ready||run.ReadySubmissions!=1)
            {
                if(driver is not null){Stop("ReadyLost");return ReplacementControlledMovementStatus.Stopped;}
                return ReplacementControlledMovementStatus.WaitingReady;
            }
            var snapshot=world.Snapshots().SingleOrDefault(s=>s.Object.Handle.ObjectId==selected);
            if(snapshot is not { Object.Phase: SceneObjectPhase.Finalized, Replication: not null }||snapshot.Object.ContainerId!=0
                ||!world.IsCurrent(snapshot.Replication)||Vector3.DistanceSquared(snapshot.Placement.Transform.RowY,Vector3.UnitY)>1e-8)
            {Stop("UnsupportedPlacement");return ReplacementControlledMovementStatus.Stopped;}
            var current=motions.Find(snapshot);
            if(driver is null)
            {
                if(current is not null&&(current.Authoritative||current.Speed!=0||current.Transform!=snapshot.Placement.Transform))
                {Stop("NonFreshMotion");return ReplacementControlledMovementStatus.Stopped;}
                driver=new(world,motions,selected,0,100000,4);
                driver.Submit(0,new(direction*0.5f,null));
                incarnation=snapshot.Object.Incarnation;publication=motions.Find(snapshot);accepted=nowMilliseconds;
                return ReplacementControlledMovementStatus.WaitingTime;
            }
            if(snapshot.Object.Incarnation!=incarnation||!ReferenceEquals(current,publication)||current is { Authoritative:true })
            {Stop("CorrectionOrForeignMotion");return ReplacementControlledMovementStatus.Stopped;}
            if(nowMilliseconds-accepted!.Value<400)return ReplacementControlledMovementStatus.WaitingTime;
            if(session.ZonePendingMessages!=0)return ReplacementControlledMovementStatus.Backpressure;
            uint next=(uint)queued+1,stamp=next*400;
            var state=driver.Advance(stamp*1000L);
            if(state.FaultType is not null){Stop(state.FaultType);return ReplacementControlledMovementStatus.Stopped;}
            publication=motions.Find(snapshot)??throw new InvalidOperationException("Issued local motion required.");
            if(!run.TryQueueLocal(snapshot,publication,0x23,next,stamp,nowMilliseconds))
            {Stop("UnexpectedAdmissionBackpressure");return ReplacementControlledMovementStatus.Stopped;}
            queued++;accepted=nowMilliseconds;return ReplacementControlledMovementStatus.Queued;
        }
        catch(Exception error){Stop(error.GetType().Name);throw;}
        finally{busy=false;}
    }
    public void Dispose()
    {
        Owner();if(busy)throw new InvalidOperationException("Movement reentry.");
        if(disposed)return;disposed=true;Stop("Disposed");
    }
}
