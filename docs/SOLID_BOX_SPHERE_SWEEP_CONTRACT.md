# Continuous sphere against a solid oriented box

## Scope

`SolidBoxSphereCollider` owns a captured `PortalBarrierCollider` bounds/pose value. It accepts the existing finite rigid right-handed pose tolerance and models the transpose mapping as the box's local frame. Approximate authored rotations therefore retain that established numerical policy; exact arbitrary affine scaling is unsupported. Coordinates/translation are bounded to±1,000,000, positive sphere radius to100, and each world translation component to100. Degenerate zero-thickness box axes are accepted. This query is a modern compatibility implementation, not recovered original controller behavior.

## Geometry and contact

The squared distance from sphere center to the solid local box is the sum of squared coordinate distances outside each interval. A center inside the solid has distance0 and is an initial overlap even if no surface lies within the sphere radius. This avoids treating the solid as a hollow triangle shell.

Center-coordinate crossings of the six box face planes partition time into at most seven intervals. Within each interval the squared distance is quadratic. Stable roots locate the first closed radius crossing; face, rounded edge and rounded corner contacts use the same equation. No radius-expanded cube substitutes for rounded features. Precise double path coordinates and contact centers are preserved.

Default initial touch blocks. Optional separating policy permits a non-strict touch with nonnegative outward-dot-motion; strict overlap remains held at parameter0. Strictness uses radius minus1e-9 distance tolerance. Outward normal is transformed and normalized; a center inside the solid reports the nearest exit-face diagnostic normal without depenetrating.

Passage true clears after bounded query validation. Passage false reports blocked/initial overlap. Unknown permission reports `PermissionUnknown` only on geometric contact; a clear geometry query remains clear. This matches the existing barrier status convention. No floor ownership, door animation, stepping or sliding is implemented in the kernel.

## Validation

Native analytic gates cover face/edge/three-axis corner/grazing, rounded-corner clear, deep interior overlap, outward/tangent contact, permission branches, zero thickness, rotated owner and invalid queries. Independent `verify_solid_box_sphere.py` minimizes convex clamped-point distance over time and bisects entry, without face partitioning or quadratic roots.512 synthetic cases, seed2026100113:269 blocked,115 clear,128 initial overlap, maximum contact-time error6.8833827526759706e-15. Fixture SHA-256 `9CCCDE1CB7420EB918E172DA4CE578DDF148C8246E87649549AD4FCC03A1D05E`.

## Precise floor and one-transfer composition

`FloorPathSolidBoxSphereSweep` owns and validates the whole path before any geometric or passage shortcut. It adds all three explicit center-offset components and preserves double segment/global progress. `PortalFloorSphereBoxWalker` snapshots up to64 obstacle records and prepares owned solid colliders before external permission callbacks. All transformed paths are validated even with no boxes. Total segment-by-box work is capped at262,144.

Obstacle metadata uses existing `PortalFloorBox` records. These are obstacle shapes; the actor is a sphere. Participating source and target boxes are queried on both legs, including target contact before source feet cross. Exact-time ties retain caller order. Supported landing subtracts the declared world offset and verifies stationary and swept clearance. Unrepresentable landing holds the initial owner; target parameter-zero overlap retains source ownership. No step or slide is added.

512 independent synthetic slope/seam/rotated-box/policy cases agree:36 accepted/281 obstacle stops/195 floor stops,464 zero/48 one transfers. Native100-contact holds, retreat, snapshot mutation, callback failure, malformed paths, owner transforms and precision rollback gates pass. See the [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_SPHERE_BOX_FLOOR_TRANSFER.md).

Directional permission-barrier and physical leaf capture are composed by `PortalDoorSphereFloorWalker`; the [persistent scene](INTERIOR_DOOR_SCENE_CONTRACT.md) exposes an explicit sphere volume. Closing safety remains an enclosing-cube policy. Preview activation and original occupied-threshold validation follow.
