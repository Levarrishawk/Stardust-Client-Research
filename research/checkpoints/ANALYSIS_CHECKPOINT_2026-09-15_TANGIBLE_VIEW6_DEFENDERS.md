# TangibleObject View-6 Defender Replication

Date: 2026-09-15  
Scope: `Stardust.exe`, current Stardust server source, inherited `TANO:6` baseline/delta contract

## Result

`TangibleObjectMessage6` and `TangibleObjectDeltaMessage6` are now closed at the wire and state boundary. View 6 contains two inherited fields: an unresolved common U32 at index 0 and an ordered defender `NetworkId` vector at index 1. The delta subclass contains no convenience methods because all active producers write the inherited defender vector directly after `startUpdate(1)`.

## Schema

| Index | Baseline value | Wire encoding | Delta use | Confidence |
|---:|---|---|---|---|
| 0 | constant `0x76` for ordinary `TANO` | U32 | no direct producer found | Width/order confirmed; semantic label unknown |
| 1 | defender list | U32 count, U32 update counter, repeated U64 object IDs | `DeltaVector` command stream | Confirmed |

`CreatureObjectMessage6` changes the inherited constant to `0x3D`; derived tags can also extend the view. These constants must remain protocol data rather than native flags inferred from pointer-sized state.

## Defender delta grammar

The server uses `DeltaVector<ManagedReference<SceneObject*>>`. `ManagedReference::toBinaryStream` writes `getObjectID()` as U64. The incremental envelope is U32 update-count followed by U32 new update-counter and repeated commands:

| Command | Server method | Payload |
|---:|---|---|
| 0 | remove | U16 index |
| 1 | add | U16 index, U64 defender object ID |
| 2 | set | U16 index, U64 defender object ID |
| 4 | remove all | no item payload |

This is the vector grammar, not the set grammar used by the defective visible-component field. Client object offset `+0x2F0` is registered into view 6 by `0x005554C0`; the `0x0063EE10` constructor keeps it separate from view-3 offset `+0x328`.

## Active state transitions

- `addDefender` emits command 1, broadcasts the delta, sets combat state, raises `DEFENDERADDED`, and may synchronize PvP status.
- `removeDefender` emits command 0 or command 4 for the last defender, raises `DEFENDERDROPPED`, synchronizes PvP status and clears combat state when empty.
- `removeDefenders` emits command 4 after observer notifications and preserves the relevant PvP-status recipients.
- `setDefender` can reorder the vector by emitting two command-2 operations under one update count: it moves the requested defender to index zero and moves the prior first element to the old slot.

Index zero is therefore semantically privileged as the primary/current defender, while remaining entries represent additional combat opponents. Ordering must be preserved; a set is not an adequate replacement data structure.

## Client route

```text
DeltasMessage 0x12862153, tag/view from concrete object
  -> 0x0051C76D
  -> 0x00ADB2D0
  -> object lookup / 0x00554E90 view dispatch
  -> view-6 registry
  -> 0x0118AA00 indexed decoder
  -> 0x0118A970 atomic commit/callback boundary
```

The defender vector stores U64 wire IDs. Resolution to live objects belongs after replication commit and must tolerate an object not yet instantiated or already destroyed.

## Replacement requirements

1. Keep the vector update counter as U32 and indices as U16.
2. Keep defender IDs as U64 regardless of native pointer size.
3. Preserve command ordering inside a multi-operation delta.
4. Do not store raw object pointers in protocol state; use stable object handles/IDs and lifecycle-safe resolution.
5. Apply combat/PvP/UI reactions after the entire delta commits.
6. Preserve the inherited index-0 constant even though its exact semantic name remains unknown.

## Confidence

- Confirmed: field count/order, all wire widths, command grammar, active producers, primary-defender ordering behavior, client view separation and generic route.
- Unknown: historical semantic name of view-6 index 0.

## Next block

Analyze the highest-reference remaining parent-relative server movement packet, including parent/cell identity, transform packing, movement sequencing and the client world-object update path.
