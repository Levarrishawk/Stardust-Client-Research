# Standalone tangible replication store

R26 Runtime, 2026-10-03. This engine-neutral store consumes frozen tangible common-field models. It does not decode TANO wire bytes or integrate the world coordinator, router, raw source, or presentation journal. Existing creature and component behavior remains unchanged.

## API and identity

`SceneTangibleReplicationStore(registry, limits = null, revisionSeed = 0)` borrows a `SceneObjectRegistry`. `Seed(incarnation, baseline)` returns an immutable `SceneTangibleReplicationSnapshot`; `Apply(incarnation, plan)` and `Apply(exactSnapshot, plan)` return the next snapshot. `Find`, `IsCurrent`, `Snapshots`, `RetainedDecodedBytes`, `Revision`, `Close`, and `Dispose` expose lifecycle state.

Snapshots contain incarnation, global store revision, historical baseline, current names, common tangible scalars, customization bytes, and normalized components. Registry-current pending and finalized incarnations are accepted. Family, recipe, and world publication barriers require future integration. Exact snapshots require the same store owner and current object reference. Stale incarnations or snapshots return null; a current incarnation without a seed throws. A recreated object cannot accept an old incarnation's work.

Every public operation requires the creating managed thread. A busy guard rejects reentry. No public callback or mutable caller enumeration executes during an operation, so same-thread reentry is guarded by inspection rather than a dynamically reachable test. The host must serialize access to the borrowed registry as well.

## Ordered state and budgets

Defaults are 64 actors, 16 MiB logical decoded bytes, 64 raw component keys, 64 normalized component elements, 65,536 bytes per string/customization field, 65,536 UTF-16 units, 4,096 updates, and 4,096 aggregate component commands. Constructor ceilings are respectively 4,096; 128 MiB; 262,144; 65,536; 1 MiB; 524,288; 4,096; and 65,536. Actor/update minima are one, decoded minimum is 47, and other minima are zero.

Seed preserves raw historical component order and duplicates while normalizing current keys to a signed unique set. Apply walks common indices 0 through 10 in plan order, including repeated updates. It reuses the accepted component kernel with a trusted count observer and one command pass. Name/customization bounds and global decoded charge are checked after every update; component growth is checked inside each command, including growth followed by a clear. A later shrinking update cannot rescue an earlier budget violation.

Logical charge is `baseline.DecodedBytes + 16 + 4 * currentComponentCount + names.AdditionalDecodedBytes(baseline) + currentCustomizationBytes`. Customization contributes zero additional bytes only when it aliases the baseline storage. Name accounting uses the frozen per-role baseline alias rule. Equal bytes in separate storage still cost bytes. Fixed common scalar state is covered by the baseline charge. This is a compatibility budget, not managed heap size; externally retained old snapshots are excluded.

All candidates, allocations, checks, and checked arithmetic precede assignment of the staged map, retained total, and revision. A successful seed or plan increments one global revision; failures publish no prefix and preserve exact prior snapshots, charges, and revision. Revision overflow rejects. Writes do not incidentally prune unrelated stale entries. `Find`, `IsCurrent`, `Snapshots`, and retained-byte reads retire registry-stale entries; a revision read does not. Consequently stale slots may consume capacity until an explicit state read. Genuine reseeding replaces current overlays and recalculates charge.

Close is owner-thread-only and idempotent, clears owned entries and charge, preserves revision, and leaves the borrowed registry open. Closed writes throw; state reads expose empty state.

## Evidence and validation

These are confirmed implementation properties of the store (external historical reference), exercised by native scenarios (external historical reference) and an independent ordered receipt oracle (external historical reference). Native testing passes 93 checks in 28 cases, including malformed capacities, intermediate overflow, stale identity, reset/recreation, duplicate keys, counter wrap boundaries, all common scalars, signed zero, thread ownership, and cleanup. The oracle recomputes ordered names/raw bytes/component arithmetic and logical charges from synthetic fixtures (external historical reference); it is not another scheduler or a TANO wire decoder.

The seed fixture projects a decoded synthetic CREO model through frozen `FromCreatureCommonFields`. This confirms model-store behavior only. No original executable, live connection, or asset loader ran. See the [bounded checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R26_TANGIBLE_REPLICATION.md) for provenance and regression results.

Next: add internal owner-held preparation/commit tokens before joining tangible changes to a world transaction, then validate that combined publication preserves atomicity and presentation ownership. A direct sequential world mutation followed by public store Apply is insufficient evidence of that future guarantee.
