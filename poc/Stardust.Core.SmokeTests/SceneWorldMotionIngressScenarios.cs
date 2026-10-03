using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

namespace Stardust.Core.SmokeTests;

/// <summary>Offline synthetic wire/state fixtures through the actual native ingress and pump.</summary>
public static class SceneWorldMotionIngressScenarios
{
    private static int checks;
    private static void Check(bool value, string why) { checks++; if (!value) throw new Exception(why); }
    private static void Reject<T>(Action action) where T : Exception
    { try { action(); } catch (T) { checks++; return; } throw new Exception("Expected " + typeof(T).Name); }
    public sealed record Peer(Dictionary<string,string> Applications, string[] Initial);
    private sealed class Fake(ScenePreparedObjectPublication publication) : ISceneWorldObjectPresentation
    {
        public ScenePreparedObjectPublication Publication { get; } = publication;
        public int Disposals;
        public bool Tick(double seconds) => true;
        public bool Apply(SceneWorldObjectSnapshot snapshot) => true;
        public void Dispose() => Disposals++;
    }
    private sealed record Job(SceneObjectResourceRequest Request, CancellationToken Token, TaskCompletionSource<SceneObjectResources> Task);
    private sealed class Context : IDisposable
    {
        public readonly SceneWorldCoordinator World;
        public readonly SceneWorldObjectMotionStore Motions;
        public readonly SceneWorldMotionApplicationRouter Router;
        public readonly SceneWorldPresentationPump Pump;
        public readonly List<Job> Jobs = new();
        public readonly List<Fake> Actors = new();
        public readonly long Generation;
        public Action? Callback;
        public int ProjectionCalls;
        private readonly Peer peer;
        private readonly Dictionary<SceneObjectResourceRecipe,SceneObjectResources> resources;
        public byte[] Bytes(string key) => Convert.FromHexString(peer.Applications[key]);
        public SceneWorldObjectSnapshot Human => World.Snapshots().Single(s => s.Object.Handle.ObjectId == 0xF122334455667788UL);
        public Context(Peer peer, ObjectTemplateCatalog catalog, IReadOnlyDictionary<SceneObjectResourceRecipe,SceneObjectResourceOptions> options,
            Dictionary<SceneObjectResourceRecipe,SceneObjectResources> resources, bool barrier = true)
        {
            this.peer = peer; this.resources = resources;
            World = new(catalog, options, (q,ct) => { var t = new TaskCompletionSource<SceneObjectResources>(); Jobs.Add(new(q,ct,t)); return t.Task; },
                SceneCreateZeroQuaternionPolicy.Reject, maximumObjects:2, maximumOutstandingJobs:3,
                replicationPolicy:new(SceneWorldReplicationMode.CreatureView3Scalars,new(1024,64,64,16,1024),32,16384));
            Motions = new(World,2);
            Router = new(World,Motions,(packet,snapshot) =>
            {
                ProjectionCalls++;
                Check(packet.RawX == -32768 && packet.RawZ == 9 && packet.RawY == -7 && packet.SpeedRaw == -128 && packet.DirectionRaw == 127,
                    "Projector receives exact signed metadata");
                Callback?.Invoke();
                return (new AppearanceTransform(Vector3.UnitX,Vector3.UnitY,Vector3.UnitZ,packet.NativePosition),2f);
            });
            Pump = new(World,(p,s) => { var actor = new Fake(p); Actors.Add(actor); return actor; },
                new(MaximumMessagesPerStep:3),applicationReceiver:(g,b) => Router.Accept(g,b.Span));
            Generation = World.BeginSelection(0xF122334455667788UL).Generation;
            foreach (var key in peer.Initial) Check(Router.Accept(Generation,Bytes(key)),"Ordinary initial stage");
            if (barrier) Check(World.CompleteMinimumSceneBarrier(Generation,true) is not null,"Minimum barrier");
        }
        public void Complete() { foreach (var j in Jobs) j.Task.TrySetResult(resources[j.Request.Options.Recipe]); }
        public void Dispose()
        {
            Pump.Dispose();
            foreach (var j in Jobs) j.Task.TrySetException(new Exception("Synthetic late completion"));
            Pump.DrainCompletion.GetAwaiter().GetResult();
            Check(Jobs.All(j => j.Token.IsCancellationRequested || j.Task.Task.IsCompleted),"Jobs observed");
            Check(Actors.All(a => a.Disposals == 1),"Exactly once actor disposal");
            Router.Dispose(); Motions.Dispose();
        }
    }
    public static void Run(string[] inputs, string peerPath, string exportPath)
    {
        checks = 0;
        var peer = JsonSerializer.Deserialize<Peer>(File.ReadAllBytes(peerPath)) ?? throw new InvalidDataException("Peer required");
        var catalog = ObjectTemplateCatalogProfile.Decode(File.ReadAllBytes(inputs[2]));
        using var bankFile = File.OpenRead(inputs[3]); var bank = SkeletalAnimationBankProfile.Load(bankFile);
        var options = new Dictionary<SceneObjectResourceRecipe,SceneObjectResourceOptions>
        {
            [SceneObjectResourceRecipe.CreatureDetail0] = new SceneCreatureResourceOptions(new("appearance/lat/all_m.lat","emt_standing_idle_ambient_1_ag","798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B"),bank,true),
            [SceneObjectResourceRecipe.StaticAppearance] = new SceneStaticResourceOptions(SceneStaticMaterialPolicy.RequireComplete,new("shader/anim_mission_terminal.sht","B5AA5A251A76AD8F152DB85F50FBC3061D0494847499F1068A9B55C375785A62",0))
        };
        var cfg = File.ReadAllBytes(inputs[1]);
        Check(Convert.ToHexString(SHA256.HashData(cfg)) == RetainedSkeletalDetailBundleReader.ConfigurationPin,"Configuration pin");
        int archiveScopes = 0;
        var resources = new Dictionary<SceneObjectResourceRecipe,SceneObjectResources>();
        foreach (var key in new[] { "humanCreate", "terminalCreate" })
        {
            var create = SceneObjectLifecycleCodec.DecodeCreate(Convert.FromHexString(peer.Applications[key]));
            var resource = Task.Run(() =>
            {
                Interlocked.Increment(ref archiveScopes);
                try { using var text = new StringReader(System.Text.Encoding.UTF8.GetString(cfg)); using var overlay = ConfiguredTreOverlay.Open(inputs[0],text);
                    return SceneObjectResourceRouter.Load(create,catalog,options[catalog.Require(create.TemplateCrc).Recipe],overlay.Lookup); }
                finally { Interlocked.Decrement(ref archiveScopes); }
            }).GetAwaiter().GetResult();
            resources.Add(resource.Kind,resource);
        }
        Array.Clear(cfg); Check(archiveScopes == 0,"Archive scopes closed before queueing");
        Context New(bool barrier = true) => new(peer,catalog,options,resources,barrier);
        var gates = new Dictionary<string,bool>();
        void Gate(string key, bool value) { Check(value,key); gates.Add(key,value); }
        using (var c = New(false))
        {
            Gate("BeforeBarrier",!c.Router.Accept(c.Generation,c.Bytes("first")) && c.ProjectionCalls == 0);
            var malformed = c.Bytes("malformed"); Reject<InvalidDataException>(() => c.Router.Accept(c.Generation,malformed));
        }
        using (var c = New())
        {
            c.Pump.QueueApplication(c.Generation-1,c.Bytes("malformed"));
            c.Pump.QueueApplication(c.Generation,c.Bytes("ready"));
            var step = c.Pump.Step(0);
            Gate("PumpStaleGeneration",step.AcceptedMessages == 1 && step.DroppedMessages == 1 && c.World.State == ZoneSceneTransitionState.Ready);
        }
        var serial = new List<object>();
        using (var c = New())
        {
            foreach (var key in new[] { "first","wrap","duplicate","older","half","next" })
            {
                bool accepted = c.Router.Accept(c.Generation,c.Bytes(key)); var m = c.Motions.Find(c.Human)!;
                serial.Add(new { Key=key,Accepted=accepted,m.Revision,Counter=m.MovementCounter,X=m.Transform.Translation.X,Y=m.Transform.Translation.Y,Z=m.Transform.Translation.Z,m.Speed });
            }
            int before = c.ProjectionCalls;
            Gate("Missing",!c.Router.Accept(c.Generation,c.Bytes("missing")) && c.ProjectionCalls == before);
            Gate("Parents",!c.Router.Accept(c.Generation,c.Bytes("parentZero")) && !c.Router.Accept(c.Generation,c.Bytes("parentNonzero")) && c.ProjectionCalls == before);
            Gate("StaleGeneration",!c.Router.Accept(c.Generation-1,c.Bytes("malformed")) && c.Motions.Find(c.Human)!.Revision == 3);
            var oldIncarnation = c.Human.Object.Incarnation;
            Check(c.Router.Accept(c.Generation,c.Bytes("humanDestroy")),"Destroy delegation");
            Check(c.Router.Accept(c.Generation,c.Bytes("humanCreate")),"Create delegation");
            Gate("Pending",!c.Router.Accept(c.Generation,c.Bytes("next")) && c.Motions.Find(c.Human) is null);
            Check(c.Router.Accept(c.Generation,c.Bytes("humanBaseline")),"Recreation baseline");
            Check(c.Router.Accept(c.Generation,c.Bytes("humanEnd")),"Recreation finalization");
            Gate("Recreation",c.Router.Accept(c.Generation,c.Bytes("next")) && c.Human.Object.Incarnation != oldIncarnation && c.Motions.Find(c.Human)!.Revision == 4);
        }
        using (var c = New())
        {
            Check(c.Router.Accept(c.Generation,c.Bytes("first")),"Seed row before callback clear");
            c.Callback = c.Motions.Clear;
            Gate("MotionMutation",!c.Router.Accept(c.Generation,c.Bytes("next")) && c.Motions.Find(c.Human) is null);
        }
        using (var c = New())
        {
            c.Callback = () => c.World.Accept(c.Generation,c.Bytes("terminalDelta"));
            Gate("OtherObjectMutation",!c.Router.Accept(c.Generation,c.Bytes("next")) && c.Motions.Find(c.Human) is null);
        }
        object pumpReceipt;
        using (var c = New())
        {
            var copy = c.Bytes("next"); c.Pump.QueueApplication(c.Generation,c.Bytes("heightA"));
            c.Pump.QueueApplication(c.Generation,copy); Array.Clear(copy);
            c.Pump.QueueApplication(c.Generation,c.Bytes("parentZero"));
            var two = c.Bytes("next"); two[20] = 2; c.Pump.QueueApplication(c.Generation,two); Array.Clear(two);
            var first = c.Pump.Step(0); Check(c.Jobs.Count == 2,"Controlled loaders start"); c.Complete();
            var second = c.Pump.Step(0); Check(c.Human.Replication!.Scalars.Height == BitConverter.UInt32BitsToSingle(0x3FB33333),"Ordinary scalar delegated");
            pumpReceipt = new { first.AcceptedMessages,first.DroppedMessages,first.PendingMessages,SecondAccepted=second.AcceptedMessages,FinalCounter=c.Motions.Find(c.Human)!.MovementCounter,Actors=c.Pump.Consumer.Count };
            Check(first.AcceptedMessages == 2 && first.DroppedMessages == 1 && first.PendingMessages == 1 && second.AcceptedMessages == 1 && c.Actors.Count == 2,"Actual pump cap/copy/publication");
            bool foreignRejected=false; Exception? foreignError=null;byte[] foreignBytes=c.Bytes("next");
            var foreignThread=new Thread(()=> {try {c.Router.Accept(c.Generation,foreignBytes);}catch(InvalidOperationException){foreignRejected=true;}catch(Exception error){foreignError=error;}}){IsBackground=true};
            foreignThread.Start();Check(foreignThread.Join(5000),"Foreign owner probe deadline");Array.Clear(foreignBytes);
            if(foreignError is not null)throw foreignError;
            Gate("OwnerThread",foreignRejected);
            c.Router.Dispose(); c.Router.Dispose();
            Gate("SeparateDisposal",c.World.State == ZoneSceneTransitionState.AwaitingServerReady && c.Motions.Find(c.Human) is not null);
            Reject<ObjectDisposedException>(() => c.Router.Accept(c.Generation,c.Bytes("next")));
        }
        using (var c = New())
        {
            c.Callback = () => c.World.Accept(c.Generation,c.Bytes("heightB"));
            Gate("Freshness",!c.Router.Accept(c.Generation,c.Bytes("next")) && c.Motions.Find(c.Human) is null);
        }
        using (var c = New())
        {
            c.Callback = () => c.World.BeginSelection(0xF122334455667788UL);
            Gate("Reset",!c.Router.Accept(c.Generation,c.Bytes("next")) && c.World.Generation != c.Generation);
        }
        foreach (var failure in new[] { "CaughtReentry","ThrownProjection","MalformedCleanup" })
        {
            using var c = New(); c.Pump.Step(0); c.Complete(); c.Pump.Step(0); Check(c.Actors.Count == 2,"Failure owns actors");
            if (failure == "CaughtReentry") c.Callback = () => { try { c.Router.Accept(c.Generation,c.Bytes("next")); } catch (InvalidOperationException) { } };
            if (failure == "ThrownProjection") c.Callback = () => throw new InvalidOperationException("Synthetic projector failure");
            c.Pump.QueueApplication(c.Generation,c.Bytes(failure == "MalformedCleanup" ? "malformed" : "next"));
            c.Pump.QueueApplication(c.Generation,c.Bytes("heightA"));
            if (failure == "MalformedCleanup") Reject<InvalidDataException>(() => c.Pump.Step(0)); else Reject<InvalidOperationException>(() => c.Pump.Step(0));
            Gate(failure,c.World.State == ZoneSceneTransitionState.Closed && c.World.Snapshots().Count == 0 && c.Pump.QueuedMessageCount == 0 && c.Pump.Consumer.Count == 0 && c.Actors.All(a => a.Disposals == 1));
        }
        Gate("Cleanup",archiveScopes == 0);
        File.WriteAllText(exportPath,JsonSerializer.Serialize(new { Serial=serial,Pump=pumpReceipt,Gates=gates,OriginalSources=new[] { resources[SceneObjectResourceRecipe.CreatureDetail0].Sources.Count,resources[SceneObjectResourceRecipe.StaticAppearance].Sources.Count },Checks=checks },new JsonSerializerOptions { WriteIndented=true }));
        Console.WriteLine($"Motion ingress: {checks} native checks passed");
    }
}
