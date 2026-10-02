# Opt-in typed world replication

R10-RUNTIME-01, 2026-10-02. [SceneWorldCoordinator](../poc/Stardust.Core/Session/SceneWorldCoordinator.cs) optionally binds the accepted [full CREO:3 baseline](CREATURE_VIEW3_BASELINE_CONTRACT.md) and [six-scalar plan](CREATURE_VIEW3_SCALAR_DELTA_CONTRACT.md) through the [incarnation store](SCENE_CREATURE_REPLICATION_STORE_CONTRACT.md). The original [opaque world contract](SCENE_WORLD_COORDINATOR_CONTRACT.md) remains the default. This is a modern offline policy, not recovered original readiness or presentation behavior.

## Configuration and API

```csharp
public enum SceneWorldReplicationMode { Opaque, CreatureView3Scalars, CreatureView3Mixed, CreatureView3MixedComponents }
public sealed record SceneWorldReplicationPolicy(SceneWorldReplicationMode Mode,
    CreatureView3BaselineLimits BaselineLimits, int MaximumScalarUpdates,
    long MaximumDecodedBytes, CreatureView3MixedDeltaLimits? MixedLimits = null,
    CreatureWoundsApplicationPolicy WoundsPolicy = CreatureWoundsApplicationPolicy.StrictInterval,
    CreatureView3ComponentsDeltaLimits? ComponentsLimits = null,
    SceneCreatureComponentsReplicationPolicy? ComponentsPolicy = null);
public SceneWorldCoordinator(ObjectTemplateCatalog catalog,
    LogicalAnimationClipSelection idle,
    IReadOnlyDictionary<string, LogicalAnimationClipSelection> movement,
    Func<SceneCreatureResourceRequest, CancellationToken, Task<SceneCreatureResources>> loader,
    SceneCreateZeroQuaternionPolicy zeroPolicy, int maximumObjects = 64,
    int maximumOutstandingJobs = 8, int maximumStagedMessages = 256,
    int maximumStagedBytes = 1048576, int maximumEnvelopes = 256,
    int maximumEnvelopeBytes = 1048576, int maximumPayloadBytes = 65536,
    SceneWorldReplicationPolicy? replicationPolicy = null,
    SceneWorldChangePolicy? changePolicy = null);
public SceneWorldReplicationMode ReplicationMode { get; }
public long RetainedReplicationDecodedBytes { get; }
public SceneWorldReplicationSnapshot? FindReplication(long generation, ulong objectId);
public bool IsCurrent(SceneWorldReplicationSnapshot snapshot);
// Existing IsCurrent(ScenePreparedCreaturePublication publication) remains intact.
// Constructor-compatible addition to SceneWorldObjectSnapshot:
public SceneWorldReplicationSnapshot? Replication { get; init; }
```

Null policy preserves every previous constructor call and opaque fixture. An explicit policy with Mode=CreatureView3Scalars enables the typed store. Mode=Opaque retains opaque behavior. Policy records and baseline limits are snapshotted; no mutable configuration arrays are introduced. A representative synthetic policy uses baseline limits (1024,64,64,16,512), MaximumScalarUpdates=16 and MaximumDecodedBytes=240.

Policy limits: baseline payload 2..existing maximumPayloadBytes; string bytes 0..1048576; Unicode units 0..524288; collection elements 0..262144; per-baseline decoded bytes 0..134217728; scalar updates 0..4096; aggregate retained decoded bytes 81..134217728. Invalid modes/bounds throw ArgumentOutOfRangeException, null limits throw ArgumentNullException. Existing object/staging/job/opaque envelope constructor bounds still apply. Aggregate accounting is accepted baseline logical bytes, excluding CLR overhead, temporary decoder buffers and snapshots retained by consumers; it is not a heap cap.

The sealed world replication snapshot exposes Generation, Incarnation, local Revision, complete Baseline and current Scalars. Its private world discriminator and internal store result prevent foreign publications from passing freshness. No registry or store instance is exposed. FindReplication returns null for stale generation, missing object, no baseline seed, opaque mode or closed world. Snapshot.Replication provides the same immutable state observation with the existing object/placement record. Its three-argument constructor remains compatible.

## Ordered decoding, replay and failure

Typed mode fully decodes a complete baseline or all scalar updates before staging owned APPLICATION bytes. On the explicit caller barrier, replay decodes those owned bytes again and applies in arrival order. Create must precede baseline; a complete baseline must precede scalar delta. End-baselines and containment can follow without erasing incarnation state. The caller barrier remains a modern readiness choice and resources only start on subsequent PollResources.

Scalar typed mode strictly accepts full CREO:3 baselines and scalar indices 11..16 deltas. Wrong tag/view, unsupported index (including wounds index 17), malformed tail/value, nonfinite floats or limits failure reject before staging/application. Missing object, delta-before-baseline and aggregate budget failure during replay fail the generation, clear registry/replication/queues, cancel current resource jobs and return no ready bytes. A later failure clears already-published state but cannot recall a ready datagram already sent. The caller owns transport/scene recovery. Stale generations return false before parsing, as before.

No typed envelope history is retained. Each supported baseline replaces the current bounded seed and resets its scalar overlay, including byte-identical baselines with differing declared count. Each delta applies the complete ordered plan atomically to the six current scalars. Fixed baseline fields and baseline.Scalars remain original seed observations; snapshot.Scalars is the authoritative current overlay. The declared delta count is validated/tolerated by the accepted codec but no plan/history is stored. Initial staged traffic still obeys staging count/byte limits; repeated supported traffic after the barrier cannot exhaust the opaque append-only history limits.

Unknown application opcodes still fail. Other replication families/views and unsupported CREO fields strictly fail typed modes; there is no opaque fallback. Default mode continues outer-envelope-only bookkeeping for every accepted family/view and retains its existing history budgets. In scalar mode, wounds/components remain baseline observations only. Mixed mode adds current wounds through the accepted plan; components remain baseline observations.

## Two freshness boundaries

Use world.IsCurrent(replicationSnapshot) immediately before consuming current state. It checks world identity, external generation, active scene, stable registry incarnation and latest local store revision. Scalar/baseline changes invalidate older state snapshots but preserve a current resource publication: they do not change exact registry handle, restart attempts, queue resource loads, or invoke the loader.

Use world.IsCurrent(resourcePublication) independently before using resource data. Exact registry revision and initializer attempt remain mandatory. Containment/end-baselines retain scalar state through incarnation while changing resource handles when those operations issue a new revision. Duplicate lifecycle notifications preserve both when the registry handle is unchanged. Destroy/recreation/selection/failure/close retire state; recreated IDs require a new complete baseline, with no defaults. The private typed store survives scene resets solely to preserve monotonic local revisions and is cleared by registry retirement.

Wire payloads identify object ID without an incarnation field. The world derives the current token from its serialized registry and expects ordered delivery for the active generation. It cannot distinguish arbitrarily injected old same-generation payload bytes from a legitimate update to a newly seeded same-ID incarnation. The exposed freshness guard protects retained local observations; it does not invent a wire identity discriminator.

Source placement remains separate. Height is raw finite F32, including negative/nonunit values and signed zero; no scale, posture animation, parent transform, shader or presentation behavior is implemented. Owner-thread/reentrancy rules apply to new queries and snapshots. Close clears typed state immediately while cancelled-but-unfinished resource work remains budgeted until existing DrainCompletion observes completion/faults.

## Evidence and reproduction

Confirmed native behavior: real lifecycle/application bytes through existing offline SOE, complete owned staging, ordered typed replay, exact ready/retry, full baseline and scalar bits, state/resource freshness separation, atomic late replay failure, one load before lifecycle change, 1000 baseline/delta pairs retaining 119 logical bytes and zero history, old-generation faults/backpressure, query reentrancy/foreign thread, close/drain and default opaque regression. These establish modern implementation behavior, not additional protocol research coverage.

The independently hand-packed peer explicitly negotiates 496-byte datagrams in request/response and checks every frame is within that bound. Complete baseline frames exceed the old peer's 128-byte bound; the old peer remains unchanged. No fragmentation claim, socket or server contact. Original assets and executable are not inputs; the resource closure is independently authored synthetic managed data.

```powershell
$env:APPDATA=Join-Path $env:TEMP 'stardust-runtime-r10-appdata'
$env:NUGET_PACKAGES='C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
New-Item -ItemType Directory -Force $env:APPDATA,local-private/round10 | Out-Null
python poc/round10-tests/Runtime/Generate-Peer.py local-private/round10/peer.json
dotnet restore poc/round10-tests/Runtime/Runtime.csproj --configfile poc/round10-tests/Runtime/NuGet.Config -p:NuGetAudit=false -p:UseSharedCompilation=false
dotnet build poc/round10-tests/Runtime/Runtime.csproj --no-restore -p:NuGetAudit=false -p:UseSharedCompilation=false
dotnet poc/round10-tests/Runtime/bin/Debug/net8.0/Runtime.dll local-private/round10/peer.json local-private/round10/export.json
python poc/round10-tests/Runtime/Verify-World.py local-private/round10/peer.json local-private/round10/export.json
```

The independent oracle verifies 496-byte handshake/framing/reliable sequence, delivered application order, baseline wire, ordered raw-bit state/reset and stream/accounting outputs. Native tests directly verify rejection, cancellation, freshness and rollback; exported metadata is not a substitute. Lead-owned registration: generate/parse peer JSON and call SceneWorldReplicationScenarios.Run(peer, optionalExportPath). See the [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-02_R10_WORLD_REPLICATION.md) for scope and regression gates. Subsequent render binding must check both freshness boundaries and define presentation policy separately.

## Round11 integration —2026-10-02

Round11 adds [bounded acknowledged changes](SCENE_WORLD_CHANGE_DELIVERY_CONTRACT.md) and [world-owned original presentation](OFFLINE_REPLICATED_WORLD_PREVIEW_CONTRACT.md). Resource and replication freshness remain independent; world generation is not the registry epoch. Mixed plans were separate at Round11; the Round12 opt-in mode below now binds them.


## Round12 mixed mode — 2026-10-02

`CreatureView3Mixed` binds the accepted [mixed codec/state/plan](CREATURE_VIEW3_MIXED_DELTA_CONTRACT.md) and [instruction-backed wounds policy](CREATURE_WOUNDS_LEGACY_POLICY_CONTRACT.md), without changing opaque or scalar-only behavior. Explicit `MixedLimits` is required. Its six constructor fields are `MaximumPayloadBytes`, `MaximumFields`, `MaximumScalarUpdates`, `MaximumWoundsCommands`, `MaximumWoundsValues`, `MaximumWoundsElements`. Payload must fit the existing world payload bound; fields/scalars/raw commands are 0..4096; values/elements are 0..262144. The scalar-only `MaximumScalarUpdates` remains unchanged; mixed parsing uses `MixedLimits.MaximumScalarUpdates`. World policy records and both limit records are copied at construction.

Choose `WoundsPolicy` before parsing: `StrictInterval` remains the default, while `LegacyBounded` is an explicit alternative with instruction-backed assign-loop cursor, mutation-dependent counter advancement and bounded zero-fill growth. There is no policy bit or autodetection in wire bytes. World decoding and private store application use the same policy and element limit. Other supported budgets remain codec-owned and enforced on the entire message before staging. Wrong policy grammar, unsupported index, truncation, invalid height or over-budget payload fails the whole generation; no opaque fallback is added.

Both store and world replication snapshots now expose `CreatureView3WoundsState? Wounds { get; }`: null for scalar-only state, owned immutable current wounds for mixed state. Complete `Baseline` remains unchanged, including its original scalars, wounds and components. A mixed baseline seed initializes both current overlays. Mixed deltas apply all interleaved/repeated indices 11..17 in order to a temporary candidate, then check aggregate logical bytes and checked local revision before one store commit. No baseline replacement or partial field publication occurs during delta application.

The aggregate mixed charge is `Baseline.DecodedBytes + 16 + 4 * currentWounds.Count` per entry: U64 object ID, U32 count, U32 counter and S32 values for current wounds. It is a deterministic modern logical-byte policy, excluding CLR overhead, temporary candidates and snapshots retained by consumers. Synthetic baseline119/current3 charges147; assign/remove current2 charges143; zero-fill current7 charges163; clear charges135. Scalar-only remains119 with no second wounds array. Retained original baseline arrays remain charged even after current clear. This bounds retained current state rather than total process memory.

Each complete mixed application marks Replication and increments the [world mutation revision](SCENE_WORLD_CHANGE_DELIVERY_CONTRACT.md) once, including accepted wound-counter no-ops. Capacity/revision preflight precedes application. Only a successful full replay becomes drainable. Late decode, semantics, element/aggregate capacity or revision failure clears registry/state/queues/pending changes, cancels current resource jobs and leaves no partial batch or ready result. Pending jobs remain budgeted until polled or closed/drained; `DrainCompletion` is the existing close drain, not a new failure-wait signal.

Current-state and resource freshness remain independent. Mixed state-only updates neither change exact resource handles nor queue another load. Containment/end-baselines preserve mixed incarnation; destruction/recreation/reset/selection/failure/close retire it. A recreated object must receive a complete new baseline. Owner-thread and reentrancy guards are unchanged. No terrain readiness, networking component-set integration, GPU/render consumer or live-server contact is added.

Confirmed native synthetic tests and an independent Python oracle cover 496-byte peer framing/order, all six scalar fields interleaved with wounds, assignment cursor, bounded zero-fill, counter no-ops, strict alternative, full-message atomic rollback, byte/element/policy bounds, overflow/capacity, lifecycle, resource separation and cancellation/late-fault cleanup. A 1000-pair baseline/mixed stream retains one current entry and one pending ID with no resource reload. This establishes modern implementation behavior; it adds no executable-function or original-protocol research coverage. Reproduction and exact scope are in the [Round12 checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-02_R12_WORLD_MIXED_REPLICATION.md).

## Lead Round12 original-resource and presentation acceptance

The [combined report](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_12_2026-10-02.md) accepts the native mixed world with actual39-source owned original palette resources, acknowledged latest changes, state/resource freshness and atomic late-failure cleanup. The [inspector](OFFLINE_REPLICATED_WORLD_PREVIEW_CONTRACT.md) now explicitly opts into this mixed mode after all worker APIs are accepted. Baseline/current-wounds logical charges109/121 in the independent integration fixture differ from the119/147 scalar/mixed seed fixture because their baseline strings/vectors differ; neither is a universal memory cost. Components/other fields/views and original callback effects remain separate.


## Round13 components and composite mode - 2026-10-02

`CreatureView3MixedComponents` (value3) requires existing MixedLimits/WoundsPolicy plus explicit ComponentsLimits and ComponentsPolicy. Existing modes retain their prior null Components/accounting and routing. ComponentsLimits fields are `(MaximumPayloadBytes, MaximumFields, MaximumCommands, MaximumKeys, MaximumElements)`; payload2..world bound, fields/commands0..4096, keys/elements0..262144. ComponentsPolicy.MaximumRawKeys is0..262144; its MaximumElements exactly equals ComponentsLimits.MaximumElements. All configuration records are copied.

Every delta in this new mode uses the [ordered composite codec](CREATURE_VIEW3_COMPOSITE_DELTA_CONTRACT.md): field5 and11..17 may repeat/interleave in either order. No first-field selection or pair of independent commits occurs. The immutable composite limits derive total payload and total field-occurrence caps as the **minimum** of the configured components and mixed values; callers must explicitly budget both for the complete message. Remaining independent scalar/wounds and components command/key/value/element caps retain their configured values. Four native world tests prove either group's smaller payload/field cap rejects before staged state or resource work. Raw declared field count does not drive traversal. Unsupported fields reject the whole message.

Full decode occurs before staging and during ordered barrier replay. Store application checks current incarnation, both enabled configurations, exact wounds policy and both element bounds. All three overlays are computed privately; aggregate retained charge and checked state revision pass before one immutable entry is replaced. One accepted message produces one local state revision and one Replication observer mutation, including empty/no-op messages. Scalar and standalone component store APIs remain available; each preserves other enabled overlays.

Raw complete baseline components retain order/duplicates/count/counter and exact re-encoding. Current Components is separately owned signed-sorted unique state. The logical current component charge16+4*uniqueCount is added to complete baseline and optional current wounds charge; see [store contract](SCENE_CREATURE_REPLICATION_STORE_CONTRACT.md). Resource freshness remains independent and state-only updates never reload resources. Incarnation/lifecycle/owner/acknowledgment rules remain as accepted.

Late parse/counter/element/aggregate/revision/observer failure retires the whole world generation, clears current state/pending changes, invalidates prior publications/batches and cancels outstanding work. Failed replay returns no ready bytes or partial observation. Cancellation does not free unfinished jobs early; existing close/drain observes late faults. No original callbacks or component gameplay/presentation effects are invented.

Worker standalone/sequential acceptance and69 recovered set cases pass. Lead integration removes its temporary cross-field rejection and verifies both field orders, one observation, exact normalization/accounting, four total-bound failures, aggregate/revision/policy/token rollback and an independently framed original39-source palette route. The integration fixture baseline93 bytes charges145/145/157 for three complete states; baseline raw keys remain `[-1,int.MinValue,-1]`, current final keys `[int.MinValue,int.MaxValue]` at counter5, and wounds counter14. Late wounds growth retires the complete generation. Old Mode2 native/model and shared regressions pass. [Round13 report](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_13_2026-10-02.md) gives reproduction and remaining scope.
