# Analysis checkpoint: terrain LOD, height bias and cache policy

Date: 2026-09-12  
Primary subject: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Outcome

This block recovers the procedural-terrain level-of-detail equation, its configuration defaults, quadtree transition behavior, camera-update cadence, work-record field layout and the distinction between normal LOD caching and dirty-region regeneration.

Names are stable provisional names. Decompiler output remains reconstructed pseudocode, not original source.

## Configuration and derived values

`ConfigClientTerrain` initialization at `0x00844D90` reads these deployed defaults:

| Key | Default | LOD-controller use |
|---|---:|---|
| `threshold` | `1.0f` | Distance scale |
| `highLevelOfDetailThreshold` | `3.5f` | Fine-detail radius in base-chunk widths |
| `heightBiasDisabled` | `true` | Inverted to `useHeightBias`; height bias is disabled by default |
| `heightBiasFactor` | `8.0f` | Converts vertical relief into a bias quantum |
| `heightBiasMax` | `50` | Caps relief bias relative to node scale |
| `terrainMultiThreaded` | `true` | Enables the worker path recovered in the preceding checkpoint |

The local `C:\Stardust-DEV` CFG/INI files do not override these keys.

Controller setters derive:

```text
inverseDistanceScale = 1 / (baseChunkWidth * threshold)
heightBiasQuantum    = ceil(baseChunkWidth * heightBiasFactor)
fineDetailRadius     = baseChunkWidth * highLevelOfDetailThreshold + epsilon
fineDetailRadiusSq   = fineDetailRadius * fineDetailRadius
```

Evidence: `0x0085E1C0`, `0x0085E220`, `0x0085E260`; epsilon is the constant at `0x015E71C0` (approximately `1/81`). Configuration changes set a controller-dirty byte so LOD is recomputed.

## Exact distance decision

`terrain_lod_compute_target_scale_VA_0085E350` obtains the observer world position and asks node routine `0x00B608B0` for squared distance from that point to the node's axis-aligned bounds. Its broad decision is:

```text
if node is outside the controller's outer squared-distance envelope:
    mark outside and return current node scale

distanceBeyondFineRadius = max(0, sqrt(distanceSquared) - fineDetailRadius)
targetScale = max(1,
    ceil(distanceBeyondFineRadius * inverseDistanceScale)
    - optionalHeightBias
    + 1)
```

The first branch and returned values are confirmed. “Outer envelope” is a behavioral name; its precise public configuration source is not established. During a traversal, `0x0085EA60` sets it to `(sqrt(2) * 2048)^2`, using constants at `0x015DC810` and `0x015E71C4`.

When enabled, relief bias uses the node's vertical range (`maxY-minY`), the derived height quantum, `heightBiasMax`, and node scale. The exact integer sequence is recovered, but its designer-facing unit is not confidently named:

```text
reliefQuanta = trunc(ceil(maxY - minY) / heightBiasQuantum)
capQuanta    = trunc(nodeScale * heightBiasMax / 100)
optionalHeightBias = min(reliefQuanta, capQuanta) * nodeScale
```

Height bias only applies to nodes with valid vertical bounds and is disabled by deployed default. Its effect is to retain finer subdivision in vertically complex terrain.

## Quadtree transitions and seams

- LOD nodes store signed 16-bit X, Z and scale fields at `+0`, `+2`, `+4`, four child pointers at `+0x10`, and a resident chunk at `+0x0C`.
- `terrain_lod_update_node_VA_0085E620` compares computed target scale with node scale.
- When finer detail is required, `terrain_lod_create_missing_children_VA_0085E450` creates/describes up to four half-scale children and returns the number of missing chunk requests.
- When coarse detail suffices, the parent is activated, descendants are recursively deactivated, and child nodes remain allocated.
- `terrain_lod_apply_neighbor_stitching_VA_0085E710` compares the active state of four neighbors with four per-edge flags and calls `0x00861870` when the seam mask changes. This propagates an edge mask to every render primitive in the ClientChunk.
- Active/inactive chunk visibility is implemented by `0x00862980` and `0x00862A10`, which add/remove the chunk's render-world object. Deactivation does not destroy the chunk.

The four-edge mask is therefore a confirmed crack/stitching contract. The exact vertex/index variant selected by `0x00865FF0` remains to be decoded.

## Camera cadence and hysteresis

Appearance routine `0x0084A960` floors the observer's world X/Y/Z values and reruns LOD when any integer coordinate changes, when controller configuration is dirty, or when the prior traversal was incomplete. While dirty-region regeneration is active, ordinary camera LOD evaluation is suppressed.

No separate pair of split/merge distance thresholds was observed. Stability comes from integer-position gating, retained nodes/chunks, active-edge state, and the `+1`/rounding behavior in the target-scale equation. Calling this classical distance hysteresis would be unsupported.

## Work records

The 20-byte request/completion record can now be labeled:

| Offset | Meaning | Confidence |
|---:|---|---|
| `+0x00` | signed chunk X coordinate promoted to 32-bit | Confirmed |
| `+0x04` | signed chunk Z coordinate promoted to 32-bit | Confirmed |
| `+0x08` | chunk scale/width | Confirmed |
| `+0x0C` | frame-relative scheduling/order value (`global frame counter + caller offset`) | Confirmed expression; exact policy label tentative |
| `+0x10` | generated `ClientChunk*`, zero before completion | Confirmed |

`0x00754D80` returns global frame counter `0x01922E68`. `0x0085E450` adds its caller-supplied offset to that value when constructing requests. The ordered pending container uses the record in its comparison/insertion path, but whether `+0x0C` represents urgency, age or a not-before frame remains **Tentative**.

## Cache and dirty-region policy

Normal observer-driven LOD does not show distance eviction. Coarsening unregisters render objects and preserves resident ClientChunks and quadtree descendants for reuse. This is **Strongly inferred** from the complete LOD transition path: it invokes activation/deactivation and recursive state changes, not chunk destruction.

The 16-byte queue at appearance `+0x1F8` is a different mechanism. `0x0085F820` is a late virtual on the concrete terrain appearance and receives an axis-aligned X/Z region. `0x0085F950` processes its newest region, cancels pending work, finds resident nodes intersecting that area, and enters the drain/rebuild/reconcile state machine. This is best named a **dirty-region regeneration path**, not a camera-region transition. Exact caller semantics remain strongly inferred because the call is virtual.

Chunk destruction occurs on stale/duplicate completion, dirty-region replacement/drain, or complete terrain teardown. No independent time/LRU/memory-pressure eviction routine was established in this block.

## Replacement consequences

- Preserve deterministic X/Z/scale identity and edge-stitch relationships; do not preserve raw node or chunk layouts.
- Implement LOD policy as data/configurable equations, including the original default profile for compatibility testing.
- Retain a dirty-region invalidation API separate from camera-driven residency.
- A modern client may use bounded LRU eviction, but must recreate a chunk deterministically and must not confuse eviction with terrain modification.
- Use skirts, geomorphing, fixed index variants or another modern seam solution only if it reproduces compatible visible topology. Original four-edge masks are useful reference behavior, not a mandatory implementation.
- Treat the legacy 2048 outer constant as a recovered policy value requiring multi-zone validation, not a universal asset-format constant.

## Remaining unknowns

1. Exact semantic unit and ordering direction of work-record field `+0x0C`.
2. Exact public origin of the fixed 2048 traversal envelope and whether all zones use it.
3. Edge-mask-to-index/vertex transformation in `0x00865FF0`.
4. ClientChunk CPU/GPU payload layout and terrain blend construction.
5. WMAP/SMAP consumers.
6. Dirty-region callers and their connection to runtime terrain modifications.

## Progress estimate

- Expanded comprehensive whole-client research: **58%**.
- Replacement-relevant behavioral/specification readiness: **79%**.
- Renderer workstream: **63%**.
- Terrain/world/collision/camera/transitions workstream: **46%**.

These are replacement-relevant evidence estimates, not raw-function naming percentages.
