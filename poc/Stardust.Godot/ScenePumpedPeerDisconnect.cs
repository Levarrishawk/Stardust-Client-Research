using System;
using System.Text.Json;
using Godot;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

public partial class ScenePumpedWorldPreview
{
    private void AcceptFinalPeerDisconnect()
    {
        if(!pointAcceptedLocomotionHosted)return;
        if(pointCameraFollowHosted){AcceptFinalOuterPeerDisconnect();return;}
        byte[] datagram=Convert.FromHexString(pointProfile!.RootElement.GetProperty("SoeDisconnectHex").GetString()!);
        var worldState=world!.State;int queued=pump!.QueuedMessageCount;int count=manualOutboundQueue!.Count;ulong accepted=runTransportBridge!.AcceptedCount;
        try {
            try {
                var receipt=soeApplicationIngress!.Receive(generation,datagram);
                Check(receipt.Accepted&&receipt.PeerDisconnected&&receipt.ReturnedApplications==0&&receipt.QueuedApplications==0
                    &&receipt.ControlDatagrams.Count==0&&runTransport!.State==SoeClientTransportState.Closed,"Matching peer disconnect closes actual transport without application");
                Check(world.State==worldState&&pump.QueuedMessageCount==queued&&manualOutboundQueue.Count==count
                    &&!runTransportBridge.TryQueueNext(generation)&&runTransportBridge.AcceptedCount==accepted,"Peer disconnect preserves borrowed scene and declines outbound before take");
                GD.Print("soe_peer_disconnect="+JsonSerializer.Serialize(new{Hex=Convert.ToHexString(datagram),AtStep=pointIndex,
                    PeerDisconnected=true,TransportClosed=true,QueuedApplications=0,Controls=0,BorrowedWorldPreserved=true,
                    BorrowedPumpPreserved=true,BorrowedQueuePreserved=true,BridgeDeclinedBeforeTake=true,
                    PendingMessages=runTransport!.PendingMessageCount,RetainedPackets=runTransport.RetainedOutboundPackets,
                    CompletedUpdates=pointDriver!.State.CompletedUpdates,Sent=false,OriginalCloseReasonSemantics=false}));
            } catch {
                GD.Print("soe_peer_disconnect_failure="+JsonSerializer.Serialize(new{Hex=Convert.ToHexString(datagram),AtStep=pointIndex,
                    TransportEstablished=runTransport!.State==SoeClientTransportState.Established,IngressFaulted=soeApplicationIngress!.IsFaulted,
                    BorrowedWorldPreserved=world.State==worldState,BorrowedPumpPreserved=pump.QueuedMessageCount==queued,
                    BorrowedQueuePreserved=manualOutboundQueue.Count==count,CompletedUpdates=pointDriver!.State.CompletedUpdates,Sent=false}));
                throw;
            }
        }finally{Array.Clear(datagram);}
    }
}
