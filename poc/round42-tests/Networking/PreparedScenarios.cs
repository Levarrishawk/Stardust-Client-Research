using System.Reflection;
using Stardust.Core.Protocol;
using Stardust.Core.Session;
using Stardust.Core.SmokeTests;
internal static class PreparedScenarios
{
 static int checks;
 static object? Call(object target,string name,params object?[] args)
 {try{return target.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(target,args);}catch(TargetInvocationException e)when(e.InnerException is not null){System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e.InnerException).Throw();throw;}}
 static object Get(object o,string name)=>o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(o)??throw new Exception("Missing fault-injection field");
 static void Set(object o,string name,object value)=>o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(o,value);
 static void Check(bool b,string why){checks++;if(!b)throw new Exception(why);}
 static void Reject(Action a){try{a();}catch(Exception e)when(e is InvalidOperationException or InvalidDataException or OverflowException or ObjectDisposedException or ArgumentException){checks++;return;}throw new Exception("Expected prepared rejection");}
 public static void Run()
 {
  const ulong id=9;var r=new SceneObjectRegistry(4,1);var h=SceneCreatureView4SupportedStoreScenarios.Create(r,id);using var s=new SceneCreatureView4SupportedReplicationStore(r);using var foreign=new SceneCreatureView4SupportedReplicationStore(r);
  var p=Call(s,"PrepareSeed",h,SceneCreatureView4SupportedStoreScenarios.Empty(id))!;
  try
  {
   Check((ulong)Get(s,"revision")==0&&(long)Get(s,"bytes")==0,"Prepare prebuild no publication");Reject(()=>s.DrainEvents());Reject(()=>s.Close());Reject(()=>Call(s,"PrepareSeed",h,SceneCreatureView4SupportedStoreScenarios.Empty(id)));
   var fp=Call(foreign,"PrepareSeed",h,SceneCreatureView4SupportedStoreScenarios.Empty(id))!;try{Reject(()=>Call(s,"CheckPrepared",fp));Reject(()=>Call(s,"CommitPrepared",fp));Reject(()=>Call(s,"ReleasePrepared",fp));Call(s,"CheckPrepared",p);}finally{Call(foreign,"ReleasePrepared",fp);}
   var clone=typeof(object).GetMethod("MemberwiseClone",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(p,null)!;Reject(()=>Call(s,"CommitPrepared",clone));Reject(()=>Call(s,"ReleasePrepared",clone));
   Exception? wrong=null;Task.Run(()=>{try{Call(s,"ReleasePrepared",p);}catch(Exception e){wrong=e;}}).GetAwaiter().GetResult();Check(wrong is InvalidOperationException,"Wrong-thread release does not unlock");
   Call(s,"CheckPrepared",p);Call(s,"CommitPrepared",p);Reject(()=>Call(s,"CommitPrepared",p));Reject(()=>Call(s,"CheckPrepared",p));
  }finally{Call(s,"ReleasePrepared",p);}
  Check(s.Revision==1&&s.Find(h) is not null,"Committed exact prepared seed");Reject(()=>Call(s,"CommitPrepared",p));Reject(()=>Call(s,"ReleasePrepared",p));
  var snapshot=s.Find(h)!;var issued=s.DrainEvents();var cancel=Call(s,"PrepareApply",h,snapshot,SceneCreatureView4SupportedStoreScenarios.Scalar(id))!;try{Call(s,"CheckPrepared",cancel);}finally{Call(s,"ReleasePrepared",cancel);}Check(s.IsCurrent(snapshot)&&ReferenceEquals(issued,s.DrainEvents())&&s.Revision==1,"Cancellation retains map journal epoch");Reject(()=>Call(s,"CommitPrepared",cancel));
  foreach(string fault in new[]{"move","recreate","reset"})
  {
   var rr=new SceneObjectRegistry(4,1);var hh=SceneCreatureView4SupportedStoreScenarios.Create(rr,id);using var ss=new SceneCreatureView4SupportedReplicationStore(rr);var candidate=Call(ss,"PrepareSeed",hh,SceneCreatureView4SupportedStoreScenarios.Empty(id))!;
   try{if(fault=="move")rr.EndBaselines(1,new(id));else if(fault=="recreate")rr.Replace(1,rr.Find(id)!.Create);else rr.Reset();Reject(()=>Call(ss,"CheckPrepared",candidate));Reject(()=>Call(ss,"CommitPrepared",candidate));Check((ulong)Get(ss,"revision")==0&&(long)Get(ss,"bytes")==0,"Stale commit no prefix");}finally{Call(ss,"ReleasePrepared",candidate);}
   Check(ss.Revision==0&&ss.PendingEventCount==0,"Stale release restored gate");
  }
  {
   var rr=new SceneObjectRegistry(2,1);var hh=SceneCreatureView4SupportedStoreScenarios.Create(rr,id);using var ss=new SceneCreatureView4SupportedReplicationStore(rr);Set(ss,"epoch",ulong.MaxValue);Reject(()=>ss.Seed(hh,SceneCreatureView4SupportedStoreScenarios.Empty(id)));Check(ss.Revision==0&&(long)Get(ss,"bytes")==0,"Seed epoch overflow before publication");Set(ss,"epoch",0UL);var seed=ss.Seed(hh,SceneCreatureView4SupportedStoreScenarios.Empty(id))!;var current=ss.Apply(hh,seed,SceneCreatureView4SupportedStoreScenarios.Scalar(id))!;Set(ss,"epoch",ulong.MaxValue);var batch=ss.DrainEvents();Reject(()=>ss.Acknowledge(batch));Check(ReferenceEquals(batch,ss.DrainEvents())&&ss.PendingEventBytes==48&&ss.IsCurrent(current),"Ack epoch overflow retains receipt");rr.Destroy(1,new(id,0));Reject(()=>{_ =ss.RetainedStateBytes;});Check((long)Get(ss,"bytes")==72&&(ulong)Get(ss,"revision")==2,"Retirement epoch overflow prebuild atomic");Set(ss,"epoch",2UL);Check(ss.RetainedStateBytes==0&&ss.PendingEventBytes==48,"Epoch fault recovery explicit retirement");
  }
  {
   var rr=new SceneObjectRegistry(2,1);var hh=SceneCreatureView4SupportedStoreScenarios.Create(rr,id);using var ss=new SceneCreatureView4SupportedReplicationStore(rr);var seed=ss.Seed(hh,SceneCreatureView4SupportedStoreScenarios.Empty(id))!;Reject(()=>Call(ss,"PrepareApply",hh,seed,new CreatureView4SupportedDelta(id+1,0,SceneCreatureView4SupportedStoreScenarios.Scalar(id).Updates)));Check(ss.IsCurrent(seed),"Failed preparation releases gate");ss.Close();Reject(()=>Call(ss,"PrepareSeed",hh,SceneCreatureView4SupportedStoreScenarios.Empty(id)));Check(ss.Revision==1&&ss.PendingEventCount==0,"Closed prepare fails and releases");
  }
  Console.WriteLine($"R42_PREPARED_SEPARATE_ASSEMBLY_PASS checks={checks} groups=7");
 }
}
