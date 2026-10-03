# R15-R object presentation consumer

October 2, 2026. Question: can one bounded engine-neutral consumer safely own heterogeneous exact world publications and acknowledge only observations successfully applied by adapters? Stopping condition: frozen API implemented with deterministic native ownership/failure/retry tests, baseline regression and exact scope gates; no next milestone.

## Input identity and orientation

Verified Round15 baseline manifest SHA-256 `1736485311ac5622ceaed02758295293b307ccc52d09c9e982d8b8440b86be1f`, ZIP `920d8655531c68c524d42bd9db2b28a35a91c8b9fa7462328282a7c00ba2835b`, 2,197 files. Snapshot HEAD `1fc55dd89cd23ce3af49e3fe471210bdebe9735c`; isolated checkout HEAD remains `65de8c88141071744dc75594be126b3bab3696a5`. Lead explicitly corrected the stale assignment HEAD; no Git checkout/history/index mutation occurred. All ZIP members preflighted for exact unique manifest set, safe relative paths/resolved containment/no symlinks, length/SHA before writes, and every overlaid output SHA afterward. Listed bytes only; ignored/unlisted files preserved.

Read AGENTS, START_HERE/[latest Round14 report](../sessions/PARALLEL_DEVELOPMENT_ROUND_14_2026-10-02.md), [Round15 preparation](../sessions/ROUND_15_PREPARATION_2026-10-02.md), current status/ledger/UNKNOWN, relevant mixed/initializer/observer/preview contracts/checkpoints, frozen interface, coordinator, old consumer, accepted scenario/producer/curated packet map. No VA claim, so no executable address reliance. Original configuration exact pin `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524` checked by the scenario; C:/Stardust-DEV is read-only. Existing Python/.NET8/offline cache only, sources-cleared restore/task APPDATA/no shared compiler. Synthetic wire fixture is accepted Round14 producer output, not a new protocol recovery.

## Evidence, interpretation and consequences

Observation: [new consumer](../../poc/Stardust.Core/Session/SceneWorldObjectPresentationConsumer.cs) stores one adapter/exact publication/creation incarnation per object, checks coordinator resource and typed state freshness separately, reads latest snapshots at resource acceptance and tracks the exact applied batch reference. Missing adapters are acknowledged and later accept current state. Interpretation: engine-neutral modern presentation ownership without duplicating world/state/loading. Confidence: Confirmed source/native tests. Consequence: adapters own engine roots and expose the frozen publication; typed state and opaque static behavior remain separate.

Observation: candidate callbacks run under thread/reentrancy guards and local rollback ownership; exact idempotent acceptance performs no new Apply. Replacement validates/prepares candidate before retiring owned old adapter. Foreign/stale batches, same-revision different batch references and callback-staled batches never acknowledge. Later-adapter false/error/staleness tests preserve earlier application and pending observations, then retry successfully. Interpretation: serialized retryable application, not GPU/visual rollback transaction. Confidence: Confirmed deterministic fake-adapter tests. Consequence: integration must tolerate reapplication; state advancement uses batches rather than repeated idempotent acceptance.

Observation: capacity rejection precedes factory; arbitrary errors propagate, faulting installed adapter retires, disposal detaches ownership before callbacks and attempts every adapter. Dispose releases captured world/factory even with aggregated cleanup failure. Global generation/failure/close retire roots; cancellation-ignoring jobs remain world-owned until drain. Interpretation: bounded logical roots with explicit failure/cleanup policy. Confidence: Confirmed native negative/late-fault tests. Consequence: factory owns unreturned partial allocations; consumer cannot undo engine side effects or infer Height-specific failures from the generic interface.

Original human/terminal completions use baseline sealed router/options: dedicated workers own and close archives, clear borrowed buffers while owned fingerprints remain stable, and enforce 39/27 source closures. This reuses accepted loaders for genuine publications; it is not a new 66-source pixel/original-corpus oracle or rendering proof. New tests create no reflection-forged publication or copied decompiler implementation.

## Reproduction and validation

Scenario entry: `SceneWorldObjectPresentationScenarios.Run(string[] inputs, IReadOnlyDictionary<string,string> peer, string exportPath)`, inputs exactly root/config/catalog/bank. Own [runner](../../poc/round15-tests/Runtime/Runtime.csproj) takes six command arguments and links the new scenario; shared harness remains lead-owned.

```powershell
$env:PYTHONUTF8='1'
$env:APPDATA=Join-Path $env:TEMP 'stardust-runtime-r15-appdata'
$env:NUGET_PACKAGES='C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
New-Item -ItemType Directory -Force $env:APPDATA,local-private/round15 | Out-Null
python poc/round14-tests/Runtime/Generate-Peer.py local-private/round15/peer.json
dotnet restore poc/round15-tests/Runtime/Runtime.csproj --configfile poc/round15-tests/Runtime/NuGet.Config -p:NuGetAudit=false
dotnet build poc/round15-tests/Runtime/Runtime.csproj --no-restore -p:UseAppHost=false -p:UseSharedCompilation=false -p:NuGetAudit=false
dotnet poc/round15-tests/Runtime/bin/Debug/net8.0/Runtime.dll C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg poc/preview-profiles/scene-object-catalog.json poc/preview-profiles/human-locomotion.json local-private/round15/peer.json local-private/round15/export.json
python tools/python/validate_organized_workspace.py .
git diff --check
```

Executed new native runner: PASS, 138 checks; zero warnings/errors. Cases include both recipes/latest delayed state, idempotence, capacity, constructor/retry/time bounds, foreign owners, exact applied reference, stale acknowledgment, restart/recreate, null/false/wrong/recycled candidates, candidate callback exception/staleness/generation, factory/adapter/dispose reentrancy, foreign thread, later-adapter false/throw/stale and successful retry, arbitrary Tick/Apply errors, disposal aggregation, global scene failure, close/cancel/late fault drain. Build initially identified two unused test declarations; these were removed/initialized before the successful run. Accepted R14 mixed-world native regression and shared native default suite also pass with zero-warning builds. Final workspace/scope/whitespace/Git-state results accompany the handoff.

## Scope and next action

Only seven additions: new core consumer, new SmokeTests scenario, this checkpoint, one contract, and own Runtime Program.cs/Runtime.csproj/NuGet.Config. Frozen interface/coordinator/old consumer/shared entry points/current docs/status/ledger/navigation/curated data are untouched. Logs/fixture/export/byte hashes/diffs remain ignored; no assets/executable/private corpus promoted.

Lead edits needed: register scenario Run in shared native harness; construct consumer with accepted recipe adapters, poll coordinator and explicitly AcceptResource, consume acknowledged changes and Tick, Dispose roots before closing/awaiting externally owned world. Reconcile navigation/current contracts/status/ledger as lead-owned. No new Height diagnostic API is needed or invented. Limits: no GPU/visual rollback, hostile adapter isolation, external actor ownership, original callback/render semantics, parent transform graph, new protocol schemas or live contact. Exact next probe is joined consumer-to-recipe-adapter native/Godot/headless/cancellation proof. Worker stops after Round15; no next milestone or cross-chat messages.
