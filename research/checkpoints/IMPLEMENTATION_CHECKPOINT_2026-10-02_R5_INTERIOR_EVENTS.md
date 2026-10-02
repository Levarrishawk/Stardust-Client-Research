# R5-RUNTIME-01: interior accepted events — October 2, 2026

Question: can an opt-in diagnostic ring consume the accepted Round 4 event clock on the existing original interior's post-collision ticks, preserve old state/presentation, reject invalid delivery atomically, and retain resources through controls/owner changes? Stop after native, original headless, default-route, compiler and scope gates, then handoff. GPU execution belongs to the lead after Rendering stops.

## Identity and orientation

Worker: C:/Users/ancie/.codex/worktrees/9175/Stardust-Client. HEAD 65de8c88141071744dc75594be126b3bab3696a5. Round 5 accepted snapshot: 1824 public files; ZIP SHA-256 705a7fb6d48cc95fd49efaf764761fd879cb218a26190c8945c48b33743dfcd8, manifest 77d479877ea2ed1a4b8a79a50c23df12daa5100f03c0a9521c09ef513badd12f. Verified safe paths, every member size/hash, then every resulting overlay hash; ignored/unlisted files preserved. No dependency on another worker's unfinished Round 5 changes.

Read START_HERE and its latest development report, the [accepted Round 4 report](../sessions/PARALLEL_DEVELOPMENT_ROUND_4_2026-10-02.md), current status/ledger/unknown terminology, prior locomotion/event/diagnostic/interior/camera contracts and checkpoints, existing scene/raw route, launcher, synthetic fixtures and scope helper. No original executable addresses or execution used.

Original configuration SHA-256 22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524. Scene specification SHA-256 12BF5B21EC0D7BDA15CB322BB14FF5FC4C7C640B81E7C1DB38CA11F66D9349CA selects mining-outpost doors 5/1 and 6/1, leaves 99C8ED2EEE49BF44DB9CCDFBA4436A2A56949389D3AB79306A1196493481A3D3. POB 79085C909D585A9530017150EB07DA49EDAABA1E010C48D845FFD19F97B4D818; door table C4C572DEBD51DC9D77E57E7D5A8D6741A01A03CAF00BF31601180A05551536B7. Human object/SAT/idle/walk/run use the [accepted Round 4 pins](IMPLEMENTATION_CHECKPOINT_2026-10-02_R4_LOCOMOTION_EVENTS.md), resolved by the existing bank chain rather than guessed filenames. FCHS 01A19A5B18E81C9366B86B5F313C31D8F0C1961F8230DB4535AA5C50E95C543D. Original source union: 123 sources / 15,389,461 bytes; retained character texture cache 13 / 18,524,828 bytes. All asset reads are static in replacement readers; no original executable or external contact.

## Observation, interpretation and consequence

Observation: TickScene calls the existing locomotion clock and the new event helper using the same accepted world delta, seconds and animation active flag. The helper asserts exact clock-state equality on every tick. Interpretation: events track modern accepted gait phase without changing old presentation. Confidence: Confirmed by source and original route execution. Consequence: preserve the post-collision tick integration point; render elapsed/intended delta must never be substituted.

Observation: the new bounded sink retains one frontier and fixed ring, rejects complete invalid batches before mutation, keeps lifetime totals across epoch/reset changes, and does not change AnimationEventDiagnosticLog. Native fixtures pass capacities 1/3/4096, three duplicate frame identities, 2000 ticks per capacity, owned snapshots, forward/backward/replay/empty/stale/foreign checks and clear/reset/switch behavior. Interpretation: the R3 delivery policy applies to accepted-movement owners. Confidence: Confirmed within tested limits. Consequence: diagnostics are ordered delivery counts; disabled/discarded batches do not catch up. Long-counter exhaustion guard is inspected, not exhausted in execution.

Observation: original headless full interior passes both owner poses; each walk/run produces six ordered events over three wraps (frames 7/23 and 6/17), all-gait state equality, blocked-history clearing, pause/focus/reset/clear/disable and teardown. Topology and texture counts/bytes remain retained, and the helper has no asset resolver/resource creation path. Existing door/collision/key/mouse/camera assertions and default-route regression pass. Interpretation: this modern diagnostic integrates with the existing retained interior. Confidence: Confirmed for pinned inputs; original event scheduling/crossfade timing remains Unknown. Consequence: no audio/effects/root motion or compatibility score claim.

Lead requested visible capture diagnostics. Only opt-in capture waits for UI layout and calls ScrollContainer.EnsureControlVisible on the label before rendered-frame waits; default scroll/camera behavior remains unchanged. Compiled successfully; GPU visibility is deliberately unrun by the worker and remains a lead inspection gate.

## Reproduction and checks

Use installed dotnet/.NET 8, bundled Python and the existing Godot 4.7.2 replacement engine. No toolchain/dependency download. Set APPDATA to a task-specific temporary directory because the system NuGet configuration is inaccessible in the sandbox. Native restore uses the owned sources-cleared NuGet.Config and the main accepted offline package cache. Main core-cache package bytes match the existing worker/user copies for five dependencies, and their metadata contentHash agrees with the accepted lock files. Cache archive/stored SHA-512 differs from lock contentHash; these were recorded separately, not claimed equivalent. Godot.NET.Sdk is in the existing default user resolver cache, matching the prior worker copy (archive SHA-256 679fe9c6736ca1943b06732ac5113f50c248ea0280593ff7db08814b72057acb); it is absent from main's core-package cache. Remove NUGET_PACKAGES for restored Godot builds. Accepted package locks remain byte-identical to baseline. Private handoff records include assets/lock/cache hashes.

Commands from repository root:

1. Set APPDATA to a fresh task-specific temporary directory and create it; remove NUGET_PACKAGES for Godot.
2. dotnet restore poc/round5-tests/Runtime/Runtime.csproj --configfile poc/round5-tests/Runtime/NuGet.Config --packages C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages -p:NuGetAudit=false
3. dotnet build poc/round5-tests/Runtime/Runtime.csproj --no-restore -p:UseSharedCompilation=false
4. dotnet poc/round5-tests/Runtime/bin/Debug/net8.0/Runtime.dll
5. Build restored poc/Stardust.Godot/Stardust.Godot.csproj and poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj with --no-restore -p:UseSharedCompilation=false; execute the shared SmokeTests DLL.
6. Run poc/round5-tests/Runtime/Verify-Options.ps1: 19 launcher negatives reject before nonexistent paths can resolve. The original self-test additionally runs 41 raw-option missing/duplicate/conflict checks before asset reads.
7. Run poc/round5-tests/Runtime/Invoke-Original.ps1 with GodotPath and SceneSpecification below. Repeat with -DefaultRoute for the existing full-route regression.
8. python tools/python/validate_organized_workspace.py .; git diff --check; new-file whitespace checks; baseline-relative parallel_round_workspace.py review.

Exact headless/lead GPU inputs and command (PowerShell; GPU command is provided, not executed here):

    $eventGodot = 'C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/toolchain/godot-4.7.2-dotnet/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe'
    $eventSpec = 'C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/generated-evidence/precise-door-actor-milestone/original-scene/scene-spec.json'
    & poc/round5-tests/Runtime/Invoke-Original.ps1 -GodotPath $eventGodot -SceneSpecification $eventSpec
    & poc/round5-tests/Runtime/Invoke-Original.ps1 -GodotPath $eventGodot -SceneSpecification $eventSpec -DefaultRoute
    & poc/round5-tests/Runtime/Invoke-Original.ps1 -GodotPath $eventGodot -SceneSpecification $eventSpec -Gpu -CapturePath "$PWD/local-private/round5/interior-events-fresh.png"

Create the private capture directory first and choose a fresh filename. The existing launcher enforces its 45-second deadline. Inspect pose, interior framing and the visible event/lifetime label. Worker original final headless log ends with the teardown assertion; default headless ends with original camera assertions. Known certificate-store startup error persists despite passing offline runs. No GPU/readback/visual proof is inferred from headless tests.

Initial compiler checks exposed a duplicated shared record declaration and helper accessibility mismatch; both fixed before passing builds. A synthetic ring-fill assertion needed 2000 ticks to fill capacity 4096; the corrected fixture passes. The package audit initially compared archive SHA-512 with NuGet contentHash; final audit separates archive identity, stored hashes and metadata/lock identity. No package/cache/lock repair or global environment changes were made.

Validation outcome: workspace validator reports 1044 Markdown files, zero broken links/private links, 2602 provisional rows with zero duplicates and 345 packet rows. Baseline-relative scope review reports exactly twelve authorized changes; git diff --check and focused new-file whitespace checks pass. The Main partial retains its accepted LF endings; launcher retains CRLF. Final status inspected, index empty, HEAD unchanged.

## Scope and handoff

Canonical contract: [interior diagnostics](../../docs/INTERIOR_LOCOMOTION_EVENT_DIAGNOSTICS.md). Twelve files: ten additions and two byte-preserving existing-file edits (Main.InteriorScene.cs and Invoke-StardustAssetPreview.ps1). Existing clocks, elapsed-playback log, shaders/mesh, shared runner/status/ledger/registries remain untouched. Baseline-relative before/after hashes and focused diff accompany the private handoff. Nothing staged/committed/pushed; HEAD unchanged.

Next exact action: lead reviews the twelve-file manifest, integrates, registers LocomotionEventDiagnosticScenarios.Run() in the shared runner, then runs the fresh GPU command after Rendering is idle. Original scheduling and crossfade event timing remain Unknown; long-counter exhaustion and capture-only visibility remain unexecuted here. Runtime stops after Round 5 until lead acceptance and Round 6 assignment.
