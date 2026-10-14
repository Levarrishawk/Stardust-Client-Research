# Strict APT resolution and exterior appearance — 2026-09-29

## Question and stopping condition

Can the original building APT resolve its strict target, preserve failure boundaries, and feed verified exterior LOD resources to an explicit inspector? Stop at independent root/target/geometry/pixel checks and a private GPU capture. No live or original-executable execution.

## Evidence and identity

Baseline executable remains SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. The [appearance fallback research](ANALYSIS_CHECKPOINT_2026-09-22_APPEARANCE_FETCH_FALLBACK.md) establishes separate strict and path-default call sites. Static shader/vertex structure follows the baseline loader `vertex_array_load_v0003_VA_00784D90` already correlated in the [DTLA reader checkpoint](IMPLEMENTATION_CHECKPOINT_2026-09-29_DTLA_READER.md).

| Source | Priority / bytes | SHA-256 |
|---|---|---|
| appearance/thm_tato_imprv_building_s09.apt | 31 / 79 | 0020B7D12ACDE73C3E470AE94BBDA67B2B26A041688C5642E54EB57AA97160A8 |
| appearance/lod/thm_tato_imprv_building_s09.lod | 30 / 2780 | 990E9599C564068A009A4563A49E593AFB75628AA0BE16F90E546AB118DC1635 |
| appearance/mesh/thm_tato_imprv_building_s09_l2.msh | 33 / 7029 | 52DBD428C9E02DD63966DF5E86B7ABB649D4B55B8B8E57BD1E45B1739F3F92A0 |
| appearance/mesh/thm_tato_imprv_building_s09_l1.msh | 33 / 21232 | E9352E903FC82F4B014D41E1C5FE8F5630559ECA6B3294530144411D524682C3 |
| appearance/mesh/thm_tato_imprv_building_s09_l0.msh | 33 / 33740 | 3602E453839738026C8DFC84C74396049FD7F129164755E16D30BE23BBC97E21 |

Observation: the APT/0000 NAME points to the exterior LOD, distinct from the previously selected room `r0_mesh` detail parent. The exterior's L2/L1/L0 contain 179/353/556 vertices and 150/294/536 triangles. Independent geometry hashes match native data for all three. Confidence: Confirmed bytes/overlay. This proves the asset graph, not that a live world instantiates this building.

## Implemented boundaries

`AppearanceResolver.FetchStrict` resolves one owned APT/0000 target without default substitution. `FetchOrDefault` substitutes the documented default only for an empty, missing or masked requested path. A live malformed APT or broken/masked target still fails strictly. APT-to-APT by suffix or target FORM is rejected. Optional expected SHA belongs to the selected root (including default when used). Payloads are bounded to 16 MiB each; callers own extraction allocations before checks. Terminal type-specific decoding remains the consumer's responsibility. Tests cover positive resolution, every truncated redirect prefix, source ownership, identity, empty/missing/masked roots, broken targets and disguised recursive redirects.

The detail inspector reader now accepts either DTLA or an APT leading to DTLA through the same source cache. It does not automatically apply fetch-or-default to authored children. The exterior bundle owns three children and 27 source identities.

The exterior declares `0xD205` (48 bytes: position, normal, 2D set then 4D set) and `0xD5405` (64 bytes: position, normal, three 2D sets then one 4D set). The core preserves each auxiliary coordinate's dimension and all components. These follow count/dimension bits and original DATA lengths; final 4D semantics are not relabeled as confirmed tangent data. Synthetic fixtures verify widths, four-component retention, truncation and nonfinite rejection. Existing 1105/55405 geometry hashes remain stable.

## GPU and independent validation

The exterior has a material without the default inspector's authored single-MAIN fallback. Default mode fails explicitly. New opt-in `-MainTextureOnly` requires an opaque candidate binding MAIN and samples the named MAIN texture with UV0 under unlit policy; other candidate/program data is retained. The inspector clearly labels this mode. It does not translate normal/detail/dirt blending or claim original lighting fidelity.

`verify_detail_appearance.py` independently parses APT/DTLA and matches root/target identities, all authored bands, child counts/geometry and indexed visibility metadata. `verify_textured_mesh_bundle.py` matches the L0 exterior's 11 textures / 99 mip levels against Pillow, including legacy BGRA normal textures. The reference verifier initially rejected that raw format and was extended with mask-checked BGRA identification and proper per-mip pitch; all 99 now match.

Prototype host Debug builds without warnings/errors. Hidden GPU inspector checks all three child uploads/switches and the asymmetric UV/front-face fixture, exits 0, and saves a private 1280×720 capture inspected visually. Existing certificate-store diagnostic remains. Run the launcher with the APT member/hash above, `-Detail -MainTextureOnly`, explicit archive/config/runtime paths and a fresh private capture path. Original captures remain ignored.

## Next action

Implement bounded CMPA composition with independent transform fixtures and cycle/part-local failure policies, or close the actual PIVT distance-origin consumer. BC7 remains the next modern DDS codec milestone. Faithful full materials, visibility semantics, lazy runtime instances, original default mesh rendering and world reachability remain open.
