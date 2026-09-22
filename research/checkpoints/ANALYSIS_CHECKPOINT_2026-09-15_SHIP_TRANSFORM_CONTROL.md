# Analysis checkpoint: ship transform control and prediction

Date: 2026-09-15  
Target: `C:\Stardust-DEV\Stardust.exe`  
Scope: active space-flight transform message, local/remote controller handling,
and Stardust server authority policy

## Outcome

The active ship-motion loop is closed in both directions. `0x76026FB9` is a
single 27-byte application message used by player clients and by the server for
observer updates. The executable contains both its serializer and parser. The
local `PlayerShipController` samples its predicted transform, packs it, and
sends it; the server performs lifecycle, timestamp and broad displacement
checks, accepts the client transform, and broadcasts an extrapolated update.
Remote clients feed that update into an interpolation controller. Corrections
received by the controlling client instead update its local prediction state.

The collision/correction message `0x763648D0` is also parsed by the client, but
its sole producer in the current Stardust callback is commented out. It is a
defined compatibility format, not currently reachable server behavior.

## Confirmed wire contract: `0x76026FB9`

Application header: U16 operand count `8`, U32 opcode `0x76026FB9`.

| Offset after opcode | Width | Field | Encoding |
|---:|---:|---|---|
| 0 | 2 | ship short/unique ID | U16 |
| 2 | 4 | orientation | signed I8 W, X, Y, Z; component / 127 |
| 6 | 6 | position | signed I16 X, Z, Y; component * 8000 / 32767 |
| 12 | 2 | speed | signed I16; value * 512 / 32767 |
| 14 | 2 | velocity direction | packed unit vector described below |
| 16 | 1 | yaw rate | signed I8; value * (pi/2) / 127 |
| 17 | 1 | pitch rate | same |
| 18 | 1 | roll rate | same |
| 19 | 4 | movement/synchronization stamp | U32 |

The packed direction uses sign bits X=`0x8000`, Y=`0x4000`, Z=`0x2000`, a
six-bit X magnitude in bits 6..11 and a six-bit Y magnitude in bits 0..5. The
third magnitude is `63-X-Y`; the reconstructed vector is normalized. The
sender supplies a non-negative magnitude even though the stored speed word is
signed.

Client serializer `0x00AD6370` clamps quaternion components to [-1,1], scales
them by 127, clamps position to approximately [-8000,8000], derives speed and
direction from the velocity vector, clamps angular rates to +/-pi/2, and adds
the stamp. Client parsing starts at `0x00AD6930`; helpers `0x00AD6A90` through
`0x00AD6D40` expose the ID, transform, velocity, three rates and stamp. The
client normalizes the decoded quaternion.

## Client outbound path

`PlayerShipController::sendTransform` (provisional name, `0x0061C560`) reads
the predicted/current transform object at controller `+0x14`, its velocity and
three angular rates, and a client synchronization time from `0x00462C50`. It
constructs the packet at `0x00AD6370`, then submits it through the central game
connection sender `0x00462670`.

A significant compatibility detail is confirmed: this path passes literal
zero as the U16 ship ID. The current server parser reads that field but uses it
only for debugging; it resolves the authoritative ship from the session
pilot's root object. A replacement should preserve zero until controlled tests
prove that a nonzero value is accepted across deployments.

The controller-side preparation routine `0x0061C560` is invoked from the local
flight update family. It suppresses transmission when the controlled object is
in the excluded lifecycle state at object `+0x38E`. The exact cadence policy
within the very large update function remains a bounded implementation detail;
the packet builder, state sources and send boundary are no longer unknown.

## Client inbound path

The `GroundScene` opcode branch at `0x0051D783` parses the message, looks up the
ship by its U16 short ID through `0x00538BC0`, obtains the object controller at
object `+0x2C`, and calls virtual slot `+0x34`.

- `RemoteShipController` (`0x00617F70`) rejects non-newer stamps using
  `0x00462CA0`, decodes transform, velocity and all three angular rates, and
  advances its interpolation target/timing state.
- `PlayerShipController` (`0x0061B230`) applies a fresh authoritative transform
  and velocity to local prediction/correction state. It does not use the three
  received angular-rate fields semantically.
- The main local update `0x0061B2D0` maintains transform/prediction objects at
  controller `+0x14/+0x18`; the subpath near `0x0061BFC0` copies position,
  velocity and rates, advances interpolation, and writes the result back to the
  controlled ship.

RTTI corroborates the hierarchy: `ShipController`, `PlayerShipController`, and
`RemoteShipController` have type descriptors at `0x01877DE0`, `0x0186F4AC`,
and `0x01877E00` respectively.

## Stardust server acceptance and broadcast

`ShipUpdateTransformCallback` is registered for `0x76026FB9`. It resolves the
pilot from the session and rejects invalid lifecycle states: missing pilot or
ghost, frozen/teleporting/loading pilot, missing or non-ship root, unlaunched
ship, and docking. It does not trust the packet to select the ship.

Timestamp policy is `delta = incomingStamp - lastMovementStamp`:

- less than -10000 ms: bounce/correct;
- less than 200 ms: silently discard;
- greater than 800 ms: permits a forced stationary refresh;
- otherwise: process normally.

Position components are checked against roughly +/-8000. The displacement from
the authoritative position must be no more than
`max(actualMaximumSpeed*10, 200)`. This bound is not multiplied by elapsed
time. Accepted ordinary updates do not independently cap reported speed,
validate angular rates, validate the U16 ship ID, or re-normalize the
quaternion. The policy is therefore client-authoritative inside a broad
positional envelope, not server simulation authority.

On movement, the server applies the incoming position but broadcasts one-step
linear extrapolation: `incoming + (incoming - previousServerPosition)`. On an
orientation-only or forced refresh it broadcasts the current position. Each
recipient receives its own synchronization stamp. AI-controlled ships use the
same message type through `ShipObjectTransform::broadcastTransform`.

## Collision/correction message

Opcode `0x763648D0` contains U64 object ID; four F32 direction/orientation
values in the server's X,Z,Y,W write order; three F32 position values multiplied
by `4.0958748`; three zero F32 values; and U32 movement counter. Client parser
`0x00AD6190` stores the ID, transform, extra vector and counter. The branch at
`0x0051DAC8` requires that the object be the local controlled ship and invokes
`0x0061B120` to reconcile its transform.

The server collision test and message construction in the active transform
callback are commented out. This path is dormant in this Stardust revision.

## Replacement-client implications

1. Keep every wire field at its original width; none becomes pointer-sized in
   an x64 process.
2. Implement packing/unpacking as explicit byte operations, not packed native
   structs. Preserve X/Z/Y position ordering.
3. Normalize received quaternions and handle the packed-vector zero case
   deterministically.
4. Maintain a wrap-aware U32 stamp comparator. Do not compare stamps as ordinary
   signed 64-bit application time.
5. Separate local prediction/reconciliation from remote interpolation even if
   both consume the same wire packet.
6. Initially preserve the client's zero ship-ID behavior. Treat session/root
   ownership as a server contract rather than inventing a stronger identifier
   contract.
7. Treat the server's existing permissive validation as observed compatibility,
   not as the security model for a future redesign.

## Confidence and residual unknowns

| Finding | Confidence |
|---|---|
| Exact normal transform layout and quantization | Confirmed |
| Client serializer/parser and central send/dispatch routes | Confirmed |
| Local versus remote controller behavior | Confirmed at subsystem level |
| Server validation, extrapolation and recipient-stamp policy | Confirmed |
| Collision format and client correction route | Confirmed |
| Collision message reachability in current Stardust | Confirmed dormant |
| Exact local send cadence and every smoothing coefficient | Unknown/bounded |

The remaining cadence and smoothing constants affect feel and tuning, not the
minimum interoperable packet implementation. They remain on the function
ledger for a later flight-dynamics fidelity pass.
