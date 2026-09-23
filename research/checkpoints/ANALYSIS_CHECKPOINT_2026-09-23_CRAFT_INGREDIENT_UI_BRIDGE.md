# Craft ingredient-selection dialog to `0x107`

Date: 2026-09-23. Target: `C:/Stardust-DEV/Stardust.exe`; current server source read-only.

## Result

The assembly UI's ingredient chooser now has a direct client sender route.
`SwgCuiCraftAssembly` widget dispatcher `0x00D00520` resolves a selected
`ClientObject` and slot. The slot chooser at `0x00E9D770`, after finding
compatible candidate indices, calls `0x00E9DBA0`; for a nonnegative choice,
that obtains an object from the selected slot and calls manager
`0x0098BFC0`. The manager refuses a slot already pending, validates an active
crafting state, prepares transfer/status tracking, and calls `0x00701AC0`.
That function allocates the typed add-ingredient record and enqueues
ObjectController inner `0x107` (`0x83` flags). The existing byte-level codec
checkpoint establishes its fixed-width payload.

## Evidence chain

| Step | Evidence | Confidence |
|---|---|---|
| Assembly interaction/slot choice | `0x00D00520`, `0x00E9D770`, `0x00E9DBA0` | Confirmed by executable code, with precise widget gesture labels tentative |
| Manager pending-slot gate | `0x0098BFC0`, slot pending array `DAT_01932860`, warning text `CuiCraftManager::transferToSlot` | Confirmed by executable code/text |
| Controller sender | `0x00701AC0` constructs record and calls `0x00B3D250(0x107,...,0x83)` | Confirmed by executable code |
| Server receive | `CraftingAddIngredientCallback.h`: reads object ID, slot, one extra `int`, and byte counter; resolves tangible, aborts trade, calls `CraftingSession::addIngredient(tano,slot,counter)` | Confirmed by current server source |

The extra 32-bit field is present on the wire and must be encoded for
compatibility, but current Stardust source **discards** it. Calling it an
enforced quantity would be an overstatement. The actual amount/resource
behavior is determined inside the session and associated slot data.

## Open

- The decompiler has poor argument recovery for `0x0098BFC0` and the detailed
  slot-choice route. The call chain and controller type are solid; exact
  positional mapping of its C++ call-site arguments should be verified from
  disassembly before a replacement sender is written.
- Alternative UI actions that can call the same manager route remain to be
  enumerated.

Scratch: `local-private/session-2026-09-23-marathon/block35_ingredient.txt`,
`block35_transfer_disasm.tsv`, and matching decompiles.
