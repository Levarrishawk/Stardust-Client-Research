namespace Stardust.Core.Assets;

/// <summary>Single-door compatibility facade over the shared typed timestamped input timeline.</summary>
public sealed class PortalActorInputTimeline
{
    public const int MaximumPendingTransitions = PortalActorInputTimeline<PortalDoorActorState>.MaximumPendingTransitions;
    public const long MaximumFutureMicroseconds = PortalActorInputTimeline<PortalDoorActorState>.MaximumFutureMicroseconds;
    private readonly FixedStepPortalActorHost host;
    private readonly PortalActorInputTimeline<PortalDoorActorState> timeline;
    public PortalActorInput CurrentInput => timeline.CurrentInput;
    public int PendingTransitions => timeline.PendingTransitions;
    public FixedStepPortalActorState State => host.State;
    public PortalActorInputTimeline(FixedStepPortalActorHost host, long timestampMicroseconds, PortalActorInput initialInput)
    {
        ArgumentNullException.ThrowIfNull(host); this.host = host;
        timeline = new(host.Scheduler, timestampMicroseconds, initialInput);
    }
    public void Submit(PortalActorInputTransition transition) => timeline.Submit(transition);
    public FixedStepPortalActorState AdvanceTo(long timestampMicroseconds, bool active = true)
    { timeline.AdvanceTo(timestampMicroseconds, active); return host.State; }
    public void Reset(long timestampMicroseconds, PortalActorInput initialInput) => timeline.Reset(timestampMicroseconds, initialInput);
}
