# Stardust.exe Analysis Checkpoint: Chat Room Destruction

Date: 2026-09-15  
Target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Scope

This block traces `ChatOnDestroyRoom` (`0xE8EC5877`) from active Stardust producers through the executable's parser and room-manager handler. It concentrates on the intentional difference between a successful broadcast and a request-correlated failure.

## Confirmed executable route

```text
ChatOnDestroyRoom / 0xE8EC5877
        |
        v
chat_room_manager_message_listener_VA_00A6E9C0
        |
        +-- descriptor comparison 0x00A6ED0B..0x00A6ED1E
        v
chat_destroy_room_result_parse_VA_00B07490
        |
        v
chat_room_manager_apply_destroy_room_VA_00A2B100
        |
        +-- success broadcast (result 0, request 0): remove room and tree entry
        +-- correlated failure (result != 0, request != 0): localized failure prose
        `-- inconsistent/missing-room combinations: diagnostics or safe no-op
```

The executable contains `ChatOnDestroyRoom` at `0x018AB904` and RTTI text at `0x018B2EAC`. The descriptor slot is `0x015E9F40`. The common listener matches it at `0x00A6ED1E`, constructs `0x00B07490`, then passes four decoded values to `0x00A2B100` at `0x00A6ED5F`.

## Wire contract

The parser registers four members. Correlation with the server constructor establishes their exact meaning and order:

| Order | Field | Wire representation | Executable storage | Confidence |
|---:|---|---|---|---|
| 1 | destroyer identity | `ChatAvatarId`: ASCII16 game, galaxy, character | composite beginning at parser `+0x38` | Confirmed |
| 2 | result/error | LE32 | parser `+0x60` | Confirmed |
| 3 | room ID | LE32 | parser `+0x68` | Confirmed |
| 4 | request/sequence ID | LE32 | parser `+0x70` | Confirmed |

Both constructors in `ChatOnDestroyRoom.h` emit the same shape. Successful notifications populate the identity and normally use result/request zero. Error replies emit empty identity strings, nonzero result, the requested room ID and the original request ID.

## Server lifecycle and client behavior

Stardust's `destroyRoom()` broadcasts a successful `ChatOnDestroyRoom` to clients before clearing the room's players and disabling or deleting the server room. That constructor does not receive the initiating request ID, so the successful broadcast intentionally carries request ID zero. `ChatRoomImplementation::sendDestroyTo()` uses the same broadcast-style successful form for an individual client.

`handleChatDestroyRoom()` permits destruction only when the room exists and the requester owns it. Otherwise it sends error code 1 using the alternate constructor and preserves the request ID.

The executable mirrors this design:

- It resolves the room ID through `0x00A283E0` before mutation.
- A present room, result zero and request ID zero enters the authoritative destruction path.
- The path derives room/tree identity, removes the retained room through `0x00A2AF90`, updates the room hierarchy rooted at global `0x019392FC` through `0x00A32EF0`, releases associated retained state, and notifies room observers.
- A nonzero result with a nonzero request ID builds `ui_chatroom:destroy_fail_prose` (localized-object global `0x01939E24`) and publishes it through the chat-room status transceiver at `0x01939414`.
- A failed response with request ID zero logs the explicit diagnostic `CuiChatRoomManager received a failed OnDestroyRoom with zero sequenceId`.
- A message naming a room that is already absent follows a diagnostic/safe-no-op path. Its embedded diagnostic records room ID, destroyer, result and sequence instead of attempting a second removal.
- A nominal success carrying a nonzero request ID is rejected as an inconsistent mode; authoritative removal is driven by the zero-sequence broadcast.

## Packet-handler mapping

| Server message / opcode | Parser/deserializer | Handler | Affected subsystem/state | Confidence |
|---|---|---|---|---|
| `ChatOnDestroyRoom` / `0xE8EC5877` | `0x00B07490` | `0x00A2B100` | room lookup and removal; room-tree pruning; observer notification; request-correlated localized failure | Confirmed |

## Replacement-client requirements

- Preserve all numeric fields as 32-bit wire values in the x64 client.
- Model successful destruction as an authoritative, unsolicited/broadcast state event; do not require a matching pending request before deleting the room.
- Model the permission failure as a correlated command result and retain its request ID until feedback is delivered.
- Make destruction idempotent. An unknown/already-removed room should be logged and ignored, never partially recreated or dereferenced.
- Remove the room from the canonical registry before or atomically with rebuilding the UI tree so no mediator can retain a discoverable stale entry.
- Preserve the localized `destroy_fail_prose` contract, while allowing a headless proof of concept to log the equivalent structured result.

## Confidence and uncertainty

- **Confirmed by code:** opcode, message identity, four-field order, parser, listener branch, handler, server producers, success broadcast convention, room removal/tree update, failure localization key, and diagnostic branches.
- **Strongly inferred:** the precise observer signal names and fine-grained ordering inside the legacy transceiver containers.
- **Unknown:** whether every historical server variant used the same success/request-zero convention; the deployed Stardust source does.

No DRM, authentication-protection or access-control mechanism was examined or altered.

## Progress effect

After publication and matrix regeneration, coverage is **222 documented / 123 reachable gaps / 48 reachable gaps with literal opcodes**. Weighted whole-client coverage remains conservatively **78%**; this single closure does not justify rounding the overall estimate upward.
