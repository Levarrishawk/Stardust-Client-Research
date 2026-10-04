# CREO4 field3 skill-map delta contract

Round40,2026-10-03. [CreatureSkillModsView4Codec](../poc/Stardust.Core/Protocol/CreatureSkillModsView4Codec.cs) owns ordered field3-only delta records and supports opcodes0/1/2. [CreatureSkillModsView4Kernel](../poc/Stardust.Core/Protocol/CreatureSkillModsView4Kernel.cs) materializes the [accepted complete CREO4 baseline](CREATURE_VIEW4_BASELINE_AND_SCALARS_CONTRACT.md)'s raw skill records into an immutable ordered byte-key map. This is one narrow compatibility component; other fields and full CREO4 publication remain separate. The [field2 component](CREATURE_ENCUMBRANCES_VIEW4_CONTRACT.md) is unchanged.

## Evidence, identity and confidence

All VAs refer to executable SHA256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`, freshly checked before static analysis. Existing cached pefile/Capstone read the executable; no original execution occurred. The [verifier](../poc/round40-tests/Networking/static_audit.py) checks144 exact new instruction anchors plus accepted38's107 prerequisite anchors, fourteen registrations, exact field3 vtable and RTTI. Wrong/truncated images reject before PE imports/VA use.

| Observation | Interpretation / confidence | Compatibility consequence |
|---|---|---|
| Constructor0042F2CF selects +770; registration is index3 in the fourteen-field+C4 registry | Exact field3 specialization, **Confirmed** | Do not borrow HAM or server mutation rules |
| Vtable015DCE70 slots0..7 are00445870,0043EF90,0043F190,0043ED30,0043EF80,0043F080,0043F490,0043F180; RTTI contains narrow basic_string, pair@HH and CreatureObject | Raw byte-string to two signed32-bit values with CreatureObject owner, **Confirmed** | Field names skill/base/bonus are accepted client/server correlation; raw types are direct evidence |
|0042F2F0 constructs map,0042F2F5 initializes counter0,0042F30E/311/314 zero three holders,0042F317 installs exact vtable | Empty construction default, **Confirmed** | Complete received snapshot determines contents, not synthesized gameplay state |
|0043F490 consumes U32 extent/counter then command records;0043F080 emits the corresponding pending records | Three regular command shapes and counter transitions, **Confirmed** | Preserve raw wire order, signed values, key bytes and counters |
|00447390 compares explicit begin/end spans using unsigned byte flags at004473BA/BE/C0, then length at004473C7/D7; snapshot0043F420 and remove453EF0 have equivalent comparisons | Unsigned lexicographic byte order, exact byte identity including NUL, **Confirmed** | No Unicode decoding, case folding, C-string termination or locale comparator |

Prior routing comes from the [September15 checkpoint](../research/checkpoints/ANALYSIS_CHECKPOINT_2026-09-15_CREATURE_VIEW4_DELTAS.md) and accepted38/39 direct registry audit: DeltasMessage12862153 -> view4 registry+C4 -> indexed reader0118AA00 -> field slot+18 -> commit0118A970. Raw U16 metadata is read but is not the indexed loop bound. The modern codec rejects every index except3 and requires complete bounded payload exhaustion. It does not reproduce the legacy registry's unchecked index dereference.

## Raw wire grammar and supported subset

Envelope: Delta opcode12862153, CREO tag4352454F, view4, U64 recipient. All numeric widths are little-endian. After raw U16 declared metadata, consume repeated `U16 field=3; U32 extent; U32 incomingCounter; extent command records`. Unlike vector replacement, each supported record contributes exactly1 to extent. Zero-extent records are valid; an empty overall delta rejects. Duplicate field records and command order are retained.

Every supported record is `U8 opcode; byte-string key; S32 base; S32 bonus`. Key encoding is U16 byte length, or sentinelFFFF plus U32 length, followed by exactly that many bytes. Extended prefixes for short/empty keys are accepted and preserved in the wire DTO. No UTF8/text semantics are imposed. Both numeric operands are consumed even for remove.

| Opcode | Verified mutation |
|---:|---|
|0|Upsert. Absent key inserts; existing unequal pair updates; equal pair does nothing.|
|1|Remove by key. Missing key does nothing. Incoming pair is consumed but ignored for mutation and old-value callback.|
|2|Same upsert behavior as0, including insertion when absent.|

Reader dispatch0043F5D8/5DB/5DE sends0/2 to shared0043F5E4 and1 to0043F60A. All three paths read the byte-string and two4-byte values. Removal calls0044CBD0 at0043F696, whose missing-node branch0044CC02 returns and whose successful branch captures the stored pair at0044CC3E/41. Upsert lookup0043F725 branches to insertion0043F733..77F or existing pair comparison0043F789. Equality helper00454560 compares both32-bit values;0043F793 bypasses callbacks/queue/counter on equality. Changed existing values are stored at0043F7C6/D1.

Legacy pending output distinguishes **effect**, not input label: an absent-key upsert queues0 at0043F73A, a changed existing pair queues2 at0043F79C, and a successful remove queues1 in0044CBD0. Thus input opcode0 can mean change and opcode2 can mean insert. Modern events retain the input opcode; old/new presence identifies the actual mutation.

The bounded codec deliberately excludes opcodes3..255. Their static tolerance is known but is context-dependent: applied unknown bytes jump0043F5DE->0043F807 and consume only the opcode, while skipped unknown records pass through0043F510..5A9 and consume a full key/pair. Writer0043F0DA/DD similarly omits bodies for pending opcodes above2. A state-independent record codec cannot assign one shape to that tolerance. No clear/replacement command is invented, and this component does not claim the entire tolerated byte language. Supporting that branch later requires an explicitly state-aware framing contract; ordinary verified map mutations are fully supported here.

## Counter reconciliation

At each field record, compute wrapped U32 `sum=localCounter+extent`. If unsigned `sum<=incomingCounter`, skip0 records. Otherwise skip `min(uncheckedU32(localCounter-incomingCounter+extent),extent)` records. Parse every skipped record but apply no mutation. This exact extra comparison is at0043F4E9..4EE; skip subtraction/clamp is0043F4F0..4F8. It must not be replaced with the field2 formula.

Each successful insertion/change/removal increments localCounter modulo2^32. Equal pairs and missing removes do not. After all records, set localCounter to incomingCounter only if it is **unsigned smaller**, at0043F81C..821. A future incoming counter can therefore advance state even when no map entry changes, including zero-extent records. Wrapped increments can be raised back by this final comparison. Incoming counters are not blindly assigned before command application.

Writer0043F080 computes pending24-byte record count at0043F08D..0AD, emits count/counter as4 bytes, opcode as1, short/extended byte-string length, raw bytes and the two4-byte values. It advances24 bytes per internal pending record at0043F159 and clears pending records through slot+0C at0043F168. These instructions independently confirm count units and complete remove operands.

## Snapshot materialization, callbacks and lifetime

The accepted baseline DTO intentionally preserves raw snapshot command bytes, order and duplicate records. Snapshot0043F190 ignores the consumed command discriminator, clears the old map, reads/install counter directly, looks up/inserts each key, then **unconditionally** writes its pair at0043F353/35F. Therefore duplicate byte keys keep one runtime entry with the last received pair. Byte comparator0043F420 and the exact tree helpers establish ordering; the replacement does not infer a server dictionary policy. Raw snapshots remain raw evidence; the kernel projection is a distinct runtime model.

Kernel entries are sorted by unsigned byte order. `CreatureSkillModsView4Entry.Name` owns the exact key bytes but uses the shortest valid length prefix as a **modern representation choice**; prefix choice is not map identity. Wire command DTOs retain their original prefix. Equal bytes with different prefix widths coalesce. The kernel does not expose an exact post-mutation legacy wire snapshot, preserve ignored raw snapshot command bytes in runtime entries, or normalize the bytes themselves.

Three member-callback holders live at+28(remove),+2C(insert/snapshot entry) and+30(change). Remove0044CBD0 increments counter before erasing, marks owner through0118A7C0, then calls holder+28 with key/stored old pair at0044CCFA. Upsert insertion calls owner marker0043E860, insert holder through0044CD50, queues pending record, then increments counter at0043F7F8. Change calls owner marker, change holder00452BA0(key,old,new), queues record and then increments counter. Equal pairs/missing removes bypass those hooks. Snapshot calls the insert/snapshot holder for **every raw record**, including overwrite duplicates, at0043F385.

Holder dispatch uses recipient, function and `this` adjustment (+0/+8/+C), as0044CD53..71 and00452BA3..BC5 show. This differs from field2's simpler holder. Generic changed-field registration/commit is separate from these immediate callbacks: field slot+0C0043ED30 clears pending records, and registry0118A970 clears its changed-field set. Actual subscribers, installation after construction, callback reentrancy and gameplay effects are **Unknown**. No callback or owner dispatch is invoked by the modern kernel.

Destructor00445870 delegates0043EBF0. It frees all three holders, destroys each pending string in24-byte records, frees pending storage, recursively releases map nodes and owned keys. Temporary parsed strings are destroyed on reader paths. Values are signed integers, not game-object pointers. Modern kernels share only immutable entry/key objects across retained states; candidate lists, new entries and event collections are owned. Failure retains the previous counter/map/revision and emits no event.

## Modern budgets, summaries and work

Codec defaults/hard maxima: payload1048576/8388608 bytes (includes2 metadata bytes), fields256/4096, commands4096/65536, per-key65536/1048576 bytes, aggregate key bytes262144/4194304, decoded charge2097152/16777216 and work1048576/16777216. Decoded charge is field wire bytes +24*commands; work is fields+commands+key bytes. Both are explicit logical units, not CLR heap/CPU measurements. All repeated records and skipped bodies contribute to message totals. Scan validates the whole bounded body before allocating owned command/key DTOs; one envelope byte copy precedes it. Update/Delta constructors freeze command/update enumerables under hard65536/4096 ceilings. DTO extent must exactly equal supported command count.

Kernel defaults/hard maxima: entries4096/65536, raw seed records65536/65536, per-key65536/1048576, aggregate key bytes262144/4194304, state bytes1048576/16777216, events4096/262144, output bytes2097152/33554432, work1048576/16777216. State/output minimum8, other caps nonnegative. Seed preflight bounds **all raw** key bytes and records before canonical entry allocation, so duplicate coalescing cannot evade input limits. Retained/candidate entries and total key bytes are independently bounded during Apply. Non-skipped lookup keys also obey the kernel per-key cap; skipped keys obey codec limits without becoming retained state.

State bytes=`8+16*entries+retainedKeyBytes`; each successful command emits one modern summary charged32+keyLength. Output combines candidate state and all accumulated summaries. Candidate capacities include pre-removal state and its summary; temporary insertion followed by removal still needs its larger intermediate capacity. Summary payloads use canonical byte keys and actual old/new pairs. Counter-only changes produce no mutation summary. All summaries publish only after the complete message succeeds, rather than replaying legacy callback timing.

Deterministic sorted-list lookup charges `1+min(existingKeyLength,inputKeyLength)` before each comparison until an equal/greater key or end. This is a modern conservative byte-comparison bound; the legacy tree's shape/allocation policy is not copied. Seed work begins rawRecordCount+rawKeyBytes, adds lookup charges,1 per assignment and shifting `oldCount-index` on insert, then finalEntryCount+retainedKeyBytes. Apply begins initialEntryCount+initialKeyBytes+codecWork, adds lookup charges for every non-skipped command, then remove `oldCount-index` or upsert `1+(absent?oldCount-index:0)` on actual mutation. Equal/missing no-ops add only lookup cost. Finish adds finalEntryCount+finalKeyBytes+summaryCount+summaryKeyBytes. Charges precede expensive comparisons/candidate allocation and publication.

Checked modern revisions advance on every successful Apply, including counter-only/no-op messages. Wrong recipient or revision overflow rejects. Reseed requires the same recipient and projects a complete raw baseline at the next revision. A successful operation-limit override becomes the new kernel's retained policy. Limits/revisions/transactional publication/canonical prefix/summary accounting are modern policy, not recovered legacy fields.

## Validation and open questions

[Independent Python peer](../poc/round40-tests/Networking/peer.py) creates literal synthetic bytes and independently calculates raw-key order, last-value-wins snapshots, counters, summaries, work and intermediate caps. [Native scenarios](../poc/Stardust.Core.SmokeTests/CreatureSkillModsView4Scenarios.cs) run the actual separate Core assembly and source-linked standalone. Both pass122 cases,24719 negative checks,2808 budget checks and1065 ownership/retention checks. Exact exports agree with the independent oracle.

Coverage includes all three opcodes, both upsert effects, equal pairs, missing remove, ignored remove operands, raw/embedded-NUL/high-bit/empty keys, duplicate snapshots/arbitrary ignored raw commands, short/extended prefixes including65534/65535 bytes, future/stale/partial/wrapped counters, final counter repair, repeated fields, growth/removal intermediate peaks, recipient/revision failure and late rollback. Deterministic random cases include2 base-only and4 bonus-only changes. Every truncation cut is checked for small messages; large key fixtures use sampled cuts. Unsupported opcodes are all rejected. Message and kernel exact/one-less limits, aggregate raw seed limits, readonly collections and aliases are checked.

Accepted39 field2 and accepted38 complete-baseline/scalar regressions pass against the current Core DLL and their independent peers. Shared64-bit Core smoke builds0 warnings/errors and passes18 allowlisted journal events/0 failures. No actual wire capture/live occurrence is claimed; every fixture is synthetic. See the [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-03_R40_CREO4_SKILL_MAP.md) for input pins, exact commands and corrected attempts.

Next: close exact field13 mission-set reader0043E510, comparator/full-pair identity, skip/counter and duplicate/no-op rules before complete CREO4 composition. Subscriber/gameplay installation and state-aware unknown-opcode tolerance remain separate probes at0044CBD0/0044CD50/00452BA0 and0043F510/0043F5DE. Function/name/protocol totals and research/readiness scores are unchanged.
