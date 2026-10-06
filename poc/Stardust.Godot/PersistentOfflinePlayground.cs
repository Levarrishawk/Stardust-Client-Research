using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

/// <summary>Persistent caller shell; one fully retired fresh Version13 preview at a time.</summary>
public partial class PersistentOfflinePlayground : Node
{
    private readonly int ownerThread=System.Environment.CurrentManagedThreadId;
    private Func<int,string[]>? arguments;
    private Func<int,ScenePumpedWorldPreview>? factory;
    private Action<ScenePumpedWorldPreview>? beforeRetirement;
    private ScenePumpedWorldPreview? child,rejectedCandidate;
    private TerrainOfflineRestartHud? hud;
    private OfflinePreviewCompletionReport? pending,last;
    private readonly List<OfflinePreviewCompletionReport> reports=new();
    private Node? fixedParent;
    private Viewport? fixedViewport;
    private bool configured,attempted,active,busy,reentered,terminal,closing,shutdownRequested,nativeExit,readyNotified,terminalReportObserved;
    private int maximumRuns,started,generation,retired;
    public event Action<int>? RunReady;
    public event Action<int,OfflinePreviewCompletionReport>? RunCompleted;
    private void AssertOwner(){if(System.Environment.CurrentManagedThreadId!=ownerThread||OS.GetThreadCallerId()!=OS.GetMainThreadId())throw new InvalidOperationException("Persistent playground owner/main thread required.");}
    private void Guard(bool admission=false){AssertOwner();if(nativeExit||!GodotObject.IsInstanceValid(this)||!IsInsideTree()||IsQueuedForDeletion()||!GodotObject.IsInstanceValid(fixedParent)||fixedParent!.IsQueuedForDeletion()||GetParent()!=fixedParent||GetViewport()!=fixedViewport||admission&&reentered)throw new InvalidOperationException("Exact persistent parent/viewport required.");}
    public ScenePumpedWorldPreview? CurrentPreview {get{AssertOwner();return child;}}
    public bool CurrentReady {get{AssertOwner();return child is not null&&GodotObject.IsInstanceValid(child)&&child.OfflineRestartReady;}}
    public OfflinePreviewCompletionReport? LastCompletedReport {get{AssertOwner();return last;}}
    public OfflinePreviewCompletionReport[] CompletedReports {get{AssertOwner();return reports.ToArray();}}
    public int StartedRuns {get{AssertOwner();return started;}}
    public int RetiredPreviews {get{AssertOwner();return retired;}}
    public bool IsTerminal {get{AssertOwner();return terminal;}}
    public bool IsClosing {get{AssertOwner();return closing;}}
    public string? LastFailureType {get;private set;}
    public string? LastFactoryFailureType {get;private set;}
    public TerrainOfflineRestartHud? RestartHud {get{AssertOwner();return hud;}}
    public string? OutcomeCaption {get{AssertOwner();return hud?.Caption;}}
    public void Configure(Func<int,string[]> arguments,Func<int,ScenePumpedWorldPreview> factory,int maximumRuns,
        Action<ScenePumpedWorldPreview>? beforeRetirement=null)
    {
        AssertOwner();if(attempted||IsInsideTree()||GetParent() is not null)throw new InvalidOperationException("Configure fresh shell before AddChild once.");attempted=true;
        ArgumentNullException.ThrowIfNull(arguments);ArgumentNullException.ThrowIfNull(factory);if(maximumRuns is <1 or >32)throw new ArgumentOutOfRangeException(nameof(maximumRuns));
        this.arguments=arguments;this.factory=factory;this.maximumRuns=maximumRuns;this.beforeRetirement=beforeRetirement;configured=true;
    }
    public override void _Ready()
    {
        try{AssertOwner();if(!configured)throw new InvalidOperationException("Configured persistent shell required.");fixedParent=GetParent();fixedViewport=GetViewport();GetTree().AutoAcceptQuit=false;active=true;Guard();PrepareInspection();if(!TryStart())StopTerminal(LastFactoryFailureType??"InitialFactoryRejected");}
        catch(Exception error){StopTerminal(error.GetType().FullName!);}
    }
    private bool TryStart()
    {
        Guard(true);if(terminal||closing||child is not null||rejectedCandidate is not null||started>=maximumRuns)return false;
        if(busy){reentered=true;throw new InvalidOperationException("Persistent factory reentry.");}
        busy=true;reentered=false;ScenePumpedWorldPreview? candidate=null;bool admitted=false;
        try
        {
            int run=started+1,nextGeneration=checked(generation+1);var args=arguments!(run);Guard(true);
            candidate=factory!(run);
            if(candidate is null||!GodotObject.IsInstanceValid(candidate)||candidate.GetParent() is not null||candidate.IsInsideTree()||candidate.IsQueuedForDeletion())throw new InvalidOperationException("Factory must return one fresh unattached preview.");
            admitted=true;Guard(true);var exact=candidate;
            candidate.ConfigureOfflineRestart(args,report=>Receive(nextGeneration,exact,report),beforeRetirement);Guard(true);
            child=candidate;generation=nextGeneration;started=run;readyNotified=false;terminalReportObserved=false;pending=null;shutdownRequested=false;
            AddChild(candidate);Guard(true);if(!GodotObject.IsInstanceValid(candidate)||candidate.GetParent()!=this)throw new InvalidOperationException("Candidate exited during admission.");
            LastFactoryFailureType=null;GD.Print("persistent_preview_admitted="+JsonSerializer.Serialize(new{Run=started,Generation=generation,ChildId=candidate.GetInstanceId(),SingleChild=true}));return true;
        }
        catch(Exception error)
        {
            LastFactoryFailureType=error.GetType().FullName;
            if(child is not null){StopTerminal(LastFactoryFailureType!);return false;}
            if(admitted&&GodotObject.IsInstanceValid(candidate))rejectedCandidate=candidate;
            return false; // Old outcome survives a pre-AddChild candidate rejection.
        }
        finally{busy=false;}
    }
    private void Receive(int capturedGeneration,ScenePumpedWorldPreview exact,OfflinePreviewCompletionReport report)
    {
        AssertOwner();if(nativeExit||capturedGeneration!=generation||!ReferenceEquals(exact,child)||pending is not null)return;
        pending=report; // Never Free, create a successor, invoke events, or dispose HUD in completion callbacks.
    }
    public bool RequestRestart()
    {
        Guard();if(terminal||closing)return false;
        if(busy){reentered=true;throw new InvalidOperationException("Persistent restart reentry.");}Guard(true);
        if(terminal||closing||child is not null||hud is null||started>=maximumRuns)return false;
        try{return hud.RequestRestart();}catch(Exception error){StopTerminal(error.GetType().FullName!);throw;}
    }
    public void RequestClose(){AssertOwner();if(nativeExit||closing)return;inspectionCloseRequests++;closing=true;arguments=null;factory=null;beforeRetirement=null;}
    private void StopTerminal(string failure){terminal=true;LastFailureType=failure;arguments=null;factory=null;beforeRetirement=null;}
    private void RetireHud(){var old=hud;hud=null;old?.Dispose();}
    public override void _Process(double delta)
    {
        if(!active||nativeExit)return;
        try
        {
            Guard();if(rejectedCandidate is not null){var reject=rejectedCandidate;rejectedCandidate=null;if(GodotObject.IsInstanceValid(reject))reject.Free();}
            if(child is not null&&!GodotObject.IsInstanceValid(child)){child=null;StopTerminal("PreviewDestroyedBeforeCompletion");}
            if(child is not null&&child.OfflineRestartReady&&!readyNotified)
            {
                readyNotified=true;RetireHud();GD.Print("persistent_preview_ready="+JsonSerializer.Serialize(new{Run=started,Generation=generation,ChildId=child.GetInstanceId(),PriorOutcomeRetired=true}));RunReady?.Invoke(started);
            }
            if((closing||terminal)&&child is not null&&!shutdownRequested&&pending is null){shutdownRequested=true;child.RequestOfflineShutdown();}
            if(child is not null&&child.OfflineRestartCleanupFinished&&pending is null&&!terminalReportObserved)pending=child.OfflineRestartCompletion;
            if(pending is not null)
            {
                var report=pending;pending=null;var old=child;
                if(old is null||!GodotObject.IsInstanceValid(old)||!old.OfflineRestartCleanupFinished)throw new InvalidOperationException("Completed preview must remain alive until caller Free.");
                if(report.CleanupSucceeded){child=null;old.Free();retired++;}else terminalReportObserved=true;
                last=report;reports.Add(report);
                RetireHud();if(report.ValidOutcome){hud=new();hud.Configure(this,report.Outcome,TryStart);}
                if(!report.Restartable&&!(closing&&report.CleanupSucceeded))StopTerminal(report.FailureType??"PreviewCleanupOrCompletionRejected");
                GD.Print("persistent_preview_retired="+JsonSerializer.Serialize(new{Run=started,Generation=generation,RetiredPreviews=retired,report.Restartable,report.CleanupSucceeded,ActualFreeBeforeNextFactory=report.CleanupSucceeded}));
                RefreshInspection();RunCompleted?.Invoke(started,report);
            }
            RefreshInspection();
            if(closing&&(child is null||terminalReportObserved)&&!inspectionQuitIssued){inspectionQuitIssued=true;RetireHud();RetireInspection();active=false;GetTree().Quit(terminal?1:0);}
        }
        catch(Exception error){StopTerminal(error.GetType().FullName!);GD.PushError("Persistent preview: "+error);}
    }
    public override void _Notification(int what){if(what==NotificationWMCloseRequest)RequestClose();}
    public override void _ExitTree(){nativeExit=true;active=false;ForgetInspection();child=null;rejectedCandidate=null;hud=null;arguments=null;factory=null;beforeRetirement=null;pending=null;RunReady=null;RunCompleted=null;fixedParent=null;fixedViewport=null;/* Native teardown never Free. Controlled RequestClose drains first. */}
}
