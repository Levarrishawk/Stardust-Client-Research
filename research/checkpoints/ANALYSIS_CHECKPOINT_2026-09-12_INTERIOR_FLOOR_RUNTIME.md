# Analysis checkpoint: interior floor loading and movement runtime

Date: 2026-09-12

## Scope

Client-side `FLOR` loading, floor-location traversal, cross-floor movement and replicated cell permission. Evidence is static disassembly of `Stardust.exe`, correlated with Stardust server `PortalLayout`, `CellProperty` and `FloorMesh` readers.

## Confirmed findings

### The client directly supports all `FLOR` versions 0000 through 0006

`floor_mesh_load_VA_01273B80` enters `FLOR`, reads its version tag and dispatches through seven dedicated loaders:

| Version | Client loader |
|---|---|
| `0000` | `0x01273D10` |
| `0001` | `0x01274050` |
| `0002` | `0x01274400` |
| `0003` | `0x01274720` |
| `0004` | `0x01274A50` |
| `0005` | `0x01274CD0` |
| `0006` | `0x012751E0` |

Unsupported versions emit `FloorMesh::Invalid version`. After load, the client flips stored triangle normal components, computes derived floor data, and for pre-v5 formats runs an additional compatibility/build pass.

### `FLOR/0006` stored acceleration and path data are consumed

The deployed-version loader `0x012751E0` confirms:

- `VERT`: explicit count followed by 12-byte XYZ vertices.
- `TRIS`: explicit count followed by disk records expanded into **100-byte client runtime triangle records**.
- optional `BTRE`: allocated and parsed through `0x01292890`, which enters `BTRE/0000`; therefore the client consumes the stored bounding tree rather than necessarily rebuilding it.
- optional `BEDG`: read into three separate internal edge collections according to the triangle edge/portal classification.
- optional `PGRF`: loaded through the registered path-graph factory when installed/enabled, otherwise skipped as a form.

This matches the server's v6 structural reader while adding client-specific runtime sizes and ownership.

### Floor movement is stateful graph traversal

The runtime uses a `FloorLocator`-like 64-byte state object with at least:

- current floor pointer;
- current triangle/index state;
- world/local position;
- collision/path-walk output;
- owning floor/cell recovery.

`test_floor_collision_VA_012647B0`:

1. requires a non-null starting floor (logs and returns permissively if absent);
2. initializes an output locator;
3. calls the floor path walker at `0x0126A600` with start/end locators and triangle hints;
4. interprets `PathWalkResult` values;
5. copies the destination collision point;
6. resolves the destination floor owner and rejects malformed ownerless results conservatively.

`collision_world_can_move_on_floor_VA_01267380` is the broader motion solver. It constructs start/end contact points using the moving object's contact height, invokes floor path walking, returns distinct status codes for blocked, accepted and cell/floor-transition cases, and can recursively continue after a boundary transition. A global recursion counter caps this process at five levels; exceeding it logs `CollisionWorld::canMoveOnFloor - infinite recursion detected` and returns a failure/status result.

This proves that interior motion cannot be replaced by a single mesh raycast. It depends on triangle adjacency, boundary-edge semantics, portal/floor ownership and cross-cell transitions.

### Replicated cell permission is a live flag, not cache invalidation

`cell_object_ctor_VA_006E58A0`:

- initializes the replicated allow-entry byte at `CellObject +0x29C` to true;
- registers the cell-object state;
- creates and attaches a 100-byte `PortalProperty` object.

`cell_object_set_allow_entry_VA_006E6850` only stores the incoming byte at `+0x29C`. It does not rebuild portal geometry, collision meshes or visibility state. Enforcement therefore occurs at movement/entry decision points reading current cell state, not by invalidating cached floor resources.

## Strong inferences

- The three v6 `BEDG` vectors separate uncrossable/connected/blocking edge families, consistent with server edge flags. Exact vector-to-enum labels need one more consumer trace.
- Floor path-walk result `10` represents a boundary condition that may require recursive continuation into another floor/cell when a control flag is enabled. Exact enum label is not yet proven.
- Result `4` participates in special floor/cell identity checks; exact historical enum label is unknown.

## Replacement requirements

1. Decode all installed `FLOR` versions, with v6 as the reference implementation and compatibility loaders for older assets.
2. Preserve disk structures exactly, but convert them to pointer-free 64-bit runtime resources.
3. Retain triangle adjacency, per-edge flags, portal IDs, owner links and cross-cell transitions.
4. Parse stored `BTRE` or deterministically build a validated modern acceleration structure with equivalent query results.
5. Keep floor collision/navigation resources immutable and shareable; keep locator, owner and transition state runtime-local.
6. Consult replicated cell allow-entry state dynamically during transition decisions.

## Confidence and open items

| Claim | Confidence |
|---|---|
| Client loaders for FLOR 0000..0006 | Confirmed |
| v6 consumes VERT, TRIS, optional BTRE, BEDG and PGRF | Confirmed |
| runtime triangle record is 100 bytes | Confirmed |
| movement uses adjacency/path-walk rather than raycast alone | Confirmed |
| recursion limit is five | Confirmed |
| exact PathWalkResult enum names | Unknown |
| exact BEDG-vector enum mapping | Strongly inferred |
| exact portal-door collision coupling | Unknown |

