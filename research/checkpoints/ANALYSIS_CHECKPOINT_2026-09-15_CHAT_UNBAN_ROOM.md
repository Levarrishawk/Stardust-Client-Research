# Stardust.exe Analysis Checkpoint: Chat Room Unban

Date: 2026-09-15  
Target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Scope

This block traces Stardust's `ChatOnUnbanFromRoom` (`0xBAF9B815`) through the legacy client's corresponding `ChatOnUnbanAvatarFromRoom` class, parser, cached-room mutation and localized outcome presentation.

## Naming compatibility

The executable embeds `ChatOnUnbanAvatarFromRoom` at `0x018AB92C`; Stardust calls its wire-compatible server class `ChatOnUnbanFromRoom`. The opcode and field sequence match. This is a historical source-name difference, not a protocol difference.

## Confirmed route

```text
ChatOnUnbanFromRoom / 0xBAF9B815
        |
        v
chat-room listener 0x00A6E9C0
        |
        +-- descriptor slot 0x015E9F38
        +-- comparison 0x00A6F121..0x00A6F134
        v
chat_unban_avatar_from_room_parse_VA_00B04C90
        |
        v
chat_room_manager_apply_unban_result_VA_00A2CC90
        |
        +-- result 0: remove target from cached banned set; refresh tree
        `-- all results: choose localized outcome and publish status
```

## Wire contract

The parser registers five fields in this order:

| Order | Field | Wire representation | Parser storage | Confidence |
|---:|---|---|---|---|
| 1 | full room path | ASCII16 | `+0x38` | Confirmed |
| 2 | unbanner/actor | `ChatAvatarId` (ASCII16 game, galaxy, name) | `+0x48` | Confirmed |
| 3 | unbanned target | `ChatAvatarId` | `+0x70` | Confirmed |
| 4 | result | LE32 | `+0x98` | Confirmed |
| 5 | request ID | LE32 | `+0xA0` | Confirmed |

This exactly matches `ChatOnUnbanFromRoom.h`. The client-to-server request is separately confirmed by `ChatUnbanFromRoomCallback.h`: target ChatAvatarId, room path and request ID.

## Handler behavior

`0x00A2CC90` normalizes both avatar identities, resolves the room by its full path through `0x00A285E0`, and safely logs/returns if the room does not exist locally. For a resolved room it decodes the result with a compact table and selects a localized `ui_chatroom` prose template.

For result zero, `0x00A6A4A0` removes the target identity from the room's cached banned set. The handler emits a typed room-state notification through global `0x01939024` and refreshes the room hierarchy at `0x019392FC` through `0x00A32EF0`. It then publishes the localized result through the chat status path.

The exact active result mapping is:

| Result | Stardust meaning | Client localization key | Client action |
|---:|---|---|---|
| 0 | success | `unban_success_prose` | remove cached ban, refresh tree, publish success |
| 4 | avatar not found | `unban_fail_avatar_not_found_prose` | no room mutation; publish failure |
| 6 | room does not exist | `unban_fail_room_not_exist_prose` | no mutation; publish failure when local room resolves; otherwise diagnostic return |
| 12 | invalid ban state / target not banned | `unban_fail_banned_prose` | no mutation; publish failure |
| 16 | insufficient privileges | `unban_fail_no_privs_prose` | no mutation; publish failure |
| other | unknown/general failure | `unban_fail_unknown_prose` | no mutation; publish fallback |

The executable's table also aliases adjacent legacy result values: 3-4 use avatar-not-found and 5-7 use room-not-found. Out-of-range values select the generic failure.

## Stardust producer semantics

`handleChatUnbanPlayer()` validates, in order: non-null actor, room existence, moderator permission, target existence, target not being the owner, and current banned membership. It returns errors 6, 16, 4, 1 or 12 respectively. On success it removes the target from the server banned list and broadcasts a request-zero success notification to current room members. If the initiating moderator is not a room member, Stardust additionally sends that moderator a request-correlated success response.

The same notification is also used by related invitation/moderation flows that remove a ban before continuing, explaining why the client treats request-zero success as an authoritative state event rather than only a command acknowledgement.

## Packet-handler map

| Server message / opcode | Parser/deserializer | Handler | Affected subsystem/state | Confidence |
|---|---|---|---|---|
| `ChatOnUnbanFromRoom` / `0xBAF9B815` | `0x00B04C90` | `0x00A2CC90` | cached room banned-member set; room hierarchy; room observers; localized request result | Confirmed |

## Replacement-client requirements

- Preserve the exact five-field order and all 32-bit result/request widths.
- Accept both request-zero broadcasts and request-correlated success without applying the state change twice; banned-set removal must be idempotent.
- Key rooms by canonical full path as well as room ID because this message identifies the room by path.
- Treat the executable/server class-name difference as an alias in documentation only; dispatch by opcode on the wire.
- Bound every ASCII16 field before normalization or lookup.
- Maintain the server's numeric result mapping and a generic fallback for unknown legacy values.
- A locally unknown room should produce diagnostics and no mutation. A modern client may still retire a matching pending request so UI state cannot remain stuck; this refinement should be tested against live Stardust behavior.

## Confidence and remaining uncertainty

- **Confirmed by code:** descriptor, parser, all fields, handler, success mutation, tree update, result table, localization keys, client request shape and active server validations/producers.
- **Strongly inferred:** exact semantic names of the legacy observer/transceiver types.
- **Unknown:** whether the locally-missing-room branch retires a pending request in every historical client build; the inspected executable exits after diagnostics.

No DRM, authentication-protection or access-control mechanism was examined or altered.

## Progress effect

After publication and matrix regeneration, coverage is **224 documented / 121 reachable gaps / 47 reachable gaps with literal opcodes**. Two entries closed because this block documents both the client-bound result and the server-bound `ChatUnbanFromRoomCallback` request contract. Weighted whole-client coverage remains conservatively **78%**.
