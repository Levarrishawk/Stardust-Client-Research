# CREO4 complete baseline and scalar delta contract

Round38, 2026-10-03. [CreatureView4Codec](../../SWG-Godot-Client/poc/Stardust.Core/Protocol/CreatureView4Codec.cs) owns the complete fourteen-field wire snapshot and an ordered eleven-field scalar delta subset. [CreatureView4State](../../SWG-Godot-Client/poc/Stardust.Core/Protocol/CreatureView4State.cs) retains a complete immutable raw model. Collection deltas2/3/13 reject; their client mutation/equality/notification semantics require separate investigation.

## Direct client evidence and server correlation

Executable SHA256 is `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Identity is checked before PE imports or address use. Constructor0042ECF0 registers exactly fourteen entries through00555440 at0042F7C0..0042F876. The registrar targets object+C4. Snapshot dispatch00554D6C/72 reaches that registry. Generic snapshot loop0118B740 reads the raw U16 declared metadata, then traverses actual registered pointers and calls snapshot slot+08 until registry end; the declared count is not its loop bound.

The earlier [September15 checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-15_CREATURE_VIEW4_DELTAS.md) established ordering/scalar widths and server collection correlation. This round verifies the actual three client collection specializations and snapshot readers/writers independently. Current server `MMOCoreORB/src/server/zone/packets/creature/CreatureObjectMessage4.h`, constructor, lines13–62 correlates field names/order; `objects/scene/variables/DeltaVectorMap.h::insertToMessage`, lines140–153, correlates count/counter, command0/key/value map emission. `objects/creature/variables/SkillModEntry.h::toBinaryStream` correlates the first S32 to skillMod and the second to skillBonus. These server facts are correlation, not proof of client layouts or delta behavior.

| Index | Correlated role | Legacy member | Client type and snapshot reader / writer |
|---:|---|---:|---|
| 0 | acceleration base | +690 | F32,00439B30 /00439AC0 |
| 1 | acceleration modifier | +67C | F32, same |
| 2 | encumbrances | +4F8 | S32 AutoDeltaVector DefaultObjectType,0043C070 /0043B700 |
| 3 | skill modifiers | +770 | byte-string to S32/S32 AutoDeltaMap,0043F190 /0043EF90 |
| 4 | speed base | +640 | F32,00439B30 /00439AC0 |
| 5 | speed modifier | +62C | F32, same |
| 6 | listen target | +848 | NetworkId U64,00440310 /00440290 |
| 7 | run speed | +668 | F32,00439B30 /00439AC0 |
| 8 | slope angle | +6B8 | F32, same |
| 9 | slope percent | +6CC | F32, same |
| 10 | turn scale | +6A4 | F32, same |
| 11 | walk speed | +654 | F32, same |
| 12 | water modifier | +6E0 | F32, same |
| 13 | mission ID pairs | +6F4 | AutoDeltaSet of pair<NetworkId,NetworkId>,0043E380 /0043DE40 |

Observation: constructor installations, registry order, vtable slots and RTTI agree on all fourteen types; ten F32 fields share015DCF0C, encumbrances015DD03C, skill modifiers015DCE70, listen015DCDE0 and mission pairs015DCEDC. Interpretation: complete wire field structure is established; role names correlate with the server getters. Confidence: Confirmed direct types/order/widths; Strongly inferred semantic naming from correlation. Consequence: no legacy member offset or pointer enters the replacement model. Mission field13 is a **client set of complete ID pairs**; its server map-like API does not justify an owner-keyed replacement dictionary.

## Exact snapshot grammar and tolerance

The accepted generic replication envelope uses baseline opcode68A75F0C, CREO tag4352454F, view4, U64 recipient and raw U16 declared metadata. Snapshot bodies have no per-field index: fourteen bodies follow in the above order. Every multibyte value is little endian. All counts/counters are U32; encumbrance and skill numeric payloads are S32; identities are raw U64; float values are raw32-bit IEEE storage.

- Field2: U32 count, U32 counter, count S32 elements.
- Field3: U32 count, U32 counter, then count entries of U8 raw command, byte-string key, S32 base value, S32 bonus. A string has U16 length; FFFF selects an additional U32 length, followed by exact bytes. Empty/nontext/embedded-zero keys and both length spellings are retained.
- Field13: U32 count, U32 counter, then count ordered U64 first/owner ID and U64 second/mission ID pairs. There is no per-entry command byte in this snapshot.

Observation: encumbrance reads0043C09F/AC/D0 and writer0043B712/25/40 establish widths and count loop. Skill reads0043F1F6/214/240/24E/26F/2C0/2CD and writer0043EFA0/FB3/EFD0/F03C/F049 establish the exact variable record. Snapshot command storage is read once and never tested in the entry loop; the client writer always emits0 at0043EFD8. Mission count/counter reads0043E3FD/417 precede two00AA5830 calls0043E438/442; its writer likewise emits two00AA5890 values per entry0043DE7B/85. Helpers00AA57F0 and00AA5810 read/write8 bytes. Confidence: Confirmed. Consequence: the DTO preserves arbitrary consumed snapshot command bytes; it does not interpret them as map mutation commands. Baseline raw entries retain input order and duplicates for reproducibility. They are a wire record, not a reconstruction of the client's sorted/coalesced runtime maps/sets.

Observation: encumbrance construction pushes9 at0042EF13, helper00446860 zero-fills initial cells at004468D1, and initial counter is0 at0042EF21. Skill/mission counters initialize0 at0042F2F5/0042F258. Snapshot parsers clear existing containers, install received counters directly, then consume count elements; their loop tests do not impose9. Interpretation: constructor defaults are not snapshot validation rules. Confidence: Confirmed. Consequence: decode generic lengths including0/1/8/9/10/31; gameplay nine-stat eligibility belongs to a later policy. Scalar defaults include constructor/template inputs and are never synthesized when a complete snapshot field is missing.

F32 snapshot/delta00439B30/50 copy4 raw bytes; baseline post-read00439A90 copies storage without floating arithmetic. Listen snapshot/delta00440310/40 read8 bytes;00440260 copies both words. NaN payloads, signaling-bit patterns, infinity, denormals and signed zero remain raw bits. Finite/speed eligibility is a separate runtime policy.

The generic snapshot registry ignores declared cardinality and does not require end-of-input. Its view4 branch jumps00554D7F to cleanup00554E44; iterator cleanup0118AC90 merely clears its reference and does not inspect the cursor. Modern Decode requires the complete fourteen bodies and exact EOF, rejecting trailing bytes instead of reproducing that tolerance. Unknown/oversized counts, truncation and string extents reject before owned decoded entry allocation. Prefix reads use checked00439910/0118B030; the legacy skill string path constructs from pointer+length at0043F29A before cursor advance, so modern bounded string/truncation checks are explicit safety policy, not a claim of identical legacy failure behavior.

## Lifecycle, owned API and scalar state

Vector snapshot clears through0043C08A before count reads, then may invoke an optional aggregate callback. Skill snapshot clears its map via0043F1C7, creates temporary owned key/pair values, stores both S32 values and can invoke per-entry callback0043F385; temporary key cleanup is0043F3A3. Mission snapshot clears its set0043E3B7, creates two NetworkId temporaries, inserts a complete pair0043E454, may invoke aggregate0043E479, and destroys temporaries0043E485/494. Constructor installs mission aggregate target00438E90 at0042F8AE/B3. Destructors004454D0/00445870/00445690 delegate to the exact vector/map/set destructors, which release callback holders, container backing nodes/buffers, key strings and NetworkId temporaries. These direct local hooks do not establish every subscriber/gameplay effect. Original parsing mutates in place and can fail after clearing or earlier fields; it does not provide modern whole-message rollback.

Baseline owns ten ordered FloatBits at field indices0,1,4,5,7,8,9,10,11,12; ListenTo is the raw field6 U64. `RawBits(field)` accesses the eleven scalar fields. Encumbrances, SkillMods and SpaceMissionObjects retain owned readonly Values and raw Counter. SkillMod entries retain RawCommand, immutable raw Name, BaseValue and Bonus. MissionPair values retain both IDs. Constructors freeze enumerables under hard ceilings; string inspection returns copies. There are no borrowed spans, live target handles or resource lookups.

Delta uses opcode12862153 and ordered U16 field index plus raw4-byte F32 or8-byte listen value. All eleven scalar indices are supported, including repeated/equal assignments;2/3/13 and unknown indices reject before reading an assumed collection grammar. Empty deltas reject. Declared metadata is preserved independently of actual update count.

State retains immutable full Model, checked U64 Revision, DecodedBytes and copied policy. FromBaseline advances revisionSeed once; Reseed requires the same recipient and advances once; Apply preflights the entire scalar plan and creates one complete candidate with one outer revision. Existing collections, raw entries/counters and baseline declared metadata survive unchanged. Empty/foreign plans, overflow and late capacity failure preserve the input and earlier results. Receipt retains only Current, delta DeclaredCount, owned Events and WorkUsed.

Each modern assignment description contains Index, Ordinal, OldBits and NewBits and costs24 logical bytes, independent of CLR layout. Every supplied scalar assignment emits one description, even when equal; this is explicit modern receipt behavior, not a claim of original callback equality/coalescing. No original owner notification, live/world/movement effect executes.

## Global accounting and reproducible evidence

Message policy bounds payload, fields, each of three collections, aggregate elements, maximum/aggregate key contents, decoded units and work. Payload is field bytes+2; decoded units are field bytes+10. Baseline work is14+all elements+key content bytes; scalar delta work is update count. Hard ceilings are8MiB payload,4096 fields,65536 per collection/196608 total elements,1MiB per key/4MiB contents,16MiB decoded/work. Limits are validated before the generic envelope copy. The envelope owns its bounded copy before specialized preflight; the envelope overload also uses a bounded public inspection copy.

Complete model bytes are72 fixed bytes +4 per encumbrance +16 per mission pair +9 per skill entry plus its actual2/6-byte string prefix and content. State policy independently caps every retained collection/string, aggregates, model bytes, updates/events/output and work. Seed work is14+elements+key contents. Apply work is seed work + update count times(1+seed work), an explicit modern validation/assignment policy. Scalar widths and retained collections are constant through Apply; state bytes stay constant and event/output/work charges grow monotonically. Preflight of the final event/work totals therefore bounds every intermediate prefix before any candidate/event allocation. Fixed constructor/wrapper overhead remains bounded by hard ceilings; logical units are not measured heap/CPU sizes.

The independent [Python oracle](../../SWG-Godot-Client/poc/round38-tests/Networking/peer.py) packs/parses literal grammar and derives raw baseline/scalar models, old/new-bit events, revisions, ownership-independent fixtures and exact caps. [Static audit](../../SWG-Godot-Client/poc/round38-tests/Networking/static_audit.py) verifies107 instruction anchors,14 registrations and5 RTTI/vtable specializations, with wrong/truncated identity rejection before imports/addresses. [Native scenarios](../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/CreatureView4Scenarios.cs) passed40 cases,151672 negatives,1592 budget checks and471 ownership checks in both actual-Core-assembly and source-linked standalone runners. Full CREO6 on the current Core assembly and shared64-bit Core smoke regressions pass.

[Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R38_CREO4_BASELINE_SCALARS.md) records pins, commands, corrected audit attempt and exact scope. Confidence: Confirmed for deterministic synthetic contract behavior and direct static grammar; actual live interoperability and runtime collection/gameplay policy remain unverified. Next probe: close field2's exact DefaultObjectType vector delta/notification specialization before adding one bounded collection delta component. Skill/mission deltas, finite movement policy, incarnation-aware store/world publication and owner callbacks remain separate work.

## Round39 supplied terrain grounding and CREO4 prerequisites

Field2 deltas are now available through the separate [encumbrance codec/kernel](CREATURE_ENCUMBRANCES_VIEW4_CONTRACT.md). This frozen snapshot/scalar codec continues rejecting collections; field3/13 and a composed full view4 path remain open. [Evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R39_TERRAIN_GROUNDING_AND_PRIVATE_VIEW.md).
## Round40 layer and CREO4 composition

The frozen snapshot/scalar codec retains its original supported subset. New [scalar/encumbrance composition](CREATURE_VIEW4_SCALAR_ENCUMBRANCE_CONTRACT.md) preserves the full raw baseline while accepting2 alongside scalars. The [skill-map codec/kernel](CREATURE_SKILL_MODS_VIEW4_CONTRACT.md) remains separate. Mission13 and complete publication remain open.
## Round41 supplied height execution and CREO4 fields

[Supported union](CREATURE_VIEW4_SUPPORTED_CONTRACT.md) canonicalizes byte-key maps at runtime while frozen raw baseline decoding remains unchanged. [Mission-set semantics](CREATURE_MISSION_OBJECTS_VIEW4_CONTRACT.md) are separate pending full composition.
## Round42 complete private state and supplied terrain sequence

The frozen raw codec remains unchanged. [Complete runtime union](CREATURE_VIEW4_FULL_CONTRACT.md) canonicalizes both skills and mission sets under full budgets. [Prepared full storage](SCENE_CREATURE_VIEW4_FULL_STORE_CONTRACT.md) retains current incarnations and variable journals; world routing remains next.
