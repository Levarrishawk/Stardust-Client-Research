using System;
using System.Linq;
using System.Text.Json;
using Godot;
using Stardust.Core.Session;

public partial class ScenePumpedWorldPreview
{
    private bool pointOfflineHosted;
    private TerrainOfflineSessionBudget? offlineSessionBudget;
    private TerrainOfflinePlaygroundHud? offlineHud;
    private SceneWorldRootMotionCadenceOutbox? offlineOutbox;
    private long offlineStartClock;
    private int OfflineMaximumUpdates=>pointConfigured?offlineSettings!.MaximumUpdates:pointInteractive?6000:pointOfflineHosted?1000:128;
    private int OfflineMaximumClicks=>pointConfigured?offlineSettings!.MaximumClickAttempts:16;
    private void PrepareOfflinePlayground()
    {
        if(!pointOfflineHosted)return;
        var p=pointProfile!.RootElement;
        Check(pointInteractive||p.GetProperty("SoeCadenceAckHex").GetArrayLength()==256,"Bounded independent cadence acknowledgements");
        offlineStartClock=localClock;PrepareOfflineFrameSchedule();
        if(pointSessionControlled&&!pointConfigured)offlineRootSession=new(world!,motions!,manualOutboundQueue!,runTransport!,p.GetProperty("SoeSeed").GetUInt32(),maxSamples:OfflineMaximumUpdates,
            counterBase:p.GetProperty("OutboundCounterBase").GetUInt32(),timestampBase:p.GetProperty("OutboundTimestampBase").GetUInt32(),flags:p.GetProperty("OutboundFlags").GetUInt32());
        else if(!pointSessionControlled)offlineOutbox=new(world!,motions!,manualOutboundQueue!,maxSamples:OfflineMaximumUpdates,
            counterBase:p.GetProperty("OutboundCounterBase").GetUInt32(),timestampBase:p.GetProperty("OutboundTimestampBase").GetUInt32());
        offlineHud=new();offlineHud.Configure(this,()=>{
            var s=pointDriver!.State;
            return new TerrainOfflinePlaygroundHudState(s.Mode.ToString(),s.DriverResult.Speed,s.CompletedUpdates,OfflineMaximumUpdates,
                OfflineMaximumUpdates-s.CompletedUpdates,s.Target is not null,true);
        },default,pointSessionControlled);
        GD.Print("offline_playground_ready="+JsonSerializer.Serialize(new{MaximumUpdates=OfflineMaximumUpdates,MaximumTargets=OfflineMaximumClicks,
            StepMicroseconds=10000,MovementIntervalMicroseconds=100000,StationaryHeartbeatMicroseconds=1000000,
            ModernPolicy=true,Offline=true,SocketCreated=false,OriginalSenderParity=false,Caption=offlineHud.Caption}));
    }
    private void AdvanceOfflinePlaygroundSchedule()
    {
        if(!pointOfflineHosted||pointSessionControlled&&pointIndex>=600)return;
        if(pointIndex is 150 or 300 or 450 or 600 or 750 or 900){
            bool outward=pointIndex%300!=0;
            SelectAdaptedPointTarget(outward?-1.5f:2f,outward?0f:3f);
            GD.Print("offline_playground_target="+JsonSerializer.Serialize(new{AtStep=pointIndex,NativeTarget=new[]{outward?-1.5f:2f,outward?0f:3f},ActualViewportEvent=true}));
        }
        if(pointIndex==998)ClearHostedPointTarget();
    }
    private void ObserveOfflineRootMotion()
    {
        if(pointSessionControlled){ObserveOfflineSessionRoot();return;}
        var snapshot=world!.Snapshots().Single(s=>s.Object.Handle.ObjectId==humanId);
        var motion=motions!.Find(snapshot)!;
        var receipt=offlineOutbox!.Observe(snapshot,motion,checked(localClock-offlineStartClock),pointProfile!.RootElement.GetProperty("OutboundFlags").GetUInt32());
        GD.Print("offline_root_cadence="+JsonSerializer.Serialize(new{Index=pointIndex,Status=receipt.Status.ToString(),receipt.ElapsedMicroseconds,
            receipt.Samples,receipt.MovementCounter,receipt.Timestamp,QueueCount=manualOutboundQueue!.Count,ActualIssuedLocalRow=motions.IsCurrent(motion),Sent=false}));
        if(receipt.Status==SceneWorldRootMotionCadenceStatus.Queued){manualOutboundSteps.Enqueue(pointIndex);if(pointElapsedHosted)DrainOfflineScheduledRoot();else DrainRunTransport();}
        Check(receipt.Status!=SceneWorldRootMotionCadenceStatus.DeclinedBackpressure&&manualOutboundQueue.Count==0,"Offline caller drains scheduled frames without backlog");
    }
    private void FrameOfflinePlayground()
    {
        if(!pointOfflineHosted)return;
        Check(offlineHud!.Refresh(),"Actual offline HUD refresh");
        GD.Print("offline_playground_hud="+JsonSerializer.Serialize(new{Index=pointIndex,offlineHud.RefreshCount,offlineHud.Caption,
            MaximumUpdates=OfflineMaximumUpdates,RemainingUpdates=OfflineMaximumUpdates-pointDriver!.State.CompletedUpdates,
            ActualAcceptedDriverState=true,Offline=true}));
    }
    private void CloseOfflinePlayground()
    {
        CloseOfflineFrameSchedule();
        var hud=offlineHud;offlineHud=null;
        if(hud is not null){hud.Dispose();hud.Dispose();Check(hud.IsRetired&&hud.RetirementCount==1,"Offline HUD retires owned UI once");
            GD.Print("offline_playground_hud_cleanup="+JsonSerializer.Serialize(new{Retired=true,hud.RetirementCount,hud.RefreshCount,BorrowedOwnerPreserved=GodotObject.IsInstanceValid(this)}));}
        var outbox=offlineOutbox;offlineOutbox=null;
        if(outbox is not null){var state=world!.State;int count=manualOutboundQueue!.Count;outbox.Dispose();outbox.Dispose();
            Check(world.State==state&&manualOutboundQueue.Count==count,"Cadence owner preserves borrowed world and queue");
            GD.Print("offline_root_cadence_cleanup="+JsonSerializer.Serialize(new{Disposed=true,outbox.Samples,outbox.MovementCounter,
                outbox.QueuedCount,outbox.ElapsedMicroseconds,outbox.LastSuccessfulQueueMicroseconds,outbox.IsFaulted,
                BorrowedWorldPreserved=true,BorrowedQueuePreserved=true,Sent=false}));}
    }
}
