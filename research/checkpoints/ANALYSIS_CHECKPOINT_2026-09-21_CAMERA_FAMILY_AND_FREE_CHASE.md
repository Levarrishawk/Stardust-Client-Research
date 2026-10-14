# Camera Family and Free-Chase Controller

## Confirmed concrete camera types

MSVC RTTI and vtable recovery identifies these concrete or directly instantiated camera classes:

| Class | Vtable | Constructor |
|---|---:|---:|
| `Camera` | `0x015E351C` | `0x00767530` |
| `GameCamera` | `0x015E3E44` | `0x007885F0` |
| `RenderWorldCamera` | `0x015E37EC` | `0x00778E60` |
| `CockpitCamera` | `0x015DE95C` | `0x00580900` |
| `StructurePlacementCamera` | `0x015E15FC` | `0x006CB0F0` |
| `FreeChaseCamera` | `0x015E16A4` | `0x006D5EF0` |
| `ShipTurretCamera` | `0x015E173C` | `0x006D89B0` |
| `FlyByCamera` | `0x015E17B4` | `0x006D9400` |
| `DebugPortalCamera` | `0x015E182C` | `0x006D9C70` |
| `FreeCamera` | `0x015E18B4` | `0x006DB0D0` |
| `ObjectListCamera` | `0x015E437C` | `0x0078FED0` |

`ObjectWatcherListCamera`, `CameraController`, and blueprint-texture camera/setup types also have RTTI, but were not proven as ordinary scene-camera instances in this block.

All scene-camera derivatives share the same large base vtable prefix. They selectively override destruction, per-frame alteration and a target/activation-related slot while inheriting projection, viewport, transform and render-world services. Camera simulation is therefore cleanly separable from the graphics backend.

## Free-chase configuration asset

`FreeChaseCamera` loads `camera/freechasecamera.iff` in constructor `0x006D5EF0`. The accepted grammar is:

```text
FORM FCHS
  FORM 0000
    CHUNK ZOOM  repeated F32 values
    CHUNK FRST  F32
    CHUNK DEFH  F32
    CHUNK PSHM  repeated (S32, S32, F32)
    CHUNK PSTM  repeated (S32, F32)
```

Confirmed mechanics:

- `ZOOM` length divided by four determines the zoom-setting count;
- each zoom value is unit-converted before storage;
- the persisted `currentSetting` is range-checked and falls back to the middle setting;
- `PSHM` accepts only first index `[0,14]` and second index `[0,33]` before insertion;
- `PSTM` accepts only indices `[0,14]`;
- configuration/preferences include `ClientGame/FreeChaseCamera` keys `cameraMode`, `currentSetting`, `pitch`, and `cameraZoomSpeed`;
- global `ClientGame.spinYawDegreesPerSecond` controls automatic yaw input.

Later consumer and corpus analysis closes their behavior: `FRST` is the first-person distance threshold multiplier, `DEFH` is the default vertical target-height multiplier, `PSHM` is a posture-plus-active-state height override, and `PSTM` is the posture-only height map. See `ANALYSIS_CHECKPOINT_2026-09-21_FREE_CHASE_PROFILE_SEMANTICS.md`.

## Free-chase frame behavior

Per-frame alter `0x006D6970`:

1. requires an active camera and watched target;
2. resolves the watched object and, when available, its `PlayerCreatureController` and `ShipObject` identity;
3. drains mapped input messages into yaw, pitch, zoom-setting, camera-mode, automatic-spin and related toggles;
4. wraps yaw, clamps pitch, clamps zoom to the configured list and smooths zoom toward the selected value;
5. varies some sensitivity/zoom behavior for ships and feature state;
6. rebuilds camera orientation and position from the watched object's transform plus configured offsets;
7. invokes inherited camera/world update behavior.

The zoom control is discrete at the preference level but smoothly interpolated at runtime. Camera state is not derived from render matrices; it is simulation state which then writes the camera transform.

## Other modes

- `CockpitCamera` overrides the same per-frame and target/activation slots and is a first-class camera, not a FreeChase flag.
- `ShipTurretCamera` is likewise distinct and has its own update path.
- `FlyByCamera` uses separate transition timing/state.
- `StructurePlacementCamera` is a dedicated placement controller.
- `FreeCamera` has a large independent motion controller with yaw/pitch/translation commands, acceleration/smoothing, and local/world-relative movement.
- `DebugPortalCamera` and `ObjectListCamera` serve debugging/specialized render workflows and should not drive the first gameplay milestone.

## Replacement design consequence

Use a backend-neutral camera rig interface with concrete controllers. The initial compatible world client needs FreeChase plus its IFF configuration reader; cockpit, turret, fly-by, placement, and debug controllers can follow as separately testable modes. Preserve the legacy asset-driven numeric tables in an intermediate camera-profile model rather than hard-coding them in prototype host or renderer code.

## Confidence

- **Confirmed:** class inventory, vtable/constructor addresses, FreeChase IFF grammar, configuration keys, discrete zoom table and main input/update stages.
- **Strongly inferred:** high-level roles of several input IDs from state mutation; exact input names are available through the IMAP mapping rather than embedded here.
- **Unknown:** exact unpublished editor-facing expansions of the four tags and exact user-facing label of every mode value; the four chunk behaviors are closed in the later profile-semantics checkpoint.

## Evidence

- `camera_rtti_raw.txt`
- `camera_vtables.tsv`
- `decompiled-camera-family/`
- `C:\Stardust-DEV\Stardust.exe`
