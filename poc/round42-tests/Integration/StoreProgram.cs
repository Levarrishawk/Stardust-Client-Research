using System.Reflection;
using System.Text.Json;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

try
{
    if(args.Length!=2 || new FileInfo(args[0]).Length is <=0 or >33554432)throw new ArgumentException("bounded full-fixture native-receipt");
    var modelLimits=new CreatureView4StateLimits(4096,65536,196608,1048576,4194304,16777216,262144,33554432,16777216);
    var messageLimits=new CreatureView4FullLimits(8388608,4096,65536,1048576,1048576,4194304,16777216,16777216);
    var stateLimits=new CreatureView4FullStateLimits(modelLimits,messageLimits);
    var storeLimits=new SceneCreatureView4FullStoreLimits(4,134217728,262144,262144,268435456,16777216);
    var baselineLimits=new CreatureView4Limits(8388608,4096,65536,196608,1048576,4194304,16777216,16777216);
    void Check(bool value,string why){if(!value)throw new InvalidDataException(why);}
    void Reject(Action action){try{action();}catch(Exception e)when(e is InvalidDataException or InvalidOperationException or ArgumentException or OverflowException or ObjectDisposedException){return;}throw new Exception("Missing rejection");}
    SceneCreateObjectByCrcMessage Create(ulong id)=>new(id,0,0,0,1,0,0,0,0,0);
    object Text(ReplicationByteString s)=>new{Hex=Convert.ToHexString(s.CopyBytes()),Extended=s.UsesExtendedLength};
    object Model(CreatureView4Baseline m)=>new{m.ObjectId,m.DeclaredCount,FloatBits=m.FloatBits.ToArray(),m.ListenTo,
        Encumbrances=new{m.Encumbrances.Counter,Values=m.Encumbrances.Values.ToArray()},
        SkillMods=new{m.SkillMods.Counter,Values=m.SkillMods.Values.Select(s=>new{s.RawCommand,Name=Text(s.Name),s.BaseValue,s.Bonus}).ToArray()},
        SpaceMissionObjects=new{m.SpaceMissionObjects.Counter,Values=m.SpaceMissionObjects.Values.ToArray()}};
    object Row(CreatureView4FullState s)=>new{Model=Model(s.Model),s.Revision,s.DecodedBytes};
    object Event(CreatureView4FullEvent e)=>new{e.Kind,e.Field,e.UpdateOrdinal,e.Opcode,e.Index,e.OldBits,e.NewBits,e.OldValue,e.NewValue,
        Name=e.Name is null?null:Text(e.Name),e.OldBase,e.OldBonus,e.NewBase,e.NewBonus,e.Pair,e.LogicalBytes};
    object? Invoke(SceneCreatureView4FullReplicationStore store,string name,params object[] parameters)
    {
        var method=typeof(SceneCreatureView4FullReplicationStore).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic)!;
        try{return method.Invoke(store,parameters);}catch(TargetInvocationException e)when(e.InnerException is not null){throw e.InnerException;}
    }
    using var fixture=JsonDocument.Parse(File.ReadAllBytes(args[0]));var records=new List<object>();object? lifecycle=null;
    foreach(var c in fixture.RootElement.GetProperty("Cases").EnumerateArray())
    {
        var baseline=CreatureView4Codec.DecodeBaseline(Convert.FromHexString(c.GetProperty("Baseline").GetString()!),baselineLimits);
        var plan=CreatureView4FullCodec.Decode(Convert.FromHexString(c.GetProperty("Hex").GetString()!),messageLimits);
        var registry=new SceneObjectRegistry(4,7);var handle=registry.Create(7,Create(baseline.ObjectId))!.Handle;
        using var store=new SceneCreatureView4FullReplicationStore(registry,storeLimits,stateLimits);
        var initial=store.Seed(handle,baseline)!;string retained=JsonSerializer.Serialize(Row(initial.State));
        var prepared=Invoke(store,"PrepareApply",handle,initial,plan)!;
        Reject(()=>{_=store.Revision;});Invoke(store,"CheckPrepared",prepared);Invoke(store,"ReleasePrepared",prepared);
        Check(store.Revision==1&&ReferenceEquals(store.Find(handle),initial)&&store.PendingEventCount==0,"Aborted preparation does not publish");
        prepared=Invoke(store,"PrepareApply",handle,initial,plan)!;Invoke(store,"CheckPrepared",prepared);
        Invoke(store,"CommitPrepared",prepared);Invoke(store,"ReleasePrepared",prepared);
        Reject(()=>Invoke(store,"CheckPrepared",prepared));
        var current=store.Find(handle)!;var batch=store.DrainEvents();
        Check(store.Revision==2&&current.Revision==2&&current.State.Revision==2&&store.IsCurrent(current),"Complete prepared state current");
        Check(JsonSerializer.Serialize(Row(initial.State))==retained,"Historical model retained");
        Check(store.PendingEventBytes==batch.Events.Sum(e=>e.Event.LogicalBytes)&&store.RetainedStateBytes==current.State.DecodedBytes,"Variable journal accounting");
        Check(batch.Events.All(e=>e.Handle==handle&&e.Incarnation==current.Incarnation&&e.StoreRevision==2),"Exact event identity");
        Check(Enumerable.Range(0,batch.Events.Count).All(i=>store.IsCurrentEvent(batch,i)),"Current event ownership");
        records.Add(new{Name=c.GetProperty("Name").GetString(),Initial=Row(initial.State),Current=Row(current.State),Events=batch.Events.Select(e=>Event(e.Event)),
            StoreRevision=store.Revision,StateBytes=store.RetainedStateBytes,EventBytes=store.PendingEventBytes,PreparedAbortRetained=true});
        if(lifecycle is null)
        {
            using var foreign=new SceneCreatureView4FullReplicationStore(registry,storeLimits,stateLimits);
            Reject(()=>Invoke(foreign,"CommitPrepared",prepared));
            Check(Task.Run(()=>{try{_=store.Revision;return false;}catch(InvalidOperationException){return true;}}).GetAwaiter().GetResult(),"Foreign thread rejected");
            Check(!foreign.Acknowledge(batch)&&foreign.Apply(handle,current,plan) is null,"Foreign ownership rejected");
            Check(registry.EndBaselines(7,new(baseline.ObjectId)),"Finalize registry handle");var nextHandle=registry.Find(baseline.ObjectId)!.Handle;
            Check(!store.IsCurrent(current)&&ReferenceEquals(store.Find(nextHandle),current),"Same incarnation retains state");
            bool historicalCurrent=Enumerable.Range(0,batch.Events.Count).All(i=>store.IsCurrentEvent(batch,i));Check(historicalCurrent,"Historical journal survives same incarnation");
            var scalar=new CreatureView4FullDelta(baseline.ObjectId,7,[new CreatureView4FullUpdate(new CreatureView4Update(0,0x3F800000))]);
            var next=store.Apply(nextHandle,current,scalar)!;Check(next.Revision==3&&next.State.Revision==3,"Same incarnation write");
            Check(!store.Acknowledge(batch)&&store.Apply(nextHandle,current,plan) is null,"Superseded batch/snapshot rejected");
            var currentBatch=store.DrainEvents();Check(store.Acknowledge(currentBatch)&&!store.Acknowledge(currentBatch),"Exact single acknowledgment");
            Check(registry.Destroy(7,new(baseline.ObjectId,0)),"Destroy old incarnation");
            Check(store.Apply(nextHandle,null!,null!) is null,"Stale recipient before plan access");
            var recreated=registry.Create(7,Create(baseline.ObjectId))!.Handle;
            Check(store.Find(recreated) is null&&!store.IsCurrent(next),"Recreation retires old state");
            var reseeded=store.Seed(recreated,baseline)!;Check(reseeded.Revision==4&&reseeded.State.Revision==1,"New incarnation state revision");
            var staleCandidate=Invoke(store,"PrepareApply",recreated,reseeded,plan)!;
            Check(registry.Destroy(7,new(baseline.ObjectId,0)),"Registry mutation while candidate gated");
            Reject(()=>Invoke(store,"CommitPrepared",staleCandidate));Invoke(store,"ReleasePrepared",staleCandidate);
            Check(store.Revision==4,"Stale candidate cannot commit");
            recreated=registry.Create(7,Create(baseline.ObjectId))!.Handle;
            store.Close();store.Close();Check(store.RetainedStateBytes==0&&store.PendingEventBytes==0&&registry.IsCurrent(recreated),"Borrowed registry survives owned close");
            lifecycle=new{HistoricalCurrentAfterFinalize=historicalCurrent,StoreRevision=store.Revision,RecreatedStateRevision=reseeded.State.Revision,ClosedStateBytes=store.RetainedStateBytes,ClosedEventBytes=store.PendingEventBytes,BorrowedRegistryCurrent=registry.IsCurrent(recreated)};
        }
    }
    CreatureView4Baseline Empty(ulong id)=>new(id,14,new uint[10],0,new(0,Array.Empty<int>()),new(0,Array.Empty<CreatureView4SkillMod>()),new(0,Array.Empty<CreatureView4MissionPair>()));
    object Budget(bool mission,long cap)
    {
        var registry=new SceneObjectRegistry(4,9);var h1=registry.Create(9,Create(1))!.Handle;var h2=registry.Create(9,Create(2))!.Handle;
        using var store=new SceneCreatureView4FullReplicationStore(registry,storeLimits with{MaximumOutputBytes=cap},stateLimits);
        var first=store.Seed(h1,Empty(1))!;_=store.Seed(h2,Empty(2));
        var scalar=new CreatureView4FullDelta(1,0,[new CreatureView4FullUpdate(new CreatureView4Update(0,0x3F800000))]);
        var before=store.Apply(h1,first,scalar)!;var batch=store.DrainEvents();
        var update=mission?new CreatureView4FullUpdate(new CreatureMissionObjectsView4Update(13,1,1,[new(1,new CreatureView4MissionPair(ulong.MaxValue,1UL<<63))]))
            :new CreatureView4FullUpdate(new CreatureSkillModsView4Update(3,1,1,[new(0,ReplicationByteString.Create([120],false),1,2)]));
        var delta=new CreatureView4FullDelta(1,0,[update]);bool accepted=cap==(mission?272:274);
        if(accepted)
        {
            var after=store.Apply(h1,before,delta)!;Check(store.RetainedStateBytes+store.PendingEventBytes==cap,"Exact global variable output cap");
            Reject(()=>store.Apply(h1,after,scalar));Check(ReferenceEquals(store.Find(h1),after),"Late aggregate rejection retains state");
        }
        else{Reject(()=>store.Apply(h1,before,delta));Check(ReferenceEquals(store.Find(h1),before)&&ReferenceEquals(store.DrainEvents(),batch)&&store.Revision==3,"One-less cap is atomic");}
        return new{Mission=mission,Cap=cap,Accepted=accepted,StateBytes=store.RetainedStateBytes,EventBytes=store.PendingEventBytes,StoreRevision=store.Revision};
    }
    var budgets=new[]{Budget(false,274),Budget(false,273),Budget(true,272),Budget(true,271)};
    File.WriteAllText(args[1],JsonSerializer.Serialize(new{Records=records,Lifecycle=lifecycle,Budgets=budgets,Synthetic=true,WorldRouting=false}));
    Console.WriteLine($"PASS complete CREO4 prepared-store join sequences={records.Count} variable journal/global caps/incarnation/abort/recreation/close");
}
catch(Exception e){Console.Error.WriteLine(e);Environment.ExitCode=1;}
