# Same-cell surface-to-route selection — 2026-09-30

## Question and stopping condition

Can a solid floor pick select a nearest graph node in that same cell, without substituting a layout/other-cell graph, and drive the route inspector through the reflected host boundary? Close on owned-map/tie/absence tests, independent original probes and headless/GPU control gates. This is spatial route inspection, not legal movement.

## Evidence and identity

Uses [verified floor queries](IMPLEMENTATION_CHECKPOINT_2026-09-30_FLOOR_SURFACE_INSPECTION.md) and [directed route queries](IMPLEMENTATION_CHECKPOINT_2026-09-30_DIRECTED_ROUTE_INSPECTOR.md). Their independent all-corpus gates remain the prerequisites. Original YT-1300 root SHA `8B4B59335CF34BA91DAA5CCB092AF60C9A996849DB29E77EB154FD4DF202DF4A`; unchanged configuration SHA `22158E5BD9B7EBE5A068F10C4D15FCFAA7AB56C9130AF875A1BA47A9B41DA524`, 48 mounts. Baseline executable identity is unchanged from the earlier overnight static gates. No additional original execution or live contact.

## Findings and implementation consequence

**Confirmed implementation:** `PortalSurfacePathQuery` snapshots zero-based floor/graph cell maps while retaining immutable query objects. Up to 4,096 keys per map. Closest solid segment hit wins; exact cross-cell ties choose lowest cell index. Optional cell filter prevents other-cell hits. Nearest node uses the existing modern Euclidean query with ordinal tie policy. Missing and empty cell graphs retain distinct statuses and never fall back to the layout graph. No hit returns null; nonfinite segments, invalid keys/filter and oversized maps fail.

**Confirmed independent original agreement:** all 149 solid horizontal triangle-centroid segment probes match exhaustive plane/barycentric hits and raw-node Euclidean nearest selection in the seven YT-1300 floors. Every probe hits and identifies a same-cell node. Ordered cell/triangle/node/status plus hit coordinates agree. Source/layout/floor/graph identities also pass under 21 sources, seven floors/149 triangles and eight graphs. This denominator is one complete original scene, not every installed graph/floor pairing.

**Confirmed host connection:** floor picking retains a typed result through Z reflection. Right-clicking a surface switches the named-cell control, sets route start to its nearest node, and preserves that cell's route destination policy. Seven original centroid samples exercise these controls in headless and GPU hosts. Missing/empty graph picks remain inspectable without an invented route. Floor-only/cell selection and existing node/edge upload gates pass. Original legal walking, costs, cross-cell permission/doors and portal transitions are unimplemented.

## Validation and reproduction

Full native smoke suite passes owned maps, duplicate floor/node ties, cell filtering, missing/empty graph distinctions, no-hit/zero-length and invalid boundaries. The first synthetic connection test used a non-solid floor outside its vertical segment; the fixture was corrected to a solid triangle with a segment spanning its authored Y=2 plane. No production policy changed to satisfy it.

Run `verify_portal_scene.py ROOT CONFIG appearance/yt1300.pob SHA CORE_DLL --surface-paths`. The verifier reuses existing independent raw floor/graph readers and checks every current solid horizontal triangle centroid; native `--portal-surface-paths` caps probes at 10,000. Godot `-Interior -InteriorFloors -InteriorPaths` enables the connection. Use `-FloorOnly -Cell 1` for the hall inspector. Headless passes seven control probes; GPU passes the same controls, node transform readback and host coordinate fixture. Captures remain private and were visually inspected. Existing certificate-store startup error remains nonfatal.

## Next action

Recover the original FloorLocator/path-walk and portal transition rules before a compatibility movement slice. The existing route inspection can support that work, but does not authorize or establish gameplay movement. Registry, packet and research-score counts are unchanged.
