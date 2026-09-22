# Stardust.exe Analysis Checkpoint: Send Chat-Room Message

Date: 2026-09-15  
Target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Scope

This block closes `ChatSendToRoom` / `ChatSendToRoomCallback` from client to Stardust and the `ChatOnSendRoomMessage` acknowledgement (`0xE7B61633`) from Stardust to the client. It separates request acknowledgement from actual message delivery, reconstructs the legacy reserved Unicode field and sequence table, maps acknowledgement errors, and documents Stardust's broadcast-success/no-response edge cases.

## Confirmed executable route

```text
chat_room_manager_send_to_room_VA_00A289A0
        |
        +-- resolve room ID; reject null room locally
        +-- increment U32 sequence at 0x01939FC0
        +-- retain sequence -> room path via 0x00A6FA60
        +-- construct ChatSendToRoom via 0x00AFB960
        `-- game_network_send_VA_00462670(reliable=true)

ChatOnSendRoomMessage / 0xE7B61633
        |
        v
chat-room listener 0x00A6E9C0
        |
        +-- descriptor slot 0x015E9F58
        +-- type text 0x018AB878
        +-- branch 0x00A6EEAD..0x00A6EED1
        v
chat_send_room_result_parse_VA_00B062B0
        |
        +-- result U32
        `-- sequence U32
        v
chat_room_manager_apply_send_result_VA_00A2D9E0
        |
        +-- consume sequence from pending table via 0x00A6FC10
        +-- result 0: silent completion
        `-- failure: publish result-specific localized room prose
```

Actual chat text arrives independently through `ChatRoomMessage` (`0xCD4CE444`), already mapped to parser `0x00B08480` and delivery handler `0x00A2D790`. The acknowledgement never carries or displays the message body.

## Client-to-server wire contract

The executable constructor at `0x00AFB960` registers four fields:

| Order | Field | Wire representation | Stardust handling | Confidence |
|---:|---|---|---|---|
| 1 | chat text | Unicode string: U32 code-unit count + UTF-16LE data | parsed into `chatMessage` | Confirmed |
| 2 | reserved/out-of-band text | Unicode string | current server advances exactly four bytes, consuming only an empty Unicode length | Confirmed structure; intended historical use Unknown |
| 3 | room ID | LE32 | parsed as `roomID` | Confirmed |
| 4 | sequence/counter | LE32 | parsed as `counter` | Confirmed |

The server callback's `shiftOffset(4)` is not arbitrary padding in the client object: it corresponds to the encoded length word of the second registered Unicode field. For compatibility with the current Stardust parser that field must be empty. A non-empty value would leave UTF-16 payload bytes before the room ID and desynchronize parsing.

`0x00A289A0` verifies the room exists locally, increments the 32-bit counter at `0x01939FC0`, retains the sequence-to-room-path association through `0x00A6FA60`, constructs the packet, and requests reliable network send. The null-room path logs `null room in sendToRoom` and sends nothing.

## Server-to-client acknowledgement

`ChatOnSendRoomMessage.h` emits only:

| Order | Field | Wire representation | Confidence |
|---:|---|---|---|
| 1 | result/error | LE32 | Confirmed |
| 2 | original sequence | LE32 | Confirmed |

The listener passes both values to `0x00A2D9E0`. `chat_send_room_pending_resolve_VA_00A6FC10` consumes the sequence from the dedicated pending table rooted at `0x0193B140` and recovers the room path used for error prose. Result zero performs no UI/state mutation after correlation cleanup.

## Result handling

| Result | Client localization key | Client action | Stardust use |
|---:|---|---|---|
| 0 | none | silent pending completion | broadcast after successful delivery |
| 9 | `send_err_not_a_moderator_prose` | publish failure for retained room | direct reply when a moderated room rejects a non-moderator |
| 16 | `send_err_insufficient_privs_prose` | publish failure for retained room | legacy-compatible client case; not emitted by this Stardust handler |
| other nonzero | `send_err_unknown_prose` | publish generic failure for retained room | fallback |

The embedded `send_err_no_message` and `send_err_not_a_member` strings are not selected by this acknowledgement handler. They belong to local/parser-side validation or other presentation paths. Stardust also does not send corresponding errors from `handleChatRoomMessage`; several invalid states return without an acknowledgement.

## Stardust transaction and routing quirks

`ChatManagerImplementation::handleChatRoomMessage()`:

1. Resolves the authenticated player and enforces player mute state.
2. Resolves room ID and requires current room membership and a valid zone.
3. For a moderated room, directly returns result 9 to a non-moderator.
4. Formats the text server-side.
5. Broadcasts `ChatRoomMessage` using per-recipient ignore-list filtering.
6. Broadcasts successful `ChatOnSendRoomMessage(sequence, 0)` to the channel.

Consequences:

- Success acknowledgement is broadcast to all room members, even though its sequence belongs to one sender. Other clients normally have no matching pending entry and silently ignore the result-zero acknowledgement after lookup.
- Sequence numbers are client-local U32 counters, not globally unique IDs. A robust replacement should scope correlation to its own pending table and make foreign/unmatched success acknowledgements harmless.
- Message delivery and acknowledgement are separate broadcasts. Ignore filtering applies to `ChatRoomMessage`; the success acknowledgement uses the ordinary room broadcast.
- Muted sender, missing room, non-member sender, missing zone, or absent player/manager can produce no acknowledgement. A client cannot wait indefinitely.
- On the success path the sender normally receives its own delivered `ChatRoomMessage` as part of the room broadcast; local echo should not be fabricated solely from acknowledgement receipt.

## Packet-handler map

| Direction/message | Parser/deserializer | Handler | Resulting state/action | Confidence |
|---|---|---|---|---|
| C→Z `ChatSendToRoom` / callback | constructor `0x00AFB960`; Stardust callback parser | client sender `0x00A289A0`; server `handleChatRoomMessage` | reliable request with Unicode text, empty reserved Unicode, room ID and sequence; validate and broadcast delivered text plus success acknowledgement | Confirmed |
| Z→C `ChatOnSendRoomMessage` / `0xE7B61633` | `chat_send_room_result_parse_VA_00B062B0` | `chat_room_manager_apply_send_result_VA_00A2D9E0` | consume sequence; success is silent; errors 9/16/fallback publish localized room failure | Confirmed |
| Z→C `ChatRoomMessage` / `0xCD4CE444` | `0x00B08480` | `0x00A2D790` | independently deliver sender/text/room message to chat consumers | Confirmed; previously closed |

## Replacement-client requirements

- Serialize the reserved second Unicode field as zero length until Stardust's callback is deliberately revised in coordination with the client.
- Preserve room ID and sequence as 32-bit wire integers even in a 64-bit client.
- Maintain a bounded sequence-to-room-context table; handle U32 wrap and expired entries.
- Treat unmatched result-zero acknowledgements as harmless because Stardust broadcasts them to peers.
- Do not use acknowledgement as chat delivery or create local echo from it; consume `ChatRoomMessage` as the authoritative displayed delivery.
- Implement timeouts for server early-return cases and distinguish timeout from an explicit nonzero result.
- Preserve structured localization keys in the asset/presentation layer.
- Apply ignore filtering behavior to delivered messages, not to acknowledgement correlation.

## Confidence and unknowns

- **Confirmed by code:** descriptor/listener/parser/result handler; two-field acknowledgement; four-field outbound constructor; empty-field compatibility requirement; U32 counter/pending table/reliable send; result switch; separate delivery opcode; Stardust validation, ignore-filtered delivery and broadcast acknowledgement behavior.
- **Strongly inferred:** all active legacy call paths supply an empty second Unicode value, as required by Stardust's four-byte skip. The constructor supports a full Unicode value, but the current server cannot safely consume non-empty content.
- **Unknown:** historical intended semantics of the second Unicode field; legacy pending-entry timeout/maximum size; whether counter collision between two clients has observable effects beyond harmless result-zero correlation cleanup.

No DRM, licensing, authentication-protection or access-control mechanism was examined or altered.

## Progress effect

Both active server packet classes should move to documented after matrix regeneration: **241 documented / 104 reachable gaps / 38 literal-opcode gaps**. Weighted whole-client coverage remains conservatively **78%**.
