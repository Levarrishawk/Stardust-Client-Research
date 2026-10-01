# Persistent actor and one shared doorway leaf

## Owned modern boundary

[PortalDoorActorSimulation](../poc/Stardust.Core/Assets/PortalDoorActorSimulation.cs) persists a single actor's floor locator, world position, frame, cumulative transfer count and current door state. Each tick takes finite elapsed seconds (0–1), world movement delta and nullable current entry permission. It automatically derives proximity contact from the actual actor position, advances one normal leaf, queries the [integrated walker](INTERIOR_DOOR_MOVEMENT_CONTRACT.md), and adopts its returned locator for the next tick.

One physical leaf and one controller are explicitly shared by both incident references. This is a modern coordination choice, **not recovered legacy paired-controller synchronization**. The simulation requires uniquely paired target reference identity, matching nonempty style, eligible flags, explicit authored transform and exactly matching reverse pose. It supplies the same building owner pose to both cells. Arbitrary mismatched door placement, independent leaf controllers, force fields and multiple actors are outside this slice.

The original style/curve, contact radius, retention pruning and cap remain as documented in the [single-leaf contract](SINGLE_LEAF_DOOR_CONTRACT.md). The additional declared incident cell lets `SingleLeafDoorRuntime` accept contacts and collider bindings from either owner. The single-source constructor retains its old behavior. Runtime geometry, timing and contacts stay engine-neutral.

## Tick, ownership and lifetime

1. Validate the finite bounded time/delta and actor center before modifying state.
2. Submit the stable actor handle at its current owner and world center to the leaf; denied/unknown entry cannot add a new opening pulse.
3. Advance/prune/refresh retained contacts using current feet position and permission.
4. Capture leaf geometry and evaluate physical/permission contact and at most one portal transfer.
5. Adopt local floor locator and world position. Increment frame and cumulative transfers. The next tick starts from this result, including blocked source contacts.

The caller controls elapsed time and input delta. No wall clock, engine physics body, networking or hidden actor teleport is used. Invalid time/delta is rejected without changing actor/controller state. Unsupported nonlevel motion or a projected floor result with unsupported height change retains the existing locator. Reset returns to the validated initial locator, clears controller/contact/transfer history and resets frame zero. Dispose releases retained contact handles and rejects further ticks.

The initial locator is normalized by the existing floor query within 1e-4 of supplied placement; reset uses that owned canonical locator. Small float differences after world/local transforms are not a new floor ownership interpretation.

## Retreat from a touching face

Persistent movement exposed a limitation of the old inspection query: a returned contact can be classified as initial overlap on the next frame because a box face is inclusive. The simulation opts into an **owned separating-contact policy**. An actor within 1e-5 of an expanded collider face may move outward from that face. Moving inward still blocks; genuine interior overlap remains explicit without depenetration. The tolerance recognizes rounded contact placement and does not expand passage clearance.

`PortalBarrierCollider.Sweep` and `PortalBarrierFloorWalker` retain their former default behavior unless this option is enabled. This is not a claim about original solver tolerances. An animated door can still close into an occupied actor envelope: continuous moving-leaf collision/depenetration is deferred. Restoring permission lets a retained near actor reopen the door; forced interior-overlap response remains unresolved.

## Verified original trace

The original cantina/private-room inputs and six hashes are unchanged from the [instance checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_INTERIOR_DOOR_MOVEMENT.md). [Independent trace verification](../tools/python/verify_persistent_door_actor.py) decodes the effective inputs and computes proximity/pulse/curve, swept contacts, source/target adoption and reset independently.

Two building owner poses × 474 ticks match **948 ticks, 44 transfers and 34 blocked ticks**. The trace includes ten round trips per pose, denied reset/approach, retreat, unknown permission, recovery/return and final reset. These counts describe deterministic fixtures, not full locomotion coverage. Synthetic tests additionally complete 124 repeated transfers, preserve one retained handle across owner changes, reject mismatched reverse pose/invalid actor inputs, distinguish separating touch from genuine overlap, and verify reset/dispose.

The Godot inspector adds `-DoorWalk` to the prior explicit POB/table/leaf command. It has forward/backward/wait buttons, permission, transformed-owner and reset controls. Unlike fixed crossing replay, every step consumes actual persisted actor ownership and position. Headless/GPU controls pass 20 repeated transfers over two poses with retreat, reset, retained contact and reflected actor/leaf/floor placement. GPU capture was inspected and stays private. [Continuous keyboard/render scheduling](FIXED_STEP_ACTOR_CONTRACT.md) now passes its bounded gate.

## Remaining fidelity and next work

Original global collision ordering and narrow-phase leaf shape remain Unknown. Physical clearance continues to use a conservative primary appearance box. Slopes, arbitrary cell owner differences, sliding, animated overlap response, effects/audio and general world obstacle collision are deferred.

The [fixed-step follow-up](FIXED_STEP_ACTOR_CONTRACT.md) closes bounded scheduling and keyboard adaptation. Next: timestamped input transitions between render frames.

## Precise movement opt-in

The [precise door actor contract](PRECISE_DOOR_ACTOR_CONTRACT.md) adds usePreciseMovement to the engine-neutral simulation. Connected sloped source/target paths are supported under this option; the historical level adapter remains the default. Pinned YT-2400 traces pass 1,896 ticks and 88 transfers across both starting directions and two owner poses. Moving-leaf overlap and original global ordering remain unresolved.
