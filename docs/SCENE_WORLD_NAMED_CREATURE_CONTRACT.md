# Atomic named CREO3 world state

Round25 Runtime, October3,2026 Eastern. Extends [common/full transaction](SCENE_WORLD_EXTENDED_CREATURE_CONTRACT.md) using frozen Names (external historical reference) and NamedPlan (external historical reference). Display/localization/path interpretation is a separate boundary.

```csharp
bool world.ApplyNamedCreatureDelta(long generation, CreatureView3NamedDeltaPlan plan,
    SceneWorldCustomizationStateStore rawStore);
new SceneWorldNamedCreatureApplicationRouter(world, rawStore,
    Func<ReplicationEnvelope,CreatureView3NamedDeltaPlan> decoder,
    Func<long,ReadOnlyMemory<byte>,bool>? downstream=null,
    int maximumPayloadBytes=65536);
bool router.Accept(long generation, ReadOnlyMemory<byte> message);
```

## Current names, history and compatibility

Field1 contains owned raw NameFile bytes, raw U32 NameSpacer and owned raw NameKey bytes; field2 contains raw UTF16 U16 units, including NUL/unpaired units. No display decoding, path normalization, name fallback, locale cache or original UI policy occurs here. Frozen new updates cap individual strings/units at65536; the frozen named union caps aggregate name payload at1MiB and total1..4096 records. These are modern new-update limits. Names.FromBaseline aliases existing owned immutable fields without applying these caps to older complete baselines. A65537-unit independently authored baseline verifies that separation.

Snapshot.Names and TangibleScalars are current overlays; Baseline remains historic. Every scalar/mixed/components/composite and staged scalar/full/extended/named replacement carries BOTH current overlays explicitly. Genuine baseline Seed resets them to the new baseline. Older ingress cannot drop current names or their variable charge. Raw-only preserves typed state/revision/name identity; names-only advances one typed revision and preserves raw journal/issued receipt/global raw revision. Receipt validity still depends on baseline/incarnation freshness.

## Charge and atomic preparation

Logical retained typed charge is baseline.DecodedBytes plus current wounds/component overlay counts plus Names.AdditionalDecodedBytes(baseline). The frozen Additional method charges current file/key bytes and2*custom-unit count only when each field reference differs from its corresponding historic baseline field. Baseline aliases add0; equal bytes in newly owned storage add their payload size. NameSpacer is already fixed actor accounting. This is per-field historic alias accounting, not arbitrary global reference-graph deduplication; CLR/name-state object/container/GPU/caller retention overhead is excluded. Raw sidecar reference accounting remains separate.

Replication store (external historical reference) uses this charge consistently for seed replacement subtraction, current candidate construction, old mixed/components/composite ingress, staged scalar/named/full/extended preparation and stale retirement. Scalar-only changes preserve the existing charge. Baseline Seed needs no additional name payload because its state aliases its own baseline. Old full/extended preparation delegates to shared named typed preparation with owned wrapper records; work is bounded linear in records plus accepted underlying kernel costs, with no prefix replay.

Named preparation walks input order. Each name update produces private current names, checks its resulting global decoded charge immediately, and rejects before any later replacement/empty name can conceal growth. Common/creature/compound fields use accepted kernels and single-pass count observers; observer fixed bytes include current name charge. Historic baseline and all other actors' charge remain retained. The coordinator (external historical reference) validates new NameFile/NameKey lengths against configured BaselineLimits.MaximumStringBytes and custom units against MaximumUnicodeUnits. It counts name+common+creature scalar attempts under existing scalar limits and union fields under configured mixed/components limits. Repeated/no-op attempts count. Compound enabled mode/policy/element/aggregate limits remain unchanged.

Owner thread, active generation, finalized current creature/complete world-owned seed and exact open raw-source binding precede staging. All raw bounds, world pending capacity, intermediate/aggregate typed bytes, checked global raw/typed/world/mutation increments and current snapshots are preflighted into private candidates before callback-free assignment. Commit contains no external callback/parser/loader/allocation/checked arithmetic/world reentry. Rejected preparation leaves Names/TangibleScalars/scalars/containers/raw journals/issued receipts/global revisions/retained charge/pending world batch unchanged, with no incidental stale pruning. Guards release in finally. Direct ObjectId addresses the current incarnation; older host tokens need host validation. Ordinary validation guarantees exclude process termination/catastrophic runtime failure.

## Injected router

Router (external historical reference) routes exact CREO3 delta envelopes with firstindex0..17. Other families/views/first indices/application messages go downstream, default world.Accept. Injected decoder must consume the entire supported payload transactionally, reject unsupported tails and enforce bounded wire allocation; no unfinished NamedCodec dependency is required here. Outer object ID/raw DeclaredCount must match the frozen plan. DeclaredCount remains metadata, not an invented update denominator.

Accepted24 owner/current generation/source/world-journal/target/typed freshness/caught-reentry purity guards remain. Stale generation rejects before copying/parsing/callbacks. Decoder must be pure; ordinary downstream world mutation is permitted, source mutation/reset/reentry is not. External callback effects cannot be rolled back; resource-only mutations lack a universal coordinator token. Current parse/callback/budget failure closes only the router and releases borrows/callbacks; foreign-thread rejection precedes closure. Router-owned message memory is cleared in finally; downstream may borrow it only during the callback, caller memory is unchanged, and independently owned decoder envelope may be retained.

## Evidence and next join

Native scenarios (external historical reference) pass489 checks/93 cases: raw names/spacer/unpaired ownership, both orderings, historic aliases vs equal bytes/new storage, exact additional charge, names-only receipt/raw-only identity, specialized24/23/22/21 and world scalar/mixed/composite carry, genuine baseline reset, new widths, intermediate shrink/late compound/name-included observer/global multiactor byte caps, combined attempts, existing lifecycle/owner/purity/overflow and4096-command cases. The65537-unit baseline remains accepted/aliased; new65537-unit update rejects. All replacement carry/charge call sites, including direct components ingress, were inspected; direct components-after-name execution is not separately claimed. Correct Runtime14 synthetic peers and accepted standalone compound codecs supply inputs. Loader calls0; no original/GPU/assets preparation.

Independent events (external historical reference) and Python oracle (external historical reference) calculate raw/name/unit/spacer/order and variable charge plus inherited common/compound arithmetic without production imports. This independently checks outputs, not scheduler/wire/baseline execution. Prior extended407/81, full338/67, scalar196/40, raw136/28, consumer160, pump171, drain1165/37 and accepted kernels/inherited audits pass. Build zero warnings/errors. [Checkpoint/reproduction](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R25_NAMED_CREATURE_STATE.md). No coverage denominator changes.

Lead: register SceneWorldNamedCreatureScenarios.Run(inputs,peer,exportPath), inject accepted named decoder into this router above motion ingress, pass router.Accept into pump. Keep one raw projector and exact complete-current acknowledgment after projection; optional name consumer reads current Names through an independently accepted bounded display resolver. Actual named-wire/localization/render join remains lead-owned. Worker stops at this milestone.
