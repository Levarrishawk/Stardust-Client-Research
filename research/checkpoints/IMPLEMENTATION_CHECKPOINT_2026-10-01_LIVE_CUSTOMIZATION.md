# Live character color/style revisions —2026-10-01

## Question and stopping condition

Can already-owned shared color/style assets support rapid live edits without outdated publication, accumulating GPU textures or changing the current pose/actor state? Stop at deterministic worker ordering/cleanup gates and offline standalone/interior control/GPU proofs. Original configuration/object/SAT/scene/material/face/idle pins are unchanged from [shared selection](IMPLEMENTATION_CHECKPOINT_2026-10-01_SHARED_CUSTOMIZATION.md); no new executable-address or protocol claim and no source/archive access during revision composition.

## Implementation and observations

The [durable live contract](../../docs/LIVE_SKELETAL_CUSTOMIZATION_CONTRACT.md) specifies one CPU composer plus one replaceable pending snapshot, cooperative cancellation and revision checks that also reject noncooperative stale completion. Failures transfer explicitly; disposal forbids later publication. Core control ranges derive from owned source cardinalities.

Prototype host exposes labeled color/style controls and reset in both preview hosts. Frame polling updates existing materials after all face uploads prepare, retaining geometry/animation/actor objects. Invalid identities/dimensions/RGBA extents reject; preparation/commit errors restore old bindings. Successful updates release old generated texture cache entries. Original textures remain untouched.

Portable controlled tests pass bounded concurrency/coalescing, snapshot ownership, late/completed stale results, single transfer, failure/recovery and disposal. Offline GPU controls pass rapid newest selection, eight further revisions, invalid snapshot preservation and reset. Owned cache remains11 textures/14,592,672 decoded bytes. Full uploaded pose/material/mesh fingerprint is unchanged per publication; topology/UV/resource identity remains unchanged across requests even while animation advances. Malformed face-pixel and changed factor-identity revisions reject with retained GPU readback intact.

Standalone and interior captures inspected. Prior idle/pose/focus/controls, original multi-door route/guarded closing/actor placement and filtered factor/face readback checks remain green. The initial interior test compared poses across elapsed asynchronous work and failed as animation legitimately advanced; repaired to separate per-commit pose preservation from across-time topology/resource identity. Exact GPU tolerances remain unchanged. No additional original asset was loaded during edits.

## Reproduction, confidence and next action

Use the pinned `-CreatureCustomizationProfile`/`-SceneCharacterCustomizationProfile` preview commands with a fresh ignored capture destination. Native suite includes deterministic revision fixtures; prior original descriptor/interior independent audits remain the source/pixel authority. Final build zero warnings/errors, native suite and workspace/link/diff checks pass. Private logs/captures remain ignored; nothing staged/published.

**Confirmed:** tested CPU queue and observed replacement host ownership/control/GPU behavior. **Explicit prototype policy:** main-thread material updates over the bounded albedo/face preview; full original customization lifecycle and device-loss recovery unproven. Research scores and canonical counts unchanged. Next probe: original MGN blend descriptions and engine-neutral shape deltas, preserving their relationship to skinning and animation.
