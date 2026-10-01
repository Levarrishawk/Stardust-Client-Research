# One-step floor movement with box clearance

[SingleCellFloorStepBoxWalker](../poc/Stardust.Core/Assets/SingleCellFloorStepBoxWalker.cs) combines strict floor walking, the [bounded riser proposal](FLOOR_STEP_PROPOSAL_CONTRACT.md), actor-sized box clearance and supported-floor continuation. It permits one modern step per same-space request. It does not transfer cells or replace the default door actor movement.

## Sequence and ownership

1. Validate actor bounds and source floor request; invalid starts invoke no permission callback.
2. Capture each immutable box's current passage decision once for the whole request. First sweep the original strict floor prefix. A physical contact on that prefix takes precedence over a later step.
3. At an UnsupportedSurface vertical boundary, require an accepted bounded step proposal and available combined traversal budget.
4. Sweep the actor box along the full vertical rise/drop and the bounded float landing bridge. Any blocked/unknown contact retains the supported source floor contact and horizontal prefix progress. The diagnostic is VerticalStepBlocked; no floating Y becomes a locator.
5. Continue on the target floor using the same box decisions. A target-only initial overlap retains the source floor. A later horizontal box contact may adopt the target floor, with parameter mapped back to original horizontal request progress.

Crossing accounting includes the original prefix, vertical-strip triangles, landing and horizontal continuation. Boxes remain conservative rigid snapshots; this is not continuous moving-leaf collision. Slopes are allowed on supported portions, but all coordinates/actor bounds are in the same space. World-axis bounds under unrelated owner frames require a separate adapter.

## Bounded acceptance

Native synthetic gates pass clear rise/drop, 200 repeated step transitions, low-ceiling rollback, 100 repeat holds without drift, source retreat, unknown/passable clearance, post-step target contact/global progress, invalid inputs and traversal limits.

[Independent installed checks](../tools/python/verify_floor_step_proposals.py) with --with-movement preserve nine accepted proposals and ten explicit strip rejections across 19 original boundaries. On the nine accepted original floors, **36** clear/blocked/unknown/passable cases pass independently checked planes and vertical box intervals. Blocked repeats have zero drift. Nine floors × 20 round trips also pass **360 step transitions** with reusable output and return error at most 0.1 mm. Original floors are paired with explicitly synthetic blockers; these counts do not establish complete original world collision.

## Next boundary

Integrate the one-step behavior with precise source/target portal paths, physical leaf snapshots and directional permission. Preserve source/target actor ownership, one permission decision per link, world actor bounds and rollback during vertical collision. Original step policy remains Unknown; declared limits are modern choices.
