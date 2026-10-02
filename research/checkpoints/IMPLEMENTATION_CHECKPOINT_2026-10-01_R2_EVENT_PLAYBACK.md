# R2-RUNTIME-01 elapsed event playback — 2026-10-01

## Question and stopping condition

Can bounded elapsed seconds and FPS feed the accepted modern event cursor with transactional time accumulation and stale-generation rejection? Stop after engine-neutral bridge/owned batches, deterministic and pinned-original verification, shared compile and lead handoff. No preview-clock, scheduler, effects/audio, root motion, graph or Godot changes.

## Baseline and orientation

Checkout C:/Users/ancie/.codex/worktrees/9175/Stardust-Client; HEAD65de8c88141071744dc75594be126b3bab3696a5. Round2 manifest SHA256 E2AFB787D6CDB9F8C1971183D5A63CDC2B4736E24EC41E039F94FEEAF058D097; ZIP SHA256 CEBF4BD8017249ECC1FC9A9647900BD4062C134CA10040D5B2420C04A3CC4981. Verified both, validated every ZIP path/entry stays inside checkout (no traversal/symlink), verified every member hash/length before overlay, then verified all1735 destination hashes. Accepted Round1 files replaced by exact baseline; ignored/unlisted outputs preserved. Baseline plus HEAD defines review scope; Git-relative prior changes are not worker changes.

Reviewed AGENTS, START_HERE/latest named session, parallel rules, [Round1 acceptance](../sessions/PARALLEL_DEVELOPMENT_ROUND_1_2026-10-01.md), status/ledger/unknown terminology, existing cursor/preview clock, existing loader/overlay and message-boundary/locomotion evidence. No new executable address claim or research recovery; original scheduler remains unknown. Existing integer collector and cursor/preview clock stayed read-only.

## Input identities and evidence

Effective C:/Stardust-DEV/swgemu_live.cfg SHA25622158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524. Priority30 walk/run member SHA256 FF86567A4CDBFC1EB574C5C8FC399144A37DAD218AA49DE0070AC9F1F19F65A6 /72D5842D2959D8438884B44F4220F3E323D221A745D849CB8726E905A8AAF2F4. Existing ConfiguredTreOverlay and CKAT/CompressedLocomotion decoder load the originals without copying their bytes into public fixtures. Original FPS30/end33,22/messages7,23 and6,17 match [durable evidence](../../docs/COMPRESSED_LOCOMOTION_CONTRACT.md).

Observation: compensated active seconds times FPS produces the cursor's absolute unwrapped phase; all enumeration uses the accepted cursor. Interpretation: MODERN replacement bridge, including generalized authored-frame precision snap. Confidence Confirmed for tested behavior, not original scheduling. Consequence: host adapters may supply elapsed time without duplicating MSGS collection.

Observation: batches carry owner-local generation and immutable occurrence records; reset/restart/replacement increment generation, including same-source reset. IsCurrent checks both owner identity and generation. Confidence Confirmed by current/stale/foreign-owner and retained-batch cases. Consequence: consumers can reject stale asynchronous work without losing authored duplicates or conflating clip epochs.

Observation: invalid input/overbudget tick rejects before time/compensation commit; cursor itself rejects transactionally. Rejected tick then subdivisions equals fresh control. Pause/focus discard elapsed time; nonloop clamps/stops while accepting the final tick's complete duration. Confidence Confirmed modern behavior. Consequence: no hidden catch-up or partial output; callers choose bounded subdivisions explicitly.

## Validation

Exact PowerShell commands, using the existing privately copied package cache and sources-cleared permanent console configuration:

```powershell
$env:APPDATA = Join-Path $env:TEMP 'stardust-runtime-r2-appdata'
New-Item -ItemType Directory -Force $env:APPDATA | Out-Null
dotnet restore poc/round2-tests/Runtime/Runtime.csproj --configfile poc/round2-tests/Runtime/NuGet.Config --packages "$PWD/local-private/round1/nuget-packages" -p:NuGetAudit=false
dotnet run --project poc/round2-tests/Runtime/Runtime.csproj --no-restore -p:UseSharedCompilation=false -- C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg
dotnet restore poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj --configfile poc/round2-tests/Runtime/NuGet.Config --packages "$PWD/local-private/round1/nuget-packages" -p:NuGetAudit=false
dotnet build poc/Stardust.Core/Stardust.Core.csproj --no-restore -p:UseSharedCompilation=false
dotnet build poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj --no-restore -p:UseSharedCompilation=false
$env:PYTHONUTF8 = '1'
& C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe tools/python/validate_organized_workspace.py .
git -c safe.directory=C:/Users/ancie/.codex/worktrees/9175/Stardust-Client diff --check
```

Console PASS:1120 absolute-time/unequal-partition cases at seven varied FPS; exact/fractional/end/zero/duplicate events; loop/nonloop; pause/focus/resume/reset/restart/replacement; foreign-owner/stale-generation rejection; invalid FPS/delta; atomic event/cycle budget failure/retry; retained fractional snap accumulation; source/batch ownership. Both original clips PASS three cycles/six occurrences at120Hz, exact source/configuration identity, authored metadata, absolute interval oracle, pause and restart. Core/shared SmokeTests builds PASS zero warnings/errors. No full shared suite execution or Godot runtime run is claimed; acceptance requires compilation plus the isolated bridge suite. Exhausting long generation or the2^40 phase ceiling via long-running playback was not executed; guards are explicit and inherited cursor limits remain unchanged.

## Handoff and uncertainty

New files only: AnimationEventPlayback.cs, AnimationEventPlaybackScenarios.cs, unique contract/checkpoint and isolated round2 Runtime project/Program/NuGet.Config. Private baseline-relative file/hash manifest is generated for lead review; no baseline file change is intended. Shared runner registration: AnimationEventPlaybackScenarios.Run(); optional Original(root,configuration). Lead owns shared navigation/status/ledger updates and combined acceptance.

[Durable contract](../../docs/ANIMATION_EVENT_PLAYBACK_CONTRACT.md) states all time/generation/ownership/rounding/failure policies. Original scheduler semantics, callbacks, effect/audio, clip-graph transitions, engine threading and precision beyond the selected modern policy remain unknown/unimplemented. No score/function/protocol registry changes. Suggested next assignment: current-generation diagnostic batch consumption in an adapter; not begun. No original executable execution, network/DNS/socket contact, staging, commit or publication.

Final workspace validator PASS:1016 Markdown files/0 broken links/0 private links;2602 names/0 duplicate addresses;345 packet rows. Git diff --check passed with line-ending warnings from the accepted overlay. Focused added-file checks and the private baseline comparison confirm scope in the handoff. Representability rejection for an elapsed delta too small to advance accumulated time also passes. Scores/counts are observations, not new coverage claims.
