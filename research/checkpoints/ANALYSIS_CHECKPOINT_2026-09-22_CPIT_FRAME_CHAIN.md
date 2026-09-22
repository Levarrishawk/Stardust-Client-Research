# CPIT cockpit-frame asset-chain closure

Date: 2026-09-22  
Target: `C:\Stardust-DEV\Stardust.exe` and the configured `C:\Stardust-DEV\swgemu_live.cfg` TRE overlay

## Finding

The 36 effective `CPIT/0000` profiles include 11 intentionally empty `FRAM` names and 25 nonempty frame references (24 distinct APT paths). All 25 references resolve to live APT records and live immediate targets: three direct MSH and 22 DTLA LOD. Each of those 22 LOD records declares one child and that child is live in the same effective overlay. There are no absent or masked paths in this static CPIT → APT → LOD → child-MSH chain.

This corrects the prior 18-frame false-negative inventory caused by treating kind-0 TRE records with `csize=0` as empty despite nonzero `usize`. It also extends the corrected immediate-target result to the first LOD-child level. It does **not** prove all 25 frames instantiate, texture correctly, or render in the executable.

## Reproduction

`tools/python/cockpit_frame_closure.py` joins the corrected CPIT and APT inventories with `dtla_inventory_2026-09-22.csv`, then resolves each referenced child against the effective `swgemu_live.cfg` archive priority order using the corrected TRE size rule. The generated private evidence table is `local-private/generated-evidence/cockpit_frame_closure_2026-09-22.csv`.

| Chain stage | Count | Path availability |
|---|---:|---:|
| CPIT profiles | 36 | 36 valid forms |
| Empty `FRAM` | 11 | no frame requested |
| Nonempty `FRAM` | 25 | 25 live APTs |
| Direct APT → MSH | 3 | 3 live targets |
| APT → DTLA LOD | 22 | 22 live targets |
| DTLA child references | 22 | 22 live children; zero masked/absent |

`0x00580CE0` activates the cockpit controller and loads the resolved ship profile; `0x00582A40` creates/attaches an optional frame appearance under `ShipCockpitFrame`. The per-frame controller can continue with profile/global camera offsets if no frame is available. Thus empty `FRAM` is a supported authoring state, not an asset failure. The code path is described in `ANALYSIS_CHECKPOINT_2026-09-21_SPACE_CAMERA_OWNERSHIP.md`; this block adds corpus closure, not new semantics to those functions.

## Confidence and limits

- CPIT counts, APT/LOD references and path presence: **Confirmed mechanically** against the configured overlay and existing IFF inventories.
- Optional frame-object behavior: **Confirmed by previously decompiled branches**.
- Runtime construction of each authored frame and deeper mesh/shader/texture closure: **Unknown**. A path-complete LOD child may still fail because of parse, shader or resource errors.

The closure script is intentionally path-level. It does not assert MSH schema validity, resolve material/texture dependencies, or execute the client.
