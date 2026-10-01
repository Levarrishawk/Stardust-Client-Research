# Continuous stationary triangle / sphere contact

## Modern query boundary

`StaticTriangleSphereCollider` is an engine-neutral replacement query, distinct from original actor participation. It owns nondegenerate triangles with unique nonnegative IDs and sorts them by ID. Default capacity is2,048; explicit capacity may reach65,536. Coordinates are finite within ±1,000,000; query radius is positive and at most100, and each translation component is at most100. Rotation of a sphere does not change this query. Sliding, depenetration, moving geometry, floor locator adoption and controller behavior are outside the kernel.

The sphere follows a continuous segment. Offset face planes, bounded edge cylinders and vertex balls cover the exact rounded triangle. Face intersections use cross-product barycentric coordinates; edge/vertex entry uses stable quadratic roots. Double calculations determine earliest time in[0,1]. Exact ties retain the smallest source ID. Normal points from closest triangle point toward sphere center; an initially coincident center uses a diagnostic face normal opposing motion.

This is an exact geometric sphere policy within its numerical bounds. Expanding a box by radius is insufficient at rounded corners and is not used as narrow phase. Optional conservative swept AABB filtering uses outward rounding and1e-9 padding, then the same exact feature query. It scans source bounds linearly; no additional hierarchy is claimed. `SweepWithStatistics` reports complete source count and candidate count without mutable per-collider counters. Disabling bounds supplies an exhaustive reference.

## Initial touch and overlap

Default closed contact blocks initial touch. Optional separating-contact policy permits non-strict initial touch when the nearest-point outward vector dotted with displacement is nonnegative; convex distance cannot subsequently decrease through that active boundary on the same linear segment. Strict initial overlap holds at parameter0. Strictness uses radius minus1e-9 distance tolerance; interval and feature tests otherwise use double inequalities. Radius0 is explicitly unsupported in this slice.

## Validation

Native controlled gates cover face, edge, vertex, finite-edge grazing, tangent/outward retreat, strict overlap, rounded-corner counterexamples, skinny faces, ownership, stable equal-time IDs, malformed geometry and bounds.512 varied native queries match exhaustive contact records exactly:29 contacts,101 admitted candidates versus65,536 exhaustive triangle queries.

Independent `verify_triangle_sphere_sweeps.py` solves point-to-triangle distance by constrained least-squares active sets. It minimizes convex distance over time and bisects first radius crossing, independently of feature-time quadratics.512 synthetic cases with seed2026100110 produce249 moving contacts,134 clear and129 initial overlaps. Maximum parameter error is1.5543122344752192e-15; maximum normal component error is5.7060532077102266e-08. Fixture SHA-256: `5004CACF5D213A2A7F365BA40285A540217DCD8326B55452EE2173445EEC1C97`. Controlled grazing and skinny-face cases are native analytic gates, separate from this randomized oracle sample.

## Precise floor paths and supported adoption

`FloorPathTriangleSphereSweep` owns and validates all path segments before an early hit. It adds a finite explicit three-component center offset to precise double feet coordinates, then maps contact back to path progress. Maximum4,097 segments and262,144 source-triangle segment work apply. `SingleCellFloorTriangleSphereWalker` subtracts that offset and reuses supported floor landing with stationary and swept clearance checks. A contact without a representable floor locator holds the source. The legacy shared `FloorBoxWalkStatus.BoxStopped` enum denotes obstacle stops in this adapter too; result/contact types identify sphere geometry.

Synthetic flat/slope/ceiling contact,100 repeated holds, retreat and precision rollback pass. The unchanged1,984-triangle hall produces16 boundary stops and zero triangle contacts with center(0,1,0), radii0.5/0.65,1,600 stable repeats and16 retreats. Independent extraction/floor/distance checks agree. This corpus has no positive sphere contacts; synthetic gates supply them. See the [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_SPHERE_FLOOR_ADOPTION.md).

## One-transfer owner composition

`PortalFloorTriangleSphereWalker` queries owned world triangles across both precise floor legs and applies the same supported landing checks. Center offset is declared world space and remains constant through a transfer. All colliders participate on both legs; earliest progress wins with deterministic cell/source ordering. Target parameter-zero contact retains the source seam owner; unrepresentable landing rolls back to the initial supported owner. No stepping or sliding is added.

512 independent synthetic portal cases agree on201 accepted/248 obstacle stops/63 floor stops, maximum time error1.22e-15. The original human template chain supplies center(0,1,0), radius0.5 to the unchanged42,986-triangle room route: eight accepted requests,800 stable endpoint repeats and eight reverse transfers. The two low render rises clear without altered geometry or feet clearance. Independent swept-bounds separation proves these original clear paths; positive contacts are covered by synthetic probes and64 native exhaustive comparisons. See the [portal checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_PORTAL_SPHERE_PROFILE.md).

## Next integration

Add sphere contact against captured oriented door/barrier boxes, including interior overlap and rounded features. Original runtime participation and a complete actor controller remain distinct from this modern query.
