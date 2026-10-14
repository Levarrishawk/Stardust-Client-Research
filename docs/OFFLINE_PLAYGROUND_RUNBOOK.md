# Configured offline playground

This is a bounded offline preview using the original supplied resource chain, modern terrain/movement policy, and synthetic protocol peers. It is not a live client login or a standalone bootstrap. [Round81 evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-06_R81_FULL_PLAYGROUND_RESTART.md) records full persistent restart and controlled-close boundaries; the existing single-run entry remains available.

## Prepare the profile

Use the accepted Version12 profile and original-resource/peer context already used by the launcher (external historical reference). The producer (external historical reference) writes a fresh Version13 profile into an ignored output directory. It does not extract assets or contact a server.

```powershell
python poc/round77-tests/Integration/create_configured_profile.py --source $SourceProfile --output $OutputProfile
$HostArguments.TerrainPointTargetProfile = $OutputProfile
& poc/round16-tests/Integration/Launch.ps1 @HostArguments
```

Here `$SourceProfile` is the accepted Version12 profile, `$OutputProfile` is a new ignored path, and `$HostArguments` is the existing accepted host argument dictionary, including prototype host path, original archive/configuration, catalog, synthetic peer partitions and customization context. Those inputs must be supplied; the profile does not replace them. Use the existing cached prototype host/.NET build setup described in the PoC README (external historical reference).

Default settings are below. Supply their UTF8 JSON with `--settings $SettingsJson` to change the bounded modern policy. All eight names are mandatory; duplicates, unknown names, malformed UTF8, coercion, nesting and oversized input are rejected. See the [settings contract](TERRAIN_OFFLINE_PLAYGROUND_SETTINGS_CONTRACT.md).

```json
{
  "Version": 1,
  "MaximumUpdates": 6000,
  "MaximumClickAttempts": 128,
  "StepMicroseconds": 10000,
  "WalkSpeed": 3,
  "RunSpeed": 4,
  "ArrivalRadius": 0.001,
  "HoldMissingDestination": true
}
```

Updates range1..6000 and clicks1..128. Each update is10ms; the default budget is60 simulation seconds. Paused updates consume budget. Click attempts include misses. Click exhaustion declines further clicks while movement controls remain available. Run speed must be at least walk speed; both are finite and at most10. The guard holds movement when the candidate endpoint has no supplied ground. It does not provide swept collision, sliding or pathfinding.

## Controls and outcome

| Control | Behavior |
| --- | --- |
| WASD | Camera-relative movement |
| Shift | Configured run speed |
| Left-click | Terrain target |
| Right-drag / wheel | Orbit / zoom |
| P | Pause or resume |
| Escape | Request finish; next ordinary tick clears movement and target |
| Focus loss | Clear held keys and pause; focus gain restores prior pause with empty keys |

Normal completion records a primitive budget/Escape outcome and shows the outcome panel during the existing final capture/exit sequence. It is not a persistent post-session screen. The root report records FIFO and transport buffers independently of movement completion. A fault can have empty transport buffers while movement remains faulted, without a successful final zero tick. Failure reports preserve completed/attempted counts and the accepted prefix; they do not claim rollback. Full-host fault capture is verified in logs; a visible full-host failure image is not established.

The default profile starts idle with real measured frame timing and no scripted correction or input. `--replay` selects the existing declared6000-update/32-click scripted proof; `--synthetic` additionally supplies bounded frame times. The128-click maximum has a separate independently checked native acceptance fixture. Original input, terrain physics, camera, runtime scheduling and live-server parity remain unproved. No socket is created by these offline helpers.

## Reusable restart slice: Round80

[Movement restart owner](TERRAIN_OFFLINE_SESSION_RESTART_OWNER_CONTRACT.md), [root restart owner](OFFLINE_SOE_ROOT_MOTION_RESTART_OWNER_CONTRACT.md) and [persistent display](TERRAIN_OFFLINE_RESTART_HUD_CONTRACT.md) now have an independently verified [joined prototype](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-06_R80_RESTART_COMPOSITION.md). Each successful generation owns fresh children; completion reports are captured before retirement and remain immutable. Factories run only after old children retire. A false restart request keeps the old outcome available; acceptance disables duplicates, and the caller disposes the display only after fresh admission. Runtime and root factories are sequential: a later failure closes an already admitted prefix and does not roll back caller placement. Failed owned retirement is terminal. Borrowed world/store/surface/FIFO remain caller-owned, and empty root buffers can coexist with a faulted movement outcome.

The Round80 small standalone prototype host proof passes two completed runs, a terrain fault and recovery with actual GUI routing, four handshakes and three exact offline roots. Its display persists across frames and failed requests. That round leaves the single-run launcher unchanged. Round81 below closes the full terrain/actor/input/camera restart wiring; physical native-window clicking and live multiplayer remain separate acceptance gates.

## Persistent full preview: Round81

The interactive launcher (external historical reference) creates one fresh full preview at a time. Completed or faulted previews drain their input, camera, clocks, root transport, actors, terrain and resource jobs before exposing the persistent outcome. Click **Restart offline preview** to load the next explicit run. The prior outcome remains visible until new resources/input owners are ready. Existing one-run launches still exit normally.

Prepare a default idle Version13 profile above, with `InteractiveReplay=false`. Supply `$BaseArgumentsJson` as the complete accepted native argument vector, including original asset/configuration/catalog and every existing synthetic replication/customization partition. It contains one `--terrain-point-target-profile` argument; the producer substitutes a private profile per run and reseeds inherited synthetic protected fixtures. It does not build missing input context or contact a server.

> Historical prototype-specific command or code example omitted from the research-only documentation.


Build the current prototype host project with the existing cached offline SDK/packages before launching. The argument file is a UTF8 JSON array of1..32 full string argument arrays, at most262144 bytes; each run has2..256 arguments of at most4096 characters. The per-session simulation/click/settings limits remain explicit. At the successful run cap, additional restart requests decline; start another launcher to begin a new bounded group. Failed cleanup is terminal and retains its incomplete prefix.

Joined verification (external historical reference) passes four actual original-resource previews in headless/GPU:401,401,225 completed/226 attempted terrain fault, then401 healthy updates;140 protected roots and26 original footstep occurrences. Actual viewport W/Shift/orbit/wheel retirement stress resets on the successor. Genuine SubViewport button events reject/retry one factory and admit three replacements; physical OS pointer routing is unexecuted. Controlled active close through this launcher passes31 updates/three roots, reports Incomplete, drains and exits without a successor. Physical OS close notification remains unexecuted. Full reproduction, independent oracles and uncertainty are in the [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-06_R81_FULL_PLAYGROUND_RESTART.md).

## Named scenario entry: Round82

The [scenario manifest](OFFLINE_SCENARIO_MANIFEST_CONTRACT.md) stores named input
paths, eight explicit Boolean options and one private Version13 profile per run.
The producer (external historical reference) accepts the same
complete `$BaseArgumentsJson` context above and a supplied `$OutputProfile`.
Use an idle profile with `InteractiveReplay=false` for human-operated inspection.
The producer reseeds every run and writes a fresh ignored scenario directory.

> Historical prototype-specific command or code example omitted from the research-only documentation.


Build with the existing offline SDK/package setup first. All16 context inputs
remain required; this entry does not extract missing assets or create missing
replication/customization fixtures. The explicit producer profile replaces the
base argument file's point-profile template; both source identities are recorded.
Relative base input paths resolve against the base file. Relative manifest paths
resolve against the manifest directory. The scenario path is the only user
argument in this mode; mixed argument overrides are rejected before shell creation.

The [inspection panel](OFFLINE_PLAYGROUND_INSPECTION_CONTRACT.md) displays the
last completed run's update counts, fault and cleanup status, actors, jobs,
archives and root frames. These are historical counts while the next preview is
active. At the finite run cap, Restart is disabled and the panel explains the
limit. **Close offline preview** requests drain and exit. Owned prototype host window-close
signals use the same idempotent route. Physical OS pointer and close-button
routing remain unverified.

Manifest parsing checks existence at admission; it does not pin private files
against later changes or validate profile semantics in place of the child guard.
Keep the producer's receipt and private profiles together. A manifest hash alone
does not identify the contents of the files it references.
