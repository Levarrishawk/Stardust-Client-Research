# Recovered MKAT combat marker appearances

Date: 2026-09-22  
Target: `C:\Stardust-DEV\Stardust.exe` plus its configured TRE overlay

## Result

Of 16 effective `.mkr` paths, **15 live records** were hidden from old compressed-size-based TRE scans. Every recovered record is a bounded `MKAT/0000/INFO` IFF from `mtg_patch_011_files_01.tre`, and every embedded shader path resolves to a live effective `.sht`. Fourteen names are the authored `combat/appearance/ui_marker_*` family already referenced by combat-special-move presentation; the fifteenth is `test_marker_01.mkr`. Thus these are usable original combat presentation assets, not overlay tombstones.

The executable's MKAT factory is registered under tag `MKAT` at `0x007973C0`. `0x007975D0` allocates a 0x4C-byte marker appearance and calls constructor `0x00797630`; that constructor accepts nested `0000` and delegates the `INFO` payload to `0x00797740`. The reader consumes, in order:

```text
FORM MKAT / FORM 0000 / INFO
  32-bit scalar
  32-bit scalar
  shader path string (fetched through shader-template registry)
  byte, byte, byte, byte, byte
  32-bit scalar
```

All 15 recovered records have exactly nine bytes after the shader's terminating NUL, matching the five-byte plus four-byte tail. The loader places the first two scalars at `+0x20/+0x24`, shader handle at `+0x28`, first two byte fields at `+0x2C/+0x30`, derives two reciprocal scalars at `+0x34/+0x38`, and stores the remaining bytes/scalar at `+0x3C..+0x48`. This supports a sprite-sheet/animated-marker interpretation but does not prove every field's user-facing meaning; the four-byte values should not be labeled blindly as particular dimensions, timing, or scale without rendering-consumer tracing.

## Evidence and confidence

- `tools/python/marker_zero_csize_corpus.py` and ignored `local-private/generated-evidence/marker_zero_csize_corpus_2026-09-22.csv`: **confirmed** 15/15 valid forms, 15/15 live shader references.
- Read-only Ghidra pseudocode for `0x007975D0`, `0x00797630`, `0x00797740` (private under `local-private/decompiler/marker_loader_2026-09-22/`): **confirmed by code** reader order and internal destinations.
- Animation frame-selection and rendering interpretation: **provisional**; runtime playback not exercised.

Three new stable names bring the registry to **2,214 unique addresses / zero duplicates**. Packet matrix and rounded planning estimates are unchanged.
