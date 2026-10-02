# Owned component appearance and geometry graph

## Evidence and scope

The [baseline CMPA investigation](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-22_COMPONENT_APPEARANCE_CMPA.md) establishes deployed 0003–0005 parts and runtime composition. The [implementation checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_COMPONENT_GEOMETRY.md) verifies all 165 effective templates, 779 parts and 9,348 transform floats against an independent reader. This is template/geometry acceptance, not full appearance rendering or runtime lifecycle equivalence.

## Parser

`ComponentAppearanceDecoder` owns the IFF tree, retains APPR metadata and reads ordered PART records: a nonempty normalized NUL-terminated ASCII appearance path followed by exactly twelve finite little-endian F32 values. Relative names acquire the `appearance/` namespace. Repeated paths remain distinct placements. Modern versions 0003/0004 have APPR then PART; 0005 also has RADR with INFO U32 and optional IDTL/0000 indexed geometry. RADR values and geometry are retained without assigning an unproven semantic label. Versions 0001/0002 remain unsupported by this slice.

The authored transform maps column-vector points:

```text
x' = m0*x + m1*y + m2*z + m3
y' = m4*x + m5*y + m6*z + m7
z' = m8*x + m9*y + m10*z + m11
```

`parent.Compose(local)` applies local first. The parser preserves scale and shear and rejects nonfinite input. The 1,024-part cap is replacement policy, not an inferred original limit.

## Static graph

`AppearanceGeometryGraphReader` strictly resolves APT, CMPA, DTLA and the implemented static MESH subset. It shares immutable decoded templates by normalized logical resource path. Each edge retains its authored index and local transform; consumers create separate mutable instances. Components report missing, masked, unsupported, malformed or recursive child failures locally as `UnavailablePart` diagnostics. These diagnostics do not assert that every such failure was observed in the original client. Detail templates require all authored band children; a failed detail template can become an omitted component part.

Defaults bound 256 attempted resources, 128 MiB of source bytes, recursion depth 32 and 4,096 template edges. Aggregate limits abort the load and cannot be swallowed as omitted parts. Lookup bytes are copied before retention. Extraction and one-input parsing have their own bounds; this is not a precise peak-memory ceiling. Only static geometry is loaded: shader/texture graph, particles, sprites, no-render families and complete APPR extent semantics are outside this reader.

## Godot boundary

`GeometryAppearancePreview` shares immutable ArrayMesh resources and creates distinct transform nodes. It converts authored transforms using `S R S`, translation `S t`, where `S = diag(1,1,-1)` matches the mesh Z reflection. Geometry indices are preserved after Z reflection, which already converts tested source CCW to Godot CW; see [geometry facing](GODOT_GEOMETRY_FACING_CONTRACT.md). An independent asymmetric rotation/scale/translation fixture verifies the conversion and parent/local order.

The neutral inspector bounds expansion to 4,096 instances and requires finite positive-determinant transforms. Singular/mirrored transforms remain supported structurally but are rejected by this adapter pending a dedicated culling contract. Surface positions, normals, UV0 and indices are read back and compared. LOD midpoint visibility tests run for every loaded detail node. The slider supplies one explicit offline distance to all selectors; it does not imply PIVT or per-child camera distance fidelity.

Run the existing preview launcher with `-Component`, explicit roots/configuration/member/SHA-256 and optionally `-Headless` or a fresh private capture. Neutral colors are inspection materials. Omitted-part counts are shown. Original materials, particles, lighting, DPVS behavior, lazy ownership and gameplay readiness are not reproduced.

## Reproduction

Build the smoke project; `verify_component_appearance.py ROOT CONFIG MEMBER SHA CORE_DLL` compares one selected component. `--inventory EXISTING_CMPA_CSV` verifies the existing inventory's component set and derives each input SHA from the configured overlay; the positional member/SHA are unused in that explicit inventory mode. It does not independently discover completeness. Regenerate that inventory using `cmpa_corpus_inventory.py` when the installed corpus changes. Native `--geometry-graph` uses the same explicit root/configuration/member/SHA options to report supported geometry and omissions.

## Nested garage follow-up

[Sorted-index support](STATIC_MESH_SORTED_INDEX_CONTRACT.md) now unblocks all nine garage meshes and preserves 400 variants. Independent source/geometry/sorted hashes and neutral GPU gates pass. The earlier checkpoint omission is historical; current graph has fourteen sources and zero omissions.

## Shared material inspection

`TexturedAppearanceGraphReader` adds per-load shared static material/effect/DDS descriptions to the geometry graph. Inputs are cached by logical path; decoded immutable descriptions share SHA-256 content keys. The requested graph path and terminal mesh source path remain distinct for APT. Missing/unsupported material bundles report `UnavailableMaterialBundle` and retain neutral geometry. Resource, source and unique decoded-byte limits remain fatal. The decoded cap is checked after one bounded image decode and is not a peak-allocation promise.

The component adapter uses one owned GPU ImageTexture cache keyed by the shared DDS object identity, bounded to 128 MiB of RGBA upload bytes. Mesh/material adapters release before this cache; mutable transform nodes remain independent. Opt-in material inspection replaces only unsupported shader batches with labeled neutral surfaces. Standalone adapters remain strict unless that policy is explicitly supplied. Premultiplied MAIN eligibility is material-specific; no pixel conversion is implied.

Launcher flags `-Component -ComponentTextures -MainTextureOnly` request mixed unlit MAIN inspection. CPU bundle failures and GPU material-policy failures are reported separately. The interface shows textured/neutral surface counts; original lighting, blending and SIDX rendering remain untranslated. `verify_component_appearance.py ... --textured` discovers original dependency paths and checks complete source identities plus every mip hash for fully supported CPU graphs. Native `--textured-graph` reports identities, bundle counts and RGBA hashes.

The [shared-material checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_SHARED_COMPONENT_MATERIALS.md) verifies the garage's fifty sources, eleven shaders, eight effects, seventeen unique textures and 151 mip levels. Nine GPU mesh templates expose 35 textured/ten neutral surfaces with seventeen shared uploads.


## Camera distance follow-up

[Extent and distance](APPEARANCE_EXTENT_DISTANCE_CONTRACT.md) now closes PIVT origin. Each detail placement uses its own global transform and authored sphere center unless PIVT selects translation. The camera checkbox and `-CameraLod` opt-in drive real distance; toggling off restores the manual slider. Host midpoint tests pass for every detail instance.
