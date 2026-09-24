# Analysis checkpoint — auction detail screen, cache miss and actions

Question A09 / W10+W04. This extends the already established `GetAuctionDetails` request/response and auction command mappings to the previously unvisited `SwgCuiAuctionDetails` mediator. Source of client behavior: verified original `C:\Stardust-DEV\Stardust.exe`; decompiled output is reconstructed pseudocode.

RTTI vtables `0x015EF914/0x015EF998` and constructor `0x00C1FD90` identify the mediator. It binds Exit, Bid, Retrieve, Sell, Withdraw, Accept, Next and Prev controls; composite item/description text; price, name, location, seller, time, type, description and attributes; plus a preview viewer. Missing required `textAttribs` or `viewer` CodeData reaches a fatal diagnostic, so the original UI page layout has a strict binding contract even though a replacement need not recreate its widget class graph.

Activation `0x00C20360` subscribes to `AuctionManagerClient::Messages::DetailsReceived` (`0x00C210F0`) and `AuctionToViewChanged` (`0x00C21120`). Deactivation `0x00C20400` disconnects both exact owned subscriptions and clears the selected NetworkId. The detail callback `0x00C210F0` compares the response's U64 listing ID against the retained selection and updates only a match. The selection-change callback pulls the selected cached record and calls `0x00C20550`; the latter stores selected ID, updates action availability and display fields, and reaches `0x00C20A10`. If the selected record lacks detail data, `0x00C20A10` calls the previously mapped `0x004CFB10` request sender. Thus the screen is a consumer of the existing cache/packet path and has an explicit cache-miss fetch, not its own decoder.

The UI callback `0x00C20FB0` maps controls to actions:

| Control | Immediate client route | Interpretation |
|---|---|---|
| Exit | `0x009C17A0` | close screen |
| Bid | selected record lookup, `0x004D1B40`, then opens AuctionBuy when immediate-sale flag is set or AuctionBid otherwise | modal route, not direct packet construction here |
| Retrieve | `0x00929990` | separate retrieve path |
| Sell | opens AuctionSell, supplies selected ID, then closes details | separate sale mediator |
| Withdraw / Accept | `0x00929D60` / `0x00929CD0` when selected ID is nonzero | separate actions; current-server availability/policy must be checked independently |
| Next / Prev | `0x004D2350(1/0)` | list selection navigation |

`0x00C21370` periodically checks whether the retained listing is still represented in the auction cache and refreshes the screen after a timer threshold; exact timer/focus policy is not fully classified. The server's `GetAuctionDetailsResponse` body and the different command result packets remain as documented in the [packet map](../../docs/PACKET_HANDLER_MAP.md); this block adds no opcode. Do not infer that every visible button is enabled or supported by the current Stardust server in every context.

Confidence: **confirmed by code** for bindings, subscriptions, matching-ID refresh, cache-miss detail request and immediate button routes; **partial** for exact button enablement, preview behavior, periodic refresh and server policy per action. No whole-client percentage change.
