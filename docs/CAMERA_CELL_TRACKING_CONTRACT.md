# Camera-cell tracking contract

`PortalCameraCellQuery` owns selected PRTO aperture triangles and reciprocal reference relationships. It follows the segment from a trusted actor-cell target anchor to the collision-resolved camera position. This is explicit modern presentation policy, not a general point-in-room classifier or recovered legacy DPVS ownership algorithm. It requires a valid anchor and a boom already constrained by room/door collision. It does not prove that arbitrary visual meshes form closed room volumes.

## Traversal and fallback

Select the nearest remaining aperture intersection, cross to its reciprocal target, and continue with strictly increasing segment parameters. Ignore intersections before the last crossing; repeated triangles of the same tessellated aperture do not cause repeated transfers. Use double plane/barycentric arithmetic with source F32 vectors. Boundary plane distance<=1e-5 and barycentric tolerance1e-6 are explicit replacement tolerances. At an anchor/end on an aperture, return `AmbiguousBoundary`. Coincident different portals, unpaired references, unloaded target cells and crossing-budget exhaustion return unresolved statuses with no guessed cell. Prototype host shows all owned rooms for every unresolved result.

Require1..64 unique selected cells, valid bounded geometry/references, up to4,096 layout cells/geometries,32,768 total references,128 references per selected cell,8,192 nondegenerate selected incidence triangles and configurable0..128 crossings (default32). Snapshot arrays/selection; preserve source ordering. Degenerate aperture triangles do not participate. Finite coordinates must be within one million; world overload accepts a validated rigid owner and transforms anchor/camera to layout space. Query results own immutable crossing snapshots.

## Prototype host integration

First-person uses the authoritative actor cell. Third-person starts at the same trusted target anchor but determines the camera cell along its resolved boom. Portal visibility then receives the actual camera eye transformed to layout space, its look direction and the resolved source cell. Overview and unresolved locations show all selected rooms. Camera room and visible-room count are available in inspection status. Room/leaf visibility does not move actors/doors, unload decoded sources or replace skeletal resources. [Camera obstruction](INTERIOR_CHASE_CAMERA_CONTRACT.md).

## Evidence and limits

Synthetic forward/reverse/two-crossing/aperture-miss/zero-segment, boundary/unloaded/unpaired/budget fallback, ownership/owner transform and negative fixtures pass.28 original doorway samples across two paired portals and both directions produce26 located results,13 camera-cell changes and two ambiguous endpoints. Independent `verify_camera_cells.py` reads original PRTO bytes and uses segment-ray intersections plus closest-point boundary checks; all statuses/cells/crossings agree, maximum parameter error3.85904e-6 at2e-5 tolerance. These representative samples do not establish all possible aperture arrangements.

Prototype host64 owner/yaw/pitch/zoom views validate source-room inclusion and node publication. Two additional original doorway runs place the actor in the hall while the camera remains in the entrance room, once per owner pose. Visibility toggles preserve simulation state. Original route/retained animation/live controls/material proofs, full native/build/GPU/headless and visual capture pass. [Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_CAMERA_CELL_TRACKING.md).

[Distance recovery](CAMERA_DISTANCE_RECOVERY_CONTRACT.md) is implemented with immediate collision retraction, preserving actual-eye room selection. Original cell-light descriptions are next.
