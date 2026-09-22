# GroundScene Named-Message Listener Inventory

Baseline: `C:\Stardust-DEV\Stardust.exe`, SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.

This is a mechanically recovered inventory of literal named-message checks in the central `GroundScene` listener region `0x0051BD10`–`0x0051E5A0`. The helper `tools/scene_listener_inventory.py` locates literal names passed to the executable's name-hash comparison helper `0x00AC4D10` or gated-name helper `0x0051E670`. It does not infer semantics from names alone. Detailed field/action evidence belongs in `PACKET_HANDLER_MAP.md`.

| Branch VA | Literal message | Route status |
|---:|---|---|
| `0x0051BD65` | `SceneCreateObjectByName` | Confirmed; shares create handler with CRC variant |
| `0x0051BD7F` | `SceneCreateObjectByCrc` | Confirmed |
| `0x0051BD97` | `SceneDestroyObject` | Confirmed |
| `0x0051BEEC` | `UpdateContainmentMessage` | Confirmed |
| `0x0051C206` | `CmdSceneReady` | Confirmed |
| `0x0051C287` | `UpdateCellPermissionMessage` | Confirmed |
| `0x0051C314` | `SceneEndBaselines` | Confirmed |
| `0x0051C41E` | `ServerTimeMessage` | Confirmed |
| `0x0051C6BE` | `BaselinesMessage` | Confirmed |
| `0x0051C76D` | `DeltasMessage` | Confirmed |
| `0x0051C823` | `UpdateTransformMessage` | Confirmed |
| `0x0051C9AC` | `UpdateTransformWithParentMessage` | Confirmed |
| `0x0051CB49` | `PlayMusicMessage` | Confirmed |
| `0x0051CBC8` | `ClientOpenContainerMessage` | Confirmed |
| `0x0051CCF6` | `LogoutMessage` | Confirmed principal lifecycle calls |
| `0x0051CD19` | `DebugTransformMessage` | Confirmed transform + target-reference update |
| `0x0051CFED` | `CreateProjectileMessage` | Confirmed |
| `0x0051D2AD` | `CreateMissileMessage` | Confirmed |
| `0x0051D65C` | `UpdateMissileMessage` | Confirmed |
| `0x0051D783` | `ShipUpdateTransformMessage` | Confirmed |
| `0x0051DAC8` | `ShipUpdateTransformCollisionMessage` | Confirmed |
| `0x0051DBD6` | `DogfightTauntPlayerMessage` | Inline decode and UI/localization action Strong; field labels Unknown |
| `0x0051DFEF` | `HyperspaceMessage` | Confirmed principal object-transition route |
| `0x0051E074` | `IsFlattenedTheaterMessage` | Confirmed |
| `0x0051E11B` | `AiDebugString` | Recognized and ignored in this build |
| `0x0051E130` | `SaveTextOnClient` | Recognized and ignored in this build |
| `0x0051E145` | `LaunchBrowserMessage` | Confirmed |
| `0x0051E1D5` | `SceneCreateObjectByName` | Second create-format branch; confirmed parser split |

## Coverage statement

All 28 literal checks in this bounded listener region now have stable branch addresses and recognition status. Twenty-seven have a confirmed principal route or deliberate no-op behavior; `DogfightTauntPlayerMessage` has a strongly identified UI/localization result but its two source-field meanings remain unknown. The duplicate by-name create branches still need their format distinction documented. This is 100% inventory coverage of this one listener region, not 100% of client messages or executable functions.

## Replacement implications

- Use a generated opcode/name registry feeding typed decoders; do not reproduce a monolithic comparison chain.
- Keep object lookup and state mutation behind a main-thread/transactional command boundary.
- Treat server-directed browser launching as privileged UI behavior with scheme allowlisting and confirmation.
- Preserve deliberate no-op handling for debug/file-writing messages unless a safe development-only facility is explicitly enabled.
- Keep space projectile/missile simulation separate from replicated scene-object ownership.

## Next bounded work

1. Resolve `DebugTransformMessage` parser fields and the target-object transform action.
2. Resolve `DogfightTauntPlayerMessage` local UI/localization construction.
3. Enumerate other listener classes (login, connection-server, chat, UI/controller) using the same mechanical method.
