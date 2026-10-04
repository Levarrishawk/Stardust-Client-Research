using Stardust.Core.Protocol;
namespace Stardust.Core.Session;
public sealed record SceneCreatureView4PartialStoreLimits(int MaximumObjects=64,long MaximumStateBytes=1048576,
 int MaximumPendingEvents=4096,int MaximumEventsPerApply=4096,long MaximumOutputBytes=2097152,long MaximumWork=1048576);
public sealed class SceneCreatureView4PartialSnapshot
{
 internal SceneCreatureView4PartialSnapshot(object owner,SceneObjectHandle handle,SceneObjectIncarnation incarnation,ulong revision,CreatureView4PartialState state,ushort declaredCount,long workUsed)
 {Owner=owner;Handle=handle;Incarnation=incarnation;Revision=revision;State=state;DeclaredCount=declaredCount;WorkUsed=workUsed;}
 internal object Owner{get;} public SceneObjectHandle Handle{get;} public SceneObjectIncarnation Incarnation{get;}
 public ulong Revision{get;} public CreatureView4PartialState State{get;}public ushort DeclaredCount{get;}public long WorkUsed{get;}
}
public sealed record SceneCreatureView4PartialEventRecord(SceneObjectHandle Handle,SceneObjectIncarnation Incarnation,ulong StoreRevision,CreatureView4PartialEvent Event);
public sealed class SceneCreatureView4PartialEventBatch
{
 internal SceneCreatureView4PartialEventBatch(object owner,ulong revision,ulong epoch,SceneCreatureView4PartialEventRecord[] events)
 {Owner=owner;StoreRevision=revision;Epoch=epoch;Events=Array.AsReadOnly(events);}
 internal object Owner{get;} internal ulong Epoch{get;} public ulong StoreRevision{get;}
 public IReadOnlyList<SceneCreatureView4PartialEventRecord> Events{get;}
}
/// <summary>Owned received-only CREO4 state and fixed 64-byte logical events over a borrowed serialized registry.
/// Received-only scalars; pending/finalized incarnations, no defaults or full baseline conversion.</summary>
public sealed class SceneCreatureView4PartialReplicationStore:IDisposable
{
 private readonly SceneObjectRegistry registry;
 private readonly SceneCreatureView4PartialStoreLimits limits;
 private readonly CreatureView4PartialLimits stateLimits;
 private readonly object identity=new(); private readonly int ownerThread=Environment.CurrentManagedThreadId;
 private SortedDictionary<ulong,SceneCreatureView4PartialSnapshot> entries=new();
 private SceneCreatureView4PartialEventRecord[] pending=Array.Empty<SceneCreatureView4PartialEventRecord>();
 private Prepared? activePrepared;
 private SceneCreatureView4PartialEventBatch? issued; private long bytes; private ulong revision,epoch; private bool busy,closed;
 public SceneCreatureView4PartialReplicationStore(SceneObjectRegistry registry,SceneCreatureView4PartialStoreLimits? limits=null,CreatureView4PartialLimits? stateLimits=null,ulong revisionSeed=0)
 {
  ArgumentNullException.ThrowIfNull(registry);var l=limits??new();var s=stateLimits??new();
  if(l.MaximumObjects is <1 or >4096||l.MaximumStateBytes is <8 or >134217728||l.MaximumPendingEvents is <0 or >262144||l.MaximumEventsPerApply is <0 or >262144||l.MaximumOutputBytes is <8 or >268435456||l.MaximumWork is <0 or >16777216)throw new ArgumentOutOfRangeException(nameof(limits));
  var message=s.MessageLimits??new();
  if(s.MaximumUpdates is <0 or >4096||s.MaximumKnownFields is <0 or >11||s.MaximumEvents is <0 or >4096||s.MaximumStateBytes is <0 or >16777216||s.MaximumOutputBytes is <0 or >33554432||s.MaximumWork is <0 or >16777216)throw new ArgumentOutOfRangeException(nameof(stateLimits));
  CreatureView4Codec.Validate(message);
  this.registry=registry;this.limits=l with {};this.stateLimits=s with{MessageLimits=message with{}};revision=revisionSeed;
 }
 private void Enter(){if(Environment.CurrentManagedThreadId!=ownerThread)throw new InvalidOperationException("CREO4 partial store owner thread required.");if(busy)throw new InvalidOperationException("CREO4 partial store reentry.");busy=true;}
 private void Open()=>ObjectDisposedException.ThrowIf(closed,this);
 private bool CurrentHandle(SceneObjectHandle handle)=>registry.IsCurrent(handle)&&registry.Find(handle.ObjectId)?.Phase is SceneObjectPhase.PendingBaselines or SceneObjectPhase.Finalized;
 private CreatureView4PartialLimits Effective(long other,long overhead)
 {
  long state=Math.Min(stateLimits.MaximumStateBytes,limits.MaximumStateBytes-other),output=Math.Min(stateLimits.MaximumOutputBytes,limits.MaximumOutputBytes-other-64L*pending.Length);
  return stateLimits with{MaximumStateBytes=state,MaximumOutputBytes=output,MaximumEvents=Math.Min(stateLimits.MaximumEvents,Math.Min(limits.MaximumEventsPerApply,limits.MaximumPendingEvents-pending.Length)),MaximumWork=Math.Min(stateLimits.MaximumWork,limits.MaximumWork-overhead),MessageLimits=stateLimits.MessageLimits! with{}};
 }
 internal sealed class Prepared
 {
  internal Prepared(SceneCreatureView4PartialReplicationStore owner,SceneObjectHandle handle,SceneObjectIncarnation incarnation,SortedDictionary<ulong,SceneCreatureView4PartialSnapshot> entries,long bytes,SceneCreatureView4PartialEventRecord[] events,ulong revision,ulong epoch,SceneCreatureView4PartialSnapshot snapshot,SceneCreatureView4PartialSnapshot? expected)
  {Owner=owner;Handle=handle;Incarnation=incarnation;Entries=entries;Bytes=bytes;Events=events;Revision=revision;Epoch=epoch;Snapshot=snapshot;Expected=expected;}
  internal SceneCreatureView4PartialReplicationStore Owner{get;} internal SceneObjectHandle Handle{get;} internal SceneObjectIncarnation Incarnation{get;}
  internal SortedDictionary<ulong,SceneCreatureView4PartialSnapshot> Entries{get;} internal long Bytes{get;} internal SceneCreatureView4PartialEventRecord[] Events{get;}
  internal ulong Revision{get;} internal ulong Epoch{get;} internal SceneCreatureView4PartialSnapshot Snapshot{get;} internal SceneCreatureView4PartialSnapshot? Expected{get;}internal bool Consumed{get;set;}
 }
 internal Prepared? PrepareApply(SceneObjectHandle handle,SceneCreatureView4PartialSnapshot? expected,CreatureView4Delta plan)
 {
  Enter();try
  {
   Open();if(!CurrentHandle(handle)){busy=false;return null;}
   var incarnation=registry.Find(handle.ObjectId)!.Incarnation;entries.TryGetValue(handle.ObjectId,out var old);
   var current=old?.Incarnation==incarnation?old:null;
   // Null is first-current-incarnation only. It never silently overwrites an existing partial receipt.
   if(expected is null?current is not null:!ReferenceEquals(expected.Owner,identity)||!ReferenceEquals(expected,current)||expected.Incarnation!=incarnation){busy=false;return null;}
   ArgumentNullException.ThrowIfNull(plan);if(plan.ObjectId!=handle.ObjectId||plan.Updates.Count==0||plan.Updates.Count>stateLimits.MaximumUpdates)throw new InvalidDataException("CREO4 partial recipient/update cap");
   var cost=CreatureView4Codec.MeasureDelta(plan,stateLimits.MessageLimits!);ulong next=checked(revision+1),nextEpoch=checked(epoch+1),stateNext=checked((current?.State.Revision??0)+1);
   ushort mask=current?.State.KnownMask??0;foreach(var u in plan.Updates){if(u.Index!=6)_=CreatureView4Codec.FloatSlot(u.Index);mask|=(ushort)(1<<u.Index);}
   int known=System.Numerics.BitOperations.PopCount((uint)mask),count=plan.Updates.Count,actors=entries.Count+(old is null?1:0);
   long stateBytes=16+32L*known,other=checked(bytes-(old?.State.DecodedBytes??0)),total=checked(other+stateBytes);
   int queued=checked(pending.Length+count);long kernelWork=22L+cost.Work+2L*count+known;
   long overhead=1+cost.Work+count+(current is null?11:0)+entries.Count+pending.Length+count,totalWork=checked(overhead+kernelWork);
   if(actors>limits.MaximumObjects||known>stateLimits.MaximumKnownFields||stateBytes>stateLimits.MaximumStateBytes||total>limits.MaximumStateBytes||count>stateLimits.MaximumEvents||count>limits.MaximumEventsPerApply||queued>limits.MaximumPendingEvents||stateBytes+64L*count>stateLimits.MaximumOutputBytes||total+64L*queued>limits.MaximumOutputBytes||kernelWork>stateLimits.MaximumWork||totalWork>limits.MaximumWork)throw new InvalidDataException("CREO4 partial prospective global capacity/work");
   var effective=Effective(other,overhead);
   // All recipient, shape, revisions, actor/state/journal and work reservations precede Empty/candidates.
   var previous=current?.State??CreatureView4PartialState.Empty(handle.ObjectId,effective);
   var change=previous.Apply(plan,effective);
   if(change.Current.Revision!=stateNext||change.Current.KnownMask!=mask||change.Current.DecodedBytes!=stateBytes||change.Events.Count!=count||change.WorkUsed!=kernelWork)throw new InvalidDataException("CREO4 partial prediction mismatch");
   var snapshot=new SceneCreatureView4PartialSnapshot(identity,handle,incarnation,next,change.Current,plan.DeclaredCount,totalWork);
   var staged=new SortedDictionary<ulong,SceneCreatureView4PartialSnapshot>(entries){[handle.ObjectId]=snapshot};
   var queue=new SceneCreatureView4PartialEventRecord[queued];pending.CopyTo(queue,0);for(int i=0;i<count;i++)queue[pending.Length+i]=new(handle,incarnation,next,change.Events[i]);
   if(!CurrentHandle(handle)||registry.Find(handle.ObjectId)?.Incarnation!=incarnation)throw new InvalidDataException("Stale CREO4 partial prepared recipient");
   var prepared=new Prepared(this,handle,incarnation,staged,total,queue,next,nextEpoch,snapshot,expected);activePrepared=prepared;return prepared;
  }catch{busy=false;throw;}
 }
 internal void CheckPrepared(Prepared p)
 {
  ArgumentNullException.ThrowIfNull(p);
  if(Environment.CurrentManagedThreadId!=ownerThread||!busy||closed||!ReferenceEquals(p.Owner,this)||!ReferenceEquals(activePrepared,p)||p.Consumed||p.Revision!=checked(revision+1)||p.Epoch!=checked(epoch+1)||!CurrentHandle(p.Handle)||registry.Find(p.Handle.ObjectId)?.Incarnation!=p.Incarnation)
   throw new InvalidOperationException("Stale/foreign CREO4 partial prepared candidate.");
  var current=entries.GetValueOrDefault(p.Handle.ObjectId);if(current?.Incarnation!=p.Incarnation)current=null;
  if(!ReferenceEquals(current,p.Expected))throw new InvalidOperationException("Changed exact CREO4 partial expected receipt.");
 }
 // Exact current ownership is revalidated before prebuilt assignments; no callbacks.
 internal void CommitPrepared(Prepared p)
 {CheckPrepared(p);entries=p.Entries;bytes=p.Bytes;pending=p.Events;revision=p.Revision;epoch=p.Epoch;issued=null;p.Consumed=true;}
 internal void ReleasePrepared(Prepared p)
 {ArgumentNullException.ThrowIfNull(p);if(Environment.CurrentManagedThreadId!=ownerThread||!ReferenceEquals(p.Owner,this)||!ReferenceEquals(activePrepared,p))throw new InvalidOperationException("Foreign CREO4 partial preparation release.");activePrepared=null;busy=false;}
 public SceneCreatureView4PartialSnapshot? Apply(SceneObjectHandle handle,SceneCreatureView4PartialSnapshot? expected,CreatureView4Delta plan)
 {var p=PrepareApply(handle,expected,plan);if(p is null)return null;try{CheckPrepared(p);CommitPrepared(p);return p.Snapshot;}finally{ReleasePrepared(p);}}
 private bool CurrentBatch(SceneCreatureView4PartialEventBatch batch)=>!closed&&ReferenceEquals(batch.Owner,identity)&&ReferenceEquals(batch,issued)&&batch.StoreRevision==revision&&batch.Epoch==epoch;
 internal bool IsCurrentBatch(SceneCreatureView4PartialEventBatch batch)
 {Enter();try{return CurrentBatch(batch);}finally{busy=false;}}
 /// <summary>Exact journal plus current registry incarnation only; host phase/delivery serialization is separate.</summary>
 public bool IsCurrentEvent(SceneCreatureView4PartialEventBatch batch,int index)
 {Enter();try{ArgumentNullException.ThrowIfNull(batch);return CurrentBatch(batch)&&index>=0&&index<batch.Events.Count&&registry.IsCurrent(batch.Events[index].Incarnation)&&registry.Find(batch.Events[index].Incarnation.ObjectId)?.Phase is SceneObjectPhase.PendingBaselines or SceneObjectPhase.Finalized;}finally{busy=false;}}
 private void Retire()
 {
  var stale=entries.Where(p=>!registry.IsCurrent(p.Value.Incarnation)).Select(p=>p.Key).ToArray();if(stale.Length==0)return;
  var staged=new SortedDictionary<ulong,SceneCreatureView4PartialSnapshot>(entries);long total=bytes;foreach(var id in stale){total-=staged[id].State.DecodedBytes;staged.Remove(id);}ulong nextEpoch=checked(epoch+1);
  entries=staged;bytes=total;epoch=nextEpoch;issued=null;
 }
 public ulong Revision{get{Enter();try{return revision;}finally{busy=false;}}}
 public long RetainedStateBytes{get{Enter();try{Retire();return bytes;}finally{busy=false;}}}
 public int PendingEventCount{get{Enter();try{return pending.Length;}finally{busy=false;}}}
 public long PendingEventBytes{get{Enter();try{return 64L*pending.Length;}finally{busy=false;}}}
 public SceneCreatureView4PartialSnapshot? Find(SceneObjectHandle handle)
 {Enter();try{Retire();return !closed&&CurrentHandle(handle)&&entries.TryGetValue(handle.ObjectId,out var s)&&registry.Find(handle.ObjectId)!.Incarnation==s.Incarnation?s:null;}finally{busy=false;}}
 public bool IsCurrent(SceneCreatureView4PartialSnapshot s)
 {Enter();try{ArgumentNullException.ThrowIfNull(s);Retire();return !closed&&ReferenceEquals(s.Owner,identity)&&registry.IsCurrent(s.Incarnation)&&entries.TryGetValue(s.Handle.ObjectId,out var latest)&&ReferenceEquals(s,latest);}finally{busy=false;}}
 public SceneCreatureView4PartialEventBatch DrainEvents()
 {Enter();try{Open();Retire();return issued??=new(identity,revision,epoch,pending.ToArray());}finally{busy=false;}}
 public bool Acknowledge(SceneCreatureView4PartialEventBatch batch)
 {Enter();try{ArgumentNullException.ThrowIfNull(batch);if(closed||!ReferenceEquals(batch.Owner,identity)||!ReferenceEquals(batch,issued)||batch.StoreRevision!=revision||batch.Epoch!=epoch)return false;ulong next=checked(epoch+1);pending=Array.Empty<SceneCreatureView4PartialEventRecord>();epoch=next;issued=null;return true;}finally{busy=false;}}
 public void Close(){Enter();try{if(closed)return;entries.Clear();pending=Array.Empty<SceneCreatureView4PartialEventRecord>();bytes=0;issued=null;closed=true;}finally{busy=false;}}
 public void Dispose()=>Close();
}
