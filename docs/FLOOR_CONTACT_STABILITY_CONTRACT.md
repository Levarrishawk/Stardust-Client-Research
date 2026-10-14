# Stable returned floor contacts

## Observed failure

The [detail-instance screen](DETAIL_DOOR_INSTANCE_READINESS.md) exposed six actors whose next tick returned InvalidStart after a previous FloorStopped contact. The affected selections are black-sun transport 2/0, 2/1, 5/1 and 7/0; Tatooine large house 4/2; light Jedi enclave 1/3. First failures occurred at frames 24, 24, 23, 23, 30 and 29 respectively. Source floor-edge intersections were calculated in double, then their XZ positions rounded to float. A rounded contact could lie beyond the same triangle's strict barycentric start tolerance.

Observation: the locator was authored by the replacement walker itself, not supplied as invalid external input. Confidence: Confirmed native trace and independent geometry. Consequence: make returned contacts reusable while preserving strict start validation. This is modern numerical policy; original actor solver tolerance remains Unknown.

## Bounded output canonicalization

[SingleCellFloorWalker](../../SWG-Godot-Client/poc/Stardust.Core/Assets/SingleCellFloorWalker.cs) keeps its existing 1e-8 barycentric start tolerance. At a returned edge stop, it checks the rounded point against the current triangle. If already valid, the point is unchanged. Otherwise it tries inward normals of violated edges, then a direction toward the triangle center, with increasing offsets. Each candidate is projected onto the same floor plane and must pass the unchanged start test.

The final three-dimensional correction is limited to **1e-4 metres (0.1 mm)**. The theoretical edge parameter and edge/portal identity are retained. This is an explicitly bounded output bias toward an existing floor, not a global acceptance tolerance. Thin triangles motivated edge-normal correction: movement toward a distant center can round tangentially without reaching the interior.

If no candidate fits the budget, `UnrepresentableContact` returns the validated initial triangle/position with parameter zero and no crossing. It does not return an unusable contact, teleport to a triangle center or retry implicitly. Callers already map this unsupported floor result to FloorStopped. At very large coordinates, one float ULP can exceed the correction budget; the explicit hold is intentional.

## Verification

[Seeded synthetic gates](../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/FloorContactScenarios.cs) exercise 5,000 noninteger triangular boundaries, including 1,000 at large coordinates. Results include 1,109 corrected contacts and 501 precision-budget holds. Every returned locator is reusable, valid contacts allow inward retreat, correction stays within the geometric budget and external outside starts remain rejected.

[Independent installed floor verification](../../SWG-Godot-Client/tools/python/verify_floor_walks.py) preserves all 17,658 expected line/edge results over 4,441 eligible floors (4,730 input records). All 17,658 returned locators pass a fresh native no-op start check. Outcomes remain 8,846 Accepted, 4,370 BlockedEdge, 3,970 PortalBoundary, 384 OpenBoundary and 88 UnsupportedEdgePolicy. No installed corpus fixture needs the precision hold. Initial tighter/centroid-only approaches caused explicit holds; the final edge-normal policy passes the complete corpus without changing those outcome counts.

The 782-candidate actor screen retains 131 exact round trips and 113 unsupported-nonlevel results; all six former InvalidStart actors now end Accepted after retreat. Native ticks remain 31,980. Independent YT-1300 14-link / 56-probe transfer and 948-tick cantina actor regressions pass. Core smoke and Godot build/headless Mustafar controls pass.

## Remaining work

The [piecewise trace contract](FLOOR_PATH_TRACE_CONTRACT.md) now exposes triangle-plane paths with independently verified height/ordering. Next: collision along those segments. The current door actor remains level-only. Full actor/world collision, moving-leaf overlap and original global ordering remain separate.

## Precise-path returned height follow-up

Rounded XZ inside the same triangle is now reprojected onto its plane, rather than retaining height computed at an unrounded edge. A correction exceeding the existing 0.1 mm budget yields UnrepresentableContact and a validated source hold. The black-sun transport 15/0 screen exposed a 0.307798 mm plane mismatch; no external-start tolerance was changed. The 1,000 thin steep-plane gates pass 593 bounded/accepted outputs and 407 explicit holds. All 17,658 installed floor probes remain reusable with unchanged outcomes. The [precise actor contract](PRECISE_DOOR_ACTOR_CONTRACT.md) records the expanded doorway screen and original slope gates.
