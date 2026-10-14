# R6-RUNTIME-01: bounded creature initialization — October 2, 2026

Question: can one serialized owner compose accepted R5 registry handles and resource results, retain cancellation-ignoring workers within a hard budget, transfer one current owned result, and prevent every revised/stale/faulted/cancelled completion from publication? Stop after deterministic native barriers, one pinned original reader job, independent source audit, compiler/shared native and repository scope gates. No other Round 6 worker dependency or GPU work.

## Orientation and identity

Worker C:/Users/ancie/.codex/worktrees/9175/Stardust-Client; HEAD 65de8c88141071744dc75594be126b3bab3696a5. Accepted Round 6 baseline: 1855 public files. ZIP SHA-256 0e55506a789046b8a875688c83285e27e535a51fbe09b2c28e7696c679077453; manifest 3a1e812a4dd0d3de1c61b8e385a30c7851918c015b7757fe316296de4bd6d83a. Verified safe members, sizes and every hash before overlay, then every destination hash; ignored/unlisted material preserved.

Read START_HERE, its already oriented latest development report, [accepted Round 5](../sessions/PARALLEL_DEVELOPMENT_ROUND_5_2026-10-02.md), status/ledger/unknown terminology, R5 registry/resource contracts and checkpoints, existing registry/reader/catalog/background ownership APIs, source verifier and smoke fixtures. Existing clocks/registry/readers/transition/engine/runners remain read-only. No executable addresses or original executable input/execution are used.

Original inputs match [R5 resource identity](IMPLEMENTATION_CHECKPOINT_2026-10-02_R5_CREATE_RESOURCES.md): configuration SHA-256 22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524; human object CRC 2937962913, root E0B61445E8A85CCF0AC5937F0F9CBCA8711718AFEE63384F30970D5AEE120243; SAT 951FCE8C2FFC0EB318C751026D24D8C9BF17263D706EB3C93D015867EC801A9C. Canonical idle table/name/pin and the accepted human-locomotion.json walk/run routes are used without guessed filenames. The create envelope is synthetic, with full-width object ID and real original path CRC. Static original sources remain private.

## Findings, confidence and consequences

Observation: SceneCreatureResourceRequest copies mutable selection dictionaries/routes and scalar registry/create records before Start. Initializer binds one construction thread, verifies exact current snapshot/create bytes, and gives each real start a distinct owner/handle/attempt ticket. Same-handle Start remains idempotent after one-shot completion; restart is explicit. Interpretation: work identity cannot be silently retagged after registry changes or restarts. Confidence: Confirmed by source and native barriers. Consequence: injected callback closures must separately snapshot their own configuration and must not mutate the registry.

Observation: job capacity includes cancelled-but-unfinished and completed-but-unpolled tasks; object metadata has a separate bound. Restart/capacity tests retain old cancellation-ignoring workers and reject further starts atomically. Poll alone checks registry identity and transfers; Dispose uses bounded cleanup continuations without registry access. Interpretation: stale retirement cannot create an unbounded job collection. Confidence: Confirmed within configured limits. Consequence: cancellation is cooperative; indefinite ignoring work retains capacity/drain incompletion rather than being treated as finished.

Observation: deterministic TaskCompletionSource tests cover all registry invalidations, late success/fault/cancel, explicit new-revision completion before old fault, same-handle restart, owner-thread/reentrancy, tracked/outstanding capacity, retained request ownership and single transfer. Disposal tests wait actual cleanup barriers and verify zero outstanding jobs after late completion. Fault AggregateExceptions are observed. Interpretation: only current valid completed work can transfer. Confidence: Confirmed in synthetic fixtures. Consequence: consumers must still recheck current handles at later publication, and must dispose when abandoning polling.

Lead requested completeness validation for public loader results. Observation: compact retained-descriptor checks reject null/incomplete creature data, altered root/SAT pins, missing idle/walk/run/geometry/skin/skeleton/LAT/material closure, selected-idle inconsistency and bad source accounting. Synthetic positives construct a tiny authored CKAT and managed closure; forged negatives alter public records. Interpretation: readiness cannot be established by create identity alone. Confidence: Confirmed for tested forgeries. Consequence: trust the existing reader for deeper decoded correctness; this gate introduces no raw parser or recovered readiness predicate.

Observation: one Task.Run original reader job closes configured archives before its actual task completes, resolves 37 distinct paths exactly once / 4,683,848 bytes, and returns the pinned detail0 material/skeleton/geometry/three-clip closure. Owner Poll transfers it once with exact create bytes and no registry mutation. Independent Python reopens the effective overlay and checks every hash/size; a deliberately changed export hash rejects. Interpretation: the accepted original reader composes with this modern completion barrier. Confidence: Confirmed for pinned inputs; original asynchronous scheduling and terrain/minimum-scene readiness remain Unknown.

## Commands and validation

Run from this checkout. Use installed .NET 8 and bundled Python; no downloads. Accepted Core package lock remains byte-identical to baseline. Offline restore uses the main private NuGet cache and the owned sources-cleared configuration. APPDATA is task-specific to avoid inaccessible system NuGet settings. Native-only: no prototype host build/execution, headless renderer or GPU assertion.

    $env:APPDATA = Join-Path $env:TEMP 'stardust-runtime-r6-appdata'
    New-Item -ItemType Directory -Force $env:APPDATA | Out-Null
    $env:PYTHONUTF8 = '1'
    dotnet restore poc/round6-tests/Runtime/Runtime.csproj --configfile poc/round6-tests/Runtime/NuGet.Config --packages C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages -p:NuGetAudit=false
    dotnet build poc/round6-tests/Runtime/Runtime.csproj --no-restore -p:UseSharedCompilation=false
    dotnet poc/round6-tests/Runtime/bin/Debug/net8.0/Runtime.dll C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg poc/preview-profiles/human-locomotion.json > local-private/round6/native-original.txt
    python poc/round6-tests/Runtime/Verify-Original.py C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg local-private/round6/native-original.txt
    dotnet build poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj --no-restore -p:UseSharedCompilation=false
    dotnet poc/Stardust.Core.SmokeTests/bin/Debug/net8.0/Stardust.Core.SmokeTests.dll
    python tools/python/validate_organized_workspace.py .
    git diff --check

Where python is absent from PATH, use C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe. Console with no arguments executes synthetic barriers alone. Builds report zero warnings/errors; synthetic and original console, independent source audit and existing shared native suite pass. New scenarios compile in the shared project but execute through the owned console until lead runner registration. One source-hash-tampered export is rejected. The completeness addition initially needed the standard DoesNotReturnIf guard annotation for nullable flow; corrected before passing builds. An earlier build failure's previously built DLL output was not accepted as evidence for that source revision.

Original exported sources are read-only identity aggregates, not copied asset content. TaskCompletionSource and actual task completion waits replace sleeps, clock assumptions or busy polling. Attempt exhaustion, allocation failure, arbitrary malicious nested record construction and GC timing are not execution claims. Loader cancellation callbacks must be prompt; no forced termination or bounded worker runtime is promised.

Final repository checks: exactly eight new authorized files and no existing-file changes; zero broken/private links across 1053 Markdown files; 2602 provisional rows/zero duplicates and 345 packet rows. Focused new-file whitespace and git diff --check pass; status inspected, index empty and HEAD unchanged.

## Scope and handoff

Canonical conclusion: [initializer contract](../../docs/SCENE_CREATURE_RESOURCE_INITIALIZER_CONTRACT.md). Eight additions only: initializer, scenario, contract/checkpoint, and four owned Runtime console/audit files. Existing registry/resource reader/catalog/clocks/transition/engine/shared runner/status/ledger/registries remain unchanged. Baseline-relative scope/hashes, focused whitespace, links/workspace and final status checks accompany the handoff. Nothing staged/committed/pushed; HEAD unchanged.

Next exact action: lead reviews the eight-file manifest, integrates and registers SceneCreatureResourceInitializerScenarios.Run() in the shared runner; optionally run Original(root, configuration, bankProfile). A future renderer/scene consumer must recheck initializer.IsCurrent(ticket), including the transferred attempt and registry handle and define its own publication policy. No same-round placement/codec dependency or original scene-ready claim is introduced. Runtime stops after Round 6 until acceptance and a new assignment.
