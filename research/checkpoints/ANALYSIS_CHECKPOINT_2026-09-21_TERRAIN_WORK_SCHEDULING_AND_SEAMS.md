# Terrain Work Scheduling and Seam Selection

Target: `C:\Stardust-DEV\Stardust.exe`

This block closes three items left open by the 2026-09-12 terrain LOD analysis: the 20-byte work-record ordering field, the origin of the fixed outer traversal envelope, and the edge-mask transformation used by `ClientChunk` primitives.

## Evidence standard

- **Confirmed by code** means the complete relevant expression or branch was observed in disassembly/decompiler output.
- **Strongly inferred** means the data flow is complete but a historical source identifier is unavailable.
- No historical C++ names in this report are claimed as original symbols.

## Work-record ordering

The 20-byte request/completion record remains:

| Offset | Meaning | Confidence |
|---:|---|---|
| `+0x00` | signed chunk X coordinate promoted to 32 bits | Confirmed |
| `+0x04` | signed chunk Z coordinate promoted to 32 bits | Confirmed |
| `+0x08` | chunk scale/width | Confirmed |
| `+0x0C` | ascending scheduling priority | Confirmed |
| `+0x10` | generated `ClientChunk*`, initially null | Confirmed |

`terrain_lod_create_missing_children_VA_0085E450` stores:

```text
priority = globalFrameCounter + callerOffset
```

The caller offset is produced by truncating a nonnegative squared-distance margin computed by `0x0085E350`/`0x0085E620`. The pending-work wrapper constructed by `0x00860140` copies record `+0x0C` as its comparison key. `0x00860070` inserts smaller keys into the left side of the ordered tree, and the worker consumes the leftmost/begin record. Therefore the field is an **ascending priority**, not a not-before time.

Practical policy: chunks nearest the outer eligibility boundary have smaller margins and are generated first; missing chunks farther inside that boundary receive larger keys. A branch that doubles the margin consequently deprioritizes that request. Duplicate/requeued records in `0x0085F400` advance the key by one while preserving the ordered-container invariant.

The descriptive name `frameRelativeDistancePriority` is provisional. Its unit is a truncated squared-distance-derived scalar plus the frame counter; it must not be treated as wall time.

## Outer traversal envelope

Initialization routine `0x0085DD90` loads the literal IEEE-754 double `2.0`, takes its square root, and stores the single-precision result in global `0x01924C20`. `0x0085EA60` then computes:

```text
outerSquaredEnvelope = (sqrt(2.0f) * 2048.0f)^2
```

The `2048.0f` operand is the read-only value at `0x015E71C4`; it has no code writer. This confirms the binary-local origin of the previously observed 2048 extent and the diagonal `sqrt(2)` expansion. No configuration lookup or per-zone override was found in this data path. Calling it a universal SWG asset-format constraint would still be unsupported; it is confirmed as this executable's procedural-terrain traversal policy.

## Edge-mask-to-index-variant transformation

`terrain_lod_apply_neighbor_stitching_VA_0085E710` maintains four seam bits in node flag bits 3..6 and calls `0x00861870` only when the chunk's current mask differs. That call ultimately reaches `terrain_shader_set_select_seam_indices_VA_00865FF0` for every shader primitive.

The selector owns nine prebuilt 16-bit index lists initialized by `0x00865A80`: variant 0 is ordinary topology and variants 1..8 cover the four stitched edges and four stitched corners. For each primitive, `0x00865FF0` examines the primitive's two integer grid coordinates against `0` and `gridWidth - 1`, combines the boundary position with the four-bit edge mask, chooses a variant, rebuilds its triangle spatial/collision receiver from that variant's index triples, and stores the selected variant pointer at primitive `+0x120`.

The recovered variant mapping is:

| Variant | Boundary condition |
|---:|---|
| 0 | no applicable stitched boundary |
| 1 | second-axis maximum edge |
| 2 | second-axis maximum plus first-axis minimum corner |
| 3 | first-axis minimum edge |
| 4 | second-axis minimum plus first-axis minimum corner |
| 5 | second-axis minimum edge |
| 6 | second-axis minimum plus first-axis maximum corner |
| 7 | first-axis maximum edge |
| 8 | second-axis maximum plus first-axis maximum corner |

The historical compass labels (north/east/south/west) are deliberately not assigned because the local coordinate-to-world orientation has not been proven in this routine. The significant architectural fact is confirmed: the legacy client resolves quadtree cracks by selecting one of nine fixed triangle-index variants; it does not morph vertex positions here.

## Replacement implications

- Preserve the deterministic residency priority semantics for compatibility/performance comparisons, but represent the key with an explicitly sized internal type rather than copying the legacy 32-bit layout.
- Preserve the visible seam topology. A modern renderer may generate equivalent index buffers, skirts, or another crack-free method; the raw legacy pointers and collision receiver are not compatibility surfaces.
- Keep terrain extent policy configurable in the replacement even though this executable hard-codes 2048 and the diagonal multiplier.
- Physics/raycast topology and rendered seam topology should derive from the same selected mesh so the modern client does not recreate visual/collision disagreement.

## Remaining terrain unknowns after this block

1. Complete `ClientChunk` packed vertex/blend payload and GPU declaration.
2. Exact external caller family and gameplay intent for dirty-region regeneration.
3. Historical compass names for the four seam bits (low importance once coordinate orientation is reconstructed elsewhere).

