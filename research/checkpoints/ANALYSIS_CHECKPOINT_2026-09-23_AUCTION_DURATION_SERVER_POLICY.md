# Analysis checkpoint — auction duration is currently server-discarded

The auction-sell client UI computes a duration in seconds for normal listing (`0x00C44A20`), with a seven-day default in one option. Both `CreateAuctionMessageCallback` and `CreateImmediateAuctionMessageCallback` in current Stardust server source parse a U32 duration and pass it to `AuctionManager::addSaleItem`. Their comments disagree about minutes versus seconds; neither comment is the runtime contract.

The implementation resolves the apparent unit dispute differently: `AuctionManagerImplementation::addSaleItem` passes the value to `createVendorItem`, but `AuctionManagerImplementation::createVendorItem` does **not use its `duration` parameter**. Instead it computes `vendorExpire = time(0) + VENDOREXPIREPERIOD` and `commodityExpire = time(0) + COMMODITYEXPIREPERIOD`, then selects a constant based on bazaar/vendor/offer state. This is **confirmed by current server source**. The client-provided duration remains a real wire field and a client UI validation gate, but it currently has no effect on listing expiration in this source tree.

This is a gameplay/UX mismatch, not a byte-level protocol failure. A replacement client must retain the U32 duration field for wire compatibility; it should not promise that the selected duration controls expiry against the current server. Any future server change to honor it would be a separate server-behavior decision and must explicitly choose units. No server code was changed in this analysis.

Evidence: `Stardust.exe` `0x00C44A20 -> 0x0092A1C0 -> 0x00929DF0 -> 0x004D1040`; server `CreateAuctionMessageCallback.h`, `CreateImmediateAuctionMessageCallback.h`, and `AuctionManagerImplementation.cpp` `addSaleItem`/`createVendorItem`.
