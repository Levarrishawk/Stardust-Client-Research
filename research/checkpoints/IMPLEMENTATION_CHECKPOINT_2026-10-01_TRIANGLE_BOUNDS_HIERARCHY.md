# October 1: conservative triangle bounds hierarchy

## Question and stopping condition

Can static triangle queries scale beyond the2,048 default without changing contact/ownership policy or truncating obstacles? Close on conservative spatial pruning, exact exhaustive agreement, resource rejection and unchanged original hall results. Full room activation is the next separate gate.

## Implementation

`StaticTriangleBoxCollider` now builds an owned binary bounds hierarchy over prepared triangles. Leaves contain at most8 entries. Largest-axis midpoint ordering and source-ID tie breaks make construction deterministic. Swept body AABB uses double endpoints, outward rounding and conservative1e-9 broad-phase padding. Node and triangle AABBs can reject only candidates; existing SAT remains the exact narrow phase. Candidate indices are restored to source-ID order before contact tests, preserving equal-time contact selection. No candidate cap silently drops geometry.

`SweepWithStatistics` reports node visits, candidate triangles and narrow-phase triangles per query without mutable shared counters. Explicit `useBoundsIndex:false` supplies exhaustive comparison. Defaults retain2,048 accepted triangles; callers may explicitly request at most65,536. Same-cell, step, portal and indexed extraction APIs accept this capacity. Portal capacity is a total across owners. The existing262,144 complete-path/source-triangle pair limit remains conservative and is checked before contact. Larger geometry does not imply unbounded floor traces. Same-cell step and continuation now share one owned prepared collider.

## Validation

512 deterministic native varied queries against128 arbitrary triangles match exhaustive contact records exactly, including status, time, ID and normal;284 produce contact. Broad phase passes8,445 candidates versus65,536 exhaustive triangle tests. A3,072-triangle grid (explicitly beyond default capacity) admits one candidate/19 node visits for its selected sweep and matches exhaustive contact. Ownership mutation, equal-time ordering, distant near-boundary, default/maximum resource and invalid capacity cases pass. These are work counts, not a performance benchmark.

Full native smoke and Godot build pass. Original hall extraction/16 requests/1,600 stable holds/16 retreats plus independent raw mesh/floor/LP comparison remain passing after the hierarchy change. Source pins and canonical geometry hash are unchanged; see the [original hall checkpoint](IMPLEMENTATION_CHECKPOINT_2026-10-01_ORIGINAL_HALL_COLLISION.md).

## Confidence and consequence

**Confirmed by execution:** conservative pruning for tested analytic/random/exhaustive boundaries, owned data and explicit larger capacity. Narrow-phase LP proof and modern feet-clearance semantics are inherited; no original acceleration algorithm or actor policy claim. No semantic addresses added;2,599 names,345 packet rows and research scores89%/94% unchanged.

Reproduce through the full native smoke suite (`TriangleBoundsScenarios`) and the original hall verifier described in its checkpoint. Next: load the two complete original20,501-triangle room meshes with the hall, account for every eligible/nondegenerate triangle and exercise supported original portal transitions under the declared modern triangle policy.
