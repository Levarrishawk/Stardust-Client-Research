# Supplied current-height FHGT layer sequence

TerrainSuppliedHeightFilteredSequence (external historical reference) provides a bounded, engine-neutral **modern supplied flat sequence**. Each layer queries its enabled FHGT filters against the height left by earlier layers, then delegates combination and AHCN mutation to the accepted frozen evaluators. It accepts explicit nearest-even 24/53/64 query, combination, incoming-product and affector precisions. Original active controls remain Unknown. The [Round 43 checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R43_HEIGHT_FILTERED_SEQUENCE.md) records reproduction and acceptance evidence.

## Evidence and interpretation

The [selected original layer-order investigation](TERRAIN_HEIGHT_LAYER_ORDER_CONTRACT.md)
confirms fresh preparation and shared-height parent/sibling write-before-read
ordering for one pinned FHGT path. That path uses AHFR and preceding FFRA,
with no AHCN in the examined prefix. This supplied flat FHGT/AHCN evaluator
therefore remains synthetic; original fractal values and derived caches are open.

Addresses are VAs for executable SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Static verifiers check identity before instruction reads. The [FHGT contract](TERRAIN_FILTER_HEIGHT_CONTRACT.md), [layer influence contract](TERRAIN_LAYER_INFLUENCE_CONTRACT.md), [height composition contract](TERRAIN_SUPPLIED_LAYER_HEIGHT_CONTRACT.md) and [flat sequence contract](TERRAIN_SUPPLIED_LAYER_SEQUENCE_CONTRACT.md) remain the recovered boundaries.

| Observation | Interpretation / confidence | Implementation consequence |
| --- | --- | --- |
| FHGT query `00B77739/3C` reads the supplied state's +18 grid; `00B7774B..5A` obtains row-major current cell data. | Query-time cell read, Confirmed. | Bind each query when its layer executes, using the current raw height word. |
| Filter call `00B640B8` and later affector call `00B6418C` receive the same state; AHCN `00B7657B/86` obtains its +18 grid/data. | This layer filters precede its affectors and share the mutable grid, Confirmed. Visibility of prior supplied writes is Strongly inferred for ordered shared-state traversal. | Query before this layer's AHCN loop; pass each completed layer's final height to the next supplied layer. Full original traversal remains Unknown. |
| FHGT's span/query graph has no F32 spill; filter selector/candidate storage is at `00B642B0` / `00B640E9`. | Retained query and unspilled comparison, Confirmed for finite explicit arithmetic. | The new carrier retains `Number`, not the inspection projection. Shape at the combination profile, then use the frozen combine/min/store policy. |
| Exact synthetic native receipts match an independent Fraction oracle; deliberately stale and projected variants change final heights. | Modern bounded composition is Confirmed for the tested domain. | This establishes implementation readiness for this slice, not full terrain parity, corpus reachability or function/protocol coverage. |

## Public API and ownership

`TerrainHeightFilteredLayer.Create(boundaries, filters, rules, incomingWeightBits, boundaryGate, filterGate, invertBoundary, invertFilter, queryPrecision, combinePrecision, productPrecision, affectorPrecision, maximumQueries=256, maximumRules=256)` snapshots supplied boundary entries, decoded FHGT input references and AHCN rule references. The referenced inputs and carriers are immutable. B+F must fit the descriptor's query cap; rules use the frozen owned height template and operation charge. FHGT must have decoded header and fields; opaque versions reject. Numeric fields may remain nonfinite until a reached query uses them.

`TerrainHeightFilteredPlan.Create(initialHeightBits, layers, maximumLayers=64)` snapshots ordered layer references. `Evaluate(plan, limits)` returns an immutable plan/stage/final-height receipt. `EvaluateBatch(plans, limits)` snapshots the caller's plan references after complete preflight and returns results only if all plans succeed. `CopyHeightBits()` returns independent storage. Later failure cannot change prior inputs or receipts. Empty plans preserve the initial word, including nonfinite words and signed zero; no active query or affector is invented.

The sole existing production edit appends `HeightFilter=4` to `TerrainLayerInfluenceQuerySource`, adds nullable `HeightFilter` provenance and `FromHeightFilter(TerrainHeightFilterQuery)`. It keeps the query reference, precision and retained arithmetic value. `ProjectionBits` remains inspection; `RawBits` is null. Existing source values 0..3 and factory behavior are unchanged. Null factory input throws `ArgumentNullException`.

## Lazy binding and ordered execution

For each stage, a boundary-only frozen evaluation uses **zero incoming weight** and a disabled filter gate to discover the post-boundary accumulator and `BoundaryPositive`. This modern prepass cannot overflow from the supplied incoming weight. Repeated boundary evaluation is explicitly reserved.

Each original FHGT position receives a `TerrainHeightFilterBinding` with index, state, nullable original query receipt and nullable shaped candidate word. State priority is Disabled, BoundaryGateFailed, FilterGateDisabled, EarlierZeroExit, otherwise Queried. Nonzero IHDR enabled permits a query. A failed post-inversion boundary-positive gate, nonpositive filter gate or earlier exact numeric zero prevents it. Disabled numeric fields are not inspected. Selector defaults to zero for serialized versions 0000/0001.

A reached filter queries the stage's **current height before any rules in that stage**, wraps the retained value, shapes at `CombinePrecision`, stores the candidate, and compares the unspilled candidate against the F32 accumulator. Exact zero stops later queries before filter inversion; filter inversion remains the frozen evaluator's later step. The scheduler duplicates only this control dependency. A supplied filter can therefore reduce a large incoming product before the actual product is formed.

Reached entries and reached disabled positions form a bound filter prefix; unqueried positions do not receive fabricated query carriers. The complete original descriptor and all original binding positions stay in the outer receipt. The child receipt's `Sample.Filters` describes this bound prefix, not the original full filter list. Its visits describe the actual frozen child traversal. This distinction is intentional modern provenance, including when no filter traversal occurs.

The full frozen layer evaluation then uses the original incoming weight, gates, inversions and explicit combination/product profiles. The frozen AHCN evaluator applies ordered rules using `AffectorPrecision`; the next stage receives its final F32 height. All recovery, eligibility, unknown-operation, disabled-rule and finite-projection behavior stays at those frozen boundaries.

## Complete reservations and failure

`TerrainHeightFilteredLimits` contains the accepted `TerrainSuppliedLayerSequenceLimits` plus `MaximumQueryWork` (default 196608, hard 3145728) and `MaximumCandidateSlots` (default 589824, hard 9437184). Sequence defaults/hard maxima and rule operation charges remain those in the flat contract. Descriptor query/rule caps and plan layer caps have hard maximum 4096.

Before execution/result arrays or any query, preflight scans every supplied plan and reserves total layers, B+F queries, rules, operations, per-plan/per-layer caps, rule references `2*R+M` (M is the largest rule list), abstract query work `2*B+3*F`, and conservative logical candidate slots `9*(B+F)`. Charges include disabled, gated and unreachable entries. Work reserves two boundary passes and FHGT query plus scheduling shape plus frozen shape. It is an operation-group cap, not instruction count. Candidate slots bound logical entries, bindings, carriers and visits conservatively; they are not measured heap bytes. Fixed wrappers, immutable source bytes and garbage awaiting collection are outside this logical count. Rule transfers and frozen snapshots retain the separate rule-reference charge.

Configuration validation reuses the frozen empty-batch validator and may allocate fixed empty wrappers. Single evaluation uses a one-element preflight array. These fixed control allocations precede execution reservations; no unbounded execution buffer is allocated there. Null arguments throw `ArgumentNullException`; invalid profile/option ranges throw `ArgumentOutOfRangeException`; insufficient reservations, unavailable decoded input and reached invalid numeric work throw `InvalidDataException`. No partial batch is published, external handles are absent, and deterministic garbage-collection timing is not promised.

## Verification and remaining scope

The offline runner (external historical reference) builds the actual Core project reference and native scenarios (external historical reference), then compares complete results with the independent Fraction verifier (external historical reference). Final replay: 1141 new cases, 1088 accepted and 53 rejected, 50 ownership/cap/atomicity checks; all 6354 frozen carrier cases and 53 ownership checks still pass. There are 407 stale-initial-height receipt witnesses (326 change final height) and 87 premature-projection receipt witnesses (22 change final height). All four profiles are crossed independently. Fixtures are explicitly synthetic; they cover old FHGT versions, selectors, gates/inversions, unused nonfinite inputs, early zero, exact/one-less reservations, batch aggregates, aliases, empty plans and late failure.

Static checks reverify the accepted layer graph, 90 FHGT instruction anchors plus five current-height/caller anchors and 18 finite-status cases, and AHCN's process/caller/store graph. Actual separate Core/SmokeTests also builds with zero warnings/errors. Mutated and truncated executable identities reject before decoder loading or fixture/receipt creation. No original executable, server, network, GPU or generated terrain evaluation runs.

Unknown: original active floating controls, original complete generator traversal/initialization, recursion, inherited parent gating, other filter families, cache invalidation and real generated-grid parity. This modern supplied flat API does not expand those claims. Next probe: trace one supported real ordered LAYR sequence's FHGT query and preceding AHCN write through shared grid state, including parent enablement and cache initialization, before any full-generator admission.

## Round43 integration

The lead native geometry join (external historical reference) independently passes nine profiles/3600 samples/144 patches/576 queries, including758 stale-initial influence witnesses. This join is synthetic and native-only; the GPU host still uses the accepted41 supplied three-rule terrain.

## Round45 accepted compatibility increment

Follow-up: [actual prototype host host](SCENE_WORLD_HEIGHT_FILTERED_TERRAIN_PREVIEW_CONTRACT.md). The original unresolved boundaries above remain explicit.
