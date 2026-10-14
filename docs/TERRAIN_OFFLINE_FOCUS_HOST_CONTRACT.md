# Offline preview focus owner

October6,2026. Modern replacement policy for focus loss and return. No original
client focus behavior is inferred. Source (external historical reference),
native proof (external historical reference),
independent model (external historical reference).

## API and exact ownership

Attach TerrainOfflineFocusHost as a child of the borrowed owner, then configure:

```csharp
void Configure(Node owner, Func<bool> pausedSource, Action<bool> pauseSink,
    Action<bool> gameplayEnabledSink, CancellationToken cancellation = default);
bool HandleFocus(bool focused, CancellationToken cancellation = default);
void Close();
void Dispose();
```

Configure accepts once without reading state or calling a sink. It captures the
exact live owner, owner parent, viewport and GetWindow result, then connects its
handlers to Window.FocusExited and FocusEntered. Failed admission clears references
and permits retry. It owns only its node, two signal connections, primitive state
and one latest diagnostic. It borrows callbacks and all scene/controller/input objects.

The creating managed thread and prototype host main thread are required for all explicit
methods and getters. Every substantive transition checks exact owner attachment,
parent, viewport and window before and after callbacks, including queued deletion.
The Configure token is retained as lifetime cancellation; HandleFocus also admits a
per-call token. Ignored repeated signals do not evaluate either token. No polling,
hardware capture, OS focus request or new engine-neutral session dependency is added.

## Loss, gain and prefixes

Initially the host considers gameplay active. Configure does not inspect OS focus or
change input/pause state; caller owns initial admission. Actual window signals route
through HandleFocus. Loss reads pausedSource once, captures its bool after the source
guard, calls gameplayEnabledSink(false), then pauseSink(true). A committed loss sets
IsSuspended. Repeated loss performs no callback. Gain while suspended calls
 gameplayEnabledSink(true), then pauseSink(captured prior value), then clears suspension.
Prior-paused sessions remain paused; previously active sessions resume. Repeated gain
and gain before any loss are ignored.

The gameplay sink owns clearing held WASD/Shift and re-enabling fresh key sets. This
host never restores held input. The proof uses actual accepted manual/run hosts and
viewport-delivered keys: loss calls their SetEnabled(false), clearing both masks and
submitting zero intents; gain calls SetEnabled(true) without replaying held keys.
A second fresh press works normally. Simulation/session clock ownership stays external.

TerrainOfflineFocusDiagnostic records Kind, nullable PriorPaused, each sink's
MayHaveRun/Returned frontier, StateCommitted, FailureType and FromWindowSignal.
StateReadCount and sink counts count attempts; TransitionCount counts successful
complete transitions. A gain failure retains the earlier committed loss count and
suspension. Captured prior pause and primitive diagnostics survive retirement.
Callbacks may alter external state before a later guard fails; no rollback is claimed.

Source/sink exception, cancellation, lifetime loss and caught reentry close/fault
and disconnect this host. Direct HandleFocus rethrows; signal handlers catch at the
engine boundary and set FromWindowSignal. Nested mutation poisons the outer call
regardless of a callback catching its rejection. A source failure calls neither sink;
a failed gameplay callback prevents pause; a failed pause can follow completed gameplay.

## Retirement

Close disconnects only its two handlers, clears borrowed references and performs no
pause/gameplay callback. It leaves the node attached and does not restore prior pause.
Other window listeners remain connected. ExitTree closes automatically. Concrete
Dispose and reimplemented IDisposable.Dispose close and Free the own node; the protected
native disposal callback never Free-recurses. Raw attached/detached Free, idle parent
Free/QueueFree and repeated explicit disposal are supported by actual native tests.
Use the concrete host or IDisposable for explicit own-node deletion; statically
Prototype hostObject-typed Dispose retains prototype host binding-disposal semantics.

## Confirmed evidence and limits

Actual prototype host4.7.2 .NET runs pass81 headless/82 GPU checks. Independently authored
pre-native expectations compare22 focus rows, exact callback trace,22 failure prefixes
and five native teardown outcomes. Both initial pause values, repeated signals,
manual/run clear/fresh admission, lifetime/per-call cancellation, caught reentry,
owner/window exit or deletion, off-thread rejection, admission retry, signal failure
and survival of unrelated listeners/borrowed owners are covered. The accepted75
keyboard proof passes74 unchanged checks,15 actions,16 negatives and five teardown
cases. Build has zero warnings/errors; no leak/resource-in-use diagnostics occur.

Window signals are synthetically injected into real prototype host Window instances. This
confirms connection/routing semantics, not OS scheduling, physical focus changes or
keyboard hardware. The GPU run completes an actual rendered frame; no new visible
UI is introduced. Original focus policy, full preview/controller integration,
initial unfocused startup, arbitrary listener ordering and checked-counter overflow
remain unproved. The offline certificate-store diagnostic persists with exit0.

Compatibility consequence: caller must provide a current pause source, own the
held-input clear and session clock, and honor irreversible callback prefixes.
Next probe: lead joins the idle-start preview and engine-neutral session, injects
bounded loss/gain with held movement and both pause states, and verifies no motion
or packet-clock advance while suspended and deterministic final cleanup.
