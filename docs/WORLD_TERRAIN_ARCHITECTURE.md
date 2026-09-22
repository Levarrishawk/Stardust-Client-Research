# World and Terrain Architecture

## Confirmed ground-terrain container

`terrain/corellia.trn` is a `PTAT` version `0014` IFF form. Its top-level children are:

```text
PTAT/0014
|-- DATA                 terrain dimensions, chunking, water, time and flora/radial parameters
|-- TGEN/0000            procedural rule groups and layers
`-- 0001
    |-- DATA
    |-- WMAP (524,288 bytes)
    `-- SMAP (524,288 bytes)
```

The current Stardust server terrain reader supports PTAT versions 0013 and 0014. It decodes terrain size, chunk size, tiles per chunk, global water configuration/shader, time cycle, collidable/non-collidable flora ranges and seeds, and near/far radial ranges and seeds. Version 0015 is explicitly noted but unsupported in that reader. This is **Confirmed by server source**, while client RTTI independently confirms `ClientProceduralTerrainAppearance`, its template, client chunks and a dedicated zero-argument member-function thread wrapper.

## Procedural generator model

The `TGEN/0000` sequence contains shader, flora, radial, environment and map groups; an optional bitmap group; then a layer or layer group. Layers recursively contain:

- boundaries determining spatial influence;
- filters modifying influence based on height, slope, direction, fractals, bitmaps or shaders;
- affectors modifying height, color/shader, flora, radial placement, environment, water and passability;
- child layers with inherited/combined influence.

The server implementation evaluates height, environment and passability from the same recursive rule graph and supports object-ID-keyed runtime terrain modifications. This gives a high-quality behavioral reference for non-rendering terrain queries. It does not reconstruct client chunk tessellation, texture blending, flora instancing or streaming policies.

## Client runtime evidence

Stardust.exe RTTI exposes `ClientProceduralTerrainAppearance`, `ClientChunk`, `ClientCreateChunkData`, `ProceduralTerrainAppearance`, `TerrainAppearance`, spatial sphere-tree types and `MemberFunctionThreadZero<ClientProceduralTerrainAppearance>`. Constructor `0x00849350` confirms a dedicated client terrain worker, DPVS-facing object, LOD controller, three flora manager families, and global/local water managers.

The concrete vtable is `0x015E66B4`. GroundScene installs callbacks around every active camera draw. Terrain pre-render `0x0084AAA0` traverses visibility nodes and invokes selected renderable entries. `ClientTerrainSorter` then groups primitives into five category buckets, submits them through `ShaderPrimitiveSorter`, and recycles temporary nodes. This camera-integrated callback path—not the environment-color getter—is the terrain geometry boundary.

Material dependencies now include `effect/dot3_terrain.eft`, `shader/terrain_%sblend%i.sht`, `shader/terrain_cloudtile.sht`, and `terrain/environment/%s.iff`.

The asynchronous chunk lifecycle is now substantially recovered. A dedicated terrain member-function worker consumes an ordered request container under a shared mutex/condition, builds a 236-byte pooled ClientChunk outside the lock, and publishes a 20-byte completion record. The main thread runs a three-state normal/drain/reconcile controller: it installs a completed chunk only if its tree slot is still vacant, destroys stale/duplicate results, and drains or requeues work during region rebuilds.

The LOD policy uses squared distance from the observer to each quadtree node's bounds. Inside a configurable fine-detail radius it requests scale 1; beyond that radius, target scale increases according to `ceil(distance / (baseChunkWidth * threshold))`. Optional relief bias retains finer terrain for nodes with larger vertical range. Deployed defaults are threshold 1.0, high-detail threshold 3.5 chunk widths, height-bias factor 8.0, height-bias cap 50%, and height bias disabled.

Normal coarsening removes a ClientChunk's render-world object but retains the chunk and child nodes. The 16-byte region queue is separate dirty-region invalidation for terrain regeneration, not ordinary camera movement. No distance/LRU eviction was established.

Terrain destruction sets a stop byte, wakes the worker, joins it, then clears pending work and destroys uncommitted chunks. No zone-owned terrain data may therefore be freed while a worker can still publish into it.

## ClientChunk mesh and material construction

`client_chunk_build_render_data_VA_008618D0` converts generated grids into owned ClientChunk arrays and pooled per-cell primitives. Each visible cell is a nine-vertex patch: vertex 0 is the center and vertices 1 through 8 form the perimeter ring. A primitive retains nine positions, nine normal-like vectors, nine packed per-vertex values, a resolved shader reference, bounds, an index-list pointer and eight triangle planes.

Material selection samples a 3x3 classification neighborhood. `terrain_fetch_blend_shader_VA_008654F0` caches the resulting combinations and constructs `shader/terrain_%sblend%i.sht` instances supporting a base layer plus three additional layers. Confirmed parameter tags are `TERR`, `LVL0..3`, optional `NRM0..3`, and `ALP1..3`; source shaders expose `MAIN` and optionally `NRML`. Exact per-vertex blend-value encoding remains open.

The four-edge LOD mask selects one of nine embedded 16-bit index lists. The base list is an eight-triangle center fan; four alternatives collapse one perimeter midpoint and four alternatives collapse adjacent-edge midpoint pairs. Counts are 24, 21 or 18 indices. Stitching swaps the index-list pointer and recomputes triangle planes without regenerating vertices.

Visible primitives are not permanent individual GPU meshes. `terrain_shader_set_append_visible_geometry_VA_008668E0` appends their positions and selected indices into per-shader CPU batches, offsetting indices by the current destination vertex count. `ClientTerrainSorter` and `ShaderPrimitiveSorter` own the downstream renderer handoff. Occlusion data remains separate: ClientChunk builds either a real `(2N+1)^2` grid with `2*N*N` triangles or an eight-vertex/36-index proxy volume according to configuration.

## Terrain collision and surface queries

`terrain_appearance_collide_VA_0084BDF0` performs broad-phase traversal of the procedural terrain's four-way tree and dispatches candidate leaf segments to `client_chunk_collide_segment_VA_00860F60`. ClientChunk rejects against its extent, then checks each ShaderSet through `terrain_shader_set_collide_segment_VA_008662E0`. Exact intersection uses the primitive's current stitched 8/7/6-triangle index list and stored triangle planes, updating the nearest result point, normal and distance.

Two ClientChunk virtuals provide vertical surface lookup: `client_chunk_query_height_VA_00860FF0` returns intersection Y, while `client_chunk_query_height_normal_VA_00861140` also returns the triangle normal. Both cast a vertical segment across chunk Y bounds at the requested X/Z and test the same selected topology. Generic procedural-terrain wrappers locate the relevant leaf and call these virtuals.

The optional real/proxy occlusion model at ClientChunk `+0xDC` is not used by either collision or height lookup. This separation must remain explicit in a replacement. An engine-native heightfield/BVH may replace the legacy traversal, but it must represent the generated surface consistently and must not elevate occlusion proxies to gameplay collision.

## Auxiliary generated channels

Constructor callbacks identify the retained ClientChunk grids. `+0xA4` carries static-collidable flora samples; `+0xC8` is static non-collidable flora; `+0xCC` and `+0xD0` are dynamic-near and dynamic-far flora; `+0xC4` supplies their packed three-byte color; `+0xD4` selects an environment family; and `+0xD8` contains surface normals. Later executable tracing confirms `+0xA8` is a binary baked-lighting/shading mask used during terrain vertex-color construction, not passability.

Stardust source identifies PTAT `WMAP` and `SMAP` as water and seed maps. Executable loader `0x00B9A520` proves that MAPS/0001 is two F32 values (`worldSize`, `metersPerBit`), two U32 values (`bytesPerRow`, `bitDimension`), then two `bytesPerRow * bitDimension` LSB-first bit grids. Effective Tatooine stores 16384.0, 8.0, 256 and 2048, yielding two 2,048-square 524,288-byte masks.

The common terrain vtable exposes WMAP/SMAP point and inclusive rectangle-any queries. Three direct consumers use the WMAP point query for local camera water-coverage estimation, water-cell admission during terrain/render traversal, and early exclusion of water cells from a procedural cell-processing path. No direct consumer of either SMAP query was found in Stardust.exe. The replacement must implement the exact centered coordinate mapping and retain both maps losslessly; active SMAP semantics must not be invented.

## Flora runtime

Client terrain owns one `ClientStaticRadialFloraManager` for `FloraStaticNonCollidable` and two `ClientDynamicRadialFloraManager` instances for `FloraDynamicNear` and `FloraDynamicFar`. They share camera-relative radial residency but use distinct generated grids and distance policies. Static decorative flora owns individual appearances and smooth distance fading. Dynamic flora uses material buckets, normalized packed color, optional time-driven motion and batches up to 500 quads per primitive using a shared 3,000-index buffer.

The replacement may use GPU instancing/MultiMesh, but must preserve deterministic family selection, category separation, transforms, color and radial residency. Static collidable flora belongs to a separate object/collision path and must not be conflated with decorative flora.

## Water runtime architecture

Client terrain creates separate `ClientGlobalWaterManager2` and `ClientLocalWaterManager` objects. Both register their shader primitives explicitly into `WATR`. Global water selects a static or dynamically generated large planar primitive according to graphics capability; the static path can draw four independently visible grid quadrants. Local water owns individually bounded primitives, frustum-tests each, and registers only visible instances.

Water preparation derives above/below-plane state from the active camera, advances shader animation time, uploads planar constants and binds shader/vertex/index resources. No separate reflected/refracted scene camera or render-target pass appears in the recovered water path. Original shader data may still sample environment/dynamic textures; this remains to be decoded.

Cell/interior RTTI separately confirms `CellProperty`, `PortalProperty`, render-world portal appearances, portal barriers, floor meshes, collision properties and occlusion notifications. Ground terrain and portalized interiors should therefore remain separate world providers under a shared spatial/scene interface.

## Replacement constraints

- Preserve X/Z/Y protocol/file conventions at codec boundaries while using one documented internal coordinate convention.
- Keep procedural rule evaluation separate from render-mesh generation.
- Allow deterministic server-compatible height/passability queries even when visual chunks use engine-specific meshes.
- Treat WMAP/SMAP as cached/derived map inputs until their precise semantics are confirmed.
- Support background CPU chunk construction with immutable result publication, generation-aware stale-result rejection, and explicit main-thread/GPU handoff.
- Stop/cancel and join or fence all zone-owned jobs before terrain, assets or scene state are released.
- Do not bind the object replication model directly to an engine terrain node hierarchy.

## Engine-evaluation implications

Godot or another engine should be tested for large terrain chunk streaming, custom mesh generation, background preparation, material-layer limits, flora/radial instancing, water boundaries, origin/precision strategy and coexistence with portalized interiors. The engine need not reproduce the legacy chunk implementation, but it must represent the recovered terrain behavior and asset dependencies without a permanent engine fork.

## Remaining analysis

1. Preserve the now-closed ClientChunk vertex declaration, ARGB lighting color, auxiliary blend UVs and `ALP1..3` texture roles in implementation fixtures; original unpublished field names are not required.
2. Decode the supplied water shader/material contract and local polygon construction.
3. Decode PTAT `DATA` from representative zones and compare with server parsing.
4. Determine whether SMAP is consumed by an indirect/plugin/editor path absent from the direct Stardust.exe call graph; its format and dormant client interface are already closed.
5. Trace interior floor/cell collision and orchestration across replicated objects and complete zone transitions.

## 2026-09-12 lighting-mask and static-collidable flora correction

The byte grid at `ClientChunk +0xA8` is confirmed as a binary baked-lighting/shading input to terrain vertex-color construction, not passability. At `0x00861E4A..0x00861EAF`, zero/nonzero selects an endpoint color and generated RGB is interpolated toward it by `0.6f`. No height, collision, pathing or flora query consumes this field.

Static-collidable flora does not use the three client radial managers. Shared terrain code at `0x00B5E780` divides the world into deterministic 16-meter cells, derives a stable linear cell seed, queries `ClientChunk +0xA4`, creates full objects/appearances and tracks them in cell-residency maps. This must remain a distinct replacement subsystem from instanced decorative flora.

ClientChunk `+0xD4` is now confirmed as the environment-family byte grid. Query `0x008628E0` samples it using terrain position and resolves the byte through the PTAT family group. GroundEnvironment combines the resulting family ID with current weather and selects a 25-field descriptor from `datatables/environment/<terrain>.iff`; missing nonzero weather falls back to the same family at weather zero. The installed Tatooine terrain has 17 families and a complete five-weather matrix. Preserve this family grid as a gameplay-visible terrain output even if a modern engine replaces the legacy chunk renderer.
