# Create/DestroyClientPath debug-path messages

Date: 2026-09-13  
Client authority: `C:\Stardust-DEV\Stardust.exe`, SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`  
Server authority: local Stardust-3 source

## Protocol and handler

The main scene message listener registers both classes through name-table entries `0x015EAF40` and `0x015EAF20`.

| Message | Wire data | Client action |
|---|---|---|
| `CreateClientPathMessage` / `0x71957628` | U32 coordinate count, followed by count triples of float X/Z/Y | deserialize at `0x00AD3610`; destroy any existing path object; if the decoded coordinate vector is nonempty, construct a 0xA8-byte client path appearance/object at `0x00620AA0`, attach/activate it, and register it with the current GroundScene |
| `DestroyClientPathMessage` / `0xA75E85EB` | no message payload beyond opcode | deserialize at `0x00AD32E0`; destroy and clear the retained path object and unregister/reset the GroundScene path state |

The client computes point count from vector byte length divided by 12, confirming tightly serialized float3 points and no per-point flags. GroundScene retains at most one client path (`scene listener +0xC8`); a new Create atomically replaces the old visualization.

The server helper methods use this channel for diagnostic geometry, including bounding spheres and boxes, and explicitly reorder engine coordinates into the wire's X/Z/Y convention. This is a development/debug visualization facility, not navigation authority and not gameplay pathfinding.

## Replacement policy

Implement this late in the scene-debug layer, not in the networking proof of concept. Decode with a bounded count and overflow check, convert SWG coordinates at one boundary, and replace the previous retained line/path visualization only after the full new list validates. An empty list should clear the previous path without allocating a new object. The renderer may use a modern line-strip/debug-draw implementation; the legacy 0xA8 object layout is irrelevant.

