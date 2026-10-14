# Supplied terrain local motion

SceneWorldTerrainMotionDriver (external historical reference) implements the frozen local motion interface (external historical reference) over an immutable supplied collection of [surface queries](TERRAIN_PATCH_SURFACE_QUERY_CONTRACT.md). It uses native X/Z for planar travel and Y for height. This is modern grounding policy, with no original floor/collision, prediction, extrapolation, sliding, jumping, gravity, normal alignment or procedural generation claim.

## Construction and lifetime

```csharp
new SceneWorldTerrainMotionDriver(SceneWorldCoordinator world,
    SceneWorldObjectMotionStore motions, ulong objectId,
    IReadOnlyList<TerrainPatchSurfaceQuery> surfaces, float footOffset = 0,
    long initialTimestampMicroseconds = 0, long stepMicroseconds = 16667,
    int maximumStepsPerUpdate = 8, SceneWorldTerrainMotionLimits? limits = null);
```

State, Submit, Advance, Reset and Dispose follow the frozen interface. The selected object must be a current finalized typed CREO incarnation in AwaitingServerReady or Ready. World/store affinity and same-world pairing are checked through the frozen snapshot/store APIs. One driver per selected object is a caller contract; writers are not arbitrated. It never attaches to a recreated object with the same ID.

The constructor reads the collection count once, checks it before allocating, copies each nonnull query reference in source order, and copies the limits record. Caller collection access occurs during construction before selecting the world snapshot; callers must keep the collection stable during this copy. Later array/list changes cannot alter the driver. Each frozen query owns immutable copied geometry and has no callbacks or disposable owner. The driver borrows query objects, world and motion store; it owns only the bounded reference vector, selected incarnation, native pose, last publication, private fixed-step host and timeline. Dispose releases query/world/store/clock/history/publication references without clearing motion rows or closing borrowed objects. State remains available as inactive last diagnostics; repeated disposal is harmless.

Initial pose/speed adopts current motion if available, otherwise immutable create placement and zero speed. It grounds that pose and publishes once. Initial/reset grounding failure rejects before publishing. Reset always grounds the immutable create placement, rather than using a later correction; the query result is prepared before publication and passed to the private host reset action without another query. Raw create bytes remain unchanged.

## Bounds and deterministic coverage

SceneWorldTerrainMotionLimits defaults are MaximumPatches=64, MaximumTriangleTestsPerCandidate=512, MaximumPositionComponent=1,000,000f, MaximumFootOffset=1000f. Hard admissible ranges are patches 1..256, triangle reservations 1..2048, finite positive position cap at most 1e9f, and finite nonnegative foot-offset cap at most 1000f. FootOffset itself must be finite and its absolute value must not exceed the configured offset cap; negative offsets are explicitly supported. Surface count must be nonzero and within the configured cap.

For every constructor, reset or local-tick candidate:

1. Require finite incoming X/Y/Z within the configured position cap. Incoming Y is validated before replacement, so an invalid altitude is not masked.
2. Scan patches in copied source order. Before calling each patch, reserve its entire TriangleCount against the candidate budget, even if its bounding box will reject immediately. There is no refund for early triangle hits. A later patch requiring more than the remaining reservation faults before its query.
3. Use the first patch returning a hit. Its frozen query selects its own first eligible source triangle on closed edges. Overlaps and shared patch boundaries therefore choose the earliest supplied patch; no averaging or seam-height repair occurs. A query exception faults immediately rather than trying another patch.
4. Compute `double height = hit.Height + (double)footOffset`, require finite bounded height, cast once to float, replace only Translation.Y and validate the resulting position again. The hard position cap also prevents finite-to-float overflow. Motion-store publication validates finite speed and the full proper rigid transform within its frozen 1e-4 tolerance before row mutation.

Outside every supplied patch is InvalidDataException. No fallback height or extrapolation is invented. Budget and coverage are per candidate, rather than mutable global counters. With at most 32 fixed ticks, the hard reservation bound is 65,536 triangle tests per Advance, in addition to frozen world/store snapshot scans and bounded input history. This is a logical reservation/work bound, not a heap, timing or CPU measurement. Query normals must satisfy the frozen query's eligibility rules but do not rotate the actor.

## Clock, input and publication

The [accepted planar driver contract](SCENE_WORLD_LOCAL_MOTION_CONTRACT.md) supplies the timing and movement rules. This driver directly uses the frozen FixedStepActorHost and generic PortalActorInputTimeline: step 1,000..100,000 microseconds, maximum steps 1..32, tick-start input selection, 1,024 queued transitions and 60-second future horizon. Input components are finite and bounded by 10, with Y exactly zero. Equal timestamp submissions preserve order/latest wins; EntryAllowed has no grounding/portal semantics. Invalid or late admission rejects before modifying history and does not latch a healthy driver.

Each tick computes the same float velocity-times-seconds displacement, X/Z accumulation, horizontal speed and native Y-yaw as the planar driver. Nonzero velocity sets rows `(cos,0,sin)`, `(0,1,0)`, `(-sin,0,cos)`; zero velocity preserves facing and publishes speed zero. Speed/distance describe planar travel, not slope arc length. No Height replication scaling is applied. Grounding runs even at zero velocity. No render-frame pseudo-tick is published.

A fresh current world snapshot is required each advance/tick/reset. Grounding and PublishLocal run without injected callbacks or mutable external query code between them, on the same owner thread. Pose and last-publication identity advance only after successful store publication. Currentness includes the selected creation incarnation and active finalized typed world state. Containment/resource changes preserving it remain eligible; destroy/recreate, selection generation, failure and close cannot resurrect it.

Pause/loss is expressed by Advance(active:false). Frozen suspension/backlog policy retains fractional timing when active, bounds catch-up, drops whole excess ticks, and consumes skipped input to its latest applicable value. Inactive and first resumed updates discard elapsed/pending travel; no suspended movement is replayed. State reports the frozen timestamp, completed steps, dropped/suspended time, pose, speed, activity and sticky fault type.

## Corrections, failures and reset

Before a healthy Advance, a new current authoritative record whose revision exceeds the last observed correction and differs from the exact last local record snaps owned pose/speed after position eligibility validation. Its full rigid transform was already validated by the motion store. The clock, fractional carry and input history stay intact. No-tick or paused advances expose its raw authoritative altitude without local overwrite; the next actual local tick queries terrain and replaces Y. A bounded correction outside terrain coverage may therefore be displayed until that next tick faults. An over-position-cap correction faults before owned adoption. The driver does not undo an externally published authoritative record.

Partition determinism requires the same executed ticks, tick-start history and correction observation boundary. Complete ticks up to the common boundary before publishing a correction there. A motion record has no authoritative event timestamp, so observation at different partially completed boundaries cannot be retrospectively reconciled.

Operational query/publication/currentness failures latch FaultType and disable movement. A late failing tick does not publish that candidate or roll back successful earlier ticks in the same Advance. The shared scheduler retains completed count, drops the uncompleted interval and does not retry it. Later Advance suspends while faulted; Submit rejects until explicit Reset. A currentness failure before clock admission preserves the previous timestamp. Invalid backward Advance/negative Reset remain nonsticky admission errors.

Reset requires the same current incarnation, grounds and publishes initial placement with speed zero, then clears private clock/input/fault state. Failed reset preserves prior valid owned pose/history and cannot recover a destroyed incarnation. Source revisions/counters and unrelated rows remain borrowed and monotonic. All state reads/operations enforce owner thread and reject reentry. Reentry guard and absence of external operation callbacks are source-verified; no forged callback execution is claimed.

## Verification and next boundary

Separate-assembly native scenarios (external historical reference) pass 2,954 checks over 22 exported sequences, plus configuration/alias/lifecycle/owner/capacity/reset negatives. The independent peer (external historical reference) derives analytic flat/sloped/seamed heights, binary32 planar accumulation/yaw/speed and integer-clock schedules without native expected results. It passes 21,735 comparisons of position, all orientation rows, speed, timestamp, completed/dropped/suspended steps, fault/activity, motion revision and authoritative/local status. Four groups of three render partitions have bit-identical final poses and revisions when tick and correction boundaries match. Numeric oracle comparisons use 2e-6 relative/absolute tolerance; this does not replace the frozen surface query's broader barycentric tests.

Late outside, insufficient second-patch budget, invalid selected normal and position-bound cases retain two accepted ticks, their exact current publication and sticky-fault/no-retry behavior. Exact/one-less constructor caps, input bounds/order/horizon/capacity, collection/inspection-copy isolation, zero-velocity orientation retention, maximum diagonal yaw, pause/stall/corrections, borrowed store closure/capacity, owner thread, recreate/generation/failure/close and disposal are tested. Core, own runner and actual separate SmokeTests builds have zero warnings/errors. No original assets/executable, resource loads, GPU, world integration or live contact are used.

[Checkpoint and reproduction](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R39_TERRAIN_MOTION.md). Next probe: lead-owned original host integration with explicitly supplied native-space geometry and existing local-driver interface, preserving modern grounding and original collision/procedural parity boundaries.

## Round62 retained-set overload

A new driver constructor borrows the actual [retained-set surface query](TERRAIN_MODERN_PATCH_SET_SURFACE_QUERY_CONTRACT.md), reserves its complete patch/triangle upper bound before local publication, and routes initial/tick/reset grounding through aggregate Query. The existing supplied-list constructor preserves its prior ordered policy. Common finite height/foot-offset checks remain shared. Native admission/grounding, the prior deterministic motion suite and original-resource preview gates pass. Disposal releases references and preserves borrowed source/motion. Original floor/collision parity remains Unknown.

## Round65 point-target consumer

[Point controller](SCENE_WORLD_TERRAIN_POINT_TARGET_CONTRACT.md) owns this driver and computes supplied modern planar velocity toward an admitted actual terrain hit. It reads a newer authoritative row before steering, matching adoption during Advance before Tick. One consecutive zero-speed update after arrival stops borrowed motion before owned disposal. Sole-writer, bounded fixed-clock, source-provenance and late-publication-prefix limits remain explicit.

## Round66 queued correction ordering

[Joined acceptance](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-06_R66_POINT_INPUT_PAUSE_AND_QUEUED_CORRECTION.md) uses active zero-velocity pause ticks while retaining this actual driver. A queued full correction is processed through the existing pump before a subsequent steering tick; the host does not advance the controller clock during that pump frame. No extra zero-time driver update, direct full-router bypass, timeline suspension or original interpolation claim is introduced.
