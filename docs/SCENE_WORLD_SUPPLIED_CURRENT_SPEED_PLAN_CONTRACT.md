# Current paired-speed plan composition

`SceneWorldSuppliedCurrentSpeedPlan` borrows an existing [paired current-speed adapter](SCENE_WORLD_SUPPLIED_CURRENT_SPEED_INPUTS_CONTRACT.md). Construct it with that adapter and optional bounded limits. `Build(pair, initialCurrentBits, steps, cancellationToken)` requires an actual paired receipt issued by that borrowed adapter and returns an immutable `SceneWorldSuppliedCurrentSpeedPlanReceipt`, or null for a stale/foreign pair. No current reader is reloaded; one exact paired receipt supplies the entire plan.

## Inputs and exact composition

`SceneWorldSuppliedCurrentSpeedPlanStep` is a readonly value containing only `RunFlag`, ordered `Direction`, signed dyadic `Factor`, finite `SecondsBits` and optional signed dyadic `SuppliedRate`. It exposes no run/walk words or precision override. Every mapped child step uses the actual `pair.Run.ReturnBits`, `pair.Walk.ReturnBits` and the pair's mandatory `SuppliedLocalOnly` nearest-even24/53/64 precision. Negative finite values, signed zero and subnormals follow the frozen supplied-input contracts.

The implementation checks the borrowed adapter's actual `IsCurrent(pair)`, primitive-preflights all caller steps and aggregate limits, copies/revalidates the owned steps, maps them into bounded child inputs and invokes the actual frozen [atomic speed plan](CREATURE_NOMINAL_SPEED_PLAN_CONTRACT.md). That child preserves each actual target and transition receipt and advances through `Transition.StoredBits`. The composition does not approximate those receipts or recompute the paired getters. Both source freshness and cancellation are checked after the child completes and before publication.

The result retains the exact `Pair` and actual `Plan` objects. `Pair` exposes the actual view, fields7/11/4/5 and independent run/walk reader results; `Plan` exposes its actual step receipts, retained arithmetic, branches, clamps, counters and reservations. The wrapper owns a read-only collection of captured caller steps and exposes generation/incarnation/root pin, pair precision/policy/provenance, initial/final current, logical accounting and child counters. `ReaderReloads` is always zero. The pair's previously performed two reader calls remain its own counts and are not charged as new composition work.

Observation: executable-established supplied operation graphs live in the frozen producers; this new adapter directly preserves their actual input/result objects. Interpretation: a host can consume one current pair and one atomic plan without copying run/walk injection logic into its own engine adapter. Confidence: Confirmed source and independent deterministic fixtures. Consequence: use actual `Plan` receipts and wrapper currency, while retaining the supplied-local-only context. Original predicates, recursion/global/vehicle substitutions, units, controls, receiver lifetime and physical/full-locomotion parity remain Unknown.

## Bounded reservation before copies

Hard/default step cap is128. Option ranges are0..128 steps,0..112,928 output units,0..110,592 copy units and0..9,445,504 work units. Construction validates ranges and requires the fixed output header and own-work charge before retaining the borrowed adapter. The owned policy is copied by value.

For `n` steps:

| Charge | Logical units |
| --- | --- |
| Paired reference/header output | 2,336 = existing pair output2,080 + composition header256 |
| Owned caller step copy | 96n |
| Mapped child-step array | 128n |
| Actual child-plan copy reservation | 640n |
| Total copy | 864n |
| Total output | 2,336 + 864n |
| Own work | 128 + 64n |
| Child work | Sum of frozen target primitive requirements + 8,192n |
| Total reserved work | Own work + child work |

Each step's seconds/rate profile and frozen target direction/factor/speed inputs are validated before any source or mapped array copy or BigInteger arithmetic. Magnitudes are already bounded unsigned64 values by their public types; factor/rate exponents retain the frozen ±512 limits. Both child and aggregate reservations are determined before copying. Exact child limits are passed to `CreatureNominalSpeedPlan.Build`, and the returned actual child copy/work reservations must equal preflight. The result exposes own copy/work, child reserved copy/work and actual child arithmetic work separately. Charges are modern deterministic logical accounting, not CLR heap measurements or original instruction costs. An empty valid plan reserves2,336 output, zero copy and128 own work; it preserves initial raw bits, including negative zero, and performs zero target/transition evaluations.

Captured caller steps are revalidated before mapping/arithmetic; reservation totals must remain equal to initial preflight. Callers must not mutate source memory concurrently during Build. Equal totals do not prove a coherent snapshot under same-budget concurrent multiword changes. Mutation after Build cannot alter the owned caller steps or frozen child sources.

## Currency, errors and ownership

Every operation enforces the constructing thread and rejects reentry. `IsCurrent(result)` requires this composition's issuing identity and the actual borrowed adapter's currency for `result.Pair`. Foreign composition receipts return false even when they share the same adapter/pair. Source changes stale receipts as the frozen adapter dictates; independent motion-only publication preserves input freshness. Historical pair/plan data remain immutable after invalidation.

Constructor range/header errors, null pair arguments, disposed composition, cancellation and owner/reentry misuse throw. The step-count cap is checked before borrowed freshness. After that, stale/foreign/disposed-borrowed-adapter pairs return null before inspecting remaining malformed step data. Current paired input with invalid primitive values, insufficient caps, late target/candidate overflow or an ordered transition without a rate throws without publishing a wrapper or partial plan. Missing rates remain legal for computed equality. Cancellation is checked before source currency, during both preflights/mapping and child work, and immediately before return. Only deterministic pre-cancellation is executed by the tests; timed mid-build cancellation/source mutation are inspection-only.

Dispose is idempotent on the owner thread and clears only the composition's borrowed adapter reference. It does not dispose the pair adapter, deeper current reader, world or motion source. The adapter stores no previous result. Previously returned receipts survive late failures, source staleness and composition disposal as immutable historical data.

## Verification and resume

The [independent verifier](../../SWG-Godot-Client/poc/round61-tests/Runtime/verify_composition.py) derives raw-current provenance from20 synthetic authored profiles and schema-correct legacy-framed received messages, computes both paired reader returns with Fraction arithmetic, then feeds those independently derived words into the target Fraction/isqrt and transition models. Native result words never seed expected outputs. It compares exact pair fields/origins and reader products, every child retained value/store/branch, current chain, counts and all wrapper/child reservations.

All597 cases agree:426 accepted plans covering2,273 steps,35 composition rejections,15 rejected pair inputs,120 unavailable pairs and one cancellation. Tests include all three precisions, empty/single/60-step plans, accepted128 and rejected129 steps, four exact/four one-below limit pairs, retained-intermediate store witnesses,15 late failures and equality without a rate. Native10,549 assertions verify exact object identity, owned caller inputs, immutable collections, no source mutation, foreign pair/composition, source update, motion-only freshness, owner-thread/reentry/disposal,100 repeated builds and prior-result preservation. Reentry uses test-only private busy-flag injection; production has no reflection or callbacks.

The initial oracle mistakenly routed composition copy/work caps into the paired-reader stage. Separating stage-specific options fixed the model; the native run passed unchanged and the complete independent rerun passed. See the [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-05_R61_CURRENT_SPEED_PLAN.md). Next action is lead hash review and independent replay, then direct host consumption of actual `result.Plan`; no further round is authorized by this milestone.
