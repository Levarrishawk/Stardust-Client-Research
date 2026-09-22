# Analysis Checkpoint: Auction Action Responses

Date: 2026-09-14  
Executable authority: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Outcome

Three active Stardust auction response gaps are closed from wire identity through the client auction model and UI-event boundary: bid/buy, item retrieval, and live-auction cancellation. The executable does not compare their numeric opcodes directly in the listener. `AuctionManagerClient` compares the incoming message-name hash against static descriptors whose embedded RTTI/name strings establish the identities exactly.

## Dispatcher map

| Message | Listener branch | Parser | Value extraction | Model handler |
|---|---:|---:|---:|---:|
| `BidAuctionResponseMessage` | `0x006A843F` | `0x00AE8CA0` | `0x006A8850` | `0x004CDED0` |
| `RetrieveAuctionItemResponseMessage` | `0x006A84C8` | `0x00AE7320` | `0x006A8A70` | `0x004CEB20` |
| `CancelLiveAuctionResponseMessage` | `0x006A832D` | `0x00AE8970` | `0x006A8880` | `0x004CED60` |

All three routes are inside `auction_message_listener_receive_VA_006A8010`.

## Wire contracts

### BidAuctionResponseMessage / `0x8FCBEF4A`

```text
LE16 opcount = 3
LE32 opcode
LE64 auctioned object/listing ID
LE32 result status
```

The parser registers the network ID at object `+0x38` and result at `+0x48`. The handler distinguishes an ordinary auction bid from an instant purchase using retained pending-request/listing state; that distinction is not encoded as an additional response field. It selects bid- or buy-specific localized feedback, updates/removes matching cached auction data as appropriate, requests/refans affected lists on successful paths, and publishes `AuctionManagerClient::Messages::BidResponse` containing the ID, result, and localized text.

The current server emits status values 0, 1, 2, 4, 9, 10, 11, 13, and 14. The executable has a larger legacy result switch (bounded through 23) and logs `AuctionManagerClient::receiveBidResponse unhandled result %d` for unrecognized values. A replacement should preserve the numeric value even when it lacks presentation text.

### RetrieveAuctionItemResponseMessage / `0x9499EF8C`

```text
LE16 opcount = 3
LE32 opcode
LE64 auctioned object/listing ID
LE32 result status
```

The response enters `0x004CEB20`, whose diagnostic identifies it as `receiveRetrieveResponse`. It selects localized success/error feedback, mutates the corresponding retained auction entry/list state, and publishes `AuctionManagerClient::Messages::Retrieved` with the ID, result, and text. Stardust sends 0 for success and may send 1 or 12 as client-visible failures. Server-internal values `0x100` (`TOOFAR`) and `0x200` (`DONTRETRIEVE`) control server flow and are not normally serialized as final responses.

### CancelLiveAuctionResponseMessage / `0x7DA2246C`

```text
LE16 opcount = 4
LE32 opcode
LE64 auctioned object/listing ID
LE32 result status
U8   trailing compatibility field (server sends 0)
```

The executable parser registers all three payload members. The listener passes the ID and result to `0x004CED60`; the trailing byte is consumed for framing but is not passed into the model handler. The handler selects status text, updates/removes the matching live-auction record on success, and publishes `AuctionManagerClient::Messages::CancelLiveAuctionResponse`. Current server statuses are 0, 1, 2, 8, and 15. Unknown values reach the embedded `receiveCancelLiveAuctionResponse unhandled result %d` diagnostic.

## Server-side correlation

- `AuctionManagerImplementation::bid` and instant-purchase paths construct `BidAuctionResponseMessage` with the auctioned object ID and status.
- `retrieveAuctionedItem` constructs `RetrieveAuctionItemResponseMessage` after validating ownership, location, inventory capacity, and item state.
- `cancelLiveAuction` constructs `CancelLiveAuctionResponseMessage`; its extra zero byte is therefore part of Stardust's active wire contract even though this client does not use it semantically.

## Replacement-client contract

- Represent every auction/listing ID as an explicit wire-width `uint64`, never a pointer-sized integer.
- Decode result codes as signed/unsigned 32-bit wire values without widening the wire representation.
- Retain request context separately so a shared bid response can be presented as bid versus instant purchase.
- Consume the cancellation compatibility byte even when ignoring its value.
- Update the auction model/cache before emitting typed UI events; UI code must not parse these packets directly.
- Preserve unknown status numbers and log them rather than assigning fabricated meanings.

## Confidence

- Message identity, wire fields, branch/parser/handler addresses, and typed publication boundaries: **Confirmed**.
- Detailed cache/list effects on the major success paths: **Strongly inferred from direct handler control flow**.
- Semantic meaning of the cancellation trailing byte: **Unknown**; only its presence and current zero value are confirmed.

## Coverage effect

After publishing these three closures and rerunning the server/client matrix, the counts move from 210 documented / 135 reachable-gap candidates / 60 literal-opcode gaps to **213 documented / 132 reachable-gap candidates / 57 literal-opcode gaps**. Weighted whole-client coverage remains 77% because the block adds depth within an already established subsystem rather than a full percentage point across the entire executable.
