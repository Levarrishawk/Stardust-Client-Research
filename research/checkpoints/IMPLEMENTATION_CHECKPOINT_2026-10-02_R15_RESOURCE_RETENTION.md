# R15 resource retention checkpoint — 2026-10-02

## Question and stopping condition

Do state-only CREO Height/opaque TANO updates retain exact resources and creates, while terminal destroy/recreate demands a fresh ticket and preserves the human publication? Stop after the bounded original two-recipe positive sequence, terminal recreation isolation, six pure-gate negatives, independent expected accounting/hash audit and scope validation pass. No next round or consumer implementation is included.

## Input identity and method

Round15 verified manifest:2,197 unique files; manifest SHA-256 `1736485311ac5622ceaed02758295293b307ccc52d09c9e982d8b8440b86be1f`; ZIP SHA-256 `920d8655531c68c524d42bd9db2b28a35a91c8b9fa7462328282a7c00ba2835b`. Lead corrected the authoritative snapshot HEAD to `1fc55dd89cd23ce3af49e3fe471210bdebe9735c`. This isolated checkout retains HEAD `65de8c88141071744dc75594be126b3bab3696a5`; its HEAD/index are preserved. Snapshot bytes, not older checkout history, define the comparison baseline. Every member path/length/hash, uniqueness, containment and symlink exclusion was verified before overlay; every baseline output hash was checked afterward. Ignored/unlisted files were preserved.

Original input root `C:/Stardust-DEV` is readonly, with pinned `swgemu_live.cfg`, existing scene-object catalog and human locomotion bank. No executable VA is used and no original executable is run. Accepted .NET8/offline cached NuGet and bundled Python are the only tools. Existing world/initializer/router/pure gate supply behavior; existing ownership regression is reused without expanding to a format/corpus campaign.

## Findings, evidence and confidence

[Durable contract](../../docs/SCENE_OBJECT_RESOURCE_RETENTION_CONTRACT.md) records the independently authored fixture sequence and implementation consequence. Native scenario (external historical reference) verifies synthetic raw create, typed baseline/Height (-1.75 then negative zero), opaque terminal history, exact publication/resource identity, fingerprints, batch acknowledgements and completed close. Controlled tasks provide deterministic delivery without timing loops.

**Observation / Confirmed modern behavior:** both state updates preserve the raw create and complete decoded-resource fingerprint; loader calls stay2 and both original publications stay current. Destroy/recreate invalidates the old terminal ticket, queues one terminal-only load, and publishes an owned replacement with identical resource closure and a distinct ticket. Human identity/currentness is retained throughout. Interpretation: replication revision and resource publication identity are separate accepted modern boundaries. Compatibility consequence: do not reload resources merely for these state updates; do not reuse an old object's ticket based on matching source hashes.

**Observation / Confirmed accounting:** human39/4,686,371 bytes/10 textures/80 mips; terminal27/1,401,794 bytes/7 textures/62 mips; terminal repeat same source/mip manifests. Initial66/6,088,165 bytes/142 mips, total93/7,489,959 bytes with62 repeated terminal mips. Original overlays close before controlled delivery; borrowed arrays clear without altering owned fingerprints. Existing pure gate passes original create/options after updates and rejects six changed-create/recipe/options cases.

The Python oracle (external historical reference) independently defines expected wire bits/counts/accounting and compares source/mip manifests. Native receipts and oracle output contain metadata/hashes only and remain ignored under `local-private/round15/`. No proprietary extracted content is promoted. Python does not independently execute ticket/reference semantics. No legacy runtime, renderer, new field meaning, exclusive ownership or encoded-origin inference follows.

## Reproduction and verification

```powershell
$env:APPDATA=Join-Path $env:TEMP 'stardust-assets-round15-appdata'
$env:NUGET_PACKAGES='C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
$env:PYTHONUTF8='1'
dotnet restore poc/round15-tests/Assets/Assets.csproj --configfile local-private/round15/NuGet.Config -p:NuGetAudit=false
dotnet build poc/round15-tests/Assets/Assets.csproj --no-restore -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/round15-tests/Assets/bin/Debug/net8.0/Assets.dll C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg poc/preview-profiles/scene-object-catalog.json poc/preview-profiles/human-locomotion.json > local-private/round15/native.txt
& 'C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' poc/round15-tests/Assets/verify_retention.py local-private/round15/native.txt > local-private/round15/python-audit.json
& 'C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' tools/python/validate_organized_workspace.py .
git diff --check
```

Restore/build and native sequence pass; zero compiler warnings/errors. Python audit passes. Workspace validation passes:1,136 Markdown files,zero broken/private links,2,602 provisional rows/zero duplicates and345 packet rows. Whitespace and complete baseline-relative scope checks pass; final receipts record exact counts/hashes. Exactly six authorized new public files, all2,197 baseline files unchanged; no staged files and checkout HEAD unchanged. No existing product/shared fixture/project/status/ledger/registry edits, commit/push/download/publication or network/GPU/server/executable execution.

## Handoff and precise next action

Lead must explicitly register `SceneObjectResourceRetentionScenarios.Run(new[]{root,config,catalog,bank})` in the smoke host, or run the isolated four-argument runner. Expect one `retention_receipt=` JSON line and PASS,3 loader calls,2 retained state updates,6 gate negatives, matching terminal manifests and drained close. Required consumer integration remains lead-owned: consume state batches while checking exact ticket currentness before any presentation action. Rendering/Height scaling semantics are unresolved; this fixture does not implement presentation. Round15 milestone is closed; stop idle, no16.
