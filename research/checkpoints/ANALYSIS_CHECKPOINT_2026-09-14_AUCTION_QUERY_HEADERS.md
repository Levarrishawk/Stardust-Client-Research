# Analysis Checkpoint: Auction Query Headers Response

Date: 2026-09-14  
Executable authority: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Outcome

`AuctionQueryHeadersResponseMessage` (`0xFA500E52`) is closed from the active Stardust producer through client deserialization, indexed-string resolution, list-family dispatch, cache reconciliation, pagination metadata, and typed list-change publication.

## Receive path

```text
AuctionManagerClient listener 0x006A8010
  -> message-descriptor comparison at 0x006A8120
  -> parser 0x00AE9090
  -> compact-header/string-index resolver 0x00AE9B40
  -> screen switch at 0x006A8158
  -> screen wrapper 0x004CCED0..0x004CCFB0
  -> shared cache/list applicator 0x004CCFD0
  -> list-specific AuctionManagerClient transceiver
```

The embedded descriptor at `0x015EB81C` names `AuctionQueryHeadersResponseMessage`, matching the server class and opcode exactly.

## Complete active wire schema

All integers are little-endian. Strings use the existing SWG ASCII16 or Unicode32 archive codecs.

```text
LE16 opcount = 8
LE32 opcode = 0xFA500E52
LE32 requestCounter
LE32 screen/searchType

LE32 stringCount
repeat stringCount:
  ASCII16 string

LE32 itemNameCount
repeat itemNameCount:
  Unicode32 itemName

LE32 itemCount
repeat itemCount:
  LE64 auctionedObjectId
  U8   itemNameIndex
  LE32 currentPrice
  LE32 secondsRemaining
  U8   saleType                 // 0 auction, 1 instant/vendor sale
  LE16 vendorUidStringIndex
  LE64 sellerId
  LE16 sellerNameStringIndex
  LE64 bidderOrBuyerId
  LE16 bidderNameStringIndex
  LE32 myProxyBid
  LE32 myBid
  LE32 itemType
  LE32 auctionOptions
  LE32 entranceFee

LE16 pageOffset
U8   continues
```

The client parser registers these as seven archive members at object offsets `+0x38`, `+0x40`, `+0x48`, `+0x58`, `+0x68`, `+0x84`, and `+0x8C`. The raw item decoder at `0x00AE9D80` confirms every field width and order independently of the server source.

## Compact versus resolved records

The parser's raw item array uses 72-byte (`0x48`) in-memory records. `0x00AE9B40` validates/uses the three indices and constructs a separate 112-byte (`0x70`) resolved record:

- `itemNameIndex` resolves through the Unicode item-name array.
- vendor UID, seller name, and bidder/buyer name indices resolve through the shared ASCII string table.
- scalar IDs, price/time/type/options/fee values are copied without reinterpretation.

These are legacy x86 object-layout facts, not wire structures. A new x64 client should decode directly into an owned model record and must bounds-check all three counts and every referenced index before dereferencing.

## Search-type to client-list mapping

The listener accepts screen/search types 2 through 9. Its jump table maps them to the client's eight retained list channels:

| Wire search type | Server meaning | Client list publication |
|---:|---|---|
| 2 | all/category results | `ListAllChanged` |
| 3 | player sales | `ListSalesChanged` |
| 4 | player bids | `ListBidsChanged` |
| 5 | player stockroom | `ListAvailableChanged` |
| 6 | vendor offers | `ListVendorOffersChanged` |
| 7 | vendor selling | `ListVendorSellingChanged` plus vendor-location list update |
| 8 | vendor stockroom | `ListVendorStockroomChanged` |
| 9 | player offers to vendor | same retained vendor-offers channel |

Values outside 2-9 do not enter a list wrapper.

## Cache and pagination behavior

`auction_query_headers_apply_list_VA_004CCFD0` selects the target retained list, replaces its current displayed records with the resolved response records, and reconciles matching IDs into other retained lists so shared auction state does not become stale. Each resolved record is converted into the larger `AuctionManagerClientData` model representation before insertion.

The handler retains the response `pageOffset` and `continues` flag per client list and publishes the corresponding typed list-change event after mutation. The request counter is parsed and preserved by the message object but is not passed into the observed list applicator; the client therefore does not use it as the final cache-mutation gate in this path.

Stardust currently sets transport compression on this response. Compression is a transport-envelope concern and must be removed before the application parser sees the schema above.

## Server behavior affecting compatibility

- Stardust limits a page to `ITEMSPERPAGE = 100` while constructing the result.
- `secondsRemaining` is clamped to zero when already expired.
- The server builds one deduplicated/sorted shared string table for vendor UID, seller name, and bidder/buyer name.
- For an unsold auction, bidder ID is zero and bidder-name index refers to the table's empty string.
- `0x800` is ORed into auction options when the current player owns a sale/offer and may withdraw it.
- Entrance fee is obtained from the vendor terminal's root building when available.

## Replacement-client contract

- Preserve all wire widths regardless of x64 internal types.
- Bound counts, total allocation, UTF-16 length, and every item index before constructing model records.
- Decode transport compression outside the auction codec.
- Treat `pageOffset` and `continues` as explicit pagination state, not native container offsets.
- Replace/update the auction model before notifying UI subscribers.
- Preserve unknown auction-option bits; only interpret bits proven by either endpoint.
- Do not serialize or deserialize the observed 72-/112-/184-byte x86 runtime records as network structures.

## Confidence

- Identity, receive path, field order/widths, index resolution, screen dispatch, and typed publications: **Confirmed**.
- Cross-list reconciliation intent and pagination-state ownership: **Strongly inferred from direct handler control flow**.
- Meanings of unlabelled option bits other than the server-proven withdrawal bit: **Unknown**.

## Coverage effect

This closes one of the highest-use remaining literal-opcode candidates. The refreshed matrix now reports **214 documented / 131 reachable-gap candidates / 56 literal-opcode gaps**, improving from 213/132/57. Weighted whole-client coverage remains 77%; protocol breadth and auction subsystem confidence improve without constituting another whole-client percentage point.
