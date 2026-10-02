# Stable-incarnation creature replication store

R9-RUNTIME-01, 2026-10-02. Modern engine-neutral binding of the accepted [complete CREO:3 baseline](CREATURE_VIEW3_BASELINE_CONTRACT.md) and [six-scalar delta plan](CREATURE_VIEW3_SCALAR_DELTA_CONTRACT.md). This complements the unchanged [exact-revision scalar store](SCENE_CREATURE_SCALAR_STORE_CONTRACT.md); it does not weaken exact handles used for asynchronous resource freshness.

## Registry API and identity

```csharp
public readonly record struct SceneObjectIncarnation(
    Guid RegistryId, ulong SceneGeneration, ulong ObjectId, ulong CreationRevision);
// Added to existing SceneObjectSnapshot, preserving its five-argument constructor:
public SceneObjectIncarnation Incarnation { get; init; }
// On SceneObjectRegistry:
public bool IsCurrent(SceneObjectIncarnation incarnation);
// Existing IsCurrent(SceneObjectHandle handle) remains unchanged.
```

Create assigns CreationRevision from its checked unique registry revision. Replace first allocates any detached-child revisions, then assigns the new object's creation revision from that same monotonic counter. Duplicate create preserves the existing incarnation. Containment, first end-baselines and detached-child handle changes preserve incarnation through record copies; destroy/reset/close retire it. IsCurrent requires open registry, matching registry GUID and scene generation, existing ID and exact incarnation equality. Identity is a local ownership discriminator, not a wire field or security credential.

Exact handles still become stale on those intermediate revisions. The compatible snapshot constructor defaults the new property; such a manually constructed snapshot has no valid incarnation unless a caller explicitly copies one from the registry. Registry-created snapshots always carry it. Checked creation/replacement overflow commits neither a new handle nor incarnation. Scene reset clears objects but never rewinds the registry revision counter.

## Store API for subsequent world binding

```csharp
public SceneCreatureReplicationStore(SceneObjectRegistry registry,
    int capacity = 64, long maximumDecodedBytes = 16 * 1024 * 1024,
    ulong revisionSeed = 0, SceneCreatureMixedReplicationPolicy? mixedPolicy = null,
    SceneCreatureComponentsReplicationPolicy? componentsPolicy = null);
public SceneCreatureReplicationSnapshot? Seed(SceneObjectIncarnation incarnation,
    CreatureView3BaselineSnapshot baseline);
public SceneCreatureReplicationSnapshot? Apply(SceneObjectIncarnation incarnation,
    CreatureView3ScalarDeltaPlan plan);
public SceneCreatureReplicationSnapshot? Find(SceneObjectIncarnation incarnation);
public bool IsCurrent(SceneCreatureReplicationSnapshot snapshot);
public IReadOnlyList<SceneCreatureReplicationSnapshot> Snapshots();
public long RetainedDecodedBytes { get; }
public void Close(); // also IDisposable
```

The sealed immutable result exposes Incarnation, U64 Revision (local state revision), complete Baseline, and current Scalars. A private store discriminator prevents another store from accepting its publication. IsCurrent requires that discriminator, current registry incarnation and latest stored local revision. Snapshots are owned read-only arrays sorted by unsigned object ID. Retained old results remain immutable observations after retirement.

Seed requires a complete accepted codec snapshot with matching ObjectId, a current caller incarnation, and available capacity/decoded-byte budget. It accepts pending or finalized registry objects: no original baseline-arrival/readiness rule is imposed. There are no invented defaults. Baseline and its encapsulated arrays are already immutable and owned by the accepted decoder, so the store may share that immutable snapshot safely; it retains no borrowed input message. Current scalar state begins as a copied baseline scalar record.

Repeated Seed for the same current incarnation deliberately replaces the whole baseline and resets the overlay to its six baseline values. Every successful seed, including a byte-identical replacement, advances the local revision once and invalidates older publications. Replacement replaces the old logical byte charge rather than double charging. No merge of components/wounds, suppression by count, or implicit field carry-forward occurs.

Apply requires the current caller incarnation, matching plan ObjectId and a seeded entry owned by this store. It applies all accepted updates in received order to a new scalar state, checks the next local revision, and commits one immutable result. Fixed baseline fields and original baseline Scalars remain unchanged; snapshot.Scalars is the authoritative current overlay for the supported six fields. Empty plans still advance local revision. Wire deltas contain no extra scene generation: callers must supply the token for every Seed/Apply. Binding survives containment/end-baselines; exact handles remain separately necessary for resource publications.

Stale, missing and foreign tokens return null before baseline/plan validation. A stale old token cannot clear a newer same-ID entry. Wrong object IDs throw InvalidDataException; applying before a complete seed throws InvalidOperationException. Null current inputs throw ArgumentNullException. Capacity/byte-budget violations throw InvalidDataException. Checked counter exhaustion throws OverflowException. These failures leave live state/accounting/counter unchanged; stale housekeeping may still remove unrelated retired entries.

## Bounds, ownership and cleanup

Capacity is 1..4096. Maximum retained logical decoded bytes is 81..134217728; each seed charges accepted baseline.DecodedBytes (81 fixed plus variable content). This is aggregate across entries, including baseline replacements. It excludes CLR overhead, temporary decoder/encoder buffers and retained snapshots held by callers; it is not a total heap cap. Scalar overlays have fixed size and do not change that charge. Per-message/string/container limits remain the caller's accepted codec limits before Seed/Apply.

Every store operation lazily retires entries whose incarnations fail registry freshness, releasing their logical byte charge. Destroy/replacement/recreation/reset/registry close can never resurrect old state. Closing the store clears it and accounting without touching the registry or incrementing counters. Close is idempotent even at maximum revision; Seed/Apply thereafter throw ObjectDisposedException, Find returns null, IsCurrent false and Snapshots empty.

Use one serialized construction thread for registry and store operations. The store rejects other threads and reentrant calls; it adds no locking to direct registry operations. There is no callback surface; reentrancy guard coverage is inspection, not a claimed callback execution test. No mutable arrays escape. Signed zero and finite negative/nonunit height, raw posture/rank bytes and high-bit integer values remain untouched; no scaling, animation, parent transform, terrain readiness, engine presentation, component-set reconciliation or states-bit interpretation is implemented.

## Evidence, confidence and reproduction

The inherited byte layouts and immutable codec behavior are established in the linked baseline/delta contracts. Stable incarnation, seeding/replacement, retirement, bounds and atomic publication are **modern policy**, **Confirmed by native synthetic tests**, not newly recovered original runtime behavior. No original executable, asset or live-server input is required. See the [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-02_R9_INCARNATION_STORE.md).

From this checkout with installed Python/dotnet 8 and existing offline packages:

```powershell
$env:APPDATA=Join-Path $env:TEMP 'stardust-runtime-r9-appdata'
$env:NUGET_PACKAGES='C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
New-Item -ItemType Directory -Force $env:APPDATA | Out-Null
New-Item -ItemType Directory -Force local-private/round9 | Out-Null
python poc/round9-tests/Runtime/Generate-Peer.py local-private/round9/peer.json
dotnet restore poc/round9-tests/Runtime/Runtime.csproj --configfile poc/round9-tests/Runtime/NuGet.Config -p:NuGetAudit=false -p:UseSharedCompilation=false
dotnet build poc/round9-tests/Runtime/Runtime.csproj --no-restore -p:NuGetAudit=false -p:UseSharedCompilation=false
dotnet poc/round9-tests/Runtime/bin/Debug/net8.0/Runtime.dll local-private/round9/peer.json local-private/round9/export.json
python poc/round9-tests/Runtime/Verify-Store.py local-private/round9/peer.json local-private/round9/export.json
```

The producer uses accepted independently authored baseline fixture bytes and new hand-packed lifecycle/delta variants. The oracle compares complete unchanged baseline wire, ordered raw-bit overlays/reseed, handle and incarnation metadata, revisions and cleanup accounting. Native negatives exercise ownership/rejection/rollback directly; exported metadata is not a substitute for those tests. Shared registration is lead-owned: generate peer JSON, parse its dictionary, then call SceneCreatureReplicationStoreScenarios.Run(peer, optionalExportPath). No world integration or later-round dependency is included.


## Round12 optional mixed overlay — 2026-10-02

```csharp
public sealed record SceneCreatureMixedReplicationPolicy(
    CreatureWoundsApplicationPolicy Policy, int MaximumWoundsElements);
public SceneCreatureReplicationSnapshot? Apply(SceneObjectIncarnation incarnation,
    CreatureView3MixedDeltaPlan plan);
// On SceneCreatureReplicationSnapshot (and SceneWorldReplicationSnapshot):
public CreatureView3WoundsState? Wounds { get; }
```

Null `mixedPolicy` preserves the existing seed/scalar API and baseline-only byte accounting; `Wounds` is null and no current wounds array is allocated. An explicit policy accepts only `StrictInterval` or `LegacyBounded` and element bound0..262144. Seed clones current wounds from the complete immutable baseline and validates that element bound. Scalar Apply remains usable and preserves the optional current wounds state.

Mixed Apply requires a complete mixed seed for the current incarnation. It rejects object-ID, policy or **exact** plan/store maximum-element mismatch with InvalidDataException. A mixed plan on a scalar-only store throws InvalidOperationException. Stale/foreign tokens still return null before plan checks. Decode limits remain the caller's responsibility; the world selects the same explicit policy/limits for parsing and application. The store adds no auto-policy interpretation.

The entire ordered mixed plan runs against a temporary state. It checks final candidate wounds plus retained complete baseline against the aggregate byte budget and checked next local revision before replacing one entry. The charge is `baseline.DecodedBytes + 16 + 4L * wounds.Values.Count`; scalar-only stays `baseline.DecodedBytes`. The fixed16 is a modern logical U64 ID/U32 count/U32 counter allocation model, not measured managed memory. There is no retained plan/history. Candidate allocations and consumer-retained immutable observations are excluded. Failed candidates leave the current snapshot, byte charge and revision unchanged; existing stale-entry housekeeping still applies.

Baseline reset replaces the complete seed and both current overlays; baseline fields themselves never mutate. Current wounds are immutable separately owned values. Retirement releases both charges, and a new same-ID incarnation inherits neither overlay. Existing owner/thread/close/overflow rules apply equally to the overload. Confirmed synthetic coverage and independent wire/state comparison are recorded in the [Round12 checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-02_R12_WORLD_MIXED_REPLICATION.md). This is engine-neutral implementation policy using the accepted [mixed plan](CREATURE_VIEW3_MIXED_DELTA_CONTRACT.md), not a new original lifecycle claim.


## Round13 optional standalone components overlay — 2026-10-02

```csharp
public sealed record SceneCreatureComponentsReplicationPolicy(
    int MaximumRawKeys, int MaximumElements);
public SceneCreatureReplicationSnapshot? Apply(SceneObjectIncarnation incarnation,
    CreatureView3ComponentsDeltaPlan plan);
// Added to store and world replication snapshots:
public CreatureView3ComponentsState? Components { get; }
```

Null policy preserves scalar/mixed constructors and accounting; Components is null. Explicit bounds each require0..262144. There is one accepted recovered components kernel, not a new strict/legacy policy switch. Complete baseline decode must already obey its raw-container/string/payload limits. Seed additionally checks MaximumRawKeys (duplicates still count), then normalizes signed ascending unique current keys under MaximumElements and preserves the raw complete Baseline unchanged, including original order, duplicates, raw count and counter. Components-only store configuration is supported independently of optional mixed wounds.

Standalone Apply requires current incarnation, object ID and exact plan/store MaximumElements; absent option or absent seed throws InvalidOperationException, mismatch throws InvalidDataException. Stale/foreign tokens are ignored before plan checks. Caller decode budgets remain its responsibility. The complete accepted [components plan](CREATURE_VIEW3_COMPONENTS_DELTA_CONTRACT.md) computes a temporary normalized state, including its conditional unsigned sum gate, mutation-only increments and final unsigned maximum. Aggregate logical bytes and checked next local revision are verified before one entry replacement; late failure preserves current state/accounting/revision. Scalar and mixed overloads preserve current Components, and component Apply preserves current Scalars/Wounds.

Current components add `16 + 4L * Components.Keys.Count` to the accepted baseline plus any current wounds charge. The fixed16 models U64 object ID/U32 unique count/U32 counter, not managed memory. Duplicate raw baseline entries remain included in Baseline.DecodedBytes. Synthetic baseline119/raw3/unique2 charges143 components-only or171 with current wounds3; after mixed current wounds2 the total is167. Clearing current components retains16, so baseline119/current wounds3/empty components charges163. Transient plans/candidates and caller-retained immutable history are excluded; no internal history is retained. Exact seed budgets, one-byte failures, aggregate replacements and candidate-growth rollback are tested.

All accepted incarnation/lifecycle/owner-thread/close rules apply. Parent replacement retires the parent charge while detached children retain their incarnation and normalized overlay. Same-ID recreation requires a complete seed. No original set callbacks/presentation semantics are reproduced. Modern immutable publication, logical charging and observer policy are Confirmed by synthetic native tests and an independent oracle, not recovered original lifecycle behavior. See the [Round13 checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-02_R13_WORLD_COMPONENTS.md) for69 arithmetic cases and lead composite hookup points.


## Round13 composite application

`Apply(SceneObjectIncarnation, CreatureView3CompositeDeltaPlan)` requires both optional wounds/components policies and seeded current overlays. It rejects object ID, wounds policy and exact wounds/components element-cap mismatch before applying. Missing configuration/seed throws InvalidOperationException; mismatches/budget violations throw InvalidDataException. Stale/foreign tokens return null first. Accepted ordered kernels compute private candidate Scalars/Wounds/Components, then aggregate charge and checked local revision precede one publication. Never call standalone Apply twice to emulate this transaction.

A bounded raw components view is reconstructed from the unchanged complete baseline for the codec; it is temporary and is not retained as a second seed/history. Current normalized overlays and baseline remain separate. Charge is unchanged from the optional standalone formula. Native lead tests prove final-budget and revision-overflow rollback, exact cap/policy and foreign-incarnation rejection. The [world contract](SCENE_WORLD_REPLICATION_CONTRACT.md#round13-components-and-composite-mode---2026-10-02) states derived whole-message limits and one acknowledged observer mutation. Confidence: Confirmed modern composition using the accepted recovered kernels; no original callback or heap-cap claim.
