# Piecewise floor-path actor-box sweep

## Supported boundary

[FloorPathBoxSweep](../../SWG-Godot-Client/poc/Stardust.Core/Assets/FloorPathBoxSweep.cs) sweeps a conservative axis-aligned actor box along the precise feet segments of a [floor trace](FLOOR_PATH_TRACE_CONTRACT.md). The path and collider use the same coordinate space. Actor center is feet plus half-height; actor world axes remain fixed. An immutable rigid box supplies its authored local bounds and pose. Each segment is transformed to box space in double, with the actor support bounds conservatively projected into that space.

The first interval overlap returns the original request parameter, segment index, precise center and transformed box-face normal. Ordered segments make the first hit the earliest hit; normal-axis ties keep the first axis. Clear returns the accepted prefix endpoint and parameter. This is an owned replacement algorithm, not recovered original narrowphase. Confidence: Confirmed synthetic analytic and independent interval fixtures; full original actor solver fidelity remains Unknown.

## Policies and limits

Allowed passage skips collision after complete path validation. False produces Blocked or InitialOverlap; null produces PermissionUnknown at an overlap. Optional outward retreat matches the existing collider's 1e-5 face recognition; actual interior overlap stays blocked. Contact reuse also holds inward motion at a rounded outside face within 1e-4 metres, matching the landing-correction budget. All other coordinates must also be within that budget of the expanded box. This is conservative early blocking, not increased passage clearance. No sliding, bounce, depenetration, moving-box time interpolation or locator adoption occurs.

Path validation rejects nonfinite bounds/points, negative actor halves, invalid parameters, more than 4,097 segments, parameter gaps, a terminal-prefix mismatch and adjacent positional discontinuity above 1e-6 metres. Empty movement has no swept contact. Direct caller-created traces must obey this contract. A returned geometric contact must be validated into a floor locator before an actor adopts it; float conversion alone does not establish a reusable locator.

## Verification

[Native gates](../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/FloorPathBoxScenarios.cs) cover a flat/ramp crease, global contact parameters, reverse traversal, unknown/allowed permission, initial overlap, touching retreat, traversal prefixes and malformed/oversized input. A low crease obstacle is hit by the real path and missed by an endpoint chord; an overhead obstacle has the opposite disagreement.

[Independent Python reference](../tools/python/verify_floor_path_boxes.py) generates **6,000 explicitly synthetic cases**, seed 2026093015, with analytically reconstructed crease planes and rotated box interval constraints. Results agree at 1e-9 parameter/center tolerance and 1e-6 normal tolerance: **1,946 Blocked, 2,898 Clear, 394 InitialOverlap, 762 PermissionUnknown**. Fixture SHA-256 `1FBCDCC8424605536BCB63EC74BF1482300B0C8410F1D4FA0300C8B0E525E7B7`.

```powershell
dotnet run --no-restore --project poc/Stardust.Core.SmokeTests
python tools/python/verify_floor_path_boxes.py <private-output-directory> poc/Stardust.Core.SmokeTests/bin/Debug/net8.0/Stardust.Core.SmokeTests.dll
```

## Next integration

Validate geometric contacts into reusable single-cell locators with deterministic precision failure. Portal transfer, target-cell collision and moving-leaf interaction require further gates; the existing persistent door actor remains level-only.
