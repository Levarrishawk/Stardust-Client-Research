# Ship self/target diagnostic information widgets

Date: 2026-09-22

## Result

`CuiShipSelfInfo` and `CuiShipTargetInfo` are thin selectors over a shared
client-side ship diagnostic formatter. They display raw replicated/runtime ship
status as text and introduce no protocol. They are useful as a validation tool
for the replacement ship model, but are not the primary styled space HUD.

## Derived widgets

| Address | Provisional name | Role |
|---|---|---|
| `0x013BD7F0` | `cui_ship_target_info_construct_VA_013BD7F0` | Constructs target diagnostic panel and clones its sample text widget. |
| `0x013BD940` | `cui_ship_target_info_refresh_VA_013BD940` | Resolves current player target as a ship, sets target name/ship data, and appends localized targeted-component slot text. |
| `0x013BDBC0` | `cui_ship_self_info_refresh_VA_013BDBC0` | Resolves player's current ship and supplies it to the shared formatter. |
| `0x013DF250` | `cui_ship_info_activate_VA_013DF250` | Refreshes and enables periodic updates. |
| `0x013DF270` | `cui_ship_info_deactivate_VA_013DF270` | Disables periodic updates. |

## Shared formatter

| Address | Provisional name | Role |
|---|---|---|
| `0x013DEDC0` | `cui_ship_info_construct_VA_013DEDC0` | Builds the common text-row set from the authored `comp` page and `textSample`. |
| `0x013DF280` | `cui_ship_info_format_hitpoints_VA_013DF280` | `HP: current / max`. |
| `0x013DF340` | `cui_ship_info_format_front_armor_VA_013DF340` | Front armor current/max. |
| `0x013DF400` | `cui_ship_info_format_back_armor_VA_013DF400` | Back armor current/max. |
| `0x013DF4C0` | `cui_ship_info_format_front_shield_VA_013DF4C0` | Front shield current/max for the player's own ship. |
| `0x013DF580` | `cui_ship_info_format_back_shield_VA_013DF580` | Back shield current/max for the player's own ship. |
| `0x013DF640` | `cui_ship_info_format_capacitor_VA_013DF640` | Capacitor current/max for the player's own ship. |
| `0x013DF700` | `cui_ship_info_format_ammo_VA_013DF700` | Scans installed weapon slots 12..110 and emits current/max ammo rows. |
| `0x013DF970` | `cui_ship_info_format_refire_VA_013DF970` | Scans up to 100 weapon groups/indices and emits a ten-cell refire progress bar plus time. |
| `0x013DFC40` | `cui_ship_info_format_acquire_throttle_VA_013DFC40` | Emits acquisition state/time and active-threat count. |
| `0x013DFD50` | `cui_ship_info_set_title_VA_013DFD50` | Formats title and localized ship name. |
| `0x013DFE80` | `cui_ship_info_set_ship_VA_013DFE80` | Shows/hides panel, refreshes hull/armor, conditionally adds self-only shield/capacitor/weapon rows. |

## Data ownership

The formatter queries the resolved client `ShipObject`, installed component slots,
weapon/ammunition state, acquisition/refire state, current target and selected
targeted component. All of these are downstream views of replicated ship state
and local prediction/control state analyzed in the ship replication/controller
blocks. No send, request, opcode or command is present.

The target view intentionally omits the self-only shield/capacitor/weapon detail
unless the displayed ship is the player's current ship. The target panel adds a
localized `ship_slot_n` label for the selected subsystem.

## Replacement-client contract

Retain this as an optional diagnostics overlay backed by the common ship
view-model. It makes a strong acceptance-test oracle: given replicated component
data, the x64 client should report identical hull/armor/shield/capacitor/ammo and
targeted-slot values. A production HUD may present the values graphically.

## Confidence

- Self/target selection and shared formatter roles: **Confirmed**.
- Displayed status families and slot ranges: **Confirmed**.
- No independent protocol: **Confirmed**.
- Exact historical user-facing availability: **Unknown/optional diagnostics,
  not required for wire compatibility**.
