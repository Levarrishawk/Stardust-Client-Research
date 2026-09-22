# Procedural-terrain open-item reconciliation

Date: 2026-09-21  
Target: `C:\Stardust-DEV\Stardust.exe`

The open list originally recorded in `ANALYSIS_CHECKPOINT_2026-09-12_PROCEDURAL_TERRAIN_SUBMISSION.md` is stale. Each architectural item has since been addressed:

| Original open item | Current status | Evidence |
|---|---|---|
| identity of virtual-`+8` recipients at `0x0084CAF0` | Closed: three radial flora managers in `+0x1B8`, global/local water managers in `+0x1C8`; never ClientChunk | `ANALYSIS_CHECKPOINT_2026-09-21_TERRAIN_RENDERABLE_DISPATCH.md` |
| five terrain-sorter categories | Closed behaviorally: material-complexity category produced as relevant-material count plus one; not global render phases | `ANALYSIS_CHECKPOINT_2026-09-20_TERRAIN_SORTER_CATEGORIES.md` |
| global/local water primitive generation and selection | Closed architecturally and to concrete manager/primitive classes, WATR registration, global capability selection, local polygon primitives and frustum admission | `ANALYSIS_CHECKPOINT_2026-09-12_WATER_RENDERING.md` |
| chunk creation/LOD/worker-to-main commit | Closed architecturally: ordered request queue, dedicated worker, 20-byte completions, outside-lock build, three-state main-thread commit/reconcile, stale-result destruction and joined shutdown | `ANALYSIS_CHECKPOINT_2026-09-12_TERRAIN_CHUNK_PIPELINE.md` |
| flora culling/instancing | Closed architecturally: static/dynamic radial managers, deterministic family/grid selection, camera-relative residency, individual static appearances, dynamic 500-quad batches and shared 3,000-index buffer | `ANALYSIS_CHECKPOINT_2026-09-12_TERRAIN_LIGHTING_STATIC_FLORA.md`, `WORLD_TERRAIN_ARCHITECTURE.md` |

Historical C++ identifiers for several small interfaces and category labels remain unavailable. They are documentation-label gaps, not missing replacement-client behavior.

The next terrain research should therefore target bounded fidelity questions—collision policy flags/tolerances and exact dirty-region invalidation semantics—rather than repeating the already recovered manager, water, flora, sorter or worker architecture.
