# Offline outcome display owner

October6,2026. Modern noninteractive display over primitive caller-supplied outcome
state. [Source](../../SWG-Godot-Client/poc/Stardust.Godot/TerrainOfflineOutcomeHud.cs),
[native verification](../../SWG-Godot-Client/poc/Stardust.Godot/TerrainOfflineOutcomeHudVerification.cs),
[independent oracle](../../SWG-Godot-Client/poc/round79-tests/Rendering/verify_picker.py).

```csharp
public sealed record TerrainOfflineOutcomeHudState(string Reason,
    int CompletedUpdates, int MaximumUpdates, ulong RootFrames,
    int QueueCount, int PendingMessages, int RetainedPackets);
void Configure(Node owner, Func<TerrainOfflineOutcomeHudState> source,
    CancellationToken token = default);
bool Refresh(CancellationToken token = default);
void Dispose();
```

## Primitive admission and exact caption

Reason accepts only BudgetCompleted, FinishRequested, Faulted or Incomplete.
MaximumUpdates is1..6000, CompletedUpdates0..MaximumUpdates, RootFrames0..6000,
QueueCount/PendingMessages0..8 and RetainedPackets0..6000. RootFrames is unsigned64;
all other counts are signed32. Null record/reason or malformed fields reject. No
relationship between reason, counters and actual session provenance is inferred.
The caller owns that association; no sibling completion-report enum/type is imported.

The exact five-line caption uses invariant culture:

```text
Offline preview outcome
Reason: {Budget completed|Finished with Escape|Stopped after an error|Incomplete}
Updates: {completed} / {max}
Offline root frames: {frames}
Buffers: {empty|remaining}
```

Buffers is empty only when QueueCount, PendingMessages and RetainedPackets are all
zero. Any one nonzero value selects remaining. The human reason mapping is fixed;
no caller-provided text is rendered directly.

## Layout and guarded refresh

The owner creates one CanvasLayer22, Panel at16,328 sized608x148 and Label at12,8
inside that panel, font16. Panel and Label both use MouseFilter.Ignore/FocusMode.None.
The Panel uses the existing Godot theme; no separate style resource is created.
The inspected1280x720 GPU image shows all five lines clear below the existing limits
line and playground HUD. No input handler, button or focus capture is added.

Configure is attempted once and performs the first Refresh. It captures the exact
borrowed owner, its parent and viewport. Pre/post-source and post-Label.Text guards
require creating managed/Godot main thread, live unqueued exact attachments and
noninteractive control flags. The callback is read once per refresh attempt that
passes preflight. Formatting/state validation occurs before native text assignment;
Caption/RefreshCount commit only after the last guard. A nested mutation caught by
the source still poisons the outer operation. Per-call tokens are not retained.

Caption, RefreshCount, RetirementCount and IsRetired are historical owner-thread
getters; IsConfigured records successful initial admission. Source/state/cancel/
lifetime/reentry failures retire once and rethrow, retaining the last committed
caption/count. Unconfigured Refresh and repeated/failed Configure are terminal.
Off-thread rejection leaves the source and lifetime untouched. Retired Refresh returns
false without reading source or inspecting its token. Native intermediate effects and
external callback effects are not rolled back after a later guard failure.

## Native ownership

Only layer/panel/label are owned. No caller owner/parent, existing HUD, settings,
world, clock, session, transport or captured callback object is freed. Layer exit and
protected native disposal clear their callback before marking managed retirement;
they never call Free. Explicit concrete/interface HUD Dispose retires first, then
frees only a still-live owned layer, clearing its reference before native callbacks.
Parent Free/QueueFree, layer raw/deferred deletion and repeated Dispose therefore
avoid native Free recursion. A detached live layer remains pending explicit disposal,
which safely releases it. Native retirement immediately drops source/borrowed refs.

## Evidence and limits

Confirmed by85 headless/86 GPU checks: eight pre-native independent exact captions,
source counts and actual Label.Text; all four reasons and independently nonzero
queue/pending/retained buffers;30 failure rows; five native/deferred/interface teardown
outcomes; W/left-click pass-through within the panel; borrowed-parent survival and
historical retirement. Malformed numeric/reason/null state, source exception,
caught refresh/dispose reentry, cancellation, owner exit/deletion, layer/panel/label
loss, control capture tampering, detached Configure and off-thread calls are covered.
Bounded frozen playground/limits captions remain exact without full playground runs.
Build has zero warnings/errors; logs show no native leak/resource-in-use diagnostic.
The final GPU image was visually inspected.

Observation: these are synthetic supplied outcome records displayed by real Godot.
Confidence: Confirmed for the fixtures. Consequence: lead may join an actual report
and capture normal completion before caller cleanup. Faulted-state caption/lifetime
is separately tested; a complete failed-host screenshot is not established here.
Original UI/protocol/session semantics, actual report provenance, arbitrary viewport
sizes, accessibility/localization and counter overflow remain unverified. The fixed
panel can overlap other caller overlays on smaller surfaces. Offline certificate-store
diagnostic persists with exit0; no network request is required.

Next probe: lead maps its actual normal completion report into this primitive record,
checks exact updates/root counts/empty buffers, captures before cleanup and then drains
this owner. Keep full host-failure image claims separate from standalone Faulted state.
