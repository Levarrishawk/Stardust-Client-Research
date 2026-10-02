# R10-RUNTIME-01: opt-in typed world replication

2026-10-02. Question: can the existing generation-owned world replay complete CREO:3 baselines and six-scalar plans atomically, retaining incarnation state through lifecycle revisions while preserving exact resource freshness and opaque defaults? Stop after synthetic offline/native/oracle/regression/ownership/cleanup and exact scope gates. Round 10 only; no Round 11 work.

## Baseline and orientation

Accepted public snapshot: 2000 files, HEAD 65de8c88141071744dc75594be126b3bab3696a5. ZIP SHA-256 B22195465DD313D509571CA0D732A5FFFB058658D41EE2A41750B82448FB4E2A; manifest 58C635C920215D7FBB47DE5D8D7B9C636F96291B8B841E61593E195529072F82. Verified hashes, safe unique relative member paths/no symlinks, every size/hash before overlay and every resulting file afterward. Only own checkout changed; ignored/unlisted files preserved.

Read AGENTS/START_HERE/latest accepted Round 9 report, current ledgers/function terminology, world/incarnation/baseline/scalar contracts and prior checkpoints, accepted code/independent tools and curated CreatureObjectDeltaMessage3 packet row. No new schema/address/coverage denominator. No executable or original asset input, hence no VA or original SHA-dependent claim. New resource test closure reuses independently authored managed synthetic fixture design, not original bytes.

Installed dotnet 8 and Python, existing offline package cache, own sources-cleared NuGet.Config, task-specific temporary APPDATA, NuGetAudit=false and UseSharedCompilation=false. No downloads, lock edits, original execution, live/socket/DNS contact, capture/fuzzing, GPU/engine edits, other-chat messaging, staging/commit/push/publication. R9 registry/store, accepted codecs, shared runners/projects and living documents remain read-only.

## Observations, interpretation, confidence, consequences

- Observation: a final optional immutable policy enables typed baseline/scalar Decode before staging; null retains the old envelope decoder. Barrier replay applies owned messages in order, uses the current private registry incarnation and accepted store, and releases exact ready only on complete success. A later unseeded second object clears an earlier valid seed and all objects. Interpretation: typed semantics can fit the existing whole-generation failure boundary. Confidence: **Confirmed native synthetic behavior; modern policy**. Consequence: missing object/baseline, unsupported schema/index or budget errors never leak a partial replay publication.
- Observation: repeated complete baselines replace one bounded seed and reset overlay; scalar deltas apply atomically without retaining plans or envelopes. 1000 additional baseline/delta pairs preserve 119 logical bytes and zero opaque history, despite history limits of one envelope/25 bytes. Interpretation: state budgets can be independent of receipt count. Confidence: **Confirmed native/oracle stream outputs**. Consequence: initial staging remains bounded; post-barrier typed traffic cannot exhaust an append-only message history. Aggregate baseline bytes and object capacity still constrain retained state.
- Observation: scalar/baseline events keep a current resource ticket and do not invoke the loader; lifecycle revision changes retain state incarnation but invalidate resource handle/attempt freshness as before. World-owned state guard rejects old local revision, foreign world, old generation, destroyed/recreated object and failed/closed scene. Confidence: **Confirmed native observations**. Consequence: consumers must separately validate current state and current resources; no automatic presentation behavior is implied.
- Observation: handshake/response and all new peer frames declare/check negotiated496 bytes; full baseline fits496 but not the older128-byte peer. Existing SOE reorders create/start, suppresses duplicate sequence, rejects corrupt CRC and emits exact ready/retry. Independent oracle decrypts/rechecks frame CRC, sequence/application bytes, complete baseline and ordered scalar bits. Confidence: **Confirmed synthetic offline wire audit**, not capture/live evidence. Consequence: the old opaque peer is untouched; no fragmentation behavior is claimed by this proof.
- Observation: failed/selected/closed worlds immediately lose typed state while ignored cancellation stays in the resource budget until completion; late faults are observed and can release capacity for the new generation without affecting its state. Foreign thread and actual loader-callback query reentrancy reject. Confidence: **Confirmed controlled TaskCompletionSource and dedicated Thread tests**. Consequence: typed storage adds no unbounded worker or registry access path.

The [contract](../../docs/SCENE_WORLD_REPLICATION_CONTRACT.md) lists exact signatures, limits, policies and reproduction for subsequent render binding. Height/posture are raw state only; source placement is separate. Full baseline wounds/components stay immutable seed observations; index17 and other unsupported delta schemas are strictly rejected. Arbitrary old same-generation payload bytes have no wire incarnation discriminator; ordered caller delivery remains a precondition, not an invented protocol field.

## Validation and reproduction

Passed focused offline restore/build (zero warnings/errors), typed native scenarios and independent Verify-World.py. Positives: actual offline SOE/lifecycle/baseline/delta bytes; no prebarrier world/load; false/stale barrier; ordered ready; duplicate baseline reset; exact raw bits; retained incarnation through changed containment; resource freshness separation; owned arrays/bytes; foreign world/thread; query reentrancy; 1000-pair bounded stream; destruction/recreation/selection retirement; stale generation before parse; late old-generation failure/backpressure; close/drain. Negatives: wrong tag/view, unsupported wounds index, malformed baseline/delta, missing object, delta-before-baseline, late replay failure after an earlier seed and aggregate budget failure. Each clears the whole world and returns no ready publication where applicable.

First focused build failed because the new test named the existing retry API GetRetransmissions rather than GetRetransmissionDatagrams. Corrected the test call; final build/run passed. No product transport change. Shared native build and --creature-replication regression passed; accepted R7 opaque world and R9 incarnation-store isolated builds/runs passed. No repeated GPU/original proof is required for this native state-only change.

Focused exact commands/input-output paths are in the contract: local-private/round10/peer.json to local-private/round10/export.json, both ignored synthetic outputs. Python executable C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe. Regression commands used already generated accepted inputs; regenerate with their existing producers when absent:

```powershell
dotnet build poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj --no-restore -p:UseAppHost=false -p:UseSharedCompilation=false -p:NuGetAudit=false
dotnet poc/Stardust.Core.SmokeTests/bin/Debug/net8.0/Stardust.Core.SmokeTests.dll --creature-replication local-private/round9/baseline.bin local-private/round8/peer.json
dotnet build poc/round7-tests/Runtime/Runtime.csproj --no-restore -p:UseSharedCompilation=false -p:NuGetAudit=false
dotnet poc/round7-tests/Runtime/bin/Debug/net8.0/Runtime.dll local-private/round7/peer.json
dotnet build poc/round9-tests/Runtime/Runtime.csproj --no-restore -p:UseSharedCompilation=false -p:NuGetAudit=false
dotnet poc/round9-tests/Runtime/bin/Debug/net8.0/Runtime.dll local-private/round9/peer.json local-private/round10/prior-store-export.json
python tools/python/validate_organized_workspace.py .
git diff --check
```

Passed final gates: workspace validator (zero broken/private links, 2,602 unique names and 345 packet rows), inspected focused source diff and UTF-8/LF/whitespace, baseline-relative exact review (eight assigned additions plus one coordinator edit; no other accepted public changes), unchanged HEAD and empty index. Final git status inspected. Independent oracle also rejects a deliberately corrupted long-stream signed-zero export. Only the assigned coordinator file may differ from existing baseline; the new scenario/contract/checkpoint/own runner are additions. Curated documents contain no private evidence links, binaries or proprietary corpus. Lead owns living-document consolidation and registration; research/function/protocol metrics remain separate and unchanged.

## Resume

Lead review/integrate hashes and register SceneWorldReplicationScenarios.Run(peer, optionalExportPath). The policy/query/snapshot API is ready for separately dispatched next-round presentation; both freshness checks remain required. No automatic state-driven render adapter, scaling/posture assumptions, terrain readiness, parent transform recovery or live work is included. Worker stops after Round 10 pending explicit Round 11 assignment.
