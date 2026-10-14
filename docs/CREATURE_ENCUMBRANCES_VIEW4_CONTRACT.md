# CREO4 field2 encumbrance delta contract

Round39, 2026-10-03. [CreatureEncumbrancesView4Codec](../../SWG-Godot-Client/poc/Stardust.Core/Protocol/CreatureEncumbrancesView4Codec.cs) decodes and re-encodes ordered field2-only CREO4 deltas. [CreatureEncumbrancesView4Kernel](../../SWG-Godot-Client/poc/Stardust.Core/Protocol/CreatureEncumbrancesView4Kernel.cs) projects the owned S32 vector/counter from the accepted [complete CREO4 baseline](CREATURE_VIEW4_BASELINE_AND_SCALARS_CONTRACT.md), then publishes immutable mutations transactionally. Scalars and fields3/13 are rejected by this narrow codec. Full CREO4 composition remains a later milestone.

## Identity, evidence and confidence

All addresses are baseline VAs for executable SHA256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. The [static verifier](../../SWG-Godot-Client/poc/round39-tests/Networking/static_audit.py) gates this identity before imports/VA access and checks131 exact instruction anchors plus accepted38's107 prerequisite anchors, fourteen registrations, the exact eight-slot vtable and both command jump tables. No original executable was run. Independently authored descriptions and small instruction anchors are retained; bulk output stays private.

| Observation | Interpretation / confidence | Compatibility consequence |
|---|---|---|
| Constructor0042ECF0 registers +4F8 at0042F7D3 through00555440; accepted38 verifies view4 registry+C4 | Field2 owns S32 AutoDeltaVector with DefaultObjectType, **Confirmed**; encumbrance role correlated in the accepted baseline | Do not substitute HAM's CreatureObject specialization or require a nine-element wire vector |
| Vtable015DD03C slots0..7:004454D0,0043B700,0043C070,0043B6C0,0043B6F0,0043B760,0043C1B0,0043C5D0; exact RTTI contains AutoDeltaVector@HUDefaultObjectType | Exact snapshot/delta writer/reader and lifecycle specialization, **Confirmed** |0043C1B0 is the delta entry;0043C200 is inside counter arithmetic |
| Snapshot0043C070 consumes count/counter plus count S32; writer0043B700 emits those widths | Owned variable vector, **Confirmed** | Accepted38 collection seeds this component without inventing omitted defaults |
| Constructor0042EF13 pushes9,00446860 initializes zeros;0042EF21 stores counter0;0042EF3A/3D/40/43 zero four callback holders | Construction default is nine zero slots, **Confirmed** | Construction default is not a parser/gameplay shape restriction |
|00554F1A selects registry+C4;00554F0B invokes reader slot+18 then00554F12 slot+1C;0118AA00 reads U16 metadata and indexed fields to payload exhaustion | Existing CREO4 indexed route, **Confirmed** | Preserve raw declared metadata and repeated ordered field2 records; reject unsupported indices before state publication |
|0118AA48 dereferences registry[index] without a bounds check;00439910 performs bounded byte reads | Legacy malformed outer index can be unsafe and mutations are nontransactional, **Confirmed** | Modern narrow scanner rejects unsupported/truncated input; never reproduce pointer faults |

No new semantic names, function counts, packet rows or research percentages are claimed. The [September15 checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-15_CREATURE_VIEW4_DELTAS.md) supplies prior role/routing context; its nine-stat gameplay description is separate from the variable wire container proved here.

## Wire grammar and counter cursor

Use the public replication envelope: Delta opcode12862153, tag4352454F (CREO), view4, U64 object id; all numeric encodings are little-endian. After raw U16 declared metadata, consume repeated `U16 field=2; U32 extent; U32 incomingCounter; commands`. Field updates and commands retain order. Empty overall delta rejects; a field update with extent0 is valid. Metadata does not control iteration.

| Opcode | Body | Cursor contribution | Mutation |
|---:|---|---:|---|
|0 remove|U16 index|1|If index is outside current length, no change. Otherwise remove/shift; counter+1.|
|1 insert|U16 index,S32 value|1|Grow with zeros up to index if needed, insert/shift; counter+1.|
|2 set|U16 index,S32 value|1|Equal existing value is a no-op. Otherwise grow to index+1 with zeros and assign; counter+1.|
|3 replace|U16 count,count S32 values|1+count|Replace whole vector, including equal/empty replacement; counter+1+count.|
|4 clear|No body|1|Empty vector is a no-op; otherwise remove all values; counter+1.|

At each field record, compute `skip = min(uncheckedU32(extent - incomingCounter + localCounter), extent)`. Start cursor0. A command is skipped when its **starting** cursor is below skip, then advance by its complete contribution. Decode every skipped body to locate the next command. Skipping replace may pass both skip and extent; it never partially applies values. Stop when cursor reaches or exceeds extent. A replace's complete body is accepted even when its contribution overshoots extent. The narrow encoder preserves such raw extent/counter values; it does not normalize them to a canonical legacy-emitted queue size.

Direct arithmetic/skip evidence:0043C1F8..0043C206 uses32-bit subtract/add and unsigned clamp;0043C213..0043C26A consumes skipped shapes;0043C243 counts each skipped replacement S32,0043C262 adds the command unit.0043C276..0043C290 dispatches opcodes0..4 through0043C5B0. Applied replacement0043C538 increments cursor per value and0043C3BD increments per command. No-op counter effects follow mutation guards, not incomingCounter assignment. All counter increments wrap U32; incomingCounter is never copied into localCounter.

Direct mutation evidence: remove0043C2C6/2C8 guards length then0043C2E8 increments; insert0043C35D increments and0043C36C..37E grows to index using zero before004468F0 inserts; set0043C40B..414 suppresses equal value,0043C42D increments,0043C440..44D grows to index+1 using zero,0043C454..460 stores. Resize00448620 has zero-fill branch00448712. Replacement0043BD80 increments once at0043BDD9 and for each queued set at0043BF1E; it does not call the equality-suppressing setter. Clear0043B870 returns on empty at0043B87F, otherwise increments0043B8BD.

Legacy delta writer0043B760 emits the physical pending8-byte record count as U32 at0043B776..787, counter+18 at0043B78C..794, each opcode U8, required index/count U16 and S32 values. Replacement consumes count extra queued value records at0043B823..83C; their internal opcodes are not transmitted. Writer slot+0C clears pending records at0043B853. This independently confirms widths and replacement's `1+count` accounting.

Modern exclusions: unknown opcodes reject even when legacy skipped-command handling might tolerate a byte plus generic operands; extra/incomplete DTO command sequences reject. The codec accepts only the structurally verified opcodes0..4. Legacy allocator size limits, counter-cursor overflow after billions of contributions, subscriber reentrancy and pointer failures are not reproduced. The bounded message policy prevents those sizes.

## Notifications and ownership

Legacy general holder+28 dispatches through0044C9A0; remove holder+2C through0044C9C0(index,old), insert holder+30 through0044C9E0(index,new), and set holder+34 through0044CA00(index,old,new). Successful remove/insert/set invokes the corresponding item helper then general notification. Invalid remove/equal set invokes neither. These calls are conditional on non-null holder and recipient.

Clear0043B870 calls holder+2C for each original element at0043B8D0..8E5, then clears the vector and calls general holder+28 at0043B933. Replace0043BD80 copies the replacement, queues its set records, then calls only general holder+28 at0043BF4E; it does not issue individual remove/insert/set callbacks. Snapshot0043C194..1A1 calls general holder after reading. These are **Confirmed structural callback edges**, not proof of subscriber gameplay effects.

Mutation paths conditionally call0118A7C0 with owner+8 to register a changed field in an ordered pointer set; its0118A825 insertion and optional owner holder0118A837 are separate from the four immediate callbacks. Registry commit0118A970 calls field slot+0C at0118A98B, then clears the set. For this field, slot+0C0043B6C0 clears pending command records; it is not the general callback. Subscriber identity, active callback installation, reentrant behavior and gameplay consequences remain **Unknown**.

Destructor wrapper004454D0 calls0043B5D0. It frees all four callback holders (+28/+2C/+30/+34), the pending8-byte record buffer and the current4-byte S32 buffer; no game object pointers are stored in the wire values. The modern kernel owns immutable value copies, recipient/revision and limits. It invokes no legacy callback, owner dispatcher or engine service. Failures retain the previous object, counter, vector and revision.

A modern `CreatureEncumbrancesView4Event` is one24-logical-byte **mutation summary** per successful command; replacement/clear summaries have no index or old/new scalar. This deliberately differs from clear's per-item legacy notifications and is published only after the entire message succeeds. It is not a callback replay API. A successful no-op message still increments modern revision. Wrong recipient and revision overflow reject. Reseed requires matching recipient and preserves the next checked revision; it reprojects only field2 from a complete baseline.

## Modern limits and reproducible accounting

Codec limits are aggregate across all repeated field2 records. The default/hard maxima are payload16384/8388608 bytes (includes2 metadata bytes), fields256/4096, commands4096/65536, S32 values65536/1048576, and logical decoded field bytes1048576/16777216 (excludes metadata). DTO construction owns input collections and caps commands/update65536, updates4096 and replacement values65535. Decode scans the complete bounded body before allocating command/value DTO arrays. The envelope's bounded byte copy is made once.

Kernel default/hard maxima are elements65536/65536, state bytes262152/16777216, events4096/262144, output bytes1048576/33554432 and work1048576/16777216. Limits are nonnegative except state/output minimum8. The U16 index65535 may create65536 values; inserting into an already65536-element vector rejects before allocating growth. The legacy generic vector has no corresponding modern hard count guard on that growth path.

State bytes = `8 + 4*valueCount`. Output bytes = state bytes +24*summaryCount. Capacity checks cover the starting state and every candidate intermediate mutation, including a growth immediately followed by clear. Remove/clear candidate capacity charges the pre-removal count; the output summary can coexist with that vector during staging. These are deterministic logical accounting rules, not CLR heap/allocator sizes.

Seed work = initial valueCount. Apply work starts with initial valueCount + fieldRecords + all parsed commands + all parsed S32 values, including skipped commands. Additional mutation work: remove `oldCount-index`; insert `newCount-oldCount+max(0,oldCount-index)`; set `newCount-oldCount+1`; replace `oldCount+replacementCount`; clear `oldCount`. Skipped/equal/invalid/empty no-ops add no mutation work. Finish adds finalCount+summaryCount. Work/output/capacity are checked before publication. Validation, transient envelope copies and actual CLR copy multiplicity are outside this logical model.

## Verification and open boundary

[Independent Python peer](../../SWG-Godot-Client/poc/round39-tests/Networking/peer.py) constructs literal synthetic wire bytes and independently calculates list/counter/event/work/peak-capacity results. [Native scenarios](../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/CreatureEncumbrancesView4Scenarios.cs) exercise the actual separate Core assembly and a standalone build.121 cases pass, including all opcodes, equal/empty/invalid no-ops, variable baseline lengths, U32 wrap, stale/partial skips, replacement cursor overshoot, repeated records, intermediate grow/clear, index65535 and replacement count65535. All truncation offsets are tested for small messages; large replacement fixtures use deterministic sampled offsets. Unsupported fields/tags/views/opcodes, exact and one-less aggregate/intermediate limits, aliases, recipient/revision failures and retained-state checks are covered.

Both builds report13994 negative checks,1748 budget checks and806 ownership/retention checks; their exports are identical and match the independent oracle. Accepted38's complete CREO4 codec/scalar state passes against the current Core assembly; the shared64-bit Core smoke passes with allowlisted journal18 events/0 failures. See the [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R39_CREO4_ENCUMBRANCE.md) for reproduction and bounded scope.

No live interoperation or actual corpus delta occurrence is asserted; all executable input was read statically and all wire cases are labelled synthetic. Next: independently close the exact field3 skill-map delta at0043F490, particularly dropped payload shape, key equality/order, ownership and callback subscribers, before composing complete CREO4 state. Gameplay encumbrance mapping and general callback installation remain separate open probes at0044C9A0/0044C9C0/0044C9E0/0044CA00.
