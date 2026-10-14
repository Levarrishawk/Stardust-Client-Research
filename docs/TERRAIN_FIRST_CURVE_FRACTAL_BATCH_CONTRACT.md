# Conditional modern first-curve fractal batch

The [owned batch](../../SWG-Godot-Client/poc/Stardust.Core/Geometry/TerrainFirstCurveFractalBatch.cs) implements one explicitly opted-in modern approximation after a frozen curves-disabled kernel. Every batch and curve sample reports `ModernBinary64LogPowApproximation` and `OriginalNumericParity=false`. This does not admit curved inputs through an existing operational selector or sampler, activate the original layer3, or implement full AHFR. The [recovered curve finish](TERRAIN_FRACTAL_CURVE_FINISH_CONTRACT.md) still has unresolved logarithm, power, control and exceptional-result parity.

## API and input ownership

`TerrainFirstCurveFractalBatch.Build(encoded, width, rows, requests, controls, policy, limits, cancellation)` requires the policy argument. Controls carry separate table, construction and query precision, each24/53/64. The complete rectangle has dimensions1..128, one unique request per row-major cell, at most1024 cells, and finite supplied F32 X/Z coordinates. The default sample cap is256; callers must explicitly raise it for larger batches. Coordinate duplicates across different cells remain legal; repeated cell indices and missing cells reject.

The strict84-byte MFRC/0001 decoder is reused. First flag means the **entire serialized U32 is nonzero**, including256; second must be zero. First parameter must be finite and strictly between0and1; modes0..5, octaves1..16, eligible seed and finite used kernel parameters are required. An unused second-curve parameter may remain nonfinite metadata. No input bytes or request storage are retained by reference.

Two owned raw snapshots and their SHA-256 identities are exposed only through copying methods. Derivation clones the original84 bytes and clears bytes36..39, the first flag word; every other byte is explicitly checked equal. Derived raw input must have both curves disabled. Returned read-only cells preserve the actual kernel result and its retained exact sample, request and modern curve sample. Mutating an input or exported copy cannot alter a completed receipt.

## Numerical boundary and provenance

Exactly one fresh frozen `CreateForSupportedModes` sampler and one full-rectangle query produce actual `ConditionalComputedMiss` results. Every result must have that origin. The mandatory curve base is `KernelResult.Sample.ProjectionBits`, the F32 snapshot of the actual retained miss, promoted exactly to binary64. The retained kernel value is preserved separately; it is never passed directly to the modern power operation.

The modern domain is0<=base<=1. Exponent is host binary64 `Math.Log((double)parameter)/Math.Log(0.5)`. For base0, including negative zero, the output policy is positive zero; for base1 it is exactly1. Interior bases use `Math.Pow(base, exponent)`. All outputs must be finite and in[0,1]. These are deliberate modern semantics. They do not reproduce retained x87 logarithm/exponent operands, original special cases, controls, callbacks, or original cache publication.

`TerrainFirstCurveApproximationSample` owns the base F32 bits, exponent/output binary64 bits and normalized exact unsigned dyadic magnitude/exponent. Its `ToSuppliedSample()` performs an exact conversion through the frozen `FromWideMagnitude`; the resulting `SuppliedWideMiss` denotes caller-supplied data. The surrounding sample's modern policy and false parity must accompany it; converting does not create recovered numeric provenance.

The frozen supplied primitive accepts exponents only within±512. A nonzero modern output whose normalized exact binary64 dyadic exponent is below−512 rejects the **whole batch before publication**, without rounding or replacement. A true `Math.Pow` underflow to positive zero is separately accepted and exports exact zero. This extra modern export guard is not an original-client rejection claim. No curved cache or curve-hit path exists; the temporary kernel cache is disposed before publication.

## Preflight, accounting and failure

Policy/controls/limit ranges, dimensions, complete count, exact input extent and every budget are checked before raw snapshots, result arrays or sampler allocation. For N cells, conservative logical reservations are:

| Quantity | Reservation |
|---|---:|
| Owned copies/receipts |696+176N bytes|
| Route |10088+1632N bytes|
| Work |20424+8289N units|
| Frozen query bytes |1456N|
| Frozen query work |8257N|

Reservations assume16 octaves. Default route/copy/work caps are4MiB/1MiB/16Mi units; hard maxima16MiB/4MiB/64Mi units. Query byte/work caps remain frozen2MiB/16Mi units; input cap default84, hard65536. Actual logical route/work charges for O octaves are10088+N(352+80O) bytes and20424+N(97+512O) units. These are deterministic policy charges, not heap measurements, CPU timings or recovered original allocation sizes. Fixed440 accounts for two84-byte snapshots plus two136-byte decoded owners; receipts/request copies and shared/kernel query bounds are accounted conservatively. Curve work adds32 logical units per cell.

Successful receipts report one factory/query/disposal, N actual misses/curve evaluations, N times O kernel octaves, two source-level logarithm calls, interior power calls and endpoint evaluations. No hit count is reported for a curved cache. Cancellation is checked before copies, during request preflight, in the frozen factory/query, at each curve and before final publication. `using` disposes the sampler on late domain/export failure or cancellation. Failure returns no candidate receipt; an independently completed prior batch remains intact. The batch retains no thread-affine sampler, so completed immutable receipts can be shared.

## Evidence, verification and limits

Observation, **Confirmed implementation**: owned raw derivation, actual retained miss identity, mandatory F32 base, explicit modern operations, normalized exact export and atomic return are inspectable in the source. Interpretation: this is a bounded compatibility experiment for the known first-curve ordering. Consequence: consumers must keep the modern policy and false original-parity label; existing curve guards remain unchanged.

Observation, **Confirmed serialized occurrence**: the one previously pinned layer3 AHFR/family4 MFRC is84 bytes at TGEN offset7062, SHA-256 `77FDAC709B0D3E3113FAAD8A82F4746FDF3969434DC8BF2BAF42273AE89D5FE3`, first enabled/second disabled and finite parameter in(0,1). Its private bytes are used directly, then derived only inside the new batch. This establishes an approximate first-curve sample on real serialized input, not actual layer activation or original output parity.

The [focused runner](../../SWG-Godot-Client/poc/round57-tests/Rendering/Invoke-Curve.ps1), [independent oracle](../../SWG-Godot-Client/poc/round57-tests/Rendering/verify_curve.py) and [native scenarios](../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/TerrainFirstCurveFractalBatchScenarios.cs) use frozen Fraction noise/kernel arithmetic, exact F32 base conversion and100-digit Python Decimal ln/exp. Numerical comparison permits1e−12 relative plus1e−15 absolute error; exact retained/kernel/base/raw identities and exported binary64 dyadics are compared separately. Expected numeric values are never passed to Core. All six modes/all27 precision combinations, parameters/flags/endpoints, malformed bytes, second curve, used nonfinite fields, budgets, normalization/base/export rejection, true underflow, largest shape, cancellation, ownership and prior-result survival are covered. Exact final counts and replay details belong to the [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-04_R57_FIRST_CURVE_FRACTAL_BATCH.md).

Original transcendental/control/special-case parity remains Unknown. The next implementation probe is an explicitly supplied modern batch consumer preserving this provenance; original numerical admission instead requires the separate bounded power-path research already identified in the curve-finish contract. No operational selector is relaxed by this artifact.
