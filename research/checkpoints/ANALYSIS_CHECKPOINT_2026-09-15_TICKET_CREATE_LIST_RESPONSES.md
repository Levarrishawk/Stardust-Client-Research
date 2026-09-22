# Stardust.exe Analysis Checkpoint: Ticket Create/List Responses

Date: 2026-09-15  
Target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Result

This continuation closes `CreateTicketResponseMessage` (`0x550A407A`) and `GetTicketsResponseMessage` (`0xBB567F98`) in the same customer-service listener `0x00A19580`. Both client paths are real; both current server implementations are dormant/incomplete.

## Create-ticket response

Descriptor `0x015E9530` selects branch `0x00A19A25`, which parses through `0x00AFA650`. The client codec registers two 32-bit fields. The branch interprets the first as result (`success = result==0`) and retains the second in the published response structure. A special `(result==6, second==0)` case follows a separate notification path; other results publish to registered customer-service observers.

Stardust's server header declares operand count nine but writes only one U32 parameter, and its only manager construction is commented out. It therefore does not satisfy the complete client codec contract. A replacement must document this mismatch rather than copying the incomplete constructor as authoritative.

## Ticket-list response

Descriptor `0x015E9538` selects branch `0x00A19D8E`, parser `0x00AF9860`, then handler `0x005CA660`. The codec registers:

1. result/status U32;
2. secondary U32;
3. counted ticket record vector.

Each Stardust ticket record contains two reserved U32 values, ASCII name, Unicode body, reserved ASCII string, ticket ID U32, timestamp U64, unread U8 and closed U8. The listener derives success from result==0 and passes the full parsed message into the manager destination.

The server class initializes both header U32 values to zero and patches the count at absolute packet offset 15 as tickets are appended. No active producer is present in the current tree.

## Replacement requirements

- Model create response as two U32 fields; tolerate a truncated legacy/server-development packet only in a bounded compatibility mode.
- Preserve the ticket list header and per-record field order exactly, including U64 timestamp and the inverted read/open encodings used by Stardust.
- Validate count against remaining bytes before allocation and bound Unicode/ASCII lengths.
- Keep ticket IDs U32 and timestamps U64 regardless of host architecture.
- Publish immutable result objects on the UI thread rather than retaining pointers into receive buffers.
- Treat current server headers as dormant compatibility references pending implementation tests; neither is required by the networking POC.

## Confidence

- **Confirmed:** executable descriptors, codecs, branch semantics and destinations; server byte construction and absence of producers.
- **Confirmed mismatch:** create response writes fewer fields than the client registers.
- **Unknown:** second create field meaning, result code six semantics, second list-header field and timestamp epoch/unit.

## Progress

Two reachable packets move to documented: **264 documented / 81 reachable gaps / 18 literal-opcode gaps**. Weighted whole-client coverage remains **78%**.
