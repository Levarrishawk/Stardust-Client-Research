# Terrain dirty-region callers and lifecycle

Date: 2026-09-21  
Target: `C:\Stardust-DEV\Stardust.exe`

## Result

The external caller family for `ClientProceduralTerrainAppearance` virtual slot `+0x168` is closed sufficiently for replacement architecture.

### Confirmed by code and RTTI

- `0x0085F820` is `ClientProceduralTerrainAppearance` vtable entry 90 (`+0x168`). It appends a 16-byte world-space rectangle to the appearance dirty-region queue at `+0x1F8`.
- The live game-object caller is RTTI class `ClientObjectTerrainModificationNotification`; its vtable begins at `0x015DFC34`. Entries at `0x015DFC38..0x015DFC48` are `0x006129D0`, `0x00612A20`, `0x00612A70`, `0x00612AA0`, and `0x00612AD0`.
- `0x006129D0` handles attachment/addition: it registers the terrain-modification object through `0x00B5E2E0`, then calls common updater `0x00612B00(object, false, true)` so only the new region is dirtied.
- `0x00612A20` handles removal: it unregisters through `0x00B5E3E0`, then calls `0x00612B00(object, true, false)` so only the old region is dirtied.
- `0x00612A70` and `0x00612AD0` are transform-change callbacks. When the object is in-world (`0x00B222C0`), both old and new regions are dirtied.
- `0x00612AA0` is the paired callback which deliberately dirties neither region; it still performs the common transform/modifier update.
- `0x00612B00` obtains the modifier state through `0x00557770`, snapshots its prior valid rectangle at `+0x54..+0x60`, updates world position/heading and recalculates it, snapshots the new rectangle, and independently submits the requested old/new rectangles through the global terrain appearance at `0x01916424`.
- The template corpus names the source field `terrainModificationFileName`; the client executable also contains the exact diagnostic/config names `ClientObjectTerrainModificationNotification::install` and `reportBuildingObjectTerrainModificationNotification`.

### Call-site discrimination

A raw search for indirect calls at virtual displacement `+0x168` found four instruction sites. Only the two calls in `0x00612B00` dereference the global terrain appearance (`0x01916424`) and carry one 16-byte rectangle. `0x00759DF0` and `0x00B5CE50` use unrelated receiver globals/object members and signatures; they are coincidental uses of the same vtable byte offset, not terrain dirty-region producers.

## Rebuild contract

```text
terrain-modifying object add/remove/move
  -> ClientObjectTerrainModificationNotification
  -> update terrain-modifier placement and bounds
  -> queue old rectangle and/or new rectangle
  -> ClientProceduralTerrainAppearance dirty queue
  -> cancel/intersect pending chunk work
  -> rebuild affected ClientChunks
```

A replacement must preserve the behavior, not the notification class layout: derive the modifier footprint from the original terrain modification asset, invalidate both old and new regions on movement, invalidate only the new region on addition, and only the old region on removal. Region invalidation must drive both visible chunk regeneration and any terrain-query cache invalidation.

## Server correlation

The Stardust server independently loads the same building-template `terrainModificationFileName`, installs/removes a `TerrainGenerator` keyed by the 64-bit object ID, and clears its height cache (`TerrainManager::addTerrainModification`, `removeTerrainModification`, and `clearCache`). This does not transmit the modifier geometry as a special packet: client reconstruction is asset/template driven when the corresponding scene object exists. Client and server therefore need identical terrain-modification assets and placement semantics, while their cache implementations may differ.

## Confidence

- RTTI class, vtable membership, lifecycle polarity, rectangle width, and terrain virtual dispatch: **confirmed**.
- Semantic callback labels (add/remove/transform phases): **strongly inferred** from paired registration calls and old/new flags.
- Exact unpublished names of the rectangle and modifier-state fields: **unknown and unnecessary for compatibility**.

