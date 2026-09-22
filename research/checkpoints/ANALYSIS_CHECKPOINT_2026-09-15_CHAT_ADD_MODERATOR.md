# Stardust.exe Analysis Checkpoint: Add Chat-Room Moderator

Date: 2026-09-15  
Target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Scope

This block traces `ChatOnAddModeratorToRoom` (`0x36A03858`) and the corresponding server-bound request. It establishes the wire contract, result mapping, successful cached moderator mutation, room-tree refresh, and the extra Stardust state transitions performed for private or banned targets.

## Confirmed executable route

```text
ChatOnAddModeratorToRoom / 0x36A03858
        |
        v
chat-room listener 0x00A6E9C0
        |
        +-- descriptor 0x015E9F30 -> "ChatOnAddModeratorToRoom"
        +-- comparison 0x00A6ED7D..0x00A6ED90
        v
chat_add_moderator_result_parse_VA_00B06FD0
        |
        v
chat_room_manager_apply_add_moderator_result_VA_00A2C3A0
        |
        +-- result 0: add target to cached moderator set; notify; refresh tree
        `-- nonzero: build result-specific ui_chatroom prose and publish status
```

## Wire contracts

### Server to client

The executable parser registers five fields in the same order emitted by `ChatOnAddModeratorToRoom.h`:

| Order | Field | Wire representation | Parser storage | Confidence |
|---:|---|---|---|---|
| 1 | promoted target | `ChatAvatarId` | `+0x38` | Confirmed |
| 2 | acting moderator | `ChatAvatarId` | `+0x60` | Confirmed |
| 3 | result | LE32 | `+0x88` | Confirmed |
| 4 | full room path | ASCII16 | `+0x90` | Confirmed |
| 5 | request ID | LE32 | `+0xA0` | Confirmed |

### Client to server

`ChatAddModeratorToRoomCallback.h` confirms the request payload: target `ChatAvatarId` encoded as game/galaxy/name ASCII16 strings, ASCII16 room path, then LE32 request ID. Stardust ignores the request's game/galaxy components after parsing and derives the actor from the authenticated session.

## Client handler behavior

`0x00A2C3A0` extracts the request ID and result first. A request-correlation helper at `0x00A70090` resolves/retire the pending operation context.

For result zero, the handler resolves the room path through `0x00A283A0`. If the room is absent locally it emits the diagnostic `received ChatOnAddModeratorToRoom but room doesn't exist on client` and returns without mutation. Otherwise it normalizes the target identity, calls `0x00A6A480` with add=true to insert it into the room's cached moderator set, publishes a moderator-state event through global `0x0193921C`, and refreshes the hierarchy rooted at `0x019392FC` through `0x00A32EF0`.

For nonzero results, it builds localized structured prose using the target and room identities and publishes it through the chat status channel. The compact dispatch table covers results 0-16 and falls back safely outside that range:

| Result | Stardust/legacy meaning | Localization key | Mutation |
|---:|---|---|---|
| 4 (and legacy 3) | avatar not found | `mod_add_fail_avatar_not_found_prose` | none |
| 6 (legacy 5-7) | room does not exist | `mod_add_fail_room_not_exist_prose` | none |
| 9 | room not moderated | `mod_add_fail_room_not_moderated_prose` | none |
| 16 | actor is not a moderator / insufficient privilege | `mod_add_fail_not_moderator_prose` | none |
| 1 and other unmapped values | generic failure | `mod_add_fail_unknown_prose` | none |

The executable initializes `mod_add_success_prose`, but this handler's observed result-zero branch performs the state/event path instead of entering the failure-prose builder. Its presence should not be treated as evidence that the handler emits that string directly.

## Stardust behavior beyond the result packet

`handleChatAddModerator()` validates room existence, actor moderator permission, target existence, target not being the room owner, and target not already being a moderator. On success:

1. If the target is banned, Stardust removes the ban and broadcasts `ChatOnUnbanFromRoom` success.
2. If the room is private and the target is not invited, Stardust adds the invitation, exposes the room to the target, broadcasts updated query results, and sends a room invitation.
3. Stardust adds the target to the moderator set.
4. It broadcasts request-zero `ChatOnAddModeratorToRoom` success to room members.
5. If the actor is not a current room member, it separately sends the actor a request-correlated success.

Therefore a replacement client must accept unban, room-list/query, invitation and moderator packets as an ordered cluster without assuming that promotion is an isolated state change.

## Packet-handler map

| Direction/message | Parser/deserializer | Handler | Resulting state/action | Confidence |
|---|---|---|---|---|
| Z→C `ChatOnAddModeratorToRoom` / `0x36A03858` | `0x00B06FD0` | `0x00A2C3A0` | correlate operation; add cached moderator on success; notify and refresh tree; localized failure on error | Confirmed |
| C→Z `ChatAddModeratorToRoomCallback` | server callback parser | `ChatManagerImplementation::handleChatAddModerator` | validate authority/target and orchestrate unban, invite and promotion transitions | Confirmed by Stardust source |

## Replacement-client requirements

- Preserve the target-before-actor ordering and all 32-bit result/request fields.
- Dispatch by opcode, while retaining the embedded type name only as diagnostic metadata.
- Make moderator insertion idempotent because both request-zero broadcast and request-correlated success may reach the initiating client under different membership conditions.
- Keep banned, invited and moderator sets distinct; promotion can modify all three through separate messages.
- Process room discovery/invitation updates without assuming they precede or follow the moderator event under all network conditions.
- If the room is unknown, log and safely ignore the state mutation; pending request UI must still have a timeout/failure policy.
- Preserve generic handling for unknown result codes.

## Confidence and unknowns

- **Confirmed by code:** descriptor, parser fields, listener branch, handler, successful moderator mutation, tree update, localization table, request callback and Stardust validation/orchestration.
- **Strongly inferred:** semantic labels of the legacy observer/transceiver payload types.
- **Unknown:** precise UI ordering when the private-room invitation/query-result cluster is received under packet loss; that belongs to the reliable SOE channel policy already documented separately.

No DRM, authentication-protection or access-control mechanism was examined or altered.

## Progress effect

After publication and regeneration, both directions moved to documented: **226 documented / 119 reachable gaps / 46 reachable gaps with literal opcodes**. Weighted whole-client coverage remains conservatively **78%**.
