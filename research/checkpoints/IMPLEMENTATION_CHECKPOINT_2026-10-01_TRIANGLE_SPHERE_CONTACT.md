# October 1, 2026 — Continuous triangle sphere contact

## Question and stopping condition

Can an owned translating sphere produce continuous face/edge/vertex contact independently validated over deterministic fixtures? Stop at the kernel and numerical agreement; actor-host adoption and original participation are separate.

## Implementation

`StaticTriangleSphereCollider` adds the modern query specified in the [durable contract](../../docs/TRIANGLE_SPHERE_SWEEP_CONTRACT.md). It preserves original source IDs, owned geometry, exact tie ordering, positive radius bounds and capacity. Conservative linear AABB filtering is independently switchable; it does not approximate the rounded contact surface. No original executable or server runs are involved.

Motivation comes from the [original object-root profile](IMPLEMENTATION_CHECKPOINT_2026-10-01_ORIGINAL_CREATURE_TEMPLATE_PROFILE.md): the recovered local fallback sphere has a different offset from the existing preview box. This milestone does not replace the preview box or assert that original actor queries use selected mesh triangles.

## Evidence and validation

Native analytic gates verify face, edge, vertex, grazing, overlap/touch, outward retreat, rounded corner, skinny face, owned mutation, stable IDs and invalid resources/queries.512 varied bounds-filtered queries match exhaustive records exactly, with29 contacts and101 candidates of65,536.

Independent convex-distance minimization and bisection agree on512 synthetic cases:249 moving contacts,134 clear and129 initial overlaps; max time error1.5543122344752192e-15 and max normal component error5.7060532077102266e-08. Fixture SHA-256 `5004CACF5D213A2A7F365BA40285A540217DCD8326B55452EE2173445EEC1C97`; seed2026100110. Generated cases/native detail remain ignored.

Reproduce with `verify_triangle_sphere_sweeps.py CORE_SMOKE_DLL PRIVATE_OUTPUT_DIRECTORY`. Full native smoke passes. Confidence: Confirmed bounded modern geometry and sampled independent agreement, not recovered original solver behavior. Documentation/link validation follows promotion.

## Next action

Add precise floor-path sphere sweep and supported contact adoption using the resolved profile offset. Then test the same original hall and room route. No canonical name or research/protocol score change.
