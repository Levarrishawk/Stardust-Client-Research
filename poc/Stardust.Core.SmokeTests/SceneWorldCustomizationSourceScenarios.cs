using System.Buffers.Binary;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

namespace Stardust.Core.SmokeTests;

/// <summary>Real accepted world, independently authored packed attempts, fake single-consumer journal/projector.</summary>
public static class SceneWorldCustomizationSourceScenarios
{
    private const ulong Human=0xF122334455667788,Other=0xE122334455667799;
    private static readonly ShaderCustomizationKey A=new("synthetic_a",false),B=new("synthetic_b",true);
    public static void Run(string peerPath,string catalogPath,string? exportPath=null)
    {
        int assertions=0;void Check(bool value,string why){if(!value)throw new Exception(why);assertions++;}
        void Reject<T>(Action action)where T:Exception{try{action();}catch(T){assertions++;return;}throw new Exception("Expected "+typeof(T).Name);}
        var peer=JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllBytes(peerPath))!;
        var recipe=ObjectTemplateCatalogProfile.Decode(File.ReadAllBytes(catalogPath)).Require(2937962913);var catalog=new ObjectTemplateCatalog(new[]{recipe});
        const string pin="AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
        var idle=new LogicalAnimationClipSelection("appearance/lat/synthetic.lat","idle",pin);
        var bank=new Dictionary<string,LogicalAnimationClipSelection>{{"walk",idle},{"run",idle}};
        SceneWorldCoordinator World()=>new(catalog,idle,bank,(q,t)=>throw new Exception("Source test must not load assets"),SceneCreateZeroQuaternionPolicy.Reject,
            replicationPolicy:new(SceneWorldReplicationMode.CreatureView3Scalars,new(1024,1024,1024,128,16384),16,16384));
        void Accept(SceneWorldCoordinator world,string name,ulong? id=null)
        {
            var bytes=Convert.FromHexString(peer[name]);if(id is not null)BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(6),id.Value);
            Check(world.Accept(world.Generation,bytes),"accepted world fixture "+name);Array.Clear(bytes);
        }
        SceneWorldObjectSnapshot Snapshot(SceneWorldCoordinator world,ulong id=Human)=>world.Snapshots().Single(s=>s.Object.Handle.ObjectId==id);
        void Stage(SceneWorldCoordinator world)
        {
            long generation=world.BeginSelection(Human).Generation;Accept(world,"start");foreach(string suffix in new[]{"create","baseline","end"})Accept(world,"human_"+suffix);
            Check(world.CompleteMinimumSceneBarrier(generation,true)is not null,"minimum scene barrier");Accept(world,"ready");
        }
        ReplicationByteString Wire(params byte[] bytes)=>ReplicationByteString.Create(bytes);
        // Literal fixtures independent of the production encoder. v1/count and exact FF03 EOS.
        var empty=Wire();var count0=Wire(1,0,255,3);var three=Wire(1,1,1,3,255,3);var four=Wire(1,1,1,4,255,3);
        var seven=Wire(1,1,1,7,255,3);var eight=Wire(1,1,1,8,255,3);var invalid=Wire(1,1,1,255,2,255,3);
        int seeds=0,projections=0;Action? seedAction=null,projectAction=null;int projectionMode=0;
        var seedValues=new Dictionary<ShaderCustomizationKey,int>{{A,1},{B,2}};Dictionary<ShaderCustomizationKey,int>? returned=null;
        IReadOnlyDictionary<ShaderCustomizationKey,int> Seed(SceneWorldObjectSnapshot snapshot){seeds++;seedAction?.Invoke();return seedValues;}
        IReadOnlyDictionary<ShaderCustomizationKey,int> Project(PackedCustomizationSnapshot packed,IReadOnlyDictionary<ShaderCustomizationKey,int> values)
        {
            projections++;projectAction?.Invoke();var result=values.ToDictionary(p=>p.Key,p=>p.Value);
            foreach(var record in packed.Records)
            {
                var key=record.Id switch{1=>A,2=>B,_=>throw new InvalidDataException("Synthetic unknown ID")};var raw=record.CopyRawValue();
                int value=raw.Length==1?raw[0]:BinaryPrimitives.ReadInt16LittleEndian(raw);if(value is >=0 and <10)result[key]=value;
            }
            if(projectionMode==1)result.Remove(B);if(projectionMode==2)result[new("extra",false)]=0;
            if(projectionMode==3)throw new InvalidDataException("Synthetic projection failure");if(projectionMode==4)return null!;
            returned=result;return result;
        }
        using var world=World();Stage(world);var rawSource=new Journal();
        using var adapter=new SceneWorldCreatureCustomizationSourceAdapter(world,rawSource,Seed,Project);
        var freshA=Snapshot(world);var freshB=Snapshot(world);
        Check(!ReferenceEquals(freshA.Replication,freshB.Replication)&&world.IsCurrent(freshA.Replication!)&&world.IsCurrent(freshB.Replication!),"fresh outer wrappers retain identical current underlying state");
        void Batch(ulong through,params (ulong,ReplicationByteString)[] rows)=>rawSource.Set(Snapshot(world).Object.Incarnation,through,rows);
        SceneWorldCreatureCustomizationSnapshot? Find()=>adapter.Find(Snapshot(world));
        Batch(2,(1,empty),(2,count0));Check(Find() is null&&seeds==0&&projections==0&&rawSource.Acks==1,"empty/count0 do not seed or reset: "+adapter.LastFailure+" seed="+seeds+" project="+projections+" ack="+rawSource.Acks);
        Batch(3,(3,three));var first=Find()!;Check(first.Revision==3&&first.Values[A]==3&&first.Values[B]==2&&seeds==1,"explicit full seed and first projection");
        seedValues[A]=9;returned![A]=9;Check(first.Values[A]==3&&first.Values[B]==2,"seed/projected return ownership");
        Batch(4,(4,three));var same=Find()!;Check(same.Revision==4&&same.Values[A]==3&&!ReferenceEquals(same,first),"same-value notification revision preserved");
        var duplicates=Wire(1,3,1,4,1,7,1,255,2,255,3);Batch(5,(5,duplicates));Check(Find()!.Values[A]==7,"ordered duplicates preserve earlier valid value before invalid attempt");
        Batch(7,(6,three),(7,invalid));var applied=Find()!;Check(applied.Revision==7&&applied.Values[A]==3,"all packets applied; last invalid range does not erase earlier write");
        Batch(9,(8,empty),(9,count0));Check(ReferenceEquals(Find(),applied)&&seeds==1,"empty updates retain prior source revision");
        Check(ReferenceEquals(Find(),applied)&&rawSource.Batch!.Updates.Count==0,"acknowledged empty journal retains full values");
        int acks=rawSource.Acks;
        Batch(8,(8,four));Check(ReferenceEquals(Find(),applied)&&adapter.LastFailure is not null&&rawSource.Acks==acks,"rewind rejected");
        Batch(9,(9,four));Check(ReferenceEquals(Find(),applied)&&adapter.LastFailure is not null&&rawSource.Acks==acks,"same revision mutation/replay rejected");
        Batch(10,(10,four));rawSource.AckResult=false;Check(ReferenceEquals(Find(),applied)&&rawSource.Batch!.Updates.Count==1&&adapter.CachedObjects==1,"failed exact ack publishes/cache changes nothing");
        rawSource.AckResult=true;rawSource.OnAck=()=>returned![A]=9;var ten=Find()!;rawSource.OnAck=null;Check(ten.Revision==10&&ten.Values[A]==4,"projection owned before ack callback mutation");
        Batch(11,(11,seven));int calls=0;rawSource.OnFind=()=>{if(++calls==2)Batch(12,(11,seven),(12,eight));};
        Check(ReferenceEquals(Find(),ten)&&adapter.LastFailure is not null&&rawSource.Batch!.ThroughRevision==12,"source batch identity race rejects before ack");rawSource.OnFind=null;
        var twelve=Find()!;Check(twelve.Revision==12&&twelve.Values[A]==8,"ordered replacement batch recovers");
        rawSource.Batch=new(Snapshot(world).Object.Incarnation,12,ReplicationByteString.Create(eight.CopyBytes(),true),Array.Empty<SceneWorldRawCustomizationUpdate>());
        Check(ReferenceEquals(Find(),twelve)&&adapter.LastFailure is not null,"same raw revision extended-length mutation rejected");
        rawSource.Batch=new(Snapshot(world).Object.Incarnation,13,ReplicationByteString.Create(three.CopyBytes(),true),new[]{new SceneWorldRawCustomizationUpdate(13,three)});
        Check(ReferenceEquals(Find(),twelve)&&adapter.LastFailure is not null,"final update/current extended-length flags must agree");
        Batch(13,(13,three));rawSource.AfterAck=()=>rawSource.Batch=new(Snapshot(world).Object.Incarnation,13,ReplicationByteString.Create(three.CopyBytes(),true),Array.Empty<SceneWorldRawCustomizationUpdate>());
        Check(ReferenceEquals(Find(),twelve)&&adapter.LastFailure is not null,"post-ack metadata race cannot publish or change cache");rawSource.AfterAck=null;
        rawSource.Batch=null;Check(ReferenceEquals(Find(),twelve),"missing raw source retains last supplied values");
        rawSource.Batch=new(Snapshot(world).Object.Incarnation,13,three,Array.Empty<SceneWorldRawCustomizationUpdate>());
        Check(ReferenceEquals(Find(),twelve)&&adapter.LastFailure is not null,"unseen externally acknowledged revision rejected");
        rawSource.Batch=new(Snapshot(world).Object.Incarnation,14,three,new[]{new SceneWorldRawCustomizationUpdate(13,three)});
        Check(ReferenceEquals(Find(),twelve)&&adapter.LastFailure is not null,"through revision must close the journal");
        rawSource.Batch=new(Snapshot(world).Object.Incarnation,13,four,new[]{new SceneWorldRawCustomizationUpdate(13,three)});
        Check(ReferenceEquals(Find(),twelve)&&adapter.LastFailure is not null,"current bytes must match final ordered update");
        foreach(var wire in new[]{Wire(2,0,255,3),Wire(1,1,1),Wire(1,1,0,3,255,3),Wire(1,0,255,3,0),ReplicationByteString.Create(new byte[1537])})
        {Batch(13,(13,wire));acks=rawSource.Acks;Check(ReferenceEquals(Find(),twelve)&&adapter.LastFailure is not null&&rawSource.Acks==acks&&rawSource.Batch!.Updates.Count==1,"malformed packed transaction remains unacked");}
        // Even a valid earlier packet rolls back when a later packet is structurally malformed.
        Batch(14,(13,three),(14,Wire(1,1,1)));Check(ReferenceEquals(Find(),twelve)&&adapter.LastFailure is not null,"batch structural failure is explicitly atomic");
        foreach(int bad in new[]{1,2,3,4}){projectionMode=bad;Batch(15,(15,three));acks=rawSource.Acks;Check(ReferenceEquals(Find(),twelve)&&rawSource.Acks==acks&&adapter.LastFailure is not null,"bad projection retains exact full key closure");}projectionMode=0;
        Batch(16,(16,Wire(1,1,3,1,255,3)));Check(ReferenceEquals(Find(),twelve)&&adapter.LastFailure is not null,"unresolved fake ID has no guessed projection");
        Batch(17,(17,Wire(1,1,129,255,2,255,2,255,3)));Check(Find()!.Values[A]==8,"two-byte negative rejected by synthetic declared range");
        Batch(18,(18,three));var beforeReentry=rawSource.Acks;projectAction=()=>{try{Find();}catch(InvalidOperationException){}};
        Reject<InvalidOperationException>(()=>Find());Check(rawSource.Acks==beforeReentry,"caught reentry still prevents commit/ack");projectAction=null;
        projectAction=()=>{try{adapter.Dispose();}catch(InvalidOperationException){}};Reject<InvalidOperationException>(()=>Find());Check(adapter.CachedObjects==1,"reentrant disposal rejected without ownership loss");projectAction=null;
        Check(Task.Run(()=>{try{Find();return false;}catch(InvalidOperationException){return true;}}).GetAwaiter().GetResult(),"worker Find owner guard");
        Check(Task.Run(()=>{try{adapter.Dispose();return false;}catch(InvalidOperationException){return true;}}).GetAwaiter().GetResult(),"worker Dispose owner guard");
        var stale=Snapshot(world);projectAction=()=>Accept(world,"human_height");Check(adapter.Find(stale) is null&&rawSource.Acks==beforeReentry,"replication changes during projection invalidate transaction");projectAction=null;
        Check(adapter.Find(stale) is null,"stale world snapshot rejected without source call");var eighteen=Find()!;Check(eighteen.Revision==18&&eighteen.Values[A]==3,"current snapshot recovery");
        Batch(19,(19,four));rawSource.OnAck=()=>{world.BeginSelection(Human);rawSource.AckResult=false;};Check(Find() is null&&adapter.CachedObjects==0,"world change inside failed ack prunes stale cache");rawSource.OnAck=null;rawSource.AckResult=true;
        Stage(world);rawSource.Set(Snapshot(world).Object.Incarnation,1,(1,seven));Check(Find()!.Revision==1&&adapter.CachedObjects==1,"new generation resets revision namespace");
        var oldInc=Snapshot(world).Object.Incarnation;Accept(world,"human_destroy");Accept(world,"human_create");Accept(world,"human_baseline");Accept(world,"human_end");
        rawSource.Set(oldInc,2,(2,four));Check(Find() is null&&adapter.CachedObjects==0&&adapter.LastFailure is not null,"recreation prunes old incarnation and rejects foreign journal");
        Batch(1,(1,three));Check(Find()!.Revision==1,"same object ID new incarnation seeds independently");
        // Capacity and prune on a separate consumer/journal, avoiding competing journal consumers.
        var capacityRaw=new Journal();using var capacity=new SceneWorldCreatureCustomizationSourceAdapter(world,capacityRaw,Seed,Project,1);
        capacityRaw.Set(Snapshot(world).Object.Incarnation,1,(1,three));Check(capacity.Find(Snapshot(world)) is not null,"first actor capacity admission");
        foreach(string suffix in new[]{"create","baseline","end"})Accept(world,"human_"+suffix,Other);
        capacityRaw.Set(Snapshot(world,Other).Object.Incarnation,1,(1,seven));int beforeSeeds=seeds;
        Check(capacity.Find(Snapshot(world,Other)) is null&&capacity.LastFailure is not null&&seeds==beforeSeeds&&capacity.CachedObjects==1,"capacity rejection before callbacks");
        Accept(world,"human_destroy");Check(capacity.Find(Snapshot(world,Other))!.Values[A]==7&&capacity.CachedObjects==1,"destroyed actor pruned before new admission");
        var coldRaw=new Journal();coldRaw.Set(Snapshot(world,Other).Object.Incarnation,1,(1,three));coldRaw.Acknowledge(coldRaw.Batch!);
        using var cold=new SceneWorldCreatureCustomizationSourceAdapter(world,coldRaw,Seed,Project);beforeSeeds=seeds;Check(cold.Find(Snapshot(world,Other)) is null&&seeds==beforeSeeds,"cold already-acked last-only bytes do not invent values");
        foreach(var badSeed in new IReadOnlyDictionary<ShaderCustomizationKey,int>[]
        {
            new Dictionary<ShaderCustomizationKey,int>(),new Dictionary<ShaderCustomizationKey,int>{{new("bad name",false),0}},
            new Dictionary<ShaderCustomizationKey,int>{{new(new string('a',257),false),0}},
            Enumerable.Range(0,4097).ToDictionary(i=>new ShaderCustomizationKey("key"+i,false),i=>i)
        })
        {
            var rejectedRaw=new Journal();rejectedRaw.Set(Snapshot(world,Other).Object.Incarnation,1,(1,three));
            using var rejected=new SceneWorldCreatureCustomizationSourceAdapter(world,rejectedRaw,_=>badSeed,Project);
            Check(rejected.Find(Snapshot(world,Other)) is null&&rejected.LastFailure is not null&&rejectedRaw.Acks==0&&rejected.CachedObjects==0,"invalid explicit seed bounded before projection/ack");
        }
        var throwingRaw=new Journal();throwingRaw.Set(Snapshot(world,Other).Object.Incarnation,1,(1,three));
        using(var throwing=new SceneWorldCreatureCustomizationSourceAdapter(world,throwingRaw,_=>throw new InvalidDataException("Seed failure"),Project))
            Check(throwing.Find(Snapshot(world,Other)) is null&&throwingRaw.Acks==0&&throwing.LastFailure is not null,"seed callback failure leaves journal unacked");
        using(var staleSeed=new SceneWorldCreatureCustomizationSourceAdapter(world,throwingRaw,s=>{world.BeginSelection(Human);return seedValues;},Project))
        {
            var old=Snapshot(world,Other);Check(staleSeed.Find(old) is null&&throwingRaw.Acks==0&&staleSeed.CachedObjects==0,"generation mutation during seed rejects before ack");
        }
        Stage(world);foreach(string suffix in new[]{"create","baseline","end"})Accept(world,"human_"+suffix,Other);
        Reject<ArgumentOutOfRangeException>(()=>new SceneWorldCreatureCustomizationSourceAdapter(world,rawSource,Seed,Project,0));
        Reject<ArgumentOutOfRangeException>(()=>new SceneWorldCreatureCustomizationSourceAdapter(world,rawSource,Seed,Project,4097));
        Reject<ArgumentNullException>(()=>new SceneWorldCreatureCustomizationSourceAdapter(world,null!,Seed,Project));
        Reject<ArgumentNullException>(()=>new SceneWorldCreatureCustomizationSourceAdapter(world,rawSource,null!,Project));
        Reject<ArgumentNullException>(()=>new SceneWorldCreatureCustomizationSourceAdapter(world,rawSource,Seed,null!));
        Check(Task.Run(()=>{try{_=new SceneWorldCreatureCustomizationSourceAdapter(world,rawSource,Seed,Project);return false;}catch(InvalidOperationException){return true;}}).GetAwaiter().GetResult(),"constructor serialized world owner guard");
        adapter.Dispose();adapter.Dispose();Check(adapter.CachedObjects==0&&!rawSource.Disposed&&world.Snapshots().Count==2,"dispose clears cache but borrows remain live");Reject<ObjectDisposedException>(()=>adapter.Find(Snapshot(world,Other)));
        var closeRaw=new Journal();var beforeClose=Snapshot(world,Other);closeRaw.Set(beforeClose.Object.Incarnation,1,(1,seven));
        using(var closing=new SceneWorldCreatureCustomizationSourceAdapter(world,closeRaw,Seed,Project))
        {Check(closing.Find(beforeClose) is not null&&closing.CachedObjects==1,"close probe populated");world.Close();Check(closing.Find(beforeClose) is null&&closing.CachedObjects==0&&!closeRaw.Disposed,"closed world prunes all entries without disposing borrow");}
        var result=new{assertions,seeds,projections,ordered_packets=true,duplicate_invalid_old_value=true,atomic_malformed=true,ack_cache_atomic=true,ownership_reentry_freshness=true,generation_prune_capacity=true,no_assets=true};
        if(exportPath is not null)File.WriteAllText(exportPath,JsonSerializer.Serialize(result));Console.WriteLine("customization_source="+JsonSerializer.Serialize(result));Console.WriteLine("PASS: ordered raw journal to supplied full customization source");
    }
    private sealed class Journal:ISceneWorldRawCustomizationSource,IDisposable
    {
        internal SceneWorldRawCustomizationBatch? Batch;internal bool AckResult=true,Disposed;internal int Acks;internal Action? OnFind,OnAck,AfterAck;
        internal void Set(SceneObjectIncarnation incarnation,ulong through,params (ulong Revision,ReplicationByteString Value)[] updates)
        {Batch=new(incarnation,through,updates[^1].Value,updates.Select(p=>new SceneWorldRawCustomizationUpdate(p.Revision,p.Value)).ToArray());}
        public SceneWorldRawCustomizationBatch? Find(SceneWorldObjectSnapshot snapshot){OnFind?.Invoke();return Batch;}
        public bool Acknowledge(SceneWorldRawCustomizationBatch batch)
        {OnAck?.Invoke();if(!AckResult||!ReferenceEquals(Batch,batch))return false;Acks++;Batch=new(batch.Incarnation,batch.ThroughRevision,batch.Current,Array.Empty<SceneWorldRawCustomizationUpdate>());AfterAck?.Invoke();return true;}
        public void Dispose(){Disposed=true;Batch=null;}
    }
}
