# Owned palette reader contract

## Byte grammar and acceptance

`PaletteDecoder` reads fixed RIFF `PAL `/`data`, all sizes little-endian. Physical size 24..4,120; U32 RIFF size must equal data size+12, data size must equal4+4*U16 entry count, and the declared extent must fit the physical input. Count caps 1,024. Version high byte must be3; a nonzero low byte is retained with a diagnostic, following original 00AB801A–00AB8063. Each entry is raw U8 red/green/blue/flags. Three current files have nonzero ignored trailing data after a shorter declared RIFF extent; the reader accepts and owns those bytes. Modern truncation/bounds checks prevent the original parser's possible invalid declared-length reads.

Descriptions own raw entries, version, ignored tail and diagnostics. Imported color is **0xFFRRGGBB** regardless of raw flags. Original00AB8135–00AB8182 establishes shifts and opaque alpha. No gamma/color-space transform is performed.

## Lookup and fallback

`Select` preserves the requested integer. A valid index selects its entry; negative or >=count selects **entry zero**, matching const getter00AB7B10. This differs from texture-choice clamping to the nearest valid endpoint. Empty palettes are accepted structurally but produce no color under the modern safe query; the original empty getter safety is not established.

`CreateMissingFallback` produces one opaque **cyan 0xFF00FFFF** entry and an explicit missing-resource diagnostic. Original cache00AB69FC stores that exact integer. Prior dated research called it magenta; that color label conflicts with confirmed ARGB packing and is corrected here. Malformed present files are not automatically converted into missing-resource fallback. Named resource loading is provided by the bounded factor-bundle consumer below.

## Current corpus and reference audit

All264 live effective palettes from 48 mounts agree independently:23,410 raw entries,24,730 valid/invalid selection cases and three tolerated-tail files. Every current version is0300; counts 2..256. Member-list SHA `08A9C87D0D0ED14561E45E98F76F30FF4D2D26EEC2D9824B8E10372BD9FB08A3`.

The5,173 CSHD shaders reference243 distinct palette paths. **One is missing: palette/swamp_trooper.pal**. Sixteen serialized palette-factor defaults lie outside their present palette range. Both conditions remain explicit and must be handled at application time; defaults are not rewritten in the shader decoder. This denominator is independent of the 226 ACST references in the historical research checkpoint.

## Reproduction and next action

Native synthetic asymmetric RGB/flags, ownership, exact size/version/count, complete truncation, maximum count, empty palette, trailing acceptance and low-version warning pass. All valid entries plus five invalid-index probes per palette match an independent Python implementation. Run `verify_palettes.py ROOT CONFIG PRIVATE_OUTPUT --core-dll DLL --cshd-inventory PRIVATE_CSHD_JSON` after producing the pinned CSHD inventory. Native `--palette-list` requires the exact member-list hash.

## Factor bundles and supplied state

`CustomizableShaderPaletteBundleReader.Load` loads one pinned CSHD and its distinct referenced palettes through the shared owned source cache. Defaults cap 256 attempted resources and 128 MiB encoded sources. Missing/masked palettes retain an explicit missing path and cyan sentinel; malformed present files fail. Empty present palettes remain empty and produce no color. Sources list only present bytes, while missing attempts consume resource budget. Textures and effects are outside this dependency graph.

`CustomizableShaderPaletteQuery.Select` snapshots supplied exact-case name/private keys. Absent keys use the serialized authored default; this is an explicit inspection state, not a claim about initialized live objects. Results own requested/selected indices, target tags, colors/raw flags and default/missing provenance. Per-query repeated references resolve once. No shader material factor is mutated.

All 7,908 factors across 5,173 shaders agree independently under three policies: 23,724 cases. Exactly one factor references the missing palette; sixteen authored defaults use entry zero. Run `verify_customizable_shaders.py ROOT CONFIG PRIVATE_OUTPUT --core-dll DLL --palette-inventory PRIVATE_PALETTE_JSON`; native `--cshd-palette-list` remains manifest-pinned.

[Reader checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_PALETTE_READER.md). [Factor checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_PALETTE_FACTOR_BUNDLES.md). Next: inspect factor colors in Godot; original material lighting and live customization remain separate.

## Godot swatch inspection

`Invoke-StardustAssetPreview.ps1 -PaletteInspector` accepts a pinned CSHD root. Rows expose target tags, exact variable scope/name, palette paths, requested/selected indices, colors/flags and missing provenance. Authored reset restores per-record defaults; supplied values share exact keys. Maximum 128 UI rows. Headless control gates and actual GPU readback pass on present/missing original shaders. These canvas colors do not translate original material programs. See the [inspector checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_PALETTE_FACTOR_INSPECTOR.md).
