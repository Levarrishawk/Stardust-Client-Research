# Terrain renderable dispatch identity closure

Date: 2026-09-21  
Target: `C:\Stardust-DEV\Stardust.exe`  
Scope: `0x00849350`, `0x0084AAA0`, `0x0084AC30`, `0x0084CAF0`, flora/water manager vtables

## Result

The objects dispatched through the virtual-`+8` thunk at `0x0084CAF0` are now identified from their producing constructors and owning vectors. This thunk is a generic interface call, not a class-specific renderer.

Two terrain-owned vectors use it:

| Terrain member | Producers inserted by `0x00849350` | Consumer |
|---|---|---|
| `+0x1B8` | `FloraDynamicFar` (`ClientDynamicRadialFloraManager`), `FloraStaticNonCollidable` (`ClientStaticRadialFloraManager`), `FloraDynamicNear` (`ClientDynamicRadialFloraManager`) | `client_procedural_terrain_visibility_render_VA_0084AAA0` |
| `+0x1C8` | optional `ClientGlobalWaterManager2`, optional `ClientLocalWaterManager` | `client_procedural_terrain_auxiliary_render_VA_0084AC30`, gated by the terrain water-enable member at `+0x1CC` |

The construction code appends the three explicitly labeled flora instances only to `+0x1B8`. It appends the global and local water manager results only to `+0x1C8`. This closes the former uncertainty over whether the selected entries might be chunks or an unbounded family of terrain renderables: `ClientChunk` is not an element of either vector.

## Vtable evidence

- `ClientStaticRadialFloraManager` vtable `0x015E64C8` has `0x0085B3C0` at slot `+8`.
- `ClientDynamicRadialFloraManager` vtable `0x015E6FFC` also has `0x0085B3C0` at slot `+8`.
- `0x0085B3C0` is a `ret 4` no-op implementation. Flora's actual maintained renderables are registered through its radial/DPVS and shader-primitive machinery; this interface callback does not directly draw geometry in these two concrete classes.
- Existing RTTI already identifies `ClientGlobalWaterManager2` vtable `0x015E6F6C` and `ClientLocalWaterManager` vtable `0x015E7434`; their instances are the only water-vector producers.

The neighboring thunks preserve the same interface layout: `0x0084CAE0` invokes virtual `+4`, while `0x0084CB00` invokes virtual `+0x0C`. The terrain update path calls `+4` on both auxiliary vectors; the cleanup/auxiliary stage calls `+0x0C` on flora and conditionally `+8` on water.

## Confidence

- **Confirmed:** exact instance classes inserted into the flora and water vectors.
- **Confirmed:** both concrete flora vtables share the no-op virtual-`+8` implementation.
- **Confirmed:** `0x0084CAF0` is only a virtual dispatch thunk and has no independent behavior.
- **Unknown:** original source-level name of the small common interface and its three virtual methods. This is a naming gap, not a behavioral or architectural blocker.

## Replacement-client consequence

Model flora and water as separate terrain auxiliaries with independent ownership and submission paths. Do not treat the `+8` thunk as evidence for a shared immediate-mode draw routine and do not place chunks in this manager list. Modern flora may use instancing and modern water may use dedicated passes, provided their camera/update, visibility, and original-asset material behavior remain compatible.
