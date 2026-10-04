using System;
using System.Collections.Generic;
using Godot;
using Stardust.Core.Assets;
using Stardust.Core.Session;

/// <summary>Explicit fixed-source native direction caller; owns admission, borrows received reader.</summary>
public partial class SceneWorldSourcedInputHost : Node, IDisposable
{
    private readonly int owner = System.Environment.CurrentManagedThreadId;
    private readonly Func<long> clock;
    private readonly HashSet<Key> held = new();
    private SceneWorldExplicitMovementAdmission? admission;
    private SceneWorldReceivedCreatureMovementInputs? received;
    private SceneWorldMovementSpeedSource source;
    private long generation, timestamp;
    private bool configured, closed, busy, exitRequested, focused = true, paused, unavailable;
    private ActorViewOrientation orientation = new();
    public Exception? Failure { get; private set; }
    public SceneWorldLocalMotionDriverState? LastState { get; private set; }
    public SceneWorldExplicitMovementReceipt? LastAdmission { get; private set; }
    public bool LastStopAccepted { get; private set; }
    public SceneWorldMovementSpeedSource SpeedSource => source;
    public int HeldKeyCount => held.Count;
    public bool IsClosed => closed;
    public bool SourceUnavailable => unavailable;
    public SceneWorldSourcedInputHost() : this(() => checked((long)Time.GetTicksUsec())) { }
    public SceneWorldSourcedInputHost(Func<long> clock) { ArgumentNullException.ThrowIfNull(clock); this.clock = clock; }
    private void OwnerThread()
    {
        if (System.Environment.CurrentManagedThreadId != owner || OS.GetThreadCallerId() != OS.GetMainThreadId())
            throw new InvalidOperationException("Sourced input owner/main thread required.");
    }
    private void Enter()
    {
        OwnerThread(); if (busy) throw new InvalidOperationException("Sourced input reentry.");
        ObjectDisposedException.ThrowIf(closed, this); busy = true;
    }
    private void RequireAdmission()
    { if (!configured || admission is null) throw new InvalidOperationException("Configure admission first."); }
    private long Timestamp(long value)
    { if (value < 0 || value < timestamp) throw new ArgumentOutOfRangeException(nameof(value), "Monotonic nonnegative microseconds required."); return value; }
    private void LiveParent(Node parent)
    {
        if (exitRequested || !IsInsideTree() || !GodotObject.IsInstanceValid(parent) || !parent.IsInsideTree() || GetParent() != parent)
            throw new InvalidOperationException("Live unchanged input parent required.");
    }
    private long ReadClock(SceneWorldExplicitMovementAdmission candidate)
    {
        var parent = GetParent() ?? throw new InvalidOperationException("Live input parent required."); LiveParent(parent);
        var before = candidate.State; long now = Timestamp(clock()); LiveParent(parent);
        if (!ReferenceEquals(candidate.State, before)) throw new InvalidOperationException("Clock callback changed admission state.");
        if (now < before.TimestampMicroseconds) throw new ArgumentException("Clock precedes admission state.");
        return now;
    }
    public void Configure(SceneWorldExplicitMovementAdmission admission, long generation = 0,
        SceneWorldReceivedCreatureMovementInputs? receivedReader = null, ActorViewOrientation? orientation = null)
    {
        Enter(); try
        {
            ArgumentNullException.ThrowIfNull(admission);
            if (configured || generation < 0) throw new InvalidOperationException("Configure once with nonnegative generation.");
            var selected = admission.SpeedSource;
            bool needsReceived = selected is SceneWorldMovementSpeedSource.ReceivedField7 or SceneWorldMovementSpeedSource.ReceivedField11;
            if (needsReceived && (receivedReader is null || generation == 0)) throw new ArgumentException("Explicit received reader and selected generation required.");
            if (!needsReceived && receivedReader is not null) throw new ArgumentException("Received reader supplied for authored source.");
            long now = ReadClock(admission);
            if (!admission.SubmitStop(now)) throw new InvalidOperationException("Selected admission incarnation is no longer current.");
            SetProcessUnhandledInput(true);
            this.admission = admission; received = receivedReader; this.generation = generation;
            source = selected; this.orientation = orientation ?? new(); timestamp = now;
            LastState = admission.State; LastStopAccepted = true; configured = true;
        } finally { busy = false; }
    }
    private bool Issue(long now, HashSet<Key> keys, ActorViewOrientation view, bool enabled)
    {
        var velocity = enabled ? PlanarActorInputMapping.Create(view.PlanarForward(System.Numerics.Vector3.UnitZ),
            (keys.Contains(Key.W) ? 1 : 0) - (keys.Contains(Key.S) ? 1 : 0),
            (keys.Contains(Key.D) ? 1 : 0) - (keys.Contains(Key.A) ? 1 : 0), 1, null).WorldVelocity : System.Numerics.Vector3.Zero;
        SceneWorldExplicitMovementReceipt? selected = null;
        bool missing = false;
        if (velocity != System.Numerics.Vector3.Zero)
        {
            var receipt = received?.Read(generation, admission!.State.ObjectId);
            selected = admission!.Submit(now, new(velocity.X, velocity.Z), receipt);
            missing = selected is null;
        }
        bool stopped = selected is null;
        if (stopped && !admission!.SubmitStop(now)) throw new InvalidOperationException("Selected input incarnation is no longer current.");
        // Commit diagnostics only after one selected movement or one explicit stop is admitted.
        LastAdmission = selected; LastStopAccepted = stopped; unavailable = missing; timestamp = now;
        if (missing) LastState = CheckedAdvance(now, false); // Never catch up stale motion while an observed source is unavailable.
        return !missing;
    }
    private SceneWorldLocalMotionDriverState CheckedAdvance(long now, bool active)
    {
        var state = admission!.Advance(now, active);
        if (state.FaultType is not null) throw new InvalidOperationException("Owned sourced motion faulted: " + state.FaultType);
        return state;
    }
    public bool HandleInput(InputEvent input)
    {
        Enter(); try
        {
            if (!configured || input is not InputEventKey key || key.Echo || (key.Pressed && (key.AltPressed || key.CtrlPressed || key.MetaPressed))) return false;
            Key code = key.PhysicalKeycode != Key.None ? key.PhysicalKeycode : key.Keycode;
            if (code is not (Key.W or Key.A or Key.S or Key.D) || paused || !focused) return false;
            var candidate = new HashSet<Key>(held);
            bool changed = key.Pressed ? candidate.Add(code) : candidate.Remove(code); if (!changed) return false;
            long now = ReadClock(admission!); Issue(now, candidate, orientation, true);
            held.Clear(); held.UnionWith(candidate); return true;
        } catch (Exception error) { Retire(error); throw; } finally { busy = false; }
    }
    public SceneWorldLocalMotionDriverState Advance(long timestampMicroseconds, bool active = true)
    {
        Enter(); try
        {
            RequireAdmission(); long now = Timestamp(timestampMicroseconds); bool enabled = active && focused && !paused;
            var keys = enabled ? held : new HashSet<Key>(); bool available = Issue(now, keys, orientation, enabled);
            if (!enabled) held.Clear();
            LastState = CheckedAdvance(now, enabled && available); timestamp = now; return LastState;
        } catch (Exception error) { Retire(error); throw; } finally { busy = false; }
    }
    public void SetPaused(bool value) => Suspend(value, focused);
    public void SetFocused(bool value) => Suspend(paused, value);
    private void Suspend(bool pause, bool focus)
    {
        Enter(); try
        {
            RequireAdmission(); if (paused == pause && focused == focus) return;
            long now = ReadClock(admission!); Issue(now, new(), orientation, false);
            LastState = CheckedAdvance(now, false); held.Clear(); timestamp = now; paused = pause; focused = focus;
        } catch (Exception error) { Retire(error); throw; } finally { busy = false; }
    }
    public void SetOrientation(ActorViewOrientation value)
    {
        Enter(); try
        {
            RequireAdmission(); ArgumentNullException.ThrowIfNull(value); long now = ReadClock(admission!);
            Issue(now, held, value, focused && !paused); orientation = value;
        } catch (Exception error) { Retire(error); throw; } finally { busy = false; }
    }
    public void Reset(long timestampMicroseconds)
    {
        Enter(); try
        {
            RequireAdmission(); long now = Timestamp(timestampMicroseconds); admission!.Reset(now);
            held.Clear(); orientation = new(); LastAdmission = null; LastStopAccepted = false; unavailable = false;
            timestamp = now; LastState = admission.State;
        } catch (Exception error) { Retire(error); throw; } finally { busy = false; }
    }
    private void Retire(Exception error)
    {
        Failure ??= error;
        try { CloseOwned(timestamp); } catch (Exception close) { Failure = new AggregateException(error, close); }
    }
    private void CloseOwned(long now)
    {
        if (closed) return; closed = true; if (GodotObject.IsInstanceValid(this)) SetProcessUnhandledInput(false);
        var owned = admission; admission = null; received = null; held.Clear(); LastAdmission = null;
        if (owned is not null)
        {
            try
            {
                LastStopAccepted = owned.SubmitStop(now);
                if (LastStopAccepted) LastState = owned.Advance(now, false);
            } finally { owned.Dispose(); }
        }
    }
    public void Close()
    {
        OwnerThread(); if (busy) throw new InvalidOperationException("Sourced input reentry."); if (closed) return;
        busy = true; try { CloseOwned(admission is null ? timestamp : ReadClock(admission)); }
        catch (Exception error) { Retire(error); throw; } finally { busy = false; }
    }
    public new void Dispose() { Close(); base.Dispose(); }
    public override void _EnterTree() { OwnerThread(); SetProcessUnhandledInput(configured && !closed); }
    public override void _UnhandledInput(InputEvent input) { try { HandleInput(input); } catch (Exception error) { Failure ??= error; } }
    public override void _Notification(int what)
    {
        if (!configured || closed) return;
        try
        {
            if (what == NotificationWMWindowFocusOut) SetFocused(false); else if (what == NotificationWMWindowFocusIn) SetFocused(true);
            else if (what == NotificationPaused) SetPaused(true); else if (what == NotificationUnpaused) SetPaused(false);
        } catch (Exception error) { Failure ??= error; }
    }
    public override void _ExitTree()
    {
        OwnerThread(); if (busy) { exitRequested = true; return; }
        try { CloseOwned(timestamp); } catch (Exception error) { Failure ??= error; }
    }
}
