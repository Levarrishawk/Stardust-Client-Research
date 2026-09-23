# Analysis checkpoint — XFMS transform masks

Target: verified `C:\Stardust-DEV\Stardust.exe` (SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`) and effective configured TRE overlay. Static inspection only. Decompiler output is reconstructed pseudocode, not source.

## Findings and confidence

| Finding | Evidence | Confidence |
|---|---|---|
| `XFMS` is a versioned, separately cached transform-mask resource | `0x008291B0` opens existing path and caches constructed mask; `0x0083CDD0` enters `XFMS`, dispatches only `0000` to `0x0083D000` | Confirmed by code |
| `XFMS/0000` contains an `INGR` chunk with a little-endian U16 count followed by that many zero-terminated transform names | `0x0083D000` reads two-byte count, repeats bounded string read and inserts names in an ordered set; all nine corpus records match | Confirmed by code and assets |
| The listed names designate included transform groups/bones, not packed skeleton indices | `head`, `neck`, `spine1`, `rarm`, `lthigh`, Bantha facial names in corpus; parser constructs CRC strings | Strongly inferred |
| The exact animation-blending semantics of membership (inclusion/exclusion or weighting) | Loader alone does not establish it; following runtime trace establishes in/out routing but not blend mathematics | Partially resolved in the next checkpoint |

The nine effective live files are `appearance/mask/mask_upper_body.iff` (28 names), `mask_rider.iff` (9), `mask_spine3_arms.iff` (26), `mask_rider_no_combat.iff` (26), `mask_head.iff` (2), `mask_arm_right.iff` (12), `mask_arm_left.iff` (11), `spine.iff` (5), and `bantha_upper_spine.iff` (9). Every file is `FORM XFMS/FORM 0000/CHUNK INGR`; each declared count equals the parsed number of names. No parser mismatch was observed. All are supplied by `mtg_patch_001_appearance_01.tre` in the configured effective overlay.

`0x008290C0` initializes the transform-mask cache. The loader at `0x008291B0` tests logical-path existence, opens the resource through the VFS, constructs a mask, and stores it in the cache. The mask constructor `0x0083CDD0` enforces the outer tag and version; `0x0083D000` builds a name set rather than preserving disk order as a semantic index. `transformMaskLocation` in the masked-priority animation-template code (`0x00817F80`) reaches the cache lookup `0x008291B0`. This is a direct path from authored animation template to XFMS resource loading, not merely a dormant parser. The matching serialization/debug function is `0x00818330`.

## Replacement-client implication

Retain name-based mask resources and map names to each loaded skeleton's transform indices at runtime. Do not serialize native pointer/index width into the asset contract. Import and cache XFMS independently of skeleton files. The follow-up `ANALYSIS_CHECKPOINT_2026-09-23_XFMS_RUNTIME_ROUTING.md` establishes the in/out group membership route; blend/priority mathematics remains open.

Scratch corpus probe: ignored `local-private/session-2026-09-23-marathon/scan_xfms.py`. Read-only Ghidra decompilations are in the same ignored session directory.
