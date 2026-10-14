# Owned DTLA reader and four-set vertices — 2026-09-29

## Bounded question

Can the current parent detail appearance be decoded into owned bands, child paths and visibility geometry, and can all authored child meshes be verified? No runtime LOD fidelity claim is made by this parser checkpoint.

## Identity and evidence

Executable baseline SHA-256 was rechecked: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. The [DTLA research](ANALYSIS_CHECKPOINT_2026-09-22_DTLA_DETAIL_APPEARANCE.md) establishes the deployed grammar and ID association. The original `appearance/lod/thm_tato_imprv_building_s09_r0_mesh.lod` is 1,434 bytes at priority 30, SHA-256 `BD808473CB6E13D2083F85BBC4ACA91F834134E51413E00734E8326E234F13AF`, from `mtg_patch_001_appearance_01.tre`.

Observation: version 0007, PIVT 0, four authored records: ID 2/L3 at 400–2000, ID 3/L2 at 200–400, ID 4/L1 at 80–200, ID 5/L0 at 0–80. The prior L4 preview mesh is not referenced by this parent. Filename similarity does not establish runtime reachability. Confidence: Confirmed bytes and overlay.

| Child | SHA-256 | Vertices | Triangles |
|---|---|---:|---:|
| L3 | AFC235A78054617113AFB9677DCDBA859D306CBB090BF4A9720EDD9D35CC935A | 106 | 95 |
| L2 | 9C532268397DE776EE34D01E73B028408AAD009EB929E4A8F36F716F52872A29 | 188 | 159 |
| L1 | E2348257C1E42DE559B6E6B0D778A27FE29DFE68E6240453534316B444471107 | 349 | 292 |
| L0 | 8C912F891F51F6E3CFE329D22FC0E54E564E9736C94B5283E9F4B3608F877942 | 552 | 534 |

All children resolve at priority 33. RADR flag 1 owns 21 vertices/57 indices; TEST flag 0 has no geometry; WRIT flag 1 owns 12 vertices/36 indices. These are preserved as visibility descriptions, not rendered triangles.

L0/L1 contain declaration `0x55405`, stride 56. Baseline loader `vertex_array_load_v0003_VA_00784D90` reads position when bit 0 is set, normal when bit 2 is set, coordinate count from bits 8–11, and each coordinate dimension as its two-bit field starting at bit 12 plus one. This declaration therefore contains position, normal and four 2D coordinate sets. Original DATA byte lengths agree. Confidence: Confirmed structure; no tangent interpretation is invented. The core now owns all four sets and preserves the existing 1105 interchange hash.

## Implementation and validation

`DetailAppearanceDecoder` supports deployed versions 0005–0007, owned APPR metadata, PIVT, ID-keyed bands and children, and RADR/TEST/WRIT policy geometry. Bounds limit records/vertices/indices; nonfinite data, unknown versions, invalid child IDs and out-of-range indices fail. Reversed intervals and duplicate child paths generate explicit diagnostics. The deterministic replacement duplicate policy retains the last path; exact original duplicate semantics remain a separate verification item.

Native synthetic fixtures cover every truncated prefix, versions, ID association, duplicate/reversed diagnostics, finite fields, ownership and geometry errors. Four-coordinate mesh fixtures cover auxiliary data, truncation, nonfinite values and ownership. `tools/python/verify_detail_appearance.py` uses independent TRE/DTLA/geometry readers and invokes native probes for comparison. It takes archive root, configuration, parent member, expected SHA and smoke project; no original files are emitted.

## Compatibility consequences and next probe

Keep authored bands separate from selection state and engine objects. Retain visibility policy metadata without assigning unknown RADR semantics. The parser does not apply skipL0, global distance scale, hysteresis, previous-detail search or availability fallback. Next: implement a bounded selector from baseline `0x00787710`, retaining boundary ties and adjacent-availability order, then demonstrate switching in prototype host with uploaded mesh/texture verification. APT redirect loading is an independent dependency boundary.
