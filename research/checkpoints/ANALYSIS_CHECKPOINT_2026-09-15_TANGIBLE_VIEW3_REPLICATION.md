# TangibleObject View-3 Baseline and Delta Replication

Date: 2026-09-15  
Scope: `Stardust.exe`, current Stardust server source, `TANO:3` snapshot/delta contract  
Conclusion discipline: executable pseudocode is reconstructed; names below are provisional unless supplied by RTTI/server source.

## Result — corrected 2026-09-21

This block closes the paired `TangibleObjectMessage3` and `TangibleObjectDeltaMessage3` rows. The baseline contains eleven fields and the delta builder actively updates seven of them. **Correction:** the later eight-byte association was false. `ANALYSIS_CHECKPOINT_2026-09-21_TANGIBLE_COMPONENT_WIDTH_CORRECTION.md` proves index 5 is a four-byte `AutoDeltaSet<int>` matching the server; the eight-byte codec belongs to a separate `AutoDeltaVector<CachedNetworkId>` in another view.

## Generic delivery route

`DeltasMessage` opcode `0x12862153` follows:

```text
0x0051C76D message route
  -> 0x00ADB2D0 delta envelope parser
  -> object lookup
  -> 0x00554E90 object/view dispatcher
  -> object +0x124 view-3 registry
  -> 0x0118AA00 indexed field decoder
  -> 0x0118A970 callback/commit boundary
```

The baseline is the corresponding tag `TANO`, view 3 snapshot and seeds the same registry before later deltas are accepted.

## Exact server baseline

| Index | Server value | Wire encoding | Client interpretation | Confidence |
|---:|---|---|---|---|
| 0 | complexity | F32 | complexity scalar; change callback | Confirmed |
| 1 | object `StringId` | ASCII file, U32 zero, ASCII key | localized object name | Confirmed |
| 2 | custom name | Unicode | display name; server may append the colored admin tag | Confirmed |
| 3 | volume | U32 | container volume | Confirmed |
| 4 | customization string | ASCII | appearance customization data; invokes appearance-data reaction | Confirmed |
| 5 | visible components | `AutoDeltaSet<int>` snapshot; S32 values | visible-component integer set | Confirmed client/server match |
| 6 | options bitmask | U32 | tangible condition/options state | Confirmed |
| 7 | use count | U32 | item count/countdown value | Confirmed |
| 8 | condition damage | U32 | damage taken/current condition damage | Confirmed |
| 9 | maximum condition | U32 | maximum hit points/condition | Confirmed |
| 10 | object visible | U8 | presentation visibility | Confirmed |

The baseline's declared operand count `0x0B` agrees with its eleven logical entries. Native C++ structure layout is not part of this contract.

## Active delta producers

| Delta index | Builder/producer | Resulting client state | Confidence |
|---:|---|---|---|
| 0 | `updateComplexity()`; crafting paths | complexity | Confirmed |
| 1 | `updateObjectName()` | localized-name identifier | Confirmed |
| 2 | `updateCustomName()`; PlayerManager/admin paths | custom display name | Confirmed |
| 4 | `updateCustomizationString()`; prototype/final crafting and object mutation | appearance customization | Confirmed |
| 5 | `addVisibleComponent`, `removeVisibleComponent`, `removeAllVisibleComponents` | visible-component collection | Server producer confirmed; client value width unresolved |
| 6 | `updateOptionsBitmask()` | options/condition flags | Confirmed |
| 7 | `updateCountdownTimer()` and use-count mutation | use count/countdown | Confirmed |
| 8 | `updateConditionDamage()` | condition damage | Confirmed |
| 9 | `updateMaxCondition()` | maximum condition | Confirmed |

There are no direct methods in this builder for volume (3) or object-visible (10). That does not make them absent from the baseline contract.

Visible-component state is reachable, not dead declaration: crafting selects component-map values and calls `addVisibleComponent(..., false)` before the object baseline is sent; the administrative object command also hashes component names and populates the collection. Notify-client variants create a view-3 delta at index 5 and broadcast it.

## Client codec evidence

The TangibleObject constructor at `0x0063EE10` appends seven tangible-owned fields after the four common `ClientObject` fields. Stable provisional codec names assigned in this block are:

- `0x00641030` `tangible_customization_snapshot_decode`
- `0x00641180` `tangible_customization_delta_decode`
- `0x006421A0` `tangible_component_container_snapshot_decode`
- `0x006423E0` `tangible_component_container_delta_decode`

The customization decoder supports the client's extended ASCII length form (U16 length, with `0xFFFF` followed by U32) and calls the owning object's appearance-data reaction only when the value changes.

At `0x006423E0`, the candidate component delta decoder first reads two U32 header values, then parses command records. It reads a U8 command; command 4 has no following U16, other commands read a U16 position/index. Command 3 reads a U16 count followed by that many eight-byte values. Other value-bearing operations also read eight bytes. The runtime's committed entry stride is sixteen bytes because entries include local bookkeeping in addition to the eight-byte replicated value.

### Corrected index-5 compatibility result

Current server source exposes the collection as integer component IDs. Baseline insertion delegates to `AutoDeltaSet<int>::insertToMessage`; that template writes count U32, update counter U32 and then `TypeInfo<int32>::toBinaryStream` for every key. Client RTTI at `0x015E05E0` independently identifies `AutoDeltaSet<int>`, and its actual codecs `0x00642B80`, `0x00642F00`, `0x00642BE0`, and `0x00643050` write/read four bytes per key.

The constructor registers `+0x328` through `0x005554A0` into the view registry at object `+0x124`. The eight-byte container at `+0x2F0` is instead RTTI `AutoDeltaVector<CachedNetworkId>` and is registered through `0x005554C0` into the distinct registry at object `+0x144`. It was the source of the earlier mistaken codec association.

Nonempty visible-component baselines and deltas are therefore structurally compatible. No special malformed-data decoder or server correction is required for this field.

## Replacement-client rule

Represent `TANO:3` as an explicit fixed-width wire schema and translate it into native x64 state. Keep U64 network object IDs where the wire requires them, but keep all U32 fields above at 32 bits. Treat callbacks as post-commit subsystem notifications: replication mutation must not directly own appearance/UI behavior. Support inherited tags (`CREO`, `INSO`, weapons and other Tangible derivatives) through schema composition rather than native C++ inheritance layouts.

## Confidence summary

- Confirmed: eleven-field baseline order; generic delta envelope/dispatch; indices 0-4 and 6-10 widths/semantics; active producer locations; customization callback behavior.
- Confirmed: constructor registration at tangible index 5 corresponds to four-byte `AutoDeltaSet<int>` and matches current server serialization.
- No second visible-component U32 exists; the earlier unknown is retired.

## Next bounded block

Recover the semantic meaning of the second index-5 U32 when an asset/crafting path exposes it, but do not block adjacent schema closure on that label. Proceed to `TANO:6` defenders and other high-reference baseline/delta pairs.
