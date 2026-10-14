# Retained modern patch-set surface queries

ROUND62, 2026-10-05 Eastern. TerrainModernPatchSetSurfaceQuery (external historical reference) queries an actual immutable [modern patch set](TERRAIN_MODERN_HEIGHT_PATCH_SET_CONTRACT.md) through once-built actual [frozen surface-query children](TERRAIN_PATCH_SURFACE_QUERY_CONTRACT.md). No geometry, normals, or set admission is rebuilt. This is a modern native X/Z surface policy; `OriginalCollisionParity` is false.

## API, identity, and selection

`Build(TerrainModernHeightPatchSet source, TerrainModernPatchSetSurfaceQueryLimits? limits=null, CancellationToken cancellation=default)` retains `Source` and creates one actual `TerrainPatchSurfaceQuery` per actual retained patch geometry. `Children` is immutable membership in set order then patch order. Each membership retains ChunkIndex, PatchIndex, actual Chunk, actual Patch, actual Geometry, and actual Query. `CopyChildren()` exports a detached reference array. `ChunkCount`, `PatchCount`, `TotalTriangles`, and `ChildBuilds` expose accounting; NormalBuilds, GeometryBuilds, and SetAdmissions are zero.

`Query(x,z,cancellation)` returns nullable `TerrainModernPatchSetSurfaceHit`. A hit retains its actual chunk, patch, geometry, child query, relative list indices, and the child's exact `TerrainPatchSurfaceHit` value. A miss is null. `QueryBatch(ReadOnlySpan<TerrainPatchSurfaceLocation>,cancellation)` returns an immutable detached result list in requested order. Empty batches are valid within budgets. `GetQueryReservation(count)` exposes and checks the same conservative per-request reservation used by scalar and batch admission.

Selection is the first child hit in actual retained set order, then actual patch order, then the frozen child's triangle order. Closed shared edges and corners can match multiple children; the first supplied-order match wins. Reversing the set can therefore change chunk identity at a shared boundary. There is no sorting by request key, nearest-height choice, epsilon, collision material, or fallback after a selected child error. Child interpolation/normal failure throws and does not try another child.

Coordinates and arithmetic retain the frozen child policy: finite bounded native X/Z, ordinary binary64 edge predicates and anchored interpolation, and normalization of interpolated supplied normals. Query normals are shading directions, not original physics contacts or geometric face normals. Arbitrary near-internal-edge exact-real classification is not claimed. historical implementation path (omitted) reflections and other transforms remain the consumer's responsibility.

## Construction bounds and charges

Hard/default caps are 16 chunks, 256 patches, 2,048 triangles, and 4,096 queries. Parent limit ranges and every child option are validated, including empty input. Borrowed set/chunk identity, primitive triangle counts, geometry position bounds, and aggregate construction budgets are checked before membership arrays or child copies. Each frozen geometry has exactly nine positions/normals and at most eight triangles. Actual child area/winding eligibility is enforced by its constructor; a later constructor failure publishes no wrapper. Children and sources are immutable managed objects and need no external teardown.

For K chunks, P patches, and T actual triangles:

| Construction reservation | Copy bytes | Array bytes | Work units |
|---|---:|---:|---:|
| Wrapper-owned | 0 | 512 + 128P | 128 + 64K + 64P |
| Actual child construction | 216P + 6T | 472P + 14T | 64P + 128T |
| Aggregate | 216P + 6T | 512 + 600P + 14T | 128 + 64K + 128P + 128T |

Child copies are explicitly positions 108P, normals 108P, and indices 6T. Areas allocate 8T new bytes. Child array charges additionally reserve 256P for metadata/policy/array overhead. The actual frozen `LogicalOwnedBytes` payload sum is independently checked against 216P + 14T; it includes primitive buffers and cached areas, but excludes headers and policy metadata and is not a standalone copy or total heap figure. These declared logical/conservative charges are not measured CLR heap sizes or CPU instructions.

Default construction copy/array/work caps are 1,048,576 / 2,097,152 / 1,048,576; hard maxima are 4,194,304 / 8,388,608 / 16,777,216. `BorrowedSetArrayReservation` reports the already-retained set's own plus borrowed array reservations separately. It does not charge executed set, geometry, or normal builds.

## Request preflight and cancellation

For Q requests, the conservative reservation is copy 16Q, arrays 256 + 160Q, work 128 + 16Q + Q(16P + 64T), and triangle-test bound QT. The triangle bound assumes every child/triangle might be inspected; bounds checks and early hits can reduce actual work. Request defaults are copy/array 1,048,576 each, work 1,073,741,824, and triangle tests 8,388,608. Hard maxima are copy 4,194,304, arrays 8,388,608, work 2,147,483,648, triangle tests 8,388,608. Count caps can be reduced to zero. Scalar calls use the one-request conservative batch reservation even though they allocate no query arrays.

Count/copy/array/work/triangle caps and every borrowed location are checked before capturing locations. Captured locations are revalidated before result allocation. Arbitrary concurrent caller mutation is not an atomic capture guarantee; the actual owned coordinates used for results must be finite and bounded. Caller mutation after capture cannot alter output. A valid shorter batch is a valid prefix, not malformed byte truncation: this API accepts typed immutable receipts and locations rather than encoded data. Tests separately execute the frozen source builder's truncated-height rejection and preserve prior queries.

Cancellation checks occur during wrapper preflight, before and after each child build/query, during location validation, per batch point, and before publication. Frozen child constructors and triangle loops are synchronous and have no token; cancellation cannot interrupt their internal work. Child `MaximumQueriesPerBatch` validates its own API; the wrapper invokes child scalar queries and applies its separate aggregate batch cap.

## Reproduction and evidence

Native scenarios (external historical reference) build actual frozen receipt fixtures as setup, then verify set/patch/geometry/child/hit identity, scalar/batch equality, immutable and detached membership/results, inspected-copy mutation, repeated/concurrent reads, precancellation, late interpolation failure, and prior preservation. Production contains no reflection; fixture setup retains prior test-only signed-zero metadata construction.

The independent oracle (external historical reference) uses frozen independent coordinate/storage/normal evidence, exact Fraction triangle/barycentric containment and height, and independently weighted normal normalization. It never reads native output to seed expected values. Reference checks use height/weights absolute tolerance 1e-9 and relative 2e-11; normals use 3e-6 because the frozen normal-field inputs are checked against a 100-digit Decimal reference. Tolerances validate fixtures and do not change production hit selection.

Final replay: 289 cases, 228 accepted / 61 rejected, 10,102 hits and 753 misses compared, and 14,978 ownership checks. Coverage includes all 16 seam masks, coordinate profiles, empty/single/multiple/disconnected/reversed sets, shared edges/corners, representable outer misses, signed-zero coordinates, invalid/nonfinite locations, cap equality/one-less/negative/hard limits, typed-source truncation, child construction/query rejection, and cancellation. The combined maximum of 16 chunks / 256 patches / 2,048 triangles / 4,096 queries passes with QT=8,388,608 and work=553,713,792; either one-less budget rejects before request copies/results.

One SHA-pinned accepted original-layer modern fixture is reconstructed through actual frozen height/normal/patch/set setup. All 400 height/normal values retain their accepted snapshot; 43 aggregate queries independently compare triangle/hit/height/normal results. The resulting wrapper retains four chunks, 16 actual children, and 128 triangles. Synthetic warm priors and modern curve arithmetic remain explicit; no original collision/terrain/normal parity is inferred.

The offline runner (external historical reference) SHA-checks the executable and sealed private fixture before output/model/build work, uses cached dependencies and fresh ignored output, and passes nine input/containment guards. Native/Core builds finish with zero warnings/errors. Existing engine/shared files and all frozen input files remain unchanged. A separately authorized consumer can now route grounding through the aggregate query while retaining its explicit modern policy and identities.
