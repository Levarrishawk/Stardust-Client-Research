using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;
internal static class SceneWorldCreatureView6CompositeScenarios
{
 static int checks;static void Check(bool value,string why){checks++;if(!value)throw new Exception(why);}
 static void Reject<T>(Action action)where T:Exception{try{action();}catch(T){checks++;return;}throw new Exception("Expected "+typeof(T).Name);}
 static readonly string path="object/synthetic_round32.iff",pin=new('0',64);
 static readonly uint crc=LegacyAssetPath.Checksum(path);
 static ObjectTemplateCatalog Catalog()=>new(new[]{new ObjectTemplateCatalogEntry(crc,path,pin,pin,SceneObjectResourceRecipe.CreatureDetail0)});
 static CreatureView6CompositeStateLimits Wide()=>new(new(MaximumStringBytes:1048576,MaximumTotalStringBytes:4194304,MaximumElements:65536,MaximumTotalElements:262144,MaximumStateBytes:16777216,MaximumEvents:262144,MaximumOutputBytes:33554432,MaximumWork:16777216),new(MaximumStringBytes:1048576,MaximumTotalStringBytes:4194304,MaximumWork:16777216));
 static SceneWorldCreatureView6CompositePolicy Policy()=>new(new(),new(MaximumStringBytes:1048576,MaximumTotalStringBytes:4194304,MaximumWork:16777216),Wide(),new(MaximumStateBytes:134217728,MaximumOutputBytes:268435456,MaximumWork:16777216));
 static readonly LogicalAnimationClipSelection idle=new("appearance/lat/synthetic.lat","synthetic_idle",pin);
 static readonly IReadOnlyDictionary<string,LogicalAnimationClipSelection> movement=new Dictionary<string,LogicalAnimationClipSelection>{{"walk",idle},{"run",idle}};
 static SceneWorldCoordinator World(SceneWorldCreatureView6CompositePolicy? policy=null,SceneWorldChangePolicy? changes=null,bool typed=true,Func<SceneObjectResourceRequest,CancellationToken,Task<SceneObjectResources>>? loader=null)=>new(Catalog(),new Dictionary<SceneObjectResourceRecipe,SceneObjectResourceOptions>{{SceneObjectResourceRecipe.CreatureDetail0,new SceneCreatureResourceOptions(idle,movement,false)}},loader??((_,_)=>throw new Exception("Unexpected synthetic loader call")),SceneCreateZeroQuaternionPolicy.Reject,8,2,32,1048576,32,1048576,1048576,changePolicy:changes,creatureView6CompositePolicy:typed?(policy??Policy()):null);
 static byte[] Start(ulong id){using var s=new MemoryStream();using var w=new BinaryWriter(s);w.Write((ushort)9);w.Write(ZoneSceneProtocolCodec.CmdStartSceneOpcode);w.Write((byte)0);w.Write(id);w.Write((ushort)0);w.Write(0f);w.Write(0f);w.Write(0f);w.Write((ushort)0);w.Write(0UL);return s.ToArray();}
 static byte[] Create(ulong id)=>SceneObjectLifecycleCodec.EncodeCreate(new(id,0,0,0,1,0,0,0,crc,0));
 static byte[] emptyWire=Array.Empty<byte>();
 static byte[] Relabel(byte[] wire,ulong id){var copy=wire.ToArray();System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(copy.AsSpan(6),id);return copy;}
 static CreatureView6Baseline Baseline(ulong id)=>CreatureView6Codec.DecodeBaseline(Relabel(emptyWire,id),new());
 static CreatureView6CompositeDelta Add(ulong id,uint ignored=1)=>new(id,65535,new[]{new CreatureView6CompositeUpdate(CreatureView6Update.String(3,ReplicationByteString.Create(new byte[]{255})))});
 static long Stage(SceneWorldCoordinator w,byte[] baseline,bool missing=false)
 {
  ulong id=ReplicationEnvelopeCodec.DecodeBaseline(baseline,1048576).ObjectId;long g=w.BeginSelection(id).Generation;Check(w.Accept(g,Start(id)),"Start selection");Check(w.Accept(g,Create(id)),"Stage create");if(!missing)Check(w.Accept(g,baseline),"Stage baseline");Check(w.Accept(g,SceneObjectLifecycleCodec.EncodeEndBaselines(new(id))),"Stage end");Check(w.CompleteMinimumSceneBarrier(g,true) is not null,"Serialized replay barrier");return g;
 }
 static SceneWorldCreatureView6CompositeSnapshot Current(SceneWorldCoordinator w,ulong id)=>w.FindCreatureView6Composite(w.Generation,id)!;
 static object Text(ReplicationByteString s)=>new{Hex=Convert.ToHexString(s.CopyBytes()),Extended=s.UsesExtendedLength};
 static object Vector<T>(CreatureView6Vector<T> v)=>new{v.Counter,Values=v.Values.ToArray()};
 static object Model(CreatureView6Baseline s)=>new{s.ObjectId,s.DeclaredCount,s.Passive,Defenders=Vector(s.Defenders),s.Level,PerformanceAnimation=Text(s.PerformanceAnimation),MoodString=Text(s.MoodString),s.WeaponId,s.GroupId,s.InviterId,s.InviteCounter,s.Guild,s.TargetId,s.MoodId,s.PerformanceStart,s.PerformanceType,Ham=Vector(s.Ham),MaximumHam=Vector(s.MaximumHam),Wearables=new{s.Wearables.Counter,Values=s.Wearables.Values.Select(w=>new{Customization=Text(w.Customization),w.Arrangement,w.ObjectId,w.TemplateCrc}).ToArray()},AlternateAppearance=Text(s.AlternateAppearance),s.Frozen};
 static object State(CreatureView6CompositeState s)=>new{Model=Model(s.Model),s.Revision,s.DecodedBytes};
 static object Events(SceneWorldCreatureView6CompositeEventBatch b)=>b.Events.Select(e=>new{e.StoreRevision,Kind=e.Event.Kind.ToString(),e.Event.Field,e.Event.UpdateOrdinal,e.Event.Opcode,e.Event.ElementIndex,e.Event.OldValue,e.Event.NewValue}).ToArray();
 static byte[] Baseline3(ulong id,bool creature)
 {
  using var stream=new MemoryStream();using var b=new BinaryWriter(stream);b.Write(1f);b.Write((ushort)1);b.Write((byte)65);b.Write(0U);b.Write((ushort)1);b.Write((byte)66);b.Write(2U);b.Write((ushort)67);b.Write((ushort)68);b.Write(1U);b.Write((ushort)0);b.Write(0U);b.Write(0U);b.Write(uint.MaxValue);b.Write(2U);b.Write(3U);b.Write(4U);b.Write((byte)1);
  if(creature){b.Write((byte)2);b.Write((byte)3);b.Write(1UL<<63);b.Write(1.25f);b.Write(0U);b.Write(ulong.MaxValue);b.Write(0U);b.Write(0U);}
  return ReplicationEnvelopeCodec.EncodeBaseline(new(ReplicationEnvelopeKind.Baseline,id,creature?CreatureView3BaselineCodec.RawTag:TangibleView3BaselineCodec.RawTag,3,0,stream.ToArray(),65536),65536);
 }
 internal static void Run(string fixture,string output)
 {
  checks=0;using var doc=JsonDocument.Parse(File.ReadAllBytes(fixture));var records=new List<object>();var cases=new List<string>();emptyWire=Convert.FromHexString(doc.RootElement.GetProperty("EmptyBaseline").GetString()!);
  foreach(var c in doc.RootElement.GetProperty("Cases").EnumerateArray())using(var w=World())
  {
   var baselineBytes=Convert.FromHexString(c.GetProperty("Baseline").GetString()!);var baseline=CreatureView6Codec.DecodeBaseline(baselineBytes,new());long g=Stage(w,baselineBytes);ulong id=baseline.ObjectId;var seed=Current(w,id);
   Check(w.IsCurrent(seed)&&seed.Incarnation==w.Snapshots()[0].Object.Incarnation&&w.Snapshots()[0].CreatureView6Composite is not null&&w.Snapshots()[0].Envelopes.Count==0,"Typed full model without opaque history");
   Check(w.AcknowledgeChanges(w.DrainChanges(g)!),"Initial journal ack");var oldEvents=w.DrainCreatureView6CompositeEvents(g)!;var wire=Convert.FromHexString(c.GetProperty("Hex").GetString()!);var plan=CreatureView6CompositeCodec.Decode(wire,new(MaximumStringBytes:1048576,MaximumTotalStringBytes:4194304,MaximumWork:16777216));var expected=seed.State.Apply(plan,Wide());
   using var router=new SceneWorldCreatureView6CompositeApplicationRouter(w,maximumPayloadBytes:1048576);Check(router.Accept(g,wire),"Ordered scalar/HAM wire ingress");Array.Clear(wire);var current=Current(w,id);
   Check(!w.IsCurrent(seed)&&w.IsCurrent(current)&&!w.AcknowledgeCreatureView6CompositeEvents(oldEvents)&&!w.ApplyCreatureView6CompositeDelta(g,seed,null!),"Exact receipt supersession and stale journal");var batch=w.DrainChanges(g)!;
   Check(batch.Entries.Count==1&&batch.Entries[0].Current!.CreatureView6Composite!.Revision==current.Revision,"World batch contains committed composite");var events=w.DrainCreatureView6CompositeEvents(g)!;
   Check(w.RetainedCreatureView6CompositeBytes==current.State.DecodedBytes+24L*events.Events.Count&&w.RetainedCreatureView6Bytes==0,"Separate global logical partition");for(int i=0;i<events.Events.Count;i++)Check(w.IsCurrentCreatureView6CompositeEvent(events,i),"Current finalized event guard");
   records.Add(new{Name=c.GetProperty("Name").GetString(),Initial=State(seed.State),State=State(current.State),StoreRevision=current.Revision,Queue=Events(events),expected.WorkUsed});
   Check(w.AcknowledgeCreatureView6CompositeEvents(events)&&!w.AcknowledgeCreatureView6CompositeEvents(events),"Exactly once journal ownership");
  }
  const ulong target=7;var initial=CreatureView6Codec.EncodeBaseline(Baseline(target),new());
  foreach(string fault in new[]{"state","events","queue","output","work","store-revision"})using(var w=World(Policy() with {StoreLimits=new(MaximumStateBytes:fault=="state"?108:4096,MaximumPendingEvents:fault=="queue"?0:4096,MaximumEventsPerApply:fault=="events"?0:4096,MaximumOutputBytes:fault=="output"?108:8192,MaximumWork:fault=="work"?18:1048576),RevisionSeed=fault=="store-revision"?ulong.MaxValue-1:0}))
  {
   long g=Stage(w,initial);var before=Current(w,target);var batch=w.DrainChanges(g)!;var events=w.DrainCreatureView6CompositeEvents(g)!;
   if(fault=="store-revision")Reject<OverflowException>(()=>w.ApplyCreatureView6CompositeDelta(g,before,Add(target)));else Reject<InvalidDataException>(()=>w.ApplyCreatureView6CompositeDelta(g,before,Add(target)));
   Check(w.IsCurrent(before)&&w.IsCurrent(batch)&&w.RetainedCreatureView6CompositeBytes==108&&w.State==ZoneSceneTransitionState.AwaitingServerReady,"Budget failure preserves world/state/journal/phase");Check(w.AcknowledgeCreatureView6CompositeEvents(events),"Failed candidate preserves issued store batch");cases.Add(fault);
  }
  using(var w=World(changes:new(1)))
  {long g=Stage(w,initial);w.AcknowledgeChanges(w.DrainChanges(g)!);w.Accept(g,Create(target+1));var before=Current(w,target);var batch=w.DrainChanges(g)!;var events=w.DrainCreatureView6CompositeEvents(g)!;Reject<InvalidDataException>(()=>w.ApplyCreatureView6CompositeDelta(g,before,Add(target)));Check(w.IsCurrent(before)&&w.IsCurrent(batch)&&w.AcknowledgeCreatureView6CompositeEvents(events),"World journal cap before store commit");cases.Add("world-journal");}
  using(var w=World(changes:new(8,ulong.MaxValue-3)))
  {long g=Stage(w,initial);var before=Current(w,target);var batch=w.DrainChanges(g)!;Reject<OverflowException>(()=>w.ApplyCreatureView6CompositeDelta(g,before,Add(target)));Check(w.IsCurrent(before)&&w.IsCurrent(batch),"World overflow no prefix");cases.Add("world-revision");}
  using(var w=World(Policy() with {StoreLimits=new(MaximumStateBytes:216)}))
  {long g=Stage(w,initial);w.Accept(g,Create(target+1));w.Accept(g,CreatureView6Codec.EncodeBaseline(Baseline(target+1),new()));var before=Current(w,target);var batch=w.DrainChanges(g)!;Reject<InvalidDataException>(()=>w.ApplyCreatureView6CompositeDelta(g,before,Add(target)));Check(w.IsCurrent(before)&&w.IsCurrent(batch)&&w.RetainedCreatureView6CompositeBytes==216,"Other world actor charge included");cases.Add("global-state");}
  using(var w=World())
  {
   long g=Stage(w,initial);var seed=Current(w,target);Check(w.ApplyCreatureView6CompositeDelta(g,seed,Add(target)),"Valid earlier update");var before=Current(w,target);var events=w.DrainCreatureView6CompositeEvents(g)!;var worldBatch=w.DrainChanges(g)!;
   var malformed=CreatureView6CompositeCodec.Encode(Add(target,2),new());malformed[25]=1;Reject<InvalidDataException>(()=>w.Accept(g,malformed));Check(w.IsCurrent(before)&&w.IsCurrent(worldBatch)&&events.Events.Count==1&&w.IsCurrentCreatureView6CompositeEvent(events,0),"Later invalid exact delta retains valid earlier work");
   w.Accept(g,SceneObjectLifecycleCodec.EncodeContainment(new(target,0,0)));Check(w.IsCurrent(before)&&w.ApplyCreatureView6CompositeDelta(g,before,new(target,0,new[]{new CreatureView6CompositeUpdate(CreatureView6Update.Scalar(0,123))})),"Moved same incarnation carries state");var moved=Current(w,target);
   w.Accept(g,SceneObjectLifecycleCodec.EncodeDestroy(new(target,0)));Check(!w.IsCurrent(moved)&&!w.IsCurrentCreatureView6CompositeEvent(events,0)&&w.DrainCreatureView6CompositeEvents(g)!.Events.Count==2,"Removed recipient historical events retained but delivery blocked");
   w.Accept(g,Create(target));w.Accept(g,initial);w.Accept(g,SceneObjectLifecycleCodec.EncodeEndBaselines(new(target)));var fresh=Current(w,target);Check(fresh.Incarnation!=moved.Incarnation&&fresh.State.Revision==1&&!w.ApplyCreatureView6CompositeDelta(g,moved,null!),"Recreated recipient rejects old receipt");
   var historical=w.DrainCreatureView6CompositeEvents(g)!;Check(!w.IsCurrentCreatureView6CompositeEvent(historical,0)&&w.AcknowledgeCreatureView6CompositeEvents(historical),"Current historical journal may ack but not deliver to recreated target");
   Check(!w.ApplyCreatureView6CompositeDelta(g-1,null!,null!),"Generation before receipt access");w.BeginSelection(target);Check(!w.IsCurrent(fresh)&&!w.AcknowledgeCreatureView6CompositeEvents(historical)&&w.RetainedCreatureView6CompositeBytes==0,"Generation retires own state/journal should clear");cases.Add("history-move-recreate-generation");
  }
  using(var a=World())using(var b=World())
  {long ga=Stage(a,initial),gb=Stage(b,initial);Check(!a.ApplyCreatureView6CompositeDelta(ga,Current(b,target),null!)&&!a.AcknowledgeCreatureView6CompositeEvents(b.DrainCreatureView6CompositeEvents(gb)!),"Foreign world receipts reject");Exception? error=null;Task.Run(()=>{try{a.Close();}catch(Exception e){error=e;}}).GetAwaiter().GetResult();Check(error is InvalidOperationException&&a.IsCurrent(Current(a,target)),"Owner rejection before close");using(var router=new SceneWorldCreatureView6CompositeApplicationRouter(a)){router.Dispose();Check(a.IsCurrent(Current(a,target)),"Router disposal leaves borrowed world");}a.Close();Check(a.RetainedCreatureView6CompositeBytes==0&&a.Snapshots().Count==0,"Close clears owned state");Reject<ObjectDisposedException>(()=>a.ApplyCreatureView6CompositeDelta(ga,null!,null!));cases.Add("foreign-owner-close");}
  foreach(string fault in new[]{"null","id","count","world-mutation","ack","router-reentry","dispose","generation","event-drain","event-ack","stale-event-ack","world-attempt","pure-read"})using(var w=World())
  {
   long g=Stage(w,initial);var before=Current(w,target);var batch=w.DrainChanges(g)!;SceneWorldCreatureView6CompositeApplicationRouter? router=null;router=new(w,e=>
   {if(fault=="null")return null!;if(fault=="world-mutation")w.ApplyCreatureView6CompositeDelta(g,before,Add(target));if(fault=="ack")w.AcknowledgeChanges(batch);if(fault=="generation")w.BeginSelection(target);
    if(fault=="event-drain")w.DrainCreatureView6CompositeEvents(g);
    if(fault=="event-ack")w.AcknowledgeCreatureView6CompositeEvents(w.DrainCreatureView6CompositeEvents(g)!);
    if(fault=="stale-event-ack"){var stale=w.DrainCreatureView6CompositeEvents(g)!;w.AcknowledgeCreatureView6CompositeEvents(stale);w.AcknowledgeCreatureView6CompositeEvents(stale);}
    if(fault=="world-attempt")Check(!w.ApplyCreatureView6CompositeDelta(g-1,null!,null!),"Caught stale mutation attempt");
    if(fault=="pure-read")Check(w.IsCurrent(before)&&w.FindCreatureView6Composite(g,target) is not null&&w.Snapshots().Count==1,"Pure inspection callback");
    if(fault=="router-reentry"){try{router!.Accept(g,CreatureView6CompositeCodec.Encode(Add(target),new()));}catch(InvalidOperationException){}}if(fault=="dispose"){try{router!.Dispose();}catch(InvalidOperationException){}}
    return fault=="id"?Add(target+1):fault=="count"?new(target,0,Add(target).Updates):Add(target);});
   if(fault=="pure-read")Check(router.Accept(g,CreatureView6CompositeCodec.Encode(Add(target),new())),"Pure read callback permitted");else if(fault is "null" or "id" or "count")Reject<InvalidDataException>(()=>router.Accept(g,CreatureView6CompositeCodec.Encode(Add(target),new())));else Reject<InvalidOperationException>(()=>router.Accept(g,CreatureView6CompositeCodec.Encode(Add(target),new())));
   if(fault is not ("world-mutation" or "generation" or "pure-read"))Check(w.IsCurrent(before),"Callback rejection no candidate publication");if(fault!="pure-read")Reject<ObjectDisposedException>(()=>router.Accept(g,initial));router.Dispose();cases.Add("callback-"+fault);
  }
  foreach(string fault in new[]{"wrong-family","pending-target","router-malformed","router-budget","router-owner"})using(var w=World())
  {
   long g=Stage(w,initial);var before=Current(w,target);var batch=w.DrainChanges(g)!;
   if(fault=="wrong-family"){var invalid=CreatureView6CompositeCodec.Encode(Add(target),new());invalid[14]=0;Reject<InvalidDataException>(()=>w.Accept(g,invalid));Check(w.Snapshots().Count==0&&w.State==ZoneSceneTransitionState.Failed,"Wrong family preserves existing generation-fatal envelope policy");}
   else if(fault=="pending-target"){w.Accept(g,Create(target+1));w.Accept(g,CreatureView6Codec.EncodeBaseline(Baseline(target+1),new()));var pending=Current(w,target+1);Reject<InvalidDataException>(()=>w.ApplyCreatureView6CompositeDelta(g,pending,Add(target+1)));Check(w.IsCurrent(before)&&w.IsCurrent(pending),"Pending target cannot consume delta");}
   else
   {
    using var router=new SceneWorldCreatureView6CompositeApplicationRouter(w,maximumPayloadBytes:fault=="router-budget"?2:65536);
    if(fault=="router-owner"){Exception? error=null;Task.Run(()=>{try{router.Accept(g,CreatureView6CompositeCodec.Encode(Add(target),new()));}catch(Exception e){error=e;}}).GetAwaiter().GetResult();Check(error is InvalidOperationException&&router.Accept(g,CreatureView6CompositeCodec.Encode(Add(target),new())),"Router owner rejection preserves later valid access");}
    else {var invalid=CreatureView6CompositeCodec.Encode(Add(target),new());if(fault=="router-malformed")invalid[25]=1;Reject<InvalidDataException>(()=>router.Accept(g,invalid));Check(w.IsCurrent(before)&&w.IsCurrent(batch),"Malformed/budget ingress leaves world intact");}
   }
   cases.Add(fault);
  }
  foreach(bool generic in new[]{false,true})
  {
   var prior=new SceneWorldReplicationPolicy(SceneWorldReplicationMode.CreatureView3Scalars,new(65536,64,16,16,4096),16,4096);
   using var w=generic?new SceneWorldCoordinator(Catalog(),new Dictionary<SceneObjectResourceRecipe,SceneObjectResourceOptions>{{SceneObjectResourceRecipe.CreatureDetail0,new SceneCreatureResourceOptions(idle,movement,false)}},(_,_)=>throw new Exception("No loads"),SceneCreateZeroQuaternionPolicy.Reject,replicationPolicy:prior,creatureView6CompositePolicy:Policy(),maximumPayloadBytes:1048576):new SceneWorldCoordinator(Catalog(),idle,movement,(_,_)=>throw new Exception("No loads"),SceneCreateZeroQuaternionPolicy.Reject,replicationPolicy:prior,creatureView6CompositePolicy:Policy(),maximumPayloadBytes:1048576);
   long g=w.BeginSelection(target).Generation;foreach(var wire in new[]{Start(target),Create(target),Baseline3(target,true),initial,SceneObjectLifecycleCodec.EncodeEndBaselines(new(target))})w.Accept(g,wire);w.CompleteMinimumSceneBarrier(g,true);var before=w.FindReplication(g,target)!;var c6=Current(w,target);Check(w.IsCurrent(before)&&w.IsCurrent(c6)&&before.Names.CustomNameUnits.SequenceEqual(new ushort[]{67,68}),"Prior names and scalar/motion partition present");using var router=new SceneWorldCreatureView6CompositeApplicationRouter(w);Check(router.Accept(g,CreatureView6CompositeCodec.Encode(Add(target),new()))&&w.IsCurrent(before)&&ReferenceEquals(w.FindReplication(g,target)!.Names,before.Names)&&ReferenceEquals(w.FindReplication(g,target)!.Scalars,before.Scalars),"CREO6 join preserves exact previous name/motion state");cases.Add(generic?"generic-creo3-coexist":"creature-constructor-coexist");
  }
  {
   string staticPath="object/synthetic_round34_static.iff";uint sc=LegacyAssetPath.Checksum(staticPath);var catalog=new ObjectTemplateCatalog(Catalog().Entries.Values.Concat(new[]{new ObjectTemplateCatalogEntry(sc,staticPath,pin,pin,SceneObjectResourceRecipe.StaticAppearance)}));var options=new Dictionary<SceneObjectResourceRecipe,SceneObjectResourceOptions>{{SceneObjectResourceRecipe.CreatureDetail0,new SceneCreatureResourceOptions(idle,movement,false)},{SceneObjectResourceRecipe.StaticAppearance,new SceneStaticResourceOptions(SceneStaticMaterialPolicy.AllowNeutral,null)}};
   var prior=new SceneWorldReplicationPolicy(SceneWorldReplicationMode.CreatureView3Scalars,new(65536,64,16,16,4096),16,4096);var t3=new SceneWorldTangibleReplicationPolicy(new(65536,64,16,16,4096),new(65536,32,32,32,64,4096,64,64,64,32,64,16),new());var t6=new SceneWorldTangibleView6Policy(new(),new(),new());
   using var w=new SceneWorldCoordinator(catalog,options,(_,_)=>throw new Exception("No loads"),SceneCreateZeroQuaternionPolicy.Reject,replicationPolicy:prior,tangibleReplicationPolicy:t3,tangibleView6Policy:t6,creatureView6CompositePolicy:Policy(),maximumPayloadBytes:1048576);long g=w.BeginSelection(target).Generation;var staticCreate=SceneObjectLifecycleCodec.DecodeCreate(Create(target+1)) with {TemplateCrc=sc};
   foreach(var wire in new[]{Start(target),Create(target),SceneObjectLifecycleCodec.EncodeCreate(staticCreate),Baseline3(target,true),initial,Baseline3(target+1,false),TangibleView6Codec.EncodeBaseline(new(target+1,0,0,0,Array.Empty<ulong>()),new()),SceneObjectLifecycleCodec.EncodeEndBaselines(new(target)),SceneObjectLifecycleCodec.EncodeEndBaselines(new(target+1))})w.Accept(g,wire);w.CompleteMinimumSceneBarrier(g,true);
   var c3=w.FindReplication(g,target)!;var ts=w.FindTangibleReplication(g,target+1)!;var td=w.FindTangibleView6(g,target+1)!;var cs=Current(w,target);Check(w.IsCurrent(c3)&&w.IsCurrent(ts)&&w.IsCurrent(td)&&w.IsCurrent(cs),"All four typed partitions coexist");Check(w.ApplyCreatureView6CompositeDelta(g,cs,Add(target))&&w.IsCurrent(c3)&&w.IsCurrent(ts)&&w.IsCurrent(td),"CREO6 does not supersede existing partitions");Check(w.Snapshots().All(o=>o.Envelopes.Count==0),"Typed view boundaries retain no opaque history");cases.Add("mixed-tano3-tano6-creo3");
  }
  using(var w=World(typed:false)){long g=Stage(w,initial);Check(!w.CreatureView6CompositeEnabled&&w.Snapshots()[0].CreatureView6Composite is null&&w.Snapshots()[0].Envelopes.Count==1,"Default opaque mode unchanged");cases.Add("default-opaque");}
  using(var w=World()){Reject<InvalidDataException>(()=>Stage(w,initial,true));Check(w.Snapshots().Count==0&&w.RetainedCreatureView6CompositeBytes==0,"Missing baseline end barrier drains generation");cases.Add("missing-seed");}
  {
   var held=new TaskCompletionSource<SceneObjectResources>(TaskCreationOptions.RunContinuationsAsynchronously);CancellationToken token=default;using var w=World(loader:(_,t)=>{token=t;return held.Task;});long g=Stage(w,initial);w.PollObjectResources();Check(w.OutstandingJobs==1,"Owned held synthetic job");var current=Current(w,target);Check(w.ApplyCreatureView6CompositeDelta(g,current,Add(target)),"Valid update with held resource job");current=Current(w,target);var bad=CreatureView6CompositeCodec.Encode(Add(target),new());bad[25]=15;Reject<InvalidDataException>(()=>w.Accept(g,bad));Check(w.IsCurrent(current)&&w.OutstandingJobs==1&&!token.IsCancellationRequested,"Later invalid packet retains presentation/resource ownership");w.Close();Check(token.IsCancellationRequested&&!w.DrainCompletion.IsCompleted,"Close cancels and exposes pending drain");held.SetException(new InvalidDataException("Synthetic late fault"));Check(w.DrainCompletion.Wait(TimeSpan.FromSeconds(5))&&w.OutstandingJobs==0&&w.RetainedCreatureView6CompositeBytes==0,"Late completion drains without resurrection");cases.Add("cancel-drain");
  }
  foreach(bool generic in new[]{false,true})
  {
   var scalar=new SceneWorldCreatureView6Policy(new(),new(),new());
   Reject<ArgumentException>(()=>{using var invalid=generic?new SceneWorldCoordinator(Catalog(),new Dictionary<SceneObjectResourceRecipe,SceneObjectResourceOptions>{{SceneObjectResourceRecipe.CreatureDetail0,new SceneCreatureResourceOptions(idle,movement,false)}},(_,_)=>throw new Exception("No loads"),SceneCreateZeroQuaternionPolicy.Reject,creatureView6Policy:scalar,creatureView6CompositePolicy:Policy(),maximumPayloadBytes:1048576):new SceneWorldCoordinator(Catalog(),idle,movement,(_,_)=>throw new Exception("No loads"),SceneCreateZeroQuaternionPolicy.Reject,creatureView6Policy:scalar,creatureView6CompositePolicy:Policy(),maximumPayloadBytes:1048576);});
  }
  cases.Add("exclusive-creo6-policies");
  foreach(string limit in new[]{"global-output-journal","global-queue"})using(var w=World(Policy() with {StoreLimits=new(MaximumOutputBytes:limit=="global-output-journal"?241:8192,MaximumPendingEvents:limit=="global-queue"?1:4096)}))
  {
   long g=Stage(w,initial);w.Accept(g,Create(target+1));w.Accept(g,CreatureView6Codec.EncodeBaseline(Baseline(target+1),new()));w.Accept(g,SceneObjectLifecycleCodec.EncodeEndBaselines(new(target+1)));Check(w.ApplyCreatureView6CompositeDelta(g,Current(w,target),Add(target)),"Exact other actor and journal capacity");var other=Current(w,target+1);var worldBatch=w.DrainChanges(g)!;var events=w.DrainCreatureView6CompositeEvents(g)!;
   Reject<InvalidDataException>(()=>w.ApplyCreatureView6CompositeDelta(g,other,Add(target+1)));Check(w.IsCurrent(other)&&w.IsCurrent(worldBatch)&&events.Events.Count==1&&w.IsCurrentCreatureView6CompositeEvent(events,0)&&w.RetainedCreatureView6CompositeBytes==241,"Prior actor/journal charges retained on failed second actor");cases.Add(limit);
  }
  using(var w=World(Policy() with {StoreLimits=new(MaximumStateBytes:113)}))
  {
   long g=Stage(w,initial);Check(w.ApplyCreatureView6CompositeDelta(g,Current(w,target),Add(target)),"Earlier scalar publication");var before=Current(w,target);var events=w.DrainCreatureView6CompositeEvents(g)!;var batch=w.DrainChanges(g)!;
   var grow=new CreatureView6CompositeUpdate(new CreatureHamView6Update(13,3,3,new[]{new CreatureHamView6Command(3,0,new[]{1,2})}));var clear=new CreatureView6CompositeUpdate(new CreatureHamView6Update(13,1,4,new[]{new CreatureHamView6Command(4,0,Array.Empty<int>())}));var plan=new CreatureView6CompositeDelta(target,0,new[]{new CreatureView6CompositeUpdate(CreatureView6Update.Scalar(0,99)),grow,clear});
   Reject<InvalidDataException>(()=>w.ApplyCreatureView6CompositeDelta(g,before,plan));Check(w.IsCurrent(before)&&w.IsCurrent(batch)&&w.IsCurrentCreatureView6CompositeEvent(events,0)&&before.Model.Passive==Baseline(target).Passive&&before.Model.Ham.Values.Count==Baseline(target).Ham.Values.Count,"Late HAM intermediate overflow leaves no scalar/vector prefix");cases.Add("late-ham-intermediate");
  }
  using(var w=World())
  {
   long g=Stage(w,initial);var seed=Current(w,target);var noop=new CreatureView6CompositeDelta(target,0,new[]{new CreatureView6CompositeUpdate(new CreatureHamView6Update(13,1,4,new[]{new CreatureHamView6Command(4,0,Array.Empty<int>())}))});Check(w.ApplyCreatureView6CompositeDelta(g,seed,noop),"No-op HAM publication accepted");var value=Current(w,target);Check(value.Revision==2&&value.State.Revision==2&&w.DrainCreatureView6CompositeEvents(g)!.Events.Count==0&&!w.IsCurrent(seed),"No-op still advances exact outer publication");
   var valid=CreatureView6CompositeCodec.Encode(Add(target),new());foreach(byte field in new byte[]{1,15}){var bad=valid.ToArray();bad[25]=field;var batch=w.DrainChanges(g)!;Reject<InvalidDataException>(()=>w.Accept(g,bad));Check(w.IsCurrent(value)&&w.IsCurrent(batch),"Unsupported defender/wearable does not consume candidate");}
   var malformed=new byte[valid.Length+11];valid.CopyTo(malformed,0);var tail=malformed.AsSpan(valid.Length);System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(tail,13);System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(tail[2..],1);tail[10]=255;System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(malformed.AsSpan(19),(uint)(malformed.Length-23));Reject<InvalidDataException>(()=>w.Accept(g,malformed));Check(w.IsCurrent(value)&&w.RetainedCreatureView6CompositeBytes==108,"Late malformed HAM after scalar field retains prior model");cases.Add("noop-and-late-wire");
  }
  File.WriteAllText(output,JsonSerializer.Serialize(new{Checks=checks,Sequences=records.Count,Cases=cases,Records=records},new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine($"R36_CREO6_COMPOSITE_WORLD_PASS checks={checks} sequences={records.Count} cases={cases.Count}");
 }
}
