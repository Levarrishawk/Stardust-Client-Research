# Offline client root motion construction

Round67 constructs a full outbound root frame from actual current typed LOCAL motion. It borrows the accepted [world motion store](SCENE_WORLD_OBJECT_MOTION_CONTRACT.md) and uses the [directional full codec](OBJECT_CONTROLLER_TRANSFORM_CODEC_CONTRACT.md). There is no send, socket, queue, timer, cadence or server contact.

```csharp
using var builder = new SceneWorldClientRootMotionBuilder(world, motions,
    new SceneWorldClientRootMotionPolicy(maximumNativePositionComponent, maximumNativeSpeed));
SceneWorldClientRootMotionPacket packet = builder.Build(currentSnapshot, currentLocalMotion,
    flags, movementCounter, timestampBits, cancellationToken);
```

Policy is mandatory: position component cap must be finite and positive; speed cap finite and nonnegative. Native position is used directly as XYZ; no Godot Z reflection, unit conversion, height scaling, velocity inference or speed clamp occurs. Flags, counter and timestamp are explicit U32 inputs with no automatic increment or ordering requirement. Timestamp retains every bit pattern, including words that would be float NaN/infinity. Original timestamp units, sender cadence, quaternion sign and gameplay admission remain Unknown.

## Currentness and ownership

Construction proves world owner affinity and open exact-world store binding with IsBoundTo. Build/Dispose require the construction thread and defensive nonreentrancy. There are no callbacks or asynchronous operations. Disposed/precanceled calls throw before reading identities. Snapshot and motion arguments are mandatory.

Build requires active AwaitingServerReady/Ready world, current finalized ContainerId0 object, current typed CREO replication, canonical object and placement references, exact opaque envelope values/current typed revision, matching incarnation, and exact currently store-issued motion reference. Local means Authoritative false and MovementCounter null; a publicly cloned/forged motion, another store's equal row, foreign-world row or authoritative correction rejects. Snapshot outer wrappers and typed wrappers are recreated by world APIs; canonical members and opaque typed-state currentness, rather than unstable outer-wrapper reference identity, establish currency. A shallow outer snapshot copy with unchanged canonical members can therefore be valid. Altered placement/replication/envelopes reject.

Before returning, the builder rechecks world generation/change-batch stamp and all current identities. DrainChanges is non-mutating inspection despite its name; actual acknowledgement clears entries. Native cases verify the prior inspected batch remains current and retains its entry count. The builder never acknowledges that batch, edits replication/placement, publishes a row, clears borrowed rows or retains output packets. Dispose releases only borrowed references; world/store and prior caller packets survive.

Packet.Raw is the immutable ClientToServer Root0x71 message. Packet.Application is a fresh exact62-byte array exclusively owned by the caller; clearing or modifying it cannot change Raw, SourceMotion, another build's array or the store. SourceSnapshot/SourceMotion retain actual historical identities. Later publication, typed-state change, containment, recreation or reset can make them obsolete; construction is not a promise of later send eligibility. Call store.IsCurrent on SourceMotion and obtain a fresh snapshot when currentness is needed. The builder sends nothing.

## Matrix conversion and boundary

Rows are native column-vector affine rows. Finite proper rigidity is checked in double: squared row lengths and determinant differ from1 by at most1e-4; pairwise row dot magnitudes are at most1e-4, matching the existing store admission tolerance. Position and speed must additionally satisfy caller bounds and speed>=0. Negative zero is admitted and its position/speed bits remain exact.

Conversion uses standard trace/largest-diagonal matrix-to-XYZW quaternion branches in double. Trace>0 selects positive W; otherwise the largest diagonal selects positive X/Y/Z, with X then Y then Z tie precedence. Double normalization precedes binary32 component rounding. Near180 rotations avoid dividing by a vanishing W. All three largest-component branches are fixture-covered. This deterministic sign is modern policy; sign-equivalent quaternions represent the same basis. Slight rigid drift admitted by the store is normalized into a rotation, so no exact row-bit reconstruction is promised.

Only the existing codec performs wire packing: U16 count5/U32 hash80CE5E46/flags/type71/U64 object/U32 timestamp/U32 caller counter/quaternion XYZW/position XZY/speed. No parent payload is produced. Codec decode/encode and placement reconstruction corroborate the emitted boundary; they do not prove original sender or server gameplay behavior.

## Validation and limits

[Own replay](../poc/round67-tests/Networking/replay.py) independently generates binary32 rows/positions and expected frames before native execution. A symmetric4x4 Jacobi eigenvector quaternion oracle differs from the native trace/max-diagonal algorithm; basis is obtained via vector cross products. Eight stable authored axis/asymmetric/driver frames are whole-byte pinned. Near180 uses exact expected header/position/speed bytes and independent basis equivalence at2e-7, rather than imposing a quaternion-bit/sign claim on two conversions of rounded matrices. Native corroboration tolerance is2e-6 and was not relaxed.

```powershell
python poc/round67-tests/Networking/replay.py --output-dir local-private/round67-review-replay
```

A fresh guarded ignored output directory, installed .NET8 and existing offline package cache are required. Nine cases include identity, yaw90, X/Y/Z180, asymmetric cyclic rotation, near180, actual SceneWorldLocalMotionDriver one-step +X movement and actual SceneWorldTerrainMotionDriver one-step +Z grounded movement over an authored constant-height patch. Exact native XYZ, arbitrary flags/counters/timestamp bits and signed-zero speed/position are checked; outputs are disconnected from any transport.

Final replay passes93 native checks, nine basis/layout cases and eight whole-frame pins; build has zero warnings/errors. Negatives cover copied/forged/foreign/same-world-other-store/authoritative/untyped rows, altered snapshots, typed refresh, containment, missing old incarnation, pending/recreated/reset objects, closed world/store, caller bounds, invalid policy, foreign thread/disposal and precancellation. Invalid scales/reflections/collapse/nonfinite/negative speed are dynamically rejected by the existing store before rows can become issued. Forged invalid rows are rejected by builder identity admission; the builder's duplicate numerical defense is source-inspected because the frozen immutable store cannot issue such a row without violating its own validation.

Mid-conversion mutation/reentry and revision overflow are not injected: conversion exposes no callbacks, and inherited overflow defenses retain prior evidence. Only catalog/bank profile JSON and synthetic lifecycle/terrain data are read; no original asset/resource jobs or live sends occur. Original interpolation, timestamp arithmetic and quaternion sign/cadence remain unproved. Next action: consume offline caller-owned packets in the lead host and validate actual source-row/trajectory/wire joins without enabling networking.

## Round68 owned historical root FIFO

[Queue contract](SCENE_WORLD_CLIENT_ROOT_MOTION_QUEUE_CONTRACT.md) owns one builder and adopts its fresh arrays. It bounds pending count/bytes, transfers FIFO arrays and clears stale-incarnation/old-generation prefixes on eligible takes. Later same-incarnation state changes leave historical frames transferable; this is not send authorization. Caller owns transferred bytes, queue drains retained only. Three exact frames/70 checks pass; retained-byte clearing is explicitly source-inspected rather than exposed through a test backdoor.

## Round69 supplied run intent and offline transport join

[Joined evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-06_R69_RUN_SPEED_AND_OFFLINE_TRANSPORT.md) passes110 headless/GPU updates with four supplied speed changes, three actual Shift events, nine reliable SOE datagrams and nine independent ACK releases. [Speed](SCENE_WORLD_TERRAIN_MOVEMENT_SPEED_CONTRACT.md) changes only owned intent until the next tick; [Shift host](TERRAIN_RUN_INPUT_HOST_CONTRACT.md) tracks independent locations, GUI/focus and callback prefixes; [bridge](SCENE_WORLD_ROOT_MOTION_TRANSPORT_BRIDGE_CONTRACT.md) transfers FIFO bytes only when the whole transport has no pending messages. A27-update clock rejection preserves earlier motion and drains two pending frames. Previous Version4 and both changed core branches pass. This is offline ownership/framing evidence, with no socket/send or original movement/terrain/sender parity claim.

## Round70 camera-relative movement and SOE ingress

[Joined evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-06_R70_CAMERA_RELATIVE_AND_SOE_INGRESS.md) passes109 headless/GPU updates with three actual orbit inputs, eight camera-relative manual intents, one held-key remap and two inbound SOE applications/two unsent controls. [Projection](TERRAIN_CAMERA_RELATIVE_MOVEMENT_CONTRACT.md) stays engine-neutral; [orbit](TERRAIN_CAMERA_ORBIT_HOST_CONTRACT.md) borrows camera; [ingress](SCENE_WORLD_SOE_APPLICATION_INGRESS_CONTRACT.md) copies to actual pump and transfers controls to caller. A valid SOE/truncated application preserves20 updates before actual pump closes world and all owners drain. Previous Version5 passes110 updates. No original camera/control/terrain parity or live send claim.

## Round71 accepted displacement and disconnect

[Joined evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-06_R71_ACCEPTED_LOCOMOTION_AND_DISCONNECT.md) passes109 headless/GPU updates with actual grounded displacement driving retained original idle/walk/run frames and two footstep occurrences. [Observer](SCENE_WORLD_TERRAIN_LOCOMOTION_OBSERVER_CONTRACT.md) rebases authoritative correction without teleport distance; [presentation](SCENE_WORLD_ACCEPTED_LOCOMOTION_VISUAL_CONTRACT.md) adds no elapsed clock. Matching offline peer disconnect closes transport before caller scene teardown; wrong-ID failure preserves109 updates and all owners drain. Prior Version6 passes109. Modern selection/scheduling remains explicit, original graph/control/terrain/live parity unclaimed.

## Round72 following-camera join

[Joined evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-06_R72_FOLLOWING_CAMERA_AND_DISCONNECT_PREFIX.md) passes109 headless/GPU updates using actual accepted player position, native offset and explicit correction snap. [Follow clock](TERRAIN_CAMERA_PIVOT_FOLLOW_CONTRACT.md) and [pivot adapter](TERRAIN_CAMERA_PIVOT_HOST_CONTRACT.md) preserve orbit/input ownership; new follow-only held-direction deadband squared1e-12 avoids float translation jitter. Original supplied poses/two footstep events remain retained. Final outer disconnect queues one reliable application/ACK prefix; explicit caller policy drops copied prefix on scene close, suffix never delivers. Wrong-ID after109 updates drains all owners; prior Version7 passes109. Modern camera/selection policy and original terrain/root/live limitations remain explicit.

## Round73 longer offline session

[Joined checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-06_R73_LONG_OFFLINE_SESSION.md) passes1000 headless/GPU updates with persistent original supplied poses/17 footstep occurrences, modern following camera, six later viewport target changes, owned controls/status HUD and90 scheduled offline root frames/ACKs. Explicit budget preserves earlier defaults; wrong-ID disconnect drains all owners, prior Version8 passes109. Scripted10 simulated seconds does not establish real-time interactive play or original camera/movement/cadence/live parity. [Budget](TERRAIN_OFFLINE_SESSION_BUDGET_CONTRACT.md), [HUD](TERRAIN_OFFLINE_PLAYGROUND_HUD_CONTRACT.md) and [cadence](SCENE_WORLD_ROOT_MOTION_CADENCE_OUTBOX_CONTRACT.md) remain separate boundaries.

## Round74 elapsed offline scene

[Joined checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-06_R74_ELAPSED_OFFLINE_HOST.md) passes1000 motion updates with actual separate fixed presentation steps, original supplied poses/17 events/HUD/camera and90 offline protected roots/ACKs. [Planner](TERRAIN_OFFLINE_FRAME_CLOCK_CONTRACT.md) separates reserved slots and explicit time drops from completed motion; [host](SCENE_WORLD_FIXED_STEP_PRESENTATION_HOST_CONTRACT.md) preserves its exact old signature/default proof. [Reusable drain](SCENE_WORLD_OFFLINE_ROOT_MOTION_DRAIN_CONTRACT.md) validates ACK before FIFO take and owns one bridge. Measured GPU593 plans/10,013,260us/no time drop; supplied headless379 plans/exact drop accounting. Rejected first stop ACK preserves one queued frame until caller cleanup; prior Version9 passes1000. Modern offline policy, original actor/camera/cadence/live limits remain explicit.
