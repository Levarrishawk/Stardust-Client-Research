# R1-NET-01: offline zone scene codecs

Date: October 1, 2026. Question: can the replacement preserve the exact
SelectCharacter and CmdStartScene application bytes with explicit rejection and
owned decoded data, independently verified without network or scene effects?
Stopping condition: two bounded codecs, synthetic positive/negative/ownership
scenarios, independent Python agreement and Core build; no further milestone.

## Inputs and observations

Checkout: `C:/Users/ancie/.codex/worktrees/c55a/Stardust-Client`.
Baseline HEAD: `65de8c88141071744dc75594be126b3bab3696a5`.
Existing modified sphere-floor adoption checkpoint was present and preserved.
Read AGENTS, START_HERE, latest named session and parallel rules; searched current
status/ledger/unknown functions, prior checkpoints, protocol tools and curated
packet rows. Static hash check of `C:/Stardust-DEV/Stardust.exe` matched
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.
No executable execution or server contact.

[Durable contract](../../docs/ZONE_SCENE_PROTOCOL_CODEC_CONTRACT.md) lists exact
client-backed evidence, corresponding server symbols, layouts, APIs and policies.
Observation: original SelectCharacter is count=2/opcode/U64 only; server helper
has an extra hash, but callback reads offset 6. Observation: server scene writes
count=9/opcode/U8/U64/ASCII16/F32 X/Z/Y/ASCII16/U64, matching the durable client
parser evidence. Interpretation: these are application message boundaries,
separate from SOE and scene lifecycle. Confidence: **Confirmed** for layout;
**Unknown** for general flag semantics. Strict invalid-input rejection is a
chosen replacement policy. No selected-object validation is inferred from bytes.

## Implementation and consequences

Added immutable owned DTOs and codec, unique standalone console verification
project, smoke scenarios and independent Python struct oracle. High bits and
coordinate order survive; raw strings and flag survive without normalization.
All malformed paths throw before returning a DTO. No resources, callbacks,
scene changes or connection operations are owned by this implementation.

## Validation and handoff

Native runner and independent oracle results are recorded in the final handoff.
Offline restore uses a private NuGet.Config with cleared sources and existing
primary-checkout package cache; APPDATA points at task-specific TEMP directory.
The first restore hit inaccessible user NuGet.Config; second hit online audit;
cleared offline sources resolved both. Sandbox DLL write restrictions required
approved build execution. No project dependency or shared runner was changed.

Lead must review/integrate this milestone, add `ZoneSceneProtocolScenarios.Run()`
to shared runner if desired, and reconcile shared acceptance/status/ledger and
packet documentation as appropriate. Curated packet rows remain read-only;
existing SelectCharacter helper row direction is not treated as wire authority.
Remaining uncertainty: unknown flag semantics, deployment-specific helper suffix
dependency, legal session-state validation and asynchronous scene lifecycle.
Next probe: bounded offline phase/selected-object validation, preserving byte
parsing as a separate layer. Do not begin it in this round.

Verified commands (all passed):

```powershell
$env:APPDATA = Join-Path $env:TEMP 'stardust-appdata-networking'
$env:NUGET_PACKAGES = 'C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
dotnet restore poc/round1-tests/Networking/Networking.csproj --configfile local-private/round1/NuGet.Config -p:NuGetAudit=false
dotnet run --no-restore --project poc/round1-tests/Networking/Networking.csproj -- --export local-private/round1/zone-scene
dotnet build --no-restore poc/Stardust.Core/Stardust.Core.csproj
$env:PYTHONUTF8 = '1'
& C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe tools/python/verify_zone_scene_protocol.py --exports local-private/round1/zone-scene
& C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe tools/python/validate_organized_workspace.py .
git diff --check
```

Native positive/malformed/ownership and maximum ASCII16 length cases pass.
Independent Python compares actual native exports and passes its own negative
vectors. Core build: zero warnings/errors. Workspace validator: 1,006 Markdown,
zero broken/private links, 2,602 names with zero duplicate addresses, 345 packet
rows. Diff check passes (existing sphere checkpoint has a Git CRLF advisory).
No live execution-based compatibility claim: all wire vectors are synthetic;
original executable and real server interoperability were not run.

Exact new paths (absent in worker preparation baseline):

- `poc/Stardust.Core/Protocol/ZoneSceneProtocolCodec.cs`
- `poc/Stardust.Core.SmokeTests/ZoneSceneProtocolScenarios.cs`
- `poc/round1-tests/Networking/Networking.csproj`
- `poc/round1-tests/Networking/Program.cs`
- `tools/python/verify_zone_scene_protocol.py`
- `docs/ZONE_SCENE_PROTOCOL_CODEC_CONTRACT.md`
- `research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_R1_ZONE_SCENE_CODECS.md`

Result: **ready for review**, pending lead acceptance/integration. Unrelated
sphere-floor checkpoint remains modified; nothing staged or committed.
