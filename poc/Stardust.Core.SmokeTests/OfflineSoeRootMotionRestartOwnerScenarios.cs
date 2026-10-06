using System.Numerics;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Geometry;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

namespace Stardust.Core.SmokeTests;

public static class OfflineSoeRootMotionRestartOwnerScenarios
{
    public sealed record Peer(Dictionary<string,string> Applications,string[] Initial,Dictionary<string,string> Datagrams,Dictionary<string,string> Acks,string Request,string Response,RunCase[] Cases);
    public sealed record RunCase(uint ConnectionId,uint Seed,bool Fault);
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




        var runs=new List<object>();using var restart=new OfflineSoeRootMotionRestartOwner(3);
        OfflineSoeRootMotionSessionOwner? priorOwner=null;OfflineSoeRootMotionCompletion? priorReport=null;
        TerrainModernPatchSetRayQuery Ray()
        {
            var precision=(TerrainHeightArithmeticPrecision)64;
            var grid=TerrainSampleCoordinateGrid.Build(new(BitConverter.SingleToUInt32Bits(16),4,1,0,0,1),precision,precision,precision,precision);
            int count=grid.Parameters.Count*grid.Parameters.Count;
            var patch=TerrainCoordinateHeightPatch.Build(grid,Enumerable.Repeat(BitConverter.SingleToUInt32Bits(2),count).ToArray(),Enumerable.Repeat(Vector3.UnitY,count).ToArray(),new uint[count]);
            var modern=TerrainModernHeightPatch.Build(patch,TerrainModernNormalPolicy.ModernBinary64CentralDifference);
            return TerrainModernPatchSetRayQuery.Build(TerrainModernPatchSetSurfaceQuery.Build(TerrainModernHeightPatchSet.Build(new[]{modern},TerrainModernPatchEdgePolicy.RequireBitIdenticalActiveSharedEdges,TerrainModernPatchNormalPolicy.RequireBitIdenticalSharedNormals)));
        }





        for(int run=0;run<peer.Cases.Length;run++)
        using(var c=new Context(peer,catalog,options))
        {
            var row=peer.Cases[run];c.Store.PublishLocal(c.Human,AppearanceTransform.Identity,0);
            using var driver=new SceneWorldTerrainPointTargetDriver(c.World,c.Store,0xF122334455667788UL,Ray(),2,.01f,0,10000,maximumUpdates:3);
            using var queue=new SceneWorldClientRootMotionQueue(c.World,c.Store,new(100,10));
            OfflineSoeRootMotionSessionOwner Factory()
            {Check(priorOwner is null||priorOwner.IsDisposed&&priorOwner.Transport.State==SoeClientTransportState.Closed,"Factory only after prior retired");return new(c.World,c.Store,queue,row.ConnectionId,row.Seed,maxSamples:3,flags:5);}
            var active=run==0?restart.Start(Factory):restart.Restart(Factory);
            Check(restart.Generation==run+1&&ReferenceEquals(restart.Active,active)&&ReferenceEquals(restart.LastCompletion,priorReport),"Successful admission frontier");
            bool called=false;Reject<InvalidOperationException>(()=>restart.Restart(()=>{called=true;return Factory();}));Check(!called&&ReferenceEquals(restart.Active,active),"Active restart refuses callback");
            Reject<ArgumentNullException>(()=>restart.Complete(null!));
            var receipts=new List<SceneWorldOfflineRootMotionSessionReceipt>();driver.SetManualInput(new(0,1),0);
            for(int i=1;i<=3;i++)
            {
                if(i==3)driver.SetManualInput(Vector2.Zero,20000);
                var state=driver.AdvanceNext((i-1)*10000,i*10000);Check(state.CompletedUpdates==i&&state.FaultType is null,"Actual bounded run tick");
                byte[] ack=OfflineSoeAcknowledgementFactory.Create(row.Fault&&i==3?(ushort)1:active.Session.NextSequence,row.Seed);
                try
                {
                    if(row.Fault&&i==3)Reject<InvalidDataException>(()=>active.Session.Observe(c.Human,c.Store.Find(c.Human)!,30000,30,ack));
                    else receipts.Add(active.Session.Observe(c.Human,c.Store.Find(c.Human)!,i*10000,i*10,ack));
                }finally{Array.Clear(ack);}
            }
            var current=c.Store.Find(c.Human)!;
            if(run==0)
            {
                queue.Enqueue(c.Human,current,8,9,30);Reject<InvalidOperationException>(()=>restart.Complete(queue));
                Check(restart.Generation==1&&restart.LastCompletion is null&&ReferenceEquals(restart.Active,active)&&!active.IsDisposed&&queue.Count==1,"Healthy outstanding queue rejection no retirement/rollback");
                Check(queue.TryTake(c.Generation,out var handed),"Caller releases outstanding queue");Array.Clear(handed!);
                active.Transport.QueueReliableMessage(new byte[]{1},2);Reject<InvalidOperationException>(()=>restart.Complete(queue));
                var busy=active.Transport.PumpQueuedDatagrams(30);foreach(var packet in busy)Array.Clear(packet);
                Reject<InvalidOperationException>(()=>restart.Complete(queue));
                byte[] otherAck=Convert.FromHexString(peer.Datagrams["otherAck"]);
                Check(active.Transport.AcceptDatagram(otherAck).ReleasedOutboundPackets==1,"Caller releases outstanding channel2");Array.Clear(otherAck);
            }
            var history=active.Session.State;var report=restart.Complete(queue);
            Check(active.IsDisposed&&active.Transport.State==SoeClientTransportState.Closed&&active.Session.State==history&&restart.Active is null&&restart.Generation==run+1&&ReferenceEquals(restart.LastCompletion,report),"Retirement then immutable report commit");
            int queueAtRetirement=queue.Count;
            Check(queueAtRetirement==(row.Fault?1:0)&&c.Store.IsCurrent(current),"Honest fault FIFO and scene borrower survival");
            if(run==0)
            {
                void Preserved(){Check(restart.Generation==1&&restart.Active is null&&ReferenceEquals(restart.LastCompletion,report),"Factory failure preserves generation/report");}
                Reject<ArgumentNullException>(()=>restart.Restart(null!));Preserved();
                Reject<InvalidDataException>(()=>restart.Restart(()=>null!));Preserved();
                Reject<InvalidDataException>(()=>restart.Restart(()=>throw new InvalidDataException("Synthetic factory failure")));Preserved();
                Reject<InvalidDataException>(()=>restart.Restart(()=>active));Preserved();
                foreach(string kind in new[]{"disposed","advanced","pending","retained","reentry"})
                {
                    OfflineSoeRootMotionSessionOwner? candidate=null;
                    OfflineSoeRootMotionSessionOwner BadFactory()
                    {
                        if(kind=="reentry"){try{restart.Restart(()=>throw new Exception("Must not call"));}catch(InvalidOperationException){}}
                        candidate=new(c.World,c.Store,queue,0x99112233,0,maxSamples:3);
                        if(kind=="disposed")candidate.Dispose();
                        if(kind=="advanced")candidate.Session.Observe(c.Human,current,10000,10,Array.Empty<byte>());
                        if(kind is "pending" or "retained")candidate.Transport.QueueReliableMessage(new byte[]{1});
                        if(kind=="retained"){var packets=candidate.Transport.PumpQueuedDatagrams(0);foreach(var packet in packets)Array.Clear(packet);}
                        return candidate;
                    }
                    if(kind=="reentry")Reject<InvalidOperationException>(()=>restart.Restart(BadFactory));
                    else Reject<InvalidDataException>(()=>restart.Restart(BadFactory));
                    Preserved();Check(candidate is not null&&candidate.IsDisposed&&candidate.Transport.State==SoeClientTransportState.Closed&&candidate.Transport.PendingMessageCount==0&&candidate.Transport.RetainedOutboundPackets==0&&queue.Count==0&&c.Store.IsCurrent(current),"Rejected returned prefix cleaned, borrowers survive");
                }
                bool invoked=false;Exception? error=null;var thread=new Thread(()=>{try{restart.Restart(()=>{invoked=true;return Factory();});}catch(Exception e){error=e;}});thread.Start();thread.Join();Check(error is InvalidOperationException&&!invoked,"Wrong thread cannot invoke factory");Preserved();
                invoked=false;Reject<InvalidOperationException>(()=>restart.Start(()=>{invoked=true;return Factory();}));Check(!invoked,"Start initial generation only");
            }
            queue.Dispose();Check(queue.Count==0&&report.QueueCount==queueAtRetirement&&ReferenceEquals(restart.LastCompletion,report),"Caller drain leaves immutable completion prefix");
            runs.Add(new{Generation=restart.Generation,ConnectionId=row.ConnectionId,Seed=row.Seed,RequestHex=active.RequestHex,ResponseHex=active.ResponseHex,Receipts=receipts,Completion=report,QueueAtRetirement=queueAtRetirement,TransportAtRetirement=active.Transport.State.ToString()});
            priorOwner=active;priorReport=report;
        }
        bool capCalled=false;Reject<InvalidOperationException>(()=>restart.Restart(()=>{capCalled=true;return priorOwner!;}));Check(!capCalled&&restart.Generation==3&&ReferenceEquals(restart.LastCompletion,priorReport),"Cap before callback");
        Reject<ArgumentNullException>(()=>restart.Complete(null!));
        restart.Dispose();restart.Dispose();Check(restart.IsDisposed&&restart.Active is null&&restart.Generation==3&&ReferenceEquals(restart.LastCompletion,priorReport),"Controller disposal preserves completed history");
        Reject<ObjectDisposedException>(()=>restart.Restart(()=>priorOwner!));
        Reject<ArgumentOutOfRangeException>(()=>new OfflineSoeRootMotionRestartOwner(0));Reject<ArgumentOutOfRangeException>(()=>new OfflineSoeRootMotionRestartOwner(33));
        using(var upper=new OfflineSoeRootMotionRestartOwner(32)){Check(upper.Generation==0,"Maximum bound admitted");}
        using(var c=new Context(peer,catalog,options))
        {
            c.Store.PublishLocal(c.Human,AppearanceTransform.Identity,0);var motion=c.Store.Find(c.Human)!;
            using var queue=new SceneWorldClientRootMotionQueue(c.World,c.Store,new(100,10));queue.Enqueue(c.Human,motion,1,1,0);
            using var controller=new OfflineSoeRootMotionRestartOwner(1);
            var child=controller.Start(()=>new(c.World,c.Store,queue,1,0,maxSamples:3));controller.Dispose();
            Check(child.IsDisposed&&child.Transport.State==SoeClientTransportState.Closed&&queue.Count==1&&c.Store.IsCurrent(motion)&&controller.LastCompletion is null,"Emergency disposal owns only active child, no invented completion");
            Reject<ObjectDisposedException>(()=>controller.Start(()=>child));
        }
        string? cleanupFailureType=null;OfflineSoeRootMotionCompletion? cleanupPreservedReport=null;
        using(var c=new Context(peer,catalog,options))
        {
            using var queue=new SceneWorldClientRootMotionQueue(c.World,c.Store,new(100,10));
            using var controller=new OfflineSoeRootMotionRestartOwner(2);
            controller.Start(()=>new(c.World,c.Store,queue,9,0,maxSamples:3));
            var preservedReport=controller.Complete(queue);
            cleanupPreservedReport=preservedReport;
            OfflineSoeRootMotionSessionOwner? foreignOwner=null;Exception? foreignError=null;bool foreignClosed=false;
            using var ready=new ManualResetEventSlim();using var release=new ManualResetEventSlim();
            var thread=new Thread(()=>
            {
                try
                {
                    using var foreign=new Context(peer,catalog,options);
                    using var foreignQueue=new SceneWorldClientRootMotionQueue(foreign.World,foreign.Store,new(100,10));
                    using var child=new OfflineSoeRootMotionSessionOwner(foreign.World,foreign.Store,foreignQueue,7,0,maxSamples:3);
                    foreignOwner=child;ready.Set();release.Wait();child.Dispose();
                    foreignClosed=child.IsDisposed&&child.Transport.State==SoeClientTransportState.Closed;
                }
                catch(Exception e){foreignError=e;ready.Set();}
            });
            thread.Start();try
            {
                Check(ready.Wait(5000)&&foreignError is null&&foreignOwner is not null,"Foreign owned prefix setup");
                AggregateException? aggregate=null;
                try{controller.Restart(()=>foreignOwner!);}catch(AggregateException e){aggregate=e;checks++;}
                Check(aggregate is not null&&aggregate.InnerExceptions.Count==2&&aggregate.InnerExceptions.All(e=>e is InvalidOperationException),"Original admission and cleanup causes retained");
                bool invoked=false;Reject<InvalidOperationException>(()=>controller.Start(()=>{invoked=true;return foreignOwner!;}));
                Reject<InvalidOperationException>(()=>controller.Restart(()=>{invoked=true;return foreignOwner!;}));
                Reject<InvalidOperationException>(()=>controller.Complete(queue));
                cleanupFailureType=controller.CleanupFaultType;
                Check(!invoked&&cleanupFailureType==nameof(InvalidOperationException)&&controller.Generation==1&&controller.Active is null&&ReferenceEquals(controller.LastCompletion,preservedReport),"Cleanup failure terminal and previous immutable report preserved");
                controller.Dispose();controller.Dispose();
                Check(controller.IsDisposed&&controller.CleanupFaultType==cleanupFailureType,"Dispose never retries foreign failed prefix");
            }
            finally{release.Set();thread.Join();}
            Check(foreignError is null&&foreignClosed,"Explicit producer-thread cleanup after terminal controller failure");
        }
        File.WriteAllText(outputPath,JsonSerializer.Serialize(new{Checks=checks,Runs=runs,CleanupFailureType=cleanupFailureType,CleanupPreservedReport=cleanupPreservedReport,OriginalAssets=0}));Console.WriteLine("PASS serialized restart checks="+checks);
    }
}
