# Offline limits display owner

October6,2026. A modern noninteractive Godot display over supplied immutable state.
[Source](../../SWG-Godot-Client/poc/Stardust.Godot/TerrainOfflineLimitsHud.cs),
[actual native proof](../../SWG-Godot-Client/poc/Stardust.Godot/TerrainOfflineLimitsHudVerification.cs),
[independent oracle](../../SWG-Godot-Client/poc/round78-tests/Rendering/verify_picker.py).

```csharp
public sealed record TerrainOfflineLimitsHudState(int MaximumUpdates,
    int MaximumClickAttempts, int RemainingClickAttempts,
    bool GameplayEnabled, bool Paused);
void Configure(Node owner, Func<TerrainOfflineLimitsHudState> source,
    CancellationToken token = default);
bool Refresh(CancellationToken token = default);
void Dispose();
```

## Admission and exact text

MaximumUpdates is1..6000, MaximumClickAttempts1..128 and RemainingClickAttempts
0..MaximumClickAttempts. Null state and invalid ranges reject. Both gameplay/pause
booleans are caller-associated state; no relationship is inferred between them and
no additional status suffix is displayed. Every admitted state uses the exact accepted
caption, with invariant culture:

`{MaximumUpdates*.01:0.0} simulation seconds | Terrain clicks remaining: {RemainingClickAttempts} | Limit keeps movement active`

This is a supplied10ms-update duration policy, not measured elapsed time. The label
is at28,286 with font size16 on CanvasLayer21. It has MouseFilter.Ignore and
FocusMode.None. The actual1280x720 GPU capture shows the line clear below the existing
interactive HUD ending at276. No button, focus request or input handler is introduced.
Small update budgets can round to0.0; the exact accepted formatting is retained.

## Guards, commits and ownership

Configure is attempted once and performs the first Refresh. It captures the borrowed
owner, its exact parent and viewport, then creates only one owned CanvasLayer and Label.
The source is read exactly once per successful refresh attempt, after pre-guard and
before post-guard. A valid formatted caption is assigned to Label.Text, followed by
another guard before Caption/RefreshCount commit. The display owns no world, driver,
session settings, input adapter, transport, existing HUD or caller callback object.

Explicit methods and getters require the creating managed thread and Godot main
thread. Guards require live unqueued exact owner/parent/viewport, exact layer/label
attachment and noninteractive control flags. Caught nested Configure/Refresh/Dispose
still poisons the outer refresh. Configure/Refresh tokens apply to that call; no
lifetime token is retained. Source/state/cancel/lifetime/reentry failure retires once
and rethrows. Off-thread rejection occurs before source or retirement.

Caption, RefreshCount, RetirementCount and IsRetired are owner-thread historical
readers. IsConfigured records successful initial construction. Caption/count retain
the last successful commit after retirement. Retired Refresh returns false without
reading the source, even with a canceled token. Unconfigured Refresh and failed or
repeated Configure are terminal, consistent with the existing HUD owner convention.
Label assignment and external callback effects are not rolled back if a later guard
fails; successful historical commit is distinguished from intermediate native effects.

## Native lifetime

The owned layer's exit/native disposal callback marks managed retirement and releases
borrowed references without calling Free. It clears its callback first. Explicit HUD
Dispose clears retirement/callback state before freeing only a still-live owned layer;
its public/interface disposal is idempotent. Native parent Free/QueueFree and layer
Free/QueueFree therefore cannot recursively free from a disposal callback. A detached
but still-live owned layer remains pending explicit Dispose, which frees it safely.
The owner/parent and all unrelated children are never freed by this HUD.

## Evidence and limitations

Confirmed by67 headless/68 GPU checks: eight independently authored exact caption
rows and source counts,21 failure rows, five raw/deferred/interface teardown outcomes,
actual W/left-click pass-through inside the label, borrowed-owner survival, repeated
Dispose and no source read after retirement. Malformed limits, null/throwing sources,
caught refresh/dispose reentry, cancellation, owner exit/deletion, owned display loss,
external mouse capture, detached Configure and off-thread rejection are covered.
One bounded frozen default-HUD caption regression also agrees exactly; no full input
suite is duplicated. Build has zero warnings/errors and logs have no native leak or
resource-in-use diagnostic. The GPU capture was visually inspected.

Observation: these tests execute replacement Godot behavior using synthetic state.
Confidence: Confirmed for the fixtures. Consequence: the caller can replace its raw
layer/label lifetime with this owner and keep exact accepted caption expectations.
Association with controller/settings, variable-duration updates, original UI parity,
arbitrary viewport sizes, localization, accessibility and counter overflow remain
unverified. Smaller viewports can clip this fixed single line. The offline
certificate-store diagnostic remains with exit0; no network request is required.

Next probe: lead supplies frozen session limits and actual remaining click capacity
through the callback, removes its raw layer/label and verifies configured preview
refreshes, error prefixes, focus/pause and final native owner cleanup.
