# Precise portal actor with bounded steps

## Explicit modern option

[PortalFloorStepBoxWalker](../../SWG-Godot-Client/poc/Stardust.Core/Assets/PortalFloorStepBoxWalker.cs) combines one bounded step and at most one paired-cell transfer per world-space request. `PortalDoorActorSimulation(usePreciseMovement: true, stepPolicy: new FloorStepPolicy())` selects it through the precise door adapter. Defaults retain their previous behavior. Step limits are modern policy; original step height, gravity and collision ordering remain Unknown.

Owners must be upright rigid frames; world-axis actor bounds are preserved under yaw/translation. Full caller-provided world collider snapshots are copied before permission callbacks and checked through the original prefix, vertical lift/drop and continuation. This includes a future target collider whose bounds extend into an earlier source portion. Current directional permission decisions are cached once per link for the whole request. Shared leaf capture remains before every external callback.

## Ownership and limits

A step can precede a portal transfer or follow it. Vertical collision returns the supported prefix locator. If the prefix already transferred, its target owner and transfer count are retained; if not, the source owner remains. No floating midpoint is adopted. Zero-time overlap after landing holds the supported prefix. Clear continuation maps local progress back to the complete horizontal request and records Steps = 1.

The combined traversal budget includes each prefix floor, strip triangles and supported landing. After one transfer, continuation uses maximumTransfers = 0, stopping at a further portal before an external permission callback. Step rejection remains explicit, including strip/height limits; generic floor walking is unchanged. Sliding and continuous moving-leaf collision remain separate.

## Pinned original gate

The small-house threshold is `appearance/ply_tato_house_sm_s01_fp1.pob`, cell 0/portal 0 ↔ cell 1/portal 0. POB SHA-256 `48BD954D46F04AC134DAF53A92A91E5C241BB174EE48954FA30C8159866B0758`; primary `appearance/door_tato_filler_s01.apt` SHA-256 `65661A0EF2BDA1865A988E81D79F75322548DFE4C895239909A840ACE96ABFE4`. Configuration and table pins are unchanged from the [precise actor contract](PRECISE_DOOR_ACTOR_CONTRACT.md).

[Independent persistent verification](../../SWG-Godot-Client/tools/python/verify_persistent_door_actor.py) with --precise --steps uses independently authored original triangle/box/controller references and [reciprocal riser checks](../../SWG-Godot-Client/tools/python/step_door_actor_reference.py). Both starting directions/two owner poses pass **1,896 ticks / 88 transfers / 88 step events / 58 blocked ticks**. Each direction yields 948/44/44/29. Exact owner, status, kind, transfer and step counts match; numerical position/parameter budgets are unchanged. An exactly-on-seam start required the reference to model an immediate zero-progress transfer, rather than walk beyond the source boundary.

Native synthetic gates additionally verify step-before/after-transfer, world owner poses, blocked rise/drop ownership, 200 repeat holds without drift, callback snapshot mutation, traversal limits and zero-transfer continuation without callbacks. Existing YT-2400 precise trace remains 948/44/27 with steps disabled.

## Screen scope and next gate

On the same 782 authored / 533 instantiated candidates, the explicit step-enabled mode completes **504 exact short round trips**, versus 496 precise without steps and historical 131 level-only. Remaining 29 end Accepted without exact return; all are categorized by first forward boundary. The eight newly passing small-house selections do not imply arbitrary stair or world collision support. Broader connected strips retain conservative rejection.

Next: explicit Godot step option and pinned original headless/GPU scheduling, ownership and placement checks. Current host precise option supports slopes but does not yet select this step policy.

## Godot host acceptance

[Explicit step host gate](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_STEP_DOOR_GODOT.md) passes pinned small-house forward headless/reverse GPU controls over two poses, scheduling/input and reflected placement. Headless manual trace counts 20 repeated transfers and 23 step events. Private GPU capture inspected. Launcher requires -DoorWalk -PreciseDoorWalk -StepDoorWalk; defaults stay unchanged.
