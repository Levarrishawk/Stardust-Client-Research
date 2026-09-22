# Global and local water rendering checkpoint

Date: 2026-09-12

Subject: `C:\Stardust-DEV\Stardust.exe` SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.

## Result

The terrain-owned water subsystem is now separated into two concrete manager families and four shader-primitive classes. Both global and local water register explicitly into sorter phase tag `WATR`. Global water chooses a static or dynamic implementation according to graphics capability; local water owns a vector of bounded local primitives and culls each before registration.

No separate reflection or refraction scene-render pass was found in the recovered manager or primitive paths. The water code sets shader constants, binds its shader and vertex/index resources, and issues geometry draws. Reflection-like appearance may still be expressed by shader textures such as the environment map, but a mirrored-camera/render-target pass is currently **Unknown**, not inferred.

```text
ClientProceduralTerrainAppearance
  |-- ClientGlobalWaterManager2 (vtable 0x015E6F6C)
  |    `-- one primitive selected by graphics capability
  |         |-- StaticShaderPrimitive (vtable 0x015E6E44)
  |         `-- DynamicShaderPrimitive (vtable 0x015E6EA4)
  `-- ClientLocalWaterManager (vtable 0x015E7434)
       `-- vector<LocalShaderPrimitive> (vtable 0x015E73E4)

manager visibility/register
  -> test capability/frustum
  -> ShaderPrimitiveSorter phase lookup('WATR')
  -> add primitive to WATR
  -> prepare shader/constants/buffers
  -> indexed geometry draw
```

## RTTI and vtables

| Confirmed type | RTTI descriptor | Vtable |
|---|---:|---:|
| `ClientGlobalWaterManager2` | `0x01898C70` | `0x015E6F6C` |
| `ClientLocalWaterManager` | `0x018992E8` | `0x015E7434` |
| `LocalShaderPrimitive@ClientGlobalWaterManager2` | `0x01898B8C` | `0x015E6EF4` |
| `StaticShaderPrimitive@ClientGlobalWaterManager2` | `0x01898BCC` | `0x015E6E44` |
| `DynamicShaderPrimitive@ClientGlobalWaterManager2` | `0x01898C0C` | `0x015E6EA4` |
| `LocalShaderPrimitive@ClientLocalWaterManager` | `0x018992AC` | `0x015E73E4` |

## Confirmed behavior

- Global manager constructor `0x008569D0` selects implementation by graphics capability returned from `0x00754D40`: values below `0x101` construct the dynamic primitive at `0x00854C50`; higher capability constructs the static primitive at `0x00854600`.
- Global manager registration `0x00856B60` resolves phase tag `WATR` (`0x57415452`) and registers its owned primitive with `ShaderPrimitiveSorter` when water is enabled.
- Local manager `0x008632F0` owns a dynamic vector. `0x00863AA0` forwards its per-frame operation to every local primitive. `0x00863AD0` frustum-tests each primitive through `0x00767F40` and registers visible primitives into `WATR`.
- Global static prepare `0x00854940` derives whether the camera is above or below the water plane, rebuilds resources when camera/output conditions change, uploads five four-float shader constant registers, binds shader/vertex/index resources, and tracks up to four enabled geometry quadrants.
- Static draw `0x00854B50` emits up to four indexed draw ranges according to a four-bit visibility mask. Counts are derived from vertex and index buffers rather than fixed asset values.
- Dynamic prepare `0x00854D60` obtains current camera state, updates/generated vertex and index buffers, chooses winding/normal direction from camera side, uploads the same planar-water constant family, and binds shader plus geometry resources.
- Local primitive prepare `0x00863210` binds its shader, vertex buffer and index buffer. Its adjacent draw virtual `0x00863240` is the corresponding geometry submission entry.
- Common primitive update `0x00855290` advances a double-precision animation/time accumulator and forwards delta time to the shader.
- Common position getter `0x008552C0` returns the global water reference position. Local primitives instead return their own center and expose a plane-height/distance value through `0x008631D0`.

## Geometry and asset model

Global water is a large planar grid centered or rebuilt relative to camera and terrain parameters. The static path retains GPU geometry and draws visible quadrants; the lower-capability dynamic path regenerates/binds geometry around the camera. Local water consists of individually bounded primitives, consistent with rivers, pools or authored local polygons. Exact PTAT field-to-constructor parameter labels are still pending.

The shader name is supplied through terrain/template data rather than hard-coded in these constructors. The previously recovered PTAT DATA includes global-water enable, height and shader configuration. Original asset compatibility therefore requires preserving shader logical paths and water geometry parameters even if the new renderer substitutes modern shader implementations.

## Reflection/refraction finding

The recovered functions call shader constant, shader binding, buffer binding and indexed-draw wrappers. They do not select a water render target, invoke another camera, or execute the bloom/render-target-copy service. A targeted executable string scan also found no water-specific reflection/refraction resource name. Therefore:

- separate mirrored/refraction camera pass: **not observed**;
- absence across the entire executable: **not proven**;
- shader-based environment/specular/reflection approximation: **plausible but not yet traced through the supplied water shader asset**.

## Provisional names

| Address | Provisional name | Confidence |
|---:|---|---|
| `0x008569D0` | `client_global_water_manager_construct_VA_008569D0` | Confirmed |
| `0x00856B60` | `client_global_water_register_watr_phase_VA_00856B60` | Confirmed |
| `0x00854600` | `global_water_static_primitive_construct_VA_00854600` | Confirmed class/role |
| `0x00854C50` | `global_water_dynamic_primitive_construct_VA_00854C50` | Confirmed class/role |
| `0x00854940` | `global_water_static_prepare_VA_00854940` | Confirmed broad role |
| `0x00854B50` | `global_water_static_draw_VA_00854B50` | Confirmed |
| `0x00854D60` | `global_water_dynamic_prepare_VA_00854D60` | Confirmed broad role |
| `0x008632F0` | `client_local_water_manager_construct_VA_008632F0` | Confirmed |
| `0x00863AD0` | `client_local_water_cull_and_register_watr_VA_00863AD0` | Confirmed |
| `0x00863210` | `local_water_primitive_prepare_VA_00863210` | Confirmed |
| `0x00863240` | `local_water_primitive_draw_VA_00863240` | Strongly inferred from vtable position and paired binding |

## Remaining unknowns

- Exact global static/dynamic capability threshold name and why the branch favors dynamic below `0x101`.
- Full vertex declaration and generated grid dimensions.
- Local-water polygon/triangle construction and PTAT/TGEN source records.
- Supplied water shader's texture tags, blend state and environment-map usage.
- Relationship between `WBLW`/`WABV` and underwater/above-water non-water geometry.
- Whether another subsystem implements planar reflection outside these managers; no evidence presently connects one.

## Replacement implications

- Model water as a terrain-owned semantic renderer with distinct global-plane and local-polygon inputs.
- Preserve `WATR` ordering between below-water and above-water transparent geometry.
- Replace camera-relative grid rebuilding with modern clipmap/grid techniques if visual behavior remains compatible.
- Do not implement an expensive reflection/refraction frame graph merely because modern engines support it; first recover the original shader contract and treat enhancement as optional.
- Keep water animation time and camera-side constants renderer-neutral and use original shader/material names as compatibility keys.

## Progress

- Expanded comprehensive whole-client coverage: **56%**.
- Replacement-relevant behavioral/specification readiness: **77%**.
- Renderer workstream: **61%**.
- Terrain/world workstream: **30%**.

These are weighted estimates, not raw function-count coverage.
