# CREO view 3 wounds delta contract

`CreatureView3WoundsDeltaCodec` decodes and encodes only CREO view 3 field index 17. `CreatureView3WoundsState.FromBaseline` seeds the object ID, raw counter and ordered S32 entries from the accepted complete baseline. This milestone provides an engine-neutral immutable plan and atomic state application. It does not bind the plan into the world coordinator or scalar store.

The application envelope is the accepted [replication envelope](REPLICATION_ENVELOPE_CODEC_CONTRACT.md): U16 operand count 5, U32 delta opcode `0x12862153`, U64 object ID, U32 numeric tag `0x4352454F`, U8 view 3, U32 payload size and U16 declared field count. Its outer header is 23 bytes; server framing adds four transport bytes. The declared field count is retained without driving traversal. The bounded opaque payload is a sequence of field-index/update pairs, including repeated field 17 updates in wire order. Empty payload is accepted as a no-op. Any other field index rejects the whole plan; general or mixed CREO deltas are not supported.

Every field update is `U16 index=17 | U32 commandCount | U32 incomingCounter | commands`. All multibyte values are little endian. Exactly commandCount commands must be decoded before the next field or end.

| U8 command | Wire body | Modern application |
|---|---|---|
| 0 remove | U16 index | Require index less than current size, then remove |
| 1 insert | U16 index, S32 value | Require index at most size and capacity available, then insert |
| 2 replace | U16 index, S32 value | Require index less than size, then replace |
| 3 assign | U16 value count, S32 values | Replace the whole vector with the ordered sequence |
| 4 clear | None | Empty the vector |

Values remain raw signed integers. There is no HAM clamping, deduplication or inferred fixed wound count. Empty assignments, duplicate values, negative integers, U16 count/index maxima and high U32 counters are representable. The active server applies its own gameplay validation before emitting wounds; synthetic negative values test structure rather than server gameplay.

## Evidence and confidence

Observation: [replication schema](REPLICATION_SCHEMA_MAP.md#vector-delta-operations) records RTTI `AutoDeltaVector<int, CreatureObject>`, snapshot `0043B050` and delta `0043B1A0`, with all five command bodies, count/counter framing and stale-prefix consumption. Field registration places the integer vector at CREO:3 index 17. Interpretation: the inherited integer-vector grammar supplies this field's structural decoder. Confidence: Confirmed in the existing evidence record. No new VA investigation was performed.

The [September 11 checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-11_BLOCK2.md#malformed-payload-behavior) records bounded reads, unchecked field lookup and in-place partial legacy mutation. Atomic application and unsupported-index rejection are deliberate modern policies.

Server correlation (sibling Stardust/Core3 tree): `MMOCoreORB/src/server/zone/objects/creature/CreatureObjectImplementation.cpp::setWounds`, lines 1385-1386, emits field `0x11` through `wounds.set`. `scene/variables/DeltaVector.h::set` lines 136-150 emits command 2/U16 index/S32 value, `add` lines 157-173 emits command 1, `remove` lines 190-205 emits command 0, and `removeAll` lines 208-216 emits command 4. Counter updates and `startList` are explicit there. Generic server source does not emit command 3; the original client evidence establishes it as tolerated compatibility grammar. Server source is correlation, not proof of client execution.

## Counter policy and intentional divergences

The durable record confirms skipping a represented prefix but leaves exact overflow behavior Unknown. This implementation uses an explicitly modern modulo interval policy, not a recovered original formula. For each update, let `N` be command count and `C` incoming counter. Its start counter is `(C - N) mod 2^32`. Let `S = (localCounter - start) mod 2^32`. Require `S <= N`; skip precisely S complete parsed commands and apply the remaining suffix. Advance to C only after this update succeeds. N is capped by a nonnegative signed-int command budget, so the interval is shorter than half the U32 counter range.

This accepts fresh contiguous updates, overlapping prefixes, duplicates at the end counter and bounded wraparound intervals. It rejects gaps before the interval and counters beyond its end, including wholly older packets. Zero-command fields require the exact local counter. This is stricter than broad legacy stale-packet acceptance; the original rule outside the represented interval remains Unknown. Skipped commands still undergo full structural parsing and budgets, but their indices are not checked against current state since they are not applied.

Legacy remove ignores invalid indices; insert/replace may grow. Modern apply rejects invalid remove/replace and insert gaps, allowing insertion only at an existing position or append. It creates no implicit zero-filled gaps. Limits apply to transient states, so a too-large assignment rejects even when a later clear would make the final state small. Object ID must match the initial state. These policies must be reviewed against original counter/growth behavior before claiming full interoperability.

## Ownership, limits and verification

States, updates and command-value lists own defensive copies or read-only wrappers over privately owned arrays. Decode completes all commands and fields before returning a plan. Apply copies the initial state, processes all fields, and returns a new owned state only when all checks pass; any exception leaves the input unchanged. There are no callbacks, sockets, threads, retained native handles or external mutations.

Explicit limits bound application payload bytes, field updates, aggregate commands, aggregate decoded values and transient state elements. Payload budget includes the U16 declared field count. Aggregate commands/values apply across repeated fields, including skipped commands. Counts and remaining wire bytes are checked before allocation. Element limits bound logical entries rather than total CLR heap/capacity overhead. Encoding independently checks payload/field/command/value budgets; the decoded plan retains its apply element cap. It writes fields from structured commands, rather than replaying borrowed wire bytes.

The isolated runner `poc/round9-tests/Networking` can reproduce verification without shared-runner edits. Python independently hand-packs messages, compares native encoded bytes at declared counts 0 and 65535, checks every exported command and verifies fifteen application results, including a real baseline-codec seed with synthetic input. Native tests additionally cover every truncation, unknown commands, partial/tailing/mixed fields, wrong envelope/schema, impossible counts, budgets, wraparound, repeated fields, rollback after a late failure, growth rejection, maximum U16 assignments/indices and immutable ownership.

Next probe: recover exact original integer-vector counter arithmetic and growth/ignore behavior from the documented baseline before relaxing the modern policy, then integrate wounds with stable creature incarnation and existing scalar state in one atomic store transaction. This contract adds no original-runtime, live-server or research-coverage claim.

## Round10 recovery update

The strict policy above is unchanged. Direct pinned instructions now resolve integer-vector count/counter/growth mechanics in the opt-in [LegacyBounded kernel](CREATURE_WOUNDS_LEGACY_POLICY_CONTRACT.md), including parser assignment cursor semantics. Earlier unknown arithmetic refers to Round9's evidence state. Next integrate the selected kernel with mixed scalar/wounds plans and incarnation-bound world state; callbacks and unsafe recovery remain separate.

## Round11 integration —2026-10-02

Round11 [mixed scalar/wounds plans](CREATURE_VIEW3_MIXED_DELTA_CONTRACT.md) preserve received interleaving/order and selected strict/legacy grammar with whole-plan immutable application. World binding follows separately.
