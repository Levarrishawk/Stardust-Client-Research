using Stardust.Core.Protocol;
namespace Stardust.Core.Session;
public sealed record SceneCreatureView4SupportedStoreLimits(int MaximumObjects=64,long MaximumStateBytes=1048576,
 int MaximumPendingEvents=4096,int MaximumEventsPerApply=4096,long MaximumOutputBytes=2097152,long MaximumWork=1048576);
public sealed class SceneCreatureView4SupportedSnapshot
{
 internal SceneCreatureView4SupportedSnapshot(object owner,SceneObjectHandle handle,SceneObjectIncarnation incarnation,ulong revision,CreatureView4SupportedState state)
 {Owner=owner;Handle=handle;Incarnation=incarnation;Revision=revision;State=state;}
 internal object Owner{get;} public SceneObjectHandle Handle{get;} public SceneObjectIncarnation Incarnation{get;}
 public ulong Revision{get;} public CreatureView4SupportedState State{get;}
}
public sealed record SceneCreatureView4SupportedEventRecord(SceneObjectHandle Handle,SceneObjectIncarnation Incarnation,ulong StoreRevision,CreatureView4SupportedEvent Event);
public sealed class SceneCreatureView4SupportedEventBatch
{
 internal SceneCreatureView4SupportedEventBatch(object owner,ulong revision,ulong epoch,SceneCreatureView4SupportedEventRecord[] events)
 {Owner=owner;StoreRevision=revision;Epoch=epoch;Events=Array.AsReadOnly(events);}
 internal object Owner{get;} internal ulong Epoch{get;} public ulong StoreRevision{get;}
 public IReadOnlyList<SceneCreatureView4SupportedEventRecord> Events{get;}
}
/// <summary>Owned CREO4 supported state/journal over borrowed serialized registry; no dispatch or world integration.</summary>
public sealed class SceneCreatureView4SupportedReplicationStore:IDisposable
{
 private readonly SceneObjectRegistry registry;
 private readonly SceneCreatureView4SupportedStoreLimits limits;
 private readonly CreatureView4SupportedStateLimits stateLimits;
 private readonly object identity=new(); private readonly int ownerThread=Environment.CurrentManagedThreadId;
 private SortedDictionary<ulong,SceneCreatureView4SupportedSnapshot> entries=new();
 private SceneCreatureView4SupportedEventRecord[] pending=Array.Empty<SceneCreatureView4SupportedEventRecord>();
 private Prepared? activePrepared;
 private SceneCreatureView4SupportedEventBatch? issued; private long bytes; private ulong revision,epoch; private bool busy,closed;
 public SceneCreatureView4SupportedReplicationStore(SceneObjectRegistry registry,SceneCreatureView4SupportedStoreLimits? limits=null,CreatureView4SupportedStateLimits? stateLimits=null,ulong revisionSeed=0)
 {
  ArgumentNullException.ThrowIfNull(registry);var l=limits??new();var s=stateLimits??new();
  if(l.MaximumObjects is <1 or >4096||l.MaximumStateBytes is <8 or >134217728||l.MaximumPendingEvents is <0 or >262144||l.MaximumEventsPerApply is <0 or >262144||l.MaximumOutputBytes is <8 or >268435456||l.MaximumWork is <0 or >16777216)throw new ArgumentOutOfRangeException(nameof(limits));
  var model=s.ModelLimits??new();var message=s.MessageLimits??new();if(model.MaximumUpdates is <0 or >4096||model.MaximumElements is <0 or >65536||model.MaximumTotalElements is <0 or >196608||model.MaximumStringBytes is <0 or >1048576||model.MaximumTotalStringBytes is <0 or >4194304||model.MaximumStateBytes is <0 or >16777216||model.MaximumEvents is <0 or >262144||model.MaximumOutputBytes is <0 or >33554432||model.MaximumWork is <0 or >16777216)throw new ArgumentOutOfRangeException(nameof(stateLimits));CreatureView4SupportedCodec.Validate(message);
  this.registry=registry;this.limits=l with {};this.stateLimits=new(model with {},message with {});revision=revisionSeed;
 }
 private void Enter(){if(Environment.CurrentManagedThreadId!=ownerThread)throw new InvalidOperationException("CREO4 supported store owner thread required.");if(busy)throw new InvalidOperationException("CREO4 supported store reentry.");busy=true;}
 private void Open()=>ObjectDisposedException.ThrowIf(closed,this);
 private bool CurrentHandle(SceneObjectHandle handle)=>registry.IsCurrent(handle);
 private CreatureView4SupportedStateLimits Effective(long other)
 {
  var model=stateLimits.ModelLimits!;var message=stateLimits.MessageLimits!;
  long state=Math.Min(model.MaximumStateBytes,limits.MaximumStateBytes-other);
  long output=Math.Min(model.MaximumOutputBytes,limits.MaximumOutputBytes-other-PendingBytes());
  if(state<8||output<8)throw new InvalidDataException("Global CREO4 supported state/output capacity.");
  return new(model with {MaximumStateBytes=state,MaximumOutputBytes=output,MaximumEvents=Math.Min(model.MaximumEvents,Math.Min(limits.MaximumEventsPerApply,limits.MaximumPendingEvents-pending.Length)),MaximumWork=Math.Min(model.MaximumWork,limits.MaximumWork)},message with {MaximumWork=Math.Min(message.MaximumWork,limits.MaximumWork)});
 }
 private long PendingBytes()=>pending.Sum(e=>e.Event.LogicalBytes);
 public long PendingEventBytes{get{Enter();try{return PendingBytes();}finally{busy=false;}}}
 private Prepared Build(SceneObjectHandle handle,SceneObjectIncarnation incarnation,CreatureView4SupportedState state,IReadOnlyList<CreatureView4SupportedEvent> events,long other,ulong next)
 {
  long total=checked(other+state.DecodedBytes);ulong nextEpoch=checked(epoch+1);
  int queued=checked(pending.Length+events.Count),actors=entries.Count+(entries.ContainsKey(handle.ObjectId)?0:1);
  if(actors>limits.MaximumObjects||total>limits.MaximumStateBytes||queued>limits.MaximumPendingEvents||events.Count>limits.MaximumEventsPerApply||checked(total+PendingBytes()+events.Sum(e=>e.LogicalBytes))>limits.MaximumOutputBytes)throw new InvalidDataException("Global CREO4 supported publication capacity.");
  var snapshot=new SceneCreatureView4SupportedSnapshot(identity,handle,incarnation,next,state);
  var staged=new SortedDictionary<ulong,SceneCreatureView4SupportedSnapshot>(entries){[handle.ObjectId]=snapshot};
  var queue=new SceneCreatureView4SupportedEventRecord[queued];pending.CopyTo(queue,0);
  for(int i=0;i<events.Count;i++)queue[pending.Length+i]=new(handle,incarnation,next,events[i]);
  if(!CurrentHandle(handle)||registry.Find(handle.ObjectId)?.Incarnation!=incarnation)throw new InvalidDataException("Stale CREO4 supported commit recipient.");
  // Prebuilt assignment only: state is published before events become observable after owner gate releases.
  // Candidate payloads are owned; active identity binds check/release.
  var prepared=new Prepared(this,handle,incarnation,staged,total,queue,next,nextEpoch,snapshot);activePrepared=prepared;return prepared;
 }
 internal sealed class Prepared
 {
  internal Prepared(SceneCreatureView4SupportedReplicationStore owner,SceneObjectHandle handle,SceneObjectIncarnation incarnation,SortedDictionary<ulong,SceneCreatureView4SupportedSnapshot> entries,long bytes,SceneCreatureView4SupportedEventRecord[] events,ulong revision,ulong epoch,SceneCreatureView4SupportedSnapshot snapshot)
  {Owner=owner;Handle=handle;Incarnation=incarnation;Entries=entries;Bytes=bytes;Events=events;Revision=revision;Epoch=epoch;Snapshot=snapshot;}
  internal SceneCreatureView4SupportedReplicationStore Owner{get;} internal SceneObjectHandle Handle{get;} internal SceneObjectIncarnation Incarnation{get;}
  internal SortedDictionary<ulong,SceneCreatureView4SupportedSnapshot> Entries{get;} internal long Bytes{get;} internal SceneCreatureView4SupportedEventRecord[] Events{get;}
  internal ulong Revision{get;} internal ulong Epoch{get;} internal SceneCreatureView4SupportedSnapshot Snapshot{get;} internal bool Consumed{get;set;}
 }
 internal Prepared? PrepareSeed(SceneObjectHandle handle,CreatureView4Baseline baseline)
 {
  Enter();try
  {
   Open();if(!CurrentHandle(handle)){busy=false;return null;}ArgumentNullException.ThrowIfNull(baseline);
   if(baseline.ObjectId!=handle.ObjectId)throw new InvalidDataException("CREO4 supported seed recipient.");
   var incarnation=registry.Find(handle.ObjectId)!.Incarnation;entries.TryGetValue(handle.ObjectId,out var old);
   if(old is null&&entries.Count==limits.MaximumObjects)throw new InvalidDataException("CREO4 supported object capacity.");
   long other=checked(bytes-(old?.State.DecodedBytes??0));ulong next=checked(revision+1);
   var state=CreatureView4SupportedState.FromBaseline(baseline,Effective(other),old?.Incarnation==incarnation?old.State.Revision:0);
   return Build(handle,incarnation,state,Array.Empty<CreatureView4SupportedEvent>(),other,next);
  }catch{busy=false;throw;}
 }
 internal Prepared? PrepareApply(SceneObjectHandle handle,SceneCreatureView4SupportedSnapshot expected,CreatureView4SupportedDelta plan)
 {
  Enter();try
  {
   Open();if(!CurrentHandle(handle)){busy=false;return null;}ArgumentNullException.ThrowIfNull(expected);
   if(!ReferenceEquals(expected.Owner,identity)||!entries.TryGetValue(handle.ObjectId,out var current)||!ReferenceEquals(expected,current)||registry.Find(handle.ObjectId)!.Incarnation!=expected.Incarnation){busy=false;return null;}
   ArgumentNullException.ThrowIfNull(plan);if(plan.ObjectId!=handle.ObjectId)throw new InvalidDataException("CREO4 supported delta recipient.");
   long other=checked(bytes-current.State.DecodedBytes);ulong next=checked(revision+1);
   var change=current.State.Apply(plan,Effective(other));return Build(handle,current.Incarnation,change.Current,change.Events,other,next);
  }catch{busy=false;throw;}
 }
 internal void CheckPrepared(Prepared p)
 {
  ArgumentNullException.ThrowIfNull(p);
  if(Environment.CurrentManagedThreadId!=ownerThread||!busy||closed||!ReferenceEquals(p.Owner,this)||!ReferenceEquals(activePrepared,p)||p.Consumed||p.Revision!=checked(revision+1)||p.Epoch!=checked(epoch+1)||!CurrentHandle(p.Handle)||registry.Find(p.Handle.ObjectId)?.Incarnation!=p.Incarnation)
   throw new InvalidOperationException("Stale/foreign CREO4 supported prepared candidate.");
 }
 // Revalidate exact active ownership/currentness before prebuilt assignments; no callbacks.
 internal void CommitPrepared(Prepared p)
 {CheckPrepared(p);entries=p.Entries;bytes=p.Bytes;pending=p.Events;revision=p.Revision;epoch=p.Epoch;issued=null;p.Consumed=true;}
 internal void ReleasePrepared(Prepared p)
 {ArgumentNullException.ThrowIfNull(p);if(Environment.CurrentManagedThreadId!=ownerThread||!ReferenceEquals(p.Owner,this)||!ReferenceEquals(activePrepared,p))throw new InvalidOperationException("Foreign CREO4 supported preparation release.");activePrepared=null;busy=false;}
 public SceneCreatureView4SupportedSnapshot? Seed(SceneObjectHandle handle,CreatureView4Baseline baseline)
 {var p=PrepareSeed(handle,baseline);if(p is null)return null;try{CheckPrepared(p);CommitPrepared(p);return p.Snapshot;}finally{ReleasePrepared(p);}}
 public SceneCreatureView4SupportedSnapshot? Apply(SceneObjectHandle handle,SceneCreatureView4SupportedSnapshot expected,CreatureView4SupportedDelta plan)
 {var p=PrepareApply(handle,expected,plan);if(p is null)return null;try{CheckPrepared(p);CommitPrepared(p);return p.Snapshot;}finally{ReleasePrepared(p);}}
 private bool CurrentBatch(SceneCreatureView4SupportedEventBatch batch)=>!closed&&ReferenceEquals(batch.Owner,identity)&&ReferenceEquals(batch,issued)&&batch.StoreRevision==revision&&batch.Epoch==epoch;
 internal bool IsCurrentBatch(SceneCreatureView4SupportedEventBatch batch)
 {Enter();try{return CurrentBatch(batch);}finally{busy=false;}}
 /// <summary>Exact journal plus current registry incarnation only; host phase/delivery serialization is separate.</summary>
 public bool IsCurrentEvent(SceneCreatureView4SupportedEventBatch batch,int index)
 {Enter();try{ArgumentNullException.ThrowIfNull(batch);return CurrentBatch(batch)&&index>=0&&index<batch.Events.Count&&registry.IsCurrent(batch.Events[index].Incarnation);}finally{busy=false;}}
 private void Retire()
 {
  var stale=entries.Where(p=>!registry.IsCurrent(p.Value.Incarnation)).Select(p=>p.Key).ToArray();if(stale.Length==0)return;
  var staged=new SortedDictionary<ulong,SceneCreatureView4SupportedSnapshot>(entries);long total=bytes;foreach(var id in stale){total-=staged[id].State.DecodedBytes;staged.Remove(id);}ulong nextEpoch=checked(epoch+1);
  entries=staged;bytes=total;epoch=nextEpoch;issued=null;
 }
 public ulong Revision{get{Enter();try{return revision;}finally{busy=false;}}}
 public long RetainedStateBytes{get{Enter();try{Retire();return bytes;}finally{busy=false;}}}
 public int PendingEventCount{get{Enter();try{return pending.Length;}finally{busy=false;}}}
 public SceneCreatureView4SupportedSnapshot? Find(SceneObjectHandle handle)
 {Enter();try{Retire();return !closed&&CurrentHandle(handle)&&entries.TryGetValue(handle.ObjectId,out var s)&&registry.Find(handle.ObjectId)!.Incarnation==s.Incarnation?s:null;}finally{busy=false;}}
 public bool IsCurrent(SceneCreatureView4SupportedSnapshot s)
 {Enter();try{ArgumentNullException.ThrowIfNull(s);Retire();return !closed&&ReferenceEquals(s.Owner,identity)&&CurrentHandle(s.Handle)&&entries.TryGetValue(s.Handle.ObjectId,out var latest)&&ReferenceEquals(s,latest);}finally{busy=false;}}
 public SceneCreatureView4SupportedEventBatch DrainEvents()
 {Enter();try{Open();Retire();return issued??=new(identity,revision,epoch,pending.ToArray());}finally{busy=false;}}
 public bool Acknowledge(SceneCreatureView4SupportedEventBatch batch)
 {Enter();try{ArgumentNullException.ThrowIfNull(batch);if(closed||!ReferenceEquals(batch.Owner,identity)||!ReferenceEquals(batch,issued)||batch.StoreRevision!=revision||batch.Epoch!=epoch)return false;ulong next=checked(epoch+1);pending=Array.Empty<SceneCreatureView4SupportedEventRecord>();epoch=next;issued=null;return true;}finally{busy=false;}}
 public void Close(){Enter();try{if(closed)return;entries.Clear();pending=Array.Empty<SceneCreatureView4SupportedEventRecord>();bytes=0;issued=null;closed=true;}finally{busy=false;}}
 public void Dispose()=>Close();
}
