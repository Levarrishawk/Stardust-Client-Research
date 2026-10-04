using System.Reflection;
using System.Text.Json;
using Stardust.Core.Protocol;
using Stardust.Core.Session;
int checks=0;var cases=new List<string>();
void Check(bool value,string why){checks++;if(!value)throw new Exception(why);}
void Reject<T>(Action a)where T:Exception{try{a();}catch(T){checks++;return;}throw new Exception("Expected "+typeof(T).Name);}
SceneObjectHandle Create(SceneObjectRegistry r,ulong id)=>r.Create(r.SceneGeneration,new(id,0,0,0,1,0,0,0,1,0))!.Handle;
CreatureView6Baseline Empty(ulong id)=>new(id,22,0,new(0,Array.Empty<ulong>()),0,ReplicationByteString.Create(Array.Empty<byte>()),ReplicationByteString.Create(Array.Empty<byte>()),0,0,0,0,0,0,0,0,0,new(0,Array.Empty<int>()),new(0,Array.Empty<int>()),new(0,Array.Empty<CreatureView6Wearable>()),ReplicationByteString.Create(Array.Empty<byte>()),0);
CreatureView6FullDelta Plan(ulong id)=>new(id,65535,new[]{new CreatureView6FullUpdate(CreatureView6Update.String(3,ReplicationByteString.Create(new byte[]{255})))});
try
{
 const ulong id=7;var baseline=Empty(id);
 {
  var r=new SceneObjectRegistry(4,1);var h=Create(r,id);using var s=new SceneCreatureView6FullReplicationStore(r);var batch=s.DrainEvents();var candidate=s.PrepareSeed(h,baseline)!;
  Reject<InvalidOperationException>(()=>{_=s.Revision;});Reject<InvalidOperationException>(()=>s.DrainEvents());Reject<InvalidOperationException>(()=>s.Close());Reject<InvalidOperationException>(()=>s.PrepareSeed(h,baseline));
  r.EndBaselines(1,new(id));Reject<InvalidOperationException>(()=>s.CheckPrepared(candidate));s.ReleasePrepared(candidate);Check(s.Revision==0&&s.RetainedStateBytes==0&&ReferenceEquals(batch,s.DrainEvents()),"Stale prepared handle cancellation retains exact prior publication/batch");
  var now=r.Find(id)!.Handle;var seed=s.Seed(now,baseline)!;var issued=s.DrainEvents();var canceled=s.PrepareApply(now,seed,Plan(id))!;s.ReleasePrepared(canceled);Check(s.IsCurrent(seed)&&s.PendingEventCount==0&&ReferenceEquals(issued,s.DrainEvents()),"Released candidate no publication");var active=s.PrepareApply(now,seed,Plan(id))!;
  Reject<InvalidOperationException>(()=>s.CheckPrepared(canceled));Reject<InvalidOperationException>(()=>s.ReleasePrepared(canceled));s.CheckPrepared(active);s.CommitPrepared(active);Reject<InvalidOperationException>(()=>s.CheckPrepared(active));s.ReleasePrepared(active);Reject<InvalidOperationException>(()=>s.ReleasePrepared(active));Check(!s.IsCurrent(seed)&&s.PendingEventCount==1&&s.Revision==2&&!s.Acknowledge(issued),"Exact active/consumed candidate assignment-only publication");cases.Add("stale-cancel-slot-consumed");
 }
 {
  var r=new SceneObjectRegistry(4,1);var h=Create(r,id);using var a=new SceneCreatureView6FullReplicationStore(r);using var b=new SceneCreatureView6FullReplicationStore(r);var sa=a.Seed(h,baseline)!;var sb=b.Seed(h,baseline)!;var ba=a.DrainEvents();var bb=b.DrainEvents();var pa=a.PrepareApply(h,sa,Plan(id))!;var pb=b.PrepareApply(h,sb,Plan(id))!;
  Reject<InvalidOperationException>(()=>a.CheckPrepared(pb));Reject<InvalidOperationException>(()=>a.ReleasePrepared(pb));Exception? offthread=null;Task.Run(()=>{try{a.CheckPrepared(pa);}catch(Exception e){offthread=e;}}).GetAwaiter().GetResult();Check(offthread is InvalidOperationException,"Check owner thread rejects without consuming candidate");offthread=null;Task.Run(()=>{try{a.ReleasePrepared(pa);}catch(Exception e){offthread=e;}}).GetAwaiter().GetResult();Check(offthread is InvalidOperationException,"Release owner thread retains gate");a.CheckPrepared(pa);b.CheckPrepared(pb);a.ReleasePrepared(pa);b.ReleasePrepared(pb);Check(a.IsCurrent(sa)&&b.IsCurrent(sb)&&ReferenceEquals(ba,a.DrainEvents())&&ReferenceEquals(bb,b.DrainEvents()),"Foreign token attempts leave both active candidates unchanged");cases.Add("foreign-owner-candidate");
 }
 {
  var r=new SceneObjectRegistry(4,1);var h=Create(r,id);using var s=new SceneCreatureView6FullReplicationStore(r);var seed=s.Seed(h,baseline)!;var batch=s.DrainEvents();var p=s.PrepareApply(h,seed,Plan(id))!;r.Destroy(1,new(id,0));var recreated=Create(r,id);Reject<InvalidOperationException>(()=>s.CheckPrepared(p));s.ReleasePrepared(p);Check(s.Revision==1&&s.PendingEventCount==0&&!s.IsCurrent(seed)&&s.Find(recreated) is null&&!s.Acknowledge(batch),"Recreate during preparation retires old incarnation without delta prefix");var fresh=s.Seed(recreated,baseline)!;var later=s.PrepareApply(recreated,fresh,Plan(id))!;r.Reset();Reject<InvalidOperationException>(()=>s.CheckPrepared(later));s.ReleasePrepared(later);Check(s.RetainedStateBytes==0&&s.PendingEventCount==0&&s.Revision==2,"Registry reset before check cancels prepared candidate");cases.Add("recreate-reset-prepared");
 }
 {
  var r=new SceneObjectRegistry(4,1);var h=Create(r,id);using var s=new SceneCreatureView6FullReplicationStore(r);var seed=s.Seed(h,baseline)!;var current=s.Apply(h,seed,Plan(id))!;
  typeof(SceneCreatureView6FullReplicationStore).GetField("epoch",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(s,ulong.MaxValue);typeof(SceneCreatureView6FullReplicationStore).GetField("issued",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(s,null);var batch=s.DrainEvents();Reject<OverflowException>(()=>s.Acknowledge(batch));Reject<OverflowException>(()=>s.Apply(h,current,Plan(id)));Check(s.IsCurrent(current)&&s.Revision==2&&s.PendingEventCount==1&&ReferenceEquals(batch,s.DrainEvents()),"Exhausted epoch ack/build preserves prior state/issued batch");r.Destroy(1,new(id,0));Reject<OverflowException>(()=>{_=s.RetainedStateBytes;});Check(s.Revision==2&&s.PendingEventCount==1,"Retirement epoch overflow leaves publication fields unchanged");cases.Add("epoch-exhaustion");
 }
 File.WriteAllText(args[0],JsonSerializer.Serialize(new{Checks=checks,Cases=cases},new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine($"R38_FULL_STORE_INTERNAL_PASS checks={checks} cases={cases.Count}");
}
catch(Exception e){Console.Error.WriteLine(e);Environment.ExitCode=1;}
