# Stardust.exe Analysis Checkpoint: Creature View-6 Deltas

Date: 2026-09-15  
Target: `C:\Stardust-DEV\Stardust.exe`  
SHA-256: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`

## Result

This block closes the active server builder `CreatureObjectDeltaMessage6` against the executable's generic `DeltasMessage` route and the complete inherited `CREO:6` field registry. The message is not a distinct top-level opcode: it uses `DeltasMessage` (`0x12862153`), object tag `CREO` (`0x4352454F`) and view 6.

The end-to-end route is:

`DeltasMessage 0x12862153` -> dispatcher `0x0051C76D` -> parser `0x00ADB2D0` -> object lookup -> view dispatcher `0x00554E90` -> view-6 `AutoDeltaByteStream` at object `+0x144` -> indexed field decoder -> deferred callback commit `0x0118A970`.

## Outer delta grammar

The server writes:

1. U16 operand count `5`;
2. U32 opcode `0x12862153`;
3. U64 object ID;
4. U32 object tag `0x4352454F`;
5. U8 view `6`;
6. U32 payload byte length, patched at offset 23;
7. U16 update count, patched at offset 27;
8. repeated U16 field index followed by that field's codec.

The executable bounds the payload stream, but its legacy indexed dispatcher does not validate the U16 index before indexing the registered-field array. A replacement must validate the index and decode transactionally before committing any state.

## Complete view-6 schema

Inheritance produces eighteen registered fields:

| Index | Field | Wire codec | Principal effect |
|---:|---|---|---|
| 0 | common legacy value | U32 | retained common client-object state; semantics still unknown |
| 1 | defenders | delta vector | replace/mutate defender NetworkId collection |
| 2 | level | U16 | update displayed/simulation level |
| 3 | performance animation | ASCII16 | update animation skill/performance state |
| 4 | mood string | ASCII16 | publish animation-mood change |
| 5 | weapon | U64 NetworkId | change equipped/active weapon reference |
| 6 | group | U64 NetworkId | publish group change to ground and space group UI |
| 7 | group invitation | U64 inviter ID + U64 invite counter | replace the single pair-valued invitation field |
| 8 | guild | U32 | update guild membership identifier |
| 9 | look-at target | U64 NetworkId | update local targeting/controller state and publish target change |
| 10 | mood ID | U8 | update numeric mood state |
| 11 | performance start time | U32 | update performance timing state |
| 12 | performance type | S32/U32 bits | publish performance-type change |
| 13 | HAM | indexed S32 vector delta | mutate current health/action/mind attributes |
| 14 | maximum HAM | indexed S32 vector delta | mutate maximum attribute values |
| 15 | wearables metadata | specialized indexed vector delta | update customization/arrangement/object-ID/template-CRC metadata |
| 16 | alternate appearance | ASCII16 | route template-name change into appearance state |
| 17 | frozen | U8 boolean | update frozen state |

Constructor `0x0042ECF0` registers the sixteen creature-owned fields at indices 2-17 after the common and tangible fields. The order is directly visible in its calls to `client_view6_register_field_VA_005554C0`. The group-invitation pair is one field index despite containing two U64 values.

## Active Stardust producers

Current source actively emits scalar changes for level, performance animation/type/start time, mood string/ID, weapon, group, invitation pair, guild, target and alternate appearance. HAM, maximum-HAM, wearables and frozen use their specialized variable/container machinery rather than helper methods in this small builder header. Guild management, group lifecycle, target selection, entertainers, droid playback and login cleanup all exercise this view.

Wearables are metadata only. Actual visual attachment remains driven by `UpdateContainmentMessage`; the replacement must not instantiate or own scene nodes solely because index 15 changed.

## Replacement model

- Key schemas by `(object tag, view, field index)`, not by C++ packet-builder class.
- Decode U64 values as protocol `NetworkId` values, never pointers.
- Validate index `< 18`, payload length and all nested container counts before mutation.
- Stage all mutations, then commit atomically and coalesce each field's change notification once.
- Emit callbacks in deterministic schema-index order. Do not reproduce the legacy x86 pointer-order artifact.
- Resolve object references lazily because referenced weapons, group objects or wearables may not yet exist locally.
- Keep alternate-appearance asset resolution and containment-driven wearable attachment in the presentation/object layer, outside the wire decoder.

## Confidence

- **Confirmed:** outer delta layout and executable route; all eighteen field indices and codecs; server scalar builders; callback/coalescing mechanics; wearable containment boundary.
- **Strongly inferred:** high-level presentation labels for some scalar callbacks where original symbols survive only through RTTI.
- **Unknown:** semantic purpose of index 0 and any historical uses beyond current Stardust producers. These do not prevent a compatible opaque-U32 implementation.

## Progress

`CreatureObjectDeltaMessage6` moves to documented: **269 documented / 76 reachable gaps / 14 literal-opcode gaps**. Weighted whole-client coverage remains conservatively **78%**.
