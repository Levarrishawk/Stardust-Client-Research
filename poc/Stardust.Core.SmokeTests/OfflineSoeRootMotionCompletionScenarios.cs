using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

namespace Stardust.Core.SmokeTests;

public static class OfflineSoeRootMotionCompletionScenarios
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



        var receipts=new List<SceneWorldOfflineRootMotionSessionReceipt>();var reports=new Dictionary<string,OfflineSoeRootMotionCompletion>();
        string requestHex="",responseHex="";
        TerrainModernPatchSetRayQuery Ray()
        {
            var precision=(TerrainHeightArithmeticPrecision)64;
            var grid=TerrainSampleCoordinateGrid.Build(new(BitConverter.SingleToUInt32Bits(16),4,1,0,0,1),precision,precision,precision,precision);
            int count=grid.Parameters.Count*grid.Parameters.Count;
            var patch=TerrainCoordinateHeightPatch.Build(grid,Enumerable.Repeat(BitConverter.SingleToUInt32Bits(2),count).ToArray(),Enumerable.Repeat(Vector3.UnitY,count).ToArray(),new uint[count]);
            var modern=TerrainModernHeightPatch.Build(patch,TerrainModernNormalPolicy.ModernBinary64CentralDifference);
            return TerrainModernPatchSetRayQuery.Build(TerrainModernPatchSetSurfaceQuery.Build(TerrainModernHeightPatchSet.Build(new[]{modern},TerrainModernPatchEdgePolicy.RequireBitIdenticalActiveSharedEdges,TerrainModernPatchNormalPolicy.RequireBitIdenticalSharedNormals)));
        }




        foreach(bool fault in new[]{false,true})
        using(var c=new Context(peer,catalog,options))
        {
            c.Store.PublishLocal(c.Human,AppearanceTransform.Identity,0);
            using var driver=new SceneWorldTerrainPointTargetDriver(c.World,c.Store,0xF122334455667788UL,Ray(),2,.01f,0,10000,maximumUpdates:3);
            using var queue=new SceneWorldClientRootMotionQueue(c.World,c.Store,new(100,10));
            using var owner=new OfflineSoeRootMotionSessionOwner(c.World,c.Store,queue,0xF00D00DB,0xA1B2C3D4,maxSamples:3,flags:5);
            var session=owner.Session;var transport=owner.Transport;requestHex=owner.RequestHex;responseHex=owner.ResponseHex;
            if(!fault)
            {
                reports["initial"]=OfflineSoeRootMotionCompletion.Capture(session,transport,queue);
                Reject<ArgumentNullException>(()=>OfflineSoeRootMotionCompletion.Capture(null!,transport,queue));
                Reject<ArgumentNullException>(()=>OfflineSoeRootMotionCompletion.Capture(session,null!,queue));
                Reject<ArgumentNullException>(()=>OfflineSoeRootMotionCompletion.Capture(session,transport,null!));
                Exception? error=null;var thread=new Thread(()=>{try{OfflineSoeRootMotionCompletion.Capture(session,transport,queue);}catch(Exception e){error=e;}});thread.Start();thread.Join();Check(error is InvalidOperationException,"Inherited session owner guard");
                SceneWorldClientRootMotionQueue? foreignQueue=null;Exception? foreignError=null;
                using var ready=new ManualResetEventSlim();using var release=new ManualResetEventSlim();
                var foreignThread=new Thread(()=>{try{using var foreign=new Context(peer,catalog,options);using var ownQueue=new SceneWorldClientRootMotionQueue(foreign.World,foreign.Store,new(100,10));foreignQueue=ownQueue;ready.Set();release.Wait();}catch(Exception e){foreignError=e;ready.Set();}});
                foreignThread.Start();try{Check(ready.Wait(5000)&&foreignError is null&&foreignQueue is not null,"Foreign queue setup");Reject<InvalidOperationException>(()=>OfflineSoeRootMotionCompletion.Capture(session,transport,foreignQueue!));}finally{release.Set();foreignThread.Join();}
                Check(foreignError is null,"Foreign owner cleanup");
            }
            driver.SetManualInput(new(0,1),0);
            for(int i=1;i<=3;i++)
            {
                if(i==3)driver.SetManualInput(Vector2.Zero,20000);
                var state=driver.AdvanceNext((i-1)*10000,i*10000);Check(state.CompletedUpdates==i&&state.FaultType is null,"Actual small point trace");
                byte[] ack=OfflineSoeAcknowledgementFactory.Create(fault&&i==3?(ushort)1:session.NextSequence,0xA1B2C3D4);
                try
                {
                    if(fault&&i==3)Reject<InvalidDataException>(()=>session.Observe(c.Human,c.Store.Find(c.Human)!,30000,30,ack));
                    else{var receipt=session.Observe(c.Human,c.Store.Find(c.Human)!,i*10000,i*10,ack);if(!fault)receipts.Add(receipt);}
                }finally{Array.Clear(ack);}
            }
            var history=session.State;int beforeCount=queue.Count,beforeBytes=queue.QueuedBytes;
            var completion=OfflineSoeRootMotionCompletion.Capture(session,transport,queue);
            Check(ReferenceEquals(session.State,history)&&queue.Count==beforeCount&&queue.QueuedBytes==beforeBytes&&transport.PendingMessageCount==0&&transport.RetainedOutboundPackets==0&&completion==OfflineSoeRootMotionCompletion.Capture(session,transport,queue),"Capture is read-only and primitive stable value");
            reports[fault?"fault":"normal"]=completion;
            if(!fault)
            {
                var motion=c.Store.Find(c.Human)!;queue.Enqueue(c.Human,motion,8,9,30);
                // Teardown/report fixture only; normal session advancement is finished.
                transport.QueueReliableMessage(new byte[]{1},2);var packets=transport.PumpQueuedDatagrams(30);foreach(var packet in packets)Array.Clear(packet);
                transport.QueueReliableMessage(new byte[]{2},3);
                reports["retained"]=OfflineSoeRootMotionCompletion.Capture(session,transport,queue);
                Check(!reports["retained"].CleanDrain&&queue.Count==1&&transport.PendingMessageCount==1&&transport.RetainedOutboundPackets==1,"Report preserves retained/pending/FIFO fixture");
            }
            owner.Dispose();reports[fault?"faultDisposed":"ownerDisposed"]=OfflineSoeRootMotionCompletion.Capture(session,transport,queue);
            Check(session.State==history&&transport.State==SoeClientTransportState.Closed&&queue.Count==1&&completion==reports[fault?"fault":"normal"],"Primitive historical report and session state survive cleanup");
            queue.Dispose();reports[fault?"faultCleared":"callerQueueDisposed"]=OfflineSoeRootMotionCompletion.Capture(session,transport,queue);
            Check(reports[fault?"faultCleared":"callerQueueDisposed"].CleanDrain==!fault&&c.Store.Find(c.Human) is not null,"Fault remains unclean even after byte drain; store borrowed");
        }
        File.WriteAllText(outputPath,JsonSerializer.Serialize(new{Checks=checks,RequestHex=requestHex,ResponseHex=responseHex,Receipts=receipts,Reports=reports,OriginalAssets=0}));Console.WriteLine("PASS primitive root completion checks="+checks);
    }
}
