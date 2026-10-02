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

/// <summary>Separate bounded two-incarnation replacement preview; direct synthetic application messages.</summary>
public partial class TwoCreatureWorldPreview : Node
{
    public const ulong A=0xF122334455667788,B=0xE122334455667799;
    private readonly TaskCompletionSource<bool> completion=new(TaskCreationOptions.RunContinuationsAsynchronously);
    public Task Completion=>completion.Task;
    private SceneWorldCoordinator? world;
    private SceneWorldPresentationConsumer? consumer;
    private Godot.Environment? environment;
    private readonly List<Task<SceneCreatureResources>> jobs=new();
    private readonly List<object> loads=new();
    private readonly TaskCompletionSource<bool> entered=new(TaskCreationOptions.RunContinuationsAsynchronously);
    private Dictionary<string,string> fixture=new();
    private long generation;
    private int calls,closed,cancelStarted;
    private bool disposed,cancel;
    private readonly int ownerThread=System.Environment.CurrentManagedThreadId;
    public static Dictionary<string,string> ReadFixture(string path)
    {
        var file=new FileInfo(path);if(!file.Exists || file.Length is <1 or >65536)throw new ArgumentException("Bounded fixture required.");
        var data=JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllBytes(path))??throw new InvalidDataException("Fixture object required.");
        if(data.Count!=18 || data.Values.Any(v=>v.Length>4096))throw new InvalidDataException("Fixture shape/budget.");
        foreach(string key in new[]{"start","ready","a_invalid_height","a_bad"}.Concat(new[]{"a","b"}.SelectMany(k=>new[]{"create","baseline","hidden","shown","height","end","destroy"}.Select(s=>k+"_"+s))))if(!data.ContainsKey(key))throw new InvalidDataException("Fixture key absent.");
        return data;
    }
    public static void Stage(SceneWorldCoordinator owner,long gen,IReadOnlyDictionary<string,string> bytes)
    {Accept(owner,gen,bytes,"start");foreach(string id in new[]{"a","b"})foreach(string op in new[]{"create","baseline","end"})Accept(owner,gen,bytes,id+"_"+op);}
    public static void Accept(SceneWorldCoordinator owner,long gen,IReadOnlyDictionary<string,string> fixture,string key)
    {var bytes=Convert.FromHexString(fixture[key]);try{if(!owner.Accept(gen,bytes))throw new InvalidDataException("Application not accepted: "+key);}finally{Array.Clear(bytes);}}
    public static object Batch(string stage,SceneWorldChangeBatch b)=>new{stage,b.Generation,b.MutationRevision,entries=b.Entries.Select(e=>new{e.ObjectId,kind=(int)e.Kind,current=e.Current is null?null:new{incarnation=e.Current.Object.Incarnation.CreationRevision,handle=e.Current.Object.Handle.Revision,revision=e.Current.Replication!.Revision,heightBits=BitConverter.SingleToUInt32Bits(e.Current.Replication.Scalars.Height),visibility=e.Current.Replication.Baseline.Visibility,wounds=e.Current.Replication.Wounds!.Values.ToArray()}}).ToArray()};
    public override async void _Ready()
    {
        try{await Run(OS.GetCmdlineUserArgs());completion.TrySetResult(true);}
        catch(Exception failure){GD.PushError("Two creature preview: "+failure);try{await Shutdown();}catch(Exception cleanup){GD.PushError("Two creature cleanup: "+cleanup.GetType().Name);}completion.TrySetException(failure);GetTree().Quit(1);}
    }
    private async Task Run(string[] args)
    {
        string Read(string name){int at=Array.IndexOf(args,name);if(at<0 || at+1>=args.Length || args.Count(v=>v==name)!=1)throw new ArgumentException("One option required: "+name);return args[at+1];}
        GetTree().AutoAcceptQuit=false;
        string root=Read("--asset-root");byte[] configuration=File.ReadAllBytes(Read("--asset-config"));
        if(Convert.ToHexString(SHA256.HashData(configuration))!=RetainedSkeletalDetailBundleReader.ConfigurationPin)throw new InvalidDataException("Configuration pin differs.");
        fixture=ReadFixture(Read("--two-fixture"));var catalog=ObjectTemplateCatalogProfile.Decode(File.ReadAllBytes(Read("--catalog-profile")));
        using var profile=File.OpenRead(Path.Combine(ProjectSettings.GlobalizePath("res://"),"..","preview-profiles","human-locomotion.json"));var bank=SkeletalAnimationBankProfile.Load(profile);
        var idle=new LogicalAnimationClipSelection("appearance/lat/all_m.lat","emt_standing_idle_ambient_1_ag","798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B");
        cancel=args.Contains("--two-cancel");bool gpu=DisplayServer.GetName()!="headless";
        string? capture=args.Contains("--two-capture")?Read("--two-capture"):null;
        if(cancel && gpu || capture is not null && (!gpu || cancel))throw new ArgumentException("Separate bounded modes required.");
        if(capture is not null)
        {
            string prefix=Path.GetFullPath(Path.Combine(ProjectSettings.GlobalizePath("res://"),"..","..","local-private"))+Path.DirectorySeparatorChar;
            foreach(string image in Captures(capture))if(!Path.GetFullPath(image).StartsWith(prefix,StringComparison.OrdinalIgnoreCase) || Path.GetExtension(image)!=".png" || File.Exists(image) || !Directory.Exists(Path.GetDirectoryName(Path.GetFullPath(image))))throw new ArgumentException("Fresh private PNG set required.");
        }
        Task<SceneCreatureResources> Load(SceneCreatureResourceRequest request,CancellationToken token)
        {
            int serial=++calls;var config=configuration.ToArray();
            var task=Task.Run(()=>
            {
                var reads=new HashSet<string>();int thread=System.Environment.CurrentManagedThreadId;
                try
                {
                    using var text=new StringReader(System.Text.Encoding.UTF8.GetString(config));using var overlay=ConfiguredTreOverlay.Open(root,text);
                    TreLookupResult Resolve(string path)
                    {
                        token.ThrowIfCancellationRequested();
                        if(cancel){if(Interlocked.Increment(ref cancelStarted)==2)entered.TrySetResult(true);if(!token.WaitHandle.WaitOne(TimeSpan.FromSeconds(10)))throw new InvalidOperationException("Cancellation timeout.");token.ThrowIfCancellationRequested();}
                        if(!reads.Add(path))throw new InvalidDataException("Duplicate source read.");return overlay.Lookup(path);
                    }
                    var source=SceneCreatureResourceReader.Load(request.Snapshot.Create,request.Catalog,request.Idle,request.Movement,Resolve,token,loadAlbedoFactors:true);
                    if(thread==ownerThread || reads.Count!=39 || source.ExternalReads!=39 || source.SourceBytes!=4686371)throw new InvalidDataException("Original closure/worker differs.");
                    lock(loads)loads.Add(new{serial,id=source.Create.ObjectId,thread,reads=source.ExternalReads,bytes=source.SourceBytes,sourceHash=SourceHash(source)});
                    return source;
                }
                finally{Array.Clear(config);Interlocked.Increment(ref closed);}
            },token);jobs.Add(task);return task;
        }
        world=new(catalog,idle,bank,Load,SceneCreateZeroQuaternionPolicy.Reject,maximumObjects:2,maximumOutstandingJobs:2,maximumStagedMessages:8,maximumEnvelopes:2,maximumEnvelopeBytes:56,maximumPayloadBytes:1024,replicationPolicy:ReplicatedWorldEvidence.Policy(true));
        generation=world.BeginSelection(A).Generation;Stage(world,generation,fixture);
        if(world.Snapshots().Count!=0 || world.OutstandingJobs!=0 || world.StagedCount!=6)throw new InvalidDataException("Explicit two actor barrier differs.");
        if(world.CompleteMinimumSceneBarrier(generation,true)is null)throw new InvalidDataException("Barrier missing.");
        world.PollResources();if(jobs.Count!=2)throw new InvalidDataException("Two resource jobs required.");
        if(cancel)
        {
            var cancelJobs=Task.WhenAll(jobs);if(await Task.WhenAny(entered.Task,cancelJobs)!=entered.Task)throw new InvalidDataException("Cancellation barrier absent.");world.BeginSelection(A);try{await cancelJobs;}catch(OperationCanceledException){}
            var retired=world.PollResources();if(retired.Prepared.Count!=0 || closed!=calls || calls!=2)throw new InvalidDataException("Cancel publication/leak.");
            await Shutdown();Array.Clear(configuration);GD.Print("PASS: two canceled jobs=2 publications=0 archives_closed=true drain_complete=true");GetTree().Quit();return;
        }
        await Task.WhenAll(jobs);var publications=world.PollResources().Prepared.ToDictionary(p=>p.Ticket.Handle.ObjectId);
        if(publications.Count!=2 || calls!=2 || closed!=2)throw new InvalidDataException("Initial publications differ.");
        world.Accept(generation,Convert.FromHexString(fixture["ready"]));
        consumer=new(world,this,2);foreach(var p in publications.Values)if(!consumer.AcceptResource(p))throw new InvalidDataException("Initial publication failed.");
        foreach(ulong id in new[]{A,B}){consumer.Find(id)!.Visual.SeekIdle(0);consumer.Find(id)!.Visual.SetPlaying(false);}
        Consume("initial");var actorA=consumer.Find(A)!;var actorB=consumer.Find(B)!;
        var oldA=world.FindReplication(generation,A)!;var oldB=world.FindReplication(generation,B)!;
        string? aIds=actorA.Visual.Fingerprint(gpu:gpu),bIds=actorB.Visual.Fingerprint(gpu:gpu);int allocations=SceneCreatureVisual.Allocations;
        var aSource=publications[A].Resources;var bSource=publications[B].Resources;
        if(ReferenceEquals(aSource,bSource) || ReferenceEquals(aSource.Creature,bSource.Creature))throw new InvalidDataException("Actor closures aliased.");
        foreach(var p in publications.Values){GD.Print("two_source_begin="+p.Ticket.Handle.ObjectId);ReplicatedWorldEvidence.Write(p.Resources,s=>GD.Print(s));}
        environment=new(){BackgroundMode=Godot.Environment.BGMode.Color,BackgroundColor=new Color("#14202e")};AddChild(new WorldEnvironment{Environment=environment});
        // Fixed frontal view with room for Height 1.4 and both separately placed originals.
        var camera=new Camera3D{Current=true,Fov=40,Position=new Vector3(0,1.2f,9)};AddChild(camera);camera.LookAt(new Vector3(0,1.2f,0));
        var label=new Label{Text="Two distinct synthetic world IDs / original human albedo",Position=new(20,20)};var canvas=new CanvasLayer();AddChild(canvas);canvas.AddChild(label);
        State("initial",gpu);if(capture is not null)await Capture(Captures(capture)[0]);
        // Latest entries coalesce independently; stale ack must preserve both pending IDs.
        Accept(world,generation,fixture,"a_height");var held=world.DrainChanges(generation)!;
        if(!consumer.ApplyBatch(held))throw new InvalidDataException("Held apply failed.");
        Accept(world,generation,fixture,"b_height");if(consumer.AcknowledgeAppliedBatch(held) || world.DrainChanges(generation)!.Entries.Count!=2)throw new InvalidDataException("Stale ack lost pending IDs.");
        Consume("both_height");if(actorA.Apply(oldA) || actorB.Apply(oldB))throw new InvalidDataException("Stale state accepted.");
        Accept(world,generation,fixture,"b_hidden");Consume("b_hidden");if(actorB.Visual.Root.Visible || !actorA.Visual.Root.Visible)throw new InvalidDataException("Per object visibility differs.");
        Accept(world,generation,fixture,"b_shown");Consume("b_shown");
        if(!actorB.Visual.Root.Visible || actorB.Visual.Root.Transform.Basis.X.Length()!=1 || !world.IsCurrent(publications[B]))throw new InvalidDataException("B baseline restore differs.");
        if(actorA.Visual.Fingerprint(gpu:gpu)!=aIds || actorB.Visual.Fingerprint(gpu:gpu)!=bIds || SceneCreatureVisual.Allocations!=allocations || calls!=2)throw new InvalidDataException("State update reloaded/reallocated.");
        // Failed raw Height retains pending work and the prior accepted pose.
        var pose=actorA.Visual.Root.Transform;Accept(world,generation,fixture,"a_invalid_height");if(consumer.ConsumePending() || consumer.RejectedHeightBits!=0x80000000 || actorA.Visual.Root.Transform!=pose || world.DrainChanges(generation)!.Entries.Count!=1)throw new InvalidDataException("Invalid Height cleared pending/pose.");
        Accept(world,generation,fixture,"a_height");Consume("height_retry");
        using(var foreign=new SceneWorldCoordinator(catalog,idle,bank,(_,_)=>Task.FromResult(aSource),SceneCreateZeroQuaternionPolicy.Reject,replicationPolicy:ReplicatedWorldEvidence.Policy(true)))
        {
            long fg=foreign.BeginSelection(A).Generation;foreach(string key in new[]{"start","a_create","a_baseline","a_end"})Accept(foreign,fg,fixture,key);foreign.CompleteMinimumSceneBarrier(fg,true);foreign.PollResources();var fp=foreign.PollResources().Prepared.Single();
            if(consumer.AcceptResource(fp) || actorA.Apply(foreign.FindReplication(fg,A)!) || consumer.ApplyBatch(foreign.DrainChanges(fg)!))throw new InvalidDataException("Foreign world accepted.");
        }
        var acknowledged=world.DrainChanges(generation)!;if(!consumer.ApplyBatch(acknowledged))throw new InvalidDataException("Current empty apply failed.");
        foreach(string method in new[]{"consume","apply","ack"})
        {
            bool rejected=await Task.Run(()=>{try{if(method=="consume")consumer.ConsumePending();else if(method=="apply")consumer.ApplyBatch(acknowledged);else consumer.AcknowledgeAppliedBatch(acknowledged);return false;}catch(InvalidOperationException){return true;}});
            if(!rejected)throw new InvalidDataException("Wrong thread accepted.");
        }
        if(!consumer.AcknowledgeAppliedBatch(acknowledged))throw new InvalidDataException("Wrong thread changed ack metadata.");
        State("updated",gpu);if(capture is not null)await Capture(Captures(capture)[1]);
        var beforeRecreate=world.FindReplication(generation,A)!;Accept(world,generation,fixture,"a_destroy");Consume("destroy");
        if(consumer.Find(A)is not null || consumer.Count!=1 || !actorA.Visual.IsRetired || actorB.Visual.Fingerprint(gpu:gpu)!=bIds || !world.IsCurrent(publications[B]))throw new InvalidDataException("One destroy retired unaffected actor.");
        foreach(string key in new[]{"a_create","a_baseline","a_end","a_height"})Accept(world,generation,fixture,key);
        world.PollResources();if(calls!=3 || jobs.Count!=3)throw new InvalidDataException("Recreation must load only A.");await jobs[^1];var replacement=world.PollResources().Prepared.Single();
        if(consumer.AcceptResource(publications[A]) || world.IsCurrent(beforeRecreate) || world.IsCurrent(publications[A]) || replacement.Ticket.Handle.ObjectId!=A || !consumer.AcceptResource(replacement))throw new InvalidDataException("Recreated ownership differs.");
        Consume("recreate");var newA=consumer.Find(A)!;newA.Visual.SeekIdle(0);newA.Visual.SetPlaying(false);
        if(newA.Apply(beforeRecreate) || newA.Visual.Fingerprint(gpu:gpu)==aIds || actorB.Visual.Fingerprint(gpu:gpu)!=bIds || SceneCreatureVisual.Allocations!=allocations+1 || consumer.Count!=2 || calls!=3 || closed!=3)throw new InvalidDataException("Recreation altered B/currentness/allocations.");
        State("recreated",gpu);if(capture is not null)await Capture(Captures(capture)[2]);
        using(var limited=new SceneWorldPresentationConsumer(world,this,1))
        {
            if(!limited.AcceptResource(replacement))throw new InvalidDataException("Capacity control actor absent.");int allocated=SceneCreatureVisual.Allocations;bool rejected=false;
            try{limited.AcceptResource(publications[B]);}catch(InvalidDataException){rejected=true;}
            if(!rejected || limited.Count!=1 || SceneCreatureVisual.Allocations!=allocated || !world.IsCurrent(publications[B]))throw new InvalidDataException("Capacity failure allocated/altered B.");
        }
        bool failed=false;try{Accept(world,generation,fixture,"a_bad");}catch(InvalidDataException){failed=true;}
        if(!failed || consumer.ConsumePending() || consumer.Count!=0 || world.State!=ZoneSceneTransitionState.Failed || world.IsCurrent(publications[B]))throw new InvalidDataException("Two-actor failure did not retire globally.");
        GD.Print("PASS: two capacity rejected_before_allocation=true limit=1 global_failure_retired=2");
        GD.Print("two_loads="+JsonSerializer.Serialize(loads));GD.Print("PASS: two lifecycle coalesced=true apply_before_ack=true stale_ack_two_pending=true per_object_height_visibility=true stale_foreign_rejected=true wrong_thread=3 invalid_height_pending=true recreated_only_a=true unchanged_b_ids=true reads=39_per_load calls=3");
        await Shutdown();Array.Clear(configuration);GD.Print("PASS: two teardown roots_released=true archives_closed=true drain_complete=true outstanding=0");GetTree().Quit();
    }
    private static string SourceHash(SceneCreatureResources source)=>Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(source.Creature.Sources.OrderBy(p=>p.Key,StringComparer.Ordinal).Select(p=>p.Value))));
    private void Consume(string stage)
    {var batch=world!.DrainChanges(generation)!;GD.Print("two_batch="+JsonSerializer.Serialize(Batch(stage,batch)));if(!consumer!.ApplyBatch(batch) || !consumer.AcknowledgeAppliedBatch(batch) || world.DrainChanges(generation)!.Entries.Count!=0)throw new InvalidDataException("Current apply/ack failed.");}
    private void State(string stage,bool gpu)
    {
        var actors=new[]{A,B}.Select(id=>
        {var a=consumer!.Find(id)!;var s=world!.FindReplication(generation,id)!;if(!a.Verify(gpu))throw new InvalidDataException("Actor verification failed.");return new{id,incarnation=s.Incarnation.CreationRevision,revision=s.Revision,heightBits=BitConverter.SingleToUInt32Bits(s.Scalars.Height),visible=a.Visual.Root.Visible,transform=CreatureStatePreview.PoseEvidence(a.Visual.Root.Transform),ids=a.Visual.Fingerprint(gpu:gpu),phase=world.Snapshots().Single(p=>p.Object.Handle.ObjectId==id).Object.Phase.ToString()};}).ToArray();
        GD.Print("two_state="+JsonSerializer.Serialize(new{stage,actors,calls,closed,allocations=SceneCreatureVisual.Allocations,disposals=SceneCreatureVisual.Disposals}));
    }
    public static string[] Captures(string first)=>new[]{first,Path.Combine(Path.GetDirectoryName(first)!,Path.GetFileNameWithoutExtension(first)+"-updated.png"),Path.Combine(Path.GetDirectoryName(first)!,Path.GetFileNameWithoutExtension(first)+"-recreated.png")};
    private async Task Capture(string path)
    {await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);using var image=GetViewport().GetTexture().GetImage();if(image.SavePng(path)!=Error.Ok)throw new IOException("Capture failed.");GD.Print("two_capture="+Path.GetFileName(path));}
    private async Task Shutdown()
    {if(disposed)return;disposed=true;consumer?.Dispose();consumer=null;world?.Close();if(world is not null){await world.DrainCompletion;if(world.OutstandingJobs!=0 || jobs.Any(t=>!t.IsCompleted) || SceneCreatureVisual.Disposals!=SceneCreatureVisual.Allocations)throw new InvalidDataException("Drain incomplete.");}jobs.Clear();loads.Clear();fixture.Clear();environment?.Dispose();environment=null;}
    public override void _Notification(int what){if(what==NotificationWMCloseRequest){GD.PushError("Bounded two creature preview closed early.");GetTree().Quit(1);}}
    public override void _ExitTree(){consumer?.Dispose();consumer=null;world?.Close();environment?.Dispose();environment=null;}
}
