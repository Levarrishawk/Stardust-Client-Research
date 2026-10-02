# Atomic CREO view 3 mixed scalar/wounds delta

The mixed codec supports only CREO:3 indices 11-17. It parses one bounded application envelope into an owned ordered plan, then applies every update before returning a new combined state. Fields 0-10 and any other index reject the entire plan. This is not an eighteen-field delta implementation, store/world integration or a live interoperability claim.

## Runtime12 API

All types are in `Stardust.Core.Protocol`. Public entry points:

```csharp
public sealed record CreatureView3MixedDeltaLimits(
    int MaximumPayloadBytes, int MaximumFields, int MaximumScalarUpdates,
    int MaximumWoundsCommands, int MaximumWoundsValues, int MaximumWoundsElements);

CreatureView3MixedState.FromBaseline(CreatureView3BaselineSnapshot baseline,
    int maximumWoundsElements);
new CreatureView3MixedState(ulong objectId, CreatureView3ScalarState scalars,
    CreatureView3WoundsState wounds);

CreatureView3MixedDeltaCodec.Decode(ReadOnlySpan<byte> message,
    CreatureView3MixedDeltaLimits limits,
    CreatureWoundsApplicationPolicy policy = CreatureWoundsApplicationPolicy.StrictInterval);
CreatureView3MixedDeltaCodec.Decode(ReplicationEnvelope envelope,
    CreatureView3MixedDeltaLimits limits,
    CreatureWoundsApplicationPolicy policy = CreatureWoundsApplicationPolicy.StrictInterval);
CreatureView3MixedDeltaCodec.Encode(CreatureView3MixedDeltaPlan plan,
    CreatureView3MixedDeltaLimits limits);
plan.Apply(CreatureView3MixedState initial);
```

Decode returns `CreatureView3MixedDeltaPlan`; Encode returns `byte[]`; FromBaseline and Apply return `CreatureView3MixedState`. The state exposes ObjectId, Scalars and Wounds. The plan exposes ObjectId, DeclaredCount, Policy, MaximumWoundsElements and read-only Updates. Each `CreatureView3MixedUpdate` exposes Index, nullable Scalar (`CreatureView3ScalarUpdate`) and nullable Wounds (`CreatureWoundsUpdate`); exactly one is populated. Plans/updates cannot be publicly constructed or mutated. Existing immutable scalar/wounds types are reused.

Runtime12 should seed from the accepted complete baseline, select one wounds policy before Decode, call Apply against the matching current incarnation's combined state, then publish the complete returned state once. The plan validates object ID but contains no registry/store/generation token. Freshness and publication belong to the caller's future transaction. Do not independently publish successful prefix scalars before applying wounds.

## Wire and evidence

The accepted [envelope](REPLICATION_ENVELOPE_CODEC_CONTRACT.md) supplies delta opcode `0x12862153`, numeric CREO tag `0x4352454F`, view3, U64 ID, exact payload size and raw U16 declared field count. Application outer header is 23 bytes; payload includes the two-byte declared count. Declared count does not drive traversal. Read field-index/value pairs until exact bounded exhaustion; duplicate fields remain in received order, including scalar-wounds-scalar interleaving and repeated wounds fields.

[Scalar contract](CREATURE_VIEW3_SCALAR_DELTA_CONTRACT.md): indices11/12 are U8 posture/rank,13 U64 link,14 F32 height,15 U32 shock bits,16 U64 states. All multibyte scalars are little endian. Finite signed-zero/nonunit height and high raw bits are retained. Nonfinite height rejects. Immutable scalar update classes and the accepted scalar plan application are reused.

[Wounds contract](CREATURE_VIEW3_WOUNDS_DELTA_CONTRACT.md) and [instruction-backed legacy policy](CREATURE_WOUNDS_LEGACY_POLICY_CONTRACT.md) define index17: U32 loop bound/counter plus commands0remove,1insert,2replace,3assign,4clear; U16 index/count and S32 values. StrictInterval interprets each command as one loop unit and uses its modern interval/index policy. LegacyBounded preserves assign's k+1 loop units, raw loop bounds/overshoot, unsigned skip clamp, mutation counters and bounded zero-fill growth. Selection affects parsing as well as application and is retained by the plan. Encode preserves the same raw bounds; use the same policy when decoding its output. No wire policy discriminator is invented.

Observation: the accepted codecs/contracts establish these bytes and the R10 decoder/helper branches. Interpretation: their ordered structural updates can be composed into one transaction using the same immutable types and application kernels. Confidence: Confirmed inherited wire/legacy-kernel evidence and fixture-backed modern composition; original downstream callbacks, packet ordering guarantees and outer recovery remain outside this implementation. No new executable-address research, function names or protocol coverage counts.

## Atomicity, ownership and limits

Decode uses one envelope-owned byte array and directly builds existing immutable scalar/wounds update objects. It does not synthesize or copy an envelope for each field. A small local reader must express the inherited scalar widths and wounds framing because existing codec readers are private; it performs no independent reinterpretation. Differential native fixtures compare all five parsed wounds commands to the accepted codec for both policies. Existing codec files remain unchanged.

Apply invokes accepted scalar/wounds application plans in wire order against private temporary immutable states. Only the final mixed wrapper returns on success. Late counter/index/growth exceptions cannot change the input state or publish a partial result. Per-field adapter arrays/lists have one update each; wound state copies are bounded by the configured element and field caps. There are no callbacks, retained native handles, worker threads, sockets or external mutation. Seeded scalar records and wounds states are already immutable/owned; baseline seeding creates an owned wounds snapshot.

Limits bound payload bytes, total field occurrences, scalar occurrences, aggregate raw wounds loop bounds, aggregate decoded S32 wounds values and transient wounds elements. Counts are checked against remaining budgets and bytes before allocation. Skipped commands still consume structural/value budgets. Legacy final-assign overshoot is separately bounded by value/element limits. Apply enforces its retained element cap; Encode enforces wire/field/scalar/command/value caps and does not change the plan's application policy or element cap. Limits are logical quantities, not total CLR heap overhead. Repeated wounds application may copy bounded state per field; no unbounded per-field synthetic wire allocation occurs.

## Verification and next probe

Native tests cover both policies, all scalar types and five wounds commands, repeated/interleaved fields, raw count0/1/65535, signed zero/high bits, exact limits and each exceeded cap, late counter/growth/unsupported/nonfinite/truncation failures, wrong object/envelope/schema, ownership and empty plans. Independent Python hand-packing compares encoded bytes and every combined output field for seventeen cases. It seeds through the accepted complete baseline codec and reuses the separately authored strict and instruction-derived legacy arithmetic oracles. Wrong-policy cases detect both legacy assignment parsing failure and strict-packet legacy counter/state divergence.

Next probe: add this plan/state API to stable-incarnation storage and world staging with one receipt/atomic transaction, then drive a bounded state presentation change. No store/world files are changed here. Original notification callbacks and broader delta views remain separate.
