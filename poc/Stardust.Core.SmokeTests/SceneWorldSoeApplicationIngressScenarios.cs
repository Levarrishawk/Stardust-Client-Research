using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

namespace Stardust.Core.SmokeTests;

public static class SceneWorldSoeApplicationIngressScenarios
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
        var controls=new Dictionary<string,string>();
        void Control(string key,SceneWorldSoeApplicationIngressReceipt receipt)
        {Check(receipt.ControlDatagrams.Count==1,"Explicit caller-owned control array");controls[key]=Convert.ToHexString(receipt.ControlDatagrams[0]);Check(controls[key]==peer.Acks[key],"Independent exact ACK");}
        object? motionOutput=null;
        using(var c=new Context(peer,catalog,options))
        {
            Check(c.Ingress.Receive(c.Generation,Wire("correction")).Status==SceneWorldSoeApplicationIngressStatus.TransportNotEstablished&&c.Ingress.ProcessedDatagramCount==0,"Offline decline before mutation");
            Check(Convert.ToHexString(c.Transport.BeginSession(0x12345678,496))==peer.Request,"Actual request before response");Check(c.Ingress.Receive(c.Generation,Wire("correction")).Status==SceneWorldSoeApplicationIngressStatus.TransportNotEstablished&&c.Ingress.ProcessedDatagramCount==0,"Session-requested decline before receive");c.Transport.AcceptSessionResponse(Convert.FromHexString(peer.Response));
            Reject<OperationCanceledException>(()=>c.Ingress.Receive(c.Generation,Wire("correction"),new CancellationToken(true)));
            Check(c.Ingress.Receive(c.Generation-1,Wire("correction")).Status==SceneWorldSoeApplicationIngressStatus.StaleGeneration&&c.Ingress.ProcessedDatagramCount==0&&c.Pump.QueuedMessageCount==0,"Stale/precancel no reliability progress");
            Exception? error=null;var thread=new Thread(()=>{try{c.Ingress.Receive(c.Generation,Wire("correction"));}catch(Exception e){error=e;}});thread.Start();thread.Join();Check(error is InvalidOperationException&&c.Ingress.ProcessedDatagramCount==0,"Foreign owner before transport");
            error=null;var constructor=new Thread(()=>{try{new SceneWorldSoeApplicationIngress(c.World,c.Pump,c.Transport,8,256);}catch(Exception e){error=e;}});constructor.Start();constructor.Join();Check(error is InvalidOperationException,"World/pump owner constructor proof");
            error=null;var foreignDispose=new Thread(()=>{try{c.Ingress.Dispose();}catch(Exception e){error=e;}});foreignDispose.Start();foreignDispose.Join();Check(error is InvalidOperationException&&c.Ingress.ProcessedDatagramCount==0,"Foreign disposal before effects");
            var shortFrame=c.Ingress.Receive(c.Generation,Wire("shortFraming"));Check(shortFrame.Status==SceneWorldSoeApplicationIngressStatus.RejectedFraming&&shortFrame.ControlDatagrams.Count==0&&!c.Ingress.IsFaulted,"Actual short framing nonfatal");
            var bad=c.Ingress.Receive(c.Generation,Wire("badCrc"));Check(bad.Status==SceneWorldSoeApplicationIngressStatus.RejectedFraming&&bad.QueuedApplications==0&&bad.ControlDatagrams.Count==0&&!c.Ingress.IsFaulted,"Actual bad CRC is nonfatal");
            c.Store.PublishLocal(c.Human,AppearanceTransform.Identity,0);
            Vector2[] layout=[new(.5f,.5f),new(0,.5f),new(0,0),new(.5f,0),new(1,0),new(1,.5f),new(1,1),new(.5f,1),new(0,1)];var points=layout.Select(p=>new Vector3(-10+p.X*20,2,10-p.Y*20)).ToArray();var surface=new TerrainPatchSurfaceQuery(new TerrainPatchGeometry(points,Enumerable.Repeat(Vector3.UnitY,9).ToArray(),new uint[9],1,0));
            using var driver=new SceneWorldTerrainMotionDriver(c.World,c.Store,0xF122334455667788UL,new[]{surface},stepMicroseconds:10000);driver.Submit(0,new(new(2,0,0),null));
            byte[] input=Wire("correction");var first=c.Ingress.Receive(c.Generation,input);Array.Clear(input);Control("correction",first);Check(first.QueuedApplications==1&&c.Pump.QueuedMessageCount==1,"Pump copy survives caller/source clearing");
            var duplicateSoe=c.Ingress.Receive(c.Generation,Wire("correction"));Control("duplicateSoe",duplicateSoe);Check(duplicateSoe.ReturnedApplications==0&&c.Pump.QueuedMessageCount==1,"Duplicate SOE sequence not application counter");
            var duplicateApplication=c.Ingress.Receive(c.Generation,Wire("duplicateApplication"));Control("duplicateApplication",duplicateApplication);Check(duplicateApplication.QueuedApplications==1&&c.Pump.QueuedMessageCount==2,"Distinct SOE sequence admits duplicate application counter");
            Check(c.Ingress.ProcessedDatagramCount==5&&c.Ingress.QueuedApplicationCount==2,"Progress counters separate framing/queued apps");
            c.Ingress.Dispose();c.Ingress.Dispose();Check(Convert.ToHexString(first.ControlDatagrams[0])==peer.Acks["correction"]&&c.Pump.QueuedMessageCount==2&&c.Transport.State==SoeClientTransportState.Established,"Disposed bridge never clears transferred controls or borrowers");Reject<ObjectDisposedException>(()=>c.Ingress.Receive(c.Generation,Wire("correction")));
            var step=c.Pump.Step(0);Check(step.AcceptedMessages==1&&step.DroppedMessages==1&&c.Routes.Count==2,"Actual combined pump applies one/drops one");var authoritative=c.Store.Find(c.Human)!;Check(authoritative.Authoritative&&authoritative.MovementCounter==33&&ReferenceEquals(c.Routes[0].FullResult!.Motion,c.Routes[1].FullResult!.PriorMotion),"Actual correction/duplicate preserves row identity");
            var state=driver.Advance(10000);Check(state.FaultType is null&&state.StepsThisUpdate==1,"Frozen driver adopts correction on ordinary tick");var local=c.Store.Find(c.Human)!;Check(!local.Authoritative&&local.Revision==authoritative.Revision+1,"Driver publishes current LOCAL row");
            motionOutput=new{RawHex=Convert.ToHexString(ObjectControllerTransformCodec.Encode(c.Routes[0].FullResult!.Raw)),Accepted=step.AcceptedMessages,Dropped=step.DroppedMessages,AuthoritativeRevision=authoritative.Revision,LocalRevision=local.Revision,PositionBits=new[]{local.Transform.Translation.X,local.Transform.Translation.Y,local.Transform.Translation.Z}.Select(BitConverter.SingleToUInt32Bits).ToArray(),SpeedBits=BitConverter.SingleToUInt32Bits(local.Speed),Rows=new[]{local.Transform.RowX.X,local.Transform.RowX.Y,local.Transform.RowX.Z,local.Transform.RowY.X,local.Transform.RowY.Y,local.Transform.RowY.Z,local.Transform.RowZ.X,local.Transform.RowZ.Y,local.Transform.RowZ.Z}};
            foreach(var receipt in new[]{first,duplicateSoe,duplicateApplication})foreach(var control in receipt.ControlDatagrams)Array.Clear(control);
        }
        var prefixes=new List<object>();
        foreach(string kind in new[]{"queueCapacity","countBound","totalBytes","perApplication"})
        using(var c=new Context(peer,catalog,options,pumpCapacity:kind=="queueCapacity"?1:8,maxApplications:kind=="countBound"?1:8,maxBytes:kind=="totalBytes"?80:kind=="perApplication"?61:256))
        {
            Establish(c);string key=kind=="perApplication"?"correction":"multi";
            Reject<InvalidDataException>(()=>c.Ingress.Receive(c.Generation,Wire(key)));int prefix=kind=="queueCapacity"?1:0;
            Check(c.Ingress.IsFaulted&&c.Ingress.ProcessedDatagramCount==1&&c.Ingress.QueuedApplicationCount==(ulong)prefix&&c.Pump.QueuedMessageCount==prefix,"Real post-transport bounds/capacity prefix");
            Reject<InvalidOperationException>(()=>c.Ingress.Receive(c.Generation,Wire("duplicateApplication")));Check(c.Pump.QueuedMessageCount==prefix,"Terminal bridge no retry");
            var consumed=c.Transport.AcceptDatagram(Wire(key));Check(consumed.ApplicationPayloads.Count==0&&consumed.ControlDatagrams.Count==1,"Reliability consumed even on queue/bound failure");foreach(var control in consumed.ControlDatagrams)Array.Clear(control);
            if(prefix==1){var step=c.Pump.Step(0);Check(step.AcceptedMessages==1&&c.Store.Find(c.Human)!.MovementCounter==33,"Earlier queued prefix remains applicable");}
            prefixes.Add(new{Kind=kind,QueuedPrefix=prefix,TransportConsumed=true});
        }
        using(var c=new Context(peer,catalog,options))
        {
            Establish(c);var first=c.Ingress.Receive(c.Generation,Wire("correction"));foreach(var control in first.ControlDatagrams)Array.Clear(control);c.Pump.Step(0);var prior=c.Store.Find(c.Human)!;
            var bad=c.Ingress.Receive(c.Generation,Wire("malformed"));Control("malformed",bad);var suffix=c.Ingress.Receive(c.Generation,Wire("suffix"));Control("suffix",suffix);
            Check(!c.Ingress.IsFaulted&&c.Pump.QueuedMessageCount==2,"Malformed application is valid transport admission");Reject<InvalidDataException>(()=>c.Pump.Step(0));
            Check(c.World.State==ZoneSceneTransitionState.Closed&&c.Pump.QueuedMessageCount==0&&c.Pump.QueuedMessageBytes==0&&!c.Store.IsCurrent(prior)&&prior.MovementCounter==33&&c.Jobs.All(j=>j.Token.IsCancellationRequested),"Actual pump terminal cleanup after honest prior motion prefix");
            ulong processed=c.Ingress.ProcessedDatagramCount;Check(c.Ingress.Receive(c.World.Generation,Wire("suffix")).Status==SceneWorldSoeApplicationIngressStatus.ClosedWorld&&c.Ingress.ProcessedDatagramCount==processed,"Closed world declines before transport");foreach(var receipt in new[]{bad,suffix})foreach(var control in receipt.ControlDatagrams)Array.Clear(control);
        }
        using(var c=new Context(peer,catalog,options))
        {
            Establish(c);c.Transport.Close();Check(c.Ingress.Receive(c.Generation,Wire("correction")).Status==SceneWorldSoeApplicationIngressStatus.TransportNotEstablished&&c.Ingress.ProcessedDatagramCount==0,"Closed transport decline before effects");
            Reject<ArgumentOutOfRangeException>(()=>new SceneWorldSoeApplicationIngress(c.World,c.Pump,c.Transport,0,256));Reject<ArgumentOutOfRangeException>(()=>new SceneWorldSoeApplicationIngress(c.World,c.Pump,c.Transport,129,256));Reject<ArgumentOutOfRangeException>(()=>new SceneWorldSoeApplicationIngress(c.World,c.Pump,c.Transport,8,0));Reject<ArgumentOutOfRangeException>(()=>new SceneWorldSoeApplicationIngress(c.World,c.Pump,c.Transport,8,65537));
        }
        File.WriteAllText(outputPath,JsonSerializer.Serialize(new{Controls=controls,Motion=motionOutput,Prefixes=prefixes,Checks=checks,OriginalAssets=0,RareMidReceiveCancellationTested=false}));Console.WriteLine("PASS SOE application ingress checks="+checks);
    }
}
