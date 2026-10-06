# Offline session keyboard host

October6,2026. Modern caller policy for the offline preview. This is replacement
Godot behavior, with no claim about original client keys or session semantics.
[Host source](../poc/Stardust.Godot/TerrainOfflineSessionInputHost.cs),
[native verification](../poc/Stardust.Godot/TerrainOfflineSessionInputHostVerification.cs),
[independent model](../poc/round75-tests/Rendering/verify_picker.py).

## Admission and actions

Add TerrainOfflineSessionInputHost as a child of the borrowed owner before Configure:

```csharp
void Configure(Node owner, Func<bool> paused, Action<bool> setPaused,
    Action requestStop, CancellationToken cancellation = default);
bool Dispatch(InputEvent input, CancellationToken cancellation = default);
void Close();
void Dispose();
```

Configure accepts once, borrows three callbacks, and captures the exact owner,
owner parent and viewport. Configuration failure before acceptance clears references
and permits retry. Configure, Dispatch, Close, Dispose and diagnostics require the
creating managed thread and Godot main thread. Reentry is rejected and poisons the
outer action even if the callback catches the rejection.

Logical P pressed without Echo reads the pause source once and calls setPaused with
its inverse. Logical Escape pressed without Echo invokes requestStop once, with no
pause-source read. FinishRequested is latched before attempting that sink; repeated
Escape and subsequent P are suppressed. Releases, echoes, unrelated keys and mouse
input return false without callbacks. These ignored paths do not evaluate the
per-dispatch cancellation token. The Configure token is an admission token, not a
retained ongoing cancellation source.

_UnhandledInput admits actions only after GUI routing. Successful actions mark the
viewport input handled and increment HandledCount. A focused Control that accepts
an event prevents this host from seeing it. Explicit Dispatch bypasses that GUI
routing and returns whether a recognized action committed; it does not mark input
handled. It is intended for bounded caller tests, not a substitute for GUI routing.

## Callback prefixes and lifetime

Before and after each callback, recognized actions check cancellation, healthy
state, exact live owner/parent/viewport and host attachment. A callback exception,
caught reentry, cancellation or lifetime change faults and closes the host, clears
borrowed references and retains an honest LastDiagnostic. SinkMayHaveRun is set
before invoking a sink; SinkReturned only after return; StateCommitted only after
the final guard. No rollback of callback effects is attempted. AcceptedCount counts
committed actions; StateReadCount counts attempted source reads. Engine input catches
exceptions at its boundary and sets FromGodotEvent; direct Dispatch rethrows them.

Close disables unhandled processing and releases callbacks, while leaving the
closed host node attached. Dispose also frees the host's own node, idempotently.
Godot's base Dispose alone does not establish node removal. The concrete public Dispose and reimplemented IDisposable.Dispose perform the explicit
free. The protected Godot disposal callback only retires managed references and
calls base disposal; it never invokes Free. Raw Free, parent Free and QueueFree can
therefore enter the native disposal callback without recursive node destruction.
A statically GodotObject-typed Dispose retains Godot binding-disposal semantics;
use the concrete host or IDisposable when requesting explicit own-node deletion. Native owner deletion during a callback is tolerated
after _ExitTree closes the host, while the outer action still fails its post guard.
ExitTree closes without freeing the borrowed owner. Getters retain managed diagnostic
state after retirement/disposal. No world, driver, controller, camera or borrowed
owner is created, disposed or advanced by this host.

## Optional visible controls

[HUD source](../poc/Stardust.Godot/TerrainOfflinePlaygroundHud.cs) retains the exact
legacy Configure(Node, Func<State>, CancellationToken = default) signature and
captions. A new overload requires the first three arguments explicitly:

```csharp
void Configure(Node owner, Func<TerrainOfflinePlaygroundHudState> source,
    CancellationToken cancellation, bool interactiveControls);
```

True selects the subtitle Movement controls, appends P pause | Escape finish, and
uses a608x260 panel. False preserves the608x230 panel and original text. The HUD
remains noninteractive and never captures keyboard/mouse events. All prior state
admission, refresh and ownership rules remain in the
[existing HUD contract](TERRAIN_OFFLINE_PLAYGROUND_HUD_CONTRACT.md).

## Evidence and limits

Confirmed by actual Godot4.7.2 .NET runs:74 headless/75 GPU checks,15 viewport actions
and16 independent exact failure-prefix comparisons, two focused-GUI-consumed keys,
two pause toggles and one finish, direct and native callback failures, owner removal
and deletion, cancellation, off-thread and caught reentry, retry after rejected
configuration, and borrowed-owner survival. Five independently modeled teardown
cases cover detached/attached raw Free, idle parent Free/QueueFree and interface
disposal, including repeated Dispose after each. Logs complete without native leak
or resource-in-use diagnostics. The unchanged accepted HUD proof passes
116 checks, eight exact default captions and21 negative cases. The640x480 GPU capture
was inspected: controls fit without clipping or overlap.

Interpretation: these fixtures establish the bounded host contract, not integration
with a controller or the entire preview. Compatibility consequence: the caller must
supply current pause state, idempotent stop policy and its own teardown ordering.
No keyboard-layout/hardware capture, live network, original executable execution,
localization, responsive layout or accessibility behavior was tested. Checked counter
overflow and arbitrary caller callback behavior beyond these cases remain untested.
The offline root-certificate-store diagnostic appears with exit0; no network is needed.

Next probe: join this host to the engine-neutral session controller, deliver actual
P events at fixed-update boundaries5/8 and Escape at600, and independently verify
pause clocks, movement/packet counters and final cleanup in the complete preview.
