# Inventory container details: table and selection adapter

Date: 2026-09-24. Scope A09/W10/W05. Original `C:\Stardust-DEV\Stardust.exe`, static-only; Ghidra output is reconstructed pseudocode. The previously documented `SwgCuiInventoryContainer` navigation/open-request path remains separate.

`SwgCuiInventoryContainerDetails` constructor `0x00FEB030` binds a UI `table`, installs a table model and icon callback, and retains its original data-source pointer. RTTI exposes two owned transceiver types: parent-container `Messages::ListReset` and `CuiContainerSelectionChanged::Type`. Activation `0x00FEB360` registers those callbacks, refreshes the table and synchronizes active selection; deactivation `0x00FEB430` removes both, releases object/UI observation and clears the selection hooks. The table model is `SwgCuiInventoryContainerDetailsTableModel` (RTTI at `0x018D79D0`), not a network decoder.

`0x00FEB4E0` maps the model's visible row order to backing container indices, then calls `0x00FEB5F0` to reconcile the selected-object vector. `0x00FEBD80` finds the current selected object ID in the model's `(object, id)` rows and applies the corresponding selected row, with a local guard to suppress feedback loops while changing selection. The examined callback leaf `0x00FEBD20` invokes common UI refresh; the other small callback entry at `0x00FEBD40` is a no-op in the reconstructed body. Exact sorting/filtering criteria and the full icon callback are not closed here.

No direct packet or command send appears in these inspected mediator methods. This screen projects provider/container and selection state; opening another container, moving/equipping an item or requesting attributes belongs to other manager/action paths. A replacement should keep stable U64 object identities distinct from row indices and update selection after model reorder/list reset.

Confidence: **Confirmed by executable** for table binding, typed subscription lifecycle and row/selection reconciliation; **Strongly inferred** for its read-only projection role in these methods; **Unknown** complete filter/sort and all inherited drag/drop actions. No new opcode or whole-client percentage change.
