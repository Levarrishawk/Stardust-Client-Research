# Ship Weapon Group Assignment Checkpoint

Date: 2026-09-22  
Target: `C:\Stardust-DEV\Stardust.exe`

## Finding

Ship weapon groups are a client-local, per-ship control configuration. The UI
does not send group membership to the server. It writes a compact local IFF
record and publishes local manager events consumed by the fly-out/HUD.

## UI model

`SwgCuiWeaponGroupAssignment` constructor `0x00DD9630` binds three lists
(`leftlist`, `toplist`, `bottomlist`), add/remove buttons and top/bottom
attribute text. Activation resolves the current ship, rebuilds list models and
subscribes to `ShipWeaponGroupManager::WeaponGroupsResetForShip`.

Selection/button handling moves installed weapon slots between the unassigned
list and the selected group and updates the two group-mode attributes. The
manager publishes `(GroupType, int)` `WeaponGroupChanged` events; the space
fly-out consumes these to refresh weapon presentation.

## Persistence format

Manager load/save routines `0x006F3670`, `0x006F3840` and `0x006F39D0` use IFF
form/tag `SWGI`. Each group record contains:

- signed/validated group index;
- U32 member count;
- repeated U32 weapon component-slot index;
- in the current format, two U8 boolean mode flags per member.

One legacy load path derives the two booleans from current ship state rather
than reading them, showing backward-compatible evolution. Indices outside the
configured group count are warned and skipped. Save iterates at most 100
component slots and emits only assigned slots. Dirty groups publish a reset
event after load.

## Replacement requirements

- Import/export the original `SWGI` profile and preserve component-slot
  numbering and both flags.
- Key configurations by stable ship identity/profile rather than native object
  addresses.
- Keep assignment local; firing commands may reference local group selection,
  but server ship state remains authoritative for installed weapons/ammunition.
- Make unknown future fields/version forms preservable and validate counts and
  slot ranges before allocation.

## Confidence

Local ownership, UI lists/actions, event shape, `SWGI` record fields and
100-slot bound are **confirmed**. Original semantic names for the two per-member
flags are **unknown naming-only**; their independent boolean behavior is
preserved. Exact profile pathname remains a focused lookup item.

