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

/// <summary>Offline original-asset proof with supplied immutable values and controlled CPU workers.</summary>
public partial class SceneWorldCreatureCustomizationPreview : Node
{
    private const ulong Human=0xF122334455667788;
    private SceneWorldCoordinator? world;
    private SceneWorldCreatureCustomizationVisual? actor;
    private SceneWorldObjectNameVisual? nameActor;
    private Godot.Environment? environment;
    private readonly List<Task> jobs=new();
    private readonly ManualResetEventSlim release=new(false);
    private TaskCompletionSource<bool> entered=new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly ValuesSource source=new();
    private readonly MotionSource motion=new();
    private int mode,compositions,canceledTokens,archivesClosed,active,peakActive;
    private long generation;
    private Dictionary<string,string> peer=new();
    private readonly Dictionary<string,byte[]> raw=new();
    private bool closed;
    public override async void _Ready()
    {
        try{await Run();GD.Print("PASS: world customization complete");GetTree().Quit();}
        catch(Exception error){GD.PushError("World customization failed: "+error);try{await Shutdown();}catch(Exception drain){GD.PushError(drain.ToString());}GetTree().Quit(1);}
    }
    private async Task Run()
    {
        var args=OS.GetCmdlineUserArgs();string Arg(string key){int i=Array.IndexOf(args,key);if(i<0||i+1>=args.Length)throw new ArgumentException(key);return args[i+1];}
        string root=Arg("--asset-root"),cfgPath=Arg("--asset-config"),export=Arg("--customization-export");
        peer=JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllBytes(Arg("--mixed-peer")))!;
        bool gpu=DisplayServer.GetName()!="headless";string? capture=args.Contains("--customization-capture")?Arg("--customization-capture"):null;
        bool heightRace=args.Contains("--customization-height-race"),containerRace=args.Contains("--customization-container-race"),visibilityRace=args.Contains("--customization-visibility-race"),nameRace=args.Contains("--customization-name-race");
        Check(new[]{heightRace,containerRace,visibilityRace,nameRace}.Count(v=>v)<=1,"separate race modes");bool bodyRace=heightRace||containerRace,race=bodyRace||visibilityRace||nameRace;
        string retirement=race&&args.Contains("--customization-race-retire")?Arg("--customization-race-retire"):"generation";
        Check(retirement is "generation" or "destroy","bounded race retirement mode");
        if(!race&&args.Contains("--customization-race-retire"))throw new ArgumentException("Retirement mode requires explicit race mode.");
        string prefix=Path.GetFullPath(ProjectSettings.GlobalizePath("res://../../local-private"))+Path.DirectorySeparatorChar;
        void Fresh(string path,string ext){Check(Path.GetFullPath(path).StartsWith(prefix,StringComparison.OrdinalIgnoreCase)&&Path.GetExtension(path)==ext&&!File.Exists(path)&&Directory.Exists(Path.GetDirectoryName(Path.GetFullPath(path))),"fresh private output");}
        Fresh(export,".json");if(capture is not null){Check(gpu,"GPU capture mode");foreach(string path in Captures(capture))Fresh(path,".png");}
        if(capture is not null&&bodyRace)foreach(string path in RaceCaptures(capture))Fresh(path,".png");
        if(capture is not null&&visibilityRace)foreach(string path in VisibilityCaptures(capture))Fresh(path,".png");
        if(capture is not null&&nameRace)foreach(string path in NameCaptures(capture))Fresh(path,".png");
        byte[] cfg=File.ReadAllBytes(cfgPath);Check(Convert.ToHexString(SHA256.HashData(cfg))==RetainedSkeletalDetailBundleReader.ConfigurationPin,"configuration identity");
        var catalog=ObjectTemplateCatalogProfile.Decode(File.ReadAllBytes(ProjectSettings.GlobalizePath("res://../preview-profiles/scene-object-catalog.json")));
        using var bankFile=File.OpenRead(ProjectSettings.GlobalizePath("res://../preview-profiles/human-locomotion.json"));var bank=SkeletalAnimationBankProfile.Load(bankFile);
        using var faceFile=File.OpenRead(ProjectSettings.GlobalizePath("res://../preview-profiles/human-face-zero.json"));var face=new SceneCreatureFaceInspectionPolicy(BlueprintFaceCustomizationProfile.Load(faceFile));
        var idle=new LogicalAnimationClipSelection("appearance/lat/all_m.lat","emt_standing_idle_ambient_1_ag","798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B");
        var options=new Dictionary<SceneObjectResourceRecipe,SceneObjectResourceOptions>{[SceneObjectResourceRecipe.CreatureDetail0]=new SceneCreatureResourceOptions(idle,bank,true,128,32*1024*1024,face),
            [SceneObjectResourceRecipe.StaticAppearance]=new SceneStaticResourceOptions(SceneStaticMaterialPolicy.RequireComplete,new("shader/anim_mission_terminal.sht","B5AA5A251A76AD8F152DB85F50FBC3061D0494847499F1068A9B55C375785A62",0))};
        Task<SceneObjectResources> Load(SceneObjectResourceRequest request,CancellationToken token)
        {
            var copy=cfg.ToArray();var job=Task.Run(()=>
            {
                try{using var text=new StringReader(Encoding.UTF8.GetString(copy));using var overlay=ConfiguredTreOverlay.Open(root,text);
                    TreLookupResult Resolve(string path){token.ThrowIfCancellationRequested();var result=overlay.Lookup(path);if(result.Payload is not null)raw.Add(path,result.Payload.ToArray());return result;}
                    return SceneObjectResourceRouter.Load(request.Snapshot.Create,request.Catalog,request.Options,Resolve,token);
                }finally{Array.Clear(copy);Interlocked.Increment(ref archivesClosed);}
            },token);jobs.Add(job);return job;
        }
        var policy=containerRace
            ?new SceneWorldReplicationPolicy(SceneWorldReplicationMode.CreatureView3MixedComponents,new(1024,1024,1024,128,16384),16,16384,
                new(16384,32,16,32,128,128),CreatureWoundsApplicationPolicy.StrictInterval,new(16384,32,32,128,128),new(128,128))
            :new SceneWorldReplicationPolicy(SceneWorldReplicationMode.CreatureView3Scalars,new(1024,1024,1024,128,16384),16,16384);
        world=new(catalog,options,Load,SceneCreateZeroQuaternionPolicy.Reject,replicationPolicy:policy);
        generation=world.BeginSelection(Human).Generation;Accept("start");foreach(string suffix in new[]{"create","baseline","end"})Accept(visibilityRace&&suffix=="baseline"?"visibility_hidden_baseline":"human_"+suffix);
        Check(world.CompleteMinimumSceneBarrier(generation,true)is not null,"scene barrier");world.PollObjectResources();await Task.WhenAll(jobs);Array.Clear(cfg);
        var publication=world.PollObjectResources().Prepared.Single();Accept("ready");
        SceneWorldObjectSnapshot Snapshot()=>world.Snapshots().Single(s=>s.Object.Handle.ObjectId==Human);
        if(containerRace)
        {
            // Reuse immutable managed descriptors; no additional archive/GPU resources are created.
            foreach(var guardMode in new[]{SceneWorldReplicationMode.CreatureView3Scalars,SceneWorldReplicationMode.CreatureView3Mixed,SceneWorldReplicationMode.CreatureView3MixedComponents,SceneWorldReplicationMode.Opaque})
            {
                var guardPolicy=guardMode==SceneWorldReplicationMode.Opaque?null:policy with{Mode=guardMode};
                using var guardWorld=new SceneWorldCoordinator(catalog,options,(_,_)=>Task.FromResult(publication.Resources),SceneCreateZeroQuaternionPolicy.Reject,replicationPolicy:guardPolicy);
                long guardGeneration=guardWorld.BeginSelection(Human).Generation;
                foreach(string key in new[]{"start","human_create","human_baseline","human_end"})Check(guardWorld.Accept(guardGeneration,Convert.FromHexString(peer[key])),"guard scene fixture");
                Check(guardWorld.CompleteMinimumSceneBarrier(guardGeneration,true)is not null,"guard scene barrier");guardWorld.PollObjectResources();
                var guardPublication=guardWorld.PollObjectResources().Prepared.Single();var guardSnapshot=guardWorld.Snapshots().Single();
                Check(SceneWorldObjectVisual.Matches(guardWorld,guardPublication,guardSnapshot)==(guardMode!=SceneWorldReplicationMode.Opaque),"explicit typed modes/opaque exclusion");
                Check(!SceneWorldObjectVisual.Matches(guardWorld,publication,guardSnapshot)&&!SceneWorldObjectVisual.Matches(guardWorld,guardPublication,guardSnapshot with{Replication=null}),"foreign publication and missing typed state rejected");
                Check(guardWorld.Accept(guardGeneration,Convert.FromHexString(peer["human_height"])),"guard body update");
                Check(!SceneWorldObjectVisual.Matches(guardWorld,guardPublication,guardSnapshot),"stale snapshot rejected");
                guardWorld.BeginSelection(Human);Check(!SceneWorldObjectVisual.Matches(guardWorld,guardPublication,guardWorld.Snapshots().FirstOrDefault()??guardSnapshot),"old generation/publication rejected");
                guardWorld.Close();await guardWorld.DrainCompletion;
            }
            GD.Print("PASS: creature eligibility three_typed_modes=true opaque_rejected=true missing_state_foreign_stale_generation_rejected=true");
        }
        var basis=publication.Resources.Creature!.Creature;Check(publication.Resources.ExternalReads==76&&publication.Resources.SourceBytes==5940390&&archivesClosed==1,"bounded original closure");
        IReadOnlyDictionary<ShaderCustomizationKey,int> Profile(string name){using var file=File.OpenRead(ProjectSettings.GlobalizePath("res://../preview-profiles/human-customization-"+name+".json"));return SkeletalAppearanceCustomization.LoadProfile(file);}
        var zero=Profile("zero");var varied=Profile("varied");Check(SkeletalAppearanceCustomization.DescribeControls(basis).Count==8,"full eight-key union");
        var plain=(SceneWorldObjectVisual)new SceneWorldObjectVisualFactory(world,this).Create(publication,Snapshot())!;
        var moving=new SceneWorldObjectMotionVisual(world,plain,motion);var locomotion=new SceneWorldLocomotionVisual(world,moving,motion);
        using(var foreign=new SceneWorldCoordinator(catalog,options,Load,SceneCreateZeroQuaternionPolicy.Reject))
        {bool reject=false;try{_=new SceneWorldCreatureCustomizationVisual(foreign,locomotion,source);}catch(ArgumentException){reject=true;}Check(reject&&!locomotion.IsRetired,"failed constructor retains caller ownership");}
        Check(await Task.Run(()=>{try{_=new SceneWorldCreatureCustomizationVisual(world,locomotion,source);return false;}catch(InvalidOperationException){return true;}}),"worker constructor rejected");
        CreatureAppearanceDescriptorBundle Compose(CreatureAppearanceDescriptorBundle original,IReadOnlyDictionary<ShaderCustomizationKey,int> values,CancellationToken token)
        {
            Interlocked.Increment(ref compositions);int concurrent=Interlocked.Increment(ref active);peakActive=Math.Max(peakActive,concurrent);
            try
            {
            int selected=Interlocked.Exchange(ref mode,0);
            if(selected==1){entered.TrySetResult(true);if(!release.Wait(TimeSpan.FromSeconds(8)))throw new InvalidDataException("Blocked composer deadline");if(token.IsCancellationRequested)Interlocked.Increment(ref canceledTokens);}
            if(selected==2)throw new InvalidDataException("Supplied composer failure");
            var result=SkeletalAppearanceCustomization.Apply(original,values,selected==1?CancellationToken.None:token);
            if(selected==3)return result with{Sources=result.Sources.ToDictionary(p=>p.Key,p=>p.Value)};
            if(selected==4)
            {
                var outputs=result.GeneratedFaces!.Outputs.ToDictionary(p=>p.Key,p=>p.Value);var first=outputs.First();var image=first.Value.Image;
                outputs[first.Key]=first.Value with{Image=image with{Mips=new[]{image.Mips[0] with{Rgba=new byte[4]}}}};
                return result with{GeneratedFaces=result.GeneratedFaces with{Outputs=outputs}};
            }
            return result;
            }finally{Interlocked.Decrement(ref active);}
        }
        actor=new(world,locomotion,source,Compose);var visual=plain.CreatureVisual!;locomotion.SetPlaying(false);
        int nameResolves=0;
        if(nameRace){NameGuards(world,actor,plain);nameActor=new(world,actor,plain,n=>{nameResolves++;return DiagnosticName(n);});}
        bool Tick(double seconds)=>nameActor?.Tick(seconds)??actor.Tick(seconds);
        bool Apply(SceneWorldObjectSnapshot snapshot)=>nameActor?.Apply(snapshot)??actor.Apply(snapshot);

        bool createdHidden=visibilityRace&&!visual.Root.Visible;if(visibilityRace)Check(createdHidden,"factory creates current hidden visibility without a visible publication");
        string initial=Identity(visual,false);Check(Tick(0)&&actor.LastAppliedRevision==0&&Identity(visual,false)==initial,"missing initial values preserve base");
        environment=new(){BackgroundMode=Godot.Environment.BGMode.Color,BackgroundColor=new Color("#14202e")};AddChild(new WorldEnvironment{Environment=environment});
        var camera=new Camera3D{Current=true,Fov=race?40:32,Position=race?new Vector3(-1.5f,1.3f,-6):new Vector3(-1.5f,1.05f,-4.2f)};AddChild(camera);camera.LookAt(new Vector3(-1.5f,race?1.3f:1.05f,0));
        void Set(ulong revision,IReadOnlyDictionary<ShaderCustomizationKey,int> values){source.Value=new(Snapshot().Object.Incarnation,revision,values);Check(Tick(0),"alive submit");}
        async Task Applied(ulong revision)
        {
            for(int i=0;i<500&&actor.LastAppliedRevision!=revision;i++){await Task.Delay(10);Check(Tick(0),"alive poll");}
            Check(actor.LastAppliedRevision==revision&&actor.LastFailure is null&&visual.Verify(gpu),"accepted exact revision and readback");
        }
        async Task Failed(ulong retained)
        {
            await actor.DrainCompletion;Check(Tick(0)&&actor.LastAppliedRevision==retained&&actor.LastFailure is not null&&visual.Verify(gpu),"failure retains accepted display");
            int count=compositions;for(int i=0;i<8;i++)Check(Tick(0),"failure polling alive");Check(compositions==count,"no retry loop");
        }
        void Block(){release.Reset();entered=new(TaskCreationOptions.RunContinuationsAsynchronously);mode=1;}
        async Task Entered(){Check(await Task.WhenAny(entered.Task,Task.Delay(5000))==entered.Task,"controlled worker entered");}
        Set(1,zero);await Applied(1);string zeroHash=actor.GeneratedFaceSha256!;Check(Identity(visual,false)==initial,"mesh material shader original textures retained");
        if(visibilityRace){Check(!visual.Root.Visible,"initial appearance also commits hidden");Accept("human_baseline");Check(Apply(Snapshot())&&visual.Root.Visible,"nonzero1 restores initial appearance");}
        source.Value=new(Snapshot().Object.Incarnation,1,varied);int sameCalls=compositions;
        Check(Tick(0)&&actor.LastAppliedRevision==1&&actor.LastFailure is not null&&compositions==sameCalls,"same applied revision changed values rejected");
        if(capture is not null)await Capture(Captures(capture)[0]);
        motion.Value=new(Snapshot().Object.Incarnation,1,new(new(1,0,0),new(0,1,0),new(0,0,1),new(-1.5f,.2f,.1f)),2,null,false);
        locomotion.SetPlaying(true);Check(Tick(.1)&&locomotion.SelectedClip=="walk","walk clock");locomotion.SetFocused(false);double frame=locomotion.Frame;Check(Tick(.2)&&locomotion.Frame==frame,"focus freeze");locomotion.SetFocused(true);locomotion.SetPlaying(false);
        if(!race){Accept("human_height");Check(Apply(Snapshot()),"Height applied");}
        string? HeadHash(){if(!gpu)return null;var head=visual.Root.GetChild(0).GetChildren().OfType<MeshInstance3D>().Single(n=>n.Name=="HeadBindGeometry");var material=(ShaderMaterial)head.Mesh.SurfaceGetMaterial(0);using var image=((ImageTexture)material.GetShaderParameter("albedo_texture").AsGodotObject()).GetImage();return Convert.ToHexString(SHA256.HashData(image.GetData()));}
        object RaceState()
        {
            var state=Snapshot().Replication!;var t=visual.Root.Transform;
            return new{state.Revision,name=nameRace?new{text=nameActor!.Text,rawFile=Convert.ToHexString(state.Names.NameFile.CopyBytes()),rawKey=Convert.ToHexString(state.Names.NameKey.CopyBytes()),state.Names.NameSpacer,units=state.Names.CustomNameUnits,labelId=nameActor.LabelId,labelVisible=nameActor.IsVisibleInTree,nameResolves}:null,visibility=(visibilityRace||nameRace)?(byte?)state.TangibleScalars.Visibility:null,visible=visual.Root.Visible,state.Scalars.Posture,state.Scalars.Rank,state.Scalars.LinkId,heightBits=BitConverter.SingleToUInt32Bits(state.Scalars.Height),state.Scalars.Shock,state.Scalars.States,
                components=containerRace?new{counter=state.Components!.Counter,keys=state.Components.Keys}:null,
                wounds=containerRace?new{counter=state.Wounds!.Counter,values=state.Wounds.Values}:null,
                transform=new{origin=new[]{t.Origin.X,t.Origin.Y,t.Origin.Z},x=new[]{t.Basis.X.X,t.Basis.X.Y,t.Basis.X.Z},y=new[]{t.Basis.Y.X,t.Basis.Y.Y,t.Basis.Y.Z},z=new[]{t.Basis.Z.X,t.Basis.Z.Y,t.Basis.Z.Z}},
                pose=Identity(visual,true),ownership=Identity(visual,false),locomotion.Frame,locomotion.SelectedClip,sourceRevision=source.Value!.Revision,sourceValues=source.Value.Values.Count,actor.LastAppliedRevision,
                actor.GeneratedFaceSha256,gpuFaceSha256=HeadHash(),peakActive,active};
        }
        void Containers()
        {
            var current=Snapshot().Replication!;
            Check(current.Components!.Counter==12&&current.Components.Keys.SequenceEqual(new[]{-7,99})&&current.Wounds!.Counter==12&&current.Wounds.Values.SequenceEqual(new[]{5,-6,5}),"latest containers/counters including replay");
        }
        object? heldState=null,committedState=null,resetState=null,visibleBefore=null,shownState=null,firstNameState=null,hiddenNameState=null;
        if(visibilityRace)
        {
            visibleBefore=RaceState();if(capture is not null)await Capture(VisibilityCaptures(capture)[0]);
            Accept("visibility_hidden_baseline");Check(Apply(Snapshot())&&!visual.Root.Visible&&Snapshot().Replication!.TangibleScalars.Visibility==0,"complete baseline hides current actor");
        }
        Block();Set(2,varied);await Entered();Set(3,zero);Set(4,varied);
        if(visibilityRace)
        {
            Check(!visual.Root.Visible&&actor.LastAppliedRevision==1&&active==1&&actor.GeneratedFaceSha256==zeroHash&&visual.Verify(gpu),"hidden CPU accepts and coalesces source revisions");
            heldState=RaceState();if(capture is not null)await Capture(VisibilityCaptures(capture)[1]);
        }
        if(nameRace)
        {
            Accept("name_first_baseline");Check(Apply(Snapshot())&&nameActor!.Text=="FIRST CAPTION"&&active==1,"first name while appearance held");
            firstNameState=RaceState();if(capture is not null)await Capture(NameCaptures(capture)[0]);
            Accept("name_latest_baseline");Check(Apply(Snapshot())&&nameActor!.Text=="LATEST CAPTION"&&active==1,"latest name before GPU commit");
            heldState=RaceState();if(capture is not null)await Capture(NameCaptures(capture)[1]);
        }
        if(bodyRace)
        {
            Accept(containerRace?"race_containers_first":"race_scalars_first");Check(Apply(Snapshot())&&actor.LastAppliedRevision==1&&active==1,"first scalar update while CPU held");
            locomotion.SetPlaying(true);Check(Tick(.05),"clock progresses during held composition");locomotion.SetFocused(false);double suspended=locomotion.Frame;
            Accept(containerRace?"race_containers_latest":"race_scalars_latest");Check(Apply(Snapshot())&&Tick(.2)&&locomotion.Frame==suspended,"latest scalars and focus freeze while CPU held");locomotion.SetFocused(true);locomotion.SetPlaying(false);
            if(containerRace){Accept("race_containers_latest");Check(Apply(Snapshot()),"compound counter replay accepted");Containers();}
            var state=Snapshot().Replication!.Scalars;
            Check(state.Height==1.6f&&state.Posture==7&&state.Rank==42&&state.LinkId==0xF102030405060799&&state.Shock==0xDEADBEEF&&state.States==0xFEDCBA9876543211,"latest six scalar values");
            var expected=SceneCreatureStatePresentation.DeriveTransform(GodotAppearanceTransform.ToGodot(motion.Value!.Transform),state.Height);
            Check(visual.Root.Transform==expected&&actor.GeneratedFaceSha256==zeroHash&&visual.Verify(gpu)&&peakActive==1,"latest Height applied once before commit");
            heldState=RaceState();if(capture is not null)await Capture(RaceCaptures(capture)[0]);
        }
        var transform=visual.Root.Transform;string pose=Identity(visual,true);double fixedFrame=locomotion.Frame;
        var scalarState=Snapshot().Replication!.Scalars;var containerState=Snapshot().Replication;var sourceState=source.Value;var retainedNames=Snapshot().Replication!.Names;int retainedResolves=nameResolves;release.Set();await Applied(4);
        Check(actor.LastAppliedRevision==4&&compositions==3&&actor.GeneratedFaceSha256!=zeroHash&&Identity(visual,true)==pose&&visual.Root.Transform==transform&&locomotion.Frame==fixedFrame,"latest pending skips intermediate and preserves pose Height clock");
        if(race)
        {
            Check(Snapshot().Replication!.Scalars==scalarState&&ReferenceEquals(source.Value,sourceState)&&visual.Verify(gpu),"appearance commit cannot revert body/source state");
            if(containerRace){Containers();Check(ReferenceEquals(Snapshot().Replication!.Components,containerState!.Components)&&ReferenceEquals(Snapshot().Replication!.Wounds,containerState.Wounds),"appearance retains current immutable container identities");}
            if(visibilityRace)Check(!visual.Root.Visible&&Snapshot().Replication!.TangibleScalars.Visibility==0,"newest appearance commits while hidden");
            if(nameRace)Check(nameActor!.Text=="LATEST CAPTION"&&ReferenceEquals(Snapshot().Replication!.Names,retainedNames)&&nameResolves==retainedResolves,"appearance cannot revert raw name or trigger resolution");
            committedState=RaceState();if(capture is not null)await Capture(nameRace?NameCaptures(capture)[2]:visibilityRace?VisibilityCaptures(capture)[2]:RaceCaptures(capture)[1]);
        }
        if(visibilityRace)
        {
            int committedCalls=compositions;Accept("visibility_shown_baseline");
            Check(Apply(Snapshot())&&visual.Root.Visible&&Snapshot().Replication!.TangibleScalars.Visibility==255&&actor.LastAppliedRevision==4&&compositions==committedCalls&&visual.Verify(gpu),"nonzero255 shows latest appearance without composition/reload");
            Check(Identity(visual,true)==pose&&visual.Root.Transform==transform&&locomotion.Frame==fixedFrame&&ReferenceEquals(source.Value,sourceState)&&world.IsCurrent(publication)&&jobs.Count==1&&archivesClosed==1,"show preserves pose clock source publication and resource identities");
            shownState=RaceState();if(capture is not null)await Capture(VisibilityCaptures(capture)[3]);
        }
        if(nameRace)
        {
            Accept("name_hidden_baseline");Check(Apply(Snapshot())&&!visual.Root.Visible&&!nameActor!.IsVisibleInTree&&nameActor.Text=="LATEST CAPTION","caption inherits hidden parent");
            hiddenNameState=RaceState();if(capture is not null)await Capture(NameCaptures(capture)[3]);
            Accept("name_latest_baseline");Check(Apply(Snapshot())&&visual.Root.Visible&&nameActor!.IsVisibleInTree&&nameActor.Text=="LATEST CAPTION"&&compositions==3&&jobs.Count==1&&world.IsCurrent(publication)&&Identity(visual,true)==pose&&visual.Root.Transform==transform,"caption restores latest without appearance reload");
            shownState=RaceState();if(capture is not null)await Capture(NameCaptures(capture)[4]);
        }
        string variedHash=actor.GeneratedFaceSha256!;var broken=zero.ToDictionary(p=>p.Key,p=>p.Value);broken.Remove(broken.Keys.First());Set(5,broken);await Failed(4);
        Set(6,zero);await Applied(6);Check(actor.GeneratedFaceSha256==zeroHash,"explicit zero reset");source.Value=null;Check(Tick(0)&&actor.LastAppliedRevision==6,"missing retains reset");
        source.Value=new(Snapshot().Object.Incarnation,4,varied);Check(Tick(0)&&actor.LastAppliedRevision==6,"older ignored");
        source.Value=new(Snapshot().Object.Incarnation with{ObjectId=999},7,varied);Check(Tick(0)&&actor.LastAppliedRevision==6,"foreign ignored");
        Block();Set(7,varied);await Entered();int blockedCalls=compositions;
        for(ulong revision=8;revision<=16;revision++){source.Value=null;Check(Tick(0),"missing cancels pending");Set(revision,varied);}
        Check(compositions==blockedCalls&&peakActive==1&&!actor.DrainCompletion.IsCompleted,"one retiring worker and replaceable pending across repeated disappearance");
        source.Value=null;Check(Tick(0),"last pending canceled");release.Set();await actor.DrainCompletion;Check(Tick(0)&&actor.LastAppliedRevision==6,"noncooperative completion cannot commit after missing");
        Set(17,varied);await Applied(17);mode=2;Set(18,zero);await Failed(17);
        source.Value=new(Snapshot().Object.Incarnation,18,varied);Check(Tick(0)&&actor.LastAppliedRevision==17&&compositions==blockedCalls+2,"same failed revision changed values rejected");
        mode=3;Set(19,zero);await Failed(17);mode=4;Set(20,zero);await Failed(17);
        Set(21,zero);await actor.DrainCompletion;int finds=0;source.OnFind=()=>{if(++finds==2)source.Value=new(Snapshot().Object.Incarnation,22,varied);};
        Check(Tick(0)&&actor.LastAppliedRevision==17,"freshness rechecked immediately before commit");source.OnFind=null;Check(Tick(0),"newer request after discarded ready");await Applied(22);
        motion.Value=motion.Value! with{Revision=2,Speed=6};locomotion.SetPlaying(true);Check(Tick(.1)&&locomotion.SelectedClip=="run","run clock survives customization");locomotion.Reset();Check(locomotion.Frame==0,"locomotion reset");
        motion.Value=null;locomotion.SetPlaying(false);Check(Tick(0)&&locomotion.SelectedClip=="idle","idle return");
        // Captures use the same initial scale and frame, separately from the live Height proof above.
        var height=Convert.FromHexString(peer["human_height"]);BitConverter.GetBytes(1f).CopyTo(height,height.Length-4);Check(world.Accept(generation,height)&&Apply(Snapshot()),"capture Height reset");Array.Clear(height);
        Check(Identity(visual,false)==initial,"retained topology/material/shader/original texture identity");if(capture is not null)await Capture(Captures(capture)[1]);
        if(bodyRace){if(containerRace)Containers();resetState=RaceState();if(capture is not null)await Capture(RaceCaptures(capture)[2]);}
        var oracle=new[]{("zero",zero),("varied",varied)}.Select(profile=>
        {
            var applied=SkeletalAppearanceCustomization.Apply(basis,profile.Item2);return new{name=profile.Item1,sha256=Convert.ToHexString(SHA256.HashData(applied.GeneratedFaces!.Outputs.Single().Value.Image.Mips[0].Rgba.Span)),
                evidence=applied.FaceBlueprints!.Select(p=>{var values=applied.GeneratedFaces.Values;var prepared=BlueprintFacePreparer.Prepare(p.Value,applied.FaceResources!,values);var composed=applied.GeneratedFaces.Outputs[p.Key];return new{blueprint=new{path=p.Key,p.Value.Width,p.Value.Height,p.Value.CameraScalar,p.Value.RenderSets,p.Value.ShaderPaths,p.Value.TexturePaths,p.Value.PrepareLists},prepared=new{path=p.Key,requested=0,values=values.Select(v=>new{v.Key.VariableName,v.Key.FactoryFlag,Value=v.Value}),prepared.ShaderSlots,prepared.Operations},composed=new{path=p.Key,requested=0,policy=BlueprintComposedFace.Policy,composed.Image.Width,composed.Image.Height,composed.Layers,sha256=Convert.ToHexString(SHA256.HashData(composed.Image.Mips[0].Rgba.Span))}};}).ToArray()};
        }).ToArray();
        Check(oracle[0].sha256==zeroHash&&oracle[1].sha256==variedHash&&actor.GeneratedFaceSha256==variedHash,"accepted diagnostic matches owned independent-oracle inputs");
        File.WriteAllText(export,JsonSerializer.Serialize(new{raw=raw.ToDictionary(p=>p.Key,p=>Convert.ToBase64String(p.Value)),oracle,reads=publication.Resources.ExternalReads,bytes=publication.Resources.SourceBytes,zeroHash,variedHash,
            heightRace=heightRace?new{gpu,retirement,held=heldState,committed=committedState,reset=resetState}:null,
            containerRace=containerRace?new{gpu,retirement,woundsPolicy="StrictInterval",held=heldState,committed=committedState,reset=resetState}:null,
            visibilityRace=visibilityRace?new{gpu,retirement,createdHidden,inputPolicy="CompleteBaselineReseed",before=visibleBefore,held=heldState,committed=committedState,shown=shownState}:null,
            nameRace=nameRace?new{gpu,retirement,inputPolicy="CompleteBaselineReseed",resolverPolicy="SyntheticCustomThenExactStringId",first=firstNameState,held=heldState,committed=committedState,hidden=hiddenNameState,shown=shownState}:null}));raw.Clear();
        GD.Print($"PASS: customization latest_only=true full_keys=8 malformed_retain=true same_revision_rejected=true no_retry=true GPU_atomic_rejections=2 peak_active={peakActive} motion_focus_height_clock=true original_identities=true zero_hash={zeroHash} varied_hash={variedHash}");
        if(visibilityRace){Accept("visibility_hidden_baseline");Check(Apply(Snapshot())&&!visual.Root.Visible,"retirement begins hidden without reload");}
        if(nameRace){Accept("name_hidden_baseline");Check(Apply(Snapshot())&&!nameActor!.IsVisibleInTree,"name retirement starts hidden");}
        Block();Set(23,zero);await Entered();if(race&&retirement=="destroy")Accept("human_destroy");else world.BeginSelection(Human);
        Check(!Tick(0)&&actor.IsRetired&&visual.IsRetired&&actor.LastAppliedRevision==22,"incarnation loss retires owned visual without held commit");if(nameActor is not null){nameActor.Dispose();nameActor.Dispose();Check(nameActor.IsRetired,"name owner retired");}else actor.Dispose();Check(!actor.DrainCompletion.IsCompleted,"noncooperative drain observable after disposal");release.Set();await actor.DrainCompletion;Check(canceledTokens>=3&&peakActive==1,"cancel signaled and one composer throughout");
        if(nameRace)GD.Print($"PASS: customization name race retirement={retirement} latest_caption=true resolver_cached=true raw_name_pose_source_resources_retained=true hidden_caption=true peak_active={peakActive} drain_complete=true baseline_reseed_not_delta=true");
        if(visibilityRace)GD.Print($"PASS: customization visibility race retirement={retirement} hidden_commit=true raw_visibility255=true shown_latest_without_reload=true peak_active={peakActive} hidden_retirement_drain=true baseline_reseed_not_delta=true");
        if(containerRace)GD.Print($"PASS: customization container race retirement={retirement} latest_containers_counters=true replay=true pose_clock_source_retained=true peak_active={peakActive} held_close_wait=true");
        if(heightRace)GD.Print($"PASS: customization Height race retirement={retirement} latest_height=1.6 six_scalars=true pose_clock_source_retained=true peak_active={peakActive} held_close_wait=true");
        await Shutdown();Check(SceneCreatureVisual.Allocations==1&&SceneCreatureVisual.Disposals==1&&archivesClosed==1,"final owned lifecycle");GD.Print("PASS: customization teardown roots=1/1 archives=1 drain_complete=true");
    }
    private static string DiagnosticName(CreatureView3NameState names)
    {
        // Explicit supplied fixture policy, independent of any original locale/name priority.
        if(names.CustomNameUnits.Count>64||names.NameFile.Length>32||names.NameKey.Length>32)throw new InvalidDataException("Diagnostic name input bound.");
        if(names.CustomNameUnits.Count!=0)
        {
            if(names.CustomNameUnits.Any(u=>u is <32 or >126))throw new InvalidDataException("Diagnostic custom name must be printable ASCII units.");
            return new string(names.CustomNameUnits.Select(u=>(char)u).ToArray());
        }
        var file=names.NameFile.CopyBytes();var key=names.NameKey.CopyBytes();
        if(file.Length==0&&key.Length==0)return "BASE CAPTION";
        if(!file.AsSpan().SequenceEqual("diagnostic"u8))throw new InvalidDataException("Unknown diagnostic table.");
        if(key.AsSpan().SequenceEqual("alpha"u8))return "FIRST CAPTION";
        if(key.AsSpan().SequenceEqual("beta"u8))return "SECOND CAPTION";
        throw new InvalidDataException("Unknown diagnostic key.");
    }
    private void NameGuards(SceneWorldCoordinator current,ISceneWorldObjectPresentation owned,SceneWorldObjectVisual basis)
    {
        int labels=basis.CreatureVisual!.Root.GetChildren().OfType<Label3D>().Count();
        foreach(Func<CreatureView3NameState,string> bad in new Func<CreatureView3NameState,string>[]
            {_=>null!,_=>new string('X',257),_=>"\uD800",_=>"line\n",_=>throw new InvalidDataException("supplied resolver failure")})
        {
            var probe=new NameProbe(owned.Publication);bool reject=false;
            try{_=new SceneWorldObjectNameVisual(current,probe,basis,bad);}catch(InvalidDataException){reject=true;}
            Check(reject&&probe.Disposals==0&&current.IsCurrent(owned.Publication),"failed name constructor keeps caller ownership");probe.Dispose();
        }
        var mutation=new NameProbe(owned.Publication);bool mutationRejected=false;
        try{_=new SceneWorldObjectNameVisual(current,mutation,basis,_=>{Accept("name_first_baseline");return "changed";});}catch(InvalidOperationException){mutationRejected=true;}
        Check(mutationRejected&&mutation.Disposals==0,"name constructor detects resolver world mutation");mutation.Dispose();Accept("human_baseline");
        int resolutions=0;var caching=new NameProbe(owned.Publication);var cache=new SceneWorldObjectNameVisual(current,caching,basis,_=>{resolutions++;return "PROBE";});
        var stale=current.Snapshots().Single();Check(cache.Tick(0)&&resolutions==1,"same owned names skip callback");Accept("name_first_baseline");
        Check(!cache.Apply(stale)&&cache.Tick(0)&&resolutions==2,"stale Apply rejected and fresh names resolved");cache.Dispose();cache.Dispose();Check(caching.Disposals==1&&cache.LabelId==0,"caption and inner disposed exactly once");Accept("human_baseline");
        var failing=new NameProbe(owned.Publication);bool fail=false;var failure=new SceneWorldObjectNameVisual(current,failing,basis,_=>fail?throw new InvalidDataException("late resolver failure"):"PROBE");
        fail=true;Accept("name_first_baseline");bool caught=false;try{failure.Tick(0);}catch(InvalidDataException){caught=true;}
        Check(caught&&failure.IsRetired&&failure.LastFailure is not null&&failing.Disposals==1&&failure.LabelId==0,"resolver failure retires owned label/inner");failure.Dispose();Accept("human_baseline");
        var reentry=new NameProbe(owned.Publication);SceneWorldObjectNameVisual? recursive=null;
        recursive=new(current,reentry,basis,_=>{if(recursive is not null){try{recursive.Tick(0);}catch(InvalidOperationException){}}return "PROBE";});
        Accept("name_first_baseline");caught=false;try{recursive.Tick(0);}catch(InvalidOperationException){caught=true;}
        Check(caught&&recursive.IsRetired&&reentry.Disposals==1,"caught resolver reentry cannot publish");recursive.Dispose();Accept("human_baseline");
        bool move=false;var moved=new NameProbe(owned.Publication);var movedCaption=new SceneWorldObjectNameVisual(current,moved,basis,_=>
            {if(move)basis.CreatureVisual.Root.GetChildren().OfType<Label3D>().Single().Reparent(this);return "PROBE";});
        move=true;Accept("name_first_baseline");caught=false;try{movedCaption.Tick(0);}catch(InvalidOperationException){caught=true;}
        Check(caught&&movedCaption.IsRetired&&moved.Disposals==1&&movedCaption.LabelId==0,"callback cannot move caption ownership before publication");movedCaption.Dispose();Accept("human_baseline");
        Check(basis.CreatureVisual.Root.GetChildren().OfType<Label3D>().Count()==labels&&!basis.CreatureVisual.IsRetired,"all probe labels removed and borrowed base retained");
        GD.Print("PASS: name guards ctor_ownership=true cache=true stale_apply=true callback_mutation_reentry_failure=true label_inner_once=true");
    }
    private sealed class NameProbe:ISceneWorldObjectPresentation
    {
        public NameProbe(ScenePreparedObjectPublication publication){Publication=publication;}
        public ScenePreparedObjectPublication Publication{get;}
        public int Disposals{get;private set;}
        public bool Tick(double seconds)=>true;
        public bool Apply(SceneWorldObjectSnapshot snapshot)=>true;
        public void Dispose(){Disposals++;}
    }
    private static string Identity(SceneCreatureVisual visual,bool pose)
    {
        using var hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        void U(ulong value)=>hash.AppendData(BitConverter.GetBytes(value));void F(float value)=>hash.AppendData(BitConverter.GetBytes(value));
        U(visual.Root.GetInstanceId());int count=0;
        foreach(var node in visual.Root.GetChild(0).GetChildren().OfType<MeshInstance3D>())
        {U(node.Mesh.GetInstanceId());for(int i=0;i<node.Mesh.GetSurfaceCount();i++)
            {count++;var material=(ShaderMaterial)node.Mesh.SurfaceGetMaterial(i);U(material.GetInstanceId());U(material.Shader.GetInstanceId());
                if(!(node.Name=="HeadBindGeometry"&&i==0))U(material.GetShaderParameter("albedo_texture").AsGodotObject().GetInstanceId());
                using var arrays=node.Mesh.SurfaceGetArrays(i);foreach(var v in arrays[(int)Mesh.ArrayType.TexUV].AsVector2Array()){F(v.X);F(v.Y);}foreach(int index in arrays[(int)Mesh.ArrayType.Index].AsInt32Array())hash.AppendData(BitConverter.GetBytes(index));
                if(pose)foreach(var field in new[]{Mesh.ArrayType.Vertex,Mesh.ArrayType.Normal})foreach(var v in arrays[(int)field].AsVector3Array()){F(v.X);F(v.Y);F(v.Z);}
            }}Check(count==5,"five diagnostic surfaces");return Convert.ToHexString(hash.GetHashAndReset());
    }
    private void Accept(string key){byte[] bytes=Convert.FromHexString(peer[key]);try{Check(world!.Accept(generation,bytes),"application fixture "+key);}finally{Array.Clear(bytes);}}
    public static string[] Captures(string first)=>new[]{first,Path.Combine(Path.GetDirectoryName(first)!,Path.GetFileNameWithoutExtension(first)+"-varied.png")};
    public static string[] RaceCaptures(string first)=>new[]{"-held","-committed","-reset"}.Select(s=>Path.Combine(Path.GetDirectoryName(first)!,Path.GetFileNameWithoutExtension(first)+s+".png")).ToArray();
    public static string[] VisibilityCaptures(string first)=>new[]{"-visible-before","-hidden-held","-hidden-committed","-shown"}.Select(s=>Path.Combine(Path.GetDirectoryName(first)!,Path.GetFileNameWithoutExtension(first)+s+".png")).ToArray();
    public static string[] NameCaptures(string first)=>new[]{"-first","-latest-held","-latest-committed","-hidden","-shown"}.Select(s=>Path.Combine(Path.GetDirectoryName(first)!,Path.GetFileNameWithoutExtension(first)+s+".png")).ToArray();
    private async Task Capture(string path){await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);using var image=GetViewport().GetTexture().GetImage();Check(image.SavePng(path)==Error.Ok,"capture");}
    private async Task Shutdown(){if(closed)return;closed=true;release.Set();if(nameActor is not null)nameActor.Dispose();else actor?.Dispose();if(actor is not null)await actor.DrainCompletion;world?.Close();if(world is not null)await world.DrainCompletion;await Task.WhenAll(jobs);environment?.Dispose();environment=null;raw.Clear();}
    private static void Check(bool value,string message){if(!value)throw new InvalidDataException(message);}
    private sealed class ValuesSource:ISceneWorldCreatureCustomizationSource{internal SceneWorldCreatureCustomizationSnapshot? Value;internal Action? OnFind;public SceneWorldCreatureCustomizationSnapshot? Find(SceneWorldObjectSnapshot snapshot){OnFind?.Invoke();return Value;}}
    private sealed class MotionSource:ISceneWorldObjectMotionSource{internal SceneWorldObjectMotionSnapshot? Value;public SceneWorldObjectMotionSnapshot? Find(SceneWorldObjectSnapshot snapshot)=>Value;}
    public override void _ExitTree(){release.Set();if(nameActor is not null)nameActor.Dispose();else actor?.Dispose();world?.Close();environment?.Dispose();}
}
