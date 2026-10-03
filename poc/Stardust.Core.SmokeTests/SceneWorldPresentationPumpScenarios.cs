using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

internal static class SceneWorldPresentationPumpScenarios
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
        var replication=new SceneWorldReplicationPolicy(SceneWorldReplicationMode.CreatureView3MixedComponents,new(1024,64,64,16,512),16,1024,new(1024,32,24,64,64,16),CreatureWoundsApplicationPolicy.LegacyBounded,new(1024,32,64,64,16),new(16,16));
        SceneWorldCoordinator World(Func<SceneObjectResourceRequest,CancellationToken,Task<SceneObjectResources>>? loader=null)=>new(catalog,options,loader??((q,ct)=>Task.FromResult(loaded[q.Options.Recipe])),SceneCreateZeroQuaternionPolicy.Reject,4,2,32,8192,16,4096,1024,replication);
        long Stage(SceneWorldCoordinator w){long g=w.BeginSelection(humanId).Generation;foreach(var n in new[]{"start","create","staticCreate","baseline","staticBaseline","end","staticEnd"})w.Accept(g,Bytes(n));w.CompleteMinimumSceneBarrier(g,true);return g;}
        var limits=new SceneWorldPresentationPumpPolicy(MaximumQueuedMessages:16,MaximumQueuedBytes:2048,MaximumMessageBytes:256,MaximumMessagesPerStep:2,MaximumPendingPublications:2,MaximumPublicationsPerStep:1,MaximumPublicationAttempts:3,MaximumChangeAttemptsPerStep:2,MaximumActors:2,MaximumPendingResourceWeight:8);
        using(var w=World())
        {
            var actors=new List<Fake>();using var pump=new SceneWorldPresentationPump(w,(p,s)=>{var f=new Fake(p);actors.Add(f);return f;},limits,_=>1);
            Check(!pump.Step(0).Active&&w.State==ZoneSceneTransitionState.Idle,"Idle Tick false not fatal");long g=w.BeginSelection(humanId).Generation;
            foreach(var n in new[]{"start","create","staticCreate","baseline","staticBaseline","end","staticEnd"}){var bytes=Bytes(n);pump.QueueApplication(g,bytes);Array.Clear(bytes);}
            Check(pump.QueuedMessageCount==7&&w.Snapshots().Count==0,"Copied queue owns input and no immediate world mutation");int[] accepted={2,2,2,1};foreach(int count in accepted){var step=pump.Step(0);Check(step.AcceptedMessages==count&&!step.Active,"FIFO perstep message bound before barrier");}
            Check(w.State==ZoneSceneTransitionState.Initializing&&actors.Count==0&&pump.QueuedMessageBytes==0,"Host barrier unchanged");w.CompleteMinimumSceneBarrier(g,true);pump.Step(0);var first=pump.Step(0);
            Check(first.PreparedResources==2&&first.PublishedResources==1&&first.PendingPublications==1&&pump.PendingResourceWeight==1,"Burst retained and one publication per step");var second=pump.Step(.016);Check(second.PublishedResources==1&&pump.Consumer.Count==2&&pump.PendingResourceWeight==0&&actors.All(a=>a.Ticks>0),"Remaining resource never lost; active tick");
            var humanActor=(Fake)pump.Consumer.Find(humanId)!;var staticActor=(Fake)pump.Consumer.Find(staticId)!;pump.QueueApplication(g,Bytes("composite"));pump.QueueApplication(g,Bytes("staticDelta"));var update=pump.Step(.02);Check(update.AcceptedMessages==2&&update.ChangeAcknowledged&&actors.Count==2&&humanActor.Last!.Replication!.Revision==2&&staticActor.Last!.Replication is null,"State/opaque changes preserve adapters");
            long next=w.BeginSelection(humanId).Generation;pump.QueueApplication(g,Bytes("serverReady"));pump.QueueApplication(next,Bytes("start"));var reset=pump.Step(0);Check(reset.DroppedMessages==1&&reset.AcceptedMessages==1&&actors.All(a=>a.Disposals==1)&&pump.Consumer.Count==0,"External reset clears old generation roots/messages");
            Exception? foreign=null;var thread=new Thread(()=>{try{pump.Step(0);}catch(Exception e){foreign=e;}});thread.Start();thread.Join();Check(foreign is InvalidOperationException&&w.State!=ZoneSceneTransitionState.Closed,"Foreign thread rejected before mutation");pump.Close();pump.Close();Check(pump.DrainCompletion.IsCompleted&&pump.QueuedMessageCount==0&&pump.PendingPublicationCount==0,"Idempotent close/empty ownership");Reject<ObjectDisposedException>(()=>pump.QueueApplication(next,Bytes("start")));Check(!pump.Step(0).Active,"Closed step false");
        }
        using(var w=World())
        {
            var small=limits with{MaximumQueuedMessages=2,MaximumQueuedBytes=12,MaximumMessageBytes=6,MaximumMessagesPerStep=1};using var pump=new SceneWorldPresentationPump(w,(p,s)=>new Fake(p),small,_=>1);long g=w.BeginSelection(humanId).Generation;
            pump.QueueApplication(g,Bytes("serverReady"));pump.QueueApplication(g,Bytes("serverReady"));Reject<InvalidDataException>(()=>pump.QueueApplication(g,Bytes("serverReady")));Check(pump.QueuedMessageCount==2&&pump.QueuedMessageBytes==12,"Queue capacity rejection preserves retained bytes");Reject<ArgumentOutOfRangeException>(()=>pump.QueueApplication(g,new byte[7]));Reject<ArgumentOutOfRangeException>(()=>pump.QueueApplication(g,Array.Empty<byte>()));Check(pump.QueuedMessageCount==2&&w.State==ZoneSceneTransitionState.Selecting,"Size rejects before copy/world mutation");
        }
        foreach(var bad in new[]{limits with{MaximumQueuedMessages=0},limits with{MaximumQueuedBytes=0},limits with{MaximumMessageBytes=2049},limits with{MaximumMessagesPerStep=17},limits with{MaximumPendingPublications=0},limits with{MaximumPublicationsPerStep=3},limits with{MaximumPublicationAttempts=0},limits with{MaximumChangeAttemptsPerStep=17},limits with{MaximumActors=0},limits with{MaximumPendingResourceWeight=0}})
        using(var w=World()){Reject<ArgumentOutOfRangeException>(()=>new SceneWorldPresentationPump(w,(p,s)=>null,bad));Check(w.State==ZoneSceneTransitionState.Idle,"Invalid constructor does not close unowned world");}
        foreach(var mode in new[]{"count","weight","overflow","negative","weight-throw"})
        using(var w=World())
        {
            Stage(w);int factories=0;var bound=mode=="count"?limits with{MaximumPendingPublications=1}:mode=="weight"?limits with{MaximumPendingResourceWeight=1}:limits with{MaximumPendingResourceWeight=long.MaxValue};
            using var pump=new SceneWorldPresentationPump(w,(p,s)=>{factories++;return new Fake(p);},bound,_=>mode=="overflow"?long.MaxValue:mode=="negative"?-1:mode=="weight-throw"?throw new ApplicationException("weight callback"):1);
            pump.Step(0);if(mode=="overflow")Reject<OverflowException>(()=>pump.Step(0));else if(mode=="weight-throw")Reject<ApplicationException>(()=>pump.Step(0));else Reject<InvalidDataException>(()=>pump.Step(0));Check(factories==0&&pump.PendingPublicationCount==0&&pump.PendingResourceWeight==0&&w.State==ZoneSceneTransitionState.Closed,"Entire burst preflight fails without factory prefix "+mode);
        }
        using(var w=World())
        {
            Stage(w);using var pump=new SceneWorldPresentationPump(w,(p,s)=>new Fake(p));pump.Step(0);var step=pump.Step(0);Check(step.PreparedResources==2&&step.PublishedResources==2,"Default SourceBytes weight positive completions");
        }
        using(var w=World())
        {
            Stage(w);int calls=0;using var pump=new SceneWorldPresentationPump(w,(p,s)=>{calls++;return calls<=2?null:new Fake(p);},limits,_=>1);pump.Step(0);var one=pump.Step(0);Check(calls==1&&one.PendingPublications==2,"False acceptance attempted once, retained");var two=pump.Step(0);Check(calls==2&&two.PendingPublications==2,"FIFO rotation prevents spinning");var three=pump.Step(0);var four=pump.Step(0);Check(calls==4&&three.PublishedResources==1&&four.PublishedResources==1&&pump.Consumer.Count==2,"False then successful later-step retry");
        }
        using(var w=World())
        {
            Stage(w);int calls=0;using var pump=new SceneWorldPresentationPump(w,(p,s)=>{calls++;return null;},limits with{MaximumPublicationAttempts=1},_=>1);pump.Step(0);Reject<InvalidDataException>(()=>pump.Step(0));Check(calls==1&&w.State==ZoneSceneTransitionState.Closed&&pump.PendingResourceWeight==0,"Exhausted acceptance closes retained burst");
        }
        foreach(var mode in new[]{"factory","reentrant-caught","apply","tick","dispose"})
        using(var w=World())
        {
            Stage(w);var actors=new List<Fake>();SceneWorldPresentationPump? pump=null;
            pump=new(w,(p,s)=>{if(mode=="factory")throw new ApplicationException("factory");if(mode=="reentrant-caught")Reject<InvalidOperationException>(()=>pump!.QueueApplication(w.Generation,Bytes("staticDelta")));var f=new Fake(p);actors.Add(f);if(mode=="apply")f.OnApply=()=>throw new ApplicationException("apply");if(mode=="tick")f.OnTick=()=>throw new ApplicationException("tick");if(mode=="dispose")f.OnDispose=()=>throw new ApplicationException("dispose");return f;},limits,_=>1);using(pump)
            {
                pump.Step(0);if(mode=="dispose"){pump.Step(0);Reject<AggregateException>(()=>pump.Close());}else if(mode=="reentrant-caught")Reject<InvalidOperationException>(()=>pump.Step(0));else Reject<ApplicationException>(()=>pump.Step(0));
                Check(w.State==ZoneSceneTransitionState.Closed&&pump.PendingPublicationCount==0&&pump.QueuedMessageBytes==0&&actors.All(f=>f.Disposals==1),"Callback error closes all ownership "+mode);
            }
        }
        using(var w=World())
        {
            long g=Stage(w);var actors=new List<Fake>();using var pump=new SceneWorldPresentationPump(w,(p,s)=>{var f=new Fake(p);actors.Add(f);return f;},limits,_=>1);pump.Step(0);pump.Step(0);pump.Step(0);var humanActor=(Fake)pump.Consumer.Find(humanId)!;int calls=0;humanActor.OnApply=()=>{calls++;return false;};pump.QueueApplication(g,Bytes("composite"));var failed=pump.Step(0);Check(calls==2&&!failed.ChangeAcknowledged&&w.State!=ZoneSceneTransitionState.Closed&&actors.All(a=>a.Disposals==0),"Change frame budget retains actors and pending state");humanActor.OnApply=null;Check(pump.Step(0).ChangeAcknowledged&&((Fake)pump.Consumer.Find(humanId)!).Last!.Replication!.Revision==2,"False application retries successfully in next frame");
        }
        using(var w=World())
        {
            long g=Stage(w);using var pump=new SceneWorldPresentationPump(w,(p,s)=>new Fake(p),limits,_=>1);pump.Step(0);pump.Step(0);pump.Step(0);var humanActor=(Fake)pump.Consumer.Find(humanId)!;humanActor.OnApply=()=>{w.BeginSelection(humanId);return true;};pump.QueueApplication(g,Bytes("composite"));Check(!pump.Step(0).Active&&w.State==ZoneSceneTransitionState.Selecting&&pump.Consumer.Count==0,"Callback external generation reset is not failure");
        }
        using(var w=World())
        {
            long g=Stage(w);using var pump=new SceneWorldPresentationPump(w,(p,s)=>new Fake(p),limits with{MaximumChangeAttemptsPerStep=1},_=>1);pump.Step(0);pump.Step(0);pump.Step(0);var actor=(Fake)pump.Consumer.Find(humanId)!;bool once=false;actor.OnApply=()=>{if(!once){once=true;w.Accept(g,Bytes("staticDelta"));}return true;};pump.QueueApplication(g,Bytes("composite"));var stale=pump.Step(0);Check(!stale.ChangeAcknowledged&&w.DrainChanges(g)!.Entries.Count==2&&pump.Consumer.Count==2,"Callback stale batch remains pending at one-attempt frame budget");Check(pump.Step(0).ChangeAcknowledged&&w.DrainChanges(g)!.Entries.Count==0,"Newer current batch acknowledged next frame");
        }
        using(var w=World())
        {
            long old=w.BeginSelection(humanId).Generation;using var pump=new SceneWorldPresentationPump(w,(p,s)=>null,limits,_=>1);for(int i=0;i<3;i++)pump.QueueApplication(old,Bytes("serverReady"));long next=w.BeginSelection(humanId).Generation;pump.QueueApplication(next,Bytes("start"));pump.QueueApplication(next,Bytes("create"));
            var a=pump.Step(0);var b=pump.Step(0);var c=pump.Step(0);Check(a.AcceptedMessages+a.DroppedMessages==2&&a.DroppedMessages==2&&a.PendingMessages==3,"Reset leaves old/new queued FIFO under perstep cap");Check(b.AcceptedMessages==1&&b.DroppedMessages==1&&c.AcceptedMessages==1&&c.DroppedMessages==0&&pump.QueuedMessageCount==0,"Reset drops old then applies new FIFO without exceeding budget");
        }
        var pending=new List<TaskCompletionSource<SceneObjectResources>>();var requests=new List<SceneObjectResourceRequest>();var tokens=new List<CancellationToken>();
        Task<SceneObjectResources> Delayed(SceneObjectResourceRequest q,CancellationToken t){requests.Add(q);tokens.Add(t);var task=new TaskCompletionSource<SceneObjectResources>();pending.Add(task);return task.Task;}
        using(var w=World(Delayed))
        {
            long g=Stage(w);using var pump=new SceneWorldPresentationPump(w,(p,s)=>new Fake(p),limits,_=>1);pump.Step(0);int h=requests.FindIndex(q=>q.Options.Recipe==SceneObjectResourceRecipe.CreatureDetail0),t=1-h;pending[t].SetResult(loaded[SceneObjectResourceRecipe.StaticAppearance]);Check(pump.Step(0).PublishedResources==1&&pump.Consumer.Find(staticId) is not null,"Out-of-order completion publishes without waiting for older job");pump.QueueApplication(g,Bytes("composite"));pump.Step(0);pending[h].SetResult(loaded[SceneObjectResourceRecipe.CreatureDetail0]);pump.Step(0);Check(((Fake)pump.Consumer.Find(humanId)!).Last!.Replication!.Revision==2,"Delayed resource sees already acknowledged latest state");
            pump.Close();Check(pump.DrainCompletion.IsCompleted&&pump.Consumer.Count==0,"Completed jobs close cleanly");
        }
        pending.Clear();requests.Clear();tokens.Clear();
        using(var w=World(Delayed))
        {
            Stage(w);using var pump=new SceneWorldPresentationPump(w,(p,s)=>new Fake(p),limits,_=>1);pump.Step(0);pump.QueueApplication(w.Generation,Bytes("staticDelta"));pump.Close();Check(tokens.All(t=>t.IsCancellationRequested)&&w.OutstandingJobs==2&&!pump.DrainCompletion.IsCompleted&&pump.QueuedMessageCount==0,"Close cancels jobs/clears queued bytes and captures late drain");foreach(var t in pending)t.SetException(new Exception("synthetic ignored cancellation"));pump.DrainCompletion.GetAwaiter().GetResult();Check(w.OutstandingJobs==0&&pump.Consumer.Count==0,"Late faults observed through captured drain");
        }
        using(var w=World())
        {
            Stage(w);using var pump=new SceneWorldPresentationPump(w,(p,s)=>new Fake(p),limits,_=>1);pump.Step(0);pump.QueueApplication(w.Generation,new byte[]{1});Reject<InvalidDataException>(()=>pump.Step(0));Check(w.State==ZoneSceneTransitionState.Closed&&pump.QueuedMessageBytes==0,"Malformed message fails and closes pump");
        }
        using(var w=World())
        {
            Stage(w);using var pump=new SceneWorldPresentationPump(w,(p,s)=>null,limits,_=>2);pump.Step(0);pump.Step(0);Check(pump.PendingPublicationCount==2&&pump.PendingResourceWeight==4,"False publications retain logical weight");long g=w.BeginSelection(humanId).Generation;pump.QueueApplication(g,Bytes("start"));var step=pump.Step(0);Check(step.RetiredResources==2&&pump.PendingPublicationCount==0&&pump.PendingResourceWeight==0&&step.AcceptedMessages==1,"External generation reset releases retained publications before current work");
        }
        using(var w=World())
        {
            long g=Stage(w);using var pump=new SceneWorldPresentationPump(w,(p,s)=>null,limits,_=>1);pump.Step(0);pump.Step(0);w.RestartResource(g,staticId);var step=pump.Step(0);Check(step.RetiredResources==1&&pump.PendingPublicationCount==1&&pump.PendingResourceWeight==1,"Restart invalidates retained exact attempt without retrying stale publication");
        }
        using(var w=World())
        {
            long g=w.BeginSelection(humanId).Generation;using var pump=new SceneWorldPresentationPump(w,(p,s)=>null,limits,_=>1);for(int i=0;i<3;i++)pump.QueueApplication(g-1,Bytes("serverReady"));var step=pump.Step(0);Check(step.DroppedMessages==2&&step.PendingMessages==1,"Normal stale dequeue charged to perstep message budget");Check(pump.Step(0).DroppedMessages==1&&pump.QueuedMessageBytes==0,"Stale FIFO drains next step without parse");
        }
        using(var w=World())
        {
            var small=limits with{MaximumQueuedMessages=3,MaximumQueuedBytes=12,MaximumMessageBytes=6};using var pump=new SceneWorldPresentationPump(w,(p,s)=>null,small,_=>1);pump.QueueApplication(0,Bytes("serverReady"));pump.QueueApplication(0,Bytes("serverReady"));Reject<InvalidDataException>(()=>pump.QueueApplication(0,Bytes("serverReady")));Check(pump.QueuedMessageCount==2&&pump.QueuedMessageBytes==12,"Byte budget rejects before count cap");
        }
        pending.Clear();requests.Clear();tokens.Clear();
        using(var w=World(Delayed))
        {
            long g=Stage(w);using var pump=new SceneWorldPresentationPump(w,(p,s)=>new Fake(p),limits,_=>1);pump.Step(0);w.RestartResource(g,staticId);int h=requests.FindIndex(q=>q.Options.Recipe==SceneObjectResourceRecipe.CreatureDetail0),t=1-h;pending[t].SetException(new Exception("synthetic stale completion"));Check(pump.Step(0).RetiredResources==1&&w.State!=ZoneSceneTransitionState.Closed,"Late superseded fault retires without pump failure");pending[h].SetResult(loaded[SceneObjectResourceRecipe.CreatureDetail0]);pump.Step(0);pending[2].SetException(new Exception("synthetic current completion fault"));Reject<InvalidDataException>(()=>pump.Step(0));Check(pump.Consumer.Count==0&&w.State==ZoneSceneTransitionState.Closed,"Current resource fault closes consumer/world");
        }
        pending.Clear();requests.Clear();tokens.Clear();
        using(var w=World(Delayed))
        {
            Stage(w);using var pump=new SceneWorldPresentationPump(w,(p,s)=>new Fake(p){OnDispose=()=>throw new ApplicationException("adapter cleanup")},limits,_=>1);pump.Step(0);int h=requests.FindIndex(q=>q.Options.Recipe==SceneObjectResourceRecipe.CreatureDetail0),t=1-h;pending[h].SetResult(loaded[SceneObjectResourceRecipe.CreatureDetail0]);pump.Step(0);Reject<AggregateException>(()=>pump.Close());Check(w.State==ZoneSceneTransitionState.Closed&&tokens[t].IsCancellationRequested&&!pump.DrainCompletion.IsCompleted,"Throwing consumer disposal still closes/cancels world and captures drain");pending[t].SetException(new Exception("synthetic delayed fault after failed cleanup"));pump.DrainCompletion.GetAwaiter().GetResult();Check(w.OutstandingJobs==0&&pump.Consumer.Count==0,"Drain available after cleanup error");
        }
        using(var w=World()){using var pump=new SceneWorldPresentationPump(w,(p,s)=>null,limits,_=>1);Reject<ArgumentOutOfRangeException>(()=>pump.Step(double.NaN));Check(w.State==ZoneSceneTransitionState.Closed,"Invalid step time is a deterministic failure");}
        File.WriteAllText(exportPath,JsonSerializer.Serialize(new{Checks=checks,Recipes=2,OriginalSources=66,ArchiveScopes=archiveScopes,Result="PASS"},new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine("R16_PUMP_PASS checks="+checks);
    }
}
