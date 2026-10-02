using System.Buffers.Binary;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;
using Stardust.Core.Proof;

internal static class SceneWorldCoordinatorScenarios
{
    private const ulong Oid=OfflineSceneWorldScenario.ObjectId;
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
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static void Reject<T>(Action action) where T:Exception {try{action();}catch(T){return;}throw new Exception("Expected "+typeof(T).Name);}
    internal static void Run(IReadOnlyDictionary<string,string> peer)
    {
        byte[] Bytes(string key)=>Convert.FromHexString(peer[key]);
        SceneWorldCoordinator World(Func<SceneCreatureResourceRequest,CancellationToken,Task<SceneCreatureResources>>? loader=null,int jobs=1,int staged=16,int stagedBytes=4096,int envelopeCount=16,int envelopeBytes=4096)=>
            new(Catalog,Selection,Bank,loader??((q,t)=>Task.FromResult(Stub(q))),SceneCreateZeroQuaternionPolicy.Reject,4,jobs,staged,stagedBytes,envelopeCount,envelopeBytes);
        long Stage(SceneWorldCoordinator world)
        {
            long g=world.BeginSelection(Oid).Generation;byte[] raw=Bytes("start");world.Accept(g,raw);Array.Clear(raw);Check(world.Scene!.Terrain=="A/B","Owned start DTO");
            foreach(string key in new[]{"syntheticCreate","baseline","delta","end","contain"}){raw=Bytes(key);world.Accept(g,raw);Array.Clear(raw);}
            return g;
        }
        Reject<ArgumentException>(()=>new ObjectTemplateCatalog(new[]{Recipe with{Recipe=(SceneObjectResourceRecipe)255}}));
        // Lead combined integration: accepted static recipe is legal in a catalog, rejected by this creature-only world.
        var staticPath="object/synthetic/static.iff";
        var staticRecipe=Recipe with { ObjectPath=staticPath,TemplateCrc=LegacyAssetPath.Checksum(staticPath),Recipe=SceneObjectResourceRecipe.StaticAppearance };
        var mixed=new ObjectTemplateCatalog(new[]{staticRecipe,Recipe});
        using(var mixedWorld=new SceneWorldCoordinator(mixed,Selection,Bank,(q,t)=>Task.FromResult(Stub(q)),SceneCreateZeroQuaternionPolicy.Reject))
        {
            long g=Stage(mixedWorld);mixedWorld.CompleteMinimumSceneBarrier(g,true);mixedWorld.PollResources();
            Check(mixedWorld.PollResources().Prepared.Count==1,"Supported creature works with static-first mixed catalog");
            Reject<InvalidDataException>(()=>mixedWorld.Accept(g,SceneObjectLifecycleCodec.EncodeCreate(SceneObjectLifecycleCodec.DecodeCreate(Bytes("syntheticCreate")) with { ObjectId=Oid+1,TemplateCrc=staticRecipe.TemplateCrc })));
            Check(mixedWorld.State==ZoneSceneTransitionState.Failed&&mixedWorld.Snapshots().Count==0,"Unsupported static create clears mixed world before publication");
        }
        {
            var batch=new List<TaskCompletionSource<SceneCreatureResources>>();var captured=new List<SceneCreatureResourceRequest>();
            using var world=World((q,t)=>{captured.Add(q);var task=new TaskCompletionSource<SceneCreatureResources>();batch.Add(task);return task.Task;},jobs:2);
            long g=Stage(world);world.Accept(g,Bytes("syntheticParent"));world.Accept(g,SceneObjectLifecycleCodec.EncodeEndBaselines(new(Oid+1)));world.CompleteMinimumSceneBarrier(g,true);world.PollResources();
            Check(batch.Count==2,"Two resource jobs started");batch[0].SetResult(Stub(captured[0]));batch[1].SetException(new Exception("synthetic batch fault"));
            Reject<InvalidDataException>(()=>world.PollResources());Check(world.State==ZoneSceneTransitionState.Failed&&world.Snapshots().Count==0,"Mixed completion batch cannot expose partial publication");
        }
        var tasks=new List<TaskCompletionSource<SceneCreatureResources>>();var requests=new List<SceneCreatureResourceRequest>();var tokens=new List<CancellationToken>();
        Task<SceneCreatureResources> Load(SceneCreatureResourceRequest q,CancellationToken t){requests.Add(q);tokens.Add(t);var source=new TaskCompletionSource<SceneCreatureResources>();tasks.Add(source);return source.Task;}
        using(var world=World(Load))
        {
            long old=Stage(world);Check(world.Snapshots().Count==0&&tasks.Count==0,"Staging does not apply or load");
            Check(world.CompleteMinimumSceneBarrier(old,false) is null&&world.CompleteMinimumSceneBarrier(old-1,true) is null,"Caller barrier not inferred");
            var ready=world.CompleteMinimumSceneBarrier(old,true)!;Check(ready.SequenceEqual(Bytes("serverReady"))&&world.CompleteMinimumSceneBarrier(old,true) is null,"Exact one-shot ready after replay");
            var snapshot=world.Snapshots().Single();Check(snapshot.Object.Phase==SceneObjectPhase.Finalized&&snapshot.Object.ArrangementBits==0xFFFFFFFE&&snapshot.Envelopes.Count==2,"Ordered end/contain replay");
            var opaque=snapshot.Envelopes[0].CopyOpaqueFields();Array.Clear(opaque);Check(world.Snapshots().Single().Envelopes[0].CopyOpaqueFields()[0]==127,"Opaque ownership");
            world.PollResources();Check(tasks.Count==1&&world.OutstandingJobs==1,"Finalized-only load");
            long g=Stage(world);Check(g>old&&tokens[0].IsCancellationRequested,"Generation restart cancels old load");
            Check(!world.Accept(old,Array.Empty<byte>())&&world.CompleteMinimumSceneBarrier(old,true) is null,"Stale tag ignores even malformed bytes");
            world.CompleteMinimumSceneBarrier(g,true);Check(world.PollResources().Prepared.Count==0&&tasks.Count==1,"Ignored canceled work still budgets new generation");
            tasks[0].SetResult(Stub(requests[0]));var retired=world.PollResources();Check(retired.Retired.Single().Kind==SceneCreatureResourceCompletionKind.Stale&&tasks.Count==2,"Old late success released/new queued load starts");
            tasks[1].SetResult(Stub(requests[1]));var publication=world.PollResources().Prepared.Single();Check(world.IsCurrent(publication)&&world.PollResources().Prepared.Count==0,"One-shot owned current resource");
            using(var foreign=World())Check(!foreign.IsCurrent(publication),"Foreign coordinator publication");
            Check(world.Accept(g,Bytes("serverReady"))&&!world.Accept(g,Bytes("serverReady")),"Server-ready echo one-shot");
            world.RestartResource(g,Oid);Check(!world.IsCurrent(publication),"Queued restart immediately suppresses old attempt");world.PollResources();tasks[2].SetResult(Stub(requests[2]));var replaced=world.PollResources().Prepared.Single();
            Check(replaced.Ticket.Handle==publication.Ticket.Handle&&replaced.Ticket.Attempt!=publication.Ticket.Attempt&&world.IsCurrent(replaced)&&!world.IsCurrent(publication),"Attempt freshness beyond same registry handle");
            world.Accept(g,SceneObjectLifecycleCodec.EncodeContainment(new(Oid,0,7)));Check(!world.IsCurrent(replaced),"Contain revision invalidates prepared resource");world.PollResources();
            world.Accept(g,Bytes("destroy"));tasks[3].SetException(new Exception("synthetic late destroyed fault"));var late=world.PollResources();Check(late.Prepared.Count==0&&late.Retired.Single().Error is AggregateException&&world.State==ZoneSceneTransitionState.Ready&&world.Snapshots().Count==0,"Destroyed late fault observed without harming current scene");
            Check(snapshot.Object.Phase==SceneObjectPhase.Finalized&&snapshot.Envelopes.Count==2,"Retained world snapshot");
            Exception? foreignError=null;
            var foreignThread=new Thread(()=>{try{Reject<InvalidOperationException>(()=>world.Snapshots());}catch(Exception error){foreignError=error;}});
            foreignThread.Start();foreignThread.Join();if(foreignError is not null)throw foreignError;
        }
        foreach(string failure in new[]{"missingparent","envelopes","envelopebytes","stagedcount","stagedbytes","unknowncrc","unknownopcode","malformed","conflict"})
        {
            using var world=World(staged:failure=="stagedcount"?1:16,stagedBytes:failure=="stagedbytes"?46:4096,envelopeCount:failure=="envelopes"?1:16,envelopeBytes:failure=="envelopebytes"?40:4096);
            long g=world.BeginSelection(Oid).Generation;world.Accept(g,Bytes("start"));
            if(failure=="unknowncrc")Reject<InvalidDataException>(()=>world.Accept(g,SceneObjectLifecycleCodec.EncodeCreate(SceneObjectLifecycleCodec.DecodeCreate(Bytes("syntheticCreate")) with{TemplateCrc=0})));
            else if(failure=="unknownopcode")Reject<InvalidDataException>(()=>world.Accept(g,new byte[]{1,0,1,2,3,4}));
            else if(failure=="malformed")Reject<InvalidDataException>(()=>world.Accept(g,Bytes("syntheticCreate")[..46]));
            else if(failure=="stagedbytes")Reject<InvalidDataException>(()=>world.Accept(g,Bytes("syntheticCreate")));
            else
            {
                if(failure=="missingparent")world.Accept(g,SceneObjectLifecycleCodec.EncodeContainment(new(Oid,Oid+1,0)));
                world.Accept(g,Bytes("syntheticCreate"));
                if(failure=="stagedcount")Reject<InvalidDataException>(()=>world.Accept(g,Bytes("end")));
                else
                {
                    if(failure=="conflict")world.Accept(g,SceneObjectLifecycleCodec.EncodeCreate(SceneObjectLifecycleCodec.DecodeCreate(Bytes("syntheticCreate")) with{PositionX=99}));
                    if(failure is "envelopes" or "envelopebytes"){world.Accept(g,Bytes("baseline"));world.Accept(g,Bytes("delta"));}
                    Reject<InvalidDataException>(()=>world.CompleteMinimumSceneBarrier(g,true));
                }
            }
            Check(world.State==ZoneSceneTransitionState.Failed&&world.Snapshots().Count==0&&world.OutstandingJobs==0&&world.CompleteMinimumSceneBarrier(g,true) is null,"Whole generation replay/staging failure "+failure);
        }
        {
            using var world=World();long g=world.BeginSelection(Oid).Generation;world.Accept(g,Bytes("start"));world.Accept(g,Bytes("syntheticCreate"));world.CompleteMinimumSceneBarrier(g,true);world.PollResources();Check(world.OutstandingJobs==0,"Pending baseline phase never loads");
            world.Accept(g,Bytes("end"));world.PollResources();Check(world.PollResources().Prepared.Count==1,"After-barrier end starts current revision");
            world.Accept(g,Bytes("baseline"));world.Accept(g,Bytes("delta"));world.Accept(g,Bytes("destroy"));Check(world.Snapshots().Count==0,"After-barrier real envelopes/destroy");
        }
        {
            var source=new TaskCompletionSource<SceneCreatureResources>();SceneCreatureResourceRequest? request=null;var world=World((q,t)=>{request=q;return source.Task;});long g=Stage(world);world.CompleteMinimumSceneBarrier(g,true);world.PollResources();world.Close();Check(world.OutstandingJobs==1&&!world.DrainCompletion.IsCompleted,"Close retains unfinished canceled worker");source.SetException(new Exception("late closed fault"));world.DrainCompletion.GetAwaiter().GetResult();Check(world.OutstandingJobs==0&&world.State==ZoneSceneTransitionState.Closed&&world.Snapshots().Count==0,"Close drains late fault/clears snapshots");world.Close();
        }
        {
            using var world=World((q,t)=>Task.FromException<SceneCreatureResources>(new Exception("synthetic current resource fault")));long g=Stage(world);world.CompleteMinimumSceneBarrier(g,true);world.PollResources();Reject<InvalidDataException>(()=>world.PollResources());Check(world.State==ZoneSceneTransitionState.Failed&&world.Snapshots().Count==0,"Current resource failure retires whole world");
        }
        Console.WriteLine("PASS: world coordinator deterministic barriers; real codecs/ordered replay, caller ready, opaque ownership, revisions/restarts, backpressure across generations, late faults, atomic whole-world failure and close drain");
    }
    internal static void Original(IReadOnlyDictionary<string,string> peer,string root,string configurationPath,string bankPath,string exportPath)
    {
        byte[] cfg=File.ReadAllBytes(configurationPath);Check(Convert.ToHexString(SHA256.HashData(cfg))=="22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524","Config pin");
        var recipe=new ObjectTemplateCatalogEntry(2937962913,"object/creature/player/shared_human_male.iff","E0B61445E8A85CCF0AC5937F0F9CBCA8711718AFEE63384F30970D5AEE120243","951FCE8C2FFC0EB318C751026D24D8C9BF17263D706EB3C93D015867EC801A9C");
        var catalog=new ObjectTemplateCatalog(new[]{recipe});var idle=new LogicalAnimationClipSelection("appearance/lat/all_m.lat","emt_standing_idle_ambient_1_ag","798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B");using var bankFile=File.OpenRead(bankPath);var bank=SkeletalAnimationBankProfile.Load(bankFile);
        Task<SceneCreatureResources>? worker=null;int archivesClosed=0,loads=0;var reads=new Dictionary<string,int>();
        using var world=new SceneWorldCoordinator(catalog,idle,bank,(q,token)=>
        {
            loads++;return worker=Task.Run(()=>
            {
                try{using var reader=new StringReader(Encoding.UTF8.GetString(cfg));using var overlay=ConfiguredTreOverlay.Open(root,reader);
                    TreLookupResult Resolve(string path){reads[path]=reads.GetValueOrDefault(path)+1;return overlay.Lookup(path);}
                    return SceneCreatureResourceReader.Load(q.Snapshot.Create,q.Catalog,q.Idle,q.Movement,Resolve,token,q.MaximumResources,q.MaximumSourceBytes);
                }finally{Interlocked.Exchange(ref archivesClosed,1);}
            });
        },SceneCreateZeroQuaternionPolicy.Reject,maximumOutstandingJobs:1);
        var proof=OfflineSceneWorldScenario.Run(world,peer,()=>worker!.GetAwaiter().GetResult());var result=proof.Publication.Resources;
        Check(loads==1&&archivesClosed==1&&reads.Count==37&&reads.Values.All(v=>v==1)&&result.ExternalReads==37&&result.SourceBytes==4683848,"One pinned archive-closed/read-once job");
        File.WriteAllText(exportPath,JsonSerializer.Serialize(proof.Transcript,new JsonSerializerOptions{WriteIndented=true}));
        foreach(var source in result.Creature.Sources.OrderBy(p=>p.Key))Console.WriteLine($"source {source.Key} {source.Value.Sha256} {source.Value.Bytes}");
        Console.WriteLine("PASS: original full offline world slice; existing SOE transport/real application layouts, exact select/ready peer wire, one pinned 37-source/4683848-byte creature, placement and one-shot publication; destroy retires it");
    }
}
