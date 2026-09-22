# Examine and inventory-info runtime

Date: 2026-09-22

## Result

`SwgCuiExamine` is a small lifecycle shell around the reusable
`SwgCuiInventoryInfo` presentation component. It retains the selected object,
forwards it to that component, suppresses/restores ordinary world input while
active, and closes when its content is no longer valid. It does not own a unique
examine packet.

## Concrete client path

| Address | Provisional name | Role |
|---|---|---|
| `0x00D438E0` | `swg_cui_examine_construct_VA_00D438E0` | Constructs `SwgCuiExamine`, resolves `info`, and constructs embedded `SwgCuiInventoryInfo`. |
| `0x00D43AB0` | `swg_cui_examine_destroy_VA_00D43AB0` | Releases retained target, child info mediator and callback token. |
| `0x00D43B60` | `swg_cui_examine_activate_VA_00D43B60` | Activates modal/input state, re-applies current target to child info, and enables updates. |
| `0x00D43BA0` | `swg_cui_examine_deactivate_VA_00D43BA0` | Closes mediator state and restores ordinary input/update behavior. |
| `0x00D43BD0` | `swg_cui_examine_set_target_VA_00D43BD0` | Reference-counts the target and forwards it to inventory-info with refresh enabled. |
| `0x00D43C30` | `swg_cui_examine_update_VA_00D43C30` | Runs base update and closes when the embedded content reports invalid/finished. |
| `0x00E72390` | `swg_cui_inventory_info_construct_VA_00E72390` | Resolves 3-D viewer, content page, label, attributes text and description text; configures viewer behavior. |
| `0x00E72C60` | `swg_cui_inventory_info_set_target_VA_00E72C60` | Normalizes target/client object, optionally requests attributes, swaps retained target, updates 3-D viewer and rebuilds text. |
| `0x00E730B0` | `swg_cui_inventory_info_set_creature_VA_00E730B0` | Specialized creature identity/title/species/guild composition and viewer update; requests guild data when applicable. |

The target setter uses the object's client-facing representative, initiates an
attribute-manager request when the selected object changes, updates the
`CuiWidget3dObjectListViewer`, then composes label, description and attribute
text from the object and cache. Waypoints receive a specialized formatting
branch. Creature objects are routed to the separate creature formatter.

## Protocol and server boundary

There is no `ExamineMessage`. Ordinary item inspection depends on the already
closed object-attribute path:

`getAttributesBatch` queued command -> Stardust object resolution and
`fillAttributeList(...)` -> `AttributeListMessage` (`0xF3F12F2A`) -> client
attribute cache -> inventory-info refresh.

The server owns attribute visibility and value generation through each object's
`fillAttributeList` implementation. The client owns localization, ordering,
description assembly and 3-D presentation. Creature inspection additionally
uses replicated creature/player state and the existing guild request/response
family; it is not an item-attribute schema variant.

## Replacement-client contract

Build a reusable inspection view-model with an optional 3-D preview, localized
name/description and attribute rows. Let an examine window, inventory tooltip,
auction details and crafting views consume it with context-specific policy. Keep
the U64 object ID and attribute wire schema fixed while using native 64-bit
handles internally.

## Confidence

- Examine lifecycle and delegation: **Confirmed**.
- Non-creature attribute-cache dependency: **Confirmed**.
- Creature specialized path and guild request: **Confirmed**.
- Exact historical ordering/style of every formatted row: **Tentatively bounded;
  presentation fidelity remains**.
