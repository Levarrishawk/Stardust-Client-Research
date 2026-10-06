using Stardust.Core.Protocol;

namespace Stardust.Core.Session;

public sealed record SceneWorldOfflineRootMotionSessionReceipt(
    SceneWorldRootMotionCadenceReceipt Cadence, SceneWorldOfflineRootMotionDrainReceipt? Drain,
    int QueueCountAfterCadence, int QueueBytesAfterCadence);
public sealed record SceneWorldOfflineRootMotionSessionState(int Samples, int Queued, ulong Drained,
    ushort NextSequence, bool IsFaulted, long LastNowMilliseconds, long RelativeElapsedMicroseconds,
    SceneWorldOfflineRootMotionSessionReceipt? LastReceipt);

/// <summary>Bounded modern cadence then one offline drain; borrows all world/network owners.</summary>
public sealed class SceneWorldOfflineRootMotionSession : IDisposable
{
    private SceneWorldCoordinator? world;
    private SceneWorldClientRootMotionQueue? queue;
    private SceneWorldRootMotionCadenceOutbox? outbox;
    private SceneWorldOfflineRootMotionDrain? drain;
    private readonly uint flags;
    private readonly int owner=Environment.CurrentManagedThreadId;
    private bool busy,disposed,faulted;
    private long lastNow=-1;
    private SceneWorldOfflineRootMotionSessionReceipt? lastReceipt;
    private SceneWorldOfflineRootMotionSessionState state=new(0,0,0,0,false,-1,0,null);
    public SceneWorldOfflineRootMotionSession(SceneWorldCoordinator world, SceneWorldObjectMotionStore store,
        SceneWorldClientRootMotionQueue queue, SoeClientTransport transport, uint seed,
        int maxSamples=6000,uint counterBase=0,uint timestampBase=0,uint flags=0,
        int maxPendingPackets=8,int maxPendingBytes=496,int maximumDatagram=496)
    {
        ArgumentNullException.ThrowIfNull(world);ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(queue);ArgumentNullException.ThrowIfNull(transport);
        this.flags=flags;
        try
        {
            outbox=new(world,store,queue,maxSamples:maxSamples,maxPendingPackets:maxPendingPackets,
                maxPendingBytes:maxPendingBytes,counterBase:counterBase,timestampBase:timestampBase);
            drain=new(world,queue,transport,seed,maximumDatagram);
            this.world=world;this.queue=queue;
        }
        catch{drain?.Dispose();outbox?.Dispose();drain=null;outbox=null;throw;}
    }
    private void Enter()
    {
        if(Environment.CurrentManagedThreadId!=owner)throw new InvalidOperationException("Root session owner thread required.");
        if(busy)throw new InvalidOperationException("Root session reentrancy.");busy=true;
    }
    private void Capture()=>state=new(outbox!.Samples,outbox.QueuedCount,drain!.AcceptedCount,
        drain.NextSequence,faulted||outbox.IsFaulted||drain.IsFaulted,lastNow,outbox.ElapsedMicroseconds,lastReceipt);
    public SceneWorldOfflineRootMotionSessionState State{get{Enter();try{return state;}finally{busy=false;}}}
    public int Samples=>State.Samples;
    public int Queued=>State.Queued;
    public ulong Drained=>State.Drained;
    public ushort NextSequence=>State.NextSequence;
    public bool IsFaulted=>State.IsFaulted;
    public SceneWorldOfflineRootMotionSessionReceipt? LastReceipt=>State.LastReceipt;
    public SceneWorldOfflineRootMotionSessionReceipt Observe(SceneWorldObjectSnapshot snapshot,
        SceneWorldObjectMotionSnapshot issuedLocal,long relativeElapsedUs,long nowMs,
        ReadOnlySpan<byte> independentAck,CancellationToken cancellationToken=default)
    {
        Enter();try
        {
            ObjectDisposedException.ThrowIf(disposed,this);cancellationToken.ThrowIfCancellationRequested();
            if(state.IsFaulted)throw new InvalidOperationException("Root session terminally faulted.");
            if(nowMs<0||nowMs<lastNow)throw new ArgumentOutOfRangeException(nameof(nowMs));
            int before=outbox!.Samples;
            try
            {
                var cadence=outbox.Observe(snapshot,issuedLocal,relativeElapsedUs,flags,cancellationToken);
                lastNow=nowMs;
                int queueCount=queue!.Count,queueBytes=queue.QueuedBytes;
                var drained=drain!.TryDrainNext(world!.Generation,nowMs,independentAck,cancellationToken);
                var receipt=new SceneWorldOfflineRootMotionSessionReceipt(cadence,drained,queueCount,queueBytes);
                lastReceipt=receipt;Capture();return receipt;
            }
            catch
            {
                if(outbox.Samples!=before){faulted=true;lastNow=nowMs;}
                if(outbox.IsFaulted||drain!.IsFaulted)faulted=true;
                Capture();throw;
            }
        }finally{busy=false;}
    }
    public void Dispose()
    {
        Enter();try
        {
            if(disposed)return;Capture();disposed=true;
            drain!.Dispose();outbox!.Dispose();drain=null;outbox=null;world=null;queue=null;
        }finally{busy=false;}
    }
}
