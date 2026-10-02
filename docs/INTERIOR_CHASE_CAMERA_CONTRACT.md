# Interior chase-camera contract

The engine-neutral `ChaseCameraObstruction` owns selected room triangles and accepts live world-space door boxes. Target/desired positions are explicit: target is authoritative actor feet plus the original FCHS posture-height result; desired is target minus normalized look direction times selected authored zoom. The Godot adapter uses posture0, no active state, appearance metric1 and maximum-zoom multiplier1 as explicit preview inputs. These constants do not recover the original appearance metric or runtime preferences. [Original profile](FREE_CHASE_CAMERA_PROFILE_CONTRACT.md).

## Explicit replacement policy

Sweep a radius0.12 sphere from target toward desired against owned room triangles in the rigid building frame and live door boxes in world space. Choose the nearest contact; exact ties retain the room result. Retract0.025 before contact, clamp to zero, and hold at target on initial overlap. Do not depenetrate, move an actor or modify doors. Require finite bounded vectors, distance0..100, rigid owner, up to32 door boxes and up to65,536 triangles. Current original interior has42,986 selected triangles and two live door leaves.

The authored FRST0.5 threshold chooses character visibility: hide at resolved distance<=threshold, show at greater distance. First-person and overview remain explicit controls. Third-person now uses [camera-cell tracking](CAMERA_CELL_TRACKING_CONTRACT.md) for portal room selection. Unresolved ownership shows all three owned rooms; this conservative fallback does not reproduce legacy DPVS. Camera smoothing, target metric derivation, mount offsets, exact original collision filters and zoom input policy remain open.

## Ownership and renderer boundary

The original profile joins the existing cache within the archive worker scope. Complete scene closure is148 unique sources/16,612,055 bytes, including the two owned movement clips; every external source is read once. Camera queries consume snapshots after archive disposal. Camera/zoom/view changes retain actor state, door state, skeletal pose/topology/material identities and texture cache. Reflect world positions/look directions once at Godot publication.

This integration exposed an incorrect extra triangle reversal in both static and skeletal upload. The corrected adapter preserves source indices after Z reflection. Original source geometry and the two-sided GPU probe establish the corrected boundary; see [geometry facing](GODOT_GEOMETRY_FACING_CONTRACT.md). Earlier inside-out captures do not establish outward-face rendering.

## Validation

Native synthetic wall/owner/door/clear/initial-overlap/negative fixtures pass. `--original-interior-chase-character` uses the full existing pinned original-interior command arguments and emits360 boom queries plus owned live-door snapshots. `verify_original_interior_sphere.py` resolves all original source identities and independently decodes room triangles. `verify_chase_camera.py` uses convex distance minimization and bisection over6,280 candidate triangles plus live boxes:360 results/224 blocked/zero initial overlap agree, maximum distance error6.80139e-6 at2e-4 tolerance. Door-controller behavior is not independently reimplemented by this audit. Original profile fields/135 height queries also agree.

GPU64 owner/yaw/pitch/zoom cases include34 obstructed views, near-mode visibility, unchanged actor/resources and restored first/third/overview controls. Original2,880-tick/24-transfer/182-contact route and retained locomotion, blend, color, shape, detail and material proofs still pass. Full build/native/GPU/headless/cancellation and corrected visual captures pass. [Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_INTERIOR_CHASE_CAMERA.md).

[Distance recovery](CAMERA_DISTANCE_RECOVERY_CONTRACT.md) is implemented with immediate retraction and explicit exponential release; camera-cell tracking uses the final displayed eye. Next: original cell-light records.
