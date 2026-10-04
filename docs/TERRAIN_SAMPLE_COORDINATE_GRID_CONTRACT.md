# Terrain sample coordinate grid contract

[TerrainSampleCoordinateGrid](../poc/Stardust.Core/Geometry/TerrainSampleCoordinateGrid.cs) implements the bounded conditional graph established by the [coordinate producer evidence](TERRAIN_SAMPLE_COORDINATE_PRODUCER_CONTRACT.md). It consumes supplied raw metadata/work values; it does not recover a live work request, choose active runtime profiles, execute a terrain layer or generate heights. Metadata loading, chunk building, layer-coordinate arithmetic and position-coordinate arithmetic are four mandatory independent nearest-even Binary24/53/64 profiles.

## Inputs, arithmetic and evidence

`TerrainSampleCoordinateRequest` retains raw `ChunkWidthBits` and signed Int32 `Tiles`, `Margin`, `First`, `Second`, `Scale`. Signed integer loads are exact before the next arithmetic instruction. The first/second names preserve the observed column/row order; the joined native first/height/second vector corresponds strongly to X/Y/Z. Scale naming remains strongly inferred. No default margin or work value is guessed.

Let `S` be nearest-even F32 storage and `R_l/R_b/R_q/R_p` nearest-even significand rounding at the selected loading/build/layer/position precision. Integer loads are not prematurely rounded to that significand width.

```text
derived = S(R_l(chunkWidth / signed(tiles)))
scaleStored = S(signed(scale))              # FST inspection slot; exact integer remains on stack
step = S(R_b(R_b(signed(scale) * derived) * 0.5))
marginStored = S(R_b(signed(margin) * step))
firstOrigin  = S(R_b(R_b(signed(first)  * chunkWidth) - marginStored))
secondOrigin = S(R_b(R_b(signed(second) * chunkWidth) - marginStored))
count = checked(2 * checked(margin + tiles))
layerFirst[column] = S(R_q(R_q(signed(column) * step) + firstOrigin))
layerSecond[row]   = S(R_q(R_q(S(signed(row)) * step) + secondOrigin))
positionFirst[column] = S(R_p(R_p(signed(column) * step) + firstOrigin))
positionSecond[row]   = S(R_p(R_p(signed(row)    * step) + secondOrigin))
verticalOrigin = positive-zero F32
```

The unused scale FST is recorded as `ScaleStoredBits`; the step uses the exact integer still on the x87 stack. The layer row spill is preserved. Position row/column and layer column have no intermediate integer F32 spill. Under this bounded count<=1024, all row indices are exactly representable in F32; that fact does not justify unifying the independently supplied layer/position arithmetic profiles. No double PC64 substitute, reassociation or FMA is used. Frozen internal dyadic `Number` arithmetic is reused within Core; the helper stays internal and its source is unchanged.

| Observation | Interpretation / confidence | Compatibility consequence |
| --- | --- | --- |
| PTAT0013/0014 `00B6EDE2/EA/FA`, `00B6F100/108/118`; chunk builder `0084A5A0..EA`. | Confirmed signed division, scale/half, margin and ordered origin graph. | Preserve each F32 storage and arithmetic rounding point. |
| Layer `00B63F2F/32`, `00B63F5A..6F`, boundary call `00B63FAF`; position `00B65790/98/9B/9E`, `00B657A8/B1/B6/B9`. | Confirmed row spill, exact other integer loads, first/second pair order and separate coordinate stores. | Expose separate layer and position receipts. |
| Client constructor `0084937C` calls base `00B5D7C0`; `00B5D7F1/F8` write member+58=3 and member+5C=2. | Confirmed fresh-object stores, independently checked after the lead's observation. | A fresh margin candidate3 is evidence, not a current-state/configuration/default guarantee. Member+5C is not used as this API's scale argument. |
| Actual work values, later member writes and phase control-word mutation. | Unknown beyond the bounded accepted chain. | Keep request and four profiles explicit; no original runtime coordinate claim. |

Executable SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150` is checked before PE/VA decoding. The verifier reuses the accepted58 anchors/half constant and independently checks nine additional anchors: unused scale FST, count add/shift, zero vertical value, position integer loads and the constructor call/stores. Original execution and generated-coordinate comparison are absent.

## Modern eligibility and duplicate axes

`Build(request, metadataLoading, chunkBuilding, layerCoordinates, positionCoordinates, limits=null)` requires positive finite raw chunk width, positive signed tiles and positive signed scale. Negative margin is supported if the checked count is positive. First/second signed values span the complete Int32 domain. These restrictions and checked count arithmetic are modern policies; legacy signed overflow, nonpositive loop skipping, allocation failures and traps are not reproduced.

Every observed stored parameter/coordinate must remain finite. Unspilled intermediates use wide exponents; only observed F32 stores are tested for overflow. A finite derived spacing or step that underflows to zero remains valid. Finite duplicate stored axes from underflow or large origins are preserved exactly; the producer does not reject, repair, nudge or deduplicate them.

`FirstStrictlyIncreasing`, `SecondStrictlyIncreasing` and `HasStrictlyIncreasingDisplayAxes` compare the stored finite axis values. These flags report the strict-axis precondition for the [supplied height patch](TERRAIN_CIRCLE_HEIGHT_PATCH_CONTRACT.md), not complete geometry eligibility. Display position limits, sample/cell shape, tiny/degenerate triangles and the height grid require separate checks. In particular count is even, while the supplied modern patch requires an odd active width; this API introduces no implicit crop/resampling or padding mapping.

## Results, limits and ownership

`Request` and `Policies` are immutable value receipts. `Parameters` reports count, derived spacing, scale store, step, margin, first/zero-vertical/second origins. `Layer` and `Position` each own inaccessible first/second arrays and expose read-only views. Copies are independent; `GetPair(column,row)` returns an immutable raw-bit pair with row/column indices. `CopyPairs()` produces a fresh row-major array; it does not retain caller memory or expose engine-specific coordinates.

Defaults permit count<=256 and count-squared<=65536. Hard maxima are1024 and1048576; caps must be at least2/4. These limits precede axis allocation. Count is checked as signed Int32 sum then multiplication by two before modern budget checks. There is no empty single grid; negative/zero count and count overflow reject. All four axes are computed before the result is returned; a later nonfinite axis leaves no published partial grid.

`BuildBatch(requestSpan, fourProfiles, limits=null, batchLimits=null)` supports an empty batch. Default request cap64 and aggregate logical pair cap1048576; hard maxima4096 and16777216, with zero permitting only empty work. The logical pair charge is count-squared per request, covering the two phase descriptions of each location once. It is not a byte count or two separately allocated pair arrays. Configuration, request/count validity and aggregate charges precede computation; requests are copied, grids computed into owned candidates, and the batch returned only after complete success. Mutation after return, aliasing spans, later invalid requests and earlier retained results are tested. Concurrent mutation during this synchronous call is unsupported.

Construction retains four count-sized arrays per grid and bounded immutable metadata/wrappers; it does not allocate count-squared pairs. Pair arrays are allocated only by explicit caller copies. The count-squared cap bounds that subsequent accessor. Work is fixed dyadic arithmetic per parameter plus four coordinates per index. Operand widths/exponents and count/batch caps bound work; no unbounded numeric iteration is introduced. Caller-retained grids/copies and CLR reclamation/heap/RSS are outside the logical per-call accounting.

## Verification and reproduction

[Public API scenarios](../poc/Stardust.Core.SmokeTests/TerrainSampleCoordinateGridScenarios.cs) use no internal helpers across the actual Core assembly boundary. The [independent Fraction/integer oracle](../poc/round38-tests/Rendering/verify_grid.py) computes every stored parameter, both axes, every copied pair, strictness flags and raw request/profile receipts. It covers all81 profile combinations, signed work indices and margin, large integer operands/origins, subnormals/underflow, duplicate axes, late finite-store overflow, policy/cap/count failure and all four policy positions.

[Invoke-Grid.ps1](../poc/round38-tests/Rendering/Invoke-Grid.ps1) uses explicit cached Python/.NET/static packages, copies existing BCn dependencies into a private cache, clears feeds, disables shared compilation/apphost, builds the actual Core-reference runner and actual separate Core/SmokeTests projects, then invokes the CPU DLL. [Identity tests](../poc/round38-tests/Rendering/test_identity.py) prove modified and truncated original inputs reject before decoding and fixture/receipt creation.

No GPU, corpus sweep, original assets in public fixtures, original process execution, live contact or downloads occur. Exact final counts, failed attempts and next probe are in the [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R38_TERRAIN_COORDINATE_GRID.md). Original height/boundary/layer generation remains separate. Next: close actual request/current margin provenance and the crop/padding/active-cell relationship before connecting these even-sized coordinates to an original generated surface.
