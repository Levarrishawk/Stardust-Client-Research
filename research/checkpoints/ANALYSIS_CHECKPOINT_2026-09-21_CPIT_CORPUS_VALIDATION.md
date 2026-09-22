# CPIT cockpit-profile corpus validation

Date: 2026-09-21  
Target corpus: configured `swgemu_live.cfg` TRE overlay

## Result

All 36 effective `cockpit/*.iff` profiles parse as `CPIT/0000` with no structural errors. Their zoom arrays are monotonically ordered and their field layouts match the recovered Stardust.exe loader. Correction (2026-09-22): 11 profiles use an empty `FRAM`, while all 25 nonempty frame APT paths are present in the effective overlay and each redirects to a present target. The original claim that 18 were absent/tombstoned was an extractor error: uncompressed APT records with `csize=0`, `usize>0` were misclassified as empty. Path/target presence does not by itself prove successful runtime rendering of each cockpit frame.

## Corpus summary

| Property | Result |
|---|---:|
| effective CPIT profiles | 36 |
| version `0000` | 36 |
| parse failures | 0 |
| unsorted zoom arrays | 0 |
| six serialized zoom values | 12 profiles |
| seven serialized zoom values | 23 profiles |
| fourteen serialized zoom values | 1 profile |
| profiles with `ISPB/HYPR` offset | 10 |
| empty frame path | 11 |
| nonempty but absent/tombstoned effective frame path | 0 |
| nonempty frame APT with present target path | 25 |

The ten hyperspace profiles include capital/multiplayer and transport hulls such as the Corellian corvette, gunships, Y-8, YT-1300 and YT-2400. `HYPR` is therefore a real camera-placement exception for hyperspace presentation rather than unused format padding.

## Runtime consequence

The frame-creation helper already treats absent frame ownership as a negative result. The per-frame cockpit controller falls back to profile/global camera offsets when no frame object is available. A replacement must preserve this as a supported runtime state, while recognizing that the installed corpus does **not** mask the 25 authored frame APTs:

- an empty `FRAM` does not invalidate the CPIT profile;
- zoom, first-person threshold and offsets remain usable without cockpit geometry;
- a live frame is a presentation child, never a prerequisite for controlling/rendering the ship;
- `HYPR` is applied only in the corresponding space presentation state.

Do not describe the 18 earlier false negatives as intentional Stardust tombstones. The corrected inventory proves path and immediate APT-target availability, not every mesh/LOD child or runtime frame outcome.

## Evidence

- `cpit_corpus_inventory.csv` — effective profile rows, source archives, fields and reference status.
- `cpit_corpus_inventory.py` — reproducible read-only configured-overlay scanner.
- `0x005823F0` / `0x005824F0` — executable CPIT dispatch and v0000 loader.
- `0x00580CE0` / `0x00582A40` — activation, fallback and optional frame construction.

## Confidence

- Counts, versions, ordering, offsets and corrected APT/target path presence: **Confirmed mechanically**.
- Missing frame as supported fallback: **Confirmed by runtime branches and deployed corpus**.
- Runtime completion and display of every authored frame: **Unknown** without deeper child-asset/runtime tests.
