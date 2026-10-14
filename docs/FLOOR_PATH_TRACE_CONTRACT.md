# Piecewise single-cell floor paths

## Boundary and confidence

[SingleCellFloorWalker.Trace](../../SWG-Godot-Client/poc/Stardust.Core/Assets/SingleCellFloorWalker.cs) exposes the accepted geometric prefix of the existing horizontal-XZ walk. Each segment identifies its triangle, original request start/end parameters and double-precision three-dimensional feet points. Height follows that triangle's authored plane. A flat-to-ramp crease must retain the crease height; interpolating only the request's first and final heights produces a different path.

Observation: 17,658 installed requests produce 22,063 segments and 4,405 triangle transitions, including 8,393 sloped segments. Independent plane checks pass. Confidence: Confirmed for these pinned fixtures and synthetic tests. This is a replacement API and numerical policy; original actor traversal/order remains Unknown. No original function names or research scores change.

## Numerical and ownership rules

`Trace.Result` equals `Walk` for the same request. Existing status, portal/edge identity, crossing limit and strict start validation are preserved. Segment parameters are ordered, contiguous and within the returned prefix. A limit of N crossings produces at most N+1 segments; the maximum is 4,097. The returned collection is read-only and retains no external mutable topology. A valid no-op produces one stationary segment. Invalid starts produce no manufactured movement; an unrepresentable-contact rollback clears the path.

Geometric points retain double X/Z intersections and plane heights. An initial float-based trace failed a steep installed triangle because rounded X/Z no longer matched its calculated height. Double points remove that avoidable quantization. Conversion to `Vector3` is explicit through `ToVector3`; collision consumers must account for conversion error.

The path terminal point represents the theoretical geometric intersection. The authoritative reusable actor locator is separately stored in `Result.Position`, including [bounded contact canonicalization](FLOOR_CONTACT_STABILITY_CONTRACT.md) and float height rounding. Consumers must use the result for the next actor start and must not substitute the theoretical boundary point. Corpus terminal differences are below 0.1 mm; this is not a universal bound at arbitrarily large float coordinates.

## Verification and reproduction

[Synthetic tests](../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/FloorPathScenarios.cs) cover a flat/ramp crease, reverse traversal, boundary prefixes, traversal limits, invalid input, stationary movement and 1,000 seeded two-plane traces. The separate existing `Walk` result is compared on every optional native trace.

[Independent verifier](../../SWG-Godot-Client/tools/python/verify_floor_paths.py) pins the generated fixture and every original floor, then checks ordering, horizontal request parameters, adjacency, positional continuity, terminal agreement, reusable locators and five plane samples per segment. All **110,315 plane checks** pass over **4,441 eligible floors / 17,658 requests**. Outcomes remain 8,846 Accepted, 4,370 BlockedEdge, 3,970 PortalBoundary, 384 OpenBoundary and 88 UnsupportedEdgePolicy.

Generate the fixtures with the existing floor-walk verifier, then run:

```powershell
python tools/python/verify_floor_paths.py C:\Stardust-DEV C:\Stardust-DEV\swgemu_live.cfg <private-walk-fixtures.json> <private-output-directory> poc/Stardust.Core.SmokeTests/bin/Debug/net8.0/Stardust.Core.SmokeTests.dll
```

## Next boundary

Sweep conservative actor boxes along these individual three-dimensional segments before integrating slopes with portal transfer. Door actors remain level-only. Floor traces alone do not establish full actor/world collision, sliding, moving-leaf response, slope limits or original solver fidelity.
