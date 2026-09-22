# Analysis checkpoint: ordinary world-space `DataTransform`

Date: 2026-09-15

## Result

The ordinary ground-object full transform path is now closed in both directions. `DataTransform` is ObjectController inner type `0x71`, carried by outer opcode `0x80CE5E46` at priority `0x1B`. It is the world-space counterpart to the previously closed parent-relative `0xF1` path.

This full-precision message is used for initial placement and authoritative correction. Routine accepted movement is normally fanned out through compact `LightUpdateTransformMessage`/`UpdateTransformMessage` traffic. A replacement client therefore needs both paths and must not treat `0x71` as the ordinary high-frequency observer update.

## Wire contract

The outer ObjectController record is U16 count (`5`), U32 opcode `0x80CE5E46`, U32 priority `0x1B`, S32 inner type `0x71`, U64 addressed object, then a directional F32/U32 scalar. The nested S->C payload is U32 movement counter; quaternion F32 x/y/z/w; position F32 x/z/y; F32 speed.

Client registration `0x00AD4763` installs serializer `0x00AD4810` and parser `0x00AD4860`; payload construction starts at `0x00AD4790`. The client payload object stores the counter at `+0x04`, a 48-byte transform at `+0x08`, and speed at `+0x38`. RTTI identifies `MessageQueueDataTransform`. The parser replaces a NaN quaternion with identity.

Like `0xF1`, `0x71` is directionally asymmetric at the outer/nested boundary. C->S leaves the outer scalar at the server callback cursor, where `Transform::parseDataTransform` consumes it as timestamp before the nested counter/transform/speed. S->C writes zero in that scalar; the client's outer parser consumes it and `0x00AD4860` starts at movement counter.

## Client dispatch and state effects

Generic controller consumer `0x00B4B000` recognizes inner `0x71`, RTTI-casts the queue object, and calls virtual slot `+0x24`.

### Local/player-controlled object

`PlayerCreatureController` slot `+0x24` is `0x0062C980`. It stores the incoming counter at controller `+0x58`, resolves the controlled creature, redirects through the relevant mounted/contained creature controller when necessary, requires an active object and non-negative counter, then calls `0x006E5760`/`0x006E4E50` for authoritative application. It subsequently notifies the controlled object through `0x012663E0` and completes counter bookkeeping through `0x0062C910`.

`0x006E4E50` checks the destination against the world/collision boundary. On success it changes cell/world association if required, copies the 12-dword transform into the object transform at `+0x50`, invokes the object's transform-change virtual, and brackets mutation with the engine transform-update guard. This is an immediate correction, not remote interpolation.

### Remote creature

`RemoteCreatureController` slot `+0x24` is `0x00637D30`. It drops a message whose counter is not newer than controller `+0x58`, stores a fresh counter, rejects the engine's invalid/sentinel transform test at `0x00AB9A20`, and copies the transform. Its special global-origin branch rebases/rotates through the global world-cell transform. It then submits world cell, transform and a zero auxiliary scalar to target setter `0x00637270`.

`0x00637270` retains/releases the target cell reference, copies the 48-byte target transform to controller `+0x64`, and stores the auxiliary scalar at `+0x94`. The full `DataTransform` path passes zero there; serialized speed at payload `+0x38` is not consumed by this remote-creature handler. Fine blending occurs downstream.

## Server acceptance path

`ZonePacketHandler` registers `DataTransformCallback` for `0x71`. It requires a live player, zone, ghost and validated position; rejects teleport state, invalid timing, non-finite coordinates, illegal posture/inertia, frozen/overloaded state, movement locks, invalid floor position, speed or path failures; resolves authoritative floor height; applies position/quaternion/speed; updates validated state and stamps; and normally broadcasts compact transforms. A qualifying static synchronization divergence produces a full `DataTransform` correction.

Active S->C producers also use it for selected-character placement, zone/scene placement, ship correction/relocation, dismount and incapacitated-player dragging.

## End-to-end mapping

| Message | Parser | Handler | Affected state | Confidence |
|---|---|---|---|---|
| ObjController `0x71` C->S | outer callback -> `Transform::parseDataTransform` | `DataTransformCallback::run` | authoritative creature position/orientation/speed, validation stamps, compact observer broadcasts | Confirmed |
| ObjController `0x71` S->C local | outer parser -> `0x00AD4860` | `0x00B4B000` -> `0x0062C980` -> `0x006E4E50` | immediate controlled-object/world-parent correction | Confirmed |
| ObjController `0x71` S->C remote | outer parser -> `0x00AD4860` | `0x00B4B000` -> `0x00637D30` -> `0x00637270` | fresh remote interpolation target transform/cell | Confirmed through target handoff; downstream blend Strong |

## Replacement-client requirements

- Preserve timestamp and nested U32 movement counter separately.
- Preserve quaternion x/y/z/w and position x/z/y wire order.
- Decode field-by-field into native x64 structures; never reuse 32-bit queue-object layout as a wire ABI.
- Separate full corrections from compact motion updates and parent-relative transforms.
- Keep world/cell ownership explicit and never encode native pointers in protocol state.
- Retain original asset/world collision data compatibility; the server remains authoritative.

## Remaining bounded unknowns

- Exact downstream interpolation coefficients after `0x00637270`.
- Exact semantic name of the origin-sentinel rebase branch in `0x00637D30`.
- Whether a live non-creature producer depends on nested speed; this remote-creature handler does not consume it.

These do not block correct parsing/dispatch or the networking proof of concept.
