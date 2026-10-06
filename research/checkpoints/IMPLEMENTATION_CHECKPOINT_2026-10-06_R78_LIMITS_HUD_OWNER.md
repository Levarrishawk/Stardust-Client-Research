# Round78: owned offline limits HUD

Date:2026-10-06. Question: can one noninteractive label preserve the accepted limits
caption while replacing caller raw-node lifetime with exact guarded refresh and safe
native retirement? Stop after independent state/caption/failure/teardown comparisons,
headless/GPU inspection, bounded default caption and scope validation pass.

## Inputs and method

Accepted77 frozen input4537 files. Manifest SHA-256:
fae3830a886e9cb4bfd6639c6337052539a35e77c84dd9c5dadf62212f32650f;
archive SHA-256:4a11a66f312fd654aae850dc94c3214b58130068ce7896108e31dc78a5d275d4.
Original executable identity read/rechecked:
E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150.
No original execution, VAs, assets, new network or unfinished78 APIs are used.
Cached Python, offline .NET8/Godot4.7.2 .NET x64 packages/toolchain, headless and one
OpenGL compatibility GPU run. Frozen accepted playground HUD is the sole staged
source dependency; its default expected caption comes from the accepted73 oracle.

[Durable contract](../../docs/TERRAIN_OFFLINE_LIMITS_HUD_CONTRACT.md),
[owner](../../poc/Stardust.Godot/TerrainOfflineLimitsHud.cs),
[verification](../../poc/Stardust.Godot/TerrainOfflineLimitsHudVerification.cs),
[runner](../../poc/round78-tests/Rendering/Invoke-Picker.ps1),
[oracle](../../poc/round78-tests/Rendering/verify_picker.py).

Run the explicit-input runner with cached Python/dotnet, original executable identity
input, offline package cache and fresh ignored RunRoot. It pins manifest/archive,
checks/stages frozen HUD bytes, emits independent fixtures before build, then executes
and compares headless/GPU results. Detailed logs/PNG/receipts stay ignored without
curated links into private paths.

## Observations, confidence and consequences

- Eight independently authored rows match actual Label.Text/Caption/RefreshCount
  and exactly one source read per refresh. Counts32/128, zero remaining, paused/focus
  bool variants and tiny update budgets preserve accepted invariant text. Confirmed.
  The bools introduce no suffix and simulation duration remains supplied10ms policy.
- Twenty-one independent failure rows cover every admitted limit boundary, null and
  throwing source, caught mutation/disposal reentry, source/pre-call cancellation,
  owner exit/deletion, lost layer/label, capture-flag tampering, repeated/unconfigured
  admission, detached owner and off-thread rejection. Last committed caption/count
  and one retirement are retained. Confirmed; native intermediate effects are not
  claimed to roll back.
- Five actual native teardown rows cover parent Free/QueueFree, detached layer Free,
  layer QueueFree and interface Dispose followed by repeated Dispose. The layer's
  native callback retires references without Free; explicit disposal frees live own
  layer only. Borrowers survive as appropriate and logs show no leak/resource-in-use
  diagnostics. Confirmed; no native recursive node destruction.
- Actual W/left click inside the visible label reaches the underlying probe without
  focus capture. Font16/28,286 placement is inspected in1280x720 beside the frozen
  interactive HUD; no overlap/clipping. One frozen default caption remains exact.
  Confirmed for this layout/input fixture, not all GUI or viewport arrangements.

The initial negative harness omitted InvalidDataException from expected rejection
handling; adding that exception type allowed intended malformed-state cases to run.
No production admission change or expected-output relaxation was needed.

Final67 headless/68 GPU checks, eight captions/21 failure rows/five native teardown
outcomes. Build zero warnings/errors. Eight path/executable/reparse guard probes,
UTF8/AST/XML/PowerShell syntax, relative links, focused diff/whitespace, workspace
validator and full frozen scope pass. All4537 baseline files unchanged; ten new owned
public files. HEAD/index/private preserved, index empty. Offline certificate-store
diagnostic persists with successful exit0; no clean-environment claim is made.

## Limits and exact resume

No controller/settings/transport association, original UI or timing semantics,
responsive layout/localization/accessibility or overflow execution is established.
A detached live layer needs explicit Dispose; native exits release source/borrowers
and preserve historical values. Source-only facade owns no borrowed parent/world.
Next lead probe: replace raw limits CanvasLayer/Label with this owner in the configured
preview, source exact maximum updates/remaining click capacity and check refresh/error
prefixes plus final teardown. No shared/main HUD/session edits are part of this worker.
