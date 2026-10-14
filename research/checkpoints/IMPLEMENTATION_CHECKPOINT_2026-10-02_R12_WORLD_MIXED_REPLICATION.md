# R12-RUNTIME-01: atomic world mixed replication

Date: 2026-10-02. Bounded question: can the native world opt into accepted ordered CREO:3 scalar+wounds plans while preserving the immutable complete seed, state/resource freshness and one postcommit observer mutation, with no visible prefix on failure? Stopping condition: native positive/negative/lifecycle/budget tests, independent496-byte peer/model comparison, prior runtime regressions and exact baseline-relative scope review pass. Round12 only; no later-round work.

## Inputs and tools

Accepted Round12 public snapshot: 2077 files, manifest SHA-256 `4bb617a17cde0ae3cd13ebddb06afa2141ed29d35938028689113440b9cf2766`, ZIP SHA-256 `21b81fe530b5e5d6e056014fe6d56c85e2be73a5e195fbc67f021f1c3ceae335`. Exact member set, unique safe paths, sizes and hashes were checked before overlay, then every output hash afterward. Ignored/unlisted private files and unrelated dirty baseline files were preserved. HEAD `65de8c88141071744dc75594be126b3bab3696a5` unchanged. No commit, stage, push or publication.

Orientation: START_HERE and accepted Round11 session, current status/ledger/unknown-function terminology, prior runtime checkpoints, curated CREO3 packet row and accepted codecs/fixtures. Inputs are independently authored synthetic application/lifecycle bytes and a tiny managed resource closure. No executable/VA claim, original assets, decompiler output, network/socket/DNS or external execution was needed. Tools: installed Python, .NET8, existing offline NuGet cache and sources-cleared task NuGet.Config. No tool download.

## Observation, interpretation and consequences

**Observation:** [store source](../../../SWG-Godot-Client/poc/Stardust.Core/Session/SceneCreatureReplicationStore.cs) exposes optional `SceneCreatureMixedReplicationPolicy` and a mixed-plan Apply overload. Seed creates separate owned current wounds from the full baseline. Mixed Apply validates current incarnation, object ID, exact policy and exact element limit; computes the complete candidate, aggregate charge and checked revision before replacing one entry. **Interpretation:** a single immutable commit protects both scalar and wounds observations. **Confidence:** Confirmed by native synthetic rollback, identity/policy/element/capacity/overflow tests. **Compatibility consequence:** implementation consumers must distinguish `Baseline` from current `Scalars`/`Wounds`, and use store/world freshness guards before use.

**Observation:** [world source](../../../SWG-Godot-Client/poc/Stardust.Core/Session/SceneWorldCoordinator.cs) adds named `CreatureView3Mixed`, final optional MixedLimits/WoundsPolicy record fields and nullable snapshot Wounds. Accepted mixed decoding runs before staging, and again on owned replay bytes. The same configured policy and element limit are used at private store application. **Interpretation:** policy is selected out of band, not guessed from bytes. **Confidence:** Confirmed positive LegacyBounded/StrictInterval and strict wrong-grammar rejection. **Consequence:** default StrictInterval and explicit LegacyBounded remain distinct parsing/application contracts; there is no automatic fallback. Wire structures and instruction evidence remain owned by the [mixed contract](../../docs/CREATURE_VIEW3_MIXED_DELTA_CONTRACT.md) and [legacy policy contract](../../docs/CREATURE_WOUNDS_LEGACY_POLICY_CONTRACT.md); this worker adds no original executable semantic claim.

**Observation:** baseline logical119/current3 seed147; assign/remove current2 final143; legacy replace index6 zero-fills to current7 charge163; clear charge135. Scalar-only seed remains119 and Wounds=null. **Interpretation:** `baseline.DecodedBytes + 16 + 4L * currentWounds.Count` is a deterministic retained logical-byte bound. **Confidence:** Confirmed native seed/replacement/growth/clear/aggregate tests. **Consequence:** it includes unchanged seed wounds plus separately retained current wounds, excludes CLR overhead, temporary candidates and caller-held snapshots, and must not be described as total heap usage.

**Observation:** interleaved all-six scalars plus assignment/remove, growth and invalid-remove/equal-replace counter no-ops yield store revisions2/3/4 and observer mutations5/6/7; counter values6/7/7. A complete accepted no-op plan still produces one application revision. Late malformed tail or semantic growth failure produces no candidate/prefix publication; world failure clears registry/replication/queues/pending changes and invalidates held batches. **Confidence:** Confirmed native assertions plus independent Python ordered state and single-mutation comparison. **Consequence:** drain current Scalars/Wounds together after a successful application; no per-field events are exposed.

**Observation:** state-only mixed changes leave the same current resource publication, including a 1000-pair seed/mixed stream with one current entry, one pending ID and one load. Containment/end preserve state incarnation; destroy, same-ID recreation, reset, failure and close retire it. Pending loads cancel on failure; later faults are observed by polling and outstanding jobs reach zero. Existing DrainCompletion is the close-drain contract. **Confidence:** Confirmed native synthetic lifecycle/resource/thread/reentrancy/cleanup checks and prior runtime regressions. **Consequence:** resources and current state require separate freshness checks. No rendering, terrain readiness or component-set integration is implemented.

## Validation and reproducibility

The new independently authored [scenario](../../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/SceneWorldMixedReplicationScenarios.cs) is linked by the isolated [runner](../../../SWG-Godot-Client/poc/round12-tests/Runtime/Runtime.csproj); shared Program/project registration is lead-owned. Synthetic peer negotiates496-byte datagrams, checks every frame within bound, buffers create-before-start, suppresses duplicate start and compares exact select/ready. [Python oracle](../../../SWG-Godot-Client/poc/round12-tests/Runtime/Verify-Mixed.py) independently unframes/rechecks CRC/selector4/sequence, checks unchanged synthetic baseline bytes and runs accepted strict/legacy Python state models against native exports. No native encoder output is used as the expected peer fixture.

```powershell
$env:PYTHONUTF8='1'
$env:APPDATA=Join-Path $env:TEMP 'stardust-runtime-r12-appdata'
$env:NUGET_PACKAGES='C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
New-Item -ItemType Directory -Force $env:APPDATA,local-private/round12 | Out-Null
python poc/round12-tests/Runtime/Generate-Peer.py local-private/round12/peer.json
dotnet restore poc/round12-tests/Runtime/Runtime.csproj --configfile poc/round12-tests/Runtime/NuGet.Config -p:NuGetAudit=false
dotnet build poc/round12-tests/Runtime/Runtime.csproj --no-restore -p:UseAppHost=false -p:UseSharedCompilation=false -p:NuGetAudit=false
dotnet poc/round12-tests/Runtime/bin/Debug/net8.0/Runtime.dll local-private/round12/peer.json local-private/round12/export.json
python poc/round12-tests/Runtime/Verify-Mixed.py local-private/round12/peer.json local-private/round12/export.json
python tools/python/validate_organized_workspace.py .
git diff --check
```

Executed: core/new runner builds zero warnings/errors; mixed native scenario and independent oracle; rebuilt Round11 acknowledged-change, Round10 typed world, Round9 incarnation store and Round7 opaque world runners; unchanged prior scenarios pass. Shared smoke build and `--creature-replication` full baseline/exact-scalar checks pass. Prior independent change/world/store oracles also pass. Documentation links, private-link prohibition, exact scope/encoding/whitespace and baseline-relative focused diffs are checked before handoff. No private input or required toolchain was missing.

## Scope and next probe

Only two store/world sources, one new scenario, three named durable contracts, this checkpoint and five own runner files change from the accepted snapshot. Living status/ledger/registries and shared registrations are lead-owned and untouched. Regenerable synthetic peer/export/scope evidence stays ignored. No binary/assets/proprietary content is added to public paths.

Remaining uncertainty: this modern offline integration is not evidence of original scene readiness, presentation, component-set behavior or live-server interoperability. The accepted codec intentionally supports only CREO3 indices11..17. Logical-byte budgets do not cap candidate allocations or caller-retained history. Wire object IDs have no incarnation discriminator; ordered active-generation delivery remains a prerequisite.

Exact next action: lead reviews the baseline-relative hashes/diff and registers `SceneWorldMixedReplicationScenarios.Run(peer, exportPath)` in the shared harness if desired. A subsequent explicitly authorized consumer must select the named mixed policy and validate resource plus replication freshness independently. Worker stops idle after this milestone; no Round13 or engine integration.
