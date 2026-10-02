using System.Buffers.Binary;
using System.Numerics;
using System.Text;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

internal static class SceneWorldReplicationScenarios
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
    internal static void Run(IReadOnlyDictionary<string,string> peer,string? exportPath=null)
    {
        byte[] Bytes(string name)=>Convert.FromHexString(peer[name]);
        var limits=new CreatureView3BaselineLimits(1024,64,64,16,512);
        SceneWorldReplicationPolicy Policy(long bytes=240)=>new(SceneWorldReplicationMode.CreatureView3Scalars,limits,16,bytes);
        SceneWorldCoordinator World(Func<SceneCreatureResourceRequest,CancellationToken,Task<SceneCreatureResources>>? loader=null,SceneWorldReplicationPolicy? policy=null)=>
            new(Catalog,Selection,Bank,loader??((q,t)=>Task.FromResult(Stub(q))),SceneCreateZeroQuaternionPolicy.Reject,4,1,16,8192,1,25,1024,policy??Policy());
        long Stage(SceneWorldCoordinator world,bool baseline=true)
        {
            long g=world.BeginSelection(Oid).Generation;world.Accept(g,Bytes("start"));world.Accept(g,Bytes("create"));
            if(baseline)world.Accept(g,Bytes("baseline"));world.Accept(g,Bytes("end"));return g;
        }
        var tasks=new List<TaskCompletionSource<SceneCreatureResources>>();var requests=new List<SceneCreatureResourceRequest>();var tokens=new List<CancellationToken>();
        Task<SceneCreatureResources> Load(SceneCreatureResourceRequest q,CancellationToken token) { requests.Add(q);tokens.Add(token);var task=new TaskCompletionSource<SceneCreatureResources>();tasks.Add(task);return task.Task; }
        using var world=World(Load);var transport=new SoeClientTransport();var delivered=new List<string>();
        try
        {
            var selection=world.BeginSelection(Oid);long g=selection.Generation;
            string request=Convert.ToHexString(transport.BeginSession(0x12345678,496));transport.AcceptSessionResponse(Bytes("response"));
            string selectWire=Convert.ToHexString(transport.CreateReliableDatagram(selection.CopySelectMessage(),0));Check(selectWire==peer["expectedSelect"],"Exact independently packed select");
            void Receive(string name)
            {
                byte[] wire=Bytes(name);Check(wire.Length<=496,"Declared negotiated bound");var result=transport.AcceptDatagram(wire);
                foreach(var payload in result.ApplicationPayloads) { delivered.Add(Convert.ToHexString(payload));world.Accept(g,payload);Array.Clear(payload); }Array.Clear(wire);
            }
            Receive("ackSelect");byte[] corrupt=Bytes("peer_baseline");corrupt[^1]^=1;Check(transport.AcceptDatagram(corrupt).RejectedFraming&&world.StagedCount==0,"CRC rejection before world");
            Receive("peer_create");Check(world.StagedCount==0,"Reordered future create buffered");Receive("peer_start");Receive("peer_start");
            foreach(string name in new[]{"baseline","delta","end","contain"})Receive("peer_"+name);
            Check(world.StagedCount==5&&world.Snapshots().Count==0&&tasks.Count==0&&world.RetainedReplicationDecodedBytes==0,"Fully decoded owned messages staged without world/resource publication");
            Check(world.CompleteMinimumSceneBarrier(g,false) is null&&world.CompleteMinimumSceneBarrier(g-1,true) is null,"Explicit barrier and generation");
            byte[] ready=world.CompleteMinimumSceneBarrier(g,true)!;string readyWire=Convert.ToHexString(transport.CreateReliableDatagram(ready,100));Check(readyWire==peer["expectedReady"]&&world.CompleteMinimumSceneBarrier(g,true) is null,"Ready after complete typed replay only once");
            string retry=Convert.ToHexString(transport.GetRetransmissionDatagrams(200,100).Single());Check(retry==readyWire,"Exact ready retry");
            var first=world.FindReplication(g,Oid)!;Check(first.Revision==2&&world.IsCurrent(first)&&world.Snapshots().Single().Replication!.Revision==first.Revision,"World-owned typed snapshot/current query");
            Check(first.Scalars.Posture==7&&BitConverter.SingleToUInt32Bits(first.Scalars.Height)==0x80000000&&world.Snapshots().Single().Envelopes.Count==0&&world.RetainedReplicationDecodedBytes==119,"Typed scalar bits without opaque history");
            var owned=world.Snapshots();Reject<NotSupportedException>(()=>((IList<SceneWorldObjectSnapshot>)owned)[0]=owned[0]);
            Check(world.PollResources().Prepared.Count==0&&tasks.Count==1,"One finalized load after barrier");tasks[0].SetResult(Stub(requests[0]));var resource=world.PollResources().Prepared.Single();Check(world.IsCurrent(resource),"Resource revision and attempt current");
            Receive("ackReady");Receive("peer_serverReady");Receive("peer_baselineReset");var reset=world.FindReplication(g,Oid)!;
            Check(reset.Scalars==reset.Baseline.Scalars&&!world.IsCurrent(first)&&world.IsCurrent(resource)&&tasks.Count==1,"Duplicate supported baseline resets overlay without resource reload");
            Receive("peer_deltaReset");var resetDelta=world.FindReplication(g,Oid)!;
            for(int i=0;i<1000;i++) { world.Accept(g,Bytes("baseline"));world.Accept(g,Bytes("delta")); }
            var streamed=world.FindReplication(g,Oid)!;Check(streamed.Revision==resetDelta.Revision+2000&&world.RetainedReplicationDecodedBytes==119&&world.Snapshots().Single().Envelopes.Count==0&&world.IsCurrent(resource)&&tasks.Count==1,"Long bounded replace stream independent of one-envelope history limit");
            Check(world.PollResources().Prepared.Count==0&&world.IsCurrent(resource),"Scalar-only events preserve valid resource publication");
            world.Accept(g,SceneObjectLifecycleCodec.EncodeContainment(new(Oid,0,7)));
            Check(world.IsCurrent(streamed)&&!world.IsCurrent(resource),"Incarnation state survives changed lifecycle while resource revision retires");world.PollResources();tasks[1].SetResult(Stub(requests[1]));var movedResource=world.PollResources().Prepared.Single();Check(world.IsCurrent(movedResource),"Current revised resource attempt");
            using(var foreign=World()) {long fg=Stage(foreign);foreign.CompleteMinimumSceneBarrier(fg,true);Check(!foreign.IsCurrent(streamed),"Foreign world state rejected");}
            Exception? error=null;var thread=new Thread(()=>{try{world.FindReplication(g,Oid);}catch(Exception e){error=e;}});thread.Start();thread.Join();Check(error is InvalidOperationException,"Dedicated foreign thread rejects query");
            Receive("peer_destroy");Check(world.FindReplication(g,Oid) is null&&!world.IsCurrent(streamed)&&!world.IsCurrent(movedResource)&&world.RetainedReplicationDecodedBytes==0,"Destroy retires state/resource/accounting");
            world.Accept(g,Bytes("create"));Check(world.FindReplication(g,Oid) is null,"Same-ID recreation has no fabricated baseline");world.Accept(g,Bytes("baseline"));world.Accept(g,Bytes("end"));world.PollResources();
            var recreated=world.FindReplication(g,Oid)!;Check(recreated.Incarnation!=streamed.Incarnation&&!world.IsCurrent(streamed),"Recreated incarnation distinct");
            long next=Stage(world);Check(tokens[2].IsCancellationRequested&&!world.Accept(g,new byte[]{0})&&world.FindReplication(g,Oid) is null&&world.FindReplication(next,Oid) is null,"Selection retires old state and stale generation before parse");
            world.CompleteMinimumSceneBarrier(next,true);world.PollResources();Check(tasks.Count==3&&world.OutstandingJobs==1,"Cancelled ignoring task remains budgeted across selection");
            tasks[2].SetException(new Exception("synthetic old-generation fault"));var retired=world.PollResources();Check(retired.Prepared.Count==0&&retired.Retired.Single().Error is not null&&tasks.Count==4&&world.FindReplication(next,Oid) is not null,"Late retired fault frees budget without harming typed scene");
            var current=world.FindReplication(next,Oid)!;Reject<InvalidDataException>(()=>world.Accept(next,Bytes("delta")[..^1]));Check(world.State==ZoneSceneTransitionState.Failed&&!world.IsCurrent(current)&&world.FindReplication(next,Oid) is null&&tokens[3].IsCancellationRequested,"Malformed active typed update fails/clears whole world and cancels jobs");
            tasks[3].SetException(new Exception("synthetic failed-generation late fault"));Check(world.PollResources().Retired.Single().Error is not null&&world.RetainedReplicationDecodedBytes==0,"Failed typed scene observes retired fault");
            if(exportPath is not null)
            {
                var export=new {NegotiatedBytes=496,SessionRequest=request,SelectDatagram=selectWire,ReadyDatagram=readyWire,Retransmission=retry,DeliveredApplications=delivered,
                    First=Scalar(first.Scalars),BaselineReset=Scalar(reset.Scalars),AfterResetDelta=Scalar(resetDelta.Scalars),AfterLongStream=Scalar(streamed.Scalars),
                    FirstRevision=first.Revision,ResetRevision=reset.Revision,ResetDeltaRevision=resetDelta.Revision,StreamRevision=streamed.Revision,
                    BaselineHex=Convert.ToHexString(CreatureView3BaselineCodec.Encode(first.Baseline,limits)),BaselineDecodedBytes=first.Baseline.DecodedBytes,
                    StreamPairs=1000,OpaqueHistoryCount=owned.Single().Envelopes.Count,ResourceLoadsBeforeLifecycleChange=1,FinalReplicationBytes=world.RetainedReplicationDecodedBytes,FinalState=world.State.ToString()};
                File.WriteAllText(exportPath,JsonSerializer.Serialize(export,new JsonSerializerOptions {WriteIndented=true})+"\n");
            }
        }
        finally {transport.Close();}
        foreach(string failure in new[]{"nobaseline","lateunseeded","missingobject","wrongtag","wrongview","unsupported","malformed","aggregate"})
        {
            using var invalid=World(policy:Policy(failure=="aggregate"?118:240));long g=invalid.BeginSelection(Oid).Generation;invalid.Accept(g,Bytes("start"));
            if(failure=="wrongtag"||failure=="wrongview"||failure=="unsupported"||failure=="malformed")
            {
                byte[] bad=Bytes(failure=="malformed"?"baseline":"delta");
                if(failure=="wrongtag")bad[14]^=1;if(failure=="wrongview")bad[18]^=1;
                if(failure=="unsupported")BinaryPrimitives.WriteUInt16LittleEndian(bad.AsSpan(25),17);if(failure=="malformed")Array.Resize(ref bad,bad.Length-1);
                Reject<InvalidDataException>(()=>invalid.Accept(g,bad));
            }
            else if(failure=="lateunseeded")
            {
                invalid.Accept(g,Bytes("create"));invalid.Accept(g,Bytes("baseline"));
                byte[] second=Bytes("create"),delta=Bytes("delta");BinaryPrimitives.WriteUInt64LittleEndian(second.AsSpan(6),Oid+1);BinaryPrimitives.WriteUInt64LittleEndian(delta.AsSpan(6),Oid+1);
                invalid.Accept(g,second);invalid.Accept(g,delta);Reject<InvalidDataException>(()=>invalid.CompleteMinimumSceneBarrier(g,true));
            }
            else
            {
                if(failure!="missingobject")invalid.Accept(g,Bytes("create"));invalid.Accept(g,Bytes(failure=="nobaseline"?"delta":"baseline"));
                Reject<InvalidDataException>(()=>invalid.CompleteMinimumSceneBarrier(g,true));
            }
            Check(invalid.State==ZoneSceneTransitionState.Failed&&invalid.Snapshots().Count==0&&invalid.RetainedReplicationDecodedBytes==0&&invalid.OutstandingJobs==0&&invalid.CompleteMinimumSceneBarrier(g,true) is null,"Atomic failure/no Ready: "+failure);
        }
        {
            var task=new TaskCompletionSource<SceneCreatureResources>();var closed=World((q,t)=>task.Task);long g=Stage(closed);closed.CompleteMinimumSceneBarrier(g,true);var state=closed.FindReplication(g,Oid)!;closed.PollResources();closed.Close();
            Check(!closed.IsCurrent(state)&&closed.RetainedReplicationDecodedBytes==0&&closed.OutstandingJobs==1&&!closed.DrainCompletion.IsCompleted,"Close retires typed state while tracking unfinished cancellation");task.SetException(new Exception("synthetic close fault"));closed.DrainCompletion.GetAwaiter().GetResult();Check(closed.OutstandingJobs==0,"Close drain observes late fault");closed.Close();
        }
        SceneWorldCoordinator? reentrant=null;
        reentrant=World((q,t)=> { Reject<InvalidOperationException>(()=>reentrant!.FindReplication(reentrant.Generation,Oid));return Task.FromResult(Stub(q)); });
        using(reentrant) {long g=Stage(reentrant);reentrant.CompleteMinimumSceneBarrier(g,true);reentrant.PollResources();Check(reentrant.PollResources().Prepared.Count==1,"Loader callback cannot reenter new typed query");}
        using(var opaque=new SceneWorldCoordinator(Catalog,Selection,Bank,(q,t)=>Task.FromResult(Stub(q)),SceneCreateZeroQuaternionPolicy.Reject))
        {long g=Stage(opaque);opaque.CompleteMinimumSceneBarrier(g,true);Check(opaque.ReplicationMode==SceneWorldReplicationMode.Opaque&&opaque.FindReplication(g,Oid) is null&&opaque.Snapshots().Single().Envelopes.Count==1,"Default constructor remains opaque even for complete baseline bytes");}
        Console.WriteLine("PASS: typed496-byte offline world replay; six scalars/full baseline, bounded stream, separate state/resource freshness, negatives/backpressure/fault/close; opaque default preserved");
    }
}
