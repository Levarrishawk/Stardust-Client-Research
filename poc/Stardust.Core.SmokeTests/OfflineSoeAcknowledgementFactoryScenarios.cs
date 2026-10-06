using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

namespace Stardust.Core.SmokeTests;

public static class OfflineSoeAcknowledgementFactoryScenarios
{
    public sealed record Peer(Dictionary<string,string> Applications,string[] Initial,Dictionary<string,string> Datagrams,Dictionary<string,string> Acks,string Request,string Response,AckCase[] Cases);
    public sealed record AckCase(ushort Sequence,uint Seed,int CrcWidth,int Maximum);
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



        var vectors=new List<object>();
        foreach(var row in peer.Cases)
        {
            byte[] ack=OfflineSoeAcknowledgementFactory.Create(row.Sequence,row.Seed,row.CrcWidth,row.Maximum);
            string hex=Convert.ToHexString(ack);
            Check(ack.Length==5+row.CrcWidth&&ack.Length<=9,"Small bounded ACK");
            Check(SoeDatagramCodec.TryDecode(ack,row.Seed,row.CrcWidth,row.Maximum,out var decoded)&&decoded.Length==4,"Exact decoded transport width");
            vectors.Add(new{row.Sequence,row.Seed,row.CrcWidth,row.Maximum,Hex=hex,DecodedHex=Convert.ToHexString(decoded)});Array.Clear(decoded);
            byte[] other=OfflineSoeAcknowledgementFactory.Create(row.Sequence,row.Seed,row.CrcWidth,row.Maximum);Array.Clear(other);
            Check(!ReferenceEquals(ack,other)&&Convert.ToHexString(ack)==hex,"Fresh caller-owned array survives other caller clearing");Array.Clear(ack);Check(ack.All(b=>b==0),"Caller clears owned return");
        }
        foreach(int width in new[]{-1,0,5})Reject<ArgumentOutOfRangeException>(()=>OfflineSoeAcknowledgementFactory.Create(0,0,width));
        foreach(int maximum in new[]{-1,0,3,65508,int.MaxValue})Reject<ArgumentOutOfRangeException>(()=>OfflineSoeAcknowledgementFactory.Create(0,0,2,maximum));
        for(int width=1;width<=4;width++)Reject<InvalidDataException>(()=>OfflineSoeAcknowledgementFactory.Create(0,0,width,4+width));
        byte[] upper=OfflineSoeAcknowledgementFactory.Create(ushort.MaxValue,uint.MaxValue,4,65507);Check(upper.Length==9,"Transport upper bound accepted without large allocation");Array.Clear(upper);
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
            using var driver=new SceneWorldTerrainPointTargetDriver(c.World,c.Store,0xF122334455667788UL,Ray(),2,.01f,0,10000,maximumUpdates:15);
            using var queue=new SceneWorldClientRootMotionQueue(c.World,c.Store,new(100,10));
            using var session=new SceneWorldOfflineRootMotionSession(c.World,c.Store,queue,c.Transport,0xA1B2C3D4,maxSamples:15,flags:5);
            driver.SetManualInput(new(0,1),0);
            for(int i=1;i<=15;i++)
            {
                if(i==11)driver.SetPaused(true,100000);if(i==13)driver.SetPaused(false,120000);
                if(i==15)driver.SetManualInput(Vector2.Zero,140000);
                var state=driver.AdvanceNext((i-1)*10000,i*10000);Check(state.CompletedUpdates==i&&state.FaultType is null,"Actual brief point-driver step");
                var before=session.State;int count=queue.Count;byte[] ack=OfflineSoeAcknowledgementFactory.Create(session.NextSequence,0xA1B2C3D4);
                Check(session.State==before&&queue.Count==count&&c.Transport.PendingMessageCount==0&&c.Transport.RetainedOutboundPackets==0,"Stateless factory leaves owners unchanged");
                string hex=Convert.ToHexString(ack);var receipt=session.Observe(c.Human,c.Store.Find(c.Human)!,i*10000,i*10,ack);receipts.Add(receipt);
                Check(Convert.ToHexString(ack)==hex&&queue.Count==0&&c.Transport.PendingMessageCount==0&&c.Transport.RetainedOutboundPackets==0,"Synthetic supplied ACK releases offline prefix only");Array.Clear(ack);
            }
            Check(session.Samples==15&&session.Queued==3&&session.Drained==3,"Three bounded offline publications");
            var history=session.State;session.Dispose();Check(session.State==history&&driver.State.CompletedUpdates==15&&c.Store.Find(c.Human) is not null,"Borrower/history survival");
        }
        File.WriteAllText(outputPath,JsonSerializer.Serialize(new{Checks=checks,Vectors=vectors,Receipts=receipts,OriginalAssets=0}));Console.WriteLine("PASS synthetic ACK checks="+checks);
    }
}
