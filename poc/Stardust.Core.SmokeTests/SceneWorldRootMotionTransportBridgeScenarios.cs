using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

namespace Stardust.Core.SmokeTests;

public static class SceneWorldRootMotionTransportBridgeScenarios
{
    public sealed record Case(string Name,uint[] RowBits,uint[] PositionBits,uint SpeedBits,uint Flags,uint Counter,uint Timestamp,string Hex);
    public sealed record Peer(Dictionary<string,string> Applications,string[] Initial,Case[] Cases,string Request,string Response,Dictionary<string,string> Wires,Dictionary<string,string> Acks);
    private static int checks;
    private static void Check(bool value,string why){checks++;if(!value)throw new Exception(why);}
    private static void Reject<T>(Action action) where T:Exception
    {try{action();}catch(T){checks++;return;}throw new Exception("Expected "+typeof(T).Name);}
    private static float F(uint bits)=>BitConverter.UInt32BitsToSingle(bits);
    public static void Run(string peerPath,string outputPath)
    {
        checks=0;var peer=JsonSerializer.Deserialize<Peer>(File.ReadAllBytes(peerPath))!;
        byte[] Bytes(string key)=>Convert.FromHexString(peer.Applications[key]);ulong id=SceneObjectLifecycleCodec.DecodeCreate(Bytes("humanCreate")).ObjectId;
        var catalog=ObjectTemplateCatalogProfile.Decode(File.ReadAllBytes("poc/preview-profiles/scene-object-catalog.json"));using var bankFile=File.OpenRead("poc/preview-profiles/human-locomotion.json");var bank=SkeletalAnimationBankProfile.Load(bankFile);
        var options=new Dictionary<SceneObjectResourceRecipe,SceneObjectResourceOptions>{{SceneObjectResourceRecipe.CreatureDetail0,new SceneCreatureResourceOptions(new("appearance/lat/all_m.lat","emt_standing_idle_ambient_1_ag","798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B"),bank,true)},{SceneObjectResourceRecipe.StaticAppearance,new SceneStaticResourceOptions(SceneStaticMaterialPolicy.RequireComplete,null)}};
        int loads=0;
        SceneWorldCoordinator World()=>new(catalog,options,(q,t)=>{loads++;return Task.FromException<SceneObjectResources>(new Exception("No resource jobs"));},SceneCreateZeroQuaternionPolicy.Reject,4,2,32,8192,16,4096,1024,new(SceneWorldReplicationMode.CreatureView3MixedComponents,new(1024,64,64,16,512),16,1024,new(1024,32,24,64,64,16),CreatureWoundsApplicationPolicy.LegacyBounded,new(1024,32,64,64,16),new(16,16)));
        long Stage(SceneWorldCoordinator world){long g=world.BeginSelection(id).Generation;foreach(string key in peer.Initial)world.Accept(g,Bytes(key));world.CompleteMinimumSceneBarrier(g,true);return g;}
        SceneWorldObjectSnapshot Snapshot(SceneWorldCoordinator world)=>world.Snapshots().Single(s=>s.Object.Handle.ObjectId==id);
        void Put(SceneWorldCoordinator world,SceneWorldObjectMotionStore store,SceneWorldClientRootMotionQueue queue,Case item)
        {var a=item.RowBits.Select(F).ToArray();var pose=new AppearanceTransform(new(a[0],a[1],a[2]),new(a[3],a[4],a[5]),new(a[6],a[7],a[8]),new(F(item.PositionBits[0]),F(item.PositionBits[1]),F(item.PositionBits[2])));var snapshot=Snapshot(world);Check(store.PublishLocal(snapshot,pose,F(item.SpeedBits)),"Actual local issued row");queue.Enqueue(snapshot,store.Find(snapshot)!,item.Flags,item.Counter,item.Timestamp);}
        void Establish(SoeClientTransport transport){Check(Convert.ToHexString(transport.BeginSession(0x12345678,496))==peer.Request,"Actual session request");transport.AcceptSessionResponse(Convert.FromHexString(peer.Response));Check(transport.State==SoeClientTransportState.Established,"Synthetic response establishes actual transport");}
        var output=new Dictionary<string,string>();var acknowledgements=new Dictionary<string,string>();
        void PumpOne(string key,SoeClientTransport sender,SoeClientTransport receiver,string application)
        {
            var datagrams=sender.PumpQueuedDatagrams(123);Check(datagrams.Count==1,"One unfragmented offline datagram");byte[] wire=datagrams[0];output.Add(key,Convert.ToHexString(wire));Check(output[key]==peer.Wires[key],"Independent encrypted/CRC wire");
            var received=receiver.AcceptDatagram(wire);Check(received.ApplicationPayloads.Count==1&&Convert.ToHexString(received.ApplicationPayloads[0])==application,"Actual reliable peer recovers owned application");
            Check(received.ControlDatagram is not null,"Actual ACK generated");acknowledgements.Add(key,Convert.ToHexString(received.ControlDatagram!));Check(acknowledgements[key]==peer.Acks[key],"Independent ACK wire");
            Check(sender.AcceptDatagram(received.ControlDatagram!).ReleasedOutboundPackets==1&&sender.RetainedOutboundPackets==0,"Actual ACK releases retained send packet");
            Array.Clear(received.ApplicationPayloads[0]);Array.Clear(wire);Array.Clear(received.ControlDatagram!);
        }
        using(var world=World())
        {
            long g=Stage(world);using var store=new SceneWorldObjectMotionStore(world);using var queue=new SceneWorldClientRootMotionQueue(world,store,new(100,100));foreach(var item in peer.Cases)Put(world,store,queue,item);
            var transport=new SoeClientTransport();var receiver=new SoeClientTransport();using var bridge=new SceneWorldRootMotionTransportBridge(queue,transport);
            Check(!bridge.TryQueueNext(g)&&queue.Count==3&&bridge.AcceptedCount==0,"Offline declines before take");
            Check(Convert.ToHexString(transport.BeginSession(0x12345678,496))==peer.Request,"Begin real session");Check(!bridge.TryQueueNext(g)&&queue.Count==3,"Requested declines before take");transport.AcceptSessionResponse(Convert.FromHexString(peer.Response));Establish(receiver);
            byte[] unrelated=new byte[]{0x12,0x34,0x56};transport.QueueReliableMessage(unrelated,3);Array.Clear(unrelated);Check(!bridge.TryQueueNext(g)&&queue.Count==3&&transport.PendingMessageCount==1&&bridge.OwnedPendingBytes==0,"Any-channel pending backpressure before take");PumpOne("other-channel",transport,receiver,"123456");
            Reject<OperationCanceledException>(()=>bridge.TryQueueNext(g,new CancellationToken(true)));Check(queue.Count==3&&transport.PendingMessageCount==0&&!bridge.IsFaulted,"Precancel no transferred prefix/fault");Check(!bridge.TryQueueNext(g-1)&&queue.Count==3,"Wrong generation no take");
            for(int i=0;i<peer.Cases.Length;i++)
            {
                var batch=world.DrainChanges(g)!;int entries=batch.Entries.Count;
                Check(bridge.TryQueueNext(g)&&bridge.AcceptedCount==(ulong)(i+1)&&bridge.OwnedPendingBytes==0&&queue.Count==2-i&&transport.PendingMessageCount==1,"Actual SOE copy accepted then bridge array cleared/released");
                Check(world.IsCurrent(batch)&&world.DrainChanges(g)!.Entries.Count==entries,"No world batch acknowledgement");Check(!bridge.TryQueueNext(g)&&queue.Count==2-i&&bridge.AcceptedCount==(ulong)(i+1),"Channel-zero pending conservative decline");
                if(i==2){bridge.Dispose();bridge.Dispose();Check(transport.State==SoeClientTransportState.Established&&transport.PendingMessageCount==1&&queue.Count==0&&store.Find(Snapshot(world)) is not null,"Bridge borrower survival and accepted SOE copy retained");}
                PumpOne("root-"+i,transport,receiver,peer.Cases[i].Hex);
            }
            Check(bridge.OwnedPendingBytes==0&&bridge.AcceptedCount==3&&!bridge.IsFaulted,"Disposed historical counters");Reject<ObjectDisposedException>(()=>bridge.TryQueueNext(g));
            Put(world,store,queue,peer.Cases[0]);Check(queue.TryTake(g,out var caller)&&Convert.ToHexString(caller!)==peer.Cases[0].Hex,"Borrowed queue still transfers after bridge disposal");Array.Fill(caller!,(byte)0xA5);Check(transport.PendingMessageCount==0&&transport.RetainedOutboundPackets==0,"Caller mutation isolated from accepted transport history");transport.Close();receiver.Close();
        }
        using(var world=World())
        {
            long g=Stage(world);using var store=new SceneWorldObjectMotionStore(world);using var queue=new SceneWorldClientRootMotionQueue(world,store,new(100,100));Put(world,store,queue,peer.Cases[0]);
            var transport=new SoeClientTransport();var receiver=new SoeClientTransport();Establish(transport);Establish(receiver);using var bridge=new SceneWorldRootMotionTransportBridge(queue,transport);
            world.Accept(g,Bytes("humanDestroy"));world.Accept(g,Bytes("humanCreate"));world.Accept(g,Bytes("humanBaseline"));world.Accept(g,Bytes("humanEnd"));Put(world,store,queue,peer.Cases[1]);
            Check(bridge.TryQueueNext(g)&&queue.Count==0&&bridge.AcceptedCount==1&&!bridge.IsFaulted,"Stale prefix dropped before replacement transfer");PumpOne("replacement",transport,receiver,peer.Cases[1].Hex);
            Put(world,store,queue,peer.Cases[0]);world.Accept(g,Bytes("humanDestroy"));Check(!bridge.TryQueueNext(g)&&queue.Count==0&&bridge.AcceptedCount==1&&!bridge.IsFaulted,"All stale discard, no SOE copy");
            long next=Stage(world);Put(world,store,queue,peer.Cases[0]);Check(!bridge.TryQueueNext(g)&&queue.Count==1,"Old supplied generation preserves prefix");
            Exception? error=null;var thread=new Thread(()=>{try{bridge.TryQueueNext(next);}catch(Exception e){error=e;}});thread.Start();thread.Join();Check(error is InvalidOperationException&&queue.Count==1,"Foreign bridge thread before transfer");
            error=null;var dispose=new Thread(()=>{try{bridge.Dispose();}catch(Exception e){error=e;}});dispose.Start();dispose.Join();Check(error is InvalidOperationException&&queue.Count==1,"Foreign bridge disposal preserves prefix");
            error=null;var construct=new Thread(()=>{try{new SceneWorldRootMotionTransportBridge(queue,transport);}catch(Exception e){error=e;}});construct.Start();construct.Join();Check(error is InvalidOperationException,"Constructor verifies borrowed queue affinity");
            store.Dispose();Check(!bridge.TryQueueNext(next)&&queue.Count==1&&!bridge.IsFaulted,"Closed store decline no transferred fault");world.Close();Check(!bridge.TryQueueNext(world.Generation)&&queue.Count==1&&!bridge.IsFaulted,"Closed world decline no transferred fault");transport.Close();Check(!bridge.TryQueueNext(world.Generation)&&queue.Count==1,"Closed transport decline before queue take");
        }
        using(var world=World())
        {
            long g=Stage(world);using var store=new SceneWorldObjectMotionStore(world);using var queue=new SceneWorldClientRootMotionQueue(world,store,new(100,100));var transport=new SoeClientTransport();Establish(transport);using var bridge=new SceneWorldRootMotionTransportBridge(queue,transport);
            Check(!bridge.TryQueueNext(g)&&bridge.AcceptedCount==0&&!bridge.IsFaulted,"Empty eligible queue");queue.Dispose();Reject<ObjectDisposedException>(()=>bridge.TryQueueNext(g));Check(bridge.OwnedPendingBytes==0&&!bridge.IsFaulted,"Disposed borrowed queue failure before transfer");transport.Close();
        }
        Check(loads==0,"No resource jobs/network sends");File.WriteAllText(outputPath,JsonSerializer.Serialize(new{Wires=output,Acks=acknowledgements,Checks=checks,ResourceLoads=loads,RarePostTakeFaultTested=false}));Console.WriteLine("PASS root transport bridge checks="+checks);
    }
}
