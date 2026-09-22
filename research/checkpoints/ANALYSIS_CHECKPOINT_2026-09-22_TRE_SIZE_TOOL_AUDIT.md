# TRE kind-0 tool audit and cockpit-frame correction — 2026-09-22

## Scope

This block corrected five targeted scripts from the appearance-fetch queue against `C:\Stardust-DEV\Stardust.exe`'s installed asset configuration. It changes no executable or server code. For a TREE record with `kind=0`, payload length is `usize` even when `csize=0`; SearchTree's presence test likewise uses the uncompressed-size field. Those scripts were `tools/tre_probe.py`, `tools/python/cpit_corpus_inventory.py`, `tools/python/iff_shader_structure.py`, `tools/python/scan_effective_asset_references.py`, and `tools/python/sound_overlay_inventory.py`. A subsequent global script audit found additional older tools with the same bug; see `ANALYSIS_CHECKPOINT_2026-09-22_TRE_LEGACY_TOOL_REAUDIT.md`.

## Revalidation results

| Surface | Corrected result | Change to prior conclusion | Confidence |
|---|---|---|---|
| 36 effective `cockpit/*.iff` profiles | 36 CPIT/0000, zero parse failures, ordered zooms; 11 empty `FRAM`, all 25 nonempty frame APT paths present | **18 false missing/tombstoned frame paths withdrawn** | Confirmed by corrected configured-overlay scan |
| 25 nonempty frame APT paths | Each parses as APT/0000 and points to a present immediate `.msh`/`.lod` target | VFS-name/target closure, not proof every nested LOD child renders | Confirmed at immediate redirect boundary |
| Effective STF records with `kind=0,csize=0,usize>0` | Six 13-byte STF files in `mtg_patch_013_configurable_02.tre`, each with `CD AB` magic, variant 1 and zero entries | They are valid empty tables, not archive masks | Confirmed by bytes/header |
| Effective sound overlay | 7,242 live `.snd`, zero masks/errors | No output change after corrected extractor | Confirmed by rerun and CSV comparison |
| Forward-shader path-reference scan | 27 targets, 203,617 effective winners, 40 reference rows | No output change after corrected extractor/target CSV | Confirmed by exact CSV comparison |
| Direct IFF/representative TREE probes | `shader/a_skin_.sht` extracts as a valid `OPST/0000` IFF; no-render APT extracts 57 bytes and redirects to no-render IFF | Confirms corrected direct-extraction tools handle live `csize=0` records | Confirmed by local probes |

The prior CPIT checkpoint wrongly described 18 nonempty frame paths as masks and inferred intentional masking of stock cockpit frames. Corrected effective rows instead show 25/25 named frames present, with 25/25 immediate APT targets present. This does **not** remove the original runtime code's missing-frame fallback: it remains a valid behavior for other configurations, later load failures, or empty `FRAM` paths. The user-visible architecture should not infer a Stardust-specific missing-frame policy from the old false counts.

## Evidence and reproduction

- Corrected `tools/python/cpit_corpus_inventory.py C:\Stardust-DEV <output.csv>` produces `effective_profiles=36 errors=0 missing_frames=11`.
- Corrected `tools/python/sound_overlay_inventory.py C:\Stardust-DEV C:\Stardust-DEV\swgemu_live.cfg <output.csv>` produces 7,242 live winners; its complete rows match the prior output.
- Corrected `tools/python/scan_effective_asset_references.py` with the corrected shader-overlay winners CSV produces 40 rows exactly matching the earlier reference CSV.
- Six STF header rows and APT target cross-checks were generated read-only. Generated CSVs and decompilation evidence live under ignored `local-private/`, not Git.

## Remaining boundary

The 25 frame APTs' immediate targets exist, but nested LOD child availability and actual cockpit-frame display are not established by this audit. STF tables with zero entries may be valid empty resources; their per-key localization behavior is already separately specified by the executable-side STF research. This block adds no provisional executable functions or packet mappings. Planning estimates remain 96% implementation-grade / 90% comprehensive; registry 2,204 unique addresses and packet matrix 345/0.
