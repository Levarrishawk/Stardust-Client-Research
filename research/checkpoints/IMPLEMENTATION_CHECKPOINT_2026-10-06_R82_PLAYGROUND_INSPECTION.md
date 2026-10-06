# Round82 playground inspection â€” 2026-10-06

## Bounded question and stopping condition

Can the frozen81 persistent shell expose completed/attempted, admission and cleanup status without retaining retired previews, visibly disable restart at its finite cap, and route Close/owning-window requests through one controlled shutdown? Stop after independent primitive snapshots, actual standalone GUI controls, synthetic window/native-teardown routing, build/regression/scope checks and a sealed handoff. Full asset-backed joins remain the lead's next probe.

## Inputs and tools

Frozen81 public baseline: 4,687 files. Manifest SHA-256 `e142a5e7e1c2846a9f7727a1fe05c82a0aab7735b47aff0167ccc7604ac01ddf`; archive SHA-256 `a4a13af44ae9b35c42eb87f7f1a361f11c7e810fc63ce2f65147e7cc84e29990`. Original executable identity was hash-checked only: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`; no execution or disassembly used. Worker HEAD `65de8c88141071744dc75594be126b3bab3696a5` remains unchanged, index empty.

Inputs: frozen public tree and archive, cached offline Godot4.7.2 .NET packages, .NET SDK and bundled Python. All generated staging, builds, logs, fixtures and screenshots are ignored private output. No sibling Round82 implementation was imported. Public runner [Invoke-Picker.ps1](../../poc/round82-tests/Rendering/Invoke-Picker.ps1) accepts explicit Python/Dotnet/Exe/CachedPackages/RunRoot paths. Use a fresh ignored RunRoot; hash/output/reparse guards reject unsafe inputs before model/build work.

## Findings versus interpretation

The [inspection partial](../../poc/Stardust.Godot/PersistentOfflinePlaygroundInspection.cs) exposes the immutable snapshot and owns its panel, Close button and exact current Window connection. Existing [shell](../../poc/Stardust.Godot/PersistentOfflinePlayground.cs) changes are bounded to preparing/refreshing/disconnecting that UI, one accepted close counter, one quit guard and clearing native-exit references. Existing [restart HUD](../../poc/Stardust.Godot/TerrainOfflineRestartHud.cs) gains one-way disable and availability accessors without changing counters or frozen outcome. The [durable contract](../../docs/OFFLINE_PLAYGROUND_INSPECTION_CONTRACT.md) owns field semantics and compatibility requirements.

Observation: independently authored pre-native expected snapshots matched eight headless scenarios and three GPU scenarios: loading, active, idle replacement, rejection/retry availability, terminal cleanup failure, cap, abrupt teardown, and active synthetic window close. Actual viewport press/release exercises restart and Close; disabled cap/terminal GUI attempts do not invoke the factory or HUD request. Passive inspection panel press/release reaches unhandled input. Duplicate synthetic Window signals plus WM notification count one close; late Window signal after native shell teardown does nothing. GPU images show seven readable status lines, retained outcome at cap/terminal and visibly disabled restart without overlap.

Confidence: Confirmed within the synthetic standalone fixture boundary. Test-only partial methods set readiness and deliver authored primitive reports, labeled `SyntheticAuthoredCompletionAndReadyFixtures`; they do not run original assets. The idle GPU screenshot shows successor readiness and retained previous report counts, as intended. Physical OS window-manager routing was not executed. Controlled active close uses the existing child bridge with an inert test preview; this is not a full world shutdown proof.

Validation: private full Godot/Core and isolated HUD builds passed with 0 warnings/0 errors. Frozen HUD regressions passed 94 headless/95 GPU checks (22 failure cases, five native-teardown cases), and frozen81 synthetic shell admission suite passed25 checks. New inspection passed52 headless/25 GPU checks across eight/three scenarios. Runner guards passed8 checks. Scope audit preserved4,685 frozen files and found only two authorized existing-file edits plus15 additions; workspace validation found zero broken/private links and `git diff --check` passed. Keep these denominators separate from original-resource coverage. The first compile attempt rejected a test WM notification constant needing an explicit int cast; only the test call was repaired. No behavioral oracle was seeded from observed native output.

## Consequence, uncertainty and next action

Consume the primitive snapshot on the owner thread; nullable last-report counts are historical during successor activity. Run-limit/terminal/close blocking must remain visible and enforceable through both GUI and direct request. Only `_Process` performs controlled drain/owned-Free/quit; native exit remains abort and clears references.

Next probe: lead joins frozen81 plus each owned Round82 change, runs the full original-resource normal/fault/recovery scenarios and compares actual completion/admission/cleanup reports to inspection snapshots. Exercise actual active/idle/cap Close and optional physical OS close separately. Worker releases exclusive GPU ownership after sealing and remains IDLE. No ledgers or shared integration artifacts are edited by this bounded worker.
