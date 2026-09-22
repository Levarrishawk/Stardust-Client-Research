# Analysis Checkpoint: Chat and Persistent-Mail Requests

## Outcome

Eight active client-to-server chat callback rows are closed. Exact message classes and construction sites exist in `Stardust.exe`, and the server registrations/parsers define the corresponding opcodes and field contracts.

| Message / opcode | Client constructor / caller | Wire body | Server action |
|---|---|---|---|
| `ChatCreateRoom` / `0x35366BED` | `0x00AFBE10` / `0x00A28C10` | U8 permission; U8 moderation; U16 zero; ASCII room path; ASCII title; U32 request ID | create room and return request-correlated result |
| `ChatDestroyRoom` / `0x094B2A77` | `0x00AFC350` / `0x00A28E80` | U32 room ID; U32 request ID | destroy room |
| `ChatRemoveModeratorFromRoom` / `0x8A3F8E04` | `0x00AFDB50` / `0x00A2A1F0` | ASCII game; ASCII galaxy; ASCII target avatar; ASCII room path; U32 request ID | remove room moderator |
| `ChatInstantMessageToCharacter` / `0x84BB21F7` | `0x00AF2900` / `0x00900A60` | ASCII game; ASCII galaxy; ASCII avatar; Unicode message; U32 spacer; U32 sequence | route instant message and return sequence-correlated result |
| `ChatPersistentMessageToServer` / `0x25A29FA6` | `0x00AF0B10` / `0x008F0790` | Unicode body; U32 attachment-byte-count/2; tagged OOB parameter records; U32 sequence; Unicode subject; U32 spacer; ASCII recipients | deliver mail and return `ChatOnSendPersistentMessage` |
| `ChatDeletePersistentMessage` / `0x8F251641` | `0x00AF1C10` / `0x008F3400` | U32 mail ID | delete one persistent message |
| `ChatRequestPersistentMessage` / `0x07E3559F` | `0x00AF1EB0` / `0x008F3400` | U32 mail ID | request full body for a header/list entry |
| `ChatDeleteAllPersistentMessages` / `0x8B1E8E72` | `0x00AEF750` / `0x008EFB40` | U64 player ID; U64 target ID | validate self-target and clear mailbox |

The persistent-mail OOB segment is length-delimited; Stardust consumes StringId and waypoint parameter records. It must be decoded as bounded tagged records rather than native x64 structures. Request IDs and sequences remain U32 wire values, and original ASCII game/galaxy/avatar addressing remains part of the protocol.

## Confidence

Message identities, opcodes, client construction/call sites, server parsers and actions: **Confirmed**. Semantics of every possible legacy OOB parameter tag beyond StringId and waypoint: **Unknown**.

