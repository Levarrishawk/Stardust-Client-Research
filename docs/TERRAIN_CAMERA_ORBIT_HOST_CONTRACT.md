# Supplied-pivot camera orbit host

TerrainCameraOrbitHost (external historical reference) is a modern prototype host Node borrowing one actual Camera3D around a fixed supplied engine-world pivot. It owns configuration/scalars, checked counters and one primitive diagnostic. It creates no camera, terrain or picker; it changes no projection/FOV/clip setting, OS mouse capture or original input setting. It performs no simulation/network work. Original gameplay/camera/input parity remains unknown.

## API and numeric policy

Attach under a live parent and call once:

`Configure(Camera3D camera, Vector3 pivot, double yawRadians, double pitchRadians, double distance, TerrainCameraOrbitPolicy? policy = null, CancellationToken cancellation = default)`

`TerrainCameraOrbitPolicy` defaults to pitch0.05..1.45 radians, distance0.25..1000, radiansPerPixel0.005 and wheelDistanceStep1. Pitch endpoints are finite, ordered and strictly inside(0,PI/2). Distance endpoints are finite/ordered, minimum at least0.001 and maximum at most1e6. Sensitivity is1e-9..1 radians/pixel; wheel step is positive and at most1e6. Pivot components are finite within1e6; initial yaw is finite within absolute1e12; pitch/distance must be finite and are clamped. Yaw uses IEEE remainder by2PI into[-PI,PI]. Relative mouse components must be finite within absolute1e6.

Position is `pivot + distance * (sin(yaw)*cos(pitch), sin(pitch), cos(yaw)*cos(pitch))`, evaluated in binary64 then cast once to prototype host binary32. Candidate components must remain within2e6. Rounded camera position must differ from pivot and its normalized target direction's cross with Up must have squared length at least1e-8. This rejects binary32 collapse and near-pole LookAt cases before effects even for mathematically admissible policy angles.

Configuration checks constructing historical implementation path (omitted) main thread, active perspective camera, exact live host/camera parents, same viewport, token and numeric admission, then sets actual GlobalPosition and calls actual LookAt(pivot,Up). It commits stable scalars only after both calls and post-mutation guards. Failed pre-effect configuration clears tentative borrowed references; a mutation failure retains its honest prefix until Close. Orthographic cameras are unsupported and never converted.

## Event and enable route

`Dispatch(InputEvent input, CancellationToken cancellation = default)` handles actual mouse motion only when ButtonMask contains RIGHT, using yaw-=Relative.X*sensitivity and pitch+=Relative.Y*sensitivity followed by normalization/clamp. Pressed WheelUp decreases distance by one supplied step; pressed WheelDown increases it. Wheel factor is not used. Left/other buttons, releases, motion without RIGHT and unchanged clamped states return false without pose attempts or diagnostic change.

Actual `_UnhandledInput` calls Dispatch and marks only successful relevant events handled. Focused GUI Controls consuming mouse events precede it. `SetEnabled(bool enabled)` toggles routing without resetting pose; repeated setting returns false. Window-focus-loss notification disables; focus gain does not auto-enable. UI callers explicitly disable/re-enable around GUI capture. No OS input capture is requested.

`Yaw`, `Pitch`, `Distance` expose stable last committed binary64 scalars. `Camera` exposes the actual borrowed reference until Close. `PoseAttemptCount` includes admitted configuration/input mutation attempts; `Revision` counts committed poses, including configuration. `AcceptedCount` counts successful changed Dispatch events; `HandledCount` counts successful engine event consumption. Enable/ignored/GUI events do not change pose counters. These are different denominators, not coverage scores.

## Guards, prefixes and terminal lifetime

Before each effect and after actual position/LookAt calls, verify owner, caught reentry, exact live captured host/camera parents, active camera identity, same viewport, perspective projection and cancellation. A camera reparented within the same viewport is still rejected because its original parent identity changed. Freed/queued/inactive or externally orthographic camera/parent is unavailable. Pure direct argument/live preflight rejection propagates without inventing a new accepted diagnostic; actual event failure records bounded LastFailure and disables routing. Direct mutation-sequence failures record a prefix and rethrow.

`LastDiagnostic` records kind, requested angles/distance/position, PositionMayHaveChanged/PositionReturned, LookAtMayHaveRun/LookAtReturned, StateCommitted, failure type and engine-event origin. It retains no camera/exception object, message or stack. A setter started flag means effects are possible even if that call does not return.

Caught nested mutation during actual local-transform notification poisons the outer host. It can leave actual position changed before LookAt, or leave both actual position and basis changed before stable scalar/counter publication is rejected. No camera-pose rollback is promised; stable scalar state can differ from actual borrowed pose after failure. Faulted hosts disable input and require terminal Close/new configuration to resume.

Close is owner-thread guarded, terminal and idempotent. It drops borrowed camera/parent/policy references and disables processing without resetting/freeing the camera. Actual `_ExitTree` closes, poisoning a busy outer operation. Caller owns host Free and camera/scene cleanup; scalar/primitive diagnostics remain inspectable. SetEnabled requires current healthy live bindings; Close remains the cleanup path after external invalidation.

## Reproduction

The offline runner (external historical reference) stages only this host and its verification node (external historical reference) using cached prototype host. It has no Core/sibling dependency. The independent Python model (external historical reference) generates angles/clamps, binary32 position and ideal LookAt basis before native execution, then derives perspective rays from that independent pose with actual viewport/pixel metadata. Explicit tolerances are2e-5 position/origin and3e-5 basis/direction; scalar angles/distance compare within1e-12.

Actual Viewport.PushInput, Control GUI consumption, native focus notification, ProjectRayOrigin/Normal, two real transform-notification prefix phases and borrower-lifetime checks are recorded in the [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-06_R70_CAMERA_ORBIT_HOST.md). Focus is injected through native prototype host notification, not captured OS/user input. GPU cyan-target readback proves a supplied pivot is viewed in this synthetic scene; it does not establish original rendering/camera behavior.

## Round72 following-camera join

[Joined evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-06_R72_FOLLOWING_CAMERA_AND_DISCONNECT_PREFIX.md) passes109 headless/GPU updates using actual accepted player position, native offset and explicit correction snap. [Follow clock](TERRAIN_CAMERA_PIVOT_FOLLOW_CONTRACT.md) and [pivot adapter](TERRAIN_CAMERA_PIVOT_HOST_CONTRACT.md) preserve orbit/input ownership; new follow-only held-direction deadband squared1e-12 avoids float translation jitter. Original supplied poses/two footstep events remain retained. Final outer disconnect queues one reliable application/ACK prefix; explicit caller policy drops copied prefix on scene close, suffix never delivers. Wrong-ID after109 updates drains all owners; prior Version7 passes109. Modern camera/selection policy and original terrain/root/live limitations remain explicit.
