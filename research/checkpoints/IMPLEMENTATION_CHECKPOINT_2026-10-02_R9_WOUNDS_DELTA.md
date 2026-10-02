# 2026-10-02 R9-NET-01: atomic CREO:3 wounds deltas

Bounded question: can an engine-neutral field-17 codec decode the recovered integer-vector grammar into an immutable plan and apply it without partial state publication? Stop at this isolated codec/state boundary. World/store integration and exact original counter/growth recovery are separate probes.

## Inputs and evidence

Accepted dirty-main Round9 baseline: HEAD `65de8c88141071744dc75594be126b3bab3696a5`, 1,963 public files. Archive SHA-256 `da266e684b8318aa605b0ef8730331915f7ba803bdccb2b693a2402ad9017dd8`; manifest SHA-256 `29bdfd7c8bac3898aa219d68d0b9189b518c7f7934a1b1c1e248fddf494a4b56`. Relative ZIP paths, membership, all content hashes and destination containment were checked before the overlay; ignored/unlisted private files were preserved.

Original executable SHA-256 rechecked statically: `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`. No new VA investigation or executable execution. Existing [replication schema](../../docs/REPLICATION_SCHEMA_MAP.md#vector-delta-operations), [September11 checkpoint](ANALYSIS_CHECKPOINT_2026-09-11_BLOCK2.md#malformed-payload-behavior) and accepted [Round8 baseline](IMPLEMENTATION_CHECKPOINT_2026-10-02_R8_CREATURE_BASELINES.md) supply the structural evidence. Canonical provisional row `0043B1A0` retains its existing name; no registry changes.

Server correlation inputs in sibling Stardust/Core3 (HEAD `9a8529ef8291efc5ca1b41d9927f0e88d64b79bb`): `MMOCoreORB/src/server/zone/objects/scene/variables/DeltaVector.h` SHA-256 `3A56C8F4FC7B5DD610A227B9F17408615BE1F179251598FB61C15B686416DB52`; `MMOCoreORB/src/server/zone/objects/creature/CreatureObjectImplementation.cpp` SHA-256 `8C69D2129FF65F81CBD24485BD5DE4B7AE216C3976B70F804C4864017115DA93`. Exact symbols/lines are cited in the [durable contract](../../docs/CREATURE_VIEW3_WOUNDS_DELTA_CONTRACT.md). Read-only source correlation performed; no server contact or writes.

Observation: CREO:3 index17 is the ordered integer vector; existing executable evidence establishes U32 operation count/counter and commands 0remove,1insert,2replace,3assign,4clear with U16 indices/assignment counts. The client consumes represented prefixes and may ignore/grow invalid positions, with partial legacy mutation on later errors. Interpretation: structural decode can be separated from state policy and transactional publication. Confidence: Confirmed structural evidence; exact original modulo arithmetic and out-of-interval stale handling remain Unknown. Compatibility consequence: the modern strict modulo-interval/index policy is explicit and cannot be counted as recovered original counter/growth behavior.

## Result and policy

Added the immutable wounds state/command/update/plan, full ordered decode/encode, accepted baseline seeding, and atomic Apply. Only field17 is supported; mixed fields reject as a whole. Raw declared counts and counters remain preserved. Multiple field17 updates apply sequentially inside one atomic operation. Decoding includes skipped commands and all bounds/budgets. No world binding, callbacks or engine dependency.

Modern rule: packet interval starts at `(incomingCounter - commandCount) mod 2^32`; local distance from start must be at most command count. Skip that distance, apply the suffix and publish the incoming counter after success. Counter gaps/ahead/wholly older packets reject; zero-command updates require equal counters. Remove/replace require an existing index; insert allows at most append. Implicit growth/zero filling and invalid-index ignore are deliberately excluded. The contract states these divergences and the unresolved original behavior.

Limits independently bound payload, fields, aggregate commands/decoded values and transient state elements. Commands and values are budgeted even in stale prefixes. State limits are logical entries, not a total heap cap. Inputs and output snapshots stay immutable; late failures produce no observable update. Managed temporary allocations have no external resource/cleanup lifecycle.

## Reproduction

Run from the isolated checkout in PowerShell. Use cached dependencies, an ignored NuGet config with cleared package sources, and these commands:

```powershell
$py = 'C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$env:PYTHONUTF8 = '1'
$env:APPDATA = Join-Path $env:TEMP 'stardust-appdata-networking-r9'
New-Item -ItemType Directory -Force $env:APPDATA | Out-Null
$env:NUGET_PACKAGES = 'C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
New-Item -ItemType Directory -Force local-private/round9 | Out-Null
Set-Content local-private/round9/NuGet.Config '<configuration><packageSources><clear /></packageSources></configuration>'
& $py tools/python/verify_creature_view3_wounds_deltas.py --generate local-private/round9/fixtures
dotnet restore poc/round9-tests/Networking/Networking.csproj --configfile local-private/round9/NuGet.Config -p:NuGetAudit=false
dotnet run --no-restore --project poc/round9-tests/Networking/Networking.csproj -p:UseSharedCompilation=false -- local-private/round9/fixtures local-private/round9/export
& $py tools/python/verify_creature_view3_wounds_deltas.py --verify local-private/round9/export
dotnet build --no-restore poc/Stardust.Core/Stardust.Core.csproj -p:UseSharedCompilation=false
dotnet restore poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj --configfile local-private/round9/NuGet.Config -p:NuGetAudit=false
dotnet build --no-restore poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj -p:UseSharedCompilation=false
& $py tools/python/validate_organized_workspace.py .
git diff --check
```

Python hand-packs the main 82-byte delta (SHA-256 `4c58d736359e494d8a0ca540e58c21a73974e69bd2f3d046bb6cfea0c84152fd`) and the 152-byte synthetic complete baseline with wounds counter `FFFFFFFD` (SHA-256 `02e2e8e5a4b86efa3ddeb114dc8c433399b596a72a900c66ddeca9b709016009`). No captured or proprietary bytes are used. All generated exports stay ignored. Native outputs are encoded binaries, full plan JSON, baseline-seeded result and fifteen accepted/rejected application cases; Python independently compares wire bytes, every command and final states. The synthetic baseline helper is the accepted R8 Python oracle, imported read-only.

Native negative/boundary tests cover all wire truncations, corrected-envelope partial fields, tail/mixed fields, wrong envelope/tag/view, unknown commands, huge command/assignment counts, each budget and aggregate repeated-field limits, counter gap/ahead/duplicate/prefix/wrap cases, invalid indices/implicit growth, transient element caps, mismatch object IDs, rollback after a late field failure, empty vectors/assignments/envelopes, full U16 assignment count and maximum index for insert/replace/remove, signed extremes, raw declared counts 0/1/65535, borrowed-input independence and immutable lists.

Focused native scenarios and independent oracle passed. Core and shared smoke-test projects built with zero warnings/errors. The shared full suite was built, not executed; the new scenario was executed through the isolated runner. For lead integration, call `CreatureView3WoundsDeltaScenarios.Run()` from the shared runner. Existing public files and lead-owned shared runner/ledgers remain untouched.

## Resume point

Recover exact original `0043B1A0` counter arithmetic, including wraparound and packets wholly behind the local counter, plus integer-vector growth/ignore details; compare against the modern interval/index fixtures before changing policy. Then compose wounds and scalars into a stable-incarnation store transaction. No broad/mixed delta or live interoperability claim. Stop after Round9; lead acceptance must precede Round10 dispatch. Research/implementation/function/protocol metrics remain unchanged.

Final workspace validation: 1,081 Markdown files, zero broken/private links, 2,602 provisional rows with zero duplicates, and 345 packet rows. Scope review confirms exactly seven permitted additions and all 1,963 baseline files unchanged; HEAD/index unchanged, diff whitespace check passed, and new files checked for UTF-8/LF/ASCII text and trailing whitespace. Final changed-file SHA-256 manifest and status are retained in ignored Round9 evidence.
