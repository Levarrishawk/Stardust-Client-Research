# 2026-09-30 — Timestamped actor input transitions

## Bounded question

Can changes between render/tick boundaries produce the same original actor trajectory across render partitions, without backdating new input or replaying skipped/suspended motion? Stop after independent replay and Godot event-routing checks.

## Identity and evidence

Executable SHA-256 remains `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`, rechecked this run; no new VA labels. Original root/configuration and six asset pins match the [instance checkpoint](IMPLEMENTATION_CHECKPOINT_2026-09-30_INTERIOR_DOOR_MOVEMENT.md). Configuration SHA-256 remains `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`, 48 mounts.

Observation: transitions at 137,000 / 1,213,000 / 2,419,000 / 2,817,000 / 3,007,000 / 4,113,000 microseconds drive equal tick-start input across 100 Hz, 20 Hz and jittered rendering. Confidence: Confirmed modern replacement policy. Original event quantization is Unknown. The [durable contract](../../docs/ACTOR_INPUT_TIMELINE_CONTRACT.md) records queue limits, failure, skipped history and ownership requirements.

## Validation

```powershell
dotnet run --no-restore --project poc/Stardust.Core.SmokeTests
dotnet build --no-restore poc/Stardust.Godot
python tools/python/verify_actor_input_timeline.py C:\Stardust-DEV C:\Stardust-DEV\swgemu_live.cfg local-private/generated-evidence/input-timeline-milestone poc/Stardust.Core.SmokeTests/bin/Debug/net8.0/Stardust.Core.SmokeTests.dll
```

Core smoke passes; Godot builds with zero warnings/errors. Independent original replay passes 1,488 updates / 1,320 ticks / 18 transfers, zero discrepancies. Synthetic direct tick-indexed comparison, queue limits, rejection, no-backdating, suspension/stall and reset gates pass. The prior bounded launcher with `-DoorWalk` passes headless and GPU; injected `_Input` transitions reach the queue, release/cancellation drain correctly, and one nonaligned original event crosses after 60 ticks. Private capture inspected. No original executable execution or server contact. Known nonfatal certificate-store startup diagnostic persists.

## Next probe

Add normalized planar W/A/S/D input in the same level two-cell slice; verify lateral/diagonal trajectories, speed limits, transformed-owner conventions and explicit floor/portal stops. Original moving-leaf collision, narrow phase and global solver ordering remain open. Research scores 89% / 94%, canonical names 2,599 and packet rows 345 unchanged.
