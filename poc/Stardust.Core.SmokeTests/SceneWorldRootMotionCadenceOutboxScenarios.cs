using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

namespace Stardust.Core.SmokeTests;

public static class SceneWorldRootMotionCadenceOutboxScenarios
{
    public sealed record Peer(Dictionary<string,string> Applications,string[] Initial,Dictionary<string,string> Datagrams,Dictionary<string,string> Acks,string Request,string Response);
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
        checks=0;var peer=JsonSerializer.Deserialize<Peer>(File.ReadAllBytes(peerPath))!;
        var catalog=ObjectTemplateCatalogProfile.Decode(File.ReadAllBytes("poc/preview-profiles/scene-object-catalog.json"));using var bankFile=File.OpenRead("poc/preview-profiles/human-locomotion.json");var bank=SkeletalAnimationBankProfile.Load(bankFile);
        var options=new Dictionary<SceneObjectResourceRecipe,SceneObjectResourceOptions>{{SceneObjectResourceRecipe.CreatureDetail0,new SceneCreatureResourceOptions(new("appearance/lat/all_m.lat","emt_standing_idle_ambient_1_ag","798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B"),bank,true)},{SceneObjectResourceRecipe.StaticAppearance,new SceneStaticResourceOptions(SceneStaticMaterialPolicy.RequireComplete,null)}};
        void Establish(Context c){Check(Convert.ToHexString(c.Transport.BeginSession(0x12345678,496))==peer.Request,"Actual session request");c.Transport.AcceptSessionResponse(Convert.FromHexString(peer.Response));}


        var frames=new List<object>();var statuses=new List<string>();string? wireHex=null;
        using(var c=new Context(peer,catalog,options))
        {
            Establish(c);c.Store.PublishLocal(c.Human,AppearanceTransform.Identity,0);
            Vector2[] layout=[new(.5f,.5f),new(0,.5f),new(0,0),new(.5f,0),new(1,0),new(1,.5f),new(1,1),new(.5f,1),new(0,1)];
            var points=layout.Select(p=>new Vector3(-100+p.X*200,2,100-p.Y*200)).ToArray();
            var surface=new TerrainPatchSurfaceQuery(new TerrainPatchGeometry(points,Enumerable.Repeat(Vector3.UnitY,9).ToArray(),new uint[9],1,0));
            using var driver=new SceneWorldTerrainMotionDriver(c.World,c.Store,0xF122334455667788UL,new[]{surface},stepMicroseconds:10000);
            using var queue=new SceneWorldClientRootMotionQueue(c.World,c.Store,new(100,10),2,124);
            using var outbox=new SceneWorldRootMotionCadenceOutbox(c.World,c.Store,queue,maxSamples:1000,maxPendingPackets:2,maxPendingBytes:124,counterBase:0xFFFFFFFE,timestampBase:0xFFFFFFF0);
            driver.Submit(0,new(new(0,0,2),null));
            for(int i=1;i<=1000;i++)
            {
                if(i is 301 or 701)driver.Submit((i-1)*10000,new(Vector3.Zero,null));
                if(i==501)driver.Submit(5000000,new(new(0,0,2),null));
                var state=driver.Advance(i*10000);Check(state.StepsThisUpdate==1&&state.FaultType is null,"Actual single driver tick");
                var receipt=outbox.Observe(c.Human,c.Store.Find(c.Human)!,i*10000,5);statuses.Add(receipt.Status.ToString());
                Check(outbox.Samples==i&&outbox.ElapsedMicroseconds==i*10000,"Exact observation frontier");
                if(wireHex is null&&queue.Count>0)
                {
                    using var bridge=new SceneWorldRootMotionTransportBridge(queue,c.Transport);
                    Check(bridge.TryQueueNext(c.Generation)&&bridge.AcceptedCount==1,"Actual outbound bridge admission");
                    var datagram=c.Transport.PumpQueuedDatagrams(0).Single();wireHex=Convert.ToHexString(datagram);
                    Check(SoeDatagramCodec.TryDecode(datagram,0xA1B2C3D4,2,496,out var decoded),"Actual protected frame decode");
                    frames.Add(new{At=i,Hex=Convert.ToHexString(decoded.AsSpan(4))});Array.Clear(decoded);Array.Clear(datagram);
                    Check(c.Transport.AcceptDatagram(Convert.FromHexString(peer.Acks["correction"])).ReleasedOutboundPackets==1,"Actual protected ACK release");
                }
                if(i<20||i>=45)
                while(queue.TryTake(c.Generation,out var app))
                {
                    frames.Add(new{At=i,Hex=Convert.ToHexString(app!)});
                    Array.Clear(app!);
                }
            }
            Reject<InvalidDataException>(()=>outbox.Observe(c.Human,c.Store.Find(c.Human)!,10010000,5));
            Check(!outbox.IsFaulted&&outbox.Samples==1000,"Budget preflight preserves frontier");
            outbox.Dispose();Check(outbox.QueuedCount==54&&outbox.Samples==1000&&c.Store.Find(c.Human) is not null&&queue.Count==0&&c.Transport.State==SoeClientTransportState.Established,"Dispose preserves history and borrowers");
        }
        using(var c=new Context(peer,catalog,options))
        using(var foreign=new Context(peer,catalog,options))
        {
            c.Store.PublishLocal(c.Human,AppearanceTransform.Identity,0);var old=c.Store.Find(c.Human)!;
            using var queue=new SceneWorldClientRootMotionQueue(c.World,c.Store,new(100,10),2,124);
            using var outbox=new SceneWorldRootMotionCadenceOutbox(c.World,c.Store,queue,maxSamples:120,maxPendingPackets:2,maxPendingBytes:124);
            Reject<OperationCanceledException>(()=>outbox.Observe(c.Human,old,10000,cancellationToken:new(true)));
            Reject<ArgumentOutOfRangeException>(()=>new SceneWorldRootMotionCadenceOutbox(c.World,c.Store,queue,maxSamples:6001));
            Reject<ArgumentOutOfRangeException>(()=>new SceneWorldRootMotionCadenceOutbox(c.World,c.Store,queue,stepMicroseconds:9999));
            Reject<InvalidDataException>(()=>outbox.Observe(c.Human,old,20000));
            foreign.Store.PublishLocal(foreign.Human,AppearanceTransform.Identity,0);
            Reject<InvalidDataException>(()=>outbox.Observe(foreign.Human,foreign.Store.Find(foreign.Human)!,10000));
            Reject<ArgumentException>(()=>new SceneWorldRootMotionCadenceOutbox(c.World,foreign.Store,queue));
            Exception? error=null;var thread=new Thread(()=>{try{outbox.Observe(c.Human,old,10000);}catch(Exception e){error=e;}});thread.Start();thread.Join();Check(error is InvalidOperationException&&outbox.Samples==0,"Owner before effects");
            c.Store.PublishLocal(c.Human,AppearanceTransform.Identity,0);Reject<InvalidDataException>(()=>outbox.Observe(c.Human,old,10000));
            c.Store.PublishAuthoritative(c.Human,AppearanceTransform.Identity,0,1);Reject<InvalidDataException>(()=>outbox.Observe(c.Human,c.Store.Find(c.Human)!,10000));
            c.Store.PublishLocal(c.Human,AppearanceTransform.Identity,0);
            for(int i=1;i<=110;i++)outbox.Observe(c.Human,c.Store.Find(c.Human)!,i*10000);
            Check(queue.Count==2&&outbox.MovementCounter==2&&outbox.LastSuccessfulQueueMicroseconds==1100000,"Initial stationary send plus heartbeat");
            Check(queue.TryTake(c.Generation,out var handed),"Handed array");var hex=Convert.ToHexString(handed!);outbox.Dispose();
            Check(Convert.ToHexString(handed!)==hex&&queue.Count==1&&c.Store.Find(c.Human) is not null,"Outbox never clears transferred array or FIFO");Array.Clear(handed!);
        }
        using(var c=new Context(peer,catalog,options))
        {
            c.Store.PublishLocal(c.Human,AppearanceTransform.Identity,1);
            using var queue=new SceneWorldClientRootMotionQueue(c.World,c.Store,new(100,10),1,62);
            using var outbox=new SceneWorldRootMotionCadenceOutbox(c.World,c.Store,queue,maxPendingPackets:2,maxPendingBytes:124);
            for(int i=1;i<=19;i++){c.Store.PublishLocal(c.Human,AppearanceTransform.Identity with{Translation=new(0,0,i)},1);outbox.Observe(c.Human,c.Store.Find(c.Human)!,i*10000);}
            c.Store.PublishLocal(c.Human,AppearanceTransform.Identity with{Translation=new(0,0,20)},1);
            Reject<InvalidDataException>(()=>outbox.Observe(c.Human,c.Store.Find(c.Human)!,200000));
            Check(outbox.IsFaulted&&outbox.Samples==20&&outbox.ElapsedMicroseconds==200000&&outbox.MovementCounter==1&&outbox.LastSuccessfulQueueMicroseconds==100000&&queue.Count==1,"Actual mismatched caller capacity honest terminal prefix");
            Reject<InvalidOperationException>(()=>outbox.Observe(c.Human,c.Store.Find(c.Human)!,210000));
        }
        using(var c=new Context(peer,catalog,options))
        {
            c.Store.PublishLocal(c.Human,AppearanceTransform.Identity,1);
            using var queue=new SceneWorldClientRootMotionQueue(c.World,c.Store,new(100,10),1,62);
            using var outbox=new SceneWorldRootMotionCadenceOutbox(c.World,c.Store,queue,maxPendingPackets:1,maxPendingBytes:62);
            for(int i=1;i<=10;i++)outbox.Observe(c.Human,c.Store.Find(c.Human)!,i*10000);
            c.Store.PublishLocal(c.Human,AppearanceTransform.Identity,0);
            Check(outbox.Observe(c.Human,c.Store.Find(c.Human)!,110000).Status==SceneWorldRootMotionCadenceStatus.DeclinedBackpressure&&outbox.MovementCounter==1&&outbox.LastSuccessfulQueueMicroseconds==100000,"Immediate stop backpressure preserves success frontier");
            Check(queue.TryTake(c.Generation,out var first),"Release historical moving frame");Array.Clear(first!);
            Check(outbox.Observe(c.Human,c.Store.Find(c.Human)!,120000).Status==SceneWorldRootMotionCadenceStatus.Queued&&outbox.MovementCounter==2,"Pending stop queues latest once without interval wait");
            Reject<InvalidDataException>(()=>outbox.Observe(c.Human,c.Store.Find(c.Human)!,120000));
            var snapshot=c.Human;var motion=c.Store.Find(snapshot)!;c.Pump.Close();
            Reject<InvalidDataException>(()=>outbox.Observe(snapshot,motion,130000));Check(outbox.Samples==12,"Closed-world preflight preserves frontier");
        }
        File.WriteAllText(outputPath,JsonSerializer.Serialize(new{Checks=checks,Frames=frames,Statuses=statuses,WireHex=wireHex,DriverSteps=1000,OriginalAssets=0}));Console.WriteLine("PASS cadence checks="+checks);
    }
}
