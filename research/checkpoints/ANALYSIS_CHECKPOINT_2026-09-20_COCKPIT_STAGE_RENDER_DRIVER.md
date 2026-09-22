# Cockpit stage camera/render driver identity

Date: 2026-09-20

## Result

The previously unnamed owner of shared render driver `0x00573D90` is now resolved by MSVC RTTI. The function occupies the same virtual slot in `CockpitStage` and `CockpitLeaveStage` vtables.

## RTTI proof

The first table starts at `0x015DE768`; its preceding Complete Object Locator pointer `0x0160E750` names type descriptor `.?AVCockpitStage@@`. The second starts at `0x015DE79C`; locator `0x0160E79C` names `.?AVCockpitLeaveStage@@`. Both tables place `0x00573D90` at their fourth callable slot.

The adjacent RTTI string corpus also contains `.?AVThirdPersonStage@@`, establishing this as the space cockpit/third-person camera stage family rather than a generic scene camera class.

## Render behavior

`cockpit_stage_render_VA_00573D90` requires the owned scene/controller and camera pointers, conditionally installs a space-terrain callback after a time/state threshold, then:

1. begins bloom scene capture;
2. derives/configures the camera viewport/state;
3. updates the camera transform from the cockpit stage camera provider;
4. clears color/depth using the current environment color;
5. brackets the actual camera render with procedural-terrain callbacks where applicable;
6. submits the camera/world through the common camera render path;
7. executes a post-camera routine;
8. resolves/composites bloom.

The method is therefore a stage-owned world-camera render driver. Its ownership is now confirmed, while some subordinate virtual method spellings remain reconstructed behavior names.

## Confidence

- `CockpitStage` and `CockpitLeaveStage` ownership: **Confirmed by RTTI Complete Object Locators**.
- Camera/world/bloom ordering: **Confirmed by code**.
- Exact original source method name `render`: **Strongly inferred** from vtable position and behavior.

## Evidence

- RTTI/vtables: `0x015DE764..0x015DE7B0`, COLs `0x0160E750` and `0x0160E79C`
- render driver: `0x00573D90..0x00573EC0`
- paired update/readiness methods: `0x00573D80`, `0x00573ED0`, `0x00573EF0`
