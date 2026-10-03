using System;
using System.Threading.Tasks;
using Godot;
using Stardust.Core.Session;

public sealed record SceneWorldPresentationHostFrame(ulong Index, double Seconds, SceneWorldPresentationPumpStep Step);

/// <summary>Main-thread process host. Owns an accepted pump and its close/drain/disposal.</summary>
public partial class SceneWorldPresentationHost : Node
{
    private readonly int ownerThread = System.Environment.CurrentManagedThreadId;
    private readonly TaskCompletionSource<bool> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private ISceneWorldPresentationPump? pump;
    private Action<double>? beforeStep;
    private Action<SceneWorldPresentationPumpStep>? frameCompleted;
    private bool configured, configuring, closing, inFrame, drainFinished;
    public Task Completion => completion.Task;
    public Exception? Failure { get; private set; }
    public bool IsClosing => closing;
    public ulong CompletedFrames { get; private set; }
    public SceneWorldPresentationHostFrame? LastFrame { get; private set; }
    public Action<double>? BeforeStep
    {
        get { OwnerThread(); return beforeStep; }
        set { MutableCallbacks(); beforeStep = value; }
    }
    public event Action<SceneWorldPresentationPumpStep>? FrameCompleted
    {
        add { MutableCallbacks(); frameCompleted += value; }
        remove { MutableCallbacks(); frameCompleted -= value; }
    }
    private void OwnerThread()
    {
        if (System.Environment.CurrentManagedThreadId != ownerThread || OS.GetThreadCallerId() != OS.GetMainThreadId())
            throw new InvalidOperationException("Presentation host requires the creating Godot main thread.");
    }
    private void MutableCallbacks()
    {
        OwnerThread();
        if (inFrame || configuring || closing) throw new InvalidOperationException("Callbacks cannot change during configuration, a frame or after close.");
    }
    public void Configure(ISceneWorldPresentationPump pump, Action<double>? beforeStep = null)
    {
        OwnerThread(); ArgumentNullException.ThrowIfNull(pump);
        if (configured || configuring || closing || inFrame) throw new InvalidOperationException("Host configuration is accepted once.");
        configuring = true;
        try
        {
            if (!GodotObject.IsInstanceValid(this) || !IsInsideTree() || GetParent() is not { } parent ||
                !GodotObject.IsInstanceValid(parent) || !parent.IsInsideTree()) throw new InvalidOperationException("Live parent required before configuration.");
            if (pump.DrainCompletion is null) throw new ArgumentException("Pump must expose drain completion.", nameof(pump));
            if (closing) throw new InvalidOperationException("Host closed during configuration.");
            // All rejection checks precede ownership transfer. The caller retains rejected pumps.
            SetProcess(true);
            this.pump = pump; this.beforeStep = beforeStep; configured = true;
        }
        finally { configuring = false; }
    }
    public override void _EnterTree() { OwnerThread(); SetProcess(configured && !closing); }
    public override void _Process(double delta)
    {
        OwnerThread();
        if (inFrame) throw new InvalidOperationException("Presentation frame reentrancy.");
        if (!configured || closing) return;
        inFrame = true;
        try
        {
            if (!double.IsFinite(delta) || delta < 0) throw new ArgumentOutOfRangeException(nameof(delta), "Finite nonnegative delta required.");
            if (beforeStep is not null)
                foreach (Action<double> callback in beforeStep.GetInvocationList())
                { if (closing) break; callback(delta); }
            if (closing) return;
            var step = pump!.Step(delta) ?? throw new InvalidOperationException("Pump returned no step receipt.");
            if (closing) return;
            var receipt = new SceneWorldPresentationHostFrame(checked(CompletedFrames + 1), delta, step);
            CompletedFrames = receipt.Index; LastFrame = receipt;
            if (frameCompleted is not null)
                foreach (Action<SceneWorldPresentationPumpStep> callback in frameCompleted.GetInvocationList())
                { if (closing) break; callback(step); }
        }
        catch (Exception error) { StartClose(error); }
        finally { inFrame = false; if (drainFinished) Finish(); }
    }
    public Task CloseAsync()
    {
        OwnerThread(); StartClose(null); return Completion;
    }
    private void Remember(Exception error)
    {
        Failure = Failure is null ? error : new AggregateException("Presentation host failures.", Failure, error);
    }
    private void StartClose(Exception? error)
    {
        if (error is not null) Remember(error);
        if (closing) return;
        closing = true; SetProcess(false); beforeStep = null; frameCompleted = null;
        var owned = pump; pump = null;
        if (owned is null) { Finish(); return; }
        try { owned.Close(); } catch (Exception closeError) { Remember(closeError); }
        Task drain;
        try { drain = owned.DrainCompletion ?? throw new InvalidOperationException("Pump lost drain completion."); }
        catch (Exception drainError) { Remember(drainError); drain = Task.CompletedTask; }
        _ = DrainAndDispose(owned, drain); // Completion is the observable, nonthrowing handoff from this method.
    }
    private async Task DrainAndDispose(ISceneWorldPresentationPump owned, Task drain)
    {
        try { await drain; } catch (Exception drainError) { Remember(drainError); }
        try { OwnerThread(); owned.Dispose(); } catch (Exception disposeError) { Remember(disposeError); }
        Finish();
    }
    private void Finish()
    {
        drainFinished = true;
        if (inFrame) return; // A closing callback may still throw before this frame unwinds.
        if (Failure is null) completion.TrySetResult(true); else completion.TrySetException(Failure);
    }
    public override void _ExitTree()
    {
        OwnerThread(); StartClose(null); // Synchronous close; callers await Completion while the engine remains alive.
    }
}
