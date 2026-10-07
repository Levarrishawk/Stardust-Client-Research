using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

public sealed record ReplacementClientHostOptions(uint LoginConnectionId, uint ZoneConnectionId,
    bool SyntheticOffline, ReplacementControlledMovementScenario? MovementScenario = null,
    long DisconnectTimeoutMilliseconds = 3000, ReplacementControlledAttempt? ControlledAttempt = null,
    ulong? ApprovedCharacterId = null);

/// <summary>Explicit injected application entry. Never constructs a socket, resolves DNS or persists credentials.</summary>
public partial class ReplacementClientHost : Node3D
{
    private readonly int owner = System.Environment.CurrentManagedThreadId;
    private readonly TaskCompletionSource<bool> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private ReplacementClientApplication? application;
    private ReplacementClientHostOptions? options;
    private Func<long>? clock;
    private SynchronizationContext? context;
    private SceneTree? owningTree;
    private ReplacementPlayerPresentationHost? presentation;
    private ReplacementControlledMovement? movement;
    private LoginCharacter[] characters = Array.Empty<LoginCharacter>();
    private bool configured, busy, retiring, finalized, protocolClosing, nativeExit, finalizerStarted, movementStopped;
    private bool originalAutoQuit;
    private long lastClock = -1;
    private int roots, localReady;
    private bool serverReady;
    private string? failure;
    private Task presentationCompletion = Task.CompletedTask;
    private readonly List<string> cleanupFailures = new();

    public LineEdit UsernameField { get; private set; } = null!;
    public LineEdit PasswordField { get; private set; } = null!;
    public Button LoginButton { get; private set; } = null!;
    public Button AuthenticateButton { get; private set; } = null!;
    public OptionButton Characters { get; private set; } = null!;
    public Button SelectButton { get; private set; } = null!;
    public Button MoveButton { get; private set; } = null!;
    public Button StopButton { get; private set; } = null!;
    public Button CloseButton { get; private set; } = null!;
    public Label StatusLabel { get; private set; } = null!;
    public Task Completion => completion.Task;
    public Task PresentationCompletion { get { AssertOwner(); return presentationCompletion; } }
    public bool IsFinalized { get { AssertOwner(); return finalized; } }
    public bool IsConfigured { get { AssertOwner(); return configured; } }
    public ReplacementControlledAttemptState? AttemptState { get { AssertOwner(); return options?.ControlledAttempt?.State; } }
    public bool IsClosing { get { AssertOwner(); return retiring || protocolClosing; } }
    public string? FailureType { get { AssertOwner(); return failure; } }
    public IReadOnlyList<string> CleanupFailures { get { AssertOwner(); return cleanupFailures.ToArray(); } }
    public int RootsQueued { get { AssertOwner(); return movement?.RootsQueued ?? roots; } }
    public int ReadySubmissions { get { AssertOwner(); return localReady; } }
    public bool ReceivedServerReady { get { AssertOwner(); return serverReady; } }
    public bool HasPublishedPlayer { get { AssertOwner(); return !retiring && !finalized && presentation?.PlayerPublished == true; } }
    public ReplacementPlayerPresentationHost? Presentation { get { AssertOwner(); return retiring || finalized ? null : presentation; } }
    public ReplacementClientApplication Application
    {
        get { AssertOwner(); ObjectDisposedException.ThrowIf(finalized, this); return application ?? throw new InvalidOperationException("Host unconfigured."); }
    }
    public int DeferredApplications { get; private set; }
    public string SelectionReason { get; private set; } = "Choose a listed supported human.";

    public override void _Ready()
    {
        var layer = new CanvasLayer { Name = "ClientControls" }; AddChild(layer);
        var panel = new PanelContainer { Position = new Vector2(18, 18), Size = new Vector2(360, 410) }; layer.AddChild(panel);
        var box = new VBoxContainer(); panel.AddChild(box);
        box.AddChild(new Label { Text = "Stardust bounded replacement client" });
        box.AddChild(new Label { Text = "Explicit injected transport required" });
        LoginButton = Button(box, "Login", "Start login");
        UsernameField = new LineEdit { Name = "Username", PlaceholderText = "Username", MaxLength = 256 }; box.AddChild(UsernameField);
        PasswordField = new LineEdit { Name = "Password", PlaceholderText = "Password", Secret = true, MaxLength = 256 }; box.AddChild(PasswordField);
        AuthenticateButton = Button(box, "Authenticate", "Authenticate");
        Characters = new OptionButton { Name = "Characters" }; box.AddChild(Characters);
        SelectButton = Button(box, "Select", "Select supported human");
        MoveButton = Button(box, "Move", "Walk forward · three roots maximum (W)");
        StopButton = Button(box, "Stop", "Stop controlled movement");
        CloseButton = Button(box, "Close", "Disconnect and close (Esc)");
        StatusLabel = new Label { Text = "Unconfigured · no connection started", AutowrapMode = TextServer.AutowrapMode.WordSmart }; box.AddChild(StatusLabel);
        LoginButton.Pressed += () => ControlAction(StartLogin);
        AuthenticateButton.Pressed += Authenticate;
        SelectButton.Pressed += Select;
        MoveButton.Pressed += () => ControlAction(StartMovement);
        StopButton.Pressed += () => ControlAction(StopMovement);
        CloseButton.Pressed += RequestClose;
        UpdateControls();
    }
    private static Button Button(Node parent, string name, string text)
    {
        var button = new Button { Name = name, Text = text }; parent.AddChild(button); return button;
    }
    public void Configure(ReplacementClientApplication candidate, ReplacementClientHostOptions configuration, Func<long> nowMilliseconds)
    {
        Enter();
        bool admitted = false;
        try
        {
            if (configured || retiring || nativeExit) throw new InvalidOperationException("Host accepts one application.");
            ArgumentNullException.ThrowIfNull(candidate); ArgumentNullException.ThrowIfNull(configuration); ArgumentNullException.ThrowIfNull(nowMilliseconds);
            if (!IsInsideTree() || GetParent() is null || IsQueuedForDeletion() || StatusLabel is null ||
                SynchronizationContext.Current is null) throw new InvalidOperationException("Live ready Godot host and context required.");
            if (configuration.LoginConnectionId == configuration.ZoneConnectionId || configuration.DisconnectTimeoutMilliseconds is < 1 or > 60000)
                throw new ArgumentException("Distinct connection identities and bounded disconnect required.");
            if (configuration.ApprovedCharacterId == 0 || configuration.ControlledAttempt is { State.Started: true } ||
                configuration.ControlledAttempt is not null && configuration.ApprovedCharacterId is null)
                throw new ArgumentException("Fresh attempt and explicit approved character required.");
            if (candidate.Closing || candidate.IsFinalized || candidate.Session.Phase != ReplacementClientPhase.Offline ||
                candidate.World.State != ZoneSceneTransitionState.Idle || candidate.Creature.Catalog.Entries.Count != 1)
                throw new ArgumentException("Fresh prepared one-human application required.");
            // All rejecting preflight precedes ownership transfer. No factory, clock or application mutation here.
            application = candidate; options = configuration; clock = nowMilliseconds;
            context = SynchronizationContext.Current; configured = true;
            admitted = true;
            owningTree = GetTree(); originalAutoQuit = owningTree.AutoAcceptQuit; owningTree.AutoAcceptQuit = false;
            StatusLabel.Text = configuration.SyntheticOffline ? "Injected synthetic/offline peer · no live compatibility" : "Injected transport · live compatibility unverified";
            UpdateControls();
        }
        catch (Exception error) { if (admitted) Fail(error); throw; }
        finally { busy = false; }
    }
    private void AssertOwner()
    {
        if (System.Environment.CurrentManagedThreadId != owner || OS.GetThreadCallerId() != OS.GetMainThreadId())
            throw new InvalidOperationException("Host creating Godot main thread required.");
    }
    private void Enter() { AssertOwner(); if (busy) throw new InvalidOperationException("Host reentry."); busy = true; }
    private long Now()
    {
        long now = clock!();
        if (now < 0 || now < lastClock) throw new ArgumentOutOfRangeException(nameof(now), "Monotonic caller clock required.");
        lastClock = now; return now;
    }
    private void ControlAction(Action action)
    {
        Enter();
        try
        {
            if (!configured || retiring || protocolClosing || finalized) return;
            try { if (!CheckAttempt(Now())) return; action(); UpdateControls(); }
            catch (Exception error) { Fail(error); }
        }
        finally { busy = false; }
    }
    private void StartLogin()
    {
        if (application!.Session.Phase != ReplacementClientPhase.Offline) return;
        long now = Now(); options!.ControlledAttempt?.Start(now);
        application.StartLogin(options.LoginConnectionId, now);
    }
    private void Authenticate() => ControlAction(() =>
    {
        if (application!.Session.Phase != ReplacementClientPhase.LoginAuthentication) return;
        try { application.Authenticate(UsernameField.Text, PasswordField.Text, Now()); }
        finally { PasswordField.Text = ""; }
    });
    private void Select() => ControlAction(() =>
    {
        int index = Characters.Selected;
        if (application!.Session.Phase != ReplacementClientPhase.CharacterList || index < 0 || index >= characters.Length) return;
        var character = characters[index];
        if (options!.ApprovedCharacterId is { } approved && character.ObjectId != approved)
        {
            SelectionReason = "Unapproved selection: this attempt permits one explicit character.";
            StatusLabel.Text = SelectionReason; return;
        }
        if (character.ObjectId == 0 || !application.Creature.Catalog.Entries.ContainsKey(character.RaceCrc))
        {
            SelectionReason = "Unsupported selection: template is outside the prepared human catalog.";
            StatusLabel.Text = SelectionReason; return;
        }
        application.SelectCharacter(character.ObjectId, options!.ZoneConnectionId, Now());
        characters = Array.Empty<LoginCharacter>(); Characters.Clear(); UsernameField.Text = ""; PasswordField.Text = "";
        var candidate = new ReplacementPlayerPresentationHost { Name = "SelectedPlayerPresentation" }; AddChild(candidate);
        // Retain actual completion even if presentation admission closes its acquired prefix and throws.
        presentation = candidate; presentationCompletion = candidate.Completion;
        candidate.Configure(application.World, character.ObjectId, PresentationUpdate, application.Motions);
        SelectionReason = "Selected prepared human; waiting for received server-ready.";
        StatusLabel.Text = SelectionReason;
    });
    private void StartMovement()
    {
        if (movement is not null || retiring || protocolClosing || application!.World.State != ZoneSceneTransitionState.Ready ||
            application.Run.ReadySubmissions != 1 || presentation?.PlayerPublished != true) return;
        var scenario = options!.MovementScenario;
        if (scenario is null || scenario.SelectedObjectId != application.Session.SelectedObjectId)
        { StatusLabel.Text = "Movement requires an explicit matching controlled scenario."; return; }
        movement = new(application.Run, application.Session, application.World, application.Motions, scenario, System.Numerics.Vector3.UnitZ);
    }
    private void StopMovement()
    {
        if (movement is null) return;
        roots = movement.RootsQueued; movement.Dispose();
        movementStopped = true;
        // Preserve the retired pacer to prevent restarting the three-root cap.
        MoveButton.Disabled = true; StopButton.Disabled = true;
    }
    public override void _UnhandledInput(InputEvent input)
    {
        if (input is not InputEventKey { Pressed: true, Echo: false } key || !configured || retiring || finalized) return;
        if (GetViewport().GuiGetFocusOwner() is LineEdit) return;
        if (key.Keycode == Key.W) { ControlAction(StartMovement); GetViewport().SetInputAsHandled(); }
        else if (key.Keycode == Key.Escape) { RequestClose(); GetViewport().SetInputAsHandled(); }
    }
    public override void _Process(double delta)
    {
        if (!configured || finalized || nativeExit) return;
        Enter();
        try
        {
            if (retiring) return;
            if (protocolClosing)
            {
                var tick = application!.Tick(Now()); Clear(tick);
                if (tick.FailureType is { } kind) { failure ??= kind; Retire(); }
                else if (application.Run.DisconnectSendCompleted) Retire();
                return;
            }
            if (!CheckAttempt(Now())) return;
            if (presentation is null) UpdateSession(delta);
            if (retiring) return;
            if (presentation?.Failure is { } error) { Fail(error); return; }
            ObserveReadiness();
            UpdateControls();
        }
        catch (Exception error) { Fail(error); }
        finally { busy = false; }
    }
    private void UpdateSession(double seconds)
    {
        AssertOwner(); if (retiring || protocolClosing) return;
        long now = Now(); if (!CheckAttempt(now)) return;
        var tick = application!.Tick(now); Clear(tick);
        if (tick.FailureType is { } kind) { failure ??= kind; ObserveAttempt(now, faulted: true); Retire(); return; }
        if (application.Session.Phase == ReplacementClientPhase.CharacterList && characters.Length == 0)
        {
            characters = application.Session.Characters.ToArray();
            foreach (var character in characters)
            {
                string name = new(character.Name.Where(c => !char.IsControl(c)).Take(64).ToArray());
                bool supported = application.Creature.Catalog.Entries.ContainsKey(character.RaceCrc);
                Characters.AddItem(name + (supported ? " · prepared human" : " · unsupported"));
            }
            if (characters.Length > 0) Characters.Select(0);
        }
        ObserveReadiness();
        if (!CheckAttempt(now)) return;
        if (movement is not null && !movementStopped) movement.Tick(Now());
        UpdateControls();
    }
    private void PresentationUpdate(double seconds)
    {
        Enter();
        try { UpdateSession(seconds); }
        finally { busy = false; }
    }
    private void Clear(ReplacementClientRunTick tick)
    {
        DeferredApplications += tick.UnhandledApplications.Count;
        foreach (byte[] value in tick.UnhandledApplications) CryptographicOperations.ZeroMemory(value);
    }
    private void ObserveReadiness()
    {
        localReady = application!.Run.ReadySubmissions;
        serverReady |= application.World.State == ZoneSceneTransitionState.Ready;
    }
    private void UpdateControls()
    {
        if (StatusLabel is null || nativeExit) return;
        bool active = configured && !retiring && !protocolClosing && !finalized;
        var phase = active ? application!.Session.Phase : ReplacementClientPhase.Closed;
        LoginButton.Disabled = !active || phase != ReplacementClientPhase.Offline;
        AuthenticateButton.Disabled = !active || phase != ReplacementClientPhase.LoginAuthentication;
        UsernameField.Editable = PasswordField.Editable = active && phase == ReplacementClientPhase.LoginAuthentication;
        Characters.Disabled = !active || phase != ReplacementClientPhase.CharacterList;
        SelectButton.Disabled = !active || phase != ReplacementClientPhase.CharacterList || characters.Length == 0;
        MoveButton.Disabled = !active || movement is not null || !serverReady || presentation?.PlayerPublished != true || options?.MovementScenario is null;
        StopButton.Disabled = !active || movement is null || movementStopped;
        CloseButton.Disabled = !configured || retiring || protocolClosing || finalized;
        if (active)
        {
            string mode = options!.SyntheticOffline ? "Injected synthetic/offline peer" : "Injected transport · compatibility unverified";
            string state = phase switch
            {
                ReplacementClientPhase.Offline => "Start login explicitly.",
                ReplacementClientPhase.LoginHandshake => "Waiting for login handshake.",
                ReplacementClientPhase.LoginAuthentication => "Enter credentials for this admission only.",
                ReplacementClientPhase.LoginResponses => "Waiting for character enumeration.",
                ReplacementClientPhase.CharacterList => SelectionReason,
                ReplacementClientPhase.Selected when !serverReady => "Client ready submitted: " + localReady + "; received server-ready: no.",
                ReplacementClientPhase.Selected when presentation?.PlayerPublished != true => "Received server-ready; waiting for original human publication.",
                ReplacementClientPhase.Selected => "Received server-ready · roots " + RootsQueued + "/3 · " + (movement?.StopReason ?? "controlled scenario only"),
                _ => "Waiting for selected zone admission."
            };
            StatusLabel.Text = mode + "\n" + state;
        }
    }
    private ReplacementControlledAttemptState? ObserveAttempt(long now, bool faulted = false, bool closing = false)
    {
        var attempt = options?.ControlledAttempt;
        if (attempt is null || !attempt.State.Started) return attempt?.State;
        var phase = application!.Session.Phase;
        bool ready = phase == ReplacementClientPhase.Selected && application.World.State == ZoneSceneTransitionState.Ready;
        return attempt.Observe(now, new(phase, phase == ReplacementClientPhase.Selected && application.Run.ReadySubmissions == 1,
            ready, ready && presentation?.PlayerPublished == true, faulted, closing));
    }
    private bool CheckAttempt(long now)
    {
        var state = ObserveAttempt(now);
        if (state is null || state.StopReason == ReplacementControlledAttemptStopReason.None) return true;
        RequestCloseCore(); return false;
    }
    private void Fail(Exception error) { failure ??= error.GetType().Name; ObserveAttempt(lastClock < 0 ? 0 : lastClock, faulted: true); Retire(); }
    public void RequestClose()
    {
        Enter();
        try { RequestCloseCore(); }
        finally { busy = false; }
    }
    private void RequestCloseCore()
    {
            if (retiring || finalized || protocolClosing) return;
            if (!configured) { retiring = true; finalized = true; completion.TrySetResult(true); UpdateControls(); return; }
            long now = Now(); ObserveAttempt(now, closing: true);
            PasswordField.Text = ""; UsernameField.Text = ""; StopMovement();
            if (!application!.Closing && application.Session.Phase == ReplacementClientPhase.Selected && application.Run.Generation > 0)
            {
                try { application.BeginDisconnect(now, options!.DisconnectTimeoutMilliseconds); protocolClosing = true; UpdateControls(); return; }
                catch (Exception error) { failure ??= error.GetType().Name; }
            }
            Retire();
    }
    private void Retire()
    {
        AssertOwner(); if (retiring) return; retiring = true; protocolClosing = false;
        if (application is not null) ObserveAttempt(lastClock < 0 ? 0 : lastClock, closing: true);
        if (!nativeExit) { PasswordField.Text = ""; UsernameField.Text = ""; StatusLabel.Text = failure is null ? "Retiring owned work…" : "Retiring after " + failure; UpdateControls(); }
        if (movement is not null) { roots = movement.RootsQueued; movement.Dispose(); }
        presentation?.RequestClose();
        application?.BeginClose(presentationCompletion);
        if (!finalizerStarted) { finalizerStarted = true; _ = FinalizeOnOwner(); }
    }
    private async Task FinalizeOnOwner()
    {
        try
        {
            while (application is not null && !application.TryFinalizeClose(presentationCompletion)) await Task.Delay(8);
            if (application is not null) cleanupFailures.AddRange(application.CleanupFailures);
            if (presentationCompletion.IsFaulted) cleanupFailures.AddRange(presentationCompletion.Exception!.Flatten().InnerExceptions.Select(e => e.GetType().Name));
            else if (presentationCompletion.IsCanceled) cleanupFailures.Add(nameof(TaskCanceledException));
            finalized = true; characters = Array.Empty<LoginCharacter>();
            if (GodotObject.IsInstanceValid(owningTree) && owningTree!.AutoAcceptQuit == false) owningTree.AutoAcceptQuit = originalAutoQuit;
            if (!nativeExit) { StatusLabel.Text = failure is null && cleanupFailures.Count == 0 ? "Closed · actual ownership finalized" : "Closed · failure observed"; UpdateControls(); }
            if (failure is null && cleanupFailures.Count == 0) completion.TrySetResult(true);
            else completion.TrySetException(new InvalidOperationException(failure ?? "Cleanup failed"));
        }
        catch (Exception error) { failure ??= error.GetType().Name; completion.TrySetException(new InvalidOperationException(failure)); }
    }
    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest && configured && !nativeExit) RequestClose();
    }
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(PasswordField)) PasswordField.Text = "";
        if (GodotObject.IsInstanceValid(UsernameField)) UsernameField.Text = "";
        nativeExit = true;
        // Native removal must unwind before presentation destroys its actors.
        if (configured && !finalized) context!.Post(_ => Retire(), null);
        else if (!configured) { finalized = true; completion.TrySetResult(true); }
    }
}
