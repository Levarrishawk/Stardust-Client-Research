# Opt-in modern seven-family terrain interpolation

TerrainModernSevenFamilyBlend (external historical reference)
and the prototype host adapters admit the explicit family set 1/3/4/5/6/8/9. This is modern
vertex interpolation of ordered partial labels. Original slope arithmetic,
child texture choice, ALP blending and complete material traversal parity are
false. The [query-time normal contract](TERRAIN_MODERN_SLOPE_NORMAL_GRID_CONTRACT.md)
remains separate from render normals.

## Explicit channels and defaults

`Bind(geometry, nineFamilyKeys, cancellation)` retains the exact immutable
geometry and owns detached labels/attributes. Positions, normals, COLOR, UV,
auxiliary UVs and indices remain unchanged. The channel order is:

| GPU field | Families |
| --- | --- |
| CUSTOM0 RGBA_FLOAT | 1,3,4,5 |
| CUSTOM1 RG_FLOAT | 6,8 |
| Remaining weight `1-sum(CUSTOM0)-sum(CUSTOM1.xy)` | 9 |

Every source vertex has exactly one family contribution. Triangle interpolation
produces the modern blend. `CopyWeights()` returns four components;
`CopyExtraWeights()` returns two. All accessors detach storage. `Encode(keys)`
admits 1..4096 labels with per-label cancellation; unknown keys and invalid
counts reject. The focused Core suite (external historical reference)
passes 6,227 checks and 219 independently rational barycentric samples, including
all seven families, negative-X adjacent seams, reversed triangle orientation,
quarter 3/9 contributions, geometry preservation, ownership, malformed inputs,
exact/oversized caps and pre-cancellation.

Existing [five-family helper and GPU defaults](TERRAIN_FIVE_FAMILY_GPU_BLEND_CONTRACT.md)
remain selected unless explicitly opted in. Render policy enum values 0..5 are
unchanged; seven-family production/diagnostic values are 6/7.
`TerrainPatchBlendMaterialBinding(materials, textures, orderedSlope:false)`
still requires exactly 3/5/6/8/9. With true it requires exactly 1/3/4/5/6/8/9.
The binding borrows textures, owns one shader/material and refuses disposal
while fine/far visuals borrow it. Numeric diffuse uniforms remain Vector4;
production retains declared sRGB texture hints, repeat and anisotropic mips.
The fragment sums sampled texture times diffuse multiplier times family
contribution, then multiplies by interpolated vertex COLOR.

`TerrainModernFarTerrainView.Build(..., cancellation, orderedSlope:false)` and
`BuildSupplied` preserve default five-family rejection of 1/4. Explicit true
keeps all stitching/topology/normal guards and selects the same seven-family
layout. Mesh `OrderedSlope` identifies its layout; default `CopyExtraWeights()`
is empty. The far adapter requires exact mesh/binding policy agreement and
reads back both formats and every component bit. It keeps the existing bounds,
fine-edge copies, winding, position/normal/COLOR/UV/index paths and grounding
authority. The extra retained array is 1,920 two-float entries for the current
bounded ring. There is no extra chunk cohort, worker or texture owner.

## Host and original binding boundary

Developer CLI `--terrain-ordered-slope` follows `--terrain-modern-blend` and
requires an explicit terrain region plus modern blending. Export option
`--terrain-ordered-slope` requires `--modern-blend`; it adds feature
`stardust_terrain_ordered_slope` and build receipt `OrderedSlopeTexturing=true`.
Host Source.Decode receives `orderedSlope:true`; this selects
`OriginalFourteenShaderLayersModernOrderedSlopePartialLabels`. Default source
and five-family rendering remain unchanged.

The host loads first SGRP entries through the existing bounded supplied-material
reader, uploads their actual MAIN textures, and includes shader/texture source
path, archive priority, byte count and SHA256 identities in
`SurfacePreview.Bindings`. Direct pinned Tatooine SGRP0006 bytes associate the
two added families with `shader/rock_gravel_small.sht` (1) and
`shader/rock_cliffcracked_brngrey.sht` (4). First-entry selection is modern;
original random child selection is not recovered. No guessed rock texture is
substituted. Actual original geographic rendering/export acceptance remains a
separate parent-host gate.

Association input is the existing pinned Tatooine TGEN SHA256
`B623B789E5F8486414C1D68CD341005E7E25FD97AF30567E64E7AC848DEB6C32`.
The existing independent SGRP reader confirms those first-entry strings; this
is original-byte association evidence, not a shader-execution claim.

Proof files `terrain-ordered-slope.json` and
`terrain-ordered-slope-{generation}.json` carry FineChunks and optional
CoarseChunks, indexed consistently with patch source ChunkIndex. They retain
layer/position axes, final heights, all labels/owners, query-time height/cache
snapshots, slope/height/shader influences and write revisions. Cache origins
are explicit XYZ arrays. Geometry receipts retain four BlendWeights plus two
ExtraBlendWeights in ordered mode. Render normals are never substituted for
the diagnostic query-time cache.

Only the ordered streaming proof extends synthetic motion to28 seconds, with
walk/run/idle captures at0.6/18.6/29.8 seconds. Five-family and nonstream proof
schedules retain their previous values; ordinary gameplay is unchanged.
`terrain_proof_schedule` stdout and `ProofSchedule` metadata record the selected
schedule. The minimum three successor publications, reused-chunk requirement,
motion and resource gates remain unchanged. Longer proof time accommodates the
new ordered CPU work without claiming frame-pacing acceptance.

## Focused GPU acceptance

The standalone scene (external historical reference)
builds the actual bounded synthetic stitched ring at negative coordinates,
one adjoining fine patch, and seven pure-family patches. Its independent
barycentric reference accumulates supplied family labels rather than sampling
the uploaded channels. Nonwhite numeric diffuse modulation, the1/4 transition,
asymmetric UV repeat and back culling have distinct pixels.

The current acceptance receipt includes direct live ArrayMesh exports:
UploadedFarChannels and eight UploadedFineChannels entries, each Custom0,
Custom1 and Format. This is read back before cleanup. Actual x64/DX12
Forward+ on RTX4070 SUPER passes 912 checks and 10 expected rejections:
24 pixels, 11,952 custom components, 1,920 far vertices/1,728 triangles,
256 distinct fine-edge samples in 292 copies, and balanced 1 far/8 fine/1
shared-binding allocation/disposal. Maximum RGB error is 0.0020514727, below
0.015. Texture/modulation tamper, stale/default policy, malformed labels,
cancellation and disposal with live borrowers reject. Headless passes 883
attribute/ownership checks without claiming pixels.

The independent checker (external historical reference)
passes 24 PNG-bound pixels,11,952 uploaded components and 12 negative mutations,
including both live custom arrays, source labels/weights, texture data,
cleanup and false parity claims. Fresh private uploaded-array acceptance
supersedes the earlier receipt without discarding it. Local engine cache-path
and certificate-store diagnostics remain visible in stdout; no shader,
renderer-admission or attribute-readback failure occurred.

Five-family regression also passes its original 36 checks/7 rejections,
14 independent PNG pixels and 252 component bits. Far regression passes 902
checks/4 rejections,17 independent PNG pixels and 7,680 components. Independent
negative mutations remain 6 and 7 respectively.

```powershell
dotnet restore poc/terrain-seven-family-blend-tests/TerrainSevenFamilyBlendTests.csproj --configfile poc/terrain-seven-family-blend-tests/NuGet.Config
dotnet run --no-restore --project poc/terrain-seven-family-blend-tests/TerrainSevenFamilyBlendTests.csproj
python tools/python/verify_terrain_seven_family_blend_gpu.py --receipt <private-json> --stdout <private-stdout>
```

The scene requires the pinned local prototype host 4.7.2 .NET executable and actual
`--rendering-driver d3d12 --rendering-method forward_plus`; its four captures
use `.png`, `.pure.png`, `.uv.png` and `.back.png`. Original slope numerical
parity, shader execution parity and full-frame performance remain unclaimed.
