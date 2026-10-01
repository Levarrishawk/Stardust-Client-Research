# 2026-09-30 triangle clearance for one floor step

## Question and boundary

Can one bounded same-cell step preserve supported source rollback when a triangle ceiling blocks lift, while retaining triangle guards on approach and continuation? Stop at authored synthetic rise/drop, ceiling/repeat/retreat, wall and height-policy gates. No new original inputs or executable claims.

## Evidence and consequence

`SingleCellFloorStepTriangleBoxWalker` composes the existing floor step proposal with the [independently verified triangle query](IMPLEMENTATION_CHECKPOINT_2026-09-30_TRIANGLE_FLOOR_PATH.md) and [supported contact adoption](IMPLEMENTATION_CHECKPOINT_2026-09-30_TRIANGLE_FLOOR_WALK.md). It snapshots a bounded triangle set, validates modern policy and permits one step. Source approach must reach the unsupported riser before proposing. Lift/landing are continuously checked before target continuation; failure never publishes a partially raised locator.

Native smoke gates pass a 0.1-unit rise and drop; ceilingY1.85 against actor half-height0.9 holds source feet at(1,0,0.5), reports zero steps and `VerticalStepBlocked`;100 repeats preserve that locator and retreat succeeds. Source wall blocks before a proposal. Target wallX1.4 stops after the step at supportedX≈1.15/Y0.1 on target triangle 3. A 0.21-unit rise exceeds the existing0.2 policy and does not step.

Confidence: Confirmed bounded replacement step guard, using synthetic fixtures and previously independently verified contact math. The [durable contract](../../docs/TRIANGLE_BOX_SWEEP_CONTRACT.md) records failure, budget and adoption policy. No original walls/ceilings, scene physics integration or research-score increase are claimed.

## Next probe

Extend the guarded floor path across one portal with source/target owner transforms, including collision before/after transfer and step rollback. Then identify and justify an explicit original obstacle source before enabling triangle collision in the Mustafar host. Visual render meshes alone remain insufficient proof of legacy obstacle semantics.
