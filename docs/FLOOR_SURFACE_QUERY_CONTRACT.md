# Deterministic floor surface inspection

`FloorSurfaceQuery` owns positions/triangle indices and builds a modern deterministic median-split bounding hierarchy with eight-triangle leaves. Splitting uses longest bounding axis, centroid order then authored array ordinal. Empty floors are valid. Limits are one million vertices/triangles. This hierarchy is independently authored; original BTRE is retained by the reader but not consumed here.

## Query policy

Finite start/end coordinates define a closed segment. Zero-length, coplanar and degenerate-triangle cases return no surface hit. Two-sided double-precision triangle intersections return nearest segment parameter, triangle ordinal/authored ID, hit position, barycentric coordinates and geometric winding normal. Authored stored normals remain untouched and are not used for intersection. Equal exact hit parameters choose the lowest triangle ordinal. Non-solid means any nonzero authored byte; those triangles are excluded by default and can be included explicitly. No epsilon expands triangle interiors in the native intersection. Floating boundary results are a modern numeric policy, not a recovered original guarantee.

Queries do not consult edge flags, floor locators, cell permission or portal ownership and cannot decide navigability. They are inspection tools, not the original FloorLocator/path-walk behavior.

## Cell ownership and adapter

`PortalSceneBundleReader.Load(..., loadFloors:true)` loads floor paths through the same source-count/encoded-byte budget as POB and appearances. Repeated floor paths share immutable decoded resources. Missing/invalid floors are local diagnostics; aggregate budget failures remain fatal. Source identities include every loaded floor.

Prototype host `--interior-floors` supplies a floor-only toggle; green surfaces are solid and orange surfaces non-solid. Named-cell selection filters appearances and floors independently. Right-click projects the camera segment through a reflected engine-neutral query and displays triangle/ID with a hit marker. The host verifies reflected uploaded positions/indices and picking for every available horizontal solid sample. No traversal, DPVS, door or permission behavior is implied.

## Validation

[Checkpoint and exact reproduction](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_FLOOR_SURFACE_INSPECTION.md). Independent exhaustive plane/barycentric tests agree on 32,918 probes across all 4,730 deployed floors, with 26,317 hits. Native tests cover 1,000 seeded analytic-grid cases, reverse nearest-layer selection, closed endpoints, same-surface ties across BVH leaves, empty/degenerate/coplanar cases, non-solid filtering, caller mutation and nonfinite rejection. Original seven-floor scene identities/field hashes and headless/GPU adapter checks pass.

## Same-cell surface-to-route selection

`PortalSurfacePathQuery` owns sorted cell-map snapshots and retains immutable floor/graph queries. Maximum 4,096 keys per map; keys must be zero-based cell ordinals. It chooses the closest solid hit, lowest cell on exact ties, then the nearest node in that cell's graph. Missing/empty graph status is explicit. A layout graph is never substituted. Optional cell filtering and finite segment guards apply.

All 149 YT-1300 solid horizontal centroid probes agree independently on cell/triangle/node/status and positions. Prototype host preserves the typed reflected pick, switches to that cell and sets its route-start control; seven original sample connections pass headless/GPU gates. This remains spatial inspection, without original walking, costs, permissions or cross-cell transitions. See [connection checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_SURFACE_ROUTE_SELECTION.md). Reproduce with `verify_portal_scene.py ... --surface-paths`.


## Bounded single-cell movement — 2026-09-30

The [movement contract](SINGLE_CELL_FLOOR_MOVEMENT_CONTRACT.md) now separates original edge predicate/caller observations from a modern owned horizontal walker. Prototype host floor inspection exposes two-pick attained-contact/status visualization. Missing-neighbor portal/open-floor exits and special collision edges stop explicitly; cross-cell ownership/entry permission and full original collision remain open.
