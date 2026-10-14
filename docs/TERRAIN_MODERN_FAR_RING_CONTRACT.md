# Explicit modern stitched far terrain ring

[TerrainModernFarTerrainView](../../SWG-Godot-Client/poc/Stardust.Core/Geometry/TerrainModernFarTerrainView.cs)
builds a640m view around the existing320m fine ground window. The policy is
`Modern640MetreFarRingWithExactFineBoundary`, an explicit modern rendering
choice. It does not recover original LOD scheduling, simplify the fine height
grid, provide a new grounding surface or claim original normal/shader parity.

```csharp
TerrainModernFarRingMesh Build(OriginalTerrainRegion fine,
                              OriginalTerrainRegion coarse,
                              CancellationToken cancellation = default);
```

The fine region has64 width40 chunks and5m sample spacing. The coarse region
has16 width160 chunks and20m spacing. Both have matching terrain/TGEN identities,
owned source receipts and actual partial material labels. Their source generation
is unchanged. There are1,024 fine10m fans and256 coarse40m fans. The320m rectangle
must sit on coarse40m patch edges, strictly inside the640m rectangle with at least
40m outer margin. Uneven margins are supported. Malformed layouts, inconsistent
shared source heights/normals/colors/families or missing labels reject.

`BuildSupplied(finePatches, coarsePatches, limits, cancellation)` is the explicit
authored-input seam for fixtures. Each `TerrainModernFarTerrainPatch` supplies
its immutable geometry, exact nine categorical keys and source chunk/patch IDs.
It has the same fixed topology/domain and marks provenance synthetic-supplied.
It is not a route to claim original generation from authored heights.

## Topology, channels and texture orientation

The builder omits64 coarse fans wholly inside the fine320m rectangle. Each of
the192 remaining fans keeps its actual coarse center and coarse perimeter
vertices. On each of32 hole-adjacent40m edges, its two20m segments become eight
5m segments using actual fine vertices. This adds192 vertex occurrences and192
triangles:1,920 vertices and1,728 triangles total. There is no overlay, skirt,
height offset or interpolated guess for missing fine samples.

Every vertex on the fine hole boundary, including diagonal-touching corners,
copies exact fine position, normal, packed ARGB and family label. Other vertices
keep actual coarse values. Fine colors/labels are also checked across duplicate
source geometry occurrences. Categorical keys3/5/6/8/9 become the established
[modern four-channel/fifth-residual weights](TERRAIN_MODERN_FIVE_FAMILY_BLEND_CONTRACT.md).
All exported channels and indices are owned/detached; failed construction
publishes no partial mesh. Coarse provenance stores source identities and
resolution metadata; the mesh does not itself retain input regions.

Actual fine geometry vertices2/4/8 and their actual base UVs determine the UV
basis. It is verified on every fine patch, including its10m period and consistent
phase. In the current assembly U increases with X while V decreases with Z:
vertex2 is lowX/highZ and UV(0,0), vertex8 is lowX/lowZ and UV(0,1). Merely checking
modulo phase at5m samples cannot distinguish the opposite V orientation; basis
derivation is therefore required. Far UVs use this affine world-coordinate frame,
so they agree with fine UVs modulo integer texture repeat and retain orientation
between vertices. Coarse40m UV scaling is not copied into the far ring.

Each emitted triangle must have positive upward projected area and finite
positive dot product between its face vector and the sum of its three preserved
normals. No normal repair changes the fine edge or coarse channels. An unsupported
transition profile fails explicitly. `MinimumAverageFaceDot` reports the minimum
unnormalized dot product; it is a coherence check, not an angle in degrees.

## Output API and bounded ownership

The mesh exposes `FineBounds`/`OuterBounds` (MinimumX/MinimumZ/MaximumX/MaximumZ),
`CoarseProvenance`, `Fans`, `VertexCount`, `TriangleCount`, `MinimumAverageFaceDot`
and the derived `UvOriginWorld`, `UvOrigin`, `UvXStep`, `UvZStep`.
`CopyPositions`, `CopyNormals`, `CopyArgbColors`, `CopyFamilyKeys`, `CopyUvs`,
`CopyWeights` and `CopyIndices` return separate arrays; indices are int32.
Each fan records SourceChunkIndex/SourcePatchIndex, VertexStart/VertexCount,
IndexStart/IndexCount and TransitionEdges bits top/right/bottom/left in X/Z.

Hard ceilings are4,096 output vertices/triangles and1,000,000 work units. The
fixed accepted shape reserves1,920 vertices,1,728 triangles and250,000 work units
before source capture. Actual captures are bounded by1,024+256 nine-vertex
patches. Cancellation is checked before capture, per patch/sample and before
publication. Invalid options throw ArgumentOutOfRangeException; unsupported
geometry/attributes/bounds/caps throw InvalidDataException; null inputs throw
ArgumentNullException. These are explicit modern policy bounds, not measured RSS.

The parent composes fine and coarse on its existing single worker. Its integrated
region may retain16 coarse source receipts for independent proof, producing an
explicit160 current+candidate receipt bound (64fine+16coarse, twice), in addition
to owned far arrays. Fine residency telemetry remains128. Coarse reuse/cache
and GPU upload/publication belong to the parent scheduler; this helper starts
no worker and performs no caching or source reads.

## Focused gate and limitations

[Native tests](../../SWG-Godot-Client/poc/terrain-far-ring-tests/Program.cs) pass22,199 synthetic
assertions and44,379 with the pinned original file. Exact integer/rational X/Z
area is307,200m², the640m square minus the320m hole. Undirected3D edge incidence
checks every interior edge twice,256 distinct5m hole subedges and128 outer20m
subedges once, with no cracks or triangle interiors covering the hole.
The292 duplicated fine-boundary vertex occurrences copy source channels exactly.
Negative cases cover counts, duplicate/unaligned/touching layouts, changed shared
height, missing/unknown labels, incorrect UV period, conflicting normals,
cancellation and lowered/hard ceilings.

The original fixture uses negative-coordinate pond fine request(-135,65,8,8),
coarse request(-35,15,4,4,160), and actual partial family labels. It preserves
the original terrain/TGEN inputs already admitted by the source. Minimum
average face dot is251.53766017088785, positive for every triangle. One measured
run took11,090.4781ms to generate fine64 chunks,2,960.4431ms for coarse16 chunks,
and6.2887ms for BuildFar alone. These are one-run elapsed measurements, not a
frame-time guarantee or benchmark. The dominant observed cost is generation.

A separate bounded geographic QA run builds the following original fine8×8
requests and their enclosing coarse4×4 requests. All four pass generation,
fixed ring counts and the preserved-normal transition guard. Individual failures
would remain in its private receipt; none occurred. Shared CPU contention makes
these useful acceptance counts, not comparative performance results.

| Site | Fine first X/Z | Coarse first X/Z | Minimum average face dot |
|---|---|---|---:|
|Mos|98,58|23,13|217.14254396921888|
|Tusken|−103,152|−27,37|263.0063549897977|
|Pond|−135,65|−35,15|251.53766017088785|
|Baked road|−140,−162|−36,−42|283.96183326408135|

This checks four specific640m footprints. It does not establish unrestricted
world coverage, and the builder continues rejecting unknown/invalid profiles.

The [independent ring verifier](../../SWG-Godot-Client/tools/python/verify_terrain_far_ring.py)
passes both native synthetic and original receipts: exact Fraction area,
topology, source channels and periodic UVs, including11,520 original TRN family
assignments. Its [ten mutation tests](../../SWG-Godot-Client/tools/python/test_verify_terrain_far_ring.py)
reject wrong source IDs/ranges/transition bits and coherently altered original
source/output labels. Source weights are explicitly derived from categorical
labels rather than supplied original blend fields; output weights are compared.

The [GPU adapter gate](../../SWG-Godot-Client/poc/terrain-far-gpu-tests/README.md) separately checks
asymmetric texture orientation, source attributes, blend channels, fine-edge
samples and disposal. Integrated streaming/rollback acceptance belongs to the
separate host proof. The helper's default synthetic suite
is registered as `terrain-far-ring` in the
[offline regression runner](../../SWG-Godot-Client/docs/CLIENT_REGRESSION_RUNBOOK.md).

```powershell
dotnet restore poc/terrain-far-ring-tests/TerrainFarRingTests.csproj --configfile poc/terrain-far-ring-tests/NuGet.Config
dotnet run --no-restore --project poc/terrain-far-ring-tests/TerrainFarRingTests.csproj
```

Optional original mode takes exactly two app arguments: explicit original TRN
and private JSON output. Its source patch and output arrays remain private.
Use separate artifact directories for concurrent builds. Original LOD/material
completeness, far-surface collision, physical input and full-world view scheduling
remain outside this bounded rendering slice.


## Integrated frozen host acceptance

The [joined far/water checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-13_FAR_TERRAIN_AND_WATER.md)
records20 accepted frozen x64/DX12 checks,3412 motion positions and three
successor publications. Four independent far windows preserve46080 original
family assignments and exact boundary topology. Water clips against visible
outer bounds; returned-target cancellation and far/water rollback have actual
GPU regressions. All resources balance. Full frame pacing and original LOD
parity remain open; the checkpoint preserves the earlier rejected uncached run
and the measured pond-clipping correction.
