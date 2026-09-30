namespace Stardust.Core.Assets;

public enum DoorTransitionEvent { None = -1, OpenBegin = 0, OpenEnd = 1, CloseBegin = 2, CloseEnd = 3 }
public readonly record struct DoorControllerState(float Fraction, bool Active, bool Opening, DoorTransitionEvent Transition);

/// <summary>Bounded normal-door controller with explicit frame pulses. Effects, trigger objects and force-field variants remain caller-owned.</summary>
public sealed class PortalDoorController
{
    private readonly float openSeconds, closeSeconds;
    private long? requestFrame;
    private long lastFrame = -1;
    public DoorControllerState State { get; private set; } = new(0, false, false, DoorTransitionEvent.None);
    public PortalDoorController(float openSeconds, float closeSeconds)
    {
        if (!float.IsFinite(openSeconds) || !float.IsFinite(closeSeconds) || openSeconds <= 0 || closeSeconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(openSeconds), "Door durations must be finite and positive.");
        this.openSeconds = openSeconds; this.closeSeconds = closeSeconds;
    }
    public void RequestOpen(long frame)
    {
        if (frame < 0 || frame < lastFrame || requestFrame.HasValue && frame < requestFrame.Value) throw new ArgumentOutOfRangeException(nameof(frame));
        requestFrame = frame; State = State with { Active = true };
    }
    public DoorControllerState Advance(long frame, float seconds)
    {
        if (frame < 0 || frame <= lastFrame || requestFrame.HasValue && frame < requestFrame.Value || !float.IsFinite(seconds) || seconds < 0)
            throw new ArgumentOutOfRangeException(nameof(frame), "Door frames must advance; delta must be finite and nonnegative.");
        bool opening = requestFrame.HasValue && frame - requestFrame.Value <= 1;
        float previous = State.Fraction;
        double raw = previous + (opening ? (double)seconds / openSeconds : -(double)seconds / closeSeconds);
        DoorTransitionEvent transition = previous == 0 && raw > 0 ? DoorTransitionEvent.OpenBegin :
            previous < 1 && raw >= 1 ? DoorTransitionEvent.OpenEnd : previous == 1 && raw < 1 ? DoorTransitionEvent.CloseBegin :
            previous > 0 && raw <= 0 ? DoorTransitionEvent.CloseEnd : DoorTransitionEvent.None;
        float fraction = (float)Math.Clamp(raw, 0, 1);
        State = new(fraction, State.Active && fraction > 0, opening, transition); lastFrame = frame;
        return State;
    }
    public void Reset() { requestFrame = null; lastFrame = -1; State = new(0, false, false, DoorTransitionEvent.None); }
    public bool PermissionBarrierActive(bool doorEligible) => !doorEligible;
    public bool CandidateBarrierActive(bool doorEligible) => State.Active && !doorEligible;
    public bool ModeBarrierActive(bool doorEligible, int rawMode) => !State.Active || !doorEligible && rawMode != 1;
}
