# Customer-service shell and ConnectPlayer trigger

Date: 2026-09-24. Question A09/W10 plus W04: what does the previously unvisited `SwgCuiService` shell own, and does it originate the server's `ConnectPlayer` request? Input is `C:\Stardust-DEV\Stardust.exe`, SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Method: RTTI/vtable references, read-only Ghidra decompilation including transient raw-entry functions, then current Stardust-3 source correlation. Pseudocode is reconstructed, not original source.

## Executable evidence

| VA | Observed behavior | Confidence |
|---|---|---|
| `0x00F80D40` | `SwgCuiService` constructor installs vtables `0x015F6A90/0x015F6A0C`, creates four child mediators from `pageHowToSearch`, `pageKnow`, `pageKnownIssues`, `pageTickets`, and binds Bug, Harassment, Done and tab controls. | Confirmed |
| `0x00F81220` | Activation calls the currently selected child, enables a UI mode through `0x00881940(1)`, tests customer-service state via `0x005CC140`, and calls `0x005CA570` when false. | Confirmed control flow; UI-mode semantics Strongly inferred |
| `0x005CA570` | When a network connection exists, the customer-service state flag is false, and a timer gate allows it, constructs at `0x00ADF800` and reliably sends a message, then updates the request timestamp. | Confirmed |
| `0x00ADF800` | Constructs a class whose embedded name is `ConnectPlayerMessage`. The packet opcode is corroborated by server receive registration `0x2E365218`; the constructor's generic base serializer obscures the opcode in pseudocode. | Confirmed class/send; opcode Strongly inferred from matching server route |
| `0x005CA600` | On a true result, initializes empty manager vectors if needed and sets the same customer-service state flag. | Confirmed |
| `0x00F812A0` | Done closes the shell. Bug dispatches a UI action. Harassment gates on two customer-service vectors and a configured ticket limit, otherwise shows a localized message before action dispatch. | Confirmed branching; exact action-string identity Unknown |
| `0x00F814B0` | Tab callback iterates four child slots, deactivates nonselected children and updates the selected tab state. | Strongly inferred from control flow and constructor layout |
| `0x00F81260`, `0x00F81150` | Deactivation restores the UI mode and closes children; destruction releases the four children and controls. | Confirmed |

`ConnectPlayerResponse` was already mapped: client listener `0x00A19580` -> parser `0x00AFB0F0` -> manager consumer `0x005CA600`. Current server's `ConnectPlayerMessageCallback` in `MMOCoreORB/src/server/zone/packets/zone/ConnectPlayerResponseMessage.h` has an empty parse and sends a response containing U32 zero. Its registration is `MMOCoreORB/src/server/zone/ZonePacketHandler.cpp` `0x2E365218`. This supports a customer-service availability/handshake path, **not** the primary account, character, or zone-authentication handshake. Prior [ticket UI](ANALYSIS_CHECKPOINT_2026-09-22_CUSTOMER_SERVICE_TICKET_UI.md) covers a different `SwgCuiTicketSubmission` child and is not recounted here.

## Compatibility consequence and limits

The replacement may model the four service pages as separate views over one optional customer-service manager. A successful `ConnectPlayerResponse` should update that manager's local connected state; it does not prove ticket creation or knowledge-base content is enabled. Preserve the existing wire format and status handling if this optional service is implemented. This new shell-to-request link does not add a new opcode or alter the 345-row server-source packet matrix.

Remaining: exact UI action literals for Bug/Harassment; interpretation of all response status values; knowledge-base/known-issues child data and failure UX; live behavior not tested. No whole-client percentage change.
