# Stardust.exe Analysis Checkpoint: Chat Room Creation Result

Date: 2026-09-15  
Target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Scope

This block traces `ChatOnCreateRoom` (`0x35D7CC9F`) from the active Stardust server producer through the executable's message listener, deserializer, request correlation, room-registry mutation and error presentation. All names below are provisional unless supplied by RTTI or server source.

## Confirmed route

```text
Stardust ChatOnCreateRoom
        |
        v
chat-room listener 0x00A6E9C0
        |
        +-- descriptor comparison at 0x00A6ECAA
        v
chat_create_room_result_parse_VA_00B07FD0
        |
        v
chat_room_manager_apply_create_room_result_VA_00A2CF80
        |
        +-- result 0: merge room registry/tree and notify observers
        +-- result 24: silent cleanup (autojoin workaround)
        `-- other errors: construct and publish localized failure prose
```

The executable contains the literal type name `ChatOnCreateRoom` at `0x018AB918`, RTTI at `0x018B2ECC`, and a descriptor reference at `0x015E9F3C`. The shared chat-room listener reaches the type comparison at `0x00A6ECAA`, constructs the parser at `0x00B07FD0`, and calls the semantic handler at `0x00A2CF80` from `0x00A6ECED`.

## Wire contract

The top-level executable parser registers exactly three members:

| Order | Executable field | Wire representation | Confidence |
|---:|---|---|---|
| 1 | result at object `+0x38` | LE32 | Confirmed |
| 2 | room record at `+0x40` | nested room codec `0x00B045C0` | Confirmed |
| 3 | request ID at `+0xC8` | LE32 | Confirmed |

The nested record is the same room representation used by `ChatRoomList`:

```text
LE32 roomId
LE32 privateFlag
U8   moderated
ASCII16 fullPath
ChatAvatarId owner       (ASCII16 game, galaxy, name)
ChatAvatarId creator     (ASCII16 game, galaxy, name)
Unicode32 title
LE32 moderatorCount
moderatorCount * ChatAvatarId
LE32 memberCount
memberCount * ChatAvatarId
```

This matches the successful Stardust constructor in `MMOCoreORB/src/server/zone/packets/chat/ChatOnCreateRoom.h`, followed by the request ID. The successful constructor enables transport compression.

## Handler behavior

`0x00A2CF80` first resolves pending request state through `0x00A6F6D0`. An absent/unmatched correlation enters a diagnostic path rather than fabricating a successful request.

For result zero, the handler validates the returned room identity and record, merges/resolves it through `0x00A69890`, updates the retained room collection and tree/index through calls including `0x00A322D0` and `0x00A32EF0`, and emits downstream room-state work/notifications. This is the client-side completion path for an explicit room creation.

For nonzero results, the handler maps the result to localized prose and publishes it through the chat UI notification path. Result 6 selects a distinct localized message object; most observed failures select the general creation-failure object. Result 24 bypasses publication and proceeds directly to cleanup.

The special handling of 24 is independently corroborated by the alternate Stardust constructor: it ignores its supplied error, emits code 24 with an empty/zero room record, preserves the request ID, and comments that this prevents repeated autojoin error spam. The executable's silent branch is therefore confirmed protocol behavior, not an unused server quirk.

## Active Stardust producers

`ChatManagerImplementation.cpp` constructs this response on both successful and unsuccessful create/autojoin paths (observed near lines 676, 2140 and 2254 in the current tree). The server source is especially valuable here because it establishes the intent of the otherwise opaque result-24 branch and proves the exact successful room-record ordering.

## Packet-handler map

| Server message / opcode | Parser/deserializer | Handler | Affected subsystem/state | Confidence |
|---|---|---|---|---|
| `ChatOnCreateRoom` / `0x35D7CC9F` | `0x00B07FD0`, using room codec `0x00B045C0` | `0x00A2CF80` | pending request correlation; room registry/tree insertion; room observers; localized failure presentation; silent result-24 autojoin failure | Confirmed |

## Replacement-client implications

- Preserve every wire width; the room and request identifiers remain 32-bit even in a native x64 process.
- Reuse one canonical room-record decoder for room lists and create-room responses.
- Keep request correlation separate from room storage so unsolicited/autojoin outcomes can be handled without manufacturing a pending UI operation.
- Treat result 24 as a compatibility policy with no user-facing error. Do not reinterpret it as success.
- Enforce count and string bounds before allocating moderator/member arrays; the legacy client trusts its message framework more than a new network boundary should.
- Compression belongs to the SOE transport envelope, not this semantic decoder.

## Remaining uncertainty

- The user-facing wording associated with every possible nonzero result was not exhaustively labeled; the state transition and special cases 6 and 24 are established.
- The exact semantic name of every downstream observer signal remains provisional.
- The alternate server constructor does not explicitly request compression; this is a transport-policy detail, not a semantic format difference.

No DRM, authentication-protection or access-control mechanism was examined or altered in this block.

## Progress effect

The regenerated server-packet matrix is now **221 documented / 124 reachable gaps / 49 reachable gaps with literal opcodes**. Weighted whole-client coverage remains conservatively **78%**: this block materially closes one protocol path but is not large enough by itself to justify a whole percentage-point increase.
