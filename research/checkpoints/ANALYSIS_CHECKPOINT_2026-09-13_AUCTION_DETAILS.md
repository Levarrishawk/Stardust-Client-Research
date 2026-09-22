# Analysis Checkpoint: Auction Detail Request/Response

Date: 2026-09-13  
Executable authority: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Outcome

The prior `AuctionItemDescriptionMessage` server-name miss is closed. Opcode `0xFE0E644B` is the client identity `GetAuctionDetailsResponse`, and the executable supports a richer schema than the two obsolete standalone server wrappers suggest.

## Request path

`auction_request_details_VA_004CFB10` constructs `GetAuctionDetails` through `0x00ACFCC0` and sends it reliably through `0x00462670`.

Wire body after the standard opcount/opcode header:

`U64 auctionListingId`

The engine hash confirms `GetAuctionDetails == 0xD36EFAE4`, exactly matching the callback registered by the current zone server. The server resolves the listing and underlying object through `AuctionManager::getItemAttributes`.

## Response schema

Canonical identity: `GetAuctionDetailsResponse` / `0xFE0E644B`.

```text
U64       auctionListingId
Unicode32 description
U32       attributeCount
repeat attributeCount:
  ASCII16   attributeName
  Unicode32 attributeValue
ASCII16   clientTemplatePath
ASCII16   customizationString
```

The executable's response object contains `Auction::ItemDataDetails`; serializer/parser routines independently show the U64, Unicode string, 24-byte in-memory attribute pairs, and two final ASCII strings. Counts and string lengths remain wire-width values and must be bounded independently in a new x64 client.

## Executable receive path

```text
auction listener 0x006A8010
  -> hash comparison for GetAuctionDetailsResponse
  -> archive parser 0x00AE7B60
  -> plain record conversion 0x006A88E0
  -> cache/list application 0x004CD5B0
  -> AuctionManagerClient::Messages::DetailsReceived
  -> auction detail/sell UI subscribers
```

The handler inserts or replaces a details record keyed by the network/listing ID, looks for matching records across active auction-list collections, applies the details to matches, and publishes the resulting record through the typed details transceiver.

## Server correlation and dead wrappers

The current active server constructs `AttributeListMessage(objectid, description)` with `0xFE0E644B`, appends the object's generated attributes, then appends the client template filename and tangible customization string.

`AuctionItemDescriptionMessage` and `GetAuctionDetailsResponse.h` are not instantiated anywhere under current `MMOCoreORB/src`. Both encode the same response prefix with zero attributes; neither includes the final template/customization fields used by the active producer and expected by the executable. They should be treated as obsolete convenience wrappers, not separate protocol messages and not the authoritative complete schema.

Hash evidence:

- `GetAuctionDetailsResponse` -> `0xFE0E644B`
- `AuctionItemDescriptionMessage` -> `0x174D8A0E`

## Replacement-client contract

- Canonicalize by numeric opcode plus direction and validated schema; retain source class names as aliases.
- Key records by a fixed-width U64 network/listing ID, never by pointer-sized types.
- Bound the attribute count and every string length before allocation.
- Preserve UTF-16/Unicode semantics for description and values and narrow-string semantics for names/template/customization.
- Publish a model-level `AuctionDetailsReceived` event after cache mutation; UI widgets should not parse network packets directly.
- Accept zero attributes, but require the final template/customization strings for the active Stardust schema. A compatibility parser may diagnose older truncated wrappers separately rather than silently desynchronizing.

## Coverage effect

- Server exact-name misses correlated: 33 of 37.
- Remaining direct correlations: 4.
- Message-class identity-resolved lower bound: 110 of 114 (96.49%).
- Overall weighted behavioral-research estimate remains 74%; this closes one complete feature route but does not round the whole-client estimate upward.

## Remaining auction gap

`ItemSoldMessage` (`0x0E61CC92`) is now the only active auction-specific message in the server-name miss ledger without a direct executable consumer correlation.
