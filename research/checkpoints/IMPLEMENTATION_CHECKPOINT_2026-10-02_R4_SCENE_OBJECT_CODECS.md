# R4-NET-01: strict scene object lifecycle codecs

Date: October 2, 2026. Question: can four documented lifecycle messages encode
and decode exact bytes while preserving raw flags/full-width fields/transform
ordering and independently rejecting malformed boundaries? Stop at codecs and
synthetic fixtures; no registry, scene application or live contact.

## Baseline, orientation and identity

Checkout `C:/Users/ancie/.codex/worktrees/c55a/Stardust-Client`, HEAD
`65de8c88141071744dc75594be126b3bab3696a5` plus 1,792 accepted public files.
Manifest SHA256 `E53B9DECDA458CB7EA9F335EA4EDA9422EA915415F514316D7E07DF2507609D7`;
ZIP SHA256 `D7364D604436D086B9103C2A76489CE51BB27B1BBE464B07FB918F4A586A5B38`.
Verified input hashes/all ZIP paths and content before overlay, then all resulting
hashes. Preserved ignored/unlisted data; main untouched. Read START_HERE/latest
session/Round3 acceptance, lifecycle packet/replication docs, prior end-baselines
checkpoint, status/ledger/function terminology, curated producer rows and existing
codec/oracle tools. SHA256 of original executable matched
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.
No original executable execution or server/socket/DNS activity.

## Findings versus policy

[Durable contract](../../docs/SCENE_OBJECT_LIFECYCLE_CODEC_CONTRACT.md) records
exact client-backed parsers, current server constructor symbols and layouts.
Observation: create is count5/FE89DDEA/U64/quaternion X/Y/Z/W/position X/Z/Y/U32/U8;
destroy is count3/4D45D504/U64/U8; containment is count4/56CBDE9E/U64/U64/U32;
end-baselines is count2/2C436037/U64. Width/order/identities **Confirmed** by
existing durable client evidence and independent server boundary. Interpretation:
these map to scalar immutable protocol DTOs without native padding. Consequence:
IDs never become pointer-sized; raw flags, finite float bits, quaternion scale and
arrangement high bits are preserved. General noncanonical flag meaning and
semantic arrangement signedness are **Unknown**; uint ArrangementBits preserves
server uint32 type and wire bits without guessing. Nonfinite/exact-boundary
rejection is **modern policy**. No original state behavior is newly inferred.

## Implementation and verification

Added one engine-neutral codec/owned DTO file, native smoke scenarios,
standalone console, independent Python oracle, contract and this checkpoint.
Golden fixture is hand-authored synthetic content; oracle packs/parses with
independent Python struct and compares actual native exports and full DTO fields.
No proprietary corpus, parser duplication, shared registry or state application.

Commands/results appended below after final validation. Lead-owned shared runner
call proposed: `SceneObjectLifecycleScenarios.Run()`. Next bounded question:
offline object registry readiness/existence/order policies, separately from byte
parsing. Worker stops at this milestone, pending lead acceptance.

Final verification commands (passed):

```powershell
$env:APPDATA = Join-Path $env:TEMP 'stardust-appdata-networking-r4'
$env:NUGET_PACKAGES = 'C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
dotnet restore poc/round4-tests/Networking/Networking.csproj --configfile local-private/round4/NuGet.Config -p:NuGetAudit=false
dotnet run --no-restore --project poc/round4-tests/Networking/Networking.csproj -p:UseSharedCompilation=false -- --export local-private/round4/lifecycle
$env:PYTHONUTF8 = '1'
& C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe tools/python/verify_scene_object_lifecycle.py --exports local-private/round4/lifecycle
dotnet restore poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj --configfile local-private/round4/NuGet.Config -p:NuGetAudit=false
dotnet build --no-restore poc/Stardust.Core/Stardust.Core.csproj -p:UseSharedCompilation=false
dotnet build --no-restore poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj -p:UseSharedCompilation=false
& C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe tools/python/validate_organized_workspace.py .
git diff --check
```

Private NuGet.Config clears package sources; cached packages only. Native full
field/golden/negative/ownership suite and independent Python actual-byte/full-DTO
oracle pass. Both Core/shared SmokeTests builds have zero warnings/errors.
Validator: 1,035 Markdown/zero broken or private links, 2,602 names/zero duplicate
addresses, 345 packet rows. Diff check exits zero; baseline line-ending advisories
retained privately. Shared smoke executable compiled but broader unrelated suite
not run. No original executable, live server, asset/GPU or registry execution.

Exact seven additions, absent from accepted baseline:

- `poc/Stardust.Core/Protocol/SceneObjectLifecycleCodec.cs`
- `poc/Stardust.Core.SmokeTests/SceneObjectLifecycleScenarios.cs`
- `poc/round4-tests/Networking/Networking.csproj`
- `poc/round4-tests/Networking/Program.cs`
- `tools/python/verify_scene_object_lifecycle.py`
- `docs/SCENE_OBJECT_LIFECYCLE_CODEC_CONTRACT.md`
- `research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-02_R4_SCENE_OBJECT_CODECS.md`

All 1,792 baseline files hash-verified unchanged; private scope/hash inventory
records exact additions. No shared runners/docs/registries changed. Ready for
review, pending lead integration and runner registration. No staging, commit,
push/publication, other-chat messaging or subsequent milestone started.
