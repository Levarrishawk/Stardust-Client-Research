# Data Storage UI

Date: 2026-09-22

## Result

`SwgCuiDataStorage` is a four-pane object browser over already replicated
player-owned state. It does not define a new network protocol. It projects
waypoints, generic data objects, points of interest and draft schematics,
tracks capacity, and reacts to ordinary container membership changes.

## Client evidence

| Address | Provisional name | Role | Confidence |
|---|---|---|---|
| `0x00D414C0` | `swg_cui_data_storage_construct_VA_00D414C0` | constructs the four containers, two capacity displays, waypoint controls, filters and object viewers | Confirmed |
| `0x00D42190` | `swg_cui_data_storage_new_waypoint_VA_00D42190` | creates a waypoint from the current scene/location context | Strongly inferred |
| `0x00D421E0` | `swg_cui_data_storage_waypoint_option_changed_VA_00D421E0` | applies current-planet, single-waypoint and indicator visibility options | Confirmed controls; subordinate setting labels Strong |
| `0x00D42320` | `swg_cui_data_storage_tab_changed_VA_00D42320` | switches active object provider/view and selection state by pane | Strongly inferred |
| `0x00D42A00` | `swg_cui_data_storage_save_settings_VA_00D42A00` | persists active tab, waypoint options and per-pane view state | Confirmed |
| `0x00D42AC0` | `swg_cui_data_storage_load_settings_VA_00D42AC0` | restores the same mediator settings and refreshes affected views | Confirmed |

RTTI identifies three concrete filters (`FilterWps`, `FilterData`, `FilterPoi`)
and subscriptions to `ClientObject::Messages::AddedToContainer` and
`RemovedFromContainer`. A separate callback controls display of group
waypoints.

## Pane model

```text
SwgCuiDataStorage
  |- Waypoints       capacity bar/label + create/filter/display controls
  |- Data            generic stored data/intangible objects
  |- Points of Interest
  `- Drafts          draft schematic objects
```

The constructor binds four distinct `container*` pages and installs object
viewer/provider helpers for each. Waypoints use a dedicated capacity display;
the remaining stored-data side uses the general capacity display.

## State and authority boundary

- Membership is sourced from the replicated player/datapad/container object
  graph and updated by added/removed events.
- Waypoint attributes and activation are part of the already-mapped waypoint
  object/manager and command paths.
- Draft schematic availability derives from replicated progression and the
  original schematic/skill assets.
- POI identity and display metadata are content-driven.
- Tab choice, filters and view modes are local `.uis` mediator settings.

The UI therefore must never manufacture authoritative possession. Creation or
mutation actions delegate to the established waypoint/object command layer;
subsequent replicated state is final.

## Original-asset dependency

The four-pane layout, viewer styles, icons, localized labels, POI definitions
and draft schematic metadata depend on the original asset corpus. A modern UI
may change layout, but it must consume those definitions and preserve their
logical identities.

## Replacement-client requirement

Build a generic filtered-object collection view over stable object handles,
with category-specific adapters for waypoint, data, POI and draft records.
Subscribe to domain-level containment and waypoint events rather than parsing
packets in the UI. Keep capacity values and ownership server-authoritative and
persist only presentation preferences locally.

## Residual uncertainty

Some historical filter flags and exact per-pane sort defaults retain naming
uncertainty. The object sources, categories, lifecycle events, persistence and
authority boundaries are closed.
