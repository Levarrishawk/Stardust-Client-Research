# Round74 fixed-step presentation host

Date: October6,2026. Question: can the accepted host execute a bounded supplied schedule
as separate fixed before/pump/completion slots while preserving all existing default
calls, ownership and honest failure prefixes? Stop after independent traces, default
regression, scoped validation and sealed handoff for lead integration.

## Inputs and tools

Accepted73 frozen input:4404 files. Manifest SHA-256
`85118db10a4fc52080f514e0262201a8bdedc3200f6c7809b1b2d103e2040f1e`; archive
`3ffc59188b6813da65e21b078f78fc71cbac75142f0787877f0e3a8672c6bbc6`.
Trusted sync preserves HEAD/index/private/unrelated work before edits. Original executable
is hashed only against
`E782BF1C49B3DA730F66B54BC558947C46493F7133F672EED1D8E0C56A6B6150`.
Cached Python/.NET8/Godot4.7.2 .NET x64 and offline packages execute only replacement
scenes. Frozen Core, default host proof and two compact accepted catalog/bank profiles
are staged privately from the verified archive. No original assets are read.

## Findings versus interpretation

Observation: [the scheduled branch](../../poc/Stardust.Godot/SceneWorldPresentationHost.cs)
reads schedule once, bounds0..8, tests optional continue before each slot and executes
fixed BeforeStep, actual Step and completion independently. Four supplied0/1/2/8 counts
agree with pre-native exact traces and yield11 real frozen concrete pump calls. Confidence:
Confirmed bounded replacement behavior. Consequence: caller owns planner/reservation;
zero and discarded endpoint slots do not create presentation frames or refund callbacks.

Observation: receipts commit after Step and before observers; second-before failure leaves
one receipt and second-observer cancellation leaves two. Caught nested Process/Configure,
including Configure(null), and callback mutation fault the outer scheduled frame.
Normal CloseAsync, reparent/ExitTree/free stop without additional slots. Confidence:
Confirmed25 exact independently expected prefixes. Consequence: retain the actual prefix,
not a batch rollback promise, and distinguish successful receipts from session success.

Observation: adding an all-optional five-parameter Configure beside the old overload
failed compilation because existing calls became ambiguous. Interpretation: optional
source compatibility and exact old delegate/CLR signature need an explicit overload
boundary. Confidence: Confirmed compiler result retained privately. Consequence:
retain the exact old two-parameter overload and require beforeStep/schedule explicitly on
the new overload, while fixed seconds and continue remain optional. The corrected actual
legacy delegate invocation preserves variable0.037 delta; unchanged default proof passes.
The [durable contract](../../docs/SCENE_WORLD_FIXED_STEP_PRESENTATION_HOST_CONTRACT.md)
states the final API and modern scheduled-only guards.

Observation: scheduled configuration rechecks owner/parent after foreign drain getter;
six invalid option/getter cases leave candidate close/dispose counts at zero. Accepted
pumps close/drain/dispose once; borrowed callbacks/captured references clear on Close.
Off-thread Process rejects before schedule/pump. Confidence: Confirmed bounded owner
fixtures plus direct cleared-reference implementation. Consequence: keep cancellation
and planner lifetime with callers; no Runtime74 source or CancellationToken API is needed.

## Validation

[Independent producer/verifier](../../poc/round74-tests/Rendering/verify_picker.py)
generates counts, call trace and25 exact failure prefixes before each native run.
[Standalone verification](../../poc/Stardust.Godot/SceneWorldFixedStepHostVerification.cs)
uses a counting/fault-injection wrapper around actual frozen SceneWorldPresentationPump,
with a cold generic world and loader/factory that reject if reached. A wrapper fault
before its inner call is explicitly distinguished from an actual concrete pump call.

Final build: zero warnings/errors;80 headless/81 GPU checks. Four schedule counts,
11 actual positive steps, two fixed interval endpoints, endpoint stop2 of8 and six
configuration rejections pass. Twenty-five prefix cases cover invalid count/delta,
schedule/continue/before/observer caught reentry, callback mutation, source/before/wrapper/
observer failures, normal source/continue/before/pump/observer close, reparent, exit/free,
off-thread, later-slot failure/cancellation and null-configure reentry. Existing default
proof:102 checks,18 pump owners,7 attempted steps,18 closes/disposals,zero asset reads.

Initial incorrect cached-Python launch, test-lambda type error and all-optional overload
ambiguity remain in ignored failed outputs; corrected replays are separate. Private GPU
image was inspected. Eight runner guards, PowerShell syntax, Python/XML/Markdown,
workspace validation, diff whitespace, scope/hash and HEAD/index/private-ignore gates pass.
Exactly one allowed existing host changes;4403 frozen files stay unchanged, with nine
new public files. The offline Godot root-certificate-store warning remains with exit0;
no network or resource-leak diagnostic is observed.

No main scene, shared ledger/registry or unfinished sibling import changed. Cold-pump
evidence does not establish live actor/network throughput, original timing parity or
frame-counter-overflow execution. Callback disposal/clearing is not caller-object disposal.

## Exact next action

Lead supplies its pure planner to the explicit schedule overload, fixed0.01 before/pump
steps and endpoint continue gate in the actual playground. Verify multi-slot process
bursts preserve separate actor/presentation receipts, elapsed-budget HUD time, close
and ingress endpoint prefixes. Keep smoothing, asset publication and gameplay cadence
claims separate from this bounded host scheduler.
