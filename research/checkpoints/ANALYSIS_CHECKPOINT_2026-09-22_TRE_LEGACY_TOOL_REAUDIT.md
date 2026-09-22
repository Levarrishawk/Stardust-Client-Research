# Legacy TRE scanner re-audit and CDF corpus correction

Date: 2026-09-22  
Target: `C:\Stardust-DEV\Stardust.exe` configured asset overlay; no binary or server modifications

## Scope and cause

A repository-wide scan found 15 additional legacy Python entry points that sliced kind-0 TRE payloads using `csize` or treated `csize=0` as a mask. For kind 0 the stored payload length is `usize`; an effective record is live when `usize>0`. Some scripts were duplicated between `tools/` and `tools/python/`. All identified entry points were patched with the same rule. `tools/python/tre_zero_csize_audit.py` records the affected extension distribution and prevents this class of assumption from remaining invisible.

Across the configured archive overlay, **1,576 effective winners** have `kind=0,csize=0,usize>0`: 939 APT, 430 CEF, 86 CDF, 53 TGA, 20 SHT, 19 IFF, 15 MKR, six STF, four PAL, three WAV, and one TXT. These are live payloads, not tombstones. The configured overlay contains 203,617 distinct effective paths.

## Material revalidation

| Surface | Original result | Corrected result | Interpretation |
|---|---:|---:|---|
| effective CDF inventory | 6,204 paths; 6,200 valid; four masks | **6,288 paths; 6,284 valid; four masks** | 84 omitted live CDFs recovered; two prior rows selected lower-priority duplicates |
| newly recovered CDF child records | — | 75 `ASND`, five `CEFT`, three `HOBJ`; one childless valid CDF | Supplement existing CDF subsystem corpus counts |
| shader version/payload scanner live winners | 23,286 | **23,306** | 20 short SHTs recovered; detailed EFCT/IMPL/PASS/TXM payload-row CSV is byte-for-byte unchanged because these 20 are OPST redirects outside that scanner's selected nested records |
| CNCF clear-flora records | 147 in 95 files | 147 in 95 files | prior specialized count remains valid |
| LOBJ/HLOB light records | 121 in 117 files | 121 in 117 files | prior specialized count remains valid |
| IHOB barrel records | 196 in 80 files | 196 in 80 files | prior specialized count remains valid |
| CONT records | 174 | 174 | prior specialized count remains valid |

The recovered CDF rows are mostly tiny sound-object, effect, and hardpoint definitions. This correction changes the corpus coverage denominator and some asset counts, not the recovered executable-side CDF grammar. The four zero-byte CDF masks remain true masks. The two changed duplicate rows have the same parsed tags but now correctly attribute source to the higher-priority patch archive.

## Tool changes and evidence

Patched: `tools/scan_tre_sound_references.py`, `tools/shader_version_payload_inventory.py`, and under `tools/python/`: `animation_action_payload_inventory.py`, `dds_overlay_inventory.py`, `extract_tre_file.py`, `inventory_effective_cdf_tags.py`, `inventory_client_regions.py`, `profile_effective_cdf_payloads.py`, `scan_cont_records.py`, `scan_cncf_records.py`, `scan_effective_environment_tables.py`, `scan_effective_envm.py`, `scan_lobj_records.py`, `scan_ihob_records.py`, and `shader_version_payload_inventory.py`. The new `tre_zero_csize_audit.py` is read-only.

Reproductions and private evidence: `local-private/generated-evidence/cdf_tags_corrected_2026-09-22.csv`, `shader_version_payloads_corrected_2026-09-22.csv`. All patched scripts passed Python bytecode compilation. The corrected shader CSV compared equal against the pre-patch version of the *same* scanner; the older historical `with_pffp` CSV differs due an unrelated earlier EFCT-extension revision and is not the proper baseline for this bug.

## Confidence and remaining boundary

- TRE size rule, effective-winner distribution, corrected CDF/selected-specialist counts: **Confirmed mechanically**.
- The 20 recovered SHTs being OPST redirect-only in this scanner: **Confirmed by output equality and representative direct IFF probe**, but not a claim of full render behavior.
- The corrected scripts have not all been exhaustively rerun over every possible corpus; environment/client-region/DDS and animation-action scanners are patched prophylactically. Current affected-winner distribution shows no kind-0 zero-csize DDS and no client-region/environment IFF paths, so their published configured-overlay counts should be unaffected by this specific defect.

This audit supersedes the earlier wording that five targeted scripts were *all* remaining affected tools. It does not change packet mapping, stable-function registry, or the rounded planning estimates of 96% implementation-grade and 90% comprehensive.
