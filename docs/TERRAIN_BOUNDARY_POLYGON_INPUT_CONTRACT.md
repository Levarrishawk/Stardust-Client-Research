# Terrain boundary polygon serialized input

This contract describes owned, engine-neutral `BPOL` input decoding in TerrainBoundaryPolygonInput.cs (external historical reference). It establishes serialized structure, ownership and bounded rejection policy. It does not implement polygon influence, geometry editing, coordinate conversion, cached bounds or layer composition. Evidence and reproduction are recorded in the [Round 37 checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R37_BPOL_INPUT.md).

## Evidence and confidence

**Confirmed:** the pinned executable's boundary factory `00B79DF0` compares `BPOL` at `00B79E1A`, allocates 0x48 bytes, constructs through `00B73BA0` and calls loader slot +8 at `00B79F40`. Vtable `015ED434` points that slot to `00B73FE0`. Its seven-entry table at `00B7412C` selects versions `0000` through `0006`; loader addresses are `00B74150`, `00B74220`, `00B74300`, `00B743F0`, `00B74520`, `00B74660`, and `00B747B0` respectively. Each known path calls cached-bounds slot +38 after loading. Original corpus evidence covers version `0005`; other supported versions have executable evidence and synthetic fixtures.

**Confirmed structure, unknown semantics:** extension words and strings retain neutral names. Server terminology is correlation evidence only. Point components are floating-point values in the cache computation, but their axes and units remain unknown. Raw widths do not establish semantic meaning.

## Container and byte layout

The decoder accepts a complete `FORM BPOL` snapshot, or an owned `TerrainGeneratorSection`. Existing IFF framing supplies big-endian container extents and ordered child boundaries. A recognized version requires exactly ordered `FORM IHDR`, then `DATA`. Scalar payload words are little-endian raw 32-bit values. Each point stores `FirstBits`, then `SecondBits`, with an eight-byte stride. No point order, repeated endpoint, winding or signed-zero normalization occurs.

Known IHDR versions use the frozen TGEN convention: `0001` has an enabled raw U32 followed by a NUL-terminated byte name; `0000` adds three discarded single bytes after that name. Nonzero enabled bits represent local enablement. Strings are byte sequences without an inferred text encoding; copies exclude the terminator, while string budgets include it.

| BPOL version | DATA order |
| --- | --- |
| `0000` | Remaining bytes are ordered point pairs. |
| `0001` | Discarded raw U32 prefix; remaining point pairs. |
| `0002` | Selector U32; amount raw F32; remaining point pairs. |
| `0003` | Selector; amount; extension flag U32; first extension scalar raw U32; NUL extension name; remaining point pairs. |
| `0004` | Selector; amount; extension flag; first scalar; second scalar raw U32; NUL extension name; remaining point pairs. |
| `0005` | Signed 32-bit count; that many point pairs; selector; amount; extension flag; first scalar; second scalar; NUL extension name. |
| `0006` | Same as `0005`, with one discarded raw U32 **before** the extension name. |

Words without a repeated width above remain raw 32-bit words. `CopyMetadataWordBits()` preserves all non-point DATA words in serialized order, including explicit counts and discarded words. `CopyPoints()` preserves the ordered raw pair array. The complete source snapshot remains independently copyable.

The legacy remaining-count helper `00A94EE0`, called with eight, divides remaining bytes by eight. The modern decoder requires divisibility by eight; this is a stricter malformed-input policy. Explicit legacy counts use a signed `jle` test (`00B7469D`, `00B747ED`): nonpositive counts skip the point loop and proceed to metadata. The modern decoder rejects negative counts. It accepts zero, one and two points as serialized records without claiming they form valid evaluator geometry. Explicit count disagreement and trailing DATA are rejected.

## Raw values, defaults and runtime separation

All raw F32 bit patterns are retained, including infinities, signaling/quiet NaNs, signed zeros and subnormals. `HasFinitePoints` and `HasFiniteSerializedFeatheringAmount` inspect exponent bits separately from acceptance; unknown opaque records return no finite conclusion. Typed empty point arrays are vacuously finite. No amount range clamp is imposed: amount setter `00B65FC0` stores directly at +18, and the recovered loaders contain no clamp.

Construction initializes selector +14 and amount +18 to zero; the point vector starts empty. BPOL initializes extension flag +38 false, first scalar +3C zero, second scalar +40 to raw `0x40000000`, and name +44 null. These are runtime defaults, not invented serialized fields: absent metadata properties stay null. In particular, version `0003` does not serialize the second scalar.

`00B738B0` recomputes four bounds fields (+28 through +34) from the loaded point array, starting minima at `0x7F7FFFFF` and maxima at `0xFF7FFFFF`. It does not rewrite serialized point order. The decoder deliberately publishes no computed bounds. Query `00B73D00` and layer call site `00B63FAF` establish later runtime use; their arithmetic and ancestor/filter combination are outside this input contract.

## Ownership, limits and opaque policy

The decoder publishes immutable owned values only after structural and budget preflight. Source bytes, header/name bytes, point array and metadata array are isolated from caller mutation. Copy accessors return independent arrays; the exposed point collection rejects mutation through `IList`. A failed later decode cannot alter an earlier result. Managed temporary allocations are reclaimed by the runtime; no timed heap-cleanup claim is made.

Defaults are 262,144 input bytes, 64 IFF nodes, depth 8, 8,192 points, 4,096 bytes per NUL-terminated string, 8,192 combined string bytes and 262,144 logical decoded-copy bytes. Options are checked before parsing; point counts, input extent and string scans are bounded before typed allocation. The decoded-copy charge is `8 * pointCount + 4 * metadataWordCount + headerNameLength + headerLegacyLength + extensionNameLength`. It counts retained array payloads, excluding terminators, encoded snapshots, fixed objects and temporary fixed-size parsing overhead; it is not a heap measurement.

Unknown BPOL versions remain whole-record opaque, with null header/metadata and no typed point interpretation. Recognized BPOL with unknown IHDR version also remains opaque without interpreting DATA. Outer framing, input/node/depth bounds still apply. Opaque retention is a modern forward-inspection policy, not proof the legacy client safely accepts those versions. Legacy unknown-version diagnostics and malformed extent recovery remain unresolved.

Malformed or over-budget input throws `InvalidDataException`; invalid limit options throw `ArgumentOutOfRangeException`; a null section throws `ArgumentNullException`. Truncated forms, wrong tags/order, unterminated names, invalid point extents, negative counts, count budgets and aggregate copy budgets have deterministic negative fixtures.

## Lifecycle and compatibility consequence

Legacy destructor `00B73C00` frees extension name +44, then calls `00B73740`, which frees the bulk point vector +20 and clears capacity/pointer/count. Boundary teardown ultimately frees the IHDR name through `00B62EF0`; deleting wrapper `00B73BD0` optionally frees the object. The modern result has only managed owned values and no external handles or callbacks.

A replacement must preserve recognized layouts, raw bits and point order at this boundary. It may use different internal memory, safer limits and immutable ownership. Extension meanings, axes, units, influence arithmetic and exception/precision behavior require separate evidence before runtime implementation. The server `MMOCoreORB/src/terrain/layer/boundaries/BoundaryPolygon.h` recognizes `0005` and `0007`; its `0007` extra word follows the string and must not be confused with this client's `0006` pre-string word.

## Verification

Independent Python oracle (external historical reference) and native scenarios (external historical reference) agree on 670 fixtures (146 accepted, 524 rejected), 19 ownership checks and all 57 pinned original records. Static verification checks 71 instruction anchors, exact read widths, version dispatch, raw stores, header reads, callbacks and teardown. Original polygons contain 406 points (4–18 each), all `0005` with IHDR `0001`; 54 are locally enabled. Selector distributions 0/1/2/3 are 19/3/2/33, and four extension flags are nonzero. All original points and amounts are finite. These are corpus observations, not wider format coverage or extension semantics.
## Round40 layer and CREO4 composition

[Fresh polygon influence](TERRAIN_BOUNDARY_POLYGON_INFLUENCE_CONTRACT.md) now evaluates the independently verified finite ordered-point graph. Raw decoding remains unchanged; original query profiles/exception controls and edited runtime state remain unknown.


## Narrow downstream semantic closure (October 13, 2026)

The raw decoder above deliberately retains neutral extension names. Direct
Pre-CU consumer evidence now establishes local-water use of flag+38,
height+3C, shader size+40 and shader name+44, with ordered point components
mapped to worldX/Z, for the admitted original Tatooine BPOL0005 pond records.
See the [local-water contract](TERRAIN_LOCAL_WATER_CONTRACT.md) for exact
addresses, input identity, confidence and modern implementation limits. This
does not retroactively assign every unknown BPOL version or consumer semantics.
