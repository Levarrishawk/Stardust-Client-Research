using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

/// <summary>Offline original static resources with explicitly supplied synthetic projection; opaque TANO only.</summary>
public partial class SceneStaticStatePreview:Node
{
    private const ulong Terminal=0xE122334455667799;
    private SceneWorldCoordinator? world;
    private SceneWorldStaticStateVisual? actor;
    private Counter? owned;
    private Godot.Environment? environment;
    private long generation;
    private Dictionary<string,string> peer=new();
    private SceneStaticPresentationProjection? supplied;
    private int loads,closed,resolves;
    public override async void _Ready(){try{await Run();GD.Print("PASS: supplied static state complete");GetTree().Quit();}catch(Exception error){GD.PushError(error.ToString());try{await Shutdown();}catch(Exception drain){GD.PushError(drain.ToString());}GetTree().Quit(1);}}
    private void Accept(string key)=>Check(world!.Accept(generation,Convert.FromHexString(peer[key])),"opaque fixture "+key);
    private SceneWorldObjectSnapshot Snapshot()=>world!.Snapshots().Single();
    private static void Check(bool value,string why){if(!value)throw new InvalidDataException(why);}
    private async Task Run()
    {
        var args=OS.GetCmdlineUserArgs();string Arg(string key){int n=Array.IndexOf(args,key);if(n<0||n+1>=args.Length)throw new ArgumentException(key);return args[n+1];}
        peer=JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllBytes(Arg("--mixed-peer")))!;
        string assetRoot=Arg("--asset-root"),export=Arg("--customization-export"),retirement=Arg("--static-retire");Check(retirement is "generation" or "destroy","bounded retirement");
        bool gpu=DisplayServer.GetName()!="headless";string? capture=args.Contains("--customization-capture")?Arg("--customization-capture"):null;
        string prefix=Path.GetFullPath(ProjectSettings.GlobalizePath("res://../../local-private"))+Path.DirectorySeparatorChar;
        void Fresh(string path,string extension){Check(Path.GetFullPath(path).StartsWith(prefix,StringComparison.OrdinalIgnoreCase)&&Path.GetExtension(path)==extension&&!File.Exists(path)&&Directory.Exists(Path.GetDirectoryName(Path.GetFullPath(path))),"fresh private output");}
        Fresh(export,".json");if(capture is not null){Check(gpu,"GPU mode");foreach(string path in Captures(capture))Fresh(path,".png");}
        byte[] cfg=File.ReadAllBytes(Arg("--asset-config"));Check(Convert.ToHexString(SHA256.HashData(cfg))==RetainedSkeletalDetailBundleReader.ConfigurationPin,"config pin");
        var counts=new Dictionary<string,int>();var raw=new Dictionary<string,byte[]>();Task<SceneObjectResources>? job=null;
        var catalog=StaticSwitchTexturePreview.Catalog();var options=new Dictionary<SceneObjectResourceRecipe,SceneObjectResourceOptions>{{SceneObjectResourceRecipe.StaticAppearance,new SceneStaticResourceOptions(SceneStaticMaterialPolicy.RequireComplete,new(StaticSwitchTexturePreview.ShaderPath,StaticSwitchTexturePreview.ShaderPin,0))}};
        Task<SceneObjectResources> Load(SceneObjectResourceRequest request,CancellationToken token)
        {
            loads++;job=Task.Run(()=>{try{using var text=new StringReader(Encoding.UTF8.GetString(cfg));using var overlay=ConfiguredTreOverlay.Open(assetRoot,text);
                TreLookupResult Resolve(string path){token.ThrowIfCancellationRequested();counts[path]=counts.GetValueOrDefault(path)+1;var found=overlay.Lookup(path);if(found.Payload is not null)raw[path]=found.Payload.ToArray();return found;}
                return SceneObjectResourceRouter.Load(request.Snapshot.Create,request.Catalog,request.Options,Resolve,token);
            }finally{Interlocked.Increment(ref closed);}},token);return job;
        }
        world=new(catalog,options,Load,SceneCreateZeroQuaternionPolicy.Reject);generation=world.BeginSelection(Terminal).Generation;
        Accept("static_start");Accept("terminal_create");Accept("terminal_baseline");Accept("terminal_end");Check(world.CompleteMinimumSceneBarrier(generation,true)is not null,"minimum barrier");world.PollObjectResources();await job!;Array.Clear(cfg);
        var publication=world.PollObjectResources().Prepared.Single();Accept("ready");Check(world.ReplicationMode==SceneWorldReplicationMode.Opaque&&Snapshot().Replication is null,"no typed TANO model");
        var visual=(SceneWorldObjectVisual)new SceneWorldObjectVisualFactory(world,this,true).Create(publication,Snapshot())!;var geometry=visual.StaticVisual!;
        Check(loads==1&&closed==1&&raw.Count==27&&counts.Values.All(n=>n==1)&&publication.Resources.ExternalReads==27&&publication.Resources.SourceBytes==1401794&&geometry.Verify(gpu),"original terminal closure/readback");
        var first=Names("name_first_baseline");var latest=Names("name_latest_baseline");var incarnation=Snapshot().Object.Incarnation;
        Guards(visual,first,latest,gpu);
        supplied=new(incarnation,1,first,255);owned=new(publication,visual);actor=new(world,owned,visual,_=>supplied,n=>{resolves++;return Display(n);});
        var pose=geometry.Root.Transform;string identities=geometry.Fingerprint(gpu)!;ulong labelId=actor.LabelId;
        environment=new(){BackgroundMode=Godot.Environment.BGMode.Color,BackgroundColor=new Color("#14202e")};AddChild(new WorldEnvironment{Environment=environment});
        var bounds=geometry.Bounds;var mesh=publication.Resources.Static!.Appearance.Meshes[publication.Resources.Static.Appearance.Geometry.Root.Detail!.Levels[3].ChildPath!];
        var local=mesh.Mesh.Primitives.Where(p=>p.ShaderPath==StaticSwitchTexturePreview.ShaderPath).SelectMany(p=>p.Vertices).Aggregate(Vector3.Zero,(v,x)=>v+new Vector3(x.Normal.X,x.Normal.Y,-x.Normal.Z)).Normalized();var outward=(pose.Basis*local).Normalized();var side=new Vector3(outward.Z,0,-outward.X).Normalized();float radius=Math.Max(bounds.Size.Length()*.75f,1);
        var camera=new Camera3D{Current=true,Fov=50,Position=bounds.GetCenter()+outward*radius*2.4f+Vector3.Up*radius*.35f+side*radius*.45f};AddChild(camera);camera.LookAt(bounds.GetCenter()+Vector3.Up*.15f);
        object State()=>new{revision=actor.Revision,visibility=actor.Visibility,text=actor.Text,labelId=actor.LabelId,labelVisible=actor.LabelVisible,rootVisible=geometry.Root.Visible,
            rawFile=Convert.ToHexString(supplied!.Names.NameFile.CopyBytes()),rawKey=Convert.ToHexString(supplied.Names.NameKey.CopyBytes()),supplied.Names.NameSpacer,units=supplied.Names.CustomNameUnits,resolves,identities=geometry.Fingerprint(gpu),pose=geometry.Root.Transform.ToString(),owned.Ticks,owned.Applies};
        var states=new List<object>{State()};if(capture is not null)await Capture(Captures(capture)[0]);
        supplied=new(incarnation,2,latest,255);Check(actor.Apply(Snapshot())&&actor.Text=="LATEST CAPTION"&&resolves==2,"current supplied name changes");states.Add(State());if(capture is not null)await Capture(Captures(capture)[1]);
        int before=resolves;Check(actor.Tick(0)&&resolves==before,"same Names skips resolution");
        supplied=new(incarnation,3,latest,0);Check(actor.Tick(0)&&!geometry.Root.Visible&&!actor.LabelVisible&&resolves==before,"zero hides root and caption without resolution");states.Add(State());if(capture is not null)await Capture(Captures(capture)[2]);
        supplied=new(incarnation,4,latest,255);Check(actor.Tick(0)&&geometry.Root.Visible&&actor.LabelVisible&&resolves==before,"raw255 restores projection without reload");states.Add(State());if(capture is not null)await Capture(Captures(capture)[3]);
        Check(geometry.Root.Transform==pose&&geometry.Fingerprint(gpu)==identities&&geometry.Verify(gpu)&&actor.LabelId==labelId&&loads==1&&closed==1&&owned.Ticks==3&&owned.Applies==1,"pose/resources/upload and exact inner call counts");
        var evidence=StaticSwitchTexturePreview.FrameEvidence(publication.Resources.Static!,0,counts);
        var readback=gpu?Readback(geometry.Root,publication.Resources.Static!):new Dictionary<string,string[]>();
        File.WriteAllText(export,JsonSerializer.Serialize(new{gpu,retirement,inputPolicy="SuppliedProjectionOpaqueTano",states,readback,raw=raw.ToDictionary(p=>p.Key,p=>Convert.ToBase64String(p.Value)),evidence}));raw.Clear();
        if(retirement=="destroy")Accept("terminal_destroy");else world.BeginSelection(Terminal);
        Check(!actor.Tick(0)&&actor.IsRetired&&geometry.IsRetired&&owned.Disposals==1&&!actor.DrainCompletion.IsCompleted&&actor.LabelId==0,"retirement frees label/inner and freezes held drain");actor.Dispose();Check(owned.Disposals==1,"dispose once");owned.Release();await actor.DrainCompletion;
        await Shutdown();Check(SceneStaticVisual.Allocations==1&&SceneStaticVisual.Disposals==1&&loads==1&&closed==1,"root/archive balanced");GD.Print("PASS: supplied static state retirement="+retirement+" root=1/1 archive=1 raw255=true cache=true inner_once=true drain=true");
    }
    private static Dictionary<string,string[]> Readback(Node root,SceneStaticResources resources)
    {
        var result=new Dictionary<string,string[]>();
        IEnumerable<MeshInstance3D> Meshes(Node node){foreach(Node child in node.GetChildren()){if(child is MeshInstance3D mesh)yield return mesh;foreach(var nested in Meshes(child))yield return nested;}}
        foreach(var node in Meshes(root))
        {
            var mesh=resources.Appearance.Meshes[node.GetMeta("original_mesh_path").AsString()];
            for(int surface=0;surface<mesh.Mesh.Primitives.Count;surface++)
            {
                var material=mesh.Materials[mesh.Mesh.Primitives[surface].ShaderPath];
                string path=material.References.Textures.Single(t=>t.Tag==0x4D41494E).AssetPath??throw new InvalidDataException("MAIN path missing");
                var gpu=node.GetSurfaceOverrideMaterial(surface)??node.Mesh.SurfaceGetMaterial(surface);
                Texture2D texture=gpu is ShaderMaterial shader?(Texture2D)shader.GetShaderParameter("albedo_texture").AsGodotObject():((StandardMaterial3D)gpu).AlbedoTexture;
                using var image=texture.GetImage();byte[] bytes=image.GetData();int offset=0;
                var hashes=mesh.Textures[path].Mips.Select(m=>{int length=m.Rgba.Length;string hash=Convert.ToHexString(SHA256.HashData(bytes.AsSpan(offset,length)));offset+=length;return hash;}).ToArray();
                Check(offset==bytes.Length,"readback exact mip length");if(result.TryGetValue(path,out var prior))Check(prior.SequenceEqual(hashes),"shared readback agrees");else result.Add(path,hashes);
            }
        }
        return result;
    }
    private CreatureView3NameState Names(string key)=>CreatureView3NameState.FromBaseline(CreatureView3BaselineCodec.Decode(Convert.FromHexString(peer[key]),new(1024,1024,1024,128,16384)));
    private static string Display(CreatureView3NameState names)
    {
        if(names.CustomNameUnits.Count>64||names.NameFile.Length>32||names.NameKey.Length>32)throw new InvalidDataException("Synthetic resolver bound");
        if(names.CustomNameUnits.Count!=0){if(names.CustomNameUnits.Any(u=>u is <32 or >126))throw new InvalidDataException("Synthetic printable units only");return new string(names.CustomNameUnits.Select(u=>(char)u).ToArray());}
        if(names.NameFile.CopyBytes().AsSpan().SequenceEqual("diagnostic"u8)&&names.NameKey.CopyBytes().AsSpan().SequenceEqual("alpha"u8))return "FIRST TERMINAL";
        throw new InvalidDataException("Synthetic mapping missing");
    }
    private async Task Shutdown(){actor?.Dispose();owned?.Release();if(actor is not null)await actor.DrainCompletion;world?.Close();if(world is not null)await world.DrainCompletion;environment?.Dispose();environment=null;}
    public static string[] Captures(string first)=>new[]{"-first","-latest","-hidden","-shown"}.Select(s=>Path.Combine(Path.GetDirectoryName(first)!,Path.GetFileNameWithoutExtension(first)+s+".png")).ToArray();
    private async Task Capture(string path){await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);using var image=GetViewport().GetTexture().GetImage();Check(image.SavePng(path)==Error.Ok,"capture");}
    private sealed class Counter:ISceneWorldObjectPresentation,ISceneWorldObjectPresentationDrain
    {
        private readonly ISceneWorldObjectPresentation? inner;private readonly TaskCompletionSource<bool> drain=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Counter(ScenePreparedObjectPublication publication,ISceneWorldObjectPresentation? inner=null){Publication=publication;this.inner=inner;if(inner is null)drain.SetResult(true);}
        public ScenePreparedObjectPublication Publication{get;}public int Ticks{get;private set;}public int Applies{get;private set;}public int Disposals{get;private set;}
        public Task DrainCompletion=>drain.Task;public bool Tick(double seconds){Ticks++;return inner?.Tick(seconds)??true;}public bool Apply(SceneWorldObjectSnapshot snapshot){Applies++;return inner?.Apply(snapshot)??true;}public void Dispose(){Disposals++;inner?.Dispose();}public void Release()=>drain.TrySetResult(true);
    }
    private void Guards(SceneWorldObjectVisual basis,CreatureView3NameState first,CreatureView3NameState latest,bool gpu)
    {
        var incarnation=Snapshot().Object.Incarnation;var valid=new SceneStaticPresentationProjection(incarnation,1,first,255);int tests=0;
        foreach(Func<SceneWorldObjectSnapshot,SceneStaticPresentationProjection?> bad in new Func<SceneWorldObjectSnapshot,SceneStaticPresentationProjection?>[]{_=>null,_=>valid with{Revision=0},_=>valid with{Incarnation=incarnation with{ObjectId=999}},_=>throw new InvalidDataException("source failure")})
        {var counter=new Counter(basis.Publication);bool rejected=false;try{_=new SceneWorldStaticStateVisual(world!,counter,basis,bad,Display);}catch(InvalidDataException){rejected=true;}Check(rejected&&counter.Disposals==0,"ctor retains ownership");counter.Dispose();tests++;}
        foreach(Func<CreatureView3NameState,string> bad in new Func<CreatureView3NameState,string>[] {_=>null!,_=>new string('X',257),_=>"x\n",_=>"\uD800",_=>throw new InvalidDataException("resolver failure")})
        {var counter=new Counter(basis.Publication);bool rejected=false;try{_=new SceneWorldStaticStateVisual(world!,counter,basis,_=>valid,bad);}catch(InvalidDataException){rejected=true;}Check(rejected&&counter.Disposals==0,"caption ctor retains ownership");counter.Dispose();tests++;}
        foreach(string kind in new[]{"null","old","reused","foreign","mutate","reentry","moved","resolver","unstable"})
        {
            var counter=new Counter(basis.Publication);SceneStaticPresentationProjection? value=kind=="old"?valid with{Revision=2}:valid;SceneWorldStaticStateVisual? probe=null;bool armed=false;int reads=0;
            SceneStaticPresentationProjection? Source(SceneWorldObjectSnapshot snapshot)
            {if(armed&&kind=="mutate")Accept("terminal_delta");if(armed&&kind=="reentry"){try{probe!.Tick(0);}catch(InvalidOperationException){}}if(armed&&kind=="unstable"&&++reads==2)value=value! with{Revision=value!.Revision+1};return value;}
            string Resolve(CreatureView3NameState n){if(armed&&kind=="resolver")throw new InvalidDataException("late resolver");if(armed&&kind=="moved")basis.StaticVisual!.Root.GetChildren().OfType<Label3D>().Single().Reparent(this);return Display(n);}
            probe=new(world!,counter,basis,Source,Resolve);Check(probe.Tick(0)&&counter.Ticks==1,"inner once");armed=true;
            value=kind switch{"null"=>null,"old"=>valid,"reused"=>valid with{Names=latest},"foreign"=>valid with{Incarnation=incarnation with{ObjectId=999}},_=>valid with{Revision=2,Names=latest}};
            bool rejected=false;try{probe.Tick(0);}catch(Exception error)when(error is InvalidOperationException or InvalidDataException){rejected=true;}
            Check(rejected&&probe.IsRetired&&counter.Disposals==1&&probe.LabelId==0,"late failure cleanup "+kind);probe.Dispose();Check(counter.Disposals==1,"no double disposal");tests++;
        }
        var noLabelCounter=new Counter(basis.Publication);using(var noLabel=new SceneWorldStaticStateVisual(world!,noLabelCounter,basis,_=>valid,Display,createLabel:false))Check(noLabel.LabelId==0&&noLabel.Tick(0),"optional label absent");
        var ownerCounter=new Counter(basis.Publication);bool worker=Task.Run(()=>{try{_=new SceneWorldStaticStateVisual(world!,ownerCounter,basis,_=>valid,Display);return false;}catch(InvalidOperationException){return true;}}).GetAwaiter().GetResult();Check(worker&&ownerCounter.Disposals==0,"worker ctor rejected");ownerCounter.Dispose();
        var staleCounter=new Counter(basis.Publication);using(var stale=new SceneWorldStaticStateVisual(world!,staleCounter,basis,_=>valid,Display))
        {var old=Snapshot();Accept("terminal_delta");bool rejected=false;try{stale.Apply(old);}catch(InvalidDataException){rejected=true;}Check(rejected&&stale.IsRetired&&staleCounter.Applies==0&&staleCounter.Disposals==1,"stale Apply before inner");}
        var threadCounter=new Counter(basis.Publication);using(var threadProbe=new SceneWorldStaticStateVisual(world!,threadCounter,basis,_=>valid,Display))
        {bool rejected=Task.Run(()=>{try{threadProbe.Tick(0);return false;}catch(InvalidOperationException){return true;}}).GetAwaiter().GetResult();Check(rejected&&!threadProbe.IsRetired&&threadCounter.Ticks==0,"late owner guard preserves owner cleanup");}
        GD.Print("PASS: static additional guards stale_apply=true late_owner=true");
        Check(basis.StaticVisual!.Root.GetChildren().OfType<Label3D>().Count()==0&&basis.StaticVisual.Verify(gpu),"probe labels removed/base retained");GD.Print("PASS: static guards tests="+tests+" ctor_late_stale_callback_reentry_owner_optional_label=true");
    }
}
