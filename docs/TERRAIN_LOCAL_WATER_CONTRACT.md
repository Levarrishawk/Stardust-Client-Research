# Pinned Tatooine local water and modern clipped geometry

OriginalTatooineLocalWater (external historical reference)
admits four authored ponds from the pinned original Tatooine terrain. It leaves
the neutral [BPOL codec](TERRAIN_BOUNDARY_POLYGON_INPUT_CONTRACT.md) unchanged.
TerrainModernLocalWaterPolygon (external historical reference)
interprets its existing extension fields only under the supported local-water
profile and constructs explicit modern geometry. Full shader execution, legacy
tessellation, reflection, water physics and swimming are outside this contract.

## Direct evidence and confidence

The target executable SHA256 is
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.
Its hash was checked before these VA observations. The existing
[water rendering](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-12_WATER_RENDERING.md)
and [local geometry/material](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-21_LOCAL_WATER_AND_SHADER_CONTRACT.md)
research establishes terrain-owned bounded local primitives, winding correction,
shader resolution and WATR registration.

**Confirmed target observations:** consumer0084AE30 checks boundary enable
through00B62F20, boundary type2 and flag getter00B73C60. The latter loads byte+38.
It copies ordered point pairs from+20/count+24. At0084AF95/AF98/AFA1 it loads
+3C/+40/+44, resolves the supplied name through00B94D50 and calls008637D0
at0084AFFD. At00863922/29 that callee loads second/first pair components;
00863931/34/37 stores first/passed-height/second into successive XYZ words.
It constructs the local primitive through00862CA0 at008639CA. Combined with
the independently established BPOL0005 loader stores, these close the flag,
height and authored point association. The shader-size/name roles are supported
by the caller/primitive chain, installed resources and prior material evidence;
no NGE-only field interpretation is introduced.

Pinned NGE commit `949451032647e45e42c3aaef3f41b132c8af36e3`,
`src/engine/client/library/clientTerrain/src/shared/appearance/ClientProceduralTerrainAppearance.cpp`,
`buildLocalWaterTable`, and sharedTerrain `src/shared/generator/Boundary.cpp`,
`BoundaryPolygon::load_0005`, corroborate this chain. They are supporting
correlation rather than the compatibility authority. In particular, modern
two-triangle quads and clipping do not reproduce the target's adaptive subdivision.

## Original occurrence and ownership

`Bind(OriginalTerrainRegionSource source, limits, cancellation)` validates TRN
SHA256 `6039D0C15F9CCB518D72AE01A1ABDF41FCFF2153B5CBF7EB3BA6E6C634A3A427`
and TGEN SHA256
`B623B789E5F8486414C1D68CD341005E7E25FD97AF30567E64E7AC848DEB6C32`.
`Decode(terrainBytes, limits, cancellation)` additionally checks byte budget and
TRN identity before decoding. Tatooine's PTAT0014 global water flag is zero:
no global plane is invented. The local layer chain0→53(Oasis)→56(Water!?!)
is locally enabled, LAYR0003/IHDR0001, with zero ADTA words. Layer56 has exactly
four BPOL0005 records, ordinal2..5, extension flag1, shader size2 and logical
shader `wter_dsrt_bright`. The record offsets below are relative to TGEN;
the original file's complete TGEN starts at210.

| Ordinal | TGEN offset | X bounds | Z bounds | Water Y |
|---|---:|---|---|---:|
|2|42573|−5330..−5168|2675..2827|0|
|3|42730|6640..6714.25|5417.5..5496.75|1|
|4|42887|6109..6396|−533..−240|0|
|5|43044|1744..1897.5|−6169..−6010.5|0|

`Polygons` is a read-only list of `TerrainModernLocalWaterPolygon`.
`Occurrences` additionally exposes layer index, child ordinal and section offset.
Each polygon retains its exact immutable `SourceInput`, shader path, height,
shader size, original bounds and current geometry bounds. Position/UV/index
exports are detached arrays. The original point sequence remains in SourceInput;
clipping never replaces that provenance. The collection owns no GPU resource,
worker or source archive and never evaluates terrain height itself.

The actual shader resolves as `shader/wter_dsrt_bright.sht`, priority38,
672 bytes, SHA256
`80D46EC3D3BF4FA5CD8F1BFDBCAC7C5928C836332ECA04FB6CC2B37956F125CC`.
It is SSHT0001 and references `effect\water.eft`, with MFFP/ENVM/_SKY/CUBE/MAIN/NRML
bindings. MAIN is `texture/wter_aqua.dds`, priority37,87536 bytes,256×256 DXT5,
SHA256 `4F187C6A25DCE89CCC74BAF9D95BB8EDB1E4ABBEC71D8FA21E53B9CB949CE043`.
The existing supplied MAIN loader (external historical reference)
is the bounded texture boundary; texture/effect execution belongs to the adapter.
The policy name `ModernSuppliedWaterMainTextureWorldXZ` explicitly omits original
normal/environment animation and effect execution. These asset identities and
fields are observations; original reflective appearance is not claimed.

## Explicit modern geometry and clipping

`TerrainModernLocalWaterPolygon.Build(input, limits, cancellation)` requires
enabled BPOL0005/IHDR0001, a nonzero water extension flag, exactly four finite
ordered points, finite bounded height, positive finite shader size and a bounded
ASCII logical shader name. It requires strict convexity and nonzero successive
turns; repeated, collinear, nonconvex and self-intersecting quads reject. Shader
paths normalize slash direction and add the shader namespace/extension;
empty segments, traversal and colon paths reject. Paths must be canonical
under the existing LegacyAssetPath lookup convention; uppercase or
other noncanonical completed paths reject. Stored clipped UVs are checked for
finite binary32 values before receipt construction completes. This domain is
a modern stricter policy, not recovered original malformed-input behavior.

`Clip(minimumX, minimumZ, maximumX, maximumZ, limits, cancellation)` returns
null for an empty or zero-area intersection and the same receipt for full
containment. Otherwise fixed minX/maxX/minZ/maxZ Sutherland-Hodgman clipping
uses binary64 intersection arithmetic and stores new coordinates as binary32.
The clipping plane component is exactly the supplied binary32 boundary.
Adjacent/closing duplicates and collinear stored points are removed; results
with fewer than three vertices are empty. A clipped result preserves original
input/bounds and height. The caller clips to the currently displayed ground
window before publication, preventing water geometry outside admitted ground.

Both original winding directions yield up-facing fan triangles. The fan uses
vertex0 and each successive pair, swapping their order when necessary. UVs
are world `(X/shaderSize,Z/shaderSize)`, binary64 division then binary32 store;
they remain anchored across windows. The explicit geometry policy is
`ModernConvexWaterPolygonClippedFan`. It is independent of terrain's5m sample
spacing and does not modify terrain geometry, heights or grounding.

Hard geometry ceilings are eight vertices/six triangles per polygon; original
collection cap is four polygons. Build reserves64 work units and Clip256;
options can lower these limits but cannot exceed fixed ceilings. Default
coordinate/height cap is1,000,000, shader-size cap65536, shader-name128 bytes
and source input2MiB. Invalid options throw ArgumentOutOfRangeException,
unsupported input/domain/work throws InvalidDataException, null input throws
ArgumentNullException and pre/call-loop cancellation throws OperationCanceledException.
No partial receipt is published after failure. Reservations are bounded policy
counts, not measured total heap usage or original scheduler controls.

## Validation and visible-water prerequisite

Focused tests (external historical reference) use authored byte
fixtures and independently recorded geometry bounds. They cover both windings,
source/export ownership, logical names, full/partial/repeated/no-hit/tangent clips,
a diamond producing eight vertices/six triangles, malformed/truncated input,
disabled/inactive/unknown records, invalid count/order, nonconvex/self-crossing/
nonfinite/degenerate geometry, invalid height/size/name, lowered caps and cancellation.
The synthetic gate passes270 assertions; optional pinned original gate311.
The the separate implementation record
separately passes all24 original receipt clips with zero ULP differences.
Its mutation tests (external historical reference)
pass16 cases. This closes the original CPU byte/geometry comparison; the
separate host proof is recorded by the parent integration task.

The optional original gate builds the actual320m region(-135,65),8×8 chunks,
and queries the authored first-pond centroid(-5242.25,2765.5). Displayed terrain
there is−0.515142560005188m while authored water Y is0, establishing positive
water depth at that sample. This is one actual generated terrain query, not a
claim that every pond pixel is submerged or that modern terrain has x87 parity.
The original private receipt retains all four full meshes and24 bounded clips.

```powershell
dotnet restore poc/terrain-local-water-tests/TerrainLocalWaterTests.csproj --configfile poc/terrain-local-water-tests/NuGet.Config
dotnet run --no-restore --project poc/terrain-local-water-tests/TerrainLocalWaterTests.csproj
```

For original mode pass exactly two app arguments: explicit original TRN path
and a private JSON receipt path. Use isolated artifacts during concurrent
builds. Actual texture decode, visible pond capture, current-window replacement
and balanced GPU disposal remain host acceptance. No original process or
network execution is required or performed by these core tests.

The synthetic focused suite is required as `terrain-local-water` in the
offline regression runner (external historical reference), without original inputs.


## Accepted offline DX12 host (October 13)

The opt-in `--terrain-local-water` host consumes these four original occurrences,
clips them to the currently published visible terrain window, and uses the original
MAIN texture with the explicitly modern `ModernStaticMainTextureWaterAlpha055`
policy. Constant opacity0.55 does not reproduce the original water program.
The first pond at window(-135,65),8×8 chunks, is visibly present above generated
terrain. Independent original-byte/Fraction replay checks five streamed windows,
21 clipped vertices and11 triangles with zero ULP differences. A synthetic
failure after preparing a successor water visual retains the previous terrain
and water; all candidate resources are disposed.

The frozen self-contained water-enabled x64/DX12 package passes17 acceptance
checks on RTX4070SUPER:3170 motion positions over101.4447m, four successor
windows, exact original material association, water clipping, cancellation,
staged cancellation, reset and both rollback gates. All1536 fine visuals,
five water visuals and both shared material bindings are disposed; peak water
ownership is two visuals, below the explicit current-plus-candidate limit8.
All31 default offline regression suites pass. The independent synthetic water
GPU gate checks14 pixels across transparency, occlusion and backface images and
rejects six receipt/image mutations.

The eight-patch upload budget governs fine terrain operations. Preparing up to
four clipped water meshes is separate bounded commit work, included in owner
wall time; neither that work nor the soft2ms start deadline establishes a hard
frame-time limit. Post-publication wall intervals in the packaged run were
13.46..29.29ms, with an802ms maximum startup/capture interval. Full frame pacing,
physical input, swimming, waves, reflection/refraction and original water parity
remain unverified. Export water-rollback acceptance uses the known pond window
so that it cannot pass by testing an empty water window.


When modern far view is enabled, the visible clipping domain is the640m outer
source region. Without far view it remains the fine region. The receipt names
`PublishedFarOuterBounds` or `PublishedFineBounds`; the independent verifier
reconstructs bounds from signed request coordinates using mathematical floor.
It does not trust copied output bounds. QA found an artificial19.4444m² cut in
the pond when generation4's fine minimumZ2680 cut its authored minimumZ2675;
that cut lay inside the displayed far terrain. The host now prepares the water
candidate against the same visible outer extent, retaining the four-occurrence,
eight-live-visual limits and unchanged fine-only grounding. The corrected combined package now passes20 frozen acceptance checks: four
water windows preserve the complete pond with16 vertices/eight triangles and
zero ULP differences. Earlier fine-only water acceptance is unchanged. See the
[joined checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-13_FAR_TERRAIN_AND_WATER.md).
