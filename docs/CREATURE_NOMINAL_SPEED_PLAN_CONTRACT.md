# Atomic supplied nominal target and transition plan

`CreatureNominalSpeedPlan.Build(initialCurrentBits, steps, precision, limits, cancellationToken)` composes the frozen [supplied target](CREATURE_NOMINAL_TARGET_SPEED_CONTRACT.md) and [nominal transition](CREATURE_NOMINAL_SPEED_TRANSITION_CONTRACT.md) APIs. This is an engine-neutral compatibility prototype using supplied values. It does not recover physical getters, engine movement, original arithmetic controls, or complete locomotion.

## Owned inputs and result

`CreatureNominalSpeedPlanStep` is a readonly value containing run and walk F32 words, the raw run byte, the three ordered direction words, a signed dyadic factor, finite caller seconds and an optional signed dyadic nominal rate. One nearest-even precision profile (24, 53 or 64 bits) applies to the entire plan. Initial current must be finite, including for an empty plan.

Each immutable `CreatureNominalSpeedPlanReceipt` retains the copied `Source`, `CurrentBeforeBits`, the actual frozen `Target` result and the actual frozen `Transition` result. The next step consumes `Transition.StoredBits`, including a raw target clamp; it does not consume a recomputed or retained intermediate candidate. `Steps` wraps a private receipt array in a read-only collection. No caller array, getter, controller, driver or callback is retained.

An empty valid plan preserves the initial raw bits, including negative zero, and reports zero evaluations, work and copy reservation. Every option is still validated. Missing rates are legal for equality; an ordered transition without a rate rejects the complete plan. Negative seconds, signed zero, negative speeds and signed factors follow the frozen finite supplied-input contracts.

## Preflight and atomic publication

Before copying or any BigInteger arithmetic, primitive preflight validates precision, initial current, option ranges, count, logical copy budget, every finite getter/direction/seconds input, factor/rate profiles and aggregate conservative work. Factor and rate magnitudes are unsigned 64-bit values with a separate sign and exponents bounded to ±512.

Default caps are 4,096 steps, 4,194,304 logical copy bytes and 268,435,456 logical work units. Hard caps are 65,536 steps, 67,108,864 copy units and 4,294,967,296 work units. Copy accounting reserves 640 units per step; this is a stable policy, not measured CLR allocation. Work reserves each frozen target primitive requirement plus the frozen transition hard bound of 8,192 per step. The bound is reserved even when the computed transition later proves equal and needs zero transition work. `ActualTargetWork` and `ActualTransitionWork` report the sums of the actual producer receipts, not elapsed time or instruction counts.

After primitive preflight, the API copies the steps and revalidates that owned snapshot before computing targets. It requires identical reservation totals. Callers must not mutate the source during the call: equal reservation totals cannot establish an atomic snapshot of concurrently changing multiword values. Post-call source mutation cannot alter the owned result.

Targets and transitions are evaluated sequentially into a private array. A late finite-projection overflow, missing rate, rejected input or cancellation publishes no partial plan. Previously returned immutable plans remain usable. Cancellation is checked before preflight, during preflight, before copying, at each step, inside the frozen producers and immediately before publication. The deterministic scenarios exercise pre-cancellation; timed cancellation during arithmetic is not claimed as an executed test.

Counts distinguish target evaluations, transition evaluations, rates actually used, equality branches and clamps. Target F32 stores are twice the step count. Transition F32 stores count ordered candidate stores plus additional clamp stores; equality has none. These are nominal operation counts, not recovered runtime getter calls.

## Evidence and validation

Observation: the source calls the accepted frozen target and transition APIs and stores their actual result objects. Interpretation: serial composition preserves their established arithmetic and raw store boundaries. Confidence: Confirmed by source inspection and independent deterministic fixtures. Consequence: adapters may consume the resulting values while preserving each receipt and the current-bit chain; they need not reproduce legacy memory layouts.

The standalone [oracle](../poc/round58-tests/Runtime/verify_plan.py) composes independent Fraction/isqrt models from rounds 57 and 56. It compares every retained fraction, target word, candidate word, branch, clamp, work receipt and aggregate against the native plan. Its 343 synthetic cases include 322 accepted plans and 2,312 accepted steps, 20 input rejections and one cancellation. Tests cover all three precisions, retained intermediate underflow/store witnesses, retained candidate clamps, varying targets/directions/factors/rates/seconds, signed zero, equality without a rate, exact and one-below count/copy/work caps, and nine late failures. Native scenarios verify copied sources, read-only collections, actual producer receipts, prior-result preservation, 100 repetitions and 64 independent parallel calls.

Atomic owned planning and resource caps are modern implementation policy, not recovered original behavior. Physical speed semantics, SI units, dynamic getter/factor provenance, original precision controls, receiver lifetime, vector/root-motion effects and downstream simulation remain Unknown. See the [bounded checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-04_R58_NOMINAL_SPEED_PLAN.md).
