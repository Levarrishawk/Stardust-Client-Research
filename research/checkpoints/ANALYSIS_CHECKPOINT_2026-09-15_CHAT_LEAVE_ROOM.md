# Stardust.exe Analysis Checkpoint: Leave Chat Room

Date: 2026-09-15  
Target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Scope

This block traces `ChatOnLeaveRoom` (`0x60B5098B`) and its client-to-server request. It closes the inverse of entered-room membership: common member removal, local-player channel teardown, other-player departure notification, special system-room cleanup and room-tree refresh.

## Confirmed executable route

```text
ChatOnLeaveRoom / 0x60B5098B
        |
        v
chat-room listener 0x00A6E9C0
        |
        +-- descriptor 0x015E9F4C -> "ChatOnLeaveRoom"
        +-- comparison 0x00A6EBC2..0x00A6EBD6
        v
chat_leave_room_parse_VA_00B08950
        |
        v
chat_room_manager_apply_leave_VA_00A2C040
        |
        +-- common: remove avatar from cached member set
        +-- local avatar: deactivate/remove active channel and publish self-left
        `-- other avatar: publish participant-left event
                         |
                         `-- refresh room tree
```

## Wire contracts

### Server to client

`0x00B08950` registers four members matching `ChatOnLeaveRoom.h`:

| Order | Field | Wire representation | Parser storage | Confidence |
|---:|---|---|---|---|
| 1 | leaving avatar | `ChatAvatarId` | `+0x38` | Confirmed |
| 2 | result/error | LE32 | `+0x60` | Confirmed; Stardust emits zero |
| 3 | room ID | LE32 | `+0x68` | Confirmed |
| 4 | request ID | LE32 | `+0x70` | Confirmed; Stardust emits zero |

Unlike `ChatOnEnteredRoom`, the shared listener does not split on result. Stardust's only constructor fixes result and request ID to zero, and `0x00A2C040` consumes the avatar and room ID without interpreting those two compatibility fields.

### Client to server

`ChatLeaveRoomCallback.h` parses four ASCII16 strings: game, galaxy, leaving player name and full room path. There is no request ID. Stardust derives the authenticated actor from the session:

- if the supplied leaving name matches the authenticated player case-insensitively, it performs an ordinary self-leave;
- if it differs, the same request shape is routed to `handleChatKickPlayer`, making it a moderator kick request rather than trusting the supplied name as the actor.

## Client state behavior

`0x00A2C040` resolves the room through `0x00A283E0`. An unknown room emits `received receiveOnLeaveRoom but room [%d] does not exist on client` and returns safely.

The handler recognizes several special/system room classes and invokes their dedicated teardown helpers when the leaving avatar is local and the room matches the retained special-room ID. These branches remove subsystem-specific channel state before exiting.

For the general path, it normalizes the leaving `ChatAvatarId`, calls `0x00A6A440` with add=false, and therefore removes the identity from the cached member set. Paired evidence with entered-room upgrades this function to a confirmed bidirectional member-membership setter.

The handler then compares the leaving identity with the local chat identity:

- **Self leave:** verifies that the room is actually present in the active-channel collection; otherwise it logs `received self ChatOnLeaveRoom for room not entered.` It removes the active room/channel record, publishes the self-left event through global `0x019393DC`, and releases associated retained UI/channel state.
- **Other leave:** publishes a room-plus-avatar departure event through global `0x019394BC` without deactivating the local channel.

Both branches refresh the room hierarchy at `0x019392FC` through `0x00A32EF0`.

## Stardust lifecycle behavior

`ChatRoomImplementation::removePlayer()` optionally removes the room ID from the player's persistent/current chat-room list, broadcasts `ChatOnLeaveRoom` while the player is still present in the room's broadcast list, then drops the player from the room's `playerList`. Consequently, the departing client receives its own leave notification during normal leave/kick handling and can execute the self branch.

The `disconnecting` flag skips mutation of the player's stored room list but does not suppress the leave broadcast or removal from the live room list. This distinction is server lifecycle bookkeeping; the packet observed by remaining clients has the same format.

The same removal path is reused by voluntary leave, moderator kick, uninvite, ban and other server cleanup. A replacement client must therefore infer presentation from message context/state, not from a different leave opcode.

## Packet-handler map

| Direction/message | Parser/deserializer | Handler | Resulting state/action | Confidence |
|---|---|---|---|---|
| Z→C `ChatOnLeaveRoom` / `0x60B5098B` | `0x00B08950` | `0x00A2C040` | remove cached member; self branch tears down active channel, other branch publishes participant departure; refresh tree | Confirmed principal behavior |
| C→Z `ChatLeaveRoomCallback` | Stardust parses game/galaxy/leaving-name/room-path ASCII16 | self: `handleChatLeaveRoom`; different target: `handleChatKickPlayer` | voluntary leave or authenticated moderator kick using one request shape | Confirmed by Stardust source |

## Replacement-client requirements

- Preserve the four response fields even though Stardust fixes result/request to zero.
- Treat member removal as idempotent and apply it before self/other presentation dispatch.
- A self-leave must remove active channel/UI state; another player's leave must not close the local channel.
- Do not infer voluntary leave versus kick from the server notification alone. Both use the same event; companion system prose or locally pending intent may refine presentation.
- Preserve separate active-channel and room-registry concepts: leaving a room does not necessarily delete it from the channel browser.
- Unknown rooms and duplicate self-leaves must be diagnostic safe no-ops.
- Keep special guild/group/planet/auction channel cleanup behind typed room policy rather than hard-wiring it into the generic wire decoder.
- The outbound request must never authorize kicking locally; Stardust remains authoritative and derives the actor from the authenticated session.

## Confidence and unknowns

- **Confirmed by code:** descriptor, parser, field layout, handler, room lookup, member removal, self/other split, active-channel removal, observer paths, tree refresh, request callback and Stardust broadcast ordering.
- **Strongly inferred:** detailed ownership semantics of the legacy active-channel/UI containers.
- **Unknown:** precise labels of each special system-room teardown helper; their gating and separation from generic membership are established.

No DRM, authentication-protection or access-control mechanism was examined or altered.

## Progress effect

Both directions moved to documented after matrix regeneration: **232 documented / 113 reachable gaps / 43 reachable gaps with literal opcodes**. Weighted whole-client coverage remains conservatively **78%**.
