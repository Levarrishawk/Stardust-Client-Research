using System;
using System.Text.Json;
using Godot;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

public partial class ScenePumpedWorldPreview
{
    private bool pointElapsedHosted;
    private TerrainOfflineFrameClock? offlineFrameClock;
    private SceneWorldOfflineRootMotionDrain? offlineDrain;
    private int offlineFramePlans;
    private int offlineBootstrapFrames;
    private long offlineClampedMicroseconds;
    private void ConfigureOfflinePresentationHost()
    {
        if(pointElapsedHosted)host!.Configure(pump!,Before,PlanOfflinePresentationSteps,.01,()=>!finishing&&!pointComplete);
        else host!.Configure(pump!,Before);
    }
    private void PrepareOfflineFrameSchedule()
    {
        if(!pointElapsedHosted)return;
        offlineBootstrapFrames=frames;
        offlineFrameClock=new(maximumReservedSteps:pointInteractive?OfflineMaximumUpdates:2000);
        GD.Print("offline_frame_clock_ready="+JsonSerializer.Serialize(new{StepMicroseconds=10000,MaximumStepsPerFrame=8,
            MaximumReservedSteps=pointInteractive?OfflineMaximumUpdates:2000,MaximumFrameMicroseconds=250000,Synthetic=pointProfile!.RootElement.GetProperty("SyntheticFrameClock").GetBoolean(),
            ReservedSlotsAreCompletedUpdates=false,ModernPolicy=true}));
    }
    private int PlanOfflinePresentationSteps(double seconds)
    {
        if(!pointStarted)return 1; // Bootstrap retains one actual pump slot per process.
        if(finishing||pointComplete)return 0;
        bool synthetic=pointProfile!.RootElement.GetProperty("SyntheticFrameClock").GetBoolean();
        Check(double.IsFinite(seconds)&&seconds>=0&&seconds<=60,"Bounded measured Godot frame time");
        long raw=synthetic?((offlineFramePlans%5) switch{0=>0,1=>5000,2=>15000,3=>33000,_=>250000}):checked((long)Math.Round(seconds*1000000,MidpointRounding.ToEven));
        long admitted=Math.Min(raw,250000),clamped=raw-admitted;
        offlineClampedMicroseconds=checked(offlineClampedMicroseconds+clamped);
        var receipt=offlineFrameClock!.AdvanceFrame(admitted);
        GD.Print("offline_frame_clock_state="+JsonSerializer.Serialize(new{Index=offlineFramePlans++,MeasuredSeconds=seconds,RawMicroseconds=raw,
            AdmittedMicroseconds=admitted,ClampedMicroseconds=clamped,TotalClampedMicroseconds=offlineClampedMicroseconds,Receipt=receipt,
            CompletedDriverUpdates=pointDriver!.State.CompletedUpdates,Synthetic=synthetic,ModernPolicy=true}));
        Check(!receipt.Exhausted||pointComplete||pointInteractive&&receipt.Steps>0,"Reservation cap cannot silently strand an active offline session");
        return receipt.Steps;
    }
    private ulong OfflineTransportAccepted=>pointElapsedHosted?offlineDrain!.BridgeAcceptedCount:runTransportBridge!.AcceptedCount;
    private bool OfflineClosedTransportDeclined()=>pointElapsedHosted
        ?offlineDrain!.TryDrainNext(generation,localClock/1000,ReadOnlySpan<byte>.Empty) is null
        :!runTransportBridge!.TryQueueNext(generation);
    private void DrainOfflineScheduledRoot()
    {
        var p=pointProfile!.RootElement;
        byte[] ack=Convert.FromHexString(p.GetProperty("SoeCadenceAckHex")[p.GetProperty("InvalidOfflineDrainAck").GetBoolean()&&runDatagrams==0?1:runDatagrams].GetString()!);
        try
        {
            var receipt=offlineDrain!.TryDrainNext(generation,localClock/1000,ack)??throw new InvalidOperationException("Eligible scheduled offline frame must drain");
            int index=manualOutboundSteps.Dequeue();byte[] application=Convert.FromHexString(receipt.ApplicationHex);
            try
            {
                var raw=ObjectControllerTransformCodec.Decode(application,ObjectControllerTransformDirection.ClientToServer);
                Check(receipt.Sequence==runDatagrams&&receipt.AcceptedCount==(ulong)runDatagrams+1,"Actual dedicated offline sequence and accepted prefix");
                GD.Print("point_outbound_root="+JsonSerializer.Serialize(new{AtStep=index,ReadAtStep=pointIndex,Hex=receipt.ApplicationHex,
                    Counter=raw.MovementCounter,Timestamp=raw.Timestamp,Native=new[]{raw.PositionX,raw.PositionY,raw.PositionZ},raw.Speed,
                    ActualIssuedLocalRowAtEnqueue=true,HistoricalOwnedFrame=true,CallerOwnedBytes=true,TransferredViaBridge=true,Sent=false,OriginalSenderParity=false}));
                GD.Print("soe_root_datagram="+JsonSerializer.Serialize(new{AtStep=index,ReadAtStep=pointIndex,receipt.Sequence,Hex=receipt.DatagramHex,receipt.AckHex,
                    ReliableCopyAccepted=true,AckReleased=1,PendingMessages=0,RetainedPackets=0,SocketCreated=false,Sent=false,OriginalSenderParity=false}));
                pointOutboundPackets++;manualTransferred++;runDatagrams++;
            }
            finally{Array.Clear(application);}
        }
        catch
        {
            GD.Print("offline_root_drain_failure="+JsonSerializer.Serialize(new{AtStep=pointIndex,CompletedDriverUpdates=pointDriver!.State.CompletedUpdates,
                QueueCount=manualOutboundQueue!.Count,QueueBytes=manualOutboundQueue.QueuedBytes,offlineDrain!.AcceptedCount,
                offlineDrain.BridgeAcceptedCount,offlineDrain.IsFaulted,PendingMessages=runTransport!.PendingMessageCount,
                RetainedPackets=runTransport.RetainedOutboundPackets,Sent=false,RollbackClaimed=false}));
            throw;
        }
        finally{Array.Clear(ack);}
    }
    private void CloseOfflineFrameSchedule()
    {
        var clock=offlineFrameClock;offlineFrameClock=null;
        if(clock is not null){var before=clock.State;clock.Dispose();clock.Dispose();Check(ReferenceEquals(before,clock.State),"Frame reservation history survives owner disposal");
            GD.Print("offline_frame_clock_cleanup="+JsonSerializer.Serialize(new{Disposed=true,Plans=offlineFramePlans,State=before,
                CompletedDriverUpdates=pointDriver!.State.CompletedUpdates,ReportedPointIndex=pointIndex,TotalClampedMicroseconds=offlineClampedMicroseconds,
                HistoricStatePreserved=true,ReservedSlotsAreCompletedUpdates=false}));}
        var drain=offlineDrain;offlineDrain=null;
        if(drain is not null){int count=manualOutboundQueue!.Count;var state=runTransport!.State;drain.Dispose();drain.Dispose();
            Check(count==manualOutboundQueue.Count&&state==runTransport.State,"Drain owner preserves actual borrowed queue and transport");
            GD.Print("offline_root_drain_cleanup="+JsonSerializer.Serialize(new{Disposed=true,drain.AcceptedCount,drain.BridgeAcceptedCount,
                drain.NextSequence,drain.LastNowMilliseconds,drain.IsFaulted,BorrowedQueueCount=count,BorrowedQueuePreserved=true,BorrowedTransportPreserved=true,Sent=false}));}
    }
}
