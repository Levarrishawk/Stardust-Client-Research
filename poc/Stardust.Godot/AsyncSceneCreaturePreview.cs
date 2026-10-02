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

/// <summary>Offline presentation fixture, not a live world coordinator or terrain barrier.</summary>
public partial class AsyncSceneCreaturePreview : Node
{
    // Independently authored application packet; no original packet capture.
    public const string CreateHex = "0500EADD89FE01000000EFBEADDECDCC4C3E000000809A99993E9A99D93F000000C00000E04000008040A1C11DAFFF";
    public static SceneCreateObjectByCrcMessage DecodeFixture()
    {
        byte[] bytes = Convert.FromHexString(CreateHex);
        var result = SceneObjectLifecycleCodec.DecodeCreate(bytes); Array.Clear(bytes); return result;
    }
    public static object PlacementEvidence(SceneCreatePlacement placement, Transform3D engine)
    {
        float[] V(Vector3 v) => new[] { v.X, v.Y, v.Z };
        float[] N(System.Numerics.Vector3 v) => new[] { v.X, v.Y, v.Z };
        var points = new[] { new Vector3(0, 0, 0), Vector3.Right, Vector3.Up, Vector3.Back, new Vector3(2.3f, -1.7f, .8f) };
        return new { raw = Convert.ToHexString(SceneObjectLifecycleCodec.EncodeCreate(placement.RawCreate)),
            norm = placement.InputQuaternionNorm, normalized = placement.AppliedNormalization, zero = placement.UsedZeroIdentity,
            policy = placement.ZeroPolicy.ToString(), rows = new[] { N(placement.Transform.RowX), N(placement.Transform.RowY), N(placement.Transform.RowZ) },
            translation = N(placement.Transform.Translation), columns = new[] { V(engine.Basis.X), V(engine.Basis.Y), V(engine.Basis.Z) }, origin = V(engine.Origin),
            points = points.Select(p => new { source = V(p), engineInput = V(new(p.X, p.Y, -p.Z)), result = V(engine * new Vector3(p.X, p.Y, -p.Z)) }).ToArray() };
    }
    private SceneObjectRegistry registry = null!;
    private SceneCreatureResourceInitializer initializer = null!;
    private ObjectTemplateCatalog catalog = null!;
    private IReadOnlyDictionary<string, LogicalAnimationClipSelection> bank = null!;
    private readonly LogicalAnimationClipSelection idle = new("appearance/lat/all_m.lat", "emt_standing_idle_ambient_1_ag", "798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B");
    private SceneCreatureVisual? visual;
    private SceneCreatureResources? source;
    private SceneCreatureResourceTicket? ticket;
    private readonly Dictionary<string, AnimationPreviewClock> movement = new();
    private readonly List<Resource> owned = new();
    private readonly List<Task<SceneCreatureResources>> workerTasks = new();
    private readonly TaskCompletionSource<bool> firstLookup = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private byte[] configuration = null!;
    private string archiveRoot = "", selected = "idle";
    private Label? status;
    private PreviewOrbitCamera? camera;
    private bool active, disposed, cancelFixture, playing = true, focused = true;
    private int workerThread, closed;
    private readonly int ownerThread = System.Environment.CurrentManagedThreadId;

    internal async Task InitializeAsync(string[] args)
    {
        string Read(string key) { int at = Array.IndexOf(args, key); if (at < 0 || at + 1 >= args.Length || args.Count(s => s == key) != 1) throw new ArgumentException("Missing/duplicate async preview option."); return args[at + 1]; }
        GetTree().AutoAcceptQuit = false;
        archiveRoot = Read("--asset-root"); configuration = File.ReadAllBytes(Read("--asset-config"));
        if (Convert.ToHexString(SHA256.HashData(configuration)) != RetainedSkeletalDetailBundleReader.ConfigurationPin) throw new InvalidDataException("Configuration pin differs.");
        cancelFixture = args.Contains("--async-cancel"); bool fixture = args.Contains("--async-self-test");
        string? capture = args.Contains("--async-capture") ? Read("--async-capture") : null;
        if (cancelFixture && (fixture || capture is not null) || capture is not null && (fixture || DisplayServer.GetName() == "headless") || cancelFixture && DisplayServer.GetName() != "headless" || DisplayServer.GetName() == "headless" && !fixture && !cancelFixture) throw new ArgumentException("Separate bounded modes required.");
        if (capture is not null)
        {
            capture = Path.GetFullPath(capture); string prefix = Path.GetFullPath(Path.Combine(ProjectSettings.GlobalizePath("res://"), "..", "..", "local-private")) + Path.DirectorySeparatorChar;
            if (!capture.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || Path.GetExtension(capture) != ".png" || File.Exists(capture) || !Directory.Exists(Path.GetDirectoryName(capture))) throw new ArgumentException("Fresh private PNG required.");
        }
        using (var profile = File.OpenRead(Path.Combine(ProjectSettings.GlobalizePath("res://"), "..", "preview-profiles", "human-locomotion.json"))) bank = SkeletalAnimationBankProfile.Load(profile);
        var create = DecodeFixture(); catalog = new(new[] { new ObjectTemplateCatalogEntry(create.TemplateCrc, RetainedSkeletalDetailBundleReader.ObjectPath, RetainedSkeletalDetailBundleReader.ObjectPin, RetainedSkeletalDetailBundleReader.AppearancePin) });
        registry = new(8, 1); initializer = new(registry, LoadOriginal, maximumOutstandingJobs: 2, maximumObjects: 1);
        Spawn(); StartLoad(false);
        var initialWorker = workerTasks.Single();
        if (cancelFixture)
        {
            if (await Task.WhenAny(firstLookup.Task, initialWorker) != firstLookup.Task) throw new AssetPreviewException("Cancellation lookup barrier not reached.");
            initializer.Cancel(ticket!.Value);
        }
        try { await initialWorker; } catch (OperationCanceledException) when (cancelFixture) { }
        if (disposed || !IsInsideTree()) return;
        var completions = initializer.Poll();
        if (cancelFixture)
        {
            if (completions.Count != 1 || completions[0].Kind != SceneCreatureResourceCompletionKind.Canceled || visual is not null || closed != 1) throw new AssetPreviewException($"Canceled original worker boundary: completions={completions.Count} kind={completions.FirstOrDefault()?.Kind} closed={closed} visual={visual is not null} error={completions.FirstOrDefault()?.Error?.GetType().Name}");
            GD.Print("PASS: async canceled first_lookup=true archives_closed=true publications=0"); await ShutdownAsync(); GetTree().Quit(); return;
        }
        if (completions.Count != 1 || completions[0].Kind != SceneCreatureResourceCompletionKind.Prepared || workerThread == ownerThread || System.Environment.CurrentManagedThreadId != ownerThread || closed != 1) throw new AssetPreviewException("Original owner transfer differs.");
        Publish(completions[0]);
        GD.Print($"PASS: async original resources={source!.ExternalReads} bytes={source.SourceBytes} reads_once=true archives_closed=true worker_transfer=true finalized=true");
        foreach (var row in source.Creature.Sources.Values) GD.Print("async_source=" + JsonSerializer.Serialize(row));
        var placement = SceneCreatePlacementBuilder.Create(source.Create, SceneCreateZeroQuaternionPolicy.Reject);
        GD.Print("async_placement=" + JsonSerializer.Serialize(PlacementEvidence(placement, visual!.Root.Transform)));
        BuildControls();
        if (fixture || capture is not null)
        {
            await VerifyFixtureAsync(DisplayServer.GetName() != "headless");
            if (capture is not null)
            {
                visual!.SeekIdle(45); visual.SetPlaying(false); Refresh();
                for (int frame = 0; frame < 3; frame++) await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                visual.Verify(true); using var image = GetViewport().GetTexture().GetImage(); if (image.SavePng(capture) != Error.Ok) throw new IOException("Async preview capture failed.");
                GD.Print($"PASS: async GPU capture frame={visual.IdleFrame} textures={visual.TextureCount} mip_readback=true source_transform_applied=true reflection_once=true");
            }
            await ShutdownAsync(); GD.Print("PASS: async teardown roots_released=true drain_complete=true outstanding=0"); GetTree().Quit(); return;
        }
        focused = GetWindow().HasFocus(); visual!.SetFocused(focused); active = true;
    }
    private Task<SceneCreatureResources> LoadOriginal(SceneCreatureResourceRequest request, CancellationToken token)
    {
        // Capture owned immutable configuration before scheduling; no Godot API in this worker.
        byte[] config = configuration.ToArray(); string root = archiveRoot;
        var task = Task.Run(() =>
        {
            workerThread = System.Environment.CurrentManagedThreadId; var counts = new Dictionary<string, int>();
            try
            {
                using var text = new StringReader(Encoding.UTF8.GetString(config)); using var overlay = ConfiguredTreOverlay.Open(root, text);
                TreLookupResult Resolve(string path)
                {
                    token.ThrowIfCancellationRequested();
                    if (cancelFixture && firstLookup.TrySetResult(true)) { if (!token.WaitHandle.WaitOne(TimeSpan.FromSeconds(10))) throw new InvalidOperationException("Cancellation deadline."); token.ThrowIfCancellationRequested(); }
                    counts[path] = counts.GetValueOrDefault(path) + 1; return overlay.Lookup(path);
                }
                var result = SceneCreatureResourceReader.Load(request.Snapshot.Create, request.Catalog, request.Idle, request.Movement, Resolve, token, request.MaximumResources, request.MaximumSourceBytes);
                if (counts.Count != result.ExternalReads || counts.Values.Any(n => n != 1)) throw new InvalidDataException("Read-once closure differs.");
                return result;
            }
            finally { Interlocked.Increment(ref closed); }
        }, token); workerTasks.RemoveAll(t => t.IsCompleted); workerTasks.Add(task); return task;
    }
    private void Spawn()
    {
        if (registry.Find(DecodeFixture().ObjectId) is not null) return;
        var create = DecodeFixture(); registry.Create(registry.SceneGeneration, create); registry.EndBaselines(registry.SceneGeneration, new(create.ObjectId));
    }
    private SceneCreatureResourceRequest Request(SceneObjectRegistry owner) => new(owner.Find(DecodeFixture().ObjectId) ?? throw new InvalidOperationException("Spawn a creature first."), catalog, idle, bank);
    private void StartLoad(bool restart)
    {
        ticket = initializer.Start(Request(registry), restart); RetireCurrent(); Refresh();
    }
    private void RetireCurrent() { if (visual is not null && !visual.Tick(0)) { visual = null; source = null; movement.Clear(); } }
    private void Publish(SceneCreatureResourceCompletion completion)
    {
        if (completion.Kind != SceneCreatureResourceCompletionKind.Prepared || !initializer.IsCurrent(completion.Ticket)) return;
        var prepared = completion.Resources!; var placement = SceneCreatePlacementBuilder.Create(prepared.Create, SceneCreateZeroQuaternionPolicy.Reject);
        var token = completion.Ticket;
        var candidate = SceneCreatureVisual.TryPublish(registry, token.Handle, prepared, GodotAppearanceTransform.ToGodot(placement.Transform), this,
            currentAttempt: () => !initializer.IsDisposed && initializer.IsCurrent(token));
        if (candidate is null) return;
        visual?.Dispose(); visual = candidate; source = prepared; ticket = token; selected = "idle"; movement.Clear();
        foreach (var pair in prepared.Creature.AdditionalAnimations!) movement.Add(pair.Key, new(pair.Value.FrameRate, pair.Value.EndFrame, pair.Value.ProxyTimeScale));
        visual.SetPlaying(playing); visual.SetFocused(focused); AimCamera(); Refresh();
    }
    private void AimCamera() { if (camera is not null && visual is not null) { var bounds = visual.Root.Transform * visual.Bounds; camera.Configure(bounds.GetCenter(), Math.Max(bounds.Size.Length() * .85f, 1)); } }
    private void BuildControls()
    {
        var environment = new Godot.Environment { BackgroundMode = Godot.Environment.BGMode.Color, BackgroundColor = new Color("#14202e") }; owned.Add(environment); AddChild(new WorldEnvironment { Environment = environment });
        camera = new PreviewOrbitCamera { Current = true, Fov = 50, VOffset = .65f }; AddChild(camera); AimCamera();
        var layer = new CanvasLayer(); AddChild(layer); var panel = new ColorRect { Color = new(.02f, .04f, .08f, .92f), OffsetRight = 1280, OffsetBottom = 185 }; layer.AddChild(panel);
        panel.AddChild(new Label { Text = "STARDUST | ASYNC PLACED CREATURE", Position = new(24, 10) }); status = new Label { Position = new(24, 36) }; panel.AddChild(status);
        void Button(string text, int x, int y, Action action) { var button = new Button { Text = text, Position = new(x, y), Size = new(108, 30) }; panel.AddChild(button); button.Pressed += () => Guard(action); }
        Button("Spawn", 24, 70, Spawn); Button("Load", 144, 70, () => StartLoad(false)); Button("Cancel", 264, 70, () => { if (ticket is { } t) initializer.Cancel(t); });
        Button("Restart", 384, 70, () => StartLoad(true)); Button("Replace", 504, 70, () => { var current = registry.Find(DecodeFixture().ObjectId) ?? throw new InvalidOperationException("Spawn first."); registry.Replace(registry.SceneGeneration, current.Create with { PositionX = current.Create.PositionX + 1 }); registry.EndBaselines(registry.SceneGeneration, new(current.Create.ObjectId)); RetireCurrent(); StartLoad(false); });
        Button("Remove", 624, 70, () => { registry.Destroy(registry.SceneGeneration, new(DecodeFixture().ObjectId, 0)); RetireCurrent(); }); Button("Reset scene", 744, 70, () => { registry.Reset(); RetireCurrent(); });
        Button("Idle", 24, 112, () => { selected = "idle"; visual?.ResumeIdle(); }); Button("Walk", 144, 112, () => SelectMovement("walk")); Button("Run", 264, 112, () => SelectMovement("run"));
        Button("Play / pause", 384, 112, () => { playing = !playing; visual?.SetPlaying(playing); foreach (var clock in movement.Values) clock.SetPlaying(playing); });
        panel.AddChild(new Label { Text = "Decoded source placement | authored MAIN | drag to orbit, scroll to zoom, R reset", Position = new(24, 154) }); Refresh();
    }
    private void SelectMovement(string key) { if (visual is null) return; selected = key; movement[key].SetPlaying(playing); movement[key].SetFocused(focused); visual.SetMovement(key, movement[key].Frame, 0); }
    private void Refresh() { if (status is not null) status.Text = $"{selected} | attempt {ticket?.Attempt} | jobs {(initializer.IsDisposed ? 0 : initializer.OutstandingJobs)}/2 | {(visual is null ? "no visual" : "current visual")} | raw source X/Z/Y preserved"; }
    private void Guard(Action action) { if (disposed) return; try { action(); Refresh(); } catch (InvalidDataException error) { if (status is not null) status.Text = error.Message; } catch (InvalidOperationException error) { if (status is not null) status.Text = error.Message; } }
    public override void _Process(double delta)
    {
        if (!active || disposed) return;
        try
        {
            foreach (var completion in initializer.Poll()) Publish(completion);
            workerTasks.RemoveAll(t => t.IsCompleted);
            RetireCurrent();
            if (visual is not null)
            {
                if (selected == "idle") visual.Tick(delta);
                else { var clock = movement[selected]; clock.Advance(Math.Min(delta, .25)); visual.SetMovement(selected, clock.Frame, Math.Min(delta, .25)); }
            }
            Refresh();
        }
        catch (Exception error) { GD.PushError("Async creature boundary failed: " + error.GetType().Name); active = false; GetTree().Quit(1); }
    }
    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest && !disposed) { _ = CloseAsync(); return; }
        if (what == NotificationWMWindowFocusIn || what == NotificationWMWindowFocusOut) { focused = what == NotificationWMWindowFocusIn; visual?.SetFocused(focused); foreach (var clock in movement.Values) clock.SetFocused(focused); }
    }
    private async Task CloseAsync() { await ShutdownAsync(); GetTree().Quit(); }
    private async Task ShutdownAsync()
    {
        if (disposed) return; disposed = true; active = false; visual?.Dispose(); visual = null; source = null; movement.Clear();
        initializer.Dispose(); registry.Close(); await initializer.DrainCompletion;
        if (initializer.OutstandingJobs != 0 || workerTasks.Any(t => !t.IsCompleted)) throw new AssetPreviewException("Worker drain incomplete.");
        workerTasks.Clear(); foreach (var resource in owned) resource.Dispose(); owned.Clear(); configuration = Array.Empty<byte>();
    }
    public override void _ExitTree()
    {
        // Godot cannot await a notification. Explicit fixture shutdown awaits; interactive exit observes drain.
        if (!disposed) { _ = ShutdownAsync().ContinueWith(t => { _ = t.Exception; }, TaskScheduler.Default); }
    }

    private async Task VerifyFixtureAsync(bool gpu)
    {
        var prepared = source!; var pose = visual!.Root.Transform; string retained = visual.Fingerprint(gpu: gpu)!;
        for (int tick = 0; tick < 96; tick++) { visual.Tick(.017); visual.SetVisible(tick % 2 == 0); visual.Verify(gpu); if (visual.Fingerprint(gpu: gpu) != retained) throw new AssetPreviewException("Placed idle retention failed."); }
        foreach (string key in new[] { "walk", "run" }) for (int tick = 0; tick < 32; tick++) { visual.SetMovement(key, tick % prepared.Creature.AdditionalAnimations![key].EndFrame, .017); visual.Verify(gpu); if (visual.Fingerprint(gpu: gpu) != retained) throw new AssetPreviewException("Placed movement retention failed."); }
        visual.ResumeIdle(); visual.SetVisible(true); visual.SetFocused(false); double held = visual.IdleFrame; visual.Tick(.1); if (held != visual.IdleFrame) throw new AssetPreviewException("Focus gate."); visual.SetFocused(true); visual.SetPlaying(false); visual.Tick(.1); if (held != visual.IdleFrame) throw new AssetPreviewException("Pause gate."); visual.SetPlaying(true);
        // Every task below has explicit completion barriers and deliberately ignores cancellation.
        var owner = new SceneObjectRegistry(8, 1); owner.Create(1, prepared.Create); owner.EndBaselines(1, new(prepared.Create.ObjectId));
        var jobs = new List<(TaskCompletionSource<SceneCreatureResources> Completion, CancellationToken Token)>();
        Task<SceneCreatureResources> Controlled(SceneCreatureResourceRequest _, CancellationToken token) { var completion = new TaskCompletionSource<SceneCreatureResources>(TaskCreationOptions.RunContinuationsAsynchronously); jobs.Add((completion, token)); return completion.Task; }
        using var queue = new SceneCreatureResourceInitializer(owner, Controlled, 2, 1);
        var request = Request(owner); var first = queue.Start(request); jobs[0].Completion.SetResult(prepared); var transferred = queue.Poll().Single();
        using var old = SceneCreatureVisual.TryPublish(owner, first.Handle, transferred.Resources!, pose, this, currentAttempt: () => !queue.IsDisposed && queue.IsCurrent(first))!;
        ulong rootId = old.Root.GetInstanceId(); var second = queue.Start(request, true);
        if (old.Tick(0) || !old.IsRetired || GodotObject.InstanceFromId(rootId) is not null || !owner.IsCurrent(first.Handle)) throw new AssetPreviewException("Same-handle restart did not retire attempt visual.");
        int before = SceneCreatureVisual.Allocations;
        if (SceneCreatureVisual.TryPublish(owner, first.Handle, prepared, pose, this, currentAttempt: () => queue.IsCurrent(first)) is not null || SceneCreatureVisual.Allocations != before) throw new AssetPreviewException("Stale attempt preflight allocated.");
        var third = queue.Start(request, true); bool capacity = false; try { queue.Start(request, true); } catch (InvalidDataException) { capacity = true; }
        if (!capacity || queue.Find(prepared.Create.ObjectId) != third || !jobs[1].Token.IsCancellationRequested || queue.OutstandingJobs != 2) throw new AssetPreviewException("Bounded capacity rollback failed.");
        jobs[2].Completion.SetResult(prepared); var fresh = queue.Poll().Single(); if (fresh.Kind != SceneCreatureResourceCompletionKind.Prepared || fresh.Ticket != third) throw new AssetPreviewException("Fresh attempt transfer failed.");
        jobs[1].Completion.SetException(new InvalidOperationException("Synthetic late fault")); if (queue.Poll().Single().Kind != SceneCreatureResourceCompletionKind.Stale) throw new AssetPreviewException("Late fault resurrected.");
        int disposals = SceneCreatureVisual.Disposals;
        if (SceneCreatureVisual.TryPublish(owner, third.Handle, prepared, pose, this, beforePublication: () => queue.Start(request, true), currentAttempt: () => queue.IsCurrent(third)) is not null || SceneCreatureVisual.Disposals != disposals + 1) throw new AssetPreviewException("Staged attempt invalidation leaked.");
        var staged = queue.Find(prepared.Create.ObjectId)!.Value; jobs[^1].Completion.SetResult(prepared); queue.Poll();
        ulong attachedId = 0;
        void OnAttach(Node node) { if (node is Node3D n) { if (n.Visible) throw new AssetPreviewException("Staged root visible."); attachedId = n.GetInstanceId(); queue.Start(request, true); } }
        ChildEnteredTree += OnAttach;
        try { if (SceneCreatureVisual.TryPublish(owner, staged.Handle, prepared, pose, this, currentAttempt: () => queue.IsCurrent(staged)) is not null) throw new AssetPreviewException("Post-attach stale attempt published."); }
        finally { ChildEnteredTree -= OnAttach; }
        if (attachedId == 0 || GodotObject.InstanceFromId(attachedId) is not null) throw new AssetPreviewException("Attached candidate survived.");
        jobs[^1].Completion.SetResult(prepared); queue.Poll();
        // Registry changes cancel pending work; late successes and faults cannot publish.
        int transitions = 0;
        foreach (string action in new[] { "finalize", "contain", "replace", "destroy", "reset" })
        {
            var r = new SceneObjectRegistry(8, 1); r.Create(1, prepared.Create); if (action != "finalize") r.EndBaselines(1, new(prepared.Create.ObjectId));
            var gate = new TaskCompletionSource<SceneCreatureResources>(TaskCreationOptions.RunContinuationsAsynchronously); CancellationToken cancellation = default;
            using var q = new SceneCreatureResourceInitializer(r, (_, token) => { cancellation = token; return gate.Task; }, 1, 1);
            var t = q.Start(Request(r));
            if (action == "finalize") r.EndBaselines(1, new(prepared.Create.ObjectId));
            if (action == "contain") r.Contain(1, new(prepared.Create.ObjectId, 0, 1));
            if (action == "replace") r.Replace(1, prepared.Create with { PositionY = 9 });
            if (action == "destroy") r.Destroy(1, new(prepared.Create.ObjectId, 0)); if (action == "reset") r.Reset();
            if (q.Poll().Count != 0 || !cancellation.IsCancellationRequested || q.IsCurrent(t)) throw new AssetPreviewException("Pending registry invalidation not canceled.");
            if (transitions % 2 == 0) gate.SetResult(prepared); else gate.SetException(new IOException("Synthetic late failure"));
            var result = q.Poll().Single(); if (result.Kind != SceneCreatureResourceCompletionKind.Stale || result.Resources is not null) throw new AssetPreviewException("Old revision resurrected."); transitions++;
        }
        // Explicit cancel on a still-current handle must suppress cancellation-ignoring success.
        var canceled = queue.Start(request, true); if (!queue.Cancel(canceled)) throw new AssetPreviewException("Cancel failed."); jobs[^1].Completion.SetResult(prepared);
        if (queue.Poll().Single().Kind != SceneCreatureResourceCompletionKind.Canceled) throw new AssetPreviewException("Canceled success published.");
        queue.Start(request, true); var successGate = jobs[^1]; queue.Start(request, true); var faultGate = jobs[^1]; queue.Dispose();
        if (queue.DrainCompletion.IsCompleted || !successGate.Token.IsCancellationRequested || !faultGate.Token.IsCancellationRequested) throw new AssetPreviewException("Disposal did not retain/await workers.");
        successGate.Completion.SetResult(prepared); faultGate.Completion.SetException(new IOException("Synthetic disposal late fault")); await queue.DrainCompletion;
        if (queue.OutstandingJobs != 0) throw new AssetPreviewException("Controlled drain incomplete.");
        if (visual!.Root.Transform != pose || visual.Fingerprint(gpu: gpu) != retained) throw new AssetPreviewException("Fixture changed live placed visual.");
        GD.Print($"PASS: async retained idle96 movement64 mip_readback={gpu} same_handle_restart=true preflight=true staged_cleanup=true post_attach_cleanup=true capacity2=true lifecycle5=true canceled_late_success=true late_fault_observed=true disposal_drain=true");
    }
}
