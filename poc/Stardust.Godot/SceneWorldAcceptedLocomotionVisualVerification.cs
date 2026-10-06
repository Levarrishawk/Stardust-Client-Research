using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

/// <summary>Standalone accepted-state presentation replay; original resources are read, never executed.</summary>
public partial class SceneWorldAcceptedLocomotionVisualVerification : Node3D
{
    private const ulong Actor = 0xF122334455667788;
    private int checks;
    private readonly List<object> samples = new();
    private SceneWorldCoordinator? world;
    private SceneWorldAcceptedLocomotionVisual? current;
    private SceneCreatureResources? prepared;
    private SceneObjectResources? resources;
    private Dictionary<string,string> peer = new();
    private ObjectTemplateCatalog catalog = null!;
    private LogicalAnimationClipSelection idle = null!;
    private IReadOnlyDictionary<string,LogicalAnimationClipSelection> bank = null!;
    private string assetRoot = "", config = "", output = "";
    public override async void _Ready()
    {
        try { await Run(); GetTree().Quit(); }
        catch (Exception error) { GD.PushError(error.ToString()); current?.Dispose(); world?.Close(); GetTree().Quit(1); }
    }
    private void Check(bool value, string reason) { checks++; if (!value) throw new InvalidDataException(reason); }
    private SceneWorldObjectSnapshot Snapshot() => world!.Snapshots().Single(s => s.Object.Handle.ObjectId == Actor);
    private void Accept(string key) { Check(world!.Accept(world.Generation, Convert.FromHexString(peer[key])), "application " + key); }
    private async Task<SceneWorldObjectMotionVisual> Fresh()
    {
        current?.Dispose(); current = null;
        if (world is not null) { world.Close(); await world.DrainCompletion; }
        Task<SceneObjectResources>? job = null;
        var options = new Dictionary<SceneObjectResourceRecipe,SceneObjectResourceOptions>
        { [SceneObjectResourceRecipe.CreatureDetail0] = new SceneCreatureResourceOptions(idle,bank,true),
          [SceneObjectResourceRecipe.StaticAppearance] = new SceneStaticResourceOptions(SceneStaticMaterialPolicy.RequireComplete,null) };
        world = new(catalog, options, (request, token) => job = Task.Run(() =>
        {
            if (resources is not null) return resources;
            using var text = new StringReader(File.ReadAllText(config)); using var overlay = ConfiguredTreOverlay.Open(assetRoot, text);
            return SceneObjectResourceRouter.Load(request.Snapshot.Create, request.Catalog, request.Options, overlay.Lookup, token);
        }), SceneCreateZeroQuaternionPolicy.Reject, replicationPolicy:
            new(SceneWorldReplicationMode.CreatureView3Scalars, new(1024,1024,1024,128,16384),16,16384));
        world.BeginSelection(Actor); Accept("start"); Accept("a_create"); Accept("a_baseline"); Accept("a_end");
        Check(world.CompleteMinimumSceneBarrier(world.Generation, true) is not null, "scene barrier");
        world.PollObjectResources();
        await job!;
        ScenePreparedObjectPublication? publication = world.PollObjectResources().Prepared.SingleOrDefault();
        Check(publication is not null, "prepared publication");
        resources ??= publication!.Resources; prepared ??= resources.Creature!; Accept("ready");
        var visual = (SceneWorldObjectVisual)new SceneWorldObjectVisualFactory(world, this).Create(publication!, Snapshot())!;
        return new(world, visual, new EmptyMotion());
    }
    private sealed class EmptyMotion : ISceneWorldObjectMotionSource
    { public SceneWorldObjectMotionSnapshot? Find(SceneWorldObjectSnapshot snapshot) => null; }
    private sealed class Chain : ISceneWorldObjectPresentation
    {
        public readonly SceneWorldObjectMotionVisual Inner;
        public ScenePreparedObjectPublication? OverridePublication;
        public Action? OnPublication;
        public ScenePreparedObjectPublication Publication { get { OnPublication?.Invoke(); return OverridePublication ?? Inner.Publication; } }
        public int Ticks, Applies, Disposals;
        public double LastSeconds;
        public Chain(SceneWorldObjectMotionVisual inner) { Inner = inner; }
        public bool Tick(double seconds) { Ticks++; LastSeconds = seconds; return Inner.Tick(seconds); }
        public bool Apply(SceneWorldObjectSnapshot snapshot) { Applies++; return Inner.Apply(snapshot); }
        public void Dispose() { Disposals++; Inner.Dispose(); }
    }
    private float[][] Vertices(SceneWorldObjectMotionVisual inner)
    {
        var body = inner.Inner.CreatureVisual!.Root.FindChild("BodyBindGeometry", true, false) as MeshInstance3D;
        using var arrays = body!.Mesh.SurfaceGetArrays(0);
        return arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array().Take(8).Select(v => new[]{v.X,v.Y,v.Z}).ToArray();
    }
    private async Task Run()
    {
        string[] args = OS.GetCmdlineUserArgs();
        string Arg(string key) { int at = Array.IndexOf(args,key); if (at < 0 || at + 1 >= args.Length) throw new ArgumentException(key); return args[at+1]; }
        assetRoot = Arg("--asset-root"); config = Arg("--asset-config"); output = Arg("--out");
        peer = JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllText(Arg("--peer")))!;
        string repository = Arg("--repository");
        catalog = ObjectTemplateCatalogProfile.Decode(File.ReadAllBytes(Path.Combine(repository,"poc/preview-profiles/scene-object-catalog.json")));
        using (var stream = File.OpenRead(Path.Combine(repository,"poc/preview-profiles/human-locomotion.json"))) bank = SkeletalAnimationBankProfile.Load(stream);
        idle = new("appearance/lat/all_m.lat","emt_standing_idle_ambient_1_ag","798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B");
        var inner = await Fresh(); var chain = new Chain(inner); bool enabled = false; int reads = 0;
        var resource = prepared!.Creature;
        var clock = new LocomotionPreviewClock(LocomotionPreviewClock.Describe(resource.PreparedAnimation!),
            LocomotionPreviewClock.Describe(resource.AdditionalAnimations!["walk"]), LocomotionPreviewClock.Describe(resource.AdditionalAnimations["run"]),4);
        current = new(world!,inner,() => { reads++; return clock.State; },chain,() => enabled);
        Check(current.Tick(.1) && reads == 0 && chain.LastSeconds == .1, "disabled forwards elapsed without source");
        Check(current.Apply(Snapshot()) && chain.Applies == 1 && reads == 0, "disabled Apply forwards once");
        enabled = true;
        var steps = JsonSerializer.Deserialize<Step[]>(File.ReadAllText(Arg("--steps")))!;
        foreach (var step in steps)
        {
            if (step.Advance) clock.Advance(step.Seconds,new(step.X,0,step.Z));
            var state = clock.State; long before = current.SourceReads;
            Check(step.Apply ? current.Apply(Snapshot()) : current.Tick(step.TickSeconds), "supplied state presentation");
            Check(current.SourceReads == before + 1 && ReferenceEquals(current.LastState,state) &&
                current.Frame == state.Frame && current.SelectedClip == state.Mode.ToString().ToLowerInvariant(), "exact single source observation");
            if (!step.Apply) Check(chain.LastSeconds == 0, "enabled zero-time inner tick");
            var pose = inner.Inner.CreatureVisual!.Fingerprint(pose:true);
            Check(current.Tick(60) && inner.Inner.CreatureVisual.Fingerprint(pose:true) == pose && current.Frame == state.Frame,
                "same state pose idempotent despite elapsed time");
            samples.Add(new {step.Label,Mode=current.SelectedClip,current.Frame,state.NormalizedPhase,state.AcceptedDistance,state.Switches,
                Heading=new[]{state.Heading.X,state.Heading.Y,state.Heading.Z},Vertices=Vertices(inner)});
        }
        Check(await Task.Run(() => { try { current.Tick(0); return false; } catch(InvalidOperationException) { return true; } }), "off-thread before callback");
        Check(!current.IsRetired && current.Tick(0), "owner survives off-thread rejection");
        var obsolete = Snapshot(); long oldReads = current.SourceReads;
        Accept("a_height");
        Check(!current.Apply(obsolete) && !current.IsRetired && current.SourceReads==oldReads, "obsolete supplied snapshot rejected before callbacks");
        Check(current.Apply(Snapshot()) && Math.Abs(inner.Inner.CreatureVisual!.Root.Transform.Basis.Y.Length()-1.4f)<1e-6,
            "actual current Height publication applied");
        current.Dispose(); current.Dispose(); Check(chain.Disposals == 1 && world!.IsCurrent(inner.Publication), "one chain disposal borrowed world survives");
        var oldPublication = inner.Publication;
        var negatives = new List<string>();
        foreach (string kind in new[]{"nan-frame","large-frame","phase","distance","heading","mode","switches","null","source-throw",
            "source-tick-reentry","source-apply-reentry","source-dispose-reentry","enabled-reentry","enabled-throw","canceled","callback-cancel","nan-seconds","negative-seconds","detached","freed","publication-loss","enabled-publication-loss","revision-change"})
        {
            inner = await Fresh(); chain = new Chain(inner); var initial = clock.State with {Mode=PreviewLocomotionMode.Idle,Frame=0,NormalizedPhase=0};
            if(kind=="nan-frame")
            {
                var wrong = new Chain(inner) {OverridePublication=oldPublication}; bool rejected=false;
                try { new SceneWorldAcceptedLocomotionVisual(world!,inner,() => initial,wrong); } catch(ArgumentException) { rejected=true; }
                Check(rejected && wrong.Disposals==0 && !inner.IsRetired, "mismatched chain rejected before adoption");
            }
            using var cancellation = new CancellationTokenSource();
            LocomotionPreviewState Source()
            {
                switch(kind)
                {
                    case "source-throw": throw new IOException("fixture callback");
                    case "source-tick-reentry": try { current!.Tick(0); } catch(InvalidOperationException) {} break;
                    case "source-apply-reentry": try { current!.Apply(Snapshot()); } catch(InvalidOperationException) {} break;
                    case "source-dispose-reentry": try { current!.Dispose(); } catch(InvalidOperationException) {} break;
                    case "callback-cancel": cancellation.Cancel(); break;
                    case "publication-loss": world!.BeginSelection(Actor); break;
                    case "revision-change": Accept("a_height"); break;
                }
                return kind switch {
                    "nan-frame" => initial with {Frame=double.NaN}, "large-frame" => initial with {Frame=1e9},
                    "phase" => initial with {NormalizedPhase=1}, "distance" => initial with {AcceptedDistance=double.PositiveInfinity},
                    "heading" => initial with {Heading=new(float.NaN,0,1)}, "mode" => initial with {Mode=(PreviewLocomotionMode)99},
                    "switches" => initial with {Switches=-1}, "null" => null!, _ => initial };
            }
            bool Enable()
            {
                if(kind=="enabled-reentry") { try { current!.Tick(0); } catch(InvalidOperationException) {} }
                if(kind=="enabled-throw") throw new IOException("fixture enable callback");
                if(kind=="enabled-publication-loss") world!.BeginSelection(Actor);
                return true;
            }
            current = new(world!,inner,Source,chain,Enable);
            if(kind=="canceled") cancellation.Cancel();
            if(kind=="detached") RemoveChild(inner.Inner.CreatureVisual!.Root);
            if(kind=="freed") inner.Inner.CreatureVisual!.Root.Free();
            bool failed = false;
            try { failed = !current.Tick(kind=="nan-seconds" ? double.NaN : kind=="negative-seconds" ? -1 : .1,cancellation.Token); }
            catch(Exception error) when(error is InvalidDataException or InvalidOperationException or OperationCanceledException or ArgumentOutOfRangeException or IOException or ObjectDisposedException) { failed = true; }
            Check(failed && current.IsRetired && chain.Disposals==1 && inner.Inner.CreatureVisual!.IsRetired, "owned failure retirement " + kind);
            bool beforeSource = kind is "enabled-reentry" or "enabled-throw" or "enabled-publication-loss" or "canceled" or "nan-seconds" or "negative-seconds" or "detached" or "freed";
            Check(current.SourceReads==(beforeSource ? 0 : 1), "bounded source observation on failure " + kind);
            Check(!current.Tick(0) && chain.Disposals==1, "retired no further callbacks " + kind);
            current.Dispose(); negatives.Add(kind);
        }
        inner = await Fresh(); chain = new Chain(inner) {OnPublication=() => world!.BeginSelection(Actor)};
        bool getterRejected=false; int getterSourceReads=0;
        try { new SceneWorldAcceptedLocomotionVisual(world!,inner,() => {getterSourceReads++; return clock.State;},chain); }
        catch(ArgumentException) {getterRejected=true;}
        Check(getterRejected && getterSourceReads==0 && chain.Disposals==0 && !inner.IsRetired,
            "constructor getter publication loss rejects before ownership adoption");
        chain.OnPublication=null; chain.Dispose(); Check(chain.Disposals==1,"constructor-failed chain remains caller-owned");
        inner = await Fresh(); current = new(world!,inner,() => clock.State);
        Check(current.Tick(0), "default exact inner ownership");
        var camera = new Camera3D {Current=true,Position=new Vector3(0,1.3f,-6)}; AddChild(camera); camera.LookAt(new Vector3(0,1.1f,0));
        AddChild(new DirectionalLight3D {RotationDegrees=new Vector3(-35,-25,0),LightEnergy=2});
        bool gpu = DisplayServer.GetName() != "headless";
        if(gpu)
        {
            await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            using var image = GetViewport().GetTexture().GetImage(); Check(image.SavePng(Path.ChangeExtension(output,"png"))==Error.Ok,"replacement GPU PNG");
        }
        current.Dispose(); world!.Close(); await world.DrainCompletion;
        File.WriteAllText(output,JsonSerializer.Serialize(new {Passed=true,Checks=checks,Gpu=gpu,Samples=samples,Negatives=negatives,
            OriginalSources=prepared!.ExternalReads,OriginalBytes=prepared.SourceBytes,NoSecondClock=true,SourceAssociation="caller contract"}));
        GD.Print("PASS accepted locomotion checks="+checks);
    }
    private sealed record Step(string Label,bool Advance,double Seconds,float X,float Z,double TickSeconds,bool Apply);
}
