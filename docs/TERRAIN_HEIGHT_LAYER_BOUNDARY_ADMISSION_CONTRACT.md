# Explicit height-layer first boundary gate

Round 56, 2026-10-04. [TerrainHeightLayerBoundaryAdmission](../../SWG-Godot-Client/poc/Stardust.Core/Geometry/TerrainHeightLayerBoundaryAdmission.cs) evaluates the first boundary gate of one accepted, explicitly selected layer across one complete immutable Layer-phase coordinate grid. Its output says whether filters **would** be admitted. No filter, fractal or height samples are produced.

The inputs are the [owned layer slice](TERRAIN_GENERATOR_HEIGHT_LAYER_SLICE_CONTRACT.md), [coordinate grid](TERRAIN_SAMPLE_COORDINATE_GRID_CONTRACT.md), explicit cache/query/combine arithmetic controls, limits and cancellation token. [Native scenarios](../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/TerrainHeightLayerBoundaryAdmissionScenarios.cs), [independent oracle](../../SWG-Godot-Client/poc/round56-tests/Assets/peer.py), [isolated replay](../../SWG-Godot-Client/poc/round56-tests/Assets/Invoke-SelectedRule.ps1) and [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-04_R56_HEIGHT_LAYER_BOUNDARY_ADMISSION.md) make the boundary reproducible.

## Accepted inputs and provenance

`Evaluate(slice, grid, controls, limits, token)` requires `TypedEvidence`, a locally enabled layer, typed enabled BCIR/0002, finite serialized center/radius/feather amount and the accepted slice's exact selected circle child metadata. The existing slice factory establishes the complete BCIR/FFRA/AHFR shape, typed headers, owned generator/family identity, contiguous child extents and global rule index. This consumer retains that immutable evidence; it does not export or decode proprietary input bytes again.

The selected circle is child ordinal 2, with BCIR/0002 tag/version, nonnegative global rule index, a valid extent inside the selected layer and a 64-character accepted hash. The result exposes its original generator/layer identity through `Slice` and the exact child through `CircleSource`. There is no caller-supplied substitute circle or metadata.

Operational AHFR support is unnecessary at this gate. Evidence slices with unsupported height-family modes or curve flags remain eligible if their layer and circle satisfy this contract. Filter enabled state, filter/height eligibility, ADTA word 1, ADTA word 2 and ancestor enabled state do not participate in boundary admission. The accepted slice's `BoundaryInverted` preserves the raw ADTA word 0 nonzero test; it is not a float interpretation.

Negative finite radius remains accepted and is squared by the established circle query. Serialized finite feather amounts outside [0,1] remain accepted and are clamped by that query. Disabled layer/circle, nonfinite geometry/amount, non-evidence policy, invalid controls/options, incomplete shape and exhausted budgets reject. A finite input can still reject later if an intermediate circle cache/distance store is nonfinite.

## Complete grid and exact evaluation sequence

The consumer retains the existing grid/request, including metadata loading, chunk building, Layer and Position arithmetic policies. It uses only `grid.Layer.GetPair(column,row)`, with row outermost and column innermost. Every row/column pair appears exactly once; repeated numerical coordinate values do not merge distinct cells. The complete shape is `Count * Count`; both axes, the Layer phase, parameter count and Position count must agree.

Cache, query and combine precision are independent mandatory choices from 24, 53 or 64 significand bits. The original executable's active arithmetic controls remain Unknown. These choices define a deterministic compatibility model, not original-run parity.

For each pair:

1. `TerrainBoundaryCircleInfluence.Query` consumes the selected immutable circle, first/second coordinate bits and explicit cache/query precision. Its retained value remains unspilled across the next step; `QueryProjectionBits` is an inspection projection.
2. `TerrainLayerInfluenceQuery.FromCircle` retains that exact query instance.
3. The frozen layer combiner receives one enabled boundary entry with the **raw BCIR selector**, boundary gate 1, no filters, filter gate 0, ADTA boundary inversion, filter inversion false, incoming F32 one and the explicit combine precision for both combine/product operations.
4. The frozen receipt's `BoundaryPositive` becomes `Admitted`; its stored `LocalInfluenceBits` becomes `BoundaryBits`.

Selector 0 is identity, 1 square, 2 square root, 3 the established ordered cubic, and any other raw selector shapes to zero. Existing compare/store/early-one behavior and post-boundary inversion are preserved through the [layer influence contract](TERRAIN_LAYER_INFLUENCE_CONTRACT.md) and [circle influence contract](TERRAIN_BOUNDARY_CIRCLE_INFLUENCE_CONTRACT.md). No premature F32 query projection replaces the retained carrier.

The retained `TerrainLayerInfluenceReceipt` is explicitly a **boundary-only probe**. Its product and affector-related fields describe this artificial incoming-one/empty-filter configuration. They do not establish the selected original layer's final product, filter admission outcome after filtering, affector eligibility or numerical height. The useful first-gate statement is solely `BoundaryPositive`.

## Ownership, budgets and publication

Each immutable cell retains coordinates, actual circle query, actual carrier and actual receipt. Reference identities are checked before publication. `Slice` and `Grid` retain the exact immutable input instances. Cells are held in an owned array exposed through a read-only wrapper. `CopyBoundaryBits` and `CopyAdmission` return independent caller-owned arrays; grid/slice exports also cannot mutate the result. Repeated and concurrent evaluations share no mutable cache.

Preflight uses checked finite hard bounds and long arithmetic before the output array or any query/carrier/receipt is created. Default axis/cell/query caps are 256/65,536/65,536; hard maxima are 1,024/1,048,576/1,048,576. Caller budgets may be zero to reject all work. Invalid negative or above-hard options reject.

For `N = Count * Count`, deterministic logical accounting charges 256*N array/storage bytes, 24*N copied entry bytes and 64*N work units. Default caps are 16,777,216 storage bytes, 1,572,864 copy bytes and 4,194,304 work units; hard maxima are 268,435,456/25,165,824/67,108,864. These charges are conservative policy units, not measured managed heap sizes or elapsed time. `CopiedInputBytes` is zero: accepted owned input is retained without exporting source bytes. Caller-requested export copies after publication fall outside evaluation budgets.

The result reports actual cells/query/carrier/receipt counts and the corresponding full-grid charges. This implementation does not claim bounds culling or fewer queries for outside cells. Cancellation is checked before allocation, before each query, after each receipt and before final publication. A malformed intermediate or cancellation throws without publishing a partial result. Previously published results remain unchanged.

## Evidence and remaining work

**Confirmed:** deterministic synthetic/native agreement for complete grid ordering, original selected child metadata, all selector shapes, nonzero ADTA inversion, independent precision controls, unsupported-height evidence acceptance, negative radius/clamped amounts, rejection budgets and immutable publication. Static BCIR, ADTA, coordinate and layer-combine anchors are tied to the documented executable SHA-256.

**Unknown:** active original floating controls, original-run numerical parity, ancestor traversal, bounds policy, filter sampling/combination and curves-enabled AHFR sampling. The sole original-data probe is the already pinned Tatooine layer 3 under TypedEvidence. Its unsupported AHFR curves are retained as evidence and do not prevent this boundary-only evaluation. No wider operational-layer search or original executable execution occurs.

The next bounded step is filter admission and FFRA composition for a supported synthetic slice using these retained boundary cells, with first-gate rejection and ADTA filter inversion proven separately. Original layer 3 height sampling still requires independent support for its authored curves.
