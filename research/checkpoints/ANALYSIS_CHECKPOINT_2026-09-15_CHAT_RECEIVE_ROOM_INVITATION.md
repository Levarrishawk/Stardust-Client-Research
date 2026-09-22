# Stardust.exe Analysis Checkpoint: Receive Chat-Room Invitation

Date: 2026-09-15  
Target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Scope

This block traces the unsolicited, target-facing `ChatOnReceiveRoomInvitation` message (`0xC17EB06D`). It closes the path intentionally left outside the preceding invite-result block: wire decoding, room lookup, local invitee-state mutation, `InviteeAdded` publication, room-tree refresh and localized invitation presentation. The analysis is static and does not examine or alter any protection mechanism.

## Confirmed executable route

```text
ChatOnReceiveRoomInvitation / 0xC17EB06D
        |
        v
chat-room listener 0x00A6E9C0
        |
        +-- descriptor slot 0x015E9F50
        +-- type text 0x018AB8B0
        +-- comparison/branch 0x00A6EE44..0x00A6EE68
        v
chat_receive_room_invitation_parse_VA_00B06600
        |
        +-- inviter ChatAvatarId value at +0x3C
        `-- room-path string value at +0x64
        v
chat_room_manager_receive_invitation_VA_00A2D3B0
        |
        +-- resolve room by full path (0x00A283A0)
        +-- normal room: add local avatar to invited set (0x00A6A460)
        +-- publish CuiChatRoomManager::Messages::InviteeAdded
        +-- refresh room tree (0x00A32EF0)
        `-- publish ui_chatroom:invited_prose
```

## Wire contract

Stardust's `ChatOnReceiveRoomInvitation.h` constructs the following payload after the common message header:

| Order | Field | Wire representation | Client storage | Confidence |
|---:|---|---|---|---|
| 1 | game | ASCII16; current server literal `SWG` | inviter `ChatAvatarId` component | Confirmed |
| 2 | galaxy | ASCII16 | inviter `ChatAvatarId` component | Confirmed |
| 3 | moderator/inviter name | ASCII16 | inviter `ChatAvatarId` component | Confirmed |
| 4 | full room path | ASCII16 | room-path field | Confirmed |

The three identity strings are one client `CuiChatAvatarId`. There is no result code or request ID: this is an unsolicited notification to the invited target, not the request-correlated acknowledgement sent to the inviter.

The message object's archive wrappers begin at offsets `+0x38` and `+0x60`; their decoded values begin at `+0x3C` and `+0x64`. These offsets are internal 32-bit implementation details and are not a wire-layout requirement for a replacement client.

## Handler behavior

`0x00A2D3B0` first calls `chat_room_manager_find_room_by_full_path_VA_00A283A0` with the decoded room path. If the room is absent, it emits the exact diagnostic:

```text
received receiveChatOnReceiveRoomInvitation but room doesn't exist on client.
```

and returns without fabricating a room. This proves the ordering dependency already visible in Stardust: the server sends `room->sendTo(invitee)` before this notification so that the private room is present in the target's channel-browser model.

For an ordinary room, the handler:

1. Obtains the local player's normalized `CuiChatAvatarId` through `0x009E8F90`.
2. Copies the packet's moderator/inviter identity through `0x00A719E0`.
3. Calls `0x00A6A460` on the resolved room with the local identity and add=true, inserting the receiving player into the cached invited set.
4. Publishes the typed payload `pair(room, pair(localInvitee, inviter))` through `CuiChatRoomManager::Messages::InviteeAdded` and its observer fanout at global `0x019390CC`.
5. Refreshes the room-tree projection at global `0x019392FC` through `0x00A32EF0`.
6. Formats and publishes `ui_chatroom:invited_prose`, whose authoritative client-string text is `%TT has invited you to join the [%TO] chatroom.`; `%TT` is the inviter and `%TO` is the resolved room presentation object/path.

The local-player identity, rather than the inviter, is the entry added to the room's invitee set. The two identities remain distinct in the notification payload so UI consumers can present both the changed invitee and the actor.

## Reserved group/guild behavior

Before the ordinary-room path, the handler compares the resolved room's category/name with two retained identifiers. The called functions make their meaning explicit through RTTI:

| Candidate branch | Function | Result |
|---|---|---|
| group room | `chat_room_manager_set_group_room_id_VA_00A2E510` | stores the room ID and publishes `CuiChatRoomManager::Messages::GroupRoomIdChanged` |
| guild room | `chat_room_manager_set_guild_room_id_VA_00A2E7B0` | stores the room ID and publishes `CuiChatRoomManager::Messages::GuildRoomIdChanged` |

Either special branch returns immediately and does not run the normal invitation prose/state path. This is confirmed client capability, but it is not used by the two active Stardust producers found in this block; those producers send this notification for private user-room invitation and private-room moderator promotion.

## Stardust server correlation

There are two active producers in `ChatManagerImplementation.cpp`:

- `handleChatInvitePlayer`: after validation and optional unban, Stardust adds the target to the room's invited set, sends the room record to expose the private room in the channel browser, then sends this invitation.
- `handleChatAddModerator`: for a private room, Stardust first adds/exposes/invites a target who was not already invited, then grants moderator status.

The first producer subsequently acknowledges the inviter with `ChatOnInviteToRoom` and broadcasts a query snapshot. The promotion producer subsequently adds moderator state and broadcasts `ChatOnAddModeratorToRoom`. A modern client therefore must tolerate the notification as one step in a compound, partially redundant convergence sequence.

## Packet-handler map

| Server message / opcode | Parser/deserializer | Handler | Parsed fields | Resulting action | Confidence | Evidence |
|---|---|---|---|---|---|---|
| `ChatOnReceiveRoomInvitation` / `0xC17EB06D` (Z→C) | `chat_receive_room_invitation_parse_VA_00B06600` | `chat_room_manager_receive_invitation_VA_00A2D3B0` | inviter `ChatAvatarId`; ASCII room path | require pre-existing room; add local avatar to invited set; publish `InviteeAdded`; refresh tree; display `ui_chatroom:invited_prose`; special group/guild rooms update their retained IDs | Confirmed | executable descriptor/xrefs/parser/listener/RTTI/diagnostic/localization path; active Stardust constructor and producers |

## Replacement-client requirements

- Decode exactly four ASCII16 strings and combine the first three into the inviter identity.
- Do not add result or request-ID fields to this opcode.
- Apply this only after the room record is known. If arrival ordering is made asynchronous, queue briefly by room path or log/drop deterministically; never create a structurally incomplete room from this packet alone.
- Add the local avatar—not the inviter—to the invited set, idempotently.
- Preserve inviter separately as the actor used by UI/event consumers.
- Treat room visibility, invitation, moderation and membership as separate state dimensions.
- Retain special group/guild room-ID transitions if compatibility testing demonstrates that the server uses them.
- Resolve `ui_chatroom:invited_prose` from original assets rather than baking English into protocol logic.

## Confidence and remaining boundary

- **Confirmed by code:** descriptor and dispatch branch; two-field object schema/four-string wire shape; room lookup and missing-room diagnostic; local-avatar invited-set insertion; actor/local distinction; `InviteeAdded` publication; room-tree refresh; localized prose key/text; group/guild ID publications; both active Stardust producers and their send ordering.
- **Strongly inferred:** the two room-name globals compared before the special setters are specifically the reserved group and guild identifiers. The destination transceiver types prove the behavior even though the globals themselves are not symbolically named.
- **Unknown:** whether Stardust can produce a real-world ordering race between the preceding room-list message and this invitation under all transport batching conditions. The legacy client assumes the room already exists and only warns when it does not.

## Progress effect

This closes one reachable server packet class and its complete target-side semantic path. The packet-gap matrix should move from **236 documented / 109 reachable gaps / 41 literal-opcode gaps** to **237 documented / 108 reachable gaps / 40 literal-opcode gaps** after regeneration. Weighted whole-client coverage remains conservatively **78%**.
