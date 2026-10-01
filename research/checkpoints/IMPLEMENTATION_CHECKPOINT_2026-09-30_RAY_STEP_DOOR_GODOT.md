# 2026-09-30 local-step doorway host

## Question and inputs

Does the explicit local coverage option preserve host controls and placement on the independently checked black-sun threshold? Inputs and SHA-256 pins are those in the [actor checkpoint](IMPLEMENTATION_CHECKPOINT_2026-09-30_RAY_STEP_DOOR_ACTOR.md), selected 1/1 forward and 2/1 reverse. Tools: Godot 4.7.2 .NET x64 and the bounded offline PowerShell preview launcher.

## Confirmed result

`-LocalStepDoorWalk` requires `-StepDoorWalk`, which requires precise movement. It selects `FloorStepPolicy(UseLocalRayCoverage: true)` in the engine-neutral actor. Forward headless and reverse GPU runs pass: each checks 20 repeated manual transfers and two owner poses, retreat/reset/contact/reflection, fixed-step pause/focus/stall handling, timestamped physical keys and diagonal owner round trips. Manual step-event counts are 23 and 25 respectively. These host counts are separate from the independent 1,896-tick actor fixture.

The private reverse capture was inspected: actor, floor and leaf placement are consistent with the selected bridge/elevator ownership and reflected owner transform. Build passes with zero warnings/errors. The known nonfatal certificate-store startup diagnostic persists. No original executable or server was run.

Confidence: Confirmed bounded host integration. Original solver policy and arbitrary multi-floor traversal remain Unknown. No new executable names or protocol rows.

## Next action

Anchor optional inspection approaches to the selected paired floor edge. Test stacked-floor selection independently before changing any default or reporting a new screen count.
