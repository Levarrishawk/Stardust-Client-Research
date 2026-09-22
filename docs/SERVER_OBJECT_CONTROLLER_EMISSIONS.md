# Server-to-Client Object-Controller Emissions

Baseline: current local Stardust-3 source correlated with `C:\Stardust-DEV\Stardust.exe` (SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`) on 2026-09-11.

## Static inventory result

`tools/server_object_controller_usage.py` found 71 direct construction sites representing **51 distinct constant controller types**. Of those, **50/51 (98.04%)** have a recovered Stardust.exe factory registration. This is a lower-bound inventory: the generic `ObjCtrlMessageQueueString(type)` wrapper and any dynamically selected types are deliberately not guessed.

| Type | Server producer/purpose | Priority | Client inbound factory | Status |
|---:|---|---:|---:|---|
| `0x71` | `DataTransform` | `0x1B` | `0x00AD4860` | Registered |
| `0xCC` | `CombatAction` | `0x1B` | `0x0119C330` | Registered |
| `0xDD` | start NPC conversation | `0x0B` | `0x00B12F20` | Registered |
| `0xDE` | stop NPC conversation | `0x0B` | `0x00B12BD0` | Registered |
| `0xDF` | NPC conversation message | `0x0B` | `0x00B14FF0` | Registered |
| `0xE0` | conversation string list | `0x0B` | `0x00B123A0` | Registered |
| `0xEA` | harvester resource data | `0x0B` | `0x00B17F00` | Registered |
| `0xEE` | generic response | `0x0B` | `0x00B191F0` | Registered |
| `0xF1` | parent-relative transform | `0x1B` | `0x00AD43B0` | Registered |
| `0xF2` | animation | `0x1B` | `0x00B12880` | Registered |
| `0xF4` | spatial chat | `0x0B` | `0x00AEF2C0` | Registered |
| `0xFA` | mission accept response | `0x0B` | `0x00B165D0` | Registered |
| `0x102` | crafting session response | `0x0B` | `0x00AF6D00` | Registered |
| `0x103` | crafting session response | `0x1B` | `0x00AF7280` | Registered |
| `0x10C` | crafting status/step | `0x1B` | `0x00B193B0` | Registered |
| `0x113` | crafting status/step | `0x1B` | `0x00B193B0` | Registered |
| `0x116` | command queue enqueue | `0x0B` | `0x00AE3EF0` | Registered |
| `0x117` | command queue remove | `0x1B` | `0x00AE3850` | Registered |
| `0x12E` | emote | `0x0B` | `0x00AF3E70` | Registered |
| `0x131` | posture | `0x1B` | `0x00B11F50` | Registered |
| `0x134` | combat spam | `0x1B` | `0x00AEED70` | Registered |
| `0x13B` | sit on object | `0x1B` | `0x00ACE300` | Registered |
| `0x140` | weapon ranges | `0x1B` | `0x00B11BB0` | Registered |
| `0x142` | mission abort response | `0x0B` | `0x00ADE520` | Registered |
| `0x147` | object-menu response | `0x0B` | `0x00AF35F0` | Registered |
| `0x166` | flourish | `0x1B` | `0x01194F20` | Registered |
| `0x167` | command queue add | `0x0B` | `0x01195040` | Registered |
| `0x1BD` | fly text | `0x1B` | `0x00B13280` | Registered |
| `0x1BE` | crafting status/step | `0x1B` | `0x00B193B0` | Registered |
| `0x1BF` | draft schematic response | `0x0B` | `0x00B19CB0` | Registered |
| `0x1C2` | crafting status/step | `0x1B` | `0x011951C0` | Registered |
| `0x1DB` | biography | `0x1B` | `0x01196180` | Registered |
| `0x1E7` | players near you | `0x0B` | `0x01196340` | Registered |
| `0x1FC` | starting location list | `0x1B` | `0x011967B0` | Registered |
| `0x207` | draft schematic response | `0x0B` | `0x00B13C30` | Registered |
| `0x229` | add buff | `0x1B` | `0x00AC81C0` | Registered |
| `0x22A` | remove buff | `0x1B` | `0x00AC80C0` | Registered |
| `0x22B` | harvester setup | `0x1B` | `0x00AC8310` | Registered |
| `0x238` | image-design change | `0x1B` | `0x00B0B990` | Registered |
| `0x239` | image-design rejection | `0x1B` | `0x00B0B990` | Registered |
| `0x23A` | image-design start | `0x1B` | `0x00B11260` | Registered |
| `0x3FE` | begin hyperspace | `0x1B` | `0x01195B30` | Registered |
| `0x41D` | JTL ship-list response | `0x1B` | `0x01196A60` | Registered |
| `0x42D` | orient for hyperspace | `0x1B` | `0x01195B30` | Registered |
| `0x432` | ship-hit event | `0x1B` | `0x00B107A0` | Registered |
| `0x437` | group-member space invite | `0x1B` | `0x01195890` | Registered |
| `0x439` | group-member invite response | `0x1B` | `0x01196EE0` | Registered |
| `0x43C` | open loot-lottery window | `0x0B` | `0x01195890` | Registered |
| `0x43D` | close loot-lottery window | `0x0B` | `0x01195890` | Registered |
| `0x441` | server labels quest-task counter | `0x0B` | `0x00B0F0D0` | **Registered to the client's timer class, not counter** |
| `0x444` | server labels quest-task timer | `0x0B` | — | **No factory registration recovered** |

## Quest counter/timer compatibility finding

The executable's registrations and RTTI associations are:

| Client type | Client typed class | Outbound factory | Inbound factory | Registration site |
|---:|---|---:|---:|---:|
| `0x440` | `MessageQueueQuestTaskCounterMessage` | `0x00B0F630` | `0x00B0F6F0` | `0x00B0F3B6` |
| `0x441` | `MessageQueueQuestTaskTimerMessage` | `0x00B0F020` | `0x00B0F0D0` | `0x00B0EDA6` |

This mapping is confirmed by the class-specific memory-manager strings, RTTI descriptors/vtables, factory construction calls, and the immediate type passed to `obj_controller_factory_register_VA_00B0CFC0`. No direct `0x444` factory registration exists in the recovered 139-entry registry.

The server emits its quest counter as `0x441` with five fields and exposes a timer as `0x444` with four fields. The client expects its four-field timer at `0x441` and its five-field counter at `0x440`. Therefore the IDs are not merely missing; they are shifted/mismatched:

- **Confirmed:** a server `0x441` counter will be decoded through the client's timer codec, which consumes only the timer-shaped prefix and cannot represent the trailing current-count field.
- **Strong:** server `0x444` reaches the unknown-inner-type path because no registration was recovered.
- **Not currently demonstrated:** timer traffic in ordinary gameplay, because no screenplay calls `Journal.timer()` beyond its wrapper definition. Counter traffic is available through `Journal.counter()` and should be audited separately for active calls.

Treat this as a confirmed protocol-definition mismatch in optional quest-journal functionality and a requirement for the replacement client's Stardust-compatibility profile, not as evidence that login or normal scene entry is blocked.

## Implications

The 98.04% numeric intersection is strong evidence that Stardust.exe and this server tree share the intended controller vocabulary. It does not measure field-schema completeness. Each registered type still needs directional field decoding and an effect contract before feature-complete replacement.

For a narrow login/network proof of concept, none of these 51 inner messages is required before authentication and the first known response. For scene-entry proof, `0x71`, `0xF1`, `0x116`, `0x117`, `0x131`, and common social/UI messages become high-priority after object creation/baselines are in place.

Field-level layouts for common combat, conversation, command, posture, social, and quest types are in `CORE_OBJECT_CONTROLLER_SCHEMAS.md`. Harvester, JTL, loot, image-design, and crafting layouts are in `EXTENDED_OBJECT_CONTROLLER_SCHEMAS.md`.
