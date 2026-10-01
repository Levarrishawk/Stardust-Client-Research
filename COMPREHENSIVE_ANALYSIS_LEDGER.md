# Comprehensive Client Analysis Ledger

Updated: 2026-09-25 measurement note; historical entries below retained unchanged

**Current scoring status:** the 2026-09-25 evidence-backed rescore is **89% comprehensive behavioral coverage / 94% implementation-and-engine-decision research readiness**. These supersede the historical 90%/96% planning estimates. Function/address coverage remains separately uncalibrated; 2,568 curated addresses divided by 93,287 non-unwind analyzer entries is only a 2.75% raw registry ratio, not a completion score. Do not extrapolate from block counts. See the [current rescore](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-25_EVIDENCE_BASED_COVERAGE_RESCORE.md), [research coverage rebaseline](RESEARCH_COVERAGE_REBASELINE_2026-09-25.md), and [model-transition audit](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-25_MODEL_TRANSITION_AUDIT.md).

This ledger measures the expanded objective: an implementation-grade picture of every material Stardust.exe subsystem before engine selection or construction begins. It excludes compiler thunks, standard-library internals and trivial forwarding functions unless they affect behavior or ABI boundaries.

2026-09-23 overnight continuation: 84 bounded blocks completed by the 05:00
EDT stop extend original-asset and runtime coverage, most notably the effective
CKAT/SKTM/XFMS animation chain, plus specialized playback actions, space
terrain/planet and portal/floor resources, lightsaber behavior, and
inventory/auction/crafting UI-to-server paths. The CKAT corpus validates
298,315 transform bindings, 171,000 animated rotation channels, 40,701
animated translation channels, four optional root-motion modes, 6,952 named
message groups and exact quantizer-table generation. Rare numerical edge
cases and cross-system long tails remain. This does **not** justify changing
the rounded **90% comprehensive / 96% implementation-grade** estimates;
additional bounded closures should be judged against remaining workstream
breadth, not raw block count. See the 2026-09-23 overnight session index.

2026-09-22 late continuation: playback-script `PBSC/0001` and `PBSC/0002` dispatch, the effective 32-file v0002 corpus, 25 direct action factory registrations, per-thread scheduler and scene-update placement are now evidence-backed. One installed script has `FIPL`/`PPRL` tags absent from the observed direct registry; five `PSKA/0003` actions in four installed files exceed this executable's `0002` PSKA ceiling. Ordinary reachability of these files remains unproven. The supported PSKA start path reaches skeletal animation. This adds animation/presentation breadth but does not yet warrant a whole-point increase over the current **90% comprehensive / 96% implementation-grade** planning estimates. See `research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-22_PLAYBACK_SCRIPT_CORPUS_AND_FACTORY.md` and `research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-22_PSKA_FORWARD_VERSION_AND_RUNTIME.md`.

The current weighted completion estimate is **90%**. This is a stricter
denominator than the replacement-relevant implementation estimate (**96%**);
no work has regressed. The table immediately below is the 2026-09-21 baseline
rubric snapshot retained for auditability; dated continuations record each
increment and the latest entries are authoritative for the current totals.

2026-09-22 evening continuation: CLEF runtime creation-to-retirement is now
mapped for particles, sounds, lights, camera shake and optional feedback;
version-sensitive CPAP/CLGT/CAMS/FFBK numeric fields are corpus-audited. A
literal trailing-space CPAP path corrected the full CEF direct-reference
availability from 1,877/12 to **1,876 live / 13 absent**, and the reachable
particle closure from 577 to **576** nodes. Enabled empty-name PTEX records
have a confirmed fallback material. Exact Light attenuation-like scalars and
archive-backed `.ffe` handoff to DirectInput remain open. This narrows the
asset/presentation risk but does not justify rounding comprehensive coverage
above **90%**. See `research/sessions/ANALYSIS_SESSION_2026-09-22_TO_1900.md`.

## 2026-09-21 baseline reconciliation (historical 82% snapshot)

The server packet matrix has reached 345 documented rows and zero reachable gaps for the current source tree. Subsequent blocks broadened the non-network denominator: UI mediator lifecycle, cameras, input and command policy, diagnostics, allocators, thread boundaries, gameplay managers, client effects/events, collision, weapon/ship presentation, and a corpus-backed reconstruction of the `ClientDataFile` dispatcher and its principal record families. The effective CDF corpus and runtime consumers now constrain object composition, damage/destruction, events, customization, wearables, lights, flora-clearing zones, movable wings, contrails, turrets/flags, ship-weapon recoil, and engine audio.

The conservative whole-client estimate advances to **82%**. The remaining 18% is real: long-tail UI/gameplay behavior, exact semantics for rare presentation scalars/extensions, additional ownership and failure-path auditing, and deeper coverage of specialized simulation/presentation code. Packet inventory closure must not be misreported as whole-client closure.

| Workstream | Weight | Coverage | Exit criterion |
|---|---:|---:|---|
| Executable identity, imports, resources, compiler/RTTI | 5% | 85% | remaining resource/class gaps catalogued |
| Startup, shutdown, configuration, logging, main loop | 5% | 70% | ordered lifecycle and failure cleanup reconstructed |
| SOE transport, sockets, reliability and policy | 12% | 80% | creation, transforms, timers, remap and close semantics specified |
| Login, ConnectionServer and zone state machines | 8% | 82% | every required transition/message/failure path mapped |
| Named/controller messages and handler registry | 12% | 40% | material families inventoried; required codecs/handlers mapped |
| Object hierarchy, baselines, deltas and lifecycle | 12% | 45% | major shared/derived classes have schemas and state effects |
| VFS/TRE, templates, tables, localization and assets | 10% | 73% | lookup precedence, static meshes, shader/effect/pass/texture envelopes, and corpus versions are evidence-backed |
| Renderer, shaders, materials, meshes, textures, effects | 12% | 68% | outer frame, scene/bloom/CUI, terrain/water/flora submission, chunk material batching, targets and reset lifecycle reconstructed |
| Animation, skeletal appearance and equipment | 6% | 10% | graph, locomotion, attachments and blending mapped |
| Terrain, world, collision, camera and transitions | 5% | 64% | terrain generation/LOD/seams/mesh/collision plus typed flora/environment channels and radial flora runtime mapped |
| UI, input, commands and mediator lifecycle | 6% | 17% | framework and representative interaction families mapped |
| Audio | 3% | 20% | initialization, lookup, spatial playback and shutdown mapped |
| Memory, allocators, threading and jobs | 2% | 20% | ownership, pool and concurrency boundaries catalogued |
| Remaining client gameplay/simulation | 2% | 10% | combat, crafting, missions, social and space roles classified |

Percentages are conservative research judgments, not raw function-count ratios.
The table above is historical; the current result is stated in the opening
paragraph and at the end of the latest dated continuation.

## 2026-09-13 two-hour marathon continuation

Seven completed units closed the modern EMTR flocking/cage schema and its 0011→0012 boundary, local-space/collision/wind behavior, environment-color influence, CPU quad batching, PTMH resource/tint policy, the first server-source packet gap matrix, and five high-use message handlers. After those closures were published and the matrix rerun, it separates 210 documentation hits from 135 source-referenced candidates, only 60 of which expose standalone literal opcodes (58 S2C and 2 C2S); this prevents structural helper classes from inflating the handler backlog. ChatSystem, PlayClientEffectObject, PlayClientEffectLoc, CreateClientPath and DestroyClientPath are now end-to-end mappings.

The weighted whole-client estimate advances conservatively to **77%**. The dominant remainder is breadth rather than foundational architecture: 60 literal-opcode packet candidates after this block, derived baseline/delta families, UI/gameplay workflows, animation exceptions, specialized rendering/visibility, camera/space behavior and lifecycle/concurrency audit.

### 2026-09-14 auction action-response block

`BidAuctionResponseMessage`, `RetrieveAuctionItemResponseMessage`, and `CancelLiveAuctionResponseMessage` are now end-to-end mappings. Each has an exact embedded descriptor, listener branch, archive parser, fixed-width schema, AuctionManagerClient model handler, and typed UI publication. This closes three more active literal-opcode candidates. The post-publication matrix now reads **213 documented / 132 reachable-gap candidates / 57 literal-opcode gaps**. The cancellation trailing U8 remains intentionally opaque but is confirmed as consumed, ignored by the handler, and emitted as zero by Stardust. Weighted coverage remains **77%** because this is meaningful packet-map breadth inside an already mature protocol category rather than another full percentage point of the entire client.

### 2026-09-14 auction query-header block

The principal auction-list response is now closed. The executable independently confirms Stardust's counter/search-type/string-table/name-table/item-array/offset/continuation schema and every compact item field. Its index resolver, screen-to-list routing, model conversion, cross-list reconciliation, pagination retention, and list-change publications are mapped. The replacement contract explicitly rejects using the observed x86 record sizes as wire layouts. The refreshed matrix reads **214 documented / 131 reachable-gap candidates / 56 literal-opcode gaps**.

### 2026-09-15 six chat blocks

Friend presence, room messages, room-list registry reconstruction, moderator removal, outgoing tell acknowledgement, and incoming tell delivery are now end-to-end mappings. These blocks establish the shared ChatAvatarId contract, nested room-list records, reserved-room filtering, moderator-set mutation, U32 tell-sequence correlation, ignore-before-publication policy, last-reply state, and explicit preservation of two opaque trailing dwords. The refreshed matrix reads **220 documented / 125 reachable-gap candidates / 50 literal-opcode gaps**. Weighted whole-client coverage advances to **78%** after six active handler closures and a materially more complete chat/social architecture.

## Completion rules

A workstream reaches 100% only when its lifecycle, important structures, input-to-state effects, server correlations, unknowns, evidence and confidence are durable enough for another session to resume without rediscovery.

## Current sequence

This list replaces the older 2026-09-11 ordering; the dated material below is
retained for provenance.

1. Resolve remaining active asset/render unknowns with bounded consumers:
   Light `+0x100..+0x108`, archive-backed `.ffe` handoff, and forward-version
   shader semantics where this executable has no decoder.
2. Alternate each asset block with a distinct long-tail animation, UI,
   simulation or lifecycle/ownership block; the remaining 10% comprehensive
   denominator is not solely an asset-format problem.
3. Re-audit exact original-asset path identity, fallback order, and any
   server-triggered presentation families not already covered by the 345-row
   packet matrix.
4. Perform a final contradiction/unknown and reproducibility audit before
   returning to engine/build selection. Do not equate this with naming every
   raw compiler-generated function.

## 2026-09-12 continuation estimate

| Area | Previous | Current | Reason |
|---|---:|---:|---|
| VFS/TRE, templates, tables, localization and assets | 65% | 67% | live IMPL 0009/PASS 0010 payload widths and child grammar are corpus-proven |
| Renderer, shaders, materials, meshes, textures, effects | 30% | 33% | generated texture-coordinate vertex shader cache/compiler is closed; STAG color/alpha semantics remain |

The conservative weighted total is approximately **50.6%**, reported as **51%** when rounded to a whole-number checkpoint. This is not a claim that 51% of raw functions have names; it is weighted, replacement-relevant research coverage under the established rubric.

## 2026-09-12 STAG fixed-function continuation

| Area | Previous | Current | Reason |
|---|---:|---:|---|
| VFS/TRE, templates, tables, localization and assets | 67% | 68% | effective-corpus STAG 0000 count, payload width, uniqueness and field distributions are measured |
| Renderer, shaders, materials, meshes, textures, effects | 33% | 36% | STAG loader layout, D3DTSS defaults/applicator and active programmable-pass reachability are separated |

The conservative weighted total is approximately **51.1%**, still reported as **51%** at whole-number precision. The STAG schema is structurally decoder-ready, but four exact semantic labels remain explicitly open.

## 2026-09-12 texture-binding continuation

| Area | Previous | Current | Reason |
|---|---:|---:|---|
| VFS/TRE, templates, tables, localization and assets | 68% | 70% | TXM 0000-0002 schemas and all 41,741 effective records are inventoried |
| Renderer, shaders, materials, meshes, textures, effects | 36% | 39% | tag lookup, sampler inheritance, named/dynamic sources and GPU binding are traced end-to-end |

The conservative weighted total is approximately **51.7%**, reported as **52%**. The next texture unit is below the parameter layer: DDS/resource creation, format selection, upload, mip behavior and device-loss recovery.

## 2026-09-12 DDS texture-resource continuation

| Area | Previous | Current | Reason |
|---|---:|---:|---|
| VFS/TRE, templates, tables, localization and assets | 70% | 73% | all effective DDS records, formats, dimensions, mips, cubes and anomalies are inventoried |
| Renderer, shaders, materials, meshes, textures, effects | 47% | 50% | BeginScene/IoWin/EndScene/Present envelope and twelve semantic sorter phases are traced |

The conservative weighted total is approximately **53.3%**, reported as **53%** at whole-number precision. The outer frame and phase envelope are bounded; nested GroundScene post-processing/UI placement remains open.

## 2026-09-12 GroundScene/bloom/CUI continuation

| Area | Previous | Current | Reason |
|---|---:|---:|---|
| Renderer, shaders, materials, meshes, textures, effects | 50% | 55% | exact lower-scene capture, four-stage bloom, GroundScene post pass, CUI and final-overlay order recovered |
| Terrain, world, collision, camera and transitions | 15% | 17% | TerrainObject singleton and appearance render boundary confirmed |
| UI, input, commands and mediator lifecycle | 15% | 17% | CuiIoWin lower-layer and root-CUI render composition confirmed |

The conservative weighted total is approximately **54.1%**, reported as **54%**. Remaining depth is concentrated in concrete terrain submission, animation, UI mediator behavior, audio, allocation/concurrency and gameplay families—not in outer-frame uncertainty.

## 2026-09-12 procedural-terrain submission continuation

| Area | Previous | Current | Reason |
|---|---:|---:|---|
| Renderer, shaders, materials, meshes, textures, effects | 55% | 58% | camera callback, terrain batching and ShaderPrimitiveSorter submission path recovered |
| Terrain, world, collision, camera and transitions | 17% | 23% | concrete terrain vtable, visibility traversal, material dependencies and water-manager ownership bounded |

The conservative weighted total is approximately **54.8%**, reported as **55%**. Global/local water and asynchronous chunk lifecycle are the next terrain depth units.

## 2026-09-12 water-rendering continuation

| Area | Previous | Current | Reason |
|---|---:|---:|---|
| Renderer, shaders, materials, meshes, textures, effects | 58% | 61% | global static/dynamic and local water primitive preparation/draw paths recovered |
| Terrain, world, collision, camera and transitions | 23% | 30% | both water managers, culling, WATR registration and camera-side behavior reconstructed |

The conservative weighted total is approximately **55.5%**, reported as **56%**. The next major terrain unit is asynchronous chunk creation, LOD and main-thread commit.

## 2026-09-12 asynchronous terrain-chunk continuation

| Area | Previous | Current | Reason |
|---|---:|---:|---|
| Renderer, shaders, materials, meshes, textures, effects | 61% | 62% | CPU chunk production and main-thread live-tree commit boundary recovered |
| Terrain, world, collision, camera and transitions | 30% | 38% | request ordering, worker publication, three-state reconciliation and stop/signal/join teardown reconstructed |

The conservative weighted total is approximately **56.9%**, reported as **57%**. Remaining terrain depth is concentrated in exact LOD/eviction policy, PTAT auxiliary maps, client chunk render-data layout and orchestration above zone teardown.

## 2026-09-12 terrain LOD-policy continuation

| Area | Previous | Current | Reason |
|---|---:|---:|---|
| Renderer, shaders, materials, meshes, textures, effects | 62% | 63% | active/deactive render-world ownership and four-edge seam-mask updates recovered |
| Terrain, world, collision, camera and transitions | 38% | 46% | exact distance equation, configuration defaults, camera cadence, quadtree transitions and persistent-cache behavior reconstructed |

The conservative weighted total is approximately **57.5%**, reported as **58%**. The next terrain depth unit is ClientChunk mesh/blend construction and the edge-mask-to-geometry path.

## 2026-09-12 ClientChunk mesh/blend continuation

| Area | Previous | Current | Reason |
|---|---:|---:|---|
| Renderer, shaders, materials, meshes, textures, effects | 63% | 67% | per-cell primitives, cached multi-layer materials and CPU batching boundary recovered |
| Terrain, world, collision, camera and transitions | 46% | 54% | exact nine-vertex topology, all nine seam patterns and occlusion representations reconstructed |

The conservative weighted total is approximately **58.4%**, reported as **59%**. The terrain mesh path is now constrained from generated arrays through renderer submission; remaining depth is concentrated in auxiliary-grid semantics, blend encoding, collision and PTAT WMAP/SMAP use.

## 2026-09-12 terrain collision/query continuation

| Area | Previous | Current | Reason |
|---|---:|---:|---|
| Terrain, world, collision, camera and transitions | 54% | 58% | quadtree broad phase, exact stitched-triangle segment collision, vertical height/normal queries and occlusion separation recovered |

The conservative whole-client estimate is now reported as **60%**. Terrain collision may use a modern acceleration structure, but the client-generated surface must remain consistent with authoritative server PTAT/TGEN height behavior.

## 2026-09-12 auxiliary-grid and flora continuation

| Area | Previous | Current | Reason |
|---|---:|---:|---|
| Renderer, shaders, materials, meshes, textures, effects | 67% | 68% | dynamic flora material bucketing and 500-quad submission boundary recovered |
| Terrain, world, collision, camera and transitions | 58% | 64% | ClientChunk flora/color/environment/normal channels and three radial flora-manager policies identified |

The comprehensive estimate is now **61%** and replacement readiness **82%**. WMAP/SMAP consumers, passability-mask proof, seed/tile formula and static-collidable flora remain explicit terrain unknowns.

## 2026-09-12 terrain-lighting and interiors continuation

| Area | Previous | Current | Reason |
|---|---:|---:|---|
| VFS/TRE, templates, tables, localization and assets | 73% | 76% | FLOR 0000..0006 and PRTO/CELL/PRTL version families, stored BTRE/BEDG/PGRF data and door-style table semantics recovered |
| Terrain, world, collision, camera and transitions | 64% | 72% | lighting-mask semantics, deterministic static-collidable flora, floor path walking, cell transitions, portal linking and live door blocking reconstructed |
| Renderer, shaders, materials, meshes, textures, effects | 68% | 69% | static-collidable full-object path and door appearance/effect synchronization bounded |

The weighted comprehensive estimate is now approximately **62%**. The earlier WMAP/SMAP uncertainty remains, but the supposed passability mask was corrected to a baked lighting/shading mask; server passability is procedurally evaluated. Interior movement now has an implementation-grade structural model, while exact path-walk enum labels and specialized visibility traversal remain open.

## 2026-09-12 animation, audio and input continuation

| Area | Previous | Current | Reason |
|---|---:|---:|---|
| VFS/TRE, templates, tables, localization and assets | 76% | 79% | SD2D/SD3D version envelopes, exact deployed sound scalar order, writer symmetry and IMAP/0006 grammar recovered |
| Skeletal appearance, customization and animation | 48% | 59% | SLOD/SKTM/LATT/CKAT/KFAT/ASHT families plus logical-to-physical track runtime, pose caches, root motion, queued transitions and event fan-out reconstructed |
| UI, input, audio and platform services | 43% | 55% | DirectInput acquisition/reacquisition, normalized device events, FIFO/coalescing, IoWin dispatch and data-driven input bindings reconstructed; audio template boundary materially deepened |

The conservative whole-client estimate is now **64%** and replacement-readiness research is **84%**. This does not mean 64% of raw machine functions have names. It means 64% of the weighted client behavior needed for a faithful clean-room replacement is documented to the evidence standard in this ledger. Remaining depth is concentrated in detailed gameplay/UI behavior, complete animation state semantics, exact sound-field labels/playback policy, specialized asset variants, and the long tail of message handlers.

## 2026-09-12 skeletal pose-modifier continuation

| Area | Previous | Current | Reason |
|---|---:|---:|---|
| Skeletal appearance, customization and animation | 59% | 65% | typed yaw template/runtime, signed child selection, cross-fade delegation, local-space look-at/target clamps and chained spine/neck/head attachment recovered |

The conservative whole-client estimate is now **65%** and replacement-readiness research is **85%**. The modifier block connects original animation assets to live post-controller bone changes; remaining animation depth is concentrated in complete ASHT link/action decisions, other modifier classes and appearance-specific exceptions.

## 2026-09-12 state-hierarchy runtime continuation

| Area | Previous | Current | Reason |
|---|---:|---:|---|
| Skeletal appearance, customization and animation | 65% | 71% | state-path common-prefix traversal, link and ancestor-action resolution, default LATT fallback, semantic loop/action/add routing and physical-track submission recovered |

The conservative whole-client estimate is now **66%** and replacement-readiness research is **86%**. ASHT/LATT assets are now connected to physical pose evaluation; remaining animation work is principally predicate/interruption detail and the long tail of specialized templates/modifiers.

## 2026-09-12 audio playback bridge continuation

| Area | Previous | Current | Reason |
|---|---:|---:|---|
| UI, input, audio and platform services | 55% | 65% | concrete Sound2d/Sound3d voice runtime, sample selection, dynamic volume/pitch/envelope policy and exact Miles volume/rate/obstruction/occlusion destinations recovered |

The conservative whole-client estimate is now **67%** and replacement-readiness research is **87%**. The audio boundary is no longer only a file-format envelope: original descriptor policies now connect to per-instance runtime state and backend calls. Subsequent closure within this same block confirmed loop-count versus sample-metadata loop-block ownership, all 7,242 effective descriptor shapes, category/master/per-instance gain separation, and the exact resident-versus-streamed 2D cutoff. Remaining audio depth is installed scalar-value distribution and exact names for lifetime-envelope, category and pitch modes.

## 2026-09-12 ASHT policy and game-music continuation

| Area | Previous | Current | Reason |
|---|---:|---:|---|
| Skeletal appearance, customization and animation | 71% | 75% | corrected link model, exact path matching, first-replace/later-queue transitions, direct-action replacement and movement/basic action semantics recovered |
| UI, input, audio and platform services | 65% | 72% | PlayMusicMessage fields and channel policy, GMUS state machine, planet environment audio columns, layered ambience and category-13 reachability bounded |
| Named/controller messages and handler registry | 40% | 42% | PlayMusicMessage is now traced from server constructor through parser, dispatcher and resulting audio state |

The conservative whole-client estimate is now **68%** and replacement-readiness research is **88%**. The music subsystem is implementation-grade at its wire, asset, state and channel boundaries. Remaining audio depth is concentrated in regional/weather override precedence, unresolved category-13 intended gain, and the long tail of descriptor scalar labels.

## 2026-09-12 interior and weather audio continuation

| Area | Previous | Current | Reason |
|---|---:|---:|---|
| UI, input, audio and platform services | 72% | 76% | exact InteriorEnvironmentBlockManager identity, 13-column schema, three-level POB/cell fallback and indoor/outdoor ambient precedence recovered |
| Named/controller messages and handler registry | 42% | 44% | ServerWeatherMessage wire/parser/dispatcher and wind/environment destinations traced against current server source |

The conservative whole-client estimate is now **69%** and replacement-readiness research is **89%**. Regional/weather audio precedence is now implementation-grade. Remaining audio gaps are category-13 intended gain, room-type acoustic intent and the descriptor scalar long tail; broad remaining client work is concentrated in replication, gameplay/UI and specialized asset consumers.

## 2026-09-12 environment-family visual continuation

| Area | Previous | Current | Reason |
|---|---:|---:|---|
| VFS/TRE, templates, tables, localization and assets | 76% | 80% | complete 25-column DTII outdoor descriptor, 50-table effective corpus and separate ENVM/0000 celestial asset recovered |
| Renderer, shaders, materials, meshes, textures, effects | 69% | 72% | sky/two-cloud/fog/ramp/environment-map inputs and fixed five-second previous/current descriptor blend reconstructed |
| Terrain, world, collision, camera and transitions | 72% | 76% | ClientChunk environment-family byte, PTAT resolution, composite family/weather key and exact fallback order confirmed |
| Named/controller messages and handler registry | 44% | 45% | ServerWeatherMessage is now connected through the complete environment selection and visual transition result |

The conservative whole-client estimate is now **70%** and replacement-readiness research remains **89%**. Outdoor environment selection is implementation-grade at the asset, terrain-family, weather, resource and transition boundaries. Remaining visual-environment depth is the friendly meaning of individual 256×8 ramp rows and detailed CELS celestial transform semantics.

## 2026-09-12 four-hour protocol, gameplay and appearance continuation

This session completed 23 bounded blocks after the environment-family visual
checkpoint. It closed SOE optional transforms; terrain tag classification,
global wind, PTAT WMAP/SMAP and structure-placement dependencies; startup stale
rows; ObjectController unknown-type policy; CommandQueueRemove; ticket/travel,
LaunchIntoSpace, radial menu, SUI, NPC conversation, character-sheet, group
space-invite, group-loot and image-design flows; customization manager,
palette, palette-column, presentation, group-instance and slot-rule data; and
the effective appearance/animation corpus plus MLOD/SLOD runtime containers.

| Area | Previous | Current | Reason |
|---|---:|---:|---|
| SOE transport, sockets, reliability and policy | 80% | 84% | optional transform negotiation and retained prepared-packet behavior closed |
| Startup, shutdown, configuration, logging, main loop | 70% | 74% | stale-row/config initialization behavior narrowed |
| Named/controller messages and handler registry | 45% | 57% | ten gameplay/UI protocol families mapped client-to-server-to-state |
| VFS/TRE, templates, tables, localization and assets | 80% | 87% | customization tables/palettes, complete appearance corpus and LOD containers recovered |
| Animation, skeletal appearance and equipment | 75% | 83% | customization presentation/slot application, QCHN codec, LDTB policy and MLOD/SLOD ownership closed |
| Terrain, world, collision, camera and transitions | 76% | 82% | WMAP/SMAP classification, wind and structure/travel/space transition paths narrowed |
| UI, input, commands and mediator lifecycle | 17% | 35% | radial/SUI/conversation/sheet/group/image-design workflows traced |
| Remaining client gameplay/simulation | 10% | 24% | travel, structure, loot and space invitation state machines classified |

The conservative weighted whole-client estimate is now **74%** and
replacement-readiness research is **92%**. This is weighted behavioral and
data-contract coverage, not a claim that 74% of raw disassembler function
objects have been individually named. The latter would be a compiler-output
cloning metric and includes thunks, CRT/STL, middleware and false boundaries.

The stable-address ledger now has 824 evidence rows covering 805 distinct
primary addresses; 789 rows explicitly carry Confirmed evidence, 43 carry
Strong evidence (categories overlap), and none is merely status `Unknown`.
The remaining material queue is maintained in
`FUNCTION_UNCERTAINTY_AUDIT.md`, not inferred from the historical filename
`UNKNOWN_FUNCTIONS.md`.

The 2026-09-15 `ChatOnCreateRoom` continuation closes opcode `0x35D7CC9F`: listener branch `0x00A6ECAA`, parser `0x00B07FD0`, shared room codec `0x00B045C0`, and semantic handler `0x00A2CF80`. Stardust source and executable behavior agree on the complete room record and request ID. Success inserts/merges the room into the registry/tree; ordinary failures publish localized feedback; code 24 is deliberately silent to suppress autojoin error spam. The refreshed packet matrix is **221 documented / 124 reachable gaps / 49 literal-opcode gaps**. Weighted whole-client coverage remains conservatively **78%**.

The following `ChatOnDestroyRoom` block closes opcode `0xE8EC5877`, parser `0x00B07490` and handler `0x00A2B100`. The client and Stardust server agree on ChatAvatarId/result/room-ID/request-ID ordering. A zero-result, zero-request broadcast is authoritative room removal and tree pruning; request-correlated failures produce `ui_chatroom:destroy_fail_prose`. Missing rooms and inconsistent mode combinations use diagnostic safe-no-op paths. The packet matrix is now **222 documented / 123 reachable gaps / 48 literal-opcode gaps**. Weighted coverage remains **78%**.

The subsequent `ChatOnUnbanFromRoom` block resolves the server/client naming difference (`ChatOnUnbanAvatarFromRoom`) and closes opcode `0xBAF9B815`, parser `0x00B04C90`, handler `0x00A2CC90`, both avatar identities, room path, result and request correlation. Result-zero messages remove the cached ban and refresh the tree; errors 4/6/12/16 and the generic case map to their exact `ui_chatroom` prose keys. The server-bound callback contract is also documented, moving two matrix entries to documented: **224 documented / 121 reachable gaps / 47 literal-opcode gaps**. Weighted coverage remains **78%**.

The next block closes both directions of add-moderator operation: `ChatOnAddModeratorToRoom` opcode `0x36A03858`, parser `0x00B06FD0`, handler `0x00A2C3A0`, and the server callback contract. Success inserts the normalized target into the cached moderator set and refreshes observers/tree; failures map to exact avatar/room/moderation/permission/fallback prose. Server source proves promotion can be a compound unban-plus-invite-plus-room-exposure transition. The refreshed matrix is **226 documented / 119 reachable gaps / 46 literal-opcode gaps**. Weighted coverage remains **78%**.

The following ban block closes `ChatOnBanFromRoom` opcode `0x5A38538D`, executable alias `ChatOnBanAvatarFromRoom`, parser `0x00B05160`, handler `0x00A2C9A0`, and the server callback. Result zero inserts the target into the cached banned set and refreshes observers/tree; exact result prose covers avatar/room/already-banned/permission/fallback cases. Stardust expands successful ban into distinct de-op, uninvite, kick, ban and private-room destruction notifications. The refreshed matrix is **228 documented / 117 reachable gaps / 45 literal-opcode gaps**. Weighted coverage remains **78%**.

The entered-room block closes `ChatOnEnteredRoom` opcode `0xE69BDC0A`, parser `0x00B08D60`, success `0x00A2BAF0`, failure `0x00A2D1D0`, and the request-ID/room-ID callback. Success inserts membership then distinguishes local activation from another-member notification; failure correlates and maps Stardust's room-missing/banned/not-invited outcomes. The large self branch registers retained room/UI state and refreshes the tree; special system-room helpers remain bounded. The refreshed matrix is **230 documented / 115 reachable gaps / 44 literal-opcode gaps**. Weighted coverage remains **78%**.

The leave-room block closes `ChatOnLeaveRoom` opcode `0x60B5098B`, parser `0x00B08950`, handler `0x00A2C040`, and the outbound callback. Common processing removes cached membership; local identity removes active-channel state and publishes self-left, while another identity publishes participant-left. Stardust fixes result/request to zero and reuses the outbound request for self-leave or authenticated moderator kick. The refreshed matrix is **232 documented / 113 reachable gaps / 43 literal-opcode gaps**. Weighted coverage remains **78%**.

The query-room block closes `ChatQueryRoomResults` opcode `0xC4DE864E`, parser `0x00AFF190`, handler `0x00A2B520`, shared room record and the outbound callback. Four counted identity lists replace members, invitees, moderators and banned users wholesale; the canonical room is merged/created and registry/tree observers are reconciled. Explicit request IDs and request-zero broadcasts share the same apply path. The refreshed matrix is **234 documented / 111 reachable gaps / 42 literal-opcode gaps**. Weighted coverage remains **78%**.

The invite-result block closes `ChatOnInviteToRoom` opcode `0x493FE74A`, parser `0x00B05EA0`, pending request resolver `0x00A6FD90`, handler `0x00A2DB50`, and the outbound callback. Although the response decodes actor/target/path, the handler trusts correlated pending context for target and room; success adds the cached invitee and refreshes tree/event state. Stardust supplies the broader unban/expose/notify/acknowledge/query transaction. The corrected matrix is **236 documented / 109 reachable gaps / 41 literal-opcode gaps**; the separate target-facing notification remains uncounted until traced. Weighted coverage remains **78%**.

The target-facing invitation block closes `ChatOnReceiveRoomInvitation` opcode `0xC17EB06D`, parser `0x00B06600` and handler `0x00A2D3B0`. It proves that the prior room-exposure message is a required ordering dependency: a missing room produces a diagnostic and no synthetic state. For an ordinary room the receiving client's own normalized avatar enters the cached invited set, while the packet avatar remains the inviter actor carried through `InviteeAdded`; the room tree and `ui_chatroom:invited_prose` presentation then update. Reserved group/guild branches instead publish retained room-ID changes. The refreshed matrix is **237 documented / 108 reachable gaps / 40 literal-opcode gaps**. Weighted coverage remains **78%**.

The uninvite block closes `ChatOnUninviteFromRoom` opcode `0xBE33C7E8` and `ChatUninviteFromRoomCallback`: outbound constructor `0x00AFC990`, inbound parser `0x00B059D0`, pending resolver `0x00A6FF10` and semantic handler `0x00A2DEC0`. Request ID selects a dedicated retained-context table; the redundant response identities/path are not authoritative. Correlated success removes the normalized target through `0x00A6A460(add=false)`, publishes `InviteeRemoved`, refreshes the room tree and emits exact result prose. Stardust's server transaction additionally coordinates de-op, kick, snapshot and target browser-room removal. The refreshed matrix is **239 documented / 106 reachable gaps / 39 literal-opcode gaps**. Weighted coverage remains **78%**.

The room-send block closes `ChatSendToRoomCallback` and `ChatOnSendRoomMessage` opcode `0xE7B61633`: client sender `0x00A289A0`, outbound constructor `0x00AFB960`, sequence table wrappers `0x00A6FA60`/`0x00A6FC10`, acknowledgement parser `0x00B062B0` and handler `0x00A2D9E0`. The second outbound Unicode field is real and must remain empty for Stardust's four-byte skip. Successful acknowledgement is silent and broadcast to all members separately from ignore-filtered message delivery; nonzero result selects moderator/privilege/fallback prose, while several server early returns send no acknowledgement. The refreshed matrix is **241 documented / 104 reachable gaps / 38 literal-opcode gaps**. Weighted coverage remains **78%**.

The friend-snapshot block classifies `ChatOnGetFriendsList` opcode `0xE97AB594` as an unsupported legacy message in this exact executable: no embedded name, RTTI, opcode literal, listener registration, parser or adjacent-DLL implementation exists. Stardust's wire contract is nonetheless complete—owner U64, count and repeated identity triples with compression enabled—and its send sequence immediately supplies the active alternative through `PlayerObjectDeltaMessage9`. The client registers `PLAY:9[7]` and `[8]` as friend/ignore string vectors, while the previously mapped incremental named update owns online presence. The refreshed matrix is **242 documented / 103 reachable gaps / 37 literal-opcode gaps**. Weighted coverage remains **78%**.

The matching ignore-snapshot block classifies `ChatOnGetIgnoreList` opcode `0xF8C275B0` identically: no executable name, RTTI, opcode literal, codec, registration or adjacent-DLL implementation exists. The server schema is owner U64, count U32 and repeated identity triples. `PLAY:9[8]` is active for initialization and normalized add/remove deltas, while CommunityManager exposes separate UI script wrappers. The refreshed matrix is **243 documented / 102 reachable gaps / 36 literal-opcode gaps**. Weighted coverage remains **78%**.

The ignore-mutation block classifies `ChatOnChangeIgnoreStatus` opcode `0x70E9DA0F` as another unsupported legacy notification in this executable and records its complete seven-field payload. Stardust sends it before the authoritative `PLAY:9[8]` delta, then sends localized success feedback; command validation failures use system messages without either state packet. Add/remove requests travel through ordinary queue commands rather than a dedicated callback. The refreshed matrix is **244 documented / 101 reachable gaps / 35 literal-opcode gaps**. Weighted coverage remains **78%**.

The paired friend-mutation block classifies `ChatOnAddFriend` (`0x2B2A0D94`) and `ChatOnChangeFriendStatus` (`0x54336726`) as unsupported legacy messages in this executable. Their server bytes and complete add/remove ordering are documented: the authoritative structural mutation is `PLAY:9[7]`, presence is separately delivered by the mapped presence update, and user results are localized system messages. The executable's differently named and structured `ChatAddFriend` RTTI is explicitly kept separate pending later role analysis. The matrix is **246 documented / 99 reachable gaps / 33 literal-opcode gaps**. Weighted coverage remains **78%**.

The chat-connect block closes `ChatOnConnectAvatar` (`0xD72FE9BE`) through descriptor `0x015E9F74`, zero-payload parser `0x00B0A600`, listener `0x00A6E9C0`, flag-setting handler `0x005AB170` and deferred CommunityManager update `0x005AB180`. The event coalesces a friend/ignore refresh until subsystem installation is complete. Stardust's class has no active producer found, so the replacement should support but not gate login on it. The matrix is **247 documented / 98 reachable gaps / 32 literal-opcode gaps**. Weighted coverage remains **78%**.

The chat-status block closes the duplicate-opcode definitions `ChatServerStatus` and `unkByteFlag` at `0x7102B15F`. Both carry one U8 status-like value, have no current server producer, and have no name/opcode/RTTI/listener presence in `Stardust.exe`; the repository test client recognizes the former but deliberately no-ops. They are classified as dormant compatibility artifacts. The matrix is **248 documented / 97 reachable gaps / 31 literal-opcode gaps** (`unkByteFlag` was already recognized by the corpus). Weighted coverage remains **78%**.

The holocron block closes open-page opcode `0x7CB65021` and close opcode `0xC0938A9D` through the UI listener at `0x00E40E40`. Open parser `0x013F34A0` extracts the page key, converts it to Unicode and forwards it into presentation; close directly deactivates the `WS_Holocron` mediator. The server actively exposes open to Lua, while close has no current producer. The matrix is **250 documented / 95 reachable gaps / 29 literal-opcode gaps**. Weighted coverage remains **78%**.

The tutorial-trigger block closes `NewbieTutorialRequest` (`0x90DD61AF`) and the client echo handled by `NewbieTutorialResponseCallback` (actual registered opcode `0xCA88FBAD`). Request parser `0x00AD3C60` arms subsystem-owned action flags; numerous event-edge callers construct `NewbieTutorialResponse` through `0x00AD2390`. The server maps selected echoes to observer events and gates `clientReady` into the tutorial screenplay. The matrix is **252 documented / 93 reachable gaps / 27 literal-opcode gaps**. Weighted coverage remains **78%**.

The generic-chat block closes `ConGenericMessage` (`0x08C5FC76`) through listener `0x00F38C20`, parser `0x013F4E70`, Unicode conversion and chat presentation wrapper `0x00F3BFD0`. The compressed schema is ASCII text plus a trailing U32 ignored by this client, and presentation is readiness-gated. No active Stardust producer was found. The matrix is **253 documented / 92 reachable gaps / 26 literal-opcode gaps**. Weighted coverage remains **78%**.

The object client-event block classifies `PlayClientEventObjectMessage` (`0xAF83C3F2`) as a dormant definition absent from `Stardust.exe` and without a current server producer. Its ASCII event/resource key, auxiliary key and U64 target schema are preserved, but it is explicitly separated from the active client-effect message because no semantic consumer exists. The matrix is **254 documented / 91 reachable gaps / 25 literal-opcode gaps**. Weighted coverage remains **78%**.

The inactivity block closes `ClientInactivityMessageCallback` at registered opcode `0x0F5D5325`. Client sender `0x0055E110` reliably transmits one U8 state from the configured `InputActivityManager` timer/update path; Stardust parses and only logs it. It is explicitly separated from SOE heartbeat/link-dead behavior. The matrix is **255 documented / 90 reachable gaps / 25 literal-opcode gaps**. Weighted coverage remains **78%**.

The player-resource block closes payload-free stomach (`0xB75DD5D7`) and money (`0x9D105AA1`) requests, sequentially and reliably sent by client helper `0x00FA1280`. Stardust returns stomach state via `PLAY:9` and money via two-U32 `PlayerMoneyResponse`; this executable parses then discards the latter, leaving replication authoritative. The matrix is **257 documented / 88 reachable gaps / 25 literal-opcode gaps**. Weighted coverage remains **78%**.

The `unkZone` block classifies opcode `0x58C07F21` as a dormant test artifact: the source marks it test-only, emits literal bytes `44 05 00 00`, has no producer, and the executable has no name/opcode/RTTI/listener presence. No zone semantics are invented. The matrix is **258 documented / 87 reachable gaps / 24 literal-opcode gaps**. Weighted coverage remains **78%**.

The customer-service block closes cancel-ticket (`0xD6FBF318`), compressed article (`0x934BAEE0`), new-activity request (`0x274F4E78`) and response (`0x6EA42D80`) rows through the shared client listener, exact codecs and typed result destinations. It corrects the matrix's shared-header request-opcode ambiguity. Only new-activity has a current Stardust producer/echo path. The matrix is **262 documented / 83 reachable gaps / 20 literal-opcode gaps**. Weighted coverage remains **78%**.

The ticket create/list block closes response opcodes `0x550A407A` and `0xBB567F98` through the same customer-service listener. Create parsing proves a two-U32 client contract that the dormant one-U32 server constructor does not satisfy. Ticket-list parsing confirms two header words and a counted record containing reserved fields, text, ID, U64 time and inverted unread/closed flags. The matrix is **264 documented / 81 reachable gaps / 18 literal-opcode gaps**. Weighted coverage remains **78%**.
The permission-list block closes active opcode `0x52F364B8`: two counted Unicode arrays and a Unicode list identifier pass through parser `0x0119B130`, UI listener `0x00E40E40` and `SwgCuiPermissionList` application `0x00FAC810`. Current Stardust structure and ship producers populate the primary array and intentionally leave the second empty. The matrix is **265 documented / 80 reachable gaps / 17 literal-opcode gaps**. Weighted coverage remains **78%**.

The stat-migration block closes active opcode `0xEFAC38C4`: ten signed 32-bit fields pass through parser `0x013F22B0` into the nine target slots and remaining-points state of `WS_StatMigration`, followed by refresh and activation. First and subsequent Stardust responses are correlated to base HAM and `MigrateStatsSession`. The matrix is **266 documented / 79 reachable gaps / 16 literal-opcode gaps**. Weighted coverage remains **78%**.

The server-command block closes active opcode `0xB1CFCE1C`: one ASCII command passes through parser `0x013F51F0` and listener `0x00F38C20` into the internal game console/UI-action interpreter. Current producers use `/conversationstop` and `/ui action questJournal`; the replacement must never route this to an OS shell. The matrix is **267 documented / 78 reachable gaps / 15 literal-opcode gaps**. Weighted coverage remains **78%**.

The tutorial-HUD block closes active opcode `0xCA375124`: ASCII role, U8 enabled and F32 blink count pass through parser `0x013F29E0`; the UI listener selects a retained HUD role (or `all`), configures blinking and shows/hides it. The matrix is **268 documented / 77 reachable gaps / 14 literal-opcode gaps**. Weighted coverage remains **78%**.
The creature view-6 delta block closes active server builder `CreatureObjectDeltaMessage6` against generic opcode `0x12862153`, tag `CREO`, view 6 and the executable's complete eighteen-field inherited registry. Scalar indices, complex invitation pair, HAM/max-HAM/wearable containers, alternate appearance, callback coalescing and the containment-driven wearable boundary are documented. The matrix is **269 documented / 76 reachable gaps / 14 literal-opcode gaps**. Weighted coverage remains **78%**.
The creature view-4 delta block closes `CreatureObjectDeltaMessage4` against generic opcode `0x12862153`, tag `CREO`, view 4 and the executable's fourteen-field registry. All movement scalars, encumbrance vector, skill-mod map, listen target, space-mission set-map and producer-side 0.1 compatibility substitution are documented. The matrix is **270 documented / 75 reachable gaps / 14 literal-opcode gaps**. Weighted coverage remains **78%**.
The tangible view-3 block closes both `TangibleObjectMessage3` and `TangibleObjectDeltaMessage3`: all eleven baseline positions, nine actively produced delta indices, the generic dispatch/commit path and appearance callbacks are correlated. A possible index-5 width mismatch is deliberately left as a named wire-risk: the candidate client component codec reads eight-byte values while current server call sites expose integer IDs. After matrix refresh the expected result is **272 documented / 73 reachable gaps / 14 literal-opcode gaps**. Weighted coverage remains **78%**. See `ANALYSIS_CHECKPOINT_2026-09-15_TANGIBLE_VIEW3_REPLICATION.md`.
Correction completed 2026-09-21: view-3 index 5 is client RTTI `AutoDeltaSet<int>` and its codecs use four-byte keys, matching server `AutoDeltaSet<int>`. The eight-byte codec belongs to `AutoDeltaVector<CachedNetworkId>` registered into another view. The earlier nonempty visible-component defect was a field-association error and is retired.
The tangible view-6 block closes `TangibleObjectDeltaMessage6` and makes the inherited baseline explicit: U32 common constant followed by an ordered U64 defender-ID vector. Add/remove/set/clear commands, update counter, primary-defender index-zero behavior and combat/PvP side effects are documented. The expected refreshed matrix is **273 documented / 72 reachable gaps / 14 literal-opcode gaps**. Weighted coverage remains **78%**. See `ANALYSIS_CHECKPOINT_2026-09-15_TANGIBLE_VIEW6_DEFENDERS.md`.
The parent-relative movement block closes `DataTransformWithParent` and its already-catalogued callback. Inner `0xF1` is directionally asymmetric: C→S consumes the outer scalar as timestamp before nested counter/parent/full transform/speed, while S→C consumes the scalar in the outer parser and begins nested decoding at the counter. Server validation, bounce-back, parent/cell rules, full correction and compact observer rebroadcast are mapped. The expected matrix is **274 documented / 71 reachable gaps / 14 literal-opcode gaps**. Weighted coverage remains **78%**. See `ANALYSIS_CHECKPOINT_2026-09-15_PARENT_RELATIVE_MOVEMENT.md`.
The character-selection block closes `SelectCharacterCallback` from the executable's exact 14-byte request through server ownership/admission, reconnect eviction, terrain/cell/ship restoration, ordered scene bootstrap and the bidirectional `CmdSceneReady` completion boundary. It distinguishes the original request from the server helper's ignored trailing `SWGEmu` word and specifies where a headless networking proof should stop. The expected matrix is **275 documented / 70 reachable gaps / 14 literal-opcode gaps**. Weighted coverage remains **78%**. See `ANALYSIS_CHECKPOINT_2026-09-15_SELECT_CHARACTER_SESSION.md`.

The SHIP view-3 block closes `ShipObjectMessage3` and its public generic-delta
updates. All 22 fields, the eleven ship-specific client registrations, exact
packed-map replacement framing/value widths, public `ShipDeltaVector` delivery and the
component-to-attachment refresh/event boundary are documented. SHIP inherits
the already-proven TANO visible-component width defect. After publication of
the staged character-selection and SHIP reports, the expected matrix is **276
documented / 69 reachable gaps / 14 literal-opcode gaps**; weighted coverage
remains **78%**. See
`ANALYSIS_CHECKPOINT_2026-09-15_SHIP_VIEW3_BASELINE.md`.

The SHIP view-1 block closes the private 38-field engineering schema: exact
client registrations, fifteen packed-map types and replacement behavior,
twenty scalar engineering values, droid U64 state, owner/member delivery and
active component/ammunition/droid producers. Component creator is a confirmed
but dormant field in current Stardust. After publication of all three staged
reports, the expected matrix is **277 documented / 68 reachable gaps / 14
literal-opcode gaps**; weighted coverage remains **78%**. See
`ANALYSIS_CHECKPOINT_2026-09-15_SHIP_VIEW1_OWNER_STATE.md`.

The SHIP view-4 block closes all five private live-resource fields and corrects
the speed label: index 1 is a template-derived chassis multiplier used in
maximum-speed calculation, while instantaneous velocity is transform state.
Current mass/capacitor/booster producers and packed refire-map replacement are
mapped. After publication of four staged reports, the expected matrix is **278
documented / 67 reachable gaps / 14 literal-opcode gaps**; weighted coverage
remains **78%**. See
`ANALYSIS_CHECKPOINT_2026-09-15_SHIP_VIEW4_LIVE_RESOURCES.md`.

The SHIP view-6 block closes the final primary ship baseline: all 23 public
fields, targetability snapshot, incremental component-CRC operations,
effective flight values, targeting callbacks, attachment refresh ownership,
shield producers and identity-field asymmetries are documented. All SHIP
views are now field-closed. After publication of five staged reports, the
expected matrix is **279 documented / 66 reachable gaps / 14 literal-opcode
gaps**; weighted coverage remains **78%**. See
`ANALYSIS_CHECKPOINT_2026-09-15_SHIP_VIEW6_PUBLIC_FLIGHT.md`.

## Ship transform control closure (2026-09-15)

- Closed bidirectional `0x76026FB9` through client serializer/parser, local and
  remote controller paths, central sender, registered Stardust callback,
  validation and recipient broadcast.
- Confirmed the original sender emits U16 ship ID zero and the server ignores
  that field for authority, resolving the controlled ship from session state.
- Bounded the dormant `0x763648D0` collision correction format and client route.
- Residual: exact local send cadence and all smoothing coefficients remain a
  later fidelity question, not a protocol blocker.
- Expected packet matrix: **280 documented / 65 reachable gaps / 14
  literal-opcode gaps**. Weighted whole-client coverage remains **78%**.

## Ship damage/destruction closure (2026-09-15)

- Closed `OnShipHit` subtype `0x432`, component destruction `0x3871D784`, and
  whole-ship destruction `0x5C680884` through active producers and executable
  handlers.
- Confirmed original hit CEF/SND table dependencies and the distinct remote
  explosion versus local `SpaceDeath` presentation routes.
- Correlated the initial explosion with delayed authoritative station
  relocation, baseline refresh, passenger transfer and ship storage.
- Expected packet matrix: **283 documented / 62 reachable gaps / 14
  literal-opcode gaps**. Weighted coverage remains **78%**.

## Planetary map-location closure (2026-09-15)

- Closed `0x1A7AB839` and `0x9F80464C` through codecs, recipient filtering and
  client map-cache installation.
- Confirmed the three-array/three-token schema and Stardust's full list-0
  snapshot with dormant empty lists/tokens.
- Mapped ID/name/x-y/category/subcategory/icon records and original map assets.
- Expected packet matrix: **290 documented / 55 reachable gaps / 11
  literal-opcode gaps**. Weighted coverage remains **78%**.

## Nebula lightning closure (2026-09-15)

- Closed `CreateNebulaLightningMessage` (`0x65F27987`) through exact wire
  record, scene parser, synchronized queue, presentation lifecycle and original
  per-zone nebula-table dependency.
- Separated visual scheduling from authoritative collision/damage.
- Expected packet matrix: **284 documented / 61 reachable gaps / 14
  literal-opcode gaps**. Weighted coverage remains **78%**.

## Ordinary `DataTransform` closure (2026-09-15)

- Closed ObjectController `0x71` across its asymmetric outer scalar, nested
  codec, server validation/application and executable controller handlers.
- Confirmed immediate local correction versus stale-filtered remote target
  installation, separate from compact observer and parent-relative motion.
- Expected packet matrix: **285 documented / 60 reachable gaps / 14
  literal-opcode gaps**. Weighted coverage remains **78%**.

## Client MFD/group-status closure (2026-09-15)

- Closed `0x2D2D6EE1` through exact single-record layout, generated executable
  codec, ID-based subscriber routing and `SwgCuiStatusGround` presentation.
- Confirmed same-zone distance/position versus cross-zone localized-planet
  behavior and separated this UI record from transforms and group membership.
- Recorded the current server producer's surprising ground/ONFIRE gate without
  treating it as intended client policy.
- Expected packet matrix: **286 documented / 59 reachable gaps / 13
  literal-opcode gaps**. Weighted coverage remains **78%**.

## Player-name existence lookup closure (2026-09-15)

- Closed request `0xBB8CAD45` and response `0xF4C498FD` through executable
  codecs, reliable send, server `existsName` query and harassment-report
  transceiver publication.
- Distinguished this lookup from character-creation and rename validation.
- Confirmed Stardust's response declares one UTF-16 code unit but supplies no
  character, while the client expects Boolean plus echoed Unicode name.
- Expected packet matrix: **288 documented / 57 reachable gaps / 12
  literal-opcode gaps**. Weighted coverage remains **78%**.

## Customer-service category closure (2026-09-15)

- Closed C→Z `0xF898E25F` and Z→C `0x61148FD4` through exact generated
  fields, recursive category grammar, server callback, manager cache replacement
  and UI-listener publication.
- Confirmed current Stardust bypasses category loading and sends a valid empty
  tree; preserved the dormant populated grammar separately.
- Expected packet matrix: **292 documented / 53 reachable gaps / 10
  literal-opcode gaps**. Weighted coverage remains **78%**.

## Survey resource-list closure (2026-09-15)

- Closed Z→C `0x8A64B1D5` through active server filtering/serialization,
  exact generated executable record grammar, listener dispatch and full survey
  UI list replacement.
- Expected packet matrix: **293 documented / 52 reachable gaps / 9
  literal-opcode gaps**. Weighted coverage remains **78%**.

## Survey density-grid closure (2026-09-15)

- Deepened already-source-documented `0x877F79AC` with the executable's exact
  four-F32 point codec, listener dispatch, plot scaling, thresholded sample
  creation and empty-result behavior.
- Packet-matrix counts remain **293 documented / 52 reachable gaps / 9
  literal-opcode gaps**; weighted coverage remains **78%**.

## Vendor-ownership closure (2026-09-15)

- Closed C→Z `0x21B55A3B` and Z→C `0xCE04173E` through exact codecs,
  Stardust ownership/error policy, client auction dispatch and event publication.
- Expected packet matrix: **295 documented / 50 reachable gaps / 8
  literal-opcode gaps**. Weighted coverage remains **78%**.

## Packet-ledger count reconciliation (2026-09-15)

- Direct CSV recount supersedes prior projected counts: **284 documented / 61
  reachable gaps**, including **7 literal-opcode gaps** and 54 callback/schema
  rows without a declared literal opcode.
- Prior `expected` totals overcounted transitions where a paired callback was
  already documented or represented differently. Evidence findings remain
  valid; only the rolling packet-row arithmetic was affected.
- Weighted architectural coverage remains **78%**.

## DSEF scalar and spawned-object semantics (2026-09-21)

- Closed the top DSEF scalar as an initial split-delay seed consumed through clamped geometric countdowns.
- Closed the break/final effect floats as nominal reference lengths used to derive uniform scale from split extent or terminal bounds.
- Closed SDAS floats as spatial-placement coupling factors rather than playback gain.
- Closed all SDOE strings as object template, appearance fallback and parent hardpoint, including prepared handles and fallback construction.
- This removes the leading Class C item without changing the conservative **82%** whole-client estimate.

## Contrail override semantics (2026-09-21)

- Closed `CONT` string A as an overrideable contrail name/key and string D as the name/key overridden by the child record.
- Confirmed child-before-parent recursion, D insertion before A lookup, and suppression without implicit insertion of A.
- Corrected the earlier provisional description of D as a hardpoint/component remap; B and C alone are the start/end hardpoints.

## Static-collidable flora determinism (2026-09-21)

- Confirmed a fresh stack-local shuffled Park–Miller RNG per 16-metre cell, seeded with the negative linearized cell key.
- Closed the exact conditional sample order: X jitter, Z jitter, yaw, then optional scale.
- Corrected the later provisional global-state claim and separately corrected dynamic-node `x + z` as transform yaw rather than an RNG seed.
- Strict original placement parity is now directly implementable without reproducing unrelated global RNG history.

## Tolerated CDF extensions (2026-09-21)

- Confirmed PALV and OBJT are unknown children to this executable's complete WEAR loader and are skipped rather than retained/applied.
- Confirmed VLEF is absent from the top-level CDF dispatcher and from executable tag references.
- Reclassified their corpus grammars as optional disabled extensions, removing their residual fields from the Stardust.exe parity blocker list.

## File-streamer completion ownership (2026-09-21)

- Closed the sole normal request producer as enqueue-then-wait with a stack-local result pointer.
- Confirmed no request handle, callback publication, or per-request cancellation path exists.
- Bounded legacy lifetime risk to borrowed file/buffer ownership during the blocking call and orderly terminal-request/join shutdown.
- Kept true cancellation and scene-generation validation as intentional modern redesign requirements.

## Asynchronous-loader lifecycle (2026-09-21)

- Closed cancellation as exact callback/context nulling across pending and completed queues, without unsafe request removal.
- Confirmed worker I/O may finish after cancellation, but main-thread callback publication is suppressed and cleanup is unconditional.
- Closed sentinel shutdown ordering: stop request, drain both queues, join, then free loader registries, result vectors and request pool.

## Procedural-terrain generation lifecycle (2026-09-21)

- Closed dirty-envelope ownership, priority-work construction, worker-side chunk creation and main-thread publication/retirement.
- Confirmed that the client serializes one active generation cycle rather than tagging and rejecting stale results.
- Confirmed observer updates and new dirty cycles are gated while generation is active; movement is reconsidered after completion.
- Closed destruction ordering as stop under lock, signal, unlock, join, then release chunks, queues and terrain managers.

## STAG coordinate-generation enum (2026-09-21)

- Classified the final STAG byte across all 951 effective records as the coordinate-generation/profile selector.
- Closed effective values 0, 3, 4 and 5 as ordinary, reflection/environment, scroll-bank 1 and scroll-bank 2 behavior respectively.
- Preserved absent values 1/2 as unknown legacy enum values; no configured original asset requires them.

## Current server schema-risk audit (2026-09-21)

- Revalidated every known client/server contradiction against the current source tree.
- Classified quest counter/timer and name verification as active high-priority compatibility risks.
- Classified create-ticket, harvester delta and loop-times discrepancies as dormant hazards.
- Established profile-keyed fixed codec selection; body-length guessing is explicitly rejected.

## FreeChase profile semantics (2026-09-21)

- Recovered the winning original `camera/freechasecamera.iff` values.
- Closed `FRST`, `DEFH`, `PSHM` and `PSTM` as first-person threshold, default target height, posture/state override and posture-only target-height data.
- Confirmed pair-before-posture-before-default precedence, appearance-relative scaling and smoothed application.

## Space camera ownership (2026-09-21)

- Closed CPIT/0000 grammar, zoom scaling, first-person threshold, first/third-person offsets and optional hyperspace offset.
- Confirmed cockpit-frame appearance creation and attachment under `ShipCockpitFrame`.
- Closed ship station 4..11 to turret 0..7 resolution, turret mount transform, zoom smoothing and obstruction shortening.
- Confirmed cockpit, turret and FreeChase as separate controllers sharing a backend-neutral camera base.

## CPIT corpus validation (2026-09-21)

- Validated all 36 effective cockpit profiles with zero parse errors and zero unsorted zoom arrays.
- Confirmed 10 live hyperspace-offset records.
- Proved missing cockpit-frame geometry is a normal supported state: 11 empty paths, 18 nonempty absent/tombstoned references and only 7 live frames.

## Effective input command/action inventory (2026-09-21)

- Enumerated all 436 effective `CMDS/0006` command definitions and all 1,744
  fixed action slots; 525 slots are populated.
- Produced an exhaustive mapping of 120 numeric action IDs to command, category,
  transition slot, raw U32 parameter and payload.
- Established that numeric action IDs select interpreters rather than uniquely
  identifying commands: action 324 is shared by 311 payload-driven commands.
- Preserved unknown historical slot/flag labels without leaving any configured
  action record undocumented.

## Effective physical input-binding inventory (2026-09-21)

- Parsed all 14 effective `IMAP/0006` profiles and recorded all 1,939 physical
  bindings across six device families.
- Resolved the physical-to-command-to-action asset chain and identified 24
  stale rows naming eleven commands absent from the winning dictionaries.
- Preserved two duplicate physical tuples and 95 valid unbound command
  definitions rather than silently normalizing the source data.
- Corrected the command mask to device-family eligibility and separated it from
  MAP INFO shift state; executable constants close all four family bits.
- Closed MAP INFO Boolean as state-zero inheritance and dense-table application
  as base-first, selected-second, serialized last-wins precedence.
- Closed the four command-action slots as press, release, held/repeat and
  forced reset/cancel through their direct runtime dispatch offsets.

## Secure-trade UI workflow (2026-09-22)

Secure player trade is now closed as a representative end-to-end gameplay UI
workflow. `SwgCuiTrade`, both `Panel` instances and
`SwgCuiContainerProviderTrade` are traced through item drops, money edits,
accept/unaccept, two-party verification, completion, abort, peer lifetime/range
checks and teardown. All associated named-message bodies are correlated with
the current Stardust handlers and `TradeSession` authority. See
`ANALYSIS_CHECKPOINT_2026-09-22_SECURE_TRADE_UI_WORKFLOW.md`. This deepens the
UI/gameplay denominator but does not alone move the conservative rounded **83%**
whole-client estimate.

## Shader forward-version reachability (2026-09-22)

The earlier shader-version warning is now reachability-bounded. The effective
overlay contains 27 live `IMPL/0009` + `PASS/0010` winners that this executable's
strict dispatchers cannot load. Eight affected shaders have direct references
from 39 live particle/environment assets, including Mustafar terrain. The new
client must decode these versions rather than inherit the legacy rejection;
the added PASS byte remains a visible Class C semantic item. See
`ANALYSIS_CHECKPOINT_2026-09-22_SHADER_FORWARD_VERSION_REACHABILITY.md` and
`unsupported_shader_effective_references.csv`.

## Crafting controller codecs (2026-09-22)

The crafting controller boundary is now byte-complete for draft-list delivery,
ingredient fill/empty, experimentation, customization, generic result messages
and window closure. Client factory direction flags, current Stardust parsers,
session states, correlated counters, trade exclusion and transient-object
cleanup are documented. Per-screen mediator behavior and exact generic result
enumerations remain the next crafting depth units. See
`ANALYSIS_CHECKPOINT_2026-09-22_CRAFTING_CONTROLLER_CODECS.md`.

## Crafting response state machine (2026-09-22)

`CuiCraftManager` response ownership is now closed through stale-counter
rejection, slot operations, assembly, experimentation, customization, finish,
replicated-stage checks and typed mediator publication. Controller replies are
proven to coordinate with PLAY/MSCO/TANO deltas rather than carry the complete
state themselves. See
`ANALYSIS_CHECKPOINT_2026-09-22_CRAFTING_RESPONSE_STATE_MACHINE.md`.

## Mission UI and replication boundary (2026-09-22)

The mission-terminal presentation boundary is now separated into active and
legacy paths. Current Stardust populates terminal results through replicated
`MISO` objects and refresh deltas, which drive browser `EnteredMissionBag` and
`LeftMissionBag` subscriptions. Accept `0xF9/0xFA` and abort `0x142` remain live
authoritative workflows. Explicit list message `0x88D9885C` and details
`0xF7/0xF8` survive in the executable but are dormant or unsupported in the
current server. `ResponseRemove` is retained as a bounded historical unknown.
See `ANALYSIS_CHECKPOINT_2026-09-22_MISSION_UI_BOUNDARY.md`.

## Persistent mail manager (2026-09-22)

Persistent mail is now closed as a durable client-state workflow. Incoming
`0x08485E17` field order, header/body merge, U32 identity, unread/read state,
OOB waypoint/prose extraction, typed UI events, send-sequence correlation, and
the 5-second body-request / 0.1-second delete queues are mapped against current
Stardust constructors and callbacks. The optional profile-local mailbox export
is separated from interoperability requirements. See
`ANALYSIS_CHECKPOINT_2026-09-22_PERSISTENT_MAIL_MANAGER.md`.

## Inventory and container manager (2026-09-22)

The inventory UI boundary is now closed across Stardust's validated
`openContainer` command, `0xDCA57409` response, request-token and multi-window
open tracking, optimistic last-view close, typed open/close/split UI events,
containment/capacity helpers, resource splitting and object destruction. The
replacement must layer UI prediction over the authoritative replicated
containment graph and retain IDs rather than raw object pointers. See
`ANALYSIS_CHECKPOINT_2026-09-22_INVENTORY_CONTAINER_MANAGER.md`.

## Matchmaking and character search (2026-09-22)

The active character-search boundary is now closed end to end. The nine-token
`requestCharacterMatch` grammar, current server filters and 32-result limit,
object-controller `0x1E7` codec, 0x50-byte legacy result object, player
dispatcher, and `CharacterMatchRetrieved` UI subscribers are mapped. The two
set-matchmaking commands are confirmed current-server no-ops; their historical
service semantics remain bounded dormant behavior rather than a prerequisite
for interoperability. See
`ANALYSIS_CHECKPOINT_2026-09-22_MATCHMAKING_AND_WHO.md`.

## Combat and damage presentation boundary (2026-09-22)

The ground damage manager is now separated from wire parsing: it subscribes to
typed creature/tangible `DamageTaken` model events and generates preference-
gated transient fly-outs. `CuiCombatManager` independently handles local
incapacity/target reactions, action queues and `CombatSpamReceived`; controller
`0xCC` animation, `0x134` text, replicated health/condition and ship-damage UI
remain distinct streams. See
`ANALYSIS_CHECKPOINT_2026-09-22_COMBAT_DAMAGE_PRESENTATION.md`.

## Historical unknown-list reconciliation (2026-09-22)

The stale early high-priority list in `UNKNOWN_FUNCTIONS.md` is now reconciled
against later evidence. Network dispatch, login/session bootstrap, object
replication, frame order, legacy raster lifecycle, TRE/IFF factories and
thread ownership are closed at the replacement-architecture boundary. The
remaining queue is optional workflow breadth and visual fidelity, not those
superseded fundamentals. See
`ANALYSIS_CHECKPOINT_2026-09-22_DOCUMENTATION_UNKNOWN_RECONCILIATION.md`.

## Object text and nameplates (2026-09-22)

The unvisited `CuiObjectTextManager` is now bounded as a scene-scoped,
presentation-only cache fed by object/player/camera/preferences state. Install,
faction and match-group UI styles, scene invalidation, per-object anchor
smoothing, invalid-reference maintenance and x64-safe replacement ownership
are mapped. Remaining field labels are optional UI fidelity rather than a
network or asset-container blocker. See
`ANALYSIS_CHECKPOINT_2026-09-22_OBJECT_TEXT_NAMEPLATES.md`.

## UI message queue and command-parser bridge (2026-09-22)

The previously unvisited `CuiMessageQueueManager` is now bounded as an internal
UI/input adapter. It publishes typed ASCII-command/Boolean requests, translates
input-map items and macros, diagnoses missing bindings and remains separate
from authoritative gameplay command transport. See
`ANALYSIS_CHECKPOINT_2026-09-22_UI_MESSAGE_QUEUE_COMMAND_BRIDGE.md`.

## Client icon-manager resolution (2026-09-22)

The icon manager is now bounded as a semantic UI action/drag resolver rather
than a texture loader. Object/template, social, mood, `ui action`, generic
command and droid paths are separated, with explicit missing-data diagnostics
and original catalog/asset dependencies. See
`ANALYSIS_CHECKPOINT_2026-09-22_ICON_MANAGER_RESOLUTION.md`.
### Cached avatar persistence and login metadata (2026-09-22)

Closed at the replacement-architecture boundary. `CuiCachedAvatarManager` is a local login/avatar-selection cache, not a live network replication subsystem. Its `PlayerSetup` and `AddedToContainer` listeners set a deferred-save dirty flag. The versioned IFF schema is confirmed as `CADF/0000` with `DATA`, `PLAN`, `WEAR`, and repeated `ITEM` records; cached wearables are recreated through original template names and normal tangible-object containment. A separate textual `characterlist` stores per-cluster character metadata. See `ANALYSIS_CHECKPOINT_2026-09-22_CACHED_AVATAR_MANAGER.md`.
### Offline knowledge base and online service split (2026-09-22)

Closed at the replacement boundary. The offline help hierarchy is loaded from the original `datatables/knowledgebase/filelist.iff` and five-column section tables into seven RTTI-confirmed node types. It is independent of the customer-service search/article protocol. Current Stardust registers the two online commands but their implementations return success without producing responses; the packet constructors are dormant. See `ANALYSIS_CHECKPOINT_2026-09-22_KNOWLEDGE_BASE_MANAGER.md`.
### Instant-message manager state and correlation (2026-09-22)

Closed end to end. Outgoing tells use a monotonic U32 sequence and timeout-managed pending map; acknowledgement codes 0/4/9/16 correlate and retire exact records; incoming tells are ignore-filtered before reply/history/event state, with away auto-replies routed through the same send service. Current Stardust's 255-character limit and privacy ordering are correlated. See `ANALYSIS_CHECKPOINT_2026-09-22_INSTANT_MESSAGE_MANAGER.md`.
### Login manager joined state and avatar selection (2026-09-22)

Closed at the manager boundary. Cluster identity, status/address, character enumeration, permissions, selected-cluster connection, optional UDP latency probes and cached original-asset avatar previews are separated and joined by stable IDs. The fourth permission byte is parsed but ignored in this executable. See `ANALYSIS_CHECKPOINT_2026-09-22_LOGIN_MANAGER_STATE.md`.
### Resource manager and harvester hopper workflow (2026-09-22)

Closed end to end. Controller `0xEA` replaces the selected installation's resource-pool snapshot and publishes `PoolsChanged`; C→Z `0xED` carries harvester/resource U64s, U32 quantity, retrieve/discard U8 and a wrapping U8 token; matching generic response `0xED` rejects stale tokens and publishes `EmptyCompleted`. Server validation and hopper mutation are correlated. See `ANALYSIS_CHECKPOINT_2026-09-22_RESOURCE_MANAGER_HARVESTER.md`.

### System-message manager (2026-09-22)

Closed end to end. `ChatSystemMessage` opcode `0x6D2A6413` carries a U8 display
policy and either literal UTF-16 text or length-delimited
`StringIdChatParameter` prose. The executable decoder, formatter, typed
`Received` fanout, chat/window consumers, preferences, and original STF-asset
dependency are mapped. See
`ANALYSIS_CHECKPOINT_2026-09-22_SYSTEM_MESSAGE_MANAGER.md`.

### Auction manager joined state (2026-09-22)

The individually decoded market packets are now consolidated into one retained
client model: eight query-list channels, pagination, ID-keyed details, action
request context, vendor capability, item taxonomy and typed UI results. Legacy
x86 record sizes are explicitly excluded from wire and x64 model layouts. See
`ANALYSIS_CHECKPOINT_2026-09-22_AUCTION_MANAGER_JOINED_STATE.md`.

### Avatar-creation workflow (2026-09-22)

The full asset-driven creation request, random-name correlation, success/failure
state and current Stardust authority are consolidated. Exact fixed wire widths
and original template/customization dependencies are explicit. A current
server defect was identified where literal error sentences are serialized as
`ui` STF keys. See `ANALYSIS_CHECKPOINT_2026-09-22_AVATAR_CREATION_WORKFLOW.md`.

### Waypoint replication runtime (2026-09-22)

`PLAY:8` waypoint bytes, map callbacks, presentation-object creation/removal,
primary-over-secondary precedence, active marker ownership and UI publication
are closed. Original waypoint template/map/icon data remains mandatory. See
`ANALYSIS_CHECKPOINT_2026-09-22_WAYPOINT_REPLICATION_RUNTIME.md`.

### Common ClientObject view-6 field (2026-09-22)

The inherited U32 is confirmed as a passive `AutoDeltaVariable<unsigned long>`
with null semantic callback ownership. Its historical label remains unknown,
but exact retention and tag/view-specific tail handling are sufficient for
compatibility. See
`ANALYSIS_CHECKPOINT_2026-09-22_COMMON_VIEW6_OPAQUE_FIELD.md`.

### Replication decode failure boundary (2026-09-22)

Checked Archive reads throw on truncation, and the complete inspected path
through field/view decoding, event dispatch, connection dispatch and received
queue has cleanup unwinding but no local recovery. The replacement requirement
is transactional decode/commit and safe session failure. See
`ANALYSIS_CHECKPOINT_2026-09-22_REPLICATION_DECODE_FAILURE_BOUNDARY.md`.

### UI event-expression corpus (2026-09-22)

All 2,720 effective UI event values are preserved with provenance. The observed
language is a small ordered assignment/arithmetic/path evaluator; all 14
`OnRunScript` uses repeat one two-statement layout formula. This replaces a
generic scripting unknown with a bounded original-asset compatibility target.
See `ANALYSIS_CHECKPOINT_2026-09-22_UI_EVENT_EXPRESSION_CORPUS.md`.

### UI style/font/atlas pipeline (2026-09-22)

The effective original UI resource layer is inventoried at 44,674 rows. Exact
glyph metrics/pages, image slices, frames, palette themes and qualified atlas
sources now constrain an original-asset-compatible adapter. See
`ANALYSIS_CHECKPOINT_2026-09-22_UI_STYLE_FONT_ATLAS_PIPELINE.md`.

### Custom UI render widgets (2026-09-22)

The object viewer and object-list viewer are confirmed renderer-backed scene
adapters with retained object/appearance ownership, transformed aggregate
bounds, transient-effect exclusion and declarative camera/light controls.
Paperdoll and ground radar are dedicated registered widget adapters. See
`ANALYSIS_CHECKPOINT_2026-09-22_CUSTOM_UI_RENDER_WIDGETS.md`.

### Ground radar runtime (2026-09-22)

Ground radar is confirmed as a client-derived terrain/object projection driven
by world transforms, procedural terrain, spatial objects and explicit waypoint
locations. Range/mode persistence, tooltip hit testing and separate blip and
waypoint panes are closed. See
`ANALYSIS_CHECKPOINT_2026-09-22_GROUND_RADAR_RUNTIME.md`.

### Paperdoll/equipment runtime (2026-09-22)

Paperdoll drag/drop is closed through tangible resolution, skeletal hardpoint
enumeration, asset-derived slot hit volumes and the authoritative container
transfer path. Original skeleton, wearable and arrangement assets are mandatory.
See `ANALYSIS_CHECKPOINT_2026-09-22_PAPERDOLL_EQUIPMENT_RUNTIME.md`.

### CUI preferences runtime (2026-09-22)

The layered typed preference registry is closed through configuration defaults,
`ClientUserInterface` key registration, change publications, machine-option
persistence, ground/space policy separation and asset-backed palette selection.
See `ANALYSIS_CHECKPOINT_2026-09-22_CUI_PREFERENCES_RUNTIME.md`.

### Machine-options IFF (2026-09-22)

The local preference file is closed as `FORM OPTN/FORM 0002` with typed `BOOL`,
`FLT `, `INT `, `STDS` and `UNIS` chunks, exact value/key/section order, unknown
chunk skipping and ConfigFile fallback. See
`ANALYSIS_CHECKPOINT_2026-09-22_MACHINE_OPTIONS_IFF.md`.

### Character UI settings `.uis` (2026-09-22)

Per-character UI persistence is closed as `UIST/0003`: stored resolution,
repeated mediator-owner records, optional geometry and Unicode/blob/S32 maps.
Four deployed files parse completely and validate the path/grammar. See
`ANALYSIS_CHECKPOINT_2026-09-22_CHARACTER_UI_SETTINGS_UIS.md`.

### Incapacitation UI workflow (2026-09-22)

The incap overlay is closed as a presentation projection of current-player
posture and replicated `CREO:3[7]`, with local countdown interpolation and no
client recovery authority. Server scheduling confirms the timer semantics. See
`ANALYSIS_CHECKPOINT_2026-09-22_INCAPACITATION_UI.md`.

### Death and cloning workflow (2026-09-22)

The current Stardust death-release path is closed as a server-authored generic
SUI list rather than a dedicated client cloning protocol. The server owns the
eligible-facility set and row-to-facility-ID mapping; the client returns only a
selected row through `SuiEventNotification`. Cancellation leaves the character
dead and `/activateClone` can reopen the request. See
`ANALYSIS_CHECKPOINT_2026-09-22_DEATH_CLONING_WORKFLOW.md`.

### Vendor UI runtime (2026-09-22)

The vendor window is closed as a thin buyer/seller/stockroom specialization of
the common auction model, and its set-price dialog as a typed auction-manager
event consumer. It introduces no separate vendor transport. See
`ANALYSIS_CHECKPOINT_2026-09-22_VENDOR_UI_RUNTIME.md`.

### Space navigation UI (2026-09-22)

The hyperspace map is closed through original destination tables/localization,
exact queued-command argument and server validation. The space-zone map is
bounded as a temporary interactive 3D scene assembled from original zonemap
assets plus live waypoint and mission-critical state. See
`ANALYSIS_CHECKPOINT_2026-09-22_SPACE_NAVIGATION_UI.md`.

### Survey UI runtime (2026-09-22)

The survey mediator is closed from resource-list population through exact
`requestSurvey`/`requestCoreSample` commands, server-authoritative sampling and
the client density/range/player-position visualization. See
`ANALYSIS_CHECKPOINT_2026-09-22_SURVEY_UI_RUNTIME.md`.

### Resource/factory-crate splitter (2026-09-22)

The inventory split dialog is closed through typed inventory event, object-kind
resolution, quantity bounds and exact authoritative server commands. See
`ANALYSIS_CHECKPOINT_2026-09-22_RESOURCE_SPLITTER.md`.

### Space radar runtime (2026-09-22)

The planar and two spherical radar implementations are bounded through base
lifecycle/range handling, custom-pane rendering, nearby-object enumeration,
relationship/type classification, waypoint/mission overlays and ship-status
instrumentation. Radar is a local projection of existing authoritative state
and adds no protocol. See
`ANALYSIS_CHECKPOINT_2026-09-22_SPACE_RADAR_RUNTIME.md`.

### HUD, target and status architecture (2026-09-22)

The HUD family is bounded as a ground/space shell over reusable target-status
mediators and camera-projected overlays. Space station-table controls,
damage/missile subscriptions, target-lead math, ship condition/component fields
and interaction capabilities are mapped without introducing a new protocol.
See `ANALYSIS_CHECKPOINT_2026-09-22_HUD_TARGET_STATUS_ARCHITECTURE.md`.

### Ship selection and component UI (2026-09-22)

Parked-ship selection, ship view, group/direct launch, component inventory/slot
management and detail presentation are now bounded. Exact install, uninstall
and repair command names are correlated with server parsing and authority; the
server's repair command is presently a no-op after eligibility checks. See
`ANALYSIS_CHECKPOINT_2026-09-22_SHIP_SELECTION_COMPONENT_UI.md`.

### Space HUD widgets and player formation (2026-09-22)

Formation assignment, missile/countermeasure counts, component fly-outs and the
ship reticle are bounded. Formation submits exact `setFormation` followed by
per-member `setFormationSlot`; no current Stardust handlers were found. The
other widgets are local projections of replicated ship/component/controller
state. See `ANALYSIS_CHECKPOINT_2026-09-22_SPACE_HUD_WIDGETS_FORMATION.md`.

### Toolbar action and persistence model (2026-09-22)

The toolbar is closed as a persistent pane/slot tagged-action model over
commands, macros, inventory objects and separators. Execution delegates to
existing subsystems; `CMD_uiToolbarSlot%02d` bindings supply display keys and
toolbar-specific settings populate the decoded `.uis` envelope. See
`ANALYSIS_CHECKPOINT_2026-09-22_TOOLBAR_ACTION_MODEL.md`.

### Quest counter/timer compatibility decision (2026-09-22)

The earlier `0x440/0x441/0x444` discrepancy is converted into an actionable
compatibility specification: original-client counter is `0x440`, timer is
`0x441`; current server field layouts already match but both IDs are wrong.
Both helpers are live-reachable from the quest journal Lua manager. See
`ANALYSIS_CHECKPOINT_2026-09-22_QUEST_COUNTER_TIMER_COMPATIBILITY_DECISION.md`.

### Combat queue UI (2026-09-22)

The combat queue is closed as a sequence-keyed projection of the shared client
command queue. Clear/cancel sends the established cancellation payload and
optimistically removes each sequence locally; add/remove/result events reconcile
the view. See `ANALYSIS_CHECKPOINT_2026-09-22_COMBAT_QUEUE_UI.md`.

### Network status and MFD widgets (2026-09-22)

Network diagnostics are bounded as a read-only ping/loss/bandwidth/FPS view;
MFD status is a reusable watched-object status bar with current/max/recent
layers, direction indication, margins, ticks and orientation. Neither adds a
protocol. See `ANALYSIS_CHECKPOINT_2026-09-22_NETWORK_STATUS_MFD_WIDGETS.md`.

### Ship weapon groups (2026-09-22)

Weapon-group assignment is closed as a client-local per-ship configuration,
persisted in `SWGI` records containing group index and repeated slot-plus-two-
flag members. UI changes publish local manager events; no new wire message is
involved. See `ANALYSIS_CHECKPOINT_2026-09-22_SHIP_WEAPON_GROUPS.md`.

### Biography request and edit workflow (2026-09-22)

Biography retrieval and editing are closed end to end. The client queues the
exact `requestBiography` and `setBiography` commands; current Stardust returns
controller subtype `0x1DB` as `U64 subject ID + Unicode biography`, which the
player controller publishes as `BiographyRetrieved` for the editor mediator.
See `ANALYSIS_CHECKPOINT_2026-09-22_BIOGRAPHY_WORKFLOW.md`.

### Droid command programming (2026-09-22)

The droid-programming mediator is closed through list/capacity state,
confirmation, exact controller subtype `0x435` request schema, server-side
ability/chip/capacity validation, module creation/destruction and ordinary
container-replication reconciliation. See
`ANALYSIS_CHECKPOINT_2026-09-22_DROID_COMMAND_PROGRAMMING.md`.

### Skills and profession UI family (2026-09-22)

The concrete skills shell, profession list/detail, five graph pages and my-
stats view are bounded over the existing skill catalog and replicated skill,
XP, modifier and command state. Skill surrender remains the sole mutation and
uses the confirmed authoritative command flow. See
`ANALYSIS_CHECKPOINT_2026-09-22_SKILLS_UI_FAMILY.md`.

### Data storage UI (2026-09-22)

The four-pane waypoint/data/POI/draft browser is bounded over replicated
containment and existing waypoint/progression state, including capacity,
filters, group-waypoint callback and local view persistence. It introduces no
new protocol. See `ANALYSIS_CHECKPOINT_2026-09-22_DATA_STORAGE_UI.md`.

### Command and macro browser (2026-09-22)

The command browser is closed as a projection of replicated player commands
plus client-local macro CRUD, producing typed drag/action descriptors consumed
by the toolbar and command interpreter. It adds no independent protocol. See
`ANALYSIS_CHECKPOINT_2026-09-22_COMMAND_BROWSER.md`.

### Experience monitor runtime (2026-09-22)

The selectable XP monitor is bounded as a read-only projection of replicated
experience through effective skill/XP-cap assets, with explicit player setup,
skill, experience and monitor-selection event inputs. See
`ANALYSIS_CHECKPOINT_2026-09-22_EXPERIENCE_MONITOR.md`.

### Ground group UI runtime (2026-09-22)

The ground-party mediator is bounded over `GRUP:6`, nearby creature state and
far-member MFD updates, including member lifecycle, target highlighting,
loot-rule/master changes and exact dismiss/disband/leave command handoffs. See
`ANALYSIS_CHECKPOINT_2026-09-22_GROUND_GROUP_UI.md`.

### Session shell and loading UI (2026-09-22)

Ground/space loading, scene selection, news, disconnect and splash mediators
are bounded as presentation shells over session state. Ground loading exposes
five distinct readiness gates—server objects, file caching, world, terrain and
player readiness—without introducing another protocol. See
`ANALYSIS_CHECKPOINT_2026-09-22_SESSION_SHELL_UI.md`.

### Locations debug mediator (2026-09-22)

`SwgCuiLocations` is classified as a developer planet/location selector with
`planetwarp` command handoff and a local `terrain/%s.trn` validation branch,
not a normal player map or required protocol subsystem. See
`ANALYSIS_CHECKPOINT_2026-09-22_LOCATIONS_DEBUG_MEDIATOR.md`.

### Harassment report UI (2026-09-22)

The verify-name response is closed through the form consumer: Boolean failure
or canonical UTF-16 name retention gates submission into the existing customer-
service path. The current server's missing UTF-16 code unit remains an explicit
compatibility defect. See
`ANALYSIS_CHECKPOINT_2026-09-22_HARASSMENT_REPORT_UI.md`.

### In-game menu (2026-09-22)

The menu is classified as a thin router for resume, orderly disconnect,
controlled exit, credits and a hidden developer locations mediator. It adds no
protocol or gameplay state. See `ANALYSIS_CHECKPOINT_2026-09-22_GAME_MENU.md`.

### Stat migration workflow (2026-09-22)

The previously mapped ten-S32 response is now extended through the complete UI
transaction: activation/data request, nine canonical HAM rows, local target
editing, ten-integer command submission, start/stop markers, server-side species
bounds/total validation and tutorial-or-staff application. The trailing submitted
points-left integer is ignored by Stardust's nine-token loop but remains part of
the original client's compatibility envelope. See
`ANALYSIS_CHECKPOINT_2026-09-22_STAT_MIGRATION_WORKFLOW.md`.

### Image designer workflow (2026-09-22)

The paired designer/recipient mediators are now bounded over the already mapped
`0x23A` start and full-state `0x238`/`0x239` object-controller messages. Preview,
price/offer, dual acceptance, timeout, salon gating, payment, hair/body/color,
holo-emote, stat migration, XP and replication reconciliation are separated into
their correct client-versus-server ownership. A duplicated designer-salon check in
the server commit branch is recorded as code debt, not invented client behavior.
See `ANALYSIS_CHECKPOINT_2026-09-22_IMAGE_DESIGNER_WORKFLOW.md`.

### Character sheet runtime (2026-09-22)

The full character sheet is bounded as a read-only projection of replicated
player/creature state plus the already decoded faction, guild, money, badge and
sheet-detail response family. Identity, attributes, consumption, PvP/faction,
locations, lots, guild and badge fields are assigned to their correct source;
stat migration is a typed handoff rather than an internal mutation. See
`ANALYSIS_CHECKPOINT_2026-09-22_CHARACTER_SHEET_RUNTIME.md`.

### Permission-list workflow (2026-09-22)

The permission-list mediator is extended from snapshot decoding through visible
members, add suggestions, exact add/remove command construction and authoritative
structure/POB-ship policy. Player and `guild:` principal resolution, list capacity,
owner/self/conflict rules, ban revocation and post-change cell-permission updates
are correlated with the server implementation. See
`ANALYSIS_CHECKPOINT_2026-09-22_PERMISSION_LIST_WORKFLOW.md`.

### Mission-details response closure (2026-09-22)

The dormant controller `0xF8` response is closed as a sequence-only completion
signal rather than an unknown details blob. Its named fixed pool uses `0x0C`-byte
objects and the publisher reads only U8 `+0x08`; matching completion refreshes
from the already replicated selected mission object. See
`ANALYSIS_CHECKPOINT_2026-09-22_MISSION_DETAILS_RESPONSE_CLOSURE.md`.

### CuiConsent boundary (2026-09-22)

The generic confirmation mediator is separated from network protocols: it owns
yes/no presentation and a caller callback, while each caller owns serialization.
The similarly named Stardust command is a dormant stub and persistent consent
lists remain ordinary commands. See
`ANALYSIS_CHECKPOINT_2026-09-22_CUI_CONSENT_BOUNDARY.md`.

### Examine and inventory-info runtime (2026-09-22)

The examine mediator is closed as a lifecycle shell over the reusable
`SwgCuiInventoryInfo` view. Target retention, 3-D preview, text composition,
ordinary `AttributeListMessage` cache use and the specialized creature/guild path
are separated; there is no unique examine packet. See
`ANALYSIS_CHECKPOINT_2026-09-22_EXAMINE_INVENTORY_INFO.md`.

### Community UI composition (2026-09-22)

The community shell is bounded as six child view-models over existing
friend/ignore chat state, character search, biography/who state, match history
and replicated matchmaking preferences. It loads the original matchmaking IFF
and introduces no additional packet family. See
`ANALYSIS_CHECKPOINT_2026-09-22_COMMUNITY_UI_COMPOSITION.md`.

### Planet-map presentation runtime (2026-09-22)

The planet-map UI is closed across original map-page selection, terrain extent,
world-to-page transforms, location-tree and marker generation, waypoint events,
player position/heading, zoom/pan and saved filters. It remains a consumer of the
already mapped manager snapshot, not another protocol parser. See
`ANALYSIS_CHECKPOINT_2026-09-22_PLANET_MAP_PRESENTATION.md`.

### Customer-service ticket UI (2026-09-22)

The ticket shell and new-ticket child are closed from manager snapshots through
category/subcategory IDs, non-empty comment validation, ticket-limit gating,
submission cooldown and `CreateTicketMessage`. Current server capability/disable
behavior remains authoritative. See
`ANALYSIS_CHECKPOINT_2026-09-22_CUSTOMER_SERVICE_TICKET_UI.md`.

### Bug-submission UI (2026-09-22)

The legacy bug composer is separated into local report composition, obsolete
beta-era auxiliary routing and an optional bridge to the normal ticket message.
Its CreateTicket tail is narrowed to two empty Unicode fields, auxiliary ASCII
metadata and a true special-route flag at this call site. See
`ANALYSIS_CHECKPOINT_2026-09-22_BUG_SUBMISSION_UI.md`.

### Vehicle hover-dynamics tuning prototype (2026-09-22)

`CuiVehicleProto` is classified as a local developer panel that reads/writes
twelve named `VehicleHoverDynamics` parameters. It carries no packet or server
authority and need not be mistaken for production vehicle UI. See
`ANALYSIS_CHECKPOINT_2026-09-22_VEHICLE_HOVER_TUNING_PROTO.md`.

### CuiTransition overlay (2026-09-22)

The paired-door transition is bounded as a local close/wait/open overlay with
token-matched readiness, asset-authored geometry/sounds and deterministic
teardown. It is explicitly not scene/network authority. See
`ANALYSIS_CHECKPOINT_2026-09-22_CUI_TRANSITION_OVERLAY.md`.

### Ship diagnostic information widgets (2026-09-22)

The self/target ship-info panels are classified and their shared formatter is
bounded across hull, armor, shield, capacitor, ammo, refire, acquisition/threat
and targeted-component state. They are protocol-free diagnostics over existing
ship state. See
`ANALYSIS_CHECKPOINT_2026-09-22_SHIP_DIAGNOSTIC_INFO_WIDGETS.md`.

### Input-box callback boundary (2026-09-22)

The reusable yes/no text-entry mediator is closed as a one-shot local callback
service. Its adjacent waypoint specialization validates a client waypoint and
hands accepted text to the existing waypoint update path; the generic widget
adds no packet or authoritative state. See
`ANALYSIS_CHECKPOINT_2026-09-22_INPUT_BOX_CALLBACK_BOUNDARY.md`.

### CuiNotepad persistence (2026-09-22)

The local per-profile `notes.txt` editor is closed through activation load,
deactivation save, narrow/wide marker handling and explicit size caps. It has no
network dependency. See
`ANALYSIS_CHECKPOINT_2026-09-22_CUI_NOTEPAD_PERSISTENCE.md`.

### Keypad and money widgets (2026-09-22)

`CuiKeypad` is closed as a local digit/action callback and `CuiMoney` as a
reactive view over replicated cash/bank fields. Neither introduces a protocol
message. See `ANALYSIS_CHECKPOINT_2026-09-22_KEYPAD_AND_MONEY_WIDGETS.md`.

### Generic transfer mediator (2026-09-22)

`CuiTransfer` is closed as a two-unit conserved-total conversion dialog with
integer rounding/clamping, slider/text synchronization, revert and local
completion. It adds no protocol. See
`ANALYSIS_CHECKPOINT_2026-09-22_GENERIC_TRANSFER_MEDIATOR.md`.

### Data-driven SUI runtime (2026-09-22)

`CuiDataDrivenPage` is closed as the executable runtime for server-authored SUI:
original-page and `MediatorData` resolution, command types 1–7, initial/update
semantics, event adapters, subscribed-property capture and reliable event return
are mapped against current Stardust source. Server enum type 8 is dormant and
unsupported by this executable. See
`ANALYSIS_CHECKPOINT_2026-09-22_DATA_DRIVEN_SUI_RUNTIME.md`.

### SUI page-manager lifecycle (2026-09-22)

The create/update/force-close and scene-change routes are closed through the
page-ID registry. Create resolves an original `UIPage`, constructs and activates
the mediator; update targets an existing page; force-close removes without a
return event; user close sends a one-shot cancel event; scene changes drain all
pages. See `ANALYSIS_CHECKPOINT_2026-09-22_SUI_PAGE_MANAGER_LIFECYCLE.md`.

### Server SUI asset footprint (2026-09-22)

All eleven root paths currently named by native/Lua Stardust SUI producers are
present in the effective `stardust_3_01.tre` UI overlay under the `Script` or
`PDA` hierarchy. Mixed-case server spellings and leading-slash/dotted lookup
forms are recorded as compatibility requirements. See
`ANALYSIS_CHECKPOINT_2026-09-22_SERVER_SUI_ASSET_FOOTPRINT.md`.

### Stardust SUI behavioral surface (2026-09-22)

Current server scripts require events 1/9/10, property extraction, list/data
mutation, sliders, text, layout/color changes, legacy `OnRunScript` execution
and an object-viewer bridge. Update-time attempts to replace subscriptions are
ineffective because the original client skips type-5 commands after creation;
that quirk is now explicit. See
`ANALYSIS_CHECKPOINT_2026-09-22_STARDUST_SUI_BEHAVIORAL_SURFACE.md`.

### PASS/0010 field alignment (2026-09-22)

The 57-byte forward shader pass is aligned as PASS/0009 plus one Boolean at
DATA offset `0x04`; all later fields shift exactly one byte. The new flag is
false in 104/108 records and true only for two `ui_bar_glow` passes and one pass
in each of two lava shaders. Its semantic label remains deliberately unknown.
See `ANALYSIS_CHECKPOINT_2026-09-22_PASS_0010_FIELD_ALIGNMENT.md`.

### IMPL/0009 forward schema (2026-09-22)

All 82 effective forward implementation forms are 0009 and share the complete
SCAP/[OPTN]/7-byte-DATA/counted-PASS grammar of executable-supported 0007.
Forward decoding can reuse that envelope while preserving the version and
generic phase/material tag. See
`ANALYSIS_CHECKPOINT_2026-09-22_IMPL_0009_FORWARD_SCHEMA.md`.

### Quest counter/timer reachability (2026-09-22)

The controller-ID mismatch is real but absent from current shipped content:
timer has no caller, and no live journal map defines the optional counter
operation. `requestQuestTimersAndCounters` is a no-op after validation. The
replacement still needs explicit historical/Stardust codec profiles before
future content enables these overlays. See
`ANALYSIS_CHECKPOINT_2026-09-22_QUEST_COUNTER_TIMER_REACHABILITY.md`.

### Session/process teardown integration (2026-09-22)

Scene reset, session disconnect and process exit are separated into explicit
ownership scopes. Confirmed main-thread publication and worker joins require
generation invalidation, callback cancellation, result drain/discard and join
before world/resource destruction. See
`ANALYSIS_CHECKPOINT_2026-09-22_SESSION_TEARDOWN_INTEGRATION.md`.

### Deployed transport selector (2026-09-22)

Executable option flow and managed-launcher IL prove the ordinary installed
package selects four-byte-length-prefixed TCP with no command-line override.
Current Stardust login and zone source directly instantiate UDP datagram
services, making the missing public deployment edge an explicit external-state
unknown. Replacement transport is therefore profile-selected: direct UDP/SOE
for the developer server and TCP only when its deployment boundary is available.
See `ANALYSIS_CHECKPOINT_2026-09-22_DEPLOYED_TRANSPORT_SELECTOR.md`.

### Launcher GPU archive gate (2026-09-22)

The launcher rewrites only overlay priority 54: NVIDIA/default uses the
208-record `_n` archive, while AMD uses a byte-identical 26-record subset `_a`
archive. The AMD choice removes 179 Mustafar ribbon/affector names without
fallback and falls back for only three. Effective asset manifests must therefore
record the post-launch archive selection and preserve name removal/fallback
semantics. See `ANALYSIS_CHECKPOINT_2026-09-22_GPU_TRE_OVERLAY_SELECTION.md`.

### Skeletal LOD composition bridge (2026-09-22)

Runtime analysis joins distance-based detail choice, caps/hysteresis, adjacent
availability fallback, SAT MSGN iteration, MLOD/basic generator level fetch and
per-detail generated-mesh aggregation. One logical selected index applies to
all SAT components while missing content can fall back to a neighboring usable
detail. See `ANALYSIS_CHECKPOINT_2026-09-22_SKELETAL_LOD_COMPOSITION.md`.

### MGN texture-renderer bindings (2026-09-22)

The rare TRTS mesh suffix is closed at its MGN boundary: three executable
version loaders, exact count/record grammar, all sixteen effective users,
their face-template paths and `MAIN` primitive bindings are documented. The
downstream BTRT graph is identified and version-dispatched; full command
execution remains a bounded renderer task. See
`ANALYSIS_CHECKPOINT_2026-09-22_MGN_TEXTURE_RENDERER_BINDINGS.md`.

### Blueprint texture-renderer envelope (2026-09-22)

All eight effective TRT resources are live BTRT/0001 face graphs. Factory and
version dispatch, ordered resource tables, static geometry, camera, render
commands and prepare commands are now bounded. Friendly command-opcode and DEST
attribute labels remain the next narrow decoder task. See
`ANALYSIS_CHECKPOINT_2026-09-22_BLUEPRINT_TEXTURE_RENDERER_ENVELOPE.md`.

### Blueprint texture-renderer commands (2026-09-22)

Executable dispatch and RTTI name the complete supported render/prepare command
families. All eight effective face graphs reduce to unconditional SSTC/SST1/STFP
preparation followed by SRSC/RTFC shader-selected triangle fans. Exact numeric
field labels remain a narrow consumer-correlation task. See
`ANALYSIS_CHECKPOINT_2026-09-22_BLUEPRINT_TEXTURE_RENDERER_COMMANDS.md`.

Live operation/runtime correlation subsequently closes all material operands:
texture table/tag binding, customization-indexed 1D selection, palette-derived
factor selection, shader-pass iteration and vertex-buffer triangle-fan draw.
Remaining BTRT unknowns are dormant variants and one policy Boolean.

### Blueprint texture-renderer dormant paths (2026-09-22)

The remaining graph variants are now code-bounded: recursive short-circuit
conditions, true/false operation branches, 2D texture selection, direct/alpha
factor operations, framebuffer clear and indexed-list drawing. CVTP is an
accepted always-true legacy alias in this executable. See
`ANALYSIS_CHECKPOINT_2026-09-22_BLUEPRINT_TEXTURE_RENDERER_DORMANT_PATHS.md`.

### Skeletal update, Godot coordinates and nameplate cache (2026-09-22)

The skeletal appearance-local frame contract is closed through cache
invalidation, ordered controller advancement, per-substep root motion,
synchronous animation-message fan-out before replacement, readiness/fade and
inactive-detail eviction. The Godot coordinate adapter now has passing pose
fixtures: position reflects Z and SWG `[w,x,y,z]` maps to Godot
`[-x,-y,z,w]`. Finally, the two object-text integer histories are identified
as current/previous layout displacement and projected screen-Y. See the
skeletal-alter, Godot-coordinate and nameplate-frame-cache checkpoints.

### SUNT runtime closure (2026-09-22)

RTTI and `SunTrackingObject::alter` map scalar 1 to yaw slew rate and scalar 4
to pitch slew rate, with exact local-space atan2 equations and delta-time
clamps. The middle `-10/+10` pair are authored lower/upper pitch limits but are
never read by this runtime. See
`ANALYSIS_CHECKPOINT_2026-09-22_SUN_TRACKER_RUNTIME.md`.

### STFP factory discriminator (2026-09-22)

The final live BTRT Boolean is not render state. `VariableFactory` lookup keys
on both customization name and the Boolean, while STFP retains only the
resulting factory index and its palette render path never reads the flag. The
original domain label remains unknown, but required behavior is closed. See
`ANALYSIS_CHECKPOINT_2026-09-22_STFP_FACTORY_FLAG.md`.

CFBC's final anonymous pairs are also closed as color/packed-ARGB,
depth/float32 and stencil/U32 enable-value pairs through the established D3D9
Clear backend signature. See
`ANALYSIS_CHECKPOINT_2026-09-22_BTRT_CLEAR_FIELDS.md`.

### Skeletal appearance alter order (2026-09-22)

The appearance-local frame contract is closed from `SkeletalAppearance2::alter`
through cache invalidation, ordered controller/physical-track advancement,
per-substep locomotion accumulation and synchronous animation-message fan-out.
Messages from an outgoing animation are delivered before its returned or queued
replacement is installed. Readiness/fade progression and periodic inactive-LOD
eviction follow controller advancement. See
`ANALYSIS_CHECKPOINT_2026-09-22_SKELETAL_APPEARANCE_ALTER_ORDER.md`.

### Godot coordinate adapter fixtures (2026-09-22)

The executable-confirmed SWG Y-up/+Z-forward basis and `[w,x,y,z]` quaternion
order are now joined to an explicit Godot adapter. Position/direction reflects
Z; quaternion maps to Godot `[-x,-y,z,w]`; winding and tangent handedness flip
once at import. Identity, three axis rotations and a mixed-pose matrix fixture
all pass. See `ANALYSIS_CHECKPOINT_2026-09-22_GODOT_COORDINATE_ADAPTER.md`.

### BTRT DEST output-format contract (2026-09-22)

`DEST` carries output width/height and an ordered U32 file-format list. The
loader applies a fixed 12-entry map, preserves duplicates, and passes the first
mapped format plus list count to texture creation as an ordered acceptable set.
The separate per-variable runtime slots track revisions, not DEST outputs.
The follow-up backend correlation recovers all 14 client enum names, D3DFORMAT
mappings, capability checks and ordered first-success behavior. Only the
historic spelling of invalid sentinel 16 is unavailable. See
`ANALYSIS_CHECKPOINT_2026-09-22_BTRT_DEST_FORMATS.md`.

### Options transaction and specialized controllers (2026-09-22)

`SwgCuiOpt` owns eleven child pages over a typed getter/setter/default binding
layer. Controls preview changes live, dialog activation snapshots every scalar
binding, OK retains changes, Cancel restores them, and close writes the
machine-options store. Chat Colors word/sentence rules, Alarm scheduling/preview
and AFK response state use explicit controller workflows. The narrow remaining
question is Cancel behavior for custom collections that bypass scalar restore
objects. See
`ANALYSIS_CHECKPOINT_2026-09-22_OPTIONS_PREFERENCES_ARCHITECTURE.md` and
`ANALYSIS_SESSION_2026-09-22_TO_1300.md`.

### ILF, CMPA and sprite/quad asset closure (2026-09-22)

The effective configured overlay now has complete, zero-error inventories for
three previously broad or tentative presentation families. `INLY/0000` has 297
live layouts and 87,937 cell-relative furnishing nodes; executable manager code
confirms local object creation, portal-cell binding, transform application and
building lifecycle ownership. `CMPA` has 165 live winners across deployed
versions 0003-0005 and 779 transformed child appearances; its runtime owns the
child/visibility hierarchy and fans operations across it. Ten nested-component
edges are acyclic, while eight legacy missing laser-bolt mesh children establish
part-local failure tolerance. Shared `SPRT`/`QUAD` v0000 has 469 effective
winners and supplies center, size, RGBA and shader identity to a runtime quad
primitive with explicit bounds. See the ILF, CMPA and sprite/quad checkpoints.

These closures deepen the original-asset requirement without changing the
conservative rounded estimates: **90% comprehensive / 96% implementation-grade**.

### BEAM, LEFX and scene-worker retirement closure (2026-09-22)

The dormant BEAM/0000 executable contract is bounded as two F32 dimensions, an
opaque Vector4 shader-instance parameter and shader path feeding a four-vertex
primitive. No effective configured BEAM asset exists, so field-range claims are
withheld.

LEFX is active and fully inventoried: 49 live `.ltn` winners, zero tombstones,
zero parse errors, all supported outer versions represented, thirteen PTEX
shaders and no missing shader/attachment/sound dependencies. The two U32 flags
control start/end attachment transform handling; the extra v0001 byte is read
and discarded. Runtime owns five procedural branch records and dynamic shader
geometry. See the BEAM and LEFX checkpoints.

Scene retirement policy is now reconciled by ownership. Scene-owned terrain
work is stopped/signalled/joined; process-global asynchronous asset work may
finish but cancelled callback identity suppresses stale publication; synchronous
file-streamer work reaches terminal completion. This closes the architectural
join-versus-discard question without claiming exact historical subscriber order.

Rounded estimates remain **90% comprehensive / 96% implementation-grade**.

### DTLA general appearance LOD closure (2026-09-22)

The next original-asset block reconstructs the distinct `DTLA` general
appearance detail selector from executable loader/runtime code and all
effective `.lod` records. The valid overlay has 6,198 live DTLA assets across
versions 0005–0007, with ID-keyed child paths, camera-distance intervals,
lazy runtime child construction, an eight-level cap and neighboring availability
fallback. It also exposes three tombstones, one non-DTLA `.lod` outlier, eight
duplicate-child-ID assets and 290 directly unresolved mesh child paths. The
385 no-render APT references resolve through a valid high-priority NRND
redirect after correcting the inventory helper's uncompressed-TRE size rule.
Policy forms `TEST` and `WRIT` feed DPVS visibility models through nested `IDTL`
geometry; RADR/flag effects remain open. Missing-child fetch failure is the immediate next targeted question. See the DTLA detail-appearance checkpoint.

This deepens the mandatory original-asset compatibility contract, but does
not justify moving the rounded estimates: **90% comprehensive / 96%
implementation-grade**. The registry is **2,198 distinct addresses / 0
duplicates**; the packet matrix remains **345 documented / 0 reachable gaps**.

### APT/IFF overlay revalidation (2026-09-22)

The kind-0 TRE payload-length correction was applied across the configured
48-archive overlay. All 6,153 live effective APT records parse as
`APT/0000/NAME`; two effective APT records are actual zero-payload entries.
There are no APT-to-APT redirects. Three live redirects have unavailable
archive-overlay targets (two absent paths and one zero-payload winner).
Nineteen live `.iff` and 939 live `.apt` records have compressed size zero
but nonzero kind-0 payloads; previous compressed-size-only scans of those
extensions require this corrected interpretation. The stable function and
packet inventories did not change. See the APT/IFF overlay checkpoint.

### Appearance fetch fallback boundary (2026-09-22)

The direct DTLA child fetch uses a VFS-existence wrapper that substitutes the
installed default appearance for an absent/masked child name. This corrects the
prior tentative hard-failure concern for those 290 directly absent mesh paths.
Recursive fetching of a live APT's NAME target does not pass through that
wrapper, so the three unavailable redirect targets remain separate risks. The
automatic absolute-path search node has priority above the configured TREE
archives and can resolve a loose file from the process working directory.
Six confirmed function names bring the registry to **2,204 unique addresses / 0
duplicates**. Rounded estimates remain **90% comprehensive / 96%
implementation-grade**; packet matrix remains **345 / 0**. See the
appearance-fetch fallback checkpoint.

### TRE size-tool and cockpit-frame correction (2026-09-22)

Five targeted Python TRE extraction tools were corrected to use `usize` for
kind-0 payload length and for SearchTree-style mask decisions. The configured
CPIT scan still has 36 valid profiles, but only 11 have no frame path; all 25
nonempty frame APT paths and their immediate targets are live. The prior 18
false missing frames came from `csize=0` on uncompressed APT records. Six
effective STF files with the same field combination are valid 13-byte empty
tables. Sound-overlay and forward-shader-reference results were unchanged on
re-run. See the TRE size-tool audit checkpoint. Rounded progress, stable names
and packet matrix remain unchanged.

### One-hour TRE and CLEF continuation (2026-09-22)

The follow-on global script scan found 15 additional legacy TRE readers with
the same kind-0/zero-csize defect. Corrected full-overlay inventories recover
84 previously omitted live CDF paths (6,288 total), 430 short CLEF assets,
53 one-row terrain TGA ramps, and 15 MKAT marker appearances. The 25 named
CPIT cockpit APTs resolve through all immediate targets and 22 live LOD
children. The full CEF corpus is 1,145 valid CLEF files plus one true mask;
all live versions are supported by Stardust.exe, with 1,889 recognized direct
child resources and 12 absent paths in configured TREs. Decompiled CLEF
readers and template-cache/audio lookup establish the three version schemas,
five leaf families, top-level CEF warning/null behavior, nested CPAP default
appearance fetch, and PSND missing-sound null handle. MKAT/0000/INFO loader
fields match all 15 recovered markers. Eleven bounded blocks add ten stable
names, bringing the registry to **2,214 unique addresses / zero duplicates**.
Rounded progress remains **90% comprehensive / 96% implementation-grade**;
packet matrix remains **345 / 0**. See the one-hour TRE/CLEF session report.

### Implementation hardening and original TRE slice (2026-09-29)

The user authorized one hour of development. The engine-neutral core now
recovers from damaged framing, clears owned state on failure/deadline, drains
large queued messages across ACK windows, and preserves the send span when
future entries receive selective ACKs. Independent peers cover reordered login,
fatal batch ordering, a 1.5 MB message and 32 seeded impairment transfers.
Outer SOE dispatch returns every control; strict JSON settings and allowlisted
diagnostics prepare the adapter boundary.

A TREE 0005 reader adds stored/zlib index and payload access, filename CRC,
priority and tombstones. One installed archive's 1,477 entries validate, and
a 348-byte extraction matches independent Python SHA-256. A reproduced missing
zlib-trailer acceptance defect led to shared header/Adler validation. This is
implementation progress; no research rescore, executable naming, packet matrix,
or curated registry change is claimed. No live server or original executable
was run. See the [session](research/sessions/IMPLEMENTATION_SESSION_2026-09-29_ONE_HOUR.md)
and [acceptance gates](docs/CLIENT_IMPLEMENTATION_ACCEPTANCE.md).

### Status observation boundary (2026-09-29)

Traced minimal server requests, 40-byte zero-statistics responses and the
close/encrypt/conditional-CRC send chain. CRC-disabled responses preserve two
zero reserved bytes. Existing client dispatcher evidence reads 40-byte status
bodies, preserving a discrepancy with the four-byte server sender. The core
now exposes bounded CRC-validated status observations; automatic replies and
the CRC exception remain gated pending direct receive-chain evidence. Fixed
independent synthetic frames and malformed-input/cleanup scenarios pass. No
semantic names, packet registries, research metrics or live-server claims change.
See the [checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-29_STATUS_BOUNDARY.md).

### Direct status CRC receive rule (2026-09-29)

Baseline SHA-256 and direct x86 instructions at `0x012C36B0` confirm that
standalone opcode `08` has no positive-width CRC bypass. Mismatch returns before
inverse transforms/dispatch, after optional failure notification. This resolves
the presumed exception while preserving the operational discrepancy with the
server's zero reserved trailer. A reusable static verifier checks identity and
18 instruction guards; the smoke suite accepts a repaired valid trailer and
rejects single-bit damage at every transformed response byte. Existing naming
is refined without registry/count changes. No network or original execution.
See the [follow-up](research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-29_STATUS_CRC_RECEIVE.md).

### Extended status reply implementation (2026-09-29)

Direct request-handler and send/accounting instructions establish the 40-byte
reply fields: echoed tick, local timestamp, two echoed peer 64-bit counters and
two local counters sampled before sending. Receive accounting precedes CRC
validation. An explicit engine-neutral builder and typed response parser match
independent Python body/protected-wire fixtures, reject incomplete requests and
preserve full-width counter bits. Replies are unsequenced and not retained.
Short server requests remain observations; automatic replies and host counters
are open. No registry/count/research-score changes or live execution.
See the [checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-29_STATUS_REPLY.md).

### Host and original asset implementation hour (2026-09-29)

Five bounded checkpoints add monotonic clock/I/O accounting, cancellable injected
login hosting, bounded persisted allowlisted JSONL, independently matched original
CPU mesh geometry and configured shader dependencies. The UDP adapter compiles
but was not instantiated. Godot's async offline host exits 0; its existing root
certificate diagnostic remains. One original mesh has 9 batches, 190 vertices,
159 triangles and a Python-matched geometry hash; all 48 configured archives
open and resolve its 9 shaders, 9 DDS envelopes and 1 effect. A raw mixed-case
index name corrected normalized-name CRC validation without creating aliases.
Primitive/0000 uses U32 file indices truncated to U16; /0001 uses U16, so SPS
version alone is insufficient. Durable specs and address observations reconciled;
no name/packet rows or research scores changed. No live activity/publication.
See the [five-checkpoint handoff](research/sessions/IMPLEMENTATION_SESSION_2026-09-29_HOST_AND_ASSET_HOUR.md).

## 2026-09-29 — textured preview implementation hour

Closed four bounded implementation checkpoints: [DDS pixels](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-29_DDS_PIXELS.md), [modern DDS](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-29_MODERN_DDS.md), [owned material graph](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-29_MATERIAL_GRAPH.md), and [original GPU preview](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-29_ORIGINAL_GODOT_PREVIEW.md). Eight BC1 textures and one BC3 texture provide 80 independently verified mips; all upload bytes and asymmetric UV/front-face GPU checks pass. STAG/0000 offsets 18/22 were corrected after a strict binding gate rejected the initial parser. No new stable address names or research score changes. See the [session](research/sessions/IMPLEMENTATION_SESSION_2026-09-29_TEXTURED_PREVIEW_HOUR.md) for reproduction and remaining scope.

## 2026-09-29 — overnight DTLA implementation checkpoint

[Owned DTLA parsing](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-29_DTLA_READER.md) now preserves deployed versions, authored ID/bands, policy geometry and irregularity diagnostics. Four parent-child meshes independently match native geometry bytes, including additional coordinate sets in declaration 0x55405. Prior L4 preview is archive-present but absent from this parent's graph. Native and independent original geometry checks pass; richer pixel-program graph support blocks L0/L1 textured bundles and is the next bounded parser task. No research metric or address registry change.

## 2026-09-29 — selector/material graph and original LOD inspector

Two bounded milestones close the [distance selector and repeated PTXM graph](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-29_DETAIL_SELECTOR_AND_MATERIALS.md) and [original four-child Godot inspector](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-29_GODOT_LOD_INSPECTOR.md). Native ordinary-distance/availability/negative tests pass; L0/L1 each have 134 reference-matching mip levels. All four original child uploads and midpoint switches pass headless and GPU gates. Preview is explicitly unlit MAIN fallback, not original material lighting or PIVT-derived runtime distance. No score/denominator or address-name change.

## 2026-09-29 — APT exterior entry point checkpoint

[Strict appearance resolution and exterior chain](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-29_APT_EXTERIOR_CHAIN.md) closes APT/0000 owned resolution, separate path-default semantics, additional 4D coordinate layouts and a three-level exterior inspector. Independent root/target/child identities, all three geometry hashes and 99 L0 texture mip hashes pass. Default preview mode still rejects a material without its required fallback; opt-in texture-only mode passes GPU gates and is clearly labeled. No research metrics or address registry changes.

## 2026-09-29 — verified modern BC7 checkpoint

[BC7 DDS extension](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-29_BC7_DECODER.md) adds bounded typed 2D RGBA decoding through pinned licensed managed dependencies. All eight modes/partition/rotation/mip cases pass 147 valid Pillow comparisons; one reserved case follows normative zero RGBA and records Pillow's opaque-black result. Godot nine-mip upload/readback passes. Original-client BC7 acceptance is not inferred, and HDR BC6H remains open. No score or function-registry changes.

## 2026-09-30 — component geometry checkpoint

[Owned CMPA and neutral composition](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_COMPONENT_GEOMETRY.md) matches 165 templates, 779 paths and 9,348 transform float bit patterns. A corpus failure corrected optional RADR IDTL geometry handling. Shared immutable templates, independent transforms, local omissions and fatal limits pass synthetic gates. Four vent-pillar LODs independently match and pass GPU neutral inspection; garage sorted-index meshes remain open. No score or registry changes.

## 2026-09-30 — sorted-index mesh and garage checkpoint

[SIDX and nested garage](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_SORTED_MESH_GARAGE.md) closes the concrete mesh blocker found in component inspection. Native U32/U16 sorted grammar, minimum-dot selection and first ties pass fixtures; all nine original geometry hashes and 400 variant hashes match independent parsing. Fourteen sources load with zero omissions and pass neutral GPU inspection. SIDX is preserved for a later real material path; no coverage or address-count change.

## 2026-09-30 — shared component material checkpoint

[Shared CPU/GPU resources](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_SHARED_COMPONENT_MATERIALS.md) verifies fifty original sources, eleven shaders, eight effects, seventeen textures and 151 independent RGBA mips. Deterministic sharing/failure/budget fixtures pass. Opt-in component inspection retains 35 supported textured surfaces and ten explicit neutral material batches, using seventeen shared GPU uploads. Camera/PIVT and faithful programs remain separate. No score/address-count change.


## APPR/PIVT camera selection increment — 2026-09-30

The [camera LOD checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_APPR_PIVT_CAMERA_LOD.md) closes the primary extent distance origin: false/absent pivot uses the transformed authored sphere center, true uses world translation, and NULL uses zero local offset. All 6,198 original primary extents agree independently. Native malformed/transform fixtures and standalone/composed headless/GPU selection gates pass. The inspector supports opt-in camera distance and manual selection. Original DPVS, lazy residency and full lighting remain deferred. Five confirmed address labels bring the canonical registry to 2,590 distinct entries; research scores and packet counts are unchanged.


## BC6H HDR extension — 2026-09-30

The [HDR checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_BC6H_HDR.md) adds a separate owned RGB half output for typed signed/unsigned BC6H. Forty standards-only fixtures/264 mips match independent Godot bcdec bit for bit and pass real GPU half upload/readback. Native endpoint, truncation, surface and budget gates pass. Static material integration and HDR presentation remain separate; this is not original executable acceptance evidence. Existing pinned dependencies, research metrics and registry counts are unchanged.


## APPR hardpoints — 2026-09-30

The [hardpoint checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_APPR_HARDPOINTS.md) adds owned exact names/CRC/transforms and explicit duplicate queries to mesh/detail/component templates. All 31,151 effective APPR bases/13,253 hardpoints agree independently. The old serialized count/name-prefix claim is corrected. Ninety-one duplicate-name groups contain different transforms; ambiguous attachment choice remains unverified. Native and focused host gates pass; metrics/registry counts are unchanged.


## Owned portal layout — 2026-09-30

The [portal reader checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_PORTAL_LAYOUT_READER.md) closes native deployed PRTO/CELL/PRTL parsing against all 291 layouts, 3,470 cells and 7,642 portal references. Owned geometry, names/door fields, optional bounds and paired zero-based adjacency pass independent agreement and negative fixtures. Scene loading, floor behavior and runtime visibility remain separate. The older 00B43E80 neighbor-helper attribution is corrected to a byte toggle. Metrics and registry counts are unchanged.


## Portal cell scene increment — 2026-09-30

The [cell scene checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_PORTAL_CELL_SCENE.md) closes shared bounded cell appearance loading and named-cell offline inspection. The pinned original YT-1300 matches independently across eight cells, 14 sources, 11 meshes, 66,617 vertices, 55,916 triangles and 310 sorted variants. VTXA `0x110D` preserves its packed word and correct UV offset. Native ownership/budget/identity tests and Godot headless/GPU gates pass. Manual visibility is distinct from unimplemented portal culling, floors, doors and lighting. Research scores, packet rows and address counts are unchanged.


## Owned floor reader — 2026-09-30

The [FLOR checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_FLOOR_MESH_READER.md) verifies all 4,730 deployed floors: 414,301 vertices, 604,429 triangles and 189,927 boundary records. Owned adjacency, authored normals/raw flags and optional payload hashes agree independently. Negative and ownership fixtures pass. Floor navigation/movement remains separate; metrics and registry counts are unchanged.


## Floor surface inspection — 2026-09-30

The [query checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_FLOOR_SURFACE_INSPECTION.md) closes a modern owned deterministic BVH surface inspector: 32,918 exhaustive independent probes across 4,730 floors agree. The original scene now includes seven floors/149 triangles under 21 shared sources; headless/GPU geometry and reflected picking pass. Native analytic/negative/ownership cases pass. Original adjacency path walking, permission and portal transitions remain unimplemented. Metrics and registry counts are unchanged.


## Owned path graph — 2026-09-30

The [PGRF checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_PATH_GRAPH_READER.md) closes deployed graph field decoding against all 3,914 graphs, 39,647 nodes and 86,964 directed edges. Raw positions/keys/types, width floats and ECNT/ESTR ranges agree independently. Empty zero-count/start-minus-one entries are preserved. Native malformed/ownership gates pass. Original navigation cost, cross-cell mapping and dynamic permission remain separate; metrics and registry counts are unchanged.


## Directed route inspection — 2026-09-30

The [route checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_DIRECTED_ROUTE_INSPECTOR.md) closes modern owned Euclidean/Dijkstra inspection against 25,847 independent Bellman–Ford probes, with exact edge-chain validation. Eight original floor/layout graphs upload and route-display in Godot; hidden GPU capture passes. Original client cost, cross-cell association and dynamic permission remain distinct. Metrics and registry counts are unchanged.


## Portal/floor association audit — 2026-09-30

The [association checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_PORTAL_PATH_ASSOCIATIONS.md) rejects floor-key-as-layout-ordinal and verifies conservative geometry-key bindings for all 291 layouts: 14,783 valid bindings, 3,581 paired candidates, 977 multi-node groups and 14 incomplete groups. Thirty-one bunker contradictions and 13 missing police-station floors remain explicit. Candidates do not establish legal movement. Shared cache failure accounting/pin guards pass; headless MultiMesh readback limits are reported accurately. Metrics and registry counts are unchanged.


## Textured interior inspection — 2026-09-30

The [textured portal checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_TEXTURED_PORTAL_SCENE.md) closes bounded shared interior materials and baseline IMPL0004/PASS0008 decoding. The original eight-cell scene independently agrees on 77 sources, seven complete material meshes, 23 textures/221 mips and 18 effect records. Four customizable exterior bundles remain explicit omissions. GPU unlit MAIN inspection uploads all texture mips and displays 78 textured/ten neutral surfaces; strict headless and native negative/budget fixtures pass. Original lighting/customization remain separate; metrics and registry counts are unchanged.


## Owned customizable shader reader — 2026-09-30

The [CSHD checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_CUSTOMIZABLE_SHADER_READER.md) verifies all 5,173 deployed version 0001 shaders,5,446 choices,856 selections and 7,908 palette factors. Native owned records agree independently on exact names, signed ranges/defaults, flags and inline shader identity. Static calls confirm private/shared-owner namespaces and clamped texture indexing. Inline values and authored defaults remain separate; runtime customization/palette translation is open. Metrics and registry counts are unchanged.


## Authored customization texture inspection — 2026-09-30

The [authored texture checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_AUTHORED_TEXTURE_INSPECTION.md) verifies856 selections under 2,568 default/extreme cases and completes all 11 original scene material bundles under explicit preview policy. Independent agreement covers87 sources,33 textures/321 mips and 39 effect records. GPU inspection shows85 textured/three neutral surfaces; unused choices are not loaded. Palette colors, original lighting and live customization remain open. Metrics and registry counts are unchanged.


## BC6H material integration — 2026-09-30

The [HDR material checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_BC6H_MATERIAL_BUNDLES.md) preserves typed BC6H RGB-half data through shared core material caches and actual Godot MAIN samplers. Independent40-fixture/264-mip agreement and 80 template bindings pass on GPU and separately headless. Mixed RGBA/HDR aggregate budgets pass exact/fatal gates. Original87-source/321-mip scene agreement remains intact. HDR tone mapping/full lighting remain separate; metrics and registry counts are unchanged.


## Owned palette reader — 2026-09-30

The [palette checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_PALETTE_READER.md) verifies264 palettes/23,410 entries and 24,730 index queries. Raw flags, opaque ARGB, three ignored tails and low-version warning policy pass. The prior missing-fallback color label is corrected to cyan 0xFF00FFFF. CSHD references expose one missing palette and 16 out-of-range authored defaults; these remain explicit. Metrics and registry counts are unchanged.


## Palette-factor bundles — 2026-09-30

The [factor checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_PALETTE_FACTOR_BUNDLES.md) closes bounded pinned palette dependencies and explicit supplied/authored factor queries. All 7,908 factors agree under 23,724 independent cases. Exact namespaces, owned results, missing/masked cyan, empty/malformed distinctions and aggregate budgets pass native gates. One missing factor and sixteen invalid defaults remain explicit. Original lighting/live customization are separate; research scores and registry counts are unchanged.


## Palette-factor inspector — 2026-09-30

The [swatch checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_PALETTE_FACTOR_INSPECTOR.md) adds editable authored/supplied color inspection with exact variable namespaces and shared-key controls. Original YT-1300 and missing-palette swamp-trooper shaders pass two-swatch GPU readback; headless and synthetic shared-control gates pass. Captures remain private. Scene material lighting/live owner state remain separate; metrics and registry counts are unchanged.


## Hardpoint instance inspection — 2026-09-30

The [placement checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_HARDPOINT_INSTANCE_INSPECTION.md) verifies owned per-instance hardpoint composition against two original graphs: 26 placements/312 fields. Exact name ambiguity, occurrence paths, cycles and limits pass native gates. Godot reflected marker transforms and cell/LOD visibility pass headless/GPU inspection with readable selected labels. Original runtime attachment lookup remains separate; metrics and registry counts are unchanged.


## Same-cell surface route selection — 2026-09-30

The [connection checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_SURFACE_ROUTE_SELECTION.md) verifies owned floor-hit-to-nearest-node selection against all 149 original scene probes. Missing/empty graph and cell isolation policies pass native gates. Seven reflected floor samples drive named-cell/route-start controls in headless/GPU hosts. Original walking, costs and portal transitions remain separate; metrics and registry counts are unchanged.


## Single-cell floor movement — 2026-09-30

The [movement checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_SINGLE_CELL_FLOOR_MOVEMENT.md) closes an owned horizontal single-cell walker and two-pick Godot inspector. Static edge/caller evidence, 500 seeded grid walks, 17,658 independent probes across 4,441 eligible installed floors, and seven reflected cell adapters pass. Portals override raw flags; missing-neighbor transitions, special collision edges and nearly vertical surfaces stop explicitly. Cross-cell permission/owner continuation remains next. Research scores, packet rows and stable-name counts are unchanged.

## 2026-09-30 — Portal floor transfer milestone

Completed engine-neutral bounded owner transfer, remaining motion and current permission seam; 14 original links / 56 independent probes and 28 Godot adapter probes pass. Static analysis confirms local floor portal-reference ordinals and entry callback ownership; corrects 0126AA80 as same-floor projection rather than cell transfer. Three names bring the canonical registry to 2,593; research scores and packet coverage unchanged. See [checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_PORTAL_FLOOR_TRANSFER.md). Next: portal barrier lifecycle and actor contact ordering.

## 2026-09-30 — Portal barrier collision milestone

Completed a bounded permission-barrier actor sweep and explicit normal-controller timing/events. Recovered BarrierObject/BarrierCollisionProperty RTTI, padded 0.05 portal-frame bounds, permission/candidate/raw-mode predicates and version-specific template flags. Confirmed that the inversion pass is PRTO/0002 only; supported flag mapping separates LeadingByte, SolidByte and WindingByte. Corpus agreement: 291 layouts / 7,642 references / 21,693 analytic probes. Host: 12 level links / 24 contact/recovery probes plus two explicit nonlevel stops. Registry now 2,597; research/packet metrics unchanged. See [checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_PORTAL_BARRIER_COLLISION.md). Next: one original style and physical leaf clearance.


## 2026-09-30 — Original single-leaf door milestone

The [checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_SINGLE_LEAF_DOOR.md) implements one normal original style with owned asset loading, contact retention and conservative clearance. Static refinement confirms local trigger center (0,1,0), retention radius 1.5 times authored radius, and power/cosine displacement blend. Independent checks match 124 original rows and 100 frames; core and headless/GPU adapter gates pass. Two semantic names bring the registry to 2,599; other coverage metrics unchanged. Next: named POB instance integration and continued solver-order evidence.


## 2026-09-30 — Original interior door movement milestone

The [checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_INTERIOR_DOOR_MOVEMENT.md) binds original cantina cell 3/reference 10 and private-room cell 12/reference 0 to the normal leaf runtime and actor-box transfer query. Authored transform composition, captured physical poses, source floor contacts and permission separation pass 24 independent original cases. Headless/GPU controls pass 12 cases over two owner poses; YT-1300 regression gates pass. No new semantic names or coverage rescore. Next: persistent actor movement and directional runtime coordination.


## 2026-09-30 — Persistent doorway actor

The [checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_PERSISTENT_DOOR_ACTOR.md) adopts actual movement locators per tick and shares one modern normal leaf across matching incident references. Contact survives owner changes; explicit separating-face policy permits blocked retreat while genuine overlap remains unresolved. Original independent trace passes 948 ticks / 44 transfers; synthetic and Godot repeated-owner controls pass. No new original semantic names or coverage rescore.

## 2026-09-30 — Fixed-step actor and keyboard adapter

The [checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_FIXED_STEP_ACTOR.md) closes modern render/simulation separation, bounded stall accounting, pause/focus suspension and continuous W/S controls. Independent three-partition/two-pose original verification passes 1,428 updates and 1,260 ticks. Core/headless/GPU controls pass. No new executable names or coverage rescore.

## 2026-09-30 — Timestamped actor inputs

The [checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_ACTOR_INPUT_TIMELINE.md) closes six between-boundary transitions across three render partitions and two original owner poses. Independent replay passes 1,488 updates / 1,320 ticks / 18 transfers; bounded queue, skipped/pause history and Godot event routing pass. No new VA names or rescore.

## 2026-09-30 — Planar doorway movement

The [checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_PLANAR_DOORWAY_ACTOR.md) closes normalized planar input, transformed lateral/diagonal round trips and explicit authored-floor stops. Independent original 268 ticks / four transfers plus native/headless/GPU checks pass. No new semantic labels or rescore.

## 2026-09-30 — Rigid detail door capabilities

The [checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_DETAIL_DOOR_CAPABILITIES.md) inventories all installed styles/POBs and adds complete DTLA primary leaves with fixed authored root boxes. Acceptance rises 53 to 91 rows; 38 new rows pass 3,556 independent vertex checks. Current and former boundaries are reproducible; native/headless/GPU/actor regression gates pass. No new semantic names or coverage rescore.

## 2026-09-30 — Detail-door instances

The [checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_DETAIL_DOOR_INSTANCES.md) reconciles floor pairing/identities for 782 candidates and screens 31,980 native actor ticks. Selected upward Mustafar leaf passes 1,896 independent ticks / 88 transfers over both directions/two poses and headless/GPU adapter gates. Six boundary-locator failures are preserved as the next numerical probe; no rescore/new names.

## 2026-09-30 — Floor contact stability

The [checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_FLOOR_CONTACT_STABILITY.md) fixes six returned-contact failures while preserving strict input rejection. Seeded 5,000 and installed 17,658 reuse checks pass; YT transfer/cantina/host regressions pass. No new semantic names or rescore.

## 2026-09-30 — Piecewise floor paths

The [checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_FLOOR_PATH_TRACES.md) establishes ordered double-precision triangle-plane traces without changing walk outcomes. Installed 17,658 requests / 22,063 segments / 110,315 independent plane checks and seeded crease gates pass. No rescore or new names.


## 2026-09-30 — Piecewise actor-box sweep

The [checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_FLOOR_PATH_BOX_SWEEP.md) adds precise segment collision with bounded validation, earliest global parameters and explicit permission/overlap/retreat policy. Six thousand independent synthetic cases pass. No original solver-fidelity claim or rescore.


## 2026-09-30 — Single-cell actor-box contact landing

The [checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_SINGLE_CELL_FLOOR_BOX.md) closes stateless earliest-contact landing and reusable locators under bounded precision. Independent 6,000 synthetic cases plus retreat/order/error/large-coordinate hold gates pass. No original-corpus claim or rescore.


## 2026-09-30 — Original floor-box contact stability

The [checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_ORIGINAL_FLOOR_BOX_CONTACTS.md) verifies 600 original sloped floors with synthetic obstacles: 480 box stops, 120 clear, 28 bounded corrections and all locators reusable. Twelve thousand repeat queries have zero drift; 530 retreat to source and 70 initial overlaps remain blocked. No new original semantic labels or rescore.

## 2026-09-30 — Complete portal level-path guard

The [checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_PORTAL_LEVEL_PATH_GUARD.md) rejects hidden source/target ridges through exact transfer traces. Native counterexamples, cantina/Mustafar independent actors and Godot host checks pass. Candidate screening counts remain unchanged. Full portal slope collision remains next.

## 2026-09-30 — Precise source/target floor-box query

The [checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_PRECISE_PORTAL_FLOOR_BOX.md) verifies target boxes during source approach, global parameters, reusable ownership and world/local precision budgets. Independent 2,400 synthetic and prior landing regressions pass. No original actor-fidelity claim or rescore.

## 2026-09-30 — precise persistent sloped door actor

[Checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_PRECISE_DOOR_ACTOR.md): engine-neutral directional barrier/leaf adapter captures snapshots before callbacks. Original YT-2400 both starting directions/two poses match 1,896 independent ticks / 88 transfers / 53 blocked. Same screen improves 131 historical level round trips to 495 precise round trips; remaining 37 Accepted and one LandingFailed stay open. Bounded rounded-XZ height reprojection fixes a thin-triangle returned locator; 1,000 synthetic steep-plane gates and 17,658 original floor probes pass. No new original semantic labels, research score or protocol coverage claims.

## 2026-09-30 — precise sloped doorway Godot adapter

[Host checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_PRECISE_DOOR_GODOT.md): explicit launcher option, zero-warning build, pinned forward-start headless and reverse-start GPU controls pass. Scheduling, input, repeated transfers and reflected placement verified; private capture inspected. No research metrics or original fidelity claims changed.

## 2026-09-30 — thin portal target landing

[Checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_PORTAL_LANDING_NORMAL.md): bounded inward edge normal fallback supplements centroid bridging. Yavin reverse failure closes; original 948-tick reference and 1,000 synthetic thin targets pass. Same screen now completes 496 short round trips, with 37 Accepted non-round-trips. Original YT-1300 and 2,400 portal-box references unchanged.

## 2026-09-30 — remaining doorway boundary audit

[Checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_DOOR_BOUNDARY_AUDIT.md): first forward stops independently correlated against original floor geometry and portal references. All 37 non-round-trips categorized, including 19 solid near-vertical neighbors and six other named floors outside the two-cell load. Producer source checks and screen counts unchanged. Next: bounded modern step-over, without claiming recovered original locomotion.

## 2026-09-30 — modern reciprocal riser proposal

[Checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_FLOOR_STEP_PROPOSAL.md): owned proposal query uses explicit 0.2 m height, 1 mm width and eight-triangle limits. Synthetic negatives pass; nine of 19 original boundary proposals independently check plane and adjacency, ten reject explicitly. No actor movement or original step-tolerance claim; clearance remains next.

## 2026-09-30 — one-step box clearance

[Checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_FLOOR_STEP_BOX.md): supported source hold on blocked vertical travel, target continuation and global progress mapping. Native negative/repeat gates pass; nine original floors pass 36 independent blocker cases and 360 step transitions. Modern declared policy; door/portal integration next.

## 2026-09-30 — world portal actor step integration

[Checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_PORTAL_FLOOR_STEP.md): one step/one transfer, world snapshots and floor-owned vertical rollback. Pinned small-house independent trace 1,896/88 transfers/88 steps/58 blocked; same screen 504 short round trips. Defaults unchanged, modern policy explicit, host step gate next.

## 2026-09-30 — step doorway Godot adapter

[Checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_STEP_DOOR_GODOT.md): explicit bounded-step launcher option, zero-warning build, pinned forward headless/reverse GPU controls and scheduling/input gates pass. 20 repeated manual transfers / 23 step events; private capture inspected. Defaults unchanged.

## 2026-09-30 — local vertical step coverage

[Checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_FLOOR_STEP_RAY.md): separately declared vertical-line/triangle coverage, independent chord checks and connected-hole rejection. Original cohort 16 accepted / three NoLanding; 64 movement/blocker cases and 640 repeat transitions pass. Tiny blocked-edge jitter replaced by bounded owned hold; no global acceptance changes.

## 2026-09-30 — local-step original doorway actor

[Checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_RAY_STEP_DOOR_ACTOR.md): original black-sun 1/1 ↔ 2/1 passes 1,896 independent ticks, 88 transfers, 90 steps and 56 blocked. Same local screen 509 short round trips; previous modes remain separate. Small-house whole-strip regression unchanged; host local-option gate next.

## 2026-09-30 local-step Godot integration

[Host checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_RAY_STEP_DOOR_GODOT.md): explicit local policy flag with prerequisite checks; forward headless/reverse GPU original black-sun controls, scheduling/input and placement pass. Build zero warnings/errors. No new VA names or protocol rows.

## 2026-09-30 connected approach

Optional selected-edge approach construction, stacked-floor and obstructed-route gates, original endpoint checks and lower black-sun repeated trace pass. No default movement change or new VA/protocol rows. See the [checkpoint](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_CONNECTED_DOOR_APPROACH.md).

## 2026-09-30 connected host gate

[Lower-floor host](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_CONNECTED_DOOR_GODOT.md) passes forward headless/reverse GPU controls, scheduling/input and reflected placement; 20 manual transfers each, 48/50 manual step events. Explicit `-ConnectedDoorApproach` requires local, step and precise movement.

## 2026-09-30 multi-door scene

[Bounded owned scene](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_INTERIOR_DOOR_SCENE.md) passes synthetic three-cell/two-leaf lifecycle and 80 repeated native transfers. Independent reference matches 1,440 ticks / 24 transfers / 128 blocked ticks. Original scene gate is next; no coverage score changes.

## 2026-09-30 original multi-door route

[Existing Mustafar scene](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_ORIGINAL_INTERIOR_SCENE.md) independently passes 2,880 ticks / 24 transfers / 172 blocked ticks / zero steps across three cells, two physical leaves and two owner poses. Directional denial/unknown/retreat/recovery and controller lifecycle checked. No broad new POB survey or score change.

## 2026-09-30 shared typed scheduling

[Shared scheduler/timeline](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_TYPED_SCENE_SCHEDULING.md) preserves single-door API and complete multi-door snapshots. Three render partitions and direct 420-tick scene reference match; stall/pause/partial fault/reset/dispose/reentrancy and existing single-door native/original headless gates pass.

## 2026-09-30 pinned scene bundle

[Reusable bundle](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_INTERIOR_SCENE_BUNDLE.md) pins bounded explicit selections and shares normalized asset lookup. Malformed/duplicate/bad identity gates and unchanged 2,880-tick original scene reference pass. No score changes.

## 2026-09-30 original scene host

[Godot multi-door scene](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_INTERIOR_SCENE_GODOT.md) passes headless/GPU: 16 manual transfers / two poses, second denial/retreat/reset, physical timestamped keys and diagonal round trips, typed four-transfer route, stall/pause/focus and both-leaf reflected placement. Private capture inspected.

## 2026-09-30 explicit closing safety

[Optional closing sweep](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_DOOR_CLOSING_SAFETY.md) holds an obstructed close without granting entry. Native lifecycle/event/failure/scene-retreat gates and independent 240 ticks / 52 holds pass; original unguarded 2,880-tick scene regression unchanged. Original obstruction policy remains unknown.

## 2026-09-30 original occupied close

[Original closing/host gate](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_ORIGINAL_DOOR_CLOSING_SAFETY.md) independently matches 692 guarded/default ticks over two poses: 156 holds in guarded mode, 314 physical stops in unguarded replacement mode. Explicit `-HoldClosingDoors` headless/GPU denied-entry/retreat/release and existing timing/input/placement gates pass. Original game obstruction behavior remains unknown.

## 2026-09-30 selected scene rooms

[Selected original geometry](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_SELECTED_SCENE_ROOMS.md) independently matches three cells / 72,607 vertices / 42,986 triangles. Headless/GPU owner placement and existing multi-door controls pass; room meshes are rendering only. Research metrics unchanged.

## 2026-09-30 selected room textures

[Selected MAIN material gate](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_SELECTED_SCENE_TEXTURES.md) adds inline IMPL/0006–0007 structural support and independently matches 26 textures / 251 mips / 25 effect records. Original headless/GPU host draws 42 textured surfaces, two diagnosed neutral fallbacks. Metrics unchanged.

## 2026-09-30 zero-pass rendering

[Zero-pass surface gate](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_ZERO_PASS_SURFACES.md) passes empty/mixed/ordinary native classification and original headless/GPU mapped upload checks. Selected rooms now render 42 textured / two omitted / zero neutral surfaces, zero diagnostics. Metrics unchanged.

## 2026-09-30 portal visibility

[Bounded visibility gate](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_PORTAL_VISIBILITY.md) passes synthetic aperture/ownership/budget negatives, six original views / 384 independent sampled rays and headless/GPU two-pose room-node selection. Logical ownership remains separate. Metrics unchanged.

## 2026-09-30 camera heading input

[View/input gate](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_ACTOR_VIEW_INPUT.md) passes native heading/pitch and two-partition 35ms turn timing, plus original headless/GPU two-pose mouse event/camera/visibility/release gates. Physical OS grabbing is not manually verified. Metrics unchanged.

## 2026-09-30 combined interior content

[Owned source union](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_INTERIOR_SCENE_CONTENT.md) passes exact independent 61-source / 9,262,168-byte identity union and one-read/resource/substitution gates, original 2,880-tick regression and headless/GPU host. Selected authored bounds are NULL and FLOR supplies no walls/ceilings. Metrics unchanged.

## 2026-09-30 shared scene visuals

[Shared visual gate](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_SHARED_SCENE_VISUALS.md) independently verifies both original leaf LODs and expanded 65-source / 9,614,085-byte union. Five material templates / 28 decoded and GPU textures are shared; separate leaf nodes/controllers/poses and original host/trace gates pass. Metrics unchanged.

## 2026-09-30 background scene load

[Owned worker gate](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_BACKGROUND_SCENE_LOAD.md) passes single-transfer/stale/cancellation/fault cleanup, original headless/GPU main-thread publication and pending-first-lookup cancellation with zero scene/GPU publication. Existing scene gates remain passing. Metrics unchanged.

## 2026-09-30 continuous triangle-box contact

[Contact kernel](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_TRIANGLE_BOX_SWEEP.md) agrees with independent LP vertex enumeration over512 synthetic float32 sweeps:418 clear,79 moving contacts,15 initial contacts. Native ownership/tie/touch/invalid-input gates pass. No original collision-source or research-score change.

## 2026-09-30 precise triangle feet paths

[Path adapter gate](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_TRIANGLE_FLOOR_PATH.md) passes slope, ceiling-lift, distant-coordinate precision, full-validation/work-limit negatives and512 independent LP segment/time comparisons. Straight-sweep512-case regression passes. No original physics or research-score claim.

## 2026-09-30 supported triangle-floor contact

[Owned walker](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_TRIANGLE_FLOOR_WALK.md) adopts only validated supported contact, rechecks the complete candidate path and holds source on unrepresentable float landing. Native flat/ramp/ceiling/repeat/retreat/failure gates pass. Original host unchanged; metrics unchanged.

## 2026-09-30 triangle floor-step clearance

[One-step guard](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_TRIANGLE_FLOOR_STEP.md) preserves supported source on triangle ceiling contact, checks approach and target continuation, and passes native rise/drop/repeat/retreat/height gates. Existing modern step limits retained; no original obstacle or research-score claim.

## 2026-09-30 selected helper geometry

[Pinned three-mesh audit](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_SELECTED_HELPER_GEOMETRY.md) reuses existing inputs:96 triangles,75 vertical/21 sloped, no degenerates or horizontal faces, route-disjoint bounds. Shader naming remains correlation; no original collision integration follows.

## 2026-10-01 mesh collision eligibility

[Runtime trace](research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-01_MESH_COLLISION_SELECTION.md) establishes extent mode, primitive receiver dispatch, two selected shader predicates and IMPL version derivation. Native truth-table/negative gates and164 independently parsed original candidates pass. Hall geometry is viable within current query budget; original actor policy remains open. No address names or coverage metrics changed.

## 2026-10-01 supported portal triangle contact

[Owned one-transfer guard](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_PORTAL_TRIANGLE_CONTACT.md) passes native transformed-owner/source-approach/target-support/repeat/retreat gates and512 independent synthetic slope/seam/LP comparisons. Original hall geometry and door composition remain next. Metrics unchanged.

## 2026-10-01 original hall body contact

[Pinned original slice](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_ORIGINAL_HALL_COLLISION.md) matches1,984 extracted triangles/zero degenerates,16 requests with4 wall/12 floor stops,1,600 stable repeats and16 retreats. Independent original LP/floor checks pass. Tangent-entry correction and explicit modern0.1 mm feet clearance address authored micro-offsets. Prior1,536 synthetic comparisons remain passing. Wider rooms/host composition remain next; metrics unchanged.

## 2026-10-01 conservative triangle hierarchy

[Owned bounds index](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_TRIANGLE_BOUNDS_HIERARCHY.md) matches512 exhaustive varied queries exactly;8,445/65,536 candidates. Explicit3,072-source grid selects1 candidate/19 nodes. Opt-in capacity65,536 retains default2,048 and complete path work limits. Original hall gates pass; metrics unchanged.

## 2026-10-01 complete selected room collision geometry

[Full42,986-triangle probe](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_COMPLETE_ROOM_TRIANGLES.md) retains all original geometry with zero degenerates, independently matching hashes/contact. Eight portal requests yield4 clear/4 target contacts,8 reverse transfers and400 stable holds. Rendered0.1 m rises overlap flat FLOR; conservative body stops expose original actor participation/offset as the next decision. Wider persistent-host activation is not claimed. Metrics unchanged.


## 2026-10-01 actor collision extent boundary

[Static participation trace](research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-01_ACTOR_EXTENT_PARTICIPATION.md) confirms transformed collision extents, nested SimpleExtent shape dispatch, local/transformed invalidation and separate floor/body selection in inspected paths. It does not establish every controller's reachability or original actor triangle participation. Conservative full-triangle host activation remains deferred while extent sources are traced. Metrics unchanged.


## 2026-10-01 authored secondary extent slice

[Bounded decoder](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_AUTHORED_COLLISION_EXTENTS.md) separates primary bounds and secondary APPR collision extents. Six pinned original room/door appearances agree independently on primary F32 values and NULL secondaries. Native gates and Godot build pass; runtime fallback/actor selection remain open. Metrics unchanged.


## 2026-10-01 creature fallback dimensions

[Static fallback trace](research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-01_CREATURE_COLLISION_FALLBACK.md) confirms appearance secondary, layout cell0 extent and conditional creature sphere precedence. Ordinary sphere center uses step height plus radius; collision height is a separate template field. Original template values and controller reachability remain next. Metrics unchanged.


## 2026-10-01 original creature template profile

[Object-root slice](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_ORIGINAL_CREATURE_TEMPLATE_PROFILE.md) resolves five unchanged inherited IFF sources and eight deterministic fields with independent value/provenance agreement. Human fallback sphere is center(0,1,0), radius0.5; lowest point0.5 above origin. Native negative gates and Godot build pass. Preview actor volume and controller equivalence remain separate; metrics unchanged.


## 2026-10-01 continuous sphere contact

[Modern sphere kernel](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_TRIANGLE_SPHERE_CONTACT.md) passes analytic feature/ownership/negative gates,512 exact bounds/exhaustive comparisons and512 independent convex-distance queries.249 moving/134 clear/129 initial; max time error1.55e-15. Profile-offset floor adoption remains next; no actor-host or original solver claim. Metrics unchanged.


## 2026-10-01 sphere floor adoption

[Precise floor adapter](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_SPHERE_FLOOR_ADOPTION.md) passes synthetic contact/reuse/rollback gates and independent unchanged hall checks.16 boundary stops,1,600 stable repeats,16 retreats; raised sphere uses explicit center offset without feet clearance. Portal ramp composition remains next. Metrics unchanged.


## 2026-10-01 portal sphere profile

[Original-profile portal adapter](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_PORTAL_SPHERE_PROFILE.md) passes512 independent synthetic cases and the unchanged42,986-triangle route. All eight original requests clear, including the low render rises;800 endpoint repeats/eight reverse transfers pass. Sphere dimensions come from the original five-file human template chain. Door-box composition remains next; original actor participation and metrics unchanged.


## 2026-10-01 solid box sphere contact

[Captured solid-box kernel](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_SOLID_BOX_SPHERE_CONTACT.md) passes analytic gates and512 independent distance comparisons:269 blocked/115 clear/128 initial overlap, max time error6.88e-15. Rounded features and deep interior overlap are preserved. Floor/door composition follows; metrics unchanged.


## 2026-10-01 sphere-box floor transfer

[Captured-box floor adapter](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_SPHERE_BOX_FLOOR_TRANSFER.md) passes native ownership/snapshot/negative gates,512 independent slope/seam/rotated-box cases and Godot build.36 accepted/281 obstacle stops/195 floor stops; physical door composition follows. Metrics unchanged.


## 2026-10-01 persistent sphere doors

[Explicit scene sphere option](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_PERSISTENT_SPHERE_DOORS.md) passes80 synthetic transfers and2,880 original-profile ticks:24 transfers/182 contacts.14 sources and all returned floor planes are audited; dynamic controllers are not independently reimplemented. Conservative closing cube passes synthetic hold/release gates; original occupied-threshold/preview work follows. Metrics unchanged.


## 2026-10-01 original-profile preview

[Pinned object-root preview option](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_ORIGINAL_PROFILE_PREVIEW.md) loads70 unchanged sources/9,622,375 bytes once through the worker/cache. Native content frame audit and original headless/GPU route/occupied closing/cancellation gates pass; default box host regression passes. Character appearance is referenced, not rendered. Combined modern room/door obstruction is next; metrics unchanged.


## 2026-10-01 combined sphere scene

[Combined sphere guard](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_COMBINED_SPHERE_SCENE.md) retains42,986 selected original triangles and composes earliest supported room/door contact. Full native, original2,880-tick source/geometry/frame audit and combined headless host gates pass. Local mean tick0.702 ms; oversized work rejects before mutation. Character descriptors follow; original participation and metrics unchanged.


## 2026-10-01 human appearance descriptors

[Object-root SAT/MLOD slice](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_HUMAN_APPEARANCE_DESCRIPTORS.md) matches eight unchanged sources/9,080 bytes and every dependency/flag/band independently. Full native and original pin/mask/budget gates pass. Character skeleton/mesh/rendering remain next; metrics unchanged.


## 2026-10-01 human skeleton descriptors

[Owned SKTM/SLOD slice](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_HUMAN_SKELETON_DESCRIPTORS.md) independently matches ten unchanged sources/19,394 bytes and five levels/131 joints. Full native/original negative and field-hash gates pass. Head/ head case mismatch resolves under instruction-backed default ASCII folding; locale behavior remains separate. Bind matrices/rendering follow; metrics unchanged.


## 2026-10-01 human bind transforms

[Engine-neutral bind builder](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_HUMAN_BIND_TRANSFORMS.md) passes noncommuting known poses and independently agrees on176 original body/face matrices (max4.141e-7). Original ten-source descriptors and Godot build pass. MGN geometry/rendering follows; metrics unchanged.


## 2026-10-01 human skin sources

[Bounded original MGN prefix](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_HUMAN_SKIN_SOURCES.md) independently matches twelve sources/329,296 bytes,1,454 positions/2,170 weights and all named-joint correspondence. Native/negative gates pass. Blend/occlusion/TRTS are retained metadata; primitive decoding follows. Metrics unchanged.


## 2026-10-01 human MGN geometry

[Owned shader geometry](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_HUMAN_MGN_GEOMETRY.md) independently matches1,790 original shader vertices/2,810 triangles/five shaders/zero degenerates and retains the face texture-renderer binding. Native/negative gates pass. Neutral preview follows; skinning/customization/occlusion/face generation remain separate. Metrics unchanged.


## 2026-10-01 neutral original character preview

[Object-root inspector](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_NEUTRAL_CHARACTER_PREVIEW.md) passes original headless/cancellation/GPU upload and inspected capture. Twelve sources/five surfaces/2,810 triangles display an aligned neutral bind-pose human. Skinning/material customization/face generation/animation remain open; persistent scene placement follows. Metrics unchanged.


## 2026-10-01 interior character visual

[Interior character visual](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_INTERIOR_CHARACTER_VISUAL.md) passes77-source independent audit,2,880 original ticks, both-pose placement, cancellation and inspected GPU capture. Five neutral surfaces now follow persistent actor feet in the original interior. Binding/skinning/animation remain pending; metrics unchanged.


## 2026-10-01 skeletal joint binding

[Skeletal joint binding](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_SKELETAL_JOINT_BINDING.md) resolves53 original attached joints/52 mesh names and independently verifies4,362 modern position diagnostics. Bind preservation error below4e-7; original consumer/normal/tangent semantics remain open. Metrics unchanged.


## 2026-10-01 skeletal influence consumers

[Skeletal influence consumers](research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-01_SKELETAL_INFLUENCE_CONSUMERS.md) confirms TWHD/TWDT position groups, complete transfer without four-weight truncation, and PIDX-selected influence ownership for NIDX normals. Weighted position/direction evaluator and direct normal output traced; animated matrix producer remains open. Metrics unchanged.


## 2026-10-01 skeletal shader deformation

[Skeletal shader deformation](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_SKELETAL_SHADER_DEFORMATION.md) passes unequal-map/raw-direction/negative fixtures and independently agrees on5,370 original shader vertex diagnostics. Complete PIDX weights deform NIDX normals and DOT3 tangents; generated animation remains open. Metrics unchanged.


## 2026-10-01 weighted character pose preview

[Weighted character pose preview](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_WEIGHTED_CHARACTER_POSE_PREVIEW.md) shows a visibly bent original forearm under explicit synthetic pose. Eight headless/GPU switches/reset, complete upload and cancellation gates pass; original animation playback remains open. Metrics unchanged.


## 2026-10-01 logical animation tables

[Logical animation tables](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_LOGICAL_ANIMATION_TABLES.md) independently matches14 original sources/710,463 bytes and1,046 serialized entries. Four duplicated body names retained; authored/synthetic default and opaque typed subtrees validated. Selected clip/proxy execution remains next; metrics unchanged.


## 2026-10-01 selected animation proxy

[Selected animation proxy](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_SELECTED_ANIMATION_PROXY.md) independently matches15 original sources/726,134 bytes and one original ambient idle CKAT envelope. Owned proxy/scale and masked/wrong-pin gates pass; channel decoding/playback remain next. Metrics unchanged.


## 2026-10-01 compressed keyframe structure

[Compressed clip structure](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_COMPRESSED_KEYFRAME_STRUCTURE.md) independently matches all selected original channel arrays and 2,284 keys through fifteen unchanged sources. Thirty-seven clip names match the body skeleton; `rifle` remains unresolved. Packed quaternion evaluation/playback remain next. Native truncation/negative/ownership gates pass; metrics unchanged.


## 2026-10-01 packed quaternion numerical decoding

[Packed quaternion decoder](research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_PACKED_QUATERNION_DECODE.md) independently agrees with executable-table decoding of all 2,225 selected original rotations (maximum error9.14e-8, zero selected repairs). All deployed-selector native gates and explicit overshoot diagnostic/repair pass. Interpolation, unmatched transform handling and playback remain next; metrics unchanged.
