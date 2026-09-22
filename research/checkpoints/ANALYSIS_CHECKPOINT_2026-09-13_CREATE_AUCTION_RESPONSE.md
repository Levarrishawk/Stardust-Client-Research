# CreateAuctionResponse / server ItemSoldMessage closure

Date: 2026-09-13  
Executable authority: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Finding

The current server class `ItemSoldMessage` is a misleading source alias for the client's `CreateAuctionResponseMessage`. The response reports whether creation of an auction or immediate-sale listing succeeded. It is not the notification that a different player purchased an existing listing.

Confidence: **Confirmed** for identity, wire schema, executable receive route, server request pairing, pending-state cleanup and typed publication. Exact localization-resource identities for each legacy result value are only partly resolved.

## Identity evidence

- Exact engine hash: `CreateAuctionResponseMessage` = `0x0E61CC92`.
- Exact engine hash: `ItemSoldMessage` = `0x4C8D5CB0`.
- Server `ItemSoldMessage` nevertheless writes opcode `0x0E61CC92`.
- Stardust.exe contains `CreateAuctionResponseMessage` and corresponding RTTI, listener registration, handler diagnostic and typed transceiver strings.

The replacement protocol catalog must therefore key this packet by opcode and validated schema, with `ItemSoldMessage` retained only as a server-source alias.

## Wire contract

After the standard LE16 operation count and LE32 opcode:

| Field | Encoding | Meaning |
|---|---|---|
| object/request ID | LE64 | object being placed for sale; also keys the client's pending operation |
| status | LE32 | listing-creation result |

The server operation count is 3. Stardust.exe parser `0x00AE8650` registers exactly the U64/NetworkId member and U32 result member.

Current named server statuses are:

| Value | Server label |
|---:|---|
| 0 | SUCCESS |
| 1 | INVALIDAUCTIONER |
| 2 | INVALIDITEM |
| 3 | VENDORNOTWORKING |
| 4 | INVALIDSALEPRICE |
| 5 | INVALIDSALEDURATION |
| 6 | ALREADYFORSALE |
| 7 | UNKNOWNERROR |
| 8 | NOTOWN |
| 9 | NOTENOUGHCREDITS |
| 13 | TOOMANYITEMS |
| 14 | OVER20000 |

The executable has a broader 0-25 dispatch domain but routes several values to its unhandled diagnostic. Current server labels and emissions, rather than speculative legacy meanings, are the compatibility authority.

## Request/response pairing

Two current zone request callbacks converge on the same server operation:

```text
CreateAuctionMessage (0xAD47021D) ---------\
                                             -> AuctionManager::addSaleItem
CreateImmediateAuctionMessage (0x1E9CE308) -/       |
                                                     -> 0x0E61CC92 response
```

Both requests parse:

1. U64 object ID
2. U64 vendor/bazaar ID
3. U32 price
4. U32 duration
5. Unicode description
6. U8 premium flag

The auction callback passes `true`; the immediate-sale callback passes `false`. `addSaleItem` emits the result on validation failures and after successful listing creation.

Risk: the two callback comments call duration minutes and seconds respectively. Comments are not wire semantics. The actual `addSaleItem` implementation and a controlled exchange should determine the accepted unit before implementing the outbound request.

## Executable route

```text
auction listener 0x006A8010
  -> CreateAuctionResponse branch 0x006A8291
  -> archive parser 0x00AE8650
  -> result extraction 0x006A88B0
  -> apply/publish 0x004CD7D0
       -> remove pending entry keyed by U64 ID
       -> recover auction/immediate-sale context
       -> map status to localized result presentation
       -> publish AuctionManagerClient::Messages::CreateAuctionResponse
       -> CuiAuctionManager / sell-list UI consumers
```

Values beyond the supported table reach `AuctionManagerClient::receiveCreateAuctionResponse unhandled result %d`. The context Boolean selects paired auction-versus-sale/vendor presentation in multiple status branches.

## Replacement-client contract

The modern client should model this as a typed `CreateListingResult` containing the fixed-width U64 object ID and U32 status. Internal pointers, handles and containers may be native 64-bit, but neither wire field changes width. On receipt it should atomically resolve/remove the pending request, retain whether the request was auction or immediate sale, expose a structured result to UI/application code, and provide a safe unknown-status fallback.

Localization belongs above the protocol layer. Protocol parsing must not depend on reproducing the original client's localization globals.

## Coverage effect

- Exact-name-miss correlations resolved: **34/37**.
- Identity-resolved lower bound: **111/114 (97.37%)**.
- Remaining direct correlations: `ConnectionServerTestMessage`, `GameSceneChangedMessage`, and `LinkDeadMessage`/`HeartBeat` ambiguity.
- Overall weighted client-research estimate remains **74%**. This block closes one focused protocol route; it does not justify inflating whole-client coverage.

## Remaining uncertainties

- exact localization keys and display wording for every legacy status;
- meanings/reachability of client dispatch values not named by the current server;
- outbound duration unit, despite the fixed U32 encoding;
- detailed behavior of each final auction UI widget after typed publication.

None of these prevents implementing and testing the response codec or a headless protocol client.
