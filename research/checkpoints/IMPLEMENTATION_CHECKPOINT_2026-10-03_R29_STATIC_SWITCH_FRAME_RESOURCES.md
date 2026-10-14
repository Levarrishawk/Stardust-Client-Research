# Round29 static all-frame resource join — 2026-10-03

## Question and stopping condition

Can an explicit all-frame option join the accepted immutable SWTS frame bundle with static scene loading, using the same outer source cache, honest duplicate retained-pixel accounting and no-I/O completion checks? Stop after original source/pixel/header oracle, bounded fault/ownership/cancellation/reentry cases, default creature/static/routing/validation/accounting regressions and exact scope validation. World/Godot/cursor/clock integration remains lead-owned.

## Inputs and orientation

Verified Round29 snapshot:2,695 unique public members; manifest SHA-256 `c43768a1a852406dc18ef4dfed27b66126c72355361c8364295efc638225897b`, ZIP `268128b23b05c8e77c2ce359e957057b303f3f920362f24ad879f189ffbb3d22`. Full member list/size/hash/archive identity, containment and ZIP/target/ancestor symlink/Windows-reparse checks precede overlay; all output hashes reverified. Private baseline receipt retained. Worker HEAD `65de8c88141071744dc75594be126b3bab3696a5`, main snapshot HEAD `1fc55dd89cd23ce3af49e3fe471210bdebe9735c`; index/unlisted/private preserved.

Read START/latest accepted28/session29, current status/ledger/UNKNOWN, static/frame-bundle/router/validation/accounting contracts, checkpoints, existing source/cache/mesh/frame decoders and accepted Python original union/header/pixel tools. No new executable/VA claims or registry changes. Original root `C:/Stardust-DEV`, configuration `swgemu_live.cfg` SHA `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`, pinned scene catalog and human locomotion profiles remain read-only. No original execution/live/GPU/downloads.

## Scope and API

Four focused modifications: [static loader](../../../SWG-Godot-Client/poc/Stardust.Core/Assets/SceneStaticResourceReader.cs), [options/router](../../../SWG-Godot-Client/poc/Stardust.Core/Assets/SceneObjectResourceRouter.cs), [completion gate](../../../SWG-Godot-Client/poc/Stardust.Core/Assets/SceneObjectResourceValidation.cs), [logical accounting](../../../SWG-Godot-Client/poc/Stardust.Core/Assets/SceneObjectResourceAccounting.cs). Six additions: [scenario](../../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/SceneStaticSwitchFrameResourcesScenarios.cs), [contract](../../docs/SCENE_STATIC_SWITCH_FRAME_RESOURCES_CONTRACT.md), this checkpoint, [project](../../../SWG-Godot-Client/poc/round29-tests/Assets/Assets.csproj), [entry point](../../../SWG-Godot-Client/poc/round29-tests/Assets/Program.cs), [oracle](../../../SWG-Godot-Client/poc/round29-tests/Assets/verify_scene_switch.py). No other baseline/shared/world/Godot/default Program edits.

Append optional `SceneStaticSwitchFramePolicy? switchFrames = null` to the static loader/options constructor. Policy exposes optional bundle `Limits`, total `MaximumRetainedMips` and `MaximumPendingWeight`; resource record exposes optional immutable `SwitchFrames`. Accounting adds four get-only bundle counters. Existing calls/default scalar values remain valid; receipt JSON includes additive zero counters. Exact bounds, source/cancellation/ownership/error and validation policies live in the durable contract.

## Observations, interpretation and consequences

**Confirmed modern observation:** original default frame0 closure remains27 sources /1,401,794 bytes /7 texture representations /62 mips /2,140,836 decoded bytes /3,542,630 pending weight. Explicit all-frame closure is34 /1,479,410 /15 /126 /2,490,372 /3,969,782. Seven added unique paths are authored frames02..08, each11,088 bytes. Independent configured-overlay source identities and all64 bundle mip hashes match the [accepted original pins](IMPLEMENTATION_CHECKPOINT_2026-10-03_R28_SWITCH_FRAME_BUNDLE.md). Every authored image is128x64/eight mips/43,692 RGBA bytes. No overwritten default DDS read; accepted material effect dependencies remain present.

**Interpretation / Confirmed implementation:** appearance selected DDS and bundle selected immutable pixels are separate retained representations. The bundle adds349,536 bytes and64 mips, including another43,692-byte frame0 representation. Global decoded/mip/pending totals charge both; source paths charge once. This is logical retained-input accounting, not heap/peak/GPU memory. Alias paths share bundle storage; equal-byte distinct paths remain separate. Default appearance accounting is unchanged.

**Confirmed modern policy:** complete materials and an explicit pinned selection are required. Count/reference/interval and raw whole-path/base-binding eligibility preflight before additional DDS reads. A single outer cache supplies the entire scene union, including bundle requests; no duplicate external reads. Encoded bundle/per-input/pending growth checks run before additional cache copies. The complete encoded union is reserved before remaining global decoded/mip/pending budgets narrow bundle decode limits. Failed late data, budgets, cancellation, callback or swallowed/unswallowed recursive load publish nothing; earlier completions survive.

**Confirmed completion behavior:** private immutable association records selected pixel/source-metadata hashes, exact bundle/policy/index association and material-binding identity. Public record copies cannot bypass raw create/catalog/source/appearance checks. Selected-image backing mutation, changed bundle/reference bindings/base/count/interval/tag/path and contradictory nested sources reject. Validator hashes existing spans without clone accessors/pixel copying or source I/O, after explicit work bounds. Accounting reads lengths only. Metadata consistency is not independent origin attestation; accepted loaders are trusted, with original bytes independently checked by the oracle.

**Compatibility consequence:** the loader now provides all authored frame inputs alongside its complete static CPU material closure. No per-tick decode or world-ready/rendering/clock/legacy numeric parity claim follows. The reviewed presentation stage must consume these inputs separately.

## Validation and reproduction

Final offline cached .NET8 build: zero warnings/errors. New native gate:69 rejected cases,11 controlled checks and eight concurrent loads. Exact/one-less outer/bundle/global decoded/mip/pending limits; pre-existing/intermediate growth stops at first additional callback before source copy; all ten bundle paths missing/masked; late truncation/tail; count/interval/raw-word/ordered-tag/base faults; option/pin/binding/source/record association faults; appearance backing mutation; aliases/equal-byte path identities; borrowed-array cleanup; pre/first/last cancellation; callback exception identity; swallowed/unswallowed reentry and guard recovery pass. Original archives close before validation/receipts and borrowed inputs clear without affecting output.

Independent oracle composes accepted static boundary audit plus raw original SWTS/base and Pillow DDS decoder. It derives34-source union/seven added paths mechanically, checks all34 actual hash/priority/size identities, all64 frame mip hashes, default DDS headers/62 mip lengths and duplicate retained-storage totals. Native controlled behaviors are receipt evidence, not separately executed by Python.

Fresh default regressions: completion89 negatives/seven positives, router65, creature albedo16, switch168, accounting15. Independent regression auditors verify original source/wire/geometry/material/pixel and66-source/17-DDS/142-header-mip accounting. The unchanged R16 oracle initially rejected additional receipt keys: two prior generated-face counters plus four new switch counters. The own compatibility mode asserts all six are zero in default fixtures, removes only those keys into ignored output, then the unchanged oracle validates every prior scalar. No assertion is weakened in canonical R16 code.

First core build failed on new guard/local geometry `Check` name collision, corrected to `Guard`. First new fixture used `IOException` where modern frame eligibility throws `InvalidDataException`; exact exception expectations were corrected. These initial failures and the receipt-shape adaptation are distinct from final passing gates.

```powershell
$env:APPDATA=Join-Path $env:TEMP 'stardust-assets-round29-appdata'
$env:NUGET_PACKAGES='C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
$env:PYTHONUTF8='1'
$python='C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
# Private nuget/NuGet.Config clears packageSources; cached offline inputs only.
dotnet restore poc/round29-tests/Assets/Assets.csproj --configfile local-private/round29/nuget/NuGet.Config -p:NuGetAudit=false
dotnet build poc/round29-tests/Assets/Assets.csproj --no-restore -p:UseSharedCompilation=false -p:UseAppHost=false
$dll='poc/round29-tests/Assets/bin/Debug/net8.0/Assets.dll'
$root='C:/Stardust-DEV'; $config='C:/Stardust-DEV/swgemu_live.cfg'
$catalog='poc/preview-profiles/scene-object-catalog.json'; $bank='poc/preview-profiles/human-locomotion.json'
dotnet $dll $root $config $catalog > local-private/round29/native.txt
dotnet $dll static $root $config > local-private/round29/static.txt
foreach($mode in @('validation','router','creature','accounting')) { dotnet $dll $mode $root $config $catalog $bank > "local-private/round29/$mode.txt" }
& $python poc/round29-tests/Assets/verify_scene_switch.py $root $config local-private/round29/native.txt local-private/round29/static.txt
& $python poc/round14-tests/Assets/verify_validation.py $root $config $catalog local-private/round29/validation.txt local-private/round29/router.txt local-private/round29/creature.txt local-private/round29/static.txt
& $python poc/round29-tests/Assets/verify_scene_switch.py accounting-compat local-private/round29/accounting.txt local-private/round29/accounting-compatible.txt
& $python poc/round16-tests/Assets/verify_accounting.py $root $config local-private/round29/accounting-compatible.txt
& $python tools/python/validate_organized_workspace.py .
```

Final baseline-relative scope/whitespace/EOF/link checks: exactly four modified files/six additions, other2,691 baseline members unchanged, unchanged HEAD/empty index, private/build outputs ignored, no broken/private links. No assets or proprietary receipt corpora are promoted or staged.

## Exact next probe

Lead acceptance and explicit main-host opt-in using this completed resource route plus separately reviewed supplied GPU-frame presentation/cursor. Verify incarnation/currentness/retirement at that integration boundary. No new round starts from this worker handoff.
