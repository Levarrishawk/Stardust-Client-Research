# Palette reader — 2026-09-30

## Bounded question and inputs

Can the replacement decode every deployed palette and preserve confirmed color/index/error policies before applying CSHD factors? Closed for owned palette bytes and color queries; named shader-factor loading is next.

Executable SHA rechecked `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`; configuration SHA `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`,48 mounts. Prior [palette research](ANALYSIS_CHECKPOINT_2026-09-12_PALETTE_RESOURCE.md) and existing inventory were read first. Current member-list SHA `08A9C87D0D0ED14561E45E98F76F30FF4D2D26EEC2D9824B8E10372BD9FB08A3`.

## Evidence and correction

**Confirmed:** RIFF PAL/data fixed layout; version high3, low nonzero warning,1,024 entries/4,120 physical bytes. Original00AB7E60 validates those fields. Original00AB8135–00AB8182 imports rawRGB intoFFRRGGBB and ignores flags for alpha. Original00AB7B10 maps all invalid indices tozero.

**Correction:** cache00AB69FC writes0xFF00FFFF. Under the directly confirmed packing this is opaque cyan, not magenta. The dated September12 wording is preserved with a correction note; current architecture/unknown-function descriptions now say cyan. No address/name is repurposed.

**Confirmed independent corpus:**264 palettes/23,410 entries/24,730 valid and invalid lookup cases agree on raw fields, opaque colors and owned trailing bytes. Three original tails remain tolerated. All live versions 0300, counts 2..256. Current CSHD references243 paths, with one missing swamp_trooper palette and 16 out-of-range serialized defaults. These differ from historical ACST reference denominators and are not combined.

## Validation and compatibility impact

Native RGB-channel asymmetry, raw flag/opaque alpha, source ownership, full truncations, invalid sizes/signatures/counts/high version, low-version warning, maximum count, safe empty query, tolerated trailing and exact cyan fallback pass. Native build/smoke pass. Full independent records/query hashes and original source identity agree. No palette pixels or private evidence were promoted. Workspace/diff validation follow closure; metrics and registries are unchanged.

Canonical [palette contract](../../docs/PALETTE_DECODER_CONTRACT.md). Next exact probe: palette-factor bundles, authored/supplied state provenance and missing/malformed resource policy; original palette shading remains separate.
