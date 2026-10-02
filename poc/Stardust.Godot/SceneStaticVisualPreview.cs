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

/// <summary>Standalone pinned original terminal with owned visual lifecycle; no world/router dependency.</summary>
public partial class SceneStaticVisualPreview : Node
{
    private SceneObjectRegistry? registry;
    private readonly List<SceneStaticVisual> visuals=new();
    private Task<SceneStaticResources[]>? worker;
    private CancellationTokenSource? cancellation;
    private Godot.Environment? environment;
    private bool disposed;
    private Task? shutdown;
    public static Dictionary<string,string> ReadFixture(string path)
    {var file=new FileInfo(path);if(file.Length is <1 or >65536)throw new ArgumentException("Fixture budget.");var result=JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllBytes(path))!;if(result.Count!=6 || result.Values.Any(v=>v.Length>4096))throw new InvalidDataException("Fixture shape/budget.");foreach(string key in new[]{"a_create","a_end","a_destroy","b_create","b_end","b_destroy"})if(!result.ContainsKey(key))throw new InvalidDataException("Fixture key.");return result;}
    public static SceneCreateObjectByCrcMessage Create(IReadOnlyDictionary<string,string> fixture,string name)=>SceneObjectLifecycleCodec.DecodeCreate(Convert.FromHexString(fixture[name+"_create"]));
    public static SceneObjectHandle Finalize(SceneObjectRegistry registry,SceneCreateObjectByCrcMessage create)
    {registry.Create(registry.SceneGeneration,create);registry.EndBaselines(registry.SceneGeneration,new(create.ObjectId));return registry.Find(create.ObjectId)!.Handle;}
    public override async void _Ready()
    {try{await Run(OS.GetCmdlineUserArgs());}catch(OperationCanceledException)when(disposed){}catch(Exception failure){GD.PushError("Static visual preview: "+failure);try{await Shutdown();}catch(Exception error){GD.PushError("Static cleanup: "+error.GetType().Name);}if(GodotObject.IsInstanceValid(this) && IsInsideTree())GetTree().Quit(1);}}
    private async Task Run(string[] args)
    {
        string Read(string key){int at=Array.IndexOf(args,key);if(at<0 || at+1>=args.Length || args.Count(s=>s==key)!=1)throw new ArgumentException("One option required: "+key);return args[at+1];}
        GetTree().AutoAcceptQuit=false;string root=Read("--asset-root");var configuration=File.ReadAllBytes(Read("--asset-config"));
        if(Convert.ToHexString(SHA256.HashData(configuration))!=RetainedSkeletalDetailBundleReader.ConfigurationPin)throw new InvalidDataException("Configuration pin.");
        var fixture=ReadFixture(Read("--static-fixture"));var a=Create(fixture,"a");var b=Create(fixture,"b");bool cancel=args.Contains("--static-visual-cancel"),gpu=DisplayServer.GetName()!="headless";
        string? capture=args.Contains("--static-visual-capture")?Read("--static-visual-capture"):null;
        if(cancel && gpu || capture is not null && (!gpu || cancel))throw new ArgumentException("Separate bounded modes.");
        if(capture is not null)foreach(string image in Captures(capture)){string prefix=Path.GetFullPath(Path.Combine(ProjectSettings.GlobalizePath("res://"),"..","..","local-private"))+Path.DirectorySeparatorChar;if(!Path.GetFullPath(image).StartsWith(prefix,StringComparison.OrdinalIgnoreCase) || Path.GetExtension(image)!=".png" || File.Exists(image) || !Directory.Exists(Path.GetDirectoryName(Path.GetFullPath(image))))throw new ArgumentException("Fresh private capture pair required.");}
        registry=new(capacity:2,sceneGeneration:1);var ah=Finalize(registry,a);var bh=Finalize(registry,b);
        int ownerThread=System.Environment.CurrentManagedThreadId,workerThread=ownerThread,closed=0;var counts=new[]{new Dictionary<string,int>(),new Dictionary<string,int>()};var first=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        cancellation=new();var token=cancellation.Token;var config=configuration.ToArray();
        worker=Task.Run(()=>
        {
            workerThread=System.Environment.CurrentManagedThreadId;try
            {
                using var text=new StringReader(System.Text.Encoding.UTF8.GetString(config));using var overlay=ConfiguredTreOverlay.Open(root,text);
                var result=new List<SceneStaticResources>();int index=0;
                foreach(var create in new[]{a,b})
                {
                    var seen=counts[index++];TreLookupResult Resolve(string path){token.ThrowIfCancellationRequested();if(cancel){first.TrySetResult(true);if(!token.WaitHandle.WaitOne(TimeSpan.FromSeconds(10)))throw new InvalidOperationException("Cancellation timeout.");token.ThrowIfCancellationRequested();}seen[path]=seen.GetValueOrDefault(path)+1;return overlay.Lookup(path);}
                    result.Add(SceneStaticResourceReader.Load(create,StaticSwitchTexturePreview.Catalog(),Resolve,SceneStaticMaterialPolicy.RequireComplete,token,switchTextureFrame:new(StaticSwitchTexturePreview.ShaderPath,StaticSwitchTexturePreview.ShaderPin,0)));
                }
                return result.ToArray();
            }
            finally{Array.Clear(config);Interlocked.Increment(ref closed);}
        },token);
        if(cancel)
        {
            if(await Task.WhenAny(first.Task,worker)!=first.Task)throw new InvalidDataException("Cancellation barrier absent.");registry.Reset(2);cancellation.Cancel();try{await worker;}catch(OperationCanceledException){}
            if(registry.IsCurrent(ah) || closed!=1 || visuals.Count!=0)throw new InvalidDataException("Cancellation published/leaked.");await Shutdown();GD.Print("PASS: static canceled publications=0 archives_closed=true worker_observed=true drain_complete=true");GetTree().Quit();return;
        }
        var sources=await worker;if(disposed || !GodotObject.IsInstanceValid(this) || !IsInsideTree())return;
        if(workerThread==ownerThread || closed!=1 || sources.Any(s=>s.ExternalReads!=27 || s.SourceBytes!=1401794) || counts.Any(c=>c.Count!=27 || c.Values.Any(v=>v!=1)))throw new InvalidDataException("Original worker/closure differs.");
        var retainedCpu=sources.Select((s,i)=>JsonSerializer.Serialize(StaticSwitchTexturePreview.FrameEvidence(s,0,counts[i]))).ToArray();
        for(int i=0;i<2;i++)GD.Print("switch_frame="+retainedCpu[i]);
        var source=sources[0];var actor=SceneStaticVisual.TryPublish(registry,ah,source,this,true)!;var other=SceneStaticVisual.TryPublish(registry,bh,sources[1],this,true)!;visuals.Add(actor);visuals.Add(other);other.SetVisible(false);
        if(actor.MeshInstances!=4 || actor.TextureCount!=7 || actor.SelectedDetail!=3 || !actor.Verify(gpu) || !other.Verify(gpu))throw new InvalidDataException("Retained original static boundary differs.");
        string ids=actor.Fingerprint(gpu)!,otherIds=other.Fingerprint(gpu)!;int allocated=SceneStaticVisual.Allocations;var pose=actor.Root.Transform;
        for(int repeat=0;repeat<4;repeat++)for(int detail=0;detail<4;detail++)if(!actor.SelectDetail(detail) || actor.Fingerprint(gpu)!=ids)throw new InvalidDataException("LOD selection changed GPU IDs.");actor.SelectDetail(3);
        if(!actor.SetVisible(false) || actor.Root.Visible || !actor.SetVisible(true) || !actor.ApplyPlacement(source.Create) || actor.Root.Transform!=pose || actor.Fingerprint(gpu)!=ids || SceneStaticVisual.Allocations!=allocated)throw new InvalidDataException("Placement/visibility mutation replaced ownership.");
        int negatives=0;
        foreach(int ordinal in new[]{-1,4}){bool rejected=false;try{actor.SelectDetail(ordinal);}catch(ArgumentOutOfRangeException){rejected=true;}if(!rejected || actor.Fingerprint(gpu)!=ids)throw new InvalidDataException("Bad LOD mutated.");negatives++;}
        if(actor.ApplyPlacement(b) || actor.Root.Transform!=pose)throw new InvalidDataException("Foreign create placement accepted.");negatives++;
        var bad=Transform3D.Identity;bad.Origin=new(float.NaN,0,0);bool invalid=false;try{actor.SetEngineTransform(bad);}catch(ArgumentException){invalid=true;}if(!invalid || actor.Root.Transform!=pose)throw new InvalidDataException("Invalid transform mutated.");negatives++;
        using(var detached=new Node())
        {bool rejected=false;try{SceneStaticVisual.TryPublish(registry,ah,source,detached,true);}catch(ArgumentException){rejected=true;}if(!rejected || SceneStaticVisual.Allocations!=allocated)throw new InvalidDataException("Detached parent allocated.");negatives++;}
        foreach(var limits in new[]{new SceneStaticVisualLimits(3,64,256),new(64,6,256),new(64,64,1)})
        {bool rejected=false;try{SceneStaticVisual.TryPublish(registry,ah,source,this,true,limits);}catch(InvalidDataException){rejected=true;}if(!rejected || SceneStaticVisual.Allocations!=allocated)throw new InvalidDataException("Capacity allocated.");negatives++;}
        var foreign=new SceneObjectRegistry(capacity:2,sceneGeneration:1);var fh=Finalize(foreign,a);
        if(SceneStaticVisual.TryPublish(registry,fh,source,this,true)is not null || SceneStaticVisual.TryPublish(foreign,ah,source,this,true)is not null || SceneStaticVisual.TryPublish(registry,bh,source,this,true)is not null || SceneStaticVisual.Allocations!=allocated)throw new InvalidDataException("Foreign resource/registry allocated.");foreign.Close();negatives+=3;
        foreach(string action in new[]{"publish","visibility","placement","dispose"})
        {bool rejected=await Task.Run(()=>{try{if(action=="publish")SceneStaticVisual.TryPublish(registry,ah,source,this,true);else if(action=="visibility")actor.SetVisible(false);else if(action=="placement")actor.SetEngineTransform(Transform3D.Identity);else actor.Dispose();return false;}catch(InvalidOperationException){return true;}});if(!rejected || actor.Fingerprint(gpu)!=ids || actor.Root.Transform!=pose || !actor.Root.Visible)throw new InvalidDataException("Wrong thread mutated visual.");negatives++;}
        // Injected lifetime entry point has no duplicate registry; false rejects before allocation.
        if(SceneStaticVisual.TryPublish(source,pose,this,()=>false,true)is not null || SceneStaticVisual.Allocations!=allocated)throw new InvalidDataException("False injected guard allocated.");negatives++;
        bool live=true;var injected=SceneStaticVisual.TryPublish(source,pose,this,()=>live,true)!;visuals.Add(injected);injected.SetVisible(false);live=false;if(injected.Tick() || !injected.IsRetired)throw new InvalidDataException("Injected lifetime did not retire.");negatives++;
        bool transfer=true;int before=SceneStaticVisual.Allocations,disposedBefore=SceneStaticVisual.Disposals;
        if(SceneStaticVisual.TryPublish(source,pose,this,()=>transfer,true,beforePublication:()=>transfer=false)is not null || SceneStaticVisual.Allocations!=before+1 || SceneStaticVisual.Disposals!=disposedBefore+1)throw new InvalidDataException("Stale transfer did not roll back.");negatives++;
        before=SceneStaticVisual.Allocations;disposedBefore=SceneStaticVisual.Disposals;bool threw=false;
        try{SceneStaticVisual.TryPublish(source,pose,this,()=>true,true,beforePublication:()=>throw new InvalidDataException("synthetic transfer fault"));}catch(InvalidDataException){threw=true;}
        if(!threw || SceneStaticVisual.Allocations!=before+1 || SceneStaticVisual.Disposals!=disposedBefore+1 || actor.Fingerprint(gpu)!=ids || other.Fingerprint(gpu)!=otherIds)throw new InvalidDataException("Transfer fault leaked/changed other visual.");negatives++;
        // Default opaque policy must not silently opt into the two special screen overrides.
        before=SceneStaticVisual.Allocations;disposedBefore=SceneStaticVisual.Disposals;int children=GetChildCount();bool strictRejected=false;
        try{SceneStaticVisual.TryPublish(registry,ah,source,this);}catch(InvalidDataException){strictRejected=true;}
        if(!strictRejected || SceneStaticVisual.Allocations!=before+1 || SceneStaticVisual.Disposals!=disposedBefore+1 || GetChildCount()!=children || actor.Fingerprint(gpu)!=ids)throw new InvalidDataException("Default effect policy broadened/leaked.");negatives++;
        environment=new(){BackgroundMode=Godot.Environment.BGMode.Color,BackgroundColor=new Color("#14202e")};AddChild(new WorldEnvironment{Environment=environment});
        var bounds=actor.Bounds;var finest=source.Appearance.Meshes[source.Appearance.Geometry.Root.Detail!.Levels[3].ChildPath!];
        var local=finest.Mesh.Primitives.Where(p=>p.ShaderPath==StaticSwitchTexturePreview.ShaderPath).SelectMany(p=>p.Vertices).Aggregate(Vector3.Zero,(v,x)=>v+new Vector3(x.Normal.X,x.Normal.Y,-x.Normal.Z)).Normalized();var outward=(pose.Basis*local).Normalized();var side=new Vector3(outward.Z,0,-outward.X).Normalized();float radius=Math.Max(bounds.Size.Length()*.75f,1);
        var camera=new Camera3D{Current=true,Fov=50,Position=bounds.GetCenter()+outward*radius*2.4f+Vector3.Up*radius*.35f+side*radius*.45f};AddChild(camera);camera.LookAt(bounds.GetCenter());
        var canvas=new CanvasLayer();AddChild(canvas);canvas.AddChild(new Label{Text="Owned original terminal | frame0 / four LODs | two SWTS surfaces: unshaded MAIN inspection",Position=new(20,20)});
        State("initial",actor,ah,gpu);if(capture is not null)await Capture(Captures(capture)[0]);
        registry.Destroy(1,SceneObjectLifecycleCodec.DecodeDestroy(Convert.FromHexString(fixture["a_destroy"])));
        if(actor.SetVisible(true) || actor.SetEngineTransform(pose) || !actor.IsRetired || SceneStaticVisual.TryPublish(registry,ah,source,this,true)is not null || other.Fingerprint(gpu)!=otherIds || !registry.IsCurrent(bh))throw new InvalidDataException("Destroy retained A/retired B.");negatives++;
        var fresh=Finalize(registry,a);var replacement=SceneStaticVisual.TryPublish(registry,fresh,source,this,true)!;visuals.Add(replacement);
        if(replacement.Fingerprint(gpu)==ids || other.Fingerprint(gpu)!=otherIds || !replacement.Verify(gpu) || replacement.Root.Transform!=pose || replacement.MeshInstances!=4 || replacement.TextureCount!=7)throw new InvalidDataException("Recreate changed parsed sources/B/placement.");
        State("recreated",replacement,fresh,gpu);if(capture is not null)await Capture(Captures(capture)[1]);
        if(sources.Select((s,i)=>JsonSerializer.Serialize(StaticSwitchTexturePreview.FrameEvidence(s,0,counts[i]))).Where((value,i)=>value!=retainedCpu[i]).Any())throw new InvalidDataException("Parsed original closure mutated by visual lifecycle.");
        registry.Close();if(replacement.Tick() || other.Tick() || !replacement.IsRetired || !other.IsRetired)throw new InvalidDataException("Close retained visuals.");negatives++;
        GD.Print("static_validation="+JsonSerializer.Serialize(new{negatives,selections=16,frame=0,meshInstances=4,textures=7,strictCpu=true,screenOverrides=2,otherIdsFixed=true,injectedPredicate=true,workerThread,ownerThread,closed}));
        await Shutdown();Array.Clear(configuration);GD.Print("PASS: static teardown roots_released=true adapters_disposed=true predicates_released=true worker_observed=true archives_closed=true allocations="+SceneStaticVisual.Allocations+" disposals="+SceneStaticVisual.Disposals);GetTree().Quit();
    }
    private void State(string stage,SceneStaticVisual actor,SceneObjectHandle handle,bool gpu)=>GD.Print("static_state="+JsonSerializer.Serialize(new{stage,id=handle.ObjectId,handle=handle.Revision,incarnation=registry!.Find(handle.ObjectId)!.Incarnation.CreationRevision,pose=CreatureStatePreview.PoseEvidence(actor.Root.Transform),ids=actor.Fingerprint(gpu),detail=actor.SelectedDetail,meshes=actor.MeshInstances,textures=actor.TextureCount}));
    public static string[] Captures(string first)=>new[]{first,Path.Combine(Path.GetDirectoryName(first)!,Path.GetFileNameWithoutExtension(first)+"-recreated.png")};
    private async Task Capture(string path){await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);using var image=GetViewport().GetTexture().GetImage();if(image.SavePng(path)!=Error.Ok)throw new IOException("Capture failed.");}
    private Task Shutdown()=>shutdown??=ShutdownCore();
    private async Task ShutdownCore()
    {
        disposed=true;cancellation?.Cancel();foreach(var visual in visuals)visual.Dispose();visuals.Clear();registry?.Close();
        try{if(worker is not null){try{await worker;}catch(OperationCanceledException){}}}
        finally{worker=null;cancellation?.Dispose();cancellation=null;environment?.Dispose();environment=null;if(SceneStaticVisual.Allocations!=SceneStaticVisual.Disposals)throw new InvalidDataException("Static root leak.");}
    }
    private async Task CloseAsync(){await Shutdown();if(GodotObject.IsInstanceValid(this) && IsInsideTree())GetTree().Quit(1);}
    public override void _Notification(int what){if(what==NotificationWMCloseRequest)_ = CloseAsync();}
    public override void _ExitTree(){if(!disposed)_ = Shutdown().ContinueWith(t=>{_ = t.Exception;},TaskScheduler.Default);}
}
