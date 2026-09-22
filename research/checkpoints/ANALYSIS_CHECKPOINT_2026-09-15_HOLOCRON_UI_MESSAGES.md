# Stardust.exe Analysis Checkpoint: Holocron UI Messages

Date: 2026-09-15  
Target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Result

This block closes `OpenHolocronToPageMessage` (`0x7CB65021`) and `CloseHolocronMessage` (`0xC0938A9D`). Both are explicitly registered by the UI message listener at `0x00E40960` and dispatched by listener callback `0x00E40E40`. Open parses an ASCII page identifier and forwards its Unicode conversion into the holocron presentation path. Close directly requests deactivation of mediator type `WS_Holocron`.

## Wire contracts

| Message | Application payload |
|---|---|
| open | ASCII16 page identifier followed by fixed U16 zero |
| close | none |

The open parser at `0x013F34A0` registers an eight-byte archive field object; accessor `0x013F35C0` returns the string-bearing member at object offset `+0x3C`. Listener code converts it to Unicode before forwarding it. The fixed trailing U16 is retained in the registered field envelope but is not consulted by the semantic branch.

## Client paths

```text
OpenHolocronToPageMessage descriptor 0x0160981C
  -> UI listener 0x00E40E40 branch 0x00E4173A
  -> parser 0x013F34A0
  -> page accessor 0x013F35C0
  -> ASCII-to-Unicode 0x00489300
  -> holocron presentation sink 0x008CC460

CloseHolocronMessage descriptor 0x016097FC
  -> UI listener 0x00E40E40 branch 0x00E417FD
  -> mediator deactivate 0x00884C30(WS_Holocron)
```

Close needs no packet object construction because it has no payload. Open's exact downstream sink type remains unnamed, but message semantics, page extraction and presentation forwarding are direct.

## Server use

`CreatureObjectImplementation::sendOpenHolocronToPageMessage(page)` is an active generic send helper exposed to Lua as `sendOpenHolocronToPageMessage`. No current constructor call for close was found. The replacement must support both formats, but close should not be treated as a required current login/gameplay event.

## Replacement requirements

- Register both opcodes in the main UI-message dispatcher.
- Decode the page as the protocol's ASCII string, preserve the fixed trailing U16, then convert to the UI text/string domain.
- Route UI activation/deactivation onto the UI thread rather than mutating UI state in the network callback.
- Treat page identifiers as data keys, not filesystem paths or executable actions.
- Unknown page identifiers should fail safely while preserving the ability to close the mediator.
- No host pointer, native string object or x64 layout participates in either wire format.

## Confidence

- **Confirmed:** names, RTTI/descriptors, listener registration/branches, open field/accessor, Unicode conversion, close mediator identifier and deactivate call, server schemas and open helper.
- **Strongly inferred:** `0x008CC460` is the page presentation/action sink used to open or navigate the holocron.
- **Unknown:** internal type of that sink, supported page-key catalogue and historical producer of close.

## Progress

Two reachable packets move to documented: **250 documented / 95 reachable gaps / 29 literal-opcode gaps**. Weighted whole-client coverage remains **78%**.
