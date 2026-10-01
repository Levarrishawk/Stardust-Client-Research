# October 1, 2026 — Captured sphere-box floor transfers

## Question and stopping condition

Can captured solid boxes stop an offset sphere while retaining precise supported floor ownership across one transfer? Stop at stateless captured-box composition, snapshot/permission semantics and independent fixtures; physical door controller composition follows.

## Implementation and evidence

`FloorPathSolidBoxSphereSweep` validates a complete owned path before early contact or permission shortcuts, adds all three explicit sphere-offset components and preserves double contact centers and segment/global progress. `PortalFloorSphereBoxWalker` owns captured collider values before external callbacks, limits snapshots to64, validates all world paths even without obstacles, and caps total segment-by-box work at262,144. Captured `PortalFloorBox` records are reused as immutable metadata; they describe obstacle boxes, not actor shape.

The adapter queries participating source/target snapshots on both legs, retains caller order on exact-time ties, subtracts the sphere offset for landing and rechecks represented stationary/swept clearance against participating boxes. Unrepresentable landing holds the initial owner; target parameter-zero overlap retains the source seam owner. Initial parameter-zero body overlap remains a held validated source with a contact result. No step or slide is implemented.

## Validation

Native gates cover precise path progress/offset, complete malformed later-segment rejection even with passage allowed, source approach to target boxes, slope/global progress,100 stable contacts, reverse transfer, unknown/allowed permission, tie order, caller snapshot mutation during callbacks, failed callback reuse, rotated/asymmetric owners and large-coordinate locator rollback. Full native smoke and Godot build pass with zero build warnings/errors.

Independent `verify_portal_floor_sphere_boxes.py PRIVATE_OUTPUT CORE_DLL` reconstructs slope/seam legs and uses convex solid distance minimization:512 cases, seed2026100114, eight owner poses and varied offsets agree.36 accepted/281 obstacle stops/195 floor stops;464 zero/48 one transfers. Fixture SHA-256 `CE8FD4A3225FE3765F47C599B7F8053BF307BBFE4E6E862C1C9B6DE538E9BF69`. Includes rotated captured solids and false/unknown/true obstacle policies. Geometry details and generated fixtures remain ignored.

## Confidence and consequence

Confirmed bounded modern adapter and sampled agreement. This provides precise sphere-versus-door-box motion with existing ownership conventions. It does not claim original controller behavior or steps, nor change preview actor defaults. No new names or metrics.

Next: compose directional permission barriers and captured physical leaf controllers with this adapter, preserving pre-callback snapshots and contact metadata.
