# PlayClientEffect message family

Date: 2026-09-13  
Client authority: `C:\Stardust-DEV\Stardust.exe`, SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`  
Server authority: local Stardust-3 packet constructors

## Shared client listener

GroundScene listener `ground_scene_client_effect_listener_VA_00522440` registers three embedded message classes: `PlayClientEffectObjectMessage`, `PlayClientEffectObjectTransformMessage`, and `PlayClientEffectLocMessage`. It type-checks the incoming GameNetworkMessage, deserializes the matching concrete class, resolves object/cell/world context, then calls one of three effect-spawn helpers.

| Server message/opcode | Client path | Fields and behavior | Confidence |
|---|---|---|---|
| `PlayClientEffectObjectMessage` / `0x8855434A` | `0x00522440` → `0x00ADCB60` → `0x00523020` | ASCII effect resource, ASCII auxiliary/hardpoint name, U64 object ID; resolve the client object and attach/play the effect at the requested auxiliary point | Confirmed |
| `PlayClientEffectLoc` / `0x02949E74` | `0x00522440` → `0x00ADD2E0` → `0x005230A0` | ASCII effect, ASCII zone name, float X/Z/Y, U64 cell ID, S32 terrain delta; resolve cell if nonzero, convert cell-local position to world position, otherwise optionally query terrain height, add the vertical delta, then create the positional effect | Confirmed principal path |

The deployed client deserializer for `PlayClientEffectLocMessage` registers effect, zone, three coordinates, cell ID and terrain-delta fields. It does **not** register the server constructor's final optional `label` string. The archive decoder tolerates the trailing server field; no read/use of that label exists in the GroundScene branch. A replacement compatible with current Stardust should parse and retain/skip it explicitly so framing remains correct, but should not invent a gameplay effect for it.

The zone-name field is retained by the message object but is not consulted by the observed GroundScene handler. Scene routing has already delivered the message to the active scene; spatial placement is determined by cell ID and coordinates. A nonzero cell is resolved to its client object/cell transform. For world-space effects the terrain service supplies surface context where applicable, and the signed terrain/vertical adjustment is added before spawning.

## Replacement contract

Both opcodes belong in the scene/effect service, not the core renderer. The service resolves a logical effect resource through the VFS, resolves object or cell identity from the replicated object model, builds a world transform, and hands the effect appearance to normal scene ownership. Missing object/cell/resource failures must be nonfatal and must not leave a partially owned effect.

