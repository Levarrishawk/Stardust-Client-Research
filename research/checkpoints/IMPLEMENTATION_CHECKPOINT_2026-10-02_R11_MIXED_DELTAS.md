# 2026-10-02 R11-NET-01: mixed scalar/wounds transaction

Question: can CREO:3 indices11-17 be decoded in received order with either accepted wounds grammar, then applied to an immutable baseline-seeded combined state without partial publication? Stop at codec/state/fixture boundary; no store/world integration or Round12 work.

## Orientation and inputs

Read AGENTS/START_HERE/latest accepted Round10 report, the scalar/baseline/wounds/legacy contracts, prior R9/R10 checkpoints, current status/unknown/ledger terminology and accepted APIs before implementation. No new VA research. Existing original instructions and evidence remain cited through the accepted contracts; no original executable execution, live/socket/DNS/server contact, capture/fuzzing or dependency download.

Synchronized the accepted dirty-main Round11 baseline:2,039 files; HEAD `65de8c88141071744dc75594be126b3bab3696a5`; archive SHA-256 `287f1bdef5bcb0972ab09517c22b3c4bae1397bcbbd6dde9227ad93cde5d4da7`; manifest SHA-256 `48fb34d65ed9161e8472bc4d5adc9675f5997747a7bb4202bd00d62b49114468`. Safe relative member paths, exact membership/content identity and resolved destination containment were verified before overlay; every resulting hash verified. Ignored/unlisted private material preserved.

## Observation, interpretation and consequence

Observation: accepted scalar types encode indices11/12 U8,13 U64,14 finite F32,15 U32 raw bits,16 U64. The accepted wounds kernel supports index17 with raw U32 bound/counter, ordered five-command grammar, selected strict or legacy count/cursor semantics and immutable transactional application. Legacy assign consumes k+1 cursor units; strict assign consumes one command. All structures share CREO/view3 application framing and count-tolerant bounded field exhaustion.

Interpretation: reuse those immutable updates and application kernels inside one ordered plan, retaining selected policy before parsing and publication. Confidence: Confirmed inherited byte/kernel evidence; modern mixed transaction confirmed by fixtures. Compatibility consequence: scalar/wounds interleaving and duplicate fields cannot be independently published; a late counter/growth/error must reject the entire result. The [durable contract](../../docs/CREATURE_VIEW3_MIXED_DELTA_CONTRACT.md) contains precise Runtime12 signatures, limits and next integration action.

Result: new `CreatureView3MixedDeltaCodec`, immutable mixed state/update/plan, baseline seeding, exact encoding and direct bounded parsing. The existing scalar/wounds/baseline codec files are unchanged. Received order is retained through scalar-wounds-scalar sequences and repeated wounds fields. Apply uses accepted plan constructors/kernel APIs; modern object-ID/element checks and atomic returned-state publication are preserved. One outer envelope allocation; no synthetic wire envelopes per field. Only indices11-17 are supported;0-10 reject. No full eighteen-field delta, callbacks, store/world binding or live claim.

Budgets separately cover payload bytes, fields, scalar updates, aggregate raw wounds loop bounds, decoded wounds values and transient elements. Counts are validated before allocations. Intermediate accepted state copies are bounded per field/element budget; logical budgets are not a total CLR heap cap. Immutable snapshots own values; no handles, threads, callbacks or external cleanup lifecycle are introduced.

## Reproduction and verification

From the isolated checkout, PowerShell commands:

```powershell
$py = 'C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$env:PYTHONUTF8 = '1'
& $py tools/python/verify_creature_view3_mixed_deltas.py --generate local-private/round11/fixtures
$env:APPDATA = Join-Path $env:TEMP 'stardust-appdata-networking-r11'
New-Item -ItemType Directory -Force $env:APPDATA | Out-Null
$env:NUGET_PACKAGES = 'C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
New-Item -ItemType Directory -Force local-private/round11 | Out-Null
Set-Content local-private/round11/NuGet.Config '<configuration><packageSources><clear /></packageSources></configuration>'
dotnet restore poc/round11-tests/Networking/Networking.csproj --configfile local-private/round11/NuGet.Config -p:NuGetAudit=false
dotnet run --no-restore --project poc/round11-tests/Networking/Networking.csproj -p:UseSharedCompilation=false -- local-private/round11/fixtures local-private/round11/export
& $py tools/python/verify_creature_view3_mixed_deltas.py --verify local-private/round11/export
dotnet build --no-restore poc/Stardust.Core/Stardust.Core.csproj -p:UseSharedCompilation=false
dotnet restore poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj --configfile local-private/round11/NuGet.Config -p:NuGetAudit=false
dotnet build --no-restore poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj -p:UseSharedCompilation=false
& $py tools/python/validate_organized_workspace.py .
git diff --check
```

Inputs are independently hand-packed synthetic packets and an accepted R8 synthetic complete baseline with wounds counter zero. Exact identities: baseline152bytes SHA-256 `8233b245f232bb0eccc1b7bc68c85be37367f2a690063a0b4da428d6098ca65a`; strict121bytes `782b0a945eb726e20f75ffcd56a44276ea69c5b00336eb10b1efb6fca67433fa`; legacy121bytes `66493bf37f69c0c6bd4eed4f85567e4abaf63d965a5866b2fe64a7f743825957`. No original asset/proprietary/captured bytes. Exports are encoded packets, baseline seed JSON and combined results JSON in ignored private paths. The oracle imports accepted synthetic baseline and separately authored strict/instruction-derived legacy helpers read-only; it does not inspect replacement code.

Native scenarios pass both policies, six scalars and all five wounds commands against accepted codecs, interleaving/repeated fields, exact/over-limit budgets, truncation, late unsupported/nonfinite/counter/growth/value errors, ownership, empty plans, count0/1/65535, signed zero/high bits and object/schema mismatch. Python compares every combined output scalar/counter/value and exact encoded wire bytes for17 cases, including legacy assignment followed by a scalar and both wrong-policy discriminators. Baseline seed remains unchanged after input-buffer clearing, failed Apply and repeated use. Core/shared smoke projects build with zero warnings/errors; shared suite built, not fully executed. New scenario executed through isolated runner; lead integration entry is `CreatureView3MixedDeltaScenarios.Run()`.

## Resume

Runtime12 should use `CreatureView3MixedState.FromBaseline`, select one policy before `CreatureView3MixedDeltaCodec.Decode`, and call `plan.Apply(currentCombinedState)` inside a current stable-incarnation transaction. Publish one receipt only after successful complete application. The codec enforces object ID but carries no registry/store ownership token; caller freshness remains necessary. Original callback effects/outer failure handling, fields0-10 and other views remain separate. Stop idle after Round11; await explicit Round12 dispatch. Research/function/protocol/implementation metrics unchanged.

Final repository gate:1,099 Markdown files,zero broken/private links,2,602 provisional rows/zero duplicates,345 packet rows. Scope review confirms exactly seven allowed additions and all2,039 accepted baseline files unchanged; HEAD/index unchanged. Diff whitespace check passes; new files checked for ASCII UTF-8/LF and trailing whitespace. Focused new-source inspection completed. Baseline-relative SHA-256 change manifest and git status retained in ignored Round11 evidence.
