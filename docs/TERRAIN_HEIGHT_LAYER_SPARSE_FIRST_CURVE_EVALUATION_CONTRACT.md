# Owned sparse first-curve height evaluation

Round60, 2026-10-04. TerrainHeightLayerSparseFirstCurveEvaluation (external historical reference) owns one bounded sparse evaluation over actual [filtered influence](TERRAIN_HEIGHT_LAYER_FILTERED_INFLUENCE_CONTRACT.md). It selects requests, constructs the frozen [sparse batch](TERRAIN_FIRST_CURVE_FRACTAL_SPARSE_BATCH_CONTRACT.md), then invokes the frozen [sparse consumer](TERRAIN_HEIGHT_LAYER_FIRST_CURVE_SPARSE_CONSUMER_CONTRACT.md). It is engine-neutral and requires explicit `ModernBinary64LogPowApproximation`, curve table/construction/query controls and height precision. `OriginalNumericParity=false` remains visible.

## API, selection and actual ownership

`Evaluate(filtered, slice, grid, priors, curveControls, heightPrecision, policy, limits, token)` requires the same actual locally enabled TypedEvidence slice/grid retained by the filtered receipt. It validates the enabled AHFR0003 op0..3/finite scale and exact source child/rule/group/layer/family metadata before materializing requests or exporting source bytes. Existing strict operational curve admission is unchanged.

Full square shapes have axis2..32 and N at most1,024. Every actual filtered cell must retain its actual boundary cell, row-major index, exact finite grid pair and finite stored ProductBits. Every prior must be finite and within the modern absolute-height bound, including skipped cells.

The wrapper counts exactly M positive actual stored F32 products, then materializes only those actual Layer coordinate pairs in deterministic row-major order. It does not accept request indices, arbitrary coordinates or a replacement raw/batch/hash. It snapshots N prior bits, revalidates the owned prior snapshot, constructs M request values, and exports exactly84 selected MFRC bytes via `Raw.CopyEncodedBytes`. That private owned raw snapshot is retained and passed to the actual frozen batch. The actual frozen consumer independently proves complete raw source SHA and exact positive-set identity.

The result retains actual Filtered/Slice/Grid, actual Batch/Consumer, actual CurveControls, actual borrowed FFRA counts, read-only owned request/prior views, and the actual immutable consumer cell view. Active cells retain actual sparse/approximation/sample references; skipped cells retain null and preserve exact prior bits, including negative zero. Copying height/prior/raw exports are detached arrays. Future caller exports are outside evaluation charges.

## Preflight stages and deliberately bounded options

The API exposes aggregate limits instead of arbitrary child limits. Defaults/hard caps are axis32, cells/requests1024, aggregate copy4MiB, aggregate route16MiB, aggregate work64Mi units; conservative owned-array bound defaults1MiB/hard8MiB. The absolute-height F32 bound must be finite in[0,1,000,000], default1,000,000. All numeric limits are nonnegative. Resource limits can reject a large shape even when its cell count fits.

Explicit table/construction/query and height precisions must each be24/53/64. Selected raw first flag must be nonzero in its full U32, second flag zero, first parameter finite in(0,1), mode0..5, octaves1..16, seed other than signed INT_MAX, and all used kernel words finite. The unused second parameter remains metadata. Empty evaluation retains raw structural eligibility without tables/normalization/log/power. For nonempty evaluation this wrapper additionally rejects seed0 and exact amplitude−1/even-octave cancellation as a bounded policy for known child failure cases; it does not change the frozen child APIs.

Shape/options/controls/rule/raw eligibility, every product/coordinate/prior, M, aggregate caps and known child budget viability are checked before owned request/prior arrays, the raw export or sampler construction. Child limits are derived exactly from N/M and fixed hard bounds; no caller child options can defer a known count/budget rejection until after copying.

This is **not an exhaustive numerical global preflight**. Remaining normalization/table viability, scaled-coordinate/octave arithmetic, retained/base domain, modern power/exact-export bounds and final height arithmetic stay in their frozen child stages. Predicting them would duplicate numerical implementation or run a reference sampler. Consequently a bounded valid primitive input may still fail after snapshots or sampler construction. A successfully constructed batch disposes its sampler before returning; curve/query failure also exits its frozen `using` scope. Height failure occurs after that disposal. Only the final complete evaluation is published, and a previously completed receipt remains intact. A failed factory before producing a sampler has no published sampler/result.

## Deterministic own and child accounting

The reservation exposes each component. M=0 keeps raw/prior ownership but has zero owned factory/query/disposal/log/power counts. For M>0 there is exactly one owned frozen sampler factory, one sparse query batch and one disposal.

| Charge | Formula |
|---|---:|
| Own encoded export |84|
| Own request snapshot |16M|
| Own prior snapshot |4N|
| Own copy total |84+16M+4N|
| Own arrays/receipt allowance |340+16M+4N|
| Own conservative work |84+16N+16M|
| Batch logical copy |696+176M|
| Batch route reservation |empty440; otherwise10088+32N+1600M|
| Batch work reservation |empty168; otherwise20424+N+8288M|
| Batch query byte/work bounds |empty0/0; otherwise16N+1440M and N+8256M|
| Consumer prior copy |4N|
| Consumer arrays |256+192N|
| Consumer stack mapping |4N|
| Consumer work |84+32N+16M|

Own array allowance includes84 raw bytes plus256 fixed receipt/scratch allowance and prior/request payloads. Batch logical copy retains the frozen child's declared accounting, including its snapshots/decoded ownership/query data; it is not reported as zero or mistaken for the wrapper's84-byte export.

Aggregate copy is own+batch+consumer, or780+192M+8N. The frozen batch does not expose an array-only charge, so the conservative **OwnedArrayBytes** bound charges own arrays + the entire batch route reservation + consumer arrays. This intentionally overbounds child arrays, including some nonarray route allowance. Aggregate route adds consumer stack mapping to that union. Aggregate work is the sum of own/batch/consumer work; query caps already belong to the child and are not charged twice as extra work/storage.

Actual child declared copy/reservations are checked against the calculated components before publication. `AccountedAggregateRouteBytes` and `AccountedAggregateWork` use the batch's actual-octave logical charges instead of its16-octave reservations. These are deterministic policy charges and conservative bounds, not measured heap usage, timings or recovered original resource sizes. Already borrowed filtered/slice/grid storage and upstream FFRA work are excluded.

Owned sampler factory/query/disposal counts refer to the batch created by this evaluation. `ConsumerCounts` separately reports M borrowed curve queries, N height-consumer calls, M conversions/applications, N−M skips and zero consumer-owned samplers. `BorrowedFilterCounts` exposes the actual frozen upstream FFRA receipt separately. Test-only explicit reference routes are excluded from production counters.

## Failure, validation and compatibility consequences

Cancellation is checked before snapshots, during primitive scans/prior revalidation/request construction, by both frozen children, and immediately before publication. Executed tests cover pre-cancel, zero/one-less caps, a late nonfinite prior rejected before heavy allocation, late curve export failure, final height bound/overflow and preservation of a completed prior receipt. Mid-loop timing-driven cancellation and concurrent mutation during copying are source-reviewed invariants, not executed tests. Completed outputs retain no sampler or thread-affine live object; repeat and parallel calls own separate children and storage.

Native scenarios (external historical reference), independent verifier (external historical reference) and offline replay (external historical reference) check every successful evaluation against the explicit row-major frozen sparse batch+consumer route. This equivalence is not a full-grid curve-batch execution claim. Fraction independently verifies products/kernel/F32 bases and conditional AHFR;100-digit Decimal separately checks modern curve ln/exp with1e-12 relative plus1e-15 absolute tolerance. Exact height assertions use actual independently tolerance-checked binary64 dyadics, never native/Decimal power bit equality or injected expected samples.

**Observation, Confirmed implementation:** exact selection/actual child references, whole-input primitive/resource rejection, declared copy charges, immutable outputs and row-major explicit-route equality. **Interpretation:** an owned bounded modern evaluation suitable for a later engine adapter. **Consequence:** preserve modern provenance/false parity and the distinction between preflight and later numerical rejection.

Only the already pinned original layer3 BCIR/FFRA/AHFR/MFRC occurrence is used, with authored caller grids/weights/priors/profiles. Original words/names/bulk output remain ignored private data. The [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-04_R60_OWNED_SPARSE_HEIGHT_EVALUATION.md) records identities, mechanical results and remaining uncertainty. Original power/control behavior, original recursive activation and full-terrain parity remain Unknown.
