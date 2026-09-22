# Activity Polarity and Permission Secondary-List Closure

Target: `C:\Stardust-DEV\Stardust.exe` plus current Stardust server source/assets

This block resolves two bounded protocol semantics that were previously preserved without labels.

## ClientInactivityMessage polarity

The one-byte `ClientInactivityMessage` value has the following confirmed polarity:

| Value | Meaning | Evidence |
|---:|---|---|
| `0` | input active / activity resumed | every recognized input event calls state setter with zero and resets elapsed inactivity time |
| `1` | inactive timeout reached | frame update accumulates elapsed time only while state is zero; reaching the configured threshold calls the setter with one |

`io_win_activity_update_VA_00AB5310` is the decisive path. It accumulates delta time in global elapsed state, compares against the configured timeout, and invokes `io_win_set_inactive_state_VA_00AB5100(1)`. When `0x00AB50C0` classifies an event as activity, the same loop invokes the setter with zero and resets the timer to `0.0`.

`input_activity_get_inactive_VA_00AB5810` returns that state. On scene change, `0x00425E80` passes the current Boolean to `client_inactivity_message_send_VA_0055E110`. The installed timer callback also uses the same sender. Stardust server currently parses and logs the byte without gameplay mutation.

Replacement rule: send reliable U8 `1` on inactive transition and U8 `0` on resumed input transition. Do not use it as a transport heartbeat.

## PermissionListCreateMessage second vector

The two Unicode vectors have distinct UI destinations:

1. The primary vector populates the visible permission-member list widget (`userList -> list.lstList`).
2. The secondary vector is copied into mediator-owned state and then populates the data source of the add-name combo/input widget (`addInput -> addPage.cmbInput`).

`SwgCuiPermissionList` constructor `0x00FABC70` binds the visible list and the `addInput` combo. It obtains the combo's data source through `0x011503A0`. Application routine `0x00FAC810` clears/populates the visible list from accessor `0x0119B470`, then clears/populates the combo data source from accessor `0x0119B480`. The list-name/title string comes from `0x0119B490`.

The secondary vector is therefore an **add-entry suggestion/autocomplete list**, not a second permission-membership set. Current Stardust deliberately emits it empty, so text entry remains manual.

Replacement rule: decode both counted Unicode vectors; show the primary vector as current members and expose the secondary vector as optional add-name suggestions. An empty suggestion vector is ordinary and must not hide or disable manual entry.

## ConGenericMessage trailing U32 remains reserved

Local correlation found no producer or consumer assigning semantics to `ConGenericMessage`'s trailing U32. Stardust writes zero and the client parser retains but never reads it before routing the ASCII message to chat. It remains a confirmed required wire field with **reserved/ignored** behavior, not a material implementation unknown. The replacement should parse and preserve it diagnostically while treating zero as the current profile.

## Confidence

- Inactivity polarity, transition points, timer reset, and message sender: **Confirmed by code**.
- Permission primary visible-list and secondary combo-data-source destinations: **Confirmed by code and UI asset bindings**.
- “suggestion/autocomplete” is **Strongly inferred** from the add-input combo destination; no historical source variable name survives.
- ConGeneric U32 reserved/ignored behavior: **Confirmed for this client/server pair**; historical name unknown and unnecessary.

