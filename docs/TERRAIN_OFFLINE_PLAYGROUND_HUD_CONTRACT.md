# Offline playground controls and status

October6,2026. Opt-in modern display over a supplied immutable status record.
[TerrainOfflinePlaygroundHud](../poc/Stardust.Godot/TerrainOfflinePlaygroundHud.cs)
borrows its owner Node and state callback; it owns one CanvasLayer, Panel, three Labels
and the panel style resource. It owns no world, camera, simulation, socket or input route.

```csharp
public sealed record TerrainOfflinePlaygroundHudState(
    string Mode, double Speed, int Updates, int MaximumUpdates,
    int RemainingUpdates, bool TargetActive, bool Offline);
void Configure(Node owner, Func<TerrainOfflinePlaygroundHudState> source,
    CancellationToken cancellation = default);
bool Refresh(CancellationToken cancellation = default);
void Dispose();
```

Configure is accepted once and performs the first Refresh, reading the source exactly
once. Refresh returns true after a successful display commit and false after retirement,
without another source read. `Caption`, `RefreshCount`, `RetirementCount`, `IsConfigured`
and `IsRetired` are readable owner-thread diagnostics. Caption/count retain the last
successful display after retirement; IsConfigured records successful display construction,
not current lifetime. Configure-again and unconfigured Refresh are terminal failures.

## State and visible text

Mode is exactly Idle, Moving, Arrived, Paused or Manual. Speed is finite in0..10; Updates
is in0..MaximumUpdates, MaximumUpdates in1..6000, RemainingUpdates exactly equals
MaximumUpdates-Updates, and Offline must be true. The caller owns state production and
association; the record contains no world/actor provenance or timing metadata. Its
updates fields are retained for admission, not displayed as implementation counters.

Visible title: `Offline preview` and `Modern supplied movement`. Status shows mode,
Speed with two invariant decimal places, preview time remaining with one invariant
decimal place and `s`, and active/inactive target. The time display uses the explicit
caller policy RemainingUpdates*0.01 seconds. This requires fixed10ms preview updates;
the HUD neither advances nor measures time. No meters-per-second speed unit is inferred.
Controls show WASD move, Shift run, right-drag orbit, wheel zoom and left-click target.
All displayed strings are bounded by admitted enums/numbers and fixed captions.

The fixed608x230 panel starts at16,16 on CanvasLayer20, with separated title/status/control
sections. The inspected640x480 capture is readable without clipping or overlapping text.
Caller owns viewport sizing, resize policy and placement relative to other overlays;
responsive layout, localization and accessibility navigation are not implemented.

## Input and ownership

Every owned Control has MouseFilter.Ignore and FocusMode.None before attachment.
There are no input handlers, hardware capture calls or focus requests. Actual W and
left-click events delivered inside the visible panel reach the underlying world probe;
a right-drag reaches the unchanged accepted orbit host and is handled by that host.
This establishes bounded pass-through, not every possible GUI/input interaction.

Configure/Refresh/Dispose and getters require the creating managed thread and Godot main
thread. The exact live owner, captured parent and same viewport, and the exact owned
layer/panel/label parent chain are checked before source invocation and after callbacks
and text effects. Freed, queued, detached or reparented required nodes reject. Caught
nested Refresh/Dispose/Configure attempts poison the outer operation. Cancellation is
checked before work and after callbacks. A source callback is read once per refresh.

Main-thread failure retires the owned display once, freeing its layer subtree and style,
and drops borrowed references. Retirement tolerates an externally freed owner/root or
label; no borrowed Node is freed or reset by the HUD. Off-thread rejection occurs before
retirement/source invocation so the owner can still use/dispose it. Dispose is idempotent.
Text effects may complete before a later guard fails; no rollback is promised, but the
owned display is removed. Arbitrary external mutation of owned controls is outside the
API contract. Counter overflow is checked but not executed in fixtures.

## Reproduction and confidence

[Invoke-Picker.ps1](../poc/round73-tests/Rendering/Invoke-Picker.ps1) verifies executable
identity, uses cached offline Python/.NET/Godot, generates independent expectations,
builds and executes the replacement scene headless/GPU. The
[producer/verifier](../poc/round73-tests/Rendering/verify_picker.py) validates the incoming
4371-file manifest/archive and privately stages the frozen orbit host for input proof.
No Runtime73/Networking73 dependency or original asset read is involved.

Eight independently authored state/caption transitions agree exactly with actual Label
texts, successful refresh counts and Caption. Final116 headless/117 GPU checks pass:
21 grouped source/state/lifetime failures, four additional configure/attachment/child
guard scenarios, detached Configure, off-thread methods/getter, actual input pass-through
and borrowed survival. A private GPU capture was inspected and spacing corrected before
final sealing. Build has zero warnings/errors; runner guard, syntax, links, workspace,
diff and scope audits pass. Confidence is Confirmed for these bounded modern fixtures.
See the [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-06_R73_OFFLINE_PLAYGROUND_HUD.md)
for limitations and the main-scene resume point.
