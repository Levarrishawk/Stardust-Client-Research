# Analysis checkpoint — shader version boundary

Date: 2026-09-12

## Result

The strict weighted research estimate advances from 50% to approximately 50.6%, rounded to 51%. The replacement-relevant architecture estimate advances to approximately 68%. These are evidence-weighted engineering estimates, not function-count claims.

## Confirmed

- The actual configured overlay contains 23,286 live winning SHT/EFT paths.
- It contains 82 live `IMPL 0009` records, each with seven-byte DATA.
- It contains 108 live `PASS 0010` records, each with 57-byte DATA; all 1,325 live `PASS 0009` records use 56 bytes.
- IMPL 0009 retains `SCAP`, optional `OPTN`, DATA and counted PASS ordering.
- PASS 0010 retains the `PFFP/STAG` and `PVSH/PPSH` child alternatives.
- The additional PASS byte is in the scalar prefix before the first U32 tag/value.
- No affected winner has a lower-priority same-name version suitable for direct semantic differencing.
- `s207_r.dll:0x62A15070` caches vertex shader variants by packed texture-coordinate mapping.
- `s207_r.dll:0x62A14740` builds macros/declarations, compiles vs_1_1 or vs_2_0 source through D3DX, and creates the D3D9 vertex shader.

## Strongly inferred

- IMPL 0009 DATA preserves the established `{passCount, phaseTag, two flags}` roles because its width and enclosing grammar are identical to IMPL 0007. Flag semantics still require independent evidence.
- A modern renderer can model the PFFP vertex half as a small texture-coordinate-generation shader family rather than reproducing runtime source construction.

## Unknown

- The exact ordinal and semantic role of PASS 0010's added scalar.
- Whether all 27 affected shader/effect paths are requested in ordinary gameplay.
- The fixed-function STAG color/alpha operation and argument enumerations.

## Evidence

- `shader_version_payloads.csv`: every effective IMPL/PASS payload and child signature.
- `shader_shadow_version_pairs.csv`: zero suitable same-name historical pairs.
- `decompiled-s207-fixed-function-combiner/62a15070_FUN_62a15070.c`
- `decompiled-s207-fixed-function-combiner/62a14740_FUN_62a14740.c`
