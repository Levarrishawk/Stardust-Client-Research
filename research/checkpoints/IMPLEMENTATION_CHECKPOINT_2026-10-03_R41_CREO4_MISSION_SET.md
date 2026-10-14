# Round41 CREO4 field13 mission-set milestone

Date:2026-10-03. Question: what exact command bodies, full-pair identity/order, counter/skip and lifecycle rules does actual field13 implement? Stop after reader/writer/comparator/snapshot/mutation/callback/teardown closure, one bounded field13 codec/kernel and independent fixture/negative/budget/ownership gates. No Runtime41 or unfinished sibling is used.

## Result and evidence

[Durable contract](../../docs/CREATURE_MISSION_OBJECTS_VIEW4_CONTRACT.md) is canonical. New codec (external historical reference) retains ordered field13 records/raw U64 pair bits/U32 counters/extents/U16 metadata for remove0/insert1/clear2. New kernel (external historical reference) projects raw baseline records into an immutable complete-pair set with signed64 lexicographic ordering, exact mutation no-ops and final unsigned counter reconciliation. Modern bounds, revisions, sorted-list work, summary events and atomic publication are explicit policy.

Fresh original SHA256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150` gates every new VA probe. Existing cached pefile/Capstone perform static reads only. Exact015DCEDC vtable/RTTI bind snapshot0043E380/writer0043DE40, delta0043E510/writer0043DEB0, pending-clear0043DDA0, destructor00445690->0043DC50.135 new exact anchors plus accepted38's107 anchors/fourteen registrations pass; two wrong/truncated identity tests reject before imports/VA use.

Observation: comparator00450840 uses signed high-word comparisons then unsigned low-word comparisons for **both** IDs; insertion0044FAB0 and erase bounds0044C4B0/0044C440 independently agree. Delta0 removes exact pair,1 inserts exact pair,2 clears. Duplicate/missing/empty guards suppress local mutation/counter/notifications. Skip0043E57D..590 and final repair0043E7ED..7F2 match the field3-style wrapped sum/unsigned rule. Snapshot inserts pairs into the set and collapses exact duplicates, preserving received counter. Clear invokes remove callbacks while entries still exist then clears and aggregates; constructor installs aggregate00438E90 which walks subscribers.

Interpretation/confidence: full-pair set structure, raw widths, signed ordering, command/counter rules, duplicate materialization, callbacks/lifetime are **Confirmed** direct instructions/RTTI/reader/writer/helper evidence. Owner/mission labels are accepted correlation. Actual subscribers/gameplay, reentrancy and live occurrence are **Unknown**. Consequence: preserve U64 bits but compare signed64; same-owner/different-mission pairs must coexist. Do not infer an owner-key map or callback replay from server terminology.

Supported subset0/1/2. Applied unknown opcodes consume only the opcode at0043E611->7D8; skipped unknowns other than2 consume a full pair at0043E5AF..5C4. This context-dependent tolerance rejects in the state-independent codec. No guessed command or recovery policy is added.

## Validation

Actual Core ProjectReference and standalone both pass122 independent synthetic wire/fullpair/signed-order/model/counter/event/work cases with identical oracle-verified exports. Each reports21568 negatives,1976 exact/one-less budget checks and807 ownership/retention checks. Cases include100 ID high/low combinations, signed/unsigned-order distinction, both identity parts, raw snapshot duplicates,65536 raw duplicate seeds/65536 command records, all three commands/no-ops/skip/gap/wrap/final repair, repeated records, intermediate growth/clear and late rollback. Small messages test all truncation cuts; large messages sample deterministically.

Frozen40 skill-map replay passes122 cases/24719 negatives/2808 budgets/1065 ownership on current Core DLL and its independent peer. Shared64-bit Core smoke builds0 warnings/errors and passes18 events/0 failures. No original process/live/server/GPU/download/staging/publishing occurs.

Exploratory attempts: one constructor-window disassembly began at an interior byte and emitted unaligned leading instructions; those bytes were not promoted. Valid constructor anchors come from the full constructor registration audit and exact target instruction reads. A candidate diagnostic address was nontext data, so no string/role claim was made. No compiler, codec/kernel/oracle or static-anchor assertion failed; no missing input/toolchain blocks validation.

## Reproduction

From worker root, use UTF8/no bytecode, existing bundled Python/cached NuGet packages, private APPDATA and private NuGet.Config with packageSources/clear; no downloads/installations:

```powershell
python poc/round41-tests/Networking/static_audit.py --exe C:/Stardust-DEV/Stardust.exe --packages C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/toolchain/python-static-analysis-packages --receipt local-private/round41/static.json
python poc/round41-tests/Networking/peer.py generate local-private/round41/peer.json
dotnet restore poc/round41-tests/Networking/Networking.csproj --configfile local-private/round41/NuGet.Config -p:NuGetAudit=false
dotnet build poc/round41-tests/Networking/Networking.csproj --no-restore -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/round41-tests/Networking/bin/Debug/net8.0/Networking.dll local-private/round41/peer.json local-private/round41/export.json
python poc/round41-tests/Networking/peer.py verify local-private/round41/peer.json local-private/round41/export.json
```

Standalone uses Networking.Standalone.csproj, same flags plus absolute private BaseIntermediateOutputPath/OutputPath ending in round41/standalone-obj/ and standalone-bin/. Run Networking.Standalone.dll against peer.json/standalone-export.json, verify with the same peer. Replay frozen40 runner against current Core DLL and retained peer, then independent verify; build/run shared Stardust.Core.SmokeTests.

```powershell
python tools/python/validate_organized_workspace.py .
python tools/python/parallel_round_workspace.py review --root . --worker . --baseline C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/parallel-development/round41/frozen --allowed local-private/round41/allowed.json
git -c core.safecrlf=false diff --check
git status --short
```

## Scope and resume

Authoritative frozen baseline3174 files is the **round41/frozen** directory, not the preliminary top-level snapshot. Source HEAD5493338d9cd4255cf58f25de6c4cd15f6507b818; manifest SHA `74cc0552f0715e52b782c9aec68dc003584fabbdd8987f55cc6684c971238ea8`, ZIP SHA `c788a27f07bb5d5b5c4613c6b54a015ecf673288e3518fadff2cad79dd94c746`.

Before the one overlay, every existing target had to match its accepted prior baseline, reviewed40 own byte hash or incoming byte hash; prior file deletions/unreviewed changes rejected. Full archive/member/path/casefold/size/hash/containment and every lexical root/ancestor/target lstat symlink/junction/reparse condition preceded first write. All3174 postflight hashes passed, with HEAD/index/unlisted bytes preserved. Successful overlay is not rerun.

Private handoff receipt hashes exactly ten new scoped additions and every3174 immutable input; it verifies actual HEAD65de8c88141071744dc75594be126b3bab3696a5, empty index, links/whitespace/UTF8/LF/Python AST/project XML and reparse checks. Scope is five assigned files plus five own poc/round41-tests/Networking files. Existing shared docs/ledgers/registries are immutable; lead owns reconciliation into living session/status records.2626 address rows/zero duplicates,345 packet rows and separate research89%/readiness94% scores remain unchanged. No original assets/executable/bulk output/private fixtures are promoted or staged.

Exact next action: compose all reviewed CREO4 scalar/field2/field3/field13 kernels under one ordered, aggregate-budgeted atomic full-model publication seam, preserving raw snapshots versus canonical map/set projections. Subscriber/gameplay/reentrancy and live interoperability remain open; unknown-opcode tolerance needs a distinct state-aware framing design. Lead accepts the final milestone before new dispatch; this worker then idles.
