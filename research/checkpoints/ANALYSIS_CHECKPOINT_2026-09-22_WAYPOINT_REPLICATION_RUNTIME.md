# Waypoint Replication and Presentation Runtime

The waypoint system is closed from `PLAY:8` replication through concrete
client presentation-object lifetime and UI notification. It is not a separate
packet service.

## Wire value

Each `NetworkId -> Waypoint` map value is serialized as U32 cell ID, F32 X/Z/Y,
U64 auxiliary/unknown ID, U32 planet CRC, Unicode32 custom name, U64 waypoint
object ID, U8 color, and U8 active. The baseline map uses count/update-counter
framing. Delta decoder `0x0066A1D0` uses the established map command and
counter-reconciliation grammar. Current Stardust serializes this exact value in
`WaypointObject::insertToMessage`.

## Client lifecycle

`PlayerObject` registers a primary waypoint map in `PLAY:8` and secondary map
in `PLAY:9`. Setup `0x0065C3B0` installs insert/remove/update callbacks.

- Primary insert `0x0065C640` resolves a `ClientWaypointObject`, applies the
  value through `0x00525B20`, then publishes `WaypointsChanged`.
- Primary removal `0x0065C920` removes/releases the presentation object, then
  publishes the aggregate event.
- Primary replacement `0x0065CC40` retires the prior presentation object and
  publishes after successful resolution.
- Secondary callbacks `0x0065CF50`, `0x0065D010`, and `0x0065D110` operate only
  when the key is absent from the primary map. The visible model is a union
  with `PLAY:8` precedence.

Constructor `0x005252B0` registers seven derived view-3 fields and callbacks.
Activation callback `0x00526C00` updates a global active-waypoint collection;
location `0x00526E20`, color `0x005270D0`, and presentation-string
`0x00527380` changes update narrower rendering/UI state. The aggregate player
event feeds waypoint-monitor and space-zone-map UI.

## Original asset boundary

Planet CRC resolution, `object/waypoint/shared_waypoint.iff`, colors/styles,
icons, map pages, and localized names are original-data dependencies. The wire
value does not contain enough presentation data to replace them.

## Replacement rules

- Decode an owned fixed-width value keyed by U64 network ID.
- Project the primary/fallback union into ID-owned presentation entities.
- Make mutations idempotent and avoid raw references across notifications.
- Separate world marker, radar/map marker, and monitor row as consumers.
- Preserve the auxiliary U64 without invented semantics.

Wire order, lifecycle, active collection, event, and precedence are
**Confirmed by code**. The secondary map's historical product purpose and the
auxiliary U64 meaning remain **Unknown but bounded**.

