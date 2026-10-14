# Creature view 6 current model and publication store

Round33, 2026-10-03. This independently authored modern component applies the accepted [CREO6 supported codec union](CREATURE_VIEW6_CODEC_CONTRACT.md) to immutable full current models and composes them with borrowed serialized scene-registry tokens. It does not implement gameplay, legacy callbacks, target resolution or world routing.

Implementation: state (external historical reference) and store (external historical reference). Verification: native scenarios (external historical reference), isolated runner (external historical reference), synthetic fixtures (external historical reference), independent peer (external historical reference).

## Immutable current model and event policy

`CreatureView6State.FromBaseline(CreatureView6Baseline baseline, CreatureView6StateLimits? limits = null, ulong revisionSeed = 0)` validates all eighteen fields' aggregate storage and work, then publishes state revision seed+1 with checked arithmetic. `Model` exposes the complete current immutable `CreatureView6Baseline`, and `ObjectId`, `Revision`, `DecodedBytes` provide identity and accounting.

`Apply(CreatureView6Delta plan, CreatureView6StateLimits? limits = null)` returns `CreatureView6StateChange` with `Current`, immutable ordered `Events`, and `Work`. Nonempty supported updates 0,2-12,16-17 apply in exact duplicate order. Every update replaces its corresponding raw scalar, raw byte string or both pair values in a fresh full current model. Signed performance type is interpreted from raw32 bits; no other normalization, host text decoding or bool conversion occurs. The model retains baseline raw declared-count metadata; a delta's raw declared count remains plan metadata.

Defenders, HAM, maximum-HAM, wearables and their counters remain untouched. Their already immutable references may be shared safely. Strings retain both bytes and short/extended spelling. After an override, current state does not retain an extra seed baseline or former strings through an overlay: it retains only the full current model. Caller-held prior snapshots naturally keep their own historical models.

Modern event policy: every supported assignment, including equal-value assignments, produces `CreatureView6StateEvent(Index, Ordinal)` in packet order. Ordinal starts at zero for each plan. Events describe field assignment only; they contain no old/new string, scalar value, model graph or callback target. This policy is not recovered legacy callback behavior. Each valid nonempty plan advances state revision once, including equal-value plans. Empty plans, unsupported vector indices 1,13-15 and wrong object IDs reject. There is no vector composition in this component.

## Logical bounds and work

State limits bound actual update count, per-string and aggregate string bytes, per-vector and aggregate element counts, complete state bytes, event count, state-plus-event output and work. Defaults: 4096 updates,65536 bytes per string,262144 aggregate string bytes,4096 elements per vector,16384 aggregate elements,2 MiB state,4096 events,4 MiB output and 1048576 work. Hard ceilings: 4096/1 MiB/4 MiB/65536/262144/16 MiB/262144/32 MiB/16777216 respectively. Zero budgets are valid policies that may reject nonempty contents.

Complete current state charge equals accepted codec baseline logical decoded bytes: exact raw field representation plus 10 bytes for object ID and declared metadata. Equivalently,102 fixed bytes plus 8 per defender,4 per HAM/max-HAM value,16 per wearable, plus each string's content and 2-byte or 6-byte length spelling. This includes wearable customization strings and nested count/counter headers. Empty complete state costs 108 bytes. There is no deduplication discount for equal strings or shared immutable references.

Events charge8 logical bytes each. Output checks current candidate state plus all event descriptions produced so far. Every intermediate model is checked before a later shrinking assignment; a final fit cannot hide earlier growth. Seed work is `18 + totalElements + totalStringBytes`. Apply work sums `1 + 18 + totalElements + totalStringBytes` for each intermediate current model. These deliberately conservative modern units are not recovered CPU costs. Limit rejection occurs before returning a new state/change. Neither state nor vectors mutate in place.

## Store API and lifecycle

`SceneCreatureView6ReplicationStore(SceneObjectRegistry registry, SceneCreatureView6StoreLimits? limits = null, CreatureView6StateLimits? stateLimits = null, ulong revisionSeed = 0)` copies/validates policies. It borrows the registry, and its creating managed thread owns every operation. Reentry and foreign-thread access reject before mutation. The host must serialize the borrowed registry as well; registry rechecks are not concurrent-thread safety.

Public operations: `Seed(handle, baseline)`, `Apply(handle, exactExpectedSnapshot, delta)`, `Find(handle)`, `IsCurrent(snapshot)`, `Revision`, `RetainedStateBytes`, `PendingEventCount`, `DrainEvents()`, `Acknowledge(exactBatch)`, `Close()` and `Dispose()`. Snapshots expose Handle, Incarnation, checked global store Revision and immutable State. Store-global and per-model state revisions are separate counters.

Seed/apply require the exact currently valid registry handle before payload access. Apply additionally requires this store's exact stored snapshot reference and current incarnation. Foreign, superseded and destroyed/recreated tokens cannot overwrite state. Same-incarnation handle movement preserves state: Find(newCurrentHandle) may return the historical stored snapshot with an old handle, whose own IsCurrent is false; Apply(newCurrentHandle, thatExactStoredSnapshot, plan) refreshes publication. Same-incarnation seed continues state revision; recreated seed starts at one while store-global revision continues.

Writes deliberately do not prune stale other entries before failure. Find, IsCurrent, RetainedStateBytes and DrainEvents retire stale incarnations; same-incarnation handle changes do not retire them. Retirement preserves historical journal records, invalidates issued batch epoch and does not advance store publication revision. Revision/PendingEventCount do not prune.

Journal records contain recipient handle/incarnation, store publication revision and event metadata. They describe successful historical publication even after removal/recreation/reset. Drain does not authorize delivery; the eventual host must validate `registry.IsCurrent(record.Incarnation)` before any callback. Drain returns the same immutable issued batch until a publication, retirement or acknowledgment changes its epoch. Acknowledge accepts only this store's exact issued reference, matching publication revision and epoch. Success clears the whole journal once; stale, foreign, repeated and closed acknowledgments fail. Even an event-free reseed invalidates the prior batch.

Close is idempotent, clears owned maps/journal/issued references and retains the revision, never closes the borrowed registry. Closed writes and drain throw; find/currentness return empty/false and acknowledgment fails. Reset/destruction retire mapped state on the next retirement read; historical journal ownership may still be acknowledged.

Store defaults: 64 objects,1 MiB mapped state,4096 pending/per-apply events,2 MiB logical output and 1048576 kernel work. Ceilings: 4096 objects,128 MiB state,262144 pending/per-apply events,256 MiB output and16777216 work. Minimum store state/output budgets are8 bytes; they can intentionally be too small for a complete seed. Effective kernel limits subtract other actors' mapped charges and8 bytes for every existing journal event. Pending capacity similarly subtracts the existing queue. Global output therefore includes all mapped state and old plus newly generated events.

These are logical compatibility accounting limits, not CLR heap limits. Caller-retained old snapshots/batches are outside store retention. Kernel MaximumWork does not charge dictionary copying or journal copying; those overheads are separately bounded by MaximumObjects and MaximumPendingEvents. No lifetime work budget is claimed.

## Genuine prepared composition seam

Internal PrepareSeed/PrepareApply hold the owner gate and build the complete immutable state, replacement map, journal, snapshot, charge and checked revision/epoch. The candidate payload is private to trusted internal composition. CheckPrepared requires exact active candidate identity, store owner, unconsumed state and exact current registry handle/incarnation. The caller must complete any future world journal/resource budgeting and allocation before this check.

CommitPrepared assigns only prebuilt fields and consumed status; no callback, parsing, allocation or checked arithmetic follows the first assignment. No external mutation may intervene between check and commit. ReleasePrepared drops the exact active candidate and owner gate; releasing without commit publishes nothing. Public Seed/Apply use this same seam. Failed preparation/check preserves existing map, revision, charges, earlier journal and issued batch. A released old candidate cannot pass against a later active candidate, even if next revision/epoch happen to match.

Native reflection tests exercise the internal seam without adding friend-assembly/public APIs: owner-gated reads during preparation reject, moved recipient invalidates check, release leaves no prefix, canceled candidate cannot substitute for a newer candidate, successful checked commit publishes once and consumed check rejects.

## Evidence and remaining uncertainty

Confirmed modern implementation tests: 1429 checks, 1038 budget checks, 7 baseline sequences and 12 store case groups. Independent accepted32 Python wire parser and a separately authored full-model assignment/budget oracle agree on 112 transitions and 308 ordered events. Fixtures include all 23 accepted supported wires, full raw bits, short/extended strings, arbitrary bytes, nested vectors/counters, repeated assignments, exact/one-less intermediate caps, alias access, global other actors, valid-earlier retention, owner/closed/foreign/currentness, lifecycle and prepared cancellation/commit.

No original executable/assets, live contact, packet capture, download or dynamic legacy execution was used. No existing production/shared file changed. Unsupported defender/HAM/wearable deltas and CREO6 world joining remain outside scope. Next: compose this internal seam with the lead-owned world journal using exact current incarnation and a separately chosen assignment-event delivery policy; independently closed vector state components need explicit adapters before incorporation.
