# 2026-09-30 bounded step collision and continuation

## Question and stop

Can a bounded riser proposal be consumed only after actor-sized vertical clearance, and can every blocked/accepted result remain owned by a supported floor? Close after deterministic ceiling/blocker/permission/resource tests and original floor movement/repeat checks.

## Evidence

The [step proposal checkpoint](IMPLEMENTATION_CHECKPOINT_2026-09-30_FLOOR_STEP_PROPOSAL.md) pins the original inputs and fixture SHA-256 `EC00D231C5A85B657F2278056BC52FBC4A00383B099492D8C6988E54D15FC289`. The modern [step box walker](../../docs/FLOOR_STEP_BOX_CONTRACT.md) uses the existing independent box-interval and original plane/adjacency verifier. Synthetic blockers are labeled; original-derived detailed output stays private.

## Confirmed results

- Core smoke passes clear rise/drop, 200 repeated transitions, low ceiling, 100 zero-drift blocked repeats, retreat, unknown/passable clearance, post-step target contact and mapped request progress.
- Invalid starts have zero callbacks; one current decision per box is reused through prefix, vertical travel and continuation. Combined traversal and constructor policy limits reject explicitly.
- Nine original accepted proposals pass 36 independently checked movement/blocker cases. Ten broader strips remain rejected. Blocked outputs remain on the source floor; no mid-rise point becomes a locator.
- Nine original floors complete 20 round trips each: 360 step transitions, reusable results and return distance at most 0.1 mm.

Confidence: Confirmed bounded modern algorithm. Original step height, gravity, world collision, moving leaf and cross-cell integration remain Unknown/separate. Door screen still reports 496 short round trips; this query is not yet integrated into that actor.

## Next action

Adapt bounded steps into precise portal/door movement under captured world-space colliders and directional permissions. Start with original small-house thresholds, then verify both owner poses, reverse ownership, repeated stops and low-ceiling rollback before exposing a host option.
