using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Stardust.Core.Assets;
using Stardust.Core.Configuration;
using Stardust.Core.Hosting;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

public enum ReplacementClientBootstrapState
{
    Unconfigured, Configured, Running, RefusedNoTransport, InvalidInput,
    StartupRollback, StartupRejected, RollbackFault, Finalized, RunFault, CleanupFault
}

public enum ReplacementAttemptDiagnosticState { None, Pending, Written, Failed }
public sealed record ReplacementClientAttemptReceipt(int Version, string Outcome, bool SyntheticOffline,
    ReplacementControlledAttemptState? Attempt, IReadOnlyList<ReplacementControlledAttemptTransition> Stages,
    string? SelectedAlias, int ClientReadySubmissions, bool ServerReadyReceived, bool PlayerPublished,
    int RootsQueued, int DeferredApplications, string? ControlledLaunchSha256, string? ConfigurationSha256,
    IReadOnlyList<string> OriginalResourceSha256, string? TerrainSha256, int TerrainBytes,
    string? FailureType, IReadOnlyList<string> CleanupFailureTypes, bool OperationTaskSucceeded,
    bool LocalDrainCompleted, bool LocalDrainSucceeded, bool ApplicationFinalized,
    bool StartupRollbackCompleted, bool StartupRollbackSucceeded);

/// <summary>Separate explicit entry; default refuses contact. Injected offline transport only in this bounded build.</summary>
public partial class ReplacementClientBootstrap : Node3D
{
    private readonly int owner = System.Environment.CurrentManagedThreadId;
    private readonly TaskCompletionSource<bool> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Task noPresentation = Task.CompletedTask;
    private ReplacementControlledLaunch? launch;
    private Func<ReplacementConnection, DatagramPeer, IDatagramPort>? factory;
    private Func<CmdStartSceneMessage, ProceduralTerrainInput, Func<string, TreLookupResult>, CancellationToken, Task>? minimum;
    private SceneWorldClientRootMotionPolicy? rootPolicy;
    private Func<long>? testClock;
    private ReplacementControlledAttempt? attempt;
    private ReplacementClientApplication? application;
    private ReplacementClientHost? host;
    private SynchronizationContext? context;
    private SceneTree? sceneTree;
    private Label? status;
    private Camera3D? camera;
    private Godot.Environment? environment;
    private bool entered, busy, observing, originalAutoQuit, injected;
    private string? failureType;
    private ReplacementClientBootstrapState state;
    private readonly List<string> cleanupFailures = new();
    private bool standalone, syntheticStandalone, rollbackCompleted, rollbackSucceeded, selectedObserved, playerObserved;
    private string? diagnosticPath, diagnosticFailureType, controlledHash, configurationHash, terrainHash;
    private int terrainBytes, rootsObserved, readyObserved, deferredObserved;
    private bool serverReadyObserved;
    private Func<CancellationToken, Task>? offlineMinimumGate;
    private readonly SortedSet<string> originalHashes = new(StringComparer.Ordinal);
    private ReplacementAttemptDiagnosticState diagnosticState;
    private ReplacementClientAttemptReceipt? receipt;

    public Task Completion => completion.Task;
    public ReplacementClientBootstrapState State { get { AssertOwner(); return state; } }
    public string? FailureType { get { AssertOwner(); return failureType; } }
    public IReadOnlyList<string> CleanupFailures { get { AssertOwner(); return cleanupFailures.ToArray(); } }
    public ReplacementControlledAttemptState? AttemptState { get { AssertOwner(); return attempt?.State; } }
    public IReadOnlyList<ReplacementControlledAttemptTransition> AttemptTransitions { get { AssertOwner(); return attempt?.Transitions ?? Array.Empty<ReplacementControlledAttemptTransition>(); } }
    public bool ExplicitTestClock { get { AssertOwner(); return testClock is not null; } }
    public ReplacementClientHost Host { get { AssertOwner(); return host ?? throw new InvalidOperationException("Bootstrap host unavailable."); } }
    public bool MinimumReadinessIsOperatorAssertion => true;
    public bool SyntheticOffline { get { AssertOwner(); return !standalone || syntheticStandalone; } }
    public ReplacementAttemptDiagnosticState DiagnosticState { get { AssertOwner(); return diagnosticState; } }
    public string? DiagnosticFailureType { get { AssertOwner(); return diagnosticFailureType; } }
    public ReplacementClientAttemptReceipt? Receipt { get { AssertOwner(); return receipt; } }

    /// <summary>Same opt-in admission/composition as CLI. Always synthetic; never enters UDP construction.</summary>
    public void ConfigureStandaloneOffline(string[] arguments,
        Func<ReplacementConnection, DatagramPeer, IDatagramPort> spyFactory, Func<long>? explicitTestClock = null,
        Func<CancellationToken, Task>? explicitOfflineMinimumGate = null,
        SceneWorldClientRootMotionPolicy? explicitTestRootPolicy = null)
    {
        Enter();
        try
        {
            if (entered || state != ReplacementClientBootstrapState.Unconfigured) throw new InvalidOperationException("Configure bootstrap once before tree entry.");
            ArgumentNullException.ThrowIfNull(spyFactory);
            AdmitStandalone(arguments);
            factory = spyFactory; syntheticStandalone = true; testClock = explicitTestClock;
            offlineMinimumGate = explicitOfflineMinimumGate;
            rootPolicy = explicitTestRootPolicy ?? new(1000, 10);
        }
        finally { busy = false; }
    }
    private void AdmitStandalone(string[] arguments)
    {
        if (arguments is null || arguments.Length != 3 || arguments[0] != "--controlled-transport")
            throw new InvalidDataException("Exact explicit controlled transport arguments required.");
        var parsed = ReadControlledLaunch(arguments[1], out string hash);
        ValidateDiagnosticDestination(arguments[2]);
        launch = parsed; controlledHash = hash; diagnosticPath = arguments[2];
        standalone = true; diagnosticState = ReplacementAttemptDiagnosticState.Pending;
        rootPolicy = new(1000, 10); minimum = OriginalMinimum; state = ReplacementClientBootstrapState.Configured;
    }
    private static void ValidateDiagnosticDestination(string path)
    {
        ValidateLocalPath(path);
        if (!path.Replace('\\', '/').Split('/').Contains("local-private", StringComparer.OrdinalIgnoreCase) ||
            !string.Equals(Path.GetExtension(path), ".json", StringComparison.OrdinalIgnoreCase) ||
            File.Exists(path) || Directory.Exists(path)) throw new InvalidDataException("New private JSON diagnostic destination required.");
        string parent = Path.GetDirectoryName(path)!;
        if (!Directory.Exists(parent)) throw new InvalidDataException("Existing private diagnostic parent required.");
        RejectLinks(parent);
    }
    private IDatagramPort CreateStandalonePort(ReplacementConnection connection, DatagramPeer peer)
    {
        var approved = connection == ReplacementConnection.Login ? launch!.LoginPeer :
            connection == ReplacementConnection.Zone ? launch!.ZonePeer : throw new InvalidOperationException("Approved connection required.");
        if (peer != approved) throw new InvalidOperationException("Approved numeric peer required.");
        if (syntheticStandalone) return factory!(connection, peer);
        // SOURCE-REVIEW ONLY this round. Construction binds a real socket; no test invokes this branch.
        return new UdpDatagramPort();
    }
    private async Task OriginalMinimum(CmdStartSceneMessage message, ProceduralTerrainInput decoded,
        Func<string, TreLookupResult> lookup, CancellationToken token)
    {
        if (!launch!.MinimumReadinessApproved || message.ObjectId != launch.ApprovedCharacterId ||
            application!.Session.SelectedObjectId != message.ObjectId || application.Run.Generation == 0)
            throw new InvalidDataException("Approved selected minimum scene required.");
        if (offlineMinimumGate is not null) await offlineMinimumGate(token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        var original = lookup(message.Terrain);
        if (original.Kind != TreLookupKind.Present || original.Payload is null) throw new InvalidDataException("Original minimum terrain required.");
        var actual = ProceduralTerrainInput.Decode(original.Payload);
        if (actual.Version != decoded.Version || !actual.CopyDataRecord().AsSpan().SequenceEqual(decoded.CopyDataRecord()) ||
            !actual.CopyGeneratorForm().AsSpan().SequenceEqual(decoded.CopyGeneratorForm()))
            throw new InvalidDataException("Matching original minimum terrain required.");
        token.ThrowIfCancellationRequested();
        Volatile.Write(ref terrainHash, Convert.ToHexString(SHA256.HashData(original.Payload)));
        Volatile.Write(ref terrainBytes, original.Payload.Length);
    }

    public void ConfigureInjected(string controlledLaunchPath,
        Func<ReplacementConnection, DatagramPeer, IDatagramPort> offlineFactory,
        Func<CmdStartSceneMessage, ProceduralTerrainInput, Func<string, TreLookupResult>, CancellationToken, Task> minimumProvider,
        SceneWorldClientRootMotionPolicy policy, Func<long>? explicitTestClock = null)
    {
        Enter();
        try
        {
            if (entered || state != ReplacementClientBootstrapState.Unconfigured) throw new InvalidOperationException("Configure bootstrap once before tree entry.");
            ArgumentNullException.ThrowIfNull(offlineFactory); ArgumentNullException.ThrowIfNull(minimumProvider); ArgumentNullException.ThrowIfNull(policy);
            var parsed = ReadControlledLaunch(controlledLaunchPath);
            if (!parsed.MinimumReadinessApproved || parsed.EndpointApproval.ApprovedCharacterId != parsed.ApprovedCharacterId)
                throw new InvalidDataException("Explicit approved controlled scenario required.");
            // Configuration rejection acquires no application, port or installation resource.
            launch = parsed; factory = offlineFactory; minimum = minimumProvider; rootPolicy = policy;
            testClock = explicitTestClock; injected = true; state = ReplacementClientBootstrapState.Configured;
        }
        finally { busy = false; }
    }
    public static ReplacementControlledLaunch ReadControlledLaunch(string path) => ReadControlledLaunch(path, out _);
    private static void ValidateLocalPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || path.Length > 4096 || path.Any(char.IsControl) ||
            !Path.IsPathFullyQualified(path) || path.StartsWith("//", StringComparison.Ordinal) || path.StartsWith(@"\\", StringComparison.Ordinal))
            throw new InvalidDataException("Explicit local controlled launch path required.");
        string rest = path.Length > 1 && path[1] == ':' ? path[2..] : path;
        if (rest.Contains(':') || rest.Replace('\\', '/').Split('/').Any(p => p is "." or ".."))
            throw new InvalidDataException("Local controlled launch path rejected.");
        foreach (string part in rest.Replace('\\', '/').Split('/').Where(p => p.Length != 0))
        {
            string stem = part.Split('.')[0].ToUpperInvariant();
            if (part.EndsWith(' ') || part.EndsWith('.') || part.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                stem is "CON" or "PRN" or "AUX" or "NUL" ||
                stem.Length == 4 && (stem.StartsWith("COM", StringComparison.Ordinal) || stem.StartsWith("LPT", StringComparison.Ordinal)) && stem[3] is >= '1' and <= '9')
                throw new InvalidDataException("Ambiguous local path rejected.");
        }
    }
    private static void RejectLinks(string path)
    {
        for (string? current = path; current is not null; current = Path.GetDirectoryName(current))
            if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) throw new InvalidDataException("Linked controlled launch input rejected.");
    }
    private static ReplacementControlledLaunch ReadControlledLaunch(string path, out string hash)
    {
        ValidateLocalPath(path); RejectLinks(path);
        if (new FileInfo(path).Length is < 1 or > 131072) throw new InvalidDataException("Controlled launch byte budget.");
        byte[] bytes = File.ReadAllBytes(path);
        var parsed = ReplacementControlledLaunchCodec.Decode(bytes);
        hash = Convert.ToHexString(SHA256.HashData(bytes)); return parsed;
    }
    public override void _Ready()
    {
        AssertOwner(); entered = true; context = SynchronizationContext.Current;
        var layer = new CanvasLayer { Layer = 2 }; AddChild(layer);
        status = new Label { Position = new Vector2(18, 460), Size = new Vector2(390, 130), AutowrapMode = TextServer.AutowrapMode.WordSmart }; layer.AddChild(status);
        if (!injected && !standalone)
        {
            try
            {
                var args = OS.GetCmdlineUserArgs();
                if (args.Length == 1) { launch = ReadControlledLaunch(args[0]); state = ReplacementClientBootstrapState.RefusedNoTransport; }
                else AdmitStandalone(args);
            }
            catch (Exception error) { failureType = error.GetType().Name; state = ReplacementClientBootstrapState.InvalidInput; }
            if (!standalone)
            {
                ShowState(); GD.Print("bootstrap_state=" + state); completion.TrySetResult(true);
                return; // Default refusal never acquires preparation or transport.
            }
        }
        try
        {
            if (context is null) throw new InvalidOperationException("Godot owner context required.");
            sceneTree = GetTree(); originalAutoQuit = sceneTree.AutoAcceptQuit; sceneTree.AutoAcceptQuit = false;
            attempt = new(launch!.PhaseBudgetMilliseconds, launch.OverallBudgetMilliseconds);
            var stopwatch = Stopwatch.StartNew(); Func<long> clock = testClock ?? (() => stopwatch.ElapsedMilliseconds);
            if (standalone)
            {
                using var stream = File.OpenRead(launch.Profile.ConfigurationPath);
                configurationHash = Convert.ToHexString(SHA256.HashData(stream));
            }
            application = new(launch.Profile, launch.Options, launch.LoginPeer, launch.ZonePeer, standalone ? CreateStandalonePort : factory!, minimum!, rootPolicy!,
                endpointApproval: launch.EndpointApproval);
            if (standalone) foreach (var source in application.Creature.Sources.Values) AddOriginalHash(source.Sha256);
            host = new ReplacementClientHost { Name = "ControlledClientHost" }; AddChild(host);
            host.Configure(application, new(launch.LoginConnectionId, launch.ZoneConnectionId, SyntheticOffline, launch.MovementScenario,
                launch.DisconnectTimeoutMilliseconds, attempt, launch.ApprovedCharacterId), clock);
            state = ReplacementClientBootstrapState.Running;
            environment = new Godot.Environment { BackgroundMode = Godot.Environment.BGMode.Color, BackgroundColor = new Color("#14202e"),
                AmbientLightSource = Godot.Environment.AmbientSource.Color, AmbientLightColor = Colors.White, AmbientLightEnergy = .8f };
            AddChild(new WorldEnvironment { Environment = environment });
            AddChild(new DirectionalLight3D { RotationDegrees = new Vector3(-30,-30,0), LightEnergy = 1 });
            camera = new Camera3D { Current = true, Fov = 40 }; AddChild(camera);
            ShowState(); observing = true; _ = ObserveHost();
        }
        catch (ReplacementClientApplication.StartupFailure error)
        {
            failureType = error.FailureType; state = ReplacementClientBootstrapState.StartupRollback; ShowState();
            observing = true; _ = ObserveRollback(error.RollbackCompletion);
        }
        catch (Exception error)
        {
            failureType = error.GetType().Name; state = ReplacementClientBootstrapState.StartupRejected; ShowState();
            observing = true;
            if (host?.IsConfigured == true) { host.RequestClose(); _ = ObserveHost(); }
            else if (application is not null) _ = RetireUntransferred();
            else Finish(false);
        }
    }
    private async Task ObserveRollback(Task rollback)
    {
        try { await rollback; rollbackCompleted = true; rollbackSucceeded = true; state = ReplacementClientBootstrapState.StartupRejected; Finish(false); }
        catch (Exception error) { rollbackCompleted = rollback.IsCompleted; cleanupFailures.Add(error.GetType().Name); state = ReplacementClientBootstrapState.RollbackFault; Finish(true); }
    }
    private async Task RetireUntransferred()
    {
        try
        {
            host?.RequestClose(); application!.BeginClose(noPresentation);
            while (!application.TryFinalizeClose(noPresentation)) await Task.Delay(8);
            cleanupFailures.AddRange(application.CleanupFailures);
            state = cleanupFailures.Count == 0 ? ReplacementClientBootstrapState.StartupRejected : ReplacementClientBootstrapState.CleanupFault;
            Finish(cleanupFailures.Count != 0);
        }
        catch (Exception error) { cleanupFailures.Add(error.GetType().Name); state = ReplacementClientBootstrapState.CleanupFault; Finish(true); }
    }
    private async Task ObserveHost()
    {
        try { await host!.Completion; }
        catch { _ = host!.Completion.Exception; }
        if (standalone && (!host!.IsFinalized || application?.IsFinalized != true))
        {
            // An unexpected host-finalizer failure is not permission to abandon live borrowers.
            state = ReplacementClientBootstrapState.CleanupFault; ShowState();
            while (!host.IsFinalized || application?.IsFinalized != true) await Task.Delay(8);
        }
        cleanupFailures.AddRange(host!.CleanupFailures);
        failureType ??= host.FailureType;
        state = cleanupFailures.Count != 0 ? ReplacementClientBootstrapState.CleanupFault :
            host.Completion.IsCompletedSuccessfully ? ReplacementClientBootstrapState.Finalized : ReplacementClientBootstrapState.RunFault;
        Finish(!host.Completion.IsCompletedSuccessfully);
    }
    private void Finish(bool faulted)
    {
        if (standalone && application is not null && !application.IsFinalized)
        {
            state = ReplacementClientBootstrapState.CleanupFault; ShowState();
            return; // Unexpected finalizer failure retains ownership; no receipt, completion or forced quit.
        }
        AssertOwner(); CaptureObservations();
        if (standalone) WriteAttemptReceipt(faulted);
        ShowState();
        if (GodotObject.IsInstanceValid(sceneTree) && sceneTree!.AutoAcceptQuit == false) sceneTree.AutoAcceptQuit = originalAutoQuit;
        if (faulted || diagnosticState == ReplacementAttemptDiagnosticState.Failed) completion.TrySetException(new InvalidOperationException("Bootstrap operation, cleanup or diagnostic failed."));
        else completion.TrySetResult(true);
        if (standalone)
        {
            GD.Print("bootstrap_state=" + state + " diagnostic_state=" + diagnosticState + " local_drain_completed=" + receipt!.LocalDrainCompleted);
            if (completion.Task.IsFaulted) _ = completion.Task.Exception;
            GetTree().Quit(state == ReplacementClientBootstrapState.Finalized && diagnosticState == ReplacementAttemptDiagnosticState.Written ? 0 : 1);
        }
        // Injectable92 composition owner still decides when to free/quit.
    }
    private void AddOriginalHash(string hash)
    {
        if (originalHashes.Contains(hash)) return;
        if (originalHashes.Count >= 256) { diagnosticFailureType = nameof(InvalidDataException); diagnosticState = ReplacementAttemptDiagnosticState.Failed; return; }
        originalHashes.Add(hash);
    }
    private void CaptureObservations()
    {
        if (!standalone || host?.IsConfigured != true) return;
        rootsObserved = Math.Max(rootsObserved, host.RootsQueued); readyObserved = Math.Max(readyObserved, host.ReadySubmissions);
        deferredObserved = Math.Max(deferredObserved, host.DeferredApplications); serverReadyObserved |= host.ReceivedServerReady;
        if (host.IsFinalized || host.IsClosing) return;
        selectedObserved |= application!.Run.Generation != 0;
        playerObserved |= host.HasPublishedPlayer;
        var resources = host.Presentation?.SelectedVisual?.Publication.Resources;
        if (resources is not null) foreach (var source in resources.Sources.Values) AddOriginalHash(source.Sha256);
    }
    private void WriteAttemptReceipt(bool faulted)
    {
        bool finalized = application?.IsFinalized == true;
        bool emptyPrefix = application is null && state == ReplacementClientBootstrapState.StartupRejected && !rollbackCompleted;
        bool drainCompleted = finalized ? application!.DrainCompletion.IsCompleted : rollbackCompleted || emptyPrefix;
        bool drainSucceeded = finalized ? application!.DrainCompletion.IsCompletedSuccessfully : rollbackCompleted && rollbackSucceeded || emptyPrefix;
        receipt = new(1, state.ToString(), syntheticStandalone, attempt?.State,
            Array.AsReadOnly((attempt?.Transitions ?? Array.Empty<ReplacementControlledAttemptTransition>()).ToArray()),
            selectedObserved ? "selected-1" : null, readyObserved, serverReadyObserved, playerObserved,
            rootsObserved, deferredObserved, controlledHash, configurationHash, Array.AsReadOnly(originalHashes.ToArray()),
            Volatile.Read(ref terrainHash), Volatile.Read(ref terrainBytes), failureType, Array.AsReadOnly(cleanupFailures.Take(16).ToArray()),
            host?.IsConfigured == true && host.Completion.IsCompletedSuccessfully, drainCompleted, drainSucceeded, finalized, rollbackCompleted, rollbackSucceeded);
        try
        {
            if (diagnosticState == ReplacementAttemptDiagnosticState.Failed) throw new InvalidDataException("Attempt diagnostic observation bound.");
            ValidateDiagnosticDestination(diagnosticPath!);
            var options = new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() } };
            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(receipt, options);
            if (bytes.Length > 65536) throw new InvalidDataException("Attempt diagnostic byte bound.");
            using var stream = new FileStream(diagnosticPath!, FileMode.CreateNew, System.IO.FileAccess.Write, FileShare.None);
            stream.Write(bytes); stream.Flush(true); diagnosticState = ReplacementAttemptDiagnosticState.Written;
        }
        catch (Exception error) { diagnosticFailureType = error.GetType().Name; diagnosticState = ReplacementAttemptDiagnosticState.Failed; }
    }
    private void ShowState()
    {
        if (!GodotObject.IsInstanceValid(status)) return;
        status!.Text = "Bootstrap: " + state + (SyntheticOffline ? " · synthetic/offline" : " · explicit controlled transport") + "\nMinimum readiness: operator assertion only; terrain/floor/authority unproved.";
        if (state == ReplacementClientBootstrapState.RefusedNoTransport)
            status.Text += "\nContact refused: explicit controlled transport opt-in required.";
    }
    public void RequestClose()
    {
        AssertOwner();
        CaptureObservations();
        if (host?.IsConfigured == true) host.RequestClose();
        else if (!observing && Completion.IsCompleted && IsInsideTree()) GetTree().Quit();
        // Pending startup rollback is not shortened by a window-close request.
    }
    public override void _Process(double delta)
    {
        CaptureObservations();
        if (state != ReplacementClientBootstrapState.Running || host is null || host.IsClosing || host.IsFinalized || camera is null) return;
        var actor = host.Presentation?.SelectedVisual?.CreatureVisual?.Root;
        if (actor is null) return;
        camera.Position = actor.GlobalPosition + new Vector3(0,.8f,-7.75f);
        camera.LookAt(actor.GlobalPosition + new Vector3(0,.8f,0));
    }
    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest && entered) RequestClose();
    }
    public override void _ExitTree()
    {
        // Retain nodes and keep the engine context running through Completion before native destruction.
        if (host?.IsConfigured == true && !host.IsFinalized) context?.Post(_ => host.RequestClose(), null);
        environment?.Dispose();
    }
    private void AssertOwner()
    {
        if (System.Environment.CurrentManagedThreadId != owner || OS.GetThreadCallerId() != OS.GetMainThreadId())
            throw new InvalidOperationException("Bootstrap creating Godot main thread required.");
    }
    private void Enter() { AssertOwner(); if (busy) throw new InvalidOperationException("Bootstrap reentry."); busy = true; }
    public override string ToString() => "replacement_bootstrap_state=" + state;
}
