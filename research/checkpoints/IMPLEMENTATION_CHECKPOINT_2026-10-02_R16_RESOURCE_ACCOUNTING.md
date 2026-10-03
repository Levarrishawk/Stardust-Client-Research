# R16 logical resource accounting — 2026-10-02

## Bounded question and stopping condition

Can a pure helper deterministically measure queued source plus unique decoded texture storage for accepted human/terminal completions, rejecting represented inconsistencies and arithmetic overflow? Stop after implementation, original positives, authored negatives/mutation/cleanup checks, independent original-header accounting and complete baseline-relative scope validation pass. Runtime integration and subsequent rounds remain lead-owned.

## Input identity and orientation

Round16 manifest SHA-256 `c2bc35eea45fd6f86d968b4ae28fb9034c7ffce3225691670abd30f0efe9a8ee`; ZIP SHA-256 `8c5a649747ad8a7aa5d9d1b25bab32fd603c6f2ddea97afe4b30d324c28eac32`;2,237 unique members; authoritative snapshot HEAD `1fc55dd89cd23ce3af49e3fe471210bdebe9735c`. Isolated checkout HEAD `65de8c88141071744dc75594be126b3bab3696a5` is preserved separately. Preflight verified complete exact member set, length/hash, safe contained paths and no ZIP/destination/ancestor symlinks before first overlay write; all output hashes verified. Unlisted/private/index/HEAD preserved.

Read repository instructions, START_HERE and latest Round15 session, status/ledger/unknown-function orientation, retention/validation contracts/checkpoints, accepted reader/source/texture structures and original oracle tools. Frozen `ISceneWorldPresentationPump.cs` is read-only. No executable address is used or original executable run. Original root `C:/Stardust-DEV` and config remain read-only. Config pin `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`; accepted catalog/human locomotion profiles and exact existing human detail0/albedo and terminal frame0 choices are reused. No expanded corpus or new decoder.

## Implementation and evidence

[Pure helper](../../poc/Stardust.Core/Assets/SceneObjectResourceAccounting.cs) returns a sealed get-only scalar receipt. [Durable contract](../../docs/SCENE_OBJECT_RESOURCE_ACCOUNTING_CONTRACT.md) is the canonical counting/identity/error boundary. Source count/positive bytes/key identity and completion totals agree; images deduplicate by ordinal source path/exact shared reference, matching accepted static validation. Distinct objects for one path reject conservatively, even if metadata agrees. Existing RGBA/HDR DTO lengths are counted; no payload copies, pixel scanning, reread or decoding occurs. Receipt weight adds source bytes and decoded texture bytes with checked arithmetic. Geometry/animation/CLR/GPU/transient overhead is excluded.

**Observation / Confirmed modern fixture:** original human39 sources/4,686,371 bytes,10 unique textures/80 mips/14,330,528 decoded bytes yields weight19,016,899. Terminal27/1,401,794 bytes,7 textures/62 mips/2,140,836 decoded bytes yields3,542,630. Combined logical weight22,559,529. Interpretation: a reproducible logical queue charge, not physical memory usage. Consequence: runtime can inject the weight delegate without retaining callbacks or source handles in assets.

[Native scenario](../../poc/Stardust.Core.SmokeTests/SceneObjectResourceAccountingScenarios.cs) checks closed archive scopes,66 single-pass source reads, borrowed-array cleanup, fingerprint stability/no reads during measure, owner/worker determinism and get-only receipts. Synthetic positives check exact-reference deduplication across separate mesh descriptors, content mutation preserving length-only charge and one existing HDR mip's6-byte charge. Fifteen negatives reject null input/image/mip, empty mips, invalid dimensions/lengths, dimension overflow, source size/path/count/total mismatch, absent texture source, duplicate image/metadata disagreement and RGBA/HDR path collision. Original payloads remain unchanged after negative testing. Test-only reflection wraps accepted internal typed constructors, without production mutation APIs.

[Python oracle](../../poc/round16-tests/Assets/verify_accounting.py) independently opens the same read-only configured archive overlay using existing Index/archive tools, verifies all66 source identity/hash/size/priority rows and derives every mip's dimensions/decoded length from17 original DDS headers. Expected39/27 sources,80/62 mips and the computed byte totals exactly match native receipts. No pixel decoder campaign is repeated; no new format or corpus claim follows. Metadata/hash receipts and detailed logs remain ignored under `local-private/round16/`.

## Exact reproduction

Use accepted cached .NET8/NuGet and bundled Python only. The runner accepts exactly root/config/catalog/bank; native output is `accounting_receipt=` JSON then PASS. Oracle accepts root/config/native-output.

```powershell
$env:APPDATA=Join-Path $env:TEMP 'stardust-assets-round16-appdata'
$env:NUGET_PACKAGES='C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
$env:PYTHONUTF8='1'
dotnet restore poc/round16-tests/Assets/Assets.csproj --configfile local-private/round16/NuGet.Config -p:NuGetAudit=false
dotnet build poc/round16-tests/Assets/Assets.csproj --no-restore -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/round16-tests/Assets/bin/Debug/net8.0/Assets.dll C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg poc/preview-profiles/scene-object-catalog.json poc/preview-profiles/human-locomotion.json > local-private/round16/native.txt
& 'C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' poc/round16-tests/Assets/verify_accounting.py C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg local-private/round16/native.txt > local-private/round16/python-audit.json
& 'C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' tools/python/validate_organized_workspace.py .
git -c core.autocrlf=false -c core.safecrlf=false diff --check
```

Final restore/build/native/oracle pass; zero compiler warnings/errors. An initial nullable-flow compile error was fixed by annotating the local guard's nonreturning false branch before final execution. Scope/link/whitespace checks pass:1,147 Markdown files,zero broken/private links,2,602 provisional rows/zero duplicates and345 packet rows; all2,237 baseline files unchanged and exactly seven authorized additions. HEAD/index preserved; private/build outputs ignored. Detailed final hashes/diff/status/scope receipts remain in the private round16 directory. No stage/commit/push/download/publication, original executable, engine/GPU/server/network/live contact or external mutation.

## Limits and next action

Pure measure does not attest source hashes/origin, full compatibility, freshness, ownership exclusivity, pixel correctness or heap/GPU consumption. Passive collections and serialized access are required. HDR synthetic descriptor length accounting does not expand the current static compatibility gate. Checked long total/PendingWeight overflow exists in code but a valid bounded reader result cannot reach it here; a representable maximal mip dimension tests arithmetic overflow. No impossible gigabyte/billion-entry descriptors are fabricated. Exact next lead action: register `SceneObjectResourceAccountingScenarios.Run(new[]{root,config,catalog,bank})` and inject `r => SceneObjectResourceAccounting.Measure(r).PendingWeight` into the separately reviewed pump after acceptance. No pump/interface edits here. Stop idle after Round16 handoff.
