# Round17 rendering: native motion over accepted visuals

October 2, 2026. Bounded question: can an owned wrapper apply latest exact-incarnation
native pose and current creature Height without accumulating scale, changing
resource identity, or depending on an unfinished motion store? Stop after independent
proof and handoff.

## Inputs and orientation

Public snapshot2,272 files; manifest SHA256
`f7af15cbcd8ac9a219a03cf7ae21502c5c69b9fecca16ed3ef3a20eb1be6d832`;
ZIP SHA256 `c4b37e54c60d74c8b737a5918b97eca0d08dcc60e50ecc5c10929e258db5ae08`.
Authoritative snapshot HEAD `1fc55dd89cd23ce3af49e3fe471210bdebe9735c`; isolated
checkout HEAD `65de8c88141071744dc75594be126b3bab3696a5` remains separately preserved.
Complete preflight checked pinned manifest/archive, exact unique2272 member set,
every contained safe path/size/hash and no symlinks/private/Git members before
public writes. Targeted overlay preserved unlisted/private material and reverified
all outputs; no Git mutation.

Read AGENTS, START_HERE/latest Round16 report, status/ledger/unknown-function
terminology, create-placement/visual/movement contracts, prior rendering probes,
coordinate/Height helpers, frozen motion interface and accepted fixture/launcher
tools. No virtual addresses or original executable behavior were used.

Original root read-only `C:/Stardust-DEV`; `swgemu_live.cfg` SHA256
`22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`.
Existing catalog/human-locomotion bank and original human/terminal pinned sources.
Installed .NET8/Python/Godot4.7.2 Mono and existing private APPDATA/cache; no downloads.
Existing certificate-store startup diagnostic remains nonfatal.

## Findings and compatibility consequences

Observation: [wrapper](../../poc/Stardust.Godot/SceneWorldObjectMotionVisual.cs)
checks initial identity/motion/Height before owning inner, borrows world/source,
uses current resource/incarnation/state, converts native pose using accepted
Z reflection, and derives Height once from the unscaled pose. Null/stale/foreign
motion restores create placement without rewinding revision guard. Native rigid
and current Height failures retire owned inner; callback reentry is rejected.
Confidence: Confirmed modern presentation policy by source and original-data
execution. Consequence: lead wraps factory results through the frozen presentation
interface without copying stores, loading resources or editing old adapters.

Observation: source is an authored fake frozen-interface provider, not the
unfinished same-round store. Human native rotation rows `(0.8660254,-0.5,0)`,
`(0.5,0.8660254,0)`, `(0,0,1)` and translation `(-0.9,0.2,0.5)` become engine
origin `(-0.9,0.2,-0.5)` and Height1.4 basis. Terminal native Y rotation uses
0.70710677 coefficients and translation `(1.8,0.1,1)`, becoming engine Z=-1.
Five repeated applications preserve those poses. Confidence: Confirmed by
independently authored numeric expectations in the pixel/log oracle, not a
wire movement fixture or original client timing inference.

Observation: missing motion resets both poses; lower revision/foreign incarnation
cannot apply; revision11 after null reset restores human motion after accepted
revision10; terminal old-incarnation motion is ignored after recreation. Idle
pose fingerprint advances during Tick without locomotion selection. Create bytes,
publication/resource and GPU identities remain unchanged, with no reload from
motion/Height. Native receipts retain human39/4,686,371 and terminal27/1,401,794
read/byte closures; static recreation alone adds the third job.

Observation: invalid initial scale and represented near shear reject while leaving
passed inner alive; detached root, foreign world/worker constructor reject before ownership.
Source callback disposal reentry faults and cleans the owned inner. Invalid current
NaN pose retires only static; invalid current Height retires human. Five roots
allocated equal five disposed. Real two-archive cancellation publishes zero roots,
closes/drains both jobs; five launcher negatives launch nothing. Confidence:
Confirmed by headless/GPU positive, negative and cleanup gates.

Observation: pixel oracle records14,333 human pixels changed by Height; terminal
pixels identical through Height/recreation, and updated/recreated actor regions
identical. Motion changes21,158 human-region pixels and30,705 terminal-region
pixels. GPU/headless reflected poses agree and GPU identities stay fixed through
motion. Private motion image inspected. Confidence: Confirmed diagnostic rendering,
not original render fidelity or interpolation behavior.

Lead requested a near-shear probe, then clarified the existing normalized-dot guard
already supplies orthogonality checking; the wrapper retains that guard without
duplicating it. Unit length/proper determinant checks additionally forbid motion
scale/reflection. Proof build initially reused a local variable name; it was
renamed before successful gates. Shared baseline code was not changed.

## Reproduction

Using installed tools/offline caches and private output paths:

```powershell
python poc/round17-tests/Rendering/Generate-Peer.py <private-peer.json>
$env:APPDATA=(Resolve-Path local-private/round2/appdata).Path
dotnet build poc/Stardust.Godot/Stardust.Godot.csproj --no-restore -v minimal
dotnet run --project poc/round17-tests/Rendering/Rendering.csproj -- <root> <cfg> poc/preview-profiles/scene-object-catalog.json poc/preview-profiles/human-locomotion.json <peer> <private-native.json>
python poc/round17-tests/Rendering/verify.py <peer> <native-receipt>
& poc/round17-tests/Rendering/Launch.ps1 -GodotPath <installed-Godot> -ArchiveRoot <root> -Configuration <cfg> -FixturePath <peer> -Headless
& poc/round17-tests/Rendering/Launch.ps1 -GodotPath <installed-Godot> -ArchiveRoot <root> -Configuration <cfg> -FixturePath <peer> -CancelLoad
& poc/round17-tests/Rendering/Launch.ps1 -GodotPath <installed-Godot> -ArchiveRoot <root> -Configuration <cfg> -FixturePath <peer> -CapturePath <fresh-private.png>
python poc/round17-tests/Rendering/verify_preview.py <gpu-log> <headless-log> <cancel-log> <initial.png> <updated.png> <recreated.png> <motion.png>
& poc/round17-tests/Rendering/Verify-Launch.ps1 -GodotPath <installed-Godot> -ArchiveRoot <root> -Configuration <cfg> -FixturePath <peer> -ExistingCapture <initial.png>
python tools/python/validate_organized_workspace.py .
git -c core.safecrlf=false diff --check
```

Build zero warnings/errors; native receipt, headless/GPU, cancellation,
numeric/pixel/log oracle and launcher negatives pass. Validator:1,157 Markdown
files, zero broken/private links,2,602 provisional rows with zero duplicates,
345 packet rows, zero public files at least25MiB. Focused source/status inspection,
`git diff --check` and new-file whitespace checks pass. All2,272 baseline hashes
and sizes remain unchanged; exactly12 allowed additions, index empty and checkout
HEAD unchanged. Detailed receipts/logs/captures and scope receipt remain ignored.

Exact additions: [wrapper](../../poc/Stardust.Godot/SceneWorldObjectMotionVisual.cs),
[preview](../../poc/Stardust.Godot/SceneWorldObjectMotionVisualPreview.cs),
[contract](../../docs/SCENE_WORLD_OBJECT_MOTION_VISUAL_CONTRACT.md), this checkpoint,
and own Rendering files:
[Generate-Peer.py](../../poc/round17-tests/Rendering/Generate-Peer.py),
[Launch.ps1](../../poc/round17-tests/Rendering/Launch.ps1),
[NuGet.Config](../../poc/round17-tests/Rendering/NuGet.Config),
[Program.cs](../../poc/round17-tests/Rendering/Program.cs),
[Rendering.csproj](../../poc/round17-tests/Rendering/Rendering.csproj),
[Verify-Launch.ps1](../../poc/round17-tests/Rendering/Verify-Launch.ps1),
[verify.py](../../poc/round17-tests/Rendering/verify.py),
[verify_preview.py](../../poc/round17-tests/Rendering/verify_preview.py).

## Limits and next action

Only new wrapper, standalone preview, durable contract, this checkpoint and own
Rendering runner files. Frozen source/Core/factory/consumer/host/shared Main/project,
navigation/status/ledgers/registries unchanged. No original executable execution,
live contact, captures/fuzzing of a server, downloads, staging/commit/push/publication
or research-score changes. Original bytes are read-only; synthetic poses and opaque
TANO are explicitly diagnostic. Equal-revision changed-value guard is source policy;
this proof tests older/null/newer/foreign revisions, not a full motion-store audit.

Canonical [contract](../../docs/SCENE_WORLD_OBJECT_MOTION_VISUAL_CONTRACT.md) gives
ownership and provider monotonicity requirements. Next action: lead wraps accepted
factory output with `(world,inner,motionSource)`, disposes rejected inner candidates,
joins actual motion source into continuous pump, and runs combined original-resource
motion/cancellation acceptance. Locomotion switching, interpolation, authority
resolution and movement/interior integration remain later. Rendering worker stops.
