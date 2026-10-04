# Fractal sampler cell cache and finite arithmetic boundary

Date: 2026-10-04. This research closes the cell key, finite hit predicate, observed invalidation, and cache-store versus returned-value distinction at `013E7C00`. It does not implement a production noise sampler. The existing [AHFR supplied consumer](TERRAIN_HEIGHT_FRACTAL_CONTRACT.md) and [FFRA supplied consumer](TERRAIN_FILTER_FRACTAL_CONTRACT.md) remain explicitly narrower F32-input APIs.

All addresses are VAs for executable SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. The [verifier](../tools/python/verify_terrain_fractal_sampler.py) checks 230 selected instruction anchors, the six mode targets, four cache-value stores, and exact F32/F64 constants after validating executable identity. The original executable is never run.

## Cell selection and finite hit predicate

| Observation | Interpretation / confidence | Consequence |
| --- | --- | --- |
| `013E7C0A/10/1B/1E` selects `(row * object+2574 + column) * 16`; +257C supplies the allocation base. +2578 is the retained row capacity used by allocation/clearing, rather than the index stride. | Confirmed cell index and 16-byte stride. | Preserve retained stride when requests reuse storage. This routine itself checks neither bounds nor allocation validity. |
| Byte at cell+0 is tested for zero at `013E7C21/26/2B`; cell+4/+8 hold raw first/second coordinate words; cell+0C holds the stored F32 result. Bytes1..3 are not read by this sampler. | Confirmed occupied flag and fields. Padding semantics remain Unknown. | Any nonzero valid byte enters coordinate validation. Do not replace the full key with row/column alone. |
| `013E7C2D..81` computes cached-coordinate minus/plus F32 constant `3727C5AC`, then compares each original argument using `TEST AH,41 / JP`. | Confirmed finite **inclusive** intervals in both axes. Constant is approximately 0.00001 in caller coordinate units. | Equality at either bound hits. TEST-mask parity accepts finite less/equal and rejects greater/unordered at each directed comparison. |
| `013E7C83` loads cell+0C with FLD and returns at `013E7C8C`. | Confirmed F32 reload on hit. | A hit does not reconstruct the earlier retained wide result. |

With explicit arithmetic rounding R, a finite occupied cell hits exactly when:

`R(cachedX - epsilon) <= x <= R(cachedX + epsilon)` and `R(cachedZ - epsilon) <= z <= R(cachedZ + epsilon)`.

There is no F32 spill of these tolerance endpoints. They therefore depend on active arithmetic precision and rounding. At large coordinates an interval can collapse; equality still hits. The native research model tests separate nearest-even 24/53/64 profiles and includes positive/negative zero, large coordinates, exact endpoints, adjacent F32 epsilon words, zero/nonzero flags, and invalid modern indices. Original controls remain Unknown. Nonfinite comparisons and trapped exceptions are not promoted into a safe legacy-input policy.

AHFR call `00B768E9` and FFRA call `00B77C13` forward first/second F32 coordinates and column/row words in that order. Their caller coordinate producer stores column-derived first and row-derived second coordinates, as established in the [sample-coordinate contract](TERRAIN_SAMPLE_COORDINATE_PRODUCER_CONTRACT.md). X/Z correspondence is Strongly inferred through the position builder; physical unit naming is not needed to establish these bytes. The tolerance belongs to these original unscaled coordinates, before the sampler's frequency/offset transformations. A further direct-call instruction candidate at `00B8B2F7` matches this target; its nearby instructions forward the same four words and then use the result in integer-selection arithmetic. Its containing function identity and reachability remain Unknown, so the two selected callers are not claimed exhaustive. Family identifiers select the sampler object outside this cell key. Family-map lookup and item lifetime belong to a separate investigation.

## Miss preparation and observed invalidation

A miss sets cell byte0 to one and stores the original coordinate words at `013E7CA3/A6/A9`, **before** noise/postprocessing completes or the result is written. This is Confirmed write order. No exception recovery or atomic cache publication is established; a trapped operation could leave a changed flag/key and prior value. The modern research harness has no such mutation and makes no original cleanup claim.

`013E6AD0` grows only when either requested dimension exceeds its retained counterpart (`013E6AE4..EE`). On growth it calls the free helper for existing +257C, requires nonzero dimensions before allocating, stores both requested dimensions, allocates `requestedWidth * requestedRows * 16` bytes, and calls `013E6B50`. A request that fits both retained dimensions returns without resizing or clearing. Growth can decrease the other dimension: this is replacement with the two requested dimensions, not componentwise maxima. Arithmetic is unchecked original 32-bit multiply/shift; negative sizes, overflow, null allocation, and concurrent access remain Unknown.

`013E6B50` checks +257C for null, then clears all `retainedWidth * retainedRows * 16` bytes through REP STOSD/STOSB. This clears flags, coordinates, values, and unused bytes. `00B75B05` calls the resize routine with forwarded dimensions while iterating map items; the [existing height-order contract](TERRAIN_HEIGHT_LAYER_ORDER_CONTRACT.md) establishes its guarded generator-preparation caller. This observation does not duplicate the map's lookup/item-lifetime contract or prove request-wide invalidation on every generator call.

| Setter / operation | Observed cache effect |
| --- | --- |
| `013E6B80`, seed +0 | On a changed seed, calls embedded initialization `013E64D0`, then clears. Same seed skips both. |
| `013E6BB0`, coordinate scales +4/+8 | Stores both words, then clears. |
| `013E6BD0`, coordinate offsets +0C/+10 | Stores both words, then clears. |
| `013E6BF0`, signed octave count +14 | Stores then clears; this setter does not itself rebuild +20 normalization. |
| `013E6C10`, frequency multiplier +18 | Stores then clears. |
| `013E6C30`, amplitude multiplier +1C | Rebuilds +20 through `013E6DD0`, then clears. |
| `013E6C50`, byte +24 / parameter +28 | Stores then clears. |
| `013E6C70`, byte +2C / parameter +30 | Stores then clears. |
| `013E6C90`, sine enable byte +34 | Stores then clears. |
| `013E6CB0`, mode +38 | Sets dispatch pointers for recognized modes and clears; an out-of-range mode still reaches clearing. |
| `013E6D50`, reset | Resets controls, calls mode setup and normalization rebuild, then tails to clearing. |

Confidence: Confirmed observed setter/clear call chains. This is not an exhaustive proof that every direct writer or concurrent mutation invalidates. Constructor initializes +2574/+2578/+257C to zero; destructor `013E6930` frees +257C when nonnull and sets it to zero. No thread-affinity, locking, cancellation, or universal reuse guarantee is inferred.

## Miss call graph and storage points

On miss, multiply first coordinate by object+4 and second by +8, each with a mandatory F32 store at `013E7CB5/CCC`. With positive octave count, add offsets +0C/+10, again storing F32 at `013E7CF5/CFE` in the first mode. Initialize frequency and amplitude locals to F32 one. Each octave passes F32-stored frequency-scaled coordinates to the embedded object at sampler+44 / `013E66F0`; the five call sites are `013E7D19`, `013E7DA9`, `013E7E1D`, `013E7E8B`, and `013E7F2A`.

The returned noise value may still be wide. Each octave transforms it according to mode, multiplies amplitude, adds the prior stored sum, and stores the new sum as F32. Frequency and amplitude updates multiply +18/+1C and store F32 for the next octave. Nonpositive signed octave count skips the octave loop. This graph is Confirmed at the instruction/storage boundary; neither noise values nor an original sample occurrence were reproduced.

| Mode +38 | Target | Observed finite octave contribution / finish |
| --- | --- | --- |
| 0, 1 | 013E7CDC | raw noise; normalized sum is shifted by one then multiplied by half |
| 2 | 013E7D68 | one minus absolute noise; normalized sum |
| 3 | 013E7DDC | absolute noise; normalized sum |
| 4 | 013E7E4A | one minus noise clamped to [0,1]; normalized sum |
| 5 | 013E7EE9 | noise clamped to [0,1]; normalized sum |

These are descriptive arithmetic roles, not new canonical semantic function names. Mode above5 bypasses these loops and continues from the initialized zero accumulator into optional postprocessing. The parallel uncached dispatch pointers +3C/+40 are set by `013E6CB0`, but their complete callers are outside this question.

Normalization +20 is rebuilt at `013E6DD0`: the observed wide sum of octave weights is finally stored F32, reloaded, reciprocated, and stored F32. Constructor/reset and amplitude setter call it; changing the octave count alone does not call it in the observed setter. Invalid/nonpositive counts can encounter zero-division behavior; no modern default is substituted.

If byte+34 is nonzero, the path adds the **scaled first coordinate before its offset** to the stored octave sum, executes FSIN, and stores F32. It is not the shifted noise-coordinate input. Byte+24 enables a logarithm/exponent-helper path that explicitly discards the retained finish and reloads its F32 snapshot before calling `0131E2B0`. Byte+2C enables threshold/curve branches: below F64 0.001 returns zero, above F64 0.999 returns one, and the middle branch computes a logarithmic exponent then calls `0131E2B0`. Exact bits for these constants and F64 half are verified. FSIN, logarithmic domains, helper arithmetic, rounding, and exceptions remain unclosed; the finite research model does not emulate them.

Immediate noise boundary `013E66F0` adds a constant, calls conversion helper `0131E27C` for each coordinate, performs negative-fraction correction and masked permutation lookups at embedded offsets +510, then gradient accesses at +D18 and interpolation arithmetic. Initialization `013E64D0` calls RNG helper `00B59EF0` while constructing those tables. Observation: Confirmed these calls/data accesses. Interpretation: Strongly inferred seeded gradient-noise structure. Exact RNG recurrence, seed tables, integer conversion, complete interpolation graph, and original controls remain Unknown. No library noise or random substitute is admitted.

## Cache write versus return

The common finish uses **FST**, not FSTP or a store/reload, to write cell+0C at `013E8074`; other finish stores are `013E7FE5`, `013E8006`, and `013E804A`. Consequently a finite miss can leave the retained pre-store result in ST0 while caching its F32 projection. A later hit returns the F32 reload. Intermediate inspection FST stores do not by themselves replace ST0.

AHFR multiplies the returned value by scale at `00B768EE` and retains it into its height arithmetic until the final height F32 store. FFRA multiplies at `00B77C18` and mandatorily stores the scaled result F32 at `00B77C24`. Its scale store does not make supplying a pre-rounded F32 sampler return equivalent to the original wide miss. Confidence: Confirmed store/caller distinctions. Consequence: any future production sampler must preserve hit/miss return width and caller-specific stores under evidenced controls. Existing supplied F32 APIs remain conditional.

## Research verification and limits

The own-role native harness links the frozen exact Core arithmetic source and references the actual Core assembly for regressions. Its independently authored scalar models cover the inclusive probe, retained-stride growth/reuse, positive-octave coordinate-store graph, and a synthetic post-result product carried through AHFR operation1 and FFRA scale stores. Products stand in for a retained postprocessing value; they are **not generated noise samples**. The post-result witnesses use synthetic non-unit scale `3F800001`; the selected original FFRA instead has scale `3F800000` (one). These witnesses therefore establish the general store boundary, rather than a hit/miss numerical change in that original occurrence. Python uses independent Fraction arithmetic. Profiles are explicitly nearest-even24/53/64, not asserted original controls.

Final comparison passes 2,466 synthetic cases (66 modern rejections), 360 cache-hit cases, 715 AHFR-side retained-return witnesses, and 303 FFRA-side witnesses. Modern test eligibility bounds dimensions to128 each and1024 cells, requires coherent dimensions/indices and finite used values, and rejects nonfinite F32 projections. Unoccupied-cell unused cached words are not evaluated. These are research-harness policies, not original validation behavior or a production cache API.

Frozen regressions pass: AHFR151 raw /8836 supplied consumers; FFRA152 raw /2326 queries;6354 retained carrier cases;1141 filtered-sequence cases, with their existing ownership and budget checks. Actual Core/reference runner builds with zero warnings/errors. Eight in-process wrong-executable identities and four separate wrong-identity process cases reject before decoding; the latter use absent decoder packages and create no output root. No production file changed, so the default smoke executable was not rebuilt or rerun for this research-only milestone. Focused scope/link/AST/XML/PowerShell and organized-workspace checks are recorded in the hash handoff.

Next probe: close `013E64D0 -> 00B59EF0` seeded table construction and `013E66F0 -> 0131E27C` conversion/interpolation for one selected original family, including effective controls and optional postprocessing. Only then consider production noise generation or original terrain sample admission. Family-map/item lifetime remains the separate Assets investigation.

## Round47 compatibility increment

The [seed/table/conversion/interpolation closure](TERRAIN_FRACTAL_SEED_NOISE_CONTRACT.md) now has289 anchors/1004 native comparisons and520 recurrence checks for conditional profiles. The immediate noise element is closed under those policies; the complete octave/mode/normalization/cache miss graph, effective controls, optional postprocessing and generated-sample reachability remain separate. [Retained supplied consumers](TERRAIN_RETAINED_FRACTAL_SAMPLE_CONTRACT.md) preserve the miss/hit storage distinction without generating noise.

## Round48 compatibility increment

The [owned immediate noise element](TERRAIN_SEEDED_NOISE_KERNEL_CONTRACT.md) and [raw MFRC/0001 decoder](TERRAIN_FRACTAL_RAW_INPUT_CONTRACT.md) are implemented. [864 actual noise-to-consumer compositions](TERRAIN_COMPUTED_NOISE_CONSUMER_JOIN_CONTRACT.md) preserve36 high-bit results and120 final retained/reload differences. Full octave/normalization/cache composition, optional curves and original controls remain open.

## Round49 compatibility increment

[Complete conditional sampler composition](TERRAIN_COMPLETE_SAMPLER_CONSUMER_JOIN_CONTRACT.md) now passes432 actual Core joins, preserving108 final miss/cache differences. The [owned mode0 sampler](TERRAIN_CONDITIONAL_FRACTAL_SAMPLER_CONTRACT.md) preserves normalization/octave stores and atomic modern cache batches. Other modes/curves, effective original controls, request/family lifetime and generated terrain parity remain open.
