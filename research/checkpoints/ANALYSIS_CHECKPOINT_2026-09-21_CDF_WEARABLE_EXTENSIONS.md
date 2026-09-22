# CDF Wearable Palette and Object-Template Extensions Block

Date: 2026-09-21

## Result

This block resolves the rare `PALV` and `OBJT` child records embedded inside `FORM WEAR`. They extend the previously reconstructed wearable mesh/customization path and must be supported for the small set of NPC outfits that use them.

## PALV palette value

All observed `PALV` payloads contain:

1. customization-variable path string;
2. palette resource path string;
3. little-endian `uint16` palette index.

Examples from `client_shared_npc_dressed_jinkins.cdf` map `/private/index_color_1` or `/private/index_color_2` through `palette/wr_cloth_general.pal` to indices including `174`, `253`, `13`, `16`, and `3`.

There are eight PALV records across two effective CDFs. Their location inside individual WEAR forms means each record supplies a palette-aware customization value for that wearable, rather than a global character customization.

## OBJT wearable object template

The eight effective `OBJT` records each contain:

1. NUL-terminated object-template path;
2. trailing 32-bit zero/reserved value.

The Chiss poacher sample references `object/tangible/wearables/backpack/shared_ep3_chiss_poacher_backpack.iff` followed by zero. OBJT occurs inside the corresponding WEAR form alongside mesh and wearable customization records.

This provides an object-template-backed wearable path in addition to direct `MESH` skeletal-mesh references. It allows the CDF to instantiate a tangible wearable object (for example a backpack) whose own shared template/appearance/customization participates in the ordinary containment and skeletal attachment pipeline.

## Superseding executable-policy trace

`ANALYSIS_CHECKPOINT_2026-09-21_CDF_TOLERATED_EXTENSIONS.md` establishes that this executable's complete WEAR loop recognizes only `MESH` and `WCSI`. `PALV` and `OBJT` enter the generic unknown-child skip path, set the CDF unknown-data flag, and are not retained or applied. The schemas below remain valid corpus evidence for optional future enhancement, but the inferred active wearable behavior is superseded.

## Replacement-client consequence

The wearable CDF decoder should model each WEAR form as a typed bundle that may include:

- one or more skeletal mesh references;
- direct customization assignments (`WCSI`/related records);
- palette-indexed assignments (`PALV`);
- an optional object-template-backed wearable (`OBJT`).

Palette indices remain 16-bit file values. Object-template CRC/path resolution remains separate from native pointers. The resulting object should enter the same ownership and hardpoint/skeletal wear lifecycle already established for containment-driven equipment.

## Confidence

- PALV two-string plus uint16 schema: confirmed across all effective examples.
- OBJT string plus trailing 32-bit value schema: confirmed across eight effective examples.
- Palette customization role and object-template-backed wearable role: strongly inferred to confirmed from paths, nesting, and the established customization/wear pipeline.
- Meaning of OBJT trailing zero in a newer extension: unknown; not consumed by this executable and therefore not a Stardust.exe parity blocker.
