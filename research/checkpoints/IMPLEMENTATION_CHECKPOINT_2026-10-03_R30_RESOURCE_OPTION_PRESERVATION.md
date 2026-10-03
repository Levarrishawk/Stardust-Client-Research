# Round30 resource option preservation — 2026-10-03

## Question and stopping condition

Does the accepted29 `SceneObjectResourceRequest.CopyOptions` fix preserve every static all-frame option, plus creature nondefault/face options, through public request construction and actual initializer callbacks? Can the same boundary accept matching pinned terminal completion and suppress mismatched or retired work? Stop after focused field/ownership/completion gates, independent original source/mip/header oracle and exact scope/link/whitespace checks. No production change or repeated69-resource-negative campaign.

## Inputs and orientation

Verified Round30 snapshot:2,725 unique public files; manifest SHA-256 `ba8f50e9b3c9fb5b378e5136bbbbfc0bcd14c2a9088cc401cef04d1ad90ded48`; ZIP `58909621e901a194eb73ba54011b51a45399e819fe57128f3234e51af27d3c0a`. Full archive/member/size/hash identity, safe resolved/lexical containment and archive/target/ancestor symlink/Windows-reparse preflight precede listed-byte overlay; all2,725 output hashes verified. Private baseline recorded. Worker HEAD `65de8c88141071744dc75594be126b3bab3696a5`, snapshot HEAD `1fc55dd89cd23ce3af49e3fe471210bdebe9735c`, index/unlisted/private preserved.

Read START/latest29/session30, status/ledger/UNKNOWN, initializer/all-frame/router contracts, accepted29 combined checkpoint and option-copy correction, prior initializer checkpoint, constructors/registry/world existing fixtures, and curated profile/tools before adding the own field oracle. Registry currently2,626 distinct names/zero duplicates; no new name/VA/count claim. No executable input or original execution. Read-only root `C:/Stardust-DEV`; config SHA `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`; accepted scene catalog profile. Synthetic create fields preserve signed zero/subnormal/nonunit values and use original terminal CRC2907007746.

## Scope and implementation

Six additions only: [scenario](../../poc/Stardust.Core.SmokeTests/SceneResourceOptionPreservationScenarios.cs), [durable contract](../../docs/SCENE_RESOURCE_OPTION_PRESERVATION_CONTRACT.md), this checkpoint, [project](../../poc/round30-tests/Assets/Assets.csproj), [entry point](../../poc/round30-tests/Assets/Program.cs), [fixture producer/oracle](../../poc/round30-tests/Assets/verify_options.py). Existing production/shared Program/world/Godot remain byte-identical to baseline. Test entry point `SceneResourceOptionPreservationScenarios.Run(string[] args)` takes root/config/catalog/literal-fixtures. No new production API.

The own literal producer defines ten cases, including constructor defaults/null-policy levels, every nondefault static selection/graph/decoded/all-frame/bundle/DDS field, two creature choices and explicit signed face values/factory flags. Native projection names every public field directly; independent Python expectations apply documented constructor path/pin normalization and defaults, not reflection-derived output. Expected request and actual callback values are checked separately. Synthetic animation paths/routes/face values test option ownership only, without claims that their assets load.

## Findings, confidence and consequences

**Observation / Confirmed modern behavior:**10 cases produce20 exact request/callback projections. Snapshot/create/options and static nested records are copied. Creature banks/selections/routes own readonly copies; clearing/overwriting original mutable routes/maps/face-value dictionaries preserves projections. Sealed immutable face policy may be shared. All synthetic callback jobs cancel and drain with unfinished work charged until Poll. Interpretation: an option added to constructors can no longer disappear silently at this tested request boundary. Consequence: future option fields need explicit copy/projection regression coverage.

**Regression / Confirmed:** reconstructed static options that omit `SwitchFrames` fail the exact all-frame projection. The actual request cases additionally require owned nonnull all-frame policy and every nested limit, so removing the accepted copy argument would fail before original asset work. Production code is not modified to simulate the old defect.

**Original input / Confirmed:** actual initializer callback starts a dedicated worker using copied exact all-frame options. Its readonly overlay reads34 unique paths, returns the accepted complete resource, closes archives and clears borrowed arrays before task completion. Poll returns the exact resource once. Independent raw template inheritance/APT/DTLA/MESH/SSHT/SWTS parsing reconstructs the34-source union; all priority/size/SHA records and64 Pillow DDS mip hashes match. Header-derived totals remain126 retained mips/3,969,782 pending bytes. Seven authored frame paths supplement the default27-source union; selected appearance/frame0 duplicate retained representation stays charged. Private in-memory snapshots allow default and altered-policy resources without additional original archive jobs.

**Completion / Confirmed modern behavior:** matching Prepared; missing-bundle/default/altered-policy Faulted; explicit cancellation Canceled; containment and registry closure Stale. All seven outcomes leave zero outstanding jobs and only Prepared exposes resources. The existing validator establishes option/resource consistency; initializer Poll invokes it, handles fault conversion, ticket/currentness and transfer. Two closed initializer drains cover late success/fault, retain unfinished charge until completion, observe fault and reject closed Poll. Repeated Dispose remains safe. No scheduling/GPU/live/original-lifecycle claim follows.

**API rejection / Confirmed:** null options, both mismatched recipes and unsupported catalog recipe reject. Supported option implementations are sealed and their abstract base has private-protected construction, making an unsupported subclass unavailable through the public API. Reflection checks only API constructibility and constructs one malformed completion through the existing internal result wrapper; it does not derive expected fields. No execution coverage is claimed for the unreachable internal unsupported-option fallback branch.

## Validation and reproduction

Final cached offline .NET8 build:zero warnings/errors; native PASS and independent oracle PASS. Ten option cases/20 projections, seven completion outcomes, two closed drains,34 source identities/64 frame mip hashes; all2725 baseline files unchanged. No production defect found. Earlier accepted resource/world/renderer behavior is not retested wholesale for this test-only milestone.

Initial new fixture build referred to nonexistent `BlueprintCustomizationKey.Shared`; corrected to the existing raw `FactoryFlag` member and explicit fixture label. First independent union audit assumed the mesh inspector returned a `shaders` list; its actual schema reports aggregate `shader_dependencies`, so shader names are read from the accepted raw SPS IFF structure. Neither failure required production edits or weakened evidence checks. The final gates above passed after correction.

```powershell
$env:APPDATA=Join-Path $env:TEMP 'stardust-assets-round30-appdata'
$env:NUGET_PACKAGES='C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
$env:PYTHONUTF8='1'
$python='C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
# Private nuget/NuGet.Config: <configuration><packageSources><clear/></packageSources></configuration>
& $python poc/round30-tests/Assets/verify_options.py generate local-private/round30/fixtures.json
dotnet restore poc/round30-tests/Assets/Assets.csproj --configfile local-private/round30/nuget/NuGet.Config -p:NuGetAudit=false
dotnet build poc/round30-tests/Assets/Assets.csproj --no-restore -p:UseSharedCompilation=false -p:UseAppHost=false
dotnet poc/round30-tests/Assets/bin/Debug/net8.0/Assets.dll C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg poc/preview-profiles/scene-object-catalog.json local-private/round30/fixtures.json > local-private/round30/native.txt
& $python poc/round30-tests/Assets/verify_options.py C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg local-private/round30/native.txt > local-private/round30/python-audit.json
& $python tools/python/validate_organized_workspace.py .
```

Private logs: native.txt, python-audit.json, fixtures.json, baseline.json, changed-files.json, baseline-relative.diff, scope-review.json, workspace-validation.txt and git-status.txt under the task's ignored round30 directory. Final full manifest-relative review confirms six additions/zero existing modifications, unchanged HEAD/empty index, ignored private/build output, no broken/private links and clean whitespace/EOF. No assets or proprietary receipt corpus are promoted.

## Exact resume point

Lead replay/acceptance and shared scenario registration if desired. Existing initializer option-copy fix needs no further production change. Continue only with another assigned milestone after combined30 acceptance.
