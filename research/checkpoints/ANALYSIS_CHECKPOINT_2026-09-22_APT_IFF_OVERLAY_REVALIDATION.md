# APT/IFF overlay revalidation — 2026-09-22

## Scope and reproducibility

Primary binary remains `C:\Stardust-DEV\Stardust.exe` (SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`). This block revalidates archive-asset conclusions after correcting the shared TRE extractor: for `kind == 0`, the record's payload length is its uncompressed size, even if its compressed-size field is zero. The archive order is the 48 `searchTree_00_*` entries in `C:\Stardust-DEV\swgemu_live.cfg`; higher-priority entries win, including zero-payload masks. Run `tools/python/apt_redirect_inventory.py C:\Stardust-DEV C:\Stardust-DEV\swgemu_live.cfg <output.csv>` to reproduce the redirect inventory. The detailed generated CSV belongs in ignored `local-private/generated-evidence/`, not Git.

## Findings

| Observation | Evidence | Confidence |
|---|---|---|
| 203,617 effective logical paths across 48 configured archives; 6,155 end in `.apt` | Corrected archive extraction and priority overlay enumeration | Confirmed for this installed corpus/configuration |
| 6,153 APT payloads parse as `FORM APT / FORM 0000 / NAME`; zero parse failures | Full effective APT payload scan with IFF bounds and NUL-string checks | Confirmed |
| Two effective APT records have zero payload: `appearance/wp_mle_2h_kashyyk_sword.apt` and `appearance/wp_sword_marauder.apt`, both from `mtg_patch_002_appearance_02.tre` | Effective winner payload lengths | Confirmed |
| Target suffixes: 5,052 `.lod`, 1,085 `.msh`, 13 `.cmp`, one each `.iff`, `.ssa`, `.prt`; zero `.apt` | Parsed `NAME` target strings from all 6,153 live APTs | Confirmed |
| Three live redirects lack a nonempty target in the configured archive overlay | Exact path lookup against effective winners | Confirmed for the overlay; loose-file fallback untested |
| 939 effective APT and 19 effective IFF records have `kind=0`, `csize=0`, `usize>0` | Header-field scan of effective records | Confirmed; this is the corrected extractor's blast radius, not a tombstone count |

The unavailable targets are:

| APT | Target | Overlay result |
|---|---|
| `appearance/speeder_usv5_s02.apt` | `appearance/lod/speeder_usv5_s02.lod` | Absent |
| `appearance/wp_rifle_dlt20a.apt` | `appearance/ssa/wp_rifle_dlt20a.ssa` | Zero-payload winner |
| `appearance/eft_cloud_cumulus_01.apt` | `appearance/mesh/eft_cloud_cumulus_01.msh` | Absent |

The 19 live `.iff` records affected by the zero-compressed-size/nonzero-uncompressed-size combination have outer tags `SLTD` (13), `CLEF` (2), and one each `NRND`, `CSFO`, `OUTL`, and `XFMS`. They are not 19 tombstones. The previously challenged `appearance/no_render.apt` is a live APT and its `appearance/no_render.iff` target is a live `NRND` IFF. Consequently the earlier claim of missing no-render APT redirects is withdrawn; DTLA's 290 absent direct mesh child paths remain a separate observed issue.

## Architectural implication

The replacement asset resolver needs two separate operations: resolve the configured archive overlay (respecting true zero-payload winners), then parse and dispatch the selected resource. For APT, preserve one logical-path redirect and reject APT-to-APT recursion as the original fetcher does. Do not infer an APT's runtime reachability from its mere archive presence. Likewise, the three unavailable target paths do not establish how the client responds when one is requested, and loose-file overrides may matter. No network-message/function mapping changed in this block.

## Remaining questions / next block

Trace executable appearance-fetcher behavior for a requested APT whose target is absent or zero-payload, including any loose-file search and fallback. Separately audit earlier IFF-family reports only where they relied on compressed-size-zero as a tombstone test; avoid reopening unrelated formats without evidence. Rounded planning estimates remain 96% implementation-grade and 90% comprehensive. Stable addresses remain 2,198 and the packet matrix 345/0.
