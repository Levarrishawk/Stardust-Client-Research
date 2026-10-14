# R16-R bounded presentation pump

October 2, 2026. Bounded question: can an engine-neutral step loop own copied current-generation input, retain every eligible completion until accepted, bound publication/change retries, and close/drain all owned work on failure? Stopping condition: frozen API implementation, deterministic positive/negative/cleanup tests, baseline regressions and exact scope gates; stop after handoff.

## Identity and method

Round16 full overlay verified: 2,237 public files; manifest SHA-256 `c2bc35eea45fd6f86d968b4ae28fb9034c7ffce3225691670abd30f0efe9a8ee`; ZIP `8c5a649747ad8a7aa5d9d1b25bab32fd603c6f2ddea97afe4b30d324c28eac32`. Snapshot manifest HEAD `1fc55dd89cd23ce3af49e3fe471210bdebe9735c`; isolated checkout HEAD remains `65de8c88141071744dc75594be126b3bab3696a5`. Exact unique manifest/member set, all safe relative/resolved contained paths/no symlinks, every size/SHA preflighted before writes; all overlaid output hashes verified. Listed bytes only; unlisted/ignored files/index/HEAD preserved. No main edit or Git publication/mutation.

Read AGENTS, START_HERE/[accepted Round15](../sessions/PARALLEL_DEVELOPMENT_ROUND_15_2026-10-02.md), status/ledger/UNKNOWN, mixed/consumer/preview contracts and R15 consumer integration/checkpoints, frozen pump interface, coordinator/initializer/consumer/scenario APIs, curated packet map and existing producer/workspace tooling. No new VA use or executable research. Original root C:/Stardust-DEV is read-only; exact cfg pin `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524` verified by scenario. Existing cached Python/.NET8/offline NuGet only; task APPDATA/sources-cleared restore/no shared compiler. All generated fixture/export/log/diff/hash files remain ignored. No downloads, sockets/DNS/live contact, original executable, asset edits or proprietary publication.

## Evidence and implementation consequences

Observation: pump (external historical reference) owns copied generation-tagged FIFO bytes, retained publication/weight/attempt bookkeeping and one newly constructed accepted consumer. Entire completion bursts are preflighted before any factory in that burst; SourceBytes default and independent injected nonnegative logical weights are checked. Interpretation: bounded modern scheduling over the existing world, not an extra registry/loader or recovered timing. Confidence: Confirmed native queue/count/byte/size/per-step, burst count/weight/overflow/negative/callback and ownership tests. Consequence: host keeps explicit selection/barrier responsibilities; logical weights are not total memory accounting.

Observation: false publication acceptance retains and rotates current attempts, at most once per step and under per-step/total retry bounds; stale attempts retire without exhausting retries. State batches retain exact consumer acknowledgment semantics and bounded frame retries, preserving unapplied changes for later steps; idle/pre-ready Tick false is normal. External reset clears stale retained publications/actors while queued stale entries remain charged to normal per-step FIFO processing; it returns to host-controlled initialization. Interpretation: continuous bounded scheduling with separate resource/state lifetime and retryable adapter application. Confidence: Confirmed FIFO/false-then-success/exhaustion, retained-weight restart/reset, normal stale budget, delayed latest state and callback reset tests. Consequence: no ready policy or opaque static semantics is inferred; no completion is silently discarded by a per-step publication limit.

Observation: callback exception/reentry latch, current world/resource failures, accounting overflow and exhausted publication retries close retained queue/resources/actors. Close tries consumer and world cleanup, captures world.DrainCompletion after close even when disposal errors occur, and preserves initiating plus cleanup failures. Interpretation: deterministic owned failure/drain boundaries. Confidence: Confirmed factory/Apply/Tick/weight/reentry/thread, stale/current late fault, malformed input, finite-time, cleanup exception and delayed cancellation-ignoring fault tests. Consequence: caller awaits the captured close receipt; blocking work is cancelled cooperatively, and engine side effects/unreturned factory roots cannot be rolled back by the pump.

Tests reuse accepted synthetic Round14 wire layouts and baseline original sealed router completion loading: dedicated workers own/close configured archives, clear borrowed buffers while CPU payload fingerprints remain stable, and check human39/static27 source closures. Genuine coordinator-issued publications only, no reflection forgery. This is fixture reuse and ownership evidence, not a new full corpus/pixel audit, original scheduler or GPU proof. Durable [contract](../../docs/SCENE_WORLD_PRESENTATION_PUMP_CONTRACT.md) specifies ordering, counters, failure/borrowed ownership and limits.

## Reproduction and results

Scenario entry: `SceneWorldPresentationPumpScenarios.Run(string[] inputs, IReadOnlyDictionary<string,string> peer, string exportPath)`, inputs root/config/catalog/bank. Own runner (external historical reference) takes six arguments and links the scenario; shared entry point is lead-owned.

```powershell
$env:PYTHONUTF8='1'
$env:APPDATA=Join-Path $env:TEMP 'stardust-runtime-r16-appdata'
$env:NUGET_PACKAGES='C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
New-Item -ItemType Directory -Force $env:APPDATA,local-private/round16 | Out-Null
python poc/round14-tests/Runtime/Generate-Peer.py local-private/round16/peer.json
dotnet restore poc/round16-tests/Runtime/Runtime.csproj --configfile poc/round16-tests/Runtime/NuGet.Config -p:NuGetAudit=false
dotnet build poc/round16-tests/Runtime/Runtime.csproj --no-restore -p:UseAppHost=false -p:UseSharedCompilation=false -p:NuGetAudit=false
dotnet poc/round16-tests/Runtime/bin/Debug/net8.0/Runtime.dll C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg poc/preview-profiles/scene-object-catalog.json poc/preview-profiles/human-locomotion.json local-private/round16/peer.json local-private/round16/export.json
python tools/python/validate_organized_workspace.py .
git diff --check
```

New native tests PASS: 171 checks, atomic tally reset each invocation; build zero warnings/errors. Cases cover copied mutated input, admission-before-world-mutation, explicit barrier, message FIFO/budgets, retained burst/per-step limits, logical weight release/default/overflow, retry fairness/exhaustion, missing actors/latest typed state, opaque static updates, thread/callback reentrancy, callback generation reset, source/candidate errors, stale/current late faults, finite time, queue/actor/resource close and awaited delayed drain even with disposal failure. Lead review corrected reset ingress cleanup to preserve FIFO/per-step drop bounds and corrected change attempts to a frame work budget with later-step retry. New reset mixed-ingress and false/stale batch subsequent-frame tests pass. R15 consumer regression passes 160 checks; shared native default suite passes with a zero-warning build. Final workspace/scope/whitespace/Git-state gates accompany handoff. No required private input/tool was absent. GPU/host integration and live execution are unrun/outside this worker scope.

## Exact scope and lead handoff

Seven additions only: core SceneWorldPresentationPump.cs; SmokeTests SceneWorldPresentationPumpScenarios.cs; docs/SCENE_WORLD_PRESENTATION_PUMP_CONTRACT.md; this checkpoint; own poc/round16-tests/Runtime Program.cs, Runtime.csproj, NuGet.Config. Frozen interface/policy/coordinator/consumer/shared project/Program/navigation/current docs/status/ledger/curated data unchanged.

Lead registers scenario Run in shared native harness; instantiate pump with accepted recipe adapters, enqueue decoded application messages with selected generation, drive Step from one owner loop, explicitly complete minimum barrier, and await pump.DrainCompletion after Close/Dispose. Borrowed Consumer supports inspection; no competing manual consumer or world resource polling. Integrate any independently accepted weight estimator explicitly after review. Next suggestion: joined continuous historical implementation path (omitted) proof and subsequent bounded movement/interior host integration. No original timing/schema/fidelity/GPU claim; no next milestone started or cross-chat message. Worker stops after Round16.
