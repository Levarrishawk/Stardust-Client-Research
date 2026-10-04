using System.Text.Json;
using Stardust.Core.Protocol;
using Stardust.Core.Session;
namespace Stardust.Core.SmokeTests;
public static class SceneCreatureView4SupportedStoreScenarios
{
 static int checks,budgets,negatives;
 static void Check(bool v,string why){checks++;if(!v)throw new Exception(why);}
 static void Reject(Action a,bool budget=false){try{a();}catch(Exception e)when(e is InvalidDataException or ArgumentException or OverflowException or InvalidOperationException or ObjectDisposedException){negatives++;if(budget)budgets++;return;}throw new Exception("Expected reject");}
 static CreatureView4SupportedStateLimits Wide()=>new(new(4096,65536,196608,1048576,4194304,16777216,262144,33554432,16777216),new(8388608,4096,65536,1048576,1048576,4194304,16777216,16777216));
 static CreatureView4Limits BaselineLimits=>new(8388608,4096,65536,196608,1048576,4194304,16777216,16777216);
 public static SceneObjectHandle Create(SceneObjectRegistry r,ulong id)=>r.Create(r.SceneGeneration,new(id,0,0,0,1,0,0,0,1,0))!.Handle;
 public static CreatureView4Baseline Empty(ulong id)=>new(id,65535,new uint[10],ulong.MaxValue,new(0,Array.Empty<int>()),new(0,Array.Empty<CreatureView4SkillMod>()),new(0,Array.Empty<CreatureView4MissionPair>()));
 public static CreatureView4SupportedDelta Scalar(ulong id)=>new(id,0,[new(new CreatureView4Update(6,0x8000000000000001UL))]);
 static CreatureView4SupportedDelta Skill(ulong id,byte op,byte[] key,int value)=>new(id,0,[new(new CreatureSkillModsView4Update(3,1,1,[new(op,ReplicationByteString.Create(key),value,int.MinValue)]))]);
 static object Text(ReplicationByteString s)=>new{Hex=Convert.ToHexString(s.CopyBytes()),Extended=s.UsesExtendedLength};
 static object Model(CreatureView4Baseline m)=>new{m.ObjectId,m.DeclaredCount,FloatBits=m.FloatBits.ToArray(),m.ListenTo,Encumbrances=new{m.Encumbrances.Counter,Values=m.Encumbrances.Values.ToArray()},SkillMods=new{m.SkillMods.Counter,Values=m.SkillMods.Values.Select(s=>new{s.RawCommand,Name=Text(s.Name),s.BaseValue,s.Bonus}).ToArray()},SpaceMissionObjects=new{m.SpaceMissionObjects.Counter,Values=m.SpaceMissionObjects.Values.ToArray()}};
 static object Row(CreatureView4SupportedState s)=>new{Model=Model(s.Model),s.Revision,s.DecodedBytes};
 static object Event(CreatureView4SupportedEvent e)=>new{e.Kind,e.Field,e.UpdateOrdinal,e.Opcode,e.Index,e.OldBits,e.NewBits,e.OldValue,e.NewValue,Name=e.Name is null?null:Text(e.Name),e.OldBase,e.OldBonus,e.NewBase,e.NewBonus,e.LogicalBytes};
 public static void Run(string fixture,string output)
 {
  checks=budgets=negatives=0;using var doc=JsonDocument.Parse(File.ReadAllBytes(fixture));var records=new List<object>();
  foreach(var c in doc.RootElement.GetProperty("Cases").EnumerateArray())
  {
   string name=c.GetProperty("Name").GetString()!;var baseline=CreatureView4Codec.DecodeBaseline(Convert.FromHexString(c.GetProperty("Baseline").GetString()!),BaselineLimits);var plan=CreatureView4SupportedCodec.Decode(Convert.FromHexString(c.GetProperty("Hex").GetString()!),Wide().MessageLimits!);var sc=c.GetProperty("StateCost");var seedCost=c.GetProperty("SeedCost");long L(string k)=>sc.GetProperty(k).GetInt64();long S(string k)=>seedCost.GetProperty(k).GetInt64();
   long maxState=Math.Max(L("StateBytes"),S("StateBytes")),maxOutput=Math.Max(L("OutputBytes"),S("OutputBytes")),maxWork=Math.Max(L("Work"),S("Work"));int eventCount=sc.GetProperty("Events").GetInt32();
   var limits=new SceneCreatureView4SupportedStoreLimits(1,maxState,eventCount,eventCount,maxOutput,maxWork);var r=new SceneObjectRegistry(4,1);var h=Create(r,baseline.ObjectId);using var store=new SceneCreatureView4SupportedReplicationStore(r,limits,Wide());var seed=store.Seed(h,baseline)!;var initial=store.DrainEvents();var kernel=seed.State.Apply(plan,Wide());var current=store.Apply(h,seed,plan)!;
   Check(current.Revision==2&&current.State.Revision==2&&!store.IsCurrent(seed)&&store.IsCurrent(current),name+" exact publication identities");Check(store.Apply(h,seed,null!) is null&&!store.Acknowledge(initial),name+" stale before plan / issued invalidation");var batch=store.DrainEvents();Check(ReferenceEquals(batch,store.DrainEvents())&&store.PendingEventCount==eventCount&&store.PendingEventBytes==kernel.Events.Sum(e=>e.LogicalBytes),name+" retained variable journal");Check(store.RetainedStateBytes==current.State.DecodedBytes,name+" state accounting");try{((IList<SceneCreatureView4SupportedEventRecord>)batch.Events).Clear();throw new Exception("Journal writable");}catch(NotSupportedException){checks++;}
   for(int i=0;i<batch.Events.Count;i++)Check(store.IsCurrentEvent(batch,i)&&batch.Events[i].Handle==h&&batch.Events[i].Incarnation==current.Incarnation,name+" current event recipient");Check(!store.IsCurrentEvent(batch,-1)&&!store.IsCurrentEvent(batch,batch.Events.Count),name+" journal index range");
   var rawWire=Convert.FromHexString(c.GetProperty("Hex").GetString()!);
   int step=rawWire.Length<=512?1:Math.Max(1,rawWire.Length/32);
   for(int cut=0;cut<rawWire.Length;cut+=step){int at=cut;Reject(()=>CreatureView4SupportedCodec.Decode(rawWire.AsSpan(0,at),Wide().MessageLimits!));}
   Check(store.IsCurrent(current)&&ReferenceEquals(batch,store.DrainEvents()),name+" outer truncation has no store side effects");
   string previous=JsonSerializer.Serialize(Row(current.State));var queue=batch.Events.Select(e=>new{e.StoreRevision,Event=Event(e.Event)}).ToArray();
   Reject(()=>store.Apply(h,current,new(baseline.ObjectId^1,0,plan.Updates)));Check(store.IsCurrent(current)&&ReferenceEquals(batch,store.DrainEvents())&&previous==JsonSerializer.Serialize(Row(current.State)),name+" failed recipient atomic");
   // Reseed needs seed capacity plus historical variable bytes, rather than the apply-only peak.
   Check(store.Acknowledge(batch)&&!store.Acknowledge(batch),name+" exact once ack");   // Export a separate sequence seed/apply/reseed under wide global caps for the independent historical model.
   var rr=new SceneObjectRegistry(2,1);var hh=Create(rr,baseline.ObjectId);using var ss=new SceneCreatureView4SupportedReplicationStore(rr,new(MaximumStateBytes:134217728,MaximumOutputBytes:268435456,MaximumPendingEvents:262144,MaximumEventsPerApply:262144,MaximumWork:16777216),Wide());var iseed=ss.Seed(hh,baseline)!;var icurrent=ss.Apply(hh,iseed,plan)!;var ihistory=ss.DrainEvents();long ibytes=ss.PendingEventBytes;var irseed=ss.Seed(hh,baseline)!;
   records.Add(new{Name=name,Initial=Row(iseed.State),Current=Row(icurrent.State),StoreRevision=icurrent.Revision,RetainedBytes=icurrent.State.DecodedBytes,PendingBytes=ibytes,Queue=ihistory.Events.Select(e=>new{e.StoreRevision,Event=Event(e.Event)}).ToArray(),Reseed=Row(irseed.State),ReseedStoreRevision=irseed.Revision,ReseedPendingBytes=ss.PendingEventBytes,kernel.WorkUsed,SeedWork=S("Work")});
   Check(!ss.Acknowledge(ihistory)&&!ss.IsCurrent(icurrent)&&ss.IsCurrent(irseed),name+" reseed invalidates old snapshot/batch");var fresh=ss.DrainEvents();Check(ss.Acknowledge(fresh)&&ss.PendingEventBytes==0,name+" fresh history acknowledges");
   foreach(string cap in new[]{"state","output","work","pending","events"})
   {
    if((cap is "pending" or "events")&&eventCount==0)continue;var tight=limits with{MaximumStateBytes=maxState-(cap=="state"?1:0),MaximumOutputBytes=maxOutput-(cap=="output"?1:0),MaximumWork=maxWork-(cap=="work"?1:0),MaximumPendingEvents=eventCount-(cap=="pending"?1:0),MaximumEventsPerApply=eventCount-(cap=="events"?1:0)};
    var tr=new SceneObjectRegistry(2,1);var th=Create(tr,baseline.ObjectId);using var ts=new SceneCreatureView4SupportedReplicationStore(tr,tight,Wide());var tb=ts.DrainEvents();SceneCreatureView4SupportedSnapshot? before=null;
    try{before=ts.Seed(th,baseline);}catch(InvalidDataException){Check(ts.Revision==0&&ReferenceEquals(tb,ts.DrainEvents())&&ts.PendingEventCount==0,name+" one-less seed no prefix");budgets++;continue;}
    tb=ts.DrainEvents();Reject(()=>ts.Apply(th,before!,plan),true);Check(ts.Revision==1&&ts.IsCurrent(before!)&&ReferenceEquals(tb,ts.DrainEvents())&&ts.PendingEventCount==0,name+" one-less apply no prefix");
   }
   budgets++;
  }
  var lifecycle=new List<string>();object? globalTrace=null;const ulong id=7;
  {
   var r=new SceneObjectRegistry(4,1);var h=Create(r,id);using var s=new SceneCreatureView4SupportedReplicationStore(r);var seed=s.Seed(h,Empty(id))!;var one=s.Apply(h,seed,Scalar(id))!;var b=s.DrainEvents();r.EndBaselines(1,new(id));var moved=r.Find(id)!.Handle;Check(!s.IsCurrent(one)&&ReferenceEquals(one,s.Find(moved))&&s.Apply(h,one,null!) is null&&s.IsCurrentEvent(b,0),"Historical handle same incarnation");var two=s.Apply(moved,one,Scalar(id))!;r.Destroy(1,new(id,0));var recreated=Create(r,id);Check(s.Apply(recreated,two,null!) is null&&!s.IsCurrentEvent(s.DrainEvents(),0)&&s.PendingEventCount==2,"Recreate invalidates delivery preserves journal");var fresh=s.Seed(recreated,Empty(id))!;Check(fresh.State.Revision==1&&fresh.Incarnation!=two.Incarnation,"Fresh revision on recreation");var old=s.DrainEvents();r.Reset();Check(!s.IsCurrentEvent(old,0)&&s.RetainedStateBytes==0&&!s.Acknowledge(old)&&s.Acknowledge(s.DrainEvents()),"Reset read retirement epoch");lifecycle.Add("history-move-recreate-reset");
  }
  {
   var r=new SceneObjectRegistry(4,1);var h=Create(r,id);using var s=new SceneCreatureView4SupportedReplicationStore(r,new(MaximumObjects:1));var seed=s.Seed(h,Empty(id))!;var other=Create(r,id+1);Reject(()=>s.Seed(other,Empty(id+1)),true);var alienRegistry=new SceneObjectRegistry(1,1);var alienHandle=Create(alienRegistry,id);Check(s.Seed(alienHandle,null!) is null&&s.Apply(alienHandle,seed,null!) is null,"Foreign handle rejected before input");Reject(()=>s.Seed(h,null!));using var foreign=new SceneCreatureView4SupportedReplicationStore(r);var fs=foreign.Seed(h,Empty(id))!;Check(s.Apply(h,fs,null!) is null&&!s.Acknowledge(foreign.DrainEvents()),"Foreign snapshots/batches");Exception? error=null;Task.Run(()=>{try{s.Close();}catch(Exception e){error=e;}}).GetAwaiter().GetResult();Check(error is InvalidOperationException&&s.IsCurrent(seed),"Owner gate before cleanup");s.Close();s.Close();Check(r.IsCurrent(h)&&s.RetainedStateBytes==0&&s.PendingEventBytes==0&&!s.IsCurrent(seed),"Borrowed registry unaffected close");Reject(()=>s.Seed(h,Empty(id)));lifecycle.Add("capacity-foreign-owner-close");
  }
  {
   // 72+72 state; insertion adds16+3 and variable event64+3 =>230 exact global bytes.
   var r=new SceneObjectRegistry(4,1);var h=Create(r,id);var h2=Create(r,id+1);using var s=new SceneCreatureView4SupportedReplicationStore(r,new(MaximumOutputBytes:230,MaximumPendingEvents:1));var a=s.Seed(h,Empty(id))!;s.Seed(h2,Empty(id+1));var key=new byte[]{0,128,255};var plan=Skill(id,0,key,-1);Array.Clear(key); // Plan owns name bytes.
   var first=s.Apply(h,a,plan)!;var b=s.DrainEvents();Check(s.RetainedStateBytes==163&&s.PendingEventBytes==67&&s.PendingEventCount==1&&b.Events[0].Event.Name!.CopyBytes().SequenceEqual(new byte[]{0,128,255}),"Actual key length accounting and ownership");Reject(()=>s.Apply(h2,s.Find(h2)!,Scalar(id+1)),true);Check(ReferenceEquals(b,s.DrainEvents())&&s.IsCurrent(first),"Other actor and retained variable history reservation");var noop=new CreatureView4SupportedDelta(id,0,[new(new CreatureSkillModsView4Update(3,0,0,[]))]);var next=s.Apply(h,first,noop)!;Check(next.Revision==4&&s.PendingEventBytes==67&&!s.Acknowledge(b),"Noop advances global identity without event space");globalTrace=new{ActorA=Row(next.State),ActorB=Row(s.Find(h2)!.State),StoreRevision=s.Revision,ActorAStoreRevision=next.Revision,ActorBStoreRevision=s.Find(h2)!.Revision,RetainedBytes=s.RetainedStateBytes,PendingBytes=s.PendingEventBytes,Queue=s.DrainEvents().Events.Select(e=>new{e.StoreRevision,Event=Event(e.Event)}).ToArray()};Check(s.Acknowledge(s.DrainEvents())&&s.PendingEventBytes==0,"Variable history ack");lifecycle.Add("global-variable-history");
  }
  {
   var r=new SceneObjectRegistry(2,1);var h=Create(r,id);using var s=new SceneCreatureView4SupportedReplicationStore(r,new(MaximumStateBytes:72));var seed=s.Seed(h,Empty(id))!;var b=s.DrainEvents();var grow=Skill(id,0,new byte[]{1},1).Updates[0];var remove=Skill(id,1,new byte[]{1},0).Updates[0];Reject(()=>s.Apply(h,seed,new(id,0,[grow,remove])),true);var late=new CreatureView4SupportedDelta(id,0,[new(new CreatureView4Update(6,1)),new(new CreatureEncumbrancesView4Update(2,1,1,[new(2,99,[1])]))]);Reject(()=>s.Apply(h,seed,late));Check(s.IsCurrent(seed)&&s.Revision==1&&ReferenceEquals(b,s.DrainEvents()),"Intermediate peak and late error atomic");lifecycle.Add("intermediate-late-failure");
  }
  {
   var r=new SceneObjectRegistry(4,1);var h=Create(r,id);using var s=new SceneCreatureView4SupportedReplicationStore(r,new(MaximumObjects:1));s.Seed(h,Empty(id));var b=s.DrainEvents();r.Destroy(1,new(id,0));var other=Create(r,id+1);Reject(()=>s.Seed(other,Empty(id+1)),true);Check(s.Revision==1&&s.Acknowledge(b),"Write failure does not prune or invalidate issued");Check(s.RetainedStateBytes==0,"Read retirement releases capacity");Check(s.Seed(other,Empty(id+1))!.Revision==2,"Capacity after retirement");lifecycle.Add("stale-other-no-write-prune");
  }
  {
   var r=new SceneObjectRegistry(2,1);var h=Create(r,id);using var s=new SceneCreatureView4SupportedReplicationStore(r,revisionSeed:ulong.MaxValue-1);var seed=s.Seed(h,Empty(id))!;var b=s.DrainEvents();Reject(()=>s.Apply(h,seed,Scalar(id)));Reject(()=>s.Seed(h,Empty(id)));Check(s.IsCurrent(seed)&&ReferenceEquals(b,s.DrainEvents()),"Checked global revision atomic");lifecycle.Add("revision-overflow");
  }
  {
   var r=new SceneObjectRegistry(2,1);var h=Create(r,id);using var s=new SceneCreatureView4SupportedReplicationStore(r,new(MaximumPendingEvents:0,MaximumEventsPerApply:0));var seed=s.Seed(h,Empty(id))!;var b=s.DrainEvents();Check(s.Acknowledge(b)&&!s.Acknowledge(b),"Empty receipt once");var plan=new CreatureView4SupportedDelta(id,0,[new(new CreatureSkillModsView4Update(3,0,uint.MaxValue,[]))]);var one=s.Apply(h,seed,plan)!;Check(one.State.Model.SkillMods.Counter==uint.MaxValue&&s.PendingEventCount==0&&one.Revision==2,"Counter-only no event");lifecycle.Add("zero-event-ack");
  }
  {
   var r=new SceneObjectRegistry(2,1);foreach(var l in new[]{new SceneCreatureView4SupportedStoreLimits(MaximumObjects:0),new(MaximumStateBytes:7),new(MaximumPendingEvents:-1),new(MaximumEventsPerApply:262145),new(MaximumOutputBytes:268435457),new(MaximumWork:16777217)})Reject(()=>new SceneCreatureView4SupportedReplicationStore(r,l));Reject(()=>new SceneCreatureView4SupportedReplicationStore(r,stateLimits:new(new(MaximumStringBytes:-1))));lifecycle.Add("invalid-policies");
  }
  File.WriteAllText(output,JsonSerializer.Serialize(new{Checks=checks,Budgets=budgets,Negatives=negatives,Lifecycle=lifecycle,GlobalTrace=globalTrace,Records=records},new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine($"R42_SUPPORTED_STORE_PASS sequences={records.Count} checks={checks} budgets={budgets} negatives={negatives} lifecycle={lifecycle.Count}");
 }
}
