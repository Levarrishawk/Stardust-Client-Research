# Combat options runtime effects

Date: 2026-09-25. Workstream A09 refinement. Target: verified `Stardust.exe`. Decompiled output is reconstructed pseudocode.

## Result

`SwgCuiOptCombat` is a three-setting, client-only presentation page. Its controls alter floating combat text immediately; they do not send a command or packet and do not change authoritative combat state.

| Widget | Config key/default | Live state | Runtime consumer | Effect |
|---|---|---:|---:|---|
| `sliderDamageNumSize` | `ClientUserInterface.damageOverHeadSizeModifer`, `1.5` | `0x01929D34` | `0x009563B0` | forwards the scale to the overhead-damage-text construction path |
| `checkDamageNumOthers` | `ClientUserInterface.showDamageOverHeadOthers`, `true` | `0x01929D39` | `0x00956A10` | gates creation of overhead-damage display records for non-player targets |
| `checkDamageNumSelf` | `ClientUserInterface.showDamageOverHeadSelf`, `true` | `0x01929D38` | `0x00956A10` | gates creation of overhead-damage display records for the local player |

The slider binding at constructor `0x00EA5B70` has an executable-confirmed range of `0.25` through `2.0`. The two remaining bindings are Boolean checkboxes.

## Lifecycle and data flow

```text
ConfigClientUserInterface install 0x009B2F30
  -> reads three ClientUserInterface keys and defaults
CuiCombatManager install 0x008C74A0
  -> copies defaults into three live globals
  -> registers the live globals as machine-option/debug-flag settings
SwgCuiOptCombat 0x00EA5B70
  -> generic float/Boolean option bindings
widget change
  -> live setter 0x008C9900 / 0x008C98E0 / 0x008C98F0
  -> immediate damage-number presentation change
OK or Cancel
  -> shared SwgCuiOpt transaction/persistence behavior
```

`0x00956A10` compares the affected object's identity with the local-player object and chooses the self or others gate. When enabled it constructs a local display record and appends it to a manager-owned vector. `0x009563B0` passes the live size modifier into the text-display construction helper. Neither function contains a network send in the inspected path.

The page therefore relies on the shared options architecture already recovered: changes preview live, parent Cancel restores the captured values, and deactivation writes machine options. The config globals are default getters, while the manager globals are the mutable runtime values.

## Replacement contract

Model these as local presentation preferences, not protocol fields. A replacement must keep the self/others distinction and the original range/defaults, but it need not reproduce the legacy paired-global or debug-flag registration implementation.

## Confidence

- **Confirmed by code:** controls, range, config keys/defaults, manager initialization, self/other selection, live gates, size consumer, and absence of a send in the inspected paths.
- **Strongly inferred:** friendly names for the display-record helpers and exact historical type names.
- **Unknown but non-blocking:** the original misspelling `Modifer` should remain only as a compatibility key; a modern internal setting name can be corrected.

Ignored reproduction evidence: `local-private/generated-evidence/opt_combat_global_xrefs.tsv` and `local-private/ghidra/decompiled/opt-combat*/`.
