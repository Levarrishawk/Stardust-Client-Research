# Height-range filter input and conditional influence

[TerrainFilterHeightInput](../../SWG-Godot-Client/poc/Stardust.Core/Assets/TerrainFilterHeightInput.cs) preserves bounded owned `FHGT` records. [TerrainFilterHeightInfluence](../../SWG-Godot-Client/poc/Stardust.Core/Geometry/TerrainFilterHeightInfluence.cs) evaluates the recovered fresh finite query against one **supplied current height word**, with explicit nearest-even 24/53/64 query precision and retained shaping. It does not read or generate a height grid, adapt the existing layer carrier, evaluate other filters or claim original terrain parity. The [Round 42 checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R42_HEIGHT_FILTER.md) records pins, reproduction, corrected attempts and resume action.

## Identity, observations and consequences

Addresses are VAs for executable SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`, rehashed before static instruction/RTTI reads. Neither original executable execution nor analogous server code supplies these conclusions.

| Observation | Interpretation / confidence | Compatibility consequence |
| --- | --- | --- |
| Filter factory compares `46484754` at `00B7A00A`, selects branch `00B7A12A`, allocates 0x24 at `00B7A13A`, constructs via `00B776C0` at `00B7A154`, loads through +8 at `00B7A16B`. Constructor installs `015ED6E0`; its COL at `01626358` names type descriptor `018B8A78`, `.?AVFilterHeight@@`. | Actual FHGT/FilterHeight identity, Confirmed. | Scope this one family; no guessed tag or sibling-filter schema. |
| Constructor passes kind 0/FHGT to `00B633E0`, clears lower +1C/upper +20; common base `00B633F3/F6` clears selector +14 and amount +18. | Fresh defaults, Confirmed. | Older serialized versions use runtime selector/amount +0 while absent serialized fields remain null. |
| Vtable +8/+C/+10/+14 resolve to dispatcher `00B777E0`, writer `00B77AA0`, amount setter `00B65FE0`, query `00B77730`. Dispatcher subtracts 0000 then decrements twice, choosing `00B778A0`, `00B77920`, `00B779B0`. | Three versions and actual query identity, Confirmed. | Preserve separate layouts below; unsupported versions stay opaque under modern inspection policy. |
| Version-0 reads `00B778DA/ED` write +1C/+20. Version-1 first read `00B7795A` is overwritten, then `00B77967/7A` write range. Version-2 reads `00B779EA/FD`, `00B77A10/28` write range/selector/amount. | Exact raw order, Confirmed; discarded prefix meaning Unknown. | Preserve the prefix word without inventing semantics. |
| Version-2 loader tests amount against zero/one at `00B77A30..65` then invokes setter +10; `00B65FE6` itself simply stores +18. Range setters `00B77B30/40` simply store raw words. | Fresh load clamp versus raw setters, Confirmed. | Preserve serialized amount, clamp finite version-2 amount once in fresh query resolution; do not infer a clamping edit API or range normalization. |
| Query `00B77739/3C` obtains fifth argument's +18 grid descriptor; descriptor +0 supplies stride, +8 supplies data, index is row*stride+column at `00B7774B..5A`. | Current row-major cell read, Confirmed structure; height role supported by RTTI and AHCN sharing. | A supplied current F32 word suffices for this conditional evaluator; no initial-height or precomputed-layer assumption. |
| Filter caller pushes state EBX at `00B640A6` and queries +14 at `00B640B8`. Later affector caller pushes the same EBX at `00B6417A`, invokes +10 at `00B6418C`; AHCN reads state +18 at `00B7657B` and grid data +8 at `00B76586`. | Same state/grid and filter-before-this-layer-affectors timing, Confirmed. | This layer's FHGT sees the cell at query time, before its own affector loop. Earlier writes may be visible; complete traversal/initialization remains outside this API. |
| Query compares height strictly above lower and strictly below upper at `00B77768/75`; it computes span without an F32 spill at `00B77789..8F`, then lower/upper ramps through `00B777AA/C7`. | Exclusive range endpoints, retained span and query value, Confirmed for defined finite arithmetic. | Equal/reversed ranges yield outside +0; no swap, blanket inclusive edge rule or premature projection. |
| Filter selector table `00B642B0` dispatches identity/square/sqrt/ordered cubic, candidate store `00B640E9` retains arithmetic for later combination. | Shared filter shaping/storage, Confirmed. | Own Shape uses retained query; layer comparison/combination is deferred. |
| Destructor `00B77720` tails to `00B63410`, then common header-name teardown `00B62EF0`; deleting wrapper `00B776F0` optionally frees object. | Scalar fields plus owned header lifecycle, Confirmed. | Managed immutable input/query/batch objects have no external handles; collection timing differs. |

## Serialized layouts and raw preservation

Exactly one `FORM FHGT` contains one version form. Known versions require ordered `FORM IHDR`, then leaf `DATA`. IFF container sizes use the accepted big-endian framing; DATA words are raw little-endian U32 encodings with F32 runtime uses. No coordinate units or axes are assigned.

| Version | DATA order | Exact bytes |
| --- | --- | --- |
| 0000 | Lower raw F32; upper raw F32. | 8 |
| 0001 | Discarded legacy raw U32; lower raw F32; upper raw F32. | 12 |
| 0002 | Lower raw F32; upper raw F32; selector U32; amount raw F32. | 16 |

The writer `00B77AA0` emits 0002 and four separate four-byte writes in range/selector/amount order, providing independent serialization direction. Recognized IHDR 0001 preserves enabled U32 and NUL-terminated raw name bytes; IHDR 0000 also preserves three following legacy bytes. The accepted [TGEN header convention](TERRAIN_GENERATOR_INPUT_CONTRACT.md) supplies the immutable header type. Nonzero raw enabled indicates local enablement; no text encoding is invented.

`Fields` keeps nullable prefix/selector/amount when absent. `CopyDataWordBits()` preserves every serialized word in order. All raw F32 categories are accepted by the decoder, including signed zeros, subnormals, infinities and NaNs. Finite inspection is separate from structure. Neither lower nor upper is swapped or normalized. Fresh runtime finite amount is version-2 serialized amount clamped by numeric <0/>1 comparisons; equal -0 keeps its bits. Versions 0/1 resolve runtime amount/selector to +0, without synthesizing serialized words. Original nonfinite clamp/exception behavior remains Unknown.

Unsupported FHGT or IHDR versions preserve the entire bounded source opaquely with no typed fields/header/data words. This is a modern inspection policy, not proof that original diagnostics safely accept them. Wrong family, known child order/tags/extents, duplicate or missing children, trailing DATA bytes, unterminated names and over-budget framing reject with `InvalidDataException`.

## Exact finite query and shaping

Let `lo`, `hi`, `h` be raw finite lower, upper and supplied current height; `f` is fresh resolved amount. `R` rounds each arithmetic instruction nearest-even to the explicit significand width with wide exponents. Comparisons are numeric finite comparisons; raw signed-zero fields remain preserved.

If `h <= lo` or `h >= hi`, return positive zero. Consequently equal/reversed ranges have no interior, and both endpoints are excluded. Otherwise retain:

```text
span = R(R(R(hi-lo)*f)*0.5)
if R(lo+span) > h:
    u = R(R(h-lo)/span)
else if R(hi-span) < h:
    u = R(R(hi-h)/span)
else:
    u = 1
```

Lower branch precedes upper branch. Equality at either feather threshold takes interior one. Span is retained across both comparisons and division; no F32 cache or span store exists in the original graph. The query reloads an F32 copy of the grid word, which preserves a finite input word. The original query has no calls and no final influence F32 store. Modern `QueryProjectionBits` and `FeatherSpanProjectionBits` are inspection projections; Shape uses retained `u`, not those projections.

Selector 0 retains `u`; 1 computes `R(u*u)`; 2 `sqrt_R(u)`; 3 ordered `R(R(R(3-R(u+u))*u)*u)`. Other unsigned selector values produce positive zero after query. The shaped output is stored once to F32. The existing layer caller subsequently compares its retained shaped value against the accumulator; that combination and adapter are outside this standalone API.

The implementation uses the frozen exact Number primitive with its own independently derived filter graph. Original active precision/rounding/exception controls remain Unknown. Modern active queries require finite range, height and serialized amount; undefined arithmetic or nonfinite inspection/shaping projection rejects. Wide intermediates are not arbitrarily F32-spilled. No claim about legacy NaNs or malformed grid indexing follows.

## Ownership, bounds and failure policy

Input defaults: 65,536 source bytes, 64 IFF nodes, depth 8, 4,096 name bytes excluding terminator, 65,536 logical decoded-copy bytes. Hard option maxima are 4,194,304 source/copy bytes, 65,536 nodes/name bytes and depth 64; minimum input cap is 24, minimum nodes/depth 1, name/copy caps may be zero. `DecodedCopyBytes = 4*wordCount + nameBytes + legacyBytes`; terminator/source/temporary snapshots and fixed overhead are excluded. Source cap is checked before source/section copy; decoded-copy/name checks precede typed header/word allocations. Published arrays are private, and inspection methods copy. Header and fields remain immutable; failed later decoding cannot modify earlier inputs.

`Query` is direct geometry and does not apply local enabled. `Shape` consumes a retained receipt. `Evaluate` gates local enabled and uses the serialized selector/default. `EvaluateBatch` snapshots supplied height words, publishes immutable results only after every candidate succeeds, and keeps each input reference immutable. `CopyInfluenceBits()` returns independent storage; no callbacks or external resources participate.

Query limits default to 65,536 samples and 131,072 abstract query-work units; hard maxima are 1,048,576 and 2,097,152. Direct Query charges one unit; active Evaluate charges two (query plus shape); active batch charges 2N. Shape alone is fixed work. These are modern bounded operation-group charges, not observed instruction counts. Sample/work checks precede height/result snapshots. Disabled evaluation/batches charge zero and skip unused nonfinite floats, while validating known input, profiles and caps. Empty batches execute no query. Height-array payload is 4N bytes, not measured total managed heap usage. Decoder and query budgets are separate.

Null reference arguments throw `ArgumentNullException`; invalid option/profile ranges throw `ArgumentOutOfRangeException`; unsupported input, exceeded caps, active nonfinite/undefined arithmetic and malformed source throw `InvalidDataException`. Late batch failure returns no partial result and leaves prior receipts unchanged. Managed temporary lifetime does not guarantee deterministic wiping or legacy teardown timing.

## Verification and remaining scope

[Native scenarios](../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/TerrainFilterHeightScenarios.cs) and the [independent raw/Fraction verifier](../../SWG-Godot-Client/poc/round42-tests/Assets/verify_height_filter.py) agree on 375 raw cases (38 accepted, 337 rejected), 13,959 query cases (13,934 accepted, 25 rejected), 30 ownership/budget checks and five null negatives. Fixtures cover all versions, complete-record/DATA truncations, raw nonfinite fields, unknown versions, malformed order/extents, equal/reversed ranges, strict endpoints, feather thresholds, all selectors, three profiles, negative/above-one/zero/subnormal amounts, wide ranges, disabled cases, aliases, one-less budgets and late failure.

Static verification checks 90 instruction anchors, five height-source/caller anchors, 18 finite status cases, RTTI/vtable and version read counts. It finds 117 query-profile-sensitive inputs and 138 premature-projection shaping cases; these do not identify original active controls. The separate runner and actual Core/default smoke build and run pass with zero warnings/errors. One pinned original FHGT raw record is independently decoded; no original query or terrain parity is claimed.

Remaining Unknown: original floating controls/nonfinite recovery, units/axes, initial grid and complete traversal state, edited range/amount lifecycle and legacy malformed-input handling. All other real filters are excluded. Exact next action after acceptance: add a retained height-filter carrier at the layer-query boundary and replay ordered supplied-layer execution using each query-time current height, without precomputing later layers or projecting before shaping.

## Round43 integration

The [query-time sequence](TERRAIN_SUPPLIED_HEIGHT_FILTERED_SEQUENCE_CONTRACT.md) binds retained queries to each stage current height before affectors. Its native geometry join is accepted; original recursive traversal and active floating controls remain Unknown.
