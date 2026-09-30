namespace Stardust.Core.Assets;

public readonly record struct PortalActorInputTransition(long TimestampMicroseconds, PortalActorInput Input);

/// <summary>Bounded serialized input history. Changes apply at the first tick whose start is at or after their timestamp.</summary>
public sealed class PortalActorInputTimeline
{
    public const int MaximumPendingTransitions = 1024;
    public const long MaximumFutureMicroseconds = 60_000_000;
    private readonly FixedStepPortalActorHost host;
    private readonly Queue<PortalActorInputTransition> pending = new();
    private long lastSubmitted;
    public PortalActorInput CurrentInput { get; private set; }
    public int PendingTransitions => pending.Count;
    public FixedStepPortalActorState State => host.State;

    public PortalActorInputTimeline(FixedStepPortalActorHost host, long timestampMicroseconds, PortalActorInput initialInput)
    {
        ArgumentNullException.ThrowIfNull(host);
        if (host.State.TimestampMicroseconds.HasValue) throw new ArgumentException("Timeline requires an unanchored host.", nameof(host));
        FixedStepPortalActorHost.ValidateInput(initialInput);
        if (timestampMicroseconds < 0) throw new ArgumentOutOfRangeException(nameof(timestampMicroseconds));
        this.host = host; CurrentInput = initialInput; lastSubmitted = timestampMicroseconds;
        host.AdvanceTo(timestampMicroseconds, initialInput);
    }
    public void Submit(PortalActorInputTransition transition)
    {
        FixedStepPortalActorHost.ValidateInput(transition.Input);
        long now = host.State.TimestampMicroseconds!.Value;
        if (transition.TimestampMicroseconds < now || transition.TimestampMicroseconds < lastSubmitted ||
            transition.TimestampMicroseconds - now > MaximumFutureMicroseconds)
            throw new ArgumentOutOfRangeException(nameof(transition), "Ordered, nonlate, bounded input timestamp required.");
        if (pending.Count >= MaximumPendingTransitions) throw new InvalidOperationException("Input transition capacity reached.");
        pending.Enqueue(transition); lastSubmitted = transition.TimestampMicroseconds;
    }
    private PortalActorInput SelectAt(long timestampMicroseconds)
    {
        while (pending.TryPeek(out var transition) && transition.TimestampMicroseconds <= timestampMicroseconds)
        {
            CurrentInput = pending.Dequeue().Input;
        }
        return CurrentInput;
    }
    public FixedStepPortalActorState AdvanceTo(long timestampMicroseconds, bool active = true)
    {
        var state = host.AdvanceWithInputAtTick(timestampMicroseconds, SelectAt, active);
        // Consume skipped/suspended history through the next tick start, retaining the latest applicable input.
        SelectAt(timestampMicroseconds - state.PendingMicroseconds);
        return state;
    }
    public void Reset(long timestampMicroseconds, PortalActorInput initialInput)
    {
        FixedStepPortalActorHost.ValidateInput(initialInput);
        host.Reset(timestampMicroseconds); pending.Clear(); CurrentInput = initialInput; lastSubmitted = timestampMicroseconds;
    }
}
