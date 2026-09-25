# Controls Menu and key-rebinding workflow

Date: 2026-09-25  
Target: `C:\Stardust-DEV\Stardust.exe`

## Reachability and effective asset

`SwgCuiControlsMenu` is registered with `CuiMediatorFactory`; factory slot
`0x00BCC670` allocates the mediator and calls constructor `0x00CEBB50`.
Activation `0x00CEC180` copies the active input scheme into a private working
scheme, enables menu input ownership, populates presets/categories/check boxes
and rebuilds the binding table. This is a live local configuration editor, not
a server-authoritative UI.

The configured overlay winner for `ui/ui_controls_menu.inc` is priority-53
`stardust_3_01.tre`. Its payload is nonempty (**10,138 bytes**) and contains the
owned rebind `EntryPage`, OK/Cancel widgets, bind-result text and the
`bind_confirm_overwrite1` / `bind_confirm_overwrite2` conflict tokens. The
executable constructor additionally binds the main menu buttons, preset combo,
binding table, tabs and four preference check boxes from its supplied page.

## Main-menu behavior

Button callback `0x00CEC2A0` implements:

| Control | Effect |
|---|---|
| Cancel | closes without copying the private working scheme into the active scheme |
| OK | commits/saves through `0x00CECE90`; closes only on success |
| Apply | commits/saves but leaves the menu active |
| Delete preset | rejects the protected preset, otherwise deletes and refreshes the list |
| Reset defaults | resolves the selected preset and reloads it; displays `Unable to load defaults` on failure |
| Rebind | lazily creates `CuiControlsMenuBindEntry`, hides the parent page and supplies the selected command/binding |

Commit helper `0x00CECE90` deep-copies the working scheme into the active input
map (`0x00AAEB90`) and writes the selected configuration through
`0x00AAD7C0`. A failed write displays `Unable to save configuration`, returns
false and prevents OK from closing the editor. On success it refreshes the
runtime command/input state and requests machine-option persistence.

Preset population `0x00CECFB0` enumerates installed schemes, excludes the
reserved scheme and localizes labels. Category population `0x00CED1B0` builds
the tab set from input-map categories. Preset selection immediately loads the
selected scheme into the working state; table selection updates rebind control
availability.

## Scene-scoped preferences

The four check boxes are not serialized into the cloned binding list:

| Check box | Setter | Contract |
|---|---:|---|
| mouse mode | `0x00910A10` | writes the current ground/space scene slot and immediately updates active UI/input state where installed |
| modal chat | `0x00910A70` via `0x00CED470` | confirmation-gated toggle; scene-scoped and forced on when policy requires it |
| chase camera | `0x006D7C40` | updates the live chase-camera option and its associated option notification |
| turn/strafe behavior | `0x00910A60` | updates the stored preference byte |

Modal-chat close callback `0x00CED470` changes the preference only for accepted
message-box result values, then resynchronizes the checkbox from the effective
getter. Thus Cancel discards uncommitted key-map edits, but preference check-box
changes are live option changes and are persisted during normal option saving.

## Rebind child and conflict handling

`CuiControlsMenuBindEntry` constructor `0x013DB900` binds OK, Cancel, prompt and
result widgets and enables input capture. `0x013DBDD0` stores the selected
command and input scheme, clears the candidate binding and formats the command
prompt. Its activation path centers input capture, saves/restores the previous
input mode and installs a capture window.

OK enters validator/commit helper `0x013DC0B0`:

1. the captured input type must be allowed by the selected command's mask;
2. a modal-chat-sensitive binding may require confirmation;
3. an existing identical binding on another command produces a localized
   overwrite confirmation unless it is the same command;
4. confirmed conflicts remove the old mapping, then the new mapping is added;
5. a `CuiControlsMenuBindEntry::Messages` event refreshes the parent table.

Unsupported candidates produce a message box and leave the capture dialog
open. `0x013DC500` is the message-box continuation: it clears the retained box,
restores disabled UI state, and retries commit only after an accepted result.
Cancel closes without mutating the working scheme. Destruction releases the
retained message box/capture object, disconnects callbacks and restores input
mode.

## Replacement contract

Implement the controls screen as a transaction over an internal input-map
model. Keep legacy file/wire widths separate from native pointer width. The
renderer/UI technology may differ, but the replacement must retain:

- ground/space-scoped preferences;
- command input-type masks;
- protected/default scheme policy;
- explicit conflict confirmation and old-binding removal;
- save failure that leaves the editor open and recoverable;
- cancellation that cannot leak captured-input ownership; and
- change notification so the visible binding table reflects the committed
  working model.

No client/server packet is emitted by this workflow.

## Confidence and residual fixture

| Finding | Confidence | Basis |
|---|---|---|
| factory, activation and private working-copy transaction | Confirmed | factory/vtable entries and `0x00CEC180` |
| button behavior and save-failure retention | Confirmed | `0x00CEC2A0`, `0x00CECE90` |
| scene-scoped preference handling | Confirmed | setters/getters and `0x00CED470` |
| conflict validation, removal and notification | Confirmed | `0x013DC0B0`, `0x013DC500` |
| exact user-visible platform key names | Unknown until runtime/platform fixture | formatting helpers and localization assets were not exhaustively decoded |

A later runtime fixture should deny configuration-file writes and exercise one
reserved/unsupported key plus one overwrite conflict. That verifies presentation
and platform key naming, but it is not an architecture blocker.
