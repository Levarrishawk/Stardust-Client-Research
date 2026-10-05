# World and Terrain Architecture

The [Round44 original layer-order trace](TERRAIN_HEIGHT_LAYER_ORDER_CONTRACT.md)
confirms fresh preparation, separate height/influence/product grids, and parent
whole-grid completion before child traversal for one pinned FHGT occurrence.
Actual preceding height writers are AHFR and a preceding filter is FFRA; fractal
values, FSLP/AFDN and derived-cache numerics remain open. The accepted supplied
flat sequence is a bounded implementation slice, not full original terrain parity.

The separate space-terrain family is `STAT/0000`, not a ground `PTAT` variant. All 16 effective space `.trn` files and their eleven child-form families have executable-backed grammar, including repeated `PLAN`, `DIST`, and `CELE` entries and active `SKYB` parsing. See [STAT space-terrain checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-23_STAT_SPACE_TERRAIN_GRAMMAR.md). Runtime rendering/visibility is still open.

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

## Round31 bounded implementation prerequisites

[Owned map masks](TERRAIN_MAP_MASKS_CONTRACT.md) confirm effective Tatooine PTAT/0014 stores a direct FORM0001 containing DATA/WMAP/SMAP; MAPS is a conceptual object name, not a required literal wrapper. The earlier top-level diagram already reflects this direct form. One original sample and independent hashes/grid bits validate retained mask bytes; world-coordinate conversion and active SMAP use remain outside the new API.

[Exact patch topology](TERRAIN_PATCH_TOPOLOGY_CONTRACT.md) implements all nine observed index patterns and checked rebasing. It selects explicit pattern indices, not an unverified neighbor mask mapping. Synthetic coordinates validate the helper without claiming generated procedural terrain. PTAT DATA/TGEN generation and complete outdoor rendering remain subsequent implementation milestones.

## Round32 accepted world and terrain inputs

[PTAT0013/0014 input](PROCEDURAL_TERRAIN_INPUT_CONTRACT.md) implements the exact DATA/read branches and owns complete TGEN bytes, with one original0014 sample and synthetic0013 fixtures. [Supplied patch geometry](TERRAIN_PATCH_GEOMETRY_CONTRACT.md) owns nine positions/normals/ARGB words, scaled base UVs, up to three of four exact auxiliary sets and explicit topology. The inspection adapter supports one auxiliary set, verifies reflection/winding/readback and balanced disposal. Original generated sample ordering, seam-mask selection and procedural height/material evaluation remain subsequent boundaries.

## Round33 terrain structure and creature state

Owned TGEN inputs expose 257 layers and 87 AHCN records in the same pinned Tatooine file while retaining unsupported rules. Exact nine-sample terrain assembly and seam branch priority pass independent static/native checks. Complete current CREO6 scalar/string/pair models now have bounded incarnation-owned publication and a prepared commit seam. Separately, the exact HAM/max-HAM vector codec and mutation kernel pass independent wire/state/counter/work oracles. These remain separate partitions, not a composed full CREO6 world update. Original mixed-world headless and combined Core regressions pass. Height evaluation, group semantics, vector composition, original owner side effects and live interoperability remain open. Research89%/readiness94%,2626 names and345 packet-source rows remain unchanged. [Combined evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R33_TERRAIN_AND_CREATURE_STATE.md).

## Round34 creature world and terrain arithmetic

[Constant-height operation](TERRAIN_HEIGHT_CONSTANT_OPERATION_CONTRACT.md) closes arithmetic order/local gating under explicit precision profiles. [Vertex color](TERRAIN_VERTEX_COLOR_CONTRACT.md) preserves normalization, conditional binary-mask endpoint selection and truncation-before-add. Influence, active worker control word, source RGB/mask production and full generator remain separate. [Combined evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R34_CREATURE_AND_TERRAIN_KERNELS.md).

## Round35 terrain and composite publication prerequisites

[BCIR input](TERRAIN_BOUNDARY_CIRCLE_INPUT_CONTRACT.md) adds typed boundary metadata. [Runtime arithmetic audit](TERRAIN_ARITHMETIC_RUNTIME_POLICY.md) traces actual worker creation and CRT endpoint initialization, while leaving worker initial/control-word mutation unknown. Precision, color enable/endpoints and coordinate producers remain explicit inputs. [Evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R35_TERRAIN_AND_COMPOSITE_PREREQUISITES.md).

## Round36 composite world and terrain boundaries

[Circle influence](TERRAIN_BOUNDARY_CIRCLE_INFLUENCE_CONTRACT.md) closes supplied finite query/shaping arithmetic with exact division/sqrt rounding and mandatory separate cache/loading and query/shaping precision. It retains the unspilled query for shaping. [BREC serialized input](TERRAIN_BOUNDARY_RECTANGLE_INPUT_CONTRACT.md) precedes a separate evaluator. Sample-coordinate production, boundary/filter/layer combination and complete generation remain open. [Evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R36_COMPOSITE_WORLD_AND_TERRAIN_BOUNDARIES.md).

## Round37 complete creature kernel and supplied terrain

[Supplied circle/AHCN patch](TERRAIN_CIRCLE_HEIGHT_PATCH_CONTRACT.md) and [surface query](TERRAIN_PATCH_SURFACE_QUERY_CONTRACT.md) pass a joined independent59-case/268-patch/4704-query oracle. All27 explicit precision triples are represented. [Coordinate producer](TERRAIN_SAMPLE_COORDINATE_PRODUCER_CONTRACT.md) closes the builder/layer/position graph and F32 stores, while actual margin/requests/runtime arithmetic and full layer combination remain open. Modern fixed up normals/color and direct influence handoff are not original generator behavior. [Evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R37_COMPLETE_CREATURE_AND_SUPPLIED_TERRAIN.md).

## Round38 full creature publication and terrain inputs

[Explicit coordinate grids](TERRAIN_SAMPLE_COORDINATE_GRID_CONTRACT.md) retain separate layer/position raw axes under four mandatory arithmetic profiles.81 combinations pass; duplicate finite axes are preserved separately from display eligibility. [Fresh request provenance](TERRAIN_CHUNK_SAMPLE_REQUEST_CONTRACT.md) confirms margin3 at construction and20-byte worker record arguments. Later settings, active profiles, cell-border joining and complete original generation remain open. [Evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R38_FULL_CREATURE_PUBLICATION_AND_TERRAIN_INPUTS.md).

## Round39 supplied terrain grounding and CREO4 prerequisites

[Padded supplied assembly](TERRAIN_COORDINATE_HEIGHT_PATCH_CONTRACT.md) and [original-actor supplied-terrain host](SCENE_WORLD_SUPPLIED_TERRAIN_PREVIEW_CONTRACT.md) pass independent native/headless/GPU joins. [General layer influence](TERRAIN_LAYER_INFLUENCE_CONTRACT.md) now records direct unspilled max/min, exact early exits, two inversions and incoming products. Original generator output remains unresolved. [Evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R39_TERRAIN_GROUNDING_AND_PRIVATE_VIEW.md).
## Round40 layer and CREO4 composition

Fresh BPOL influence, supplied layer combination, restricted CREO4 scalar/encumbrance composition and the separate byte-key skill-map kernel pass independent main replays. The supplied layer/AHCN/coordinate/patch/query join passes nine profiles/3600 samples/144 patches/576 queries. ADTA version-to-runtime inversion mapping is verified separately; unknown flag/cache roles remain explicit. Original-resource headless regressions and combined/default builds pass. Full CREO4 publication, original terrain generation, owner gameplay and live interoperability remain open. Research89%/readiness94%,2626 names and345 packet-source rows remain separate and unchanged. [Evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R40_LAYER_AND_CREATURE4_COMPOSITION.md).
## Round41 supplied height execution and CREO4 fields

Open-polyline influence, ordered supplied height execution, scalar/encumbrance/skill-map CREO4 composition and the separate mission-set kernel pass independent main replay. The native terrain join passes3600 samples/144 patches/576 queries across nine profiles. Actual computed-height original-actor headless/GPU joins pass400 samples/16 patches/nine grounded states and exact disposal; late malformed fourth-chunk input drains12 partial visuals. Static layer preparation closes enabled counts/local bounds/cache stride conditionally. Full14-field CREO4 composition/publication, real height filters, original complete terrain, gameplay and live interoperability remain open. Research89%/readiness94%,2626 names and345 packet-source rows remain separate and unchanged. [Evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R41_SUPPLIED_HEIGHT_AND_CREO4_FIELDS.md).
## Round42 complete private state and supplied terrain sequence

Complete14-field CREO4 codec/state and full prepared storage pass122 independent full-model sequences/2439 events. Separate supported storage passes122 sequences/924 events and40 prepared-seam checks. Ordered supplied layer sequences and a two-layer geometry/query join pass15456 plans and3600 joined samples/144 patches/576 queries. Direct FHGT evidence establishes current-height reads before this layer's affectors; the raw/query implementation passes375 raw cases/13959 queries. World routing, query-time filter composition, original recursive terrain, gameplay and live interoperability remain next. Research89%/readiness94%,2626 names and345 packet-source rows remain separate and unchanged. [Evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R42_PRIVATE_CREATURE_AND_TERRAIN_SEQUENCE.md).

## Round43 private world and current-height filters

Optional complete CREO4 world publication and original-resource headless/GPU joins pass independent full-model, ordered-event, variable-journal and cleanup checks. Query-time FHGT supplied execution and its two-layer geometry/query join pass independently. FSLP raw structure is implemented; initializer timing/x87 sine preprocessing remains unresolved. Server observer CREO4 deltas can arrive before end-baselines without a full view4 baseline; original default/template initialization and modern admission for that path remain the next compatibility milestone. The current seeded finalized private route does not yet cover it. Original recursive terrain, gameplay and live interoperability remain open. Research89%/readiness94%,2626 names/zero duplicates and345 packet-source rows remain separate and unchanged. [Evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R43_PRIVATE_WORLD_AND_HEIGHT_FILTERS.md).

## Round45 accepted compatibility increment

The [Round45 report](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_45_2026-10-04.md) records independently reviewed/replayed supplied
AHFR/FFRA consumers, an incarnation-bound received-only partial CREO4 store,
one human movement-template input chain and the actual current-height FHGT/AHCN
Godot preview. The store accepts pending/finalized objects without default seeds
but has no world/router admission yet. Original fractal generation/cache lifetime,
floating controls, complete terrain and later creature writers remain unknown.
Research scores and canonical name/packet denominators remain unchanged.

## Round46 accepted compatibility increment

The [Round46 report](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_46_2026-10-04.md) records received-only CREO4 world/router admission and its original-resource headless/GPU preview, an owned authored movement profile, and static family-map/sampler prerequisites. Partial/full4 policies are mutually exclusive; no default seed is fabricated. Fractal cache misses can return retained wide values while hits reload F32, so supplied-F32 consumer acceptance is not original sampler parity. Research scores,2626 canonical names and345 packet-source rows are unchanged.

## Round47 compatibility increment

[Retained supplied sample consumers](TERRAIN_RETAINED_FRACTAL_SAMPLE_CONTRACT.md) and the [786-case composition](TERRAIN_RETAINED_FRACTAL_CONSUMER_JOIN_CONTRACT.md) preserve AHFR retained scaling and FFRA mandatory F32 storage, including conditional unit-scale double rounding. A [seeded noise element](TERRAIN_FRACTAL_SEED_NOISE_CONTRACT.md) is independently modeled under explicit profiles. Complete sampler/cache/lifetime/effective control and original terrain admission remain open.

## Round48 compatibility increment

[Owned immediate seeded noise](TERRAIN_SEEDED_NOISE_KERNEL_CONTRACT.md) and [864 computed consumer joins](TERRAIN_COMPUTED_NOISE_CONSUMER_JOIN_CONTRACT.md) are implemented under explicit table/query profiles. The raw MFRC decoder preserves metadata but does not apply octaves/modes/optional curves. Full sampler/cache composition and original generated parity remain open.

## Round49 compatibility increment

[Complete conditional sampler composition](TERRAIN_COMPLETE_SAMPLER_CONSUMER_JOIN_CONTRACT.md) now passes432 actual Core joins, preserving108 final miss/cache differences. The [owned mode0 sampler](TERRAIN_CONDITIONAL_FRACTAL_SAMPLER_CONTRACT.md) preserves normalization/octave stores and atomic modern cache batches. Other modes/curves, effective original controls, request/family lifetime and generated terrain parity remain open.

## Round50 accepted boundary

The [owned family/sampler join](TERRAIN_FRACTAL_FAMILY_SAMPLER_JOIN_CONTRACT.md), [current resource movement host](SCENE_WORLD_CURRENT_MOVEMENT_RESOURCE_PREVIEW_CONTRACT.md), and [controller input chain](CREATURE_CONTROLLER_SPEED_INPUT_CONTRACT.md) extend the prior accepted slice. Exact conditional compositions and modern movement/source ownership are independently checked; original terrain/controls/full movement parity remain Unknown. See [Round50](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_50_2026-10-04.md) for assembled acceptance and next probes.

## Round51 bounded boundary

[Generator-to-family height routing](TERRAIN_GENERATOR_FAMILY_HEIGHT_JOIN_CONTRACT.md) passes486 actual joins under explicit fresh-call controls. [Run-default preference](PLAYER_CONTROLLER_RUN_FLAG_CONTRACT.md) and [acceleration helper](CREATURE_ACCELERATION_HELPER_CONTRACT.md) have bounded static evidence; message timing, getter sampling and units remain open. The shared original-resource host remains the separately accepted Round50 slice. [Round51 record](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_51_2026-10-04.md).

## Round52 accepted boundary

[Computed family-height terrain](SCENE_WORLD_FAMILY_HEIGHT_TERRAIN_PREVIEW_CONTRACT.md) reaches headless/GPU surface and grounding checks under explicit synthetic rules/controls. [Acceleration getter](CREATURE_ACCELERATION_TEMPLATE_GETTER_CONTRACT.md) and [run-input producer](PLAYER_CONTROLLER_RUN_INPUT_PRODUCER_CONTRACT.md) have bounded original evidence; update units, active queue binding and full terrain/controller parity remain open. [Round52](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_52_2026-10-04.md) records independent acceptance gates.

## Round53 controlled original-rule preview

The [owned selected AHFR](TERRAIN_GENERATOR_HEIGHT_FRACTAL_RULE_CONTRACT.md) now
feeds the [single-sampler patch](TERRAIN_SINGLE_SAMPLER_HEIGHT_PATCH_CONTRACT.md)
and [Godot surface/grounding preview](SCENE_WORLD_FAMILY_HEIGHT_TERRAIN_PREVIEW_CONTRACT.md#round53-selected-original-rule-with-one-sampler-per-patch).
Exact400 retained samples/heights and headless/GPU cleanup pass. Coordinates,
initial height/influence and arithmetic controls are caller-supplied preview
policy; full recursive terrain, activation and materials remain Unknown.


## Round54 accepted increment

The [selected original filter](TERRAIN_GENERATOR_FRACTAL_FILTER_RULE_CONTRACT.md) and [per-cell field](TERRAIN_FAMILY_HEIGHT_FIELD_CONTRACT.md) pass independent replays. Lead Version6 composes selected root AHFR and layer3 FFRA under explicit synthetic coordinates, prior heights and controls;400 native/headless/GPU cells, mixed/zero-state checks and late zero-visual-allocation rejection pass. Original layer admission, traversal and active controls remain Unknown.


## Round55 owned slice and filter pipeline

[Accepted bounded integration](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-04_R55_LAYER_SLICE_AND_FILTERED_FIELD.md) adds explicit original-layer evidence/rejection and immutable retained filter-to-field composition. Version7 headless/GPU/native/Fraction matches400 cells/16 patches/128 triangles/nine grounded states; late fourth-chunk invalid input allocates zero terrain visuals and drains cleanly. Prior Version6 remains verified. Original layer3's height curves are unsupported; full terrain traversal and original numeric parity remain unclaimed. Controller seconds have conditional carrier evidence; queue lifetime and physical acceleration remain open.


## Round56 boundary and nominal motion

[Round56 integration](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-04_R56_BOUNDARY_ADMISSION_AND_NOMINAL_SPEED.md) adds retained boundary-only first-gate evaluation and a60-step supplied nominal-speed trace through the real native motion driver/original-resource presentation. Native/Fraction/headless/GPU, late invalid-step preflight/no-new-publication drain and prior terrain gates pass. Original layer3 curve support remains unsupported; curve operand/store research closes with exact power/control parity still Unknown. Queue storage writer/cleanup is evidenced, with internals/lifecycle still open.


## Round57 filters, approximate curves and supplied targets

[Round57 integration](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-04_R57_BOUNDARY_FILTER_AND_TARGET_SPEED.md) adds boundary-admitted retained FFRA products, a separately labeled modern first-curve batch and supplied target construction through the original-resource motion host. Worker/lead numeric and malformed-input/cleanup gates pass; exact original curve output and full original locomotion remain unclaimed. Strict curve admission is unchanged.


## Round58 conditional curved-height display

[Version8 preview](SCENE_WORLD_FIRST_CURVE_TERRAIN_PREVIEW_CONTRACT.md) joins one original local BCIR/FFRA/AHFR layer using explicit modern first-curve approximation. Native/Fraction/Decimal/headless/GPU and late invalid-input zero-allocation cleanup pass. The [sparse batch](TERRAIN_FIRST_CURVE_FRACTAL_SPARSE_BATCH_CONTRACT.md) is independently verified; this complete-batch host claims no sparse query suppression. Original recursive activation and transcendental/control parity remain Unknown.


## Round59 sparse terrain and received local speed

[Round59 lead checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-04_R59_SPARSE_HEIGHT_NORMALS_AND_CURRENT_SPEED.md) adds positive-cell sparse heights, modern stored-height normals/lit policy and actual received local fields through paired supplied readers/atomic speed planning. Independent worker/main/native/headless/GPU/malformed-input/cleanup and prior-mode gates pass. Height-query count200 versus400 with explicit supplied warm priors; no original cache/normal/control/locomotion parity is inferred. Queue backing release ordering closes while concrete payload ownership remains Unknown. Research89/readiness94,2626 names and345 packet rows stay unchanged.

## Round60 composition boundary

The preview now consumes actual bounded engine-neutral [owned sparse evaluation](TERRAIN_HEIGHT_LAYER_SPARSE_FIRST_CURVE_EVALUATION_CONTRACT.md) and [modern height patch](TERRAIN_MODERN_HEIGHT_PATCH_CONTRACT.md) receipts. Height/normal output, accounting, ownership and rejection cleanup pass independent native and replacement-host checks. Original material and numerical parity remain open. Development stops after this round at Levarris's request.
