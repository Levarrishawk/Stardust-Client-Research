# Bounded supplied retained fractal samples

Date: 2026-10-04. This implements a modern immutable supplied binary value and conditional AHFR/FFRA overloads. It consumes the confirmed [wide miss versus F32 cache-hit boundary](TERRAIN_FRACTAL_SAMPLER_CONTRACT.md), without generating noise, resolving family IDs or implementing an original cache. [Checkpoint and reproduction](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-04_R47_RETAINED_FRACTAL_SAMPLE.md).

## Value and provenance

`TerrainSuppliedFractalSample.FromWide(long signedSignificand, int exponent, bool negativeZero=false)` represents exactly `signedSignificand * 2^exponent`. Its signed significand is bounded by the primitive Int64 type; exponent is restricted to [-512,512]. Negative-zero metadata is accepted only with zero significand. These primitive checks precede BigInteger construction or shifts. The exact nearest-even F32 overflow boundary is compared using primitive integers and rejected before BigInteger work. This representation is explicitly modern supplied data, not native x87 encoding and not proof of a sampled original value.

Round48 adds `FromWideMagnitude(ulong magnitude, bool negative, int exponent)` for the full unsigned64 magnitude with explicit sign. It preserves the same exponent[-512,512], primitive finite-F32 overflow preflight, signed zero, SuppliedWideMiss provenance and32-byte logical accounting. The exact internal Number is built from U64 before applying sign; no positive U64 is wrapped or narrowed through Int64.

Inspection now exposes `Magnitude` and `IsNegative`. `Significand` is nullable Int64: old signed-factory values remain the same exact numeric values, negative magnitude2^63 returns Int64.MinValue, and values outside the signed range return null. This changes the C# public property signature from long to long? and can require source changes in callers assigning directly to long; universal source compatibility is not claimed. Callers needing a full value use magnitude/sign, and callers requiring signed inspection check the nullable result. No getter throws during JSON serialization. NegativeZero remains separate from signed inspection of numeric zero. Existing supplied-F32/signed factory call signatures, numeric results, receipts and frozen regressions remain preserved; the unsigned factory adds no wider exponent or work budget. The [raw input milestone](TERRAIN_FRACTAL_RAW_INPUT_CONTRACT.md) records its independent U64 boundary tests.

`FromF32(bits)` and `FromCacheF32(bits)` require finite raw F32 before arithmetic. Both preserve signed zero, but carry distinct SuppliedF32 and CacheReloadF32 provenance. `FromWide` carries SuppliedWideMiss provenance. `ToCacheReload()` creates a separate immutable sample from the wide sample's exact F32 projection. Provenance records the caller's assertion; no cache event or noise source is verified by these factories.

All properties have getters only. The owned internal Number is exact at construction. `ProjectionBits` is an inspection/cache snapshot; consumers do not substitute it for the wide operand. No active precision is chosen by the value factory. Consumer profiles remain explicit nearest-even24,53 or64 and use frozen TerrainCircleArithmetic.Number. Original precision control, rounding mode, exceptions and noise remain Unknown.

## Consumer boundary

| API | Gate and arithmetic | Owned result |
| --- | --- | --- |
| AHFR Evaluate with nullable sample | Typed input/precision first; disabled and nonpositive influence return before sample inspection. Active path requires sample and finite height/scale, multiplies sample Value by scale at selected precision and retains it through the recovered height graph until final F32 store. | TerrainRetainedHeightFractalResult contains the existing consumer receipt plus the actual immutable sample; skipped receipts contain null sample and zero inspection word. |
| FFRA Evaluate with nullable sample | Disabled path skips sample inspection. Active path requires finite range/scale/amount and sample, multiplies its Value then performs mandatory F32 scale store before range/query arithmetic. | TerrainRetainedFractalFilterResult and query retain the sample. Existing SuppliedSampleBits is its inspection projection, not the arithmetic input. |
| FFRA Query with sample | Explicit query remains active regardless of serialized enable flag, as with the original supplied-F32 Query API. | Retained query value continues into Shape without premature query projection. |
| FromRetainedFractalFilter(query) | Requires a query with an explicitly owned retained sample; delegates to existing retained query carrier. | Carrier exposes the same immutable sample reference and logical accounted sample bytes. It does not regenerate or project the query before shaping. |

The existing F32 overload signatures, results, finite/gated-unused-NaN behavior and work accounting are preserved. Their frozen receipts remain equal. The new FromRetainedFractalFilters batch reserves two work tokens and40 logical sample/reference bytes per query before snapshots/carrier allocation, rejects late unowned queries atomically, and preserves the copied input references. The scalar retained carrier factory applies the same single-query policy. Existing layer executor accounting is unchanged.

The new overloads are opt-in; no driver, engine executor or smoke dispatch was changed. AHFR operation4 still validates used operands and computes its target before returning unchanged height. Overflow of an active final height or FFRA scale F32 store rejects publication under modern finite policy.

## Bounded batch policy

New TerrainRetainedFractalLimits defaults: 65536 samples,393216 work tokens,3145728 logical snapshot bytes. Hard bounds:1048576 samples,6291456 work tokens,50331648 snapshot bytes. Negative or oversized policies reject. AHFR reserves6 work tokens and48 bytes per input slot; FFRA reserves3 active work tokens (zero if disabled) and40 bytes per slot. Each slot charges32 logical bytes for a retained sample,8 for its immutable reference snapshot, plus8 AHFR height/influence bytes. Null/skipped slots are conservatively charged. Logical byte accounting is not a CLR heap-size promise.

Count/length/work/byte checks precede array snapshots and candidate result allocation. Input arrays are copied; samples can be shared because they are immutable. A late invalid null or overflow throws before the batch is returned, preserving supplied arrays and earlier published results. There is no externally visible partial batch. Empty batches and disabled paths retain the same aggregate policy ordering.

The exponent window also bounds arithmetic work: sample significands have at most64 bits, serialized/height/influence operands at most24, and the consumer graphs have fixed operation depth. AHFR has at most three multiplications along a sample-dependent path before the final addition; alignment spans remain below2048 bits even for signed-zero exponent endpoints. FFRA projects the product to finite F32 before all filter comparisons/divisions. Thus fixed operation tokens do not admit arbitrary exponent-driven BigInteger shifts. These are modern bounds; no original exponent acceptance policy is inferred.

## Evidence and verification

Observation: sampler FST at013E8074 retains pre-store ST0 on a miss, whereas hit FLD at013E7C83 reloads F32. AHFR00B768EE retains its scaled result; FFRA00B77C24 stores it F32. Confidence: Confirmed static storage boundaries for pinned executable SHA-256 E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150.

The independent Python Fraction oracle generates fixtures before native execution and compares full receipts. All7762 cases pass, including143 rejected cases;255 AHFR output,15 FFRA unit-scale and150 FFRA general-scale miss/hit witnesses. The unit-scale witnesses use synthetic supplied values with a nearest53 double-rounding boundary; the original selected FFRA's scale word is one, but no original sample value is claimed. At nearest64 the same exact supplied value's unit-scale miss/hit F32 stores agree. General-scale witnesses independently retain the multiplication-before-store distinction.

Native ownership/budget tests pass13 ownership,21 budget/late-rejection and18 factory-negative checks. Frozen45 AHFR151 raw/8836 consumer, FFRA152 raw/2326 query, carrier6354 and filtered1141 cases pass, as do the frozen46 sampler research and supplied join. Detailed final results and corrected failures are recorded in the checkpoint. Static checks do not establish original sample parity.

Next boundary: close original seed/table/interpolation and optional postprocessing, controls and family lifetime before admitting original-valued noise samples. The new supplied representation is usable independently of those unknowns.

## Round47 compatibility increment

The [786-case composed join](TERRAIN_RETAINED_FRACTAL_CONSUMER_JOIN_CONTRACT.md) passes independently, including nine supplied unit-scale double-rounding witnesses. The current public factory is explicitly Int64-significand-bounded; general positive64-bit computed significands can exceed that range. Expanding its supplied representation is a separate prerequisite for a general computed64-bit noise result, rather than silently narrowing it.
