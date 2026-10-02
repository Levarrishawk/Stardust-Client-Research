using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Stardust.Core.Assets;

/// <summary>Own lifecycle for modern original-clip event diagnostics; no scene clock or effect delivery.</summary>
public partial class AnimationEventDiagnosticPreview : Node
{
    private OwnedBackgroundAssetLoad<CreatureAppearanceDescriptorBundle>? load;
    private CreatureAppearanceDescriptorBundle? bundle;
    private SkeletalMeshPreview? mesh;
    private AnimationEventPlayback? playback;
    private AnimationEventDiagnosticLog? log;
    private Label? status, events;
    private readonly List<Resource> resources = new();
    private string selected = "walk";
    private bool active, disposed, windowFocused = true, focusAllowed = true;
    private Button? playButton;
    internal const string ObjectPath = "object/creature/player/shared_human_male.iff";
    internal const string ObjectPin = "E0B61445E8A85CCF0AC5937F0F9CBCA8711718AFEE63384F30970D5AEE120243";
    internal const string AppearancePin = "951FCE8C2FFC0EB318C751026D24D8C9BF17263D706EB3C93D015867EC801A9C";
    internal const string ConfigurationPin = "22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524";

    internal async Task InitializeAsync(string[] args)
    {
        string Read(string key)
        {
            int at = Array.IndexOf(args, key);
            if (at < 0 || at + 1 >= args.Length || args.Count(v => v == key) != 1) throw new ArgumentException("Missing/duplicate animation diagnostic option.");
            return args[at + 1];
        }
        string root = Read("--asset-root"), configuration = Read("--asset-config");
        bool cancel = args.Contains("--animation-event-cancel"), fixture = args.Contains("--animation-event-self-test");
        string? capture = args.Contains("--animation-event-capture") ? Read("--animation-event-capture") : null;
        string captureClip = args.Contains("--animation-event-capture-clip") ? Read("--animation-event-capture-clip") : "walk";
        if (captureClip is not ("walk" or "run") || (capture is not null && (cancel || fixture || DisplayServer.GetName() == "headless")))
            throw new ArgumentException("Capture requires a separate bounded GPU preview and explicit walk/run clip.");
        if (capture is not null)
        {
            capture = Path.GetFullPath(capture);
            string privateRoot = Path.GetFullPath(Path.Combine(ProjectSettings.GlobalizePath("res://"), "..", "..", "local-private")) + Path.DirectorySeparatorChar;
            if (!capture.StartsWith(privateRoot, StringComparison.OrdinalIgnoreCase) || !string.Equals(Path.GetExtension(capture), ".png", StringComparison.OrdinalIgnoreCase) || File.Exists(capture) || !Directory.Exists(Path.GetDirectoryName(capture)))
                throw new ArgumentException("Capture needs a fresh PNG under the checkout private directory.");
        }
        if (cancel && DisplayServer.GetName() != "headless") throw new ArgumentException("Cancellation fixture requires headless mode.");
        byte[] config = File.ReadAllBytes(configuration);
        if (Convert.ToHexString(SHA256.HashData(config)) != ConfigurationPin) throw new InvalidDataException("Animation diagnostic configuration identity mismatch.");
        string profilePath = Path.GetFullPath(Path.Combine(ProjectSettings.GlobalizePath("res://"), "..", "preview-profiles", "human-locomotion.json"));
        using var profile = File.OpenRead(profilePath);
        var selections = SkeletalAnimationBankProfile.Load(profile);
        if (selections.Count != 2 || !selections.ContainsKey("walk") || !selections.ContainsKey("run") ||
            selections["walk"].ClipSha256 != "FF86567A4CDBFC1EB574C5C8FC399144A37DAD218AA49DE0070AC9F1F19F65A6" ||
            selections["run"].ClipSha256 != "72D5842D2959D8438884B44F4220F3E323D221A745D849CB8726E905A8AAF2F4")
            throw new InvalidDataException("Original locomotion profile identities mismatch.");
        int mainThread = System.Environment.CurrentManagedThreadId, workerThread = 0, closed = 0;
        var first = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var reads = new Dictionary<string, int>(StringComparer.Ordinal);
        var request = new OwnedBackgroundAssetLoad<CreatureAppearanceDescriptorBundle>(token =>
        {
            workerThread = System.Environment.CurrentManagedThreadId;
            try
            {
                token.ThrowIfCancellationRequested(); using var reader = new StringReader(Encoding.UTF8.GetString(config));
                using var overlay = ConfiguredTreOverlay.Open(root, reader);
                TreLookupResult Resolve(string path)
                {
                    token.ThrowIfCancellationRequested();
                    if (cancel && first.TrySetResult(true))
                    {
                        token.WaitHandle.WaitOne(TimeSpan.FromSeconds(10)); token.ThrowIfCancellationRequested();
                        throw new InvalidOperationException("Animation cancellation fixture timed out.");
                    }
                    reads[path] = reads.GetValueOrDefault(path) + 1;
                    return overlay.Lookup(path);
                }
                var result = CreatureAppearanceDescriptorBundleReader.Load(Resolve, ObjectPath, ObjectPin, AppearancePin,
                    limits: new(MaximumResources: 128, MaximumSourceBytes: 32 * 1024 * 1024),
                    loadSkeletons: true, loadSkinSources: true, loadGeometry: true, loadLogicalTables: true, loadMaterialDefaults: true,
                    animationSelection: new("appearance/lat/all_m.lat", "emt_standing_idle_ambient_1_ag", "798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B"),
                    prepareSelectedAnimation: true, additionalAnimationSelections: selections);
                token.ThrowIfCancellationRequested(); return result;
            }
            finally { Volatile.Write(ref closed, 1); }
        });
        load = request;
        try
        {
            Task<CreatureAppearanceDescriptorBundle> transfer = request.TakeAsync();
            if (cancel && await Task.WhenAny(first.Task, request.WorkerCompletion) == first.Task) request.Cancel();
            var transferred = await transfer;
            if (disposed || !GodotObject.IsInstanceValid(this) || !IsInsideTree()) return;
            bundle = transferred;
            if (System.Environment.CurrentManagedThreadId != mainThread || workerThread == mainThread || Volatile.Read(ref closed) != 1 || reads.Values.Any(n => n != 1))
                throw new AssetPreviewException("Animation worker transfer/archive/read-once ownership mismatch.");
            GD.Print($"PASS: event preview original object/SAT idle+walk/run sources={bundle.Sources.Count} bytes={bundle.Sources.Values.Sum(s => (long)s.Bytes)} read_once=true worker_transfer=true archives_closed=true");
        }
        catch (OperationCanceledException)
        {
            if (disposed || !GodotObject.IsInstanceValid(this) || !IsInsideTree()) return;
            if (!cancel || Volatile.Read(ref closed) != 1 || mesh is not null) throw new AssetPreviewException("Animation cancellation ownership mismatch.");
            GD.Print("PASS: event preview canceled at first lookup; archive_closed=true geometry_publications=0");
            DisposeOwned(); GetTree().Quit(); return;
        }
        finally { request.Dispose(); if (ReferenceEquals(load, request)) load = null; }
        mesh = new(bundle!); AddChild(mesh.Root); mesh.VerifyBoundary(DisplayServer.GetName() != "headless");
        if (mesh.TextureCount == 0 || bundle!.MaterialBundle is null) throw new AssetPreviewException("Authored MAIN textures were not prepared/uploaded.");
        int mainTextures = bundle.MaterialBundle.Materials.Values.SelectMany(m => m.References.Textures).Where(t => t.Tag == 0x4D41494E).Select(t => t.AssetPath).Distinct(StringComparer.Ordinal).Count();
        GD.Print($"PASS: event preview authored MAIN bindings={bundle.MaterialBundle.Materials.Count} distinct_MAIN={mainTextures} texture_uploads={mesh.TextureCount} uploaded_bytes={mesh.TextureBytes} gpu_readback={DisplayServer.GetName() != "headless"}");
        var clip = bundle!.AdditionalAnimations!["walk"];
        playback = new(clip.Locomotion!, clip.FrameRate); log = new(playback, 32);
        mesh.SetClipFrame("walk", 0); mesh.SetMovementFrame("walk", playback.CurrentFrame, 0);
        if (fixture)
        {
            VerifyFixture(); DisposeOwned();
            GD.Print("PASS: event preview teardown; updates_disabled=true geometry_resources_released=true loader_disposed=true");
            GetTree().Quit(); return;
        }
        if (DisplayServer.GetName() == "headless") throw new ArgumentException("Headless animation preview requires a fixture or cancellation flag.");
        BuildControls(); windowFocused = GetWindow().HasFocus(); ApplyFocus(); RefreshLabels();
        if (capture is not null)
        {
            if (captureClip != selected) SelectClip(captureClip);
            playback.SetFocused(true);
            ushort firstFrame = bundle.AdditionalAnimations![selected].Locomotion!.Messages[0].Frames[0];
            Tick((double)(playback.EndFrame + firstFrame + 2) / playback.FrameRate); playback.Pause(); RefreshLabels();
            for (int frame = 0; frame < 3; frame++) await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            mesh.VerifyBoundary(true);
            using Image image = GetViewport().GetTexture().GetImage();
            if (image.SavePng(capture) != Error.Ok) throw new IOException("Animation diagnostic capture failed.");
            GD.Print($"PASS: event preview GPU capture clip={selected} frame={playback.CurrentFrame:0.##} generation={playback.Generation} events={log.GenerationDeliveredTotal} MAIN_textures={mesh.TextureCount} gpu_readback=true");
            DisposeOwned(); GetTree().Quit(); return;
        }
        active = true;
    }

    private void BuildControls()
    {
        var environment = new Godot.Environment { BackgroundMode = Godot.Environment.BGMode.Color, BackgroundColor = new Color("#14202e"), AmbientLightSource = Godot.Environment.AmbientSource.Color, AmbientLightColor = Colors.White, AmbientLightEnergy = .65f };
        resources.Add(environment); AddChild(new WorldEnvironment { Environment = environment });
        AddChild(new DirectionalLight3D { RotationDegrees = new(-35, 25, 0), LightEnergy = .8f });
        var camera = new PreviewOrbitCamera { Current = true, Fov = 50, VOffset = .55f }; AddChild(camera);
        camera.Configure(mesh!.Bounds.GetCenter(), Math.Max(mesh.Bounds.Size.Length() * .85f, 1));
        var layer = new CanvasLayer(); AddChild(layer);
        var panel = new ColorRect { Color = new(.02f, .04f, .08f, .92f), OffsetRight = 1280, OffsetBottom = 240 }; layer.AddChild(panel);
        panel.AddChild(new Label { Text = "STARDUST | ORIGINAL WALK / RUN EVENT DIAGNOSTICS", Position = new(24, 12) });
        status = new Label { Position = new(24, 45) }; panel.AddChild(status);
        events = new Label { Position = new(24, 120) }; panel.AddChild(events);
        void Button(string text, float x, Action action)
        {
            var button = new Button { Text = text, Position = new(x, 78), Size = new(110, 30) };
            panel.AddChild(button); button.Pressed += () => Guard(action);
        }
        Button("Walk", 24, () => SelectClip("walk")); Button("Run", 145, () => SelectClip("run"));
        playButton = new Button { Text = "Pause", Position = new(266, 78), Size = new(110, 30) }; panel.AddChild(playButton);
        playButton.Pressed += () => Guard(() => { if (playback!.Playing) playback.Pause(); else playback.Resume(); RefreshLabels(); });
        Button("Restart", 387, () => { playback!.Restart(); log!.SynchronizeGeneration(); mesh!.SetClipFrame(selected, 0); RefreshLabels(); });
        Button("Clear history", 508, () => { log!.ClearRecent(); RefreshLabels(); });
        var focus = new CheckBox { Text = "Allow playback (window focus also required)", ButtonPressed = true, Position = new(650, 78) }; panel.AddChild(focus);
        focus.Toggled += allowed => Guard(() => { focusAllowed = allowed; ApplyFocus(); RefreshLabels(); });
    }
    private void Guard(Action action)
    {
        if (disposed) return;
        try { action(); }
        catch (Exception failure) { active = false; GD.PushError("Animation diagnostic boundary failed: " + failure.GetType().Name); GetTree().Quit(1); }
    }
    private void SelectClip(string key)
    {
        var clip = bundle!.AdditionalAnimations![key];
        playback!.ReplaceClip(clip.Locomotion!, clip.FrameRate); selected = key; log!.SynchronizeGeneration();
        // Prime the same key so SetMovementFrame(...,0) samples directly rather than starting a crossfade.
        mesh!.SetClipFrame(key, 0); mesh.SetMovementFrame(key, playback.CurrentFrame, 0); RefreshLabels();
    }
    private AnimationEventPlaybackBatch Tick(double seconds)
    {
        var batch = playback!.Advance(seconds);
        if (!playback.IsCurrent(batch)) throw new AssetPreviewException("Stale main-frame event batch.");
        if (batch.Count > 0) log!.Deliver(batch);
        mesh!.SetMovementFrame(selected, playback.CurrentFrame, 0); RefreshLabels(); return batch;
    }
    private void ApplyFocus() => playback?.SetFocused(windowFocused && focusAllowed);
    private void RefreshLabels()
    {
        if (playback is null || log is null) return;
        if (log.Generation != playback.Generation || log.Recent.Any(e => e.Generation != playback.Generation)) throw new AssetPreviewException("Stale main-frame diagnostic history.");
        if (status is not null) status.Text = $"{selected} | frame {playback.CurrentFrame:0.##}/{playback.EndFrame} | cycle {playback.CompletedLoops} | generation {playback.Generation} | {(playback.Playing ? "playing" : "paused")} | focus {playback.Focused} | delivered {log.GenerationDeliveredTotal} (total {log.DeliveredTotal})";
        if (playButton is not null) playButton.Text = playback.Playing ? "Pause" : "Resume";
        if (events is not null) events.Text = "Recent authored occurrences:\n" + (log.Count == 0 ? "none" : string.Join("\n", log.Recent.TakeLast(6).Chunk(3).Select(row => string.Join(" | ", row.Select(e => $"{e.Occurrence.Name} @ {e.Occurrence.Frame}, cycle {e.Occurrence.Cycle}"))))) + "\nModern time policy | no root translation, audio or effects | drag to orbit, scroll to zoom";
    }
    public override void _Process(double delta)
    {
        if (!active || disposed) return;
        Guard(() => { if (!double.IsFinite(delta) || delta < 0) throw new AssetPreviewException("Invalid event preview elapsed time."); Tick(Math.Min(delta, .25)); });
    }
    public override void _Notification(int what)
    {
        if (what == NotificationWMWindowFocusIn || what == NotificationWMWindowFocusOut)
            Guard(() => { windowFocused = what == NotificationWMWindowFocusIn; ApplyFocus(); RefreshLabels(); });
    }
    private void VerifyFixture()
    {
        playback!.SetFocused(true); long total = 0; int uploads = 0;
        string resourcesBefore = mesh!.GeometryFingerprint(false); var transformBefore = mesh.Root.Transform;
        foreach (string key in new[] { "walk", "run" })
        {
            if (key != selected) SelectClip(key);
            var frames = key == "walk" ? new ushort[] { 7, 23 } : new ushort[] { 6, 17 };
            var expected = Enumerable.Range(0, 3).SelectMany(c => frames.Select((f, i) => new AnimationEventOccurrence(c, 0, i, "event_footstep", f))).ToArray();
            var delivered = new List<AnimationEventOccurrence>(); AnimationEventPlaybackBatch? last = null;
            for (int tick = 0; tick < playback.EndFrame * 3; tick++)
            {
                var batch = Tick(1.0 / playback.FrameRate); delivered.AddRange(batch); if (batch.Count > 0) last = batch;
                mesh.VerifyBoundary(false); uploads++;
                if (tick % 11 == 0)
                {
                    string observed = mesh.GeometryFingerprint(); float displacement = mesh.MaximumDisplacement;
                    mesh.SetClipFrame(key, playback.CurrentFrame);
                    if (mesh.GeometryFingerprint() != observed || mesh.MaximumDisplacement != displacement) throw new AssetPreviewException("Event frame differs from direct prepared sample.");
                }
            }
            if (!delivered.SequenceEqual(expected) || log!.GenerationDeliveredTotal != 6 || playback.CompletedLoops != 3 || playback.CurrentFrame != 0)
                throw new AssetPreviewException("Original diagnostic six-event/cycle boundary mismatch.");
            long held = log.DeliveredTotal;
            try { log.Deliver(last!); throw new AssetPreviewException("Repeated original batch accepted."); } catch (InvalidDataException) { }
            log.ClearRecent(); try { log.Deliver(last!); throw new AssetPreviewException("Cleared original batch accepted."); } catch (InvalidDataException) { }
            if (log.DeliveredTotal != held) throw new AssetPreviewException("Rejected diagnostic delivery changed total.");
            total += 6; GD.Print($"PASS: original event diagnostic {key} ticks={playback.EndFrame * 3} cycles=3 footsteps=6 duplicate_after_clear_rejected=true");
        }
        playback.Pause(); double phase = playback.UnwrappedPhase; long deliveredBefore = log!.DeliveredTotal;
        Tick(60); if (playback.UnwrappedPhase != phase || log.DeliveredTotal != deliveredBefore) throw new AssetPreviewException("Paused preview caught up.");
        playback.Resume(); playback.SetFocused(false); Tick(60); if (playback.UnwrappedPhase != phase) throw new AssetPreviewException("Unfocused preview caught up.");
        playback.SetFocused(true); var old = playback.Advance(.2); playback.Restart(); log.SynchronizeGeneration();
        try { log.Deliver(old); throw new AssetPreviewException("Stale restart batch accepted."); } catch (InvalidDataException) { }
        SelectClip("walk");
        if (total != 12 || mesh.GeometryFingerprint(false) != resourcesBefore || mesh.Root.Transform != transformBefore || mesh.Surfaces == 0 || mesh.Vertices == 0)
            throw new AssetPreviewException("Diagnostic geometry retention/root boundary mismatch.");
        GD.Print($"PASS: event preview deterministic frame publication uploads={uploads} current_generation_only=true pause_focus_restart=true resources_retained=true root_translation=false");
    }
    private void DisposeOwned()
    {
        if (disposed) return; disposed = true; active = false; load?.Dispose(); load = null;
        if (mesh is not null)
        {
            if (GodotObject.IsInstanceValid(mesh.Root)) { if (mesh.Root.GetParent() == this) RemoveChild(mesh.Root); mesh.Root.Free(); }
            mesh.Dispose(); mesh = null;
        }
        foreach (var resource in resources) resource.Dispose(); resources.Clear();
        playback = null; log = null; bundle = null; status = null; events = null; playButton = null;
    }
    public override void _ExitTree() => DisposeOwned();
}
