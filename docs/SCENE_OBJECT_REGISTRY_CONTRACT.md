# Bounded scene object registry contract

R5-NET-01, October 2, 2026. Engine-neutral single-owner registry driven by accepted
[R4 lifecycle DTOs](SCENE_OBJECT_LIFECYCLE_CODEC_CONTRACT.md). No wire parser,
template/engine object, baseline field requirement, resource loader or callback.

## Evidence and confidence

[Replication lifecycle](REPLICATION_SCHEMA_MAP.md), replacement implications,
[login/zone lifecycle](LOGIN_AND_ZONE_STATE_MACHINE.md) and
[end-baselines checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-13_SCENE_END_BASELINES.md)
confirm the distinction between creation, end-of-initial-baseline finalization
and destruction. R4 independently validated the four exact wire boundaries.
These are **Confirmed existing research**, not newly recovered behavior.
This registry's duplicate/replacement, revision, graph, limits and recovery
choices are **modern policy**. It does not reproduce original readiness virtuals,
repair bad positions, infer baseline field completeness or normalize transforms.
`Finalized` means only that this bounded registry accepted end-baselines, not
that a renderable object or resource barrier is ready.

## API and identity

`SceneObjectRegistry(capacity, sceneGeneration, revisionSeed=0)` requires
nonnegative capacity. The caller supplies U64 scene generation. The optional
revision seed supports deterministic exhaustion probes; normal construction uses
zero. Mutators must be called by one serialized owner; no internal locking.

Snapshots are sealed immutable records containing:

- Handle: registry GUID, scene generation, U64 object ID, U64 revision.
- Original immutable create DTO: raw transform, CRC and hyperspace byte.
- Phase PendingBaselines or Finalized; raw U64 container ID and U32 arrangement bits.

`Find(id)` returns the current immutable snapshot or null. `Snapshots()` returns
a read-only owned array in ascending unsigned object-ID order. Previous snapshots
remain valid owned observations after later changes; they are not live handles.
`IsCurrent(handle)` requires this registry's GUID, current scene generation,
existing object and exact current revision, and an open registry. Registry GUID
is a local ownership discriminator, not a wire field or an authorization token.

## Modern transition and rejection policy

Every mutation takes sceneGeneration first. Different generations, either older
or future, return false/null **before DTO validation or mutation**. Same generation
against a closed registry throws InvalidOperationException. Invalid arguments
and current-state errors are not conflated with stale deliveries.

| API | Effect | Duplicate / missing / rejection |
|---|---|---|
| Create(generation, DTO) | Adds PendingBaselines at root/container0/arrangement0 with new revision | Same ID and byte-identical original create is idempotent, returning current snapshot even if finalized/contained; conflict throws InvalidDataException; capacity excess throws |
| Replace(generation, DTO) | Explicitly resets existing ID to PendingBaselines/root/arrangement0; issues new revision and detaches direct children | Missing ID throws; always replaces even if create bytes match; no implicit conflicting replacement |
| Contain(generation, DTO) | Applies raw parent/arrangement with new object revision | Missing child or nonzero parent, self relation or cycle throws atomically; identical relation is idempotent |
| EndBaselines(generation, DTO) | PendingBaselines to Finalized with new revision | Missing ID throws; repeated end is idempotent and never removes an entry |
| Destroy(generation, DTO) | Removes ID and detaches its direct children | Missing/repeated destroy returns false; never finalizes |
| Reset(newGeneration) | Clears entries and installs strictly larger supplied generation | Equal/lower generation throws ArgumentOutOfRangeException; closed throws |
| Reset() | Checked current generation +1, then clears | U64 exhaustion throws OverflowException without changes |
| Close() | Clears entries and permanently marks closed | Idempotent, works at maximum generation; closed flag invalidates handles without needing counter increment |

Null DTOs throw ArgumentNullException for current open operations. Create/Replace
reuse the accepted R4 codec's validation to reject manually constructed nonfinite
DTOs. Object ID0 is rejected by the registry: zero is reserved as its root parent
sentinel. That is modern registry policy; the byte codec still accepts ID0.
Raw hyperspace flags are stored as-is on creation; destroy flag is accepted but
causes no special disappearance behavior in this model. No meaning is invented
for noncanonical bytes or arrangement bits. CRC, position X/Z/Y and quaternion
X/Y/Z/W are preserved without template lookup, coordinate conversion or scaling.

Byte-identical create comparison uses the accepted codec, preserving signed zero
and finite float bits rather than numeric record equality. Thus +0/-0 change is
a deliberate conflicting create requiring Replace. No approximate comparison.

## Graph and revision invariants

Missing parents use strict failure, with no deferred relation queue or guessed
repair. Existing graph is acyclic. Containment walks ancestors before mutation;
self and longer cycles leave every snapshot/revision unchanged. Parent0 is root,
not an entry. Root may retain arbitrary arrangement bits.

Destroy/Replace detach direct children to parent0 in ascending unsigned ID order,
preserving their phase, create DTO and raw arrangement bits. Each changed child
gets a new revision; grandchildren remain attached to their immediate parent.
Replace then gets the next revision for its own reset object. The removed ID is
absent after Destroy. A later recreation uses a strictly newer revision.

A single U64 monotonically increasing counter supplies revisions across all
entries and never rewinds on reset. Every actual snapshot change consumes one;
idempotent operations do not. Handles therefore invalidate async work on changes,
replacement, destruction, reset and close. Async consumers must call IsCurrent
at their commit boundary; this registry owns no actual tasks or task cancellation.
A scene-generation update accepted with the wrong caller tag cannot be detected
from DTO bytes; generation is external and no wire generation field is inferred.

Transactions build a candidate dictionary and candidate revision counter, then
commit both after all validation/allocation/checked arithmetic succeeds. Cycle,
capacity, invalid data and revision overflow cannot partially mutate entries.
Even overflow after the first detached child leaves the original graph intact.
Destroy without children needs no new snapshot revision and can succeed after
revision exhaustion; subsequent Create/Replace/change cannot allocate a revision.
Close/reset release owned entry references; immutable caller-owned snapshots
remain inspectable. No external resources require disposal.

## Verification and lead integration

[Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-02_R5_OBJECT_REGISTRY.md)
records commands/results. Native fixtures decode independently pinned R4 bytes
for create/containment/end/destroy before registry operations. Tests cover strict
and identical duplicates, explicit replacement, graph chains/cycles, parent
absence, count capacity, raw fields, old snapshot ownership, foreign owner/scene
and stale handles, reset/close, zero sentinel, signed zero, nonfinite DTO and
scene/revision exhaustion with multi-child rollback. No new wire oracle is needed:
R4 fixtures and codec/oracle remain read-only evidence.

Shared runner suggestion: `SceneObjectRegistryScenarios.Run()`; lead owns it.
Next bounded question: explicit required baseline-view bookkeeping, without
mixing object resource readiness or terrain behavior into this registry.
