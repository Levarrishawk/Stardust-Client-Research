# CREO4 field13 mission-object set contract

Round41,2026-10-03. CreatureMissionObjectsView4Codec (external historical reference) owns ordered field13-only CREO4 deltas for remove0, insert1 and clear2. CreatureMissionObjectsView4Kernel (external historical reference) projects the accepted [complete baseline](CREATURE_VIEW4_BASELINE_AND_SCALARS_CONTRACT.md)'s raw pairs into an immutable set with exact client comparison and counter behavior. Existing [field2](CREATURE_ENCUMBRANCES_VIEW4_CONTRACT.md), [field3](CREATURE_SKILL_MODS_VIEW4_CONTRACT.md) and restricted composition remain frozen; full CREO4 publication is a later milestone.

## Input identity and direct evidence

All VAs refer to SHA256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. Identity was checked before static PE/Capstone reads; the original executable was never run. Static audit (external historical reference) verifies135 exact new instruction anchors plus accepted38's107 prerequisite anchors, fourteen registrations, exact vtable and RTTI. Wrong/truncated images reject before imports/VA use.

| Observation | Interpretation / confidence | Compatibility consequence |
|---|---|---|
| Constructor0042ECF0 registers +6F4 at index13 through view4+C4 registrar; constructor0042F253 creates set,0042F258 initializes counter0 | Exact field13 member, **Confirmed** | Seed received data; do not invent defaults or a fixed cardinality |
| Vtable015DCEDC slots0..7:00445690,0043DE40,0043E380,0043DDA0,0043DE20,0043DEB0,0043E510,0043DF40; RTTI AutoDeltaSet<pair<NetworkId,NetworkId>,CreatureObject> | Full-pair set, **Confirmed** | Both IDs participate in membership; never replace it with an owner-key dictionary |
| Read/write helpers00AA5830/00AA5890 consume/emit8 raw bytes for each ID | Two64-bit wire values, **Confirmed** | Public CreatureView4MissionPair retains raw U64 bits, including zero/all high bits |
| Pair comparator00450840 checks first high word with signed branches00450858/5A, low word unsigned0045085E, then second high/low at00450870/72/7A | Lexicographic **signed int64** first, then signed int64 second, **Confirmed** | Order raw U64 values by their unchecked signed interpretations; do not use unsigned U64 order |
| Insertion0044FAB0 and erase-bound helpers0044C4B0/0044C440 independently compare both complete IDs with signed-high/unsigned-low rules | Exact identity and set order, **Confirmed** | Same first ID with differing second IDs is two entries; complete duplicates collapse |

The role labels owner/mission come from accepted client/server correlation. They do not change full-pair identity or prove gameplay semantics. The [September15 checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-15_CREATURE_VIEW4_DELTAS.md) supplies the established DeltasMessage/view4 route; accepted38 independently binds the complete registry. The modern narrow reader validates field13 and complete payload exhaustion before publication, rather than repeating the original unchecked registry indexing or nontransactional mutation.

## Wire grammar, commands and tolerated exclusions

Generic replication envelope: Delta opcode12862153, CREO tag4352454F, view4, U64 recipient; all numeric fields little-endian. After raw U16 declared metadata, consume repeated `U16 field=13; U32 extent; U32 incomingCounter; extent command records`. Metadata does not bound the legacy indexed loop. Records/commands remain ordered, including repeated field13 updates. Zero extent is valid; an empty overall delta rejects. Supported commands each contribute exactly1 to extent.

| Opcode | Body | Mutation |
|---:|---|---|
|0 remove|U64 first ID,U64 second ID|Remove exact pair if present; missing pair is a no-op.|
|1 insert|U64 first ID,U64 second ID|Insert exact pair if absent; duplicate pair is a no-op.|
|2 clear|No body|Clear nonempty set; empty set is a no-op.|

Dispatch0043E603 selects remove0043E755,0043E60A selects insert0043E6C4, and0043E611 selects clear0043E617. Remove/insert both call00AA5830 twice. Remove calls exact-pair erase00448E10 at0043E773 and tests its count at0043E778/77A. Insert calls0044FAB0 at0043E6E6 and tests insertion flag0043E6FE/700. Clear tests current count0043E61A/61C. Only actual mutations queue records, increment counter and notify. Counter increments are0043E7B0(remove),0043E734(insert) and0043E64A(clear).

Writer0043DEB0 counts physical24-byte pending records, emits U32 count/counter and U8 opcode. Opcodes0/1 emit two8-byte IDs at0043DF14/1E; opcode2 emits no pair because0043DF0D skips its body. It advances24 bytes per internal record and invokes pending-clear slot+0C at0043DF34. There is no per-entry opcode in the **baseline** snapshot: accepted38 reader0043E380/writer0043DE40 use count/counter then repeated16-byte pairs.

Opcodes3..255 reject in this component. Their legacy tolerance is context-dependent and explicitly excluded: applied unknown opcodes jump0043E611->0043E7D8, consuming only the opcode, while skipped opcodes other than2 read a complete pair at0043E5AF..5C4. Writer emits no bodies above1. A state-independent codec cannot assign one universal record body to that tolerance. No additional mutation, replacement or malformed-input recovery is invented. Supporting it later requires an explicit state-aware framing API.

## Counter and snapshot rules

For each field record compute wrapped U32 `sum=localCounter+extent`. If unsigned sum<=incomingCounter, skip0; otherwise skip `min(uncheckedU32(localCounter-incomingCounter+extent),extent)`. Direct arithmetic/unsigned compare is0043E57D..590. Skipped command bodies are parsed to retain framing; clear has no pair. Actual successful mutation increments localCounter modulo2^32 once, regardless of clear size. Duplicate insert/missing remove/empty clear do not increment. After the record, raise localCounter to incomingCounter only if unsigned smaller, at0043E7ED..7F2. This allows counter-only advancement and final repair after wrapped increments; it is not field2's reconciliation rule.

Raw baseline `SpaceMissionObjects.Values` preserves received order/duplicates and Counter. Snapshot0043E380 clears the original set then installs the received counter; each pair is inserted through0043E454->0044FAB0. Exact duplicates therefore coalesce without counter increments. Runtime iteration follows signed complete-pair order. The kernel materializes that separate projection; it does not pretend its canonical set sequence is the original raw snapshot. Same-owner/different-mission and different-owner/same-mission pairs remain distinct.

## Callback edges, lifecycle and modern summaries

Three holders are +28 aggregate, +2C remove, +30 insert. Constructor0042F271/274/277 zeros them. Dispatch helpers0044CB10/30/60 use recipient+function+this-adjustment (+0/+8/+C). Aggregate target00438E90 is installed at0042F8AE/B3; it walks a registered callback collection and calls subscriber slot+4 with the owner pointer at00438EE3. The global active flag is set/cleared00438EAB/EF7. Subscriber identities, reentrancy consequences and gameplay are **Unknown**; no role is inferred from a nearby nontext data address.

Successful insert queues opcode1/pair, increments counter, marks owner via0043E860->0118A7C0, invokes insert helper0044CB60, then aggregate0044CB10. Remove similarly queues opcode0/pair, increments counter, marks owner, invokes remove helper0044CB30, then aggregate. Duplicate/missing operations bypass those edges. Clear queues2 and increments once, walks current pairs and invokes remove helper at0043E676 **before** clearing through00448F30, then owner marking and aggregate0043E6AD. Snapshot invokes aggregate once after the complete pair loop at0043E479. Generic registry commit0118A970 is separate: field slot+0C0043DDA0 destroys/clears pending records.

Destructor wrapper00445690 calls0043DC50. It frees all three holders, destroys both NetworkId values in every pending24-byte record at0043DCBD/C8, releases pending storage and set nodes/values. Parsed NetworkId temporaries are destroyed0043E805/814. The replacement retains only immutable numeric pair values; it owns candidate lists, arrays and summary collections, with no original object pointers, callbacks, owner dispatcher or engine services.

Modern `CreatureMissionObjectsView4Event` is one32-logical-byte summary for each successful command. Remove/insert carry the complete pair; clear carries null and produces one summary rather than replaying per-item legacy callbacks. Counter-only changes emit no mutation summary. All summaries publish only after the full message succeeds. Any late failure preserves previous pair sequence/counter/revision and publishes nothing. Checked modern revisions advance for every successful message; wrong recipient/overflow reject. Reseed accepts only the same recipient and reprojects a complete baseline at the next revision. A successful operation-limit override becomes the new kernel's retained policy.

## Explicit modern bounds and work

Codec defaults/hard maxima: payload1048576/8388608 bytes (includes2 metadata bytes), fields256/4096, commands4096/65536, decoded charge2097152/16777216, work1048576/16777216. Logical decoded charge=field wire bytes+24*commands; work=fields+commands, including skipped commands. Scan validates complete bounded input before owned command DTO allocation; one envelope copy precedes scan. Update/Delta construction freezes at65536 commands/4096 updates and requires extent equal to command count. Clear cannot carry a pair; remove/insert require one.

Kernel defaults/hard maxima: retained pairs65536/65536, raw seed pairs65536/65536, state bytes1048584/16777216, summaries4096/262144, output bytes2097152/33554432, work1048576/16777216. State/output minimum8; remaining limits nonnegative. Raw seed count is checked before candidate allocation; duplicate coalescing cannot evade that input cap. Retained/candidate count, bytes, summaries, output and work are checked independently.

State bytes=`8+16*pairCount`; output adds32*summaryCount. Capacities include the starting state and every candidate intermediate. Remove/clear summary capacity includes the pre-removal set; growth immediately followed by clear still requires the larger intermediate capacity. Charges are deterministic logical units, not CLR heap/CPU/allocation measurements.

Modern sorted-list lookup charges1 for the first signed-ID comparison and another1 when first bits match, before comparing the second. Stop at first equal/greater pair or list end. Seed work starts rawPairCount, adds lookup charges,1 per membership decision, shifts `oldCount-index` for new insertions, then finalPairCount. Apply starts initialPairCount+codecWork, adds lookup charges for every non-skipped insert/remove; actual insert adds `1+oldCount-index`, remove `oldCount-index`, and clear oldCount. No-op clear adds nothing. Finish adds finalPairCount+summaryCount. Bounds precede comparisons/candidate mutation and publication. Legacy tree layout/allocator complexity is not copied.

## Verification and resume

Independent Python peer (external historical reference) constructs literal synthetic wires and independently computes signed full-pair order, duplicate collapse, counters, summaries, work and intermediate capacities. Native scenarios (external historical reference) run an actual separate Core assembly and source-linked standalone. Both pass122 cases,21568 negatives,1976 budget checks and807 ownership/retention checks; identical exports match the independent oracle.

Coverage includes all commands, clear/duplicate/missing no-ops, both identity parts,100 high/low-bit combinations, signed-order boundaries, raw duplicate snapshots,65536 duplicate seed records,65536 command records, future/stale/partial/wrapped counters, skipped clear, final counter repair, repeated records, intermediate growth/clear, aliases/readonly collections, wrong recipient/revision and late rollback. Small messages test every truncation cut; large records use deterministic sampled cuts. Unknown opcodes3..255 all reject. Exact/one-less aggregate and intermediate message/kernel/seed limits are tested.

Frozen40 skill-map regression passes122 cases/24719 negatives/2808 budgets/1065 ownership with its independent peer on current Core DLL. Shared64-bit Core smoke builds0 warnings/errors and passes18 journal events/0 failures. No live packet occurrence/capture or original execution is claimed; all fixtures are synthetic. [Checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R41_CREO4_MISSION_SET.md) records commands, pins and scope.

Next: compose reviewed scalar/encumbrance/skill-map/mission-set components into complete ordered CREO4 state with one global budget and atomic publication, preserving raw snapshot versus canonical runtime distinctions. Subscriber/gameplay/reentrancy and live interoperability remain separate. Unknown-opcode tolerance requires a future state-aware framing contract. Shared ledgers/status/registries and research/function/protocol metrics are unchanged by this worker.
