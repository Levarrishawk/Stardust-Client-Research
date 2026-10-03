using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

internal static class SceneWorldCustomizationStateScenarios
{
    private static int checks;
    private static void Check(bool value,string why){checks++;if(!value)throw new Exception(why);}
    private static void Reject<T>(Action action)where T:Exception{try{action();}catch(T){checks++;return;}throw new Exception("Expected "+typeof(T).Name);}
    private static ReplicationByteString Raw(params byte[] bytes)=>ReplicationByteString.Create(bytes);
    private static string Hex(ReplicationByteString value)=>Convert.ToHexString(value.CopyBytes());
    private sealed class ReenteringEnvelopes(IReadOnlyList<ReplicationEnvelope> values,Action callback):IReadOnlyList<ReplicationEnvelope>
    {
        public int Count=>values.Count;
        public ReplicationEnvelope this[int index]=>values[index];
        public IEnumerator<ReplicationEnvelope> GetEnumerator(){callback();return values.GetEnumerator();}
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()=>GetEnumerator();
    }
    internal static void Run(string[] inputs,IReadOnlyDictionary<string,string> peer,string exportPath)
    {
        checks=0;Check(inputs.Length==4,"root config catalog bank");Check(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(inputs[1])))==RetainedSkeletalDetailBundleReader.ConfigurationPin,"Configuration pin");
        var catalog=ObjectTemplateCatalogProfile.Decode(File.ReadAllBytes(inputs[2]));using var stream=File.OpenRead(inputs[3]);var bank=SkeletalAnimationBankProfile.Load(stream);
        var idle=new LogicalAnimationClipSelection("appearance/lat/all_m.lat","emt_standing_idle_ambient_1_ag","798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B");
        var options=new Dictionary<SceneObjectResourceRecipe,SceneObjectResourceOptions>{{SceneObjectResourceRecipe.CreatureDetail0,new SceneCreatureResourceOptions(idle,bank,true)},{SceneObjectResourceRecipe.StaticAppearance,new SceneStaticResourceOptions(SceneStaticMaterialPolicy.RequireComplete,new("shader/anim_mission_terminal.sht","B5AA5A251A76AD8F152DB85F50FBC3061D0494847499F1068A9B55C375785A62",0))}};
        byte[] Bytes(string key)=>Convert.FromHexString(peer[key]);ulong id=SceneObjectLifecycleCodec.DecodeCreate(Bytes("create")).ObjectId;int loaderCalls=0;
        var policy=new SceneWorldReplicationPolicy(SceneWorldReplicationMode.CreatureView3MixedComponents,new(4096,1024,64,16,8192),16,8192,new(4096,32,24,64,64,16),CreatureWoundsApplicationPolicy.LegacyBounded,new(4096,32,64,64,16),new(16,16));
        SceneWorldCoordinator World()=>new(catalog,options,(request,token)=>{loaderCalls++;throw new Exception("State fixture must not load assets");},SceneCreateZeroQuaternionPolicy.Reject,4,2,32,8192,16,4096,4096,policy);
        byte[] Baseline(byte[] value,ulong? target=null)
        {
            var envelope=ReplicationEnvelopeCodec.DecodeBaseline(Bytes("baseline"),4096);var fields=envelope.CopyOpaqueFields();int cursor=4;
            void String(){int n=BinaryPrimitives.ReadUInt16LittleEndian(fields.AsSpan(cursor));cursor+=2;if(n==65535){n=checked((int)BinaryPrimitives.ReadUInt32LittleEndian(fields.AsSpan(cursor)));cursor+=4;}cursor+=n;}
            String();cursor+=4;String();int units=checked((int)BinaryPrimitives.ReadUInt32LittleEndian(fields.AsSpan(cursor)));cursor+=4+units*2+4;int start=cursor;String();
            var replacement=new byte[start+2+value.Length+fields.Length-cursor];fields.AsSpan(0,start).CopyTo(replacement);BinaryPrimitives.WriteUInt16LittleEndian(replacement.AsSpan(start),(ushort)value.Length);value.CopyTo(replacement,start+2);fields.AsSpan(cursor).CopyTo(replacement.AsSpan(start+2+value.Length));
            return ReplicationEnvelopeCodec.EncodeBaseline(new(ReplicationEnvelopeKind.Baseline,target??id,envelope.RawTag,3,envelope.DeclaredCount,replacement,4096),4096);
        }
        long Stage(SceneWorldCoordinator w,byte[]? seed=null){long g=w.BeginSelection(id).Generation;foreach(string key in new[]{"start","create","staticCreate"})w.Accept(g,Bytes(key));w.Accept(g,Baseline(seed??new byte[]{1,2,3,4}));foreach(string key in new[]{"staticBaseline","end","staticEnd"})w.Accept(g,Bytes(key));w.CompleteMinimumSceneBarrier(g,true);return g;}
        SceneWorldObjectSnapshot Current(SceneWorldCoordinator w)=>w.Snapshots().Single(s=>s.Object.Handle.ObjectId==id);
        CreatureView3CustomizationDeltaPlan Plan(params ReplicationByteString[] updates)=>new(id,65535,updates);
        byte[] Wire(ulong target,ushort first=4,uint tag=CreatureView3BaselineCodec.RawTag,byte view=3,ushort count=65535)=>ReplicationEnvelopeCodec.EncodeDelta(new(ReplicationEnvelopeKind.Delta,target,tag,view,count,new byte[]{(byte)first,(byte)(first>>8),0xAA},4096),4096);
        var cases=new List<string>();var receipts=new List<object>();
        using(var w=World())
        {
            long g=Stage(w);using var store=new SceneWorldCustomizationStateStore(w);var snapshot=Current(w);var baseline=snapshot.Replication!.Baseline;var seed=store.Find(snapshot)!;
            Check(seed.ThroughRevision==1&&seed.Updates.Count==1&&ReferenceEquals(seed.Current,baseline.Customization),"Lazy immutable seed");Check(ReferenceEquals(seed,store.Find(snapshot)),"Stable issued batch");
            var a=Raw(0,255,2);var empty=Raw();var b=Raw(9);Check(store.Apply(snapshot,Plan(a,a,empty,b)),"Ordered raw replacements");var batch=store.Find(snapshot)!;
            Check(batch.Updates.Select(u=>u.Revision).SequenceEqual(new ulong[]{1,2,3,4,5})&&batch.Updates.Select(u=>Hex(u.Value)).SequenceEqual(new[]{"01020304","00FF02","00FF02","","09"}),"Duplicates/empty/order retained");Check(Hex(baseline.Customization)=="01020304"&&ReferenceEquals(batch.Current,b),"Historic baseline unchanged");
            var forged=new SceneWorldRawCustomizationBatch(batch.Incarnation,batch.ThroughRevision,batch.Current,batch.Updates);Check(!store.Acknowledge(forged)&&!store.Acknowledge(seed),"Forged/superseded batch cannot clear");
            var c=Raw(8);store.Apply(snapshot,Plan(c));long beforeAckBytes=store.RetainedBytes;Check(!store.Acknowledge(batch)&&store.PendingUpdateCount==6,"New accepted writes invalidate issued receipt without loss");var tail=store.Find(snapshot)!;Check(tail.Updates.Count==6&&ReferenceEquals(tail.Current,c),"Fresh batch retains entire ordered prefix and new tail");Check(store.Acknowledge(tail)&&store.PendingUpdateCount==0&&!store.Acknowledge(tail),"Exact current acknowledgment clears its prefix once");long afterAckBytes=store.RetainedBytes;
            w.Accept(g,Bytes("composite"));Check(store.Find(snapshot) is null,"Stale scalar snapshot rejected");var body=Current(w);Check(Hex(store.Find(body)!.Current)=="08"&&store.Revision==6,"Body delta does not reset field4");
            var old=store.Find(body)!;w.Accept(g,Baseline(new byte[]{7,7}));Check(!store.Acknowledge(old)&&store.Find(body) is null,"Baseline replacement invalidates old seed receipt");var reseed=store.Find(Current(w))!;Check(reseed.Updates.Count==1&&Hex(reseed.Current)=="0707"&&reseed.ThroughRevision==7,"Same incarnation replacement appends seed");
            store.Apply(Current(w),Plan(Raw(6)));w.Accept(g,Baseline(new byte[]{5}));var ordered=store.Find(Current(w))!;Check(ordered.Updates.Select(u=>Hex(u.Value)).SequenceEqual(new[]{"0707","06","05"}),"Reseeding preserves unacknowledged order");
            receipts.Add(new {Name="ordered",Revisions=batch.Updates.Select(u=>u.Revision),Values=batch.Updates.Select(u=>Hex(u.Value)),CurrentAfterAck=Hex(tail.Current),BeforeAckBytes=beforeAckBytes,AfterAckBytes=afterAckBytes,ReseedValues=ordered.Updates.Select(u=>Hex(u.Value)),ReseedBytes=store.RetainedBytes});
            ulong revision=store.Revision;store.Clear();Check(store.Count==0&&store.Revision==revision,"Clear never rewinds");Check(store.Find(Current(w))!.ThroughRevision==revision+1,"Clear reseeds with fresh global revision");
            w.Accept(g,Bytes("destroy"));Check(store.Prune()==1&&store.Count==0,"Destroy prunes");w.Accept(g,Bytes("create"));w.Accept(g,Baseline(new byte[]{4}));w.Accept(g,Bytes("end"));var recreated=store.Find(Current(w))!;Check(recreated.Incarnation!=batch.Incarnation&&recreated.ThroughRevision>revision+1&&!store.Acknowledge(ordered),"Recreation binds new incarnation");
            w.BeginSelection(id);Check(store.Count==0,"Generation clears lazily");store.Dispose();Check(w.State!=ZoneSceneTransitionState.Closed&&store.IsClosed&&store.Find(body) is null&&!store.Apply(body,Plan(a)),"Store dispose only releases borrows");cases.Add("seed-order-ack-body-reseed-lifecycle");
        }
        using(var w=World())
        {
            Stage(w);var current=Current(w);using var store=new SceneWorldCustomizationStateStore(w,maximumPendingUpdates:2,maximumStringBytes:4);store.Find(current);ulong before=store.Revision;
            Reject<InvalidDataException>(()=>store.Apply(current,Plan(Raw(1),Raw(2))));Check(store.Revision==before&&store.PendingUpdateCount==1,"Whole journal plan preflight");
            Reject<InvalidDataException>(()=>store.Apply(current,Plan(Raw(1),Raw(1,2,3,4,5))));Check(store.Revision==before&&Hex(store.Find(current)!.Current)=="01020304","Late invalid string no prefix");
            Reject<InvalidDataException>(()=>store.Apply(current,new(id+1,0,new[]{Raw(1)})));Check(!store.Apply(current with {Object=current.Object with {}},Plan(Raw(1))),"Forged object snapshot rejected");
            using(var foreign=World()){Stage(foreign);Check(store.Find(Current(foreign)) is null,"Foreign world snapshot rejected");}
            Check(store.Find(w.Snapshots().Single(s=>s.Object.Handle.ObjectId!=id)) is null,"Static has no invented field4");
            Exception? failure=null;var thread=new Thread(()=>{try{store.Clear();}catch(Exception e){failure=e;}});thread.Start();thread.Join();Check(failure is InvalidOperationException&&store.Revision==before,"Foreign thread mutation rejected");cases.Add("transaction-target-thread");
        }
        using(var w=World())
        {
            Stage(w);var current=Current(w);var shared=Raw(8,9,10);using var store=new SceneWorldCustomizationStateStore(w,maximumRetainedBytes:7);store.Find(current);store.Apply(current,Plan(shared,shared));Check(store.RetainedBytes==7,"Current/pending/shared reference dedup includes seed");store.Acknowledge(store.Find(current)!);var emptyBatch=store.Find(current)!;
            Reject<InvalidDataException>(()=>store.Apply(current,Plan(Raw(6,7,8,9))));Check(store.Revision==3&&store.PendingUpdateCount==0,"Whole retained-byte preflight");Check(store.Apply(current,Plan(Raw(6,7,8)))&&store.RetainedBytes==7&&!store.Acknowledge(emptyBatch),"Replacement dedup budget and stale empty receipt");cases.Add("logical-retention-reference-budget");
        }
        using(var w=World())
        {
            Stage(w);var current=Current(w);using var store=new SceneWorldCustomizationStateStore(w,maximumPendingUpdates:1);Reject<InvalidDataException>(()=>store.Apply(current,Plan(Raw(1))));Check(store.Count==0&&store.Revision==0,"Unseeded plan preflights seed and update together");
            using var overflow=new SceneWorldCustomizationStateStore(w,revisionSeed:ulong.MaxValue);Reject<OverflowException>(()=>overflow.Find(current));Check(overflow.Count==0&&overflow.Revision==ulong.MaxValue,"Seed checked revision overflow atomic");
            using var later=new SceneWorldCustomizationStateStore(w,revisionSeed:ulong.MaxValue-2);var seed=later.Find(current)!;Reject<OverflowException>(()=>later.Apply(current,Plan(Raw(1),Raw(2))));Check(later.Revision==ulong.MaxValue-1&&ReferenceEquals(seed,later.Find(current)),"Late revision overflow atomic");cases.Add("seed-plan-overflow");
        }
        using(var w=World())
        {
            Stage(w,Array.Empty<byte>());using var store=new SceneWorldCustomizationStateStore(w,maximumStringBytes:0,maximumRetainedBytes:0);Check(store.Find(Current(w))!.Current.Length==0&&store.RetainedBytes==0,"Empty seed zero budgets");store.Apply(Current(w),Plan(Raw(),Raw()));Check(store.PendingUpdateCount==3,"Empty writes still journaled");cases.Add("empty-bypass-preservation");
        }
        using(var w=World())
        {
            long g=Stage(w);using var store=new SceneWorldCustomizationStateStore(w);int decodes=0,downstream=0;byte[]? borrowed=null;var first=Raw(1,1);var second=Raw(255,255);using var router=new SceneWorldCustomizationApplicationRouter(w,store,e=>{decodes++;return Plan(decodes==1?first:second);},(generation,message)=>{downstream++;borrowed=message.ToArray();return w.Accept(generation,message.Span);});
            Check(!router.Accept(g-1,new byte[]{0})&&decodes==0&&downstream==0,"Stale before parse");Check(router.Accept(g,Wire(id))&&router.Accept(g,Wire(id)),"Two ordered packets");var batch=store.Find(Current(w))!;Check(batch.Updates.Select(u=>Hex(u.Value)).SequenceEqual(new[]{"01020304","0101","FFFF"}),"Prior valid raw before later invalid attempt is not collapsed");
            int fakeLocal=0;foreach(var update in batch.Updates){var raw=update.Value.CopyBytes();if(raw.Length==2){int attempted=BinaryPrimitives.ReadUInt16LittleEndian(raw);if(attempted<=300)fakeLocal=attempted;}}Check(fakeLocal==257,"Synthetic ranged subscriber retains prior valid value before later invalid attempt");
            Check(router.Accept(g,Bytes("composite"))&&downstream==1&&decodes==2,"Ordinary delta downstream");Check(Hex(store.Find(Current(w))!.Current)=="FFFF","Body downstream preserves raw");Check(router.Accept(g,Bytes("staticDelta"))&&downstream==2,"TANO downstream");Check(router.Accept(g,Bytes("destroy"))&&store.Count==0,"Ordinary destruction downstream allowed");router.Dispose();Check(!store.IsClosed&&w.State!=ZoneSceneTransitionState.Closed&&borrowed is not null,"Router borrows do not close world/store");Reject<ObjectDisposedException>(()=>router.Accept(g,Wire(id)));cases.Add("router-routing-order-borrow");
        }
        foreach(string mode in new[]{"throw","null","target","count","world","generation","ack-world","clear-source","dispose-source","reentry","dispose-router","source-find"})
        using(var w=World())
        {
            long g=Stage(w);using var store=new SceneWorldCustomizationStateStore(w);store.Find(Current(w));ulong before=store.Revision;SceneWorldCustomizationApplicationRouter? router=null;
            router=new(w,store,e=>{if(mode=="throw")throw new ApplicationException("decoder");if(mode=="null")return null!;if(mode=="world")w.Accept(g,Bytes("composite"));if(mode=="generation")w.BeginSelection(id);if(mode=="ack-world")w.AcknowledgeChanges(w.DrainChanges(g)!);if(mode=="clear-source")store.Clear();if(mode=="dispose-source")store.Dispose();if(mode=="reentry")Reject<InvalidOperationException>(()=>router!.Accept(g,Wire(id)));if(mode=="dispose-router")Reject<InvalidOperationException>(()=>router!.Dispose());if(mode=="source-find")store.Acknowledge(store.Find(Current(w))!);return new(mode=="target"?id+1:id,mode=="count"?(ushort)0:e.DeclaredCount,new[]{Raw(9)});});using(router)
            {
                if(mode=="throw")Reject<ApplicationException>(()=>router.Accept(g,Wire(id)));else if(mode is "null" or "target" or "count")Reject<InvalidDataException>(()=>router.Accept(g,Wire(id)));else Reject<InvalidOperationException>(()=>router.Accept(g,Wire(id)));
                Check(store.Revision==before,"Decoder failure never commits plan "+mode);Reject<ObjectDisposedException>(()=>router.Accept(g,Wire(id)));Check(w.State!=ZoneSceneTransitionState.Closed,"Failure does not close borrowed world");cases.Add("router-"+mode);
            }
        }
        foreach(string mode in new[]{"throw","reentry","store","generation","false"})using(var w=World())
        {
            long g=Stage(w);using var store=new SceneWorldCustomizationStateStore(w);store.Find(Current(w));SceneWorldCustomizationApplicationRouter? router=null;router=new(w,store,e=>Plan(Raw(9)),(generation,message)=>{if(mode=="throw")throw new ApplicationException("downstream");if(mode=="reentry")Reject<InvalidOperationException>(()=>router!.Accept(g,message));if(mode=="store")store.Clear();if(mode=="generation")w.BeginSelection(id);return mode!="false";});using(router)
            {
                if(mode=="throw")Reject<ApplicationException>(()=>router.Accept(g,new byte[]{1}));else if(mode=="false")Check(!router.Accept(g,new byte[]{1}),"Downstream false preserved");else Reject<InvalidOperationException>(()=>router.Accept(g,new byte[]{1}));cases.Add("downstream-"+mode);
            }
        }
        using(var w=World())
        {
            long g=Stage(w);using var store=new SceneWorldCustomizationStateStore(w);int decodes=0;using var router=new SceneWorldCustomizationApplicationRouter(w,store,e=>{decodes++;return Plan(Raw(1));});var bytes=Wire(id);Array.Resize(ref bytes,bytes.Length-1);Reject<InvalidDataException>(()=>router.Accept(g,bytes));Check(decodes==0&&store.Revision==0,"Outer malformed before decoder");cases.Add("outer-malformed");
        }
        using(var w=World())
        {
            long g=Stage(w);w.Accept(g,Bytes("staticDestroy"));var create=Bytes("staticCreate");BinaryPrimitives.WriteUInt32LittleEndian(create.AsSpan(42),2937962913);w.Accept(g,create);w.Accept(g,Baseline(new byte[]{5,6},id+1));w.Accept(g,Bytes("staticEnd"));var first=Current(w);var second=w.Snapshots().Single(s=>s.Object.Handle.ObjectId==id+1);
            using(var limited=new SceneWorldCustomizationStateStore(w,maximumObjects:1)){limited.Find(first);Reject<InvalidDataException>(()=>limited.Find(second));Check(limited.Count==1&&limited.Revision==1,"Actor seed capacity atomic");}
            using(var pending=new SceneWorldCustomizationStateStore(w,maximumPendingUpdates:3)){pending.Find(first);pending.Find(second);Reject<InvalidDataException>(()=>pending.Apply(first,Plan(Raw(1),Raw(2))));Check(pending.Revision==2&&pending.PendingUpdateCount==2,"Global journal cap across actors");pending.Acknowledge(pending.Find(first)!);Check(pending.Apply(first,Plan(Raw(1),Raw(2)))&&pending.PendingUpdateCount==3,"Acknowledgment frees global capacity");}
            using(var bytes=new SceneWorldCustomizationStateStore(w,maximumRetainedBytes:9)){bytes.Find(first);bytes.Find(second);var shared=Raw(7,8,9);bytes.Apply(first,Plan(shared));bytes.Apply(second,new(id+1,0,new[]{shared}));Check(bytes.RetainedBytes==9,"Immutable reference charge dedups across actors");}
            Reject<ArgumentOutOfRangeException>(()=>new SceneWorldCustomizationStateStore(w,maximumObjects:0));Reject<ArgumentOutOfRangeException>(()=>new SceneWorldCustomizationStateStore(w,maximumPendingUpdates:4097));Reject<ArgumentOutOfRangeException>(()=>new SceneWorldCustomizationStateStore(w,maximumStringBytes:-1));Reject<ArgumentOutOfRangeException>(()=>new SceneWorldCustomizationStateStore(w,maximumRetainedBytes:134217729));cases.Add("multi-object-global-budgets");
        }
        using(var w=World())
        {
            long g=Stage(w);using var store=new SceneWorldCustomizationStateStore(w);int decoded=0,calls=0;ReadOnlyMemory<byte> retained=default;using var router=new SceneWorldCustomizationApplicationRouter(w,store,e=>{decoded++;var copied=e.CopyOpaqueFields();Array.Clear(copied);return Plan(Raw(3));},(generation,message)=>{calls++;retained=message;return true;});
            var caller=Wire(id,5);var before=caller.ToArray();Check(router.Accept(g,caller)&&decoded==0&&calls==1&&caller.SequenceEqual(before)&&retained.Span.ToArray().All(b=>b==0),"Downstream borrow copied then cleared; caller unchanged");
            Check(router.Accept(g,Wire(id,4,CreatureView3BaselineCodec.RawTag,1))&&router.Accept(g,Wire(id,4,0x54414E4F))&&decoded==0&&calls==3,"Other views/families downstream");
            Exception? failure=null;var thread=new Thread(()=>{try{router.Accept(g,Wire(id));}catch(Exception e){failure=e;}});thread.Start();thread.Join();Check(failure is InvalidOperationException&&router.Accept(g,Wire(id))&&decoded==1,"Foreign router thread fails before mutation and does not close owner");
            var valueBytes=new byte[]{0,255,128};var immutable=ReplicationByteString.Create(valueBytes,true);Array.Clear(valueBytes);store.Apply(Current(w),Plan(immutable));Check(Hex(store.Find(Current(w))!.Current)=="00FF80"&&immutable.UsesExtendedLength,"Raw byte ownership and extended form preserved");var copy=immutable.CopyBytes();Array.Clear(copy);Check(Hex(immutable)=="00FF80","Copy cannot mutate retained field");
            w.Close();Check(store.Prune()==1&&store.Count==0,"World close releases raw entries without store closing world");cases.Add("router-views-thread-byte-ownership");
        }
        using(var w=World())
        {
            long g=Stage(w);using var store=new SceneWorldCustomizationStateStore(w);using var router=new SceneWorldCustomizationApplicationRouter(w,store,e=>Plan(Raw(1)),maximumPayloadBytes:2);Reject<InvalidDataException>(()=>router.Accept(g,Wire(id)));Check(store.Count==0&&store.Revision==0,"Outer budget before mutation");
            using(var other=World()){Stage(other);Reject<ArgumentException>(()=>new SceneWorldCustomizationApplicationRouter(other,store,e=>Plan(Raw(1))));}cases.Add("router-budget-world-binding");
        }
        using(var w=World())
        {
            Stage(w);using var store=new SceneWorldCustomizationStateStore(w);var snapshot=Current(w);var seed=store.Find(snapshot)!;
            var caught=snapshot with {Envelopes=new ReenteringEnvelopes(snapshot.Envelopes,()=>Reject<InvalidOperationException>(()=>store.Clear()))};Check(ReferenceEquals(seed,store.Find(caught)),"Caught store reentry cannot mutate raw journal");
            var failing=snapshot with {Envelopes=new ReenteringEnvelopes(snapshot.Envelopes,()=>store.Clear())};Reject<InvalidOperationException>(()=>store.Find(failing));Check(store.Revision==1&&store.PendingUpdateCount==1,"Uncaught store reentry leaves complete old state");
            Check(store.Find(snapshot with {Object=null!}) is null&&store.Find(snapshot with {Envelopes=null!}) is null,"Malformed nested snapshot rejected");cases.Add("store-reentry-malformed-snapshot");
        }
        Check(loaderCalls==0,"No asset preparation/execution");File.WriteAllText(exportPath,JsonSerializer.Serialize(new{Checks=checks,Cases=cases,Receipts=receipts,LoaderCalls=loaderCalls,Result="PASS"},new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine("R21_STATE_PASS checks="+checks+" cases="+cases.Count);
    }
}
