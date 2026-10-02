# R2-NET-01: offline scene transition coordinator

Date: October 1, 2026. Question: can an engine-neutral coordinator preserve the
selected-object/deferred-ready boundary while atomically staging owned opaque
messages and rejecting stale completion? Stop after offline coordinator, exact
ready codec, independent fixture and proportional ownership/negative checks.

## Baseline and orientation

Checkout `C:/Users/ancie/.codex/worktrees/c55a/Stardust-Client`; HEAD
`65de8c88141071744dc75594be126b3bab3696a5` plus accepted Round 2 file manifest.
Manifest SHA-256 `E2AFB787D6CDB9F8C1971183D5A63CDC2B4736E24EC41E039F94FEEAF058D097`;
ZIP SHA-256 `CEBF4BD8017249ECC1FC9A9647900BD4062C134CA10040D5B2420C04A3CC4981`.
Verified both input hashes, validated every ZIP path and source hash before
writing, overlaid exactly 1,735 files into this checkout, then verified every
output hash. Unlisted files/ignored outputs preserved. Primary checkout untouched.

Read current AGENTS/START_HERE/latest session, Round 1 integration report,
parallel rules, scene lifecycle specs and prior checkpoint, status/ledger/function
entries, packet rows and existing codec/oracle tools. No duplicated R1 parser.
Executable hash matched the documented baseline before citing VAs. No original
program executed, no live connection or endpoints.

## Observation, interpretation and consequence

[Contract](../../docs/ZONE_SCENE_TRANSITION_CONTRACT.md) carries exact evidence and
API policy. Observation: client handler begins asynchronous scene setup, sole
readiness sender emits header-only one-shot acknowledgement, later server-ready
branch marks final readiness. Server constructor independently emits count=1,
opcode=43FD1C22. Confidence: **Confirmed** for these bounded facts.
Interpretation: an offline generation-tagged barrier can preserve the deferred
boundary without defining resource or replication semantics. **Modern policy**
for generations, budgets, replacement and recovery; no legacy queue claim.
Consequence: opaque messages stay ordered/owned; only current successful barrier
publishes one header plus queue, and later valid echo marks Ready.

## Implementation and limits

Added coordinator/state enum/owned publication, exact SceneReady codec, permanent
standalone Round 2 console, smoke scenarios and independent Python wire oracle.
Wrong state/ID/header, malformed DTO and overbudget queue additions do not mutate
accepted state. Stale completion is inert. Reset/failure/replacement/close clear
coordinator-owned references; previously published caller-owned data survives.
Single serialized owner is required. The generation is local, absent on the
wire; delayed old echo incorrectly tagged current cannot be distinguished here.
No terrain construction, cancellation of real tasks, payload consumer or ready
socket transmission exists. See contract for exact rejection policies.

## Validation

Results and exact commands appended after final verification. Public golden
bytes are independently authored synthetic headers. No proprietary assets or
raw decompiler output promoted. Shared root documents/registries/runners remain
lead-owned. Proposed runner call: `ZoneSceneTransitionScenarios.Run()`.
Next question: bounded offline transport transcript and generation-routing
policy. Round 3 dispatch belongs to the lead; no next milestone started.

Final verification passed:

```powershell
$env:APPDATA = Join-Path $env:TEMP 'stardust-appdata-networking-r2'
$env:NUGET_PACKAGES = 'C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
dotnet restore poc/round2-tests/Networking/Networking.csproj --configfile local-private/round2/NuGet.Config -p:NuGetAudit=false
dotnet restore poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj --configfile local-private/round2/NuGet.Config -p:NuGetAudit=false
dotnet run --no-restore --project poc/round2-tests/Networking/Networking.csproj -p:UseSharedCompilation=false -- --export local-private/round2/scene-ready
dotnet build --no-restore poc/Stardust.Core/Stardust.Core.csproj -p:UseSharedCompilation=false
dotnet build --no-restore poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj -p:UseSharedCompilation=false
$env:PYTHONUTF8 = '1'
& C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe tools/python/verify_scene_ready_protocol.py --exports local-private/round2/scene-ready
& C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe tools/python/validate_organized_workspace.py .
git diff --check
```

The private NuGet.Config clears package sources; cached packages only. Native
positive/negative/ownership/cleanup suite and independent actual-byte oracle
pass. Both Core and shared SmokeTests compile with zero warnings/errors.
Validator: 1,016 Markdown files, zero broken/private links, 2,602 names with zero
address duplicates, 345 packet rows. git diff --check exits zero; Git emits
baseline line-ending conversion advisories, not validation failures. Shared
SmokeTests executable was compiled, not run as a broad unrelated suite. No
original executable, network, scene resource loader or live server test run.

New files only (all absent from accepted manifest):

- `poc/Stardust.Core/Session/ZoneSceneTransition.cs`
- `poc/Stardust.Core/Protocol/SceneReadyProtocolCodec.cs`
- `poc/Stardust.Core.SmokeTests/ZoneSceneTransitionScenarios.cs`
- `poc/round2-tests/Networking/Networking.csproj`
- `poc/round2-tests/Networking/Program.cs`
- `tools/python/verify_scene_ready_protocol.py`
- `docs/ZONE_SCENE_TRANSITION_CONTRACT.md`
- `research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_R2_ZONE_SCENE_TRANSITION.md`

Private baseline-relative file/hash inventory is recorded for lead review; no
private path links in curated documents. Lead must review/integrate, register the
proposed shared runner call, and reconcile shared acceptance/status/ledger as
appropriate. Result: **ready for review**, not final lead acceptance. No staging,
commit, publication, other-chat message or Round 3 work performed.
