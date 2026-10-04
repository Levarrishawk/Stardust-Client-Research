using Stardust.Core.Protocol;
namespace Stardust.Core.Session;
public sealed record SceneCreatureView6StoreLimits(int MaximumObjects=64,long MaximumStateBytes=1048576,
 int MaximumPendingEvents=4096,int MaximumEventsPerApply=4096,long MaximumOutputBytes=2097152,long MaximumWork=1048576);
public sealed class SceneCreatureView6Snapshot
{
 internal SceneCreatureView6Snapshot(object owner,SceneObjectHandle handle,SceneObjectIncarnation incarnation,ulong revision,CreatureView6State state)
 {Owner=owner;Handle=handle;Incarnation=incarnation;Revision=revision;State=state;}
 internal object Owner{get;} public SceneObjectHandle Handle{get;} public SceneObjectIncarnation Incarnation{get;}
 public ulong Revision{get;} public CreatureView6State State{get;}
}
public sealed record SceneCreatureView6EventRecord(SceneObjectHandle Handle,SceneObjectIncarnation Incarnation,ulong StoreRevision,CreatureView6StateEvent Event);
public sealed class SceneCreatureView6EventBatch
{
 internal SceneCreatureView6EventBatch(object owner,ulong revision,ulong epoch,SceneCreatureView6EventRecord[] events)
 {Owner=owner;StoreRevision=revision;Epoch=epoch;Events=Array.AsReadOnly(events);}
 internal object Owner{get;} internal ulong Epoch{get;} public ulong StoreRevision{get;}
 public IReadOnlyList<SceneCreatureView6EventRecord> Events{get;}
}
/// <summary>Owned CREO6 state/journal over borrowed serialized registry; no dispatch or world integration.</summary>
public sealed class SceneCreatureView6ReplicationStore:IDisposable
{
 private readonly SceneObjectRegistry registry;
 private readonly SceneCreatureView6StoreLimits limits;
 private readonly CreatureView6StateLimits stateLimits;
 private readonly object identity=new(); private readonly int ownerThread=Environment.CurrentManagedThreadId;
 private SortedDictionary<ulong,SceneCreatureView6Snapshot> entries=new();
 private SceneCreatureView6EventRecord[] pending=Array.Empty<SceneCreatureView6EventRecord>();
 private Prepared? activePrepared;
 private SceneCreatureView6EventBatch? issued; private long bytes; private ulong revision,epoch; private bool busy,closed;
 public SceneCreatureView6ReplicationStore(SceneObjectRegistry registry,SceneCreatureView6StoreLimits? limits=null,CreatureView6StateLimits? stateLimits=null,ulong revisionSeed=0)
 {
  ArgumentNullException.ThrowIfNull(registry);var l=limits??new();var s=stateLimits??new();
  if(l.MaximumObjects is <1 or >4096||l.MaximumStateBytes is <8 or >134217728||l.MaximumPendingEvents is <0 or >262144||l.MaximumEventsPerApply is <0 or >262144||l.MaximumOutputBytes is <8 or >268435456||l.MaximumWork is <0 or >16777216)throw new ArgumentOutOfRangeException(nameof(limits));
  CreatureView6State.Validate(s);
  this.registry=registry;this.limits=l with {};this.stateLimits=s with {};revision=revisionSeed;
 }
 private void Enter(){if(Environment.CurrentManagedThreadId!=ownerThread)throw new InvalidOperationException("CREO6 store owner thread required.");if(busy)throw new InvalidOperationException("CREO6 store reentry.");busy=true;}
 private void Open()=>ObjectDisposedException.ThrowIf(closed,this);
 private bool CurrentHandle(SceneObjectHandle handle)=>registry.IsCurrent(handle);
 private CreatureView6StateLimits Effective(long other)
 {
  long state=Math.Min(stateLimits.MaximumStateBytes,limits.MaximumStateBytes-other);
  long output=Math.Min(stateLimits.MaximumOutputBytes,limits.MaximumOutputBytes-other-8L*pending.Length);
  if(state<8||output<8)throw new InvalidDataException("Global CREO6 state/output capacity.");
  return stateLimits with {MaximumStateBytes=state,MaximumOutputBytes=output,MaximumEvents=Math.Min(stateLimits.MaximumEvents,Math.Min(limits.MaximumEventsPerApply,limits.MaximumPendingEvents-pending.Length)),MaximumWork=Math.Min(stateLimits.MaximumWork,limits.MaximumWork)};
 }
 private Prepared Build(SceneObjectHandle handle,SceneObjectIncarnation incarnation,CreatureView6State state,IReadOnlyList<CreatureView6StateEvent> events,long other,ulong next)
 {
  var snapshot=new SceneCreatureView6Snapshot(identity,handle,incarnation,next,state);
  var staged=new SortedDictionary<ulong,SceneCreatureView6Snapshot>(entries){[handle.ObjectId]=snapshot};
  var queue=new SceneCreatureView6EventRecord[checked(pending.Length+events.Count)];pending.CopyTo(queue,0);
  for(int i=0;i<events.Count;i++)queue[pending.Length+i]=new(handle,incarnation,next,events[i]);
  long total=checked(other+state.DecodedBytes);ulong nextEpoch=checked(epoch+1);
  if(!CurrentHandle(handle)||registry.Find(handle.ObjectId)?.Incarnation!=incarnation)throw new InvalidDataException("Stale CREO6 commit recipient.");
  // Prebuilt assignment only: state is published before events become observable after owner gate releases.
  // Candidate payloads are owned; active identity binds check/release.
  var prepared=new Prepared(this,handle,incarnation,staged,total,queue,next,nextEpoch,snapshot);activePrepared=prepared;return prepared;
 }
 internal sealed class Prepared
 {
  internal Prepared(SceneCreatureView6ReplicationStore owner,SceneObjectHandle handle,SceneObjectIncarnation incarnation,SortedDictionary<ulong,SceneCreatureView6Snapshot> entries,long bytes,SceneCreatureView6EventRecord[] events,ulong revision,ulong epoch,SceneCreatureView6Snapshot snapshot)
  {Owner=owner;Handle=handle;Incarnation=incarnation;Entries=entries;Bytes=bytes;Events=events;Revision=revision;Epoch=epoch;Snapshot=snapshot;}
  internal SceneCreatureView6ReplicationStore Owner{get;} internal SceneObjectHandle Handle{get;} internal SceneObjectIncarnation Incarnation{get;}
  internal SortedDictionary<ulong,SceneCreatureView6Snapshot> Entries{get;} internal long Bytes{get;} internal SceneCreatureView6EventRecord[] Events{get;}
  internal ulong Revision{get;} internal ulong Epoch{get;} internal SceneCreatureView6Snapshot Snapshot{get;} internal bool Consumed{get;set;}
 }
 internal Prepared? PrepareSeed(SceneObjectHandle handle,CreatureView6Baseline baseline)
 {
  Enter();try
  {
   Open();if(!CurrentHandle(handle)){busy=false;return null;}ArgumentNullException.ThrowIfNull(baseline);
   if(baseline.ObjectId!=handle.ObjectId)throw new InvalidDataException("CREO6 seed recipient.");
   var incarnation=registry.Find(handle.ObjectId)!.Incarnation;entries.TryGetValue(handle.ObjectId,out var old);
   if(old is null&&entries.Count==limits.MaximumObjects)throw new InvalidDataException("CREO6 object capacity.");
   long other=checked(bytes-(old?.State.DecodedBytes??0));ulong next=checked(revision+1);
   var state=CreatureView6State.FromBaseline(baseline,Effective(other),old?.Incarnation==incarnation?old.State.Revision:0);
   return Build(handle,incarnation,state,Array.Empty<CreatureView6StateEvent>(),other,next);
  }catch{busy=false;throw;}
 }
 internal Prepared? PrepareApply(SceneObjectHandle handle,SceneCreatureView6Snapshot expected,CreatureView6Delta plan)
 {
  Enter();try
  {
   Open();if(!CurrentHandle(handle)){busy=false;return null;}ArgumentNullException.ThrowIfNull(expected);
   if(!ReferenceEquals(expected.Owner,identity)||!entries.TryGetValue(handle.ObjectId,out var current)||!ReferenceEquals(expected,current)||registry.Find(handle.ObjectId)!.Incarnation!=expected.Incarnation){busy=false;return null;}
   ArgumentNullException.ThrowIfNull(plan);if(plan.ObjectId!=handle.ObjectId)throw new InvalidDataException("CREO6 delta recipient.");
   long other=checked(bytes-current.State.DecodedBytes);ulong next=checked(revision+1);
   var change=current.State.Apply(plan,Effective(other));return Build(handle,current.Incarnation,change.Current,change.Events,other,next);
  }catch{busy=false;throw;}
 }
 internal void CheckPrepared(Prepared p)
 {
  if(Environment.CurrentManagedThreadId!=ownerThread||!busy||closed||!ReferenceEquals(p.Owner,this)||!ReferenceEquals(activePrepared,p)||p.Consumed||p.Revision!=revision+1||p.Epoch!=epoch+1||!CurrentHandle(p.Handle)||registry.Find(p.Handle.ObjectId)?.Incarnation!=p.Incarnation)
   throw new InvalidOperationException("Stale/foreign CREO6 prepared candidate.");
 }
 // Caller checks while the owner gate is held; no callbacks or foreign mutations may intervene.
 internal void CommitPrepared(Prepared p)
 {entries=p.Entries;bytes=p.Bytes;pending=p.Events;revision=p.Revision;epoch=p.Epoch;issued=null;p.Consumed=true;}
 internal void ReleasePrepared(Prepared p)
 {if(Environment.CurrentManagedThreadId!=ownerThread||!ReferenceEquals(p.Owner,this)||!ReferenceEquals(activePrepared,p))throw new InvalidOperationException("Foreign CREO6 preparation release.");activePrepared=null;busy=false;}
 public SceneCreatureView6Snapshot? Seed(SceneObjectHandle handle,CreatureView6Baseline baseline)
 {var p=PrepareSeed(handle,baseline);if(p is null)return null;try{CheckPrepared(p);CommitPrepared(p);return p.Snapshot;}finally{ReleasePrepared(p);}}
 public SceneCreatureView6Snapshot? Apply(SceneObjectHandle handle,SceneCreatureView6Snapshot expected,CreatureView6Delta plan)
 {var p=PrepareApply(handle,expected,plan);if(p is null)return null;try{CheckPrepared(p);CommitPrepared(p);return p.Snapshot;}finally{ReleasePrepared(p);}}
 internal bool IsCurrentBatch(SceneCreatureView6EventBatch batch)
 {Enter();try{return !closed&&ReferenceEquals(batch.Owner,identity)&&ReferenceEquals(batch,issued)&&batch.StoreRevision==revision&&batch.Epoch==epoch;}finally{busy=false;}}
 private void Retire()
 {
  var stale=entries.Where(p=>!registry.IsCurrent(p.Value.Incarnation)).Select(p=>p.Key).ToArray();if(stale.Length==0)return;
  var staged=new SortedDictionary<ulong,SceneCreatureView6Snapshot>(entries);long total=bytes;foreach(var id in stale){total-=staged[id].State.DecodedBytes;staged.Remove(id);}ulong nextEpoch=checked(epoch+1);
  entries=staged;bytes=total;epoch=nextEpoch;issued=null;
 }
 public ulong Revision{get{Enter();try{return revision;}finally{busy=false;}}}
 public long RetainedStateBytes{get{Enter();try{Retire();return bytes;}finally{busy=false;}}}
 public int PendingEventCount{get{Enter();try{return pending.Length;}finally{busy=false;}}}
 public SceneCreatureView6Snapshot? Find(SceneObjectHandle handle)
 {Enter();try{Retire();return !closed&&CurrentHandle(handle)&&entries.TryGetValue(handle.ObjectId,out var s)&&registry.Find(handle.ObjectId)!.Incarnation==s.Incarnation?s:null;}finally{busy=false;}}
 public bool IsCurrent(SceneCreatureView6Snapshot s)
 {Enter();try{ArgumentNullException.ThrowIfNull(s);Retire();return !closed&&ReferenceEquals(s.Owner,identity)&&CurrentHandle(s.Handle)&&entries.TryGetValue(s.Handle.ObjectId,out var latest)&&ReferenceEquals(s,latest);}finally{busy=false;}}
 public SceneCreatureView6EventBatch DrainEvents()
 {Enter();try{Open();Retire();return issued??=new(identity,revision,epoch,pending.ToArray());}finally{busy=false;}}
 public bool Acknowledge(SceneCreatureView6EventBatch batch)
 {Enter();try{ArgumentNullException.ThrowIfNull(batch);if(closed||!ReferenceEquals(batch.Owner,identity)||!ReferenceEquals(batch,issued)||batch.StoreRevision!=revision||batch.Epoch!=epoch)return false;ulong next=checked(epoch+1);pending=Array.Empty<SceneCreatureView6EventRecord>();epoch=next;issued=null;return true;}finally{busy=false;}}
 public void Close(){Enter();try{if(closed)return;entries.Clear();pending=Array.Empty<SceneCreatureView6EventRecord>();bytes=0;issued=null;closed=true;}finally{busy=false;}}
 public void Dispose()=>Close();
}
