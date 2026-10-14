# Supplied local-current speed-reader arithmetic

`CreatureSuppliedCurrentSpeedReader.Evaluate(baseBits, firstModifierSnapshotBits, secondModifierBits, policy, precision, limits, cancellationToken)` implements one pure supplied reader invocation. The mandatory policy is `CreatureSuppliedCurrentSpeedPolicy.SuppliedLocalOnly`; zero, unknown and other policies reject. Nearest-even precision is explicitly 24, 53 or 64 bits. This API does not select an original branch or query game state.

## Evidence and operation order

The [movement writer contract](CREATURE_MOVEMENT_WRITER_CONTRACT.md) establishes the complete run reader `004388A0` and walk reader `00438950`. Both select a base, call `00438A70`, spill the first modifier to F32, call `00438A20`, multiply that second returned modifier by the spilled first modifier, then multiply by the selected base. Run sites are `0043892D/32/37/3C/40`; walk sites are `004389F5/FA/FF`, `00438A04/09`. The caller at `00629F7E/8A` stores the two returned reader values at `00629F83/8F`. Existing static anchors (external historical reference) support this already accepted observation; no new broad executable audit is part of this prototype.

`baseBits` supplies the selected local base: current run storage correlates to field7/+674, and walk to field11/+660. `firstModifierSnapshotBits` supplies the first query's already spilled word, correlated to local field4/+64C. `secondModifierBits` supplies the second query's F32 local word, correlated to field5/+638. These correlations identify the supplied local-current boundary. They do not assert that the original recursive getters always return these local fields. The mandatory policy deliberately provides the local-only context; it does not infer gameplay predicate values or substitute template, global, vehicle or related-object values.

The exact implemented graph is:

1. `retainedModifiers = round_p(secondModifier * firstModifierSnapshot)`.
2. `retainedSpeed = round_p(retainedModifiers * base)`.
3. `returnBits = storeF32(retainedSpeed)`.

Both multiplications use the existing engine-neutral retained arithmetic. There is no F32 projection/reload between them and no regrouping through `first * base`. The first F32 spill belongs to the supplied input boundary; it is not an additional store performed by this API. Negative finite words, signed zero and subnormal words are accepted. Intermediate underflow or overflow relative to F32 is allowed because that product remains retained. Only a nonfinite final F32 projection rejects. This finite-input and final finite-return policy is modern bounded behavior; original exception and control-state parity remain Unknown.

Observation: accepted executable dataflow and caller stores fix the operation order. Interpretation: supplying the local F32 operands isolates that complete arithmetic finish for implementation. Confidence: Confirmed order and field correlation; Unknown original effective gameplay context, physical units, original arithmetic controls, recursion and receiver/global lifetime. Consequence: preserve the graph and supply provenance explicitly; do not present this API as physical speed or full movement parity.

## Preflight, ownership and publication

`GetRequiredReservations` validates the token, mandatory policy, precision, all option ranges and all three finite F32 words before BigInteger work or result construction. It reserves 128 logical value-copy units and conservative work:

`64 + 8 * (sum(operandSignificandWidth + abs(operandExponent)) + precision + 8)`.

The primitive decomposition uses width zero for zero, width at most 24, and exponent -149 for subnormals or encoded exponent minus150 for normals. Hard/default work cap is 8,192; hard/default copy cap is128. Each finite triple's work bound is at most4,792. These are explicit logical accounting units, not CLR allocation bytes, runtime instruction counts or recovered original limits. Exact and one-below caller caps are deterministic acceptance boundaries. Empty or skipped invocation semantics do not exist: each call independently performs three supplied-word reads, two multiplications and one caller F32 store.

`CreatureSuppliedCurrentSpeedResult` owns raw source words, precision, the actual `RetainedModifierProduct` and `RetainedSpeed`, `ReturnBits`, reservations, policy/provenance and counters through get-only properties. The source words and retained values are immutable values; no caller memory, controller, getter, driver, callback or external resource is retained. Separate run and walk calls return distinct receipts, each with its own two multiplications and caller store. Modifier work is never silently shared.

Cancellation is checked before primitive planning, before arithmetic, between products and immediately before result publication. Any rejected input, late final-projection overflow or cancellation returns no result. Previous results stay usable. Deterministic pre-cancellation is tested; timed mid-arithmetic cancellation is inspection-only.

## Validation and use boundary

The independent Fraction oracle (external historical reference) uses the accepted nearest-even arithmetic model and compares every retained fraction, final word, source word, reservation, policy and counter. Its2,763 synthetic cases produce2,575 accepted results,187 input rejections and one cancellation. There are290 retained-versus-reloaded witnesses,125 grouping witnesses, three underflow rescues, three intermediate-overflow rescues,563 negative-zero returns, two exact/two one-below cap cases and three late final overflows. A last-operand NaN rejects even when the preceding supplied words are zero. Native checks cover2,575 ownership results, immutable public properties, prior-result preservation,100 repetitions and64 independent parallel invocations. Separate run/walk receipts report four total multiplications and two caller stores.

The API does not modify the frozen target, transition or plan implementations and does not wire a host or world. A future adapter may derive supplied words from finalized current receipts under an explicitly authorized local-only context, invoke run and walk independently, and pass their actual returned words to the frozen target/plan. Effective original predicates, source substitution, recursion, units, original controls, borrowed-pointer lifetime and full locomotion require separate evidence closure. See the [bounded checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-04_R59_SUPPLIED_CURRENT_SPEED_READER.md).
