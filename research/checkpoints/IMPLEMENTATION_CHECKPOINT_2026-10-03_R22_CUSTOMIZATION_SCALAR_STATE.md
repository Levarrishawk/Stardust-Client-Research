# Round22 Runtime customization/scalar transaction

Date: October2,2026 Eastern; filename uses the lead-assigned October3 UTC round label. Question: can ordered field4 and six creature scalar records commit together to current finalized world state, with bounded all-or-nothing failure across raw, replication and world journals? Stopping condition: injected/frozen-plan native cases plus prior gates, independent oracle, durable contract and exact authorized scope handoff. No next milestone is started.

## Inputs and tools

Frozen public baseline:2443 files, manifest SHA256 `ef026386b3fcb2bbb0c03340e75e761dcd62f856798dda7ff1f00aa917894ecb`, ZIP `2e52e818dbcd3a713bfb0827b6b3663c98573371fe47f562997c6cedfcd01746`. Exact full entry/name/hash/length/containment/symlink/junction preflight preceded overlay; postflight matched all2443 bytes. Worker HEAD/index and unlisted/private files preserved. START/latest accepted21, current status/ledger/unknown names, raw-state and scalar contracts/checkpoints, canonical customization rows and existing producers/codecs/validators were inspected. No virtual-address research, new executable evidence or corpus sweep was required; original executable identity was not used for any new claim.

Read-only input configuration `C:/Stardust-DEV/swgemu_live.cfg` matches pin `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`. Runtime14 generator supplies the correct accepted synthetic creature/static family. Cached .NET8 packages, own no-source config and private APPDATA avoid downloads/shared compiler use. Python explicit UTF8; all generated logs/receipts/snapshots/diffs remain ignored private evidence. No original asset payload is added to public fixtures.

## Observations and confidence

**Observation / Confirmed implementation:** [coordinator transaction](../../poc/Stardust.Core/Session/SceneWorldCoordinator.cs) owns current target checks and prebuilt world journal, then holds internal [raw](../../poc/Stardust.Core/Session/SceneWorldCustomizationStateStore.cs) and [replication](../../poc/Stardust.Core/Session/SceneCreatureReplicationStore.cs) staged tokens. Preparation checks all policy/binding/global revision/capacity bounds before callback-free assignments. Raw preparation bypasses public Latest/Prune/world methods to avoid owner recursion. Nonretiring replication inspection ensures rejection does not perform incidental housekeeping. **Interpretation:** a bounded modern transactional compatibility slice, rather than evidence of original atomic mutation. **Consequence:** hosts can reject an entire owned plan without clearing otherwise-current world/source state or losing pending receipts.

**Observation / Confirmed fixtures:** both ordered union directions produce exact independent raw sequences and final Height; all six known records retain baseline/wounds/components. Raw-only preserves typed revision/state; scalar-only preserves raw receipt/revision/journal. All ordinary count/string/byte/actor/owner/binding/unseeded/closed/three-global-overflow/world-journal failures preserve the old state/journals. **Consequence:** an earlier accepted local raw write remains available before a later opaque invalid attempt. Actual packed-variable parsing/ranges and unsupported scalar/component/wounds indices remain separate. The [durable contract](../../docs/SCENE_WORLD_CUSTOMIZATION_SCALAR_CONTRACT.md) is canonical for exact semantics and bounds.

**Observation / Confirmed router fixtures:** [new router](../../poc/Stardust.Core/Session/SceneWorldCustomizationScalarApplicationRouter.cs) drops stale generation before parse, routes first4/11..16 through a bounded pure injected decoder, verifies envelope/plan/current target/source/world freshness and closes only itself on faults. Callback-induced external effects are not rolled back. **Consequence:** lead may inject the real accepted union decoder without depending on unfinished worker networking. Resource-only mutations beyond exposed guards remain governed by callback purity.

Native new196 checks/40 cases, previous raw136/28, consumer160, pump171, drain1165/37 pass. [Independent oracle](../../poc/round22-tests/Runtime/verify.py) reads authored symbolic union events and checks ordered raw/revisions/final scalar receipts, then runs unchanged Round21/20 inherited audits. Loader calls0. This does not execute an independent scheduler, prove a new wire grammar or expand coverage. Initial implementation build passed; first fixture190/38 passed, then source/replication nonretiring inspection and extra actor/unseeded/closed cases yielded final196/40. Final build zero warnings/errors.

## Reproduction

From this checkout:

```powershell
$env:PYTHONUTF8='1'
$env:APPDATA=Join-Path $env:TEMP 'stardust-runtime-r22-appdata'
$env:NUGET_PACKAGES='C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
New-Item -ItemType Directory -Force local-private/round22 | Out-Null
& 'C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' poc/round14-tests/Runtime/Generate-Peer.py local-private/round22/peer.json
dotnet restore poc/round22-tests/Runtime/Runtime.csproj --configfile poc/round22-tests/Runtime/NuGet.Config -p:NuGetAudit=false
dotnet build poc/round22-tests/Runtime/Runtime.csproj --no-restore -p:UseSharedCompilation=false -p:UseAppHost=false
foreach($mode in @('scalars','state','consumer','pump','drain')) {
 dotnet poc/round22-tests/Runtime/bin/Debug/net8.0/Runtime.dll $mode C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg poc/preview-profiles/scene-object-catalog.json poc/preview-profiles/human-locomotion.json local-private/round22/peer.json "local-private/round22/$mode-receipt.json" > "local-private/round22/$mode-native.txt"
 if($LASTEXITCODE -ne 0){throw "Native mode failed: $mode"}
}
& 'C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' poc/round22-tests/Runtime/verify.py local-private/round22
& 'C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' tools/python/validate_organized_workspace.py .
git -c core.autocrlf=false -c core.safecrlf=false diff --check
```

Final scope audit passes:2443 baseline files,2440 byte-identical outside the three authorized existing core files, nine authorized additions (12 scoped paths total), no deletions/unrelated public changes. HEAD/index preserved; private receipts and bin/obj ignored. Scoped UTF8/LF/noBOM/trailing-whitespace/EOF and git diff --check pass; focused diff and git status inspected. Workspace validator:1207 Markdown files,zero broken/private links,2602 names/zero duplicates,345 packet rows and zero public files25MiB or larger. Shared Program, frozen DTO, Godot and canonical ledgers/navigation remain lead-owned.

## Remaining uncertainty and exact next action

Original setter notification/intermediate mutation timing is not reproduced by this modern transaction. Direct object-ID application targets the current incarnation; router prevents decoder-time replacement, while older caller-held incarnation tokens require host validation. Logical byte charge is not physical memory. Actual packed CIDM/local ranges/render joining and union codec malformed-input policy remain separately reviewed boundaries; this slice contains only field4 and the six known finite scalar records.

Lead action: register SceneWorldCustomizationScalarScenarios.Run(inputs,peer,exportPath); inject accepted union decoder into SceneWorldCustomizationScalarApplicationRouter(world,rawStore,decoder,motionRouter.Accept,maxPayloadBytes); pass router.Accept into pump. Preserve single projector and exact complete current raw acknowledgment. Stop after this handoff; all GPU runs remain lead-owned.
