using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

namespace Stardust.Core.SmokeTests;

public static class SceneWorldCombinedMotionApplicationScenarios
{
    public sealed record Peer(Dictionary<string,string> Applications,string[] Initial,string[] Sequence);
    private static int checks;
    private static void Check(bool value,string why){checks++;if(!value)throw new Exception(why);}
    private static void Reject<T>(Action action) where T:Exception
    {try{action();}catch(T){checks++;return;}throw new Exception("Expected "+typeof(T).Name);}
    private sealed class Context:IDisposable
    {
        public readonly SceneWorldCoordinator World;
        public readonly SceneWorldObjectMotionStore Store;
        public readonly SceneWorldCombinedMotionApplicationRouter Router;
        public readonly SceneWorldPresentationPump Pump;
        public readonly List<(CancellationToken Token,TaskCompletionSource<SceneObjectResources> Completion)> Jobs=new();
        public readonly List<SceneWorldCombinedMotionApplicationResult> Receipts=new();
        public readonly List<byte[]> BorrowedArrays=new();
        public readonly long Generation;
        public Action? ProjectionCallback;
        public int Projections,Factories;
        private readonly Peer peer;
        public byte[] Bytes(string key)=>Convert.FromHexString(peer.Applications[key]);
        public SceneWorldObjectSnapshot Human=>World.Snapshots().Single(s=>s.Object.Handle.ObjectId==0xF122334455667788UL);
        public Context(Peer peer,ObjectTemplateCatalog catalog,Dictionary<SceneObjectResourceRecipe,SceneObjectResourceOptions> options)
        {
            this.peer=peer;
            World=new(catalog,options,(q,t)=>{var job=new TaskCompletionSource<SceneObjectResources>();Jobs.Add((t,job));return job.Task;},SceneCreateZeroQuaternionPolicy.Reject,4,2,32,8192,16,4096,1024,
                new(SceneWorldReplicationMode.CreatureView3MixedComponents,new(1024,64,64,16,512),16,1024,new(1024,32,24,64,64,16),CreatureWoundsApplicationPolicy.LegacyBounded,new(1024,32,64,64,16),new(16,16)));
            Store=new(World);
            Router=new(World,Store,(raw,snapshot)=>{Projections++;ProjectionCallback?.Invoke();return(new AppearanceTransform(Vector3.UnitX,Vector3.UnitY,Vector3.UnitZ,raw.NativePosition),2);},SceneCreateZeroQuaternionPolicy.Identity,ObjectControllerTransformDirection.ServerToClient);
            Pump=new(World,(p,s)=>{Factories++;throw new Exception("No resources completed");},new(MaximumQueuedMessages:32,MaximumQueuedBytes:1024,MaximumMessageBytes:256,MaximumMessagesPerStep:3),applicationReceiver:(g,b)=>
            {if(MemoryMarshal.TryGetArray(b,out var segment))BorrowedArrays.Add(segment.Array!);var receipt=Router.Receive(g,b.Span);Receipts.Add(receipt);return receipt.Accepted;});
            Generation=World.BeginSelection(0xF122334455667788UL).Generation;
        }
        public void Queue(string key){var bytes=Bytes(key);Pump.QueueApplication(Generation,bytes);Array.Clear(bytes);}
        public void Stage()
        {
            foreach(var key in peer.Initial)Queue(key);
            Check(Pump.QueuedMessageCount==7,"Initial queued stage");
            foreach(int remaining in new[]{4,1,0}){var step=Pump.Step(0);Check(step.PendingMessages==remaining,"Three-message cap during stage");}
            Check(World.CompleteMinimumSceneBarrier(Generation,true) is not null,"Barrier");Pump.Step(0);
            Check(Jobs.Count==2&&Factories==0,"Controlled uncompleted jobs, no assets/actors");
        }
        public void Dispose()
        {
            Pump.Dispose();foreach(var job in Jobs)job.Completion.TrySetException(new Exception("Synthetic late cancellation"));Pump.DrainCompletion.GetAwaiter().GetResult();
            Check(Jobs.All(j=>j.Token.IsCancellationRequested&&j.Completion.Task.IsCompleted),"Canceled and observed jobs");
            Check(BorrowedArrays.All(b=>b.All(v=>v==0)),"Pump clears every consumed owned buffer");
            Router.Dispose();Store.Dispose();
        }
    }
    public static void Run(string peerPath,string outputPath)
    {
        checks=0;var peer=JsonSerializer.Deserialize<Peer>(File.ReadAllBytes(peerPath))!;
        var catalog=ObjectTemplateCatalogProfile.Decode(File.ReadAllBytes("poc/preview-profiles/scene-object-catalog.json"));
        using var bankFile=File.OpenRead("poc/preview-profiles/human-locomotion.json");var bank=SkeletalAnimationBankProfile.Load(bankFile);
        var options=new Dictionary<SceneObjectResourceRecipe,SceneObjectResourceOptions>{{SceneObjectResourceRecipe.CreatureDetail0,new SceneCreatureResourceOptions(new("appearance/lat/all_m.lat","emt_standing_idle_ambient_1_ag","798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B"),bank,true)},{SceneObjectResourceRecipe.StaticAppearance,new SceneStaticResourceOptions(SceneStaticMaterialPolicy.RequireComplete,null)}};
        var observed=new List<object>();var steps=new List<object>();
        using(var c=new Context(peer,catalog,options))
        {
            c.Stage();Check(c.Receipts.All(r=>r.Route==SceneWorldCombinedMotionRoute.World&&r.Accepted),"Ordinary lifecycle fallback");c.Receipts.Clear();
            foreach(var key in peer.Sequence)c.Queue(key);
            int offset=0;
            while(c.Pump.QueuedMessageCount>0)
            {
                var step=c.Pump.Step(0);steps.Add(new{step.AcceptedMessages,step.DroppedMessages,step.PendingMessages});
                foreach(var receipt in c.Receipts.Skip(offset))
                {
                    string key=peer.Sequence[offset++];var current=c.Store.Find(c.Human);
                    if(receipt.FullResult is {Applied:true} full)Check(ReferenceEquals(receipt.Motion,full.Motion)&&full.Motion!.Authoritative,"Full actual receipt identity");
                    if(receipt.FullResult is {Status:SceneWorldFullTransformApplicationStatus.CounterRejected} rejected)Check(ReferenceEquals(rejected.Motion,rejected.PriorMotion),"Duplicate prior identity");
                    var motion=receipt.Motion;
                    observed.Add(new{Key=key,Route=receipt.Route.ToString(),receipt.Accepted,FullStatus=receipt.FullResult?.Status.ToString(),Revision=motion?.Revision,Counter=motion?.MovementCounter,
                        Rows=motion is null?null:new[]{motion.Transform.RowX.X,motion.Transform.RowX.Y,motion.Transform.RowX.Z,motion.Transform.RowY.X,motion.Transform.RowY.Y,motion.Transform.RowY.Z,motion.Transform.RowZ.X,motion.Transform.RowZ.Y,motion.Transform.RowZ.Z},
                        Position=motion is null?null:new[]{motion.Transform.Translation.X,motion.Transform.Translation.Y,motion.Transform.Translation.Z}});
                    Check(receipt.FullResult is null||Convert.ToHexString(ObjectControllerTransformCodec.Encode(receipt.FullResult.Raw))==peer.Applications[key],"Raw full receipt retained after pump clear");
                }
            }
            Check(c.Store.Find(c.Human)!.MovementCounter==1,"Mixed route ordering");
            var held=c.Store.Find(c.Human)!;Check(ReferenceEquals(c.Router.LastResult,c.Receipts[^1]),"Last completed historical receipt");
            Check(!c.Router.Accept(c.Generation-1,c.Bytes("fullNext")),"Stale preflight");
            c.Router.Dispose();Check(c.Store.IsCurrent(held)&&c.World.State!=ZoneSceneTransitionState.Closed,"Borrowed wrapper disposal survival");
            Reject<ObjectDisposedException>(()=>c.Router.Accept(c.Generation,c.Bytes("fullNext")));
        }
        var failures=new List<object>();
        foreach(string bad in new[]{"malformed","unknownOc","callbackThrow","callbackCaughtAccept","callbackCaughtDispose","callbackWorldPrefix"})
        using(var c=new Context(peer,catalog,options))
        {
            c.Stage();c.Queue("fullFirst");c.Pump.Step(0);var held=c.Store.Find(c.Human)!;ulong revision=held.Revision;
            if(bad.StartsWith("callback"))c.ProjectionCallback=()=>
            {
                if(bad=="callbackThrow")throw new InvalidDataException("Synthetic projector failure");
                if(bad=="callbackWorldPrefix"){c.World.Accept(c.Generation,c.Bytes("heightA"));Check(c.Human.Replication!.Scalars.Height==1.4f,"Explicit callback world-mutation prefix");}
                try{if(bad=="callbackCaughtDispose")c.Router.Dispose();else c.Router.Accept(c.Generation,c.Bytes("fullNext"));}catch(InvalidOperationException){}
                Check(ReferenceEquals(held,c.Store.Find(c.Human)),"Caught reentry cannot publish");
            };
            c.Queue(bad.StartsWith("callback")?"compactWrap":bad);c.Queue("fullNext");
            if(bad.StartsWith("callback")&&bad!="callbackThrow")Reject<InvalidOperationException>(()=>c.Pump.Step(0));else Reject<InvalidDataException>(()=>c.Pump.Step(0));
            Check(c.World.State==ZoneSceneTransitionState.Closed&&c.Pump.QueuedMessageCount==0&&c.Pump.QueuedMessageBytes==0,"Pump failure closes world and queued suffix");
            Check(held.Revision==revision&&held.MovementCounter==uint.MaxValue,"Completed prior prefix retained as historical value");
            Check(!c.Store.IsCurrent(held)&&c.Jobs.All(j=>j.Token.IsCancellationRequested),"Close retires motion/cancels jobs");
            failures.Add(new{Key=bad,Closed=true,QueueCleared=true,PriorRevision=revision,PriorCounter=held.MovementCounter});
        }
        using(var c=new Context(peer,catalog,options))
        {
            c.Stage();c.Router.Accept(c.Generation,c.Bytes("fullFirst"));var held=c.Store.Find(c.Human)!;var last=c.Router.LastResult;
            foreach(var bad in new[]{"malformed","badCount","extra","nonfinite","shortOc"})
            {Reject<InvalidDataException>(()=>c.Router.Receive(c.Generation,c.Bytes(bad)));Check(ReferenceEquals(held,c.Store.Find(c.Human))&&ReferenceEquals(last,c.Router.LastResult),"Direct malformed preserves prior receipt/state");}
            Reject<OperationCanceledException>(()=>c.Router.Receive(c.Generation,c.Bytes("fullNext"),new CancellationToken(true)));
            Exception? error=null;var thread=new Thread(()=>{try{c.Router.Accept(c.Generation,c.Bytes("fullNext"));}catch(Exception e){error=e;}});thread.Start();thread.Join();Check(error is InvalidOperationException&&ReferenceEquals(held,c.Store.Find(c.Human)),"Foreign thread atomic");
            var mutable=c.Bytes("compactWrap");c.ProjectionCallback=()=>Array.Clear(mutable);
            var compactResult=c.Router.Receive(c.Generation,mutable);Check(compactResult.Accepted&&ReferenceEquals(compactResult.Motion,c.Store.Find(c.Human))&&mutable.All(b=>b==0),"Callback source mutation cannot corrupt post-publication receipt");
            c.ProjectionCallback=null;
            c.Store.Dispose();Check(c.Router.Receive(c.Generation,c.Bytes("heightA")).Route==SceneWorldCombinedMotionRoute.DeclinedIdentity,"Closed store preflight includes fallback");
        }
        using(var c=new Context(peer,catalog,options))using(var foreign=new Context(peer,catalog,options))
        {
            Reject<ArgumentException>(()=>new SceneWorldCombinedMotionApplicationRouter(c.World,foreign.Store,(p,s)=>(AppearanceTransform.Identity,0),SceneCreateZeroQuaternionPolicy.Identity,ObjectControllerTransformDirection.ServerToClient));
            Reject<ArgumentOutOfRangeException>(()=>new SceneWorldCombinedMotionApplicationRouter(c.World,c.Store,(p,s)=>(AppearanceTransform.Identity,0),SceneCreateZeroQuaternionPolicy.Identity,ObjectControllerTransformDirection.ClientToServer));
            for(int i=0;i<32;i++)foreign.Queue("ready");Reject<InvalidDataException>(()=>foreign.Queue("ready"));Check(foreign.Pump.QueuedMessageCount==32&&foreign.Pump.QueuedMessageBytes==192,"Count capacity preserves prefix");
            Reject<ArgumentOutOfRangeException>(()=>c.Pump.QueueApplication(c.Generation,new byte[257]));
            for(int i=0;i<16;i++)c.Queue("fullNext");int count=c.Pump.QueuedMessageCount;long bytes=c.Pump.QueuedMessageBytes;
            Reject<InvalidDataException>(()=>c.Queue("fullNext"));Check(c.Pump.QueuedMessageCount==count&&c.Pump.QueuedMessageBytes==bytes&&bytes==992,"Byte capacity preserves queued prefix");
        }
        File.WriteAllText(outputPath,JsonSerializer.Serialize(new{Observed=observed,Steps=steps,Failures=failures,Checks=checks,OriginalAssetReads=0}));Console.WriteLine("PASS combined ingress checks="+checks);
    }
}
