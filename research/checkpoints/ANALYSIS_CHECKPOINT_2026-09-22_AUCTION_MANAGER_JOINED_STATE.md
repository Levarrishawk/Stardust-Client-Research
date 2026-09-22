# Auction Manager Joined State

## Outcome

The previously decoded auction packets form one client coordinator rather than
independent UI transactions. The replacement boundary can now be stated as a
typed market service with retained lists, detail cache, request context,
pagination state, and result events.

## Confirmed inbound routes

`AuctionManagerClient` listener `0x006A8010` discriminates and decodes:

- query headers `0xFA500E52` -> indexed record resolution -> one of eight list
  channels for search types 2..9;
- details `0xFE0E644B` -> description/attributes/template/customization cache;
- create response `0x0E61CC92`;
- bid/buy response `0x8FCBEF4A`;
- retrieve response `0x9499EF8C`;
- cancel response `0x7DA2246C`;
- legacy accept-bid response `0xC58A446E` (no current server producer);
- vendor-owner response `0xCE04173E`;
- commodity item-type catalog `0xD4E937FC`.

The manager's RTTI exposes owned subscriptions for `Retrieved`,
`AcceptBidResponse`, `CancelLiveAuctionResponse`, `CreateAuctionResponse`,
`VendorOwnerResponse`, and message-box `Closed`. These are internal typed
events, not additional network opcodes.

## Retained model

The query response replaces the selected displayed list, retains page offset
and continuation state, and reconciles matching listing IDs across other lists.
Details are cached by listing ID and merged into visible records. Action
responses carry listing ID plus result and use retained request/listing context
to distinguish bid versus immediate buy and to update/remove successful items
before publishing localized UI feedback. Vendor ownership is a separate
capability result keyed by terminal object ID.

The compact 72-byte response record, 112-byte resolved record, and larger
legacy UI record are x86 implementation layouts only. None is a wire or asset
structure.

## Outbound service boundary

The coordinator issues the already confirmed query, details, bid/buy, cancel,
retrieve, create-sale, and vendor-owner requests. U32 request counters and
stable U64 listing/terminal IDs belong in request contexts; x64 UI code should
never retain pointers to records across an asynchronous response.

## Current Stardust constraints

- Query pages contain at most 100 records.
- Current commodity catalog response identifies `Core3.0` but contains no
  hierarchy entries, so the original asset-side item taxonomy remains needed
  for a useful browser unless the server is extended later.
- Accept-bid response is a client-supported legacy route with no current
  producer and is not required for the first interoperability milestone.
- Two query S32 fields, the cancel compatibility byte, vendor-owner trailing
  U16, and some auction option-bit names remain opaque but byte-preserved.

## Replacement model

```text
MarketProtocol codecs
  -> MarketService (request contexts + ID-keyed caches)
     -> list snapshots + pagination
     -> listing details
     -> action results
     -> vendor capability
  -> UI view-model subscribers
```

Decode counts and string-table indices with hard bounds. Preserve unknown
fields and option bits. Perform cache mutation before notifying subscribers.
Keep transport compression outside application codecs.

## Confidence

- Message identities, schemas, listener, list channels, details merge, typed
  result publications: **Confirmed by code**.
- Complete coordinator/cache ownership model: **Strongly inferred** from the
  joined handlers, transceiver RTTI, and request wrappers.
- Friendly semantics for preserved compatibility fields: **Unknown**, bounded
  and non-blocking.

