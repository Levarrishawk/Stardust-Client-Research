using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

namespace Stardust.Core.SmokeTests;

public static class SceneWorldOfflineRootMotionSessionScenarios
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


        var receipts=new List<SceneWorldOfflineRootMotionSessionReceipt>();
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
            Establish(c);c.Store.PublishLocal(c.Human,AppearanceTransform.Identity,0);
            using var driver=new SceneWorldTerrainPointTargetDriver(c.World,c.Store,0xF122334455667788UL,Ray(),2,.01f,0,10000,maximumUpdates:120);
            using var queue=new SceneWorldClientRootMotionQueue(c.World,c.Store,new(100,10));
            using var session=new SceneWorldOfflineRootMotionSession(c.World,c.Store,queue,c.Transport,0xA1B2C3D4,maxSamples:120,counterBase:0xFFFFFFFE,timestampBase:0xFFFFFFF0,flags:5);
            driver.SetManualInput(new(0,1),0);
            for(int i=1;i<=120;i++)
            {
                if(i==31)driver.SetPaused(true,300000);if(i==41)driver.SetPaused(false,400000);
                if(i==61)driver.SetManualInput(Vector2.Zero,600000);
                var state=driver.AdvanceNext((i-1)*10000,i*10000);Check(state.CompletedUpdates==i&&state.FaultType is null,"Actual point driver step");
                byte[] ack=Wire("ack"+session.NextSequence);string original=Convert.ToHexString(ack);
                var receipt=session.Observe(c.Human,c.Store.Find(c.Human)!,i*10000,i*10,ack);receipts.Add(receipt);
                Check(session.Samples==i&&queue.Count==0&&c.Transport.PendingMessageCount==0&&c.Transport.RetainedOutboundPackets==0&&Convert.ToHexString(ack)==original,"One bounded observe/drain and caller ACK preserved");Array.Clear(ack);
            }
            var history=session.State;session.Dispose();Check(session.State==history&&driver.State.CompletedUpdates==120&&c.Store.Find(c.Human) is not null&&c.Transport.State==SoeClientTransportState.Established,"History/borrowers after disposal");
            Reject<ObjectDisposedException>(()=>session.Observe(c.Human,c.Store.Find(c.Human)!,1210000,1210,Wire("ack0")));
        }
        var failures=new List<object>();
        foreach(string kind in new[]{"badAck","enqueue","backpressure","closed"})
        using(var c=new Context(peer,catalog,options))
        {
            Establish(c);c.Store.PublishLocal(c.Human,AppearanceTransform.Identity,1);var motion=c.Store.Find(c.Human)!;
            using var queue=new SceneWorldClientRootMotionQueue(c.World,c.Store,new(100,10),1,62);
            if(kind is "enqueue" or "backpressure")
            {queue.Enqueue(c.Human,motion,7,9,11);c.Transport.QueueReliableMessage(new byte[]{1},2);}
            if(kind=="closed")c.Transport.Close();
            using var session=new SceneWorldOfflineRootMotionSession(c.World,c.Store,queue,c.Transport,0xA1B2C3D4,maxPendingPackets:kind=="backpressure"?1:2,maxPendingBytes:kind=="backpressure"?62:124);
            for(int i=1;i<=9;i++)session.Observe(c.Human,motion,i*10000,i*10,Wire("ack0"));
            if(kind=="backpressure")
            {var packets=c.Transport.PumpQueuedDatagrams(90);foreach(var packet in packets)Array.Clear(packet);Check(c.Transport.AcceptDatagram(Wire("otherAck")).ReleasedOutboundPackets==1,"Release external busy prefix");}
            if(kind is "badAck" or "enqueue")
            {
                Reject<InvalidDataException>(()=>session.Observe(c.Human,motion,100000,100,Wire(kind=="badAck"?"badAck":"ack0")));
                Check(session.IsFaulted&&session.Samples==10&&session.Queued==(kind=="badAck"?1:0)&&session.Drained==0&&queue.Count==1&&session.LastReceipt!.Cadence.Samples==9,"Honest failed combined sample/FIFO prefix");
                Check(session.State.RelativeElapsedMicroseconds==100000&&session.State.LastNowMilliseconds==100,"Failure clocks retain actual consumed sample");
                Reject<InvalidOperationException>(()=>session.Observe(c.Human,motion,110000,110,Wire("ack0")));
            }
            else
            {
                var receipt=session.Observe(c.Human,motion,100000,100,Wire("ack0"));
                Check(!session.IsFaulted&&session.Samples==10,"Busy/closed drain policy is ordinary decline");
                if(kind=="backpressure")Check(receipt.Cadence.Status==SceneWorldRootMotionCadenceStatus.DeclinedBackpressure&&receipt.Drain is not null&&receipt.QueueCountAfterCadence==1&&receipt.QueueBytesAfterCadence==62&&queue.Count==0&&session.Queued==0&&session.Drained==1,"BP observation drains one historical frame");
                else Check(receipt.Cadence.Status==SceneWorldRootMotionCadenceStatus.Queued&&receipt.Drain is null&&queue.Count==1,"Closed transport preserves newly queued sample");
            }
            failures.Add(new{Kind=kind,Samples=session.Samples,Queued=session.Queued,Drained=session.Drained,Faulted=session.IsFaulted,QueueCount=queue.Count});
            session.Dispose();Check(c.Store.IsCurrent(motion),"Fault/closed disposal preserves store");
        }
        using(var c=new Context(peer,catalog,options))
        {
            Establish(c);c.Store.PublishLocal(c.Human,AppearanceTransform.Identity,0);var motion=c.Store.Find(c.Human)!;
            using var queue=new SceneWorldClientRootMotionQueue(c.World,c.Store,new(100,10));
            Reject<ArgumentOutOfRangeException>(()=>new SceneWorldOfflineRootMotionSession(c.World,c.Store,queue,c.Transport,0xA1B2C3D4,maximumDatagram:68));
            Check(c.Store.IsCurrent(motion)&&queue.Count==0&&c.Transport.State==SoeClientTransportState.Established,"Partial constructor preserves borrowers");
            using var session=new SceneWorldOfflineRootMotionSession(c.World,c.Store,queue,c.Transport,0xA1B2C3D4);
            Reject<OperationCanceledException>(()=>session.Observe(c.Human,motion,10000,10,Wire("ack0"),new(true)));
            Reject<ArgumentOutOfRangeException>(()=>session.Observe(c.Human,motion,10000,-1,Wire("ack0")));
            Reject<InvalidDataException>(()=>session.Observe(c.Human,motion,20000,10,Wire("ack0")));
            Exception? error=null;var thread=new Thread(()=>{try{session.Observe(c.Human,motion,10000,10,Wire("ack0"));}catch(Exception e){error=e;}});thread.Start();thread.Join();Check(error is InvalidOperationException&&session.Samples==0,"Owner preflight");
            c.Store.PublishLocal(c.Human,AppearanceTransform.Identity,0);Reject<InvalidDataException>(()=>session.Observe(c.Human,motion,10000,10,Wire("ack0")));
            Check(!session.IsFaulted&&session.Samples==0,"Stale/preflight rejects remain healthy");motion=c.Store.Find(c.Human)!;
            queue.Enqueue(c.Human,motion,7,9,11);queue.Enqueue(c.Human,motion,8,10,12);
            var receipt=session.Observe(c.Human,motion,10000,10,Wire("ack0"));
            Check(receipt.Cadence.Status==SceneWorldRootMotionCadenceStatus.NotDue&&receipt.Drain is not null&&receipt.QueueCountAfterCadence==2&&receipt.QueueBytesAfterCadence==124&&queue.Count==1&&session.Queued==0&&session.Drained==1,"NotDue drains at most one historical frame");
            Reject<ArgumentOutOfRangeException>(()=>session.Observe(c.Human,motion,20000,9,Wire("ack1")));
            session.Dispose();Check(queue.Count==1&&c.Transport.State==SoeClientTransportState.Established,"Dispose leaves historical FIFO");
        }
        File.WriteAllText(outputPath,JsonSerializer.Serialize(new{Checks=checks,Receipts=receipts,Failures=failures,OriginalAssets=0}));Console.WriteLine("PASS root session checks="+checks);
    }
}
