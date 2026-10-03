# Round25 Runtime current names and atomic state

Date: October3,2026 Eastern. Question: can raw names1/2 join the accepted24 union atomically while every older replacement preserves current Names/common values and decoded charge? Stop after native/independent/prior/scope/documentation gates; no new milestone begins.

## Inputs and tools

Frozen2558-file manifest SHA256 `3c1f730fe25ab6570e60287d21e55dae143575d21611e89a648e886ad6e27b5f`; ZIP `d546b369229b42415b92a4a7e92131e2a30d4f1a945633c6de4a0a6cb025c334`. Whole member/path/size/hash/containment/ancestor-target symlink/junction preflight before overlay and exact postflight2558 files; HEAD/index/unlisted/private preserved. START/latest24/session25/status/ledger/UNKNOWN/relevant name/localization checkpoint/tools/durable24 contract inspected. No new executable VA/behavior or original execution claim; executable identity is not relied upon.

Frozen NameState/NamedPlan/snapshot forwarder unchanged. Read-only cfg `C:/Stardust-DEV/swgemu_live.cfg` pin `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`; Runtime14 independently authored peer family; accepted standalone compound codecs create frozen nested commands. No unfinished NamedCodec dependency. Cached .NET8/no-source own config/private APPDATA/UseSharedCompilation=false; Python explicitUTF8. Loader calls0; no download/original/GPU/asset preparation. Private detailed evidence stays ignored; public files contain only authored schema/code/fixtures.

## Observations, confidence and consequences

**Confirmed implementation:** [store](../../poc/Stardust.Core/Session/SceneCreatureReplicationStore.cs) carries Names/TangibleScalars through all old replacements and includes current Names.AdditionalDecodedBytes in every old/new charge, seed subtraction and retirement path. Shared named preparation applies ordered private name state and checks intermediate/global retained bytes before later shrinking updates. Accepted compound observers include current name charge. [Coordinator](../../poc/Stardust.Core/Session/SceneWorldCoordinator.cs) preflights configured name widths, combined name/common/creature attempts and all accepted raw/typed/world bounds before callback-free commit. **Interpretation:** modern all-or-nothing compatible boundary, not recovered original notification timing. **Consequence:** old ingress retains new names/history/charge; invalid later names/compound updates cannot publish an earlier prefix.

**Confirmed native:**489 checks/93 cases verify high/NUL name bytes, raw spacer/U16/unpaired ownership, aliases/equal newly owned storage, both orderings/receipts, old24/23/22/21 and scalar/mixed/composite carry, genuine baseline reset, width/attempt/intermediate shrink/name-included compound/multiactor budgets and inherited lifecycle/owner/revision/purity/growth cases. A synthetic65537-unit baseline aliases successfully while equally large new custom-name update rejects. Direct components carry/charge is inspected, not separately executed after names. **Consequence:** frozen new-update caps do not silently shrink legacy baseline acceptance. Logical accounting is per-field baseline reference aliasing, excludes CLR/GPU/caller overhead and keeps raw sidecar accounting separate.

**Confirmed output audit:** [oracle](../../poc/round25-tests/Runtime/verify.py) independently calculates name/raw/unit/spacer/order/variable charge and prior common/compound arithmetic from authored events/exported accepted initial state, with no production imports. Prior extended407/81, full338/67, scalar196/40, raw136/28, consumer160, pump171, drain1165/37 plus accepted kernels and inherited audits pass. Final build zero warnings/errors. **Limit:** this is output arithmetic evidence, not independent scheduler/wire/baseline execution; display/locale/path policy remains separate. No research/function/protocol metric changes.

[Durable contract](../../docs/SCENE_WORLD_NAMED_CREATURE_CONTRACT.md) is canonical for APIs, aliases/current history, budgets, rollback, callbacks and next join.

## Reproduction

```powershell
$env:PYTHONUTF8='1'
$env:APPDATA=Join-Path $env:TEMP 'stardust-runtime-r25-appdata'
$env:NUGET_PACKAGES='C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
New-Item -ItemType Directory -Force local-private/round25 | Out-Null
& 'C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' poc/round14-tests/Runtime/Generate-Peer.py local-private/round25/peer.json
dotnet restore poc/round25-tests/Runtime/Runtime.csproj --configfile poc/round25-tests/Runtime/NuGet.Config -p:NuGetAudit=false
dotnet build poc/round25-tests/Runtime/Runtime.csproj --no-restore -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/round25-tests/Runtime/bin/Debug/net8.0/Runtime.dll kernels > local-private/round25/kernels-native.txt
foreach($mode in @('named','extended','full','scalars','state','consumer','pump','drain')) {
 dotnet poc/round25-tests/Runtime/bin/Debug/net8.0/Runtime.dll $mode C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg poc/preview-profiles/scene-object-catalog.json poc/preview-profiles/human-locomotion.json local-private/round25/peer.json "local-private/round25/$mode-receipt.json" > "local-private/round25/$mode-native.txt"
 if($LASTEXITCODE -ne 0){throw "Native mode failed: $mode"}
}
& 'C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' poc/round25-tests/Runtime/verify.py local-private/round25
& 'C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' tools/python/validate_organized_workspace.py .
git -c core.autocrlf=false -c core.safecrlf=false diff --check
```

Final scope passes:two authorized existing core files changed plus nine additions (11 changed paths). All2556 other baseline files are byte-identical, no unrelated public changes/deletions; HEAD/index preserved and private receipts/bin/obj ignored. Scoped UTF8/LF/noBOM/trailing-whitespace/EOF and git diff --check pass; focused carry/charge diff and git status inspected. Workspace validator:1238 Markdown files,zero broken/private links,2602 names/zero duplicates,345 packet rows,zero public files25MiB or larger. Frozen APIs/raw store/kernels/shared Program/Godot/canonical ledgers/navigation remain unchanged.

## Limits and exact next action

Display/path/locale/cache/live-name/TANO/other-view behavior remains unclaimed. Direct ObjectId targets current incarnation; old host tokens need separate validation. Callback external effects cannot be rolled back; resource-only mutations lack universal freshness token. Name variable billing follows the frozen baseline-field alias method rather than total heap/reference-graph deduplication.

Lead: register SceneWorldNamedCreatureScenarios.Run(inputs,peer,exportPath), inject accepted named parser into SceneWorldNamedCreatureApplicationRouter(world,rawStore,decoder,motionRouter.Accept,maxPayloadBytes), pass router.Accept to pump. Keep one raw projector/exact complete-current acknowledgment and read current Names through separately accepted resolver/presentation. Stop at milestone; named-wire/STF/GPU joining is lead-owned.
