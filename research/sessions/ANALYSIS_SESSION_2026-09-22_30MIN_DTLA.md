# Thirty-minute continuation — DTLA general appearance LOD

Date: 2026-09-22, America/New_York  
Scope: one bounded original-asset block after the LEFX checkpoint. Static only. Target remains the verified `C:\Stardust-DEV\Stardust.exe` (SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`). No code build, server connection, Git commit or push.

## Completed

- Reconstructed `DTLA` general detail appearances separately from skeletal MLOD and terrain LOD. `0x0079A370` supports versions 0001–0007; 0005–0007 occur in the installed overlay.
- Identified the packed IFF grammar, little-endian ID/F32/F32 distance records, ID-keyed CHLD appearance paths, optional PIVT and visibility-policy forms. A 6,198-file corpus audit confirms the grammar.
- Traced template construction, lazy child fetch/instantiation, runtime eight-child cap, distance selection, prior-detail hint, adjacent-level availability fallback and `skipL0` behavior.
- Resolved `IDTL/0000/VERT/INDX` as optional indexed geometry: 1,836 effective records with in-range indices. Imported DPVS calls establish that TEST and WRIT produce visibility models, not visible render meshes.
- Identified boundary anomalies: three tombstones, one non-DTLA `.lod` extension outlier, eight duplicate-child-ID files, one reversed interval file, and 290 distinct mesh child paths unresolved by direct overlay lookup. These affect 69 DTLA parents, 60 of which have no resolvable child at any level. All 385 no-render APT references resolve to a valid high-priority APT/NRND pair. The runtime's precise response to an actively selected missing mesh remains unverified and is not hidden by a claim of graceful fallback.
- Corrected an inventory-helper bug before final validation: uncompressed TRE records use their uncompressed byte size even when compressed size is zero. The false interim count of 675 missing references and false no-render tombstone conclusion were withdrawn; the corrected scan gives 290 missing meshes.
- A narrow blast-radius audit found 939 effective `.apt` and 19 `.iff` records with this size-field combination, versus zero effective `.cmp`, `.ltn`, `.spr`, `.lod` or `.msh` records. Prior APT/IFF zero-payload claims made with the shared helper need explicit revalidation; this block does not silently revise other reports.
- Fixed the shared `tools/python/scan_tre_sound_references.py` extractor to use uncompressed size for kind-0 records and verified it returns the high-priority 57-byte no-render APT as `FORM` data.
- Added 20 stable provisional names (2,178 → 2,198) and reconciled stale entry-point/port-plan wording. Packet-source matrix unchanged at 345 documented / 0 reachable inventory gaps.

## Evidence and confidence

The [DTLA checkpoint](../checkpoints/ANALYSIS_CHECKPOINT_2026-09-22_DTLA_DETAIL_APPEARANCE.md) contains executable addresses, corpus counts, structural schema, confidence and replacement contract. The reproducible [corpus inventory script](../../tools/python/dtla_corpus_inventory.py) reads the configured overlay and writes its detailed CSV only to an ignored local-private path. The curated documents and stable-name registry were updated; no raw assets or bulk decompiler output were added to Git.

Confirmed: version acceptance, field widths, ID mapping, sorted distance bands, child cap/lazy creation, adjacent availability fallback, IDTL geometry, DPVS TEST/WRIT model roles and corpus counts. Strongly inferred: engine-neutral detail-appearance/visibility abstraction. Unknown: RADR and flag semantics; selected-child failure behavior for missing mesh paths; operational behavior of the one non-DTLA path. The no-render redirect is present and valid in the effective overlay.

## Current position and next bounded question

The rounded research estimates remain **96% implementation-grade / 90% comprehensive**; these are planning judgments, not raw function coverage. The 2,198-address registry and 345-row server packet matrix have separate denominators.

Next revalidate prior APT/IFF conclusions that treated kind-0 compressed-size zero as a tombstone, using the corrected shared extractor. Then investigate the appearance fetcher's missing-file/error substitution and `RADR`/policy-flag consumers. A focused static caller/callee audit should establish whether absent DTLA children are skipped, replaced, or can fail if selected. Do not move to Godot/client construction until the user-requested comprehensive analysis gate is reached.
