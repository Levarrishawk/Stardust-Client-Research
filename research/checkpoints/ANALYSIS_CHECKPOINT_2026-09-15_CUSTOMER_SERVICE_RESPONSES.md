# Stardust.exe Analysis Checkpoint: Customer-Service Response Cluster

Date: 2026-09-15  
Target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Result

This block closes four customer-service matrix rows: `CancelTicketResponseMessage`, `GetArticleResponseMessage`, `NewTicketActivityResponseMessage`, and the misspelled server callback `NewTicketActivityMessageCalback`. All three responses are positively implemented by the client customer-service listener at `0x00A19580` (containing the recovered branches around `0x00A19D20`-`0x00A19FA3`).

## Protocol map

| Direction | Message/opcode | Fields | Client/server action |
|---|---|---|---|
| Z→C | cancel response `0xD6FBF318` | result U32 (0 success), ticket ID U32 | parser `0x00AFA0D0`; handler `0x005CB9E0(success,ticketId)` |
| Z→C | article response `0x934BAEE0` | result U32; Unicode article when successful, blank Unicode framing on failure | compressed; parser `0x00AF7BB0`; publish success/article pair through transceiver at `0x0193885C` |
| C→Z | new-activity request `0x274F4E78` | ticket ID U32 | server callback echoes response flag zero and ticket ID |
| Z→C | new-activity response `0x6EA42D80` | flag U8; ticket ID U32 | parser `0x00AF7880`; handler `0x005CA640(flag,ticketId)` |

The mechanical matrix associates the response opcode with the callback class because both occupy one source header. The callback factory registration proves that the request opcode is actually `0x274F4E78`.

## Client listener evidence

- Cancel descriptor `0x015E9528`, branch `0x00A19D1E`.
- Article descriptor `0x015E9544`, branch `0x00A19F11`.
- New-activity response descriptor `0x015E9548`, branch `0x00A19FA3`.
- Each branch constructs its exact named codec from the received archive, derives success as result==0 where applicable, then calls a typed manager/transceiver destination.
- Article handling packages both success and Unicode article content, preserving failure state separately from empty content.

## Server reachability

The new-activity request callback is actively registered and constructs its response. No active server construction site was found for cancel or article responses outside their definitions, so the client implementations are real but those two current Stardust producers are dormant/incomplete.

## Replacement requirements

- Keep request and response opcodes distinct for new-ticket activity.
- Preserve result/flag widths and ticket IDs as U32.
- Decompress article responses before Unicode parsing and enforce bounded text lengths.
- Represent article outcome as `(success, content)` so an empty successful article is distinguishable from failure.
- Dispatch results on the customer-service/UI owner thread.
- Support cancel/article response parsing for compatibility, but do not depend on current Stardust producing them without corresponding server work.
- Preserve the server class spelling only in source cross-references; use a corrected internal replacement-client type name.

## Confidence

- **Confirmed:** response codecs, descriptors, listener branches, field flow and semantic destinations; exact server schemas; new-activity request registration/echo.
- **Unknown:** meanings of nonzero new-activity flags, absent current cancel/article producers, and full customer-service backend behavior.

## Progress

Four reachable rows move to documented: **262 documented / 83 reachable gaps / 20 literal-opcode gaps**. Weighted whole-client coverage remains **78%**.
