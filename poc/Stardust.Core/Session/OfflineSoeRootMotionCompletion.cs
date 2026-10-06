using Stardust.Core.Protocol;

namespace Stardust.Core.Session;

/// <summary>Primitive offline observation only; CleanDrain is not a full-session or server receipt claim.</summary>
public sealed record OfflineSoeRootMotionCompletion(int Samples,int Queued,ulong Drained,ushort NextSequence,
    bool SessionFaulted,string TransportState,int QueueCount,int QueueBytes,int PendingMessages,int RetainedPackets,
    bool CleanDrain)
{
    public static OfflineSoeRootMotionCompletion Capture(SceneWorldOfflineRootMotionSession session,
        SoeClientTransport transport,SceneWorldClientRootMotionQueue queue)
    {
        ArgumentNullException.ThrowIfNull(session);ArgumentNullException.ThrowIfNull(transport);ArgumentNullException.ThrowIfNull(queue);
        var state=session.State;int count=queue.Count,bytes=queue.QueuedBytes;
        string transportState=transport.State.ToString();int pending=transport.PendingMessageCount,retained=transport.RetainedOutboundPackets;
        return new(state.Samples,state.Queued,state.Drained,state.NextSequence,state.IsFaulted,transportState,
            count,bytes,pending,retained,!state.IsFaulted&&count==0&&bytes==0&&pending==0&&retained==0);
    }
}
