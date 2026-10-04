# Scene CREO6 composite store contract

This engine-neutral prototype publishes full CREO6 state and an ordered event journal over a borrowed `SceneObjectRegistry`. It uses the accepted [composite kernel](CREATURE_VIEW6_COMPOSITE_CONTRACT.md) unchanged. It adds no dispatch, networking, world delivery, original executable behavior claims, or engine adapter.

## Publication and identity

`SceneCreatureView6CompositeReplicationStore` exposes `Seed`, `Apply`, `Find`, `IsCurrent`, `Revision`, `RetainedStateBytes`, `PendingEventCount`, `DrainEvents`, `Acknowledge`, and `Close`/`Dispose`. Every operation requires the constructing managed thread; reentry is rejected. The host must serialize registry mutation with store use.

Seed requires an exact current registry handle and matching object identifier. Apply additionally requires the exact snapshot reference issued by this store for the same current incarnation. Stale handles or foreign/superseded snapshots return null before consuming a plan. A valid plan publishes one global store revision and one outer composite state revision, including a valid HAM no-op plan. Arithmetic overflow rejects publication. Same-incarnation reseeding advances the prior state revision; recreation starts state revision one while the global store revision continues.

A handle change within an incarnation preserves stored state. `Find` through the new current handle can return the historical snapshot; that snapshot's old handle is not current, but applying through the new current handle with that exact stored snapshot is permitted. Registry destruction/recreation never authorizes the old incarnation.

All candidate model, map, queue, totals, and next revision/epoch are prepared before publication. Internal `PrepareSeed`/`PrepareApply`, `CheckPrepared`, `CommitPrepared`, and `ReleasePrepared` provide a bounded integration seam. An active candidate holds the owner gate. Check requires exact owner, active candidate identity, unconsumed token, expected revision/epoch, current handle, and incarnation. Commit performs prebuilt assignments; its trusted caller must check first and allow no callbacks or registry mutation between check and commit. Release without commit cancels publication. Released or consumed candidates cannot pass a later check.

Failure preserves published state, revision, bytes, pending journal, and issued batch. Failed writes do not opportunistically prune other stale actors. Read-side `Find`, `IsCurrent`, `RetainedStateBytes`, and `DrainEvents` retire stale incarnations, preserving same-incarnation handle changes. Retirement advances the batch epoch without advancing the store revision or deleting historical events.

## Model and journal

The snapshot owns an immutable baseline-shaped composite state: all eighteen baseline fields, raw strings, scalar bit representations, pairs, signed HAM/max-HAM elements, and counters. Scalar assignments at indices 0, 2-12, and 16-17 emit assignment events even for equal values. HAM updates at 13/14 emit actual frozen-kernel mutations; skips/no-ops emit none. Defender index 1, wearable index 15, and unknown delta indices remain unsupported. Untouched immutable defender/wearable arrays are shared safely. Baseline declared count remains baseline metadata; delta declared count belongs to the plan/kernel change, not a separate snapshot field.

Event records retain handle, incarnation, store revision, and composite event metadata (kind, field, update ordinal, optional opcode/index/old/new value). They do not retain historical models. Drain returns the same immutable issued batch until publication, retirement, or acknowledgement invalidates it. Acknowledge requires exact owner and batch reference plus revision/epoch; it clears the whole journal once. Stale, repeated, foreign, or closed acknowledgements return false.

Historical events survive object recreation. Journal acknowledgement is not permission to deliver events to a live object: the host must check each record's incarnation against the serialized registry immediately before delivery. The store invokes no callbacks. Close clears owned state and journal, preserves the last store revision, and leaves the borrowed registry open.

## Resource policy

Store limits bound actor count, total retained logical state bytes, pending events, events per apply, global logical output bytes, and work. Nested model/message policies are validated and copied. Logical model sizes follow the frozen kernel (empty full model: 108 bytes). Each composite event costs 24 logical bytes; these are accounting units, not CLR heap allocation measurements.

For a replacement candidate, subtract every other actor's retained model bytes from state capacity. Subtract those bytes plus 24 times all already pending events from output capacity. The kernel checks the candidate against the resulting limits, including every intermediate model and its conservative prospective event checks. A later shrinking update cannot rescue an earlier over-budget intermediate. Per-apply and remaining queue capacities both narrow the kernel event limit.

Message work and kernel work are separate limits, each narrowed by the store work ceiling; they are not summed into a lifetime meter. Message work accounts for decoded fields, commands, values, and string bytes. Kernel work includes its frozen seed/copy and update policy. Initial validation and bounded scans/event conversion/map/journal copying retain the accepted kernel's hard ceilings; the work counter is not a complete instruction or allocation count.

## Evidence and verification

Confirmed implementation behavior is exercised by [the store scenarios](../poc/Stardust.Core.SmokeTests/SceneCreatureView6CompositeStoreScenarios.cs) and [the own runner](../poc/round35-tests/Runtime/Program.cs). All fixtures are synthetic. The [independent oracle](../poc/round35-tests/Runtime/peer.py) reuses the accepted Round34 independent wire/model/event oracle, regenerates the compact fixture, and compares full initial/final models, revisions, logical sizes, ordered metadata, and kernel work for 34 sequences and 106 events.

The native run passed 521 checks, including 373 budget checks and twelve store/lifecycle groups. These establish the bounded prototype contract; they add no executable research or protocol-coverage percentage. See [the checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R35_CREO6_COMPOSITE_STORE.md) for reproduction and scope gates.

Remaining integration work: join this prepared publication seam to a world-owned transaction and retain explicit incarnation checks at event delivery. No such join is claimed here.

## Round36 composite world and terrain boundaries

The prepared publication seam is now joined to the optional [world partition](SCENE_WORLD_CREATURE_VIEW6_COMPOSITE_CONTRACT.md), with guarded event delivery and original-host proof. This store/kernel still supports scalar/HAM updates; defender/wearable complete-union publication remains separate.

## Round37 complete creature kernel and supplied terrain

The separate complete CREO6 kernel is accepted. This prepared store still owns only its frozen scalar/HAM state and24-byte pending descriptions. A new complete current-incarnation store must be accepted before world delivery uses all18 delta indices. [Evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R37_COMPLETE_CREATURE_AND_SUPPLIED_TERRAIN.md).

## Round38 full creature publication and terrain inputs

A separate [full prepared store](SCENE_CREATURE_VIEW6_FULL_STORE_CONTRACT.md) and world join are accepted. This frozen scalar/HAM store retains24-byte journal accounting and its own regression suite. [Evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R38_FULL_CREATURE_PUBLICATION_AND_TERRAIN_INPUTS.md).
