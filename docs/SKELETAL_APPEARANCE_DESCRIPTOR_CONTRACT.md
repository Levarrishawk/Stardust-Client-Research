# Skeletal appearance descriptors

## Bounded deployed readers

The engine-neutral decoder accepts SMAT/0003 and MLOD/0000, using owned strict IFF framing (big-endian sizes, no alignment padding) and little-endian fields. Older executable-accepted versions remain unsupported. One descriptor is bounded to1 MiB/8,192 nodes/depth8; lists are capped at64 and ASCII strings at1,024 bytes. These are modern resource/rejection limits.

SMAT mandatory order is INFO (U32 mesh count/U32 skeleton count/U8 raw flag), MSGN (count NUL paths), SKTI (count path/attachment-name NUL pairs). Attachment may be empty. Optional order is LATX (U16 count plus skeleton/LAT path pairs), FORM LDTB/0000/INFO (U16 count plus F32 lower/upper pairs), SFSK U8 and APAG U8. Booleans require0/1. Unknown/duplicate/unordered records, trailing string bytes, nonfinite distances and unsupported versions reject. Signed distance endpoints and overlapping intervals are preserved without turning them into adjacent cutoffs; no runtime selector is implemented here. INFO flag retains an opaque name; SFSK/APAG preserve raw names and absent versus false.

MLOD INFO is signedI16; negative count clamps to zero as recovered. That many NAME chunks follow. Every serialized path is retained; `ExposedPaths` takes at most four, with skip-L0 removing serialized index0 only when more than one exists. MLOD contains no distances. This descriptor does not instantiate mesh generators or implement caches/customization/availability fallback.

## Object-root dependency slice

`CreatureAppearanceDescriptorBundleReader.Load` requires paired explicit object-root and appearance SHA-256 pins, validates options before lookup, resolves existing deterministic object-template inheritance, follows its appearance reference and loads ordered MLOD descriptors through one owned source cache. Missing/masked children and aggregate count/byte exhaustion reject; successful sources resolve externally once. The slice requires MLOD children; direct SKMG remains unsupported. Skeleton/LAT paths are retained references, not loaded by this milestone. No arbitrary appearance default substitution is made.

The original human sample resolves five unchanged inherited object templates, hum_m.sat and two LMGs: eight sources/9,080 bytes. SAT has body/head generators, all_b skeleton plus face skeleton attached at head, two LAT associations and four bands (-1,10),(9,20),(19,40),(39,10000). Both LMGs preserve four MGN paths. Original-source identity, field values and paths agree independently. [Implementation checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_HUMAN_APPEARANCE_DESCRIPTORS.md) records reproduction and negative gates.

## Evidence and next boundary

Grammar and runtime caps originate in the [appearance census](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-12_APPEARANCE_CORPUS_VERSIONS.md), [MLOD runtime](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-12_MLOD_RUNTIME.md) and [shared-detail bridge](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-22_SKELETAL_LOD_COMPOSITION.md). Exact structures are Confirmed; no new historical flag semantics are inferred. Next is owned SKTM/SLOD decoding and attachment validation, then mesh data and bind-pose rendering. Character skinning/animation/rendering is not supplied here.


Optional skeleton loading is now implemented under the [owned SKTM/SLOD contract](SKELETON_TEMPLATE_CONTRACT.md). Ten-source original union and attachment matching pass; LAT/MGN remain references.


Optional selected MGN skin-source loading is implemented under the [prefix contract](SKELETAL_MESH_SKIN_SOURCE_CONTRACT.md). Detail0 twelve-source union agrees independently; primitive generation/rendering remains separate.

## Explicit detail1 verification — 2026-10-01

[Selected detail contract](SKELETAL_LOD_SELECTION_CONTRACT.md) verifies the original human object/SAT/MLOD chain at explicit mesh detail1, including selected body/head MGN records, level0 skeleton bind identity, material/texture closure, missing/masked resources, invalid detail and budgets. Independent bytes agree: 29 sources / 4,167,914 bytes, 968 positions / 1,904 triangles. Automatic mesh/skeleton level coupling and distance-driven selection remain unknown or unimplemented; this result establishes explicit resource selection only.
