# Craft draft-selection mediator to server command

Date: 2026-09-23. Target: `C:/Stardust-DEV/Stardust.exe`.

## Result

The draft-selection UI-to-command boundary is now directly evidenced, separate
from the already mapped crafting ObjectController messages. `SwgCuiCraftDraft`
populates its tree from `CuiCraftManager`'s draft list and subscribes to draft
schematics, draft slots, and session-ended events. Its Next action obtains the
selected index and calls manager routine `0x0098BDF0`. That routine validates
the index against the twelve-byte draft records, converts the **zero-based
index** to decimal text, and queues the command whose name is literally
`selectDraftSchematic`. Current Stardust server source registers that command,
tokenizes one integer, and invokes `CraftingSession::selectDraftSchematic`.

## Evidence chain

| Boundary | Evidence | Confidence |
|---|---|---|
| UI construction | `0x00D1F980` identifies `SwgCuiCraftDraft`, Back/Next buttons, treeView, checkShowAll, tabs | Confirmed by executable code |
| Activation/events | `0x00D1FEA0` subscribes to `DraftSlotsReceived`, `DraftSchematicsReceived`, `SessionEnded`; `0x00D1FF20` removes those listeners | Confirmed by executable code/RTTI |
| Next/selection | `0x00D200E0` calls `0x00D20B20` on Next; tree event `0x00D20120` also calls it for an eligible selected row | Confirmed by executable code; exact tree-event gesture meaning still tentative |
| Manager bridge | `0x00D20B20` obtains selected index and calls `0x0098BDF0`; `0x0098BDF0` range-checks against `(end-start)/12`, decimal-formats index, calls command queue `0x0046E5F0` | Confirmed by executable code |
| Command identity | Initializer at `0x009889F0` hashes ASCII literal `selectDraftSchematic` at `0x018A2CF4` into `0x01932B0C`, which `0x0098BDF0` passes to command queue | Confirmed by executable bytes/disassembly/xrefs |
| Server action | `CommandConfigManager3.cpp` registers `SelectDraftSchematicCommand`; callback parses integer index, requires active crafting session, aborts trade, calls `session->selectDraftSchematic(index)` | Confirmed by current Stardust source |

The draft screen also rebuilds list/tree state at `0x00D20200` and processes a
selected-row change at `0x00D209D0`. The latter sets a selected item/preview
through `0x00E72C60`; it is **not** the command send path. The malformed
decompiler pseudocode for much of tree construction does not justify a detailed
widget data-layout claim.

## Replacement implication

Keep the allowed schematic list and its index ordering from session message
`0x102` intact. A UI may display the entries differently, but a draft choice
must send the selected list index as the `selectDraftSchematic` command
argument, not a schematic CRC or local object ID. The server's subsequent
manufacture-schematic/prototype creation and replicated updates are separate
state transitions.

## Remaining uncertainty

- Exact `checkShowAll` filtering, tab categorization, visual preview choices,
  and tree-row data encoding are not yet fully reconstructed.
- The tree event's numeric event type `0x0B` is observed, but its user gesture
  name is not asserted without UI framework confirmation.
- Full downstream session success/failure presentation in all crafting
  mediators remains open.

Scratch: `local-private/session-2026-09-23-marathon/block33_craftdraft.txt`,
`xrefs_block33.tsv`, `block33_init_disasm.tsv` and corresponding decompiles.
