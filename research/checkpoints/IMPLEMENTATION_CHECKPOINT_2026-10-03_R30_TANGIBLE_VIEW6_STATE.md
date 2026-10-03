# Round 30: bounded owned Tangible view6 state

Date: 2026-10-03. Question: can the accepted TANO6 codec and specialized counter/mutation policy support an owned immutable state with deterministic events and bounded failure preservation? Stop condition: independent wire/state/event/work oracle passes, prior codec and shared smoke regression pass, and only the assigned seven files differ from the round snapshot.

## Inputs and evidence

Main round-30 snapshot manifest SHA256: `ba8f50e9b3c9fb5b378e5136bbbbfc0bcd14c2a9088cc401cef04d1ad90ded48`; ZIP SHA256: `58909621e901a194eb73ba54011b51a45399e819fe57128f3234e51af27d3c0a`. All 2725 unique members were checked for containment, bytes, hashes and symlink/reparse ancestors before overlay, then rehashed. HEAD/index and unlisted/private paths were preserved.

Behavioral evidence is inherited from the [round-29 specialized policy](../../docs/TANGIBLE_VIEW6_STATE_POLICY.md) and [accepted codec contract](../../docs/TANGIBLE_VIEW6_CODEC_CONTRACT.md). No new executable address claims or dynamic client execution occurred. The inherited baseline executable identity is SHA256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.

## Findings and confidence

Confirmed by deterministic native/oracle comparison: raw U64 values, duplicates/order, passive metadata, exact unsigned skip filtering, whole-assignment skip and final overshoot, zero-filled growth, equality/invalid/empty no-ops, modulo counter wrap and repeated-field recomputation. The implementation preserves the accepted codec boundary.

Confirmed replacement properties: independent owned nonempty ID storage, immutable previous/candidate/event receipts, complete-plan failure preservation, exact and one-less resource bounds, checked revision overflow, baseline reseeding and independent cross-thread branches. Whole-message atomicity and logical budgets are modern choices, not claims about legacy failure recovery. Revisions advance for successful skipped/no-op plans; optional operation limits replace and may widen retained policy. Message-byte budgets include the two-byte declared-count prefix.

The [durable contract](../../docs/TANGIBLE_VIEW6_STATE_CONTRACT.md) defines accounting, lifecycle and compatibility consequences. Original conditional callbacks are represented only by ordered descriptions; no actual original UI recipient is inferred.

## Reproduction and results

Use .NET 8 and Python 3 with offline package restoration available. Generate synthetic peer data into an ignored private directory, restore/build the isolated runner, execute it and verify its output:

```powershell
python poc/round30-tests/Networking/peer.py --generate local-private/round30/peer.json
dotnet restore poc/round30-tests/Networking/Networking.csproj --configfile local-private/round30/NuGet.Config
dotnet build poc/round30-tests/Networking/Networking.csproj --no-restore -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/round30-tests/Networking/bin/Debug/net8.0/Networking.dll local-private/round30/peer.json local-private/round30/export.json
python poc/round30-tests/Networking/peer.py --peer local-private/round30/peer.json --verify local-private/round30/export.json
```

The private NuGet.Config clears package sources; the runner has only the local Core project dependency. Fixture bytes are synthetic. The Python peer reuses the accepted round-28 independent wire constructor/parser but implements state/event/work policy independently of native state code.

Passed: 21 sequences and 23 transitions; 700 negative, 315 budget and 114 ownership checks; Python exact wire/state/ordered-event/work verification; zero native build warnings/errors. Accepted round-28 regression passed 17 exact, 394 negative and 148 global-cap checks plus its Python oracle. The shared Core smoke executable exited 0 after a zero-warning/error build.

The first native run exposed an overstrict test assertion: an empty immutable read-only collection may share its singleton identity. The assertion now permits that case while retaining independent-storage checks for nonempty IDs. The rerun passed; no production state change was required for this correction.

Workspace validation passed: 1288 Markdown files, zero broken links or links into private paths, 2626 provisional rows with zero duplicate addresses and 345 packet rows. Snapshot scope review passed; all seven assigned files are additions and the 2725 snapshot members remain unchanged. `git diff --check` passed, source files were checked/normalized as UTF-8 without BOM and LF with one final newline, and the index remained empty.

## Scope, uncertainty and next probe

Assigned changes are the state, native scenarios, durable contract, this checkpoint and three isolated Networking runner files. No registry, ledger, metric, protocol map or shared runner changes are needed for this bounded implementation; canonical names remain 2626 and packet rows 345. No files were staged, committed or published.

The state is engine-neutral and has no world registry, weak-target ownership or callback execution. Compatibility at current-incarnation selection and publication remains unknown. Next action: separately specify and fixture the caller's object-incarnation check and candidate publication/event delivery ordering before integrating this component into a scene registry.
