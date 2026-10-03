using System.Text.Json;
using Stardust.Core.Protocol;
using Stardust.Core.Session;
internal static class SceneTangibleView6StoreScenarios
{
 static int checks;
 static void Check(bool v,string why){checks++;if(!v)throw new Exception(why);}
 static void Reject<T>(Action a)where T:Exception{try{a();}catch(T){checks++;return;}throw new Exception("Expected "+typeof(T).Name);}
 static SceneObjectHandle Create(SceneObjectRegistry r,ulong id)=>r.Create(r.SceneGeneration,new(id,0,0,0,1,0,0,0,1,0))!.Handle;
 static object State(TangibleView6State s)=>new{s.ObjectId,s.Passive,s.Counter,Ids=s.Ids.ToArray(),s.Revision,s.DecodedBytes};
 static object Events(SceneTangibleView6EventBatch b)=>b.Events.Select(e=>new{e.StoreRevision,Kind=e.Event.Kind.ToString(),e.Event.Index,e.Event.OldId,e.Event.NewId}).ToArray();
 static TangibleView6Delta Add(ulong id,ushort index=0)=>new(id,65535,new[]{new TangibleView6Update(1,1,new[]{new TangibleView6Command(1,index,new ulong[]{ulong.MaxValue})})});
 internal static void Run(string input,string output)
 {
  checks=0;using var doc=JsonDocument.Parse(File.ReadAllBytes(input));var records=new List<object>();
  foreach(var c in doc.RootElement.GetProperty("Cases").EnumerateArray())
  {
   var baseline=TangibleView6Codec.DecodeBaseline(Convert.FromHexString(c.GetProperty("Baseline").GetString()!),new());var r=new SceneObjectRegistry(4,1);var h=Create(r,baseline.ObjectId);using var store=new SceneTangibleView6ReplicationStore(r);
   var seed=store.Seed(h,baseline)!;Check(store.IsCurrent(seed)&&store.RetainedStateBytes==seed.State.DecodedBytes,"Owned seed/current charge");var steps=new List<object>();var current=seed;
   foreach(var step in c.GetProperty("Steps").EnumerateArray())
   {
    var before=current;var batch=store.DrainEvents();var plan=TangibleView6Codec.DecodeDelta(Convert.FromHexString(step.GetProperty("Hex").GetString()!),new());current=store.Apply(h,current,plan)!;
    Check(!store.IsCurrent(before)&&store.IsCurrent(current)&&!store.Acknowledge(batch),"Exact revision and stale acknowledgement");
    Check(store.Apply(h,before,plan) is null,"Superseded expected receipt cannot overwrite");
    var pending=store.DrainEvents();Check(ReferenceEquals(pending,store.DrainEvents()),"Stable issued journal identity");
    steps.Add(new{State=State(current.State),StoreRevision=current.Revision,Queue=Events(pending)});
   }
   var final=store.DrainEvents();Check(store.Acknowledge(final)&&!store.Acknowledge(final)&&store.PendingEventCount==0,"Exactly once whole journal acknowledgement");records.Add(new{Name=c.GetProperty("Name").GetString(),Initial=State(seed.State),Steps=steps});
  }
  var cases=new List<string>();ulong id=1;var baselineSimple=new TangibleView6Baseline(id,65535,0x80000000,0,new ulong[]{0,ulong.MaxValue});
  foreach(string fault in new[]{"state","queue","events","output","work","revision"})
  {
   var r=new SceneObjectRegistry(4,1);var h=Create(r,id);var limits=new SceneTangibleView6StoreLimits(MaximumStateBytes:fault=="state"?24:4096,MaximumPendingEvents:fault=="queue"?1:4096,MaximumEventsPerApply:fault=="events"?1:4096,MaximumOutputBytes:fault=="output"?24:8192,MaximumWork:fault=="work"?2:65536);
   using var s=new SceneTangibleView6ReplicationStore(r,limits,revisionSeed:fault=="revision"?ulong.MaxValue-1:0);var seed=s.Seed(h,baselineSimple)!;var batch=s.DrainEvents();
   if(fault=="revision")Reject<OverflowException>(()=>s.Apply(h,seed,Add(id)));else Reject<InvalidDataException>(()=>s.Apply(h,seed,Add(id)));
   Check(s.IsCurrent(seed)&&s.Revision==seed.Revision&&ReferenceEquals(s.DrainEvents(),batch)&&s.RetainedStateBytes==24,"Rejected budgets preserve state/journal");cases.Add(fault);
  }
  {
   var r=new SceneObjectRegistry(4,1);var h=Create(r,id);using var s=new SceneTangibleView6ReplicationStore(r);var seed=s.Seed(h,baselineSimple)!;var one=s.Apply(h,seed,Add(id))!;var batch=s.DrainEvents();
   Reject<InvalidDataException>(()=>s.Apply(h,one,new(id+1,0,Add(id).Updates)));Check(ReferenceEquals(batch,s.DrainEvents())&&s.Acknowledge(batch),"Later invalid update does not lose valid queue");
   r.EndBaselines(1,new(id));var now=r.Find(id)!.Handle;Check(!s.IsCurrent(one)&&s.Apply(h,one,null!) is null,"Old exact handle rejects before plan access");
   Check(ReferenceEquals(s.Find(now),one)&&!s.IsCurrent(one),"Find current moved handle returns historical exact stored snapshot");
   var refreshed=s.Apply(now,one,new(id,0,new[]{new TangibleView6Update(123)}))!;Check(s.IsCurrent(refreshed)&&refreshed.Incarnation==one.Incarnation,"Same incarnation new current handle refresh");
   refreshed=s.Apply(now,refreshed,new(id,0,new[]{new TangibleView6Update(1,2,Add(id).Updates[0].Commands)}))!;var historical=s.DrainEvents();Check(historical.Events.Count>0,"Journal retains earlier publication descriptions");
   r.Destroy(1,new(id,0));var recreated=Create(r,id);Check(!r.IsCurrent(historical.Events[0].Incarnation)&&ReferenceEquals(s.DrainEvents().Events[0],historical.Events[0]),"Historical journal recipient is stale; drain does not authorize delivery");Check(s.Apply(recreated,refreshed,Add(id)) is null&&s.Seed(now,null!) is null,"Recreated recipient rejects former incarnation");
   var fresh=s.Seed(recreated,baselineSimple)!;Check(fresh.State.Revision==1&&fresh.Incarnation!=refreshed.Incarnation,"New incarnation fresh state revision");r.Reset();Check(!s.IsCurrent(fresh)&&s.RetainedStateBytes==0,"Reset retires own state");cases.Add("journal-handle-recreate-reset");
  }
  {
   var r=new SceneObjectRegistry(4,1);var h=Create(r,id);using var s=new SceneTangibleView6ReplicationStore(r,new(MaximumObjects:1));var seed=s.Seed(h,baselineSimple)!;var other=Create(r,2);
   Reject<InvalidDataException>(()=>s.Seed(other,new(2,0,0,0,Array.Empty<ulong>())));Check(s.IsCurrent(seed),"Object capacity no prefix");
   using var foreign=new SceneTangibleView6ReplicationStore(r);var foreignSeed=foreign.Seed(h,baselineSimple)!;Check(s.Apply(h,foreignSeed,Add(id)) is null&&!s.Acknowledge(foreign.DrainEvents()),"Foreign owner tokens rejected");
   Exception? error=null;Task.Run(()=>{try{s.Close();}catch(Exception e){error=e;}}).GetAwaiter().GetResult();Check(error is InvalidOperationException&&s.IsCurrent(seed),"Owner rejection before cleanup");
   r.Destroy(1,new(id,0));Reject<InvalidDataException>(()=>s.Seed(other,new(2,0,0,0,Array.Empty<ulong>())));Check(s.RetainedStateBytes==0&&s.Seed(other,new(2,0,0,0,Array.Empty<ulong>())) is not null,"Stale entries remain charged until explicit retirement read");
   s.Close();s.Close();Check(r.IsCurrent(other)&&!s.IsCurrent(seed)&&s.RetainedStateBytes==0&&s.PendingEventCount==0,"Close clears only own state");Reject<ObjectDisposedException>(()=>s.Seed(h,baselineSimple));cases.Add("capacity-foreign-owner-close");
  }
  {
   var r=new SceneObjectRegistry(4,1);var h=Create(r,id);var other=Create(r,2);using var s=new SceneTangibleView6ReplicationStore(r,new(MaximumStateBytes:48));var a=s.Seed(h,baselineSimple)!;s.Seed(other,new(2,0,0,0,new ulong[]{1,2}));
   var batch=s.DrainEvents();Reject<InvalidDataException>(()=>s.Apply(h,a,Add(id)));Check(s.IsCurrent(a)&&ReferenceEquals(batch,s.DrainEvents())&&s.RetainedStateBytes==48,"Global other actor budget included");cases.Add("global-state");
  }
  File.WriteAllText(output,JsonSerializer.Serialize(new{Checks=checks,Sequences=records.Count,Cases=cases,Records=records},new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine($"R31_TANO6_STORE_PASS checks={checks} sequences={records.Count} cases={cases.Count}");
 }
}
