# Round24 Runtime extended creature state

Date: October3,2026 Eastern. Bounded question: can seven fixed common fields join the accepted23 ordered union atomically, while every older snapshot replacement preserves the new common overlay instead of restoring historical baseline values? Stop after native/prior/independent/scope/doc gates; no next round begins.

## Input identity and tools

Frozen2519-file manifest SHA256 `b601923e5e05c33cb41ade174c72773b7d9ece509eb30ba8d4d09d0d2c1f7f19`; ZIP `7f87d3b2bb0644a3e237711cc953f8685914f623e93ff0c7607dbc5fe0764e95`. Whole archive/member set/path/length/hash/containment/ancestor-target symlink-junction preflight before overlay, all2519 files postverified. HEAD/index/unlisted/private preserved. START/latest23/session24/status/ledger/UNKNOWN/relevant curated tangible rows/prior checkpoints/durable23 contract/tools inspected. No new VA/executable claim or original execution; executable identity is not used for this modern implementation claim.

Lead-frozen ExtendedDeltaPlan and TangibleScalarState/Update and snapshot forwarder are unchanged. Read-only cfg `C:/Stardust-DEV/swgemu_live.cfg` matches `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`. Runtime14 generator supplies correct prior synthetic peer, standalone accepted codecs create compound objects, no unfinished ExtendedCodec dependency. Cached .NET8/no-source package config/task APPDATA/UseSharedCompilation=false; Python explicit UTF8. No download/assets preparation/GPU; loader calls0. Detailed manifests/receipts/diffs/logs remain ignored private evidence; only authored fixtures/schema/code are public.

## Evidence and consequence

**Observation / Confirmed implementation:** [replication store](../../poc/Stardust.Core/Session/SceneCreatureReplicationStore.cs) carries current TangibleScalars through every scalar/mixed/components/composite/staged replacement. Baseline Seed alone resets common state. Shared extended preparation walks the ordered owned union using frozen common Apply and accepted single-pass kernels/observer. [Coordinator](../../poc/Stardust.Core/Session/SceneWorldCoordinator.cs) preflights total fields/combined scalar attempts and all accepted source/compound/byte/world/global revision bounds before callback-free commit. **Interpretation:** a modern bounded atomic state slice, rather than recovered original intermediate setter timing. **Consequence:** older ingress cannot overwrite new current common state with historic seed values; late compound/byte failure leaves common/raw/typed/world journals untouched.

**Observation / Confirmed native:**407 checks/81 cases cover all seven fields, signed-zero complexity, U32/U8 extremes/order/repeats, common-only fixed decoded accounting/raw receipts, raw-only typed identity/revision, both-order compounds, specialized23/22/21 and world composite/scalar/mixed common preservation, true baseline reset, combined attempt/field limits, prior atomic/lifecycle/owner/policy/growth/byte/global overflow and4096-command cases. Direct components replacement carry is inspected, not separately executed after a common update. [Router](../../poc/Stardust.Core/Session/SceneWorldExtendedCreatureApplicationRouter.cs) first0/3/4/5/6..17 and inherited purity/reentry/stale/fault behavior pass. **Consequence:** lead may inject actual extended codec and route current Visibility to presentation independently.

**Observation / Confirmed independent output:** [oracle](../../poc/round24-tests/Runtime/verify.py) models authored common/raw/scalar/compound events and counter arithmetic without production imports. It reuses unchanged23/22/21/drain audits. Prior full338/67, scalar196/40, raw136/28, consumer160, pump171, drain1165/37 and accepted strict/legacy wounds/component/mixed kernel scenarios pass. Final build zero warnings/errors. **Limit:** arithmetic/output verification is not independent scheduler/wire-parser/baseline execution; current logical fixed field charge excludes CLR overlay overhead. No research/function/protocol score changes.

[Durable contract](../../docs/SCENE_WORLD_EXTENDED_CREATURE_CONTRACT.md) is canonical for API, bounds, current/history, ownership, callback/cleanup and next join semantics.

## Reproduction

```powershell
$env:PYTHONUTF8='1'
$env:APPDATA=Join-Path $env:TEMP 'stardust-runtime-r24-appdata'
$env:NUGET_PACKAGES='C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
New-Item -ItemType Directory -Force local-private/round24 | Out-Null
& 'C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' poc/round14-tests/Runtime/Generate-Peer.py local-private/round24/peer.json
dotnet restore poc/round24-tests/Runtime/Runtime.csproj --configfile poc/round24-tests/Runtime/NuGet.Config -p:NuGetAudit=false
dotnet build poc/round24-tests/Runtime/Runtime.csproj --no-restore -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/round24-tests/Runtime/bin/Debug/net8.0/Runtime.dll kernels > local-private/round24/kernels-native.txt
foreach($mode in @('extended','full','scalars','state','consumer','pump','drain')) {
 dotnet poc/round24-tests/Runtime/bin/Debug/net8.0/Runtime.dll $mode C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg poc/preview-profiles/scene-object-catalog.json poc/preview-profiles/human-locomotion.json local-private/round24/peer.json "local-private/round24/$mode-receipt.json" > "local-private/round24/$mode-native.txt"
 if($LASTEXITCODE -ne 0){throw "Native mode failed: $mode"}
}
& 'C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' poc/round24-tests/Runtime/verify.py local-private/round24
& 'C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' tools/python/validate_organized_workspace.py .
git -c core.autocrlf=false -c core.safecrlf=false diff --check
```

Final scope passes:two authorized existing core files changed plus nine additions (11 changed paths). All2517 other baseline files are byte-identical, with no unrelated public changes/deletions; HEAD/index preserved, private receipts and bin/obj ignored. Scoped UTF8/LF/noBOM/trailing-whitespace/EOF checks and git diff --check pass; focused diff/current carry arguments and git status inspected. Workspace validator:1228 Markdown files,zero broken/private links,2602 names/zero duplicates,345 packet rows,zero public files25MiB or larger. Raw store/kernels/frozen DTOs/Godot/shared Program/canonical ledgers/navigation remain unchanged from snapshot.

## Remaining limits and exact next action

Names1/2, inherited TANO, live default/locale/context and actual wire/GPU application remain separate boundaries. Common/full fields lack coupled value semantics; raw journal preserves its own ordered notifications and commit exposes no intermediate state. Direct ObjectId targets current incarnation; old host tokens need separate validation. External callback effects cannot be rolled back; unrelated resource-only mutation lacks universal guard. Logical fixed decoded/raw bytes exclude CLR/GPU/caller overhead.

Lead: register SceneWorldExtendedCreatureScenarios.Run(inputs,peer,exportPath); inject accepted extended decoder into SceneWorldExtendedCreatureApplicationRouter(world,rawStore,decoder,motionRouter.Accept,maxPayloadBytes); pass router.Accept to pump. Preserve one raw projector and exact-current complete acknowledgment; presentation reads current TangibleScalars.Visibility. Stop at this milestone; full wire/GPU join stays lead-owned.
