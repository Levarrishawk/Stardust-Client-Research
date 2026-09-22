# Interior Layout ILF Runtime Closure

Target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Result

The deployed `.ilf` family is no longer tentative. `INLY/0000` is a building-furnishing resource containing repeated `NODE` chunks. Each node is:

1. a null-terminated object-template path;
2. a null-terminated portal-cell name;
3. twelve little-endian F32 values forming the authored 3x4 transform.

The Stardust server reader independently agrees with this byte order. The client executable supplies the missing runtime behavior.

## Executable path

`client_interior_layout_manager_install_VA_0059B500` registers `ClientInteriorLayoutManager`, reads `ClientGame.disableLazyInteriorLayoutCreation`, registers a removal callback, and installs the manager in the client application sequence.

The normal frame path calls `client_interior_layout_manager_apply_pending_world_layouts_VA_0059B5A0`. Unless lazy creation is disabled, it walks current world objects, rejects the player and invalid/non-building candidates, reaches the building/portal-layout resource, and iterates cell/layout records not yet materialized.

`client_interior_layout_manager_apply_building_layout_VA_0059B7B0` is the bounded per-building path. It validates the building and layout, resolves every authored cell name against the building's portal property, creates each node's object from its object-template path, attaches the object to that cell, copies the 48-byte transform, applies position/transform notification, adds the object to the client world, and releases the creation reference. Failed cell or object resolution skips that node rather than aborting the whole layout.

The eager and per-building paths share the same object creation, cell attachment, transform application, world insertion, and release sequence. The manager therefore owns client-local furnishing presentation; these objects are not evidence of server-replicated scene objects.

## Configuration distinction

- `ClientGame.disableLazyInteriorLayoutCreation` controls the manager's automatic world scan.
- `ClientGame.useInteriorLayoutFiles` is a separate default-true option exposed through the ClientGame option table.

They must not be collapsed into one setting. The former selects creation policy; the latter controls whether ILF content is used.

## Effective configured corpus

`tools/python/ilf_corpus_inventory.py` applies the configured TRE priority/tombstone rules and parses every effective ILF:

| Measure | Result |
|---|---:|
| live effective ILFs | 297 |
| tombstones | 1 |
| versions | 297 x `INLY/0000` |
| nodes | 87,937 |
| distinct object templates | 2,278 |
| distinct cell names | 664 |
| parse failures | 0 |

The 411 previously reported values are archive record copies, not effective winners. Live winners are supplied by `mtg_patch_012_configurable_01.tre` (170), `mtg_patch_017.tre` (125), `mtg_planets.tre` (1), and `stardust_3_00.tre` (1).

## Replacement contract

- Decode all twelve little-endian transform floats at fixed 32-bit width regardless of x64 internals; IFF chunk sizes remain big-endian.
- Resolve node templates through the normal overlay-aware object-template/resource system.
- Resolve cell names against the instantiated portalized building, not a global scene namespace.
- Instantiate ILF furnishings as client-local presentation objects unless independent server replication exists.
- Treat a missing template or cell as a node-local failure.
- Preserve authored cell-relative transforms and normal object/world insertion hooks.
- Permit lazy, eager, or streamed implementation internally, but prevent duplicate materialization and remove locally created furnishings with their owning building/layout.

## Confidence

- File grammar and deployed version/corpus: **Confirmed** by complete mechanical parsing and the server reader.
- Manager install, per-frame policy, object creation, cell binding, transform and world insertion: **Confirmed** by executable code.
- Historical original class/member names beyond RTTI/config strings: **Unknown**; provisional names remain explicit.
