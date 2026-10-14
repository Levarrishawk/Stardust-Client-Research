# Planar doorway actor input

## Owned coordinate and speed policy

[PlanarActorInputMapping](../../SWG-Godot-Client/poc/Stardust.Core/Assets/PlanarActorInputMapping.cs) converts horizontal native forward, forward/right axes in [-1,1], speed in [0,10] metres/second and nullable permission into an engine-neutral input snapshot. It normalizes the supplied forward direction, defines native right as up cross forward, and normalizes axis magnitude above one. Analog magnitudes below one remain proportional. Double intermediate arithmetic handles finite large forward components without float overflow; output velocity is float and has zero Y.

Observation: 100 axis/basis combinations preserve bounded magnitude; native right for +Z is +X. Confidence: Confirmed replacement mapping. This is a modern control policy, not recovered original keybindings or camera-relative locomotion. Godot reflects native Z at the render boundary; input is calculated in native coordinates before reflection. The building owner rotates both axes consistently.

## Live adapter and movement limits

The `-DoorWalk` adapter maps W/S toward target/source and A/D to doorway-relative left/right. Opposing keys cancel per axis; diagonal speed stays 1.6 m/s. Physical transitions and permission changes use the [timestamped timeline](ACTOR_INPUT_TIMELINE_CONTRACT.md). Pause/focus/mode changes reconcile all four held keys and preserve suspension. UI labels describe doorway-relative controls.

The existing persistent actor and floor/portal walker own movement. Lateral and diagonal requests can traverse floor triangles and the selected paired portal. Unsupported level changes, missing floors, unpaired portals and floor boundaries remain explicit stops. **Floor navigation remains a point/feet query; actor half extents apply to door/barrier clearance, not arbitrary walls or a full world collision body.** This slice does not claim general collision, sliding, slopes, arbitrary camera input or multiple doors.

## Verification

[Synthetic scenarios](../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/PlanarActorScenarios.cs) check cardinal/analog/diagonal magnitude, invalid direction/axes/speed, very large finite basis values, native right convention and two-owner planar round trips. A lateral attempt beyond the synthetic floor stops with source ownership preserved.

[Independent original verifier](../../SWG-Godot-Client/tools/python/verify_planar_door_actor.py) computes mapping and trajectory for six phases × two owner poses: 268 ticks / four transfers. The route waits for opening, moves laterally, crosses diagonally, returns through the door and settles at its original position. Six source pins remain those of the [original instance checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_INTERIOR_DOOR_MOVEMENT.md). Full native smoke and Godot build pass.

Headless/GPU checks inject A/D and W/S, verify opposing lateral cancellation, diagonal magnitude and release, then run full keyboard-diagonal square round trips over both owner poses. Both round trips retain actual owner state and return to the start; reflected actor placement is checked. Private GPU capture inspected. Injected event tests do not claim human interaction testing.

## Next work

Assess authored door variants and choose a second original normal-door style/instance before generalizing the controller. Keep force-field, secondary-leaf, flip and geometry acceptance distinct from installed occurrence and runtime support. Moving-leaf overlap and original actor ordering remain Unknown.
