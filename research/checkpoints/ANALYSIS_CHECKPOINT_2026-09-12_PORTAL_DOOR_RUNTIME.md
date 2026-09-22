# Analysis checkpoint: portal-layout, cell and door runtime

Date: 2026-09-12

## Scope

Client-side `PRTO`, `CELL` and `PRTL` loading; post-load portal linking; runtime cell floor ownership; portal crossing; and data-driven door construction. Evidence is static disassembly of `Stardust.exe`, correlated with the Stardust server schema and portal-layout readers.

## Confirmed findings

### Portal-layout formats are explicitly versioned

`portal_layout_load_VA_00B461B0` enters `PRTO` and dispatches versions `0000` through `0004`:

| Version | Client loader |
|---|---|
| `0000` | `0x00B46260` |
| `0001` | `0x00B46710` |
| `0002` | `0x00B46BF0` |
| `0003` | `0x00B47210` |
| `0004` | `0x00B47790` |

The adjacent `portal_layout_read_crc_VA_00B45BA0` is a limited CRC probe/extractor rather than the full loader. It recognizes the same outer versions and reads the version-appropriate CRC location.

`portal_cell_template_load_VA_00B440B0` dispatches `CELL/0001` through `CELL/0005`. `portal_reference_load_VA_00B43880` likewise dispatches `PRTL/0001` through `PRTL/0005`.

### Layout loading builds geometry, cells and connectivity

The PRTO loaders populate separate collections for portal geometry and cell templates, retain a layout CRC, and in v2 and later can load an optional `PGRF` through the registered path-graph factory. A common post-load pass iterates cell portal references and invokes `0x00B43E80`, resolving references into connected-neighbor relationships.

This makes the file a coupled visibility/movement graph, not merely a collection of interior meshes.

### Runtime cells acquire shared floor resources

The CellProperty construction path:

1. creates and attaches a cell appearance when the cell template names one;
2. obtains the floor resource name from the cell template;
3. acquires the corresponding Floor through the resource/cache function at `0x0128DF80`;
4. stores both the logical cell-template pointer and runtime Floor pointer;
5. logs explicit diagnostics if required floor ownership cannot be established.

`cell_property_find_destination_cell_VA_00B2B240` iterates portal references, transforms the tested segment between owner/local frames where required, tests the portal polygon, and returns the nearest connected destination CellProperty. This is the geometric bridge used when motion or visibility crosses a cell boundary.

### Portal doors are data-driven runtime objects

`portal_runtime_create_door_VA_00B53500` conditionally creates a door for a portal record. It looks up the portal's door-style name in the global door-style data table, reads columns 1 through 17, constructs a `DoorObject`, applies the portal transform and registers the door and its related objects.

The server SQL schema supplies the exact column semantics and matches the client reads:

1. frame appearance;
2. primary door appearance;
3. secondary door appearance;
4. secondary-door flip;
5. move X;
6. move Y;
7. move Z;
8. open time;
9. close time;
10. springiness;
11. smoothness;
12. trigger radius;
13. force-field flag;
14. open-begin effect;
15. open-end effect;
16. close-begin effect;
17. close-end effect.

`door_object_ctor_VA_0128FF40` initializes the runtime object. `door_object_create_appearances_VA_012902D0` creates the base child and loads frame/door appearances, with a distinct force-field attachment/effect path. `door_object_create_collision_property_VA_01290560` installs a dedicated `DoorCollisionProperty` and initializes its contact/collision shape from the configured motion vector. `door_object_create_portal_barrier_VA_012906F0` creates the portal barrier object whose enabled state participates in open/closed traversal.

RTTI independently confirms `DoorObject`, `DoorCollisionProperty`, `CellProperty` and `PortalProperty` classes.

## Architectural consequence

Interior import must preserve the PRTO cell/portal graph, portal polygons, floor-resource associations, transforms, CRC and optional path graph. Door behavior should be recreated as a runtime controller layered onto that graph, using the original data-table values. Treating doors as ordinary animated meshes would lose portal blocking, collision and cross-cell traversal semantics.

## Confidence and open items

| Claim | Confidence |
|---|---|
| PRTO supports versions 0000..0004 | Confirmed |
| CELL and PRTL support versions 0001..0005 | Confirmed |
| post-load pass resolves cell/portal neighbors | Confirmed broad behavior |
| cells acquire named Floor resources | Confirmed |
| portal crossing tests polygon and returns connected destination | Confirmed broad behavior |
| door-style columns and semantics | Confirmed by executable/server correlation |
| doors own appearance, collision and portal-barrier components | Confirmed |
| precise open/close state-machine methods and network triggers | Unknown; next trace |
| exact visibility traversal relationship to the same portal graph | Strongly inferred; consumer trace incomplete |

