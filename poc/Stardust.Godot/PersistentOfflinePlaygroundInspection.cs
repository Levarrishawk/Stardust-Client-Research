using System;
using System.Globalization;
using Godot;

/// <summary>Immutable primitive inspection snapshot. Update counts belong to the last report, not live ticks.</summary>
public sealed record OfflinePlaygroundInspectionStatus(string State,int StartedRuns,int MaximumRuns,int RemainingRuns,
    bool ChildPresent,bool CurrentReady,bool Terminal,bool Closing,bool RestartEnabled,bool CloseEnabled,
    string? Reason,int? CompletedUpdates,int? AttemptedUpdates,bool? Faulted,bool? CleanupSucceeded,
    int? Actors,int? OutstandingArchiveJobs,int? ArchiveCalls,int? ArchiveClosed,ulong? RootFrames,
    string? FailureType,string? CleanupFailureTypes,int CloseRequests,bool WindowCloseConnected,bool QuitIssued);

public partial class PersistentOfflinePlayground
{
    private CanvasLayer? inspectionLayer;
    private Panel? inspectionPanel;
    private Label? inspectionLabel;
    private Button? inspectionCloseButton;
    private Window? inspectionWindow;
    private bool inspectionWindowConnected,inspectionQuitIssued;
    private int inspectionCloseRequests;
    public OfflinePlaygroundInspectionStatus InspectionStatus
    {
        get
        {
            AssertOwner();bool present=!nativeExit&&child is not null&&GodotObject.IsInstanceValid(child)&&child.IsInsideTree();
            bool ready=present&&child!.OfflineRestartReady;
            string state=closing?"Closing":terminal?"Terminal":present?(ready?"Active":"Loading"):started>=maximumRuns&&configured?"RunLimit":"Idle";
            bool restart=active&&!nativeExit&&!terminal&&!closing&&!present&&rejectedCandidate is null&&started<maximumRuns&&hud?.IsRestartEnabled==true;
            return new(state,started,maximumRuns,Math.Max(0,maximumRuns-started),present,ready,terminal,closing,restart,
                active&&!nativeExit&&!closing,last?.Reason,last?.CompletedUpdates,last?.AttemptedUpdates,last is null?null:last.Reason=="Faulted",last?.CleanupSucceeded,
                last?.Actors,last?.OutstandingArchiveJobs,last?.ArchiveCalls,last?.ArchiveClosed,last?.RootFrames,
                last?.FailureType,last?.CleanupFailureTypes,inspectionCloseRequests,inspectionWindowConnected&&GodotObject.IsInstanceValid(inspectionWindow),inspectionQuitIssued);
        }
    }
    public string InspectionCaption {get{AssertOwner();return inspectionLabel is not null&&GodotObject.IsInstanceValid(inspectionLabel)?inspectionLabel.Text:"";}}
    private static string InspectionText(OfflinePlaygroundInspectionStatus s)
    {
        string N(int? value)=>value?.ToString(CultureInfo.InvariantCulture)??"awaiting report";
        string reason=s.Reason switch{"BudgetCompleted"=>"Budget completed","FinishRequested"=>"Finished with Escape","Faulted"=>"Stopped after an error","Incomplete"=>"Incomplete",_=>"No completed report"};
        string availability=s.Closing?"Closing: draining the preview before exit.":s.Terminal?"Restart blocked. Close exits the preview.":s.ChildPresent?"Preview active. Close drains and exits.":s.RemainingRuns==0?"Run limit reached. Restart is disabled.":"Restart available. Close exits the preview.";
        return "Offline preview inspection | "+s.State+"\n"+
            "Runs started: "+s.StartedRuns+" / "+s.MaximumRuns+" | Remaining: "+s.RemainingRuns+"\n"+
            "Last run: "+reason+" | Fault: "+(s.Faulted is null?"unknown":s.Faulted.Value?"yes":"no")+"\n"+
            "Last updates: "+N(s.CompletedUpdates)+" | Attempted: "+N(s.AttemptedUpdates)+"\n"+
            "Cleanup: "+(s.CleanupSucceeded is null?"awaiting report":s.CleanupSucceeded.Value?"drained":"failed")+" | Actors: "+N(s.Actors)+" | Jobs: "+N(s.OutstandingArchiveJobs)+"\n"+
            "Archives closed: "+N(s.ArchiveClosed)+" / "+N(s.ArchiveCalls)+" | Root frames: "+(s.RootFrames?.ToString(CultureInfo.InvariantCulture)??"awaiting report")+"\n"+availability;
    }
    private void PrepareInspection()
    {
        Guard();if(inspectionLayer is not null||inspectionWindowConnected)throw new InvalidOperationException("Inspection ownership accepted once.");
        inspectionWindow=GetWindow();if(!GodotObject.IsInstanceValid(inspectionWindow)||!inspectionWindow!.IsInsideTree())throw new InvalidOperationException("Live owned window required.");
        inspectionWindow.CloseRequested+=OnOwnedWindowClose;inspectionWindowConnected=true;
        inspectionLayer=new CanvasLayer{Name="OwnedOfflineInspection",Layer=24};
        inspectionPanel=new Panel{Name="OfflineInspectionPanel",Position=new(640,328),Size=new(624,196),MouseFilter=Control.MouseFilterEnum.Ignore,FocusMode=Control.FocusModeEnum.None};
        inspectionLabel=new Label{Name="OfflineInspectionStatus",Position=new(12,8),Size=new(600,180),MouseFilter=Control.MouseFilterEnum.Ignore,FocusMode=Control.FocusModeEnum.None,AutowrapMode=TextServer.AutowrapMode.WordSmart};
        inspectionLabel.AddThemeFontSizeOverride("font_size",14);inspectionPanel.AddChild(inspectionLabel);inspectionLayer.AddChild(inspectionPanel);
        inspectionCloseButton=new Button{Name="OfflineCloseButton",Text="Close offline preview",Position=new(272,484),Size=new(240,40),MouseFilter=Control.MouseFilterEnum.Stop,FocusMode=Control.FocusModeEnum.None};
        inspectionCloseButton.AddThemeFontSizeOverride("font_size",16);inspectionCloseButton.Pressed+=OnInspectionClose;inspectionLayer.AddChild(inspectionCloseButton);AddChild(inspectionLayer);RefreshInspection();
    }
    private void OnOwnedWindowClose()=>RequestClose(); // Synthetic signal proof is distinct from physical OS routing.
    private void OnInspectionClose()=>RequestClose(); // Flag only; no Free/quit/drain during GUI callback.
    private void RefreshInspection()
    {
        if(inspectionLayer is null)return;
        if(!GodotObject.IsInstanceValid(inspectionLayer)||inspectionLayer.GetParent()!=this||inspectionLayer.GetViewport()!=fixedViewport||
            !GodotObject.IsInstanceValid(inspectionPanel)||inspectionPanel!.GetParent()!=inspectionLayer||
            !GodotObject.IsInstanceValid(inspectionLabel)||inspectionLabel!.GetParent()!=inspectionPanel||
            !GodotObject.IsInstanceValid(inspectionCloseButton)||inspectionCloseButton!.GetParent()!=inspectionLayer)
            throw new InvalidOperationException("Exact live inspection subtree required.");
        if(hud is not null&&(terminal||closing||started>=maximumRuns))hud.DisableRestart();
        var status=InspectionStatus;inspectionLabel.Text=InspectionText(status);inspectionCloseButton.Disabled=!status.CloseEnabled;
    }
    private void DisconnectInspectionCallbacks()
    {
        if(inspectionWindowConnected&&GodotObject.IsInstanceValid(inspectionWindow))inspectionWindow!.CloseRequested-=OnOwnedWindowClose;
        inspectionWindowConnected=false;inspectionWindow=null;
        if(GodotObject.IsInstanceValid(inspectionCloseButton))inspectionCloseButton!.Pressed-=OnInspectionClose;
    }
    private void RetireInspection()
    {
        DisconnectInspectionCallbacks();var owned=inspectionLayer;inspectionLayer=null;inspectionPanel=null;inspectionLabel=null;inspectionCloseButton=null;
        if(GodotObject.IsInstanceValid(owned))owned!.Free(); // Controlled _Process cleanup only.
    }
    private void ForgetInspection()
    {
        DisconnectInspectionCallbacks();inspectionLayer=null;inspectionPanel=null;inspectionLabel=null;inspectionCloseButton=null; // Native exit never Free.
    }
}
