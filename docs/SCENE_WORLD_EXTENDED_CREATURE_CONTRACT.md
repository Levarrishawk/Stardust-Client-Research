# Atomic extended CREO3 common and creature state

Round24 Runtime, October3,2026 Eastern. Extends the accepted [full transaction](SCENE_WORLD_FULL_CREATURE_CONTRACT.md) with seven fixed common fields using the lead-frozen extended union (external historical reference) and the separate implementation record. No original execution or new wire grammar is claimed.

```csharp
bool world.ApplyExtendedCreatureDelta(long generation, CreatureView3ExtendedDeltaPlan plan,
    SceneWorldCustomizationStateStore rawStore);
new SceneWorldExtendedCreatureApplicationRouter(world, rawStore,
    Func<ReplicationEnvelope,CreatureView3ExtendedDeltaPlan> decoder,
    Func<long,ReadOnlyMemory<byte>,bool>? downstream=null,
    int maximumPayloadBytes=65536);
bool router.Accept(long generation, ReadOnlyMemory<byte> message);
```

## Current overlay and history

Common fields:0 finite F32 complexity,3 U32 volume,6 U32 options,7 U32 use count,8 U32 condition damage,9 U32 maximum condition,10 raw U8 visibility. Signed zero, high U32 bits and visibility255 survive without reinterpretation. Names1/2 and inherited TANO remain outside this boundary. The frozen update factories reject unsupported/nonfinite common inputs; state is immutable and seeded from a complete baseline.

SceneCreatureReplicationSnapshot.TangibleScalars is current state; Baseline is historic. All historical-seed-preserving replacements in scalar/mixed/components/composite Apply and staged scalar/full/extended preparation explicitly carry current common state. Genuine baseline Seed alone initializes it from the replacement baseline. Thus old specialized ingress cannot silently revert newly received common values to history. SceneWorldReplicationSnapshot forwards the same state. No frozen API or raw/compound kernel changes are needed.

Extended typed preparation walks the input union in order. Common updates apply through the frozen state.Apply; full typed updates use the accepted scalar/wounds/components kernels with Round23 single-pass count observation. Raw writes keep their ordered subsequence in the separate journal. Common/full subsequences have no coupled value semantics; all global bounds are checked before any observation/publication. Last repeated scalar/common write wins. Historical baseline remains exact. One typed revision advances for any accepted common or creature/compound field, including no-op/replay. Raw-only preserves typed state/revision/common identity; typed-only validates the borrowed source but leaves raw entries/journal/issued batch/revisions untouched. Baseline freshness/incarnation rules still govern later acknowledgments/reseeding.

## Transaction and accounting

Coordinator (external historical reference) checks owner thread, current generation, active finalized creature/complete world-owned typed seed and exact open raw-source binding. Stale generation returns false before inputs, copying or parsing. Current invalid target/source/policy/budget throws while preserving semantic state and borrowed objects. Direct ObjectId addresses the coordinator's current incarnation; host-held earlier tokens need host validation. Router captures/revalidates incarnation across decoder callbacks.

Total union field count obeys configured mixed/components MaximumFields. Total scalar attempts count common plus existing creature scalars under MaximumScalarUpdates and mixed MaximumScalarUpdates; no-op and repeated attempts still count. Frozen union bounds1..4096. Compound aggregate command/value/key limits, enabled modes and policy/element matching retain Round23 semantics. Common/raw/scalar-only work in earlier scalar and mixed modes; compounds require corresponding configured support. DeclaredCount remains raw metadata, not an invented update denominator.

Preparation uses nonretiring inspection, private world journal, owner-held raw token and private typed map. It checks every global world/raw/typed revision increment, raw actor/pending/string/reference-byte bounds, world pending-object capacity and intermediate/global typed decoded charge before callback-free commit. Common state does not add variable logical decoded storage: baseline fixed-width actor charge already represents these fields. This logical accounting does not measure the additional CLR immutable-overlay objects, heap/container overhead, caller references or GPU memory. Compound retained charge remains baseline plus current wounds/component counts; trusted local observers reject command growth before a later Clear can conceal it. Failed preparation preserves common/scalar/compound/raw state, revisions, raw receipts, retained charge and pending world batch without incidental stale pruning.

Commit assigns only prebuilt state/revisions/charge/world journal under owner guards. It has no decoder, loader, callback, allocation, checked arithmetic or world reentry. Full23 preparation delegates to the shared extended typed preparation, so semantics and single-pass compound accounting stay aligned. Public old scalar/full application APIs remain available. Ordinary validation guarantees exclude process termination/catastrophic runtime failure.

## Injected ingress and callbacks

Router (external historical reference) routes exact CREO3 delta outer envelopes whose first field is0,3,4,5 or6..17. Other families/views/first fields/messages go downstream, default world.Accept. Injected decoder must consume the complete supported payload transactionally, reject names/unsupported tails and enforce bounded wire parsing. No unfinished ExtendedCodec dependency is used in Runtime fixtures. Plan object ID/raw DeclaredCount must match the outer envelope.

Round23 guards remain: construction/entry owner affinity, caught reentry latch, source mutation/closure, current generation and observable target/replication/world-journal freshness around pure decoder callbacks. Ordinary downstream world mutation is allowed; source mutation/reset/reentry is rejected. External callback effects cannot be rolled back and resource-only changes lack a universal coordinator token. Current parse/callback/budget failure closes only the router and releases callbacks/borrows; foreign-thread rejection precedes closure. Router clears its owned bounded message copy; downstream memory is callback-duration borrowed and caller bytes remain unchanged. Decoder envelope is independently owned/retainable.

## Evidence and join

Native scenarios (external historical reference) pass407 checks/81 cases: both common/full orders and repeats, seven exact fields/signed zero, historical baseline, fixed common-only charge, raw-only identity, typed-only receipt, specialized23/22/21 plus world composite/scalar/mixed preservation, genuine baseline reset, combined scalar/field limits, late common/raw/compound/byte/overflow rollback, lifecycle/binding/thread, first0/3/4/5/6..17 routing/fault cleanup and inherited4096-command compound checks. Exact carry arguments for every replacement, including direct components Apply, were also inspected; separate direct-components-after-common execution is not claimed. Correct Runtime14 peer family and accepted standalone compound codecs supply synthetic inputs. Loader calls0; no original/GPU/asset preparation.

Independent authored events (external historical reference) and Python oracle (external historical reference) compute common bits/U32/U8 values, raw/scalar order and compound arithmetic from authored events/exported accepted initial state, without production imports. This is independent output evidence rather than a second scheduler/wire/baseline proof. Prior full338/67, scalar196/40, raw136/28, consumer160, pump171, drain1165/37 and strict/legacy/component/mixed kernels plus inherited audits pass. Build zero warnings/errors. [Checkpoint/reproduction](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R24_EXTENDED_CREATURE_STATE.md). No coverage denominator changes.

Lead next action: register SceneWorldExtendedCreatureScenarios.Run(inputs,peer,exportPath); replace full router with extended router over motion/application ingress, inject accepted extended decoder and pass router.Accept into pump. Keep one raw projector and exact complete-current acknowledgment after projection; render visibility reads current TangibleScalars. Actual extended wire/GPU/name/default/context joining remains separately lead-owned. Worker stops at this milestone.
