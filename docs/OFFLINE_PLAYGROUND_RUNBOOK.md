# Configured offline playground

This is a bounded offline preview using the original supplied resource chain, modern terrain/movement policy, and synthetic protocol peers. It is not a live client login or a standalone bootstrap. [Round79 evidence](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-06_R79_OFFLINE_OUTCOME.md) records its tested boundaries.

## Prepare the profile

Use the accepted Version12 profile and original-resource/peer context already used by the [launcher](../poc/round16-tests/Integration/Launch.ps1). The [producer](../poc/round77-tests/Integration/create_configured_profile.py) writes a fresh Version13 profile into an ignored output directory. It does not extract assets or contact a server.

```powershell
python poc/round77-tests/Integration/create_configured_profile.py --source $SourceProfile --output $OutputProfile
$HostArguments.TerrainPointTargetProfile = $OutputProfile
& poc/round16-tests/Integration/Launch.ps1 @HostArguments
```

Here `$SourceProfile` is the accepted Version12 profile, `$OutputProfile` is a new ignored path, and `$HostArguments` is the existing accepted host argument dictionary, including Godot path, original archive/configuration, catalog, synthetic peer partitions and customization context. Those inputs must be supplied; the profile does not replace them. Use the existing cached Godot/.NET build setup described in the [PoC README](../poc/README.md).

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
