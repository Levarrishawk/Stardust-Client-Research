# Stardust.exe Analysis Checkpoint: Creature View-4 Deltas

Date: 2026-09-15  
Target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Result

This block closes `CreatureObjectDeltaMessage4` against the executable's complete fourteen-field `CREO:4` registry. Like view 6, this server class is an indexed specialization of generic `DeltasMessage` (`0x12862153`), not a separate top-level message: object tag `CREO` (`0x4352454F`), view 4.

The confirmed route is:

`DeltasMessage 0x12862153` -> dispatcher `0x0051C76D` -> parser `0x00ADB2D0` -> object lookup -> view dispatcher `0x00554E90` -> view-4 registry at object `+0xC4` -> indexed decoder `0x0118AA00` -> callback commit `0x0118A970`.

## Complete view-4 schema

| Index | Field | Wire codec | Replacement role |
|---:|---|---|---|
| 0 | acceleration multiplier base | F32 | locomotion parameter |
| 1 | acceleration multiplier modifier | F32 | locomotion modifier |
| 2 | encumbrances | indexed S32 vector | nine-stat encumbrance state |
| 3 | skill modifiers | keyed delta map | ASCII16 skill-mod name -> S32 base mod + S32 bonus |
| 4 | speed multiplier base | F32 | locomotion parameter |
| 5 | speed multiplier modifier | F32 | locomotion modifier |
| 6 | listen-to object | U64 NetworkId | entertainer/listening relationship |
| 7 | run speed | F32 | locomotion parameter |
| 8 | slope-modifier angle | F32 | terrain-negotiation threshold |
| 9 | slope-modifier percent | F32 | terrain-negotiation scale |
| 10 | turn scale | F32 | rotation/locomotion scale |
| 11 | walk speed | F32 | locomotion parameter |
| 12 | water modifier percent | F32 | swimming/water movement scale |
| 13 | space-mission objects | keyed U64/U64 set-map | mission-owner NetworkId -> mission-object NetworkId |

Constructor `0x0042ECF0` registers exactly fourteen fields into view 4 via `client_view4_register_field_VA_00555440`. This ordering independently matches `CreatureObjectMessage4` and every active scalar index in the delta builder.

## Container grammars

### Encumbrances, index 2

The baseline form is U32 element count, U32 update counter, then repeated S32 values. Delta form uses the shared indexed-vector control structure: operation count and update counter, followed by command byte, U16 element index where required, and S32 value where required. The replacement must require the canonical nine-stat shape where gameplay consumes it while still parsing the generic container defensively.

### Skill modifiers, index 3

The current server's `SkillModList` derives from `DeltaVectorMap<String, SkillModEntry>`. Snapshot entries are command byte `ADD=0`, ASCII16 key, S32 skill modifier and S32 skill bonus. Delta commands are `ADD=0`, `DROP=1`, `SET=2`; current `DROP` also serializes the removed value pair. Client RTTI exposes `CreatureObject::Messages::SkillModsChanged`, consumed by the character-stat/skills UI. Internal server aggregation by permanent/bonus groups is not transmitted; only the visible combined entry is wire state.

### Space-mission objects, index 13

Snapshot form is U32 count, U32 update counter, then repeated U64 owner ID and U64 mission-object ID. Current whole-map delta emission writes U32 operation count, U32 update counter and, for each entry, command `1` plus both U64 values. Clear writes operation count one, incremented counter and command `2`. These are NetworkIds, not pointers.

## Active producer behavior

Skill acquisition/removal sends a self-only batch of base/modifier speed, acceleration, run/walk, slope and water values for terrain-negotiation and movement changes. Individual setters emit their corresponding scalar field. Listen-target changes are self-only. Space-mission lifecycle emits index 13 whole-map updates or clear.

`updateSpeedAndAccelerationMods()` contains a compatibility workaround: when both acceleration and speed modifiers are zero, acceleration sent on the wire becomes `0.1`; for non-self recipients, a zero speed modifier becomes `0.1`. The replacement must preserve received values as authoritative and must not reinterpret this producer-side substitution as a general decoder clamp.

## Architecture implications

- View 4 is character locomotion/gameplay state, not renderer state. Feed validated values into a movement-parameter model used by prediction and animation.
- Preserve IEEE-754 32-bit wire values even if internal calculations use wider precision.
- Reject NaN/infinite or unreasonable movement values at a policy layer without changing the wire schema.
- Apply container changes transactionally and coalesce `SkillModsChanged` after successful commit.
- Keep skill-mod definitions and localization in the asset/data-table layer; the packet contains names and numeric values only.
- Treat listen and space-mission IDs as lazily resolvable NetworkIds.

## Confidence

- **Confirmed:** generic delta route, all fourteen indices and widths, scalar server producers, server container layouts/commands, view-4 registration count/order and skill-mod change-event identity.
- **Strongly inferred:** exact prediction/animation consumers of each movement scalar; those are downstream systems, not wire uncertainties.
- **Unknown:** whether historical servers used additional container commands beyond those present in current Stardust/shared container implementations.

## Progress

`CreatureObjectDeltaMessage4` moves to documented: **270 documented / 75 reachable gaps / 14 literal-opcode gaps**. Weighted whole-client coverage remains conservatively **78%**.
