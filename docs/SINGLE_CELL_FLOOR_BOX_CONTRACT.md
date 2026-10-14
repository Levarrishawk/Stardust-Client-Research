# Single-cell floor and actor-box movement

## Owned boundary

SingleCellFloorBoxWalker (external historical reference) combines the [piecewise trace](FLOOR_PATH_TRACE_CONTRACT.md) and [box sweep](FLOOR_PATH_BOX_SWEEP_CONTRACT.md). It owns a copied floor and collider list, accepts at most 64 immutable boxes, and uses one shared coordinate space for floor, fixed actor axes and collider poses. This is engine-neutral single-cell movement; it does not transfer cells or advance door controllers.

One query captures each collider's permission once, compares earliest global request parameters and preserves input order on exact ties. Invalid starts have no movement and invoke no permission callback. The query retains no mutable actor state, so a callback exception leaves no partial adopted state.

## Contact landing

A geometric box contact is converted to a float feet target, then re-walked from the validated source through floor adjacency. Landing must be Accepted, reusable by a fresh no-op, within 0.1 mm of the geometric contact and no more than 1 micrometre inside the stopping-face direction. Successful BoxStopped returns that validated floor locator with the original contact parameter. Unknown permission remains visible in the contact status.

If direct rounding fails, bounded candidates move toward the outward face normal in XZ, then backward along the current segment. Each candidate is re-walked and must meet the same strict validation and three-dimensional budget. If none qualifies, ContactLandingUnsupported returns the validated source with parameter zero and diagnostic UnrepresentableBoxContactLanding. It retains the candidate contact for diagnosis. FloorStopped preserves an unsupported/boundary floor prefix. Accepted denotes an unobstructed accepted floor walk. This precision/retreat policy is modern; original narrowphase, sliding and actor update ordering remain Unknown.

At an initial contact (global parameter zero), the validated source is retained exactly. With contact reuse enabled, inward movement within the 0.1 mm rounded-face budget stops at the current position; outward movement retains the existing retreat policy. This conservative start recognition prevents cumulative tangential creep after biased quantization. It can stop a newly supplied near-face start slightly early; it does not create passage clearance.

## Verification

Native tests (external historical reference) verify earliest/tied collision, copied list ownership, once-only policy, repeat stops, inward/outward movement, ramp landing/reverse retreat, traversal limits, invalid starts, callback failure and collider budget. A fixture at coordinates 1,000,000 proves an unrepresentable box landing holds its validated source.

The [independent synthetic verifier](../tools/python/verify_floor_path_boxes.py), with `--with-landings`, checks all 6,000 geometric results plus independently projected float locators, selected triangles, global parameters and fresh-start reuse: **3,102 BoxStopped / 2,898 Accepted**, no unsupported landing in this small-coordinate sample. Synthetic fixture identity remains `1FBCDCC8424605536BCB63EC74BF1482300B0C8410F1D4FA0300C8B0E525E7B7`.

```powershell
dotnet run --no-restore --project poc/Stardust.Core.SmokeTests
python tools/python/verify_floor_path_boxes.py <private-output-directory> poc/Stardust.Core.SmokeTests/bin/Debug/net8.0/Stardust.Core.SmokeTests.dll --with-landings
```

## Original sloped-floor verification — 2026-09-30

Original-floor verifier (external historical reference) selects 600 original floors with accepted multi-triangle sloped requests from the independently audited trace corpus. Obstacles are explicitly synthetic boxes, not recovered floor collision objects. Native trace triangle identities/parameters are inputs from the prior plane/adjacency verification; the collision oracle independently reconstructs plane points, box intervals and float landing constraints. This is not an independent navigator implementation.

Final results: **480 BoxStopped / 120 Accepted**, all 600 locators reusable. Twenty repeated attempts per fixture (**12,000 queries**) have **zero position drift**. Retreat returns all 530 initially clear/non-overlapping fixtures to their source; 70 genuine InitialOverlap fixtures remain BoxStopped. Twenty-eight contacts require bounded approach-side correction, independently checked against triangle planes, strict barycentric membership, contact-distance and stopping-face budgets. The initial implementation held these 28; an intermediate correction showed tangential creep, resolved by the explicit contact-reuse rule. Large-coordinate precision holds remain tested.

Fixture SHA-256 `22483159A8A1158993388E271D6CE1C5A129CE275102C4AAC8EC1E9DF3145D68`; seed 2026093016. Prior walk fixture SHA-256 `2FFC7933774409C5886E6AF26AD34B965F7640C72D74E945DCFCE2DB9510D85C`. The private audited path-output digest is recorded by the verifier for reproducibility. Run it with root, configuration, private walk fixtures, audited native path output, private output and core DLL.

Next: source/target-cell path collision and portal adoption. Existing doorway actors remain level-only; static boxes do not solve moving-leaf overlap or general world collision.
