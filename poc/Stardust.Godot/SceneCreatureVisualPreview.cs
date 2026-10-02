using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Stardust.Core.Assets;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

public partial class SceneCreatureVisualPreview : Node
{
    private OwnedBackgroundAssetLoad<SceneCreatureResources>? load;
    private SceneCreatureResources? source;
    private SceneCreatureVisual? visual;
    private SceneObjectRegistry? registry;
    private readonly Dictionary<string, AnimationPreviewClock> movement = new();
    private readonly List<Resource> resources = new();
    private Label? status;
    private string selectedClip = "idle";
    private bool active, disposed, playing = true, focused = true;
    private static readonly Transform3D EnginePose = new(new Basis(Vector3.Up, .65f), new Vector3(1.4f, .2f, -.75f));

    internal async Task InitializeAsync(string[] args)
    {
        string Read(string key) { int at = Array.IndexOf(args, key); if (at < 0 || at + 1 >= args.Length || args.Count(v => v == key) != 1) throw new ArgumentException("Missing/duplicate visual option."); return args[at + 1]; }
        string root = Read("--asset-root"), configuration = Read("--asset-config");
        bool cancel = args.Contains("--visual-cancel"), fixture = args.Contains("--visual-self-test");
        string? capture = args.Contains("--visual-capture") ? Read("--visual-capture") : null;
        if (cancel && (fixture || capture is not null) || capture is not null && (fixture || DisplayServer.GetName() == "headless") || cancel && DisplayServer.GetName() != "headless" || DisplayServer.GetName() == "headless" && !cancel && !fixture) throw new ArgumentException("Separate bounded visual modes required.");
        if (capture is not null)
        {
            capture = Path.GetFullPath(capture); string prefix = Path.GetFullPath(Path.Combine(ProjectSettings.GlobalizePath("res://"), "..", "..", "local-private")) + Path.DirectorySeparatorChar;
            if (!capture.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || Path.GetExtension(capture) != ".png" || File.Exists(capture) || !Directory.Exists(Path.GetDirectoryName(capture))) throw new ArgumentException("Fresh private PNG required.");
        }
        byte[] config = File.ReadAllBytes(configuration);
        if (Convert.ToHexString(SHA256.HashData(config)) != RetainedSkeletalDetailBundleReader.ConfigurationPin) throw new InvalidDataException("Configuration pin differs.");
        using var profile = File.OpenRead(Path.Combine(ProjectSettings.GlobalizePath("res://"), "..", "preview-profiles", "human-locomotion.json"));
        var bank = SkeletalAnimationBankProfile.Load(profile);
        var create = new SceneCreateObjectByCrcMessage(0xDEADBEEF00000001, .2f, BitConverter.Int32BitsToSingle(int.MinValue), .3f, 1.7f, -2, 7, 4, 0xAF1DC1A1, 255);
        var catalog = new ObjectTemplateCatalog(new[] { new ObjectTemplateCatalogEntry(create.TemplateCrc, RetainedSkeletalDetailBundleReader.ObjectPath, RetainedSkeletalDetailBundleReader.ObjectPin, RetainedSkeletalDetailBundleReader.AppearancePin) });
        var idle = new LogicalAnimationClipSelection("appearance/lat/all_m.lat", "emt_standing_idle_ambient_1_ag", "798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B");
        var reads = new Dictionary<string, int>(); int main = System.Environment.CurrentManagedThreadId, worker = main, closed = 0;
        var first = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var layer = new CanvasLayer(); AddChild(layer); var cancelButton = new Button { Text = "Cancel visual resource load", Position = new(24, 24) }; layer.AddChild(cancelButton);
        var request = new OwnedBackgroundAssetLoad<SceneCreatureResources>(token =>
        {
            worker = System.Environment.CurrentManagedThreadId;
            try
            {
                using var reader = new StringReader(System.Text.Encoding.UTF8.GetString(config)); using var overlay = ConfiguredTreOverlay.Open(root, reader);
                TreLookupResult Resolve(string path)
                {
                    token.ThrowIfCancellationRequested();
                    if (cancel && first.TrySetResult(true)) { token.WaitHandle.WaitOne(TimeSpan.FromSeconds(10)); token.ThrowIfCancellationRequested(); throw new InvalidOperationException("Cancellation timeout."); }
                    reads[path] = reads.GetValueOrDefault(path) + 1; return overlay.Lookup(path);
                }
                return SceneCreatureResourceReader.Load(create, catalog, idle, bank, Resolve, token);
            }
            finally { Volatile.Write(ref closed, 1); }
        });
        load = request; cancelButton.Pressed += request.Cancel;
        try
        {
            var transfer = request.TakeAsync(); if (cancel && await Task.WhenAny(first.Task, request.WorkerCompletion) == first.Task) request.Cancel();
            var prepared = await transfer; if (disposed || !IsInsideTree()) return; source = prepared;
            if (worker == main || main != System.Environment.CurrentManagedThreadId || Volatile.Read(ref closed) != 1 || reads.Values.Any(n => n != 1) || reads.Count != prepared.ExternalReads) throw new AssetPreviewException("Visual worker transfer/closure/read-once differs.");
            GD.Print($"PASS: visual original resources={source.Creature.Sources.Count} bytes={source.SourceBytes} external_reads={source.ExternalReads} worker_transfer=true archives_closed=true");
            GD.Print("visual_create=" + Convert.ToHexString(SceneObjectLifecycleCodec.EncodeCreate(source.Create)));
            foreach (var row in source.Creature.Sources.Values) GD.Print("visual_source=" + System.Text.Json.JsonSerializer.Serialize(row));
        }
        catch (OperationCanceledException)
        {
            if (disposed || !IsInsideTree()) return;
            if (Volatile.Read(ref closed) != 1 || visual is not null) throw new AssetPreviewException("Canceled visual published geometry.");
            GD.Print("PASS: visual canceled archives_closed=true geometry_publications=0"); DisposeOwned(); GetTree().Quit(); return;
        }
        finally { request.Dispose(); if (ReferenceEquals(load, request)) load = null; }
        layer.Free(); registry = new(8, 1); var handle = registry.Create(1, source!.Create)!.Handle;
        visual = SceneCreatureVisual.TryPublish(registry, handle, source, EnginePose, this) ?? throw new AssetPreviewException("Current visual failed publication.");
        foreach (var pair in source.Creature.AdditionalAnimations!) movement.Add(pair.Key, new(pair.Value.FrameRate, pair.Value.EndFrame, pair.Value.ProxyTimeScale));
        BuildControls();
        if (fixture || capture is not null)
        {
            VerifyFixture(DisplayServer.GetName() != "headless");
            if (capture is not null)
            {
                visual!.SeekIdle(45); visual.SetPlaying(false); Refresh();
                for (int frame = 0; frame < 3; frame++) await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                visual.Verify(true); using var image = GetViewport().GetTexture().GetImage(); if (image.SavePng(capture) != Error.Ok) throw new IOException("Visual capture failed.");
                GD.Print("visual_engine_origin=" + System.Text.Json.JsonSerializer.Serialize(new { visual.Root.Transform.Origin.X, visual.Root.Transform.Origin.Y, visual.Root.Transform.Origin.Z }));
                GD.Print($"PASS: visual GPU capture engine_pose=explicit frame={visual.IdleFrame} textures={visual.TextureCount} mip_readback=true source_transform_unapplied=true");
            }
            DisposeOwned(); GD.Print("PASS: visual teardown roots_released=true resources_disposed=true loader_disposed=true"); GetTree().Quit(); return;
        }
        focused = GetWindow().HasFocus(); visual.SetFocused(focused); active = true;
    }
    private void BuildControls()
    {
        var environment = new Godot.Environment { BackgroundMode = Godot.Environment.BGMode.Color, BackgroundColor = new Color("#14202e") }; resources.Add(environment); AddChild(new WorldEnvironment { Environment = environment });
        var bounds = visual!.Root.Transform * visual.Bounds;
        var camera = new PreviewOrbitCamera { Current = true, Fov = 50, VOffset = .55f }; AddChild(camera); camera.Configure(bounds.GetCenter(), Math.Max(bounds.Size.Length() * .85f, 1));
        var layer = new CanvasLayer(); AddChild(layer); var panel = new ColorRect { Color = new(.02f, .04f, .08f, .92f), OffsetRight = 1280, OffsetBottom = 155 }; layer.AddChild(panel);
        panel.AddChild(new Label { Text = "STARDUST | OWNED SCENE CREATURE VISUAL", Position = new(24, 12) }); status = new Label { Position = new(24, 44) }; panel.AddChild(status);
        void Button(string text, int x, Action action) { var button = new Button { Text = text, Position = new(x, 78), Size = new(105, 30) }; panel.AddChild(button); button.Pressed += () => Guard(action); }
        Button("Idle", 24, () => { selectedClip = "idle"; visual!.ResumeIdle(); }); Button("Walk", 140, () => SelectMovement("walk")); Button("Run", 256, () => SelectMovement("run"));
        Button("Play / pause", 372, () => { playing = !playing; visual!.SetPlaying(playing); foreach (var clock in movement.Values) clock.SetPlaying(playing); });
        Button("Show / hide", 500, () => visual!.SetVisible(!visual.Root.Visible)); Button("Reset idle", 630, () => { selectedClip = "idle"; visual!.SeekIdle(0); });
        panel.AddChild(new Label { Text = "Explicit caller engine pose | authored MAIN | source-create conversion deferred | drag to orbit, scroll to zoom, R reset", Position = new(24, 120) }); Refresh();
    }
    private void SelectMovement(string key) { selectedClip = key; movement[key].SetPlaying(playing); visual!.SetMovement(key, movement[key].Frame, 0); Refresh(); }
    private void Refresh() { if (status is not null && visual is not null) status.Text = $"{selectedClip} | idle frame {visual.IdleFrame:0.##} | textures {visual.TextureCount} | strict registry revision | {(visual.IsRetired ? "retired" : "current")}"; }
    private void Guard(Action action) { if (disposed) return; try { action(); Refresh(); } catch (Exception error) { active = false; GD.PushError("Creature visual boundary failed: " + error.GetType().Name); GetTree().Quit(1); } }
    private void VerifyFixture(bool gpu)
    {
        var current = visual!; string retained = current.Fingerprint(gpu: gpu)!; int allocations = SceneCreatureVisual.Allocations;
        for (int tick = 0; tick < 128; tick++)
        {
            current.Tick(.017); current.SetVisible(tick % 2 == 0); current.Verify(gpu);
            if (current.Fingerprint(gpu: gpu) != retained || SceneCreatureVisual.Allocations != allocations) throw new AssetPreviewException("Idle/visibility recreated visual resources.");
        }
        current.SetVisible(true); current.SetFocused(false); double held = current.IdleFrame; current.Tick(.2); if (current.IdleFrame != held) throw new AssetPreviewException("Visual focus gate failed.");
        current.SetFocused(true); current.SetPlaying(false); current.Tick(.2); if (current.IdleFrame != held) throw new AssetPreviewException("Visual pause failed."); current.SetPlaying(true);
        foreach (string key in new[] { "walk", "run" })
            for (int tick = 0; tick < 48; tick++)
            {
                current.SetMovement(key, tick % source!.Creature.AdditionalAnimations![key].EndFrame, .017); current.Verify(gpu);
                if (current.Fingerprint(gpu: gpu) != retained) throw new AssetPreviewException("Movement replaced visual resources.");
            }
        current.ResumeIdle(); current.SeekIdle(0); current.SetEngineTransform(Transform3D.Identity); current.Verify(gpu); current.SetEngineTransform(EnginePose);
        if (current.Fingerprint(gpu: gpu) != retained) throw new AssetPreviewException("Explicit placement replaced resources.");
        foreach (string transition in new[] { "finalize", "contain", "replace", "destroy", "reset", "close" })
        {
            var owner = new SceneObjectRegistry(8, 1); var handle = owner.Create(1, source!.Create)!.Handle;
            using var candidate = SceneCreatureVisual.TryPublish(owner, handle, source, EnginePose, this)!;
            ulong root = candidate.Root.GetInstanceId();
            if (transition == "finalize") owner.EndBaselines(1, new(source.Create.ObjectId));
            if (transition == "contain") owner.Contain(1, new(source.Create.ObjectId, 0, 1));
            if (transition == "replace") owner.Replace(1, source.Create);
            if (transition == "destroy") owner.Destroy(1, new(source.Create.ObjectId, 0));
            if (transition == "reset") owner.Reset(); if (transition == "close") owner.Close();
            if (candidate.Tick(0) || !candidate.IsRetired || GodotObject.InstanceFromId(root) is not null) throw new AssetPreviewException("Stale revision visual survived.");
            int before = SceneCreatureVisual.Allocations;
            if (SceneCreatureVisual.TryPublish(owner, handle, source, EnginePose, this) is not null || SceneCreatureVisual.Allocations != before) throw new AssetPreviewException("Stale preflight allocated.");
            var snapshot = owner.Find(source.Create.ObjectId);
            if (snapshot is not null) { using var fresh = SceneCreatureVisual.TryPublish(owner, snapshot.Handle, source, EnginePose, this); if (fresh is null || !fresh.Verify(gpu)) throw new AssetPreviewException("Explicit new revision failed publication."); }
        }
        var stagedOwner = new SceneObjectRegistry(2, 1); var stagedHandle = stagedOwner.Create(1, source!.Create)!.Handle; int disposedBefore = SceneCreatureVisual.Disposals;
        var stale = SceneCreatureVisual.TryPublish(stagedOwner, stagedHandle, source, EnginePose, this, beforePublication: () => stagedOwner.EndBaselines(1, new(source.Create.ObjectId)));
        if (stale is not null || SceneCreatureVisual.Disposals != disposedBefore + 1) throw new AssetPreviewException("Stale staged allocation leaked/published.");
        bool threadRejected = false;
        try { Task.Run(() => current.Tick(0)).GetAwaiter().GetResult(); } catch (InvalidOperationException) { threadRejected = true; }
        if (!threadRejected || current.Fingerprint(gpu: gpu) != retained) throw new AssetPreviewException("Wrong-thread update was not rejected atomically.");
        var attachedOwner = new SceneObjectRegistry(2, 1); var attachedHandle = attachedOwner.Create(1, source.Create)!.Handle;
        ulong stagedRoot = 0;
        void InvalidateOnAttach(Node node)
        {
            if (node is Node3D root)
            {
                if (root.Visible) throw new AssetPreviewException("Staged root became visible before final guard.");
                stagedRoot = root.GetInstanceId(); attachedOwner.EndBaselines(1, new(source.Create.ObjectId));
            }
        }
        ChildEnteredTree += InvalidateOnAttach;
        try { if (SceneCreatureVisual.TryPublish(attachedOwner, attachedHandle, source, EnginePose, this) is not null) throw new AssetPreviewException("Post-attach stale root published."); }
        finally { ChildEnteredTree -= InvalidateOnAttach; }
        if (stagedRoot == 0 || GodotObject.InstanceFromId(stagedRoot) is not null) throw new AssetPreviewException("Post-attach stale resources survived.");
        int beforeMismatch = SceneCreatureVisual.Allocations;
        var wrong = source with { Create = source.Create with { QuaternionY = 0 } };
        var token = registry!.Find(source.Create.ObjectId)!.Handle;
        if (SceneCreatureVisual.TryPublish(registry, token, wrong, EnginePose, this) is not null || SceneCreatureVisual.Allocations != beforeMismatch) throw new AssetPreviewException("Exact create bits mismatch allocated.");
        bool rejected = false; try { current.SetMovement("walk", double.NaN, 0); } catch (ArgumentException) { rejected = true; } if (!rejected || current.Fingerprint(gpu: gpu) != retained) throw new AssetPreviewException("Invalid movement mutated resources.");
        GD.Print($"PASS: visual idle128 movement96 visibility focus_pause placement retained={retained} mip_readback={gpu} lifecycle6 stale_preflight=true staged_cleanup=true exact_create_bits=true new_revision_explicit=true wrong_thread_rejected=true post_attach_cleanup=true");
    }
    public override void _Process(double delta)
    {
        if (!active || disposed) return;
        Guard(() => { if (selectedClip == "idle") visual!.Tick(delta); else { var clock = movement[selectedClip]; clock.Advance(Math.Min(delta, .25)); visual!.SetMovement(selectedClip, clock.Frame, Math.Min(delta, .25)); } });
    }
    public override void _Notification(int what)
    {
        if (what == NotificationWMWindowFocusIn || what == NotificationWMWindowFocusOut) Guard(() => { focused = what == NotificationWMWindowFocusIn; visual?.SetFocused(focused); foreach (var clock in movement.Values) clock.SetFocused(focused); });
    }
    private void DisposeOwned() { if (disposed) return; disposed = true; active = false; load?.Dispose(); load = null; visual?.Dispose(); visual = null; registry?.Close(); registry = null; source = null; movement.Clear(); foreach (var resource in resources) resource.Dispose(); resources.Clear(); }
    public override void _ExitTree() => DisposeOwned();
}
