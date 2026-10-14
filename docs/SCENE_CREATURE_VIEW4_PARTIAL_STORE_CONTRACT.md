# CREO4 received-only partial replication store

This bounded engine-neutral prototype retains only scalar observations received for an exact scene incarnation. It borrows SceneObjectRegistry (external historical reference) and uses the accepted [partial-state kernel](CREATURE_VIEW4_PARTIAL_STATE_CONTRACT.md). It does not establish recovered client lifecycle behavior, baseline semantics, default values, or gameplay interpretations.

## API and identity

SceneCreatureView4PartialReplicationStore (external historical reference) accepts `Apply(currentHandle, expectedSnapshot?, delta)` in PendingBaselines and Finalized phases. Null expected is legal only for the first publication in the exact current incarnation. A subsequent publication requires the exact stored snapshot from this store. Stale handles, foreign/stale snapshots, and null overwrites return null without processing the plan. Eligible malformed or over-budget plans throw before publication.

A snapshot records publication Handle, Incarnation, global store Revision, immutable partial State, raw DeclaredCount, and total WorkUsed. Store revision advances once per committed plan; the per-incarnation state revision begins at one and advances independently. EndBaselines or containment may change the registry handle without changing incarnation: `Find(newHandle)` retrieves the same snapshot and `IsCurrent(snapshot)` remains true. Apply and prepared commit still require the exact current registry handle. Replacing or destroying/recreating an ID creates a new incarnation and fresh unknown partial state; no fields transfer.

Fields 0,1,4,5,7..12 retain raw U32 words and field6 a raw U64. Unknown scalar fields remain absent, including before the first store publication. Received zero is known zero. Collection fields 2/3/13 remain rejected by the frozen codec. Every assignment, including an equal assignment, produces one ordered event with old presence, nullable old bits/provenance, and received source revision/declared count/ordinal. No float normalization is introduced.

## Atomic preparation and reservations

Internal PrepareApply, CheckPrepared, CommitPrepared, ReleasePrepared form a real preparation lifecycle. Prepare holds the store gate and builds candidates; Check requires the active preparation, exact owner, unconsumed revision/epoch, exact registry handle/incarnation, and exact expected snapshot. Commit rechecks and installs prebuilt candidates. Release unlocks both cancellation and consumed operations. Public Apply uses these methods with finally release. The store enforces its construction thread and rejects public reentry while a preparation is active. The borrowed registry is assumed serialized by its host; this is not cross-thread synchronization.

The entire plan is measured by frozen `CreatureView4Codec.MeasureDelta`. Recipient, scalar type/width, update count, checked next store/state revision and journal epoch, known mask, actor/state/event/output caps, and total work are checked before Empty, snapshot, dictionary, or journal candidates are allocated. This is logical compatibility accounting, not managed allocation accounting.

Let N be the number of ordered updates, K the target's final known scalar count, A the retained dictionary count before this operation (including stale entries), P the existing pending journal count, and I=1 for the first publication in the current incarnation, otherwise 0.

- Target state bytes: `16 + 32*K`.
- Each event: fixed 64 logical bytes, including equal assignments.
- Kernel work: `22 + 3*N + K`.
- Store overhead: `1 + 3*N + 11*I + A + P`.
- Total store work: `23 + 6*N + K + 11*I + A + P`.
- Global output: all prospective retained state bytes plus `64*(P+N)`.

The overhead charges eligibility lookup, full plan measure, mask scan, first Empty initialization, dictionary copy, and old/new journal copy. Defaults are 64 objects, 1 MiB retained state, 4096 pending/per-apply events, 2 MiB output, and 1 MiB work. Independent partial-state and message limits also apply; effective per-operation kernel limits deduct other actors, existing journal bytes, and store overhead. Immutable construction policies are copied. RevisionSeed applies only to store identity metadata.

Writes do not prune unrelated stale entries. A failing write cannot free their actor/state reservations. Replacing the target stale incarnation subtracts its old bytes only in the prospective successful replacement. Read-side Find/IsCurrent/RetainedStateBytes/DrainEvents retire stale state, preserve historical events, and advance the journal epoch. This behavior and its limits are modern prototype policy.

## Journal versus delivery

DrainEvents returns an exact issued batch with store revision and internal epoch. It does not remove events. Every successful write or stale-state retirement invalidates that batch. Acknowledge succeeds only for the exact current issued batch from the same store, clears the whole pending journal, and advances epoch without changing store revision. Double, foreign, and stale acknowledgement fail.

IsCurrentEvent requires that exact issued batch and a current registry incarnation in either accepted phase. Replacement, destroy, and reset leave historical journal records available to drain/acknowledge while making their delivery checks false. Historical acknowledgement does not require that each event is deliverable. The host must serialize delivery and lifecycle. Close clears state and journal, invalidates receipts, and does not close the borrowed registry. Apply/Drain throw after store close; identity queries return false and counters report cleared state.

## Evidence and reproducibility

Observation: the source implements the reservations and preparation ordering above; the independently authored Python model matches actual Core outputs for synthetic literal CREO4 envelopes, state presence/raw words/provenance, ordered journal records, revisions, work and aggregate bytes. Confidence: Confirmed for this prototype by deterministic execution, not a claim about the legacy executable. Implementation consequence: a future world join must preserve these identities and accounting rather than turn missing fields into defaults.

[Round45 checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-04_R45_CREO4_PARTIAL_STORE.md) records the frozen input and validation. Scenario suite (external historical reference) is linked only into the separate runner (external historical reference), which references the actual Core project. Reflection exercises genuine internal preparations; no production source is duplicated into the runner. Independent peer (external historical reference) regenerates the synthetic fixtures and compares native receipts without importing native expectations.

Next probe: define a bounded world/router join for received-only partial state with explicit baseline exclusion, lifecycle serialization, delivery revalidation, and failure cleanup. This round adds no such join and makes no coverage-denominator change.

## Round46 accepted compatibility increment

The [partial world/router](SCENE_WORLD_CREATURE_VIEW4_PARTIAL_CONTRACT.md) and [original-resource preview](SCENE_WORLD_CREATURE_VIEW4_PARTIAL_PREVIEW_CONTRACT.md) now accept pending/finalized received scalar state without defaults. The earlier next-probe paragraph describes the Round45 boundary; it is closed by Round46 for this explicit policy. Collections, merging and original defaults remain open.
