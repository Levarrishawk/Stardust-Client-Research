using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

internal static class SceneWorldCustomizationScalarScenarios
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
        SceneWorldCoordinator World(SceneWorldReplicationPolicy? selected=null,SceneWorldChangePolicy? changes=null)=>new(catalog,options,(request,token)=>{loaderCalls++;throw new Exception("State fixture must not load assets");},SceneCreateZeroQuaternionPolicy.Reject,4,2,32,8192,16,4096,4096,selected??policy,changes);
        byte[] Baseline(byte[] value,ulong? target=null)
        {
            var envelope=ReplicationEnvelopeCodec.DecodeBaseline(Bytes("baseline"),4096);var fields=envelope.CopyOpaqueFields();int cursor=4;
            void String(){int n=BinaryPrimitives.ReadUInt16LittleEndian(fields.AsSpan(cursor));cursor+=2;if(n==65535){n=checked((int)BinaryPrimitives.ReadUInt32LittleEndian(fields.AsSpan(cursor)));cursor+=4;}cursor+=n;}
            String();cursor+=4;String();int units=checked((int)BinaryPrimitives.ReadUInt32LittleEndian(fields.AsSpan(cursor)));cursor+=4+units*2+4;int start=cursor;String();
            var replacement=new byte[start+2+value.Length+fields.Length-cursor];fields.AsSpan(0,start).CopyTo(replacement);BinaryPrimitives.WriteUInt16LittleEndian(replacement.AsSpan(start),(ushort)value.Length);value.CopyTo(replacement,start+2);fields.AsSpan(cursor).CopyTo(replacement.AsSpan(start+2+value.Length));
            return ReplicationEnvelopeCodec.EncodeBaseline(new(ReplicationEnvelopeKind.Baseline,target??id,envelope.RawTag,3,envelope.DeclaredCount,replacement,4096),4096);
        }
        long Stage(SceneWorldCoordinator w,byte[]? seed=null,bool creatureOnly=false){long g=w.BeginSelection(id).Generation;foreach(string key in (creatureOnly?new[]{"start","create"}:new[]{"start","create","staticCreate"}))w.Accept(g,Bytes(key));w.Accept(g,Baseline(seed??new byte[]{1,2,3,4}));foreach(string key in (creatureOnly?new[]{"end"}:new[]{"staticBaseline","end","staticEnd"}))w.Accept(g,Bytes(key));w.CompleteMinimumSceneBarrier(g,true);return g;}
        SceneWorldObjectSnapshot Current(SceneWorldCoordinator w)=>w.Snapshots().Single(s=>s.Object.Handle.ObjectId==id);
        byte[] Wire(ulong target,ushort first=4,uint tag=CreatureView3BaselineCodec.RawTag,byte view=3,ushort count=65535)=>ReplicationEnvelopeCodec.EncodeDelta(new(ReplicationEnvelopeKind.Delta,target,tag,view,count,new byte[]{(byte)first,(byte)(first>>8),0xAA},4096),4096);
        CreatureView3CustomizationScalarUpdate R(params byte[] b)=>CreatureView3CustomizationScalarUpdate.FromCustomization(Raw(b));
        CreatureView3CustomizationScalarUpdate S(CreatureView3ScalarUpdate v)=>CreatureView3CustomizationScalarUpdate.FromScalar(v);
        CreatureView3CustomizationScalarDeltaPlan Plan(params CreatureView3CustomizationScalarUpdate[] values)=>new(id,65535,values);
        var cases=new List<string>();var receipts=new List<object>();
        void Same(SceneWorldCoordinator w,SceneWorldCustomizationStateStore source,SceneWorldObjectSnapshot before,SceneWorldRawCustomizationBatch raw,SceneWorldChangeBatch journal,ulong revision)
        {
            var after=Current(w);Check(ReferenceEquals(before.Replication!.Scalars,after.Replication!.Scalars)&&before.Replication.Revision==after.Replication.Revision,"Replication unchanged on rejection");
            Check(ReferenceEquals(raw,source.Find(after))&&source.Revision==revision,"Raw receipt/revision unchanged on rejection");
            var pending=w.DrainChanges(w.Generation)!;Check(pending.MutationRevision==journal.MutationRevision&&pending.Entries.Select(e=>(e.ObjectId,e.Kind)).SequenceEqual(journal.Entries.Select(e=>(e.ObjectId,e.Kind))),"World pending batch unchanged on rejection");
            Check(w.State==ZoneSceneTransitionState.AwaitingServerReady,"Rejection does not fail world");
        }
        foreach(bool reverse in new[]{false,true})
        using(var w=World())
        {
            long g=Stage(w);using var source=new SceneWorldCustomizationStateStore(w);var before=Current(w);var seed=source.Find(before)!;var baseline=before.Replication!.Baseline;
            var writes=new[]{R(8),S(new CreatureHeightUpdate(1.25f)),R(9),S(new CreatureHeightUpdate(2.5f)),S(new CreaturePostureUpdate(3)),S(new CreatureRankUpdate(4)),S(new CreatureLinkUpdate(ulong.MaxValue)),S(new CreatureShockUpdate(uint.MaxValue)),S(new CreatureStatesUpdate(0xFEDCBA9876543210))};
            if(reverse)writes= writes.Reverse().ToArray();Check(w.ApplyCustomizationScalars(g,Plan(writes),source),"Ordered mixed transaction");
            var after=Current(w);var batch=source.Find(after)!;Check(ReferenceEquals(baseline,after.Replication!.Baseline)&&ReferenceEquals(before.Replication.Wounds,after.Replication.Wounds)&&ReferenceEquals(before.Replication.Components,after.Replication.Components),"Historic baseline/wounds/components preserved");
            Check(after.Replication.Revision==before.Replication.Revision+1&&after.Replication.Scalars.Height==(reverse?1.25f:2.5f)&&after.Replication.Scalars.Posture==3&&after.Replication.Scalars.Rank==4&&after.Replication.Scalars.LinkId==ulong.MaxValue&&after.Replication.Scalars.Shock==uint.MaxValue,"Six scalar records ordered");
            Check(batch.Updates.Select(u=>Hex(u.Value)).SequenceEqual(reverse?new[]{"01020304","09","08"}:new[]{"01020304","08","09"})&&batch.ThroughRevision==3,"Raw subsequence ordered");
            Check(!source.Acknowledge(seed)&&source.Acknowledge(batch),"Mixed commit invalidates prior receipt; current acknowledges");
            var issued=source.Find(Current(w))!;ulong rawRevision=source.Revision;Check(w.ApplyCustomizationScalars(g,Plan(S(new CreatureHeightUpdate(7))),source)&&ReferenceEquals(issued,source.Find(Current(w)))&&source.Revision==rawRevision,"Scalar-only preserves issued receipt/raw journal");
            Check(source.Acknowledge(issued),"Scalar-only old receipt still current");var scalar=Current(w).Replication!;
            Check(w.ApplyCustomizationScalars(g,Plan(R(),R(0,255),R(0,255)),source),"Opaque repeated empty raw-only");var rawOnly=Current(w);
            Check(ReferenceEquals(scalar.Scalars,rawOnly.Replication!.Scalars)&&scalar.Revision==rawOnly.Replication.Revision,"Raw-only preserves scalar state/revision");
            Check(source.Find(rawOnly)!.Updates.Select(u=>Hex(u.Value)).SequenceEqual(new[]{"","00FF","00FF"}),"Empty/high-byte/repeated raw preserved");
            var journal=w.DrainChanges(g)!;Check(journal.Entries.Single(e=>e.ObjectId==id).Kind.HasFlag(SceneWorldChangeKind.Replication),"Raw-only emits world change");
            receipts.Add(new{Name=reverse?"reverse":"forward",Height=after.Replication.Scalars.Height,Raw=batch.Updates.Select(u=>Hex(u.Value)),RawRevisions=batch.Updates.Select(u=>u.Revision),States=after.Replication.Scalars.States.ToString("X16")});cases.Add(reverse?"reverse-order-six-scalars":"forward-order-six-scalars");
        }
        foreach(string fault in new[]{"raw-count","raw-string","raw-bytes","raw-overflow","scalar-limit","replication-overflow","world-overflow","source-binding","source-closed"})
        using(var w=World(fault=="scalar-limit"?policy with {MaximumScalarUpdates=0}:fault=="replication-overflow"?policy with {RevisionSeed=ulong.MaxValue-1}:policy,
            fault=="world-overflow"?new SceneWorldChangePolicy(4,ulong.MaxValue-3):null))
        {
            long g=Stage(w,creatureOnly:fault=="world-overflow");using var source=new SceneWorldCustomizationStateStore(w,maximumPendingUpdates:fault=="raw-count"?1:64,maximumStringBytes:fault=="raw-string"?4:1536,maximumRetainedBytes:fault=="raw-bytes"?4:1048576,revisionSeed:fault=="raw-overflow"?ulong.MaxValue-2:0);
            var before=Current(w);var batch=source.Find(before)!;var journal=w.DrainChanges(g)!;ulong revision=source.Revision;
            using var foreignWorld=World();Stage(foreignWorld);using var foreign=new SceneWorldCustomizationStateStore(foreignWorld);
            if(fault=="source-closed")foreign.Dispose();
            var selected=fault is "source-binding" or "source-closed"?foreign:source;
            var plan=Plan(R(1,2,3,4,5),S(new CreatureHeightUpdate(4)),R(6));
            if(fault.EndsWith("overflow"))Reject<OverflowException>(()=>w.ApplyCustomizationScalars(g,plan,selected));else Reject<InvalidDataException>(()=>w.ApplyCustomizationScalars(g,plan,selected));
            Same(w,source,before,batch,journal,revision);cases.Add(fault+"-atomic");
        }
        using(var w=World(changes:new(1)))
        {
            long g=Stage(w,creatureOnly:true);using var source=new SceneWorldCustomizationStateStore(w);var before=Current(w);var raw=source.Find(before)!;w.AcknowledgeChanges(w.DrainChanges(g)!);w.Accept(g,Bytes("staticCreate"));var journal=w.DrainChanges(g)!;
            Reject<InvalidDataException>(()=>w.ApplyCustomizationScalars(g,Plan(R(2),S(new CreatureHeightUpdate(4))),source));Same(w,source,before,raw,journal,source.Revision);cases.Add("world-journal-capacity-atomic");
        }
        using(var w=World())
        {
            long g=Stage(w);using var source=new SceneWorldCustomizationStateStore(w);var before=Current(w);var raw=source.Find(before)!;var journal=w.DrainChanges(g)!;
            Check(!w.ApplyCustomizationScalars(g-1,null!,null!),"Stale before plan/source access");Same(w,source,before,raw,journal,source.Revision);
            Reject<InvalidDataException>(()=>w.ApplyCustomizationScalars(g,new(id+900,0,Plan(R(2)).Updates),source));Same(w,source,before,raw,journal,source.Revision);
            Exception? error=null;var thread=new Thread(()=>{try{w.ApplyCustomizationScalars(g,Plan(R(2)),source);}catch(Exception e){error=e;}});thread.Start();thread.Join();Check(error is InvalidOperationException,"Foreign owner rejected");Same(w,source,before,raw,journal,source.Revision);cases.Add("stale-target-thread-atomic");
            Check(w.ApplyCustomizationScalars(g,Plan(R(8),S(new CreatureHeightUpdate(4))),source),"Lifecycle seed write");w.Accept(g,Baseline(new byte[]{7}));var reseed=source.Find(Current(w))!;Check(reseed.Updates.Select(u=>Hex(u.Value)).SequenceEqual(new[]{"01020304","08","07"})&&!source.Acknowledge(raw),"Reseed historic ordered journal");
            w.Accept(g,Bytes("destroy"));Reject<InvalidDataException>(()=>w.ApplyCustomizationScalars(g,Plan(R(2)),source));w.Accept(g,Bytes("create"));w.Accept(g,Baseline(new byte[]{6}));w.Accept(g,Bytes("end"));
            Check(w.ApplyCustomizationScalars(g,Plan(S(new CreatureHeightUpdate(9)),R(5)),source),"New incarnation atomic seed/write");var recreated=source.Find(Current(w))!;Check(recreated.Incarnation!=reseed.Incarnation&&recreated.Updates.Select(u=>Hex(u.Value)).SequenceEqual(new[]{"06","05"})&&!source.Acknowledge(reseed),"New incarnation excludes stale raw state");
            w.BeginSelection(id);Check(!w.ApplyCustomizationScalars(g,Plan(R(2)),source)&&source.Count==0,"Generation stale no application");cases.Add("reseed-destroy-recreate-generation");
        }
        foreach(var mode in new[]{SceneWorldReplicationMode.CreatureView3Scalars,SceneWorldReplicationMode.CreatureView3Mixed})
        using(var w=World(policy with {Mode=mode}))
        {
            long g=Stage(w);using var source=new SceneWorldCustomizationStateStore(w);var baseline=Current(w).Replication!;Check(w.ApplyCustomizationScalars(g,Plan(R(2),S(new CreatureHeightUpdate(5))),source),"Mode supports scalar/raw");Check(Current(w).Replication!.Scalars.Height==5,"Mode height");cases.Add("mode-"+mode);
        }
        using(var w=World())
        {
            long g=Stage(w);var before=Current(w);using var unseeded=new SceneWorldCustomizationStateStore(w,maximumPendingUpdates:1);var journal=w.DrainChanges(g)!;
            Reject<InvalidDataException>(()=>w.ApplyCustomizationScalars(g,Plan(S(new CreatureHeightUpdate(7)),R(8)),unseeded));
            Check(unseeded.Count==0&&unseeded.Revision==0&&Current(w).Replication!.Revision==before.Replication!.Revision&&w.IsCurrent(journal),"Unseeded failure has no prefix in either subsystem");
            using var closed=new SceneWorldCustomizationStateStore(w);closed.Dispose();Reject<InvalidDataException>(()=>w.ApplyCustomizationScalars(g,Plan(S(new CreatureHeightUpdate(7))),closed));
            Check(Current(w).Replication!.Revision==before.Replication!.Revision&&w.IsCurrent(journal),"Scalar-only validates closed same-world source before commit");cases.Add("unseeded-closed-source-atomic");
            using var source=new SceneWorldCustomizationStateStore(w,maximumObjects:1);var issued=source.Find(Current(w))!;ulong revision=source.Revision;
            var create=SceneObjectLifecycleCodec.DecodeCreate(Bytes("create"));ulong other=id+100;
            w.Accept(g,SceneObjectLifecycleCodec.EncodeCreate(create with {ObjectId=other}));w.Accept(g,Baseline(new byte[]{7},other));w.Accept(g,SceneObjectLifecycleCodec.EncodeEndBaselines(new(other)));
            var otherBefore=w.FindReplication(g,other)!;var all=w.DrainChanges(g)!;
            Reject<InvalidDataException>(()=>w.ApplyCustomizationScalars(g,new(other,0,Plan(S(new CreatureHeightUpdate(7)),R(8)).Updates),source));
            Check(w.FindReplication(g,other)!.Revision==otherBefore.Revision&&w.IsCurrent(all)&&ReferenceEquals(issued,source.Find(Current(w)))&&source.Revision==revision,"Global actor budget rejects both sides atomically");cases.Add("source-actor-capacity-atomic");
        }
        foreach(ushort first in new ushort[]{4,11,12,13,14,15,16})
        using(var w=World())
        {
            long g=Stage(w);using var source=new SceneWorldCustomizationStateStore(w);int decodes=0,downstream=0;
            using var router=new SceneWorldCustomizationScalarApplicationRouter(w,source,e=>{decodes++;return Plan(S(new CreatureHeightUpdate(4)),R(8));},(generation,message)=>{downstream++;return w.Accept(generation,message.Span);});
            Check(!router.Accept(g-1,new byte[]{0})&&decodes==0,"Router stale before parse");Check(router.Accept(g,Wire(id,first))&&decodes==1&&Current(w).Replication!.Scalars.Height==4,"Router routes frozen union candidate");
            Check(router.Accept(g,Bytes("staticDelta"))&&downstream==1&&decodes==1,"Other family downstream");router.Dispose();Check(!source.IsClosed&&w.State!=ZoneSceneTransitionState.Closed,"Router only releases borrows");cases.Add("router-first-"+first);
        }
        foreach(string fault in new[]{"throw","null","target","count","world","ack","source","reentry","dispose","generation"})
        using(var w=World())
        {
            long g=Stage(w);using var source=new SceneWorldCustomizationStateStore(w);var seed=source.Find(Current(w))!;var initial=Current(w).Replication!;ulong rawRevision=source.Revision;
            SceneWorldCustomizationScalarApplicationRouter? router=null;
            router=new(w,source,e=>{
                if(fault=="throw")throw new InvalidDataException("Injected decoder fault");
                if(fault=="null")return null!;
                if(fault=="world")w.Accept(g,Bytes("composite"));
                if(fault=="ack")w.AcknowledgeChanges(w.DrainChanges(g)!);
                if(fault=="source")source.Clear();
                if(fault=="reentry")try{router!.Accept(g,Wire(id));}catch(InvalidOperationException){}
                if(fault=="dispose")try{router!.Dispose();}catch(InvalidOperationException){}
                if(fault=="generation")w.BeginSelection(id);
                return new(fault=="target"?id+1:id,fault=="count"?(ushort)0:(ushort)65535,Plan(R(9),S(new CreatureHeightUpdate(100))).Updates);
            });
            Reject<Exception>(()=>router.Accept(g,Wire(id)));Reject<ObjectDisposedException>(()=>router.Accept(g,Wire(id)));
            Check(!source.IsClosed,"Decoder failure does not close borrowed source");
            if(fault is not ("world" or "generation"))Check(Current(w).Replication!.Revision==initial.Revision&&ReferenceEquals(Current(w).Replication!.Scalars,initial.Scalars),"Failed callback does not commit scalar plan");
            if(fault is not ("source" or "generation"))Check(source.Revision==rawRevision&&ReferenceEquals(seed,source.Find(Current(w))),"Failed callback does not commit raw plan");
            cases.Add("decoder-"+fault);
        }
        foreach(string fault in new[]{"throw","source","reentry","generation"})
        using(var w=World())
        {
            long g=Stage(w);using var source=new SceneWorldCustomizationStateStore(w);source.Find(Current(w));SceneWorldCustomizationScalarApplicationRouter? router=null;
            router=new(w,source,e=>Plan(R(9)),(generation,message)=>{
                if(fault=="throw")throw new InvalidDataException("Downstream fault");
                if(fault=="source")source.Clear();
                if(fault=="reentry")try{router!.Accept(g,Wire(id));}catch(InvalidOperationException){}
                if(fault=="generation")w.BeginSelection(id);
                return false;
            });
            Reject<Exception>(()=>router.Accept(g,Bytes("staticDelta")));Reject<ObjectDisposedException>(()=>router.Accept(g,Wire(id)));Check(!source.IsClosed,"Downstream cleanup borrows");cases.Add("downstream-"+fault);
        }
        using(var w=World())
        {
            long g=Stage(w);using var source=new SceneWorldCustomizationStateStore(w);int calls=0;ReadOnlyMemory<byte> memory=default;
            using(var router=new SceneWorldCustomizationScalarApplicationRouter(w,source,e=>{calls++;return Plan(R(7));},(generation,message)=>{memory=message;return false;}))
            {
                var original=Bytes("staticDelta");var copy=original.ToArray();Check(!router.Accept(g,original)&&memory.Span.ToArray().All(b=>b==0)&&original.SequenceEqual(copy),"Borrowed downstream memory cleared caller unchanged");
                Exception? error=null;var thread=new Thread(()=>{try{router.Accept(g,Wire(id));}catch(Exception e){error=e;}});thread.Start();thread.Join();Check(error is InvalidOperationException&&router.Accept(g,Wire(id))&&calls==1,"Foreign thread does not close router");
            }
            using var malformed=new SceneWorldCustomizationScalarApplicationRouter(w,source,e=>{calls++;return Plan(R(7));});var bad=Wire(id);bad[19]=255;Reject<InvalidDataException>(()=>malformed.Accept(g,bad));Check(calls==1,"Malformed outer before decoder");cases.Add("router-memory-thread-outer");
        }
        Check(loaderCalls==0,"No asset preparation or CPU jobs");
        File.WriteAllText(exportPath,JsonSerializer.Serialize(new {Checks=checks,Cases=cases,LoaderCalls=loaderCalls,Receipts=receipts},new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine($"R22_SCALAR_STATE_PASS checks={checks} cases={cases.Count}");
    }
}
