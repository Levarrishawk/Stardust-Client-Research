# Planet Map Manager Client

Date: 2026-09-21  
Target: `C:\Stardust-DEV\Stardust.exe`

## Result

`PlanetMapManagerClient` is the client cache and presentation bridge for planetary map locations. It combines three inputs:

1. server-provided `GetMapLocationsResponseMessage` snapshots;
2. client object/waypoint state used to augment/filter the snapshot;
3. original data tables defining categories and planets where the map is disabled.

It is neither a renderer nor the authoritative registry. It requests a snapshot for the current scene, validates that the reply still matches the current planet, stores locations by category/subcategory, and publishes `LocationsRequested` / `LocationsReceived` events consumed by `SwgCuiPlanetMap`.

## Lifecycle and principal functions

| VA | Provisional name | Behavior |
|---|---|---|
| `0x004B8600` | `planet_map_manager_client_install` | Registers listeners for `GetMapLocationsResponseMessage` and `Game::SCENE_CHANGED`; loads the disabled-planet table. **Confirmed.** |
| `0x004B87E0` | `planet_map_manager_client_listener_receive` | Dispatches map responses through the generated parser and clears caches on scene change. **Confirmed.** |
| `0x004B8930` | `planet_map_manager_client_clear` | Clears request state, revision tokens, category buckets and retained location arrays. **Confirmed.** |
| `0x004B9270` | `planet_map_manager_client_request_locations` | Coalesces duplicate requests; sends the reliable request or schedules the offline/single-player generator; emits `LocationsRequested(true)`. **Confirmed.** |
| `0x004B9D40` | `planet_map_manager_client_apply_locations_response` | Validates outstanding request and planet, replaces changed arrays by token, rebuilds category buckets, augments player waypoints, emits completion. **Confirmed.** |
| `0x004B7E40` | `planet_map_manager_client_init_special_categories` | Initializes twelve well-known category names used by client object discovery. **Strong.** |
| `0x004B8190` | `planet_map_manager_client_generate_offline_locations` | Generates synthetic/randomized data in single-player/offline mode; not used by normal Stardust networking. **Confirmed branch role.** |

The twelve explicit category strings are `bank`, `cantina`, `capitol`, `cloningfacility`, `garage`, `guild`, `hotel`, `medicalcenter`, `hospital`, `shuttleport`, `starport`, and `themepark`.

## Network contract

The prior packet block is retained rather than duplicated. The now-correlated manager chain is:

| Message/opcode | Parser | Manager handler | Resulting state |
|---|---|---|---|
| `GetMapLocationsMessage` / `0x1A7AB839` C→Z | constructor `0x00ACEAC0` | request `0x004B9270` | ASCII planet plus three U32 cache/revision tokens; reliable send; request-in-progress flag and `LocationsRequested` event |
| `GetMapLocationsResponseMessage` / `0x9F80464C` Z→C | `0x00ACF5A0` and element codec | listener `0x004B87E0` → apply `0x004B9D40` | validate planet/request; replace up to three arrays whose tokens changed; rebuild category map; publish `LocationsReceived` |

Each 32-byte client location value corresponds to the server's wire element:

```text
objectId    : uint64
displayName : Unicode string
x           : float32
y/mapZ      : float32
category    : uint8
subcategory : uint8
icon        : uint8
```

Stardust currently populates only the first array and sends both remaining arrays empty, followed by three zero tokens. The client genuinely supports three independently revisioned arrays, so a replacement parser must consume all three even if the present server does not use them. The manager ignores stale cross-planet replies with the diagnostic `received planet locations for %s, but we are now on %s`; it also diagnoses a reply received with no request outstanding.

Server `MapLocationEntry::insertToMessage` confirms that filtering is authoritative and per-player: faction-only categories and faction/Jedi-restricted cloning facilities can be omitted before serialization. The client must not manufacture hidden server locations from assets.

## Data-table contracts

### Disabled planets

Effective `datatables/planetary_map/disabled_planetary_map.iff` comes from `stardust_3_01.tre` and has one string column, `Planet`. Its five rows are:

```text
tutorial, dungeon1, dungeon2, jakku, elysium
```

The manager stores these names at install time. This means a valid `datatables/clientregion/jakku.iff` can coexist with a disabled planetary map: region-label availability and map availability are separate policies.

### Category registry

Effective `datatables/player/planet_map_cat.iff` comes from `mtg_patch_013_configurable_02.tre` and contains **75 rows** with seven columns:

| Column | Type | Role |
|---|---|---|
| `name` | string | stable category key |
| `index` | int32 | one-byte wire category/subcategory ID |
| `iscategory` | bool/int | top-level category marker |
| `issubcategory` | bool/int | subcategory marker |
| `canBeActive` | bool/int | supports active/map behavior |
| `faction` | string | empty, `rebel`, or `imperial` |
| `factionVisibleOnly` | bool/int | server visibility policy input |

The table deliberately contains aliases/duplicate indices: `garage` occurs at indices 6 and 55, while `sf_rebel_hq`/`sf_imperial_hq` reuse indices 47/48. Therefore a replacement must preserve row/name lookup and cannot assume index is a unique database primary key.

## Cache and UI behavior

- One byte guards an outstanding request; repeat calls are coalesced.
- Three U32 revision/cache tokens are echoed on the next request and compared independently on response.
- Locations are regrouped under their category byte; the client then incorporates scene waypoints for the current planet.
- A location whose displayed category string is `invisible` is excluded by local object-derived augmentation logic.
- `SwgCuiPlanetMap` subscribes to both typed manager events plus container/name/waypoint-color/waypoint-location changes, so the UI is a consumer of semantic cache events rather than the owner of protocol parsing.

## Replacement implications

1. Keep the protocol/cache layer outside the UI and expose immutable location snapshots/events.
2. Parse all three arrays and all three tokens, even with today's first-array-only Stardust producer.
3. Treat U64 object IDs, UTF-16 names, float32 coordinates and byte category fields as fixed wire widths in x64.
4. Load the original category and disabled-map IFFs through the normal overlay VFS.
5. Preserve duplicate category indices and aliases.
6. Keep server filtering authoritative; assets describe categories, not which per-player entries are visible.
7. The offline random-location generator and legacy STL containers need not be reproduced for the networked client.

