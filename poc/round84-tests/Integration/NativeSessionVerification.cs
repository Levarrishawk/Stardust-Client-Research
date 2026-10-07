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
using Stardust.Core.Configuration;
using Stardust.Core.Hosting;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

/// <summary>Offline verification composition. Injected authored peers only; never constructs sockets.</summary>
public partial class NativeSessionVerification : Node3D
{
    private const ulong ObjectId = 0x1122334455667788;
    private OriginalInstallationInputs? inputs;
    private ReplacementClientSession? session;
    private ReplacementDatagramPump? driver;
    private ReplacementSceneBridge? bridge;
    private SceneWorldCoordinator? world;
    private ReplacementPlayerPresentationHost? presentation;
    private TestPort? login, zone;
    private JsonDocument? transcript;
    private string output = "", mode = "success";
    private long frame, staged, publishedAt;
    private ulong publishedRootId;
    private int unhandled, terrainBytes;
    private bool authenticated, selected, closing, published, saved;
    private string? failure;
    private object? preparationReceipt;
    private SceneObjectResources? loaded;
    private string? terrainHash;
    private readonly TaskCompletionSource<bool> minimumGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private Camera3D? camera;
    private WorldEnvironment? lighting;
    private Godot.Environment? environment;
    private DirectionalLight3D? sun;

    public override void _Ready()
    {
        try
        {
            string[] args = OS.GetCmdlineUserArgs();
            if (args.Length != 5) throw new ArgumentException("root cfg transcript private-output success|cancel|terrain-failure required");
            output = args[3]; mode = args[4];
            if (mode is not ("success" or "cancel" or "terrain-failure")) throw new ArgumentException("mode");
            Directory.CreateDirectory(output);
            transcript = JsonDocument.Parse(File.ReadAllBytes(args[2]));
            var t = transcript.RootElement;
            string objectPath = t.GetProperty("ObjectPath").GetString()!;
            inputs = OriginalInstallationInputs.Open(args[0], args[1], new[] { objectPath });
            inputs.RequireAllPresent();
            var prep = InstalledCreaturePreparation.Create(inputs, objectPath,
                new("appearance/lat/all_m.lat", "emt_standing_idle_ambient_1_ag"),
                new("appearance/lat/all_m.lat", "loop_standing", new[] { 0, 1 }),
                new("appearance/lat/all_m.lat", "loop_standing", new[] { 0, 2 }));
            preparationReceipt = new { Catalog = prep.Catalog.Entries.Values.Single(), prep.Sources,
                Idle = prep.Options.Idle, Walk = prep.Options.Movement["walk"], Run = prep.Options.Movement["run"] };
            if (prep.Catalog.Entries.Keys.Single() != t.GetProperty("TemplateCrc").GetUInt32()) throw new InvalidDataException("independent CRC mismatch");
            world = new(prep.Catalog, new Dictionary<SceneObjectResourceRecipe, SceneObjectResourceOptions>
                { [SceneObjectResourceRecipe.CreatureDetail0] = prep.Options },
                (request, token) => Task.Run(() =>
                {
                    var result = SceneObjectResourceRouter.Load(request.Snapshot.Create, request.Catalog,
                        request.Options, inputs.BorrowedOverlay.Lookup, token); loaded = result; return result;
                }, token), SceneCreateZeroQuaternionPolicy.Reject, replicationPolicy: new(
                    SceneWorldReplicationMode.CreatureView3Scalars, new(1024, 1024, 1024, 128, 16384), 16, 16384));
            session = new(new LoginClientOptions("login.invalid", 44452, "20260817-SD:01"));
            login = new(t, false, new(0xC0000201, 44452));
            zone = new(t, true, new(0xC0000202, 44453));
            driver = new(session, login, login.Peer);
            driver.StartLogin(0x12345678, 0);
            environment = new Godot.Environment { BackgroundMode = Godot.Environment.BGMode.Color,
                BackgroundColor = new Color("#14202e"), AmbientLightSource = Godot.Environment.AmbientSource.Color,
                AmbientLightColor = Colors.White, AmbientLightEnergy = .8f };
            lighting = new WorldEnvironment { Environment = environment }; AddChild(lighting);
            sun = new DirectionalLight3D { RotationDegrees = new Vector3(-30, -30, 0), LightEnergy = 1 }; AddChild(sun);
            camera = new Camera3D { Position = new Vector3(1.25f, 3.3f, -4f), Fov = 40, Current = true };
            AddChild(camera); camera.LookAt(new Vector3(1.25f, 3.3f, 3.75f));
            GetViewport().GetWindow().Size = new Vector2I(800, 600);
        }
        catch (Exception error) { Failed(error); }
    }
    public override void _Process(double delta)
    {
        frame++;
        try
        {
            if (closing) { FinishWhenDrained(); return; }
            if (frame > 20000) throw new TimeoutException("offline frame budget");
            if (presentation is null) UpdateSession(delta);
            if (closing) return;
            if (world!.State == ZoneSceneTransitionState.Initializing)
            {
                staged = Math.Max(staged, world.StagedCount);
                if (staged >= 3)
                {
                    if (bridge!.ReadySubmissions != 0 || presentation!.PlayerPublished) throw new InvalidDataException("early ready/publication");
                    if (mode == "cancel") { BeginClose(); return; }
                    // This is a modern bounded minimum provider: original PTAT decode plus a live scene owner.
                    // It does not assert full original procedural surface readiness.
                    if (frame > 60) minimumGate.TrySetResult(true);
                }
            }
            if (presentation?.Failure is { } nativeFailure) throw nativeFailure;
            if (presentation?.PlayerPublished == true && !published)
            {
                if (world.State != ZoneSceneTransitionState.Ready || staged < 3 || bridge!.ReadySubmissions != 1 || unhandled != 1)
                    throw new InvalidDataException("joined readiness/order/sentinel");
                var visual = (SceneWorldObjectVisual)presentation.Pump.Consumer.Find(ObjectId)!;
                var creature = visual.CreatureVisual ?? throw new InvalidDataException("selected creature missing");
                if (!creature.Verify(DisplayServer.GetName() != "headless")) throw new InvalidDataException("original geometry/upload verification");
                var expected = new Vector3(1.25f, 2.5f, 3.75f);
                if (creature.Root.GlobalPosition.DistanceTo(expected) > .0001f || !creature.Root.IsVisibleInTree())
                    throw new InvalidDataException("original-create placement/visibility");
                if (world.Snapshots().Single().Object.Handle.ObjectId != ObjectId) throw new InvalidDataException("selected identity");
                if (world.Snapshots().Single().Replication?.Baseline.ObjectId != ObjectId)
                    throw new InvalidDataException("typed selected replicated identity");
                publishedRootId = creature.Root.GetInstanceId(); published = true; publishedAt = frame;
            }
            if (published && frame - publishedAt > 12)
            {
                if (DisplayServer.GetName() != "headless")
                {
                    using var image = GetViewport().GetTexture().GetImage();
                    Error error = image.SavePng(Path.Combine(output, "selected-player.png"));
                    if (error != Error.Ok) throw new IOException("viewport save failed");
                    saved = true;
                }
                BeginClose();
            }
        }
        catch (Exception error) { Failed(error); }
    }
    private void UpdateSession(double seconds)
    {
        long now = frame * 10;
        foreach (var exchange in driver!.Tick(now))
        {
            if (exchange.FailureType is not null) throw new InvalidDataException(exchange.FailureType);
            if (exchange.Datagrams.Count != 0) throw new InvalidDataException("driver wire ownership");
            if (bridge is null)
            {
                if (exchange.ZoneApplications.Count != 0) throw new InvalidDataException("premature application");
            }
            else
            {
                var remainder = bridge.ObserveExchange(exchange);
                if (remainder.FailureType is not null) throw new InvalidDataException(remainder.FailureType);
                foreach (byte[] app in remainder.ZoneApplications) { unhandled++; CryptographicOperations.ZeroMemory(app); }
                driver.Submit(remainder);
            }
        }
        if (!authenticated && session!.Phase == ReplacementClientPhase.LoginAuthentication)
        { authenticated = true; driver.Authenticate("synthetic", "fixture", now); }
        if (!selected && session!.Phase == ReplacementClientPhase.CharacterList)
        {
            selected = true; driver.BeginZone(ObjectId, 0x87654321, zone!, zone!.Peer, now);
            bridge = new(session, world!, MinimumResources); bridge.BindSelection();
            presentation = new ReplacementPlayerPresentationHost(); AddChild(presentation);
            presentation.Configure(world!, ObjectId, UpdateSession);
        }
        if (bridge is not null)
        {
            try { driver.Submit(bridge.Poll(now)); }
            catch (Exception error) when (mode == "terrain-failure" && terrainBytes == -1)
            { failure = "expected-minimum-resource-failure:" + error.GetType().Name; BeginClose(); }
        }
    }
    private async Task MinimumResources(CmdStartSceneMessage message, CancellationToken token)
    {
        await minimumGate.Task.WaitAsync(token).ConfigureAwait(false);
        await Task.Run(() =>
        {
            token.ThrowIfCancellationRequested();
            if (mode == "terrain-failure") { terrainBytes = -1; throw new InvalidDataException("authored missing minimum terrain"); }
            var source = inputs!.BorrowedOverlay.Lookup(message.Terrain);
            if (source.Kind != TreLookupKind.Present || source.Payload is null) throw new InvalidDataException("original terrain missing");
            _ = ProceduralTerrainInput.Decode(source.Payload); terrainBytes = source.Payload.Length;
            terrainHash = Convert.ToHexString(SHA256.HashData(source.Payload));
            token.ThrowIfCancellationRequested();
        }, token).ConfigureAwait(false);
    }
    private void Failed(Exception error)
    { failure = error.GetType().Name + ":" + error.Message; GD.PrintErr(failure); BeginClose(); }
    private void BeginClose()
    {
        if (closing) return; closing = true;
        driver?.Dispose(); bridge?.Dispose();
        if (presentation is not null) presentation.RequestClose(); else world?.Close();
    }
    private void FinishWhenDrained()
    {
        if (driver?.DrainCompletion.IsCompleted == false || bridge?.DrainCompletion.IsCompleted == false ||
            (presentation is null ? world?.DrainCompletion.IsCompleted == false : !presentation.Completion.IsCompleted)) return;
        bool expected = mode switch { "success" => published && failure is null && terrainBytes > 0,
            "cancel" => !published && failure is null && bridge?.ReadySubmissions == 0,
            _ => !published && failure?.StartsWith("expected-minimum-resource-failure:", StringComparison.Ordinal) == true && bridge?.ReadySubmissions == 0 };
        bool clean = login?.Disposed == true && zone?.Disposed == true && world?.OutstandingJobs == 0 &&
            (presentation is null || presentation.Pump.Consumer.Count == 0) &&
            driver?.DrainCompletion.IsCompletedSuccessfully == true &&
            bridge?.DrainCompletion.IsCompletedSuccessfully == true &&
            (presentation is null || presentation.Completion.IsCompletedSuccessfully) &&
            (publishedRootId == 0 || GodotObject.InstanceFromId(publishedRootId) is null);
        session?.Dispose(); inputs?.Dispose();
        clean = clean && session?.Phase == ReplacementClientPhase.Closed && session.CredentialLength == 0 &&
            session.ZonePendingMessages == 0 && session.ZoneRetainedPackets == 0;
        if (output.Length != 0)
        {
            var receipt = new { Passed = expected && clean, Mode = mode, SyntheticPeer = true, OriginalResources = true,
                LiveCompatibility = false, FullTerrainReadiness = false, AuthoredTypedCreo3 = true,
                CompleteServerBaselineSet = false, Published = published, Screenshot = saved,
                Staged = staged, ReadySubmissions = bridge?.ReadySubmissions ?? 0, Unhandled = unhandled,
                TerrainBytes = terrainBytes, Clean = clean, Failure = failure, Frames = frame };
            File.WriteAllText(Path.Combine(output, "receipt.json"), JsonSerializer.Serialize(receipt, new JsonSerializerOptions { WriteIndented = true }) + "\n");
            File.WriteAllText(Path.Combine(output, "captured.json"), JsonSerializer.Serialize(zone?.Captured ?? new Dictionary<string,string>()) + "\n");
            File.WriteAllText(Path.Combine(output, "resources.json"), JsonSerializer.Serialize(new
                { Preparation = preparationReceipt, ClosureSources = loaded?.Sources, TerrainSha256 = terrainHash }) + "\n");
        }
        loaded = null; camera?.Free(); sun?.Free(); lighting?.Free(); environment?.Dispose();
        transcript?.Dispose(); GetTree().Quit(expected && clean ? 0 : 1); SetProcess(false);
    }
    private sealed class TestPort(JsonElement transcript, bool isZone, DatagramPeer peer) : IDatagramPort
    {
        public DatagramPeer Peer => peer;
        public bool Disposed { get; private set; }
        public Dictionary<string,string> Captured { get; } = new();
        private readonly Queue<byte[]> queue = new();
        private TaskCompletionSource<InboundDatagram>? pending;
        private int applications;
        private bool handshake;
        private void Enqueue(string hex)
        {
            byte[] bytes = Convert.FromHexString(hex);
            if (pending is { } p) { pending = null;p.TrySetResult(new(peer, bytes)); }
            else queue.Enqueue(bytes);
        }
        public ValueTask SendAsync(DatagramPeer destination, ReadOnlyMemory<byte> bytes, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (Disposed || destination != peer) throw new InvalidOperationException("test port pin/ownership");
            ReadOnlySpan<byte> wire = bytes.Span;
            if (wire.Length >= 2 && wire[0] == 0 && wire[1] == 1 && !handshake)
            { handshake = true; Enqueue(transcript.GetProperty(isZone ? "ZoneResponse" : "LoginResponse").GetString()!); }
            else if (wire.Length >= 4 && wire[0] == 0 && wire[1] == 9)
            {
                // Authored dynamic peer responds to actual emitted application frames. No sockets.
                // Sequence bytes are protected on wire. Never interpret them as plaintext here.
                if (!isZone)
                {
                    if (applications != 0) return ValueTask.CompletedTask;
                    applications++;
                    foreach (var value in transcript.GetProperty("LoginReplies").EnumerateArray()) Enqueue(value.GetString()!);
                }
                else
                {
                    int seq = applications;
                    string captured = Convert.ToHexString(wire);
                    if (Captured.Values.Contains(captured)) return ValueTask.CompletedTask;
                    string expected = seq switch { 0 => "ExpectedAuthWire", 1 => "ExpectedSelectWire", 2 => "ExpectedReadyWire", _ => throw new InvalidDataException("unexpected application") };
                    if (captured != transcript.GetProperty(expected).GetString()) throw new InvalidDataException("independent protected application mismatch");
                    applications++;
                    string key = seq switch { 0 => "AuthWire", 1 => "SelectWire", 2 => "ReadyWire", _ => throw new InvalidDataException("unexpected application") };
                    Captured.Add(key, captured);
                    Enqueue(transcript.GetProperty("ZoneAcks")[seq].GetString()!);
                    if (seq == 0) Enqueue(transcript.GetProperty("Permissions").GetString()!);
                    if (seq == 1) foreach (var value in transcript.GetProperty("WorldFrames").EnumerateArray()) Enqueue(value.GetString()!);
                    if (seq == 2) Enqueue(transcript.GetProperty("ServerReady").GetString()!);
                }
            }
            return ValueTask.CompletedTask;
        }
        public ValueTask<InboundDatagram> ReceiveAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (Disposed || pending is not null) throw new InvalidOperationException("test receive ownership");
            if (queue.Count != 0) return ValueTask.FromResult(new InboundDatagram(peer, queue.Dequeue()));
            pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
            var task = pending.Task;
            var registration = token.Register(() => pending?.TrySetCanceled(token));
            return new(Wait(task, registration));
        }
        private static async Task<InboundDatagram> Wait(Task<InboundDatagram> task, CancellationTokenRegistration registration)
        { try { return await task.ConfigureAwait(false); } finally { registration.Dispose(); } }
        public ValueTask DisposeAsync()
        {
            Disposed = true;pending?.TrySetCanceled();pending = null;
            while (queue.Count != 0) CryptographicOperations.ZeroMemory(queue.Dequeue());
            return ValueTask.CompletedTask;
        }
    }
}
