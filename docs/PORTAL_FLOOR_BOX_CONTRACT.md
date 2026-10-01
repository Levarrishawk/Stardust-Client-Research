# Precise one-transfer floor and actor-box query

## Supported seam

[PortalFloorBoxWalker](../poc/Stardust.Core/Assets/PortalFloorBoxWalker.cs) combines the existing paired-floor transfer with precise three-dimensional source and target paths. Floor geometry and finite rigid yaw owner poses are owned snapshots. Each request copies at most 64 immutable world-space box records, keyed by cell and caller ID, before invoking external entry policy. Actor axes remain world-aligned; support bounds are transformed into each box frame in double.

Boxes from both participating cells are swept along both floor legs. A target-cell box can therefore stop the actor while its feet still belong to the source. Each leg's contact parameter is mapped into the original request; target progress is sourceParameter + (1-sourceParameter) × targetParameter. Earliest contact wins; exact ties preserve source-leg then caller-box order. Unrelated-cell boxes are ignored after validation. Box passability and portal entry/barrier permission remain separate inputs.

Confidence: Confirmed deterministic synthetic seam/slope fixtures and independent transformed-world reference. This is a replacement policy, not recovered original actor ordering. Feet-level entry permission alone does not establish a physical actor-sized permission barrier; the door adapter must supply that box explicitly.

## Landing and ownership

The shared [contact landing policy](SINGLE_CELL_FLOOR_BOX_CONTRACT.md) validates float local locators without weakening start tests. World output must also stay within 0.1 mm of the geometric contact and no more than 1 micrometre inside the stopping-face normal. Bounded outward/backward correction must satisfy both coordinate-space checks.

Source contacts retain source ownership; valid target contacts adopt one transfer and a reusable target locator. A target-only initial seam overlap retains the source boundary with TargetSeamOverlap. Unrepresentable contact rolls the complete query back to its validated source, zero progress/transfers and UnrepresentablePortalBoxContactLanding. The per-floor geometric bridge remains the existing audited transfer policy. No sliding, moving-box interpolation, gravity or general world-collision claim is made.

## Verification

[Native gates](../poc/Stardust.Core.SmokeTests/PortalFloorBoxScenarios.cs) cover sloped forward/reverse transfer; target boxes contacting on the source approach; target global parameters/heights; 100 repeat stops; current/unknown permission; ties; snapshot-before-callback; invalid keys/counts; callback failure; transformed owners; asymmetric target frames; and explicit rollback when a large world translation cannot represent a contact.

[Independent verifier](../tools/python/verify_portal_floor_boxes.py) checks **2,400 synthetic requests** across eight quarter-turn/translation poses, two directions, five entry policies and rotated world boxes. It reconstructs the plane, portal/triangle split points, existing float ABI bridge and box intervals. Results: **138 Accepted, 1,560 BoxStopped, 702 FloorStopped**, with **173 one-transfer results / 2,227 zero-transfer results**, all returned locators reusable. Fixture SHA-256 `490E69F3F00F3D0B38853812C30A95989CF4832E43E112A6EEF43760D69CA411`, seed 2026093022. Triangle split points are required because conservative rounded-contact recognition applies at each segment start. A target-only bridge overlap is explicitly checked as a source-owner hold.

Shared-landing regressions retain 6,000 independent synthetic results and all 600 original-plane fixtures / 12,000 zero-drift repeats. Core builds and smoke pass. Original-floor regression obstacles are synthetic; this milestone adds no new original-door readiness claim.

```powershell
dotnet run --no-restore --project poc/Stardust.Core.SmokeTests
python tools/python/verify_portal_floor_boxes.py <private-output-directory> poc/Stardust.Core.SmokeTests/bin/Debug/net8.0/Stardust.Core.SmokeTests.dll
```

Next: supply captured physical door and directional permission boxes to this query, then verify one pinned original sloped doorway before enabling persistent actors or the Godot adapter.
