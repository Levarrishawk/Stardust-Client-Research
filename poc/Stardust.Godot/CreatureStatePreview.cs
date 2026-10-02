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

public partial class CreatureStatePreview : Node
{
    private OwnedBackgroundAssetLoad<SceneCreatureResources>? load;
    private SceneCreatureStatePresentation? presentation;
    private SceneObjectRegistry? registry;
    private SceneCreatureReplicationStore? store;
    private Godot.Environment? environment;
    private bool disposed;
    private Label? status;
    public static readonly CreatureView3BaselineLimits Limits = new(1024,64,64,64,4096);
    public static Dictionary<string,byte[]> ReadPeer(string path) => JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllText(path))!
        .ToDictionary(p => p.Key,p => Convert.FromHexString(p.Value));
    public static Transform3D EnginePlacement(SceneCreateObjectByCrcMessage create)
        => GodotAppearanceTransform.ToGodot(SceneCreatePlacementBuilder.Create(create,SceneCreateZeroQuaternionPolicy.Reject).Transform);
    public static object PoseEvidence(Transform3D pose) => new { origin = new[] {pose.Origin.X,pose.Origin.Y,pose.Origin.Z},
        columns = new[] {new[] {pose.Basis.X.X,pose.Basis.X.Y,pose.Basis.X.Z},new[] {pose.Basis.Y.X,pose.Basis.Y.Y,pose.Basis.Y.Z},new[] {pose.Basis.Z.X,pose.Basis.Z.Y,pose.Basis.Z.Z}} };
    internal async Task InitializeAsync(string[] args)
    {
        string Read(string key) { int at=Array.IndexOf(args,key); if(at<0 || at+1>=args.Length || args.Count(v=>v==key)!=1) throw new ArgumentException("Missing/duplicate state option."); return args[at+1]; }
        string root=Read("--asset-root"),configuration=Read("--asset-config");
        var peer=ReadPeer(Read("--state-fixture"));
        bool cancel=args.Contains("--state-cancel"),test=args.Contains("--state-self-test"),headless=DisplayServer.GetName()=="headless";
        string? capture=args.Contains("--state-capture")?Read("--state-capture"):null;
        if(cancel && (test || capture is not null || !headless) || capture is not null && (test || headless) || headless && !cancel && !test) throw new ArgumentException("Separate bounded state modes required.");
        if(capture is not null)
        {
            string prefix=Path.GetFullPath(Path.Combine(ProjectSettings.GlobalizePath("res://"),"..","..","local-private"))+Path.DirectorySeparatorChar;
            foreach(string path in new[]{capture,SecondCapture(capture)})
                if(!Path.GetFullPath(path).StartsWith(prefix,StringComparison.OrdinalIgnoreCase) || Path.GetExtension(path)!=".png" || File.Exists(path) || !Directory.Exists(Path.GetDirectoryName(Path.GetFullPath(path)))) throw new ArgumentException("Fresh private capture pair required.");
        }
        var create=SceneObjectLifecycleCodec.DecodeCreate(peer["create"]);
        // Fixed synthetic dispatch identity selects the accepted, pinned human input set.
        var expected=new SceneCreateObjectByCrcMessage(0xDEADBEEF00000001,.2f,BitConverter.Int32BitsToSingle(int.MinValue),.3f,1.7f,-2,7,4,0xAF1DC1A1,255);
        if(!SceneObjectLifecycleCodec.EncodeCreate(expected).AsSpan().SequenceEqual(peer["create"])) throw new InvalidDataException("State inspector create fixture differs.");
        var initial=CreatureView3BaselineCodec.Decode(peer["baseline"],Limits);
        var delta=CreatureView3ScalarDeltaCodec.Decode(peer["delta"],1024,16);
        if(initial.ObjectId!=create.ObjectId || initial.Scalars.Height!=1 || initial.Visibility!=1 || delta.Apply(initial.Scalars).Height!=1.5f) throw new InvalidDataException("State inspector pair differs.");
        byte[] config=File.ReadAllBytes(configuration);
        if(Convert.ToHexString(SHA256.HashData(config))!=RetainedSkeletalDetailBundleReader.ConfigurationPin) throw new InvalidDataException("Configuration pin differs.");
        using var profile=File.OpenRead(Path.Combine(ProjectSettings.GlobalizePath("res://"),"..","preview-profiles","human-locomotion.json"));
        var bank=SkeletalAnimationBankProfile.Load(profile);
        var catalog=new ObjectTemplateCatalog(new[]{new ObjectTemplateCatalogEntry(create.TemplateCrc,RetainedSkeletalDetailBundleReader.ObjectPath,RetainedSkeletalDetailBundleReader.ObjectPin,RetainedSkeletalDetailBundleReader.AppearancePin)});
        var idle=new LogicalAnimationClipSelection("appearance/lat/all_m.lat","emt_standing_idle_ambient_1_ag","798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B");
        int main=System.Environment.CurrentManagedThreadId,worker=main,closed=0; var reads=new Dictionary<string,int>();
        var first=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var request=new OwnedBackgroundAssetLoad<SceneCreatureResources>(token=>
        {
            worker=System.Environment.CurrentManagedThreadId;
            try
            {
                using var reader=new StringReader(System.Text.Encoding.UTF8.GetString(config)); using var overlay=ConfiguredTreOverlay.Open(root,reader);
                TreLookupResult Resolve(string path)
                {
                    token.ThrowIfCancellationRequested();
                    if(cancel && first.TrySetResult(true)){token.WaitHandle.WaitOne(TimeSpan.FromSeconds(10));token.ThrowIfCancellationRequested();throw new InvalidOperationException("Cancellation timeout.");}
                    reads[path]=reads.GetValueOrDefault(path)+1;return overlay.Lookup(path);
                }
                return SceneCreatureResourceReader.Load(create,catalog,idle,bank,Resolve,token);
            }
            finally{Volatile.Write(ref closed,1);}
        });
        load=request; SceneCreatureResources source;
        try
        {
            var transfer=request.TakeAsync();if(cancel && await Task.WhenAny(first.Task,request.WorkerCompletion)==first.Task)request.Cancel();source=await transfer;
            if(disposed || !IsInsideTree())return;
            if(worker==main || main!=System.Environment.CurrentManagedThreadId || Volatile.Read(ref closed)!=1 || reads.Values.Any(n=>n!=1) || reads.Count!=source.ExternalReads)throw new AssetPreviewException("State load transfer/read-once differs.");
            foreach(var row in source.Creature.Sources.Values)GD.Print("source="+JsonSerializer.Serialize(row));
            GD.Print($"PASS: state original resources={source.Creature.Sources.Count} bytes={source.SourceBytes} worker_transfer=true archives_closed=true");
        }
        catch(OperationCanceledException)
        {
            if(Volatile.Read(ref closed)!=1 || presentation is not null)throw new AssetPreviewException("Canceled state load leaked.");
            GD.Print("PASS: state canceled archives_closed=true geometry_publications=0");DisposeOwned();GetTree().Quit();return;
        }
        finally{request.Dispose();if(ReferenceEquals(load,request))load=null;}
        registry=new(8,1);var created=registry.Create(1,create)!;registry.EndBaselines(1,new(create.ObjectId));var current=registry.Find(create.ObjectId)!;
        store=new(registry);var snapshot=store.Seed(created.Incarnation,initial)!;var basis=EnginePlacement(create);
        presentation=SceneCreatureStatePresentation.TryPublish(registry,current.Handle,source,store,snapshot,basis,this)??throw new AssetPreviewException("State publication failed.");
        presentation.Visual.SeekIdle(0);presentation.Visual.SetPlaying(false);
        environment=new(){BackgroundMode=Godot.Environment.BGMode.Color,BackgroundColor=new Color("#14202e")};AddChild(new WorldEnvironment{Environment=environment});
        var bounds=SceneCreatureStatePresentation.DeriveTransform(basis,1.5f)*presentation.Visual.Bounds;
        var camera=new PreviewOrbitCamera{Current=true,Fov=50,VOffset=.55f};AddChild(camera);camera.Configure(bounds.GetCenter(),Math.Max(bounds.Size.Length()*.85f,1));
        var layer=new CanvasLayer();AddChild(layer);var panel=new ColorRect{Color=new(.02f,.04f,.08f,.92f),OffsetRight=1280,OffsetBottom=155};layer.AddChild(panel);
        panel.AddChild(new Label{Text="STARDUST | CREATURE STATE PRESENTATION",Position=new(24,12)});status=new Label{Position=new(24,44)};panel.AddChild(status);
        panel.AddChild(new Label{Text="Modern Height multiplier [0.25,2] | baseline visibility | authored MAIN | other scalars diagnostic only",Position=new(24,120)});
        Refresh(snapshot);
        if(test || capture is not null)
        {
            VerifyFixture(source,peer,headless==false);
            // Fixture tests use other owners; the displayed baseline stays the original current state.
            string retained=presentation.Visual.Fingerprint(gpu:!headless)!;int allocations=SceneCreatureVisual.Allocations;Transform3D fixedCamera=camera.GlobalTransform;
            GD.Print("state_create="+Convert.ToHexString(SceneObjectLifecycleCodec.EncodeCreate(create)));
            GD.Print("state_base="+JsonSerializer.Serialize(PoseEvidence(basis)));
            Image? before=null;
            if(capture is not null)before=await Capture(capture);
            var updated=store.Apply(created.Incarnation,delta)!;
            if(!presentation.Tick(0) || presentation.Visual.IsRetired || presentation.Apply(snapshot) || !presentation.Apply(updated))throw new AssetPreviewException("State freshness retired actor or accepted stale state.");
            Refresh(updated);
            GD.Print("state_height="+JsonSerializer.Serialize(PoseEvidence(presentation.Visual.Root.Transform)));
            if(capture is not null)
            {
                using(before)
                using(var after=await Capture(SecondCapture(capture)))
                {
                    int changed=0;for(int y=180;y<before!.GetHeight();y++)for(int x=0;x<before.GetWidth();x++)if(before.GetPixel(x,y)!=after.GetPixel(x,y))changed++;
                    if(changed<100 || camera.GlobalTransform!=fixedCamera)throw new AssetPreviewException("Height pair actor pixels/fixed camera differs.");
                    GD.Print($"PASS: state GPU pair actor_changed_pixels={changed} fixed_camera=true UI_excluded_y=180 mip_readback=true");
                }
            }
            if(presentation.Visual.Fingerprint(gpu:!headless)!=retained || SceneCreatureVisual.Allocations!=allocations)throw new AssetPreviewException("Height update replaced GPU resources.");
            GD.Print($"PASS: state revision_expiration_keeps_visual=true stale_snapshot_rejected=true resource_ids_fixed=true retained={retained}");
            DisposeOwned();GD.Print("PASS: state teardown roots_released=true resources_disposed=true loader_disposed=true");GetTree().Quit();return;
        }
        void Button(string label,int x,Action action){var button=new Button{Text=label,Position=new(x,78)};panel.AddChild(button);button.Pressed+=()=>{try{action();}catch(Exception e){GD.PushError(e.GetType().Name);GetTree().Quit(1);}};}
        Button("Height 1.5",24,()=>{var state=store.Apply(created.Incarnation,delta)!;presentation.Apply(state);Refresh(state);});
        Button("Reset baseline",170,()=>{var state=store.Seed(created.Incarnation,initial)!;presentation.Apply(state);Refresh(state);});
        Button("Hide",340,()=>{var state=store.Seed(created.Incarnation,CreatureView3BaselineCodec.Decode(peer["hidden"],Limits))!;presentation.Apply(state);Refresh(state);});
    }
    private void Refresh(SceneCreatureReplicationSnapshot s){status!.Text=$"revision {s.Revision} | Height {s.Scalars.Height} | visibility {s.Baseline.Visibility} | posture {s.Scalars.Posture} rank {s.Scalars.Rank} shock {s.Scalars.Shock:X8} states {s.Scalars.States:X16}";}
    private static string SecondCapture(string path)=>Path.Combine(Path.GetDirectoryName(path)!,Path.GetFileNameWithoutExtension(path)+"-frame1.png");
    private async Task<Image> Capture(string path)
    {
        for(int i=0;i<3;i++)await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
        presentation!.Visual.Verify(true);var image=GetViewport().GetTexture().GetImage();if(image.SavePng(path)!=Error.Ok){image.Dispose();throw new IOException("State capture failed.");}return image;
    }
    private void VerifyFixture(SceneCreatureResources source,Dictionary<string,byte[]> peer,bool gpu)
    {
        CreatureView3BaselineSnapshot Baseline(string key)=>CreatureView3BaselineCodec.Decode(peer[key],Limits);
        CreatureView3ScalarDeltaPlan Delta(string key)=>CreatureView3ScalarDeltaCodec.Decode(peer[key],1024,16);
        var owner=new SceneObjectRegistry(8,1);using var states=new SceneCreatureReplicationStore(owner);
        var entry=owner.Create(1,source.Create)!;var seed=states.Seed(entry.Incarnation,Baseline("baseline"))!;
        using var actor=SceneCreatureStatePresentation.TryPublish(owner,entry.Handle,source,states,seed,EnginePlacement(source.Create),this)!;
        string fingerprint=actor.Visual.Fingerprint(gpu:gpu)!;Transform3D original=actor.Visual.Root.Transform;
        foreach(string key in new[]{"zero","negativeZero","negative","small","large"})
        {
            var bad=states.Apply(entry.Incarnation,Delta(key))!;bool rejected=false;try{actor.Apply(bad);}catch(ArgumentOutOfRangeException){rejected=true;}
            if(!rejected || actor.Visual.Root.Transform!=original || states.Find(entry.Incarnation)!=bad || actor.Visual.Fingerprint(gpu:gpu)!=fingerprint)throw new AssetPreviewException("Invalid height mutated raw state/visual.");
        }
        foreach(string key in new[]{"minimum","maximum"})if(!actor.Apply(states.Apply(entry.Incarnation,Delta(key))!))throw new AssetPreviewException("Height bound rejected.");
        foreach(string key in new[]{"hidden","shown"})
        {
            var state=states.Seed(entry.Incarnation,Baseline(key))!;if(!actor.Apply(state) || actor.Visual.Root.Visible!=(key=="shown"))throw new AssetPreviewException("Raw visibility mapping differs.");
        }
        var before=states.Find(entry.Incarnation)!;
        if(actor.Apply(before,()=>states.Apply(entry.Incarnation,Delta("delta"))) || !actor.Tick(0) || actor.Visual.Fingerprint(gpu:gpu)!=fingerprint)throw new AssetPreviewException("Application recheck failed.");
        using var foreign=new SceneCreatureReplicationStore(owner);var foreignState=foreign.Seed(entry.Incarnation,Baseline("baseline"))!;
        if(actor.Apply(foreignState))throw new AssetPreviewException("Foreign store state accepted.");
        ulong oldRoot=actor.Visual.Root.GetInstanceId();owner.Destroy(1,new(source.Create.ObjectId,0));var recreated=owner.Create(1,source.Create)!;
        var fresh=states.Seed(recreated.Incarnation,Baseline("baseline"))!;
        if(actor.Apply(fresh) || !actor.Visual.IsRetired || GodotObject.InstanceFromId(oldRoot)is not null)throw new AssetPreviewException("Destroy/recreate old actor survived.");
        using(var replacement=SceneCreatureStatePresentation.TryPublish(owner,recreated.Handle,source,states,fresh,EnginePlacement(source.Create),this)!)
            if(replacement.Apply(before) || !replacement.Tick(0))throw new AssetPreviewException("Old incarnation affected replacement.");
        foreach(bool attach in new[]{false,true})
        {
            var own=new SceneObjectRegistry(2,1);using var ss=new SceneCreatureReplicationStore(own);var row=own.Create(1,source.Create)!;var state=ss.Seed(row.Incarnation,Baseline("baseline"))!;
            int disposals=SceneCreatureVisual.Disposals;ulong root=0;
            void Invalidate(Node node){if(node is Node3D staged){if(staged.Visible)throw new AssetPreviewException("Staging was visible.");root=staged.GetInstanceId();ss.Apply(row.Incarnation,Delta("delta"));}}
            if(attach)ChildEnteredTree+=Invalidate;
            try{if(SceneCreatureStatePresentation.TryPublish(own,row.Handle,source,ss,state,EnginePlacement(source.Create),this,attach?null:()=>ss.Apply(row.Incarnation,Delta("delta")))is not null)throw new AssetPreviewException("Stale publication accepted.");}
            finally{if(attach)ChildEnteredTree-=Invalidate;}
            if(SceneCreatureVisual.Disposals!=disposals+1 || attach && (root==0 || GodotObject.InstanceFromId(root)is not null))throw new AssetPreviewException("Staged state leaked.");
        }
        int count=SceneCreatureVisual.Allocations;
        if(SceneCreatureStatePresentation.TryPublish(owner,recreated.Handle,source,states,before,EnginePlacement(source.Create),this)is not null || SceneCreatureVisual.Allocations!=count)throw new AssetPreviewException("Stale preflight allocated.");
        GD.Print("PASS: state invalid_heights=5 bounds=2 visibility=0,255 foreign_store=true application_recheck=true staged_cleanup=true post_attach_cleanup=true destroy_recreate=true stale_preflight_no_allocation=true");
    }
    private void DisposeOwned(){if(disposed)return;disposed=true;load?.Dispose();load=null;presentation?.Dispose();presentation=null;store?.Dispose();store=null;registry?.Close();registry=null;environment?.Dispose();environment=null;}
    public override void _ExitTree()=>DisposeOwned();
}
