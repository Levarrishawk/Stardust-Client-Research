using System;
using System.Buffers.Binary;
using System.Text.Json;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

public partial class ScenePumpedWorldPreview
{
    private SoeClientTransport? runTransport;
    private SceneWorldRootMotionTransportBridge? runTransportBridge;
    private int runDatagrams;
    private OfflineSoeRootMotionSessionOwner? configuredRootOwner;
    private void PrepareRunTransport()
    {
        var p=pointProfile!.RootElement;
        if(pointConfigured){
            configuredRootOwner=new(world!,motions!,manualOutboundQueue!,p.GetProperty("SoeConnectionId").GetUInt32(),p.GetProperty("SoeSeed").GetUInt32(),
                maxSamples:OfflineMaximumUpdates,counterBase:p.GetProperty("OutboundCounterBase").GetUInt32(),timestampBase:p.GetProperty("OutboundTimestampBase").GetUInt32(),flags:p.GetProperty("OutboundFlags").GetUInt32());
            runTransport=configuredRootOwner.Transport;offlineRootSession=configuredRootOwner.Session;
            Godot.GD.Print("soe_root_transport_ready="+JsonSerializer.Serialize(new{configuredRootOwner.RequestHex,configuredRootOwner.ResponseHex,Established=runTransport.State==SoeClientTransportState.Established,Synthetic=true,SocketCreated=false,Sent=false,OriginalSenderParity=false}));return;
        }
        runTransport=new();
        byte[] request=runTransport.BeginSession(p.GetProperty("SoeConnectionId").GetUInt32(),p.GetProperty("SoeMaximumDatagram").GetUInt32());
        byte[] response=Convert.FromHexString(p.GetProperty("SoeResponseHex").GetString()!);
        try {
            var decoded=SoeSessionCodec.DecodeResponse(response,p.GetProperty("SoeConnectionId").GetUInt32());
            Check(Convert.ToHexString(request)==p.GetProperty("SoeRequestHex").GetString()&&decoded.Seed==p.GetProperty("SoeSeed").GetUInt32()
                &&decoded.CrcWidth==2&&decoded.FirstTransformSelector==1&&decoded.SecondTransformSelector==4&&decoded.MaximumDatagramSize==496,"Independent synthetic handshake bytes and actual response parse");
            runTransport.AcceptSessionResponse(response);
            // The consolidated session owner is constructed after the initial point target.
            if(!pointSessionControlled){
                if(pointElapsedHosted)offlineDrain=new(world!,manualOutboundQueue!,runTransport,p.GetProperty("SoeSeed").GetUInt32());
                else runTransportBridge=new(manualOutboundQueue!,runTransport);
            }
            Godot.GD.Print("soe_root_transport_ready="+JsonSerializer.Serialize(new{RequestHex=Convert.ToHexString(request),ResponseHex=Convert.ToHexString(response),
                Established=runTransport.State==SoeClientTransportState.Established,Synthetic=true,SocketCreated=false,Sent=false,OriginalSenderParity=false}));
        }finally{Array.Clear(request);Array.Clear(response);}
    }
    private void DrainRunTransport()
    {
        var profile=pointProfile!.RootElement;uint seed=profile.GetProperty("SoeSeed").GetUInt32();
        while(manualOutboundQueue!.Count>0){
            Check(runTransportBridge!.TryQueueNext(generation),"Actual root ownership transfer to empty SOE pending channel");
            int remaining=manualOutboundQueue.Count;int bytes=manualOutboundQueue.QueuedBytes;ulong accepted=runTransportBridge.AcceptedCount;
            Check(runTransport!.PendingMessageCount==1&&runTransportBridge.OwnedPendingBytes==0&&!runTransportBridge.TryQueueNext(generation)
                &&manualOutboundQueue.Count==remaining&&manualOutboundQueue.QueuedBytes==bytes&&runTransportBridge.AcceptedCount==accepted,"Busy transport declines before another FIFO take");
            Godot.GD.Print("soe_root_backpressure="+JsonSerializer.Serialize(new{ReadAtStep=pointIndex,PendingMessages=1,RemainingQueue=remaining,QueuePreserved=true,OwnedBridgeBytes=0,Sent=false}));
            var datagrams=runTransport.PumpQueuedDatagrams(localClock/1000);Check(datagrams.Count==1&&runTransport.PendingMessageCount==0,"Actual socket-independent reliable datagram pump");
            byte[] datagram=datagrams[0];byte[] packet=SoeDatagramCodec.Decode(datagram,seed,2,496);
            byte[] root=packet.AsSpan(4).ToArray();byte[] ack=Convert.FromHexString(profile.GetProperty(pointOfflineHosted?"SoeCadenceAckHex":"SoeAckHex")[runDatagrams].GetString()!);
            try {
                int index=manualOutboundSteps.Dequeue();ushort sequence=BinaryPrimitives.ReadUInt16BigEndian(packet.AsSpan(2));
                Check(packet.Length==66&&packet[0]==0&&packet[1]==9&&sequence==runDatagrams,"Actual channel-zero reliable root envelope");
                var raw=ObjectControllerTransformCodec.Decode(root,ObjectControllerTransformDirection.ClientToServer);
                Godot.GD.Print("point_outbound_root="+JsonSerializer.Serialize(new{AtStep=index,ReadAtStep=pointIndex,Hex=Convert.ToHexString(root),Counter=raw.MovementCounter,
                    Timestamp=raw.Timestamp,Native=new[]{raw.PositionX,raw.PositionY,raw.PositionZ},raw.Speed,ActualIssuedLocalRowAtEnqueue=true,HistoricalOwnedFrame=true,
                    CallerOwnedBytes=true,TransferredViaBridge=true,Sent=false,OriginalSenderParity=false}));
                var received=runTransport.AcceptDatagram(ack);
                Check(!received.RejectedFraming&&received.ApplicationPayloads.Count==0&&received.ReleasedOutboundPackets==1
                    &&runTransport.RetainedOutboundPackets==0&&runTransport.PendingMessageCount==0,"Independent synthetic ACK releases actual transport copy");
                Godot.GD.Print("soe_root_datagram="+JsonSerializer.Serialize(new{AtStep=index,ReadAtStep=pointIndex,Sequence=sequence,Hex=Convert.ToHexString(datagram),AckHex=Convert.ToHexString(ack),
                    ReliableCopyAccepted=true,AckReleased=1,PendingMessages=0,RetainedPackets=0,SocketCreated=false,Sent=false,OriginalSenderParity=false}));
                pointOutboundPackets++;manualTransferred++;runDatagrams++;
            }finally{Array.Clear(root);Array.Clear(packet);Array.Clear(datagram);Array.Clear(ack);}
        }
        Check(manualOutboundSteps.Count==0&&manualOutboundQueue.Count==0&&manualOutboundQueue.QueuedBytes==0&&runTransportBridge!.OwnedPendingBytes==0
            &&runTransport!.PendingMessageCount==0&&runTransport.RetainedOutboundPackets==0,"Offline root/FIFO/SOE owners drained per batch");
    }
    private void CloseRunTransport()
    {
        if(runTransportBridge is not null){var bridge=runTransportBridge;runTransportBridge=null;int count=manualOutboundQueue!.Count;
            int own=bridge.OwnedPendingBytes;ulong accepted=bridge.AcceptedCount;var priorState=runTransport!.State;bridge.Dispose();bridge.Dispose();
            Check(bridge.OwnedPendingBytes==0&&manualOutboundQueue.Count==count&&runTransport!.State==priorState,"Bridge disposal preserves borrowed queue and transport");
            Godot.GD.Print("soe_root_bridge_cleanup="+JsonSerializer.Serialize(new{Disposed=true,OwnedPendingBefore=own,OwnedPendingAfter=0,Accepted=accepted,
                BorrowedQueueCount=count,BorrowedQueuePreserved=true,BorrowedTransportPreserved=true,Sent=false}));
        }
        if(configuredRootOwner is not null){var owner=configuredRootOwner;configuredRootOwner=null;var session=owner.Session;var transport=owner.Transport;
            var before=session.State;int count=manualOutboundQueue!.Count;owner.Dispose();owner.Dispose();
            Check(owner.IsDisposed&&ReferenceEquals(before,session.State)&&manualOutboundQueue.Count==count&&transport.State==SoeClientTransportState.Closed
                &&transport.PendingMessageCount==0&&transport.RetainedOutboundPackets==0,"Configured offline root owner drains owned session/transport only");
            Godot.GD.Print("configured_root_owner_cleanup="+JsonSerializer.Serialize(new{Disposed=owner.IsDisposed,Closed=true,session.Samples,session.Drained,HistoryPreserved=true,BorrowedQueuePreserved=true,PendingMessages=0,RetainedPackets=0}));
        }
        if(runTransport is not null){var transport=runTransport;runTransport=null;transport.Close();transport.Close();
            Check(transport.State==SoeClientTransportState.Closed&&transport.PendingMessageCount==0&&transport.RetainedOutboundPackets==0,"Caller closes its synthetic transport");
            Godot.GD.Print("soe_root_transport_cleanup="+JsonSerializer.Serialize(new{Closed=true,Datagrams=runDatagrams,PendingMessages=0,RetainedPackets=0,
                OwnsSyntheticTransport=true,SocketCreated=false,Sent=false}));
        }
    }
}
