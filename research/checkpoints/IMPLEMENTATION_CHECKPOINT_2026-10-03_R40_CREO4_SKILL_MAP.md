# Round40 CREO4 field3 skill-map milestone

Date:2026-10-03. Question: what exact bytes, key identity/order, mutation/counter and ownership rules does field3's actual client specialization implement? Stop after independently auditing reader/writer/comparator/snapshot/mutations/callback/teardown, implementing one field3-only component over accepted38's baseline and validating separate assemblies against an independent synthetic oracle. No Runtime40 or sibling dependency is used.

## Result and evidence

[Durable contract](../../docs/CREATURE_SKILL_MODS_VIEW4_CONTRACT.md) is the canonical conclusion. New [codec](../../poc/Stardust.Core/Protocol/CreatureSkillModsView4Codec.cs) preserves raw command order, signed pair values, byte keys and short/extended prefix choice for opcodes0/1/2. New [kernel](../../poc/Stardust.Core/Protocol/CreatureSkillModsView4Kernel.cs) projects raw baseline duplicates into a sorted last-value-wins map, preserves the exact wrapped skip and unsigned final-counter rule, and publishes immutable modern summaries transactionally. Complete CREO4 composition is deferred.

Fresh original SHA256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150` precedes every new VA probe. Cached pefile/Capstone provide static reads only. Vtable015DCE70 and full RTTI confirm narrow basic_string -> pair<int,int>, CreatureObject owner; reader0043F490/writer0043F080/snapshot0043F190/writer0043EF90/pending-clear0043ED30/destructor00445870->0043EBF0.144 new exact anchors plus accepted38's107 anchors/fourteen registrations pass. Wrong/truncated identity tests reject before imports/VA use.

Observations: reader0/2 share upsert branch0043F5E4,1 removes via0044CBD0 after consuming both numeric operands; comparator00447390 uses unsigned length-aware byte comparisons. Snapshot0043F353/35F always writes the pair, so last duplicate wins. Equal existing pairs compare through00454560; missing removes return before mutation. Skip first compares wrapped local+extent to incoming0043F4E9..EE, and end0043F81C..821 raises counter only when unsigned smaller. Input labels do not determine pending effect opcode. Exact structural callbacks are remove+28/insert+2C/change+30 with member-function this adjustment; insertion/change increment after callback/queue, removal increments before erase/callback. Subscriber behavior remains unknown.

Interpretation/confidence: wire shapes/types, exact byte identity/order, duplicate materialization, equality/no-op/counter rules, callback structure and ownership are **Confirmed** by direct instructions plus independent writer/RTTI/helper evidence. Role names base/bonus are accepted server correlation, not inferred from an analogous map class. Active subscribers, gameplay, reentrancy and live occurrence are **Unknown**. Consequence: no UTF8/case-fold/C-string interpretation or server-only mutation assumption; modern canonical prefix, sorted-list work model, bounded ownership, revisions and atomic publication are explicit policy.

Supported subset:0/1/2 only. Applied opcodes3..255 consume one byte and no operands at0043F5DE->0043F807; skipped ones consume a full key/pair at0043F510..5A9. This known context-dependent tolerance has no state-independent grammar; all such opcodes reject in the narrow codec. No clear/replacement behavior is invented. A future state-aware tolerance component must name its framing/state dependency separately.

## Validation

Actual Core ProjectReference and source-linked standalone pass122 independent literal wire/map/rawkey/counter/event/work cases; exports are identical and match Python. Each reports24719 negatives,2808 exact/one-less budget checks,1065 ownership/retention checks. Tests include65534/65535 key-prefix boundaries, binary/empty/NUL keys, duplicate snapshots/raw commands, both pair equality branches, future/stale/partial/wrapped counters, counter-only records, repeated updates, intermediate growth/removal and late atomic failure. Small messages test every truncation offset; large key messages use deterministic sampled offsets.

Frozen39 field2 regression passes121 cases/13994 negatives/1748 budgets/806 ownership and its independent peer on current Core DLL. Frozen38 complete CREO4 baseline/scalar regression passes40 cases/151672 negatives/1592 budgets/471 ownership and its existing peer. Shared64-bit Core smoke builds0 warnings/errors and passes18 journal events/0 failures. No original process, external server contact, download, staging or publishing occurs.

Corrected attempts: one invocation had an extra python path segment and failed before executing the saved creation helper; corrected to the existing bundled interpreter. First scenario build caught the alias test local `commands` colliding with the nested command-count variable; renamed the alias local, rebuilt and passed. The attempted runner after that failed build had no DLL and did not execute tests. No codec/kernel/oracle assertion failed. No unavailable input or toolchain blocks verification.

## Reproduction

From worker root, use UTF8/no bytecode, private APPDATA, existing ignored NuGet cache and a private NuGet.Config with packageSources/clear. No installation/download is needed:

```powershell
python poc/round40-tests/Networking/static_audit.py --exe C:/Stardust-DEV/Stardust.exe --packages C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/toolchain/python-static-analysis-packages --receipt local-private/round40/static.json
python poc/round40-tests/Networking/peer.py generate local-private/round40/peer.json
dotnet restore poc/round40-tests/Networking/Networking.csproj --configfile local-private/round40/NuGet.Config -p:NuGetAudit=false
dotnet build poc/round40-tests/Networking/Networking.csproj --no-restore -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/round40-tests/Networking/bin/Debug/net8.0/Networking.dll local-private/round40/peer.json local-private/round40/export.json
python poc/round40-tests/Networking/peer.py verify local-private/round40/peer.json local-private/round40/export.json
```

Standalone uses Networking.Standalone.csproj, the same flags and absolute private BaseIntermediateOutputPath/OutputPath ending in round40/standalone-obj/ and standalone-bin/. Run Networking.Standalone.dll against peer.json/standalone-export.json, then verify with the same Python peer. Copy the current Core DLL into each existing ignored prior runner output and run accepted39/38 against their retained peers. Existing38 syntax is --peer/--verify;39 uses positional verify. Build/run shared Stardust.Core.SmokeTests with the same offline flags.

```powershell
python tools/python/validate_organized_workspace.py .
python tools/python/parallel_round_workspace.py review --root . --worker . --baseline C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/parallel-development/round40 --allowed local-private/round40/allowed.json
git -c core.safecrlf=false diff --check
git status --short
```

## Scope, pins and resume

Input baseline3127 public files, source HEAD5493338d9cd4255cf58f25de6c4cd15f6507b818. Manifest SHA `32f715040a532c164c056e76d2eb9547f5b592edb3dc3dda2090c5afbde62bfe`, ZIP SHA `c46605bde6a8c9b6e698de63d4395da2929b20d0f9615c8d3399b42d2b8f7d95`. Overlay completed once after full archive/member/path/casefold/size/hash/containment and lstat target/ancestor reparse preflight, followed by full postflight. It is not rerun. Private receipt verifies ten scoped new additions, all3127 frozen hashes, HEAD65de8c88141071744dc75594be126b3bab3696a5 and empty index; it also checks Python AST, project XML, UTF8/LF/whitespace, links and focused scope. Generated/private original-derived evidence is not promoted or staged.

Permitted changes are five assigned new files and five own poc/round40-tests/Networking files only. Frozen shared ledgers/status/registries/docs remain immutable. Lead acceptance reconciles this result into living records and updates the current skill-map gap without rewriting dated history. No coverage score or denominator is combined or changed;2626 semantic-address rows/zero duplicates and345 packet rows remain unchanged.

Open boundary: complete field13 mission-set delta and complete CREO4 publication; subscriber installation/gameplay/reentrancy and live interoperability. Next exact probe: field13 reader0043E510 with its exact vtable/comparator/full ID-pair identity, command/counter/skip, duplicate/no-op, callbacks and teardown; build its own bounded oracle before composition. Unknown-opcode field3 tolerance would require a separate state-aware framing API, not expansion by guessed payload shape. Lead accepts the finished handoff before dispatching further work.
