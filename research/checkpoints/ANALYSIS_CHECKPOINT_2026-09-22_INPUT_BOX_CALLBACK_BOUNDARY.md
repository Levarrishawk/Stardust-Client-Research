# Analysis checkpoint: CuiInputBox callback boundary

Date: 2026-09-22  
Target: `C:\Stardust-DEV\Stardust.exe`  
Scope: generic text-entry mediator and the immediately adjacent waypoint-name specialization

## Result

`CuiInputBox` is a local modal text-entry service, not a network-message parser. It owns a question text widget, an editable text widget, affirmative and negative buttons, a completion flag, an opaque callback pointer and an eight-byte caller context. Button activation and mediator cancellation converge on one completion method. That method invokes the installed callback as `(accepted, enteredText, context)` exactly once and then marks the mediator complete.

MSVC RTTI identifies the adjacent derived class as `CuiSetName`. It specializes the callback for waypoint naming: on an accepted result it resolves the context object, verifies `ClientObject` then `ClientWaypointObject`, and forwards the entered text to the already-mapped waypoint rename path. The network consequence therefore belongs to waypoint management; the generic input box introduces no opcode, serialization, or authoritative state of its own.

## Stable provisional functions

| Address | Provisional name | Behavior | Confidence |
|---|---|---|---|
| `0x013BBB90` | `cui_input_box_construct_VA_013BBB90` | constructs mediator; binds question, input, yes and no widgets; allocates callback helper | Confirmed |
| `0x013BBE90` | `cui_input_box_destroy_VA_013BBE90` | releases callback helper and mediator resources | Confirmed |
| `0x013BBF40` | `cui_input_box_on_button_press_VA_013BBF40` | yes -> accepted; no -> rejected; supplies current edit text | Confirmed |
| `0x013BBFB0` | `cui_input_box_cancel_VA_013BBFB0` | rejects on cancellation unless already completed | Confirmed |
| `0x013BC020` | `cui_input_box_set_callback_VA_013BC020` | installs the completion callback pointer | Confirmed |
| `0x013BC030` | `cui_input_box_set_context_VA_013BC030` | copies the eight-byte callback context | Confirmed |
| `0x013BC050` | `cui_input_box_complete_VA_013BC050` | invokes callback and latches completion | Confirmed |
| `0x013BC0C0` | `cui_set_name_construct_VA_013BC0C0` | constructs RTTI-confirmed `CuiSetName` specialization | Confirmed |
| `0x013BC140` | `cui_set_name_complete_VA_013BC140` | chains generic completion and applies an accepted name to a verified client waypoint | Confirmed |
| `0x013BC1B0` | `cui_set_name_open_VA_013BC1B0` | opens/configures the SetName prompt for a selected object | Strong |

## Evidence

- MSVC RTTI identifies both primary vtables as `CuiInputBox`.
- Constructor strings identify `CuiInputBox`, `question`, `buttonYes`, and `buttonNo`; missing required widgets are fatal asset errors.
- Direct control flow at `0x013BBF40`, `0x013BBFB0`, and `0x013BC050` proves the yes/no/cancel convergence and one-shot completion latch.
- The specialization performs RTTI casts from `ClientObject` to `ClientWaypointObject` before calling the known waypoint update path.
- No network-message type, opcode constant, archive reader/writer, or message dispatch registration occurs in this family.

## Replacement implication

Implement one reusable asynchronous text-prompt service returning accepted/rejected plus Unicode text and caller context. Keep domain actions outside the widget. A waypoint rename adapter may consume the result and invoke the protocol layer, but the prompt must not know the wire format. The original UI page still supplies required widget names and should remain consumable for compatibility.

## Remaining uncertainty

- The class name `CuiSetName` is recovered; exact historical member-function names remain provisional.
- Exact visual geometry and localized prompt wording remain asset-authored UI breadth.
