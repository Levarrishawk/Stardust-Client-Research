using System.Buffers.Binary;
using System.Numerics;
using System.Text;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

internal static class SceneWorldChangeScenarios
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
    private static object Scalar(CreatureView3ScalarState v)=>new {v.Posture,v.Rank,v.LinkId,HeightBits=BitConverter.SingleToUInt32Bits(v.Height),v.Shock,v.States};
    private static object Batch(SceneWorldChangeBatch b)=>new {b.Generation,b.MutationRevision,Entries=b.Entries.Select(e=>new {e.ObjectId,Kind=(int)e.Kind,Present=e.Current is not null,Incarnation=e.Current?.Object.Incarnation,Container=e.Current?.Object.ContainerId,Phase=e.Current?.Object.Phase.ToString(),Scalars=e.Current?.Replication is {} r?Scalar(r.Scalars):null})};
    internal static void Run(IReadOnlyDictionary<string,string> peer,string? exportPath=null)
    {
        byte[] Bytes(string name)=>Convert.FromHexString(peer[name]);
        byte[] ObjectBytes(string name,ulong id) {var bytes=Bytes(name);BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(6),id);return bytes;}
        var policy=new SceneWorldReplicationPolicy(SceneWorldReplicationMode.CreatureView3Scalars,new(1024,64,64,16,512),16,512);
        SceneWorldCoordinator World(SceneWorldChangePolicy? delivery=null,Func<SceneCreatureResourceRequest,CancellationToken,Task<SceneCreatureResources>>? loader=null)=>
            new(Catalog,Selection,Bank,loader??((q,t)=>Task.FromResult(Stub(q))),SceneCreateZeroQuaternionPolicy.Reject,4,1,16,8192,1,25,1024,policy,delivery);
        long Stage(SceneWorldCoordinator world,bool full=true)
        {long g=world.BeginSelection(Oid).Generation;world.Accept(g,Bytes("start"));world.Accept(g,Bytes("create"));world.Accept(g,Bytes("baseline"));if(full)world.Accept(g,Bytes("delta"));world.Accept(g,Bytes("end"));if(full)world.Accept(g,Bytes("contain"));return g;}
        var tasks=new List<TaskCompletionSource<SceneCreatureResources>>();var requests=new List<SceneCreatureResourceRequest>();var tokens=new List<CancellationToken>();
        Task<SceneCreatureResources> Load(SceneCreatureResourceRequest q,CancellationToken token) {requests.Add(q);tokens.Add(token);var task=new TaskCompletionSource<SceneCreatureResources>();tasks.Add(task);return task.Task;}
        using var world=World(new(3),Load);var transport=new SoeClientTransport();var delivered=new List<string>();
        try
        {
            var selection=world.BeginSelection(Oid);long g=selection.Generation;
            string request=Convert.ToHexString(transport.BeginSession(0x12345678,496));transport.AcceptSessionResponse(Bytes("response"));
            string select=Convert.ToHexString(transport.CreateReliableDatagram(selection.CopySelectMessage(),0));Check(select==peer["expectedSelect"],"Independent select wire");
            void Receive(string name)
            {byte[] wire=Bytes(name);Check(wire.Length<=496,"Declared datagram bound");var result=transport.AcceptDatagram(wire);foreach(var payload in result.ApplicationPayloads) {delivered.Add(Convert.ToHexString(payload));world.Accept(g,payload);Array.Clear(payload);}Array.Clear(wire);}
            Receive("ackSelect");Receive("peer_create");Check(world.DrainChanges(g) is null,"No selecting/prebarrier delivery");Receive("peer_start");Receive("peer_start");
            foreach(string name in new[]{"baseline","delta","end","contain"})Receive("peer_"+name);
            Check(world.DrainChanges(g) is null&&world.DrainChanges(g-1) is null&&tasks.Count==0,"Staged owned bytes emit no changes or resources");
            Check(world.CompleteMinimumSceneBarrier(g,false) is null,"False caller barrier");
            string ready=Convert.ToHexString(transport.CreateReliableDatagram(world.CompleteMinimumSceneBarrier(g,true)!,100));Check(ready==peer["expectedReady"],"Ready after committed replay");
            var first=world.DrainChanges(g)!;Check(first.MutationRevision==5&&first.Entries.Count==1&&first.Entries[0].Kind==(SceneWorldChangeKind.Lifecycle|SceneWorldChangeKind.Replication)&&first.Entries[0].Current!.Object.Phase==SceneObjectPhase.Finalized,"Create/baseline/delta/end/contain coalesced to one latest observation");
            Check(world.IsCurrent(first)&&world.DrainChanges(g)!.Entries.Count==1,"Unacknowledged slots retained without snapshot history");
            Reject<NotSupportedException>(()=>((IList<SceneWorldChangeEntry>)first.Entries)[0]=first.Entries[0]);
            using(var foreign=World()) {long fg=Stage(foreign);foreign.CompleteMinimumSceneBarrier(fg,true);var fb=foreign.DrainChanges(fg)!;Check(fb.Generation==first.Generation&&fb.MutationRevision==first.MutationRevision&&!foreign.IsCurrent(first)&&!foreign.AcknowledgeChanges(first)&&foreign.DrainChanges(fg)!.Entries.Count==1,"Foreign owner rejected despite identical generation/revision");}
            world.PollResources();tasks[0].SetResult(Stub(requests[0]));var resource=world.PollResources().Prepared.Single();Check(world.IsCurrent(first)&&world.DrainChanges(g)!.MutationRevision==5&&world.IsCurrent(resource),"Resource polls/publications do not dirty world delivery");
            Receive("ackReady");Receive("peer_serverReady");Check(world.AcknowledgeChanges(first)&&world.DrainChanges(g)!.Entries.Count==0,"Current acknowledgement clears slots");
            Receive("peer_baselineReset");var reset=world.DrainChanges(g)!;Check(reset.Entries.Single().Kind==SceneWorldChangeKind.Replication,"Typed baseline marks replication only");
            Receive("peer_deltaReset");Check(!world.IsCurrent(reset)&&!world.AcknowledgeChanges(reset)&&world.DrainChanges(g)!.Entries.Count==1,"Stale acknowledgement cannot lose unseen changed ID");
            for(int i=0;i<1000;i++) {world.Accept(g,Bytes("baseline"));world.Accept(g,Bytes("delta"));}
            var stream=world.DrainChanges(g)!;Check(stream.MutationRevision==2007&&stream.Entries.Count==1&&world.IsCurrent(resource)&&tasks.Count==1&&world.RetainedReplicationDecodedBytes==119,"Long stream one slot/no resource reload");
            Check(BitConverter.SingleToUInt32Bits(reset.Entries.Single().Current!.Replication!.Scalars.Height)==0xBFE00000&&BitConverter.SingleToUInt32Bits(stream.Entries.Single().Current!.Replication!.Scalars.Height)==0x80000000,"Retained batch immutable; drain resolves current state");
            Check(world.AcknowledgeChanges(stream)&&world.DrainChanges(g)!.Entries.Count==0,"Stream acknowledgement clears delivery metadata");
            world.Accept(g,ObjectBytes("create",Oid+1));world.Accept(g,ObjectBytes("baseline",Oid+1));world.Accept(g,Bytes("delta"));world.Accept(g,ObjectBytes("destroy",Oid+1));
            var removed=world.DrainChanges(g)!;Check(removed.Entries.Select(e=>e.ObjectId).SequenceEqual(new[]{Oid+1,Oid})&&removed.Entries[0].Current is null,"First-touch order and tombstone at drain");
            world.Accept(g,ObjectBytes("create",Oid+1));world.Accept(g,ObjectBytes("baseline",Oid+1));
            Check(!world.IsCurrent(removed)&&!world.AcknowledgeChanges(removed),"Old removal batch cannot delete replacement");
            var recreated=world.DrainChanges(g)!;Check(recreated.Entries.Select(e=>e.ObjectId).SequenceEqual(new[]{Oid+1,Oid})&&recreated.Entries[0].Current is not null&&recreated.Entries[0].Current!.Replication is not null,"Destroy/recreate ID resolves final incarnation without deleting it");
            Check(world.AcknowledgeChanges(recreated),"Current recreated delivery accepted");
            world.Accept(g,SceneObjectLifecycleCodec.EncodeContainment(new(Oid,Oid+1,42)));var attached=world.DrainChanges(g)!;world.AcknowledgeChanges(attached);
            world.Accept(g,ObjectBytes("destroy",Oid+1));var detached=world.DrainChanges(g)!;
            Check(detached.Entries.Select(e=>e.ObjectId).SequenceEqual(new[]{Oid,Oid+1})&&detached.Entries[0].Current!.Object.ContainerId==0&&detached.Entries[1].Current is null,"Direct child detachment delivered; equal first-sequence tie ordered unsigned ID");
            Check(world.AcknowledgeChanges(detached)&&!world.IsCurrent(resource),"Lifecycle resource freshness remains strict and separate");
            world.PollResources();Check(world.DrainChanges(g)!.Entries.Count==0&&tasks.Count==2,"Starting revised resource load emits no world batch");
            var empty=world.DrainChanges(g)!;Exception? error=null;var thread=new Thread(()=>{try{world.DrainChanges(g);}catch(Exception e){error=e;}});thread.Start();thread.Join();Check(error is InvalidOperationException,"Foreign-thread drain rejected");
            Reject<InvalidDataException>(()=>world.Accept(g,Bytes("delta")[..^1]));Check(world.State==ZoneSceneTransitionState.Failed&&world.DrainChanges(g) is null&&!world.IsCurrent(empty)&&!world.AcknowledgeChanges(empty)&&tokens[1].IsCancellationRequested,"Failed generation discards pending delivery and invalidates held batches");
            tasks[1].SetException(new Exception("synthetic failed scene late fault"));Check(world.PollResources().Retired.Single().Error is not null,"Failure cleanup observes late task fault");
            long next=world.BeginSelection(Oid).Generation;Check(world.DrainChanges(g) is null&&world.DrainChanges(next) is null&&!world.Accept(g,new byte[]{0}),"Selection clears delivery and stale generation ignored before parse");
            if(exportPath is not null)
            {
                var export=new {NegotiatedBytes=496,SessionRequest=request,SelectDatagram=select,ReadyDatagram=ready,DeliveredApplications=delivered,
                    First=Batch(first),Reset=Batch(reset),Stream=Batch(stream),Removed=Batch(removed),Recreated=Batch(recreated),Detached=Batch(detached),
                    StreamPairs=1000,FinalDrainEmpty=world.DrainChanges(next) is null,FinalReplicationBytes=world.RetainedReplicationDecodedBytes,ResourceLoadsBeforeLifecycle=1};
                File.WriteAllText(exportPath,JsonSerializer.Serialize(export,new JsonSerializerOptions {WriteIndented=true})+"\n");
            }
        }
        finally {transport.Close();}
        using(var capacity=World(new(1)))
        {
            long g=Stage(capacity,false);capacity.Accept(g,ObjectBytes("create",Oid+1));Reject<InvalidDataException>(()=>capacity.CompleteMinimumSceneBarrier(g,true));
            Check(capacity.State==ZoneSceneTransitionState.Failed&&capacity.DrainChanges(g) is null&&capacity.Snapshots().Count==0&&capacity.RetainedReplicationDecodedBytes==0,"Capacity replay failure discards earlier committed slots/state without partial batch/ready");
        }
        using(var capacity=World(new(1)))
        {
            long g=Stage(capacity,false);capacity.CompleteMinimumSceneBarrier(g,true);var old=capacity.DrainChanges(g)!;
            Reject<InvalidDataException>(()=>capacity.Accept(g,ObjectBytes("create",Oid+1)));Check(!capacity.IsCurrent(old)&&capacity.DrainChanges(g) is null,"Live unseen capacity exhaustion fails whole generation rather than dropping slots");
        }
        using(var reusable=World(new(1)))
        {
            long g=Stage(reusable,false);reusable.CompleteMinimumSceneBarrier(g,true);reusable.AcknowledgeChanges(reusable.DrainChanges(g)!);reusable.Accept(g,ObjectBytes("create",Oid+1));var added=reusable.DrainChanges(g)!;
            Check(added.Entries.Count==1&&added.Entries[0].ObjectId==Oid+1&&added.Entries[0].Current!.Replication is null,"Acknowledgement frees slot; query creates no default baseline");
        }
        using(var overflow=World(new(3,ulong.MaxValue-4)))
        {
            long g=Stage(overflow);Reject<OverflowException>(()=>overflow.CompleteMinimumSceneBarrier(g,true));Check(overflow.State==ZoneSceneTransitionState.Failed&&overflow.DrainChanges(g) is null&&overflow.Snapshots().Count==0,"Sequence exhaustion during replay clears all earlier state/change slots");overflow.Close();
        }
        using(var overflow=World(new(3,ulong.MaxValue-4)))
        {
            long g=Stage(overflow,false);overflow.CompleteMinimumSceneBarrier(g,true);overflow.AcknowledgeChanges(overflow.DrainChanges(g)!);overflow.Accept(g,Bytes("delta"));var last=overflow.DrainChanges(g)!;
            Check(last.MutationRevision==ulong.MaxValue,"Last unique mutation revision accepted");Reject<OverflowException>(()=>overflow.Accept(g,Bytes("delta")));Check(!overflow.IsCurrent(last)&&overflow.DrainChanges(g) is null,"Live sequence overflow invalidates unconsumed batch");overflow.Close();
        }
        SceneWorldCoordinator? reentrant=null;reentrant=World(loader:(q,t)=>{Reject<InvalidOperationException>(()=>reentrant!.DrainChanges(reentrant.Generation));return Task.FromResult(Stub(q));});
        using(reentrant) {long g=Stage(reentrant);reentrant.CompleteMinimumSceneBarrier(g,true);var batch=reentrant.DrainChanges(g)!;reentrant.PollResources();Check(reentrant.PollResources().Prepared.Count==1&&reentrant.IsCurrent(batch),"No synchronous callbacks; actual loader query reentrancy rejected");reentrant.Close();Check(reentrant.DrainChanges(g) is null&&!reentrant.AcknowledgeChanges(batch),"Close discards batches without counter increment");reentrant.Close();}
        using(var opaque=new SceneWorldCoordinator(Catalog,Selection,Bank,(q,t)=>Task.FromResult(Stub(q)),SceneCreateZeroQuaternionPolicy.Reject))
        {long g=Stage(opaque,false);opaque.CompleteMinimumSceneBarrier(g,true);var batch=opaque.DrainChanges(g)!;Check(batch.Entries.Single().Current!.Replication is null&&batch.Entries.Single().Current!.Envelopes.Count==1&&opaque.AcknowledgeChanges(batch),"Opaque snapshot mode and delivery coexist");}
        Reject<ArgumentOutOfRangeException>(()=>World(new(0)));Reject<ArgumentOutOfRangeException>(()=>World(new(4097)));
        Console.WriteLine("PASS: bounded coalesced postcommit changes;496-byte peer, latest drain/ack, stable order, replacement/detachment, stale owner/revision, resource separation, capacity/overflow rollback and clear");
    }
}
