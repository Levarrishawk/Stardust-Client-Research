# Craft summary mediator and return routing

Date: 2026-09-23. Target: `C:/Stardust-DEV/Stardust.exe`.

## Result

`SwgCuiCraftSummary` builds a display table from the current drafting/
manufacture-schematic manager state, not from an independent network packet.
Activation `0x00D36360` requires an active crafting state, obtains the current
schematic/object data, enumerates up to the manager's experiment attribute
count, and populates a UI table via `0x0098DC40` in the production path.
An explicit `client_test_table` branch exists under the client's local test
flag and is not evidence of production server data. The screen subscribes to
`CuiCraftManager::SessionEnded`; deactivation `0x00D36D60` unsubscribes and
clears table/preview resources.

Its return button `0x00D36E80` routes back to `WS_CraftOption` when there are
multiple choices and an eligible option flag, otherwise sets the prototype
mode (`0x0098DB60(1)`) and opens `WS_CraftCustomize`. No network send is
visible in that button handler. This reinforces that rendering the summary
and selecting a final output are separate concerns.

## Evidence/confidence

- RTTI/vtables `0x015F150C/0x015F151C/0x015F15A0`, activation
  `0x00D36360`, deactivation `0x00D36D60`, return callback `0x00D36E80`:
  **confirmed by executable code**.
- Exact semantic labels of each summary column and all experiment-result
  numeric values remain **partially known**. The decompiler output for table
  construction is not treated as original source.

## Replacement implication

The summary view can be rebuilt from the crafting session model and
replicated schematic/prototype attributes; it is not a protocol object to
mirror byte-for-byte. Keep the branch eligibility and output-mode transitions
in the session coordinator, not in a UI panel.

Scratch: `local-private/session-2026-09-23-marathon/block41_summary.txt`
and corresponding decompiles.
