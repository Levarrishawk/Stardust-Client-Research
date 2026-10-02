using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Stardust.Core.Assets;
using Godot;

/// <summary>Manual retained original-human selection; no automatic LOD policy.</summary>
public partial class SkeletalRetainedDetailPreview : Node
{
    private OwnedBackgroundAssetLoad<RetainedSkeletalDetailSet>? load;
    private RetainedSkeletalDetailSet? bundles;
    private readonly Dictionary<int, SkeletalMeshPreview> meshes = new();
    private readonly Dictionary<int, double> sampledFrames = new();
    private AnimationPreviewClock? clock;
    private readonly List<Resource> resources = new();
    private Label? status;
    private HSlider? scrub;
    private CheckBox? play, focus;
    private Button? twoButton, threeButton, reset;
    private bool active, disposed, windowFocused = true, focusAllowed = true;
    private int selected = 2;
    private PreviewOrbitCamera? camera;
    private Aabb sphereBounds;
    private readonly SkeletalDetailSelector selector = new(new(new[] { 240d, 120, 60 }, .1));
    private bool automatic;
    private int? fixtureProjectionHeight;
    private CheckBox? autoControl;
    private readonly Dictionary<int, Button> detailButtons = new();

    internal async Task InitializeAsync(string[] args)
    {
        string Read(string key)
        {
            int at = Array.IndexOf(args, key);
            if (at < 0 || at + 1 >= args.Length || args.Count(v => v == key) != 1) throw new ArgumentException("Missing/duplicate retained-detail option.");
            return args[at + 1];
        }
        string root = Read("--asset-root"), configuration = Read("--asset-config");
        bool cancel = args.Contains("--retained-cancel"), fixture = args.Contains("--retained-self-test");
        string? capture = args.Contains("--retained-capture") ? Read("--retained-capture") : null;
        string detail = args.Contains("--retained-detail") ? Read("--retained-detail") : "2";
        if (detail is not ("0" or "1" or "2" or "3") || (cancel && (fixture || capture is not null)) || (capture is not null && (fixture || DisplayServer.GetName() == "headless")))
            throw new ArgumentException("Retained preview requires explicit detail 0..3 and separate fixture/capture/cancel modes.");
        if (fixture && DisplayServer.GetName() == "headless") fixtureProjectionHeight = 720;
        selected = int.Parse(detail); automatic = args.Contains("--retained-auto");
        if (cancel && DisplayServer.GetName() != "headless") throw new ArgumentException("Cancellation requires headless mode.");
        if (DisplayServer.GetName() == "headless" && !cancel && !fixture) throw new ArgumentException("Headless inspection requires a bounded fixture.");
        if (capture is not null)
        {
            capture = Path.GetFullPath(capture);
            string prefix = Path.GetFullPath(Path.Combine(ProjectSettings.GlobalizePath("res://"), "..", "..", "local-private")) + Path.DirectorySeparatorChar;
            if (!capture.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || Path.GetExtension(capture) != ".png" || File.Exists(capture) || !Directory.Exists(Path.GetDirectoryName(capture)))
                throw new ArgumentException("Capture must be a fresh private PNG.");
        }
        byte[] config = File.ReadAllBytes(configuration);
        if (Convert.ToHexString(SHA256.HashData(config)) != RetainedSkeletalDetailBundleReader.ConfigurationPin) throw new InvalidDataException("Configuration identity mismatch.");
        var loading = new CanvasLayer(); AddChild(loading);
        var cancelButton = new Button { Text = "Cancel retained detail loading", Position = new(24, 24) }; loading.AddChild(cancelButton);
        int mainThread = System.Environment.CurrentManagedThreadId, workerThread = 0, closed = 0;
        var first = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var reads = new Dictionary<string, int>(StringComparer.Ordinal);
        var request = new OwnedBackgroundAssetLoad<RetainedSkeletalDetailSet>(token =>
        {
            workerThread = System.Environment.CurrentManagedThreadId;
            try
            {
                token.ThrowIfCancellationRequested(); using var reader = new StringReader(System.Text.Encoding.UTF8.GetString(config));
                using var overlay = ConfiguredTreOverlay.Open(root, reader);
                TreLookupResult Resolve(string path)
                {
                    token.ThrowIfCancellationRequested();
                    if (cancel && first.TrySetResult(true))
                    {
                        token.WaitHandle.WaitOne(TimeSpan.FromSeconds(10)); token.ThrowIfCancellationRequested();
                        throw new InvalidOperationException("Retained cancellation fixture timed out.");
                    }
                    reads[path] = reads.GetValueOrDefault(path) + 1; return overlay.Lookup(path);
                }
                return RetainedSkeletalDetailBundleReader.LoadAll(Resolve, token);
            }
            finally { Volatile.Write(ref closed, 1); }
        });
        load = request; cancelButton.Pressed += request.Cancel;
        try
        {
            var transfer = request.TakeAsync();
            if (cancel && await Task.WhenAny(first.Task, request.WorkerCompletion) == first.Task) request.Cancel();
            var prepared = await transfer;
            if (disposed || !GodotObject.IsInstanceValid(this) || !IsInsideTree()) return;
            bundles = prepared;
            if (System.Environment.CurrentManagedThreadId != mainThread || workerThread == mainThread || Volatile.Read(ref closed) != 1 || reads.Values.Any(n => n != 1) || reads.Count != prepared.ExternalReads)
                throw new AssetPreviewException("Retained worker/cache/archive transfer differs.");
            GD.Print($"PASS: retained union sources={prepared.Sources.Count} bytes={prepared.SourceBytes} external_reads={prepared.ExternalReads} read_once=true archives_closed=true worker_transfer=true");
            foreach (var row in prepared.Sources.Values) GD.Print("retained_source=" + System.Text.Json.JsonSerializer.Serialize(row));
        }
        catch (OperationCanceledException)
        {
            if (disposed || !GodotObject.IsInstanceValid(this) || !IsInsideTree()) return;
            if (Volatile.Read(ref closed) != 1 || meshes.Count != 0) throw new AssetPreviewException("Retained cancellation ownership differs.");
            GD.Print(cancel ? "PASS: retained canceled at first lookup; archives_closed=true geometry_publications=0" : "PASS: retained user canceled loading; archives_closed=true geometry_publications=0"); DisposeOwned(); GetTree().Quit(); return;
        }
        finally { request.Dispose(); if (ReferenceEquals(load, request)) load = null; }
        loading.Free();
        foreach (int level in new[] { 0, 1, 2, 3 })
        {
            var bundle = bundles!.Details[level];
            var mesh = new SkeletalMeshPreview(bundle, meshDetail: level, specularPreview: level < 2, vertexLitOnly: level >= 2); meshes.Add(level, mesh); AddChild(mesh.Root);
            mesh.SetOriginalFrame(0); sampledFrames.Add(level, 0); mesh.Root.Visible = level == selected;
            if (mesh.Surfaces != (level < 2 ? 5 : 2) || mesh.Vertices != new[] { 1790, 1256, 542, 163 }[level] || mesh.Triangles != new[] { 2810, 1904, 756, 190 }[level]) throw new AssetPreviewException("Retained selected geometry counts differ.");
            mesh.VerifyBoundary(DisplayServer.GetName() != "headless");
            GD.Print($"PASS: retained detail={level} surfaces={mesh.Surfaces} vertices={mesh.Vertices} triangles={mesh.Triangles} skeleton_detail=0 texture_uploads={mesh.TextureCount} mip_readback={DisplayServer.GetName() != "headless"}");
        }
        var animation = bundles!.Details[2].PreparedAnimation!; clock = new(animation.FrameRate, animation.EndFrame, animation.ProxyTimeScale); clock.SetPlaying(true);
        BuildControls(); if (automatic) UpdateAutomatic();
        if (fixture || capture is not null)
        {
            VerifyFixture(DisplayServer.GetName() != "headless");
            if (capture is not null)
            {
                Seek(animation.EndFrame * .375); ManualSelect(int.Parse(detail)); clock.SetPlaying(false); Refresh();
                if (selected >= 2) await SkeletalMaterialPreview.VerifyVertexOnlyGpuBoundary(this);
                else await SkeletalSpecularPreview.VerifyGpuBoundary(this, vertexVaryings: true);
                for (int frame = 0; frame < 3; frame++) await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                foreach (var mesh in meshes.Values) mesh.VerifyBoundary(true);
                using var image = GetViewport().GetTexture().GetImage(); if (image.SavePng(capture) != Error.Ok) throw new IOException("Retained capture failed.");
                GD.Print($"PASS: retained GPU capture detail={selected} frame={clock.Frame:0.##} current_hidden_phase=true mip_readback=true");
            }
            DisposeOwned(); GD.Print("PASS: retained teardown updates_disabled=true roots_released=true resources_disposed=true loader_disposed=true"); GetTree().Quit(); return;
        }
        windowFocused = GetWindow().HasFocus(); ApplyFocus(); active = true; Refresh();
    }

    private void BuildControls()
    {
        var environment = new Godot.Environment { BackgroundMode = Godot.Environment.BGMode.Color, BackgroundColor = new Color("#14202e"), AmbientLightSource = Godot.Environment.AmbientSource.Color, AmbientLightColor = Colors.White, AmbientLightEnergy = .65f };
        resources.Add(environment); AddChild(new WorldEnvironment { Environment = environment });
        camera = new PreviewOrbitCamera { Current = true, Fov = 50, VOffset = .55f, KeepAspect = Camera3D.KeepAspectEnum.Height }; AddChild(camera);
        sphereBounds = meshes.Values.Select(m => m.Bounds).Aggregate((a, b) => a.Merge(b)); camera.Configure(sphereBounds.GetCenter(), Math.Max(sphereBounds.Size.Length() * .85f, 1));
        var layer = new CanvasLayer(); AddChild(layer); var panel = new ColorRect { Color = new(.02f, .04f, .08f, .92f), OffsetRight = 1280, OffsetBottom = 155 }; layer.AddChild(panel);
        panel.AddChild(new Label { Text = "STARDUST | ORIGINAL HUMAN | RETAINED DETAILS 0 / 1 / 2 / 3", Position = new(24, 12) });
        status = new Label { Position = new(24, 44) }; panel.AddChild(status);
        foreach (int level in new[] { 0, 1, 2, 3 })
        {
            int value = level; var button = new Button { Text = $"Detail {level}", Position = new(24 + 95 * level, 76), Size = new(85, 30) };
            panel.AddChild(button); detailButtons.Add(level, button); button.Pressed += () => Guard(() => ManualSelect(value));
        }
        twoButton = detailButtons[2]; threeButton = detailButtons[3];
        autoControl = new CheckBox { Text = "Auto", Position = new(410, 78), ButtonPressed = automatic }; panel.AddChild(autoControl);
        autoControl.Toggled += value => Guard(() => { automatic = value; selector.Reset(); if (automatic) UpdateAutomatic(); Refresh(); });
        play = new CheckBox { Text = "Play idle", Position = new(510, 78), ButtonPressed = true }; panel.AddChild(play); play.Toggled += value => Guard(() => { clock!.SetPlaying(value); Refresh(); });
        focus = new CheckBox { Text = "Focus gate", Position = new(625, 78), ButtonPressed = true }; panel.AddChild(focus); focus.Toggled += value => Guard(() => { focusAllowed = value; ApplyFocus(); Refresh(); });
        reset = new Button { Text = "Reset idle", Position = new(800, 76), Size = new(110, 30) }; panel.AddChild(reset); reset.Pressed += () => Guard(() => { selector.Reset(); Seek(0); if (automatic) UpdateAutomatic(); });
        scrub = new HSlider { Position = new(935, 80), Size = new(315, 24), MinValue = 0, MaxValue = clock!.EndFrame, Step = .25 }; panel.AddChild(scrub); scrub.ValueChanged += value => Guard(() => Seek(value));
        panel.AddChild(new Label { Text = "Modern auto 240/120/60 px, hysteresis 10% | shared idle phase | diagnostic lighting | drag to orbit, scroll to zoom, R to reset camera", Position = new(24, 120) });
    }
    private void Guard(Action action) { if (disposed) return; try { action(); } catch (Exception failure) { active = false; GD.PushError("Retained detail boundary failed: " + failure.GetType().Name); GetTree().Quit(1); } }
    private void SelectDetail(int detail)
    {
        if (detail < 0 || detail > 3) throw new ArgumentOutOfRangeException(nameof(detail));
        // Only visibility and sampled pose change. Hidden mesh catches up to this shared clock now.
        meshes[detail].SetOriginalFrame(clock!.Frame); sampledFrames[detail] = clock.Frame;
        selected = detail; foreach (var pair in meshes) pair.Value.Root.Visible = pair.Key == detail; Refresh();
    }
    private void ManualSelect(int detail) { automatic = false; autoControl?.SetPressedNoSignal(false); selector.Reset(); SelectDetail(detail); }
    private SkeletalDetailProjectionSample Projection()
    {
        if (camera!.Projection != Camera3D.ProjectionType.Perspective || camera.KeepAspect != Camera3D.KeepAspectEnum.Height) throw new AssetPreviewException("Vertical perspective projection required.");
        var owner = meshes[0].Root.GlobalTransform;
        if (meshes.Values.Any(m => m.Root.GlobalTransform != owner)) throw new AssetPreviewException("Retained root transforms differ.");
        var basis = owner.Basis; var scale = basis.Scale.Abs();
        if (Math.Abs(basis.X.Normalized().Dot(basis.Y.Normalized())) > 1e-4 || Math.Abs(basis.X.Normalized().Dot(basis.Z.Normalized())) > 1e-4 || Math.Abs(basis.Y.Normalized().Dot(basis.Z.Normalized())) > 1e-4) throw new AssetPreviewException("Sheared sphere transform unsupported.");
        double radius = sphereBounds.Size.Length() * .5 * Math.Max(scale.X, Math.Max(scale.Y, scale.Z));
        var center = owner * sphereBounds.GetCenter(); var view = camera.GetCameraTransform().AffineInverse() * center;
        double depth = -view.Z;
        return new(radius, depth, camera.Fov, fixtureProjectionHeight ?? checked((int)GetViewport().GetVisibleRect().Size.Y));
    }
    private void UpdateAutomatic()
    {
        var choice = selector.Select(Projection()); if (choice.Detail != selected) SelectDetail(choice.Detail); else Refresh();
    }
    private void Seek(double frame) { clock!.Seek(frame); meshes[selected].SetOriginalFrame(frame); sampledFrames[selected] = frame; Refresh(); }
    private void Tick(double seconds)
    {
        if (!double.IsFinite(seconds) || seconds < 0) throw new AssetPreviewException("Invalid retained elapsed time.");
        if (automatic) UpdateAutomatic();
        if (clock!.Advance(Math.Min(seconds, .25))) { meshes[selected].SetOriginalFrame(clock.Frame); sampledFrames[selected] = clock.Frame; } Refresh();
    }
    private void ApplyFocus() => clock?.SetFocused(windowFocused && focusAllowed);
    private void Refresh()
    {
        if (clock is null) return;
        if (status is not null) status.Text = $"{(automatic ? "Auto" : "Manual")} detail {selected} | {meshes[selected].Vertices} vertices / {meshes[selected].Triangles} triangles | idle frame {clock.Frame:0.##}/{clock.EndFrame} | cycles {clock.CompletedLoops} | {(clock.Playing ? "playing" : "paused")} | focus {clock.Focused}";
        play?.SetPressedNoSignal(clock.Playing); scrub?.SetValueNoSignal(clock.Frame);
    }
    // Includes mesh/material/shader/texture instance identity and mip bytes, but excludes mutable pose.
    private string ResourceFingerprint(bool gpu)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        void Add(ulong id) => hash.AppendData(BitConverter.GetBytes(id));
        foreach (var pair in meshes.OrderBy(p => p.Key))
        {
            hash.AppendData(System.Text.Encoding.ASCII.GetBytes(pair.Value.GeometryFingerprint(false))); Add(pair.Value.Root.GetInstanceId());
            foreach (MeshInstance3D node in pair.Value.Root.GetChildren())
            {
                Add(node.GetInstanceId()); Add(node.Mesh.GetInstanceId());
                for (int surface = 0; surface < node.Mesh.GetSurfaceCount(); surface++)
                {
                    var material = (ShaderMaterial)node.Mesh.SurfaceGetMaterial(surface); Add(material.GetInstanceId()); Add(material.Shader.GetInstanceId());
                    foreach (string key in new[] { "albedo_texture", "specular_texture", "normal_texture" })
                    {
                        if (material.GetShaderParameter(key).AsGodotObject() is not ImageTexture texture) continue; Add(texture.GetInstanceId());
                        if (gpu) { using var image = texture.GetImage(); hash.AppendData(image.GetData()); }
                    }
                }
            }
        }
        return Convert.ToHexString(hash.GetHashAndReset());
    }
    private void VerifyFixture(bool gpu)
    {
        automatic = false; string retained = ResourceFingerprint(gpu); long loops = clock!.CompletedLoops;
        windowFocused = true; clock.SetFocused(true); clock.SetPlaying(true);
        clock.Seek(0);
        var idle = bundles!.Details[2].PreparedAnimation!;
        for (int tick = 0; tick < 480; tick++) Tick(idle.DurationSeconds / (idle.ProxyTimeScale * 240));
        if (clock.CompletedLoops - loops != 2 || clock.Frame != 0) throw new AssetPreviewException("Retained shared idle loop fixture differs.");
        for (int i = 0; i < 128; i++)
        {
            Tick(.017); double frame = clock.Frame; long cycle = clock.CompletedLoops;
            int destination = (selected + 1) % 4; double oldHidden = sampledFrames[destination];
            detailButtons[destination].EmitSignal(BaseButton.SignalName.Pressed);
            if (selected != destination || sampledFrames[destination] != frame || clock.Frame != frame || clock.CompletedLoops != cycle || meshes.Count(p => p.Value.Root.Visible) != 1)
                throw new AssetPreviewException("Retained switch restarted/skipped current idle phase.");
            string pose = meshes[destination].GeometryFingerprint(); meshes[destination].SetOriginalFrame(frame);
            if (pose != meshes[destination].GeometryFingerprint() || ResourceFingerprint(gpu) != retained) throw new AssetPreviewException("Retained pose/resource identity changed.");
            foreach (var mesh in meshes.Values) mesh.VerifyBoundary(gpu);
            if (i == 0 && oldHidden == frame) throw new AssetPreviewException("Hidden catch-up fixture did not discriminate stale phase.");
        }
        play!.EmitSignal(BaseButton.SignalName.Toggled, false); double held = clock.Frame; Tick(.2); if (clock.Frame != held) throw new AssetPreviewException("Retained pause failed.");
        play.EmitSignal(BaseButton.SignalName.Toggled, true); focus!.EmitSignal(BaseButton.SignalName.Toggled, false); Tick(.2); if (clock.Frame != held) throw new AssetPreviewException("Retained focus pause failed.");
        focus.EmitSignal(BaseButton.SignalName.Toggled, true); Tick(.1); if (clock.Frame == held) throw new AssetPreviewException("Retained resume failed.");
        scrub!.EmitSignal(Godot.Range.SignalName.ValueChanged, (double)clock.EndFrame); if (clock.Frame != clock.EndFrame) throw new AssetPreviewException("Retained scrub endpoint failed.");
        foreach (int detail in new[] { 0, 1, 2, 3 }) { SelectDetail(detail); if (sampledFrames[detail] != clock.EndFrame) throw new AssetPreviewException("Retained endpoint activation failed."); }
        reset!.EmitSignal(BaseButton.SignalName.Pressed); if (clock.Frame != 0) throw new AssetPreviewException("Retained reset failed.");
        SelectDetail(2); SelectDetail(3);
        if (ResourceFingerprint(gpu) != retained || bundles!.ExternalReads != bundles.Sources.Count) throw new AssetPreviewException("Retained resource/read identity changed.");
        VerifyAutomaticFixture(gpu, retained);
        GD.Print($"PASS: retained switches=134 same_frame=true hidden_catchup=true idle_cycles={clock.CompletedLoops - loops} pause_focus_scrub_reset=true resource_fingerprint={retained} geometry_material_shader_texture_retained=true mip_readback={gpu}");
    }
    private void VerifyAutomaticFixture(bool gpu, string retained)
    {
        var oldTransform = camera!.Transform; float oldOffset = camera.VOffset; camera.VOffset = 0;
        var center = meshes[0].Root.GlobalTransform * sphereBounds.GetCenter();
        void AtPixels(double pixels)
        {
            double radius = Projection().BoundingRadius;
            double depth = radius * Projection().ViewportHeight / Math.Tan(camera.Fov * Math.PI / 360) / pixels;
            camera.Position = center + new Vector3(0, 0, (float)depth); camera.LookAt(center); UpdateAutomatic();
            var sample = Projection(); GD.Print("projection=" + System.Text.Json.JsonSerializer.Serialize(new { sample, selected, pixels = selector.Current!.Value.ProjectedDiameterPixels }));
            if (sampledFrames[selected] != clock!.Frame) throw new AssetPreviewException("Auto hidden activation phase differs.");
            meshes[selected].VerifyBoundary(gpu);
            if (ResourceFingerprint(gpu) != retained) throw new AssetPreviewException("Auto replaced retained resources.");
        }
        automatic = true; selector.Reset();
        foreach (var pair in new[] { (400d, 0), (180d, 1), (90d, 2), (35d, 3), (90d, 2), (180d, 1), (400d, 0) })
        { Tick(.017); AtPixels(pair.Item1); if (selected != pair.Item2) throw new AssetPreviewException("Real camera sweep selected wrong detail."); }
        AtPixels(180); for (int i = 0; i < 24; i++) { AtPixels(i % 2 == 0 ? 235 : 245); if (selected != 1) throw new AssetPreviewException("Real camera hysteresis jitter oscillated."); }
        reset!.EmitSignal(BaseButton.SignalName.Pressed); if (selected != 0 || clock!.Frame != 0) throw new AssetPreviewException("Auto reset did not reinitialize from real projection.");
        double originalRadius = Projection().BoundingRadius;
        foreach (var mesh in meshes.Values) mesh.Root.Scale = new(1.5f, .75f, 1);
        if (Math.Abs(Projection().BoundingRadius - originalRadius * 1.5) > 1e-5) throw new AssetPreviewException("Nonuniform world scale sphere differs.");
        foreach (var mesh in meshes.Values) mesh.Root.Scale = Vector3.One;
        AtPixels(275); if (selected != 0) throw new AssetPreviewException("Real camera refine boundary failed.");
        AtPixels(210); if (selected != 1) throw new AssetPreviewException("Real camera coarsen boundary failed.");
        ManualSelect(3); camera.Position = center + new Vector3(0, 0, (float)(Projection().BoundingRadius * .5)); Tick(.017); if (selected != 3 || automatic) throw new AssetPreviewException("Manual override failed.");
        autoControl!.EmitSignal(BaseButton.SignalName.Toggled, true); if (selected != 0 || selector.Current?.NearCamera != true) throw new AssetPreviewException("Near/inside camera rule failed.");
        camera.Position = center; UpdateAutomatic(); if (selected != 0 || selector.Current?.NearCamera != true) throw new AssetPreviewException("Zero-depth rule failed.");
        var before = selector.Current; camera.Position = center - new Vector3(0, 0, 1);
        try { UpdateAutomatic(); throw new AssetPreviewException("Behind-camera input accepted."); } catch (ArgumentOutOfRangeException) { if (selector.Current != before) throw new AssetPreviewException("Invalid camera changed selector."); }
        camera.Transform = oldTransform; camera.VOffset = oldOffset; automatic = false; selector.Reset(); autoControl.SetPressedNoSignal(false);
        foreach (var mesh in meshes.Values) mesh.VerifyBoundary(gpu);
        if (ResourceFingerprint(gpu) != retained) throw new AssetPreviewException("Camera sweep changed resources.");
        GD.Print("PASS: real camera all4 sweep jitter24 hysteresis=true near_inside_zero_guard=true invalid_atomic=true manual_override=true reset=true world_scale=true retained=true");
    }
    public override void _Process(double delta) { if (active && !disposed) Guard(() => Tick(delta)); }
    public override void _Notification(int what)
    {
        if (what == NotificationWMWindowFocusIn || what == NotificationWMWindowFocusOut) Guard(() => { windowFocused = what == NotificationWMWindowFocusIn; ApplyFocus(); Refresh(); });
    }
    private void DisposeOwned()
    {
        if (disposed) return; disposed = true; active = false; load?.Dispose(); load = null; clock = null;
        foreach (var mesh in meshes.Values)
        {
            ulong rootId = mesh.Root.GetInstanceId(); mesh.Dispose(); mesh.Root.Free();
            if (GodotObject.InstanceFromId(rootId) is not null) throw new AssetPreviewException("Retained root survived teardown.");
        }
        meshes.Clear(); sampledFrames.Clear();
        for (int i = resources.Count - 1; i >= 0; i--) resources[i].Dispose(); resources.Clear(); bundles = null;
    }
    public override void _ExitTree() => DisposeOwned();
}
