# Complete CREO4 prepared storage

[SceneCreatureView4FullReplicationStore](../poc/Stardust.Core/Session/SceneCreatureView4FullReplicationStore.cs)
owns [complete CREO4 state](CREATURE_VIEW4_FULL_CONTRACT.md) and variable-size
historical journals over a borrowed serialized SceneObjectRegistry. No world,
ingress, presentation, original subscriber or gameplay effects are introduced.

## State, limits and identity

Seed, Apply, Find, IsCurrent, DrainEvents, Acknowledge, IsCurrentEvent and Close
follow the accepted [prepared lifecycle contract](SCENE_CREATURE_VIEW6_FULL_STORE_CONTRACT.md).
All operations require the constructing managed thread. Exact handles and
incarnations distinguish same-incarnation handle changes from object recreation.
Apply requires this store's exact current snapshot; stale recipients return null
before accessing a plan. Valid plans advance one store and one state revision.
Recreation restarts state revision; store revision continues. Read-side stale
retirement changes batch epoch, while writes do not prune unrelated stale actors.
The borrowed registry must be mutated serially; there are no store callbacks.

Limits default to64 actors,1MiB retained state,4096 pending/per-apply events,
2MiB logical output and1048576 kernel work units. Hard ceilings are4096 actors,
128MiB state,262144 events,256MiB output and16777216 work. State/output caps have
minimum8; the empty full state actually requires72 bytes. Nested model/message
limits are copied and validated. Work measures the kernel's defined operations;
it is not total allocation size or elapsed CPU time.

PendingEventBytes sums each immutable event's LogicalBytes: scalar/encumbrance48,
skill64+raw key length, mission64. Global output reserves other actors and the
entire pending journal before the complete kernel performs its own intermediate,
mapped-event and child-construction reservations. Raw duplicate seed budgets
precede runtime map/set normalization. Publication totals are checked before
allocating the replacement snapshot/map/journal.

## Prepared seam and journals

PrepareSeed/PrepareApply build the complete candidate under the owner gate.
CheckPrepared validates owner, active candidate identity, unconsumed status,
checked next revision/epoch and exact current registry recipient. CommitPrepared
rechecks those conditions immediately before assigning prebuilt references;
ReleasePrepared clears the active slot. Abandoning preparation publishes nothing.
Foreign, consumed, released or registry-stale candidates cannot commit.

DrainEvents issues an exact owner/revision/epoch-bound immutable batch. Historical
events remain valid across later state snapshots in the same incarnation while
that exact batch is current. IsCurrentEvent additionally checks registry
incarnation. Acknowledge accepts only the current issued batch once. Close clears
owned state/journal and leaves the borrowed registry open; caller-held old
snapshots/batches remain immutable outside store retention.

## Verification

[The native integration runner](../poc/round42-tests/Integration/StoreProgram.cs)
uses the actual separate Core assembly. Its [independent peer](../poc/round42-tests/Integration/store_peer.py)
reconstructs122 full initial/final models and2439 ordered events from literal
wire fixtures. It checks exact variable journal/state accounting and outer store
revision, aborted preparation, stale/foreign commit, owner thread, historical
events, acknowledgment, recreation and deterministic close.

Two-actor literal tests admit exact274-byte skill and272-byte mission global
output, reject273/271, and retain prior snapshots/batches on failure. Later
growth rejects after the exact admitted limit. The [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R42_PRIVATE_CREATURE_AND_TERRAIN_SEQUENCE.md)
records executed corrections and acceptance. World delivery remains next.

## Round43 integration

The optional [world seam](SCENE_WORLD_CREATURE_VIEW4_FULL_CONTRACT.md) and [private preview](SCENE_WORLD_CREATURE_VIEW4_FULL_PREVIEW_CONTRACT.md) are accepted for seeded finalized targets. The [observer delivery finding](CREATURE_VIEW4_DELIVERY_CONTRACT.md) requires a separate default-origin/pending-baseline policy; the complete store does not establish those defaults.
