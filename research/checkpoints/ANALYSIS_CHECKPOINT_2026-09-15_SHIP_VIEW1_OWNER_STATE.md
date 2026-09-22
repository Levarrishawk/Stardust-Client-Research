# Analysis checkpoint: SHIP view-1 owner/member state

Date: 2026-09-15  
Target: `C:\Stardust-DEV\Stardust.exe`  
Server correlation: current Stardust-3 source tree

## Scope and result

This block closes `ShipObjectMessage1` and the corresponding `SHIP:1`
`DeltasMessage` fields. View 1 is the private, detailed ship-engineering view:
component performance/name/ammunition maps, chassis capacity, shield and
capacitor rates, engine handling, reactor output, booster characteristics and
droid state.

The complete 38-field order, wire types, legacy client registrations, packed
map behavior, server producers and recipient rules are now specified. Flight
controls and current-energy state are not part of this view; current mass,
speed, capacitor and booster energy reside in SHIP view 4.

## Recipient boundary

`ShipObjectImplementation::sendBaselinesTo` sends view 1 only when the target
is the ship owner or a subchild/occupant of that ship. Ordinary observers get
views 3 and 6, but not views 1 or 4.

Aggregated updates use `ShipDeltaVector`, which classifies view 1 as private.
On send it clones the delta to the owner, sends it to ship members, and resets
the accumulator. Several specialized component installers also construct a
view-1 `DeltaMessage` and send it directly to the pilot. A replacement must
model this authorization/interest boundary; SHIP:1 is not public telemetry.

## Confirmed 38-field schema

The first two U32 values are inherited client view-1 scalar slots. The ship
server labels them `serverObjectsStart` and `serverObjectsEnd` and always emits
zero. Other object families reuse the inherited positions differently, so the
replacement ship model should retain them as zero compatibility placeholders
rather than assign creature cash/bank semantics.

| Index | Wire type | Ship meaning | Legacy client offset |
|---:|---|---|---:|
| 0 | U32 | zero start placeholder | inherited |
| 1 | U32 | zero end placeholder | inherited |
| 2 | packed map U32 -> F32 | component efficiency | `+0x598` |
| 3 | packed map U32 -> F32 | component energy efficiency | `+0x5D0` |
| 4 | packed map U32 -> F32 | component energy cost | `+0x608` |
| 5 | packed map U32 -> F32 | component mass | `+0x640` |
| 6 | packed map U32 -> Unicode | displayed component name | `+0x760` |
| 7 | packed map U32 -> U64 | component creator object ID | `+0x798` |
| 8 | packed map U32 -> F32 | maximum weapon damage | `+0x7D0` |
| 9 | packed map U32 -> F32 | minimum weapon damage | `+0x808` |
| 10 | packed map U32 -> F32 | shield effectiveness | `+0x840` |
| 11 | packed map U32 -> F32 | armor effectiveness | `+0x878` |
| 12 | packed map U32 -> F32 | weapon energy per shot | `+0x8B0` |
| 13 | packed map U32 -> F32 | weapon refire rate | `+0x8E8` |
| 14 | packed map U32 -> S32 | current ammunition | `+0x958` |
| 15 | packed map U32 -> S32 | maximum ammunition | `+0x990` |
| 16 | packed map U32 -> U32 | ammunition class | `+0x9C8` |
| 17 | F32 | maximum chassis mass | `+0x438` |
| 18 | F32 | shield recharge rate | `+0xA50` |
| 19 | F32 | maximum capacitor energy | `+0xA78` |
| 20 | F32 | capacitor recharge rate | `+0xA8C` |
| 21 | F32 | engine acceleration rate | `+0xAA0` |
| 22 | F32 | engine deceleration rate | `+0xAB4` |
| 23 | F32 | engine pitch acceleration rate | `+0xAC8` |
| 24 | F32 | engine yaw acceleration rate | `+0xADC` |
| 25 | F32 | engine roll acceleration rate | `+0xAF0` |
| 26 | F32 | maximum engine pitch rate | `+0xB04` |
| 27 | F32 | maximum engine yaw rate | `+0xB18` |
| 28 | F32 | maximum engine roll rate | `+0xB2C` |
| 29 | F32 | maximum engine speed | `+0xB40` |
| 30 | F32 | reactor generation rate | `+0xB54` |
| 31 | F32 | maximum booster energy | `+0xB7C` |
| 32 | F32 | booster recharge rate | `+0xB90` |
| 33 | F32 | booster consumption rate | `+0xBA4` |
| 34 | F32 | booster acceleration | `+0xBB8` |
| 35 | F32 | maximum booster speed | `+0xBCC` |
| 36 | F32 | droid command speed | `+0xBE0` |
| 37 | U64 | associated droid control-device ID | `+0xBF8` |

`ship_object_replication_ctor_VA_00536670` registers the 36 additions through
the view-1 registration helper `0x00555420` in precisely this order after the
two inherited slots. The different map vtables independently distinguish the
F32, Unicode, U64, signed-S32 and unsigned-U32 value families. The offsets are
legacy ABI evidence only and must not be copied into a native x64 layout.

## Packed-map wire behavior

All indices 2-16 are `DeltaAutoPackedMap` fields. A baseline field is:

```text
U32 entryCount
U32 updateCounter
repeat entryCount times:
    U8 command = 0 (ADD)
    U32 componentSlot
    value
```

Unicode values use the established U32 code-unit count plus UTF-16LE units;
creator IDs are fixed U64; ammunition counts are signed 32-bit values.

The setter's `ADD=0`, `DROP=1`, or `SET=2` argument changes the server's local
map. `DeltaAutoPackedMap::update` then emits the entire resulting map through
`insertToMessage`; every transmitted entry is ADD. Consequently a SHIP:1 map
field in a delta is an atomic replacement snapshot, not an operation list.
This is the same important packed-map behavior proven for SHIP:3.

The public ammunition setters accept `float`, but indices 14 and 15 are
instantiated as `int`. Values therefore narrow to signed 32-bit storage/wire
form. Current producers pass integral ammunition counts. A replacement should
model ammunition as S32 and reject fractional fixture data rather than
preserving this source-level type inconsistency.

## Active server producers

### Base component installation

Installing or removing any ship component updates indices 2-6: displayed
name, mass, energy efficiency, general efficiency and energy cost. The same
transaction updates public SHIP:3 health/armor/options, current mass in view 4
and component appearance CRC in view 6. This is a multi-view state change that
should be validated and applied coherently in the replacement model.

Index 7, component creator ID, has a complete schema and client registration,
but no call to `setComponentCreator` exists in the current source tree. It is
therefore **supported but dormant** in current Stardust behavior.

### Weapon and ammunition state

Weapon installation/removal and ammunition loading update indices 8-16.
Projectile creation decrements current ammunition at index 14. Weapon maps
drive damage range, armor/shield effectiveness, energy drain, refire timing,
remaining/max ammo and ammunition class.

### Chassis and specialized components

- Ship-template and deed creation populate maximum chassis mass and base
  acceleration/deceleration/rotation acceleration.
- Engine install/remove populates pitch/yaw/roll maximum rates and maximum
  speed; AI templates also populate the acceleration fields.
- Shield install/remove changes shield recharge rate.
- Capacitor install/remove changes maximum energy and recharge rate.
- Reactor install/remove changes generation rate.
- Booster install/remove changes maximum/recharge/consumption energy,
  acceleration and maximum speed.
- Droid-interface install/remove changes command speed.
- Droid association, disassociation and launch cleanup change the U64 droid
  control-device ID.
- Droid commands and `resetEfficiency` alter the per-slot efficiency maps.

Server simulation consumes these values directly for energy allocation,
shield/capacitor/booster recharge, component load, rotation, speed and weapon
fire. They are not presentation-only attributes.

## Client-side effects

All fields enter the generic baseline/delta object registry. Exact downstream
consumer granularity for every scalar remains intentionally unclaimed, but
the model is consumed by ship HUD, component-management and flight systems.

The specialized U64 field at index 37 uses snapshot/delta decoders
`0x00545050` and `0x005450E0`. Its callback publishes
`ShipObject::Messages::DroidPcdChanged`, confirming that the client interprets
the value as the associated droid persistent-control-device relationship.

Unlike public SHIP:3 component CRC/health maps, the view-1 engineering maps do
not by themselves establish attachment appearance. Appearance reconciliation
belongs to the correlated view-6 component CRC and view-3 component mutation
path. A replacement should expose one ship-state transaction spanning these
views rather than let UI or rendering react to partially received data.

## Evidence and confidence

### Confirmed by code

- Exact 38-field server order and all primitive/container value types.
- Exact client registration order and legacy member offsets.
- Private baseline and delta recipient policy.
- Full-map replacement behavior for all fifteen packed maps.
- Active install/remove/ammunition/droid producers.
- Current absence of a component-creator setter call.
- Droid relationship callback and event identity.

### Strongly inferred

- Detailed engineering fields feed the named legacy ship HUD/component/flight
  consumers visible in RTTI. Their subsystem role is clear, but individual
  listener methods have not all been address-traced.

### Unknown or intentionally deferred

- Exact listener/redraw behavior for every engineering field.
- Whether an external script/plugin path not present in this tree can populate
  component creator IDs.
- Cross-message atomicity expectations when one install operation emits views
  1, 3, 4 and 6 as separate `DeltasMessage` packets.
- SHIP view 4 current-resource behavior, which is the recommended next block.

## Replacement-client contract

1. Treat view 1 as authorized owner/crew state, never public observer state.
2. Preserve every wire width: U32 slots/classes, S32 ammunition, U64 object
   IDs and F32 simulation parameters remain fixed-width on x64.
3. Decode packed maps as bounded complete replacements and commit only after
   exact field consumption.
4. Store component creator/droid IDs as protocol object identifiers, never
   native pointers.
5. Separate replicated engineering state from derived flight calculations and
   presentation notifications.
6. Coalesce multi-view component changes in a semantic transaction where
   possible, while preserving the server's independent packet ordering.

## Coverage effect

After publication of the three currently staged reports, the expected packet
matrix is **277 documented / 68 reachable gaps / 14 literal-opcode gaps**.
Weighted whole-client research remains conservatively **78%**; this closes the
owner engineering schema but not SHIP view 4, view 6 or flight-control traffic.
