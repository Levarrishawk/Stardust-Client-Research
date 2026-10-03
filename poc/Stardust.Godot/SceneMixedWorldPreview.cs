using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

/// <summary>Bounded original human/terminal diagnostic over one accepted mixed world.</summary>
public partial class SceneMixedWorldPreview : Node
{
    private ulong Human, Terminal;
    private readonly TaskCompletionSource<bool> completion=new(TaskCreationOptions.RunContinuationsAsynchronously);
    public Task Completion=>completion.Task;
    private SceneWorldCoordinator? world;
    private SceneWorldObjectPresentationConsumer? consumer;
    private SceneCreatureVisual? human;
    private SceneStaticVisual? terminal;
    private Godot.Environment? environment;
    private readonly List<Task<SceneObjectResources>> jobs=new();
    private readonly TaskCompletionSource<bool> entered=new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int calls,closed,cancelEntered;
    private bool disposed;
    private long generation;
    private Dictionary<string,string> peer=new();
    private readonly int owner=System.Environment.CurrentManagedThreadId;
    public override async void _Ready()
    {
        try { await Run(); completion.TrySetResult(true); }
        catch(Exception error) { GD.PushError("Mixed world failed: "+error); try{await Shutdown();}catch(Exception drain){GD.PushError("Mixed world drain failed: "+drain);}completion.TrySetException(error);GetTree().Quit(1); }
    }
    private async Task Run()
    {
        GetTree().AutoAcceptQuit=false;
        string[] args=OS.GetCmdlineUserArgs();
        string Arg(string key) { int i=Array.IndexOf(args,key); if(i<0 || i+1>=args.Length || args.Count(a=>a==key)!=1)throw new ArgumentException(key+" required once");return args[i+1]; }
        string root=Arg("--asset-root"),configPath=Arg("--asset-config");
        peer=JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllBytes(Arg("--mixed-peer")))??throw new InvalidDataException("Peer required");
        if(peer.Count!=14 || peer.Values.Any(v=>v.Length>4096))throw new InvalidDataException("Bounded mixed fixture required");
        ulong ObjectId(string key)
        {
            byte[] bytes=Convert.FromHexString(peer[key]);
            try { if(bytes.Length!=47)throw new InvalidDataException("Create fixture size");return System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(6,8)); }
            finally { Array.Clear(bytes); }
        }
        Human=ObjectId("human_create");Terminal=ObjectId("terminal_create");
        if(Human==Terminal)throw new InvalidDataException("Distinct fixture identities required");
        bool useConsumer=args.Contains("--object-consumer");
        bool gpu=DisplayServer.GetName()!="headless",cancel=args.Contains("--mixed-cancel");
        string? capture=args.Contains("--mixed-capture")?Arg("--mixed-capture"):null;
        if(capture is not null)
        {
            string prefix=Path.GetFullPath(Path.Combine(ProjectSettings.GlobalizePath("res://"),"..","..","local-private"))+Path.DirectorySeparatorChar;
            if(!gpu || cancel)throw new ArgumentException("Capture requires separate GPU run");
            foreach(string path in Captures(capture))if(!Path.GetFullPath(path).StartsWith(prefix,StringComparison.OrdinalIgnoreCase) || Path.GetExtension(path)!=".png" || File.Exists(path) || !Directory.Exists(Path.GetDirectoryName(Path.GetFullPath(path))))throw new ArgumentException("Fresh private PNG triple required");
        }
        byte[] cfg=File.ReadAllBytes(configPath);
        if(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(cfg))!=RetainedSkeletalDetailBundleReader.ConfigurationPin)throw new InvalidDataException("Configuration pin");
        var catalog=ObjectTemplateCatalogProfile.Decode(File.ReadAllBytes(Arg("--catalog-profile")));
        using var bankFile=File.OpenRead(ProjectSettings.GlobalizePath("res://../preview-profiles/human-locomotion.json"));
        var bank=SkeletalAnimationBankProfile.Load(bankFile);
        var idle=new LogicalAnimationClipSelection("appearance/lat/all_m.lat","emt_standing_idle_ambient_1_ag","798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B");
        var options=new Dictionary<SceneObjectResourceRecipe,SceneObjectResourceOptions>{
            [SceneObjectResourceRecipe.CreatureDetail0]=new SceneCreatureResourceOptions(idle,bank,true),
            [SceneObjectResourceRecipe.StaticAppearance]=new SceneStaticResourceOptions(SceneStaticMaterialPolicy.RequireComplete,new("shader/anim_mission_terminal.sht","B5AA5A251A76AD8F152DB85F50FBC3061D0494847499F1068A9B55C375785A62",0))};
        Task<SceneObjectResources> Load(SceneObjectResourceRequest request,CancellationToken token)
        {
            calls++;var copy=cfg.ToArray();
            var job=Task.Run(()=>
            {
                try
                {
                    using var text=new StringReader(Encoding.UTF8.GetString(copy));using var overlay=ConfiguredTreOverlay.Open(root,text);
                    var seen=new HashSet<string>();
                    TreLookupResult Resolve(string path)
                    {
                        token.ThrowIfCancellationRequested();
                        if(cancel){if(Interlocked.Increment(ref cancelEntered)==2)entered.TrySetResult(true);if(!token.WaitHandle.WaitOne(TimeSpan.FromSeconds(10)))throw new InvalidDataException("Cancellation deadline");token.ThrowIfCancellationRequested();}
                        if(!seen.Add(path))throw new InvalidDataException("Duplicate lookup");return overlay.Lookup(path);
                    }
                    var result=SceneObjectResourceRouter.Load(request.Snapshot.Create,request.Catalog,request.Options,Resolve,token);
                    if(System.Environment.CurrentManagedThreadId==owner || seen.Count!=result.ExternalReads)throw new InvalidDataException("Dedicated owned source job");
                    return result;
                }
                finally{Array.Clear(copy);Interlocked.Increment(ref closed);}
            },token);jobs.Add(job);return job;
        }
        var policy=new SceneWorldReplicationPolicy(SceneWorldReplicationMode.CreatureView3Scalars,new(1024,1024,1024,128,16384),16,16384);
        world=new(catalog,options,Load,SceneCreateZeroQuaternionPolicy.Reject,maximumObjects:2,maximumOutstandingJobs:2,maximumStagedMessages:8,maximumPayloadBytes:1024,maximumEnvelopes:4,maximumEnvelopeBytes:1024,replicationPolicy:policy);
        generation=world.BeginSelection(Human).Generation;Accept("start");
        foreach(string p in new[]{"human","terminal"})foreach(string suffix in new[]{"create","baseline","end"})Accept(p+"_"+suffix);
        Check(world.Snapshots().Count==0 && calls==0,"staged publication");
        Check(world.CompleteMinimumSceneBarrier(generation,true)is not null,"explicit barrier");world.PollObjectResources();
        Check(calls==2,"both recipe jobs");
        if(cancel)
        {
            var all=Task.WhenAll(jobs);Check(await Task.WhenAny(entered.Task,all)==entered.Task,"both archives enter cancellation");
            world.BeginSelection(Human);try{await all;}catch(OperationCanceledException){}
            Check(world.PollObjectResources().Prepared.Count==0 && closed==2,"no canceled publication");
            await Shutdown();Array.Clear(cfg);GD.Print("PASS: mixed canceled jobs=2 publications=0 archives_closed=true drain_complete=true");GetTree().Quit();return;
        }
        await Task.WhenAll(jobs);
        var publications=world.PollObjectResources().Prepared.ToDictionary(p=>p.Ticket.Handle.ObjectId);
        Check(publications.Count==2 && closed==2,"two archive-closed publications");Accept("ready");
        var hp=publications[Human];var tp=publications[Terminal];
        Check(hp.Resources.Kind==SceneObjectResourceRecipe.CreatureDetail0 && hp.Resources.ExternalReads==39 && hp.Resources.SourceBytes==4686371 &&
            tp.Resources.Kind==SceneObjectResourceRecipe.StaticAppearance && tp.Resources.ExternalReads==27 && tp.Resources.SourceBytes==1401794,"exact original recipe closures");
        var baseHuman=GodotAppearanceTransform.ToGodot(hp.Placement.Transform);
        if(useConsumer)
        {
            var factory=new SceneWorldObjectVisualFactory(world,this,terminalFrame0Inspection:true);
            consumer=new(world,factory.Create,maximumActors:2);
            foreach(var publication in publications.Values)Check(consumer.AcceptResource(publication),"generic resource transfer");
            Check(consumer.Count==2 && consumer.AcceptResource(hp),"bounded idempotent transfer");
            human=((SceneWorldObjectVisual)consumer.Find(Human)!).CreatureVisual??throw new InvalidDataException("Creature adapter");
            terminal=((SceneWorldObjectVisual)consumer.Find(Terminal)!).StaticVisual??throw new InvalidDataException("Static adapter");
        }
        else
        {
            human=SceneCreatureVisual.TryPublishCurrent(hp.Resources.Creature!,baseHuman,this,()=>world.IsCurrent(hp))??throw new InvalidDataException("Human transfer");
            terminal=SceneStaticVisual.TryPublish(tp.Resources.Static!,GodotAppearanceTransform.ToGodot(tp.Placement.Transform),this,()=>world.IsCurrent(tp),terminalFrame0Inspection:true)??throw new InvalidDataException("Terminal transfer");
        }
        human.SeekIdle(0);human.SetPlaying(false);
        Check(human.Verify(gpu) && terminal.Verify(gpu),"owned GPU boundaries");
        string humanIds=human.Fingerprint(gpu:gpu)!,terminalIds=terminal.Fingerprint(gpu)!;
        var originalHumanInc=world.Snapshots().Single(s=>s.Object.Handle.ObjectId==Human).Object.Incarnation;
        environment=new(){BackgroundMode=Godot.Environment.BGMode.Color,BackgroundColor=new Color("#14202e")};AddChild(new WorldEnvironment{Environment=environment});
        var camera=new Camera3D{Current=true,Fov=40,Position=new Vector3(0,1.3f,-9)};AddChild(camera);camera.LookAt(new Vector3(0,1.3f,0));
        var canvas=new CanvasLayer();AddChild(canvas);canvas.AddChild(new Label{Position=new Vector2(20,20),Text="One mixed world / original human + terminal / frame0 inspection; opaque TANO"});
        var held=world.DrainChanges(generation)!;
        Check(useConsumer?consumer!.ApplyBatch(held) && consumer.AcknowledgeAppliedBatch(held):world.AcknowledgeChanges(held),"initial acknowledgment");
        State("initial");if(capture is not null)await Capture(Captures(capture)[0]);
        Accept("human_height");var stale=world.DrainChanges(generation)!;
        if(useConsumer)Check(consumer!.ApplyBatch(stale),"apply held batch before newer mutation");
        Accept("terminal_delta");Check(useConsumer?!consumer!.AcknowledgeAppliedBatch(stale):!world.AcknowledgeChanges(stale),"stale batch preserves both IDs");
        var latest=world.DrainChanges(generation)!;var state=world.FindReplication(generation,Human)!;
        if(useConsumer)Check(latest.Entries.Count==2 && consumer!.ConsumePending(),"generic current batch retry");
        else
        {
            Check(world.IsCurrent(state) && human.SetEngineTransform(SceneCreatureStatePresentation.DeriveTransform(baseHuman,state.Scalars.Height)) && world.IsCurrent(state),"typed Height application");
            Check(latest.Entries.Count==2 && world.AcknowledgeChanges(latest),"current mixed batch acknowledgment");
        }
        Check(world.IsCurrent(hp) && world.IsCurrent(tp) && human.Fingerprint(gpu:gpu)==humanIds && terminal.Fingerprint(gpu)==terminalIds && calls==2,"state has no resource reload");
        State("updated");if(capture is not null)await Capture(Captures(capture)[1]);
        Accept("terminal_destroy");
        if(useConsumer)Check(consumer!.Tick(0) && consumer.Count==1 && consumer.Find(Terminal) is null,"consumer removes destroyed actor");
        Check(!terminal.Tick() && terminal.IsRetired && !world.IsCurrent(tp) && human.Fingerprint(gpu:gpu)==humanIds,"only terminal retires");
        Accept("terminal_create");Accept("terminal_end");world.PollObjectResources();Check(calls==3,"one static reload");await jobs[^1];
        var replacement=world.PollObjectResources().Prepared.Single();Check(replacement.Ticket.Handle.ObjectId==Terminal && world.IsCurrent(replacement),"static replacement");
        if(useConsumer)
        {
            Check(!consumer!.AcceptResource(tp) && consumer.AcceptResource(replacement) && consumer.Count==2,"consumer rejects stale and accepts replacement");
            terminal=((SceneWorldObjectVisual)consumer.Find(Terminal)!).StaticVisual??throw new InvalidDataException("Replacement adapter");
        }
        else terminal=SceneStaticVisual.TryPublish(replacement.Resources.Static!,GodotAppearanceTransform.ToGodot(replacement.Placement.Transform),this,()=>world.IsCurrent(replacement),terminalFrame0Inspection:true)??throw new InvalidDataException("Replacement transfer");
        Check(terminal.Verify(gpu) && terminal.Fingerprint(gpu)!=terminalIds && human.Fingerprint(gpu:gpu)==humanIds &&
            world.Snapshots().Single(s=>s.Object.Handle.ObjectId==Human).Object.Incarnation==originalHumanInc,"unaffected human and new static GPU IDs");
        var recreated=world.DrainChanges(generation)!;
        Check(useConsumer?consumer!.ApplyBatch(recreated) && consumer.AcknowledgeAppliedBatch(recreated):world.AcknowledgeChanges(recreated),"recreated acknowledgment");
        State("recreated");if(capture is not null)await Capture(Captures(capture)[2]);
        bool failed=false;try{Accept("terminal_bad_family");}catch(InvalidDataException){failed=true;}
        if(useConsumer)Check(!consumer!.Tick(0) && consumer.Count==0,"consumer retires failed generation");
        Check(failed && world.State==ZoneSceneTransitionState.Failed && !human.Tick(0) && !terminal.Tick() && world.DrainChanges(generation)is null,"family disagreement retires both");
        GD.Print("PASS: mixed lifecycle typed_creature=true opaque_terminal=true stale_ack_retained=true static_only_reload=true human_ids_unchanged=true family_failure_retired=2 calls=3");
        if(useConsumer)GD.Print("PASS: generic consumer dynamic_ids=true actors=2 idempotent=true applied_stale_ack_retried=true stale_resource_rejected=true failed_count=0");
        await Shutdown();Array.Clear(cfg);GD.Print("PASS: mixed teardown roots_released=true archives_closed=true drain_complete=true outstanding=0");GetTree().Quit();
    }
    private void Accept(string key){byte[] bytes=Convert.FromHexString(peer[key]);try{Check(world!.Accept(generation,bytes),"current application");}finally{Array.Clear(bytes);}}
    private void State(string stage)
    {
        var h=world!.FindReplication(generation,Human)!;var t=world.Snapshots().Single(s=>s.Object.Handle.ObjectId==Terminal);
        GD.Print("mixed_state="+JsonSerializer.Serialize(new{stage,calls,closed,heightBits=BitConverter.SingleToUInt32Bits(h.Scalars.Height),humanIncarnation=h.Incarnation.CreationRevision,terminalIncarnation=t.Object.Incarnation.CreationRevision,opaque=t.Envelopes.Count,humanIds=human!.Fingerprint(),terminalIds=terminal!.Fingerprint(),humanPose=CreatureStatePreview.PoseEvidence(human.Root.Transform),terminalPose=CreatureStatePreview.PoseEvidence(terminal.Root.Transform)}));
    }
    public static string[] Captures(string first)=>new[]{first,Path.Combine(Path.GetDirectoryName(first)!,Path.GetFileNameWithoutExtension(first)+"-updated.png"),Path.Combine(Path.GetDirectoryName(first)!,Path.GetFileNameWithoutExtension(first)+"-recreated.png")};
    private async Task Capture(string path){await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);using var image=GetViewport().GetTexture().GetImage();if(image.SavePng(path)!=Error.Ok)throw new IOException("Mixed capture");}
    private async Task Shutdown(){if(disposed)return;disposed=true;consumer?.Dispose();human?.Dispose();terminal?.Dispose();world?.Close();if(world is not null){await world.DrainCompletion;Check(world.OutstandingJobs==0 && closed==calls && jobs.All(j=>j.IsCompleted),"mixed drain");}jobs.Clear();peer.Clear();environment?.Dispose();environment=null;}
    private static void Check(bool value,string reason){if(!value)throw new InvalidDataException(reason);}
    private async Task CloseEarly(){try{await Shutdown();}finally{GetTree().Quit(1);}}
    public override void _Notification(int what){if(what==NotificationWMCloseRequest && !disposed)_=CloseEarly();}
    public override void _ExitTree(){consumer?.Dispose();human?.Dispose();terminal?.Dispose();world?.Close();environment?.Dispose();environment=null;}
}
