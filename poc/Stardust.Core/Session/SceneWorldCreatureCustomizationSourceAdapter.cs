using Stardust.Core.Assets;
using Stardust.Core.Protocol;

namespace Stardust.Core.Session;

/// <summary>Single-consumer transactional bridge from ordered raw journals to explicit full appearance values.</summary>
public sealed class SceneWorldCreatureCustomizationSourceAdapter : ISceneWorldCreatureCustomizationSource, IDisposable
{
    private sealed record Entry(SceneObjectIncarnation Incarnation,ulong Through,byte[] Current,bool Extended,SceneWorldCreatureCustomizationSnapshot? Snapshot);
    private SceneWorldCoordinator? world;
    private ISceneWorldRawCustomizationSource? rawSource;
    private Func<SceneWorldObjectSnapshot,IReadOnlyDictionary<ShaderCustomizationKey,int>>? seed;
    private Func<PackedCustomizationSnapshot,IReadOnlyDictionary<ShaderCustomizationKey,int>,IReadOnlyDictionary<ShaderCustomizationKey,int>>? projector;
    private readonly Dictionary<ulong,Entry> cache=new();
    private readonly int owner=Environment.CurrentManagedThreadId,maximumObjects;
    private bool disposed,busy,reentered;
    public Exception? LastFailure {get;private set;}
    public int CachedObjects {get{Owner();return cache.Count;}}

    public SceneWorldCreatureCustomizationSourceAdapter(SceneWorldCoordinator world,ISceneWorldRawCustomizationSource rawSource,
        Func<SceneWorldObjectSnapshot,IReadOnlyDictionary<ShaderCustomizationKey,int>> seed,
        Func<PackedCustomizationSnapshot,IReadOnlyDictionary<ShaderCustomizationKey,int>,IReadOnlyDictionary<ShaderCustomizationKey,int>> projector,
        int maxObjects=64)
    {
        ArgumentNullException.ThrowIfNull(world);ArgumentNullException.ThrowIfNull(rawSource);ArgumentNullException.ThrowIfNull(seed);ArgumentNullException.ThrowIfNull(projector);
        if(maxObjects is <1 or >4096)throw new ArgumentOutOfRangeException(nameof(maxObjects));
        _=world.Snapshots(); // Establish serialized owner access before borrowing callbacks.
        this.world=world;this.rawSource=rawSource;this.seed=seed;this.projector=projector;maximumObjects=maxObjects;
    }
    private void Owner(){if(Environment.CurrentManagedThreadId!=owner)throw new InvalidOperationException("Customization source owner thread required.");}
    private void Enter(){Owner();if(busy){reentered=true;throw new InvalidOperationException("Customization source reentry.");}busy=true;reentered=false;}
    private IReadOnlyList<SceneWorldObjectSnapshot> Prune()
    {
        var current=world!.Snapshots();
        foreach(var pair in cache.ToArray())if(!current.Any(s=>s.Object.Handle.ObjectId==pair.Key&&s.Object.Incarnation==pair.Value.Incarnation&&s.Object.Phase==SceneObjectPhase.Finalized))cache.Remove(pair.Key);
        return current;
    }
    private bool Current(SceneWorldObjectSnapshot snapshot)=>world!.Snapshots().Any(s=>
        s.Object==snapshot.Object&&s.Object.Phase==SceneObjectPhase.Finalized&&s.Placement==snapshot.Placement&&
        (snapshot.Replication is null?s.Replication is null:world.IsCurrent(snapshot.Replication)&&s.Replication is not null&&
            ReferenceEquals(s.Replication.State,snapshot.Replication.State))&&
        s.Envelopes.Count==snapshot.Envelopes.Count&&s.Envelopes.Zip(snapshot.Envelopes).All(p=>ReferenceEquals(p.First,p.Second)));
    private void Guard(SceneWorldObjectSnapshot snapshot)
    {
        if(reentered)throw new InvalidOperationException("A callback attempted source reentry.");
        if(!Current(snapshot))throw new InvalidDataException("World snapshot changed during customization transaction.");
    }
    private static IReadOnlyDictionary<ShaderCustomizationKey,int> Own(SceneObjectIncarnation incarnation,
        IReadOnlyDictionary<ShaderCustomizationKey,int> values,IReadOnlyDictionary<ShaderCustomizationKey,int>? expected=null)
    {
        ArgumentNullException.ThrowIfNull(values);
        var copy=new SceneWorldCreatureCustomizationSnapshot(incarnation,1,values).Values;
        if(expected is not null && (copy.Count!=expected.Count || !copy.Keys.ToHashSet().SetEquals(expected.Keys)))
            throw new InvalidDataException("Projection must retain exactly the explicit full seed key set.");
        return copy;
    }
    private static bool SameRaw(ReplicationByteString a,ReplicationByteString b)=>a.UsesExtendedLength==b.UsesExtendedLength&&a.Bytes.SequenceEqual(b.Bytes);
    public SceneWorldCreatureCustomizationSnapshot? Find(SceneWorldObjectSnapshot snapshot)
    {
        Enter();try
        {
            ObjectDisposedException.ThrowIf(disposed,this);ArgumentNullException.ThrowIfNull(snapshot);Prune();LastFailure=null;
            if(!Current(snapshot))return null;
            ulong id=snapshot.Object.Handle.ObjectId;cache.TryGetValue(id,out var previous);
            if(previous is null&&cache.Count>=maximumObjects){LastFailure=new InvalidDataException("Customization source object capacity.");return null;}
            try
            {
                var batch=rawSource!.Find(snapshot);Guard(snapshot);
                if(batch is null)return previous?.Snapshot;
                if(batch.Incarnation!=snapshot.Object.Incarnation)throw new InvalidDataException("Foreign customization batch incarnation.");
                if(previous is not null && (batch.ThroughRevision<previous.Through ||
                    batch.ThroughRevision==previous.Through && (!batch.Current.Bytes.SequenceEqual(previous.Current)||batch.Current.UsesExtendedLength!=previous.Extended||batch.Updates.Count!=0)))
                    throw new InvalidDataException("Customization batch rewind or same-revision mutation/replay.");
                if(batch.Current.Length>1536)throw new InvalidDataException("Customization current byte budget.");
                if(batch.Updates.Count==0)
                {
                    if(previous is not null&&batch.ThroughRevision!=previous.Through)throw new InvalidDataException("Journal was acknowledged outside this consumer.");
                    return previous?.Snapshot; // No unseen value is reconstructed from last-only bytes.
                }
                if(batch.Updates[^1].Revision!=batch.ThroughRevision || !SameRaw(batch.Updates[^1].Value,batch.Current) ||
                    previous is not null&&batch.Updates[0].Revision<=previous.Through)
                    throw new InvalidDataException("Customization journal boundary/order.");
                IReadOnlyDictionary<ShaderCustomizationKey,int>? values=previous?.Snapshot?.Values;
                var keySet=values;bool nonempty=false;
                foreach(var update in batch.Updates)
                {
                    var packed=PackedCustomizationCodec.Decode(update.Value.Bytes);
                    if(packed.IsOuterEmpty||packed.Records.Count==0)continue;
                    if(values is null){values=Own(batch.Incarnation,seed!(snapshot));keySet=values;Guard(snapshot);}
                    values=Own(batch.Incarnation,projector!(packed,values),keySet);Guard(snapshot);nonempty=true;
                }
                var candidate=nonempty?new SceneWorldCreatureCustomizationSnapshot(batch.Incarnation,batch.ThroughRevision,values!):previous?.Snapshot;
                var entry=new Entry(batch.Incarnation,batch.ThroughRevision,batch.Current.CopyBytes(),batch.Current.UsesExtendedLength,candidate);
                Guard(snapshot);
                if(!ReferenceEquals(rawSource.Find(snapshot),batch))throw new InvalidDataException("Raw batch identity changed before acknowledgment.");
                Guard(snapshot);
                if(!rawSource.Acknowledge(batch)){Guard(snapshot);LastFailure=new InvalidDataException("Exact raw acknowledgment failed.");return previous?.Snapshot;}
                Guard(snapshot); // Acknowledge callbacks cannot change the world and still publish.
                var acknowledged=rawSource.Find(snapshot);Guard(snapshot);
                if(acknowledged is null || acknowledged.Incarnation!=batch.Incarnation || acknowledged.ThroughRevision!=batch.ThroughRevision ||
                    acknowledged.Updates.Count!=0 || !SameRaw(acknowledged.Current,batch.Current))
                    throw new InvalidDataException("Raw journal changed during acknowledgment.");
                cache[id]=entry;return candidate;
            }
            catch(Exception failure)when(!reentered)
            {
                LastFailure=failure;Prune();return Current(snapshot)?previous?.Snapshot:null;
            }
        }
        finally{busy=false;}
    }
    public void Dispose()
    {
        Enter();try{if(disposed)return;disposed=true;cache.Clear();world=null;rawSource=null;seed=null;projector=null;LastFailure=null;}finally{busy=false;}
    }
}
