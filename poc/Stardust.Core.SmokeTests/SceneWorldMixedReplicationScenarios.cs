using System.Buffers.Binary;
using System.Numerics;
using System.Text;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

internal static class SceneWorldMixedReplicationScenarios
{
    private const ulong Oid=0xF122334455667788;
    private const string Pin="AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
    private static readonly string PathName="object/synthetic/fixture.iff";
    private static readonly ObjectTemplateCatalogEntry Recipe=new(LegacyAssetPath.Checksum(PathName),PathName,Pin,Pin);
    private static readonly ObjectTemplateCatalog Catalog=new(new[]{Recipe});
    private static readonly LogicalAnimationClipSelection Selection=new("appearance/lat/fixture.lat","idle",Pin);
    private static readonly IReadOnlyDictionary<string,LogicalAnimationClipSelection> Bank=new Dictionary<string,LogicalAnimationClipSelection>{{"walk",Selection},{"run",Selection}};
    // Independently authored tiny managed closure. No original bytes and no rendering claim.
    private static SceneCreatureResources Stub(SceneCreatureResourceRequest request)
    {
        const string sat="appearance/fixture.sat", lod="appearance/mesh/fixture.lmg", mesh="appearance/mesh/fixture.mgn", skeletonPath="appearance/skeleton/fixture.skt", clipPath="appearance/animation/fixture.ans", shader="shader/fixture.sht", effect="effect/fixture.eft", texture="texture/fixture.dds";
        var sources=new Dictionary<string,AssetSourceIdentity>();foreach(string path in new[]{PathName,sat,lod,mesh,skeletonPath,Selection.TablePath,clipPath,shader,effect,texture})sources[path]=new(path,0,1,Pin);
        var q=new ScalarFirstQuaternion(1,0,0,0);var skeleton=new SkeletonTemplateDescription(new[]{new BasicSkeletonDescription(new[]{new SkeletonJoint("root",-1,q,q,Vector3.Zero,q,0)})});
        var skin=new SkeletalMeshSkinSourceDescription(Array.Empty<int>(),Array.Empty<short>(),new[]{skeletonPath},new[]{"root"},new[]{Vector3.Zero},new[]{new SkeletalWeightRange(0,1)},new[]{new SkeletalTransformWeight(0,1)},Array.Empty<Vector3>(),Array.Empty<IffNode>());
        var geometry=new SkeletalMeshGeometryDescription(skin,Array.Empty<Vector4>(),new[]{new SkeletalShaderGeometry(shader,new uint[]{0},null,null,null,Array.Empty<SkeletalTextureCoordinates>(),Array.Empty<SkeletalMeshTriangle>(),Array.Empty<SkeletalMeshTriangle>())},Array.Empty<SkeletalTextureRendererReference>(),0,0,0);
        byte[] Chunk(string tag,byte[] payload){byte[] length=new byte[4];BinaryPrimitives.WriteInt32BigEndian(length,payload.Length);return Encoding.ASCII.GetBytes(tag).Concat(length).Concat(payload).ToArray();}
        byte[] Form(string tag,params byte[][] children)=>Chunk("FORM",Encoding.ASCII.GetBytes(tag).Concat(children.SelectMany(c=>c)).ToArray());
        byte[] info=BitConverter.GetBytes(16f).Concat(new short[]{8,0,0,0,0,0}.SelectMany(BitConverter.GetBytes)).ToArray();
        using var stream=new MemoryStream();using(var writer=new BinaryWriter(stream,Encoding.UTF8,true)){writer.Write(1f);writer.Write((short)2);writer.Write((ushort)0);writer.Write(0f);writer.Write(0f);writer.Write(0f);writer.Write((ushort)8);writer.Write(0f);writer.Write(0f);writer.Write(0f);}
        var clip=IffDocument.Parse(Form("CKAT",Form("0001",Chunk("INFO",info),Form("XFRM"),Chunk("LOCT",stream.ToArray())))).Root;
        var metadata=IffDocument.Parse(Form("TEST")).Root;
        var material=new StaticMeshMaterial(new("0000",effect,new[]{new ShaderTextureReference(0,0,"0000",texture,ReadOnlyMemory<byte>.Empty)},metadata),new(new Dictionary<uint,IReadOnlyList<float>>(),new Dictionary<uint,byte>()));
        var materials=new SkeletalMaterialBundle(new Dictionary<string,StaticMeshMaterial>{{shader,material}},new Dictionary<string,DdsImage>{{texture,new(DdsFormat.Rgba32,new[]{new DdsMip(1,1,new byte[]{0,0,0,255})})}},new Dictionary<string,StaticEffectDescription>{{effect,new(false,Array.Empty<StaticEffectImplementation>(),metadata)}},new Dictionary<string,CustomizableShaderPaletteBundle>());
        var appearance=new SkeletalAppearanceDescription(false,new[]{lod},new[]{new SkeletalSkeletonReference(skeletonPath,"")},new[]{new SkeletalAnimationReference(skeletonPath,Selection.TablePath)},Array.Empty<SkeletalDistanceInterval>(),null,null);
        var logical=new LogicalAnimationEntry("idle","idle",null,true);
        var selected=new SelectedLogicalAnimationClip(Selection,new(clipPath,Array.Empty<float>(),null),clip);
        var bundle=new CreatureAppearanceDescriptorBundle(new(PathName,sat,new(0,1,1,0,0,1,1),new Dictionary<string,ResolvedTemplateField>(),sources),appearance,new Dictionary<string,LodMeshGeneratorDescription>{{lod,new(new[]{mesh})}},sources,
            Skeletons:new Dictionary<string,SkeletonTemplateDescription>{{skeletonPath,skeleton}},SkinSources:new Dictionary<string,SkeletalMeshSkinSourceDescription>{{mesh,skin}},Geometries:new Dictionary<string,SkeletalMeshGeometryDescription>{{mesh,geometry}},LogicalTables:new Dictionary<string,LogicalAnimationTableDescription>{{Selection.TablePath,new("",new[]{logical},new[]{logical},logical)}},SelectedAnimation:selected,MaterialBundle:materials);
        var prepared=PreparedSkeletalAnimation.Create(bundle,MissingAnimationTransformPolicy.BindPose,true);
        bundle=bundle with{PreparedAnimation=prepared,AdditionalAnimations=new Dictionary<string,PreparedSkeletalAnimation>{{"walk",prepared},{"run",prepared}}};
        return new(request.Snapshot.Create with{},Recipe,bundle,sources.Count,sources.Count);
    }
    private static void Check(bool value,string message) { if(!value)throw new Exception(message); }
    private static void Reject<T>(Action action) where T:Exception { try { action(); } catch(T) { return; } throw new Exception("Expected "+typeof(T).Name); }
    private static object State(SceneWorldReplicationSnapshot r)=>new {ObjectId=r.Incarnation.ObjectId,r.Revision,r.Scalars.Posture,r.Scalars.Rank,r.Scalars.LinkId,HeightBits=BitConverter.SingleToUInt32Bits(r.Scalars.Height),r.Scalars.Shock,r.Scalars.States,WoundsCounter=r.Wounds!.Counter,Wounds=r.Wounds.Values};
    internal static void Run(IReadOnlyDictionary<string,string> peer,string exportPath)
    {
        byte[] Bytes(string name)=>Convert.FromHexString(peer[name]);
        byte[] ObjectBytes(string name,ulong id) {var bytes=Bytes(name);BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(6),id);return bytes;}
        var limits=new CreatureView3MixedDeltaLimits(1024,32,24,64,64,16);
        var baselineLimits=new CreatureView3BaselineLimits(1024,64,64,16,512);
        var legacy=CreatureWoundsApplicationPolicy.LegacyBounded;
        SceneWorldCoordinator World(long bytes=512,CreatureWoundsApplicationPolicy policy=CreatureWoundsApplicationPolicy.LegacyBounded,SceneWorldChangePolicy? change=null,Func<SceneCreatureResourceRequest,CancellationToken,Task<SceneCreatureResources>>? loader=null)=>
            new(Catalog,Selection,Bank,loader??((q,t)=>Task.FromResult(Stub(q))),SceneCreateZeroQuaternionPolicy.Reject,4,1,16,8192,1,25,1024,new(SceneWorldReplicationMode.CreatureView3Mixed,baselineLimits,16,bytes,limits,policy),change);
        long Stage(SceneWorldCoordinator w,string? delta=null,bool seed=true)
        {long g=w.BeginSelection(Oid).Generation;w.Accept(g,Bytes("start"));w.Accept(g,Bytes("create"));if(seed)w.Accept(g,Bytes("baseline"));if(delta is not null)w.Accept(g,Bytes(delta));w.Accept(g,Bytes("end"));return g;}
        var baseline=CreatureView3BaselineCodec.Decode(Bytes("baseline"),baselineLimits);
        CreatureView3MixedDeltaPlan Plan(string n,CreatureWoundsApplicationPolicy policy=CreatureWoundsApplicationPolicy.LegacyBounded)=>CreatureView3MixedDeltaCodec.Decode(Bytes(n),limits,policy);
        var registry=new SceneObjectRegistry(3,1);var create=SceneObjectLifecycleCodec.DecodeCreate(Bytes("create"));var inc=registry.Create(1,create)!.Incarnation;
        using(var store=new SceneCreatureReplicationStore(registry,2,512,mixedPolicy:new(legacy,16)))
        {
            Reject<InvalidOperationException>(()=>store.Apply(inc,Plan("delta")));
            var seed=store.Seed(inc,baseline)!;Check(seed.Wounds!.Counter==0&&store.RetainedDecodedBytes==147,"Owned mixed seed logical charge");
            Reject<InvalidDataException>(()=>store.Apply(inc,Plan("strict",CreatureWoundsApplicationPolicy.StrictInterval)));
            var different=CreatureView3MixedDeltaCodec.Decode(Bytes("delta"),limits with {MaximumWoundsElements=15},legacy);
            Reject<InvalidDataException>(()=>store.Apply(inc,different));
            var applied=store.Apply(inc,Plan("delta"))!;Check(applied.Revision==2&&applied.Wounds!.Values.SequenceEqual(new[]{-1,7})&&applied.Wounds.Counter==6,"Ordered mixed commit once");
            Check(seed.Wounds!.Values.SequenceEqual(new[]{-2,int.MaxValue,-2})&&applied.Baseline==baseline&&store.RetainedDecodedBytes==143,"Baseline and old snapshot immutable");
            Reject<NotSupportedException>(()=>((IList<int>)applied.Wounds!.Values)[0]=0);
            store.Seed(inc,baseline);var before=store.Find(inc)!;Reject<InvalidDataException>(()=>store.Apply(inc,Plan("late")));
            Check(store.Find(inc)==before&&store.IsCurrent(before)&&store.RetainedDecodedBytes==147,"Late mixed failure no scalar/wounds prefix or revision");
            var foreign=new SceneObjectRegistry(1,1).Create(1,create)!.Incarnation;Check(store.Apply(foreign,Plan("delta")) is null,"Foreign incarnation ignored");
            byte[] wrong=Bytes("delta");BinaryPrimitives.WriteUInt64LittleEndian(wrong.AsSpan(6),Oid+1);Reject<InvalidDataException>(()=>store.Apply(inc,CreatureView3MixedDeltaCodec.Decode(wrong,limits,legacy)));
            registry.Contain(1,new(Oid,0,42));registry.EndBaselines(1,new(Oid));Check(store.IsCurrent(before),"Lifecycle handle changes preserve mixed incarnation");
            Exception? error=null;var thread=new Thread(()=>{try{store.Apply(inc,Plan("delta"));}catch(Exception e){error=e;}});thread.Start();thread.Join();Check(error is InvalidOperationException,"Mixed foreign-thread mutation rejected");
            var child=registry.Create(1,create with {ObjectId=Oid+1})!;var childBaseline=CreatureView3BaselineCodec.Decode(ObjectBytes("baseline",Oid+1),baselineLimits);var childSeed=store.Seed(child.Incarnation,childBaseline)!;
            registry.Contain(1,new(Oid+1,Oid,42));registry.Replace(1,create);
            Check(store.IsCurrent(childSeed)&&registry.Find(Oid+1)!.ContainerId==0&&!store.IsCurrent(before)&&store.RetainedDecodedBytes==147&&store.Apply(inc,Plan("delta")) is null,"Parent replacement retires parent charge and preserves detached child mixed incarnation");
            registry.Destroy(1,new(Oid+1,255));
            registry.Destroy(1,new(Oid,255));Check(store.Apply(inc,Plan("delta")) is null&&store.RetainedDecodedBytes==0,"Destroyed incarnation retires mixed bytes");
            inc=registry.Create(1,create)!.Incarnation;Reject<InvalidOperationException>(()=>store.Apply(inc,Plan("delta")));store.Seed(inc,baseline);
            registry.Reset(2);Check(store.RetainedDecodedBytes==0,"Reset retires mixed state");inc=registry.Create(2,create)!.Incarnation;
        }
        using(var bounded=new SceneCreatureReplicationStore(registry,2,160,mixedPolicy:new(legacy,16)))
        {bounded.Seed(inc,baseline);var current=bounded.Apply(inc,Plan("delta"))!;Reject<InvalidDataException>(()=>bounded.Apply(inc,Plan("grow")));Check(bounded.Find(inc)==current&&bounded.RetainedDecodedBytes==143,"Candidate growth aggregate-byte rollback");}
        using(var aggregate=new SceneCreatureReplicationStore(registry,2,280,mixedPolicy:new(legacy,16)))
        {var aggregateHeld=aggregate.Seed(inc,baseline)!;var second=registry.Create(2,create with {ObjectId=Oid+1})!;var raw=ObjectBytes("baseline",Oid+1);var other=CreatureView3BaselineCodec.Decode(raw,baselineLimits);Reject<InvalidDataException>(()=>aggregate.Seed(second.Incarnation,other));Check(aggregate.Find(inc)==aggregateHeld&&aggregate.RetainedDecodedBytes==147,"Aggregate mixed seeds rollback");registry.Destroy(2,new(Oid+1,255));}
        using(var capacity=new SceneCreatureReplicationStore(registry,1,512,mixedPolicy:new(legacy,16)))
        {capacity.Seed(inc,baseline);var second=registry.Create(2,create with {ObjectId=Oid+1})!;var other=CreatureView3BaselineCodec.Decode(ObjectBytes("baseline",Oid+1),baselineLimits);Reject<InvalidDataException>(()=>capacity.Seed(second.Incarnation,other));Check(capacity.Snapshots().Count==1,"Mixed store object capacity");registry.Destroy(2,new(Oid+1,255));}
        using(var seedBytes=new SceneCreatureReplicationStore(registry,2,146,mixedPolicy:new(legacy,16)))
        {Reject<InvalidDataException>(()=>seedBytes.Seed(inc,baseline));Check(seedBytes.RetainedDecodedBytes==0,"Seed current-wounds charge checked");}
        using(var elements=new SceneCreatureReplicationStore(registry,2,512,mixedPolicy:new(legacy,2)))
        {Reject<InvalidDataException>(()=>elements.Seed(inc,baseline));Check(elements.Snapshots().Count==0,"Seed element bound checked");}
        using(var overflow=new SceneCreatureReplicationStore(registry,2,512,ulong.MaxValue-1,new(legacy,16)))
        {var last=overflow.Seed(inc,baseline)!;Reject<OverflowException>(()=>overflow.Apply(inc,Plan("delta")));Check(overflow.Find(inc)==last&&overflow.RetainedDecodedBytes==147,"Mixed local revision overflow atomic");overflow.Close();}
        using(var scalar=new SceneCreatureReplicationStore(registry))
        {var seed=scalar.Seed(inc,baseline)!;Check(seed.Wounds is null&&scalar.RetainedDecodedBytes==119,"Default scalar API no current wounds");Reject<InvalidOperationException>(()=>scalar.Apply(inc,Plan("delta")));}
        int loads=0;using var world=World(loader:(q,t)=>{loads++;return Task.FromResult(Stub(q));});var transport=new SoeClientTransport();var delivered=new List<string>();
        var selection=world.BeginSelection(Oid);long generation=selection.Generation;
        string request=Convert.ToHexString(transport.BeginSession(0x12345678,496));transport.AcceptSessionResponse(Bytes("response"));
        string select=Convert.ToHexString(transport.CreateReliableDatagram(selection.CopySelectMessage(),0));Check(select==peer["expectedSelect"],"Independent select bytes");
        void Receive(string n) {byte[] wire=Bytes(n);Check(wire.Length<=496,"Negotiated frame bound");foreach(var payload in transport.AcceptDatagram(wire).ApplicationPayloads){delivered.Add(Convert.ToHexString(payload));world.Accept(generation,payload);Array.Clear(payload);}Array.Clear(wire);}
        Receive("ackSelect");Receive("peer_create");Receive("peer_start");Receive("peer_start");
        foreach(string n in new[]{"baseline","delta","end","contain"})Receive("peer_"+n);
        Check(world.DrainChanges(generation) is null&&loads==0,"Staged complete mixed decode no publication");
        string ready=Convert.ToHexString(transport.CreateReliableDatagram(world.CompleteMinimumSceneBarrier(generation,true)!,100));Check(ready==peer["expectedReady"],"Ready after replay");
        var first=world.DrainChanges(generation)!;var firstState=world.FindReplication(generation,Oid)!;
        Check(first.MutationRevision==5&&firstState.Revision==2&&first.Entries.Count==1&&first.Entries[0].Kind==(SceneWorldChangeKind.Lifecycle|SceneWorldChangeKind.Replication),"One mutation per mixed application and coalesced latest state");
        Check(world.RetainedReplicationDecodedBytes==143&&firstState.Wounds!.Counter==6,"Mixed initial final charge");
        world.PollResources();var resource=world.PollResources().Prepared.Single();Check(world.IsCurrent(resource)&&loads==1,"Synthetic resource publication");world.AcknowledgeChanges(first);
        world.Accept(generation,Bytes("grow"));var grown=world.FindReplication(generation,Oid)!;var grownBatch=world.DrainChanges(generation)!;
        Check(grown.Revision==3&&grown.Wounds!.Values.SequenceEqual(new[]{-1,7,0,0,0,0,9})&&world.RetainedReplicationDecodedBytes==163,"Legacy replace zero-fill growth");
        world.Accept(generation,Bytes("noop"));var noop=world.FindReplication(generation,Oid)!;var noopBatch=world.DrainChanges(generation)!;
        Check(noop.Revision==4&&noop.Wounds!.Counter==7&&noop.Wounds.Values.SequenceEqual(grown.Wounds!.Values)&&noopBatch.MutationRevision==7&&!world.IsCurrent(grownBatch),"Wounds counter no-op still one completed mixed mutation");
        Check(world.IsCurrent(resource)&&loads==1&&!world.IsCurrent(firstState)&&world.IsCurrent(noop),"Resource and current-state identity independent");
        for(int i=0;i<1000;i++){world.Accept(generation,Bytes("baseline"));world.Accept(generation,Bytes("delta"));}
        Check(world.RetainedReplicationDecodedBytes==143&&world.DrainChanges(generation)!.Entries.Count==1&&loads==1&&world.IsCurrent(resource),"Long mixed stream bounded state/change metadata and no reload");
        world.Accept(generation,Bytes("grow"));world.Accept(generation,Bytes("noop"));
        world.Accept(generation,Bytes("clear"));Check(world.RetainedReplicationDecodedBytes==135&&world.FindReplication(generation,Oid)!.Wounds!.Values.Count==0,"Clear current charge only; full baseline unchanged");
        world.Accept(generation,Bytes("baseline"));Check(world.RetainedReplicationDecodedBytes==147&&world.FindReplication(generation,Oid)!.Wounds!.Counter==0,"Reseed resets both overlays");
        var held=world.DrainChanges(generation)!;Reject<InvalidDataException>(()=>world.Accept(generation,Bytes("late")));
        Check(world.State==ZoneSceneTransitionState.Failed&&world.DrainChanges(generation) is null&&!world.IsCurrent(held)&&world.Snapshots().Count==0&&world.RetainedReplicationDecodedBytes==0&&!world.IsCurrent(resource),"Late semantic failure clears whole generation and no batch/prefix");
        Check(world.DrainCompletion.IsCompleted,"Failed generation drained completed resource");transport.Close();
        using(var strict=World(policy:CreatureWoundsApplicationPolicy.StrictInterval))
        {long g=Stage(strict,"strict");strict.CompleteMinimumSceneBarrier(g,true);Check(strict.FindReplication(g,Oid)!.Wounds!.Counter==3&&strict.FindReplication(g,Oid)!.Wounds!.Values.SequenceEqual(new[]{-1,7}),"Explicit strict alternative");}
        foreach(string bad in new[]{"late","truncated"})using(var failure=World())
        {long g=Stage(failure);failure.CompleteMinimumSceneBarrier(g,true);Reject<InvalidDataException>(()=>failure.Accept(g,Bytes(bad)));Check(failure.DrainChanges(g) is null&&failure.RetainedReplicationDecodedBytes==0,"Late decode/apply failure no delivery");}
        using(var replay=World())
        {long g=Stage(replay,"late");Reject<InvalidDataException>(()=>replay.CompleteMinimumSceneBarrier(g,true));Check(replay.DrainChanges(g) is null&&replay.Snapshots().Count==0&&replay.RetainedReplicationDecodedBytes==0,"Late replay failure publishes no partial batch/ready");}
        using(var parsing=World())
        {long g=Stage(parsing);Reject<InvalidDataException>(()=>parsing.Accept(g,Bytes("truncated")));Check(parsing.StagedCount==0&&parsing.DrainChanges(g) is null,"Complete decode before staging rejects malformed tail");}
        using(var wrongPolicy=World(policy:CreatureWoundsApplicationPolicy.StrictInterval))
        {long g=Stage(wrongPolicy);Reject<InvalidDataException>(()=>wrongPolicy.Accept(g,Bytes("delta")));Check(wrongPolicy.DrainChanges(g) is null,"Legacy assign grammar not autodetected by strict world");}
        using(var unseeded=World())
        {long g=Stage(unseeded,"delta",false);Reject<InvalidDataException>(()=>unseeded.CompleteMinimumSceneBarrier(g,true));Check(unseeded.DrainChanges(g) is null&&unseeded.Snapshots().Count==0,"No default mixed baseline on replay");}
        using(var bytes=World(160))
        {long g=Stage(bytes,"delta");bytes.CompleteMinimumSceneBarrier(g,true);Reject<InvalidDataException>(()=>bytes.Accept(g,Bytes("grow")));Check(bytes.DrainChanges(g) is null&&bytes.RetainedReplicationDecodedBytes==0,"World candidate budget failure clears generation");}
        using(var capacity=World(change:new(1)))
        {long g=Stage(capacity);capacity.CompleteMinimumSceneBarrier(g,true);Reject<InvalidDataException>(()=>capacity.Accept(g,ObjectBytes("create",Oid+1)));Check(capacity.DrainChanges(g) is null,"Mixed observer capacity failure clears generation");}
        using(var overflow=World(change:new(2,ulong.MaxValue-4)))
        {long g=Stage(overflow);overflow.CompleteMinimumSceneBarrier(g,true);overflow.Accept(g,Bytes("delta"));Check(overflow.DrainChanges(g)!.MutationRevision==ulong.MaxValue,"Last mixed world revision");Reject<OverflowException>(()=>overflow.Accept(g,Bytes("noop")));Check(overflow.DrainChanges(g) is null&&overflow.RetainedReplicationDecodedBytes==0,"Mixed observer overflow clears generation");}
        using(var lifecycle=World())
        {long g=Stage(lifecycle,"delta");lifecycle.CompleteMinimumSceneBarrier(g,true);var state=lifecycle.FindReplication(g,Oid)!;lifecycle.Accept(g,Bytes("contain"));Check(lifecycle.IsCurrent(state),"Contain preserves mixed state");lifecycle.Accept(g,Bytes("destroy"));Check(!lifecycle.IsCurrent(state)&&lifecycle.RetainedReplicationDecodedBytes==0,"Destroy retires state");lifecycle.Accept(g,Bytes("create"));Reject<InvalidDataException>(()=>lifecycle.Accept(g,Bytes("delta")));Check(lifecycle.DrainChanges(g) is null,"Same ID recreation cannot inherit mixed state");}
        SceneWorldCoordinator? reentrant=null;reentrant=World(loader:(q,t)=>{Reject<InvalidOperationException>(()=>reentrant!.DrainChanges(reentrant.Generation));return Task.FromResult(Stub(q));});
        using(reentrant){long g=Stage(reentrant,"delta");reentrant.CompleteMinimumSceneBarrier(g,true);reentrant.PollResources();Check(reentrant.PollResources().Prepared.Count==1,"Mixed resource loader reentrancy rejected");Exception? error=null;var thread=new Thread(()=>{try{reentrant.FindReplication(g,Oid);}catch(Exception e){error=e;}});thread.Start();thread.Join();Check(error is InvalidOperationException,"Mixed world foreign thread rejected");reentrant.Close();Check(reentrant.DrainChanges(g) is null,"Close clears mixed delivery");}
        using(var cancelled=World(loader:(q,t)=>{pendingToken=t;return pendingTask.Task;}))
        {long g=Stage(cancelled,"delta");cancelled.CompleteMinimumSceneBarrier(g,true);cancelled.PollResources();Reject<InvalidDataException>(()=>cancelled.Accept(g,Bytes("truncated")));Check(pendingToken.IsCancellationRequested&&cancelled.OutstandingJobs==1,"Failure cancels outstanding mixed-world load");pendingTask.SetException(new Exception("synthetic late fault"));Check(cancelled.PollResources().Retired.Single().Error is not null&&cancelled.OutstandingJobs==0,"Late failure observed and drained");}
        Reject<ArgumentNullException>(()=>new SceneWorldCoordinator(Catalog,Selection,Bank,(q,t)=>Task.FromResult(Stub(q)),SceneCreateZeroQuaternionPolicy.Reject,replicationPolicy:new(SceneWorldReplicationMode.CreatureView3Mixed,baselineLimits,16,512)));
        Reject<ArgumentOutOfRangeException>(()=>new SceneCreatureReplicationStore(registry,mixedPolicy:new((CreatureWoundsApplicationPolicy)99,16)));
        File.WriteAllText(exportPath,JsonSerializer.Serialize(new {NegotiatedBytes=496,SessionRequest=request,SelectDatagram=select,ReadyDatagram=ready,DeliveredApplications=delivered,First=State(firstState),Grown=State(grown),Noop=State(noop),FirstMutation=first.MutationRevision,GrownMutation=grownBatch.MutationRevision,NoopMutation=noopBatch.MutationRevision,ResourceLoads=loads,FinalBytes=world.RetainedReplicationDecodedBytes},new JsonSerializerOptions {WriteIndented=true})+"\n");
        Console.WriteLine("PASS: atomic mixed store/world; legacy/strict,496-byte peer, ordered scalar/wounds state, byte/element/policy bounds, rollback, lifecycle, revision/capacity, resource separation and cleanup");
    }
    private static readonly TaskCompletionSource<SceneCreatureResources> pendingTask=new();
    private static CancellationToken pendingToken;
}
