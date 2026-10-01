# 2026-09-30 supported floor stops against triangles

## Question and stopping condition

Can the precise triangle contact query yield a reusable supported same-cell floor locator, with stable repeat/retreat and conservative rollback when float quantization cannot represent contact? Stop at authored synthetic flat/ramp/ceiling cases and failure gates. Reuse the [independent contact oracle](IMPLEMENTATION_CHECKPOINT_2026-09-30_TRIANGLE_FLOOR_PATH.md); no original obstacle input is selected.

## Observations and consequence

`SingleCellFloorTriangleBoxWalker` composes owned floor/triangle queries with the existing bounded floor-contact landing resolver. Its additional gate rechecks both stationary body overlap and the entire candidate floor path using precise centers. This prevents float locator adoption across another obstacle. Contact at parameter 0 retains the supported source. Unrepresentable landing returns explicit `ContactLandingUnsupported` and source parameter 0.

Native smoke tests pass wall stop at X≈0.9 on the flat source, a stopped ramp locator satisfyingY=X+Z−2,100 unchanged repeat requests, flat/ramp retreat, and rising-floor ceiling stopY≈0.55. A fixture near coordinate999,998 forces a contact outside the float landing tolerance and confirms source rollback. Empty-obstacle slope traversal, invalid start, crossing limit, negative half extent and stateless recovery pass.

Confidence: Confirmed bounded replacement same-cell contact adoption. The [durable contract](../../docs/TRIANGLE_BOX_SWEEP_CONTRACT.md) records tolerance, bias and failure policy. Original response fidelity is unknown; this does not add collision to the original Mustafar host. Steps, portals, dynamic geometry and sliding remain open. Research scores and canonical registries unchanged.

## Next action

Extend this triangle guard to the bounded step proposal: check source approach, vertical lift and target continuation; hold a supported source on ceiling contact and verify repeat/retreat. Preserve original-source selection as a separate evidence decision before host integration.
