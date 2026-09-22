# Stardust.exe Analysis Checkpoint: Entered Chat Room

Date: 2026-09-15  
Target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Scope

This block traces `ChatOnEnteredRoom` (`0xE69BDC0A`) and the server-bound enter-by-ID request. It separates failure handling from successful membership, then separates local-player entry from another avatar entering the same room.

## Confirmed route

```text
ChatOnEnteredRoom / 0xE69BDC0A
        |
        v
chat-room listener 0x00A6E9C0
        |
        +-- descriptor 0x015E9F44 -> "ChatOnEnteredRoom"
        +-- comparison 0x00A6EB2F..0x00A6EB42
        v
chat_entered_room_parse_VA_00B08D60
        |
        +-- result != 0 -> chat_room_manager_apply_enter_failure_VA_00A2D1D0
        `-- result == 0 -> chat_room_manager_apply_enter_success_VA_00A2BAF0
                                  |
                                  +-- entered avatar is local: activate/register self room state
                                  `-- other avatar: publish other-entered membership event
```

The listener reads the parsed result directly. Only a zero result reaches the membership handler; failures pass request ID and result to the dedicated error path.

## Wire contracts

### Server to client

`0x00B08D60` registers four fields matching `ChatOnEnteredRoom.h`:

| Order | Field | Wire representation | Parser storage | Confidence |
|---:|---|---|---|---|
| 1 | entering avatar | `ChatAvatarId` | `+0x38` | Confirmed |
| 2 | result | LE32 | `+0x60` | Confirmed |
| 3 | room ID | LE32 | `+0x68` | Confirmed |
| 4 | request ID | LE32 | `+0x70` | Confirmed |

### Client to server

`ChatEnterRoomByIdCallback.h` parses exactly two LE32 values in request-ID-then-room-ID order. Stardust derives the entering player from the authenticated session and calls `handleChatEnterRoomById(player, roomID, requestID)`.

## Failure path

For a nonzero result, `0x00A2D1D0` requires a nonzero request ID before resolving the pending request via `0x00A6F850`. It builds parameterized `ui_chatroom` prose and publishes it through global status channel `0x0193944C`.

Active Stardust mappings are:

| Result | Meaning | Client localization key | Confidence |
|---:|---|---|---|
| 6 | room missing | `join_fail_invalid_room_name_prose` | Confirmed |
| 12 | banned | `join_fail_banned_prose` | Confirmed |
| 13 | not invited | `join_fail_not_invited_prose` | Confirmed |
| 1/other | unknown/general failure | `join_fail_unknown_prose` | Confirmed fallback |

The legacy table also maps result 5 to `join_fail_invalid_room_id_prose`, aliases result 16 to not-invited prose, and silently consumes result 36. Those values are not emitted by Stardust's current enter implementation but should remain tolerated for protocol compatibility.

## Successful membership path

`0x00A2BAF0` resolves the room ID through `0x00A283E0`. If no local room record exists it logs `received ChatOnEnteredRoom but room ...` and returns without inventing state.

For a valid room, the handler applies special handling for several system-room classifications before the general path. It normalizes the entering `ChatAvatarId` and calls `0x00A6A440` with add=true to insert the avatar into the room's cached member set.

It then compares the entering identity with the local chat identity:

- **Local-player entry:** resolves a nonzero pending request when present, registers/activates the room in retained active-channel/UI structures, updates self-entry observers (the executable exposes `CuiChatRoomDataNode::SelfEntered` RTTI), handles duplicate-self-entry diagnostically, and reconciles room-specific retained state.
- **Other-player entry:** publishes a smaller room-plus-avatar membership event through global `0x019394F4` without activating a local chat channel.

Both branches finish by refreshing the room tree rooted at `0x019392FC` via `0x00A32EF0`. Member insertion is therefore common; channel activation is strictly a self-entry concern.

## Stardust producer behavior

`handleChatEnterRoomById()` checks room existence and, unless security is explicitly bypassed by an internal server call, invokes `ChatRoom::checkEnterPermission()`. The active policy allows system rooms according to their owner context and custom rooms unless the player is banned or a private-room invitation is absent.

On failure Stardust sends only the requester a response retaining the request ID. On success:

- if already present, it sends the requester the success response directly;
- otherwise it adds the player server-side and broadcasts the same success to the room, so all members update their cached member set.

Internal group-room creation can call the handler with request ID `-1` and bypass security. On the wire that value remains the 32-bit bit pattern `0xFFFFFFFF`; a new client must not widen or reinterpret it as a native pointer-sized value.

## Packet-handler map

| Direction/message | Parser/deserializer | Handler | Resulting state/action | Confidence |
|---|---|---|---|---|
| Z→C `ChatOnEnteredRoom` / `0xE69BDC0A` success | `0x00B08D60` | `0x00A2BAF0` | add cached member; self-entry activates/registers room, other-entry publishes participant event; refresh room tree | Confirmed principal behavior |
| Z→C `ChatOnEnteredRoom` failure | `0x00B08D60` | `0x00A2D1D0` | correlate request and publish result-specific join-failure prose | Confirmed |
| C→Z `ChatEnterRoomByIdCallback` | server parses LE32 request ID, LE32 room ID | `ChatManagerImplementation::handleChatEnterRoomById` | validate room/security, add player, send direct failure/duplicate response or broadcast success | Confirmed by Stardust source |

## Replacement-client requirements

- Preserve request-ID-before-room-ID ordering in the outbound request and the four fixed-width response fields.
- Treat request IDs as 32-bit opaque correlation values; preserve `0xFFFFFFFF` where encountered.
- Add room membership idempotently before dispatching self/other presentation events.
- Never activate a local room merely because another avatar entered it.
- Self-entry must be able to complete from a request-correlated reply or a room broadcast.
- Duplicate self-entry and unknown-room events must be safe diagnostics, not fatal state transitions.
- Keep system-room policy outside the generic member container so guild/group/planet/auction behaviors can be represented without contaminating the wire model.
- A headless proof of concept needs only decode/log/correlate this event; UI channel creation belongs to a later phase.

## Confidence and unknowns

- **Confirmed by code:** descriptor, parser, field order, listener split, failure table, room lookup, member insertion, self/other identity comparison, distinct observer paths, tree refresh, request callback and Stardust permission/broadcast behavior.
- **Strongly inferred:** exact semantic names for several retained UI/channel containers inside the long self-entry branch.
- **Unknown:** detailed behavior of each special system-room helper selected before the general membership path; they are bounded and do not alter the base wire contract.

No DRM, authentication-protection or access-control mechanism was examined or altered.

## Progress effect

The response and request callback both became documented after regeneration: **230 documented / 115 reachable gaps / 44 reachable gaps with literal opcodes**. Weighted whole-client coverage remains conservatively **78%**.
