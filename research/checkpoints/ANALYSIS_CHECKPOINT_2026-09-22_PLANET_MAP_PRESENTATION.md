# Planet-map presentation runtime

Date: 2026-09-22

## Result

`SwgCuiPlanetMap` is a presentation/cache consumer over the already mapped
`PlanetMapManagerClient` snapshot and replicated waypoint collections. It selects
an original UI map page for the active planet, derives the world extent from
terrain/map metadata, converts world X/Z into page coordinates, builds a
category tree and marker widgets, and continuously updates the player's
position/heading. It does not parse network messages itself.

## Principal functions

| Address | Provisional name | Role |
|---|---|---|
| `0x00D6F830` | `swg_cui_planet_map_construct_VA_00D6F830` | Constructs the mediator and resolves map page, zoom slider, city/waypoint/name filters, tree, player icon, marker/text templates, refresh control and embedded map pages. |
| `0x00D70980` | `swg_cui_planet_map_destroy_VA_00D70980` | Releases marker/category containers, subscriptions and retained strings. |
| `0x00D70B20` | `swg_cui_planet_map_activate_VA_00D70B20` | Detects planet changes, selects map asset, requests/refreshes location data, rebuilds categories/markers and subscribes to location/waypoint/object events. |
| `0x00D70E70` | `swg_cui_planet_map_deactivate_VA_00D70E70` | Unsubscribes events, stops updates and clears generated marker widgets. |
| `0x00D71480` | `swg_cui_planet_map_handle_ui_message_VA_00D71480` | Handles drag, wheel/keyboard zoom, cursor coordinate display and marker activation/context actions. |
| `0x00D723B0` | `swg_cui_planet_map_apply_zoom_VA_00D723B0` | Computes scroll extent/zoom and rebuilds visible markers after page-size changes. |
| `0x00D724B0` | `swg_cui_planet_map_update_player_marker_VA_00D724B0` | Converts player world transform to page position and applies heading rotation. |
| `0x00D72550` | `swg_cui_planet_map_update_VA_00D72550` | Animates panning, observes resized content and updates the player marker. |
| `0x00D72A00` | `swg_cui_planet_map_rebuild_location_tree_VA_00D72A00` | Builds category/subcategory entries from the semantic location snapshot and associates visibility/icon state. |
| `0x00D732E0` | `swg_cui_planet_map_clear_generated_markers_VA_00D732E0` | Returns/removes generated icon and label widgets and clears marker lookup state. |
| `0x00D73510` | `swg_cui_planet_map_rebuild_markers_VA_00D73510` | Combines selected categories, city/waypoint filters and current player/waypoint state. |
| `0x00D73780` | `swg_cui_planet_map_place_filtered_markers_VA_00D73780` | Validates map positions, resolves waypoint objects, clones icon/label templates, applies color/active visibility and clips labels to the page. |
| `0x00D73F60` | `swg_cui_planet_map_select_planet_asset_VA_00D73F60` | Selects a child of `pageMaps` by localized/current planet key, falls back to `default`, and establishes world extent. |
| `0x00D74220` | `swg_cui_planet_map_save_settings_VA_00D74220` | Persists selected category/subcategory and three visibility filters. |
| `0x00D74320` | `swg_cui_planet_map_load_settings_VA_00D74320` | Restores those settings and rebuilds the tree. |

## Original asset dependency

The constructor requires the authored `/PlanetMap`-family UI composition,
including `pageMaps`, marker templates and category-specific sample buttons. The
recognized built-in marker styles are generic, bank, cantina, cloning facility,
guild, hotel, medical center/hospital, shuttleport, starport, theme park, city
and waypoint. Planet imagery is selected by name from children of `pageMaps`; a
missing planet page logs a warning and uses `default`.

This is an important original-asset contract: a replacement may render the map
with native Godot/UI primitives, but it must import the same authored planet map
image/page mapping and semantic category names. Hard-coding only the known
planets would discard overlay/custom content.

## Coordinate and event model

- Terrain extent is obtained from `ProceduralTerrainAppearance` when available,
  then may be overridden by a property on the selected map widget.
- World horizontal coordinates are transformed into the map-page rectangle;
  invalid transformed positions are diagnosed and omitted.
- Player position and heading come from the live world transform.
- Location snapshots come from `PlanetMapManagerClient`; waypoints come from
  replicated `PlayerObject` collections.
- Added/removed objects plus name, waypoint color, waypoint location and active
  state events drive rebuilding without a new map request.
- Zoom/pan/filter state is local UI state and is persisted separately from
  server data.

## Replacement-client contract

Separate: (1) location/waypoint data service, (2) planet-map asset resolver,
(3) world-to-map transform, and (4) UI presentation. Preserve original map
assets and category keys. Use stable U64 object IDs for marker identity and keep
native engine object references out of saved settings and wire data.

## Confidence

- Lifecycle, event subscriptions and cache ownership: **Confirmed**.
- Asset-page selection/fallback and semantic marker families: **Confirmed**.
- Terrain-derived extent and world-to-map conversion: **Confirmed behaviorally**.
- Exact historical property names behind decompiler global tokens: **Not assigned;
  raw tokens/behavior should be recovered when implementing the importer**.
