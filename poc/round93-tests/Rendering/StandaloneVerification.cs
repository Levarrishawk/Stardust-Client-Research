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

/// <summary>Opt-in standalone branch with labeled spy ports; never calls UDP construction.</summary>
public partial class StandaloneVerification : Node3D
{
    private ReplacementClientBootstrap bootstrap=null!;
    private ReplacementClientHost host=null!;
    private ReplacementClientApplication app=null!;
    private TestPort login=null!,zone=null!;
    private JsonDocument transcript=null!;
    private string output="",mode="success",diagnostic="";
    private long frame,offset,readyAt,movementAt;
    private int factoryCalls,zoneFactoryCalls,negativeCases,observedRoots;
    private bool authenticated,selected,wrongIdRejected,passwordCleared,preReadyRejected,published,startedMovement,written,rollbackPending;
    private readonly TaskCompletionSource<bool> minimum=new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly List<float[]> positions=new();
    private readonly List<long> times=new();
    private ulong actorId;
    public override void _Ready()
    {
        try
        {
            var args=OS.GetCmdlineUserArgs();if(args.Length!=6)throw new ArgumentException("six explicit offline inputs");
            output=args[3];mode=args[4];Directory.CreateDirectory(output);diagnostic=Path.Combine(output,"attempt.json");
            transcript=JsonDocument.Parse(File.ReadAllBytes(args[2]));var launch=ReplacementClientBootstrap.ReadControlledLaunch(args[5]);
            if(Path.GetFullPath(launch.Profile.AssetRoot)!=Path.GetFullPath(args[0])||Path.GetFullPath(launch.Profile.ConfigurationPath)!=Path.GetFullPath(args[1]))throw new InvalidDataException("input identity");
            bool startup=mode.StartsWith("startup-",StringComparison.Ordinal);
            login=new(transcript.RootElement,false,launch.LoginPeer,startup?mode:"success");zone=new(transcript.RootElement,true,launch.ZonePeer,startup?"success":mode);
            bootstrap=new ReplacementClientBootstrap();
            string existing=Path.Combine(output,"existing.json");File.WriteAllText(existing,"{\"sentinel\":true}");
            string malformed=Path.Combine(output,"malformed.json");File.WriteAllText(malformed,"{}");
            string[] admitted={"--controlled-transport",args[5],diagnostic};
            foreach(var invalid in new[]{Array.Empty<string>(),new[]{"--controlled-transport",args[5]},new[]{"--controlled-transport",args[5],diagnostic,"extra"},new[]{"--unexpected",args[5],diagnostic},new[]{"--controlled-transport","relative.json",diagnostic},new[]{"--controlled-transport",args[5],existing},new[]{"--controlled-transport",malformed,diagnostic},new[]{"--controlled-transport",args[5],"relative.json"},new[]{"--controlled-transport",args[5],@"\\unresolved.invalid\share\attempt.json"},new[]{"--controlled-transport",args[5],Path.Combine(output,"..","attempt.json")},new[]{"--controlled-transport",args[5],Path.Combine(Path.GetTempPath(),"unapproved-attempt.json")}})
            {
                bool rejected=false;try{bootstrap.ConfigureStandaloneOffline(invalid,Factory,Clock);}catch(Exception e)when(e is InvalidDataException or IOException or ArgumentException){rejected=true;}
                if(!rejected||factoryCalls!=0)throw new InvalidDataException("pre-acquisition admission");negativeCases++;
            }
            if(Directory.Exists(Path.Combine(output,"linked")))
            {
                bool rejected=false;try{bootstrap.ConfigureStandaloneOffline(new[]{"--controlled-transport",args[5],Path.Combine(output,"linked","attempt.json")},Factory,Clock);}catch(InvalidDataException){rejected=true;}
                if(!rejected||factoryCalls!=0)throw new InvalidDataException("linked destination");negativeCases++;
            }
            bool wrongThread=false;Task.Run(()=>{try{bootstrap.ConfigureStandaloneOffline(admitted,Factory,Clock);}catch(InvalidOperationException){wrongThread=true;}}).GetAwaiter().GetResult();
            if(!wrongThread||factoryCalls!=0)throw new InvalidDataException("owner rejection");negativeCases++;
            bootstrap.ConfigureStandaloneOffline(admitted,Factory,Clock,Gate,startup?new(-1,10):null);
            if(factoryCalls!=0||!bootstrap.SyntheticOffline)throw new InvalidDataException("configure acquisition");
            AddChild(bootstrap);
            GetViewport().GetWindow().Size=new Vector2I(1000,700);
            if(startup)return;
            host=bootstrap.Host;app=host.Application;
            if(factoryCalls!=1||host.AttemptState?.Started!=false||!host.PasswordField.Secret)throw new InvalidDataException("composition prefix");
            host.LoginButton.EmitSignal(Button.SignalName.Pressed);
        }
        catch(Exception e){Fail(e);}
    }
    private long Clock()=>frame*10+offset;
    private Task Gate(CancellationToken token)=>minimum.Task.WaitAsync(token);
    private IDatagramPort Factory(ReplacementConnection connection,DatagramPeer peer)
    {factoryCalls++;if(connection==ReplacementConnection.Zone)zoneFactoryCalls++;return connection==ReplacementConnection.Login&&peer==login.Peer?login:connection==ReplacementConnection.Zone&&peer==zone.Peer?zone:throw new InvalidDataException("spy fixed peer");}
    public override void _Process(double delta)
    {
        frame++;
        try
        {
            if(frame>10000)throw new TimeoutException("offline harness frame bound");
            if(mode.StartsWith("startup-",StringComparison.Ordinal))
            {
                if(frame==1)rollbackPending=!bootstrap.Completion.IsCompleted&&bootstrap.State==ReplacementClientBootstrapState.StartupRollback&&login.DisposeRequested;
                if(frame==5)bootstrap._Notification((int)NotificationWMCloseRequest);
                if(frame==20){if(!rollbackPending||bootstrap.Completion.IsCompleted)throw new InvalidDataException("rollback was shortened");Record();login.ReleaseDisposal();}
                return;
            }
            if(written)return;
            var session=app.Session;var world=app.World;var run=app.Run;
            if(!authenticated&&session.Phase==ReplacementClientPhase.LoginAuthentication)
            {authenticated=true;host.UsernameField.Text="synthetic";host.PasswordField.Text="fixture";host.AuthenticateButton.EmitSignal(Button.SignalName.Pressed);passwordCleared=host.PasswordField.Text.Length==0;}
            if(!selected&&session.Phase==ReplacementClientPhase.CharacterList&&host.Characters.ItemCount==2)
            {
                if(mode=="list-phase-timeout"){Record();offset+=30000;return;}
                host.Characters.Select(1);host.SelectButton.EmitSignal(Button.SignalName.Pressed);
                wrongIdRejected=host.SelectionReason.StartsWith("Unapproved selection:",StringComparison.Ordinal)&&factoryCalls==1&&run.Generation==0&&session.Phase==ReplacementClientPhase.CharacterList;
                if(!wrongIdRejected)throw new InvalidDataException("wrong supported ID mutated application");
                host.Characters.Select(0);host.SelectButton.EmitSignal(Button.SignalName.Pressed);selected=true;
            }
            if(world.State==ZoneSceneTransitionState.Initializing&&world.StagedCount>=5)
            {
                if(mode=="cancel"){Record();bootstrap.RequestClose();return;}
                if(mode=="readiness-phase-timeout"&&host.AttemptState?.Stage==ReplacementControlledAttemptStage.SceneReadiness){Record();offset+=30000;return;}
                minimum.TrySetResult(true);
            }
            if(zone.ReadyWaiting)
            {
                if(readyAt==0){readyAt=frame;host.MoveButton.EmitSignal(Button.SignalName.Pressed);preReadyRejected=run.ReadySubmissions==1&&world.State==ZoneSceneTransitionState.AwaitingServerReady&&host.RootsQueued==0;}
                if(frame-readyAt>=30)zone.ReleaseReady();
            }
            if(host.HasPublishedPlayer&&host.ReceivedServerReady)
            {
                var actor=host.Presentation!.SelectedVisual!.CreatureVisual!;
                if(!published){if(!actor.Verify(DisplayServer.GetName()!="headless")||!preReadyRejected)throw new InvalidDataException("original player/readiness");published=true;actorId=actor.Root.GetInstanceId();}
                if(actorId!=actor.Root.GetInstanceId())throw new InvalidDataException("actor replaced");
                if(mode=="overall-timeout"){Record();offset+=120000;return;}
                if(mode=="movement-disabled")
                {host.MoveButton.EmitSignal(Button.SignalName.Pressed);if(!host.MoveButton.Disabled||host.RootsQueued!=0)throw new InvalidDataException("missing movement approval");Record();bootstrap.RequestClose();return;}
                if(host.RootsQueued>observedRoots)
                {
                    var transform=app.Motions.Find(world.Snapshots().Single())!.Transform.Translation;
                    if(actor.Root.GlobalPosition.DistanceTo(new(transform.X,transform.Y,-transform.Z))>.00001f)return;
                    observedRoots=host.RootsQueued;positions.Add(new[]{actor.Root.GlobalPosition.X,actor.Root.GlobalPosition.Y,actor.Root.GlobalPosition.Z});times.Add(Clock());
                }
                if(!startedMovement&&!host.MoveButton.Disabled)
                {startedMovement=true;host.UsernameField.ReleaseFocus();host.PasswordField.ReleaseFocus();using var key=new InputEventKey{Keycode=Key.W,PhysicalKeycode=Key.W,Pressed=true};Input.ParseInputEvent(key);}
                if(host.RootsQueued==3&&zone.Captured.ContainsKey("Movement2")&&movementAt==0)movementAt=frame;
                if(movementAt!=0&&frame-movementAt>220)
                {Record();if(mode=="diagnostic-write-failure")File.WriteAllText(diagnostic,"sentinel");if(mode=="window-close")bootstrap._Notification((int)NotificationWMCloseRequest);else bootstrap.RequestClose();}
            }
        }
        catch(Exception e){Fail(e);}
    }
    private void Record()
    {
        if(written)return;written=true;
        if(zoneFactoryCalls==0)zone.DisposeAsync().GetAwaiter().GetResult();
        bool screenshot=false;
        if(DisplayServer.GetName()!="headless"&&published)
        {using var image=GetViewport().GetTexture().GetImage();if(image.SavePng(Path.Combine(output,"standalone.png"))!=Error.Ok)throw new IOException("offline screenshot");screenshot=true;}
        var hashes=zone.Captured.ToDictionary(pair=>pair.Key,pair=>Convert.ToHexString(SHA256.HashData(Convert.FromHexString(pair.Value))));
        File.WriteAllText(Path.Combine(output,"observer.json"),JsonSerializer.Serialize(new{Passed=true,Mode=mode,SyntheticOffline=bootstrap.SyntheticOffline,FactoryCalls=factoryCalls,ZoneFactoryCalls=zoneFactoryCalls,NegativeCases=negativeCases,RollbackPending=rollbackPending,WrongSupportedIdRejected=wrongIdRejected,PasswordCleared=passwordCleared,PreReadyRejected=preReadyRejected,Published=published,Positions=positions,Times=times,WireDigests=hashes,login.LoginWireVerified,Screenshot=screenshot,CompletionBeforeClose=bootstrap.Completion.IsCompleted}));
    }
    private void Fail(Exception error)
    {
        SetProcess(false);login?.ReleaseDisposal();
        if(output.Length!=0)File.WriteAllText(Path.Combine(output,"observer.json"),JsonSerializer.Serialize(new{Passed=false,FailureType=error.GetType().Name}));
        if(bootstrap is not null&&bootstrap.IsInsideTree())bootstrap.RequestClose();else {if(bootstrap is not null&&GodotObject.IsInstanceValid(bootstrap))bootstrap.Free();GetTree().Quit(1);}
    }
    private sealed class TestPort(JsonElement transcript, bool isZone, DatagramPeer peer, string mode="success") : IDatagramPort
    {
        public DatagramPeer Peer => peer;
        public bool Disposed { get; private set; }
        public bool DisposeRequested { get; private set; }
        private readonly TaskCompletionSource<bool> disposeGate=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public void ReleaseDisposal()=>disposeGate.TrySetResult(true);
        private async Task DelayedDisposal()
        { await disposeGate.Task.ConfigureAwait(false);Disposed=true;if(mode=="startup-rollback-fault")throw new IOException("authored rollback disposal fault"); }
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
            DisposeRequested=true;
            pending?.TrySetCanceled();pending = null;
            while (queue.Count != 0) CryptographicOperations.ZeroMemory(queue.Dequeue());
            if(mode.StartsWith("startup-",StringComparison.Ordinal))return new(DelayedDisposal());
            Disposed=true;
            if(mode=="cleanup-fault"&&isZone)return new(Task.FromException(new IOException("authored disposal fault")));
            return ValueTask.CompletedTask;
        }
    }
}
