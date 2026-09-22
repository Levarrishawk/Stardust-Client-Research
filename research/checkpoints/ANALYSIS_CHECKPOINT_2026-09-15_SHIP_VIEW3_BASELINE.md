# Analysis checkpoint: SHIP view-3 baseline and public deltas

Date: 2026-09-15  
Target: `C:\Stardust-DEV\Stardust.exe`  
Server correlation: current Stardust-3 source tree

## Scope and result

This block closes the current server's `ShipObjectMessage3` baseline and the
same view's generic `DeltasMessage` updates. The wire schema, client field
registration order, server mutation containers, delivery scope and the
presentation side effects of component-map changes are now sufficiently
specified for a replacement client.

This is a replication block, not a complete ship-flight block. SHIP views 1,
4 and 6, ship transforms/controls and space combat messages remain separate
work.

## Confirmed view contract

`ShipObjectMessage3` uses tag `SHIP` (`0x53484950`), view `3`, and declares 22
composed fields. Fields 0-10 are inherited from `TangibleObjectMessage3` and
fields 11-21 are ship-specific:

| Index | Wire type | Meaning | Client evidence |
|---:|---|---|---|
| 0 | F32 | complexity | inherited TANO codec |
| 1 | StringId | localized object name | inherited TANO codec |
| 2 | Unicode | launched/display name | inherited TANO codec |
| 3 | U32 | volume | inherited TANO codec |
| 4 | ASCII | customization string | inherited TANO codec |
| 5 | tangible visible-components container | visible component state | inherited TANO codec; see compatibility defect below |
| 6 | U32 | options bitmask | inherited TANO codec |
| 7 | U32 | use count | inherited TANO codec |
| 8 | U32 | condition damage | inherited TANO codec |
| 9 | U32 | maximum condition | inherited TANO codec |
| 10 | U8 | visibility | inherited TANO codec |
| 11 | F32 | chassis slip/slide factor | registered at client object `+0x3E8` |
| 12 | F32 | current chassis health | `+0x3FC` |
| 13 | F32 | maximum chassis health | `+0x410` |
| 14 | U32 | chassis/ship template CRC | `+0x424` |
| 15 | map U32 -> F32 | maximum armor by component slot | `+0x528` |
| 16 | map U32 -> F32 | current armor by component slot | `+0x560` |
| 17 | map U32 -> F32 | current hitpoints by component slot | `+0x6AC` |
| 18 | map U32 -> F32 | maximum hitpoints by component slot | `+0x6E4` |
| 19 | map U32 -> U32 | component option flags by slot | `+0x71C` |
| 20 | F32 | maximum front shield | `+0xA14` |
| 21 | F32 | maximum rear shield | `+0xA3C` |

The client offsets describe the legacy 32-bit ABI only. They are evidence for
field identity and order, not a layout requirement for the x64 client.

`ship_object_replication_ctor_VA_00536670` calls the TangibleObject
constructor and then registers exactly these eleven additional view-3 fields
through `0x005554A0`, in the order shown above. This independently confirms
the current server constructor rather than relying on the server source alone.

## Ship name policy

For a normal player ship, field 2 is the Unicode launched name. For a
`ShipAiAgent`, the server resolves `@space/ship_names:<agent-template-name>`
through `StringIdManager` and transmits the resulting Unicode string. A failed
AI cast produces an empty name. Therefore a replacement does not need to
repeat this particular localization lookup to decode the baseline; the server
has already materialized the display string.

## Packed component-map grammar

All five maps are current `DeltaAutoPackedMap` instances backed by
`DeltaVectorMap`.

Baseline/full-field payload:

```text
U32 entryCount
U32 updateCounter
repeat entryCount times:
    U8 command = 0 (ADD)
    U32 componentSlot
    value               # F32 for indices 15-18; U32 for index 19
```

View-3 field update:

```text
U16 fieldIndex          # in the surrounding DeltasMessage
U32 entryCount
U32 updateCounter
repeat entryCount times:
    U8 command = 0 (ADD)
    U32 componentSlot
    value
```

`DeltaAutoPackedMap::update` accepts local mutation commands `ADD=0`, `DROP=1`
and `SET=2`, but it deliberately does not pass the message into that operation.
After changing server state, it calls `insertToMessage` and emits the complete
current map, with every wire entry encoded as ADD. Therefore current SHIP
view-3 map deltas are field-replacement snapshots, not operation lists; DROP
and SET do not appear on the wire through these setters. This must not be
conflated with ordinary `DeltaVectorMap` incremental messages.

## Delta production and delivery

The scalar fields at 11-14 and 20-21 are `DeltaAutoVariable` values. They may
broadcast immediately, append to a caller-supplied `DeltaMessage`, or append
to a `ShipDeltaVector`. The map setters at 15-19 use the same supplied-message
or `ShipDeltaVector` pattern.

`ShipDeltaVector` creates a generic `DeltaMessage(objectID, 'SHIP', view)` and
coalesces updates by view. View 3 is public: `sendMessages` closes and clones
the accumulated message, broadcasts it to observers, then resets the vector.
Views 1 and 4 are private to the owner/ship members; view 6 is public. This is
delivery policy, not part of the field codec.

## Client-side resulting action

The five component maps maintain damage/repair state and per-slot option
flags. Four float-map objects share the same client container implementation;
the option map uses its U32-value specialization. They participate in the
broader component transaction, but the attachment/appearance refresh callbacks
belong specifically to the correlated SHIP:6[15] component-CRC map. A
replacement must not trigger attachment reconstruction merely because armor,
hitpoints or an option value changed.

## Compatibility defect inherited from TANO

SHIP view 3 inherits TANO field 5 unchanged. Prior constructor/serializer
proof established that Stardust.exe's registered visible-component container
expects an eight-byte entry while the current server's `AutoDeltaSet<int>`
emits a four-byte `int32` key. Non-empty field-5 state is therefore a current,
reachable compatibility defect for ships as well as tangibles. It is not an
unknown codec. Empty sets happen to decode without exposing the width
mismatch.

A replacement should initially preserve the server's actual four-byte wire
form, record a compatibility test fixture, and avoid copying the legacy
client's unexplained second word into its internal model.

## Evidence and confidence

### Confirmed by code

- Exact server field order and primitive/container types.
- Exact client count and registration order for the eleven ship additions.
- Client legacy object offsets and container specializations.
- Full packed-map framing and the replacement-snapshot behavior of map field updates.
- Public delivery of SHIP view 3 through `ShipDeltaVector`.
- Correlation of view-3 condition maps with the separate view-6 component-CRC
  appearance path.
- Inherited visible-component width incompatibility.

### Strongly inferred

- The client applies the five component maps transactionally through the same
  generic indexed-delta engine already proven for other object views. The
  shared dispatcher/commit route is confirmed; individual map callback labels
  are reconstructed rather than symbol-bearing originals.

### Unknown or intentionally deferred

- Semantic meaning of the unexplained second 32-bit word in the legacy
  visible-component entry.
- Full SHIP views 1, 4 and 6.
- Space-flight transform/control traffic and all component-stat meanings
  outside view 3.
- Exact event-listener consumers downstream of view-6
  `ComponentsChanged` publication.

## Replacement-client contract

1. Register SHIP view 3 as the eleven tangible fields plus the eleven fields
   above; never represent the legacy object offsets in a public ABI.
2. Decode each map field into staging state with count/allocation limits,
   command 0 entries and exact value widths, then replace that map atomically.
3. Commit only when the complete baseline/delta validates.
4. Preserve wire U32 slots, CRCs and counters regardless of x64 pointer size.
5. Reconcile component attachments only when the correlated view-6 component
   CRC map changes; notify presentation systems after commit, not while parsing.
6. Add a regression fixture for non-empty inherited field 5 and document the
   server-compatible four-byte form.

## Coverage effect

Once the staged character-selection and this SHIP report are published into
the private research workspace, the expected server-packet matrix is **276
documented / 69 reachable gaps / 14 literal-opcode gaps**. Weighted whole-
client research remains conservatively **78%** because this closes one
important replication class, not the remaining ship views or flight stack.
