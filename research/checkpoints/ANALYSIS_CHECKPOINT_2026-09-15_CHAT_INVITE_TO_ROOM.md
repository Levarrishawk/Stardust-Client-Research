# Stardust.exe Analysis Checkpoint: Invite to Chat Room Result

Date: 2026-09-15  
Target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Scope

This block traces `ChatOnInviteToRoom` (`0x493FE74A`) and `ChatInviteToRoomCallback`. It documents the result packet, pending-request reconstruction, successful invitee-set mutation, room-tree refresh, exact error prose and Stardust's compound unban/expose/invite/snapshot transaction. The separate target-facing room-invitation notification is intentionally left for the next block.

## Confirmed executable route

```text
ChatOnInviteToRoom / 0x493FE74A
        |
        v
chat-room listener 0x00A6E9C0
        |
        +-- descriptor 0x015E9F48 -> "ChatOnInviteToRoom"
        +-- comparison 0x00A6EF1C..0x00A6EF30
        v
chat_invite_to_room_result_parse_VA_00B05EA0
        |
        +-- listener extracts result and request ID only
        v
chat_room_manager_apply_invite_result_VA_00A2DB50
        |
        +-- recover target/room from pending request
        +-- result 0 and room resolved: add cached invitee; notify; refresh tree
        `-- all outcomes: publish result-specific localized prose
```

## Wire contracts

### Server to client

The parser registers five fields matching `ChatOnInviteToRoom.h`:

| Order | Field | Wire representation | Parser storage | Confidence |
|---:|---|---|---|---|
| 1 | inviter/actor | `ChatAvatarId` | `+0x38` | Confirmed |
| 2 | invitee/target | `ChatAvatarId` | `+0x60` | Confirmed |
| 3 | full room path | ASCII16 | `+0x88` | Confirmed |
| 4 | result | LE32 | `+0x98` | Confirmed |
| 5 | request ID | LE32 | `+0xA0` | Confirmed |

The listener deliberately passes only result and request ID to the semantic handler. The actor, target and path are decoded for message compatibility but are not the handler's authoritative context.

### Client to server

`ChatInviteToRoomCallback.h` parses the target as three ASCII16 `ChatAvatarId` components, then ASCII16 room path and LE32 request ID. Stardust derives the inviter from the authenticated session.

## Pending-request design

`0x00A2DB50` resolves request ID through `0x00A6FD90` and reconstructs the intended room and target from the retained outbound request. That design has three consequences:

- a normal result is correlated by request ID before mutating local invite state;
- redundant identity/path fields in the response do not override the original request context;
- this message is not the mechanism for unsolicited invite-set synchronization.

Unsolicited changes arrive through request-zero `ChatQueryRoomResults`, while the invited target receives a separate room-invitation notification.

## Success and failure behavior

When correlation yields a valid room ID and result is zero, the handler resolves the room, normalizes the invitee identity from pending context, and calls `0x00A6A460` with add=true. It publishes an invitation-set event through global `0x019390CC` and refreshes the room tree at `0x019392FC` through `0x00A32EF0`.

The localized result table is:

| Result | Meaning | Client localization key | Mutation |
|---:|---|---|---|
| 0 | success | `invite_success_prose` | add cached invitee when correlated room resolves |
| 4 (legacy 3-4) | avatar not found | `invite_fail_avatar_not_found_prose` | none |
| 6 (legacy 5-7) | room does not exist | `invite_fail_room_not_exist_prose` | none |
| 9 | room is not private | `invite_fail_room_not_private_prose` | none |
| 16 | inviter lacks moderator authority | `invite_fail_not_moderator_prose` | none |
| other/out of range | general failure | `invite_fail_unknown_prose` | none |

Every outcome is formatted using the reconstructed room/target context and published through the common chat status channel. A missing or stale correlation prevents success mutation; the UI still receives a bounded result/fallback path rather than trusting response-supplied state blindly.

## Stardust compound transaction

`handleChatInvitePlayer()` validates room existence, private status, inviter moderator authority, target existence and exact target-name capitalization. Exact capitalization is enforced specifically to avoid duplicate client list entries.

On success Stardust:

1. Removes an existing ban and broadcasts `ChatOnUnbanFromRoom` if needed.
2. Adds the target to the server invitee set.
3. Sends the private room record to the target so it appears in the channel browser.
4. Sends the separate room-invitation notification to the target.
5. Sends request-correlated `ChatOnInviteToRoom` success to the inviter.
6. Broadcasts request-zero `ChatQueryRoomResults` to current members so their invitee snapshots converge.

If the target is already invited, Stardust returns generic result 1 and does not repeat the transaction.

## Packet-handler map

| Direction/message | Parser/deserializer | Handler | Resulting state/action | Confidence |
|---|---|---|---|---|
| Z→C `ChatOnInviteToRoom` / `0x493FE74A` | `0x00B05EA0`; listener forwards result/request only | `0x00A2DB50` | correlate pending invite; on success add cached invitee and refresh tree; publish exact localized outcome | Confirmed |
| C→Z `ChatInviteToRoomCallback` | Stardust parses target ChatAvatarId, room path, U32 request ID | `ChatManagerImplementation::handleChatInvitePlayer` | validate then optionally unban, expose private room, notify target, acknowledge inviter and broadcast snapshot | Confirmed by Stardust source |

## Replacement-client requirements

- Preserve all five response fields and the target/path/request outbound ordering.
- Treat request ID as the authoritative correlation key for `ChatOnInviteToRoom` results.
- Do not use this result packet as the global invitee-list replication channel; `ChatQueryRoomResults` is authoritative for snapshots.
- Make invite insertion idempotent and normalize capitalization consistently with Stardust's canonical first-name policy.
- Keep room visibility, invitation membership, banned membership and actual room membership as separate state.
- A success with missing correlation must not mutate an arbitrary room based solely on untrusted redundant fields.
- Handle timeout explicitly: if server-side room/player lookup prevents a response in related flows, pending UI cannot remain indefinitely.
- Process the compound unban/room-list/invitation/result/query sequence safely under duplicates.

## Confidence and unknowns

- **Confirmed by code:** descriptor, parser, field order, listener argument reduction, request lookup, success invitee mutation, observer/tree update, result table, localization keys, outbound callback and Stardust transaction.
- **Strongly inferred:** exact internal layout and expiry policy of the pending invite-request record.
- **Unknown:** timeout duration used by the legacy pending-request manager; a modern client should define and test its own bounded policy.

No DRM, authentication-protection or access-control mechanism was examined or altered.

## Progress effect

Both analysed directions moved to documented after matrix regeneration: **236 documented / 109 reachable gaps / 41 reachable gaps with literal opcodes**. The separate target-facing notification remains deliberately uncounted until its own handler is traced. Weighted whole-client coverage remains conservatively **78%**.
