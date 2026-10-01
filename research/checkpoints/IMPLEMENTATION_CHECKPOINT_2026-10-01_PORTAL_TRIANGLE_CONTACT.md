# October 1: supported triangle contact across a portal

## Question and scope

Can the replacement triangle guard preserve floor support and owner identity over one portal transfer, including target geometry reached before the feet seam? Stop at bounded native cases and an independent synthetic contact/ownership comparison. This is modern volume movement policy, separate from [original segment eligibility](ANALYSIS_CHECKPOINT_2026-10-01_MESH_COLLISION_SELECTION.md).

## Implementation

`PortalFloorTriangleBoxWalker` snapshots cell-local triangles, transforms them into world space, and reuses precise floor tracing, triangle SAT and supported contact landing. All owned colliders participate on source and target legs. Target triangles can stop the body before its feet cross. Successful target contact retains target ownership and maps its leg parameter into whole-request progress. Unrepresentable contact holds the validated source; target zero-progress seam contact rolls back to supported source contact. Only validated floor locators are adopted. Initial overlap holds source and reports contact; it does not claim that source is physically clear.

Triangle IDs are unique within an owner; owners are traversed in ascending key order, then triangle ID, with chronological leg order. Limits remain2,048 total triangles and262,144 original segment/triangle pairs per query; one transfer, no step or sliding. Complete transformed paths/body centers are validated even with empty obstacle sets. Collision checks are independent of rendering visibility and current owning cell. External permission callbacks cannot mutate owned triangle data.

## Evidence and validation

Native tests pass clear slope transfer, target wall stopping source-side feet, target slope contact/global progress,100 stable repeated holds, retreat and reverse owner transfer, denied entry, shared rotated/translated pose, asymmetric target floor/triangle pose, caller mutation and malformed owner/resource/body/callback gates. Existing native smoke suite passes.

`tools/python/verify_portal_floor_triangles.py` uses separately calculated slope/seam ownership plus three-variable LP contact against unchanged synthetic float32 geometry. It covers512 requests/eight owner poses, both movement directions, allowed/denied entry, empty geometry and initial contact. Results:179 accepted,255 box-stopped,78 floor-stopped;241 zero-transfer and271 one-transfer results. Maximum contact-parameter difference3.3306690738754696e-16. Fixture SHA `6EC246844DB563C8B19773F16DA01AF8B0BE8AA3F525028B31B8B7318FE62928`. The first verifier run required normalizing native JSON decimal output back to float32 for exact source-hold comparisons; core behavior was unchanged by that correction.

Reproduce by building `poc/Stardust.Core.SmokeTests` and running the Python verifier with an ignored output directory and its built DLL. Native probe `--portal-floor-triangles` requires the generated fixture SHA. Synthetic proprietary-free fixture output remains ignored. No original executable addresses added; research scores,2,599 names and345 packets unchanged.

## Confidence and compatibility consequence

**Confirmed by execution:** supported owner/contact behavior for this bounded replacement slice and independent synthetic cases. Original controller ordering, actor selector and tolerances remain Unknown. Original scene geometry and persistent door composition have not yet been activated. World-axis body bounds are a declared replacement policy. Full room triangle sets exceed current resources.

Next action: extract the pinned original hall6 eligible indexed geometry, account explicitly for degenerate triangles, and validate a floor-supported collision stop against an original wall. Then add conservative acceleration before wider room integration.
