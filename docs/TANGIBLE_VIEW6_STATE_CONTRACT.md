# Tangible view6 owned state contract

## Result and evidence

TangibleView6State (external historical reference) implements an immutable, bounded TANO view6 state over the [accepted codec](TANGIBLE_VIEW6_CODEC_CONTRACT.md) and [specialized executable policy](TANGIBLE_VIEW6_STATE_POLICY.md). The executable observations and their confidence remain in that policy. This contract describes tested replacement behavior; whole-message atomicity, resource budgets, revisions and event receipts are explicit modern implementation choices.

The state owns raw U64 IDs, preserving all bits, duplicates and order. It retains the object ID, baseline declared-count metadata, passive U32, raw U32 counter and a checked U64 branch revision. It does not resolve IDs into objects or reproduce legacy Watcher ownership.

## Publication and lifetime

`FromBaseline` copies the decoded IDs and starts revision at `revisionSeed + 1`. `Reseed` requires the same object ID, restores every baseline field, retains the current limits and advances revision. `Apply` validates a complete decoded plan, builds a private candidate and returns `Previous`, `Current`, declared-count metadata, ordered event descriptions and logical work used. It executes no callbacks. Any failure leaves the input state intact, including failure after intermediate candidate mutations.

Every successful Apply advances revision, including an entirely skipped or no-op plan. Revision overflow rejects the operation. Revisions identify positions on one immutable branch; they do not establish global uniqueness, current world incarnation or publication ordering. A caller must perform that join and publish the candidate before delivering its event descriptions. Independent calls may safely branch from the same immutable state.

An optional `operationLimits` replaces the previous limits, may tighten or widen them, and becomes the returned candidate's retained policy. The previous state retains its own policy. Limits are copied and validated against hard ceilings before work.

## Compatibility rules

For each vector field, compute `skip = min((C - U + N) modulo 2^32, N)` using the current local counter, incoming counter and raw loop extent. A command whose starting cursor is below skip is consumed whole without mutation. Every command advances cursor once; assignment additionally advances by its entire value count. Assignment can therefore overshoot skip or the final extent. Repeated vector fields recompute skip from the updated counter. Incoming counters never resynchronize the local counter.

Valid removal emits Removed then Changed and increments the counter once; invalid removal is a no-op. Add grows with zero IDs to the requested index, inserts the new ID, emits Added then Changed and increments once. Set grows with zero IDs through the requested index and emits Set then Changed unless an existing element already equals the requested ID. An equal existing set is a no-op. Assignment replaces the whole vector even when empty or equal, emits Changed and increments by value count plus one. Nonempty clear emits Removed in ascending old index order followed by Changed and increments once; empty clear is a no-op. Counter arithmetic wraps modulo 2^32. Passive fields update only the passive U32.

Event records contain kind, nullable index and nullable old/new IDs. They are immutable descriptions of the candidate's mutations, not proof that the original Tangible object had an active callback recipient. Assignment emits no individual removal/addition events.

## Resource accounting

Limits cover final and intermediate vector size, field and actual command counts, aggregate assignment and whole-message ID counts, message bytes, state bytes, event count, decoded output bytes and logical work. `MaximumMessageBytes` includes the two-byte declared-count prefix plus decoded field payload bytes, matching the accepted codec payload budget; it excludes the outer object/family/view envelope. Baseline uses the same convention.

State bytes are `8 + 8 * ID count`. Decoded output bytes are state bytes plus 24 per event. These are deterministic logical budgets, not managed heap measurements. Intermediate capacity checks conservatively retain the pre-removal/pre-clear vector when budgeting prospective events.

Work starts with old ID count plus field count, actual command count and every message ID, including skipped IDs. Applied removal charges current count minus index; add charges growth plus shifted old elements; changed set charges growth plus one; assignment charges old plus new count; nonempty clear charges old count. Final copying charges final ID count plus event count. These work units are modern policy, independently reproduced by the Python oracle. All global plan limits are checked before candidate copying, and intermediate growth/events/work are checked before their respective allocations or mutations.

## Validation and remaining boundary

The independent Python peer (external historical reference) constructs literal wire fixtures and independently computes states, event order, work and peak budgets. The native scenarios (external historical reference) compare exact baseline/delta bytes, transitions and receipts, exercise exact and one-less limits, and verify input preservation, reseeding, revision overflow and branching ownership.

Round 30 passed 21 sequences / 23 transitions, 700 negative checks, 315 budget checks and 114 ownership checks. Accepted round-28 codec regression and the shared Core smoke suite also passed. See the [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R30_TANGIBLE_VIEW6_STATE.md) for reproducibility.

Current-world recipient selection, incarnation rejection, weak-target resolution, callback dispatch and integration with a scene registry remain outside this state component. The next bounded probe is the caller's current-incarnation/publication join using these immutable candidates.
