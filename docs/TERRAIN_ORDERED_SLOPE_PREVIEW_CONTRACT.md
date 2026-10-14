# Ordered partial slope-family preview

`OriginalTerrainRegionSource.Decode(bytes, surfacePreview=false, orderedSlope=false)` adds an explicit opt-in ordered shader policy. `orderedSlope=true` implies surface labels and selects `OriginalFourteenShaderLayersModernOrderedSlopePartialLabels`; `OrderedSlopeTexturing` reports the choice. Existing height-only and eight-rule/five-family preview defaults remain selectable and unchanged. This is a partial family-label policy, not complete material traversal, original numeric parity, child-choice RNG or original shader blending.

The source (external historical reference), joined traversal (external historical reference), and sampler (external historical reference) consume the pinned original Tatooine generator SHA-256 `B623B789E5F8486414C1D68CD341005E7E25FD97AF30567E64E7AC848DEB6C32`. The [existing boundary policy](TERRAIN_SURFACE_PREVIEW_CONTRACT.md), [FSLP endpoint/query evidence](TERRAIN_FILTER_SLOPE_CONTRACT.md), [normal producer](TERRAIN_SLOPE_NORMAL_PRODUCER_CONTRACT.md) and [modern normal primitive](TERRAIN_MODERN_SLOPE_NORMAL_GRID_CONTRACT.md) are the compatibility boundaries. The original executable SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150` was rechecked before relying on VAs. No original executable was run.

## Admitted rules and ordering

The new policy retains layers 0,78,87,93,105,129,135,138 and adds only these six enabled slope-shader branches:

| Layer | Parent | Shader family key | Admitted filters |
| --- | --- | --- | --- |
| 79 | 78 | 8 | FSLP0002 |
| 80 | 78 | 1 | FSLP0002 |
| 81 | 78 | 6 | FSLP0002 |
| 88 | 87 | 4 | FSLP0002 |
| 89 | 87 | 1 | FHGT followed by FSLP0002 |
| 133 | 128 | 6 | FSLP0002 |

The sampler exposes source-order layers `0,78,79,80,81,87,88,89,93,105,129,133,135,138`, seven distinct keys `1,3,4,5,6,8,9`, and dynamically computed deferred shader layers. There are 40 deferred ASCN layers; the former six deferred slope-shader branches are now admitted. Other filter kinds/occurrences remain deferred. Layer/ancestor version 0003, enabled headers, exact zero ADTA words, boundary kinds/counts, enabled ASCN0001 keys/clamps and known local filters are validated against the pinned generator. FSLP admission requires 0002, finite endpoints, selector word0 and amount word0. Layer89's enabled finite FHGT is queried from current heights at that layer entry.

Traversal includes selected shader-only subtrees which height-only traversal previously omitted. A demanding admitted layer prepares its normal cache before boundary/filter work. Child admission follows any positive local postfilter influence, rather than requiring positive incoming product. Parent shader products remain distinct from local influence. Zero incoming product therefore does not masquerade as zero local influence.

Shader weights use the existing retained boundary queries through `TerrainLayerInfluence`, including retained polyline shaping and retained FHGT query shaping. Its explicit existing 64-significand combine/product profile remains the established boundary-preview profile. The legacy height-weight path is retained separately so geometry arithmetic is not switched. An entirely zero-product height batch retains the legacy geometry cull, including signed-zero height words, while the processed affector still marks the filter cache dirty. This modern geometry-preservation choice does not claim original zero-weight numeric output parity.

Each processed enabled AHCN/AHFR/AHTR/AROA batch marks dirty even if height words do not change. The target's respective virtual+14 predicates return true (`00B76640`, `00B76980`, `00B771A0`, `00B87DF0`); these are supported driver dirty predicates. Diagnostics use a logical revision per processed affector batch, not a recovered original revision counter. Later height writers and road heights do not alter earlier immutable query snapshots.

## Explicit modern numerical choices

TerrainModernSlopeFilter (external historical reference) uses policy `ModernBinary64LiteralComplementSineF32SlopeCache`. It computes P*s then *Q with binary64 operations, spills the angle argument F32, clamps it against literal C (`3FC90FDB`), evaluates `Math.Sin(C-angle)` in binary64 and stores the cache F32. P/Q are target literal words `40490FDB`/`3BB60B61`. It does not substitute cosine. The synthetic30-degree cache is `3F5DB3D8`, distinguishing the one-ULP cosine substitution. Cached endpoint comparisons are strict; unfeathered selector0/amount0 yields only0 or1. Nonfinite values and unsupported selectors/amounts reject.

TerrainModernSlopeCacheLifecycle (external historical reference) starts dirty, rebuilds on the first demanding entry after a writer, and shares a generation across untouched siblings. It uses `TerrainModernSlopeNormalGrid.Build(width,spacingBits,origin,currentHeightBits)` directly. It does not use the Binary53 coordinate adapter or replace the supplied geometry's Binary24 coordinates. Cache snapshots report generated X/Z axes and the count of differences from actual geometry axes. An unmarked height mutation rejects as stale.

The normal policy remains `ModernBinary64QueryTimeTriangleNormals`, including target-backed diagonal, F32 stores, finite edges and literal short-vector threshold. The endpoint sine, normal square root and binary64 basic arithmetic are modern choices. Effective original controls and original output bits remain unknown; all numeric/blend/RNG parity flags remain false.

## Immutable receipts and bounds

`chunk.SurfacePreview.OrderedSlopeDiagnostics` is present only for ordered policy. Diagnostics (external historical reference) expose:

- `NormalCacheBuilds`: generation, logical height revision, triggering layer, width/spacing/origin, owned pre-query height words, generated X/Z axes, position mismatch count and policies.
- `SlopeLayerQueries`: layer, cache generation/revision, raw filter input and angle/cache words, shared pre-query height snapshot, incoming/local/product influence words, diagnostic normalY/slope values and optional FHGT influence values.
- `ShaderLayerQueries`: all visited selected shader layers' incoming/local/product words and entry height revision.
- `HeightWriteEvents`: layer, rule offset, revision before/after and dirty=true.

Height snapshots are owned once per cache generation and shared by its query receipts, not cloned per sibling. NormalY words are diagnostic comparisons; an independent verifier must recompute them from the snapshot heights and raw position parameters. Geometry/current-height outputs and final family keys/indices/owners use their existing APIs. Compose rejects mixed old/new policy receipts and inconsistent diagnostic shapes.

The ordered grid cap is32 samples per axis/1024 cells. Cache generation cap is6, slope query occurrences at most6, shader occurrences at most14, writer revisions at most4096; pinned input structure bounds traversal. Existing boundary/fractal/road/normal operation limits remain enforced. Cache controller checks cancellation on entry, comparison, build and publication; traversal checks layer/cell boundaries and existing numeric kernels. No partial result is published after failure.

## Focused verification and limits

The console harness (external historical reference) requires no private input for synthetic endpoint, dirty/shared-cache, stale, late-height/FHGT, ownership, cancellation and caps tests. Optional original mode verifies input SHA and compares default/ordered height words and mixed-policy composition failures. It separately reconstructs filter-free retained boundary/local/product weights using public query components.

Original probes establish tangible source assignments: Tusken chunk(-101,154) changes175/196 labels through133; Junland2×2(-90,-28) changes751/784, with final families1=329/6=422/8=33; Arch Valley2×2(22,112) changes781/784, with4=447/1=334/3=3. All measured cache position mismatches are zero. Native default/ordered height words are bit-identical across these probes. The independent ordered verifier (external historical reference) reconstructs all1764 label samples and nine normal-cache snapshots, including all six branches and FHGT89, from supplied query-time height words. It establishes1707 changed labels independently. Full original height-generation parity is not established by a supplied-height oracle.

GPU rendering and packaged acceptance are separate adapter work. The subsequent
[joined checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-13_ORDERED_SLOPE_TEXTURING.md)
records the accepted21-check DX12 package and its independent source/normal/
shader/far replay. This Core contract does not establish original rendering or
numeric parity. No additional shader/filter branch should be admitted without
its own evidence boundary.
