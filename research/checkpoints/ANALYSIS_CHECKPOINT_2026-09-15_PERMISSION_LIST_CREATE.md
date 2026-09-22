# Stardust.exe Analysis Checkpoint: Permission List Creation

Date: 2026-09-15  
Target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Result

This block closes the active `PermissionListCreateMessage` (`0x52F364B8`) path from Stardust's structure/ship permission producers through the client parser and into the `SwgCuiPermissionList` mediator.

## Wire contract

The server writes operand count four and the following payload:

1. U32 primary-name count;
2. repeated Unicode names;
3. U32 secondary-name count;
4. repeated Unicode names;
5. Unicode list name (`ADMIN`, `ENTRY`, etc.).

The current Stardust producer always writes a zero secondary count. Both `StructurePermissionList::sendTo` and `ShipPermissionList::sendTo` actively construct the packet. Player object IDs are resolved to names; guild IDs become `guild:<abbreviation>` entries. Invalid IDs are removed from the server-side permission set.

## Client path

- descriptor: `0x015FE944`;
- broad game-UI listener: `0x00E40E40`, branch `0x00E40F7B`;
- archive parser: `0x0119B130`;
- UI route: `0x00E420D0`;
- mediator factory: `0x00FAC7A0`, using page `/PDA.PermissionList`;
- message application: `0x00FAC810`.

The codec exposes its primary vector, secondary vector and list-name Unicode string through accessors `0x0119B470`, `0x0119B480` and `0x0119B490`. The application routine clears and repopulates both list widgets, copies the list name into mediator state, localizes/formats it through the UI string manager and updates the title widget. The listener creates and activates the mediator if it is not already present.

## Replacement requirements

- Preserve both counted Unicode arrays even though Stardust currently emits an empty second array.
- Bound both counts and every Unicode length before allocation.
- Treat list names as protocol identifiers, not pointers or host-sized values.
- Resolve permission entries on the server as today; the client consumes display strings only.
- Deliver decoded immutable data to a UI-facing permission-list model. A modern UI need not reproduce the original mediator implementation or `/PDA.PermissionList` layout, but must preserve behavior.

## Confidence

- **Confirmed:** opcode, complete codec shape, listener/parser route, both list iterations, list-name propagation, mediator creation and active server producers.
- **Confirmed destination:** the primary vector populates the visible member list; the secondary vector populates the add-name combo's data source. It is therefore an optional suggestion/autocomplete vector (Strong semantic label). Current Stardust sends it empty, leaving manual entry available.

## Progress

One reachable packet moves to documented: **265 documented / 80 reachable gaps / 17 literal-opcode gaps**. Weighted whole-client coverage remains conservatively **78%**.
