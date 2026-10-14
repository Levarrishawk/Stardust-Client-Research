# Round66 point-target input adapter

Date: 2026-10-06. Status: implemented and verified; Rendering worker idle after seal.

## Bounded question and stopping condition

Can a main/owner-thread adapter forward only real pressed-left mouse events through the actual retained camera picker and actual terrain point-target controller, with exact query/receipt identities, non-mutating preflight, bounded diagnostics, honest partial-effect reporting and borrower-safe disposal? Stop after actual headless/GPU runs, independent camera/hit/target/clock comparisons, borrower cleanup and frozen-scope checks pass.

## Inputs, amendment and evidence

The trusted Round66 worker sync verified 4,123 public files. Baseline manifest SHA-256: `b6fc2f1db7ebe4a7513b089fca2f68c23824e234e349ce2852b29d99804318f3`; archive: `292d84bcfd2180121f82f14b8958f0b2d6be4865e4878cba13287ba221ba98ed`. Original executable identity was checked against `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150historical prototype identifier (omitted)2eb2eea17e552c25c37f7b82bf859ff2f3dd254c38b6cf5182021b51134e108b`.

Initial source inspection found that fresh borrowers lacked public non-mutating admission predicates: picker `IsCurrent` requires an issued current receipt; controller `IsCurrent` requires an issued current target. Cached state alone cannot prove current incarnation after world retirement. The lead explicitly approved a narrow amendment. Rendering owns the existing picker file only to expose `ValidateAdmission` and share actual existing validation with Pick. Runtime owns `ValidateTargetAdmission`, including target budget. Target-cap rejection therefore precedes picking; post-pick prefix tests use callback cancellation/reentry instead.

The one imported approved Runtime dependency is SceneWorldTerrainPointTargetDriver (external historical reference), exact SHA-256 `916fbd99070bf46889c70e1bd82d2230555466e37ab534358d2f32a696091c7d`. The destination was checked against its frozen65 manifest hash before exact-copy; source and resulting destination matched the announced lead-approved final hash. This file is a dependency exception, not a Rendering-owned change. No sibling source was copied. An earlier private compile-only declaration was not published or executed; all final execution uses the actual approved controller.

## Observations and findings

Confirmed by actual source and execution: TerrainPointTargetInputAdapter (external historical reference) borrows exact same query instances, actual picker/controller and owns only managed diagnostics/references. Constructor admits current controller clock and live picker without query. Pressed-left input is passed to actual picker HandleInput; exact returned hit is passed to actual controller SubmitTarget. Released/right/motion events return false without counts or selection mutation. Misses report the actual nullable pick and retain the prior target. Fresh misses without a prior target remain current.

Confirmed: owner/main-thread, token, invalid pixel/range/projection, query identity, revision overflow, picker/adapter budgets, current clock, unavailable/retired world and reduced controller target-budget cases reject at their documented boundaries. A maximumTargets=1 controller's last admissible successful target remains current; the next left press rejects before another adapter or picker charge. Target receipt currency uses actual controller IsCurrent independently of capacity. Historical adapter results stale when replaced; latest exact references remain available in diagnostics.

Confirmed: a real marker VisibilityChanged callback cancels after picker publication. The failed adapter result retains the exact current pick and query-charge prefix, no new target receipt, prior actual target and failure type. A real ChildEnteredTree callback catches nested adapter HandleInput and Dispose exceptions; the poison flag still prevents the outer target suffix while retaining the committed picker receipt. A nested picker ValidateAdmission caught inside the actual AddChild callback preserves picker reentry poison, rejects its outer publication and drains staged resources. No atomic two-borrower transaction, picker rollback or marker rollback is claimed.

Confirmed: adapter disposal drops only its references/LastResult. Picker marker/current receipt and controller target survive; subsequent explicit owners drain their resources. Terrain and typed world/store are supplied without resource-loading jobs.

## Reproduction and validation

Run the standalone offline runner (external historical reference) with explicit Python, .NET, documented executable and accepted cached prototype host packages, choosing a fresh ignored private RunRoot. It pins the approved Runtime source before output/build, pins executable identity without execution, stages only replacement sources and produces synthetic geometry from accepted65/62 models. The private handoff contains the exact portable replay command and all owned source hashes.

Final replay: actual build zero warnings/errors. Headless69 checks and GPU70 checks passed. Each mode independently matched six camera/projection/reflection/triangle samples, five hits, one miss and five exact binary32 target X/Z conversions. Caller clock expectations are fixed independently by scenario label, not seeded by emitted expected-clock metadata. Target coordinates derive from the Fraction triangle oracle, not native hit/target fields. Native assertions verify actual issued object identities and actual adapter/picker counter values. Fresh-miss, reduced-cap, callback-prefix and cleanup assertions also passed.

GPU readback proved the actual magenta marker at the camera center; the private capture was visually inspected. This diagnostic checkerboard geometry and marker do not prove original rendering or input fidelity. The offline runtime reports its existing root-certificate-store warning in both modes; processes exit0 and no network operation is requested. No object/resource leak diagnostic appeared.

Frozen Round65 picker regression also passed: headless51 checks/18 projections/13 hits/5 misses; GPU52 checks/18 projections/11 hits/7 misses. Its actual camera/query/marker/ownership behavior remains verified after the admission refactor. Eight runner guards rejected wrong/empty executable, public/existing output, live/dangling junction paths and non-directory ancestors; prior receipt and linked target remained unchanged.

The final scope receipt verifies all baseline files except the owned picker amendment and exact imported Runtime exception, all owned new files, HEAD/index, UTF8/noBOM/LF/whitespace, Python/XML syntax, relative links, staged execution hashes and private output identities. Organized-workspace validation and git diff check/status are retained in the handoff.

## Compatibility consequence and next probe

The bounded engine adapter can select modern straight-line native targets without rebuilding terrain or transferring borrower ownership. Admission closes the fresh-borrower liveness/current-incarnation gap; repeated admission cannot make two separate mutations atomic. Cooperative cancellation or external borrower mutation can still leave a committed pick prefix. Original picking/gameplay/movement units and collision/pathfinding parity remain unproved; no research/protocol/function coverage score changes.

Next probe: integrate the adapter as the scene's sole input-to-target path, use the caller's current controller clock and prove full-root authoritative correction, retarget, pause/resume and generation teardown with exact shared ray-query identities. Preserve the standalone prefix and disposal tests; do not infer original behavior from the modern adapter.
