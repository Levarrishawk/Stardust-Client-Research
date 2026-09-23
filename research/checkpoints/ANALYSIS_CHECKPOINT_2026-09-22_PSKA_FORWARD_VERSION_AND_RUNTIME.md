# PSKA playback action: supported runtime and installed forward-version gap

Date: 2026-09-22. Binary: `C:\Stardust-DEV\Stardust.exe`, SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Decompiler output is reconstructed pseudocode, not original source. This follows the `PBSC` action-factory checkpoint.

## Executable behavior

`0x005FA000` registers tag `PSKA` with factory `0x005FA180`. The template constructor `0x005FA1E0` accepts `FORM PSKA` versions `0000`, `0001` and `0002`, dispatching respectively to `0x005FA300`, `0x005FA3C0` and `0x005FA4C0`. It has **no `0003` branch**. Version 0002 reads a seven-byte `INFO` payload: a two-byte playback actor index into template `+0x08`, a four-byte animation-name variable tag into `+0x0C`, and a one-byte boolean into `+0x20`. `0x00407A20` confirms the index resolves an actor in the script's eight-byte-stride actor table; `0x00407D10` resolves the tag in the script string-variable map. Optional `PSTR` is one four-byte tag and optional `FTRK` is one two-byte value. The first two meanings and all widths are **confirmed by code**; the final boolean's exact policy remains open.

The PSKA template creates a runtime action via `0x005FA100`/`0x005F9910`; the latter installs vtable `0x015DF210`. Its virtual update at `0x005F9AA0` runs once to attempt animation start (`0x005F9B50`), then checks the actor/animation state on subsequent updates. The start path resolves an actor through playback variables, requires `SkeletalAppearance2`, resolves the four-byte variable tag to an animation name, optionally reads a track tag, and calls the existing animation-message application path `0x007D78F0` or a CreatureController-specific branch `0x00639DF0`. It can return failure when actor, appearance or animation-name lookup fails. This is a **confirmed behavioral bridge** from playback action to the skeletal animation system, not a full decode of every optional control bit or completion rule.

## Effective content mismatch

The configured 48-archive overlay contains **42 `PSKA` actions** in effective `PBSC/0002` scripts: **37** use `PSKA/0002`, and **five** use `PSKA/0003`. Version 0002 `INFO` is seven bytes in all 37 records; version 0003 `INFO` is eleven bytes in all five. The extra trailing four bytes in v0003 are nonzero tag-shaped values in this corpus (`43 4E 41 41` in four instances; `45 4E 4F 4E` in one). The exact meaning is **unknown**. Optional `PSTR` remains four bytes and `FTRK` two bytes where present.

The five v0003 actions occur in four effective files, all from `mtg_patch_017.tre` (priority 46):

| Script | v0003 PSKA count | Additional issue |
|---|---:|---|
| `playback/hoth_snow_ball_throw.pst` | 1 | — |
| `playback/fire_projectiles_location.pst` | 1 | Also contains unregistered-by-direct-scan `FIPL` and `PPRL` tags |
| `playback/creature_throw.pst` | 1 | — |
| `playback/use_particle_system_defender.pst` | 2 | — |

`0x005FA1E0` does not dispatch v0003, so loading one of these actions through the observed executable factory cannot use the v0002 parser without an explicit compatibility decision. `tools/python/pst_reference_audit.py` traversed **203,617 effective configured TRE entries** and searched their non-empty, non-PST payloads: **28 of 32** effective `.pst` files have full-path references in other payloads, many in `combat/combat_manager.iff`; the **only four with no full-path or basename reference are exactly the four v0003 PSKA files above**. Literal searches of the executable and current Stardust server source also found no basename reference to those four. This is strong evidence they are orphaned or dormant in this installation, **not** proof of runtime unreachability: names may be synthesized, CRC-referenced or supplied by external data. No live test or dynamic trace was performed.

The reference split has a code-side anchor: setup `0x00422E80` supplies the literal `combat/combat_manager.iff` to `ClientCombatPlaybackManager::install` at `0x00469250`; its IFF reader `0x00469B10` dispatches `MTBC/0002` and calls `0x00469E80`, which fetches the referenced playback templates via `0x00402FA0`. Thus the common combat-manager references are not merely stray strings. The four v0003 scripts are absent from that asset's literal path set.

## Replacement decision boundary

Original-asset compatibility requires the new parser to retain `PSKA/0003` and its extra tag as typed data, with diagnostics and a separately researched semantic policy. Do not silently truncate its `INFO` to the v0002 width or claim Stardust.exe executes it successfully. The supported v0002 start path is a candidate for direct behavioral reimplementation, while the five newer records are an explicit forward-compatibility risk.

Reproduce the corpus counts with `tools/python/pst_overlay_inventory.py C:\Stardust-DEV C:\Stardust-DEV\swgemu_live.cfg` and the reference split with `tools/python/pst_reference_audit.py` using the same two arguments.
