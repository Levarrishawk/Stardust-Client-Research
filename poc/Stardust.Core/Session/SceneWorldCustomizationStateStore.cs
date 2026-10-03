using Stardust.Core.Protocol;

namespace Stardust.Core.Session;

/// <summary>Owner-thread raw field-4 sidecar and single-consumer ordered journal over a borrowed world.</summary>
public sealed class SceneWorldCustomizationStateStore : ISceneWorldRawCustomizationSource, IDisposable
{
    private sealed record Entry(SceneObjectIncarnation Incarnation, CreatureView3BaselineSnapshot Seed,
        ReplicationByteString Current, ulong Revision, IReadOnlyList<SceneWorldRawCustomizationUpdate> Pending,
        SceneWorldRawCustomizationBatch? Issued = null);
    private SceneWorldCoordinator? world;
    private Dictionary<ulong,Entry> entries=new();
    private readonly int ownerThread=Environment.CurrentManagedThreadId, maximumObjects, maximumPendingUpdates, maximumStringBytes;
    private readonly long maximumRetainedBytes;
    private ulong revision, mutation;
    private bool busy, closed;
    public SceneWorldCustomizationStateStore(SceneWorldCoordinator world,int maximumObjects=64,int maximumPendingUpdates=64,
        int maximumStringBytes=1536,long maximumRetainedBytes=1048576,ulong revisionSeed=0)
    {
        ArgumentNullException.ThrowIfNull(world);
        if(maximumObjects is <1 or >4096 || maximumPendingUpdates is <1 or >4096 || maximumStringBytes is <0 or >1048576 || maximumRetainedBytes is <0 or >134217728)
            throw new ArgumentOutOfRangeException(nameof(maximumObjects));
        _=world.Snapshots();this.world=world;this.maximumObjects=maximumObjects;this.maximumPendingUpdates=maximumPendingUpdates;
        this.maximumStringBytes=maximumStringBytes;this.maximumRetainedBytes=maximumRetainedBytes;revision=revisionSeed;
    }
    private void Enter(){if(Environment.CurrentManagedThreadId!=ownerThread)throw new InvalidOperationException("Customization owner thread required.");if(busy)throw new InvalidOperationException("Customization store reentrancy.");busy=true;}
    private T Read<T>(Func<T> read){Enter();try{PruneCore();return read();}finally{busy=false;}}
    public int Count=>Read(()=>entries.Count);
    public int PendingUpdateCount=>Read(()=>entries.Values.Sum(e=>e.Pending.Count));
    public long RetainedBytes=>Read(()=>Charge(entries.Values));
    public ulong Revision=>Read(()=>revision);
    private T Inspect<T>(Func<T> read){Enter();try{return read();}finally{busy=false;}}
    public bool IsClosed=>Inspect(()=>closed);
    internal ulong MutationVersion=>Inspect(()=>mutation);
    internal bool Borrows(SceneWorldCoordinator value)=>Read(()=>ReferenceEquals(world,value)&&!closed);
    private bool Active=>!closed && world!.State is ZoneSceneTransitionState.AwaitingServerReady or ZoneSceneTransitionState.Ready;
    private void PruneCore()
    {
        if(closed)return;
        var current=Active?world!.Snapshots().Where(s=>s.Object.Phase==SceneObjectPhase.Finalized && s.Replication is not null).ToDictionary(s=>s.Object.Handle.ObjectId):new Dictionary<ulong,SceneWorldObjectSnapshot>();
        var stale=entries.Where(p=>!current.TryGetValue(p.Key,out var snapshot)||snapshot.Object.Incarnation!=p.Value.Incarnation||!world!.IsCurrent(snapshot.Replication!)).Select(p=>p.Key).ToArray();
        if(stale.Length==0)return;ulong next=checked(mutation+1);foreach(ulong id in stale)entries.Remove(id);mutation=next;
    }
    private SceneWorldObjectSnapshot? Latest(SceneWorldObjectSnapshot supplied)
    {
        if(!Active || supplied.Object is null || supplied.Placement is null || supplied.Envelopes is null || supplied.Replication is null || supplied.Object.Phase!=SceneObjectPhase.Finalized || !world!.IsCurrent(supplied.Replication))return null;
        var latest=world.Snapshots().SingleOrDefault(s=>s.Object.Handle.ObjectId==supplied.Object.Handle.ObjectId);
        return latest is not null && ReferenceEquals(latest.Object,supplied.Object) && latest.Placement==supplied.Placement &&
            latest.Envelopes.SequenceEqual(supplied.Envelopes) && latest.Object.Incarnation==supplied.Replication.Incarnation?latest:null;
    }
    private long Charge(IEnumerable<Entry> values)
    {
        var seen=new HashSet<ReplicationByteString>(ReferenceEqualityComparer.Instance);long bytes=0;
        void Add(ReplicationByteString value){if(seen.Add(value))bytes=checked(bytes+value.Length);}
        foreach(var e in values){Add(e.Seed.Customization);Add(e.Current);foreach(var u in e.Pending)Add(u.Value);if(e.Issued is {} batch){Add(batch.Current);foreach(var u in batch.Updates)Add(u.Value);}}
        return bytes;
    }
    private (Entry Entry,ulong Revision) Prepare(SceneWorldObjectSnapshot latest,IReadOnlyList<ReplicationByteString> updates,Dictionary<ulong,Entry>? staged=null)
    {
        var state=staged??entries;ulong id=latest.Object.Handle.ObjectId;state.TryGetValue(id,out var old);
        var seed=latest.Replication!.Baseline;var pending=old?.Pending.ToList()??new List<SceneWorldRawCustomizationUpdate>();ulong next=revision;
        bool reseed=old is null || !ReferenceEquals(old.Seed,seed);
        ReplicationByteString current=old?.Current??seed.Customization;ulong currentRevision=old?.Revision??0;
        void Append(ReplicationByteString value){if(value.Length>maximumStringBytes)throw new InvalidDataException("Raw customization string budget.");currentRevision=checked(++next);pending.Add(new(currentRevision,value));current=value;}
        if(reseed)Append(seed.Customization);
        foreach(var value in updates)Append(value);
        var candidate=new Entry(latest.Object.Incarnation,seed,current,currentRevision,pending.AsReadOnly(),reseed||updates.Count>0?null:old!.Issued);
        if(old is null && state.Count>=maximumObjects)throw new InvalidDataException("Customization actor capacity.");
        long count=checked(state.Values.Where(e=>e.Incarnation.ObjectId!=id).Sum(e=>(long)e.Pending.Count)+pending.Count);
        if(count>maximumPendingUpdates)throw new InvalidDataException("Customization journal capacity.");
        if(Charge(state.Where(p=>p.Key!=id).Select(p=>p.Value).Append(candidate))>maximumRetainedBytes)throw new InvalidDataException("Customization retained byte capacity.");
        return(candidate,next);
    }
    internal sealed class PreparedWorldCustomization
    {
        internal PreparedWorldCustomization(SceneWorldCustomizationStateStore owner, object? staged, ulong revision, ulong mutation)
        { Owner=owner;Staged=staged;Revision=revision;Mutation=mutation; }
        internal SceneWorldCustomizationStateStore Owner {get;}
        internal object? Staged {get;}
        internal ulong Revision {get;}
        internal ulong Mutation {get;}
    }
    // Coordinator already owns and verifies the snapshot. Never call world APIs here.
    internal PreparedWorldCustomization PrepareWorldCustomization(SceneWorldCoordinator owner,SceneWorldObjectSnapshot latest,
        IReadOnlyList<ReplicationByteString> updates,IReadOnlySet<SceneObjectIncarnation> current)
    {
        Enter();try
        {
            if(closed || !ReferenceEquals(world,owner))throw new InvalidDataException("Customization source must borrow this open world.");
            if(updates.Count==0)return new(this,null,revision,mutation); // Scalar-only leaves receipts and stale housekeeping untouched.
            var staged=entries.Where(p=>current.Contains(p.Value.Incarnation)).ToDictionary(p=>p.Key,p=>p.Value);
            var prepared=Prepare(latest,updates,staged);ulong nextMutation=checked(mutation+1);
            staged[latest.Object.Handle.ObjectId]=prepared.Entry;
            return new(this,staged,prepared.Revision,nextMutation);
        }
        catch{busy=false;throw;}
    }
    internal void CommitWorldCustomization(PreparedWorldCustomization token)
    { if(token.Staged is not null){entries=(Dictionary<ulong,Entry>)token.Staged;revision=token.Revision;mutation=token.Mutation;} }
    internal void ReleaseWorldCustomization(PreparedWorldCustomization token){busy=false;}
    public SceneWorldRawCustomizationBatch? Find(SceneWorldObjectSnapshot snapshot)
    {
        Enter();try
        {
            ArgumentNullException.ThrowIfNull(snapshot);PruneCore();var latest=Latest(snapshot);if(latest is null)return null;
            ulong id=latest.Object.Handle.ObjectId;
            var prepared=Prepare(latest,Array.Empty<ReplicationByteString>());var entry=prepared.Entry;
            if(entry.Issued is {} issued && issued.ThroughRevision==entry.Revision && issued.Updates.SequenceEqual(entry.Pending))return issued;
            var batch=new SceneWorldRawCustomizationBatch(entry.Incarnation,entry.Revision,entry.Current,entry.Pending);
            ulong nextMutation=checked(mutation+1);entries[id]=entry with {Issued=batch};revision=prepared.Revision;mutation=nextMutation;return batch;
        }finally{busy=false;}
    }
    public bool Apply(SceneWorldObjectSnapshot snapshot,CreatureView3CustomizationDeltaPlan plan)
    {
        Enter();try
        {
            ArgumentNullException.ThrowIfNull(snapshot);ArgumentNullException.ThrowIfNull(plan);PruneCore();var latest=Latest(snapshot);if(latest is null)return false;
            if(plan.ObjectId!=latest.Object.Handle.ObjectId)throw new InvalidDataException("Customization plan target mismatch.");
            var prepared=Prepare(latest,plan.Updates);ulong nextMutation=checked(mutation+1);
            entries[plan.ObjectId]=prepared.Entry;revision=prepared.Revision;mutation=nextMutation;return true;
        }finally{busy=false;}
    }
    public bool Acknowledge(SceneWorldRawCustomizationBatch batch)
    {
        Enter();try
        {
            ArgumentNullException.ThrowIfNull(batch);PruneCore();
            if(closed || !entries.TryGetValue(batch.Incarnation.ObjectId,out var entry) || !ReferenceEquals(entry.Issued,batch) || entry.Incarnation!=batch.Incarnation || batch.ThroughRevision!=entry.Revision || !ReferenceEquals(batch.Current,entry.Current))return false;
            var latest=world!.Snapshots().SingleOrDefault(s=>s.Object.Incarnation==entry.Incarnation);
            if(latest?.Replication is null || !world.IsCurrent(latest.Replication) || !ReferenceEquals(latest.Replication.Baseline,entry.Seed) || batch.Updates.Count!=entry.Pending.Count || !entry.Pending.SequenceEqual(batch.Updates))return false;
            ulong nextMutation=checked(mutation+1);entries[batch.Incarnation.ObjectId]=entry with {Pending=Array.AsReadOnly(entry.Pending.Skip(batch.Updates.Count).ToArray()),Issued=null};mutation=nextMutation;return true;
        }finally{busy=false;}
    }
    public int Prune(){Enter();try{int before=entries.Count;PruneCore();return before-entries.Count;}finally{busy=false;}}
    public void Clear(){Enter();try{if(entries.Count!=0){ulong next=checked(mutation+1);entries.Clear();mutation=next;}}finally{busy=false;}}
    public void Dispose(){Enter();try{if(closed)return;entries.Clear();world=null;closed=true;}finally{busy=false;}}
}
