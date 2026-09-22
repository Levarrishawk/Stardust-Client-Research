# Analysis checkpoint: `ClientMfdStatusUpdateMessage`

Date: 2026-09-15

## Result

`ClientMfdStatusUpdateMessage` (`0x2D2D6EE1`) is now closed from its active Stardust producer through the executable's generated message codec, central dispatch, per-object subscriber lookup and ground group-status UI behavior.

Despite the generic MFD name and its placement near space-related systems, the active Stardust use is a remote **ground group-member position/status update**. It supplies enough state for a group member's status mediator to show local distance/position when both players share a zone, or a localized planet/zone label when they do not.

## Wire contract

The server constructors write:

| Field | Type | Notes |
|---|---|---|
| operand count | U16 | `6` |
| opcode | U32 | `0x2D2D6EE1` |
| planet/zone | ASCII string | length-prefixed engine string |
| member object ID | U64 | group creature ID |
| world X | F32 | |
| world Z | F32 | wire axis order |
| world Y | F32 | wire axis order |

The zero-argument constructor writes only the six-byte header. The one-object and object-plus-explicit-planet constructors write one complete member record. `UpdateMember` can append additional records in the same field order, but there is no active call to it in this source tree and the recovered client message object models one record. Multi-record semantics are therefore dormant/unproven and must not be assumed.

## Executable message object and dispatch

The executable contains both `ClientMfdStatusUpdateMessage` and its RTTI. Generated constructor `0x013EEAE0` initializes the message with:

- ASCII planet/zone variable at object `+0x38` (data accessor `0x013EEF00`);
- network/object ID variable at `+0x48` (value accessor `0x013EEF10`);
- three-float position variable at `+0x58` (value accessor `0x013EEF20`).

Deserializer constructor `0x013EEC70` initializes those variables and consumes the incoming byte stream through the generated message-variable framework. Destructor `0x013EEDC0` confirms ownership of the three variables and their observer storage.

Central listener `0x00BA68C0` compares the incoming message type against the `ClientMfdStatusUpdateMessage` descriptor at `0x01609698`, obtains the payload stream, invokes `0x013EEC70`, and forwards the parsed object to `0x00BD0550`.

`0x00BD0550` indexes the registered status-subscriber collection by the message's U64 object ID. For every matching registration it invokes subscriber virtual slot `+0x50` with the parsed message. This is an ID-routed UI/state update rather than a global scene-object transform.

## Ground status/MFD consumer

RTTI and vtable `0x015EEF94` identify the principal consumer as `SwgCuiStatusGround`. Its message handler is `0x00BD0270`.

Confirmed behavior:

1. Copy or replace the most recent parsed message in mediator member `+0xF4`.
2. Compare the message's planet/zone string against the current scene's zone name.
3. If the zone matches:
   - pass the three-float world position to virtual slot `+0x58`;
   - when the mediator's distance mode is enabled and the local player exists, calculate Euclidean distance from the local player's world position;
   - format the suffix `\\#ffffff  (%.0fm)\\#.` and publish refreshed presentation text;
   - enable the associated local-position/radar presentation through the status widget.
4. If the zone differs:
   - resolve/localize the supplied planet/zone identity;
   - build a parenthesized remote-location label;
   - publish refreshed presentation text;
   - disable the local-position/radar presentation.
5. Notify the bound UI object that its status content changed.

The packet does not directly move the remote creature and does not update authoritative group membership. The member ID selects an already registered status mediator; the record augments presentation for an out-of-range group member.

## Stardust producer and reachability

The only active construction site is `CreatureObjectImplementation::updateGroupMFDPositions`. It:

- requires a group, zone, group list and close-object receiver set;
- constructs one message for the moving creature and its current zone;
- sends it to every other group member except members already present in the sender's close-object set;
- thereby targets group members who cannot receive ordinary nearby spatial updates.

Current source calls `updateGroupMFDPositions` from `notifySelfPositionUpdate` on a two-second cooldown. In the inspected source, that cooldown and call remain lexically inside the condition `zone != nullptr && zone->isGroundZone() && hasState(CreatureState::ONFIRE)`. No other caller was found. Therefore the current Stardust implementation appears to emit these updates only for burning ground creatures. This is **confirmed source control flow**, but whether it is an intentional policy or an indentation/regression defect is unknown. A replacement client should implement the message normally and should not encode the ONFIRE restriction as client behavior.

## End-to-end mapping

| Message/opcode | Parser | Handler | Affected subsystem/state | Confidence |
|---|---|---|---|---|
| `ClientMfdStatusUpdateMessage` / `0x2D2D6EE1` | central branch `0x00BA6DCA` -> `0x013EEC70` | ID router `0x00BD0550` -> `SwgCuiStatusGround::handleMfdUpdate` `0x00BD0270` | cached remote group-member zone/position; distance or remote-planet label; local-position indicator visibility | Confirmed |

## Replacement-client requirements

- Preserve U64 object IDs and x/z/y wire order; map them into native x64 objects without pointer casts.
- Treat the planet string as protocol/content identity, not a filesystem path.
- Route by stable object ID to group presentation state; tolerate an unknown/unregistered ID by ignoring it.
- Localize remote planet labels through original string-table assets.
- Calculate display distance from world-space positions only when zones match.
- Keep this presentation record separate from authoritative transforms and group-membership replication.

## Remaining bounded unknowns

- Intended server behavior of the current `ONFIRE` producer gate.
- Dormant semantics, if any, of the header-only constructor and `UpdateMember` record appending.
- Exact user preference that controls the distance-mode byte at mediator `+0xE0`.

None blocks correct single-record parsing or group-status presentation.
