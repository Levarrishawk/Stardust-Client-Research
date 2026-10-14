# Conditional mode0 octave sampler and owned cache

Date:2026-10-04. The sampler (external historical reference) composes the actual frozen [MFRC/0001 raw decoder](TERRAIN_FRACTAL_RAW_INPUT_CONTRACT.md), [seeded kernel](TERRAIN_SEEDED_NOISE_KERNEL_CONTRACT.md) and [full-U64 sample](TERRAIN_RETAINED_FRACTAL_SAMPLE_CONTRACT.md) into a bounded fresh mode0 miss/cache graph. It is conditional on caller-supplied arithmetic profiles; original generated-sample parity is unavailable.

## Evidence and operational subset

The [Round46 sampler evidence](TERRAIN_FRACTAL_SAMPLER_CONTRACT.md) establishes coordinate/offset/octave spills, the retained normalization loop, mode0 finish and inclusive cache key. [Round47 noise evidence](TERRAIN_FRACTAL_SEED_NOISE_CONTRACT.md) establishes table construction and immediate interpolation. [Round48 computed joins](TERRAIN_COMPUTED_NOISE_CONSUMER_JOIN_CONTRACT.md) establish the actual kernel/U64/consumer bridge. Observation: the native implementation agrees with an independent Fraction composition of those graphs. Confidence: Confirmed under explicit nearest-even profiles; no active original control default is inferred.

`Create` takes an already decoded immutable raw input, fixed width/rows, separate table and construction/normalization precision24/53/64, explicit byte/work budgets and cancellation. `Query` separately takes query precision24/53/64 and exact caller-supplied column/row/F32 coordinates. All arithmetic is software nearest-even; there is no host floating-point pow, random service or FPU-state dependency.

Modern eligibility requires mode0, both raw curve flags disabled, explicit sine-disabled fresh state,1..16 octaves, dimensions1..128 and at most1024 cells. Mode1 shares the recovered body but is deliberately outside this API's operational subset. Unsupported modes/flags/sine reject before normalization/table construction. Frequency, amplitude, coordinate scales and offsets must be finite. Disabled curve-parameter words are not evaluated: NaN/Infinity there can remain raw metadata even when `HasFiniteParameters` is false. Seed eligibility and zero-gradient rejection follow the existing kernel. The raw input and its bytes are never mutated.

## Normalization and miss stores

At `013E6DD0`, accumulate a retained weight sum starting at zero and weight one. Each iteration adds weight under construction precision and multiplies the retained weight by the raw amplitude multiplier; there is no F32 weight spill. Store the final sum F32 at `013E6E06`, reload it, reciprocate under construction precision at `013E6E0C`, then store the normalization F32 at `013E6E12`. Zero or nonfinite stored sums/reciprocals reject modern construction. Frequency does not participate in this sum. Explicit rebuild computes normalization from the complete immutable input, rather than reproducing mutable setter sequencing or constructor shared-global RNG side effects.

On miss, scale first then second raw coordinate and store each F32. Add second then first offsets and store each F32 (`013E7CF5/CFE`). Initialize frequency/amplitude to F32 one and sum to F32 zero. For each octave:

1. Store frequency-scaled second and first coordinates F32 (`013E7D0C/16`) and call the actual owned kernel.
2. Preserve its retained unsigned64 dyadic result through `FromWideMagnitude`; multiply amplitude, add the prior stored sum, and store the new sum F32 (`013E7D1F..25`).
3. Multiply/store frequency then amplitude F32 (`013E7D28..37`), including the final iteration's updates.

Finish with retained `R((R(R(sum * normalization) + 1)) * half)` under query precision, where half is exact F32 one-half. The common finish caches the F32 projection while returning the retained pre-store value; no store/reload is introduced into the returned miss. Modern eligibility requires every recovered F32 store to remain finite, including the last unused multiplier updates. Original masked overflow/trap/fallback behavior is not modeled.

## Cache and returned origin

Cache shape is fixed; there is no inferred resize, map reload, request-wide invalidation or family-pointer lifetime. The key is the original unscaled F32 coordinate pair at the exact caller cell. Occupied cells hit when both coordinates lie in inclusive `R(cached - epsilon)..R(cached + epsilon)` intervals; epsilon is F32 bits `3727C5AC`. Endpoints are retained arithmetic, not F32 spills. Hits do not change keys. Hit samples use the existing `FromCacheF32` factory.

`Result.Origin` distinguishes `ConditionalComputedMiss` from `CacheReloadF32`. The nested sample uses the existing supplied-wide representation on a computed miss for consumer compatibility; its `SuppliedWideMiss` label does not identify an original observed runtime sample. This wrapper's origin is the computation provenance. Misses return full unsigned64 magnitude/sign/exponent; hits return the F32 reload. Both preserve the finite projection and immutable sample after later cache changes/disposal.

A batch clones a candidate cache, processes requests in order (so a repeated same-cell point can hit within the batch), and commits only after all queries, cancellation and result-collection construction succeed. Late failure returns no partial batch and preserves the prior cache and snapshots. This is an explicit modern policy: the original sampler writes its flag/key before miss completion, and original failure recovery remains Unknown.

`Clear` explicitly clears all owned cells after its pre-operation cancellation check. It does not imply that an original caller clears on every work request. `CopyCache` returns an independent read-only snapshot. Owner-thread query/copy/clear/dispose, idempotent disposal, active-operation guards and finally-based recovery are modern policies. There are no callbacks or external enumerators during operations; synchronous reentry is structurally excluded. No shared RNG, I/O or engine adapter is used.

## Bounded work and validation

All budgets are explicit logical payload/work accounting, independent of CLR headers or machine instructions. Factory checks precede normalization/arithmetic/table allocation:9512 kernel bytes plus16 bytes per cell plus136 raw-input allowance, bounded at1048576; work requires20000 plus16 per octave, bounded at1048576. Fixed-width finite inputs and16 octaves bound normalization intermediates even before F32 eligibility checks.

Query accepts at most1024 requests. Before copies/arithmetic, reserve16 bytes per candidate cell plus `(160 + 80 * octaves)` bytes per request, capped at2097152. This conservatively includes copied requests, returned samples and cumulative per-octave kernel/sample payload. Work reserves one per cell plus `(512 * octaves + 64)` per request, capped at16777216, even for hits. CopyCache reserves16 bytes per cell, capped at16384. Kernel arithmetic and allocation limits remain in force. Cancellation polls before allocation, before each request/octave and before candidate publication. Clear has a single pre-commit cancellation check; disposal releases owned references without promising deterministic managed-memory wiping.

The independent verifier (external historical reference) SHA-gates the original executable/config/source/TGEN/first family/MFRC. It derives original selected words privately and composes frozen46 stores with frozen47 Fraction/integer noise; native output is never used as expected arithmetic. One pinned fresh original family plus synthetic parameter frames/coordinates/profiles yields274 scenarios:28 factory rejections,7086 actions,5940 returned queries,1134 deliberate action rejections and792 retained-miss/F32-hit differences.21 API negative checks cover budgets, precision, indices, late failure, cancellation, foreign owner, immutable snapshots and disposal. Raw input preservation and snapshot survival pass. Optional sine/curves and mode generalization remain excluded.

Frozen48 kernel1155 cases, raw330/U642592, retained7762/786 composition and prior terrain/sampler regressions pass, along with actual default smoke. The runner (external historical reference) uses fresh private outputs, lexical ancestor reparse checks before creation, explicit cached runtimes/offline packages and actual Core/reference builds. Its default runs all frozen gates; `SamplerOnly` repeats just changed sampler checks when frozen evidence is already retained.

Remaining Unknown: original active controls/exception policy, optional FSIN/log/helpers, other modes, mutable setters/reload/lifetime, work-request reachability, full recursive terrain and original generated parity. The [worker runtime policy](TERRAIN_ARITHMETIC_RUNTIME_POLICY.md) already bounds startup evidence. Next: an explicitly bounded sampler-to-AHFR/FFRA retained-consumer join with caller cell/clear policy supplied; preserve computation/cache origin and do not infer a full world request or original controls. Lead owns canonical reconciliation and acceptance.
