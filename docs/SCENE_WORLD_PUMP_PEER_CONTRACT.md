# Per-frame ingress and presentation pump peer

Round16-N provides exact baseline lifecycle observations plus compiled assertions for an injected pump. The worker implements no pump or queue substitute. Baseline mode explicitly applies the authored Ingress list to the accepted coordinator and probes the accepted consumer directly; those observations do not establish pump timing or scheduling behavior.

## Entry and ownership

```csharp
SceneWorldPumpPeerScenarios.Run(
    string[] originalInputs, string peerPath, string exportPath,
    Func<SceneWorldCoordinator,
         Func<ScenePreparedObjectPublication, SceneWorldObjectSnapshot,
              ISceneWorldObjectPresentation?>,
         SceneWorldPresentationPumpPolicy,
         ISceneWorldPresentationPump>? pumpFactory);
```

`originalInputs` contains exactly root/configuration/catalog/animation-bank paths. Null factory runs standalone baseline proof and exports `ActualPumpRan=false`. The factory receives the already active coordinator, an adapter-creation delegate and the authored policy; **the pump constructs and owns its consumer**. Injected assertions use `pump.Consumer` exclusively. The baseline consumer is lazy and never constructed in injected contexts. The test adapter exposes the exact publication, records applied snapshots and verifies exactly-once disposal; it owns no engine roots.

Lead integration can call:

```csharp
SceneWorldPumpPeerScenarios.Run(inputs, peerPath, exportPath,
    (world, factory, policy) => new SceneWorldPresentationPump(world, factory, policy));
```

The concrete type above is supplied by lead integration, not referenced by the worker scenario. Only the frozen [pump interface](../../SWG-Godot-Client/poc/Stardust.Core/Session/ISceneWorldPresentationPump.cs) and accepted baseline APIs are compile dependencies. The [standalone runner](../../SWG-Godot-Client/poc/round16-tests/Networking/Networking.csproj) takes six paths and passes null. The [producer/model verifier](../../SWG-Godot-Client/poc/round16-tests/Networking/peer_model.py) reuses prior independently authored wire/model helpers, never production encoders or C# source. `--require-pump` refuses a baseline-only export.

## Fixture and obligations

Version-1 JSON contains Applications (whole APPLICATION hex), Initial, eight Frames, Policy and readable Obligations. A Frame has Name, Queue (messages arriving before this step), Ingress (expected ordered baseline application boundary), Complete (controlled job indices), AllowAdapters and Failure. Queue is test input, not an implemented worker queue. Initial seven-message start/human/terminal create/baseline/end sequence is atomically replayed through the accepted ready barrier before a pump is constructed. IDs, raw create bits and catalogue CRCs come from the independent [Round15 peer](SCENE_MIXED_PRESENTATION_PEER_CONTRACT.md). TANO fields remain SYNTHETIC OPAQUE bytes with no schema claim.

| Frame | Arrivals / baseline ingress | Completion and obligation |
| --- | --- | --- |
| start-delayed | stale-generation invalid byte, human Height A | At most two ingress items; stale delivery drops before decode; initial two jobs start |
| held-publication | human Height B | Human success completes; adapter creation deferred; publication remains pending |
| terminal-burst | destroy/create/baseline/end terminal; only first two ingress | Burst spans steps; current held human survives terminal-only recreate |
| stale-terminal-completion | remaining terminal baseline/end | Old terminal success retires; revised terminal job starts |
| retry-latest-state | human Height A | New terminal success; adapters enabled; human retry receives latest state |
| settle | none | Both current recipes eventually attach under one publication per step |
| late-failure-burst | Height B, terminal opaque delta, bad terminal family; first two ingress | Pending final invalid message remains ordered behind valid updates |
| family-failure | remaining bad family | No surviving objects, actors, queued work, pending publications or weight |

Height A/B bits are `0x3FB33333`/`0x40000000`. Registry/state revisions and exact raw creates are independently predicted by the baseline model. Human resource handle/attempt stays current through terminal recreation and state-only updates; final family failure invalidates that held resource. Cancelled old terminal success never publishes Resources.

Policy: six queued messages, 1,024 queued bytes, 256 bytes per message, two ingress items per step, two pending publications, one publication per step, four publication attempts, one change attempt per step, two actors and 67,108,864 pending resource weight. These are authored modern bounds, not recovered legacy timing. Failed Step may throw or return inactive; if it throws, PumpFrames records StepReturned=false and null counters, without inventing returned pump statistics.

Additional injected-only obligations check:

- Count-full, byte-full and oversized-message rejection, with unchanged caller bytes, existing queue count/bytes and world snapshots. Accepted caller buffers are cleared after enqueue to test ownership.
- Two simultaneous current completions under a one-publication pending cap reject before any adapter allocation or visible Prepared/actor prefix.
- Both deferred publications are held, then terminal destroy/recreate retires its old held publication before a new completion. Only the new terminal attempt may attach.
- Close with two unfinished jobs cancels them, keeps DrainCompletion incomplete until late success/fault observation, then reports no jobs or actors. All fake adapters retire exactly once.

No broad transport campaign is duplicated; stale/ordered APPLICATION delivery is the scheduling boundary here. Queue and publication burst rejection are acceptance obligations awaiting the concrete injected implementation.

## Verified and pending evidence

Worker native baseline proof and independent oracle agree on all eight frames' AcceptedMessages/DroppedMessages, State, Generation, exact Objects/raw scalar/opaque bytes, OutstandingJobs and Prepared/Retired tickets. Direct accepted-consumer deferral/retry uses the current snapshot; terminal recreation preserves human state; stale completion and final failure/close/drain pass. Two read-only original workers obtain genuine sealed resource closures through the accepted router, with the pinned installed configuration and all archives closed before controlled scheduling. No original source-byte/mip re-audit is claimed.

Confidence: Confirmed bounded baseline execution, with inherited byte evidence from [mixed world](SCENE_MIXED_WORLD_CONTRACT.md) and [presentation consumer](SCENE_WORLD_OBJECT_PRESENTATION_CONTRACT.md) contracts. **Actual-pump assertions were compiled but not executed by the worker.** Their execution and scheduling compatibility await lead injection. There is no new protocol/original scheduling/schema/name/coverage or live-interoperability claim.

## Reproduction and export

```powershell
$python = 'C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$env:PYTHONUTF8 = '1'
$env:PYTHONDONTWRITEBYTECODE = '1'
$env:APPDATA = Join-Path $env:TEMP 'stardust-appdata-networking-r16'
$env:NUGET_PACKAGES = 'C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
New-Item -ItemType Directory -Force $env:APPDATA, local-private/round16 | Out-Null
'<configuration><packageSources><clear /></packageSources></configuration>' | Set-Content local-private/round16/NuGet.Config
& $python poc/round16-tests/Networking/peer_model.py --generate local-private/round16/peer.json
dotnet restore poc/round16-tests/Networking/Networking.csproj --configfile local-private/round16/NuGet.Config -p:NuGetAudit=false
dotnet run --no-restore --project poc/round16-tests/Networking/Networking.csproj -p:UseSharedCompilation=false -- C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg poc/preview-profiles/scene-object-catalog.json poc/preview-profiles/human-locomotion.json local-private/round16/peer.json local-private/round16/export.json
& $python poc/round16-tests/Networking/peer_model.py --peer local-private/round16/peer.json --verify local-private/round16/export.json
# After lead's injected run, require execution rather than accepting the baseline-only receipt:
& $python poc/round16-tests/Networking/peer_model.py --peer local-private/round16/peer.json --verify local-private/round16/injected-export.json --require-pump
```

Configuration pin: `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`. UTF-8/LF peer hash: `6C117B229C74EC42922E4768D05D1ED059B0AC00D0CD57F45485922FE043EB2D`. Generated files/logs remain ignored. Export keys are BaselineFrames, WireIdentities, BaselineChecks, OriginalJobs, ArchiveScopes, ActualPumpRan, PumpFrames and PumpChecks. Null PumpChecks and false ActualPumpRan mean no actual-pump verification. Next action is the lead's injected run against the concrete pump, followed by the same oracle with `--require-pump`.
