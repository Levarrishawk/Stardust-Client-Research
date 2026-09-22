# Recovered terrain color-ramp TGAs

Date: 2026-09-22  
Target: configured `C:\Stardust-DEV\Stardust.exe` asset overlay

## Finding

All **53 effective `.tga` records** affected by the kind-0/zero-csize TRE metadata are live terrain color-ramp images in `mtg_patch_013_configurable_02.tre`. They are not masks. Each has an internally consistent TGA header, uncompressed true-color image type 2, height one, and a 26-byte TGA 2.0 footer (`TRUEVISION-XFILE.` signature). Pixel rows have 24-bit BGR or 32-bit BGRA samples:

| Width × height × depth | Files |
|---|---:|
| 64 × 1 × 24 | 23 |
| 32 × 1 × 24 | 9 |
| 256 × 1 × 24 | 9 |
| 128 × 1 × 24 | 7 |
| 64 × 1 × 32 | 3 |
| 8 × 1 × 24 | 1 |
| 16 × 1 × 24 | 1 |

All 53 satisfy `file bytes = 18-byte header + width × depth/8 + 26-byte footer`; none has a color map or image-ID segment. The configured overlay contains 280 effective TGA names in total. These 53 form a concrete asset-compatibility requirement for the future terrain/environment renderer. The original ENVM analysis already identified TGA ramp references (for example `terrain/colorramp/stars_tato.tga`); this block establishes a second family of installed one-row terrain ramps that a replacement must not discard as tombstones.

This is a file-format/path inventory, not proof of exactly how every ramp is sampled or blended by Stardust.exe. A modern renderer may upload them as 1D or one-row 2D textures after preserving BGR(A) channel order and the authored sample count; it need not retain the 32-bit client's GPU resource layout.

## Reproduction and confidence

`tools/python/tga_zero_csize_corpus.py` scans effective winners, validates TGA headers, and writes `local-private/generated-evidence/tga_zero_csize_corpus_2026-09-22.csv`. Counts, dimensions, footer length and archive source are **confirmed from original asset bytes**. The precise shader/terrain lookup rules for this group are **not established by this block**.
