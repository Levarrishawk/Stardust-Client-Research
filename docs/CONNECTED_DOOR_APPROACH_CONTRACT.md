# Connected doorway inspection approach

`PortalSingleLeafDoorBundle.ConnectedApproach(distance, stepPolicy)` constructs an optional modern fixture from the selected paired floor edge. It establishes inspection points, not original spawn, navigation, gravity or collision policy. The existing vertical-cast `InspectionApproach` remains available.

## Selection and traversal

Only uniquely paired floor edges on the explicitly selected cell/portal participate. If the portal has several tessellated edges, select the midpoint nearest the authored door translation, then triangle/edge index for deterministic ties. Each owner begins at its own canonical supported edge midpoint and walks along the inward horizontal edge normal by the requested distance (default 0.8 m). The source and target use their own floor topology; no vertical cast can choose an unrelated higher sheet.

The query uses the [bounded step walker](FLOOR_STEP_BOX_CONTRACT.md), empty collider list and zero actor bounds, with at most eight one-step requests. Unsupported or obstructed routes reject explicitly; no partial endpoint is accepted. This proves a connected floor approach only. Actor clearance remains a separate [precise door movement](PRECISE_DOOR_ACTOR_CONTRACT.md) gate. Distances must be finite, positive and at most 100 m; the supplied step policy is validated.

## Evidence and limits

[Original screen and repeated trace](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_CONNECTED_DOOR_APPROACH.md) independently check original identities, selected pairing, edge-normal displacement, continuous floor planes/reciprocal step chords and supported endpoint reuse. The same 782 authored candidates produce 528 constructed approaches, 524 exact short round trips and four leaf contacts. The vertical-inspection local-step screen remains 533 constructed / 509 round trips. Seventeen pass selections are gained and two lost; these are different fixture definitions, not a single increasing coverage metric.

No arbitrary multi-cell loading or original navigation semantics are implied. Whole-strip policy remains the API default; the screen explicitly enables local ray coverage. No collision-free spawn guarantee is made by approach construction.

## 2026-09-30 connected host gate

[Lower-floor host](../research/checkpoints/IMPLEMENTATION_CHECKPOINT_2026-09-30_CONNECTED_DOOR_PROTOTYPE.md) passes forward headless/reverse GPU controls, scheduling/input and reflected placement; 20 manual transfers each, 48/50 manual step events. Explicit `-ConnectedDoorApproach` requires local, step and precise movement.
