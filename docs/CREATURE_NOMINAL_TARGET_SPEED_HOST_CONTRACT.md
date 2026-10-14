# Supplied nominal target and transition host

Round57 extends the [Round56 supplied-rate host](CREATURE_NOMINAL_SPEED_TRANSITION_HOST_CONTRACT.md) with opt-in fixture Version2 and policy `supplied_nominal_target_and_rate_modern_planar_trace`. [Target contract](CREATURE_NOMINAL_TARGET_SPEED_CONTRACT.md) and [transition contract](CREATURE_NOMINAL_SPEED_TRANSITION_CONTRACT.md) remain engine-neutral.

## Inputs and order

The bounded owned JSON supplies run and walk binary32 getter-return words, a byte run flag, three direction words in recovered operand order (locals -8, -C, -4), and a retained nominal factor for each step. These words do not assert physical units, original getter behavior, original global input state or an original direction coordinate convention. Explicit nearest-even64 controls and the frozen supplied rate complete the modern trace.

Round58 Version2 uses one actual [atomic plan](CREATURE_NOMINAL_SPEED_PLAN_CONTRACT.md). It primitive-validates the whole supplied sequence before arithmetic, copies it, then retains the actual target and transition receipts in order. The target builder preserves its squared sum, square root, intermediate binary32 snapshot and retained factor multiplication; each transition consumes its actual target. The host checks all stored speeds before closing the preceding input writer and constructing the motion driver. A failure publishes no new motion writer. Version1 retains its prior supplied-target loop.

The 60-step fixture exercises run, high-bit byte run, walk fallback, a non-unit direction and a factor of two. The modern planar adapter sends each actual stored transition speed along native positive Z; it does not use the supplied direction as a recovered movement vector. Native-to-prototype host Z reflection and resource identity retain the existing host contract.

## Receipts and cleanup

`nominal_speed_plan_ready` exposes actual aggregate evaluations, stores, work and retained-reference checks. `nominal_target_state` exposes the actual target receipt alongside each motion frame. The inspection projection renames retained values for comparison; it does not recompute them. Native and host acceptance require an independent Fraction/isqrt target oracle, then the prior transition and planar position oracle. Original-resource retained identity and unchanged archive I/O counters are required.

The late invalid-direction fixture changes only the final direction word to NaN. Round57 historically reported59 completed pure steps from its loop. Round58 atomic preflight rejects that final primitive before any target arithmetic; acceptance requires zero completed/published plan steps, no new driver or motion publication, no plan/target/motion frame records, zero remaining actors and drained archive ownership. The owned JsonDocument input bytes remain alive until the document is disposed; buffers are then cleared.

These are bounded modern supplied-input gates. Full original locomotion, physical acceleration, active getter/factor ownership, effective floating-point controls, later speed zeroing, vector delivery, root motion, collision and reconciliation remain separate.

## Status

Native/headless/GPU60-step target/transition/position and late-rejection gates pass; prior Version1 regression passes. Final assembled acceptance is recorded in the [Round57 session](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_57_2026-10-04.md).


## Round58 atomic host

Native/headless/GPU60-step comparisons, late rejection and Version1 regression pass in [Round58](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_58_2026-10-04.md). Logical host caps are128 steps,81920 copy units and8388608 work units. No plan receipt reaches the motion driver before complete native acceptance.


Round58 headless/GPU and native60-step checks pass with actual plan receipts; late direction rejects at whole-plan primitive preflight with zero published plan steps and clean drain. Prior Version1 regression passes.


## Round59 sparse terrain and received local speed

[Round59 lead checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-04_R59_SPARSE_HEIGHT_NORMALS_AND_CURRENT_SPEED.md) adds positive-cell sparse heights, modern stored-height normals/lit policy and actual received local fields through paired supplied readers/atomic speed planning. Independent worker/main/native/headless/GPU/malformed-input/cleanup and prior-mode gates pass. Height-query count200 versus400 with explicit supplied warm priors; no original cache/normal/control/locomotion parity is inferred. Queue backing release ordering closes while concrete payload ownership remains Unknown. Research89/readiness94,2626 names and345 packet rows stay unchanged.
