# Ship Weapon Group Manager

Date: 2026-09-21  
Target: `C:\Stardust-DEV\Stardust.exe`  
Scope: local space-combat weapon grouping, persistence, selection, and notifications

## Confidence key

- **Confirmed**: directly visible in decompiled code, RTTI, strings, or asset/server definitions.
- **Strongly inferred**: behavior follows from multiple mutually consistent call sites.
- **Tentative**: a useful working interpretation whose exact public name is not recovered.

## Result

`ShipWeaponGroupManager` is a client-local space-combat service. It maintains a per-ship collection of eight weapon groups, persists them in a versioned IFF profile (`profiles/... .wpg`), resets/validates group membership against the currently installed ship components, exposes group selection and usability queries, and broadcasts typed UI/gameplay events. This is not a server-owned replicated structure.

## Lifecycle and profile persistence

| VA | Provisional name | Evidence |
|---|---|---|
| `0x00589A20` | `ship_weapon_group_manager_install` | Loads `forcefeedback/guns_fire.ffe`, establishes exit cleanup. |
| `0x00589B30` | `ship_weapon_group_manager_remove` | Named diagnostic `ShipWeaponGroupManager::remove`; tears down installed state. |
| `0x00589C10` | `ship_weapon_group_manager_load_profile` | Constructs a path under `profiles/` with `.wpg`, opens an IFF and dispatches versions. |
| `0x00589F60` | `ship_weapon_group_manager_load_wpg_v0000` | Reads `FORM WPGS/0000`, then repeated per-ship `FORM WGFS/CHUNK INFO` records. |
| `0x0058A160` | `ship_weapon_group_manager_load_wpg_v0001` | Same outer framing, using the newer group-member deserializer. |
| `0x0058A340` | `ship_weapon_group_manager_save_profile` | Always writes `WPGS/0001`; writes a temporary/output file and uses the client file replacement helper. |

The outer record is:

```text
FORM WPGS
  FORM 0001
    FORM WGFS
      CHUNK INFO
        shipNetworkId : client string representation
        repeated group records written by ShipWeaponGroupsForShip::save
```

Only valid retained ship entries are emitted. A failed final write produces a named warning. The file is preference/profile state and must not be treated as a network protocol structure.

## Per-ship and per-group model

`0x006F32B0` constructs `ShipWeaponGroupsForShip` with exactly **eight** group objects. Each group object (`0x006F3F90`) is 20 bytes and contains:

- a 32-bit group index;
- three separately allocated set/bitset-like containers;
- a one-byte dirty flag.

The component membership API is bounded to **100 indexed slots** in the observed loops. Server-side `Components::WEAPON_START` is 12 and `CAPITALSLOTMAX` is 111, yielding exactly 100 possible weapon positions (`12..111`). This independently explains the client bound: weapon-group member index 0 corresponds conceptually to component slot 12, and index 99 to slot 111. This mapping is **strongly inferred**, while the matching counts and server constants are confirmed.

The three set-like fields represent membership and two per-member boolean classifications. Version `0001` explicitly persists each member as:

```text
groupIndex       : int32
memberCount      : int32
repeat memberCount:
  weaponIndex    : int32   // bounded by the 100-slot domain
  classificationA: uint8
  classificationB: uint8
```

Version `0000` stores only `weaponIndex`; on load it reconstructs the two classifications by querying the live ship component. Version `0001` preserves the booleans directly. Exact original names for the two classifications are not recovered; call sites show that they distinguish projectile/missile-like weapon capabilities and drive the two group-selection modes. They remain provisional rather than being given invented field names.

## Validation, reset, and events

| VA | Provisional name | Behavior |
|---|---|---|
| `0x0058A5E0` | `ship_weapon_groups_get_or_create_for_ship` | Rejects the invalid/zero `NetworkId`, returns or constructs per-ship state. |
| `0x006F34D0` | `ship_weapon_groups_reset_from_ship_components` | Clears eight groups, scans up to 100 weapon positions, classifies installed components, assigns them across four passes, and clears dirty state. |
| `0x0058A9B0` | `ship_weapon_group_manager_reset_ship` | Performs reset (forced or when not initialized), refreshes the current ship selection, then emits `WeaponGroupsResetForShip(NetworkId const&)`. |
| `0x0058AE50` | `ship_weapon_group_manager_cycle_selected_group` | Cycles forward/backward through eligible groups for one or both group types and emits `WeaponGroupChanged(pair<GroupType,int> const&)`. |
| `0x0058B2C0` | `ship_weapon_group_manager_refresh_both_selections` | Calls the cycle/validation routine for both group types. |
| `0x0058A8C0` | `ship_weapon_group_manager_selected_group_is_usable` | Tests current game/ship state and determines whether a selected group contains an installed, currently usable weapon. |

The reset routine divides installed weapons into four client-defined categories based on three component/template predicates. The precise original enum labels are not recovered, but the behavior is clear: membership is recomputed from the ship's installed component table, not trusted blindly from disk.

## Replacement-client implications

1. Reproduce the eight logical groups and 100-weapon index domain; do not copy 32-bit pointers or STL layouts.
2. Keep `.wpg` as a versioned compatibility/preferences reader if interoperability with existing profiles is desired. A modern internal representation can use vectors/bitsets and explicit value types.
3. Resolve a group member against live ship component state before firing. Persisted membership is advisory and can become stale after refitting.
4. Preserve the two logical group-selection channels and event semantics, even if the UI is redesigned.
5. Treat the two saved classification bytes as compatibility fields until their exact names are recovered from additional call sites/assets.
6. Component slot IDs remain 32-bit protocol/data identifiers; an x64 process must not widen file or wire fields.

## Remaining local unknowns

- Exact original enum names for `GroupType` and the two per-member booleans.
- Exact filename identity components inserted between `profiles/` and `.wpg` (the path builder is shared profile infrastructure).
- Exact UI mediators subscribing to both transceivers; the typed payloads and dispatch behavior are confirmed.

These unknowns do not block a behavioral implementation of group storage, selection, validation, or persistence.

