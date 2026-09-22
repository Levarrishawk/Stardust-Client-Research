# Ground Radar Runtime

Date: 2026-09-22  
Target: `C:\Stardust-DEV\Stardust.exe`

## Confirmed behavior

`SwgCuiGroundRadar` is the mediator wrapped around the lower-level
`CuiWidgetGroundRadar`. Its RTTI also exposes separate `BlipPane` and
`WaypointPane` UI classes.

The frame-update path at `0x00F03A40`:

- resizes a subordinate render/UI surface when its enclosing widget changes;
- derives radar heading from a world object's `Object::getTransform_o2w()`;
- obtains the current world's `ClientProceduralTerrainAppearance` when present;
- obtains the player/world object position; and
- updates the radar terrain projection with position, terrain appearance and a
  time/range-derived refresh condition.

This confirms that ground radar is a local world/terrain projection, not a
server-supplied bitmap. It depends on the same procedural terrain and object
coordinate model as the world renderer.

The context/tooltip path at `0x00F03C60` hit-tests a displayed radar object,
builds its localized name/details, and appends integer world location and player
distance. `ClientWaypointObject` is special-cased to use its waypoint location;
ordinary objects use their world transform. Thus waypoint blips and live scene
objects share the radar selection surface but have different position sources.

`0x00F03B80` and `0x00F03BC0` save/load two radar preferences: a clamped integer
range/zoom value and a Boolean display mode. Applying the Boolean invokes the
underlying ground-radar widget and marks the mediator for refresh.

## Replacement contract

The ground-radar implementation needs:

1. player and camera/world heading inputs;
2. procedural-terrain sampling/rendering at player position;
3. a spatial-query/blip provider for relevant client objects;
4. waypoint objects using their explicit replicated location;
5. range and display-mode persistence;
6. deterministic world-to-radar projection and hit testing; and
7. original UI atlas/frame assets and color/icon conventions.

Network traffic does not directly supply the radar image. It supplies the
objects, waypoints and state from which the client constructs the presentation.

## Confidence

| Finding | Confidence | Evidence |
|---|---|---|
| Ground radar uses procedural terrain | Confirmed | RTTI cast and update path at `0x00F03A40` |
| Radar rotates from world transform | Confirmed | heading calculation at `0x00F03A40` |
| Tooltip includes object/waypoint location and distance | Confirmed | `0x00F03C60` |
| Range and mode are persisted | Confirmed | paired save/load functions `0x00F03B80`/`0x00F03BC0` |
| Blip and waypoint layers are separate panes | Confirmed | MSVC RTTI/vtables |

Exact filtering/color priority for every blip category remains detailed fidelity
work, not an architectural blocker.
