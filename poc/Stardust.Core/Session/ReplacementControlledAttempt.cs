namespace Stardust.Core.Session;

public enum ReplacementControlledAttemptStage { NotStarted,LoginHandshake,Credentials,LoginResponses,CharacterSelection,ZoneHandshake,ZonePermissions,SceneReadiness,PlayerPublication,Active }
public enum ReplacementControlledAttemptStopReason { None,PhaseTimeout,OverallTimeout,Canceled,Faulted,Closing,InvalidObservation }
public sealed record ReplacementControlledAttemptObservation(ReplacementClientPhase Phase,bool ClientReadySubmitted,bool ServerReadyReceived,bool VisiblePlayer,bool Faulted=false,bool Closing=false);
public sealed record ReplacementControlledAttemptState(bool Started,ReplacementControlledAttemptStage Stage,ReplacementControlledAttemptStopReason StopReason,long ElapsedMilliseconds,long PhaseElapsedMilliseconds);
public sealed record ReplacementControlledAttemptTransition(ReplacementControlledAttemptStage Stage,long ElapsedMilliseconds);

/// <summary>One caller-clock budget; typed observations never create readiness or dispose owners.</summary>
public sealed class ReplacementControlledAttempt
{
    private readonly int owner=Environment.CurrentManagedThreadId;
    private readonly long phaseBudget,overallBudget;
    private readonly List<ReplacementControlledAttemptTransition> transitions=[];
    private long origin,phaseOrigin,clock=-1;
    private long? stoppedAt;
    private bool started,busy;
    private ReplacementControlledAttemptStage stage;
    private ReplacementControlledAttemptStopReason reason;
    public ReplacementControlledAttempt(long phaseBudgetMilliseconds,long overallBudgetMilliseconds)
    {
        if(phaseBudgetMilliseconds is <1 or >30000||overallBudgetMilliseconds is <1 or >120000||overallBudgetMilliseconds<phaseBudgetMilliseconds)
            throw new ArgumentOutOfRangeException(nameof(phaseBudgetMilliseconds),"Bounded attempt durations required.");
        phaseBudget=phaseBudgetMilliseconds;overallBudget=overallBudgetMilliseconds;
    }
    private void Owner(){if(Environment.CurrentManagedThreadId!=owner)throw new InvalidOperationException("Attempt owner thread required.");}
    private void Enter(long now)
    {
        Owner();if(busy)throw new InvalidOperationException("Attempt reentry.");
        if(now<0||now<clock)throw new ArgumentOutOfRangeException(nameof(now));busy=true;
    }
    public ReplacementControlledAttemptState State
    {get{Owner();long now=stoppedAt??clock;return new(started,stage,reason,started?now-origin:0,started?now-phaseOrigin:0);}}
    public IReadOnlyList<ReplacementControlledAttemptTransition> Transitions {get{Owner();return transitions.ToArray();}}
    public void Start(long nowMilliseconds)
    {
        Enter(nowMilliseconds);try
        {
            if(started)throw new InvalidOperationException("One attempt cannot restart.");
            started=true;clock=origin=phaseOrigin=nowMilliseconds;stage=ReplacementControlledAttemptStage.LoginHandshake;transitions.Add(new(stage,0));
        }finally{busy=false;}
    }
    private void Stop(ReplacementControlledAttemptStopReason value){reason=value;stoppedAt=clock;}
    public ReplacementControlledAttemptState Observe(long nowMilliseconds,ReplacementControlledAttemptObservation observation,CancellationToken cancellationToken=default)
    {
        Enter(nowMilliseconds);try
        {
            ArgumentNullException.ThrowIfNull(observation);if(!started)throw new InvalidOperationException("Actual login start required.");
            clock=nowMilliseconds;if(reason!=ReplacementControlledAttemptStopReason.None)return State;
            if(cancellationToken.IsCancellationRequested)Stop(ReplacementControlledAttemptStopReason.Canceled);
            else if(observation.Faulted||observation.Phase==ReplacementClientPhase.Faulted)Stop(ReplacementControlledAttemptStopReason.Faulted);
            else if(observation.Closing||observation.Phase==ReplacementClientPhase.Closed)Stop(ReplacementControlledAttemptStopReason.Closing);
            else if(clock-origin>=overallBudget)Stop(ReplacementControlledAttemptStopReason.OverallTimeout);
            else if(clock-phaseOrigin>=phaseBudget)Stop(ReplacementControlledAttemptStopReason.PhaseTimeout);
            else
            {
                var next=Map(observation);
                if(next is null||next.Value<stage)Stop(ReplacementControlledAttemptStopReason.InvalidObservation);
                else if(next.Value!=stage){stage=next.Value;phaseOrigin=clock;transitions.Add(new(stage,clock-origin));}
            }
            return State;
        }finally{busy=false;}
    }
    private static ReplacementControlledAttemptStage? Map(ReplacementControlledAttemptObservation o)
    {
        if(o.ServerReadyReceived&&!o.ClientReadySubmitted||o.VisiblePlayer&&!o.ServerReadyReceived
            ||o.Phase!=ReplacementClientPhase.Selected&&(o.ClientReadySubmitted||o.ServerReadyReceived||o.VisiblePlayer))return null;
        return o.Phase switch
        {
            ReplacementClientPhase.LoginHandshake=>ReplacementControlledAttemptStage.LoginHandshake,
            ReplacementClientPhase.LoginAuthentication=>ReplacementControlledAttemptStage.Credentials,
            ReplacementClientPhase.LoginResponses=>ReplacementControlledAttemptStage.LoginResponses,
            ReplacementClientPhase.CharacterList=>ReplacementControlledAttemptStage.CharacterSelection,
            ReplacementClientPhase.ZoneHandshake=>ReplacementControlledAttemptStage.ZoneHandshake,
            ReplacementClientPhase.ZonePermissions=>ReplacementControlledAttemptStage.ZonePermissions,
            ReplacementClientPhase.Selected=>o.VisiblePlayer?ReplacementControlledAttemptStage.Active:o.ServerReadyReceived?ReplacementControlledAttemptStage.PlayerPublication:ReplacementControlledAttemptStage.SceneReadiness,
            _=>null
        };
    }
    public override string ToString(){Owner();return $"controlled_attempt_started={started} stage={stage} stop={reason}";}
}
