# Toolbar Action Model

## Scope

This block maps the original toolbar's ownership, persistence boundary, invalidation events, input binding and execution route. Pixel layout and widget rendering remain UI-content concerns.

## Findings

`SwgCuiToolbar` is a `CuiMediator` with dedicated subscriptions to:

- `PlayerObject::CommandAdded` and `CommandRemoved`;
- `CreatureObject::PlayerSetup`;
- `Game::SceneChanged`;
- `ClientMacroManager::Changed` and `Reset`;
- input-map/keybinding changes.

This proves toolbar entries are references into live command, macro, object and drag/action systems rather than copied executable command logic. Replicated `PLAY:9` ability changes and macro mutations invalidate/rebuild presentation, while scene change and player setup rebind object/player-dependent entries.

### Persistence schema

`toolbar_save_VA_00F68900` stores a user/settings record with these stable keys:

- `numPanes`;
- per-pane `numItems` keys formatted as `%s_%d`;
- per-entry keys formatted as `%s_%d_%d` for `itemType`, `itemCmd`, `itemStr`, and `itemId`.

All panes must have a consistent item count; mismatches produce diagnostics and terminate the inconsistent portion of the save/load. `toolbar_load_VA_00F690B0` reads the pane/item dimensions, reconstructs each slot, validates the persisted drag type, and for object entries resolves the persisted `NetworkId`. Missing/invalid objects do not become raw pointers. The serialized representation is therefore portable reference data, not a dump of the 32-bit slot object.

When no usable saved layout exists, the executable seeds commands including follow/stopFollow, stance changes, burstRun, social actions and mood commands. Exact defaults are presentation policy and may be retained through original settings/content rather than hard-coded into the renderer.

### Slot execution and input mapping

`toolbar_refresh_bindings_VA_00F696D0` walks toolbar action objects, constructs logical action names `CMD_uiToolbarSlot%02d`, looks them up in the active input map, and publishes the resulting display binding. Missing input maps or binding strings are diagnostic, not fatal.

`toolbar_perform_action_VA_00F67990` bounds-checks the current pane/slot, finds the retained slot item, flashes the UI state, then invokes the generic draggable/action object's execution interface. Empty/separator slots have no executable object. Execution failure produces a toolbar warning and failure feedback. This deliberately converges command, macro, social/mood and object-use slots behind one semantic action interface.

The adjacent context actions confirm the supported presentation operations: describe, bind, insert/remove separator, perform action, item info, use item, and remove. These are UI operations over the retained slot reference and do not define new network packets.

## Relationship to command availability

The original command table supplies command metadata and the player's replicated `PLAY:9` ability list determines current ownership/availability. The toolbar listens for both initial player setup and incremental command additions/removals, so a slot may persist while temporarily unresolved or unusable. Invocation ultimately enters the registered local action or ordinary command-queue path; authoritative gameplay commands remain server-validated through controller `0x116`.

## Replacement requirements

- Persist semantic slot references (`kind`, command/string, object ID) and pane/slot indices, never native addresses.
- Re-resolve all entries after player setup, scene transition, macro changes and command-list changes.
- Keep toolbar input actions logical and remappable; do not bind renderer key codes directly to gameplay commands.
- Validate pane/slot bounds and drag kinds before invocation.
- Preserve original U64 `NetworkId` values in persisted object references on x64.
- Treat toolbar state as local preference data, not server authority.

## Confidence and bounded unknowns

- **Confirmed:** subscriptions, stable settings keys and index formatting, pane/item consistency checks, object-ID resolution, input action naming, generic execution boundary and failure behavior.
- **Strongly inferred:** `0x00F690B0` is the complete load counterpart; it is not present as a recovered function object but its prologue, entire control range and referenced diagnostics were verified in disassembly.
- **Unknown/non-blocking:** original numeric labels for every drag-type enum and the exact widget-effector timings.

## Evidence

- `C:\Stardust-DEV\Stardust.exe`: `SwgCuiToolbar` RTTI/transceivers and routines `0x00F67990`, `0x00F68900`, `0x00F690B0`, `0x00F696D0`, `0x00F69890`.
- Existing `PLAY:9` ability-list schema and ObjectController `0x116` command path documentation.

