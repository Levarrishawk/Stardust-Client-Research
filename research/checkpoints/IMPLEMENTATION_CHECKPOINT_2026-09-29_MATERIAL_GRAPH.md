# Owned static material graph — 2026-09-29

## Question and stopping condition

Can the verified static mesh resolve an owned, bounded shader/effect/texture graph without selecting or executing legacy shader programs? Stop after one original graph and independent synthetic positive/negative fixtures.

## Identity and evidence

Baseline executable SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`, rechecked before address correlation. Configuration SHA-256: `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`.

The mesh `appearance/mesh/thm_tato_imprv_building_s09_r0_mesh_l4.msh` has SHA-256 `CA4F3070B8BBC9CD50DEC617AC2C4C249F3E888E15EB10E6F832113F7C6D126B`. Its graph contains 9 shaders, 9 textures and one shared effect: 20 source identities including the mesh. Texture mips total 80 (eight BC1 textures and one BC3).

`effect/a_simple.eft` is 853 bytes, SHA-256 `883D8493D89906F620E849DA19922A716380CD1673D45E8638B9C82C1E9C0BFC`, mounted at priority 52 from `stardust_3_00.tre`. EFCT/0001 contains three IMPL/0005 candidates, each with one PASS/0009: VS/PS 2.0 with DOT3 option, VS/PS 1.1, then fixed-function fallback. Program names, capability words, options, phase MAIN, pass fields and raw metadata are retained. Candidate order is preserved; no candidate is selected automatically.

Observation: the 33-byte STAG/0000 has 18 U8 fields, LE U32 texture tag at offset 18, LE U32 coordinate tag at offset 22, then seven U8 fields. Both tags in this effect are MAIN. Static inspection of baseline loader `texture_stage_load_v0000_VA_00762CA0` corroborates the field-read sequence. An initial offset 17/21 in this hour's parser was rejected by the strict preview guard and corrected to 18/22 in code and synthetic fixtures. Confidence: Confirmed structure; individual operation semantics remain outside this implementation.

MATS/0000 stores tag plus 17 finite F32 scalars; their component meaning is Unknown in the new reader. TCSS/0000 stores five-byte tag/index records. Original MAIN selects UV0. SSHT texture references and dynamic flags are retained.

## Implementation and validation

Engine-neutral `StaticEffectDecoder`, `StaticShaderParameters` and `TexturedStaticMeshReader` own the decoded graph. Limits bound candidates, passes, tags, resources, aggregate source bytes (128 MiB), and decoded texture bytes (128 MiB). The explicit resolver owns extraction allocation before bundle budget checks. Missing/masked references and mismatched mesh identity fail. No filesystem discovery or engine object enters the core.

`MaterialScenarios` uses independent synthetic graph fixtures, prefix truncation, duplicate tags, invalid indices/counts/versions, nonfinite material values, missing/masked dependencies and identity failures. `tools/python/inspect_static_material_slice.py` independently reads original IFF structure. `tools/python/verify_textured_mesh_bundle.py` independently checks raw index CRCs, overlay resolution, geometry hash and all 80 Pillow/native mip hashes. Run the latter with archive root, configuration, logical mesh member, the mesh SHA above, and the smoke-test project path. Original source material remains ignored; curated records contain independently authored descriptions and identities.

## Compatibility consequences and next probe

Confirmed: dependency ownership, graph structure and selected corpus bindings. The graph supports later material translation without reproducing legacy object layout. Preview sampling is a separate explicit policy; it does not prove original lighting, shader constants or fixed-function combiner equivalence. Next: resolve the parent appearance and its LOD selection, then implement a fixture-backed material interpretation for the selected branch.
