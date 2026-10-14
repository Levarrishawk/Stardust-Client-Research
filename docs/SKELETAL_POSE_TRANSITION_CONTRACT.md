# Skeletal pose transition contract

`SkeletalPoseBlender` blends equal, finite, right-handed rigid local-joint matrix arrays (1..8192 joints). Validate every matrix using existing rigid-pose gates, even at endpoints. Weight must be finite0..1. Exact endpoints return owned copies of their input matrices. Intermediate rotation extracts normalized quaternions, uses shortest-arc Slerp then normalizes; translation interpolates linearly. Recompose rigid matrices before hierarchy/world/palette/skin deformation. This is an explicit replacement blend policy, not recovered legacy graph timing or blend arithmetic.

`SkeletalPoseTransition` owns the last published pose and target key. Explicit duration must be positive and <=1 second. Changing target freezes the current published pose as transition source, resets age and blends toward the newly sampled moving target. Zero-time switches preserve the published pose exactly, including interrupted transitions. End age preserves the new target matrix exactly. The source snapshot stays fixed during a transition while target pose may move each tick.

Prepare validates/owns a candidate without publishing. Commit accepts only its own candidate at the current version; stale/double/foreign publication fails. Callers serialize access. This lets the prototype host adapter validate and upload a candidate before committing transition state. No concurrent mutation, world root motion, event dispatch or actor ownership is supplied.

## Original adapter

Movement preview uses0.15-second duration. Accepted simulation ticks accumulate animation presentation time; the render update samples the latest selected idle/walk/run target and publishes a blended local pose once. The host catch-up cap bounds elapsed presentation time. Paused/suspended/discarded ticks contribute no time. Reset/direct diagnostic clip selection resets transition state; shape changes deform the currently published pose and preserve the transition. Material/texture changes retain geometry and transition state.

Actor root feet/heading and door simulation remain independent of local-joint transitions. Original source closure and14-texture cache are unchanged from the [movement preview](MOVEMENT_SKELETAL_PREVIEW_CONTRACT.md).

## Verification and confidence

Synthetic known +/-170-degree yaw fixture takes the short arc through180 degrees, preserving linear translation and exact endpoint bits. Ownership, prepare-before-commit, interrupted continuity, stale/foreign and invalid weight/count/nonrigid/time/key fixtures pass. Native original idle/walk/run quarter-cycle poses produce795 local matrices: three pairs x53 joints x five weights. The independent NumPy double quaternion/translation oracle agrees at max4.1209798062880054e-7, tolerance2e-5. Oracle scope is blend math on supplied native local-pose fixtures; it does not independently recover their raw animation decoding. Existing source/pose evidence is separate.

Original17-source bank identity audit passes; full native/build gates pass. Prototype host validates100 blended updates, complete target convergence and zero-time interrupted-pose continuity while retaining actor/door/root/resources. Existing480 direct clip updates, accepted route, live color/shape/age and GPU material fixtures pass. GPU/headless/capture inspection pass.

Unknown: original transition durations/curves, speed graph weighting, priorities and callbacks. Next: a bounded third-person room view for inspecting the moving actor, with camera obstruction tested against owned original geometry.
