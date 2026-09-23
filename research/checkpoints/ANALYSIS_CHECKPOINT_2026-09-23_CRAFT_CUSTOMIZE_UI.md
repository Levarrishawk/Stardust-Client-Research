# Craft customization mediator and `0x15A` sender

Date: 2026-09-23. Target: `C:/Stardust-DEV/Stardust.exe`; current server source read-only.

## Result

`SwgCuiCraftCustomize` mediates a bounded schematic-count choice and final
customization submission. Its button handler `0x00D17640` accepts a numeric
value only in `1..1000`, writes the selection to its UI state, and invokes
manager `0x0098C570`. Activation `0x00D17160` also seeds/configures a `1000`
upper bound. The manager branches on current crafting stage and selected
output type; in stage `4`, it builds the typed customization record and sends
ObjectController inner `0x15A`. It collects custom variable index/value pairs
from the prototype and fills the schematic count from the current schematic
object. Stage `5` instead queues a completion-related command; that branch is
not the `0x15A` packet.

Current Stardust server's `CraftingCustomizationCallback` parses the Unicode
name, byte template choice, 32-bit schematic count, then a **byte count of
customization pairs** followed by 32-bit index/value pairs. It calls
`CraftingSession::customization(...)`. This byte is not a generic request
correlation counter; the older codec checkpoint correctly called it
`pairCount`, and that distinction should be preserved.

## Evidence and confidence

| Observation | Evidence | Confidence |
|---|---|---|
| Mediator type and events | RTTI/vtables `0x015F116C/0x015F11F0`, `SessionEnded`/`Complete` transceivers, activation `0x00D17160` | Confirmed by executable code |
| Count bound and button path | `0x00D17640` tests `<1` or `>1000`, then calls `0x0098C570`; activation initializes 1000 bound | Confirmed by executable code |
| Stage-4 sender | `0x0098C570` checks manager stage `4`, builds record, calls `0x00B3D250(0x15A,...,0x83)` | Confirmed by executable code |
| Server parse | `CraftingCustomizationCallback.h`, `CraftingSessionImplementation::customization` | Confirmed by current server source |

## Open

The exact UI naming/presentation of every output choice and the stage-5
completion command branch still require a dedicated block. The decompiler's
argument recovery in `0x0098C570` is unreliable; do not infer source-level
parameter order from pseudocode alone. The packet field order/widths are
independently fixed by the codec and server parser.

Scratch: `local-private/session-2026-09-23-marathon/block37_customize.txt`
and matching decompiles.
