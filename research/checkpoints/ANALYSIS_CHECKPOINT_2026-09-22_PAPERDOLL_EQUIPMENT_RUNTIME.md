# Paperdoll and Equipment Runtime

Date: 2026-09-22  
Target: `C:\Stardust-DEV\Stardust.exe`

## Confirmed behavior

The paperdoll is a specialized 3D object viewer with container-selection and
drag/drop semantics. Its primary event handler at `0x00A60D30` operates only
when a displayed client object exists and delegates unhandled UI events to the
base object viewer.

For a dragged tangible object, the handler:

- resolves the dragged UI object to a `ClientObject`/`TangibleObject`;
- resolves both source and destination containment state;
- checks whether the displayed target is a creature and whether the item is
  valid for the target;
- examines `SkeletalAppearance2` and the target appearance extent;
- enumerates item attachment arrangements/hardpoints;
- obtains hardpoint transforms from the skeletal appearance;
- builds per-slot transformed hit volumes; and
- on a successful drop, calls the established container/equipment transfer
  path rather than mutating appearance state directly.

The same handler publishes container selection changes and clears transient
drag state on cancellation/end events. Warnings are emitted if an accepted
item still cannot be added, demonstrating that UI acceptance and authoritative
inventory movement are separate stages.

## Architectural consequence

The replacement paperdoll cannot be implemented as a static character image.
It requires the original wearable/container arrangement data, creature
skeleton, hardpoints, skeletal appearance attachment rules and object-template
metadata. The visual slot hit regions are derived from asset transforms and
extents rather than solely from fixed UI rectangles.

The modern implementation should separate:

- `EquipmentModel`: authoritative logical slot/arrangement validation;
- `PaperdollScene`: skeletal preview instance and attached appearances;
- `PaperdollHitMap`: asset-derived transformed slot volumes;
- `InventoryTransferService`: request/response or command path for movement;
- `PaperdollMediator`: UI selection, drag state and feedback.

Internal x64 handles may replace raw pointers, but network object IDs,
arrangement identifiers and original asset indices retain their legacy widths.

## Confidence

| Finding | Confidence | Evidence |
|---|---|---|
| Paperdoll is a specialized 3D object viewer | Confirmed | inheritance/vtables and base-event delegation |
| Dragged items resolve to tangible client objects | Confirmed | RTTI casts in `0x00A60D30` |
| Slot regions use skeletal hardpoint transforms | Confirmed | skeletal appearance and hardpoint enumeration in `0x00A60D30` |
| Drop uses container/equipment transfer service | Confirmed | terminal transfer calls and failure warnings |
| Original wearable/skeleton data is mandatory | Confirmed architectural dependency | runtime data consumed by the handler |

The friendly labels of numeric UI event codes and every arrangement-validation
helper remain bounded naming work. Their observable state transitions and asset
dependencies are recovered.
