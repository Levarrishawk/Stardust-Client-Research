using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

namespace Stardust.Core.SmokeTests;

/// <summary>Baseline observations and injected pump obligations, with no worker pump implementation.</summary>
public static class SceneWorldPumpPeerScenarios
{
    private static void Check(bool value,string why){if(!value)throw new Exception(why);}
    private static string Hex(ReadOnlySpan<byte> bytes)=>Convert.ToHexString(bytes);
    public sealed record Frame(string Name,string[] Queue,string[] Ingress,int[] Complete,bool AllowAdapters,bool Failure);
    public sealed record Peer(int Version,string Label,Dictionary<string,string> Applications,string[] Initial,Frame[] Frames,SceneWorldPresentationPumpPolicy Policy,string[] Obligations);
    private sealed class Fake(ScenePreparedObjectPublication publication):ISceneWorldObjectPresentation
    {
        public ScenePreparedObjectPublication Publication{get;}=publication;
        public SceneWorldObjectSnapshot? Last;public int Disposals;
        public bool Tick(double seconds)=>true;
        public bool Apply(SceneWorldObjectSnapshot snapshot){Last=snapshot;return true;}
        public void Dispose(){Disposals++;Last=null;}
    }
    private sealed record Job(SceneObjectResourceRequest Request,CancellationToken Token,TaskCompletionSource<SceneObjectResources> Task);
    private sealed class Context:IDisposable
    {
        public readonly SceneWorldCoordinator World;private SceneWorldObjectPresentationConsumer? baselineConsumer;
        public SceneWorldObjectPresentationConsumer Consumer=>baselineConsumer??=new(World,AdapterFactory,2);
        public Func<ScenePreparedObjectPublication,SceneWorldObjectSnapshot,ISceneWorldObjectPresentation?> AdapterFactory=>CreateAdapter;
        public readonly List<Job> Jobs=new();public readonly List<Fake> Created=new();public bool Allow;
        public readonly long Generation;private readonly Peer peer;private readonly Dictionary<SceneObjectResourceRecipe,SceneObjectResources> resources;
        public Context(Peer peer,ObjectTemplateCatalog catalog,IReadOnlyDictionary<SceneObjectResourceRecipe,SceneObjectResourceOptions> options,Dictionary<SceneObjectResourceRecipe,SceneObjectResources> resources)
        {
            this.peer=peer;this.resources=resources;
            World=new(catalog,options,(q,ct)=>{var task=new TaskCompletionSource<SceneObjectResources>();Jobs.Add(new(q,ct,task));return task.Task;},SceneCreateZeroQuaternionPolicy.Reject,
                maximumObjects:2,maximumOutstandingJobs:3,maximumStagedMessages:16,maximumEnvelopes:8,maximumEnvelopeBytes:4096,maximumPayloadBytes:1024,
                replicationPolicy:new(SceneWorldReplicationMode.CreatureView3Scalars,new(1024,64,64,16,1024),32,16384));
            ulong id=SceneObjectLifecycleCodec.DecodeCreate(Bytes("humanCreate")).ObjectId;
            Generation=World.BeginSelection(id).Generation;
            foreach(string key in peer.Initial)Check(World.Accept(Generation,Bytes(key)),"Initial stage");
            Check(Hex(World.CompleteMinimumSceneBarrier(Generation,true)!)==peer.Applications["ready"],"Atomic initial ready");
            World.Accept(Generation,Bytes("ready"));
        }
        private ISceneWorldObjectPresentation? CreateAdapter(ScenePreparedObjectPublication publication,SceneWorldObjectSnapshot snapshot)
        {if(!Allow)return null;var fake=new Fake(publication);Created.Add(fake);return fake;}
        public byte[] Bytes(string key)=>Convert.FromHexString(peer.Applications[key]);
        public void Complete(int index)=>Jobs[index].Task.SetResult(resources[Jobs[index].Request.Options.Recipe]);
        public void Dispose()
        {
            try{baselineConsumer?.Dispose();}finally{World.Close();foreach(var job in Jobs)job.Task.TrySetException(new Exception("Synthetic close drain"));World.DrainCompletion.GetAwaiter().GetResult();}
            Check(Created.All(f=>f.Disposals==1),"Exactly once owned fake retirement");
        }
    }
    private static object ObjectView(SceneWorldObjectSnapshot v)=>new
    {
        ObjectId=v.Object.Handle.ObjectId,v.Object.Handle.Revision,v.Object.Incarnation.CreationRevision,RegistryGeneration=v.Object.Handle.SceneGeneration,
        Phase=v.Object.Phase.ToString(),CreateHex=Hex(SceneObjectLifecycleCodec.EncodeCreate(v.Placement.RawCreate)),ScalarRevision=v.Replication?.Revision,
        Scalars=v.Replication is {} r?(object)new {r.Scalars.Posture,r.Scalars.Rank,r.Scalars.LinkId,HeightBits=BitConverter.SingleToUInt32Bits(r.Scalars.Height),r.Scalars.Shock,r.Scalars.States}:null,
        OpaqueHex=v.Envelopes.Select(e=>Hex(e.CopyOpaqueFields())).ToArray()
    };
    private static object PollView(SceneWorldObjectResourcePoll p)=>new
    {Prepared=p.Prepared.Select(r=>new {r.Ticket.Handle.ObjectId,r.Ticket.Handle.Revision,r.Ticket.Attempt}).ToArray(),
        Retired=p.Retired.Select(r=>new {r.Ticket.Handle.ObjectId,r.Ticket.Handle.Revision,r.Ticket.Attempt,Kind=r.Kind.ToString(),HasResources=r.Resources is not null}).ToArray()};

    public static void Run(string[] originalInputs,string peerPath,string exportPath,
        Func<SceneWorldCoordinator,Func<ScenePreparedObjectPublication,SceneWorldObjectSnapshot,ISceneWorldObjectPresentation?>,SceneWorldPresentationPumpPolicy,ISceneWorldPresentationPump>? pumpFactory)
    {
        Check(originalInputs.Length==4,"root/config/catalog/bank");byte[] source=File.ReadAllBytes(peerPath);Check(source.Length<=1048576,"Fixture byte cap");
        var peer=JsonSerializer.Deserialize<Peer>(source)??throw new InvalidDataException("Peer required");
        Check(peer.Version==1&&peer.Frames.Length==8&&peer.Policy.MaximumMessagesPerStep==2,"Bounded fixture shape");
        var catalog=ObjectTemplateCatalogProfile.Decode(File.ReadAllBytes(originalInputs[2]));using var bankFile=File.OpenRead(originalInputs[3]);var bank=SkeletalAnimationBankProfile.Load(bankFile);
        var options=new Dictionary<SceneObjectResourceRecipe,SceneObjectResourceOptions>
        {
            [SceneObjectResourceRecipe.CreatureDetail0]=new SceneCreatureResourceOptions(new("appearance/lat/all_m.lat","emt_standing_idle_ambient_1_ag","798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B"),bank,true),
            [SceneObjectResourceRecipe.StaticAppearance]=new SceneStaticResourceOptions(SceneStaticMaterialPolicy.RequireComplete,new("shader/anim_mission_terminal.sht","B5AA5A251A76AD8F152DB85F50FBC3061D0494847499F1068A9B55C375785A62",0))
        };
        byte[] cfg=File.ReadAllBytes(originalInputs[1]);Check(Hex(SHA256.HashData(cfg))==RetainedSkeletalDetailBundleReader.ConfigurationPin,"Configuration pin");
        int originalJobs=0,archiveScopes=0;var resources=new Dictionary<SceneObjectResourceRecipe,SceneObjectResources>();
        foreach(string key in new[]{"humanCreate","terminalCreate"})
        {
            var create=SceneObjectLifecycleCodec.DecodeCreate(Convert.FromHexString(peer.Applications[key]));
            var result=Task.Run(()=>
            {
                Interlocked.Increment(ref originalJobs);Interlocked.Increment(ref archiveScopes);
                try{using var text=new StringReader(System.Text.Encoding.UTF8.GetString(cfg));using var overlay=ConfiguredTreOverlay.Open(originalInputs[0],text);
                    return SceneObjectResourceRouter.Load(create,catalog,options[catalog.Require(create.TemplateCrc).Recipe],overlay.Lookup);}
                finally{Interlocked.Decrement(ref archiveScopes);}
            }).GetAwaiter().GetResult();resources.Add(result.Kind,result);
        }
        Array.Clear(cfg);Check(archiveScopes==0,"Archives close before controlled transfer");
        Context New()=>new(peer,catalog,options,resources);
        ulong human=SceneObjectLifecycleCodec.DecodeCreate(Convert.FromHexString(peer.Applications["humanCreate"])).ObjectId;
        var baselineFrames=new List<object>();bool delayed=false,recreated=false,retired=false,family=false;
        using(var c=New())
        {
            ScenePreparedObjectPublication? heldHuman=null;string? humanBefore=null;
            foreach(var f in peer.Frames)
            {
                c.Allow=f.AllowAdapters;foreach(int i in f.Complete)c.Complete(i);
                int accepted=0,dropped=0;bool failed=false;
                foreach(string key in f.Ingress)
                {
                    try{if(c.World.Accept(key=="stale"?c.Generation-1:c.Generation,c.Bytes(key)))accepted++;else dropped++;}
                    catch(InvalidDataException){failed=true;break;}
                }
                Check(failed==f.Failure,"Baseline explicit frame failure");
                var poll=failed?new SceneWorldObjectResourcePoll([],[]):c.World.PollObjectResources();
                if(f.Name=="held-publication")
                {heldHuman=poll.Prepared.Single();Check(!c.Consumer.AcceptResource(heldHuman),"Accepted consumer can defer publication");humanBefore=JsonSerializer.Serialize(ObjectView(c.World.Snapshots().Single(v=>v.Object.Handle.ObjectId==human)));}
                if(f.Name=="stale-terminal-completion")
                {retired=poll.Retired.Single().Kind==SceneObjectResourceCompletionKind.Stale&&poll.Retired[0].Resources is null;
                    recreated=c.World.IsCurrent(heldHuman!)&&humanBefore==JsonSerializer.Serialize(ObjectView(c.World.Snapshots().Single(v=>v.Object.Handle.ObjectId==human)))&&c.Jobs.Count==3;}
                if(f.Name=="retry-latest-state")
                {Check(c.Consumer.AcceptResource(heldHuman!),"Direct accepted consumer retry");foreach(var p in poll.Prepared)Check(c.Consumer.AcceptResource(p),"Terminal current resource");
                    delayed=((Fake)c.Consumer.Find(human)!).Last!.Replication!.Scalars.Height==BitConverter.UInt32BitsToSingle(0x3FB33333);}
                if(f.Failure){c.Consumer.Tick(0);family=c.World.Snapshots().Count==0&&c.World.DrainChanges(c.Generation) is null&&c.Consumer.Count==0&&!c.World.IsCurrent(heldHuman!);}
                baselineFrames.Add(new {f.Name,AcceptedMessages=accepted,DroppedMessages=dropped,WorldState=c.World.State.ToString(),c.World.Generation,
                    Objects=c.World.Snapshots().Select(ObjectView).ToArray(),c.World.OutstandingJobs,Poll=PollView(poll)});
            }
        }
        bool outstanding=false;
        using(var closing=New())
        {
            closing.World.PollObjectResources();closing.World.Close();
            outstanding=closing.Jobs.Count==2&&closing.Jobs.All(j=>j.Token.IsCancellationRequested)&&!closing.World.DrainCompletion.IsCompleted;
            closing.Complete(0);closing.Jobs[1].Task.SetException(new Exception("Synthetic late close fault"));closing.World.DrainCompletion.GetAwaiter().GetResult();
            outstanding&=closing.World.OutstandingJobs==0;
        }
        Check(delayed&&recreated&&retired&&family&&outstanding,"Baseline lifecycle and cleanup checks");
        var pumpFrames=new List<object>();object? pumpChecks=null;
        if(pumpFactory is not null)
        {
            bool latest=false,preserved=false,noPrefix=false;
            // Pump creates/owns its consumer. No baseline consumer is delegated or attached here.
            using(var c=New())using(var pump=pumpFactory(c.World,c.AdapterFactory,peer.Policy))
            {
                ScenePreparedObjectPublication? held=null;
                foreach(var f in peer.Frames)
                {
                    c.Allow=f.AllowAdapters;foreach(int i in f.Complete)c.Complete(i);
                    foreach(string key in f.Queue){byte[] bytes=c.Bytes(key);pump.QueueApplication(key=="stale"?c.Generation-1:c.Generation,bytes);Array.Clear(bytes);}
                    SceneWorldPresentationPumpStep? step=null;bool threw=false;
                    try{step=pump.Step(1.0/60);}catch(InvalidDataException){threw=true;}
                    Check(!threw||f.Failure,"Only declared pump failure may throw");
                    if(step is not null)Check(step.AcceptedMessages>=0&&step.DroppedMessages>=0&&step.AcceptedMessages+step.DroppedMessages<=2&&step.PublishedResources is >=0 and <=1,"Per-step ingress/publication bounds");
                    Check(pump.QueuedMessageCount is >=0 and <=6&&pump.QueuedMessageBytes is >=0 and <=1024&&pump.PendingPublicationCount is >=0 and <=2&&pump.PendingResourceWeight is >=0 and <=67108864,"Observable queue/resource bounds");
                    if(f.Name=="held-publication")Check(pump.PendingPublicationCount>0&&pump.Consumer.Count==0,"Deferred publication stays pending");
                    if(f.Name=="settle")
                    {var adapter=(Fake?)pump.Consumer.Find(human);latest=adapter?.Last?.Replication?.Scalars.Height==BitConverter.UInt32BitsToSingle(0x3FB33333);
                        held=adapter?.Publication;preserved=held is not null&&c.World.IsCurrent(held)&&held.Ticket.Attempt==1&&c.Jobs.Count==3&&pump.Consumer.Count==2;}
                    if(f.Failure)noPrefix=c.World.Snapshots().Count==0&&pump.Consumer.Count==0&&pump.QueuedMessageCount==0&&pump.PendingPublicationCount==0&&pump.PendingResourceWeight==0&&held is not null&&!c.World.IsCurrent(held);
                    pumpFrames.Add(new {f.Name,StepReturned=step is not null,AcceptedMessages=step?.AcceptedMessages,DroppedMessages=step?.DroppedMessages,
                        PublishedResources=step?.PublishedResources,PendingMessages=pump.QueuedMessageCount,QueuedBytes=pump.QueuedMessageBytes,
                        PendingPublications=pump.PendingPublicationCount,PendingWeight=pump.PendingResourceWeight});
                }
                Check(latest&&preserved&&noPrefix,"Injected lifecycle obligations");pump.Close();pump.DrainCompletion.GetAwaiter().GetResult();
            }
            bool countReject=false,bytesReject=false,sizeReject=false,callerUnchanged=true;
            for(int mode=0;mode<3;mode++)using(var c=New())
            {
                var policy=mode==1?peer.Policy with{MaximumQueuedMessages=16,MaximumQueuedBytes=64,MaximumMessageBytes=64}:peer.Policy;
                using var pump=pumpFactory(c.World,c.AdapterFactory,policy);byte[] caller=mode==2?new byte[257]:c.Bytes("heightA");byte[] copy=caller.ToArray();
                int fill=mode==0?6:mode==1?2:0;for(int i=0;i<fill;i++)pump.QueueApplication(c.Generation,c.Bytes("heightA"));
                int beforeCount=pump.QueuedMessageCount;long beforeBytes=pump.QueuedMessageBytes;string beforeWorld=JsonSerializer.Serialize(c.World.Snapshots().Select(ObjectView));
                bool reject=false;try{pump.QueueApplication(c.Generation,caller);}catch(Exception e)when(e is InvalidDataException or ArgumentException){reject=true;}
                bool unchanged=caller.SequenceEqual(copy)&&pump.QueuedMessageCount==beforeCount&&pump.QueuedMessageBytes==beforeBytes&&beforeWorld==JsonSerializer.Serialize(c.World.Snapshots().Select(ObjectView));
                callerUnchanged&=unchanged;Check(reject&&unchanged,"Queue budget rejection atomically preserves caller/queue/world");
                if(mode==0)countReject=true;else if(mode==1)bytesReject=true;else sizeReject=true;
            }
            bool burst=false;
            using(var c=New())using(var pump=pumpFactory(c.World,c.AdapterFactory,peer.Policy with{MaximumPendingPublications=1}))
            {
                c.Allow=true;pump.Step(0);Check(c.Jobs.Count==2,"Initial pump jobs");c.Complete(0);c.Complete(1);
                bool rejected=false;try{var result=pump.Step(0);rejected=!result.Active;}catch(InvalidDataException){rejected=true;}
                burst=rejected&&c.Created.Count==0&&pump.Consumer.Count==0&&pump.PendingPublicationCount==0&&pump.PendingResourceWeight==0;
                Check(burst,"No externally visible prefix on pending-publication burst budget failure");
            }
            bool staleHeld=false;
            using(var c=New())using(var pump=pumpFactory(c.World,c.AdapterFactory,peer.Policy))
            {
                pump.Step(0);c.Complete(0);c.Complete(1);pump.Step(0);Check(pump.PendingPublicationCount==2,"Both deferred publications held");
                foreach(string key in new[]{"terminalDestroy","terminalCreate"})pump.QueueApplication(c.Generation,c.Bytes(key));pump.Step(0);
                Check(pump.PendingPublicationCount<=1&&pump.Consumer.Count==0,"Stale held terminal retires before replacement completion");
                foreach(string key in new[]{"terminalBaseline","terminalEnd"})pump.QueueApplication(c.Generation,c.Bytes(key));pump.Step(0);
                Check(c.Jobs.Count==3,"Only revised terminal starts");c.Complete(2);c.Allow=true;pump.Step(0);pump.Step(0);
                ulong terminal=SceneObjectLifecycleCodec.DecodeCreate(c.Bytes("terminalCreate")).ObjectId;
                staleHeld=pump.Consumer.Count==2&&c.Created.Where(f=>f.Publication.Ticket.Handle.ObjectId==terminal).All(f=>f.Publication.Ticket.Attempt==3);
                Check(staleHeld,"Held old terminal never attaches after recreate");
            }
            bool drain=false;
            using(var c=New())using(var pump=pumpFactory(c.World,c.AdapterFactory,peer.Policy))
            {pump.Step(0);pump.Close();drain=c.Jobs.Count==2&&c.Jobs.All(j=>j.Token.IsCancellationRequested)&&!pump.DrainCompletion.IsCompleted;
                c.Complete(0);c.Jobs[1].Task.SetException(new Exception("Synthetic late pump close fault"));pump.DrainCompletion.GetAwaiter().GetResult();drain&=c.World.OutstandingJobs==0&&pump.Consumer.Count==0;Check(drain,"Pump owns close and observes outstanding tasks");}
            pumpChecks=new {QueueCountReject=countReject,QueueBytesReject=bytesReject,MessageSizeReject=sizeReject,RejectedCallerUnchanged=callerUnchanged,
                HeldLatestRetry=latest,HumanPreserved=preserved,FailureNoPrefix=noPrefix,PublicationBurstNoPrefix=burst,StaleHeldPublicationRetired=staleHeld,OutstandingCloseDrained=drain};
        }
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(exportPath))!);
        File.WriteAllBytes(exportPath,JsonSerializer.SerializeToUtf8Bytes(new {BaselineFrames=baselineFrames,WireIdentities=peer.Applications,
            BaselineChecks=new {DelayedLatestSnapshot=delayed,TerminalOnlyRecreated=recreated,StaleCompletionRetired=retired,FamilyFailureCleared=family,
                ShutdownOutstandingObserved=outstanding,ClosedDrained=archiveScopes==0},OriginalJobs=originalJobs,ArchiveScopes=archiveScopes,
            ActualPumpRan=pumpFactory is not null,PumpFrames=pumpFrames,PumpChecks=pumpChecks},new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine("PASS exact baseline eight-frame peer and cleanup; injected actual pump "+(pumpFactory is null?"NOT RUN":"passed"));
    }
}
