# Analysis checkpoint — auction sell mediator to request sender

Static target: verified `C:\Stardust-DEV\Stardust.exe`; current `Stardust-3` server source used only for correlation. No traffic capture or runtime mutation. Ghidra decompilation is reconstructed pseudocode.

## Client route

`SwgCuiAuctionSell` is constructed at `0x00C43480` and retains inventory/datapad container providers plus create-response and selection listeners. Activation `0x00C43FD0` configures the selected item/container context; deactivation `0x00C44580` removes the transceivers and UI providers. The submit callback `0x00C44CA0` reads the mediator's mode bit at the `+0xB4` control: one branch calls `0x00C44870`, the other `0x00C44A20`.

| Branch | UI validation | Request bridge | Wire message |
|---|---|---|---|
| `0x00C44870` | requires sell eligibility, positive parsed price and nonzero selected object ID | `0x0092A200 -> 0x00929DF0 -> 0x004D1040` | `CreateImmediateAuctionMessage` / `0x1E9CE308` |
| `0x00C44A20` | requires positive price; for duration-controlled option computes `((days*24 + hours)*60 + minutes)*60` and requires positive result; requires selected vendor/item context | `0x0092A1C0 -> 0x00929DF0 -> 0x004D1040` | `CreateAuctionMessage` / `0xAD47021D` |

The normal branch otherwise uses a `0x93A80` (604,800-second) default. Both wrappers converge on `0x004D1040`, which constructs the respective named message (`0x00AD0A60` normal, `0x00AD0F50` immediate) and calls reliable network send `0x00462670`. The common body is U64 item ID, U64 vendor/terminal ID, U32 price, U32 duration, Unicode description, U8 premium. The immediate constructor carries an additional trailing U8 field whose meaning is not established by this trace; the current server callback reads only the common fields. Preserve/profile the extra byte rather than silently conflating the two message classes.

The server callbacks parse the common fields, validate an authenticated player and vendor/bazaar terminal, then call `AuctionManager::addSaleItem` with auction-mode true for normal listing and false for immediate sale. The manager emits `ItemSoldMessage` (wire `CreateAuctionResponseMessage` / `0x0E61CC92`). The client auction manager routes that response to a typed transceiver; sell-mediator callback `0x00C44EF0` correlates the response's object ID with its selected item before `0x00C44F30` performs the result UI update. This is an end-to-end UI → packet → server → result route, not merely a message class inventory.

The exact mode-bit/UI-widget label mapping and immediate message's trailing byte semantic remain open. The route and normal/immediate message identities are **confirmed by code**, as are the server callback destinations. The duration input is confirmed seconds on the normal UI calculation; the server's actual handling is addressed separately in the following checkpoint.
