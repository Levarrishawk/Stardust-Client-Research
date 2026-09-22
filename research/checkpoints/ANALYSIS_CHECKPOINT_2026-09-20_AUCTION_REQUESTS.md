# Analysis Checkpoint: Client Auction Requests

## Outcome

Four active client-to-server auction callbacks are closed as complete request paths. Each server registration opcode matches an exact message class embedded in `Stardust.exe`; client construction sites serialize the same field sequence that the server callback parses.

| Message / opcode | Client construction and send | Wire body after opcode | Server action |
|---|---|---|---|
| `AuctionQueryHeadersMessage` / `0x679E0D00` | constructors at `0x00ACFFA0`/`0x00AD02F0`; request-family wrappers `0x004CFBB0..0x004D09E0`; reliable send boundary `0x00462670` | S32 locationType; S32 counter; S32 searchType; U32 itemCategory bitmask; S32 unknown1; Unicode filterText; S32 unknown2; S32 minPrice; S32 maxPrice; U8 includeEntranceFee; U64 vendorID; U8 isVendor; U16 offset | `AuctionManager::getData`, selecting list/search family and pagination |
| `BidAuctionMessage` / `0x91125453` | constructor `0x00AD0710`; wrapper `0x004D0FC0`; reliable send `0x00462670` | U64 listing ID; U32 price1; U32 price2 | `AuctionManager::buyItem`; retained client request context distinguishes ordinary bid from instant buy |
| `CancelLiveAuctionMessage` / `0x3687A4D2` | constructor `0x00AD1490`; wrapper `0x004D1130`; reliable send `0x00462670` | U64 listing ID | `AuctionManager::cancelItem` |
| `RetrieveAuctionItemMessage` / `0x12B0D449` | constructor `0x00AD1780`; wrapper `0x004D1430`; reliable send `0x00462670` | U64 listing ID; U64 bazaar/vendor terminal ID | `AuctionManager::retrieveItem` |

All fixed-width values retain the original little-endian archive encoding. The query message's two unknown S32 fields are confirmed wire fields but remain semantically unknown; a replacement must preserve/configure their observed legacy values rather than discard or reinterpret them.

## Architectural consequence

The request and response sides now form complete auction transactions: query → paginated list response, bid/buy → bid response, cancel → cancel response, and retrieve → retrieve response. These can be implemented as protocol-domain commands independent of UI widgets, with correlation/list context retained in the auction subsystem.

## Confidence

- Identities, opcodes, field order/width, client construction, reliable send boundary, server parsing and manager calls: **Confirmed**.
- Semantic labels for query `unknown1` and `unknown2`: **Unknown**.

