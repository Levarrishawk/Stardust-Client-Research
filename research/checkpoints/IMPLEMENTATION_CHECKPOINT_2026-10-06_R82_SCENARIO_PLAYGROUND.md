# Round82: offline scenario entry and inspection

Date: October6,2026. Status: joined replays passed; final acceptance recorded in the session.

## Bounded question and input

Can one authored scenario manifest admit the existing full offline context,
create fresh bounded previews, expose their actual primitive outcome, and route
a controlled close without creating a successor?

Input: [accepted Round81](../sessions/PARALLEL_DEVELOPMENT_ROUND_81_2026-10-06.md).
The scenario format is modern authored configuration; it is not an original
client format or network message. Original executable research remains static.
The main public snapshot contains4687 files at
`ece6e3943ac84fb10d84bc9d44314fa7ba032f92` with an empty index.

## Components and evidence boundary

- [Strict engine-neutral scenario parser](../../poc/Stardust.Core/Offline/OfflineScenarioManifest.cs)
  validates exact keys, duplicate properties, UTF8, bounds, local paths and
  required input existence before returning immutable argument configuration.
  Version13 profile semantics remain the existing preview admission gate.
- [Scenario producer](../../poc/round82-tests/Networking/scenario_author.py)
  translates explicit accepted arguments into named inputs and Boolean options,
  resolves paths against their source file, and reseeds inherited synthetic
  transport fixtures using the independent Round81 codec.
- [Actual launcher](../../poc/Stardust.Godot/PersistentOfflinePlaygroundLauncher.cs)
  accepts `--offline-scenario <file>` without argument overrides and then uses
  the existing fresh-preview shell. Invalid manifest admission precedes factory
  creation. The existing argument-array entry remains available.
- [Inspection](../../poc/Stardust.Godot/PersistentOfflinePlaygroundInspection.cs)
  reports last-completed counts and cleanup as immutable primitive history,
  disables restart at the finite cap, and provides controlled close.
- [Joined runner](../../poc/round82-tests/Integration/Invoke-ScenarioPlayground.ps1)
  stores an independent policy before native execution and checks actual
  movement prefixes, handshakes, transport buffers, fresh state and cleanup.

The short proof passes two three-update idle runs using original resources in both headless and GPU.
The close proof passes31 successful updates/three protected roots using the known scripted movement prefix and an explicit
synthetic `Window.CloseRequested` signal. That signal is not a physical OS event.
Diagnostic wall-clock readiness/retirement costs do not drive simulation and
include resource setup, simulation and drain; they do not establish a general
performance comparison or justify a cache.

## Acceptance and next probe

Lead reviews/integrates31 exact final worker hashes (Assets4, Runtime6, Networking4, Rendering17). No worker revision after seal. Static rule replay passes19 negatives/eight authored effects; parser28 positive/90 negative; producer412 checks/44 rejections. Actual Core/producer argument joins preserve the full accepted context with only per-run profile substitution. Rendering replay uses pristine frozen81 plus its own sealed82 files, preserving all dependency guards;52 headless/25 GPU new inspection checks, frozen94/95 HUD and25 shell regression pass. Eight runner guards are worker-replayed; the lead repeats the native component positives.

The actual manifest launcher passes two three-update idle previews in headless/GPU. Each uses fresh preview/camera IDs and transport identities, zero inherited queues/latches, actual original terrain/actor resources, three closed archive jobs, and no remaining actors/jobs/publications. Independent prior81 trajectory prefixes and transport/reset verifiers check the logs; no native output becomes an expected trajectory. The cap screenshot was inspected: disabled Restart, enabled Close, readable primitive outcome and inspection counts. Four native malformed admissions (missing input, mixed override, resource URI, missing path) exit1 before preview/root admission.

Owned synthetic Window.CloseRequested passes31 completed/31 attempted updates, three protected roots/ACKs and an Incomplete report with full drain. Duplicate RequestClose remains one intent; no successor is created. Actual terrain fault through the new manifest entry was not replayed; prior81 full fault/recovery evidence and new synthetic inspection fixtures remain separate. Physical OS pointer/close and live multiplayer are unexecuted.

Readiness wall times observed for the first/second short previews: headless5.477/4.266 seconds, GPU6.657/4.537 seconds. Total diagnostic times include simulation/drain (headless5.553/4.299, GPU6.733/4.564). No simulation clock changes, cache or performance/model-speed claim. Components were replayed concurrently with headless execution, so these are not controlled measurements.

Corrections before acceptance: Runtime resolved-path length bound; Networking extra-colon admission parity; lead launcher local scenario-path guard. Rendering's first synthetic test compile needed an explicit integer cast for the notification constant; corrected test-only and complete replay passed. Lead's first Networking invocation omitted required --profile and rejected before output; the corrected explicit-input invocation passes. Offline certificate diagnostics remain in successful engine logs. Controlled close intentionally logs `OperationCanceledException: Persistent window close`; its expected Incomplete report and clean drain are verified. No unexpected validation error, native cleanup failure or arbitrary guard weakening remains.

Reproduction: author a scenario with [scenario_author.py](../../poc/round82-tests/Networking/scenario_author.py), run [ScenarioAdmission.csproj](../../poc/round82-tests/Integration/ScenarioAdmission.csproj) against that scenario and the accepted base context, then [Invoke-ScenarioPlayground.ps1](../../poc/round82-tests/Integration/Invoke-ScenarioPlayground.ps1) with explicit Python/Godot/cache/scenario/trajectory/fresh-private output. Use the accepted pre-native independent trajectory generated by Round81's [producer](../../poc/round81-tests/Integration/playground_restart_peer.py) and source normal/fault/terrain/original-clip inputs. Short profiles must be idle Version13 with MaximumUpdates3; close fixture is the accepted6000-update scripted normal profile and --CloseSignal. --Gpu captures the cap image. [verify_launcher_admission.py](../../poc/round82-tests/Integration/verify_launcher_admission.py) repeats four admission negatives against an already built host. Own worker producers/checkpoints retain their explicit-input reproduction commands.

Next bounded implementation question: can the complete explicit input context be prepared by one reproducible offline command with stable identities, avoiding dependence on manually assembled historical argument vectors? Do not add a cache before controlled setup measurements. Next research probe:193-byte00AA4250 delegate, direct writes/reference forwarding/discriminator0 and immediate failure boundary; global writer/allocator/lifetime graph remains separate.
