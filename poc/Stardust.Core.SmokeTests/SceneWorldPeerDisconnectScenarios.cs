using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

namespace Stardust.Core.SmokeTests;

public static class SceneWorldPeerDisconnectScenarios
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
        checks=0;var peer=JsonSerializer.Deserialize<Peer>(File.ReadAllBytes(peerPath))!;byte[] Wire(string key)=>Convert.FromHexString(peer.Datagrams[key]);
        var catalog=ObjectTemplateCatalogProfile.Decode(File.ReadAllBytes("poc/preview-profiles/scene-object-catalog.json"));using var bankFile=File.OpenRead("poc/preview-profiles/human-locomotion.json");var bank=SkeletalAnimationBankProfile.Load(bankFile);
        var options=new Dictionary<SceneObjectResourceRecipe,SceneObjectResourceOptions>{{SceneObjectResourceRecipe.CreatureDetail0,new SceneCreatureResourceOptions(new("appearance/lat/all_m.lat","emt_standing_idle_ambient_1_ag","798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B"),bank,true)},{SceneObjectResourceRecipe.StaticAppearance,new SceneStaticResourceOptions(SceneStaticMaterialPolicy.RequireComplete,null)}};
        void Establish(Context c){Check(Convert.ToHexString(c.Transport.BeginSession(0x12345678,496))==peer.Request,"Actual session request");c.Transport.AcceptSessionResponse(Convert.FromHexString(peer.Response));}

        using(var c=new Context(peer,catalog,options))
        {
            Establish(c);
            c.Store.PublishLocal(c.Human,AppearanceTransform.Identity,0);
            Vector2[] layout=[new(.5f,.5f),new(0,.5f),new(0,0),new(.5f,0),new(1,0),new(1,.5f),new(1,1),new(.5f,1),new(0,1)];
            var points=layout.Select(p=>new Vector3(-10+p.X*20,2,10-p.Y*20)).ToArray();
            var surface=new TerrainPatchSurfaceQuery(new TerrainPatchGeometry(points,Enumerable.Repeat(Vector3.UnitY,9).ToArray(),new uint[9],1,0));
            using var driver=new SceneWorldTerrainMotionDriver(c.World,c.Store,0xF122334455667788UL,new[]{surface},stepMicroseconds:10000);
            driver.Submit(0,new(new(2,0,0),null));
            var correction=c.Ingress.Receive(c.Generation,Wire("correction"));
            Check(Convert.ToHexString(correction.ControlDatagrams.Single())==peer.Acks["correction"],"Exact caller ACK");
            Check(c.Pump.Step(0).AcceptedMessages==1,"Typed correction prefix");
            Check(driver.Advance(10000).StepsThisUpdate==1,"Actual ordinary driver tick");
            var local=c.Store.Find(c.Human)!;
            Check(!local.Authoritative&&c.Store.IsCurrent(local),"Issued current local prefix");
            using var queue=new SceneWorldClientRootMotionQueue(c.World,c.Store,new(100,10));
            using var bridge=new SceneWorldRootMotionTransportBridge(queue,c.Transport);
            queue.Enqueue(c.Human,local,0,34,100);queue.Enqueue(c.Human,local,0,35,101);
            c.Transport.QueueReliableMessage(new byte[]{1,2,3},3);
            var unsent=c.Transport.PumpQueuedDatagrams(0);Check(unsent.Count==1&&c.Transport.RetainedOutboundPackets==1,"Actual retained reliability");
            foreach(var packet in unsent)Array.Clear(packet);
            c.Transport.QueueReliableMessage(new byte[]{4,5},2);
            Check(c.Transport.PendingMessageCount==1,"Actual pending transport message");
            foreach(string key in new[]{"badCrc","shortFraming"})
            {
                var rejected=c.Ingress.Receive(c.Generation,Wire(key));
                Check(rejected.Status==SceneWorldSoeApplicationIngressStatus.RejectedFraming&&!rejected.PeerDisconnected&&rejected.ReturnedApplications==0&&rejected.ControlDatagrams.Count==0,"Framing rejection");
                Check(c.Transport.State==SoeClientTransportState.Established&&c.Transport.PendingMessageCount==1&&c.Transport.RetainedOutboundPackets==1&&!c.Ingress.IsFaulted,"Framing preserves transport prefix");
            }
            var suffix=c.Ingress.Receive(c.Generation,Wire("duplicateApplication"));
            Check(c.Pump.QueuedMessageCount==1,"Pending scene copy before disconnect");
            var disconnect=c.Ingress.Receive(c.Generation,Wire("disconnect"));
            Check(disconnect.Status==SceneWorldSoeApplicationIngressStatus.Accepted&&disconnect.PeerDisconnected&&disconnect.ReturnedApplications==0&&disconnect.QueuedApplications==0&&disconnect.QueuedBytes==0&&disconnect.ControlDatagrams.Count==0,"Actual peer-disconnect receipt");
            Check(c.Transport.State==SoeClientTransportState.Closed&&c.Transport.PendingMessageCount==0&&c.Transport.RetainedOutboundPackets==0,"Close clears transport counts");
            Check((c.World.State is ZoneSceneTransitionState.Ready or ZoneSceneTransitionState.AwaitingServerReady)&&c.Store.IsCurrent(local)&&c.Pump.QueuedMessageCount==1&&c.Jobs.All(j=>!j.Token.IsCancellationRequested),"Disconnect preserves scene borrowers");
            Check(!bridge.TryQueueNext(c.Generation)&&queue.Count==2&&queue.QueuedBytes==124&&bridge.AcceptedCount==0,"Closed transport declines before FIFO take");
            Check(c.Ingress.Receive(c.Generation,Wire("disconnect")).Status==SceneWorldSoeApplicationIngressStatus.TransportNotEstablished,"Second receive preflight decline");
            c.Ingress.Dispose();bridge.Dispose();
            Check(queue.Count==2&&c.Store.IsCurrent(local)&&c.Pump.QueuedMessageCount==1,"Bridge disposal preserves owners");
            Check(Convert.ToHexString(correction.ControlDatagrams[0])==peer.Acks["correction"],"Transferred ACK survives disconnect/dispose");
            queue.Dispose();Check(queue.Count==0&&queue.QueuedBytes==0&&c.Store.IsCurrent(local),"FIFO owner drains only own bytes");
            int routes=c.Routes.Count;c.Pump.Close();
            Check(c.World.State==ZoneSceneTransitionState.Closed&&c.Pump.QueuedMessageCount==0&&c.Pump.QueuedMessageBytes==0&&!c.Store.IsCurrent(local),"Caller closes world and pending scene suffix");
            foreach(var job in c.Jobs)job.Completion.TrySetException(new Exception("Synthetic late completion"));
            c.Pump.DrainCompletion.GetAwaiter().GetResult();
            Check(c.Routes.Count==routes&&c.Jobs.Count==2&&c.Jobs.All(j=>j.Token.IsCancellationRequested&&j.Completion.Task.IsCompleted),"Canceled jobs drained without late publication");
            Check(Convert.ToHexString(correction.ControlDatagrams[0])==peer.Acks["correction"],"Pump closure preserves caller controls");
            foreach(var receipt in new[]{correction,suffix})foreach(var control in receipt.ControlDatagrams)Array.Clear(control);
        }
        foreach(string key in new[]{"wrongConnection","shortPacket"})
        using(var c=new Context(peer,catalog,options))
        {
            Establish(c);c.Transport.QueueReliableMessage(new byte[]{7},1);
            Reject<InvalidDataException>(()=>c.Ingress.Receive(c.Generation,Wire(key)));
            Check(c.Ingress.IsFaulted&&c.Ingress.ProcessedDatagramCount==0&&c.Transport.State==SoeClientTransportState.Established&&c.Transport.PendingMessageCount==1&&c.Pump.QueuedMessageCount==0,"Transport exception leaves prefix; ingress terminal");
            Reject<InvalidOperationException>(()=>c.Ingress.Receive(c.Generation,Wire("disconnect")));
            Check(c.Transport.State==SoeClientTransportState.Established,"Faulted ingress does not close borrower");
        }
        using(var c=new Context(peer,catalog,options))
        {
            Establish(c);var result=c.Transport.AcceptDatagram(Wire("alternateReason"));
            Check(result.PeerDisconnected&&result.ApplicationPayloads.Count==0&&result.ControlDatagrams.Count==0&&c.Transport.State==SoeClientTransportState.Closed,"Pure decoder accepts arbitrary reason bytes");
        }
        File.WriteAllText(outputPath,JsonSerializer.Serialize(new{Checks=checks,DisconnectHex=peer.Datagrams["disconnect"],ExactControls=1,DriverSteps=1,NegativePackets=4,PendingRootPackets=2,JobsDrained=2,OriginalAssets=0}));
        Console.WriteLine("PASS peer disconnect checks="+checks);
    }
}
