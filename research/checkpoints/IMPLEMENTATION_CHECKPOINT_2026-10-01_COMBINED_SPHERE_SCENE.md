# October 1, 2026 — Combined sphere room and door guard

## Bounded question

Can the explicit modern room-triangle guard compose with persistent sphere doors, preserve supported floor ownership, and remain within the preview tick budget on the existing original route? Stop at owned extraction, earliest-contact composition, atomic resource rejection and original host acceptance.

## Implementation and evidence

`InteriorSceneTriangleGuardReader` extracts owned geometry for direct loaded static-mesh room roots using an explicit selector. Source and retained triangle budgets are separate; default retained capacity is2,048, explicit maximum65,536. At most64 selected cells and65,536 source triangles are accepted. Component/detail composition is deliberately unsupported. Shader candidates must agree on collision eligibility. This is a modern selected guard; original actor selector and mesh participation remain unknown.

`InteriorDoorActorScene` accepts the guard only with explicit sphere volume. It preflights the complete static path before controller mutation, then captures door/barrier contact and compares global progress. Doors win exact contact ties; triangles win a tie against floor-only stopping. A changed landing must be accepted and contact-free under stationary static and dynamic queries. Unsupported combined landing retains initial position/owner with zero transfers. Initial strict overlap may remain held; no depenetration, stepping or sliding is supplied. Static contacts retain obstacle cell/source triangle identity.

The combined content worker creates the guard before delivery. Launcher `-SceneTriangleGuard` requires the paired actor profile and textured scene geometry. The original sample uses explicit selector1 and65,536 capacity; all42,986 source/eligible triangles survive with zero degenerates. Each source still resolves externally once:70 effective unchanged sources/9,622,375 bytes.

## Validation

Full native smoke passes synthetic source/target earliest contacts, exact tie policy, owned-input mutation, callback count/failure,400 held-contact ticks, retreat, owner transfer and reset. Original `--original-interior-sphere-guard` passes2,880 ticks/24 transfers/182 contacts with the prior phase endpoints. Independent `verify_original_interior_sphere.py` reconciles all70 effective source identities, inherited profile values, canonical triangle hashes/counts for all three meshes and every returned floor plane. It does not independently reimplement dynamic controllers. The original corridor route has no positive room-triangle contact; positive combined contacts are synthetic.

A nine-segment original request exceeding262,144 segment-triangle work rejects before state/controller mutation. Timing the complete original scene tick in this local run gives mean0.702 ms, p951.155 ms, maximum12.133 ms across2,880 ticks. This is one local sample, not a universal performance guarantee; the existing linear conservative sphere bounds filter remains adequate for this gate.

Godot build passes with zero errors/warnings. Combined-guard original headless host passes resource delivery, both-owner manual/key/diagonal/timestamped routes, denial/retreat/reset, clock stall/pause/focus, mouse events, reflected placement and occupied closing hold/denial/retreat/release. GPU and cancellation gates were run for the immediately preceding profile preview, not rerun for this optional guard. Known nonfatal certificate-store diagnostic remains unchanged.

## Confidence, reproduction and next action

Confirmed bounded modern composition and unchanged source extraction. Original movement/closing/selection equivalence is not established. Existing research scores and canonical semantic names remain unchanged. Use the original profile preview command with `-SceneTriangleGuard -SceneGeometry -SceneTextures`; private inputs/pins are those recorded in the original-profile and complete-room checkpoints. The native flag above produces audit input for the existing independent verifier. All derived evidence remains ignored/private.

Next: load bounded human skeletal appearance descriptors from the object-root appearance reference, retaining exact original dependencies and rejecting unsupported versions before any character rendering claim.
