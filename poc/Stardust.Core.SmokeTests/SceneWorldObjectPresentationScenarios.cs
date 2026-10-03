using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

internal static class SceneWorldObjectPresentationScenarios
{
    private static int checks;
    private static void Check(bool value,string why){Interlocked.Increment(ref checks);if(!value)throw new Exception(why);}
    private static void Reject<T>(Action action) where T:Exception {try{action();}catch(T){Interlocked.Increment(ref checks);return;}throw new Exception("Expected "+typeof(T).Name);}
    private static string Hash(ReadOnlySpan<byte> bytes)=>Convert.ToHexString(SHA256.HashData(bytes));
    private static string Fingerprint(SceneObjectResources r)=>Hash(JsonSerializer.SerializeToUtf8Bytes(r.Creature is {} c?(object)c:r.Static!));
    private sealed class Fake : ISceneWorldObjectPresentation
    {
        public ScenePreparedObjectPublication Publication {get;}
        public int Applies,Ticks,Disposals; public SceneWorldObjectSnapshot? Last;
        public Func<bool>? OnApply,OnTick; public Action? OnDispose = null;
        public Fake(ScenePreparedObjectPublication publication){Publication=publication;}
        public bool Apply(SceneWorldObjectSnapshot snapshot){Applies++;Last=snapshot;return OnApply?.Invoke()??true;}
        public bool Tick(double seconds){Ticks++;return OnTick?.Invoke()??true;}
        public void Dispose(){Disposals++;Last=null;OnDispose?.Invoke();}
    }
    internal static void Run(string[] inputs,IReadOnlyDictionary<string,string> peer,string exportPath)
    {
        Volatile.Write(ref checks,0);
        Check(inputs.Length==4,"root config catalog bank");Check(Hash(File.ReadAllBytes(inputs[1]))==RetainedSkeletalDetailBundleReader.ConfigurationPin,"Installed configuration pin");
        var catalog=ObjectTemplateCatalogProfile.Decode(File.ReadAllBytes(inputs[2]));using var bankFile=File.OpenRead(inputs[3]);var bank=SkeletalAnimationBankProfile.Load(bankFile);
        var idle=new LogicalAnimationClipSelection("appearance/lat/all_m.lat","emt_standing_idle_ambient_1_ag","798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B");
        var creatureOptions=new SceneCreatureResourceOptions(idle,bank,true);var frame=new SwitchTextureFrameSelection("shader/anim_mission_terminal.sht","B5AA5A251A76AD8F152DB85F50FBC3061D0494847499F1068A9B55C375785A62",0);
        var staticOptions=new SceneStaticResourceOptions(SceneStaticMaterialPolicy.RequireComplete,frame);
        var options=new Dictionary<SceneObjectResourceRecipe,SceneObjectResourceOptions>{{SceneObjectResourceRecipe.CreatureDetail0,creatureOptions},{SceneObjectResourceRecipe.StaticAppearance,staticOptions}};
        byte[] Bytes(string n)=>Convert.FromHexString(peer[n]);var human=SceneObjectLifecycleCodec.DecodeCreate(Bytes("create"));var terminal=SceneObjectLifecycleCodec.DecodeCreate(Bytes("staticCreate"));ulong humanId=human.ObjectId,staticId=terminal.ObjectId;
        int callerThread=Environment.CurrentManagedThreadId,archiveScopes=0;var receipts=new List<object>();var receiptLock=new object();
        SceneObjectResources Original(SceneCreateObjectByCrcMessage create,SceneObjectResourceOptions option,CancellationToken token,bool receipt)
        {
            int thread=Environment.CurrentManagedThreadId;Check(thread!=callerThread,"Original worker owns resolver");var borrowed=new List<byte[]>();var reads=new Dictionary<string,int>();SceneObjectResources loaded;
            Interlocked.Increment(ref archiveScopes);
            try{using(var config=File.OpenText(inputs[1]))using(var overlay=ConfiguredTreOverlay.Open(inputs[0],config))
            {
                TreLookupResult Read(string path){Check(Environment.CurrentManagedThreadId==thread,"Resolver thread affinity");reads[path]=reads.GetValueOrDefault(path)+1;var result=overlay.Lookup(path);if(result.Payload is not null)borrowed.Add(result.Payload);return result;}
                loaded=SceneObjectResourceRouter.Load(create,catalog,option,Read,token);string before=Fingerprint(loaded);foreach(var b in borrowed)Array.Clear(b);Check(Fingerprint(loaded)==before,"Original borrowed buffers cleared after owned parse");
            }}finally{Interlocked.Decrement(ref archiveScopes);}
            Check(reads.Values.All(n=>n==1)&&loaded.ExternalReads==reads.Count,"One owned source read");
            var images=loaded.Creature?.Creature.MaterialBundle!.Textures ?? loaded.Static!.Appearance.Meshes.Values.SelectMany(m=>m.Textures).GroupBy(p=>p.Key).ToDictionary(g=>g.Key,g=>g.First().Value);
            if(receipt)lock(receiptLock)receipts.Add(new {Kind=loaded.Kind.ToString(),Wire=Convert.ToHexString(SceneObjectLifecycleCodec.EncodeCreate(loaded.Create)),loaded.ExternalReads,loaded.SourceBytes,Sources=loaded.Sources.Values.OrderBy(p=>p.Path).ToArray(),Textures=images.OrderBy(p=>p.Key).Select(p=>new {Path=p.Key,Mips=p.Value.Mips.Select(m=>Hash(m.Rgba.Span)).ToArray()}).ToArray(),WorkerThread=thread,CallerThread=callerThread,ArchiveClosed=true,BorrowedBuffers=borrowed.Count});
            return loaded;
        }
        Task<SceneObjectResources> Fresh(SceneObjectResourceRequest q,CancellationToken t,bool receipt=true)=>Task.Factory.StartNew(()=>Original(q.Snapshot.Create,q.Options,t,receipt),CancellationToken.None,TaskCreationOptions.LongRunning,TaskScheduler.Default);
        var loaded=new Dictionary<SceneObjectResourceRecipe,SceneObjectResources>();
        foreach(var create in new[]{human,terminal}){var registry=new SceneObjectRegistry(1,1);var snapshot=registry.Create(1,create)!;loaded[catalog.Require(create.TemplateCrc).Recipe]=Fresh(new(snapshot,catalog,options[catalog.Require(create.TemplateCrc).Recipe]),CancellationToken.None,false).GetAwaiter().GetResult();}
        Check(archiveScopes==0&&loaded[SceneObjectResourceRecipe.CreatureDetail0].ExternalReads==39&&loaded[SceneObjectResourceRecipe.StaticAppearance].ExternalReads==27,"Fresh original preload closure");
        var policy=new SceneWorldReplicationPolicy(SceneWorldReplicationMode.CreatureView3MixedComponents,new(1024,64,64,16,512),16,1024,new(1024,32,24,64,64,16),CreatureWoundsApplicationPolicy.LegacyBounded,new(1024,32,64,64,16),new(16,16));
        SceneWorldCoordinator World(Func<SceneObjectResourceRequest,CancellationToken,Task<SceneObjectResources>>? loader=null)=>new(catalog,options,loader??((q,ct)=>Task.FromResult(loaded[q.Options.Recipe])),SceneCreateZeroQuaternionPolicy.Reject,4,2,32,8192,16,4096,1024,policy);
        long Stage(SceneWorldCoordinator w){long g=w.BeginSelection(humanId).Generation;foreach(var n in new[]{"start","create","staticCreate","baseline","staticBaseline","end","staticEnd"})w.Accept(g,Bytes(n));w.CompleteMinimumSceneBarrier(g,true);return g;}
        ScenePreparedObjectPublication[] Publications(SceneWorldCoordinator w){w.PollObjectResources();return w.PollObjectResources().Prepared.ToArray();}
        using(var w=World())
        {
            long g=Stage(w);var pubs=Publications(w);var fakes=new List<Fake>();using var consumer=new SceneWorldObjectPresentationConsumer(w,(p,s)=>{var f=new Fake(p);fakes.Add(f);return f;});
            Reject<ArgumentOutOfRangeException>(()=>new SceneWorldObjectPresentationConsumer(w,(p,s)=>null,0));Reject<ArgumentOutOfRangeException>(()=>consumer.ConsumePending(17));Reject<ArgumentOutOfRangeException>(()=>consumer.Tick(double.NaN));
            foreach(var p in pubs)Check(consumer.AcceptResource(p),"Accept both recipes");Check(consumer.Count==2&&fakes.Count==2,"Bounded heterogeneous dictionary");var humanActor=(Fake)consumer.Find(humanId)!;var staticActor=(Fake)consumer.Find(staticId)!;
            Check(staticActor.Last!.Replication is null&&humanActor.Last!.Replication is not null,"Static opaque / typed creature separate");Check(consumer.AcceptResource(pubs[0])&&fakes.Count==2,"Exact publication idempotent");
            var batch=w.DrainChanges(g)!;Check(consumer.ApplyBatch(batch),"Apply initial batch");var sameRevision=w.DrainChanges(g)!;Check(!consumer.AcknowledgeAppliedBatch(sameRevision),"Exact batch reference required");
            using(var other=World()){long og=Stage(other);Check(!consumer.AcknowledgeAppliedBatch(other.DrainChanges(og)!),"Foreign batch");Check(!consumer.AcceptResource(Publications(other)[0]),"Foreign publication");}
            w.Accept(g,Bytes("composite"));Check(!consumer.AcknowledgeAppliedBatch(batch)&&w.DrainChanges(g)!.Entries.Count==2,"Stale acknowledgment preserves newer pending IDs");Check(consumer.ConsumePending(),"Retry latest batch");Check(humanActor.Last!.Replication!.Revision==2&&fakes.Count==2,"State-only keeps adapter/resources");
            w.Accept(g,Bytes("staticDelta"));Check(consumer.ConsumePending()&&staticActor.Last!.Replication is null&&staticActor.Last.Envelopes.Count==2,"Opaque static apply no invented semantics");Check(!consumer.ConsumePending(),"Empty pending false");
            Exception? threadError=null;var thread=new Thread(()=>{try{_=consumer.Count;}catch(Exception e){threadError=e;}});thread.Start();thread.Join();Check(threadError is InvalidOperationException,"Owner thread");
            humanActor.OnApply=()=>{Reject<InvalidOperationException>(()=>consumer.Find(humanId));return true;};w.Accept(g,Bytes("composite"));Check(consumer.ConsumePending(),"Adapter callback reentrancy rejected");humanActor.OnApply=null;
            w.RestartResource(g,staticId);Check(consumer.Count==1&&staticActor.Disposals==1&&!consumer.AcceptResource(pubs.Single(p=>p.Ticket.Handle.ObjectId==staticId)),"Restart retires exact static only");var replacement=Publications(w).Single();Check(consumer.AcceptResource(replacement)&&consumer.Find(humanId)==humanActor,"Replacement preserves human");
            var recreated=(Fake)consumer.Find(staticId)!;w.Accept(g,Bytes("staticDestroy"));w.Accept(g,Bytes("staticCreate"));w.Accept(g,Bytes("staticEnd"));Check(consumer.ConsumePending()&&recreated.Disposals==1&&consumer.Count==1,"Recreation retires prior incarnation before acknowledgment");var recreatePub=Publications(w).Single();Check(consumer.AcceptResource(recreatePub),"New incarnation accepts");
            var tickActor=(Fake)consumer.Find(staticId)!;tickActor.OnTick=()=>false;Check(consumer.Tick(0)&&tickActor.Disposals==1&&consumer.Count==1,"False Tick retires roots");
            humanActor.OnTick=()=>throw new ApplicationException("adapter tick");Reject<ApplicationException>(()=>consumer.Tick(0));Check(humanActor.Disposals==1&&consumer.Count==0,"Arbitrary Tick errors propagate with retirement");
            consumer.RetireAll();consumer.Dispose();consumer.Dispose();Check(consumer.Count==0&&!consumer.Tick(0)&&!consumer.AcceptResource(recreatePub),"Disposed no roots");
        }
        using(var w=World())
        {
            Stage(w);var pubs=Publications(w);int factories=0;using var c=new SceneWorldObjectPresentationConsumer(w,(p,s)=>{factories++;return new Fake(p);},1);Check(c.AcceptResource(pubs[0]),"Capacity first");Reject<InvalidDataException>(()=>c.AcceptResource(pubs[1]));Check(factories==1&&c.Count==1,"Capacity throws before factory");
        }
        foreach(var mode in new[]{"null","false","wrong","stale","throw","generation"})
        using(var w=World())
        {
            long g=Stage(w);var pubs=Publications(w);Fake? candidate=null;SceneWorldObjectPresentationConsumer? c=null;
            c=new(w,(p,s)=>{Reject<InvalidOperationException>(()=>c!.Tick(0));if(mode=="null")return null;candidate=new(mode=="wrong"?pubs[1]:p);candidate.OnApply=()=>{if(mode=="throw")throw new ApplicationException("adapter apply");if(mode=="stale")w.Accept(g,Bytes("composite"));if(mode=="generation")w.BeginSelection(humanId);return mode!="false";};return candidate;});using(c)
            {
                if(mode=="wrong")Reject<InvalidDataException>(()=>c.AcceptResource(pubs[0]));else if(mode=="throw")Reject<ApplicationException>(()=>c.AcceptResource(pubs[0]));else Check(!c.AcceptResource(pubs[0]),"Candidate rejects "+mode);
                Check(c.Count==0&&(candidate is null||candidate.Disposals==1),"Candidate rollback "+mode);
            }
        }
        using(var w=World())
        {
            long g=Stage(w);var pubs=Publications(w);Fake? actor=null;using var c=new SceneWorldObjectPresentationConsumer(w,(p,s)=>actor=new Fake(p));c.AcceptResource(pubs[0]);actor!.OnApply=()=>{w.Accept(g,Bytes("staticDelta"));return true;};var b=w.DrainChanges(g)!;Check(!c.ApplyBatch(b)&&!c.AcknowledgeAppliedBatch(b)&&w.DrainChanges(g)!.Entries.Count==2,"Callback invalidates batch; newer work pending");actor.OnApply=()=>false;Check(!c.ConsumePending(1)&&c.Count==1,"False Apply retains current actor and pending state");actor.OnApply=()=>throw new ArgumentOutOfRangeException("arbitrary adapter");Reject<ArgumentOutOfRangeException>(()=>c.ApplyBatch(w.DrainChanges(g)!));Check(actor.Disposals==1&&c.Count==0,"Arbitrary Apply error not swallowed as Height");
        }
        using(var w=World())
        {
            long g=Stage(w);var pubs=Publications(w);Fake? old=null;Fake? newer=null;using var c=new SceneWorldObjectPresentationConsumer(w,(p,s)=>{if(old is null)return old=new Fake(p);Check(old.Disposals==0,"Replacement factory runs before owned old retirement");newer=new Fake(p);newer.OnApply=()=>{Check(old.Disposals==0,"Replacement prepared before old disposal");return true;};return newer;});var pub=pubs.Single(p=>p.Ticket.Handle.ObjectId==staticId);c.AcceptResource(pub);w.RestartResource(g,staticId);var replacement=Publications(w).Single();Check(c.AcceptResource(replacement)&&old!.Disposals==1&&c.Find(staticId)==newer,"Candidate then old retirement and install");
        }
        using(var w=World())
        {
            long g=Stage(w);var pubs=Publications(w);Fake? candidate=null;using var c=new SceneWorldObjectPresentationConsumer(w,(p,s)=>{candidate=new Fake(p);candidate.OnApply=()=>{w.Accept(g,Bytes("staticDelta"));return true;};return candidate;});Check(!c.AcceptResource(pubs.Single(p=>p.Ticket.Handle.ObjectId==staticId))&&candidate!.Disposals==1&&c.Count==0,"Opaque history changes during candidate callback roll back without interpreting bytes");
        }
        foreach(var mode in new[]{"late-false","late-throw","late-stale"})
        using(var w=World())
        {
            long g=Stage(w);var pubs=Publications(w);using var c=new SceneWorldObjectPresentationConsumer(w,(p,s)=>new Fake(p));foreach(var p in pubs)c.AcceptResource(p);
            var first=(Fake)c.Find(humanId)!;var later=(Fake)c.Find(staticId)!;int before=first.Applies;
            later.OnApply=()=>{if(mode=="late-throw")throw new ApplicationException("second adapter");if(mode=="late-stale")w.Accept(g,Bytes("staticDelta"));return mode!="late-false";};var batch=w.DrainChanges(g)!;
            if(mode=="late-throw")Reject<ApplicationException>(()=>c.ApplyBatch(batch));else Check(!c.ApplyBatch(batch),"Later callback invalidates application");
            Check(first.Applies==before+1&&!c.AcknowledgeAppliedBatch(batch)&&w.DrainChanges(g)!.Entries.Count==2,"Earlier apply retained; no ack/pending preserved "+mode);
            later.OnApply=null;Check(c.ConsumePending(),"Retry latest after partial adapter apply "+mode);
        }
        using(var w=World())
        {
            Stage(w);var pubs=Publications(w);Fake? first=null;using var c=new SceneWorldObjectPresentationConsumer(w,(p,s)=>first??=new Fake(p));c.AcceptResource(pubs[0]);Reject<InvalidDataException>(()=>c.AcceptResource(pubs[1]));Check(c.Count==1&&first!.Disposals==0,"Factory cannot recycle an owned adapter");
            first!.OnDispose=()=>{Reject<InvalidOperationException>(()=>c.RetireAll());throw new ApplicationException("dispose first");};Reject<AggregateException>(()=>c.RetireAll());Check(c.Count==0&&first.Disposals==1,"Throwing disposal unroots ownership exactly once");
        }
        using(var w=World())
        {
            Stage(w);var pubs=Publications(w);var list=new List<Fake>();using var c=new SceneWorldObjectPresentationConsumer(w,(p,s)=>{var f=new Fake(p);list.Add(f);return f;});foreach(var p in pubs)c.AcceptResource(p);
            list[0].OnDispose=()=>throw new ApplicationException("dispose failure");Reject<AggregateException>(()=>c.Dispose());Check(list.All(f=>f.Disposals==1)&&c.Count==0,"Dispose continues across adapters and releases closures");c.Dispose();
        }
        var pending=new List<TaskCompletionSource<SceneObjectResources>>();var requests=new List<SceneObjectResourceRequest>();var tokens=new List<CancellationToken>();
        Task<SceneObjectResources> Delayed(SceneObjectResourceRequest q,CancellationToken t){requests.Add(q);tokens.Add(t);var job=new TaskCompletionSource<SceneObjectResources>();pending.Add(job);return job.Task;}
        using(var w=World(Delayed))
        {
            long g=Stage(w);using var c=new SceneWorldObjectPresentationConsumer(w,(p,s)=>new Fake(p));w.PollObjectResources();Check(c.ConsumePending()&&c.Count==0,"Missing actors acknowledged");w.Accept(g,Bytes("composite"));Check(c.ConsumePending(),"New state acknowledged before delayed load");for(int i=0;i<2;i++)pending[i].SetResult(loaded[requests[i].Options.Recipe]);foreach(var p in w.PollObjectResources().Prepared)c.AcceptResource(p);Check(((Fake)c.Find(humanId)!).Last!.Replication!.Revision==2,"Delayed acceptance latest state");w.Close();Check(c.Count==0&&w.DrainCompletion.IsCompleted,"Close retires roots");
        }
        pending.Clear();requests.Clear();tokens.Clear();
        using(var w=World(Delayed))
        {
            long g=Stage(w);using var c=new SceneWorldObjectPresentationConsumer(w,(p,s)=>new Fake(p));w.PollObjectResources();int humanJob=requests.FindIndex(q=>q.Options.Recipe==SceneObjectResourceRecipe.CreatureDetail0),staticJob=1-humanJob;
            pending[humanJob].SetResult(loaded[SceneObjectResourceRecipe.CreatureDetail0]);var humanPub=w.PollObjectResources().Prepared.Single();c.AcceptResource(humanPub);var humanActor=(Fake)c.Find(humanId)!;
            w.RestartResource(g,staticId);pending[staticJob].SetException(new Exception("synthetic superseded static fault"));Check(w.PollObjectResources().Retired.Count==1&&c.Count==1,"Late stale job does not retire current presentation");
            pending[2].SetResult(loaded[SceneObjectResourceRecipe.StaticAppearance]);var staticPub=w.PollObjectResources().Prepared.Single();Check(c.AcceptResource(staticPub)&&c.Find(humanId)==humanActor,"Delayed replacement accepts without affecting human");
            w.RestartResource(g,staticId);w.PollObjectResources();pending[3].SetException(new Exception("synthetic current resource fault"));Reject<InvalidDataException>(()=>w.PollObjectResources());Check(!c.Tick(0)&&c.Count==0&&humanActor.Disposals==1,"Current resource fault globally retires adapters");
        }
        pending.Clear();requests.Clear();tokens.Clear();
        using(var w=World(Delayed))
        {
            Stage(w);using var c=new SceneWorldObjectPresentationConsumer(w,(p,s)=>new Fake(p));w.PollObjectResources();w.Close();Check(!c.Tick(0)&&tokens.All(t=>t.IsCancellationRequested),"Close cancellation");foreach(var t in pending)t.SetException(new Exception("synthetic late fault"));w.DrainCompletion.GetAwaiter().GetResult();Check(w.OutstandingJobs==0&&c.Count==0,"Late errors observed/drain");
        }
        using(var w=World())
        {
            long g=Stage(w);var pubs=Publications(w);var list=new List<Fake>();using var c=new SceneWorldObjectPresentationConsumer(w,(p,s)=>{var f=new Fake(p);list.Add(f);return f;});foreach(var p in pubs)c.AcceptResource(p);Reject<InvalidDataException>(()=>w.Accept(g,Bytes("baseline")[..^1]));Check(!c.Tick(0)&&c.Count==0&&list.All(f=>f.Disposals==1),"Global failure retirement");
        }
        File.WriteAllText(exportPath,JsonSerializer.Serialize(new{Checks=checks,Recipes=2,OriginalSources=66,ArchiveScopes=archiveScopes,Result="PASS"},new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine("R15_PRESENTATION_PASS checks="+checks);
    }
}
