using System.Buffers.Binary;
using System.Numerics;
using System.Text;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

internal static class SceneWorldComponentsReplicationScenarios
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
    private static void Check(bool value,string why) {if(!value)throw new Exception(why);}
    private static void Reject<T>(Action action) where T:Exception {try{action();}catch(T){return;}throw new Exception("Expected "+typeof(T).Name);}
    private sealed record Case(string Name,string Baseline,string Wire,int Elements,int KeyBudget);
    private static object View(SceneWorldReplicationSnapshot r)=>new {r.Revision,r.Scalars.Posture,r.Scalars.Rank,r.Scalars.LinkId,HeightBits=BitConverter.SingleToUInt32Bits(r.Scalars.Height),r.Scalars.Shock,r.Scalars.States,WoundsCounter=r.Wounds!.Counter,Wounds=r.Wounds.Values,ComponentCounter=r.Components!.Counter,Components=r.Components.Keys};
    internal static void Run(IReadOnlyDictionary<string,string> peer,string exportPath)
    {
        byte[] Bytes(string n)=>Convert.FromHexString(peer[n]);
        byte[] ObjectBytes(string n,ulong id){var b=Bytes(n);BinaryPrimitives.WriteUInt64LittleEndian(b.AsSpan(6),id);return b;}
        var bl=new CreatureView3BaselineLimits(1024,64,64,16,512);var ml=new CreatureView3MixedDeltaLimits(1024,32,24,64,64,16);
        var cl=new CreatureView3ComponentsDeltaLimits(1024,4,32,32,8);var cp=new SceneCreatureComponentsReplicationPolicy(16,8);
        CreatureView3ComponentsDeltaPlan Plan(string n)=>CreatureView3ComponentsDeltaCodec.Decode(Bytes(n),cl);
        var baseline=CreatureView3BaselineCodec.Decode(Bytes("baseline"),bl);var create=SceneObjectLifecycleCodec.DecodeCreate(Bytes("create"));
        var cases=new List<object>();
        foreach(var test in JsonSerializer.Deserialize<Case[]>(peer["componentCases"])!)
        {
            var registry=new SceneObjectRegistry(1,1);var inc=registry.Create(1,create)!.Incarnation;
            using var store=new SceneCreatureReplicationStore(registry,1,1024,componentsPolicy:new(16,test.Elements));
            var seed=store.Seed(inc,CreatureView3BaselineCodec.Decode(Convert.FromHexString(test.Baseline),bl))!;bool decoded=false,accepted=false;
            try{var plan=CreatureView3ComponentsDeltaCodec.Decode(Convert.FromHexString(test.Wire),cl with {MaximumElements=test.Elements,MaximumKeys=test.KeyBudget});decoded=true;store.Apply(inc,plan);accepted=true;}catch(InvalidDataException){Check(store.Find(inc)==seed&&store.IsCurrent(seed),"Case rollback "+test.Name);}
            var state=store.Find(inc)!;cases.Add(new {test.Name,Decoded=decoded,Accepted=accepted,Counter=state.Components!.Counter,state.Components.Keys,state.Revision});
        }
        var reg=new SceneObjectRegistry(4,1);var token=reg.Create(1,create)!.Incarnation;
        using(var store=new SceneCreatureReplicationStore(reg,2,512,mixedPolicy:new(CreatureWoundsApplicationPolicy.LegacyBounded,16),componentsPolicy:cp))
        {
            Reject<InvalidOperationException>(()=>store.Apply(token,Plan("components")));var seed=store.Seed(token,baseline)!;
            Check(seed.Components!.Keys.SequenceEqual(new[]{int.MinValue,-1})&&seed.Components.Counter==0&&store.RetainedDecodedBytes==171,"Raw duplicate normalization and additive charge");
            Check(seed.Baseline.Components.Values.SequenceEqual(new[]{-1,int.MinValue,-1})&&CreatureView3BaselineCodec.Encode(seed.Baseline,bl).SequenceEqual(Bytes("baseline")),"Complete raw duplicate/order/count/counter baseline preserved");
            var current=store.Apply(token,Plan("components"))!;Check(current.Revision==2&&current.Components!.Keys.SequenceEqual(new[]{int.MinValue,int.MaxValue})&&current.Components.Counter==7,"Standalone plan one commit");
            Reject<NotSupportedException>(()=>((IList<int>)current.Components!.Keys)[0]=0);
            var mixed=CreatureView3MixedDeltaCodec.Decode(Bytes("delta"),ml,CreatureWoundsApplicationPolicy.LegacyBounded);var afterMixed=store.Apply(token,mixed)!;
            Check(afterMixed.Components==current.Components&&afterMixed.Wounds!.Counter==6&&store.RetainedDecodedBytes==167,"Mixed plan preserves components with independent charge");
            var scalar=CreatureView3ScalarDeltaCodec.Decode(Bytes("scalar"),1024,16);var afterScalar=store.Apply(token,scalar)!;Check(afterScalar.Components==current.Components&&afterScalar.Wounds==afterMixed.Wounds,"Existing scalar overload preserves both optional overlays");
            store.Seed(token,baseline);store.Apply(token,Plan("componentClear"));Check(store.RetainedDecodedBytes==163&&store.Find(token)!.Components!.Keys.Count==0,"Empty current components retain fixed16 plus complete raw baseline/wounds charge");
            store.Seed(token,baseline);var before=store.Find(token)!;Reject<InvalidDataException>(()=>store.Apply(token,Plan("lateGrowth")));Check(store.Find(token)==before&&store.RetainedDecodedBytes==171,"Late valid-prefix then element failure atomic");
            var mismatch=CreatureView3ComponentsDeltaCodec.Decode(Bytes("components"),cl with {MaximumElements=7});Reject<InvalidDataException>(()=>store.Apply(token,mismatch));
            var foreign=new SceneObjectRegistry(1,1).Create(1,create)!.Incarnation;Check(store.Apply(foreign,Plan("components")) is null,"Foreign token ignored");
            var wrong=CreatureView3ComponentsDeltaCodec.Decode(ObjectBytes("components",Oid+1),cl);Reject<InvalidDataException>(()=>store.Apply(token,wrong));
            reg.Contain(1,new(Oid,0,42));reg.EndBaselines(1,new(Oid));Check(store.IsCurrent(before),"Contain/end preserve optional overlays");
            var child=reg.Create(1,create with {ObjectId=Oid+1})!;var childSeed=store.Seed(child.Incarnation,CreatureView3BaselineCodec.Decode(ObjectBytes("baseline",Oid+1),bl))!;reg.Contain(1,new(Oid+1,Oid,1));reg.Replace(1,create);
            Check(store.IsCurrent(childSeed)&&reg.Find(Oid+1)!.ContainerId==0&&!store.IsCurrent(before)&&store.RetainedDecodedBytes==171&&store.Apply(token,Plan("components")) is null,"Parent replace retires state; detached child retains components");
            reg.Destroy(1,new(Oid+1,255));token=reg.Find(Oid)!.Incarnation;Reject<InvalidOperationException>(()=>store.Apply(token,Plan("components")));store.Seed(token,baseline);
            Exception? error=null;var thread=new Thread(()=>{try{store.Apply(token,Plan("components"));}catch(Exception e){error=e;}});thread.Start();thread.Join();Check(error is InvalidOperationException,"Foreign thread rejected");
            reg.Reset(2);Check(store.RetainedDecodedBytes==0&&store.Apply(token,Plan("components")) is null,"Reset retires all charges");token=reg.Create(2,create)!.Incarnation;
            store.Close();Reject<ObjectDisposedException>(()=>store.Apply(token,Plan("components")));
        }
        using(var aggregate=new SceneCreatureReplicationStore(reg,2,280,componentsPolicy:cp))
        {var held=aggregate.Seed(token,baseline)!;var other=reg.Create(2,create with {ObjectId=Oid+1})!;Reject<InvalidDataException>(()=>aggregate.Seed(other.Incarnation,CreatureView3BaselineCodec.Decode(ObjectBytes("baseline",Oid+1),bl)));Check(aggregate.Find(token)==held&&aggregate.RetainedDecodedBytes==143,"Aggregate normalized-state seeds rollback");reg.Destroy(2,new(Oid+1,255));}
        using(var capacity=new SceneCreatureReplicationStore(reg,1,512,componentsPolicy:cp))
        {var held=capacity.Seed(token,baseline)!;var other=reg.Create(2,create with {ObjectId=Oid+1})!;Reject<InvalidDataException>(()=>capacity.Seed(other.Incarnation,CreatureView3BaselineCodec.Decode(ObjectBytes("baseline",Oid+1),bl)));Check(capacity.Find(token)==held&&capacity.Snapshots().Count==1,"Component store object capacity rollback");reg.Destroy(2,new(Oid+1,255));}
        using(var plain=new SceneCreatureReplicationStore(reg))
        {var seed=plain.Seed(token,baseline)!;Check(seed.Components is null&&seed.Wounds is null&&plain.RetainedDecodedBytes==119,"Default scalar charge unchanged");Reject<InvalidOperationException>(()=>plain.Apply(token,Plan("components")));}
        using(var mixedOnly=new SceneCreatureReplicationStore(reg,mixedPolicy:new(CreatureWoundsApplicationPolicy.LegacyBounded,16)))
        {Check(mixedOnly.Seed(token,baseline)!.Components is null&&mixedOnly.RetainedDecodedBytes==147,"Old mixed charge unchanged");}
        foreach(var policy in new[]{new SceneCreatureComponentsReplicationPolicy(2,8),new SceneCreatureComponentsReplicationPolicy(16,1)})using(var bounded=new SceneCreatureReplicationStore(reg,componentsPolicy:policy))
        {Reject<InvalidDataException>(()=>bounded.Seed(token,baseline));Check(bounded.Snapshots().Count==0,"Raw and normalized seed limits independent");}
        Reject<InvalidDataException>(()=>CreatureView3BaselineCodec.Decode(Bytes("baseline"),bl with {MaximumContainerElements=2}));
        using(var boundary=new SceneCreatureReplicationStore(reg,1,143,componentsPolicy:cp))
        {var held=boundary.Seed(token,baseline)!;Check(boundary.RetainedDecodedBytes==143,"Exact components-only seed budget");var wire=Bytes("byteGrowth");Reject<InvalidDataException>(()=>boundary.Apply(token,CreatureView3ComponentsDeltaCodec.Decode(wire,cl)));Check(boundary.Find(token)==held&&boundary.RetainedDecodedBytes==143,"Late candidate aggregate/growth budget rollback");}
        using(var exact=new SceneCreatureReplicationStore(reg,1,143,componentsPolicy:new(3,2)))
        {var state=exact.Seed(token,baseline)!;Check(state.Components!.Keys.Count==2,"Exact raw3 and normalized2 independent seed boundaries");}
        using(var seedBudget=new SceneCreatureReplicationStore(reg,1,142,componentsPolicy:cp)){Reject<InvalidDataException>(()=>seedBudget.Seed(token,baseline));Check(seedBudget.RetainedDecodedBytes==0,"One byte under seed budget");}
        using(var overflow=new SceneCreatureReplicationStore(reg,1,512,ulong.MaxValue-1,componentsPolicy:cp))
        {var held=overflow.Seed(token,baseline)!;Reject<OverflowException>(()=>overflow.Apply(token,Plan("components")));Check(overflow.Find(token)==held&&overflow.RetainedDecodedBytes==143,"Local revision exhaustion rollback");overflow.Close();}
        SceneWorldCoordinator World(long bytes=512,SceneWorldChangePolicy? change=null,Func<SceneCreatureResourceRequest,CancellationToken,Task<SceneCreatureResources>>? load=null)=>
            new(Catalog,Selection,Bank,load??((q,t)=>Task.FromResult(Stub(q))),SceneCreateZeroQuaternionPolicy.Reject,4,1,32,8192,1,25,1024,new(SceneWorldReplicationMode.CreatureView3MixedComponents,bl,16,bytes,ml,CreatureWoundsApplicationPolicy.LegacyBounded,cl with {MaximumFields=ml.MaximumFields},cp),change);
        long Stage(SceneWorldCoordinator w,string? delta=null,bool seed=true)
        {long g=w.BeginSelection(Oid).Generation;w.Accept(g,Bytes("start"));w.Accept(g,Bytes("create"));if(seed)w.Accept(g,Bytes("baseline"));if(delta is not null)w.Accept(g,Bytes(delta));w.Accept(g,Bytes("end"));return g;}
        int loads=0;using var world=World(load:(q,t)=>{loads++;return Task.FromResult(Stub(q));});var transport=new SoeClientTransport();var selection=world.BeginSelection(Oid);long generation=selection.Generation;
        string request=Convert.ToHexString(transport.BeginSession(0x12345678,496));transport.AcceptSessionResponse(Bytes("response"));string select=Convert.ToHexString(transport.CreateReliableDatagram(selection.CopySelectMessage(),0));
        Check(select==peer["expectedSelect"],"Independent select");var delivered=new List<string>();
        void Receive(string n){byte[] wire=Bytes(n);Check(wire.Length<=496,"Peer bound");foreach(byte[] payload in transport.AcceptDatagram(wire).ApplicationPayloads){delivered.Add(Convert.ToHexString(payload));world.Accept(generation,payload);Array.Clear(payload);}Array.Clear(wire);}
        Receive("ackSelect");Receive("peer_create");Receive("peer_start");Receive("peer_start");foreach(string n in new[]{"baseline","components","delta","end","contain"})Receive("peer_"+n);
        Check(world.DrainChanges(generation) is null&&loads==0,"No staged observer/resources");string ready=Convert.ToHexString(transport.CreateReliableDatagram(world.CompleteMinimumSceneBarrier(generation,true)!,100));Check(ready==peer["expectedReady"],"Ready only after complete replay");
        var first=world.FindReplication(generation,Oid)!;var firstBatch=world.DrainChanges(generation)!;
        Check(first.Revision==3&&firstBatch.MutationRevision==6&&firstBatch.Entries.Single().Kind==(SceneWorldChangeKind.Lifecycle|SceneWorldChangeKind.Replication)&&world.RetainedReplicationDecodedBytes==167,"One component plus one mixed mutation; coalesced latest view");
        world.PollResources();var resource=world.PollResources().Prepared.Single();world.AcknowledgeChanges(firstBatch);
        world.Accept(generation,Bytes("scalar"));var scalarState=world.FindReplication(generation,Oid)!;var scalarBatch=world.DrainChanges(generation)!;world.Accept(generation,Bytes("componentNoop"));var noop=world.FindReplication(generation,Oid)!;var noopBatch=world.DrainChanges(generation)!;
        Check(!world.IsCurrent(scalarBatch)&&!world.AcknowledgeChanges(scalarBatch)&&world.IsCurrent(noopBatch)&&noop.Components!.Counter==7&&noop.Revision==5&&noopBatch.MutationRevision==8,"No-op whole plan revision and stale ack retains slot");
        world.Accept(generation,Bytes("future"));var future=world.FindReplication(generation,Oid)!;Check(future.Components!.Counter==50&&future.Components.Keys.SequenceEqual(noop.Components!.Keys),"Zero commands still incoming future counter maximum");
        Check(world.IsCurrent(resource)&&loads==1&&!world.IsCurrent(first)&&world.IsCurrent(future),"Resource/state freshness independent; no field5 reload");
        for(int i=0;i<1000;i++){world.Accept(generation,Bytes("baseline"));world.Accept(generation,Bytes("components"));world.Accept(generation,Bytes("delta"));}
        var stream=world.FindReplication(generation,Oid)!;var streamBatch=world.DrainChanges(generation)!;Check(streamBatch.Entries.Count==1&&world.RetainedReplicationDecodedBytes==167&&loads==1&&world.IsCurrent(resource),"1000 sequential streams one bounded state/slot no history or reload");
        world.AcknowledgeChanges(streamBatch);world.Accept(generation,Bytes("baseline"));var reseed=world.FindReplication(generation,Oid)!;Check(reseed.Components!.Counter==0&&reseed.Components.Keys.SequenceEqual(new[]{int.MinValue,-1})&&reseed.Wounds!.Counter==0&&world.RetainedReplicationDecodedBytes==171,"Reseed resets all optional overlays, preserves full raw seed");
        world.Accept(generation,Bytes("cross5first"));var crossFirst=world.FindReplication(generation,Oid)!;
        Check(crossFirst.Revision==reseed.Revision+1&&crossFirst.Scalars.Posture==8&&crossFirst.Components!.Contains(17)&&world.IsCurrent(resource),"Components-first composite commits one state and retains resources");
        world.Accept(generation,Bytes("cross11first"));var crossSecond=world.FindReplication(generation,Oid)!;
        Check(crossSecond.Revision==crossFirst.Revision+1&&world.DrainChanges(generation)!.Entries.Single().Current!.Replication!.Revision==crossSecond.Revision,"Scalar-first composite commits one coalesced state");
        Reject<InvalidDataException>(()=>world.Accept(generation,Bytes("lateParse")));Check(world.DrainChanges(generation) is null&&world.RetainedReplicationDecodedBytes==0&&!world.IsCurrent(resource),"Late malformed composite clears whole generation");transport.Close();
        foreach(string bad in new[]{"lateParse","lateGrowth"})using(var failure=World())
        {long g=Stage(failure);failure.CompleteMinimumSceneBarrier(g,true);var held=failure.DrainChanges(g)!;Reject<InvalidDataException>(()=>failure.Accept(g,Bytes(bad)));Check(failure.State==ZoneSceneTransitionState.Failed&&failure.DrainChanges(g) is null&&!failure.IsCurrent(held)&&failure.Snapshots().Count==0&&failure.RetainedReplicationDecodedBytes==0,"Late field5/cross failure clears all state");}
        using(var replay=World()){long g=Stage(replay,"lateGrowth");Reject<InvalidDataException>(()=>replay.CompleteMinimumSceneBarrier(g,true));Check(replay.DrainChanges(g) is null&&replay.Snapshots().Count==0,"Late replay no prefix/batch/ready");}
        using(var parse=World()){long g=Stage(parse);Reject<InvalidDataException>(()=>parse.Accept(g,Bytes("lateParse")));Check(parse.StagedCount==0&&parse.DrainChanges(g) is null,"Full decode before staging");}
        using(var missing=World()){long g=Stage(missing,"components",false);Reject<InvalidDataException>(()=>missing.CompleteMinimumSceneBarrier(g,true));Check(missing.DrainChanges(g) is null,"Complete seed required");}
        using(var budget=World(170)){long g=Stage(budget);Reject<InvalidDataException>(()=>budget.CompleteMinimumSceneBarrier(g,true));Check(budget.RetainedReplicationDecodedBytes==0&&budget.DrainChanges(g) is null,"Seed aggregate replay failure");}
        using(var candidateBudget=World(171))
        {long g=Stage(candidateBudget);candidateBudget.CompleteMinimumSceneBarrier(g,true);Reject<InvalidDataException>(()=>candidateBudget.Accept(g,Bytes("byteGrowth")));Check(candidateBudget.RetainedReplicationDecodedBytes==0&&candidateBudget.DrainChanges(g) is null,"Component-only candidate aggregate overflow clears whole world");}
        using(var overflow=World(change:new(2,ulong.MaxValue-3)))
        {long g=Stage(overflow);overflow.CompleteMinimumSceneBarrier(g,true);Reject<OverflowException>(()=>overflow.Accept(g,Bytes("components")));Check(overflow.DrainChanges(g) is null&&overflow.RetainedReplicationDecodedBytes==0,"World mutation exhaustion clears generation");}
        using(var capacity=World(change:new(1))){long g=Stage(capacity);capacity.CompleteMinimumSceneBarrier(g,true);Reject<InvalidDataException>(()=>capacity.Accept(g,ObjectBytes("create",Oid+1)));Check(capacity.DrainChanges(g) is null,"Observer capacity failure");}
        using(var lifecycle=World())
        {long g=Stage(lifecycle,"components");lifecycle.CompleteMinimumSceneBarrier(g,true);var held=lifecycle.FindReplication(g,Oid)!;lifecycle.Accept(g,Bytes("contain"));Check(lifecycle.IsCurrent(held),"World contain keeps component state");lifecycle.Accept(g,Bytes("destroy"));Check(!lifecycle.IsCurrent(held)&&lifecycle.RetainedReplicationDecodedBytes==0,"World destroy releases optional charges");lifecycle.Accept(g,Bytes("create"));Reject<InvalidDataException>(()=>lifecycle.Accept(g,Bytes("components")));Check(lifecycle.DrainChanges(g) is null,"Same ID recreate has no baseline");}
        var pending=new TaskCompletionSource<SceneCreatureResources>();CancellationToken cancellation=default;
        using(var cancelled=World(load:(q,t)=>{cancellation=t;return pending.Task;}))
        {long g=Stage(cancelled);cancelled.CompleteMinimumSceneBarrier(g,true);cancelled.PollResources();Reject<InvalidDataException>(()=>cancelled.Accept(g,Bytes("lateParse")));Check(cancellation.IsCancellationRequested&&cancelled.OutstandingJobs==1,"Failure cancels outstanding load");pending.SetException(new Exception("synthetic late fault"));Check(cancelled.PollResources().Retired.Single().Error is not null&&cancelled.OutstandingJobs==0,"Late fault observed and drained");}
        SceneWorldCoordinator? reentrant=null;reentrant=World(load:(q,t)=>{Reject<InvalidOperationException>(()=>reentrant!.DrainChanges(reentrant.Generation));return Task.FromResult(Stub(q));});
        using(reentrant){long g=Stage(reentrant);reentrant.CompleteMinimumSceneBarrier(g,true);reentrant.PollResources();Check(reentrant.PollResources().Prepared.Count==1,"Owner reentrancy guard");Exception? error=null;var thread=new Thread(()=>{try{reentrant.FindReplication(g,Oid);}catch(Exception e){error=e;}});thread.Start();thread.Join();Check(error is InvalidOperationException,"World foreign thread rejected");var held=reentrant.DrainChanges(g)!;long next=reentrant.BeginSelection(Oid).Generation;Check(!reentrant.Accept(g,new byte[]{0})&&reentrant.DrainChanges(next) is null&&!reentrant.IsCurrent(held),"Stale generation rejected before parse and reset clears");reentrant.Close();Check(reentrant.RetainedReplicationDecodedBytes==0,"Close clears all optional state");}
        File.WriteAllText(exportPath,JsonSerializer.Serialize(new {NegotiatedBytes=496,SessionRequest=request,SelectDatagram=select,ReadyDatagram=ready,DeliveredApplications=delivered,First=View(first),Scalar=View(scalarState),Noop=View(noop),Future=View(future),Stream=View(stream),Reseed=View(reseed),FirstMutation=firstBatch.MutationRevision,NoopMutation=noopBatch.MutationRevision,StreamMutation=streamBatch.MutationRevision,Cases=cases,ResourceLoads=loads,FinalBytes=world.RetainedReplicationDecodedBytes},new JsonSerializerOptions {WriteIndented=true})+"\n");
        Console.WriteLine("PASS: optional standalone components store/world,69 arithmetic cases, sequential mixed/scalar, normalized/raw ownership, atomic bounds/revisions, lifecycle/observer/resources/cleanup; composite accepted in both orders");
    }
}
