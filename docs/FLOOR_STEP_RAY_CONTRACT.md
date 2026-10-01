# Local vertical coverage for modern floor steps

`FloorStepPolicy(UseLocalRayCoverage: true)` is a separate explicit option on the [riser proposal](FLOOR_STEP_PROPOSAL_CONTRACT.md). The existing whole-component policy remains the default. This option follows only reciprocal solid vertical faces intersecting the contact's vertical line, then requires coverage of the complete source-to-target height interval. Distant attached walls cannot expand the local strip traversal; a connected route around a gap cannot establish direct vertical coverage.

## Geometry and rejection

The native query derives a line/triangle interval from three-dimensional barycentric inequalities and a bounded plane-distance condition. Plane distance is at most the smaller of configured strip width and **10 micrometres**; vertical coverage gaps above **one micrometre** reject. Height, reciprocal passable edges, solid membership, no portals, triangle count, supported target containment and canonical landing budgets remain unchanged. No global floor start tolerance changes.

The [independent verifier](../tools/python/verify_floor_step_proposals.py) uses constant-horizontal-coordinate triangle-edge chord cuts in a vertical projection, rather than the native Gram-matrix calculation. It independently checks source edge intersection, target plane/containment, strip connectivity and coverage union. This is modern step policy, not proof of original traversal tolerances.

## Bounded results

On the unchanged 19 original boundary fixtures, local coverage yields **16 Accepted / three NoLanding**, compared with historical whole-strip nine Accepted / ten StripLimit. All 16 accepted landings have independent source/target/chord checks. The remaining failures are hall4, elevator2 and hall5 black-sun floors; no usable contained supported landing is forced.

Original floors with synthetic blockers pass **64** clear/blocked/unknown/passable movement cases and **640** repeated step transitions. Repeated blocked attempts have zero drift. A tiny angled-edge repeat jitter motivated an owned hold policy: with separating-contact mode, no floor crossing/transfer and a source already within 0.1 mm of the blocked boundary, retain the canonical source rather than author another rounded edge point. Retreat and clear steps remain available. This source hold is conservative and explicitly bounded.

Synthetic gates preserve whole-strip side-wall rejection, accept the local ray through the same valid riser and reject a reciprocal connected holed strip. Core smoke passes. Portal/door actor screening under the local option is the next separate gate; the 504 step-enabled doorway count still uses the existing whole-strip policy.

## World doorway actor gate

[Original black-sun threshold](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_RAY_STEP_DOOR_ACTOR.md) passes both starting directions/two poses: 1,896 independent ticks / 88 transfers / 90 step events / 56 blocked. The same explicit local-step screen completes 509 short round trips with 24 Accepted non-round-trips. Historical whole-strip counts stay separate; host local-option gate is next.

## Host option

The explicit `-LocalStepDoorWalk` launcher flag requires step and precise modes. Pinned black-sun forward headless/reverse GPU checks pass, including controls, timing/input and reflected placement; manual transfers are 20 each and step events 23/25. See the [host checkpoint](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_RAY_STEP_DOOR_GODOT.md).
