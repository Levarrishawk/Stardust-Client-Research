# Conditional modern sparse first-curve batch

The [sparse batch](../poc/Stardust.Core/Geometry/TerrainFirstCurveFractalSparseBatch.cs) queries only an explicitly supplied unique subset of a bounded full shape. It requires the frozen `ModernBinary64LogPowApproximation` policy, always reports `OriginalNumericParity=false`, and reuses the frozen [approximation sample and exact export boundary](TERRAIN_FIRST_CURVE_FRACTAL_BATCH_CONTRACT.md). Existing full-batch, sampler and operational-selector behavior remains unchanged. No curved cache, recovered curve-hit behavior, original layer activation or full AHFR is claimed.

## API, ordering and ownership

`TerrainFirstCurveFractalSparseBatch.Build(encoded, width, rows, requests, controls, policy, limits, cancellation)` requires policy explicitly. Controls are the frozen `TerrainFirstCurveFractalControls`, with independent table/construction/query24/53/64 precisions. `TerrainFirstCurveFractalSparseLimits` separates `MaximumCells` (default/hard1024) from `MaximumRequests` (default256/hard1024). Dimensions1..128 and full shape C=width times rows must fit the cell cap; supplied request count M may range0..C and must fit the request cap.

Each request has explicit row/column and finite F32 X/Z bits. Arbitrary order and coordinate duplicates at different cell indices are valid. Duplicate cell indices reject even when their coordinates differ; no cache reuse or silent deduplication occurs. A fixed128-byte stack bitset checks uniqueness before copies and again on the owned request snapshot. Returned cells preserve caller order with `RequestIndex`0..M−1, full-shape `Index=row*width+column`, exact request, actual kernel result and modern approximation sample. Omitting cells requires no coordinates for them.

Strict84-byte MFRC/0001 ownership/derivation matches the frozen full batch: entire first U32 flag nonzero, second zero, first parameter finite in(0,1), mode0..5, octaves1..16, seed except signed INT_MAX, and finite used frequency/amplitude/scale/offset words. Unused second parameter may remain nonfinite metadata. The original snapshot is cloned; only bytes36..39 are cleared. Every other byte is checked equal, the derived raw is decoded and both derived curve flags must be disabled. Original/derived SHA-256 and copying exports survive input mutation. The receipt/cells contain no borrowed raw/request storage or retained sampler.

## Empty and nonempty behavior

An empty subset still checks controls/policy/caps/full shape, raw envelope/version, owned raw flags/parameter domain and used kernel options, owns the original/derived bytes and returns an immutable empty receipt. Sampler factory, query, disposal, computed miss, octave, curve, logarithm, power and endpoint counts are all zero. No kernel cache, table, normalization or exponent is constructed. Consequently raw eligibility is established, but runtime-only table/normalization viability is **unevaluated**: for example seed0 or a zero normalization sum can have an empty metadata receipt but cannot thereby obtain a nonempty sample. Empty metadata is not a promise that a future nonempty batch will succeed.

For M>0, exactly one fresh frozen curves-disabled sampler owns the full C-cell shape. Exactly one query receives the supplied subset, in supplied order; every result must be an actual `ConditionalComputedMiss`. Unique indices prevent cache reload. The mandatory modern base is the actual retained sample's `ProjectionBits` F32 snapshot; retained kernel data stays separate. No omitted cell is queried by the sparse production path.

Exponent is host binary64 `Math.Log((double)parameter)/Math.Log(0.5)`. Base must be finite in[0,1]; zero, including negative zero, maps to positive zero, one maps to exact one, and interior values use `Math.Pow`. Output must be finite in[0,1]. The frozen `TerrainFirstCurveApproximationSample` preserves binary64 bits, modern policy and false parity, normalizes the exact dyadic and rejects nonzero export exponents outside±512. Genuine modern power underflow to zero is separately accepted. `ToSuppliedSample()` exports exactly as caller-supplied `SuppliedWideMiss`; the modern wrapper must accompany that conversion. Original retained-ln/x87/control/special-case/cache parity remains Unknown.

## Preflight and deterministic accounting

All count/shape/limit/precision/policy/input-extent/resource bounds precede byte snapshots, request/result arrays and sampler construction. Reservations assume16 octaves and distinguish full cache shape C from supplied subset M:

| Quantity |M>0|M=0|
|---|---:|---:|
| Logical owned-copy bytes |696+176M|696|
| Route bytes |10088+32C+1600M|440|
| Work units |20424+C+8288M|168|
| Frozen query bytes |16C+1440M|0|
| Frozen query work |C+8256M|0|

Actual logical route/work charges for O octaves are10088+32C+M(320+80O) bytes and20424+C+M(96+512O) units. Empty retains only its440-byte raw ownership charge and168 raw-decode work units. Fixed696 copy reservation includes two84-byte snapshots/two136-byte decoded owners plus256 fixed receipt/scratch allowance. The32C route term covers full cache plus query candidate; query storage is16C plus subset result/octave records. These are declared logical policy charges, not heap measurements, timings or recovered original resource sizes.

Input/copy/route/work/query defaults and hard limits match the frozen modern full batch (input84/65536; copy1/4MiB; route4/16MiB; work16/64Mi units; query2MiB/16Mi units). Empty may use zero query caps; it still must cover raw/copy charges. Increasing C while holding M fixed affects nonempty cache reservations, not curve evaluation count. For complete subsets, reservations and actual charges equal the frozen full batch's formulas.

Successful nonempty counts are one factory/query/disposal, M actual misses/curve evaluations, M times O octaves, two source-level logarithm calls, interior power calls and endpoint evaluations. Cancellation is checked before copies, throughout borrowed/owned request validation, by frozen factory/query, at each curve and before publication. `using` disposes the temporary sampler before final return or on failure. Late kernel/domain/export failure publishes no candidate and leaves an independently completed prior receipt intact. No thread-affine live object remains in a completed receipt.

## Evidence and validation

**Observation, Confirmed implementation:** owned exact derivation, arbitrary-order uniqueness, subset-only query, mandatory actual F32 base, frozen approximation/export wrapper and empty branch before numerical work are directly inspectable. **Interpretation:** a bounded modern sparse numerical experiment. **Consequence:** callers preserve policy/false parity and cannot infer original or operational admission from a sparse receipt.

Only the previously pinned original layer3/family4 MFRC occurrence is used: TGEN offset7062/length84, SHA-256 `77FDAC709B0D3E3113FAAD8A82F4746FDF3969434DC8BF2BAF42273AE89D5FE3`, first enabled/second disabled and finite first parameter in(0,1). Four supplied coordinates produce modern approximate outputs, with no new corpus survey or original activation. Raw words/names remain private.

The [own runner](../poc/round58-tests/Rendering/Invoke-Sparse.ps1) builds actual Core offline, runs [native scenarios](../poc/Stardust.Core.SmokeTests/TerrainFirstCurveFractalSparseBatchScenarios.cs), and compares the [independent oracle](../poc/round58-tests/Rendering/verify_sparse.py). Frozen Fraction arithmetic independently proves retained kernel/F32 bases;100-digit Decimal ln/exp checks modern exponent/output with1e−12 relative plus1e−15 absolute tolerance. Exact actual binary64 dyadic conversions are audited separately. Expected numerical values are never injected into Core.

Native tests independently compare every accepted nonempty sparse output against the frozen full batch at the same coordinates, populating omitted reference coordinates with authored zero inputs. These extra full-reference factories/queries belong to the test, not sparse receipt accounting. Reversed/shuffled/sparse/complete/empty cases cover all six modes/all27 profiles, full-word flags, endpoint/underflow/export/domain behavior, malformed/unsupported/nonfinite inputs, caps, ownership/mutation/cancellation and late prior-result preservation. The [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-04_R58_FIRST_CURVE_FRACTAL_SPARSE_BATCH.md) records final mechanical counts and scope. The next bounded consumer must preserve this modern provenance; original numerical parity needs the separate unresolved power/control research.
