# Analysis Checkpoint: Accept Auction Response

## Outcome

`AcceptAuctionResponseMessage` (`0xC58A446E`) is closed from wire identity through the client auction-manager publication boundary. The packet remains implemented by `Stardust.exe`, but the current Stardust server source contains only an unused packet wrapper and no producer. It is therefore a confirmed legacy-compatible client path, not a currently reachable server flow.

## Evidence

- Server wrapper: `AcceptAuctionResponseMessage.h` emits `U16 3`, opcode `0xC58A446E`, `U64 objectID`, and `U32 errorCode`. No construction site exists under current `MMOCoreORB/src`.
- Executable descriptor `0x015EB86C` points to the exact name `AcceptAuctionResponseMessage`; the listener at `0x006A8010` hashes that descriptor and selects the branch at `0x006A83A3`.
- Archive parser `0x00AEBE10` registers the network ID at object `+0x38` and the result integer at `+0x48`.
- Extractor `0x006A8820` supplies the parsed ID to handler `0x004CF230`; the listener supplies the parsed result separately.
- Handler `0x004CF230` handles result values through 15, uses `accept_bid_successful` on its success path, maps known failures to `err_accept_bid_*` string IDs, logs `receiveAcceptAuctionResponseMessage unhandled result %d` otherwise, updates the retained auction record on success through `0x004CC1C0`, and publishes a typed result through the auction-manager transceiver at `0x0190ED38`.

## Replacement contract

Treat the wire body as exactly `U64 listing/object ID + U32 result`, in original little-endian SWG archive encoding. Preserve unknown result values even if no localized presentation is available. Implement the decoder in the protocol library, but gate active use on a future server producer or an observed capture; the current Stardust source cannot cause this response.

## Confidence

- Identity, opcode, schema, parser, dispatch, result range, success mutation, and publication: **Confirmed**.
- Individual error-code-to-localization mapping: **Strongly inferred** from the handler jump table and adjacent initialized string IDs; not required for the networking proof of concept.
- Current server reachability: **Confirmed absent** by source-wide constructor search.

