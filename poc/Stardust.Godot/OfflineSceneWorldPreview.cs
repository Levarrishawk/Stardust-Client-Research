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

/// <summary>Offline fixture driver shared with the native diagnostic; no socket or duplicate registry.</summary>
public sealed class OfflineWorldPeerDriver : IDisposable
{
    private readonly SceneWorldCoordinator world;
    private readonly Dictionary<string, string> peer;
    private SoeClientTransport? transport;
    private readonly List<string> delivered = new(), controls = new();
    private string request = "", select = "", ready = "", retry = "";
    public ulong ObjectId { get; }
    public long Generation { get; private set; }
    public OfflineWorldPeerDriver(SceneWorldCoordinator owner, IReadOnlyDictionary<string, string> fixture)
    { if (fixture.Count > 32 || fixture.Values.Any(v => v.Length > 4096)) throw new ArgumentException("Bounded independent peer fixture required."); world = owner; peer = fixture.ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal); ObjectId = SceneObjectLifecycleCodec.DecodeCreate(Convert.FromHexString(peer["create"])).ObjectId; }
    public void SelectAndStage()
    {
        transport?.Close(); transport = new(); delivered.Clear(); controls.Clear(); ready = retry = "";
        var selection = world.BeginSelection(ObjectId); Generation = selection.Generation;
        request = Convert.ToHexString(transport.BeginSession(0x12345678, 128)); transport.AcceptSessionResponse(Convert.FromHexString(peer["response"]));
        byte[] selected = transport.CreateReliableDatagram(selection.CopySelectMessage(), 0); select = Convert.ToHexString(selected); Check(select == peer["expectedSelect"], "Select bytes"); Array.Clear(selected);
        Receive("ackSelect"); Check(transport.RetainedOutboundPackets == 0, "Select ACK");
        byte[] bad = Convert.FromHexString(peer["peer_create"]); bad[^1] ^= 1; Check(transport.AcceptDatagram(bad).RejectedFraming && world.State == ZoneSceneTransitionState.Selecting, "CRC isolation");
        Receive("peer_create"); Check(world.StagedCount == 0, "Reordered create retained in transport");
        Receive("peer_start"); Receive("peer_start");
        foreach (string name in new[] { "baseline", "delta", "end", "contain" }) Receive("peer_" + name);
        Check(world.State == ZoneSceneTransitionState.Initializing && world.StagedCount == 5 && world.Snapshots().Count == 0, "Staged owned bytes before barrier");
    }
    public void OpenCallerBarrier()
    {
        Check(world.CompleteMinimumSceneBarrier(Generation, false) is null && world.CompleteMinimumSceneBarrier(Generation - 1, true) is null, "False/stale barrier");
        byte[] application = world.CompleteMinimumSceneBarrier(Generation, true)!;
        Check(world.CompleteMinimumSceneBarrier(Generation, true) is null, "One-shot barrier");
        var snapshot = world.Snapshots().Single(); Check(snapshot.Object.Phase == SceneObjectPhase.Finalized && snapshot.Envelopes.Count == 2, "Finalized opaque world object");
        foreach (var envelope in snapshot.Envelopes)
        {
            Check(envelope.RawTag == 0x4352454F && envelope.View == 3 && envelope.DeclaredCount == 99 && envelope.CopyOpaqueFields().SequenceEqual(new byte[] { 127, 0, 255 }), "Synthetic opaque bytes only");
            byte[] copy = envelope.CopyOpaqueFields(); Array.Clear(copy); Check(envelope.CopyOpaqueFields().SequenceEqual(new byte[] { 127, 0, 255 }), "Owned envelope copy");
        }
        byte[] wire = transport!.CreateReliableDatagram(application, 100); ready = Convert.ToHexString(wire); Check(ready == peer["expectedReady"], "Ready bytes");
        Check(transport.GetRetransmissionDatagrams(199, 100).Count == 0, "Retry deadline"); retry = Convert.ToHexString(transport.GetRetransmissionDatagrams(200, 100).Single()); Check(retry == ready, "Owned ready retry");
        Array.Clear(application); Array.Clear(wire); Check(Convert.ToHexString(transport.GetRetransmissionDatagrams(300, 100).Single()) == ready, "Retained ready ownership");
    }
    public void ServerReady() { Receive("ackReady"); Receive("peer_serverReady"); Check(world.State == ZoneSceneTransitionState.Ready && transport!.RetainedOutboundPackets == 0, "Server-ready/ACK"); }
    public void Destroy() => Receive("peer_destroy");
    public void ContainAgain() => Receive("peer_containAgain");
    public void ResetSelection() { transport?.Close(); transport = null; Generation = world.BeginSelection(ObjectId).Generation; }
    private void Receive(string name)
    {
        byte[] wire = Convert.FromHexString(peer[name]); var result = transport!.AcceptDatagram(wire);
        controls.AddRange(result.ControlDatagrams.Select(Convert.ToHexString));
        if (controls.Count > 64) controls.RemoveRange(0, controls.Count - 64);
        foreach (byte[] application in result.ApplicationPayloads) { delivered.Add(Convert.ToHexString(application)); if (delivered.Count > 16) delivered.RemoveAt(0); world.Accept(Generation, application); Array.Clear(application); }
        Array.Clear(wire);
    }
    public object Export() => new { SessionRequest = request, SelectDatagram = select, ReadyDatagram = ready, Retransmission = retry,
        DeliveredApplications = delivered.ToArray(), Controls = controls.ToArray(), FinalState = world.State.ToString(), RetainedPackets = transport?.RetainedOutboundPackets ?? 0 };
    public void Dispose() { transport?.Close(); transport = null; }
    private static void Check(bool value, string message) { if (!value) throw new InvalidDataException("Offline preview peer: " + message); }
}

/// <summary>Offline world-owned presentation. Finalized, prepared and ready are separate observations.</summary>
public partial class OfflineSceneWorldPreview : Node
{
    private SceneWorldCoordinator world = null!;
    private OfflineWorldPeerDriver driver = null!;
    private SceneCreatureVisual? visual;
    private ScenePreparedCreaturePublication? publication;
    private ObjectTemplateCatalog catalog = null!;
    private IReadOnlyDictionary<string, LogicalAnimationClipSelection> bank = null!;
    private IReadOnlyDictionary<string, string> peer = null!;
    private readonly LogicalAnimationClipSelection idle = new("appearance/lat/all_m.lat", "emt_standing_idle_ambient_1_ag", "798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B");
    private readonly List<Task<SceneCreatureResources>> workers = new();
    private readonly List<Resource> owned = new();
    private readonly Dictionary<string, AnimationPreviewClock> movement = new();
    private readonly TaskCompletionSource<bool> firstLookup = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private byte[] configuration = null!;
    private string root = "", clip = "idle";
    private int closed, workerThread;
    private readonly int ownerThread = System.Environment.CurrentManagedThreadId;
    private bool disposed, active, cancelFixture, playing = true, focused = true;
    private Label? status;
    private PreviewOrbitCamera? camera;

    internal async Task InitializeAsync(string[] args)
    {
        string Read(string key) { int at = Array.IndexOf(args, key); if (at < 0 || at + 1 >= args.Length || args.Count(s => s == key) != 1) throw new ArgumentException("Missing/duplicate world preview option."); return args[at + 1]; }
        GetTree().AutoAcceptQuit = false; root = Read("--asset-root"); configuration = File.ReadAllBytes(Read("--asset-config"));
        if (Convert.ToHexString(SHA256.HashData(configuration)) != RetainedSkeletalDetailBundleReader.ConfigurationPin) throw new InvalidDataException("Configuration pin differs.");
        string peerPath = Read("--peer-fixture"); if (new FileInfo(peerPath).Length > 65536) throw new ArgumentException("Peer fixture file budget exceeded.");
        peer = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(peerPath)) ?? throw new InvalidDataException("Peer fixture missing.");
        cancelFixture = args.Contains("--world-cancel"); bool fixture = args.Contains("--world-self-test"); string? capture = args.Contains("--world-capture") ? Read("--world-capture") : null;
        if (cancelFixture && (fixture || capture is not null) || capture is not null && (fixture || DisplayServer.GetName() == "headless") || cancelFixture && DisplayServer.GetName() != "headless" || DisplayServer.GetName() == "headless" && !fixture && !cancelFixture) throw new ArgumentException("Separate bounded modes required.");
        if (capture is not null)
        {
            capture = Path.GetFullPath(capture); string prefix = Path.GetFullPath(Path.Combine(ProjectSettings.GlobalizePath("res://"), "..", "..", "local-private")) + Path.DirectorySeparatorChar;
            if (!capture.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || Path.GetExtension(capture) != ".png" || File.Exists(capture) || !Directory.Exists(Path.GetDirectoryName(capture))) throw new ArgumentException("Fresh private PNG required.");
        }
        using (var profile = File.OpenRead(Path.Combine(ProjectSettings.GlobalizePath("res://"), "..", "preview-profiles", "human-locomotion.json"))) bank = SkeletalAnimationBankProfile.Load(profile);
        var create = SceneObjectLifecycleCodec.DecodeCreate(Convert.FromHexString(peer["create"]));
        catalog = new(new[] { new ObjectTemplateCatalogEntry(create.TemplateCrc, RetainedSkeletalDetailBundleReader.ObjectPath, RetainedSkeletalDetailBundleReader.ObjectPin, RetainedSkeletalDetailBundleReader.AppearancePin) });
        world = MakeWorld(LoadOriginal); driver = new(world, peer); driver.SelectAndStage();
        if (world.OutstandingJobs != 0) throw new AssetPreviewException("Pre-barrier worker started.");
        driver.OpenCallerBarrier(); GD.Print("world_phase=Finalized state=AwaitingServerReady prepared=false");
        if (world.PollResources().Prepared.Count != 0 || workers.Count != 1) throw new AssetPreviewException("World finalized start differs.");
        var worker = workers.Single();
        if (cancelFixture)
        {
            if (await Task.WhenAny(firstLookup.Task, worker) != firstLookup.Task) throw new AssetPreviewException("First lookup barrier missing.");
            driver.ResetSelection();
        }
        try { await worker; } catch (OperationCanceledException) when (cancelFixture) { }
        if (disposed || !IsInsideTree()) return;
        var results = world.PollResources(); PruneWorkers();
        if (cancelFixture)
        {
            if (results.Prepared.Count != 0 || results.Retired.Count != 1 || visual is not null || closed != 1) throw new AssetPreviewException("World canceled worker published or failed closure.");
            GD.Print("PASS: world canceled first_lookup=true generation_reset=true archives_closed=true publications=0"); await ShutdownAsync(); GetTree().Quit(); return;
        }
        if (results.Prepared.Count != 1 || closed != 1 || workerThread == ownerThread || System.Environment.CurrentManagedThreadId != ownerThread) throw new AssetPreviewException("Original world transfer differs.");
        Publish(results.Prepared.Single()); GD.Print("world_phase=Prepared state=AwaitingServerReady finalized=true");
        driver.ServerReady(); GD.Print("world_phase=Ready finalized=true prepared=true");
        GD.Print("world_wire=" + JsonSerializer.Serialize(driver.Export()));
        foreach (var row in publication!.Resources.Creature.Sources.Values) GD.Print("world_source=" + JsonSerializer.Serialize(row));
        GD.Print("world_placement=" + JsonSerializer.Serialize(AsyncSceneCreaturePreview.PlacementEvidence(publication.Placement, visual!.Root.Transform)));
        GD.Print($"PASS: world original resources={publication.Resources.ExternalReads} bytes={publication.Resources.SourceBytes} reads_once=true archives_closed=true worker_transfer=true");
        BuildControls();
        if (fixture || capture is not null)
        {
            await VerifyAsync(DisplayServer.GetName() != "headless");
            if (capture is not null)
            {
                visual!.SeekIdle(45); visual.SetPlaying(false); Refresh();
                for (int frame = 0; frame < 3; frame++) await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                visual.Verify(true); using var image = GetViewport().GetTexture().GetImage(); if (image.SavePng(capture) != Error.Ok) throw new IOException("World capture failed.");
                GD.Print($"PASS: world GPU capture frame={visual.IdleFrame} textures={visual.TextureCount} mip_readback=true reflection_once=true");
            }
            await ShutdownAsync(); GD.Print("PASS: world teardown roots_released=true drain_complete=true outstanding=0 transport_closed=true"); GetTree().Quit(); return;
        }
        focused = GetWindow().HasFocus(); visual!.SetFocused(focused); active = true;
    }
    private SceneWorldCoordinator MakeWorld(Func<SceneCreatureResourceRequest, CancellationToken, Task<SceneCreatureResources>> loader, int maximumJobs = 2)
        => new(catalog, idle, bank, loader, SceneCreateZeroQuaternionPolicy.Reject, maximumObjects: 1, maximumOutstandingJobs: maximumJobs,
            maximumStagedMessages: 8, maximumStagedBytes: 1048576, maximumEnvelopes: 2, maximumEnvelopeBytes: 56, maximumPayloadBytes: 64);
    private Task<SceneCreatureResources> LoadOriginal(SceneCreatureResourceRequest request, CancellationToken token)
    {
        PruneWorkers(); byte[] config = configuration.ToArray(); string archive = root;
        var task = Task.Run(() =>
        {
            workerThread = System.Environment.CurrentManagedThreadId;
            try
            {
                using var text = new StringReader(Encoding.UTF8.GetString(config)); using var overlay = ConfiguredTreOverlay.Open(archive, text); var reads = new Dictionary<string, int>();
                TreLookupResult Resolve(string path)
                {
                    token.ThrowIfCancellationRequested();
                    if (cancelFixture && firstLookup.TrySetResult(true)) { if (!token.WaitHandle.WaitOne(TimeSpan.FromSeconds(10))) throw new InvalidOperationException("Cancellation deadline."); token.ThrowIfCancellationRequested(); }
                    reads[path] = reads.GetValueOrDefault(path) + 1; return overlay.Lookup(path);
                }
                var resource = SceneCreatureResourceReader.Load(request.Snapshot.Create, request.Catalog, request.Idle, request.Movement, Resolve, token, request.MaximumResources, request.MaximumSourceBytes);
                if (reads.Count != 37 || reads.Values.Any(n => n != 1) || resource.ExternalReads != 37 || resource.SourceBytes != 4683848) throw new InvalidDataException("Pinned world closure differs."); return resource;
            }
            finally { Interlocked.Increment(ref closed); }
        }, token); workers.Add(task); if (workers.Count > 2) throw new InvalidOperationException("Worker tracking budget exceeded."); return task;
    }
    private void Publish(ScenePreparedCreaturePublication prepared)
    {
        if (!world.IsCurrent(prepared)) return;
        var candidate = SceneCreatureVisual.TryPublishWorld(world, prepared, GodotAppearanceTransform.ToGodot(prepared.Placement.Transform), this);
        if (candidate is null) return;
        visual?.Dispose(); visual = candidate; publication = prepared; clip = "idle"; movement.Clear();
        foreach (var pair in prepared.Resources.Creature.AdditionalAnimations!) movement.Add(pair.Key, new(pair.Value.FrameRate, pair.Value.EndFrame, pair.Value.ProxyTimeScale));
        visual.SetPlaying(playing); visual.SetFocused(focused); Aim(); Refresh();
    }
    private void PruneWorkers() => workers.RemoveAll(t => t.IsCompleted);
    private void Retire() { if (visual is not null && !visual.Tick(0)) { visual = null; publication = null; movement.Clear(); } }
    private void Aim() { if (visual is not null && camera is not null) { var bounds = visual.Root.Transform * visual.Bounds; camera.Configure(bounds.GetCenter(), Math.Max(bounds.Size.Length() * .85f, 1)); } }
    private void BuildControls()
    {
        var environment = new Godot.Environment { BackgroundMode = Godot.Environment.BGMode.Color, BackgroundColor = new Color("#14202e") }; owned.Add(environment); AddChild(new WorldEnvironment { Environment = environment });
        camera = new PreviewOrbitCamera { Current = true, Fov = 50, VOffset = .65f }; AddChild(camera); Aim();
        var layer = new CanvasLayer(); AddChild(layer); var panel = new ColorRect { Color = new(.02f, .04f, .08f, .92f), OffsetRight = 1280, OffsetBottom = 185 }; layer.AddChild(panel);
        panel.AddChild(new Label { Text = "STARDUST | OFFLINE WORLD CREATURE", Position = new(24, 10) }); status = new Label { Position = new(24, 36) }; panel.AddChild(status);
        void Button(string text, int x, int y, Action action) { var b = new Button { Text = text, Position = new(x, y), Size = new(112, 30) }; panel.AddChild(b); b.Pressed += () => Guard(action); }
        Button("Select / stage", 24, 70, () => { driver.SelectAndStage(); Retire(); });
        Button("Open barrier", 152, 70, driver.OpenCallerBarrier); Button("Server ready", 280, 70, driver.ServerReady);
        Button("Restart load", 408, 70, () => { world.RestartResource(world.Generation, driver.ObjectId); Retire(); });
        Button("Destroy", 536, 70, () => { driver.Destroy(); Retire(); }); Button("Reset", 664, 70, () => { driver.ResetSelection(); Retire(); });
        Button("Idle", 24, 112, () => { clip = "idle"; visual?.ResumeIdle(); }); Button("Walk", 152, 112, () => SelectMovement("walk")); Button("Run", 280, 112, () => SelectMovement("run"));
        Button("Play / pause", 408, 112, () => { playing = !playing; visual?.SetPlaying(playing); foreach (var clock in movement.Values) clock.SetPlaying(playing); });
        panel.AddChild(new Label { Text = "Explicit caller barrier | opaque fixture fields | authored MAIN | drag to orbit, scroll to zoom, R reset", Position = new(24, 154) }); Refresh();
    }
    private void SelectMovement(string key) { if (visual is null) return; clip = key; movement[key].SetPlaying(playing); movement[key].SetFocused(focused); visual.SetMovement(key, movement[key].Frame, 0); }
    private void Refresh()
    {
        if (status is null) return;
        string phase = world.Snapshots().FirstOrDefault()?.Object.Phase.ToString() ?? "none";
        status.Text = $"World {world.State} | Object {phase} | Prepared {publication is not null} | jobs {world.OutstandingJobs}/2 | {clip}";
    }
    private void Guard(Action action) { if (disposed) return; try { action(); Refresh(); } catch (Exception error) when (error is InvalidDataException or InvalidOperationException) { Retire(); if (status is not null) status.Text = error.Message; } }
    public override void _Process(double delta)
    {
        if (!active || disposed) return;
        try
        {
            var polled = world.PollResources(); PruneWorkers(); foreach (var result in polled.Prepared) Publish(result); Retire();
            if (visual is not null) { if (clip == "idle") visual.Tick(delta); else { var clock = movement[clip]; clock.Advance(Math.Min(delta, .25)); visual.SetMovement(clip, clock.Frame, Math.Min(delta, .25)); } }
            Refresh();
        }
        catch (Exception error) { Retire(); active = false; GD.PushError("World preview failed: " + error.GetType().Name); GetTree().Quit(1); }
    }
    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest && !disposed) { _ = CloseAsync(); return; }
        if (what == NotificationWMWindowFocusIn || what == NotificationWMWindowFocusOut) { focused = what == NotificationWMWindowFocusIn; visual?.SetFocused(focused); foreach (var clock in movement.Values) clock.SetFocused(focused); }
    }
    private async Task CloseAsync() { await ShutdownAsync(); GetTree().Quit(); }
    private async Task ShutdownAsync()
    {
        if (disposed) return; disposed = true; active = false; visual?.Dispose(); visual = null; publication = null; movement.Clear(); driver.Dispose(); world.Close(); await world.DrainCompletion;
        if (world.OutstandingJobs != 0 || workers.Any(t => !t.IsCompleted)) throw new AssetPreviewException("World drain incomplete."); workers.Clear();
        foreach (var resource in owned) resource.Dispose(); owned.Clear(); configuration = Array.Empty<byte>();
    }
    public override void _ExitTree() { if (!disposed && world is not null) _ = ShutdownAsync().ContinueWith(t => { _ = t.Exception; }, TaskScheduler.Default); }

    private async Task VerifyAsync(bool gpu)
    {
        if (workers.Count != 0) throw new AssetPreviewException("Completed original tasks retained their result bundles.");
        var prepared = publication!.Resources; var pose = visual!.Root.Transform; string retained = visual.Fingerprint(gpu: gpu)!;
        for (int tick = 0; tick < 96; tick++) { visual.Tick(.017); visual.SetVisible(tick % 2 == 0); visual.Verify(gpu); if (visual.Fingerprint(gpu: gpu) != retained) throw new AssetPreviewException("World idle retention."); }
        foreach (string key in new[] { "walk", "run" }) for (int tick = 0; tick < 32; tick++) { visual.SetMovement(key, tick % prepared.Creature.AdditionalAnimations![key].EndFrame, .017); visual.Verify(gpu); if (visual.Fingerprint(gpu: gpu) != retained) throw new AssetPreviewException("World movement retention."); }
        visual.ResumeIdle(); visual.SetVisible(true); visual.SetFocused(false); double held = visual.IdleFrame; visual.Tick(.1); if (held != visual.IdleFrame) throw new AssetPreviewException("Focus gate."); visual.SetFocused(true); visual.SetPlaying(false); visual.Tick(.1); if (held != visual.IdleFrame) throw new AssetPreviewException("Pause gate."); visual.SetPlaying(true);
        var jobs = new List<(TaskCompletionSource<SceneCreatureResources> Completion, CancellationToken Token)>();
        Task<SceneCreatureResources> Controlled(SceneCreatureResourceRequest _, CancellationToken token) { var gate = new TaskCompletionSource<SceneCreatureResources>(TaskCreationOptions.RunContinuationsAsynchronously); jobs.Add((gate, token)); return gate.Task; }
        using var owner = MakeWorld(Controlled); using var peerDriver = new OfflineWorldPeerDriver(owner, peer);
        peerDriver.SelectAndStage(); peerDriver.OpenCallerBarrier(); owner.PollResources(); jobs[0].Completion.SetResult(prepared); var first = owner.PollResources().Prepared.Single();
        using var old = SceneCreatureVisual.TryPublishWorld(owner, first, pose, this)!; ulong oldRoot = old.Root.GetInstanceId();
        owner.RestartResource(owner.Generation, peerDriver.ObjectId);
        if (old.Tick(0) || !old.IsRetired || GodotObject.InstanceFromId(oldRoot) is not null) throw new AssetPreviewException("Queued restart did not retire world publication.");
        int allocations = SceneCreatureVisual.Allocations;
        if (SceneCreatureVisual.TryPublishWorld(owner, first, pose, this) is not null || allocations != SceneCreatureVisual.Allocations) throw new AssetPreviewException("Stale world preflight allocated.");
        owner.PollResources(); owner.RestartResource(owner.Generation, peerDriver.ObjectId); owner.PollResources(); owner.RestartResource(owner.Generation, peerDriver.ObjectId); owner.PollResources();
        if (owner.OutstandingJobs != 2 || jobs.Count != 3 || owner.Snapshots().Count != 1 || !jobs[1].Token.IsCancellationRequested || !jobs[2].Token.IsCancellationRequested) throw new AssetPreviewException("World bounded queue.");
        jobs[2].Completion.SetResult(prepared); var suppressed = owner.PollResources(); if (suppressed.Prepared.Count != 0 || suppressed.Retired.Count != 1 || jobs.Count != 4) throw new AssetPreviewException("Queued restart late success escaped.");
        jobs[1].Completion.SetException(new IOException("Synthetic stale fault")); if (owner.PollResources().Retired.Count != 1) throw new AssetPreviewException("Stale fault unobserved.");
        jobs[3].Completion.SetResult(prepared); var fresh = owner.PollResources().Prepared.Single();
        int disposals = SceneCreatureVisual.Disposals;
        if (SceneCreatureVisual.TryPublishWorld(owner, fresh, pose, this, beforePublication: () => owner.RestartResource(owner.Generation, peerDriver.ObjectId)) is not null || SceneCreatureVisual.Disposals != disposals + 1) throw new AssetPreviewException("Staged world invalidation leak.");
        owner.PollResources(); jobs[^1].Completion.SetResult(prepared); fresh = owner.PollResources().Prepared.Single();
        ulong attached = 0;
        void OnAttach(Node node) { if (node is Node3D n) { if (n.Visible) throw new AssetPreviewException("Staged root visible."); attached = n.GetInstanceId(); owner.BeginSelection(peerDriver.ObjectId); } }
        ChildEnteredTree += OnAttach;
        try { if (SceneCreatureVisual.TryPublishWorld(owner, fresh, pose, this) is not null) throw new AssetPreviewException("Attached stale world published."); }
        finally { ChildEnteredTree -= OnAttach; }
        if (attached == 0 || GodotObject.InstanceFromId(attached) is not null) throw new AssetPreviewException("Attached root survived.");
        // Real peer application invalidations with deterministic cancellation-ignoring completion.
        foreach (string action in new[] { "selection", "destroy", "contain" })
        {
            CancellationToken cancellation = default; var gate = new TaskCompletionSource<SceneCreatureResources>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var w = MakeWorld((_, token) => { cancellation = token; return gate.Task; }, 1); using var d = new OfflineWorldPeerDriver(w, peer); d.SelectAndStage(); d.OpenCallerBarrier(); w.PollResources();
            long generation = w.Generation;
            if (action == "selection") d.ResetSelection(); else { d.ServerReady(); if (action == "destroy") d.Destroy(); else d.ContainAgain(); }
            if (w.PollResources().Prepared.Count != 0 || !cancellation.IsCancellationRequested) throw new AssetPreviewException("World invalidation did not cancel.");
            d.ResetSelection(); if (w.Accept(generation, new byte[] { 1 }) || w.Snapshots().Count != 0) throw new AssetPreviewException("Old generation accepted.");
            if (action == "destroy") gate.SetException(new IOException("Synthetic destroyed late fault")); else gate.SetResult(prepared);
            var result = w.PollResources(); if (result.Prepared.Count != 0 || result.Retired.Count != 1 || result.Retired[0].Resources is not null) throw new AssetPreviewException("Old world resurrected.");
        }
        peerDriver.SelectAndStage(); peerDriver.OpenCallerBarrier(); owner.PollResources(); var success = jobs[^1]; owner.RestartResource(owner.Generation, peerDriver.ObjectId); owner.PollResources(); var fault = jobs[^1];
        owner.Close(); if (owner.DrainCompletion.IsCompleted || !success.Token.IsCancellationRequested || !fault.Token.IsCancellationRequested) throw new AssetPreviewException("Close did not retain workers.");
        success.Completion.SetResult(prepared); fault.Completion.SetException(new IOException("Synthetic close late fault")); await owner.DrainCompletion;
        if (owner.OutstandingJobs != 0 || visual!.Root.Transform != pose || visual.Fingerprint(gpu: gpu) != retained) throw new AssetPreviewException("Drain or retained live world failed.");
        GD.Print($"PASS: world retained idle96 movement64 mip_readback={gpu} queued_restart=true stale_preflight=true staged_cleanup=true post_attach_cleanup=true capacity2=true generation_destroy_contain=true late_success_fault=true awaited_drain=true");
    }
}
