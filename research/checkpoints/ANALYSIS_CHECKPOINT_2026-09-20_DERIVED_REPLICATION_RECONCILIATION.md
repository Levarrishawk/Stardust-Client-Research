# Analysis Checkpoint: Derived Replication Reconciliation

Seven remaining source-class rows are resolved against the executable's already recovered constructor registries.

- `InstallationObjectDeltaMessage3`: `INSO:3` indices 11-13 are active U8, surplus-power F32, and base-rate F32. The current delta's active index 11 is correct.
- `InstallationObjectDeltaMessage6`: Installation adds no view-6 fields; index 1 is the inherited Tangible defender vector. Its list commands are decoded by the inherited container.
- `IntangibleObjectMessage3`: `ITNO:3` is common fields 0-3 plus U32 status at index 4.
- `IntangibleObjectDeltaMessage3`: name index 2, data-size/volume index 3, and status index 4 align with that registry (the server float helper for index 3 is tolerated as the source's historical representation).
- `IntangibleObjectMessage6`: the executable registers only the inherited common U32 in view 6. The server's `0x6E` satisfies it; appended identifier/description strings have no registered consumer in this build and must be treated as bounded trailing compatibility data.
- `IntangibleObjectDeltaMessage6`: contains no active update method; its commented identifier setter is correctly not emitted because the executable has no such field.
- `MissionObjectMessage6`: `ClientMissionObject` adds no view-6 fields. Server constant `0x79` populates the inherited common U32; commented long values are not part of the active packet.

These results reinforce a replacement rule: dispatch replicated fields by the executable-confirmed tag/view registry and payload bounds, not by the number of `insert*()` calls or declared operand count in server convenience classes.

## Confidence

Field registry composition, active indices, and absence of derived view-6 fields: **Confirmed**. Historical semantic name of the inherited common view-6 U32 remains **Unknown**.

