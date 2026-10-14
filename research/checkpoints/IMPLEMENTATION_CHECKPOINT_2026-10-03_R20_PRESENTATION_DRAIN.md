# Round20 presentation drain ownership

Round20 Runtime, October2,2026 Eastern; requested checkpoint filename uses the Round20 October3 label. Bounded question: can every retired/rollback adapter's optional worker task remain observable through close, failures and capacity pressure without unbounded task histories? Stopping condition: controlled TCS lifecycle tests and all prior pump/consumer regressions pass, public scope is exact and the next integration action is explicit.

## Inputs and orientation

Full snapshot2,374 files: manifest SHA256 `52d1c308ffd18481ff6a30ccc334a59c98f9dd700b201838e1bb83dcc3fee167`; ZIP `5f379b875a8c3af1c19b8fe1607851c4a252c395c893b322a795bb82d47048e5`. Exact member set, per-entry lengths/hashes, safe contained destinations and absence of ZIP/destination/ancestor symlinks or junctions were checked before the first write. Complete output verification followed. Snapshot HEAD stays separate from worker HEAD `65de8c88141071744dc75594be126b3bab3696a5`; index hash and HEAD were captured/preserved. Ignored/private/unlisted files were not overlaid.

Read START_HERE/latest accepted19, status/ledger/UNKNOWN, consumer/pump durable contracts, prior checkpoints, existing source/tools and the frozen new interfaces. Original root/config/catalog/locomotion inputs are unchanged: C:/Stardust-DEV/swgemu_live.cfg SHA256 `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`. Native fixture reuses pinned human idle/walk/run/albedo and terminal frame0, with genuine coordinator publications. Accepted independent synthetic peer generator is unchanged. Cached .NET8/NuGet and bundled Python only; no original executable/address reliance, GPU, downloads, network/server contact or external-system mutation.

## Evidence, interpretation and compatibility consequence

**Observation / Confirmed modern implementation:** consumer (external historical reference) captures optional drain after Dispose on every centralized retirement path, including throwing Dispose and candidate rollback. Clear still detaches/attempts every actor. Pump (external historical reference) attempts consumer and world cleanup and joins both captured completions, with a stable handle after errors. **Interpretation:** rendering/CPU workers may outlive detached presentation roots but remain known shutdown obligations. **Consequence:** asynchronous adapters must expose a durable post-disposal task; close callers await it before claiming success. Synchronous adapters and their synchronous error channel remain compatible with prior tests.

**Observation / Confirmed invariant:** capacity reserves pending+owned+candidate before factory execution. Default drain bound is twice the actor bound; explicit maximum is8,192 and at least maximumActors. A failed capacity admission creates no candidate; pump failure cleanup can still retire every previously reserved actor. Completed tasks are removed, task references deduplicate and only the first drain error is retained diagnostically. Each fault is nevertheless observed. **Consequence:** direct callers may retry after successful workers finish; asynchronous failure prevents new active operations and requires Dispose. Pump closes on capacity/fault rather than silently dropping ownership. [Durable contract](../../docs/SCENE_WORLD_PRESENTATION_DRAIN_CONTRACT.md) specifies getters, group lifecycle and limits.

**Observation / Confirmed boundedness correction:** allocating repeated WhenAll snapshots would attach an increasing number of joins behind a long worker. The implementation instead gives overlapping retirements one locked counter/TCS group. Native128-short-worker churn behind one held task checks reference equality of every returned group task and exactly one pending task after each short completion. A separate128-rollback cycle checks no completed-task tombstones. **Consequence:** getters do not create an unbounded aggregate history; no worker continuation mutates the serialized actor dictionary/world.

**Observation / Confirmed native proof:** scenario (external historical reference) passes1,165 checks/37 cases. Cases cover every retirement path; wrong/false/throw/stale/reset candidate rollback; Dispose/getter/null/getter-reentry/both-error paths; replacement; bounded admission/reuse; shared tasks; late failure/cancel; partial batch application without acknowledgment; foreign thread/caught reentry; pump close/factory/tick/getter failures; pump drain-capacity failclosed; and a synchronous disposal exception plus delayed presentation fault while an independent canceled world job remains pending. Completion does not settle before that world job ends. TCS transitions drive the tests without sleeps or original execution. Prior full consumer160/pump171 checks still pass. All three scenarios use accepted66-source original human/terminal preloads and close every archive before lifecycle assertions.

Compact audit (external historical reference) checks exact37 named cases, expected native counts, PASS receipts and zero original archive scopes. It is an audit of native assertions, not an independent scheduler implementation or new original asset/schema census. Detailed receipts, source/proprietary-derived logs, diffs, manifests and helper scripts remain ignored private evidence.

## Reproduction

```powershell
$env:PYTHONUTF8='1'
$env:APPDATA=Join-Path $env:TEMP 'stardust-runtime-r20-appdata'
$env:NUGET_PACKAGES='C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
New-Item -ItemType Directory -Force local-private/round20 | Out-Null
& 'C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' poc/round14-tests/Runtime/Generate-Peer.py local-private/round20/peer.json
dotnet restore poc/round20-tests/Runtime/Runtime.csproj --configfile poc/round20-tests/Runtime/NuGet.Config -p:NuGetAudit=false
dotnet build poc/round20-tests/Runtime/Runtime.csproj --no-restore -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/round20-tests/Runtime/bin/Debug/net8.0/Runtime.dll drain C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg poc/preview-profiles/scene-object-catalog.json poc/preview-profiles/human-locomotion.json local-private/round20/peer.json local-private/round20/drain-receipt.json > local-private/round20/drain-native.txt
dotnet poc/round20-tests/Runtime/bin/Debug/net8.0/Runtime.dll consumer C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg poc/preview-profiles/scene-object-catalog.json poc/preview-profiles/human-locomotion.json local-private/round20/peer.json local-private/round20/consumer-receipt.json > local-private/round20/consumer-native.txt
dotnet poc/round20-tests/Runtime/bin/Debug/net8.0/Runtime.dll pump C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg poc/preview-profiles/scene-object-catalog.json poc/preview-profiles/human-locomotion.json local-private/round20/peer.json local-private/round20/pump-receipt.json > local-private/round20/pump-native.txt
& 'C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' poc/round20-tests/Runtime/verify.py local-private/round20
& 'C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' tools/python/validate_organized_workspace.py .
git -c core.autocrlf=false -c core.safecrlf=false diff --check
```

Initial suite caught candidate getter errors being masked by cleanup boundary fault checks; cleanup now captures/reaps drains without overwriting that primary error. A null-getter fixture initially fell back to its normal task; corrected to return actual null. Final tests use the unchanged accepted wire generator and cached runtimes. Final build has zero warnings/errors. Native drain1,165/37, consumer160 and pump171 checks pass; compact receipt audit passes. Workspace validator reports1,187 Markdown files,zero broken/private links,2,602 canonical addresses/zero duplicates and345 packet rows. Scoped whitespace/newline checks and focused ZIP-relative review pass. Scope audit confirms2,372 unchanged baseline files, exactly the two assigned edits and seven additions; frozen interfaces, shared source and unrelated baseline bytes stay unchanged. Worker HEAD/index hashes remain preserved. New files are UTF8/LF/noBOM; existing core line endings stay intact.

## Limits and resume

Only trusted passive tasks/adapters are in scope; an unavailable task getter is explicit failed cleanup and cannot prove the hidden worker stopped. Factory-before-return failures remain factory-owned. Exception payload memory, caller-retained completion references and rendering-owned cancellation policy are outside this logical count. Current group completion before Dispose does not promise to drain future actors/groups. No rendering, customization or raw field4 state is implemented here.

Lead next action: register `SceneWorldPresentationDrainScenarios.Run(new[]{root,config,catalog,bank},peer,exportPath)`, join the optional frozen drain implementation in recipe adapters, and await pump.DrainCompletion after host Close on success and error paths. Shared Program/project/coordinator/interfaces/policy/ledgers remain untouched. Stop after this milestone; no next round started.
