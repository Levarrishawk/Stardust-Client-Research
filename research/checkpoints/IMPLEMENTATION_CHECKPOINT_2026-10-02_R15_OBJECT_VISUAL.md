# Round15 rendering: generic recipe factory

October 2, 2026. Bounded question: can one Godot factory turn accepted generic
publications into creature/static presentations while preserving independent
resource-attempt and typed-state freshness? Stop after standalone proof and
handoff; no Round16 work.

## Inputs and identity

Required orientation: START_HERE, latest Round14 report, Round15 preparation,
mixed-world/static visual contracts, prior rendering checkpoints, current status,
ledger and unknown-function terminology. Existing visual/resource/fixture tools
were inspected and reused. No virtual addresses were used, so no executable
identity claim is made.

Authoritative dirty-tree snapshot: 2,197 public members; baseline manifest SHA256
`1736485311ac5622ceaed02758295293b307ccc52d09c9e982d8b8440b86be1f`;
ZIP `920d8655531c68c524d42bd9db2b28a35a91c8b9fa7462328282a7c00ba2835b`.
Snapshot HEAD is `1fc55dd89cd23ce3af49e3fe471210bdebe9735c`, explicitly corrected
by lead. The isolated checkout retains older HEAD
`65de8c88141071744dc75594be126b3bab3696a5`; public baseline bytes are authoritative.
Overlay verified every unique safe path/size/hash before copying, rejected
symlinks and private/Git members, copied only listed paths, and reverified all
2,197 outputs. Unlisted ignored material was preserved.

Original read-only archive root `C:/Stardust-DEV`; configuration `swgemu_live.cfg`
SHA256 `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`.
Installed .NET8, Python and Godot4.7.2 Mono, existing offline caches; no downloads.
Default NuGet settings were inaccessible; existing private APPDATA resolved the
build. Nonfatal Godot certificate-store diagnostic persists.

## Findings, evidence and consequences

Confirmed modern behavior in
[factory/adapter](../../poc/Stardust.Godot/SceneWorldObjectVisualFactory.cs): exact
publication/current finalized metadata and independent typed scalar freshness;
recipe selection without fixed IDs; public `SceneWorldObjectVisual` getters;
main-thread/live-parent checks; terminal inspection opt-in before allocation;
postattachment guard and rollback; current Height/visibility application without
reloading; stale retirement; idempotent disposal. The
[durable contract](../../docs/SCENE_WORLD_OBJECT_VISUAL_CONTRACT.md) specifies
failure behavior and borrowed CPU versus owned GPU lifetime.

Observation: native source receipts retain original human 39 reads/4,686,371
bytes and terminal 27 reads/1,401,794 bytes, each read once per request. Static
recreation performs exactly one additional job, preserving human incarnation.
Confidence: Confirmed by the original-data native runner and independent receipt
oracle. Consequence: adapters consume existing completions without another loader.

Observation: standalone Godot proof rejects cross-object/recipe and altered
placement without allocating, rejects detached parent and worker thread, rejects
default terminal policy without allocating, and injects a Height change via
`ChildEnteredTree` to prove staged-state rollback. Stale typed and opaque
observations are rejected; latest state applies with identical resource/GPU IDs.
Static destruction retires only static; recreation changes its GPU IDs and
preserves human. Finite Height 0.1 is retained by the typed codec but rejected by
modern presentation bounds, disposing only human. Family disagreement fails the
world and retires the remaining visual. Four allocated roots equal four disposed
roots, including the rolled-back candidate. Confidence: Confirmed by headless
and GPU execution. Consequence: no consumer must silently retain a failed actor.

Observation: independent fixed-camera oracle reports 14,333 changed human pixels;
terminal pixels identical through Height update and static recreation; updated
and recreated actor regions identical; GPU/headless state and poses agree.
Confidence: Confirmed diagnostic output, not proof of original rendering fidelity.
Cancellation enters both archives, publishes zero actors, closes both jobs and
drains outstanding work. Five invalid launcher cases create no logs/captures.

The initial no-allocation policy test exposed deferred rejection in the old
static construction path; the new factory now explicitly rejects switch-texture
content before allocation. Initial invalid-Height proof used NaN, which the
accepted codec rejects; the proof was corrected to finite out-of-policy 0.1.
Existing baseline files were not changed to resolve either issue.

## Reproduction

From the repository, using the installed Python path and existing private
APPDATA/NuGet cache:

```powershell
python poc/round15-tests/Rendering/Generate-Peer.py <private-peer.json>
dotnet build poc/Stardust.Godot/Stardust.Godot.csproj --no-restore
dotnet run --project poc/round15-tests/Rendering/Rendering.csproj -- <root> <cfg> poc/preview-profiles/scene-object-catalog.json poc/preview-profiles/human-locomotion.json <peer> <private-native.json>
python poc/round15-tests/Rendering/verify.py <peer> <native-receipt>
& poc/round15-tests/Rendering/Launch.ps1 -GodotPath <installed-godot> -ArchiveRoot <root> -Configuration <cfg> -FixturePath <peer> -Headless
& poc/round15-tests/Rendering/Launch.ps1 -GodotPath <installed-godot> -ArchiveRoot <root> -Configuration <cfg> -FixturePath <peer> -CancelLoad
& poc/round15-tests/Rendering/Launch.ps1 -GodotPath <installed-godot> -ArchiveRoot <root> -Configuration <cfg> -FixturePath <peer> -CapturePath <fresh-private.png>
python poc/round15-tests/Rendering/verify_preview.py <gpu-log> <headless-log> <cancel-log> <initial.png> <updated.png> <recreated.png>
& poc/round15-tests/Rendering/Verify-Launch.ps1 -GodotPath <installed-godot> -ArchiveRoot <root> -Configuration <cfg> -FixturePath <peer> -ExistingCapture <initial.png>
python tools/python/validate_organized_workspace.py .
git -c core.safecrlf=false diff --check
```

Build passes with zero warnings/errors. Native source/fixture receipt, headless,
GPU, real cancellation, pixel/log oracle and launcher negatives pass. Repository
validator: 1,136 Markdown files, zero broken/private links, 2,602 provisional rows
with zero duplicate addresses, 345 packet rows, zero public files at least 25MiB.
`git diff --check` and new-file whitespace checks pass. Exact scope audit confirms
all 2,197 baseline hashes/sizes unchanged and exactly 12 allowed additions. Git
status was inspected; inherited dirty files preserved, isolated HEAD unchanged,
index empty. Detailed logs, native export, PNGs and scope receipt remain ignored.

## Scope and next probe

Only new factory, preview, durable contract, this checkpoint and own
`poc/round15-tests/Rendering/` runner files are authorized. No old visual,
consumer, Main, shared project/Core/interface, status, ledger, registry or
navigation edits. No Git staging/commit/push/publication, original execution,
live contact, packet capture/fuzzing or research-score changes.

Unresolved: combined consumer capacity/acknowledgment retry and integrated mixed
acceptance are lead work. Other recipes/modes, original effect timing, terrain,
parent transforms, face/gameplay and live interoperability are not established.
Exact next action: lead wires `factory.Create` through the frozen presentation
interface, reviews rollback/invalid-current-state policy, and runs combined
acceptance. Rendering worker stops after this Round15 handoff.
