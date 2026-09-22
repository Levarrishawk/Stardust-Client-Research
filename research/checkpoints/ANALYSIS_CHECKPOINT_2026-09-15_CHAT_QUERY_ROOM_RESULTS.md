# Stardust.exe Analysis Checkpoint: Chat Room Query Results

Date: 2026-09-15  
Target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Scope

This block traces `ChatQueryRoomResults` (`0xC4DE864E`) and `ChatQueryRoomCallback`. It establishes the complete authoritative room-detail snapshot, shared nested room codec, registry merge behavior, four identity-set replacements, observer/tree reconciliation and request-ID treatment.

## Confirmed route

```text
ChatQueryRoomResults / 0xC4DE864E
        |
        v
chat-room listener 0x00A6E9C0
        |
        +-- descriptor 0x015E9F60 -> "ChatQueryRoomResults"
        +-- comparison 0x00A6EAC6..0x00A6EAD9
        v
chat_query_room_results_parse_VA_00AFF190
        |
        +-- four ChatAvatarId vectors
        +-- shared room record via 0x00B045C0
        `-- request ID
        v
chat_room_manager_apply_query_results_VA_00A2B520
        |
        +-- merge/create room record
        +-- replace members/invitees/moderators/banned sets
        +-- reconcile registry/tree and notify observers
        `-- publish query-received status
```

## Wire contract

The executable parser registers six top-level members. Stardust source fixes their semantics and order:

| Order | Field | Wire representation | Parser storage | Confidence |
|---:|---|---|---|---|
| 1 | current members | LE32 count + repeated `ChatAvatarId` | `+0x38` | Confirmed |
| 2 | invitees | LE32 count + repeated `ChatAvatarId` | `+0x48` | Confirmed |
| 3 | moderators | LE32 count + repeated `ChatAvatarId` | `+0x58` | Confirmed |
| 4 | banned users | LE32 count + repeated `ChatAvatarId` | `+0x68` | Confirmed |
| 5 | room record | shared codec `0x00B045C0` | `+0x78` | Confirmed |
| 6 | request ID | LE32 | `+0x100` | Confirmed |

Each `ChatAvatarId` is three ASCII16 fields: game, galaxy and character name.

The nested room record is:

```text
LE32 roomId
LE32 privateFlag
U8   moderated
ASCII16 fullPath
ChatAvatarId owner
ChatAvatarId creator
Unicode32 title
LE32 duplicateModeratorCount
duplicateModeratorCount * ChatAvatarId
LE32 duplicateMemberCount
duplicateMemberCount * ChatAvatarId
```

Stardust deliberately writes zero for the two duplicate lists because the authoritative moderator and member arrays were already emitted at the top level. The response enables transport compression.

## Client reconciliation behavior

`0x00A2B520` extracts the nested room ID and checks whether a room already exists through `0x00A283E0`. It then merges or creates the canonical room record through `0x00A69890`.

For a successfully resolved room it replaces all four cached identity sets through adjacent room-data operations:

- `0x00A6A660`: members,
- `0x00A6A680`: invitees,
- `0x00A6A6A0`: moderators,
- `0x00A6A6C0`: banned users.

These are snapshot replacements, not append-only deltas. A replacement client must remove identities absent from the new response.

If the room did not previously exist, the handler performs the broader room-registry/list observer insertion path and rebuilds relevant retained indexes. If it already existed, it refreshes the existing room node through the hierarchy rooted at `0x019392FC`. Both paths converge on observer/status publication, including the embedded `ui_chatroom:query_received` status object.

The parser preserves the request ID, but the inspected semantic handler does not read the stored request-ID value or require a pending-request match. Explicit query replies and request-zero broadcasts therefore use the same authoritative reconciliation path. This matches Stardust's two producer modes:

- `handleChatQueryRoom()` returns a snapshot containing the caller's request ID;
- `broadcastQueryResultsToRoom()` broadcasts the same snapshot with default request ID zero after invitation-set changes.

## Client-to-server request

`ChatQueryRoomCallback.h` parses LE32 request ID followed by ASCII16 room path. Stardust resolves the player from the authenticated session, looks up the room by full path and sends a locked snapshot. If player or room lookup fails, the current implementation returns without a `ChatQueryRoomResults` error packet.

That no-response behavior means a modern client must time out pending query UI independently rather than waiting forever for a protocol-level error.

## Packet-handler map

| Direction/message | Parser/deserializer | Handler | Resulting state/action | Confidence |
|---|---|---|---|---|
| Z→C `ChatQueryRoomResults` / `0xC4DE864E` | `0x00AFF190`, nested room codec `0x00B045C0` | `0x00A2B520` | merge canonical room; replace four identity sets; reconcile room registry/tree; publish query status | Confirmed |
| C→Z `ChatQueryRoomCallback` | Stardust parses U32 request ID then ASCII16 room path | `ChatManagerImplementation::handleChatQueryRoom` | resolve and return a compressed locked snapshot; invalid lookup produces no reply | Confirmed by Stardust source |

## Replacement-client requirements

- Preserve the six top-level members, nested record widths and 32-bit counts/request ID.
- Apply strict count and aggregate-size limits before allocating any of the four vectors.
- Decode `ChatAvatarId` once in a reusable codec and normalize identities consistently.
- Treat the four lists as authoritative replacements. Do not retain stale members, invitees, moderators or bans.
- Reuse the exact room-record decoder shared with room-list and create-room responses.
- Ignore the duplicate nested moderator/member lists when Stardust sends zero, but parse them safely for compatibility with other historical servers.
- Accept request-zero broadcasts as ordinary state snapshots. Do not gate reconciliation on pending correlation.
- Add a client-side request timeout because Stardust silently drops queries for missing rooms or absent players.
- Compression remains an SOE transport-envelope concern, not part of the semantic message decoder.

## Confidence and unknowns

- **Confirmed by code:** descriptor, parser, all arrays and record ordering, handler, room merge, four set replacements, existing/new room branches, tree/observer updates, request parser and active Stardust producers.
- **Strongly inferred:** exact legacy observer type names and the fine-grained order in which every UI mediator receives the new-room branch notifications.
- **Unknown:** why the fourth vector uses a distinct generated container helper in the parser; server ordering and downstream banned-set destination establish its semantics despite that implementation detail.

No DRM, authentication-protection or access-control mechanism was examined or altered.

## Progress effect

Both directions moved to documented after matrix regeneration: **234 documented / 111 reachable gaps / 42 reachable gaps with literal opcodes**. Weighted whole-client coverage remains conservatively **78%**.
