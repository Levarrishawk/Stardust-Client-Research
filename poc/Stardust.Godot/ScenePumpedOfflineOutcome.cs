using System;
using System.Text.Json;
using Godot;
using Stardust.Core.Session;

public partial class ScenePumpedWorldPreview
{
    private TerrainOfflineOutcomeHud? offlineOutcomeHud;
    private bool offlineOutcomeRecorded;
    private void RecordConfiguredOutcome()
    {
        if(!pointConfigured||!pointStarted||offlineOutcomeRecorded||offlineSessionController is null||offlineRootSession is null||runTransport is null||manualOutboundQueue is null)return;
        var completion=TerrainOfflineSessionCompletion.Capture(offlineSessionController,OfflineMaximumUpdates,offlineSessionInput?.FinishRequested??false);
        var roots=OfflineSoeRootMotionCompletion.Capture(offlineRootSession,runTransport,manualOutboundQueue);
        GD.Print("offline_completion_summary="+JsonSerializer.Serialize(new{Reason=completion.Reason.ToString(),completion.MaximumUpdates,
            completion.CompletedUpdates,completion.AttemptedUpdates,completion.Stopped,completion.Faulted,completion.Mode,completion.Speed,completion.TargetActive}));
        GD.Print("offline_root_completion_summary="+JsonSerializer.Serialize(roots));
        offlineOutcomeHud=new();offlineOutcomeHud.Configure(this,()=>new TerrainOfflineOutcomeHudState(completion.Reason.ToString(),
            completion.CompletedUpdates,completion.MaximumUpdates,roots.Drained,roots.QueueCount,roots.PendingMessages,roots.RetainedPackets));
        GD.Print("offline_outcome_caption="+JsonSerializer.Serialize(new{Reason=completion.Reason.ToString(),Caption=offlineOutcomeHud.Caption,
            Source="ActualControllerAndRootSnapshot",SocketCreated=false}));
        offlineOutcomeRecorded=true;
    }
    private void CloseOfflineOutcome()
    {
        var hud=offlineOutcomeHud;offlineOutcomeHud=null;if(hud is null)return;
        hud.Dispose();hud.Dispose();Check(hud.IsRetired&&hud.RetirementCount==1,"Outcome display retires exactly once");
        GD.Print("offline_outcome_hud_cleanup="+JsonSerializer.Serialize(new{Retired=hud.IsRetired,hud.RetirementCount,hud.RefreshCount,BorrowedOwnerPreserved=true}));
    }
}
