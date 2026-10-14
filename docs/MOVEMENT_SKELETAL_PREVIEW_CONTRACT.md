# Movement-linked skeletal preview contract

The selected original interior can opt into `-SceneCharacterLocomotionProfile poc/preview-profiles/human-locomotion.json` alongside its pinned idle animation. The [owned animation bank](SKELETAL_ANIMATION_BANK_CONTRACT.md) resolves original LATT/explicit SSAT/SPAT/PXAT/CKAT references inside the CPU worker. Archives close before the retained prototype host character is published. Cancellation publishes no scene/GPU content.

## Explicit presentation policy

`LocomotionPreviewClock` is engine-neutral and deliberately modern. Constructor requires finite clip frame/end/scale values, positive authored walk/run LOCT speeds and an explicit run threshold. Interior uses3 position-units/second; selected input walk/run speeds are1.6/5.2 m/s, with a Run speed checkbox. These are preview choices, not recovered original speed thresholds.

Every successful simulation tick supplies elapsed seconds and accepted world-position difference, after collision/door/floor transfer. Project displacement to world XZ. At distance <=1e-6 select idle; otherwise accepted speed >=threshold selects run, below selects walk. Idle phase increment is seconds*FPS*proxyScale/endFrame. Movement increment is distance*FPS/(authoredLOCTspeed*endFrame), making stride phase depend on traveled distance. Preserve shared normalized phase across switches; wrap modulo1. Movement time scale cancels under this explicit distance policy. Preserve last nonzero accepted heading. Zero-duration movement, nonfinite/bounded-invalid inputs and invalid clips fail before clock mutation.

A suspended animation clock retains its state. The existing fixed-step timeline controls focus/simulation suspension and discarded catch-up time; absent ticks contribute no locomotion phase. Separate accepted opposite steps contribute their lengths, even if aggregate displacement is zero. Manual step buttons call the same tick wrapper. This is sampled presentation: no MSGS callbacks or LOCT root delta drives actor position. The subsequent [pose transition layer](SKELETAL_POSE_TRANSITION_CONTRACT.md) smooths target changes with explicit0.15-second blends.

## Prototype host boundary

`SetClipFrame` samples idle or an owned named additional animation with the same binding/deformed selected shape. Validate the complete CPU pose before updating surfaces; retain ArrayMesh/material identities, topology, skin maps, textures and generated face. Apply only visual yaw atan2(-headingX,headingZ), verified by transforming prototype host Forward against reflected authored +Z. Root position remains the actor's authoritative world feet. Clip updates preserve actor state and all doors.

Full original interior closure147 sources/16,611,843 bytes adds only two clips/13,352 bytes to the previous145-source material scene. Character texture cache remains14/18,786,972 decoded/uploaded bytes. No resource lookup occurs during playback or live controls.

## Verification

Synthetic core fixtures cover gait/heading/shared phase, accepted distance subdivision, opposite steps, blocked idle, suspension/reset and transactional rejection. Native original bank prepares222 movement poses and independently audits source/routes/keys; original interior checker retains2880 ticks/24 transfers/182 contacts. Prototype host passes480 retained clip updates, all three modes on accepted route ticks, pause/reset, reflected headings, invalid key/frame preservation, original door/actor route, color/shape/age control checks and20 specular/3 normal/3 albedo GPU fixtures. Headless and pending first-lookup cancellation pass; capture inspected.

Unknown: original graph speed blending, transition duration/curve, callbacks/footstep effects, full legacy light selection and LIT compilation. The pose transition layer is complete; legacy graph equivalence remains unclaimed.
