# Space camera ownership and profile contract

Date: 2026-09-21  
Target: `C:\Stardust-DEV\Stardust.exe`

## Result

Cockpit and ship-turret views are independent `GameCamera` controllers with distinct target-resolution and asset contracts. They are not flags or special matrices inside `FreeChaseCamera`. A replacement scene must switch controller ownership while keeping projection/render submission in the shared camera service.

## Cockpit camera

Activation `0x00580CE0` first applies the common camera activation path, then resolves the watched player's controlled ship and its cockpit-profile path. Changing the resolved ship releases the prior retained reference. The profile is loaded by `0x005823F0` and supports:

```text
FORM CPIT
  FORM 0000
    FRAM  zero-terminated cockpit appearance-template path
    ZOOM  repeated F32 distances
    FRST  F32 first-person/cockpit transition threshold
    3OFF  three F32 third-person offsets
    1OFF  three F32 first-person offsets
    optional FORM ISPB
      HYPR three F32 hyperspace offsets
```

`ZOOM` is multiplied by configurable `ClientGame/CockpitCamera.zoomMultiplier`; a zero entry is always prepended. Distances are validated/trimmed relative to `FRST`. `0x00582A40` creates the profile's cockpit frame appearance, attaches it under the named `ShipCockpitFrame`, and hands the resulting object to camera ownership. The per-frame controller interpolates yaw/pitch and zoom, updates cockpit versus exterior visibility/render-stage selection, optionally aims at the current target, and delegates the final world-camera update to the common base.

The deployed `cockpit/xwing.iff` fixture contains frame `appearance/xwing_cockpit.apt`, seven serialized zoom distances `0,10,14,18,22,26,30`, threshold `10`, and zero first/third-person offsets. This proves the CPIT reader is a mandatory original-asset path, not merely a debug configuration.

## Ship turret camera

`0x006D88B0` resolves the watched player's ship and station. Only station values 4 through 11 are accepted and normalized to turret indices 0 through 7. The ship supplies the corresponding turret object; its named camera-mount transform is fetched into camera-local state. Activation retains/releases the resolved turret as the station changes.

The constructor builds twenty linearly spaced zoom/distance settings. Per-frame update composes the turret mount transform with the ship/world transform, handles discrete zoom input with smoothed distance, applies a global pitch offset, runs a collision trace from mount to desired camera position, shortens the distance on obstruction, and completes through the common `GameCamera` update.

## Ownership and transition rules

- The session/scene chooses one camera controller; controllers own transient target references and release them when the resolved ship/turret changes.
- Cockpit-frame objects are presentation children attached to a named frame and must be destroyed/replaced with the profile/session, not persisted in the network object model.
- A missing ship, station, turret, profile or mount transform disables that camera path safely for the frame; it must not fabricate a network object.
- Camera input state and interpolation remain simulation/controller state. historical implementation path (omitted) receives only the resulting transform, projection and visibility policy.

## Confidence

- Class separation, activation paths and retained-target replacement: **Confirmed by code**.
- CPIT grammar, X-wing fixture values and cockpit-frame attachment: **Confirmed by loader and original asset**.
- Station 4..11 to turret 0..7 mapping and mount-transform resolution: **Confirmed by code**.
- Exact user-facing names of every cockpit input message and optional `ISPB` acronym: **Unknown naming only**.
