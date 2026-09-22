# Dynamic textures, render-target copy path, and reset lifecycle

Date: 2026-09-12

Scope: `C:\Stardust-DEV\Stardust.exe` and its configured `s207_r.dll`. This checkpoint does not treat `SWGEmu.exe` as evidence.

## Outcome

This block recovered the procedural `Texture` construction family in the executable and a distinct raster-DLL render-target/copy helper. It also connected the helper to the renderer's pre-reset release and post-reset reconstruction phases. The initially inspected `0x62A110C0..0x62A11F60` cluster was rejected as a render-target implementation; it belongs primarily to compiled PASS vector copying and vertex-buffer handling.

## Confirmed findings

### Executable-side procedural textures

All three factories allocate and return a fetched 0x30-byte `Texture`:

| Factory | Constructor | Behavior |
|---|---|---|
| `0x00765000` | `0x00768C30` | Creates a one-pixel CPU-initialized texture and writes a clamped packed four-channel value. |
| `0x00765070` | `0x00768D60` | Creates a texture from supplied CPU pixels; copies whole rows or honors backend pitch. |
| `0x007650E0` | `0x00768EB0` | Creates an empty procedural/renderable texture with topology/size/mip/format inputs. |

`0x007650E0` has direct users in post-processing/bloom (`0x0064B1E0`, shaders `2d_downsample_4x4`, `2d_blur`, `2d_bloom`), the dynamic-normal-map test (`0x00507880`), terrain/environment texture setup (`0x00845CC0`), UI/radar canvas allocation (`0x008608F0`), and another size-driven texture user (`0x00869CD0`). This proves the generic texture abstraction serves CPU-filled, renderable and subsystem-owned dynamic resources.

### Raster render-target/copy helper

`0x62A0AD10` saves render target 0 and the current depth-stencil surface, evicts managed resources, and lazily creates two fixed 512x512 A8R8G8B8 textures:

- a D3DUSAGE_RENDERTARGET / default-pool texture;
- a system-memory texture used for readback/staging.

`0x62A0AF40` selects a texture surface (including a cube-face branch), computes the selected mip dimensions, selects a staging mip capable of containing those dimensions, clears the depth-stencil binding where required, and installs the selected surface as render target 0.

`0x62A0B120` performs readback when dimensions fit the 512x512 helper. `GetRenderTargetData` copies GPU target data to the system-memory surface; `D3DXLoadSurfaceFromSurface` copies/converts the requested rectangle to the target surface. D3D error `0x88760868` is handled as a nonfatal false result. Temporary COM surfaces are released, then the original render target and depth-stencil surface are restored by `0x62A0AED0`.

This is a scoped render-target copy/readback service, not the complete frame renderer.

### Device loss/reset ownership

`0x62A0AE30` releases all saved/temporary surfaces plus the default-pool render-target and system-memory helper textures. It is called both at helper shutdown and from renderer pre-reset routine `0x62A03520`.

`0x62A03520` invokes registered pre-reset callbacks, releases several other resource families, releases this helper, and marks reset-related state. Paired routine `0x62A03560` recreates this helper and other resource families, invokes registered post-reset callbacks, and marks reconstruction complete.

`0x62A0ACE0` / `0x62A0ACF0` are guarded install/remove wrappers for this helper. `0x62A04060` is the public selection/restoration wrapper: a non-null texture selects a target; null restores the saved target/depth state.

## Strong inferences

- Executable procedural-texture constructor byte at object `+0x14` distinguishes the render-target-capable path used by the raster helper; exact source enum naming remains provisional.
- The `param_2 == 6` branch in `0x62A0AF40` denotes a special surface-selection case, probably direct mip selection versus a translated cube-face value. Exact public enum labels are not yet assigned.
- Calls that bind `MAIN` and `SMAL` in the bloom setup are dynamic texture tags consumed by shader parameters, consistent with the previously recovered tag registry.

## Unknowns and limits

- Exact names and complete value domains for constructor topology, format and mip-policy parameters.
- Whether `0x88760868` corresponds to the precise historical symbolic D3D error assumed by surrounding code; the behavior is confirmed, the label is not.
- Full render-to-texture pass scheduling and all users of the raster API target-selection slot.
- The render-target helper uses fixed global scratch resources and is not evidence that all engine render targets are globally singleton.

## Replacement consequences

- Preserve the abstract operations—create sampled/renderable image, choose subresource, transition/bind as target, read back/copy, restore prior target—but do not reproduce global COM pointers or D3D9 pool semantics.
- DX12/Godot adapters require explicit image usage flags, barriers/layout transitions, staging/readback resources, fences and reset/recreation policy.
- Dynamic shader tag registration should reference renderer-neutral texture handles. The material system must not retain raw pointers to backend images.
- Post-processing clearly requires multiple same-frame render targets and shader chaining, so the future renderer needs an explicit render graph or equivalent ordered pass description.

## Stable provisional names

| Address | Provisional name | Confidence |
|---|---|---|
| `0x00765000` | `texture_create_solid_color_VA_00765000` | Confirmed broad behavior |
| `0x00765070` | `texture_create_from_memory_VA_00765070` | Confirmed |
| `0x007650E0` | `texture_create_procedural_VA_007650E0` | Confirmed broad behavior |
| `0x62A0AD10` | `d3d9_render_target_copy_helper_create_VA_62A0AD10` | Confirmed |
| `0x62A0AE30` | `d3d9_render_target_copy_helper_release_VA_62A0AE30` | Confirmed |
| `0x62A0AED0` | `d3d9_restore_saved_render_target_VA_62A0AED0` | Confirmed |
| `0x62A0AF40` | `d3d9_select_texture_render_target_VA_62A0AF40` | Confirmed broad behavior |
| `0x62A0B120` | `d3d9_finish_render_target_copy_VA_62A0B120` | Confirmed broad behavior |
| `0x62A03520` | `d3d9_pre_reset_release_resources_VA_62A03520` | Confirmed broad role |
| `0x62A03560` | `d3d9_post_reset_recreate_resources_VA_62A03560` | Confirmed broad role |

## Progress

- Comprehensive whole-client ledger: approximately **52.9%**, reported as **53%**.
- Replacement-relevant research: approximately **73%**.
- Renderer workstream: **47%**.

These remain conservative architectural-coverage estimates, not claims that 53% of raw machine-code functions have been individually named.

