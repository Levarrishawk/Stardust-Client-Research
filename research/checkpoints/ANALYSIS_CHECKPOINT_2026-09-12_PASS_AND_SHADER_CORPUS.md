# Analysis Checkpoint — PASS Layout and Shader Corpus

Date: 2026-09-12  
Primary binary: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Outcome

The executable-supported PASS family is now structurally decoded through version 0009, including its program/fixed-function branches. The entire raw installed SHT/EFT corpus was inventoried, exposing a later-patch compatibility issue that was not visible from the executable dispatcher alone.

## Confirmed executable layout

PASS supports 0000-0009. Version 0009 reads 24 U8 values (the first is a child-stage count) and eight U32 values from its 56-byte DATA chunk into an 0x80-byte runtime description. It then loads either fixed-function `PFFP` or named vertex shader `PVSH`, followed by either pixel shader `PPSH` or counted fixed-function texture stages. PFFP supports 0000/0001. Older PASS versions populate additive subsets under common defaults.

Exact state labels are not being guessed. The wire widths, order, object offsets, branch tags, and lifetimes are confirmed; final depth/blend/cull/stencil labels require graphics-consumer correlation.

## Installed raw corpus

Correction (2026-09-22): the counts below are historical and used compressed-size-zero as an empty-record test. The corrected inventory has 37,842 live SHT, 87 masks, 467 live EFT, SSHT/0000 35,293, SSHT/0001 2,395, and PASS/0009 1,703. Other PASS 0008/0007 and IMPL/PASS 0010 counts here are unchanged; see the appearance-fetch fallback checkpoint. The executable PASS layout conclusions are unaffected.

- 36,686 live SHT and 1,243 SHT tombstones.
- 467 live EFT.
- SSHT 0000: 34,302; SSHT 0001: 2,266.
- EFCT 0000: 4; EFCT 0001: 463.
- Dominant embedded passes: PASS 0009 = 1,697; PASS 0008 = 658; PASS 0007 = 86.
- No extraction, decompression, or IFF framing errors.

## Compatibility discrepancy

Later patch records contain 108 PASS 0010 forms and 82 IMPL 0008/0009 forms. The main Stardust.exe shader dispatchers stop at PASS 0009 and IMPL 0007. An executable-wide immediate search did not identify a second shader dispatcher; the apparent `0010` code match is the unrelated EMTR particle emitter. These forms may be unreachable/unused by this executable, but the future client's requirement to consume the complete original assets means they must be analyzed rather than ignored.

## Progress

- Strict whole-client analysis: **48%**.
- Replacement-client-relevant understanding: **64%**.
- VFS/TRE/templates/assets: **63%**.

## Exact resume point

Trace the PASS 0009 runtime fields into the graphics interface to assign exact depth, alpha-blend, cull, fog, stencil, color-write and material semantics. Decode texture-stage versions and their operation/argument enums. Then resolve effective overlay visibility and reference reachability for IMPL 0008/0009 and PASS 0010.
