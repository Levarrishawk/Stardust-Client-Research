using System.Numerics;

namespace Stardust.Core.Assets;

public sealed record FixedStepPortalActorState(PortalDoorActorState Actor, long? TimestampMicroseconds, int StepsThisUpdate,
    long PendingMicroseconds, long DroppedMicroseconds, long SuspendedMicroseconds, bool Active, string? FaultType);

/// <summary>Single-door compatibility facade over the shared typed fixed-step scheduler. Borrows the actor.</summary>
public sealed class FixedStepPortalActorHost
{
    internal FixedStepActorHost<PortalDoorActorState> Scheduler { get; }
    public int StepMicroseconds => Scheduler.StepMicroseconds;
    public int MaximumStepsPerUpdate => Scheduler.MaximumStepsPerUpdate;
    public FixedStepPortalActorState State => Convert(Scheduler.State);
    public FixedStepPortalActorHost(PortalDoorActorSimulation actor, int stepMicroseconds = 20_000, int maximumStepsPerUpdate = 8)
    {
        ArgumentNullException.ThrowIfNull(actor);
        Scheduler = new(() => actor.State, (seconds, delta, allowed) => actor.Tick(seconds, delta, allowed), actor.Reset, stepMicroseconds, maximumStepsPerUpdate);
    }
    public FixedStepPortalActorState AdvanceTo(long timestampMicroseconds, PortalActorInput input, bool active = true) =>
        Convert(Scheduler.AdvanceTo(timestampMicroseconds, input, active));
    internal static void ValidateInput(PortalActorInput input) => FixedStepActorHost<PortalDoorActorState>.ValidateInput(input);
    internal FixedStepPortalActorState AdvanceWithInputAtTick(long timestampMicroseconds, Func<long, PortalActorInput> inputAtTick, bool active) =>
        Convert(Scheduler.AdvanceWithInputAtTick(timestampMicroseconds, inputAtTick, active));
    public void Reset(long timestampMicroseconds) => Scheduler.Reset(timestampMicroseconds);
    private static FixedStepPortalActorState Convert(FixedStepActorState<PortalDoorActorState> state) =>
        new(state.Actor, state.TimestampMicroseconds, state.StepsThisUpdate, state.PendingMicroseconds, state.DroppedMicroseconds,
            state.SuspendedMicroseconds, state.Active, state.FaultType);
}
