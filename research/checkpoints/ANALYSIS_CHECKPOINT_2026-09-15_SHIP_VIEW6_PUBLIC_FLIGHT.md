# Analysis checkpoint: SHIP view-6 public flight and presentation state

Date: 2026-09-15  
Target: `C:\Stardust-DEV\Stardust.exe`  
Server correlation: current Stardust-3 source tree

## Scope and result

This block closes `ShipObjectMessage6` and the corresponding public `SHIP:6`
delta fields. The view exposes observer-relevant flight limits, target state,
targetable components, installed-component appearance CRCs, ship identity
strings, current shields and guild identity.

All 23 fields, legacy client registrations, container formats, active server
producers and the principal client callbacks are now mapped. Combined with the
previous blocks, all four primary SHIP baseline views are field-closed.

## Delivery boundary

`ShipObjectMessage6` is sent to every recipient for whom the ship is created.
`ShipDeltaVector` classifies view 6 as public and broadcasts accumulated deltas
to observers. This contrasts with private owner/member views 1 and 4.

## Confirmed 23-field schema

Fields 0-1 are inherited from TangibleObject view 6. Fields 2-22 are registered
by the ShipObject constructor:

| Index | Wire type | Meaning | Legacy client offset |
|---:|---|---|---:|
| 0 | U32 | common flags/constant; server emits `0x76` | inherited |
| 1 | ordered vector of U64 | defender object IDs | inherited `+0x2F0` |
| 2 | U16 | session/runtime ship unique ID | `+0x3D0` |
| 3 | F32 | effective acceleration rate | `+0x474` |
| 4 | F32 | effective deceleration rate | `+0x488` |
| 5 | F32 | effective pitch acceleration rate | `+0x49C` |
| 6 | F32 | effective yaw acceleration rate | `+0x4B0` |
| 7 | F32 | effective roll acceleration rate | `+0x4C4` |
| 8 | F32 | effective pitch rate | `+0x4D8` |
| 9 | F32 | effective yaw rate | `+0x4EC` |
| 10 | F32 | effective roll rate | `+0x500` |
| 11 | F32 | effective maximum speed | `+0x514` |
| 12 | U64 | selected target object ID | `+0xC20` |
| 13 | U32 | selected target component slot | `+0xC58` |
| 14 | byte-vector bit array | targetable component slots | `+0xC74` |
| 15 | delta map U32 -> U32 | component slot to attachment/appearance CRC | `+0x678` |
| 16 | ASCII | wing name; current server emits empty | `+0xCA8` |
| 17 | ASCII | ship type name; baseline populated for AI ships only | `+0xCCC` |
| 18 | ASCII | difficulty label | `+0xCF0` |
| 19 | ASCII | faction label | `+0xD14` |
| 20 | F32 | current front shield | `+0xA00` |
| 21 | F32 | current rear shield | `+0xA28` |
| 22 | U32 | owner guild ID | `+0xD38` |

`ship_object_replication_ctor_VA_00536670` registers exactly 21 ship-specific
members through view-6 helper `0x005554C0` after the two inherited fields. The
offsets describe only the legacy 32-bit object ABI.

## Container formats

### Defender vector

The inherited defender field uses the already-closed TangibleObject ordered
U64 vector: baseline count/counter plus U64 IDs; deltas support indexed
remove/add/set and clear. Index zero is the primary defender.

### Targetable-component bit array

Index 14 is a `DeltaAutoBitArray`. A baseline or field update contains:

```text
U32 byteCount
U32 updateCounter
U8 bytes[byteCount]
```

Component slot `n` maps to byte `n/8`, bit `n%8`. The server transmits the
complete byte vector when this field is updated, rather than a single-bit
operation. Bounds and byte-count allocation must be validated before commit.

### Component attachment/appearance map

Index 15 is `DeltaAutoMap<uint32,uint32>` rather than a packed map. Its
baseline is count/counter followed by ADD entries. Its delta is a genuine
operation list:

```text
U32 operationCount
U32 updateCounter
repeat operationCount times:
    U8 command           # ADD=0, DROP=1, SET=2
    U32 componentSlot
    U32 appearanceCRC    # DROP includes the previous value
```

Component install/remove and appearance recalculation actively update this
map. This distinction is compatibility-critical: SHIP:1/3/4 packed maps send
complete replacements, while SHIP:6[15] sends incremental operations.

## Effective flight values

Indices 3-11 are public, server-derived flight limits. Template/base values
live in SHIP:1 and the chassis scalar lives in SHIP:4; server methods combine
them with installed-component state, general efficiency, damage/power flags,
wing state, booster state and speed-rotation factors.

`updateActualEngineValues` recalculates acceleration, deceleration, pitch/yaw/
roll acceleration, pitch/yaw/roll rates and maximum speed, emitting only values
that changed. Engine install/remove initializes or clears relevant effective
rates. These values are suitable inputs to client prediction/control limits,
but instantaneous pose and velocity still come from transform traffic.

The U16 unique ID is allocated by `ShipUniqueIdMap`, set during initialization
and cleared on removal. It is a compact runtime identifier, not a replacement
for the U64 network object ID.

## Targeting and client callbacks

Target selection is active end to end:

- object-controller `TargetUpdate` validates the player's piloted/root ship
  and changes index 12;
- `UpdateShipTargetSlot` validates the target slot and changes index 13;
- space commands consume the selected U64 target;
- component targetability is recomputed from chassis slot metadata, installed
  components and damage/option state.

The executable provides specialized codecs and callbacks:

| Field | Snapshot/delta codec | Callback/result |
|---|---|---|
| target U64 | `0x00545290` / `0x005453C0` | `0x0053D040`; updates targeting/controller state and publishes `LookAtTargetChanged` |
| target slot U32 | `0x00545550` / `0x005455A0` | `0x00696760`; reconciles slot validity and publishes `LookAtTargetSlotChanged` |
| targetable bitfield | `0x00545860` / `0x005458F0` | `0x006969F0`; refreshes selection when the changed ship is the local player's root ship |

This establishes a clean replacement boundary: protocol state commits first;
target selection and UI/controller notifications run afterward.

## Component appearance callbacks

The add/set/drop callbacks at `0x0053C3E0`, `0x0053C130`, and `0x0053BE80`
belong to the index-15 component CRC map. They call
`ship_component_attachment_refresh_VA_00539B70` for the affected slot and then
publish `ShipObject::Messages::ComponentsChanged`. The refresh resolves the
installed component CRC, validates the chassis and attachment CRC, and updates
the ship attachment/appearance.

This corrects an earlier imprecise attribution to SHIP:3 health/armor maps.
Component installation normally emits both view-3 condition state and view-6
appearance CRC changes, but the attachment callback is owned by SHIP:6[15].

## Identity strings, shields and guild ID

- Index 16 is a required empty ASCII compatibility field in the current
  baseline; no server delta variable or producer exists.
- Index 17 contains the type string only for AI ships in the baseline. Player
  ships receive an empty string even though internal ship type state exists.
- Difficulty and faction strings come from templates/AI data. Faction can also
  be changed by active admin/script paths and synchronizes the inherited
  faction hash.
- Front/rear shields are current public values. Component install/removal,
  combat, nebula damage, recovery, repair, demolition and droid commands all
  update them. Maximum shield values remain SHIP:3 indices 20-21.
- The baseline derives index 22 directly from the current owner guild. The
  separate `shipGuildID` delta variable has no call to `setShipGuildID` in the
  current source tree. Baseline guild identity is active; delta production is
  currently dormant and source ownership is asymmetrical.

## Evidence and confidence

### Confirmed by code

- Exact server and client 23-field order and primitive/container types.
- Public delivery of view 6.
- Bit-array full-snapshot and component-map incremental grammars.
- Effective-flight recalculation and producer paths.
- Unique-ID allocation lifecycle.
- Target, target-slot and targetability client callbacks/events.
- Component CRC map ownership of attachment refresh callbacks.
- Shield producers and baseline identity-string rules.
- Dormant wing-name and guild-delta producers.

### Strongly inferred

- Effective flight values parameterize legacy client prediction/control. Their
  inputs and public replication are confirmed; the complete controller math is
  part of the subsequent transform/control block.

### Unknown or intentionally deferred

- Semantic bit meanings inside inherited constant `0x76`.
- Historical non-empty wing-name behavior.
- Intended synchronization policy for owner guild changes after baseline.
- Full client flight-prediction and transform reconciliation algorithms.

## Replacement-client contract

1. Make view 6 public observer state and views 1/4 private owner/crew state.
2. Preserve U16 unique ID separately from U64 object IDs.
3. Decode targetability as a bounded full byte vector.
4. Decode component CRC deltas as true ADD/DROP/SET operations; do not reuse
   the packed-map replacement decoder.
5. Stage all changes and run targeting/appearance notifications only after a
   successful commit.
6. Keep current and maximum shields separate and derive ratios outside the
   protocol layer.
7. Preserve empty wing/type compatibility fields and baseline-derived guild
   behavior until fixtures justify a different policy.

## Coverage effect

After publication of the five currently staged reports, the expected packet
matrix is **279 documented / 66 reachable gaps / 14 literal-opcode gaps**.
Weighted whole-client research remains conservatively **78%**. All primary
SHIP replication views are now field-closed; the next ship block should focus
on transform/control/prediction traffic rather than another baseline view.
