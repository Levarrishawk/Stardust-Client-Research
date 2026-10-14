# R18-R local motion driver

October 2, 2026. Bounded question: can the accepted integer clock/input timeline drive native planar poses for one current typed incarnation without losing authoritative corrections, replaying paused/stalled input, scaling Height or leaking borrowed ownership? Done when frozen driver API, independent native tick/correction/lifecycle tests and exact repository gates pass; stop after handoff.

## Identity and orientation

Round18 full overlay verified: 2,310 public files; manifest SHA-256 `023b090af15474a13a76ba1836d59cfc3800baab6dc27057436a165608e8a910`; ZIP `e2fe55b4f1a8c4c21da802d28dc922930f8963d002ee38b5c9f71129145fc43d`. Snapshot HEAD `1fc55dd89cd23ce3af49e3fe471210bdebe9735c`; isolated checkout HEAD separately preserved `65de8c88141071744dc75594be126b3bab3696a5`. All archive members unique/exact manifest set, all contained safe relative/no-symlink paths/sizes/hashes preflighted before writes; all listed output hashes verified after overlay. Unlisted/private/HEAD/index preserved. No main/shared/Git publication/mutation.

Read AGENTS, START_HERE/[accepted Round17](../sessions/PARALLEL_DEVELOPMENT_ROUND_17_2026-10-02.md), status/ledger/UNKNOWN, motion/fixed-step/input-timeline/pump contracts and prior timeline/motion/integration probes, frozen driver/source interfaces, shared generic clock/timeline, current store/coordinator and accepted fixture/tool APIs/curated movement boundary rows. No new executable addresses, wire parsing or static research claims. Existing Python/.NET8/offline cache only; task APPDATA/sources-cleared NuGet/no shared compiler. Profile JSON and independently authored synthetic fixture bytes suffice; no original assets/config/executable/TRE are read. No original execution/live contact/download/stage/commit/push/publication.

## Evidence, interpretation and consequences

Observation: [driver](../../../SWG-Godot-Client/poc/Stardust.Core/Session/SceneWorldLocalMotionDriver.cs) borrows one world/store, binds one current finalized typed incarnation, creates private FixedStepActorHost/Pose and PortalActorInputTimeline, publishes a starting local pose and remembers exact last local publication. No replacement clock/input queue is invented. Finite planar input/tick-start selection and native Y-yaw/position/speed are bounded by accepted shared policy. Interpretation: modern free movement, not original floor/collision/prediction/interpolation. Confidence: Confirmed native three-partition comparison against independent tick-indexed double trajectory (float tolerance) plus bit-identical partition results. Consequence: use one local driver per object, keep Height/resources separate, and provide explicit timestamped input.

Observation: a new current authoritative motion snaps owned pose/speed before advancement while retaining clock/fraction/input; no-tick/suspended advances expose correction without local overwrite. Next fixed tick publishes from correction. Common-boundary correction across three partitions and paused correction/resume pass. Interpretation: immediate modern correction without smoothing or event backdating. Confidence: Confirmed native tests. Consequence: correction observation time is caller responsibility; the frozen motion record has no server timestamp, so different observation schedules are not retrospectively made equivalent.

Observation: stall caps drop whole backlog, pause/resume suspends pending time and consumes history to latest input. Admission negatives preserve healthy pose/clock/publication; runtime source-capacity failure latches and stops ticks until explicit reset. Reset publishes immutable create placement, clears only private clock/input/fault and advances source global revision. Dispose preserves world/store/unrelated motion rows. Old incarnation never reattaches to destroy/recreate/generation replacement. Interpretation: bounded borrowed lifecycle and explicit recovery. Confidence: Confirmed native state/fault/reset/ownership/affinity tests; callback-free self-reentry guard is source-verified, not forged execution. Consequence: new incarnations need a new driver; successful earlier ticks/engine side effects are not rolled back.

## Reproduction and validation

Scenario entry: `SceneWorldLocalMotionScenarios.Run(IReadOnlyDictionary<string,string> peer, string exportPath)`. [Own runner](../../../SWG-Godot-Client/poc/round18-tests/Runtime/Runtime.csproj) takes peer/export from repository root; accepted catalog/motion-bank JSON metadata are read from poc/preview-profiles. Shared harness registration remains lead-owned. Accepted Round14 independent producer supplies synthetic lifecycle/CREO/TANO fixture; driver adds no network codec or asset reader.

```powershell
$env:PYTHONUTF8='1'
$env:APPDATA=Join-Path $env:TEMP 'stardust-runtime-r18-appdata'
$env:NUGET_PACKAGES='C:/Users/ancie/Documents/GitHub/Stardust-Client/local-private/nuget-packages'
New-Item -ItemType Directory -Force $env:APPDATA,local-private/round18 | Out-Null
python poc/round14-tests/Runtime/Generate-Peer.py local-private/round18/peer.json
dotnet restore poc/round18-tests/Runtime/Runtime.csproj --configfile poc/round18-tests/Runtime/NuGet.Config -p:NuGetAudit=false
dotnet build poc/round18-tests/Runtime/Runtime.csproj --no-restore -p:UseAppHost=false -p:UseSharedCompilation=false -p:NuGetAudit=false
dotnet poc/round18-tests/Runtime/bin/Debug/net8.0/Runtime.dll local-private/round18/peer.json local-private/round18/export.json
python tools/python/validate_organized_workspace.py .
git diff --check
```

Native driver PASS:90 checks, three regular/jitter partitions and three common-correction partitions; zero-warning build; zero resource loads/asset reads. Tests cover fractional/equal-time transitions, native yaw/hold/speed changes, immutable raw create, Height/resource independence, fractional-clock correction, suspension correction/no replay, bounded stall, input admission/order/late/horizon/capacity/time negatives, source capacity sticky fault/reset, create/reset pose policy, generation/recreation/failure/close, mismatch/static/missing/invalid constructor and owner affinity, borrowed-row/world disposal. R17 motion-store regression passes 280 checks; shared native default suite passes with a zero-warning build. Final workspace/link/encoding/whitespace/scope/HEAD/index results accompany handoff. No required private input/tool was absent. GPU, human input, physics/interior host, original prediction/smoothing, movement ingress and live execution are unrun/outside this worker scope.

Durable [contract](../../docs/SCENE_WORLD_LOCAL_MOTION_CONTRACT.md) defines constructor publication, paused speed, exact counters/timestamps, error types/lifetime, correction observation requirements and modern limits.

## Exact scope and lead join

Seven additions only: core SceneWorldLocalMotionDriver.cs; SmokeTests SceneWorldLocalMotionScenarios.cs; docs/SCENE_WORLD_LOCAL_MOTION_CONTRACT.md; this checkpoint; own poc/round18-tests/Runtime Program.cs/Runtime.csproj/NuGet.Config. Frozen interfaces/source/store/clock/timeline/pump receiver/coordinator/shared Main/Program/projects/navigation/ledgers/registries are unchanged. All logs/fixture/exports/diffs/hashes remain ignored.

Lead registers Run(peer,export), constructs driver after current typed finalization and source creation, submits bounded native input timestamps, advances driver on one owner thread, then lets the existing motion-aware presentation sample the source. Pass active/focus boundaries explicitly; dispose driver separately from borrowed pump/world/store. Unified movement receiver and locomotion/interior rendering remain independently reviewed integration work, without unfinished same-round dependency. Suggested next probe: joined input/local-correction/native presentation proof under continuous host, followed by bounded floor/interior binding. Worker stops after Round18; no next milestone/cross-chat message.
