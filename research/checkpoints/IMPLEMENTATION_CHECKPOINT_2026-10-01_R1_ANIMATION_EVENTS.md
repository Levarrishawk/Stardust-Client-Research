# R1-RUNTIME-01 animation event cursor — 2026-10-01

## Question and stopping condition

Can the existing owned MSGS records feed a deterministic bounded engine-neutral event cursor with explicit modern fractional/loop/control policy? Stop at independent synthetic and pinned original tests, atomic guards, ownership and lead handoff. No original scheduler recovery, effects/audio delivery, root translation, Godot or animation graph change.

## Baseline, inputs and tools

Isolated checkout: C:/Users/ancie/.codex/worktrees/9175/Stardust-Client. HEAD: 65de8c88141071744dc75594be126b3bab3696a5. Existing modified sphere-floor checkpoint preserved. Read AGENTS, START_HERE, latest named development session, parallel rules, current status/ledger/unknown terminology, prior MSGS interval and locomotion checkpoints and existing decoder/collector/overlay before implementation. No new registry names or parser.

Read-only executable SHA256 recheck: E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150. Pinned effective configuration SHA256: 22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524. Walk/run exact hashes, priority30 and byte evidence are in the [existing locomotion contract](../../docs/COMPRESSED_LOCOMOTION_CONTRACT.md). Existing ConfiguredTreOverlay/CompressedKeyframeDecoder/CompressedLocomotion resolve and validate original assets. No original executable execution, server contact, staging, commit or publication.

## Observation, interpretation, confidence and consequence

Observation: existing MSGS groups preserve authored occurrence indices; integer collector uses [start,end). Walk has footstep7/23,end33; run6/17,end22. Confirmed existing boundary and original member identities. Consequence: preserve names/frame duplicates without deriving gameplay effects.

Observation: new cursor returns immutable cycle/group/occurrence identities and uses (previousPhase,targetPhase], including initial frame0 only on first positive progress. Exact loop boundary returns old terminal before new frame0. Interpretation: explicit MODERN scheduler policy. Confidence: Confirmed for tested replacement behavior; original scheduling semantics remain UNKNOWN. Consequence: adapters can consume ordered batches, but must not call this original-compatible dispatch semantics.

Observation: preflight binary counts reject invalid time/phase or cycle/event excess before mutation. Finite monotonic timestamps, bounded monotonic unwrapped phase, paused fixed phase, reset epoch and preserved old batches are tested. Confirmed tested atomicity/ownership. Consequence: callers may subdivide oversized advances without losing events; pause has no implicit catch-up.

## Verification

`dotnet run --project poc/round1-tests/Runtime/Runtime.csproj -- C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg` runs synthetic and original checks. In this sandbox use copied existing private package cache, local RestoreSources, NuGetAudit=false and UseSharedCompilation=false; no package/version changes. Initial default restore failed NU1301, subsequent audit lookup failed NU1900, shared compiler failed CS2012; offline source and process-local compilation resolved these environment failures. Cache remains ignored under local-private/round1; generated bin/obj remain ignored.

Passing output: 160 independent oracle/quarter-partition targets through four loops, repeated ticks/identity uniqueness, fractional boundaries, duplicate frames/names, terminal/frame0 ordering, nonloop end, pause/resume/no-progress, backward/NaN/infinite/equal-time-progress/phase ceiling, reset, empty messages, owned source/batches and atomic event/cycle rejection/retry. Both pinned original clips pass member hash/configuration/priority checks, three cycles/six occurrences, quarter partition equality and existing collector agreement. Original inputs lack terminal/frame0 events; endpoint policy is verified synthetically.

Core build and organized-workspace/diff checks are recorded in the final handoff. No execution-based original scheduler claim follows.

## Integration and remaining uncertainty

Allowed additions only: cursor, smoke scenario, isolated Runtime runner/project, [durable contract](../../docs/ANIMATION_EVENT_CURSOR_CONTRACT.md), this checkpoint. Lead should add `AnimationEventCursorScenarios.Run();` to common smoke runner, optionally expose `AnimationEventCursorScenarios.Original(root, configuration)`, and reconcile shared navigation/status/ledger as appropriate. Worker does not edit those lead-owned files.

Original scheduler, float-to-integer runtime conversion, graph/event consumers, effect/audio dispatch and adapter threading remain outside this milestone. Research/function/protocol measures unchanged. Suggested next milestone: a separately assigned diagnostic event-batch consumer with explicit clip-switch epoch ownership; do not begin it. Result awaits lead review/integration.

Final worker checks: Core build succeeded with 0 warnings/errors. Organized validator:1006 Markdown files,0 broken links,0 links into local-private,2602 provisional rows/0 duplicate addresses,345 packet rows. `git diff --check` passed (only a line-ending warning on the pre-existing sphere-floor file). No baseline file was changed by the worker; the six new text files are unstaged.

Exact successful environment commands (PowerShell):

```powershell
$env:APPDATA = Join-Path $env:TEMP 'stardust-runtime-r1-appdata'
dotnet run --project poc/round1-tests/Runtime/Runtime.csproj -p:RestorePackagesPath="$PWD/local-private/round1/nuget-packages" -p:RestoreSources="$PWD/local-private/round1/nuget-packages" -p:NuGetAudit=false -p:UseSharedCompilation=false -- C:/Stardust-DEV C:/Stardust-DEV/swgemu_live.cfg
dotnet build poc/Stardust.Core/Stardust.Core.csproj --no-restore -p:UseSharedCompilation=false
$env:PYTHONUTF8 = '1'
& C:/Users/ancie/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe tools/python/validate_organized_workspace.py .
git -c safe.directory=C:/Users/ancie/.codex/worktrees/9175/Stardust-Client diff --check
```
