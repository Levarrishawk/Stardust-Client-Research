using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;

/// <summary>Primitive historical completion; no scene/session/resource references.</summary>
public sealed record OfflinePreviewCompletionReport(string Reason,int MaximumUpdates,int CompletedUpdates,int AttemptedUpdates,
    ulong RootFrames,int QueueCount,int PendingMessages,int RetainedPackets,bool ValidOutcome,bool ReadyReached,
    bool CleanupSucceeded,int Actors,int QueuedPublications,int PendingPublications,int OutstandingArchiveJobs,
    int ArchiveCalls,int ArchiveClosed,string? FailureType,string CleanupFailureTypes,bool WindowClosing)
{
    public bool Restartable=>ValidOutcome&&ReadyReached&&CleanupSucceeded&&!WindowClosing&&
        Reason is "BudgetCompleted" or "FinishRequested" or "Faulted";
    public TerrainOfflineOutcomeHudState Outcome=>new(Reason,CompletedUpdates,MaximumUpdates,RootFrames,QueueCount,PendingMessages,RetainedPackets);
}

public partial class ScenePumpedWorldPreview
{
    private string[]? offlineRestartArguments;
    private Action<OfflinePreviewCompletionReport>? offlineRestartCompleted;
    private Action<ScenePumpedWorldPreview>? offlineBeforeRetirement;
    private bool offlineRestartConfigured,offlineRestartNativeExit,offlineRestartDelivered,offlineRestartReachedReady,
        offlineRetirementHookCalled,offlineRestartWindowClosing,offlineFinishCleanupFailed;
    private Task? offlineRestartCleanupTask;
    private OfflinePreviewCompletionReport? offlineRestartSnapshot;
    public OfflinePreviewCompletionReport? OfflineRestartCompletion {get{RestartOwner();return offlineRestartSnapshot;}}
    public bool OfflineRestartReady {get{RestartOwner();return CaptureOfflineRestartReadiness();}}
    public bool OfflineRestartCleanupFinished {get{RestartOwner();return offlineRestartDelivered;}}
    private void RestartOwner(){if(System.Environment.CurrentManagedThreadId!=owner||OS.GetThreadCallerId()!=OS.GetMainThreadId())throw new InvalidOperationException("Preview owner/main thread required.");}
    public void ConfigureOfflineRestart(string[] args,Action<OfflinePreviewCompletionReport> onCompleted,
        Action<ScenePumpedWorldPreview>? beforeRetirement=null)
    {
        RestartOwner();ArgumentNullException.ThrowIfNull(args);ArgumentNullException.ThrowIfNull(onCompleted);
        if(offlineRestartConfigured||offlineRestartNativeExit||IsInsideTree()||GetParent() is not null||IsQueuedForDeletion())throw new InvalidOperationException("Fresh unattached preview required before AddChild.");
        if(args.Length is <2 or >256||args.Any(a=>a is null||a.Length>4096))throw new ArgumentException("Bounded restart arguments.");
        int i=Array.IndexOf(args,"--terrain-point-target-profile");
        if(i<0||i+1>=args.Length||args.Count(a=>a=="--terrain-point-target-profile")!=1)throw new ArgumentException("Configured Version13 profile required.");
        using var stream=File.OpenRead(args[i+1]);if(stream.Length is <=0 or >16384)throw new InvalidDataException("Bounded restart profile.");
        using var profile=JsonDocument.Parse(stream,new JsonDocumentOptions{MaxDepth=4});var p=profile.RootElement;
        if(p.GetProperty("Version").GetInt32()!=13||p.GetProperty("Policy").GetString()!="SuppliedConfiguredOfflinePlayground")throw new InvalidDataException("Only Version13 restart preview admitted.");
        offlineRestartArguments=(string[])args.Clone();offlineRestartCompleted=onCompleted;offlineBeforeRetirement=beforeRetirement;offlineRestartConfigured=true;
    }
    private bool CaptureOfflineRestartReadiness()
    {
        if(!offlineRestartConfigured||offlineRestartNativeExit)return false;
        if(!finishing&&pointConfigured&&pointStarted&&phase==72&&configuredSessionOwner is not null&&pointDriver is not null&&
            offlineSessionController is not null&&offlineRootSession is not null&&runTransport is not null&&manualOutboundQueue is not null&&
            offlineSessionInput is not null&&offlineFocus is not null&&configuredLimitsHud is not null&&offlineHud is not null&&
            manualEventHost is not null&&runEventHost is not null&&cameraOrbitHost is not null&&pointEventHost is not null&&
            suppliedTerrainRayQuery is not null&&world is not null&&host is not null&&pump is not null&&visuals.Count==2)
            offlineRestartReachedReady=true;
        return offlineRestartReachedReady;
    }
    private bool OfflineContinuationAllowed=>!offlineRestartConfigured||!offlineRestartNativeExit&&GodotObject.IsInstanceValid(this)&&IsInsideTree()&&!IsQueuedForDeletion();
    private void BeforeOfflineRetirement()
    {
        if(!offlineRestartConfigured||offlineRetirementHookCalled||offlineRestartSnapshot is not {ValidOutcome:true,ReadyReached:true})return;
        offlineRetirementHookCalled=true;var callback=offlineBeforeRetirement;offlineBeforeRetirement=null;callback?.Invoke(this);
        if(!OfflineContinuationAllowed)throw new InvalidOperationException("Preview exited during retirement callback.");
    }
    private void CompleteOfflineRestart(int exitCode,string? failureType=null,string cleanupFailureTypes="")
    {
        if(!offlineRestartConfigured){GetTree().Quit(exitCode);return;}
        if(offlineRestartDelivered||!OfflineContinuationAllowed)return;
        int actors=pump?.Consumer.Count??0,queued=pump?.QueuedMessageCount??0,pending=pump?.PendingPublicationCount??0,jobsRemaining=world?.OutstandingJobs??0;
        bool drained=!offlineFinishCleanupFailed&&cleanupFailureTypes.Length==0&&actors==0&&queued==0&&pending==0&&jobsRemaining==0&&closed==calls&&jobs.All(j=>j.IsCompleted);
        var old=offlineRestartSnapshot;
        offlineRestartSnapshot=new(old?.Reason??"Incomplete",old?.MaximumUpdates??0,old?.CompletedUpdates??0,old?.AttemptedUpdates??0,
            old?.RootFrames??0,old?.QueueCount??0,old?.PendingMessages??0,old?.RetainedPackets??0,old?.ValidOutcome??false,offlineRestartReachedReady,
            drained,actors,queued,pending,jobsRemaining,calls,closed,failureType,cleanupFailureTypes,offlineRestartWindowClosing);
        offlineRestartDelivered=true;offlineRestartArguments=null;offlineBeforeRetirement=null;var callback=offlineRestartCompleted;offlineRestartCompleted=null;
        GD.Print("offline_preview_completed="+JsonSerializer.Serialize(offlineRestartSnapshot));
        if(drained)jobs.Clear();
        if(offlineRestartSnapshot.Restartable)callback?.Invoke(offlineRestartSnapshot); // Successful actual completion only. Terminal attempts are polled; no native Free here.
    }
    private Task StartOfflineCleanup(Func<Task> work)
    {
        if(offlineRestartCleanupTask is not null)return offlineRestartCleanupTask;
        var completion=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        offlineRestartCleanupTask=completion.Task;_ = RunOfflineCleanup(work,completion);return completion.Task;
    }
    private static async Task RunOfflineCleanup(Func<Task> work,TaskCompletionSource<bool> completion)
    {
        try{await work();completion.TrySetResult(true);}catch(Exception error){completion.TrySetException(error);}
    }
    private async Task DrainOfflineFailureHost(Exception originalFailure)
    {
        var completion=host!.CloseAsync();
        if(!offlineRestartConfigured){await completion;return;}
        // Completion carries the original frame failure as well as cleanup errors. Observe it without
        // relabeling a proven original terrain fault as a cleanup failure; aggregates remain terminal.
        await Task.WhenAny(completion);
        if(!OfflineContinuationAllowed)return;
        if(completion.IsFaulted&&ReferenceEquals(host.Failure,originalFailure)&&originalFailure is not AggregateException&&
            pump!.DrainCompletion.IsCompletedSuccessfully&&world!.DrainCompletion.IsCompletedSuccessfully)
        {
            _=completion.Exception;
            GD.Print("offline_preview_original_fault_drained="+JsonSerializer.Serialize(new{FailureType=originalFailure.GetType().FullName,ActualHostFailure=true,PresentationAndWorldDrained=true}));
            return;
        }
        await completion; // Any distinct/aggregate/drain failure enters the honest cleanup error collector.
    }
    public void RequestOfflineShutdown()
    {
        RestartOwner();if(!offlineRestartConfigured)throw new InvalidOperationException("Restart bridge required.");
        if(offlineRestartDelivered||offlineRestartNativeExit)return;offlineRestartWindowClosing=true;_ = Fail(new OperationCanceledException("Persistent window close"));
    }
    private void OfflineRestartExited()
    {
        offlineRestartNativeExit=true;finishing=true;offlineRestartCompleted=null;offlineBeforeRetirement=null;offlineRestartArguments=null;
    }
}
