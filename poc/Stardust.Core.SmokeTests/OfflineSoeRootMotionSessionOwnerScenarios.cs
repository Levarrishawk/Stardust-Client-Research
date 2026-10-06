using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

namespace Stardust.Core.SmokeTests;

public static class OfflineSoeRootMotionSessionOwnerScenarios
{
    public sealed record Peer(Dictionary<string,string> Applications,string[] Initial,Dictionary<string,string> Datagrams,Dictionary<string,string> Acks,string Request,string Response,BootstrapCase[] Cases);
    public sealed record BootstrapCase(uint ConnectionId,uint Seed);
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


        var receipts=new List<SceneWorldOfflineRootMotionSessionReceipt>();string requestHex="",responseHex="";
        TerrainModernPatchSetRayQuery Ray()
        {
            var precision=(TerrainHeightArithmeticPrecision)64;
            var grid=TerrainSampleCoordinateGrid.Build(new(BitConverter.SingleToUInt32Bits(16),4,1,0,0,1),precision,precision,precision,precision);
            int count=grid.Parameters.Count*grid.Parameters.Count;
            var patch=TerrainCoordinateHeightPatch.Build(grid,Enumerable.Repeat(BitConverter.SingleToUInt32Bits(2),count).ToArray(),Enumerable.Repeat(Vector3.UnitY,count).ToArray(),new uint[count]);
            var modern=TerrainModernHeightPatch.Build(patch,TerrainModernNormalPolicy.ModernBinary64CentralDifference);
            return TerrainModernPatchSetRayQuery.Build(TerrainModernPatchSetSurfaceQuery.Build(TerrainModernHeightPatchSet.Build(new[]{modern},TerrainModernPatchEdgePolicy.RequireBitIdenticalActiveSharedEdges,TerrainModernPatchNormalPolicy.RequireBitIdenticalSharedNormals)));
        }



        using(var c=new Context(peer,catalog,options))
        {
            c.Store.PublishLocal(c.Human,AppearanceTransform.Identity,0);
            using var driver=new SceneWorldTerrainPointTargetDriver(c.World,c.Store,0xF122334455667788UL,Ray(),2,.01f,0,10000,maximumUpdates:3);
            using var queue=new SceneWorldClientRootMotionQueue(c.World,c.Store,new(100,10));
            using var owner=new OfflineSoeRootMotionSessionOwner(c.World,c.Store,queue,0xF00D00DB,0xA1B2C3D4,maxSamples:3,flags:5);
            var session=owner.Session;var transport=owner.Transport;requestHex=owner.RequestHex;responseHex=owner.ResponseHex;
            Check(!owner.IsDisposed&&transport.State==SoeClientTransportState.Established&&session.Samples==0,"Actual owned children initialized");
            byte[] canceledAck=OfflineSoeAcknowledgementFactory.Create(0,0xA1B2C3D4);
            Reject<OperationCanceledException>(()=>session.Observe(c.Human,c.Store.Find(c.Human)!,10000,10,canceledAck,new(true)));
            Reject<ArgumentNullException>(()=>session.Observe(null!,c.Store.Find(c.Human)!,10000,10,canceledAck));
            Reject<ArgumentNullException>(()=>session.Observe(c.Human,null!,10000,10,canceledAck));Array.Clear(canceledAck);
            var stale=c.Store.Find(c.Human)!;c.Store.PublishLocal(c.Human,AppearanceTransform.Identity with{Translation=new(0,2,0)},0);
            byte[] staleAck=OfflineSoeAcknowledgementFactory.Create(0,0xA1B2C3D4);
            Reject<InvalidDataException>(()=>session.Observe(c.Human,stale,10000,10,staleAck));Array.Clear(staleAck);
            Check(session.Samples==0&&!session.IsFaulted&&queue.Count==0,"Child preflight preserves owner session");
            Exception? error=null;var thread=new Thread(()=>{try{_=owner.Transport;}catch(Exception e){error=e;}});thread.Start();thread.Join();Check(error is InvalidOperationException,"Foreign property owner guard");
            error=null;thread=new Thread(()=>{try{owner.Dispose();}catch(Exception e){error=e;}});thread.Start();thread.Join();Check(error is InvalidOperationException&&!owner.IsDisposed,"Foreign disposal before effects");
            driver.SetManualInput(new(0,1),0);
            for(int i=1;i<=3;i++)
            {
                if(i==3)driver.SetManualInput(Vector2.Zero,20000);
                var state=driver.AdvanceNext((i-1)*10000,i*10000);Check(state.CompletedUpdates==i&&state.FaultType is null,"Three actual ordinary point updates");
                byte[] ack=OfflineSoeAcknowledgementFactory.Create(session.NextSequence,0xA1B2C3D4);string hex=Convert.ToHexString(ack);
                receipts.Add(session.Observe(c.Human,c.Store.Find(c.Human)!,i*10000,i*10,ack));
                Check(Convert.ToHexString(ack)==hex&&queue.Count==0&&transport.PendingMessageCount==0&&transport.RetainedOutboundPackets==0,"Actual offline frame/ACK admission");Array.Clear(ack);
            }
            Check(session.Queued==1&&session.Drained==1,"One bounded root join");
            var current=c.Store.Find(c.Human)!;queue.Enqueue(c.Human,current,8,9,30);
            // Deliberate cleanup fixture only, after normal session advancement has ended.
            transport.QueueReliableMessage(new byte[]{1},2);var packets=transport.PumpQueuedDatagrams(30);foreach(var packet in packets)Array.Clear(packet);
            transport.QueueReliableMessage(new byte[]{2},3);Check(transport.PendingMessageCount==1&&transport.RetainedOutboundPackets==1,"Owned transport retained/pending teardown fixture");
            var history=session.State;owner.Dispose();owner.Dispose();
            Check(owner.IsDisposed&&ReferenceEquals(owner.Session,session)&&ReferenceEquals(owner.Transport,transport)&&session.State==history&&transport.State==SoeClientTransportState.Closed&&transport.PendingMessageCount==0&&transport.RetainedOutboundPackets==0,"Owner idempotent teardown/history");
            Check(owner.RequestHex==requestHex&&owner.ResponseHex==responseHex&&queue.Count==1&&c.Store.IsCurrent(current)&&c.World.State is ZoneSceneTransitionState.Ready or ZoneSceneTransitionState.AwaitingServerReady,"Borrowed scene/FIFO and strings survive owned teardown");
            byte[] disposedAck=OfflineSoeAcknowledgementFactory.Create(1,0xA1B2C3D4);
            Reject<ObjectDisposedException>(()=>session.Observe(c.Human,current,40000,40,disposedAck));Array.Clear(disposedAck);
            Check(queue.TryTake(c.Generation,out var handed),"Caller can drain surviving borrowed FIFO");Array.Clear(handed!);
            Check(driver.State.CompletedUpdates==3,"Borrowed point driver remains inspectable");
        }
        using(var c=new Context(peer,catalog,options))
        {
            c.Store.PublishLocal(c.Human,AppearanceTransform.Identity,0);var current=c.Store.Find(c.Human)!;
            using var queue=new SceneWorldClientRootMotionQueue(c.World,c.Store,new(100,10));queue.Enqueue(c.Human,current,1,1,0);
            foreach(int maximum in new[]{0,6001})Reject<ArgumentOutOfRangeException>(()=>new OfflineSoeRootMotionSessionOwner(c.World,c.Store,queue,0,0,maxSamples:maximum));
            Reject<ArgumentNullException>(()=>new OfflineSoeRootMotionSessionOwner(null!,c.Store,queue,0,0));
            Reject<ArgumentNullException>(()=>new OfflineSoeRootMotionSessionOwner(c.World,null!,queue,0,0));
            Reject<ArgumentNullException>(()=>new OfflineSoeRootMotionSessionOwner(c.World,c.Store,null!,0,0));
            Check(queue.Count==1&&c.Store.IsCurrent(current)&&c.World.State is ZoneSceneTransitionState.Ready or ZoneSceneTransitionState.AwaitingServerReady,"Partial construction failure preserves borrowed prefix");
            c.Pump.Close();Reject<InvalidDataException>(()=>new OfflineSoeRootMotionSessionOwner(c.World,c.Store,queue,0,0));
            Check(c.World.State==ZoneSceneTransitionState.Closed&&queue.Count==1,"Closed world rejected without resurrection/queue rollback");
        }
        File.WriteAllText(outputPath,JsonSerializer.Serialize(new{Checks=checks,RequestHex=requestHex,ResponseHex=responseHex,Receipts=receipts,OriginalAssets=0}));Console.WriteLine("PASS offline owned root checks="+checks);
    }
}
