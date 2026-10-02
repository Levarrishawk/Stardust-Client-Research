# R5-NET-01: owned scene object registry

Date: October 2, 2026. Question: can bounded lifecycle state preserve raw accepted
DTOs while rejecting invalid graph/scene updates and invalidating stale async
handles atomically? Stop after offline registry proof and documented modern
policy; no baseline parser, resource/engine adapter or live contact.

## Accepted baseline and orientation

Checkout `C:/Users/ancie/.codex/worktrees/c55a/Stardust-Client`; HEAD
`65de8c88141071744dc75594be126b3bab3696a5` plus 1,824-file accepted manifest.
Manifest SHA256 `77D479877EA2ED1A4B8A79A50C23DF12DAA5100F03C0A9521C09EF513BADD12F`;
ZIP SHA256 `705A7FB6D48CC95FD49EFAF764761FD879CB218A26190C8945C48B33743DFCD8`.
Verified both inputs and all safe relative ZIP paths/content before overlay, then
all output hashes in this checkout; unlisted/ignored data preserved, root untouched.
Read START_HERE/latest session/Round4 acceptance and R4 codec contract,
replication/lifecycle evidence, prior end-baselines checkpoint and status/ledger/
function terminology. No new VA research or executable use. R4 fixture/oracle
identity remains accepted evidence; no new parser or independent asset dependency.

## Findings, policy and compatibility consequence

[Durable contract](../../docs/SCENE_OBJECT_REGISTRY_CONTRACT.md) defines API,
identity/ownership, graph/revision invariants and all rejection/duplicate policies.
Observation: accepted research separates created/pending, finalized via end and
removed via destroy. **Confirmed existing evidence**. Observation: native
registry transactions pass pinned R4 lifecycle/graph/exhaustion scenarios.
**Confirmed synthetic execution**. Interpretation: a bounded engine-neutral
registry can model that distinction without parsing actual baseline fields.
All duplicate, revision, strict-parent, graph, cleanup and generation choices
are **modern policy**, not inferred original repair or readiness requirements.

Consequence: original raw scalar data stays immutable, no normalization or engine
lookup occurs, and async consumers get owner/scene/object/revision handles.
Conflicting create requires explicit replacement. Parent absence/cycles are
strictly rejected, not repaired. Finalized only records end acceptance and is
not full resource/baseline readiness. Reset/Close clear registry-owned entries
without mutating previous snapshots.

## Implementation and negative closure

Added one registry/state/snapshot/handle file, smoke scenarios, unique console,
contract and checkpoint. Codec/transition/proof/shared runners and research
registries remain read-only. R4 hand-pinned packet bytes feed tests directly,
including high-bit IDs, raw flags, asymmetric quaternion and arrangement bits.
Tests cover atomic count/cycle/parent/DTO/scene rejection, stale/foreign handles,
identity-preserving duplicates, explicit replacement/direct-child detachment,
finalization versus removal, owned old snapshots and scene/revision exhaustion.
Multi-child destroy/replace overflow is rejected without partial changes.

Commands/results appended after final verification. Lead must review/integrate
and register `SceneObjectRegistryScenarios.Run()` in shared runner if accepted.
Next probe: explicit required baseline-view bookkeeping; no next milestone starts.

Final verified commands:

```powershell
$env:APPDATA = Join-Path $env:TEMP 'stardust-appdata-networking-r5'
$env:NUGET_PACKAGES = 'C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
dotnet restore poc/round5-tests/Networking/Networking.csproj --configfile local-private/round5/NuGet.Config -p:NuGetAudit=false
dotnet run --no-restore --project poc/round5-tests/Networking/Networking.csproj -p:UseSharedCompilation=false
dotnet restore poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj --configfile local-private/round5/NuGet.Config -p:NuGetAudit=false
dotnet build --no-restore poc/Stardust.Core/Stardust.Core.csproj -p:UseSharedCompilation=false
dotnet build --no-restore poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj -p:UseSharedCompilation=false
$env:PYTHONUTF8 = '1'
& C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe tools/python/validate_organized_workspace.py .
git diff --check
```

Private NuGet.Config clears package sources; offline accepted cache only.
Final native pinned lifecycle/graph/revision/ownership/exhaustion scenarios pass.
Core/shared SmokeTests compile with zero warnings/errors. Validator: 1,044
Markdown/zero broken or private links, 2,602 names/zero duplicates, 345 packet
rows. Diff check exit0; baseline line-ending advisories saved privately.
Broader shared smoke executable compiled, not run. No new Python wire oracle
because accepted R4 bytes/codecs are unchanged. No original executable, network,
GPU/headless scene, template/assets or baseline field execution performed.

Six additions absent from baseline:

- `poc/Stardust.Core/Session/SceneObjectRegistry.cs`
- `poc/Stardust.Core.SmokeTests/SceneObjectRegistryScenarios.cs`
- `poc/round5-tests/Networking/Networking.csproj`
- `poc/round5-tests/Networking/Program.cs`
- `docs/SCENE_OBJECT_REGISTRY_CONTRACT.md`
- `research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-02_R5_OBJECT_REGISTRY.md`

All 1,824 baseline hashes unchanged; private exact baseline-relative hash list
records these additions. Shared runners/docs/registries untouched, index empty.
Result ready for review, pending lead acceptance/integration. No staging/commit,
publishing, other-chat message or Round6 work started.
