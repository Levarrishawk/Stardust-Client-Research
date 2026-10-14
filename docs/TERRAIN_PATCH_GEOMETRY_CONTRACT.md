# Terrain patch geometry and Godot inspection contract

## Scope

[TerrainPatchGeometry](../../SWG-Godot-Client/poc/Stardust.Core/Geometry/TerrainPatchGeometry.cs)
owns a supplied nine-vertex CPU cell: positions, normals, raw ARGB words,
scaled base UVs, up to three selected auxiliary UV sets, and an explicit
[accepted topology pattern](TERRAIN_PATCH_TOPOLOGY_CONTRACT.md).
It performs no PTAT/TGEN evaluation, height sampling, neighbor/seam selection,
material lookup, ALP texture binding, or original terrain rendering.

[TerrainPatchVisual](../../SWG-Godot-Client/poc/Stardust.Godot/TerrainPatchVisual.cs) is a separate
owned Godot inspection adapter. The proof renders synthetic heights with exact
topology and an explicit UV/COLOR diagnostic shader. It does not claim original
appearance, lighting, blending, or material parity.

## Direct evidence

Baseline executable SHA-256:
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.
Addresses are VAs. Prior semantic evidence is the
[terrain vertex/blend checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-21_TERRAIN_VERTEX_AND_BLEND_PAYLOAD.md).

| Observation | Interpretation and confidence | Consequence |
| --- | --- | --- |
| Builder `00865DA0` copies 27 words to primitive `+0C`, 27 to `+78`, and nine to `+E4`; stores scale at `+10C` and selectors at `+110`. | Confirmed nine float3 positions/normals, nine raw color words, scale and separate selectors. | Preserve supplied vectors and color bits; do not reinterpret color as blend weights. |
| Writer `0084E270` copies position, normal, and color into separate semantic destinations. Base pairs at `015E69D0` are read with 16-byte stride and multiplied by scale. | Confirmed semantic channels and nine scaled base UVs. | Expose semantic arrays rather than an x86/D3D byte layout. |
| Lookup `00866DB0` reads float2 at `015E7640 + (selector*9 + vertex)*8`; the first four sets match independently checked literal rotations. | Confirmed four supported auxiliary vectors and addressing. | Copy selected auxiliary pairs without applying base scale. |
| Writer derives coordinate count from format bits 8..11 and emits `count-1` auxiliary pairs. | Confirmed shader-dependent channel count; this slice supports at most three. | Do not silently drop unsupported channels. |
| Color pack `00ABA1C0` assembles four bytes by shifts; prior color-conversion/destination tracing establishes ARGB diffuse. | Confirmed packed channel structure; lighting/modulation role strongly inferred. | Preserve raw uint and decode normalized RGBA without gamma conversion. |

Exact base UV order:

`(.5,.5),(0,.5),(0,0),(.5,0),(1,0),(1,.5),(1,1),(.5,1),(0,1)`.

For each base pair `(u,v)`, supported auxiliary selectors are:
0 = `(u,v)`, 1 = `(1-v,u)`, 2 = `(1-u,1-v)`,
3 = `(v,1-u)`. These describe verified bytes, not a guessed mapping from
terrain material layers to selectors. Original malformed/out-of-range selector
behavior is not reproduced.

## Owned CPU API and modern policy

Constructor inputs are read-only spans of exactly nine positions, normals and
uint colors, a scale, pattern index 0..8, optional zero-to-three selectors, and
immutable limits. It copies bounded inputs before validating the owned
vectors/selectors. Every inspection accessor returns a new array; bounds and
other scalar properties are values. Normals remain unchanged in the CPU model.

Positions and normals must have finite components within their respective
caps. Normal squared length, evaluated in double, must be at least `1e-12`.
Scale must be finite, positive and within its cap. Default component caps are
1,000,000 for positions and 4096 for normals; default scale cap is 65536.
Configured caps must be finite, positive and at most `1e9`.
These are explicit modern eligibility policies, not recovered rejection rules.

`CopyPositions/CopyNormals/CopyArgbColors/CopyBaseUvs/CopyIndices` preserve
order. `CopyRgbaColors` maps ARGB to `(R,G,B,A)/255f`, preserving raw words
separately. `CopyAuxiliarySelectors` and `CopyAuxiliaryUvs(setIndex)` preserve
selection order; invalid set indices reject. Storage/work is fixed by nine
vertices, at most three auxiliary sets and 24 indices. Owned array payload is
at most 600 bytes, excluding object/array overhead and shared definitions.
There is no engine, I/O, clock, callback, generator, or mutable published state
in the core type.

## Godot boundary

The adapter validates a live parent and Godot main thread before creating
resources. It supports base UV and at most one auxiliary set (Godot UV2);
additional sets reject before allocation. Its inspection scale cap is 16.
It requires nondegenerate source triangles whose cross products agree with
the sum of supplied normals. This is a deliberately bounded adapter policy.

Positions/normals reflect Z. Original index order is preserved: source CCW
becomes Godot CW under reflection. The adapter explicitly normalizes normals
at the engine boundary; CPU normals remain raw. ARGB-derived RGBA and base UV
are uploaded separately; the one auxiliary set goes to UV2. Readback verifies
cardinality, indices, reflected vectors, color channels and both UV channels.

One instance owns a root, mesh instance, ArrayMesh, shader and material. Its
three resources are disposed after detaching/freeing the node tree and clearing
mesh surfaces. Disposal is owner-thread-only and idempotent. CPU input is an
immutable borrowed geometry value; the parent is borrowed. No shared Main,
project, world adapter or existing launchers are modified.

The inspection shader uses thresholded base UV and COLOR.r with back culling.
Auxiliary UV upload/readback is verified; ALP sampling and original terrain
shaders are not implemented.

## Verification and remaining seam

The [independent oracle](../../SWG-Godot-Client/poc/round32-tests/Rendering/verify_geometry.py)
SHA-gates the original before PE/VA use, checks nine base points, four auxiliary
sets and 30 instruction sites across four ranges. Native fixtures use declared
synthetic heights, 27 pattern/scale/selector cases, and raw color words spanning
alpha/fullbits. All 158 native checks pass, including 103 rejection cases;
output bits, indices, RGBA, UVs and bounds match the independent oracle.

Headless and hidden GPU proofs verify all nine patterns and four auxiliary
selectors, 81 atlas vertices plus nine varied-ARGB vertices, six preflight/
owner rejection cases, and balanced resource disposal. GPU image readback
passes four front UV/color samples and four culled-back samples. Captures and
detailed receipts remain private. Native and Godot focused builds report zero
warnings/errors. The known pre-state certificate-store diagnostic is allowed;
other engine warnings/errors, including object leaks, reject acceptance.

Original vertex sample ordering, seam-mask integration, TGEN generation,
material/ALP program semantics and terrain scene integration remain deferred.
See the [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R32_TERRAIN_PATCH_GEOMETRY.md)
for reproduction and exact next action. No original executable was executed.
