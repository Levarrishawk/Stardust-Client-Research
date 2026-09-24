# Analysis checkpoint — harvester SetResource selection to queued command

Question A09 / W10+W13, following the [main extraction panel](ANALYSIS_CHECKPOINT_2026-09-24_RESOURCE_EXTRACTION_MAIN_PANEL.md). This is the previously open `WS_ResourceExtraction_SetResource` child workflow. Target: verified original `Stardust.exe`; Stardust-3 command source is read-only correlation.

`SwgCuiResourceExtraction_SetResource` RTTI maps to main mediator table `0x015F2108` and callback table `0x015F2084`. Constructor `0x00D82290` binds `SetResourceTable`, `buttonOk`, and `buttonCancel`, creates its table model, and connects it to the UI table. Activation `0x00D82730` refreshes the candidate list through `0x00D828D0`, verifies the selected object as an `InstallationObject`, and establishes the mediator state. The list builder reads the selected installation's `ClientInstallationSynchronizedUi` and the resource manager's pool snapshot; deactivation `0x00D827C0` clears the list/model and restores UI state. Exact sorting and density-display policy are not assigned here.

The button handler `0x00D82810` reads the selected table row on OK. It checks the index against the 8-byte-per-record local array, copies the selected two-dword resource ID via `0x00AA4920`, checks installation/player prerequisites, and calls `0x0070B2B0`. That helper formats the selected 64-bit ID with `%I64i` through `0x00AA4950`, chooses the exact command name `harvesterSelectResource`, and enqueues it with the installation target through `0x0046E5F0` / ordinary command queue `0x0046D7A0`. OK hides the panel after the attempt; an invalid selection does not send. Cancel hides without sending. The selected ID is an **argument string within the legacy command envelope**, not a new binary packet field. A 64-bit replacement must preserve the decimal command argument and wire-level U64 target ID separately.

Current Stardust `HarvesterSelectResourceCommand.h` parses `Long::valueOf(arguments.toString())`, requires a valid player and installation, then checks admin list and 20-metre range before `changeActiveResourceID(spawnId)` under the installation lock. The server sends a “too far” system message in its combined failed-rights/range branch; that text does not prove distance was the only failing condition. Success is reflected by authoritative installation state updates, not an optimistic local SetResource mutation.

| UI event | Client path | Server path/result | Confidence |
|---|---|---|---|
| Valid row + OK | `0x00D82810` -> `0x00AA4920` -> `0x0070B2B0` -> `0x00AA4950` -> ObjController command queue inner `0x116` | `HarvesterSelectResourceCommand` parses decimal resource ID and changes active resource after checks | Confirmed by code on both sides |
| No valid selected row | `0x00D82810` | No command enqueued; panel closes | Confirmed by code |
| Cancel | `0x00D82810` | No command enqueued; panel closes | Confirmed by code |

Remaining: table sorting/filtering, dynamic pool-refresh behavior while the child panel is open, and precise error presentation after server rejection. This closes the chosen submit contract, not all ResourceExtraction mediators. No whole-client percentage change.
