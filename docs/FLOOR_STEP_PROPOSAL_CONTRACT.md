# Bounded modern floor step proposals

## Scope and policy

[FloorStepTransitionQuery](../../SWG-Godot-Client/poc/Stardust.Core/Assets/FloorStepTransitionQuery.cs) proposes a landing through a solid near-vertical floor strip after a strict walker returns UnsupportedSurface. It neither moves an actor nor grants collider clearance. Original locomotion step height, gravity and original step response remain Unknown.

The explicit modern default policy permits at most **0.2 m** absolute height change, **0.001 m** horizontal width relative to the encountered edge plane and **eight** vertical-strip triangles. Policy bounds are validated; the ordinary SingleCellFloorWalker remains unchanged unless a caller separately consumes a proposal.

The source prefix must finish at the declared supported triangle/edge and precise contact. Source and strip edges require reciprocal reversed vertex identity, raw passable flag 1 and no portal. Non-solid triangles, malformed topology, excessive span, exhausted strip budget, no usable landing and ambiguous landing are explicit rejections. Candidate supported surfaces must contain the precise XZ contact and extend in the requested movement direction. Their height must differ from the source within the declared limit. Returned float landing uses bounded canonicalization and passes the unchanged no-op floor start test.

Observation: original small-house thresholds include two solid vertical triangles reciprocally linking lower and upper walkable planes. Interpretation: bounded steps can preserve this connected topology while strict height-field walking cannot traverse the riser. Confidence: Confirmed geometry; modern stepping interpretation Strongly inferred. Implementation consequence: collision must check vertical travel and continuation before adoption. Rejected proposals retain source ownership; no teleport or implicit gravity is authorized by this query.

## Verification and limits

[Synthetic gates](../../SWG-Godot-Client/poc/Stardust.Core.SmokeTests/FloorStepTransitionScenarios.cs) pass rise/drop, reusable landing, height/resource limits and malformed/portal/non-solid rejection. [Independent original verification](../../SWG-Godot-Client/tools/python/verify_floor_step_proposals.py) consumes the reproducible private boundary screen, rechecks original source hashes and verifies accepted planes and reciprocal connectivity. On the 19 audited vertical boundaries, **nine proposals are Accepted and ten rejected by StripLimit**. All nine accepted landings are independently checked and reusable; rejected output holds its validated source. This is proposal acceptance, separate from doorway round-trip and actor-clearance metrics.

The component-wide strip budget deliberately rejects broader connected vertical geometry. Relaxing that behavior requires a local vertical-segment coverage proof and bounded route selection. Actor collision, partial step contact policy, stacked-floor ambiguity and complete integration remain next gates.
