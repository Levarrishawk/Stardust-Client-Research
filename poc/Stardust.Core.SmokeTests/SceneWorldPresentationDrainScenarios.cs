using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

internal static class SceneWorldPresentationDrainScenarios
{
    private static int checks;
    private static void Check(bool value,string why){Interlocked.Increment(ref checks);if(!value)throw new Exception(why);}
    private static void Reject<T>(Action action) where T:Exception {try{action();}catch(T){Interlocked.Increment(ref checks);return;}throw new Exception("Expected "+typeof(T).Name);}
    private static string Hash(ReadOnlySpan<byte> bytes)=>Convert.ToHexString(SHA256.HashData(bytes));
    private static string Fingerprint(SceneObjectResources r)=>Hash(JsonSerializer.SerializeToUtf8Bytes(r.Creature is {} c?(object)c:r.Static!));
    private sealed class Fake : ISceneWorldObjectPresentation, ISceneWorldObjectPresentationDrain
    {
        public ScenePreparedObjectPublication Publication {get;}
        public readonly TaskCompletionSource Completion=new();
        public int Disposals,Getters;
        public Func<bool>? OnApply,OnTick;
        public Action? OnDispose;
        public Func<Task>? OnDrain;
        public Fake(ScenePreparedObjectPublication publication){Publication=publication;}
        public bool Apply(SceneWorldObjectSnapshot snapshot)=>OnApply?.Invoke()??true;
        public bool Tick(double seconds)=>OnTick?.Invoke()??true;
        public void Dispose(){Disposals++;OnDispose?.Invoke();}
        public Task DrainCompletion {get {Getters++;Check(Disposals==1,"Drain getter follows disposal exactly once");return OnDrain is {} getter?getter():Completion.Task;}}
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

        var cases=new List<string>();
        foreach(string mode in new[]{"retire","dispose","tick-false","tick-throw","apply-throw","destroy-batch","reset","close-boundary"})
        using(var w=World())
        {
            long g=Stage(w);var pubs=Publications(w);Fake? adapter=null;using var c=new SceneWorldObjectPresentationConsumer(w,(p,s)=>adapter=new(p));c.AcceptResource(pubs[0]);
            if(mode=="retire")c.RetireAll();
            if(mode=="dispose")c.Dispose();
            if(mode=="tick-false"){adapter!.OnTick=()=>false;c.Tick(0);}
            if(mode=="tick-throw"){adapter!.OnTick=()=>throw new ApplicationException("tick");Reject<ApplicationException>(()=>c.Tick(0));}
            if(mode=="apply-throw"){adapter!.OnApply=()=>throw new ApplicationException("apply");Reject<ApplicationException>(()=>c.ApplyBatch(w.DrainChanges(g)!));}
            if(mode=="destroy-batch"){w.Accept(g,Bytes(pubs[0].Ticket.Handle.ObjectId==humanId?"destroy":"staticDestroy"));c.ApplyBatch(w.DrainChanges(g)!);}
            if(mode=="reset"){w.BeginSelection(humanId);_=c.Count;}
            if(mode=="close-boundary"){w.Close();c.Tick(0);}
            Check(adapter!.Disposals==1&&adapter.Getters==1&&c.Count==0&&c.DrainingPresentationCount==1,"Every retirement captures pending task "+mode);
            var drain=c.DrainCompletion;Check(!drain.IsCompleted,"Pending retired worker "+mode);adapter.Completion.SetResult();drain.GetAwaiter().GetResult();Check(c.DrainingPresentationCount==0,"Retirement reaped "+mode);cases.Add(mode);
        }
        foreach(string mode in new[]{"false","wrong","throw","stale","generation","dispose-throw","getter-throw","getter-null","getter-reentry","dispose-and-getter-throw"})
        using(var w=World())
        {
            long g=Stage(w);var pubs=Publications(w);Fake? candidate=null;SceneWorldObjectPresentationConsumer? c=null;
            c=new(w,(p,s)=>{candidate=new(mode=="wrong"?pubs[1]:p);candidate.OnApply=()=>{if(mode=="throw")throw new ApplicationException("candidate");if(mode=="stale")w.RestartResource(g,p.Ticket.Handle.ObjectId);if(mode=="generation")w.BeginSelection(humanId);return false;};
                if(mode is "dispose-throw" or "dispose-and-getter-throw")candidate.OnDispose=()=>throw new ApplicationException("dispose");
                if(mode is "getter-throw" or "dispose-and-getter-throw")candidate.OnDrain=()=>throw new ApplicationException("getter");
                if(mode=="getter-null")candidate.OnDrain=()=>null!;
                if(mode=="getter-reentry")candidate.OnDrain=()=>{_=c!.Count;return candidate.Completion.Task;};return candidate;});using(c)
            {
                if(mode=="dispose-and-getter-throw")Reject<AggregateException>(()=>c.AcceptResource(pubs[0]));
                else if(mode is "wrong" or "getter-null")Reject<InvalidDataException>(()=>c.AcceptResource(pubs[0]));
                else if(mode=="getter-reentry")Reject<InvalidOperationException>(()=>c.AcceptResource(pubs[0]));
                else if(mode is "throw" or "dispose-throw" or "getter-throw")Reject<ApplicationException>(()=>c.AcceptResource(pubs[0]));
                else Check(!c.AcceptResource(pubs[0]),"Rollback false "+mode);
                Check(candidate!.Disposals==1&&candidate.Getters==1,"Rollback captures drain after Dispose "+mode);
                var drain=c.DrainCompletion;
                if(mode.StartsWith("getter",StringComparison.Ordinal)||mode=="dispose-and-getter-throw"){Check(drain.IsFaulted,"Unobtainable drain cannot report success");Reject<Exception>(()=>drain.GetAwaiter().GetResult());}
                else {Check(!drain.IsCompleted,"Rollback worker outstanding "+mode);candidate.Completion.SetResult();drain.GetAwaiter().GetResult();}
                c.Dispose();Check(c.Count==0,"Rollback no active roots");cases.Add("rollback-"+mode);
            }
        }
        using(var w=World())
        {
            long g=Stage(w);var pub=Publications(w).Single(p=>p.Ticket.Handle.ObjectId==staticId);var made=new List<Fake>();using var c=new SceneWorldObjectPresentationConsumer(w,(p,s)=>{var f=new Fake(p);made.Add(f);return f;});
            c.AcceptResource(pub);w.RestartResource(g,staticId);c.AcceptResource(Publications(w).Single());Check(c.Count==1&&made[0].Disposals==1&&c.DrainingPresentationCount==1,"Replacement old worker retained");
            c.Dispose();var drain=c.DrainCompletion;made[1].Completion.SetResult();Check(!drain.IsCompleted,"Close includes previously retired worker");made[0].Completion.SetResult();drain.GetAwaiter().GetResult();cases.Add("replacement-close");
        }
        using(var w=World())
        {
            Stage(w);var pubs=Publications(w);var made=new List<Fake>();int factories=0;using var c=new SceneWorldObjectPresentationConsumer(w,(p,s)=>{factories++;var f=new Fake(p);made.Add(f);return f;},2,2);
            Reject<ArgumentOutOfRangeException>(()=>new SceneWorldObjectPresentationConsumer(w,(p,s)=>null,2,1));Reject<ArgumentOutOfRangeException>(()=>new SceneWorldObjectPresentationConsumer(w,(p,s)=>null,2,8193));
            foreach(var pub in pubs)c.AcceptResource(pub);c.RetireAll();Check(c.DrainingPresentationCount==2,"Drain cap full");Reject<InvalidDataException>(()=>c.AcceptResource(pubs[0]));Check(factories==2&&c.Count==0,"Capacity before candidate factory");
            made[0].Completion.SetResult();Check(c.AcceptResource(pubs[0])&&factories==3,"Completed slot admits again");c.Dispose();var drain=c.DrainCompletion;made[2].Completion.SetResult();Check(!drain.IsCompleted,"Other retired task retained");made[1].Completion.SetResult();drain.GetAwaiter().GetResult();Check(c.DrainingPresentationCount==0,"Bounded tasks release");cases.Add("capacity-reservation");
        }
        using(var w=World())
        {
            Stage(w);var pub=Publications(w)[0];Fake? made=null;using var c=new SceneWorldObjectPresentationConsumer(w,(p,s)=>made=new(p){OnApply=()=>false},1,1);
            for(int i=0;i<128;i++){Check(!c.AcceptResource(pub),"Churn rollback");Check(c.DrainingPresentationCount==1,"One reserved rollback");made!.Completion.SetResult();Check(c.DrainingPresentationCount==0,"Completed task not a tombstone");}
            Check(c.DrainCompletion.IsCompletedSuccessfully,"Churn drained");cases.Add("bounded-churn128");
        }
        using(var w=World())
        {
            Stage(w);var pubs=Publications(w);var made=new List<Fake>();using var c=new SceneWorldObjectPresentationConsumer(w,(p,s)=>{var f=new Fake(p){OnApply=()=>false};made.Add(f);return f;},2,2);
            c.AcceptResource(pubs[0]);var longDrain=c.DrainCompletion;
            for(int i=0;i<128;i++){c.AcceptResource(pubs[1]);Check(ReferenceEquals(longDrain,c.DrainCompletion),"One group task behind long worker");made[^1].Completion.SetResult();Check(c.DrainingPresentationCount==1,"Only long task retained");}
            c.Dispose();Check(ReferenceEquals(longDrain,c.DrainCompletion)&&!longDrain.IsCompleted,"Stable close group");made[0].Completion.SetResult();longDrain.GetAwaiter().GetResult();cases.Add("bounded-long-worker-churn128");
        }
        using(var w=World())
        {
            Stage(w);var pubs=Publications(w);var shared=new TaskCompletionSource();var made=new List<Fake>();using var c=new SceneWorldObjectPresentationConsumer(w,(p,s)=>{var f=new Fake(p){OnDrain=()=>shared.Task};made.Add(f);return f;},2,2);
            foreach(var pub in pubs)c.AcceptResource(pub);c.RetireAll();Check(c.DrainingPresentationCount==1,"Exact shared task dedup");shared.SetResult();c.DrainCompletion.GetAwaiter().GetResult();Check(made.All(f=>f.Getters==1),"Each adapter's task getter observed");cases.Add("shared-task");
        }
        foreach(bool canceled in new[]{false,true})using(var w=World())
        {
            Stage(w);var pub=Publications(w)[0];Fake? made=null;using var c=new SceneWorldObjectPresentationConsumer(w,(p,s)=>made=new(p));c.AcceptResource(pub);c.RetireAll();var first=c.DrainCompletion;
            if(canceled)made!.Completion.SetCanceled();else made!.Completion.SetException(new ApplicationException("late worker fault"));
            Reject<Exception>(()=>first.GetAwaiter().GetResult());Reject<InvalidDataException>(()=>c.AcceptResource(pub));Check(c.DrainingPresentationCount==0&&c.DrainCompletion.IsFaulted,"Observed fault remains one sticky diagnostic");c.Dispose();cases.Add(canceled?"cancel-failclosed":"fault-failclosed");
        }
        using(var w=World())
        {
            long g=Stage(w);var pubs=Publications(w);var made=new List<Fake>();using var c=new SceneWorldObjectPresentationConsumer(w,(p,s)=>{var f=new Fake(p);made.Add(f);return f;});foreach(var pub in pubs)c.AcceptResource(pub);
            var batch=w.DrainChanges(g)!;var later=(Fake)c.Find(batch.Entries[1].ObjectId)!;later.OnApply=()=>throw new ApplicationException("partial batch");Reject<ApplicationException>(()=>c.ApplyBatch(batch));Check(c.Count==1&&c.DrainingPresentationCount==1&&!c.AcknowledgeAppliedBatch(batch),"Partial batch retirement remains unacknowledged");
            c.Dispose();var drain=c.DrainCompletion;later.Completion.SetResult();Check(!drain.IsCompleted,"Other actor still draining");made.Single(f=>!ReferenceEquals(f,later)).Completion.SetResult();drain.GetAwaiter().GetResult();cases.Add("partial-batch");
        }
        using(var w=World())
        {
            Stage(w);var pub=Publications(w)[0];Fake? made=null;using var c=new SceneWorldObjectPresentationConsumer(w,(p,s)=>made=new(p));c.AcceptResource(pub);
            Exception? threadError=null;var thread=new Thread(()=>{try{_=c.DrainCompletion;}catch(Exception e){threadError=e;}});thread.Start();thread.Join();Check(threadError is InvalidOperationException&&made!.Disposals==0,"Foreign drain getter cannot mutate");
            made!.OnDispose=()=>Reject<InvalidOperationException>(()=>c.RetireAll());made.OnDrain=()=>{Reject<InvalidOperationException>(()=>_=c.DrainCompletion);return made.Completion.Task;};c.Dispose();Check(!c.DrainCompletion.IsCompleted,"Caught reentry retains task");made.Completion.SetResult();c.DrainCompletion.GetAwaiter().GetResult();cases.Add("thread-reentry");
        }
        var limits=new SceneWorldPresentationPumpPolicy(MaximumActors:2,MaximumPublicationsPerStep:2,MaximumPendingPublications:2);
        foreach(string mode in new[]{"normal","dispose-throw","getter-throw","worker-fault","factory-reentry","tick-throw","tick-false","getter-reentry"})
        using(var w=World())
        {
            Stage(w);SceneWorldPresentationPump? pump=null;var made=new List<Fake>();pump=new(w,(p,s)=>{var f=new Fake(p);made.Add(f);if(mode=="dispose-throw")f.OnDispose=()=>throw new ApplicationException("dispose");if(mode=="getter-throw"&&made.Count==1)f.OnDrain=()=>throw new ApplicationException("getter");if(mode=="factory-reentry")Reject<InvalidOperationException>(()=>_=pump!.DrainCompletion);if(mode=="tick-throw")f.OnTick=()=>throw new ApplicationException("tick");if(mode=="tick-false")f.OnTick=()=>false;if(mode=="getter-reentry")f.OnDrain=()=>{Reject<InvalidOperationException>(()=>_=pump!.DrainCompletion);return f.Completion.Task;};return f;},limits,_=>1);using(pump)
            {
                pump.Step(0);
                if(mode=="factory-reentry")Reject<InvalidOperationException>(()=>pump.Step(0));else if(mode=="tick-throw")Reject<ApplicationException>(()=>pump.Step(0));else {pump.Step(0);if(mode is "dispose-throw" or "getter-throw" or "getter-reentry")Reject<AggregateException>(()=>pump.Close());else {if(mode=="tick-false")Check(!pump.DrainCompletion.IsCompleted,"Open pump exposes retired CPU task");pump.Close();}}
                Check(w.State==ZoneSceneTransitionState.Closed&&pump.Consumer.Count==0&&made.All(f=>f.Disposals==1&&f.Getters==1),"Pump closes all ownership "+mode);var drain=pump.DrainCompletion;
                if(mode=="getter-throw"){Check(!drain.IsCompleted,"Bad getter does not skip known pending task");made[1].Completion.SetResult();Reject<Exception>(()=>drain.GetAwaiter().GetResult());Check(drain.IsFaulted,"Bad task getter failclosed");}
                else
                {
                    Check(!drain.IsCompleted,"Pump waits retired CPU tasks "+mode);
                    for(int i=0;i<made.Count;i++){if(mode=="worker-fault"&&i==0)made[i].Completion.SetException(new ApplicationException("worker"));else made[i].Completion.SetResult();}
                    if(mode=="worker-fault")Reject<Exception>(()=>drain.GetAwaiter().GetResult());else drain.GetAwaiter().GetResult();
                }
                Check(ReferenceEquals(drain,pump.DrainCompletion),"Closed completion stable");cases.Add("pump-"+mode);
            }
        }
        using(var w=World())
        {
            Stage(w);var made=new List<Fake>();using var pump=new SceneWorldPresentationPump(w,(p,s)=>{var f=new Fake(p){OnApply=()=>false};made.Add(f);return f;},limits,_=>1);
            pump.Step(0);pump.Step(0);pump.Step(0);Check(made.Count==4&&pump.Consumer.DrainingPresentationCount==4,"Pump default reserves twice actor capacity");
            Reject<InvalidDataException>(()=>pump.Step(0));Check(made.Count==4&&w.State==ZoneSceneTransitionState.Closed,"Capacity failcloses before fifth candidate");var drain=pump.DrainCompletion;Check(!drain.IsCompleted,"All rollback workers remain owned");foreach(var f in made)f.Completion.SetResult();drain.GetAwaiter().GetResult();cases.Add("pump-capacity-failclosed");
        }
        var requests=new List<SceneObjectResourceRequest>();var pending=new List<TaskCompletionSource<SceneObjectResources>>();var tokens=new List<CancellationToken>();
        Task<SceneObjectResources> Delayed(SceneObjectResourceRequest request,CancellationToken token){requests.Add(request);tokens.Add(token);var t=new TaskCompletionSource<SceneObjectResources>();pending.Add(t);return t.Task;}
        using(var w=World(Delayed))
        {
            Stage(w);Fake? made=null;using var pump=new SceneWorldPresentationPump(w,(p,s)=>made=new Fake(p){OnDispose=()=>throw new ApplicationException("dispose")},limits,_=>1);pump.Step(0);pending[0].SetResult(loaded[requests[0].Options.Recipe]);pump.Step(0);
            Reject<AggregateException>(()=>pump.Close());var drain=pump.DrainCompletion;Check(!drain.IsCompleted&&tokens[1].IsCancellationRequested&&w.State==ZoneSceneTransitionState.Closed,"Cleanup throw still cancels world job");
            made!.Completion.SetException(new ApplicationException("presentation late fault"));Check(!drain.IsCompleted,"Fault does not bypass outstanding world job");pending[1].SetException(new ApplicationException("late world job"));Reject<Exception>(()=>drain.GetAwaiter().GetResult());Check(w.OutstandingJobs==0,"Combined world/presentation drained before error propagation");cases.Add("pump-combined-error-wait");
        }
        Check(archiveScopes==0,"Original archives closed");File.WriteAllText(exportPath,JsonSerializer.Serialize(new{Checks=checks,Cases=cases,OriginalSources=66,ArchiveScopes=archiveScopes,NoSleeps=true,Result="PASS"},new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine("R20_DRAIN_PASS checks="+checks+" cases="+cases.Count);
    }
}
