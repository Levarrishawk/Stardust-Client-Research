# World local motion driver

R18-R, October 2, 2026. [Driver](../../SWG-Godot-Client/poc/Stardust.Core/Session/SceneWorldLocalMotionDriver.cs) implements the frozen [local driver/state interface](../../SWG-Godot-Client/poc/Stardust.Core/Session/ISceneWorldLocalMotionDriver.cs), reusing [FixedStepActorHost](../../SWG-Godot-Client/poc/Stardust.Core/Assets/FixedStepActorHost.cs) and [PortalActorInputTimeline](../../SWG-Godot-Client/poc/Stardust.Core/Assets/PortalActorInputTimeline.Generic.cs). It is modern planar free movement; no floor, collision, portal permission, original prediction/smoothing, interpolation, protocol codec or engine conversion is claimed.

## Construction and borrowed lifetime

```csharp
public SceneWorldLocalMotionDriver(SceneWorldCoordinator world,
    SceneWorldObjectMotionStore motions, ulong objectId,
    long initialTimestampMicroseconds = 0, long stepMicroseconds = 16667,
    int maximumStepsPerUpdate = 8);
public SceneWorldLocalMotionDriverState State { get; }
public void Submit(long timestampMicroseconds, PortalActorInput input);
public SceneWorldLocalMotionDriverState Advance(long timestampMicroseconds, bool active = true);
public void Reset(long timestampMicroseconds);
public void Dispose();
```

Initial time is nonnegative; steps are 1,000..100,000 microseconds, maximum ticks 1..32. Constructor validates configuration and coordinator/store owner affinity before publishing/retaining dependencies. The selected object must be current, finalized, in an active world and have current typed CREO replication. Static/opaque/missing/pending objects reject. Same-world store pairing is proved by initial PublishLocal; mismatched stores reject. It constructs one private generic host and timeline, anchored to initial time with zero input.

Start pose/speed adopts an existing current motion if present, otherwise native create placement and speed zero. Constructor publishes that local starting pose once; source private authoritative-counter freshness is preserved. Reset's immutable initial pose is always the constructor's raw-create-derived native placement, not a later correction. Raw create fields/placement are never edited.

The driver borrows world and motion store, owns only its selected creation incarnation, copied native pose/speed, one last-publication record, bounded timeline and clock/history. It does not retain arbitrary old world snapshots or resources. One local driver per object is a caller contract; duplicate local writers are not arbitrated. All operations/state reads enforce owner thread and reject driver reentrancy. No external callbacks are injected by this driver; guard behavior is source-verified, and foreign-thread constructor/operation rejection is execution-tested.

## Input, movement and time

Input reuses the shared finite component bound of magnitude10 per X/Z component and additionally requires Y zero. Diagonal magnitude can reach sqrt200; no magnitude normalization is invented. EntryAllowed is accepted as part of the existing input value but has no effect on this free-movement driver. Submit uses the shared ordered/nonlate 60-second horizon and 1,024 pending-transition limit. Equal timestamps preserve submission order/latest wins. Invalid velocity/time/order/capacity rejects before enqueuing new history or publishing motion; invalid admission does not latch an otherwise healthy driver.

Input is sampled at each fixed tick's **start**. Position advances by the host-provided velocity-times-fixed-seconds displacement; native altitude stays fixed unless authoritative correction changes it. A nonzero sampled velocity sets proper native Y-yaw with forward +Z aligned to normalized world X/Z velocity. Rows are `(cos,0,sin)`, `(0,1,0)`, `(-sin,0,cos)` where sin=vX/speed, cos=vZ/speed. Zero velocity retains existing facing and publishes speed zero. Initial/corrected proper orientation can contain other rotation and is held until a nonzero local velocity establishes Y-yaw. No Height scaling is included; typed Height and resource attempts remain independent.

Each successful tick publishes through motions.PublishLocal using a fresh current world snapshot, then commits owned pose and remembers that exact issued local record. No per-render pseudo-tick publication is added. Timestamp/suspension/backlog behavior comes from the accepted integer host/timeline: bounded catch-up, whole-tick dropping, fractional carry, inactive updates and first resumed update discard pending/elapsed movement, skipped/suspended history drains to its latest applicable input. Stored/driver speed remains the last tick/correction speed while no tick runs; Active signals suspension. No old movement is replayed after pause/stall.

Equal tick-start histories produce bit-identical final values across render partitions when the same ticks run. Drop limits intentionally depend on frame work budget. There is no historical rollback for late input. State reports last accepted clock timestamp, steps this update, cumulative dropped/suspended microseconds, pose/speed, activity and sticky fault type. Backward Advance and negative Reset reject before owned motion/history mutation.

## Authoritative correction and boundaries

Before a healthy Advance, the driver reads current motion and adopts a **new current authoritative record** whose global revision exceeds its last observed correction and differs from its exact local publication. Pose and speed snap immediately; the input clock, fractional carry and queued input are not reset. A no-tick Advance therefore exposes the correction without writing an older local pose over it; the next local tick advances from the corrected position. This is immediate modern correction, not original smoothing/prediction. Corrections during suspension are adopted but cause no movement. Determinism across partitions requires the same correction observation boundary; the motion source carries no authoritative event timestamp, so different observation times cannot be retrospectively reconciled.

Currentness requires the original selected incarnation and current finalized typed state each advance/tick/reset. Containment, Height and resource restart preserve that incarnation. Destroy/recreate, selection generation reset, failure and close stop it; the driver never silently attaches to another incarnation. A new driver is required for a replacement object, even if its ID/create bytes match.

Operational/publication failure latches FaultType and disables local movement. Completed successful ticks remain committed; the shared scheduler does not retry a failed tick. Later Advance suspends instead of replaying movement until explicit Reset. If selected-incarnation validation fails before clock admission, the reported timestamp remains the prior accepted clock timestamp on that failed attempt. Submit on a sticky fault rejects; runtime fault diagnostics are type names, not a recovery/smoothing policy. State reads are last observed diagnostics and do not scan the world themselves.

Reset validates nonnegative time and the same still-current incarnation, publishes the immutable initial native pose/speed zero, then resets shared clock/input history and clears the driver fault. It never calls motions.Clear: source global revision remains monotonic and unrelated rows/counter history survive. Failed reset cannot revive an old incarnation. Dispose releases private clock/timeline/dependency/publication references, makes State inactive and never closes world or clears any borrowed motion rows. Repeated disposal is harmless; further Submit/Advance/Reset throws ObjectDisposedException.

[Native scenarios](../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/SceneWorldLocalMotionScenarios.cs) compare three partitions against an independently indexed tick trajectory and common-boundary correction trajectories, test fractional/equal-time keys, pause/stall/history, input bounds/capacity/time atomicity, correction during suspension, Height/resource separation, fault/reset, old/recreated/generation/failed/closed incarnations, owner affinity and borrowed disposal. No asset/resource loading is required. [Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-02_R18_LOCAL_MOTION.md) records identity/reproduction and the host join.

## Round48 compatibility increment

The [explicit source adapter](SCENE_WORLD_EXPLICIT_MOVEMENT_ADMISSION_CONTRACT.md) owns a newly constructed driver, validates authored/received source identity and supplies captured modern planar velocity. [144 actual original-authored/synthetic-received joins](SCENE_WORLD_CAPTURED_MOVEMENT_JOIN_CONTRACT.md) pass. One active local writer per selected object remains a caller requirement; original modifiers/physics are not supplied.

## Round49 compatibility increment

[Original-resource sourced preview](SCENE_WORLD_SOURCED_MOVEMENT_RESOURCE_PREVIEW_CONTRACT.md) passes authored/received headless and authored GPU joins through the actual original recipe closure with zero extra movement reads. [Current raw inputs](SCENE_WORLD_CREATURE_MOVEMENT_CURRENT_INPUT_CONTRACT.md) retain explicit constructor provenance and exact received overrides without store seeding; [explicit stop and sourced input](SCENE_WORLD_SOURCED_INPUT_HOST_CONTRACT.md) own the modern direction/clock lifecycle. Original full arbitration, modifier/vehicle semantics and units remain separate.
