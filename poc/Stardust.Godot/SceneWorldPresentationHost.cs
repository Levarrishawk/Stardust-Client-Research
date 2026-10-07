using System;
using System.Threading;
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
    private Func<double,int>? fixedStepSchedule;
    private Func<bool>? continueFixedSteps;
    private double fixedStepSeconds=.01;
    private Node? fixedStepParent;
    private Viewport? fixedStepViewport;
    private bool scheduledFrame,scheduledConfiguration,scheduledReentered;
    private bool configured, configuring, closing, inFrame, drainFinished;
    private bool deferNativeExitClose;
    private SynchronizationContext? nativeExitContext;
    /// <summary>Opt-in before Configure: native exit latches close, then dispatches pump cleanup after tree removal.</summary>
    public bool DeferNativeExitClose
    {
        get { OwnerThread();return deferNativeExitClose; }
        set { MutableCallbacks();if(configured)throw new InvalidOperationException("Native-exit policy is fixed before configuration.");deferNativeExitClose=value; }
    }
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
        if (inFrame || configuring || closing)
        { if(scheduledFrame||scheduledConfiguration)scheduledReentered=true;throw new InvalidOperationException("Callbacks cannot change during configuration, a frame or after close."); }
    }
    public void Configure(ISceneWorldPresentationPump pump, Action<double>? beforeStep = null)
        => Configure(pump,beforeStep,null,.01,null);
    public void Configure(ISceneWorldPresentationPump pump, Action<double>? beforeStep,
        Func<double,int>? fixedStepSchedule,double fixedStepSeconds=.01,Func<bool>? continueFixedSteps=null)
    {
        OwnerThread();if((scheduledFrame||scheduledConfiguration)&&(inFrame||configuring))scheduledReentered=true;
        ArgumentNullException.ThrowIfNull(pump);
        if (configured || configuring || closing || inFrame)
        { if(scheduledFrame||scheduledConfiguration)scheduledReentered=true;throw new InvalidOperationException("Host configuration is accepted once."); }
        if(!double.IsFinite(fixedStepSeconds)||fixedStepSeconds<.001||fixedStepSeconds>.1)
            throw new ArgumentOutOfRangeException(nameof(fixedStepSeconds));
        if(fixedStepSchedule is null&&(fixedStepSeconds!=.01||continueFixedSteps is not null))
            throw new ArgumentException("Fixed-step options require a schedule.");
        configuring = true;scheduledConfiguration=fixedStepSchedule is not null;scheduledReentered=false;
        try
        {
            if (!GodotObject.IsInstanceValid(this) || !IsInsideTree() || GetParent() is not { } parent ||
                !GodotObject.IsInstanceValid(parent) || !parent.IsInsideTree()) throw new InvalidOperationException("Live parent required before configuration.");
            if (pump.DrainCompletion is null) throw new ArgumentException("Pump must expose drain completion.", nameof(pump));
            var exitContext=deferNativeExitClose?SynchronizationContext.Current:null;
            if(deferNativeExitClose&&exitContext is null)throw new InvalidOperationException("Deferred native exit requires the live Godot main-thread synchronization context.");
            if (closing) throw new InvalidOperationException("Host closed during configuration.");
            if(scheduledConfiguration&&(scheduledReentered||!GodotObject.IsInstanceValid(this)||!IsInsideTree()||
                IsQueuedForDeletion()||!GodotObject.IsInstanceValid(parent)||!parent.IsInsideTree()||parent.IsQueuedForDeletion()||GetParent()!=parent))
                throw new InvalidOperationException("Scheduled configuration changed during drain acquisition.");
            // All rejection checks precede ownership transfer. The caller retains rejected pumps.
            SetProcess(true);
            this.pump = pump; this.beforeStep = beforeStep; configured = true;
            nativeExitContext=exitContext;
            this.fixedStepSchedule=fixedStepSchedule;this.fixedStepSeconds=fixedStepSeconds;this.continueFixedSteps=continueFixedSteps;
            if(fixedStepSchedule is not null){fixedStepParent=parent;fixedStepViewport=GetViewport();}
        }
        finally { configuring = false;scheduledConfiguration=false; }
    }
    public override void _EnterTree() { OwnerThread(); SetProcess(configured && !closing); }
    public override void _Process(double delta)
    {
        OwnerThread();
        if (inFrame||scheduledConfiguration)
        { if(scheduledFrame||scheduledConfiguration)scheduledReentered=true;throw new InvalidOperationException("Presentation frame reentrancy."); }
        if (!configured || closing) return;
        inFrame = true;scheduledFrame=fixedStepSchedule is not null;scheduledReentered=false;
        try
        {
            if (!double.IsFinite(delta) || delta < 0) throw new ArgumentOutOfRangeException(nameof(delta), "Finite nonnegative delta required.");
            if(scheduledFrame){Scheduled(delta);return;}
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
        finally { inFrame = false;scheduledFrame=false;if (drainFinished) Finish(); }
    }
    private bool ScheduledGuard()
    {
        if(scheduledReentered)throw new InvalidOperationException("Scheduled callback attempted host reentry.");
        if(closing)return false;
        if(!GodotObject.IsInstanceValid(this)||!IsInsideTree()||IsQueuedForDeletion()||
            !GodotObject.IsInstanceValid(fixedStepParent)||!fixedStepParent!.IsInsideTree()||fixedStepParent.IsQueuedForDeletion()||
            GetParent()!=fixedStepParent||!GodotObject.IsInstanceValid(fixedStepViewport)||GetViewport()!=fixedStepViewport||fixedStepParent.GetViewport()!=fixedStepViewport)
            throw new InvalidOperationException("Exact live scheduled host parent/viewport required.");
        return true;
    }
    private void Scheduled(double delta)
    {
        if(!ScheduledGuard())return;
        int slots=fixedStepSchedule!(delta);if(!ScheduledGuard())return;
        if(slots is <0 or >8)throw new InvalidOperationException("Schedule must return0..8 slots.");
        for(int index=0;index<slots;index++)
        {
            if(!ScheduledGuard())return;
            if(continueFixedSteps is not null)
            { bool proceed=continueFixedSteps();if(!ScheduledGuard()||!proceed)return; }
            if(beforeStep is not null)
                foreach(Action<double> callback in beforeStep.GetInvocationList())
                {if(!ScheduledGuard())return;callback(fixedStepSeconds);if(!ScheduledGuard())return;}
            if(!ScheduledGuard())return;
            var step=pump!.Step(fixedStepSeconds)??throw new InvalidOperationException("Pump returned no step receipt.");
            if(!ScheduledGuard())return;
            var receipt=new SceneWorldPresentationHostFrame(checked(CompletedFrames+1),fixedStepSeconds,step);
            CompletedFrames=receipt.Index;LastFrame=receipt;
            if(frameCompleted is not null)
                foreach(Action<SceneWorldPresentationPumpStep> callback in frameCompleted.GetInvocationList())
                {if(!ScheduledGuard())return;callback(step);if(!ScheduledGuard())return;}
        }
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
        fixedStepSchedule=null;continueFixedSteps=null;fixedStepParent=null;fixedStepViewport=null;
        nativeExitContext=null;
        var owned = pump; pump = null;
        if (owned is null) { Finish(); return; }
        CloseOwned(owned);
    }
    private void CloseOwned(ISceneWorldPresentationPump owned)
    {
        OwnerThread();
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
        OwnerThread();
        if(!deferNativeExitClose){StartClose(null);return;}
        if(closing)return;
        // No actor Free while Godot's parent is traversing/removing children.
        closing=true;SetProcess(false);beforeStep=null;frameCompleted=null;
        fixedStepSchedule=null;continueFixedSteps=null;fixedStepParent=null;fixedStepViewport=null;
        var owned=pump;pump=null;
        if(owned is null){Finish();return;}
        var context=nativeExitContext!;nativeExitContext=null;
        context.Post(_=>CloseOwned(owned),null); // Managed dispatcher survives native detachment; engine loop must remain alive.
    }
}
