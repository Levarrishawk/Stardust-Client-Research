using System;
using System.Linq;
using System.Text.Json;
using Godot;
using Stardust.Core.Protocol;
using Stardust.Core.Session;
using NativeVector3=System.Numerics.Vector3;

public partial class ScenePumpedWorldPreview
{
    private bool pointCameraFollowHosted;
    private TerrainCameraPivotFollowClock? cameraFollow;
    private TerrainCameraPivotFollowState? cameraFollowState;
    private void PrepareCameraFollow()
    {
        if(!pointCameraFollowHosted)return;
        var p=pointProfile!.RootElement;var offset=p.GetProperty("FollowNativeOffset");
        Check(offset.GetArrayLength()==3&&p.GetProperty("FollowResponseSeconds").GetDouble()==.1,"Explicit supplied modern follow policy");
        cameraFollow=new(pointDriver!.State.DriverResult.Transform.Translation,new NativeVector3(offset[0].GetSingle(),offset[1].GetSingle(),offset[2].GetSingle()),localClock,10000,.1,OfflineMaximumUpdates,4,budget:offlineSessionBudget);
        cameraFollowState=cameraFollow.State;PresentCameraFollow();
        GD.Print("camera_follow_ready="+JsonSerializer.Serialize(new{NativePivot=new[]{cameraFollowState.Pivot.X,cameraFollowState.Pivot.Y,cameraFollowState.Pivot.Z},
            NativeOffset=new[]{offset[0].GetSingle(),offset[1].GetSingle(),offset[2].GetSingle()},cameraFollowState.TimestampMicroseconds,
            StepMicroseconds=10000,ResponseSeconds=.1,ActualDriverPosition=true,ModernFollowPolicy=true,OriginalCameraParity=false}));
    }
    private void PresentCameraFollow()
    {
        var p=cameraFollowState!.Pivot;var h=cameraOrbitHost!;
        double yaw=h.Yaw,pitch=h.Pitch,distance=h.Distance;int accepted=h.AcceptedCount,handled=h.HandledCount;
        h.SetPivot(new Vector3(p.X,p.Y,-p.Z));
        Check(h.Pivot==new Vector3(p.X,p.Y,-p.Z)&&h.Yaw==yaw&&h.Pitch==pitch&&h.Distance==distance&&h.AcceptedCount==accepted&&h.HandledCount==handled,
            "Native pivot reflected once; follow preserves orbit/input state");
    }
    private void RebaseCameraFollow(SceneWorldObjectMotionSnapshot authoritative)
    {
        if(!pointCameraFollowHosted)return;
        var before=cameraFollow!.State;cameraFollowState=cameraFollow.Rebase(authoritative.Transform.Translation,localClock);PresentCameraFollow();
        Check(cameraFollowState.Updates==before.Updates&&cameraFollowState.TimestampMicroseconds==before.TimestampMicroseconds,"Correction snaps camera without elapsed/refund");
        GD.Print("camera_follow_rebase="+JsonSerializer.Serialize(new{AtStep=pointIndex,NativePivot=new[]{cameraFollowState.Pivot.X,cameraFollowState.Pivot.Y,cameraFollowState.Pivot.Z},
            cameraFollowState.Updates,cameraFollowState.Rebases,cameraFollowState.TimestampMicroseconds,ActualAuthoritativeRow=true,ClockPreserved=true,UpdateBudgetPreserved=true}));
    }
    private void AdvanceCameraFollow()
    {
        if(!pointCameraFollowHosted)return;
        cameraFollowState=cameraFollow!.Advance(pointDriver!.State.DriverResult.Transform.Translation,localClock);PresentCameraFollow();
    }
    private void FrameCameraFollow()
    {
        if(!pointCameraFollowHosted)return;
        var s=cameraFollowState!;var h=cameraOrbitHost!;var c=orbitCamera!;var p=c.GlobalPosition;var f=-c.GlobalBasis.Z;
        Check(s.Updates==pointDriver!.State.CompletedUpdates&&s.TimestampMicroseconds==localClock&&ReferenceEquals(s,cameraFollow!.State),"Exact accepted follow publication");
        GD.Print("camera_follow_state="+JsonSerializer.Serialize(new{Index=pointIndex,s.Updates,s.Rebases,s.TimestampMicroseconds,
            NativePivot=new[]{s.Pivot.X,s.Pivot.Y,s.Pivot.Z},NativeTarget=new[]{s.Target.X,s.Target.Y,s.Target.Z},EnginePivot=new[]{h.Pivot.X,h.Pivot.Y,h.Pivot.Z},
            EnginePosition=new[]{p.X,p.Y,p.Z},EngineForward=new[]{f.X,f.Y,f.Z},h.Yaw,h.Pitch,h.Distance,h.AcceptedCount,h.HandledCount,
            ActualDriverPosition=true,ActualBorrowedCamera=true,EngineZReflectedOnce=true,ModernFollowPolicy=true,OriginalCameraParity=false}));
    }
    private void CloseCameraFollow()
    {
        var owned=cameraFollow;cameraFollow=null;if(owned is null)return;
        var before=owned.State;var h=cameraOrbitHost!;var camera=orbitCamera!;var pose=camera.GlobalTransform;var pivot=h.Pivot;
        owned.Dispose();owned.Dispose();
        Check(ReferenceEquals(owned.State,before)&&GodotObject.IsInstanceValid(camera)&&camera.GlobalTransform==pose&&h.Pivot==pivot,"Follow disposal preserves history and borrowed camera/orbit");
        GD.Print("camera_follow_cleanup="+JsonSerializer.Serialize(new{Disposed=true,before.Updates,before.Rebases,HistoricStatePreserved=true,BorrowedCameraPreserved=true,BorrowedOrbitPreserved=true}));
    }
    private void AcceptFinalOuterPeerDisconnect()
    {
        byte[] datagram=Convert.FromHexString(pointProfile!.RootElement.GetProperty("SoeOuterDisconnectHex").GetString()!);
        var worldState=world!.State;int queued=pump!.QueuedMessageCount;int count=manualOutboundQueue!.Count;ulong accepted=OfflineTransportAccepted;
        SceneWorldSoeApplicationIngressReceipt? receipt=null;
        try
        {
            try
            {
                receipt=soeApplicationIngress!.Receive(generation,datagram);
                string[] controls=receipt.ControlDatagrams.Select(Convert.ToHexString).ToArray();
                Check(receipt.Accepted&&receipt.PeerDisconnected&&receipt.ReturnedApplications==1&&receipt.QueuedApplications==1&&receipt.QueuedBytes==62
                    &&controls.Length==1&&controls[0]==pointProfile.RootElement.GetProperty("SoeOuterAckHex").GetString()
                    &&runTransport!.State==SoeClientTransportState.Closed,"Outer prefix copied and ACK transferred before matching disconnect; suffix ignored");
                Check(world.State==worldState&&pump.QueuedMessageCount==queued+1&&pump.QueuedMessageBytes==62&&manualOutboundQueue.Count==count
                    &&OfflineClosedTransportDeclined()&&OfflineTransportAccepted==accepted,"Closed transport declines before take; caller explicitly drops queued prefix on close");
                foreach(var control in receipt.ControlDatagrams)Array.Clear(control);soeIngressControls+=controls.Length;
                GD.Print("soe_outer_peer_disconnect="+JsonSerializer.Serialize(new{Hex=Convert.ToHexString(datagram),AtStep=pointIndex,PeerDisconnected=true,TransportClosed=true,
                    receipt.ReturnedApplications,receipt.QueuedApplications,receipt.QueuedBytes,ControlHex=controls,CallerControlsCleared=true,CallerPolicy="DropQueuedPrefixOnSceneClose",
                    BorrowedWorldPreserved=true,BorrowedQueuePreserved=true,BridgeDeclinedBeforeTake=true,PendingMessages=runTransport!.PendingMessageCount,
                    RetainedPackets=runTransport!.RetainedOutboundPackets,CompletedUpdates=pointDriver!.State.CompletedUpdates,SuffixDelivered=false,Sent=false,OriginalCloseReasonSemantics=false}));
            }
            catch
            {
                GD.Print("soe_outer_peer_disconnect_failure="+JsonSerializer.Serialize(new{Hex=Convert.ToHexString(datagram),AtStep=pointIndex,
                    TransportEstablished=runTransport!.State==SoeClientTransportState.Established,IngressFaulted=soeApplicationIngress!.IsFaulted,
                    BorrowedWorldPreserved=world.State==worldState,BorrowedPumpPreserved=pump.QueuedMessageCount==queued,
                    BorrowedQueuePreserved=manualOutboundQueue.Count==count,CompletedUpdates=pointDriver!.State.CompletedUpdates,
                    ReliabilityPrefixConsumed=true,UntransferredControlsZeroingSourceInspected=true,RollbackClaimed=false,Sent=false}));
                throw;
            }
        }
        finally{Array.Clear(datagram);if(receipt is not null)foreach(var control in receipt.ControlDatagrams)Array.Clear(control);}
    }
}
