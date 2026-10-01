# October 1, 2026 — Solid box sphere contact

## Question and stopping condition

Can a continuous sphere query against a captured rigid door/barrier box preserve rounded features and detect deep interior overlap? Stop at the solid-box kernel and independent deterministic agreement.

## Implementation and evidence

`SolidBoxSphereCollider` implements the [durable contract](../../docs/SOLID_BOX_SPHERE_SWEEP_CONTRACT.md), independently authored from geometric distance constraints. It owns captured bounds and pose, validates query bounds before permission shortcuts, partitions the exact local box distance into quadratic intervals and reports earliest closed contact. Interior overlap is detected from solid distance0; surface-only triangle approximation is not used.

Existing rigid pose tolerance remains explicit. This does not establish original runtime actor participation, nor exact affine behavior for approximate rotations. No original executable or external system runs are involved.

## Validation

Full native smoke passes including analytic face/edge/corner/grazing, rounded-corner separation, deep interior overlap, retreat/tangent, permission branches, zero thickness, transform and malformed-input gates.

`verify_solid_box_sphere.py CORE_DLL PRIVATE_OUTPUT` agrees on512 synthetic queries:269 blocked,115 clear,128 initial overlaps; maximum time error6.8833827526759706e-15. Seed2026100113 and fixture SHA-256 `9CCCDE1CB7420EB918E172DA4CE578DDF148C8246E87649549AD4FCC03A1D05E`. Its independent convex minimization/bisection differs from native face-cut quadratic solving. Generated fixtures/native detail remain ignored.

## Confidence, consequence and next action

Confirmed bounded modern kernel and sampled independent agreement. This supplies a sphere primitive for original authored door boxes while preserving solid interior behavior. No complete actor solver claim, canonical names or metric change.

Next: precise floor-path and one-transfer adapter against immutable captured boxes, then connect directional permission barriers and physical door leaves.
