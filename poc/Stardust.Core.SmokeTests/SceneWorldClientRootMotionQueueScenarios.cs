using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

namespace Stardust.Core.SmokeTests;

public static class SceneWorldClientRootMotionQueueScenarios
{
    public sealed record Case(string Name,uint[] RowBits,uint[] PositionBits,uint SpeedBits,uint Flags,uint Counter,uint Timestamp,string Hex);
    public sealed record Peer(Dictionary<string,string> Applications,string[] Initial,Case[] Cases);
    private static int checks;
    private static void Check(bool value,string why){checks++;if(!value)throw new Exception(why);}
    private static void Reject<T>(Action action) where T:Exception
    {try{action();}catch(T){checks++;return;}throw new Exception("Expected "+typeof(T).Name);}
    private static float F(uint bits)=>BitConverter.UInt32BitsToSingle(bits);
    public static void Run(string peerPath,string outputPath)
    {
        checks=0;var peer=JsonSerializer.Deserialize<Peer>(File.ReadAllBytes(peerPath))!;
        byte[] Bytes(string key)=>Convert.FromHexString(peer.Applications[key]);
        ulong id=SceneObjectLifecycleCodec.DecodeCreate(Bytes("humanCreate")).ObjectId,terminalId=SceneObjectLifecycleCodec.DecodeCreate(Bytes("terminalCreate")).ObjectId;
        var catalog=ObjectTemplateCatalogProfile.Decode(File.ReadAllBytes("poc/preview-profiles/scene-object-catalog.json"));
        using var bankFile=File.OpenRead("poc/preview-profiles/human-locomotion.json");var bank=SkeletalAnimationBankProfile.Load(bankFile);
        var options=new Dictionary<SceneObjectResourceRecipe,SceneObjectResourceOptions>{{SceneObjectResourceRecipe.CreatureDetail0,new SceneCreatureResourceOptions(new("appearance/lat/all_m.lat","emt_standing_idle_ambient_1_ag","798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B"),bank,true)},{SceneObjectResourceRecipe.StaticAppearance,new SceneStaticResourceOptions(SceneStaticMaterialPolicy.RequireComplete,null)}};
        int loads=0;
        SceneWorldCoordinator World()=>new(catalog,options,(q,t)=>{loads++;return Task.FromException<SceneObjectResources>(new Exception("No resources expected"));},SceneCreateZeroQuaternionPolicy.Reject,4,2,32,8192,16,4096,1024,
            new(SceneWorldReplicationMode.CreatureView3MixedComponents,new(1024,64,64,16,512),16,1024,new(1024,32,24,64,64,16),CreatureWoundsApplicationPolicy.LegacyBounded,new(1024,32,64,64,16),new(16,16)));
        long Stage(SceneWorldCoordinator world){long g=world.BeginSelection(id).Generation;foreach(string key in peer.Initial)world.Accept(g,Bytes(key));world.CompleteMinimumSceneBarrier(g,true);return g;}
        SceneWorldObjectSnapshot Snapshot(SceneWorldCoordinator world)=>world.Snapshots().Single(s=>s.Object.Handle.ObjectId==id);
        (SceneWorldObjectSnapshot Snapshot,SceneWorldObjectMotionSnapshot Motion) Publish(SceneWorldCoordinator world,SceneWorldObjectMotionStore store,Case item)
        {float[] a=item.RowBits.Select(F).ToArray();var pose=new AppearanceTransform(new(a[0],a[1],a[2]),new(a[3],a[4],a[5]),new(a[6],a[7],a[8]),new(F(item.PositionBits[0]),F(item.PositionBits[1]),F(item.PositionBits[2])));var snapshot=Snapshot(world);Check(store.PublishLocal(snapshot,pose,F(item.SpeedBits)),"Actual current local row");return(snapshot,store.Find(snapshot)!);}
        void Put(SceneWorldClientRootMotionQueue queue,(SceneWorldObjectSnapshot Snapshot,SceneWorldObjectMotionSnapshot Motion) row,Case item)=>queue.Enqueue(row.Snapshot,row.Motion,item.Flags,item.Counter,item.Timestamp);
        var fifo=new List<object>();var gates=new Dictionary<string,bool>();
        using(var world=World())
        {
            long g=Stage(world);using var store=new SceneWorldObjectMotionStore(world);using var queue=new SceneWorldClientRootMotionQueue(world,store,new(100,100),3,186);
            foreach(var item in peer.Cases){var row=Publish(world,store,item);var batch=world.DrainChanges(g)!;int entries=batch.Entries.Count;Put(queue,row,item);Check(world.IsCurrent(batch)&&world.DrainChanges(g)!.Entries.Count==entries&&ReferenceEquals(row.Motion,store.Find(row.Snapshot)),"Queue does not acknowledge world or publish motion");}
            Check(queue.Count==3&&queue.QueuedBytes==186,"Exact full capacity");var current=Snapshot(world);var held=store.Find(current)!;
            Reject<InvalidDataException>(()=>queue.Enqueue(current,held,0,0,0));Check(queue.Count==3&&queue.QueuedBytes==186,"Count cap preserves prefix");
            Reject<InvalidDataException>(()=>queue.Enqueue(null!,null!,0,0,0));Check(queue.Count==3,"Capacity preflight before Build validation");
            Reject<OperationCanceledException>(()=>queue.TryTake(g,out _,new CancellationToken(true)));Check(queue.Count==3&&queue.QueuedBytes==186,"Precancel take does not prune");
            Check(!queue.TryTake(g-1,out var none)&&none is null&&queue.Count==3,"Foreign generation preserves prefix");
            // New local and authoritative rows in the same incarnation cannot retroactively stale built history.
            store.PublishAuthoritative(current,AppearanceTransform.Identity,10,99);world.Accept(g,Bytes("heightA"));world.Accept(g,SceneObjectLifecycleCodec.EncodeContainment(new(id,terminalId,42)));
            byte[]? transferred=null;
            foreach(var item in peer.Cases){Check(queue.TryTake(g,out var bytes)&&bytes is not null,"FIFO transfer");Check(Convert.ToHexString(bytes!)==item.Hex,"Independent exact frame");fifo.Add(new{item.Name,Hex=Convert.ToHexString(bytes!)});if(transferred is null){transferred=bytes;Array.Fill(transferred!,(byte)0xA5);}else Check(transferred.All(b=>b==0xA5),"Next take cannot clear prior transferred buffer");}
            Check(queue.Count==0&&queue.QueuedBytes==0&&!queue.TryTake(g,out _),"Empty FIFO");queue.Dispose();queue.Dispose();Check(transferred!.All(b=>b==0xA5)&&world.State!=ZoneSceneTransitionState.Closed,"Disposal never clears caller transfers or closes borrowers");Check(queue.Count==0&&queue.QueuedBytes==0,"Disposed counters zero");Reject<ObjectDisposedException>(()=>queue.Enqueue(current,held,0,0,0));Reject<ObjectDisposedException>(()=>queue.TryTake(g,out _));gates["HistoricalSameIncarnation"]=true;
        }
        using(var world=World())
        {
            long g=Stage(world);using var store=new SceneWorldObjectMotionStore(world);using var queue=new SceneWorldClientRootMotionQueue(world,store,new(100,100),4,125);var item=peer.Cases[0];var row=Publish(world,store,item);Put(queue,row,item);Put(queue,row,item);
            Reject<InvalidDataException>(()=>Put(queue,row,item));Check(queue.Count==2&&queue.QueuedBytes==124,"Byte cap independent of packet cap");
            Check(queue.TryTake(g,out var packet)&&Convert.ToHexString(packet!)==item.Hex,"Byte cap retains first frame");queue.Dispose();Check(Convert.ToHexString(packet!)==item.Hex&&store.IsCurrent(row.Motion),"Retained drain leaves transferred bytes and store intact");gates["ByteCapAndDispose"]=true;
        }
        using(var world=World())
        {
            long g=Stage(world);using var store=new SceneWorldObjectMotionStore(world);using var queue=new SceneWorldClientRootMotionQueue(world,store,new(100,100));var first=peer.Cases[0];var next=peer.Cases[1];Put(queue,Publish(world,store,first),first);
            world.Accept(g,Bytes("humanDestroy"));world.Accept(g,Bytes("humanCreate"));world.Accept(g,Bytes("humanBaseline"));world.Accept(g,Bytes("humanEnd"));Put(queue,Publish(world,store,next),next);
            Check(queue.Count==2&&queue.TryTake(g,out var bytes)&&Convert.ToHexString(bytes!)==next.Hex&&queue.Count==0&&queue.QueuedBytes==0,"Bounded stale-prefix discard then one replacement frame");gates["ReincarnationDiscard"]=true;
            Put(queue,Publish(world,store,first),first);world.Accept(g,Bytes("humanDestroy"));Check(!queue.TryTake(g,out _)&&queue.Count==0&&queue.QueuedBytes==0,"Destroyed all-stale discard");gates["DestroyedDiscard"]=true;
            g=Stage(world);Put(queue,Publish(world,store,first),first);long newer=Stage(world);Put(queue,Publish(world,store,next),next);Check(!queue.TryTake(g,out _)&&queue.Count==2,"Old supplied generation cannot prune current world queue");
            Check(queue.TryTake(newer,out bytes)&&Convert.ToHexString(bytes!)==next.Hex&&queue.Count==0,"Current generation discards previous scene prefix");gates["GenerationDiscard"]=true;
        }
        using(var world=World())using(var foreign=World())
        {
            long g=Stage(world);Stage(foreign);using var store=new SceneWorldObjectMotionStore(world);using var other=new SceneWorldObjectMotionStore(foreign);var item=peer.Cases[0];var row=Publish(world,store,item);var outside=Publish(foreign,other,item);
            Reject<ArgumentException>(()=>new SceneWorldClientRootMotionQueue(world,other,new(100,100)));
            foreach(int cap in new[]{-1,0,4097})Reject<ArgumentOutOfRangeException>(()=>new SceneWorldClientRootMotionQueue(world,store,new(100,100),cap,496));
            foreach(int cap in new[]{-1,0,61,253953})Reject<ArgumentOutOfRangeException>(()=>new SceneWorldClientRootMotionQueue(world,store,new(100,100),8,cap));
            Reject<ArgumentOutOfRangeException>(()=>new SceneWorldClientRootMotionQueue(world,store,new(float.NaN,100)));
            using var queue=new SceneWorldClientRootMotionQueue(world,store,new(100,100));Put(queue,row,item);
            Reject<InvalidDataException>(()=>Put(queue,outside,item));Reject<InvalidDataException>(()=>queue.Enqueue(row.Snapshot,row.Motion with{},0,0,0));Reject<OperationCanceledException>(()=>queue.Enqueue(row.Snapshot,row.Motion,0,0,0,new CancellationToken(true)));
            var authored=row;store.PublishAuthoritative(row.Snapshot,AppearanceTransform.Identity,1,0);Reject<InvalidDataException>(()=>queue.Enqueue(row.Snapshot,store.Find(row.Snapshot)!,0,0,0));
            var prior=store.Find(row.Snapshot);Reject<ArgumentOutOfRangeException>(()=>store.PublishLocal(row.Snapshot,AppearanceTransform.Identity,-1));Reject<InvalidDataException>(()=>store.PublishLocal(row.Snapshot,AppearanceTransform.Identity with{RowX=Vector3.Zero},0));Check(ReferenceEquals(prior,store.Find(row.Snapshot)),"Store invalid-value boundary, no queue effects");
            store.PublishLocal(row.Snapshot,AppearanceTransform.Identity with{Translation=new(101,0,0)},0);Reject<InvalidDataException>(()=>queue.Enqueue(row.Snapshot,store.Find(row.Snapshot)!,0,0,0));
            store.PublishLocal(row.Snapshot,AppearanceTransform.Identity,101);Reject<InvalidDataException>(()=>queue.Enqueue(row.Snapshot,store.Find(row.Snapshot)!,0,0,0));
            store.PublishLocal(row.Snapshot,AppearanceTransform.Identity,0);var staleMotion=store.Find(row.Snapshot)!;world.Accept(g,Bytes("heightA"));Reject<InvalidDataException>(()=>queue.Enqueue(row.Snapshot,staleMotion,0,0,0));
            Check(queue.Count==1&&queue.QueuedBytes==62,"All failed enqueue preserves built prefix");
            Exception? error=null;var thread=new Thread(()=>{try{queue.TryTake(g,out _);}catch(Exception e){error=e;}});thread.Start();thread.Join();Check(error is InvalidOperationException&&queue.Count==1,"Owner-thread prefix");
            error=null;var dispose=new Thread(()=>{try{queue.Dispose();}catch(Exception e){error=e;}});dispose.Start();dispose.Join();Check(error is InvalidOperationException&&queue.Count==1,"Foreign disposal cannot drain");
            error=null;var enqueueThread=new Thread(()=>{try{Put(queue,authored,item);}catch(Exception e){error=e;}});enqueueThread.Start();enqueueThread.Join();Check(error is InvalidOperationException&&queue.Count==1,"Foreign enqueue cannot build or mutate prefix");
            store.Dispose();Check(!queue.TryTake(g,out _)&&queue.Count==1,"Closed store preserves prefix until owner disposal");Reject<InvalidDataException>(()=>Put(queue,authored,item));world.Close();Check(!queue.TryTake(world.Generation,out _)&&queue.Count==1,"Closed world preserves prefix until owner disposal");gates["FailurePrefix"]=true;
        }
        Check(loads==0,"No resources or sends");File.WriteAllText(outputPath,JsonSerializer.Serialize(new{Fifo=fifo,Gates=gates,Checks=checks,ResourceLoads=loads}));Console.WriteLine("PASS outbound queue checks="+checks);
    }
}
