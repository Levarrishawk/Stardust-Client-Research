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

/// <summary>Actual host controls with authored offline peers; no physical OS input or sockets.</summary>
public partial class HostVerification : Node3D
{
    private const ulong ObjectId = 0x1122334455667788;
    private ReplacementClientHost host = null!;
    private ReplacementClientApplication app = null!;
    private ReplacementClientSession session = null!;
    private ReplacementClientRun run = null!;
    private SceneWorldCoordinator world = null!;
    private TestPort login = null!, zone = null!;
    private JsonDocument transcript = null!;
    private string output = "", mode = "success";
    private long frame, readyWaitingAt, movementAt, closeAt;
    private bool authenticated, selected, unsupportedRejected, passwordCleared, moved, closed, published, saved, stopped;
    private bool configureRejected, threadRejected, finalizedReadsRejected, preReadyRejected;
    private int observedMovement;
    private ulong actorId;
    private string? failure;
    private object? preparation;
    private SceneObjectResources? resources;
    private string? profileHash, terrainHash;
    private int terrainBytes;
    private readonly TaskCompletionSource<bool> minimum = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private Camera3D camera = null!;
    private readonly List<float[]> positions = new();
    private readonly List<long> sendTimes = new();
    private int closeRoots;

    public override void _Ready()
    {
        try
        {
            var args = OS.GetCmdlineUserArgs();
            if (args.Length != 6) throw new ArgumentException("explicit root/config/transcript/output/mode/profile required");
            output = args[3]; mode = args[4]; Directory.CreateDirectory(output);
            if (mode is not ("success" or "cancel" or "terrain-failure" or "send-error" or "send-timeout" or "stop" or "window-close" or "native-exit")) throw new ArgumentException("mode");
            transcript = JsonDocument.Parse(File.ReadAllBytes(args[2])); var t = transcript.RootElement;
            byte[] profileBytes = File.ReadAllBytes(args[5]); profileHash = Convert.ToHexString(SHA256.HashData(profileBytes));
            var profile = ReplacementLaunchProfileCodec.Decode(profileBytes);
            if (!string.Equals(Path.GetFullPath(profile.AssetRoot), Path.GetFullPath(args[0]), StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(Path.GetFullPath(profile.ConfigurationPath), Path.GetFullPath(args[1]), StringComparison.OrdinalIgnoreCase) ||
                profile.ObjectPath != t.GetProperty("ObjectPath").GetString()) throw new InvalidDataException("explicit profile mismatch");
            login = new(t, false, new(0xC0000201,44452)); zone = new(t, true, new(0xC0000202,44453),mode);
            app = new(profile, new LoginClientOptions("login.invalid",44452,"20260817-SD:01"), login.Peer, zone.Peer,
                (connection, peer) => peer == login.Peer ? login : peer == zone.Peer ? zone : throw new InvalidDataException("peer pin"),
                Minimum, new(1000,10), endpointApproval: new(
                    new("login.invalid",login.Peer.Address,login.Peer.Port),
                    new("zone.invalid",zone.Peer.Address,zone.Peer.Port),7));
            session = app.Session; run = app.Run; world = app.World;
            var prep = app.Creature;
            preparation = new { Catalog=prep.Catalog.Entries.Values.Single(), prep.Sources, Idle=prep.Options.Idle, Walk=prep.Options.Movement["walk"], Run=prep.Options.Movement["run"] };
            var options = new ReplacementClientHostOptions(t.GetProperty("LoginId").GetUInt32(),t.GetProperty("ZoneId").GetUInt32(),true,
                new(ObjectId,true,true,true),1000);
            host = new ReplacementClientHost { Name="ClientHost" };
            try { host.Configure(app,options,()=>frame*10); }
            catch (InvalidOperationException) { configureRejected = !app.Closing && !login.Disposed; }
            AddChild(host);
            Task.Run(() => { try { host.Configure(app,options,()=>frame*10); } catch (InvalidOperationException) { threadRejected=true; } }).GetAwaiter().GetResult();
            host.Configure(app,options,()=>frame*10);
            if (!configureRejected || !threadRejected || session.Phase!=ReplacementClientPhase.Offline || !host.PasswordField.Secret) throw new InvalidDataException("configuration boundary");
            host.LoginButton.EmitSignal(Button.SignalName.Pressed);
            var environment = new Godot.Environment { BackgroundMode=Godot.Environment.BGMode.Color, BackgroundColor=new Color("#14202e"),
                AmbientLightSource=Godot.Environment.AmbientSource.Color, AmbientLightColor=Colors.White, AmbientLightEnergy=.8f };
            AddChild(new WorldEnvironment { Environment=environment });
            AddChild(new DirectionalLight3D { RotationDegrees=new Vector3(-30,-30,0),LightEnergy=1 });
            camera = new Camera3D { Position=new Vector3(1.25f,3.3f,-4),Fov=40,Current=true }; AddChild(camera); camera.LookAt(new Vector3(1.25f,3.3f,3.75f));
            GetViewport().GetWindow().Size=new Vector2I(1000,700);
        }
        catch (Exception error) { Failed(error); }
    }
    public override void _Process(double delta)
    {
        frame++;
        try
        {
            if (frame>20000) throw new TimeoutException("offline frame budget");
            if (host is null) { GetTree().Quit(1); return; }
            if (closed)
            {
                if (host.Completion.IsCompleted) Finish();
                return;
            }
            if (host.FailureType is { } kind)
            {
                if (mode is not ("terrain-failure" or "send-error" or "send-timeout")) throw new InvalidDataException(kind);
                closed=true; return;
            }
            if (!authenticated && session.Phase==ReplacementClientPhase.LoginAuthentication)
            {
                authenticated=true; host.UsernameField.Text="synthetic";host.PasswordField.Text="fixture";
                host.AuthenticateButton.EmitSignal(Button.SignalName.Pressed); passwordCleared=host.PasswordField.Text.Length==0;
            }
            if (!selected && session.Phase==ReplacementClientPhase.CharacterList && host.Characters.ItemCount==2)
            {
                host.Characters.Select(1);host.SelectButton.EmitSignal(Button.SignalName.Pressed);
                unsupportedRejected=host.SelectionReason.StartsWith("Unsupported selection:",StringComparison.Ordinal)&&run.Generation==0&&session.Phase==ReplacementClientPhase.CharacterList;
                if (!unsupportedRejected) throw new InvalidDataException("unsupported selection mutated application");
                host.Characters.Select(0);host.SelectButton.EmitSignal(Button.SignalName.Pressed);selected=true;
            }
            if (world.State==ZoneSceneTransitionState.Initializing && world.StagedCount>=5)
            {
                if (mode=="cancel") { CloseButton(); return; }
                minimum.TrySetResult(true);
            }
            if (zone.ReadyWaiting)
            {
                if (readyWaitingAt==0)
                {
                    readyWaitingAt=frame;
                    if (run.ReadySubmissions!=1 || world.State!=ZoneSceneTransitionState.AwaitingServerReady || !host.MoveButton.Disabled) throw new InvalidDataException("local/received readiness conflated");
                    host.MoveButton.EmitSignal(Button.SignalName.Pressed);
                    preReadyRejected=host.RootsQueued==0;
                }
                if (frame-readyWaitingAt>=30) zone.ReleaseReady();
            }
            if (host.HasPublishedPlayer && host.ReceivedServerReady)
            {
                var visual=host.Presentation!.SelectedVisual!; var actor=visual.CreatureVisual!;
                if (!published)
                {
                    if (!host.ReceivedServerReady || !preReadyRejected || !actor.Verify(DisplayServer.GetName()!="headless")) throw new InvalidDataException("original publication/readiness");
                    actorId=actor.Root.GetInstanceId();resources=visual.Publication.Resources;published=true;
                }
                if (actor.Root.GetInstanceId()!=actorId) throw new InvalidDataException("actor replaced during input");
                int count=host.RootsQueued;
                if (count>observedMovement)
                {
                    var row=app.Motions.Find(world.Snapshots().Single())!;var p=row.Transform.Translation;
                    if (actor.Root.GlobalPosition.DistanceTo(new(p.X,p.Y,-p.Z))>.00001f) return;
                    observedMovement=count;positions.Add(new[]{actor.Root.GlobalPosition.X,actor.Root.GlobalPosition.Y,actor.Root.GlobalPosition.Z});sendTimes.Add(frame*10);
                    if (mode=="stop" && count==1) { host.StopButton.EmitSignal(Button.SignalName.Pressed); stopped=true;movementAt=frame; }
                }
                if (!moved && !host.MoveButton.Disabled)
                {
                    moved=true;host.UsernameField.ReleaseFocus();host.PasswordField.ReleaseFocus();
                    using var key=new InputEventKey { Keycode=Key.W,PhysicalKeycode=Key.W,Pressed=true };
                    Input.ParseInputEvent(key);
                }
                if (host.RootsQueued==3 && zone.Captured.ContainsKey("Movement2") && movementAt==0) movementAt=frame;
                if (movementAt!=0 && frame-movementAt>220)
                {
                    if (mode=="stop" && host.RootsQueued!=1) throw new InvalidDataException("stopped pacer emitted more roots");
                    if (DisplayServer.GetName()!="headless")
                    {
                        using var image=GetViewport().GetTexture().GetImage();
                        if(image.SavePng(Path.Combine(output,"client-host.png"))!=Error.Ok)throw new IOException("screenshot"); saved=true;
                    }
                    closeRoots=host.RootsQueued;closeAt=frame;
                    if (mode=="native-exit") { closed=true;RemoveChild(host); }
                    else if (mode=="window-close") { closed=true;host._Notification((int)NotificationWMCloseRequest); }
                    else CloseButton();
                }
            }
        }
        catch (Exception error) { Failed(error); }
    }
    private void CloseButton() { closed=true;closeAt=frame;host.CloseButton.EmitSignal(Button.SignalName.Pressed); }
    private async Task Minimum(CmdStartSceneMessage message,ProceduralTerrainInput decoded,Func<string,TreLookupResult> lookup,CancellationToken token)
    {
        await minimum.Task.WaitAsync(token).ConfigureAwait(false);
        if (mode=="terrain-failure") { terrainBytes=-1;throw new InvalidDataException("authored minimum provider failure"); }
        var original=lookup(message.Terrain);token.ThrowIfCancellationRequested();
        if(original.Kind!=TreLookupKind.Present||original.Payload is null||decoded.Version!=ProceduralTerrainInput.Decode(original.Payload).Version)throw new InvalidDataException("original PTAT");
        terrainBytes=original.Payload.Length;terrainHash=Convert.ToHexString(SHA256.HashData(original.Payload));
    }
    private void Failed(Exception error)
    {
        failure??=error.GetType().Name;GD.PrintErr(failure + ":" + error.StackTrace);
        closed=true;
        if(host is not null && !host.IsClosing)host.RequestClose();
    }
    private void Finish()
    {
        try { _=host.Application; } catch(ObjectDisposedException) { finalizedReadsRejected=true; }
        if(host.Completion.IsFaulted)_=host.Completion.Exception;
        bool expected=mode switch
        {
            "cancel"=>!published&&run.ReadySubmissions==0&&host.FailureType is null&&run.DisconnectSendCompleted,
            "terrain-failure"=>!published&&run.ReadySubmissions==0&&host.FailureType=="InvalidDataException"&&terrainBytes==-1,
            "send-error"=>published&&host.RootsQueued==3&&host.FailureType=="IOException"&&run.DisconnectSubmitted&&!run.DisconnectSendCompleted,
            "send-timeout"=>published&&host.RootsQueued==3&&host.FailureType=="TimeoutException"&&run.DisconnectSubmitted&&!run.DisconnectSendCompleted,
            "stop"=>published&&stopped&&host.RootsQueued==1&&run.DisconnectSendCompleted,
            "native-exit"=>published&&closeRoots==3&&host.FailureType is null,
            _=>published&&host.RootsQueued==3&&host.FailureType is null&&run.DisconnectSendCompleted
        };
        bool observedProviderFault=mode=="terrain-failure"&&host.CleanupFailures.SequenceEqual(new[]{"InvalidDataException"})&&!app.DrainCompletion.IsCompletedSuccessfully;
        bool clean=host.IsFinalized&&finalizedReadsRejected&&login.Disposed&&zone.Disposed&&world.OutstandingJobs==0&&session.Phase==ReplacementClientPhase.Closed&&session.CredentialLength==0&&
            (actorId==0||GodotObject.InstanceFromId(actorId) is null)&&(host.CleanupFailures.Count==0&&app.DrainCompletion.IsCompletedSuccessfully||observedProviderFault);
        var receipt=new {Passed=expected&&clean&&failure is null&&configureRejected&&threadRejected&&passwordCleared&&unsupportedRejected,
            Mode=mode,SyntheticPeer=true,OriginalResources=true,PhysicalOSInput=false,LiveCompatibility=false,FullTerrainReadiness=false,Published=published,Screenshot=saved,
            Clean=clean,ObservedProviderFault=observedProviderFault,ApplicationCleanupFailures=host.CleanupFailures,ApplicationDrainSucceeded=app.DrainCompletion.IsCompletedSuccessfully,OwnershipFinalized=host.IsFinalized,Failure=failure,HostFailureType=host.FailureType,ConfigureRejectedRetainsCaller=configureRejected,WrongThreadRejected=threadRejected,
            PasswordCleared=passwordCleared,UnsupportedSelectionRejected=unsupportedRejected,PreServerReadyMovementRejected=preReadyRejected,ReceivedServerReady=host.ReceivedServerReady,
            TerrainBytes=terrainBytes,ReadySubmissions=host.ReadySubmissions,RootsQueued=host.RootsQueued,ActorIdentity=actorId,CompletionSucceeded=host.Completion.IsCompletedSuccessfully,FinalizedBorrowRejected=finalizedReadsRejected,
            DisconnectSubmitted=run.DisconnectSubmitted,DisconnectSendCompleted=run.DisconnectSendCompleted,CloseFrames=frame-closeAt,DeferredApplications=host.DeferredApplications};
        File.WriteAllText(Path.Combine(output,"receipt.json"),JsonSerializer.Serialize(receipt,new JsonSerializerOptions{WriteIndented=true})+"\n");
        File.WriteAllText(Path.Combine(output,"captured.json"),JsonSerializer.Serialize(zone.Captured)+"\n");
        File.WriteAllText(Path.Combine(output,"observations.json"),JsonSerializer.Serialize(new{Positions=positions,Times=sendTimes,login.LoginWireVerified})+"\n");
        File.WriteAllText(Path.Combine(output,"resources.json"),JsonSerializer.Serialize(new{Preparation=preparation,ClosureSources=resources?.Sources,TerrainSha256=terrainHash,LaunchProfileSha256=profileHash})+"\n");
        if (GodotObject.IsInstanceValid(host) && !host.IsInsideTree()) host.Free();
        transcript.Dispose();GetTree().Quit(receipt.Passed?0:1);SetProcess(false);
    }
    private sealed class TestPort(JsonElement transcript, bool isZone, DatagramPeer peer, string mode="success") : IDatagramPort
    {
        public DatagramPeer Peer => peer;
        public bool Disposed { get; private set; }
        public bool LoginWireVerified { get; private set; }
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
        public bool ReadyWaiting { get; private set; }
        public void ReleaseReady()
        {
            if (!ReadyWaiting) throw new InvalidOperationException("ready release order");
            ReadyWaiting = false; Enqueue(transcript.GetProperty("ServerReady").GetString()!);
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
                if(!isZone || applications<2 || applications>6 || Captured.ContainsKey("DisconnectWire"))throw new InvalidDataException("disconnect sequence");
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
                    if (Convert.ToHexString(wire) != transcript.GetProperty("ExpectedLoginWire").GetString()) throw new InvalidDataException("independent login wire mismatch");
                    LoginWireVerified = true;
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
                    if (seq == 2) ReadyWaiting = true;
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
