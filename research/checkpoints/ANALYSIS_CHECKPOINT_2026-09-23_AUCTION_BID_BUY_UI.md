# Auction bid and immediate-buy mediator paths (overnight block 31)

This extends the already documented `BidAuctionMessage` protocol with its concrete UI workflow. The two mediators are distinct (`SwgCuiAuctionBid` and `SwgCuiAuctionBuy` RTTI/vtables) but share request wrapper `0x004D0FC0` and response event type `AuctionManagerClient::Messages::BidResponse`.

| UI path | Selection and pre-send checks | Submission |
| --- | --- | --- |
| Bid `0x00C06380 -> 0x00C06110` | Selected auction must exist and not be instant-sale (`+0x28` false). UI pre-fills starting price and suggested increments of max(10, 10%) and max(20, 20%). Submission parses two numeric inputs, requires entered bid above retained current price, and compares available credit totals with the larger/proxy amount. | `0x004D0FC0(listingId, enteredBid, proxyAmount, 0)` |
| Buy `0x00C13710 -> 0x00C134E0` | Selected auction must exist and be instant-sale (`+0x28` true). UI displays retained listing price and checks available credit totals. | `0x004D0FC0(listingId, listedPrice, listedPrice, 1)` |

The final 0/1 wrapper argument is retained client request context; the wire constructor sends listing U64 plus the two U32 prices, not a separate Boolean. Both paths replace an existing confirmation/wait message, install a message-closed callback, and subscribe while active to `AuctionToBidOnChanged` and `BidResponse`. Deactivation removes those typed listeners and releases the pending message handle. Response callbacks `0x00C065D0` (bid) and `0x00C138E0` (buy) dismiss the pending message, present the response text from the typed event, and close their mediator on status zero. Thus the UI does not parse network bytes directly.

The Stardust server callback `BidAuctionMessageCallback` parses the same three wire fields and calls `AuctionManager::buyItem`. The server distinguishes the listing's auction-versus-instant-sale state and applies its own price, credit and ownership checks before emitting `BidAuctionResponseMessage`; client-side checks are advisory and may race with server state. The pair of prices must remain distinct for bidding, even though immediate buy sends equal values.

Confidence: **confirmed** for mediator types, direct call edges, input/status gates, wrapper arguments, typed event subscriptions and close-on-zero behavior; **strongly inferred** for friendly `proxyAmount` label from UI/server auction semantics; **unknown** for every localized error string and exact UI button focus behavior.
