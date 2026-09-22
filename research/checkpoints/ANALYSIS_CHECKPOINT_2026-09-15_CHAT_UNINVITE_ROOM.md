# Stardust.exe Analysis Checkpoint: Uninvite From Chat Room

Date: 2026-09-15  
Target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Scope

This block closes both directions of the room-uninvite operation: `ChatUninviteFromRoom` / `ChatUninviteFromRoomCallback` from client to Stardust, and `ChatOnUninviteFromRoom` (`0xBE33C7E8`) from Stardust to the client. It traces request correlation, cached invited-set removal, typed observer and room-tree updates, exact result prose, and the server's compound de-op/kick/uninvite/browser-removal transaction.

## Confirmed executable route

```text
outbound ChatUninviteFromRoom
        |
        +-- constructor/field registration 0x00AFC990
        +-- target ChatAvatarId, room path, request ID
        `-- pending context retained in uninvite table

ChatOnUninviteFromRoom / 0xBE33C7E8
        |
        v
chat-room listener 0x00A6E9C0
        |
        +-- descriptor slot 0x015E9F5C
        +-- type text 0x018AB860
        +-- branch 0x00A6EF7D..0x00A6EFA1
        v
chat_uninvite_from_room_result_parse_VA_00B059D0
        |
        +-- all five response fields decoded
        `-- listener forwards result and request ID only
        v
chat_room_manager_apply_uninvite_result_VA_00A2DEC0
        |
        +-- uninvite pending resolver 0x00A6FF10
        +-- recover target and room from retained request
        +-- success: remove cached invitee via 0x00A6A460(add=false)
        +-- publish InviteeRemoved and refresh room tree
        `-- publish exact result-specific ui_chatroom prose
```

## Wire contracts

### Client to server

The executable constructor at `0x00AFC990` independently confirms the callback parsed by Stardust:

| Order | Field | Wire representation | Confidence |
|---:|---|---|---|
| 1 | target game | ASCII16 | Confirmed |
| 2 | target galaxy | ASCII16 | Confirmed |
| 3 | target avatar name | ASCII16 | Confirmed |
| 4 | full room path | ASCII16 | Confirmed |
| 5 | request ID | LE32 | Confirmed |

Stardust derives the uninviting actor from the authenticated `ZoneClientSession`; no actor identity is accepted from the request.

### Server to client

The parser at `0x00B059D0` registers the same fields emitted by `ChatOnUninviteFromRoom.h`:

| Order | Field | Wire representation | Client decoded value | Confidence |
|---:|---|---|---|---|
| 1 | full room path | ASCII16 | string | Confirmed |
| 2-4 | uninviter/actor | three ASCII16 strings | `CuiChatAvatarId` | Confirmed |
| 5-7 | uninvitee/target | three ASCII16 strings | `CuiChatAvatarId` | Confirmed |
| 8 | result | LE32 | U32 | Confirmed |
| 9 | request ID | LE32 | U32 | Confirmed |

Although all fields are decoded, the listener passes only result and request ID into `0x00A2DEC0`. As in the invite-result path, authoritative target/room context is recovered from the retained outbound request rather than from redundant response fields.

## Pending request and success mutation

`chat_uninvite_pending_resolve_VA_00A6FF10` uses request ID against the dedicated table rooted at `0x0193B128`. This is distinct from the invite table at `0x0193B134`. The result record yields the retained room identifier and target input; the handler resolves the canonical room, normalizes the target avatar, and reconstructs a displayable room path.

Only result zero with a valid correlated room performs mutation. The handler calls `chat_room_set_invited_membership_VA_00A6A460` on that room with the target identity and add=false. Paired invite/uninvite evidence therefore upgrades this function from an add-only interpretation to a confirmed bidirectional invited-membership setter.

The success path publishes:

- `CuiChatRoomManager::Messages::InviteeRemoved` through observer global `0x01939094`, carrying `pair(room, pair(target, localActor))`;
- a room-tree refresh through global `0x019392FC` and `0x00A32EF0`;
- the same localized operation-result channel used by adjacent room commands.

A missing/stale correlation or unresolved room suppresses mutation. Result feedback still uses the bounded fallback context instead of trusting the response's redundant target/path fields.

## Result table

The handler's compact switch maps legacy ranges and active Stardust errors as follows:

| Result | Stardust meaning | Client localization key | Mutation |
|---:|---|---|---|
| 0 | success | `uninvite_success_prose` | remove target from cached invited set when correlation and room resolution succeed |
| 3 or 4 | avatar not found | `uninvite_fail_avatar_not_found_prose` | none |
| 5, 6 or 7 | room does not exist | `uninvite_fail_room_not_exist_prose` | none |
| 9 | room is not private | `uninvite_fail_room_not_private_prose` | none |
| 13 | target is not invited | `uninvite_fail_avatar_not_invited_prose` | none |
| 16 | actor lacks moderator authority | `uninvite_fail_not_moderator_prose` | none |
| other/out of range | general failure | `uninvite_fail_unknown_prose` | none |

The original `ui_chatroom` string table supplies target (`%TT`) and room (`%TU`) prose. These strings remain asset-layer data, not protocol constants.

## Stardust compound transaction

`ChatManagerImplementation::handleChatUninvitePlayer()` validates the room, privacy, moderator authority, target existence, non-owner status, and current invitation state. On success Stardust performs an ordered compound operation:

1. If the target is a moderator, remove moderator status and broadcast `ChatOnRemoveModeratorFromRoom`.
2. If the target is currently in the room, remove membership and send that target `guild:kick_target` system prose.
3. Remove the target from the server invited set.
4. Send the request-correlated `ChatOnUninviteFromRoom` result to the initiating moderator.
5. Broadcast `ChatQueryRoomResults` to remaining room members.
6. Send the target a room-destruction message last, removing the private room from its channel browser.

The final destroy is intentionally last so a self-uninvite can format its result using the still-present room path. The target normally does not receive the uninvite result directly; its state converges through the kick prose, query/broadcast effects where applicable, and private-room destruction.

## Packet-handler map

| Direction/message | Parser/deserializer | Handler | Resulting state/action | Confidence |
|---|---|---|---|---|
| C→Z `ChatUninviteFromRoom` / callback | executable request constructor `0x00AFC990`; Stardust callback parser | `ChatManagerImplementation::handleChatUninvitePlayer` | validate authority/state; optionally de-op and kick; remove invitation; acknowledge actor; update members; remove target's browser room | Confirmed |
| Z→C `ChatOnUninviteFromRoom` / `0xBE33C7E8` | `chat_uninvite_from_room_result_parse_VA_00B059D0`; listener forwards result/request only | `chat_room_manager_apply_uninvite_result_VA_00A2DEC0` | correlate retained request; on success remove cached invitee, emit `InviteeRemoved`, refresh tree; publish exact localized result | Confirmed |

## Replacement-client requirements

- Preserve request ordering: target ChatAvatarId, room path, U32 request ID.
- Preserve response ordering: room path, actor ChatAvatarId, target ChatAvatarId, result, request ID.
- Use request ID as the authoritative correlation key for the initiating client's mutation and prose context.
- Keep invitation, moderator, active membership and room visibility as independent state dimensions.
- Make invited-set removal idempotent and normalize avatar capitalization consistently.
- Tolerate the ordered multi-message success sequence, including de-op, kick prose, query snapshot, acknowledgement and room destruction.
- Do not destroy the target's private-room browser entry merely from the initiator's success result; the dedicated server destruction message owns that transition.
- Define an explicit timeout for pending uninvite requests; Stardust early returns outside the normal handler path could otherwise leave UI state pending.

## Confidence and unknowns

- **Confirmed by code:** both wire shapes; message descriptor/listener/parser; result/request-only handoff; dedicated pending table; correlated target/room reconstruction; add=false invited mutation; `InviteeRemoved` event; tree refresh; complete result switch and localization keys; active Stardust validation and compound success ordering.
- **Strongly inferred:** the precise internal record fields stored alongside the pending request beyond the observed target and room identifier.
- **Unknown:** the legacy client's pending-request expiry duration. This is a replacement-client policy requirement, not a wire-format unknown.

No DRM, licensing, authentication-protection or access-control mechanism was examined or altered.

## Progress effect

Both server packet classes should move to documented after matrix regeneration: **239 documented / 106 reachable gaps / 39 literal-opcode gaps**. Weighted whole-client coverage remains conservatively **78%**.
