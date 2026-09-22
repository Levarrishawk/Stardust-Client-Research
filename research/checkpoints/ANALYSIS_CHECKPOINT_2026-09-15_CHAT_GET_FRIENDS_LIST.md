# Stardust.exe Analysis Checkpoint: Legacy Friends-List Snapshot

Date: 2026-09-15  
Target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Scope and result

This block investigates Stardust's `ChatOnGetFriendsList` packet (`0xE97AB594`). Unlike the recently traced chat-room messages, this packet has **no consumer in `Stardust.exe`**. The negative result is supported by independent name, opcode, RTTI, listener-registration, and adjacent-DLL checks. Stardust sends it as a redundant legacy snapshot alongside the actual `PlayerObject` view-9 friend-list replication used by this client generation.

This is a compatibility classification, not a claim that the server packet is malformed. A replacement client can parse it deliberately, but should choose one authoritative social-list state source and reconcile duplicate inputs idempotently.

## Confirmed server wire contract

`ChatOnGetFriendsList.h` emits:

| Order | Field | Wire representation | Confidence |
|---:|---|---|---|
| 1 | owning creature ID | LE64 (`ghost->getParentID()`) | Confirmed by Stardust source |
| 2 | friend count | LE32 | Confirmed by Stardust source |
| 3 | repeated friend identity | count × (`ASCII16("SWG")`, `ASCII16(galaxy)`, `ASCII16(friendName)`) | Confirmed by Stardust source |

The message sets its compression flag. This is a packet/transport encoding policy, not an extra application field; a replacement must pass the complete serialized message through the already documented SOE compression handling before parsing the application payload.

The first LE16 value is 3, consistent with three top-level message operands: owner ID, count, and the repeated identity collection.

## Confirmed absence from Stardust.exe

The following independent searches returned no match for this packet:

- no embedded ASCII message name in the complete PE string inventory;
- no MSVC RTTI class-name fragment;
- no little-endian opcode bytes or executable immediate reference for `0xE97AB594`;
- no registration in the chat-room listener constructor at `0x00A6E840`;
- no comparison branch in its listener at `0x00A6E9C0`;
- no matching string in adjacent EXE/DLL files under `C:\Stardust-DEV`.

By contrast, the same scans recover `ChatFriendsListUpdate`, its RTTI, descriptor slot, parser and chat listener branch. The absence is therefore message-specific rather than a failure of the analysis method.

There is consequently no honest executable mapping of:

```text
opcode -> parser -> handler -> client state
```

for this legacy packet. It reaches the generic unknown/unregistered-message boundary and has no identified client-side state effect in this build.

## Active friend-list state path

`PlayerObjectImplementation::sendFriendLists()` sends three forms of social data during initialization:

1. this legacy full friend-list packet;
2. the corresponding legacy full ignore-list packet;
3. `PlayerObjectDeltaMessage9`, containing friend and ignore updates.

The executable's recovered `PlayerObject` view-9 registry at constructor `0x0065B800` contains:

| View/index | Client object offset | Registered type | Server meaning | Confidence |
|---|---:|---|---|---|
| `PLAY:9[7]` | `+0x480` | `AutoDeltaVector<std::string, PlayerObject>` | friend-name vector | Confirmed by client registration order and server delta builder |
| `PLAY:9[8]` | `+0x4B8` | `AutoDeltaVector<std::string, PlayerObject>` | ignore-name vector | Confirmed by client registration order and server delta builder |

Stardust also sends view-9 index 7 deltas after `addFriend()` and `removeFriend()`. This makes replication—not the unhandled legacy snapshot—the continuing structural source for this executable. Online/offline presence is carried separately by the already mapped `ChatFriendsListUpdate` (`0x6CD2FCD8`) path.

The server additionally emits add/change notification messages around structural updates. Those distinct packets remain separate analysis targets; their class names are intentionally not used here so the packet-coverage matrix does not count them prematurely.

## CommunityManager boundary

The executable retains a separate `CommunityManager` service for presentation metadata and UI operations. Confirmed functions include:

- `0x005AA370`: derives the per-profile `*_friend_data.iff` path;
- `0x005AA7F0`: exposes the script-facing `getFriendList` operation;
- `0x005AA850`: exposes `removeFriend`;
- `0x005AA880`: exposes `addFriend`.

The per-profile IFF is presentation metadata such as groups/comments/preferences, not evidence that `0xE97AB594` is parsed. The structural name vector is replicated through `PlayerObject`, while the community layer owns user-facing organization and observer messages such as `FriendListChanged` and `FriendOnlineStatusChanged`.

The exact bridge that projects every view-9 vector mutation into all CommunityManager UI structures is outside this packet-focused block. Its absence does not change the confirmed conclusion that this legacy named message has no executable codec or listener.

## Server/client behavior model

```text
Stardust sendFriendLists()
        |
        +-- legacy named friend snapshot 0xE97AB594
        |       `-- Stardust.exe: no registered consumer / no state effect
        |
        +-- legacy named ignore snapshot
        |       `-- separate target, not classified in this block
        |
        `-- PLAY:9 delta
                +-- index 7 -> PlayerObject friend vector
                `-- index 8 -> PlayerObject ignore vector

Later structural friend changes
        `-- PLAY:9[7] delta -> replicated friend vector

Presence changes
        `-- ChatFriendsListUpdate -> online/offline projection
```

## Packet-handler map

| Server message / opcode | Candidate client function | Parsed fields | Resulting action | Confidence | Evidence |
|---|---|---|---|---|---|
| `ChatOnGetFriendsList` / `0xE97AB594` (Z→C) | none in `Stardust.exe`; unregistered/ignored legacy message | server emits owner U64, count U32, repeated game/galaxy/name ASCII triples | no direct effect in this executable; friend structure arrives through `PLAY:9[7]` | Confirmed absence / server schema confirmed | full PE string/opcode/RTTI/registration scan; adjacent-DLL string scan; server constructor and `sendFriendLists`; client view-9 registry |
| `PLAY:9[7]` friend vector (Z→C replication) | generic baseline/delta engine; `PlayerObject` registrar `0x0065B800` | AutoDeltaVector update protocol containing ASCII friend names | maintain structural friend-name vector on local PlayerObject | Confirmed principal state path | executable 18-field view-9 registration; server baseline/delta construction and add/remove producers |
| `ChatFriendsListUpdate` / `0x6CD2FCD8` (Z→C) | listener `0x00A6E9C0` → parser `0x00B055D0` → handler `0x005AC300` | ChatAvatarId; U8 online | update presence state for a friend | Confirmed; previously closed | executable descriptor/parser/handler and server producers |

## Replacement-client requirements

- Preserve support for the server's actual structural source, `PLAY:9[7]`, including baseline and delta container operations.
- Treat object ID as a 64-bit wire value even though friend names and container sizes retain their original formats.
- Keep friend-list membership separate from online/offline presence and from local group/comment metadata.
- Either ignore the legacy snapshot explicitly with a known-message diagnostic, matching this executable, or parse it as an idempotent bootstrap aid. Do not let it race destructively with `PLAY:9[7]`.
- If both sources are accepted, key snapshots by owner ID and make the replication stream authoritative after the owning PlayerObject exists.
- Preserve case-normalization policy consistently; Stardust stores lower-case friend names in its active add/remove path.
- Keep decompression at the transport/message envelope boundary rather than embedding it into the friend-list model.

## Confidence and unknowns

- **Confirmed by code:** exact server schema and producer; packet compression flag; complete absence of name/opcode/RTTI/registration/handler from the executable and adjacent DLLs; `PLAY:9[7]` and `[8]` registered string vectors; structural friend deltas after server add/remove; separate presence-update handler.
- **Strongly inferred:** the legacy packet is retained for compatibility with other client revisions and is redundant for this Stardust executable.
- **Unknown:** historical reason it remains in the server send sequence; exact internal callback chain from all `PLAY:9[7]` mutations into CommunityManager presentation; behavior of other client versions.

No DRM, licensing, authentication-protection or access-control mechanism was examined or altered.

## Progress effect

This reachable packet moves to documented as a confirmed unsupported/redundant compatibility message. Expected regenerated totals are **242 documented / 103 reachable gaps / 37 literal-opcode gaps**. Weighted whole-client coverage remains conservatively **78%**.
