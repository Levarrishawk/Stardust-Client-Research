# Round71 accepted locomotion visual

Date: October6,2026. Question: can an opt-in Godot wrapper display a supplied native
locomotion state exactly, while preserving an existing disabled presentation chain and
rejecting callback/publication/lifetime failures without adding elapsed animation time?
Stopping condition: standalone headless/GPU state and original-pose agreement, ownership
and negative replay, focused scope audit, then handoff for the lead's main-scene join.

## Inputs and tools

- Frozen accepted70 public input:4297 files; manifest SHA-256
  `12bb3453e94a2041068db521c9cdcea6486a009baedab72a7e2f6e73cf814c37`, archive
  `eb66e5727b76f79969738c26c5ac89da1cb16f7422612920cacd4d26b9f7428d`.
- Original executable SHA-256
  `E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`, verified
  before executable-table reads. Configuration SHA-256
  `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`.
- Idle/walk/run clip pins are retained in the [independent producer](../../poc/round71-tests/Rendering/verify_visual.py).
  All three resolve at overlay priority30. Body mesh priority53:
  `93378D74AA75B1C6673C9008914FFB51CF7DC380FF18459DBBFC26EF016A59B0`;
  body skeleton priority53:
  `7F3EB762EB97B27C2170014BE66BFCA0C232E35B2F8CDBEFEA912736FB681CC3`.
- Cached Python, .NET8 and Godot4.7.2 .NET x64; offline cached package restoration.
  Trusted incoming sync completed before edits; final audit independently verifies every
  frozen byte, preserved HEAD/index and exactly ten new public files.

## Findings and confidence

Observation: [the new wrapper](../../poc/Stardust.Godot/SceneWorldAcceptedLocomotionVisual.cs)
reads the supplied immutable state once per enabled presentation, maps its exact enum
and frame to existing prepared pose APIs, and contains no animation clock/Advance call.
Interpretation: accepted-displacement phase can be displayed without elapsed-frame drift.
Confidence: Confirmed for the bounded replacement fixtures. Implementation consequence:
the caller owns state production and actor association; no speed threshold or second clock
belongs in this wrapper.

Observation: the public source record contains no provenance. The optional chain exposes
a publication but not private nesting. Interpretation: a valid wrong-actor/stale source
cannot be identified from this API. Confidence: Confirmed from frozen API inspection.
Implementation consequence: require the caller's same-actor/publication/clip-metadata
contract; require exact chain publication reference and recheck after its getter before
ownership adoption. The [durable contract](../../docs/SCENE_WORLD_ACCEPTED_LOCOMOTION_VISUAL_CONTRACT.md)
records these limits explicitly.

Observation: independent expectations precede native execution. Six supplied transitions
idle/walk/run/held-run/idle/walk agree in mode, frame, phase, accepted distance, switch count
and heading. Forty-eight first-shader body vertices agree with the independently sampled
original skeleton/clip/weight oracle; maximum component error1.7581968769775358e-7.
Confidence: Confirmed bounded state and pose boundary. Implementation consequence:
modern instant blend preserves the supplied pose and repeated same-state calls are
idempotent despite elapsed Tick60. This is not a whole-mesh or original timing claim.

Observation: disabled Tick forwards elapsed time and reads no state; enabled Tick forwards
zero seconds; Apply forwards exactly once and rejects an obsolete snapshot without state
read. Actual current Height is applied after a legitimate revision. Callback-induced
revision loss retires the chain instead of presenting over stale Height. Confidence:
Confirmed by [standalone verification](../../poc/Stardust.Godot/SceneWorldAcceptedLocomotionVisualVerification.cs).
Implementation consequence: the lead can retain legacy prepoint acceptance gates and
enable the wrapper when the supplied observer becomes active.

## Validation

Final standalone build: zero warnings/errors. Headless286 checks; GPU287 checks and a
private replacement PNG. Each independently compares six states/48 original vertices.
Twenty-three failure cases cover invalid frame/phase/distance/heading/mode/switches/null,
source and enable exceptions, caught source Tick/Apply/Dispose reentry, caught enabled
reentry, initial/callback cancellation, invalid time, detached/freed root, source/enabled
publication loss and source-induced current-state revision loss. Additional tests cover
off-thread rejection before callbacks, idempotent ownership disposal, wrong-chain
constructor rejection, and a matching publication getter that closes the world before
ownership adoption. The latter leaves the caller's chain undisposed and source unread.

The original prepared bundle is loaded once per run:39 sources/4,686,371 bytes; failure
fixtures reuse it. Eight runner guard cases reject wrong/empty executable, public or
existing output, linked/dangling output or ancestors, and nondirectory ancestors while
preserving prior receipts and linked targets. PowerShell syntax, Python/XML/Markdown,
workspace validator, frozen scope hashes and git diff whitespace checks pass. No shared
source, ledgers, registry or sibling dependency changes belong to this worker's handoff.

The offline Godot root-certificate-store warning remains with successful exit0 and no
network requested. Frozen external-freed-root disposal can throw after its owned resources
are cleared; retirement/single disposal are tested, no new no-throw promise is made.
Original executable execution, live server contact/capture/fuzz, whole animation/texture
suite, original locomotion parity and counter overflow are outside this validation.

## Resume point

Lead joins the outer wrapper around the actual old locomotion/customization/name chain,
with enabled false until pointStarted and source returning the same actor observer.State.
Verify existing prepoint gates, accepted point-driven mode/frame/pose and cancellation
through the main scene. No Runtime71 API is required by this standalone wrapper. A future
provenance-bearing source interface is a separate design question if caller association
needs machine-verifiable enforcement.
