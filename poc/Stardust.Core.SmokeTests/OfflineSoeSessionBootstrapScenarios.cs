using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

namespace Stardust.Core.SmokeTests;

public static class OfflineSoeSessionBootstrapScenarios
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

        var vectors=new List<object>();
        foreach(var row in peer.Cases)
        {
            var result=OfflineSoeSessionBootstrap.Create(row.ConnectionId,row.Seed);var transport=result.Transport;
            Check(transport.State==SoeClientTransportState.Established&&transport.PendingMessageCount==0&&transport.RetainedOutboundPackets==0,"Actual established synthetic bootstrap");
            byte[] request=Convert.FromHexString(result.RequestHex),response=Convert.FromHexString(result.ResponseHex);
            var decodedRequest=SoeSessionCodec.DecodeRequest(request);var decodedResponse=SoeSessionCodec.DecodeResponse(response,row.ConnectionId);
            Check(request.Length==14&&response.Length==17&&decodedRequest.ConnectionId==row.ConnectionId&&decodedRequest.MaximumDatagramSize==496,"Exact raw request boundary");
            Check(decodedResponse.ConnectionId==row.ConnectionId&&decodedResponse.Seed==row.Seed&&decodedResponse.CrcWidth==2&&decodedResponse.FirstTransformSelector==1&&decodedResponse.SecondTransformSelector==4&&decodedResponse.MaximumDatagramSize==496,"Fixed response boundary");
            string requestHex=result.RequestHex,responseHex=result.ResponseHex;Array.Clear(request);Array.Clear(response);
            transport.QueueReliableMessage(new byte[]{1});var packets=transport.PumpQueuedDatagrams(0);foreach(var packet in packets)Array.Clear(packet);
            transport.QueueReliableMessage(new byte[]{2},2);Check(transport.PendingMessageCount==1&&transport.RetainedOutboundPackets==1,"Actual caller-owned transport state before cleanup");
            transport.Close();transport.Close();
            Check(transport.State==SoeClientTransportState.Closed&&transport.PendingMessageCount==0&&transport.RetainedOutboundPackets==0&&result.RequestHex==requestHex&&result.ResponseHex==responseHex,"Caller closes transport; strings survive scratch/owner cleanup");
            vectors.Add(new{row.ConnectionId,row.Seed,result.RequestHex,result.ResponseHex});
        }
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
            var bootstrap=OfflineSoeSessionBootstrap.Create(0xF00D00DB,0xA1B2C3D4);var transport=bootstrap.Transport;
            try
            {
                c.Store.PublishLocal(c.Human,AppearanceTransform.Identity,0);
                using var driver=new SceneWorldTerrainPointTargetDriver(c.World,c.Store,0xF122334455667788UL,Ray(),2,.01f,0,10000,maximumUpdates:3);
                using var queue=new SceneWorldClientRootMotionQueue(c.World,c.Store,new(100,10));
                using var session=new SceneWorldOfflineRootMotionSession(c.World,c.Store,queue,transport,0xA1B2C3D4,maxSamples:3,flags:5);
                byte[] canceledAck=OfflineSoeAcknowledgementFactory.Create(0,0xA1B2C3D4);
                Reject<OperationCanceledException>(()=>session.Observe(c.Human,c.Store.Find(c.Human)!,10000,10,canceledAck,new(true)));Array.Clear(canceledAck);
                Check(session.Samples==0&&!session.IsFaulted&&queue.Count==0&&transport.State==SoeClientTransportState.Established,"Session precancel preserves bootstrap owner");
                driver.SetManualInput(new(0,1),0);
                for(int i=1;i<=3;i++)
                {
                    if(i==3)driver.SetManualInput(Vector2.Zero,20000);
                    var state=driver.AdvanceNext((i-1)*10000,i*10000);Check(state.CompletedUpdates==i&&state.FaultType is null,"Three ordinary actual point updates");
                    byte[] ack=OfflineSoeAcknowledgementFactory.Create(session.NextSequence,0xA1B2C3D4);string hex=Convert.ToHexString(ack);
                    receipts.Add(session.Observe(c.Human,c.Store.Find(c.Human)!,i*10000,i*10,ack));
                    Check(Convert.ToHexString(ack)==hex&&queue.Count==0&&transport.PendingMessageCount==0&&transport.RetainedOutboundPackets==0,"Actual offline frame/ACK cleanup");Array.Clear(ack);
                }
                Check(session.Samples==3&&session.Queued==1&&session.Drained==1,"Immediate stop creates one bounded frame");
                var history=session.State;session.Dispose();Check(session.State==history&&transport.State==SoeClientTransportState.Established&&c.Store.Find(c.Human) is not null,"Composed owner leaves bootstrap transport borrowed");
            }
            finally{transport.Close();}
            Check(transport.State==SoeClientTransportState.Closed&&bootstrap.RequestHex.Length==28&&bootstrap.ResponseHex.Length==34,"Caller final cleanup and immutable handshake history");
        }
        File.WriteAllText(outputPath,JsonSerializer.Serialize(new{Checks=checks,Vectors=vectors,Receipts=receipts,OriginalAssets=0}));Console.WriteLine("PASS synthetic bootstrap checks="+checks);
    }
}
