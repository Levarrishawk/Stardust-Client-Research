using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

internal static class SceneWorldTangibleReplicationScenarios
{
    private static int checks;
    private static void Check(bool value,string why){checks++;if(!value)throw new Exception(why);}
    private static void Reject<T>(Action action)where T:Exception{try{action();}catch(T){checks++;return;}throw new Exception("Expected "+typeof(T).Name);}
    internal static void Run(string[] inputs,IReadOnlyDictionary<string,string> peer,string fixturePath,string exportPath)
    {
        checks=0;Check(inputs.Length==4,"root config catalog bank");Check(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(inputs[1])))==RetainedSkeletalDetailBundleReader.ConfigurationPin,"Configuration pin");
        var catalog=ObjectTemplateCatalogProfile.Decode(File.ReadAllBytes(inputs[2]));using var stream=File.OpenRead(inputs[3]);var bank=SkeletalAnimationBankProfile.Load(stream);
        var idle=new LogicalAnimationClipSelection("appearance/lat/all_m.lat","emt_standing_idle_ambient_1_ag","798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B");
        var options=new Dictionary<SceneObjectResourceRecipe,SceneObjectResourceOptions>{{SceneObjectResourceRecipe.CreatureDetail0,new SceneCreatureResourceOptions(idle,bank,true)},{SceneObjectResourceRecipe.StaticAppearance,new SceneStaticResourceOptions(SceneStaticMaterialPolicy.RequireComplete,new("shader/anim_mission_terminal.sht","B5AA5A251A76AD8F152DB85F50FBC3061D0494847499F1068A9B55C375785A62",0))}};
        byte[] Bytes(string key)=>Convert.FromHexString(peer[key]);ulong id=SceneObjectLifecycleCodec.DecodeCreate(Bytes("create")).ObjectId;int loaderCalls=0;

        var fixtures=JsonSerializer.Deserialize<Dictionary<string,JsonElement>>(File.ReadAllText(fixturePath))!;
        byte[] Wire(string key)=>Convert.FromHexString(fixtures[key].GetString()!);
        ulong sid=fixtures["id"].GetUInt64();var cases=new List<string>();var receipts=new List<object>();
        var baselineLimits=new CreatureView3BaselineLimits(4096,64,16,16,8192);
        var deltaLimits=new TangibleView3DeltaLimits(4096,64,64,64,64,8192,64,64,4,64,64,16);
        var tangiblePolicy=new SceneWorldTangibleReplicationPolicy(baselineLimits,deltaLimits,new(MaximumComponentsElements:4));
        var creaturePolicy=new SceneWorldReplicationPolicy(SceneWorldReplicationMode.CreatureView3MixedComponents,new(4096,1024,64,16,8192),16,8192,new(4096,32,24,64,64,16),CreatureWoundsApplicationPolicy.LegacyBounded,new(4096,32,64,64,16),new(16,16));
        SceneWorldCoordinator World(SceneWorldTangibleReplicationPolicy? selected=null,SceneWorldChangePolicy? changes=null,bool typed=true,Func<SceneObjectResourceRequest,CancellationToken,Task<SceneObjectResources>>? suppliedLoader=null)=>new(catalog,options,suppliedLoader??((request,token)=>{loaderCalls++;throw new InvalidDataException("Synthetic resource failure");}),SceneCreateZeroQuaternionPolicy.Reject,4,2,32,8192,16,4096,4096,creaturePolicy,changes,typed?(selected??tangiblePolicy):null);
        TangibleView3DeltaPlan Plan(string key)=>TangibleView3DeltaCodec.Decode(Wire(key),deltaLimits);
        long Stage(SceneWorldCoordinator w,bool missing=false,bool otherView=false)
        {
            long g=w.BeginSelection(id).Generation;
            foreach(string key in new[]{"start","create","staticCreate","baseline"})w.Accept(g,Bytes(key));
            if(!missing){var seed=otherView?Bytes("staticBaseline"):Wire("baseline");if(otherView)seed[18]=6;w.Accept(g,seed);}
            w.Accept(g,Bytes("end"));w.Accept(g,Bytes("staticEnd"));w.CompleteMinimumSceneBarrier(g,true);return g;
        }
        SceneWorldTangibleReplicationSnapshot Current(SceneWorldCoordinator w)=>w.FindTangibleReplication(w.Generation,sid)!;
        void Same(SceneWorldCoordinator w,SceneWorldTangibleReplicationSnapshot before,SceneWorldChangeBatch batch,long charge)
        {var after=Current(w);Check(w.IsCurrent(before)&&after.Revision==before.Revision&&ReferenceEquals(after.Names,before.Names)&&ReferenceEquals(after.Customization,before.Customization)&&ReferenceEquals(after.Components,before.Components),"Failed preparation preserves exact state");Check(w.IsCurrent(batch)&&w.RetainedTangibleReplicationDecodedBytes==charge&&w.State==ZoneSceneTransitionState.AwaitingServerReady,"Failed preparation preserves journal/charge/phase");}
        foreach(string order in new[]{"forward","reverse"})using(var w=World())
        {
            long g=Stage(w);var seed=Current(w);long seedCharge=w.RetainedTangibleReplicationDecodedBytes;var objects=w.Snapshots();var creature=objects.Single(o=>o.Object.Handle.ObjectId==id).Replication!;
            Check(w.TangibleReplicationEnabled&&objects.Single(o=>o.Object.Handle.ObjectId==sid).TangibleReplication is not null,"Typed snapshot propagation");
            Check(seed.Baseline.DecodedBytes==73&&w.RetainedTangibleReplicationDecodedBytes==97&&seed.Components.Keys.SequenceEqual(new[]{int.MinValue,-1})&&seed.Baseline.Components.Values.SequenceEqual(new[]{-1,int.MinValue,-1}),"Exact baseline/raw duplicates/normalized budget");
            Check(w.IsCurrent(seed)&&objects.Single(o=>o.Object.Handle.ObjectId==sid).Envelopes.Count==0,"Complete barrier current typed no opaque history");
            var initialBatch=w.DrainChanges(g)!;Check(w.AcknowledgeChanges(initialBatch),"Acknowledge initial journal");
            using var router=new SceneWorldTangibleApplicationRouter(w,maximumPayloadBytes:4096);var owned=Wire(order);
            Check(router.Accept(g,owned),"Exact router delivery");Array.Clear(owned);var value=Current(w);long currentCharge=w.RetainedTangibleReplicationDecodedBytes;
            Check(!w.IsCurrent(seed)&&w.IsCurrent(value)&&w.IsCurrent(creature)&&ReferenceEquals(value.Baseline,seed.Baseline),"Snapshot supersession preserves other family/baseline");
            Check(value.Components.Keys.SequenceEqual(new[]{int.MinValue,-1,123})&&value.Components.Counter==2&&value.Names.CustomNameUnits.SequenceEqual(new ushort[]{65,0xd800})&&value.TangibleScalars.Options==uint.MaxValue,"Ordered names/components/common fields");
            Check(w.RetainedTangibleReplicationDecodedBytes==(order=="forward"?109:110)&&value.Revision==2,"Independent expected ordered charge/revision");
            var batch=w.DrainChanges(g)!;Check(batch.Entries.Count==1&&batch.Entries[0].Current!.TangibleReplication!.Revision==value.Revision,"Typed journal snapshot");
            var aliasUpdates=new[]{CreatureView3NamedDeltaUpdate.FromName(CreatureView3NameUpdate.FromStringId(seed.Baseline.NameFile,0,seed.Baseline.NameKey)),CreatureView3NamedDeltaUpdate.FromExtended(CreatureView3ExtendedDeltaUpdate.FromFull(CreatureView3FullDeltaUpdate.FromCustomization(seed.Baseline.Customization)))};
            Check(w.ApplyTangibleDelta(g,new(sid,0,aliasUpdates,4))&&w.RetainedTangibleReplicationDecodedBytes==105,"World historical alias charge");long aliasCharge=w.RetainedTangibleReplicationDecodedBytes;
            var equal=CreatureView3NamedDeltaUpdate.FromExtended(CreatureView3ExtendedDeltaUpdate.FromFull(CreatureView3FullDeltaUpdate.FromCustomization(ReplicationByteString.Create(seed.Baseline.Customization.CopyBytes()))));
            Check(w.ApplyTangibleDelta(g,new(sid,0,new[]{equal},4))&&w.RetainedTangibleReplicationDecodedBytes==108,"World equal new storage charged");long equalCharge=w.RetainedTangibleReplicationDecodedBytes;
            Check(w.ApplyTangibleDelta(g,Plan("noop")),"Duplicate/noop commits one revision");var duplicate=Current(w);Check(duplicate.Components.Keys.SequenceEqual(value.Components.Keys)&&duplicate.Revision==5,"Duplicate keys unchanged");
            Check(w.Accept(g,Wire("baseline")),"Genuine baseline reseed");Check(Current(w).Revision==6&&w.RetainedTangibleReplicationDecodedBytes==97&&Current(w).Customization.CopyBytes().SequenceEqual(new byte[]{1,2,3}),"Reseed resets overlays and charge");
            receipts.Add(new{Name=order,BaselineBytes=seed.Baseline.DecodedBytes,SeedBytes=seedCharge,CurrentBytes=currentCharge,AliasBytes=aliasCharge,EqualBytes=equalCharge,Keys=value.Components.Keys,Counter=value.Components.Counter,Raw=Convert.ToHexString(value.Customization.CopyBytes()),File=Convert.ToHexString(value.Names.NameFile.CopyBytes()),Key=Convert.ToHexString(value.Names.NameKey.CopyBytes()),Units=value.Names.CustomNameUnits,Visibility=value.TangibleScalars.Visibility,Options=value.TangibleScalars.Options,Revision=value.Revision});cases.Add("mixed-"+order);
        }
        foreach(string fault in new[]{"raw","name","components","store-revision","global"})using(var w=World(tangiblePolicy with {StoreLimits=new(MaximumComponentsElements:4,MaximumDecodedBytes:fault=="global"?194:97),RevisionSeed=fault=="store-revision"?ulong.MaxValue-1:0}))
        {
            long g=Stage(w);if(fault=="global")
            {var create=SceneObjectLifecycleCodec.DecodeCreate(Bytes("staticCreate"));w.Accept(g,SceneObjectLifecycleCodec.EncodeCreate(create with {ObjectId=sid+100}));w.Accept(g,Wire("baselineOther"));w.Accept(g,SceneObjectLifecycleCodec.EncodeEndBaselines(new(sid+100)));}
            var before=Current(w);var batch=w.DrainChanges(g)!;long charge=w.RetainedTangibleReplicationDecodedBytes;
            string key=fault=="name"?"nameEmpty":fault=="components"?"growthClear":fault=="store-revision"?"noop":"rawEmpty";
            if(fault=="store-revision")Reject<OverflowException>(()=>w.ApplyTangibleDelta(g,Plan(key)));else Reject<InvalidDataException>(()=>w.ApplyTangibleDelta(g,Plan(key)));
            Same(w,before,batch,charge);Check(w.ApplyTangibleDelta(g-1,null!)==false,"Stale generation before plan access");cases.Add("rollback-"+fault);
        }
        using(var w=World(changes:new(2)))
        {
            long g=Stage(w);w.AcknowledgeChanges(w.DrainChanges(g)!);var create=SceneObjectLifecycleCodec.DecodeCreate(Bytes("staticCreate"));
            foreach(ulong extra in new[]{sid+100,sid+101})w.Accept(g,SceneObjectLifecycleCodec.EncodeCreate(create with {ObjectId=extra}));
            var before=Current(w);var batch=w.DrainChanges(g)!;Reject<InvalidDataException>(()=>w.ApplyTangibleDelta(g,Plan("forward")));Same(w,before,batch,97);cases.Add("journal-capacity");
        }
        using(var w=World(changes:new(4,ulong.MaxValue-6)))
        {long g=Stage(w);var before=Current(w);var batch=w.DrainChanges(g)!;Reject<OverflowException>(()=>w.ApplyTangibleDelta(g,Plan("noop")));Same(w,before,batch,97);cases.Add("world-revision-overflow");}
        foreach(string fault in new[]{"missing","other-view","opaque-short"})using(var w=World())
        {
            if(fault=="opaque-short")Reject<InvalidDataException>(()=>{long g=w.BeginSelection(id).Generation;foreach(string k in new[]{"start","create","staticCreate","baseline","staticBaseline","end","staticEnd"})w.Accept(g,Bytes(k));w.CompleteMinimumSceneBarrier(g,true);});
            else Reject<InvalidDataException>(()=>Stage(w,fault=="missing",fault=="other-view"));
            Check(w.Snapshots().Count==0&&w.RetainedTangibleReplicationDecodedBytes==0,"Bad end barrier drains typed generation");cases.Add("barrier-"+fault);
        }
        using(var w=World(typed:false)){long g=w.BeginSelection(id).Generation;foreach(string k in new[]{"start","create","staticCreate","baseline","staticBaseline","end","staticEnd"})w.Accept(g,Bytes(k));w.CompleteMinimumSceneBarrier(g,true);Check(!w.TangibleReplicationEnabled&&w.Snapshots().Single(o=>o.Object.Handle.ObjectId==sid).TangibleReplication is null&&w.Snapshots().Single(o=>o.Object.Handle.ObjectId==sid).Envelopes.Count==1,"Opaque default remains source compatible");cases.Add("opaque-default");}
        foreach(string fault in new[]{"throw","null","target","count","world","ack","restart","reentry","dispose","generation"})using(var w=World())
        {
            long g=Stage(w);var before=Current(w);var batch=w.DrainChanges(g)!;SceneWorldTangibleApplicationRouter? router=null;
            router=new(w,envelope=>
            {
                if(fault=="throw")throw new InvalidDataException("Injected failure");if(fault=="null")return null!;
                if(fault=="world")w.ApplyTangibleDelta(g,Plan("noop"));if(fault=="ack")w.AcknowledgeChanges(batch);if(fault=="restart")w.RestartResource(g,sid);
                if(fault=="reentry")try{router!.Accept(g,Wire("noop"));}catch(InvalidOperationException){}
                if(fault=="dispose")try{router!.Dispose();}catch(InvalidOperationException){}
                if(fault=="generation")w.BeginSelection(id);
                var p=Plan("forward");return new(fault=="target"?sid+1:sid,fault=="count"?(ushort)0:p.DeclaredCount,p.Updates,p.MaximumComponentsElements);
            },maximumPayloadBytes:4096);
            using(router){Reject<Exception>(()=>router.Accept(g,Wire("forward")));Reject<ObjectDisposedException>(()=>router.Accept(g,Wire("noop")));}
            if(fault is not ("world" or "generation"))Check(w.IsCurrent(before)&&Current(w).Revision==before.Revision,"Callback failure does not publish returned plan");
            if(fault=="world")Check(Current(w).Revision==before.Revision+1,"Callback side effect explicitly retained");
            if(fault=="ack")Check(w.DrainChanges(g)!.Entries.Count==0,"Callback acknowledgement retained and detected");cases.Add("decoder-"+fault);
        }
        foreach(bool baselineCallback in new[]{false,true})
        {
            SceneWorldCoordinator? w=null;var p=tangiblePolicy;
            if(baselineCallback)p=p with {BaselineDecoder=e=>{try{w!.Snapshots();}catch(InvalidOperationException){}return TangibleView3BaselineCodec.Decode(e,baselineLimits);}};
            else p=p with {DeltaDecoder=e=>{try{w!.Snapshots();}catch(InvalidOperationException){}return TangibleView3DeltaCodec.Decode(e,deltaLimits);}};
            using(w=World(p))
            {
                if(baselineCallback){Reject<InvalidOperationException>(()=>Stage(w));Check(w.Snapshots().Count==0,"Baseline caught reentry fails barrier");}
                else{long g=Stage(w);var before=Current(w);using var router=new SceneWorldTangibleApplicationRouter(w,maximumPayloadBytes:4096);Reject<InvalidOperationException>(()=>router.Accept(g,Wire("noop")));Check(w.IsCurrent(before),"Policy decoder caught reentry has no prefix");}
            }cases.Add(baselineCallback?"baseline-policy-reentry":"delta-policy-reentry");
        }
        using(var w=World())
        {
            long g=Stage(w);var before=Current(w);using var router=new SceneWorldTangibleApplicationRouter(w,maximumPayloadBytes:4096);
            Reject<InvalidDataException>(()=>router.Accept(g,Wire("unknown")));Check(w.IsCurrent(before),"Unknown field rejects without opaque fallback");
            w.Accept(g,Bytes("staticDestroy"));Check(!w.IsCurrent(before)&&w.RetainedTangibleReplicationDecodedBytes==0,"Destroy retires tangible");
            w.Accept(g,Bytes("staticCreate"));w.Accept(g,Wire("baseline"));w.Accept(g,Bytes("staticEnd"));Check(Current(w).Incarnation!=before.Incarnation&&!w.IsCurrent(before),"Recreation excludes old identity");
            w.BeginSelection(id);Check(w.RetainedTangibleReplicationDecodedBytes==0&&!w.ApplyTangibleDelta(g,Plan("noop")),"Reset drains/stale work rejected");cases.Add("unknown-destroy-recreate-reset");
        }
        using(var w=World())
        {
            long g=Stage(w);var before=Current(w);using var foreign=World();Stage(foreign);Check(!foreign.IsCurrent(before),"Foreign world token rejected");
            Exception? error=null;Task.Run(()=>{try{w.ApplyTangibleDelta(g,Plan("noop"));}catch(Exception e){error=e;}}).GetAwaiter().GetResult();Check(error is InvalidOperationException&&w.IsCurrent(before),"Foreign thread preflight");
            using(var router=new SceneWorldTangibleApplicationRouter(w,downstream:(generation,message)=>w.Accept(generation,message.Span),maximumPayloadBytes:4096))Check(router.Accept(g,Bytes("serverReady")),"Non TANO downstream CREO chain preserved");
            w.Close();w.Close();Check(!w.IsCurrent(before)&&w.RetainedTangibleReplicationDecodedBytes==0,"Close drains/idempotent");Reject<ObjectDisposedException>(()=>w.ApplyTangibleDelta(g,Plan("noop")));cases.Add("owner-close-downstream");
        }
        using(var w=World())
        {long g=Stage(w);var before=Current(w);Check(w.PollObjectResources().Prepared.Count==0,"First poll starts jobs only");Reject<InvalidDataException>(()=>w.PollObjectResources());Check(!w.IsCurrent(before)&&w.Snapshots().Count==0&&w.RetainedTangibleReplicationDecodedBytes==0,"Resource failure drains typed state");cases.Add("resource-failure");}
        using(var w=World())
        {long g=Stage(w);var before=Current(w);var batch=w.DrainChanges(g)!;Reject<InvalidDataException>(()=>w.Accept(g,Wire("unknown")));Same(w,before,batch,97);Check(w.Accept(g,Wire("noop")),"Healthy direct typed ingress recovers");cases.Add("direct-ingress-rollback");}
        {
            var jobs=new List<TaskCompletionSource<SceneObjectResources>>();
            using var w=World(suppliedLoader:(request,cancel)=>{var job=new TaskCompletionSource<SceneObjectResources>(TaskCreationOptions.RunContinuationsAsynchronously);jobs.Add(job);return job.Task;});
            long g=Stage(w);var before=Current(w);w.PollObjectResources();Check(jobs.Count==2&&w.OutstandingJobs==2,"Controlled pending jobs");
            w.Accept(g,Bytes("staticDestroy"));w.BeginSelection(id);Check(w.RetainedTangibleReplicationDecodedBytes==0&&!w.IsCurrent(before),"Destroy/reset drains while jobs held");
            foreach(var job in jobs)job.SetException(new InvalidDataException("Late stale synthetic failure"));
            var poll=w.PollObjectResources();Check(poll.Prepared.Count==0&&poll.Retired.Count==2&&w.OutstandingJobs==0&&w.Snapshots().Count==0,"Late stale jobs retire without typed resurrection");cases.Add("held-stale-jobs");
        }
        using(var w=World())
        {
            long g=Stage(w);using var router=new SceneWorldTangibleApplicationRouter(w,maximumPayloadBytes:4096);var before=Current(w);
            Check(!router.Accept(g-1,ReadOnlyMemory<byte>.Empty)&&w.IsCurrent(before),"Router stale generation before parse");
            Exception? error=null;Task.Run(()=>{try{router.Accept(g,Wire("noop"));}catch(Exception e){error=e;}}).GetAwaiter().GetResult();
            Check(error is InvalidOperationException&&router.Accept(g,Wire("noop")),"Router owner rejection leaves healthy ingress");cases.Add("router-owner-stale");
        }
        foreach(string limit in new[]{"baseline","delta","elements"})
        {
            var bad=limit=="baseline"?tangiblePolicy with {BaselineLimits=baselineLimits with {MaximumStringBytes=-1}}:
                limit=="delta"?tangiblePolicy with {DeltaLimits=deltaLimits with {MaximumFields=4097}}:
                tangiblePolicy with {DeltaLimits=deltaLimits with {MaximumComponentElements=3}};
            Reject<ArgumentOutOfRangeException>(()=>World(bad));cases.Add("policy-limit-"+limit);
        }
        using(var w=World())
        {
            long g=Stage(w);int called=0;var before=Current(w);using var router=new SceneWorldTangibleApplicationRouter(w,e=>{called++;return Plan("noop");},maximumPayloadBytes:4096);
            Reject<InvalidDataException>(()=>router.Accept(g,Wire("unknown")));Check(called==0,"Unknown grammar rejected before injected decoder");Check(w.IsCurrent(before),"Unknown callback cannot reinterpret creature field");cases.Add("unknown-before-injected-decoder");
        }
        using(var w=World())
        {
            long g=w.BeginSelection(id).Generation;foreach(string key in new[]{"start","create","staticCreate","baseline"})w.Accept(g,Bytes(key));
            var wrong=Wire("baseline");BinaryPrimitives.WriteUInt64LittleEndian(wrong.AsSpan(6),id);w.Accept(g,wrong);w.Accept(g,Bytes("end"));w.Accept(g,Bytes("staticEnd"));
            Reject<InvalidDataException>(()=>w.CompleteMinimumSceneBarrier(g,true));Check(w.Snapshots().Count==0&&w.RetainedTangibleReplicationDecodedBytes==0,"TANO family cannot seed creature recipe");cases.Add("barrier-wrong-family");
        }
        using(var w=World())
        {long g=Stage(w);var before=Current(w);var batch=w.DrainChanges(g)!;var p=Plan("noop");Reject<InvalidDataException>(()=>w.ApplyTangibleDelta(g,new(id,p.DeclaredCount,p.Updates,p.MaximumComponentsElements)));Same(w,before,batch,97);cases.Add("delta-wrong-recipe");}
        File.WriteAllText(exportPath,JsonSerializer.Serialize(new{Checks=checks,Cases=cases,Receipts=receipts,LoaderCalls=loaderCalls,ExactTanoWires=true},new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine($"R27_TANGIBLE_WORLD_PASS checks={checks} cases={cases.Count}");
    }
}
