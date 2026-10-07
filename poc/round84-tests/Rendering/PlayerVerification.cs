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
using Stardust.Core.Configuration;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

/// <summary>Real adapter/pump/host tests with authored applications; explicit original resources optional.</summary>
public partial class PlayerVerification : Node
{
    private int checks, callbacks, calls, closed;
    private readonly List<string> observations = new();
    private readonly List<ReplacementPlayerPresentationHost> adapters = new();
    private readonly List<object> originals = new();
    private Dictionary<string,string> peer = new();
    private string output = "", assetRoot = "", config = "";
    private bool removeOriginal, freeOriginal;
    private const ulong Selected = 42;
    private void Check(bool good,string why) { checks++;if (!good)throw new InvalidDataException(why);observations.Add(why); }
    private void Reject<T>(Action action,string why) where T:Exception
    { bool failed=false;try { action(); } catch(T) { failed=true; }Check(failed,why); }
    private async Task Frames(int count=2) { for(int i=0;i<count;i++)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame); }
    private async Task Until(Func<bool> ready)
    { var end=DateTime.UtcNow.AddSeconds(30);while(!ready()){if(DateTime.UtcNow>end)throw new TimeoutException("Native component deadline");await Frames(1);} }
    private async Task End(ReplacementPlayerPresentationHost adapter,bool fault=false)
    { await Until(()=>adapter.Completion.IsCompleted);bool failed=false;try{await adapter.Completion;}catch{failed=true;}Check(failed==fault&&(adapter.Failure is not null)==fault,"completion/failure agree"); }
    private ReplacementPlayerPresentationHost Adapter() { var a=new ReplacementPlayerPresentationHost();AddChild(a);adapters.Add(a);return a; }
    private static readonly string Zero = new('0',64);
    private static SceneWorldCoordinator World(ulong oid=Selected,Func<SceneObjectResourceRequest,CancellationToken,Task<SceneObjectResources>>? load=null)
    {
        string path="object/root.iff";var catalog=new ObjectTemplateCatalog([new(LegacyAssetPath.Checksum(path),path,Zero,Zero)]);
        var options=new Dictionary<SceneObjectResourceRecipe,SceneObjectResourceOptions>{[SceneObjectResourceRecipe.CreatureDetail0]=new SceneCreatureResourceOptions(new("appearance/lat/test.lat","idle",Zero),new Dictionary<string,LogicalAnimationClipSelection>{{"walk",new("appearance/lat/test.lat","idle",Zero)},{"run",new("appearance/lat/test.lat","idle",Zero)}},false)};
        var world=new SceneWorldCoordinator(catalog,options,load??((_,_)=>Task.FromException<SceneObjectResources>(new InvalidDataException("No asset input"))),SceneCreateZeroQuaternionPolicy.Reject,replicationPolicy:new(SceneWorldReplicationMode.CreatureView3Scalars,new(1024,1024,1024,128,16384),16,16384));
        world.BeginSelection(oid);return world;
    }
    private void Stage(SceneWorldCoordinator world,bool unrelated=false)
    {
        string prefix=unrelated?"other":"player";
        foreach(string key in new[]{"start",prefix+"Create",prefix+"Baseline",prefix+"End"})Check(world.Accept(world.Generation,Convert.FromHexString(peer[key])),"authored "+key+" accepted");
        Check(world.CompleteMinimumSceneBarrier(world.Generation,true) is not null,"explicit test minimum barrier");
        Check(world.Accept(world.Generation,Convert.FromHexString(peer["ready"])),"authored server ready accepted");
    }
    public override async void _Ready()
    {
        try
        {
            var args=OS.GetCmdlineUserArgs();string Arg(string k)=>args[Array.IndexOf(args,k)+1];
            output=Arg("--out");removeOriginal=args.Contains("--native-exit");freeOriginal=args.Contains("--native-free");peer=JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllText(Arg("--peer")))!;
            GetTree().AutoAcceptQuit=false;await Structural();
            if(args.Contains("--asset-root")){assetRoot=Arg("--asset-root");config=Arg("--config");await Original();}
            File.WriteAllText(output,JsonSerializer.Serialize(new{Passed=true,Checks=checks,Observations=observations,Originals=originals,ArchiveCalls=calls,ArchivesClosed=closed,Gpu=DisplayServer.GetName()!="headless",SyntheticPeer=true,OriginalExecutable=false,LiveCompatibility=false}));
            GD.Print("PASS selected-player adapter checks="+checks);GetTree().Quit();
        }
        catch(Exception error)
        {
            GD.PushError(error.ToString());foreach(var adapter in adapters)try{adapter.RequestClose();}catch{}
            GetTree().Quit(2);
        }
    }
    private async Task Structural()
    {
        using var world=World();var detached=new ReplacementPlayerPresentationHost();
        try { Reject<InvalidOperationException>(()=>detached.Configure(world,Selected,_=>{}),"detached rejection retains world"); }finally { detached.Free(); }
        var adapter=Adapter();Reject<ArgumentNullException>(()=>adapter.Configure(null!,Selected,_=>{}),"null world");Reject<ArgumentNullException>(()=>adapter.Configure(world,Selected,null!),"null callback");Reject<ArgumentOutOfRangeException>(()=>adapter.Configure(world,0,_=>{}),"zero identity");
        Check(world.State==ZoneSceneTransitionState.Selecting,"invalid admission did not close borrowed world");
        using var alreadyClosed=World();alreadyClosed.Close();var rejectedClosed=Adapter();Reject<ArgumentException>(()=>rejectedClosed.Configure(alreadyClosed,Selected,_=>{}),"closed caller world rejects before pump acquisition");rejectedClosed.RequestClose();await End(rejectedClosed);
        Check(await Task.Run(()=>{try{adapter.Configure(world,Selected,_=>{});return false;}catch(InvalidOperationException){return true;}}),"foreign configure rejected before ownership");
        Task completion=adapter.Completion;adapter.Configure(world,Selected,_=>callbacks++);await Until(()=>adapter.CompletedFrames>=2);
        Check(callbacks==(int)adapter.CompletedFrames&&!adapter.PlayerPublished&&adapter.Pump.Consumer.Count==0,"real BeforeStep order/count without invented player");
        using var untouched=World();Reject<InvalidOperationException>(()=>adapter.Configure(untouched,Selected,_=>{}),"configuration once");Check(untouched.State==ZoneSceneTransitionState.Selecting,"second candidate remains caller-owned");
        Check(await Task.Run(()=>{try{_=adapter.Pump;return false;}catch(InvalidOperationException){return true;}}),"borrowed pump observation owner guard");
        adapter.RequestClose();adapter.RequestClose();await End(adapter);int before=callbacks;ulong frames=adapter.CompletedFrames;await Frames();
        Check(callbacks==before&&adapter.CompletedFrames==frames&&!adapter.PlayerPublished&&ReferenceEquals(completion,adapter.Completion)&&world.State==ZoneSceneTransitionState.Closed&&adapter.Pump.Consumer.Count==0,"close clears callback, stable primitive history and one drained world");
        using var faultWorld=World();var fault=Adapter();fault.Configure(faultWorld,Selected,_=>throw new IOException("authored update failure"));await End(fault,true);Check(fault.CompletedFrames==0&&faultWorld.State==ZoneSceneTransitionState.Closed,"update failure before pump publication");
        using var reentryWorld=World();using var rejected=World();var reentry=Adapter();reentry.Configure(reentryWorld,Selected,_=>{try{reentry.Configure(rejected,Selected,_=>{});}catch(InvalidOperationException){}});await End(reentry,true);Check(reentry.CompletedFrames==0&&rejected.State==ZoneSceneTransitionState.Selecting,"caught configure reentry remains failure, rejected world untouched");
        using var closeWorld=World();var inside=Adapter();int n=0;inside.Configure(closeWorld,Selected,_=>{n++;inside.RequestClose();});await End(inside);Check(n==1&&inside.CompletedFrames==0,"callback close suppresses step");
        using var generationWorld=World();var stale=Adapter();stale.Configure(generationWorld,Selected,_=>generationWorld.BeginSelection(Selected));await End(stale,true);Check(stale.CompletedFrames==0,"generation mutation requires fresh adapter");
        using var externallyClosed=World();var external=Adapter();external.Configure(externallyClosed,Selected,_=>externallyClosed.Close());await End(external,true);Check(external.CompletedFrames==0,"external world close stops update and host before step");
        using var wrongWorld=World(43);Check(wrongWorld.Accept(wrongWorld.Generation,Convert.FromHexString(peer["otherStart"])),"other selected scene accepted by its world");var wrong=Adapter();Reject<ArgumentException>(()=>wrong.Configure(wrongWorld,Selected,_=>{}),"wrong scene identity rejects without close");Check(wrongWorld.State==ZoneSceneTransitionState.Initializing,"wrong world remains open");wrong.RequestClose();await End(wrong);
        var pending=new TaskCompletionSource<SceneObjectResources>(TaskCreationOptions.RunContinuationsAsynchronously);using var delayed=World(load:(_,_)=>pending.Task);var exiting=Adapter();
        // Fixture CRC matches the synthetic catalog. The pending loader deliberately ignores cancellation until released.
        Stage(delayed);exiting.Configure(delayed,Selected,_=>{});await Until(()=>delayed.OutstandingJobs==1);RemoveChild(exiting);
        Check(!exiting.Completion.IsCompleted&&!exiting.PlayerPublished&&exiting.GetNode<SceneWorldPresentationHost>("SelectedPlayerProcessHost").IsClosing,"native exit immediately closes admission, completion waits actual job");pending.SetCanceled();await End(exiting);Check(delayed.State==ZoneSceneTransitionState.Closed&&delayed.OutstandingJobs==0&&exiting.Pump.Consumer.Count==0,"native exit drains late canceled job");exiting.Free();
        using var freshWorld=World();var fresh=Adapter();fresh.Configure(freshWorld,Selected,_=>{});await Until(()=>fresh.CompletedFrames>=1);Check(fresh.GetInstanceId()!=adapter.GetInstanceId()&&fresh.CompletedFrames>0&&!fresh.PlayerPublished,"fresh owner independent of retired host");fresh.RequestClose();await End(fresh);
    }
    private async Task Original()
    {
        peer["playerCreate"]=peer["originalPlayerCreate"];peer["otherCreate"]=peer["originalOtherCreate"];
        const string path="object/creature/player/shared_human_male.iff";
        using var inputs=OriginalInstallationInputs.Open(assetRoot,config,[path]);var overlay=inputs.BorrowedOverlay;
        string Hash(byte[] raw)=>Convert.ToHexString(SHA256.HashData(raw));
        byte[] Read(string p)=>overlay.Lookup(p).Payload??throw new InvalidDataException("Missing original input");
        string rootPin=Hash(Read(path));var profile=SharedCreatureTemplateProfileReader.Load(overlay.Lookup,path,rootPin);
        var catalog=new ObjectTemplateCatalog([new(LegacyAssetPath.Checksum(path),path,rootPin,Hash(Read(profile.AppearancePath)))]);
        LogicalAnimationClipSelection Clip(string name,int[]? route=null)
        {
            const string table="appearance/lat/all_m.lat";var lat=LogicalAnimationTableDecoder.Decode(Read(table));var entry=lat.LookupEntries.Single(e=>e.Name==name);
            var proxy=LogicalAnimationProxyDecoder.Decode(entry.Template!,route);return new(table,name,Hash(Read(proxy.ClipPath)),route);
        }
        var options=new Dictionary<SceneObjectResourceRecipe,SceneObjectResourceOptions>{[SceneObjectResourceRecipe.CreatureDetail0]=new SceneCreatureResourceOptions(Clip("emt_standing_idle_ambient_1_ag"),new Dictionary<string,LogicalAnimationClipSelection>{{"walk",Clip("loop_standing",[0,1])},{"run",Clip("loop_standing",[0,2])}},true)};
        Task<SceneObjectResources> Load(SceneObjectResourceRequest request,CancellationToken token)
        {
            calls++;return Task.Run(()=>{try{using var reader=File.OpenText(config);using var owned=ConfiguredTreOverlay.Open(assetRoot,reader);return SceneObjectResourceRouter.Load(request.Snapshot.Create,request.Catalog,request.Options,owned.Lookup,token);}finally{Interlocked.Increment(ref closed);}},token);
        }
        var environment=new Godot.Environment{BackgroundMode=Godot.Environment.BGMode.Color,BackgroundColor=new Color("#14202e"),AmbientLightSource=Godot.Environment.AmbientSource.Color,AmbientLightColor=Colors.White,AmbientLightEnergy=.8f};
        var sky=new WorldEnvironment{Environment=environment};AddChild(sky);var camera=new Camera3D{Current=true,Fov=40,Position=new(1.5f,3.3f,-6)};AddChild(camera);camera.LookAt(new(1.5f,3.1f,3));
        var sun=new DirectionalLight3D{RotationDegrees=new(-30,-30,0),LightEnergy=1};AddChild(sun);
        using(var original=new SceneWorldCoordinator(catalog,options,Load,SceneCreateZeroQuaternionPolicy.Reject,replicationPolicy:new(SceneWorldReplicationMode.CreatureView3Scalars,new(1024,1024,1024,128,16384),16,16384)))
        {
            original.BeginSelection(Selected);Stage(original);var adapter=Adapter();adapter.Configure(original,Selected,_=>{});await Until(()=>adapter.PlayerPublished);
            var visual=(SceneWorldObjectVisual)adapter.Pump.Consumer.Find(Selected)!;var root=visual.CreatureVisual!.Root;ulong id=root.GetInstanceId();var snapshot=original.Snapshots().Single();
            Check(adapter.Pump.Consumer.Count==1&&snapshot.Object.Handle.ObjectId==Selected&&visual.Publication.Ticket.Handle.ObjectId==Selected,"actual selected original actor identity");
            Check(root.Transform.Origin.DistanceTo(new(1.5f,2,3))<.00001f&&root.Transform.Basis.IsEqualApprox(Basis.Identity),"independent native-to-Godot transform and unit height");
            Check(visual.CreatureVisual.Verify(DisplayServer.GetName()!="headless"),"original geometry/upload boundaries");
            if(DisplayServer.GetName()!="headless"){await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);using var image=GetViewport().GetTexture().GetImage();Check(image.SavePng(Path.ChangeExtension(output,"png"))==Error.Ok,"private original selected screenshot");}
            originals.Add(new{ObjectId=Selected,RootInstanceId=id,Position=new[]{root.Transform.Origin.X,root.Transform.Origin.Y,root.Transform.Origin.Z},RootObjectSha256=rootPin,AppearancePath=profile.AppearancePath,Published=true,Frames=adapter.CompletedFrames,TextureCount=visual.CreatureVisual.TextureCount});
            if(freeOriginal)adapter.Free();else if(removeOriginal)RemoveChild(adapter);else adapter.RequestClose();await End(adapter,freeOriginal);Check(GodotObject.InstanceFromId(id) is null&&!adapter.PlayerPublished&&adapter.Pump.Consumer.Count==0&&original.OutstandingJobs==0,freeOriginal?"native free abort reports retired root and drains jobs":removeOriginal?"native removal frees original root and drains jobs":"controlled close frees original root and drains jobs");
            if(removeOriginal&&!freeOriginal)adapter.Free();
        }
        int allocations=SceneCreatureVisual.Allocations;
        using(var unrelated=new SceneWorldCoordinator(catalog,options,Load,SceneCreateZeroQuaternionPolicy.Reject,replicationPolicy:new(SceneWorldReplicationMode.CreatureView3Scalars,new(1024,1024,1024,128,16384),16,16384)))
        {
            unrelated.BeginSelection(Selected);var adapter=Adapter();adapter.Configure(unrelated,Selected,_=>{});Stage(unrelated,true);await End(adapter,true);
            Check(SceneCreatureVisual.Allocations==allocations&&!adapter.PlayerPublished&&adapter.Pump.Consumer.Count==0,"unrelated actual original publication fails before visual allocation");
        }
        Check(calls==closed&&SceneCreatureVisual.Allocations==SceneCreatureVisual.Disposals+(freeOriginal?1:0),freeOriginal?"native free abort drains sources; successful visual disposal counter remains incomplete":"all original source jobs and roots retired");camera.Free();sun.Free();sky.Free();environment.Dispose();
    }
}
