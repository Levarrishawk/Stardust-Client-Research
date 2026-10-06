using System.Numerics;
using Stardust.Core.Assets;

namespace Stardust.Core.Session;

public enum SceneWorldRootMotionCadenceStatus { NotDue, Queued, DeclinedBackpressure }
public sealed record SceneWorldRootMotionCadenceReceipt(SceneWorldRootMotionCadenceStatus Status,
    long ElapsedMicroseconds, int Samples, uint MovementCounter, uint Timestamp);

/// <summary>Modern scheduling only; borrows all owners and retains no encoded bytes.</summary>
public sealed class SceneWorldRootMotionCadenceOutbox : IDisposable
{
    private SceneWorldCoordinator? world;
    private SceneWorldObjectMotionStore? store;
    private SceneWorldClientRootMotionQueue? queue;
    private readonly long step, interval, heartbeat;
    private readonly int maximumSamples, maximumPackets, maximumBytes, owner = Environment.CurrentManagedThreadId;
    private readonly uint timestampBase;
    private long elapsed, lastSuccess;
    private int samples, queuedCount;
    private uint counter, lastFlags;
    private SceneObjectIncarnation? incarnation;
    private long generation;
    private AppearanceTransform lastTransform;
    private float lastSpeed;
    private bool hasQueued, wasMoving, pendingStop, disposed, faulted, busy;
    public SceneWorldRootMotionCadenceOutbox(SceneWorldCoordinator world, SceneWorldObjectMotionStore store,
        SceneWorldClientRootMotionQueue queue, long stepMicroseconds = 10000,
        long movementIntervalMicroseconds = 100000, long stationaryHeartbeatMicroseconds = 1000000,
        int maxSamples = 6000, int maxPendingPackets = 8, int maxPendingBytes = 496,
        uint counterBase = 0, uint timestampBase = 0)
    {
        ArgumentNullException.ThrowIfNull(world); ArgumentNullException.ThrowIfNull(store); ArgumentNullException.ThrowIfNull(queue);
        if (stepMicroseconds != 10000 || movementIntervalMicroseconds < stepMicroseconds ||
            movementIntervalMicroseconds % stepMicroseconds != 0 || stationaryHeartbeatMicroseconds < movementIntervalMicroseconds ||
            stationaryHeartbeatMicroseconds % stepMicroseconds != 0 || maxSamples is < 1 or > 6000 ||
            maxPendingPackets is < 1 or > 4096 || maxPendingBytes is < 62 or > 253952)
            throw new ArgumentOutOfRangeException(nameof(stepMicroseconds));
        _ = world.Snapshots(); _ = queue.Count;
        if (!store.IsBoundTo(world)) throw new ArgumentException("Exact open world/store binding required.");
        this.world=world; this.store=store; this.queue=queue; step=stepMicroseconds; interval=movementIntervalMicroseconds;
        heartbeat=stationaryHeartbeatMicroseconds; maximumSamples=maxSamples; maximumPackets=maxPendingPackets;
        maximumBytes=maxPendingBytes; counter=counterBase; this.timestampBase=timestampBase;
    }
    private void Enter()
    {
        if(Environment.CurrentManagedThreadId!=owner)throw new InvalidOperationException("Outbox owner thread required.");
        if(busy)throw new InvalidOperationException("Outbox reentrancy.");busy=true;
    }
    public bool IsFaulted { get { Enter();try{return faulted;}finally{busy=false;} } }
    public long ElapsedMicroseconds { get { Enter();try{return elapsed;}finally{busy=false;} } }
    public long LastSuccessfulQueueMicroseconds { get { Enter();try{return lastSuccess;}finally{busy=false;} } }
    public int Samples { get { Enter();try{return samples;}finally{busy=false;} } }
    public uint MovementCounter { get { Enter();try{return counter;}finally{busy=false;} } }
    public int QueuedCount { get { Enter();try{return queuedCount;}finally{busy=false;} } }
    private void Validate(SceneWorldObjectSnapshot snapshot, SceneWorldObjectMotionSnapshot motion)
    {
        ArgumentNullException.ThrowIfNull(snapshot);ArgumentNullException.ThrowIfNull(motion);
        if(!store!.IsBoundTo(world!) || world!.State is not (ZoneSceneTransitionState.Ready or ZoneSceneTransitionState.AwaitingServerReady))
            throw new InvalidDataException("Active exact world/store required.");
        var latest=world.Snapshots().SingleOrDefault(s=>s.Object.Incarnation==snapshot.Object.Incarnation);
        if(latest is not {Object.Phase:SceneObjectPhase.Finalized,Replication:not null} || latest.Object.ContainerId!=0 ||
            !ReferenceEquals(latest.Object,snapshot.Object)||!ReferenceEquals(latest.Placement,snapshot.Placement)||
            !latest.Envelopes.SequenceEqual(snapshot.Envelopes)||snapshot.Replication is null||!world.IsCurrent(snapshot.Replication)||
            latest.Replication.Revision!=snapshot.Replication.Revision||motion.Authoritative||motion.MovementCounter is not null||
            motion.Incarnation!=latest.Object.Incarnation||!ReferenceEquals(store.Find(snapshot),motion)||!store.IsCurrent(motion)||
            (incarnation is not null && (incarnation!=motion.Incarnation || generation!=world.Generation)))
            throw new InvalidDataException("Exact current typed uncontained LOCAL incarnation required.");
        static bool Finite(Vector3 v)=>float.IsFinite(v.X)&&float.IsFinite(v.Y)&&float.IsFinite(v.Z);
        if(!Finite(motion.Transform.RowX)||!Finite(motion.Transform.RowY)||!Finite(motion.Transform.RowZ)||
            !Finite(motion.Transform.Translation)||!float.IsFinite(motion.Speed)||motion.Speed<0)
            throw new InvalidDataException("Finite source required.");
    }
    public SceneWorldRootMotionCadenceReceipt Observe(SceneWorldObjectSnapshot snapshot, SceneWorldObjectMotionSnapshot motion,
        long elapsedMicroseconds, uint flags=0, CancellationToken cancellationToken=default)
    {
        Enter();try
        {
            ObjectDisposedException.ThrowIf(disposed,this);cancellationToken.ThrowIfCancellationRequested();
            if(faulted)throw new InvalidOperationException("Outbox terminally faulted.");
            if(samples==maximumSamples || elapsedMicroseconds!=checked(elapsed+step))throw new InvalidDataException("Exact consecutive clock and sample budget required.");
            Validate(snapshot,motion);
            bool moving=motion.Speed>0;
            bool stop=pendingStop||(samples>0&&wasMoving&&!moving);
            bool dirty=!hasQueued||lastTransform!=motion.Transform||lastSpeed!=motion.Speed||lastFlags!=flags;
            bool due=stop || (dirty&&elapsedMicroseconds-lastSuccess>=interval) ||
                (!moving&&hasQueued&&elapsedMicroseconds-lastSuccess>=heartbeat);
            // Count/byte preflight is observational. Supplied capacity and queue binding are caller contracts.
            bool full=due&&(queue!.Count>=maximumPackets||queue.QueuedBytes>maximumBytes-62);
            uint timestamp=unchecked(timestampBase+(uint)(elapsedMicroseconds/1000));
            elapsed=elapsedMicroseconds;samples++;wasMoving=moving;pendingStop=stop;
            incarnation??=motion.Incarnation;generation=world!.Generation;
            if(!due)return new(SceneWorldRootMotionCadenceStatus.NotDue,elapsed,samples,counter,timestamp);
            if(full)return new(SceneWorldRootMotionCadenceStatus.DeclinedBackpressure,elapsed,samples,counter,timestamp);
            uint next=unchecked(counter+1);
            try {queue!.Enqueue(snapshot,motion,flags,next,timestamp,cancellationToken);}
            catch {faulted=true;throw;}
            counter=next;queuedCount++;lastSuccess=elapsed;lastTransform=motion.Transform;lastSpeed=motion.Speed;lastFlags=flags;
            hasQueued=true;pendingStop=false;
            return new(SceneWorldRootMotionCadenceStatus.Queued,elapsed,samples,counter,timestamp);
        }finally{busy=false;}
    }
    public void Dispose()
    {Enter();try{if(disposed)return;disposed=true;world=null;store=null;queue=null;}finally{busy=false;}}
}
