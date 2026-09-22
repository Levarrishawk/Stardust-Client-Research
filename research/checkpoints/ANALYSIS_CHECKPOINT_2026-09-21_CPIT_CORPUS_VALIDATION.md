# CPIT cockpit-profile corpus validation

Date: 2026-09-21  
Target corpus: configured `swgemu_live.cfg` TRE overlay

## Result

All 36 effective `cockpit/*.iff` profiles parse as `CPIT/0000` with no structural errors. Their zoom arrays are monotonically ordered and their field layouts match the recovered Stardust.exe loader. The corpus also proves that a cockpit frame is optional: 11 profiles intentionally use an empty `FRAM`, and 18 more name a frame appearance that is absent/tombstoned in the effective Stardust overlay. Only seven currently resolve a live frame.

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
| nonempty but absent/tombstoned effective frame | 18 |
| live effective frame | 7 |

The ten hyperspace profiles include capital/multiplayer and transport hulls such as the Corellian corvette, gunships, Y-8, YT-1300 and YT-2400. `HYPR` is therefore a real camera-placement exception for hyperspace presentation rather than unused format padding.

## Runtime consequence

The frame-creation helper already treats absent frame ownership as a negative result. The per-frame cockpit controller falls back to profile/global camera offsets when no frame object is available. A replacement must preserve this as a supported content state:

- empty or tombstoned `FRAM` does not invalidate the CPIT profile;
- zoom, first-person threshold and offsets remain usable without cockpit geometry;
- a live frame is a presentation child, never a prerequisite for controlling/rendering the ship;
- `HYPR` is applied only in the corresponding space presentation state.

This is especially important for Stardust's configured overlay, which intentionally tombstones many stock cockpit-frame appearances while retaining their CPIT camera profiles.

## Evidence

- `cpit_corpus_inventory.csv` — effective profile rows, source archives, fields and reference status.
- `cpit_corpus_inventory.py` — reproducible read-only configured-overlay scanner.
- `0x005823F0` / `0x005824F0` — executable CPIT dispatch and v0000 loader.
- `0x00580CE0` / `0x00582A40` — activation, fallback and optional frame construction.

## Confidence

- Counts, versions, ordering, offsets and reference presence: **Confirmed mechanically**.
- Missing frame as supported fallback: **Confirmed by runtime branches and deployed corpus**.
- Intent behind each individual Stardust tombstone: **Unknown**, but no intent assumption is needed for compatibility.
