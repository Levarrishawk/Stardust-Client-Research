# Creature view6 owned codec contract

## Scope and API

[CreatureView6Codec](../../SWG-Godot-Client/poc/Stardust.Core/Protocol/CreatureView6Codec.cs) provides `DecodeBaseline`, `DecodeDelta`, `EncodeBaseline`, `EncodeDelta`, `EncodeBaselineEnvelope` and `EncodeDeltaEnvelope`. Decode overloads accept whole application bytes or an owned `ReplicationEnvelope`. It accepts only CREO (`4352454F`), view6 and the matching generic Baselines/Deltas opcode.

`CreatureView6Baseline` owns the complete eighteen-field value. `CreatureView6Vector<T>` freezes bounded collections used for raw U64 defenders, S32 HAM/max-HAM and sealed `CreatureView6Wearable` entries. Strings use existing immutable `ReplicationByteString`: owned raw bytes, copied inspection access and retained `UsesExtendedLength`. Immutable string/vector/entry references may be shared safely between models. `CreatureView6Delta` freezes an ordered collection of sealed `CreatureView6Update` values, constructed with `Scalar`, `String` or `Pair`. Field12 exposes its signed `PerformanceType` interpretation while retaining the raw bits.

This component decodes and encodes values. It performs no state application, callbacks, target resolution, incarnation selection or world integration. The [prefix prerequisite](CREATURE_VIEW6_PREFIX_CONTRACT.md) supplies direct executable evidence; the [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R32_CREO6_CODEC.md) supplies reproduction and results.

## Complete baseline grammar

All multibyte values use little endian. The application envelope is the accepted23-byte header, followed by U16 declared count and these fields without per-field indices:

| Index | Body retained |
|---:|---|
| 0 | passive U32 |
| 1 | U32 count, U32 counter, count raw U64 defender IDs |
| 2 | raw16-bit level word |
| 3 | raw performance-animation byte string |
| 4 | raw mood byte string |
| 5 | U64 weapon ID |
| 6 | U64 group ID |
| 7 | U64 inviter ID followed by U64 second invitation value |
| 8 | raw32-bit guild word |
| 9 | U64 target ID |
| 10 | raw U8 mood ID |
| 11 | raw32-bit performance-start word |
| 12 | S32 performance type |
| 13 | U32 count, U32 counter, count S32 HAM values |
| 14 | U32 count, U32 counter, count S32 maximum-HAM values |
| 15 | U32 count, U32 counter, count WearableEntry bodies |
| 16 | raw alternate-appearance byte string |
| 17 | raw U8 frozen storage |

Every byte string is U16 length followed by raw bytes, or FFFF followed by U32 length and raw bytes. Empty/extended-small spellings, embedded zero and arbitrary high bytes round-trip exactly; no host text normalization or terminator is added. Lengths65535 and above require extended spelling. Baseline metadata0,22 and65535 all retain their exact value and still consume eighteen fields. Missing fields or trailing bytes reject under explicit modern strict extent policy.

WearableEntry is raw customization string, U32 arrangement, U64 object ID and U32 template CRC in that order. Direct reader `00B94C60` calls string reader `00439F60`, then reads4/8/4 bytes; baseline parser `00441740` invokes that element reader. Round32 reran the pinned round31 verifier before implementing this body. The legacy20h memory stride is not a wire length or replacement ABI.

IDs preserve all64 bits, zeros, duplicates and order. All nested counters preserve32 bits. Level/guild/start words preserve their complete bit patterns rather than imposing server-derived signedness. Frozen preserves any byte, includingFF; the codec neither validates nor converts it to a host bool. Client level RTTI is a short specialization while current server insertion uses uint16. The invitation pair's second value is named invite counter by the server but typed NetworkId in client RTTI; no arithmetic meaning is introduced here.

## Ordered supported deltas

A delta repeats U16 index followed by the corresponding scalar/string/pair body:

- U32 at0,8,11 and12; index12 retains S32 interpretation.
- U16 at2; U64 at5,6 and9; U8 at10 and17.
- Raw short/extended byte strings at3,4 and16.
- Two consecutive U64 values at7, as one field.

Repeated fields and packet order are preserved without coalescing. Declared count is raw metadata and does not control iteration. A nonempty delta ends at payload exhaustion. A size-adjusted prefix ending at a complete field boundary can therefore be a valid delta, even with the original declared count; a partial discriminator/body rejects.

Indices1 and13–15 are explicitly unsupported in this union, as are all other indices. Field1 omission is a bounded scope choice: the accepted TANO6 grammar/state remains separately implemented and must be composed through an explicit family/schema adapter. Fields13–15 need specialization-specific delta/state work. Unsupported indices reject even after a valid earlier update; no partial plan escapes.

## Bounds and ownership

`CreatureView6Limits` bounds payload bytes, actual field count, per-string bytes, aggregate string bytes, per-vector elements, aggregate elements, logical decoded bytes and work. Defaults are1MiB payload,256 fields,65536 bytes per string,262144 aggregate string bytes,4096 elements per vector,16384 aggregate elements,2MiB decoded and1048576 work units. Hard ceilings are8MiB/4096/1MiB/4MiB/65536/262144/16MiB/16777216 respectively. Invalid policies reject before parsing. Public vector/update model collection constructors separately bound enumeration at65536/4096 entries.

Payload bytes include the two-byte declared count and exclude the outer23-byte header. Logical decoded bytes are `opaqueFieldBytes + 10`, charging the object ID, metadata and complete raw field representation, including nested count/counter and string-length spelling. Work is `actualFields + aggregateElements + aggregateStringBytes`; baseline charges18 fields. These are deterministic modern limits, not managed heap measurements or recovered legacy CPU costs.

Decode validates the envelope and performs a complete field/element/string scan without allocating decoded strings or collections. The bounded outer envelope itself owns a payload copy. Counts/string lengths and aggregate work are checked before nested loops/copies. Only a second pass builds the immutable model. Encode first measures/checks the complete immutable model before allocating its exact-sized field stream, then writes the same representation. No partially decoded model or wire result is returned on failure.

## Validation and next boundary

The [independent Python peer](../../SWG-Godot-Client/poc/round32-tests/Networking/peer.py) constructs literal synthetic wires and independently parses every field, string spelling, nested counter and budget. The [native scenarios](../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/CreatureView6CodecScenarios.cs) compare exact models/bytes, wire/envelope overloads, exact/one-less budgets, adjusted complete delta prefixes, partial truncation, unsupported indices, malformed headers/counts, signed/full-bit values and ownership.

Passed23 exact wires,3870 negative checks,282 budget checks and122 ownership checks plus independent Python verification. Pinned static verification passed109 anchors/18 registrations/16RTTI. Accepted TANO6 state regression and shared Core smoke suite passed with zero build warnings/errors. All fixture bytes are synthetic; no proprietary data is promoted.

Server `Stardust-3/MMOCoreORB/src/server/zone/packets/creature/CreatureObjectMessage6.h` constructor lines14–43 and `CreatureObjectDeltaMessage6.h` helpers lines19–73 are correlation only. The server's22 declared operands do not create four extra client fields. Exact client evidence remains authoritative.

Next bounded milestone: current-incarnation storage/publication for this supported union, or separately recover/implement field1 composition and integer/wearable deltas. Existing TANO/CREO world routing, target callbacks, visible wearable attachment and live interoperability remain outside this component.
