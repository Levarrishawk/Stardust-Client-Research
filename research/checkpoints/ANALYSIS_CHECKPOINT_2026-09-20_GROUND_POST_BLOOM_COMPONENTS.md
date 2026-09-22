# GroundScene post-bloom component identities

Date: 2026-09-20

## Scope and result

This block closes the component identities in `ground_scene_post_bloom_render_VA_0051BCE0`, the stage executed after world bloom composition but before CUI rendering.

## Confirmed call sequence

```text
GroundScene::postBloom (0x0051BCE0)
  current camera from GroundScene camera array (0x0051A4E0)
    -> GameCamera screen-effect/overlay render (0x00788970)
  if GroundScene+0xC4:
    -> OverheadMap render (0x006DDD70)
  if GroundScene+0xD4:
    -> SpaceTargetBracketOverlay render (0x0057DA60)
```

`0x0051A4E0` indexes the camera pointer array at `GroundScene+0x80` using the current camera index at `+0x9c`. The target code cluster is anchored by configuration strings `ClientObject/GameCamera`, `disableScreenShader`, and `disableCameraJitter`, confirming the camera class family. `0x00788970` conditionally draws up to three screen-space shader/primitive effects and finalizes them through the graphics layer.

The `0x006DDD70` cluster is anchored by `ClientGame/OverheadMap` and its behavior projects the current camera/player relationship into a map overlay. The `0x0057DA60` cluster is anchored by `SpaceTargetBracketOverlay::install`, `ClientGame/SpaceTargetBracketOverlay`, `disable`, and `showTargetStatusForAllShips`; it reads the same current camera and scene state before drawing space target brackets.

These overlays are intentionally above bloom but below CUI. A replacement frame graph must preserve that composition boundary even if the individual overlay implementations are redesigned.

## Confidence

- Current-camera array/index and exact call/conditional order: **Confirmed by code**.
- GameCamera, OverheadMap, and SpaceTargetBracketOverlay class identities: **Confirmed by colocated class/config strings and behavior**.
- Exact original method spelling for `0x00788970`: **Unknown**; its screen-effect/overlay behavior is confirmed.

## Evidence

- post-bloom dispatcher: `0x0051BCE0..0x0051BD0E`
- active camera getter: `0x0051A4E0`
- GameCamera overlay routine: `0x00788970`
- OverheadMap routine/config cluster: `0x006DDD70`, string at `0x0188A188`
- SpaceTargetBracketOverlay routine/config cluster: `0x0057DA60`, strings at `0x0187A24C..0x0187A27C`
