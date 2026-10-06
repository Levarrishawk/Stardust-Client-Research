using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

namespace Stardust.Core.SmokeTests;

public static class SceneWorldMultipacketDisconnectScenarios
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


        var results=new List<object>();
        foreach(string policy in new[]{"apply","drop","disconnectFirst"})
        using(var c=new Context(peer,catalog,options))
        {
            Establish(c);c.Store.PublishLocal(c.Human,AppearanceTransform.Identity,0);var before=c.Store.Find(c.Human)!;
            using var queue=new SceneWorldClientRootMotionQueue(c.World,c.Store,new(100,10));
            using var bridge=new SceneWorldRootMotionTransportBridge(queue,c.Transport);
            queue.Enqueue(c.Human,before,0,1,100);
            bool first=policy=="disconnectFirst";var receipt=c.Ingress.Receive(c.Generation,Wire(first?"disconnectFirst":"applicationFirst"));
            Check(receipt.Accepted&&receipt.PeerDisconnected&&c.Transport.State==SoeClientTransportState.Closed,"Actual outer disconnect");
            Check(receipt.ReturnedApplications==(first?0:1)&&receipt.QueuedApplications==(first?0:1)&&receipt.QueuedBytes==(first?0:62)&&c.Pump.QueuedMessageCount==(first?0:1),"Ordered application prefix only");
            Check(receipt.ControlDatagrams.Count==(first?0:1),"Ordered control prefix only");
            if(!first)Check(Convert.ToHexString(receipt.ControlDatagrams[0])==peer.Acks["correction"],"Exact pre-disconnect ACK");
            Check(!bridge.TryQueueNext(c.Generation)&&queue.Count==1&&bridge.AcceptedCount==0,"Closed bridge before take");
            c.Ingress.Dispose();Check(c.Store.IsCurrent(before)&&c.Pump.QueuedMessageCount==(first?0:1),"Ingress leaves scene owner alive");
            int accepted=0;
            if(policy=="apply")
            {
                accepted=c.Pump.Step(0).AcceptedMessages;var motion=c.Store.Find(c.Human)!;
                Check(accepted==1&&c.Routes.Count==1&&motion.Authoritative&&motion.MovementCounter==33,"Caller explicitly applies received prefix despite closed transport");
                Check(Convert.ToHexString(ObjectControllerTransformCodec.Encode(c.Routes[0].FullResult!.Raw))==peer.Applications["root"],"Exact legal root bytes");
            }
            queue.Dispose();c.Pump.Close();
            foreach(var job in c.Jobs)job.Completion.TrySetException(new Exception("Synthetic late completion"));c.Pump.DrainCompletion.GetAwaiter().GetResult();
            Check(c.Pump.QueuedMessageCount==0&&c.World.State==ZoneSceneTransitionState.Closed&&queue.Count==0&&c.Routes.Count==accepted,"Caller drops queued suffix and drains owners");
            Check(c.Jobs.All(j=>j.Token.IsCancellationRequested&&j.Completion.Task.IsCompleted),"No late resource publication");
            if(!first)Check(Convert.ToHexString(receipt.ControlDatagrams[0])==peer.Acks["correction"],"Caller ACK survives all owner cleanup");
            results.Add(new{Policy=policy,Applications=receipt.ReturnedApplications,Controls=receipt.ControlDatagrams.Select(Convert.ToHexString).ToArray(),Accepted=accepted});
            foreach(var control in receipt.ControlDatagrams)Array.Clear(control);
        }
        var negatives=new List<object>();
        foreach(string key in new[]{"malformedSuffix","wrongConnection"})
        using(var c=new Context(peer,catalog,options))
        {
            Establish(c);Reject<InvalidDataException>(()=>c.Ingress.Receive(c.Generation,Wire(key)));
            Check(c.Ingress.IsFaulted&&c.Ingress.ProcessedDatagramCount==0&&c.Ingress.QueuedApplicationCount==0&&c.Pump.QueuedMessageCount==0&&c.Transport.State==SoeClientTransportState.Established,"Throwing outer receive has no returned receipt/copy");
            Reject<InvalidOperationException>(()=>c.Ingress.Receive(c.Generation,Wire("correction")));
            var retry=c.Transport.AcceptDatagram(Wire("correction"));bool consumed=key=="wrongConnection";
            Check(retry.ApplicationPayloads.Count==(consumed?0:1)&&retry.ControlDatagrams.Count==1,"Direct borrowed transport proves reliability completion prefix");
            Check(Convert.ToHexString(retry.ControlDatagrams[0])==peer.Acks["correction"],"Exact direct retry ACK");
            negatives.Add(new{Kind=key,Consumed=consumed,RetryApplications=retry.ApplicationPayloads.Count});
            foreach(var app in retry.ApplicationPayloads)Array.Clear(app);foreach(var control in retry.ControlDatagrams)Array.Clear(control);
        }
        using(var c=new Context(peer,catalog,options))
        {
            Establish(c);var result=c.Transport.AcceptDatagram(Wire("applicationFirst"));
            Check(result.PeerDisconnected&&result.ApplicationPayloads.Count==1&&Convert.ToHexString(result.ApplicationPayloads[0])==peer.Applications["root"]&&result.ControlDatagrams.Count==1,"Pure transport returns caller-owned prefix");
            Check(Convert.ToHexString(result.ControlDatagrams[0])==peer.Acks["correction"],"Pure transport exact control");
            c.Transport.Close();Check(Convert.ToHexString(result.ApplicationPayloads[0])==peer.Applications["root"],"Returned app survives repeated transport close");
            foreach(var app in result.ApplicationPayloads)Array.Clear(app);foreach(var control in result.ControlDatagrams)Array.Clear(control);
        }
        File.WriteAllText(outputPath,JsonSerializer.Serialize(new{Checks=checks,Results=results,Negatives=negatives,OriginalAssets=0}));Console.WriteLine("PASS outer multipacket checks="+checks);
    }
}
