# Supplied nominal-speed trace in the original-resource host

Round56,2026-10-04. This is an opt-in offline integration of the [pure nominal transition](CREATURE_NOMINAL_SPEED_TRANSITION_CONTRACT.md) with the existing native planar movement driver and acknowledged Godot presentation. Its inputs are synthetic targets, rate and time. It does not recover physical acceleration units, the dynamic legacy rate getter, prediction or full locomotion parity.

## Explicit boundary

[Host](../poc/Stardust.Godot/ScenePumpedNominalSpeed.cs) uses `--nominal-speed-fixture` through the [bounded preview launcher](../poc/round16-tests/Integration/Launch.ps1). It requires the complete local-input/partial-view offline proof and excludes conflicting sourced-input, terrain, cancellation and input-loss modes. Existing modes are unaffected when the option is absent.

The owned JSON document is limited to262144 bytes/depth12 and1..128 steps. Version1 and policy `supplied_nominal_rate_modern_planar_trace` require initial positive-zero current speed,10000-microsecond ticks and the corresponding supplied binary32 seconds word. Precision and retained rate use the pure core API. Every resulting stored speed must be finite and within0..10, the existing driver's explicit velocity bound. The trace ends at stored positive zero. These are modern host restrictions, not inferred legacy acceptance rules.

The final test fixture is independently authored:60 steps at target2, then6, then0, with supplied rate32 and explicit nearest-even64 arithmetic. It exercises rise/fall/equality and clamping. The independent Fraction peer checks each retained candidate, candidate store, final store and branch. Engine movement uses the frozen driver's binary32 displacement/publication graph and integer timeline, separately from the selected speed arithmetic.

## Atomic preflight and ownership

At the end of the existing offline transcript, the selected original creature is finalized/current and its prior input driver is at zero speed. The entire pure transition plan is computed before closing that writer or constructing a new driver. A late invalid input publishes no new motion and constructs no new driver. A preflight failure retains only private diagnostics and normal host failure cleanup drains the existing scene.

After success, the host closes the previous writer and owns one `SceneWorldLocalMotionDriver`, borrowing the current world/motion store. Each tick submits the actual stored speed as native Z velocity at the preceding tick timestamp and advances exactly one10000-microsecond step. This is a controlled supplied-velocity trace, not a keyboard adapter or a recovered PlayerController invocation. Presentation consumes the resulting actual motion store; native-to-Godot reflection is checked.

Every step verifies that the actual original resource publication and visual fingerprint remain retained, with no extra archive job or asset read. The stop uses an explicit zero velocity and inactive driver transition; close disposes the driver before draining the borrowed world/presentation. Cleanup is idempotent across finish, failure and tree exit. The pure result's rate provenance does not become an original rate getter merely because the actor uses original assets.

## Reproduction and evidence

[Native join](../poc/round56-tests/Integration/NominalSpeedJoinProgram.cs) consumes the same synthetic trace. [Independent peer](../poc/round56-tests/Integration/nominal_speed_host_peer.py) provides `generate`, `native`, `host` and `negative` actions with fresh ignored-private fixture/receipt destinations. Native and host outputs are compared against regenerated Fraction expectations; position updates and reflected presentation are checked independently. The negative fixture changes only the last target to a synthetic NaN.

Original resource/configuration identities and original object-to-appearance/mesh/material loading are enforced by the existing accepted offline preview. No original executable execution, live connection, asset mutation or publication is involved. Generated traces/logs/screenshots stay private.

Final executed gates and corrected failures belong in the [Round56 session](../research/sessions/PARALLEL_DEVELOPMENT_ROUND_56_2026-10-04.md) and [lead checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-04_R56_BOUNDARY_ADMISSION_AND_NOMINAL_SPEED.md). Remaining integration work: connect an explicitly evidenced target-construction policy and rate source, then selected-object/lifetime and reconciliation behavior. Conditional callback seconds alone does not close those boundaries.



## Round57 target-input extension

[Version2 supplied target host](CREATURE_NOMINAL_TARGET_SPEED_HOST_CONTRACT.md) constructs actual target receipts before the existing transition/motion stage. Version1 remains accepted by the same flag and passes its independent regression; all target/transition steps complete before the motion writer transfers.
