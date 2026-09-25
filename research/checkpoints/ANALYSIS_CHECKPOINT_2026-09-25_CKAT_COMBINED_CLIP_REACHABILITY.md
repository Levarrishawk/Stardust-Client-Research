# CKAT combined-root clip reachability audit

Date: 2026-09-25. Question A03. Scope: the two effective configured clips that contain both top-level QCHN and LOCT.

The two files are:

- `appearance/animation/all_b_cbt_hop_turn_right_90.ans`
- `appearance/animation/mouse_droid_idl_stand.ans`

Two independent static searches found no consumer references outside the files' own archive directory records:

1. raw full-path and basename byte searches across every configured TRE archive;
2. full decompression of every TRE member followed by full-path and basename payload searches.

Exact-name searches of `Stardust.exe`, the research repository, and the current Stardust server source likewise found no loading/spawn reference. The effective appearance inventory confirms that both assets exist in `mtg_patch_001_appearance_01.tre`; presence alone is not reachability.

## Conclusion

Normal installed-content reachability is **not demonstrated**, and the two clips are **strongly inferred dormant/orphaned** under the configured overlay and current server. This is not a proof of impossibility: a path could be synthesized, addressed by CRC, selected through an external tool/data source, or introduced by future content.

The replacement importer must still support the combined QCHN+LOCT mode because original-asset compatibility is non-negotiable and the executable implements the mode explicitly. It does not need to block the first playable milestone on reproducing an observed gameplay use that current static content cannot establish.

A03 is now narrowed to one semantic issue: naming the external actor/basis convention of the already recovered combined translation formula. The inner arithmetic and configured-content reference audit are closed.

Evidence is reproducible with `tools/python/scan_tre_string_occurrences.py` and `tools/python/find_tre_members_containing.py` against `C:\Stardust-DEV`, using both full paths and both basenames. Confidence: **confirmed** for zero literal payload/reference hits in the searched scope; **strongly inferred** dormancy; **unknown** for synthesized/CRC/external reachability.
