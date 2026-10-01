# October 1, 2026 — Portal sphere and original human profile

## Bounded question

Can explicit world-offset sphere contact preserve supported floor locators over one owner transfer, and does the recovered human sphere clear the original room render rises? Stop at the floor/triangle adapter and unchanged three-cell route. Door and actor-host integration follow separately.

## Implementation

`PortalFloorTriangleSphereWalker` owns and transforms each selected cell's triangles. It validates every transformed floor path, queries all loaded colliders on both legs, maps target contact to global progress and reuses supported landing with stationary/swept clearance validation. Unrepresentable contact holds the initial owner; target parameter-zero contact retains the source seam locator. Center offset is explicitly world space and constant across both legs. This is a modern policy; it does not choose original body orientation or obstacle participation.

## Validation and evidence

Native gates cover source approach to target geometry, target slope/global progress,100 held contacts, reverse transfer, rotated/translated owners, asymmetric target pose, owned input mutation and callback/resource failures.

Independent `verify_portal_floor_spheres.py` uses convex-distance minimization over synthetic slope legs.512 deterministic cases, seed2026100112, eight owner poses and varied X/Z sphere offsets agree:201 accepted,248 obstacle stops,63 floor stops;219 zero/293 one transfers. Maximum parameter error1.2247147740396258e-15. Fixture SHA-256 `58E8D9C84913D51E8997918135344B9157183649BFC2F3A956E063E2B873CE19`.

`--original-portal-sphere` loads the same unchanged POB/room/material/floor chain as the [complete room probe](IMPLEMENTATION_CHECKPOINT_2026-10-01_COMPLETE_ROOM_TRIANGLES.md), plus the five original inherited human object IFFs through `SharedCreatureTemplateProfileReader`. Recovered center(0,1,0), radius0.5 drives the query. All42,986 triangles remain present. Eight destinations under identity and quarter-turn/translation poses are accepted, including Z58/71, where the old box stopped at the0.1 m render rises.800 repeated endpoint requests remain stable and eight reverse owner transfers succeed.

Independent `verify_original_portal_sphere.py` re-extracts unchanged geometry, checks all three canonical hashes and126 floor-plane samples. Swept sphere bounds are disjoint from every original triangle on these eight paths; zero distance candidates are necessary. This is a conservative geometric separation proof for the sample, not eight positive sphere-contact comparisons.64 additional native queries with actual contacts match2,751,104 exhaustive triangle queries exactly, admitting1,876 candidates. Original template value/provenance agreement was separately established in the [object-root milestone](IMPLEMENTATION_CHECKPOINT_2026-10-01_ORIGINAL_CREATURE_TEMPLATE_PROFILE.md).

## Confidence and consequence

Confirmed bounded adapter and sampled original results. The raised sphere clears low render geometry without changing floor bytes, removing triangles or adding feet clearance. This resolves the modern box-versus-render-ramp obstruction for these requests. Original executable controller reachability, dynamic extent selection and mesh participation remain unresolved. Full body/head clearance and original step behavior are not represented by the sphere alone. Existing preview host remains unchanged.

## Reproduction and next action

Run full native smoke, `verify_portal_floor_spheres.py PRIVATE_OUTPUT CORE_DLL`, the pinned `--original-portal-sphere` flag, and `verify_original_portal_sphere.py ROOT CONFIGURATION NATIVE_OUTPUT --configuration-sha256 HASH`. Source pins remain those in the complete room and object-root checkpoints; no proprietary detail is promoted. No new semantic names or metric changes.

Next bounded slice: continuous sphere contact with the captured oriented door/barrier boxes, including interior overlap and rounded corners, before composing dynamic doors or enabling a preview volume option.
