using System;
using System.Text.Json;
using Godot;
using Stardust.Core.Session;

public partial class ScenePumpedWorldPreview
{
    private bool pointInteractive,pointConfigured;
    private TerrainOfflinePlaygroundSettings? offlineSettings;
    private TerrainOfflineFocusHost? offlineFocus;
    private Label? interactiveLimits;
    private TerrainOfflineLimitsHud? configuredLimitsHud;
    private bool interactiveGameplayEnabled=true;
    private void PrepareOfflineInteractive()
    {
        if(!pointInteractive)return;
        if(pointConfigured){configuredLimitsHud=new();configuredLimitsHud.Configure(this,()=>new TerrainOfflineLimitsHudState(
            OfflineMaximumUpdates,OfflineMaximumClicks,pointInputAdapter!.RemainingAttempts,interactiveGameplayEnabled,offlineSessionController!.State.Paused));}
        else{interactiveLimits=new(){Name="OfflinePreviewLimits",Text=InteractiveLimitsText(),
            Position=new Vector2(28,284),MouseFilter=Control.MouseFilterEnum.Ignore,FocusMode=Control.FocusModeEnum.None};AddChild(interactiveLimits);}
        offlineFocus=new(){Name="OwnedOfflineFocus"};AddChild(offlineFocus);
        offlineFocus.Configure(this,()=>offlineSessionController!.State.Paused,
            paused=>offlineSessionController!.SetPaused(paused),enabled=>{
                if(!enabled&&pointProfile!.RootElement.GetProperty("InvalidSessionInput").GetBoolean())throw new InvalidOperationException("Prescribed focus gameplay sink rejection");
                manualEventHost!.SetEnabled(enabled);runEventHost!.SetEnabled(enabled);
                cameraOrbitHost!.SetEnabled(enabled);pointEventHost!.SetProcessUnhandledInput(enabled);
                offlineSessionInput!.SetProcessUnhandledInput(enabled);
                pointDriver!.SetMovementSpeed(PointFloat(pointProfile!.RootElement,"WalkSpeedBits"),localClock);
                interactiveGameplayEnabled=enabled;
            });
        GD.Print("offline_interactive_ready="+JsonSerializer.Serialize(new{IdleStart=true,ScriptedCorrection=false,
            MaximumUpdates=OfflineMaximumUpdates,MaximumClickAttempts=OfflineMaximumClicks,Replay=pointProfile!.RootElement.GetProperty("InteractiveReplay").GetBoolean(),
            HoldMissingDestination=!pointConfigured||offlineSettings!.HoldMissingDestination,FocusPolicy="ClearHeldKeysPauseAndRestorePriorPause",SyntheticAcks=true,SocketCreated=false}));
    }
    private void PushInteractiveSessionKey(Key key)
    {
        using var input=new InputEventKey{Keycode=key,PhysicalKeycode=key,Pressed=true};
        GetViewport().PushInput(input,true);
        Check(offlineSessionInput!.LastDiagnostic?.FromGodotEvent==true&&!offlineSessionInput.IsFaulted,"Actual interactive session key");
        GD.Print("offline_interactive_key="+JsonSerializer.Serialize(new{AtStep=pointIndex,Key=key.ToString(),ActualViewportEvent=true,
            Paused=offlineSessionController!.State.Paused,offlineSessionInput.FinishRequested}));
    }
    private void AdvanceOfflineInteractive()
    {
        bool replay=pointProfile!.RootElement.GetProperty("InteractiveReplay").GetBoolean();
        if(replay)switch(pointIndex){
            case 10:PushManualKey(Key.W,true);break;
            case 20:PushRunKey(true);break;
            case 40:GetWindow().EmitSignal(Window.SignalName.FocusExited);PrintInteractiveFocus(false);break;
            case 45:GetWindow().EmitSignal(Window.SignalName.FocusEntered);PrintInteractiveFocus(true);break;
            case 50:PushManualKey(Key.W,true);break;
            case 60:PushRunKey(true);break;
            case 250:PushManualKey(Key.W,false);PushManualKey(Key.S,true);break;
            case 280:PushManualKey(Key.S,false);break;
            case 300:SelectAdaptedPointTarget(2,2);if(pointConfigured)ReplayConfiguredClickBudget();break;
            case 320:PushInteractiveSessionKey(Key.P);break;
            case 325:PushInteractiveSessionKey(Key.P);break;
            case 400:PushInteractiveSessionKey(Key.Escape);break;
        }
        if(manualEventHost!.IsFaulted||runEventHost!.IsFaulted||cameraOrbitHost!.IsFaulted||pointEventHost!.IsFaulted||
            offlineSessionInput!.IsFaulted||offlineFocus!.IsFaulted){
            GD.Print("offline_interactive_failure="+JsonSerializer.Serialize(new{AtStep=pointIndex,
                FocusDiagnostic=offlineFocus?.LastDiagnostic,CompletedUpdates=pointDriver!.State.CompletedUpdates,
                RootSamples=offlineRootSession!.Samples,Paused=offlineSessionController!.State.Paused,RollbackClaimed=false}));
            throw new InvalidOperationException("Interactive input owner faulted; caller cleanup required");
        }
        RefreshCameraRelativeManual();
    }
    private void PrintInteractiveFocus(bool gained)
    {
        GD.Print("offline_interactive_focus="+JsonSerializer.Serialize(new{AtStep=pointIndex,Gained=gained,SyntheticWindowSignal=true,
            GameplayEnabled=interactiveGameplayEnabled,Paused=offlineSessionController!.State.Paused,
            HeldManualEmpty=manualEventHost!.CurrentInput==System.Numerics.Vector2.Zero,RunHeldEmpty=!runEventHost!.IsRunning,
            SpeedIntent=pointDriver!.MovementSpeed,OsFocusProof=false}));
    }
    private void FrameOfflineInteractive()
    {
        if(!pointInteractive)return;
        if(pointConfigured){Check(configuredLimitsHud!.Refresh(),"Configured limits display refresh");GD.Print("offline_configured_limits="+JsonSerializer.Serialize(new{Index=pointIndex,Caption=configuredLimitsHud.Caption}));}
        var receipt=pointDriver!.LastDestinationReceipt;
        GD.Print("offline_interactive_destination="+JsonSerializer.Serialize(new{Index=pointIndex,Receipt=receipt is null?null:new{
            Proposed=new[]{receipt.ProposedPosition.X,receipt.ProposedPosition.Y,receipt.ProposedPosition.Z},
            Velocity=new[]{receipt.AcceptedVelocity.X,receipt.AcceptedVelocity.Y,receipt.AcceptedVelocity.Z},receipt.HeldAtMissingDestination},
            GameplayEnabled=interactiveGameplayEnabled,Paused=offlineSessionController!.State.Paused,ClickAttempts=pointInputAdapter!.Attempts,
            SimulationBudgetRemaining=OfflineMaximumUpdates-pointDriver.State.CompletedUpdates,ModernPolicy=true}));
    }
    private string InteractiveLimitsText()=>pointConfigured?
        $"{OfflineMaximumUpdates*.01:0.0} simulation seconds | Terrain clicks remaining: {pointInputAdapter!.RemainingAttempts} | Limit keeps movement active":
        "60 simulation seconds | 16 terrain clicks | Edges hold movement";
    private void ReplayConfiguredClickBudget()
    {
        // One ray was already admitted at300. Exhaust this replay's explicit bounded click budget.
        Check(OfflineMaximumClicks is >=1 and <=128,"Bounded configured replay click schedule");
        for(int i=1;i<OfflineMaximumClicks;i++)SelectAdaptedPointTarget(2,2);
        var before=pointDriver!.State;var prior=pointInputAdapter!.LastResult;var diagnostic=pointEventHost!.LastDiagnostic;
        int clocks=pointEventHost.ClockReadCount,handled=pointEventHost.HandledCount,picks=pointPicker!.Attempts;
        using var press=new InputEventMouseButton{ButtonIndex=MouseButton.Left,Pressed=true,Position=Vector2.Zero};GetViewport().PushInput(press,true);
        Check(!pointEventHost.IsFaulted&&pointEventHost.ClockReadCount==clocks&&pointEventHost.HandledCount==handled&&pointPicker.Attempts==picks
            &&ReferenceEquals(before,pointDriver.State)&&ReferenceEquals(prior,pointInputAdapter.LastResult)&&ReferenceEquals(diagnostic,pointEventHost.LastDiagnostic),"Exhausted viewport click declines without mutation");
        GD.Print("offline_click_budget_declined="+JsonSerializer.Serialize(new{AtStep=pointIndex,Attempts=pointInputAdapter.Attempts,
            Remaining=pointInputAdapter.RemainingAttempts,IsExhausted=pointInputAdapter.IsExhausted,ClockCallbackSkipped=true,PickerUnchanged=true,
            HostFaulted=pointEventHost.IsFaulted,DriverIntentPreserved=true,LastResultPreserved=true,ActualViewportEvent=true}));
    }
    private void CloseOfflineInteractive()
    {
        CloseOfflineOutcome();
        if(configuredLimitsHud is not null){var hud=configuredLimitsHud;configuredLimitsHud=null;hud.Dispose();hud.Dispose();
            Check(hud.IsRetired&&hud.RetirementCount==1,"Configured limits HUD retired exactly once");
            GD.Print("configured_limits_hud_cleanup="+JsonSerializer.Serialize(new{Retired=hud.IsRetired,hud.RetirementCount,hud.RefreshCount,BorrowedOwnerPreserved=true}));
        }
        var label=interactiveLimits;interactiveLimits=null;if(GodotObject.IsInstanceValid(label))label!.Free();
        var focus=offlineFocus;offlineFocus=null;if(focus is null)return;
        focus.Close();focus.Close();
        GD.Print("offline_interactive_focus_cleanup="+JsonSerializer.Serialize(new{Closed=focus.IsClosed,focus.IsFaulted,
            BorrowedControllerPreserved=offlineSessionController is not null,OsFocusProof=false}));
        if(GodotObject.IsInstanceValid(focus))focus.Dispose();
    }
}
