# Independent mixed presentation observation peer

Round15-N drives the accepted generic `SceneWorldCoordinator` directly and predicts its observations with an independently written Python model. It does not implement or require a presentation consumer, adapter, codec, transport or registry change. The frozen [presentation interface](../poc/Stardust.Core/Session/ISceneWorldObjectPresentation.cs) remains read-only. These observations can later drive a consumer test without conflating resource-attempt freshness and state-incarnation freshness.

## API, inputs and peer shape

```csharp
SceneMixedPresentationPeerScenarios.Run(
    string originalRoot, string configurationPath, string catalogPath,
    string bankPath, string peerPath, string exportPath);
```

The public entry point is in `Stardust.Core.SmokeTests`; the [standalone runner](../poc/round15-tests/Networking/Networking.csproj) takes those exact six arguments. The [producer/oracle](../poc/round15-tests/Networking/peer_model.py) handpacks application bytes without production encoders and reuses the previously independent [Round14 wire helper](../poc/round14-tests/Networking/peer.py) for a small lifecycle-order framing trace. It does not read C# source or import production codecs. The native fixture is capped at 1 MiB, version 1, seven initial messages and at most 64 scripted actions.

Peer JSON keys are Version, Label, Applications (key to whole APPLICATION hex), Initial (ordered keys), SessionResponse, Transcript (wire hex deliveries), and Actions (Name, Op, nullable Key/Job). Supported actions are stage, barrier, accept, hold, ack, foreign, complete, poll, fail and close. Held batches and resource completions are local test metadata, not wire fields. Generated peer SHA-256 is `F3923E71D61A5F386E50FBC023D8FB00E9C42BDCC42A7CCFC0BC5B332BD734AB` with deterministic UTF-8/LF output.

IDs are human `0xF122334455667788` and terminal `0xFE02030405060708`, with the accepted catalog CRCs `2937962913` and `2907007746`. Whole 47-byte creates retain quaternion X/Y/Z/W, position wire order X/Z/Y, negative zero, a subnormal and raw flags 255/165. Human CREO3 baseline/scalar deltas use accepted layouts. TANO3 baseline/delta fields are explicitly SYNTHETIC OPAQUE bytes and raw count 65535; no static field schema is asserted.

Initial start/create/baseline/end messages are sent in a short independent SOE channel-0 transcript: one CRC-damaged delivery, reordering and a duplicate. Accepted framing/reliable ordering stages exactly seven applications. This trace tests lifecycle order; broad transport impairment coverage remains in prior scenarios.

## Deterministic observations

Forty scripted observations cover:

- Atomic initial barrier replay before any resource job starts; ready then activates generation 1.
- Two delayed jobs. Human Height changes to raw bits `0x3FB33333` while its exact create handle/resource attempt remains unchanged. A held initial batch becomes stale; its failed acknowledgment retains both pending IDs. A latest batch reflects current state and acknowledges successfully.
- Human destroy/recreate while its old job remains charged and cancellation-ignoring. A replacement starts under the explicit three-job cap. The old success is Retired/Stale with no Resources and no Prepared prefix. Terminal and replacement human then publish exact current tickets.
- Human Height changes to `0x40000000`; terminal opaque state then invalidates a held acknowledgment. A newer batch coalesces both latest observations. A foreign batch with identical generation and mutation revision rejects on ownership and leaves pending work intact; the current batch clears it.
- Terminal-only destroy/recreate/reseed/finalize produces only one additional controlled load. Human create/state/incarnation/current publication stay unchanged. Terminal gets a new creation revision and attempt.
- A further human state change leaves pending work. A current terminal CREO-family mismatch clears all objects, pending batches and publication currentness. Close advances world generation from 1 to 2 and drains all jobs.

The four controlled requests have global attempts 1..4: human/terminal initial, human replacement, terminal replacement. Registry creation revisions are human 1 then 5 and terminal 2 then 7; finalized handle revisions are 3/4 then 6/8. These are deterministic modern policy observations, not wire counters. Scalar state revisions advance independently of those resource handles. Batches carry the latest current snapshot at drain time and stay pending until a current acknowledgment.

The independent model predicts every event's Result, State, Generation, OutstandingJobs, Objects, Calls, Publications and Pending. Objects include exact raw CreateHex, registry/creation revisions, optional scalar revision and all six scalar values/raw Height bits, plus opaque field hex. Calls retain completed/canceled metadata; publications retain exact object/handle/attempt and currentness. Pending batches include mutation revision, ordered/coalesced flags and current snapshots. GUID ownership is tested locally and omitted from deterministic output.

## Resource driver, failures and evidence

Two dedicated read-only original workers use the accepted router/options and pinned installed configuration. They return sealed human and terminal CPU resources with all archive scopes closed before deterministic scheduling. The exact same independently authored creates are used on identical recreate, allowing these immutable owned closures to supply later controlled tasks without extra original reads. Driver receipts assert create metadata and existing 39/27 source counts and 4,686,371/1,401,794 bytes. This is resource-backed execution through the accepted readers/validator, not a new source-byte, mip, parser or corpus re-audit.

Negative cases independently check an intact outer CREO delta containing a valid posture prefix followed by NaN Height during staged replay (no returned ready, jobs, objects or drainable batch); a simultaneous current success/current fault (no returned Prepared prefix or batch); and prior-generation held-batch rejection. Every world closes and observes late tasks, with zero final outstanding jobs and archive scopes. CPU resource ownership stays with the test driver; no GPU roots or consumer retirement implementation is claimed.

Observation: native receipts match all forty independent model events and resource-driver assertions. Confidence: Confirmed for this bounded replacement execution. Byte evidence is inherited from the [lifecycle contract](SCENE_OBJECT_LIFECYCLE_CODEC_CONTRACT.md), [scalar contract](CREATURE_VIEW3_SCALAR_DELTA_CONTRACT.md) and [mixed world contract](SCENE_MIXED_WORLD_CONTRACT.md). Acknowledgment, generation, incarnation, cancellation and failure rules are the accepted [modern change policy](SCENE_WORLD_CHANGE_DELIVERY_CONTRACT.md) and [initializer policy](SCENE_OBJECT_RESOURCE_INITIALIZER_CONTRACT.md). No new original semantics, TANO schema, function names, packet coverage or live interoperability are established.

## Reproduction and handoff

From the repository root with the installed cached runtimes:

```powershell
$python = 'C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$env:PYTHONUTF8 = '1'
$env:PYTHONDONTWRITEBYTECODE = '1'
$env:APPDATA = Join-Path $env:TEMP 'stardust-appdata-networking-r15'
$env:NUGET_PACKAGES = 'C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
New-Item -ItemType Directory -Force $env:APPDATA, local-private/round15 | Out-Null
'<configuration><packageSources><clear /></packageSources></configuration>' | Set-Content local-private/round15/NuGet.Config
& $python poc/round15-tests/Networking/peer_model.py --generate local-private/round15/peer.json
dotnet restore poc/round15-tests/Networking/Networking.csproj --configfile local-private/round15/NuGet.Config -p:NuGetAudit=false
dotnet run --no-restore --project poc/round15-tests/Networking/Networking.csproj -p:UseSharedCompilation=false -- C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg poc/preview-profiles/scene-object-catalog.json poc/preview-profiles/human-locomotion.json local-private/round15/peer.json local-private/round15/export.json
& $python poc/round15-tests/Networking/peer_model.py --peer local-private/round15/peer.json --verify local-private/round15/export.json
```

The configuration is checked against `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524` before resource loads. All generated peers, logs and exports remain ignored. Export also includes Delivered, Driver, OriginalJobs, ArchiveScopes, FinalOutstanding, Checks and Negatives. Lead-owned shared registration can call the six-argument entry point. A future authorized consumer test can replay the same observation sequence against the frozen presentation interface; it must separately validate each resource publication and latest state/batch before acknowledgment. Round15-N stops at this reusable proof.
