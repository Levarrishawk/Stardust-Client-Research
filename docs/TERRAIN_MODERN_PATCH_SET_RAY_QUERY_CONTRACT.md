# Modern retained-set finite ray query

The [ray wrapper](../poc/Stardust.Core/Geometry/TerrainModernPatchSetRayQuery.cs) consumes an actual [round62 surface query](TERRAIN_MODERN_PATCH_SET_SURFACE_QUERY_CONTRACT.md). It copies retained geometry positions, supplied normals, and active indices once. It never rebuilds surface queries, normals, geometry, or set admission. This is explicit modern native-space policy; `OriginalCollisionParity` is false.

## API and identities

`TerrainModernPatchSetRayQuery.Build(surface, limits?, cancellation)` retains the actual surface source and ordered child memberships. `Query(ray, cancellation)` returns a nullable immutable hit. `QueryBatch(ReadOnlySpan<TerrainModernPatchSetRay>, cancellation)` returns a read-only detached list. `GetQueryReservation(count)` provides checked preflight costs. Position, normal, and index exports are cloned arrays.

`TerrainModernPatchSetRay` contains binary64 `Origin`, `Direction`, `MinimumT`, and `MaximumT`. `TerrainModernRayVector` has X/Y/Z components. Direction is **not normalized**: position is `Origin + T * Direction`, so T is a parameter, not generally distance. Hit fields include T, native position, barycentric weights, topology pattern, triangle index, three vertex indices, and normalized interpolated supplied shading normal. `Source`, `Membership`, `Chunk`, `Patch`, `Geometry`, and `Child` preserve actual frozen object identities. Consumers apply their own native-to-engine coordinate adapter after querying.

## Intersection and numerical policy

Every retained triangle is considered in actual chunk/patch/triangle order. Binary64 face-plane intersection is double-sided; neither orientation is culled. The nearest geometrically eligible computed T inside the **closed** interval is selected. Equal **computed binary64 T** values retain the earlier ordered triangle. There is no epsilon tie band, first-surface policy, or face-normal substitution.

A zero computed plane denominator gives a miss, including parallel/coplanar triangles and rounded-underflow denominators. Nonfinite computed T gives a miss. Otherwise, the intersection is projected by dropping the largest absolute face-normal component; closed barycentric containment requires every computed weight in [0,1]. No epsilon expands edges. Degenerate projected geometry cannot enter a valid frozen surface query; the upstream rejection is tested separately.

This does not establish exact arbitrary-real predicates. Very small directions remain legal when any component is nonzero; length-squared underflow does not reject them. Their computed denominator can still underflow to zero, or T can overflow or fall outside the finite interval. Near internal edges, rounding can change containment, triangle selection, or mathematically equal T ordering. Fraction comparison verifies the published fixtures with explicit numerical tolerances; it does not eliminate these limitations.

The normal is computed only after the final nearest triangle has been selected, by interpolating its supplied F32 vertex normals in binary64 and normalizing. A selected-normal length at or below the configured minimum rejects the complete query/batch, without falling back to a farther triangle. Nonfinite barycentric arithmetic rejects. A contained candidate examined as a potential nearer hit whose position exceeds the component cap rejects immediately, even if a later triangle would be nearer. These conservative failures publish no partial result.

## Admission and accounting

Default/hard count ceilings are 16 chunks, 256 patches, 2,048 triangles, and 4,096 rays. Counts, retained identities/order, geometry bounds, and complete construction costs are checked before snapshots or geometry exports. All options are checked before owned arrays, copies, or scan work. Each ray must have finite bounded origin/direction components, a nonzero direction (all signed-zero components reject), and finite `0 <= MinimumT <= MaximumT <= MaximumParameter`. Default component cap is 1,000,000, hard maximum 1e9; default parameter cap is 1e9, hard maximum 1e12. The positive finite minimum normal length defaults to 1e-6, hard maximum 1e9.

For K chunks, P patches, T triangles, and Q requests, conservative declared reservations are:

| Reservation | Formula |
|---|---|
| Own build copy bytes | 216P + 6T |
| Own build array bytes | 512 + 256P + own build copy bytes |
| Own build work | 128 + 64K + 128P + 128T |
| Query copy bytes | 64Q |
| Query array bytes | 256 + 256Q |
| Query work | 128 + 64Q + Q(16P + 128T) |
| Triangle test bound | QT |
| Borrowed surface/set array reservation | actual source aggregate array bytes + actual source borrowed-set array reservation |

Borrowed reservations describe already-retained data, not new copies, work, or measured CLR heap. Build copy/array/work defaults are 1,048,576 / 2,097,152 / 1,048,576; hard limits 4,194,304 / 8,388,608 / 16,777,216. Query copy/array/work defaults are 1,048,576 / 2,097,152 / 2,147,483,648; hard limits 4,194,304 / 8,388,608 / 4,294,967,296. Triangle tests have a hard/default ceiling of 8,388,608. Limits can be reduced to zero where the requested operation fits; empty batches still reserve 256 array bytes and 128 work units.

Batch preflight computes checked QT and all costs before request capture/results. All borrowed rays are validated, then copied and validated again before the result array. Capture is not atomic against arbitrary caller mutation. Cancellation checks occur during construction, per ray validation, between **every triangle**, after scanning, and before publishing the final hit or list. Export methods allocate explicit caller-requested detached copies outside query reservations. Failed or cancelled operations preserve earlier wrappers/results and actual source objects.

## Reproducible evidence

The [own replay runner](../poc/round63-tests/Rendering/Invoke-Ray.ps1), [native scenarios](../poc/Stardust.Core.SmokeTests/TerrainModernPatchSetRayQueryScenarios.cs), and [independent verifier](../poc/round63-tests/Rendering/verify_ray.py) build actual frozen Core APIs offline. The verifier uses exact Fraction face-plane intersections and a three-dimensional Gram barycentric solve, independently of the native largest-component projection. Expected results are produced before native execution. Supplied normal inputs use the frozen independent Decimal/F32 model, with absolute normal tolerance 3e-6; T, position, and weights use absolute 1e-9 / relative 2e-11.

The sealed original-layer modern fixture SHA-256 is `d1adcbb5de1759386c3ece620d1a8cd2d04ba30f35f9515cf21efa1eb99fbaa0`. The executable is identity-checked at SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150` and never executed. Actual frozen source reconstruction checks 400 stored height/normal values before ray querying. The fixture uses an original generator layer with synthetic warm priors and modern arithmetic; no original collision, terrain, normal, material, or control parity is implied.

Final results and exact bounded next action are recorded in the [round63 checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-05_R63_SET_RAY_QUERY.md). No shared status/ledger or engine consumer is modified by this worker.
