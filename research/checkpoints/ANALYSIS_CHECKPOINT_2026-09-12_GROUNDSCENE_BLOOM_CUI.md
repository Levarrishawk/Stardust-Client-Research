# GroundScene, bloom, and CUI composition checkpoint

Date: 2026-09-12

Subject: `C:\Stardust-DEV\Stardust.exe` SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.

## Result

The previously open nested frame order is now substantially closed. `CuiIoWin` is the top composition layer in the normal game path. Its render virtual captures the lower IoWin (the scene) into bloom, resolves/composites bloom, invokes a GroundScene-only post-scene stage, and only then renders the CUI. UI is therefore not blurred with the world. This is **Confirmed** by direct call order and RTTI.

```text
outer BeginScene
  -> IoWinManager top render
     -> CuiIoWin render 0x0093B2B0
        -> bloom begin/capture 0x0064B500
        -> render next/lower IoWin 0x00AB58E0
           -> GroundScene render 0x0051B770
              -> terrain/world appearance path
        -> bloom resolve/composite 0x0064B560
        -> GroundScene-only post pass 0x0051BCE0
        -> CUI manager/layer drawing 0x00881210
        -> optional screen-space fade/overlay quad
  -> outer EndScene
  -> Present
```

## Evidence

- `0x0093B2B0` calls `0x0064B500`, `0x00AB58E0`, and `0x0064B560` in that exact order. It then dynamic-casts the global `Scene` to `GroundScene`, conditionally calls `0x0051BCE0`, calls `0x00881210`, and finally may draw a rectangular overlay.
- `0x00AB58E0` invokes virtual slot `+8` on `this+8`, proving that it renders the next/lower IoWin rather than an unrelated object.
- The surrounding class evidence includes `.?AVCuiIoWin@@`, constructor-region string `CuiIoWin`, and the established IoWin vtable contract. Classification of `0x0093B2B0` as `CuiIoWin::render` is **Strongly inferred**, while its behavior is **Confirmed**.
- `0x0064B040` reads `ClientGame/Bloom`, `standardDeviation`, and `weightMultiplier`. `0x0064B1E0` loads `shader/2d_downsample_4x4.sht`, `shader/2d_blur.sht`, and `shader/2d_bloom.sht` and creates the render textures.
- `0x0064B560` executes downsample, horizontal blur, vertical blur, and final backbuffer composite. `0x0064B3D0` constructs the seventeen Gaussian weight/offset constants.
- `0x0051B770` obtains the singleton at `DAT_01947194`. RTTI and the `TerrainObject::remove` reference confirm this singleton as `TerrainObject`. Later vtable resolution corrected the appearance virtual `+0x128` classification: it is the terrain environment/clear-color getter, not geometry submission.
- Main wrappers `0x007558C0` and `0x00755C10` reach s207 D3D9 Clear and SetViewport respectively, confirming GroundScene camera clear/viewport setup rather than relying on decompiler guesses.
- `0x00561060` and `0x005610F0` install/remove camera-related callbacks only when the terrain appearance dynamic-casts to `ClientProceduralTerrainAppearance`. `0x00573650` separately detects `ClientSpaceTerrainAppearance`. These are strong evidence that the related `0x00573D90` vtable slot is a reusable scene-camera render driver spanning ground/space variants.
- `0x00881210` resets a full-screen viewport, temporarily adjusts camera/UI state, calls the CUI manager/layer chain, renders the root UI object through `0x010E7230`, performs vertex-info cleanup, restores graphics state, and draws an optional terrain-adjusted marker/debug overlay. Its broad role as the CUI render stage is **Confirmed**; exact original subroutine names remain unavailable.

## Conservative labels

| Address | Provisional role | Confidence |
|---:|---|---|
| `0x0093B2B0` | `cui_iowin_render_VA_0093B2B0` | Strongly inferred class/name; confirmed behavior |
| `0x00AB58E0` | `iowin_render_next_lower_VA_00AB58E0` | Confirmed |
| `0x0064B500` | `bloom_begin_scene_capture_VA_0064B500` | Confirmed |
| `0x0064B560` | `bloom_resolve_and_composite_VA_0064B560` | Confirmed |
| `0x0051BCE0` | `ground_scene_post_bloom_render_VA_0051BCE0` | Confirmed position; component identities partial |
| `0x00881210` | `cui_render_root_and_overlays_VA_00881210` | Confirmed broad role |
| `0x00B5B710` | `terrain_object_get_instance_VA_00B5B710` | Confirmed |
| `0x00B5CB50` | `terrain_object_get_environment_color_VA_00B5CB50` | Corrected by concrete ClientProceduralTerrainAppearance vtable; Confirmed |

## What remains open

- The four component calls within `0x0051BCE0` are bounded but not all named. One is conditional on GroundScene `+0xC4`, another on `+0xD4`; their likely environmental/debug-overlay roles are not promoted without more evidence.
- The final `CuiIoWin` rectangle is clearly a time/condition-controlled screen overlay, but whether its product name was fade, flash, dim, or transition remains unknown.
- The exact sorter activity inside `TerrainObject`'s concrete appearance virtual and the per-phase flush/sort policies still require descent.
- `0x00573D90` occupies matching vtable slots in two related tables. Its camera/world behavior and use of bloom are proven, but the owning original class name is not yet proven.

## Replacement consequence

The replacement frame graph can now preserve behavior without copying IoWin or D3D9 state:

1. Render the active world/scene layer to an HDR/color target.
2. Apply optional bloom to that scene target.
3. Execute ground-scene post-world overlays that intentionally sit above bloom.
4. Render CUI/root pages at native output resolution.
5. Render final fades/transitions.
6. Present.

The original asset requirement includes the three bloom shaders semantically, but a modern renderer may reproduce their behavior with modern shaders rather than execute D3D9 bytecode.

## Progress

- Expanded comprehensive whole-client coverage: **54%**.
- Replacement-relevant behavioral/specification readiness: **75%**.
- Renderer workstream: **55%**.

These are weighted research estimates, not a percentage of raw function symbols.
