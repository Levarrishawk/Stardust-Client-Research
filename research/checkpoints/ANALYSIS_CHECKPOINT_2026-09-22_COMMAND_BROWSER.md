# Command and Macro Browser

Date: 2026-09-22

## Result

`SwgCuiCommandBrowser` is the catalog/editor/drag-source layer for replicated
player commands and client-local macros. It adds no network message. Executing
a command or macro delegates to the already-mapped command queue/macro manager;
the browser itself owns presentation and macro CRUD only.

## Client evidence

| Address | Provisional name | Role | Confidence |
|---|---|---|---|
| `0x00CD9080` | `swg_cui_command_browser_construct_VA_00CD9080` | binds command and macro volumes, tabs, name/description text, sample template and new/edit/delete controls | Confirmed |
| `0x00CD9470` | `swg_cui_command_browser_activate_VA_00CD9470` | subscribes to player-command and macro-manager events and rebuilds catalogs | Strongly inferred |
| `0x00CD9850` | `swg_cui_command_browser_drag_handler_VA_00CD9850` | resolves a dragged command/macro item and emits its transferable action payload | Confirmed |
| `0x00CD9A00` | `swg_cui_command_browser_selection_changed_VA_00CD9A00` | refreshes selected name/description and edit/delete state | Strongly inferred |
| `0x00CD9C30` | `swg_cui_command_browser_button_handler_VA_00CD9C30` | opens new/edit macro UI, serializes selected macro fields for editing, or deletes selected macro | Confirmed |

RTTI exposes direct subscriptions to:

- `PlayerObject::Messages::CommandsChanged`;
- individual `CommandAdded` and `CommandRemoved` events;
- `ClientMacroManager::Messages::Added`, `Removed` and `Modified`.

## Data ownership

The command half is a projection of the replicated player command set resolved
through original command-table/localization/icon assets. The macro half is
owned by `ClientMacroManager` and persisted locally. Both become tagged action
payloads that the toolbar and drag/drop system can retain.

```text
Player command replication -> command catalog -> drag tagged command action
ClientMacroManager          -> macro catalog   -> drag tagged macro action
                                               -> toolbar/action execution
```

Macro create/edit/delete is not a server-authoritative gameplay transaction.
Commands invoked later still flow through the normal command dispatcher and
server validation.

## Replacement-client requirement

Keep three layers separate:

1. replicated command entitlement/availability;
2. original-asset command metadata and localization;
3. client-local macro definitions and persistence.

Expose both command and macro entries as stable typed action descriptors rather
than UI widget pointers. Do not let macro text bypass the normal in-game
command interpreter, and never route it to an operating-system shell.

## Residual uncertainty

Exact historical drag-property keys and per-category sort ordering remain
fidelity details. Catalog ownership, update events, macro CRUD and downstream
action semantics are closed.
