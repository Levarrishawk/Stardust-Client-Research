# 2026-09-30 — Fixed-step actor and continuous keyboard adapter

## Question and stopping condition

Can a bounded engine-neutral scheduler separate actor ticks from rendering, preserve equal tick inputs across three render partitions, and suspend cleanly at pause/focus boundaries? Close after synthetic, independent original and headless/GPU adapter checks pass.

## Inputs, evidence and confidence

Executable identity remains SHA-256 `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`, rechecked this run. No new VA claims. Original root `C:\Stardust-DEV`, configuration `swgemu_live.cfg` SHA-256 `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`, 48 mounts and six input pins are unchanged from the [instance checkpoint](IMPLEMENTATION_CHECKPOINT_2026-09-30_INTERIOR_DOOR_MOVEMENT.md).

Observation: the integer scheduler runs the same fixed tick input sequence under 100 Hz, 20 Hz and jittered rendering. Interpretation: modern actor results can be independent of rendering when input boundaries are controlled. Confidence: Confirmed for these fixtures. Consequence: preserve explicit timing, suspension and dropped-time accounting; do not infer input transition timestamps from late render snapshots.

Godot's installed 4.7.2 API XML documents monotonic `Time.GetTicksUsec`, physical key polling and buffered-event flushing. Injected immediate key polling initially failed with only a physical code. Supplying both logical/physical codes and flushing buffered events resolves the fixture. This is adapter evidence, not a recovered original input system.

The [durable contract](../../docs/FIXED_STEP_ACTOR_CONTRACT.md) records exact scheduler, ownership, failure and sampling limits. Core timing remains engine-neutral.

## Validation

```powershell
dotnet run --no-restore --project poc/Stardust.Core.SmokeTests
dotnet build --no-restore poc/Stardust.Godot
python tools/python/verify_fixed_step_actor.py C:\Stardust-DEV C:\Stardust-DEV\swgemu_live.cfg local-private/generated-evidence/fixed-actor-milestone poc/Stardust.Core.SmokeTests/bin/Debug/net8.0/Stardust.Core.SmokeTests.dll
```

Core smoke and Godot build pass. Independent verification: 1,428 clock updates / 1,260 original ticks / 30 phase results, three render partitions × two owner poses, zero discrepancies. Reused independent oracle retains the previous 948-tick persistent trace. Synthetic gates cover invalid time/input, stall limit, suspend/resume, borrowed disposal and fault handling. The [prior launcher](IMPLEMENTATION_CHECKPOINT_2026-09-30_INTERIOR_DOOR_MOVEMENT.md) with `-DoorWalk` passes headless and GPU controls, including physical press/release/cancellation and pause/focus notifications; private capture inspected. Known root-certificate startup diagnostic remains nonfatal.

## Next probe

Queue timestamped input transitions at deterministic simulation boundaries, bound queue/time ranges and verify between-render changes without backdating. Conservative actor box, level-only movement and moving-door overlap remain unresolved. No new semantic names, protocol rows or coverage rescore: 2,599 / 345 / 89% / 94% remain separate current metrics.
