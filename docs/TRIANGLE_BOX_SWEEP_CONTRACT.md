# Continuous stationary-triangle / actor-box contact

## Scope and ownership

`StaticTriangleBoxCollider` is an engine-neutral replacement query. It snapshots at most 2,048 stationary, nondegenerate triangles with unique nonnegative IDs and sorts them by ID. Coordinates are finite and within ±1,000,000. A query translates a world-axis box; half extents are 0–100 and each displacement component is at most 100. Rotation, deforming geometry, sliding, floor locator adoption and spatial acceleration are outside this boundary.

Thirteen separating-axis candidates are prepared per triangle: world axes, triangle normal and each triangle edge crossed with each world axis. Zero axes are omitted. Double projections determine the common continuous interval. Earliest contact in [0,1] wins; exact ties choose the smallest ID. Clear returns parameter 1, no triangle and zero normal. Contact normal opposes motion along the axis establishing entry. Initial-overlap normals are diagnostic and are not penetration-resolution vectors.

## Touch and overlap

Default policy blocks initial touch. Optional `allowSeparatingContact` ignores a non-strict initial contact when an active boundary has outward or tangent velocity; linear translation cannot move back through that boundary. Strict initial overlap remains rejected. A 1e-9 projected distance tolerance classifies strictness and active boundaries; interval separation itself uses double inequalities. Zero half extents permit ray queries. This policy is modern replacement behavior, not recovered original collision response.

## Independent verification

`tools/python/verify_triangle_box_sweeps.py` generates 512 synthetic cases with seed 20260930 and float32-rounded coordinates, including 128 zero-box rays. Its independent oracle minimizes time in a three-variable linear feasibility problem: time plus two triangle barycentric coordinates. It enumerates all triples of11 constraint planes and checks their vertices; it does not use separating axes. Native results agree: 418 clear, 79 moving contacts, 15 initial contacts; maximum parameter difference 4.440892098500626e-16. Native controlled tests separately establish overlap/touch status, support/outward/inward policy, missed triangle edges, tunneling, owned input, deterministic ties and malformed bounds/geometry rejection.

The deterministic synthetic fixture SHA-256 is `445F0E4E577C434F4BB29403052ADD3B0875AE60B3D0D2F1FB5B841DB82F0D3E`. Detailed generated output stays ignored. Reproduce with the Python verifier, the built smoke-test DLL and an output directory beneath `local-private`.

Confidence: Confirmed within the bounded synthetic query and sampled independent oracle. No original obstacle source is selected. The [selected scene source audit](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_INTERIOR_SCENE_CONTENT.md) found NULL authored collision bounds and no wall/ceiling FLOR triangles. Rendering geometry remains separate from authoritative collision.

## Precise feet-path adapter

`FloorPathTriangleBoxSweep` snapshots and validates all segments before querying. It uses existing double `FloorPathPoint` coordinates directly, offsets feet by box half-height, and returns segment index, triangle ID, exact center and global request parameter. Segment order resolves contacts at equal global parameters, including vertical lift segments whose start/end parameters are equal. A clear result ends at the precise terminal segment; the rounded floor result is used only for an empty path. No result adopts a floor locator or a partially lifted actor.

At most 4,097 continuous segments and 262,144 segment/triangle pairs are accepted. Every segment has finite bounded endpoints, a bounded displacement, nonnegative floor triangle index, ordered parameters within the result prefix and positional continuity within 1e-6. Centers must also fit coordinate limits. All segments are checked before any early contact, so malformed later data cannot escape validation.

Native tests cover global progress through two segments, sloped Y, ceiling contact during a zero-parameter lift, tangent support, large-coordinate double precision, malformed late/discontinuous segments and the work budget. A second independent 512-case LP comparison covers two-segment paths: 357 clear, 145 moving contacts, 10 initial contacts; both segment index and global time match. Maximum difference 2.498001805406602e-16. Its synthetic fixture pin is `9DA86EC6B5CA265173B512F189C1A423D2CBAA56C1BC24DA62C310484A9EF31D`; reproduce with the same verifier plus `--paths`. The original512 straight sweeps pass after the shared-kernel refactor.

## Supported same-cell contact adoption

`SingleCellFloorTriangleBoxWalker` owns a floor walker and triangle collider. It traces the floor prefix, queries precise body contact and reuses the bounded floor-contact landing resolver. A float locator must remain on a valid floor, within 1e-4 of the precise contact, on the outward side within 1e-6, free of strict triangle overlap, and reachable by a separately checked collision-free floor path (endpoint touch allowed). Small outward/path-backward biases use the existing landing policy. If no representable supported contact passes, it holds the validated source with parameter 0 and `ContactLandingUnsupported`. Diagnostic contact retains the rejected proposal's time/center.

Tests establish flat/ramp supported stops, 100 identical repeated holds, outward retreat, rising-floor ceiling contact and large-coordinate source rollback. Empty obstacles preserve clear slope traversal; invalid starts and crossing limits retain floor failure status. Queries are stateless after invalid input. Steps, cell transfers, sliding and persistent scene integration are still outside this walker.

## One same-cell step with triangle clearance

`SingleCellFloorStepTriangleBoxWalker` uses the existing modern step proposal/policy and permits at most one step per request. A source approach contact prevents proposing a step. The accepted lift and inward landing are checked against owned triangles before target continuation. A blocked lift retains the supported source prefix, returns zero steps and `VerticalStepBlocked`; its contact center remains diagnostic and is not adopted. A zero-progress failed target contact similarly rolls back with `StepLandingOverlap`. Otherwise the target continuation uses supported contact adoption and consumes the remaining crossing budget; request progress and crossing counts compose with the source prefix.

Native controlled tests establish clear rise/drop, ceiling-held source,100 stable repeats with retreat, approach wall, target continuation wall and excessive-height rejection. This preserves modern bounded step policy, not original collision behavior. Portal transfer, persistent scene integration and original obstacle selection remain open.

## Original source selection constraint

The [selected hidden-helper audit](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_SELECTED_HELPER_GEOMETRY.md) finds 96 triangles across the two omitted room surfaces, with no helper in the connecting hall. Their bounds are disjoint from the current walking route. They fit the query budget but cannot alone enclose that route. Shader names and zero-pass rendering are insufficient evidence to select legacy collision geometry; trace the original static-mesh eligibility boundary before adopting an original obstacle set.
