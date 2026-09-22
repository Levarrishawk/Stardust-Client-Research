# Droid Command Programming Workflow

Date: 2026-09-22

## Result

`SwgCuiDroidCommand` and current Stardust's object-controller subtype `0x435`
form one complete authoritative transaction for loading and unloading droid
program modules. The UI maintains a proposed left/right assignment, previews
storage usage, confirms changes, then sends one batched request. Replicated
container changes reconcile the display afterward.

## Client boundary

| Address | Provisional name | Role | Confidence |
|---|---|---|---|
| `0x00DFA6D0` | `swg_cui_droid_command_construct_VA_00DFA6D0` | binds left/right lists, load/unload/OK/cancel controls, storage-volume widgets and container-change subscriptions | Confirmed |
| `0x00DFACE0` | `swg_cui_droid_command_activate_VA_00DFACE0` | activates mediator, builds available/installed collections and subscribes to added/removed container events | Strongly inferred |
| `0x00DFB3D0` | `swg_cui_droid_command_button_handler_VA_00DFB3D0` | moves selected entries between proposed sets, cancels, or begins OK confirmation | Confirmed |
| `0x00DFB720` | `swg_cui_droid_command_update_VA_00DFB720` | polls pending object resolution and updates capacity/progress presentation | Strongly inferred |
| `0x00DFCED0` | `swg_cui_droid_command_confirm_send_VA_00DFCED0` | on accepted confirmation builds the request, sends inner `0x435`, resets staged state and closes | Confirmed |

RTTI proves the mediator subscribes to `ClientObject::Messages::AddedToContainer`
and `RemovedFromContainer`. Thus the lists are object-model projections, not a
private server response format.

## Wire schema

Current Stardust's registered `DroidCommandProgrammingCallback` parses the
client request after the common controller scalar as:

```text
ObjController inner subtype 0x435 (C -> Z)
    U32 commandNameCount
    commandNameCount * ASCII command ability name
    U32 inventoryModuleAddCount
    inventoryModuleAddCount * U64 programming-chip object ID
    U32 installedModuleRemoveCount
    installedModuleRemoveCount * U64 installed-module object ID
    U64 pet control-device object ID
```

The executable's confirmation path constructs exactly three staged collections
before invoking its controller-send helper with literal subtype `0x435`; the
current server parser fixes their byte widths and order.

## Server action

The server validates the session player, ghost, control device and droid
datapad. It then:

1. removes requested installed modules from the datapad and database;
2. validates named abilities and available data capacity, then creates command
   intangible objects in the datapad;
3. validates inventory programming chips, clones their program into the
   datapad, then destroys the consumed chip;
4. relies on ordinary object/container replication for client reconciliation.

The effective module template is
`object/intangible/data_item/droid_command.iff`; command identity is stored in
the intangible item identifier and data size is replicated by the intangible
object model.

## Replacement-client requirement

The replacement must preserve all three independent collections and their
ordering, retain U64 object IDs on the wire, and treat capacity as
server-authoritative. UI optimism must be reconciled only through normal
container/object replication. The confirmation dialog is presentation policy,
not part of the protocol.

## Residual uncertainty

Several anonymous list-row conversion helpers retain only naming uncertainty.
The transaction, byte schema, authority, object lifecycle and reconciliation
route are closed.
