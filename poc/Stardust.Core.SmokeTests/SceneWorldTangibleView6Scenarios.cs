using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;
internal static class SceneWorldTangibleView6Scenarios
{
 static int checks;static void Check(bool value,string why){checks++;if(!value)throw new Exception(why);}
 static void Reject<T>(Action action)where T:Exception{try{action();}catch(T){checks++;return;}throw new Exception("Expected "+typeof(T).Name);}
 static readonly string path="object/synthetic_round32.iff",pin=new('0',64);
 static readonly uint crc=LegacyAssetPath.Checksum(path);
 static ObjectTemplateCatalog Catalog()=>new(new[]{new ObjectTemplateCatalogEntry(crc,path,pin,pin,SceneObjectResourceRecipe.StaticAppearance)});
 static SceneWorldTangibleView6Policy Policy()=>new(new(),new(),new());
 static SceneWorldCoordinator World(SceneWorldTangibleView6Policy? policy=null,SceneWorldChangePolicy? changes=null,bool typed=true,Func<SceneObjectResourceRequest,CancellationToken,Task<SceneObjectResources>>? loader=null)=>new(Catalog(),new Dictionary<SceneObjectResourceRecipe,SceneObjectResourceOptions>{{SceneObjectResourceRecipe.StaticAppearance,new SceneStaticResourceOptions(SceneStaticMaterialPolicy.AllowNeutral,null)}},loader??((_,_)=>throw new Exception("Unexpected synthetic loader call")),SceneCreateZeroQuaternionPolicy.Reject,8,2,32,65536,32,65536,65536,changePolicy:changes,tangibleView6Policy:typed?(policy??Policy()):null);
 static byte[] Start(ulong id){using var s=new MemoryStream();using var w=new BinaryWriter(s);w.Write((ushort)9);w.Write(ZoneSceneProtocolCodec.CmdStartSceneOpcode);w.Write((byte)0);w.Write(id);w.Write((ushort)0);w.Write(0f);w.Write(0f);w.Write(0f);w.Write((ushort)0);w.Write(0UL);return s.ToArray();}
 static byte[] Create(ulong id)=>SceneObjectLifecycleCodec.EncodeCreate(new(id,0,0,0,1,0,0,0,crc,0));
 static TangibleView6Baseline Baseline(ulong id)=>new(id,65535,0x80000000,0,new ulong[]{0,ulong.MaxValue});
 static TangibleView6Delta Add(ulong id,uint counter=1)=>new(id,65535,new[]{new TangibleView6Update(1,counter,new[]{new TangibleView6Command(1,0,new ulong[]{ulong.MaxValue})})});
 static long Stage(SceneWorldCoordinator w,byte[] baseline,bool missing=false)
 {
  ulong id=ReplicationEnvelopeCodec.DecodeBaseline(baseline,65536).ObjectId;long g=w.BeginSelection(id).Generation;Check(w.Accept(g,Start(id)),"Start selection");Check(w.Accept(g,Create(id)),"Stage create");if(!missing)Check(w.Accept(g,baseline),"Stage baseline");Check(w.Accept(g,SceneObjectLifecycleCodec.EncodeEndBaselines(new(id))),"Stage end");Check(w.CompleteMinimumSceneBarrier(g,true) is not null,"Serialized replay barrier");return g;
 }
 static SceneWorldTangibleView6Snapshot Current(SceneWorldCoordinator w,ulong id)=>w.FindTangibleView6(w.Generation,id)!;
 static object State(TangibleView6State s)=>new{s.ObjectId,s.Passive,s.Counter,Ids=s.Ids.ToArray(),s.Revision,s.DecodedBytes};
 static object Events(SceneWorldTangibleView6EventBatch b)=>b.Events.Select(e=>new{e.StoreRevision,Kind=e.Event.Kind.ToString(),e.Event.Index,e.Event.OldId,e.Event.NewId}).ToArray();
 internal static void Run(string fixture,string output)
 {
  checks=0;using var doc=JsonDocument.Parse(File.ReadAllBytes(fixture));var records=new List<object>();var cases=new List<string>();
  foreach(var c in doc.RootElement.GetProperty("Cases").EnumerateArray())using(var w=World())
  {
   var wire=Convert.FromHexString(c.GetProperty("Baseline").GetString()!);var baseline=TangibleView6Codec.DecodeBaseline(wire,new());long g=Stage(w,wire);ulong id=baseline.ObjectId;var seed=Current(w,id);
   Check(w.IsCurrent(seed)&&seed.Incarnation==w.Snapshots()[0].Object.Incarnation&&w.Snapshots()[0].TangibleView6 is not null&&w.Snapshots()[0].Envelopes.Count==0,"Typed current incarnation snapshot without opaque history");
   Check(w.AcknowledgeChanges(w.DrainChanges(g)!),"Initial journal ack");var steps=new List<object>();using var router=new SceneWorldTangibleView6ApplicationRouter(w);
   foreach(var step in c.GetProperty("Steps").EnumerateArray())
   {
    var before=Current(w,id);var oldEvents=w.DrainTangibleView6Events(g)!;var bytes=Convert.FromHexString(step.GetProperty("Hex").GetString()!);Check(router.Accept(g,bytes),"Exact ordered envelope route");Array.Clear(bytes);
    var value=Current(w,id);Check(!w.IsCurrent(before)&&w.IsCurrent(value)&&!w.AcknowledgeTangibleView6Events(oldEvents),"Supersession and old journal reject");
    Check(!w.ApplyTangibleView6Delta(g,before,null!),"Stale receipt before plan access");var batch=w.DrainChanges(g)!;Check(batch.Entries.Count==1&&batch.Entries[0].Current!.TangibleView6!.Revision==value.Revision,"World publication contains committed state");
    var events=w.DrainTangibleView6Events(g)!;Check(w.RetainedTangibleView6Bytes==value.State.DecodedBytes+24L*events.Events.Count,"State plus journal logical charge");steps.Add(new{State=State(value.State),StoreRevision=value.Revision,Queue=Events(events)});
   }
   var final=w.DrainTangibleView6Events(g)!;Check(w.AcknowledgeTangibleView6Events(final)&&!w.AcknowledgeTangibleView6Events(final),"Exactly once event ownership ack");records.Add(new{Name=c.GetProperty("Name").GetString(),Initial=State(seed.State),Steps=steps});
  }
  const ulong target=7;var initial=TangibleView6Codec.EncodeBaseline(Baseline(target),new());
  foreach(string fault in new[]{"state","events","queue","output","work","store-revision"})using(var w=World(Policy() with {StoreLimits=new(MaximumStateBytes:fault=="state"?24:4096,MaximumPendingEvents:fault=="queue"?1:4096,MaximumEventsPerApply:fault=="events"?1:4096,MaximumOutputBytes:fault=="output"?24:8192,MaximumWork:fault=="work"?2:65536),RevisionSeed=fault=="store-revision"?ulong.MaxValue-1:0}))
  {
   long g=Stage(w,initial);var before=Current(w,target);var batch=w.DrainChanges(g)!;var events=w.DrainTangibleView6Events(g)!;
   if(fault=="store-revision")Reject<OverflowException>(()=>w.ApplyTangibleView6Delta(g,before,Add(target)));else Reject<InvalidDataException>(()=>w.ApplyTangibleView6Delta(g,before,Add(target)));
   Check(w.IsCurrent(before)&&w.IsCurrent(batch)&&w.RetainedTangibleView6Bytes==24&&w.State==ZoneSceneTransitionState.AwaitingServerReady,"Budget failure preserves world/state/journal/phase");Check(w.AcknowledgeTangibleView6Events(events),"Failed candidate preserves issued store batch");cases.Add(fault);
  }
  using(var w=World(changes:new(1)))
  {long g=Stage(w,initial);w.AcknowledgeChanges(w.DrainChanges(g)!);w.Accept(g,Create(target+1));var before=Current(w,target);var batch=w.DrainChanges(g)!;var events=w.DrainTangibleView6Events(g)!;Reject<InvalidDataException>(()=>w.ApplyTangibleView6Delta(g,before,Add(target)));Check(w.IsCurrent(before)&&w.IsCurrent(batch)&&w.AcknowledgeTangibleView6Events(events),"World journal cap before store commit");cases.Add("world-journal");}
  using(var w=World(changes:new(8,ulong.MaxValue-3)))
  {long g=Stage(w,initial);var before=Current(w,target);var batch=w.DrainChanges(g)!;Reject<OverflowException>(()=>w.ApplyTangibleView6Delta(g,before,Add(target)));Check(w.IsCurrent(before)&&w.IsCurrent(batch),"World overflow no prefix");cases.Add("world-revision");}
  using(var w=World(Policy() with {StoreLimits=new(MaximumStateBytes:48)}))
  {long g=Stage(w,initial);w.Accept(g,Create(target+1));w.Accept(g,TangibleView6Codec.EncodeBaseline(Baseline(target+1),new()));var before=Current(w,target);var batch=w.DrainChanges(g)!;Reject<InvalidDataException>(()=>w.ApplyTangibleView6Delta(g,before,Add(target)));Check(w.IsCurrent(before)&&w.IsCurrent(batch)&&w.RetainedTangibleView6Bytes==48,"Other world actor charge included");cases.Add("global-state");}
  using(var w=World())
  {
   long g=Stage(w,initial);var seed=Current(w,target);Check(w.ApplyTangibleView6Delta(g,seed,Add(target)),"Valid earlier update");var before=Current(w,target);var events=w.DrainTangibleView6Events(g)!;var worldBatch=w.DrainChanges(g)!;
   var malformed=TangibleView6Codec.EncodeDelta(Add(target,2),new());malformed[25]=2;Reject<InvalidDataException>(()=>w.Accept(g,malformed));Check(w.IsCurrent(before)&&w.IsCurrent(worldBatch)&&events.Events.Count==2&&w.IsCurrentTangibleView6Event(events,0),"Later invalid exact delta retains valid earlier work");
   w.Accept(g,SceneObjectLifecycleCodec.EncodeContainment(new(target,0,0)));Check(w.IsCurrent(before)&&w.ApplyTangibleView6Delta(g,before,new(target,0,new[]{new TangibleView6Update(123)})),"Moved same incarnation carries state");var moved=Current(w,target);
   w.Accept(g,SceneObjectLifecycleCodec.EncodeDestroy(new(target,0)));Check(!w.IsCurrent(moved)&&!w.IsCurrentTangibleView6Event(events,0)&&w.DrainTangibleView6Events(g)!.Events.Count==2,"Removed recipient historical events retained but delivery blocked");
   w.Accept(g,Create(target));w.Accept(g,initial);w.Accept(g,SceneObjectLifecycleCodec.EncodeEndBaselines(new(target)));var fresh=Current(w,target);Check(fresh.Incarnation!=moved.Incarnation&&fresh.State.Revision==1&&!w.ApplyTangibleView6Delta(g,moved,null!),"Recreated recipient rejects old receipt");
   var historical=w.DrainTangibleView6Events(g)!;Check(!w.IsCurrentTangibleView6Event(historical,0)&&w.AcknowledgeTangibleView6Events(historical),"Current historical journal may ack but not deliver to recreated target");
   Check(!w.ApplyTangibleView6Delta(g-1,null!,null!),"Generation before receipt access");w.BeginSelection(target);Check(!w.IsCurrent(fresh)&&!w.AcknowledgeTangibleView6Events(historical)&&w.RetainedTangibleView6Bytes==0,"Generation retires own state/journal should clear");cases.Add("history-move-recreate-generation");
  }
  using(var a=World())using(var b=World())
  {long ga=Stage(a,initial),gb=Stage(b,initial);Check(!a.ApplyTangibleView6Delta(ga,Current(b,target),null!)&&!a.AcknowledgeTangibleView6Events(b.DrainTangibleView6Events(gb)!),"Foreign world receipts reject");Exception? error=null;Task.Run(()=>{try{a.Close();}catch(Exception e){error=e;}}).GetAwaiter().GetResult();Check(error is InvalidOperationException&&a.IsCurrent(Current(a,target)),"Owner rejection before close");using(var router=new SceneWorldTangibleView6ApplicationRouter(a)){router.Dispose();Check(a.IsCurrent(Current(a,target)),"Router disposal leaves borrowed world");}a.Close();Check(a.RetainedTangibleView6Bytes==0&&a.Snapshots().Count==0,"Close clears owned state");Reject<ObjectDisposedException>(()=>a.ApplyTangibleView6Delta(ga,null!,null!));cases.Add("foreign-owner-close");}
  foreach(string fault in new[]{"null","id","count","world-mutation","ack","router-reentry","dispose","generation"})using(var w=World())
  {
   long g=Stage(w,initial);var before=Current(w,target);var batch=w.DrainChanges(g)!;SceneWorldTangibleView6ApplicationRouter? router=null;router=new(w,e=>
   {if(fault=="null")return null!;if(fault=="world-mutation")w.ApplyTangibleView6Delta(g,before,Add(target));if(fault=="ack")w.AcknowledgeChanges(batch);if(fault=="generation")w.BeginSelection(target);
    if(fault=="router-reentry"){try{router!.Accept(g,TangibleView6Codec.EncodeDelta(Add(target),new()));}catch(InvalidOperationException){}}if(fault=="dispose"){try{router!.Dispose();}catch(InvalidOperationException){}}
    return fault=="id"?Add(target+1):fault=="count"?new(target,0,Add(target).Updates):Add(target);});
   if(fault is "null" or "id" or "count")Reject<InvalidDataException>(()=>router.Accept(g,TangibleView6Codec.EncodeDelta(Add(target),new())));else Reject<InvalidOperationException>(()=>router.Accept(g,TangibleView6Codec.EncodeDelta(Add(target),new())));
   if(fault is not ("world-mutation" or "generation"))Check(w.IsCurrent(before),"Callback rejection no candidate publication");Reject<ObjectDisposedException>(()=>router.Accept(g,initial));router.Dispose();cases.Add("callback-"+fault);
  }
  foreach(string fault in new[]{"wrong-family","pending-target","router-malformed","router-budget","router-owner"})using(var w=World())
  {
   long g=Stage(w,initial);var before=Current(w,target);var batch=w.DrainChanges(g)!;
   if(fault=="wrong-family"){var invalid=TangibleView6Codec.EncodeDelta(Add(target),new());invalid[14]=0;Reject<InvalidDataException>(()=>w.Accept(g,invalid));Check(w.Snapshots().Count==0&&w.State==ZoneSceneTransitionState.Failed,"Wrong family preserves existing generation-fatal envelope policy");}
   else if(fault=="pending-target"){w.Accept(g,Create(target+1));w.Accept(g,TangibleView6Codec.EncodeBaseline(Baseline(target+1),new()));var pending=Current(w,target+1);Reject<InvalidDataException>(()=>w.ApplyTangibleView6Delta(g,pending,Add(target+1)));Check(w.IsCurrent(before)&&w.IsCurrent(pending),"Pending target cannot consume delta");}
   else
   {
    using var router=new SceneWorldTangibleView6ApplicationRouter(w,maximumPayloadBytes:fault=="router-budget"?2:65536);
    if(fault=="router-owner"){Exception? error=null;Task.Run(()=>{try{router.Accept(g,TangibleView6Codec.EncodeDelta(Add(target),new()));}catch(Exception e){error=e;}}).GetAwaiter().GetResult();Check(error is InvalidOperationException&&router.Accept(g,TangibleView6Codec.EncodeDelta(Add(target),new())),"Router owner rejection preserves later valid access");}
    else {var invalid=TangibleView6Codec.EncodeDelta(Add(target),new());if(fault=="router-malformed")invalid[25]=2;Reject<InvalidDataException>(()=>router.Accept(g,invalid));Check(w.IsCurrent(before)&&w.IsCurrent(batch),"Malformed/budget ingress leaves world intact");}
   }
   cases.Add(fault);
  }
  using(var w=World(typed:false)){long g=Stage(w,initial);Check(!w.TangibleView6Enabled&&w.Snapshots()[0].TangibleView6 is null&&w.Snapshots()[0].Envelopes.Count==1,"Default opaque mode unchanged");cases.Add("default-opaque");}
  using(var w=World()){Reject<InvalidDataException>(()=>Stage(w,initial,true));Check(w.Snapshots().Count==0&&w.RetainedTangibleView6Bytes==0,"Missing baseline end barrier drains generation");cases.Add("missing-seed");}
  {
   var held=new TaskCompletionSource<SceneObjectResources>(TaskCreationOptions.RunContinuationsAsynchronously);CancellationToken token=default;using var w=World(loader:(_,t)=>{token=t;return held.Task;});long g=Stage(w,initial);w.PollObjectResources();Check(w.OutstandingJobs==1,"Owned held synthetic job");w.Close();Check(token.IsCancellationRequested&&!w.DrainCompletion.IsCompleted,"Close cancels and exposes pending drain");held.SetException(new InvalidDataException("Synthetic late fault"));Check(w.DrainCompletion.Wait(TimeSpan.FromSeconds(5))&&w.OutstandingJobs==0&&w.RetainedTangibleView6Bytes==0,"Late completion drains without resurrection");cases.Add("cancel-drain");
  }
  File.WriteAllText(output,JsonSerializer.Serialize(new{Checks=checks,Sequences=records.Count,Cases=cases,Records=records},new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine($"R32_TANO6_WORLD_PASS checks={checks} sequences={records.Count} cases={cases.Count}");
 }
}
