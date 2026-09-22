# Character sheet runtime

Date: 2026-09-22  
Target: `C:\Stardust-DEV\Stardust.exe`

## Result

The complete character-sheet mediator is now bounded around the previously
decoded response family. It is a mixed projection: most live attributes come
from the local replicated creature/player objects, while five legacy request/
response families populate location, faction, guild and badge details. The only
workflow handoff is stat migration; the sheet itself does not mutate gameplay
state.

## Stable functions

| Address | Provisional name | Role | Confidence |
|---|---|---|---|
| `0x00F9F290` | `swg_cui_character_sheet_construct_VA_00F9F290` | binds attributes/factions/identity/location/guild/badge/consumption/PvP widgets and stat-migration button | Confirmed |
| `0x00F9FBC0` | `swg_cui_character_sheet_activate_VA_00F9FBC0` | binds the local player, sends directory/detail queries, installs five response listeners and refreshes projections | Confirmed |
| `0x00FA0000` | `swg_cui_character_sheet_deactivate_VA_00FA0000` | removes the response listeners and player watches | Confirmed |
| `0x00FA0100` | `character_sheet_response_listener_VA_00FA0100` | dispatches faction, guild, money, badge and sheet-detail messages | Confirmed (previous block) |
| `0x00FA1090` | `swg_cui_character_sheet_update_VA_00FA1090` | periodic/event-driven player/faction/presentation refresh | Strong |

## Data ownership

The constructor proves direct presentation fields for:

- character name, title, species, born date and played time;
- nine attributes plus shock wounds;
- food and drink fill/back bars;
- PvP/faction state and Rebel/Imperial standings;
- force-power bar/text where applicable;
- spouse, badges, guild name/abbreviation;
- bind, bank and home location;
- lots available.

The response listener subscribes to:

| Message | Opcode | Sheet effect |
|---|---:|---|
| `FactionResponseMessage` | `0x5DD53957` | rank plus Rebel/Imperial/Hutt and named standings |
| `GuildResponseMessage` | `0x32263F20` | local player's guild identity |
| `PlayerMoneyResponse` | `0x367E737E` | parsed then discarded by this executable |
| `BadgesResponseMessage` | `0x6D89D25B` | local-player badge bitmask projection |
| `CharacterSheetResponseMessage` | `0x9B3A17C4` | bind/bank/home, spouse, lots and faction/status details |

The activation path issues the matching faction/guild/resource/detail requests.
Money remains authoritative through ordinary replicated object state because this
listener explicitly discards the legacy response after parsing.

## Replacement-client contract

- Model the sheet as a read-only view over typed replicated state plus the five
  bounded compatibility responses.
- Keep request correlation and local-player ID guards; responses for other object
  IDs must not overwrite the local sheet.
- Preserve the legacy money codec but do not use it as a second authoritative
  balance store.
- Route the stat-migration button through a typed `ShowStatMigration` action/event,
  not by coupling the sheet directly to the migration implementation.
- Source labels, badge descriptions, factions and species/title display from the
  original data/localization assets.

## Unknowns

Exact spacing, localized formatting and obsolete force/Jedi visibility rules are
presentation fidelity. They do not leave a wire format, authoritative state source
or mutation path unknown.

