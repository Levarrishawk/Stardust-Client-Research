# R11-RUNTIME-01: bounded coalesced postcommit changes

2026-10-02. Question: can a serialized consumer receive latest current world observations without intermediate replay batches, stale replacement deletion, unbounded history or silent loss of unconsumed IDs? Stop after focused/native/oracle/regression/ownership/cleanup and exact scope gates. Round 11 only.

## Inputs and orientation

Accepted public snapshot:2039 files, HEAD65de8c88141071744dc75594be126b3bab3696a5. ZIP SHA-256287F1BDEF5BCB0972AB09517C22B3C4BAE1397BCBBD6DDE9227AD93CDE5D4DA7; manifest48FB34D65ED9161E8472BC4D5ADC9675F5997747A7BB4202BD00D62B49114468. Verified both hashes, safe unique relative member paths/no symlinks, every member size/hash before overlay and every resulting hash afterward. Only own checkout synchronized; ignored/unlisted material preserved.

Read AGENTS/START_HERE/latest accepted10 report, current ledgers/function terminology, typed/opaque world and incarnation boundaries, prior checkpoints, existing codecs/independent tools and curated CreatureObjectDeltaMessage3 packet row. No new schema, names or coverage denominator. No executable/original-asset input or VA claim; original identity verification not needed for this state-only task. The controlled peer and managed resource closure are independently authored synthetic data, not captures/decompiler output.

Installed dotnet8/Python, existing offline package cache, own sources-cleared NuGet.Config, task-specific temporary APPDATA, NuGetAudit=false and UseSharedCompilation=false. No downloads/lock edits, original execution, live/socket/DNS/capture/fuzzing, GPU/engine edits, other-chat messaging, staging/commit/push/publication. Only assigned coordinator file changes among existing files; shared projects/runners/status/ledgers/registries remain read-only.

## Observations and conclusions

- Observation: a bounded map retains only object ID, ORed lifecycle/replication flags and first undelivered mutation revision. Drain resolves latest current object/replication state into an owned immutable batch. Destroy/recreate resolves the final replacement; parent destroy also records directly detached children. Interpretation: activity can coalesce without retaining old object snapshots or event history. Confidence: **Confirmed deterministic native/model outputs; modern policy**. Consequence: consumers act on current observation or tombstone, not replayed historical delete events.
- Observation: Drain prepares a batch but does not remove slots. Acknowledge clears only current owner/generation/active-state/mutation-revision batches; stale acknowledgment leaves IDs pending for fresh resolution. First-touch order survives intermediate destroy/recreate; equal-revision multi-ID changes sort by unsigned ID. Confidence: **Confirmed native/oracle behavior**. Consequence: a consumer never loses unconsumed IDs merely because its held batch became stale. The API is one acknowledged serialized consumer, not independent subscriptions.
- Observation: staging has no drainable batches. Failed replay/live mutation, selection and close clear slots and invalidate held batches; capacity and U64 sequence overflow fail the whole generation without partial batch/ready. Checked preflight precedes object application. Confidence: **Confirmed native negatives**. Consequence: no silent eviction makes room; consumers must handle global generation/failure/close separately. Counter exhaustion requires a fresh coordinator; Close works at maximum revision.
- Observation: resource polls/publications do not advance delivery revision or mark IDs. Existing resource attempt/exact-handle and typed incarnation/local-state checks remain separate. An actual loader callback cannot reenter drain; a dedicated foreign thread is rejected. Confidence: **Confirmed native controlled-task/thread tests**. Consequence: observer delivery adds no callbacks, thread dispatch or resource lifecycle ambiguity.
- Observation: independent496-byte request/response/framing/reliable-order checks and a deterministic registry/incarnation/coalescing/scalar model match every exported batch, including1000 baseline/delta pairs with one pending slot. Confidence: **Confirmed synthetic offline wire/state audit**. Consequence: existing128-byte opaque peer is untouched; no fragmentation/live/original-observer claim.

The [contract](../../docs/SCENE_WORLD_CHANGE_DELIVERY_CONTRACT.md) specifies exact API, acknowledgment semantics, bounds, ordering, compatible final optional constructor policy and next-consumer usage. Accepted idempotent lifecycle operations/missing-ID destroys count as modern activity. Root0 destruction does not mark unrelated root objects as detached children. Current=null does not manufacture a baseline/resource. Opaque and typed snapshot semantics remain unchanged.

## Validation

Passed focused offline restore/build with zero warnings/errors; native positive/negative scenarios; Verify-Changes.py independent peer/model audit. Covered owned lists and retained immutable observations; one-slot coalescing and latest state; first-touch/tie order; stale acknowledgments; equal generation/revision foreign owner; tombstone followed by same-ID recreation; direct-child detach; no default seed; staged/failed replay/live failure; acknowledged capacity reuse and unseen-capacity failure; replay/live sequence exhaustion; owner thread/real callback reentrancy; reset/close/fault cleanup; and resource delivery separation. Pending task cancellation/late fault is observed via accepted initializer.

Shared build and --creature-replication run passed, including accepted registry/initializer/full-baseline/exact-store boundaries. Accepted R10 typed and R7 opaque world isolated builds/runs passed. No optional GPU/original proof is needed for serialized native metadata delivery. No new failures occurred in product or test execution.

Focused exact commands/input-output paths are in the contract: local-private/round11/peer.json to local-private/round11/export.json. Python executable C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe. Regression commands used already generated accepted synthetic inputs; regenerate through their accepted producers when absent:

```powershell
dotnet build poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj --no-restore -p:UseAppHost=false -p:UseSharedCompilation=false -p:NuGetAudit=false
dotnet poc/Stardust.Core.SmokeTests/bin/Debug/net8.0/Stardust.Core.SmokeTests.dll --creature-replication local-private/round9/baseline.bin local-private/round8/peer.json
dotnet build poc/round10-tests/Runtime/Runtime.csproj --no-restore -p:UseSharedCompilation=false -p:NuGetAudit=false
dotnet poc/round10-tests/Runtime/bin/Debug/net8.0/Runtime.dll local-private/round10/peer.json local-private/round11/prior-typed-export.json
dotnet build poc/round7-tests/Runtime/Runtime.csproj --no-restore -p:UseSharedCompilation=false -p:NuGetAudit=false
dotnet poc/round7-tests/Runtime/bin/Debug/net8.0/Runtime.dll local-private/round7/peer.json
python tools/python/validate_organized_workspace.py .
git diff --check
```

Passed final gates: focused baseline-relative source/encoding/whitespace review, workspace validator (zero broken/private links, 2,602 unique names and345 packet rows), exact review (eight assigned additions plus one coordinator edit; no other accepted public changes), unchanged HEAD/empty index and final git status inspection. Independent oracle also rejects a deliberately corrupted replacement-as-removal export. No private evidence links, proprietary corpus or binaries in curated files. Research/function/protocol measures remain separate and unchanged; lead owns registration and living-document consolidation.

## Resume

Lead review/integrate scope hashes and register SceneWorldChangeScenarios.Run(peer, optionalExportPath). Rendering12 may consume a current drain batch, separately guard resource/replication data, and acknowledge only after applying it. Stale ack retries remain bounded and latest-state-based. No next-round renderer, subscriptions, wounds API dependency, original scheduling or live work is included. Worker stops after Round11 pending explicit Round12 dispatch.
