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
    private ReplacementLaunchPreparation? launch;
    private string? profileHash;
    private ReplacementClientSession? session;
    private ReplacementDatagramPump? driver;
    private ReplacementClientRun? run;
    private readonly List<string> deferred = new();
    private readonly List<string> movementDeferred = new();
    private readonly List<object> localMotions = new(), corrections = new();
    private readonly List<float[]> localPositions = new(), correctionPositions = new();
    private SceneWorldObjectMotionStore? motions;
    private SceneWorldLocalMotionDriver? localDriver;
    private int localIndex, correctionIndex, movementRemainders;
    private bool localPending, correctionPending;
    private long movementDoneAt;
    private bool protocolClosing;
    private object? selectedModels;
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
            if (args.Length != 6) throw new ArgumentException("root cfg transcript private-output mode launch-profile required");
            output = args[3]; mode = args[4];
            if (mode is not ("success" or "cancel" or "terrain-failure" or "malformed-correction" or "send-error" or "send-timeout")) throw new ArgumentException("mode");
            Directory.CreateDirectory(output);
            transcript = JsonDocument.Parse(File.ReadAllBytes(args[2]));
            var t = transcript.RootElement;
            string objectPath = t.GetProperty("ObjectPath").GetString()!;
            byte[] profileBytes = File.ReadAllBytes(args[5]);
            profileHash = Convert.ToHexString(SHA256.HashData(profileBytes));
            var profile = ReplacementLaunchProfileCodec.Decode(profileBytes);
            if (!string.Equals(Path.GetFullPath(profile.AssetRoot),Path.GetFullPath(args[0]),StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(Path.GetFullPath(profile.ConfigurationPath),Path.GetFullPath(args[1]),StringComparison.OrdinalIgnoreCase) ||
                profile.ObjectPath != objectPath) throw new InvalidDataException("profile explicit input mismatch");
            launch = ReplacementLaunchPreparation.Open(profile);
            inputs = launch.Inputs;
            var prep = launch.Creature;
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
                    SceneWorldReplicationMode.CreatureView3Scalars, new(1024, 1024, 1024, 128, 16384), 16, 16384),
                creatureView4FullPolicy: new(new(MaximumPayloadBytes: 1024), new(MaximumPayloadBytes: 1024), new(), new(MaximumObjects: 2)),
                creatureView6FullPolicy: new(new(MaximumPayloadBytes: 1024), new(MaximumPayloadBytes: 1024), new(), new(MaximumObjects: 2)));
            session = new(new LoginClientOptions("login.invalid", 44452, "20260817-SD:01"));
            login = new(t, false, new(0xC0000201, 44452));
            zone = new(t, true, new(0xC0000202, 44453),mode);
            driver = new(session, login, login.Peer);
            motions = new(world);
            run = new(session, driver, world, motions, MinimumResources, new(1000,10), SceneCreateZeroQuaternionPolicy.Reject);
            run.StartLogin(0x12345678, 0);
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
            if (protocolClosing) { UpdateDisconnect(); return; }
            if (frame > 20000) throw new TimeoutException("offline frame budget");
            if (presentation is null) UpdateSession(delta);
            if (closing) return;
            if (world!.State == ZoneSceneTransitionState.Initializing)
            {
                staged = Math.Max(staged, world.StagedCount);
                if (staged >= 5)
                {
                    if (run!.ReadySubmissions != 0 || presentation!.PlayerPublished) throw new InvalidDataException("early ready/publication");
                    if (mode == "cancel") { BeginClose(); return; }
                    // This is a modern bounded minimum provider: original PTAT decode plus a live scene owner.
                    // It does not assert full original procedural surface readiness.
                    if (frame > 60) minimumGate.TrySetResult(true);
                }
            }
            if (presentation?.Failure is { } nativeFailure) throw nativeFailure;
            if (presentation?.PlayerPublished == true && !published)
            {
                if (world.State != ZoneSceneTransitionState.Ready || staged < 5 || run!.ReadySubmissions != 1 || unhandled != transcript!.RootElement.GetProperty("ExpectedDeferred").GetArrayLength())
                    throw new InvalidDataException("joined readiness/order/sentinel");
                var visual = presentation.SelectedVisual ?? throw new InvalidDataException("selected borrowed visual missing");
                var creature = visual.CreatureVisual ?? throw new InvalidDataException("selected creature missing");
                if (!creature.Verify(DisplayServer.GetName() != "headless")) throw new InvalidDataException("original geometry/upload verification");
                var expected = new Vector3(1.25f, 2.5f, 3.75f);
                if (creature.Root.GlobalPosition.DistanceTo(expected) > .0001f || !creature.Root.IsVisibleInTree())
                    throw new InvalidDataException("original-create placement/visibility");
                if (world.Snapshots().Single().Object.Handle.ObjectId != ObjectId) throw new InvalidDataException("selected identity");
                if (world.Snapshots().Single().Replication?.Baseline.ObjectId != ObjectId)
                    throw new InvalidDataException("typed selected replicated identity");
                var snapshot = world.Snapshots().Single();
                if (snapshot.CreatureView4Full is null || snapshot.CreatureView6Full is null) throw new InvalidDataException("missing typed full state");
                selectedModels = Networking85Observation.Project(snapshot.CreatureView4Full.Model, snapshot.CreatureView6Full.Model);
                publishedRootId = creature.Root.GetInstanceId(); published = true; publishedAt = frame;
                localDriver = new(world, motions!, ObjectId, 0, 100000, 1);
                localDriver.Submit(0, new(new System.Numerics.Vector3(0,0,2), null));
            }
            if (published && (mode is "success" or "malformed-correction" or "send-error" or "send-timeout")) AdvanceMovement();
            if (published && movementDoneAt != 0 && frame - movementDoneAt > 12)
            {
                if (DisplayServer.GetName() != "headless")
                {
                    using var image = GetViewport().GetTexture().GetImage();
                    Error error = image.SavePng(Path.Combine(output, "selected-player.png"));
                    if (error != Error.Ok) throw new IOException("viewport save failed");
                    saved = true;
                }
                protocolClosing=true;
                run!.BeginDisconnect(frame*10,1000);
            }
        }
        catch (Exception error) { Failed(error); }
    }
    private void UpdateDisconnect()
    {
        var tick=run!.Tick(frame*10);
        if(tick.UnhandledApplications.Count!=0) throw new InvalidDataException("disconnect exposed work");
        if(tick.FailureType is { } failureType)
        { failure="expected-disconnect-failure:"+failureType; BeginClose(); return; }
        if(run.DisconnectSendCompleted) BeginClose();
    }
    private void UpdateSession(double seconds)
    {
        if(protocolClosing)return;
        long now=frame*10;
        var tick=run!.Tick(now);
        if(tick.FailureType is { } failureType)
        {
            if(mode=="terrain-failure" && terrainBytes==-1)
            { failure="expected-minimum-resource-failure:"+failureType; BeginClose(); return; }
            if(mode=="malformed-correction")
            { failure="expected-ingress-failure:"+failureType; BeginClose(); return; }
            throw new InvalidDataException(failureType);
        }
        foreach(byte[] app in tick.UnhandledApplications)
        {
            string hex=Convert.ToHexString(app);
            if(localIndex==3) {movementRemainders++;movementDeferred.Add(hex);}
            else {unhandled++;deferred.Add(hex);}
            CryptographicOperations.ZeroMemory(app);
        }
        if(!authenticated && session!.Phase==ReplacementClientPhase.LoginAuthentication)
        {authenticated=true;run.Authenticate("synthetic","fixture",now);}
        if(!selected && session!.Phase==ReplacementClientPhase.CharacterList)
        {
            selected=true;run.SelectCharacter(ObjectId,0x87654321,zone!,zone!.Peer,now);
            presentation=new ReplacementPlayerPresentationHost();AddChild(presentation);
            presentation.Configure(world!,ObjectId,UpdateSession,motions);
        }
    }
    private static float[] ObservePosition(Godot.Vector3 value) => new[] {value.X,value.Y,value.Z};
    private bool ActorAtMotion(SceneWorldObjectMotionSnapshot row)
    {
        var creature = presentation!.SelectedVisual?.CreatureVisual ?? throw new InvalidDataException("moving actor missing");
        if (creature.Root.GetInstanceId() != publishedRootId) throw new InvalidDataException("actor identity changed");
        var p = row.Transform.Translation;
        return creature.Root.GlobalPosition.DistanceTo(new(p.X,p.Y,-p.Z)) < .00001f;
    }
    private void AdvanceMovement()
    {
        var snapshot = world!.Snapshots().Single();
        var t = transcript!.RootElement;
        if (localIndex < 3)
        {
            if (!localPending)
            {
                localDriver!.Advance((localIndex+1)*100000);
                localPending = true;
            }
            var row = motions!.Find(snapshot)!;
            if (!ActorAtMotion(row)) return;
            bool accepted = run!.TryQueueLocal(snapshot,row,0x23,(uint)(10+localIndex),(uint)((localIndex+1)*100),frame*10);
            if (!accepted) return;
            localMotions.Add(Networking86Observation.Project(row,motions));
            localPositions.Add(ObservePosition(presentation!.SelectedVisual!.CreatureVisual!.Root.GlobalPosition));
            localIndex++; localPending=false;
            return;
        }
        if (correctionIndex < 4)
        {
            if (!correctionPending)
            {
                zone!.ReleaseCorrection(correctionIndex);
                correctionPending = true;
                return;
            }
            if (run!.AppliedCorrections+run.CounterRejectedCorrections <= correctionIndex) return;
            var row = motions!.Find(snapshot)!;
            if (!ActorAtMotion(row)) return;
            corrections.Add(new {Name=t.GetProperty("ExpectedCorrectionObservations")[correctionIndex].GetProperty("Name").GetString(),
                Applied=run.LastInboundStatus==SceneWorldFullTransformApplicationStatus.Applied,
                Status=run.LastInboundStatus?.ToString(), Motion=Networking86Observation.Project(row,motions)});
            correctionPositions.Add(ObservePosition(presentation!.SelectedVisual!.CreatureVisual!.Root.GlobalPosition));
            correctionIndex++; correctionPending=false;
            return;
        }
        if (movementRemainders < 3)
        {
            if (!correctionPending) { zone!.ReleaseCorrection(4+movementRemainders); correctionPending=true; }
            else if (zone!.ReleasedCorrections == 4+movementRemainders) correctionPending=false;
            return;
        }
        if (movementDoneAt == 0)
        {
            movementDoneAt=frame;
            var actor=presentation!.SelectedVisual!.CreatureVisual!.Root.GlobalPosition;
            camera!.Position=actor+new Vector3(0,.8f,-7.75f);
            camera.LookAt(actor+new Vector3(0,.8f,0));
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
        run?.Dispose(); driver?.Dispose(); localDriver?.Dispose();
        if (presentation is not null) presentation.RequestClose(); else world?.Close();
    }
    private void FinishWhenDrained()
    {
        if (driver?.DrainCompletion.IsCompleted == false || run?.DrainCompletion.IsCompleted == false ||
            (presentation is null ? world?.DrainCompletion.IsCompleted == false : !presentation.Completion.IsCompleted)) return;
        bool expected = mode switch { "success" => published && failure is null && terrainBytes > 0 && localIndex==3 && correctionIndex==4 && movementRemainders==3 && driver!.DisconnectSendCompleted,
            "send-error" => published && localIndex==3 && correctionIndex==4 && movementRemainders==3 && failure=="expected-disconnect-failure:IOException" && driver!.DisconnectSubmitted && !driver.DisconnectSendCompleted,
            "send-timeout" => published && localIndex==3 && correctionIndex==4 && movementRemainders==3 && failure=="expected-disconnect-failure:TimeoutException" && driver!.DisconnectSubmitted && !driver.DisconnectSendCompleted,
            "cancel" => !published && failure is null && run?.ReadySubmissions == 0,
            "malformed-correction" => published && localIndex==3 && correctionIndex==0 && failure=="expected-ingress-failure:InvalidDataException" && run?.ReadySubmissions==1,
            _ => !published && failure?.StartsWith("expected-minimum-resource-failure:", StringComparison.Ordinal)==true && run?.ReadySubmissions==0 };
        bool clean = login?.Disposed == true && zone?.Disposed == true && world?.OutstandingJobs == 0 &&
            (presentation is null || presentation.Pump.Consumer.Count == 0) &&
            driver?.DrainCompletion.IsCompletedSuccessfully == true &&
            run?.DrainCompletion.IsCompletedSuccessfully == true &&
            (presentation is null || presentation.Completion.IsCompletedSuccessfully) &&
            (publishedRootId == 0 || GodotObject.InstanceFromId(publishedRootId) is null);
        motions?.Dispose(); session?.Dispose(); launch?.Dispose();
        clean = clean && session?.Phase == ReplacementClientPhase.Closed && session.CredentialLength == 0 &&
            session.ZonePendingMessages == 0 && session.ZoneRetainedPackets == 0;
        if (output.Length != 0)
        {
            var receipt = new { Passed = expected && clean, Mode = mode, SyntheticPeer = true, OriginalResources = true,
                LiveCompatibility = false, FullTerrainReadiness = false, AuthoredTypedCreo3 = true,
                CompleteServerBaselineSet = false, AuthoredServerShaped = true, TypedSelectedViews = new[] {3,4,6}, Published = published, Screenshot = saved,
                Staged = staged, ReadySubmissions = run?.ReadySubmissions ?? 0, Unhandled = unhandled,
                TerrainBytes = terrainBytes, Clean = clean, Failure = failure, Frames = frame };
            File.WriteAllText(Path.Combine(output, "observations.json"), JsonSerializer.Serialize(new {
                DeferredHex = deferred, SelectedModels = selectedModels,
                AuthWire = zone!.Captured.GetValueOrDefault("AuthWire"),
                SelectWire = zone.Captured.GetValueOrDefault("SelectWire"),
                ReadyWire = zone.Captured.GetValueOrDefault("ReadyWire"),
                FailureObserved = failure is not null, ReadySubmissions = run?.ReadySubmissions ?? 0,
                PlayerPublished = published, Clean=clean, Mode=mode, TypedFailureObserved=run?.FailureType is not null,
                DisconnectWire=zone.Captured.GetValueOrDefault("DisconnectWire"),
                DisconnectRequested=driver?.DisconnectRequested ?? false,
                DisconnectSubmitted=driver?.DisconnectSubmitted ?? false,
                DisconnectSendCompleted=driver?.DisconnectSendCompleted ?? false,
                MovementWires=Enumerable.Range(0,3).Select(i=>zone.Captured.GetValueOrDefault("Movement"+i)).ToArray(),
                LocalMotions=localMotions, CorrectionObservations=corrections,
                ActorLocalPositions=localPositions, ActorCorrectionPositions=correctionPositions,
                AppliedCorrections=run?.AppliedCorrections ?? 0,
                CounterRejectedCorrections=run?.CounterRejectedCorrections ?? 0,
                UnhandledMovementHex=movementDeferred }, new JsonSerializerOptions { WriteIndented = true }) + "\n");
            File.WriteAllText(Path.Combine(output, "receipt.json"), JsonSerializer.Serialize(receipt, new JsonSerializerOptions { WriteIndented = true }) + "\n");
            File.WriteAllText(Path.Combine(output, "captured.json"), JsonSerializer.Serialize(zone?.Captured ?? new Dictionary<string,string>()) + "\n");
            File.WriteAllText(Path.Combine(output, "resources.json"), JsonSerializer.Serialize(new
                { Preparation = preparationReceipt, ClosureSources = loaded?.Sources, TerrainSha256 = terrainHash, LaunchProfileSha256 = profileHash }) + "\n");
        }
        loaded = null; camera?.Free(); sun?.Free(); lighting?.Free(); environment?.Dispose();
        transcript?.Dispose(); GetTree().Quit(expected && clean ? 0 : 1); SetProcess(false);
    }
    private sealed class TestPort(JsonElement transcript, bool isZone, DatagramPeer peer, string mode="success") : IDatagramPort
    {
        public DatagramPeer Peer => peer;
        public bool Disposed { get; private set; }
        public Dictionary<string,string> Captured { get; } = new();
        private readonly Queue<byte[]> queue = new();
        private TaskCompletionSource<InboundDatagram>? pending;
        private int applications;
        public int ReleasedCorrections {get;private set;}
        public void ReleaseCorrection(int index)
        {
            if (applications != 6 || ReleasedCorrections != index) throw new InvalidOperationException("correction release order");
            ReleasedCorrections++;
            Enqueue(transcript.GetProperty("CorrectionFrames")[index].GetString()!);
        }
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
            if(wire.Length>=2 && wire[0]==0 && wire[1]==5)
            {
                if(!isZone || applications!=6 || ReleasedCorrections!=7 || Captured.ContainsKey("DisconnectWire"))throw new InvalidDataException("disconnect sequence");
                string captured=Convert.ToHexString(wire);
                if(captured!=transcript.GetProperty("ExpectedDisconnectWire").GetString())throw new InvalidDataException("independent disconnect wire mismatch");
                Captured.Add("DisconnectWire",captured);
                if(mode=="send-error")throw new IOException("authored send failure");
                if(mode=="send-timeout")
                {
                    var completion=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                    var registration=token.Register(()=>completion.TrySetCanceled(token));
                    return new(WaitSend(completion.Task,registration));
                }
                return ValueTask.CompletedTask;
            }
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
                    string expected = seq switch { 0 => "ExpectedAuthWire", 1 => "ExpectedSelectWire", 2 => "ExpectedReadyWire", 3 or 4 or 5 => "movement", _ => throw new InvalidDataException("unexpected application") };
                    if (captured != (seq<3 ? transcript.GetProperty(expected).GetString() : transcript.GetProperty("ExpectedMovementWires")[seq-3].GetString())) throw new InvalidDataException("independent protected application mismatch");
                    applications++;
                    string key = seq switch { 0 => "AuthWire", 1 => "SelectWire", 2 => "ReadyWire", 3 or 4 or 5 => "Movement"+(seq-3), _ => throw new InvalidDataException("unexpected application") };
                    Captured.Add(key, captured);
                    Enqueue(seq<3 ? transcript.GetProperty("ZoneAcks")[seq].GetString()! : transcript.GetProperty("MovementAcks")[seq-3].GetString()!);
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
        private static async Task WaitSend(Task task,CancellationTokenRegistration registration)
        {try{await task.ConfigureAwait(false);}finally{registration.Dispose();}}
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
