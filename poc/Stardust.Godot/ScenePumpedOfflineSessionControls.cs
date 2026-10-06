using System;
using System.Linq;
using System.Text.Json;
using Godot;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

public partial class ScenePumpedWorldPreview
{
    private bool pointSessionControlled;
    private TerrainOfflineSessionController? offlineSessionController;
    private TerrainOfflineSessionInputHost? offlineSessionInput;
    private SceneWorldOfflineRootMotionSession? offlineRootSession;
    private static object OfflineSessionDiagnosticState(TerrainOfflineSessionState state)=>new{state.CompletedUpdates,state.Paused,
        state.StopRequested,state.Stopped,state.IsFaulted,state.FaultType,Mode=state.DriverResult.Mode.ToString()};
    private void PrepareOfflineSessionControls()
    {
        if(!pointSessionControlled)return;
        offlineSessionController=pointConfigured?configuredSessionOwner!.Controller:new(pointDriver!,OfflineMaximumUpdates);
        offlineSessionInput=new(){Name="OwnedOfflineSessionInput"};AddChild(offlineSessionInput);
        offlineSessionInput.Configure(this,()=>offlineSessionController.State.Paused,paused=>{
            if(pointProfile!.RootElement.GetProperty("InvalidSessionInput").GetBoolean())throw new InvalidOperationException("Prescribed session input sink rejection");
            offlineSessionController.SetPaused(paused);
        },()=>offlineSessionController.RequestStop());
        PrepareOfflineInteractive();
        GD.Print("offline_session_controls_ready="+JsonSerializer.Serialize(new{MaximumUpdates=OfflineMaximumUpdates,
            Keys=new[]{"P","Escape"},ActualBorrowedDriver=true,ModernPolicy=true,SocketCreated=false}));
    }
    private void AdvanceOfflineSessionInputSchedule()
    {
        if(pointIndex is not (5 or 8 or 600))return;
        var key=pointIndex==600?Key.Escape:Key.P;var before=pointDriver!.State;int handled=offlineSessionInput!.HandledCount;
        using var input=new InputEventKey{Keycode=key,PhysicalKeycode=key,Pressed=true};GetViewport().PushInput(input,true);
        var diagnostic=offlineSessionInput.LastDiagnostic;
        GD.Print("offline_session_input="+JsonSerializer.Serialize(new{AtStep=pointIndex,Key=key.ToString(),
            ActualViewportEvent=diagnostic?.FromGodotEvent==true,Handled=offlineSessionInput.HandledCount==handled+1,
            DriverStateChangedByIntent=!ReferenceEquals(before,pointDriver.State),Diagnostic=diagnostic,State=OfflineSessionDiagnosticState(offlineSessionController!.State)}));
        if(offlineSessionInput.IsFaulted)throw new InvalidOperationException("Hosted offline session input failed: "+diagnostic?.FailureType);
        Check(offlineSessionInput.HandledCount==handled+1&&ReferenceEquals(before,pointDriver.State),"Actual session key consumes once and changes only owned intent");
    }
    private void ObserveOfflineSessionRoot()
    {
        var p=pointProfile!.RootElement;var snapshot=world!.Snapshots().Single(s=>s.Object.Handle.ObjectId==humanId);var motion=motions!.Find(snapshot)!;
        byte[] ack=pointInteractive?OfflineSoeAcknowledgementFactory.Create(offlineRootSession!.NextSequence,p.GetProperty("SoeSeed").GetUInt32()):Convert.FromHexString(p.GetProperty("SoeCadenceAckHex")[runDatagrams].GetString()!);
        try
        {
            var receipt=offlineRootSession!.Observe(snapshot,motion,checked(localClock-offlineStartClock),localClock/1000,ack);
            var cadence=receipt.Cadence;
            GD.Print("offline_root_cadence="+JsonSerializer.Serialize(new{Index=pointIndex,Status=cadence.Status.ToString(),cadence.ElapsedMicroseconds,
                cadence.Samples,cadence.MovementCounter,cadence.Timestamp,QueueCount=receipt.QueueCountAfterCadence,ActualIssuedLocalRow=motions.IsCurrent(motion),Sent=false}));
            Check(cadence.Status!=SceneWorldRootMotionCadenceStatus.DeclinedBackpressure&&manualOutboundQueue!.Count==0,"Consolidated offline session drains without backlog");
            if(receipt.Drain is null)return;
            var drained=receipt.Drain;byte[] application=Convert.FromHexString(drained.ApplicationHex);
            try
            {
                var raw=ObjectControllerTransformCodec.Decode(application,ObjectControllerTransformDirection.ClientToServer);
                Check(drained.Sequence==runDatagrams&&drained.AcceptedCount==(ulong)runDatagrams+1,"Actual consolidated sequence/admission prefix");
                GD.Print("point_outbound_root="+JsonSerializer.Serialize(new{AtStep=pointIndex,ReadAtStep=pointIndex,Hex=drained.ApplicationHex,
                    Counter=raw.MovementCounter,Timestamp=raw.Timestamp,Native=new[]{raw.PositionX,raw.PositionY,raw.PositionZ},raw.Speed,
                    ActualIssuedLocalRowAtEnqueue=true,HistoricalOwnedFrame=true,CallerOwnedBytes=true,TransferredViaBridge=true,Sent=false,OriginalSenderParity=false}));
                GD.Print("soe_root_datagram="+JsonSerializer.Serialize(new{AtStep=pointIndex,ReadAtStep=pointIndex,drained.Sequence,Hex=drained.DatagramHex,drained.AckHex,
                    ReliableCopyAccepted=true,AckReleased=1,PendingMessages=runTransport!.PendingMessageCount,RetainedPackets=runTransport.RetainedOutboundPackets,SocketCreated=false,Sent=false,OriginalSenderParity=false}));
                pointOutboundPackets++;manualTransferred++;runDatagrams++;
            }
            finally{Array.Clear(application);}
        }
        finally{Array.Clear(ack);}
    }
    private void FinishOfflineSessionControls(SceneWorldTerrainPointTargetState state)
    {
        Check(state.Mode==SceneWorldTerrainPointTargetMode.Idle&&state.Target is null&&state.DriverResult.Speed==0&&manualOutboundQueue!.Count==0,
            "Actual final zero tick precedes normal caller cleanup");
        GD.Print("offline_session_stopped="+JsonSerializer.Serialize(new{state.CompletedUpdates,Mode=state.Mode.ToString(),state.DriverResult.Speed,
            TargetCleared=state.Target is null,PeerDisconnectInjected=false,Sent=false}));
        RecordConfiguredOutcome();pointComplete=true;phase=73;ScheduleFinish();
    }
    private void CloseOfflineSessionControls()
    {
        CloseOfflineInteractive();
        var input=offlineSessionInput;offlineSessionInput=null;
        if(input is not null){input.Close();input.Close();GD.Print("offline_session_input_cleanup="+JsonSerializer.Serialize(new{Closed=input.IsClosed,input.IsFaulted,
            input.AcceptedCount,input.HandledCount,input.FinishRequested,BorrowedDriverPreserved=true}));if(GodotObject.IsInstanceValid(input))input.Dispose();}
        var controller=offlineSessionController;offlineSessionController=null;
        if(controller is not null){var before=controller.State;var driver=pointDriver!.State;controller.Dispose();controller.Dispose();
            Check(ReferenceEquals(before,controller.State)&&ReferenceEquals(driver,pointDriver.State),"Controller owner preserves borrowed driver/history");
            GD.Print("offline_session_controller_cleanup="+JsonSerializer.Serialize(new{HistoricStatePreserved=true,BorrowedDriverPreserved=true,State=OfflineSessionDiagnosticState(before)}));}
        var roots=offlineRootSession;offlineRootSession=null;
        if(roots is not null){int count=manualOutboundQueue!.Count;var transport=runTransport!.State;roots.Dispose();roots.Dispose();
            Check(count==manualOutboundQueue.Count&&transport==runTransport.State,"Root session preserves borrowed queue/transport");
            GD.Print("offline_root_session_cleanup="+JsonSerializer.Serialize(new{roots.Samples,roots.Queued,roots.Drained,roots.NextSequence,roots.IsFaulted,
                BorrowedQueuePreserved=true,BorrowedTransportPreserved=true,Sent=false}));}
    }
}
