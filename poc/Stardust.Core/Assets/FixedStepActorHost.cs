using System.Numerics;

namespace Stardust.Core.Assets;

public readonly record struct PortalActorInput(Vector3 WorldVelocity, bool? EntryAllowed);
public sealed record FixedStepActorState<TState>(TState Actor, long? TimestampMicroseconds, int StepsThisUpdate,
    long PendingMicroseconds, long DroppedMicroseconds, long SuspendedMicroseconds, bool Active, string? FaultType);

/// <summary>Serialized modern render-to-simulation scheduler. Borrows the actor; integer timing, bounded catch-up and explicit suspension.</summary>
public sealed class FixedStepActorHost<TState>
{
    private readonly Func<TState> readState;
    private readonly Action<float, Vector3, bool?> tick;
    private readonly Action reset;
    private bool updating;
    public int StepMicroseconds { get; }
    public int MaximumStepsPerUpdate { get; }
    public FixedStepActorState<TState> State { get; private set; }
    public FixedStepActorHost(Func<TState> readState, Action<float, Vector3, bool?> tick, Action reset, int stepMicroseconds = 20_000, int maximumStepsPerUpdate = 8)
    {
        ArgumentNullException.ThrowIfNull(readState); ArgumentNullException.ThrowIfNull(tick); ArgumentNullException.ThrowIfNull(reset);
        if (stepMicroseconds is < 1_000 or > 100_000 || maximumStepsPerUpdate is < 1 or > 32) throw new ArgumentOutOfRangeException(nameof(stepMicroseconds));
        this.readState = readState; this.tick = tick; this.reset = reset; StepMicroseconds = stepMicroseconds; MaximumStepsPerUpdate = maximumStepsPerUpdate;
        State = new(readState(), null, 0, 0, 0, 0, true, null);
    }
    public FixedStepActorState<TState> AdvanceTo(long timestampMicroseconds, PortalActorInput input, bool active = true)
    {
        ValidateInput(input);
        return AdvanceWithInputAtTick(timestampMicroseconds, _ => input, active);
    }
    internal static void ValidateInput(PortalActorInput input)
    {
        Vector3 v = input.WorldVelocity;
        if (!float.IsFinite(v.X) || !float.IsFinite(v.Y) || !float.IsFinite(v.Z) || Math.Abs(v.X) > 10 || Math.Abs(v.Y) > 10 || Math.Abs(v.Z) > 10)
            throw new ArgumentOutOfRangeException(nameof(input), "Finite bounded input required.");
    }
    internal FixedStepActorState<TState> AdvanceWithInputAtTick(long timestampMicroseconds, Func<long, PortalActorInput> inputAtTick, bool active)
    {
        if (updating) throw new InvalidOperationException("Fixed-step updates cannot reenter.");
        updating = true;
        try { return AdvanceCore(timestampMicroseconds, inputAtTick, active); }
        finally { updating = false; }
    }
    private FixedStepActorState<TState> AdvanceCore(long timestampMicroseconds, Func<long, PortalActorInput> inputAtTick, bool active)
    {
        if (timestampMicroseconds < 0 || State.TimestampMicroseconds.HasValue && timestampMicroseconds < State.TimestampMicroseconds.Value)
            throw new ArgumentOutOfRangeException(nameof(timestampMicroseconds), "Monotonic timestamp required.");
        if (!State.TimestampMicroseconds.HasValue)
        {
            State = State with { TimestampMicroseconds = timestampMicroseconds, StepsThisUpdate = 0, Active = active }; return State;
        }
        long elapsed = timestampMicroseconds - State.TimestampMicroseconds.Value;
        long available = checked(State.PendingMicroseconds + elapsed);
        if (!active || !State.Active || State.FaultType is not null)
        {
            long suspended = checked(State.SuspendedMicroseconds + available);
            State = State with { Actor = readState(), TimestampMicroseconds = timestampMicroseconds, StepsThisUpdate = 0, PendingMicroseconds = 0, SuspendedMicroseconds = suspended, Active = active };
            return State;
        }
        int planned = (int)Math.Min(available / StepMicroseconds, MaximumStepsPerUpdate);
        long remaining = available - (long)planned * StepMicroseconds;
        long dropped = remaining / StepMicroseconds * StepMicroseconds;
        long totalDropped = checked(State.DroppedMicroseconds + dropped);
        int completed = 0;
        try
        {
            float seconds = StepMicroseconds / 1_000_000f;
            long firstTickStart = State.TimestampMicroseconds.Value - State.PendingMicroseconds;
            for (; completed < planned; completed++)
            {
                PortalActorInput input = inputAtTick(firstTickStart + (long)completed * StepMicroseconds);
                tick(seconds, input.WorldVelocity * seconds, input.EntryAllowed);
            }
        }
        catch (Exception failure) when (failure is InvalidDataException or ArgumentException or InvalidOperationException)
        {
            // Retain completed work and stop this scheduler until explicit reset. Do not retry a failed tick implicitly.
            long discarded = available - (long)completed * StepMicroseconds;
            State = new(readState(), timestampMicroseconds, completed, 0, checked(State.DroppedMicroseconds + discarded), State.SuspendedMicroseconds, active, failure.GetType().Name);
            return State;
        }
        State = new(readState(), timestampMicroseconds, completed, remaining - dropped, totalDropped, State.SuspendedMicroseconds, active, null);
        return State;
    }
    public void Reset(long timestampMicroseconds)
    {
        if (updating) throw new InvalidOperationException("Cannot reset during a fixed-step update.");
        if (timestampMicroseconds < 0) throw new ArgumentOutOfRangeException(nameof(timestampMicroseconds));
        reset(); State = new(readState(), timestampMicroseconds, 0, 0, 0, 0, true, null);
    }
}
