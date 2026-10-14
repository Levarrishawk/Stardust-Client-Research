# Sparse modern first-curve height consumer

Round 59, 2026-10-04. TerrainHeightLayerFirstCurveSparseConsumer (external historical reference) joins actual [filtered influence](TERRAIN_HEIGHT_LAYER_FILTERED_INFLUENCE_CONTRACT.md) to the accepted [sparse first-curve batch](TERRAIN_FIRST_CURVE_FRACTAL_SPARSE_BATCH_CONTRACT.md). It preserves the mandatory `ModernBinary64LogPowApproximation` policy and `OriginalNumericParity=false`. It consumes an already completed batch and creates no sampler.

## API and exact identity

`Evaluate(filtered, slice, grid, sparseBatch, priors, heightPrecision, policy, limits, token)` requires the same actual selected slice/grid retained by the filtered receipt. The slice must be locally enabled TypedEvidence. Its selected AHFR must be enabled typed 0003, operation 0..3 and finite scale. Child ordinal/global rule index, tag/version, offsets, lengths, source SHA, generator/group/layer identity and signed family key/extent must match the accepted owned receipts.

Full raw first-curve flags remain meaningful: first nonzero, second zero, first parameter finite in (0,1). The selected family's entire exact MFRC/0001 SHA must equal the actual batch's immutable `OriginalSha256`. As in the accepted [full consumer](TERRAIN_HEIGHT_LAYER_FIRST_CURVE_CONSUMER_CONTRACT.md), the consumer reconstructs the canonical 84-byte envelope and all thirteen immutable raw words on the stack and hashes them. It does not export or copy encoded source bytes, accept a caller hash, or substitute the derived curves-disabled hash. Unused raw words and full flag bit patterns participate in identity.

The full Layer grid is square, axis 2..32 and N at most 1,024. Sparse batch width/rows must match. M may be 0..N. Every actual sparse cell must have its supplied-order RequestIndex, unique full-grid Index, exact row/column and exact F32 coordinate pair. Actual computed-miss kernel provenance, mandatory F32 base, modern approximation policy and false parity are checked.

The required request set is **exactly every and only positive actual stored filtered ProductBits**. Missing positives, extra nonpositives, wrong shape/coordinates/source and substitute slice/grid objects reject. Supplied order may be arbitrary. A bounded stack map links each grid index to its actual sparse cell; outputs always contain N cells in row-major order. A positive cell retains the actual sparse cell and approximation references. A skipped cell has null Curve/Approximation/Supplied. All cells retain the actual filtered cell and returned retained-consumer value receipt; active receipts retain the exact supplied sample reference.

## Numerical gate, empty route and equivalence

All N prior heights must be finite and within the explicit modern absolute-height bound, including skipped cells. Both signed zeros and negative finite values are valid. The owned copied prior snapshot is revalidated before conversion.

For M positive products, the consumer converts the actual approximation's exported binary64 value to its exact dyadic using the frozen primitive, then calls the frozen retained AHFR evaluator with actual AHFR.Input, prior/product bits and explicit 24/53/64 height precision. It preserves the established scale and operation/store order. Other cells pass null and preserve every prior bit, including negative zero. Final outputs must be finite and bounded; a late output overflow or bound failure publishes no result.

An eligible empty sparse batch is accepted when every product is nonpositive. Its actual factory/query/disposal/log/power counts must all be zero. The consumer still performs N null-sample height calls and validates every prior and identity. Raw eligibility remains distinct from runtime-only kernel viability: the frozen empty batch deliberately does not construct tables, normalization or an exponent. No original activation or future nonempty success is implied.

When the frozen full and sparse routes are both numerically eligible, native tests require exact binary64 equality for each requested curve and exact N-cell conditional height equality. Sparse omission can avoid an invalid export at a blocked cell. Five fixtures explicitly cannot construct the full reference: three order variants with an omitted export-boundary failure, and two empty cases with runtime-only kernel viability unevaluated. Those fixtures retain independent sparse/height validation but make no full-batch execution claim. Existing operational curve guards remain unchanged.

## Ownership, limits and counts

`BorrowedCurveQueries=M`, `ConsumerCalls=N`, `DyadicConversions=HeightApplications=M`, `SkippedCells=N-M`. Owned sampler constructions/query batches/disposals are all zero. Upstream sparse batch ownership, validation conversions and numerical work remain its own receipt. Test-only full-reference factories are excluded from production counts.

Limits default to axis32/cells1024, copied-prior bytes131072, logical array bytes1048576, work1048576, mapping bytes4096 and absolute-height F32 limit1,000,000. Hard caps are axis32/cells1024, copy1048576, arrays/work8388608 and mapping4096; limits must be nonnegative. Absolute bound must be finite in [0,1,000,000].

For N full cells and M borrowed requests, complete preflight reserves:

| Charge | Formula |
|---|---:|
| Copied prior bytes |4N|
| Encoded input copies |0|
| Owned arrays/receipt bytes |256+192N|
| Temporary stack mapping bytes |4N|
| Conservative work units |84+32N+16M|

These are deterministic policy charges, not measured heap sizes, timings or recovered original resource sizes. Shape/options/identity/count and copy/mapping/array/work budgets precede owned prior/result arrays and dyadic conversion. The stack map is bounded by the already validated N. All borrowed cell requests/products/priors are validated before copying. Borrowed immutable upstream storage and future copying exports are excluded. Prior/cell views are read-only; copying exports are detached. Repeated/concurrent evaluation owns independent output storage.

Cancellation is checked before preflight work, in request/product/prior validation, after the copy, during every height cell and before final publication. Executed tests cover pre-cancel and preservation of a completed prior receipt after cancellation, cap rejection, late output-bound failure and late F32 overflow. Mid-loop timing-driven cancellation and concurrent mutation during a copy are source-reviewed invariants, not executed tests. No partial candidate escapes; completed receipts retain no sampler or thread-affine live object.

## Evidence and validation

**Observation, Confirmed implementation:** actual identity/set matching, immutable row-major output, null skips, exact dyadic conversion and distinct M/N counters are established by source inspection and native scenarios. **Interpretation:** a bounded modern sparse height experiment. **Consequence:** approximation provenance must accompany these heights; exact conditional height arithmetic does not establish original transcendental or terrain parity.

The native scenarios (external historical reference), independent verifier (external historical reference) and offline replay (external historical reference) exercise all four operations, three height precisions, 27 curve profiles, six modes, inversions, arbitrary request order, exact-set negatives, caps, nonfinite/skipped priors, late failures and ownership. Fraction independently verifies filtered products, retained kernel/F32 bases and AHFR arithmetic. The curve is checked separately against 100-digit Decimal ln/exp with 1e-12 relative plus 1e-15 absolute tolerance. The exact height oracle uses the **actual exported binary64 dyadic after that independent tolerance check**; it never asserts native/Decimal power bit equality or injects expected samples into Core.

Only the already pinned original layer3 BCIR/FFRA/AHFR/MFRC occurrence is read, using authored caller grids/weights/priors/profiles. Original source words/names and detailed output remain ignored private data. The [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-04_R59_SPARSE_FIRST_CURVE_HEIGHT_CONSUMER.md) records mechanical results, input identities and corrected test failures. Original logarithm/power/control behavior, recursive layer activation and full-terrain numerical parity remain Unknown.
