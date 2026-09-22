# Analysis checkpoint: nebula lightning

Date: 2026-09-15  
Target: `C:\Stardust-DEV\Stardust.exe`

## Outcome

`CreateNebulaLightningMessage` is closed from Stardust generation through the
client's timed environmental-effect queue. Its opcode is `0x65F27987`. The
message identifies a configured nebula row and schedules a lightning segment
between two world-space points on each recipient's synchronized clock. The
client resolves the appearance and sounds from the original per-zone nebula
datatable; those resources are not carried in the packet.

The server separately schedules authoritative shield damage and sends the
ordinary `OnShipHit`/SHIP delta traffic. This lightning message itself is
presentation-only.

## Wire format

Application header: U16 operand count `0x0B`, U32 opcode `0x65F27987`.

| Offset after opcode | Type | Field |
|---:|---|---|
| 0 | U16 | monotonically incremented lightning ID/count |
| 2 | S32 | nebula ID (server table row + 1) |
| 6 | S32 | start time in recipient synchronization domain |
| 10 | S32 | end time in recipient synchronization domain |
| 14 | 3 x F32 | world-space start point X,Y,Z |
| 26 | 3 x F32 | world-space end point X,Y,Z |

Payload width after opcode is 38 bytes. The client stores it in an aligned
40-byte `NebulaLightningData` record; the two padding bytes after the U16 are
an internal layout detail and are not present on the wire.

## Client route

The GroundScene branch at `0x0051D227` identifies the message by its descriptor,
parses it through `0x00AD2D80`, obtains the aligned data through `0x00AD2E80`,
and calls `NebulaManagerClient::enqueueLightning` at `0x0052F640`.

The enqueue routine ignores the message when `lightningEnabled` is false,
resolves the nebula ID, rejects invalid IDs or definitions without a lightning
appearance, and copies the aligned record into a timed queue entry retaining
the resolved nebula definition.

The manager update beginning at `0x0052F820` compares synchronized client time
with start/end. Before start, the record remains queued. During the active
window it lazily creates the configured `LightningAppearance`, constructs
multiple bolt primitives between the endpoints, sizes the effect from segment
extent, positions it around the midpoint, and selects configured sounds using
camera/listener relationship tests. After end it stops/releases the effect;
stale entries are removed after a 10-second grace period.

## Server generation and authority boundary

`NebulaAreaImplementation::createNewLightning` increments the event count,
chooses endpoints inside the volume, selects a delayed start (currently 1000
ms) and randomized duration, then broadcasts. The broadcaster adds each
recipient's U32 sync stamp to both relative times. Different recipients can
therefore receive different numeric timestamps for the same intended event.

Collision and damage are server-side. Ships intersecting the bolt receive
scheduled shield damage followed by `OnShipHit` and replicated shield changes.
The client must not infer damage from proximity to the rendered bolt.

## Original asset dependency

Both client and Stardust derive behavior from
`datatables/space/nebula/<space-zone>.iff`. Relevant columns include row
identity/position/radius/density, ambient sound, lightning frequency/duration,
server damage range, lightning appearance, one-shot/looping sounds, and client
and server hit effects. The packet supplies only row ID, schedule and endpoints.

## Replacement-client requirements

1. Parse the unaligned 38-byte payload explicitly; do not map it directly onto
   an x64/native structure.
2. Preserve signed 32-bit synchronized timestamps and wrap-aware comparisons.
3. Resolve nebula ID against the current space-zone nebula table.
4. Queue early events and release effects after end time or scene change.
5. Keep presentation independent from server-authoritative collision/damage.
6. A headless client may validate/log the event without loading visuals.

## Confidence and bounded unknowns

| Finding | Confidence |
|---|---|
| Opcode and exact wire field order/width | Confirmed |
| Scene parser and nebula-manager enqueue route | Confirmed |
| Invalid-ID/missing-appearance behavior | Confirmed |
| Recipient-specific synchronized scheduling | Confirmed |
| Timed appearance/audio lifecycle | Confirmed at subsystem level |
| Original per-zone nebula table dependency | Confirmed |
| Every bolt-randomization constant and audio threshold | Unknown/bounded |

Residual constants are presentation-fidelity details, not protocol or
asset-architecture blockers.
