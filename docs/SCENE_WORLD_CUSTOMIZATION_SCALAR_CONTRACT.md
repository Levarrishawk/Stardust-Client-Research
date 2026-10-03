# Atomic world customization and six-scalar application

Round22 Runtime, October2,2026 Eastern. This is a modern compatibility transaction over the accepted [raw sidecar](SCENE_WORLD_CUSTOMIZATION_STATE_CONTRACT.md), [world coordinator](../poc/Stardust.Core/Session/SceneWorldCoordinator.cs) and [creature replication store](../poc/Stardust.Core/Session/SceneCreatureReplicationStore.cs). It consumes the lead-frozen [ordered immutable union](../poc/Stardust.Core/Protocol/CreatureView3CustomizationScalarDeltaPlan.cs); the injected decoder remains a separate compatibility boundary.

```csharp
bool world.ApplyCustomizationScalars(long generation,
    CreatureView3CustomizationScalarDeltaPlan plan,
    SceneWorldCustomizationStateStore rawStore);
new SceneWorldCustomizationScalarApplicationRouter(world, rawStore,
    Func<ReplicationEnvelope,CreatureView3CustomizationScalarDeltaPlan> decoder,
    Func<long,ReadOnlyMemory<byte>,bool>? downstream=null,
    int maximumPayloadBytes=65536);
bool router.Accept(long generation, ReadOnlyMemory<byte> message);
// SceneWorldReplicationPolicy adds optional UInt64 RevisionSeed=0.
```

## State and application

Construction/entry use the serialized owner thread. Stale generations return false before touching plan/source inputs or parsing. Current direct application requires an active scene, current finalized creature incarnation, complete world-owned typed baseline and an open raw store borrowing that exact world. Missing/static/incomplete targets, foreign/closed sources and capacity errors throw without failing the world or closing borrowed objects. Opaque worlds lack the required typed baseline. The method addresses the coordinator's current incarnation by object ID; a caller carrying only a same-generation stale object ID cannot distinguish a recreated incarnation. Router capture/revalidation rejects a callback that changes the incarnation before application. Hosts needing an earlier incarnation token must validate it at their own boundary.

Field4 writes remain opaque immutable strings. Raw strings are extracted in original subsequence order, including duplicates/empty values; scalar records11..16 apply in their original subsequence order, so the last Height/Posture/etc write wins. No callback or observer sees intermediate cross-kind state. Historic baseline identity, wounds and components survive the transaction. Scalars, mixed and mixed/components world modes all support this slice. Packed structural decoding/local invalid-integer policy belongs to the downstream projector; an opaque invalid local attempt does not cause this store to clamp, drop or reinterpret the raw bytes.

Raw-only application preserves the typed replication state and revision and publishes a world Replication change. Scalar-only application preserves existing raw entries, current bytes, pending journal, issued receipt and global raw revision exactly; it validates source binding without seeding/pruning. A complete later Find may independently perform normal lazy baseline reseeding. Mixed application updates both. Raw updates invalidate issued receipts; scalar-only updates preserving baseline/incarnation leave them valid. Same-incarnation baseline replacement appends the new seed through existing sidecar behavior. Successful raw application stages removal of stale entries before seeding a recreated incarnation; no failed application prunes state.

## Preparation and commit

The coordinator enters once, inspects replication without retiring unrelated entries, and creates its own exact snapshot from the owned registry/placement/envelope state. No caller-controlled list or callback is inspected. It preflights active/current target, field/scalar policy limits, checked world mutation revision, pending-world-object capacity and a private copy of the world change map. Raw preparation enters its owner guard, checks open/source binding, stages a filtered copy of its entries, seed/current/ordered updates, global object/pending/string/reference-byte bounds and all checked raw/mutation increments. It uses the coordinator-verified snapshot directly: it never calls Latest, Prune, world.Snapshots or world.IsCurrent while the coordinator is busy.

If scalars exist, replication preparation holds its owner guard, verifies exact owned current snapshot, applies six known finite records privately, checks its checked global revision and capacity/retention invariants, and creates an owned replacement map. Scalar writes add no variable decoded storage, so retained decoded bytes remain the already-admitted baseline/wounds/components charge. MaximumScalarUpdates applies to the scalar subsequence; mixed MaximumFields/MaximumScalarUpdates and components MaximumFields additionally bound the union where configured. DeclaredCount remains transport metadata and is not used as an invented actual-update denominator. The frozen plan bounds the union to1..4096 records.

After every preparation succeeds, commit only swaps prebuilt maps/revisions under all owner guards. There is no decoder, loader, callback, world reentry, new allocation, checked arithmetic or sequential Accept-then-Apply rollback. Finally releases both store guards on success/failure. A rejected preparation preserves both semantic stores, global revisions, raw receipts and pending world change batch; private candidates are discarded. This guarantee covers ordinary validation/arithmetic failures, rather than process termination or catastrophic runtime failure. Optional replication RevisionSeed is an explicit reproduction input for global overflow, default0; revisions remain monotonic across retirement/reset like the prior store. Raw logical byte accounting remains reference-deduplicated and excludes CLR/DTO/GPU/caller-retained memory.

## Injected router

[Router](../poc/Stardust.Core/Session/SceneWorldCustomizationScalarApplicationRouter.cs) routes exact CREO3 delta outer envelopes with first field4 or11..16 through the supplied full union decoder. Other first fields/families/views/messages go downstream, default world.Accept. Replace the field4-only router at this join; do not stack two interpreters for the same union. Actual payload codecs/limits and rejection of unsupported mixed fields are the injected decoder's responsibility. Router validates plan object ID/raw DeclaredCount against envelope before application.

Round21 callback guards are retained: owner thread, caught reentry latch, source closure/mutation, generation, target object/incarnation/current replication and observable pending world batch before/after decoder. Decoder must remain pure; ordinary downstream world mutation is allowed, source mutation/reset/reentry is rejected. Guards do not expose a universal resource-only mutation token and cannot roll back already-performed external callback effects. A current parse/callback/budget failure closes only this router and releases callbacks/borrows. Foreign-thread rejection precedes closure. Router-owned downstream message bytes are cleared in finally; caller memory remains untouched and downstream may borrow memory only during the callback. The owned envelope passed to decoder is independently retainable.

## Evidence, limits and join

[Native scenarios](../poc/Stardust.Core.SmokeTests/SceneWorldCustomizationScalarScenarios.cs) use real accepted coordinators, authored frozen plans, exact prior synthetic outer/baseline fixtures and a loader that must never run. They pass196 checks/40 cases. Both orders/repeated raw and Height, six scalar values, retained wounds/components/baseline, raw-only/scalar-only receipt semantics, unseeded/closed/foreign/stale/thread/actor/count/string/byte/global-overflow failures, world pending capacity, reseed/destroy/recreate/generation and callback cleanup are exercised. Independent [authored events](../poc/round22-tests/Runtime/ordered-fixtures.json) plus [Python oracle](../poc/round22-tests/Runtime/verify.py) compute the final raw subsequence/revisions and scalar order without importing production state code. This is independent output evidence, not an independently executed C# scheduler or wire parser.

Prior raw state136/28, consumer160, pump171, drain1165/37 and their inherited independent receipt audits pass. No original executable, GPU, asset preparation or unfinished codec is used; no research/function/protocol coverage metric changes. [Checkpoint/reproduction](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R22_CUSTOMIZATION_SCALAR_STATE.md).

Exact lead join: register SceneWorldCustomizationScalarScenarios.Run(inputs,peer,exportPath), inject the accepted union decoder into this router over motion/application ingress, and pass router.Accept to the pump. Keep one accepted raw projector and acknowledge only the exact complete current raw batch after successful projection. Component/wounds fields inside this union and real packed-range/render behavior remain separate milestones.
