# Client Region Manager

Date: 2026-09-21  
Target: `C:\Stardust-DEV\Stardust.exe`

## Result

`ClientRegionManager` is a local, asset-driven named-region service for ground scenes. It is not the replicated server `CityRegion` object model. On a scene change it loads `datatables/clientregion/<scene>.iff`, materializes circular regions as spheres in a spatial subdivision, and every five seconds determines the smallest-radius region containing the player. It retains the current localized region identifier and optionally reports transitions through the UI.

## Lifecycle and update path

| VA | Provisional name | Confidence and evidence |
|---|---|---|
| `0x004AD200` | `client_region_manager_install` | **Confirmed** by diagnostic. Initializes state, subscribes to `Game::SCENE_CHANGED`, and registers debug flag `ClientGame/drawClientRegions`. |
| `0x004AD300` | `client_region_manager_scene_changed_listener` | **Confirmed** typed listener; forwards the new scene name to the loader. |
| `0x004AD440` | `client_region_manager_update` | **Confirmed** accumulator; calls the current-region resolver each time the compiled 5.0-second interval elapses. |
| `0x004AD470` | `client_region_manager_clear_current_name` | **Confirmed** clears the retained localized region identifier. |
| `0x004AD490` | `client_region_manager_load_scene` | **Confirmed** constructs `datatables/clientregion/<scene>.iff`, reads all rows and inserts sphere records. Deliberately skips `simple` and `tutorial`. |
| `0x004ADB20` | `client_region_manager_debug_draw` | **Confirmed** draws every region sphere when `drawClientRegions` is enabled. |
| `0x004ADC60` | `client_region_manager_resolve_player_region` | **Confirmed** queries spheres containing the player, selects the smallest radius, changes retained region state, and conditionally emits UI feedback. |

The installed listener and teardown are ordinary `MessageDispatch` ownership. Scene loading first destroys the old spatial tree/records, so region data cannot leak across planet changes.

## Asset schema and installed corpus

Every effective file uses DataTable-IFF `DTII/0001` with the same four columns:

| Column | Type | Meaning |
|---|---|---|
| `Name` | string | localized StringId, normally `@<planet>_region_names:<key>` |
| `X` | float32 | world X center |
| `Z` | float32 | world Z center |
| `Radius` | float32 | circular ground-plane radius |

The runtime sphere center is `(X, 0, Z)`. Containment therefore represents a circular ground-plane region through the sphere-tree service; the query point is the player world position. When multiple records overlap, `0x004ADC60` explicitly selects the record with the smallest `Radius`, making the most specific region win.

The configured overlay contains **25 effective client-region tables and 78 rows**. Ten original ground-planet tables resolve from `mtg_patch_013_configurable_02.tre`; later planet tables come from `mtg_planets.tre`, `mtg_patch_022.tre`, and Stardust's `stardust_3_01.tre`. Stardust-specific winners include Alderan, Lothal, Moraband and other custom planets. Example:

```text
datatables/clientregion/lothal.iff
  @lothal_region_names:capital_city, X=-1297, Z=-3146, Radius=800
```

Tatooine has nine records, including deliberate overlapping records with the same localized name (for example two Mos Eisley and two Mos Entha circles). The smallest-radius policy supports irregular city coverage as unions of circles while still returning one semantic name.

## Transition behavior

The manager retains a StringId-like region name. At each five-second evaluation:

1. Obtain the player object and its world position.
2. Query the sphere spatial subdivision for containing regions.
3. Select the containing record with minimum radius.
4. If no record contains the player, clear the retained name.
5. If the selected name differs, retain its center/radius/name and, when the user-facing preference is enabled, route a localized transition message through the UI.

Executable strings `region_entered`, `region_left`, and configuration key `showClientRegionChanges` corroborate the presentation role. The first missing-table/no-current-region state is suppressed by a one-byte startup/transition guard, avoiding a spurious leave message while a scene is settling. Exact string-table ownership of the two tokens is not yet named; the state behavior is confirmed.

## Server correlation

Stardust server scripts define `CityRegion`/active-area records independently, and `CityRegion::isClientRegion()` means server city rank `RANK_CLIENT`. Those records are used for zoning, taxes, structure placement, missions and other authoritative rules. The executable's table is a presentation/location-label resource and is not synchronized over the network.

Consequences:

- matching names and approximate geometry may be desirable, but the client table cannot authorize placement, taxes, PvP, spawning, or travel;
- a replacement should preserve server authority even if it consumes the original client table for UI location labels;
- custom planets require their corresponding client-region IFF and STF resources in the mounted overlay.

## Replacement contract

Implement this as an asset-backed `RegionLabelService`, not as the authoritative gameplay region manager:

- load the four-column original DataTable-IFF unchanged;
- index circles spatially or linearly (78 installed rows is tiny);
- evaluate at five-second cadence or on significant player movement;
- select the smallest-radius containing row;
- resolve the StringId through the original STF pipeline;
- expose semantic enter/leave/current-region events to UI consumers;
- retain all coordinates/radii as serialized float32 while allowing native x64 internal objects.

The legacy sphere-tree and its pointer-heavy node handles do not need to be reproduced.

