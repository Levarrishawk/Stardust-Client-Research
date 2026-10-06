using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

namespace Stardust.Core.SmokeTests;

public static class SceneWorldOfflineRootMotionDrainScenarios
{
    public sealed record Peer(Dictionary<string,string> Applications,string[] Initial,Dictionary<string,string> Datagrams,Dictionary<string,string> Acks,string Request,string Response,Input[] Cases);
    public sealed record Input(float X,float Y,float Z,float Speed,uint Flags,uint Counter,uint Timestamp);
    private static int checks;
    private static void Check(bool value,string why){checks++;if(!value)throw new Exception(why);}
    private static void Reject<T>(Action action)where T:Exception
    {try{action();}catch(T){checks++;return;}throw new Exception("Expected "+typeof(T).Name);}
    private sealed class Context:IDisposable
    {
        public readonly SceneWorldCoordinator World;
        public readonly SceneWorldObjectMotionStore Store;
        public readonly SceneWorldCombinedMotionApplicationRouter Router;
        public readonly SceneWorldPresentationPump Pump;
        public readonly SoeClientTransport Transport=new();
        public readonly SceneWorldSoeApplicationIngress Ingress;
        public readonly List<(CancellationToken Token,TaskCompletionSource<SceneObjectResources> Completion)> Jobs=new();
        public readonly List<SceneWorldCombinedMotionApplicationResult> Routes=new();
        public readonly long Generation;
        public Context(Peer peer,ObjectTemplateCatalog catalog,Dictionary<SceneObjectResourceRecipe,SceneObjectResourceOptions> options,int pumpCapacity=8,int maxApplications=8,int maxBytes=256)
        {
            World=new(catalog,options,(q,t)=>{var completion=new TaskCompletionSource<SceneObjectResources>();Jobs.Add((t,completion));return completion.Task;},SceneCreateZeroQuaternionPolicy.Reject,4,2,32,8192,16,4096,1024,new(SceneWorldReplicationMode.CreatureView3MixedComponents,new(1024,64,64,16,512),16,1024,new(1024,32,24,64,64,16),CreatureWoundsApplicationPolicy.LegacyBounded,new(1024,32,64,64,16),new(16,16)));
            Store=new(World);Router=new(World,Store,(p,s)=>(AppearanceTransform.Identity,0),SceneCreateZeroQuaternionPolicy.Identity,ObjectControllerTransformDirection.ServerToClient);
            Pump=new(World,(p,s)=>throw new Exception("No completed resources"),new(MaximumQueuedMessages:pumpCapacity,MaximumMessagesPerStep:Math.Min(2,pumpCapacity)),applicationReceiver:(g,b)=>{var result=Router.Receive(g,b.Span);Routes.Add(result);return result.Accepted;});
            Ingress=new(World,Pump,Transport,maxApplications,maxBytes);Generation=World.BeginSelection(0xF122334455667788UL).Generation;
            foreach(string key in peer.Initial)World.Accept(Generation,Convert.FromHexString(peer.Applications[key]));World.CompleteMinimumSceneBarrier(Generation,true);
        }
        public SceneWorldObjectSnapshot Human=>World.Snapshots().Single(s=>s.Object.Handle.ObjectId==0xF122334455667788UL);
        public void Dispose()
        {Ingress.Dispose();Pump.Dispose();foreach(var job in Jobs)job.Completion.TrySetException(new Exception("Synthetic late cancellation"));Pump.DrainCompletion.GetAwaiter().GetResult();Check(Jobs.All(j=>j.Token.IsCancellationRequested&&j.Completion.Task.IsCompleted),"Controlled jobs canceled/observed");Router.Dispose();Store.Dispose();Transport.Close();}
    }
    public static void Run(string peerPath,string outputPath)
    {
        checks=0;var peer=JsonSerializer.Deserialize<Peer>(File.ReadAllBytes(peerPath))!;byte[] Wire(string key)=>Convert.FromHexString(peer.Datagrams[key]);
        var catalog=ObjectTemplateCatalogProfile.Decode(File.ReadAllBytes("poc/preview-profiles/scene-object-catalog.json"));using var bankFile=File.OpenRead("poc/preview-profiles/human-locomotion.json");var bank=SkeletalAnimationBankProfile.Load(bankFile);
        var options=new Dictionary<SceneObjectResourceRecipe,SceneObjectResourceOptions>{{SceneObjectResourceRecipe.CreatureDetail0,new SceneCreatureResourceOptions(new("appearance/lat/all_m.lat","emt_standing_idle_ambient_1_ag","798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B"),bank,true)},{SceneObjectResourceRecipe.StaticAppearance,new SceneStaticResourceOptions(SceneStaticMaterialPolicy.RequireComplete,null)}};
        void Establish(Context c){Check(Convert.ToHexString(c.Transport.BeginSession(0x12345678,496))==peer.Request,"Actual session request");c.Transport.AcceptSessionResponse(Convert.FromHexString(peer.Response));}


        var receipts=new List<SceneWorldOfflineRootMotionDrainReceipt>();var failures=new List<object>();
        void Enqueue(Context c,SceneWorldClientRootMotionQueue queue,int index)
        {
            var input=peer.Cases[index];c.Store.PublishLocal(c.Human,AppearanceTransform.Identity with{Translation=new(input.X,input.Y,input.Z)},input.Speed);
            queue.Enqueue(c.Human,c.Store.Find(c.Human)!,input.Flags,input.Counter,input.Timestamp);
        }
        using(var c=new Context(peer,catalog,options))
        {
            using var queue=new SceneWorldClientRootMotionQueue(c.World,c.Store,new(100,10));
            using var drain=new SceneWorldOfflineRootMotionDrain(c.World,queue,c.Transport,0xA1B2C3D4);
            Enqueue(c,queue,0);
            Check(drain.TryDrainNext(c.Generation,0,Array.Empty<byte>()) is null&&queue.Count==1,"Offline pre-take decline");Establish(c);
            foreach(string key in new[]{"badCrc","wrongSequence","wrongChannel","wrongOpcode","short"})
            {
                Reject<InvalidDataException>(()=>drain.TryDrainNext(c.Generation,0,Wire(key)));
                Check(queue.Count==1&&c.Transport.PendingMessageCount==0&&c.Transport.RetainedOutboundPackets==0&&!drain.IsFaulted&&drain.LastNowMilliseconds==-1,"Invalid ACK before queue take");
            }
            Reject<ArgumentOutOfRangeException>(()=>drain.TryDrainNext(c.Generation,-1,Wire("ack0")));
            Reject<OperationCanceledException>(()=>drain.TryDrainNext(c.Generation,0,Wire("ack0"),new(true)));
            Check(drain.TryDrainNext(c.Generation-1,0,Wire("ack0")) is null&&queue.Count==1,"Stale generation preserves FIFO");
            Exception? error=null;var thread=new Thread(()=>{try{drain.TryDrainNext(c.Generation,0,Wire("ack0"));}catch(Exception e){error=e;}});thread.Start();thread.Join();Check(error is InvalidOperationException&&queue.Count==1,"Foreign owner before transfer");
            error=null;var disposeThread=new Thread(()=>{try{drain.Dispose();}catch(Exception e){error=e;}});disposeThread.Start();disposeThread.Join();Check(error is InvalidOperationException,"Foreign disposal before effects");
            c.Transport.QueueReliableMessage(new byte[]{7},2);
            Check(drain.TryDrainNext(c.Generation,0,Wire("ack0")) is null&&queue.Count==1,"Other-channel pending declines");
            var busy=c.Transport.PumpQueuedDatagrams(0);foreach(var d in busy)Array.Clear(d);
            Check(drain.TryDrainNext(c.Generation,0,Wire("ack0")) is null&&queue.Count==1,"Other-channel retained declines");
            Check(c.Transport.AcceptDatagram(Wire("otherAck")).ReleasedOutboundPackets==1,"Release caller-owned other channel");
            for(int i=1;i<peer.Cases.Length;i++)Enqueue(c,queue,i);
            for(int i=0;i<peer.Cases.Length;i++)
            {
                byte[] ack=Wire("ack"+i);string original=Convert.ToHexString(ack);
                var receipt=drain.TryDrainNext(c.Generation,i*10,ack)!;receipts.Add(receipt);
                Check(receipt.Sequence==i&&receipt.AcceptedCount==(ulong)(i+1)&&drain.BridgeAcceptedCount==(ulong)(i+1),"Actual one-frame sequence/history");
                Check(Convert.ToHexString(ack)==original&&c.Transport.PendingMessageCount==0&&c.Transport.RetainedOutboundPackets==0&&queue.Count==peer.Cases.Length-i-1,"Caller ACK and owners conserved");Array.Clear(ack);
            }
            Check(drain.TryDrainNext(c.Generation,80,Array.Empty<byte>()) is null,"Empty pre-take decline without ACK");
            Reject<ArgumentOutOfRangeException>(()=>drain.TryDrainNext(c.Generation,69,Wire("ack0")));
            string first=receipts[0].ApplicationHex;drain.Dispose();drain.Dispose();
            Check(drain.AcceptedCount==8&&drain.NextSequence==8&&drain.LastNowMilliseconds==70&&ReferenceEquals(drain.LastReceipt,receipts[^1])&&receipts[0].ApplicationHex==first&&c.Store.Find(c.Human) is not null&&c.Transport.State==SoeClientTransportState.Established,"History/string/borrowers survive disposal");
            Reject<ObjectDisposedException>(()=>drain.TryDrainNext(c.Generation,80,Wire("ack0")));
            Enqueue(c,queue,0);Check(queue.TryTake(c.Generation,out var handed),"Borrowed queue still usable");Array.Clear(handed!);
        }
        foreach(string kind in new[]{"sequenceOrigin","seedMismatch","fragmented"})
        using(var c=new Context(peer,catalog,options))
        {
            if(kind=="fragmented"){c.Transport.BeginSession(0x12345678,496);c.Transport.AcceptSessionResponse(Wire("smallResponse"));}else Establish(c);
            using var queue=new SceneWorldClientRootMotionQueue(c.World,c.Store,new(100,10));Enqueue(c,queue,0);
            if(kind=="sequenceOrigin")
            {var prior=c.Transport.CreateReliableDatagram(new byte[]{1},0);Array.Clear(prior);Check(c.Transport.AcceptDatagram(Wire("ack0")).ReleasedOutboundPackets==1,"Prior unrelated channel0 completed");}
            using var drain=new SceneWorldOfflineRootMotionDrain(c.World,queue,c.Transport,kind=="seedMismatch"?0xDEADBEEF:0xA1B2C3D4);
            Reject<InvalidDataException>(()=>drain.TryDrainNext(c.Generation,100,Wire(kind=="seedMismatch"?"alternateSeedAck":"ack0")));
            Check(drain.IsFaulted&&drain.AcceptedCount==0&&drain.BridgeAcceptedCount==1&&drain.LastNowMilliseconds==100&&drain.NextSequence==0&&drain.LastReceipt is null&&queue.Count==0&&c.Transport.PendingMessageCount==0&&c.Transport.RetainedOutboundPackets==(kind=="fragmented"?3:1),"Actual post-transfer mismatch honest retained prefix");
            Reject<InvalidOperationException>(()=>drain.TryDrainNext(c.Generation,110,Wire("ack0")));
            drain.Dispose();Check(c.Transport.RetainedOutboundPackets==(kind=="fragmented"?3:1)&&c.Store.Find(c.Human) is not null,"Faulted drain never closes borrower");
            failures.Add(new{Kind=kind,Retained=c.Transport.RetainedOutboundPackets,BridgeAccepted=drain.BridgeAcceptedCount});
        }
        using(var c=new Context(peer,catalog,options))
        using(var foreign=new Context(peer,catalog,options))
        {
            Establish(c);using var queue=new SceneWorldClientRootMotionQueue(foreign.World,foreign.Store,new(100,10));Enqueue(foreign,queue,0);
            using var drain=new SceneWorldOfflineRootMotionDrain(c.World,queue,c.Transport,0xA1B2C3D4);
            foreign.World.BeginSelection(1);Check(drain.TryDrainNext(c.Generation,0,Wire("ack0")) is null&&queue.Count==1,"Foreign queue generation contract mismatch declines before transfer");
            c.Transport.Close();Check(drain.TryDrainNext(c.Generation,0,Wire("ack0")) is null&&queue.Count==1,"Closed transport pre-take decline");
            c.Pump.Close();Check(drain.TryDrainNext(c.World.Generation,0,Wire("ack0")) is null&&queue.Count==1&&!drain.IsFaulted,"Closed world preserves borrowed queue");
            Reject<ArgumentOutOfRangeException>(()=>new SceneWorldOfflineRootMotionDrain(c.World,queue,c.Transport,0xA1B2C3D4,68));
        }
        File.WriteAllText(outputPath,JsonSerializer.Serialize(new{Checks=checks,Receipts=receipts,Failures=failures,OriginalAssets=0}));Console.WriteLine("PASS offline drain checks="+checks);
    }
}
