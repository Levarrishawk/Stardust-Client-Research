# October 1: complete original room triangle scene

## Question and scope

Can all selected original room triangles be retained and queried across the existing portal route with supported owner transitions? Close extraction, capacity, conservative contact and independent agreement. This is an isolated triangle/floor probe; current door leaves and the persistent prototype host actor are not yet composed with it.

## Inputs and geometry

Reuse the same pinned Mustafar POB/configuration/three meshes from the [selection checkpoint](ANALYSIS_CHECKPOINT_2026-10-01_MESH_COLLISION_SELECTION.md). The original POB loads original cell appearance/material and floor references through48 descending mounts. All candidate predicates agree; selector1 is explicit modern probe input, not a recovered actor selector.

| Cell | Source / eligible / nondegenerate triangles | Extraction SHA-256 |
|---|---:|---|
| 5 | 20,501 / 20,501 / 20,501 | `AA857A254B70D29EE233350C7672A683738127F4C5FCF3DBEDB427C240ED3F26` |
| 6 | 1,984 / 1,984 / 1,984 | `0BB4237A347FA1ED88D83BF4DCC4A66E2ED9BB26F2E4E00A0D82007DB10D02C0` |
| 7 | 20,501 / 20,501 / 20,501 | `83BCE9D62D2F4A262D21B659D5F3521119EF6DC678F8D54519583D854FA92095` |

Total42,986, zero degenerates, no truncation. `StaticMeshCollisionVerification` fingerprints signed little-endian source/eligible/degenerate/retained counts, followed by ascending source IDs and nine raw float32 coordinates per triangle. It validates accounting/order/coordinates; this is verification encoding, not an asset/protocol format. Independent raw Python extraction matches all three hashes. Capacity65,536 is explicit; complete-path work limits remain.

## Validation

Eight original route requests use hall start X−28.349918/Z64.5, two targets in each adjacent room and two rigid owner poses. Four transfer and finish clear; four transfer and stop on original target geometry. All eight reverse transfers reach the original hall locator. The four contacts hold across400 repeat requests.64 full-scene raw body queries match exhaustive records exactly; hierarchy admits1,536 candidates versus2,751,104 exhaustive tests. These are work counts, not frame-time claims.

`tools/python/verify_original_portal_triangles.py` independently rereads mesh/floor bytes, matches extraction, checks126 floor-plane points and16 LP candidate contacts. Outcomes and reported contact geometry agree; maximum contact-parameter error3.885780586188048e-15. Near-equal LP contacts are treated as a geometric tie family for independently matching the native reported triangle. Native smoke and previous original hall gates remain passing. Full-scene decoder/fingerprint changes build successfully.

Reproduce native `--original-portal-triangles ROOT --configuration CONFIG --member appearance/thm_must_mining_outpost.pob --expected-sha256 79085C909D585A9530017150EB07DA49EDAABA1E010C48D845FFD19F97B4D818`; save output under ignored evidence. Supply it to the Python verifier with explicit ROOT/CONFIG and configuration hash `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`.

## Material limitation found

The target contacts are low sloped rendered floor features, **not enclosing walls**. In room5, triangle6889 begins at Y17.769765853881836/Z57.988990783691406 and rises to Y17.869766235351562/Z57.29402160644531. The overlapping authored FLOR triangle31 stays approximately Y17.769766 across that region. Room7 has the mirrored case at Z71.16876983642578–71.86373901367188. **Confirmed asset discrepancy:** roughly0.1 m render rise above the flat movement surface. The declared body box reaches the render ramp ahead of its feet;0.1 mm micro-clearance appropriately does not erase this real rise.

These stops are mathematically correct for the modern world-axis body policy. They do not establish original actor behavior. Raising clearance until the stops disappear or discarding all upward triangles would hide an unresolved compatibility decision. Wider actor/host activation needs the original floor/body collision relationship, or an explicitly limited inspection mode.

## Confidence and next action

**Confirmed:** full owned extraction, capacity, independent original contact and supported owner transitions for the bounded probe. **Unknown:** original actor shape/contact offset, static-mesh participation and ordering relative to FLOR. No original executable execution, new semantic names or metrics change (2,599 names/345 packets/89% and94% research measures).

Next bounded static probe: trace callers around `01267380`, `012647B0` and `01267A90`, recover the body's floor/contact-height relationship and whether grounded motion consumes shader-selected mesh segments. Then choose a defensible actor integration boundary.
