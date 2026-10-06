# Terrain point-target input adapter

[TerrainPointTargetInputAdapter](../poc/Stardust.Godot/TerrainPointTargetInputAdapter.cs) borrows an actual [camera picker](TERRAIN_CAMERA_PICKER_CONTRACT.md) and actual [point-target controller](SCENE_WORLD_TERRAIN_POINT_TARGET_CONTRACT.md) retaining the exact same ray-query reference. It owns managed bounded diagnostics and borrower references only. It creates no terrain, world, motion store, marker, controller or resource job and never disposes a borrower. This is modern replacement behavior; original picking and movement parity remain unproved.

## Admission and API

Construct `TerrainPointTargetInputAdapter(picker, controller, maximumAttempts = 16)` on the Godot main thread and both borrowers' owner thread. The limit is 1..16. Constructor validates exact query identity, controller admission at its current clock and picker admission at viewport pixel zero with maximumT zero; it performs no query or publication. Thus an exhausted or unavailable borrower is rejected even during construction.

`HandleInput(InputEvent input, long timestampMicroseconds, double maximumT, out TerrainPointTargetInputResult? result, CancellationToken cancellation = default)` returns false and null for motion, other mouse buttons and released left buttons. It checks owner, disposed state and null input first; ignored input does not inspect clock, token, caps, selection or query and leaves attempts and LastResult unchanged.

A pressed left event checks adapter capacity, exact retained query identity, actual controller `ValidateTargetAdmission` and actual picker `ValidateAdmission` before charging an adapter attempt. Controller admission includes current incarnation, health, exact current clock, token and remaining target budget. Picker admission shares the actual mutation path's owner, busy/reentry, disposed, live camera/parent/marker, pixel/viewport, finite maximumT in 0..1e9, remaining query budget, revision overflow and token checks. It performs no ray query, allocation, marker mutation or publication. Actual camera ray projection and its finite-direction validation still occur inside Pick; an adapter attempt can therefore be charged without a picker query charge.

The accepted event is passed directly to actual picker `HandleInput`. An actual nullable pick receipt is retained. After checking caught adapter reentry, cancellation, actual receipt currency and renewed controller admission, its exact non-null hit is passed to actual controller `SubmitTarget`. No hit, ray query or target receipt is reconstructed. A miss retains the controller's prior target and reports no new target.

## Diagnostics and partial effects

`Attempts` counts adapter attempts admitted to picker dispatch, including later failure; maximum 16. `PickerAttemptsBefore/After` record actual query-charge counts. Preflight rejection charges neither. `LastResult` retains one immutable issued result with adapter owner, attempt, caller clock, actual pick, actual newly issued target, prior target and nullable failure type. Ignored and preflight-rejected events retain the previous LastResult. A late failure records its observed prefix and rethrows; callers inspect LastResult after catching it. There is no retry.

The two borrowers do not form a transaction. A picker can commit its receipt/marker selection before a callback-induced cancellation, caught adapter reentry, borrower invalidation or later controller rejection prevents target submission. That exact actual pick is retained in the failed result; the adapter does not replace the previous target when SubmitTarget has not committed. An external owner can independently mutate or dispose a borrower; its effects are not rolled back. Picker failure may also charge a query without publishing a pick. Its existing reused-marker property mutation prefix is not rolled back. Late cooperative cancellation can race the final borrower publication checks; no hard cancellation or universal rollback is promised.

Adapter mutation reentry, including nested Dispose, sets a poison flag before throwing. Catching that nested exception in a Godot callback still prevents the outer target suffix. Picker `ValidateAdmission` uses its existing mutation gate and preserves its own caught-reentry poison. Read-only inspection does not consume attempts.

## Currency and lifetime

`IsCurrent(result)` requires exact adapter-issued latest successful result, a current exact picker receipt and a healthy controller. For a new target or retained prior target it asks actual controller `IsCurrent(selected)`; capacity exhaustion does not stale a successfully issued last target. For a miss with no prior target it uses current controller admission. With no clear-target operation, a no-target controller has consumed no target budget. A clock advance alone does not make historical target selection stale; a new adapter result, retarget, picker receipt replacement, unavailable incarnation or disposal does.

Dispose is owner-thread guarded, idempotent and drops only adapter borrower references and LastResult. Actual picker marker/current receipt, controller target and borrowed world/store/query survive. Each owner subsequently drains its own resources. Callers must continue to serialize controller ownership and honor its sole-local-writer constraint.

## Reproduction

The [standalone runner](../poc/round66-tests/Rendering/Invoke-Picker.ps1) stages an offline Godot project under a fresh ignored private output root. It pins the documented original executable as input identity only; it never executes it. It uses accepted frozen65/62 synthetic terrain generation, the pinned accepted65 synthetic typed baseline fixture, real camera projection and pressed/released/right/motion events. The [oracle](../poc/round66-tests/Rendering/verify_picker.py) reuses frozen Fraction triangle/projection models and explicitly casts independently expected target X/Z to binary32. Native hit or target values do not seed expectations.

Headless and GPU receipt results, exact tested source hashes and the approved Runtime dependency amendment are recorded in the [Round66 checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-06_R66_POINT_TARGET_INPUT.md). GPU marker readback proves a bounded diagnostic marker on synthetic geometry, not original visual or interaction fidelity.
