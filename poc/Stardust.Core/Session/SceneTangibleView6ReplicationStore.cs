using Stardust.Core.Protocol;
namespace Stardust.Core.Session;
public sealed record SceneTangibleView6StoreLimits(int MaximumObjects=64,long MaximumStateBytes=1048576,
 int MaximumPendingEvents=4096,int MaximumEventsPerApply=4096,long MaximumOutputBytes=2097152,long MaximumWork=65536);
public sealed class SceneTangibleView6Snapshot
{
 internal SceneTangibleView6Snapshot(object owner,SceneObjectHandle handle,SceneObjectIncarnation incarnation,ulong revision,TangibleView6State state)
 {Owner=owner;Handle=handle;Incarnation=incarnation;Revision=revision;State=state;}
 internal object Owner{get;} public SceneObjectHandle Handle{get;} public SceneObjectIncarnation Incarnation{get;}
 public ulong Revision{get;} public TangibleView6State State{get;}
}
public sealed record SceneTangibleView6EventRecord(SceneObjectHandle Handle,SceneObjectIncarnation Incarnation,ulong StoreRevision,TangibleView6StateEvent Event);
public sealed class SceneTangibleView6EventBatch
{
 internal SceneTangibleView6EventBatch(object owner,ulong revision,ulong epoch,SceneTangibleView6EventRecord[] events)
 {Owner=owner;StoreRevision=revision;Epoch=epoch;Events=Array.AsReadOnly(events);}
 internal object Owner{get;} internal ulong Epoch{get;} public ulong StoreRevision{get;}
 public IReadOnlyList<SceneTangibleView6EventRecord> Events{get;}
}
/// <summary>Owned TANO6 state/journal over borrowed serialized registry; no dispatch or world integration.</summary>
public sealed class SceneTangibleView6ReplicationStore:IDisposable
{
 private readonly SceneObjectRegistry registry;
 private readonly SceneTangibleView6StoreLimits limits;
 private readonly TangibleView6StateLimits stateLimits;
 private readonly object identity=new(); private readonly int ownerThread=Environment.CurrentManagedThreadId;
 private SortedDictionary<ulong,SceneTangibleView6Snapshot> entries=new();
 private SceneTangibleView6EventRecord[] pending=Array.Empty<SceneTangibleView6EventRecord>();
 private Prepared? activePrepared;
 private SceneTangibleView6EventBatch? issued; private long bytes; private ulong revision,epoch; private bool busy,closed;
 public SceneTangibleView6ReplicationStore(SceneObjectRegistry registry,SceneTangibleView6StoreLimits? limits=null,TangibleView6StateLimits? stateLimits=null,ulong revisionSeed=0)
 {
  ArgumentNullException.ThrowIfNull(registry);var l=limits??new();var s=stateLimits??new();
  if(l.MaximumObjects is <1 or >4096||l.MaximumStateBytes is <8 or >134217728||l.MaximumPendingEvents is <0 or >262144||l.MaximumEventsPerApply is <0 or >262144||l.MaximumOutputBytes is <8 or >268435456||l.MaximumWork is <0 or >16777216)throw new ArgumentOutOfRangeException(nameof(limits));
  if(s.MaximumElements is <0 or >65536||s.MaximumFields is <0 or >4096||s.MaximumOperations is <0 or >65536||s.MaximumAssignmentValues is <0 or >65535||s.MaximumMessageIds is <0 or >1048576||s.MaximumMessageBytes is <0 or >8388608||s.MaximumStateBytes is <8 or >16777216||s.MaximumEvents is <0 or >262144||s.MaximumOutputBytes is <8 or >33554432||s.MaximumWork is <0 or >16777216)throw new ArgumentOutOfRangeException(nameof(stateLimits));
  this.registry=registry;this.limits=l with {};this.stateLimits=s with {};revision=revisionSeed;
 }
 private void Enter(){if(Environment.CurrentManagedThreadId!=ownerThread)throw new InvalidOperationException("TANO6 store owner thread required.");if(busy)throw new InvalidOperationException("TANO6 store reentry.");busy=true;}
 private void Open()=>ObjectDisposedException.ThrowIf(closed,this);
 private bool CurrentHandle(SceneObjectHandle handle)=>registry.IsCurrent(handle);
 private TangibleView6StateLimits Effective(long other)
 {
  long state=Math.Min(stateLimits.MaximumStateBytes,limits.MaximumStateBytes-other);
  long output=Math.Min(stateLimits.MaximumOutputBytes,limits.MaximumOutputBytes-other-24L*pending.Length);
  if(state<8||output<8)throw new InvalidDataException("Global TANO6 state/output capacity.");
  return stateLimits with {MaximumStateBytes=state,MaximumOutputBytes=output,MaximumEvents=Math.Min(stateLimits.MaximumEvents,Math.Min(limits.MaximumEventsPerApply,limits.MaximumPendingEvents-pending.Length)),MaximumWork=Math.Min(stateLimits.MaximumWork,limits.MaximumWork)};
 }
 private Prepared Build(SceneObjectHandle handle,SceneObjectIncarnation incarnation,TangibleView6State state,IReadOnlyList<TangibleView6StateEvent> events,long other,ulong next)
 {
  var snapshot=new SceneTangibleView6Snapshot(identity,handle,incarnation,next,state);
  var staged=new SortedDictionary<ulong,SceneTangibleView6Snapshot>(entries){[handle.ObjectId]=snapshot};
  var queue=new SceneTangibleView6EventRecord[checked(pending.Length+events.Count)];pending.CopyTo(queue,0);
  for(int i=0;i<events.Count;i++)queue[pending.Length+i]=new(handle,incarnation,next,events[i]);
  long total=checked(other+state.DecodedBytes);ulong nextEpoch=checked(epoch+1);
  if(!CurrentHandle(handle)||registry.Find(handle.ObjectId)?.Incarnation!=incarnation)throw new InvalidDataException("Stale TANO6 commit recipient.");
  // Prebuilt assignment only: state is published before events become observable after owner gate releases.
  var prepared=new Prepared(this,handle,incarnation,staged,total,queue,next,nextEpoch,snapshot);activePrepared=prepared;return prepared;
 }
 internal sealed class Prepared
 {
  internal Prepared(SceneTangibleView6ReplicationStore owner,SceneObjectHandle handle,SceneObjectIncarnation incarnation,SortedDictionary<ulong,SceneTangibleView6Snapshot> entries,long bytes,SceneTangibleView6EventRecord[] events,ulong revision,ulong epoch,SceneTangibleView6Snapshot snapshot)
  {Owner=owner;Handle=handle;Incarnation=incarnation;Entries=entries;Bytes=bytes;Events=events;Revision=revision;Epoch=epoch;Snapshot=snapshot;}
  internal SceneTangibleView6ReplicationStore Owner{get;} internal SceneObjectHandle Handle{get;} internal SceneObjectIncarnation Incarnation{get;}
  internal SortedDictionary<ulong,SceneTangibleView6Snapshot> Entries{get;} internal long Bytes{get;} internal SceneTangibleView6EventRecord[] Events{get;}
  internal ulong Revision{get;} internal ulong Epoch{get;} internal SceneTangibleView6Snapshot Snapshot{get;} internal bool Consumed{get;set;}
 }
 internal Prepared? PrepareSeed(SceneObjectHandle handle,TangibleView6Baseline baseline)
 {
  Enter();try
  {
   Open();if(!CurrentHandle(handle)){busy=false;return null;}ArgumentNullException.ThrowIfNull(baseline);
   if(baseline.ObjectId!=handle.ObjectId)throw new InvalidDataException("TANO6 seed recipient.");
   var incarnation=registry.Find(handle.ObjectId)!.Incarnation;entries.TryGetValue(handle.ObjectId,out var old);
   if(old is null&&entries.Count==limits.MaximumObjects)throw new InvalidDataException("TANO6 object capacity.");
   long other=checked(bytes-(old?.State.DecodedBytes??0));ulong next=checked(revision+1);
   var state=TangibleView6State.FromBaseline(baseline,Effective(other),old?.Incarnation==incarnation?old.State.Revision:0);
   return Build(handle,incarnation,state,Array.Empty<TangibleView6StateEvent>(),other,next);
  }catch{busy=false;throw;}
 }
 internal Prepared? PrepareApply(SceneObjectHandle handle,SceneTangibleView6Snapshot expected,TangibleView6Delta plan)
 {
  Enter();try
  {
   Open();if(!CurrentHandle(handle)){busy=false;return null;}ArgumentNullException.ThrowIfNull(expected);
   if(!ReferenceEquals(expected.Owner,identity)||!entries.TryGetValue(handle.ObjectId,out var current)||!ReferenceEquals(expected,current)||registry.Find(handle.ObjectId)!.Incarnation!=expected.Incarnation){busy=false;return null;}
   ArgumentNullException.ThrowIfNull(plan);if(plan.ObjectId!=handle.ObjectId)throw new InvalidDataException("TANO6 delta recipient.");
   long other=checked(bytes-current.State.DecodedBytes);ulong next=checked(revision+1);
   var change=current.State.Apply(plan,Effective(other));return Build(handle,current.Incarnation,change.Current,change.Events,other,next);
  }catch{busy=false;throw;}
 }
 internal void CheckPrepared(Prepared p)
 {
  if(Environment.CurrentManagedThreadId!=ownerThread||!busy||closed||!ReferenceEquals(p.Owner,this)||!ReferenceEquals(activePrepared,p)||p.Consumed||p.Revision!=revision+1||p.Epoch!=epoch+1||!CurrentHandle(p.Handle)||registry.Find(p.Handle.ObjectId)?.Incarnation!=p.Incarnation)
   throw new InvalidOperationException("Stale/foreign TANO6 prepared candidate.");
 }
 // Caller checks while the owner gate is held; no callbacks or foreign mutations may intervene.
 internal void CommitPrepared(Prepared p)
 {entries=p.Entries;bytes=p.Bytes;pending=p.Events;revision=p.Revision;epoch=p.Epoch;issued=null;p.Consumed=true;}
 internal void ReleasePrepared(Prepared p)
 {if(Environment.CurrentManagedThreadId!=ownerThread||!ReferenceEquals(p.Owner,this)||!ReferenceEquals(activePrepared,p))throw new InvalidOperationException("Foreign TANO6 preparation release.");activePrepared=null;busy=false;}
 public SceneTangibleView6Snapshot? Seed(SceneObjectHandle handle,TangibleView6Baseline baseline)
 {var p=PrepareSeed(handle,baseline);if(p is null)return null;try{CheckPrepared(p);CommitPrepared(p);return p.Snapshot;}finally{ReleasePrepared(p);}}
 public SceneTangibleView6Snapshot? Apply(SceneObjectHandle handle,SceneTangibleView6Snapshot expected,TangibleView6Delta plan)
 {var p=PrepareApply(handle,expected,plan);if(p is null)return null;try{CheckPrepared(p);CommitPrepared(p);return p.Snapshot;}finally{ReleasePrepared(p);}}
 internal bool IsCurrentBatch(SceneTangibleView6EventBatch batch)
 {Enter();try{return !closed&&ReferenceEquals(batch.Owner,identity)&&ReferenceEquals(batch,issued)&&batch.StoreRevision==revision&&batch.Epoch==epoch;}finally{busy=false;}}
 private void Retire()
 {
  var stale=entries.Where(p=>!registry.IsCurrent(p.Value.Incarnation)).Select(p=>p.Key).ToArray();if(stale.Length==0)return;
  var staged=new SortedDictionary<ulong,SceneTangibleView6Snapshot>(entries);long total=bytes;foreach(var id in stale){total-=staged[id].State.DecodedBytes;staged.Remove(id);}ulong nextEpoch=checked(epoch+1);
  entries=staged;bytes=total;epoch=nextEpoch;issued=null;
 }
 public ulong Revision{get{Enter();try{return revision;}finally{busy=false;}}}
 public long RetainedStateBytes{get{Enter();try{Retire();return bytes;}finally{busy=false;}}}
 public int PendingEventCount{get{Enter();try{return pending.Length;}finally{busy=false;}}}
 public SceneTangibleView6Snapshot? Find(SceneObjectHandle handle)
 {Enter();try{Retire();return !closed&&CurrentHandle(handle)&&entries.TryGetValue(handle.ObjectId,out var s)&&registry.Find(handle.ObjectId)!.Incarnation==s.Incarnation?s:null;}finally{busy=false;}}
 public bool IsCurrent(SceneTangibleView6Snapshot s)
 {Enter();try{ArgumentNullException.ThrowIfNull(s);Retire();return !closed&&ReferenceEquals(s.Owner,identity)&&CurrentHandle(s.Handle)&&entries.TryGetValue(s.Handle.ObjectId,out var latest)&&ReferenceEquals(s,latest);}finally{busy=false;}}
 public SceneTangibleView6EventBatch DrainEvents()
 {Enter();try{Open();Retire();return issued??=new(identity,revision,epoch,pending.ToArray());}finally{busy=false;}}
 public bool Acknowledge(SceneTangibleView6EventBatch batch)
 {Enter();try{ArgumentNullException.ThrowIfNull(batch);if(closed||!ReferenceEquals(batch.Owner,identity)||!ReferenceEquals(batch,issued)||batch.StoreRevision!=revision||batch.Epoch!=epoch)return false;ulong next=checked(epoch+1);pending=Array.Empty<SceneTangibleView6EventRecord>();epoch=next;issued=null;return true;}finally{busy=false;}}
 public void Close(){Enter();try{if(closed)return;entries.Clear();pending=Array.Empty<SceneTangibleView6EventRecord>();bytes=0;issued=null;closed=true;}finally{busy=false;}}
 public void Dispose()=>Close();
}
