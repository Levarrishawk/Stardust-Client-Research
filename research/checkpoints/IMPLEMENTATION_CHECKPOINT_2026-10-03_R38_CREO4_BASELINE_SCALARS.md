# Round38 CREO4 complete snapshot and scalar state

Date: 2026-10-03. Worker: Networking, Low. Bounded question: what is the actual client fourteen-field CREO4 snapshot grammar, and can a raw owned baseline plus independently closed scalar-only delta/state preserve every field, entry, counter and IEEE bit under exact global limits? Stop condition: direct registration/type/reader/writer/lifecycle evidence and independent byte/model/event/work/cap oracles agree with both actual-Core-assembly and standalone native runners. Collection deltas remain explicitly rejected.

## Inputs, orientation and scope

Read START_HERE, [accepted37](../sessions/PARALLEL_DEVELOPMENT_ROUND_37_2026-10-03.md), the [September15 CREO4 checkpoint](ANALYSIS_CHECKPOINT_2026-09-15_CREATURE_VIEW4_DELTAS.md), relevant replication schema, prior vector/complete CREO6 contracts, status/ledger/unknown-function entries, curated function rows and existing static/PE tools. The older checkpoint's server container correlation was not promoted to client proof.

Round38 frozen source has3030 public files. Manifest SHA256: `d28af4bf1510c02eb3e6f91b1232fc9d7e3318021a294691c4366cd662a8ba76`; archive SHA256: `c60ea7c6452d3cbf4920373f19517973afdf9c95b48dc6b8f28b9043f948ee36`. Source HEAD is `5493338d9cd4255cf58f25de6c4cd15f6507b818`; worker HEAD remains `65de8c88141071744dc75594be126b3bab3696a5` and index remains empty. Approved overlay verified member/listed sets, sizes/hashes, uniqueness/casefold, canonical containment and every existing target/ancestor reparse/symlink before the first write, then posthashed all3030. Private/unlisted files and actual HEAD/index were preserved.

Original input is C:/Stardust-DEV/Stardust.exe, freshly hashed as `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150` before address use. Every audit repeats that gate before importing PE/disassembler packages. Only bounded static reads were performed; no original execution, live contact, downloads, captures or original assets were used. Detailed disassembly/receipts/fixtures/exports stay ignored/private.

Ten public additions: two Protocol files, one SmokeTests class, the [durable contract](../../docs/CREATURE_VIEW4_BASELINE_AND_SCALARS_CONTRACT.md), this checkpoint, and own poc/round38-tests/Networking/{peer.py,static_audit.py,Program.cs,Networking.csproj,Networking.Standalone.csproj}. Existing production/shared documents remain frozen. Canonical status/ledger reconciliation belongs to lead acceptance. Research89%/readiness94%,2626 stable addresses/zero duplicates and345 packet-source rows are unchanged and are separate measures.

Tools: bundled Python3, existing private pefile/Capstone packages, .NET8.0.300, cached pinned BCnEncoder.Net2.3.0 and cleared offline package sources. Existing static audit/probe machinery was reused before creating the new bounded verifier. No generated private output is canonical data.

## Evidence, interpretation and compatibility consequence

Observation:0042ECF0 installs exact vtables into legacy members and registers fourteen fields through00555440 at0042F7C0..0042F876. Registry+C4 dispatch00554D6C/72 and snapshot loop0118B740 read raw U16 metadata but traverse registered fields, not declared cardinality. Interpretation: fixed ordered fourteen-field snapshot, independent raw metadata. Confidence: Confirmed direct instructions/RTTI/slot/register chain. Consequence: preserve count0/14/65535 and require all fourteen field bodies in the modern decoder.

Observation: ten F32 fields use015DCF0C and00439B30/00439AC0; listen6 uses015DCDE0 and00440310/00440290. Snapshot post-read copies raw words, and deltas00439B50/00440340 read the same4/8-byte widths. Interpretation: raw IEEE storage and U64 identity are authoritative input, without decoder finite/clamp substitution. Confidence: Confirmed. Consequence: uint bits preserve signed zero, denormal, NaN payload/signaling bits and infinities; runtime eligibility remains separate.

Observation: encumbrance2 uses015DD03C DefaultObjectType S32 AutoDeltaVector,0043C070/0043B700. Construction zero-fills nine initial values and counter0, but snapshots clear that vector, read U32 count/counter and consume exactly count S32 elements without a nine-value check. Interpretation: constructor shape differs from generic wire shape. Confidence: Confirmed. Consequence: modern codec accepts bounded variable counts; no gameplay nine-stat clamp is added.

Observation: skill3 uses015DCE70 byte-string/S32-pair AutoDeltaMap,0043F190/0043EF90. Its snapshot consumes U32 count/counter and U8 raw command, U16-or-FFFF/U32 length/key bytes, S32 base/bonus. The consumed command is never inspected in the entry loop, while the client writer emits0. Interpretation: baseline structure and tolerated command spelling are established independently of current server ADD/DROP/SET semantics. Confidence: Confirmed direct reader/writer and absence-of-use audit. Consequence: retain raw entries/order/duplicate keys/spelling; do not implement unproved client map deltas.

Observation: mission13 uses015DCEDC AutoDeltaSet<pair<NetworkId,NetworkId>>,0043E380/0043DE40; both snapshot directions use U32 count/counter then two U64s per entry without a command byte. Interpretation: actual client pair set, despite server map-like interface. Confidence: Confirmed direct RTTI/reads/writes. Consequence: own raw pairs and duplicates/order as wire evidence; do not infer owner-key overwrite/equality or map mutation rules.

Observation: vector/map/set snapshot paths clear existing state, install received counters, create/destroy element temporaries and conditionally notify local holders. Mission callback00438E90 is installed at0042F8AE/B3. Exact destructors free holders/container backing data, key strings and ID temporaries. Checked prefix reads can throw after earlier mutation; legacy skill content construction uses pointer+length, not the new safe span preflight. View4 snapshot dispatch reaches cleanup without inspecting final cursor;0118AC90 only clears iterator reference. Interpretation: original snapshot boundary tolerates metadata/tails and lacks whole-message atomic rollback; subscriber consequences are separate. Confidence: Confirmed local paths; complete downstream gameplay Unknown. Consequence: modern strict EOF/truncation/global bounds and immutable atomic failure are explicitly modern choices, with no owner/movement/world side effects.

Server correlation is exact but lower-order evidence: Stardust-3 `MMOCoreORB/src/server/zone/packets/creature/CreatureObjectMessage4.h::CreatureObjectMessage4`, lines13–62, gives getter names/order and mission pair emission; `objects/scene/variables/DeltaVectorMap.h::insertToMessage`, lines140–153, emits map count/counter/ADD0/key/value; `objects/creature/variables/SkillModEntry.h::toBinaryStream` independently correlates the numeric pair order to skillMod then skillBonus. Semantic role names are Strongly inferred from this correlation; it does not establish client container delta behavior.

## Implementation and validation

[Codec](../../poc/Stardust.Core/Protocol/CreatureView4Codec.cs) provides immutable raw baseline/entries/counters and ordered scalar Delta/Update, strict complete snapshot EOF, whole-message preflight, exact encode and public envelope overloads. Eleven scalar indices0/1/4/5/6/7/8/9/10/11/12 are supported;2/3/13/unknown indices and empty deltas reject. Constructor/envelope copies are separately hard bounded. [State](../../poc/Stardust.Core/Protocol/CreatureView4State.cs) retains complete Model and checked revision, preserves all collection bytes/counters and baseline metadata, and publishes one candidate after whole-plan preflight. Assignment descriptions include old/new bits and cost24 logical bytes each; equal assignment still emits a modern description. No original callback equality/coalescing claim is made.

Message units: payload=field bytes+2, decoded=field bytes+10, baseline work14+elements+key contents, scalar work=updates. State bytes=72 fixed +4 per encumbrance +16 per mission pair +9 per skill entry plus actual string prefix/content. Seed work14+elements+contents; Apply work=seed work+updates*(1+seed work). Because scalar widths and retained collections are constant, event/output/work maxima occur at the final prefix; all totals are preflighted before allocation. Logical units are explicit modern policy, not measured heap/CPU limits.

Both assemblies pass **40 exact cases,151672 negatives,1592 budget checks and471 ownership checks** with zero build warnings/errors. Independent Python derives expected raw bytes/models/counters, events, revision/work and exact caps without native inputs. It verifies exports from the separate actual Core ProjectReference and source-linked standalone runners. Fixtures cover all eleven scalars, twelve IEEE edge patterns, raw U64 extremes, repeated/equal overwrite order, variable vector lengths0/1/8/9/10/31, all raw snapshot command values0/1/2/3/128/255, duplicate/nontext/empty keys, duplicate/zero/full-width pairs, short/extended spellings,65536-byte key and aggregate strings, seeded raw patterns, foreign/reseed/revision failures, every truncation/adjusted-prefix classification and collection/scalar/string aliases. Exact and one-less per/aggregate message/model/output/work caps pass.

Static audit passes **107 instruction anchors,14 registrations and5 RTTI/vtable specializations**, including readers/writers, defaults, lifecycle/cleanup and scalar raw-word copying. Wrong and truncated inputs reject before package imports or VA use. Current-Core full CREO6 regression passes97 cases/148202 negatives/3335 budgets/967 ownership checks and Python verification; shared64-bit Core smoke build/run passes zero warnings/errors.

Corrected attempt: while strengthening raw U64 post-read assertions, two instruction operands were initially assigned to swapped addresses. The verifier rejected that assertion; the direct disassembly showed stores at00440263 and load at00440266. Corrected anchors were replayed successfully and expanded to the reader/writer/lifecycle total107. A private text-generation helper produced a Python invalid-escape SyntaxWarning; the public audit uses valid raw regex strings and parses cleanly. No codec/state/native test failed. Missing-file discovery probes were redirected to the existing static_audit.py/tool names; no missing input prevents validation.

## Reproduction and handoff gates

From worker root, enable UTF8/no bytecode, configure the existing ignored cached NuGet package directory and a private APPDATA. Private NuGet.Config has packageSources/clear only. Detailed commands and exports remain private; representative commands are:

```powershell
python poc/round38-tests/Networking/static_audit.py --exe C:/Stardust-DEV/Stardust.exe --packages C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/toolchain/python-static-analysis-packages --receipt local-private/round38/static.json
python poc/round38-tests/Networking/peer.py --generate local-private/round38/peer.json
dotnet restore poc/round38-tests/Networking/Networking.csproj --configfile local-private/round38/NuGet.Config -p:NuGetAudit=false
dotnet build poc/round38-tests/Networking/Networking.csproj --no-restore -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/round38-tests/Networking/bin/Debug/net8.0/Networking.dll local-private/round38/peer.json local-private/round38/export.json
python poc/round38-tests/Networking/peer.py --peer local-private/round38/peer.json --verify local-private/round38/export.json
```

For Networking.Standalone.csproj, use the same flags plus absolute private BaseIntermediateOutputPath and OutputPath ending in local-private/round38/standalone-obj/ and standalone-bin/. Run Networking.Standalone.dll against peer.json/standalone-export.json and verify through the same independent Python command. Build/run Stardust.Core.SmokeTests with the same flags. Replay accepted37 against the current Core DLL and its retained peer, then verify independently.

```powershell
python tools/python/validate_organized_workspace.py .
python tools/python/parallel_round_workspace.py review --root . --worker . --baseline C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/parallel-development/round38 --allowed local-private/round38/allowed.json
git -c core.safecrlf=false diff --check
git status --short
```

Final receipt hashes all ten additions and every3030 frozen file; it checks exact additions-only scope, unchanged HEAD/empty index, UTF8 without BOM/LF/final newline/no trailing spaces, Python AST/project XML and zero broken/private Markdown links. No generated/private material is staged or promoted.

Remaining uncertainty: all three collection delta specializations, raw-entry-to-runtime canonicalization policy, complete callback subscribers/gameplay consequences, finite movement eligibility, store/world incarnation ownership and live interoperability remain open. Exact next probe: isolate encumbrance field2's actual DefaultObjectType delta/equality/default/notification kernel at0043C1B0 and helpers, rather than assume HAM's CreatureObject specialization; implement one bounded collection delta component only after that closure. Lead accepts/reconciles this milestone before new work.
