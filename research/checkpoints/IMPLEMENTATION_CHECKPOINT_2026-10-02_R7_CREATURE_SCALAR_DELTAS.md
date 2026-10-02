# R7-NET-01: typed CREO:3 scalar delta plans

Date: October2,2026. Question: can six registered scalar updates preserve bounded
wire order/count tolerance and apply a complete validated plan without partial
state mutation? Stop after offline plan/state proof and independent oracle.

## Baseline and orientation

Checkout `C:/Users/ancie/.codex/worktrees/c55a/Stardust-Client`, HEAD
`65de8c88141071744dc75594be126b3bab3696a5` plus1888 accepted files.
Manifest SHA256 `A272704C8001B9FB2AD9460E7ADD0A2378FF030E834C1E0BC38EC4543D139F97`;
ZIP SHA256 `F0EAE67F7905165B14E8B2702226A2B5C7962F1EB07D12F087FC50DDF01D98D7`.
Verified both input hashes, safe relative ZIP paths/content before overlay and
all resulting hashes; ignored/unlisted data preserved and main untouched.
Read START_HERE/Round6 acceptance, existing envelope/schema/count/malformed
contracts/checkpoints/tools, status/ledger/function terminology and exact sibling
CreatureObjectDeltaMessage3 producer. Executable SHA256 matched documented
baseline before citing already established decoder/RTTI evidence. No execution.

## Evidence versus policy

[Contract](../../docs/CREATURE_VIEW3_SCALAR_DELTA_CONTRACT.md) lists exact evidence,
scalar types, API and rejection/ownership/limits. Observation: registered
indices11..16 are U8/U8/U64/F32/U32/U64, raw tag4352454F/view3; declared count is
consumed but decoding runs to bounded payload exhaustion. **Confirmed existing
client/server evidence**. Interpretation: preserve ordered typed scalar updates
and duplicate last-wins application. Transactional validation, budgets and
finite-height rejection are **modern policy**, not original callbacks or clamps.
Consequence: malformed tail/index/value never mutates the input state; raw scalar
bits and -0 survive, counts0/65535 remain independent metadata.

## Implementation and negative closure

Added immutable six-scalar state and sealed typed operations, owned plan,
bounded decode/encode using accepted envelope, focused native scenarios/console,
independent Python oracle, contract/checkpoint. Existing envelope/lifecycle/
registry/transition/proof/runners/research maps are read-only. No registry-bound
object or initializer ticket claim; binding follows later.

Golden synthetic fields include all six plus posture/height duplicates, raw
byte/high-bit values, negative nonunit height and negative zero. Native rejects
all prefix/partial-index/value/unsupported/wrong schema/budget/nonfinite cases;
owned inspection/output/input mutation tests pass. Independent oracle compares
native wire/ordered plan/immutable initial+result state with explicit height bits.

Final validation commands/results appended below. Lead runner call:
`CreatureView3ScalarDeltaScenarios.Run()`. Next bounded question: registry-revision
binding of typed plans, separately from baseline/resource readiness. Stop here.


Final verified commands:

```powershell
$env:APPDATA = Join-Path $env:TEMP 'stardust-appdata-networking-r7'
$env:NUGET_PACKAGES = 'C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
dotnet restore poc/round7-tests/Networking/Networking.csproj --configfile local-private/round7/NuGet.Config -p:NuGetAudit=false
dotnet run --no-restore --project poc/round7-tests/Networking/Networking.csproj -p:UseSharedCompilation=false -- --export local-private/round7/scalars
dotnet restore poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj --configfile local-private/round7/NuGet.Config -p:NuGetAudit=false
dotnet build --no-restore poc/Stardust.Core/Stardust.Core.csproj -p:UseSharedCompilation=false
dotnet build --no-restore poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj -p:UseSharedCompilation=false
$env:PYTHONUTF8 = '1'
& C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe tools/python/verify_creature_view3_scalar_deltas.py --exports local-private/round7/scalars
& C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe tools/python/validate_organized_workspace.py .
git diff --check
```

Private NuGet.Config clears all sources; accepted cached packages only. Native
plan/state/rejection/ownership scenarios and independent native wire/ordered
plan/state oracle pass. Core/shared SmokeTests zero warnings/errors. Validator:
1062 Markdown/zero broken/private links,2602 names/zero duplicates,345 packet
rows. Diff check exit0; baseline conversion advisories saved privately.
Shared broad smoke executable compiled, not run. No GPU/headless/original exe,
asset/registry/scene/socket/server execution. Original notification/presentation
semantics and state binding remain outside this milestone.

Seven additions absent from baseline, authored UTF8/LF:

- `poc/Stardust.Core/Protocol/CreatureView3ScalarDeltaCodec.cs`
- `poc/Stardust.Core.SmokeTests/CreatureView3ScalarDeltaScenarios.cs`
- `poc/round7-tests/Networking/Networking.csproj`
- `poc/round7-tests/Networking/Program.cs`
- `tools/python/verify_creature_view3_scalar_deltas.py`
- `docs/CREATURE_VIEW3_SCALAR_DELTA_CONTRACT.md`
- `research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-02_R7_CREATURE_SCALAR_DELTAS.md`

All1888 accepted baseline files hash-verified unchanged. Private exact scope/hash
list and existing scope-review tool verify only these additions. Shared runner/
research/registries untouched; no staging/commit/publication or next milestone.
Result ready for review pending lead acceptance and registration.
