# R3-RUNTIME-01 diagnostic event delivery and original preview — 2026-10-01

## Question and stopping condition

Can current-epoch event batches be delivered to bounded diagnostic history and an original walk/run geometry preview without replay, stale publication or root/effect dispatch? Stop at new engine-neutral sink, own Node lifecycle/controls, permanent native suite, original headless fixture/cancellation and lead handoff. No GPU/visible run by this worker, no Round4.

## Baseline and orientation

Actual checkout C:/Users/ancie/.codex/worktrees/9175/Stardust-Client; HEAD65de8c88141071744dc75594be126b3bab3696a5. Round3 manifest SHA2563549F10F0AC0602E53837010475CF0139BFEA4D2C1CE2BB3148228887C12C9BC; ZIP201434FC21C1C6641658F8CE6055BDC4BCAB2FB25D2B190A2FA84B7E8B7B0A3F. Verified both, all relative ZIP paths/entry safety and every hash/length before writing; overlaid1762 listed files into this checkout and verified all destination hashes. Ignored/unlisted files preserved. HEAD plus manifest is the review baseline.

Read START_HERE/latest development session and [Round2 accepted report](../sessions/PARALLEL_DEVELOPMENT_ROUND_2_2026-10-01.md), AGENTS/parallel rules, status/ledger/unknown terminology, R1/R2 cursor/playback contracts/checkpoints, bank/locomotion contracts, existing bank/loader/mesh transition and asset-launcher tools. Reuse scope helper/overlay/profile/loaders/mesh; no new MSGS parser or scheduler. No new executable-address claim.

## Inputs, observation and confidence

Configuration SHA25622158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524. Object object/creature/player/shared_human_male.iff SHA256E0B61445E8A85CCF0AC5937F0F9CBCA8711718AFEE63384F30970D5AEE120243; SAT951FCE8C2FFC0EB318C751026D24D8C9BF17263D706EB3C93D015867EC801A9C. Idle798AD8D1BAB2BAF24042EE5276823651B1777F254C7FC97C2C4470F8EFEA292B; walkFF86567A4CDBFC1EB574C5C8FC399144A37DAD218AA49DE0070AC9F1F19F65A6; run72D5842D2959D8438884B44F4220F3E323D221A745D849CB8726E905A8AAF2F4. Original inputs read only from C:/Stardust-DEV via the configured overlay. Existing authored human-locomotion profile selects explicit routes0/1 and0/2; graph selection semantics are not inferred.

Observation: fixed ring plus single ordered occurrence frontier retains duplicates but rejects replay/backward delivery, including after history clear. Validated owner/generation gates precede publication; new epochs clear frontier/per-epoch history while retaining lifetime totals. Confidence Confirmed for tested modern sink behavior. Consequence: bounded diagnostic delivery without a permanent growing ID set; callers must supply ascending batches and explicitly associate records with the bound owner.

Observation: original prepared geometry/bank plus authored material closure34 sources/4,681,720 bytes closes before main-thread transfer;99 walk/66 run geometry updates deliver six footstep occurrences each over three cycles. Direct prepared-frame comparison, retained geometry/resource fingerprint, unchanged root transform, pause/focus/restart and duplicate-after-clear checks pass. Confidence Confirmed for original byte identity and headless replacement frame/delivery behavior. Consequence: usable diagnostic control adapter; no effects/audio or LOCT actor translation occurs. Four authored MAIN bindings/four distinct MAIN images are present, and ten texture upload resources/14,330,528 decoded bytes are created using the existing material adapter. GPU rendering/texture readback/visual result remains unverified by this worker.

Observation: cancellation at the first source lookup closes archive scope, transfers no geometry and disposes its loader. Teardown disables future updates and releases owned mesh resources. Confidence Confirmed for executed deterministic cancellation/teardown; late Node-exit guards are code-inspected, not a separately timed user-close test.

## Exact verification commands

PowerShell from the checkout root, with an existing private cache copied from the accepted main cache; sources-cleared config ensures offline restore. Godot SDK4.7.2 was reused from the existing local SDK cache into the ignored package cache, not downloaded.

```powershell
$env:APPDATA = Join-Path $env:TEMP 'stardust-runtime-r3-appdata'
$env:NUGET_PACKAGES = Join-Path $PWD 'local-private/round1/nuget-packages'
dotnet restore poc/round3-tests/Runtime/Runtime.csproj --configfile poc/round3-tests/Runtime/NuGet.Config --packages $env:NUGET_PACKAGES -p:NuGetAudit=false
dotnet run --project poc/round3-tests/Runtime/Runtime.csproj --no-restore -p:UseSharedCompilation=false -- C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg
dotnet restore poc/Stardust.Godot/Stardust.Godot.csproj --configfile poc/round3-tests/Runtime/NuGet.Config --packages $env:NUGET_PACKAGES -p:NuGetAudit=false
dotnet build poc/Stardust.Godot/Stardust.Godot.csproj --no-restore -p:UseSharedCompilation=false -warnaserror
dotnet restore poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj --configfile poc/round3-tests/Runtime/NuGet.Config --packages $env:NUGET_PACKAGES -p:NuGetAudit=false
dotnet build poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj --no-restore -p:UseSharedCompilation=false
$eventGodot = 'C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/toolchain/godot-4.7.2-dotnet/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe'
& tools/powershell/Invoke-StardustAnimationEventPreview.ps1 -GodotPath $eventGodot -ArchiveRoot C:/Stardust-DEV -Configuration C:/Stardust-DEV/swgemu_live.cfg -Headless
& tools/powershell/Invoke-StardustAnimationEventPreview.ps1 -GodotPath $eventGodot -ArchiveRoot C:/Stardust-DEV -Configuration C:/Stardust-DEV/swgemu_live.cfg -Headless -CancelLoad
$env:PYTHONUTF8 = '1'
& C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe tools/python/validate_organized_workspace.py .
git -c safe.directory=C:/Users/ancie/.codex/worktrees/9175/Stardust-Client -c core.safecrlf=false diff --check
```

Native sink suite and both pinned originals PASS. Core/Godot/shared SmokeTests builds PASS zero warnings/errors. Headless original preview and first-lookup cancellation PASS. Godot logs include the existing ERROR: Failed to read the root certificate store at startup; it is a nonfatal existing environment issue and these offline fixtures exit0 with acceptance markers. No network/certificate changes or GPU/visible run. Full shared native suite execution is not claimed; new synthetic/original scenarios execute through the permanent console and shared project compilation verifies inclusion. Delivery-counter exhaustion and timed exit-during-load are unrun, not execution-verified.

## Scope and handoff

New AnimationEventDiagnosticLog.cs/scenarios, own AnimationEventDiagnosticPreview.cs/Main.AnimationEvents.cs, dedicated launcher, contract/checkpoint and own round3 Runtime console/Program/NuGet.Config. Main.cs changes ONLY the assigned dispatch branch before asset-creature; shared Main.SkeletalAppearance.cs/asset launcher/scene clocks/rendering code/ledgers/registries/runners remain unchanged relative to the accepted baseline. Baseline-relative paths/hashes are recorded privately with the existing scope helper for lead review.

[Durable contract](../../docs/ANIMATION_EVENT_DIAGNOSTIC_PREVIEW_CONTRACT.md) records API, ordering, ring/counter/generation/clear policy and preview controls/ownership. Suggested shared runner registration AnimationEventDiagnosticLogScenarios.Run(); optional Original(root,configuration). Lead owns shared documentation reconciliation, integration and GPU/visible inspection using the launcher -Interactive. Original LIT/scheduler/graph/effect/audio behavior remains unknown; research/function/protocol metrics unchanged. Next action: lead acceptance and visual check, not another milestone. No original executable execution, live contact, DNS/socket use, staging, commit or publication.


## Bounded capture handoff

Launcher now supports separate -CapturePath/-CaptureClip walk or run. Capture paths must be fresh PNGs in an existing ignored checkout-private directory. Hidden launch freezes walk frame9 or run frame8, cycle1/three current-generation footsteps, waits three render frames, calls existing VerifyBoundary(true) (including full texture/mip readback), saves the image and quits. Interactive remains selectable; no worker GPU/visible run. The actual authored MAIN bundle is loaded via loadMaterialDefaults=true with explicit modern128-resource/32MiB limits; headless logs confirm four MAIN bindings and ten upload resources. Visual fidelity of this original MAIN inspection (without generated face/palette/specular effects) is for lead review.

Lead GPU commands after integration and rendering ownership release (not executed here):

```powershell
New-Item -ItemType Directory -Force local-private/round3/animation-events | Out-Null
& tools/powershell/Invoke-StardustAnimationEventPreview.ps1 -GodotPath $eventGodot -ArchiveRoot C:/Stardust-DEV -Configuration C:/Stardust-DEV/swgemu_live.cfg -CapturePath "$PWD/local-private/round3/animation-events/walk-fresh.png" -CaptureClip walk
& tools/powershell/Invoke-StardustAnimationEventPreview.ps1 -GodotPath $eventGodot -ArchiveRoot C:/Stardust-DEV -Configuration C:/Stardust-DEV/swgemu_live.cfg -CapturePath "$PWD/local-private/round3/animation-events/run-fresh.png" -CaptureClip run
```


Final worker validation: workspace validator1026 Markdown/0 broken links/0 private links;2602 names/0 duplicates;345 packet rows. Scope helper review passes exactly11 assigned paths (ten additions plus Main.cs). Main.cs was rebuilt byte-for-byte from the verified baseline ZIP with only the dispatch insertion, using the observed CRLF at that insertion point. Removing that inserted byte sequence reproduces the exact baseline; normalized diff adds one line and removes none. Private normalized diff and byte-preservation proof accompany the hash manifest. Focused diff checks pass.

Authored material closure final headless result:34 source files/4,681,720 source bytes;four MAIN bindings/four distinct MAIN images;ten ImageTexture upload resources/14,330,528 decoded bytes. Actual GPU readback/capture is unrun here; capture mode invokes the existing complete texture/mip verifier when the lead runs it. Original deterministic walk/run/cancellation fixture passes with this final material closure.
