# Procedural terrain visibility and submission checkpoint

Date: 2026-09-12

Subject: `C:\Stardust-DEV\Stardust.exe` SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.

## Result

The concrete `ClientProceduralTerrainAppearance` vtable and its camera-integrated render path are now identified. Terrain is not drawn by the previously identified `TerrainObject` color query. Instead, GroundScene installs terrain callbacks around each camera's visibility/render operation. Those callbacks traverse visible terrain structures, aggregate terrain shader primitives into bounded batches, submit them to `ShaderPrimitiveSorter`, and recycle the temporary batch structures after the camera pass.

This corrects one narrow interpretation in the prior checkpoint without changing its world/bloom/CUI ordering.

```text
GroundScene 0x0051B770
  -> for each active camera: 0x00561180
     -> install procedural-terrain callbacks 0x00561060
     -> camera visibility/render virtuals 0x007685F0
        -> TerrainObject callback
           -> ClientProceduralTerrainAppearance pre-render 0x0084AAA0
              -> visibility-tree traversal
              -> visible renderable/chunk virtual +8
        -> terrain batching callback 0x0084EBA0
        -> terrain sorter-submit callback 0x0084F110
           -> ShaderPrimitiveSorter add 0x00773AE0
     -> remove callbacks 0x005610F0
```

## Confirmed evidence

- MSVC RTTI type descriptor `0x018791E8` resolves through complete-object locator `0x01619988` to vtable `0x015E66B4`, confirming the table belongs to `ClientProceduralTerrainAppearance`.
- Constructor `0x00849350` writes vtable `0x015E66B4`, initializes terrain/flora/water/render structures, installs the client terrain worker, and creates a DPVS-facing object.
- `0x00561060` dynamically checks `ClientProceduralTerrainAppearance`, then registers four callbacks on the active camera/render environment. `0x005610F0` removes the same four callbacks in reverse lifecycle roles.
- Callback stub `0x00561750` calls `TerrainObject` virtual `+0x10C`; the concrete terrain implementation is `0x0084AAA0`. It traverses a fixed local node stack, performs visibility tests, and invokes virtual `+8` on selected entries.
- `0x0084F110` walks five category buckets, their ordered nodes, and each node's primitive vector. Every primitive is passed to `0x00773AE0`, which obtains a phase/key through the installed sorter callback and appends a 0x28-byte sorter record through `0x00771D00`.
- `0x0084EDB0` groups incoming terrain primitives by category/shader identity under explicit capacity limits: accumulated vertex-like cost remains below `0x40000`, and each primitive contributes nine units plus its index/count contribution. This is a batching policy, not a serialized asset layout.
- `0x0084E510` installs `ClientTerrainSorter` pools: five top-level buckets and preallocated pools of 100 primitive-node/vector containers. RTTI string `ClientTerrainSorter::PrimitiveNode::ms_memoryBlockManager` corroborates the subsystem identity.
- `0x0084EBA0` transfers/cleans collected primitive references and resets the accumulated terrain phase mask. `0x0084E950` tears down the global sorter pools.
- The constructor creates both `ClientGlobalWaterManager2` (`0x008569D0`) and `ClientLocalWaterManager` (`0x008632F0`) when corresponding data/options are available. Water is therefore a terrain-owned manager family rather than merely a shader phase.
- Terrain material construction at `0x00864FE0` references `effect/dot3_terrain.eft` and patterned `shader/terrain_%sblend%i.sht`. Environment/cloud construction references `shader/terrain_cloudtile.sht` and `terrain/environment/%s.iff`.

## Correction to prior interpretation

`TerrainObject` wrapper `0x00B5CB50` calls appearance virtual `+0x128`. In `ClientProceduralTerrainAppearance`, that slot is `0x0084B770`, which returns the terrain environment/clear color through `0x00846B50` or a default. It is **not** the terrain draw entry. The actual renderable submission is callback-driven as described above.

## Provisional names

| Address | Name | Confidence |
|---:|---|---|
| `0x00849350` | `client_procedural_terrain_construct_VA_00849350` | Confirmed class and broad role |
| `0x0084B770` | `client_procedural_terrain_get_environment_color_VA_0084B770` | Confirmed behavior |
| `0x0084AAA0` | `client_procedural_terrain_visibility_render_VA_0084AAA0` | Confirmed broad role |
| `0x0084E510` | `client_terrain_sorter_install_VA_0084E510` | Confirmed |
| `0x0084EDB0` | `client_terrain_sorter_batch_primitive_VA_0084EDB0` | Confirmed broad role |
| `0x0084F110` | `client_terrain_sorter_submit_VA_0084F110` | Confirmed |
| `0x0084EBA0` | `client_terrain_sorter_finish_and_recycle_VA_0084EBA0` | Confirmed broad role |
| `0x00561180` | `render_active_cameras_with_terrain_callbacks_VA_00561180` | Confirmed behavior |

## Remaining terrain-render unknowns

- The selected entry type receiving virtual `+8` at `0x0084CAF0` is bounded but not yet tied to a single RTTI class; it may cover several terrain renderable managers rather than only `ClientChunk`.
- Exact meaning of the five sorter categories and mapping to TERR/WBLW/WATR/WABV remain open.
- `ClientGlobalWaterManager2` and `ClientLocalWaterManager` primitive generation, local polygon boundaries, shader selection, and reflection/refraction behavior require a dedicated descent.
- Chunk creation/LOD/worker-to-main-thread handoff is initialized but not yet reconstructed end-to-end.
- Flora manager roles are visible in construction (`FloraDynamicFar`, `FloraStaticNonCollidable`, `FloraDynamicNear`) but their culling/instancing paths remain open.

## Replacement implications

- Preserve terrain as an asynchronously prepared set of renderables feeding a renderer-neutral visibility and batching interface.
- Do not copy the global five-bucket/100-node pools or `0x40000` legacy batching limit as compatibility rules; they are implementation policies.
- Preserve terrain material identity and blend-layer semantics from original assets, including dot3 terrain effects and patterned blend shaders.
- Model global and local water separately until their asset and geometry contracts are recovered.
- Terrain environment color belongs to scene clear/environment state, while terrain geometry submission belongs to camera visibility callbacks; these must remain separate concepts.

## Progress

- Expanded comprehensive whole-client coverage: **55%**.
- Replacement-relevant behavioral/specification readiness: **76%**.
- Renderer workstream: **58%**.
- Terrain/world workstream: **23%**.

These are weighted implementation-grade estimates, not raw function-name coverage.
