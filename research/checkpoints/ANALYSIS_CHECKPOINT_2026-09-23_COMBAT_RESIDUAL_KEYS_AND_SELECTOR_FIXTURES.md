# Residual combat-manager keys and selector fixtures (2026-09-23)

## Scope and outcome

After the source-derived 297-name inventory, **109 of 406** keys in the effective `combat/combat_manager.iff` are unclassified by the audited server `CombatAction` producers. This block groups them by name, checks exact textual occurrences in current `src/server` and `bin/scripts`, and extracts representative selector trees from the original IFF. It does **not** designate any key dead or prove runtime reachability. Only `Stardust.exe` and its configured original assets were used on the client side.

## Residual-key classification

| Name family | Keys | Exact source-text occurrences | Interpretation |
|---|---:|---:|---|
| Attack variants | 18 | 0 | Named martial variants beyond the audited default generator; another data source or historical content may have emitted them. |
| Combo variants | 8 | 0 | Same caveat; no direct current-source string match. |
| Counter variants | 12 | 0 | Same caveat. |
| Force variants | 30 | 0 | Mostly particle-count/arc/level alternatives; no matching complete string in current server code/scripts. |
| Fire variants | 8 | 1 | `fire_turret` is configured as a prefix with `GENERATE_INTENSITY`, so this exact base key is not the resulting emitted animation in that route. |
| Grenade-healing names | 3 | 0 | Distinct from the current `HealDamageCommand` names ending in `_healing_longrange`. |
| Hit reactions | 8 | 1 | `get_hit_light` appears as a screenplay `doAnimation` choice, not a demonstrated `CombatAction` emission. |
| Coup de grâce | 3 | 3 | All three appear only in a `DeathBlowCommand` TODO comment in the audited source path. |
| Test-prefixed | 10 | 0 | Names suggest development fixtures, but that alone proves nothing about use. |
| Other | 9 | 2 | `change_posture` is passed to `doAnimation`, and `rescue` appears as a command name; neither is evidence of a `CombatAction` using the matching key. |
| **Total** | **109** | **7 keys** | No exact-source occurrence in this pass establishes a current direct combat-action producer. |

The exact-text search was bounded to `.h`, `.cpp`, and `.lua` under the current server's `src/server` and `bin/scripts`. It is not a whole-install binary or runtime trace, and constructed names may not appear literally. Classification is lexical, not semantic. Names such as `force_lightning_1_*_level_4_*`, `fire_9_single_*`, or `attack_special_*` must stay available to a replacement that consumes the original manager even if Stardust's current server does not visibly emit them.

## Read-only selector fixtures

Offsets below are **file offsets** in the 474,358-byte effective `CBTM/0002` payload from `mtg_patch_011_files_01.tre`, not executable addresses. IFF is big-endian for chunk lengths, but observed `DISP`/`POST` payloads are signed little-endian 16-bit values. Chunks are adjacent with no odd-size padding.

### `fire_single`: disposition, posture, script

Entry at `0x18`, key chunk at `0x24`, selector `FORM DDSP` at `0x38`:

```text
DISP 1  -> FORM DEPS
  POST 13 -> SNGL playback/fire_projectiles.pst; TCAA=fire_single; CAHD=""
  POST -1 -> SNGL playback/fire_projectiles.pst; TCAA=fire_single; CAHD=get_hit_light
DISP -1 -> SNGL playback/fire_projectiles.pst; TCAA=fire_single
```

The negative `DISP` and `POST` entries are parser default-slot declarations: `0x00468E80` and `0x00468510` call `0x0046A0F0` to place them in the first empty slot, while runtime `0x00469200`/`0x00468810` use configured fallbacks on an indexed miss. For this specific tree, a disposition value `1` and posture `13` select the first leaf; disposition `1` with an unfilled posture slot selects the second; an unfilled disposition slot selects the third. These are **code-plus-file-derived expected decisions**, not observed live playback. The trailing byte after `NAME`'s NUL is retained data; it is not part of the `.pst` path.

### Weighted `RAND` subtree

A compact `RAND` node at file offset `0x12B5E` within key `fire_defender_posture_change_up` has three `ENTR` children, each with `PROB = 100` and a `SNGL playback/fire_projectiles_reflect.pst` leaf. Their `CAMD` variables are respectively `block_high_center`, `block_mid_left`, and `block_mid_right`. Parser `0x00467710` sums and normalizes the three weights into cumulative thresholds; runtime `0x00467C00` samples and delegates to a child. Thus three equal raw weights give equal intended selection weight, subject to the original RNG/scaling details. This fixture can be used to test tree import and branch coverage without requiring deterministic random output from the old client.

## Implications and next step

For original-asset compatibility, preserve all 406 keys and all selector branches regardless of current server emission coverage. A new client should test known-context results against extracted IFF fixtures, but should not tie asset import to a whitelist of current server commands. Next analysis should inspect how `SNGL` variables (`TCAA`, `CAHD`, `CAMD`, etc.) are bound to playback-script actions and which asset-side dependencies they imply. Runtime capture, if later authorized and feasible, would be needed to establish actual frequency/reachability of the residual keys.

Read-only scratch scripts and extracted decompiler text are under ignored `local-private/session-2026-09-23-combat-selection/`. No original asset, executable, server source, build, or commit was changed.
