# Terrain boundary polyline serialized input

[TerrainBoundaryPolylineInput.cs](../poc/Stardust.Core/Assets/TerrainBoundaryPolylineInput.cs) decodes owned, bounded `BPLN` serialized records independently of an engine. Direct client evidence establishes two layouts, `0000` and `0001`. The decoder preserves input identity, raw point order and raw metadata; it performs no influence evaluation or cached-bounds computation. See the [Round 38 checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R38_BPLN_INPUT.md) for input pins and reproduction.

## Evidence and confidence

**Confirmed:** factory `00B79DF0` compares `BPLN` at `00B79E3D`, allocates 0x3C bytes and calls constructor `00B74A20` at `00B79E6D`. Its virtual loader call at `00B79E84` resolves through vtable `015ED474` slot +8 to `00B74D00`. That dispatcher subtracts `0000`, selects loader `00B74DD0` for zero, decrements and selects `00B74EE0` for one; other values enter the diagnostic path. There is no BPOL-style seven-version table.

**Confirmed structure:** the common point-vector base is `00B736E0`; both BPLN loaders call IHDR loader `00B62FA0`. Each loader reads a selector, amount and width word through separate four-byte reads and two four-byte reads per ordered point. Neither loader reads a discarded prefix, extension flag, extension scalar pair or extension name. The extra BPLN width at +38 has its own setter and cache behavior. These conclusions come from BPLN instructions, not inferred BPOL inheritance or server schema.

**Strongly inferred role:** `WidthBits` labels the scalar used to expand both bounds components. The executable confirms its arithmetic use as a raw floating-point padding value. Whether it represents a full width, half width, distance in particular units or another gameplay convention remains unknown. No geometry axes or units are assigned to point components.

## Exact layouts

The input is a single `FORM BPLN` containing one version form. Recognized versions require exactly ordered `FORM IHDR`, then a leaf `DATA`. Existing IFF framing uses big-endian container extents; DATA scalar words are raw little-endian U32 encodings. Point components and amount/width words have floating-point runtime uses, but the decoder retains their original uint bits.

| Version | DATA order | Count representation |
| --- | --- | --- |
| `0000` | Selector U32; amount raw F32; width raw F32; ordered pairs of first raw F32, second raw F32. | Remaining bytes after the first 12 bytes divided by eight. |
| `0001` | Signed 32-bit point count; ordered pairs; selector U32; amount raw F32; width raw F32. | Serialized count before pairs. |

Known IHDR `0001` contains enabled raw U32, then a NUL-terminated byte name. IHDR `0000` adds three single bytes after the name. Nonzero enabled bits indicate local enablement. Both header versions preserve raw enabled bits, name bytes and legacy bytes; no string encoding is inferred. This uses the accepted [TGEN input convention](TERRAIN_GENERATOR_INPUT_CONTRACT.md), confirmed by the shared client loader calls.

`SerializedPointCountBits` is null for `0000`, present for `0001`. Other recognized metadata words are always present. `CopyMetadataWordBits()` returns the non-point DATA words in serialized order: three words in `0000`, count plus three words in `0001`. It does not invent an implicit count or include point words. `CopyPoints()` preserves exact pair order, duplicates, repeated endpoints and raw signed-zero bits. No closure, winding, deduplication or minimum-point rule is imposed. Zero, one and two points are accepted as serialized records without claiming valid evaluator geometry.

## Raw preservation and runtime separation

All raw F32 encodings are accepted, including positive/negative infinity, quiet/signaling NaNs, signed zeros and subnormals. `HasFinitePoints`, `HasFiniteSerializedFeatheringAmount` and `HasFiniteWidth` use exponent-bit inspection separately from acceptance. Unknown opaque records have null metadata and null point-finite status; decoded empty point sets are vacuously finite.

The constructor passes kind 3 and BPLN tag to the shared base, initializes +38 to `0x40000000` and sets vtable `015ED474`. The base initializes an empty point vector (+1C capacity, +20 data pointer, +24 count), zero bounds (+28 through +34), and zero selector/amount (+14/+18). Runtime defaults are not additional serialized words: both known BPLN layouts explicitly serialize selector, amount and width.

Amount setter `00B65FC0` stores raw bits directly at +18. Width setter `00B74A80` stores directly at +38 and calls virtual slot +38. No clamp, absolute-value transformation or finite eligibility check appears in either loader or setter. Cache override `00B74AA0` calls shared point-bounds computation `00B738B0`, subtracts width from minima and adds it to maxima. Each recognized dispatch path calls the cache again after its loader. Version `0000` invokes the width setter before loading points; `0001` invokes it afterward. Intermediate cache timing is documented, not reproduced.

Query slot +24 resolves to `00B74AD0`; the common layer path checks local enablement at `00B63F8E` and queries through +24 at `00B63FAF`. The decoder does not emulate this query, feather shaping, coordinate conventions, layer/filter combination or ancestor enablement.

## Bounded modern policy

Default limits are 262,144 encoded bytes, 64 IFF nodes, depth 8, 8,192 points, 4,096 name bytes excluding the terminator, 8,192 total decoded string bytes including the terminator, and 262,144 logical decoded-copy bytes. Input size is checked before source snapshot or section-copy allocation. Structural checks, string scans, point counts/extents and aggregate copy budgets complete before publishing typed arrays.

`DecodedCopyBytes = 8 * pointCount + 4 * metadataWordCount + headerNameLength + headerLegacyLength`. This counts retained decoded array payloads; it excludes terminators, encoded snapshots, temporary IFF snapshots and fixed object overhead. The input cap separately bounds encoded snapshots. The charge is a logical budget, not measured managed heap usage.

The modern decoder requires remaining bytes to be a multiple of eight for `0000`, and exact count/payload completion for `0001`. Legacy helper `00A94EE0(8)` floors the remaining-byte division; partial-pair legacy recovery at close is unresolved. Version `0001` tests signed count at `00B74F25` and uses `jle` at `00B74F27`, so nonpositive legacy counts skip the point loop and continue metadata. The modern decoder rejects negative counts and excessive positive counts. These stricter rejection policies must not be claimed as legacy behavior.

Unknown BPLN versions retain the complete bounded source opaquely without typed fields. Known BPLN with unknown IHDR version likewise stays opaque without interpreting DATA. Container framing and input/node/depth limits still apply. Opaque preservation is a modern inspection policy; the original unknown-version diagnostic/cache path does not prove safe acceptance. Wrong family, wrong known child order/tags, duplicate/missing records, unterminated names, bad extents, count disagreements and over-budget input throw `InvalidDataException`. Invalid options throw `ArgumentOutOfRangeException`; null section throws `ArgumentNullException`.

## Ownership and lifecycle

Published source, header bytes, metadata and points are owned and immutable. All array inspection methods return copies; the point collection rejects `IList` mutation. Decoding from an owned TGEN section preserves section isolation. A failed later decode leaves earlier results unchanged. Temporary allocations use managed lifetime; no timed garbage collection claim is made.

Legacy destructor `00B74A70` tail-calls shared vector destructor `00B73740`, which frees +20 and clears capacity/pointer/count, then calls boundary teardown `00B63350`. Common teardown frees the IHDR name. Deleting wrapper `00B74A40` optionally frees the BPLN object. There is no separate BPLN extension-string owner. The modern result contains managed values with no external handles, callbacks or explicit disposal requirement.

## Verification and compatibility consequences

[Native scenarios](../poc/Stardust.Core.SmokeTests/TerrainBoundaryPolylineInputScenarios.cs) and the [independent Python oracle](../poc/round38-tests/Assets/verify_bpln_input.py) agree on 325 fixtures (55 accepted, 270 rejected), 18 ownership checks, 47 static anchors and all 34 original records from the previously pinned Tatooine TGEN. The actual separate Core/default smoke-test build and execution pass. Original observations: all BPLN/IHDR versions `0001`; 163 points total, 2–15 each; all locally enabled and finite; selector 0 appears 15 times and selector 3 appears 19 times; maximum logical decoded-copy charge 152 bytes. This is one ordered identity/raw-field comparison, not a broader corpus survey.

A replacement must preserve these serialized boundaries and raw values; internal storage and bounded failure policy may differ. Client coordinate units, width interpretation, query arithmetic and runtime precision remain unresolved. Server `MMOCoreORB/src/terrain/layer/boundaries/BoundaryPolyline.h::parseFromIffStream(Version<'0001'>)` corroborates the version-1 order but is only correlation evidence and supplies no client proof for axes or influence.
