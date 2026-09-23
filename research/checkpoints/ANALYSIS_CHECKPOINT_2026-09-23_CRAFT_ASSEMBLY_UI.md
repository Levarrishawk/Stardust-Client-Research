# Craft assembly mediator and stage command

Date: 2026-09-23. Target: `C:/Stardust-DEV/Stardust.exe`; current Stardust server source read-only.

## Result

`SwgCuiCraftAssembly` is a live event-driven mediator, not the owner of the
crafting session. Its callback `0x00D00520` handles widget/object-list events,
including dragged/selected objects and slot destinations. A slot-removal path
reaches `CuiCraftManager::transferToSlot` candidate `0x0098C190`, which builds
and sends ObjectController inner message `0x108` and marks the slot pending.
The manager's generic-response handler, mapped previously, clears pending
state on server acknowledgement.

The mediator's Next button (`0x00D00490`) invokes manager function
`0x0098C350`. This hashes/queues `nextCraftingStage` with a decimal-formatted
single-byte client counter. The current server's
`NextCraftingStageCommand::doQueueCommand` parses the argument into
`clientCounter` and calls `CraftingSession::nextCraftingStage(clientCounter)`.
Server state, not the command argument, selects `initialAssembly`,
`finishAssembly`, or later stage-finalization paths.

## Evidence and confidence

| Observation | Evidence | Confidence |
|---|---|---|
| Assembly mediator identity and typed listeners | RTTI vtables `0x015F1004/0x015F1088`, typed transceivers at `0x015F10F4..0x015F115C`, activation `0x00CFF8F0`, deactivation `0x00D00240` | Confirmed by executable code/RTTI |
| Widget event dispatcher and object-list drag/drop | `0x00D00520` switches on widget event IDs and tests `CuiWidget3dObjectListViewer`/`ClientObject` | Confirmed by executable code; exact gesture names remain tentative |
| Slot removal/controller `0x108` | `0x00D00520 -> 0x0098C190 -> 0x00AF5340`, then controller enqueue `0x108`; related codec `0x00AF5430` was mapped earlier | Confirmed by executable code |
| Next stage command | `0x00D00490 -> 0x0098C350 -> 0x0046E5F0`; initializer `0x00988A20` hashes literal `nextCraftingStage` at `0x018A2D0C` to `0x01932B08` | Confirmed by executable bytes/code |
| Server handling | `CommandConfigManager3.cpp`, `NextCraftingStageCommand.h`, `CraftingSessionImplementation.cpp:nextCraftingStage` | Confirmed by current server source |

## Design implication

The proof of compatibility is the command name, argument/counter, and server
state transition, not reproduction of the 32-bit widget tree. Build a session
model whose stage and pending slot actions are driven by controller responses
and baselines/deltas; let UI observe that model. Keep the command argument as
the original decimal client counter on the wire.

## Open

- Exact per-widget meanings of all `0x00D00520` event branches and the
  amount/choice dialog flow remain incomplete.
- The `0x107` add-ingredient call from this mediator is not yet traced to a
  specific event branch; the codec/server callback are known separately.
- The slot `0x108` method's provisional name follows the executable warning
  text, but its user-facing semantic is removal/emptying. Avoid assuming
  the name alone reveals source and destination ownership.

Scratch: `local-private/session-2026-09-23-marathon/block34_assembly.txt`,
`xrefs_block34.tsv`, `block34_init_disasm.tsv` and matching decompiles.
