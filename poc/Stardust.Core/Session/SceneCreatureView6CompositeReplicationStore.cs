using Stardust.Core.Protocol;
namespace Stardust.Core.Session;
public sealed record SceneCreatureView6CompositeStoreLimits(int MaximumObjects=64,long MaximumStateBytes=1048576,
 int MaximumPendingEvents=4096,int MaximumEventsPerApply=4096,long MaximumOutputBytes=2097152,long MaximumWork=1048576);
public sealed class SceneCreatureView6CompositeSnapshot
{
 internal SceneCreatureView6CompositeSnapshot(object owner,SceneObjectHandle handle,SceneObjectIncarnation incarnation,ulong revision,CreatureView6CompositeState state)
 {Owner=owner;Handle=handle;Incarnation=incarnation;Revision=revision;State=state;}
 internal object Owner{get;} public SceneObjectHandle Handle{get;} public SceneObjectIncarnation Incarnation{get;}
 public ulong Revision{get;} public CreatureView6CompositeState State{get;}
}
public sealed record SceneCreatureView6CompositeEventRecord(SceneObjectHandle Handle,SceneObjectIncarnation Incarnation,ulong StoreRevision,CreatureView6CompositeEvent Event);
public sealed class SceneCreatureView6CompositeEventBatch
{
 internal SceneCreatureView6CompositeEventBatch(object owner,ulong revision,ulong epoch,SceneCreatureView6CompositeEventRecord[] events)
 {Owner=owner;StoreRevision=revision;Epoch=epoch;Events=Array.AsReadOnly(events);}
 internal object Owner{get;} internal ulong Epoch{get;} public ulong StoreRevision{get;}
 public IReadOnlyList<SceneCreatureView6CompositeEventRecord> Events{get;}
}
/// <summary>Owned CREO6 composite state/journal over borrowed serialized registry; no dispatch or world integration.</summary>
public sealed class SceneCreatureView6CompositeReplicationStore:IDisposable
{
 private readonly SceneObjectRegistry registry;
 private readonly SceneCreatureView6CompositeStoreLimits limits;
 private readonly CreatureView6CompositeStateLimits stateLimits;
 private readonly object identity=new(); private readonly int ownerThread=Environment.CurrentManagedThreadId;
 private SortedDictionary<ulong,SceneCreatureView6CompositeSnapshot> entries=new();
 private SceneCreatureView6CompositeEventRecord[] pending=Array.Empty<SceneCreatureView6CompositeEventRecord>();
 private Prepared? activePrepared;
 private SceneCreatureView6CompositeEventBatch? issued; private long bytes; private ulong revision,epoch; private bool busy,closed;
 public SceneCreatureView6CompositeReplicationStore(SceneObjectRegistry registry,SceneCreatureView6CompositeStoreLimits? limits=null,CreatureView6CompositeStateLimits? stateLimits=null,ulong revisionSeed=0)
 {
  ArgumentNullException.ThrowIfNull(registry);var l=limits??new();var s=stateLimits??new();
  if(l.MaximumObjects is <1 or >4096||l.MaximumStateBytes is <8 or >134217728||l.MaximumPendingEvents is <0 or >262144||l.MaximumEventsPerApply is <0 or >262144||l.MaximumOutputBytes is <8 or >268435456||l.MaximumWork is <0 or >16777216)throw new ArgumentOutOfRangeException(nameof(limits));
  var model=s.ModelLimits??new();var message=s.MessageLimits??new();CreatureView6State.Validate(model);CreatureView6CompositeCodec.Validate(message);
  this.registry=registry;this.limits=l with {};this.stateLimits=new(model with {},message with {});revision=revisionSeed;
 }
 private void Enter(){if(Environment.CurrentManagedThreadId!=ownerThread)throw new InvalidOperationException("CREO6 composite store owner thread required.");if(busy)throw new InvalidOperationException("CREO6 composite store reentry.");busy=true;}
 private void Open()=>ObjectDisposedException.ThrowIf(closed,this);
 private bool CurrentHandle(SceneObjectHandle handle)=>registry.IsCurrent(handle);
 private CreatureView6CompositeStateLimits Effective(long other)
 {
  var model=stateLimits.ModelLimits!;var message=stateLimits.MessageLimits!;
  long state=Math.Min(model.MaximumStateBytes,limits.MaximumStateBytes-other);
  long output=Math.Min(model.MaximumOutputBytes,limits.MaximumOutputBytes-other-24L*pending.Length);
  if(state<8||output<8)throw new InvalidDataException("Global CREO6 composite state/output capacity.");
  return new(model with {MaximumStateBytes=state,MaximumOutputBytes=output,MaximumEvents=Math.Min(model.MaximumEvents,Math.Min(limits.MaximumEventsPerApply,limits.MaximumPendingEvents-pending.Length)),MaximumWork=Math.Min(model.MaximumWork,limits.MaximumWork)},message with {MaximumWork=Math.Min(message.MaximumWork,limits.MaximumWork)});
 }
 private Prepared Build(SceneObjectHandle handle,SceneObjectIncarnation incarnation,CreatureView6CompositeState state,IReadOnlyList<CreatureView6CompositeEvent> events,long other,ulong next)
 {
  var snapshot=new SceneCreatureView6CompositeSnapshot(identity,handle,incarnation,next,state);
  var staged=new SortedDictionary<ulong,SceneCreatureView6CompositeSnapshot>(entries){[handle.ObjectId]=snapshot};
  var queue=new SceneCreatureView6CompositeEventRecord[checked(pending.Length+events.Count)];pending.CopyTo(queue,0);
  for(int i=0;i<events.Count;i++)queue[pending.Length+i]=new(handle,incarnation,next,events[i]);
  long total=checked(other+state.DecodedBytes);ulong nextEpoch=checked(epoch+1);
  if(!CurrentHandle(handle)||registry.Find(handle.ObjectId)?.Incarnation!=incarnation)throw new InvalidDataException("Stale CREO6 composite commit recipient.");
  // Prebuilt assignment only: state is published before events become observable after owner gate releases.
  // Candidate payloads are owned; active identity binds check/release.
  var prepared=new Prepared(this,handle,incarnation,staged,total,queue,next,nextEpoch,snapshot);activePrepared=prepared;return prepared;
 }
 internal sealed class Prepared
 {
  internal Prepared(SceneCreatureView6CompositeReplicationStore owner,SceneObjectHandle handle,SceneObjectIncarnation incarnation,SortedDictionary<ulong,SceneCreatureView6CompositeSnapshot> entries,long bytes,SceneCreatureView6CompositeEventRecord[] events,ulong revision,ulong epoch,SceneCreatureView6CompositeSnapshot snapshot)
  {Owner=owner;Handle=handle;Incarnation=incarnation;Entries=entries;Bytes=bytes;Events=events;Revision=revision;Epoch=epoch;Snapshot=snapshot;}
  internal SceneCreatureView6CompositeReplicationStore Owner{get;} internal SceneObjectHandle Handle{get;} internal SceneObjectIncarnation Incarnation{get;}
  internal SortedDictionary<ulong,SceneCreatureView6CompositeSnapshot> Entries{get;} internal long Bytes{get;} internal SceneCreatureView6CompositeEventRecord[] Events{get;}
  internal ulong Revision{get;} internal ulong Epoch{get;} internal SceneCreatureView6CompositeSnapshot Snapshot{get;} internal bool Consumed{get;set;}
 }
 internal Prepared? PrepareSeed(SceneObjectHandle handle,CreatureView6Baseline baseline)
 {
  Enter();try
  {
   Open();if(!CurrentHandle(handle)){busy=false;return null;}ArgumentNullException.ThrowIfNull(baseline);
   if(baseline.ObjectId!=handle.ObjectId)throw new InvalidDataException("CREO6 composite seed recipient.");
   var incarnation=registry.Find(handle.ObjectId)!.Incarnation;entries.TryGetValue(handle.ObjectId,out var old);
   if(old is null&&entries.Count==limits.MaximumObjects)throw new InvalidDataException("CREO6 composite object capacity.");
   long other=checked(bytes-(old?.State.DecodedBytes??0));ulong next=checked(revision+1);
   var state=CreatureView6CompositeState.FromBaseline(baseline,Effective(other),old?.Incarnation==incarnation?old.State.Revision:0);
   return Build(handle,incarnation,state,Array.Empty<CreatureView6CompositeEvent>(),other,next);
  }catch{busy=false;throw;}
 }
 internal Prepared? PrepareApply(SceneObjectHandle handle,SceneCreatureView6CompositeSnapshot expected,CreatureView6CompositeDelta plan)
 {
  Enter();try
  {
   Open();if(!CurrentHandle(handle)){busy=false;return null;}ArgumentNullException.ThrowIfNull(expected);
   if(!ReferenceEquals(expected.Owner,identity)||!entries.TryGetValue(handle.ObjectId,out var current)||!ReferenceEquals(expected,current)||registry.Find(handle.ObjectId)!.Incarnation!=expected.Incarnation){busy=false;return null;}
   ArgumentNullException.ThrowIfNull(plan);if(plan.ObjectId!=handle.ObjectId)throw new InvalidDataException("CREO6 composite delta recipient.");
   long other=checked(bytes-current.State.DecodedBytes);ulong next=checked(revision+1);
   var change=current.State.Apply(plan,Effective(other));return Build(handle,current.Incarnation,change.Current,change.Events,other,next);
  }catch{busy=false;throw;}
 }
 internal void CheckPrepared(Prepared p)
 {
  if(Environment.CurrentManagedThreadId!=ownerThread||!busy||closed||!ReferenceEquals(p.Owner,this)||!ReferenceEquals(activePrepared,p)||p.Consumed||p.Revision!=revision+1||p.Epoch!=epoch+1||!CurrentHandle(p.Handle)||registry.Find(p.Handle.ObjectId)?.Incarnation!=p.Incarnation)
   throw new InvalidOperationException("Stale/foreign CREO6 composite prepared candidate.");
 }
 // Caller checks while the owner gate is held; no callbacks or foreign mutations may intervene.
 internal void CommitPrepared(Prepared p)
 {entries=p.Entries;bytes=p.Bytes;pending=p.Events;revision=p.Revision;epoch=p.Epoch;issued=null;p.Consumed=true;}
 internal void ReleasePrepared(Prepared p)
 {if(Environment.CurrentManagedThreadId!=ownerThread||!ReferenceEquals(p.Owner,this)||!ReferenceEquals(activePrepared,p))throw new InvalidOperationException("Foreign CREO6 composite preparation release.");activePrepared=null;busy=false;}
 public SceneCreatureView6CompositeSnapshot? Seed(SceneObjectHandle handle,CreatureView6Baseline baseline)
 {var p=PrepareSeed(handle,baseline);if(p is null)return null;try{CheckPrepared(p);CommitPrepared(p);return p.Snapshot;}finally{ReleasePrepared(p);}}
 public SceneCreatureView6CompositeSnapshot? Apply(SceneObjectHandle handle,SceneCreatureView6CompositeSnapshot expected,CreatureView6CompositeDelta plan)
 {var p=PrepareApply(handle,expected,plan);if(p is null)return null;try{CheckPrepared(p);CommitPrepared(p);return p.Snapshot;}finally{ReleasePrepared(p);}}
 internal bool IsCurrentBatch(SceneCreatureView6CompositeEventBatch batch)
 {Enter();try{return !closed&&ReferenceEquals(batch.Owner,identity)&&ReferenceEquals(batch,issued)&&batch.StoreRevision==revision&&batch.Epoch==epoch;}finally{busy=false;}}
 private void Retire()
 {
  var stale=entries.Where(p=>!registry.IsCurrent(p.Value.Incarnation)).Select(p=>p.Key).ToArray();if(stale.Length==0)return;
  var staged=new SortedDictionary<ulong,SceneCreatureView6CompositeSnapshot>(entries);long total=bytes;foreach(var id in stale){total-=staged[id].State.DecodedBytes;staged.Remove(id);}ulong nextEpoch=checked(epoch+1);
  entries=staged;bytes=total;epoch=nextEpoch;issued=null;
 }
 public ulong Revision{get{Enter();try{return revision;}finally{busy=false;}}}
 public long RetainedStateBytes{get{Enter();try{Retire();return bytes;}finally{busy=false;}}}
 public int PendingEventCount{get{Enter();try{return pending.Length;}finally{busy=false;}}}
 public SceneCreatureView6CompositeSnapshot? Find(SceneObjectHandle handle)
 {Enter();try{Retire();return !closed&&CurrentHandle(handle)&&entries.TryGetValue(handle.ObjectId,out var s)&&registry.Find(handle.ObjectId)!.Incarnation==s.Incarnation?s:null;}finally{busy=false;}}
 public bool IsCurrent(SceneCreatureView6CompositeSnapshot s)
 {Enter();try{ArgumentNullException.ThrowIfNull(s);Retire();return !closed&&ReferenceEquals(s.Owner,identity)&&CurrentHandle(s.Handle)&&entries.TryGetValue(s.Handle.ObjectId,out var latest)&&ReferenceEquals(s,latest);}finally{busy=false;}}
 public SceneCreatureView6CompositeEventBatch DrainEvents()
 {Enter();try{Open();Retire();return issued??=new(identity,revision,epoch,pending.ToArray());}finally{busy=false;}}
 public bool Acknowledge(SceneCreatureView6CompositeEventBatch batch)
 {Enter();try{ArgumentNullException.ThrowIfNull(batch);if(closed||!ReferenceEquals(batch.Owner,identity)||!ReferenceEquals(batch,issued)||batch.StoreRevision!=revision||batch.Epoch!=epoch)return false;ulong next=checked(epoch+1);pending=Array.Empty<SceneCreatureView6CompositeEventRecord>();epoch=next;issued=null;return true;}finally{busy=false;}}
 public void Close(){Enter();try{if(closed)return;entries.Clear();pending=Array.Empty<SceneCreatureView6CompositeEventRecord>();bytes=0;issued=null;closed=true;}finally{busy=false;}}
 public void Dispose()=>Close();
}
