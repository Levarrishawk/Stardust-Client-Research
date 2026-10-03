using Stardust.Core.Protocol;
namespace Stardust.Core.Session;

public sealed record SceneTangibleReplicationLimits(int Capacity=64,long MaximumDecodedBytes=16777216,
    int MaximumRawComponentKeys=64,int MaximumComponentsElements=64,int MaximumStringBytes=65536,
    int MaximumUnicodeUnits=65536,int MaximumUpdates=4096,int MaximumComponentCommands=4096);
public sealed class SceneTangibleReplicationSnapshot
{
    internal SceneTangibleReplicationSnapshot(object owner,SceneObjectIncarnation incarnation,ulong revision,
        TangibleView3BaselineSnapshot baseline,CreatureView3NameState names,CreatureView3TangibleScalarState scalars,
        ReplicationByteString customization,CreatureView3ComponentsState components)
    {Owner=owner;Incarnation=incarnation;Revision=revision;Baseline=baseline;Names=names;TangibleScalars=scalars;Customization=customization;Components=components;}
    internal object Owner {get;}
    public SceneObjectIncarnation Incarnation {get;}
    public ulong Revision {get;}
    public TangibleView3BaselineSnapshot Baseline {get;}
    public CreatureView3NameState Names {get;}
    public CreatureView3TangibleScalarState TangibleScalars {get;}
    public ReplicationByteString Customization {get;}
    public CreatureView3ComponentsState Components {get;}
}
/// <summary>Standalone owner-thread tangible model store; no world/router/engine or wire decoding.</summary>
public sealed class SceneTangibleReplicationStore:IDisposable
{
    private readonly SceneObjectRegistry registry;
    private readonly SceneTangibleReplicationLimits limits;
    private readonly int ownerThread=Environment.CurrentManagedThreadId;
    private readonly object identity=new();
    private SortedDictionary<ulong,SceneTangibleReplicationSnapshot> entries=new();
    private long retained;
    private ulong revision;
    private bool busy,closed;
    public SceneTangibleReplicationStore(SceneObjectRegistry registry,SceneTangibleReplicationLimits? limits=null,ulong revisionSeed=0)
    {
        ArgumentNullException.ThrowIfNull(registry);var value=limits??new();
        if(value.Capacity is <1 or >4096 || value.MaximumDecodedBytes is <47 or >134217728 || value.MaximumRawComponentKeys is <0 or >262144 ||
            value.MaximumComponentsElements is <0 or >65536 || value.MaximumStringBytes is <0 or >1048576 || value.MaximumUnicodeUnits is <0 or >524288 ||
            value.MaximumUpdates is <1 or >4096 || value.MaximumComponentCommands is <0 or >65536)throw new ArgumentOutOfRangeException(nameof(limits));
        this.registry=registry;this.limits=value with {};revision=revisionSeed;
    }
    private void Enter(){if(Environment.CurrentManagedThreadId!=ownerThread)throw new InvalidOperationException("Tangible store owner thread required.");if(busy)throw new InvalidOperationException("Tangible store reentrancy.");busy=true;}
    private void Open()=>ObjectDisposedException.ThrowIf(closed,this);
    private static long Charge(TangibleView3BaselineSnapshot baseline,CreatureView3NameState names,ReplicationByteString raw,int count)=>
        checked(baseline.DecodedBytes+16+4L*count+names.AdditionalDecodedBytes(baseline)+(ReferenceEquals(raw,baseline.Customization)?0:raw.Length));
    private static long Charge(SceneTangibleReplicationSnapshot value)=>Charge(value.Baseline,value.Names,value.Customization,value.Components.Keys.Count);
    private void RetireStale()
    {
        foreach(var id in entries.Where(p=>!registry.IsCurrent(p.Value.Incarnation)).Select(p=>p.Key).ToArray())
        {retained=checked(retained-Charge(entries[id]));entries.Remove(id);}
    }
    public long RetainedDecodedBytes{get{Enter();try{RetireStale();return retained;}finally{busy=false;}}}
    public ulong Revision{get{Enter();try{return revision;}finally{busy=false;}}}
    internal sealed class Prepared
    {
        internal Prepared(SceneTangibleReplicationStore owner,SortedDictionary<ulong,SceneTangibleReplicationSnapshot> entries,long bytes,ulong revision,SceneTangibleReplicationSnapshot snapshot)
        {Owner=owner;Entries=entries;Bytes=bytes;Revision=revision;Snapshot=snapshot;}
        internal SceneTangibleReplicationStore Owner {get;}
        internal SortedDictionary<ulong,SceneTangibleReplicationSnapshot> Entries {get;}
        internal long Bytes {get;}
        internal ulong Revision {get;}
        internal SceneTangibleReplicationSnapshot Snapshot {get;}
    }
    // Private owner-held transaction: no external callbacks may run between preparation and release.
    internal void CommitPrepared(Prepared token){entries=token.Entries;retained=token.Bytes;revision=token.Revision;}
    internal void ReleasePrepared(Prepared token){busy=false;}
    internal Prepared? PrepareSeed(SceneObjectIncarnation incarnation,TangibleView3BaselineSnapshot baseline)
    {
        Enter();try
        {
            Open();if(!registry.IsCurrent(incarnation)){busy=false;return null;}ArgumentNullException.ThrowIfNull(baseline);
            if(baseline.ObjectId!=incarnation.ObjectId)throw new InvalidDataException("Tangible seed target mismatch.");
            if(baseline.Components.Count>limits.MaximumRawComponentKeys || baseline.NameFile.Length>limits.MaximumStringBytes || baseline.NameKey.Length>limits.MaximumStringBytes ||
                baseline.Customization.Length>limits.MaximumStringBytes || baseline.CustomNameUnits.Count>limits.MaximumUnicodeUnits || baseline.DecodedBytes<47)
                throw new InvalidDataException("Tangible baseline raw field capacity.");
            entries.TryGetValue(incarnation.ObjectId,out var old);if(old is null && entries.Count>=limits.Capacity)throw new InvalidDataException("Tangible actor capacity.");
            var names=CreatureView3NameState.FromBaseline(baseline);var scalars=CreatureView3TangibleScalarState.FromBaseline(baseline);
            var components=new CreatureView3ComponentsState(baseline.ObjectId,baseline.Components.Counter,baseline.Components.Values.ToArray(),limits.MaximumComponentsElements);
            long other=checked(retained-(old is null?0:Charge(old)));long bytes=Charge(baseline,names,baseline.Customization,components.Keys.Count);
            if(bytes>limits.MaximumDecodedBytes-other)throw new InvalidDataException("Tangible retained decoded capacity.");
            ulong next=checked(revision+1);var candidate=new SceneTangibleReplicationSnapshot(identity,incarnation,next,baseline,names,scalars,baseline.Customization,components);
            var staged=new SortedDictionary<ulong,SceneTangibleReplicationSnapshot>(entries){[incarnation.ObjectId]=candidate};
            long total=checked(other+bytes);return new(this,staged,total,next,candidate);
        }catch{busy=false;throw;}
    }
    public SceneTangibleReplicationSnapshot? Seed(SceneObjectIncarnation incarnation,TangibleView3BaselineSnapshot baseline)
    {
        var token=PrepareSeed(incarnation,baseline);if(token is null)return null;
        try{CommitPrepared(token);return token.Snapshot;}finally{ReleasePrepared(token);}
    }
    private bool Current(SceneTangibleReplicationSnapshot value)=>!closed&&ReferenceEquals(value.Owner,identity)&&registry.IsCurrent(value.Incarnation)&&
        entries.TryGetValue(value.Incarnation.ObjectId,out var latest)&&ReferenceEquals(value,latest);
    public SceneTangibleReplicationSnapshot? Apply(SceneTangibleReplicationSnapshot snapshot,TangibleView3DeltaPlan plan)
    {
        Enter();try{Open();ArgumentNullException.ThrowIfNull(snapshot);if(!Current(snapshot))return null;var prepared=PrepareCore(snapshot,plan);CommitPrepared(prepared);return prepared.Snapshot;}finally{busy=false;}
    }
    public SceneTangibleReplicationSnapshot? Apply(SceneObjectIncarnation incarnation,TangibleView3DeltaPlan plan)
    {
        Enter();try
        {
            Open();if(!registry.IsCurrent(incarnation))return null;
            if(!entries.TryGetValue(incarnation.ObjectId,out var current)||current.Incarnation!=incarnation)throw new InvalidOperationException("Complete tangible baseline required.");
            var prepared=PrepareCore(current,plan);CommitPrepared(prepared);return prepared.Snapshot;
        }finally{busy=false;}
    }
    internal Prepared PrepareApply(SceneObjectIncarnation incarnation,TangibleView3DeltaPlan plan)
    {
        Enter();try
        {
            Open();if(!registry.IsCurrent(incarnation)||!entries.TryGetValue(incarnation.ObjectId,out var current)||current.Incarnation!=incarnation)
                throw new InvalidDataException("Current complete tangible baseline required.");
            return PrepareCore(current,plan);
        }catch{busy=false;throw;}
    }
    private Prepared PrepareCore(SceneTangibleReplicationSnapshot current,TangibleView3DeltaPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if(plan.ObjectId!=current.Incarnation.ObjectId || plan.MaximumComponentsElements!=limits.MaximumComponentsElements || plan.Updates.Count>limits.MaximumUpdates ||
            plan.Updates.Sum(u=>(long)(u.Extended?.Full?.Components?.Commands.Count??0))>limits.MaximumComponentCommands)
            throw new InvalidDataException("Tangible plan target/element/update/command capacity.");
        var names=current.Names;var scalars=current.TangibleScalars;var raw=current.Customization;var components=current.Components;
        long other=checked(retained-Charge(current));ulong next=checked(revision+1);
        foreach(var update in plan.Updates)
        {
            if(update.Name is {} name)
            {
                if(name.Index==1?name.NameFile!.Length>limits.MaximumStringBytes||name.NameKey!.Length>limits.MaximumStringBytes:name.CustomNameUnits!.Count>limits.MaximumUnicodeUnits)
                    throw new InvalidDataException("Tangible name field capacity.");
                names=names.Apply(name);
            }
            else if(update.Extended!.Tangible is {} scalar)scalars=scalars.Apply(scalar);
            else if(update.Extended.Full!.Customization is {} value)
            {if(value.Length>limits.MaximumStringBytes)throw new InvalidDataException("Tangible raw string capacity.");raw=value;}
            else if(update.Extended.Full.Components is {} component)
                components=new CreatureView3ComponentsDeltaPlan(plan.ObjectId,plan.DeclaredCount,new[]{component},limits.MaximumComponentsElements).Apply(components,count=>
                {if(Charge(current.Baseline,names,raw,count)>limits.MaximumDecodedBytes-other)throw new InvalidDataException("Intermediate tangible component decoded capacity.");});
            else throw new InvalidDataException("Unsupported tangible update.");
            if(Charge(current.Baseline,names,raw,components.Keys.Count)>limits.MaximumDecodedBytes-other)throw new InvalidDataException("Intermediate tangible decoded capacity.");
        }
        long bytes=Charge(current.Baseline,names,raw,components.Keys.Count);
        var candidate=new SceneTangibleReplicationSnapshot(identity,current.Incarnation,next,current.Baseline,names,scalars,raw,components);
        var staged=new SortedDictionary<ulong,SceneTangibleReplicationSnapshot>(entries){[plan.ObjectId]=candidate};long total=checked(other+bytes);
        return new(this,staged,total,next,candidate);
    }
    public SceneTangibleReplicationSnapshot? Find(SceneObjectIncarnation incarnation)
    {Enter();try{RetireStale();return !closed&&registry.IsCurrent(incarnation)&&entries.TryGetValue(incarnation.ObjectId,out var value)&&value.Incarnation==incarnation?value:null;}finally{busy=false;}}
    public bool IsCurrent(SceneTangibleReplicationSnapshot snapshot)
    {Enter();try{ArgumentNullException.ThrowIfNull(snapshot);RetireStale();return Current(snapshot);}finally{busy=false;}}
    public IReadOnlyList<SceneTangibleReplicationSnapshot> Snapshots()
    {Enter();try{RetireStale();return Array.AsReadOnly(entries.Values.ToArray());}finally{busy=false;}}
    public void Close(){Enter();try{closed=true;entries.Clear();retained=0;}finally{busy=false;}}
    public void Dispose()=>Close();
}
