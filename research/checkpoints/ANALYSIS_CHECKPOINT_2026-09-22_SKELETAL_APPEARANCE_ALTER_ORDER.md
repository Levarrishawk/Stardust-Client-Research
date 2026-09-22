# Skeletal appearance alter and animation-event order

Date: 2026-09-22

## Scope

Static reconstruction of the per-frame path from `SkeletalAppearance2::alter`
through its skeleton/controller aggregate, physical animation tracks, root-motion
collection, animation-message emission, transition replacement, LOD readiness,
and dormant-detail eviction.

## Confirmed order

`SkeletalAppearance2` vtable `0x015E529C` identifies `0x007C8AD0` as its alter
slot. For each call it performs this order:

1. `0x007D7790` clears the per-transform evaluated flags and iterates the
   aggregate's ordered controller list, calling wrapper `0x007D69F0` for each.
2. That wrapper calls virtual slot `+0x04` on the retained animation controller.
   For `TrackAnimationController`, the slot is `0x0082F940`.
3. `0x0082F940` resolves the distinguished logical/root physical track and calls
   physical-track alter `0x0083E630(delta, true)`.
4. The physical track resets its accumulated locomotion quaternion and
   translation to identity/zero, then advances its active `SkeletalAnimation`.
5. After each positive-time substep it extracts the animation's locomotion
   rotation/translation, accumulates them, and calls `0x00830170` when message
   delivery is enabled.
6. `0x00830170` enumerates animation messages, maps their indices to identifiers,
   optionally records diagnostics, and invokes every registered controller
   listener synchronously.
7. Only after extraction and message fan-out does `0x0083E630` retain/install a
   returned replacement animation or process completion/queued-change state.
8. Back in `SkeletalAppearance2::alter`, `0x007CBE80` advances the appearance's
   readiness/fade state unless it is already terminal state 3.
9. A randomized periodic timer invokes `0x007CBCC0`, which evicts sufficiently
   old, inactive generated-detail resources when no retained child blocks it.
10. The appearance returns the engine's ordinary next-frame alter sentinel.

This establishes a strict compatibility point: messages raised by an outgoing
animation substep are observed before its replacement becomes the active
animation. Listener fan-out is not deferred to rendering or a later frame.

## Aggregate transform cache

The object updated by `0x007D7790` owns an ordered controller vector, cached
quaternion and translation arrays, one evaluated byte per transform, and the
last delta. `0x007D7510` lazily evaluates a transform on first request during a
frame and caches it; `0x007D7610` can explicitly seed/replace a cached pose.
When no controller data exists, the lookup returns identity rotation and a
scaled fallback translation. This aggregate is the boundary between multiple
skeleton/controller contributors and the final pose queried by the appearance.

## Outer-frame placement

The previously confirmed frame schedule updates platform, network, simulation
and scene state before rendering. This block proves the complete order inside
the skeletal appearance alter call. Static indirect dispatch does not by itself
prove the exact position of every individual appearance within the global scene
update, so no finer cross-object ordering is claimed.

## Transition/readiness and resource maintenance

`0x007CBE80` transitions state 1 to 2 only after the selected detail can be
prepared, then advances a one-second accumulator before state 3. `0x007CBCC0`
is not animation evaluation: it periodically inspects generated detail vectors,
releases inactive eligible resources, marks affected detail state dirty, and
invalidates the current detail when it evicts that level.

## Replacement-client requirements

- Advance controllers before pose queries for the frame and invalidate the
  transform cache at the beginning of the skeletal alter.
- Accumulate root motion per substep rather than sampling it only once after a
  possibly multi-transition update.
- Deliver animation messages synchronously after that substep's root motion and
  before installing the returned/replacement animation.
- Preserve deterministic controller and listener order.
- Keep LOD readiness/fade and inactive-detail eviction separate from animation
  graph evaluation, even if a host engine exposes them through one node.

## Confidence

- Function addresses, call order, cache invalidation, substep root-motion
  extraction, synchronous message fan-out, replacement ordering, readiness
  update and periodic eviction: **confirmed by code**.
- Friendly label "root physical track" for the distinguished logical mapping:
  **strongly inferred**, consistent with installed `logical_root` vocabulary
  and the prior controller analysis.
- Exact scene-wide ordering among separate appearances: **unknown** and not
  required to reproduce the local controller contract.

