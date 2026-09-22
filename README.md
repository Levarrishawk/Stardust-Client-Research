# Stardust Client Research

Latest consolidated continuation: [ANALYSIS_SESSION_2026-09-22_TO_1300.md](research/sessions/ANALYSIS_SESSION_2026-09-22_TO_1300.md).

Begin with [START_HERE.md](START_HERE.md). Current progress, authoritative indexes and the recommended review order are maintained there. The expanded whole-client evidence history is tracked in `COMPREHENSIVE_ANALYSIS_LEDGER.md`.

The directory layout and publication boundary are recorded in [WORKSPACE_MANIFEST.md](WORKSPACE_MANIFEST.md). Read [PUBLICATION_GUIDE.md](PUBLICATION_GUIDE.md) before initializing Git or adding a remote.

## Godot feasibility proof

The first bounded Godot host-engine proof is under `poc/`. See `poc/POC_RESULTS.md` for verified results and explicit non-claims. It does not replace or conclude the ongoing `Stardust.exe` reverse-engineering effort.

This directory is the resumable record for clean-room-style interoperability research on `C:\Stardust-DEV\Stardust.exe`. It documents observations, evidence, confidence, and open questions. It is not original source and does not claim that reconstructed pseudocode is source-equivalent.

## Ground rules

- Static analysis only unless a later task explicitly authorizes controlled dynamic analysis.
- Do not bypass DRM, authentication, licensing, encryption used as access control, or other protections.
- Preserve SWG wire and asset formats at compatibility boundaries; internal replacement-client types may differ.
- Label claims as **Confirmed**, **Strongly inferred**, **Tentative**, or **Unknown**.
- Use stable provisional names of the form `subsystem_role_VA_XXXXXXXX` (for example, `app_entry_VA_00401050`). Never silently repurpose a provisional identifier.
- Cite executable virtual addresses (VA), strings, imports/exports, or repository file/line evidence for every nontrivial claim.

## Documents

- [START_HERE.md](START_HERE.md) — canonical review entry point and document order

- [EXECUTABLE_OVERVIEW.md](docs/EXECUTABLE_OVERVIEW.md)
- [STARDUST_STATIC_ANALYSIS_LOG.md](docs/STARDUST_STATIC_ANALYSIS_LOG.md)
- [CLIENT_ARCHITECTURE.md](docs/CLIENT_ARCHITECTURE.md)
- [NETWORK_ARCHITECTURE.md](docs/NETWORK_ARCHITECTURE.md)
- [SOE_TRANSPORT_PROTOCOL.md](docs/SOE_TRANSPORT_PROTOCOL.md)
- [SOE_TRANSPORT_TEST_VECTORS.md](docs/SOE_TRANSPORT_TEST_VECTORS.md)
- [CORE3_CLIENT_REFERENCE.md](docs/CORE3_CLIENT_REFERENCE.md)
- [LOGIN_AND_ZONE_STATE_MACHINE.md](docs/LOGIN_AND_ZONE_STATE_MACHINE.md)
- [SERVER_BUILD_CONFIGURATION.md](docs/SERVER_BUILD_CONFIGURATION.md)
- [PACKET_HANDLER_MAP.md](docs/PACKET_HANDLER_MAP.md)
- [PROTOCOL_COVERAGE_AND_BOUNDARIES.md](docs/PROTOCOL_COVERAGE_AND_BOUNDARIES.md)
- [REPLICATION_SCHEMA_MAP.md](docs/REPLICATION_SCHEMA_MAP.md)
- [RENDERING_ARCHITECTURE.md](docs/RENDERING_ARCHITECTURE.md)
- [ASSET_PIPELINE.md](docs/ASSET_PIPELINE.md)
- [ASSET_FORMAT_CATALOG.md](docs/ASSET_FORMAT_CATALOG.md)
- [APPEARANCE_ANIMATION_ARCHITECTURE.md](docs/APPEARANCE_ANIMATION_ARCHITECTURE.md)
- [WORLD_TERRAIN_ARCHITECTURE.md](docs/WORLD_TERRAIN_ARCHITECTURE.md)
- [INTERIOR_PORTAL_ARCHITECTURE.md](docs/INTERIOR_PORTAL_ARCHITECTURE.md)
- [UI_ARCHITECTURE.md](docs/UI_ARCHITECTURE.md)
- [GAME_LOOP_ARCHITECTURE.md](docs/GAME_LOOP_ARCHITECTURE.md)
- [SCENE_LIFECYCLE_ARCHITECTURE.md](docs/SCENE_LIFECYCLE_ARCHITECTURE.md)
- [PLATFORM_INPUT_AUDIO_ARCHITECTURE.md](docs/PLATFORM_INPUT_AUDIO_ARCHITECTURE.md)
- [CLIENT_SUBSYSTEMS.md](docs/CLIENT_SUBSYSTEMS.md)
- [64BIT_PORT_PLAN.md](docs/64BIT_PORT_PLAN.md)
- [ENGINE_DECISION_REQUIREMENTS.md](docs/ENGINE_DECISION_REQUIREMENTS.md)
- [ANALYSIS_STATUS.md](ANALYSIS_STATUS.md)
- [ANALYSIS_SESSION_2026-09-22_OVERNIGHT_TO_0500.md](research/sessions/ANALYSIS_SESSION_2026-09-22_OVERNIGHT_TO_0500.md) — latest consolidated session report; twenty workflow blocks, compatibility findings, progress and remaining work
- Historical milestone reports superseded by the current ledger are retained under `research/archive/superseded-progress-reports/`.
- [UNKNOWN_FUNCTIONS.md](UNKNOWN_FUNCTIONS.md)
- [GHIDRA_WORKFLOW.md](docs/GHIDRA_WORKFLOW.md)
- [ANALYSIS_CHECKPOINT_2026-09-11_GHIDRA.md](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-11_GHIDRA.md)
- [ANALYSIS_CHECKPOINT_2026-09-11_BLOCK2.md](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-11_BLOCK2.md) — containment/remap wearables, PLAY codecs, malformed-delta propagation, derived-object schemas, and platform/session handlers
- [ANALYSIS_CHECKPOINT_2026-09-11_VFS_TRE_RUNTIME.md](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-11_VFS_TRE_RUNTIME.md) — executable-backed VFS priority, path normalization, TREE/TOC lookup, tombstones, and payload extraction
- [ANALYSIS_CHECKPOINT_2026-09-11_ASSET_IDENTITY_AND_FACTORY.md](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-11_ASSET_IDENTITY_AND_FACTORY.md) — exact filename CRC, verified archive vectors, APT redirects, and typed appearance factory registry
- [ANALYSIS_CHECKPOINT_2026-09-11_MESH_BOUNDARY.md](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-11_MESH_BOUNDARY.md) — MESH versions, extent/bounds compatibility, SPS dispatch, and hardpoint semantics
- [ANALYSIS_CHECKPOINT_2026-09-11_SPS_VTXA.md](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-11_SPS_VTXA.md) — shader groups, primitive/index records, VTXA declarations, and installed mesh version profile
- [ANALYSIS_CHECKPOINT_2026-09-12_TOPOLOGY_SHADER_ENVELOPE.md](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-12_TOPOLOGY_SHADER_ENVELOPE.md) — complete topology enum, SIDX camera selection, SHT/EFT/IMPL/PASS envelope, and mesh-tombstone closure
- [ANALYSIS_CHECKPOINT_2026-09-12_PASS_AND_SHADER_CORPUS.md](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-12_PASS_AND_SHADER_CORPUS.md) — PASS 0009 layout, fixed/program branches, full SHT/EFT corpus inventory, and later-patch compatibility risk
- [ANALYSIS_CHECKPOINT_2026-09-12_PASS_D3D9_COMPILER.md](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-12_PASS_D3D9_COMPILER.md) — engine/rasterizer PASS compilation, confirmed material/alpha/stencil/factor/scroll fields, and texture-stage version widths
- [ANALYSIS_CHECKPOINT_2026-09-12_FIFTY_PERCENT.md](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-12_FIFTY_PERCENT.md) — exact core D3DRS map, sampler compiler, effective shader overlay, and comprehensive 50% checkpoint
- [ANALYSIS_CHECKPOINT_2026-09-12_SHADER_VERSION_BOUNDARY.md](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-12_SHADER_VERSION_BOUNDARY.md) — effective IMPL 0009/PASS 0010 grammar and generated texture-coordinate vertex shader variants
- [ANALYSIS_CHECKPOINT_2026-09-12_STAG_FIXED_FUNCTION.md](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-12_STAG_FIXED_FUNCTION.md) — exact STAG records, effective-corpus distributions, D3DTSS backend and active-route reachability
- [ANALYSIS_CHECKPOINT_2026-09-12_TEXTURE_BINDING.md](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-12_TEXTURE_BINDING.md) — TXM schemas, tag lookup, sampler inheritance, named textures and global dynamic slots
- [ANALYSIS_CHECKPOINT_2026-09-12_DDS_TEXTURE_RESOURCE.md](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-12_DDS_TEXTURE_RESOURCE.md) — DDS formats, mip policy, texture cache, GPU allocation/upload and reset ownership
- [ANALYSIS_CHECKPOINT_2026-09-12_DYNAMIC_RENDER_TARGETS.md](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-12_DYNAMIC_RENDER_TARGETS.md) — procedural texture users, render-target copy/readback, and pre/post-reset resource lifecycle
- [ANALYSIS_CHECKPOINT_2026-09-12_FRAME_SCHEDULE.md](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-12_FRAME_SCHEDULE.md) — outer frame order, IoWin composition, semantic phases, presentation and device recovery
- [ANALYSIS_CHECKPOINT_2026-09-12_GROUNDSCENE_BLOOM_CUI.md](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-12_GROUNDSCENE_BLOOM_CUI.md) — exact world/bloom/GroundScene-post/CUI layering and TerrainObject environment boundary
- [ANALYSIS_CHECKPOINT_2026-09-12_PROCEDURAL_TERRAIN_SUBMISSION.md](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-12_PROCEDURAL_TERRAIN_SUBMISSION.md) — concrete procedural-terrain vtable, camera callbacks, visibility, batching and sorter submission
- [ANALYSIS_CHECKPOINT_2026-09-12_WATER_RENDERING.md](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-12_WATER_RENDERING.md) — global static/dynamic and local water primitives, WATR phase, culling and reflection boundary
- [ANALYSIS_CHECKPOINT_2026-09-12_INTERIOR_WEATHER_AUDIO.md](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-12_INTERIOR_WEATHER_AUDIO.md) — InteriorEnvironmentBlockManager schema/fallback and ServerWeatherMessage-to-ambient transition route
- [ANALYSIS_CHECKPOINT_2026-09-12_TERRAIN_CHUNK_PIPELINE.md](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-12_TERRAIN_CHUNK_PIPELINE.md) — asynchronous requests, worker build, three-state commit/reconciliation and safe teardown
- [ANALYSIS_CHECKPOINT_2026-09-12_TERRAIN_LOD_POLICY.md](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-12_TERRAIN_LOD_POLICY.md) — exact distance/height-bias policy, quadtree transitions, seam masks, retained cache and dirty-region correction
- [ANALYSIS_CHECKPOINT_2026-09-12_CLIENTCHUNK_MESH_BLEND.md](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-12_CLIENTCHUNK_MESH_BLEND.md) — nine-vertex terrain cells, cached four-layer materials, exact seam topology, occlusion models and CPU batching
- [ANALYSIS_CHECKPOINT_2026-09-12_TERRAIN_COLLISION_QUERIES.md](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-12_TERRAIN_COLLISION_QUERIES.md) — quadtree broad phase, stitched-triangle collision, height/normal lookup and occlusion separation
- [ANALYSIS_CHECKPOINT_2026-09-12_TERRAIN_AUXILIARY_GRIDS.md](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-12_TERRAIN_AUXILIARY_GRIDS.md) — typed flora/color/environment/normal grids and WMAP/SMAP boundary
- [ANALYSIS_CHECKPOINT_2026-09-12_FLORA_RUNTIME.md](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-12_FLORA_RUNTIME.md) — static/dynamic radial flora ownership, residency, fading, motion and batching
- [ANALYSIS_SESSION_2026-09-12_TWO_HOUR_TERRAIN_FLORA.md](research/sessions/ANALYSIS_SESSION_2026-09-12_TWO_HOUR_TERRAIN_FLORA.md) — consolidated multi-block session report and resume point
- [ANALYSIS_CHECKPOINT_2026-09-12_TERRAIN_LIGHTING_STATIC_FLORA.md](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-12_TERRAIN_LIGHTING_STATIC_FLORA.md) — lighting-mask correction and deterministic static-collidable flora path
- [ANALYSIS_CHECKPOINT_2026-09-12_INTERIOR_FLOOR_RUNTIME.md](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-12_INTERIOR_FLOOR_RUNTIME.md) — FLOR 0000..0006 loaders, stored acceleration/path data and cross-floor movement solver
- [ANALYSIS_CHECKPOINT_2026-09-12_PORTAL_DOOR_RUNTIME.md](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-12_PORTAL_DOOR_RUNTIME.md) — PRTO/CELL/PRTL loaders, cell-floor ownership, portal crossing and data-driven doors
- [ANALYSIS_CHECKPOINT_2026-09-12_DOOR_STATE_MACHINE.md](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-12_DOOR_STATE_MACHINE.md) — proximity triggers, paired leaves, transition effects and synchronized portal blocking
- [ANALYSIS_CHECKPOINT_2026-09-12_SKELETON_ANIMATION_FORMATS.md](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-12_SKELETON_ANIMATION_FORMATS.md) — SLOD/SKTM/LATT factories and compressed/uncompressed keyframe channel envelopes
- [ANALYSIS_CHECKPOINT_2026-09-12_ANIMATION_STATE_HIERARCHY.md](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-12_ANIMATION_STATE_HIERARCHY.md) — ASHT 0000..0002, action groups, recursive root states and default-root behavior
- [ANALYSIS_CHECKPOINT_2026-09-12_AUDIO_SOUND_RUNTIME.md](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-12_AUDIO_SOUND_RUNTIME.md) — SD2D/SD3D factories, exact v3 scalar envelope, writer symmetry, normalization and sample ownership
- [ANALYSIS_CHECKPOINT_2026-09-12_TRACK_ANIMATION_CONTROLLER.md](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-12_TRACK_ANIMATION_CONTROLLER.md) — logical/physical track mapping, queued transitions, pose caches, root motion and animation-message fan-out
- [ANALYSIS_CHECKPOINT_2026-09-12_INPUT_RUNTIME_AND_MAP.md](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-12_INPUT_RUNTIME_AND_MAP.md) — DirectInput device translation, normalized keyboard/mouse/joystick events, and canonical IMAP/0006 grammar
- [ANALYSIS_CHECKPOINT_2026-09-12_SKELETAL_POSE_MODIFIERS.md](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-12_SKELETAL_POSE_MODIFIERS.md) — signed yaw selection/cross-fade, look-at and target-pitch modifiers, and chained spine/neck/head attachment
- [ANALYSIS_CHECKPOINT_2026-09-12_STATE_HIERARCHY_RUNTIME.md](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-12_STATE_HIERARCHY_RUNTIME.md) — hierarchical state/link traversal, ancestor actions, LATT instantiation and loop/action/add routing into physical tracks
- [ANALYSIS_CHECKPOINT_2026-09-12_AUDIO_PLAYBACK_BRIDGE.md](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-12_AUDIO_PLAYBACK_BRIDGE.md) — concrete Sound2d/Sound3d runtime, effective 7,242-file corpus, selection/loop/envelope policy, type-aware Miles calls and exact resident/stream cutoff
- [ANALYSIS_CHECKPOINT_2026-09-12_ASHT_LINK_ACTION_POLICY.md](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-12_ASHT_LINK_ACTION_POLICY.md) — corrected path-based ASHT links, hierarchy transition ordering, action/add track selection and physical replace/queue policy
- [ANALYSIS_CHECKPOINT_2026-09-12_PLAY_MUSIC_AUDIO_ROUTE.md](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-12_PLAY_MUSIC_AUDIO_ROUTE.md) — complete PlayMusicMessage wire/parser/channel route and category-13 reachability audit
- [ANALYSIS_CHECKPOINT_2026-09-12_GAME_MUSIC_MANAGER.md](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-12_GAME_MUSIC_MANAGER.md) — GMUS schema, environment audio columns, combat/day-night states, paired space cues and layered music policy
- [ANALYSIS_SESSION_2026-09-12_ONE_HOUR_ASHT_MUSIC.md](research/sessions/ANALYSIS_SESSION_2026-09-12_ONE_HOUR_ASHT_MUSIC.md) — consolidated one-hour report, confidence, progress and next resume point
- [ANALYSIS_SESSION_2026-09-21_EVENING_MARATHON.md](research/sessions/ANALYSIS_SESSION_2026-09-21_EVENING_MARATHON.md) — seventeen-block closure covering residual asset semantics, async ownership, server schema risks, camera profiles, and exhaustive input command/binding/runtime inventories
- [ANALYSIS_CHECKPOINT_2026-09-22_SECURE_TRADE_UI_WORKFLOW.md](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-22_SECURE_TRADE_UI_WORKFLOW.md) — concrete mediator/panel/provider flow through item, money, accept, verify, complete, abort and Stardust session authority
- [ANALYSIS_CHECKPOINT_2026-09-22_SHADER_FORWARD_VERSION_REACHABILITY.md](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-22_SHADER_FORWARD_VERSION_REACHABILITY.md) — 27 effective IMPL/0009 + PASS/0010 winners, strict legacy rejection, and direct particle/environment reference graph
- [ANALYSIS_CHECKPOINT_2026-09-22_CRAFTING_CONTROLLER_CODECS.md](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-22_CRAFTING_CONTROLLER_CODECS.md) — byte-level crafting draft, ingredient, experiment, customization, result and close controller boundary
- [ANALYSIS_CHECKPOINT_2026-09-22_CRAFTING_RESPONSE_STATE_MACHINE.md](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-22_CRAFTING_RESPONSE_STATE_MACHINE.md) — counter-correlated slot, assembly, experiment, customize and finish transitions coordinated with replicated state
- [ANALYSIS_CHECKPOINT_2026-09-22_BLUEPRINT_TEXTURE_RENDERER_DORMANT_PATHS.md](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-22_BLUEPRINT_TEXTURE_RENDERER_DORMANT_PATHS.md) — complete dormant BTRT condition and operation behavior
- [ANALYSIS_CHECKPOINT_2026-09-22_SKELETAL_APPEARANCE_ALTER_ORDER.md](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-22_SKELETAL_APPEARANCE_ALTER_ORDER.md) — exact appearance-local controller, root-motion, message and transition order
- [ANALYSIS_CHECKPOINT_2026-09-22_GODOT_COORDINATE_ADAPTER.md](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-22_GODOT_COORDINATE_ADAPTER.md) — tested SWG-to-Godot transform, quaternion, winding and tangent mapping
- [ANALYSIS_CHECKPOINT_2026-09-22_NAMEPLATE_FRAME_CACHE.md](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-22_NAMEPLATE_FRAME_CACHE.md) — current/previous layout and projected-Y cache semantics
- [ANALYSIS_CHECKPOINT_2026-09-22_SUN_TRACKER_RUNTIME.md](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-22_SUN_TRACKER_RUNTIME.md) — SUNT local yaw/pitch equations, slew rates and dormant limits
- [ANALYSIS_CHECKPOINT_2026-09-22_STFP_FACTORY_FLAG.md](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-22_STFP_FACTORY_FLAG.md) — STFP Boolean as customization factory identity, not render state
- [ANALYSIS_CHECKPOINT_2026-09-22_BTRT_CLEAR_FIELDS.md](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-22_BTRT_CLEAR_FIELDS.md) — CFBC color/depth/stencil field mapping
- [ANALYSIS_CHECKPOINT_2026-09-22_BTRT_DEST_FORMATS.md](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-22_BTRT_DEST_FORMATS.md) — DEST dimensions, ordered format mapping and runtime output cardinality
- [ANALYSIS_CHECKPOINT_2026-09-22_OPTIONS_PREFERENCES_ARCHITECTURE.md](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-22_OPTIONS_PREFERENCES_ARCHITECTURE.md) — shared live preference bindings, defaults flow and specialized settings controllers
- [ANALYSIS_SESSION_2026-09-22_TO_1130.md](research/sessions/ANALYSIS_SESSION_2026-09-22_TO_1130.md) — consolidated eight-block report, confidence and resume focus
- `sound_scalar_inventory.py` / `sound_scalar_distributions.csv` / `sound_scalar_values.csv` — reproducible aggregate and per-descriptor inventory of all 28 common SD2D/0003 scalar words

## Baseline identity

| Artifact | SHA-256 | Purpose |
|---|---|---|
| `C:\Stardust-DEV\Stardust.exe` | `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150` | Primary subject |

No other executable is an analysis substitute or baseline. Repository server/test-client source is used only to correlate protocol behavior observed or sought in `Stardust.exe`.

Research checkpoint: 2026-09-08. Repository source and deployed executable may evolve independently; re-hash before relying on addresses.

This workspace is intentionally outside the Stardust-3 Git repository. It is now arranged so its curated portion can become a separate research repository after a publication review. Do not publish it until that review is complete; `local-private/` must remain ignored.

Latest continuation reports:

- [ANALYSIS_CHECKPOINT_2026-09-22_OPTIONS_PREFERENCES_ARCHITECTURE.md](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-22_OPTIONS_PREFERENCES_ARCHITECTURE.md) — typed option bindings, OK/Cancel/default transaction and custom controllers
- [ANALYSIS_SESSION_2026-09-22_TO_1300.md](research/sessions/ANALYSIS_SESSION_2026-09-22_TO_1300.md) — BTRT backend-format closure plus options/preferences session report
