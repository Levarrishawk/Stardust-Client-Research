# Parent-Relative Movement and Full-Precision Correction

Date: 2026-09-15  
Authority: `C:\Stardust-DEV\Stardust.exe` plus the current Stardust server tree  
Scope: ObjectController inner type `0xF1`, parent/cell-relative transforms, and the related compact broadcast path

## Result

The server packet class `DataTransformWithParent` and its client-originated callback are now closed at the wire and state boundary. Inner type `0xF1` uses `MessageQueueDataTransformWithParent` in the executable. It is a full-precision transform/correction path, distinct from the compact top-level `UpdateTransformWithParent` broadcast even though both eventually feed parent-relative object-controller movement.

The most important result is a direction-dependent grammar. The ObjectController outer scalar participates in the C-to-server transform timestamp but is not part of the server-to-client nested payload. A replacement must register separate encoders/decoders by direction.

## ObjectController envelope interaction

The shared envelope is:

| Field | Width | Meaning here |
|---|---:|---|
| priority | U32 | server constructor uses `0x1B`; server callback also uses this to select full vs light rebroadcast policy |
| inner type | U32 | `0xF1` |
| addressed object | U64 | moving object/player ID |
| outer scalar | F32/raw U32 | timestamp on C-to-server movement; zero in current server-originated corrections |
| nested payload | variable | direction-specific fields below |

`ObjectControllerMessageCallback::parse` reads only priority, type, and object ID before delegating. It intentionally leaves the outer scalar at the stream cursor. Therefore `Transform::parseDataTransformWithParent` consumes that four-byte scalar as `timeStamp`, then consumes the nested movement counter. This reconciles the apparent source discrepancy.

## Client-to-server wire grammar

| Order | Type | Meaning |
|---:|---|---|
| 1 | outer F32/raw U32 | client timestamp, consumed as U32 by server transform parser |
| 2 | U32 | movement counter |
| 3 | U64 | parent/cell/ship object ID |
| 4-7 | F32 x/y/z/w | quaternion components |
| 8-10 | F32 x/z/y | local position |
| 11 | F32 | current speed |

The executable packer at `0x00AD4360` writes one U32 movement counter, a `NetworkId`, one transform value (quaternion plus translation), and speed. The outer ObjectController packer supplies the preceding scalar/timestamp.

## Server-to-client full correction grammar

Current server `DataTransformWithParent` constructs priority `0x1B`, type `0xF1`, addressed object ID and outer scalar zero. Its nested bytes are:

| Order | Type | Meaning |
|---:|---|---|
| 1 | U32 | movement counter |
| 2 | U64 | parent ID |
| 3-6 | F32 x/y/z/w | quaternion |
| 7-9 | F32 x/z/y | parent-local position |
| 10 | F32 | speed; zero for non-creatures |

Executable unpacker `0x00AD43B0` reads exactly this nested grammar after the outer parser has already consumed its scalar. It constructs a `MessageQueueDataTransformWithParent` object at `0x00AD4260`. Quaternion NaNs are replaced with identity before transform construction. The registered pack/unpack pair is installed at `0x00AD4226`; RTTI and vtable `0x015EAFB4` confirm the payload class identity.

## Related compact broadcast

Top-level opcode `UpdateTransformWithParent` (`0xC867AB5A`) is a bandwidth-reduced update sent to observers:

```text
U64 parent
U64 object
S16 x*8, S16 z*8, S16 y*8
U32 movement counter
S8 speed
S8 special direction angle
```

The executable route remains `0x0051C9AC -> 0x00ADA510 -> 0x0051C9ED..0x0051CB32`, where the target object/controller is resolved and an `0xF1` movement command is queued. This compact packet loses quaternion pitch/roll and fractional speed, so it is not a substitute for the full controller correction.

## Server validation and state effects

The C-to-server callback runs on the player's zone task queue and validates session player, zone, ghost, last validated position, teleport/forced-transform state, timestamp delta, parent existence/type and zone membership. Position changes additionally check:

- finite and bounded local coordinates;
- permitted posture and movement/animation state;
- inertia unless forced;
- frozen/overloaded-inventory restrictions for unprivileged users;
- building or POB-ship ancestry and building entry permission;
- adjacent connected cells and floor-height tolerance (`0.25`);
- container `WALKIN` permission;
- close-object range during first attachment; and
- speed-hack validation.

Accepted movement updates speed/locomotion, local position, parent membership, quaternion, movement counter, sync stamp and validated position when fully validated. Invalid authoritative cases bounce the player to the last validated `{position,parent}` and stop movement.

Static/inert updates may normalize direction and send a full `0xF1` correction back to the player. Accepted movement is normally rebroadcast to observers through compact `UpdateTransformWithParent`; priority `0x23` selects the full/reliable broadcast class while other priorities select the light/unreliable variant. Invisible players suppress observer broadcasts.

## Parent and coordinate semantics

Position is parent-local whenever parent ID is nonzero. Ground interiors require a `CellObject`; space also permits supported ship attachment points. World position is derived separately from the parent/root transform. Wire coordinate order is consistently x/z/y even though internal `Vector3` construction presents logical x/y/z. This axis conversion must be explicit at the codec boundary.

Movement counter ordering and timestamp validation are separate mechanisms. The counter travels to clients and labels authoritative order; the timestamp is used by the server for elapsed-time/inertia/synchronization tests.

## Replacement-client architecture

Use distinct types:

```text
MovementWireC2S
  timestamp_u32, counter_u32, parent_id_u64,
  quaternion_f32[4], local_xzy_f32[3], speed_f32

MovementCorrectionS2C
  counter_u32, parent_id_u64,
  quaternion_f32[4], local_xzy_f32[3], speed_f32

CompactParentMovementS2C
  parent_id_u64, object_id_u64,
  local_xzy_i16_x8[3], counter_u32, speed_i8, heading_i8
```

Decode into a parent-local transform component, then enqueue an immutable movement sample. Object lookup, parent attachment, interpolation and rendering must not occur inside the packet reader. Preserve IDs as U64 and counters/timestamps as U32; never replace them with native pointers or `size_t`. Timestamp subtraction must deliberately use wrapping 32-bit arithmetic compatible with the server.

## Confidence

- Confirmed: both directional byte layouts, outer-scalar cursor behavior, registered client codec/RTTI, server validation stages, compact observer grammar and active producer paths.
- Strongly inferred: exact division of smoothing/interpolation responsibilities below the queued client controller boundary.
- Unknown: historical semantic label for every priority bit and the complete client interpolation algorithm after command queueing.

## Next bounded block

Close the adjacent character-selection callback or the high-reference ship view-3 baseline, selected by replacement-login criticality versus schema coverage.
