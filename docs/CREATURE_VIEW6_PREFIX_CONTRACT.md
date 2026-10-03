# Creature view6 inherited prefix and extension boundary

## Result and compatibility boundary

CREO view6 inherits the exact TANO view6 field objects at indices 0 and 1: a passive U32 and the specialized CachedNetworkId defender vector. Creature-owned fields begin at index 2; the pinned client has 18 registered fields through index 17. This is confirmed static structure, not original client execution or a newly implemented CREO6 codec.

The [round-31 verifier](../poc/round31-tests/Networking/static_audit.py) hash-gates the executable, checks 109 instruction anchors, all 18 registrations and 16 distinct RTTI vtables. The [checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-10-03_R31_CREATURE_VIEW6_PREFIX.md) records reproduction and confidence. Existing [schema map](REPLICATION_SCHEMA_MAP.md), [earlier delta investigation](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-15_CREATURE_VIEW6_DELTAS.md) and [registration investigation](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-11_GHIDRA.md) remain the historical evidence record. This milestone consolidates implementation prerequisites and does not change research, name or packet denominators.

## Direct inheritance and dispatch evidence

Baseline executable SHA256 is `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Addresses below are VAs for that image.

Creature constructor `0042ECF0` calls Tangible constructor `0063EE10` at `0042ED2A`; Tangible calls common ClientObject constructor `00554240` at `0063EE3C`. Common registration `00554661/0055466A` passes object member `+230`; Tangible registration `0063F079/0063F082` passes `+2F0`. Creature registrations at `0042F91D..0042F9F8` append sixteen fields in the table order below. The fields use the same base vtables `015DE08C` and `015E0614`, not newly allocated creature-specific prefix codecs.

Registrar `005554C0` selects the view6 registry at object `+144`. Its virtual registration ultimately assigns the U16 ordinal from current vector length at `0118A846..0118A84F`, then appends at `0118A85A`. Snapshot dispatch uses the view6 branch `00554D84` and registry snapshot virtual; delta dispatch uses `00554F22`, delta virtual `00554F0B` and commit virtual `00554F12`. Both are common object dispatch paths selected by view, not separate CreatureObject field switches.

Snapshot `0118B740` reads two bytes of declared-count metadata at `0118B747/0118B752`, then walks every registered pointer from registry begin to end and invokes field snapshot slot `+8`. The declared count is not its loop bound. Delta `0118AA00` likewise consumes the two-byte metadata, then loops until stream exhaustion, reads a U16 field discriminator and invokes slot `+18`. At `0118AA48` it indexes directly without a visible bounds check. Any nonzero remainder attempts another discriminator; this does not permit truncation. The replacement must validate field indices and full nested bodies before publication.

## Prefix bytes and counter reuse

All multibyte wire values are little endian. A complete CREO6 baseline payload begins:

```text
U16 declaredCount
U32 passiveCommon
U32 defenderCount
U32 defenderCounter
defenderCount * U64 rawDefenderId
field2 and the remaining creature fields
```

Field2 starts at payload offset `14 + 8 * defenderCount`; there is no U16 field index between baseline fields. In a delta, index 0 consumes U32 and index 1 consumes the specialized defender delta grammar. Indices 0 and 1 may appear anywhere among repeated U16-indexed updates: the shared prefix is a schema inheritance boundary, not a required contiguous delta byte prefix.

The [TANO6 codec grammar](TANGIBLE_VIEW6_CODEC_CONTRACT.md) and [specialized state policy](TANGIBLE_VIEW6_STATE_POLICY.md) therefore apply to the inherited defender field itself: U32 raw extent/counter, U8 command, U16 index or assignment length, full U64 IDs, assignment extent N+1 and whole-command overshoot, exact unsigned counter filtering and modulo U32 mutation. That reuse follows identical field vtable/parser identity, not analogy to a creature integer vector. Passive value remains raw; neither family-specific server constants nor RTTI establish its historical semantic meaning.

The current [owned TANO codec](../poc/Stardust.Core/Protocol/TangibleView6Codec.cs) intentionally accepts only TANO and an exact two-field baseline. It cannot decode a CREO baseline by changing its tag or discarding the extension. A future composite decoder should reuse the characterized field grammar under an explicit CREO schema and retain its own whole-message bounds and metadata.

## Creature extension structure

The following widths/order are directly corroborated by registration, field RTTI, snapshot/delta targets and bounded helper inspection. Labels are RTTI/client interpretations correlated with current server source; raw structure is authoritative.

| Index | Member / vtable | Body | Snapshot / delta |
|---:|---|---|---|
| 2 | +90C / 015DCD74 | 16-bit level word | 00440950 / 00440970 |
| 3 | +8E0 / 015DCD98 | raw byte string, performance animation | 004405E0 / 00440740 |
| 4 | +558 / 015DCFA8 | raw byte string, mood | 0043C950 / 0043CB90 |
| 5 | +5C8 / 015DCF60 | U64 cached weapon ID | 0043CE80 / 0043CF80 |
| 6 | +7A8 / 015DCE4C | U64 cached group ID | 0043F960 / 0043FA90 |
| 7 | +7E0 / 015DCE28 | U64 first ID followed by U64 second ID | 0043FF50 / 00440020 |
| 8 | +868 / 015DD014 | 32-bit guild word | 004398F0 / 00439A20 |
| 9 | +588 / 015DCF84 | U64 cached look-at target ID | 0043D0C0 / 0043D1F0 |
| 10 | +530 / 015DCFF0 | U8 mood | 0043C640 / 0043C690 |
| 11 | +834 / 015DD014 | 32-bit performance-start word | 004398F0 / 00439A20 |
| 12 | +818 / 015DCE04 | S32 performance type, preserve raw bits | 00440160 / 004401B0 |
| 13 | +404 / 015DD07C | S32 HAM vector | 0043B050 / 0043B1A0 |
| 14 | +43C / 015DD07C | S32 maximum-HAM vector | 0043B050 / 0043B1A0 |
| 15 | +91C / 015DCD34 | WearableEntry vector | 00441740 / 00441920 |
| 16 | +954 / 015DCD10 | raw byte string, alternate appearance | 00442080 / 004421E0 |
| 17 | +9A4 / 015DCCEC | U8 frozen/bool storage | 004423F0 / 00442410 |

The level RTTI suffix `@F` identifies a client short specialization while the server inserts a uint16. Preserve the 16 bits; do not silently promote an unsigned gameplay interpretation into a recovered client type. Guild/start fields use client int RTTI but current server U32-style insertions; preserve their full bits too. Noncanonical bool-byte acceptance/normalization is not closed by this investigation.

Each string reads a U16 byte length; FFFF triggers another U32 length, then exactly that many raw bytes. Snapshot discriminators are `00440654`, `0043C9C4`, `004420F4`; delta discriminators are `004407B4`, `0043CC04`, `00442254`. The code does not require a terminator or validate a text encoding here. A replacement should retain raw bytes and short/extended spelling rather than normalize through host text. Earlier prose calling these ASCII16 is a short-form description, not a restriction to lengths below FFFF.

Pair field7 is one registered field containing two U64 values: snapshot calls `00AA5830` at `0043FF9B/0043FFA5`, delta at `0044006B/00440075`. Helper `00AA5830` calls raw eight-byte reader `00AA57F0`. The server names the second value invite counter, but client RTTI types both as NetworkId. Treat that name as correlation, not proof of counter arithmetic.

Fields13/14 baseline bodies are U32 element count, U32 counter, then count S32 elements. Their delta bodies use U32 raw extent/counter and U8 commands0..4: remove(U16 index), add/set(U16 index,S32), assign(U16 N,N S32), clear(empty). The inspected skipped assignment consumes all N elements and advances raw cursor N+1. The header unsigned clamp is directly visible at `0043B1E8..0043B1F1`; a complete mutation/callback policy audit remains separate from this structural prerequisite.

Field15 uses the same header/command widths with variable WearableEntry elements. Entry reader `00B94C60` calls raw string reader `00439F60`, reads a 32-bit arrangement word, then U64 ID and a 32-bit template CRC in that order. The string also supports FFFF/U32. Baseline header reads are `004417CD/004417E7`; entry call is `0044180D`; skipped assignment calls the same entry reader at `004419CF`. Its x86 stride20h is not its wire size. Existing [wearable ownership evidence](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-11_BLOCK2.md) establishes null replicated-vector callback holders and containment-driven attachment; this contract adds no scene-node ownership inference.

## Server correlation and next decoder

Correlation source is local Stardust-3 `MMOCoreORB/src/server/zone/packets/creature/CreatureObjectMessage6.h`, constructor lines14–43, and `CreatureObjectDeltaMessage6.h`, scalar helpers lines19–73. Tangible parent `packets/tangible/TangibleObjectMessage6.h` lines14–19 writes common word and defenders; its current constant is76h even for the creature builder, despite the historical3Dh comment. The creature builder declares22 but emits the eighteen registered fields. Client count tolerance explains compatibility; it does not invent four additional fields.

Next bounded implementation: an owned complete CREO6 baseline codec plus ordered scalar/string/pair deltas at indices0,2–12,16–17, with explicit reuse of field1 grammar if included. Preserve counts, raw signed/unsigned bits, IDs, strings and wire spelling; use deterministic fixtures and global nested budgets. Fields13–15 require their own bounded element decoders and specialization-specific state policies before full delta/state support. No broad gameplay, callbacks, current-incarnation integration or live interoperability is established here.
