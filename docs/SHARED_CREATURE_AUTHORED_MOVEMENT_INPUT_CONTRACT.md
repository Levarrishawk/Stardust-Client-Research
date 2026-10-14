# SharedCreature authored movement input consumer

Round47, 2026-10-04. This engine-neutral consumer owns an immutable typed snapshot of the accepted [Round46 profile](SHARED_CREATURE_MOVEMENT_INPUT_PROFILE_CONTRACT.md). It selects explicitly requested authored literals under a caller-named modern policy. It does not establish final runtime movement values.

Implementation: [SharedCreatureAuthoredMovementInputs.cs](../../SWG-Godot-Client/poc/Stardust.Core/Assets/SharedCreatureAuthoredMovementInputs.cs). Reproducible scenarios: [consumer scenarios](../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/SharedCreatureAuthoredMovementInputScenarios.cs), [runner](../../SWG-Godot-Client/poc/round47-tests/Networking/Program.cs), and [independent peer](../../SWG-Godot-Client/poc/round47-tests/Networking/peer.py). Evidence and validation are consolidated in the [Round47 checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-04_R47_AUTHORED_MOVEMENT_INPUTS.md).

## Typed snapshot

| Typed slot | Frozen profile key | Interpretation retained |
| --- | --- | --- |
| Speed0 | `speed[0]` | First authored speed slot |
| Speed1 | `speed[1]` | Second authored speed slot |
| SlopeAngle | `slopeModAngle` | Raw authored angle input |
| SlopePercent | `slopeModPercent` | Raw authored modifier input |
| WaterPercent | `waterModPercent` | Raw authored modifier input |

`FromProfile` retains each slot's Literal/Unsupported/Absent classification, nullable finite literal value and exact binary32 bits, winning field identity, and ordered source evidence. Evidence includes source/version/parent, nullable parameter mode and relative byte, raw literal bits where present, and independently owned encoding. `CopyEncoding` returns another copy. Sources are copied into a read-only dictionary; lists and selections are read-only. Signed zero survives unchanged. Unsupported and absent slots have no operational value or value bits, but preserve diagnostic evidence.

Observation: Round46 already stops at the nearest unresolved nonzero parameter, leaving a farther literal shadowed. Round47 copies that result; it does not traverse or resolve the parent chain again. Confidence: Confirmed for this modern implementation, with independent serialized-byte fixtures. Compatibility consequence: callers must handle unresolved slots explicitly; a missing requested input cannot receive an implicit default.

## Atomic selection

`TrySelect` requires one to five distinct valid enum slots and a policy with a printable ASCII name of 1–128 characters, at least one non-whitespace character, finite nonnegative maximum absolute value, and an optional nonnegative-value requirement. `FiniteLiteral` accepts supported finite authored literals, including negative values and signed zero. An explicit nonnegative policy rejects values below zero; negative zero remains eligible. Maximum absolute value is an inclusive modern guard in raw units.

Every requested slot must be Literal and satisfy the policy. Otherwise the method returns false and a null selection. No eligible prefix is published. Unrequested unsupported slots do not prevent a valid explicit subset. Successful results preserve the request order, policy name, and exact typed inputs. Invalid policy/request/limit arguments throw; aggregate capacity exhaustion throws `AppearanceGraphBudgetException`. The out parameter is cleared before validation.

These policy names describe caller choices, not recovered client policy. No relative operation, range sampling, weighted random choice, angle conversion, replicated CREO4 seed, multiplier formula, default speed, or final runtime writer is supplied.

## Optional planar adapter

`TryPlanarVelocity` requires explicit Speed0 or Speed1. The supplied direction must have finite components and squared length within the caller's tolerance of one, evaluated in binary64. Default tolerance is `0.000001`; valid tolerances are finite from zero through `0.001`. The method checks the supplied direction without normalizing it. It selects the single speed through the named policy, independently requires finite nonnegative speed, multiplies using modern binary32 vector arithmetic, and rejects nonfinite output. Failure publishes null. Signed-zero multiplication follows modern .NET binary32 behavior.

This pure adapter has no state transition, clock, motion-driver mutation, or engine dependency. Its numeric rules are modern implementation choices, confirmed by an independent Python binary32 oracle. They are not an assertion about original x87 intermediates or legacy motion equations.

## Bounds and accounting

Snapshot limits are caller-tightenable, capped at 16 sources, 80 evidence records, 1,048,576 raw encoding bytes, and 4,194,304 units each for snapshot bytes and work. The logical charge is:

`snapshot = 64 * sources + 64 * 5 + 64 * evidence + rawBytes`

`work = 5 + sources + evidence + rawBytes`

Raw bytes count each retained slot evidence encoding separately. Charges are deterministic reservation units, not an exact managed heap measurement. Preflight counts all records and raw lengths and checks cancellation before allocating the snapshot and encoding copies. Cancellation is also checked during construction and before publication. The accepted frozen profile remains the independently bounded source of strings, fields, and metadata.

Selection is bounded at five slots, five work units, and 512 logical output bytes. It reserves `64 + 48 * requestedCount` before allocation, after validating distinct request names and before checking eligibility. Tightened bounds can reject an otherwise ineligible request before returning false. A successful selection references immutable snapshot inputs rather than duplicating their encodings.

## Verification and remaining boundary

Twenty synthetic consumer fixtures passed native checks: twenty copy-isolation checks, sixty exact/one-below aggregate boundaries, and 220 deliberate argument, capacity, and cancellation rejections. Independent Python verification passed 659 checks across raw bits, source evidence, selections, binary32 planar output, accounting, and one configured original five-source chain. Original source bytes and authored numeric values remain private; only identities and aggregate results are recorded here. The .NET runner and default 64-bit smoke suite built with zero warnings and errors and completed successfully.

Remaining uncertainty is the same compatibility boundary as Round46: authored input does not prove replicated-state arbitration, original runtime angle conversion, final speed selection, relative/RNG evaluation, or writer order. The next bounded probe is to trace one original runtime speed writer and its arbitration with replicated state using static instructions and callers before connecting this consumer to a motion driver.

## Round48 compatibility increment

An [explicit admission adapter](SCENE_WORLD_EXPLICIT_MOVEMENT_ADMISSION_CONTRACT.md) now reaches actual local motion; [144 original-authored/synthetic-received joins](SCENE_WORLD_CAPTURED_MOVEMENT_JOIN_CONTRACT.md) pass without fallback or queue rewriting. [Direct original writer evidence](CREATURE_MOVEMENT_WRITER_CONTRACT.md) shows constructor/delivery share current slots; this modern source chooser is not original full arbitration.

## Round49 compatibility increment

[Original-resource sourced preview](SCENE_WORLD_SOURCED_MOVEMENT_RESOURCE_PREVIEW_CONTRACT.md) passes authored/received headless and authored GPU joins through the actual original recipe closure with zero extra movement reads. [Current raw inputs](SCENE_WORLD_CREATURE_MOVEMENT_CURRENT_INPUT_CONTRACT.md) retain explicit constructor provenance and exact received overrides without store seeding; [explicit stop and sourced input](SCENE_WORLD_SOURCED_INPUT_HOST_CONTRACT.md) own the modern direction/clock lifecycle. Original full arbitration, modifier/vehicle semantics and units remain separate.
