# Modern query-time slope normal grid

TerrainModernSlopeNormalGrid (external historical reference) supplies immutable triangle normals under the named policy `ModernBinary64QueryTimeTriangleNormals`. Its geometry, stores and finite edge handling follow the [target producer contract](TERRAIN_SLOPE_NORMAL_PRODUCER_CONTRACT.md), whose [static reproducer](../tools/python/verify_terrain_slope_normal_producer.py) closes descriptor/axes/timing evidence. The primitive declares `OriginalNormalParity=false`: binary64 arithmetic and `Math.Sqrt` are explicit modern choices, not recovered effective x87 controls or original output bits. The opt-in ordered slope adapter consumes this primitive; the primitive owns no traversal, surface classification or prototype host state.

## Supplied inputs and ownership

The subsequently accepted [ordered slope adapter](TERRAIN_ORDERED_SLOPE_PREVIEW_CONTRACT.md)
uses this engine-neutral primitive at demanding layer entries. The primitive
itself owns no traversal/dirty lifecycle; the adapter supplies the ordered height
snapshot. See the [joined acceptance](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-13_ORDERED_SLOPE_TEXTURING.md).

`Build(width, spacingBits, origin, currentHeightBits, limits=null, cancellation=default)` accepts a square row-major F32-word height array, positive finite F32 spacing and finite XYZ origin. Width includes every supplied sample, including any caller padding; no additional samples or padding are synthesized. N=1 is accepted and produces one zero normal. Positions are X=column*spacing+originX, Y=currentHeight+originY, Z=row*spacing+originZ, each stored F32. Rounded X and Z axes must remain strictly increasing; collapsed coordinates are rejected.

`Build(TerrainSampleCoordinateGrid, currentHeightBits, ...)` uses that grid's stored parameters and requires position policy `Binary53` (53-bit significand, corresponding to this binary64 arithmetic policy). It checks the resulting X/Z words against the grid's immutable position axes. Metadata/chunk/layer policies remain provenance on the caller's grid; the adapter does not claim they match original controls. Other position policies are rejected rather than silently substituted.

The result retains owned current-height words, positions and normals behind read-only collections. Copy accessors return detached arrays. It keeps no borrowed height array or mutable source reference. Construction snapshots heights; concurrent mutation during construction is not supported. There is no dirty-state lifecycle in this primitive: its caller must select the ordered query-time height snapshot.

## Explicit arithmetic policy

Separate non-inlined add/subtract/multiply operations prevent contraction into FMA. Each uses binary64. Position coordinates, vector differences, each complete cross-product component, each accumulation component and final normalized XYZ are F32-stored. Individual cross-product multiplications are not F32-rounded before subtraction.

For Q00=P(c,r), Q10=P(c+1,r), Q01=P(c,r+1), Q11=P(c+1,r+1), upper=(Q01−Q10)×(Q11−Q01), lower=(Q10−Q01)×(Q00−Q10). Squares are visited row-major. Upper is added to Q01/Q11/Q10, then lower to Q10/Q00/Q01. The initially zero normal array therefore includes only triangles in the finite supplied domain. Faces are not normalized separately. There is no central-difference stencil or default up-vector.

Magnitude is `Math.Sqrt((Z*Z + X*X) + Y*Y)`, with binary64 products and ordered sums. A finite magnitude strictly below F32 literal `3727C5AC` remains unchanged. Equality normalizes. The binary64 reciprocal `1/magnitude` is multiplied into each component before F32 storage. All inputs and stored intermediates must be finite. F32 underflow to zero is allowed; overflow, nonfinite words and collapsed axes reject with `InvalidDataException`. This validation is a declared modern safety boundary, not recovered original malformed-input behavior.

## Bounds, work and cancellation

Default limits are 32 axis samples, 1024 cells, 65536 copied bytes, 131072 logical array bytes and 1048576 work units. Absolute supported caps are 128 samples, 16384 cells, 1048576 copied bytes, 2097152 logical array bytes and 16777216 work units. Invalid limit values throw `ArgumentOutOfRangeException`; exceeded reservations throw `InvalidDataException` before grid array allocation.

For C=N², construction reserves copied bytes `4C`, logical array bytes `256+28C`, and work `128+128C+256(N−1)²`. The arrays are C height words and two C-vector arrays. The fixed allowance covers container bookkeeping. Work is a conservative declared operation reservation covering validation, positions, triangle accumulation, normalization and coordinate-adapter checks, not measured CPU time. Future calls to detached copy accessors allocate their requested exports and are outside construction accounting.

Cancellation is checked before allocation, during height validation, for every position, square and normalization cell, and before publication; the coordinate adapter also checks each axis comparison and publication. Exceptions publish no partial result. Existing immutable results survive failed/canceled subsequent calls. The harness proves pre-cancellation; it does not claim a timing-triggered mid-operation cancellation measurement.

## Focused verification and next adapter

The offline console harness (external historical reference) covers a hand-derived asymmetric 2×2 witness, eight independent integer-incidence grids, flat edges/corners, N=1, finite underflow and short-vector preservation, negative coordinates, exact representable translation, source/copy ownership, coordinate-policy acceptance/rejection, exact and one-less resource budgets, cancellation, malformed shapes, nonfinite values and position/difference/cross/accumulation F32 overflow.

A fractional fixture uses spacing word `3F800001` and Q11 height `3F9E064B`. Independently computed upper cross words are `(BF9E064C,3F800002,BF9E064C)`. Stored upper normal Y is `3EFE77CA`; omitting the cross F32 spill gives `3EFE77C9`. This is a modern numerical fixture and not an original capture.

The next bounded step is an ordered shader adapter that rebuilds a cache at a demanding layer entry after an admitted height affector marks dirty. It must reproduce the target query-time ordering and retain an explicit modern endpoint policy. Final render normals, including constrained fan normals, must not be substituted. Original endpoint FSIN and normal numeric parity remain unclosed; no slope branch is enabled by this primitive.
