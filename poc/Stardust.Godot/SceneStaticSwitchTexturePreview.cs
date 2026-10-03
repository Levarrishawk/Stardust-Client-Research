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
using Stardust.Core.Session;

public partial class SceneStaticSwitchTexturePreview : Node
{
    private CancellationTokenSource cancellation=new();
    private Task<(SceneStaticResources,SwitchTextureFrameBundle)>? worker;
    private SceneStaticVisual? inner,other;
    private SceneStaticSwitchTextureVisual? playback;
    private SceneObjectRegistry registry=new(capacity:2,sceneGeneration:1);
    private Godot.Environment? environment;
    private bool closed;
    public override async void _Ready()
    {try{await Run();}catch(OperationCanceledException)when(closed){}catch(Exception e){GD.PushError("Switch visual: "+e);try{await Shutdown();}catch(Exception cleanup){GD.PushError("Switch cleanup: "+cleanup);}GetTree().Quit(1);}}
    private static void Need(bool condition,string message){if(!condition)throw new InvalidDataException(message);}
    private async Task Run()
    {
        string[] args=OS.GetCmdlineUserArgs();string Read(string key){int n=Array.IndexOf(args,key);if(n<0 || n+1==args.Length || args.Count(a=>a==key)!=1)throw new ArgumentException(key);return args[n+1];}
        GetTree().AutoAcceptQuit=false;string root=Read("--asset-root"),configPath=Read("--asset-config");byte[] config=File.ReadAllBytes(configPath);
        Need(Convert.ToHexString(SHA256.HashData(config))==RetainedSkeletalDetailBundleReader.ConfigurationPin,"Configuration pin");
        bool gpu=DisplayServer.GetName()!="headless";string? capture=args.Contains("--capture")?Read("--capture"):null;
        if(capture is not null){Need(gpu,"GPU capture required");var prefix=Path.GetFullPath(Path.Combine(ProjectSettings.GlobalizePath("res://"),"..","..","local-private"))+Path.DirectorySeparatorChar;foreach(int f in new[]{0,3,7}){var path=CapturePath(capture,f);Need(Path.GetFullPath(path).StartsWith(prefix,StringComparison.OrdinalIgnoreCase) && !File.Exists(path) && Path.GetExtension(path)==".png" && Directory.Exists(Path.GetDirectoryName(path)),"Fresh private captures");}}
        int owner=System.Environment.CurrentManagedThreadId,workerThread=owner,archivesClosed=0;var counts=new[]{new Dictionary<string,int>(),new Dictionary<string,int>()};var create=StaticSwitchTexturePreview.CreateFixture();var token=cancellation.Token;
        worker=Task.Run(()=>
        {
            workerThread=System.Environment.CurrentManagedThreadId;
            try
            {
                using var reader=new StringReader(System.Text.Encoding.UTF8.GetString(config));using var overlay=ConfiguredTreOverlay.Open(root,reader);
                TreLookupResult Resolve(string path,int index){token.ThrowIfCancellationRequested();counts[index][path]=counts[index].GetValueOrDefault(path)+1;return overlay.Lookup(path);}
                var source=SceneStaticResourceReader.Load(create,StaticSwitchTexturePreview.Catalog(),p=>Resolve(p,0),SceneStaticMaterialPolicy.RequireComplete,token,switchTextureFrame:new(StaticSwitchTexturePreview.ShaderPath,StaticSwitchTexturePreview.ShaderPin,0));
                var frames=SwitchTextureFrameBundle.Load(p=>Resolve(p,1),StaticSwitchTexturePreview.ShaderPath,StaticSwitchTexturePreview.ShaderPin,cancellationToken:token);return(source,frames);
            }
            finally{Array.Clear(config);Interlocked.Increment(ref archivesClosed);}
        },token);
        var (source,bundle)=await worker;if(closed || !GodotObject.IsInstanceValid(this) || !IsInsideTree())return;Need(owner==System.Environment.CurrentManagedThreadId && workerThread!=owner && archivesClosed==1,"Worker owner/drain");
        Need(source.ExternalReads==27 && source.SourceBytes==1401794 && counts[0].Count==27 && counts[1].Count==10 && counts.All(c=>c.Values.All(v=>v==1)),"Separate explicit preload accounting");
        SceneStaticSwitchTextureVisual.ValidateInputs(source,bundle);
        var handle=SceneStaticVisualPreview.Finalize(registry,create);inner=SceneStaticVisual.TryPublish(registry,handle,source,this,true)!;
        other=SceneStaticVisual.TryPublish(source,inner.Root.Transform,this,()=>!closed,true)!;other.SetVisible(false);string otherProof=other.Fingerprint(gpu)!;
        var pose=inner.Root.Transform;int detail=inner.SelectedDetail;string structure=inner.StructureFingerprint(gpu)!;string defaultIds=inner.Fingerprint(gpu)!;
        playback=new(inner,source,bundle,64);Need(playback.UniqueUploads==8 && playback.ImageCopies==8 && playback.UploadedBytes==349536 && inner.Verify(gpu),"Frame0 default verification");
        var initial=playback.Snapshot;int negatives=0;
        foreach(float invalid in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity,-1f,float.MaxValue})
        {
            string binding=inner.Fingerprint(gpu)!;var state=playback.Snapshot;bool rejected=false;try{playback.Advance(invalid);}catch(Exception e)when(e is ArgumentOutOfRangeException or InvalidDataException){rejected=true;}
            Need(rejected && ReferenceEquals(state,playback.Snapshot) && inner.Fingerprint(gpu)==binding && playback.CursorsSynchronized,"Invalid update atomicity");negatives++;
        }
        bool duplicate=false;try{using var bad=new SceneStaticSwitchTextureVisual(inner,source,bundle);}catch(InvalidOperationException){duplicate=true;}Need(duplicate,"Exclusive binding");negatives++;
        bool badCap=false;try{using var bad=new SceneStaticSwitchTextureVisual(other,source,bundle,0);}catch(ArgumentOutOfRangeException){badCap=true;}Need(badCap && other.Fingerprint(gpu)==otherProof,"Bad cursor cap before GPU");negatives++;
        // Synthetic descriptor corruption: preclaim rejection must preserve the exception and original binding.
        var corruptedMeshes=new Dictionary<string,TexturedStaticMesh>(source.Appearance.Meshes,StringComparer.Ordinal);
        var entry=corruptedMeshes.First(p=>p.Value.Textures.ContainsKey(bundle.Frames[0].TexturePath));
        var corruptedTextures=new Dictionary<string,DdsImage>(entry.Value.Textures,StringComparer.Ordinal);
        var dds=corruptedTextures[bundle.Frames[0].TexturePath];var mips=dds.Mips.ToArray();var bytes=mips[0].Rgba.ToArray();bytes[0]^=1;mips[0]=mips[0] with{Rgba=bytes};corruptedTextures[bundle.Frames[0].TexturePath]=dds with{Mips=Array.AsReadOnly(mips)};
        corruptedMeshes[entry.Key]=entry.Value with{Textures=corruptedTextures};var corrupted=source with{Appearance=source.Appearance with{Meshes=corruptedMeshes}};
        bool preclaim=false;try{using var bad=new SceneStaticSwitchTextureVisual(other,corrupted,bundle);}catch(InvalidDataException e){preclaim=e.Message=="Static/bundle frame0 bytes differ.";}
        Need(preclaim && other.Fingerprint(gpu)==otherProof,"Preclaim failure preserves original error/binding");negatives++;
        using(var validAfterFailure=new SceneStaticSwitchTextureVisual(other,source,bundle)){Need(validAfterFailure.VerifySelected(gpu),"Valid construction after preclaim failure");}Need(other.Fingerprint(gpu)==otherProof,"Restored preclaim target");
        bool wrongOwner=await Task.Run(()=>{try{_ = playback.Snapshot;return false;}catch(InvalidOperationException){return true;}});Need(wrongOwner,"Owner rejection");negatives++;
        environment=new(){BackgroundMode=Godot.Environment.BGMode.Color,BackgroundColor=new Color("#14202e")};AddChild(new WorldEnvironment{Environment=environment});
        var bounds=inner.Bounds;var finest=source.Appearance.Meshes[source.Appearance.Geometry.Root.Detail!.Levels[3].ChildPath!];var local=finest.Mesh.Primitives.Where(p=>p.ShaderPath==StaticSwitchTexturePreview.ShaderPath).SelectMany(p=>p.Vertices).Aggregate(Vector3.Zero,(v,x)=>v+new Vector3(x.Normal.X,x.Normal.Y,-x.Normal.Z)).Normalized();var outward=(pose.Basis*local).Normalized();var side=new Vector3(outward.Z,0,-outward.X).Normalized();float radius=Math.Max(bounds.Size.Length()*.75f,1);
        var camera=new Camera3D{Current=true,Fov=50,Position=bounds.GetCenter()+outward*radius*2.4f+Vector3.Up*radius*.35f+side*radius*.45f};AddChild(camera);camera.LookAt(bounds.GetCenter());
        var label=new Label{Text="Original terminal | supplied synthetic seconds | modern cursor | unshaded MAIN/UV0",Position=new(20,20)};var canvas=new CanvasLayer();AddChild(canvas);canvas.AddChild(label);
        var syntheticImage=bundle.Frames[0].Image.CopyImage();var syntheticCache=new Dictionary<string,ImageTexture>(StringComparer.Ordinal);
        try{var aliasFrames=SceneStaticSwitchTextureVisual.UploadFrames(new[]{"synthetic/a","synthetic/a","synthetic/b"},new Dictionary<string,DdsImage>{{"synthetic/a",syntheticImage},{"synthetic/b",syntheticImage}},syntheticCache);Need(syntheticCache.Count==2 && aliasFrames[0].GetInstanceId()==aliasFrames[1].GetInstanceId() && aliasFrames[0].GetInstanceId()!=aliasFrames[2].GetInstanceId(),"Synthetic alias/distinct path cache identity");}finally{foreach(var texture in syntheticCache.Values)texture.Dispose();}
        var mipHashes=bundle.Frames.Select(f=>f.Image.Mips.Select(m=>Convert.ToHexString(SHA256.HashData(m.CopyRgba()))).ToArray()).ToArray();
        var ids=new HashSet<ulong>();
        for(int f=0;f<8;f++)
        {
            if(f>0)Need(playback.Advance(bundle.Interval),"Advance");
            Need(playback.Snapshot.Index==f && playback.VerifySelected(gpu) && inner.StructureFingerprint(gpu)==structure && inner.Root.Transform==pose && inner.SelectedDetail==detail && other.Fingerprint(gpu)==otherProof,"Selected frame and retained structure");
            Need(ids.Add(playback.SelectedTextureId),"Distinct authored path upload identity");
            foreach(int lod in new[]{0,1,2,3}){Need(inner.SelectDetail(lod) && playback.VerifySelected(gpu),"LOD binding retention");}inner.SelectDetail(detail);
            var frame=bundle.Frames[f];GD.Print("switch_frame="+JsonSerializer.Serialize(new{index=f,path=frame.TexturePath,texture=playback.SelectedTextureId,structure,pose=CreatureStatePreview.PoseEvidence(pose),gpu,mips=mipHashes[f]}));
            if(capture is not null && f is 0 or 3 or 7){label.Text="Original terminal | modern synthetic playback | frame "+f;await Capture(CapturePath(capture,f));}
        }
        Need(playback.Advance(0) && playback.Snapshot.Index==7 && playback.CursorsSynchronized,"Zero/noop");
        Need(playback.Advance(bundle.Interval*17) && playback.Snapshot.Index==0 && playback.Snapshot.LastAdvanceSteps==17 && playback.Snapshot.CumulativeAdvances==24 && playback.CursorsSynchronized && playback.VerifySelected(gpu),"Multiple wraps");
        Need(playback.Reset() && playback.Snapshot.Index==0 && playback.Snapshot.Residual==0 && inner.Verify(gpu),"Reset frame0");
        Need(inner.Fingerprint(gpu)!=defaultIds,"Separate frame0 cached upload identity");
        playback.Dispose();playback.Dispose();Need(inner.Tick() && inner.Fingerprint(gpu)==defaultIds && inner.Verify(gpu),"Borrower cleanup restores original binding");
        playback=new(inner,source,bundle,64);registry.Destroy(1,new(create.ObjectId,0));Need(!playback.Advance(bundle.Interval) && playback.IsRetired && inner.IsRetired && other.Fingerprint(gpu)==otherProof,"Destroy stale lifetime");negatives++;
        handle=SceneStaticVisualPreview.Finalize(registry,create);inner=SceneStaticVisual.TryPublish(registry,handle,source,this,true)!;playback.Dispose();playback=new(inner,source,bundle,64);registry.Reset(2);Need(!playback.Tick() && playback.IsRetired && inner.IsRetired,"Generation stale lifetime");negatives++;
        bool invalidate=false;int predicateCalls=0;
        inner=SceneStaticVisual.TryPublish(source,pose,this,()=>!invalidate || ++predicateCalls<2,true)!;playback.Dispose();playback=new(inner,source,bundle,64);var beforeLateStale=playback.Snapshot;invalidate=true;
        Need(!playback.Advance(bundle.Interval) && playback.IsRetired && inner.IsRetired && ReferenceEquals(beforeLateStale,playback.Snapshot) && other.Fingerprint(gpu)==otherProof,"Lifetime invalidation after staging, before binding");negatives++;
        // Synthetic engine-resource invalidation exercises retirement after successful cursor staging.
        inner=SceneStaticVisual.TryPublish(source,pose,this,()=>!closed,true)!;playback.Dispose();playback=new(inner,source,bundle,64);
        static IEnumerable<MeshInstance3D> Nodes(Node n){foreach(Node c in n.GetChildren()){if(c is MeshInstance3D m)yield return m;foreach(var child in Nodes(c))yield return child;}}
        var brokenMaterial=Nodes(inner.Root).SelectMany(n=>Enumerable.Range(0,n.Mesh.GetSurfaceCount()).Select(i=>n.GetSurfaceOverrideMaterial(i))).OfType<StandardMaterial3D>().Distinct().Single();
        brokenMaterial.Dispose();bool engineFailure=false;try{playback.Advance(bundle.Interval);}catch(ObjectDisposedException){engineFailure=true;}
        Need(engineFailure && playback.IsRetired && inner.IsRetired && other.Fingerprint(gpu)==otherProof,"Unexpected engine binding failure retires adapter/inner");negatives++;
        GD.Print("switch_validation="+JsonSerializer.Serialize(new{negatives,frames=8,lodSelections=32,uniqueUploads=8,imageCopies=8,uploadedBytes=349536,archiveReads=37,standalonePreloads=true,modernSeconds=true,cursorsSynchronized=true,structureFixed=true,otherFixed=true,originalParity=false,syntheticAliasUploads=2,preclaimFailure=true,engineFailureRetired=true,lateStaleRetired=true}));
        await Shutdown();Need(SceneStaticVisual.Allocations==SceneStaticVisual.Disposals,"All borrowed visual roots retired");GD.Print("PASS: switch visual teardown archives_closed=true worker_observed=true wrapper_cache_released=true borrowed_inner_retired=true");GetTree().Quit();
    }
    private static string CapturePath(string root,int frame)=>Path.Combine(Path.GetDirectoryName(root)!,Path.GetFileNameWithoutExtension(root)+"-frame"+frame+".png");
    private async Task Capture(string path){await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);using var image=GetViewport().GetTexture().GetImage();if(image.SavePng(path)!=Error.Ok)throw new IOException("Capture failed");}
    private async Task Shutdown(){if(closed)return;closed=true;cancellation.Cancel();playback?.Dispose();inner?.Dispose();other?.Dispose();registry.Close();try{if(worker is not null)await worker;}catch(OperationCanceledException){}finally{worker=null;cancellation.Dispose();environment?.Dispose();environment=null;}}
    public override void _Notification(int what){if(what==NotificationWMCloseRequest)_=Close();}
    private async Task Close(){await Shutdown();if(GodotObject.IsInstanceValid(this) && IsInsideTree())GetTree().Quit(1);}
    public override void _ExitTree(){if(!closed)_=Shutdown().ContinueWith(t=>{_ = t.Exception;},TaskScheduler.Default);}
}
