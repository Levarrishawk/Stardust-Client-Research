# Exact-current raw speed admission

Round50, October 4, 2026. This is explicit modern planar admission over the
[frozen49 current-input view](SCENE_WORLD_CREATURE_MOVEMENT_CURRENT_INPUT_CONTRACT.md).
It does not recover original movement arbitration, modifiers, vehicle behavior,
angle conversion or final runtime speed.

## API and exact binding

[SceneWorldExplicitMovementAdmission](../../SWG-Godot-Client/poc/Stardust.Core/Session/SceneWorldExplicitMovementAdmission.cs)
extends its fixed enum with `CurrentField7 = 5` and `CurrentField11 = 6`; Invalid0
and old sources1..4 retain their values and behavior. A trailing optional
`SceneWorldCreatureMovementCurrentInputs? currentInputs` constructor argument is
borrowed. Current modes require it and reject a conflicting direct received
reader. Noncurrent modes reject an unused current-reader argument. Existing
legacy authored calls that supplied their ignored optional direct reader remain
accepted for API compatibility; no new mode uses that ignored argument.

Before owned driver construction, the adapter reads and validates an exact-current
view: same immutable authored-root instance, matching independent root SHA pin,
same world registry object/generation/creation incarnation, finalized operational
typed CREO3 and issuing-reader currency. Same bytes/SHA in a separately allocated
root are insufficient. Foreign world/reader/root, missing typed state, pending
phase, closed/stale reader and conflicting arguments reject before the driver's
initial local publication. Binding still carries the frozen caller catalog trust:
normalized root path CRC is checked, but does not prove the world's private
catalog SHA/recipe. No frozen coordinator or reader API is edited or reflected
into for production binding.

## Fresh capture and selected field

`Submit(timestamp, normalizedNativeXZ)` for a current mode captures a fresh view
internally each time. A direct received receipt supplied to these modes throws.
Only field7 or field11, according to the fixed choice, can supply speed. It must
have `FiniteCurrent` eligibility, a present finite nonnegative F32 word, and exact
operational context. Existing direction tolerance0.000001, native XZ mapping,
per-component velocity magnitude10, timestamp/order/horizon and pending capacity
remain unchanged. Speed is not clamped, normalized or substituted. Other current
slots, including literal-one fields, slope/angle/water inputs and the unselected
speed do not enter velocity arithmetic.

The frozen derived reader determines presence: absent received data can expose
explicit constructor authored raw input; present received zero, signed zero,
NaN or infinity always overrides that observation. This adapter does not add
fallback. Present nonfinite values reject, negative finite values reject, and
missing/unresolved authored values remain ineligible. Finite subnormal values
retain their exact bits even when downstream binary32 displacement underflows.

The immutable receipt adds nullable `CurrentSource`. Current modes retain the
full current receipt, ten-slot origin/evidence, exact target and root pin. Their
`AuthoredSelection` and direct `ReceivedSource` remain null: derived origin is
reported through CurrentSource, avoiding a fabricated direct-reader receipt.
Old mode receipts have null CurrentSource. Context and reader currency are
rechecked immediately before the sole driver Submit. Rejected eligibility or
time/order/capacity admits no receipt/input prefix and does not change driver
state or local publication.

`IsSourceCurrent` additionally checks the captured current view. Late first
partial publication (even listen-only), typed publication, containment, later
partial revision, recreation, generation change and close invalidate relevant
historical receipts. Fresh submissions can capture a newly current compatible
context. An already queued velocity remains captured; source changes never
retroactively rewrite it. Borrowed reader disposal throws on attempted sampling,
without inventing bootstrap values or admitting a prefix.

`SubmitStop` stays independent: it samples neither direct nor current reader and
admits exact zero without a selected-speed provenance claim. It works after
borrowed current-reader disposal if the selected world incarnation is current,
and retains existing time/order/capacity gates. Dispose closes the owned driver
and clears borrowed references, but never disposes the current reader or world.

## Unchanged Godot caller and integration

The [frozen sourced Node](../../SWG-Godot-Client/poc/Stardust.Godot/SceneWorldSourcedInputHost.cs) is
byte-identical. Configure recognizes new choices as non-direct-received modes:
construct admission with its current reader, then call `host.Configure(admission)`
without passing a direct received reader or generation to the Node. Its existing
Issue supplies null direct receipt; the admission gathers its current view.
Physical WASD, fixed choice, source-unavailable stop/suspension, restoration,
focus/pause, clock, ownership and teardown policies are unchanged. The Node
implementation is linked into the proof, not copied or modified.

Lead hooks: use the exact authored snapshot retained by the actual original
resource publication to construct both current reader and admission; pass the
same trusted root pin and current object. Transfer only admission to the existing
Node. Caller retains current-reader lifetime, routes existing Advance/focus/pause
and gates presentation on unavailable/inactive state. Historical motion-store
Speed may remain until a tick, as specified by the [frozen host contract](SCENE_WORLD_SOURCED_INPUT_HOST_CONTRACT.md).
No pending round50 Assets API, shared preview or project edit is needed here.

## Evidence and limits

[Native scenarios](../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/SceneWorldCurrentMovementAdmissionScenarios.cs)
pass126 sequences/1134 states/5471 checks; the
[independent synthetic peer](../../SWG-Godot-Client/poc/round50-tests/Runtime/peer.py) passes8112
comparisons across seven authored profiles, both choices, three directions and
three render partitions. Presence transitions include bootstrap, first listen,
valid received, zero, signed zero, nonfinite, negative, component overflow,
subnormal and restoration. Negatives cover exact root/world/reader binding,
pending/missing typed state, registry/typed/partial currency, time/order/caps,
owner, disposal and source-free stop.

Fresh SHA-gated static/original-asset oracle passes50 loader anchors and the
necessary five-source8290-byte chain. Six original-authored/synthetic-received
native sequences cover54 states; [independent original raw checks](../../SWG-Godot-Client/poc/round50-tests/Runtime/verify_original.py)
pass275 comparisons including all five source identities. Detailed original
words, paths and chain output stay private; new public fixtures are synthetic.

[Actual Godot proof](../../SWG-Godot-Client/poc/round50-tests/Runtime/Godot/CurrentHostProof.cs) passes
10 cases/54 checks; [independent trajectory checks](../../SWG-Godot-Client/poc/round50-tests/Runtime/verify_godot.py)
pass20 comparisons. It drives both choices through native key dispatch and proves
late received, nonfinite stop/no fallback, restoration, zero provenance,
focus/pause, foreign owner, closed reader/world, generation and reentry cleanup.
No node-leak warning occurs. Installed Godot retains its environment certificate
store error; no network/TLS connection is used.

Frozen48 admission,49 stop/Node,47 received and default Core smoke pass; successful
builds have zero warnings/errors. Confidence is Confirmed for this bounded modern
behavior and pinned raw input identity. Original effective speed and controller
parity remain Unknown. The [checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-10-04_R50_CURRENT_MOVEMENT_ADMISSION.md)
records reproduction, exact scope and the next lead integration action.
