# Analysis checkpoint: resource/factory-crate splitter

Date: 2026-09-22

## Result

`SwgCuiResourceSplitter` is a bounded inventory-domain adapter. It receives a
typed `(source object ID, destination/container ID)` split request from
`CuiInventoryManager`, resolves the object, constrains a quantity editor to
`1..stackSize-1`, and emits one of two existing queued commands. It introduces
no private packet format.

## Client flow

RTTI type descriptor `0x018BD578`, vtables `0x015F34CC`/`0x015F3550`, and
constructor `0x00E10880` identify the mediator. Required controls are amount
textbox, slider, OK, Cancel, name text and a `CuiWidget3dObjectListViewer`.

The constructor subscribes to
`CuiInventoryManager::Messages::SplitContainer`. Callback `0x00E10F40` retains
the two U64 `NetworkId` values, resolves the source as either
`ResourceContainerObject` or `FactoryObject`, closes for invalid/too-small
stacks, configures the slider from 1 through `stackSize-1`, displays the source
name/object, and synchronizes slider/text state.

OK or Enter reaches `0x00E113A0`, which parses the integer, clamps it to the
available maximum, requires a positive value and dispatches:

| Source type | Command | Target | Argument |
|---|---|---|---|
| resource container | `resourceContainerSplit` | source U64 object ID | selected integer quantity |
| factory crate | `factoryCrateSplit` | source U64 object ID | selected integer quantity |

Cancel simply closes the mediator. Slider change `0x00E10E80` rewrites the
numeric textbox.

## Server authority

Both Stardust commands parse the first argument as an integer, require at least
one, resolve the U64 target, verify player ownership/containment and parent
capacity, then invoke the authoritative object `split()` operation. The factory
crate path additionally permits a cell parent only where the player is a
building administrator. The client maximum is convenience validation; the
server remains authoritative.

## Replacement requirement

Use a reusable quantity-dialog/view-model component fed by a typed inventory
event. Keep object IDs explicitly U64 and the quantity explicitly 32-bit in the
command-domain model. Resolve the original object template/appearance in the
preview widget, but do not couple preview availability to command correctness.

## Confidence

Event identity, object kinds, bounds, controls, command names, target and server
validation are **Confirmed**. Exact legacy visual layout is replaceable.

