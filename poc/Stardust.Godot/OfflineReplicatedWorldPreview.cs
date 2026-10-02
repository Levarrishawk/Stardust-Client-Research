using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

/// <summary>Bounded independently framed offline peer; existing opaque driver remains unchanged.</summary>
public sealed class ReplicatedWorldPeer : IDisposable
{
    private readonly SceneWorldCoordinator world;
    private readonly Dictionary<string,string> peer;
    private SoeClientTransport? transport;
    private readonly List<string> delivered=new();
    private string request="",select="",ready="",retry="";
    public long Generation{get;private set;}
    public ulong ObjectId{get;}
    public ReplicatedWorldPeer(SceneWorldCoordinator world,IReadOnlyDictionary<string,string> peer)
    {
        if(peer.Count>64 || peer.Values.Any(v=>v.Length>4096))throw new ArgumentException("Bounded peer fixture required.");
        this.world=world;this.peer=peer.ToDictionary(p=>p.Key,p=>p.Value);ObjectId=SceneObjectLifecycleCodec.DecodeCreate(Convert.FromHexString(peer["create"])).ObjectId;
    }
    public void SelectAndStage()
    {
        transport?.Close();transport=new();delivered.Clear();ready=retry="";
        var selection=world.BeginSelection(ObjectId);Generation=selection.Generation;
        request=Convert.ToHexString(transport.BeginSession(0x12345678,496));transport.AcceptSessionResponse(Convert.FromHexString(peer["response"]));
        select=Convert.ToHexString(transport.CreateReliableDatagram(selection.CopySelectMessage(),0));Check(select==peer["expectedSelect"],"Select wire");Receive("ackSelect");
        byte[] bad=Convert.FromHexString(peer["peer_create"]);bad[^1]^=1;Check(transport.AcceptDatagram(bad).RejectedFraming && world.State==ZoneSceneTransitionState.Selecting,"CRC isolation");
        Receive("peer_create");Check(world.StagedCount==0,"Reliable reordering");Receive("peer_start");Receive("peer_start");
        foreach(string name in new[]{"baseline","delta","end","contain"})Receive("peer_"+name);
        Check(world.State==ZoneSceneTransitionState.Initializing && world.StagedCount==5 && world.Snapshots().Count==0 && world.OutstandingJobs==0,"Explicit barrier staging");
    }
    public void OpenBarrier()
    {
        Check(world.CompleteMinimumSceneBarrier(Generation,false)is null && world.CompleteMinimumSceneBarrier(Generation-1,true)is null,"False/stale barrier");
        var application=world.CompleteMinimumSceneBarrier(Generation,true)!;var snapshot=world.Snapshots().Single();
        Check(snapshot.Object.Phase==SceneObjectPhase.Finalized && snapshot.Replication is not null && snapshot.Envelopes.Count==0,"Typed finalized object");
        ready=Convert.ToHexString(transport!.CreateReliableDatagram(application,100));Check(ready==peer["expectedReady"],"Ready wire");
        retry=Convert.ToHexString(transport.GetRetransmissionDatagrams(200,100).Single());Check(retry==ready,"Retained ready retry");Array.Clear(application);
        Check(world.CompleteMinimumSceneBarrier(Generation,true)is null,"One-shot barrier");
    }
    public void ServerReady(){Receive("ackReady");Receive("peer_serverReady");Check(world.State==ZoneSceneTransitionState.Ready && transport!.RetainedOutboundPackets==0,"Ready/ACK");}
    public void Receive(string key)
    {
        var wire=Convert.FromHexString(peer[key]);var result=transport!.AcceptDatagram(wire);
        foreach(byte[] app in result.ApplicationPayloads){delivered.Add(Convert.ToHexString(app));if(delivered.Count>32)delivered.RemoveAt(0);world.Accept(Generation,app);Array.Clear(app);}Array.Clear(wire);
    }
    public void Apply(string key){var bytes=Convert.FromHexString(peer[key]);world.Accept(Generation,bytes);Array.Clear(bytes);}
    public void Reset()=>Generation=world.BeginSelection(ObjectId).Generation;
    public object Export()=>new{SessionRequest=request,SelectDatagram=select,ReadyDatagram=ready,Retransmission=retry,DeliveredApplications=delivered.ToArray(),State=world.State.ToString(),RetainedPackets=transport?.RetainedOutboundPackets??0};
    public void Dispose(){transport?.Close();transport=null;}
    private static void Check(bool ok,string detail){if(!ok)throw new InvalidDataException("Replicated world peer: "+detail);}
}

public static class ReplicatedWorldEvidence
{
    public static void Write(SceneCreatureResources source,Action<string> print)
    {
        foreach(var row in source.Creature.Sources.Values)print("source="+JsonSerializer.Serialize(row));
        var materials=source.Creature.MaterialBundle!;var factors=materials.AlbedoFactors??throw new InvalidDataException("Explicit albedo factors required.");
        foreach(var pair in factors.Materials)
        {
            print("factor="+JsonSerializer.Serialize(new{path=pair.Key,factors=pair.Value}));
            string path=materials.Materials[pair.Key].References.Textures.Single(t=>t.Tag==0x4D41494E).AssetPath!;
            var rgba=materials.Textures[path].Mips[0].Rgba.Span;var sample=new System.Numerics.Vector4(rgba[0]/255f,rgba[1]/255f,rgba[2]/255f,rgba[3]/255f);
            var color=SkeletalAlbedoEvaluator.Evaluate(sample,pair.Value);
            print("sample="+JsonSerializer.Serialize(new{shader=pair.Key,texture=path,pixel=0,rgba=Convert.ToHexString(rgba[..4]),color=new[]{color.X,color.Y,color.Z}}));
        }
        foreach(var pair in factors.Programs)print("program="+JsonSerializer.Serialize(new{path=pair.Key,pair.Value.UsesAlphaHueMask,bytecodeSha256=Convert.ToHexString(SHA256.HashData(pair.Value.Metadata.Children[0].Children[1].Payload.Span))}));
        foreach(var pair in materials.Textures)print("texture="+JsonSerializer.Serialize(new{path=pair.Key,mips=pair.Value.Mips.Select(m=>Convert.ToHexString(SHA256.HashData(m.Rgba.Span))).ToArray()}));
    }
    public static SceneWorldReplicationPolicy Policy(bool mixed=false)=>new(mixed?SceneWorldReplicationMode.CreatureView3Mixed:SceneWorldReplicationMode.CreatureView3Scalars,new(1024,64,64,64,4096),16,4096,mixed?new CreatureView3MixedDeltaLimits(1024,16,16,64,64,64):null,mixed?CreatureWoundsApplicationPolicy.LegacyBounded:CreatureWoundsApplicationPolicy.StrictInterval);
    public static Transform3D BaseTransform(ScenePreparedCreaturePublication publication)=>GodotAppearanceTransform.ToGodot(publication.Placement.Transform);
}

public partial class OfflineReplicatedWorldPreview : Node
{
    private SceneWorldCoordinator world=null!;
    private ReplicatedWorldPeer driver=null!;
    private SceneWorldCreatureStatePresentation? actor;
    private SceneWorldPresentationConsumer? consumer;
    private byte[]? paletteProfile;
    private SceneCreatureResources? authoredColors;
    private ObjectTemplateCatalog catalog=null!;
    private IReadOnlyDictionary<string,LogicalAnimationClipSelection> bank=null!;
    private IReadOnlyDictionary<string,string> peer=null!;
    private readonly LogicalAnimationClipSelection idle=new("appearance/lat/all_m.lat","emt_standing_idle_ambient_1_ag","798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B");
    private readonly List<Task<SceneCreatureResources>> workers=new();
    private readonly TaskCompletionSource<bool> first=new(TaskCreationOptions.RunContinuationsAsynchronously);
    private byte[] configuration=Array.Empty<byte>();
    private string root="";
    private int closed,workerThread;
    private readonly int ownerThread=System.Environment.CurrentManagedThreadId;
    private bool disposed,cancel,mixedReplication;
    private Godot.Environment? environment;
    private Label? status;
    internal async Task InitializeAsync(string[] args,byte[]? paletteProfileBytes=null)
    {
        string Read(string key){int at=Array.IndexOf(args,key);if(at<0 || at+1>=args.Length || args.Count(v=>v==key)!=1)throw new ArgumentException("Missing/duplicate replicated-world option.");return args[at+1];}
        if(args.Count(v=>v=="--replicated-mixed")>1)throw new ArgumentException("Duplicate mixed replication flag.");
        mixedReplication=args.Contains("--replicated-mixed");
        paletteProfile=paletteProfileBytes?.ToArray();
        GetTree().AutoAcceptQuit=false;root=Read("--asset-root");configuration=File.ReadAllBytes(Read("--asset-config"));
        if(Convert.ToHexString(SHA256.HashData(configuration))!=RetainedSkeletalDetailBundleReader.ConfigurationPin)throw new InvalidDataException("Configuration pin differs.");
        string path=Read("--peer-fixture");if(new FileInfo(path).Length>65536)throw new ArgumentException("Peer file budget.");
        peer=JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllText(path))!;
        catalog=ObjectTemplateCatalogProfile.Decode(File.ReadAllBytes(Read("--catalog-profile")));
        using(var profile=File.OpenRead(Path.Combine(ProjectSettings.GlobalizePath("res://"),"..","preview-profiles","human-locomotion.json")))bank=SkeletalAnimationBankProfile.Load(profile);
        cancel=args.Contains("--replicated-cancel");bool test=args.Contains("--replicated-self-test"),headless=DisplayServer.GetName()=="headless";
        string? capture=args.Contains("--replicated-capture")?Read("--replicated-capture"):null;
        if(cancel && (test || capture is not null || !headless) || capture is not null && (test || headless) || headless && !test && !cancel)throw new ArgumentException("Separate bounded replicated-world modes.");
        if(capture is not null)
        {
            string prefix=Path.GetFullPath(Path.Combine(ProjectSettings.GlobalizePath("res://"),"..","..","local-private"))+Path.DirectorySeparatorChar;
            foreach(string image in paletteProfile is null?new[]{capture,SecondCapture(capture)}:new[]{capture,SecondCapture(capture),ColorCapture(capture)})if(!Path.GetFullPath(image).StartsWith(prefix,StringComparison.OrdinalIgnoreCase) || Path.GetExtension(image)!=".png" || File.Exists(image) || !Directory.Exists(Path.GetDirectoryName(Path.GetFullPath(image))))throw new ArgumentException("Fresh private capture set required.");
        }
        world=MakeWorld(LoadOriginal);driver=new(world,peer);driver.SelectAndStage();driver.OpenBarrier();
        GD.Print("replicated_phase=Finalized state=AwaitingServerReady prepared=false");
        if(world.PollResources().Prepared.Count!=0 || workers.Count!=1)throw new AssetPreviewException("Finalized resource job differs.");
        var worker=workers.Single();
        if(cancel){if(await Task.WhenAny(first.Task,worker)!=first.Task)throw new AssetPreviewException("Cancel barrier absent.");driver.Reset();}
        try{await worker;}catch(OperationCanceledException)when(cancel){}
        if(disposed || !IsInsideTree())return;
        var poll=world.PollResources();workers.RemoveAll(t=>t.IsCompleted);
        if(cancel)
        {
            if(poll.Prepared.Count!=0 || poll.Retired.Count!=1 || actor is not null || closed!=1)throw new AssetPreviewException("Canceled world published/leaked.");
            await ShutdownAsync();GD.Print("PASS: replicated canceled generation_reset=true archives_closed=true publications=0 drain_complete=true");GetTree().Quit();return;
        }
        var publication=poll.Prepared.Single();var source=publication.Resources;
        if(closed!=1 || workerThread==ownerThread || System.Environment.CurrentManagedThreadId!=ownerThread || workers.Count!=0 || source.ExternalReads!=39 || source.SourceBytes!=4686371)throw new AssetPreviewException("Original albedo worker transfer differs.");
        var snapshot=world.FindReplication(driver.Generation,driver.ObjectId)!;var basis=ReplicatedWorldEvidence.BaseTransform(publication);
        consumer=new(world,this,1);
        if(!consumer.AcceptResource(publication))throw new AssetPreviewException("World state resource publication failed.");
        actor=consumer.Find(driver.ObjectId)!;
        WriteMixedState("initial",snapshot);
        WriteBatch("initial");if(!consumer.ConsumePending())throw new AssetPreviewException("Initial observer apply/ack failed.");
        actor.Visual.SeekIdle(0);actor.Visual.SetPlaying(false);
        GD.Print("replicated_phase=Prepared state=AwaitingServerReady finalized=true");driver.ServerReady();GD.Print("replicated_phase=Ready finalized=true prepared=true");
        ReplicatedWorldEvidence.Write(source,s=>GD.Print(s));
        WriteColorChoice("initial",source);
        if(paletteProfile is not null)GD.Print("observer_profile_sha="+Convert.ToHexString(SHA256.HashData(paletteProfile)));
        GD.Print("state_create="+Convert.ToHexString(SceneObjectLifecycleCodec.EncodeCreate(source.Create)));
        GD.Print("state_base="+JsonSerializer.Serialize(CreatureStatePreview.PoseEvidence(basis)));
        GD.Print("replicated_baseline="+Convert.ToHexString(CreatureView3BaselineCodec.Encode(snapshot.Baseline,ReplicatedWorldEvidence.Policy().BaselineLimits)));
        environment=new(){BackgroundMode=Godot.Environment.BGMode.Color,BackgroundColor=new Color("#14202e")};AddChild(new WorldEnvironment{Environment=environment});
        var bounds=SceneCreatureStatePresentation.DeriveTransform(basis,1.5f)*actor.Visual.Bounds;Vector3 target=bounds.GetCenter();
        var camera=new Camera3D{Current=true,Fov=45,VOffset=.35f,Position=target+new Vector3(-.8f,.35f,1).Normalized()*Math.Max(bounds.Size.Length()*1.7f,2)};AddChild(camera);camera.LookAt(target);
        var layer=new CanvasLayer();AddChild(layer);var panel=new ColorRect{Color=new(.02f,.04f,.08f,.92f),OffsetRight=1280,OffsetBottom=155};layer.AddChild(panel);
        panel.AddChild(new Label{Text="STARDUST | OFFLINE REPLICATED WORLD",Position=new(24,12)});status=new Label{Position=new(24,44)};panel.AddChild(status);
        panel.AddChild(new Label{Text="Explicit barrier | typed CREO:3 | original authored albedo | modern Height [0.25,2] | fixed camera",Position=new(24,120)});Refresh(snapshot);
        if(test || capture is not null)
        {
            await VerifyAsync(source,!headless);
            await VerifyObserverAsync(source,!headless);
            string retained=actor.Visual.Fingerprint(gpu:!headless)!;int allocations=SceneCreatureVisual.Allocations;var fixedCamera=camera.GlobalTransform;
            VerifyAlbedo(source);
            Image? before=capture is null?null:await Capture(capture);
            driver.Receive("peer_height");var updated=world.FindReplication(driver.Generation,driver.ObjectId)!;
            WriteBatch("height");
            if(!consumer.ConsumePending() || !actor.Tick(0) || actor.Apply(snapshot) || actor.AppliedRevision!=updated.Revision || !world.IsCurrent(publication))throw new AssetPreviewException("Observer state-only update retired resources/accepted stale state.");
            WriteMixedState("height",updated);
            Refresh(updated);GD.Print("state_height="+JsonSerializer.Serialize(CreatureStatePreview.PoseEvidence(actor.Visual.Root.Transform)));
            if(capture is not null)
            {
                using(before)using(var after=await Capture(SecondCapture(capture)))
                {int changed=0;for(int y=180;y<before!.GetHeight();y++)for(int x=0;x<before.GetWidth();x++)if(before.GetPixel(x,y)!=after.GetPixel(x,y))changed++;
                 if(changed<100 || camera.GlobalTransform!=fixedCamera)throw new AssetPreviewException("Actual actor pair/fixed camera differs.");GD.Print($"PASS: replicated GPU pair actor_changed_pixels={changed} fixed_camera=true UI_excluded_y=180 mip_readback=true albedo_parameters=true");}
            }
            if(actor.Visual.Fingerprint(gpu:!headless)!=retained || SceneCreatureVisual.Allocations!=allocations)throw new AssetPreviewException("World state update replaced GPU IDs.");
            if(authoredColors is not null)
            {
                if(!actor.ApplyPalette(authoredColors,updated) || !actor.Verify(!headless))throw new AssetPreviewException("Prepared palette-only revision failed.");
                WriteColorChoice("authored",authoredColors);VerifyAlbedo(authoredColors,"authored");
                if(capture is not null){using var color=await Capture(ColorCapture(capture));}
                if(actor.Visual.Fingerprint(gpu:!headless)!=retained || SceneCreatureVisual.Allocations!=allocations || !actor.ApplyPalette(source,updated) || !actor.Verify(!headless))throw new AssetPreviewException("Palette revision replaced GPU resources.");
                GD.Print("PASS: observer palette off_thread=true owned_profile=true source_reads=39 no_extra_reads=true GPU_ids_fixed=true choices=2");
            }
            driver.Receive("peer_hidden");var hidden=world.FindReplication(driver.Generation,driver.ObjectId)!;WriteBatch("hidden");if(!consumer.ConsumePending() || actor.Visual.Root.Visible)throw new AssetPreviewException("Observer visibility zero differs.");
            driver.Receive("peer_shown");WriteBatch("shown");if(!consumer.ConsumePending() || !actor.Visual.Root.Visible || actor.Visual.Fingerprint(gpu:!headless)!=retained || world.DrainChanges(driver.Generation)!.Entries.Count!=0)throw new AssetPreviewException("Observer nonzero visibility/ack/GPU retention differs.");
            GD.Print("replicated_wire="+JsonSerializer.Serialize(driver.Export()));
            GD.Print("PASS: replicated state_revision_separate=true resource_ids_fixed=true visibility=0,255 original39=true");
            await ShutdownAsync();GD.Print("PASS: replicated teardown roots_released=true drain_complete=true outstanding=0 transport_closed=true");GetTree().Quit();return;
        }
        void Button(string label,int x,Action action){var b=new Button{Text=label,Position=new(x,78)};panel.AddChild(b);b.Pressed+=()=>{try{action();Refresh(world.FindReplication(driver.Generation,driver.ObjectId)!);}catch(Exception e){GD.PushError(e.GetType().Name);GetTree().Quit(1);}};}
        Button("Height 1.5",24,()=>{driver.Apply("height");consumer.ConsumePending();});
        Button("Reset baseline",170,()=>{driver.Apply("baseline");consumer.ConsumePending();});
        Button("Hide",340,()=>{driver.Apply("hidden");consumer.ConsumePending();});
        if(authoredColors is not null)
        {
            Button("Authored colors",420,()=>actor.ApplyPalette(authoredColors,world.FindReplication(driver.Generation,driver.ObjectId)!));
            Button("Profile colors",580,()=>actor.ApplyPalette(source,world.FindReplication(driver.Generation,driver.ObjectId)!));
        }
    }
    private SceneWorldCoordinator MakeWorld(Func<SceneCreatureResourceRequest,CancellationToken,Task<SceneCreatureResources>> loader)=>new(catalog,idle,bank,loader,SceneCreateZeroQuaternionPolicy.Reject,maximumObjects:1,maximumOutstandingJobs:2,maximumStagedMessages:8,maximumEnvelopes:2,maximumEnvelopeBytes:56,maximumPayloadBytes:1024,replicationPolicy:ReplicatedWorldEvidence.Policy(mixedReplication));
    private Task<SceneCreatureResources> LoadOriginal(SceneCreatureResourceRequest request,CancellationToken token)
    {
        var config=configuration.ToArray();var profile=paletteProfile?.ToArray();string archive=root;
        var task=Task.Run(()=>
        {
            workerThread=System.Environment.CurrentManagedThreadId;var reads=new HashSet<string>();
            try
            {
                IReadOnlyDictionary<ShaderCustomizationKey,int>? colors=null;
                if(profile is not null){using var stream=new MemoryStream(profile,false);colors=SceneCreaturePaletteCustomization.LoadProfile(stream);}
                using var text=new StringReader(System.Text.Encoding.UTF8.GetString(config));using var overlay=ConfiguredTreOverlay.Open(archive,text);
                TreLookupResult Resolve(string path){token.ThrowIfCancellationRequested();if(cancel && first.TrySetResult(true)){token.WaitHandle.WaitOne(TimeSpan.FromSeconds(10));token.ThrowIfCancellationRequested();throw new InvalidOperationException("Cancel timeout.");}if(!reads.Add(path))throw new InvalidDataException("Repeated original resolver read.");return overlay.Lookup(path);}
                var source=SceneCreatureResourceReader.Load(request.Snapshot.Create,request.Catalog,request.Idle,request.Movement,Resolve,token,loadAlbedoFactors:true);
                if(colors is null)return source;
                var customized=SceneCreaturePaletteCustomization.Apply(source,colors,token);
                if(reads.Count!=39 || !ReferenceEquals(source.Creature.Sources,customized.Creature.Sources))throw new InvalidDataException("Palette revision read extra sources.");
                authoredColors=source;return customized;
            }
            finally{Interlocked.Increment(ref closed);}
        },token);workers.Add(task);return task;
    }
    private void VerifyAlbedo(SceneCreatureResources source,string choice="initial")
    {
        int index=0,surfaces=0;var boundShaders=new HashSet<string>();
        var meshRoot=actor!.Visual.Root.GetChild(0);
        foreach(string generator in source.Creature.Appearance.MeshGenerators)
        {
            string path=source.Creature.LodMeshes[generator].ExposedPaths()[0];var mesh=(MeshInstance3D)meshRoot.GetChild(index++);
            for(int surface=0;surface<mesh.Mesh.GetSurfaceCount();surface++)
            {
                string shader=source.Creature.Geometries![path].Shaders[surface].ShaderPath;var expected=source.Creature.MaterialBundle!.AlbedoFactors!.Materials[shader];var material=(ShaderMaterial)mesh.Mesh.SurfaceGetMaterial(surface);
                Vector3 Color(uint value)=>new(((value>>16)&255)/255f,((value>>8)&255)/255f,(value&255)/255f);
                var firstFactor=material.GetShaderParameter("first_factor").AsVector3();var secondFactor=material.GetShaderParameter("second_factor").AsVector3();bool mask=material.GetShaderParameter("alpha_hue_mask").AsBool();
                if(firstFactor!=Color(expected.FirstArgb) || secondFactor!=Color(expected.SecondArgb) || mask!=expected.UsesAlphaHueMask)throw new AssetPreviewException("GPU albedo parameter binding differs.");
                GD.Print("gpu_factor="+JsonSerializer.Serialize(new{choice,path=shader,first=new[]{firstFactor.X,firstFactor.Y,firstFactor.Z},second=new[]{secondFactor.X,secondFactor.Y,secondFactor.Z},mask}));surfaces++;boundShaders.Add(shader);
            }
        }
        int expectedSurfaces=source.Creature.Appearance.MeshGenerators.Sum(g=>source.Creature.Geometries![source.Creature.LodMeshes[g].ExposedPaths()[0]].Shaders.Count);
        if(surfaces!=expectedSurfaces || boundShaders.Count!=4)throw new AssetPreviewException("Original surface/material albedo coverage differs.");
    }
    private void Refresh(SceneWorldReplicationSnapshot state){status!.Text=$"Ready | generation {state.Generation} revision {state.Revision} | Height {state.Scalars.Height} visibility {state.Baseline.Visibility} | 39 original sources / 10 textures";if(mixedReplication)status.Text+=$" | wounds {state.Wounds!.Values.Count} counter {state.Wounds.Counter}";}
    private static string SecondCapture(string path)=>Path.Combine(Path.GetDirectoryName(path)!,Path.GetFileNameWithoutExtension(path)+"-frame1.png");
    private static string ColorCapture(string path)=>Path.Combine(Path.GetDirectoryName(path)!,Path.GetFileNameWithoutExtension(path)+"-authored.png");
    private async Task<Image> Capture(string path){for(int i=0;i<3;i++)await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);actor!.Verify(true);var image=GetViewport().GetTexture().GetImage();if(image.SavePng(path)!=Error.Ok){image.Dispose();throw new IOException("Replicated capture failed.");}return image;}
    private void WriteMixedState(string stage,SceneWorldReplicationSnapshot state)
    {
        if(!mixedReplication)return;
        if(!world.IsCurrent(state) || state.Wounds is not { } wounds)throw new InvalidDataException("Current mixed state required.");
        GD.Print("mixed_state="+JsonSerializer.Serialize(new{stage,Mode=world.ReplicationMode.ToString(),ObjectId=state.Incarnation.ObjectId,state.Revision,HeightBits=BitConverter.SingleToUInt32Bits(state.Scalars.Height),wounds.Counter,wounds.Values}));
    }
    private void WriteBatch(string stage)=>GD.Print("observer_batch="+JsonSerializer.Serialize(BatchEvidence(stage,world.DrainChanges(driver.Generation)!)));
    public static object BatchEvidence(string stage,SceneWorldChangeBatch batch)=>new{stage,batch.Generation,batch.MutationRevision,entries=batch.Entries.Select(e=>new{e.ObjectId,kind=(int)e.Kind,current=e.Current is null?null:new{incarnation=e.Current.Object.Incarnation.CreationRevision,handle=e.Current.Object.Handle.Revision,revision=e.Current.Replication?.Revision,heightBits=e.Current.Replication is null?(uint?)null:BitConverter.SingleToUInt32Bits(e.Current.Replication.Scalars.Height),visibility=e.Current.Replication?.Baseline.Visibility}}).ToArray()};
    public static void WriteColorChoice(string name,SceneCreatureResources source,Action<string>? print=null)
    {
        print??=s=>GD.Print(s);var material=source.Creature.MaterialBundle!;
        print("observer_color="+JsonSerializer.Serialize(new{name,values=source.Creature.CustomizationValues?.Select(p=>new{p.Key.VariableName,p.Key.Private,value=p.Value}).ToArray(),factors=material.AlbedoFactors!.Materials.Select(p=>new{path=p.Key,factors=p.Value}).ToArray(),selected=material.PaletteDefaults!.SelectMany(p=>p.Value.AuthoredFactors.Select(f=>new{shader=p.Key,f.VariableName,f.Private,f.PalettePath,f.RequestedIndex,f.UsedAuthoredDefault,f.Color})).ToArray()}));
    }
    private async Task VerifyAsync(SceneCreatureResources source,bool gpu)
    {
        var jobs=new List<(TaskCompletionSource<SceneCreatureResources> Gate,CancellationToken Token)>();
        Task<SceneCreatureResources> Controlled(SceneCreatureResourceRequest _,CancellationToken token){var gate=new TaskCompletionSource<SceneCreatureResources>(TaskCreationOptions.RunContinuationsAsynchronously);jobs.Add((gate,token));return gate.Task;}
        using var owner=MakeWorld(Controlled);using var d=new ReplicatedWorldPeer(owner,peer);d.SelectAndStage();d.OpenBarrier();owner.PollResources();jobs[^1].Gate.SetResult(source);var p=owner.PollResources().Prepared.Single();var seed=owner.FindReplication(d.Generation,d.ObjectId)!;var basis=ReplicatedWorldEvidence.BaseTransform(p);
        using var visual=SceneWorldCreatureStatePresentation.TryPublish(owner,p,seed,basis,this)!;string retained=visual.Visual.Fingerprint(gpu:gpu)!;var original=visual.Visual.Root.Transform;
        foreach(string key in new[]{"zero","negativeZero","negative","small","large"})
        {d.Apply(key);var state=owner.FindReplication(d.Generation,d.ObjectId)!;bool rejected=false;try{visual.Apply(state);}catch(ArgumentOutOfRangeException){rejected=true;}if(!rejected || !owner.IsCurrent(state) || visual.Visual.Root.Transform!=original || visual.Visual.Fingerprint(gpu:gpu)!=retained)throw new AssetPreviewException("Unsupported raw height mutated visual/state.");}
        foreach(string key in new[]{"minimum","maximum"}){d.Apply(key);if(!visual.Apply(owner.FindReplication(d.Generation,d.ObjectId)!))throw new AssetPreviewException("Height boundary rejected.");}
        d.Apply("baseline");var stateBefore=owner.FindReplication(d.Generation,d.ObjectId)!;
        if(visual.Apply(stateBefore,()=>d.Apply("height")) || !visual.Tick(0))throw new AssetPreviewException("Application freshness recheck differs.");
        using(var foreign=MakeWorld((_,_)=>Task.FromResult(source)))using(var f=new ReplicatedWorldPeer(foreign,peer))
        {f.SelectAndStage();f.OpenBarrier();foreign.PollResources();foreign.PollResources();if(visual.Apply(foreign.FindReplication(f.Generation,f.ObjectId)!) || SceneWorldCreatureStatePresentation.Matches(foreign,p,owner.FindReplication(d.Generation,d.ObjectId)!))throw new AssetPreviewException("Foreign world accepted.");}
        ulong oldRoot=visual.Visual.Root.GetInstanceId();owner.RestartResource(d.Generation,d.ObjectId);if(visual.Tick(0) || GodotObject.InstanceFromId(oldRoot)is not null)throw new AssetPreviewException("Restart kept old resource visual.");
        int allocations=SceneCreatureVisual.Allocations;if(SceneWorldCreatureStatePresentation.TryPublish(owner,p,seed,basis,this)is not null || allocations!=SceneCreatureVisual.Allocations)throw new AssetPreviewException("Stale preflight allocated.");
        owner.PollResources();jobs[^1].Gate.SetResult(source);p=owner.PollResources().Prepared.Single();
        foreach(bool attach in new[]{false,true})
        {
            var state=owner.FindReplication(d.Generation,d.ObjectId)!;int disposals=SceneCreatureVisual.Disposals;ulong rootId=0;
            void OnAttach(Node node){if(node is Node3D n){if(n.Visible)throw new AssetPreviewException("Staged root visible.");rootId=n.GetInstanceId();d.Apply("height");}}
            if(attach)ChildEnteredTree+=OnAttach;
            try{if(SceneWorldCreatureStatePresentation.TryPublish(owner,p,state,basis,this,attach?null:()=>d.Apply("height"))is not null)throw new AssetPreviewException("Stale staged state published.");}
            finally{if(attach)ChildEnteredTree-=OnAttach;}
            if(SceneCreatureVisual.Disposals!=disposals+1 || attach && (rootId==0 || GodotObject.InstanceFromId(rootId)is not null))throw new AssetPreviewException("State staging leaked.");
        }
        var oldState=owner.FindReplication(d.Generation,d.ObjectId)!;d.Apply("destroy");d.Apply("create");d.Apply("baseline");d.Apply("end");owner.PollResources();jobs[^1].Gate.SetResult(source);var replacement=owner.PollResources().Prepared.Single();
        using(var fresh=SceneWorldCreatureStatePresentation.TryPublish(owner,replacement,owner.FindReplication(d.Generation,d.ObjectId)!,ReplicatedWorldEvidence.BaseTransform(replacement),this)!)if(fresh.Apply(oldState) || !fresh.Tick(0))throw new AssetPreviewException("Old incarnation affected recreated actor.");
        long generation=d.Generation;d.Reset();if(owner.Accept(generation,new byte[]{1}) || owner.IsCurrent(oldState) || owner.IsCurrent(replacement))throw new AssetPreviewException("Old generation survived.");
        // Cancellation-ignoring work retains capacity; late success and late fault are observed.
        d.SelectAndStage();d.OpenBarrier();owner.PollResources();var success=jobs[^1];owner.RestartResource(d.Generation,d.ObjectId);owner.PollResources();var fault=jobs[^1];owner.RestartResource(d.Generation,d.ObjectId);owner.PollResources();
        if(owner.OutstandingJobs!=2 || !success.Token.IsCancellationRequested || !fault.Token.IsCancellationRequested)throw new AssetPreviewException("Canceled jobs escaped capacity.");
        success.Gate.SetResult(source);var retired=owner.PollResources();if(retired.Prepared.Count!=0 || retired.Retired.Count!=1 || retired.Retired[0].Resources is not null)throw new AssetPreviewException("Late success resurrected.");
        var pending=jobs[^1];fault.Gate.SetException(new IOException("Synthetic stale fault"));if(owner.PollResources().Retired.Count!=1)throw new AssetPreviewException("Late failure unobserved.");
        owner.Close();if(owner.DrainCompletion.IsCompleted || !pending.Token.IsCancellationRequested)throw new AssetPreviewException("Close failed to retain unfinished work.");pending.Gate.SetException(new IOException("Synthetic close fault"));await owner.DrainCompletion;
        if(owner.OutstandingJobs!=0)throw new AssetPreviewException("World close drain incomplete.");
        using(var badWorld=MakeWorld((_,_)=>Task.FromResult(source)))using(var badPeer=new ReplicatedWorldPeer(badWorld,peer))
        {
            badPeer.SelectAndStage();badPeer.OpenBarrier();badWorld.PollResources();var bp=badWorld.PollResources().Prepared.Single();var bs=badWorld.FindReplication(badPeer.Generation,badPeer.ObjectId)!;
            using var ba=SceneWorldCreatureStatePresentation.TryPublish(badWorld,bp,bs,ReplicatedWorldEvidence.BaseTransform(bp),this)!;bool rejected=false;
            try{badPeer.Apply("bad");}catch(InvalidDataException){rejected=true;}
            if(!rejected || ba.Tick(0) || badWorld.IsCurrent(bs) || badWorld.RetainedReplicationDecodedBytes!=0)throw new AssetPreviewException("Malformed generation survived.");
            // Failure resets the registry without advancing the external selection generation.
            badPeer.SelectAndStage();badPeer.OpenBarrier();badWorld.PollResources();var shifted=badWorld.PollResources().Prepared.Single();var current=badWorld.FindReplication(badPeer.Generation,badPeer.ObjectId)!;
            if((ulong)shifted.Generation==shifted.Ticket.Handle.SceneGeneration)throw new AssetPreviewException("Epoch independence fixture did not discriminate.");
            using var independent=SceneWorldCreatureStatePresentation.TryPublish(badWorld,shifted,current,ReplicatedWorldEvidence.BaseTransform(shifted),this);
            if(independent is null || !independent.Tick(0))throw new AssetPreviewException("World generation was equated with registry epoch.");
        }
        GD.Print("PASS: replicated invalid_heights=5 bounds=2 foreign_world=true application_recheck=true restart_attempt=true stale_preflight=true staged_cleanup=true post_attach_cleanup=true destroy_recreate=true stale_generation=true capacity2=true late_success_fault=true close_drain=true malformed_generation=true epoch_independent=true");
    }
    private async Task VerifyObserverAsync(SceneCreatureResources source,bool gpu)
    {
        using var owner=MakeWorld((_,_)=>Task.FromResult(source));using var d=new ReplicatedWorldPeer(owner,peer);
        d.SelectAndStage();d.OpenBarrier();owner.PollResources();var publication=owner.PollResources().Prepared.Single();
        using var presenter=new SceneWorldPresentationConsumer(owner,this,1);
        var initial=owner.DrainChanges(d.Generation)!;
        if(initial.Entries.Count!=1 || (int)initial.Entries[0].Kind!=3 || !presenter.AcceptResource(publication) || !presenter.ConsumePending() || owner.DrainChanges(d.Generation)!.Entries.Count!=0)throw new AssetPreviewException("Coalesced initial observer apply/ack differs.");
        var shown=presenter.Find(d.ObjectId)!;var state=owner.FindReplication(d.Generation,d.ObjectId)!;string fingerprint=shown.Visual.Fingerprint(gpu:gpu)!;
        var threadBatch=owner.DrainChanges(d.Generation)!;int wrongThreads=0;
        foreach(Action operation in new Action[]{()=>presenter.ConsumePending(),()=>presenter.ApplyBatch(threadBatch),()=>presenter.AcknowledgeAppliedBatch(threadBatch)})
            try{await Task.Run(operation);}catch(InvalidOperationException){wrongThreads++;}
        if(wrongThreads!=3 || !presenter.AcknowledgeAppliedBatch(threadBatch) || presenter.Count!=1 || shown.Visual.Fingerprint(gpu:gpu)!=fingerprint)throw new AssetPreviewException("Observer consumer foreign thread mutated ownership/ack metadata.");
        bool rejected=false;try{shown.ApplyPalette(source with{Create=source.Create with{}},state);}catch(InvalidDataException){rejected=true;}
        if(!rejected || shown.Visual.Fingerprint(gpu:gpu)!=fingerprint)throw new AssetPreviewException("Foreign palette closure mutated visual.");
        d.Apply("height");var held=owner.DrainChanges(d.Generation)!;
        if(!presenter.ApplyBatch(held))throw new AssetPreviewException("Prepared observer batch failed application.");
        d.Apply("shown");
        if(presenter.AcknowledgeAppliedBatch(held) || owner.DrainChanges(d.Generation)!.Entries.Count!=1 || !presenter.ConsumePending() || owner.DrainChanges(d.Generation)!.Entries.Count!=0)throw new AssetPreviewException("Stale ack lost pending ID or retry failed.");
        d.Apply("negativeZero");var bad=owner.FindReplication(d.Generation,d.ObjectId)!;var before=shown.Visual.Root.Transform;
        if(presenter.ConsumePending() || presenter.RejectedHeightBits!=0x80000000 || !owner.IsCurrent(bad) || owner.DrainChanges(d.Generation)!.Entries.Count!=1 || shown.Visual.Root.Transform!=before)throw new AssetPreviewException("Unsupported raw scalar consumed/mutated presentation.");
        d.Apply("height");if(!presenter.ConsumePending() || shown.Visual.Fingerprint(gpu:gpu)!=fingerprint)throw new AssetPreviewException("Fresh scalar retry changed resources.");
        using(var foreign=MakeWorld((_,_)=>Task.FromResult(source)))using(var f=new ReplicatedWorldPeer(foreign,peer))
        {
            f.SelectAndStage();f.OpenBarrier();var batch=foreign.DrainChanges(f.Generation)!;
            if(presenter.ApplyBatch(batch) || presenter.AcknowledgeAppliedBatch(batch) || presenter.Count!=1)throw new AssetPreviewException("Foreign observer batch accepted.");
        }
        var stale=owner.DrainChanges(d.Generation)!;d.Apply("hidden");if(presenter.ApplyBatch(stale) || owner.DrainChanges(d.Generation)!.Entries.Count!=1 || !presenter.ConsumePending() || shown.Visual.Root.Visible)throw new AssetPreviewException("Stale batch cleared latest visibility.");
        owner.RestartResource(d.Generation,d.ObjectId);if(!presenter.ConsumePending() || presenter.Count!=0 || !shown.Visual.IsRetired)throw new AssetPreviewException("Resource poll separation/restart retirement differs.");
        owner.PollResources();var restart=owner.PollResources().Prepared.Single();if(!presenter.AcceptResource(restart))throw new AssetPreviewException("Current restarted publication rejected.");
        var oldState=owner.FindReplication(d.Generation,d.ObjectId)!;var old=presenter.Find(d.ObjectId)!;
        d.Apply("destroy");d.Apply("create");d.Apply("baseline");d.Apply("end");
        var replacementBatch=owner.DrainChanges(d.Generation)!;
        if(replacementBatch.Entries.Count!=1 || replacementBatch.Entries[0].Current is null || replacementBatch.Entries[0].Current!.Object.Incarnation==oldState.Incarnation || !presenter.ConsumePending() || presenter.Count!=0 || !old.Visual.IsRetired)throw new AssetPreviewException("Coalesced recreation kept old actor/history.");
        owner.PollResources();var replacement=owner.PollResources().Prepared.Single();if(!presenter.AcceptResource(replacement) || presenter.Find(d.ObjectId)!.Apply(oldState))throw new AssetPreviewException("Old state reached replacement.");
        d.Apply("destroy");var tombstone=owner.DrainChanges(d.Generation)!;
        if(tombstone.Entries.Count!=1 || tombstone.Entries[0].Current is not null || !presenter.ConsumePending() || presenter.Count!=0)throw new AssetPreviewException("Tombstone was not consumed.");
        d.SelectAndStage();d.OpenBarrier();owner.PollResources();var fresh=owner.PollResources().Prepared.Single();presenter.AcceptResource(fresh);presenter.ConsumePending();d.Reset();
        if(presenter.ConsumePending() || presenter.Count!=0 || owner.AcknowledgeChanges(tombstone))throw new AssetPreviewException("Global selection boundary retained actor/batch.");
        d.SelectAndStage();d.OpenBarrier();owner.PollResources();fresh=owner.PollResources().Prepared.Single();presenter.AcceptResource(fresh);presenter.ConsumePending();
        bool failed=false;try{d.Apply("bad");}catch(InvalidDataException){failed=true;}
        if(!failed || presenter.ConsumePending() || presenter.Count!=0 || owner.DrainChanges(d.Generation)is not null)throw new AssetPreviewException("Global failure did not retire presentation.");
        d.SelectAndStage();d.OpenBarrier();owner.PollResources();fresh=owner.PollResources().Prepared.Single();presenter.AcceptResource(fresh);presenter.ConsumePending();owner.Close();await owner.DrainCompletion;
        if(presenter.ConsumePending() || presenter.Count!=0 || owner.OutstandingJobs!=0)throw new AssetPreviewException("Closed world retained presentation.");
        GD.Print("PASS: observer initial_coalesced=true apply_before_ack=true stale_ack_pending=true retry_latest=true foreign_batch=true stale_batch=true invalid_height_pending=true restart_separate=true destroy_recreate=true tombstone=true reset_global_retire=true failure_global_retire=true close_global_retire=true no_history=true palette_closure_rejected=true wrong_thread=true");
    }
    private async Task ShutdownAsync(){if(disposed)return;disposed=true;consumer?.Dispose();consumer=null;actor?.Dispose();actor=null;authoredColors=null;paletteProfile=null;driver?.Dispose();world?.Close();if(world is not null){await world.DrainCompletion;if(world.OutstandingJobs!=0 || workers.Any(t=>!t.IsCompleted))throw new AssetPreviewException("Shutdown drain incomplete.");}workers.Clear();environment?.Dispose();environment=null;configuration=Array.Empty<byte>();}
    private async Task CloseAsync(){await ShutdownAsync();GetTree().Quit();}
    public override void _Notification(int what){if(what==NotificationWMCloseRequest && !disposed)_=CloseAsync();}
    public override void _ExitTree(){if(!disposed)_=ShutdownAsync().ContinueWith(t=>{_=t.Exception;},TaskScheduler.Default);}
}
