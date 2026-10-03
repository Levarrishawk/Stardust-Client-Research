using System.Text;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

try
{
    if (args.Length != 6) throw new ArgumentException("root cfg catalog bank peer export");
    var catalog = ObjectTemplateCatalogProfile.Decode(File.ReadAllBytes(args[2]));
    using var bankFile = File.OpenRead(args[3]);
    var bank = SkeletalAnimationBankProfile.Load(bankFile);
    var idle = new LogicalAnimationClipSelection("appearance/lat/all_m.lat", "emt_standing_idle_ambient_1_ag",
        "798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B");
    var options = new Dictionary<SceneObjectResourceRecipe, SceneObjectResourceOptions>
    {
        [SceneObjectResourceRecipe.CreatureDetail0] = new SceneCreatureResourceOptions(idle, bank, true),
        [SceneObjectResourceRecipe.StaticAppearance] = new SceneStaticResourceOptions(SceneStaticMaterialPolicy.RequireComplete,
            new("shader/anim_mission_terminal.sht", "B5AA5A251A76AD8F152DB85F50FBC3061D0494847499F1068A9B55C375785A62", 0))
    };
    var peer = JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllBytes(args[4]))!;
    byte[] cfg = File.ReadAllBytes(args[1]);
    Check(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(cfg)) == RetainedSkeletalDetailBundleReader.ConfigurationPin, "configuration identity");
    ulong A = SceneObjectLifecycleCodec.DecodeCreate(Convert.FromHexString(peer["human_create"])).ObjectId, B = SceneObjectLifecycleCodec.DecodeCreate(Convert.FromHexString(peer["terminal_create"])).ObjectId;
    int owner = Environment.CurrentManagedThreadId, calls = 0, closed = 0;
    var jobs = new List<Task<SceneObjectResources>>();
    var loads = new List<object>();
    Task<SceneObjectResources> Load(SceneObjectResourceRequest request, CancellationToken token)
    {
        calls++;
        var job = Task.Run(() =>
        {
            byte[] copy = cfg.ToArray();
            try
            {
                using var text = new StringReader(Encoding.UTF8.GetString(copy));
                using var overlay = ConfiguredTreOverlay.Open(args[0], text);
                var reads = new HashSet<string>();
                TreLookupResult Read(string path) { Check(reads.Add(path), "one lookup per source"); return overlay.Lookup(path); }
                var result = SceneObjectResourceRouter.Load(request.Snapshot.Create, request.Catalog, request.Options, Read, token);
                Check(Environment.CurrentManagedThreadId != owner, "dedicated resource worker");
                Check(reads.Count == result.ExternalReads, "source accounting");
                lock(loads) loads.Add(new { Id=result.Create.ObjectId, Reads=result.ExternalReads, Bytes=result.SourceBytes,
                    CreateHex=Convert.ToHexString(SceneObjectLifecycleCodec.EncodeCreate(result.Create)),
                    Sources=result.Sources.OrderBy(p=>p.Key,StringComparer.Ordinal).Select(p=>p.Value).ToArray() });
                return result;
            }
            finally { Array.Clear(copy); Interlocked.Increment(ref closed); }
        }, token);
        jobs.Add(job); return job;
    }
    var policy = new SceneWorldReplicationPolicy(SceneWorldReplicationMode.CreatureView3Scalars,
        new CreatureView3BaselineLimits(1024,1024,1024,128,16384), 16, 16384);
    using var world = new SceneWorldCoordinator(catalog, options, Load, SceneCreateZeroQuaternionPolicy.Reject,
        maximumObjects:2, maximumOutstandingJobs:2, maximumStagedMessages:8, maximumPayloadBytes:1024,
        maximumEnvelopes:4, maximumEnvelopeBytes:1024, replicationPolicy:policy);
    var adapters=new List<ProbeAdapter>();
    using var consumer=new SceneWorldObjectPresentationConsumer(world,(publication,snapshot)=> { var adapter=new ProbeAdapter(publication);adapters.Add(adapter);return adapter; },maximumActors:2);
    long generation=world.BeginSelection(A).Generation;
    void Accept(string key) { byte[] bytes=Convert.FromHexString(peer[key]); try { Check(world.Accept(generation,bytes), "current message"); } finally { Array.Clear(bytes); } }
    Accept("start");
    foreach(string p in new[]{"human","terminal"}) foreach(string suffix in new[]{"create","baseline","end"}) Accept(p+"_"+suffix);
    Check(world.Snapshots().Count==0 && calls==0, "staged world has no publication");
    Check(world.CompleteMinimumSceneBarrier(generation,true) is not null, "atomic explicit barrier");
    Check(world.PollObjectResources().Prepared.Count==0 && calls==2, "two recipe jobs start");
    Task.WhenAll(jobs).GetAwaiter().GetResult();
    var pubs=world.PollObjectResources().Prepared.ToDictionary(p=>p.Ticket.Handle.ObjectId);
    Check(pubs.Count==2 && closed==2 && pubs.Values.All(world.IsCurrent), "two current archive-closed publications");
    Accept("ready");
    foreach(var publication in pubs.Values)Check(consumer.AcceptResource(publication),"generic adapter acceptance");
    Check(consumer.Count==2 && consumer.AcceptResource(pubs[A]) && adapters.Count==2,"bounded idempotent generic acceptance");
    var states=new List<object>();
    object State(string stage) => new { Stage=stage, Objects=world.Snapshots().Select(s=>new {
        Id=s.Object.Handle.ObjectId, Kind=catalog.Require(s.Object.Create.TemplateCrc).Recipe.ToString(),
        Creation=s.Object.Incarnation.CreationRevision, Typed=s.Replication is not null,
        HeightBits=s.Replication is null?0U:BitConverter.SingleToUInt32Bits(s.Replication.Scalars.Height),
        OpaqueCount=s.Envelopes.Count }).ToArray() };
    states.Add(State("initial"));
    var held=world.DrainChanges(generation)!;Check(consumer.ApplyBatch(held),"initial batch applied");
    Accept("human_height"); Accept("terminal_delta");
    Check(pubs.Values.All(world.IsCurrent) && calls==2 && !consumer.AcknowledgeAppliedBatch(held), "state preserves resources and stale ack");
    var batch=world.DrainChanges(generation)!; Check(batch.Entries.Count==2 && consumer.ConsumePending(), "both latest observations acknowledge");
    Check(((ProbeAdapter)consumer.Find(A)!).HeightBits==BitConverter.SingleToUInt32Bits(1.4f),"latest typed state applied without reloading");
    states.Add(State("updated"));
    var oldTerminal=world.Snapshots().Single(s=>s.Object.Handle.ObjectId==B).Object.Incarnation;
    var human=world.Snapshots().Single(s=>s.Object.Handle.ObjectId==A).Object.Incarnation;
    Accept("terminal_destroy");Check(consumer.Tick(0) && consumer.Count==1 && adapters.Single(a=>a.Publication==pubs[B]).Disposals==1,"only static presentation retired");
    Check(!world.IsCurrent(pubs[B]) && world.IsCurrent(pubs[A]) && world.Snapshots().Count==1, "only terminal retired");
    Accept("terminal_create"); Accept("terminal_end");
    world.PollObjectResources(); Check(calls==3, "one static reload");
    jobs[^1].GetAwaiter().GetResult(); var replacement=world.PollObjectResources().Prepared.Single();
    Check(replacement.Ticket.Handle.ObjectId==B && world.IsCurrent(replacement) && !world.IsCurrent(pubs[B]) && world.IsCurrent(pubs[A]), "replacement freshness");
    Check(world.Snapshots().Single(s=>s.Object.Handle.ObjectId==A).Object.Incarnation==human &&
        world.Snapshots().Single(s=>s.Object.Handle.ObjectId==B).Object.Incarnation!=oldTerminal, "incarnation separation");
    Check(!consumer.AcceptResource(pubs[B]) && consumer.AcceptResource(replacement) && consumer.Count==2 && adapters.Count==3 && consumer.ConsumePending(),"new static adapter and latest acknowledgment");
    states.Add(State("recreated"));
    bool failed=false; try { Accept("late_bad"); } catch(InvalidDataException) { failed=true; }
    Check(failed && world.State==ZoneSceneTransitionState.Failed && world.Snapshots().Count==0 &&
        !world.IsCurrent(pubs[A]) && !world.IsCurrent(replacement) && world.DrainChanges(generation) is null, "late failure retires whole world");
    Check(!consumer.Tick(0) && consumer.Count==0 && adapters.All(a=>a.Disposals==1),"failure retires every adapter exactly once");
    consumer.Dispose();
    world.Close(); world.DrainCompletion.GetAwaiter().GetResult();
    Check(world.OutstandingJobs==0 && closed==calls, "all jobs drain");
    Array.Clear(cfg);
    File.WriteAllText(args[5],JsonSerializer.Serialize(new { Loads=loads, States=states, Adapters=adapters.Select(a=>new{Id=a.Publication.Ticket.Handle.ObjectId,Attempt=a.Publication.Ticket.Attempt,a.HeightBits,a.Applications,a.Disposals}), Flags=new {
        GenericConsumer=true,DynamicIds=true,ExactlyOnceAdapterDisposal=true,TwoRecipes=true, StagedAtomicBarrier=true, ArchiveClosedBeforeTransfer=true, WorkerOffOwner=true,
        OpaqueTerminal=true, TypedCreature=true, NoStateReload=true, StaleAckRetried=true,
        RecreatedOnlyTerminal=true, UnchangedHuman=true, LateFailureRetired=true, Drained=true } }));
    Console.WriteLine("PASS: original human/terminal mixed world; typed Height and opaque TANO; exact publication freshness, one static reload, atomic failure/drain");
}
catch(Exception error) { Console.Error.WriteLine(error); Environment.ExitCode=1; }
static void Check(bool condition,string message) { if(!condition) throw new InvalidDataException(message); }

sealed class ProbeAdapter(ScenePreparedObjectPublication publication):ISceneWorldObjectPresentation
{
    public ScenePreparedObjectPublication Publication{get;}=publication;
    public uint HeightBits{get;private set;}
    public int Applications{get;private set;}
    public int Disposals{get;private set;}
    public bool Apply(SceneWorldObjectSnapshot snapshot)
    {
        if(Disposals!=0 || snapshot.Object.Handle!=Publication.Ticket.Handle)return false;
        HeightBits=snapshot.Replication is null?0:BitConverter.SingleToUInt32Bits(snapshot.Replication.Scalars.Height);Applications++;return true;
    }
    public bool Tick(double seconds)=>Disposals==0;
    public void Dispose(){if(Disposals==0)Disposals++;}
}
