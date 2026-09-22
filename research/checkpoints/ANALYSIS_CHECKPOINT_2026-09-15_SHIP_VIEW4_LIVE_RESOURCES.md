# Analysis checkpoint: SHIP view-4 private live resources

Date: 2026-09-15  
Target: `C:\Stardust-DEV\Stardust.exe`  
Server correlation: current Stardust-3 source tree

## Scope and result

This block closes `ShipObjectMessage4` and the corresponding private `SHIP:4`
delta fields. The view contains current installed mass, the chassis speed
scalar, current capacitor and booster energy, and per-component refire
efficiency.

The five-field order, client registrations, packed-map behavior, recipient
rules and current server producers are confirmed. This also corrects an
important preliminary label: index 1 is not instantaneous ship velocity.

## Recipient boundary

View 4 uses the same private baseline boundary as view 1. Stardust sends it
only to the owner or a player contained beneath the ship. `ShipDeltaVector`
classifies view 4 as private and sends accumulated updates to the owner and
ship members. Ordinary world observers do not receive this view.

## Confirmed schema

| Index | Wire type | Meaning | Legacy client offset |
|---:|---|---|---:|
| 0 | F32 | current installed chassis/component mass | `+0x44C` |
| 1 | F32 | chassis speed scalar | `+0x460` |
| 2 | F32 | current capacitor energy | `+0xA64` |
| 3 | F32 | current booster energy | `+0xB68` |
| 4 | packed map U32 -> F32 | refire-efficiency multiplier by component slot | `+0x920` |

`ship_object_replication_ctor_VA_00536670` registers these five members in
this exact order through view-4 helper `0x00555440`. The first four use the
client float AutoDelta specialization; index 4 uses the same U32-to-F32 packed
map specialization seen in the other SHIP views. These offsets are evidence
from the old 32-bit ABI, not a replacement memory-layout requirement.

## Chassis speed is not current velocity

The server initializes `chassisSpeed` to 1.0 and loads index 1 from
`SharedShipObjectTemplate::getChassisSpeed`; a `maxSpeed` attribute may also
replace it during template loading. `calculateActualMaxSpeed` multiplies the
sum of effective engine and active-booster speed by this chassis scalar, then
applies the wing-state scalar and clamps the result to 512.

The server separately owns transient `currentSpeed`. Ship transform callbacks
and `ShipObjectTransform` update that transient value, but they do not call
`setChassisSpeed` and do not produce a SHIP:4 index-1 delta. Instantaneous
velocity belongs to ship transform/control traffic. A replacement that treats
index 1 as current speed would corrupt prediction, gauges and maximum-speed
calculation.

## Packed refire-efficiency map

Index 4 is `DeltaAutoPackedMap<uint32,float,'SHIP',4,4>`:

```text
U32 entryCount
U32 updateCounter
repeat entryCount times:
    U8 command = 0 (ADD)
    U32 componentSlot
    F32 refireEfficiency
```

As with SHIP views 1 and 3, `ADD`, `DROP`, and `SET` select the server-side map
mutation only. A baseline or delta field transmits the complete resulting map,
with every wire entry marked ADD. The replacement must atomically replace the
map after validating count, command, slot and exact payload consumption.

Weapon install/ammunition configuration adds or sets a value of 1.0;
uninstall removes or zeros it. `resetEfficiency` restores 1.0 for every slot
already present in the component-efficiency map, and AI initialization also
sets 1.0 for weapon slots.

No current server calculation reads `componentRefireEfficiency`; authoritative
AI refire uses the separate base refire-rate map in SHIP:1. The view-4 value is
therefore replicated for client-side weapon timing/presentation compatibility.
That client role is strongly inferred from the schema and refire UI/runtime
artifacts, while its exact multiplication site remains untraced.

## Active scalar producers and consumers

### Current mass

Base component installation and removal recalculate the sum of installed
component masses and update index 0 in the same `ShipDeltaVector` transaction
as the component maps. The maximum permitted chassis mass remains SHIP:1 index
17; current and maximum mass must be separate internal values.

### Chassis speed scalar

Index 1 is populated during ship-template initialization and is consumed by
server maximum-speed calculation. It is normally static after construction in
the current source tree.

### Capacitor energy

Capacitor installation initializes index 2; uninstall and demolition clear it.
The periodic recovery loop recharges or decays it according to maximum energy,
recharge rate, component efficiency and reactor-power availability. Projectile
creation subtracts weapon energy cost. Repair and droid reinforcement paths
can also change it.

### Booster energy

Booster installation initializes index 3; uninstall and demolition clear it.
While boosting, the recovery loop subtracts consumption and disables/restarts
the booster when energy is insufficient. When inactive it recharges or decays
toward the reactor- and efficiency-adjusted maximum. Repair also replenishes
it.

These resource values participate in actual server simulation and must not be
treated as cosmetic gauges.

## Client-side evidence and boundary

The executable contains typed `ShipComponentDataCapacitor` and
`ShipComponentDataBooster` classes, capacitor/booster attribute labels,
booster control actions and HUD artifacts including `BoosterGauge`. The
generic SHIP registry supplies the five values to that client object model.

No unique post-delta callback comparable to `DroidPcdChanged` or the public
component-attachment refresh has yet been proven for these five fields. The
safe replacement boundary is therefore:

1. validate and commit the private replicated state;
2. update derived resource ratios and weapon timing;
3. publish semantic resource/component changes to flight and HUD systems;
4. keep rendering/UI outside the wire decoder.

Exact legacy redraw granularity is not required for protocol compatibility.

## Evidence and confidence

### Confirmed by code

- Exact five-field server order and types.
- Exact client registration order, view selector and legacy offsets.
- Owner/occupant/member-only delivery.
- Full-map replacement behavior for index 4.
- Current mass, capacitor and booster production paths.
- Chassis-speed use as a maximum-speed scalar rather than live velocity.
- Separate transient current-speed updates in transform processing.
- Absence of a current server-side refire-efficiency consumer.

### Strongly inferred

- Refire efficiency modifies client weapon cadence or its presentation. The
  replicated name, weapon producers and client refire artifacts support this,
  but the final arithmetic call site is not yet address-backed.

### Unknown or intentionally deferred

- Exact client notification callbacks and HUD redraw paths for each scalar.
- The final legacy refire-efficiency multiplication site.
- Ship transform/control packet behavior carrying instantaneous velocity; it
  remains a separate analysis block after SHIP view 6.

## Replacement-client contract

1. Gate view 4 to owner/crew interest just like view 1.
2. Preserve all F32 wire values and the U32-keyed packed map on x64.
3. Name index 1 `chassis_speed_scalar` or equivalent, never `current_speed`.
4. Source instantaneous velocity from validated ship transform traffic.
5. Keep current mass/energy distinct from SHIP:1 maxima and rates.
6. Apply refire-map replacements atomically and derive UI/weapon timing only
   after commit.

## Coverage effect

After publication of the four currently staged reports, the expected packet
matrix is **278 documented / 67 reachable gaps / 14 literal-opcode gaps**.
Weighted whole-client research remains conservatively **78%**. SHIP view 6 and
space-flight control/transform traffic remain the next major ship boundaries.
