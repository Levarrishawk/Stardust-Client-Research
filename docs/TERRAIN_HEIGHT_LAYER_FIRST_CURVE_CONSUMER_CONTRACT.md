# Explicit modern first-curve height consumer

Round 58, 2026-10-04. [TerrainHeightLayerFirstCurveConsumer](../poc/Stardust.Core/Geometry/TerrainHeightLayerFirstCurveConsumer.cs) applies the frozen retained AHFR consumer to an actual supplied [first-curve batch](TERRAIN_FIRST_CURVE_FRACTAL_BATCH_CONTRACT.md) using actual [filtered influence](TERRAIN_HEIGHT_LAYER_FILTERED_INFLUENCE_CONTRACT.md) products. This is an explicitly modern approximation route, with `ModernBinary64LogPowApproximation` and `OriginalNumericParity=false` retained on the result and borrowed approximation samples.

[Native scenarios](../poc/Stardust.Core.SmokeTests/TerrainHeightLayerFirstCurveConsumerScenarios.cs), [independent verifier](../poc/round58-tests/Assets/peer.py), [isolated replay](../poc/round58-tests/Assets/Invoke-SelectedRule.ps1) and [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-04_R58_HEIGHT_LAYER_FIRST_CURVE_CONSUMER.md) document the bounded behavior. Existing strict operational selectors/samplers remain unchanged.

## API and actual input identity

`Evaluate(filtered, slice, grid, batch, priors, heightPrecision, modernPolicy, limits, token)` requires the same actual locally enabled TypedEvidence slice and grid retained by the filtered result. Same-byte substitute instances reject. The output retains those actual objects, the actual supplied batch and each actual filtered/batch cell.

The selected AHFR must be enabled typed 0003, operation 0..3 and finite scale. Its global rule index, ordinal 4, tag/version, offset, length and SHA must match the slice's height child. Generator/group hashes and extents, layer identity, signed family key and absolute family offset must match the accepted owned receipt. The existing slice factory already proves root/group/family byte ownership; the new consumer does not export encoded source bytes.

The height family's first curve must be enabled by its full raw nonzero flag, second curve disabled and first parameter finite in (0,1). Its **exact MFRC bytes** must match the actual batch's `OriginalSha256`. The frozen raw decoder admits only a fixed 84-byte MFRC/0001 envelope with 13 LE words. The consumer reconstructs that exact envelope and all raw words from the selected immutable Fields on the stack, then hashes it. This establishes complete byte identity, including unused fields/flag bit patterns, without an encoded-input export or caller-supplied hash. The hash is compared with the batch's immutable constructor-computed source hash; the derived curves-disabled source hash is not substituted.

The supplied batch must retain the explicit modern policy and false original parity. Its width/rows must equal the existing Layer grid count and it must contain the complete N row-major cells. Each index/column/row and first/second coordinate bit must match the actual grid pair. Actual kernel-result origin, mandatory F32 base, approximation policy and false parity are checked. A same-source batch with different shape or coordinates rejects.

`Height.HasOperationalInputs` remains false for a curve-enabled family under the existing strict selector. This consumer intentionally uses TypedEvidence plus an explicit modern batch; it does not weaken that selector or imply original numerical admission. Other raw/kernel/curve domain failures are rejected by the frozen batch constructor before this consumer can receive a batch.

## Exact consumer gate and arithmetic

Every prior must be finite and within the explicit modern absolute-height limit before copying. Signed zeros, negative heights and finite positive values are preserved as input bits.

For each cell, the actual stored `filtered.Cells[i].Influence.ProductBits` is the height-consumer operand. Its ordered value must be positive to convert the actual approximation through `ToSuppliedSample`, the frozen exact binary64-dyadic primitive. Negative products and both signed zeros pass a null sample to the frozen retained `TerrainSuppliedHeightFractal.Evaluate`; they preserve the exact prior bits, including signed zero. No boundary/local positivity test replaces the final product gate.

Positive cells call that same frozen evaluator with the actual selected AHFR.Input, prior, product, supplied dyadic and explicit 24/53/64 height precision. Scale multiplication and op0 interpolation/op1 addition/op2 subtraction/op3 multiplication retain their established operation/store order. Finite negative/zero scale is accepted; only operation 0..3 is admitted. Active cells retain the same supplied sample as the consumer receipt; skipped receipts retain null.

The result exposes exact stored height bits, prior/product, actual filtered/batch/approximation references, caller-supplied dyadic provenance and the actual retained consumer receipt. Approximation provenance remains visible around the generic supplied primitive. Neither a `SuppliedWideMiss` label nor exact height arithmetic establishes original curve parity.

## Ownership, counts, limits and publication

The supplied batch has already queried every curve cell. `BorrowedCurveQueries=N` reports that existing work; the new consumer owns **zero** sampler constructions, query batches or disposals. It performs N height-consumer calls, M dyadic conversions/applications and N-M skips. This is not early noise suppression: skipped cells still retain their already computed curve-batch cells. Any validation conversions performed by the borrowed batch are outside the new consumer's conversion counter.

The copied prior snapshot is revalidated before any conversion, preserving the finite/bounded input invariant even if borrowed storage changes during copying. Only the prior array and result cells are newly owned. Prior/cell views are read-only; `CopyPriorHeightBits` and `CopyHeightBits` return detached arrays. Borrowed immutable batch/filter/slice storage is retained rather than copied. Repeated and concurrent calls use independent result storage and preserve completed prior outputs.

Complete square shapes are bounded at axis 32/N=1,024, matching the frozen full-grid sampler/batch. Shape, options, whole-input caps, selected provenance and every prior/request are checked before prior copying or approximation conversion. For N cells, preflight charges 4*N copied prior bytes, 256+192*N logical owned array/receipt bytes and 84+32*N work units. The constant work includes the fixed raw-source identity hash. `CopiedEncodedInputBytes` is zero. These are conservative deterministic policy charges rather than heap measurements or timings; borrowed upstream storage and future caller exports are excluded.

Default copy/array/work caps are 131,072/1,048,576/1,048,576; hard maxima 1,048,576/8,388,608/8,388,608. Axis/cell hard maxima are 32/1,024. Caller options may be zero to restrict work; negative/above-hard options reject. Exact/one-less budgets are fixture-tested.

The modern absolute-height limit is an explicit finite nonnegative F32 bit value, capped at 1,000,000. It applies to all priors, including skipped ones, and every final height. It is a prototype/display limit, not recovered original terrain behavior. A last active height exceeding the limit fails after earlier pure cells without publishing a result. Cancellation checks precede work, bracket all-cell validation and each conversion/application, and precede publication. Pre-cancel rejection is executed; arbitrary mid-consumer cancellation timing is source-reviewed, not claimed as an executed gate.

## Separate numerical verification claims

The verifier makes two distinct claims:

1. Frozen Fraction kernel/F32-base evidence plus a 100-digit Decimal ln/exp model checks actual binary64 curve output and exponent with 1e-12 relative + 1e-15 absolute tolerance. Exact source/request/kernel/base identity, normalized dyadic representation, positive-zero policy and true-underflow classification are checked separately. This does not assert bit identity for Math.Log/Math.Pow.
2. After that independent tolerance check, the exact actual exported binary64 dyadic is the supplied operand of an independent Fraction AHFR model. Per-operation 24/53/64 rounding and final F32 height bits must match exactly. Expected samples are never injected into Core. Exact heights therefore verify consumer arithmetic conditional on the supplied modern value, rather than falsely claiming Decimal/native power bit parity.

One already pinned original layer 3 BCIR+FFRA+AHFR/family occurrence exercises this explicit approximation route using synthetic request/weights/priors/profiles. Its MFRC source SHA is checked independently. This does not establish original layer activation, ancestor traversal, original generated terrain or original height parity.

The next bounded action is an explicitly owned height-output/patch publication using these validated receipts and the same provenance policy. Separately, original numerical admission still requires the unresolved original transcendental/control/special-case evidence; sparse curve-query ownership remains a distinct optimization boundary.
