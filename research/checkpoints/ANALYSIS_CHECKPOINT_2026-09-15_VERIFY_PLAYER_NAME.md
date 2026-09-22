# Analysis checkpoint: player-name existence verification

Date: 2026-09-15

## Result

The bidirectional `VerifyPlayerNameMessage` / `VerifyPlayerNameResponseMessage` exchange is now mapped. Contrary to the initially plausible class-name interpretation, this is not the character-creation naming validator. The executable ownership evidence ties its response transceiver to `SwgCuiHarassmentReport`; the request asks whether a named player exists so the customer-service/report workflow can identify a target.

The analysis also confirms a live client/server schema mismatch in Stardust's response construction.

## Client request

The client request type is `VerifyPlayerNameMessage`, registered on the server as opcode `0xBB8CAD45`.

Generated message constructor `0x00ADE5A0` serializes:

| Field | Type |
|---|---|
| operand count | U16 |
| opcode/type | U32 (`0xBB8CAD45`, derived from the message name) |
| queried player name | UTF-16 string |
| requesting player's object ID | U64 |

The message object keeps its UTF-16 value at `+0x38` (accessor `0x00ADE960`) and NetworkId at `+0x48` (accessor `0x00ADE970`). Deserializer `0x00ADE730` confirms the same schema.

Request API `0x005ACED0` performs a local input check through `0x005AB830`. If the input fails that check it immediately publishes a negative result without network traffic. Otherwise it obtains the local player's NetworkId, constructs `VerifyPlayerNameMessage` at `0x00ADE5A0`, and sends it reliably through `0x00462670`.

## Server request handling

`ZonePacketHandler` registers `VerifyPlayerNameCallback` for `0xBB8CAD45`. The callback parses UTF-16 name then U64 player ID. It requires an authenticated player object, but does not otherwise use the supplied player ID. It calls `PlayerManager::existsName(name.toString())` and returns that Boolean.

The Boolean therefore means **the queried name exists**, despite the local variable and response constructor calling it `success`. It is not `NameManager::validateName`, does not apply species syntax rules, and does not reserve or create a name.

## Expected client response schema

The executable response class is `VerifyPlayerNameResponseMessage`. Constructor `0x00B046D0`, deserializer `0x00B04870`, and accessors establish this payload:

| Field | Type | Client member |
|---|---|---:|
| existence/result flag | U8 Boolean | `+0x3C`, accessor `0x00B04A80` |
| echoed queried name | UTF-16 string | `+0x44`, accessor `0x00B04A70` |

Community-message listener `0x00A6E9C0` selects the response descriptor, deserializes it at branch `0x00A6F187`, extracts the UTF-16 string and Boolean, and publishes the pair through `0x005ACFA0`. Executable RTTI identifies the owning response transceiver as belonging to `SwgCuiHarassmentReport`.

## Confirmed Stardust response defect

Stardust currently emits:

1. U16 operand count `9`;
2. U32 opcode `0xF4C498FD`;
3. U8 existence Boolean;
4. U32 value `1`;
5. no UTF-16 code unit and no echoed queried name.

The executable expects field 4 to be the UTF-16 string's U32 code-unit length, followed by that many U16 code units. Thus `insertInt(1)` declares a one-character string but supplies no character. This is a **confirmed wire-schema mismatch**, not an alternate response shape: the generated client constructor and deserializer register a Boolean and a Unicode-string variable, and the consumer explicitly retrieves both.

Likely intended server behavior is to echo the queried name with `insertUnicode(name)` after the Boolean. That is an implementation recommendation only; this analysis block does not modify the server. Until corrected, a replacement client should regard this packet as truncated. A defensive implementation may report a protocol error or associate the result with its outstanding request, but silently fabricating the missing name would be an interoperability policy decision.

## End-to-end mapping

| Message/opcode | Codec | Handler | Resulting state/action | Confidence |
|---|---|---|---|---|
| `VerifyPlayerNameMessage` / `0xBB8CAD45` C->Z | client constructor `0x00ADE5A0`; server callback parser | `VerifyPlayerNameCallback::run` | query `PlayerManager::existsName`; prepare Boolean response | Confirmed |
| `VerifyPlayerNameResponseMessage` / `0xF4C498FD` Z->C | expected parser `0x00B04870`; current server payload malformed after Boolean | listener branch `0x00A6F187` -> publisher `0x005ACFA0` | publish `(exists, queriedName)` to harassment-report workflow | Confirmed expected client contract and server mismatch; exact report-widget reaction Strong |

## Replacement-client requirements

- Preserve UTF-16 framing and U64 requester ID in the request.
- Treat the response Boolean as `nameExists`, not general name validity.
- Correlate the response with an outstanding query; the original protocol uses the echoed name rather than an explicit numeric request ID.
- Reject or explicitly recover from a declared string whose bytes are absent.
- Keep this lookup separate from character creation, rename validation and `NameManager` policy.
- Model NetworkId as a fixed U64 wire value, independent of native pointer size.

## Remaining bounded unknowns

- The exact local input policy inside `0x005AB830` was not needed to establish the protocol schema.
- The exact visual error text/state chosen by `SwgCuiHarassmentReport` after the transceiver publication remains downstream UI detail.
- Historical intent behind Stardust's literal `insertInt(1)` is unknown.
