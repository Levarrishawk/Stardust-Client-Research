# Timestamped actor input timeline

## Replacement input policy

PortalActorInputTimeline (external historical reference) exclusively drives a borrowed, initially unanchored [fixed-step host](FIXED_STEP_ACTOR_CONTRACT.md). Callers must serialize submission/advancement/reset and must not also advance the underlying host. Each transition contains an integer timestamp and a fully validated velocity/permission snapshot.

A change applies at the first tick whose **start** is at or after its timestamp. A change during a tick does not affect that earlier tick. Equal-time submissions preserve order, so the latest snapshot wins. Submit rejects timestamps before the last render advancement, out-of-order history, more than 60 seconds in the future and more than 1,024 pending transitions. These checks occur before queue mutation. There is no late-history rollback.

Whole ticks dropped by the host do not replay movement. History through the next tick start is consumed, preserving the latest input for fresh time. Suspension similarly consumes input history without advancing actor state, and resume discards the inactive interval. Reset clears queue and actor/controller state and supplies a new epoch/input. Expected host faults retain their existing explicit reset requirement. Timeline lifetime does not own actor disposal.

Observation: direct tick-indexed inputs and three render schedules produce equal final actor/controller state even when all six transitions fall between tick boundaries. Confidence: Confirmed replacement behavior; original input timestamp/quantization semantics remain Unknown. Compatibility consequence: preserve wire/asset contracts while using this explicit modern scheduling policy internally.

## Prototype host adapter

`-DoorWalk` receives non-echo physical W/S key transitions through `_Input`, records `Time.GetTicksUsec()` and submits snapshots. Permission changes also submit timestamped snapshots. Render polling reconciles held keys after mode/focus changes at the current timestamp; it cannot backdate newly observed keys into pending time. Mode boundaries clear inactive velocity and advance suspension explicitly. Reset/owner replacement start a fresh timeline; teardown removes the key callback before disposing actor resources.

The earlier snapshot-only API remains available for controlled tick input fixtures. Rendering updates use the timeline. Human key press testing remains separate from injected engine-event verification.

## Verification and limits

Synthetic tests (external historical reference) compare three render partitions with an independently indexed 220-tick native sequence, verify no mid-tick backdating, latest equal-time input, late/order/horizon/capacity rejection, stall-history adoption, suspension and reset. Invalid submissions leave clock/queue unchanged.

Independent original replay (external historical reference) checks 1,488 clock updates, 1,320 independently computed ticks and 18 transfers: three partitions × two original owner poses, six nonaligned changes. It reuses independent byte/trajectory decoding, not native controller or timeline code. Source pins are unchanged from the [original instance](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_INTERIOR_DOOR_MOVEMENT.md).

Prototype host checks confirm injected key events reach the timeline queue, cancellation/release produce zero latest velocity, suspended history drains without motion, and a nonaligned original transition crosses into the target cell after 60 ticks. GPU capture passes and was inspected. General planar locomotion, slopes, world obstacles and continuous moving-leaf collision remain deferred.
