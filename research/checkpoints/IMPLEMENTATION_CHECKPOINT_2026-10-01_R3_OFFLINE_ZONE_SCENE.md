# R3-NET-01: offline reliable zone-scene vertical slice

Date: October 1, 2026. Question: can existing reliable transport deliver scene
start and opaque initialization messages in order, then retain/retry one deferred
ready until transport ACK and mark final readiness on later server echo?
Stop after reproducible offline positive/negative transcript and independent
wire oracle. No next round started.

## Accepted baseline and identity

Checkout `C:/Users/ancie/.codex/worktrees/c55a/Stardust-Client`, HEAD
`65de8c88141071744dc75594be126b3bab3696a5` plus accepted 1,762-file manifest.
Manifest SHA256 `3549F10F0AC0602E53837010475CF0139BFEA4D2C1CE2BB3148228887C12C9BC`;
ZIP SHA256 `201434FC21C1C6641658F8CE6055BDC4BCAB2FB25D2B190A2FA84B7E8B7B0A3F`.
Verified both input hashes, every ZIP path and content before overlay, then every
output hash. Preserved unlisted/ignored files and wrote only this checkout.
Read START_HERE/Round2 acceptance, durable scene/transport/R2 policies,
prior checkpoint/tools/status/ledger/function terminology and current transport
implementation. Used existing Python framing/CRC source model rather than
another implementation. Original executable SHA256 matched the documented
baseline before citing lifecycle VAs; never executed it.

## Evidence, interpretation and consequences

[Durable transcript contract](../../docs/OFFLINE_ZONE_SCENE_TRANSCRIPT_CONTRACT.md)
records layouts, exact sequence/bytes, confidence and limitations.
Observation: existing client-backed codec/lifecycle specifications require an
asynchronous barrier, one-shot ready and later server-ready. **Confirmed**.
Observation: independently generated synthetic peer frames pass actual C#
ordered delivery/reassembly; no partial fragment or duplicate dispatch reaches
the coordinator. **Confirmed synthetic execution**, not live interoperability.
Interpretation: existing components support this bounded offline vertical slice.
Generation/time/budget/terminal failure choices are **modern policy**.
Consequences: ordered owned initialization queue transfers once after successful
current barrier, ready is reliable-retained/retransmitted until ACK, later echo
marks final state. Original terrain/replication/resource scheduler remains outside
scope; raw header has no generation ID and cannot resolve misrouted stale frames.

## Implementation and negative closure

New proof owns bounded private dispatcher plus fixture lifecycle; no separate
production router needed. R1/R2 and transport/session/login code remain read-only.
Pinned synthetic hex frames correspond to the independent Python peer. A private
export records actual request/select/ready/retry/controls/staged data for oracle
comparison; no proprietary bytes or credentials appear.

Positive transcript reorders scene and baseline fragments, mutates borrowed and
delivered arrays, rejects corrupt/truncated wire, ignores duplicates, checks
zero ready before barrier, exact retained retries and ACK cleanup, then server
ready. Negative transcripts cover malformed start, wrong selected ID, early
ready, count and byte exhaustion, cancellation of partial fragment/future packet,
replacement while ready retained, stale completion/receive and close cleanup.
No consumer callback or old-scene state exists. Terminal protocol failure clears
coordinator queues and closes transport; individual stage budget rejection is
atomic before that modern failure policy.

## Handoff

Commands/results appended after final verification. Proposed shared registration:
`OfflineZoneSceneScenarios.Run()`. Lead owns shared runner and reconciliation of
acceptance/status/ledger. No scores or packet/address counts raised. Next bounded
question: production phase/routing identity policy and delayed-frame handling,
without assuming authorization for live contact. Stop at this milestone.

Final verified commands:

```powershell
$env:PYTHONUTF8 = '1'
& C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe tools/python/verify_offline_zone_scene.py --generate local-private/round3/peer.json
$env:APPDATA = Join-Path $env:TEMP 'stardust-appdata-networking-r3'
$env:NUGET_PACKAGES = 'C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
dotnet restore poc/round3-tests/Networking/Networking.csproj --configfile local-private/round3/NuGet.Config -p:NuGetAudit=false
dotnet restore poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj --configfile local-private/round3/NuGet.Config -p:NuGetAudit=false
dotnet run --no-restore --project poc/round3-tests/Networking/Networking.csproj -p:UseSharedCompilation=false -- --peer local-private/round3/peer.json --export local-private/round3/transcript
dotnet build --no-restore poc/Stardust.Core/Stardust.Core.csproj -p:UseSharedCompilation=false
dotnet build --no-restore poc/Stardust.Core.SmokeTests/Stardust.Core.SmokeTests.csproj -p:UseSharedCompilation=false
& C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe tools/python/verify_offline_zone_scene.py --exports local-private/round3/transcript
& C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe tools/python/validate_organized_workspace.py .
git diff --check
```

Private NuGet.Config clears all sources; cached packages only. Independent peer,
self-contained native proof, imported Python-peer proof and source-wire oracle
pass, including actual exported C# bytes and exact nine-control sequence.
Both builds: zero warnings/errors. Validator: 1,026 Markdown, zero broken/private
links, 2,602 canonical names/zero duplicates, 345 packet rows. Diff check exit 0;
baseline line-ending advisories retained privately. Shared broad smoke executable
compiled but not run; focused proof ran twice (default and independent imports).
No original client, live server, socket/DNS, GPU or game resource check was run.
These are synthetic execution results, not full application interoperability.

Exact new files (absent from accepted baseline):

- `poc/Stardust.Core/Proof/OfflineZoneSceneScenario.cs`
- `poc/Stardust.Core.SmokeTests/OfflineZoneSceneScenarios.cs`
- `poc/round3-tests/Networking/Networking.csproj`
- `poc/round3-tests/Networking/Program.cs`
- `tools/python/verify_offline_zone_scene.py`
- `docs/OFFLINE_ZONE_SCENE_TRANSCRIPT_CONTRACT.md`
- `research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-01_R3_OFFLINE_ZONE_SCENE.md`

All 1,762 accepted baseline files are hash-verified unchanged after work. Private
baseline-relative file/hash inventory records these seven additions. No shared
production code, runners or canonical ledgers/registries edited. Result **ready
for review**, pending lead integration; no staging/commit/publication or next
round. Suggested lead shared runner call: `OfflineZoneSceneScenarios.Run()`.
