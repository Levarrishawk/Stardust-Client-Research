# Owned localized string table contract

The engine-neutral [reader](../../SWG-Godot-Client/poc/Stardust.Core/Assets/LocalizedStringTable.cs) implements a bounded STF variant 1 table. The [September localization investigation](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-21_LOCALIZATION_STF_RUNTIME.md) establishes runtime context; [Round24](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R24_STF_TABLE.md) verifies the byte layout against two original tables. This does not implement locale selection, fallback, caching or name presentation.

## Verified layout

All integers and UTF-16 code units are little-endian. Header offsets are measured from the first byte.

| Offset | Width | Retained field |
|---|---|---|
| 0 | U16 | Magic `0xABCD` |
| 2 | U16 | `ReservedRaw` |
| 4 | U8 | Variant, accepted value 1 |
| 5 | U32 | `MaxNextIdentityRaw` |
| 9 | U32 | Record count |

After the 13-byte header, the value pass contains exactly count records: U32 ID, U32 `ValueCrcRaw`, U32 UTF-16 **unit** count, then that many U16 units. The name pass then contains exactly count records: U32 ID, U32 **byte** count, then exact narrow key bytes. These are length-delimited, with no terminator consumed. ID joins the passes; their ordering need not match. Records retain value-pass order; `KeyPassIds` retains name-pass order.

**Confirmed for the two selected variant 1 payloads:** header and extents, exact ID relation and exhausted input. Reserved values were zero; raw max-next values were 41 and 58 for 40 and 57 records. Those observations do not establish allocator, checksum or reserved-field semantics. The implementation retains all three raw fields without validating or interpreting their meaning. Variant 0 has a distinct executable reader in prior research, but its exact schema is unresolved here: `Load` rejects it.

## API and ownership

`LocalizedStringTable.Load(ReadOnlySpan<byte>, LocalizedStringTableLimits?)` returns owned immutable records and read-only `ById` / `ByKey` maps sharing canonical record objects. `TryGetByKey` compares exact binary bytes, including case, high bytes and embedded NUL. `LocalizedStringKey` copies its constructor input; copy accessors return independent arrays. `Utf16Units` preserves unpaired surrogates and all raw units. Clearing the source or returned copies cannot alter a table.

`GetDisplayKey()` escapes non-printable/non-ASCII bytes as `\xXX` and doubles backslashes. It does not guess a narrow encoding. `GetDisplayText()` preserves valid surrogate pairs, replaces isolated surrogates with U+FFFD, escapes controls as `\uXXXX`, and doubles backslashes. Display output is separate from lookup identity and is not an HTML escaping API. Locale resolution and presentation policy remain caller work.

## Bounds and failure policy

Default limits are 16,777,216 source bytes, 65,536 records, 65,536 units per value, 4,096 bytes per key, and 4,194,304 aggregate units / aggregate key bytes. Configurable hard ceilings are 67,108,864 bytes, 1,000,000 records or units per value, 65,536 bytes per key, and 16,777,216 for each aggregate. Source budget must be at least 13; other limits may be zero. Invalid settings throw `ArgumentOutOfRangeException`.

Malformed input throws `InvalidDataException`. Count is checked against its budget and the minimum 20 bytes per joined record before allocating record collections. Each length and aggregate is checked before allocating its field. Duplicate value IDs, duplicate name IDs, orphan IDs, duplicate exact keys, truncation, unsupported magic/variant and trailing bytes reject the entire load. No result is published until both passes succeed. Zero/full-U32 IDs and empty values/keys are permitted when the relation is unique. These are explicit modern acceptance/rejection policies; identical legacy malformed-input behavior is not claimed.

No callbacks, archive handles or mutable input buffers survive the call. Independent loads/lookups require no shared mutable state. A standalone raw-key constructor/lookup also enforces the 65,536-byte hard ceiling.

## Verification and remaining boundary

The [native scenarios](../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/LocalizedStringTableScenarios.cs) and [independent Python fixtures/oracle](../../SWG-Godot-Client/poc/round24-tests/Assets/verify_stf.py) cover 102 synthetic cases (4 positive, 98 malformed), six budget failures, invalid settings, ownership, exact raw identity, pass permutation and concurrent independent lookups. They compare all 97 records of the two pinned original tables, including raw units/keys/CRC, header, pass order and display conversion. Original-derived detailed output remains ignored and private. This is targeted format validation, not corpus coverage or a runtime localization manager.

The next question is how the accepted runtime manager selects a locale and handles missing table/key references, then how object names consume that result. Cache ownership and fallback must be specified before integrating this reader into name presentation.
