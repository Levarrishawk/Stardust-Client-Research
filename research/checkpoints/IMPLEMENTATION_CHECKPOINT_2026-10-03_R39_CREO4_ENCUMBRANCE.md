# Round39 CREO4 field2 encumbrance milestone

Date:2026-10-03. Question: what bytes and counter/list transitions does the exact field2 DefaultObjectType delta specialization consume, and can that boundary be implemented transactionally without assuming HAM behavior? Stopping condition: verify registration/vtable/read/write/skip/mutations/default/equality/callback/teardown, implement only ordered field2 deltas over accepted38's owned complete-baseline projection, pass independent literal wire/model/counter/event/work tests and preserve frozen public bytes.

## Result, facts and interpretation

[Durable contract](../../docs/CREATURE_ENCUMBRANCES_VIEW4_CONTRACT.md) is authoritative for this slice. New codec/kernel are engine-neutral and field2-only. The [complete CREO4 baseline/scalar API](../../docs/CREATURE_VIEW4_BASELINE_AND_SCALARS_CONTRACT.md) remains frozen. All five verified opcodes are supported; unknown opcodes and other fields reject. Raw signed values, incoming counters/extents, declared metadata, ordered duplicates and replacement overshoot survive round-trip. Modern limits/revisions/transactional publication/summary events are explicitly new policy. No gameplay nine-slot clamp is added.

Input original executable SHA256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`, freshly checked before VA analysis. Existing cached pefile/Capstone performed static reads only. The exact vtable015DD03C binds snapshot0043C070, snapshot writer0043B700, delta reader0043C1B0, delta writer0043B760, pending-clear0043B6C0 and destructor004454D0->0043B5D0.131 new exact instruction anchors plus accepted38's107 anchors/fourteen registrations pass. Reader/writer command jump tables are separately checked.0043C200 is an interior address, not the reader entry.

Observations:0043C1F8..206 computes wrapped skip/clamp;0043C243 and0043C538 count replacement values; removal length guard0043C2C8 and equality guard0043C414 suppress mutations/counter increments. Insert/set grow with zero through00448620. Replacement0043BD80 increments1+count regardless of equality; clear0043B870 suppresses empty mutation. Notification holders+28/+2C/+30/+34 map to general/remove/insert/set and are freed by0043B5D0. Clear invokes per-item remove callbacks then general; replacement invokes only general. Registry commit0118A970 calls slot+0C to clear pending commands; immediate callbacks remain distinct.

Interpretation/confidence: wire shapes, cursor/counter operations, zero growth, equality/no-op rules, holder structure and ownership are **Confirmed** from exact instructions/vtable plus independent writer. Field-role naming is accepted client/server correlation. Gameplay semantics, active subscribers/reentrancy and live message occurrence are **Unknown**. Implementation consequence: preserve raw vector behavior at wire boundary; apply any nine-stat gameplay validation later, and never equate modern summaries with legacy callback replay.

## Validation

Two native configurations pass against121 independently generated synthetic cases: actual separate Stardust.Core ProjectReference and source-linked standalone. Outputs match byte-for-byte and Python independently verifies wire/list/counter/event/work. Each reports13994 negatives,1748 exact/one-less budget checks and806 ownership/retention checks. Large65535-value replacement truncations use deterministic sampled offsets; small fixtures test every cut. Index65535 produces65536 values; a further insertion at maximum size rejects transactionally. No claim of exhaustive U32-sized streams is made.

Frozen accepted38 regression passes40 cases/151672 negatives/1592 budgets/471 ownership checks on current Core DLL, then passes its independent Python oracle. Shared64-bit Core smoke builds with0 warnings/0 errors and passes its allowlisted journal18 events/0 failures. Wrong and truncated executable files both reject before imports/VA use with an intentionally absent packages path.

Corrected attempts: first native build caught a misspelled placeholder reseed type; replaced with the real public CreatureView4Baseline signature, then rebuilt both configurations. The initial accepted38 peer verification command used the newer peer's positional syntax; corrected to its existing --peer/--verify flags and passed. A private audit-strengthening helper shadowed the PE variable with a Path; corrected the helper variable before promoting strengthened anchors. No wire/kernel oracle assertion failed. Missing-file discovery probes were redirected to the actual accepted38 names; all required inputs are available.

## Reproduction

Use bundled Python or an equivalent environment, UTF8/no bytecode, existing ignored cached NuGet packages, private APPDATA and a private NuGet.Config with packageSources/clear. No downloads or toolchain installation are needed. From worker root:

```powershell
python poc/round39-tests/Networking/static_audit.py --exe C:/Stardust-DEV/Stardust.exe --packages C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/toolchain/python-static-analysis-packages --receipt local-private/round39/static.json
python poc/round39-tests/Networking/peer.py generate local-private/round39/peer.json
dotnet restore poc/round39-tests/Networking/Networking.csproj --configfile local-private/round39/NuGet.Config -p:NuGetAudit=false
dotnet build poc/round39-tests/Networking/Networking.csproj --no-restore -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/round39-tests/Networking/bin/Debug/net8.0/Networking.dll local-private/round39/peer.json local-private/round39/export.json
python poc/round39-tests/Networking/peer.py verify local-private/round39/peer.json local-private/round39/export.json
```

Standalone: use Networking.Standalone.csproj with the same restore/build flags plus absolute private BaseIntermediateOutputPath and OutputPath ending in round39/standalone-obj/ and standalone-bin/. Run Networking.Standalone.dll with peer.json/standalone-export.json; verify using the same Python peer. Build/run shared Stardust.Core.SmokeTests. Replay accepted38's runner against current Core DLL and verify via `python poc/round38-tests/Networking/peer.py --peer local-private/round38/peer.json --verify local-private/round39/accepted38-export.json`.

```powershell
python tools/python/validate_organized_workspace.py .
python tools/python/parallel_round_workspace.py review --root . --worker . --baseline C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/parallel-development/round39 --allowed local-private/round39/allowed.json
git -c core.safecrlf=false diff --check
git status --short
```

## Scope and resume

Frozen input3081 files, manifest SHA `4cac8ae4fe7a5315d8958682151ae982ec85de7f41f089afc8e8694ccf3a6e65`, archive SHA `b390959b5cb8e53e40523ea3764e7388a7975ca5708f897873d557e95b92dacb`. Overlay was completed once with complete preflight/hash/postflight; successful overlay is not repeated. Public scope is exactly five assigned new files plus five new files in poc/round39-tests/Networking. Private receipt hashes all ten additions and3081 unchanged inputs; it checks HEAD65de8c88141071744dc75594be126b3bab3696a5/empty index, scope, links, whitespace, Python AST and project XML. No original executable/assets/bulk output or generated private files are staged/published.

This worker's permitted additions do not authorize editing frozen shared ledgers/status/unknown-functions/registries. Lead acceptance must reconcile this bounded contract into the session/navigation/current-status record and retire accepted38's current collection2 gap, while preserving its dated history. Research coverage, implementation readiness, function coverage and packet coverage are not combined or rescored here.

Unresolved: field3 skill-map and field13 mission-set deltas, complete CREO4 publication/store composition, callback installation/subscribers/gameplay mapping and live interoperability. Exact next action: inspect field3 reader0043F490 and its exact vtable/lifecycle helpers, close command/key/value/equality/drop semantics, then implement its bounded independent kernel. For encumbrance gameplay, trace holder installation/callers of0044C9A0/0044C9C0/0044C9E0/0044CA00 before adding subscriber behavior. Lead consumes the final bounded handoff before any next milestone.
