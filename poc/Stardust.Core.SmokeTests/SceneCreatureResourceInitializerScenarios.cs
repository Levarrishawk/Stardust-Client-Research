using System.Numerics;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

internal static class SceneCreatureResourceInitializerScenarios
{
    private const string Pin = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
    private static readonly string PathName = "object/synthetic/fixture.iff";
    private static readonly ObjectTemplateCatalogEntry Recipe = new(LegacyAssetPath.Checksum(PathName), PathName, Pin, Pin);
    private static readonly ObjectTemplateCatalog Catalog = new(new[]{Recipe});
    private static readonly LogicalAnimationClipSelection Selection = new("appearance/lat/fixture.lat", "idle", Pin);
    private static readonly IReadOnlyDictionary<string,LogicalAnimationClipSelection> Bank = new Dictionary<string,LogicalAnimationClipSelection>{{"walk",Selection},{"run",Selection}};
    private static SceneCreateObjectByCrcMessage Create(ulong id=1)=>new(id,-0f,0,0,2,1,2,3,Recipe.TemplateCrc,255);
    private static SceneCreatureResourceRequest Request(SceneObjectSnapshot snapshot)=>new(snapshot,Catalog,Selection,Bank);
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
    internal static void Run()
    {
        var registry=new SceneObjectRegistry(8,1);var first=registry.Create(1,Create())!;
        var routes=new[]{0,1};var bank=new Dictionary<string,LogicalAnimationClipSelection>{{"walk",Selection with{ExplicitChildRoute=routes}},{"run",Selection}};
        var request=new SceneCreatureResourceRequest(first,Catalog,Selection,bank);routes[0]=9;bank.Clear();
        Check(request.Movement["walk"].ExplicitChildRoute!.SequenceEqual(new[]{0,1}),"Request route/dictionary ownership");
        Reject<NotSupportedException>(()=>((IDictionary<string,LogicalAnimationClipSelection>)request.Movement).Clear());
        Reject<NotSupportedException>(()=>((IList<int>)request.Movement["walk"].ExplicitChildRoute!)[0]=7);
        var tasks=new List<TaskCompletionSource<SceneCreatureResources>>();var requests=new List<SceneCreatureResourceRequest>();var tokens=new List<CancellationToken>();
        Task<SceneCreatureResources> Load(SceneCreatureResourceRequest r,CancellationToken t){requests.Add(r);tokens.Add(t);var source=new TaskCompletionSource<SceneCreatureResources>(TaskCreationOptions.RunContinuationsAsynchronously);tasks.Add(source);return source.Task;}
        using(var owner=new SceneCreatureResourceInitializer(registry,Load,2,2))
        {
            var ticket=owner.Start(request);Check(owner.Start(Request(first))==ticket&&tasks.Count==1,"Idempotent duplicate ignores new config");
            Reject<InvalidDataException>(()=>owner.Start(Request(first with{Create=first.Create with{QuaternionX=0f}}))); // signed zero is byte identity
            Reject<InvalidDataException>(()=>owner.Start(Request(first with{Phase=SceneObjectPhase.Finalized})));
            var otherRegistry=new SceneObjectRegistry(1,1);Reject<InvalidDataException>(()=>owner.Start(Request(otherRegistry.Create(1,Create())!)));
            var restarted=owner.Start(request,true);Check(restarted!=ticket&&!owner.IsCurrent(ticket)&&owner.IsCurrent(restarted)&&!owner.IsCurrent(restarted with{InitializerId=Guid.NewGuid()})&&tokens[0].IsCancellationRequested&&owner.OutstandingJobs==2,"Explicit restart retains retired budget");
            Reject<InvalidDataException>(()=>owner.Start(request,true));Check(owner.Find(1)==restarted&&!tokens[1].IsCancellationRequested,"Capacity rejection atomic");
            tasks[0].SetResult(Stub(request));var stale=owner.Poll().Single();Check(stale.Ticket==ticket&&stale.Kind==SceneCreatureResourceCompletionKind.Stale&&stale.Resources is null&&owner.OutstandingJobs==1,"Cancellation-ignoring late success dropped");
            var resource=Stub(request);tasks[1].SetResult(resource);var ready=owner.Poll().Single();Check(ready.Ticket==restarted&&ready.Kind==SceneCreatureResourceCompletionKind.Prepared&&ReferenceEquals(ready.Resources,resource),"Single owned result transfer");
            Check(owner.Poll().Count==0&&owner.IsCurrent(restarted)&&owner.Start(request)==restarted&&!owner.Cancel(restarted),"No repeated result or implicit restart");
            Task.Run(()=>Reject<InvalidOperationException>(()=>owner.Find(1))).GetAwaiter().GetResult();
            Task.Run(()=>Reject<InvalidOperationException>(()=>owner.Poll())).GetAwaiter().GetResult();
            Task.Run(()=>Reject<InvalidOperationException>(()=>owner.Start(request,true))).GetAwaiter().GetResult();
            Task.Run(()=>Reject<InvalidOperationException>(()=>owner.Dispose())).GetAwaiter().GetResult();
        }
        foreach(string change in new[]{"replace","contain","finalize","destroy","reset","close"})
        {
            var r=new SceneObjectRegistry(3,10);var snap=r.Create(10,Create())!;r.Create(10,Create(2));var source=new TaskCompletionSource<SceneCreatureResources>();CancellationToken token=default;
            using var owner=new SceneCreatureResourceInitializer(r,(q,t)=>{token=t;return source.Task;},1,1);var req=Request(snap);owner.Start(req);
            switch(change){case "replace":r.Replace(10,Create());break;case "contain":r.Contain(10,new(1,2,7));break;case "finalize":r.EndBaselines(10,new(1));break;case "destroy":r.Destroy(10,new(1,0));break;case "reset":r.Reset();break;case "close":r.Close();break;}
            Check(owner.Poll().Count==0&&token.IsCancellationRequested&&owner.OutstandingJobs==1&&owner.TrackedObjects==0,"Revision cancellation still outstanding "+change);
            source.SetResult(Stub(req));var completion=owner.Poll().Single();Check(completion.Kind==SceneCreatureResourceCompletionKind.Stale&&completion.Resources is null&&owner.OutstandingJobs==0,"Late revised result "+change);
            Reject<InvalidDataException>(()=>owner.Start(req));
        }
        {
            var r=new SceneObjectRegistry(1,1);var firstRequest=Request(r.Create(1,Create())!);var sources=new List<TaskCompletionSource<SceneCreatureResources>>();var cancelTokens=new List<CancellationToken>();
            using var owner=new SceneCreatureResourceInitializer(r,(q,t)=>{cancelTokens.Add(t);var source=new TaskCompletionSource<SceneCreatureResources>();sources.Add(source);return source.Task;},2,1);
            var old=owner.Start(firstRequest);r.EndBaselines(1,new(1));var nextRequest=Request(r.Find(1)!);var next=owner.Start(nextRequest);
            Check(old.Handle!=next.Handle&&cancelTokens[0].IsCancellationRequested&&owner.OutstandingJobs==2,"New revision starts fresh attempt rather than retagging");
            sources[1].SetResult(Stub(nextRequest));Check(owner.Poll().Single() is{Kind:SceneCreatureResourceCompletionKind.Prepared}&&owner.Find(1)==next,"Current revised result transfers first");
            sources[0].SetException(new Exception("late old revision fault"));var stale=owner.Poll().Single();Check(stale.Ticket==old&&stale.Kind==SceneCreatureResourceCompletionKind.Stale&&stale.Resources is null&&stale.Error is AggregateException&&owner.Find(1)==next,"Late old revision fault cannot erase current ticket");
        }
        var faults=new SceneObjectRegistry(3,1);var a=faults.Create(1,Create())!;var b=faults.Create(1,Create(2))!;
        var faultSources=new List<TaskCompletionSource<SceneCreatureResources>>();
        using(var owner=new SceneCreatureResourceInitializer(faults,(q,t)=>{var source=new TaskCompletionSource<SceneCreatureResources>();faultSources.Add(source);return source.Task;},2,1))
        {
            var req=Request(a);var ticket=owner.Start(req);Reject<InvalidDataException>(()=>owner.Start(Request(b)));Check(owner.OutstandingJobs==1&&owner.TrackedObjects==1,"Tracked object capacity atomic");
            faultSources[0].SetException(new InvalidDataException("synthetic fault"));var fault=owner.Poll().Single();Check(fault.Kind==SceneCreatureResourceCompletionKind.Faulted&&fault.Error is AggregateException,"Fault observed");
            Check(owner.Start(req)==ticket&&faultSources.Count==1,"Fault requires explicit restart");owner.Start(req,true);owner.Cancel(owner.Find(1)!.Value);faultSources[1].SetException(new Exception("late canceled fault"));var canceledFault=owner.Poll().Single();Check(canceledFault.Error is AggregateException&&canceledFault.Resources is null,"Canceled fault observed");
            owner.Start(req,true);faults.Destroy(1,new(1,0));faultSources[2].SetException(new Exception("late stale fault"));var staleFault=owner.Poll().Single();Check(staleFault.Kind==SceneCreatureResourceCompletionKind.Stale&&staleFault.Error is AggregateException,"Stale fault observed");
        }
        foreach(string finish in new[]{"success","fault","cancel"})
        {
            var r=new SceneObjectRegistry(1,1);var req=Request(r.Create(1,Create())!);var source=new TaskCompletionSource<SceneCreatureResources>();CancellationToken token=default;
            var owner=new SceneCreatureResourceInitializer(r,(q,t)=>{token=t;return source.Task;},1,1);owner.Start(req);owner.Dispose();owner.Dispose();
            Check(token.IsCancellationRequested&&owner.OutstandingJobs==1&&!owner.DrainCompletion.IsCompleted,"Dispose keeps bounded unfinished work "+finish);
            Reject<ObjectDisposedException>(()=>owner.Poll());Reject<ObjectDisposedException>(()=>owner.Start(req));
            if(finish=="success")source.SetResult(Stub(req));else if(finish=="fault")source.SetException(new Exception("late disposed fault"));else source.SetCanceled();
            owner.DrainCompletion.GetAwaiter().GetResult();Check(owner.OutstandingJobs==0,"Dispose drain observes/releases "+finish);
        }
        foreach(string malformed in new[]{"nulltask","throw","nullresult","wrongid","signedzero","recipe","canceled","nullcreature","root","appearance","geometry","skeleton","materials","clips"})
        {
            var r=new SceneObjectRegistry(1,1);var req=Request(r.Create(1,Create())!);
            Task<SceneCreatureResources> LoadBad(SceneCreatureResourceRequest q,CancellationToken t)=>malformed switch{
                "nulltask"=>null!,"throw"=>throw new Exception("synthetic synchronous fault"),"nullresult"=>Task.FromResult<SceneCreatureResources>(null!),
                "wrongid"=>Task.FromResult(Stub(q) with{Create=q.Snapshot.Create with{ObjectId=9}}),"signedzero"=>Task.FromResult(Stub(q) with{Create=q.Snapshot.Create with{QuaternionX=0f}}),
                "recipe"=>Task.FromResult(Stub(q) with{Recipe=Recipe with{ObjectSha256=new string('0',64)}}),
                "nullcreature"=>Task.FromResult(Stub(q) with{Creature=null!}),
                "root"=>Task.FromResult(Stub(q) with{Creature=Stub(q).Creature with{Profile=Stub(q).Creature.Profile with{RootPath="object/wrong.iff"}}}),
                "appearance"=>Task.FromResult(Stub(q) with{Creature=Stub(q).Creature with{Profile=Stub(q).Creature.Profile with{AppearancePath="appearance/wrong.sat"}}}),
                "geometry"=>Task.FromResult(Stub(q) with{Creature=Stub(q).Creature with{Geometries=null}}),
                "skeleton"=>Task.FromResult(Stub(q) with{Creature=Stub(q).Creature with{Skeletons=null}}),
                "materials"=>Task.FromResult(Stub(q) with{Creature=Stub(q).Creature with{MaterialBundle=null}}),
                "clips"=>Task.FromResult(Stub(q) with{Creature=Stub(q).Creature with{AdditionalAnimations=null}}),_=>Task.FromCanceled<SceneCreatureResources>(new CancellationToken(true))};
            using var owner=new SceneCreatureResourceInitializer(r,LoadBad);owner.Start(req);var value=owner.Poll().Single();Check(value.Kind!=SceneCreatureResourceCompletionKind.Prepared&&value.Resources is null,"Invalid task/result "+malformed);
        }
        foreach(string missing in new[]{"idle","selection","lod","skin","tables","rootpin","satpin","accounting"})
        {
            var r=new SceneObjectRegistry(1,1);var req=Request(r.Create(1,Create())!);var valid=Stub(req);var creature=valid.Creature;
            if(missing is "rootpin" or "satpin")
            {
                var sources=creature.Sources.ToDictionary(p=>p.Key,p=>p.Value);string path=missing=="rootpin"?PathName:creature.Profile.AppearancePath;
                sources[path]=sources[path] with{Sha256=new string('0',64)};creature=creature with{Sources=sources};
            }
            else creature=missing switch{"idle"=>creature with{PreparedAnimation=null},"selection"=>creature with{SelectedAnimation=null},"lod"=>creature with{LodMeshes=new Dictionary<string,LodMeshGeneratorDescription>()},"skin"=>creature with{SkinSources=null},"tables"=>creature with{LogicalTables=null},_=>creature};
            var forged=valid with{Creature=creature,ExternalReads=missing=="accounting"?0:valid.ExternalReads};
            using var owner=new SceneCreatureResourceInitializer(r,(q,t)=>Task.FromResult(forged));owner.Start(req);
            Check(owner.Poll().Single() is{Kind:SceneCreatureResourceCompletionKind.Faulted,Resources:null},"Forged closure "+missing);
        }
        {
            var r=new SceneObjectRegistry(1,1);var req=Request(r.Create(1,Create())!);var source=new TaskCompletionSource<SceneCreatureResources>();
            using var owner=new SceneCreatureResourceInitializer(r,(q,t)=>source.Task,1,1);var ticket=owner.Start(req);owner.Cancel(ticket);
            Reject<InvalidDataException>(()=>owner.Start(req,true));Check(owner.Find(1)==ticket&&owner.OutstandingJobs==1,"Canceled current worker still consumes capacity");
            source.SetResult(Stub(req));Check(owner.Poll().Single() is{Kind:SceneCreatureResourceCompletionKind.Canceled,Resources:null},"Canceled current late success never transfers");
        }
        var reentrantRegistry=new SceneObjectRegistry(1,1);var reentrantRequest=Request(reentrantRegistry.Create(1,Create())!);SceneCreatureResourceInitializer? reentrant=null;
        reentrant=new(reentrantRegistry,(q,t)=>{Reject<InvalidOperationException>(()=>reentrant!.Poll());Reject<InvalidOperationException>(()=>reentrant!.Dispose());return Task.FromResult(Stub(q));});
        using(reentrant){reentrant.Start(reentrantRequest);Check(reentrant.Poll().Single().Kind==SceneCreatureResourceCompletionKind.Prepared,"Reentrant loader cannot publish/poll");}
        Reject<ArgumentOutOfRangeException>(()=>new SceneCreatureResourceInitializer(registry,Load,0));Reject<ArgumentOutOfRangeException>(()=>new SceneCreatureResourceInitializer(registry,Load,4097));Reject<ArgumentOutOfRangeException>(()=>new SceneCreatureResourceInitializer(registry,Load,1,0));
        Reject<ArgumentException>(()=>new SceneCreatureResourceRequest(first,Catalog,Selection,new Dictionary<string,LogicalAnimationClipSelection>()));
        Reject<ArgumentException>(()=>new SceneCreatureResourceRequest(first,Catalog,Selection with{ExplicitChildRoute=new[]{-1}},Bank));
        Reject<ArgumentOutOfRangeException>(()=>new SceneCreatureResourceRequest(first,Catalog,Selection,Bank,0));
        Console.WriteLine("PASS: synthetic initializer barriers; bounded retired/jobs/objects, snapshot/config ownership, exact identity, duplicate/restart, single transfer, every revision/reset/close, late success/fault/cancel, owner/reentrancy and disposal drain");
    }
    internal static void Original(string root,string configuration,string bankPath)
    {
        byte[] cfg=File.ReadAllBytes(configuration);Check(Convert.ToHexString(SHA256.HashData(cfg))=="22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524","Original configuration pin");
        using var bankFile=File.OpenRead(bankPath);var bank=SkeletalAnimationBankProfile.Load(bankFile);
        var entry=new ObjectTemplateCatalogEntry(2937962913,"object/creature/player/shared_human_male.iff","E0B61445E8A85CCF0AC5937F0F9CBCA8711718AFEE63384F30970D5AEE120243","951FCE8C2FFC0EB318C751026D24D8C9BF17263D706EB3C93D015867EC801A9C");
        var catalog=new ObjectTemplateCatalog(new[]{entry});var idle=new LogicalAnimationClipSelection("appearance/lat/all_m.lat","emt_standing_idle_ambient_1_ag","798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B");
        // Synthetic create envelope with a real pinned original template path checksum.
        var create=Create(0xFFEEDDCCBBAA0099) with{TemplateCrc=entry.TemplateCrc};var registry=new SceneObjectRegistry(1,12);var request=new SceneCreatureResourceRequest(registry.Create(12,create)!,catalog,idle,bank);
        Task<SceneCreatureResources>? worker=null;int archivesClosed=0;var reads=new Dictionary<string,int>();int workerThread=0,ownerThread=Environment.CurrentManagedThreadId;
        using var owner=new SceneCreatureResourceInitializer(registry,(q,token)=>worker=Task.Run(()=>
        {
            workerThread=Environment.CurrentManagedThreadId;
            try
            {
                using var reader=new StringReader(Encoding.UTF8.GetString(cfg));using var overlay=ConfiguredTreOverlay.Open(root,reader);
                TreLookupResult Resolve(string path){reads[path]=reads.GetValueOrDefault(path)+1;return overlay.Lookup(path);}
                return SceneCreatureResourceReader.Load(q.Snapshot.Create,q.Catalog,q.Idle,q.Movement,Resolve,token,q.MaximumResources,q.MaximumSourceBytes);
            }
            finally{Interlocked.Exchange(ref archivesClosed,1);}
        }));
        var ticket=owner.Start(request);worker!.GetAwaiter().GetResult();
        // Wait on the actual injected task, not a sleep or a separate early notification.
        var completion=owner.Poll().Single();
        Check(completion.Kind==SceneCreatureResourceCompletionKind.Prepared&&completion.Ticket==ticket,"Pinned original result transfer");var result=completion.Resources!;
        Check(archivesClosed==1&&workerThread!=ownerThread&&reads.Count==37&&reads.Values.All(n=>n==1)&&result.ExternalReads==37&&result.SourceBytes==4683848,"Original archive closure/readonce");
        Check(result.Creature.PreparedAnimation is not null&&result.Creature.AdditionalAnimations!.Count==2&&result.Creature.AdditionalAnimations.ContainsKey("walk")&&result.Creature.AdditionalAnimations.ContainsKey("run"),"Original three clips");
        Check(SceneObjectLifecycleCodec.EncodeCreate(result.Create).SequenceEqual(SceneObjectLifecycleCodec.EncodeCreate(create))&&owner.Poll().Count==0,"Original identity/single transfer");
        foreach(var source in result.Creature.Sources.OrderBy(p=>p.Key))Console.WriteLine($"source {source.Key} {source.Value.Sha256} {source.Value.Bytes}");
        Console.WriteLine("PASS: pinned original initializer job;37 read-once sources/4683848 bytes, archives closed before owner transfer, three clips, exact create identity; synthetic packet, modern resource barrier");
    }
}
