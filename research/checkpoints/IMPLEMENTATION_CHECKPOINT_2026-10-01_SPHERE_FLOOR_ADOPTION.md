# October 1, 2026 — Sphere floor adoption and original hall

## Question and stopping condition

Can precise floor paths retain the recovered sphere offset and adopt a reusable supported contact locator? Stop at same-cell floor adoption and the existing unchanged hall corpus; portal composition follows separately.

## Implementation and evidence

`FloorPathTriangleSphereSweep` snapshots and validates the complete path before querying, preserves double feet coordinates and global progress, and adds all three explicit center-offset components. Positive radius is bounded to100; paths allow4,097 segments and262,144 source-triangle segment work. `SingleCellFloorTriangleSphereWalker` reuses the established supported contact landing policy and checks both stationary clearance and the candidate path. Unsupported float locator adoption holds the source.

Native synthetic gates cover flat and sloped contact, a rising-path ceiling,100 stable repeats, retreat, clear slopes, distant precision rollback, invalid starts, traversal prefixes, center offsets, malformed later segments and work bounds. These are modern compatibility probes, not evidence of original controller reachability.

`--original-hall-sphere` loads the unchanged POB through the configured overlay and original appearance/material chain, extracts1,984 triangles and reads the original FLOR. Explicit center offset(0,1,0), radii0.5 and0.65 yield16 floor/portal boundary stops and zero triangle stops,1,600 stable repeats and16 accepted retreats. Radius0.5 matches the recovered human fallback;0.65 is a declared sensitivity probe. No feet-clearance adjustment is used.

Independent `verify_original_hall_sphere.py` checks source pins, geometry hash,72 floor-plane samples and224 convex-distance queries: all16 results agree, maximum contact parameter error0 because this sample contains no triangle hits. Synthetic contact gates provide positive hit coverage. Original hall mesh SHA-256 `8050A71B294AFEC1DD9845062C006A459C726FABDA2549CE52EA3A5BABD3E74B`; FLOR `42D9ADF10068796F6113D9371BBCDCF6DEF17B4027D96BF5063F61C39814EFEA`; canonical geometry `0BB4237A347FA1ED88D83BF4DCC4A66E2ED9BB26F2E4E00A0D82007DB10D02C0`.

## Confidence and compatibility consequences

Confirmed bounded implementation and original sample agreement. Raised-sphere geometry changes which obstacles are touched, but this result does not establish original mesh participation, body orientation or controller completeness. Preview actor remains unchanged. The human profile's collision height is not substituted for sphere-center height.

## Reproduction and next action

Run full native smoke and its pinned original hall flag, then `verify_original_hall_sphere.py ROOT CONFIGURATION NATIVE_OUTPUT --configuration-sha256 HASH`. Configuration and POB pins are those in the [original room checkpoint](IMPLEMENTATION_CHECKPOINT_2026-10-01_COMPLETE_ROOM_TRIANGLES.md). Original/generated detail stays ignored; no new names or score changes.

Next: compose the same explicit sphere offset with the existing one-transfer floor trace and query all selected original rooms, including the two0.1 m render ramps.
