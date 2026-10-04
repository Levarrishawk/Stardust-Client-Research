using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using Stardust.Core.Protocol;
using Stardust.Core.Session;
namespace Stardust.Core.SmokeTests;
public static class SceneCreatureView4PartialStoreScenarios
{
 static int checks;
 static void Check(bool v){checks++;if(!v)throw new InvalidDataException($"Partial store check {checks}");}
 static void Reject(Action action){bool failed=false;try{action();}catch(Exception e)when(e is InvalidDataException or InvalidOperationException or OverflowException or ObjectDisposedException){failed=true;}Check(failed);}
 static object? Call(SceneCreatureView4PartialReplicationStore s,string method,params object?[] args)
 {try{return typeof(SceneCreatureView4PartialReplicationStore).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(s,args);}catch(TargetInvocationException e){ExceptionDispatchInfo.Capture(e.InnerException!).Throw();throw;}}
 static SceneCreateObjectByCrcMessage Create(ulong id)=>new(id,0,0,0,1,0,0,0,1,0);
 static SceneObjectHandle Add(SceneObjectRegistry r,ulong id)=>r.Create(r.SceneGeneration,Create(id))!.Handle;
 static CreatureView4Delta Delta(ulong id,ushort field=0,ulong bits=0)=>new(id,ushort.MaxValue,[new(field,bits)]);
 static object Row(CreatureView4PartialState s)=>new {s.ObjectId,s.Revision,s.KnownMask,s.DecodedBytes,Fields=s.KnownFields.Select(f=>new{f.Index,f.Bits,f.Provenance}).ToArray()};
 public static void Run(string fixture,string output)
 {
  var results=new List<object>();using var json=JsonDocument.Parse(File.ReadAllText(fixture));
  foreach(var c in json.RootElement.EnumerateArray())
  {
   var r=new SceneObjectRegistry(2,1);var handles=new[]{Add(r,1),Add(r,2)};using var s=new SceneCreatureView4PartialReplicationStore(r);
   var snapshots=new SceneCreatureView4PartialSnapshot?[2];var steps=new List<object>();int prior=0;
   foreach(var x in c.GetProperty("Steps").EnumerateArray())
   {
    int actor=x.GetProperty("Actor").GetInt32();var updates=x.GetProperty("Updates").EnumerateArray().Select(u=>new CreatureView4Update(u[0].GetUInt16(),u[1].GetUInt64())).ToArray();
    var p=CreatureView4Codec.DecodeDelta(Convert.FromHexString(x.GetProperty("Wire").GetString()!),new());Check(p.ObjectId==(ulong)actor+1&&p.DeclaredCount==x.GetProperty("Declared").GetUInt16());Check(p.Updates.Select(u=>(u.Index,u.Bits)).SequenceEqual(updates.Select(u=>(u.Index,u.Bits))));
    var old=snapshots[actor];var otherActor=snapshots[1-actor];var issued=s.DrainEvents();var n=s.Apply(handles[actor],old,p)!;Check(n is not null);snapshots[actor]=n;
    Check(!s.Acknowledge(issued));if(old is not null)Check(!s.IsCurrent(old));Check(s.IsCurrent(n!));Check(s.Apply(handles[actor],null,p) is null);if(otherActor is not null)Check(s.IsCurrent(otherActor)&&ReferenceEquals(s.Find(handles[1-actor]),otherActor));
    var b=s.DrainEvents();Check(b.Events.Count==prior+updates.Length);for(int i=0;i<b.Events.Count;i++)Check(s.IsCurrentEvent(b,i));
    steps.Add(new{State=Row(n!.State),n.Revision,n.WorkUsed,StateBytes=s.RetainedStateBytes,PendingBytes=s.PendingEventBytes,NewEvents=b.Events.Skip(prior).Select(e=>new{e.Incarnation.ObjectId,e.StoreRevision,e.Event}).ToArray()});prior=b.Events.Count;
   }
   var final=s.DrainEvents();results.Add(new{Steps=steps,Events=final.Events.Select(e=>new{e.Incarnation.ObjectId,e.StoreRevision,e.Event}).ToArray()});Check(s.Acknowledge(final));Check(!s.Acknowledge(final));Check(s.PendingEventCount==0);
  }
  Boundaries();HardBoundary();Prepared();Lifecycle();File.WriteAllText(output,JsonSerializer.Serialize(results));Console.WriteLine($"partial-store cases={results.Count} checks={checks}");
 }
 static void Boundaries()
 {
  // Two first assignments: 96 retained bytes, 128 journal bytes, 41 then 43 work.
  var exact=new SceneCreatureView4PartialStoreLimits(2,96,2,1,224,43);
  for(int cap=0;cap<7;cap++)
  {
   var l=cap switch{1=>exact with{MaximumObjects=1},2=>exact with{MaximumStateBytes=95},3=>exact with{MaximumPendingEvents=1},4=>exact with{MaximumEventsPerApply=0},5=>exact with{MaximumOutputBytes=223},6=>exact with{MaximumWork=42},_=>exact};
   var r=new SceneObjectRegistry(2,1);var a=Add(r,1);var b=Add(r,2);using var s=new SceneCreatureView4PartialReplicationStore(r,l);
   if(cap==4){Reject(()=>s.Apply(a,null,Delta(1)));Check(s.Revision==0&&s.PendingEventCount==0);continue;}
   Check(s.Apply(a,null,Delta(1)) is not null);var receipt=s.DrainEvents();
   if(cap==0){Check(s.Apply(b,null,Delta(2))!.WorkUsed==43);Check(s.RetainedStateBytes==96&&s.PendingEventBytes==128);}
   else{Reject(()=>s.Apply(b,null,Delta(2)));Check(s.Revision==1&&s.PendingEventCount==1);Check(ReferenceEquals(receipt,s.DrainEvents()));}
  }
  var rr=new SceneObjectRegistry(2,1);var aa=Add(rr,1);using var ss=new SceneCreatureView4PartialReplicationStore(rr,exact with{MaximumObjects=1});ss.Apply(aa,null,Delta(1));var batch=ss.DrainEvents();rr.Destroy(1,new(1,0));var bb=Add(rr,2);
  Reject(()=>ss.Apply(bb,null,Delta(2)));Check(ss.Revision==1&&ss.PendingEventCount==1);Check(ss.Acknowledge(batch));Check(ss.RetainedStateBytes==0);Check(ss.Apply(bb,null,Delta(2)) is not null);
  using var overflow=new SceneCreatureView4PartialReplicationStore(rr,revisionSeed:ulong.MaxValue);Reject(()=>overflow.Apply(bb,null,Delta(2)));Check(overflow.PendingEventCount==0&&overflow.RetainedStateBytes==0);
  using var wrong=new SceneCreatureView4PartialReplicationStore(rr);Reject(()=>wrong.Apply(bb,null,Delta(1)));Reject(()=>wrong.Apply(bb,null,new(2,0,Array.Empty<CreatureView4Update>())));Check(wrong.Revision==0);
  typeof(SceneCreatureView4PartialReplicationStore).GetField("epoch",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(wrong,ulong.MaxValue);Reject(()=>wrong.Apply(bb,null,Delta(2)));Check(wrong.Revision==0&&wrong.PendingEventCount==0);
 }
 static void HardBoundary()
 {
  var r=new SceneObjectRegistry(1,1);var a=Add(r,1);var p=new CreatureView4Delta(1,0,Enumerable.Range(0,4096).Select(_=>new CreatureView4Update(0,0)));
  var limits=new SceneCreatureView4PartialStoreLimits(1,48,4096,4096,262192,24611);
  var partial=new CreatureView4PartialLimits(4096,1,4096,48,262192,12311,new(MaximumFields:4096));
  using(var s=new SceneCreatureView4PartialReplicationStore(r,limits,partial)){var n=s.Apply(a,null,p)!;Check(n.WorkUsed==24611&&n.State.KnownCount==1&&s.PendingEventBytes==262144);var es=s.DrainEvents();Check(es.Events.Count==4096&&es.Events[4095].Event.Ordinal==4095&&es.Events[4095].Event.OldProvenance!.Value.Ordinal==4094);}
  foreach(var l in new[]{partial with{MaximumUpdates=4095},partial with{MaximumKnownFields=0},partial with{MaximumEvents=4095},partial with{MaximumStateBytes=47},partial with{MaximumOutputBytes=262191},partial with{MaximumWork=12310},partial with{MessageLimits=new(MaximumFields:4095)}})
  {using var s=new SceneCreatureView4PartialReplicationStore(r,limits,l);Reject(()=>s.Apply(a,null,p));Check(s.Revision==0&&s.PendingEventCount==0&&s.RetainedStateBytes==0);}
 }
 static void Prepared()
 {
  var r=new SceneObjectRegistry(2,1);var a=Add(r,1);using var s=new SceneCreatureView4PartialReplicationStore(r);using var other=new SceneCreatureView4PartialReplicationStore(r);
  var p=Call(s,"PrepareApply",a,null,Delta(1))!;var foreign=Call(other,"PrepareApply",a,null,Delta(1))!;
  Reject(()=>Call(s,"CheckPrepared",foreign));Reject(()=>Call(s,"ReleasePrepared",foreign));Reject(()=>{_=s.Revision;});Reject(()=>s.Close());
  Task.Run(()=>Reject(()=>Call(s,"CheckPrepared",p))).GetAwaiter().GetResult();Task.Run(()=>Reject(()=>Call(s,"ReleasePrepared",p))).GetAwaiter().GetResult();
  Call(other,"ReleasePrepared",foreign);Call(s,"CheckPrepared",p);Call(s,"ReleasePrepared",p);Check(s.Revision==0&&s.PendingEventCount==0);
  var q=Call(s,"PrepareApply",a,null,Delta(1))!;Reject(()=>Call(s,"CheckPrepared",p));Reject(()=>Call(s,"ReleasePrepared",p));Call(s,"CommitPrepared",q);Reject(()=>Call(s,"CommitPrepared",q));Call(s,"ReleasePrepared",q);Reject(()=>Call(s,"ReleasePrepared",q));Check(s.Revision==1);
  var before=s.Find(a)!;var issued=s.DrainEvents();var stale=Call(s,"PrepareApply",a,before,Delta(1,1,0x80000000))!;r.EndBaselines(1,new(1));Reject(()=>Call(s,"CheckPrepared",stale));Call(s,"ReleasePrepared",stale);Check(s.Revision==1&&s.PendingEventCount==1);Check(ReferenceEquals(issued,s.DrainEvents()));Check(s.IsCurrent(before));
  a=r.Find(1)!.Handle;Check(s.Apply(a,before,Delta(1,1,0x80000000)) is not null);
  foreach(int mutation in new[]{0,1,2})
  {
   a=r.Find(1)!.Handle;var expected=s.Find(a);var z=Call(s,"PrepareApply",a,expected,Delta(1))!;
   if(mutation==0)r.Replace(r.SceneGeneration,Create(1));else if(mutation==1){r.Destroy(r.SceneGeneration,new(1,0));Add(r,1);}else{r.Reset();Add(r,1);}
   Reject(()=>Call(s,"CommitPrepared",z));Call(s,"ReleasePrepared",z);Check(s.Apply(r.Find(1)!.Handle,null,Delta(1))!.State.Revision==1);
  }
 }
 static void Lifecycle()
 {
  var r=new SceneObjectRegistry(2,1);var a=Add(r,1);using var s=new SceneCreatureView4PartialReplicationStore(r);var first=s.Apply(a,null,Delta(1,0,0))!;Check(first.State.KnownCount==1&&!first.State.IsKnown(1));Check(first.State.TryGetField(0,out var f)&&f!.Bits==0);var batch=s.DrainEvents();Check(s.IsCurrentEvent(batch,0));
  r.EndBaselines(1,new(1));a=r.Find(1)!.Handle;Check(s.IsCurrent(first)&&ReferenceEquals(s.Find(a),first));Check(s.IsCurrentEvent(batch,0));
  var next=s.Apply(a,first,new(1,0,[new(0,0),new(6,ulong.MaxValue),new(0,0x7FC01234)]))!;Check(next.State.IsKnown(6)&&next.State.Revision==2);var events=s.DrainEvents();Check(events.Events[1].Event.OldWasKnown&&events.Events[1].Event.OldBits==0&&events.Events[2].Event.OldBits is null);Check(events.Events[3].Event.OldProvenance!.Value.Ordinal==0);
  using var other=new SceneCreatureView4PartialReplicationStore(r);Check(other.Apply(a,next,Delta(1)) is null);Check(!other.Acknowledge(events)&&!other.IsCurrentEvent(events,0));Check(s.Apply(a,first,Delta(1)) is null);
  a=r.Replace(1,Create(1))!.Handle;Check(!s.IsCurrentEvent(events,0));var fresh=s.Apply(a,null,Delta(1,1,0x80000000))!;Check(fresh.State.Revision==1&&!fresh.State.IsKnown(0));Check(s.PendingEventCount==5);var history=s.DrainEvents();Check(!s.IsCurrentEvent(history,0)&&s.IsCurrentEvent(history,4));
  r.Reset();Check(!s.IsCurrent(fresh));Check(s.PendingEventCount==5&&s.RetainedStateBytes==0);Check(!s.Acknowledge(history));var retired=s.DrainEvents();Check(retired.Events.Count==5&&s.Acknowledge(retired));s.Close();Check(!r.IsClosed&&s.PendingEventCount==0);Reject(()=>s.Apply(a,null,Delta(1)));Reject(()=>s.DrainEvents());
 }
}
