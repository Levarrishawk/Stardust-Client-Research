# Stardust.exe Analysis Checkpoint: Chat Room Ban

Date: 2026-09-15  
Target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Scope

This block traces `ChatOnBanFromRoom` (`0x5A38538D`) and its client-to-server request. It closes the wire layout, result table, cached banned-set mutation, room-tree refresh, and Stardust's compound de-op/uninvite/kick/private-room-removal behavior.

## Confirmed executable route

```text
ChatOnBanFromRoom / 0x5A38538D
        |
        v
chat-room listener 0x00A6E9C0
        |
        +-- descriptor 0x015E9F34 -> "ChatOnBanAvatarFromRoom"
        +-- comparison 0x00A6F0B8..0x00A6F0CB
        v
chat_ban_avatar_from_room_parse_VA_00B05160
        |
        v
chat_room_manager_apply_ban_result_VA_00A2C9A0
        |
        +-- result 0: add target to cached banned set; notify; refresh tree
        `-- failure: select result-specific prose; no room mutation
```

The executable uses the historical internal name `ChatOnBanAvatarFromRoom` at `0x018AB948`; Stardust's class is `ChatOnBanFromRoom`. Opcode and payload agree, so this is only a naming-version difference.

## Wire contracts

### Server to client

The parser registers five members matching `ChatOnBanFromRoom.h`:

| Order | Field | Wire representation | Parser storage | Confidence |
|---:|---|---|---|---|
| 1 | full room path | ASCII16 | `+0x38` | Confirmed |
| 2 | banner/actor | `ChatAvatarId` | `+0x48` | Confirmed |
| 3 | banned target | `ChatAvatarId` | `+0x70` | Confirmed |
| 4 | result | LE32 | `+0x98` | Confirmed |
| 5 | request ID | LE32 | `+0xA0` | Confirmed |

### Client to server

`ChatBanFromRoomCallback.h` parses the target as game/galaxy/name ASCII16 strings, then ASCII16 room path and LE32 request ID. Stardust derives the banner from the authenticated session rather than trusting an actor identity from the request.

## Client handler behavior

`0x00A2C9A0` normalizes the actor and target identities and resolves the room path through `0x00A285E0`. An unknown local room triggers the diagnostic `received ChatOnBanAvatarFromRoom but room [%s] doesn't exist on client` and exits without dereferencing or inventing room state.

For result zero, the handler calls `0x00A6A4A0` with add=true, adding the target to the room's cached banned set. Comparison with the unban handler, which calls the same function with add=false, upgrades this helper's provisional meaning to `chat_room_set_banned_membership`. It emits a typed banned-state event through global `0x0193905C`, refreshes the room tree at `0x019392FC` via `0x00A32EF0`, and continues through the localized status-publication path.

The result table is:

| Result | Stardust/legacy meaning | Client localization key | Mutation |
|---:|---|---|---|
| 0 | success | `ban_success_prose` | add cached ban; notify; refresh tree |
| 4 (legacy 3-4) | avatar not found | `ban_fail_avatar_not_found_prose` | none |
| 6 (legacy 5-7) | room missing | `ban_fail_room_not_exist_prose` | none if the local room resolves; otherwise diagnostic return |
| 12 | invalid state / already banned | `ban_fail_banned_prose` | none |
| 16 | insufficient privileges | `ban_fail_no_privs_prose` | none |
| other | general failure | `ban_fail_unknown_prose` | none |

Out-of-range result values use the generic failure object. The result prose is parameterized with room and identity data and delivered through the shared chat status channel.

## Stardust compound success transaction

`handleChatBanPlayer()` validates room existence, actor moderator status, target existence, target not being the owner, and target not already being banned. A successful operation then performs several separately observable changes:

1. If the target is a moderator, remove that role and broadcast `ChatOnRemoveModeratorFromRoom` success.
2. For a private room, remove the target from the invited set and broadcast refreshed query results.
3. If the target is currently in the room, remove them and send the target a localized kick system message.
4. Add the target to the server banned set.
5. Broadcast request-zero `ChatOnBanFromRoom` success to remaining room members.
6. If the banner is not a current room member, send a request-correlated success directly.
7. For a private room, send `ChatOnDestroyRoom` to the banned target so the room disappears from that client's channel browser.

This explains why the legacy ban handler itself only changes the banned set: moderator, membership, invitation and visibility changes arrive through their own established messages.

## Packet-handler map

| Direction/message | Parser/deserializer | Handler | Resulting state/action | Confidence |
|---|---|---|---|---|
| Z→C `ChatOnBanFromRoom` / `0x5A38538D` | `0x00B05160` | `0x00A2C9A0` | add cached ban on success; notify/refresh room; publish result-specific prose | Confirmed |
| C→Z `ChatBanFromRoomCallback` | Stardust callback parser | `ChatManagerImplementation::handleChatBanPlayer` | validate and orchestrate de-op, uninvite, kick, ban and private-room visibility removal | Confirmed by Stardust source |

## Replacement-client requirements

- Preserve room-path-first ordering and all 32-bit result/request values.
- Dispatch by opcode and treat the differing source class names as aliases only.
- Make banned-set insertion and all companion removals idempotent; request-zero broadcast and direct correlated responses can both describe the same success under different membership conditions.
- Do not collapse moderator, invitation, membership, banned, and channel-browser visibility into one boolean. Stardust changes them through distinct protocol messages.
- Tolerate intermediate states while the compound message sequence is delivered.
- Unknown local rooms must be safe no-ops with diagnostics; never synthesize a room from a ban notification alone.
- Preserve generic handling for unknown result codes.

## Confidence and unknowns

- **Confirmed by code:** descriptor, parser, fields, handler, successful ban mutation, tree/event update, result/localization mapping, request callback, and full Stardust validation/transaction.
- **Strongly inferred:** exact legacy observer type names and whether every UI mediator redraws synchronously after the tree notification.
- **Unknown:** the exact inter-message ordering visible under retransmission; transport reliability is documented, but UI state must remain robust to duplicate/reordered semantic application.

No DRM, authentication-protection or access-control mechanism was examined or altered.

## Progress effect

Both response and request callback moved to documented after regeneration: **228 documented / 117 reachable gaps / 45 reachable gaps with literal opcodes**. Weighted coverage remains conservatively **78%**.
