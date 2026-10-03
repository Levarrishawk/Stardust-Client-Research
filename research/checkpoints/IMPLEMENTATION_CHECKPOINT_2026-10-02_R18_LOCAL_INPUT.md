# Round18 rendering: local input and selected locomotion

October 2, 2026. Question: can timestamped engine input target a frozen local driver,
and loaded locomotion clips target accepted motion visuals without competing
loops, replay, reload or an unfinished same-round implementation? Stop at handoff.

## Identity and orientation

Verified snapshot2,310 files; manifest SHA256
`023b090af15474a13a76ba1836d59cfc3800baab6dc27057436a165608e8a910`;
ZIP `e2fe55b4f1a8c4c21da802d28dc922930f8963d002ee38b5c9f71129145fc43d`.
Snapshot HEAD `1fc55dd89cd23ce3af49e3fe471210bdebe9735c`; worker checkout HEAD
`65de8c88141071744dc75594be126b3bab3696a5` preserved separately. Pinned identities,
exact unique member set, contained safe paths/no symlinks/private/Git members and
all sizes/hashes checked before public writes; targeted overlay reverified2310
outputs and preserved unlisted/private/HEAD/index.

Read AGENTS, START/latest accepted17, status/ledger/unknown-function terminology,
motion/placement/view-input/movement contracts, prior checkpoints and accepted
mapping, orientation, clock, visual/transition and fixture/launcher tools.
Frozen local driver/motion source/pump receiver contracts remain read-only. No VA
or original executable claim. Installed .NET8/Python/Godot4.7.2 Mono and offline
caches/private APPDATA only. Original root `C:/Stardust-DEV` read-only; configuration
SHA256 `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`.
Existing public catalog/bank and pinned original human/terminal. Existing Godot
certificate-store diagnostic remains nonfatal.

## Observations, confidence and consequence

Observation: [input host](../../poc/Stardust.Godot/SceneWorldLocalInputHost.cs)
owns accepted driver once; real dispatched W/D/Shift map normalized native walking2
and run6. Echo/unsupported keys ignored, focus loss and tree pause release, resume
does not replay, heading maps through accepted orientation, reset clears keys,
duplicate inactive transitions are suppressed, wrong-thread/IDisposable/backward
time and detached configuration preserve/close ownership as appropriate.
Confidence: Confirmed against injected-clock fake-driver receipts in actual Godot
dispatch. Consequence: lead can call Advance before pump Step without another loop.
Actual fixed-step/drop/suspension internals remain delegated to the real driver.

Observation: [locomotion wrapper](../../poc/Stardust.Godot/SceneWorldLocomotionVisual.cs)
owns a current creature motion wrapper after threshold/clip/source checks; Speed
chooses idle/walk/run with threshold4. Original loaded clock parameters produce
walk/run frame3 after0.1 seconds at rate30/proxy1/end33/22, confirmed independently
by Python arithmetic. Focus/playing freeze; reset changes clock and displayed pose;
missing motion returns idle. Invalid current NaN Speed retires wrapper/inner;
constructor threshold failure leaves passed ownership alive. Confidence: Confirmed
modern policy. Consequence: phase advances once while native pose/Height and source
identity remain unchanged. Static uses the existing rigid wrapper separately.

Observation: initial SetMovement(...,0) proof advanced numeric clock but stalled
the retained crossfade, causing reset's displayed-pose assertion to fail. Corrected
new wrapper passes0.25 presentation seconds to complete accepted0.15 crossfade,
without advancing animation phase. This is explicitly instant diagnostic selection,
not an original interpolation/timing claim. Proof also corrected notification
integer casts and a Node.Owner helper-name warning before zero-warning gates.

Observation: original native receipts human39 reads/4,686,371 bytes, terminal27/
1,401,794 with exactly one terminal reload. Motion/Height/speed/phase changes cause
no source reload or resource/GPU ID change. Five roots allocated/disposed including
ownership/staged rollback probes. Real archive cancellation closes/drains both jobs
with zero publications; five launcher negatives launch nothing. Native pose,
Height1.4, missing/reset/newer/foreign/recreation and near-shear cases remain tested.

Pixel/log oracle: Height changes14,333 human-region pixels; terminal unchanged
through Height/recreation; updated/recreated actor regions identical. Motion plus
selected walk pose changes21,124 human and30,705 terminal pixels. GPU/headless
poses/identities agree; private GPU image inspected. Confidence: Confirmed diagnostic
rendering, not original fidelity, original root-motion transfer or server behavior.

## Reproduction

```powershell
python poc/round18-tests/Rendering/Generate-Peer.py <private-peer.json>
$env:APPDATA=(Resolve-Path local-private/round2/appdata).Path
dotnet build poc/Stardust.Godot/Stardust.Godot.csproj --no-restore -v minimal
dotnet run --project poc/round18-tests/Rendering/Rendering.csproj -- <root> <cfg> poc/preview-profiles/scene-object-catalog.json poc/preview-profiles/human-locomotion.json <peer> <native-private.json>
python poc/round18-tests/Rendering/verify.py <peer> <native-receipt>
& poc/round18-tests/Rendering/Launch.ps1 -GodotPath <installed-Godot> -ArchiveRoot <root> -Configuration <cfg> -FixturePath <peer> -Headless
& poc/round18-tests/Rendering/Launch.ps1 -GodotPath <installed-Godot> -ArchiveRoot <root> -Configuration <cfg> -FixturePath <peer> -CancelLoad
& poc/round18-tests/Rendering/Launch.ps1 -GodotPath <installed-Godot> -ArchiveRoot <root> -Configuration <cfg> -FixturePath <peer> -CapturePath <fresh-private.png>
python poc/round18-tests/Rendering/verify_preview.py <gpu-log> <headless-log> <cancel-log> <initial.png> <updated.png> <recreated.png> <motion.png>
& poc/round18-tests/Rendering/Verify-Launch.ps1 -GodotPath <installed-Godot> -ArchiveRoot <root> -Configuration <cfg> -FixturePath <peer> -ExistingCapture <initial.png>
python tools/python/validate_organized_workspace.py .
git -c core.safecrlf=false diff --check
```

Build zero warnings/errors; native independent receipts, real event headless/GPU,
archive cancellation, numeric/pixel/log oracle and launcher negatives pass.
Validator:1,167 Markdown files, zero broken/private links,2,602 provisional rows
with zero duplicates,345 packet rows and zero public files at least25MiB. Focused
source/status inspection, `git diff --check` and new-file whitespace checks pass.
All2,310 baseline hashes/sizes unchanged; exactly13 allowed additions; preserved
checkout HEAD and empty index. Detailed outputs and scope receipt stay ignored/private.

Exact additions: [host](../../poc/Stardust.Godot/SceneWorldLocalInputHost.cs),
[locomotion](../../poc/Stardust.Godot/SceneWorldLocomotionVisual.cs),
[preview](../../poc/Stardust.Godot/SceneWorldLocalInputPreview.cs),
[contract](../../docs/SCENE_WORLD_LOCAL_INPUT_CONTRACT.md), this checkpoint;
own Rendering [Generate-Peer.py](../../poc/round18-tests/Rendering/Generate-Peer.py),
[Launch.ps1](../../poc/round18-tests/Rendering/Launch.ps1),
[NuGet.Config](../../poc/round18-tests/Rendering/NuGet.Config),
[Program.cs](../../poc/round18-tests/Rendering/Program.cs),
[Rendering.csproj](../../poc/round18-tests/Rendering/Rendering.csproj),
[Verify-Launch.ps1](../../poc/round18-tests/Rendering/Verify-Launch.ps1),
[verify.py](../../poc/round18-tests/Rendering/verify.py),
[verify_preview.py](../../poc/round18-tests/Rendering/verify_preview.py).

## Limits and next action

No actual unfinished driver/store/router, shared source/project/Main/pump/frozen
interface/navigation/ledger/status/registry edits. No original EXE, live contact,
server packet capture/fuzzing, downloads, staging/commit/push/publication or score
changes. Keyboard/focus/pause injection is real Godot dispatch; physical OS input
or mouse capture is not manually tested. Known material gaps are unchanged.

Canonical [contract](../../docs/SCENE_WORLD_LOCAL_INPUT_CONTRACT.md) specifies
ownership/failure/time/clip policy. Lead next connects actual local driver/router
to Configure/Advance before presentation Step, wraps current creature motion
visual with locomotion, and runs combined original-resource continuous input,
authority/correction, cancellation and lifecycle acceptance. Fixed-step physics,
interior binding, original root motion/interpolation remain deferred. Worker stops.
