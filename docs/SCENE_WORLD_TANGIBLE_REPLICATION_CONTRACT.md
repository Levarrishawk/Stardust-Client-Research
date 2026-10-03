# Optional typed TANO3 world replication

Round27 Runtime, 2026-10-03. The [world coordinator](../poc/Stardust.Core/Session/SceneWorldCoordinator.cs) now optionally binds the accepted [complete TANO3 codecs](TANGIBLE_VIEW3_CODEC_CONTRACT.md) to the [tangible store](SCENE_TANGIBLE_REPLICATION_CONTRACT.md). This is engine-neutral offline integration; display, packed customization interpretation, live networking and Godot factory registration remain separate.

## Public API

Both existing world constructors append `SceneWorldTangibleReplicationPolicy? tangibleReplicationPolicy = null` after `changePolicy`, preserving source compatibility. The record accepts `CreatureView3BaselineLimits BaselineLimits`, `TangibleView3DeltaLimits DeltaLimits`, `SceneTangibleReplicationLimits StoreLimits`, `ulong RevisionSeed = 0`, and optional pure `Func<ReplicationEnvelope,TangibleView3BaselineSnapshot> BaselineDecoder` / `Func<ReplicationEnvelope,TangibleView3DeltaPlan> DeltaDecoder`. Omitted decoders use exact accepted codecs. Limits are copied and validated; delta normalized element metadata must equal store limits.

`TangibleReplicationEnabled`, `RetainedTangibleReplicationDecodedBytes`, `FindTangibleReplication(long generation, ulong id)`, `ApplyTangibleDelta(long generation, TangibleView3DeltaPlan plan)` and `IsCurrent(SceneWorldTangibleReplicationSnapshot)` expose typed state. `SceneWorldObjectSnapshot.TangibleReplication` propagates through ordinary snapshots and change batches. The immutable token exposes Generation, Incarnation, Revision, Baseline, Names, TangibleScalars, Customization and Components. Current tokens require this owner, generation, active phase and exact current store state. Pending complete state may be inspected; finalized object/resource publication is still gated by the end barrier.

The [router](../poc/Stardust.Core/Session/SceneWorldTangibleApplicationRouter.cs) constructor is:

```csharp
SceneWorldTangibleApplicationRouter(SceneWorldCoordinator world,
    Func<ReplicationEnvelope,TangibleView3DeltaPlan>? decoder = null,
    Func<long,ReadOnlyMemory<byte>,bool>? downstream = null,
    int maximumPayloadBytes = 65536)
```

It borrows an open typed world. Default decoder uses guarded world policy, and default downstream calls world.Accept. Put it above the existing CREO ingress chain; downstream retains that chain. Disposal releases callback references and never closes the borrowed world.

## Family, barriers and atomicity

Typed static TANO3 baselines consume all eleven fields regardless of raw declared count. Exact default grammar and limits are checked before any injected decoder, including when the callback would otherwise reinterpret an unknown field. Callback results must match object ID and declared count. Static recipe/family checks remain explicit; wrong-family packets reject. TANO other views and opaque default preserve preceding bounded envelope handling. Other-view/opaque records cannot satisfy the required complete TANO3 static end barrier. Typed TANO3 baseline/delta updates do not append opaque history.

A genuine baseline seeds the registry-current incarnation; finalized deltas require static recipe, exact current generation, and a complete seed. Recreated objects cannot receive former incarnation state. Each successful seed/delta increments the tangible store revision and world mutation revision once, including duplicate/noop plans. Genuine reseeding resets current overlays. Historic baseline and prior immutable issued snapshots remain stable.

Internal `PrepareSeed` / `PrepareApply` tokens hold the store owner gate. The coordinator prebuilds its journal dictionary, checks journal capacity and checked mutation revision, and prepares the entire ordered store candidate. Store preparation checks its own revision, field/update/command caps, all actors' retained charge and every intermediate name/raw/component growth before a later shrink. Only then does callback-free commit assign the prepared store map/charge/revision and prebuilt world journal/revision. Release recovers the gate on rejection. There is no parsing, external callback, allocation or checked arithmetic after the first commit assignment.

Rejected direct `ApplyTangibleDelta` and active exact TANO3 `Accept` preserve typed state, logical charge, world phase and pending journal. Staged replay/barrier failures remain generation-fatal and drain the world. Destroy, reset, close and current resource failure retire typed state; late stale jobs cannot resurrect it. Public standalone store gates and failure semantics remain unchanged.

## Ownership, callback purity and accounting

World/store/router operations require the creating managed thread. World-owned decoder callbacks run under the world busy gate; caught nested world access still poisons that decode. Router callbacks run against a whole-world stamp: generation/phase, checked mutation-attempt epoch, exact registry objects, both families' state revisions, and pending journal membership/kinds. The epoch detects resource restart/poll and acknowledgement attempts that need not advance the journal revision. Router reentry/disposal attempts are poisoned even if caught. Invalid callback results close the router; they do not close the borrowed world.

Callback effects outside preparation cannot roll back. If a callback itself mutates the world, acknowledges a journal, restarts a resource or changes generation, that effect remains and the returned plan rejects. Pure read-only router callbacks may inspect world state. Policy decoder callbacks may not reenter world methods, including owner-gated reads. External delegate captures and downstream effects remain caller-owned.

CREO and TANO decoded budgets are separate. `RetainedReplicationDecodedBytes` remains CREO-only; the tangible property is TANO-only. TANO charge remains historical baseline +16+4 times current normalized component count + current names' additional alias-aware bytes + customization content only when not baseline storage. Equal bytes in separately owned storage are charged. These are logical compatibility budgets, not CLR heap/temporary-copy measurements. No typed customization presentation journal was added; consumers receive current raw state only.

## Evidence and limits

Confirmed by [native scenarios](../poc/Stardust.Core.SmokeTests/SceneWorldTangibleReplicationScenarios.cs): 132 checks /37 cases, exact independent TANO wires in both orders, mixed families, aliases/equal storage, duplicates/reseed, intermediate/global growth rollback, journal/revision overflow, static barriers, stale generations/incarnations, decoder mutation/acknowledgement/restart/reentry, owner/close/resource failure and held stale jobs. Only two deliberate synthetic failing loader calls occur; no assets load.

The [independent oracle](../poc/round27-tests/Runtime/verify.py) reads [synthetic literal wires](../poc/round27-tests/Runtime/ordered-fixtures.json), recomputes byte grammar, ordered state, unsigned component arithmetic, and logical charges, and checks 284 malformed advertised envelopes. It also runs accepted prior receipt oracles. This proves deterministic fixture behavior, not original executable execution, a second scheduler implementation, or live interoperability. See the [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R27_TANGIBLE_WORLD.md).

Next: lead registers the router over existing CREO ingress and requires a current typed token in the optional static visual factory, then validates exact-wire static caption/visibility publication and cleanup. Keep localized-name/display policy and packed customization interpretation explicit.

Lead actual typed factory/router/pumped host join now passes five exact TANO stages alongside the named CREO path, including original static readback/pose/resource retention, destroy/recreate complete baseline and shutdown. [Integration record](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R27_TANGIBLE_WORLD_INTEGRATION.md).
